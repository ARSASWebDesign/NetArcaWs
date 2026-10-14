using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.Documents;
using NetArcaWs.Rendering.Html;
using NetArcaWs.Rendering.Pdf;
namespace NetArcaWs.Rendering.Examples;
public sealed record SampleCase(string Id, string Group, string Description, ComprobanteGenerado? Document, DatosEmisor Issuer, RemitoGenerado? Remito = null);
public static class SampleGallery
{
    private const string SyntheticNotice = "MUESTRA SINTETICA - SIN VALIDEZ FISCAL";
    private const long IssuerId = 30123456789;
    private const string Cae = "12345678901234";
    private static readonly DateOnly IssueDate = new(2026, 10, 13), ExpiryDate = new(2026, 10, 23);
    private static readonly DatosEmisor LegalIssuer = new(IssuerId, "Comercial Ejemplo SRL", null, "Av. Ejemplo 123, Córdoba", "Responsable Inscripto", ingresosBrutos: "30-12345678-9", inicioActividades: new(2018, 4, 1));
    private static readonly DatosEmisor BrandedIssuer = new(IssuerId, "Comercial Ejemplo SRL", "La Tienda", "Av. Ejemplo 123, Córdoba", "Responsable Inscripto", SimpleLogoPng.Create(), ingresosBrutos: "30-12345678-9", inicioActividades: new(2018, 4, 1));
    private static readonly DatosEmisor BrandedMonoIssuer = new(IssuerId, "Comercial Ejemplo SRL", "La Tienda", "Av. Ejemplo 123, Córdoba", "Monotributista", SimpleLogoPng.Create(), ingresosBrutos: "30-12345678-9", inicioActividades: new(2018, 4, 1));
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static IReadOnlyList<SampleCase> CreateCases()
    {
        DatosReceptor ri = new(20304050607, "Distribuidora Horizonte SA", "San Martín 456, Córdoba", "Responsable Inscripto", 80,
            Telefono: "+54 351 555-0100", Email: "cliente@example.test");
        DatosReceptor mono = new(27234567891, "Taller del Centro", "Belgrano 222, Córdoba", "Monotributista", 80);
        DatosReceptor final = new(0, "Consumidor Final", null, null, 99, Telefono: "+54 11 5555-0100", Email: "consumidor@example.test");
        DatosReceptor foreign = new(55000002126, "Norte Trading LLC", "21 Market St, Miami, FL", "Cliente del exterior", 80, "9876543210123");
        return [
            new("wsfe-a-ri-logo", "WSFE / Factura A", "Factura A a responsable inscripto con IVA discriminado", Wsfe(1, 1, ri, 123, 100m, 21m, "Venta de mercadería"), BrandedIssuer),
            new("wsfe-a-monotributo", "WSFE / Factura A", "Factura A a monotributista con leyenda legal automática", Wsfe(1, 6, mono, 124, 100m, 21m, "Servicio de mantenimiento"), BrandedIssuer),
            new("wsfe-b-consumidor-final", "WSFE / Factura B", "Factura B a consumidor final sin identificación; IVA transparente", Wsfe(6, 5, final, 125, 100m, 21m, "Venta al consumidor final"), BrandedIssuer),
            new("wsfe-c-consumidor-final", "WSFE / Factura C", "Factura C a consumidor final sin identificación, emitida por monotributista", Wsfe(11, 5, final, 126, 121m, 0m, "Servicio a consumidor final"), BrandedMonoIssuer),
            new("wsfe-nota-credito-a", "WSFE / Nota de crédito A", "Nota de crédito A asociada a factura A 00004-00000122", Wsfe(3, 1, ri, 127, 100m, 21m, "Anulación parcial de venta", associate: true), BrandedIssuer),
            new("wsfe-nota-debito-a", "WSFE / Nota de débito A", "Nota de débito A asociada a factura A 00004-00000122", Wsfe(2, 1, ri, 128, 100m, 21m, "Diferencia de precio", associate: true), BrandedIssuer),
            new("wsfex-e-exportacion", "WSFEX / Exportación E", "Exportación E con campos fiscales y comerciales diferenciados", Wsfex(foreign, 129), BrandedIssuer),
            new("wsmtxca-a", "WSMTXCA / Factura A", "Factura A de factura con detalle e IVA", Wsmtxca(1, 1, ri, 130, 100m, 21m), BrandedIssuer),
            new("wsmtxca-b", "WSMTXCA / Factura B", "Factura B de consumidor final con IVA transparente", Wsmtxca(6, 5, final, 131, 100m, 21m), BrandedIssuer),
            new("wsmtxca-a-multialicuota", "WSMTXCA / Factura A", "Factura A con dos ítems y alícuotas de 21 % y 10,5 %", WsmtxcaMultipleRates(1, 1, ri, 137), BrandedIssuer),
            new("wsmtxca-b-multialicuota", "WSMTXCA / Factura B", "Factura B con tres ítems y alícuotas de 21 % y 10,5 %", WsmtxcaMultipleRates(6, 5, final, 138, includeThirdItem: true), BrandedIssuer),
            new("x-recibo-total", "Recibo X", "Recibo X con pago total e imputación", Receipt("X-0001", 121m, [new("Factura A 00004-00000122", 121m, "ARS")]), LegalIssuer),
            new("x-recibo-parcial", "Recibo X", "Recibo X con pago parcial e imputación", Receipt("X-0002", 50m, [new("Factura A 00004-00000122", 50m, "ARS")]), LegalIssuer),
            new("wsmtxca-a-multipagina", "WSMTXCA / Multipágina", "Factura A con detalle y leyendas extensas para revisar paginación", Multipage(ri, 132), BrandedIssuer),
            RemitoCase("remito-r-original", "Remitos R", "Remito R original con CAI declarado y datos de imprenta", RemitoR(ri, 133, true, 12), BrandedIssuer),
            RemitoCase("remito-r-duplicado", "Remitos R", "Remito R duplicado", RemitoR(ri, 134, false, 4), BrandedIssuer),
            RemitoCase("remito-x-interno", "Remitos X", "Remito X para traslado interno en el mismo predio", RemitoX(ri, 135), BrandedIssuer),
            RemitoCase("remito-r-largo", "Remitos R", "Remito R extenso con varias páginas", RemitoR(ri, 136, true, 48), BrandedIssuer)
        ];
    }

