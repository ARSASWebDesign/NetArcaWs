namespace NetArcaWs.Transport;

public interface ISoapTransport
{
    Task<TResponse> SendAsync<TRequest, TResponse>(Uri endpoint, string action, TRequest request,
        CancellationToken cancellationToken = default);
}
