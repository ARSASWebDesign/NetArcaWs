using System.Globalization;
using System.Reflection;
using System.Xml.Serialization;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Transport;
using NetArcaWs.Wsaa;

namespace NetArcaWs.Services;

/// <summary>Shared mechanics for typed ARCA SOAP clients.</summary>
public abstract class ArcaSoapServiceBase
{
    private readonly ISoapTransport transport;
    private readonly IArcaTicketProvider tickets;

    protected ArcaSoapServiceBase(ISoapTransport transport, IArcaTicketProvider tickets)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(tickets);
        this.transport = transport;
        this.tickets = tickets;
    }

    protected async Task<TResponse> SendAuthenticatedAsync<TRequest, TResponse>(
        string service, Uri endpoint, string action, ArcaTenantContext tenant, TRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        // Freeze the caller's mutable DTO before yielding to ticket renewal. Otherwise callers
        // could mutate the request while GetTicketAsync is in flight and change the signed wire
        // payload after invocation began.
        var wireRequest = CloneRequest(request);
        var ticket = await tickets.GetTicketAsync(service, tenant, cancellationToken).ConfigureAwait(false);
        InjectCredentials(wireRequest!, ticket, tenant.Cuit);
        return await transport.SendAsync<TRequest, TResponse>(endpoint, action, wireRequest, cancellationToken)
            .ConfigureAwait(false);
    }

    protected Task<TResponse> SendUnauthenticatedAsync<TRequest, TResponse>(
        Uri endpoint, string action, TRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return transport.SendAsync<TRequest, TResponse>(endpoint, action, request, cancellationToken);
    }

    protected static Uri Endpoint(ArcaEnvironment environment, Uri homologation, Uri production)
    {
        if (!Enum.IsDefined(environment)) throw new ArgumentOutOfRangeException(nameof(environment));
        return environment == ArcaEnvironment.Production ? production : homologation;
    }

    private static T CloneRequest<T>(T request)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var stream = new BoundedMemoryStream(32 * 1024 * 1024);
        serializer.Serialize(stream, request);
        stream.Position = 0;
        return (T)(serializer.Deserialize(stream)
            ?? throw new InvalidOperationException("Could not clone the typed ARCA request."));
    }

    private static void InjectCredentials(object request, WsaaTicket ticket, long cuit)
    {
        var type = request.GetType();
        var authentication = FindProperty(type, "Auth") ?? FindProperty(type, "AuthRequest");
        if (authentication is not null)
        {
            var value = authentication.GetValue(request) ?? Activator.CreateInstance(authentication.PropertyType)
                ?? throw new InvalidOperationException($"Cannot create {authentication.PropertyType.Name} authentication data.");
            SetCredentials(value, ticket, cuit);
            authentication.SetValue(request, value);
            return;
        }

        SetCredentials(request, ticket, cuit);
    }

    private static void SetCredentials(object target, WsaaTicket ticket, long cuit)
    {
        var tokenSet = Set(target, "Token", ticket.Token);
        var signSet = Set(target, "Sign", ticket.Sign);
        var cuitSet = Set(target, "Cuit", cuit) || Set(target, "CuitRepresentada", cuit);
        if (!tokenSet || !signSet || !cuitSet)
            throw new InvalidOperationException($"The generated {target.GetType().Name} schema has no complete WSAA credential fields.");
    }

    private static bool Set(object target, string name, long value)
    {
        var property = FindProperty(target.GetType(), name);
        if (property is null || !property.CanWrite) return false;
        property.SetValue(target, Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture));
        return true;
    }

    private static bool Set(object target, string name, string value)
    {
        var property = FindProperty(target.GetType(), name);
        if (property is null || !property.CanWrite) return false;
        property.SetValue(target, value);
        return true;
    }

    private static PropertyInfo? FindProperty(Type type, string name)
        => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase));
}
