namespace NetArcaWs.Documents;

public sealed class DatosPresentacion
{
    public DatosPresentacion(DatosReceptor? receptor = null, IReadOnlyList<LineaComprobante>? detalles = null,
        IReadOnlyList<ImpuestoComprobante>? otrosImpuestos = null, string? condicionIvaReceptor = null,
        IReadOnlyList<string>? leyendas = null)
    {
        Receptor = receptor;
        Detalles = Array.AsReadOnly(detalles?.ToArray() ?? []);
        OtrosImpuestos = Array.AsReadOnly(otrosImpuestos?.ToArray() ?? []);
        CondicionIvaReceptor = condicionIvaReceptor;
        Leyendas = Array.AsReadOnly(leyendas?.ToArray() ?? []);
    }

    public DatosReceptor? Receptor { get; }
    public IReadOnlyList<LineaComprobante> Detalles { get; }
    public IReadOnlyList<ImpuestoComprobante> OtrosImpuestos { get; }
    public string? CondicionIvaReceptor { get; }
    public IReadOnlyList<string> Leyendas { get; }
    public override string ToString() => nameof(DatosPresentacion);
}
