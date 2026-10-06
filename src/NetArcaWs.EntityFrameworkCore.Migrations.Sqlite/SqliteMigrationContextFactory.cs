using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NetArcaWs.EntityFrameworkCore.Migrations;

namespace NetArcaWs.EntityFrameworkCore.Migrations.Sqlite;

public sealed class SqliteMigrationContextFactory(string connectionString) : INetArcaWsMigrationContextFactory
{
    public NetArcaWsMigrationProvider Provider => NetArcaWsMigrationProvider.Sqlite;

    public DbContext CreateContext(NetArcaWsPersistenceModule module) => module switch
    {
        NetArcaWsPersistenceModule.Invoicing => new SqliteInvoicingMigrationsDbContext(new DbContextOptionsBuilder<SqliteInvoicingMigrationsDbContext>().UseSqlite(connectionString, x => x.MigrationsAssembly(typeof(SqliteMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceMigrations")).Options),
        NetArcaWsPersistenceModule.WsaaTickets => new SqliteWsaaTicketsMigrationsDbContext(new DbContextOptionsBuilder<SqliteWsaaTicketsMigrationsDbContext>().UseSqlite(connectionString, x => x.MigrationsAssembly(typeof(SqliteMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsTicketMigrations")).Options),
        _ => throw new ArgumentOutOfRangeException(nameof(module))
    };

    public async Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlite()) throw new ArgumentException("SQLite context required.", nameof(context));
        var connectionOptions = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(context.Database.GetDbConnection().ConnectionString);
        if (!connectionOptions.Mode.Equals(Microsoft.Data.Sqlite.SqliteOpenMode.Memory) && connectionOptions.DataSource != ":memory:" && !File.Exists(connectionOptions.DataSource))
            return Array.Empty<string>();
        var existing = new List<string>();
        foreach (string name in names)
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = $name COLLATE NOCASE";
            var parameter = command.CreateParameter(); parameter.ParameterName = "$name"; parameter.Value = name; command.Parameters.Add(parameter);
            if (context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            object? value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (value is string actual) existing.Add(actual);
        }
        return Array.AsReadOnly(existing.Order(StringComparer.Ordinal).ToArray());
    }
}
