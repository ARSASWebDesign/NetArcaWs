using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NetArcaWs.EntityFrameworkCore.PostgreSql;

/// <summary>Registers the optional PostgreSQL NetArcaWs persistence provider.</summary>
public static class NetArcaWsPostgreSqlServiceCollectionExtensions
{
    /// <summary>Registers a dedicated context factory and the explicitly selected NetArcaWs stores.</summary>
    /// <remarks>Does not connect, initialize schema, or enable database execution retries.</remarks>
    public static IServiceCollection AddNetArcaWsPostgreSqlStores(
        this IServiceCollection services,
        string connectionString,
        NetArcaWsModelOptions modelOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(modelOptions);
        if (!modelOptions.InvoicingEnabled && !modelOptions.WsaaTicketsEnabled && !modelOptions.CertificatesEnabled)
            throw new ArgumentException("Select at least one NetArcaWs persistence capability before registering PostgreSQL stores.", nameof(modelOptions));

        services.TryAddSingleton(modelOptions);
        services.AddDbContextFactory<ArcaWsDbContext>((provider, options) =>
        {
            NetArcaWsModelOptions registeredOptions = provider.GetRequiredService<NetArcaWsModelOptions>();
            if (!ReferenceEquals(registeredOptions, modelOptions))
                throw new InvalidOperationException("The registered NetArcaWs model selection differs from the PostgreSQL store selection.");
            options.UseNpgsql(connectionString);
        });
        services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(modelOptions);
        return services;
    }
}
