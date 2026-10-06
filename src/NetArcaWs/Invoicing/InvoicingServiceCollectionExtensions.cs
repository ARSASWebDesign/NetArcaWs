using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetArcaWs.Wsaa;

namespace NetArcaWs.Invoicing;

public static class InvoicingServiceCollectionExtensions
{
    /// <summary>Registers durable issuance using a provider already registered as IInvoiceJournal.</summary>
    public static IServiceCollection AddNetArcaWsInvoicing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddNetArcaWs();
        services.TryAddSingleton(provider => new InvoiceCoordinator(provider.GetRequiredService<IInvoiceJournal>()));
        services.TryAddSingleton<SafeInvoiceService>();
        return services;
    }

    /// <summary>Registers a durable SQLite journal shared by processes on one host.</summary>
    public static IServiceCollection AddNetArcaWsSqliteInvoicing(this IServiceCollection services, string databasePath)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var path = Path.GetFullPath(databasePath);
        services.TryAddSingleton<IInvoiceJournal>(provider => new SqliteInvoiceJournal(path, provider.GetRequiredService<TimeProvider>()));
        return services.AddNetArcaWsInvoicing();
    }
}
