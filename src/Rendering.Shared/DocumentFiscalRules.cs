using NetArcaWs.Documents;

namespace NetArcaWs.Rendering.Shared;

internal static class DocumentFiscalRules
{
    internal const string ConsumerTransparencyTitle = "Régimen de Transparencia Fiscal al Consumidor (Ley 27.743)";
    internal const string ConsumerIvaLabel = "IVA contenido";
    internal const string ConsumerOtherTaxesLabel = "Otros impuestos nacionales indirectos";
    internal const string NetTotalLabel = "Importe neto";
    internal const string IvaTotalLabel = "IVA total";

    internal static void ValidateConditions(ComprobanteGenerado document, DocumentLayout layout)
    {
        if (!layout.IsFiscal) return;
        foreach (string? supplied in new[] { document.CondicionIvaReceptor, document.Receptor.CondicionTributaria })
            if (!string.IsNullOrWhiteSpace(supplied) && !ConditionMatches(document.CodigoCondicionIvaReceptor, supplied))
                throw new InvalidOperationException("La condición de IVA informada contradice el código fiscal correlacionado.");
        if (document.Identidad!.Tipo is 1 or 2 or 3 && document.CodigoCondicionIvaReceptor == 5)
            throw new NotSupportedException("La combinación de comprobante A y consumidor final no está habilitada para renderizar.");
        if (document.CodigoCondicionIvaReceptor == 5)
        {
            if (document.Iva is null) throw new InvalidOperationException("Falta el IVA contenido requerido para transparencia al consumidor.");
            foreach (ImpuestoComprobante tax in document.Impuestos.Where(tax => tax.Tipo == TipoImpuestoComprobante.OtroTributo))
                if (tax.Clasificacion is not ("NacionalIndirecto" or "NoNacionalIndirecto" or "NoIndirecto"))
                    throw new InvalidOperationException("La clasificación fiscal de cada otro tributo es obligatoria para la transparencia al consumidor.");
        }
    }

    internal static string ReceiverCondition(ComprobanteGenerado document) => DocumentLayout.CanonicalReceiverCondition(document.CodigoCondicionIvaReceptor)
        ?? document.CondicionIvaReceptor ?? document.Receptor.CondicionTributaria ?? string.Empty;

    private static bool ConditionMatches(int? code, string condition) => code switch
    {
        1 => condition.Contains("responsable inscripto", StringComparison.OrdinalIgnoreCase) || condition.Contains("responsable inscrito", StringComparison.OrdinalIgnoreCase),
        4 => condition.Contains("exent", StringComparison.OrdinalIgnoreCase),
        5 => condition.Contains("consumidor final", StringComparison.OrdinalIgnoreCase),
        6 => condition.Contains("monotrib", StringComparison.OrdinalIgnoreCase),
        _ => true
    };

    internal static IReadOnlyList<string> DocumentLegends(ComprobanteGenerado document)
    {
        var legends = document.Leyendas.ToList();
        if (document.Identidad!.Tipo is 1 or 2 or 3 && document.CodigoCondicionIvaReceptor == 6 &&
            !legends.Contains(DocumentLayout.MonotributoLegend, StringComparer.OrdinalIgnoreCase)) legends.Insert(0, DocumentLayout.MonotributoLegend);
        if (document.Identidad.Tipo is 6 or 7 or 8 or 11 or 12 or 13 && document.CodigoCondicionIvaReceptor == 5 &&
            !legends.Contains(DocumentLayout.ConsumerLegend, StringComparer.OrdinalIgnoreCase)) legends.Insert(0, DocumentLayout.ConsumerLegend);
        return legends;
    }
}
