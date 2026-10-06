namespace NetArcaWs.Invoicing;

/// <summary>Durably sends a prepared operation once. Unknown outcomes can only enter reconciliation.</summary>
public sealed class InvoiceCoordinator(IInvoiceJournal journal, TimeSpan? leaseDuration = null)
{
    private readonly TimeSpan leaseDuration = leaseDuration ?? TimeSpan.FromMinutes(2);

    public Task<InvoiceOperation?> FindAsync(string tenantId, string key, CancellationToken cancellationToken = default)
        => journal.FindAsync(tenantId, key, cancellationToken);

    public Task<InvoiceOperation> ReviseRejectedAsync(InvoiceSubmission replacement, long expectedVersion,
        CancellationToken cancellationToken = default)
        => journal.ReviseRejectedAsync(replacement, expectedVersion, cancellationToken);

    public async Task<InvoiceOperation> SubmitAsync(InvoiceSubmission submission,
        Func<CancellationToken, Task<InvoiceDecision>> submit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(submit);
        var operation = await journal.PrepareAsync(submission, cancellationToken).ConfigureAwait(false);
        var lease = await journal.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey, leaseDuration, false, cancellationToken).ConfigureAwait(false);
        if (lease is null) return await journal.FindAsync(submission.TenantId, submission.IdempotencyKey, cancellationToken).ConfigureAwait(false) ?? operation;
        return await ExecuteAsync(lease, submit, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InvoiceOperation> ReconcileAsync(string tenantId, string idempotencyKey,
        Func<InvoiceOperation, CancellationToken, Task<InvoiceDecision>> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var operation = await journal.FindAsync(tenantId, idempotencyKey, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The invoice operation does not exist.");
        var lease = await journal.TryAcquireAsync(tenantId, idempotencyKey, leaseDuration, true, cancellationToken).ConfigureAwait(false);
        if (lease is null) return await journal.FindAsync(tenantId, idempotencyKey, cancellationToken).ConfigureAwait(false) ?? operation;
        return await ExecuteAsync(lease, token => query(lease.Operation, token), cancellationToken).ConfigureAwait(false);
    }

    private async Task<InvoiceOperation> ExecuteAsync(InvoiceLease lease, Func<CancellationToken, Task<InvoiceDecision>> action, CancellationToken cancellationToken)
    {
        InvoiceDecision? decision = null;
        try
        {
            decision = await action(cancellationToken).ConfigureAwait(false);
            // Preserve a received fiscal result even if the initiating HTTP request was cancelled.
            return await journal.CompleteAsync(lease, decision, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // A local persistence failure after a remote response must remain uncertain,
            // while retaining that response as reconciliation evidence.
            try { await journal.CompleteAsync(lease, new(InvoiceState.Unknown, ResponseXml: decision?.ResponseXml), CancellationToken.None).ConfigureAwait(false); }
            catch { /* Keep the original failure; recovery can use the durable lease and query later. */ }
            throw;
        }
    }
}
