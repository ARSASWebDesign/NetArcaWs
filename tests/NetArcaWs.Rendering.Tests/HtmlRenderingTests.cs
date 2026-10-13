using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Net;
using StbImageSharp;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.Documents;
using NetArcaWs.Rendering.Html;
using QRCoder;
using ZXing;
using Xunit;

namespace NetArcaWs.Rendering.Tests;

public sealed class HtmlRenderingTests
{
    private static DatosEmisor Issuer(byte[]? logo = null) => new(30123456789, "Emisor <&>", "Comercial", "Calle 1",
        "Responsable Inscripto", logo, "123-456", new DateOnly(2010, 1, 2));

    private static ComprobanteGenerado Receipt() => new(1, TipoComprobanteGenerado.ReciboX, null, null, "Local",
        new DatosReceptor(20304050607, "Cliente <&>", "Calle 2", "Consumidor Final"), "ARS", 125m,
        [new LineaComprobante("Servicio <b>", 125m, "ARS")], [], [], null, null, null,
        numeroInterno: "X-1", fecha: new DateOnly(2026, 10, 13), pagos: [new PagoReciboX("Efectivo", 125m, "ARS")]);

    private const string LeyendaMonotributo = "El crédito fiscal discriminado en el presente comprobante, solo podrá ser computado a efectos del Régimen de Sostenimiento e Inclusión Fiscal para Pequeños Contribuyentes de la Ley Nº 27.618";

    private static ComprobanteGenerado Fiscal(string servicio = "wsfe", int tipo = 1, string state = "Authorized",
        int? condition = 6, string conditionText = "Monotributista", IReadOnlyList<string>? legends = null) =>
        new(1, TipoComprobanteGenerado.Fiscal, new(servicio, "Homologacion", 30123456789, 4, tipo, 123), "tenant",
            state, new DatosReceptor(20304050607, "Cliente", "Calle 2", conditionText, 80), "ARS", 121m,
            [], [], legends ?? [], WsfeRequestSnapshot(tipo, condition ?? 6), 1,
            new ResultadoComprobante("12345678901234", new DateOnly(2026, 10, 23), [], [], "A", "E"), fecha: new DateOnly(2026, 10, 13), cotizacion: 1,
            snapshotResponse: WsfeResponseSnapshot(tipo), snapshotResponseVersion: 1, neto: 121, exento: 0, noGravado: 0, iva: 0,
            codigoCondicionIvaReceptor: condition);

    [Fact]
    public void X_receipt_renders_required_legend_and_escapes_text_without_qr()
    {
        string html = new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(Receipt());
        Assert.Contains("Recibo X", html);
        Assert.Contains("DOCUMENTO NO VALIDO COMO FACTURA", html);
        Assert.Contains("Emisor &lt;&amp;&gt;", html);
        Assert.Contains("Cliente &lt;&amp;&gt;", html);
        Assert.Contains("Servicio &lt;b&gt;", html);
        Assert.DoesNotContain("<img class=\"qr\"", html);
    }

    [Fact]
    public void X_receipt_embeds_logo_when_supplied()
    {
        using var qrData = QRCodeGenerator.GenerateQrCode("logo", QRCodeGenerator.ECCLevel.M);
        byte[] logo = new PngByteQRCode(qrData).GetGraphic(2);
        string html = new ArcaWsGeneradorHTML(Issuer(logo)).GenerarHTMLComprobante(Receipt());
        Assert.Contains("data:image/png;base64,", html);
    }

