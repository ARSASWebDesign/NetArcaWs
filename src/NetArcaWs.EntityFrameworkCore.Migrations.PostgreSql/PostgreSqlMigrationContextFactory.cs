using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NetArcaWs.EntityFrameworkCore.Migrations;

namespace NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql;

public sealed class PostgreSqlMigrationContextFactory(string connectionString) : INetArcaWsMigrationContextFactory
{
    private static readonly HashSet<string> OwnedTables = new(StringComparer.Ordinal)
    {
        "NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations", "NetArcaWsaaTickets",
        "NetArcaCertificateSlots", "NetArcaCertificateVersions", "__NetArcaWsInvoiceMigrations", "__NetArcaWsTicketMigrations", "__NetArcaWsCertificateMigrations"
    };

    public NetArcaWsMigrationProvider Provider => NetArcaWsMigrationProvider.PostgreSql;

    public DbContext CreateContext(NetArcaWsPersistenceModule module) => module switch
    {
        NetArcaWsPersistenceModule.Invoicing => new PostgreSqlInvoicingMigrationsDbContext(new DbContextOptionsBuilder<PostgreSqlInvoicingMigrationsDbContext>()
            .UseNpgsql(connectionString, options => options.MigrationsAssembly(typeof(PostgreSqlMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceMigrations", "public")).Options),
        NetArcaWsPersistenceModule.WsaaTickets => new PostgreSqlWsaaTicketsMigrationsDbContext(new DbContextOptionsBuilder<PostgreSqlWsaaTicketsMigrationsDbContext>()
            .UseNpgsql(connectionString, options => options.MigrationsAssembly(typeof(PostgreSqlMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsTicketMigrations", "public")).Options),
        NetArcaWsPersistenceModule.TenantCertificates => new PostgreSqlTenantCertificatesMigrationsDbContext(new DbContextOptionsBuilder<PostgreSqlTenantCertificatesMigrationsDbContext>()
            .UseNpgsql(connectionString, options => options.MigrationsAssembly(typeof(PostgreSqlMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsCertificateMigrations", "public")).Options),
        _ => throw new ArgumentOutOfRangeException(nameof(module))
    };

    public async Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        if (!context.Database.IsNpgsql()) throw new ArgumentException("A PostgreSQL migrations context is required.", nameof(context));
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
            command.CommandText = $"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN ({string.Join(", ", parameterNames)})";
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
