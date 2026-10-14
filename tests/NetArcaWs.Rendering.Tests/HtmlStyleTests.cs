using NetArcaWs.Documents;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Rendering.Html;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace NetArcaWs.Rendering.Tests;

public sealed partial class HtmlRenderingTests
{
    [Theory]
    [InlineData("#336699", "#336699")]
    [InlineData("#abcdef", "#ABCDEF")]
    public void Principal_color_is_normalized_and_version_is_renderer_specific(string input, string expected)
    {
        var renderer = new ArcaWsGeneradorHTML(HtmlRenderingTests.Issuer(), input);
        Assert.Equal(expected, renderer.ColorPrincipal);
        Assert.Contains("/html-2", renderer.VersionRenderizado);
    }

    [Fact]
    public void Renderer_version_preserves_prerelease_and_removes_only_build_metadata()
    {
        AssemblyName name = new("SyntheticRenderer");
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var attribute = new CustomAttributeBuilder(typeof(AssemblyInformationalVersionAttribute).GetConstructor([typeof(string)])!,
            ["0.6.0-rc.1+build.42"]);
        assembly.SetCustomAttribute(attribute);
        Type style = typeof(ArcaWsGeneradorHTML).Assembly.GetType("NetArcaWs.Rendering.Shared.RenderingStyle", throwOnError: true)!;
        MethodInfo method = style.GetMethod("RendererVersion", BindingFlags.Static | BindingFlags.NonPublic)!;
        string version = (string)method.Invoke(null, [assembly, "html-2"])!;
        Assert.Equal("0.6.0-rc.1/html-2", version);
    }

    [Theory]
    [InlineData("#fff")]
    [InlineData("336699")]
    [InlineData("#12GG99")]
    [InlineData("red;body{display:none}")]
    public void Invalid_principal_color_is_rejected(string color)
    {
        Assert.Throws<ArgumentException>(() => new ArcaWsGeneradorHTML(HtmlRenderingTests.Issuer(), color));
    }

    [Fact]
    public void Renderer_uses_black_or_white_text_for_principal_color_and_keeps_rows_white()
    {
        string dark = new ArcaWsGeneradorHTML(HtmlRenderingTests.Issuer(), "#123456").GenerarHTMLComprobante(HtmlRenderingTests.Fiscal());
        string light = new ArcaWsGeneradorHTML(HtmlRenderingTests.Issuer(), "#EEEEEE").GenerarHTMLComprobante(HtmlRenderingTests.Fiscal());
        Assert.Contains("--principal:#123456;--principal-text:#FFFFFF", dark);
        Assert.Contains("--principal:#EEEEEE;--principal-text:#000000", light);
        Assert.Contains("background:#fff", dark);
        Assert.Contains("border-radius:4px", dark);
    }

