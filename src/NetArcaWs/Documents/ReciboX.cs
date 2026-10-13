namespace NetArcaWs.Documents;

public sealed class ReciboX(DatosEmisor emisor)
{
    private readonly DatosEmisor _emisor = emisor ?? throw new ArgumentNullException(nameof(emisor));

    public ComprobanteGenerado GenerarComprobante(DatosReciboX datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        if (_emisor.Identificacion is < 10_000_000_000 or > 99_999_999_999 || string.IsNullOrWhiteSpace(_emisor.RazonSocial) || string.IsNullOrWhiteSpace(_emisor.Domicilio) || string.IsNullOrWhiteSpace(_emisor.CondicionTributaria))
            throw new ArgumentException("El perfil del emisor está incompleto.", nameof(emisor));
        if (datos.Receptor is null || datos.Receptor.Identificacion < 0 || string.IsNullOrWhiteSpace(datos.Receptor.Nombre))
            throw new ArgumentException("El receptor es obligatorio y debe estar identificado.", nameof(datos));
        if (string.IsNullOrWhiteSpace(datos.NumeroInterno)) throw new ArgumentException("Se requiere número interno.", nameof(datos));
        ComprobanteValidator.ValidateCurrency(datos.Moneda);
        if (datos.Total <= 0 || datos.Detalles is null || datos.Pagos is null) throw new ArgumentException("El total, los detalles y los pagos son obligatorios.", nameof(datos));
        var details = datos.Detalles.Select(x => new LineaComprobante(x.Descripcion, x.Importe, x.Moneda)).ToArray();
        var result = new ComprobanteGenerado(1, TipoComprobanteGenerado.ReciboX, null, null, "Local",
            datos.Receptor, datos.Moneda, datos.Total, details, [], [], null, null, null,
            datos.NumeroInterno, datos.Fecha, datos.Pagos.ToArray(), datos.Imputaciones?.ToArray());
        ComprobanteValidator.Validate(result);
        return result;
    }
}
