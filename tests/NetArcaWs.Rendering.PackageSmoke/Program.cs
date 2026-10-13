using NetArcaWs.Documents;
using NetArcaWs.Rendering.Html;
using NetArcaWs.Rendering.Pdf;

var issuer = new DatosEmisor(30123456789, "Emisor de prueba", "Marca de prueba",
    "Av. Ejemplo 123, Córdoba", "Responsable Inscripto");
var receiver = new DatosReceptor(20304050607, "Receptor de prueba", "San Martín 456, Córdoba",
    "Responsable Inscripto", 80);
ComprobanteGenerado document = new ReciboX(issuer).GenerarComprobante(new DatosReciboX(
    "SMOKE-0001", new DateOnly(2026, 10, 13), receiver, "ARS", 125.50m,
    [new LineaReciboX("Servicio sintético", 125.50m, "ARS")],
    [new PagoReciboX("Transferencia", 125.50m, "ARS")]));

string html = new ArcaWsGeneradorHTML(issuer).GenerarHTMLComprobante(document);
byte[] pdf = new ArcaWsGeneradorPDF(issuer).GenerarPDFComprobante(document);
if (!html.Contains("DOCUMENTO NO VALIDO COMO FACTURA", StringComparison.Ordinal) ||
    !html.Contains("Receptor de prueba", StringComparison.Ordinal))
    throw new InvalidOperationException("The installed HTML renderer did not produce the expected X receipt content.");
if (pdf.Length < 1_000 || !pdf.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
    throw new InvalidOperationException("The installed PDF renderer did not produce a PDF document.");

Console.WriteLine("Optional HTML/PDF package consumer smoke passed with rendered synthetic X receipt output.");
