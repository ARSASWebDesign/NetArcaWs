using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NetArcaWs.Cryptography;
using NetArcaWs.Multitenancy;
using NetArcaWs.HealthChecks;

namespace NetArcaWs.Wsaa;

/// <summary>Native asynchronous port of NetArcaWs WSAA authentication and ticket operations.</summary>
public sealed partial class WsaaService
{
    public const string HttpClientName = "NetArcaWs.Wsaa";
    private static readonly ConditionalWeakTable<IMemoryCache, SemaphoreSlim[]> CacheLocks = new();
    private readonly IHttpClientFactory clients;
    private readonly IMemoryCache cache;
    private readonly TimeProvider clock;
    private readonly WsaaOptions settings;

    public WsaaService(IHttpClientFactory clients, IMemoryCache cache, IOptions<WsaaOptions> options, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);
        options.Value.Validate();
        this.clients = clients;
        this.cache = cache;
        this.clock = clock;
        settings = new WsaaOptions
        {
            Certificate = options.Value.Certificate,
            Endpoint = options.Value.Endpoint, TraTimeToLive = options.Value.TraTimeToLive,
            AllowedClockSkew = options.Value.AllowedClockSkew, MaxResponseBytes = options.Value.MaxResponseBytes
        };
    }

    /// <summary>Creates the UTF-8 TRA. Like create_tra, TTL spans both sides of the current instant.</summary>
    public string CreateTra(string service, TimeSpan? ttl = null)
    {
        ValidateService(service);
        var lifetime = ttl ?? settings.TraTimeToLive;
        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromHours(24))
            throw new ArgumentOutOfRangeException(nameof(ttl));
        var now = clock.GetUtcNow();
        return WsaaXml.Serialize(new LoginTicketRequestDto
        {
            Service = service,
            Header = new TicketHeaderDto
            {
                UniqueId = checked((uint)now.ToUnixTimeSeconds()).ToString(CultureInfo.InvariantCulture),
                GenerationTime = (now - lifetime).ToString("O", CultureInfo.InvariantCulture),
                ExpirationTime = (now + lifetime).ToString("O", CultureInfo.InvariantCulture)
            }
        });
    }

    /// <summary>Authenticates with the in-memory certificate configured in WsaaOptions.</summary>
    public Task<WsaaTicket> AuthenticateAsync(string service, CancellationToken cancellationToken = default)
    {
        ValidateService(service);
        cancellationToken.ThrowIfCancellationRequested();
        return AuthenticateWithContentAsync(service, settings.Certificate
            ?? throw new InvalidOperationException("Configure WsaaOptions.Certificate or supply signing material explicitly."), cancellationToken);
    }

    /// <summary>
    /// Authenticates with content supplied by a vault or database, without certificate files.
    /// Imports and disposes its own certificate per call. For tenant isolation, use AuthenticateForTenantAsync.
    /// </summary>
    public async Task<WsaaTicket> AuthenticateWithContentAsync(string service, WsaaCertificateContent content,
        CancellationToken cancellationToken = default)
    {
        ValidateService(service);
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();
        using var certificate = content.LoadCertificate();
        return await AuthenticateAsync(service, certificate, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Authenticates for an explicitly authorized tenant. Selects its environment and isolates its ticket cache.
    /// WSAA does not validate the represented CUIT; authorization is enforced by each business service.
    /// </summary>
    public async Task<WsaaTicket> AuthenticateForTenantAsync(string service, ArcaTenantContext tenant,
        CancellationToken cancellationToken = default)
    {
        ValidateService(service);
        ArgumentNullException.ThrowIfNull(tenant);
        cancellationToken.ThrowIfCancellationRequested();
        var endpoint = tenant.Environment == ArcaEnvironment.Production
            ? WsaaOptions.ProductionEndpoint : WsaaOptions.HomologationEndpoint;
        using var certificate = tenant.Certificate.LoadCertificate();
        return await AuthenticateCoreAsync(service, certificate, endpoint, tenant.TenantId, tenant.Cuit, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reuses the TA until its actual expiration. Concurrent callers share one login per cache key.</summary>
    public Task<WsaaTicket> AuthenticateAsync(string service, X509Certificate2 certificate,
        CancellationToken cancellationToken = default)
        => AuthenticateCoreAsync(service, certificate, settings.Endpoint, null, null, cancellationToken);

    private async Task<WsaaTicket> AuthenticateCoreAsync(string service, X509Certificate2 certificate,
        Uri endpoint, string? tenantId, long? cuit, CancellationToken cancellationToken)
    {
        ValidateService(service);
        ArgumentNullException.ThrowIfNull(certificate);
        cancellationToken.ThrowIfCancellationRequested();
        if (!certificate.HasPrivateKey)
            throw new CryptographicException("A certificate with its private key is required.");
        var now = clock.GetUtcNow();
        if (now < certificate.NotBefore.ToUniversalTime() || now >= certificate.NotAfter.ToUniversalTime())
            throw new CryptographicException("The signing certificate is outside its validity period.");
        var key = new TicketCacheKey(tenantId, cuit, endpoint.AbsoluteUri, service,
            certificate.GetCertHashString(HashAlgorithmName.SHA256));
        if (TryGetTicket(key, out var cached)) return cached!;

        // Bounded striped locks are shared by all service instances using the same IMemoryCache.
        var locks = CacheLocks.GetValue(cache, _ => Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray());
        var gate = locks[(int)((uint)key.GetHashCode() % (uint)locks.Length)];
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (TryGetTicket(key, out cached)) return cached!;
            var cms = WsaaCryptography.SignTra(CreateTra(service), certificate);
            var ticket = await LoginCmsCoreAsync(cms, endpoint, cancellationToken).ConfigureAwait(false);
            var remaining = ticket.ExpirationTime - clock.GetUtcNow();
            if (remaining <= TimeSpan.Zero) throw new FormatException("WSAA returned an expired ticket.");
            cache.Set(key, ticket, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = remaining, Size = 1 });
            return ticket;
        }
        finally { gate.Release(); }
    }

    /// <summary>Calls loginCms directly without caching. SOAP faults retain ARCA's code and detail.</summary>
    public Task<WsaaTicket> LoginCmsAsync(string cms, CancellationToken cancellationToken = default)
        => LoginCmsCoreAsync(cms, settings.Endpoint, cancellationToken);

    private async Task<WsaaTicket> LoginCmsCoreAsync(string cms, Uri endpoint, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cms);
        var payload = WsaaXml.Serialize(new WsaaSoapEnvelopeDto
        {
            Body = new WsaaSoapBodyDto { Request = new LoginCmsDto { Cms = cms } }
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(payload, Encoding.UTF8, "text/xml")
        };
        request.Headers.TryAddWithoutValidation("SOAPAction", "\"\"");
        using var client = clients.CreateClient(HttpClientName);
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (client.Timeout != Timeout.InfiniteTimeSpan) operation.CancelAfter(client.Timeout);
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, operation.Token).ConfigureAwait(false);
            var xml = await ReadResponseAsync(response.Content, operation.Token).ConfigureAwait(false);
            return ParseResponse(response, xml);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && operation.IsCancellationRequested)
        {
            throw new TaskCanceledException("The WSAA operation exceeded the configured HTTP timeout.", new TimeoutException("WSAA timeout.", ex), operation.Token);
        }
    }

    private WsaaTicket ParseResponse(HttpResponseMessage response, string xml)
    {
        WsaaSoapEnvelopeDto envelope;
        try { envelope = WsaaXml.Deserialize<WsaaSoapEnvelopeDto>(xml); }
        catch (FormatException) when (!response.IsSuccessStatusCode)
        {
            response.EnsureSuccessStatusCode();
            throw;
        }
        if (envelope.Body?.Fault is { } fault)
            throw new WsaaSoapException(fault.Code ?? "unknown", fault.Message ?? "Unspecified SOAP fault",
                fault.Detail?.OuterXml, response.StatusCode);
        response.EnsureSuccessStatusCode();
        var ticket = ParseTicket(envelope.Body?.Response?.Ticket ?? throw new FormatException("SOAP response has no loginCmsReturn."));
        var now = clock.GetUtcNow();
        if (ticket.IsExpired(now)) throw new FormatException("WSAA returned an expired ticket.");
        if (ticket.GenerationTime > now + settings.AllowedClockSkew)
            throw new FormatException("WSAA returned a ticket generated in the future. Check clock synchronization.");
        return ticket;
    }

    /// <summary>Parses a persisted or received TA without requiring that it is still valid.</summary>
    public static WsaaTicket ParseTicket(string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);
        var dto = WsaaXml.Deserialize<LoginTicketResponseDto>(xml);
        var header = dto.Header ?? throw new FormatException("Missing ticket header.");
        var credentials = dto.Credentials ?? throw new FormatException("Missing ticket credentials.");
        if (dto.Version != "1.0" || string.IsNullOrWhiteSpace(header.Source) || string.IsNullOrWhiteSpace(header.Destination) ||
            string.IsNullOrWhiteSpace(credentials.Token) || string.IsNullOrWhiteSpace(credentials.Sign) ||
            !uint.TryParse(header.UniqueId, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            throw new FormatException("Invalid or missing ticket fields.");
        var generation = ParseTimestamp(header.GenerationTime);
        var expiration = ParseTimestamp(header.ExpirationTime);
        if (expiration <= generation) throw new FormatException("Ticket expiration must follow generation.");
        return new WsaaTicket(credentials.Token, credentials.Sign, generation, expiration,
            header.Source, header.Destination, id, xml);
    }

    private bool TryGetTicket(TicketCacheKey key, out WsaaTicket? ticket)
    {
        if (cache.TryGetValue(key, out ticket) && ticket is not null && !ticket.IsExpired(clock.GetUtcNow())) return true;
        return false;
    }

    private async Task<string> ReadResponseAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > settings.MaxResponseBytes) throw new FormatException("WSAA response exceeds the configured size limit.");
        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) != 0)
        {
            if (buffer.Length + read > settings.MaxResponseBytes) throw new FormatException("WSAA response exceeds the configured size limit.");
            buffer.Write(chunk, 0, read);
        }
        return new UTF8Encoding(false, true).GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    private static DateTimeOffset ParseTimestamp(string? value)
    {
        if (value is null || !TimestampPattern().IsMatch(value)) throw new FormatException("Ticket timestamps must be XML dateTimes with a time zone.");
        try { return System.Xml.XmlConvert.ToDateTimeOffset(value); }
        catch (ArgumentException ex) { throw new FormatException("Invalid ticket timestamp.", ex); }
    }

    private static void ValidateService(string service)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        if (!ServicePattern().IsMatch(service)) throw new ArgumentException("Service must contain 3 to 32 ASCII letters, digits or underscores, starting with a letter.", nameof(service));
    }

    [GeneratedRegex("\\A[A-Za-z][A-Za-z0-9_]{2,31}\\z", RegexOptions.CultureInvariant)]
    private static partial Regex ServicePattern();
    [GeneratedRegex("T.*(?:Z|[+-][0-9]{2}:[0-9]{2})\\z", RegexOptions.CultureInvariant)]
    private static partial Regex TimestampPattern();
    private sealed record TicketCacheKey(string? TenantId, long? Cuit, string Endpoint, string Service, string CertificateHash);
}
