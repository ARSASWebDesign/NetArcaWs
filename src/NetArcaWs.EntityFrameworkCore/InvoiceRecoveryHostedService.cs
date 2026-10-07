using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Services;

namespace NetArcaWs.EntityFrameworkCore;

internal sealed class InvoiceRecoveryHostedService(IServiceScopeFactory scopeFactory,
    InvoiceRecoveryScope scope, InvoiceRecoveryWorkerOptions options, TimeProvider timeProvider,
    ILogger<InvoiceRecoveryHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope serviceScope = scopeFactory.CreateAsyncScope();
                IInvoiceRecoveryProcessor processor = serviceScope.ServiceProvider.GetRequiredService<IInvoiceRecoveryProcessor>();
                bool claimed = await processor.RunOnceAsync(scope, stoppingToken).ConfigureAwait(false);
                if (!claimed)
                    await Task.Delay(options.IdleInterval, timeProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // Do not include exception messages or tenant/fiscal data in hosted diagnostics.
                logger.LogWarning(new EventId(1, "InvoiceRecoveryIterationFailed"),
                    "Invoice recovery iteration failed; retrying after the configured idle interval.");
                await Task.Delay(options.IdleInterval, timeProvider, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}

public static class InvoiceRecoveryServiceCollectionExtensions
{
    /// <summary>Opts into the durable invoice recovery processor and hosted polling loop.</summary>
    public static IServiceCollection AddNetArcaWsInvoiceRecoveryWorker(this IServiceCollection services,
        InvoiceRecoveryScope scope, Action<InvoiceRecoveryWorkerOptionsBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(scope);
        InvoiceRecoveryWorkerOptions options = InvoiceRecoveryOptions.Create(configure);

        Require<IInvoiceRecoveryQueue>(services);
        Require<IInvoiceRecoveryContextResolver>(services);
        Require<SafeInvoiceService>(services);
        ServiceDescriptor? optionsDescriptor = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(NetArcaWsModelOptions));
        if (optionsDescriptor?.ImplementationInstance is NetArcaWsModelOptions modelOptions &&
            (!modelOptions.InvoiceRecoveryEnabled || scope.Services.Any(service => !modelOptions.InvoiceRecoveryServices.Contains(service)) ||
             scope.Services.Any(service => !modelOptions.InvoicingServices.Contains(service))))
            throw new InvalidOperationException("The recovery scope must be covered by the selected EF recovery and invoicing services.");
        foreach (ArcaService service in scope.Services)
        {
            Type required = service switch
            {
                ArcaService.Wsfev1 => typeof(IWsfev1Service),
                ArcaService.Wsfexv1 => typeof(IWsfexv1Service),
                ArcaService.Wsmtxca => typeof(IWsmtxcav1Service),
                _ => throw new ArgumentException("The recovery scope contains an unsupported service.", nameof(scope))
            };
            Require(services, required);
        }

        services.TryAddSingleton(scope);
        services.TryAddSingleton(options);
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddScoped<IInvoiceRecoveryProcessor, InvoiceRecoveryProcessor>();
        services.AddHostedService<InvoiceRecoveryHostedService>();
        return services;
    }

    private static void Require<T>(IServiceCollection services) => Require(services, typeof(T));

    private static void Require(IServiceCollection services, Type serviceType)
    {
        if (!services.Any(descriptor => descriptor.ServiceType == serviceType))
            throw new InvalidOperationException($"Register {serviceType.Name} before enabling the invoice recovery worker.");
    }
}
