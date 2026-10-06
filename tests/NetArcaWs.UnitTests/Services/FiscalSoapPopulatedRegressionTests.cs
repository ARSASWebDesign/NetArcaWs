using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using NetArcaWs.Contracts.PadronA5;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using NetArcaWs.Tests.TestSupport;
using NetArcaWs.Transport;
using Xunit;

namespace NetArcaWs.Tests.Services;

public sealed class FiscalSoapPopulatedRegressionTests
{
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string WsfeNamespace = "http://ar.gov.afip.dif.FEV1/";
    private const string PadronNamespace = "http://a5.soap.ws.server.puc.sr/";
    private const long TenantCuit = 30123456789;
    private const long SubjectCuit = 30712345678;

    [Fact]
    public async Task Wsfe_populated_CAE_and_CAEA_requests_preserve_fiscal_fields_dates_and_multiple_details()
    {
        var requests = new List<XDocument>();
        var handler = new RecordingHttpMessageHandler(async (request, token) =>
        {
            requests.Add(XDocument.Parse(await request.Content!.ReadAsStringAsync(token)));
            return RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                requests.Count == 1 ? CaeResponse() : CaeaResponse());
        });
        var factory = new TestHttpClientFactory(handler);
        var service = new Wsfev1Service(CreateTransport(factory),
            new RecordingArcaTicketProvider(TicketTestData.Create("TENANT-TOKEN", "TENANT-SIGN")));
        var tenant = CreateTenantContext("fiscal-regression");

        var caeRequest = new FecaeSolicitar
        {
            Auth = new FeAuthRequest(),
            FeCaeReq = new FecaeRequest
            {
                FeCabReq = new FecaeCabRequest { CantReg = 2, PtoVta = 12, CbteTipo = 1 }
            }
        };
        caeRequest.FeCaeReq.FeDetReq.Add(CreateDetail(1001, "S", 1));
        caeRequest.FeCaeReq.FeDetReq.Add(CreateDetail(1002, "N", 5));

        FecaeSolicitarResponse cae = await service.FECAESolicitarAsync(tenant, caeRequest, TestContext.Current.CancellationToken);

        cae.FecaeSolicitarResult.FeDetResp.Should().HaveCount(2);
        cae.FecaeSolicitarResult.FeDetResp.Select(item => item.CbteDesde).Should().Equal(1001, 1002);
        cae.FecaeSolicitarResult.FeDetResp.Select(item => item.Cae).Should().Equal("74123456789012", "74123456789013");
        XDocument caeWire = requests[0];
        XElement caeOperation = Operation(caeWire, "FECAESolicitar");
        XElement caeDetails = caeOperation.Descendants(XName.Get("FECAEDetRequest", WsfeNamespace)).First().Parent!;
        caeDetails.Elements(XName.Get("FECAEDetRequest", WsfeNamespace)).Should().HaveCount(2);
        XElement first = caeDetails.Elements(XName.Get("FECAEDetRequest", WsfeNamespace)).First();
        first.Element(XName.Get("FchServDesde", WsfeNamespace))!.Value.Should().Be("20261001");
        first.Element(XName.Get("FchServHasta", WsfeNamespace))!.Value.Should().Be("20261031");
        first.Element(XName.Get("FchVtoPago", WsfeNamespace))!.Value.Should().Be("20261110");
        first.Element(XName.Get("CanMisMonExt", WsfeNamespace))!.Value.Should().Be("S");
        first.Element(XName.Get("CondicionIVAReceptorId", WsfeNamespace))!.Value.Should().Be("1");
        XElement second = caeDetails.Elements(XName.Get("FECAEDetRequest", WsfeNamespace)).Last();
        second.Element(XName.Get("CbteDesde", WsfeNamespace))!.Value.Should().Be("1002");
        second.Element(XName.Get("FchServDesde", WsfeNamespace))!.Value.Should().Be("20261101");
        second.Element(XName.Get("FchServHasta", WsfeNamespace))!.Value.Should().Be("20261130");
        second.Element(XName.Get("FchVtoPago", WsfeNamespace))!.Value.Should().Be("20261210");
        second.Element(XName.Get("CanMisMonExt", WsfeNamespace))!.Value.Should().Be("N");
        second.Element(XName.Get("CondicionIVAReceptorId", WsfeNamespace))!.Value.Should().Be("5");

