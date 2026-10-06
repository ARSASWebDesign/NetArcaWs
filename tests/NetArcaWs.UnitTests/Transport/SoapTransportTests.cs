using System.Net;
using System.Xml.Linq;
using System.Xml.Serialization;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using NetArcaWs.Tests.TestSupport;
using NetArcaWs.Transport;
using Xunit;

namespace NetArcaWs.Tests.Transport;

public sealed class SoapTransportTests
{
    private static readonly Uri Endpoint = new("https://soap.test.example/service");
    private const string SoapNamespace = WsaaTestData.SoapNamespace;
    private const string ContractNamespace = "urn:netarcaws:soap-tests";

    [Fact]
    public async Task SendAsync_serializes_a_document_literal_soap11_request_and_deserializes_body_response()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var handler = new RecordingHttpMessageHandler(async (request, token) =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync(token);
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK, SoapEnvelope($"<EchoResponse xmlns=\"{ContractNamespace}\"><Value>answer</Value></EchoResponse>"));
        });
        var factory = new TestHttpClientFactory(handler);
        var transport = CreateTransport(factory);

        EchoResponse response = await transport.SendAsync<EchoRequest, EchoResponse>(
            Endpoint, "urn:echo", new EchoRequest { Value = "question" }, cancellationToken);

        response.Value.Should().Be("answer");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri.Should().Be(Endpoint);
        capturedRequest.Headers.GetValues("SOAPAction").Should().ContainSingle().Which.Should().Be("\"urn:echo\"");
        capturedRequest.Content!.Headers.ContentType!.MediaType.Should().Be("text/xml");
        XDocument requestDocument = XDocument.Parse(capturedBody!);
        XNamespace soap = SoapNamespace;
        XNamespace contract = ContractNamespace;
        requestDocument.Root!.Name.Should().Be(soap + "Envelope");
        requestDocument.Root.Element(soap + "Body")!.Elements().Should().ContainSingle().Which.Name.Should().Be(contract + "EchoRequest");
        requestDocument.Descendants(contract + "Value").Should().ContainSingle().Which.Value.Should().Be("question");
        handler.RequestCount.Should().Be(1);
        factory.LastClientName.Should().Be(SoapTransport.HttpClientName);
    }

    [Fact]
    public async Task SendAsync_allows_an_empty_soap_body_for_services_with_no_request_payload()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string? capturedBody = null;
        var handler = new RecordingHttpMessageHandler(async (request, token) =>
        {
            capturedBody = await request.Content!.ReadAsStringAsync(token);
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                SoapEnvelope($"<EchoResponse xmlns=\"{ContractNamespace}\"><Value>ok</Value></EchoResponse>"));
        });
        var transport = CreateTransport(handler);

        EchoResponse response = await transport.SendAsync<EchoRequest, EchoResponse>(
            Endpoint, "urn:echo", null!, cancellationToken);

        response.Value.Should().Be("ok");
        XDocument document = XDocument.Parse(capturedBody!);
        document.Root!.Element(XName.Get("Body", SoapNamespace))!.Elements().Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SendAsync_extracts_soap_fault_for_success_or_failure_http_status(HttpStatusCode statusCode)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            RecordingHttpMessageHandler.Response(statusCode, SoapFault("svc:denied", "Access denied", "tenant is inactive"))));
        var transport = CreateTransport(handler);

        Func<Task> send = () => transport.SendAsync<EchoRequest, EchoResponse>(
            Endpoint, "urn:echo", new EchoRequest { Value = "question" }, cancellationToken);

        var error = await send.Should().ThrowAsync<SoapFaultException>();
        error.Which.Code.Should().Be("svc:denied");
        error.Which.Reason.Should().Be("Access denied");
        error.Which.Detail.Should().Contain("tenant is inactive");
        error.Which.StatusCode.Should().Be(statusCode);
        handler.RequestCount.Should().Be(1);
    }

    [Theory]
    [InlineData("not XML")]
    [InlineData("<WrongRoot />")]
    [InlineData("<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Header /></s:Envelope>")]
    [InlineData("<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body /></s:Envelope>")]
    [InlineData("<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><EchoResponse xmlns=\"urn:netarcaws:soap-tests\"><Value>one</Value></EchoResponse><EchoResponse xmlns=\"urn:netarcaws:soap-tests\"><Value>two</Value></EchoResponse></s:Body></s:Envelope>")]
    [InlineData("<!DOCTYPE x [<!ENTITY e 'blocked'>]><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body><EchoResponse xmlns=\"urn:netarcaws:soap-tests\"><Value>&e;</Value></EchoResponse></s:Body></s:Envelope>")]
    public async Task SendAsync_rejects_malformed_wrong_root_missing_body_and_DTD(string responseXml)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            RecordingHttpMessageHandler.Response(HttpStatusCode.OK, responseXml)));
        var transport = CreateTransport(handler);

        Func<Task> send = () => transport.SendAsync<EchoRequest, EchoResponse>(
            Endpoint, "urn:echo", new EchoRequest { Value = "question" }, cancellationToken);

        await send.Should().ThrowAsync<FormatException>();
    }

    [Fact]
    public async Task SendAsync_throws_HttpRequestException_for_non_fault_http_failure_without_retry()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            RecordingHttpMessageHandler.Response(HttpStatusCode.ServiceUnavailable, "service unavailable")));
        var transport = CreateTransport(handler);

        Func<Task> send = () => transport.SendAsync<EchoRequest, EchoResponse>(
            Endpoint, "urn:echo", new EchoRequest { Value = "question" }, cancellationToken);

        await send.Should().ThrowAsync<HttpRequestException>();
        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_enforces_response_byte_limit()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string oversizedValue = new('x', 4096);
        string body = SoapEnvelope($"<EchoResponse xmlns=\"{ContractNamespace}\"><Value>{oversizedValue}</Value></EchoResponse>");
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            RecordingHttpMessageHandler.Response(HttpStatusCode.OK, body)));
        var transport = CreateTransport(handler, maxResponseBytes: 1024);

        Func<Task> send = () => transport.SendAsync<EchoRequest, EchoResponse>(
            Endpoint, "urn:echo", new EchoRequest { Value = "question" }, cancellationToken);

        await send.Should().ThrowAsync<FormatException>();
    }

    [Fact]
    public async Task SendAsync_enforces_request_byte_limit_before_sending_http()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            RecordingHttpMessageHandler.Response(HttpStatusCode.OK, SoapEnvelope("<EchoResponse />"))));
        var factory = new TestHttpClientFactory(handler);
        var transport = new SoapTransport(factory, Options.Create(new SoapTransportOptions { MaxRequestBytes = 1024 }));

        Func<Task> send = () => transport.SendAsync<EchoRequest, EchoResponse>(
            Endpoint, "urn:echo", new EchoRequest { Value = new string('x', 4096) }, cancellationToken);

        Exception? error = null;
        try { await send(); }
        catch (Exception exception) { error = exception; }
        error.Should().NotBeNull();
        (error is FormatException or InvalidOperationException).Should().BeTrue();
        handler.RequestCount.Should().Be(0, "serialization must reject the request before issuing HTTP");
    }

    [Fact]
    public async Task SendAsync_timeout_covers_response_body_read()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new BlockingReadHttpContent()
        }));
        var transport = CreateTransport(handler, clientTimeout: TimeSpan.FromMilliseconds(100));

        Func<Task> send = () => transport.SendAsync<EchoRequest, EchoResponse>(
            Endpoint, "urn:echo", new EchoRequest { Value = "question" }, cancellationToken);

        Func<Task> boundedSend = () => send().WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
        await boundedSend.Should().ThrowAsync<OperationCanceledException>();
        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_propagates_caller_cancellation_and_does_not_retry()
    {
        var requestEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new RecordingHttpMessageHandler(async (_, token) =>
        {
            requestEntered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK, SoapEnvelope("<EchoResponse><Value>never</Value></EchoResponse>"));
        });
        var transport = CreateTransport(handler);
        using var cancellation = new CancellationTokenSource();
        Task<EchoResponse> send = transport.SendAsync<EchoRequest, EchoResponse>(
            Endpoint, "urn:echo", new EchoRequest { Value = "question" }, cancellation.Token);
        await requestEntered.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        cancellation.Cancel();

        Func<Task> act = async () => { await send; };
        await act.Should().ThrowAsync<OperationCanceledException>();
        handler.RequestCount.Should().Be(1);
    }

    private static SoapTransport CreateTransport(HttpMessageHandler handler, int maxResponseBytes = 4 * 1024 * 1024, TimeSpan? clientTimeout = null) =>
        CreateTransport(new TestHttpClientFactory(handler, clientTimeout), maxResponseBytes);

    private static SoapTransport CreateTransport(TestHttpClientFactory factory, int maxResponseBytes = 4 * 1024 * 1024) =>
        new(factory, Options.Create(new SoapTransportOptions
        {
            MaxResponseBytes = maxResponseBytes
        }));

    private static string SoapEnvelope(string bodyChild) =>
        $"<s:Envelope xmlns:s=\"{SoapNamespace}\"><s:Body>{bodyChild}</s:Body></s:Envelope>";

    private static string SoapFault(string code, string reason, string detail) =>
        $"<s:Envelope xmlns:s=\"{SoapNamespace}\"><s:Body><s:Fault><faultcode>{code}</faultcode><faultstring>{reason}</faultstring><detail>{detail}</detail></s:Fault></s:Body></s:Envelope>";
}

[XmlRoot("EchoRequest", Namespace = "urn:netarcaws:soap-tests")]
public sealed class EchoRequest
{
    [XmlElement("Value", Namespace = "urn:netarcaws:soap-tests")]
    public string Value { get; set; } = string.Empty;
}

[XmlRoot("EchoResponse", Namespace = "urn:netarcaws:soap-tests")]
public sealed class EchoResponse
{
    [XmlElement("Value", Namespace = "urn:netarcaws:soap-tests")]
    public string Value { get; set; } = string.Empty;
}
