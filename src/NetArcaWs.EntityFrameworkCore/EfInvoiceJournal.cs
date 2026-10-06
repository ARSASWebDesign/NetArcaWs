using System.Buffers.Binary;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;

namespace NetArcaWs.EntityFrameworkCore;

/// <summary>Relational journal that creates an independent context for every operation.</summary>
public sealed class EfInvoiceJournal<TContext> : IInvoiceJournal where TContext : DbContext
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly IDbContextFactory<TContext> contextFactory;
    private readonly NetArcaWsModelOptions modelOptions;
    private readonly TimeProvider clock;

    public EfInvoiceJournal(IDbContextFactory<TContext> contextFactory, NetArcaWsModelOptions modelOptions, TimeProvider? timeProvider = null)
    {
        this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        this.modelOptions = modelOptions ?? throw new ArgumentNullException(nameof(modelOptions));
        if (!modelOptions.InvoicingEnabled) throw new ArgumentException("Select at least one invoicing service before registering the EF invoice journal.", nameof(modelOptions));
        clock = timeProvider ?? TimeProvider.System;
    }

    public async Task<InvoiceOperation> PrepareAsync(InvoiceSubmission submission, CancellationToken cancellationToken = default)
    {
        Validate(submission);
        EnsureServiceEnabled(submission.Service);
        try
        {
            return await ExecuteWithContentionRetryAsync(
                token => PrepareAttemptAsync(submission, token), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsConstraintFailure(exception))
        {
            // The failed attempt has unwound and disposed its transaction/context before this fresh read.
            InvoiceOperation? winner = await ExecuteWithContentionRetryAsync(
                token => FindAsync(submission.TenantId, submission.IdempotencyKey, token), cancellationToken).ConfigureAwait(false);
            if (winner is not null)
            {
                if (winner.Submission == submission) return winner;
                throw new InvoiceConflictException("The idempotency key is already bound to a different immutable submission.");
            }
            throw new InvoiceConflictException("The fiscal identity, remote request ID, or unresolved fiscal series is already reserved.");
        }
    }

    private async Task<InvoiceOperation> PrepareAttemptAsync(InvoiceSubmission submission, CancellationToken cancellationToken)
    {
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        (string tenantHash, string keyHash) = GetOperationKeys(submission.TenantId, submission.IdempotencyKey);
        InvoiceJournalEntity? existing = await context.Set<InvoiceJournalEntity>()
            .SingleOrDefaultAsync(x => x.TenantHash == tenantHash && x.KeyHash == keyHash, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            EnsureIdentifierMatch(existing, submission.TenantId, submission.IdempotencyKey);
            InvoiceOperation restored = ToOperation(existing);
            if (restored.Submission != submission)
                throw new InvoiceConflictException("The idempotency key is already bound to a different immutable submission.");
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return restored;
        }

        var reservation = new InvoiceSeriesReservationEntity
        {
            SeriesHash = GetSeriesHash(submission.Identity), TenantHash = tenantHash, KeyHash = keyHash
        };
        var operation = CreateEntity(submission, clock.GetUtcNow());
        context.Add(reservation);
        context.Add(operation);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return ToOperation(operation);
    }

    public async Task<InvoiceOperation> ReviseRejectedAsync(InvoiceSubmission replacement, long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        Validate(replacement);
        EnsureServiceEnabled(replacement.Service);
        if (expectedVersion < 0) throw new ArgumentOutOfRangeException(nameof(expectedVersion));
        try
        {
            return await ExecuteWithContentionRetryAsync(
                token => ReviseRejectedAttemptAsync(replacement, expectedVersion, token), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsConstraintFailure(exception))
        {
            throw new InvoiceConflictException("The revised fiscal identity, remote request ID, or series is already reserved.");
        }
    }

    private async Task<InvoiceOperation> ReviseRejectedAttemptAsync(InvoiceSubmission replacement, long expectedVersion,
        CancellationToken cancellationToken)
    {
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        (string tenantHash, string keyHash) = GetOperationKeys(replacement.TenantId, replacement.IdempotencyKey);
        InvoiceJournalEntity current = await context.Set<InvoiceJournalEntity>()
            .SingleOrDefaultAsync(x => x.TenantHash == tenantHash && x.KeyHash == keyHash, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The invoice operation does not exist.");
        EnsureIdentifierMatch(current, replacement.TenantId, replacement.IdempotencyKey);
        InvoiceOperation previous = ToOperation(current);
        InvoiceSubmission original = previous.Submission;
        if (previous.State != InvoiceState.Rejected || previous.Version != expectedVersion)
            throw new InvoiceConflictException("Only the current rejected version can be revised.");
        if (!string.Equals(original.TenantId, replacement.TenantId, StringComparison.Ordinal) ||
            !string.Equals(original.IdempotencyKey, replacement.IdempotencyKey, StringComparison.Ordinal) ||
            !string.Equals(original.Service, replacement.Service, StringComparison.Ordinal) || original.Identity != replacement.Identity)
            throw new InvoiceConflictException("A rejection revision must preserve tenant, idempotency key, service and fiscal identity.");
        if (original.RemoteRequestId != replacement.RemoteRequestId)
            throw new InvoiceConflictException("A rejection revision must retain its service-level request ID to prevent reuse.");

        bool alreadyReserved = await context.Set<InvoiceSeriesReservationEntity>()
            .AnyAsync(x => x.SeriesHash == GetSeriesHash(replacement.Identity), cancellationToken).ConfigureAwait(false);
        if (alreadyReserved) throw new InvoiceConflictException("Another unresolved operation reserves this fiscal series.");

        int revisionNumber = await context.Set<InvoiceRevisionEntity>()
            .Where(x => x.TenantHash == tenantHash && x.KeyHash == keyHash)
            .Select(x => (int?)x.RevisionNumber).MaxAsync(cancellationToken).ConfigureAwait(false) + 1 ?? 1;
        context.Add(ToRevision(current, revisionNumber));
        context.Add(new InvoiceSeriesReservationEntity
        {
            SeriesHash = GetSeriesHash(replacement.Identity), TenantHash = tenantHash, KeyHash = keyHash
        });

        long now = clock.GetUtcNow().ToUnixTimeMilliseconds();
        int changed = await context.Set<InvoiceJournalEntity>()
            .Where(x => x.TenantHash == tenantHash && x.KeyHash == keyHash && x.Version == expectedVersion && x.State == (int)InvoiceState.Rejected)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.Payload, replacement.Payload)
                .SetProperty(x => x.PayloadHash, Hash(replacement.Payload))
                .SetProperty(x => x.CanonicalVersion, replacement.CanonicalVersion)
                .SetProperty(x => x.State, (int)InvoiceState.Prepared)
                .SetProperty(x => x.Version, x => x.Version + 1)
                .SetProperty(x => x.LeaseUntilMilliseconds, (long?)null)
                .SetProperty(x => x.AuthorizationCode, (string?)null)
                .SetProperty(x => x.ResponseXml, (string?)null), cancellationToken).ConfigureAwait(false);
        if (changed != 1) throw new InvoiceConflictException("The rejected invoice changed while its revision was being saved.");
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new InvoiceOperation(replacement, InvoiceState.Prepared, checked(previous.Version + 1), previous.Attempt);
    }

    public async Task<IReadOnlyList<InvoiceOperation>> ListRevisionsAsync(string tenantId, string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(tenantId, nameof(tenantId)); ValidateIdentifier(idempotencyKey, nameof(idempotencyKey));
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        (string tenantHash, string keyHash) = GetOperationKeys(tenantId, idempotencyKey);
        List<InvoiceRevisionEntity> revisions = await context.Set<InvoiceRevisionEntity>()
            .Where(x => x.TenantHash == tenantHash && x.KeyHash == keyHash)
            .OrderBy(x => x.RevisionNumber).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (InvoiceRevisionEntity revision in revisions)
        {
            EnsureIdentifierMatch(revision.TenantId, revision.IdempotencyKey, tenantId, idempotencyKey);
            if (HashIdentifier(revision.TenantId) != revision.TenantHash || HashIdentifier(revision.IdempotencyKey) != revision.KeyHash)
                throw new InvalidDataException("The immutable invoice revision identifiers failed their integrity check.");
        }
        return revisions.Select(ToOperation).ToArray();
    }

    public async Task<InvoiceOperation?> FindAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(tenantId, nameof(tenantId)); ValidateIdentifier(idempotencyKey, nameof(idempotencyKey));
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        (string tenantHash, string keyHash) = GetOperationKeys(tenantId, idempotencyKey);
        InvoiceJournalEntity? entity = await context.Set<InvoiceJournalEntity>()
            .SingleOrDefaultAsync(x => x.TenantHash == tenantHash && x.KeyHash == keyHash, cancellationToken).ConfigureAwait(false);
        if (entity is null) return null;
        EnsureIdentifierMatch(entity, tenantId, idempotencyKey);
        return ToOperation(entity);
    }

    public async Task<IReadOnlyList<InvoiceOperation>> ListPendingAsync(string tenantId, int limit = 100,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(tenantId, nameof(tenantId));
        if (limit is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(limit));
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        string tenantHash = HashIdentifier(tenantId);
        long now = clock.GetUtcNow().ToUnixTimeMilliseconds();
        string[] enabledServices = modelOptions.InvoicingServices
            .Select(NetArcaWsModelOptions.GetTicketServiceName).Select(HashIdentifier).ToArray();
        List<InvoiceJournalEntity> entities = await context.Set<InvoiceJournalEntity>()
            .Where(x => x.TenantHash == tenantHash && enabledServices.Contains(x.ServiceHash) &&
                (x.State == (int)InvoiceState.Prepared || x.State == (int)InvoiceState.Unknown ||
                 x.State == (int)InvoiceState.Conflict || x.State == (int)InvoiceState.ManualReview ||
                 ((x.State == (int)InvoiceState.Submitting || x.State == (int)InvoiceState.Reconciling) && x.LeaseUntilMilliseconds <= now)))
            .OrderBy(x => x.CreatedUtcTicks).Take(limit).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (InvoiceJournalEntity entity in entities) EnsureIdentifierMatch(entity, tenantId, entity.IdempotencyKey);
        return entities.Select(ToOperation).ToArray();
    }

    public async Task<InvoiceLease?> TryAcquireAsync(string tenantId, string idempotencyKey, TimeSpan leaseDuration,
        bool reconciliation, CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(tenantId, nameof(tenantId)); ValidateIdentifier(idempotencyKey, nameof(idempotencyKey));
        if (leaseDuration <= TimeSpan.Zero || leaseDuration > TimeSpan.FromMinutes(30)) throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        return await ExecuteWithContentionRetryAsync(
            token => TryAcquireAttemptAsync(tenantId, idempotencyKey, leaseDuration, reconciliation, token), cancellationToken).ConfigureAwait(false);
    }

    private async Task<InvoiceLease?> TryAcquireAttemptAsync(string tenantId, string idempotencyKey, TimeSpan leaseDuration,
        bool reconciliation, CancellationToken cancellationToken)
    {
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        (string tenantHash, string keyHash) = GetOperationKeys(tenantId, idempotencyKey);
        InvoiceJournalEntity? entity = await context.Set<InvoiceJournalEntity>()
            .SingleOrDefaultAsync(x => x.TenantHash == tenantHash && x.KeyHash == keyHash, cancellationToken).ConfigureAwait(false);
        if (entity is null) throw new KeyNotFoundException("The invoice operation does not exist.");
        EnsureIdentifierMatch(entity, tenantId, idempotencyKey);
        EnsureServiceEnabled(entity.Service);
        InvoiceOperation current = ToOperation(entity);
        long now = clock.GetUtcNow().ToUnixTimeMilliseconds();
        bool active = entity.LeaseUntilMilliseconds is long expiry && expiry > now;
        bool allowed = reconciliation
            ? current.State is InvoiceState.Unknown or InvoiceState.ManualReview or InvoiceState.Conflict ||
                (current.State is InvoiceState.Submitting or InvoiceState.Reconciling && !active)
            : current.State == InvoiceState.Prepared;
        if (active || !allowed) return null;
        InvoiceState nextState = reconciliation ? InvoiceState.Reconciling : InvoiceState.Submitting;
        long nextVersion = checked(current.Version + 1);
        long leaseUntil = clock.GetUtcNow().Add(leaseDuration).ToUnixTimeMilliseconds();
        long? oldLease = entity.LeaseUntilMilliseconds;
        int changed = await context.Set<InvoiceJournalEntity>()
            .Where(x => x.TenantHash == tenantHash && x.KeyHash == keyHash && x.Version == current.Version &&
                x.State == (int)current.State && x.LeaseUntilMilliseconds == oldLease)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.State, (int)nextState)
                .SetProperty(x => x.Version, nextVersion)
                .SetProperty(x => x.Attempt, x => x.Attempt + 1)
                .SetProperty(x => x.LeaseUntilMilliseconds, leaseUntil), cancellationToken).ConfigureAwait(false);
        if (changed != 1) return null;
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new InvoiceLease(current with { State = nextState, Version = nextVersion, Attempt = current.Attempt + 1 }, nextVersion);
    }

    public async Task<InvoiceOperation> CompleteAsync(InvoiceLease lease, InvoiceDecision decision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease); ArgumentNullException.ThrowIfNull(decision);
        ValidateDecision(decision);
        return await ExecuteWithContentionRetryAsync(
            token => CompleteAttemptAsync(lease, decision, token), cancellationToken).ConfigureAwait(false);
    }

    private async Task<InvoiceOperation> CompleteAttemptAsync(InvoiceLease lease, InvoiceDecision decision,
        CancellationToken cancellationToken)
    {
        InvoiceSubmission identity = lease.Operation.Submission;
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        (string tenantHash, string keyHash) = GetOperationKeys(identity.TenantId, identity.IdempotencyKey);
        InvoiceJournalEntity? current = await context.Set<InvoiceJournalEntity>()
            .SingleOrDefaultAsync(x => x.TenantHash == tenantHash && x.KeyHash == keyHash, cancellationToken).ConfigureAwait(false);
        if (current is null) throw new KeyNotFoundException("Invoice operation not found.");
        EnsureIdentifierMatch(current, identity.TenantId, identity.IdempotencyKey);
        InvoiceOperation stored = ToOperation(current);
        if (stored.Submission == identity && stored.Version == lease.Version + 1 &&
            stored.State == decision.State && stored.AuthorizationCode == decision.AuthorizationCode &&
            stored.ResponseXml == decision.ResponseXml)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return stored;
        }
        long now = clock.GetUtcNow().ToUnixTimeMilliseconds();
        if (stored.Submission != identity || stored.Version != lease.Version ||
            stored.State is not (InvoiceState.Submitting or InvoiceState.Reconciling) ||
            current.LeaseUntilMilliseconds is not long expiry || expiry <= now)
            throw new InvalidOperationException("The invoice lease has expired or has been superseded.");
        int changed = await context.Set<InvoiceJournalEntity>()
            .Where(x => x.TenantHash == tenantHash && x.KeyHash == keyHash && x.Version == lease.Version &&
                (x.State == (int)InvoiceState.Submitting || x.State == (int)InvoiceState.Reconciling) && x.LeaseUntilMilliseconds > now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.State, (int)decision.State)
                .SetProperty(x => x.Version, x => x.Version + 1)
                .SetProperty(x => x.LeaseUntilMilliseconds, (long?)null)
                .SetProperty(x => x.AuthorizationCode, decision.AuthorizationCode)
                .SetProperty(x => x.ResponseXml, decision.ResponseXml), cancellationToken).ConfigureAwait(false);
        if (changed != 1) throw new InvalidOperationException("The invoice lease has expired or has been superseded.");
        if (decision.State is InvoiceState.Authorized or InvoiceState.Rejected)
            await context.Set<InvoiceSeriesReservationEntity>()
                .Where(x => x.TenantHash == tenantHash && x.KeyHash == keyHash)
                .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return stored with { State = decision.State, Version = stored.Version + 1,
            AuthorizationCode = decision.AuthorizationCode, ResponseXml = decision.ResponseXml };
    }

    private static InvoiceJournalEntity CreateEntity(InvoiceSubmission submission, DateTimeOffset now)
    {
        (string tenantHash, string keyHash) = GetOperationKeys(submission.TenantId, submission.IdempotencyKey);
        return new InvoiceJournalEntity
        {
            TenantHash = tenantHash, KeyHash = keyHash, TenantId = submission.TenantId,
            IdempotencyKey = submission.IdempotencyKey, Service = submission.Service, ServiceHash = HashIdentifier(submission.Service),
            Environment = (int)submission.Identity.Environment, Cuit = submission.Identity.Cuit,
            PointOfSale = submission.Identity.PointOfSale, VoucherType = submission.Identity.VoucherType,
            VoucherNumber = submission.Identity.VoucherNumber, FiscalHash = GetFiscalHash(submission.Identity),
            RemoteHash = submission.RemoteRequestId is null ? null : GetRemoteHash(submission),
            RemoteRequestId = submission.RemoteRequestId, Payload = submission.Payload,
            PayloadHash = Hash(submission.Payload), CanonicalVersion = submission.CanonicalVersion,
            State = (int)InvoiceState.Prepared, Version = 0, Attempt = 0, CreatedUtcTicks = now.UtcTicks
        };
    }

    private static InvoiceRevisionEntity ToRevision(InvoiceJournalEntity entity, int revisionNumber) => new()
    {
        TenantHash = entity.TenantHash, KeyHash = entity.KeyHash, RevisionNumber = revisionNumber,
        TenantId = entity.TenantId, IdempotencyKey = entity.IdempotencyKey, Service = entity.Service,
        Environment = entity.Environment, Cuit = entity.Cuit, PointOfSale = entity.PointOfSale,
        VoucherType = entity.VoucherType, VoucherNumber = entity.VoucherNumber, RemoteRequestId = entity.RemoteRequestId,
        Payload = entity.Payload, PayloadHash = entity.PayloadHash, CanonicalVersion = entity.CanonicalVersion,
        State = entity.State, Version = entity.Version, Attempt = entity.Attempt,
        LeaseUntilMilliseconds = entity.LeaseUntilMilliseconds, AuthorizationCode = entity.AuthorizationCode,
        ResponseXml = entity.ResponseXml,
        SnapshotHash = GetRevisionHash(entity.TenantId, entity.IdempotencyKey, entity.Service,
            new InvoiceIdentity((ArcaEnvironment)entity.Environment, entity.Cuit, entity.PointOfSale, entity.VoucherType, entity.VoucherNumber),
            entity.Payload, entity.CanonicalVersion, (InvoiceState)entity.State, entity.Version, entity.Attempt,
            entity.LeaseUntilMilliseconds, entity.AuthorizationCode, entity.ResponseXml, entity.RemoteRequestId)
    };

    private static InvoiceOperation ToOperation(InvoiceJournalEntity entity)
    {
        if (HashIdentifier(entity.TenantId) != entity.TenantHash || HashIdentifier(entity.IdempotencyKey) != entity.KeyHash)
            throw new InvalidDataException("The immutable invoice identifiers failed their integrity check.");
        var identity = new InvoiceIdentity((ArcaEnvironment)entity.Environment, entity.Cuit, entity.PointOfSale, entity.VoucherType, entity.VoucherNumber);
        var submission = new InvoiceSubmission(entity.TenantId, entity.IdempotencyKey, entity.Service, identity,
            entity.Payload, entity.CanonicalVersion, entity.RemoteRequestId);
        if (Hash(submission.Payload) != entity.PayloadHash) throw new InvalidDataException("The immutable invoice payload failed its integrity check.");
        if (HashIdentifier(submission.Service) != entity.ServiceHash || GetFiscalHash(identity) != entity.FiscalHash ||
            (submission.RemoteRequestId is null ? entity.RemoteHash is not null : GetRemoteHash(submission) != entity.RemoteHash))
            throw new InvalidDataException("The immutable invoice identifiers failed their integrity check.");
        return new InvoiceOperation(submission, (InvoiceState)entity.State, entity.Version, entity.Attempt,
            entity.AuthorizationCode, entity.ResponseXml);
    }

    private static InvoiceOperation ToOperation(InvoiceRevisionEntity entity)
    {
        var identity = new InvoiceIdentity((ArcaEnvironment)entity.Environment, entity.Cuit, entity.PointOfSale, entity.VoucherType, entity.VoucherNumber);
        var submission = new InvoiceSubmission(entity.TenantId, entity.IdempotencyKey, entity.Service, identity,
            entity.Payload, entity.CanonicalVersion, entity.RemoteRequestId);
        if (Hash(submission.Payload) != entity.PayloadHash) throw new InvalidDataException("The immutable invoice revision failed its integrity check.");
        if (HashIdentifier(entity.TenantId) != entity.TenantHash || HashIdentifier(entity.IdempotencyKey) != entity.KeyHash ||
            GetRevisionHash(entity.TenantId, entity.IdempotencyKey, entity.Service, identity, entity.Payload,
                entity.CanonicalVersion, (InvoiceState)entity.State, entity.Version, entity.Attempt,
                entity.LeaseUntilMilliseconds, entity.AuthorizationCode, entity.ResponseXml, entity.RemoteRequestId) != entity.SnapshotHash)
            throw new InvalidDataException("The immutable invoice revision failed its integrity check.");
        return new InvoiceOperation(submission, (InvoiceState)entity.State, entity.Version, entity.Attempt,
            entity.AuthorizationCode, entity.ResponseXml);
    }

    private static string GetRemoteHash(InvoiceSubmission submission) => HashParts(
        submission.Identity.Environment.ToString(), submission.Identity.Cuit.ToString(System.Globalization.CultureInfo.InvariantCulture),
        submission.Service, submission.RemoteRequestId!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private static string GetRevisionHash(string tenant, string key, string service, InvoiceIdentity identity, string payload,
        int canonicalVersion, InvoiceState state, long version, int attempt, long? leaseUntil, string? authorization,
        string? response, long? remoteRequestId) => HashParts(tenant, key, service,
            ((int)identity.Environment).ToString(System.Globalization.CultureInfo.InvariantCulture),
            identity.Cuit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            identity.PointOfSale.ToString(System.Globalization.CultureInfo.InvariantCulture),
            identity.VoucherType.ToString(System.Globalization.CultureInfo.InvariantCulture),
            identity.VoucherNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), payload,
            canonicalVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ((int)state).ToString(System.Globalization.CultureInfo.InvariantCulture),
            version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            attempt.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Optional(leaseUntil?.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            Optional(authorization), Optional(response),
            Optional(remoteRequestId?.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    private static string Optional(string? value) => value is null ? "\u0000" : "\u0001" + value;
    private static string GetSeriesHash(InvoiceIdentity identity) => HashParts(
        ((int)identity.Environment).ToString(System.Globalization.CultureInfo.InvariantCulture),
        identity.Cuit.ToString(System.Globalization.CultureInfo.InvariantCulture),
        identity.PointOfSale.ToString(System.Globalization.CultureInfo.InvariantCulture),
        identity.VoucherType.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private static string GetFiscalHash(InvoiceIdentity identity) => HashParts(
        ((int)identity.Environment).ToString(System.Globalization.CultureInfo.InvariantCulture),
        identity.Cuit.ToString(System.Globalization.CultureInfo.InvariantCulture),
        identity.PointOfSale.ToString(System.Globalization.CultureInfo.InvariantCulture),
        identity.VoucherType.ToString(System.Globalization.CultureInfo.InvariantCulture),
        identity.VoucherNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
    private static (string, string) GetOperationKeys(string tenant, string key) => (HashIdentifier(tenant), HashIdentifier(key));
    private static string HashIdentifier(string value) => HashParts(value);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string HashParts(params string[] values)
    {
        using var stream = new MemoryStream();
        Span<byte> length = stackalloc byte[4];
        foreach (string value in values)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(value);
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            stream.Write(length);
            stream.Write(bytes);
        }
        return Hash(Convert.ToBase64String(stream.ToArray()));
    }

    private static void EnsureIdentifierMatch(InvoiceJournalEntity entity, string tenant, string key) =>
        EnsureIdentifierMatch(entity.TenantId, entity.IdempotencyKey, tenant, key);
    private static void EnsureIdentifierMatch(string storedTenant, string storedKey, string tenant, string key)
    {
        if (!string.Equals(storedTenant, tenant, StringComparison.Ordinal) || !string.Equals(storedKey, key, StringComparison.Ordinal))
            throw new InvalidDataException("The stored invoice identifier failed its integrity check.");
    }

    private static bool IsConstraintFailure(Exception exception) =>
        Exceptions(exception).Any(current => current is SqliteException { SqliteErrorCode: 19 } ||
            current is DbException { SqlState: "23000" or "23505" });

    private static bool IsContentionFailure(Exception exception) => Exceptions(exception).Any(current =>
        current is SqliteException sqlite && (sqlite.SqliteErrorCode & 0xff) is 5 or 6 ||
        current is DbException { SqlState: "40001" or "40P01" or "41000" } ||
        current is DbException dbException && IsMySqlLockWaitTimeout(dbException));

    private static bool IsMySqlLockWaitTimeout(DbException exception)
    {
        if (exception.GetType().FullName != "MySqlConnector.MySqlException" || exception.SqlState != "HY000") return false;
        return exception.GetType().GetProperty("Number")?.GetValue(exception) is int number && number == 1205;
    }

    private static IEnumerable<Exception> Exceptions(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            yield return current;
    }

    private static async Task<TResult> ExecuteWithContentionRetryAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;
        for (int attempt = 1; ; attempt++)
        {
            try { return await operation(cancellationToken).ConfigureAwait(false); }
            catch (Exception exception) when (attempt < maximumAttempts && IsContentionFailure(exception))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10 * attempt), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static void Validate(InvoiceSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission); ArgumentNullException.ThrowIfNull(submission.Identity);
        ValidateIdentifier(submission.TenantId, nameof(submission.TenantId));
        ValidateIdentifier(submission.IdempotencyKey, nameof(submission.IdempotencyKey));
        ValidateIdentifier(submission.Service, nameof(submission.Service));
        if (!Enum.IsDefined(submission.Identity.Environment) || submission.Identity.Cuit is < 10_000_000_000 or > 99_999_999_999 ||
            submission.Identity.PointOfSale is < 1 or > 99999 || submission.Identity.VoucherType <= 0 || submission.Identity.VoucherNumber <= 0)
            throw new ArgumentException("Invalid fiscal identity.", nameof(submission));
        if (submission.CanonicalVersion <= 0) throw new ArgumentOutOfRangeException(nameof(submission.CanonicalVersion));
        if (submission.RemoteRequestId is <= 0) throw new ArgumentOutOfRangeException(nameof(submission.RemoteRequestId), "A service-level request ID must be positive.");
        if (string.IsNullOrWhiteSpace(submission.Payload) || submission.Payload.Length > 2 * 1024 * 1024)
            throw new ArgumentException("A nonempty payload of at most 2 MiB is required.", nameof(submission));
    }

    private static void ValidateIdentifier(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Any(char.IsControl))
            throw new ArgumentException("Identifier must be nonempty and at most 128 characters.", parameterName);
        try { _ = StrictUtf8.GetByteCount(value); }
        catch (EncoderFallbackException ex) { throw new ArgumentException("Identifier contains invalid Unicode.", parameterName, ex); }
    }

    private void EnsureServiceEnabled(string service)
    {
        if (!modelOptions.SupportsInvoiceService(service))
            throw new InvalidOperationException($"The invoice journal is disabled for ARCA service '{service}'.");
    }

    private async Task<TContext> CreateContextAsync(CancellationToken cancellationToken)
    {
        TContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        string? fingerprint = context.Model.FindAnnotation(NetArcaWsModelBuilderExtensions.OptionsAnnotationName)?.Value as string;
        bool matchesContextOptions = context is not INetArcaWsModelOptionsProvider provider ||
            string.Equals(provider.NetArcaWsModelOptions.Fingerprint, modelOptions.Fingerprint, StringComparison.Ordinal);
        if (!string.Equals(fingerprint, modelOptions.Fingerprint, StringComparison.Ordinal) || !matchesContextOptions)
        {
            await context.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException("The EF context model selection does not match the invoice journal service selection.");
        }
        return context;
    }

    private static void ValidateDecision(InvoiceDecision decision)
    {
        if (decision.State is not (InvoiceState.Authorized or InvoiceState.Rejected or InvoiceState.Unknown or InvoiceState.Conflict or InvoiceState.ManualReview))
            throw new ArgumentException("Only a definitive decision or unknown/manual-review status can complete a lease.", nameof(decision));
        if (decision.State == InvoiceState.Authorized && (string.IsNullOrEmpty(decision.AuthorizationCode) || decision.AuthorizationCode.Any(c => c is < '0' or > '9')))
            throw new ArgumentException("An authorization requires a numeric authorization code.", nameof(decision));
        if (decision.ResponseXml?.Length > 4 * 1024 * 1024) throw new ArgumentException("Response exceeds journal limit.", nameof(decision));
    }
}

public static class NetArcaWsEntityFrameworkServiceCollectionExtensions
{
    /// <summary>Registers the journal as a singleton backed by an independently-created context per call.</summary>
    /// <remarks>The same model options must be applied with <c>ModelBuilder.AddNetArcaWs</c> in consumer contexts.</remarks>
    public static IServiceCollection AddNetArcaWsEntityFrameworkStores<TContext>(this IServiceCollection services, NetArcaWsModelOptions modelOptions)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(modelOptions);
        if (!modelOptions.InvoicingEnabled) throw new ArgumentException("Select at least one invoicing service before registering the EF invoice journal.", nameof(modelOptions));
        services.TryAddSingleton(modelOptions);
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IInvoiceJournal>(provider => new EfInvoiceJournal<TContext>(
            provider.GetRequiredService<IDbContextFactory<TContext>>(), modelOptions, provider.GetRequiredService<TimeProvider>()));
        return services;
    }
}
