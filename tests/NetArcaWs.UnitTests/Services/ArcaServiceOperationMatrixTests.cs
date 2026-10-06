using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using AwesomeAssertions;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using NetArcaWs.Tests.TestSupport;
using Xunit;

namespace NetArcaWs.Tests.Services;

public sealed class ArcaServiceOperationMatrixTests
{
    private const long Cuit = 30123456789;
    private const string TicketToken = "MATRIX-TICKET-TOKEN";
    private const string TicketSign = "MATRIX-TICKET-SIGN";

    [Fact]
    public async Task Generated_service_clients_cover_all_81_operations_with_expected_environment_and_credentials()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var certificate = TestCertificates.CreateWithPrivateKey("operation-matrix");
        var content = NetArcaWs.Cryptography.WsaaCertificateContent.FromPem(
            certificate.ExportCertificatePem(), certificate.GetRSAPrivateKey()!.ExportPkcs8PrivateKeyPem());
        var tenant = new ArcaTenantContext("matrix-tenant", Cuit, ArcaEnvironment.Production, content);
        var tickets = new RecordingArcaTicketProvider(TicketTestData.Create(TicketToken, TicketSign));
        var transport = new RecordingSoapTransport();
        var specifications = new[]
        {
            new ServiceSpec(new Wsfev1Service(transport, tickets), "wsfe", "https://servicios1.afip.gov.ar/wsfev1/service.asmx", 22),
            new ServiceSpec(new Wsfexv1Service(transport, tickets), "wsfex", "https://servicios1.afip.gov.ar/wsfexv1/service.asmx", 19),
            new ServiceSpec(new Wsmtxcav1Service(transport, tickets), "wsmtxca", "https://serviciosjava.afip.gob.ar/wsmtxca/services/MTXCAService", 27),
            new ServiceSpec(new PadronA4Service(transport, tickets), "ws_sr_padron_a4", "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA4", 2),
            new ServiceSpec(new PadronA5Service(transport, tickets), "ws_sr_constancia_inscripcion", "https://aws.arca.gob.ar/sr-padron/webservices/personaServiceA5", 5),
            new ServiceSpec(new PadronA10Service(transport, tickets), "ws_sr_padron_a10", "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA10", 2),
            new ServiceSpec(new PadronA13Service(transport, tickets), "ws_sr_padron_a13", "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA13", 4)
        };

        foreach (ServiceSpec specification in specifications)
        {
            MethodInfo[] operations = specification.Service.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.Name.EndsWith("Async", StringComparison.Ordinal) && typeof(Task).IsAssignableFrom(method.ReturnType))
                .OrderBy(method => method.Name, StringComparer.Ordinal)
                .ToArray();
            operations.Should().HaveCount(specification.OperationCount, specification.Service.GetType().Name);

            foreach (MethodInfo operation in operations)
            {
                ParameterInfo[] parameters = operation.GetParameters();
                object?[] arguments = new object?[parameters.Length];
                object? callerRequest = null;
                bool isUnauthenticated = parameters[0].ParameterType == typeof(ArcaEnvironment);
                for (int index = 0; index < parameters.Length; index++)
                {
                    Type parameterType = parameters[index].ParameterType;
                    if (parameterType == typeof(ArcaTenantContext)) arguments[index] = tenant;
                    else if (parameterType == typeof(ArcaEnvironment)) arguments[index] = tenant.Environment;
                    else if (parameterType == typeof(CancellationToken)) arguments[index] = cancellationToken;
                    else
                    {
                        callerRequest = Activator.CreateInstance(parameterType)
                            ?? throw new InvalidOperationException($"Could not instantiate {parameterType.FullName}.");
                        if (!isUnauthenticated) SetCallerCredentials(callerRequest);
                        arguments[index] = callerRequest;
                    }
                }

                Task task = (Task)(operation.Invoke(specification.Service, arguments)
                    ?? throw new InvalidOperationException($"{operation.Name} did not return a task."));
                await task;

                SoapCall call = transport.Calls.Last();
                call.Endpoint.Should().Be(new Uri(specification.ProductionEndpoint));
                if (specification.Service is PadronA4Service or PadronA5Service or PadronA10Service or PadronA13Service)
                    call.Action.Should().BeEmpty("the Padrón WSDLs declare an empty SOAPAction");
                else
                    call.Action.Should().EndWith("/" + operation.Name[..^"Async".Length]);
                if (!isUnauthenticated)
                {
                    var credentials = ReadCredentials(call.Request!);
                    credentials.Token.Should().Be(TicketToken);
                    credentials.Sign.Should().Be(TicketSign);
                    credentials.Cuit.Should().Be(Cuit);
                    ReadCredentials(callerRequest!).Token.Should().Be("CALLER-TOKEN", "the service must clone the caller's mutable request");
                    tickets.Calls.Last().Service.Should().Be(specification.TicketService);
                }
            }
        }

        transport.CallCount.Should().Be(81);
        tickets.CallCount.Should().Be(74, "each service's dummy operation is unauthenticated");
    }

    private static void SetCallerCredentials(object request)
    {
        PropertyInfo? authProperty = FindProperty(request.GetType(), "Auth") ?? FindProperty(request.GetType(), "AuthRequest");
        object auth = authProperty?.GetValue(request) ?? request;
        if (authProperty is not null && authProperty.GetValue(request) is null)
        {
            auth = Activator.CreateInstance(authProperty.PropertyType)!;
            authProperty.SetValue(request, auth);
        }
        SetProperty(auth, "Token", "CALLER-TOKEN");
        SetProperty(auth, "Sign", "CALLER-SIGN");
        SetProperty(auth, "Cuit", 99999999999L);
        SetProperty(auth, "CuitRepresentada", 99999999999L);
    }

    private static (string? Token, string? Sign, long Cuit) ReadCredentials(object request)
    {
        PropertyInfo? authProperty = FindProperty(request.GetType(), "Auth") ?? FindProperty(request.GetType(), "AuthRequest");
        object auth = authProperty?.GetValue(request) ?? request;
        string? token = FindProperty(auth.GetType(), "Token")?.GetValue(auth) as string;
        string? sign = FindProperty(auth.GetType(), "Sign")?.GetValue(auth) as string;
        object? cuitValue = FindProperty(auth.GetType(), "Cuit")?.GetValue(auth)
            ?? FindProperty(auth.GetType(), "CuitRepresentada")?.GetValue(auth);
        return (token, sign, Convert.ToInt64(cuitValue, System.Globalization.CultureInfo.InvariantCulture));
    }

    private static PropertyInfo? FindProperty(Type type, string name) => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase));

    private static void SetProperty(object target, string name, object value)
    {
        PropertyInfo? property = FindProperty(target.GetType(), name);
        if (property is not null && property.CanWrite)
            property.SetValue(target, Convert.ChangeType(value, property.PropertyType, System.Globalization.CultureInfo.InvariantCulture));
    }

    private sealed record ServiceSpec(object Service, string TicketService, string ProductionEndpoint, int OperationCount);
}