    [Fact]
    public void Fiscal_receipt_uses_qr_url_and_revalidates_correlated_data()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            string html = new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(Fiscal());
            if (Environment.GetEnvironmentVariable("NETARCAWS_RENDER_SAMPLE_PATH") is { Length: > 0 } samplePath)
                File.WriteAllText(samplePath, html);
            Assert.Contains("Factura A", html);
            Assert.Contains("Monotributista", html);
            Assert.Contains("27.618", html);
            Assert.Contains("121,00 pesos", html);
            Assert.Contains("https://www.arca.gob.ar/fe/qr/", html);
            string payload = ExtractQrPayload(html);
            using JsonDocument json = JsonDocument.Parse(Convert.FromBase64String(payload));
            Assert.Equal("2026-10-13", json.RootElement.GetProperty("fecha").GetString());
            Assert.Equal(12345678901234L, json.RootElement.GetProperty("codAut").GetInt64());
            Assert.Equal(121m, json.RootElement.GetProperty("importe").GetDecimal());
            Assert.Equal("ARS", json.RootElement.GetProperty("moneda").GetString());
            Assert.Equal("E", json.RootElement.GetProperty("tipoCodAut").GetString());
            string qrUrl = "https://www.arca.gob.ar/fe/qr/?p=" + payload;
            Assert.Equal(qrUrl, DecodeRenderedQr(html));
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public void Required_fiscal_legends_are_added_from_correlated_type_and_receiver_condition()
    {
        string monotributo = new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(Fiscal(legends: []));
        Assert.Contains(LeyendaMonotributo, WebUtility.HtmlDecode(monotributo));

        string consumidorFinal = new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(
            Fiscal(tipo: 6, condition: 5, conditionText: "Consumidor final", legends: []));
        Assert.Contains("A CONSUMIDOR FINAL", consumidorFinal);
        Assert.Contains("Régimen de Transparencia Fiscal al Consumidor (Ley 27.743)", consumidorFinal);
        Assert.Contains("Otros impuestos nacionales indirectos", consumidorFinal);
        Assert.Contains("0,00 pesos", consumidorFinal);
    }

    [Fact]
    public void Fiscal_qr_accepts_trailing_zero_scales_but_rejects_excess_precision()
    {
        ComprobanteGenerado valid = Fiscal();
        var scaled = new ComprobanteGenerado(valid.Version, valid.Tipo, valid.Identidad, valid.TenantId, valid.Estado,
            valid.Receptor, valid.Moneda, 121.0000m, valid.Detalles, valid.Impuestos, valid.Leyendas,
            valid.SnapshotRequest, valid.SnapshotVersion, valid.Resultado, fecha: valid.Fecha,
            cotizacion: 1.00000000m, neto: valid.Neto, exento: valid.Exento, noGravado: valid.NoGravado,
            iva: valid.Iva, snapshotResponse: valid.SnapshotResponse, snapshotResponseVersion: valid.SnapshotResponseVersion,
            codigoCondicionIvaReceptor: valid.CodigoCondicionIvaReceptor);
        string html = new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(scaled);
        Assert.Contains("121.00", JsonDocument.Parse(Convert.FromBase64String(ExtractQrPayload(html))).RootElement.GetRawText());

        var excess = new ComprobanteGenerado(valid.Version, valid.Tipo, valid.Identidad, valid.TenantId, valid.Estado,
            valid.Receptor, valid.Moneda, 121.001m, valid.Detalles, valid.Impuestos, valid.Leyendas,
            valid.SnapshotRequest, valid.SnapshotVersion, valid.Resultado, fecha: valid.Fecha,
            cotizacion: valid.Cotizacion, neto: valid.Neto, exento: valid.Exento, noGravado: valid.NoGravado,
            iva: valid.Iva, snapshotResponse: valid.SnapshotResponse, snapshotResponseVersion: valid.SnapshotResponseVersion,
            codigoCondicionIvaReceptor: valid.CodigoCondicionIvaReceptor);
        Assert.Throws<InvalidOperationException>(() => new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(excess));
    }

    [Fact]
    public void Fiscal_render_rejects_wrong_issuer_or_incomplete_result()
    {
        var otherIssuer = new DatosEmisor(30987654321, "Otro", null, "Calle", "RI");
        Assert.Throws<ArgumentException>(() => new ArcaWsGeneradorHTML(otherIssuer).GenerarHTMLComprobante(Fiscal()));
        ComprobanteGenerado incomplete = Fiscal();
        incomplete = new ComprobanteGenerado(1, incomplete.Tipo, incomplete.Identidad, incomplete.TenantId, incomplete.Estado,
            incomplete.Receptor, incomplete.Moneda, incomplete.Total, incomplete.Detalles, incomplete.Impuestos,
            incomplete.Leyendas, incomplete.SnapshotRequest, incomplete.SnapshotVersion, null,
            fecha: incomplete.Fecha, cotizacion: incomplete.Cotizacion, snapshotResponse: incomplete.SnapshotResponse,
            snapshotResponseVersion: incomplete.SnapshotResponseVersion, codigoCondicionIvaReceptor: incomplete.CodigoCondicionIvaReceptor);
        Assert.Throws<InvalidOperationException>(() => new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(incomplete));
    }

