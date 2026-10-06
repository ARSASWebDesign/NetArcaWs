using NetArcaWs.Multitenancy;
using NetArcaWs.Wsaa;

namespace NetArcaWs.Transport;

public interface IArcaTicketProvider
{
    Task<WsaaTicket> GetTicketAsync(string service, ArcaTenantContext tenant, CancellationToken cancellationToken = default);
}

public sealed class WsaaTicketProvider(WsaaService wsaa) : IArcaTicketProvider
{
    public Task<WsaaTicket> GetTicketAsync(string service, ArcaTenantContext tenant, CancellationToken cancellationToken = default)
        => wsaa.AuthenticateForTenantAsync(service, tenant, cancellationToken);
}
