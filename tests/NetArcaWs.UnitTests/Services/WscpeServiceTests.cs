using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using NetArcaWs.Contracts.Wscpe;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using NetArcaWs.Tests.TestSupport;
using NetArcaWs.Transport;
using Xunit;

namespace NetArcaWs.Tests.Services;

public sealed class WscpeServiceTests
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string WscpeNamespace = "https://serviciosjava.afip.gob.ar/wscpe/";
    private const long TenantCuit = 30123456789;

    [Fact]
    public async Task Catalog_operation_uses_tenant_authentication_and_deserializes_populated_catalog()
    {
        string? sentSoap = null;
        var handler = new RecordingHttpMessageHandler(async (request, token) =>
        {
            sentSoap = await request.Content!.ReadAsStringAsync(token);
            request.Headers.GetValues("SOAPAction").Single()
                .Should().Be($"\"{WscpeNamespace}consultarProvincias\"");
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK, ProvincesResponse());
        });
        var factory = new TestHttpClientFactory(handler);
        var tickets = new RecordingArcaTicketProvider(TicketTestData.Create("CPE-TENANT-TOKEN", "CPE-TENANT-SIGN"));
        var service = new WscpeService(CreateTransport(factory), tickets);
        var tenant = CreateTenantContext("wscpe-catalog");
        var request = new ConsultarProvinciasRequest
        {
            Auth = new Auth { Token = "CALLER-TOKEN", Sign = "CALLER-SIGN", CuitRepresentada = 30987654321 }
        };

        ConsultarProvinciasResponse response = await service.consultarProvinciasAsync(
            tenant, request, TestContext.Current.CancellationToken);

        response.Respuesta.Provincia.Should().HaveCount(2);
        response.Respuesta.Provincia.Select(provincia => provincia.Codigo).Should().Equal("01", "06");
        response.Respuesta.Provincia.Select(provincia => provincia.Descripcion).Should().Equal("Buenos Aires", "Ciudad Autónoma de Buenos Aires");
        request.Auth.Token.Should().Be("CALLER-TOKEN");
        request.Auth.Sign.Should().Be("CALLER-SIGN");
        request.Auth.CuitRepresentada.Should().Be(30987654321);
        tickets.Calls.Should().ContainSingle().Which.Should().Be((WscpeService.TicketService, tenant));
        factory.LastClientName.Should().Be(SoapTransport.HttpClientName);

        XDocument sent = XDocument.Parse(sentSoap!);
        XElement operation = sent.Descendants(XName.Get("ConsultarProvinciasReq", WscpeNamespace)).Single();
        XElement auth = operation.Element(XName.Get("auth", string.Empty))!;
        auth.Element(XName.Get("token", string.Empty))!.Value.Should().Be("CPE-TENANT-TOKEN");
        auth.Element(XName.Get("sign", string.Empty))!.Value.Should().Be("CPE-TENANT-SIGN");
        auth.Element(XName.Get("cuitRepresentada", string.Empty))!.Value.Should().Be(TenantCuit.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Authorization_preserves_pdf_and_business_errors_without_mutating_or_resubmitting_request()
    {
        string? sentSoap = null;
        var handler = new RecordingHttpMessageHandler(async (request, token) =>
        {
            sentSoap = await request.Content!.ReadAsStringAsync(token);
            request.Headers.GetValues("SOAPAction").Single()
                .Should().Be($"\"{WscpeNamespace}autorizarCPEAutomotor\"");
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK, AuthorizationResponse());
        });
        var factory = new TestHttpClientFactory(handler);
        var tickets = new RecordingArcaTicketProvider(TicketTestData.Create("CPE-AUTH-TOKEN", "CPE-AUTH-SIGN"));
        var service = new WscpeService(CreateTransport(factory), tickets);
        var tenant = CreateTenantContext("wscpe-authorize");
        var request = new AutorizarCpeAutomotorRequest
        {
            Auth = new Auth { Token = "CALLER-TOKEN", Sign = "CALLER-SIGN", CuitRepresentada = 30987654321 },
            Solicitud = new AutorizarAutomotorSolicitud { Observaciones = "solicitud fixture" }
        };

        AutorizarCpeAutomotorResponse response = await service.autorizarCPEAutomotorAsync(
            tenant, request, TestContext.Current.CancellationToken);

        response.Respuesta.Pdf.Should().Equal(0x25, 0x50, 0x44, 0x46);
        response.Respuesta.Errores.Should().ContainSingle().Which.Codigo.Should().Be("CPE-TEST");
        response.Respuesta.Errores.Single().Descripcion.Should().Be("Solicitud de prueba observada");
        request.Auth.Token.Should().Be("CALLER-TOKEN");
        request.Auth.Sign.Should().Be("CALLER-SIGN");
        request.Auth.CuitRepresentada.Should().Be(30987654321);
        tickets.Calls.Should().ContainSingle().Which.Service.Should().Be(WscpeService.TicketService);
        handler.RequestCount.Should().Be(1);

        XDocument sent = XDocument.Parse(sentSoap!);
        XElement operation = sent.Descendants(XName.Get("AutorizarCPEAutomotorReq", WscpeNamespace)).Single();
        XElement auth = operation.Element(XName.Get("auth", string.Empty))!;
        auth.Element(XName.Get("token", string.Empty))!.Value.Should().Be("CPE-AUTH-TOKEN");
        auth.Element(XName.Get("sign", string.Empty))!.Value.Should().Be("CPE-AUTH-SIGN");
        auth.Element(XName.Get("cuitRepresentada", string.Empty))!.Value.Should().Be(TenantCuit.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Authorization_timeout_does_not_resubmit_an_uncertain_write()
    {
        var handler = new RecordingHttpMessageHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK, AuthorizationResponse());
        });
        var service = new WscpeService(CreateTransport(new TestHttpClientFactory(handler, TimeSpan.FromMilliseconds(100))),
            new RecordingArcaTicketProvider(TicketTestData.Create("CPE-TOKEN", "CPE-SIGN")));
        var request = new AutorizarCpeAutomotorRequest
        {
            Auth = new Auth { Token = "caller", Sign = "caller", CuitRepresentada = 30987654321 },
            Solicitud = new AutorizarAutomotorSolicitud { Observaciones = "timeout fixture" }
        };

        Func<Task> submit = async () => await service.autorizarCPEAutomotorAsync(
            CreateTenantContext("wscpe-timeout"), request, TestContext.Current.CancellationToken);

        await submit.Should().ThrowAsync<OperationCanceledException>();
        handler.RequestCount.Should().Be(1, "an uncertain write must not be resubmitted automatically");
    }

    private static string ProvincesResponse() => $"""
        <soap:Envelope xmlns:soap="{SoapNamespace}" xmlns:w="{WscpeNamespace}"><soap:Body>
          <w:ConsultarProvinciasResp><respuesta><provincia><codigo>01</codigo><descripcion>Buenos Aires</descripcion></provincia><provincia><codigo>06</codigo><descripcion>Ciudad Autónoma de Buenos Aires</descripcion></provincia></respuesta></w:ConsultarProvinciasResp>
        </soap:Body></soap:Envelope>
        """;

    private static string AuthorizationResponse() => $"""
        <soap:Envelope xmlns:soap="{SoapNamespace}" xmlns:w="{WscpeNamespace}"><soap:Body>
          <w:AutorizarCPEAutomotorResp><respuesta><pdf>JVBERg==</pdf><errores><error><codigo>CPE-TEST</codigo><descripcion>Solicitud de prueba observada</descripcion></error></errores></respuesta></w:AutorizarCPEAutomotorResp>
        </soap:Body></soap:Envelope>
        """;

    private static SoapTransport CreateTransport(TestHttpClientFactory factory) =>
        new(factory, Options.Create(new SoapTransportOptions()));

    private static ArcaTenantContext CreateTenantContext(string tenantId)
    {
        using var certificate = TestCertificates.CreateWithPrivateKey(tenantId);
        using var privateKey = certificate.GetRSAPrivateKey()!;
        WsaaCertificateContent certificateContent = WsaaCertificateContent.FromPem(
            certificate.ExportCertificatePem(), privateKey.ExportPkcs8PrivateKeyPem());
        return new ArcaTenantContext(tenantId, TenantCuit, ArcaEnvironment.Production, certificateContent);
    }
}
