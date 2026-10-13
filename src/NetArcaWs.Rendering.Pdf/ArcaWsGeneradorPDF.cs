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

    public ArcaWsGeneradorPDF(DatosEmisor emisor) => _issuer = emisor ?? throw new ArgumentNullException(nameof(emisor));

    public byte[] GenerarPDFComprobante(ComprobanteGenerado datos)
    {
        DocumentLayout layout = DocumentRenderValidator.Validate(datos, _issuer);
        DocumentFiscalRules.ValidateConditions(datos, layout);
        ValidatePrintableText(datos);
        string? qrUrl = layout.IsFiscal ? FiscalQrPayload.Create(datos, _issuer) : null;
        EnsureFontResolver();

        var document = new Document();
        document.Info.Title = $"{layout.Title} {layout.Letter}";
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
        section.PageSetup.TopMargin = Unit.FromMillimeter(_issuer.LogoPng is null ? 24 : 38);
        section.PageSetup.BottomMargin = Unit.FromMillimeter(layout.IsFiscal ? 54 : 14);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.4);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.4);
        Paragraph runningHeader = section.Headers.Primary.AddParagraph($"{layout.Title} {layout.Letter} · Nº {DocumentNumber(datos)}");
        runningHeader.Format.Font.Size = 8;
        runningHeader.Format.Font.Bold = true;
        runningHeader.Format.Alignment = ParagraphAlignment.Right;
        if (layout.RequiredLegend is not null)
        {
            Paragraph legend = section.AddParagraph($"COMPROBANTE {layout.Letter}    {layout.RequiredLegend}");
            legend.Format.Alignment = ParagraphAlignment.Center;
            legend.Format.Borders.Width = Unit.FromPoint(1.5);
            legend.Format.Borders.Distance = Unit.FromPoint(7);
            legend.Format.Font.Bold = true;
            legend.Format.Font.Size = 13;
        }
        AddIssuer(section, datos, layout);
        section.AddParagraph("Datos del receptor", "Heading2");
        AddLine(section, "Nombre / razón social", datos.Receptor.Nombre);
        if (!string.IsNullOrWhiteSpace(datos.Receptor.Domicilio)) AddLine(section, "Domicilio", datos.Receptor.Domicilio);
        string receiverCondition = DocumentFiscalRules.ReceiverCondition(datos);
        if (!string.IsNullOrWhiteSpace(receiverCondition)) AddLine(section, DocumentFiscalRules.ReceiverConditionLabel(datos), receiverCondition);
        AddLine(section, "Identificación", datos.Receptor.IdentificacionFiscal ?? datos.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture));

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
            AddFiscalSummary(section, datos);
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
        else section.AddParagraph(layout.RequiredLegend!, "Heading2");
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
        return OverlayImages(result, _issuer.LogoPng, qrUrl);
    }

    private void AddIssuer(Section section, ComprobanteGenerado data, DocumentLayout layout)
    {
        var table = section.AddTable();
        table.Borders.Width = Unit.FromPoint(0.4);
        table.AddColumn(Unit.FromCentimeter(7.75));
        table.AddColumn(Unit.FromCentimeter(2.7));
        table.AddColumn(Unit.FromCentimeter(7.75));
        Row row = table.AddRow();
        row.VerticalAlignment = VerticalAlignment.Center;
        Paragraph brand = row.Cells[0].AddParagraph(_issuer.RazonSocial);
        brand.Format.Font.Bold = true;
        brand.Format.Font.Size = 13;
        if (!string.IsNullOrWhiteSpace(_issuer.NombreFantasia)) { brand.AddLineBreak(); brand.AddText(_issuer.NombreFantasia); }
        if (!string.IsNullOrWhiteSpace(_issuer.Domicilio)) { brand.AddLineBreak(); brand.AddText(_issuer.Domicilio); }
        brand.AddLineBreak(); brand.AddText($"CUIT: {_issuer.Identificacion.ToString(CultureInfo.InvariantCulture)}");
        brand.AddLineBreak(); brand.AddText(_issuer.CondicionTributaria);
        if (!string.IsNullOrWhiteSpace(_issuer.IngresosBrutos)) { brand.AddLineBreak(); brand.AddText($"Ingresos Brutos: {_issuer.IngresosBrutos}"); }
        if (_issuer.InicioActividades is DateOnly start) { brand.AddLineBreak(); brand.AddText($"Inicio de actividades: {Date(start)}"); }
        Paragraph code = row.Cells[1].AddParagraph(layout.Letter);
        code.Format.Font.Bold = true;
        code.Format.Font.Size = 22;
        code.Format.Alignment = ParagraphAlignment.Center;
        if (layout.IsFiscal)
        {
            Paragraph typeCode = row.Cells[1].AddParagraph($"COD. {data.Identidad!.Tipo:000}");
            typeCode.Format.Alignment = ParagraphAlignment.Center;
            typeCode.Format.Font.Size = 7;
        }
        Paragraph title = row.Cells[2].AddParagraph(layout.Title);
        title.Format.Font.Bold = true;
        title.Format.Font.Size = 13;
        title.Format.Alignment = ParagraphAlignment.Right;
        title.AddLineBreak();
        string number = data.Identidad is null ? data.NumeroInterno ?? string.Empty : $"{data.Identidad.PuntoVenta:00000}-{data.Identidad.Numero:00000000}";
        title.AddText($"Nº {number}");
        title.AddLineBreak(); title.AddText($"Fecha: {(data.Fecha is DateOnly date ? Date(date) : "—")}");
        if (layout.IsFiscal) { title.AddLineBreak(); title.AddText("ORIGINAL"); }
        if (layout.IsFiscal) { title.AddLineBreak(); title.AddText($"CAE: {data.Resultado!.CodigoAutorizacion}"); title.AddLineBreak(); title.AddText($"Vencimiento CAE: {Date(data.Resultado.Vencimiento!.Value)}"); }
        if (data.FechaDesde is DateOnly from) { title.AddLineBreak(); title.AddText($"Período desde: {Date(from)}"); }
        if (data.FechaHasta is DateOnly through) { title.AddLineBreak(); title.AddText($"Período hasta: {Date(through)}"); }
        if (data.FechaPago is DateOnly due) { title.AddLineBreak(); title.AddText($"Vencimiento de pago: {Date(due)}"); }
        if (!string.IsNullOrWhiteSpace(data.Moneda)) { title.AddLineBreak(); title.AddText($"Moneda: {CurrencyName(data.Moneda)}"); }
        if (!IsPesoCurrency(data.Moneda) && data.Cotizacion > 0) { title.AddLineBreak(); title.AddText($"Cotización: {FullDecimal(data.Cotizacion)} ARS por {CurrencyName(data.Moneda)}"); }
        section.AddParagraph();
    }

    private static void AddDetailTable(Section section, ComprobanteGenerado data)
    {
        var table = section.AddTable();
        table.Borders.Color = Colors.LightGray;
        table.Borders.Width = Unit.FromPoint(0.35);
        table.AddColumn(Unit.FromCentimeter(8.3)); table.AddColumn(Unit.FromCentimeter(2.1));
        table.AddColumn(Unit.FromCentimeter(4.0)); table.AddColumn(Unit.FromCentimeter(3.8));
        Row header = table.AddRow(); header.HeadingFormat = true; header.Shading.Color = Colors.Gainsboro;
        header.Cells[0].AddParagraph("Descripción"); header.Cells[1].AddParagraph("Cantidad");
        header.Cells[2].AddParagraph("Precio unitario"); header.Cells[3].AddParagraph("Importe");
        foreach (Cell? cell in header.Cells) if (cell is not null) cell.Format.Font.Bold = true;
        foreach (LineaComprobante line in data.Detalles)
        {
            Row row = table.AddRow();
            row.Cells[0].AddParagraph(line.DescripcionPresentacion ?? line.Descripcion);
            row.Cells[1].AddParagraph(line.Cantidad is decimal qty ? Number(qty) : "—");
            row.Cells[2].AddParagraph(line.PrecioUnitario is decimal price ? $"{Number(price)} {CurrencyName(data.Moneda)}" : "—");
            row.Cells[3].AddParagraph(Money(line.Importe, data.Moneda));
        }
    }

    private static void AddFiscalSummary(Section section, ComprobanteGenerado data)
    {
        if (data.Impuestos.Count == 0) return;
        section.AddParagraph("Impuestos y alícuotas", "Heading2");
        AddSimpleTable(section, ["Impuesto", "Base imponible", "Alícuota / clasificación", "Importe"], data.Impuestos.Select(t => new[]
        {
            t.Tipo == TipoImpuestoComprobante.Iva ? "IVA" : t.Nombre,
            t.BaseImponible is decimal basis ? Money(basis, t.Moneda) : "—",
            t.Tipo == TipoImpuestoComprobante.Iva ? (t.Alicuota is decimal rate ? $"{FullDecimal(rate)}%" : DocumentLayout.IvaRate(t.Codigo) ?? throw new NotSupportedException($"Código IVA no soportado: {t.Codigo}")) : t.Clasificacion ?? "Clasificación no informada",
            Money(t.Importe, t.Moneda)
        }));
    }

    private static void AddSummaryRows(Section section, ComprobanteGenerado data)
    {
        section.AddParagraph("Resumen", "Heading2");
        var rows = new List<string[]>();
        foreach ((string label, decimal? value) in new[] { ("Neto gravado", data.Neto), ("No gravado", data.NoGravado), ("Exento", data.Exento) })
            if (value is decimal amount && amount != 0) rows.Add([label, Money(amount, data.Moneda)]);
        if (rows.Count == 0 && data.Impuestos.Count == 0) rows.Add(["Importe total", Money(data.Total, data.Moneda)]);
        if (rows.Count != 0) AddSimpleTable(section, ["Concepto", "Importe"], rows);
    }

    private static void AddFiscalTotals(Section section, ComprobanteGenerado data)
    {
        section.AddParagraph(data.CodigoCondicionIvaReceptor == 5 ? DocumentFiscalRules.ConsumerTransparencyTitle : "Totales", "Heading2");
        var rows = new List<string[]>();
        decimal nationalIndirect = data.Impuestos.Where(tax => tax.Tipo == TipoImpuestoComprobante.OtroTributo && tax.Clasificacion == "NacionalIndirecto").Sum(tax => tax.Importe);
        if (data.CodigoCondicionIvaReceptor == 5)
        {
            if (data.Iva is not decimal consumerIva) throw new InvalidOperationException("Falta el importe de IVA contenido requerido para transparencia al consumidor.");
            rows.Add([DocumentFiscalRules.ConsumerIvaLabel, Money(consumerIva, data.Moneda)]);
            rows.Add([DocumentFiscalRules.ConsumerOtherTaxesLabel, Money(nationalIndirect, data.Moneda)]);
        }
        else
        {
            rows.Add([DocumentFiscalRules.NetTotalLabel, Money(data.Neto ?? data.Total, data.Moneda)]);
            if (data.Iva is decimal iva) rows.Add([DocumentFiscalRules.IvaTotalLabel, Money(iva, data.Moneda)]);
            if (nationalIndirect != 0) rows.Add(["Otros impuestos nacionales indirectos", Money(nationalIndirect, data.Moneda)]);
        }
        rows.Add(["TOTAL", Money(data.Total, data.Moneda)]);
        AddSimpleTable(section, ["Concepto", "Importe"], rows);
    }

    private static void AddTotalTable(Section section, string label, decimal amount, string currency)
    {
        var table = section.AddTable(); table.Borders.Width = Unit.FromPoint(0.6);
        table.AddColumn(Unit.FromCentimeter(10)); table.AddColumn(Unit.FromCentimeter(8));
        Row row = table.AddRow(); row.Cells[0].AddParagraph(label).Format.Font.Bold = true;
        row.Cells[1].AddParagraph(Money(amount, currency)).Format.Font.Bold = true;
        row.Cells[1].Format.Alignment = ParagraphAlignment.Right;
    }

    private static void AddSimpleTable(Section section, string[] headers, IEnumerable<string[]> rows)
    {
        var table = section.AddTable(); table.Borders.Width = Unit.FromPoint(0.3);
        table.AddColumn(Unit.FromCentimeter(headers.Length == 2 ? 12 : 5));
        for (int index = 1; index < headers.Length; index++) table.AddColumn(Unit.FromCentimeter(headers.Length == 2 ? 6 : 4.4));
        Row header = table.AddRow(); header.HeadingFormat = true; header.Shading.Color = Colors.Gainsboro;
        for (int index = 0; index < headers.Length; index++) header.Cells[index].AddParagraph(headers[index]);
        foreach (Cell? cell in header.Cells) if (cell is not null) cell.Format.Font.Bold = true;
        foreach (string[] values in rows) { Row row = table.AddRow(); for (int index = 0; index < values.Length; index++) row.Cells[index].AddParagraph(values[index]); }
    }

    private static void AddLine(Section section, string label, string value)
    {
        Paragraph p = section.AddParagraph(); p.AddFormattedText(label + (label.EndsWith(':') ? " " : ": "), TextFormat.Bold); p.AddText(value);
    }

    private static byte[] OverlayImages(byte[] input, byte[]? logo, string? qrUrl)
    {
        using var source = new MemoryStream(input);
        PdfDocument document = PdfReader.Open(source, PdfDocumentOpenMode.Modify);
        if (logo is { Length: > 0 })
        {
            PdfPage firstPage = document.Pages[0];
            using XGraphics graphics = XGraphics.FromPdfPage(firstPage, XGraphicsPdfPageOptions.Append);
            using var logoStream = new MemoryStream(ToBmp(logo));
            using XImage image = XImage.FromStream(logoStream);
            double max = XUnit.FromMillimeter(23).Point;
            double scale = Math.Min(max / image.PixelWidth, max / image.PixelHeight);
            double width = image.PixelWidth * scale, height = image.PixelHeight * scale;
            graphics.DrawImage(image, XUnit.FromMillimeter(15).Point, XUnit.FromMillimeter(14).Point, width, height);
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
    private static string Money(decimal value, string currency) => $"{Number(value)} {CurrencyName(currency)}";
    private static string CurrencyName(string code) => code.ToUpperInvariant() switch { "ARS" or "PES" => "pesos", "USD" => "dólares estadounidenses", "EUR" => "euros", "BRL" => "reales", _ => code.ToUpperInvariant() };
    private static bool IsPesoCurrency(string code) => code.Equals("ARS", StringComparison.OrdinalIgnoreCase) || code.Equals("PES", StringComparison.OrdinalIgnoreCase);
    private static string DocumentNumber(ComprobanteGenerado data) => data.Identidad is null ? data.NumeroInterno ?? string.Empty : $"{data.Identidad.PuntoVenta:00000}-{data.Identidad.Numero:00000000}";

    private void ValidatePrintableText(ComprobanteGenerado document)
    {
        var values = new List<string?> { _issuer.RazonSocial, _issuer.NombreFantasia, _issuer.Domicilio, _issuer.CondicionTributaria,
            _issuer.IngresosBrutos, document.Receptor.Nombre, document.Receptor.Domicilio, document.Receptor.CondicionTributaria,
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
}
