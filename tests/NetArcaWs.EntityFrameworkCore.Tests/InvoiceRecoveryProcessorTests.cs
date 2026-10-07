using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetArcaWs.Cryptography;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using System.Reflection;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.Tests;

public sealed class InvoiceRecoveryProcessorTests
{
    [Fact]
    public void Worker_registration_is_opt_in_and_supports_scoped_resolvers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IInvoiceRecoveryContextResolver, ScopedResolver>();
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder =>
            builder.AddInvoicing(ArcaService.Wsfev1).AddInvoiceRecovery(ArcaService.Wsfev1));
        services.AddDbContextFactory<ArcaWsDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(modelOptions);
        services.AddScoped<NetArcaWs.Services.IWsfev1Service>(_ => null!);
        services.AddScoped(_ => new SafeInvoiceService(null!, null!, null!, null!));
        InvoiceRecoveryScope scope = new("tenant-a", [ArcaService.Wsfev1]);

        services.AddNetArcaWsInvoiceRecoveryWorker(scope);

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>().Should().ContainSingle();
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        first.ServiceProvider.GetRequiredService<IInvoiceRecoveryProcessor>().Should().NotBeSameAs(
            second.ServiceProvider.GetRequiredService<IInvoiceRecoveryProcessor>());
        first.ServiceProvider.GetRequiredService<IInvoiceRecoveryContextResolver>().Should().NotBeSameAs(
            second.ServiceProvider.GetRequiredService<IInvoiceRecoveryContextResolver>());
    }

    [Fact]
    public async Task Expired_claim_during_context_resolution_is_fenced_before_safe_invoice_service()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string path = Path.Combine(Path.GetTempPath(), $"netarcaws-processor-{Guid.NewGuid():N}.sqlite3");
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder =>
            builder.AddInvoicing(ArcaService.Wsfev1).AddInvoiceRecovery(ArcaService.Wsfev1));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextFactory<ArcaWsDbContext>(options => options.UseSqlite($"Data Source={path};Pooling=False"));
        services.AddSingleton<TimeProvider>(clock);
        services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(modelOptions);
        services.AddScoped<NetArcaWs.Services.IWsfev1Service>(_ => null!);
        services.AddScoped(_ => new SafeInvoiceService(null!, null!, null!, null!));
        services.AddScoped<BlockingResolver>();
        services.AddScoped<IInvoiceRecoveryContextResolver>(provider => provider.GetRequiredService<BlockingResolver>());
        InvoiceRecoveryScope scope = new("tenant-a", [ArcaService.Wsfev1]);
        services.AddNetArcaWsInvoiceRecoveryWorker(scope);

        try
        {
            using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            using (ArcaWsDbContext context = provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContext())
                context.Database.EnsureCreated();
            IInvoiceRecoveryQueue queue = provider.GetRequiredService<IInvoiceRecoveryQueue>();
            InvoiceSubmission submission = Submission();
            await queue.PrepareAndEnqueueAsync(submission, "credential-pinned", cancellationToken);

            using IServiceScope processingScope = provider.CreateScope();
            BlockingResolver resolver = processingScope.ServiceProvider.GetRequiredService<BlockingResolver>();
            Task<bool> processing = processingScope.ServiceProvider.GetRequiredService<IInvoiceRecoveryProcessor>().RunOnceAsync(scope, cancellationToken);
            await resolver.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            resolver.WorkItem!.CredentialReference.Should().Be("credential-pinned");

            try
            {
                clock.Advance(TimeSpan.FromMinutes(6));
                InvoiceRecoveryLease replacement = (await queue.TryClaimAsync(scope, TimeSpan.FromMinutes(5), cancellationToken))!;
                replacement.Generation.Should().BeGreaterThan(1);
                replacement.CurrentOperation.State.Should().Be(InvoiceState.Unknown);
                resolver.Release.TrySetResult(CreateContext());
                (await processing).Should().BeTrue();
                (await queue.IsCurrentAsync(replacement, cancellationToken)).Should().BeTrue();
            }
            finally
            {
                resolver.Release.TrySetResult(CreateContext());
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Theory]
    [InlineData(InvoiceState.Prepared, "FECAESolicitarAsync", "FECompConsultarAsync")]
    [InlineData(InvoiceState.Unknown, "FECompConsultarAsync", "FECAESolicitarAsync")]
    public async Task Only_fresh_prepared_is_authorized_and_unknown_is_query_only(
        InvoiceState initialState, string expectedCall, string forbiddenCall)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var fixture = new ProcessorFixture();
        InvoiceSubmission submission = SafeInvoiceService.CreateWsfeSubmission(CreateContext(), "processor-dispatch", Request());
        await fixture.Queue.PrepareAndEnqueueAsync(submission, "credential-pinned", cancellationToken);
        if (initialState == InvoiceState.Unknown)
        {
            InvoiceLease journalLease = (await fixture.Journal.TryAcquireAsync(submission.TenantId,
                submission.IdempotencyKey, TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
            await fixture.Journal.CompleteAsync(journalLease, new InvoiceDecision(InvoiceState.Unknown), cancellationToken);
        }

        bool processed = await fixture.Processor.RunOnceAsync(new InvoiceRecoveryScope(submission.TenantId, [ArcaService.Wsfev1]), cancellationToken);

        processed.Should().BeTrue();
        fixture.SoapProxy.Calls.Should().ContainSingle(expectedCall);
        fixture.SoapProxy.Calls.Should().NotContain(forbiddenCall);
        InvoiceRecoveryMetadata metadata = (await fixture.Queue.FindAsync(
            new InvoiceRecoveryScope(submission.TenantId, [ArcaService.Wsfev1]), submission.IdempotencyKey, cancellationToken))!;
        metadata.State.Should().Be(InvoiceRecoveryState.Scheduled);
        metadata.LastReason.Should().Be(InvoiceRecoverySafeReason.QueryEmpty);
    }

    [Fact]
    public async Task Context_mismatch_suspends_without_calling_soap()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var fixture = new ProcessorFixture(CreateContext("other-tenant"));
        InvoiceSubmission submission = SafeInvoiceService.CreateWsfeSubmission(CreateContext(), "processor-context", Request());
        await fixture.Queue.PrepareAndEnqueueAsync(submission, "credential-pinned", cancellationToken);

        await fixture.Processor.RunOnceAsync(new InvoiceRecoveryScope(submission.TenantId, [ArcaService.Wsfev1]), cancellationToken);

        fixture.SoapProxy.Calls.Should().BeEmpty();
        InvoiceRecoveryMetadata metadata = (await fixture.Queue.FindAsync(
            new InvoiceRecoveryScope(submission.TenantId, [ArcaService.Wsfev1]), submission.IdempotencyKey, cancellationToken))!;
        metadata.State.Should().Be(InvoiceRecoveryState.Suspended);
        metadata.LastReason.Should().Be(InvoiceRecoverySafeReason.AuthorizationDenied);
    }

    [Fact]
    public async Task Resolver_failure_persists_safe_reason_without_logging_exception_or_invoice_data()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var fixture = new ProcessorFixture(resolverFailure: new InvalidOperationException("SENSITIVE-RESOLVER-DETAIL"));
        InvoiceSubmission submission = SafeInvoiceService.CreateWsfeSubmission(CreateContext(), "SENSITIVE-INVOICE-KEY", Request());
        await fixture.Queue.PrepareAndEnqueueAsync(submission, "credential-pinned", cancellationToken);
        InvoiceRecoveryScope scope = new(submission.TenantId, [ArcaService.Wsfev1]);

        await fixture.Processor.RunOnceAsync(scope, cancellationToken);

        fixture.SoapProxy.Calls.Should().BeEmpty();
        InvoiceRecoveryMetadata metadata = (await fixture.Queue.FindAsync(scope, submission.IdempotencyKey, cancellationToken))!;
        metadata.State.Should().Be(InvoiceRecoveryState.Suspended);
        metadata.LastReason.Should().Be(InvoiceRecoverySafeReason.CredentialUnavailable);
        string logs = string.Join("\n", fixture.Logs.Messages);
        logs.Should().NotContain("SENSITIVE-RESOLVER-DETAIL");
        logs.Should().NotContain("SENSITIVE-INVOICE-KEY");
    }

    [Theory]
    [InlineData(InvoiceState.Authorized, InvoiceRecoveryState.Completed)]
    [InlineData(InvoiceState.Rejected, InvoiceRecoveryState.Completed)]
    [InlineData(InvoiceState.Conflict, InvoiceRecoveryState.Suspended)]
    [InlineData(InvoiceState.ManualReview, InvoiceRecoveryState.Suspended)]
    public async Task Terminal_or_review_invoice_states_are_not_polled(
        InvoiceState invoiceState, InvoiceRecoveryState expectedQueueState)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var fixture = new ProcessorFixture();
        InvoiceSubmission submission = SafeInvoiceService.CreateWsfeSubmission(CreateContext(), "processor-terminal", Request());
        await fixture.Queue.PrepareAndEnqueueAsync(submission, "credential-pinned", cancellationToken);
        InvoiceLease journalLease = (await fixture.Journal.TryAcquireAsync(submission.TenantId,
            submission.IdempotencyKey, TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        await fixture.Journal.CompleteAsync(journalLease, new InvoiceDecision(invoiceState,
            invoiceState == InvoiceState.Authorized ? "71234567890123" : null), cancellationToken);

        bool processed = await fixture.Processor.RunOnceAsync(new InvoiceRecoveryScope(submission.TenantId, [ArcaService.Wsfev1]), cancellationToken);

        processed.Should().BeFalse();
        fixture.SoapProxy.Calls.Should().BeEmpty();
        InvoiceRecoveryMetadata metadata = (await fixture.Queue.FindAsync(
            new InvoiceRecoveryScope(submission.TenantId, [ArcaService.Wsfev1]), submission.IdempotencyKey, cancellationToken))!;
        metadata.State.Should().Be(expectedQueueState);
    }

    [Fact]
    public async Task Cancellation_during_authorization_leaves_unknown_and_next_claim_queries_only()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var fixture = new ProcessorFixture();
        InvoiceSubmission submission = SafeInvoiceService.CreateWsfeSubmission(CreateContext(), "processor-cancel", Request());
        await fixture.Queue.PrepareAndEnqueueAsync(submission, "credential-pinned", cancellationToken);
        fixture.SoapProxy.BlockAuthorization = true;
        using var cancellation = new CancellationTokenSource();
        Task<bool> processing = fixture.Processor.RunOnceAsync(
            new InvoiceRecoveryScope(submission.TenantId, [ArcaService.Wsfev1]), cancellation.Token);
        await fixture.SoapProxy.AuthorizeEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        cancellation.Cancel();
        (await processing).Should().BeTrue();

        InvoiceOperation afterCancellation = (await fixture.Journal.FindAsync(
            submission.TenantId, submission.IdempotencyKey, TestContext.Current.CancellationToken))!;
        afterCancellation.State.Should().Be(InvoiceState.Unknown);
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        await fixture.Processor.RunOnceAsync(new InvoiceRecoveryScope(submission.TenantId, [ArcaService.Wsfev1]), cancellationToken);

        fixture.SoapProxy.Calls.Should().ContainInOrder("FECAESolicitarAsync", "FECompConsultarAsync");
        fixture.SoapProxy.Calls.Count(call => call == "FECAESolicitarAsync").Should().Be(1);
        fixture.SoapProxy.Calls.Count(call => call == "FECompConsultarAsync").Should().Be(1);
    }

    [Fact]
    public async Task Ten_inconclusive_queries_suspend_with_capped_backoff()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var fixture = new ProcessorFixture(maxAttempts: 10);
        InvoiceSubmission submission = SafeInvoiceService.CreateWsfeSubmission(CreateContext(), "processor-max-attempts", Request());
        await fixture.Queue.PrepareAndEnqueueAsync(submission, "credential-pinned", cancellationToken);
        InvoiceLease journalLease = (await fixture.Journal.TryAcquireAsync(submission.TenantId,
            submission.IdempotencyKey, TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        await fixture.Journal.CompleteAsync(journalLease, new InvoiceDecision(InvoiceState.Unknown), cancellationToken);
        InvoiceRecoveryScope scope = new(submission.TenantId, [ArcaService.Wsfev1]);

        for (int attempt = 0; attempt < 10; attempt++)
        {
            await fixture.Processor.RunOnceAsync(scope, cancellationToken);
            if (attempt == 0)
            {
                InvoiceRecoveryMetadata first = (await fixture.Queue.FindAsync(scope, submission.IdempotencyKey, cancellationToken))!;
                (first.NextAvailable - fixture.Clock.GetUtcNow()).Should().BeCloseTo(TimeSpan.FromSeconds(4), TimeSpan.FromMilliseconds(1));
            }
            if (attempt == 7)
            {
                InvoiceRecoveryMetadata capped = (await fixture.Queue.FindAsync(scope, submission.IdempotencyKey, cancellationToken))!;
                (capped.NextAvailable - fixture.Clock.GetUtcNow()).Should().BeCloseTo(TimeSpan.FromMinutes(5), TimeSpan.FromMilliseconds(1));
            }
            if (attempt < 9) fixture.Clock.Advance(TimeSpan.FromDays(1));
        }

        fixture.SoapProxy.Calls.Count(call => call == "FECompConsultarAsync").Should().Be(10);
        InvoiceRecoveryMetadata metadata = (await fixture.Queue.FindAsync(scope, submission.IdempotencyKey, cancellationToken))!;
        metadata.Attempt.Should().Be(10);
        metadata.State.Should().Be(InvoiceRecoveryState.Suspended);
        metadata.LastReason.Should().Be(InvoiceRecoverySafeReason.MaxAttemptsReached);
    }

    [Fact]
    public async Task Persistence_failure_after_correlated_soap_response_is_reconciled_without_resubmission()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var fixture = new ProcessorFixture(failAuthorizedCompletion: true);
        InvoiceSubmission submission = SafeInvoiceService.CreateWsfeSubmission(CreateContext(), "processor-persist-fail", Request());
        await fixture.Queue.PrepareAndEnqueueAsync(submission, "credential-pinned", cancellationToken);
        fixture.SoapProxy.ResponseFactory = method => method == "FECAESolicitarAsync" ? AuthorizedResponse() : null;
        InvoiceRecoveryScope scope = new(submission.TenantId, [ArcaService.Wsfev1]);

        await fixture.Processor.RunOnceAsync(scope, cancellationToken);
        InvoiceOperation afterFailedPersistence = (await fixture.Journal.FindAsync(
            submission.TenantId, submission.IdempotencyKey, cancellationToken))!;
        afterFailedPersistence.State.Should().Be(InvoiceState.Unknown);
        fixture.SoapProxy.Calls.Should().ContainSingle("FECAESolicitarAsync");
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        await fixture.Processor.RunOnceAsync(scope, cancellationToken);

        fixture.SoapProxy.Calls.Count(call => call == "FECAESolicitarAsync").Should().Be(1);
        fixture.SoapProxy.Calls.Count(call => call == "FECompConsultarAsync").Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1801)]
    public void Worker_rejects_lease_durations_outside_supported_bounds(int seconds)
    {
        var services = new ServiceCollection();
        InvoiceRecoveryScope scope = new("tenant-a", [ArcaService.Wsfev1]);

        Action configure = () => services.AddNetArcaWsInvoiceRecoveryWorker(scope,
            options => options.LeaseDuration = TimeSpan.FromSeconds(seconds));

        configure.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static ArcaTenantContext CreateContext(string tenantId = "tenant-a") => new(tenantId, 30123456789,
        ArcaEnvironment.Homologation, WsaaCertificateContent.FromPkcs12([1]));

    private static InvoiceSubmission Submission() => new("tenant-a", "recovery-processor", "wsfe",
        new InvoiceIdentity(ArcaEnvironment.Homologation, 30123456789, 1, 1, 1), "<invoice>synthetic</invoice>");

    private static FecaeRequest Request() => new()
    {
        FeCabReq = new FecaeCabRequest { CantReg = 1, PtoVta = 1, CbteTipo = 1 },
        FeDetReq = { new FecaeDetRequest
        {
            Concepto = 1, DocTipo = 80, DocNro = 20304050607, CbteDesde = 1, CbteHasta = 1,
            CbteFch = "20261006", ImpTotal = 121, ImpNeto = 100, ImpIva = 21,
            MonId = "PES", MonCotiz = 1
        } }
    };

    private static FecaeSolicitarResponse AuthorizedResponse() => new()
    {
        FecaeSolicitarResult = new FecaeResponse
        {
            FeCabResp = new FecaeCabResponse
            {
                Cuit = 30123456789, PtoVta = 1, CbteTipo = 1, CantReg = 1, Resultado = "A"
            },
            FeDetResp = { new FecaeDetResponse
            {
                Concepto = 1, DocTipo = 80, DocNro = 20304050607, CbteDesde = 1, CbteHasta = 1,
                CbteFch = "20261006", Resultado = "A", Cae = "71234567890123"
            } }
        }
    };

    private sealed class BlockingResolver : IInvoiceRecoveryContextResolver
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<ArcaTenantContext> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public InvoiceRecoveryWorkItem? WorkItem { get; private set; }

        public async ValueTask<ArcaTenantContext> ResolveAsync(InvoiceRecoveryWorkItem workItem,
            CancellationToken cancellationToken = default)
        {
            WorkItem = workItem;
            Entered.TrySetResult();
            return await Release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset now = now;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now += duration;
    }

    private sealed class ProcessorFixture : IDisposable
    {
        private readonly string path = Path.Combine(Path.GetTempPath(), $"netarcaws-processor-{Guid.NewGuid():N}.sqlite3");
        private readonly ServiceProvider provider;

        public ProcessorFixture(ArcaTenantContext? resolverContext = null, int maxAttempts = 10,
            bool failAuthorizedCompletion = false, Exception? resolverFailure = null)
        {
            Clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
            Logs = new CapturingLoggerProvider();
            NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder =>
                builder.AddInvoicing(ArcaService.Wsfev1).AddInvoiceRecovery(ArcaService.Wsfev1));
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddProvider(Logs));
            services.AddDbContextFactory<ArcaWsDbContext>(options => options.UseSqlite($"Data Source={path};Pooling=False"));
            services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(modelOptions);
            services.AddSingleton<IInvoiceJournal>(sp => new FailingOnceJournal(
                sp.GetRequiredService<EfInvoiceJournal<ArcaWsDbContext>>(), failAuthorizedCompletion));
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<IWsfev1Service>(_ =>
            {
                IWsfev1Service proxy = DispatchProxy.Create<IWsfev1Service, CountingSoapProxy>();
                SoapProxy = (CountingSoapProxy)proxy;
                return proxy;
            });
            services.AddScoped<IInvoiceRecoveryContextResolver>(_ => new FixedResolver(resolverContext ?? CreateContext(), resolverFailure));
            services.AddScoped(sp => new SafeInvoiceService(new InvoiceCoordinator(sp.GetRequiredService<IInvoiceJournal>()),
                sp.GetRequiredService<IWsfev1Service>(), null!, null!));
            services.AddNetArcaWsInvoiceRecoveryWorker(new InvoiceRecoveryScope("tenant-a", [ArcaService.Wsfev1]),
                options => { options.JitterRatio = 0; options.MaxAttempts = maxAttempts; });
            provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            using ArcaWsDbContext context = provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContext();
            context.Database.EnsureCreated();
            Queue = provider.GetRequiredService<IInvoiceRecoveryQueue>();
            Journal = provider.GetRequiredService<IInvoiceJournal>();
            using IServiceScope scope = provider.CreateScope();
            Processor = scope.ServiceProvider.GetRequiredService<IInvoiceRecoveryProcessor>();
        }

        public IInvoiceRecoveryQueue Queue { get; }
        public IInvoiceJournal Journal { get; }
        public IInvoiceRecoveryProcessor Processor { get; }
        public CountingSoapProxy SoapProxy { get; private set; } = null!;
        public AdjustableTimeProvider Clock { get; }
        public CapturingLoggerProvider Logs { get; }

        public void Dispose()
        {
            provider.Dispose();
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private sealed class FixedResolver(ArcaTenantContext context, Exception? failure = null) : IInvoiceRecoveryContextResolver
    {
        public ValueTask<ArcaTenantContext> ResolveAsync(InvoiceRecoveryWorkItem workItem,
            CancellationToken cancellationToken = default) => failure is null
                ? ValueTask.FromResult(context)
                : ValueTask.FromException<ArcaTenantContext>(failure);
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<string> Messages { get; } = [];
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);
        public void Dispose() { }
    }

    private sealed class CapturingLogger(List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => messages.Add(formatter(state, exception) + exception?.Message);
    }

    public class CountingSoapProxy : DispatchProxy
    {
        public List<string> Calls { get; } = [];
        public bool BlockAuthorization { get; set; }
        public TaskCompletionSource AuthorizeEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Func<string, object?>? ResponseFactory { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls.Add(targetMethod!.Name);
            Type responseType = targetMethod.ReturnType.GenericTypeArguments[0];
            if (BlockAuthorization && targetMethod.Name == "FECAESolicitarAsync")
            {
                AuthorizeEntered.TrySetResult();
                CancellationToken token = args!.OfType<CancellationToken>().Single();
                return GetType().GetMethod(nameof(WaitForCancellation), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(responseType).Invoke(null, [token]);
            }
            object? response = ResponseFactory?.Invoke(targetMethod.Name) ?? Activator.CreateInstance(responseType);
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(responseType)
                .Invoke(null, [response]);
        }

        private static async Task<T> WaitForCancellation<T>(CancellationToken token)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return default!;
        }
    }

    private sealed class FailingOnceJournal(IInvoiceJournal inner, bool failAuthorizedCompletion) : IInvoiceJournal
    {
        private int remainingFailures = failAuthorizedCompletion ? 1 : 0;
        public Task<InvoiceOperation> PrepareAsync(InvoiceSubmission submission, CancellationToken cancellationToken = default) => inner.PrepareAsync(submission, cancellationToken);
        public Task<InvoiceOperation> ReviseRejectedAsync(InvoiceSubmission replacement, long expectedVersion, CancellationToken cancellationToken = default) => inner.ReviseRejectedAsync(replacement, expectedVersion, cancellationToken);
        public Task<IReadOnlyList<InvoiceOperation>> ListRevisionsAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default) => inner.ListRevisionsAsync(tenantId, idempotencyKey, cancellationToken);
        public Task<InvoiceOperation?> FindAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default) => inner.FindAsync(tenantId, idempotencyKey, cancellationToken);
        public Task<IReadOnlyList<InvoiceOperation>> ListPendingAsync(string tenantId, int limit = 100, CancellationToken cancellationToken = default) => inner.ListPendingAsync(tenantId, limit, cancellationToken);
        public Task<InvoiceLease?> TryAcquireAsync(string tenantId, string idempotencyKey, TimeSpan leaseDuration, bool reconciliation, CancellationToken cancellationToken = default) => inner.TryAcquireAsync(tenantId, idempotencyKey, leaseDuration, reconciliation, cancellationToken);
        public Task<InvoiceOperation> CompleteAsync(InvoiceLease lease, InvoiceDecision decision, CancellationToken cancellationToken = default)
        {
            if (decision.State == InvoiceState.Authorized && Interlocked.Exchange(ref remainingFailures, 0) == 1)
                return Task.FromException<InvoiceOperation>(new IOException("synthetic persistence failure after SOAP response"));
            return inner.CompleteAsync(lease, decision, cancellationToken);
        }
    }

    private sealed class ScopedResolver : IInvoiceRecoveryContextResolver
    {
        public ValueTask<NetArcaWs.Multitenancy.ArcaTenantContext> ResolveAsync(InvoiceRecoveryWorkItem workItem,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
