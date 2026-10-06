using NetArcaWs.HealthChecks;

namespace NetArcaWs.Invoicing;

public enum InvoiceState { Prepared, Submitting, Unknown, Reconciling, Authorized, Rejected, Conflict, ManualReview }

public sealed record InvoiceIdentity(ArcaEnvironment Environment, long Cuit, int PointOfSale, int VoucherType, long VoucherNumber);

/// <summary>Frozen fiscal data. Payload must not contain WSAA credentials. Use service adapters to construct it.</summary>
/// <param name="RemoteRequestId">Optional service-level request ID (for example WSFEX Id), unique per environment, CUIT and service. A rejected revision must retain it.</param>
public sealed record InvoiceSubmission(string TenantId, string IdempotencyKey, string Service, InvoiceIdentity Identity, string Payload,
    int CanonicalVersion = 1, long? RemoteRequestId = null)
{
    public override string ToString() => "Invoice submission (fiscal data hidden)";
}

public sealed record InvoiceOperation(InvoiceSubmission Submission, InvoiceState State, long Version, int Attempt,
    string? AuthorizationCode = null, string? ResponseXml = null)
{
    public override string ToString() => $"Invoice operation {State} (fiscal data hidden)";
}

public sealed record InvoiceLease(InvoiceOperation Operation, long Version);
public sealed record InvoiceDecision(InvoiceState State, string? AuthorizationCode = null, string? ResponseXml = null)
{
    public override string ToString() => $"Invoice decision {State} (fiscal data hidden)";
}

public sealed class InvoiceConflictException(string message) : InvalidOperationException(message);

/// <summary>Atomic durable operations; providers must enforce fiscal uniqueness across tenants and services.</summary>
public interface IInvoiceJournal
{
    Task<InvoiceOperation> PrepareAsync(InvoiceSubmission submission, CancellationToken cancellationToken = default);
    /// <summary>Replaces a definitively rejected payload under the same immutable fiscal identity and idempotency key.</summary>
    Task<InvoiceOperation> ReviseRejectedAsync(InvoiceSubmission replacement, long expectedVersion, CancellationToken cancellationToken = default);
    /// <summary>Returns immutable prior submissions for audit, oldest first.</summary>
    Task<IReadOnlyList<InvoiceOperation>> ListRevisionsAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<InvoiceOperation?> FindAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InvoiceOperation>> ListPendingAsync(string tenantId, int limit = 100, CancellationToken cancellationToken = default);
    Task<InvoiceLease?> TryAcquireAsync(string tenantId, string idempotencyKey, TimeSpan leaseDuration, bool reconciliation,
        CancellationToken cancellationToken = default);
    Task<InvoiceOperation> CompleteAsync(InvoiceLease lease, InvoiceDecision decision, CancellationToken cancellationToken = default);
}
