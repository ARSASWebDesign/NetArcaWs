using NetArcaWs.Rendering.Pdf;
using NetArcaWs.Rendering.Html;
using NetArcaWs.Documents;
using UglyToad.PdfPig;
using Xunit;
using StbImageSharp;
using ZXing;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using QRCoder;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Rendering.Examples;

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
        Assert.Contains("Total", string.Join("\n", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page))));
    }

    [Fact]
    public void Pdf_embeds_logo_and_rejects_non_printable_user_text()
    {
        using var qrData = QRCodeGenerator.GenerateQrCode("synthetic logo", QRCodeGenerator.ECCLevel.M);
        byte[] logo = new PngByteQRCode(qrData).GetGraphic(2);
        byte[] pdf = new ArcaWsGeneradorPDF(Issuer(logo)).GenerarPDFComprobante(Receipt());
        using (PdfDocument parsed = PdfDocument.Open(pdf)) Assert.Contains("Recibo", string.Join("\n", parsed.GetPages().Select(page => ContentOrderTextExtractor.GetText(page))));

        ComprobanteGenerado source = Receipt();
        var invalid = new ComprobanteGenerado(source.Version, source.Tipo, source.Identidad, source.TenantId, source.Estado,
            source.Receptor, source.Moneda, source.Total, [new LineaComprobante("Control\u0001 inválido", source.Total, "ARS")],
            source.Impuestos, source.Leyendas, source.SnapshotRequest, source.SnapshotVersion, source.Resultado,
            numeroInterno: source.NumeroInterno, fecha: source.Fecha, pagos: source.Pagos);
        Assert.Throws<ArgumentException>(() => new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(invalid));
    }

    [Fact]
    public void Pdf_exposes_normalized_principal_color_and_package_renderer_version()
    {
        var renderer = new ArcaWsGeneradorPDF(Issuer(), "#1a2b3c");
        Assert.Equal("#1A2B3C", renderer.ColorPrincipal);
        Assert.EndsWith("/pdf-3", renderer.VersionRenderizado, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => new ArcaWsGeneradorPDF(Issuer(), "red"));
    }

    [Fact]
    public async Task Pdf_renderer_profiles_keep_their_color_when_rendered_concurrently()
    {
        string[] colors = ["#1A4E72", "#A14800", "#F2A100", "#333333"];
        var renders = await Task.WhenAll(colors.Select(async color =>
        {
            var renderer = new ArcaWsGeneradorPDF(Issuer(), color);
            byte[] output = await Task.Run(() => renderer.GenerarPDFComprobante(Fiscal()));
            using PdfDocument parsed = PdfDocument.Open(output);
            return (renderer.ColorPrincipal, Text: parsed.GetPage(1).Text, Pdf: output);
        }));
        Assert.Equal(colors, renders.Select(result => result.ColorPrincipal));
        Assert.All(renders, result => Assert.Contains("Factura", result.Text));
        Assert.Equal(colors.Length, renders.Select(result => Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(result.Pdf))).Distinct().Count());
    }

    [Fact]
    public void Pdf_remito_r_prints_cai_and_copy_without_fiscal_qr_or_arca_authorization()
    {
        byte[] pdf = new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(RemitoR());
        using PdfDocument document = PdfDocument.Open(pdf);
        string text = string.Join("\n", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
        Assert.Contains("ORIGINAL", text);
        Assert.Contains("DOCUMENTO NO VALIDO COMO FACTURA", text);
        Assert.Contains("12345678901234", text);
        Assert.Contains("Rango", text);
        Assert.DoesNotContain("CAE", text);
        Assert.DoesNotContain("autorizado por ARCA", text);
        Assert.Empty(document.GetPages().SelectMany(page => page.GetImages()));
    }

    [Fact]
    public void Pdf_remito_rejects_a_different_issuer_from_the_configured_profile()
    {
        var other = new DatosEmisor(30987654321, "Otro emisor", null, "Otra calle", "Responsable Inscripto", ingresosBrutos: "123");
        RemitoGenerado remito = new Remito(other).GenerarComprobante(new DatosRemito(
            ClaseRemito.X, new DateOnly(2026, 10, 13), 4, 124, new DatosReceptor(20304050607, "Cliente", "Calle 2", "Responsable Inscripto"),
            "Galpón 3", [new LineaRemito("Cajas", "bultos", 2)], ambitoInterno: "Polo Industrial Norte"));
        Assert.Throws<ArgumentException>(() => new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(remito));
    }

    [Fact]
    public void Pdf_remito_x_prints_internal_scope_without_cai_cae_qr_or_authorization_claim()
    {
        byte[] pdf = new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(RemitoX());
        using PdfDocument document = PdfDocument.Open(pdf);
        string text = string.Join("\n", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
        Assert.Contains("Polo Industrial Norte", text);
        Assert.Contains("DOCUMENTO NO VALIDO COMO FACTURA", text);
        Assert.DoesNotContain("CAE", text);
        Assert.DoesNotContain("CAI", text);
        Assert.DoesNotContain("autorizado por ARCA", text);
        Assert.Empty(document.GetPages().SelectMany(page => page.GetImages()));
    }

    [Fact]
    public void Pdf_wsfex_displays_snapshot_currency_and_distinct_export_identifiers()
    {
        ComprobanteGenerado documentData = ExportInvoice();
        byte[] pdf = new ArcaWsGeneradorPDF(Issuer()).GenerarPDFComprobante(documentData);
        using PdfDocument parsed = PdfDocument.Open(pdf);
        string text = string.Concat(parsed.GetPages().Select(page => ContentOrderTextExtractor.GetText(page))
            .SelectMany(value => value.Where(character => !char.IsWhiteSpace(character))));
        Assert.Contains("CUITpaís:55000002126", text);
        Assert.Contains("Paísdedestino(código):212", text);
        Assert.Contains("IDimpositivoextranjero", text);
        Assert.DoesNotContain("Informacióncomplementaria::", text);
        Assert.Contains("USD-DólarEstadounidense", text);
        Assert.Contains("Cotización:$123,46", text);
        Assert.Contains("18/10/2026", text);
        Assert.Contains("FOB·Puertodedestino", text);
    }

    private static ComprobanteGenerado ExportInvoice()
    {
        ComprobanteGenerado source = HtmlRenderingTests.ContractFiscal("wsfex", query: false);
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(ClsFexRequest));
        using var input = new StringReader(source.SnapshotRequest!);
        ClsFexRequest request = (ClsFexRequest)serializer.Deserialize(input)!;
        request.MonedaId = "DOL";
        request.MonedaCtz = 123.456789m;
        request.CuitPaisCliente = 55000002126;
        request.DstCmp = 212;
        request.FechaPago = "20261018";
        request.FormaPago = "Transferencia";
        request.Incoterms = "FOB";
        request.IncotermsDs = "Puerto de destino";
        string snapshot = HtmlRenderingTests.Serialize(request);
        var receiver = new DatosReceptor(55000002126, source.Receptor.Nombre, source.Receptor.Domicilio,
            source.Receptor.CondicionTributaria, 80, request.IdImpositivo);
        return new ComprobanteGenerado(source.Version, source.Tipo, source.Identidad, source.TenantId,
            source.Estado, receiver, "DOL", source.Total, source.Detalles, source.Impuestos, source.Leyendas,
            snapshot, source.SnapshotVersion, source.Resultado, fecha: source.Fecha, cotizacion: 123.456789m,
            neto: source.Neto, exento: source.Exento, noGravado: source.NoGravado, iva: source.Iva,
            snapshotResponse: source.SnapshotResponse, snapshotResponseVersion: source.SnapshotResponseVersion);
    }

    private static DatosEmisor PdfIssuer() => HtmlRenderingTests.Issuer(
        SampleGallery.CreateCases().Single(item => item.Id == "wsfe-a-ri-logo").Issuer.LogoPng);

    private static RemitoGenerado RemitoR(DatosEmisor? issuer = null) => new Remito(issuer ?? Issuer()).GenerarComprobante(new DatosRemito(
        ClaseRemito.R, new DateOnly(2026, 10, 13), 4, 123, new DatosReceptor(20304050607, "Cliente", "Calle 2", "Responsable Inscripto"),
        "Depósito de destino", [new LineaRemito("Piezas metálicas", "unidades", 12)], cai: "12345678901234",
        primerNumero: 100, ultimoNumero: 200, vencimiento: new DateOnly(2027, 1, 31), esAutoimpresor: false,
        imprenta: new DatosImprentaRemito("Imprenta SA", 30111111118, new DateOnly(2026, 9, 1), 100, 200, "HAB-77")));

    private static RemitoGenerado RemitoX(DatosEmisor? issuer = null) => new Remito(issuer ?? Issuer()).GenerarComprobante(new DatosRemito(
        ClaseRemito.X, new DateOnly(2026, 10, 13), 4, 124, new DatosReceptor(20304050607, "Cliente", "Calle 2", "Responsable Inscripto"),
        "Galpón 3", [new LineaRemito("Cajas", "bultos", 2)], ambitoInterno: "Polo Industrial Norte"));

    [Fact]
    public void Pdf_samples_are_written_only_when_an_output_directory_is_configured()
    {
        string? directory = Environment.GetEnvironmentVariable("NETARCAWS_RENDERING_SAMPLE_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var samples = new Dictionary<string, byte[]>
        {
            ["recibo-x.pdf"] = new ArcaWsGeneradorPDF(PdfIssuer()).GenerarPDFComprobante(Receipt()),
            ["factura-fiscal.pdf"] = new ArcaWsGeneradorPDF(PdfIssuer()).GenerarPDFComprobante(Fiscal()),
            ["factura-exportacion-e.pdf"] = new ArcaWsGeneradorPDF(PdfIssuer(), "#1A4E72").GenerarPDFComprobante(ExportInvoice()),
            ["remito-r.pdf"] = new ArcaWsGeneradorPDF(PdfIssuer(), "#1A4E72").GenerarPDFComprobante(RemitoR(PdfIssuer())),
            ["remito-x.pdf"] = new ArcaWsGeneradorPDF(PdfIssuer()).GenerarPDFComprobante(RemitoX(PdfIssuer())),
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