    [Fact]
    public void Fiscal_render_rejects_mismatched_request_response_and_auth_material()
    {
        ComprobanteGenerado valid = Fiscal();
        Assert.Throws<InvalidOperationException>(() => new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(
            Copy(valid, request: valid.SnapshotRequest!.Replace("<ImpTotal>121</ImpTotal>", "<ImpTotal>122</ImpTotal>", StringComparison.Ordinal))));
        Assert.Throws<InvalidOperationException>(() => new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(
            Copy(valid, response: valid.SnapshotResponse!.Replace("<Resultado>A</Resultado>", "<Resultado>R</Resultado>", StringComparison.Ordinal))));
        XDocument authDocument = XDocument.Parse(valid.SnapshotRequest!);
        authDocument.Root!.Add(new XElement("Auth", new XElement("Token", "test-only")));
        string withAuth = authDocument.ToString(SaveOptions.DisableFormatting);
        Assert.Throws<InvalidOperationException>(() => new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(Copy(valid, request: withAuth)));
    }

    [Fact]
    public void Fiscal_render_rejects_missing_request_fields_and_wrong_response_qname()
    {
        ComprobanteGenerado valid = Fiscal();
        XDocument missingReceiver = XDocument.Parse(valid.SnapshotRequest!);
        missingReceiver.Descendants().First(element => element.Name.LocalName == "DocNro").Remove();
        Assert.Throws<InvalidOperationException>(() => new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(
            Copy(valid, request: missingReceiver.ToString(SaveOptions.DisableFormatting))));

        XDocument missingTotalComponent = XDocument.Parse(valid.SnapshotRequest!);
        missingTotalComponent.Descendants().First(element => element.Name.LocalName == "ImpNeto").Remove();
        Assert.Throws<InvalidOperationException>(() => new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(
            Copy(valid, request: missingTotalComponent.ToString(SaveOptions.DisableFormatting))));

        string wrongRequestRoot = valid.SnapshotRequest!.Replace("http://ar.gov.afip.dif.FEV1/", "urn:wrong-contract", StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(
            Copy(valid, request: wrongRequestRoot)));

        string wrongResponseRoot = valid.SnapshotResponse!.Replace("http://ar.gov.afip.dif.FEV1/", "urn:wrong-contract", StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(
            Copy(valid, response: wrongResponseRoot)));
    }

    [Fact]
    public void Fiscal_renderer_accepts_real_serialized_authorize_and_query_shapes_for_all_services()
    {
        foreach (string service in new[] { "wsfe", "wsfex", "wsmtxca" })
        foreach (bool query in new[] { false, true })
        {
            ComprobanteGenerado document = ContractFiscal(service, query);
            string html = new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(document);
            Assert.Contains("Comprobante autorizado por ARCA", html);
            using JsonDocument qr = JsonDocument.Parse(Convert.FromBase64String(ExtractQrPayload(html)));
            Assert.Equal("E", qr.RootElement.GetProperty("tipoCodAut").GetString());
        }
    }

    [Fact]
    public void Wsfex_displays_foreign_receiver_text_as_complementary_information()
    {
        string html = new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(ContractFiscal("wsfex", query: false));
        int start = html.IndexOf("<section class=\"receiver\">", StringComparison.Ordinal);
        int end = html.IndexOf("</section>", start, StringComparison.Ordinal);
        string receiverSection = WebUtility.HtmlDecode(html[start..(end + "</section>".Length)]);
        Assert.True(receiverSection.Contains("Información complementaria:", StringComparison.Ordinal), receiverSection);
        Assert.DoesNotContain("Condición frente al IVA:", receiverSection);
    }

