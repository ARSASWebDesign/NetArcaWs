using NetArcaWs.Documents;
using NetArcaWs.Rendering.Html;
using NetArcaWs.Rendering.Pdf;

var issuer = new DatosEmisor(30123456789, "Emisor de prueba", "Marca de prueba",
    "Av. Ejemplo 123, Córdoba", "Responsable Inscripto", ingresosBrutos: "30-12345678-9");
var receiver = new DatosReceptor(20304050607, "Receptor de prueba", "San Martín 456, Córdoba",
    "Responsable Inscripto", 80);
ComprobanteGenerado document = new ReciboX(issuer).GenerarComprobante(new DatosReciboX(
    "SMOKE-0001", new DateOnly(2026, 10, 13), receiver, "ARS", 125.50m,
    [new LineaReciboX("Servicio sintético", 125.50m, "ARS")],
    [new PagoReciboX("Transferencia", 125.50m, "ARS")]));

var htmlRenderer = new ArcaWsGeneradorHTML(issuer, "#1A4E72");
var pdfRenderer = new ArcaWsGeneradorPDF(issuer, "#1A4E72");
if (htmlRenderer.ColorPrincipal != "#1A4E72" || pdfRenderer.ColorPrincipal != "#1A4E72" ||
    !htmlRenderer.VersionRenderizado.Contains("/html-2", StringComparison.Ordinal) ||
    !pdfRenderer.VersionRenderizado.Contains("/pdf-2", StringComparison.Ordinal))
    throw new InvalidOperationException("The installed renderer packages did not expose their color and render-version configuration.");

string html = htmlRenderer.GenerarHTMLComprobante(document);
byte[] pdf = pdfRenderer.GenerarPDFComprobante(document);
if (!html.Contains("DOCUMENTO NO VALIDO COMO FACTURA", StringComparison.Ordinal) ||
    !html.Contains("Receptor de prueba", StringComparison.Ordinal))
    throw new InvalidOperationException("The installed HTML renderer did not produce the expected X receipt content.");
if (pdf.Length < 1_000 || !pdf.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
    throw new InvalidOperationException("The installed PDF renderer did not produce a PDF document.");

var remitoR = new Remito(issuer).GenerarComprobante(new DatosRemito(ClaseRemito.R,
    new DateOnly(2026, 10, 13), 4, 2, receiver, "Depósito de ejemplo",
    [new LineaRemito("Repuestos de muestra", "cajas", 2)], cai: "12345678901234",
    primerNumero: 1, ultimoNumero: 100, vencimiento: new DateOnly(2026, 12, 31),
    imprenta: new DatosImprentaRemito("Gráfica de ejemplo", 30123456789, new DateOnly(2026, 9, 1), 1, 100, "HAB-01")));
var remitoX = new Remito(issuer).GenerarComprobante(new DatosRemito(ClaseRemito.X,
    new DateOnly(2026, 10, 13), 4, 3, receiver, "Nave B",
    [new LineaRemito("Muestras internas", "cajas", 1)], ambitoInterno: "Predio Industrial de ejemplo"));
string remitoHtml = htmlRenderer.GenerarHTMLComprobante(remitoR) + htmlRenderer.GenerarHTMLComprobante(remitoX);
byte[] remitoPdf = pdfRenderer.GenerarPDFComprobante(remitoR);
byte[] remitoXPdf = pdfRenderer.GenerarPDFComprobante(remitoX);
if (!remitoHtml.Contains("COD. 091", StringComparison.Ordinal) ||
    !remitoHtml.Contains("Predio Industrial de ejemplo", StringComparison.Ordinal) ||
    remitoHtml.Contains("CAE:", StringComparison.Ordinal) ||
    remitoPdf.Length < 1_000 || remitoXPdf.Length < 1_000)
    throw new InvalidOperationException("The installed renderer packages did not render the synthetic R/X remito samples correctly.");

Console.WriteLine("Optional HTML/PDF package consumer smoke passed with synthetic X receipt and R/X remito output.");
