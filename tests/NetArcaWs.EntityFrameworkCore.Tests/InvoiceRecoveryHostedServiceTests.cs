using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.Tests;

public sealed class InvoiceRecoveryHostedServiceTests
{
    [Fact]
    public void Worker_is_not_registered_without_explicit_opt_in()
    {
        ServiceCollection services = [];
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        provider.GetServices<IHostedService>().Should().BeEmpty();
        provider.GetService<IInvoiceRecoveryProcessor>().Should().BeNull();
        provider.GetService<IInvoiceRecoveryQueue>().Should().BeNull();
    }

    [Theory]
    [InlineData("lease-zero")]
    [InlineData("lease-too-long")]
    [InlineData("idle-zero")]
    [InlineData("idle-too-long")]
    [InlineData("attempts-zero")]
    [InlineData("attempts-too-many")]
    [InlineData("base-zero")]
    [InlineData("base-over-max")]
    [InlineData("max-backoff-zero")]
    [InlineData("max-backoff-too-long")]
    [InlineData("jitter-nan")]
    [InlineData("jitter-infinity")]
    [InlineData("jitter-negative")]
    [InlineData("jitter-over-one")]
    public void Worker_rejects_every_invalid_option_family(string invalidOption)
    {
        ServiceCollection services = [];
        InvoiceRecoveryScope scope = new("tenant-a", [ArcaService.Wsfev1]);

        Action configure = () => services.AddNetArcaWsInvoiceRecoveryWorker(scope, options =>
        {
            switch (invalidOption)
            {
                case "lease-zero": options.LeaseDuration = TimeSpan.Zero; break;
                case "lease-too-long": options.LeaseDuration = TimeSpan.FromMinutes(30).Add(TimeSpan.FromTicks(1)); break;
                case "idle-zero": options.IdleInterval = TimeSpan.Zero; break;
                case "idle-too-long": options.IdleInterval = TimeSpan.FromDays(1).Add(TimeSpan.FromTicks(1)); break;
                case "attempts-zero": options.MaxAttempts = 0; break;
                case "attempts-too-many": options.MaxAttempts = 1001; break;
                case "base-zero": options.BaseBackoff = TimeSpan.Zero; break;
                case "base-over-max": options.BaseBackoff = TimeSpan.FromMinutes(6); break;
                case "max-backoff-zero": options.MaxBackoff = TimeSpan.Zero; break;
                case "max-backoff-too-long": options.MaxBackoff = TimeSpan.FromDays(1).Add(TimeSpan.FromTicks(1)); break;
                case "jitter-nan": options.JitterRatio = double.NaN; break;
                case "jitter-infinity": options.JitterRatio = double.PositiveInfinity; break;
                case "jitter-negative": options.JitterRatio = -0.01; break;
                case "jitter-over-one": options.JitterRatio = 1.01; break;
            }
        });

        configure.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Worker_accepts_documented_option_boundaries()
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddSingleton<IInvoiceRecoveryQueue>(new NoWorkQueue());
        services.AddScoped<IInvoiceRecoveryContextResolver, UnusedResolver>();
        services.AddScoped(_ => new SafeInvoiceService(null!, null!, null!, null!));
        services.AddScoped<IWsfev1Service>(_ => null!);

        services.AddNetArcaWsInvoiceRecoveryWorker(new InvoiceRecoveryScope("tenant-a", [ArcaService.Wsfev1]), options =>
        {
            options.LeaseDuration = TimeSpan.FromMinutes(30);
            options.IdleInterval = TimeSpan.FromDays(1);
            options.MaxAttempts = 1000;
            options.BaseBackoff = TimeSpan.FromDays(1);
            options.MaxBackoff = TimeSpan.FromDays(1);
            options.JitterRatio = 1;
        }).Should().BeSameAs(services);
    }

