using System.Collections.Concurrent;
using System.Text;
using System.Xml.Serialization;
using NetArcaWs.Multitenancy;
using NetArcaWs.Transport;
using NetArcaWs.Wsaa;

namespace NetArcaWs.Tests.TestSupport;

internal sealed record SoapCall(
    Uri Endpoint,
    string Action,
    object? Request,
    Type RequestType,
    Type ResponseType,
    string SerializedRequest);

internal sealed class RecordingSoapTransport(Func<SoapCall, object?>? responseFactory = null) : ISoapTransport
{
    private readonly ConcurrentQueue<SoapCall> calls = new();

    internal IReadOnlyCollection<SoapCall> Calls => calls.ToArray();
    internal int CallCount => calls.Count;
    internal SoapCall LastCall => calls.Last();

    public Task<TResponse> SendAsync<TRequest, TResponse>(Uri endpoint, string action, TRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string requestXml = Serialize(request);
        var call = new SoapCall(endpoint, action, request, typeof(TRequest), typeof(TResponse), requestXml);
        calls.Enqueue(call);
        object? response = responseFactory?.Invoke(call) ?? Activator.CreateInstance(typeof(TResponse));
        if (response is not TResponse typed)
            throw new InvalidOperationException($"Fake SOAP response factory did not return {typeof(TResponse).FullName}.");
        return Task.FromResult(typed);
    }

    private static string Serialize<TRequest>(TRequest request)
    {
        if (request is null) return string.Empty;
        var serializer = new XmlSerializer(typeof(TRequest));
        using var writer = new StringWriter(new StringBuilder(), System.Globalization.CultureInfo.InvariantCulture);
        serializer.Serialize(writer, request);
        return writer.ToString();
    }
}

internal sealed class RecordingArcaTicketProvider(WsaaTicket? ticket = null) : IArcaTicketProvider
{
    private readonly ConcurrentQueue<(string Service, ArcaTenantContext Tenant)> calls = new();

    internal int CallCount => calls.Count;
    internal IReadOnlyCollection<(string Service, ArcaTenantContext Tenant)> Calls => calls.ToArray();

    public Task<WsaaTicket> GetTicketAsync(string service, ArcaTenantContext tenant, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        calls.Enqueue((service, tenant));
        return Task.FromResult(ticket ?? throw new InvalidOperationException("No fake WSAA ticket configured."));
    }
}

internal static class TicketTestData
{
    internal static WsaaTicket Create(
        string token = "WSAA-TOKEN",
        string sign = "WSAA-SIGN",
        DateTimeOffset? expiration = null) => new(
            token,
            sign,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            expiration ?? DateTimeOffset.UtcNow.AddHours(1),
            "CN=source",
            "CN=destination",
            98765,
            "<loginTicketResponse />");
}
