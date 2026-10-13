namespace NetArcaWs.Documents;

public sealed class DatosEmisor
{
    private readonly byte[]? _logoPng;

    public DatosEmisor(long identificacion, string razonSocial, string? nombreFantasia, string domicilio,
        string condicionTributaria, byte[]? logoPng = null, string? ingresosBrutos = null, DateOnly? inicioActividades = null)
    {
        Identificacion = identificacion;
        RazonSocial = razonSocial;
        NombreFantasia = nombreFantasia;
        Domicilio = domicilio;
        CondicionTributaria = condicionTributaria;
        IngresosBrutos = ingresosBrutos;
        InicioActividades = inicioActividades;
        if (logoPng is { Length: > 0 })
        {
            if (logoPng.Length > 1024 * 1024 || !PngValidator.IsValid(logoPng))
                throw new ArgumentException("El logo debe ser un PNG válido de hasta 1 MiB y 2048 px por lado.", nameof(logoPng));
            _logoPng = logoPng.ToArray();
        }
    }

    public long Identificacion { get; }
    public string RazonSocial { get; }
    public string? NombreFantasia { get; }
    public string Domicilio { get; }
    public string CondicionTributaria { get; }
    public string? IngresosBrutos { get; }
    public DateOnly? InicioActividades { get; }
    public byte[]? LogoPng => _logoPng?.ToArray();

    public override string ToString() => nameof(DatosEmisor);
}
