using NetArcaWs.Rendering.Examples;
using NetArcaWs.Rendering.Html;
using NetArcaWs.Rendering.Pdf;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Xunit;

namespace NetArcaWs.Rendering.Tests;

public sealed class CompactTotalsTests
{
    [Fact]
    public void Html_items_use_quantity_first_and_total_precedes_compact_tax_rows()
    {
        SampleCase sample = SampleGallery.CreateCases().Single(item => item.Id == "wsmtxca-a");
        string html = new ArcaWsGeneradorHTML(sample.Issuer).GenerarHTMLComprobante(sample.Document!);

        Assert.Contains("<th class=\"number\">Cant.</th><th>Descripción</th><th class=\"number\">P.Unit</th><th class=\"number\">Subtotal</th>", html);
        Assert.Contains("IVA 21%", html);
        Assert.True(html.IndexOf(">Total<", StringComparison.Ordinal) < html.IndexOf("IVA 21%", StringComparison.Ordinal));
        Assert.DoesNotContain("Impuestos y alícuotas", html);
        Assert.DoesNotContain("Base imponible</th>", html);
    }

    [Fact]
    public void Html_wsfe_without_items_uses_summary_rows_and_consumer_vat_disclosure_once()
    {
        SampleCase sample = SampleGallery.CreateCases().Single(item => item.Id == "wsfe-b-consumidor-final");
        string html = new ArcaWsGeneradorHTML(sample.Issuer).GenerarHTMLComprobante(sample.Document!);

        Assert.Contains("<th class=\"number\">Cant.</th><th>Descripción</th><th class=\"number\">P.Unit</th><th class=\"number\">Subtotal</th>", html);
        Assert.Contains("Neto gravado", html);
        Assert.DoesNotContain("Item 1", html);
        Assert.DoesNotContain("Cantidad</th>", html);
        Assert.Contains("IVA 21% Contenido", html);
        Assert.Contains("($100,00)", html);
        Assert.Contains("$21,00", html);
        Assert.Equal(1, Count(html, "$21,00"));
        Assert.True(html.IndexOf(">Total<", StringComparison.Ordinal) < html.IndexOf("IVA 21% Contenido", StringComparison.Ordinal));
        Assert.True(html.IndexOf("IVA 21% Contenido", StringComparison.Ordinal) < html.IndexOf("Ley 27.743", StringComparison.Ordinal));
        Assert.DoesNotContain("Impuestos y alícuotas", html);
    }

    [Fact]
    public void Pdf_uses_same_item_order_and_consumer_tax_base_after_total()
    {
        SampleCase sample = SampleGallery.CreateCases().Single(item => item.Id == "wsfe-b-consumidor-final");
        byte[] bytes = new ArcaWsGeneradorPDF(sample.Issuer).GenerarPDFComprobante(sample.Document!);
        using PdfDocument document = PdfDocument.Open(bytes);
        string text = string.Join("\n", document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
        string compact = string.Concat(text.Where(character => !char.IsWhiteSpace(character)));

        Assert.Contains("Cant.", text);
        Assert.Contains("Descripción", text);
        Assert.Contains("P.Unit", text);
        Assert.Contains("Subtotal", text);
        Assert.Contains("Total", text);
        Assert.Contains("IVA21%Contenido", compact);
        Assert.Contains("($100,00)", compact);
        Assert.DoesNotContain("Impuestosyalícuotas", compact);
        Assert.True(compact.IndexOf("Total", StringComparison.Ordinal) < compact.IndexOf("IVA21%Contenido", StringComparison.Ordinal));
        Assert.True(compact.IndexOf("IVA21%Contenido", StringComparison.Ordinal) < compact.IndexOf("Ley27.743", StringComparison.Ordinal));
    }

    [Fact]
    public void Renderer_versions_report_compact_rendering_revision()
    {
        SampleCase sample = SampleGallery.CreateCases().Single(item => item.Id == "wsfe-a-ri-logo");
        Assert.EndsWith("/html-3", new ArcaWsGeneradorHTML(sample.Issuer).VersionRenderizado, StringComparison.Ordinal);
        Assert.EndsWith("/pdf-3", new ArcaWsGeneradorPDF(sample.Issuer).VersionRenderizado, StringComparison.Ordinal);
    }

    [Fact]
    public void Wsmtxca_multi_rate_items_keep_source_totals_rates_and_unknown_bases()
    {
        SampleCase sample = SampleGallery.CreateCases().Single(item => item.Id == "wsmtxca-a-multialicuota");
        string html = new ArcaWsGeneradorHTML(sample.Issuer).GenerarHTMLComprobante(sample.Document!);
        byte[] bytes = new ArcaWsGeneradorPDF(sample.Issuer).GenerarPDFComprobante(sample.Document!);
        using PdfDocument pdf = PdfDocument.Open(bytes);
        string text = string.Concat(pdf.GetPages().Select(page => page.Text)
            .Select(value => string.Concat(value.Where(character => !char.IsWhiteSpace(character)))));

        Assert.Contains("21%", html);
        Assert.Contains("10,5%", html);
        Assert.DoesNotContain("($100,00)", html);
        Assert.Contains("$342,00", html);
        Assert.Contains("21%", text);
        Assert.Contains("10,5%", text);
        Assert.DoesNotContain("($100,00)", text);
        Assert.Contains("$342,00", text);
    }

    [Fact]
    public void Receiver_contact_fields_are_rendered_in_html_and_pdf()
    {
        SampleCase sample = SampleGallery.CreateCases().Single(item => item.Id == "wsfe-a-ri-logo");
        string html = new ArcaWsGeneradorHTML(sample.Issuer).GenerarHTMLComprobante(sample.Document!);
        Assert.Contains("Teléfono:", html);
        Assert.Contains("cliente@example.test", html);
        Assert.Contains("+54 351 555-0100", html);
        byte[] bytes = new ArcaWsGeneradorPDF(sample.Issuer).GenerarPDFComprobante(sample.Document!);
        using PdfDocument pdf = PdfDocument.Open(bytes);
        string text = string.Join("\n", pdf.GetPages().Select(page => page.Text));
        Assert.Contains("Teléfono", text);
        Assert.Contains("cliente@example.test", text);
    }

    private static int Count(string value, string fragment) =>
        (value.Length - value.Replace(fragment, string.Empty, StringComparison.Ordinal).Length) / fragment.Length;
}