    [Fact]
    public void Default_color_is_gray_and_authorization_is_unframed_plain_bold_text()
    {
        string html = new ArcaWsGeneradorHTML(HtmlRenderingTests.Issuer()).GenerarHTMLComprobante(HtmlRenderingTests.Fiscal());
        Assert.Contains("--principal:#333333", html);
        Assert.Contains("<div class=\"footer-copy\"><strong>Comprobante autorizado por ARCA</strong>", html);
        Assert.DoesNotContain("badge", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fiscal_invoice_uses_principal_header_and_two_real_receiver_columns()
    {
        string html = new ArcaWsGeneradorHTML(HtmlRenderingTests.Issuer(), "#123456")
            .GenerarHTMLComprobante(HtmlRenderingTests.Fiscal());
        Assert.Contains(".issuer{", html);
        Assert.Contains("background:var(--principal);color:var(--principal-text)", html);
        Assert.Contains(".issuer{", html);
        Assert.Contains("<div class=\"issuer-brand\">", html);
        Assert.Contains("<div class=\"document-code\"><strong>A</strong>", html);
        Assert.Contains("<div class=\"issuer-fiscal\"><div>CUIT:", html);
        Assert.Contains("<div class=\"receiver-columns\"><div class=\"receiver-left\">", html);
        Assert.Contains("<div class=\"receiver-right\">", html);
        int headerStart = html.IndexOf("<header class=\"issuer\">", StringComparison.Ordinal);
        int headerEnd = html.IndexOf("</header>", headerStart, StringComparison.Ordinal);
        Assert.DoesNotContain("CAE:", html[headerStart..headerEnd]);
        Assert.Contains("CAE: 12345678901234", html[html.IndexOf("<footer", headerEnd, StringComparison.Ordinal)..]);
    }

    [Fact]
    public void Receipt_x_uses_the_same_header_and_receiver_structure()
    {
        string html = new ArcaWsGeneradorHTML(HtmlRenderingTests.Issuer())
            .GenerarHTMLComprobante(HtmlRenderingTests.Receipt());
        Assert.Contains("<header class=\"issuer\">", html);
        Assert.Contains("background:var(--principal);color:var(--principal-text)", html);
        Assert.Contains("<div class=\"document-code\"><strong>X</strong>", html);
        Assert.Contains("<div class=\"receiver-columns\"><div class=\"receiver-left\">", html);
        Assert.Contains("<div class=\"receiver-right\">", html);
    }

    [Theory]
    [InlineData("A", 1)]
    [InlineData("B", 6)]
    [InlineData("C", 11)]
    [InlineData("E", 19)]
    public void Fiscal_letters_share_the_same_header_and_receiver_grid(string letter, int type)
    {
        ComprobanteGenerado document = letter == "E"
            ? HtmlRenderingTests.ContractFiscal("wsfex", query: false)
            : HtmlRenderingTests.Fiscal(tipo: type);
        string html = new ArcaWsGeneradorHTML(HtmlRenderingTests.Issuer())
            .GenerarHTMLComprobante(document);
        Assert.Contains($"<div class=\"document-code\"><strong>{letter}</strong>", html);
        Assert.Contains("<div class=\"issuer-brand\">", html);
        Assert.Contains("<div class=\"issuer-fiscal\">", html);
        Assert.Contains("<div class=\"receiver-columns\"><div class=\"receiver-left\">", html);
        Assert.Contains("<div class=\"receiver-right\">", html);
    }

    [Fact]
    public void Consumer_final_total_uses_the_principal_block_while_detail_rows_stay_white()
    {
        string html = new ArcaWsGeneradorHTML(HtmlRenderingTests.Issuer(), "#123456")
            .GenerarHTMLComprobante(HtmlRenderingTests.Fiscal(tipo: 6, condition: 5, conditionText: "Consumidor final"));
        Assert.Contains(".transparency>.grand-total{font-size:12pt;font-weight:700;background:var(--principal);color:var(--principal-text)", html);
        Assert.Contains(".transparency>div{display:flex;justify-content:space-between;padding:4px;border-bottom:1px solid #ddd}", html);
        Assert.Contains("background:#fff", html);
    }

    [Theory]
    [InlineData(ClaseRemito.R)]
    [InlineData(ClaseRemito.X)]
    public void Remito_renders_required_class_data_without_fiscal_qr_or_totals(ClaseRemito clase)
    {
        DatosEmisor emisor = HtmlRenderingTests.Issuer();
        bool isR = clase == ClaseRemito.R;
        var details = new DatosRemito(clase, new DateOnly(2026, 10, 13), 4, 17,
            new DatosReceptor(20304050607, "Receptor", "Calle 2", "Responsable Inscripto"),
            "Depósito norte", [new LineaRemito("Piezas <frágiles>", "cajas", 3)],
            cai: isR ? "12345678901234" : null,
            primerNumero: isR ? 1 : null, ultimoNumero: isR ? 200 : null,
            vencimiento: isR ? new DateOnly(2027, 10, 13) : null,
            imprenta: isR ? new DatosImprentaRemito("Gráfica", 30123456789,
                new DateOnly(2026, 1, 1), 1, 200, "H-123") : null,
            ambitoInterno: isR ? null : "Parque industrial local");
        RemitoGenerado generated = new Remito(emisor).GenerarComprobante(details);
        string html = new ArcaWsGeneradorHTML(emisor).GenerarHTMLComprobante(generated);
        int topLegend = html.IndexOf("DOCUMENTO NO VALIDO COMO FACTURA", StringComparison.Ordinal);
        int header = html.IndexOf("<header class=\"issuer\">", StringComparison.Ordinal);
        int footer = html.IndexOf("<footer class=\"authorization\">", StringComparison.Ordinal);
        Assert.True(topLegend >= 0 && topLegend < header);
        Assert.True(html.IndexOf("DOCUMENTO NO VALIDO COMO FACTURA", footer, StringComparison.Ordinal) >= footer);
        string? previewPath = Environment.GetEnvironmentVariable($"NETARCAWS_RENDER_REMITO_{clase}_PATH");
        if (!string.IsNullOrWhiteSpace(previewPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(previewPath))!);
            File.WriteAllText(previewPath, "<!-- Synthetic demonstration data; not a real fiscal document. -->\n" + html);
        }
        Assert.Equal(isR, html.Contains("COD. 091", StringComparison.Ordinal));
        Assert.Equal(isR, html.Contains("CAI: 12345678901234", StringComparison.Ordinal));
        Assert.Equal(!isR, html.Contains("Uso interno:", StringComparison.Ordinal));
        Assert.Contains("DOCUMENTO NO VALIDO COMO FACTURA", html);
        Assert.Contains("Piezas", html);
        Assert.DoesNotContain("<frágiles>", html);
        Assert.DoesNotContain("CAE", html);
        Assert.DoesNotContain("<img class=\"qr-image\"", html);
        Assert.DoesNotContain("<div class=\"grand-total\"", html);
        Assert.Contains("<div class=\"receiver-columns\"><div class=\"receiver-left\">", html);
        Assert.Contains("<div class=\"receiver-right\">", html);
    }

    [Fact]
    public void Wsfex_export_fields_come_from_snapshot_and_escape_user_text()
    {
        ComprobanteGenerado invoice = HtmlRenderingTests.ContractFiscal("wsfex", query: false);
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(ClsFexRequest));
        using var reader = new StringReader(invoice.SnapshotRequest!);
        var request = (ClsFexRequest)serializer.Deserialize(reader)!;
        request.MonedaId = "DOL";
        request.MonedaCtz = 123.456789m;
        request.Incoterms = "FOB";
        request.IncotermsDs = "Puerto <destino>";
        request.ObsComerciales = "Carga & embalaje";
        request.Obs = "Entrega acordada";
        string snapshot = HtmlRenderingTests.Serialize(request);
        ComprobanteGenerado changed = new(invoice.Version, invoice.Tipo, invoice.Identidad, invoice.TenantId,
            invoice.Estado, invoice.Receptor, "DOL", invoice.Total, invoice.Detalles, invoice.Impuestos,
            invoice.Leyendas, snapshot, invoice.SnapshotVersion, invoice.Resultado, fecha: invoice.Fecha,
            cotizacion: 123.456789m, neto: invoice.Neto, exento: invoice.Exento,
            noGravado: invoice.NoGravado, iva: invoice.Iva, snapshotResponse: invoice.SnapshotResponse,
            snapshotResponseVersion: invoice.SnapshotResponseVersion);
        string html = new ArcaWsGeneradorHTML(HtmlRenderingTests.Issuer()).GenerarHTMLComprobante(changed);
        Assert.Contains("DOL · USD", html);
        Assert.Contains("123.456789", html);
        Assert.Contains("ID impositivo del cliente", html);
        Assert.Contains("Puerto &lt;destino&gt;", html);
        Assert.Contains("Carga &amp; embalaje", html);
        Assert.Contains("Entrega acordada", html);
    }
}
