using System.Globalization;
using System.Net;
using System.Text;
using NetArcaWs.Documents;
using NetArcaWs.Rendering.Shared;
using QRCoder;

namespace NetArcaWs.Rendering.Html;

/// <summary>Renders authorized fiscal documents and local X receipts as self-contained printable HTML.</summary>
public sealed class ArcaWsGeneradorHTML
{
    public ArcaWsGeneradorHTML(DatosEmisor emisor, string colorPrincipal = "#333333")
    {
        _issuer = emisor ?? throw new ArgumentNullException(nameof(emisor));
        _style = new RenderingStyle(colorPrincipal);
    }

    private readonly DatosEmisor _issuer;
    private readonly RenderingStyle _style;

    public string ColorPrincipal => _style.ColorPrincipal;
    public string VersionRenderizado => RenderingStyle.RendererVersion(typeof(ArcaWsGeneradorHTML).Assembly, "html-2");

    public string GenerarHTMLComprobante(ComprobanteGenerado datos)
    {
        DocumentLayout layout = DocumentRenderValidator.Validate(datos, _issuer);
        DocumentFiscalRules.ValidateConditions(datos, layout);
        string qrUrl = layout.IsFiscal ? FiscalQrPayload.Create(datos, _issuer) : string.Empty;
        string qr = layout.IsFiscal ? MakeQr(qrUrl) : string.Empty;
        var html = new StringBuilder(12_000);
        html.Append("<!doctype html><html lang=\"es-AR\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>")
            .Append(E(layout.Title)).Append(' ').Append(E(layout.Letter)).Append("</title><style>:root{").Append(_style.CssVariables).Append('}').Append(Styles).Append("</style></head><body><main class=\"page\">");
        if (layout.IsFiscal) html.Append("<div class=\"copy-label\">ORIGINAL</div>");
        if (layout.RequiredLegend is not null)
            html.Append("<div class=\"required-legend\"><strong>COMPROBANTE ").Append(E(layout.Letter)).Append("</strong><span>").Append(E(layout.RequiredLegend)).Append("</span></div>");
        html.Append("<header class=\"issuer\">");
        AppendIssuerBrand(html, _issuer);
        html.Append("<div class=\"document-code\"><strong>").Append(E(layout.Letter)).Append("</strong>");
        if (layout.IsFiscal) html.Append("<small>COD. ").Append(datos.Identidad!.Tipo.ToString("000", CultureInfo.InvariantCulture)).Append("</small>");
        html.Append("</div><div class=\"document-meta\"><h2>").Append(E(layout.Title)).Append("</h2><div>Nº ").Append(E(datos.Identidad is null ? datos.NumeroInterno : $"{datos.Identidad.PuntoVenta:00000}-{datos.Identidad.Numero:00000000}")).Append("</div><div>Fecha: ").Append(datos.Fecha is DateOnly date ? Date(date) : "—").Append("</div>");
        html.Append("</div>");
        AppendIssuerFiscal(html, _issuer);
        html.Append("</header>");
        ExportDocumentFields? export = ExportDocumentFields.Read(datos.Identidad?.Servicio, datos.SnapshotRequest);
        html.Append("<section class=\"receiver\"><h3>Datos del receptor</h3><div class=\"receiver-columns\"><div class=\"receiver-left\"><div><b>Nombre / razón social:</b> ")
            .Append(E(datos.Receptor.Nombre)).Append("</div>");
        if (!string.IsNullOrWhiteSpace(datos.Receptor.Domicilio)) html.Append("<div><b>Domicilio:</b> ").Append(E(datos.Receptor.Domicilio)).Append("</div>");
        html.Append("</div><div class=\"receiver-right\">");
        if (!string.IsNullOrWhiteSpace(datos.Receptor.CondicionTributaria) || datos.CodigoCondicionIvaReceptor is not null) html.Append("<div><b>").Append(E(DocumentFiscalRules.ReceiverConditionLabel(datos))).Append("</b> ").Append(E(DocumentFiscalRules.ReceiverCondition(datos))).Append("</div>");
        string? receiverId = !string.IsNullOrWhiteSpace(datos.Receptor.IdentificacionFiscal)
            ? datos.Receptor.IdentificacionFiscal
            : datos.Receptor.Identificacion > 0 ? datos.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture) : null;
        if (receiverId is not null) html.Append("<div><b>").Append(E(ReceiverIdLabel(datos))).Append(":</b> ").Append(E(receiverId)).Append("</div>");
        if (datos.FechaPago is DateOnly paymentDue) html.Append("<div><b>Vencimiento de pago:</b> ").Append(Date(paymentDue)).Append("</div>");
        html.Append("</div></div></section>");
        AppendInvoiceSupplementalFields(html, datos);
        if (export is not null) AppendExportFields(html, export);

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

