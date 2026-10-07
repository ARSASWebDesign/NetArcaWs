using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;

namespace NetArcaWs.EntityFrameworkCore;

public sealed partial class EfInvoiceJournal<TContext> : IInvoiceRecoveryQueue where TContext : DbContext
{
    public async Task<InvoiceOperation> PrepareAndEnqueueAsync(InvoiceSubmission submission, string? credentialReference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submission);
        RecoveryInput.Validate(submission.TenantId, nameof(submission.TenantId));
        RecoveryInput.Validate(submission.IdempotencyKey, nameof(submission.IdempotencyKey));
        RecoveryInput.ValidateOptional(credentialReference, nameof(credentialReference));
        EnsureRecoveryService(submission.Service);
        try
        {
            return await ExecuteWithContentionRetryAsync(async token =>
            {
                await using TContext context = await CreateContextAsync(token).ConfigureAwait(false);
                await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, token).ConfigureAwait(false);
                InvoiceOperation operation = await PrepareInContextAsync(context, submission, token).ConfigureAwait(false);
                InvoiceJournalEntity invoice = await GetInvoiceEntityAsync(context, submission.TenantId, submission.IdempotencyKey, token).ConfigureAwait(false);
                InvoiceRecoveryJobEntity? job = await context.Set<InvoiceRecoveryJobEntity>()
                    .SingleOrDefaultAsync(x => x.TenantHash == invoice.TenantHash && x.KeyHash == invoice.KeyHash, token).ConfigureAwait(false);
                if (job is null)
                {
                    context.Add(CreateJob(invoice, credentialReference, clock.GetUtcNow().ToUnixTimeMilliseconds()));
                    await context.SaveChangesAsync(token).ConfigureAwait(false);
                }
                else
                {
                    EnsureJobIdentifiers(job, submission.TenantId, submission.IdempotencyKey);
                    if (job.CredentialReference != credentialReference || !FiscalSnapshotMatches(job, invoice) || job.InvoiceVersion > invoice.Version)
                        throw new InvoiceRecoveryConflictException("The queued invoice differs from its pinned snapshot.");
                    if (job.InvoiceVersion < invoice.Version)
                    {
                        int refreshed = await context.Set<InvoiceRecoveryJobEntity>()
                            .Where(x => x.TenantHash == invoice.TenantHash && x.KeyHash == invoice.KeyHash &&
                                x.InvoiceVersion == job.InvoiceVersion && x.Generation == job.Generation && x.State == job.State && x.ClaimId == job.ClaimId)
                            .ExecuteUpdateAsync(update => update.SetProperty(x => x.InvoiceVersion, invoice.Version), token).ConfigureAwait(false);
                        if (refreshed != 1) throw new InvoiceRecoveryConflictException("The recovery snapshot changed while its technical version was refreshed.");
                    }
                    // An identical replay is observational: it must not reset schedule, attempts, generation, or credential pin.
                }
                await transaction.CommitAsync(token).ConfigureAwait(false);
                return operation;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsConstraintFailure(exception))
        {
            InvoiceOperation? winner = await FindQueuedWinnerAsync(submission, credentialReference, cancellationToken).ConfigureAwait(false);
            if (winner is not null) return winner;
            throw new InvoiceRecoveryConflictException("The fiscal identity, series, or recovery job is already reserved.");
        }
        catch (InvoiceConflictException)
        {
            throw new InvoiceRecoveryConflictException("The idempotency key is bound to a different immutable submission.");
        }
    }

