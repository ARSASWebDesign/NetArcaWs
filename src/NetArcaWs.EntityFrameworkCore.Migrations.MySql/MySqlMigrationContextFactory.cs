using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NetArcaWs.EntityFrameworkCore.Migrations;

namespace NetArcaWs.EntityFrameworkCore.Migrations.MySql;

public sealed class MySqlMigrationContextFactory(string connectionString, MySqlServerVersion serverVersion) : INetArcaWsMigrationContextFactory
{
    private static readonly HashSet<string> OwnedTables = new(StringComparer.Ordinal)
    {
        "NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations", "NetArcaWsaaTickets",
        "NetArcaCertificateSlots", "NetArcaCertificateVersions", "NetArcaInvoiceRecoveryJobs", "__NetArcaWsInvoiceMigrations", "__NetArcaWsTicketMigrations", "__NetArcaWsCertificateMigrations", "__NetArcaWsInvoiceRecoveryMigrations"
    };

    public NetArcaWsMigrationProvider Provider => NetArcaWsMigrationProvider.MySql;

    public DbContext CreateContext(NetArcaWsPersistenceModule module) => module switch
    {
        NetArcaWsPersistenceModule.Invoicing => new MySqlInvoicingMigrationsDbContext(new DbContextOptionsBuilder<MySqlInvoicingMigrationsDbContext>()
            .UseMySql(connectionString, serverVersion, options => options.MigrationsAssembly(typeof(MySqlMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceMigrations")).Options),
        NetArcaWsPersistenceModule.WsaaTickets => new MySqlWsaaTicketsMigrationsDbContext(new DbContextOptionsBuilder<MySqlWsaaTicketsMigrationsDbContext>()
            .UseMySql(connectionString, serverVersion, options => options.MigrationsAssembly(typeof(MySqlMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsTicketMigrations")).Options),
        NetArcaWsPersistenceModule.TenantCertificates => new MySqlTenantCertificatesMigrationsDbContext(new DbContextOptionsBuilder<MySqlTenantCertificatesMigrationsDbContext>()
            .UseMySql(connectionString, serverVersion, options => options.MigrationsAssembly(typeof(MySqlMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsCertificateMigrations")).Options),
        NetArcaWsPersistenceModule.InvoiceRecovery => new MySqlInvoiceRecoveryMigrationsDbContext(new DbContextOptionsBuilder<MySqlInvoiceRecoveryMigrationsDbContext>()
            .UseMySql(connectionString, serverVersion, options => options.MigrationsAssembly(typeof(MySqlMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceRecoveryMigrations")).Options),
        _ => throw new ArgumentOutOfRangeException(nameof(module))
    };

    public async Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        if (context is not (MySqlInvoicingMigrationsDbContext or MySqlWsaaTicketsMigrationsDbContext or MySqlTenantCertificatesMigrationsDbContext or MySqlInvoiceRecoveryMigrationsDbContext))
            throw new ArgumentException("A MySQL migrations context is required.", nameof(context));
        ArgumentNullException.ThrowIfNull(names);
        if (names.Count == 0) return Array.Empty<string>();
        if (names.Any(name => !OwnedTables.Contains(name)))
            throw new ArgumentException("Only NetArcaWs-owned table names can be inspected.", nameof(names));

        var connection = context.Database.GetDbConnection();
        bool openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var command = connection.CreateCommand();
            var parameterNames = new string[names.Count];
            for (int i = 0; i < names.Count; i++)
            {
                parameterNames[i] = $"@table{i}";
                var parameter = command.CreateParameter();
                parameter.ParameterName = parameterNames[i];
                parameter.Value = names[i];
                command.Parameters.Add(parameter);
            }
            command.CommandText = $"SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME IN ({string.Join(", ", parameterNames)})";
            var present = new List<string>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) present.Add(reader.GetString(0));
            return Array.AsReadOnly(present.Order(StringComparer.Ordinal).ToArray());
        }
        finally
        {
            if (openedHere) await context.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }
}
