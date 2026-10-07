using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NetArcaWs.EntityFrameworkCore.MySql;

/// <summary>Registers the optional MySQL or MariaDB NetArcaWs persistence provider.</summary>
public static class NetArcaWsMySqlServiceCollectionExtensions
{
    /// <summary>Registers a dedicated context factory and the explicitly selected NetArcaWs stores.</summary>
    /// <remarks>Does not connect, initialize schema, or enable database execution retries.</remarks>
    public static IServiceCollection AddNetArcaWsMySqlStores(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion,
        NetArcaWsModelOptions modelOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(serverVersion);
        ArgumentNullException.ThrowIfNull(modelOptions);
        if (!modelOptions.InvoicingEnabled && !modelOptions.WsaaTicketsEnabled && !modelOptions.CertificatesEnabled)
            throw new ArgumentException("Select at least one NetArcaWs persistence capability before registering MySQL stores.", nameof(modelOptions));

        services.TryAddSingleton(modelOptions);
        services.AddDbContextFactory<ArcaWsDbContext>((provider, options) =>
        {
            NetArcaWsModelOptions registeredOptions = provider.GetRequiredService<NetArcaWsModelOptions>();
            if (!ReferenceEquals(registeredOptions, modelOptions))
                throw new InvalidOperationException("The registered NetArcaWs model selection differs from the MySQL store selection.");
            options.UseMySql(connectionString, serverVersion);
        });
        services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(modelOptions);
        return services;
    }
}
