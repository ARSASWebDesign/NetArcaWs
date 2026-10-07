using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.Tests;

public sealed class EfInvoiceRecoveryQueueTests
{
    [Fact]
    public async Task Prepare_and_enqueue_rolls_back_invoice_and_global_series_when_queue_write_fails()
    {
        using var db = new RecoveryDatabase(failQueueInsert: true);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InvoiceSubmission submission = Submission();
        Func<Task> prepare = () => db.Queue.PrepareAndEnqueueAsync(submission, "cred-v1", TestContext.Current.CancellationToken);

        await prepare.Should().ThrowAsync<InvalidOperationException>();
        (await db.Journal.FindAsync(submission.TenantId, submission.IdempotencyKey, cancellationToken)).Should().BeNull();
        InvoiceOperation other = await db.Journal.PrepareAsync(submission with { TenantId = "other", IdempotencyKey = "other" }, cancellationToken);
        other.State.Should().Be(InvoiceState.Prepared);
    }

    [Fact]
    public async Task Concurrent_identical_prepare_and_enqueue_returns_the_same_atomically_queued_operation()
    {
        using var db = new RecoveryDatabase();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InvoiceSubmission submission = Submission();
        using var start = new ManualResetEventSlim();
        Task<InvoiceOperation> first = Task.Run(async () =>
        {
            start.Wait(cancellationToken);
            return await db.Queue.PrepareAndEnqueueAsync(submission, "credential-v1", cancellationToken);
        }, cancellationToken);
        Task<InvoiceOperation> second = Task.Run(async () =>
        {
            start.Wait(cancellationToken);
            return await db.Queue.PrepareAndEnqueueAsync(submission, "credential-v1", cancellationToken);
        }, cancellationToken);
        start.Set();

        InvoiceOperation[] operations = await Task.WhenAll(first, second);

        operations.Should().HaveCount(2);
        operations[0].Should().BeEquivalentTo(operations[1]);
        operations[0].Submission.Should().BeEquivalentTo(submission);
        InvoiceRecoveryMetadata metadata = (await db.Queue.FindAsync(Scope(submission.TenantId), submission.IdempotencyKey, cancellationToken))!;
        metadata.State.Should().Be(InvoiceRecoveryState.Scheduled);
        metadata.Attempt.Should().Be(0);
        metadata.Generation.Should().Be(0);
    }