    public async Task<InvoiceOperation> ScheduleAsync(string tenantId, string idempotencyKey, long expectedInvoiceVersion,
        string? credentialReference, CancellationToken cancellationToken = default)
    {
        RecoveryInput.Validate(tenantId, nameof(tenantId)); RecoveryInput.Validate(idempotencyKey, nameof(idempotencyKey));
        RecoveryInput.ValidateOptional(credentialReference, nameof(credentialReference));
        if (expectedInvoiceVersion < 0) throw new ArgumentOutOfRangeException(nameof(expectedInvoiceVersion));
        return await ExecuteWithContentionRetryAsync(async token =>
        {
            await using TContext context = await CreateContextAsync(token).ConfigureAwait(false);
            await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, token).ConfigureAwait(false);
            InvoiceJournalEntity invoice = await GetInvoiceEntityAsync(context, tenantId, idempotencyKey, token).ConfigureAwait(false);
            InvoiceOperation operation = ToOperation(invoice);
            EnsureRecoveryService(operation.Submission.Service);
            long now = clock.GetUtcNow().ToUnixTimeMilliseconds();
            if (invoice.Version != expectedInvoiceVersion || operation.State is not (InvoiceState.Prepared or InvoiceState.Unknown or InvoiceState.Submitting or InvoiceState.Reconciling))
                throw new InvoiceRecoveryConflictException("The invoice version or state does not permit explicit recovery scheduling.");
            if (invoice.LeaseUntilMilliseconds is long invoiceLease && invoiceLease > now)
                throw new InvoiceRecoveryConflictException("The invoice has an active journal lease.");
            InvoiceRecoveryJobEntity? job = await context.Set<InvoiceRecoveryJobEntity>()
                .SingleOrDefaultAsync(x => x.TenantHash == invoice.TenantHash && x.KeyHash == invoice.KeyHash, token).ConfigureAwait(false);
            if (job is null)
            {
                job = CreateJob(invoice, credentialReference, now);
                job.Generation = 1;
                context.Add(job);
            }
            else
            {
                EnsureJobIdentifiers(job, tenantId, idempotencyKey);
                if (job.LeaseUntilMilliseconds is long activeLease && activeLease > now)
                    throw new InvoiceRecoveryConflictException("The recovery job has an active claim.");
                job.Service = invoice.Service;
                job.InvoiceVersion = invoice.Version;
                job.PayloadHash = invoice.PayloadHash;
                job.CanonicalVersion = invoice.CanonicalVersion;
                CopyIdentity(job, invoice);
                job.CredentialReference = credentialReference;
                job.State = (int)InvoiceRecoveryState.Scheduled;
                job.Attempt = 0;
                job.NextAvailableMilliseconds = now;
                job.LeaseUntilMilliseconds = null;
                job.ClaimId = null;
                job.Generation = checked(job.Generation + 1);
                job.LastReason = (int)InvoiceRecoverySafeReason.None;
            }
            await context.SaveChangesAsync(token).ConfigureAwait(false);
            await transaction.CommitAsync(token).ConfigureAwait(false);
            return operation;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InvoiceRecoveryLease?> TryClaimAsync(InvoiceRecoveryScope scope, TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (leaseDuration <= TimeSpan.Zero || leaseDuration > TimeSpan.FromMinutes(30)) throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        EnsureRecoveryScope(scope);
        return await ExecuteWithContentionRetryAsync(async token =>
        {
            await using TContext context = await CreateContextAsync(token).ConfigureAwait(false);
            await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, token).ConfigureAwait(false);
            long now = clock.GetUtcNow().ToUnixTimeMilliseconds();
            string tenantHash = HashIdentifier(scope.TenantId);
            string[] services = scope.Services.Select(NetArcaWsModelOptions.GetTicketServiceName).ToArray();
            InvoiceRecoveryJobEntity? job = await context.Set<InvoiceRecoveryJobEntity>()
                .Where(x => x.TenantHash == tenantHash && services.Contains(x.Service) &&
                    ((x.State == (int)InvoiceRecoveryState.Scheduled && x.NextAvailableMilliseconds <= now) ||
                     (x.State == (int)InvoiceRecoveryState.Claimed && x.LeaseUntilMilliseconds <= now)))
                .OrderBy(x => x.NextAvailableMilliseconds).ThenBy(x => x.TenantHash).ThenBy(x => x.KeyHash)
                .FirstOrDefaultAsync(token).ConfigureAwait(false);
            if (job is null) { await transaction.CommitAsync(token).ConfigureAwait(false); return null; }
            EnsureJobIdentifiers(job, scope.TenantId, job.IdempotencyKey);
            InvoiceJournalEntity? invoice = await context.Set<InvoiceJournalEntity>()
                .SingleOrDefaultAsync(x => x.TenantHash == tenantHash && x.KeyHash == job.KeyHash, token).ConfigureAwait(false);
            InvoiceOperation? storedOperation = invoice is null ? null : ToOperation(invoice);
            if (invoice is null || storedOperation is null || !FiscalSnapshotMatches(job, invoice) || job.InvoiceVersion > invoice.Version)
            {
                job.State = (int)InvoiceRecoveryState.Suspended;
                job.LeaseUntilMilliseconds = null; job.ClaimId = null;
                job.LastReason = (int)InvoiceRecoverySafeReason.IncompatibleSnapshot;
                await context.SaveChangesAsync(token).ConfigureAwait(false);
                await transaction.CommitAsync(token).ConfigureAwait(false);
                return null;
            }
            long expectedQueueInvoiceVersion = job.InvoiceVersion;
            InvoiceOperation operation = storedOperation;
            if (operation.State is InvoiceState.Conflict or InvoiceState.ManualReview)
            {
                int changed = await context.Set<InvoiceRecoveryJobEntity>()
                    .Where(x => x.TenantHash == tenantHash && x.KeyHash == job.KeyHash && x.Generation == job.Generation &&
                        x.InvoiceVersion == expectedQueueInvoiceVersion && x.State == job.State && x.ClaimId == job.ClaimId)
                    .ExecuteUpdateAsync(update => update.SetProperty(x => x.InvoiceVersion, invoice.Version)
                        .SetProperty(x => x.State, (int)InvoiceRecoveryState.Suspended)
                        .SetProperty(x => x.LeaseUntilMilliseconds, (long?)null).SetProperty(x => x.ClaimId, (string?)null)
                        .SetProperty(x => x.LastReason, (int)InvoiceRecoverySafeReason.PersistenceConflict), token).ConfigureAwait(false);
                if (changed != 1) { await transaction.RollbackAsync(token).ConfigureAwait(false); return null; }
                await transaction.CommitAsync(token).ConfigureAwait(false);
                return null;
            }
            if (operation.State is InvoiceState.Authorized or InvoiceState.Rejected)
            {
                int changed = await context.Set<InvoiceRecoveryJobEntity>()
                    .Where(x => x.TenantHash == tenantHash && x.KeyHash == job.KeyHash && x.Generation == job.Generation &&
                        x.InvoiceVersion == expectedQueueInvoiceVersion && x.State == job.State && x.ClaimId == job.ClaimId)
                    .ExecuteUpdateAsync(update => update.SetProperty(x => x.InvoiceVersion, invoice.Version)
                        .SetProperty(x => x.State, (int)InvoiceRecoveryState.Completed)
                        .SetProperty(x => x.LeaseUntilMilliseconds, (long?)null).SetProperty(x => x.ClaimId, (string?)null), token).ConfigureAwait(false);
                if (changed != 1) { await transaction.RollbackAsync(token).ConfigureAwait(false); return null; }
                await transaction.CommitAsync(token).ConfigureAwait(false);
                return null;
            }
            if (operation.State == InvoiceState.Prepared && job.State == (int)InvoiceRecoveryState.Claimed)
            {
                int changed = await context.Set<InvoiceJournalEntity>()
                    .Where(x => x.TenantHash == tenantHash && x.KeyHash == job.KeyHash && x.Version == invoice.Version &&
                        x.State == (int)InvoiceState.Prepared)
                    .ExecuteUpdateAsync(update => update.SetProperty(x => x.State, (int)InvoiceState.Unknown)
                        .SetProperty(x => x.Version, x => x.Version + 1), token).ConfigureAwait(false);
                if (changed != 1) { await transaction.CommitAsync(token).ConfigureAwait(false); return null; }
                invoice.State = (int)InvoiceState.Unknown;
                invoice.Version = checked(invoice.Version + 1);
                job.InvoiceVersion = invoice.Version;
            }
            else if (operation.State is InvoiceState.Submitting or InvoiceState.Reconciling &&
                invoice.LeaseUntilMilliseconds is long journalExpiry && journalExpiry > now)
            {
                // The invoice's own lease still fences its active operation.
                int changed = await context.Set<InvoiceRecoveryJobEntity>()
                    .Where(x => x.TenantHash == tenantHash && x.KeyHash == job.KeyHash && x.Generation == job.Generation &&
                        x.InvoiceVersion == expectedQueueInvoiceVersion && x.State == job.State && x.ClaimId == job.ClaimId)
                    .ExecuteUpdateAsync(update => update.SetProperty(x => x.InvoiceVersion, invoice.Version)
                        .SetProperty(x => x.State, (int)InvoiceRecoveryState.Scheduled)
                        .SetProperty(x => x.NextAvailableMilliseconds, journalExpiry)
                        .SetProperty(x => x.LeaseUntilMilliseconds, (long?)null).SetProperty(x => x.ClaimId, (string?)null), token).ConfigureAwait(false);
                if (changed != 1) { await transaction.RollbackAsync(token).ConfigureAwait(false); return null; }
                await transaction.CommitAsync(token).ConfigureAwait(false);
                return null;
            }
            string claimId = Guid.NewGuid().ToString("N");
            long generation = checked(job.Generation + 1);
            long expiry = checked(now + Math.Max(1, (long)Math.Ceiling(leaseDuration.TotalMilliseconds)));
            int updated = await context.Set<InvoiceRecoveryJobEntity>()
                .Where(x => x.TenantHash == tenantHash && x.KeyHash == job.KeyHash && x.Generation == job.Generation &&
                    x.InvoiceVersion == expectedQueueInvoiceVersion && x.State == job.State && x.ClaimId == job.ClaimId &&
                    (x.State == (int)InvoiceRecoveryState.Scheduled || x.LeaseUntilMilliseconds <= now))
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.State, (int)InvoiceRecoveryState.Claimed)
                    .SetProperty(x => x.Generation, generation).SetProperty(x => x.ClaimId, claimId)
                    .SetProperty(x => x.InvoiceVersion, invoice.Version)
                    .SetProperty(x => x.LeaseUntilMilliseconds, expiry).SetProperty(x => x.Attempt, x => x.Attempt + 1), token).ConfigureAwait(false);
            if (updated != 1) { await transaction.RollbackAsync(token).ConfigureAwait(false); return null; }
            job.State = (int)InvoiceRecoveryState.Claimed; job.Generation = generation; job.ClaimId = claimId;
            job.InvoiceVersion = invoice.Version; job.LeaseUntilMilliseconds = expiry; job.Attempt++;
            operation = ToOperation(invoice);
            InvoiceRecoveryWorkItem item = ToWorkItem(job);
            var claim = new InvoiceRecoveryLease(item, operation, claimId, generation, DateTimeOffset.FromUnixTimeMilliseconds(expiry));
            await transaction.CommitAsync(token).ConfigureAwait(false);
            return claim;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsCurrentAsync(InvoiceRecoveryLease lease, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        string tenantHash = HashIdentifier(lease.WorkItem.TenantId); string keyHash = HashIdentifier(lease.WorkItem.IdempotencyKey);
        long now = clock.GetUtcNow().ToUnixTimeMilliseconds();
        return await context.Set<InvoiceRecoveryJobEntity>().AnyAsync(x => x.TenantHash == tenantHash && x.KeyHash == keyHash &&
            x.State == (int)InvoiceRecoveryState.Claimed && x.ClaimId == lease.ClaimId && x.Generation == lease.Generation &&
            x.LeaseUntilMilliseconds > now, cancellationToken).ConfigureAwait(false);
    }

    public async Task CompleteAsync(InvoiceRecoveryLease lease, InvoiceRecoveryDisposition disposition,
        InvoiceRecoverySafeReason safeReason, DateTimeOffset? nextAvailable = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lease);
        if (!Enum.IsDefined(disposition) || !Enum.IsDefined(safeReason)) throw new ArgumentOutOfRangeException(nameof(disposition));
        if (disposition == InvoiceRecoveryDisposition.Reschedule && nextAvailable is null) throw new ArgumentException("Rescheduling requires a due time.", nameof(nextAvailable));
        string tenantHash = HashIdentifier(lease.WorkItem.TenantId); string keyHash = HashIdentifier(lease.WorkItem.IdempotencyKey);
        long now = clock.GetUtcNow().ToUnixTimeMilliseconds();
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        var update = context.Set<InvoiceRecoveryJobEntity>()
            .Where(x => x.TenantHash == tenantHash && x.KeyHash == keyHash && x.State == (int)InvoiceRecoveryState.Claimed &&
                x.ClaimId == lease.ClaimId && x.Generation == lease.Generation && x.LeaseUntilMilliseconds > now);
        int changed;
        if (disposition == InvoiceRecoveryDisposition.Reschedule)
            changed = await update.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.State, (int)InvoiceRecoveryState.Scheduled)
                .SetProperty(x => x.NextAvailableMilliseconds, nextAvailable!.Value.ToUnixTimeMilliseconds())
                .SetProperty(x => x.LastReason, (int)safeReason)
                .SetProperty(x => x.ClaimId, (string?)null).SetProperty(x => x.LeaseUntilMilliseconds, (long?)null), cancellationToken).ConfigureAwait(false);
        else
            changed = await update.ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.State, disposition == InvoiceRecoveryDisposition.Complete ? (int)InvoiceRecoveryState.Completed : (int)InvoiceRecoveryState.Suspended)
                .SetProperty(x => x.LastReason, (int)safeReason)
                .SetProperty(x => x.ClaimId, (string?)null).SetProperty(x => x.LeaseUntilMilliseconds, (long?)null), cancellationToken).ConfigureAwait(false);
        if (changed != 1) throw new InvoiceRecoveryConflictException("The recovery claim is stale or expired.");
    }

    public async Task<InvoiceRecoveryMetadata?> FindAsync(InvoiceRecoveryScope scope, string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope); RecoveryInput.Validate(idempotencyKey, nameof(idempotencyKey)); EnsureRecoveryScope(scope);
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        string tenantHash = HashIdentifier(scope.TenantId); string keyHash = HashIdentifier(idempotencyKey);
        InvoiceRecoveryJobEntity? job = await context.Set<InvoiceRecoveryJobEntity>()
            .SingleOrDefaultAsync(x => x.TenantHash == tenantHash && x.KeyHash == keyHash, cancellationToken).ConfigureAwait(false);
        if (job is null) return null;
        EnsureJobIdentifiers(job, scope.TenantId, idempotencyKey);
        if (!scope.Services.Select(NetArcaWsModelOptions.GetTicketServiceName).Contains(job.Service, StringComparer.Ordinal)) return null;
        return new InvoiceRecoveryMetadata((InvoiceRecoveryState)job.State, job.Attempt,
            DateTimeOffset.FromUnixTimeMilliseconds(job.NextAvailableMilliseconds), job.LeaseUntilMilliseconds is long expiry ? DateTimeOffset.FromUnixTimeMilliseconds(expiry) : null,
            job.Generation, (InvoiceRecoverySafeReason)job.LastReason);
    }

    private InvoiceRecoveryJobEntity CreateJob(InvoiceJournalEntity invoice, string? credentialReference, long now) => new()
    {
        TenantHash = invoice.TenantHash, KeyHash = invoice.KeyHash, TenantId = invoice.TenantId,
        IdempotencyKey = invoice.IdempotencyKey, Service = invoice.Service, State = (int)InvoiceRecoveryState.Scheduled,
        InvoiceVersion = invoice.Version, PayloadHash = invoice.PayloadHash, CanonicalVersion = invoice.CanonicalVersion,
        Environment = invoice.Environment, Cuit = invoice.Cuit, PointOfSale = invoice.PointOfSale, VoucherType = invoice.VoucherType,
        VoucherNumber = invoice.VoucherNumber, CredentialReference = credentialReference, NextAvailableMilliseconds = now,
        LastReason = (int)InvoiceRecoverySafeReason.None
    };

    private async Task<InvoiceOperation?> FindQueuedWinnerAsync(InvoiceSubmission submission, string? credentialReference,
        CancellationToken cancellationToken)
    {
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
        (string tenantHash, string keyHash) = GetOperationKeys(submission.TenantId, submission.IdempotencyKey);
        InvoiceJournalEntity? invoice = await context.Set<InvoiceJournalEntity>()
            .SingleOrDefaultAsync(x => x.TenantHash == tenantHash && x.KeyHash == keyHash, cancellationToken).ConfigureAwait(false);
        if (invoice is null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        EnsureIdentifierMatch(invoice, submission.TenantId, submission.IdempotencyKey);
        InvoiceOperation winner = ToOperation(invoice);
        if (winner.Submission != submission)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        InvoiceRecoveryJobEntity? job = await context.Set<InvoiceRecoveryJobEntity>()
            .SingleOrDefaultAsync(x => x.TenantHash == tenantHash && x.KeyHash == keyHash, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }
        EnsureJobIdentifiers(job, submission.TenantId, submission.IdempotencyKey);
        if (job.CredentialReference != credentialReference || !FiscalSnapshotMatches(job, invoice) || job.InvoiceVersion > invoice.Version)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return winner;
    }

    private static void CopyIdentity(InvoiceRecoveryJobEntity job, InvoiceJournalEntity invoice)
    { job.Environment = invoice.Environment; job.Cuit = invoice.Cuit; job.PointOfSale = invoice.PointOfSale; job.VoucherType = invoice.VoucherType; job.VoucherNumber = invoice.VoucherNumber; }

    private static InvoiceRecoveryWorkItem ToWorkItem(InvoiceRecoveryJobEntity job) => new(job.TenantId, job.IdempotencyKey,
        job.InvoiceVersion, job.PayloadHash, job.CanonicalVersion, job.Service switch { "wsfe" => ArcaService.Wsfev1, "wsfex" => ArcaService.Wsfexv1, _ => ArcaService.Wsmtxca },
        new((ArcaEnvironment)job.Environment, job.Cuit, job.PointOfSale, job.VoucherType, job.VoucherNumber),
        job.CredentialReference, job.Attempt, DateTimeOffset.FromUnixTimeMilliseconds(job.NextAvailableMilliseconds));

    private static bool FiscalSnapshotMatches(InvoiceRecoveryJobEntity job, InvoiceJournalEntity invoice) =>
        job.TenantHash == invoice.TenantHash && job.KeyHash == invoice.KeyHash && job.Service == invoice.Service &&
        job.PayloadHash == invoice.PayloadHash && job.CanonicalVersion == invoice.CanonicalVersion &&
        job.Environment == invoice.Environment && job.Cuit == invoice.Cuit && job.PointOfSale == invoice.PointOfSale &&
        job.VoucherType == invoice.VoucherType && job.VoucherNumber == invoice.VoucherNumber;

    private void EnsureJobIdentifiers(InvoiceRecoveryJobEntity job, string tenantId, string key)
    {
        if (job.TenantId != tenantId || job.IdempotencyKey != key || HashIdentifier(tenantId) != job.TenantHash || HashIdentifier(key) != job.KeyHash)
            throw new InvalidDataException("The recovery job identifiers failed their integrity check.");
    }

    private async Task<InvoiceJournalEntity> GetInvoiceEntityAsync(TContext context, string tenantId, string key, CancellationToken token)
    {
        (string tenantHash, string keyHash) = GetOperationKeys(tenantId, key);
        InvoiceJournalEntity invoice = await context.Set<InvoiceJournalEntity>()
            .SingleOrDefaultAsync(x => x.TenantHash == tenantHash && x.KeyHash == keyHash, token).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The invoice operation does not exist.");
        EnsureIdentifierMatch(invoice, tenantId, key);
        return invoice;
    }

    private void EnsureRecoveryService(string service)
    {
        ArcaService selected = service switch { "wsfe" => ArcaService.Wsfev1, "wsfex" => ArcaService.Wsfexv1, "wsmtxca" => ArcaService.Wsmtxca,
            _ => throw new InvoiceRecoveryConflictException("The service is not supported by invoice recovery.") };
        if (!modelOptions.InvoiceRecoveryServices.Contains(selected) || !modelOptions.InvoicingServices.Contains(selected))
            throw new InvalidOperationException("Select the invoice and recovery model capabilities for this service.");
    }

    private void EnsureRecoveryScope(InvoiceRecoveryScope scope)
    {
        foreach (ArcaService service in scope.Services)
            if (!modelOptions.InvoiceRecoveryServices.Contains(service) || !modelOptions.InvoicingServices.Contains(service))
                throw new InvalidOperationException("The recovery scope contains a service that is not selected in the EF model.");
    }
}
