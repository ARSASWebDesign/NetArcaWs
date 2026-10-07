using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Wsaa;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class ProtectedHomologationStateTests
{
    private static readonly DateTimeOffset InitialTime = new(2026, 10, 7, 15, 0, 0, TimeSpan.Zero);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Valid_ticket_is_reused_past_ten_minutes_until_its_actual_expiration()
    {
        var clock = new MutableTimeProvider(InitialTime);
        var store = new MemoryStateStore(clock);
        var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
        var login = new FakeWsaaLogin(clock, _ => TimeSpan.FromHours(2));
        var provider = new ProtectedWsaaTicketProvider(state, login, clock);
        var tenant = Tenant("tenant-a", 20123456786, ArcaEnvironment.Homologation, CertificateMaterial.Create());

        WsaaTicket first = await provider.GetTicketAsync("wsfe", tenant, Ct);
        clock.Advance(TimeSpan.FromMinutes(11));
        WsaaTicket second = await provider.GetTicketAsync("wsfe", tenant, Ct);

        Assert.Equal(first.Xml, second.Xml);
        Assert.True(second.ExpirationTime > clock.GetUtcNow());
        Assert.Equal(1, login.Calls);
    }

    [Fact]
    public async Task Valid_ticket_is_reused_by_a_new_session_after_runner_restart()
    {
        var clock = new MutableTimeProvider(InitialTime);
        var store = new MemoryStateStore(clock);
        var firstState = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
        var login = new FakeWsaaLogin(clock, _ => TimeSpan.FromHours(2));
        var tenant = Tenant("tenant-a", 20123456786, ArcaEnvironment.Homologation, CertificateMaterial.Create());
        WsaaTicket first = await new ProtectedWsaaTicketProvider(firstState, login, clock).GetTicketAsync("wsfe", tenant, Ct);

        var restartedState = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
        WsaaTicket reused = await new ProtectedWsaaTicketProvider(restartedState, login, clock).GetTicketAsync("wsfe", tenant, Ct);

        Assert.Equal(first.Xml, reused.Xml);
        Assert.Equal(1, login.Calls);
    }

    [Fact]
    public async Task Expired_ticket_cannot_trigger_login_at_599_seconds_but_can_at_600()
    {
        var clock = new MutableTimeProvider(InitialTime);
        var store = new MemoryStateStore(clock);
        var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
        var login = new FakeWsaaLogin(clock, call => call == 1 ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(20));
        var provider = new ProtectedWsaaTicketProvider(state, login, clock);
        var tenant = Tenant("tenant-a", 20123456786, ArcaEnvironment.Homologation, CertificateMaterial.Create());

        await provider.GetTicketAsync("wsfe", tenant, Ct);
        clock.Advance(TimeSpan.FromSeconds(599));
        InvalidOperationException cooldown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetTicketAsync("wsfe", tenant, Ct));
        Assert.Contains("cooldown is active", cooldown.Message, StringComparison.Ordinal);
        Assert.Equal(1, login.Calls);

        clock.Advance(TimeSpan.FromSeconds(1));
        await provider.GetTicketAsync("wsfe", tenant, Ct);
        Assert.Equal(2, login.Calls);
    }

    [Fact]
    public async Task Failed_login_observes_the_same_599_and_600_second_cooldown_boundary()
    {
        var clock = new MutableTimeProvider(InitialTime);
        var store = new MemoryStateStore(clock);
        var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
        var login = new FakeWsaaLogin(clock, _ => TimeSpan.FromMinutes(20)) { FailFirst = true };
        var provider = new ProtectedWsaaTicketProvider(state, login, clock);
        var tenant = Tenant("tenant-a", 20123456786, ArcaEnvironment.Homologation, CertificateMaterial.Create());

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetTicketAsync("wsfe", tenant, Ct));
        clock.Advance(TimeSpan.FromSeconds(599));
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetTicketAsync("wsfe", tenant, Ct));
        Assert.Equal(1, login.Calls);

        clock.Advance(TimeSpan.FromSeconds(1));
        await provider.GetTicketAsync("wsfe", tenant, Ct);
        Assert.Equal(2, login.Calls);
    }

    [Fact]
    public async Task Failed_start_marker_write_prevents_wsaa_login()
    {
        var clock = new MutableTimeProvider(InitialTime);
        var store = new MemoryStateStore(clock);
        var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
        store.FailOnSave = store.SaveCount + 1;
        var login = new FakeWsaaLogin(clock, _ => TimeSpan.FromMinutes(20));
        var provider = new ProtectedWsaaTicketProvider(state, login, clock);
        var tenant = Tenant("tenant-a", 20123456786, ArcaEnvironment.Homologation, CertificateMaterial.Create());

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetTicketAsync("wsfe", tenant, Ct));

        Assert.Contains("session is closed", failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, login.Calls);
    }

    [Fact]
    public async Task New_ticket_is_not_returned_when_its_persistence_fails()
    {
        var clock = new MutableTimeProvider(InitialTime);
        var store = new MemoryStateStore(clock);
        var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
        store.FailOnSave = store.SaveCount + 2; // Start marker persists; completion with the returned TA fails.
        var login = new FakeWsaaLogin(clock, _ => TimeSpan.FromMinutes(20));
        var provider = new ProtectedWsaaTicketProvider(state, login, clock);
        var tenant = Tenant("tenant-a", 20123456786, ArcaEnvironment.Homologation, CertificateMaterial.Create());

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetTicketAsync("wsfe", tenant, Ct));

        Assert.Equal(1, login.Calls);
        ProtectedHomologationStateBundle persisted = Decode(store.EncodedValue!);
        Assert.Empty(persisted.Tickets);
        Assert.Single(persisted.LoginGuards);
        Assert.Equal(InitialTime, persisted.LoginGuards[0].LastAttemptUtc);
        Assert.Equal(InitialTime.AddMinutes(11), persisted.LoginGuards[0].CooldownUntilUtc);
    }

    [Fact]
    public async Task Ticket_identity_isolated_by_tenant_cuit_environment_service_and_certificate()
    {
        var clock = new MutableTimeProvider(InitialTime);
        var store = new MemoryStateStore(clock);
        var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
        var login = new FakeWsaaLogin(clock, _ => TimeSpan.FromHours(2));
        var provider = new ProtectedWsaaTicketProvider(state, login, clock);
        CertificateMaterial firstCertificate = CertificateMaterial.Create();
        CertificateMaterial secondCertificate = CertificateMaterial.Create();
        var baseTenant = Tenant("tenant-a", 20123456786, ArcaEnvironment.Homologation, firstCertificate);
        var identities = new (string Service, ArcaTenantContext Tenant)[]
        {
            ("wsfe", baseTenant),
            ("wsfex", baseTenant),
            ("wsfe", Tenant("tenant-b", 20123456786, ArcaEnvironment.Homologation, firstCertificate)),
            ("wsfe", Tenant("tenant-a", 20123456787, ArcaEnvironment.Homologation, firstCertificate)),
            ("wsfe", Tenant("tenant-a", 20123456786, ArcaEnvironment.Homologation, secondCertificate))
        };

        foreach ((string service, ArcaTenantContext tenant) in identities)
        {
            await provider.GetTicketAsync(service, tenant, Ct);
            clock.Advance(TimeSpan.FromMinutes(11)); // Respect the cert/service login guard while checking ticket-key isolation.
        }
        await provider.GetTicketAsync("wsfe", baseTenant, Ct);

        Assert.Equal(identities.Length, login.Calls);
        ProtectedHomologationStateBundle persisted = Decode(store.EncodedValue!);
        Assert.Equal(identities.Length, persisted.Tickets.Count);
    }

    [Fact]
    public async Task Login_cooldown_is_shared_for_same_certificate_and_service_across_tenants()
    {
        var clock = new MutableTimeProvider(InitialTime);
        var store = new MemoryStateStore(clock);
        var state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
        var login = new FakeWsaaLogin(clock, _ => TimeSpan.FromMinutes(20));
        var provider = new ProtectedWsaaTicketProvider(state, login, clock);
        CertificateMaterial certificate = CertificateMaterial.Create();

        await provider.GetTicketAsync("wsfe", Tenant("tenant-a", 20123456786, ArcaEnvironment.Homologation, certificate), Ct);
        InvalidOperationException cooldown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetTicketAsync("wsfe", Tenant("tenant-b", 20123456786, ArcaEnvironment.Homologation, certificate), Ct));

        Assert.Contains("cooldown is active", cooldown.Message, StringComparison.Ordinal);
        Assert.Equal(1, login.Calls);
        ProtectedHomologationStateBundle persisted = Decode(store.EncodedValue!);
        Assert.Single(persisted.Tickets); // Ticket identity still includes tenant.
        Assert.Single(persisted.LoginGuards); // Login guard is shared by certificate/service.
    }

    [Fact]
    public async Task Existing_secret_without_injected_value_and_absent_secret_with_injected_value_fail_closed()
    {
        var clock = new MutableTimeProvider(InitialTime);
        var existingButNotInjected = new MemoryStateStore(clock)
        {
            Exists = true,
            EncodedValue = null,
            UpdatedAtUtc = InitialTime
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ProtectedHomologationStateSession.OpenAsync(existingButNotInjected, Ct, clock));

        var absentButInjected = new MemoryStateStore(clock)
        {
            Exists = false,
            EncodedValue = "unexpected-injected-state"
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ProtectedHomologationStateSession.OpenAsync(absentButInjected, Ct, clock));
    }

    [Fact]
    public async Task Malformed_oversized_and_stale_state_are_rejected()
    {
        var clock = new MutableTimeProvider(InitialTime);
        foreach (string malformed in new[] { "not-a-compressed-state", new string('x', 48 * 1024 + 1) })
        {
            var store = ExistingStore(clock, malformed, InitialTime);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ProtectedHomologationStateSession.OpenAsync(store, Ct, clock));
        }

        var staleBundle = new ProtectedHomologationStateBundle(1, InitialTime.AddMinutes(-10), [], [], null);
        var staleStore = ExistingStore(clock, Encode(staleBundle), InitialTime);
        InvalidOperationException stale = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ProtectedHomologationStateSession.OpenAsync(staleStore, Ct, clock));
        Assert.Contains("stale", stale.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoice_journal_restores_prepared_submitting_and_unknown_snapshots()
    {
        string root = Path.Combine(Path.GetTempPath(), $"netarcaws-protected-state-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var clock = new MutableTimeProvider(InitialTime);
        var store = new MemoryStateStore(clock);
        try
        {
            ProtectedHomologationStateSession state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
            ProtectedSqliteInvoiceJournal first = await OpenJournalAsync(root, "first", state, clock, Ct);
            InvoiceSubmission submission = Submission("prepared");
            await first.PrepareAsync(submission, Ct);

            ProtectedHomologationStateSession reopenedPreparedState = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
            ProtectedSqliteInvoiceJournal reopenedPrepared = await OpenJournalAsync(root, "prepared", reopenedPreparedState, clock, Ct);
            Assert.Equal(InvoiceState.Prepared, (await reopenedPrepared.FindAsync(submission.TenantId, submission.IdempotencyKey, Ct))!.State);

            InvoiceLease submitting = (await reopenedPrepared.TryAcquireAsync(
                submission.TenantId, submission.IdempotencyKey, TimeSpan.FromMinutes(2), reconciliation: false, cancellationToken: Ct))!;
            Assert.Equal(InvoiceState.Submitting, submitting.Operation.State);

            ProtectedHomologationStateSession reopenedSubmittingState = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
            ProtectedSqliteInvoiceJournal reopenedSubmitting = await OpenJournalAsync(root, "submitting", reopenedSubmittingState, clock, Ct);
            Assert.Equal(InvoiceState.Submitting, (await reopenedSubmitting.FindAsync(submission.TenantId, submission.IdempotencyKey, Ct))!.State);

            clock.Advance(TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(1)));
            InvoiceLease reconciling = (await reopenedSubmitting.TryAcquireAsync(
                submission.TenantId, submission.IdempotencyKey, TimeSpan.FromMinutes(2), reconciliation: true, cancellationToken: Ct))!;
            Assert.Equal(InvoiceState.Reconciling, reconciling.Operation.State);
            await reopenedSubmitting.CompleteAsync(reconciling, new InvoiceDecision(InvoiceState.Unknown), Ct);

            ProtectedHomologationStateSession reopenedUnknownState = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
            ProtectedSqliteInvoiceJournal reopenedUnknown = await OpenJournalAsync(root, "unknown", reopenedUnknownState, clock, Ct);
            Assert.Equal(InvoiceState.Unknown, (await reopenedUnknown.FindAsync(submission.TenantId, submission.IdempotencyKey, Ct))!.State);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Failed_journal_snapshot_write_prevents_authorization_callback()
    {
        string root = Path.Combine(Path.GetTempPath(), $"netarcaws-protected-state-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var clock = new MutableTimeProvider(InitialTime);
        var store = new MemoryStateStore(clock);
        try
        {
            ProtectedHomologationStateSession state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
            ProtectedSqliteInvoiceJournal journal = await OpenJournalAsync(root, "journal", state, clock, Ct);
            store.FailOnSave = store.SaveCount + 1;
            int submissions = 0;
            var coordinator = new InvoiceCoordinator(journal);

            await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.SubmitAsync(
                Submission("before-send"),
                _ =>
                {
                    submissions++;
                    return Task.FromResult(new InvoiceDecision(InvoiceState.Authorized, "74123456789012"));
                }, Ct));

            Assert.Equal(0, submissions);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Completion_snapshot_failure_restores_submitting_and_restart_reconciles_without_authorizing()
    {
        string root = Path.Combine(Path.GetTempPath(), $"netarcaws-protected-state-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var clock = new MutableTimeProvider(InitialTime);
        var store = new MemoryStateStore(clock);
        InvoiceSubmission submission = Submission("restart-unknown");
        int authorizations = 0;
        try
        {
            ProtectedHomologationStateSession state = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
            ProtectedSqliteInvoiceJournal journal = await OpenJournalAsync(root, "original", state, clock, Ct);
            var coordinator = new InvoiceCoordinator(journal, TimeSpan.FromMinutes(2));
            store.FailOnSave = store.SaveCount + 3; // Prepare and Submitting persist; completion persistence fails.

            await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.SubmitAsync(
                submission,
                _ =>
                {
                    authorizations++;
                    return Task.FromResult(new InvoiceDecision(InvoiceState.Authorized, "74123456789012", "<response/>"));
                }, Ct));
            Assert.Equal(1, authorizations);

            clock.Advance(TimeSpan.FromMinutes(3));
            ProtectedHomologationStateSession restartedState = await ProtectedHomologationStateSession.OpenAsync(store, Ct, clock);
            ProtectedSqliteInvoiceJournal restartedJournal = await OpenJournalAsync(root, "restarted", restartedState, clock, Ct);
            Assert.Equal(InvoiceState.Submitting,
                (await restartedJournal.FindAsync(submission.TenantId, submission.IdempotencyKey, Ct))!.State);

            int queries = 0;
            var restartedCoordinator = new InvoiceCoordinator(restartedJournal, TimeSpan.FromMinutes(2));
            InvoiceOperation recovered = await restartedCoordinator.ReconcileAsync(
                submission.TenantId,
                submission.IdempotencyKey,
                (_, _) =>
                {
                    queries++;
                    return Task.FromResult(new InvoiceDecision(InvoiceState.Unknown, ResponseXml: "<empty/>"));
                }, Ct);

            Assert.Equal(InvoiceState.Unknown, recovered.State);
            Assert.Equal(1, queries);
            Assert.Equal(1, authorizations);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<ProtectedSqliteInvoiceJournal> OpenJournalAsync(
        string root, string name, ProtectedHomologationStateSession state, TimeProvider clock, CancellationToken cancellationToken)
        => await ProtectedSqliteInvoiceJournal.CreateAsync(state, Path.Combine(root, name), clock, cancellationToken);

    private static InvoiceSubmission Submission(string key)
        => new("homologation-state-test", key, "wsfe",
            new InvoiceIdentity(ArcaEnvironment.Homologation, 20123456786, 9001, 6, 1), "<invoice/>");

    private static ArcaTenantContext Tenant(string tenantId, long cuit, ArcaEnvironment environment, CertificateMaterial material)
        => new(tenantId, cuit, environment, WsaaCertificateContent.FromPem(material.CertificatePem, material.PrivateKeyPem));

    private static MemoryStateStore ExistingStore(MutableTimeProvider clock, string encoded, DateTimeOffset updatedAt)
        => new(clock) { Exists = true, EncodedValue = encoded, UpdatedAtUtc = updatedAt };

    private static string Encode(ProtectedHomologationStateBundle bundle)
    {
        using var compressed = new MemoryStream();
        using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(JsonSerializer.SerializeToUtf8Bytes(bundle));
        return Convert.ToBase64String(compressed.ToArray());
    }

    private static ProtectedHomologationStateBundle Decode(string encoded)
    {
        using var input = new MemoryStream(Convert.FromBase64String(encoded));
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return JsonSerializer.Deserialize<ProtectedHomologationStateBundle>(output.ToArray())!;
    }

    private static string TicketXml(DateTimeOffset generatedAt, DateTimeOffset expiresAt, uint uniqueId)
        => $"""<loginTicketResponse version="1.0"><header><source>CN=wsaa</source><destination>CN=client</destination><uniqueId>{uniqueId}</uniqueId><generationTime>{generatedAt:O}</generationTime><expirationTime>{expiresAt:O}</expirationTime></header><credentials><token>synthetic-token-{uniqueId}</token><sign>synthetic-sign-{uniqueId}</sign></credentials></loginTicketResponse>""";

    private sealed class MemoryStateStore(MutableTimeProvider clock) : IProtectedHomologationStateStore
    {
        public bool Exists { get; set; }
        public string? EncodedValue { get; set; }
        public DateTimeOffset? UpdatedAtUtc { get; set; }
        public int SaveCount { get; private set; }
        public int? FailOnSave { get; set; }

        public Task<ProtectedHomologationSecretRead> ReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ProtectedHomologationSecretRead(EncodedValue, Exists, UpdatedAtUtc));
        }

        public Task SaveAsync(string encodedValue, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            if (SaveCount == FailOnSave) throw new InvalidOperationException("synthetic state save failure");
            EncodedValue = encodedValue;
            Exists = true;
            UpdatedAtUtc = clock.GetUtcNow();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWsaaLogin(MutableTimeProvider clock, Func<int, TimeSpan> lifetime) : IProtectedHomologationWsaaLogin
    {
        public int Calls { get; private set; }
        public bool FailFirst { get; init; }

        public Task<WsaaTicket> AuthenticateForTenantWithoutCacheAsync(
            string service, ArcaTenantContext tenant, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            if (FailFirst && Calls == 1) throw new HttpRequestException("synthetic WSAA failure");
            DateTimeOffset now = clock.GetUtcNow();
            return Task.FromResult(WsaaService.ParseTicket(TicketXml(now.AddSeconds(-1), now.Add(lifetime(Calls)), (uint)Calls)));
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; private set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
        public void Advance(TimeSpan duration) => Now = Now.Add(duration);
    }

    private sealed record CertificateMaterial(string CertificatePem, string PrivateKeyPem)
    {
        public static CertificateMaterial Create()
        {
            using RSA rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=protected-state-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using X509Certificate2 certificate = request.CreateSelfSigned(InitialTime.AddDays(-1), InitialTime.AddDays(365));
            return new(certificate.ExportCertificatePem(), rsa.ExportPkcs8PrivateKeyPem());
        }
    }
}
