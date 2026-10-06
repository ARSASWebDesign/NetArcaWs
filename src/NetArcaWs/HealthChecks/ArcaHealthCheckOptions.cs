namespace NetArcaWs.HealthChecks;

public enum ArcaService { Wsaa, Wsfev1, Wsfexv1, Wsmtxca, PadronA4, PadronA5, PadronA10, PadronA13, Wscdc, Wsfecred, Wscpe }
public enum ArcaEnvironment { Homologation, Production }

public sealed class ArcaHealthCheckOptions
{
    public ArcaService Service { get; set; }
    public ArcaEnvironment Environment { get; set; } = ArcaEnvironment.Homologation;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    public Uri? Endpoint { get; set; }
    public int MaxResponseBytes { get; set; } = 128 * 1024;

    internal ArcaHealthCheckOptions Snapshot()
    {
        if (!Enum.IsDefined(Service)) throw new ArgumentOutOfRangeException(nameof(Service));
        if (!Enum.IsDefined(Environment)) throw new ArgumentOutOfRangeException(nameof(Environment));
        if (Timeout <= TimeSpan.Zero || Timeout > TimeSpan.FromMinutes(5)) throw new ArgumentOutOfRangeException(nameof(Timeout));
        if (MaxResponseBytes < 1024 || MaxResponseBytes > 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(MaxResponseBytes));
        if (Endpoint is not null && (!Endpoint.IsAbsoluteUri || Endpoint.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(Endpoint.UserInfo) || !string.IsNullOrEmpty(Endpoint.Query) || !string.IsNullOrEmpty(Endpoint.Fragment)))
            throw new ArgumentException("Endpoint must be an HTTPS service URL without query, fragment or credentials.", nameof(Endpoint));
        return new ArcaHealthCheckOptions
        {
            Service = Service, Environment = Environment, Timeout = Timeout,
            Endpoint = Endpoint, MaxResponseBytes = MaxResponseBytes
        };
    }
}
