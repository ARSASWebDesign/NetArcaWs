namespace NetArcaWs.Documents;

public sealed record DatosReceptor(long Identificacion, string Nombre, string? Domicilio, string? CondicionTributaria)
{
    public override string ToString() => nameof(DatosReceptor);
}

public sealed record LineaComprobante(string Descripcion, decimal Importe, string Moneda);

public sealed record ImpuestoComprobante(string Nombre, decimal Importe, string Moneda);

public sealed record PagoReciboX(string Medio, decimal Importe, string Moneda);

public sealed record ImputacionReciboX(string Referencia, decimal Importe, string Moneda);

public sealed record LineaReciboX(string Descripcion, decimal Importe, string Moneda);

public sealed record DatosReciboX(
    string NumeroInterno,
    DateOnly Fecha,
    DatosReceptor Receptor,
    string Moneda,
    decimal Total,
    IReadOnlyList<LineaReciboX> Detalles,
    IReadOnlyList<PagoReciboX> Pagos,
    IReadOnlyList<ImputacionReciboX>? Imputaciones = null)
{
    public override string ToString() => nameof(DatosReciboX);
}