        var caeaRequest = new FecaeaRegInformativo
        {
            Auth = new FeAuthRequest(),
            FeCaeaRegInfReq = new FecaeaRequest
            {
                FeCabReq = new FecaeaCabRequest { CantReg = 2, PtoVta = 12, CbteTipo = 1 }
            }
        };
        caeaRequest.FeCaeaRegInfReq.FeDetReq.Add(CreateCaeaDetail(2001, 7));
        caeaRequest.FeCaeaRegInfReq.FeDetReq.Add(CreateCaeaDetail(2002, 8));

        FecaeaRegInformativoResponse caea = await service.FECAEARegInformativoAsync(tenant, caeaRequest, TestContext.Current.CancellationToken);

        caea.FecaeaRegInformativoResult.FeDetResp.Should().HaveCount(2);
        caea.FecaeaRegInformativoResult.FeDetResp.Select(item => item.CbteDesde).Should().Equal(2001, 2002);
        caea.FecaeaRegInformativoResult.FeDetResp.Select(item => item.Caea).Should().Equal("20261000001234", "20261000005678");
        XElement caeaOperation = Operation(requests[1], "FECAEARegInformativo");
        XElement[] caeaDetails = caeaOperation.Descendants(XName.Get("FECAEADetRequest", WsfeNamespace)).ToArray();
        caeaDetails.Should().HaveCount(2);
        caeaDetails[0].Element(XName.Get("CAEA", WsfeNamespace))!.Value.Should().Be("20261000001234");
        caeaDetails[0].Element(XName.Get("FchServDesde", WsfeNamespace))!.Value.Should().Be("20261001");
        caeaDetails[0].Element(XName.Get("FchServHasta", WsfeNamespace))!.Value.Should().Be("20261031");
        caeaDetails[0].Element(XName.Get("FchVtoPago", WsfeNamespace))!.Value.Should().Be("20261110");
        caeaDetails[0].Element(XName.Get("CanMisMonExt", WsfeNamespace))!.Value.Should().Be("S");
        caeaDetails[0].Element(XName.Get("CondicionIVAReceptorId", WsfeNamespace))!.Value.Should().Be("7");
        caeaDetails[0].Element(XName.Get("CbteFchHsGen", WsfeNamespace))!.Value.Should().Be("20261006120000");
        caeaDetails[1].Element(XName.Get("CAEA", WsfeNamespace))!.Value.Should().Be("20261000005678");
        caeaDetails[1].Element(XName.Get("FchServDesde", WsfeNamespace))!.Value.Should().Be("20261101");
        caeaDetails[1].Element(XName.Get("FchServHasta", WsfeNamespace))!.Value.Should().Be("20261130");
        caeaDetails[1].Element(XName.Get("FchVtoPago", WsfeNamespace))!.Value.Should().Be("20261210");
        caeaDetails[1].Element(XName.Get("CanMisMonExt", WsfeNamespace))!.Value.Should().Be("N");
        caeaDetails[1].Element(XName.Get("CondicionIVAReceptorId", WsfeNamespace))!.Value.Should().Be("8");
        caeaDetails[1].Element(XName.Get("CbteFchHsGen", WsfeNamespace))!.Value.Should().Be("20261106130000");
        caeaOperation.Descendants(XName.Get("Auth", WsfeNamespace)).Single()
            .Element(XName.Get("Token", WsfeNamespace))!.Value.Should().Be("TENANT-TOKEN");
        factory.LastClientName.Should().Be(SoapTransport.HttpClientName);
    }

    [Fact]
    public async Task Padron_A5_deserializes_multiple_tax_states_motives_null_sections_and_error_details()
    {
        var call = 0;
        var handler = new RecordingHttpMessageHandler((_, _) =>
        {
            call++;
            return Task.FromResult(RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                call == 1 ? PadronPopulatedResponse() : PadronErrorResponse()));
        });
        var service = new PadronA5Service(CreateTransport(new TestHttpClientFactory(handler)),
            new RecordingArcaTicketProvider(TicketTestData.Create("PADRON-TOKEN", "PADRON-SIGN")));
        var tenant = CreateTenantContext("padron-regression");
        var request = new GetPersona { CuitRepresentada = TenantCuit, IdPersona = SubjectCuit };

        GetPersonaResponse populated = await service.getPersonaAsync(tenant, request, TestContext.Current.CancellationToken);

        populated.PersonaReturn.DatosRegimenGeneral.Impuesto.Should().HaveCount(3);
        populated.PersonaReturn.DatosRegimenGeneral.Impuesto[0].EstadoImpuesto.Should().Be("ACTIVO");
        populated.PersonaReturn.DatosRegimenGeneral.Impuesto[0].Motivo.Should().Be("Alta de oficio");
        populated.PersonaReturn.DatosRegimenGeneral.Impuesto[0].IdImpuesto.Should().Be(30);
        populated.PersonaReturn.DatosRegimenGeneral.Impuesto[1].EstadoImpuesto.Should().Be("NO_INSCRIPTO");
        populated.PersonaReturn.DatosRegimenGeneral.Impuesto[1].Motivo.Should().Be("Sin actividad gravada");
        populated.PersonaReturn.DatosRegimenGeneral.Impuesto[2].Should().BeNull();

        GetPersonaResponse errors = await service.getPersonaAsync(tenant, request, TestContext.Current.CancellationToken);

        errors.PersonaReturn.DatosRegimenGeneral.Should().BeNull();
        errors.PersonaReturn.DatosMonotributo.Should().BeNull();
        errors.PersonaReturn.ErrorConstancia.Error.Should().ContainSingle().Which.Should().Be("No se encontró la persona solicitada");
        errors.PersonaReturn.ErrorRegimenGeneral.Error.Should().ContainSingle().Which.Should().Be("Consulta tributaria no disponible");
        handler.RequestCount.Should().Be(2);
    }

    private static FecaeDetRequest CreateDetail(long number, string mixedForeignCurrency, int ivaCondition) => new()
    {
        Concepto = 2,
        DocTipo = 80,
        DocNro = SubjectCuit,
        CbteDesde = number,
        CbteHasta = number,
        CbteFch = "20261006",
        ImpTotal = 1210.50,
        ImpTotConc = 0,
        ImpNeto = 1000,
        ImpOpEx = 0,
        ImpTrib = 0,
        ImpIva = 210.50,
        FchServDesde = number == 1001 ? "20261001" : "20261101",
        FchServHasta = number == 1001 ? "20261031" : "20261130",
        FchVtoPago = number == 1001 ? "20261110" : "20261210",
        MonId = "DOL",
        MonCotiz = 1350.25,
        CanMisMonExt = mixedForeignCurrency,
        CondicionIvaReceptorId = ivaCondition
    };

    private static FecaeaDetRequest CreateCaeaDetail(long number, int ivaCondition) => new()
    {
        Concepto = 1,
        DocTipo = 80,
        DocNro = SubjectCuit,
        CbteDesde = number,
        CbteHasta = number,
        CbteFch = "20261006",
        ImpTotal = 100,
        ImpTotConc = 0,
        ImpNeto = 100,
        ImpOpEx = 0,
        ImpTrib = 0,
        ImpIva = 0,
        FchServDesde = number == 2001 ? "20261001" : "20261101",
        FchServHasta = number == 2001 ? "20261031" : "20261130",
        FchVtoPago = number == 2001 ? "20261110" : "20261210",
        MonId = "PES",
        MonCotiz = 1,
        CanMisMonExt = number == 2001 ? "S" : "N",
        CondicionIvaReceptorId = ivaCondition,
        Caea = number == 2001 ? "20261000001234" : "20261000005678",
        CbteFchHsGen = number == 2001 ? "20261006120000" : "20261106130000"
    };

    private static XElement Operation(XDocument envelope, string name) => envelope
        .Descendants(XName.Get("Body", SoapNamespace)).Single()
        .Elements(XName.Get(name, WsfeNamespace)).Single();

    private static string CaeResponse() => $"""
        <soap:Envelope xmlns:soap="{SoapNamespace}" xmlns:ws="{WsfeNamespace}"><soap:Body>
          <ws:FECAESolicitarResponse><ws:FECAESolicitarResult>
            <ws:FeCabResp><ws:Cuit>{TenantCuit}</ws:Cuit><ws:PtoVta>12</ws:PtoVta><ws:CbteTipo>1</ws:CbteTipo><ws:FchProceso>20261006120000</ws:FchProceso><ws:CantReg>2</ws:CantReg><ws:Resultado>A</ws:Resultado><ws:Reproceso>N</ws:Reproceso></ws:FeCabResp>
            <ws:FeDetResp><ws:FECAEDetResponse><ws:Concepto>2</ws:Concepto><ws:DocTipo>80</ws:DocTipo><ws:DocNro>{SubjectCuit}</ws:DocNro><ws:CbteDesde>1001</ws:CbteDesde><ws:CbteHasta>1001</ws:CbteHasta><ws:CbteFch>20261006</ws:CbteFch><ws:Resultado>A</ws:Resultado><ws:CAE>74123456789012</ws:CAE><ws:CAEFchVto>20261016</ws:CAEFchVto></ws:FECAEDetResponse><ws:FECAEDetResponse><ws:Concepto>2</ws:Concepto><ws:DocTipo>80</ws:DocTipo><ws:DocNro>{SubjectCuit}</ws:DocNro><ws:CbteDesde>1002</ws:CbteDesde><ws:CbteHasta>1002</ws:CbteHasta><ws:CbteFch>20261006</ws:CbteFch><ws:Resultado>A</ws:Resultado><ws:CAE>74123456789013</ws:CAE><ws:CAEFchVto>20261016</ws:CAEFchVto></ws:FECAEDetResponse></ws:FeDetResp>
          </ws:FECAESolicitarResult></ws:FECAESolicitarResponse>
        </soap:Body></soap:Envelope>
        """;

    private static string CaeaResponse() => $"""
        <soap:Envelope xmlns:soap="{SoapNamespace}" xmlns:ws="{WsfeNamespace}"><soap:Body>
          <ws:FECAEARegInformativoResponse><ws:FECAEARegInformativoResult>
            <ws:FeCabResp><ws:Cuit>{TenantCuit}</ws:Cuit><ws:PtoVta>12</ws:PtoVta><ws:CbteTipo>1</ws:CbteTipo><ws:FchProceso>20261006120000</ws:FchProceso><ws:CantReg>2</ws:CantReg><ws:Resultado>A</ws:Resultado><ws:Reproceso>N</ws:Reproceso></ws:FeCabResp>
            <ws:FeDetResp><ws:FECAEADetResponse><ws:Concepto>1</ws:Concepto><ws:DocTipo>80</ws:DocTipo><ws:DocNro>{SubjectCuit}</ws:DocNro><ws:CbteDesde>2001</ws:CbteDesde><ws:CbteHasta>2001</ws:CbteHasta><ws:CbteFch>20261006</ws:CbteFch><ws:Resultado>A</ws:Resultado><ws:CAEA>20261000001234</ws:CAEA></ws:FECAEADetResponse><ws:FECAEADetResponse><ws:Concepto>1</ws:Concepto><ws:DocTipo>80</ws:DocTipo><ws:DocNro>{SubjectCuit}</ws:DocNro><ws:CbteDesde>2002</ws:CbteDesde><ws:CbteHasta>2002</ws:CbteHasta><ws:CbteFch>20261006</ws:CbteFch><ws:Resultado>A</ws:Resultado><ws:CAEA>20261000005678</ws:CAEA></ws:FECAEADetResponse></ws:FeDetResp>
          </ws:FECAEARegInformativoResult></ws:FECAEARegInformativoResponse>
        </soap:Body></soap:Envelope>
        """;

    private static string PadronPopulatedResponse() => $"""
        <soap:Envelope xmlns:soap="{SoapNamespace}" xmlns:ws="{PadronNamespace}" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"><soap:Body>
          <ws:getPersonaResponse><personaReturn><datosRegimenGeneral>
            <impuesto><descripcionImpuesto>IVA</descripcionImpuesto><estadoImpuesto>ACTIVO</estadoImpuesto><idImpuesto>30</idImpuesto><motivo>Alta de oficio</motivo><periodo>202401</periodo></impuesto>
            <impuesto><descripcionImpuesto>Ganancias</descripcionImpuesto><estadoImpuesto>NO_INSCRIPTO</estadoImpuesto><idImpuesto>10</idImpuesto><motivo>Sin actividad gravada</motivo><periodo>202401</periodo></impuesto>
            <impuesto xsi:nil="true" />
          </datosRegimenGeneral></personaReturn></ws:getPersonaResponse>
        </soap:Body></soap:Envelope>
        """;

    private static string PadronErrorResponse() => $"""
        <soap:Envelope xmlns:soap="{SoapNamespace}" xmlns:ws="{PadronNamespace}"><soap:Body>
          <ws:getPersonaResponse><personaReturn>
            <errorConstancia><error>No se encontró la persona solicitada</error></errorConstancia>
            <errorRegimenGeneral><error>Consulta tributaria no disponible</error><mensaje>Reintente más tarde</mensaje></errorRegimenGeneral>
          </personaReturn></ws:getPersonaResponse>
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
        return new ArcaTenantContext(tenantId, TenantCuit, ArcaEnvironment.Homologation, certificateContent);
    }
}
