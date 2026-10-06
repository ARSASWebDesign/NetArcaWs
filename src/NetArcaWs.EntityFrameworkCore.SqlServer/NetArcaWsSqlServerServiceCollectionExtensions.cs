using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetArcaWs.EntityFrameworkCore;

namespace NetArcaWs.EntityFrameworkCore.SqlServer;

/// <summary>Registers the optional SQL Server NetArcaWs persistence provider.</summary>
public static class NetArcaWsSqlServerServiceCollectionExtensions
{
    /// <summary>Registers a dedicated context factory and the explicitly selected NetArcaWs stores.</summary>
    /// <remarks>Does not connect, initialize schema, or enable database execution-strategy retries.</remarks>
    public static IServiceCollection AddNetArcaWsSqlServerStores(
        this IServiceCollection services,
        string connectionString,
        NetArcaWsModelOptions modelOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(modelOptions);
        if (!modelOptions.InvoicingEnabled && !modelOptions.WsaaTicketsEnabled)
            throw new ArgumentException("Select at least one NetArcaWs persistence capability before registering SQL Server stores.", nameof(modelOptions));

        services.TryAddSingleton(modelOptions);
        services.AddDbContextFactory<ArcaWsDbContext>((provider, options) =>
        {
            NetArcaWsModelOptions registeredOptions = provider.GetRequiredService<NetArcaWsModelOptions>();
            if (!ReferenceEquals(registeredOptions, modelOptions))
                throw new InvalidOperationException("The registered NetArcaWs model selection differs from the SQL Server store selection.");
            options.UseSqlServer(connectionString);
        });
        services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(modelOptions);
        return services;
    }
}
