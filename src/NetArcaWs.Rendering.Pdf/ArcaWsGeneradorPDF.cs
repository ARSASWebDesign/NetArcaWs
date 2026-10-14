using System.Globalization;
using System.Reflection;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using NetArcaWs.Documents;
using NetArcaWs.Rendering.Shared;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using QRCoder;
using StbImageSharp;

namespace NetArcaWs.Rendering.Pdf;

/// <summary>Renders authorized fiscal documents and local X receipts to PDF in memory.</summary>
public sealed class ArcaWsGeneradorPDF
{
    private static readonly object FontGate = new();
    private readonly DatosEmisor _issuer;
    private readonly RenderingStyle _style;

    public ArcaWsGeneradorPDF(DatosEmisor emisor, string colorPrincipal = "#333333")
    {
        _issuer = emisor ?? throw new ArgumentNullException(nameof(emisor));
        _style = new RenderingStyle(colorPrincipal);
    }

    public string ColorPrincipal => _style.ColorPrincipal;
    public string VersionRenderizado => RenderingStyle.RendererVersion(typeof(ArcaWsGeneradorPDF).Assembly, "pdf-3");

    public byte[] GenerarPDFComprobante(ComprobanteGenerado datos)
    {
        DocumentLayout layout = DocumentRenderValidator.Validate(datos, _issuer);
        DocumentFiscalRules.ValidateConditions(datos, layout);
        ValidatePrintableText(datos);
        string? qrUrl = layout.IsFiscal ? FiscalQrPayload.Create(datos, _issuer) : null;
        EnsureFontResolver();

        ExportDocumentFields? export = ExportDocumentFields.Read(datos.Identidad?.Servicio, datos.SnapshotRequest);
        var document = new Document();
        document.Info.Title = layout.Title;
        Style normal = document.Styles["Normal"]!;
        normal.Font.Name = EmbeddedFontResolver.Family;
        normal.Font.Size = 9;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(3);
        Style heading = document.Styles["Heading1"]!;
        heading.Font.Name = EmbeddedFontResolver.Family;
        heading.Font.Bold = true;
        heading.Font.Size = 14;

        Section section = document.AddSection();
        section.PageSetup = document.DefaultPageSetup.Clone();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromMillimeter(17);
        section.PageSetup.BottomMargin = Unit.FromMillimeter(layout.IsFiscal ? 50 : 15);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.4);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.4);
        AddRunningHeader(section, $"{layout.Title} · Nº {DocumentNumber(datos)}");
        if (layout.IsFiscal) AddCopyLabel(section, "ORIGINAL");
        if (layout.RequiredLegend is not null)
        {
            Paragraph legend = section.AddParagraph($"COMPROBANTE {layout.Letter}    {layout.RequiredLegend}");
            legend.Format.Alignment = ParagraphAlignment.Center;
            legend.Format.Borders.Width = Unit.FromPoint(.5);
            legend.Format.Borders.Distance = Unit.FromPoint(4);
            legend.Format.SpaceAfter = Unit.FromPoint(8);
            legend.Format.Font.Bold = true;
            legend.Format.Font.Size = 10;
        }
        AddIssuer(section, datos, layout, export);
        AddReceiver(section, datos, export);
        AddSupplementalFields(section, datos, export);
        IReadOnlyList<string> associatedDocuments = FiscalAssociatedDocuments.Read(datos);
        if (associatedDocuments.Count != 0)
        {
            section.AddParagraph("Comprobantes asociados", "Heading2");
            foreach (string reference in associatedDocuments) section.AddParagraph(reference);
        }

        if (datos.Detalles.Count > 0) AddDetailTable(section, datos);
        else if (datos.Tipo == TipoComprobanteGenerado.Fiscal) AddSummaryRows(section, datos);
        if (datos.Tipo == TipoComprobanteGenerado.ReciboX)
        {
            if (datos.Pagos.Count != 0)
            {
                section.AddParagraph("Pagos recibidos", "Heading2");
                AddSimpleTable(section, ["Medio", "Importe"], datos.Pagos.Select(p => new[] { p.Medio, Money(p.Importe, p.Moneda) }));
            }
            if (datos.Imputaciones.Count != 0)
            {
                section.AddParagraph("Imputaciones", "Heading2");
                AddSimpleTable(section, ["Referencia", "Importe imputado"], datos.Imputaciones.Select(p => new[] { p.Referencia, Money(p.Importe, p.Moneda) }));
            }
            AddTotalTable(section, "Total recibido", datos.Total, datos.Moneda);
        }
        else
        {
            AddFiscalTotals(section, datos);
            IReadOnlyList<string> legends = DocumentFiscalRules.DocumentLegends(datos);
            if (legends.Count != 0)
            {
                section.AddParagraph("Leyendas", "Heading2");
                foreach (string line in legends) section.AddParagraph(line);
            }
        }
        if (layout.IsFiscal)
        {
            Paragraph authorization = section.Footers.Primary.AddParagraph("Comprobante autorizado por ARCA");
            authorization.Format.Font.Bold = true;
            authorization.Format.Font.Size = 8;
            authorization.Format.Alignment = ParagraphAlignment.Left;
            Paragraph code = section.Footers.Primary.AddParagraph($"CAE: {datos.Resultado!.CodigoAutorizacion} · Vencimiento CAE: {Date(datos.Resultado.Vencimiento!.Value)}");
            code.Format.Font.Size = 8;
            code.Format.RightIndent = Unit.FromMillimeter(44);
            authorization.Format.RightIndent = Unit.FromMillimeter(44);
        }
        else
        {
            Paragraph footerLegend = section.Footers.Primary.AddParagraph(layout.RequiredLegend!);
            footerLegend.Format.Font.Size = 8;
        }
        Paragraph pageCount = section.Footers.Primary.AddParagraph();
        pageCount.Format.Alignment = ParagraphAlignment.Right;
        pageCount.Format.Font.Size = 7;
        if (layout.IsFiscal) pageCount.Format.RightIndent = Unit.FromMillimeter(44);
        pageCount.AddText("Página "); pageCount.AddPageField(); pageCount.AddText(" de "); pageCount.AddNumPagesField();

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var output = new MemoryStream();
        renderer.PdfDocument!.Save(output, closeStream: false);
        byte[] result = output.ToArray();
        return OverlayImages(result, null, qrUrl, 0);
    }

    public byte[] GenerarPDFComprobante(RemitoGenerado remito)
    {
        RemitoValidator.Validate(remito);
        if (remito.CuitEmisor != _issuer.Identificacion)
            throw new ArgumentException("El remito pertenece a un CUIT emisor distinto del perfil configurado.", nameof(remito));
        _ = new Remito(_issuer).GenerarComprobante(new DatosRemito(remito.Clase, remito.FechaEmision, remito.PuntoVenta,
            remito.Numero, remito.Receptor, remito.Destino, remito.Mercaderia, remito.EsOriginal, remito.Transportista,
            remito.Cai, remito.PrimerNumero, remito.UltimoNumero, remito.Vencimiento, remito.Imprenta,
            remito.EsAutoimpresor, remito.AmbitoInterno));
        ValidatePrintableText(remito);
        EnsureFontResolver();
        var document = CreateDocument();
        Section section = document.AddSection();
        ConfigurePage(document, section, false);
        AddRunningHeader(section, $"Remito {remito.Clase} · Nº {remito.PuntoVenta:00000}-{remito.Numero:00000000}");
        AddCopyLabel(section, remito.EsOriginal ? "ORIGINAL" : "DUPLICADO");
        Paragraph legend = section.AddParagraph($"REMITO {remito.Clase}    {remito.Leyenda}");
        legend.Format.Alignment = ParagraphAlignment.Center;
        legend.Format.Borders.Width = Unit.FromPoint(.5);
        legend.Format.Borders.Distance = Unit.FromPoint(4);
        legend.Format.SpaceAfter = Unit.FromPoint(8);
        legend.Format.Font.Bold = true;
        AddRemitoHeader(section, remito);
        AddRemitoReceiver(section, remito);
        var goods = section.AddTable();
        StyleHeader(goods, ["Descripción", "Unidad", "Cantidad"], ["10.8", "3.8", "3.8"]);
        foreach (LineaRemito item in remito.Mercaderia)
        {
            Row row = goods.AddRow();
            row.Cells[0].AddParagraph(item.Descripcion);
            row.Cells[1].AddParagraph(item.Unidad);
            row.Cells[2].AddParagraph(Number(item.Cantidad));
            row.Cells[2].Format.Alignment = ParagraphAlignment.Right;
        }
        AddRemitoDetails(section, remito);
        Paragraph reception = section.AddParagraph("Recibí conforme la mercadería detallada.");
        reception.Format.SpaceBefore = Unit.FromPoint(30);
        reception.Format.Borders.Top.Width = Unit.FromPoint(.5);
        reception.Format.Borders.Top.Color = Colors.Gray;
        reception.Format.LeftIndent = Unit.FromCentimeter(9);
        reception.Format.Font.Size = 8;
        AddPageNumber(section, false);
        return OverlayImages(Render(document), null, null, 0);
    }

    private Document CreateDocument()
    {
        var document = new Document();
        document.Info.Title = "Remito";
        Style normal = document.Styles["Normal"]!;
        normal.Font.Name = EmbeddedFontResolver.Family;
        normal.Font.Size = 9;
        normal.Font.Color = Colors.Black;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(3);
        foreach (string name in new[] { "Heading1", "Heading2" })
        {
            Style heading = document.Styles[name]!;
            heading.Font.Name = EmbeddedFontResolver.Family;
            heading.Font.Bold = true;
            heading.Font.Size = name == "Heading1" ? 14 : 10;
            heading.ParagraphFormat.SpaceBefore = Unit.FromPoint(8);
            heading.ParagraphFormat.SpaceAfter = Unit.FromPoint(4);
        }
        return document;
    }

    private static void ConfigurePage(Document document, Section section, bool fiscal)
    {
        section.PageSetup = document.DefaultPageSetup.Clone();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromMillimeter(17);
        section.PageSetup.BottomMargin = Unit.FromMillimeter(fiscal ? 35 : 15);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.4);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.4);
    }

    private void AddRunningHeader(Section section, string value)
    {
        Paragraph header = section.Headers.Primary.AddParagraph(value);
        header.Format.Font.Size = 7;
        header.Format.Font.Color = Colors.Gray;
        header.Format.Alignment = ParagraphAlignment.Right;
        header.Format.Borders.Bottom.Width = Unit.FromPoint(.3);
        header.Format.Borders.Bottom.Color = Colors.LightGray;
    }

    private static void AddCopyLabel(Section section, string value)
    {
        Paragraph copy = section.AddParagraph(value);
        copy.Format.Alignment = ParagraphAlignment.Center;
        copy.Format.Font.Bold = true;
        copy.Format.Font.Size = 8;
        copy.Format.Font.Color = Colors.Gray;
        copy.Format.SpaceAfter = Unit.FromPoint(5);
    }

    private static void AddPageNumber(Section section, bool fiscal)
    {
        Paragraph page = section.Footers.Primary.AddParagraph();
        page.Format.Alignment = ParagraphAlignment.Right;
        page.Format.Font.Size = 7;
        page.AddText("Página "); page.AddPageField(); page.AddText(" de "); page.AddNumPagesField();
        if (fiscal) page.Format.RightIndent = Unit.FromMillimeter(44);
    }

    private void StyleHeader(Table table, string[] headings, string[]? widths = null)
    {
        if (widths is not null)
        {
            foreach (string width in widths) table.AddColumn(Unit.FromCentimeter(double.Parse(width, CultureInfo.InvariantCulture)));
        }
        Row row = table.AddRow();
        StyleHeader(row, headings);
    }

    private void StyleHeader(Row row, string[] headings)
    {
        row.HeadingFormat = true;
        row.Shading.Color = ParseColor(_style.ColorPrincipal);
        for (int index = 0; index < headings.Length; index++)
        {
            Paragraph cell = row.Cells[index].AddParagraph(headings[index]);
            cell.Format.Font.Bold = true;
            cell.Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        }
    }

    private void AddReceiver(Section section, ComprobanteGenerado data, ExportDocumentFields? export)
    {
        var table = section.AddTable();
        table.Borders.Color = Colors.LightGray; table.Borders.Width = Unit.FromPoint(.3);
        table.AddColumn(Unit.FromCentimeter(9.1)); table.AddColumn(Unit.FromCentimeter(9.1));
        Row header = table.AddRow(); header.Shading.Color = ParseColor(_style.ColorPrincipal);
        header.Cells[0].Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        header.Cells[1].Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        header.Cells[0].AddParagraph("Datos del receptor").Format.Font.Bold = true;
        header.Cells[1].AddParagraph("Condición tributaria").Format.Font.Bold = true;
        Row body = table.AddRow();
        body.Shading.Color = Colors.White;
        AddLine(body.Cells[0], "Nombre / razón social", data.Receptor.Nombre);
        if (!string.IsNullOrWhiteSpace(data.Receptor.Domicilio)) AddLine(body.Cells[0], "Domicilio", data.Receptor.Domicilio);
        if (!string.IsNullOrWhiteSpace(data.Receptor.Telefono)) AddLine(body.Cells[0], "Teléfono", data.Receptor.Telefono);
        if (!string.IsNullOrWhiteSpace(data.Receptor.Email)) AddLine(body.Cells[0], "Email", data.Receptor.Email);
        string condition = DocumentFiscalRules.ReceiverCondition(data);
        if (!string.IsNullOrWhiteSpace(condition)) AddLine(body.Cells[1], DocumentFiscalRules.ReceiverConditionLabel(data).TrimEnd(':', ' '), condition);
        string? id = export?.ForeignTaxId ?? data.Receptor.IdentificacionFiscal;
        if (string.IsNullOrWhiteSpace(id) && data.Receptor.Identificacion > 0) id = data.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(id)) AddLine(body.Cells[1], export is null ? "Identificación" : "ID impositivo extranjero", id);
        if (export?.CuitPais is string cuitPais) AddLine(body.Cells[1], "CUIT país", cuitPais);
        if (export?.DestinationCountryCode is string country) AddLine(body.Cells[1], "País de destino (código)", country);
        if (data.FechaPago is DateOnly due) AddLine(body.Cells[1], "Vencimiento de pago", Date(due));
    }

    private void AddSupplementalFields(Section section, ComprobanteGenerado data, ExportDocumentFields? export)
    {
        if (export is null) return;
        var left = new List<(string Label, string Value)> { ("Moneda", CurrencyDisplay.Label(export.CurrencyCode)) };
        var right = new List<(string Label, string Value)>();
        if (export.ExchangeRate is string rate) left.Add(("Cotización", ExportExchangeRate(rate)));
        if (export.ExportCondition is { Length: > 0 } condition) left.Add(("Tipo de exportación", condition));
        if (export.PaymentMethod is { Length: > 0 } payment) left.Add(("Forma de pago", payment));
        if (FormatSnapshotDate(export.PaymentDate) is string date) right.Add(("Fecha de pago", date));
        if (export.Incoterms is { Length: > 0 } incoterm) right.Add(("Incoterms", string.IsNullOrWhiteSpace(export.IncotermsDescription) ? incoterm : $"{incoterm} · {export.IncotermsDescription}"));
        var table = section.AddTable();
        table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(9.1)); table.AddColumn(Unit.FromCentimeter(9.1));
        Row row = table.AddRow(); row.Shading.Color = Colors.White;
        foreach ((string label, string value) in left) AddLine(row.Cells[0], label, value);
        foreach ((string label, string value) in right) AddLine(row.Cells[1], label, value);
        if (export.CommercialObservations is { Length: > 0 } commercial) AddLine(section, "Observaciones comerciales", commercial);
        if (export.Observations is { Length: > 0 } observations) AddLine(section, "Observaciones", observations);
    }

    private static string ExportExchangeRate(string rate) => decimal.TryParse(rate, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
        ? "$" + value.ToString("N2", CultureInfo.GetCultureInfo("es-AR")) : rate;

    private void AddRemitoHeader(Section section, RemitoGenerado remito)
    {
        var table = section.AddTable();
        table.Borders.Color = ParseColor(_style.ColorPrincipal); table.Borders.Width = Unit.FromPoint(.2);
        table.AddColumn(Unit.FromCentimeter(7.4)); table.AddColumn(Unit.FromCentimeter(2.2)); table.AddColumn(Unit.FromCentimeter(8.6));
        Row row = table.AddRow(); row.Shading.Color = ParseColor(_style.ColorPrincipal); row.Cells[1].Shading.Color = Colors.White; row.VerticalAlignment = VerticalAlignment.Center;
        row.Cells[0].Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        row.Cells[1].Format.Font.Color = ParseColor(_style.ColorPrincipal);
        row.Cells[2].Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        AddBrandContent(row.Cells[0]);
        Paragraph letter = row.Cells[1].AddParagraph(remito.Clase.ToString()); letter.Format.Font.Bold = true; letter.Format.Font.Size = 24; letter.Format.Alignment = ParagraphAlignment.Center;
        if (remito.Clase == ClaseRemito.R) { Paragraph code = row.Cells[1].AddParagraph("COD. 091"); code.Format.Font.Size = 7; code.Format.Alignment = ParagraphAlignment.Center; }
        Paragraph meta = row.Cells[2].AddParagraph($"Remito {remito.Clase}"); meta.Format.Font.Bold = true; meta.Format.Font.Size = 14; meta.Format.Alignment = ParagraphAlignment.Right;
        Paragraph remitoDetails = row.Cells[2].AddParagraph($"Nº {remito.PuntoVenta:00000}-{remito.Numero:00000000}");
        remitoDetails.Format.Alignment = ParagraphAlignment.Right; remitoDetails.Format.Font.Size = 9;
        remitoDetails.AddLineBreak(); remitoDetails.AddText($"Fecha: {Date(remito.FechaEmision)}");
        Paragraph issuerFiscal = row.Cells[2].AddParagraph(); issuerFiscal.Format.Alignment = ParagraphAlignment.Right; issuerFiscal.Format.Font.Size = 7;
        issuerFiscal.Format.Borders.Top.Width = Unit.FromPoint(.3); issuerFiscal.Format.Borders.Top.Color = Colors.LightGray;
        issuerFiscal.AddText($"CUIT: {_issuer.Identificacion} · {_issuer.CondicionTributaria}");
        if (!string.IsNullOrWhiteSpace(_issuer.IngresosBrutos)) { issuerFiscal.AddLineBreak(); issuerFiscal.AddText($"Ingresos Brutos: {_issuer.IngresosBrutos}"); }
        if (_issuer.InicioActividades is DateOnly start) { issuerFiscal.AddLineBreak(); issuerFiscal.AddText($"Inicio de actividades: {Date(start)}"); }
    }

    private void AddRemitoReceiver(Section section, RemitoGenerado remito)
    {
        var table = section.AddTable(); table.Borders.Color = Colors.LightGray; table.Borders.Width = Unit.FromPoint(.3);
        table.AddColumn(Unit.FromCentimeter(9.1)); table.AddColumn(Unit.FromCentimeter(9.1));
        Row heading = table.AddRow(); heading.Shading.Color = ParseColor(_style.ColorPrincipal);
        heading.Cells[0].Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        heading.Cells[1].Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        heading.Cells[0].AddParagraph("Datos del receptor").Format.Font.Bold = true;
        heading.Cells[1].AddParagraph("Traslado").Format.Font.Bold = true;
        Row body = table.AddRow(); body.Shading.Color = Colors.White;
        AddLine(body.Cells[0], "Nombre / razón social", remito.Receptor.Nombre);
        AddLine(body.Cells[0], "CUIT", remito.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture));
        AddLine(body.Cells[0], "Domicilio", remito.Receptor.Domicilio ?? "");
        AddLine(body.Cells[0], "Condición tributaria", remito.Receptor.CondicionTributaria ?? "");
        if (!string.IsNullOrWhiteSpace(remito.Receptor.Telefono)) AddLine(body.Cells[0], "Teléfono", remito.Receptor.Telefono);
        if (!string.IsNullOrWhiteSpace(remito.Receptor.Email)) AddLine(body.Cells[0], "Email", remito.Receptor.Email);
        AddLine(body.Cells[1], "Destino", remito.Destino);
        if (remito.Transportista is { } carrier)
        {
            AddLine(body.Cells[1], "Transportista", carrier.RazonSocial);
            AddLine(body.Cells[1], "CUIT transportista", carrier.Cuit.ToString(CultureInfo.InvariantCulture));
            AddLine(body.Cells[1], "Domicilio transportista", carrier.Domicilio);
        }
        if (remito.AmbitoInterno is string scope) AddLine(body.Cells[1], "Ámbito de circulación interna", scope);
    }

    private static void AddRemitoDetails(Section section, RemitoGenerado remito)
    {
        if (remito.Clase != ClaseRemito.R) return;
        section.AddParagraph("Datos del talonario R", "Heading2");
        AddLine(section, "CAI declarado", remito.Cai!);
        AddLine(section, "Vencimiento de talonario", Date(remito.Vencimiento!.Value));
        AddLine(section, "Rango autorizado declarado", $"{remito.PrimerNumero:00000000}–{remito.UltimoNumero:00000000}");
        if (remito.EsAutoimpresor) AddLine(section, "Condición de impresión", "Autoimpresor");
        if (remito.Imprenta is { } printer)
        {
            AddLine(section, "Imprenta", printer.RazonSocial);
            AddLine(section, "CUIT imprenta", printer.Cuit.ToString(CultureInfo.InvariantCulture));
            AddLine(section, "Habilitación de imprenta", printer.NumeroHabilitacion);
            AddLine(section, "Fecha de impresión", Date(printer.FechaImpresion));
        }
    }

    private static string? FormatSnapshotDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateOnly.TryParseExact(value, ["yyyyMMdd", "yyyy-MM-dd", "dd/MM/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
            ? Date(date) : value;
    }

    private void AddLine(Cell cell, string label, string value)
    {
        Paragraph p = cell.AddParagraph();
        p.AddFormattedText(label + ": ", TextFormat.Bold);
        p.AddText(value);
    }

    private static Color ParseColor(string hex) => Color.FromRgb(
        Convert.ToByte(hex.Substring(1, 2), 16), Convert.ToByte(hex.Substring(3, 2), 16), Convert.ToByte(hex.Substring(5, 2), 16));

    private byte[] Render(Document document)
    {
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var output = new MemoryStream();
        renderer.PdfDocument!.Save(output, closeStream: false);
        return output.ToArray();
    }

    private void AddIssuer(Section section, ComprobanteGenerado data, DocumentLayout layout, ExportDocumentFields? export)
    {
        var table = section.AddTable();
        table.Borders.Width = Unit.FromPoint(.2);
        table.Borders.Color = ParseColor(_style.ColorPrincipal);
        table.AddColumn(Unit.FromCentimeter(7.4));
        table.AddColumn(Unit.FromCentimeter(2.2));
        table.AddColumn(Unit.FromCentimeter(8.6));
        Row row = table.AddRow();
        row.Shading.Color = ParseColor(_style.ColorPrincipal);
        row.Cells[1].Shading.Color = Colors.White;
        row.VerticalAlignment = VerticalAlignment.Center;
        row.Cells[0].Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        row.Cells[1].Format.Font.Color = ParseColor(_style.ColorPrincipal);
        row.Cells[2].Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        AddBrandContent(row.Cells[0]);
        Paragraph code = row.Cells[1].AddParagraph(layout.Letter);
        code.Format.Font.Bold = true;
            code.Format.Font.Size = 24;
        code.Format.Alignment = ParagraphAlignment.Center;
        if (layout.IsFiscal)
        {
            Paragraph typeCode = row.Cells[1].AddParagraph($"COD. {data.Identidad!.Tipo:000}");
            typeCode.Format.Alignment = ParagraphAlignment.Center;
            typeCode.Format.Font.Size = 7;
        }
        Paragraph title = row.Cells[2].AddParagraph(layout.Title);
        title.Format.Font.Bold = true;
        title.Format.Font.Size = 14;
        title.Format.Alignment = ParagraphAlignment.Right;
        string number = data.Identidad is null ? data.NumeroInterno ?? string.Empty : $"{data.Identidad.PuntoVenta:00000}-{data.Identidad.Numero:00000000}";
        Paragraph details = row.Cells[2].AddParagraph($"Nº {number}");
        details.Format.Alignment = ParagraphAlignment.Right; details.Format.Font.Size = 9;
        details.AddLineBreak(); details.AddText($"Fecha: {(data.Fecha is DateOnly date ? Date(date) : "—")}");
        if (data.FechaDesde is DateOnly from) { details.AddLineBreak(); details.AddText($"Período desde: {Date(from)}"); }
        if (data.FechaHasta is DateOnly through) { details.AddLineBreak(); details.AddText($"Período hasta: {Date(through)}"); }
        if (data.FechaPago is DateOnly due) { details.AddLineBreak(); details.AddText($"Vencimiento de pago: {Date(due)}"); }
        if (export is null && !string.IsNullOrWhiteSpace(data.Moneda)) { details.AddLineBreak(); details.AddText($"Moneda: {CurrencyDisplay.Label(data.Moneda)}"); }
        if (export is null && !CurrencyDisplay.IsPeso(data.Moneda) && data.Cotizacion > 0) { details.AddLineBreak(); details.AddText($"Cotización: {FullDecimal(data.Cotizacion)} ARS por {CurrencyDisplay.Code(data.Moneda)}"); }
        Paragraph issuerFiscal = row.Cells[2].AddParagraph();
        issuerFiscal.Format.Alignment = ParagraphAlignment.Right;
        issuerFiscal.Format.Font.Size = 7;
        issuerFiscal.Format.Borders.Top.Width = Unit.FromPoint(.3);
        issuerFiscal.Format.Borders.Top.Color = Colors.LightGray;
        issuerFiscal.AddText($"CUIT: {_issuer.Identificacion} · {_issuer.CondicionTributaria}");
        if (!string.IsNullOrWhiteSpace(_issuer.IngresosBrutos)) { issuerFiscal.AddLineBreak(); issuerFiscal.AddText($"Ingresos Brutos: {_issuer.IngresosBrutos}"); }
        if (_issuer.InicioActividades is DateOnly start) { issuerFiscal.AddLineBreak(); issuerFiscal.AddText($"Inicio de actividades: {Date(start)}"); }
        section.AddParagraph();
    }

    private void AddBrandContent(Cell cell)
    {
        if (_issuer.LogoPng is { Length: > 0 } logo)
        {
            ImageResult dimensions = ImageResult.FromMemory(logo, ColorComponents.RedGreenBlueAlpha);
            double aspect = (double)dimensions.Width / dimensions.Height;
            var paragraph = cell.AddParagraph();
            paragraph.Format.SpaceAfter = Unit.FromPoint(3);
            MigraDoc.DocumentObjectModel.Shapes.Image image = paragraph.AddImage("base64:" + Convert.ToBase64String(logo));
            image.LockAspectRatio = true;
            if (aspect <= 74d / 22d) image.Height = Unit.FromMillimeter(22);
            else image.Width = Unit.FromMillimeter(74);
        }
        Paragraph legalName = cell.AddParagraph(_issuer.RazonSocial);
        legalName.Format.Font.Bold = true;
        legalName.Format.Font.Size = 13;
        if (!string.IsNullOrWhiteSpace(_issuer.NombreFantasia))
        {
            Paragraph tradeName = cell.AddParagraph(_issuer.NombreFantasia);
            tradeName.Format.Font.Bold = true;
            tradeName.Format.Font.Size = 11;
        }
        if (!string.IsNullOrWhiteSpace(_issuer.Domicilio))
        {
            Paragraph address = cell.AddParagraph(_issuer.Domicilio);
            address.Format.Font.Bold = false;
            address.Format.Font.Size = 9;
        }
    }

    private void AddDetailTable(Section section, ComprobanteGenerado data)
    {
        var table = section.AddTable();
        table.Borders.Color = Colors.LightGray;
        table.Borders.Width = Unit.FromPoint(0.35);
        table.AddColumn(Unit.FromCentimeter(2.1)); table.AddColumn(Unit.FromCentimeter(8.3));
        table.AddColumn(Unit.FromCentimeter(4.0)); table.AddColumn(Unit.FromCentimeter(3.8));
        Row header = table.AddRow(); StyleHeader(header, ["Cant.", "Descripción", "P.Unit", "Subtotal"]);
        foreach (LineaComprobante line in data.Detalles)
        {
            Row row = table.AddRow();
            row.Shading.Color = Colors.White;
            row.Cells[0].AddParagraph(line.Cantidad is decimal qty ? Number(qty) : "—");
            row.Cells[1].AddParagraph(line.DescripcionPresentacion ?? line.Descripcion);
            row.Cells[2].AddParagraph(line.PrecioUnitario is decimal price ? Money(price, data.Moneda) : "—");
            row.Cells[3].AddParagraph(Money(line.Importe, data.Moneda));
            row.Cells[0].Format.Alignment = row.Cells[2].Format.Alignment = row.Cells[3].Format.Alignment = ParagraphAlignment.Right;
        }
    }

    private void AddFiscalSummary(Section section, ComprobanteGenerado data)
    {
        _ = section;
        _ = data;
    }

    private void AddSummaryRows(Section section, ComprobanteGenerado data)
    {
        var rows = new List<string[]>();
        foreach ((string label, decimal? value) in new[] { ("Neto gravado", data.Neto), ("No gravado", data.NoGravado), ("Exento", data.Exento) })
            if (value is decimal amount && amount != 0) rows.Add(["—", label, "—", Money(amount, data.Moneda)]);
        if (rows.Count == 0 && data.Impuestos.Count == 0) rows.Add(["—", "Importe total", "—", Money(data.Total, data.Moneda)]);
        if (rows.Count != 0)
        {
            var table = section.AddTable();
            table.Borders.Color = Colors.LightGray; table.Borders.Width = Unit.FromPoint(.3);
            table.AddColumn(Unit.FromCentimeter(2.1)); table.AddColumn(Unit.FromCentimeter(8.3));
            table.AddColumn(Unit.FromCentimeter(4.0)); table.AddColumn(Unit.FromCentimeter(3.8));
            Row header = table.AddRow(); StyleHeader(header, ["Cant.", "Descripción", "P.Unit", "Subtotal"]);
            foreach (string[] values in rows)
            {
                Row row = table.AddRow(); row.Shading.Color = Colors.White;
                for (int index = 0; index < values.Length; index++) row.Cells[index].AddParagraph(values[index]);
                row.Cells[0].Format.Alignment = row.Cells[2].Format.Alignment = row.Cells[3].Format.Alignment = ParagraphAlignment.Right;
            }
        }
    }

    private void AddFiscalTotals(Section section, ComprobanteGenerado data)
    {
        bool consumer = data.CodigoCondicionIvaReceptor == 5;
        var table = section.AddTable(); table.Borders.Color = Colors.LightGray; table.Borders.Width = Unit.FromPoint(.3);
        table.AddColumn(Unit.FromCentimeter(7.4)); table.AddColumn(Unit.FromCentimeter(3.8));
        table.Rows.LeftIndent = Unit.FromCentimeter(7.0);
        Row total = table.AddRow(); total.Shading.Color = ParseColor(_style.ColorPrincipal);
        foreach (Cell? cell in total.Cells) if (cell is not null) cell.Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        Paragraph totalLabel = total.Cells[0].AddParagraph("Total"); totalLabel.Format.Font.Bold = true; totalLabel.Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        Paragraph totalAmount = total.Cells[1].AddParagraph(Money(data.Total, data.Moneda)); totalAmount.Format.Font.Bold = true; totalAmount.Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        total.Cells[1].Format.Alignment = ParagraphAlignment.Right;
        bool hasVat = false;
        foreach (ImpuestoComprobante tax in data.Impuestos)
        {
            string? rate = tax.Alicuota is decimal explicitRate
                ? $"{FullDecimal(explicitRate)}%"
                : tax.Tipo == TipoImpuestoComprobante.Iva ? DocumentLayout.IvaRate(tax.Codigo) : null;
            if (tax.Tipo == TipoImpuestoComprobante.Iva && rate is null)
                throw new NotSupportedException($"No hay una alícuota oficial disponible para el código IVA {tax.Codigo ?? "(vacío)"}.");
            string label = tax.Tipo == TipoImpuestoComprobante.Iva
                ? $"IVA {rate}{(consumer ? " Contenido" : "")}"
                : $"{tax.Nombre}{(rate is null ? "" : $" {rate}")}{(tax.Clasificacion is null ? "" : $" — {TaxClassification(tax.Clasificacion)}")}";
            if (tax.Tipo == TipoImpuestoComprobante.Iva) hasVat = true;
            AddCompactTaxRow(table, label, tax.BaseImponible, tax.Importe, tax.Moneda);
        }
        if (!hasVat && data.Iva is decimal aggregateVat)
            AddCompactTaxRow(table, consumer ? DocumentFiscalRules.ConsumerIvaLabel : "IVA", null, aggregateVat, data.Moneda);
        bool hasNationalIndirect = data.Impuestos.Any(tax => tax.Tipo == TipoImpuestoComprobante.OtroTributo && tax.Clasificacion == "NacionalIndirecto");
        if (consumer && !hasNationalIndirect)
            AddCompactTaxRow(table, DocumentFiscalRules.ConsumerOtherTaxesLabel, null, 0m, data.Moneda);
        if (consumer)
        {
            Paragraph legend = section.AddParagraph(DocumentFiscalRules.ConsumerTransparencyTitle);
            legend.Format.Alignment = ParagraphAlignment.Right;
            legend.Format.Font.Size = 8;
        }
    }

    private static void AddCompactTaxRow(Table table, string label, decimal? basis, decimal amount, string currency)
    {
        Row row = table.AddRow(); row.Shading.Color = Colors.White;
        Paragraph name = row.Cells[0].AddParagraph(label);
        if (basis is decimal baseAmount)
        {
            Paragraph baseText = row.Cells[0].AddParagraph($"({Money(baseAmount, currency)})");
            baseText.Format.Font.Size = 7;
            baseText.Format.Font.Color = Colors.Gray;
        }
        row.Cells[1].AddParagraph(Money(amount, currency));
        row.Cells[1].Format.Alignment = ParagraphAlignment.Right;
    }

    private static string TaxClassification(string value) => value switch
    {
        "NacionalIndirecto" => "impuesto nacional indirecto",
        "NoNacionalIndirecto" => "impuesto no nacional indirecto",
        "NoIndirecto" => "no indirecto",
        _ => value
    };

    private void AddTotalTable(Section section, string label, decimal amount, string currency)
    {
        var table = section.AddTable(); table.Borders.Width = 0;
        table.AddColumn(Unit.FromCentimeter(6)); table.AddColumn(Unit.FromCentimeter(5));
        table.Rows.LeftIndent = Unit.FromCentimeter(7.2);
        Row row = table.AddRow(); row.Shading.Color = ParseColor(_style.ColorPrincipal);
        foreach (Cell? cell in row.Cells) if (cell is not null) cell.Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        Paragraph labelText = row.Cells[0].AddParagraph(label); labelText.Format.Font.Bold = true; labelText.Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        Paragraph amountText = row.Cells[1].AddParagraph(Money(amount, currency)); amountText.Format.Font.Bold = true; amountText.Format.Font.Color = ParseColor(_style.TextOnPrincipal);
        row.Cells[1].Format.Alignment = ParagraphAlignment.Right;
        row.Cells[0].Format.Alignment = ParagraphAlignment.Left;
    }

    private void AddSimpleTable(Section section, string[] headers, IEnumerable<string[]> rows)
    {
        var table = section.AddTable(); table.Borders.Color = Colors.LightGray; table.Borders.Width = Unit.FromPoint(0.3);
        double totalWidth = headers.Length == 2 ? 11.4 : 18.2;
        if (headers.Length == 2)
        {
            table.AddColumn(Unit.FromCentimeter(12));
            table.AddColumn(Unit.FromCentimeter(6.2));
        }
        else
        {
            double colWidth = totalWidth / headers.Length;
            foreach (string _ in headers) table.AddColumn(Unit.FromCentimeter(colWidth));
        }
        Row header = table.AddRow(); header.HeadingFormat = true; StyleHeader(header, headers);
        foreach (string[] values in rows)
        {
            Row row = table.AddRow(); row.Shading.Color = Colors.White;
            for (int index = 0; index < values.Length; index++)
            {
                row.Cells[index].AddParagraph(values[index]);
                if (index > 0) row.Cells[index].Format.Alignment = ParagraphAlignment.Right;
            }
        }
    }

    private static void AddLine(Section section, string label, string value)
    {
        Paragraph p = section.AddParagraph(); p.AddFormattedText(label + (label.EndsWith(':') ? " " : ": "), TextFormat.Bold); p.AddText(value);
    }

    private static byte[] OverlayImages(byte[] input, byte[]? logo, string? qrUrl, double logoTopMillimeter)
    {
        using var source = new MemoryStream(input);
        PdfDocument document = PdfReader.Open(source, PdfDocumentOpenMode.Modify);
        if (logo is { Length: > 0 })
        {
            PdfPage firstPage = document.Pages[0];
            using XGraphics graphics = XGraphics.FromPdfPage(firstPage, XGraphicsPdfPageOptions.Append);
            using var logoStream = new MemoryStream(ToBmp(logo));
            using XImage image = XImage.FromStream(logoStream);
            double maxWidth = XUnit.FromMillimeter(35).Point;
            double maxHeight = XUnit.FromMillimeter(22).Point;
            double scale = Math.Min(maxWidth / image.PixelWidth, maxHeight / image.PixelHeight);
            double width = image.PixelWidth * scale, height = image.PixelHeight * scale;
            graphics.DrawImage(image, XUnit.FromMillimeter(15).Point, XUnit.FromMillimeter(logoTopMillimeter).Point, width, height);
        }
        if (qrUrl is not null)
        {
            using var qrData = QRCodeGenerator.GenerateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
            byte[] png = new PngByteQRCode(qrData).GetGraphic(5);
            foreach (PdfPage page in document.Pages)
            {
                using XGraphics graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                using var qrStream = new MemoryStream(ToBmp(png));
                using XImage image = XImage.FromStream(qrStream);
                graphics.DrawImage(image, page.Width.Point - XUnit.FromMillimeter(48).Point, page.Height.Point - XUnit.FromMillimeter(48).Point, XUnit.FromMillimeter(32).Point, XUnit.FromMillimeter(32).Point);
            }
        }
        using var output = new MemoryStream();
        document.Save(output, closeStream: false);
        document.Dispose();
        return output.ToArray();
    }

    private static void EnsureFontResolver()
    {
        lock (FontGate)
        {
            IFontResolver? configured = GlobalFontSettings.FontResolver;
            if (configured is null) GlobalFontSettings.FontResolver = EmbeddedFontResolver.Instance;
            else if (!ReferenceEquals(configured, EmbeddedFontResolver.Instance))
                throw new InvalidOperationException("PDFsharp ya tiene otro FontResolver global. Configure Noto Sans antes de usar NetArcaWs.Rendering.Pdf.");
        }
    }

    private static byte[] ToBmp(byte[] png)
    {
        ImageResult decoded = ImageResult.FromMemory(png, ColorComponents.RedGreenBlueAlpha);
        int stride = (decoded.Width * 3 + 3) & ~3;
        int pixelBytes = checked(stride * decoded.Height);
        using var bmp = new MemoryStream(54 + pixelBytes);
        using var writer = new BinaryWriter(bmp);
        writer.Write((byte)'B'); writer.Write((byte)'M'); writer.Write(54 + pixelBytes); writer.Write(0); writer.Write(54);
        writer.Write(40); writer.Write(decoded.Width); writer.Write(decoded.Height); writer.Write((short)1); writer.Write((short)24);
        writer.Write(0); writer.Write(pixelBytes); writer.Write(2835); writer.Write(2835); writer.Write(0); writer.Write(0);
        byte[] row = new byte[stride];
        for (int y = decoded.Height - 1; y >= 0; y--)
        {
            Array.Clear(row);
            for (int x = 0; x < decoded.Width; x++)
            {
                int source = (y * decoded.Width + x) * 4;
                int target = x * 3;
                byte alpha = decoded.Data[source + 3];
                row[target] = BlendWhite(decoded.Data[source + 2], alpha);
                row[target + 1] = BlendWhite(decoded.Data[source + 1], alpha);
                row[target + 2] = BlendWhite(decoded.Data[source], alpha);
            }
            writer.Write(row);
        }
        writer.Flush();
        return bmp.ToArray();
    }

    private static byte BlendWhite(byte channel, byte alpha) => (byte)((channel * alpha + 255 * (255 - alpha)) / 255);

    private static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    private static string Number(decimal value) => value.ToString("N2", CultureInfo.GetCultureInfo("es-AR"));
    private static string FullDecimal(decimal value) => value.ToString("#,##0.########################", CultureInfo.GetCultureInfo("es-AR"));
    private static string Money(decimal value, string currency) => CurrencyDisplay.Code(currency) == "ARS"
        ? $"${Number(value)}" : $"{Number(value)} {CurrencyDisplay.Code(currency)}";
    private static string CurrencyName(string code) => CurrencyDisplay.Name(code);
    private static bool IsPesoCurrency(string code) => CurrencyDisplay.IsPeso(code);
    private static string DocumentNumber(ComprobanteGenerado data) => data.Identidad is null ? data.NumeroInterno ?? string.Empty : $"{data.Identidad.PuntoVenta:00000}-{data.Identidad.Numero:00000000}";

    private void ValidatePrintableText(ComprobanteGenerado document)
    {
        var values = new List<string?> { _issuer.RazonSocial, _issuer.NombreFantasia, _issuer.Domicilio, _issuer.CondicionTributaria,
            _issuer.IngresosBrutos, document.Receptor.Nombre, document.Receptor.Domicilio, document.Receptor.Telefono, document.Receptor.Email, document.Receptor.CondicionTributaria,
            document.Receptor.IdentificacionFiscal, document.NumeroInterno };
        values.AddRange(document.Detalles.SelectMany(line => new[] { line.Descripcion, line.DescripcionPresentacion, line.Codigo }));
        values.AddRange(document.Impuestos.Select(tax => tax.Nombre));
        values.AddRange(document.Leyendas);
        values.AddRange(document.Pagos.Select(payment => payment.Medio));
        values.AddRange(document.Imputaciones.Select(allocation => allocation.Referencia));
        foreach (string? value in values)
            if (value is not null && value.Any(character => char.IsControl(character) && character is not ('\r' or '\n' or '\t')))
                throw new ArgumentException("El comprobante contiene texto con caracteres de control no imprimibles.", nameof(document));
    }

    private void ValidatePrintableText(RemitoGenerado remito)
    {
        var values = new List<string?> { _issuer.RazonSocial, _issuer.NombreFantasia, _issuer.Domicilio,
            _issuer.CondicionTributaria, _issuer.IngresosBrutos, remito.Receptor.Nombre, remito.Receptor.Domicilio,
            remito.Receptor.CondicionTributaria, remito.Destino, remito.AmbitoInterno, remito.Cai,
            remito.Transportista?.RazonSocial, remito.Transportista?.Domicilio, remito.Imprenta?.RazonSocial,
            remito.Imprenta?.NumeroHabilitacion };
        values.AddRange(remito.Mercaderia.SelectMany(item => new[] { item.Descripcion, item.Unidad }));
        foreach (string? value in values)
            if (value is not null && value.Any(character => char.IsControl(character) && character is not ('\r' or '\n' or '\t')))
                throw new ArgumentException("El remito contiene texto con caracteres de control no imprimibles.", nameof(remito));
    }
}