    [Fact]
    public async Task Identical_replay_preserves_attempt_schedule_and_credential_reference()
    {
        using var db = new RecoveryDatabase();
        InvoiceSubmission submission = Submission();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await db.Queue.PrepareAndEnqueueAsync(submission, "credential-old", cancellationToken);
        InvoiceRecoveryScope scope = Scope(submission.TenantId);
        InvoiceRecoveryLease lease = (await db.Queue.TryClaimAsync(scope, TimeSpan.FromMinutes(1), cancellationToken))!;
        DateTimeOffset later = db.Clock.GetUtcNow().AddHours(1);
        await db.Queue.CompleteAsync(lease, InvoiceRecoveryDisposition.Reschedule, InvoiceRecoverySafeReason.QueryUnavailable, later, cancellationToken);

        Func<Task> changedCredential = () => db.Queue.PrepareAndEnqueueAsync(submission, "credential-new", cancellationToken);
        await changedCredential.Should().ThrowAsync<InvoiceRecoveryConflictException>();
        await db.Queue.PrepareAndEnqueueAsync(submission, "credential-old", cancellationToken);

        InvoiceRecoveryMetadata metadata = (await db.Queue.FindAsync(scope, submission.IdempotencyKey, cancellationToken))!;
        metadata.State.Should().Be(InvoiceRecoveryState.Scheduled);
        metadata.Attempt.Should().Be(1);
        metadata.Generation.Should().Be(lease.Generation);
        metadata.NextAvailable.Should().BeCloseTo(later, TimeSpan.FromMilliseconds(1));
        (await db.Queue.TryClaimAsync(scope, TimeSpan.FromMinutes(1), cancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task Changed_payload_replay_conflicts_and_scope_filters_tenant_and_service()
    {
        using var db = new RecoveryDatabase();
        InvoiceSubmission original = Submission();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await db.Queue.PrepareAndEnqueueAsync(original, null, cancellationToken);
        Func<Task> changed = () => db.Queue.PrepareAndEnqueueAsync(original with { Payload = "<invoice>changed</invoice>" }, null, cancellationToken);
        await changed.Should().ThrowAsync<InvoiceRecoveryConflictException>();

        (await db.Queue.TryClaimAsync(Scope("another-tenant"), TimeSpan.FromMinutes(1), cancellationToken)).Should().BeNull();
        (await db.Queue.FindAsync(new InvoiceRecoveryScope(original.TenantId, [ArcaService.Wsfexv1]), original.IdempotencyKey, cancellationToken)).Should().BeNull();
        InvoiceRecoveryLease claim = (await db.Queue.TryClaimAsync(Scope(original.TenantId), TimeSpan.FromMinutes(1), cancellationToken))!;
        claim.WorkItem.TenantId.Should().Be(original.TenantId);
        claim.WorkItem.Service.Should().Be(ArcaService.Wsfev1);

        InvoiceSubmission fiscalDuplicate = original with { TenantId = "different-tenant", IdempotencyKey = "different-key", Service = "wsfex" };
        Func<Task> duplicateSeries = () => db.Journal.PrepareAsync(fiscalDuplicate, cancellationToken);
        await duplicateSeries.Should().ThrowAsync<InvoiceConflictException>();
    }

    [Fact]
    public async Task Concurrent_claims_have_one_winner_and_expired_prepared_claim_becomes_unknown()
    {
        using var db = new RecoveryDatabase();
        InvoiceSubmission submission = Submission();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await db.Queue.PrepareAndEnqueueAsync(submission, null, cancellationToken);
        InvoiceRecoveryScope scope = Scope(submission.TenantId);
        Task<InvoiceRecoveryLease?> one = db.Queue.TryClaimAsync(scope, TimeSpan.FromSeconds(10), cancellationToken);
        Task<InvoiceRecoveryLease?> two = db.Queue.TryClaimAsync(scope, TimeSpan.FromSeconds(10), cancellationToken);
        InvoiceRecoveryLease?[] claims = await Task.WhenAll(one, two);
        claims.Count(x => x is not null).Should().Be(1);
        InvoiceRecoveryLease stale = claims.Single(x => x is not null)!;
        (await db.Queue.IsCurrentAsync(stale, cancellationToken)).Should().BeTrue();

        db.Clock.Advance(TimeSpan.FromSeconds(11));
        InvoiceRecoveryLease recovered = (await db.Queue.TryClaimAsync(scope, TimeSpan.FromSeconds(10), cancellationToken))!;
        recovered.Generation.Should().BeGreaterThan(stale.Generation);
        recovered.CurrentOperation.State.Should().Be(InvoiceState.Unknown);
        recovered.CurrentOperation.Version.Should().Be(stale.CurrentOperation.Version + 1);
        (await db.Queue.IsCurrentAsync(stale, cancellationToken)).Should().BeFalse();
        Func<Task> staleComplete = () => db.Queue.CompleteAsync(stale, InvoiceRecoveryDisposition.Complete, InvoiceRecoverySafeReason.None, cancellationToken: cancellationToken);
        await staleComplete.Should().ThrowAsync<InvoiceRecoveryConflictException>();
    }

    [Fact]
    public async Task Journal_progression_with_unchanged_fiscal_snapshot_can_be_rescheduled_and_reclaimed()
    {
        using var db = new RecoveryDatabase();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InvoiceSubmission submission = Submission();
        await db.Queue.PrepareAndEnqueueAsync(submission, "cred-v1", cancellationToken);
        InvoiceRecoveryScope scope = Scope(submission.TenantId);
        InvoiceRecoveryLease first = (await db.Queue.TryClaimAsync(scope, TimeSpan.FromMinutes(1), cancellationToken))!;

        InvoiceLease invoiceLease = (await db.Journal.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey,
            TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        InvoiceOperation unknown = await db.Journal.CompleteAsync(invoiceLease, new InvoiceDecision(InvoiceState.Unknown), cancellationToken);
        InvoiceOperation replay = await db.Queue.PrepareAndEnqueueAsync(submission, "cred-v1", cancellationToken);
        replay.Version.Should().Be(unknown.Version);
        InvoiceRecoveryMetadata duringReplay = (await db.Queue.FindAsync(scope, submission.IdempotencyKey, cancellationToken))!;
        duringReplay.State.Should().Be(InvoiceRecoveryState.Claimed);
        duringReplay.Generation.Should().Be(first.Generation);
        duringReplay.Attempt.Should().Be(1);
        duringReplay.LeaseUntil.Should().Be(first.LeaseUntil);
        await db.Queue.CompleteAsync(first, InvoiceRecoveryDisposition.Reschedule, InvoiceRecoverySafeReason.QueryUnavailable,
            db.Clock.GetUtcNow(), cancellationToken);

        InvoiceRecoveryLease reclaimed = (await db.Queue.TryClaimAsync(scope, TimeSpan.FromMinutes(1), cancellationToken))!;

        reclaimed.CurrentOperation.State.Should().Be(InvoiceState.Unknown);
        reclaimed.CurrentOperation.Version.Should().Be(unknown.Version);
        reclaimed.WorkItem.InvoiceVersion.Should().Be(unknown.Version);
    }

    [Fact]
    public async Task Expired_prepared_transition_persists_new_pin_before_a_later_reschedule_and_reclaim()
    {
        using var db = new RecoveryDatabase();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InvoiceSubmission submission = Submission();
        await db.Queue.PrepareAndEnqueueAsync(submission, null, cancellationToken);
        InvoiceRecoveryScope scope = Scope(submission.TenantId);
        InvoiceRecoveryLease expired = (await db.Queue.TryClaimAsync(scope, TimeSpan.FromSeconds(1), cancellationToken))!;
        db.Clock.Advance(TimeSpan.FromSeconds(2));

        InvoiceRecoveryLease reconciliation = (await db.Queue.TryClaimAsync(scope, TimeSpan.FromMinutes(1), cancellationToken))!;
        reconciliation.CurrentOperation.State.Should().Be(InvoiceState.Unknown);
        await db.Queue.CompleteAsync(reconciliation, InvoiceRecoveryDisposition.Reschedule, InvoiceRecoverySafeReason.QueryEmpty,
            db.Clock.GetUtcNow(), cancellationToken);

        InvoiceRecoveryLease next = (await db.Queue.TryClaimAsync(scope, TimeSpan.FromMinutes(1), cancellationToken))!;
        next.CurrentOperation.State.Should().Be(InvoiceState.Unknown);
        next.WorkItem.InvoiceVersion.Should().Be(reconciliation.CurrentOperation.Version);
        next.Generation.Should().BeGreaterThan(expired.Generation);
    }

    [Theory]
    [InlineData(InvoiceState.Conflict)]
    [InlineData(InvoiceState.ManualReview)]
    public async Task Conflict_and_manual_review_are_suspended_with_safe_reason(InvoiceState terminalState)
    {
        using var db = new RecoveryDatabase();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InvoiceSubmission submission = Submission();
        await db.Queue.PrepareAndEnqueueAsync(submission, null, cancellationToken);
        InvoiceLease invoiceLease = (await db.Journal.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey,
            TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        await db.Journal.CompleteAsync(invoiceLease, new InvoiceDecision(terminalState), cancellationToken);

        (await db.Queue.TryClaimAsync(Scope(submission.TenantId), TimeSpan.FromMinutes(1), cancellationToken)).Should().BeNull();
        InvoiceRecoveryMetadata metadata = (await db.Queue.FindAsync(Scope(submission.TenantId), submission.IdempotencyKey, cancellationToken))!;
        metadata.State.Should().Be(InvoiceRecoveryState.Suspended);
        metadata.LastReason.Should().Be(InvoiceRecoverySafeReason.PersistenceConflict);
    }

    [Fact]
    public async Task Schedule_requires_exact_current_version_and_pins_explicit_rejected_revision()
    {
        using var db = new RecoveryDatabase();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InvoiceSubmission original = Submission();
        InvoiceOperation prepared = await db.Queue.PrepareAndEnqueueAsync(original, "old-ref", cancellationToken);
        InvoiceLease invoiceLease = (await db.Journal.TryAcquireAsync(original.TenantId, original.IdempotencyKey,
            TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        InvoiceOperation rejected = await db.Journal.CompleteAsync(invoiceLease, new InvoiceDecision(InvoiceState.Rejected), cancellationToken);
        Func<Task> rejectedSchedule = () => db.Queue.ScheduleAsync(original.TenantId, original.IdempotencyKey, rejected.Version, "ref", cancellationToken);
        await rejectedSchedule.Should().ThrowAsync<InvoiceRecoveryConflictException>();
        Func<Task> staleVersion = () => db.Queue.ScheduleAsync(original.TenantId, original.IdempotencyKey, prepared.Version, "ref", cancellationToken);
        await staleVersion.Should().ThrowAsync<InvoiceRecoveryConflictException>();

        InvoiceSubmission corrected = original with { Payload = "<invoice>corrected</invoice>" };
        InvoiceOperation revised = await db.Journal.ReviseRejectedAsync(corrected, rejected.Version, cancellationToken);
        InvoiceOperation scheduled = await db.Queue.ScheduleAsync(original.TenantId, original.IdempotencyKey, revised.Version, "new-ref", cancellationToken);

        scheduled.Submission.Should().BeEquivalentTo(corrected);
        InvoiceRecoveryLease claim = (await db.Queue.TryClaimAsync(Scope(original.TenantId), TimeSpan.FromMinutes(1), cancellationToken))!;
        claim.WorkItem.InvoiceVersion.Should().Be(revised.Version);
        claim.WorkItem.PayloadHash.Should().NotBeEmpty();
        claim.WorkItem.CredentialReference.Should().Be("new-ref");
    }

    [Fact]
    public async Task Explicit_schedule_reactivates_a_completed_job_only_without_an_active_claim()
    {
        using var db = new RecoveryDatabase();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        InvoiceSubmission submission = Submission();
        await db.Queue.PrepareAndEnqueueAsync(submission, "version-1", cancellationToken);
        InvoiceRecoveryScope scope = Scope(submission.TenantId);
        InvoiceRecoveryLease first = (await db.Queue.TryClaimAsync(scope, TimeSpan.FromMinutes(1), cancellationToken))!;
        Func<Task> active = () => db.Queue.ScheduleAsync(submission.TenantId, submission.IdempotencyKey, first.CurrentOperation.Version, "version-2", cancellationToken);
        await active.Should().ThrowAsync<InvoiceRecoveryConflictException>();
        await db.Queue.CompleteAsync(first, InvoiceRecoveryDisposition.Complete, InvoiceRecoverySafeReason.None, cancellationToken: cancellationToken);

        await db.Queue.ScheduleAsync(submission.TenantId, submission.IdempotencyKey, first.CurrentOperation.Version, "version-2", cancellationToken);
        InvoiceRecoveryLease second = (await db.Queue.TryClaimAsync(scope, TimeSpan.FromMinutes(1), cancellationToken))!;

        second.Generation.Should().BeGreaterThan(first.Generation);
        second.WorkItem.CredentialReference.Should().Be("version-2");
        second.WorkItem.Attempt.Should().Be(1, "an explicit schedule begins a new attempt generation");
    }

    [Fact]
    public async Task Scope_and_contract_values_are_defensively_copied_and_redacted()
    {
        ArcaService[] services = [ArcaService.Wsfev1];
        var scope = new InvoiceRecoveryScope("private-tenant", services);
        services[0] = ArcaService.Wsfexv1;
        scope.Services.Should().Contain(ArcaService.Wsfev1).And.NotContain(ArcaService.Wsfexv1);
        scope.ToString().Should().NotContain("private-tenant");
        Action invalidUnicode = () => new InvoiceRecoveryScope("bad\ud800", [ArcaService.Wsfev1]);
        invalidUnicode.Should().Throw<ArgumentException>();
        Action tooLong = () => new InvoiceRecoveryScope(new string('t', 513), [ArcaService.Wsfev1]);
        tooLong.Should().Throw<ArgumentException>();
        Action duplicate = () => new InvoiceRecoveryScope("t", [ArcaService.Wsfev1, ArcaService.Wsfev1]);
        duplicate.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Queue_identifiers_and_credential_references_reject_invalid_unicode_and_overlong_values()
    {
        using var db = new RecoveryDatabase();
        InvoiceSubmission submission = Submission();
        Func<Task> badTenant = () => db.Queue.PrepareAndEnqueueAsync(submission with { TenantId = "bad\ud800" }, null);
        Func<Task> longKey = () => db.Queue.PrepareAndEnqueueAsync(submission with { IdempotencyKey = new string('k', 513) }, null);
        Func<Task> badCredential = () => db.Queue.PrepareAndEnqueueAsync(submission, "bad\ud800");
        Func<Task> longCredential = () => db.Queue.PrepareAndEnqueueAsync(submission, new string('r', 513));
        await badTenant.Should().ThrowAsync<ArgumentException>();
        await longKey.Should().ThrowAsync<ArgumentException>();
        await badCredential.Should().ThrowAsync<ArgumentException>();
        await longCredential.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Recovery_model_is_absent_when_module_is_not_selected()
    {
        NetArcaWsModelOptions options = NetArcaWsModelOptions.Configure(builder => builder.AddInvoicing(ArcaService.Wsfev1));
        options.InvoiceRecoveryEnabled.Should().BeFalse();
        options.InvoiceRecoveryServices.Should().BeEmpty();
        options.Fingerprint.Should().NotContain("invoice-recovery");
        var services = new ServiceCollection();
        services.AddDbContextFactory<ArcaWsDbContext>(builder => builder.UseSqlite("Data Source=:memory:"));
        services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(options);
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<IInvoiceRecoveryQueue>().Should().BeNull();
        using ArcaWsDbContext context = provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContext();
        context.Model.FindEntityType("NetArcaWs.EntityFrameworkCore.InvoiceRecoveryJobEntity").Should().BeNull();

        NetArcaWsModelOptions incomplete = NetArcaWsModelOptions.Configure(builder => builder.AddInvoiceRecovery(ArcaService.Wsfev1));
        var invalidServices = new ServiceCollection();
        Action invalidRegistration = () => invalidServices.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(incomplete);
        invalidRegistration.Should().Throw<InvalidOperationException>();
    }

    private static InvoiceRecoveryScope Scope(string tenant) => new(tenant, [ArcaService.Wsfev1]);

    private static InvoiceSubmission Submission(string tenant = "tenant-a") => new(tenant, "recovery-1", "wsfe",
        new InvoiceIdentity(ArcaEnvironment.Homologation, 30123456789, 1, 1, 1), "<invoice>frozen</invoice>");

    private sealed class RecoveryDatabase : IDisposable
    {
        private readonly ServiceProvider provider;
        private readonly string path = Path.Combine(Path.GetTempPath(), $"netarcaws-recovery-{Guid.NewGuid():N}.sqlite3");
        public RecoveryDatabase(bool failQueueInsert = false)
        {
            Clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
            ModelOptions = NetArcaWsModelOptions.Configure(builder => builder.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)
                .AddInvoiceRecovery(ArcaService.Wsfev1, ArcaService.Wsfexv1));
            var services = new ServiceCollection();
            services.AddDbContextFactory<ArcaWsDbContext>(options =>
            {
                options.UseSqlite($"Data Source={path};Pooling=False");
                if (failQueueInsert) options.AddInterceptors(new FailRecoveryInsert());
            });
            services.AddSingleton<TimeProvider>(Clock);
            services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(ModelOptions);
            provider = services.BuildServiceProvider();
            Journal = provider.GetRequiredService<IInvoiceJournal>();
            Queue = provider.GetRequiredService<IInvoiceRecoveryQueue>();
            using ArcaWsDbContext context = provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContext();
            context.Database.EnsureCreated();
        }
        public AdjustableTimeProvider Clock { get; }
        public NetArcaWsModelOptions ModelOptions { get; }
        public IInvoiceJournal Journal { get; }
        public IInvoiceRecoveryQueue Queue { get; }
        public void Dispose()
        {
            provider.Dispose(); SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private sealed class FailRecoveryInsert : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries().Any(x => x.Entity.GetType().Name == "InvoiceRecoveryJobEntity" && x.State == EntityState.Added) == true)
                throw new InvalidOperationException("synthetic queue insert failure");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset now = now;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now += duration;
    }
}
