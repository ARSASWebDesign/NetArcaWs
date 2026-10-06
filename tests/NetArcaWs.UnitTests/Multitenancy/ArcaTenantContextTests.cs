using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using AwesomeAssertions;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Tests.TestSupport;
using NetArcaWs.Wsaa;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace NetArcaWs.Tests.Multitenancy;

public sealed class ArcaTenantContextTests
{
    private const long TenantCuit = 30123456789;

    [Fact]
    public void Context_validates_tenant_id_cuit_environment_and_certificate()
    {
        WsaaCertificateContent certificate = CreateContent("tenant-context");

        Action emptyId = () => new ArcaTenantContext(" ", TenantCuit, ArcaEnvironment.Homologation, certificate);
        Action controlId = () => new ArcaTenantContext("tenant\n01", TenantCuit, ArcaEnvironment.Homologation, certificate);
        Action longId = () => new ArcaTenantContext(new string('t', 129), TenantCuit, ArcaEnvironment.Homologation, certificate);
        Action smallCuit = () => new ArcaTenantContext("tenant-01", 9_999_999_999, ArcaEnvironment.Homologation, certificate);
        Action largeCuit = () => new ArcaTenantContext("tenant-01", 100_000_000_000, ArcaEnvironment.Homologation, certificate);
        Action invalidEnvironment = () => new ArcaTenantContext("tenant-01", TenantCuit, (ArcaEnvironment)99, certificate);
        Action missingCertificate = () => new ArcaTenantContext("tenant-01", TenantCuit, ArcaEnvironment.Homologation, null!);

        emptyId.Should().Throw<ArgumentException>();
        controlId.Should().Throw<ArgumentException>();
        longId.Should().Throw<ArgumentException>();
        smallCuit.Should().Throw<ArgumentOutOfRangeException>();
        largeCuit.Should().Throw<ArgumentOutOfRangeException>();
        invalidEnvironment.Should().Throw<ArgumentOutOfRangeException>();
        missingCertificate.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Different_tenants_do_not_share_cached_tickets_even_with_the_same_certificate()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var (service, handler, cache) = CreateService();
        using MemoryCache ownedCache = cache;
        WsaaCertificateContent content = CreateContent("shared-certificate");
        var firstTenant = new ArcaTenantContext("tenant-a", TenantCuit, ArcaEnvironment.Homologation, content);
        var secondTenant = new ArcaTenantContext("tenant-b", TenantCuit, ArcaEnvironment.Homologation, content);

        await service.AuthenticateForTenantAsync("wsfe", firstTenant, cancellationToken);
        await service.AuthenticateForTenantAsync("wsfe", secondTenant, cancellationToken);

        handler.RequestCount.Should().Be(2, "ticket cache entries must be isolated per tenant");
    }

