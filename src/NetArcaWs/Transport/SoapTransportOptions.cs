namespace NetArcaWs.Transport;

public sealed class SoapTransportOptions
{
    public int MaxRequestBytes { get; set; } = 4 * 1024 * 1024;
    public int MaxResponseBytes { get; set; } = 4 * 1024 * 1024;
}
