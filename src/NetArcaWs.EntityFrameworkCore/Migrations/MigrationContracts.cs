using Microsoft.EntityFrameworkCore;

namespace NetArcaWs.EntityFrameworkCore.Migrations;

public enum NetArcaWsPersistenceModule { Invoicing, WsaaTickets }
public enum NetArcaWsMigrationProvider { Sqlite, MySql, MariaDb, PostgreSql, SqlServer }
public enum NetArcaWsMigrationState
{
    Empty, UpgradeAvailable, Current, UntrackedSchema, UnknownAppliedMigration, InconsistentHistory, TrackedSchemaIncomplete
}

public sealed record NetArcaWsModuleMigrationStatus(
    NetArcaWsPersistenceModule Module, NetArcaWsMigrationState State,
    IReadOnlyList<string> Known, IReadOnlyList<string> Applied,
    IReadOnlyList<string> Pending, IReadOnlyList<string> PresentTables);

public sealed record NetArcaWsMigrationStatus(
    NetArcaWsMigrationProvider Provider,
    IReadOnlyList<NetArcaWsModuleMigrationStatus> Modules);

public interface INetArcaWsMigrator
{
    Task<NetArcaWsMigrationStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<NetArcaWsMigrationStatus> ApplyAsync(CancellationToken cancellationToken = default);
    string GenerateScript(NetArcaWsPersistenceModule module, string fromMigration = "0", string? toMigration = null, bool idempotent = false);
}

public interface INetArcaWsMigrationContextFactory
{
    NetArcaWsMigrationProvider Provider { get; }
    DbContext CreateContext(NetArcaWsPersistenceModule module);
    Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names, CancellationToken cancellationToken);
}
