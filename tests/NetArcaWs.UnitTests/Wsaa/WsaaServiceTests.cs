using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using AwesomeAssertions;
using NetArcaWs.Tests.TestSupport;
using NetArcaWs.Wsaa;
using Xunit;

namespace NetArcaWs.Tests.Wsaa;

public sealed class WsaaServiceTests
{
    [Fact]
    public void CreateTra_uses_the_configured_service_and_symmetric_ttl()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var clock = new AdjustableTimeProvider(now);
        using var harness = CreateHarness((_, _) => Task.FromResult(RecordingHttpMessageHandler.Response(HttpStatusCode.OK, "")), clock,
            ttl: TimeSpan.FromMinutes(30));

        XDocument tra = XDocument.Parse(harness.Service.CreateTra("wsfe"));
        XElement root = tra.Root!;
        XElement header = root.Element("header")!;

        root.Name.LocalName.Should().Be("loginTicketRequest");
        root.Attribute("version")!.Value.Should().Be("1.0");
        root.Element("service")!.Value.Should().Be("wsfe");
        uint.Parse(header.Element("uniqueId")!.Value).Should().Be((uint)now.ToUnixTimeSeconds());
        DateTimeOffset.Parse(header.Element("generationTime")!.Value).Should().Be(now.AddMinutes(-30));
        DateTimeOffset.Parse(header.Element("expirationTime")!.Value).Should().Be(now.AddMinutes(30));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("ws", false)]
    [InlineData("9wsfe", false)]
    [InlineData("ws-fe", false)]
    [InlineData("wsfe", true)]
    [InlineData("abc_123", true)]
    public void CreateTra_enforces_the_ARCA_service_identifier_grammar(string service, bool valid)
    {
        using var harness = CreateHarness((_, _) => Task.FromResult(RecordingHttpMessageHandler.Response(HttpStatusCode.OK, "")));

        Action create = () => harness.Service.CreateTra(service);

        if (valid)
            create.Should().NotThrow();
        else
            create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task LoginCmsAsync_posts_qualified_soap_request_and_parses_a_success_ticket()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        HttpRequestMessage? capturedRequest = null;
        string? requestXml = null;
        var handler = new RecordingHttpMessageHandler(async (request, _) =>
        {
            capturedRequest = request;
            requestXml = await request.Content!.ReadAsStringAsync();
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(now.AddMinutes(-1), now.AddHours(1))));
        });
        using var harness = CreateHarness(handler);
        string cms = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("signed-cms"));

        WsaaTicket ticket = await harness.Service.LoginCmsAsync(cms, cancellationToken);

        ticket.Token.Should().Be("test-token");
        ticket.Sign.Should().Be("test-sign");
        ticket.UniqueId.Should().Be(12345);
        ticket.IsExpired(harness.Clock.GetUtcNow()).Should().BeFalse();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri.Should().Be(WsaaOptions.HomologationEndpoint);
        capturedRequest.Headers.GetValues("SOAPAction").Should().ContainSingle().Which.Should().Be("\"\"");
        XDocument requestDocument = XDocument.Parse(requestXml!);
        XNamespace soap = WsaaTestData.SoapNamespace;
        XNamespace wsaa = WsaaTestData.WsaaNamespace;
        requestDocument.Root!.Name.Should().Be(soap + "Envelope");
        requestDocument.Descendants(wsaa + "loginCms").Should().ContainSingle();
        requestDocument.Descendants(wsaa + "in0").Should().ContainSingle().Which.Value.Should().Be(cms);
        harness.Factory.CreateCount.Should().Be(1);
        harness.Factory.LastClientName.Should().Be(WsaaService.HttpClientName);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task LoginCmsAsync_preserves_SOAP_fault_details_for_success_and_error_http_status(HttpStatusCode status)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            RecordingHttpMessageHandler.Response(status,
                WsaaTestData.SoapFault("coe.notAuthorized", "Access denied", "WSN authorization missing."))));
        using var harness = CreateHarness(handler);

        Func<Task> login = () => harness.Service.LoginCmsAsync("Y21z", cancellationToken);

        var exception = await login.Should().ThrowAsync<WsaaSoapException>().Where(ex =>
            ex.FaultCode == "coe.notAuthorized" &&
            ex.FaultString == "Access denied" &&
            ex.Detail!.Contains("WSN authorization missing.") &&
            ex.StatusCode == status);
        exception.Which.FaultCode.Should().Be("coe.notAuthorized");
    }

    [Fact]
    public async Task LoginCmsAsync_rejects_malformed_XML_response()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            RecordingHttpMessageHandler.Response(HttpStatusCode.OK, "<soap:Envelope><broken>")));
        using var harness = CreateHarness(handler);

        Func<Task> login = () => harness.Service.LoginCmsAsync("Y21z", cancellationToken);

        await login.Should().ThrowAsync<FormatException>();
    }

    [Fact]
    public async Task LoginCmsAsync_rejects_expired_ticket_and_ticket_generated_beyond_clock_skew()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string[] ticketXmls =
        [
            WsaaTestData.TicketXml(now.AddHours(-1), now.AddSeconds(-1)),
            WsaaTestData.TicketXml(now.AddMinutes(3), now.AddHours(1))
        ];
        int responseIndex = -1;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                WsaaTestData.SoapSuccess(ticketXmls[Interlocked.Increment(ref responseIndex)]))));
        using var harness = CreateHarness(handler, new AdjustableTimeProvider(now));

        Func<Task> firstLogin = () => harness.Service.LoginCmsAsync("Y21z", cancellationToken);
        Func<Task> secondLogin = () => harness.Service.LoginCmsAsync("Y21z", cancellationToken);

        await firstLogin.Should().ThrowAsync<FormatException>().WithMessage("*expired*");
        await secondLogin.Should().ThrowAsync<FormatException>().WithMessage("*future*");
    }

    [Fact]
    public async Task AuthenticateAsync_coalesces_concurrent_requests_for_the_same_certificate_and_service()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var enteredHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new RecordingHttpMessageHandler(async (_, cancellationToken) =>
        {
            enteredHandler.TrySetResult();
            await releaseHandler.Task.WaitAsync(cancellationToken);
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(now.AddMinutes(-1), now.AddHours(1))));
        });
        using var harness = CreateHarness(handler, new AdjustableTimeProvider(now));
        using X509Certificate2 certificate = TestCertificates.CreateWithPrivateKey();
        Task<WsaaTicket>[] calls = Enumerable.Range(0, 12)
            .Select(_ => Task.Run(() => harness.Service.AuthenticateAsync("wsfe", certificate, cancellationToken), cancellationToken))
            .ToArray();

        await enteredHandler.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        releaseHandler.TrySetResult();
        WsaaTicket[] tickets = await Task.WhenAll(calls);

        tickets.Should().OnlyContain(ticket => ticket.Token == "test-token");
        handler.RequestCount.Should().Be(1);
        harness.Factory.CreateCount.Should().Be(1);
    }

    [Fact]
    public async Task AuthenticateAsync_reuses_ticket_until_exact_expiration_then_fetches_a_new_one()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var clock = new AdjustableTimeProvider(now);
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(
                    clock.GetUtcNow().AddMinutes(-1), clock.GetUtcNow().AddMinutes(10))))));
        using var harness = CreateHarness(handler, clock);
        using X509Certificate2 certificate = TestCertificates.CreateWithPrivateKey();

        await harness.Service.AuthenticateAsync("wsfe", certificate, cancellationToken);
        clock.Advance(TimeSpan.FromMinutes(9));
        await harness.Service.AuthenticateAsync("wsfe", certificate, cancellationToken);
        handler.RequestCount.Should().Be(1);

        clock.Advance(TimeSpan.FromMinutes(1));
        await harness.Service.AuthenticateAsync("wsfe", certificate, cancellationToken);

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task AuthenticateAsync_does_not_cache_a_failed_response()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        int attempt = 0;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            Interlocked.Increment(ref attempt) == 1
                ? RecordingHttpMessageHandler.Response(HttpStatusCode.OK, "not xml")
                : RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                    WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(now.AddMinutes(-1), now.AddHours(1))))));
        using var harness = CreateHarness(handler, new AdjustableTimeProvider(now));
        using X509Certificate2 certificate = TestCertificates.CreateWithPrivateKey();

        Func<Task> firstAttempt = () => harness.Service.AuthenticateAsync("wsfe", certificate, cancellationToken);
        await firstAttempt.Should().ThrowAsync<FormatException>();

        WsaaTicket recovered = await harness.Service.AuthenticateAsync("wsfe", certificate, cancellationToken);

        recovered.Token.Should().Be("test-token");
        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task AuthenticateAsync_keeps_services_and_endpoints_in_separate_cache_entries()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var clock = new AdjustableTimeProvider(now);
        using var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        int firstEndpointRequests = 0;
        int secondEndpointRequests = 0;
        using X509Certificate2 certificate = TestCertificates.CreateWithPrivateKey();
        using X509Certificate2 otherCertificate = TestCertificates.CreateWithPrivateKey("wsaa-other-cert");
        using var firstHandler = new RecordingHttpMessageHandler((_, _) =>
        {
            Interlocked.Increment(ref firstEndpointRequests);
            return Task.FromResult(RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(now.AddMinutes(-1), now.AddHours(1)))));
        });
        using var secondHandler = new RecordingHttpMessageHandler((_, _) =>
        {
            Interlocked.Increment(ref secondEndpointRequests);
            return Task.FromResult(RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(now.AddMinutes(-1), now.AddHours(1)))));
        });
        var firstFactory = new TestHttpClientFactory(firstHandler);
        var secondFactory = new TestHttpClientFactory(secondHandler);
        var first = new WsaaService(firstFactory, cache,
            Microsoft.Extensions.Options.Options.Create(new WsaaOptions { Endpoint = WsaaOptions.HomologationEndpoint }), clock);
        var second = new WsaaService(secondFactory, cache,
            Microsoft.Extensions.Options.Options.Create(new WsaaOptions { Endpoint = new Uri("https://wsaa-test.example/ws/services/LoginCms") }), clock);

        await first.AuthenticateAsync("wsfe", certificate, cancellationToken);
        await first.AuthenticateAsync("wsbfe", certificate, cancellationToken);
        await first.AuthenticateAsync("wsfe", otherCertificate, cancellationToken);
        await second.AuthenticateAsync("wsfe", certificate, cancellationToken);

        firstEndpointRequests.Should().Be(3);
        secondEndpointRequests.Should().Be(1);
    }

    [Fact]
    public async Task LoginCmsAsync_honors_HttpClient_timeout_while_reading_a_streamed_body()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new BlockingReadHttpContent() }));
        using var harness = new WsaaServiceHarness(handler,
            clientTimeout: TimeSpan.FromMilliseconds(100));

        Func<Task> login = async () => await harness.Service.LoginCmsAsync("Y21z", cancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(1), cancellationToken);

        await login.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task LoginCmsAsync_rejects_a_response_larger_than_the_configured_limit()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string largeTicket = WsaaTestData.TicketXml(now.AddMinutes(-1), now.AddHours(1), token: new string('x', 2000));
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            RecordingHttpMessageHandler.Response(HttpStatusCode.OK, WsaaTestData.SoapSuccess(largeTicket))));
        using var harness = new WsaaServiceHarness(handler, maxResponseBytes: 1024);

        Func<Task> login = () => harness.Service.LoginCmsAsync("Y21z", cancellationToken);

        await login.Should().ThrowAsync<FormatException>().WithMessage("*size limit*");
    }

    [Fact]
    public async Task AuthenticateAsync_shares_single_flight_across_instances_using_the_same_cache()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var clock = new AdjustableTimeProvider(now);
        using var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var firstRequestEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int requests = 0;
        var firstHandler = new RecordingHttpMessageHandler(async (_, token) =>
        {
            Interlocked.Increment(ref requests);
            firstRequestEntered.TrySetResult();
            await releaseFirstRequest.Task.WaitAsync(token);
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(now.AddMinutes(-1), now.AddHours(1))));
        });
        var secondHandler = new RecordingHttpMessageHandler((_, _) =>
        {
            Interlocked.Increment(ref requests);
            return Task.FromResult(RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(now.AddMinutes(-1), now.AddHours(1)))));
        });
        var first = new WsaaService(new TestHttpClientFactory(firstHandler), cache,
            Microsoft.Extensions.Options.Options.Create(new WsaaOptions()), clock);
        var second = new WsaaService(new TestHttpClientFactory(secondHandler), cache,
            Microsoft.Extensions.Options.Options.Create(new WsaaOptions()), clock);
        using X509Certificate2 certificate = TestCertificates.CreateWithPrivateKey();

        Task<WsaaTicket> firstCall = first.AuthenticateAsync("wsfe", certificate, cancellationToken);
        await firstRequestEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        Task<WsaaTicket> secondCall = second.AuthenticateAsync("wsfe", certificate, cancellationToken);
        releaseFirstRequest.TrySetResult();

        WsaaTicket[] tickets = await Task.WhenAll(firstCall, secondCall);

        tickets.Should().OnlyContain(ticket => ticket.Token == "test-token");
        requests.Should().Be(1);
    }

    private static WsaaServiceHarness CreateHarness(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond,
        AdjustableTimeProvider? clock = null,
        Uri? endpoint = null,
        TimeSpan? ttl = null) =>
        new(new RecordingHttpMessageHandler(respond), clock, endpoint, ttl);

    private static WsaaServiceHarness CreateHarness(
        RecordingHttpMessageHandler handler,
        AdjustableTimeProvider? clock = null,
        Uri? endpoint = null,
        TimeSpan? ttl = null) => new(handler, clock, endpoint, ttl);
}
