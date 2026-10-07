using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Text;
using System.Xml.Linq;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Transport;
using NetArcaWs.Cryptography;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Wsaa;
using NetArcaWs.Invoicing;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.Tests;

public sealed class SharedWsaaTicketTests
{
    [Fact]
    public void AesGcmProtectorEncryptsAndAuthenticatesTicketPayloadAgainstIdentity()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        using var protector = new AesGcmWsaaTicketProtector("key-1", new Dictionary<string, byte[]> { ["key-1"] = key });
        CryptographicOperations.ZeroMemory(key);
        byte[] identity = Encoding.UTF8.GetBytes("cert|https://wsaahomo.afip.gov.ar/ws/services/LoginCms|wsfe");
        byte[] ticket = Encoding.UTF8.GetBytes("<loginTicketResponse><credentials><token>secret-token</token></credentials></loginTicketResponse>");

        WsaaProtectedPayload protectedPayload = protector.Protect(ticket, identity);

        Encoding.UTF8.GetString(protectedPayload.Ciphertext).Should().NotContain("secret-token");
        protector.Unprotect(protectedPayload, identity).Should().Equal(ticket);
        byte[] exposedCopy = protectedPayload.Ciphertext;
        exposedCopy[0] ^= 0x40;
        protector.Unprotect(protectedPayload, identity).Should().Equal(ticket);
        Action swappedIdentity = () => protector.Unprotect(protectedPayload, Encoding.UTF8.GetBytes("other-cert|endpoint|wsfe"));
        swappedIdentity.Should().Throw<CryptographicException>();
        byte[] tamperedCiphertext = protectedPayload.Ciphertext.ToArray();
        tamperedCiphertext[0] ^= 0x40;
        var tamperedPayload = new WsaaProtectedPayload(protectedPayload.KeyId, protectedPayload.Nonce, tamperedCiphertext, protectedPayload.Tag);
        Action tampered = () => protector.Unprotect(tamperedPayload, identity);
        tampered.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void RotatedProtectorDecryptsOldKeyAndEncryptsWithActiveKey()
    {
        byte[] oldKey = RandomNumberGenerator.GetBytes(32);
        byte[] newKey = RandomNumberGenerator.GetBytes(32);
        byte[] identity = Encoding.UTF8.GetBytes("cert|endpoint|wsfe");
        byte[] ticket = Encoding.UTF8.GetBytes("ticket xml");
        WsaaProtectedPayload oldPayload;
        using (var oldProtector = new AesGcmWsaaTicketProtector("old", new Dictionary<string, byte[]> { ["old"] = oldKey }))
            oldPayload = oldProtector.Protect(ticket, identity);

        using var rotated = new AesGcmWsaaTicketProtector("new", new Dictionary<string, byte[]>
        {
            ["old"] = oldKey,
            ["new"] = newKey
        });

        rotated.Unprotect(oldPayload, identity).Should().Equal(ticket);
        rotated.Protect(ticket, identity).KeyId.Should().Be("new");
    }

