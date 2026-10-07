using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore.Migrations;

namespace NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql;

public static class PostgreSqlMigrationsServiceCollectionExtensions
{
    public static IServiceCollection AddNetArcaWsPostgreSqlMigrations(this IServiceCollection services, string connectionString, NetArcaWsModelOptions modelOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(modelOptions);
        if (services.Any(x => x.ServiceType == typeof(INetArcaWsMigrator)))
            throw new InvalidOperationException("Only one NetArcaWs migration provider may be registered in a service collection.");
        var modules = new List<NetArcaWsPersistenceModule>(2);
        if (modelOptions.InvoicingEnabled) modules.Add(NetArcaWsPersistenceModule.Invoicing);
        if (modelOptions.WsaaTicketsEnabled) modules.Add(NetArcaWsPersistenceModule.WsaaTickets);
        var factory = new PostgreSqlMigrationContextFactory(connectionString);
        services.AddSingleton<INetArcaWsMigrationContextFactory>(factory);
        services.AddSingleton<INetArcaWsMigrator>(new NetArcaWsMigrator(factory, Array.AsReadOnly(modules.ToArray())));
        return services;
    }
}
