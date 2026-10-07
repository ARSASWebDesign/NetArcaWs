using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Cryptography;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.Migrations;
using NetArcaWs.HealthChecks;
using Xunit;

namespace NetArcaWs.Persistence.Server.Tests;

public sealed class TenantCertificateProviderTests
{
    [Fact]
    public async Task Selected_postgresql_or_sqlserver_migration_roundtrips_certificates_and_serializes_competing_rotations()
    {
        PersistenceServerSettings? settings = PersistenceServerSettings.FromEnvironment();
        Assert.SkipWhen(settings is null, "Opt-in PostgreSQL/SQL Server tenant certificate migration and store test.");
        PersistenceServerSettings selected = settings!;
        Assert.SkipWhen(selected.Kind is not ("postgresql" or "sqlserver"), "This suite targets PostgreSQL/SQL Server.");

        await using ServerTestDatabase database = await ServerTestDatabase.CreateAsync(selected, builder => builder.AddCertificates());
        NetArcaWsModelOptions options = NetArcaWsModelOptions.Configure(builder => builder.AddCertificates());
        NetArcaWsMigrationStatus migration = await database.CreateOfficialMigrator(options).GetStatusAsync(TestContext.Current.CancellationToken);
        migration.Modules.Should().ContainSingle().Which.Applied.Should().ContainSingle()
            .Which.Should().Be("20261006000300_InitialTenantCertificates");

        IArcaCertificateStore store = database.Services.GetRequiredService<IArcaCertificateStore>();
        var scope = new ArcaCertificateScope("server-fixture-" + Guid.NewGuid().ToString("N"), 20_123_456_789, ArcaEnvironment.Homologation);
        WsaaCertificateContent firstContent = CreateContent("CN=server-first");
        WsaaCertificateContent secondContent = CreateContent("CN=server-second");
        WsaaCertificateContent thirdContent = CreateContent("CN=server-third");

        Task<ArcaCertificateVersion> first = store.RotateAsync(scope, firstContent, cancellationToken: TestContext.Current.CancellationToken);
        Task<ArcaCertificateVersion> second = store.RotateAsync(scope, secondContent, cancellationToken: TestContext.Current.CancellationToken);
        Task initial = Task.WhenAll(first, second);
        try { await initial; }
        catch (ArcaCertificateConcurrencyException) { }
        ((first.Status == TaskStatus.RanToCompletion ? 1 : 0) + (second.Status == TaskStatus.RanToCompletion ? 1 : 0)).Should().Be(1);

        ArcaCertificateVersion active = (await store.GetActiveAsync(scope, TestContext.Current.CancellationToken))!.Metadata;
        (await store.ListVersionsAsync(scope, TestContext.Current.CancellationToken)).Should().ContainSingle()
            .Which.VersionId.Should().Be(active.VersionId);
        Task<ArcaCertificateVersion> rotateA = store.RotateAsync(scope, secondContent, active.VersionId, TestContext.Current.CancellationToken);
        Task<ArcaCertificateVersion> rotateB = store.RotateAsync(scope, thirdContent, active.VersionId, TestContext.Current.CancellationToken);
        Task rotations = Task.WhenAll(rotateA, rotateB);
        try { await rotations; }
        catch (ArcaCertificateConcurrencyException) { }
        ((rotateA.Status == TaskStatus.RanToCompletion ? 1 : 0) + (rotateB.Status == TaskStatus.RanToCompletion ? 1 : 0)).Should().Be(1);
        (await store.ListVersionsAsync(scope, TestContext.Current.CancellationToken)).Should().HaveCount(2)
            .And.Contain(version => version.VersionId == active.VersionId && !version.IsActive)
            .And.Contain(version => version.IsActive);
        (await store.GetVersionAsync(scope, active.VersionId, TestContext.Current.CancellationToken))!.Metadata.VersionId.Should().Be(active.VersionId);
    }

    private static WsaaCertificateContent CreateContent(string subject)
    {
        using RSA key = RSA.Create(2048);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
        return WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), key.ExportPkcs8PrivateKeyPem());
    }
}
