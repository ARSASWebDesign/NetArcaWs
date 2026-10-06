using AwesomeAssertions;
using NetArcaWs.Invoicing;
using NetArcaWs.Tests.TestSupport;
using Xunit;

namespace NetArcaWs.Tests.Invoicing;

public sealed class InvoiceCoordinatorTests
{
    [Fact]
    public async Task SubmitAsync_dispatches_only_once_and_duplicates_return_durable_state()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "submit-once"));
        var coordinator = new InvoiceCoordinator(journal);
        InvoiceSubmission submission = InvoiceTestData.Submission();
        int dispatchCount = 0;
        Task<InvoiceDecision> Dispatch(CancellationToken _)
        {
            Interlocked.Increment(ref dispatchCount);
            return Task.FromResult(new InvoiceDecision(InvoiceState.Authorized, "123456", "<Authorized />"));
        }

        InvoiceOperation first = await coordinator.SubmitAsync(submission, Dispatch, cancellationToken);
        InvoiceOperation replay = await coordinator.SubmitAsync(submission, Dispatch, cancellationToken);

        first.State.Should().Be(InvoiceState.Authorized);
        first.AuthorizationCode.Should().Be("123456");
        replay.State.Should().Be(InvoiceState.Authorized);
        replay.ResponseXml.Should().Be("<Authorized />");
        dispatchCount.Should().Be(1);
    }

    [Fact]
    public async Task SubmitAsync_persists_unknown_after_dispatch_exception_and_never_resends_duplicate()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "dispatch-failure"));
        var coordinator = new InvoiceCoordinator(journal);
        InvoiceSubmission submission = InvoiceTestData.Submission();
        int dispatchCount = 0;
        Task<InvoiceDecision> DispatchFails(CancellationToken _)
        {
            Interlocked.Increment(ref dispatchCount);
            throw new HttpRequestException("connection reset after dispatch");
        }

        Func<Task> first = () => coordinator.SubmitAsync(submission, DispatchFails, cancellationToken);
        await first.Should().ThrowAsync<HttpRequestException>();
        InvoiceOperation? persisted = await journal.FindAsync(submission.TenantId, submission.IdempotencyKey, cancellationToken);
        InvoiceOperation replay = await coordinator.SubmitAsync(
            submission,
            _ => { Interlocked.Increment(ref dispatchCount); return Task.FromResult(new InvoiceDecision(InvoiceState.Authorized, "654321")); },
            cancellationToken);

        persisted.Should().NotBeNull();
        persisted!.State.Should().Be(InvoiceState.Unknown);
        replay.State.Should().Be(InvoiceState.Unknown);
        dispatchCount.Should().Be(1, "an uncertain dispatch must be reconciled, never retried");
    }

    [Fact]
    public async Task SubmitAsync_persists_unknown_when_cancelled_after_dispatch_and_reconcile_can_resolve_it()
    {
        using var database = new TemporaryDatabase();
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "cancelled-dispatch"), clock);
        var coordinator = new InvoiceCoordinator(journal, TimeSpan.FromSeconds(1));
        InvoiceSubmission submission = InvoiceTestData.Submission();
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        int sendCount = 0;
        async Task<InvoiceDecision> Dispatch(CancellationToken token)
        {
            Interlocked.Increment(ref sendCount);
            dispatched.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return new InvoiceDecision(InvoiceState.Authorized, "123456");
        }

        Task<InvoiceOperation> pending = coordinator.SubmitAsync(submission, Dispatch, cancellation.Token);
        await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        cancellation.Cancel();
        Func<Task> cancelledDispatch = async () => { await pending; };
        await cancelledDispatch.Should().ThrowAsync<OperationCanceledException>();

        InvoiceOperation? persisted = await journal.FindAsync(
            submission.TenantId, submission.IdempotencyKey, TestContext.Current.CancellationToken);
        persisted.Should().NotBeNull();
        persisted!.State.Should().Be(InvoiceState.Unknown);
        clock.Advance(TimeSpan.FromSeconds(2));

        InvoiceOperation reconciled = await coordinator.ReconcileAsync(
            submission.TenantId,
            submission.IdempotencyKey,
            (operation, _) =>
            {
                operation.State.Should().Be(InvoiceState.Reconciling);
                return Task.FromResult(new InvoiceDecision(InvoiceState.Authorized, "789012", "<QueryResult />"));
            },
            TestContext.Current.CancellationToken);

        reconciled.State.Should().Be(InvoiceState.Authorized);
        reconciled.AuthorizationCode.Should().Be("789012");
        sendCount.Should().Be(1);
    }

    [Fact]
    public async Task ReconcileAsync_query_failure_preserves_unknown_for_later_reconciliation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "query-failure"), clock);
        var coordinator = new InvoiceCoordinator(journal, TimeSpan.FromSeconds(1));
        InvoiceSubmission submission = InvoiceTestData.Submission();
        Task<InvoiceDecision> UnknownDispatch(CancellationToken _) =>
            throw new HttpRequestException("transport interrupted");
        Func<Task> submit = () => coordinator.SubmitAsync(submission, UnknownDispatch, cancellationToken);
        await submit.Should().ThrowAsync<HttpRequestException>();

        Func<Task> queryFailure = () => coordinator.ReconcileAsync(
            submission.TenantId,
            submission.IdempotencyKey,
            (_, _) => throw new HttpRequestException("status query failed"),
            cancellationToken);
        await queryFailure.Should().ThrowAsync<HttpRequestException>();
        (await journal.FindAsync(submission.TenantId, submission.IdempotencyKey, cancellationToken))!
            .State.Should().Be(InvoiceState.Unknown);
        clock.Advance(TimeSpan.FromSeconds(2));

        InvoiceOperation recovered = await coordinator.ReconcileAsync(
            submission.TenantId,
            submission.IdempotencyKey,
            (_, _) => Task.FromResult(new InvoiceDecision(InvoiceState.Rejected, ResponseXml: "<Rejected />")),
            cancellationToken);

        recovered.State.Should().Be(InvoiceState.Rejected);
        recovered.ResponseXml.Should().Be("<Rejected />");
    }
}
