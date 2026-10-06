using AwesomeAssertions;
using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NetArcaWs.HealthChecks;
using NetArcaWs.Tests.HealthChecks;
using Xunit;

namespace NetArcaWs.Tests.HealthChecks;

public sealed class HealthRegistrationExtensionTests
{
    [Fact]
    public void AddNetArcaWsService_registers_only_the_requested_service_with_the_given_name_tags_and_options()
    {
        var services = new ServiceCollection();
        IHealthChecksBuilder builder = services.AddHealthChecks();
        Uri endpoint = new("https://test.example/wsfe");

        builder.AddNetArcaWsService(ArcaService.Wsfev1, ArcaEnvironment.Production,
            TimeSpan.FromSeconds(7), endpoint, "production-wsfe");

        using ServiceProvider provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        HealthCheckRegistration registration = registrations.Should().ContainSingle().Which;
        registration.Name.Should().Be("production-wsfe");
        registration.Tags.Should().Contain(["arca", "Wsfev1", "Production"]);

        ArcaHealthCheck check = (ArcaHealthCheck)registration.Factory(provider);
        // The instance uses the configured profile options; the custom endpoint is exercised by the handler-based tests.
        check.Should().NotBeNull();
    }

    [Theory]
    [InlineData(nameof(ArcaService.Wsaa))]
    [InlineData(nameof(ArcaService.Wsfev1))]
    [InlineData(nameof(ArcaService.Wsfexv1))]
    [InlineData(nameof(ArcaService.Wsmtxca))]
    [InlineData(nameof(ArcaService.Wscdc))]
    [InlineData(nameof(ArcaService.Wsfecred))]
    [InlineData(nameof(ArcaService.Wscpe))]
    [InlineData(nameof(ArcaService.PadronA4))]
    [InlineData(nameof(ArcaService.PadronA5))]
    [InlineData(nameof(ArcaService.PadronA10))]
    [InlineData(nameof(ArcaService.PadronA13))]
    public void Convenience_extension_registers_only_the_explicitly_selected_service(string serviceName)
    {
        var services = new ServiceCollection();
        IHealthChecksBuilder builder = services.AddHealthChecks();

        AddConvenience(builder, serviceName);

        using ServiceProvider provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        registrations.Should().ContainSingle();
        registrations.Single().Tags.Should().Contain(serviceName);
    }

    [Fact]
    public void AddNetArcaWsService_rejects_invalid_service_timeout_and_endpoint_at_registration_time()
    {
        Action invalidService = () => new ServiceCollection().AddHealthChecks()
            .AddNetArcaWsService((ArcaService)999);
        Action invalidTimeout = () => new ServiceCollection().AddHealthChecks()
            .AddNetArcaWsService(ArcaService.Wsfev1, timeout: TimeSpan.Zero);
        Action invalidEndpoint = () => new ServiceCollection().AddHealthChecks()
            .AddNetArcaWsService(ArcaService.Wsfev1, endpoint: new Uri("http://test.example/wsfe"));

        invalidService.Should().Throw<ArgumentOutOfRangeException>();
        invalidTimeout.Should().Throw<ArgumentOutOfRangeException>();
        invalidEndpoint.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task HealthCheckService_executes_only_the_opted_in_service_without_calling_WSAA_login()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Uri endpoint = new("https://custom.test/wsfe");
        var handler = new CapturingHandler();
        var services = new ServiceCollection();
        services.AddHealthChecks().AddNetArcaWsService(
            ArcaService.Wsfev1, endpoint: endpoint, name: "selected-wsfe");
        services.AddSingleton<IHttpClientFactory>(new CapturingClientFactory(handler));

        using ServiceProvider provider = services.BuildServiceProvider();
        HealthReport report = await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(cancellationToken);

        report.Status.Should().Be(HealthStatus.Healthy);
        report.Entries.Should().ContainSingle().Which.Key.Should().Be("selected-wsfe");
        handler.RequestCount.Should().Be(1);
        handler.Method.Should().Be(HttpMethod.Post);
        handler.RequestUri.Should().Be(endpoint);
        handler.RequestBody.Should().Contain("FEDummy");
        handler.RequestBody.Should().NotContain("loginCms");
    }

    private static void AddConvenience(IHealthChecksBuilder builder, string serviceName)
    {
        switch (serviceName)
        {
            case nameof(ArcaService.Wsaa): builder.AddWsaaHealthCheck(name: "selected"); break;
            case nameof(ArcaService.Wsfev1): builder.AddWsfev1HealthCheck(name: "selected"); break;
            case nameof(ArcaService.Wsfexv1): builder.AddWsfexv1HealthCheck(name: "selected"); break;
            case nameof(ArcaService.Wsmtxca): builder.AddWsmtxcaHealthCheck(name: "selected"); break;
            case nameof(ArcaService.Wscdc): builder.AddWscdcHealthCheck(name: "selected"); break;
            case nameof(ArcaService.Wsfecred): builder.AddWsfecredHealthCheck(name: "selected"); break;
            case nameof(ArcaService.Wscpe): builder.AddWscpeHealthCheck(name: "selected"); break;
            case nameof(ArcaService.PadronA4): builder.AddPadronA4HealthCheck(name: "selected"); break;
            case nameof(ArcaService.PadronA5): builder.AddPadronA5HealthCheck(name: "selected"); break;
            case nameof(ArcaService.PadronA10): builder.AddPadronA10HealthCheck(name: "selected"); break;
            case nameof(ArcaService.PadronA13): builder.AddPadronA13HealthCheck(name: "selected"); break;
            default: throw new ArgumentOutOfRangeException(nameof(serviceName), serviceName, "Unknown health-check wrapper.");
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        internal int RequestCount { get; private set; }
        internal HttpMethod? Method { get; private set; }
        internal Uri? RequestUri { get; private set; }
        internal string RequestBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Method = request.Method;
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            const string responseXml = """
                <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" xmlns:fe="http://ar.gov.afip.dif.FEV1/">
                  <soap:Body><fe:FEDummyResponse><fe:FEDummyResult><fe:AppServer>OK</fe:AppServer><fe:DbServer>OK</fe:DbServer><fe:AuthServer>OK</fe:AuthServer></fe:FEDummyResult></fe:FEDummyResponse></soap:Body>
                </soap:Envelope>
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseXml, Encoding.UTF8, "text/xml")
            };
        }
    }

    private sealed class CapturingClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
