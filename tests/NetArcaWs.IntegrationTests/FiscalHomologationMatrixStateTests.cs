using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Cryptography;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class FiscalHomologationMatrixStateTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Verified_series_summaries_are_bounded_and_survive_reopen()
    {
        var store = new MatrixStateStore(Now);
        var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, new FixedClock(Now));

        await state.RecordVerifiedSeriesAsync(new("wsfe", ArcaEnvironment.Homologation, 20123456786, 7, 6, 12, Now), Ct);
        var reopened = await ProtectedHomologationStateSession.OpenAsync(store, Ct, new FixedClock(Now));

        var summary = await reopened.FindVerifiedSeriesAsync("wsfe", ArcaEnvironment.Homologation, 20123456786, 7, 6, Ct);
        Assert.Equal(12, summary?.LastAuthorizedNumber);
        Assert.DoesNotContain("20123456786", summary!.ToString(), StringComparison.Ordinal);
        Assert.Single(await reopened.ListVerifiedSeriesAsync(Ct));
    }

    [Fact]
    public async Task Legacy_v1_state_without_series_property_remains_readable()
    {
        const string legacyJson = "{\"Version\":1,\"SavedAtUtc\":\"2026-10-12T12:00:00+00:00\",\"Tickets\":[],\"LoginGuards\":[],\"JournalSnapshotBase64\":null}";
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(Encoding.UTF8.GetBytes(legacyJson));
        var store = new MatrixStateStore(Now, Convert.ToBase64String(compressed.ToArray()), true);

        var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, new FixedClock(Now));

        Assert.Empty(await state.ListVerifiedSeriesAsync(Ct));
    }

    [Fact]
    public async Task Protected_state_rejects_undefined_environment_in_series_summary()
    {
        const string json = "{\"Version\":1,\"SavedAtUtc\":\"2026-10-12T12:00:00+00:00\",\"Tickets\":[],\"LoginGuards\":[],\"JournalSnapshotBase64\":null,\"VerifiedSeriesSummaries\":[{\"Service\":\"wsfe\",\"Environment\":999,\"Cuit\":20123456786,\"PointOfSale\":7,\"VoucherType\":6,\"LastAuthorizedNumber\":1,\"ConfirmedAtUtc\":\"2026-10-12T12:00:00+00:00\"}]}";
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(Encoding.UTF8.GetBytes(json));
        var store = new MatrixStateStore(Now, Convert.ToBase64String(compressed.ToArray()), true);

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ProtectedHomologationStateSession.OpenAsync(store, Ct, new FixedClock(Now)));

        Assert.Contains("malformed", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Encoded_state_over_48_kib_fails_closed_before_any_followup_channel_call()
    {
        string root = Path.Combine(Path.GetTempPath(), $"matrix-cap-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var store = new MatrixStateStore(Now);
            var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, new FixedClock(Now));

            await Assert.ThrowsAsync<InvalidOperationException>(() => state.StoreJournalSnapshotAsync(RandomNumberGenerator.GetBytes(40_000), Ct));
            Assert.Equal(1, store.SaveCount);

            var journal = new SqliteInvoiceJournal(Path.Combine(root, "journal.sqlite"));
            var tenant = CreateTenant();
            var channel = new FakeChannel(journal);
            await Assert.ThrowsAsync<InvalidOperationException>(() => FiscalHomologationMatrix.RunAsync("workflow-56",
                [new("test", 6, "invoice-b")], tenant, journal, state,
                new Dictionary<string, IFiscalHomologationChannel> { ["test"] = channel },
                new Dictionary<string, int> { ["test"] = 7 }, new FiscalHomologationReceiver(80, 0, 5, true), Ct));

            Assert.Equal(0, channel.CatalogueQueries);
            Assert.Equal(0, channel.LastQueries);
            Assert.Equal(0, channel.Submissions);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task Channel_key_must_match_adapter_service_before_catalogue_query()
    {
        string root = Path.Combine(Path.GetTempPath(), $"matrix-channel-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var journal = new SqliteInvoiceJournal(Path.Combine(root, "journal.sqlite"));
            var state = await ProtectedHomologationStateSession.OpenAsync(new MatrixStateStore(Now), Ct, new FixedClock(Now));
            var tenant = CreateTenant();
            var misrouted = new FakeChannel(journal, "wsfex");

            InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => FiscalHomologationMatrix.RunAsync("workflow-57",
                [new("wsfe", 6, "invoice-b")], tenant, journal, state,
                new Dictionary<string, IFiscalHomologationChannel> { ["wsfe"] = misrouted },
                new Dictionary<string, int> { ["wsfe"] = 7 }, new FiscalHomologationReceiver(80, 0, 5, true), Ct));

            Assert.Contains("identifies as", failure.Message, StringComparison.Ordinal);
            Assert.Equal(0, misrouted.CatalogueQueries);
            Assert.Equal(0, misrouted.LastQueries);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task Stable_run_reuses_its_frozen_number_and_new_run_uses_next_number()
    {
        string root = Path.Combine(Path.GetTempPath(), $"matrix-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var journal = new SqliteInvoiceJournal(Path.Combine(root, "journal.sqlite"));
            var store = new MatrixStateStore(Now);
            var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, new FixedClock(Now));
            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=synthetic-fixture", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using X509Certificate2 certificate = request.CreateSelfSigned(Now.AddDays(-1), Now.AddDays(1));
            WsaaCertificateContent material = WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), rsa.ExportPkcs8PrivateKeyPem());
            var tenant = new ArcaTenantContext("tenant-fixture", 20123456786, ArcaEnvironment.Homologation, material);
            var channel = new FakeChannel(journal);
            var scenarios = new[] { new FiscalHomologationScenario("test", 6, "invoice-b") };
            var channels = new Dictionary<string, IFiscalHomologationChannel> { ["test"] = channel };
            var points = new Dictionary<string, int> { ["test"] = 7 };
            var receiver = new FiscalHomologationReceiver(80, 0, 5, FixtureReviewed: true);

            var first = await FiscalHomologationMatrix.RunAsync("workflow-44", scenarios, tenant, journal, state, channels, points, receiver, Ct);
            var replay = await FiscalHomologationMatrix.RunAsync("workflow-44", scenarios, tenant, journal, state, channels, points, receiver, Ct);
            await Assert.ThrowsAsync<InvoiceConflictException>(() => FiscalHomologationMatrix.RunAsync("workflow-44", scenarios,
                tenant, journal, state, channels, points, new FiscalHomologationReceiver(80, 123, 5, true), Ct));
            var secondRun = await FiscalHomologationMatrix.RunAsync("workflow-45", scenarios, tenant, journal, state, channels, points, receiver, Ct);

            Assert.Equal(1, first[0].Number);
            Assert.True(replay[0].Reused);
            Assert.Equal(first[0].Number, replay[0].Number);
            Assert.Equal(2, secondRun[0].Number);
            Assert.Equal(2, channel.Submissions);
            Assert.Equal(3, channel.ExactQueries);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task Existing_pending_operation_stops_matrix_before_number_query()
    {
        string root = Path.Combine(Path.GetTempPath(), $"matrix-pending-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var journal = new SqliteInvoiceJournal(Path.Combine(root, "journal.sqlite"));
            var store = new MatrixStateStore(Now);
            var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, new FixedClock(Now));
            var tenant = CreateTenant();
            await journal.PrepareAsync(new(tenant.TenantId, "prior-pending", "other",
                new(tenant.Environment, tenant.Cuit, 55, 1, 1), "frozen"), Ct);
            var channel = new FakeChannel(journal);

            await Assert.ThrowsAsync<InvalidOperationException>(() => FiscalHomologationMatrix.RunAsync("workflow-50",
                [new("test", 6, "invoice-b")], tenant, journal, state,
                new Dictionary<string, IFiscalHomologationChannel> { ["test"] = channel },
                new Dictionary<string, int> { ["test"] = 7 }, new FiscalHomologationReceiver(80, 0, 5, true), Ct));

            Assert.Equal(0, channel.LastQueries);
            Assert.Equal(0, channel.Submissions);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task Protected_journal_persistence_failure_prevents_channel_send()
    {
        string root = Path.Combine(Path.GetTempPath(), $"matrix-savefail-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var store = new MatrixStateStore(Now);
            var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, new FixedClock(Now));
            var journal = await ProtectedSqliteInvoiceJournal.CreateAsync(state, Path.Combine(root, "private"), new FixedClock(Now), Ct);
            store.FailOnSave = store.SaveCount + 1;
            var tenant = CreateTenant();
            var channel = new FakeChannel(journal);

            await Assert.ThrowsAsync<InvalidOperationException>(() => FiscalHomologationMatrix.RunAsync("workflow-51",
                [new("test", 6, "invoice-b")], tenant, journal, state,
                new Dictionary<string, IFiscalHomologationChannel> { ["test"] = channel },
                new Dictionary<string, int> { ["test"] = 7 }, new FiscalHomologationReceiver(80, 0, 5, true), Ct));

            Assert.Equal(0, channel.Submissions);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task Note_factory_receives_only_the_previously_authorized_same_family_invoice()
    {
        string root = Path.Combine(Path.GetTempPath(), $"matrix-assoc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var journal = new SqliteInvoiceJournal(Path.Combine(root, "journal.sqlite"));
            var state = await ProtectedHomologationStateSession.OpenAsync(new MatrixStateStore(Now), Ct, new FixedClock(Now));
            var tenant = CreateTenant();
            var channel = new FakeChannel(journal);
            var results = await FiscalHomologationMatrix.RunAsync("workflow-52",
                [new("test", 6, "invoice-a"), new("test", 7, "debit-a", "invoice-a")], tenant, journal, state,
                new Dictionary<string, IFiscalHomologationChannel> { ["test"] = channel },
                new Dictionary<string, int> { ["test"] = 7 }, new FiscalHomologationReceiver(80, 0, 5, true), Ct);

            Assert.Equal(6, channel.AssociatedInvoice?.VoucherType);
            Assert.Equal(results[0].Number, channel.AssociatedInvoice?.VoucherNumber);
            Assert.Equal(2, channel.Submissions);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task Different_service_channels_may_use_same_explicit_pos_for_distinct_types()
    {
        string root = Path.Combine(Path.GetTempPath(), $"matrix-pos-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var journal = new SqliteInvoiceJournal(Path.Combine(root, "journal.sqlite"));
            var state = await ProtectedHomologationStateSession.OpenAsync(new MatrixStateStore(Now), Ct, new FixedClock(Now));
            var tenant = CreateTenant();
            var wsfe = new FakeChannel(journal, "wsfe");
            var wsfex = new FakeChannel(journal, "wsfex");
            var results = await FiscalHomologationMatrix.RunAsync("workflow-53",
                [new("wsfe", 6, "invoice-b"), new("wsfex", 19, "export-e")], tenant, journal, state,
                new Dictionary<string, IFiscalHomologationChannel> { ["wsfe"] = wsfe, ["wsfex"] = wsfex },
                new Dictionary<string, int> { ["wsfe"] = 7, ["wsfex"] = 7 },
                new FiscalHomologationReceiver(80, 0, 5, true), Ct);

            Assert.Equal(2, results.Count);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task Journal_rejects_same_fiscal_identity_across_different_service_channels()
    {
        string root = Path.Combine(Path.GetTempPath(), $"matrix-crossservice-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var journal = new SqliteInvoiceJournal(Path.Combine(root, "journal.sqlite"));
            var state = await ProtectedHomologationStateSession.OpenAsync(new MatrixStateStore(Now), Ct, new FixedClock(Now));
            var tenant = CreateTenant();
            var wsfe = new FakeChannel(journal, "wsfe");
            var wsfex = new FakeChannel(journal, "wsfex");
            await FiscalHomologationMatrix.RunAsync("workflow-54",
                [new("wsfe", 6, "wsfe-b")], tenant, journal, state,
                new Dictionary<string, IFiscalHomologationChannel> { ["wsfe"] = wsfe },
                new Dictionary<string, int> { ["wsfe"] = 7 }, new FiscalHomologationReceiver(80, 0, 5, true), Ct);

            await Assert.ThrowsAsync<InvoiceConflictException>(() => FiscalHomologationMatrix.RunAsync("workflow-55",
                [new("wsfex", 6, "fex-type-six")], tenant, journal, state,
                new Dictionary<string, IFiscalHomologationChannel> { ["wsfex"] = wsfex },
                new Dictionary<string, int> { ["wsfex"] = 7 }, new FiscalHomologationReceiver(80, 0, 5, true), Ct));
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static ArcaTenantContext CreateTenant()
    {
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=synthetic-fixture", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(Now.AddDays(-1), Now.AddDays(1));
        return new ArcaTenantContext("tenant-fixture", 20123456786, ArcaEnvironment.Homologation,
            WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), rsa.ExportPkcs8PrivateKeyPem()));
    }

    private sealed class MatrixStateStore(DateTimeOffset now, string? initialValue = null, bool exists = false) : IProtectedHomologationStateStore
    {
        private string? value = initialValue;
        private bool isExists = exists;
        public int SaveCount { get; private set; }
        public int? FailOnSave { get; set; }
        public Task<ProtectedHomologationSecretRead> ReadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(isExists
                ? new ProtectedHomologationSecretRead(value, true, now)
                : new ProtectedHomologationSecretRead(value, false, null));
        public Task SaveAsync(string encodedValue, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            if (SaveCount == FailOnSave) throw new InvalidOperationException("synthetic protected-state persistence failure");
            value = encodedValue;
            isExists = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeChannel(IInvoiceJournal journal, string service = "test") : IFiscalHomologationChannel
    {
        private readonly Dictionary<int, long> lastByType = [];
        public string Service => service;
        public int Submissions { get; private set; }
        public int ExactQueries { get; private set; }
        public int LastQueries { get; private set; }
        public int CatalogueQueries { get; private set; }
        public InvoiceIdentity? AssociatedInvoice { get; private set; }
        public bool Supports(int voucherType) => voucherType is 6 or 7 or 19;
        public bool CanAssociate(int noteType, int originalType) => noteType == 7 && originalType == 6;
        public void ValidateScenario(FiscalHomologationScenario scenario, int pointOfSale, FiscalHomologationReceiver receiver)
        {
            if (scenario.VoucherType == 7 && scenario.OriginalScenarioName is null)
                throw new InvalidOperationException("Debit note fixture requires an original invoice.");
        }
        public void ValidateFrozenSubmission(InvoiceSubmission frozen, FiscalHomologationScenario scenario, int pointOfSale,
            FiscalHomologationReceiver receiver, InvoiceIdentity? associatedInvoice)
        {
            string association = associatedInvoice is null ? "none" : $"{associatedInvoice.VoucherType}:{associatedInvoice.PointOfSale}:{associatedInvoice.VoucherNumber}";
            string expectedPayload = $"fixture:{receiver.DocumentType}:{receiver.DocumentNumber}:{receiver.VatConditionId}:{association}";
            if (frozen.Payload != expectedPayload)
                throw new InvoiceConflictException("Synthetic fixture changed.");
        }
        public Task<bool> IsVoucherTypeAvailableAsync(ArcaTenantContext tenant, int voucherType, CancellationToken cancellationToken)
        {
            CatalogueQueries++;
            return Task.FromResult(true);
        }
        public Task<long> QueryLastAuthorizedAsync(ArcaTenantContext tenant, int pointOfSale, int voucherType, CancellationToken cancellationToken)
        {
            LastQueries++;
            return Task.FromResult(lastByType.GetValueOrDefault(voucherType));
        }
        public InvoiceSubmission CreateSubmission(ArcaTenantContext tenant, string key, int pointOfSale, int voucherType, long number, FiscalHomologationReceiver receiver, InvoiceIdentity? associatedInvoice)
        {
            AssociatedInvoice = associatedInvoice;
            string association = associatedInvoice is null ? "none" : $"{associatedInvoice.VoucherType}:{associatedInvoice.PointOfSale}:{associatedInvoice.VoucherNumber}";
            return new(tenant.TenantId, key, Service, new(tenant.Environment, tenant.Cuit, pointOfSale, voucherType, number),
                $"fixture:{receiver.DocumentType}:{receiver.DocumentNumber}:{receiver.VatConditionId}:{association}");
        }
        public async Task<InvoiceOperation> SubmitOrResumeAsync(ArcaTenantContext tenant, string key, InvoiceSubmission frozen, CancellationToken cancellationToken)
        {
            Submissions++;
            var prepared = await journal.PrepareAsync(frozen, cancellationToken);
            var lease = await journal.TryAcquireAsync(tenant.TenantId, key, TimeSpan.FromMinutes(1), false, cancellationToken);
            if (lease is null) return prepared;
            var result = await journal.CompleteAsync(lease, new(InvoiceState.Authorized, "12345678901234"), cancellationToken);
            lastByType[frozen.Identity.VoucherType] = Math.Max(lastByType.GetValueOrDefault(frozen.Identity.VoucherType), frozen.Identity.VoucherNumber);
            return result;
        }
        public async Task<InvoiceOperation> ReconcileAsync(ArcaTenantContext tenant, string key, CancellationToken cancellationToken)
            => await journal.FindAsync(tenant.TenantId, key, cancellationToken)
                ?? throw new KeyNotFoundException("Expected synthetic operation was not journaled.");
        public Task VerifyExactAsync(ArcaTenantContext tenant, InvoiceOperation operation, CancellationToken cancellationToken)
        {
            ExactQueries++;
            return Task.CompletedTask;
        }
    }
}