    public static string Generate(string outputRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);
        string root = Path.GetFullPath(outputRoot);
        Directory.CreateDirectory(root);
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) throw new IOException("El directorio de muestras no puede ser un enlace simbólico.");
        ExampleDocumentStore.CleanExpiredRuns(root, DateTimeOffset.UtcNow);
        DateTimeOffset created = DateTimeOffset.UtcNow;
        string run = Path.Combine(root, created.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(run);
        var html = new StringBuilder("<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Galería sintética de comprobantes</title><style>body{font:16px system-ui,sans-serif;max-width:1100px;margin:2rem auto;padding:0 1rem;color:#181818;background:#f3f3f3}h1,h2{color:#222}.group{border-top:2px solid #ccc;margin-top:2rem}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(250px,1fr));gap:1rem}.card{border:1px solid #ccc;border-radius:4px;padding:1rem;background:white}.preview{width:100%;height:160px;border:1px solid #ccc;background:#fff;pointer-events:none}.links{display:flex;gap:1rem;margin-top:1rem}</style></head><body><h1>Galería sintética de comprobantes</h1><p>Datos ficticios para revisión visual. No prueban autorización real ni homologación ARCA.</p>");
        string[] variants = ["logo-fantasia-razon-social", "sin-logo-fantasia-razon-social", "sin-logo-solo-razon-social"];
        IReadOnlyList<SampleCase> cases = CreateCases();
        foreach (SampleCase sample in cases)
        {
            html.Append("<section class=\"group\"><h2>").Append(E(sample.Group)).Append("</h2><p>").Append(E(sample.Description)).Append("</p><div class=\"grid\">");
            for (int i = 0; i < variants.Length; i++)
            {
                DatosEmisor issuer = BrandVariant(sample.Issuer, i);
                string id = sample.Id + "-" + variants[i], dir = Path.Combine(run, id), db = Path.Combine(run, id, "persistence.sqlite");
                Directory.CreateDirectory(dir);
                ExampleDocumentStore.StoredSample stored;
                if (sample.Document is not null)
                {
                    ExampleDocumentStore.Save(db, sample.Document, issuer);
                    stored = ExampleDocumentStore.LoadSample(db);
                }
                else
                {
                    RemitoGenerado remito = ReissueRemito(sample.Remito!, issuer);
                    ExampleDocumentStore.Save(db, remito, issuer);
                    stored = ExampleDocumentStore.LoadSample(db);
                }
                File.WriteAllText(Path.Combine(dir, "document.json"), stored.DocumentJson, new UTF8Encoding(false));
                DatosEmisor persistedIssuer = stored.Issuer;
                File.WriteAllText(Path.Combine(dir, "issuer.json"), JsonSerializer.Serialize(persistedIssuer, JsonOptions));
                string renderedHtml = stored.Document is not null
                    ? new ArcaWsGeneradorHTML(persistedIssuer).GenerarHTMLComprobante(stored.Document)
                    : new ArcaWsGeneradorHTML(persistedIssuer).GenerarHTMLComprobante(stored.Remito!);
                File.WriteAllText(Path.Combine(dir, "comprobante.html"), AddSyntheticNotice(renderedHtml), new UTF8Encoding(false));
                byte[] renderedPdf = stored.Document is not null
                    ? new ArcaWsGeneradorPDF(persistedIssuer).GenerarPDFComprobante(stored.Document)
                    : new ArcaWsGeneradorPDF(persistedIssuer).GenerarPDFComprobante(stored.Remito!);
                File.WriteAllBytes(Path.Combine(dir, "comprobante.pdf"), AddSyntheticNotice(renderedPdf));
                html.Append("<article class=\"card\"><h3>").Append(VariantTitle(i)).Append("</h3><p>").Append(E(sample.Description))
                    .Append("</p><p>Persistido y reabierto desde SQLite + JSON.</p><iframe class=\"preview\" loading=\"lazy\" title=\"").Append(E(sample.Id)).Append("\" src=\"").Append(E(id)).Append("/comprobante.html\"></iframe><p><a href=\"").Append(E(id))
                    .Append("/comprobante.html\">Ver HTML</a> · <a href=\"").Append(E(id)).Append("/comprobante.pdf\">Abrir PDF</a></p></article>");
            }
            html.Append("</div></section>");
        }
        SampleCase export = cases.Single(sample => sample.Id == "wsfex-e-exportacion");
        string accentId = export.Id + "-acento-azul";
        string accentDir = Path.Combine(run, accentId);
        Directory.CreateDirectory(accentDir);
        ExampleDocumentStore.Save(Path.Combine(accentDir, "persistence.sqlite"), export.Document!, export.Issuer);
        ExampleDocumentStore.StoredSample accented = ExampleDocumentStore.LoadSample(Path.Combine(accentDir, "persistence.sqlite"));
        string accentHtml = new ArcaWsGeneradorHTML(accented.Issuer, "#1A4E72").GenerarHTMLComprobante(accented.Document!);
        File.WriteAllText(Path.Combine(accentDir, "comprobante.html"), AddSyntheticNotice(accentHtml), new UTF8Encoding(false));
        byte[] accentPdf = new ArcaWsGeneradorPDF(accented.Issuer, "#1A4E72").GenerarPDFComprobante(accented.Document!);
        File.WriteAllBytes(Path.Combine(accentDir, "comprobante.pdf"), AddSyntheticNotice(accentPdf));
        html.Append("<section class=\"group\"><h2>Variante de color</h2><p>Único ejemplo con color principal personalizado; las demás muestras usan escala de grises.</p><article class=\"card\"><h3>Azul #1A4E72</h3><p>Exportación E con perfil de marca La Tienda.</p><iframe class=\"preview\" loading=\"lazy\" title=\"Exportación E azul\" src=\"").Append(accentId).Append("/comprobante.html\"></iframe><p><a href=\"").Append(accentId).Append("/comprobante.html\">Ver HTML</a> · <a href=\"").Append(accentId).Append("/comprobante.pdf\">Abrir PDF</a></p></article></section>");
        html.Append("</body></html>");
        File.WriteAllText(Path.Combine(run, "index.html"), html.ToString(), new UTF8Encoding(false));
        SampleCase logoSample = cases.Single(sample => sample.Id == "wsfe-a-ri-logo");
        DatosEmisor wideIssuer = new(logoSample.Issuer.Identificacion, logoSample.Issuer.RazonSocial, logoSample.Issuer.NombreFantasia,
            logoSample.Issuer.Domicilio, logoSample.Issuer.CondicionTributaria, SimpleLogoPng.Create(720, 240),
            logoSample.Issuer.IngresosBrutos, logoSample.Issuer.InicioActividades);
        string wideId = "wsfe-a-ri-logo-apaisado";
        string wideDir = Path.Combine(run, wideId);
        Directory.CreateDirectory(wideDir);
        ExampleDocumentStore.Save(Path.Combine(wideDir, "persistence.sqlite"), logoSample.Document!, wideIssuer);
        ExampleDocumentStore.StoredSample wideStored = ExampleDocumentStore.LoadSample(Path.Combine(wideDir, "persistence.sqlite"));
        File.WriteAllText(Path.Combine(wideDir, "document.json"), wideStored.DocumentJson, new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(wideDir, "issuer.json"), JsonSerializer.Serialize(wideStored.Issuer, JsonOptions));
        File.WriteAllText(Path.Combine(wideDir, "comprobante.html"), AddSyntheticNotice(new ArcaWsGeneradorHTML(wideStored.Issuer).GenerarHTMLComprobante(wideStored.Document!)), new UTF8Encoding(false));
        File.WriteAllBytes(Path.Combine(wideDir, "comprobante.pdf"), AddSyntheticNotice(new ArcaWsGeneradorPDF(wideStored.Issuer).GenerarPDFComprobante(wideStored.Document!)));
        html.Insert(html.ToString().IndexOf("</body>", StringComparison.Ordinal), $"<section class=\"group\"><h2>Logo apaisado</h2><article class=\"card\"><h3>Logo de proporción 3:1</h3><iframe class=\"preview\" loading=\"lazy\" title=\"Logo apaisado\" src=\"{wideId}/comprobante.html\"></iframe><p><a href=\"{wideId}/comprobante.pdf\">Abrir PDF</a></p></article></section>");
        File.WriteAllText(Path.Combine(run, "index.html"), html.ToString(), new UTF8Encoding(false));
        ExampleDocumentStore.WriteRunManifest(run, created, cases.Append(export with { Id = accentId })
            .Append(logoSample with { Id = wideId, Issuer = wideIssuer }).ToArray());
        return run;
    }

    private static DatosEmisor BrandVariant(DatosEmisor x, int i) => i switch {
        0 => new(x.Identificacion, x.RazonSocial, "La Tienda", x.Domicilio, x.CondicionTributaria, SimpleLogoPng.Create(), x.IngresosBrutos, x.InicioActividades),
        1 => new(x.Identificacion, x.RazonSocial, "La Tienda", x.Domicilio, x.CondicionTributaria, ingresosBrutos: x.IngresosBrutos, inicioActividades: x.InicioActividades),
        _ => new(x.Identificacion, x.RazonSocial, null, x.Domicilio, x.CondicionTributaria, ingresosBrutos: x.IngresosBrutos, inicioActividades: x.InicioActividades)
    };
    private static string VariantTitle(int i) => i switch { 0 => "PNG LOGO · La Tienda · razón social", 1 => "Sin logo · La Tienda · razón social", _ => "Sin logo · razón social sola" };

    private static SampleCase RemitoCase(string id, string group, string description, RemitoGenerado remito, DatosEmisor issuer) =>
        new(id, group, description, null, issuer, remito);

    private static RemitoGenerado ReissueRemito(RemitoGenerado remito, DatosEmisor issuer) =>
        new Remito(issuer).GenerarComprobante(new DatosRemito(remito.Clase, remito.FechaEmision, remito.PuntoVenta,
            remito.Numero, remito.Receptor, remito.Destino, remito.Mercaderia, remito.EsOriginal, remito.Transportista,
            remito.Cai, remito.PrimerNumero, remito.UltimoNumero, remito.Vencimiento, remito.Imprenta,
            remito.EsAutoimpresor, remito.AmbitoInterno));

    private static string AddSyntheticNotice(string html)
    {
        const string body = "<body>";
        int bodyIndex = html.IndexOf(body, StringComparison.OrdinalIgnoreCase);
        if (bodyIndex < 0) throw new InvalidDataException("El HTML renderizado no contiene el elemento body esperado.");
        int insertAt = bodyIndex + body.Length;
        const string notice = "<aside role=\"note\" style=\"box-sizing:border-box;width:100%;padding:8px 12px;margin:0 0 10px;text-align:center;font:700 12px Arial,sans-serif;letter-spacing:.04em;color:#111827;background:#f3f4f6;border:2px solid #6b7280\">MUESTRA SINTETICA - SIN VALIDEZ FISCAL</aside>";
        return html.Insert(insertAt, notice);
    }

    private static byte[] AddSyntheticNotice(byte[] pdf)
    {
        using var source = new MemoryStream(pdf);
        using PdfSharp.Pdf.PdfDocument document = PdfSharp.Pdf.IO.PdfReader.Open(source, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify);
        foreach (PdfSharp.Pdf.PdfPage page in document.Pages)
        {
            using PdfSharp.Drawing.XGraphics graphics = PdfSharp.Drawing.XGraphics.FromPdfPage(page, PdfSharp.Drawing.XGraphicsPdfPageOptions.Prepend);
            var band = new PdfSharp.Drawing.XRect(0, 0, page.Width.Point, PdfSharp.Drawing.XUnit.FromMillimeter(8).Point);
            graphics.DrawRectangle(PdfSharp.Drawing.XBrushes.Gainsboro, band);
            graphics.DrawString(SyntheticNotice, new PdfSharp.Drawing.XFont("Noto Sans", 8, PdfSharp.Drawing.XFontStyleEx.Bold),
                PdfSharp.Drawing.XBrushes.Black, band, PdfSharp.Drawing.XStringFormats.Center);
        }

        using var output = new MemoryStream();
        document.Save(output, closeStream: false);
        return output.ToArray();
    }

    private static ComprobanteGenerado Wsfe(int type, int cond, DatosReceptor receiver, long number, decimal net, decimal vat, string desc, bool associate = false)
    {
        int docType = receiver.CodigoTipoDocumento ?? 99;
        var request = new FecaeRequest { FeCabReq = new() { CantReg = 1, PtoVta = 4, CbteTipo = type } };
        var detail = new FecaeDetRequest { Concepto = 1, DocTipo = docType, DocNro = receiver.Identificacion,
            CbteDesde = number, CbteHasta = number, CbteFch = IssueDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            ImpTotal = (double)(net + vat), ImpTotConc = 0, ImpNeto = (double)net, ImpOpEx = 0, ImpTrib = 0, ImpIva = (double)vat,
            MonId = "ARS", MonCotiz = 1, CondicionIvaReceptorId = cond };
        if (vat > 0) detail.Iva.Add(new AlicIva { Id = 5, BaseImp = (double)net, Importe = (double)vat });
        if (associate) detail.CbtesAsoc.Add(new CbteAsoc { Tipo = 1, PtoVta = 4, Nro = 122, Cuit = IssuerId.ToString(CultureInfo.InvariantCulture), CbteFch = "20261012" });
        request.FeDetReq.Add(detail);
        var response = new FecaeResponse { FeCabResp = new() { CantReg = 1, Cuit = IssuerId, PtoVta = 4, CbteTipo = type, Resultado = "A" } };
        response.FeDetResp.Add(new FecaeDetResponse { Concepto = 1, DocTipo = docType, DocNro = receiver.Identificacion,
            CbteDesde = number, CbteHasta = number, CbteFch = IssueDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            Resultado = "A", Cae = Cae, CaeFchVto = ExpiryDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture) });
        return new(1, TipoComprobanteGenerado.Fiscal, new("wsfe", "Homologacion", IssuerId, 4, type, number), "muestra-sintetica",
            "Authorized", receiver, "ARS", net + vat, [],
            vat == 0 ? [] : [new("IVA 21%", vat, "ARS", "5", net, 21m, Tipo: TipoImpuestoComprobante.Iva)],
            [], Serialize(request), 1,
            new(Cae, ExpiryDate, [], [], "A", "E"), fecha: IssueDate, cotizacion: 1, neto: net, exento: 0, noGravado: 0, iva: vat,
            snapshotResponse: Serialize(new FecaeSolicitarResponse { FecaeSolicitarResult = response }), snapshotResponseVersion: 1,
            condicionIvaReceptor: receiver.CondicionTributaria, codigoCondicionIvaReceptor: cond);
    }

    private static ComprobanteGenerado Wsfex(DatosReceptor receiver, long number)
    {
        const string description = "Válvulas de acero inoxidable para proceso industrial";
        var request = new ClsFexRequest { Id = 42, FechaCbte = IssueDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            CbteTipo = 19, PuntoVta = 4, CbteNro = number, TipoExpo = 1, DstCmp = 212, Cliente = receiver.Nombre,
            CuitPaisCliente = receiver.Identificacion, DomicilioCliente = receiver.Domicilio ?? "", IdImpositivo = receiver.IdentificacionFiscal,
            MonedaId = "DOL", MonedaCtz = 1234.567891m, ImpTotal = 125m, FormaPago = "Transferencia bancaria",
            FechaPago = "20261018", Incoterms = "FOB", IncotermsDs = "Puerto de Miami",
            ObsComerciales = "Entrega parcial acordada para octubre", Obs = "Muestra sintética de exportación" };
        request.Items.Add(new Item { ProCodigo = "VAL-INOX-01", ProDs = description, ProQty = 5m, ProUmed = 7, ProPrecioUni = 25m, ProTotalItem = 125m });
        var authorized = new ClsFexOutAuthorize { Id = request.Id, Cuit = IssuerId, CbteTipo = 19, PuntoVta = 4, CbteNro = number,
            Cae = Cae, FchVencCae = ExpiryDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            FchCbte = IssueDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture), Resultado = "A" };
        return new(1, TipoComprobanteGenerado.Fiscal, new("wsfex", "Homologacion", IssuerId, 4, 19, number), "muestra-sintetica", "Authorized",
            receiver, "DOL", 125m, [new(description, 125m, "DOL", "VAL-INOX-01", 5m, 7, 25m)], [], [], Serialize(request), 1,
            new(Cae, ExpiryDate, [], [], "A", "E"), fecha: IssueDate, cotizacion: 1234.567891m, neto: 125m,
            snapshotResponse: Serialize(new FexAuthorizeResponse { FexAuthorizeResult = new FexResponseAuthorize { FexResultAuth = authorized } }), snapshotResponseVersion: 1);
    }

    private static ComprobanteGenerado Wsmtxca(short type, short cond, DatosReceptor receiver, long number, decimal net, decimal vat,
        IReadOnlyList<(string Description, decimal Net, decimal Vat)>? customItems = null, IReadOnlyList<string>? legends = null)
    {
        decimal total = net + vat;
        var request = new ComprobanteType { CodigoTipoComprobante = type, NumeroPuntoVenta = 4, NumeroComprobante = number,
            FechaEmision = IssueDate.ToDateTime(TimeOnly.MinValue), ImporteSubtotal = net, ImporteTotal = total, CodigoMoneda = "ARS", CotizacionMoneda = 1,
            CodigoTipoDocumento = checked((short)(receiver.CodigoTipoDocumento ?? 99)), NumeroDocumento = receiver.Identificacion, CondicionIvaReceptor = cond,
            ImporteGravado = net, ImporteExento = 0, ImporteNoGravado = 0 };
        IReadOnlyList<(string Description, decimal Net, decimal Vat)> items = customItems ??
            [("Juego de herramientas de mantenimiento", net, vat)];
        foreach (var line in items)
            request.ArrayItems.Add(new ItemType { Descripcion = line.Description, Codigo = "HERR-01", Cantidad = 1m,
            PrecioUnitario = cond == 5 ? line.Net + line.Vat : line.Net, CodigoCondicionIva = 1,
                ImporteIva = line.Vat, ImporteItem = line.Net + line.Vat, CodigoUnidadMedida = 7 });
        if (vat > 0) request.ArraySubtotalesIva.Add(new SubtotalIvaType { Codigo = 5, Importe = vat });
        var response = new AutorizarComprobanteResponseType { Resultado = ResultadoSimpleType.A, ComprobanteResponse = new() {
            Cuit = IssuerId, CodigoTipoComprobante = type, NumeroPuntoVenta = 4, NumeroComprobante = number,
            FechaEmision = IssueDate.ToDateTime(TimeOnly.MinValue), Cae = long.Parse(Cae, CultureInfo.InvariantCulture),
            FechaVencimientoCae = ExpiryDate.ToDateTime(TimeOnly.MinValue) } };
        return new(1, TipoComprobanteGenerado.Fiscal, new("wsmtxca", "Homologacion", IssuerId, 4, type, number), "muestra-sintetica",
            "Authorized", receiver, "ARS", total, items.Select(line => new LineaComprobante(line.Description, line.Net + line.Vat,
                "ARS", "HERR-01", 1m, 7, cond == 5 ? line.Net + line.Vat : line.Net)).ToArray(),
            vat == 0 ? [] : [new("IVA 21%", vat, "ARS", "5", null, 21m, Tipo: TipoImpuestoComprobante.Iva)], legends ?? [],
            Serialize(request), 1, new(Cae, ExpiryDate, [], [], "A", "E"), fecha: IssueDate, cotizacion: 1m, neto: net,
            exento: 0, noGravado: 0, iva: vat, snapshotResponse: Serialize(response), snapshotResponseVersion: 1,
            condicionIvaReceptor: receiver.CondicionTributaria, codigoCondicionIvaReceptor: cond);
    }

    private static ComprobanteGenerado WsmtxcaMultipleRates(short type, short cond, DatosReceptor receiver, long number, bool includeThirdItem = false)
    {
        var items = new List<(string Description, decimal Net, decimal Vat, decimal Rate, short Code)>
        {
            (Description: "Producto gravado al 21 %", Net: 100m, Vat: 21m, Rate: 21m, Code: (short)5),
            (Description: "Producto gravado al 10,5 %", Net: 200m, Vat: 21m, Rate: 10.5m, Code: (short)4)
        };
        if (includeThirdItem) items.Add(("Segundo producto gravado al 21 %", 50m, 10.5m, 21m, 5));
        decimal net = items.Sum(item => item.Net), vat = items.Sum(item => item.Vat), total = net + vat;
        var request = new ComprobanteType { CodigoTipoComprobante = type, NumeroPuntoVenta = 4, NumeroComprobante = number,
            FechaEmision = IssueDate.ToDateTime(TimeOnly.MinValue), ImporteSubtotal = net, ImporteTotal = total, CodigoMoneda = "ARS", CotizacionMoneda = 1,
            CodigoTipoDocumento = checked((short)(receiver.CodigoTipoDocumento ?? 99)), NumeroDocumento = receiver.Identificacion, CondicionIvaReceptor = cond,
            ImporteGravado = net, ImporteExento = 0, ImporteNoGravado = 0 };
        foreach (var item in items)
            request.ArrayItems.Add(new ItemType { Descripcion = item.Description, Codigo = "MUESTRA", Cantidad = 1m,
                PrecioUnitario = cond == 5 ? item.Net + item.Vat : item.Net, CodigoCondicionIva = item.Code, ImporteIva = item.Vat, ImporteItem = item.Net + item.Vat, CodigoUnidadMedida = 7 });
        foreach (var rate in items.GroupBy(item => item.Code))
            request.ArraySubtotalesIva.Add(new SubtotalIvaType { Codigo = rate.Key, Importe = rate.Sum(item => item.Vat) });
        var response = new AutorizarComprobanteResponseType { Resultado = ResultadoSimpleType.A, ComprobanteResponse = new() {
            Cuit = IssuerId, CodigoTipoComprobante = type, NumeroPuntoVenta = 4, NumeroComprobante = number,
            FechaEmision = IssueDate.ToDateTime(TimeOnly.MinValue), Cae = long.Parse(Cae, CultureInfo.InvariantCulture),
            FechaVencimientoCae = ExpiryDate.ToDateTime(TimeOnly.MinValue) } };
        return new(1, TipoComprobanteGenerado.Fiscal, new("wsmtxca", "Homologacion", IssuerId, 4, type, number), "muestra-sintetica",
            "Authorized", receiver, "ARS", total, items.Select(item => new LineaComprobante(item.Description, item.Net + item.Vat,
            "ARS", "MUESTRA", 1m, 7, cond == 5 ? item.Net + item.Vat : item.Net)).ToArray(),
            items.GroupBy(item => item.Code).Select(group => new ImpuestoComprobante($"IVA {group.First().Rate}%", group.Sum(item => item.Vat),
                "ARS", group.Key.ToString(CultureInfo.InvariantCulture), null, group.First().Rate, Tipo: TipoImpuestoComprobante.Iva)).ToArray(),
            [], Serialize(request), 1, new(Cae, ExpiryDate, [], [], "A", "E"), fecha: IssueDate, cotizacion: 1m, neto: net,
            exento: 0, noGravado: 0, iva: vat, snapshotResponse: Serialize(response), snapshotResponseVersion: 1,
            condicionIvaReceptor: receiver.CondicionTributaria, codigoCondicionIvaReceptor: cond);
    }

    private static ComprobanteGenerado Receipt(string number, decimal amount, IReadOnlyList<ImputacionReciboX> allocations)
    {
        var receiver = new DatosReceptor(20304050607, "Distribuidora Horizonte SA", "San Martín 456, Córdoba", "Responsable Inscripto", 80);
        return new ReciboX(LegalIssuer).GenerarComprobante(new(number, IssueDate, receiver, "ARS", amount,
            [new("Pago a cuenta de factura", amount, "ARS")], [new("Transferencia bancaria", amount, "ARS")], allocations));
    }

    private static ComprobanteGenerado Multipage(DatosReceptor receiver, long number)
    {
        var lines = Enumerable.Range(1, 54).Select(i => ($"Línea {i:00}: repuesto industrial de descripción extensa para validar la paginación y repetición de encabezados.", 1.65m, 0.35m)).ToList();
        lines.Add(("Línea 55: reparación final y referencia extensa para completar el importe de muestra.", 10.90m, 2.10m));
        string[] legends = Enumerable.Range(1, 20).Select(i => $"Observación {i:00}: detalle informativo de muestra para verificar que no se superponga con la autorización fiscal.").ToArray();
        return Wsmtxca(1, 1, receiver, number, 100m, 21m, lines, legends);
    }

    private static RemitoGenerado RemitoR(DatosReceptor receiver, long number, bool original, int lineCount)
    {
        var goods = Enumerable.Range(1, lineCount).Select(i => new LineaRemito(
            $"Cajas de válvulas industriales de repuesto, lote sintético {i:00}", "bultos", i % 5 + 1)).ToArray();
        return new Remito(BrandedIssuer).GenerarComprobante(new DatosRemito(ClaseRemito.R, IssueDate, 4, number,
            receiver, "Depósito de destino, Av. Ejemplo 789, Córdoba", goods, esOriginal: original,
            cai: "12345678901234", primerNumero: 100, ultimoNumero: 999, vencimiento: new DateOnly(2026, 12, 31),
            imprenta: new DatosImprentaRemito("Gráfica Ejemplo SRL", IssuerId, new DateOnly(2026, 9, 1), 100, 999, "HAB-2026-001")));
    }

    private static RemitoGenerado RemitoX(DatosReceptor receiver, long number) =>
        new Remito(BrandedIssuer).GenerarComprobante(new DatosRemito(ClaseRemito.X, IssueDate, 4, number,
            receiver, "Nave B, depósito 3", [new("Muestras de repuestos para control interno", "cajas", 2)],
            ambitoInterno: "Predio Industrial Ejemplo, nave B"));
    private static string Serialize<T>(T value) { var serializer = new XmlSerializer(typeof(T)); using var writer = new StringWriter(CultureInfo.InvariantCulture); serializer.Serialize(writer, value); return writer.ToString(); }
    private static string E(string value) => System.Net.WebUtility.HtmlEncode(value);
}
