using AwesomeAssertions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Services;
using NetArcaWs.Transport;
using NetArcaWs.Multitenancy;
using NetArcaWs.Wsaa;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Contracts.Wsmtxca;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class ArcaServicesHomologationFactAttribute : FactAttribute
{
    public ArcaServicesHomologationFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ARCA_RUN_HOMOLOGY"), "1", StringComparison.Ordinal))
            Skip = "Set ARCA_RUN_HOMOLOGY=1 to run read-only dummy probes against all seven ARCA homologation services.";
    }
}

public sealed class ArcaServicesHomologationTheoryAttribute : TheoryAttribute
{
    public ArcaServicesHomologationTheoryAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ARCA_RUN_HOMOLOGY"), "1", StringComparison.Ordinal))
            Skip = "Set ARCA_RUN_HOMOLOGY=1 to run read-only dummy probes against ARCA homologation.";
    }
}

public sealed class AuthenticatedHomologationFactAttribute : FactAttribute
{
    public AuthenticatedHomologationFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ARCA_HOMOLOGY_SERVICES")))
            Skip = "Set ARCA_HOMOLOGY_SERVICES to an explicit comma-separated list of read-only service probes to enable this test.";
    }
}

public sealed class ArcaServicesHomologationTests
{
    [ArcaServicesHomologationTheory]
    [Trait("Category", "Integration")]
    [InlineData("wsfe")]
    [InlineData("wsfex")]
    [InlineData("wsmtxca")]
    [InlineData("padron-a4")]
    [InlineData("padron-a5")]
    [InlineData("padron-a10")]
    [InlineData("padron-a13")]
    public async Task Each_service_answers_its_read_only_dummy_operation(string serviceName)
    {
        using var clients = new HttpClientFactory();
        var transport = new SoapTransport(clients, Options.Create(new SoapTransportOptions()));
        var tickets = new UnusedTicketProvider();
        CancellationToken token = TestContext.Current.CancellationToken;
        object reply = serviceName switch
        {
            "wsfe" => await new Wsfev1Service(transport, tickets).FEDummyAsync(ArcaEnvironment.Homologation, token),
            "wsfex" => await new Wsfexv1Service(transport, tickets).FEXDummyAsync(ArcaEnvironment.Homologation, token),
            "wsmtxca" => await new Wsmtxcav1Service(transport, tickets).dummyAsync(ArcaEnvironment.Homologation, token),
            "padron-a4" => await new PadronA4Service(transport, tickets).dummyAsync(ArcaEnvironment.Homologation, token),
            "padron-a5" => await new PadronA5Service(transport, tickets).dummyAsync(ArcaEnvironment.Homologation, token),
            "padron-a10" => await new PadronA10Service(transport, tickets).dummyAsync(ArcaEnvironment.Homologation, token),
            "padron-a13" => await new PadronA13Service(transport, tickets).dummyAsync(ArcaEnvironment.Homologation, token),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceName))
        };
        reply.Should().NotBeNull(serviceName);
        tickets.Calls.Should().Be(0, "dummy operations are unauthenticated and never request a WSAA ticket");
    }

    [AuthenticatedHomologationFact]
    [Trait("Category", "Integration")]
    public async Task Explicitly_enabled_authenticated_read_only_lookups_run_against_homologation()
    {
        string[] selected = (Environment.GetEnvironmentVariable("ARCA_HOMOLOGY_SERVICES") ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant()).Distinct().ToArray();
        if (selected.Length == 0) Assert.Skip("No authenticated homologation services were selected.");
        string certificatePath = Environment.GetEnvironmentVariable("WSAA_CERT_PATH")!;
        string keyPath = Environment.GetEnvironmentVariable("WSAA_KEY_PATH")!;
        if (string.IsNullOrWhiteSpace(certificatePath) || string.IsNullOrWhiteSpace(keyPath))
            throw new InvalidOperationException("Set WSAA_CERT_PATH and WSAA_KEY_PATH when ARCA_HOMOLOGY_SERVICES enables authenticated lookups.");
        long cuit = long.Parse(Environment.GetEnvironmentVariable("ARCA_CUIT")!, System.Globalization.CultureInfo.InvariantCulture);
        bool queriesPadron = selected.Any(x => x.StartsWith("padron-", StringComparison.Ordinal));
        string? configuredQueryCuit = Environment.GetEnvironmentVariable("ARCA_QUERY_CUIT");
        if (queriesPadron && string.IsNullOrWhiteSpace(configuredQueryCuit))
            throw new InvalidOperationException("Set ARCA_QUERY_CUIT to the person CUIT for a Padron lookup.");
        long queryCuit = string.IsNullOrWhiteSpace(configuredQueryCuit) ? cuit : long.Parse(configuredQueryCuit, System.Globalization.CultureInfo.InvariantCulture);
        WsaaCertificateContent content = WsaaCertificateContent.FromPem(await File.ReadAllTextAsync(certificatePath), await File.ReadAllTextAsync(keyPath), Environment.GetEnvironmentVariable("WSAA_KEY_PASSWORD"));
        var tenant = new ArcaTenantContext("homologation-contract-test", cuit, ArcaEnvironment.Homologation, content);
        var services = new ServiceCollection();
        services.AddNetArcaWs(options => options.Endpoint = WsaaOptions.HomologationEndpoint);
        using ServiceProvider provider = services.BuildServiceProvider();
        CancellationToken token = TestContext.Current.CancellationToken;
        foreach (string service in selected)
        {
            object response = service switch
            {
                "wsfe" => await provider.GetRequiredService<Wsfev1Service>().FEParamGetTiposMonedasAsync(tenant, new FeParamGetTiposMonedas(), token),
                "wsfex" => await provider.GetRequiredService<Wsfexv1Service>().FEXGetPARAM_MONAsync(tenant, new FexGetParamMon(), token),
                "wsmtxca" => await provider.GetRequiredService<Wsmtxcav1Service>().consultarMonedasAsync(tenant, new ConsultarMonedasRequestType(), token),
                "padron-a4" => await provider.GetRequiredService<PadronA4Service>().getPersonaAsync(tenant, new NetArcaWs.Contracts.PadronA4.GetPersona { IdPersona = queryCuit }, token),
                "padron-a5" => await provider.GetRequiredService<PadronA5Service>().getPersonaAsync(tenant, new NetArcaWs.Contracts.PadronA5.GetPersona { IdPersona = queryCuit }, token),
                "padron-a10" => await provider.GetRequiredService<PadronA10Service>().getPersonaAsync(tenant, new NetArcaWs.Contracts.PadronA10.GetPersona { IdPersona = queryCuit }, token),
                "padron-a13" => await provider.GetRequiredService<PadronA13Service>().getPersonaAsync(tenant, new NetArcaWs.Contracts.PadronA13.GetPersona { IdPersona = queryCuit }, token),
                _ => throw new InvalidOperationException($"Unknown ARCA_HOMOLOGY_SERVICES entry '{service}'.")
            };
            response.Should().NotBeNull(service);
        }
    }

    private sealed class UnusedTicketProvider : IArcaTicketProvider
    {
        public int Calls { get; private set; }
        public Task<WsaaTicket> GetTicketAsync(string service, ArcaTenantContext tenant, CancellationToken cancellationToken = default)
        {
            Calls++;
            throw new InvalidOperationException("Read-only dummy operations must not request a ticket.");
        }
    }

    private sealed class HttpClientFactory : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient client = new();
        public HttpClient CreateClient(string name) => client;
        public void Dispose() => client.Dispose();
    }
}
