using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.Tests;

public sealed class EfInvoiceJournalTests
{
    [Fact]
    public async Task Prepare_and_find_are_durable_idempotent_and_reject_tampered_payloads()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new JournalDatabase();
        InvoiceSubmission submission = Submission(payload: "<invoice>original</invoice>");
        await database.Journal.PrepareAsync(submission, cancellationToken);
        await using (var context = database.Factory.CreateDbContext())
        {
            await context.Database.ExecuteSqlRawAsync("UPDATE NetArcaInvoices SET Payload = '<invoice>tampered</invoice>'", cancellationToken);
        }
        Func<Task> find = () => database.Journal.FindAsync(submission.TenantId, submission.IdempotencyKey, cancellationToken);
        await find.Should().ThrowAsync<InvalidDataException>().WithMessage("*integrity check*");
        await using (var context = database.Factory.CreateDbContext())
        {
            await context.Database.ExecuteSqlRawAsync("UPDATE NetArcaInvoices SET Payload = '<invoice>original</invoice>'", cancellationToken);
        }

        using var reopened = new JournalDatabase(database.Path);
        InvoiceOperation replay = await reopened.Journal.PrepareAsync(submission, cancellationToken);
        replay.Submission.Should().BeEquivalentTo(submission);
    }

    [Fact]
    public async Task Prepare_enforces_ordinal_idempotency_global_fiscal_remote_and_series_reservations()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new JournalDatabase();
        InvoiceSubmission original = Submission(remoteId: 9876);
        await database.Journal.PrepareAsync(original, cancellationToken);
        InvoiceSubmission keyCase = original with { TenantId = "Tenant-A", IdempotencyKey = original.IdempotencyKey,
            Identity = Identity(cuit: 20999888777) };
        InvoiceOperation caseDistinct = await database.Journal.PrepareAsync(keyCase, cancellationToken);
        caseDistinct.Submission.TenantId.Should().Be("Tenant-A");

        Func<Task> idemConflict = () => database.Journal.PrepareAsync(original with { Payload = "different" }, cancellationToken);
        await idemConflict.Should().ThrowAsync<InvoiceConflictException>();

        InvoiceLease originalLease = (await database.Journal.TryAcquireAsync(original.TenantId, original.IdempotencyKey,
            TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        await database.Journal.CompleteAsync(originalLease, new InvoiceDecision(InvoiceState.Authorized, "123456"), cancellationToken);
        InvoiceSubmission duplicateFiscal = original with { TenantId = "tenant-b", IdempotencyKey = "other" };
        Func<Task> fiscalConflict = () => database.Journal.PrepareAsync(duplicateFiscal, cancellationToken);
        await fiscalConflict.Should().ThrowAsync<InvoiceConflictException>();

        InvoiceSubmission duplicateRemote = original with
        {
            TenantId = "tenant-b", IdempotencyKey = "remote-other",
            Identity = original.Identity with { VoucherNumber = 2 }
        };
        Func<Task> remoteConflict = () => database.Journal.PrepareAsync(duplicateRemote, cancellationToken);
        await remoteConflict.Should().ThrowAsync<InvoiceConflictException>();

        InvoiceSubmission reserved = original with { IdempotencyKey = "reserved", RemoteRequestId = null,
            Identity = original.Identity with { VoucherNumber = 10 } };
        await database.Journal.PrepareAsync(reserved, cancellationToken);
        InvoiceSubmission next = reserved with { IdempotencyKey = "next", Identity = reserved.Identity with { VoucherNumber = 11 } };
        Func<Task> seriesConflict = () => database.Journal.PrepareAsync(next, cancellationToken);
        await seriesConflict.Should().ThrowAsync<InvoiceConflictException>();
    }

    [Fact]
    public async Task Prepare_is_atomic_for_concurrent_new_invoices_in_the_same_series()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new JournalDatabase();
        InvoiceSubmission first = Submission(id: "one");
        InvoiceSubmission second = Submission(id: "two", identity: first.Identity with { VoucherNumber = 2 });
        Task<InvoiceOperation> firstAttempt = database.Journal.PrepareAsync(first, cancellationToken);
        Task<InvoiceOperation> secondAttempt = database.Journal.PrepareAsync(second, cancellationToken);
        Func<Task> all = async () => await Task.WhenAll(firstAttempt, secondAttempt);
        await all.Should().ThrowAsync<InvoiceConflictException>();
        (firstAttempt.IsCompletedSuccessfully ^ secondAttempt.IsCompletedSuccessfully).Should().BeTrue();
    }

    [Fact]
    public async Task Lease_expiry_fences_stale_completion_and_only_reconciliation_can_resume()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new JournalDatabase();
        InvoiceSubmission submission = Submission();
        await database.Journal.PrepareAsync(submission, cancellationToken);
        InvoiceLease first = (await database.Journal.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey,
            TimeSpan.FromSeconds(20), reconciliation: false, cancellationToken))!;
        database.Clock.Advance(TimeSpan.FromSeconds(21));
        (await database.Journal.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey,
            TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken)).Should().BeNull();
        InvoiceLease second = (await database.Journal.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey,
            TimeSpan.FromMinutes(1), reconciliation: true, cancellationToken))!;
        second.Operation.State.Should().Be(InvoiceState.Reconciling);
        Func<Task> stale = () => database.Journal.CompleteAsync(first,
            new InvoiceDecision(InvoiceState.Authorized, "123456"), cancellationToken);
        await stale.Should().ThrowAsync<InvalidOperationException>();
        (await database.Journal.CompleteAsync(second,
            new InvoiceDecision(InvoiceState.Authorized, "123456"), cancellationToken)).State.Should().Be(InvoiceState.Authorized);
    }

    [Fact]
    public async Task Rejected_revision_preserves_identity_remote_id_and_immutable_history_then_releases_series()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new JournalDatabase();
        InvoiceSubmission submission = Submission(remoteId: 4567);
        await database.Journal.PrepareAsync(submission, cancellationToken);
        InvoiceLease lease = (await database.Journal.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey,
            TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        InvoiceOperation rejected = await database.Journal.CompleteAsync(lease,
            new InvoiceDecision(InvoiceState.Rejected, ResponseXml: "<rejected />"), cancellationToken);
        InvoiceSubmission revision = submission with { Payload = "<invoice>corrected</invoice>" };
        InvoiceOperation revised = await database.Journal.ReviseRejectedAsync(revision, rejected.Version, cancellationToken);
        revised.State.Should().Be(InvoiceState.Prepared);
        revised.Submission.RemoteRequestId.Should().Be(4567);
        IReadOnlyList<InvoiceOperation> history = await database.Journal.ListRevisionsAsync(submission.TenantId, submission.IdempotencyKey, cancellationToken);
        history.Should().ContainSingle().Which.Submission.Payload.Should().Be(submission.Payload);
        history[0].ResponseXml.Should().Be("<rejected />");
        await using (var context = database.Factory.CreateDbContext())
            await context.Database.ExecuteSqlRawAsync("UPDATE NetArcaInvoiceRevisions SET ResponseXml = '<tampered />'", cancellationToken);
        Func<Task> readTamperedHistory = () => database.Journal.ListRevisionsAsync(submission.TenantId, submission.IdempotencyKey, cancellationToken);
        await readTamperedHistory.Should().ThrowAsync<InvalidDataException>().WithMessage("*revision*integrity*");
        InvoiceSubmission next = submission with { IdempotencyKey = "next", RemoteRequestId = null,
            Identity = submission.Identity with { VoucherNumber = 2 } };
        Func<Task> stillReserved = () => database.Journal.PrepareAsync(next, cancellationToken);
        await stillReserved.Should().ThrowAsync<InvoiceConflictException>();
    }

    [Fact]
    public async Task List_pending_is_tenant_scoped_and_try_acquire_rejects_invalid_decisions()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new JournalDatabase();
        InvoiceSubmission first = Submission();
        InvoiceSubmission second = Submission(tenant: "other", id: "other", identity: Identity(cuit: 20999888777));
        await database.Journal.PrepareAsync(first, cancellationToken);
        await database.Journal.PrepareAsync(second, cancellationToken);
        (await database.Journal.ListPendingAsync(first.TenantId, cancellationToken: cancellationToken)).Select(x => x.Submission.IdempotencyKey)
            .Should().Equal(first.IdempotencyKey);
        InvoiceLease lease = (await database.Journal.TryAcquireAsync(first.TenantId, first.IdempotencyKey,
            TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        Func<Task> invalidDecision = () => database.Journal.CompleteAsync(lease, new InvoiceDecision(InvoiceState.Authorized), cancellationToken);
        await invalidDecision.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Prepare_rejects_unpaired_surrogate_identifiers_instead_of_collapsing_hash_keys()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new JournalDatabase();
        InvoiceSubmission invalid = Submission(tenant: "tenant-\ud800");
        Func<Task> prepare = () => database.Journal.PrepareAsync(invalid, cancellationToken);
        await prepare.Should().ThrowAsync<ArgumentException>().WithMessage("*invalid Unicode*");
    }

    [Fact]
    public void Model_selection_is_explicit_and_changes_the_cached_dedicated_context_model()
    {
        NetArcaWsModelOptions empty = NetArcaWsModelOptions.Configure(_ => { });
        NetArcaWsModelOptions wsfe = Options(ArcaService.Wsfev1);
        NetArcaWsModelOptions wsfex = Options(ArcaService.Wsfexv1);
        var dbOptions = new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite("Data Source=:memory:").Options;

        using var unconfigured = new ArcaWsDbContext(dbOptions, empty);
        using var wsfeContext = new ArcaWsDbContext(dbOptions, wsfe);
        using var wsfexContext = new ArcaWsDbContext(dbOptions, wsfex);

        unconfigured.Model.GetEntityTypes().Should().BeEmpty();
        wsfeContext.Model.GetEntityTypes().Select(x => x.GetTableName()).Should()
            .Contain("NetArcaInvoices").And.Contain("NetArcaInvoiceRevisions").And.Contain("NetArcaInvoiceSeriesReservations");
        wsfexContext.Model.FindAnnotation(NetArcaWsModelBuilderExtensions.OptionsAnnotationName)!.Value.Should().Be(wsfex.Fingerprint);
        wsfeContext.Model.FindAnnotation(NetArcaWsModelBuilderExtensions.OptionsAnnotationName)!.Value.Should().Be(wsfe.Fingerprint);
        wsfexContext.Model.GetEntityTypes().Select(x => x.GetTableName()).Should()
            .BeEquivalentTo(wsfeContext.Model.GetEntityTypes().Select(x => x.GetTableName()));
    }

    [Fact]
    public void Invoicing_selection_rejects_non_invoice_and_undefined_arca_services()
    {
        Action unsupported = () => Options(ArcaService.Wsaa);
        Action undefined = () => Options((ArcaService)999);
        unsupported.Should().Throw<ArgumentException>().WithMessage("*Unsupported invoicing service*");
        undefined.Should().Throw<ArgumentException>().WithMessage("*Unsupported invoicing service*");
    }

    [Fact]
    public async Task Disabled_service_is_rejected_before_prepare_revision_or_lease_but_history_can_be_read()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"netarcaws-disabled-{Guid.NewGuid():N}.sqlite3");
        try
        {
            InvoiceSubmission wsfe = Submission();
            using var enabled = new JournalDatabase(path, Options(ArcaService.Wsfev1));
            await enabled.Journal.PrepareAsync(wsfe, cancellationToken);
            InvoiceLease lease = (await enabled.Journal.TryAcquireAsync(wsfe.TenantId, wsfe.IdempotencyKey,
                TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
            await enabled.Journal.CompleteAsync(lease,
                new InvoiceDecision(InvoiceState.Rejected, ResponseXml: "<rejected />"), cancellationToken);
            using var disabled = new JournalDatabase(path, Options(ArcaService.Wsfexv1));
            (await disabled.Journal.FindAsync(wsfe.TenantId, wsfe.IdempotencyKey, cancellationToken))!.Submission.Should().BeEquivalentTo(wsfe);
            Func<Task> prepare = () => disabled.Journal.PrepareAsync(wsfe with { IdempotencyKey = "new" }, cancellationToken);
            await prepare.Should().ThrowAsync<InvalidOperationException>().WithMessage("*disabled*");
            Func<Task> revise = () => disabled.Journal.ReviseRejectedAsync(wsfe with { Payload = "<fixed />" }, 2, cancellationToken);
            await revise.Should().ThrowAsync<InvalidOperationException>().WithMessage("*disabled*");
            Func<Task> acquire = () => disabled.Journal.TryAcquireAsync(wsfe.TenantId, wsfe.IdempotencyKey,
                TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken);
            await acquire.Should().ThrowAsync<InvalidOperationException>().WithMessage("*disabled*");
            (await disabled.Journal.ListPendingAsync(wsfe.TenantId, cancellationToken: cancellationToken)).Should().BeEmpty();
        }
        finally { SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Store_rejects_model_options_that_do_not_match_context_selection()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        NetArcaWsModelOptions modelOptions = Options(ArcaService.Wsfev1);
        NetArcaWsModelOptions storeOptions = Options(ArcaService.Wsfexv1);
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"netarcaws-mismatch-{Guid.NewGuid():N}.sqlite3");
        try
        {
            var services = new ServiceCollection();
            services.AddDbContextFactory<ConsumerDbContext>(builder => builder.UseSqlite($"Data Source={path}"));
            services.AddSingleton(modelOptions);
            services.AddNetArcaWsEntityFrameworkStores<ConsumerDbContext>(storeOptions);
            await using ServiceProvider provider = services.BuildServiceProvider();
            IInvoiceJournal journal = provider.GetRequiredService<IInvoiceJournal>();
            Func<Task> prepare = () => journal.PrepareAsync(Submission() with { Service = "wsfex" }, cancellationToken);
            await prepare.Should().ThrowAsync<InvalidOperationException>().WithMessage("*model selection*");
            File.Exists(path).Should().BeFalse("the mismatch is rejected before database use");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Expanding_service_allowlist_reuses_shared_tables_and_preserves_existing_invoice_data()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"netarcaws-expand-{Guid.NewGuid():N}.sqlite3");
        try
        {
            InvoiceSubmission wsfe = Submission();
            using var first = new JournalDatabase(path, Options(ArcaService.Wsfev1));
            await first.Journal.PrepareAsync(wsfe, cancellationToken);

            using var expanded = new JournalDatabase(path, Options(ArcaService.Wsfev1, ArcaService.Wsfexv1));
            (await expanded.Journal.FindAsync(wsfe.TenantId, wsfe.IdempotencyKey, cancellationToken))!.Submission.Should().BeEquivalentTo(wsfe);
            InvoiceSubmission wsfex = Submission(tenant: "tenant-b", id: "export-001", identity: Identity(cuit: 20999888777)) with
                { Service = "wsfex" };
            (await expanded.Journal.PrepareAsync(wsfex, cancellationToken)).State.Should().Be(InvoiceState.Prepared);
            await using ArcaWsDbContext context = await expanded.Factory.CreateDbContextAsync(cancellationToken);
            context.Model.GetEntityTypes().Select(x => x.GetTableName()).Should()
                .BeEquivalentTo("NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations");
        }
        finally { SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task Registration_supports_dedicated_and_consumer_contexts_without_creating_schema()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"netarcaws-di-{Guid.NewGuid():N}.sqlite3");
        try
        {
            var services = new ServiceCollection();
            NetArcaWsModelOptions modelOptions = Options(ArcaService.Wsfev1);
            services.AddDbContextFactory<ConsumerDbContext>(options => options.UseSqlite($"Data Source={path}"));
            services.AddSingleton(modelOptions);
            services.AddNetArcaWsEntityFrameworkStores<ConsumerDbContext>(modelOptions);
            await using ServiceProvider provider = services.BuildServiceProvider();
            IInvoiceJournal first = provider.GetRequiredService<IInvoiceJournal>();
            IInvoiceJournal second = provider.GetRequiredService<IInvoiceJournal>();
            ReferenceEquals(first, second).Should().BeTrue();
            await using ConsumerDbContext context = await provider.GetRequiredService<IDbContextFactory<ConsumerDbContext>>().CreateDbContextAsync(cancellationToken);
            (await context.Database.CanConnectAsync(cancellationToken)).Should().BeFalse("registration must never initialize a consumer schema");
            context.Model.FindEntityType(typeof(ConsumerRecord)).Should().NotBeNull("consumer mappings must remain present");
            context.Model.GetEntityTypes().Should().Contain(x => x.GetTableName() == "NetArcaInvoices");

            var dedicatedOptions = new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite("Data Source=:memory:").Options;
            await using var dedicated = new ArcaWsDbContext(dedicatedOptions, modelOptions);
            dedicated.Model.GetEntityTypes().Should().Contain(x => x.GetTableName() == "NetArcaInvoices");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static InvoiceSubmission Submission(string tenant = "tenant-a", string id = "invoice-001",
        InvoiceIdentity? identity = null, string payload = "<invoice>120.00</invoice>", long? remoteId = null) =>
        new(tenant, id, "wsfe", identity ?? Identity(), payload, RemoteRequestId: remoteId);

    private static NetArcaWsModelOptions Options(params ArcaService[] services) =>
        NetArcaWsModelOptions.Configure(builder => builder.AddInvoicing(services));

    private static InvoiceIdentity Identity(ArcaEnvironment environment = ArcaEnvironment.Homologation,
        long cuit = 30123456789, int pos = 1, int type = 1, long number = 1) => new(environment, cuit, pos, type, number);

    private sealed class ConsumerRecord { public int Id { get; set; } }

    private sealed class ConsumerDbContext(DbContextOptions<ConsumerDbContext> options, NetArcaWsModelOptions modelOptions) : DbContext(options)
    {
        public DbSet<ConsumerRecord> ConsumerRecords => Set<ConsumerRecord>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConsumerRecord>().HasKey(x => x.Id);
            modelBuilder.AddNetArcaWs(modelOptions);
        }
    }

    private sealed class JournalDatabase : IDisposable
    {
        private readonly ServiceProvider provider;
        public JournalDatabase(string? path = null, NetArcaWsModelOptions? modelOptions = null)
        {
            Path = path ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"netarcaws-ef-{Guid.NewGuid():N}.sqlite3");
            Clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
            ModelOptions = modelOptions ?? Options(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca);
            var services = new ServiceCollection();
            services.AddDbContextFactory<ArcaWsDbContext>(options => options.UseSqlite($"Data Source={Path};Pooling=False"));
            services.AddSingleton<TimeProvider>(Clock);
            services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(ModelOptions);
            provider = services.BuildServiceProvider();
            Factory = provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>();
            Journal = provider.GetRequiredService<IInvoiceJournal>();
            using ArcaWsDbContext context = Factory.CreateDbContext();
            context.Database.EnsureCreated();
        }
        public string Path { get; }
        public NetArcaWsModelOptions ModelOptions { get; }
        public AdjustableTimeProvider Clock { get; }
        public IDbContextFactory<ArcaWsDbContext> Factory { get; }
        public IInvoiceJournal Journal { get; }
        public void Dispose()
        {
            provider.Dispose();
            SqliteConnection.ClearAllPools();
            if (File.Exists(Path)) File.Delete(Path);
        }
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset now = now;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now += duration;
    }
}
