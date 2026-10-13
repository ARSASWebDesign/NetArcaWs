using System.Globalization;
using System.Net;
using System.Text;
using NetArcaWs.Documents;
using NetArcaWs.Rendering.Shared;
using QRCoder;

namespace NetArcaWs.Rendering.Html;

/// <summary>Renders authorized fiscal documents and local X receipts as self-contained printable HTML.</summary>
public sealed class ArcaWsGeneradorHTML(DatosEmisor emisor)
{
    private readonly DatosEmisor _issuer = emisor ?? throw new ArgumentNullException(nameof(emisor));

    public string GenerarHTMLComprobante(ComprobanteGenerado datos)
    {
        DocumentLayout layout = DocumentRenderValidator.Validate(datos, _issuer);
        DocumentFiscalRules.ValidateConditions(datos, layout);
        string qrUrl = layout.IsFiscal ? FiscalQrPayload.Create(datos, _issuer) : string.Empty;
        string qr = layout.IsFiscal ? MakeQr(qrUrl) : string.Empty;
        var html = new StringBuilder(12_000);
        html.Append("<!doctype html><html lang=\"es-AR\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>")
            .Append(E(layout.Title)).Append(' ').Append(E(layout.Letter)).Append("</title><style>").Append(Styles).Append("</style></head><body><main class=\"page\">");
        if (layout.RequiredLegend is not null)
            html.Append("<div class=\"required-legend\"><strong>COMPROBANTE ").Append(E(layout.Letter)).Append("</strong><span>").Append(E(layout.RequiredLegend)).Append("</span></div>");
        html.Append("<header class=\"issuer\">");
        if (_issuer.LogoPng is { Length: > 0 } logo)
            html.Append("<img class=\"logo\" alt=\"Logo del emisor\" src=\"data:image/png;base64,").Append(Convert.ToBase64String(logo)).Append("\">");
        html.Append("<div class=\"issuer-copy\"><h1>").Append(E(_issuer.RazonSocial)).Append("</h1>");
        if (!string.IsNullOrWhiteSpace(_issuer.NombreFantasia)) html.Append("<div>").Append(E(_issuer.NombreFantasia)).Append("</div>");
        html.Append("<div>").Append(E(_issuer.Domicilio)).Append("</div><div>CUIT: ").Append(_issuer.Identificacion.ToString(CultureInfo.InvariantCulture)).Append("</div><div>").Append(E(_issuer.CondicionTributaria)).Append("</div>");
        if (!string.IsNullOrWhiteSpace(_issuer.IngresosBrutos)) html.Append("<div>Ingresos Brutos: ").Append(E(_issuer.IngresosBrutos)).Append("</div>");
        if (_issuer.InicioActividades is DateOnly start) html.Append("<div>Inicio de actividades: ").Append(Date(start)).Append("</div>");
        html.Append("</div><div class=\"document-code\"><strong>").Append(E(layout.Letter)).Append("</strong>");
        if (layout.IsFiscal) html.Append("<small>COD. ").Append(datos.Identidad!.Tipo.ToString("000", CultureInfo.InvariantCulture)).Append("</small>");
        html.Append("</div><div class=\"document-meta\"><h2>").Append(E(layout.Title)).Append("</h2><div>Nº ").Append(E(datos.Identidad is null ? datos.NumeroInterno : $"{datos.Identidad.PuntoVenta:00000}-{datos.Identidad.Numero:00000000}")).Append("</div><div>Fecha: ").Append(datos.Fecha is DateOnly date ? Date(date) : "—").Append("</div>");
        if (layout.IsFiscal) html.Append("<div class=\"copy-label\">ORIGINAL</div>");
        if (datos.Tipo == TipoComprobanteGenerado.Fiscal)
            html.Append("<div>CAE: ").Append(E(datos.Resultado!.CodigoAutorizacion!)).Append("</div><div>Vencimiento CAE: ").Append(Date(datos.Resultado.Vencimiento!.Value)).Append("</div>");
        if (datos.FechaDesde is DateOnly from) html.Append("<div>Período desde: ").Append(Date(from)).Append("</div>");
        if (datos.FechaHasta is DateOnly through) html.Append("<div>Período hasta: ").Append(Date(through)).Append("</div>");
        if (datos.FechaPago is DateOnly paymentDue) html.Append("<div>Vencimiento de pago: ").Append(Date(paymentDue)).Append("</div>");
        if (!IsPesoCurrency(datos.Moneda) && datos.Cotizacion > 0) html.Append("<div>Cotización: ").Append(FullDecimal(datos.Cotizacion)).Append(" ARS por ").Append(E(CurrencyName(datos.Moneda))).Append("</div>");
        html.Append("</div></header>");
        html.Append("<section class=\"receiver\"><h3>Datos del receptor</h3><div><b>Nombre / razón social:</b> ").Append(E(datos.Receptor.Nombre)).Append("</div>");
        if (!string.IsNullOrWhiteSpace(datos.Receptor.Domicilio)) html.Append("<div><b>Domicilio:</b> ").Append(E(datos.Receptor.Domicilio)).Append("</div>");
        if (!string.IsNullOrWhiteSpace(datos.Receptor.CondicionTributaria) || datos.CodigoCondicionIvaReceptor is not null) html.Append("<div><b>").Append(E(DocumentFiscalRules.ReceiverConditionLabel(datos))).Append("</b> ").Append(E(DocumentFiscalRules.ReceiverCondition(datos))).Append("</div>");
        string? receiverId = !string.IsNullOrWhiteSpace(datos.Receptor.IdentificacionFiscal)
            ? datos.Receptor.IdentificacionFiscal
            : datos.Receptor.Identificacion > 0 ? datos.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture) : null;
        if (receiverId is not null) html.Append("<div><b>Identificación:</b> ").Append(E(receiverId)).Append("</div>");
        html.Append("</section>");

