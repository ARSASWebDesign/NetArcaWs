using Microsoft.Extensions.Logging;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;

namespace NetArcaWs.EntityFrameworkCore;

internal static class InvoiceRecoveryOptions
{
    internal static InvoiceRecoveryWorkerOptions Create(Action<InvoiceRecoveryWorkerOptionsBuilder>? configure)
    {
        var builder = new InvoiceRecoveryWorkerOptionsBuilder();
        configure?.Invoke(builder);
        if (builder.LeaseDuration <= TimeSpan.Zero || builder.LeaseDuration > TimeSpan.FromMinutes(30))
            throw new ArgumentOutOfRangeException(nameof(builder.LeaseDuration));
        if (builder.IdleInterval <= TimeSpan.Zero || builder.IdleInterval > TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(nameof(builder.IdleInterval));
        if (builder.MaxAttempts is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(builder.MaxAttempts));
        if (builder.MaxBackoff <= TimeSpan.Zero || builder.MaxBackoff > TimeSpan.FromDays(1))
            throw new ArgumentOutOfRangeException(nameof(builder.MaxBackoff));
        if (builder.BaseBackoff <= TimeSpan.Zero || builder.BaseBackoff > builder.MaxBackoff)
            throw new ArgumentOutOfRangeException(nameof(builder.BaseBackoff));
        if (!double.IsFinite(builder.JitterRatio) || builder.JitterRatio is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(builder.JitterRatio));
        return new(builder.LeaseDuration, builder.IdleInterval, builder.MaxAttempts,
            builder.BaseBackoff, builder.MaxBackoff, builder.JitterRatio);
    }
}

internal sealed class InvoiceRecoveryProcessor(IInvoiceRecoveryQueue queue,
    IInvoiceRecoveryContextResolver resolver, SafeInvoiceService invoices,
    InvoiceRecoveryWorkerOptions options, TimeProvider timeProvider,
    ILogger<InvoiceRecoveryProcessor> logger) : IInvoiceRecoveryProcessor
{
    public async Task<bool> RunOnceAsync(InvoiceRecoveryScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        InvoiceRecoveryLease? lease = await queue.TryClaimAsync(scope, options.LeaseDuration, cancellationToken).ConfigureAwait(false);
        if (lease is null) return false;
        if (!await queue.IsCurrentAsync(lease, cancellationToken).ConfigureAwait(false)) return true;

        ArcaTenantContext tenant;
        try
        {
            tenant = await resolver.ResolveAsync(lease.WorkItem, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await RescheduleWithPolicyAsync(lease, InvoiceRecoverySafeReason.None, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch
        {
            await FinishIfCurrentAsync(lease, InvoiceRecoveryDisposition.Suspend,
                InvoiceRecoverySafeReason.CredentialUnavailable, cancellationToken).ConfigureAwait(false);
            return true;
        }

        if (!ContextMatches(tenant, lease.WorkItem) || !scope.Services.Contains(lease.WorkItem.Service))
        {
            await FinishIfCurrentAsync(lease, InvoiceRecoveryDisposition.Suspend,
                InvoiceRecoverySafeReason.AuthorizationDenied, cancellationToken).ConfigureAwait(false);
            return true;
        }

        if (!await queue.IsCurrentAsync(lease, cancellationToken).ConfigureAwait(false)) return true;
        if (cancellationToken.IsCancellationRequested)
        {
            await RescheduleWithPolicyAsync(lease, InvoiceRecoverySafeReason.None, CancellationToken.None).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        try
        {
            InvoiceOperation result = lease.CurrentOperation.State switch
            {
                InvoiceState.Prepared => await invoices.ResumeAsync(tenant, lease.WorkItem.IdempotencyKey, cancellationToken).ConfigureAwait(false),
                InvoiceState.Unknown or InvoiceState.Submitting or InvoiceState.Reconciling =>
                    await invoices.ReconcileAsync(tenant, lease.WorkItem.IdempotencyKey, cancellationToken).ConfigureAwait(false),
                _ => lease.CurrentOperation
            };
            await CompleteResultAsync(lease, result, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A submission may have reached ARCA. The next claim must reconcile the same identity.
            await RescheduleWithPolicyAsync(lease, InvoiceRecoverySafeReason.QueryUnavailable, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch
        {
            await RescheduleWithPolicyAsync(lease, InvoiceRecoverySafeReason.QueryUnavailable, cancellationToken).ConfigureAwait(false);
        }
        return true;
    }

    private async Task CompleteResultAsync(InvoiceRecoveryLease lease, InvoiceOperation result, CancellationToken token)
    {
        switch (result.State)
        {
            case InvoiceState.Authorized:
            case InvoiceState.Rejected:
                await FinishIfCurrentAsync(lease, InvoiceRecoveryDisposition.Complete, InvoiceRecoverySafeReason.None, token).ConfigureAwait(false);
                break;
            case InvoiceState.Conflict:
            case InvoiceState.ManualReview:
                await FinishIfCurrentAsync(lease, InvoiceRecoveryDisposition.ManualReview,
                    InvoiceRecoverySafeReason.PersistenceConflict, token).ConfigureAwait(false);
                break;
            default:
                await RescheduleWithPolicyAsync(lease, InvoiceRecoverySafeReason.QueryEmpty, token).ConfigureAwait(false);
                break;
        }
    }

    private async Task RescheduleWithPolicyAsync(InvoiceRecoveryLease lease, InvoiceRecoverySafeReason reason, CancellationToken token)
    {
        if (!await queue.IsCurrentAsync(lease, token).ConfigureAwait(false)) return;
        if (lease.WorkItem.Attempt >= options.MaxAttempts)
        {
            await FinishIfCurrentAsync(lease, InvoiceRecoveryDisposition.Suspend, InvoiceRecoverySafeReason.MaxAttemptsReached, token).ConfigureAwait(false);
            return;
        }
        TimeSpan delay = Backoff(lease.WorkItem.Attempt + 1);
        await queue.CompleteAsync(lease, InvoiceRecoveryDisposition.Reschedule, reason,
            timeProvider.GetUtcNow() + delay, token).ConfigureAwait(false);
        SafeLog(reason, InvoiceRecoveryDisposition.Reschedule, lease.WorkItem.Attempt);
    }

    private async Task FinishIfCurrentAsync(InvoiceRecoveryLease lease, InvoiceRecoveryDisposition disposition,
        InvoiceRecoverySafeReason reason, CancellationToken token)
    {
        if (!await queue.IsCurrentAsync(lease, token).ConfigureAwait(false)) return;
        await queue.CompleteAsync(lease, disposition, reason, cancellationToken: token).ConfigureAwait(false);
        SafeLog(reason, disposition, lease.WorkItem.Attempt);
    }

    private TimeSpan Backoff(int attempt)
    {
        double factor = Math.Pow(2, Math.Min(attempt - 1, 30));
        double ticks = Math.Min(options.MaxBackoff.Ticks, options.BaseBackoff.Ticks * factor);
        double multiplier = 1 + ((Random.Shared.NextDouble() * 2 - 1) * options.JitterRatio);
        return TimeSpan.FromTicks((long)Math.Clamp(ticks * multiplier, 1, options.MaxBackoff.Ticks));
    }

    private static bool ContextMatches(ArcaTenantContext? context, InvoiceRecoveryWorkItem item) => context is not null &&
        context.TenantId == item.TenantId && context.Cuit == item.Identity.Cuit && context.Environment == item.Identity.Environment;

    private void SafeLog(InvoiceRecoverySafeReason reason, InvoiceRecoveryDisposition disposition, int attempt) =>
        logger.LogInformation("Invoice recovery moved to {Disposition}; reason {Reason}; attempt {Attempt}.", disposition, reason, attempt);
}
