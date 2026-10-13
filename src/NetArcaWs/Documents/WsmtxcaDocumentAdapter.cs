using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.Invoicing;

namespace NetArcaWs.Documents;

internal static class WsmtxcaDocumentAdapter
{
    internal static void Validate(ComprobanteType request, DatosPresentacion presentation)
    {
        if (request.CodigoTipoComprobante is not (1 or 2 or 3 or 6 or 7 or 8))
            throw new NotSupportedException("La fachada fiscal solo admite los tipos WSMTXCA 1, 2, 3, 6, 7 y 8.");
        DatosReceptor recipient = presentation.Receptor ?? throw new ArgumentException("El receptor de la presentación es obligatorio.", nameof(presentation));
        if (request.CodigoTipoDocumento is not null && request.NumeroDocumento is not null &&
            (recipient.CodigoTipoDocumento is not null && recipient.CodigoTipoDocumento != request.CodigoTipoDocumento ||
             recipient.Identificacion != request.NumeroDocumento))
            throw new ArgumentException("La identificación del receptor no coincide con el snapshot WSMTXCA.", nameof(presentation));
        ComprobanteValidator.ValidateCurrency(request.CodigoMoneda);
        if (request.ImporteTotal < 0 || request.CotizacionMoneda is not > 0)
            throw new ArgumentException("Los importes WSMTXCA deben ser finitos y no negativos, con cotización positiva.", nameof(request));
        decimal expectedTotal = request.ImporteSubtotal + (request.ImporteOtrosTributos ?? 0m) + request.ArraySubtotalesIva.Sum(item => item.Importe);
        if (!AmountsWithinWsmtxcaTolerance(request.ImporteTotal, expectedTotal))
            throw new ArgumentException("El subtotal, IVA y otros tributos WSMTXCA no coinciden con el total.", nameof(request));
        if (request.ArrayOtrosTributos.Count > 0 && request.ArrayOtrosTributos.Sum(tax => tax.Importe) != request.ImporteOtrosTributos)
            throw new ArgumentException("Los otros tributos WSMTXCA no coinciden con su total.", nameof(request));
        if (request.FechaEmision is null) throw new ArgumentException("WSMTXCA requiere una fecha explícita.", nameof(request));
        foreach (ItemType item in request.ArrayItems)
            if (item.Cantidad is < 0 || item.PrecioUnitario is < 0 || item.ImporteBonificacion is < 0 ||
                (item.ImporteItem < 0 || item.ImporteIva is < 0) && item.CodigoUnidadMedida != 99)
                throw new ArgumentException("Los conceptos WSMTXCA contienen cantidades o importes inválidos.", nameof(request));
        ArcaWsComprobantes.ValidateSupplemental(presentation, request.CodigoMoneda, request.ImporteTotal);
        _ = ArcaWsComprobantes.MergeSupplementalTaxes(FiscalTaxes(request, request.CodigoMoneda), presentation, request.CodigoMoneda);
        _ = ArcaWsComprobantes.ApplyPresentationDescriptions(request.ArrayItems.Select(item => new LineaComprobante(
            item.Descripcion, item.ImporteItem, request.CodigoMoneda, item.Codigo ?? item.CodigoMtx,
            item.Cantidad, item.CodigoUnidadMedida, item.PrecioUnitario)).ToArray(), presentation, request.CodigoMoneda);
    }

    private static bool AmountsWithinWsmtxcaTolerance(decimal actual, decimal expected)
    {
        decimal difference = Math.Abs(actual - expected);
        decimal scale = Math.Max(Math.Abs(actual), Math.Abs(expected));
        return difference <= 0.01m || scale > 0 && difference / scale <= 0.0001m;
    }

    internal static ComprobanteGenerado Map(InvoiceOperation operation, DatosPresentacion presentation)
    {
        ComprobanteType request = ArcaWsComprobantes.Deserialize<ComprobanteType>(operation.Submission.Payload);
        string currency = request.CodigoMoneda;
        var lines = request.ArrayItems.Select(item => new LineaComprobante(item.Descripcion,
            item.ImporteItem, currency, item.Codigo ?? item.CodigoMtx, item.Cantidad, item.CodigoUnidadMedida,
            item.PrecioUnitario)).ToArray();
        lines = ArcaWsComprobantes.ApplyPresentationDescriptions(lines, presentation, currency).ToArray();
        var taxes = ArcaWsComprobantes.MergeSupplementalTaxes(FiscalTaxes(request, currency), presentation, currency);
        decimal? rate = request.CotizacionMoneda;
        if (rate is null) throw new InvalidOperationException("The frozen WSMTXCA snapshot does not contain its exchange rate.");
        int? documentType = request.CodigoTipoDocumento;
        DateOnly date = DateOnly.FromDateTime(request.FechaEmision!.Value);
        return ArcaWsComprobantes.Build(operation, operation.Submission.Identity, presentation, currency,
            request.ImporteTotal, lines, date, rate.Value, request.ImporteGravado, request.ImporteExento,
            request.ImporteNoGravado, request.ArraySubtotalesIva.Sum(x => x.Importe), taxes,
            ArcaWsComprobantes.ResponseExpiry(operation.ResponseXml), documentType,
            request.FechaServicioDesde is null ? null : DateOnly.FromDateTime(request.FechaServicioDesde.Value),
            request.FechaServicioHasta is null ? null : DateOnly.FromDateTime(request.FechaServicioHasta.Value),
            request.FechaVencimientoPago is null ? null : DateOnly.FromDateTime(request.FechaVencimientoPago.Value),
            request.CondicionIvaReceptor);
    }

    private static IReadOnlyList<ImpuestoComprobante> FiscalTaxes(ComprobanteType request, string currency) =>
        request.ArraySubtotalesIva.Select(vat => new ImpuestoComprobante("IVA", vat.Importe, currency,
            vat.Codigo.ToString(), Tipo: TipoImpuestoComprobante.Iva)).Concat(request.ArrayOtrosTributos.Select(tax => new ImpuestoComprobante(
                tax.Descripcion ?? tax.Codigo.ToString(), tax.Importe, currency, tax.Codigo.ToString(),
                tax.BaseImponible))).ToArray();
}