    [Fact]
    public async Task Hosted_loop_uses_a_scoped_resolver_and_stops_during_a_long_idle_wait()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var queue = new NoWorkQueue();
        var resolverLifecycle = new ResolverLifecycle();
        services.AddSingleton<IInvoiceRecoveryQueue>(queue);
        services.AddScoped<IInvoiceRecoveryContextResolver>(_ => new TrackingResolver(resolverLifecycle));
        services.AddScoped(_ => new SafeInvoiceService(null!, null!, null!, null!));
        services.AddScoped<IWsfev1Service>(_ => null!);
        services.AddNetArcaWsInvoiceRecoveryWorker(new InvoiceRecoveryScope("tenant-a", [ArcaService.Wsfev1]),
            options => options.IdleInterval = TimeSpan.FromDays(1));

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        IHostedService hosted = provider.GetServices<IHostedService>().Should().ContainSingle().Subject;
        await hosted.StartAsync(TestContext.Current.CancellationToken);
        await queue.Claimed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await hosted.StopAsync(stopTimeout.Token);

        stopTimeout.IsCancellationRequested.Should().BeFalse();
        queue.ClaimCalls.Should().Be(1);
        resolverLifecycle.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task Hosted_loop_logs_safe_failure_signal_without_exception_details()
    {
        var services = new ServiceCollection();
        var logs = new CapturingLoggerProvider();
        services.AddLogging(builder => builder.AddProvider(logs));
        var queue = new NoWorkQueue { Failure = new InvalidOperationException("SENSITIVE-HOST-ERROR") };
        services.AddSingleton<IInvoiceRecoveryQueue>(queue);
        services.AddScoped<IInvoiceRecoveryContextResolver, UnusedResolver>();
        services.AddScoped(_ => new SafeInvoiceService(null!, null!, null!, null!));
        services.AddScoped<IWsfev1Service>(_ => null!);
        services.AddNetArcaWsInvoiceRecoveryWorker(new InvoiceRecoveryScope("tenant-a", [ArcaService.Wsfev1]),
            options => options.IdleInterval = TimeSpan.FromDays(1));

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        IHostedService hosted = provider.GetServices<IHostedService>().Should().ContainSingle().Subject;
        await hosted.StartAsync(TestContext.Current.CancellationToken);
        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            await queue.Claimed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            await logs.Written.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        }
        finally
        {
            await hosted.StopAsync(stopTimeout.Token);
        }

        string captured = string.Join("\n", logs.Messages);
        captured.Should().NotBeEmpty();
        captured.Should().NotContain("SENSITIVE-HOST-ERROR");
    }

    private sealed class ResolverLifecycle { public int DisposeCalls; }

    private sealed class TrackingResolver(ResolverLifecycle lifecycle) : IInvoiceRecoveryContextResolver, IDisposable
    {
        public ValueTask<ArcaTenantContext> ResolveAsync(InvoiceRecoveryWorkItem workItem,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void Dispose() => Interlocked.Increment(ref lifecycle.DisposeCalls);
    }

    private sealed class UnusedResolver : IInvoiceRecoveryContextResolver
    {
        public ValueTask<ArcaTenantContext> ResolveAsync(InvoiceRecoveryWorkItem workItem,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class NoWorkQueue : IInvoiceRecoveryQueue
    {
        private int claimCalls;
        public TaskCompletionSource Claimed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Exception? Failure { get; init; }
        public int ClaimCalls => Volatile.Read(ref claimCalls);
        public Task<InvoiceRecoveryLease?> TryClaimAsync(InvoiceRecoveryScope scope, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref claimCalls);
            Claimed.TrySetResult();
            if (Failure is not null) return Task.FromException<InvoiceRecoveryLease?>(Failure);
            return Task.FromResult<InvoiceRecoveryLease?>(null);
        }
        public Task<bool> IsCurrentAsync(InvoiceRecoveryLease lease, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task CompleteAsync(InvoiceRecoveryLease lease, InvoiceRecoveryDisposition disposition, InvoiceRecoverySafeReason safeReason,
            DateTimeOffset? nextAvailable = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<InvoiceOperation> PrepareAndEnqueueAsync(InvoiceSubmission submission, string? credentialReference, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<InvoiceOperation> ScheduleAsync(string tenantId, string idempotencyKey, long expectedInvoiceVersion, string? credentialReference, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<InvoiceRecoveryMetadata?> FindAsync(InvoiceRecoveryScope scope, string idempotencyKey, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<string> Messages { get; } = [];
        public TaskCompletionSource Written { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);
        public void Dispose() { }

        private sealed class CapturingLogger(CapturingLoggerProvider owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                lock (owner.Messages) owner.Messages.Add(formatter(state, exception) + exception?.Message);
                owner.Written.TrySetResult();
            }
        }
    }
}
