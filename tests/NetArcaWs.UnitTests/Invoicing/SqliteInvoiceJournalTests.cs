using FluentAssertions;
using Microsoft.Data.Sqlite;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Tests.TestSupport;
using Xunit;

namespace NetArcaWs.Tests.Invoicing;

public sealed class SqliteInvoiceJournalTests
{
    [Fact]
    public async Task PrepareAsync_is_durable_across_journal_instances_and_identical_replays_are_idempotent()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        string path = InvoiceTestData.NewDatabasePath(database, "durable");
        InvoiceSubmission submission = InvoiceTestData.Submission(payload: "<invoice><total>250.00</total></invoice>");
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        var firstJournal = new SqliteInvoiceJournal(path, clock);

        InvoiceOperation first = await firstJournal.PrepareAsync(submission, cancellationToken);
        var reopenedJournal = new SqliteInvoiceJournal(path, clock);
        InvoiceOperation replay = await reopenedJournal.PrepareAsync(submission, cancellationToken);
        InvoiceOperation? restored = await new SqliteInvoiceJournal(path, clock)
            .FindAsync(submission.TenantId, submission.IdempotencyKey, cancellationToken);

        first.State.Should().Be(InvoiceState.Prepared);
        replay.Submission.Should().BeEquivalentTo(submission);
        restored.Should().NotBeNull();
        restored!.Submission.Should().BeEquivalentTo(submission);
        restored.State.Should().Be(InvoiceState.Prepared);
    }

    [Fact]
    public async Task FindAsync_rejects_a_payload_that_was_modified_outside_the_journal()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        string path = InvoiceTestData.NewDatabasePath(database, "payload-integrity");
        var journal = new SqliteInvoiceJournal(path);
        InvoiceSubmission submission = InvoiceTestData.Submission(payload: "<invoice><total>250.00</total></invoice>");
        await journal.PrepareAsync(submission, cancellationToken);

        using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE invoices SET payload=$payload WHERE tenant=$tenant AND idem=$idem";
            command.Parameters.AddWithValue("$payload", "<invoice><total>0.01</total></invoice>");
            command.Parameters.AddWithValue("$tenant", submission.TenantId);
            command.Parameters.AddWithValue("$idem", submission.IdempotencyKey);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        Func<Task> readTamperedPayload = () => journal.FindAsync(
            submission.TenantId, submission.IdempotencyKey, cancellationToken);

        await readTamperedPayload.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*integrity check*");
    }

    [Fact]
    public async Task PrepareAsync_rejects_idempotency_key_reuse_with_different_submission_and_fiscal_duplicates_across_tenants()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "conflicts"));
        InvoiceSubmission original = InvoiceTestData.Submission();
        await journal.PrepareAsync(original, cancellationToken);
        InvoiceSubmission changedPayload = original with { Payload = "<invoice><total>122.00</total></invoice>" };
        InvoiceSubmission changedService = original with { Service = "wslocal" };
        InvoiceSubmission anotherTenantSameFiscalKey = original with { TenantId = "tenant-b", IdempotencyKey = "different-key" };

        Func<Task> payloadConflict = () => journal.PrepareAsync(changedPayload, cancellationToken);
        Func<Task> serviceConflict = () => journal.PrepareAsync(changedService, cancellationToken);
        Func<Task> fiscalConflict = () => journal.PrepareAsync(anotherTenantSameFiscalKey, cancellationToken);

        await payloadConflict.Should().ThrowAsync<InvoiceConflictException>();
        await serviceConflict.Should().ThrowAsync<InvoiceConflictException>();
        await fiscalConflict.Should().ThrowAsync<InvoiceConflictException>();
    }

    [Fact]
    public async Task PrepareAsync_rejects_canonical_version_changes_for_an_existing_idempotency_key()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "canonical-version"));
        InvoiceSubmission submission = InvoiceTestData.Submission();
        await journal.PrepareAsync(submission, cancellationToken);
        InvoiceSubmission differentCanonicalVersion = submission with { CanonicalVersion = 2 };

        Func<Task> conflict = () => journal.PrepareAsync(differentCanonicalVersion, cancellationToken);

        await conflict.Should().ThrowAsync<InvoiceConflictException>();
    }

    [Fact]
    public async Task PrepareAsync_reserves_service_remote_request_ids_per_environment_and_cuit()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "remote-id"));
        InvoiceSubmission original = InvoiceTestData.Submission(service: "wsfex") with { RemoteRequestId = 76543 };
        await journal.PrepareAsync(original, cancellationToken);

        InvoiceSubmission duplicateRemoteId = original with
        {
            TenantId = "tenant-b",
            IdempotencyKey = "another-key",
            Identity = original.Identity with { VoucherNumber = original.Identity.VoucherNumber + 1 }
        };
        InvoiceSubmission differentEnvironment = duplicateRemoteId with
        {
            IdempotencyKey = "different-environment",
            Identity = duplicateRemoteId.Identity with { Environment = ArcaEnvironment.Production }
        };
        InvoiceSubmission differentCuit = duplicateRemoteId with
        {
            IdempotencyKey = "different-cuit",
            Identity = duplicateRemoteId.Identity with { Cuit = duplicateRemoteId.Identity.Cuit + 1 }
        };

        Func<Task> duplicate = () => journal.PrepareAsync(duplicateRemoteId, cancellationToken);
        await duplicate.Should().ThrowAsync<InvoiceConflictException>();
        (await journal.PrepareAsync(differentEnvironment, cancellationToken)).State.Should().Be(InvoiceState.Prepared);
        (await journal.PrepareAsync(differentCuit, cancellationToken)).State.Should().Be(InvoiceState.Prepared);
    }

    [Fact]
    public async Task ReviseRejectedAsync_preserves_an_audit_snapshot_and_requires_the_current_version_and_identity()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "revisions"));
        InvoiceSubmission original = InvoiceTestData.Submission(service: "wsfex") with { RemoteRequestId = 90412 };
        await journal.PrepareAsync(original, cancellationToken);
        InvoiceLease lease = (await journal.TryAcquireAsync(
            original.TenantId, original.IdempotencyKey, TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        InvoiceOperation rejected = await journal.CompleteAsync(
            lease, new InvoiceDecision(InvoiceState.Rejected, ResponseXml: "<FEXResult />"), cancellationToken);
        InvoiceSubmission corrected = original with { Payload = "<invoice><total>120.00</total></invoice>" };

        Func<Task> staleVersion = () => journal.ReviseRejectedAsync(corrected, rejected.Version - 1, cancellationToken);
        await staleVersion.Should().ThrowAsync<InvoiceConflictException>();

        InvoiceOperation revised = await journal.ReviseRejectedAsync(corrected, rejected.Version, cancellationToken);
        IReadOnlyList<InvoiceOperation> history = await journal.ListRevisionsAsync(
            original.TenantId, original.IdempotencyKey, cancellationToken);
        InvoiceOperation? current = await journal.FindAsync(original.TenantId, original.IdempotencyKey, cancellationToken);

        revised.State.Should().Be(InvoiceState.Prepared);
        revised.Version.Should().Be(rejected.Version + 1);
        revised.Submission.RemoteRequestId.Should().Be(original.RemoteRequestId);
        current!.Submission.Payload.Should().Be(corrected.Payload);
        history.Should().ContainSingle();
        history[0].State.Should().Be(InvoiceState.Rejected);
        history[0].Submission.Payload.Should().Be(original.Payload);
        history[0].AuthorizationCode.Should().BeNull();
        history[0].ResponseXml.Should().Be("<FEXResult />");
    }

    [Fact]
    public async Task PrepareAsync_blocks_later_vouchers_in_a_series_until_authorization_and_allows_other_series()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "series-guard"));
        InvoiceSubmission first = InvoiceTestData.Submission(identity: InvoiceTestData.Identity(voucherNumber: 1));
        await journal.PrepareAsync(first, cancellationToken);
        InvoiceLease dispatchLease = (await journal.TryAcquireAsync(
            first.TenantId, first.IdempotencyKey, TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        await journal.CompleteAsync(dispatchLease, new InvoiceDecision(InvoiceState.Unknown), cancellationToken);

        InvoiceSubmission nextSameSeries = first with
        {
            IdempotencyKey = "invoice-002",
            Identity = first.Identity with { VoucherNumber = 2 }
        };
        InvoiceSubmission differentSeries = first with
        {
            IdempotencyKey = "invoice-other-series",
            Identity = first.Identity with { VoucherType = 6, VoucherNumber = 1 }
        };
        Func<Task> blockedNext = () => journal.PrepareAsync(nextSameSeries, cancellationToken);

        await blockedNext.Should().ThrowAsync<InvoiceConflictException>();
        (await journal.PrepareAsync(differentSeries, cancellationToken)).State.Should().Be(InvoiceState.Prepared);

        InvoiceLease reconciliation = (await journal.TryAcquireAsync(
            first.TenantId, first.IdempotencyKey, TimeSpan.FromMinutes(1), reconciliation: true, cancellationToken))!;
        await journal.CompleteAsync(reconciliation, new InvoiceDecision(InvoiceState.Authorized, "123456"), cancellationToken);

        (await journal.PrepareAsync(nextSameSeries, cancellationToken)).State.Should().Be(InvoiceState.Prepared);
    }

    [Fact]
    public async Task Concurrent_journal_instances_allow_only_one_active_submission_claim()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        string path = InvoiceTestData.NewDatabasePath(database, "claim");
        var first = new SqliteInvoiceJournal(path);
        var second = new SqliteInvoiceJournal(path);
        InvoiceSubmission submission = InvoiceTestData.Submission();
        await first.PrepareAsync(submission, cancellationToken);

        InvoiceLease?[] leases = await Task.WhenAll(
            first.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey, TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken),
            second.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey, TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken));

        leases.Count(lease => lease is not null).Should().Be(1);
        leases.Where(lease => lease is not null).Select(lease => lease!.Operation.State)
            .Should().ContainSingle().Which.Should().Be(InvoiceState.Submitting);
    }

    [Fact]
    public async Task Expired_submission_lease_requires_reconciliation_and_fences_stale_completion()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        string path = InvoiceTestData.NewDatabasePath(database, "fence");
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        var journal = new SqliteInvoiceJournal(path, clock);
        InvoiceSubmission submission = InvoiceTestData.Submission();
        await journal.PrepareAsync(submission, cancellationToken);
        InvoiceLease firstLease = (await journal.TryAcquireAsync(
            submission.TenantId, submission.IdempotencyKey, TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        clock.Advance(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(1)));

        InvoiceLease? resubmit = await journal.TryAcquireAsync(
            submission.TenantId, submission.IdempotencyKey, TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken);
        InvoiceLease? reconciliation = await journal.TryAcquireAsync(
            submission.TenantId, submission.IdempotencyKey, TimeSpan.FromMinutes(1), reconciliation: true, cancellationToken);

        resubmit.Should().BeNull("an expired dispatch may have reached ARCA and must never be resubmitted");
        reconciliation.Should().NotBeNull();
        reconciliation!.Operation.State.Should().Be(InvoiceState.Reconciling);
        Func<Task> staleComplete = () => journal.CompleteAsync(
            firstLease, new InvoiceDecision(InvoiceState.Authorized, AuthorizationCode: "123456"), cancellationToken);
        await staleComplete.Should().ThrowAsync<InvalidOperationException>();

        InvoiceOperation result = await journal.CompleteAsync(
            reconciliation, new InvoiceDecision(InvoiceState.Rejected, ResponseXml: "<Errors />"), cancellationToken);
        result.State.Should().Be(InvoiceState.Rejected);
    }

    [Fact]
    public async Task Authorized_completion_requires_a_numeric_authorization_code()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "authorization"));
        InvoiceSubmission submission = InvoiceTestData.Submission();
        await journal.PrepareAsync(submission, cancellationToken);
        InvoiceLease lease = (await journal.TryAcquireAsync(
            submission.TenantId, submission.IdempotencyKey, TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;

        Func<Task> emptyCode = () => journal.CompleteAsync(
            lease, new InvoiceDecision(InvoiceState.Authorized), cancellationToken);
        Func<Task> nonnumericCode = () => journal.CompleteAsync(
            lease, new InvoiceDecision(InvoiceState.Authorized, AuthorizationCode: "A123"), cancellationToken);

        await emptyCode.Should().ThrowAsync<ArgumentException>();
        await nonnumericCode.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ListPendingAsync_returns_recoverable_entries_for_the_requested_tenant_in_insertion_order()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        string path = InvoiceTestData.NewDatabasePath(database, "pending-list");
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        var journal = new SqliteInvoiceJournal(path, clock);

        await SaveState(InvoiceTestData.Submission(tenantId: "tenant-list", idempotencyKey: "manual", identity: WithTypeAndNumber(1, 10)),
            InvoiceState.ManualReview, leaseDuration: TimeSpan.FromMinutes(1));
        await SaveState(InvoiceTestData.Submission(tenantId: "tenant-list", idempotencyKey: "unknown", identity: WithTypeAndNumber(2, 20)),
            InvoiceState.Unknown, leaseDuration: TimeSpan.FromMinutes(1));
        await SaveState(InvoiceTestData.Submission(tenantId: "tenant-list", idempotencyKey: "conflict", identity: WithTypeAndNumber(3, 30)),
            InvoiceState.Conflict, leaseDuration: TimeSpan.FromMinutes(1));

        InvoiceSubmission expiredSubmitting = InvoiceTestData.Submission(
            tenantId: "tenant-list", idempotencyKey: "expired-submitting", identity: WithTypeAndNumber(4, 40));
        await journal.PrepareAsync(expiredSubmitting, cancellationToken);
        await journal.TryAcquireAsync(expiredSubmitting.TenantId, expiredSubmitting.IdempotencyKey,
            TimeSpan.FromSeconds(1), reconciliation: false, cancellationToken);

        InvoiceSubmission expiredReconciling = InvoiceTestData.Submission(
            tenantId: "tenant-list", idempotencyKey: "expired-reconciling", identity: WithTypeAndNumber(5, 50));
        await journal.PrepareAsync(expiredReconciling, cancellationToken);
        InvoiceLease reconLease = (await journal.TryAcquireAsync(expiredReconciling.TenantId, expiredReconciling.IdempotencyKey,
            TimeSpan.FromSeconds(1), reconciliation: false, cancellationToken))!;
        await journal.CompleteAsync(reconLease, new InvoiceDecision(InvoiceState.Unknown), cancellationToken);
        reconLease = (await journal.TryAcquireAsync(expiredReconciling.TenantId, expiredReconciling.IdempotencyKey,
            TimeSpan.FromSeconds(1), reconciliation: true, cancellationToken))!;
        reconLease.Operation.State.Should().Be(InvoiceState.Reconciling);

        InvoiceSubmission prepared = InvoiceTestData.Submission(
            tenantId: "tenant-list", idempotencyKey: "prepared", identity: WithTypeAndNumber(6, 5));
        await journal.PrepareAsync(prepared, cancellationToken);
        InvoiceSubmission anotherTenant = InvoiceTestData.Submission(
            tenantId: "tenant-other", idempotencyKey: "other", identity: InvoiceTestData.Identity(cuit: 20999888777, voucherNumber: 60));
        await SaveState(anotherTenant, InvoiceState.Unknown, leaseDuration: TimeSpan.FromMinutes(1));

        clock.Advance(TimeSpan.FromSeconds(2));
        IReadOnlyList<InvoiceOperation> pending = await journal.ListPendingAsync("tenant-list", limit: 100, cancellationToken);
        IReadOnlyList<InvoiceOperation> limited = await journal.ListPendingAsync("tenant-list", limit: 2, cancellationToken);
        IReadOnlyList<InvoiceOperation> otherTenantPending = await journal.ListPendingAsync("tenant-other", limit: 100, cancellationToken);

        pending.Select(operation => operation.Submission.IdempotencyKey).Should().Equal(
            "manual", "unknown", "conflict", "expired-submitting", "expired-reconciling", "prepared");
        limited.Select(operation => operation.Submission.Identity.VoucherNumber)
            .Should().Equal(10, 20);
        otherTenantPending.Select(operation => operation.Submission.IdempotencyKey)
            .Should().ContainSingle().Which.Should().Be("other");

        async Task SaveState(InvoiceSubmission submission, InvoiceState state, TimeSpan leaseDuration)
        {
            await journal.PrepareAsync(submission, cancellationToken);
            InvoiceLease lease = (await journal.TryAcquireAsync(
                submission.TenantId, submission.IdempotencyKey, leaseDuration, reconciliation: false, cancellationToken))!;
            await journal.CompleteAsync(lease,
                new InvoiceDecision(state, state == InvoiceState.Authorized ? "123456" : null), cancellationToken);
        }

        InvoiceIdentity WithTypeAndNumber(int type, long number) =>
            InvoiceTestData.Identity(voucherType: type, voucherNumber: number);
    }
}