        IReadOnlyList<string> associatedDocuments = FiscalAssociatedDocuments.Read(datos);
        if (associatedDocuments.Count != 0)
        {
            html.Append("<section class=\"references\"><h3>Comprobantes asociados</h3><ul>");
            foreach (string reference in associatedDocuments) html.Append("<li>").Append(E(reference)).Append("</li>");
            html.Append("</ul></section>");
        }

        if (datos.Tipo == TipoComprobanteGenerado.ReciboX)
        {
            AppendDetailTable(html, datos.Detalles, datos.Moneda);
            html.Append("<section class=\"payments\"><h3>Pagos recibidos</h3><table><thead><tr><th>Medio</th><th class=\"number\">Importe</th></tr></thead><tbody>");
            foreach (PagoReciboX payment in datos.Pagos) html.Append("<tr><td>").Append(E(payment.Medio)).Append("</td><td class=\"number\">").Append(Money(payment.Importe, payment.Moneda)).Append("</td></tr>");
            html.Append("</tbody></table></section>");
            if (datos.Imputaciones.Count > 0)
            {
                html.Append("<section class=\"payments\"><h3>Imputaciones</h3><table><thead><tr><th>Referencia</th><th class=\"number\">Importe imputado</th></tr></thead><tbody>");
                foreach (ImputacionReciboX allocation in datos.Imputaciones) html.Append("<tr><td>").Append(E(allocation.Referencia)).Append("</td><td class=\"number\">").Append(Money(allocation.Importe, allocation.Moneda)).Append("</td></tr>");
                html.Append("</tbody></table></section>");
            }
            html.Append("<section class=\"totals\"><div class=\"grand-total\"><span>Total recibido</span><strong>").Append(Money(datos.Total, datos.Moneda)).Append("</strong></div></section>");
        }
        else
        {
            if (datos.Detalles.Count > 0) AppendDetailTable(html, datos.Detalles, datos.Moneda);
            else AppendSummaryTable(html, datos);
            AppendTaxes(html, datos);
            AppendFiscalTotals(html, datos);
            IReadOnlyList<string> legends = DocumentFiscalRules.DocumentLegends(datos);
            if (legends.Count > 0)
            {
                html.Append("<section class=\"notes\"><h3>Leyendas</h3>");
                foreach (string legend in legends) html.Append("<p>").Append(E(legend)).Append("</p>");
                html.Append("</section>");
            }
        }
        html.Append("<footer class=\"authorization\">");
        if (layout.IsFiscal) html.Append("<div class=\"qr\" data-qr-url=\"").Append(E(qrUrl)).Append("\">").Append(qr).Append("</div>");
        html.Append("<div class=\"footer-copy\"><strong>").Append(E(layout.IsFiscal ? "Comprobante autorizado por ARCA" : "Recibo X")).Append("</strong>");
        if (layout.IsFiscal) html.Append("<div>CAE: ").Append(E(datos.Resultado!.CodigoAutorizacion!)).Append(" · Vencimiento: ").Append(Date(datos.Resultado.Vencimiento!.Value)).Append("</div>");
        else html.Append("<div>").Append(E(layout.RequiredLegend!)).Append("</div>");
        html.Append("</div></footer></main></body></html>");
        return html.ToString();
    }

    private static void AppendDetailTable(StringBuilder html, IReadOnlyList<LineaComprobante> lines, string currency)
    {
        html.Append("<table class=\"details\"><thead><tr><th>Descripción</th><th class=\"number\">Cantidad</th><th class=\"number\">Precio unitario</th><th class=\"number\">Importe</th></tr></thead><tbody>");
        foreach (LineaComprobante line in lines)
        {
            html.Append("<tr><td>").Append(E(line.DescripcionPresentacion ?? line.Descripcion)).Append("</td><td class=\"number\">").Append(line.Cantidad is decimal quantity ? FullDecimal(quantity) : "—")
                .Append("</td><td class=\"number\">").Append(line.PrecioUnitario is decimal price ? $"{FullDecimal(price)} {CurrencyName(currency)}" : "—")
                .Append("</td><td class=\"number\">").Append(Money(line.Importe, currency)).Append("</td></tr>");
        }
        html.Append("</tbody></table>");
    }

    private static void AppendSummaryTable(StringBuilder html, ComprobanteGenerado document)
    {
        html.Append("<table class=\"details summary\"><thead><tr><th>Concepto</th><th class=\"number\">Importe</th></tr></thead><tbody>");
        foreach ((string Label, decimal? Amount) row in new[] { ("Neto gravado", document.Neto), ("No gravado", document.NoGravado), ("Exento", document.Exento) })
            if (row.Amount is decimal amount && amount != 0) html.Append("<tr><td>").Append(row.Label).Append("</td><td class=\"number\">").Append(Money(amount, document.Moneda)).Append("</td></tr>");
        if (document.Impuestos.Count > 0)
            foreach (ImpuestoComprobante tax in document.Impuestos)
                html.Append("<tr><td>").Append(E(tax.Nombre)).Append("</td><td class=\"number\">").Append(Money(tax.Importe, tax.Moneda)).Append("</td></tr>");
        if (document.Neto is null && document.Exento is null && document.NoGravado is null && document.Impuestos.Count == 0)
            html.Append("<tr><td>Importe total</td><td class=\"number\">").Append(Money(document.Total, document.Moneda)).Append("</td></tr>");
        html.Append("</tbody></table>");
    }

    private static void AppendFiscalTotals(StringBuilder html, ComprobanteGenerado document)
    {
        if (document.CodigoCondicionIvaReceptor == 5)
        {
            if (document.Iva is not decimal consumerIva) throw new InvalidOperationException("Falta el importe de IVA contenido requerido para transparencia al consumidor.");
            decimal nationalTaxes = document.Impuestos.Where(tax => tax.Tipo == TipoImpuestoComprobante.OtroTributo && tax.Clasificacion == "NacionalIndirecto").Sum(tax => tax.Importe);
            html.Append("<section class=\"transparency\"><h3>").Append(DocumentFiscalRules.ConsumerTransparencyTitle).Append("</h3><div><span>").Append(DocumentFiscalRules.ConsumerIvaLabel).Append("</span><strong>").Append(Money(consumerIva, document.Moneda)).Append("</strong></div><div><span>").Append(DocumentFiscalRules.ConsumerOtherTaxesLabel).Append("</span><strong>").Append(Money(nationalTaxes, document.Moneda)).Append("</strong></div>");
        }
        else
        {
            html.Append("<section class=\"totals\"><div><span>").Append(DocumentFiscalRules.NetTotalLabel).Append("</span><strong>").Append(Money(document.Neto ?? document.Total, document.Moneda)).Append("</strong></div>");
            if (document.Iva is decimal iva) html.Append("<div><span>").Append(DocumentFiscalRules.IvaTotalLabel).Append("</span><strong>").Append(Money(iva, document.Moneda)).Append("</strong></div>");
        }
        decimal nationalIndirect = document.Impuestos.Where(tax => tax.Tipo == TipoImpuestoComprobante.OtroTributo && tax.Clasificacion == "NacionalIndirecto").Sum(tax => tax.Importe);
        if (document.CodigoCondicionIvaReceptor != 5 && nationalIndirect != 0) html.Append("<div><span>Otros impuestos nacionales indirectos</span><strong>").Append(Money(nationalIndirect, document.Moneda)).Append("</strong></div>");
        html.Append("<div class=\"grand-total\"><span>Total</span><strong>").Append(Money(document.Total, document.Moneda)).Append("</strong></div></section>");
    }

    private static void AppendTaxes(StringBuilder html, ComprobanteGenerado document)
    {
        if (document.Impuestos.Count == 0) return;
        html.Append("<section class=\"taxes\"><h3>Impuestos y alícuotas</h3><table><thead><tr><th>Impuesto</th><th class=\"number\">Base imponible</th><th class=\"number\">Alícuota / clasificación</th><th class=\"number\">Importe</th></tr></thead><tbody>");
        foreach (ImpuestoComprobante tax in document.Impuestos)
        {
            string name = tax.Nombre;
            string rateOrClass;
            if (tax.Tipo == TipoImpuestoComprobante.Iva)
            {
                string? rate = tax.Alicuota is decimal explicitRate ? $"{FullDecimal(explicitRate)}%" : DocumentLayout.IvaRate(tax.Codigo);
                if (rate is null) throw new NotSupportedException($"No hay una alícuota oficial disponible para el código IVA {tax.Codigo ?? "(vacío)"}.");
                name = "IVA";
                rateOrClass = rate;
            }
            else rateOrClass = tax.Clasificacion ?? "Clasificación no informada";
            html.Append("<tr><td>").Append(E(name)).Append("</td><td class=\"number\">").Append(tax.BaseImponible is decimal baseAmount ? Money(baseAmount, tax.Moneda) : "—")
                .Append("</td><td class=\"number\">").Append(E(rateOrClass)).Append("</td><td class=\"number\">").Append(Money(tax.Importe, tax.Moneda)).Append("</td></tr>");
        }
        html.Append("</tbody></table></section>");
    }

    private static string MakeQr(string url)
    {
        using var data = QRCodeGenerator.GenerateQrCode(url, QRCodeGenerator.ECCLevel.M);
        byte[] png = new PngByteQRCode(data).GetGraphic(5);
        return "<img class=\"qr-image\" alt=\"Código QR de verificación ARCA\" src=\"data:image/png;base64," + Convert.ToBase64String(png) + "\">";
    }

    private static string Money(decimal amount, string currency) => $"{amount.ToString("N2", ArCulture)} {CurrencyName(currency)}";
    private static string CurrencyName(string currency) => currency.ToUpperInvariant() switch
    {
        "ARS" or "PES" => "pesos", "USD" => "dólares estadounidenses", "EUR" => "euros", "BRL" => "reales", _ => currency.ToUpperInvariant()
    };
    private static bool IsPesoCurrency(string currency) => currency.Equals("ARS", StringComparison.OrdinalIgnoreCase) || currency.Equals("PES", StringComparison.OrdinalIgnoreCase);
    private static string FullDecimal(decimal value) => value.ToString("#,##0.########################", ArCulture);
    private static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", ArCulture);
    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    private static CultureInfo ArCulture => CultureInfo.GetCultureInfo("es-AR");

    private const string Styles = "@page{size:A4;margin:14mm}*{box-sizing:border-box}body{margin:0;color:#171717;background:#e8e8e8;font:10pt Arial,Helvetica,sans-serif}.page{width:210mm;min-height:297mm;margin:18px auto;padding:14mm;background:#fff;box-shadow:0 1px 8px #999;display:flex;flex-direction:column}.issuer{display:grid;grid-template-columns:1fr 19mm 1fr;gap:8px;align-items:start;border-bottom:1px solid #555;padding:8px 0 12px}.logo{grid-column:1/-1;justify-self:start;display:block;max-width:42mm;max-height:22mm;object-fit:contain}.issuer-copy h1{font-size:15pt;margin:0 0 4px}.document-code{text-align:center;border:1px solid #333;padding:4px}.document-code strong{display:block;font-size:22pt}.document-code small{display:block;font-size:8pt}.document-meta{text-align:right;font-size:9pt}.document-meta h2{font-size:14pt;margin:0 0 5px}.copy-label{font-weight:bold;letter-spacing:.12em;margin:3px 0}.receiver{padding:12px 0;border-bottom:1px solid #aaa;line-height:1.6}.receiver h3,.references h3,.payments h3,.notes h3,.taxes h3,.transparency h3{font-size:10pt;margin:0 0 5px}.references{margin-top:12px}.details{width:100%;border-collapse:collapse;margin-top:14px}.details thead{display:table-header-group}.details th{background:#eee;text-align:left;border-bottom:1px solid #777}.details th,.details td{padding:6px;border-bottom:1px solid #ddd;vertical-align:top}.number{text-align:right;white-space:nowrap}.totals{width:68%;margin:12px 0 0 auto}.totals>div,.transparency>div{display:flex;justify-content:space-between;padding:4px;border-bottom:1px solid #ddd}.totals .grand-total{font-size:12pt;border-top:1px solid #222;border-bottom:3px double #222;margin-top:5px;padding:8px 4px}.transparency>.grand-total{font-size:12pt;font-weight:bold;border-top:2px solid #222;border-bottom:3px double #222;margin-top:10px;padding:8px 4px}.notes,.taxes,.transparency{margin-top:12px}.notes p{margin:4px 0}.required-legend{display:flex;justify-content:center;align-items:center;gap:14px;border:2px solid #222;padding:7px;margin:0 auto 12px;text-align:center;width:100%;font-size:10pt}.required-legend strong{font-size:18pt}.authorization{display:flex;gap:14px;align-items:center;justify-content:space-between;border-top:1px solid #555;padding-top:9px;margin-top:auto}.qr{width:30mm;height:30mm;flex:none}.qr-image{display:block;width:100%;height:100%;image-rendering:pixelated}.footer-copy{text-align:right;font-size:8pt}.footer-copy strong{display:block;font-size:9pt}.payments{margin-top:12px}.payments table,.taxes table{width:100%;border-collapse:collapse}.payments th,.payments td,.taxes th,.taxes td{padding:6px;border-bottom:1px solid #ddd}@media print{body{background:#fff}.page{width:auto;min-height:269mm;margin:0;padding:0;box-shadow:none}tr{break-inside:avoid}.receiver,.references,.totals,.authorization,.transparency{break-inside:avoid}}@media screen and (max-width:700px){.page{width:100%;min-height:100vh;margin:0;padding:18px}.issuer{grid-template-columns:1fr 16mm}.document-meta{grid-column:1/-1;text-align:left}.totals,.transparency{width:100%}.details,.taxes{font-size:8pt}.details th,.details td,.taxes th,.taxes td{padding:4px}.authorization{align-items:flex-end}}";
}