    [Fact]
    public void TicketOnlyModelContainsOnlyItsTicketTableAndSelectionAffectsFingerprint()
    {
        NetArcaWsModelOptions empty = NetArcaWsModelOptions.Configure(_ => { });
        NetArcaWsModelOptions tickets = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.Wsfev1));
        NetArcaWsModelOptions anotherSelection = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.PadronA5));
        var dbOptions = new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite("Data Source=:memory:").Options;
        using var emptyContext = new ArcaWsDbContext(dbOptions, empty);
        using var ticketContext = new ArcaWsDbContext(dbOptions, tickets);

        emptyContext.Model.GetEntityTypes().Should().BeEmpty();
        tickets.WsaaTicketServices.Should().ContainSingle().Which.Should().Be(ArcaService.Wsfev1);
        tickets.InvoicingEnabled.Should().BeFalse();
        ticketContext.Model.GetEntityTypes().Select(entity => entity.GetTableName()).Should()
            .ContainSingle().Which.Should().Be("NetArcaWsaaTickets");
        tickets.Fingerprint.Should().NotBe(anotherSelection.Fingerprint);
    }

    [Fact]
    public void WsaaItselfCannotBeSelectedAsATicketService()
    {
        Action configure = () => NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.Wsaa));
        configure.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task ThreeIndependentProvidersCoordinateOneLoginAndTicketSurvivesProviderRestart()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.Wsfev1));
        var factory = new ArcaWsFactory(databasePath, modelOptions);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.EnsureCreatedAsync(cancellationToken);
        byte[] sharedKey = RandomNumberGenerator.GetBytes(32);
        var loginClient = new BlockingLoginClient();
        using ServiceProvider firstHost = CreateProvider(factory, modelOptions, sharedKey, loginClient);
        using ServiceProvider secondHost = CreateProvider(factory, modelOptions, sharedKey, loginClient);
        using ServiceProvider thirdHost = CreateProvider(factory, modelOptions, sharedKey, loginClient);
        firstHost.GetService<IInvoiceJournal>().Should().BeNull();
        firstHost.GetRequiredService<IArcaTicketProvider>().Should().BeOfType<DistributedArcaTicketProvider<ArcaWsDbContext>>();
        using RSA privateKey = RSA.Create(2048);
        var certRequest = new CertificateRequest("CN=distributed-wsaa-test", privateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = certRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
        WsaaCertificateContent content = WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), privateKey.ExportPkcs8PrivateKeyPem());
        var firstTenant = new ArcaTenantContext("tenant-one", 20_123_456_789, ArcaEnvironment.Homologation, content);
        var secondTenant = new ArcaTenantContext("tenant-two", 20_987_654_321, ArcaEnvironment.Homologation, content);
        var thirdTenant = new ArcaTenantContext("tenant-three", 20_111_222_333, ArcaEnvironment.Homologation, content);

        var begin = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<WsaaTicket> Request(ServiceProvider provider, ArcaTenantContext tenant) => Task.Run(async () =>
        {
            await begin.Task.WaitAsync(cancellationToken);
            return await provider.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfe", tenant, cancellationToken);
        }, cancellationToken);
        Task<WsaaTicket> first = Request(firstHost, firstTenant);
        Task<WsaaTicket> second = Request(secondHost, secondTenant);
        Task<WsaaTicket> third = Request(thirdHost, thirdTenant);
        begin.TrySetResult();
        await loginClient.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        loginClient.Release.TrySetResult();
        WsaaTicket[] tickets = await Task.WhenAll(first, second, third);

        loginClient.Calls.Should().Be(1);
        tickets.Select(ticket => ticket.Token).Should().OnlyContain(token => token == "shared-token");

        var restartedLoginClient = new BlockingLoginClient();
        using ServiceProvider restartedHost = CreateProvider(factory, modelOptions, sharedKey, restartedLoginClient);
        WsaaTicket afterRestart = await restartedHost.GetRequiredService<IArcaTicketProvider>()
            .GetTicketAsync("wsfe", secondTenant, cancellationToken);
        afterRestart.Token.Should().Be("shared-token");
        restartedLoginClient.Calls.Should().Be(0);
        File.Delete(databasePath);
    }

    [Fact]
    public async Task ExpiredTicketMayStartANewLoginButUnexpiredTicketIsReusedOnlyToItsActualExpiry()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-expiry-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.Wsfev1));
        var factory = new ArcaWsFactory(databasePath, modelOptions);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.EnsureCreatedAsync(cancellationToken);
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        var loginClient = new BlockingLoginClient(clock, releaseImmediately: true, ticketLifetime: TimeSpan.FromMinutes(10));
        byte[] key = RandomNumberGenerator.GetBytes(32);
        using ServiceProvider host = CreateProvider(factory, modelOptions, key, loginClient, clock);
        using (var identity = CreateCertificate("CN=expiry-test"))
        {
            var tenant = new ArcaTenantContext("tenant", 20_123_456_789, ArcaEnvironment.Homologation, identity.Content);
            WsaaTicket first = await host.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfe", tenant, cancellationToken);
            WsaaTicket beforeExpiry = await host.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfe", tenant, cancellationToken);
            first.UniqueId.Should().Be(beforeExpiry.UniqueId);
            loginClient.Calls.Should().Be(1);

            clock.Advance(TimeSpan.FromMinutes(10));
            WsaaTicket afterExpiry = await host.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfe", tenant, cancellationToken);
            afterExpiry.UniqueId.Should().NotBe(first.UniqueId);
            loginClient.Calls.Should().Be(2);
        }
        File.Delete(databasePath);
    }

    [Fact]
    public async Task ExpiredOwnerLeaseDoesNotAllowAnotherRemoteLoginAndOriginalOwnerCanCompleteLate()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-lease-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.Wsfev1));
        var factory = new ArcaWsFactory(databasePath, modelOptions);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.EnsureCreatedAsync(cancellationToken);
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        var loginClient = new BlockingLoginClient(clock);
        byte[] key = RandomNumberGenerator.GetBytes(32);
        using ServiceProvider owner = CreateProvider(factory, modelOptions, key, loginClient, clock,
            TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(5), TimeSpan.FromMinutes(1));
        using ServiceProvider waiter = CreateProvider(factory, modelOptions, key, loginClient, clock,
            TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(5), TimeSpan.FromMinutes(1));
        using var material = CreateCertificate("CN=lease-test");
        var tenant = new ArcaTenantContext("tenant", 20_123_456_789, ArcaEnvironment.Homologation, material.Content);

        Task<WsaaTicket> ownerTask = owner.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfe", tenant, cancellationToken);
        await loginClient.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        clock.Advance(TimeSpan.FromMinutes(2));
        Func<Task> waiterCall = () => waiter.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfe", tenant, cancellationToken);
        await waiterCall.Should().ThrowAsync<WsaaTicketLoginPendingException>();
        loginClient.Calls.Should().Be(1);

        loginClient.Release.TrySetResult();
        WsaaTicket lateTicket = await ownerTask;
        lateTicket.Token.Should().Be("shared-token");
        loginClient.Calls.Should().Be(1);
        File.Delete(databasePath);
    }

    [Fact]
    public async Task FailedLoginPersistsUnknownAndPreventsBlindRetry()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-unknown-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.Wsfev1));
        var factory = new ArcaWsFactory(databasePath, modelOptions);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.EnsureCreatedAsync(cancellationToken);
        byte[] key = RandomNumberGenerator.GetBytes(32);
        var failingClient = new FailingLoginClient();
        using ServiceProvider firstHost = CreateProvider(factory, modelOptions, key, failingClient);
        using ServiceProvider secondHost = CreateProvider(factory, modelOptions, key, new BlockingLoginClient(releaseImmediately: true));
        using var material = CreateCertificate("CN=unknown-test");
        var tenant = new ArcaTenantContext("tenant", 20_123_456_789, ArcaEnvironment.Homologation, material.Content);

        Func<Task> firstAttempt = () => firstHost.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfe", tenant, cancellationToken);
        await firstAttempt.Should().ThrowAsync<WsaaTicketLoginOutcomeUnknownException>();
        Func<Task> retry = () => secondHost.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfe", tenant, cancellationToken);
        await retry.Should().ThrowAsync<WsaaTicketLoginOutcomeUnknownException>();
        failingClient.Calls.Should().Be(1);
        ((BlockingLoginClient)secondHost.GetRequiredService<IWsaaTicketLoginClient>()).Calls.Should().Be(0);
        File.Delete(databasePath);
    }

    [Fact]
    public async Task FailedTicketCompletionAfterSuccessfulLoginMarksOutcomeUnknownAndBlocksRetry()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-complete-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.Wsfev1));
        var interceptor = new FailTicketCompletionOnceInterceptor();
        var factory = new ArcaWsFactory(databasePath, modelOptions, interceptor);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.EnsureCreatedAsync(cancellationToken);
        byte[] key = RandomNumberGenerator.GetBytes(32);
        var loginClient = new BlockingLoginClient(releaseImmediately: true);
        using ServiceProvider firstHost = CreateProvider(factory, modelOptions, key, loginClient);
        using ServiceProvider secondHost = CreateProvider(factory, modelOptions, key, new BlockingLoginClient(releaseImmediately: true));
        using var material = CreateCertificate("CN=complete-failure-test");
        var tenant = new ArcaTenantContext("tenant", 20_123_456_789, ArcaEnvironment.Homologation, material.Content);

        Func<Task> firstAttempt = () => firstHost.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfe", tenant, cancellationToken);
        await firstAttempt.Should().ThrowAsync<WsaaTicketLoginOutcomeUnknownException>();
        Func<Task> retry = () => secondHost.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfe", tenant, cancellationToken);
        await retry.Should().ThrowAsync<WsaaTicketLoginOutcomeUnknownException>();
        loginClient.Calls.Should().Be(1);
        interceptor.FailedCompletion.Should().BeTrue();
        File.Delete(databasePath);
    }

    [Fact]
    public async Task CertificateEnvironmentAndServiceIsolateSharedTicketsWhileTenantAndCuitDoNot()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-isolation-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder =>
            builder.AddWsaaTickets(ArcaService.Wsfev1, ArcaService.Wsfexv1));
        var factory = new ArcaWsFactory(databasePath, modelOptions);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.EnsureCreatedAsync(cancellationToken);
        var loginClient = new BlockingLoginClient(releaseImmediately: true);
        using ServiceProvider host = CreateProvider(factory, modelOptions, RandomNumberGenerator.GetBytes(32), loginClient);
        using var firstMaterial = CreateCertificate("CN=isolation-one");
        using var secondMaterial = CreateCertificate("CN=isolation-two");
        var first = new ArcaTenantContext("tenant-one", 20_123_456_789, ArcaEnvironment.Homologation, firstMaterial.Content);
        var sameCertDifferentTenant = new ArcaTenantContext("tenant-two", 20_987_654_321, ArcaEnvironment.Homologation, firstMaterial.Content);
        var production = new ArcaTenantContext("tenant-one", 20_123_456_789, ArcaEnvironment.Production, firstMaterial.Content);
        var differentCert = new ArcaTenantContext("tenant-one", 20_123_456_789, ArcaEnvironment.Homologation, secondMaterial.Content);
        IArcaTicketProvider tickets = host.GetRequiredService<IArcaTicketProvider>();

        uint one = (await tickets.GetTicketAsync("wsfe", first, cancellationToken)).UniqueId;
        uint shared = (await tickets.GetTicketAsync("wsfe", sameCertDifferentTenant, cancellationToken)).UniqueId;
        uint otherService = (await tickets.GetTicketAsync("wsfex", first, cancellationToken)).UniqueId;
        uint otherEnvironment = (await tickets.GetTicketAsync("wsfe", production, cancellationToken)).UniqueId;
        uint otherCertificate = (await tickets.GetTicketAsync("wsfe", differentCert, cancellationToken)).UniqueId;

        shared.Should().Be(one);
        new[] { otherService, otherEnvironment, otherCertificate }.Should().NotContain(one);
        loginClient.Calls.Should().Be(4);
        File.Delete(databasePath);
    }

    [Fact]
    public async Task CorruptEncryptedTicketFailsClosedWithoutAnotherLogin()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-corrupt-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.Wsfev1));
        var factory = new ArcaWsFactory(databasePath, modelOptions);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.EnsureCreatedAsync(cancellationToken);
        var loginClient = new BlockingLoginClient(releaseImmediately: true);
        using ServiceProvider host = CreateProvider(factory, modelOptions, RandomNumberGenerator.GetBytes(32), loginClient);
        using var material = CreateCertificate("CN=corrupt-test");
        var tenant = new ArcaTenantContext("tenant", 20_123_456_789, ArcaEnvironment.Homologation, material.Content);
        IArcaTicketProvider tickets = host.GetRequiredService<IArcaTicketProvider>();
        await tickets.GetTicketAsync("wsfe", tenant, cancellationToken);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.ExecuteSqlRawAsync("UPDATE NetArcaWsaaTickets SET Ciphertext = X'00'", cancellationToken);

        Func<Task> read = () => tickets.GetTicketAsync("wsfe", tenant, cancellationToken);
        await read.Should().ThrowAsync<CryptographicException>();
        loginClient.Calls.Should().Be(1);
        File.Delete(databasePath);
    }

    [Fact]
    public async Task BackdatedExpiryMetadataCannotTriggerASecondLoginWhileEncryptedTicketIsValid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-backdated-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.Wsfev1));
        var factory = new ArcaWsFactory(databasePath, modelOptions);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.EnsureCreatedAsync(cancellationToken);
        var loginClient = new BlockingLoginClient(null, releaseImmediately: true, ticketLifetime: TimeSpan.FromMinutes(30));
        using ServiceProvider host = CreateProvider(factory, modelOptions, RandomNumberGenerator.GetBytes(32), loginClient);
        using var material = CreateCertificate("CN=backdated-expiry-test");
        var tenant = new ArcaTenantContext("tenant", 20_123_456_789, ArcaEnvironment.Homologation, material.Content);
        IArcaTicketProvider tickets = host.GetRequiredService<IArcaTicketProvider>();
        await tickets.GetTicketAsync("wsfe", tenant, cancellationToken);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.ExecuteSqlRawAsync("UPDATE NetArcaWsaaTickets SET ExpiresUtcTicks = 1", cancellationToken);

        Func<Task> read = () => tickets.GetTicketAsync("wsfe", tenant, cancellationToken);
        await read.Should().ThrowAsync<CryptographicException>();
        loginClient.Calls.Should().Be(1);
        File.Delete(databasePath);
    }

    [Fact]
    public async Task FutureExpiryMetadataCannotExtendAnEncryptedTicketPastItsActualExpiry()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-extended-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.Wsfev1));
        var factory = new ArcaWsFactory(databasePath, modelOptions);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.EnsureCreatedAsync(cancellationToken);
        var loginClient = new BlockingLoginClient(null, releaseImmediately: true, ticketLifetime: TimeSpan.FromMinutes(10));
        using ServiceProvider host = CreateProvider(factory, modelOptions, RandomNumberGenerator.GetBytes(32), loginClient);
        using var material = CreateCertificate("CN=extended-expiry-test");
        var tenant = new ArcaTenantContext("tenant", 20_123_456_789, ArcaEnvironment.Homologation, material.Content);
        IArcaTicketProvider tickets = host.GetRequiredService<IArcaTicketProvider>();
        await tickets.GetTicketAsync("wsfe", tenant, cancellationToken);
        long forgedExpiration = DateTimeOffset.UtcNow.AddHours(1).UtcTicks;
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE NetArcaWsaaTickets SET ExpiresUtcTicks = {forgedExpiration}", cancellationToken);

        Func<Task> read = () => tickets.GetTicketAsync("wsfe", tenant, cancellationToken);
        await read.Should().ThrowAsync<CryptographicException>();
        loginClient.Calls.Should().Be(1);
        File.Delete(databasePath);
    }

    [Fact]
    public async Task CertificateNotAfterIsCheckedBeforeReusingAPersistedTicket()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-cert-expiry-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.Wsfev1));
        var factory = new ArcaWsFactory(databasePath, modelOptions);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.EnsureCreatedAsync(cancellationToken);
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        var loginClient = new BlockingLoginClient(clock, releaseImmediately: true);
        using ServiceProvider host = CreateProvider(factory, modelOptions, RandomNumberGenerator.GetBytes(32), loginClient, clock);
        using var material = CreateCertificate("CN=certificate-expiry-test", clock.GetUtcNow().AddMinutes(1));
        var tenant = new ArcaTenantContext("tenant", 20_123_456_789, ArcaEnvironment.Homologation, material.Content);
        IArcaTicketProvider tickets = host.GetRequiredService<IArcaTicketProvider>();
        await tickets.GetTicketAsync("wsfe", tenant, cancellationToken);
        clock.Advance(TimeSpan.FromMinutes(2));

        Func<Task> read = () => tickets.GetTicketAsync("wsfe", tenant, cancellationToken);
        await read.Should().ThrowAsync<CryptographicException>().WithMessage("*validity period*");
        loginClient.Calls.Should().Be(1);
        File.Delete(databasePath);
    }

    [Fact]
    public async Task DisabledServiceAndDatabaseFailureNeverFallBackToLocalWsaaLogin()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-disabled-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.Wsfev1));
        var factory = new ArcaWsFactory(databasePath, modelOptions);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.EnsureCreatedAsync(cancellationToken);
        var loginClient = new BlockingLoginClient(releaseImmediately: true);
        using ServiceProvider host = CreateProvider(factory, modelOptions, RandomNumberGenerator.GetBytes(32), loginClient);
        using var material = CreateCertificate("CN=disabled-test");
        var tenant = new ArcaTenantContext("tenant", 20_123_456_789, ArcaEnvironment.Homologation, material.Content);
        Func<Task> disabled = () => host.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfex", tenant, cancellationToken);
        await disabled.Should().ThrowAsync<InvalidOperationException>();
        loginClient.Calls.Should().Be(0);

        string missingDatabasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-missing-{Guid.NewGuid():N}.db");
        var missingFactory = new ArcaWsFactory(missingDatabasePath, modelOptions);
        using ServiceProvider noSchemaHost = CreateProvider(missingFactory, modelOptions, RandomNumberGenerator.GetBytes(32), loginClient);
        Func<Task> missingSchema = () => noSchemaHost.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfe", tenant, cancellationToken);
        await missingSchema.Should().ThrowAsync<Microsoft.Data.Sqlite.SqliteException>();
        loginClient.Calls.Should().Be(0);
        File.Delete(databasePath);
        File.Delete(missingDatabasePath);
    }

    [Fact]
    public async Task PadronA5SelectionUsesItsActualWsaaServiceIdentifier()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string databasePath = Path.Combine(Path.GetTempPath(), $"shared-wsaa-padron-a5-{Guid.NewGuid():N}.db");
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(builder => builder.AddWsaaTickets(ArcaService.PadronA5));
        var factory = new ArcaWsFactory(databasePath, modelOptions);
        await using (ArcaWsDbContext context = await factory.CreateDbContextAsync(cancellationToken))
            await context.Database.EnsureCreatedAsync(cancellationToken);
        var loginClient = new BlockingLoginClient(releaseImmediately: true);
        using ServiceProvider host = CreateProvider(factory, modelOptions, RandomNumberGenerator.GetBytes(32), loginClient);
        using var material = CreateCertificate("CN=padron-a5-test");
        var tenant = new ArcaTenantContext("tenant", 20_123_456_789, ArcaEnvironment.Homologation, material.Content);

        await host.GetRequiredService<IArcaTicketProvider>()
            .GetTicketAsync("ws_sr_constancia_inscripcion", tenant, cancellationToken);

        loginClient.LastService.Should().Be("ws_sr_constancia_inscripcion");
        File.Delete(databasePath);
    }

    [Fact]
    public async Task UncachedTenantLoginUsesAuthorizedEnvironmentAndDoesNotReuseLocalTicket()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        int calls = 0;
        var handler = new CountingWsaaHandler((request, _) =>
        {
            request.RequestUri.Should().Be(WsaaOptions.ProductionEndpoint);
            int id = Interlocked.Increment(ref calls);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SoapSuccess(TicketXml(now, id)))
            });
        });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var wsaa = new WsaaService(new TestHttpFactory(handler), cache,
            Options.Create(new WsaaOptions()), TimeProvider.System);
        using RSA key = RSA.Create(2048);
        var request = new CertificateRequest("CN=shared-ticket-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(now.AddHours(-1), now.AddHours(1));
        var tenant = new ArcaTenantContext("tenant-a", 20_123_456_789, ArcaEnvironment.Production,
            WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), key.ExportPkcs8PrivateKeyPem()));

        WsaaTicket first = await wsaa.AuthenticateForTenantWithoutCacheAsync("wsfe", tenant, cancellationToken);
        WsaaTicket second = await wsaa.AuthenticateForTenantWithoutCacheAsync("wsfe", tenant, cancellationToken);

        first.UniqueId.Should().Be(1);
        second.UniqueId.Should().Be(2);
        calls.Should().Be(2);
    }

    private static string TicketXml(DateTimeOffset now, int id) => $"""
        <loginTicketResponse version="1.0">
          <header><source>CN=wsaa-test</source><destination>CN=service-test</destination><uniqueId>{id}</uniqueId>
            <generationTime>{now.AddMinutes(-1):O}</generationTime><expirationTime>{now.AddHours(1):O}</expirationTime></header>
          <credentials><token>token-{id}</token><sign>sign-{id}</sign></credentials>
        </loginTicketResponse>
        """;

    private static string SoapSuccess(string ticket)
    {
        XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        XNamespace wsaa = "http://wsaa.view.sua.dvadac.desein.afip.gov";
        return new XDocument(new XElement(soap + "Envelope", new XAttribute(XNamespace.Xmlns + "soap", soap),
            new XAttribute(XNamespace.Xmlns + "ns", wsaa), new XElement(soap + "Body",
                new XElement(wsaa + "loginCmsResponse", new XElement(wsaa + "loginCmsReturn", ticket)))))
            .ToString(SaveOptions.DisableFormatting);
    }

    private sealed class CountingWsaaHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            responder(request, cancellationToken);
    }

    private sealed class TestHttpFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private static ServiceProvider CreateProvider(IDbContextFactory<ArcaWsDbContext> factory,
        NetArcaWsModelOptions modelOptions, byte[] key, IWsaaTicketLoginClient loginClient,
        TimeProvider? timeProvider = null, TimeSpan? waitTimeout = null, TimeSpan? pollingInterval = null,
        TimeSpan? ownerLeaseDuration = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(factory);
        services.AddSingleton(modelOptions);
        if (timeProvider is not null) services.AddSingleton(timeProvider);
        services.AddSingleton<IWsaaTicketProtector>(new AesGcmWsaaTicketProtector("test-key", new Dictionary<string, byte[]> { ["test-key"] = key }));
        services.AddSingleton<IWsaaTicketLoginClient>(loginClient);
        if (waitTimeout is null && pollingInterval is null && ownerLeaseDuration is null)
            services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(modelOptions);
        else
            services.AddWsaaTicketStores<ArcaWsDbContext>(modelOptions,
                waitTimeout ?? TimeSpan.FromSeconds(3), pollingInterval ?? TimeSpan.FromMilliseconds(10), ownerLeaseDuration);
        return services.BuildServiceProvider();
    }

    private static CertificateMaterial CreateCertificate(string subject, DateTimeOffset? notAfter = null)
    {
        RSA key = RSA.Create(2048);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), notAfter ?? DateTimeOffset.UtcNow.AddHours(1));
        return new CertificateMaterial(key, certificate,
            WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), key.ExportPkcs8PrivateKeyPem()));
    }

    private sealed class CertificateMaterial(RSA key, X509Certificate2 certificate, WsaaCertificateContent content) : IDisposable
    {
        public WsaaCertificateContent Content { get; } = content;
        public void Dispose() { certificate.Dispose(); key.Dispose(); }
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;
        public override DateTimeOffset GetUtcNow() => current;
        public void Advance(TimeSpan amount) => current += amount;
    }

    private sealed class ArcaWsFactory : IDbContextFactory<ArcaWsDbContext>
    {
        private readonly NetArcaWsModelOptions modelOptions;
        private readonly DbContextOptions<ArcaWsDbContext> options;

        public ArcaWsFactory(string path, NetArcaWsModelOptions modelOptions, IInterceptor? interceptor = null)
        {
            this.modelOptions = modelOptions;
            var builder = new DbContextOptionsBuilder<ArcaWsDbContext>()
                .UseSqlite($"Data Source={path};Pooling=False;Default Timeout=10");
            if (interceptor is not null) builder.AddInterceptors(interceptor);
            options = builder.Options;
        }

        public ArcaWsDbContext CreateDbContext() => new(options, modelOptions);
        public Task<ArcaWsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private sealed class BlockingLoginClient : IWsaaTicketLoginClient
    {
        private readonly TimeProvider? clock;
        private readonly TimeSpan ticketLifetime;
        private int calls;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls => Volatile.Read(ref calls);
        public string? LastService { get; private set; }

        public BlockingLoginClient() : this(null, false, null) { }
        public BlockingLoginClient(bool releaseImmediately) : this(null, releaseImmediately, null) { }
        public BlockingLoginClient(TimeProvider? clock, bool releaseImmediately = false, TimeSpan? ticketLifetime = null)
        {
            this.clock = clock;
            this.ticketLifetime = ticketLifetime ?? TimeSpan.FromMinutes(30);
            if (releaseImmediately) Release.TrySetResult();
        }

        public async Task<WsaaTicket> LoginAsync(string service, ArcaTenantContext tenant, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref calls);
            LastService = service;
            Entered.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            DateTimeOffset now = (clock ?? TimeProvider.System).GetUtcNow();
            uint id = (uint)Interlocked.Increment(ref ticketSequence);
            string xml = $"""
                <loginTicketResponse version="1.0"><header><source>CN=test</source><destination>CN=service</destination><uniqueId>{id}</uniqueId>
                <generationTime>{now.AddMinutes(-1):O}</generationTime><expirationTime>{now.Add(ticketLifetime):O}</expirationTime></header>
                <credentials><token>shared-token</token><sign>shared-sign</sign></credentials></loginTicketResponse>
                """;
            return new WsaaTicket("shared-token", "shared-sign", now.AddMinutes(-1), now.Add(ticketLifetime),
                "CN=test", "CN=service", id, xml);
        }

        private static int ticketSequence;
    }

    private sealed class FailingLoginClient : IWsaaTicketLoginClient
    {
        private int calls;
        public int Calls => Volatile.Read(ref calls);
        public Task<WsaaTicket> LoginAsync(string service, ArcaTenantContext tenant, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref calls);
            throw new HttpRequestException("synthetic login result lost");
        }
    }

    private sealed class FailTicketCompletionOnceInterceptor : DbCommandInterceptor
    {
        private int failed;
        public bool FailedCompletion => Volatile.Read(ref failed) == 1;

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("NetArcaWsaaTickets", StringComparison.Ordinal) &&
                command.CommandText.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) && Interlocked.Exchange(ref failed, 1) == 0)
                throw new IOException("synthetic ticket completion persistence failure");
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
