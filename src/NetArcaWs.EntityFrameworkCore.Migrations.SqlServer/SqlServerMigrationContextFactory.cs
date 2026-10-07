using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NetArcaWs.EntityFrameworkCore.Migrations;

namespace NetArcaWs.EntityFrameworkCore.Migrations.SqlServer;

public sealed class SqlServerMigrationContextFactory(string connectionString) : INetArcaWsMigrationContextFactory
{
    private static readonly HashSet<string> OwnedTables = new(StringComparer.Ordinal)
    {
        "NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations", "NetArcaWsaaTickets",
        "NetArcaCertificateSlots", "NetArcaCertificateVersions", "__NetArcaWsInvoiceMigrations", "__NetArcaWsTicketMigrations", "__NetArcaWsCertificateMigrations"
    };

    public NetArcaWsMigrationProvider Provider => NetArcaWsMigrationProvider.SqlServer;

    public DbContext CreateContext(NetArcaWsPersistenceModule module) => module switch
    {
        NetArcaWsPersistenceModule.Invoicing => new SqlServerInvoicingMigrationsDbContext(new DbContextOptionsBuilder<SqlServerInvoicingMigrationsDbContext>()
            .UseSqlServer(connectionString, options => options.MigrationsAssembly(typeof(SqlServerMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceMigrations", "dbo")).Options),
        NetArcaWsPersistenceModule.WsaaTickets => new SqlServerWsaaTicketsMigrationsDbContext(new DbContextOptionsBuilder<SqlServerWsaaTicketsMigrationsDbContext>()
            .UseSqlServer(connectionString, options => options.MigrationsAssembly(typeof(SqlServerMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsTicketMigrations", "dbo")).Options),
        NetArcaWsPersistenceModule.TenantCertificates => new SqlServerTenantCertificatesMigrationsDbContext(new DbContextOptionsBuilder<SqlServerTenantCertificatesMigrationsDbContext>()
            .UseSqlServer(connectionString, options => options.MigrationsAssembly(typeof(SqlServerMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsCertificateMigrations", "dbo")).Options),
        _ => throw new ArgumentOutOfRangeException(nameof(module))
    };

    public async Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer()) throw new ArgumentException("A SQL Server migrations context is required.", nameof(context));
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
            command.CommandText = $"SELECT t.name FROM sys.tables AS t INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id WHERE s.name = 'dbo' AND t.name IN ({string.Join(", ", parameterNames)})";
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
