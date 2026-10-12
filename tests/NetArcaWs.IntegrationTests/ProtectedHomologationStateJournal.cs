using Microsoft.Data.Sqlite;
using NetArcaWs.Invoicing;

namespace NetArcaWs.IntegrationTests;

/// <summary>SQLite invoice journal whose every durable mutation is backed up into the protected bundle.</summary>
public sealed class ProtectedSqliteInvoiceJournal : IInvoiceJournal, IInvoiceRejectionAdjudicationJournal
{
    private readonly ProtectedHomologationStateSession state;
    private readonly IInvoiceJournal inner;
    private readonly string databasePath;
    private readonly SemaphoreSlim mutationGate = new(1, 1);

    private ProtectedSqliteInvoiceJournal(
        ProtectedHomologationStateSession state,
        IInvoiceJournal inner,
        string databasePath)
    {
        this.state = state;
        this.inner = inner;
        this.databasePath = databasePath;
    }

    public string DatabasePath => databasePath;

    /// <summary>
    /// Restores into a private directory owned by this runner. The caller should use a unique path under RUNNER_TEMP.
    /// </summary>
    public static async Task<ProtectedSqliteInvoiceJournal> CreateAsync(
        ProtectedHomologationStateSession state,
        string temporaryDirectory,
        TimeProvider clock,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryDirectory);
        ArgumentNullException.ThrowIfNull(clock);

        string directory = Path.GetFullPath(temporaryDirectory);
        Directory.CreateDirectory(directory);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        string databasePath = Path.Combine(directory, "invoice-journal.sqlite");
        if (File.Exists(databasePath))
            throw new InvalidOperationException("The private homologation journal path already exists.");

        byte[]? snapshot = await state.GetJournalSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (snapshot is not null)
        {
            await File.WriteAllBytesAsync(databasePath, snapshot, cancellationToken).ConfigureAwait(false);
            SetOwnerOnlyFilePermissions(databasePath);
        }

        var inner = new SqliteInvoiceJournal(databasePath, clock);
        SetOwnerOnlyFilePermissions(databasePath);
        var journal = new ProtectedSqliteInvoiceJournal(state, inner, databasePath);
        if (snapshot is null)
            await journal.PersistSnapshotAsync(cancellationToken).ConfigureAwait(false);
        return journal;
    }

    public async Task<InvoiceOperation> PrepareAsync(InvoiceSubmission submission, CancellationToken cancellationToken = default)
        => await MutateAsync(() => inner.PrepareAsync(submission, cancellationToken), cancellationToken).ConfigureAwait(false);

    public async Task<InvoiceOperation> ReviseRejectedAsync(InvoiceSubmission replacement, long expectedVersion,
        CancellationToken cancellationToken = default)
        => await MutateAsync(() => inner.ReviseRejectedAsync(replacement, expectedVersion, cancellationToken), cancellationToken).ConfigureAwait(false);

    public Task<IReadOnlyList<InvoiceOperation>> ListRevisionsAsync(
        string tenantId, string idempotencyKey, CancellationToken cancellationToken = default)
        => inner.ListRevisionsAsync(tenantId, idempotencyKey, cancellationToken);

    public Task<InvoiceOperation> AdjudicateWsfexValidationRejectionAsync(
        string tenantId, string idempotencyKey, long expectedVersion, InvoiceRejectionEvidence evidence,
        CancellationToken cancellationToken = default)
        => MutateAsync(() => ((IInvoiceRejectionAdjudicationJournal)inner)
            .AdjudicateWsfexValidationRejectionAsync(tenantId, idempotencyKey, expectedVersion, evidence, cancellationToken), cancellationToken);

    public Task<IReadOnlyList<InvoiceRejectionAdjudication>> ListRejectionAdjudicationsAsync(
        string tenantId, string idempotencyKey, int limit = 100, CancellationToken cancellationToken = default)
        => ((IInvoiceRejectionAdjudicationJournal)inner)
            .ListRejectionAdjudicationsAsync(tenantId, idempotencyKey, limit, cancellationToken);

    public Task<InvoiceOperation?> FindAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default)
        => inner.FindAsync(tenantId, idempotencyKey, cancellationToken);

    public Task<IReadOnlyList<InvoiceOperation>> ListPendingAsync(
        string tenantId, int limit = 100, CancellationToken cancellationToken = default)
        => inner.ListPendingAsync(tenantId, limit, cancellationToken);

    /// <summary>Checks every nonterminal row, including operations hidden from ListPending by an active lease.</summary>
    public async Task<bool> HasUnresolvedOperationsAsync(string tenantId, string? exceptIdempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        await mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString();
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = exceptIdempotencyKey is null
                ? "SELECT EXISTS(SELECT 1 FROM invoices WHERE tenant=$tenant AND state NOT IN ($authorized,$rejected))"
                : "SELECT EXISTS(SELECT 1 FROM invoices WHERE tenant=$tenant AND state NOT IN ($authorized,$rejected) AND idem<>$except)";
            command.Parameters.AddWithValue("$tenant", tenantId);
            command.Parameters.AddWithValue("$authorized", (int)InvoiceState.Authorized);
            command.Parameters.AddWithValue("$rejected", (int)InvoiceState.Rejected);
            if (exceptIdempotencyKey is not null) command.Parameters.AddWithValue("$except", exceptIdempotencyKey);
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) != 0;
        }
        finally { mutationGate.Release(); }
    }

    public async Task<InvoiceLease?> TryAcquireAsync(
        string tenantId,
        string idempotencyKey,
        TimeSpan leaseDuration,
        bool reconciliation,
        CancellationToken cancellationToken = default)
    {
        await mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            InvoiceLease? lease = await inner.TryAcquireAsync(tenantId, idempotencyKey, leaseDuration, reconciliation, cancellationToken)
                .ConfigureAwait(false);
            if (lease is not null) await PersistSnapshotAsync(cancellationToken).ConfigureAwait(false);
            return lease;
        }
        finally { mutationGate.Release(); }
    }

    public async Task<InvoiceOperation> CompleteAsync(
        InvoiceLease lease,
        InvoiceDecision decision,
        CancellationToken cancellationToken = default)
        => await MutateAsync(() => inner.CompleteAsync(lease, decision, cancellationToken), cancellationToken).ConfigureAwait(false);

    private async Task<T> MutateAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            T result = await action().ConfigureAwait(false);
            await PersistSnapshotAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        finally { mutationGate.Release(); }
    }

    private async Task PersistSnapshotAsync(CancellationToken cancellationToken)
    {
        string backupPath = Path.Combine(Path.GetDirectoryName(databasePath)!, $"journal-backup-{Guid.NewGuid():N}.sqlite");
        try
        {
            var sourceConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false
            }.ToString();
            var destinationConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = backupPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString();

            await using (var source = new SqliteConnection(sourceConnectionString))
            await using (var destination = new SqliteConnection(destinationConnectionString))
            {
                await source.OpenAsync(cancellationToken).ConfigureAwait(false);
                await destination.OpenAsync(cancellationToken).ConfigureAwait(false);
                source.BackupDatabase(destination);
            }

            SetOwnerOnlyFilePermissions(backupPath);
            byte[] snapshot = await File.ReadAllBytesAsync(backupPath, cancellationToken).ConfigureAwait(false);
            await state.StoreJournalSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            TryDelete(backupPath);
            TryDelete(backupPath + "-journal");
            TryDelete(backupPath + "-wal");
            TryDelete(backupPath + "-shm");
        }
    }

    private static void SetOwnerOnlyFilePermissions(string path)
    {
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
