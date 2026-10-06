using NetArcaWs.Cryptography;

namespace NetArcaWs.Wsaa;

public sealed class WsaaOptions
{
    public static readonly Uri HomologationEndpoint = new("https://wsaahomo.afip.gov.ar/ws/services/LoginCms");
    public static readonly Uri ProductionEndpoint = new("https://wsaa.afip.gov.ar/ws/services/LoginCms");

    /// <summary>Optional default signing material held in memory. No filesystem paths are required.</summary>
    public WsaaCertificateContent? Certificate { get; set; }

    public Uri Endpoint { get; set; } = HomologationEndpoint;
    public TimeSpan TraTimeToLive { get; set; } = TimeSpan.FromHours(5);
    public TimeSpan AllowedClockSkew { get; set; } = TimeSpan.FromMinutes(2);
    public int MaxResponseBytes { get; set; } = 1024 * 1024;

    internal void Validate()
    {
        if (Endpoint is null || !Endpoint.IsAbsoluteUri || Endpoint.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(Endpoint.UserInfo) || !string.IsNullOrEmpty(Endpoint.Query) || !string.IsNullOrEmpty(Endpoint.Fragment))
            throw new ArgumentException("Endpoint must be an absolute HTTPS service URL, without credentials, query or fragment.");
        if (TraTimeToLive <= TimeSpan.Zero || TraTimeToLive > TimeSpan.FromHours(24))
            throw new ArgumentOutOfRangeException(nameof(TraTimeToLive));
        if (AllowedClockSkew < TimeSpan.Zero || AllowedClockSkew > TimeSpan.FromMinutes(10))
            throw new ArgumentOutOfRangeException(nameof(AllowedClockSkew));
        if (MaxResponseBytes < 1024 || MaxResponseBytes > 16 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(MaxResponseBytes));
    }
}
