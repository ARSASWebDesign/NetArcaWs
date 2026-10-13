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
public sealed record SampleCase(string Id, string Group, string Description, ComprobanteGenerado Document, DatosEmisor Issuer);
public static class SampleGallery
{
    private const long IssuerId = 30123456789;
    private const string Cae = "12345678901234";
    private static readonly DateOnly IssueDate = new(2026, 10, 13), ExpiryDate = new(2026, 10, 23);
    private static readonly DatosEmisor LegalIssuer = new(IssuerId, "Comercial Ejemplo SRL", null, "Av. Ejemplo 123, Córdoba", "Responsable Inscripto", ingresosBrutos: "30-12345678-9", inicioActividades: new(2018, 4, 1));
    private static readonly DatosEmisor BrandedIssuer = new(IssuerId, "Comercial Ejemplo SRL", "La Tienda", "Av. Ejemplo 123, Córdoba", "Responsable Inscripto", SimpleLogoPng.Create(), ingresosBrutos: "30-12345678-9", inicioActividades: new(2018, 4, 1));
    private static readonly DatosEmisor BrandedMonoIssuer = new(IssuerId, "Comercial Ejemplo SRL", "La Tienda", "Av. Ejemplo 123, Córdoba", "Monotributista", SimpleLogoPng.Create(), ingresosBrutos: "30-12345678-9", inicioActividades: new(2018, 4, 1));
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static IReadOnlyList<SampleCase> CreateCases()
    {
        DatosReceptor ri = new(20304050607, "Distribuidora Horizonte SA", "San Martín 456, Córdoba", "Responsable Inscripto", 80);
        DatosReceptor mono = new(27234567891, "Taller del Centro", "Belgrano 222, Córdoba", "Monotributista", 80);
        DatosReceptor final = new(0, "Consumidor Final", null, null, 99);
        DatosReceptor foreign = new(840, "Norte Trading LLC", "21 Market St, Miami, FL", "Cliente del exterior", 80, "9876543210123");
        return [
            new("wsfe-a-ri-logo", "WSFE / Factura A", "Factura A a responsable inscripto con IVA discriminado", Wsfe(1, 1, ri, 123, 100m, 21m, "Venta de mercadería"), BrandedIssuer),
            new("wsfe-a-monotributo", "WSFE / Factura A", "Factura A a monotributista con leyenda legal automática", Wsfe(1, 6, mono, 124, 100m, 21m, "Servicio de mantenimiento"), BrandedIssuer),
            new("wsfe-b-consumidor-final", "WSFE / Factura B", "Factura B a consumidor final sin identificación; IVA transparente", Wsfe(6, 5, final, 125, 100m, 21m, "Venta al consumidor final"), BrandedIssuer),
            new("wsfe-c-consumidor-final", "WSFE / Factura C", "Factura C a consumidor final sin identificación, emitida por monotributista", Wsfe(11, 5, final, 126, 121m, 0m, "Servicio a consumidor final"), BrandedMonoIssuer),
            new("wsfe-nota-credito-a", "WSFE / Nota de crédito A", "Nota de crédito A asociada a factura A 00004-00000122", Wsfe(3, 1, ri, 127, 100m, 21m, "Anulación parcial de venta", associate: true), BrandedIssuer),
            new("wsfe-nota-debito-a", "WSFE / Nota de débito A", "Nota de débito A asociada a factura A 00004-00000122", Wsfe(2, 1, ri, 128, 100m, 21m, "Diferencia de precio", associate: true), BrandedIssuer),
            new("wsfex-e-exportacion", "WSFEX / Exportación E", "Exportación E con identificador fiscal extranjero", Wsfex(foreign, 129), BrandedIssuer),
            new("wsmtxca-a", "WSMTXCA / Factura A", "Factura A de factura con detalle e IVA", Wsmtxca(1, 1, ri, 130, 100m, 21m), BrandedIssuer),
            new("wsmtxca-b", "WSMTXCA / Factura B", "Factura B de consumidor final con IVA transparente", Wsmtxca(6, 5, final, 131, 100m, 21m), BrandedIssuer),
            new("x-recibo-total", "Recibo X", "Recibo X con pago total e imputación", Receipt("X-0001", 121m, [new("Factura A 00004-00000122", 121m, "ARS")]), LegalIssuer),
            new("x-recibo-parcial", "Recibo X", "Recibo X con pago parcial e imputación", Receipt("X-0002", 50m, [new("Factura A 00004-00000122", 50m, "ARS")]), LegalIssuer),
            new("wsmtxca-a-multipagina", "WSMTXCA / Multipágina", "Factura A con detalle y leyendas extensas para revisar paginación", Multipage(ri, 132), BrandedIssuer)
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
        var html = new StringBuilder("<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Galería sintética de comprobantes</title><style>body{font:16px system-ui,sans-serif;max-width:1100px;margin:2rem auto;padding:0 1rem;color:#18212b}h1,h2{color:#132d4b}.group{border-top:2px solid #cbd5e1;margin-top:2rem}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(250px,1fr));gap:1rem}.card{border:1px solid #cbd5e1;border-radius:10px;padding:1rem}.links{display:flex;gap:1rem;margin-top:1rem}</style></head><body><h1>Galería sintética de comprobantes</h1><p>Datos ficticios para revisión visual. No prueban autorización real ni homologación ARCA.</p>");
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
                ExampleDocumentStore.Save(db, sample.Document, issuer);
                (ComprobanteGenerado doc, DatosEmisor persistedIssuer) = ExampleDocumentStore.Load(db);
                File.WriteAllText(Path.Combine(dir, "document.json"), JsonSerializer.Serialize(doc, JsonOptions));
                File.WriteAllText(Path.Combine(dir, "issuer.json"), JsonSerializer.Serialize(persistedIssuer, JsonOptions));
                File.WriteAllText(Path.Combine(dir, "comprobante.html"), new ArcaWsGeneradorHTML(persistedIssuer).GenerarHTMLComprobante(doc));
                File.WriteAllBytes(Path.Combine(dir, "comprobante.pdf"), new ArcaWsGeneradorPDF(persistedIssuer).GenerarPDFComprobante(doc));
                html.Append("<article class=\"card\"><h3>").Append(VariantTitle(i)).Append("</h3><p>").Append(E(sample.Description))
                    .Append("</p><p>Persistido y reabierto desde SQLite + JSON.</p><p><a href=\"").Append(E(id))
                    .Append("/comprobante.html\">Ver HTML</a> · <a href=\"").Append(E(id)).Append("/comprobante.pdf\">Abrir PDF</a></p></article>");
            }
            html.Append("</div></section>");
        }
        html.Append("</body></html>");
        File.WriteAllText(Path.Combine(run, "index.html"), html.ToString(), new UTF8Encoding(false));
        ExampleDocumentStore.WriteRunManifest(run, created, cases);
        return run;
    }

    private static DatosEmisor BrandVariant(DatosEmisor x, int i) => i switch {
        0 => new(x.Identificacion, x.RazonSocial, "La Tienda", x.Domicilio, x.CondicionTributaria, SimpleLogoPng.Create(), x.IngresosBrutos, x.InicioActividades),
        1 => new(x.Identificacion, x.RazonSocial, "La Tienda", x.Domicilio, x.CondicionTributaria, ingresosBrutos: x.IngresosBrutos, inicioActividades: x.InicioActividades),
        _ => new(x.Identificacion, x.RazonSocial, null, x.Domicilio, x.CondicionTributaria, ingresosBrutos: x.IngresosBrutos, inicioActividades: x.InicioActividades)
    };
    private static string VariantTitle(int i) => i switch { 0 => "PNG LOGO · La Tienda · razón social", 1 => "Sin logo · La Tienda · razón social", _ => "Sin logo · razón social sola" };

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
            CbteTipo = 19, PuntoVta = 4, CbteNro = number, TipoExpo = 1, DstCmp = 840, Cliente = receiver.Nombre,
            CuitPaisCliente = 840, DomicilioCliente = receiver.Domicilio ?? "", IdImpositivo = receiver.IdentificacionFiscal,
            MonedaId = "USD", MonedaCtz = 1200m, ImpTotal = 125m };
        request.Items.Add(new Item { ProCodigo = "VAL-INOX-01", ProDs = description, ProQty = 5m, ProUmed = 7, ProPrecioUni = 25m, ProTotalItem = 125m });
        var authorized = new ClsFexOutAuthorize { Id = request.Id, Cuit = IssuerId, CbteTipo = 19, PuntoVta = 4, CbteNro = number,
            Cae = Cae, FchVencCae = ExpiryDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            FchCbte = IssueDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture), Resultado = "A" };
        return new(1, TipoComprobanteGenerado.Fiscal, new("wsfex", "Homologacion", IssuerId, 4, 19, number), "muestra-sintetica", "Authorized",
            receiver, "USD", 125m, [new(description, 125m, "USD", "VAL-INOX-01", 5m, 7, 25m)], [], [], Serialize(request), 1,
            new(Cae, ExpiryDate, [], [], "A", "E"), fecha: IssueDate, cotizacion: 1200m, neto: 125m,
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
            PrecioUnitario = line.Net, CodigoCondicionIva = 1,
                ImporteIva = line.Vat, ImporteItem = line.Net + line.Vat, CodigoUnidadMedida = 7 });
        if (vat > 0) request.ArraySubtotalesIva.Add(new SubtotalIvaType { Codigo = 5, Importe = vat });
        var response = new AutorizarComprobanteResponseType { Resultado = ResultadoSimpleType.A, ComprobanteResponse = new() {
            Cuit = IssuerId, CodigoTipoComprobante = type, NumeroPuntoVenta = 4, NumeroComprobante = number,
            FechaEmision = IssueDate.ToDateTime(TimeOnly.MinValue), Cae = long.Parse(Cae, CultureInfo.InvariantCulture),
            FechaVencimientoCae = ExpiryDate.ToDateTime(TimeOnly.MinValue) } };
        return new(1, TipoComprobanteGenerado.Fiscal, new("wsmtxca", "Homologacion", IssuerId, 4, type, number), "muestra-sintetica",
            "Authorized", receiver, "ARS", total, items.Select(line => new LineaComprobante(line.Description, line.Net + line.Vat,
                "ARS", "HERR-01", 1m, 7, line.Net)).ToArray(),
            vat == 0 ? [] : [new("IVA 21%", vat, "ARS", "5", null, 21m, Tipo: TipoImpuestoComprobante.Iva)], legends ?? [],
            Serialize(request), 1, new(Cae, ExpiryDate, [], [], "A", "E"), fecha: IssueDate, cotizacion: 1m, neto: net,
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
    private static string Serialize<T>(T value) { var serializer = new XmlSerializer(typeof(T)); using var writer = new StringWriter(CultureInfo.InvariantCulture); serializer.Serialize(writer, value); return writer.ToString(); }
    private static string E(string value) => System.Net.WebUtility.HtmlEncode(value);
}
