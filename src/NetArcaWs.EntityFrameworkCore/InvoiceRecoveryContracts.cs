using System.Collections.Immutable;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;

namespace NetArcaWs.EntityFrameworkCore;

public sealed class InvoiceRecoveryScope
{
    private readonly ImmutableHashSet<ArcaService> services;
    public string TenantId { get; }
    public IReadOnlySet<ArcaService> Services => services;

    public InvoiceRecoveryScope(string tenantId, IEnumerable<ArcaService> services)
    {
        RecoveryInput.Validate(tenantId, nameof(tenantId));
        ArgumentNullException.ThrowIfNull(services);
        ArcaService[] selected = services.ToArray();
        if (selected.Length == 0 || selected.Any(x => !RecoveryInput.IsInvoiceService(x)) || selected.Distinct().Count() != selected.Length)
            throw new ArgumentException("Select unique supported invoice recovery services.", nameof(services));
        TenantId = tenantId;
        this.services = selected.ToImmutableHashSet();
    }

    public override string ToString() => "Invoice recovery scope (tenant and services hidden)";
}

public enum InvoiceRecoveryState { Scheduled, Claimed, Completed, Suspended }
public enum InvoiceRecoveryDisposition { Reschedule, Complete, Suspend, ManualReview }
public enum InvoiceRecoverySafeReason
{
    None, QueryEmpty, QueryUnavailable, LeaseExpired, CredentialUnavailable,
    AuthorizationDenied, IncompatibleSnapshot, MaxAttemptsReached, PersistenceConflict
}

public sealed record InvoiceRecoveryWorkItem(string TenantId, string IdempotencyKey, long InvoiceVersion,
    string PayloadHash, int CanonicalVersion, ArcaService Service, InvoiceIdentity Identity,
    string? CredentialReference, int Attempt, DateTimeOffset NextAvailable)
{
    public override string ToString() => "Invoice recovery work item (fiscal and credential data hidden)";
}

public sealed record InvoiceRecoveryLease(InvoiceRecoveryWorkItem WorkItem, InvoiceOperation CurrentOperation,
    string ClaimId, long Generation, DateTimeOffset LeaseUntil)
{
    public override string ToString() => "Invoice recovery lease (claim and fiscal data hidden)";
}

public sealed record InvoiceRecoveryMetadata(InvoiceRecoveryState State, int Attempt, DateTimeOffset NextAvailable,
    DateTimeOffset? LeaseUntil, long Generation, InvoiceRecoverySafeReason LastReason)
{
    public override string ToString() => $"Invoice recovery metadata {State} (safe metadata only)";
}

public sealed class InvoiceRecoveryConflictException(string message) : InvalidOperationException(message);

public interface IInvoiceRecoveryQueue
{
    Task<InvoiceOperation> PrepareAndEnqueueAsync(InvoiceSubmission submission, string? credentialReference, CancellationToken cancellationToken = default);
    Task<InvoiceOperation> ScheduleAsync(string tenantId, string idempotencyKey, long expectedInvoiceVersion, string? credentialReference, CancellationToken cancellationToken = default);
    Task<InvoiceRecoveryLease?> TryClaimAsync(InvoiceRecoveryScope scope, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task<bool> IsCurrentAsync(InvoiceRecoveryLease lease, CancellationToken cancellationToken = default);
    Task CompleteAsync(InvoiceRecoveryLease lease, InvoiceRecoveryDisposition disposition, InvoiceRecoverySafeReason safeReason,
        DateTimeOffset? nextAvailable = null, CancellationToken cancellationToken = default);
    Task<InvoiceRecoveryMetadata?> FindAsync(InvoiceRecoveryScope scope, string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IInvoiceRecoveryContextResolver
{
    ValueTask<ArcaTenantContext> ResolveAsync(InvoiceRecoveryWorkItem workItem, CancellationToken cancellationToken = default);
}

public sealed record InvoiceRecoveryWorkerOptions(TimeSpan LeaseDuration, TimeSpan IdleInterval, int MaxAttempts,
    TimeSpan BaseBackoff, TimeSpan MaxBackoff, double JitterRatio)
{
    public override string ToString() => "Invoice recovery worker options";
}

public sealed class InvoiceRecoveryWorkerOptionsBuilder
{
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan IdleInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxAttempts { get; set; } = 10;
    public TimeSpan BaseBackoff { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromMinutes(5);
    public double JitterRatio { get; set; } = 0.2;
    public override string ToString() => "Invoice recovery worker options builder";
}

public interface IInvoiceRecoveryProcessor
{
    Task<bool> RunOnceAsync(InvoiceRecoveryScope scope, CancellationToken cancellationToken = default);
}

internal static class RecoveryInput
{
    internal static bool IsInvoiceService(ArcaService service) => service is ArcaService.Wsfev1 or ArcaService.Wsfexv1 or ArcaService.Wsmtxca;
    internal static void Validate(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 512 || value.Any(char.IsControl))
            throw new ArgumentException("Value must be nonempty, valid Unicode, and at most 512 UTF-16 code units.", parameterName);
        try { _ = new System.Text.UTF8Encoding(false, true).GetByteCount(value); }
        catch (System.Text.EncoderFallbackException) { throw new ArgumentException("Value contains invalid Unicode.", parameterName); }
    }
    internal static void ValidateOptional(string? value, string parameterName)
    {
        if (value is not null) Validate(value, parameterName);
    }
}