    [Fact]
    public async Task Tenant_authentication_does_not_cache_a_malformed_response_or_poison_another_tenant()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        int attempt = 0;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            Interlocked.Increment(ref attempt) == 1
                ? RecordingHttpMessageHandler.Response(HttpStatusCode.OK, "not xml")
                : TicketResponse(now)));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        WsaaCertificateContent certificate = CreateContent("tenant-error-recovery");
        var service = new WsaaService(
            new TestHttpClientFactory(handler),
            cache,
            Options.Create(new WsaaOptions()),
            new AdjustableTimeProvider(now));
        var firstTenant = new ArcaTenantContext("tenant-error-a", TenantCuit, ArcaEnvironment.Homologation, certificate);
        var secondTenant = new ArcaTenantContext("tenant-error-b", TenantCuit, ArcaEnvironment.Homologation, certificate);

        Func<Task> failedAuthentication = () => service.AuthenticateForTenantAsync("wsfe", firstTenant, cancellationToken);
        await failedAuthentication.Should().ThrowAsync<FormatException>();

        WsaaTicket recovered = await service.AuthenticateForTenantAsync("wsfe", firstTenant, cancellationToken);
        WsaaTicket otherTenant = await service.AuthenticateForTenantAsync("wsfe", secondTenant, cancellationToken);

        recovered.Token.Should().Be("test-token");
        otherTenant.Token.Should().Be("test-token");
        handler.RequestCount.Should().Be(3, "the malformed response is not cached and the second tenant gets its own ticket request");
    }

    [Fact]
    public async Task Tenant_and_non_tenant_authentication_do_not_share_cached_tickets()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(TicketResponse(now)));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        WsaaCertificateContent content = CreateContent("tenant-vs-default");
        var service = new WsaaService(
            new TestHttpClientFactory(handler),
            cache,
            Options.Create(new WsaaOptions { Certificate = content }),
            new AdjustableTimeProvider(now));
        var tenant = new ArcaTenantContext("tenant-explicit", TenantCuit, ArcaEnvironment.Homologation, content);

        await service.AuthenticateAsync("wsfe", cancellationToken);
        await service.AuthenticateForTenantAsync("wsfe", tenant, cancellationToken);

        handler.RequestCount.Should().Be(2, "tenant identity is part of the tenant cache scope");
    }

    [Fact]
    public async Task Concurrent_calls_for_the_same_tenant_context_share_one_request()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var enteredHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new RecordingHttpMessageHandler(async (_, token) =>
        {
            enteredHandler.TrySetResult();
            await releaseHandler.Task.WaitAsync(token);
            return TicketResponse(now);
        });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new WsaaService(
            new TestHttpClientFactory(handler),
            cache,
            Options.Create(new WsaaOptions()),
            new AdjustableTimeProvider(now));
        var tenant = new ArcaTenantContext("tenant-concurrent", TenantCuit, ArcaEnvironment.Homologation, CreateContent("concurrent"));
        Task<WsaaTicket>[] calls = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => service.AuthenticateForTenantAsync("wsfe", tenant, cancellationToken), cancellationToken))
            .ToArray();

        await enteredHandler.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        releaseHandler.TrySetResult();
        WsaaTicket[] tickets = await Task.WhenAll(calls);

        tickets.Should().OnlyContain(ticket => ticket.Token == "test-token");
        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task Concurrent_tenants_sign_with_their_own_certificate_and_cache_the_matching_ticket()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        using X509Certificate2 firstCertificate = TestCertificates.CreateWithPrivateKey("tenant-signer-one");
        using X509Certificate2 secondCertificate = TestCertificates.CreateWithPrivateKey("tenant-signer-two");
        string firstThumbprint = firstCertificate.Thumbprint;
        string secondThumbprint = secondCertificate.Thumbprint;
        WsaaCertificateContent firstContent = FromCertificate(firstCertificate);
        WsaaCertificateContent secondContent = FromCertificate(secondCertificate);
        var firstTenant = new ArcaTenantContext("tenant-signer-a", TenantCuit, ArcaEnvironment.Homologation, firstContent);
        var secondTenant = new ArcaTenantContext("tenant-signer-b", 30987654321, ArcaEnvironment.Homologation, secondContent);
        var seenSigners = new ConcurrentBag<(string Thumbprint, string Service)>();
        var bothRequestsEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int arrivedCount = 0;
        var handler = new RecordingHttpMessageHandler(async (request, token) =>
        {
            string requestBody = await request.Content!.ReadAsStringAsync(token);
            XDocument soapRequest = XDocument.Parse(requestBody);
            XNamespace wsaaNamespace = WsaaTestData.WsaaNamespace;
            string cmsBase64 = soapRequest.Descendants(wsaaNamespace + "in0").Single().Value;
            var signedCms = new SignedCms();
            signedCms.Decode(Convert.FromBase64String(cmsBase64));
            signedCms.Detached.Should().BeFalse();
            signedCms.CheckSignature(verifySignatureOnly: true);
            string signerThumbprint = signedCms.SignerInfos[0].Certificate!.Thumbprint;
            string traXml = Encoding.UTF8.GetString(signedCms.ContentInfo.Content);
            XDocument tra = XDocument.Parse(traXml);
            tra.Root!.Element("service")!.Value.Should().Be("wsfe");
            seenSigners.Add((signerThumbprint, tra.Root.Element("service")!.Value));

            if (Interlocked.Increment(ref arrivedCount) == 2)
                bothRequestsEntered.TrySetResult();
            await bothRequestsEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), token);

            string ticketToken = signerThumbprint switch
            {
                var thumbprint when thumbprint == firstThumbprint => "ticket-for-tenant-a",
                var thumbprint when thumbprint == secondThumbprint => "ticket-for-tenant-b",
                _ => throw new InvalidOperationException("Unexpected CMS signer certificate.")
            };
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(
                    now.AddMinutes(-1), now.AddHours(1), token: ticketToken)));
        });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new WsaaService(
            new TestHttpClientFactory(handler),
            cache,
            Options.Create(new WsaaOptions()),
            new AdjustableTimeProvider(now));

        Task<WsaaTicket> firstCall = service.AuthenticateForTenantAsync("wsfe", firstTenant, cancellationToken);
        Task<WsaaTicket> secondCall = service.AuthenticateForTenantAsync("wsfe", secondTenant, cancellationToken);
        WsaaTicket[] initialTickets = await Task.WhenAll(firstCall, secondCall);

        initialTickets.Should().ContainSingle(ticket => ticket.Token == "ticket-for-tenant-a");
        initialTickets.Should().ContainSingle(ticket => ticket.Token == "ticket-for-tenant-b");
        seenSigners.Should().Contain((firstThumbprint, "wsfe"));
        seenSigners.Should().Contain((secondThumbprint, "wsfe"));
        handler.RequestCount.Should().Be(2);

        WsaaTicket firstCached = await service.AuthenticateForTenantAsync("wsfe", firstTenant, cancellationToken);
        WsaaTicket secondCached = await service.AuthenticateForTenantAsync("wsfe", secondTenant, cancellationToken);

        firstCached.Token.Should().Be("ticket-for-tenant-a");
        secondCached.Token.Should().Be("ticket-for-tenant-b");
        handler.RequestCount.Should().Be(2, "each cached ticket remains bound to the certificate that signed its CMS request");
    }

    [Fact]
    public async Task Cache_separates_same_tenant_by_cuit_environment_service_and_certificate_and_uses_environment_endpoint()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var uris = new ConcurrentBag<Uri>();
        var handler = new RecordingHttpMessageHandler((request, _) =>
        {
            uris.Add(request.RequestUri!);
            return Task.FromResult(TicketResponse(now));
        });
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new WsaaService(
            new TestHttpClientFactory(handler),
            cache,
            Options.Create(new WsaaOptions()),
            new AdjustableTimeProvider(now));
        WsaaCertificateContent firstCertificate = CreateContent("tenant-key-one");
        WsaaCertificateContent rotatedCertificate = CreateContent("tenant-key-rotated");
        var baseline = new ArcaTenantContext("tenant-matrix", TenantCuit, ArcaEnvironment.Homologation, firstCertificate);
        var differentCuit = new ArcaTenantContext("tenant-matrix", 30987654321, ArcaEnvironment.Homologation, firstCertificate);
        var production = new ArcaTenantContext("tenant-matrix", TenantCuit, ArcaEnvironment.Production, firstCertificate);
        var differentCertificate = new ArcaTenantContext("tenant-matrix", TenantCuit, ArcaEnvironment.Homologation, rotatedCertificate);

        await service.AuthenticateForTenantAsync("wsfe", baseline, cancellationToken);
        await service.AuthenticateForTenantAsync("wsfe", differentCuit, cancellationToken);
        await service.AuthenticateForTenantAsync("wsfe", production, cancellationToken);
        await service.AuthenticateForTenantAsync("wslocal", baseline, cancellationToken);
        await service.AuthenticateForTenantAsync("wsfe", differentCertificate, cancellationToken);

        handler.RequestCount.Should().Be(5);
        uris.Should().Contain(uri => uri == WsaaOptions.HomologationEndpoint);
        uris.Should().Contain(uri => uri == WsaaOptions.ProductionEndpoint);
        uris.Count(uri => uri == WsaaOptions.HomologationEndpoint).Should().Be(4);
        uris.Count(uri => uri == WsaaOptions.ProductionEndpoint).Should().Be(1);
    }

    [Fact]
    public async Task Tenant_authentication_honors_cancellation_without_sending_a_request()
    {
        var (service, handler, cache) = CreateService();
        using MemoryCache ownedCache = cache;
        var tenant = new ArcaTenantContext("tenant-cancel", TenantCuit, ArcaEnvironment.Homologation, CreateContent("cancel"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Func<Task> authenticate = () => service.AuthenticateForTenantAsync("wsfe", tenant, cancellation.Token);

        await authenticate.Should().ThrowAsync<OperationCanceledException>();
        handler.RequestCount.Should().Be(0);
    }

    private static (WsaaService Service, RecordingHttpMessageHandler Handler, MemoryCache Cache) CreateService()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(TicketResponse(now)));
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new WsaaService(
            new TestHttpClientFactory(handler),
            cache,
            Options.Create(new WsaaOptions()),
            new AdjustableTimeProvider(now));
        return (service, handler, cache);
    }

    private static HttpResponseMessage TicketResponse(DateTimeOffset now) => RecordingHttpMessageHandler.Response(
        HttpStatusCode.OK,
        WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(now.AddMinutes(-1), now.AddHours(1))));

    private static WsaaCertificateContent CreateContent(string commonName)
    {
        using X509Certificate2 certificate = TestCertificates.CreateWithPrivateKey(commonName);
        using var privateKey = certificate.GetRSAPrivateKey()!;
        return WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), privateKey.ExportPkcs8PrivateKeyPem());
    }

    private static WsaaCertificateContent FromCertificate(X509Certificate2 certificate)
    {
        using var privateKey = certificate.GetRSAPrivateKey()!;
        return WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), privateKey.ExportPkcs8PrivateKeyPem());
    }
}
