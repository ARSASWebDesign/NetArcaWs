using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Transport;
using NetArcaWs.Wsaa;

namespace NetArcaWs.IntegrationTests;

public interface IProtectedHomologationWsaaLogin
{
    Task<WsaaTicket> AuthenticateForTenantWithoutCacheAsync(
        string service,
        ArcaTenantContext tenant,
        CancellationToken cancellationToken = default);
}

public sealed class ProtectedHomologationWsaaLogin(WsaaService wsaa) : IProtectedHomologationWsaaLogin
{
    public Task<WsaaTicket> AuthenticateForTenantWithoutCacheAsync(
        string service,
        ArcaTenantContext tenant,
        CancellationToken cancellationToken = default)
        => wsaa.AuthenticateForTenantWithoutCacheAsync(service, tenant, cancellationToken);
}

/// <summary>Integration-harness ticket provider backed by the protected cross-run state bundle.</summary>
public sealed class ProtectedWsaaTicketProvider : IArcaTicketProvider
{
    private static readonly TimeSpan LoginCooldown = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LoginWindow = TimeSpan.FromSeconds(30);
    private readonly ProtectedHomologationStateSession state;
    private readonly IProtectedHomologationWsaaLogin wsaa;
    private readonly TimeProvider clock;
    private readonly SemaphoreSlim[] locks = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public ProtectedWsaaTicketProvider(ProtectedHomologationStateSession state, WsaaService wsaa, TimeProvider clock)
        : this(state, new ProtectedHomologationWsaaLogin(wsaa), clock)
    {
    }

    public ProtectedWsaaTicketProvider(
        ProtectedHomologationStateSession state,
        IProtectedHomologationWsaaLogin wsaa,
        TimeProvider clock)
    {
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        this.wsaa = wsaa ?? throw new ArgumentNullException(nameof(wsaa));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<WsaaTicket> GetTicketAsync(
        string service,
        ArcaTenantContext tenant,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        ArgumentNullException.ThrowIfNull(tenant);
        cancellationToken.ThrowIfCancellationRequested();
        if (tenant.Environment != ArcaEnvironment.Homologation)
            throw new InvalidOperationException("Protected WSAA ticket persistence is restricted to homologation.");

        using X509Certificate2 certificate = tenant.Certificate.LoadCertificate();
        string certificateHash = certificate.GetCertHashString(HashAlgorithmName.SHA256);
        Uri endpoint = WsaaOptions.HomologationEndpoint;
        string ticketKey = CreateTicketKey(tenant, endpoint, service, certificateHash);
        string guardKey = CreateLoginGuardKey(endpoint, service, certificateHash);
        SemaphoreSlim gate = locks[(int)((uint)StringComparer.Ordinal.GetHashCode(guardKey) % (uint)locks.Length)];
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ProtectedHomologationTicketState? saved = await state.FindTicketAsync(ticketKey, cancellationToken).ConfigureAwait(false);
            if (saved is not null)
            {
                WsaaTicket cached;
                try { cached = WsaaService.ParseTicket(saved.TicketXml); }
                catch (Exception exception) when (exception is FormatException or System.Xml.XmlException)
                {
                    throw new InvalidOperationException("The protected homologation ticket is malformed; authentication is stopped.");
                }
                if (!cached.IsExpired(clock.GetUtcNow())) return cached;
            }

            DateTimeOffset now = clock.GetUtcNow();
            ProtectedHomologationLoginGuardState? guard = await state.FindLoginGuardAsync(guardKey, cancellationToken).ConfigureAwait(false);
            if (guard is not null)
            {
                if (now < guard.LastAttemptUtc)
                    throw new InvalidOperationException("The protected homologation ticket timestamp is in the future; authentication is stopped.");
                if (now < guard.CooldownUntilUtc)
                {
                    double seconds = Math.Ceiling((guard.CooldownUntilUtc - now).TotalSeconds);
                    throw new InvalidOperationException($"WSAA authentication cooldown is active for this certificate/service identity for another {seconds.ToString(CultureInfo.InvariantCulture)} seconds.");
                }
            }

            // The persisted margin covers a bounded request window if the completion write fails.
            await state.RecordLoginStartedAsync(guardKey, now, cancellationToken).ConfigureAwait(false);
            WsaaTicket ticket;
            using (var bounded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                bounded.CancelAfter(LoginWindow);
                try
                {
                    ticket = await wsaa.AuthenticateForTenantWithoutCacheAsync(service, tenant, bounded.Token).ConfigureAwait(false);
                }
                catch
                {
                    await state.RecordLoginCompletedAsync(guardKey, clock.GetUtcNow(), ticketIdentityKey: null, ticketXml: null, CancellationToken.None)
                        .ConfigureAwait(false);
                    throw;
                }
            }

            // A returned ticket is unusable by callers until the protected secret has accepted it.
            await state.RecordLoginCompletedAsync(guardKey, clock.GetUtcNow(), ticketKey, ticket.Xml, CancellationToken.None)
                .ConfigureAwait(false);
            return ticket;
        }
        finally { gate.Release(); }
    }

    private static string CreateTicketKey(ArcaTenantContext tenant, Uri endpoint, string service, string certificateHash)
    {
        string material = string.Join('\n',
            tenant.TenantId,
            tenant.Cuit.ToString(CultureInfo.InvariantCulture),
            ((int)tenant.Environment).ToString(CultureInfo.InvariantCulture),
            endpoint.AbsoluteUri,
            service,
            certificateHash);
        return Hash(material);
    }

    private static string CreateLoginGuardKey(Uri endpoint, string service, string certificateHash)
        => Hash(string.Join('\n', endpoint.AbsoluteUri, service, certificateHash));

    private static string Hash(string material)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(material)));
}
