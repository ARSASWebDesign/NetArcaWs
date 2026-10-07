using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NetArcaWs.EntityFrameworkCore.Migrations;

public sealed class NetArcaWsMigrator : INetArcaWsMigrator
{
    private readonly INetArcaWsMigrationContextFactory contextFactory;
    private readonly IReadOnlyList<NetArcaWsPersistenceModule> modules;

    private static readonly IReadOnlyDictionary<NetArcaWsPersistenceModule, string[]> OwnedTables = new Dictionary<NetArcaWsPersistenceModule, string[]>
    {
        [NetArcaWsPersistenceModule.Invoicing] = ["NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations"],
        [NetArcaWsPersistenceModule.WsaaTickets] = ["NetArcaWsaaTickets"],
        [NetArcaWsPersistenceModule.TenantCertificates] = ["NetArcaCertificateSlots", "NetArcaCertificateVersions"]
    };
    private static readonly IReadOnlyDictionary<NetArcaWsPersistenceModule, string> HistoryTables = new Dictionary<NetArcaWsPersistenceModule, string>
    {
        [NetArcaWsPersistenceModule.Invoicing] = "__NetArcaWsInvoiceMigrations",
        [NetArcaWsPersistenceModule.WsaaTickets] = "__NetArcaWsTicketMigrations",
        [NetArcaWsPersistenceModule.TenantCertificates] = "__NetArcaWsCertificateMigrations"
    };

    public NetArcaWsMigrator(INetArcaWsMigrationContextFactory contextFactory, IReadOnlyList<NetArcaWsPersistenceModule> modules)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(modules);
        var selection = new HashSet<NetArcaWsPersistenceModule>();
        foreach (NetArcaWsPersistenceModule module in modules)
        {
            if (!Enum.IsDefined(module))
                throw new ArgumentOutOfRangeException(nameof(modules), module, "The migration module is undefined.");
            if (!selection.Add(module))
                throw new ArgumentException($"The migration module '{module}' was selected more than once.", nameof(modules));
        }

        this.contextFactory = contextFactory;
        this.modules = Array.AsReadOnly(selection.Order().ToArray());
    }

    public async Task<NetArcaWsMigrationStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new List<NetArcaWsModuleMigrationStatus>(modules.Count);
        foreach (NetArcaWsPersistenceModule module in modules)
            statuses.Add(await ReadStatusAsync(module, cancellationToken).ConfigureAwait(false));
        return new(contextFactory.Provider, Array.AsReadOnly(statuses.ToArray()));
    }

    public async Task<NetArcaWsMigrationStatus> ApplyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NetArcaWsMigrationStatus before = await GetStatusAsync(cancellationToken).ConfigureAwait(false);
        ThrowIfInvalid(before);
        foreach (NetArcaWsPersistenceModule module in modules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            NetArcaWsModuleMigrationStatus again = await ReadStatusAsync(module, cancellationToken).ConfigureAwait(false);
            NetArcaWsMigrationStatus status = new(contextFactory.Provider, [again]);
            ThrowIfInvalid(status);
            if (again.Pending.Count == 0) continue;
            await using DbContext context = contextFactory.CreateContext(module);
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
        return await GetStatusAsync(cancellationToken).ConfigureAwait(false);
    }

    public string GenerateScript(NetArcaWsPersistenceModule module, string fromMigration = "0", string? toMigration = null, bool idempotent = false)
    {
        EnsureSelected(module);
        if (idempotent && contextFactory.Provider == NetArcaWsMigrationProvider.Sqlite)
            throw new NotSupportedException("SQLite does not support idempotent migration scripts.");
        using DbContext context = contextFactory.CreateContext(module);
        IReadOnlyList<string> known = Array.AsReadOnly(context.Database.GetMigrations().Order(StringComparer.Ordinal).ToArray());
        ValidateRange(known, fromMigration, toMigration);
        return context.GetService<IMigrator>().GenerateScript(fromMigration, toMigration, idempotent ? MigrationsSqlGenerationOptions.Idempotent : MigrationsSqlGenerationOptions.Default);
    }

    private async Task<NetArcaWsModuleMigrationStatus> ReadStatusAsync(NetArcaWsPersistenceModule module, CancellationToken cancellationToken)
    {
        EnsureSelected(module);
        await using DbContext context = contextFactory.CreateContext(module);
        IReadOnlyList<string> known = Array.AsReadOnly(context.Database.GetMigrations().Order(StringComparer.Ordinal).ToArray());
        IReadOnlyList<string> found = await contextFactory.GetPresentTablesAsync(context, [.. OwnedTables[module], HistoryTables[module]], cancellationToken).ConfigureAwait(false);
        IReadOnlyList<string> present = Array.AsReadOnly(found.Where(name => OwnedTables[module].Contains(name, StringComparer.OrdinalIgnoreCase)).Order(StringComparer.Ordinal).ToArray());
        IReadOnlyList<string> applied = found.Any(name => string.Equals(name, HistoryTables[module], StringComparison.OrdinalIgnoreCase))
            ? Array.AsReadOnly((await context.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false)).Order(StringComparer.Ordinal).ToArray())
            : Array.Empty<string>();
        string[] pending = known.Except(applied, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        NetArcaWsMigrationState state;
        if (applied.Any(id => !known.Contains(id, StringComparer.Ordinal))) state = NetArcaWsMigrationState.UnknownAppliedMigration;
        else if (!applied.SequenceEqual(known.Take(applied.Count), StringComparer.Ordinal)) state = NetArcaWsMigrationState.InconsistentHistory;
        else if (applied.Count == 0 && present.Count > 0) state = NetArcaWsMigrationState.UntrackedSchema;
        else if (applied.Count > 0 && OwnedTables[module].Except(present, StringComparer.OrdinalIgnoreCase).Any()) state = NetArcaWsMigrationState.TrackedSchemaIncomplete;
        else if (applied.Count == 0 && present.Count == 0) state = NetArcaWsMigrationState.Empty;
        else if (pending.Length > 0) state = NetArcaWsMigrationState.UpgradeAvailable;
        else state = NetArcaWsMigrationState.Current;
        return new(module, state, known, applied, Array.AsReadOnly(pending), present);
    }

    private void EnsureSelected(NetArcaWsPersistenceModule module)
    {
        if (!Enum.IsDefined(module) || !modules.Contains(module)) throw new ArgumentException("The migration module is not selected.", nameof(module));
    }

    private static void ThrowIfInvalid(NetArcaWsMigrationStatus status)
    {
        if (status.Modules.Any(x => x.State is NetArcaWsMigrationState.UntrackedSchema or NetArcaWsMigrationState.UnknownAppliedMigration or NetArcaWsMigrationState.InconsistentHistory or NetArcaWsMigrationState.TrackedSchemaIncomplete))
            throw new NetArcaWsMigrationPreflightException(status);
    }

    private static void ValidateRange(IReadOnlyList<string> known, string from, string? to)
    {
        if (from != "0" && !known.Contains(from, StringComparer.Ordinal)) throw new ArgumentException("Unknown from migration.", nameof(from));
        if (to is not null && !known.Contains(to, StringComparer.Ordinal)) throw new ArgumentException("Unknown to migration.", nameof(to));
        if (to is not null && from != "0" && string.CompareOrdinal(from, to) >= 0) throw new ArgumentException("Migration range must be ascending.");
    }
}