    [Fact]
    public void MtXca_consumer_final_allows_contract_omission_of_document_type_and_number()
    {
        ComprobanteGenerado basis = Fiscal("wsmtxca", 6, condition: 5, conditionText: "Consumidor final");
        var receiver = new DatosReceptor(0, "Consumidor final", "Calle 2", "Consumidor final");
        var request = new ComprobanteType
        {
            CodigoTipoComprobante = 6, NumeroPuntoVenta = 4, NumeroComprobante = 123,
            FechaEmision = new DateTime(2026, 10, 13), ImporteTotal = 121, CodigoMoneda = "ARS", CotizacionMoneda = 1,
            CondicionIvaReceptor = 5, ImporteGravado = 121, ImporteExento = 0, ImporteNoGravado = 0
        };
        string html = RenderWsmtxca(basis, receiver, request, query: false);
        Assert.Contains("A CONSUMIDOR FINAL", html);
    }

    [Fact]
    public void Wsfex_qr_uses_correlated_twenty_digit_foreign_tax_id_and_ignores_unrelated_foreign_id_for_domestic_services()
    {
        const string foreignTaxId = "12345678901234567890";
        ComprobanteGenerado foreign = ContractFiscal("wsfex", query: false);
        string foreignHtml = new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(foreign);
        using JsonDocument foreignQr = JsonDocument.Parse(Convert.FromBase64String(ExtractQrPayload(foreignHtml)));
        Assert.Equal(decimal.Parse(foreignTaxId, CultureInfo.InvariantCulture), foreignQr.RootElement.GetProperty("nroDocRec").GetDecimal());

        const string oversizedForeignId = "123456789012345678901";
        ComprobanteGenerado oversized = ContractFiscal("wsfex", query: false);
        string oversizedRequest = oversized.SnapshotRequest!.Replace(foreignTaxId, oversizedForeignId, StringComparison.Ordinal);
        DatosReceptor oversizedReceiver = oversized.Receptor with { IdentificacionFiscal = oversizedForeignId };
        var oversizedDocument = new ComprobanteGenerado(oversized.Version, oversized.Tipo, oversized.Identidad, oversized.TenantId,
            oversized.Estado, oversizedReceiver, oversized.Moneda, oversized.Total, oversized.Detalles, oversized.Impuestos, oversized.Leyendas,
            oversizedRequest, oversized.SnapshotVersion, oversized.Resultado, fecha: oversized.Fecha, cotizacion: oversized.Cotizacion,
            neto: oversized.Neto, exento: oversized.Exento, noGravado: oversized.NoGravado, iva: oversized.Iva,
            snapshotResponse: oversized.SnapshotResponse, snapshotResponseVersion: oversized.SnapshotResponseVersion);
        Assert.Throws<InvalidOperationException>(() => new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(oversizedDocument));

        ComprobanteGenerado domestic = Fiscal();
        DatosReceptor tamperedReceiver = new(domestic.Receptor.Identificacion, domestic.Receptor.Nombre, domestic.Receptor.Domicilio,
            domestic.Receptor.CondicionTributaria, domestic.Receptor.CodigoTipoDocumento, foreignTaxId);
        ComprobanteGenerado tampered = new(domestic.Version, domestic.Tipo, domestic.Identidad, domestic.TenantId, domestic.Estado,
            tamperedReceiver, domestic.Moneda, domestic.Total, domestic.Detalles, domestic.Impuestos, domestic.Leyendas,
            domestic.SnapshotRequest, domestic.SnapshotVersion, domestic.Resultado, fecha: domestic.Fecha,
            cotizacion: domestic.Cotizacion, neto: domestic.Neto, exento: domestic.Exento, noGravado: domestic.NoGravado,
            iva: domestic.Iva, snapshotResponse: domestic.SnapshotResponse, snapshotResponseVersion: domestic.SnapshotResponseVersion,
            codigoCondicionIvaReceptor: domestic.CodigoCondicionIvaReceptor);
        string domesticHtml = new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(tampered);
        using JsonDocument domesticQr = JsonDocument.Parse(Convert.FromBase64String(ExtractQrPayload(domesticHtml)));
        Assert.Equal(20304050607m, domesticQr.RootElement.GetProperty("nroDocRec").GetDecimal());
    }

