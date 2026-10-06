using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using NetArcaWs.HealthChecks;

namespace NetArcaWs.Invoicing;

/// <summary>
/// Durable journal for processes sharing one local SQLite database. Do not use on network filesystems.
/// For replicas on different hosts implement IInvoiceJournal with a transactional shared database.
/// </summary>
public sealed class SqliteInvoiceJournal : IInvoiceJournal
{
    private readonly string connectionString;
    private readonly TimeProvider clock;

    public SqliteInvoiceJournal(string databasePath, TimeProvider? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        if (databasePath == ":memory:") throw new ArgumentException("A durable database file is required.", nameof(databasePath));
        var path = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false, DefaultTimeout = 30 }.ToString();
        this.clock = clock ?? TimeProvider.System;
        using var connection = Open();
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS invoices (
                tenant TEXT NOT NULL, idem TEXT NOT NULL, service TEXT NOT NULL,
                environment INTEGER NOT NULL, cuit INTEGER NOT NULL, pos INTEGER NOT NULL,
                type INTEGER NOT NULL, number INTEGER NOT NULL, payload TEXT NOT NULL, hash TEXT NOT NULL,
                state INTEGER NOT NULL, version INTEGER NOT NULL, attempt INTEGER NOT NULL,
                lease_until INTEGER, authorization TEXT, response TEXT, canonical_version INTEGER NOT NULL,
                remote_request_id INTEGER,
                PRIMARY KEY (tenant, idem), UNIQUE (environment, cuit, pos, type, number)
            );
            CREATE TABLE IF NOT EXISTS invoice_revisions (
                tenant TEXT NOT NULL, idem TEXT NOT NULL, revision_no INTEGER NOT NULL,
                service TEXT NOT NULL, environment INTEGER NOT NULL, cuit INTEGER NOT NULL, pos INTEGER NOT NULL,
                type INTEGER NOT NULL, number INTEGER NOT NULL, payload TEXT NOT NULL, hash TEXT NOT NULL,
                state INTEGER NOT NULL, version INTEGER NOT NULL, attempt INTEGER NOT NULL,
                lease_until INTEGER, authorization TEXT, response TEXT, canonical_version INTEGER NOT NULL,
                remote_request_id INTEGER,
                PRIMARY KEY (tenant, idem, revision_no)
            );
            CREATE TRIGGER IF NOT EXISTS invoice_revisions_no_update
                BEFORE UPDATE ON invoice_revisions BEGIN SELECT RAISE(ABORT, 'invoice revisions are immutable'); END;
            CREATE TRIGGER IF NOT EXISTS invoice_revisions_no_delete
                BEFORE DELETE ON invoice_revisions BEGIN SELECT RAISE(ABORT, 'invoice revisions are immutable'); END;
            """;
        command.ExecuteNonQuery();
        EnsureColumn(connection, "invoices", "remote_request_id", "INTEGER");
        EnsureColumn(connection, "invoice_revisions", "remote_request_id", "INTEGER");
        using var index = connection.CreateCommand();
        index.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS ux_invoices_remote_request_id ON invoices(environment,cuit,service,remote_request_id) WHERE remote_request_id IS NOT NULL";
        index.ExecuteNonQuery();
    }

    public Task<InvoiceOperation> PrepareAsync(InvoiceSubmission submission, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validate(submission);
        using var connection = Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        var current = Read(connection, transaction, submission.TenantId, submission.IdempotencyKey);
        if (current is not null)
        {
            if (current.Value.Operation.Submission != submission)
                throw new InvoiceConflictException("The idempotency key is already bound to a different immutable submission.");
            transaction.Commit();
            return Task.FromResult(current.Value.Operation);
        }
        using var guard = connection.CreateCommand();
        guard.Transaction = transaction;
        guard.CommandText = "SELECT COUNT(*) FROM invoices WHERE environment=$environment AND cuit=$cuit AND pos=$pos AND type=$type AND state NOT IN (4,5)";
        Add(guard, "$environment", (int)submission.Identity.Environment); Add(guard, "$cuit", submission.Identity.Cuit);
        Add(guard, "$pos", submission.Identity.PointOfSale); Add(guard, "$type", submission.Identity.VoucherType);
        if (Convert.ToInt64(guard.ExecuteScalar(), CultureInfo.InvariantCulture) != 0)
            throw new InvoiceConflictException("An unresolved operation already reserves this fiscal series. Reconcile it before preparing another invoice.");
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO invoices (tenant,idem,service,environment,cuit,pos,type,number,payload,hash,state,version,attempt,canonical_version,remote_request_id)
            VALUES ($tenant,$idem,$service,$environment,$cuit,$pos,$type,$number,$payload,$hash,0,0,0,$canonical,$remote_request_id);
            """;
        Add(command, "$tenant", submission.TenantId); Add(command, "$idem", submission.IdempotencyKey);
        Add(command, "$service", submission.Service); Add(command, "$environment", (int)submission.Identity.Environment);
        Add(command, "$cuit", submission.Identity.Cuit); Add(command, "$pos", submission.Identity.PointOfSale);
        Add(command, "$type", submission.Identity.VoucherType); Add(command, "$number", submission.Identity.VoucherNumber);
        Add(command, "$payload", submission.Payload); Add(command, "$canonical", submission.CanonicalVersion);
        Add(command, "$remote_request_id", submission.RemoteRequestId);
        Add(command, "$hash", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(submission.Payload))));
        try { command.ExecuteNonQuery(); }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvoiceConflictException("The fiscal identity or remote request ID is already reserved. Changing tenant or protocol cannot create another invoice.");
        }
        transaction.Commit();
        return Task.FromResult(new InvoiceOperation(submission, InvoiceState.Prepared, 0, 0));
    }

    public Task<InvoiceOperation> ReviseRejectedAsync(InvoiceSubmission replacement, long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Validate(replacement);
        if (expectedVersion < 0) throw new ArgumentOutOfRangeException(nameof(expectedVersion));

        using var connection = Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        var current = Read(connection, transaction, replacement.TenantId, replacement.IdempotencyKey)
            ?? throw new KeyNotFoundException("The invoice operation does not exist.");
        var previous = current.Operation;
        var original = previous.Submission;
        if (previous.State != InvoiceState.Rejected || previous.Version != expectedVersion)
            throw new InvoiceConflictException("Only the current rejected version can be revised.");
        if (!string.Equals(original.TenantId, replacement.TenantId, StringComparison.Ordinal) ||
            !string.Equals(original.IdempotencyKey, replacement.IdempotencyKey, StringComparison.Ordinal) ||
            !string.Equals(original.Service, replacement.Service, StringComparison.Ordinal) ||
            original.Identity != replacement.Identity)
            throw new InvoiceConflictException("A rejection revision must preserve tenant, idempotency key, service and fiscal identity.");
        if (original.RemoteRequestId != replacement.RemoteRequestId)
            throw new InvoiceConflictException("A rejection revision must retain its service-level request ID to prevent reuse.");

        using (var guard = connection.CreateCommand())
        {
            guard.Transaction = transaction;
            guard.CommandText = "SELECT COUNT(*) FROM invoices WHERE environment=$environment AND cuit=$cuit AND pos=$pos AND type=$type AND state NOT IN (4,5) AND NOT (tenant=$tenant AND idem=$idem)";
            Add(guard, "$environment", (int)replacement.Identity.Environment); Add(guard, "$cuit", replacement.Identity.Cuit);
            Add(guard, "$pos", replacement.Identity.PointOfSale); Add(guard, "$type", replacement.Identity.VoucherType);
            Add(guard, "$tenant", replacement.TenantId); Add(guard, "$idem", replacement.IdempotencyKey);
            if (Convert.ToInt64(guard.ExecuteScalar(), CultureInfo.InvariantCulture) != 0)
                throw new InvoiceConflictException("Another unresolved operation reserves this fiscal series; reconcile it before revising this rejection.");
        }

        using (var revision = connection.CreateCommand())
        {
            revision.Transaction = transaction;
            revision.CommandText = """
                INSERT INTO invoice_revisions (tenant,idem,revision_no,service,environment,cuit,pos,type,number,payload,hash,state,version,attempt,lease_until,authorization,response,canonical_version,remote_request_id)
                SELECT tenant,idem,(SELECT COALESCE(MAX(revision_no),0)+1 FROM invoice_revisions WHERE tenant=$tenant AND idem=$idem),
                    service,environment,cuit,pos,type,number,payload,hash,state,version,attempt,lease_until,authorization,response,canonical_version,remote_request_id
                FROM invoices WHERE tenant=$tenant AND idem=$idem AND state=$rejected AND version=$version;
                """;
            Add(revision, "$tenant", replacement.TenantId); Add(revision, "$idem", replacement.IdempotencyKey);
            Add(revision, "$rejected", (int)InvoiceState.Rejected); Add(revision, "$version", expectedVersion);
            if (revision.ExecuteNonQuery() != 1)
                throw new InvoiceConflictException("The rejected invoice changed while its revision was being saved.");
        }

        using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText = "UPDATE invoices SET payload=$payload,hash=$hash,canonical_version=$canonical,state=$prepared,version=version+1,lease_until=NULL,authorization=NULL,response=NULL WHERE tenant=$tenant AND idem=$idem AND state=$rejected AND version=$version";
            Add(update, "$payload", replacement.Payload);
            Add(update, "$hash", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(replacement.Payload))));
            Add(update, "$canonical", replacement.CanonicalVersion); Add(update, "$prepared", (int)InvoiceState.Prepared);
            Add(update, "$tenant", replacement.TenantId); Add(update, "$idem", replacement.IdempotencyKey);
            Add(update, "$rejected", (int)InvoiceState.Rejected); Add(update, "$version", expectedVersion);
            if (update.ExecuteNonQuery() != 1)
                throw new InvoiceConflictException("The rejected invoice changed while its revision was being saved.");
        }

        transaction.Commit();
        return Task.FromResult(new InvoiceOperation(replacement, InvoiceState.Prepared, checked(previous.Version + 1), previous.Attempt));
    }

    public Task<IReadOnlyList<InvoiceOperation>> ListRevisionsAsync(string tenantId, string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT service,environment,cuit,pos,type,number,payload,state,version,attempt,authorization,response,lease_until,canonical_version,hash,remote_request_id FROM invoice_revisions WHERE tenant=$tenant AND idem=$idem ORDER BY revision_no";
        Add(command, "$tenant", tenantId); Add(command, "$idem", idempotencyKey);
        var revisions = new List<InvoiceOperation>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) revisions.Add(OperationFromReader(reader, tenantId, idempotencyKey));
        return Task.FromResult<IReadOnlyList<InvoiceOperation>>(revisions);
    }

    public Task<InvoiceOperation?> FindAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = Open();
        return Task.FromResult(Read(connection, null, tenantId, idempotencyKey)?.Operation);
    }

    public Task<IReadOnlyList<InvoiceOperation>> ListPendingAsync(string tenantId, int limit = 100, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (limit is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(limit));
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT idem FROM invoices WHERE tenant=$tenant AND (state IN (0,2,6,7) OR (state IN (1,3) AND lease_until <= $now)) ORDER BY rowid LIMIT $limit";
        Add(command, "$tenant", tenantId); Add(command, "$now", clock.GetUtcNow().ToUnixTimeMilliseconds()); Add(command, "$limit", limit);
        var keys = new List<string>();
        using (var reader = command.ExecuteReader()) while (reader.Read()) keys.Add(reader.GetString(0));
        var result = new List<InvoiceOperation>();
        foreach (var key in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.Add(Read(connection, transaction, tenantId, key)!.Value.Operation);
        }
        transaction.Commit();
        return Task.FromResult<IReadOnlyList<InvoiceOperation>>(result);
    }

    public Task<InvoiceLease?> TryAcquireAsync(string tenantId, string idempotencyKey, TimeSpan leaseDuration, bool reconciliation,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (leaseDuration <= TimeSpan.Zero || leaseDuration > TimeSpan.FromMinutes(30)) throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        using var connection = Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        var stored = Read(connection, transaction, tenantId, idempotencyKey)
            ?? throw new KeyNotFoundException("The invoice operation does not exist.");
        var current = stored.Operation;
        var now = clock.GetUtcNow().ToUnixTimeMilliseconds();
        var active = stored.LeaseUntil is { } until && until > now;
        var allowed = reconciliation
            ? current.State is InvoiceState.Unknown or InvoiceState.ManualReview or InvoiceState.Conflict || (current.State is InvoiceState.Submitting or InvoiceState.Reconciling && !active)
            : current.State == InvoiceState.Prepared;
        if (active || !allowed) return Task.FromResult<InvoiceLease?>(null);
        var state = reconciliation ? InvoiceState.Reconciling : InvoiceState.Submitting;
        var version = checked(current.Version + 1);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE invoices SET state=$state,version=$version,attempt=attempt+1,lease_until=$lease WHERE tenant=$tenant AND idem=$idem";
        Add(command, "$state", (int)state); Add(command, "$version", version);
        Add(command, "$lease", clock.GetUtcNow().Add(leaseDuration).ToUnixTimeMilliseconds());
        Add(command, "$tenant", tenantId); Add(command, "$idem", idempotencyKey);
        command.ExecuteNonQuery(); transaction.Commit();
        var claimed = current with { State = state, Version = version, Attempt = current.Attempt + 1 };
        return Task.FromResult<InvoiceLease?>(new(claimed, version));
    }

    public Task<InvoiceOperation> CompleteAsync(InvoiceLease lease, InvoiceDecision decision, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(lease); ArgumentNullException.ThrowIfNull(decision);
        if (decision.State is not (InvoiceState.Authorized or InvoiceState.Rejected or InvoiceState.Unknown or InvoiceState.Conflict or InvoiceState.ManualReview))
            throw new ArgumentException("Only a definitive decision or unknown/manual-review status can complete a lease.", nameof(decision));
        if (decision.State == InvoiceState.Authorized && (string.IsNullOrEmpty(decision.AuthorizationCode) ||
            decision.AuthorizationCode.Any(c => c is < '0' or > '9')))
            throw new ArgumentException("An authorization requires a numeric authorization code.", nameof(decision));
        if (decision.ResponseXml?.Length > 4 * 1024 * 1024) throw new ArgumentException("Response exceeds journal limit.", nameof(decision));
        using var connection = Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        var tenant = lease.Operation.Submission.TenantId;
        var id = lease.Operation.Submission.IdempotencyKey;
        var stored = Read(connection, transaction, tenant, id) ?? throw new KeyNotFoundException("Invoice operation not found.");
        if (stored.Operation.Version != lease.Version || stored.Operation.State is not (InvoiceState.Submitting or InvoiceState.Reconciling) ||
            stored.LeaseUntil is not { } expiry || expiry <= clock.GetUtcNow().ToUnixTimeMilliseconds())
            throw new InvalidOperationException("The invoice lease has expired or has been superseded.");
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "UPDATE invoices SET state=$state,version=version+1,lease_until=NULL,authorization=$authorization,response=$response WHERE tenant=$tenant AND idem=$idem";
        Add(command, "$state", (int)decision.State); Add(command, "$authorization", decision.AuthorizationCode);
        Add(command, "$response", decision.ResponseXml); Add(command, "$tenant", tenant); Add(command, "$idem", id);
        command.ExecuteNonQuery(); transaction.Commit();
        return Task.FromResult(stored.Operation with { State = decision.State, Version = stored.Operation.Version + 1,
            AuthorizationCode = decision.AuthorizationCode, ResponseXml = decision.ResponseXml });
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA synchronous=FULL;";
            command.ExecuteNonQuery();
            return connection;
        }
        catch { connection.Dispose(); throw; }
    }

    private static (InvoiceOperation Operation, long? LeaseUntil)? Read(SqliteConnection connection, SqliteTransaction? transaction, string tenant, string key)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT service,environment,cuit,pos,type,number,payload,state,version,attempt,authorization,response,lease_until,canonical_version,hash,remote_request_id FROM invoices WHERE tenant=$tenant AND idem=$idem";
        Add(command, "$tenant", tenant); Add(command, "$idem", key);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return (OperationFromReader(reader, tenant, key), reader.IsDBNull(12) ? null : reader.GetInt64(12));
    }

    private static InvoiceOperation OperationFromReader(SqliteDataReader reader, string tenant, string key)
    {
        var identity = new InvoiceIdentity((ArcaEnvironment)reader.GetInt32(1), reader.GetInt64(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt64(5));
        long? remoteRequestId = reader.IsDBNull(15) ? null : reader.GetInt64(15);
        var submission = new InvoiceSubmission(tenant, key, reader.GetString(0), identity, reader.GetString(6), reader.GetInt32(13), remoteRequestId);
        if (Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(submission.Payload))) != reader.GetString(14))
            throw new InvalidDataException("The immutable invoice payload failed its integrity check.");
        var operation = new InvoiceOperation(submission, (InvoiceState)reader.GetInt32(7), reader.GetInt64(8), reader.GetInt32(9),
            reader.IsDBNull(10) ? null : reader.GetString(10), reader.IsDBNull(11) ? null : reader.GetString(11));
        return operation;
    }

    private static void Add(SqliteCommand command, string name, object? value) => command.Parameters.AddWithValue(name, value ?? DBNull.Value);

    private static void Validate(InvoiceSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission); ArgumentNullException.ThrowIfNull(submission.Identity);
        foreach (var value in new[] { submission.TenantId, submission.IdempotencyKey, submission.Service })
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Any(char.IsControl))
                throw new ArgumentException("Tenant, idempotency key and service must be nonempty and at most 128 characters.", nameof(submission));
        if (!Enum.IsDefined(submission.Identity.Environment) || submission.Identity.Cuit is < 10_000_000_000 or > 99_999_999_999 ||
            submission.Identity.PointOfSale is < 1 or > 99999 || submission.Identity.VoucherType <= 0 || submission.Identity.VoucherNumber <= 0)
            throw new ArgumentException("Invalid fiscal identity.", nameof(submission));
        if (submission.CanonicalVersion <= 0) throw new ArgumentOutOfRangeException(nameof(submission.CanonicalVersion));
        if (submission.RemoteRequestId is <= 0) throw new ArgumentOutOfRangeException(nameof(submission.RemoteRequestId), "A service-level request ID must be positive.");
        if (string.IsNullOrWhiteSpace(submission.Payload) || submission.Payload.Length > 2 * 1024 * 1024)
            throw new ArgumentException("A nonempty payload of at most 2 MiB is required.", nameof(submission));
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string type)
    {
        using var info = connection.CreateCommand();
        info.CommandText = $"PRAGMA table_info({table})";
        using var reader = info.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase)) return;
        }
        reader.Close();
        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {type}";
        alter.ExecuteNonQuery();
    }
}
