namespace NetArcaWs.Rendering.Shared;

internal sealed record DocumentLayout(string Letter, string Title, string CodeLabel, string AuthorizationLabel,
    string? RequiredLegend, bool IsFiscal)
{
    internal static string? IvaRate(string? code) => code?.TrimStart('0') switch
    {
        "3" => "0%", "4" => "10,5%", "5" => "21%", "6" => "27%",
        _ => null
    };

    internal static string? CanonicalReceiverCondition(int? code) => code switch
    {
        1 => "IVA responsable inscripto", 4 => "IVA sujeto exento", 5 => "Consumidor final", 6 => "Monotributista",
        _ => null
    };

    internal const string ConsumerLegend = "A CONSUMIDOR FINAL";
    internal const string MonotributoLegend = "El crédito fiscal discriminado en el presente comprobante, solo podrá ser computado a efectos del Régimen de Sostenimiento e Inclusión Fiscal para Pequeños Contribuyentes de la Ley Nº 27.618";

    internal static DocumentLayout For(string service, int type)
    {
        if (service.Equals("wsfe", StringComparison.OrdinalIgnoreCase))
        {
            if (type is 1 or 2 or 3) return new("A", type == 1 ? "Factura" : type == 2 ? "Nota de débito" : "Nota de crédito", "", "CAE", null, true);
            if (type is 6 or 7 or 8) return new("B", type == 6 ? "Factura" : type == 7 ? "Nota de débito" : "Nota de crédito", "", "CAE", null, true);
            if (type is 11 or 12 or 13) return new("C", type == 11 ? "Factura" : type == 12 ? "Nota de débito" : "Nota de crédito", "", "CAE", null, true);
        }
        else if (service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase))
        {
            if (type is 1 or 2 or 3) return new("A", type == 1 ? "Factura" : type == 2 ? "Nota de débito" : "Nota de crédito", "", "CAE", null, true);
            if (type is 6 or 7 or 8) return new("B", type == 6 ? "Factura" : type == 7 ? "Nota de débito" : "Nota de crédito", "", "CAE", null, true);
        }
        else if (service.Equals("wsfex", StringComparison.OrdinalIgnoreCase) && type is 19 or 20 or 21)
            return new("E", type == 19 ? "Factura de exportación" : type == 20 ? "Nota de débito de exportación" : "Nota de crédito de exportación", "", "CAE", null, true);
        throw new NotSupportedException($"No se admite la combinación de servicio {service} y tipo {type} para renderizar.");
    }

    internal static DocumentLayout ReceiptX { get; } = new("X", "Recibo", "", "", "DOCUMENTO NO VALIDO COMO FACTURA", false);
}
