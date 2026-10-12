using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Tests.TestSupport;
using System.Globalization;
using System.Xml.Linq;
using System.Xml.Serialization;
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
    public async Task Same_idempotency_key_is_scoped_to_tenant_and_restores_each_tenants_own_submission()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "tenant-idempotency"));
        InvoiceSubmission tenantA = InvoiceTestData.Submission(
            tenantId: "tenant-a", idempotencyKey: "invoice-001");
        InvoiceSubmission tenantB = InvoiceTestData.Submission(
            tenantId: "tenant-b",
            idempotencyKey: "invoice-001",
            identity: InvoiceTestData.Identity(cuit: 20999888777),
            payload: "<invoice><total>242.00</total></invoice>");

        InvoiceOperation preparedA = await journal.PrepareAsync(tenantA, cancellationToken);
        InvoiceOperation preparedB = await journal.PrepareAsync(tenantB, cancellationToken);
        InvoiceOperation? restoredA = await journal.FindAsync("tenant-a", "invoice-001", cancellationToken);
        InvoiceOperation? restoredB = await journal.FindAsync("tenant-b", "invoice-001", cancellationToken);

        preparedA.Submission.Should().BeEquivalentTo(tenantA);
        preparedB.Submission.Should().BeEquivalentTo(tenantB);
        restoredA.Should().NotBeNull();
        restoredA!.Submission.Should().BeEquivalentTo(tenantA);
        restoredB.Should().NotBeNull();
        restoredB!.Submission.Should().BeEquivalentTo(tenantB);
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

    [Theory]
    [InlineData(InvoiceState.Unknown)]
    [InlineData(InvoiceState.Conflict)]
    [InlineData(InvoiceState.ManualReview)]
    public async Task AdjudicateWsfexValidationRejectionAsync_archives_prior_state_and_persists_immutable_audit(InvoiceState priorState)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        string path = InvoiceTestData.NewDatabasePath(database, "rejection-adjudication");
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        var journal = new SqliteInvoiceJournal(path, clock);
        InvoiceSubmission original = CreateWsfexSubmission(76543);
        string priorResponse = CreateNoResultQueryResponse();
        string originalRejection = Create1550Response();
        InvoiceOperation prior = await SaveInStateAsync(journal, original, priorState, priorResponse, cancellationToken);
        clock.Advance(TimeSpan.FromSeconds(3));

        var evidence = new InvoiceRejectionEvidence(original, originalRejection, "operator-1", "manual-review", "confirmed validation rejection");
        InvoiceOperation rejected = await ((IInvoiceRejectionAdjudicationJournal)journal)
            .AdjudicateWsfexValidationRejectionAsync(original.TenantId, original.IdempotencyKey, prior.Version, evidence, cancellationToken);
        IReadOnlyList<InvoiceOperation> revisions = await journal.ListRevisionsAsync(original.TenantId, original.IdempotencyKey, cancellationToken);
        IReadOnlyList<InvoiceRejectionAdjudication> audits = await ((IInvoiceRejectionAdjudicationJournal)journal)
            .ListRejectionAdjudicationsAsync(original.TenantId, original.IdempotencyKey, limit: 100, cancellationToken);

        rejected.State.Should().Be(InvoiceState.Rejected);
        rejected.Version.Should().Be(prior.Version + 1);
        rejected.Attempt.Should().Be(prior.Attempt);
        rejected.Submission.Should().BeEquivalentTo(original);
        rejected.AuthorizationCode.Should().BeNull();
        rejected.ResponseXml.Should().Be(originalRejection);
        revisions.Should().ContainSingle();
        revisions[0].Should().BeEquivalentTo(prior);
        audits.Should().ContainSingle();
        audits[0].TenantId.Should().Be(original.TenantId);
        audits[0].IdempotencyKey.Should().Be(original.IdempotencyKey);
        audits[0].PriorVersion.Should().Be(prior.Version);
        audits[0].RejectedVersion.Should().Be(rejected.Version);
        audits[0].OriginalResponseXml.Should().Be(originalRejection);
        audits[0].Operator.Should().Be("operator-1");
        audits[0].Source.Should().Be("manual-review");
        audits[0].Reason.Should().Be("confirmed validation rejection");
        audits[0].RecordedAtUtc.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(clock.GetUtcNow().ToUnixTimeMilliseconds()));
        audits[0].SnapshotHash.Should().MatchRegex("^[A-F0-9]{64}$");
        audits[0].OriginalResponseSha256.Should().Be(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(originalRejection))));

        var reopened = (IInvoiceRejectionAdjudicationJournal)new SqliteInvoiceJournal(path, clock);
        (await reopened.ListRejectionAdjudicationsAsync(original.TenantId, original.IdempotencyKey, limit: 100, cancellationToken))
            .Should().ContainSingle().Which.Should().BeEquivalentTo(audits[0]);

        InvoiceSubmission correction = original with { Payload = original.Payload.Replace("20261012", "20261013", StringComparison.Ordinal) };
        InvoiceOperation revised = await journal.ReviseRejectedAsync(correction, rejected.Version, cancellationToken);
        revised.State.Should().Be(InvoiceState.Prepared);
        revised.Submission.Identity.Should().Be(original.Identity);
        revised.Submission.RemoteRequestId.Should().Be(original.RemoteRequestId);
        (await journal.ListRevisionsAsync(original.TenantId, original.IdempotencyKey, cancellationToken))
            .Select(operation => operation.State).Should().Equal(priorState, InvoiceState.Rejected);
        (await reopened.ListRejectionAdjudicationsAsync(original.TenantId, original.IdempotencyKey, limit: 100, cancellationToken))
            .Should().ContainSingle();

        using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        using var mutation = connection.CreateCommand();
        mutation.CommandText = "UPDATE invoice_rejection_adjudications SET reason='tampered' WHERE tenant=$tenant AND idem=$idem";
        mutation.Parameters.AddWithValue("$tenant", original.TenantId);
        mutation.Parameters.AddWithValue("$idem", original.IdempotencyKey);
        Func<Task> updateAudit = async () => await mutation.ExecuteNonQueryAsync(cancellationToken);
        await updateAudit.Should().ThrowAsync<SqliteException>().WithMessage("*immutable*");
    }

    [Fact]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_stale_duplicate_calls_without_duplicate_audit()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "adjudication-duplicate"));
        InvoiceSubmission original = CreateWsfexSubmission(76544);
        InvoiceOperation prior = await SaveInStateAsync(journal, original, InvoiceState.Unknown, CreateNoResultQueryResponse(), cancellationToken);
        var evidence = new InvoiceRejectionEvidence(original, Create1550Response(), "operator-1", "manual-review", "reason");
        var capability = (IInvoiceRejectionAdjudicationJournal)journal;

        await capability.AdjudicateWsfexValidationRejectionAsync(original.TenantId, original.IdempotencyKey, prior.Version, evidence, cancellationToken);
        Func<Task> duplicate = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, evidence, cancellationToken);

        await duplicate.Should().ThrowAsync<InvoiceConflictException>();
        (await capability.ListRejectionAdjudicationsAsync(original.TenantId, original.IdempotencyKey, limit: 100, cancellationToken))
            .Should().ContainSingle();
    }

    [Fact]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_wrong_tenant_snapshot_and_active_lease()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        string path = InvoiceTestData.NewDatabasePath(database, "adjudication-guards");
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        var journal = new SqliteInvoiceJournal(path, clock);
        InvoiceSubmission original = CreateWsfexSubmission(76545);
        InvoiceOperation prior = await SaveInStateAsync(journal, original, InvoiceState.Unknown, CreateNoResultQueryResponse(), cancellationToken);
        var capability = (IInvoiceRejectionAdjudicationJournal)journal;
        var valid = new InvoiceRejectionEvidence(original, Create1550Response(), "operator-1", "manual-review", "reason");
        var wrongSnapshot = valid with { OriginalSubmission = original with { Payload = "<different />" } };
        var wrongErrorCode = valid with { OriginalResponseXml = CreateResponse(1020) };
        var responseWithAuthorization = valid with { OriginalResponseXml = CreateResponse(1550, includeAuthorization: true) };
        var partialResponse = valid with { OriginalResponseXml = CreatePartialResponse() };
        var wrongNamespace = valid with
        {
            OriginalResponseXml = Create1550Response().Replace("http://ar.gov.afip.dif.fexv1/", "urn:unexpected", StringComparison.Ordinal)
        };
        var controlCharacterMetadata = valid with { Operator = "operator\n1" };

        Func<Task> wrongTenant = () => capability.AdjudicateWsfexValidationRejectionAsync(
            "tenant-other", original.IdempotencyKey, prior.Version, valid, cancellationToken);
        Func<Task> mismatch = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, wrongSnapshot, cancellationToken);
        Func<Task> wrongCode = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, wrongErrorCode, cancellationToken);
        Func<Task> unauthorizedAuthorization = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, responseWithAuthorization, cancellationToken);
        Func<Task> partial = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, partialResponse, cancellationToken);
        Func<Task> wrongQName = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, wrongNamespace, cancellationToken);
        Func<Task> invalidMetadata = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, controlCharacterMetadata, cancellationToken);
        await wrongTenant.Should().ThrowAsync<KeyNotFoundException>();
        await mismatch.Should().ThrowAsync<InvoiceConflictException>();
        await wrongCode.Should().ThrowAsync<InvoiceConflictException>();
        await unauthorizedAuthorization.Should().ThrowAsync<InvoiceConflictException>();
        await partial.Should().ThrowAsync<InvoiceConflictException>();
        await wrongQName.Should().ThrowAsync<InvoiceConflictException>();
        await invalidMetadata.Should().ThrowAsync<ArgumentException>();
        (await journal.FindAsync(original.TenantId, original.IdempotencyKey, cancellationToken))!.State.Should().Be(InvoiceState.Unknown);

        using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE invoices SET lease_until=NULL,authorization='123456' WHERE tenant=$tenant AND idem=$idem";
            command.Parameters.AddWithValue("$tenant", original.TenantId);
            command.Parameters.AddWithValue("$idem", original.IdempotencyKey);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        Func<Task> authorizationPresent = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, valid, cancellationToken);
        await authorizationPresent.Should().ThrowAsync<InvoiceConflictException>();
        using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE invoices SET authorization=NULL,lease_until=$lease WHERE tenant=$tenant AND idem=$idem";
            command.Parameters.AddWithValue("$lease", clock.GetUtcNow().AddMinutes(5).ToUnixTimeMilliseconds());
            command.Parameters.AddWithValue("$tenant", original.TenantId);
            command.Parameters.AddWithValue("$idem", original.IdempotencyKey);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        Func<Task> activeLease = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, valid, cancellationToken);
        await activeLease.Should().ThrowAsync<InvoiceConflictException>();
        (await capability.ListRejectionAdjudicationsAsync(original.TenantId, original.IdempotencyKey, limit: 100, cancellationToken))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task AdjudicateWsfexValidationRejectionAsync_rolls_back_revision_and_audit_when_write_fails()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        string path = InvoiceTestData.NewDatabasePath(database, "adjudication-rollback");
        var journal = new SqliteInvoiceJournal(path);
        InvoiceSubmission original = CreateWsfexSubmission(76546);
        InvoiceOperation prior = await SaveInStateAsync(journal, original, InvoiceState.Conflict, CreateNoResultQueryResponse(), cancellationToken);
        var capability = (IInvoiceRejectionAdjudicationJournal)journal;
        var evidence = new InvoiceRejectionEvidence(original, Create1550Response(), "operator-1", "manual-review", "reason");
        using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            using var trigger = connection.CreateCommand();
            trigger.CommandText = "CREATE TRIGGER reject_adjudication_insert BEFORE INSERT ON invoice_rejection_adjudications BEGIN SELECT RAISE(ABORT, 'synthetic audit failure'); END";
            await trigger.ExecuteNonQueryAsync(cancellationToken);
        }

        Func<Task> adjudicate = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, evidence, cancellationToken);
        await adjudicate.Should().ThrowAsync<SqliteException>();
        (await journal.FindAsync(original.TenantId, original.IdempotencyKey, cancellationToken))!.Should().BeEquivalentTo(prior);
        (await journal.ListRevisionsAsync(original.TenantId, original.IdempotencyKey, cancellationToken)).Should().BeEmpty();
        (await capability.ListRejectionAdjudicationsAsync(original.TenantId, original.IdempotencyKey, limit: 100, cancellationToken)).Should().BeEmpty();
    }

    [Fact]
    public async Task AdjudicateWsfexValidationRejectionAsync_serializes_concurrent_adjudicators_and_honors_tenant_bound_limit()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        string path = InvoiceTestData.NewDatabasePath(database, "adjudication-concurrent");
        var first = new SqliteInvoiceJournal(path);
        var second = new SqliteInvoiceJournal(path);
        InvoiceSubmission original = CreateWsfexSubmission(76547);
        InvoiceOperation prior = await SaveInStateAsync(first, original, InvoiceState.ManualReview, CreateNoResultQueryResponse(), cancellationToken);
        var evidence = new InvoiceRejectionEvidence(original, Create1550Response(), "operator-1", "manual-review", "reason");
        var firstCapability = (IInvoiceRejectionAdjudicationJournal)first;
        var secondCapability = (IInvoiceRejectionAdjudicationJournal)second;

        Task<bool> Adjudicate(IInvoiceRejectionAdjudicationJournal capability) => TryAdjudicateAsync(capability);
        async Task<bool> TryAdjudicateAsync(IInvoiceRejectionAdjudicationJournal capability)
        {
            try
            {
                await capability.AdjudicateWsfexValidationRejectionAsync(original.TenantId, original.IdempotencyKey, prior.Version, evidence, cancellationToken);
                return true;
            }
            catch (InvoiceConflictException) { return false; }
        }

        (await Task.WhenAll(Adjudicate(firstCapability), Adjudicate(secondCapability))).Count(x => x).Should().Be(1);
        (await firstCapability.ListRejectionAdjudicationsAsync(original.TenantId, original.IdempotencyKey, limit: 1, cancellationToken))
            .Should().ContainSingle();
        (await secondCapability.ListRejectionAdjudicationsAsync("tenant-other", original.IdempotencyKey, limit: 100, cancellationToken))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_states_outside_allowlist()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "adjudication-state-guard"));
        InvoiceSubmission original = CreateWsfexSubmission(76548);
        InvoiceOperation prepared = await journal.PrepareAsync(original, cancellationToken);
        var evidence = new InvoiceRejectionEvidence(original, Create1550Response(), "operator-1", "manual-review", "reason");
        var capability = (IInvoiceRejectionAdjudicationJournal)journal;

        Func<Task> adjudicatePrepared = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prepared.Version, evidence, cancellationToken);

        await adjudicatePrepared.Should().ThrowAsync<InvoiceConflictException>();
        (await journal.FindAsync(original.TenantId, original.IdempotencyKey, cancellationToken))!.State.Should().Be(InvoiceState.Prepared);
        (await capability.ListRejectionAdjudicationsAsync(original.TenantId, original.IdempotencyKey, limit: 100, cancellationToken))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_when_current_response_has_positive_authorization()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "adjudication-positive-query"));
        InvoiceSubmission original = CreateWsfexSubmission(76549);
        string positiveResponse = CreatePositiveQueryResponse(original);
        InvoiceOperation prior = await SaveInStateAsync(journal, original, InvoiceState.Conflict, positiveResponse, cancellationToken);
        var capability = (IInvoiceRejectionAdjudicationJournal)journal;
        var evidence = new InvoiceRejectionEvidence(original, Create1550Response(), "operator-1", "manual-review", "reason");

        Func<Task> adjudicate = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, evidence, cancellationToken);
        await adjudicate.Should().ThrowAsync<InvoiceConflictException>();

        (await journal.FindAsync(original.TenantId, original.IdempotencyKey, cancellationToken))!.Should().BeEquivalentTo(prior);
        (await journal.ListRevisionsAsync(original.TenantId, original.IdempotencyKey, cancellationToken)).Should().BeEmpty();
        (await capability.ListRejectionAdjudicationsAsync(original.TenantId, original.IdempotencyKey, limit: 100, cancellationToken))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_when_current_authorize_response_is_not_1550()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "adjudication-current-1551"));
        InvoiceSubmission original = CreateWsfexSubmission(76552);
        InvoiceOperation prior = await SaveInStateAsync(journal, original, InvoiceState.Conflict, CreateResponse(1551), cancellationToken);
        var capability = (IInvoiceRejectionAdjudicationJournal)journal;
        var evidence = new InvoiceRejectionEvidence(original, Create1550Response(), "operator-1", "manual-review", "reason");

        Func<Task> adjudicate = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, evidence, cancellationToken);
        await adjudicate.Should().ThrowAsync<InvoiceConflictException>();

        (await journal.FindAsync(original.TenantId, original.IdempotencyKey, cancellationToken))!.Should().BeEquivalentTo(prior);
        (await journal.ListRevisionsAsync(original.TenantId, original.IdempotencyKey, cancellationToken)).Should().BeEmpty();
        (await capability.ListRejectionAdjudicationsAsync(original.TenantId, original.IdempotencyKey, limit: 100, cancellationToken))
            .Should().BeEmpty();
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("Cbte_tipo")]
    [InlineData("Punto_vta")]
    [InlineData("Cbte_nro")]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_partial_positive_current_query_identity(string positiveField)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "adjudication-partial-query"));
        InvoiceSubmission original = CreateWsfexSubmission(76550);
        string query = CreatePartialIdentityQueryResponse(original, positiveField);
        InvoiceOperation prior = await SaveInStateAsync(journal, original, InvoiceState.Conflict, query, cancellationToken);
        var capability = (IInvoiceRejectionAdjudicationJournal)journal;
        var evidence = new InvoiceRejectionEvidence(original, Create1550Response(), "operator-1", "manual-review", "reason");

        Func<Task> adjudicate = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, evidence, cancellationToken);
        await adjudicate.Should().ThrowAsync<InvoiceConflictException>();

        (await journal.FindAsync(original.TenantId, original.IdempotencyKey, cancellationToken))!.Should().BeEquivalentTo(prior);
        (await journal.ListRevisionsAsync(original.TenantId, original.IdempotencyKey, cancellationToken)).Should().BeEmpty();
        (await capability.ListRejectionAdjudicationsAsync(original.TenantId, original.IdempotencyKey, limit: 100, cancellationToken))
            .Should().BeEmpty();
    }

    [Theory]
    [InlineData("query-result")]
    [InlineData("voucher-result")]
    [InlineData("error")]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_current_query_critical_names_in_foreign_namespace(string variant)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "adjudication-query-namespace"));
        InvoiceSubmission original = CreateWsfexSubmission(76551);
        string query = CreateForeignNamespaceQueryResponse(variant);
        InvoiceOperation prior = await SaveInStateAsync(journal, original, InvoiceState.Conflict, query, cancellationToken);
        var capability = (IInvoiceRejectionAdjudicationJournal)journal;
        var evidence = new InvoiceRejectionEvidence(original, Create1550Response(), "operator-1", "manual-review", "reason");

        Func<Task> adjudicate = () => capability.AdjudicateWsfexValidationRejectionAsync(
            original.TenantId, original.IdempotencyKey, prior.Version, evidence, cancellationToken);
        await adjudicate.Should().ThrowAsync<InvoiceConflictException>();

        (await journal.FindAsync(original.TenantId, original.IdempotencyKey, cancellationToken))!.Should().BeEquivalentTo(prior);
        (await journal.ListRevisionsAsync(original.TenantId, original.IdempotencyKey, cancellationToken)).Should().BeEmpty();
        (await capability.ListRejectionAdjudicationsAsync(original.TenantId, original.IdempotencyKey, limit: 100, cancellationToken))
            .Should().BeEmpty();
    }

    private static async Task<InvoiceOperation> SaveInStateAsync(SqliteInvoiceJournal journal, InvoiceSubmission submission,
        InvoiceState state, string responseXml, CancellationToken cancellationToken)
    {
        await journal.PrepareAsync(submission, cancellationToken);
        InvoiceLease dispatch = (await journal.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey,
            TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        InvoiceOperation current = await journal.CompleteAsync(dispatch,
            new InvoiceDecision(InvoiceState.Unknown, ResponseXml: responseXml), cancellationToken);
        if (state == InvoiceState.Unknown) return current;
        InvoiceLease reconcile = (await journal.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey,
            TimeSpan.FromMinutes(1), reconciliation: true, cancellationToken))!;
        return await journal.CompleteAsync(reconcile, new InvoiceDecision(state, ResponseXml: responseXml), cancellationToken);
    }

    private static InvoiceSubmission CreateWsfexSubmission(long requestId)
    {
        var request = new ClsFexRequest
        {
            Id = requestId,
            FechaCbte = "20261012",
            CbteTipo = 19,
            PuntoVta = 1,
            CbteNro = 1,
            TipoExpo = 2,
            MonedaId = "DOL",
            MonedaCtz = 1
        };
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        new XmlSerializer(typeof(ClsFexRequest)).Serialize(writer, request);
        var identity = new InvoiceIdentity(ArcaEnvironment.Homologation, 30123456789, 1, 19, 1);
        return new InvoiceSubmission("tenant-a", $"wsfex-{requestId}", "wsfex", identity, writer.ToString(), RemoteRequestId: requestId);
    }

    private static string Create1550Response() => CreateResponse(1550);

    private static string CreatePartialResponse()
    {
        var response = new FexAuthorizeResponse { FexAuthorizeResult = new FexResponseAuthorize() };
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        new XmlSerializer(typeof(FexAuthorizeResponse)).Serialize(writer, response);
        return writer.ToString();
    }

    private static string CreateNoResultQueryResponse()
    {
        var response = new FexGetCmpResponse2
        {
            FexGetCmpResult = new FexGetCmpResponse
            {
                FexResultGet = new ClsFexGetCmpr(),
                FexErr = new ClsFexErr { ErrCode = 1020, ErrMsg = "Comprobante inexistente" },
                FexEvents = CreateEvents()
            }
        };
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        new XmlSerializer(typeof(FexGetCmpResponse2)).Serialize(writer, response);
        return writer.ToString();
    }

    private static string CreatePositiveQueryResponse(InvoiceSubmission submission)
    {
        var response = new FexGetCmpResponse2
        {
            FexGetCmpResult = new FexGetCmpResponse
            {
                FexResultGet = new ClsFexGetCmpr
                {
                    Id = submission.RemoteRequestId!.Value,
                    CbteTipo = checked((short)submission.Identity.VoucherType),
                    PuntoVta = submission.Identity.PointOfSale,
                    CbteNro = submission.Identity.VoucherNumber,
                    TipoExpo = 2,
                    Cae = "12345678901234",
                    Resultado = "A"
                },
                FexEvents = CreateEvents()
            }
        };
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        new XmlSerializer(typeof(FexGetCmpResponse2)).Serialize(writer, response);
        return writer.ToString();
    }

    private static string CreatePartialIdentityQueryResponse(InvoiceSubmission submission, string positiveField)
    {
        var remote = new ClsFexGetCmpr
        {
            Id = positiveField == "Id" ? submission.RemoteRequestId!.Value : 0,
            CbteTipo = positiveField == "Cbte_tipo" ? checked((short)submission.Identity.VoucherType) : (short)0,
            PuntoVta = positiveField == "Punto_vta" ? submission.Identity.PointOfSale : 0,
            CbteNro = positiveField == "Cbte_nro" ? submission.Identity.VoucherNumber : 0
        };
        var response = new FexGetCmpResponse2
        {
            FexGetCmpResult = new FexGetCmpResponse
            {
                FexResultGet = remote,
                FexErr = new ClsFexErr { ErrCode = 1020, ErrMsg = "Comprobante inexistente" },
                FexEvents = CreateEvents()
            }
        };
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        new XmlSerializer(typeof(FexGetCmpResponse2)).Serialize(writer, response);
        return writer.ToString();
    }

    private static string CreateForeignNamespaceQueryResponse(string variant)
    {
        const string ns = "http://ar.gov.afip.dif.fexv1/";
        XDocument document = XDocument.Parse(CreateNoResultQueryResponse());
        XElement root = document.Root!;
        XElement result = root.Element(XName.Get("FEXGetCMPResult", ns))!;
        switch (variant)
        {
            case "query-result":
                root.Add(new XElement(XName.Get("FEXGetCMPResult", "urn:foreign")));
                break;
            case "voucher-result":
                result.Add(new XElement(XName.Get("FEXResultGet", "urn:foreign")));
                break;
            case "error":
                result.Add(new XElement(XName.Get("FEXErr", "urn:foreign")));
                break;
        }
        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static string CreateResponse(int code, bool includeAuthorization = false)
    {
        var response = new FexAuthorizeResponse
        {
            FexAuthorizeResult = new FexResponseAuthorize
            {
                FexErr = new ClsFexErr
                {
                    ErrCode = code,
                    ErrMsg = "Campo Permiso_existente obligatorio: debe enviarse tag"
                },
                FexResultAuth = includeAuthorization ? new ClsFexOutAuthorize { Id = 76543 } : null,
                FexEvents = CreateEvents()
            }
        };
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        new XmlSerializer(typeof(FexAuthorizeResponse)).Serialize(writer, response);
        return writer.ToString();
    }

    private static ClsFexEvents CreateEvents() => new() { EventCode = 0, EventMsg = "QA fixture" };
}
