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
using NetArcaWs.Contracts.Wscdc;
using NetArcaWs.Contracts.WsfeCred;
using NetArcaWs.Invoicing;
using System.Globalization;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class ArcaServicesHomologationFactAttribute : FactAttribute
{
    public ArcaServicesHomologationFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ARCA_RUN_HOMOLOGY"), "1", StringComparison.Ordinal))
            Skip = "Set ARCA_RUN_HOMOLOGY=1 to run read-only dummy probes against all ARCA homologation services.";
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
        bool strict = string.Equals(Environment.GetEnvironmentVariable("ARCA_REQUIRE_HOMOLOGY"), "1", StringComparison.Ordinal);
        if (!strict && Environment.GetEnvironmentVariable("ARCA_HOMOLOGY_SERVICES") is null)
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
    [InlineData("wscdc")]
    [InlineData("wsfecred")]
    [InlineData("wscpe")]
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
            "wscdc" => await new WscdcService(transport, tickets).ComprobanteDummyAsync(ArcaEnvironment.Homologation, token),
            "wsfecred" => await new WsfecredService(transport, tickets).dummyAsync(ArcaEnvironment.Homologation, token),
            "wscpe" => await new WscpeService(transport, tickets).dummyAsync(ArcaEnvironment.Homologation, token),
            "padron-a4" => await new PadronA4Service(transport, tickets).dummyAsync(ArcaEnvironment.Homologation, token),
            "padron-a5" => await new PadronA5Service(transport, tickets).dummyAsync(ArcaEnvironment.Homologation, token),
            "padron-a10" => await new PadronA10Service(transport, tickets).dummyAsync(ArcaEnvironment.Homologation, token),
            "padron-a13" => await new PadronA13Service(transport, tickets).dummyAsync(ArcaEnvironment.Homologation, token),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceName))
        };
        Assert.True(reply is not null, "The homologation dummy probe should return a response.");
        tickets.Calls.Should().Be(0, "dummy operations are unauthenticated and never request a WSAA ticket");
    }

    [AuthenticatedHomologationFact]
    [Trait("Category", "Integration")]
    public async Task Explicitly_enabled_authenticated_scenarios_run_against_homologation()
    {
        string[] selected = (Environment.GetEnvironmentVariable("ARCA_HOMOLOGY_SERVICES") ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant()).Distinct().ToArray();
        if (selected.Length == 0)
            throw new InvalidOperationException("Set ARCA_HOMOLOGY_SERVICES to at least one supported authenticated read-only lookup.");
        string mode = Environment.GetEnvironmentVariable("ARCA_HOMOLOGY_MODE") ?? "consultas";
        if (mode is not ("consultas" or "emision"))
            throw new InvalidOperationException("ARCA_HOMOLOGY_MODE must be consultas or emision.");
        string[] supported = ["wsfe", "wsfex", "wsmtxca", "wscdc", "wsfecred", "padron-a4", "padron-a5", "padron-a10", "padron-a13"];
        if (selected.Any(service => !supported.Contains(service, StringComparer.Ordinal)))
            throw new InvalidOperationException("An unsupported homologation service was selected.");
        int pointOfSale = 0, voucherType = 0;
        long voucherNumber = 0;
        if (mode == "emision" || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ARCA_HOMOLOGY_POINT_OF_SALE")))
        {
            if (selected.Length != 1 || selected[0] != "wsfe")
                throw new InvalidOperationException("Numbering and issuance are supported exclusively for WSFE homologation.");
            pointOfSale = (int)ReadPositiveSetting("ARCA_HOMOLOGY_POINT_OF_SALE", 99999);
            voucherType = (int)ReadPositiveSetting("ARCA_HOMOLOGY_VOUCHER_TYPE", 11);
            if (voucherType is not (6 or 11))
                throw new InvalidOperationException("Only ordinary B and C invoices are supported by this scenario.");
            if (mode == "emision")
                voucherNumber = ReadPositiveSetting("ARCA_HOMOLOGY_VOUCHER_NUMBER", 99999999);
        }
        string certificatePath = Environment.GetEnvironmentVariable("WSAA_CERT_PATH")!;
        string keyPath = Environment.GetEnvironmentVariable("WSAA_KEY_PATH")!;
        if (string.IsNullOrWhiteSpace(certificatePath) || string.IsNullOrWhiteSpace(keyPath))
            throw new InvalidOperationException("Set WSAA_CERT_PATH and WSAA_KEY_PATH when ARCA_HOMOLOGY_SERVICES enables authenticated lookups.");
        long cuit = long.Parse(Environment.GetEnvironmentVariable("ARCA_CUIT")!, System.Globalization.CultureInfo.InvariantCulture);
        WsaaCertificateContent content = WsaaCertificateContent.FromPem(await File.ReadAllTextAsync(certificatePath), await File.ReadAllTextAsync(keyPath), Environment.GetEnvironmentVariable("WSAA_KEY_PASSWORD"));
        var tenant = new ArcaTenantContext("homologation-contract-test", cuit, ArcaEnvironment.Homologation, content);
        var services = new ServiceCollection();
        services.AddNetArcaWs(options => options.Endpoint = WsaaOptions.HomologationEndpoint);
        using var workspace = new FiscalWorkspace();
        if (mode == "emision")
            services.AddNetArcaWsSqliteInvoicing(Path.Combine(workspace.Path, "journal.db"));
        using ServiceProvider provider = services.BuildServiceProvider();
        CancellationToken token = TestContext.Current.CancellationToken;
        List<string> padronFailures = [];
        // Complete the person matrix before unrelated catalog probes can abort a mixed run.
        foreach (string service in selected.OrderBy(service => !service.StartsWith("padron-", StringComparison.Ordinal)))
        {
            if (service == "wsfe")
            {
                var client = provider.GetRequiredService<IWsfev1Service>();
                var points = await AuthenticatedLookupValidation.ValidateWsfeAsync(client, tenant, token);
                var pointLines = points.Take(20).Select(point =>
                    $"- Punto {point.Nro}: CAE={(point.EmisionTipo == "CAE" ? "sí" : "no")}; bloqueado={(point.Bloqueado == "N" ? "no" : "sí")}");
                await ReportAsync($"### WSFE: consultas aprobadas\nPuntos devueltos: {points.Count} (hasta 20 listados).\n" + string.Join('\n', pointLines), token);
                if (mode == "consultas" && pointOfSale > 0)
                {
                    var last = (await client.FECompUltimoAutorizadoAsync(tenant,
                        new FeCompUltimoAutorizado { PtoVta = pointOfSale, CbteTipo = voucherType }, token)).FeCompUltimoAutorizadoResult;
                    Assert.True(last is not null && last.Errors.Count == 0 && last.PtoVta == pointOfSale && last.CbteTipo == voucherType && last.CbteNro >= 0,
                        "WSFE did not confirm the selected numbering series.");
                    await ReportAsync($"Punto {pointOfSale}, tipo {voucherType}: último autorizado {last!.CbteNro}; siguiente candidato {(long)last.CbteNro + 1}. La numeración se verificará nuevamente antes de emitir.", token);
                }
                continue;
            }
            if (service.StartsWith("padron-", StringComparison.Ordinal))
            {
                await ReportAsync(service == "padron-a4"
                    ? "### Padrón A4: casos del listado oficial de testing"
                    : $"### {service}: casos del listado A4 contrastados en QA el 2026-10-06", token);
                padronFailures.AddRange(await PadronTestCases.RunAsync(service, async (fixture, ct) => service switch
                {
                    "padron-a4" => await provider.GetRequiredService<PadronA4Service>().getPersonaAsync(tenant, new NetArcaWs.Contracts.PadronA4.GetPersona { IdPersona = fixture.Id }, ct),
                    "padron-a5" => await provider.GetRequiredService<PadronA5Service>().getPersonaAsync(tenant, new NetArcaWs.Contracts.PadronA5.GetPersona { IdPersona = fixture.Id }, ct),
                    "padron-a10" => await provider.GetRequiredService<PadronA10Service>().getPersonaAsync(tenant, new NetArcaWs.Contracts.PadronA10.GetPersona { IdPersona = fixture.Id }, ct),
                    "padron-a13" => await provider.GetRequiredService<PadronA13Service>().getPersonaAsync(tenant, new NetArcaWs.Contracts.PadronA13.GetPersona { IdPersona = fixture.Id }, ct),
                    _ => throw new ArgumentOutOfRangeException(nameof(service))
                }, ReportAsync, token));
                continue;
            }
            object response = service switch
            {
                "wsfex" => await provider.GetRequiredService<Wsfexv1Service>().FEXGetPARAM_MONAsync(tenant, new FexGetParamMon(), token),
                "wsmtxca" => await provider.GetRequiredService<Wsmtxcav1Service>().consultarMonedasAsync(tenant, new ConsultarMonedasRequestType(), token),
                "wscdc" => await provider.GetRequiredService<WscdcService>().ComprobantesModalidadConsultarAsync(tenant, new ComprobantesModalidadConsultar(), token),
                "wsfecred" => await provider.GetRequiredService<WsfecredService>().consultarTiposRetencionesAsync(tenant, new ConsultarTiposRetencionesRequest(), token),
                _ => throw new InvalidOperationException($"Unknown ARCA_HOMOLOGY_SERVICES entry '{service}'.")
            };
            AuthenticatedLookupValidation.ValidateResponse(response);
            await ReportAsync($"- Consulta autenticada validada: {service}.", token);
        }
        Assert.True(padronFailures.Count == 0, "Padron cases failed: " + string.Join("; ", padronFailures));
        if (mode == "emision")
        {
            var result = await WsfeFiscalHomologationScenario.RunAsync(provider, tenant, pointOfSale, voucherType, voucherNumber, token);
            await ReportAsync($"### Comprobante de homologación verificado\nPunto {pointOfSale}, tipo {voucherType}, número {voucherNumber}. " +
                (result.AlreadyExisted ? "Ya existía y coincidió con el escenario; no se envió otra autorización." : "Se autorizó y se confirmó mediante consulta posterior."), token);
        }
    }

    private static async Task ReportAsync(string text, CancellationToken token)
    {
        if (Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") is { Length: > 0 } summary)
            await File.AppendAllTextAsync(summary, text + "\n\n", token);
        Console.WriteLine(text);
    }

    private static long ReadPositiveSetting(string name, long maximum)
    {
        if (!long.TryParse(Environment.GetEnvironmentVariable(name), NumberStyles.None, CultureInfo.InvariantCulture, out long value)
            || value < 1 || value > maximum)
            throw new InvalidOperationException($"Missing or invalid homologation configuration: {name}.");
        return value;
    }

    private sealed class FiscalWorkspace : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("netarcaws-fiscal-").FullName;

        public FiscalWorkspace()
        {
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(Path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
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
