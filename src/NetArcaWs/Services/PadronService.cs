namespace NetArcaWs.Services;

/// <summary>Access to every current operation of the four supported taxpayer registries.</summary>
public sealed class PadronService(PadronA4Service a4, PadronA5Service a5, PadronA10Service a10, PadronA13Service a13)
{
    public PadronA4Service A4 { get; } = a4;
    public PadronA5Service A5 { get; } = a5;
    public PadronA10Service A10 { get; } = a10;
    public PadronA13Service A13 { get; } = a13;
}
