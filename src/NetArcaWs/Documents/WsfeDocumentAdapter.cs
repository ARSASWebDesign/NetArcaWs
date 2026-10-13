using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Invoicing;

namespace NetArcaWs.Documents;

internal static class WsfeDocumentAdapter
{
    internal static void Validate(FecaeRequest request, DatosPresentacion presentation)
    {
        var detail = request.FeDetReq.Single();
        if (request.FeCabReq!.CbteTipo is not (1 or 2 or 3 or 6 or 7 or 8 or 11 or 12 or 13))
            throw new NotSupportedException("La fachada fiscal solo admite comprobantes WSFE A/B/C cubiertos por su representación actual.");
        DatosReceptor recipient = presentation.Receptor ?? throw new ArgumentException("El receptor de la presentación es obligatorio.", nameof(presentation));
        if (recipient.Identificacion != detail.DocNro || recipient.CodigoTipoDocumento is not null && recipient.CodigoTipoDocumento != detail.DocTipo)
            throw new ArgumentException("La identificación del receptor no coincide con el snapshot WSFE.", nameof(presentation));
        decimal total = ArcaWsComprobantes.FromDouble(detail.ImpTotal, nameof(detail.ImpTotal));
        decimal rate = ArcaWsComprobantes.FromDouble(detail.MonCotiz ?? throw new ArgumentException("WSFE requiere cotización."), nameof(detail.MonCotiz));
        decimal net = ArcaWsComprobantes.FromDouble(detail.ImpNeto, nameof(detail.ImpNeto));
        decimal exempt = ArcaWsComprobantes.FromDouble(detail.ImpOpEx, nameof(detail.ImpOpEx));
        decimal nonTaxable = ArcaWsComprobantes.FromDouble(detail.ImpTotConc, nameof(detail.ImpTotConc));
        decimal vatTotal = ArcaWsComprobantes.FromDouble(detail.ImpIva, nameof(detail.ImpIva));
        decimal otherTaxes = ArcaWsComprobantes.FromDouble(detail.ImpTrib, nameof(detail.ImpTrib));
        ComprobanteValidator.ValidateCurrency(detail.MonId);
        if (new[] { total, net, exempt, nonTaxable, vatTotal, otherTaxes }.Any(value => value < 0) || rate <= 0 || total != net + exempt + nonTaxable + vatTotal + otherTaxes)
            throw new ArgumentException("Los importes WSFE deben ser no negativos y sus componentes deben coincidir con el total.", nameof(request));
        _ = ArcaWsComprobantes.ParseDate(detail.CbteFch, "WSFE issue date");
        foreach (Tributo tax in detail.Tributos)
        {
            _ = ArcaWsComprobantes.FromDouble(tax.Importe, "tax amount"); _ = ArcaWsComprobantes.FromDouble(tax.BaseImp, "tax base"); _ = ArcaWsComprobantes.FromDouble(tax.Alic, "tax rate");
        }
        foreach (AlicIva iva in detail.Iva)
        {
            _ = ArcaWsComprobantes.FromDouble(iva.Importe, "VAT amount"); _ = ArcaWsComprobantes.FromDouble(iva.BaseImp, "VAT base");
        }
        if (detail.Iva.Count > 0 && detail.Iva.Sum(iva => ArcaWsComprobantes.FromDouble(iva.Importe, "VAT amount")) != vatTotal)
            throw new ArgumentException("El detalle de alícuotas WSFE no coincide con el IVA del resumen.", nameof(request));
        if (detail.Tributos.Count > 0 && detail.Tributos.Sum(tax => ArcaWsComprobantes.FromDouble(tax.Importe, "tax amount")) != otherTaxes)
            throw new ArgumentException("El detalle de tributos WSFE no coincide con el total de tributos.", nameof(request));
        if (detail.FchServDesde is not null) _ = ArcaWsComprobantes.ParseDate(detail.FchServDesde, "WSFE service start date");
        if (detail.FchServHasta is not null) _ = ArcaWsComprobantes.ParseDate(detail.FchServHasta, "WSFE service end date");
        if (detail.FchVtoPago is not null) _ = ArcaWsComprobantes.ParseDate(detail.FchVtoPago, "WSFE payment due date");
        if (presentation.Detalles.Count > 0)
            throw new ArgumentException("WSFE se representa como resumen y no admite conceptos suplementarios que no estén en el snapshot fiscal.", nameof(presentation));
        ArcaWsComprobantes.ValidateSupplemental(presentation, detail.MonId, total);
        _ = ArcaWsComprobantes.MergeSupplementalTaxes(FiscalTaxes(detail, currency: detail.MonId), presentation, detail.MonId);
    }

    internal static ComprobanteGenerado Map(InvoiceOperation operation, DatosPresentacion presentation)
    {
        FecaeRequest request = ArcaWsComprobantes.Deserialize<FecaeRequest>(operation.Submission.Payload);
        var detail = request.FeDetReq.Single();
        string currency = detail.MonId;
        decimal total = ArcaWsComprobantes.FromDouble(detail.ImpTotal, "total");
        decimal rate = ArcaWsComprobantes.FromDouble(detail.MonCotiz!.Value, "exchange rate");
        var fiscalTaxes = FiscalTaxes(detail, currency);
        return ArcaWsComprobantes.Build(operation, operation.Submission.Identity, presentation, currency, total, [],
            ArcaWsComprobantes.ParseDate(detail.CbteFch, "WSFE issue date"), rate,
            ArcaWsComprobantes.FromDouble(detail.ImpNeto, "net amount"),
            ArcaWsComprobantes.FromDouble(detail.ImpOpEx, "exempt amount"),
            ArcaWsComprobantes.FromDouble(detail.ImpTotConc, "non-taxable amount"),
            ArcaWsComprobantes.FromDouble(detail.ImpIva, "VAT amount"),
            taxes: ArcaWsComprobantes.MergeSupplementalTaxes(fiscalTaxes, presentation, currency),
            expires: ArcaWsComprobantes.ResponseExpiry(operation.ResponseXml), documentType: detail.DocTipo,
            dateFrom: detail.FchServDesde is null ? null : ArcaWsComprobantes.ParseDate(detail.FchServDesde, "WSFE service start date"),
            dateTo: detail.FchServHasta is null ? null : ArcaWsComprobantes.ParseDate(detail.FchServHasta, "WSFE service end date"),
            paymentDate: detail.FchVtoPago is null ? null : ArcaWsComprobantes.ParseDate(detail.FchVtoPago, "WSFE payment due date"),
            ivaConditionCode: detail.CondicionIvaReceptorId);
    }

    private static IReadOnlyList<ImpuestoComprobante> FiscalTaxes(FecaeDetRequest detail, string currency) =>
        detail.Tributos.Select(tax => new ImpuestoComprobante(tax.Desc ?? $"Tributo {tax.Id}",
            ArcaWsComprobantes.FromDouble(tax.Importe, "tax amount"), currency, tax.Id.ToString(),
            ArcaWsComprobantes.FromDouble(tax.BaseImp, "tax base"), ArcaWsComprobantes.FromDouble(tax.Alic, "tax rate")))
        .Concat(detail.Iva.Select(vat => new ImpuestoComprobante("IVA",
            ArcaWsComprobantes.FromDouble(vat.Importe, "VAT amount"), currency, vat.Id.ToString(),
            ArcaWsComprobantes.FromDouble(vat.BaseImp, "VAT base"), Tipo: TipoImpuestoComprobante.Iva))).ToArray();
}
