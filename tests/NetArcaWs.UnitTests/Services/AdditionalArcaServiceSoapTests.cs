using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using NetArcaWs.Contracts.Wscdc;
using NetArcaWs.Contracts.WsfeCred;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using NetArcaWs.Tests.TestSupport;
using NetArcaWs.Transport;
using Xunit;

namespace NetArcaWs.Tests.Services;

public sealed class AdditionalArcaServiceSoapTests
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string WscdcNamespace = "http://servicios1.afip.gob.ar/wscdc/";
    private const string WsfecredNamespace = "http://ar.gob.afip.wsfecred/FECredService/";
    private const long TenantCuit = 30123456789;
    private const long CallerCuit = 30987654321;

    [Fact]
    public async Task Wscdc_constatar_preserves_business_errors_and_observations_and_uses_isolated_tenant_authentication()
    {
        string? sentSoap = null;
        var handler = new RecordingHttpMessageHandler(async (request, token) =>
        {
            sentSoap = await request.Content!.ReadAsStringAsync(token);
            request.Headers.GetValues("SOAPAction").Single().Should().Be($"\"{WscdcNamespace}ComprobanteConstatar\"");
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK, WscdcConstatacionResponse());
        });
        var factory = new TestHttpClientFactory(handler);
        var transport = CreateTransport(factory);
        var tickets = new RecordingArcaTicketProvider(TicketTestData.Create("TENANT-WSCDC-TOKEN", "TENANT-WSCDC-SIGN"));
        var service = new WscdcService(transport, tickets);
        using var certificate = TestCertificates.CreateWithPrivateKey("wscdc-soap-tenant");
        using var certificateKey = certificate.GetRSAPrivateKey()!;
        var certificateContent = WsaaCertificateContent.FromPem(
            certificate.ExportCertificatePem(), certificateKey.ExportPkcs8PrivateKeyPem());
        var tenant = new ArcaTenantContext("tenant-wscdc", TenantCuit, ArcaEnvironment.Homologation, certificateContent);
        var callerRequest = new ComprobanteConstatar
        {
            Auth = new CmpAuthRequest { Token = "CALLER-TOKEN", Sign = "CALLER-SIGN", Cuit = CallerCuit },
            CmpReq = new CmpDatos { CuitEmisor = TenantCuit, PtoVta = 4, CbteTipo = 1, CbteNro = 5678, ImpTotal = 1234.56 }
        };

        ComprobanteConstatarResponse response = await service.ComprobanteConstatarAsync(
            tenant, callerRequest, TestContext.Current.CancellationToken);

        response.ComprobanteConstatarResult.Resultado.Should().Be("R");
        response.ComprobanteConstatarResult.Observaciones.Should().ContainSingle().Which.Msg.Should().Be("Comprobante observado");
        response.ComprobanteConstatarResult.Errors.Should().ContainSingle().Which.Msg.Should().Be("CUIT emisor inválido");
        callerRequest.Auth.Token.Should().Be("CALLER-TOKEN");
        callerRequest.Auth.Sign.Should().Be("CALLER-SIGN");
        callerRequest.Auth.Cuit.Should().Be(CallerCuit);
        tickets.Calls.Should().ContainSingle().Which.Should().Be(("wscdc", tenant));

        XDocument sent = XDocument.Parse(sentSoap!);
        XElement body = sent.Descendants(XName.Get("Body", SoapNamespace)).Single();
        XElement operation = body.Elements(XName.Get("ComprobanteConstatar", WscdcNamespace)).Should().ContainSingle().Which;
        XElement auth = operation.Element(XName.Get("Auth", WscdcNamespace))!;
        auth.Element(XName.Get("Token", WscdcNamespace))!.Value.Should().Be("TENANT-WSCDC-TOKEN");
        auth.Element(XName.Get("Sign", WscdcNamespace))!.Value.Should().Be("TENANT-WSCDC-SIGN");
        auth.Element(XName.Get("Cuit", WscdcNamespace))!.Value.Should().Be(TenantCuit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        operation.Descendants(XName.Get("CbteNro", WscdcNamespace)).Single().Value.Should().Be("5678");
        factory.LastClientName.Should().Be(SoapTransport.HttpClientName);
    }

    [Fact]
    public async Task Wsfecred_accept_response_keeps_rejection_errors_and_format_errors_and_uses_isolated_tenant_authentication()
    {
        string? sentSoap = null;
        var handler = new RecordingHttpMessageHandler(async (request, token) =>
        {
            sentSoap = await request.Content!.ReadAsStringAsync(token);
            request.Headers.GetValues("SOAPAction").Single().Should().Be($"\"{WsfecredNamespace}aceptarFECred\"");
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK, WsfecredAcceptResponse());
        });
        var factory = new TestHttpClientFactory(handler);
        var transport = CreateTransport(factory);
        var tickets = new RecordingArcaTicketProvider(TicketTestData.Create("TENANT-FECRED-TOKEN", "TENANT-FECRED-SIGN"));
        var service = new WsfecredService(transport, tickets);
        using var certificate = TestCertificates.CreateWithPrivateKey("wsfecred-soap-tenant");
        using var certificateKey = certificate.GetRSAPrivateKey()!;
        var certificateContent = WsaaCertificateContent.FromPem(
            certificate.ExportCertificatePem(), certificateKey.ExportPkcs8PrivateKeyPem());
        var tenant = new ArcaTenantContext("tenant-wsfecred", TenantCuit, ArcaEnvironment.Homologation, certificateContent);
        var callerRequest = new AceptarFeCredRequestType
        {
            AuthRequest = new AuthRequestType { Token = "CALLER-TOKEN", Sign = "CALLER-SIGN", CuitRepresentada = CallerCuit },
            IdCtaCte = new IdCtaCteType { CodCtaCte = 8765 }
        };

        AceptarFeCredResponse response = await service.aceptarFECredAsync(
            tenant, callerRequest, TestContext.Current.CancellationToken);

        response.OperacionFeCredReturn.Resultado.Should().Be(ResultadoSimpleType.R);
        response.OperacionFeCredReturn.ArrayErrores.Should().ContainSingle().Which.Descripcion.Should().Be("FCE no aceptable");
        response.OperacionFeCredReturn.ArrayErroresFormato.Should().ContainSingle().Which.Descripcion.Should().Be("El importe tiene un formato inválido");
        callerRequest.AuthRequest.Token.Should().Be("CALLER-TOKEN");
        callerRequest.AuthRequest.Sign.Should().Be("CALLER-SIGN");
        callerRequest.AuthRequest.CuitRepresentada.Should().Be(CallerCuit);
        tickets.Calls.Should().ContainSingle().Which.Should().Be(("wsfecred", tenant));

        XDocument sent = XDocument.Parse(sentSoap!);
        XElement body = sent.Descendants(XName.Get("Body", SoapNamespace)).Single();
        XElement operation = body.Elements(XName.Get("aceptarFECredRequest", WsfecredNamespace)).Should().ContainSingle().Which;
        XElement auth = operation.Element(XName.Get("authRequest", string.Empty))!;
        auth.Element(XName.Get("token", string.Empty))!.Value.Should().Be("TENANT-FECRED-TOKEN");
        auth.Element(XName.Get("sign", string.Empty))!.Value.Should().Be("TENANT-FECRED-SIGN");
        auth.Element(XName.Get("cuitRepresentada", string.Empty))!.Value.Should().Be(TenantCuit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        operation.Descendants(XName.Get("codCtaCte", string.Empty)).Single().Value.Should().Be("8765");
        factory.LastClientName.Should().Be(SoapTransport.HttpClientName);
    }

    [Fact]
    public async Task Wsfecred_write_timeout_does_not_retry_an_uncertain_submission()
    {
        var handler = new RecordingHttpMessageHandler(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK, WsfecredAcceptResponse());
        });
        var factory = new TestHttpClientFactory(handler, TimeSpan.FromMilliseconds(100));
        var service = new WsfecredService(CreateTransport(factory),
            new RecordingArcaTicketProvider(TicketTestData.Create("TENANT-TOKEN", "TENANT-SIGN")));
        using var certificate = TestCertificates.CreateWithPrivateKey("wsfecred-timeout");
        using var certificateKey = certificate.GetRSAPrivateKey()!;
        var certificateContent = WsaaCertificateContent.FromPem(
            certificate.ExportCertificatePem(), certificateKey.ExportPkcs8PrivateKeyPem());
        var tenant = new ArcaTenantContext("tenant-timeout", TenantCuit, ArcaEnvironment.Homologation, certificateContent);
        var request = new AceptarFeCredRequestType
        {
            AuthRequest = new AuthRequestType { Token = "caller", Sign = "caller", CuitRepresentada = CallerCuit },
            IdCtaCte = new IdCtaCteType { CodCtaCte = 9876 }
        };

        Func<Task> submit = async () => await service.aceptarFECredAsync(tenant, request, TestContext.Current.CancellationToken);
        await submit.Should().ThrowAsync<OperationCanceledException>();
        handler.RequestCount.Should().Be(1, "an uncertain write must not be resubmitted automatically");
    }

    [Fact]
    public void IdCtaCte_requires_exactly_one_choice_and_serializes_either_allowed_branch()
    {
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(IdCtaCteType));
        Action neitherChoice = () => Serialize(serializer, new IdCtaCteType());
        Action bothChoices = () => Serialize(serializer, new IdCtaCteType
        {
            CodCtaCte = 8765,
            IdFactura = new IdComprobanteType { CuitEmisor = TenantCuit, CodTipoCmp = 1, PtoVta = 4, NroCmp = 5678 }
        });

        neitherChoice.Should().Throw<InvalidOperationException>();
        bothChoices.Should().Throw<InvalidOperationException>();

        XDocument byAccount = XDocument.Parse(Serialize(serializer, new IdCtaCteType { CodCtaCte = 8765 }));
        byAccount.Descendants().Select(element => element.Name.LocalName).Should().Contain("codCtaCte").And.NotContain("idFactura");
        XDocument byInvoice = XDocument.Parse(Serialize(serializer, new IdCtaCteType
        {
            IdFactura = new IdComprobanteType { CuitEmisor = TenantCuit, CodTipoCmp = 1, PtoVta = 4, NroCmp = 5678 }
        }));
        byInvoice.Descendants().Select(element => element.Name.LocalName).Should().Contain("idFactura").And.NotContain("codCtaCte");
    }

    [Fact]
    public void InfoTransferencia_requires_exactly_one_choice_and_serializes_either_allowed_branch()
    {
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(InfoTransferenciaType));
        Action neitherChoice = () => Serialize(serializer, new InfoTransferenciaType());
        Action bothChoices = () => Serialize(serializer, new InfoTransferenciaType
        {
            InfoAgtDptoCltv = CreateInfoAgtDptoCltv(),
            InfoSca = CreateInfoSca()
        });

        neitherChoice.Should().Throw<InvalidOperationException>();
        bothChoices.Should().Throw<InvalidOperationException>();

        XDocument byAgent = XDocument.Parse(Serialize(serializer, new InfoTransferenciaType
        {
            InfoAgtDptoCltv = CreateInfoAgtDptoCltv()
        }));
        byAgent.Descendants().Select(element => element.Name.LocalName).Should().Contain("infoAgtDptoCltv").And.NotContain("infoSCA");
        XDocument bySca = XDocument.Parse(Serialize(serializer, new InfoTransferenciaType
        {
            InfoSca = CreateInfoSca()
        }));
        bySca.Descendants().Select(element => element.Name.LocalName).Should().Contain("infoSCA").And.NotContain("infoAgtDptoCltv");
    }

    [Fact]
    public async Task Invalid_id_cta_cte_choice_is_rejected_before_ticket_or_http_request()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            Task.FromResult(RecordingHttpMessageHandler.Response(HttpStatusCode.OK, WsfecredAcceptResponse())));
        var factory = new TestHttpClientFactory(handler);
        var tickets = new RecordingArcaTicketProvider(TicketTestData.Create("TENANT-TOKEN", "TENANT-SIGN"));
        var service = new WsfecredService(CreateTransport(factory), tickets);
        using var certificate = TestCertificates.CreateWithPrivateKey("invalid-cta-cte");
        using var certificateKey = certificate.GetRSAPrivateKey()!;
        var certificateContent = WsaaCertificateContent.FromPem(
            certificate.ExportCertificatePem(), certificateKey.ExportPkcs8PrivateKeyPem());
        var tenant = new ArcaTenantContext("tenant-invalid-id", TenantCuit, ArcaEnvironment.Homologation, certificateContent);
        var request = new ConsultarCtaCteRequestType
        {
            AuthRequest = new AuthRequestType { Token = "caller", Sign = "caller", CuitRepresentada = CallerCuit },
            IdCtaCte = new IdCtaCteType()
        };

        Func<Task> call = async () => await service.consultarCtaCteAsync(tenant, request, TestContext.Current.CancellationToken);
        await call.Should().ThrowAsync<InvalidOperationException>();
        tickets.CallCount.Should().Be(0);
        handler.RequestCount.Should().Be(0);
    }

    private static SoapTransport CreateTransport(TestHttpClientFactory factory) =>
        new(factory, Options.Create(new SoapTransportOptions()));

    private static string Serialize(System.Xml.Serialization.XmlSerializer serializer, object value)
    {
        using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        serializer.Serialize(writer, value);
        return writer.ToString();
    }

    private static InfoAgtDptoCltvType CreateInfoAgtDptoCltv() => new()
    {
        FechaInfo = new DateTime(2026, 10, 6),
        CtaAgente = new CuentaEnAgenteType
        {
            CuitAgente = TenantCuit,
            IdCuenta = "acct-1",
            Denominacion = "Agente"
        },
        Recibida = SiNoSimpleType.S
    };

    private static InfoScaType CreateInfoSca() => new()
    {
        FechaAceptacionFactura = new DateTime(2026, 10, 6),
        InformaCbuReceptor = SiNoSimpleType.N,
        CbuReceptor = "1234567890123456789012"
    };

    private static string WscdcConstatacionResponse() => $"""
        <soap:Envelope xmlns:soap="{SoapNamespace}" xmlns:ws="{WscdcNamespace}">
          <soap:Body>
            <ws:ComprobanteConstatarResponse>
              <ws:ComprobanteConstatarResult>
                <ws:Resultado>R</ws:Resultado>
                <ws:Observaciones><ws:Obs><ws:Code>101</ws:Code><ws:Msg>Comprobante observado</ws:Msg></ws:Obs></ws:Observaciones>
                <ws:Errors><ws:Err><ws:Code>500</ws:Code><ws:Msg>CUIT emisor inválido</ws:Msg></ws:Err></ws:Errors>
              </ws:ComprobanteConstatarResult>
            </ws:ComprobanteConstatarResponse>
          </soap:Body>
        </soap:Envelope>
        """;

    private static string WsfecredAcceptResponse() => $"""
        <soap:Envelope xmlns:soap="{SoapNamespace}" xmlns:ws="{WsfecredNamespace}">
          <soap:Body>
            <ws:aceptarFECredResponse>
              <operacionFECredReturn xmlns="">
                <resultado>R</resultado>
                <idCtaCte><codCtaCte>8765</codCtaCte></idCtaCte>
                <arrayErrores><codigoDescripcion><codigo>17</codigo><descripcion>FCE no aceptable</descripcion></codigoDescripcion></arrayErrores>
                <arrayErroresFormato><codigoDescripcionString><codigo>IMPORTE</codigo><descripcion>El importe tiene un formato inválido</descripcion></codigoDescripcionString></arrayErroresFormato>
              </operacionFECredReturn>
            </ws:aceptarFECredResponse>
          </soap:Body>
        </soap:Envelope>
        """;
}
