using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore.Migrations;

namespace NetArcaWs.EntityFrameworkCore.Migrations.SqlServer;

public static class SqlServerMigrationsServiceCollectionExtensions
{
    public static IServiceCollection AddNetArcaWsSqlServerMigrations(this IServiceCollection services, string connectionString, NetArcaWsModelOptions modelOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(modelOptions);
        if (services.Any(x => x.ServiceType == typeof(INetArcaWsMigrator)))
            throw new InvalidOperationException("Only one NetArcaWs migration provider may be registered in a service collection.");
        var modules = new List<NetArcaWsPersistenceModule>(3);
        if (modelOptions.InvoicingEnabled) modules.Add(NetArcaWsPersistenceModule.Invoicing);
        if (modelOptions.WsaaTicketsEnabled) modules.Add(NetArcaWsPersistenceModule.WsaaTickets);
        if (modelOptions.CertificatesEnabled) modules.Add(NetArcaWsPersistenceModule.TenantCertificates);
        var factory = new SqlServerMigrationContextFactory(connectionString);
        services.AddSingleton<INetArcaWsMigrationContextFactory>(factory);
        services.AddSingleton<INetArcaWsMigrator>(new NetArcaWsMigrator(factory, Array.AsReadOnly(modules.ToArray())));
        return services;
    }
}