    /// <summary>Renders a validated local remito R/X without fiscal authorization data.</summary>
    public string GenerarHTMLComprobante(RemitoGenerado remito)
    {
        RemitoValidator.Validate(remito);
        if (remito.Emisor.Identificacion != _issuer.Identificacion)
            throw new ArgumentException("El remito pertenece a otro perfil de emisor.", nameof(remito));
        var html = new StringBuilder(8_000);
        html.Append("<!doctype html><html lang=\"es-AR\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Remito ")
            .Append(remito.Clase).Append("</title><style>:root{").Append(_style.CssVariables).Append('}').Append(Styles).Append("</style></head><body><main class=\"page\">");
        html.Append("<div class=\"copy-label\">").Append(remito.EsOriginal ? "ORIGINAL" : "DUPLICADO").Append("</div><div class=\"required-legend remito-legend\"><strong>")
            .Append(E(remito.Leyenda)).Append("</strong></div><header class=\"issuer\">");
        AppendIssuerBrand(html, _issuer);
        html.Append("<div class=\"document-code\"><strong>").Append(remito.Clase).Append("</strong>");
        if (remito.CodigoTipoComprobante is int code) html.Append("<small>COD. ").Append(code.ToString("000", CultureInfo.InvariantCulture)).Append("</small>");
        html.Append("</div><div class=\"document-meta\"><h2>Remito ").Append(remito.Clase).Append("</h2><div>Nº ")
            .Append(remito.PuntoVenta.ToString("00000", CultureInfo.InvariantCulture)).Append('-').Append(remito.Numero.ToString("00000000", CultureInfo.InvariantCulture))
            .Append("</div><div>Fecha: ").Append(Date(remito.FechaEmision)).Append("</div><div>Destino: ").Append(E(remito.Destino)).Append("</div>");
        if (remito.Cai is not null) html.Append("<div>CAI: ").Append(E(remito.Cai)).Append("</div><div>Vencimiento CAI: ").Append(Date(remito.Vencimiento!.Value)).Append("</div>");
        if (remito.EsAutoimpresor) html.Append("<div>Autoimpresor</div>");
        html.Append("</div>");
        AppendIssuerFiscal(html, _issuer);
        html.Append("</header><section class=\"receiver\"><h3>Datos del receptor</h3><div class=\"receiver-columns\"><div class=\"receiver-left\"><div><b>Nombre / razón social:</b> ")
            .Append(E(remito.Receptor.Nombre)).Append("</div><div><b>Domicilio:</b> ").Append(E(remito.Receptor.Domicilio)).Append("</div></div><div class=\"receiver-right\"><div><b>CUIT:</b> ")
            .Append(remito.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture)).Append("</div><div><b>Condición tributaria:</b> ").Append(E(remito.Receptor.CondicionTributaria)).Append("</div></div></div></section>");
        if (remito.Clase == ClaseRemito.X) html.Append("<section class=\"notes\"><strong>Uso interno:</strong> ").Append(E(remito.AmbitoInterno)).Append("</section>");
        html.Append("<table class=\"details\"><thead><tr><th>Descripción</th><th>Unidad</th><th class=\"number\">Cantidad</th></tr></thead><tbody>");
        foreach (LineaRemito item in remito.Mercaderia)
            html.Append("<tr><td>").Append(E(item.Descripcion)).Append("</td><td>").Append(E(item.Unidad)).Append("</td><td class=\"number\">").Append(FullDecimal(item.Cantidad)).Append("</td></tr>");
        html.Append("</tbody></table>");
        if (remito.Transportista is { } transportista)
            html.Append("<section class=\"notes\"><strong>Transportista:</strong> ").Append(E(transportista.RazonSocial)).Append(" · CUIT: ")
                .Append(transportista.Cuit.ToString(CultureInfo.InvariantCulture)).Append(" · ").Append(E(transportista.Domicilio)).Append("</section>");
        if (remito.Imprenta is { } printer)
            html.Append("<section class=\"notes\"><strong>Imprenta:</strong> ").Append(E(printer.RazonSocial)).Append(" · CUIT: ")
                .Append(printer.Cuit.ToString(CultureInfo.InvariantCulture)).Append(" · Habilitación: ").Append(E(printer.NumeroHabilitacion))
                .Append(" · Impreso ").Append(Date(printer.FechaImpresion)).Append(" · Rango ").Append(printer.PrimerNumero).Append("–").Append(printer.UltimoNumero).Append("</section>");
        html.Append("<footer class=\"authorization\"><strong>").Append(E(remito.Leyenda)).Append("</strong><span>Recibí conforme: ____________________</span></footer></main></body></html>");
        return html.ToString();
    }

    private static void AppendExportFields(StringBuilder html, ExportDocumentFields fields)
    {
        html.Append("<section class=\"export-fields\"><h3>Datos de exportación</h3><div class=\"receiver-columns\">");
        if (fields.CuitPais is not null) html.Append("<div><b>CUIT país del cliente:</b> ").Append(E(fields.CuitPais)).Append("</div>");
        if (fields.ForeignTaxId is not null) html.Append("<div><b>ID impositivo del cliente:</b> ").Append(E(fields.ForeignTaxId)).Append("</div>");
        if (fields.DestinationCountryCode is not null) html.Append("<div><b>Código de país destino:</b> ").Append(E(fields.DestinationCountryCode)).Append("</div>");
        html.Append("<div><b>Moneda:</b> ").Append(E(fields.CurrencyCode)).Append(" · ").Append(E(fields.CurrencyName)).Append("</div>");
        if (fields.ExchangeRate is not null) html.Append("<div><b>Cotización:</b> ").Append(E(fields.ExchangeRate)).Append(" ARS por ").Append(E(fields.CurrencyName)).Append("</div>");
        if (fields.PaymentMethod is not null) html.Append("<div><b>Forma de pago:</b> ").Append(E(fields.PaymentMethod)).Append("</div>");
        if (fields.PaymentDate is not null) html.Append("<div><b>Fecha de pago:</b> ").Append(E(fields.PaymentDate)).Append("</div>");
        if (fields.Incoterms is not null) html.Append("<div><b>Incoterms:</b> ").Append(E(fields.Incoterms)).Append("</div>");
        if (fields.IncotermsDescription is not null) html.Append("<div><b>Descripción de Incoterms:</b> ").Append(E(fields.IncotermsDescription)).Append("</div>");
        if (fields.ExportCondition is not null) html.Append("<div><b>Condición de exportación:</b> ").Append(E(fields.ExportCondition)).Append("</div>");
        if (fields.CommercialObservations is not null) html.Append("<div><b>Observaciones comerciales:</b> ").Append(E(fields.CommercialObservations)).Append("</div>");
        if (fields.Observations is not null) html.Append("<div><b>Observaciones:</b> ").Append(E(fields.Observations)).Append("</div>");
        html.Append("</div></section>");
    }

    private static void AppendIssuerBrand(StringBuilder html, DatosEmisor issuer)
    {
        html.Append("<div class=\"issuer-brand\">");
        if (issuer.LogoPng is { Length: > 0 } logo)
            html.Append("<img class=\"logo\" alt=\"Logo del emisor\" src=\"data:image/png;base64,").Append(Convert.ToBase64String(logo)).Append("\">");
        html.Append("<div class=\"issuer-copy\"><h1>").Append(E(issuer.RazonSocial)).Append("</h1>");
        if (!string.IsNullOrWhiteSpace(issuer.NombreFantasia)) html.Append("<div class=\"issuer-trade-name\">").Append(E(issuer.NombreFantasia)).Append("</div>");
        html.Append("<div>").Append(E(issuer.Domicilio)).Append("</div></div></div>");
    }

    private static void AppendIssuerFiscal(StringBuilder html, DatosEmisor issuer)
    {
        html.Append("<div class=\"issuer-fiscal\"><div>CUIT: ").Append(issuer.Identificacion.ToString(CultureInfo.InvariantCulture))
            .Append("</div><div>IVA: ").Append(E(issuer.CondicionTributaria)).Append("</div>");
        if (!string.IsNullOrWhiteSpace(issuer.IngresosBrutos)) html.Append("<div>Ingresos Brutos: ").Append(E(issuer.IngresosBrutos)).Append("</div>");
        if (issuer.InicioActividades is DateOnly start) html.Append("<div>Inicio de actividades: ").Append(Date(start)).Append("</div>");
        html.Append("</div>");
    }

    private static void AppendInvoiceSupplementalFields(StringBuilder html, ComprobanteGenerado document)
    {
        bool hasPeriod = document.FechaDesde is not null || document.FechaHasta is not null;
        bool hasCurrencyRate = !IsPesoCurrency(document.Moneda) && document.Cotizacion > 0;
        if (!hasPeriod && !hasCurrencyRate) return;
        html.Append("<section class=\"document-extra\">");
        if (document.FechaDesde is DateOnly from) html.Append("<div><b>Período desde:</b> ").Append(Date(from)).Append("</div>");
        if (document.FechaHasta is DateOnly through) html.Append("<div><b>Período hasta:</b> ").Append(Date(through)).Append("</div>");
        if (hasCurrencyRate) html.Append("<div><b>Cotización:</b> ").Append(FullDecimal(document.Cotizacion)).Append(" ARS por ").Append(E(CurrencyName(document.Moneda))).Append("</div>");
        html.Append("</section>");
    }

    private static string ReceiverIdLabel(ComprobanteGenerado document)
    {
        if (document.Identidad?.Servicio.Equals("wsfex", StringComparison.OrdinalIgnoreCase) == true)
            return "ID impositivo extranjero";
        return document.Receptor.CodigoTipoDocumento switch
        {
            80 => "CUIT",
            96 => "DNI",
            86 => "CUIL",
            87 => "CDI",
            _ => "Documento"
        };
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

    private const string Styles = """
        *{box-sizing:border-box}
        body{margin:0;color:#171717;background:#e8e8e8;font:10pt Arial,Helvetica,sans-serif}
        .page{width:210mm;min-height:297mm;margin:18px auto;padding:14mm;background:#fff;box-shadow:0 1px 8px #999;display:flex;flex-direction:column}
        .copy-label{font-weight:700;letter-spacing:.12em;text-align:center;margin:3px 0}
        .issuer{display:grid;grid-template-columns:minmax(0,1fr) 20mm minmax(0,1fr);gap:10px;align-items:start;padding:12px;background:var(--principal);color:var(--principal-text);border-radius:4px}
        .issuer-brand{grid-column:1;display:flex;align-items:center;gap:10px;min-width:0}
        .logo{display:block;flex:none;max-width:38mm;max-height:22mm;object-fit:contain}
        .issuer-copy{min-width:0;line-height:1.35}
        .issuer-copy h1{font-size:14pt;line-height:1.2;margin:0 0 3px}
        .issuer-trade-name{font-weight:700}
        .document-code{grid-column:2;grid-row:1;align-self:start;text-align:center;padding:4px 2px;background:#fff;color:#222;border:1px solid #fff;border-radius:2px}
        .document-code strong{display:block;font-size:22pt;line-height:1.1}
        .document-code small{display:block;font-size:8pt}
        .document-meta{grid-column:3;grid-row:1;text-align:right;font-size:9pt;line-height:1.4}
        .document-meta h2{font-size:13pt;margin:0 0 4px}
        .issuer-fiscal{grid-column:3;grid-row:2;text-align:right;font-size:8pt;line-height:1.35;border-top:1px solid rgba(255,255,255,.5);padding-top:5px}
        .receiver{padding:12px;background:#f2f2f2;border:1px solid #ddd;border-radius:4px;margin-top:10px;line-height:1.5}
        .receiver h3,.references h3,.payments h3,.notes h3,.taxes h3,.transparency h3,.export-fields h3{font-size:10pt;margin:0 0 5px}
        .receiver-columns{display:grid;grid-template-columns:1fr 1fr;gap:4px 18px}
        .receiver-left,.receiver-right{min-width:0}
        .receiver-right{border-left:1px solid #d2d2d2;padding-left:14px}
        .document-extra,.export-fields{margin-top:10px;padding:9px;background:#f7f7f7;border-radius:4px}
        .references{margin-top:12px}
        .details{width:100%;border-collapse:collapse;margin-top:14px}
        .details thead{display:table-header-group}
        .details th,.payments th,.taxes th{background:var(--principal);color:var(--principal-text);text-align:left}
        .details th,.details td,.payments th,.payments td,.taxes th,.taxes td{padding:6px;border-bottom:1px solid #ddd;vertical-align:top}
        .details tbody tr,.payments tbody tr,.taxes tbody tr{background:#fff}
        .number{text-align:right;white-space:nowrap}
        .totals{width:68%;margin:12px 0 0 auto}
        .totals>div,.transparency>div{display:flex;justify-content:space-between;padding:4px;border-bottom:1px solid #ddd}
        .totals .grand-total,.transparency>.grand-total{font-size:12pt;font-weight:700;background:var(--principal);color:var(--principal-text);border:0;border-radius:4px;margin-top:6px;padding:8px}
        .notes,.taxes,.transparency{margin-top:12px}
        .notes p{margin:4px 0}
        .required-legend{display:flex;justify-content:center;align-items:center;gap:14px;border:2px solid #222;border-radius:4px;padding:7px;margin:0 auto 12px;text-align:center;width:100%;font-size:10pt}
        .required-legend strong{font-size:18pt}
        .authorization{display:flex;gap:14px;align-items:center;justify-content:space-between;border-top:1px solid #555;padding-top:9px;margin-top:auto}
        .qr{width:30mm;height:30mm;flex:none}
        .qr-image{display:block;width:100%;height:100%;image-rendering:pixelated}
        .footer-copy{text-align:right;font-size:8pt}
        .footer-copy strong{display:block;font-size:9pt;font-weight:700}
        .payments{margin-top:12px}
        .payments table,.taxes table{width:100%;border-collapse:collapse}
        .export-fields .receiver-columns{font-size:9pt}
        @page{size:A4;margin:14mm}
        @media print{body{background:#fff}.page{width:auto;min-height:269mm;margin:0;padding:0;box-shadow:none}tr{break-inside:avoid}.receiver,.references,.totals,.authorization,.transparency{break-inside:avoid}}
        @media screen and (max-width:700px){.page{width:100%;min-height:100vh;margin:0;padding:18px}.issuer{grid-template-columns:minmax(0,1fr) 18mm;gap:8px;padding:10px}.issuer-brand{grid-column:1;grid-row:1}.document-code{grid-column:2;grid-row:1}.document-meta{grid-column:1/-1;grid-row:2;text-align:left}.issuer-fiscal{grid-column:1/-1;grid-row:3;text-align:left}.logo{max-width:24mm;max-height:18mm}.issuer-copy h1{font-size:12pt}.receiver-columns{grid-template-columns:1fr}.receiver-right{border-left:0;border-top:1px solid #d2d2d2;padding:6px 0 0}.totals,.transparency{width:100%}.details,.taxes{font-size:8pt}.details th,.details td,.taxes th,.taxes td{padding:4px}.authorization{align-items:flex-end}}
        """;
}
