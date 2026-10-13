namespace NetArcaWs.Documents;

public enum TipoComprobanteGenerado { Fiscal, ReciboX }

public sealed record IdentidadComprobante(string Servicio, string Ambiente, long Cuit, int PuntoVenta, int Tipo, long Numero);

public sealed class ResultadoComprobante
{
    public ResultadoComprobante(string? codigoAutorizacion, DateOnly? vencimiento,
        IReadOnlyList<string> observaciones, IReadOnlyList<string> errores, string? codigoResultado = null)
    {
        CodigoAutorizacion = codigoAutorizacion;
        Vencimiento = vencimiento;
        Observaciones = Array.AsReadOnly(observaciones?.ToArray() ?? []);
        Errores = Array.AsReadOnly(errores?.ToArray() ?? []);
        CodigoResultado = codigoResultado;
    }
    public string? CodigoAutorizacion { get; }
    public DateOnly? Vencimiento { get; }
    public IReadOnlyList<string> Observaciones { get; }
    public IReadOnlyList<string> Errores { get; }
    public string? CodigoResultado { get; }
    public override string ToString() => nameof(ResultadoComprobante);
}

public sealed class ComprobanteGenerado
{
    public ComprobanteGenerado(int version, TipoComprobanteGenerado tipo, IdentidadComprobante? identidad,
        string? tenantId, string estado, DatosReceptor receptor, string moneda, decimal total,
        IReadOnlyList<LineaComprobante> detalles, IReadOnlyList<ImpuestoComprobante> impuestos,
        IReadOnlyList<string> leyendas, string? snapshotRequest, int? snapshotVersion,
        ResultadoComprobante? resultado, string? numeroInterno = null, DateOnly? fecha = null,
        IReadOnlyList<PagoReciboX>? pagos = null, IReadOnlyList<ImputacionReciboX>? imputaciones = null,
        decimal cotizacion = 1m, decimal? neto = null, decimal? exento = null,
        decimal? noGravado = null, decimal? iva = null, DateOnly? fechaDesde = null,
        DateOnly? fechaHasta = null, DateOnly? fechaPago = null)
    {
        Version = version;
        Tipo = tipo;
        Identidad = identidad;
        TenantId = tenantId;
        Estado = estado;
        Receptor = receptor;
        Moneda = moneda;
        Total = total;
        Detalles = Array.AsReadOnly(detalles?.ToArray() ?? []);
        Impuestos = Array.AsReadOnly(impuestos?.ToArray() ?? []);
        Leyendas = Array.AsReadOnly(leyendas?.ToArray() ?? []);
        SnapshotRequest = snapshotRequest;
        SnapshotVersion = snapshotVersion;
        Resultado = resultado;
        NumeroInterno = numeroInterno;
        Fecha = fecha;
        Pagos = Array.AsReadOnly(pagos?.ToArray() ?? []);
        Imputaciones = Array.AsReadOnly(imputaciones?.ToArray() ?? []);
        Cotizacion = cotizacion;
        Neto = neto;
        Exento = exento;
        NoGravado = noGravado;
        Iva = iva;
        FechaDesde = fechaDesde;
        FechaHasta = fechaHasta;
        FechaPago = fechaPago;
    }

    public int Version { get; }
    public TipoComprobanteGenerado Tipo { get; }
    public IdentidadComprobante? Identidad { get; }
    public string? TenantId { get; }
    public string Estado { get; }
    public DatosReceptor Receptor { get; }
    public string Moneda { get; }
    public decimal Total { get; }
    public IReadOnlyList<LineaComprobante> Detalles { get; }
    public IReadOnlyList<ImpuestoComprobante> Impuestos { get; }
    public IReadOnlyList<string> Leyendas { get; }
    public string? SnapshotRequest { get; }
    public int? SnapshotVersion { get; }
    public ResultadoComprobante? Resultado { get; }
    public string? NumeroInterno { get; }
    public DateOnly? Fecha { get; }
    public IReadOnlyList<PagoReciboX> Pagos { get; }
    public IReadOnlyList<ImputacionReciboX> Imputaciones { get; }
    public decimal Cotizacion { get; }
    public decimal? Neto { get; }
    public decimal? Exento { get; }
    public decimal? NoGravado { get; }
    public decimal? Iva { get; }
    public DateOnly? FechaDesde { get; }
    public DateOnly? FechaHasta { get; }
    public DateOnly? FechaPago { get; }

    public override string ToString() => $"ComprobanteGenerado {{ Version = {Version}, Tipo = {Tipo}, Estado = {Estado} }}";
}
