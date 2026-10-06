using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetArcaWs.HealthChecks;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class PublicHealthHomologationTests
{
    [ArcaServicesHomologationTheory]
    [InlineData(ArcaService.Wsaa)]
    [InlineData(ArcaService.Wsfev1)]
    [InlineData(ArcaService.Wsfexv1)]
    [InlineData(ArcaService.Wsmtxca)]
    [InlineData(ArcaService.Wscdc)]
    [InlineData(ArcaService.Wsfecred)]
    [InlineData(ArcaService.Wscpe)]
    [InlineData(ArcaService.PadronA4)]
    [InlineData(ArcaService.PadronA5)]
    [InlineData(ArcaService.PadronA10)]
    [InlineData(ArcaService.PadronA13)]
    [Trait("Category", "Integration")]
    public async Task Public_health_probe_reports_available_infrastructure(ArcaService service)
    {
        using var clients = new ProbeClients();
        var check = new ArcaHealthCheck(clients, new ArcaHealthCheckOptions
        {
            Service = service,
            Environment = ArcaEnvironment.Homologation,
            Timeout = TimeSpan.FromSeconds(20)
        });

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.True(result.Status == HealthStatus.Healthy,
            $"{service}: {result.Description}; failure={result.Data.GetValueOrDefault("failure", "none")}");
        Assert.Equal(false, result.Data["authenticationVerified"]);
    }

    private sealed class ProbeClients : IHttpClientFactory, IDisposable
    {
        private readonly HttpClient client = new();
        public HttpClient CreateClient(string name) => client;
        public void Dispose() => client.Dispose();
    }
}
