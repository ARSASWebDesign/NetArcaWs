using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Cryptography;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.HealthChecks;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.Tests;

public sealed class EfArcaCertificateStoreTests
{
    [Fact]
    public async Task CertificateTablesAreOptInAndSelectionChangesFingerprint()
    {
        NetArcaWsModelOptions off = NetArcaWsModelOptions.Configure(_ => { });
        NetArcaWsModelOptions on = NetArcaWsModelOptions.Configure(x => x.AddCertificates());
        off.Fingerprint.Should().NotBe(on.Fingerprint);
        using var offDb = new ArcaWsDbContext(new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite("Data Source=:memory:").Options, off);
        using var onDb = new ArcaWsDbContext(new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite("Data Source=:memory:").Options, on);
        offDb.Model.GetEntityTypes().Should().BeEmpty();
        onDb.Model.GetEntityTypes().Select(x => x.GetTableName()).Should().Contain("NetArcaCertificateSlots").And.Contain("NetArcaCertificateVersions");
        await offDb.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        int tableCount = await offDb.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE type='table'")
            .SingleAsync(TestContext.Current.CancellationToken);
        tableCount.Should().Be(0);
    }

    [Fact]
    public async Task StoreEncryptsRoundTripsExactHistoryAndUsesCompareAndSwap()
    {
        string path = Path.Combine(Path.GetTempPath(), $"arca-certs-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(x => x.AddCertificates());
        byte[] key = RandomNumberGenerator.GetBytes(32);
        using var protector = new AesGcmArcaCertificateProtector("k1", new Dictionary<string, byte[]> { ["k1"] = key });
        var factory = new TestFactory(path, model);
        var store = new EfArcaCertificateStore<ArcaWsDbContext>(factory, model, protector);
        var scope = new ArcaCertificateScope("租户-Tenant-A", 20_123_456_789, ArcaEnvironment.Production);
        using CertificateFixture certA = CertificateFixture.Create("CN=store-a");
        using CertificateFixture certB = CertificateFixture.Create("CN=store-b");
        try
        {
            await using (ArcaWsDbContext db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            ArcaCertificateVersion first = await store.RotateAsync(scope, certA.Content, cancellationToken: TestContext.Current.CancellationToken);
            ArcaStoredCertificate loadedFirst = (await store.GetActiveAsync(scope, TestContext.Current.CancellationToken))!;
            loadedFirst.Metadata.VersionId.Should().Be(first.VersionId);
            loadedFirst.Content.ToString().Should().NotContain("PRIVATE KEY");
            string firstFingerprint = await ReadCiphertextCheck(path);
            firstFingerprint.Should().NotContain(certA.Content.ToString());
            (await store.ListVersionsAsync(scope, TestContext.Current.CancellationToken)).Should().ContainSingle().Which.IsActive.Should().BeTrue();
            Func<Task> createOnlyConflict = () => store.RotateAsync(scope, certB.Content, cancellationToken: TestContext.Current.CancellationToken);
            await createOnlyConflict.Should().ThrowAsync<ArcaCertificateConcurrencyException>();
            (await store.ListVersionsAsync(scope, TestContext.Current.CancellationToken)).Should().ContainSingle();

            ArcaCertificateVersion second = await store.RotateAsync(scope, certB.Content, first.VersionId, TestContext.Current.CancellationToken);
            second.VersionId.Should().NotBe(first.VersionId);
            (await store.GetVersionAsync(scope, first.VersionId, TestContext.Current.CancellationToken))!.Metadata.VersionId.Should().Be(first.VersionId);
            (await store.GetActiveAsync(scope, TestContext.Current.CancellationToken))!.Metadata.VersionId.Should().Be(second.VersionId);
            Func<Task> stale = () => store.RotateAsync(scope, certA.Content, first.VersionId, TestContext.Current.CancellationToken);
            await stale.Should().ThrowAsync<ArcaCertificateConcurrencyException>();
            (await store.ListVersionsAsync(scope, TestContext.Current.CancellationToken)).Should().HaveCount(2);
            (await store.GetActiveAsync(new ArcaCertificateScope("tenant-a", scope.Cuit, scope.Environment), TestContext.Current.CancellationToken)).Should().BeNull();

            string backup = path + ".backup"; File.Copy(path, backup);
            try
            {
                var restoredFactory = new TestFactory(backup, model);
                using var restoredProtector = new AesGcmArcaCertificateProtector("k1", new Dictionary<string, byte[]> { ["k1"] = key });
                var restoredStore = new EfArcaCertificateStore<ArcaWsDbContext>(restoredFactory, model, restoredProtector);
                (await restoredStore.GetActiveAsync(scope, TestContext.Current.CancellationToken))!.Metadata.VersionId.Should().Be(second.VersionId);
                using var missingKey = new AesGcmArcaCertificateProtector("other", new Dictionary<string, byte[]> { ["other"] = RandomNumberGenerator.GetBytes(32) });
                var unavailableStore = new EfArcaCertificateStore<ArcaWsDbContext>(restoredFactory, model, missingKey);
                Func<Task> unavailable = async () => await unavailableStore.GetActiveAsync(scope, TestContext.Current.CancellationToken);
                await unavailable.Should().ThrowAsync<ArcaCertificateDataException>();
            }
            finally { try { File.Delete(backup); } catch { } }
        }
        finally { try { File.Delete(path); } catch { } }
    }

    [Fact]
    public async Task StoreRetainsInvalidCertificateMetadataButRefusesItsContent()
    {
        string path = Path.Combine(Path.GetTempPath(), $"arca-certs-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(x => x.AddCertificates());
        using var protector = new AesGcmArcaCertificateProtector("k", new Dictionary<string, byte[]> { ["k"] = RandomNumberGenerator.GetBytes(32) });
        var time = new FixedTimeProvider(DateTimeOffset.UtcNow);
        var factory = new TestFactory(path, model);
        var store = new EfArcaCertificateStore<ArcaWsDbContext>(factory, model, protector, time);
        var scope = new ArcaCertificateScope("expiry", 20_123_456_789, ArcaEnvironment.Homologation);
        using CertificateFixture expired = CertificateFixture.Create("CN=expired", DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow.AddDays(-2));
        try
        {
            await using (ArcaWsDbContext db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            ArcaCertificateVersion version = await store.RotateAsync(scope, expired.Content, cancellationToken: TestContext.Current.CancellationToken);
            (await store.ListVersionsAsync(scope, TestContext.Current.CancellationToken)).Should().ContainSingle().Which.VersionId.Should().Be(version.VersionId);
            Func<Task> retrieval = async () => await store.GetActiveAsync(scope);
            await retrieval.Should().ThrowAsync<ArcaCertificateValidityException>();
        }
        finally { try { File.Delete(path); } catch { } }
    }

    [Fact]
    public async Task ExpiryLookupUsesActiveScopeAndInclusiveWarningBoundary()
    {
        string path = Path.Combine(Path.GetTempPath(), $"arca-certs-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(x => x.AddCertificates());
        using var protector = new AesGcmArcaCertificateProtector("k", new Dictionary<string, byte[]> { ["k"] = RandomNumberGenerator.GetBytes(32) });
        DateTimeOffset asOf = DateTimeOffset.UtcNow;
        var time = new FixedTimeProvider(asOf);
        var factory = new TestFactory(path, model);
        var store = new EfArcaCertificateStore<ArcaWsDbContext>(factory, model, protector, time);
        var scope = new ArcaCertificateScope("scope-one", 20_123_456_789, ArcaEnvironment.Homologation);
        using CertificateFixture expiring = CertificateFixture.Create("CN=expiring", asOf.AddHours(-1), asOf.AddHours(2));
        try
        {
            await using (ArcaWsDbContext db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            ArcaCertificateVersion version = await store.RotateAsync(scope, expiring.Content, cancellationToken: TestContext.Current.CancellationToken);
            (await store.FindExpiringAsync(scope, TimeSpan.FromHours(2), version.NotAfterUtc.AddHours(-2), TestContext.Current.CancellationToken))
                .Should().ContainSingle().Which.VersionId.Should().Be(version.VersionId);
            (await store.FindExpiringAsync(scope, TimeSpan.FromHours(1), version.NotAfterUtc.AddHours(-2), TestContext.Current.CancellationToken)).Should().BeEmpty();
            (await store.FindExpiringAsync(new ArcaCertificateScope("scope-two", scope.Cuit, scope.Environment), TimeSpan.FromHours(2),
                version.NotAfterUtc.AddHours(-2), TestContext.Current.CancellationToken)).Should().BeEmpty();
        }
        finally { try { File.Delete(path); } catch { } }
    }

    [Fact]
    public async Task AlteredMetadataFailsAuthenticationWithoutExposingStoredValues()
    {
        string path = Path.Combine(Path.GetTempPath(), $"arca-certs-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(x => x.AddCertificates());
        using var protector = new AesGcmArcaCertificateProtector("k", new Dictionary<string, byte[]> { ["k"] = RandomNumberGenerator.GetBytes(32) });
        var factory = new TestFactory(path, model);
        var store = new EfArcaCertificateStore<ArcaWsDbContext>(factory, model, protector);
        var scope = new ArcaCertificateScope("aad-scope", 20_123_456_789, ArcaEnvironment.Homologation);
        using CertificateFixture fixture = CertificateFixture.Create("CN=aad");
        try
        {
            await using (ArcaWsDbContext db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken)) await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            await store.RotateAsync(scope, fixture.Content, cancellationToken: TestContext.Current.CancellationToken);
            var options = new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite($"Data Source={path}").Options;
            await using (var tamper = new ArcaWsDbContext(options, model))
                await tamper.Database.ExecuteSqlRawAsync("UPDATE NetArcaCertificateVersions SET ThumbprintSha256 = '0000000000000000000000000000000000000000000000000000000000000000'", TestContext.Current.CancellationToken);
            Func<Task> load = async () => await store.GetActiveAsync(scope, TestContext.Current.CancellationToken);
            Exception failure = (await load.Should().ThrowAsync<ArcaCertificateDataException>()).Which;
            failure.Message.Should().NotContain(scope.TenantId).And.NotContain("PRIVATE KEY");
        }
        finally { try { File.Delete(path); } catch { } }
    }

    [Fact]
    public async Task CompetingRotationsHaveOneWinnerAndNoOrphanVersion()
    {
        string path = Path.Combine(Path.GetTempPath(), $"arca-certs-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(x => x.AddCertificates());
        using var protector = new AesGcmArcaCertificateProtector("k", new Dictionary<string, byte[]> { ["k"] = RandomNumberGenerator.GetBytes(32) });
        var factoryA = new TestFactory(path, model); var factoryB = new TestFactory(path, model);
        var storeA = new EfArcaCertificateStore<ArcaWsDbContext>(factoryA, model, protector);
        var storeB = new EfArcaCertificateStore<ArcaWsDbContext>(factoryB, model, protector);
        var scope = new ArcaCertificateScope("concurrent", 20_123_456_789, ArcaEnvironment.Homologation);
        using CertificateFixture initial = CertificateFixture.Create("CN=concurrent-initial");
        using CertificateFixture nextA = CertificateFixture.Create("CN=concurrent-a");
        using CertificateFixture nextB = CertificateFixture.Create("CN=concurrent-b");
        try
        {
            await using (ArcaWsDbContext db = await factoryA.CreateDbContextAsync(TestContext.Current.CancellationToken)) await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            ArcaCertificateVersion head = await storeA.RotateAsync(scope, initial.Content, cancellationToken: TestContext.Current.CancellationToken);
            Task<ArcaCertificateVersion> a = storeA.RotateAsync(scope, nextA.Content, head.VersionId, TestContext.Current.CancellationToken);
            Task<ArcaCertificateVersion> b = storeB.RotateAsync(scope, nextB.Content, head.VersionId, TestContext.Current.CancellationToken);
            Task all = Task.WhenAll(a, b);
            try { await all; } catch (ArcaCertificateConcurrencyException) { }
            int winners = (a.Status == TaskStatus.RanToCompletion ? 1 : 0) + (b.Status == TaskStatus.RanToCompletion ? 1 : 0);
            winners.Should().Be(1);
            (await storeA.ListVersionsAsync(scope, TestContext.Current.CancellationToken)).Should().HaveCount(2);
        }
        finally { try { File.Delete(path); } catch { } }
    }

    [Fact]
    public async Task StoreRejectsContextWithDifferentModelFingerprint()
    {
        NetArcaWsModelOptions selected = NetArcaWsModelOptions.Configure(x => x.AddCertificates());
        NetArcaWsModelOptions mismatched = NetArcaWsModelOptions.Configure(x => x.AddCertificates().AddInvoicing(ArcaService.Wsfev1));
        using var protector = new AesGcmArcaCertificateProtector("k", new Dictionary<string, byte[]> { ["k"] = RandomNumberGenerator.GetBytes(32) });
        var factory = new TestFactory(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db"), mismatched);
        var store = new EfArcaCertificateStore<ArcaWsDbContext>(factory, selected, protector);
        Func<Task> query = async () => await store.GetActiveAsync(new ArcaCertificateScope("mismatch", 20_123_456_789, ArcaEnvironment.Production), TestContext.Current.CancellationToken);
        await query.Should().ThrowAsync<InvalidOperationException>().WithMessage("The EF context model selection does not match the certificate store selection.");
    }

    [Fact]
    public void CertificateStoreRegistrationRequiresProtectorOnlyWhenSelected()
    {
        NetArcaWsModelOptions certificates = NetArcaWsModelOptions.Configure(x => x.AddCertificates());
        var services = new ServiceCollection();
        Action missing = () => services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(certificates);
        missing.Should().Throw<InvalidOperationException>().WithMessage("*IArcaCertificateProtector*");

        services.AddSingleton<IArcaCertificateProtector>(new AesGcmArcaCertificateProtector("k", new Dictionary<string, byte[]> { ["k"] = RandomNumberGenerator.GetBytes(32) }));
        services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(certificates);
        services.Any(x => x.ServiceType == typeof(IArcaCertificateStore)).Should().BeTrue();
    }

    private static async Task<string> ReadCiphertextCheck(string path)
    {
        var options = new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
        await using var db = new ArcaWsDbContext(options, NetArcaWsModelOptions.Configure(x => x.AddCertificates()));
        return await db.Database.SqlQueryRaw<string>("SELECT hex(Ciphertext) AS Value FROM NetArcaCertificateVersions LIMIT 1").SingleAsync();
    }

    private sealed class TestFactory(string path, NetArcaWsModelOptions model) : IDbContextFactory<ArcaWsDbContext>
    {
        private readonly DbContextOptions<ArcaWsDbContext> options = new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
        public ArcaWsDbContext CreateDbContext() => new(options, model);
        public Task<ArcaWsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }

    private sealed class FixedTimeProvider(DateTimeOffset time) : TimeProvider { public override DateTimeOffset GetUtcNow() => time; }

    private sealed class CertificateFixture(X509Certificate2 cert, RSA key, WsaaCertificateContent content) : IDisposable
    {
        public WsaaCertificateContent Content { get; } = content;
        public void Dispose() { cert.Dispose(); key.Dispose(); }
        public static CertificateFixture Create(string name, DateTimeOffset? from = null, DateTimeOffset? to = null)
        {
            RSA key = RSA.Create(2048); var req = new CertificateRequest(name, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            X509Certificate2 cert = req.CreateSelfSigned(from ?? DateTimeOffset.UtcNow.AddDays(-1), to ?? DateTimeOffset.UtcNow.AddDays(1));
            return new(cert, key, WsaaCertificateContent.FromPem(cert.ExportCertificatePem(), key.ExportPkcs8PrivateKeyPem()));
        }
    }
}
