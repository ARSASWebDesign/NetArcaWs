using NetArcaWs.Rendering.Pdf;
using NetArcaWs.Rendering.Html;
using NetArcaWs.Documents;
using UglyToad.PdfPig;
using Xunit;
using StbImageSharp;
using ZXing;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using QRCoder;

namespace NetArcaWs.Rendering.Tests;

public sealed partial class HtmlRenderingTests
{
    [Fact]
    public void Pdf_receipt_contains_legal_text_and_emitter_brand_without_fiscal_qr()
    {
        byte[] pdf = new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(Receipt());
        using PdfDocument document = PdfDocument.Open(pdf);
        string text = string.Join("\n", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
        Assert.Contains("DOCUMENTONOVALIDOCOMOFACTURA", text.Replace(" ", string.Empty, StringComparison.Ordinal));
        Assert.Contains("Emisor", text);
        Assert.Contains("Comercial", text);
        Assert.DoesNotContain("CAE", text);
        Assert.DoesNotContain("12345678901234", text);
        Assert.Empty(document.GetPages().SelectMany(page => page.GetImages()));
    }

    [Fact]
    public void Pdf_fiscal_document_contains_correlated_authorization_and_qr_payload()
    {
        byte[] pdf = new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(Fiscal());
        using PdfDocument document = PdfDocument.Open(pdf);
        string text = string.Join("\n", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
        Assert.Contains("12345678901234", text);
        Assert.Contains("VencimientoCAE", text.Replace(" ", string.Empty, StringComparison.Ordinal));
        var qrImage = document.GetPages().SelectMany(page => page.GetImages()).Single();
        Assert.True(document.GetPage(1).Letters.Where(letter => letter.Value == "A").Max(letter => letter.FontSize) > 20);
        Assert.True(qrImage.TryGetPng(out byte[]? png));
        Assert.NotNull(png);
        ImageResult decoded = ImageResult.FromMemory(png, ColorComponents.RedGreenBlue);
        var luminance = new RGBLuminanceSource(decoded.Data, decoded.Width, decoded.Height, RGBLuminanceSource.BitmapFormat.RGB24);
        Result qr = new BarcodeReaderGeneric().Decode(luminance);
        Assert.Equal(DecodeRenderedQr(new ArcaWsGeneradorHTML(Issuer()).GenerarHTMLComprobante(Fiscal())), qr.Text);
    }

    [Fact]
    public void Pdf_long_receipt_is_paginated_and_preserves_total()
    {
        ComprobanteGenerado source = Receipt();
        var longReceipt = new ComprobanteGenerado(source.Version, source.Tipo, source.Identidad, source.TenantId,
            source.Estado, source.Receptor, source.Moneda, source.Total,
            Enumerable.Range(1, 100).Select(i => new LineaComprobante($"Concepto {i} descripción extensa", 1.25m, "ARS")).ToArray(),
            source.Impuestos, source.Leyendas, source.SnapshotRequest, source.SnapshotVersion, source.Resultado,
            numeroInterno: source.NumeroInterno, fecha: source.Fecha,
            pagos: [new PagoReciboX("Efectivo", source.Total, "ARS")]);
        byte[] pdf = new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(longReceipt);
        using PdfDocument document = PdfDocument.Open(pdf);
        Assert.True(document.NumberOfPages > 1);
        Assert.Contains("Total recibido", string.Join("\n", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page))));
    }

    [Fact]
    public void Pdf_long_fiscal_document_keeps_total_and_authorization_on_final_page()
    {
        ComprobanteGenerado source = Fiscal(legends: Enumerable.Range(1, 100).Select(i => $"Observación extensa {i}: información con acentos, importes y referencias que debe conservarse en una página posterior.").ToArray());
        byte[] pdf = new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(source);
        using PdfDocument document = PdfDocument.Open(pdf);
        Assert.True(document.NumberOfPages > 1);
        string finalText = ContentOrderTextExtractor.GetText(document.GetPage(document.NumberOfPages));
        Assert.Contains("12345678901234", finalText);
        Assert.Contains("TOTAL", string.Join("\n", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page))));
    }

    [Fact]
    public void Pdf_embeds_logo_and_rejects_non_printable_user_text()
    {
        using var qrData = QRCodeGenerator.GenerateQrCode("synthetic logo", QRCodeGenerator.ECCLevel.M);
        byte[] logo = new PngByteQRCode(qrData).GetGraphic(2);
        byte[] pdf = new ArcaWsGeneradorPDF(Issuer(logo)).GenerarPDFComprobante(Receipt());
        using (PdfDocument document = PdfDocument.Open(pdf)) Assert.Single(document.GetPages().SelectMany(page => page.GetImages()));

        ComprobanteGenerado source = Receipt();
        var invalid = new ComprobanteGenerado(source.Version, source.Tipo, source.Identidad, source.TenantId, source.Estado,
            source.Receptor, source.Moneda, source.Total, [new LineaComprobante("Control\u0001 inválido", source.Total, "ARS")],
            source.Impuestos, source.Leyendas, source.SnapshotRequest, source.SnapshotVersion, source.Resultado,
            numeroInterno: source.NumeroInterno, fecha: source.Fecha, pagos: source.Pagos);
        Assert.Throws<ArgumentException>(() => new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(invalid));
    }

    [Fact]
    public void Pdf_samples_are_written_only_when_an_output_directory_is_configured()
    {
        string? directory = Environment.GetEnvironmentVariable("NETARCAWS_RENDERING_SAMPLE_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var samples = new Dictionary<string, byte[]>
        {
            ["recibo-x.pdf"] = new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(Receipt()),
            ["factura-fiscal.pdf"] = new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(Fiscal()),
            ["recibo-x-multipagina.pdf"] = new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(LongReceipt()),
            ["factura-fiscal-multipagina.pdf"] = new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(Fiscal(legends: Enumerable.Range(1, 100).Select(i => $"Observación extensa {i}: información con acentos, importes y referencias que debe conservarse en una página posterior.").ToArray()))
        };
        foreach ((string name, byte[] content) in samples) File.WriteAllBytes(Path.Combine(directory, name), content);
        File.WriteAllText(Path.Combine(directory, "manifest.json"), System.Text.Json.JsonSerializer.Serialize(new
        {
            generatedAtUtc = DateTimeOffset.UtcNow,
            scenarios = samples.Keys.ToArray()
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }

    private static ComprobanteGenerado LongReceipt()
    {
        ComprobanteGenerado source = Receipt();
        return new ComprobanteGenerado(source.Version, source.Tipo, source.Identidad, source.TenantId, source.Estado,
            source.Receptor, source.Moneda, source.Total,
            Enumerable.Range(1, 100).Select(i => new LineaComprobante($"Concepto {i} descripción extensa", 1.25m, "ARS")).ToArray(),
            source.Impuestos, source.Leyendas, source.SnapshotRequest, source.SnapshotVersion, source.Resultado,
            numeroInterno: source.NumeroInterno, fecha: source.Fecha,
            pagos: [new PagoReciboX("Efectivo", source.Total, "ARS")]);
    }

}