    [Fact]
    public async Task Two_issuers_render_concurrently_without_cross_contamination()
    {
        var issuerA = Issuer();
        var issuerB = new DatosEmisor(30987654321, "Segundo emisor", null, "Otra calle", "Responsable Inscripto", ingresosBrutos: "Exento", inicioActividades: new DateOnly(2020, 2, 3));
        ComprobanteGenerado docA = Fiscal();
        ComprobanteGenerado docB = Copy(docA, identity: docA.Identidad! with { Cuit = issuerB.Identificacion },
            response: docA.SnapshotResponse!.Replace("30123456789", issuerB.Identificacion.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
        Task<string> renderA = Task.Run(() => new ArcaWsGeneradorHTML(issuerA).GenerarHTMLComprobante(docA));
        Task<string> renderB = Task.Run(() => new ArcaWsGeneradorHTML(issuerB).GenerarHTMLComprobante(docB));
        string[] html = await Task.WhenAll(renderA, renderB);
        Assert.Contains("Emisor &lt;&amp;&gt;", html[0]);
        Assert.DoesNotContain("Segundo emisor", html[0]);
        Assert.Contains("Segundo emisor", html[1]);
        Assert.DoesNotContain("Emisor &lt;&amp;&gt;", html[1]);
    }

    private static ComprobanteGenerado Copy(ComprobanteGenerado source, IdentidadComprobante? identity = null,
        string? request = null, string? response = null) => new(1, source.Tipo, identity ?? source.Identidad, source.TenantId,
        source.Estado, source.Receptor, source.Moneda, source.Total, source.Detalles, source.Impuestos, source.Leyendas,
        request ?? source.SnapshotRequest, source.SnapshotVersion, source.Resultado, numeroInterno: source.NumeroInterno,
        fecha: source.Fecha, pagos: source.Pagos, imputaciones: source.Imputaciones, cotizacion: source.Cotizacion,
        neto: source.Neto, exento: source.Exento, noGravado: source.NoGravado, iva: source.Iva,
        fechaDesde: source.FechaDesde, fechaHasta: source.FechaHasta, fechaPago: source.FechaPago,
        snapshotResponse: response ?? source.SnapshotResponse, snapshotResponseVersion: source.SnapshotResponseVersion,
        condicionIvaReceptor: source.CondicionIvaReceptor, codigoCondicionIvaReceptor: source.CodigoCondicionIvaReceptor);

    private static string ExtractQrPayload(string html)
    {
        int start = html.IndexOf("https://www.arca.gob.ar/fe/qr/?p=", StringComparison.Ordinal);
        Assert.True(start >= 0);
        start += "https://www.arca.gob.ar/fe/qr/?p=".Length;
        int end = html.IndexOf('"', start);
        return html[start..end];
    }

    private static string WsfeRequestSnapshot(int type = 1, int condition = 6)
    {
        var request = new FecaeRequest { FeCabReq = new FecaeCabRequest { CantReg = 1, PtoVta = 4, CbteTipo = type } };
        request.FeDetReq.Add(new FecaeDetRequest
        {
            Concepto = 1, DocTipo = 80, DocNro = 20304050607, CbteDesde = 123, CbteHasta = 123,
            CbteFch = "20261013", ImpTotal = 121, ImpTotConc = 0, ImpNeto = 121, ImpOpEx = 0,
            ImpTrib = 0, ImpIva = 0, MonId = "ARS", MonCotiz = 1, CondicionIvaReceptorId = condition
        });
        return Serialize(request);
    }

    private static ComprobanteGenerado ContractFiscal(string service, bool query)
    {
        const long cuit = 30123456789;
        const string cae = "12345678901234";
        const string foreignId = "12345678901234567890";
        string requestXml;
        string responseXml;
        int type = service == "wsfex" ? 19 : service == "wsmtxca" ? 6 : 1;
        DatosReceptor receiver;
        decimal net = 121m;
        int condition = 6;
        if (service == "wsfe")
        {
            requestXml = WsfeRequestSnapshot(type, condition);
            receiver = new(20304050607, "Cliente", "Calle 2", "Monotributista", 80);
            if (query)
            {
                var result = new FeCompConsResponse
                {
                    Resultado = "A", EmisionTipo = "CAE", CodAutorizacion = cae, FchVto = "20261023",
                    PtoVta = 4, CbteTipo = type, CbteDesde = 123, DocTipo = 80, DocNro = receiver.Identificacion
                };
                responseXml = Serialize(new FeCompConsultarResponse { FeCompConsultarResult = new FeCompConsultaResponse { ResultGet = result } });
            }
            else responseXml = WsfeResponseSnapshot(type);
        }
        else if (service == "wsfex")
        {
            var request = new ClsFexRequest
            {
                Id = 42, FechaCbte = "20261013", CbteTipo = 19, PuntoVta = 4, CbteNro = 123,
                TipoExpo = 1, DstCmp = 840, Cliente = "Cliente exterior", CuitPaisCliente = 840,
                DomicilioCliente = "Calle 2", IdImpositivo = foreignId, MonedaId = "ARS", MonedaCtz = 1, ImpTotal = 121
            };
            requestXml = Serialize(request);
            receiver = new(840, "Cliente exterior", "Calle 2", "Cliente exterior", 80, foreignId);
            if (query)
            {
                var result = new ClsFexGetCmpr
                {
                    Resultado = "A", Cae = cae, FchVencCae = "20261023", CbteTipo = 19, PuntoVta = 4,
                    CbteNro = 123, CuitPaisCliente = 840, IdImpositivo = foreignId
                };
                responseXml = Serialize(new FexGetCmpResponse2 { FexGetCmpResult = new FexGetCmpResponse { FexResultGet = result } });
            }
            else
            {
                var result = new ClsFexOutAuthorize
                {
                    Id = 42, Cuit = cuit, CbteTipo = 19, PuntoVta = 4, CbteNro = 123,
                    Cae = cae, FchVencCae = "20261023", FchCbte = "20261013", Resultado = "A"
                };
                responseXml = Serialize(new FexAuthorizeResponse { FexAuthorizeResult = new FexResponseAuthorize { FexResultAuth = result } });
            }
        }
        else
        {
            type = 6;
            condition = 5;
            var request = new ComprobanteType
            {
                CodigoTipoComprobante = 6, NumeroPuntoVenta = 4, NumeroComprobante = 123,
                FechaEmision = new DateTime(2026, 10, 13), ImporteTotal = 121, CodigoMoneda = "ARS", CotizacionMoneda = 1,
                CodigoTipoDocumento = 80, NumeroDocumento = 20304050607, CondicionIvaReceptor = 5,
                ImporteGravado = 121, ImporteExento = 0, ImporteNoGravado = 0
            };
            requestXml = Serialize(request);
            receiver = new(20304050607, "Consumidor final", "Calle 2", "Consumidor final", 80);
            if (query)
            {
                request.CodigoTipoAutorizacion = CodigoTipoAutorizacionSimpleType.E;
                request.CodigoAutorizacion = long.Parse(cae, CultureInfo.InvariantCulture);
                request.FechaVencimiento = new DateTime(2026, 10, 23);
                responseXml = Serialize(new ConsultarComprobanteResponseType { Comprobante = request });
            }
            else
            {
                var result = new ComprobanteCaeResponseType
                {
                    Cuit = cuit, CodigoTipoComprobante = 6, NumeroPuntoVenta = 4, NumeroComprobante = 123,
                    FechaEmision = new DateTime(2026, 10, 13), Cae = long.Parse(cae, CultureInfo.InvariantCulture),
                    FechaVencimientoCae = new DateTime(2026, 10, 23)
                };
                responseXml = Serialize(new AutorizarComprobanteResponseType { Resultado = ResultadoSimpleType.A, ComprobanteResponse = result });
            }
        }
        return new(1, TipoComprobanteGenerado.Fiscal, new(service, "Homologacion", cuit, 4, type, 123), "tenant",
            "Authorized", receiver, "ARS", 121, [], [], [], requestXml, 1,
            new ResultadoComprobante(cae, new DateOnly(2026, 10, 23), [], [], "A", "E"),
            fecha: new DateOnly(2026, 10, 13), cotizacion: 1, snapshotResponse: responseXml,
            snapshotResponseVersion: 1, neto: net, exento: 0, noGravado: 0, iva: 0,
            codigoCondicionIvaReceptor: service == "wsfex" ? null : condition);
    }

    private static string RenderWsmtxca(ComprobanteGenerado basis, DatosReceptor receiver, ComprobanteType request, bool query)
    {
        const string cae = "12345678901234";
        string response;
        if (query)
        {
            request.CodigoTipoAutorizacion = CodigoTipoAutorizacionSimpleType.E;
            request.CodigoAutorizacion = long.Parse(cae, CultureInfo.InvariantCulture);
            request.FechaVencimiento = new DateTime(2026, 10, 23);
            response = Serialize(new ConsultarComprobanteResponseType { Comprobante = request });
        }
        else
        {
            response = Serialize(new AutorizarComprobanteResponseType
            {
                Resultado = ResultadoSimpleType.A,
                ComprobanteResponse = new ComprobanteCaeResponseType
                {
                    Cuit = 30123456789, CodigoTipoComprobante = 6, NumeroPuntoVenta = 4, NumeroComprobante = 123,
                    FechaEmision = new DateTime(2026, 10, 13), Cae = long.Parse(cae, CultureInfo.InvariantCulture), FechaVencimientoCae = new DateTime(2026, 10, 23)
                }
            });
        }
        var doc = new ComprobanteGenerado(basis.Version, basis.Tipo, basis.Identidad, basis.TenantId, basis.Estado,
            receiver, basis.Moneda, basis.Total, basis.Detalles, basis.Impuestos, basis.Leyendas, Serialize(request), 1,
            basis.Resultado, fecha: basis.Fecha, cotizacion: basis.Cotizacion, neto: basis.Neto, exento: basis.Exento,
            noGravado: basis.NoGravado, iva: basis.Iva, snapshotResponse: response, snapshotResponseVersion: 1,
            codigoCondicionIvaReceptor: basis.CodigoCondicionIvaReceptor);
        return new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(doc);
    }

    private static string WsfeResponseSnapshot(int type = 1)
    {
        var result = new FecaeResponse { FeCabResp = new FecaeCabResponse { CantReg = 1, Cuit = 30123456789, PtoVta = 4, CbteTipo = type, Resultado = "A" } };
        result.FeDetResp.Add(new FecaeDetResponse
        {
            Concepto = 1, DocTipo = 80, DocNro = 20304050607, CbteDesde = 123, CbteHasta = 123,
            CbteFch = "20261013", Resultado = "A", Cae = "12345678901234", CaeFchVto = "20261023"
        });
        return Serialize(new FecaeSolicitarResponse { FecaeSolicitarResult = result });
    }

    private static string Serialize<T>(T value)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        serializer.Serialize(writer, value);
        return writer.ToString();
    }

    private static string DecodeRenderedQr(string html)
    {
        int imageStart = html.IndexOf("data:image/png;base64,", StringComparison.Ordinal);
        Assert.True(imageStart >= 0);
        imageStart += "data:image/png;base64,".Length;
        int imageEnd = html.IndexOf('"', imageStart);
        byte[] png = Convert.FromBase64String(html[imageStart..imageEnd]);
        ImageResult image = ImageResult.FromMemory(png, ColorComponents.RedGreenBlueAlpha);
        var source = new RGBLuminanceSource(image.Data, image.Width, image.Height, RGBLuminanceSource.BitmapFormat.RGB32);
        return new BarcodeReaderGeneric().Decode(source)?.Text ?? throw new InvalidOperationException("The independent QR decoder could not decode the fiscal sample.");
    }
}
