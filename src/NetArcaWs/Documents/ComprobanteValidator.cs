namespace NetArcaWs.Documents;

public static class ComprobanteValidator
{
    public static void Validate(ComprobanteGenerado comprobante)
    {
        ArgumentNullException.ThrowIfNull(comprobante);
        if (comprobante.Version != 1) throw new ArgumentException("Versión de comprobante no soportada.", nameof(comprobante));
        if (comprobante.Detalles is null || comprobante.Impuestos is null || comprobante.Leyendas is null || comprobante.Pagos is null || comprobante.Imputaciones is null)
            throw new ArgumentException("El comprobante contiene colecciones ausentes.", nameof(comprobante));
        if (comprobante.Receptor is null || comprobante.Receptor.Identificacion < 0 || string.IsNullOrWhiteSpace(comprobante.Receptor.Nombre))
            throw new ArgumentException("El receptor es obligatorio y debe estar identificado.", nameof(comprobante));
        ValidateCurrency(comprobante.Moneda);
        if (comprobante.Total < 0) throw new ArgumentException("El total no puede ser negativo.", nameof(comprobante));
        if (comprobante.Detalles.Any(x => x is null ||
            x.Importe < 0 && !(comprobante.Tipo == TipoComprobanteGenerado.Fiscal &&
                string.Equals(comprobante.Identidad?.Servicio, "wsmtxca", StringComparison.OrdinalIgnoreCase) && x.CodigoUnidadMedida == 99) ||
            !SameCurrency(x.Moneda, comprobante.Moneda)))
            throw new ArgumentException("Los detalles deben tener importes y monedas coherentes.", nameof(comprobante));
        if (comprobante.Impuestos.Any(x => x is null || x.Importe < 0 || !SameCurrency(x.Moneda, comprobante.Moneda)))
            throw new ArgumentException("Los impuestos deben tener importes y monedas coherentes.", nameof(comprobante));

        if (comprobante.Tipo == TipoComprobanteGenerado.ReciboX)
        {
            if (comprobante.Identidad is not null || comprobante.Resultado is not null || string.IsNullOrWhiteSpace(comprobante.NumeroInterno) || comprobante.Fecha is null)
                throw new ArgumentException("El recibo X no puede contener identidad ni autorización fiscal y requiere número y fecha.", nameof(comprobante));
            if (comprobante.Detalles.Sum(x => x.Importe) != comprobante.Total)
                throw new ArgumentException("La suma de detalles debe coincidir con el total del recibo.", nameof(comprobante));
            if (comprobante.Pagos.Count == 0 || comprobante.Pagos.Any(x => x.Importe < 0 || !SameCurrency(x.Moneda, comprobante.Moneda)) || comprobante.Pagos.Sum(x => x.Importe) != comprobante.Total)
                throw new ArgumentException("Los pagos deben sumar el total y usar su misma moneda.", nameof(comprobante));
            if (comprobante.Imputaciones.Any(x => string.IsNullOrWhiteSpace(x.Referencia) || x.Importe <= 0 || !SameCurrency(x.Moneda, comprobante.Moneda)) || comprobante.Imputaciones.Sum(x => x.Importe) > comprobante.Total)
                throw new ArgumentException("Las imputaciones deben ser positivas, usar la moneda del recibo y no superar lo recibido.", nameof(comprobante));
        }
        else if (comprobante.Tipo == TipoComprobanteGenerado.Fiscal)
        {
            if (comprobante.Identidad is null || string.IsNullOrWhiteSpace(comprobante.Estado))
                throw new ArgumentException("El documento fiscal requiere identidad y estado.", nameof(comprobante));
            if (comprobante.Fecha is null || comprobante.Cotizacion <= 0)
                throw new ArgumentException("El documento fiscal requiere fecha y cotización positivas.", nameof(comprobante));
            if (comprobante.Neto < 0 || comprobante.Exento < 0 || comprobante.NoGravado < 0 || comprobante.Iva < 0)
                throw new ArgumentException("Los totales fiscales no pueden ser negativos.", nameof(comprobante));
            if (comprobante.Impuestos.Any(x => !SameCurrency(x.Moneda, comprobante.Moneda)))
                throw new ArgumentException("Los impuestos deben usar la moneda del comprobante.", nameof(comprobante));
        }
        else throw new ArgumentException("Tipo de comprobante desconocido.", nameof(comprobante));
    }

    internal static bool SameCurrency(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    internal static void ValidateCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || currency.Any(c => !char.IsAsciiLetter(c)))
            throw new ArgumentException("La moneda debe ser un código alfabético de tres letras.", nameof(currency));
    }
}
