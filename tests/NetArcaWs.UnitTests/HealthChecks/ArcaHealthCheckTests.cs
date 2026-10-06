using System.Net;
using System.Text;
using System.Xml.Linq;
using AwesomeAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetArcaWs.HealthChecks;
using Xunit;

namespace NetArcaWs.Tests.HealthChecks;

public sealed class ArcaHealthCheckTests
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string WsfeNamespace = "http://ar.gov.afip.dif.FEV1/";
    private const string WsfexNamespace = "http://ar.gov.afip.dif.fexv1/";
    private const string WsdlNamespace = "http://schemas.xmlsoap.org/wsdl/";

    [Theory]
    [InlineData(ArcaService.Wsfev1, "FEDummy", WsfeNamespace, "\"http://ar.gov.afip.dif.FEV1/FEDummy\"")]
    [InlineData(ArcaService.Wsfexv1, "FEXDummy", WsfexNamespace, "\"http://ar.gov.afip.dif.fexv1/FEXDummy\"")]
    [InlineData(ArcaService.Wsmtxca, "", "http://impl.service.wsmtxca.afip.gov.ar/service/", "\"http://impl.service.wsmtxca.afip.gov.ar/service/dummy\"")]
    [InlineData(ArcaService.PadronA4, "dummy", "http://a4.soap.ws.server.puc.sr/", "\"\"")]
    [InlineData(ArcaService.PadronA5, "dummy", "http://a5.soap.ws.server.puc.sr/", "\"\"")]
    [InlineData(ArcaService.PadronA10, "dummy", "http://a10.soap.ws.server.puc.sr/", "\"\"")]
    [InlineData(ArcaService.PadronA13, "dummy", "http://a13.soap.ws.server.puc.sr/", "\"\"")]
    public async Task Dummy_all_three_components_OK_returns_Healthy_and_reports_probe_values(
        ArcaService service, string requestOperation, string serviceNamespace, string expectedSoapAction)
    {
        HttpMethod? capturedMethod = null;
        Uri? capturedUri = null;
        string? capturedAction = null;
        string? requestBody = null;
        var handler = new RecordingHandler(async (request, cancellationToken) =>
        {
            capturedMethod = request.Method;
            capturedUri = request.RequestUri;
            capturedAction = request.Headers.GetValues("SOAPAction").Single();
            requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Response(HttpStatusCode.OK, DummyResponse(service, "OK", "OK", "OK"));
        });
        var check = CreateCheck(service, handler);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Healthy,
            $"description: {result.Description}; data: {string.Join(", ", result.Data.Select(entry => $"{entry.Key}={entry.Value}"))}");
        result.Data["probe"].Should().Be("dummy");
        result.Data["service"].Should().Be(service.ToString());
        result.Data["environment"].Should().Be(ArcaEnvironment.Homologation.ToString());
        result.Data["appServer"].Should().Be("OK");
        result.Data["dbServer"].Should().Be("OK");
        result.Data["authServer"].Should().Be("OK");
        result.Data["durationMs"].Should().BeOfType<double>();
        result.Data["authenticationVerified"].Should().Be(false);
        capturedMethod.Should().Be(HttpMethod.Post);
        capturedUri.Should().NotBeNull();
        capturedAction.Should().Be(expectedSoapAction);
        XDocument request = XDocument.Parse(requestBody!);
        XElement body = request.Descendants(XName.Get("Body", SoapNamespace)).Should().ContainSingle().Which;
        if (service == ArcaService.Wsmtxca)
            body.Elements().Should().BeEmpty();
        else
            body.Elements(XName.Get(requestOperation, serviceNamespace)).Should().ContainSingle();
    }

    [Theory]
    [InlineData("appServer")]
    [InlineData("dbServer")]
    [InlineData("authServer")]
    public async Task Dummy_any_component_reported_as_NO_returns_Unhealthy(string failedComponent)
    {
        string app = failedComponent == "appServer" ? "NO" : "OK";
        string db = failedComponent == "dbServer" ? "NO" : "OK";
        string auth = failedComponent == "authServer" ? "NO" : "OK";
        var check = CreateCheck(ArcaService.Wsfev1,
            new RecordingHandler((_, _) => Task.FromResult(Response(HttpStatusCode.OK, DummyResponse(ArcaService.Wsfev1, app, db, auth)))));

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data[failedComponent].Should().Be("NO");
        result.Data["probe"].Should().Be("dummy");
    }

    [Fact]
    public async Task Dummy_SOAP_Fault_returns_Unhealthy_with_a_bounded_failure_description()
    {
        var check = CreateCheck(ArcaService.Wsfev1,
            new RecordingHandler((_, _) => Task.FromResult(Response(HttpStatusCode.OK, SoapFault("server unavailable")))));

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Dummy_HTTP_500_returns_Unhealthy()
    {
        var check = CreateCheck(ArcaService.Wsfev1,
            new RecordingHandler((_, _) => Task.FromResult(Response(HttpStatusCode.InternalServerError, "server error"))));

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Theory]
    [InlineData("<soap:Envelope><soap:Body><broken>")]
    [InlineData("<!DOCTYPE Envelope [<!ENTITY xxe SYSTEM 'file:///etc/passwd'>]><Envelope>&xxe;</Envelope>")]
    public async Task Dummy_malformed_or_DTD_XML_returns_Unhealthy_without_resolving_entities(string body)
    {
        var check = CreateCheck(ArcaService.Wsfev1,
            new RecordingHandler((_, _) => Task.FromResult(Response(HttpStatusCode.OK, body))));

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Dummy_timeout_while_reading_response_body_returns_Unhealthy()
    {
        var handler = new RecordingHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new BlockingReadContent() }));
        var check = CreateCheck(ArcaService.Wsfev1, handler, timeout: TimeSpan.FromMilliseconds(100));

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Dummy_user_cancellation_propagates_instead_of_becoming_Unhealthy()
    {
        var handler = new RecordingHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Response(HttpStatusCode.OK, "");
        });
        var check = CreateCheck(ArcaService.Wsfev1, handler, timeout: TimeSpan.FromSeconds(5));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(50));

        Func<Task> run = () => check.CheckHealthAsync(new HealthCheckContext(), cancellation.Token);

        await run.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Wsaa_probe_validates_WSDL_loginCms_shape_and_explicitly_does_not_claim_authentication()
    {
        HttpMethod? capturedMethod = null;
        Uri? capturedUri = null;
        bool hasRequestContent = false;
        var handler = new RecordingHandler((request, _) =>
        {
            capturedMethod = request.Method;
            capturedUri = request.RequestUri;
            hasRequestContent = request.Content is not null;
            return Task.FromResult(Response(HttpStatusCode.OK, ValidWsdl()));
        });
        var check = CreateCheck(ArcaService.Wsaa, handler);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["probe"].Should().Be("wsdl");
        result.Data["authenticationVerified"].Should().Be(false);
        capturedMethod.Should().Be(HttpMethod.Get);
        capturedUri!.Query.Should().Be("?WSDL");
        hasRequestContent.Should().BeFalse();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "")]
    [InlineData(HttpStatusCode.OK, "<definitions><portType><operation name=\"other\"/></portType></definitions>")]
    [InlineData(HttpStatusCode.OK, "<wsdl:definitions xmlns:wsdl=\"http://schemas.xmlsoap.org/wsdl/\"><wsdl:portType/></wsdl:definitions>")]
    [InlineData(HttpStatusCode.OK, "<!DOCTYPE definitions [<!ENTITY xxe SYSTEM 'file:///etc/passwd'>]><definitions xmlns=\"http://schemas.xmlsoap.org/wsdl/\">&xxe;</definitions>")]
    public async Task Wsaa_probe_malformed_unavailable_or_unexpected_WSDL_returns_Unhealthy(HttpStatusCode status, string body)
    {
        var handler = new RecordingHandler((_, _) => Task.FromResult(Response(status, body)));
        var check = CreateCheck(ArcaService.Wsaa, handler);

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Wsaa_timeout_while_reading_WSDL_returns_Unhealthy()
    {
        var handler = new RecordingHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new BlockingReadContent() }));
        var check = CreateCheck(ArcaService.Wsaa, handler, timeout: TimeSpan.FromMilliseconds(100));

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    private static ArcaHealthCheck CreateCheck(
        ArcaService service,
        RecordingHandler handler,
        TimeSpan? timeout = null) => new(new RecordingClientFactory(handler), new ArcaHealthCheckOptions
    {
        Service = service,
        Environment = ArcaEnvironment.Homologation,
        Endpoint = new Uri("https://healthcheck.test/service"),
        Timeout = timeout ?? TimeSpan.FromSeconds(2),
        MaxResponseBytes = 128 * 1024
    });

    private static string DummyResponse(ArcaService service, string appServer, string dbServer, string authServer)
    {
        if (service == ArcaService.Wsmtxca)
            return SoapResponse("http://impl.service.wsmtxca.afip.gov.ar/service/", "dummyResponse",
                null, appServer, dbServer, authServer);
        if (service is ArcaService.PadronA4 or ArcaService.PadronA5 or ArcaService.PadronA10 or ArcaService.PadronA13)
            return SoapResponse(ServiceNamespace(service), "dummyResponse", "return", appServer, dbServer, authServer);
        string serviceNamespace = ServiceNamespace(service);
        string prefix = service == ArcaService.Wsfev1 ? "FE" : "FEX";
        return SoapResponse(serviceNamespace, $"{prefix}DummyResponse", $"{prefix}DummyResult", appServer, dbServer, authServer);
    }

    private static string SoapResponse(string serviceNamespace, string responseName, string? resultName,
        string appServer, string dbServer, string authServer)
    {
        XNamespace soap = SoapNamespace;
        XNamespace ns = serviceNamespace;
        XNamespace none = XNamespace.None;
        string appName = responseName == "FEDummyResponse" || responseName == "FEXDummyResponse" ? "AppServer" : "appserver";
        string dbName = responseName == "FEDummyResponse" || responseName == "FEXDummyResponse" ? "DbServer" : "dbserver";
        string authName = responseName == "FEDummyResponse" || responseName == "FEXDummyResponse" ? "AuthServer" : "authserver";
        XNamespace componentNs = serviceNamespace.Contains("puc.sr", StringComparison.Ordinal) || serviceNamespace.Contains("wsmtxca", StringComparison.Ordinal)
            ? XNamespace.None
            : ns;
        object componentElements = new object[]
        {
            new XElement(componentNs + appName, appServer),
            new XElement(componentNs + dbName, dbServer),
            new XElement(componentNs + authName, authServer)
        };
        XElement response = new(ns + responseName,
            resultName is null ? componentElements : new XElement(resultName == "return" ? none + resultName : ns + resultName, componentElements));
        return new XDocument(new XElement(soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", soap),
            new XAttribute(XNamespace.Xmlns + "ns", ns),
            new XElement(soap + "Body", response)))
            .ToString(SaveOptions.DisableFormatting);
    }

    private static string SoapFault(string faultString)
    {
        XNamespace soap = SoapNamespace;
        return new XDocument(new XElement(soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", soap),
            new XElement(soap + "Body",
                new XElement(soap + "Fault",
                    new XElement("faultcode", "soap:Server"),
                    new XElement("faultstring", faultString)))))
            .ToString(SaveOptions.DisableFormatting);
    }

    private static string ValidWsdl() => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <definitions xmlns="{WsdlNamespace}" name="LoginCmsService">
          <portType name="LoginCmsPortType">
            <operation name="loginCms" />
          </portType>
        </definitions>
        """;

    private static string ServiceNamespace(ArcaService service) => service switch
    {
        ArcaService.Wsfev1 => WsfeNamespace,
        ArcaService.Wsfexv1 => WsfexNamespace,
        ArcaService.PadronA4 => "http://a4.soap.ws.server.puc.sr/",
        ArcaService.PadronA5 => "http://a5.soap.ws.server.puc.sr/",
        ArcaService.PadronA10 => "http://a10.soap.ws.server.puc.sr/",
        ArcaService.PadronA13 => "http://a13.soap.ws.server.puc.sr/",
        _ => throw new ArgumentOutOfRangeException(nameof(service), service, "This test only covers known Dummy schemas.")
    };

    private static HttpResponseMessage Response(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "text/xml")
    };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            responder(request, cancellationToken);
    }

    private sealed class RecordingClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class BlockingReadContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context) =>
            throw new NotSupportedException();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new BlockingReadStream());
    }

    private sealed class BlockingReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
    }
}
