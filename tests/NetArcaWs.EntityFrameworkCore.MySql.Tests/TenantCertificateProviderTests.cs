using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Cryptography;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.Migrations;
using NetArcaWs.HealthChecks;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.MySql.Tests;

public sealed class TenantCertificateProviderTests
{
    [Fact]
    public async Task Selected_mysql_or_mariadb_migration_roundtrips_certificates_and_serializes_competing_rotations()
    {
        bool configured = new[] { "NETARCA_PERSISTENCE_DB", "NETARCA_PERSISTENCE_DB_KIND", "NETARCA_PERSISTENCE_DB_VERSION" }
            .Any(name => Environment.GetEnvironmentVariable(name) is not null);
        Assert.SkipWhen(!configured, "Opt-in MySQL/MariaDB tenant certificate migration and store test.");
        PersistenceDbSettings settings = PersistenceDbSettings.ReadRequired();
        Assert.SkipWhen(settings.Kind is not ("mysql" or "mariadb"), "This suite targets MySQL/MariaDB.");

        byte[] externalKey = RandomNumberGenerator.GetBytes(32);
        await using MySqlTestDatabase database = await MySqlTestDatabase.CreateAsync(settings, builder => builder.AddCertificates(), certificateProtectionKey: externalKey);
        (await database.CreateOfficialMigrator(NetArcaWsModelOptions.Configure(builder => builder.AddCertificates()))
            .GetStatusAsync(TestContext.Current.CancellationToken)).Modules.Should().ContainSingle()
            .Which.Applied.Should().ContainSingle().Which.Should().Be("20261006000300_InitialTenantCertificates");

        IArcaCertificateStore store = database.Services.GetRequiredService<IArcaCertificateStore>();
        var scope = new ArcaCertificateScope("provider-fixture-" + Guid.NewGuid().ToString("N"), 20_123_456_789, ArcaEnvironment.Homologation);
        WsaaCertificateContent firstContent = CreateContent("CN=provider-first");
        WsaaCertificateContent secondContent = CreateContent("CN=provider-second");
        WsaaCertificateContent thirdContent = CreateContent("CN=provider-third");
            Task<ArcaCertificateVersion> first = store.RotateAsync(scope, firstContent, cancellationToken: TestContext.Current.CancellationToken);
            Task<ArcaCertificateVersion> second = store.RotateAsync(scope, secondContent, cancellationToken: TestContext.Current.CancellationToken);
            Task settled = Task.WhenAll(first, second);
            try { await settled; }
            catch (ArcaCertificateConcurrencyException) { }

            int initialWinners = (first.Status == TaskStatus.RanToCompletion ? 1 : 0) + (second.Status == TaskStatus.RanToCompletion ? 1 : 0);
            initialWinners.Should().Be(1);
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

            await using MySqlTestDatabase restored = await MySqlTestDatabase.CreateAsync(settings,
                builder => builder.AddCertificates(), certificateProtectionKey: externalKey);
            await CopyCertificateTablesAsync(database, restored, TestContext.Current.CancellationToken);
            (await restored.Services.GetRequiredService<IArcaCertificateStore>().GetActiveAsync(scope, TestContext.Current.CancellationToken))!
                .Metadata.VersionId.Should().Be((await store.GetActiveAsync(scope, TestContext.Current.CancellationToken))!.Metadata.VersionId);
            using var unavailableKey = new AesGcmArcaCertificateProtector("missing", new Dictionary<string, byte[]> { ["missing"] = RandomNumberGenerator.GetBytes(32) });
            var restoredStore = new EfArcaCertificateStore<ArcaWsDbContext>(
                restored.Services.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>(),
                NetArcaWsModelOptions.Configure(builder => builder.AddCertificates()), unavailableKey);
            Func<Task> missingKeyRead = async () => await restoredStore.GetActiveAsync(scope, TestContext.Current.CancellationToken);
            await missingKeyRead.Should().ThrowAsync<ArcaCertificateDataException>();
    }

    private static async Task CopyCertificateTablesAsync(MySqlTestDatabase source, MySqlTestDatabase destination, CancellationToken cancellationToken)
    {
        foreach (string table in new[] { "NetArcaCertificateSlots", "NetArcaCertificateVersions" })
        {
            List<(string[] Columns, object[] Values)> rows = await ReadRowsAsync(source, table, cancellationToken);
            await using var context = await destination.Services.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync(cancellationToken);
            await context.Database.OpenConnectionAsync(cancellationToken);
            foreach ((string[] columns, object[] values) in rows)
            {
                await using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = $"INSERT INTO `{table}` ({string.Join(", ", columns.Select(column => $"`{column}`"))}) VALUES ({string.Join(", ", columns.Select((_, index) => $"@p{index}"))})";
                for (int index = 0; index < values.Length; index++)
                {
                    var parameter = command.CreateParameter(); parameter.ParameterName = $"@p{index}"; parameter.Value = values[index]; command.Parameters.Add(parameter);
                }
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    private static async Task<List<(string[] Columns, object[] Values)>> ReadRowsAsync(MySqlTestDatabase database, string table,
        CancellationToken cancellationToken)
    {
        await using var context = await database.Services.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync(cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var command = context.Database.GetDbConnection().CreateCommand(); command.CommandText = $"SELECT * FROM `{table}`";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        string[] columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        var rows = new List<(string[], object[])>();
        while (await reader.ReadAsync(cancellationToken))
        {
            object[] values = Enumerable.Range(0, reader.FieldCount).Select(reader.GetValue).ToArray();
            rows.Add((columns, values));
        }
        return rows;
    }

    private static WsaaCertificateContent CreateContent(string subject)
    {
        using RSA key = RSA.Create(2048);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
        return WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), key.ExportPkcs8PrivateKeyPem());
    }
}
