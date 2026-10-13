using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Invoicing;

namespace NetArcaWs.Documents;

internal static class WsfexDocumentAdapter
{
    internal static void Validate(ClsFexRequest request, DatosPresentacion presentation)
    {
        if (request.CbteTipo is not (19 or 20 or 21))
            throw new NotSupportedException("La fachada fiscal solo admite los tipos WSFEX E 19, 20 y 21.");
        _ = ArcaWsComprobantes.ParseDate(request.FechaCbte, "WSFEX issue date");
        DatosReceptor recipient = presentation.Receptor ?? throw new ArgumentException("El receptor de la presentación es obligatorio.", nameof(presentation));
        if (string.IsNullOrWhiteSpace(request.Cliente) || !string.Equals(recipient.Nombre, request.Cliente, StringComparison.Ordinal) ||
            recipient.Domicilio is not null && !string.Equals(recipient.Domicilio, request.DomicilioCliente, StringComparison.Ordinal))
            throw new ArgumentException("El nombre y domicilio del receptor deben coincidir con el snapshot WSFEX.", nameof(presentation));
        if (!string.IsNullOrWhiteSpace(recipient.IdentificacionFiscal))
        {
            if (!string.Equals(request.IdImpositivo, recipient.IdentificacionFiscal, StringComparison.Ordinal))
                throw new ArgumentException("La identificación fiscal extranjera no coincide con el snapshot WSFEX.", nameof(presentation));
        }
        else if (request.CuitPaisCliente > 0 && recipient.Identificacion != request.CuitPaisCliente)
            throw new ArgumentException("La identificación del receptor no coincide con el snapshot WSFEX.", nameof(presentation));
        ComprobanteValidator.ValidateCurrency(request.MonedaId);
        if (request.ImpTotal < 0 || request.MonedaCtz is not > 0)
            throw new ArgumentException("Los importes WSFEX deben ser finitos y no negativos, con cotización positiva.", nameof(request));
        foreach (Item item in request.Items)
            if (item.ProQty < 0 || item.ProPrecioUni < 0 || item.ProTotalItem < 0)
                throw new ArgumentException("Los conceptos WSFEX contienen cantidades o importes inválidos.", nameof(request));
        if (request.Items.Count > 0 && request.Items.Sum(item => item.ProTotalItem) != request.ImpTotal)
            throw new ArgumentException("La suma de conceptos WSFEX no coincide con el total fiscal.", nameof(request));
        ArcaWsComprobantes.ValidateSupplemental(presentation, request.MonedaId, request.ImpTotal);
        _ = ArcaWsComprobantes.ApplyPresentationDescriptions(request.Items.Select(item => new LineaComprobante(
            item.ProDs ?? item.ProCodigo ?? string.Empty, item.ProTotalItem, request.MonedaId, item.ProCodigo,
            item.ProQty, item.ProUmed, item.ProPrecioUni)).ToArray(), presentation, request.MonedaId);
    }

    internal static ComprobanteGenerado Map(InvoiceOperation operation, DatosPresentacion presentation)
    {
        ClsFexRequest request = ArcaWsComprobantes.Deserialize<ClsFexRequest>(operation.Submission.Payload);
        string currency = request.MonedaId;
        var lines = request.Items.Select(item => new LineaComprobante(item.ProDs ?? item.ProCodigo ?? "",
            item.ProTotalItem, currency, item.ProCodigo, item.ProQty, item.ProUmed, item.ProPrecioUni)).ToArray();
        lines = ArcaWsComprobantes.ApplyPresentationDescriptions(lines, presentation, currency).ToArray();
        DatosReceptor persistedRecipient = new(request.CuitPaisCliente, request.Cliente, request.DomicilioCliente,
            presentation.Receptor?.CondicionTributaria, presentation.Receptor?.CodigoTipoDocumento,
            request.IdImpositivo ?? presentation.Receptor?.IdentificacionFiscal);
        DatosPresentacion persistedPresentation = ArcaWsComprobantes.WithReceptor(presentation, persistedRecipient);
        return ArcaWsComprobantes.Build(operation, operation.Submission.Identity, persistedPresentation, currency,
            request.ImpTotal, lines, ArcaWsComprobantes.ParseDate(request.FechaCbte, "WSFEX issue date"), request.MonedaCtz!.Value,
            taxes: ArcaWsComprobantes.AddSupplementalTaxes([], presentation),
            expires: ArcaWsComprobantes.ResponseExpiry(operation.ResponseXml),
            paymentDate: string.IsNullOrWhiteSpace(request.FechaPago) ? null : ArcaWsComprobantes.ParseDate(request.FechaPago, "WSFEX payment date"));
    }
}
