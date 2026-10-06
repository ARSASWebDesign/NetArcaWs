using System.Net;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Tests.TestSupport;
using NetArcaWs.Transport;
using NetArcaWs.Wsaa;
using Xunit;

namespace NetArcaWs.Tests.Transport;

public sealed class WsaaTicketProviderTests
{
    [Fact]
    public async Task AddNetArcaWs_registers_transport_and_ticket_provider_for_tenant_ticket_resolution()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(
                    now.AddMinutes(-1), now.AddHours(1), token: "tenant-token", sign: "tenant-sign")))));
        WsaaCertificateContent content = CreateContent("ticket-provider");
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new AdjustableTimeProvider(now));
        services.AddNetArcaWs(options => options.Certificate = content)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using ServiceProvider provider = services.BuildServiceProvider();
        IArcaTicketProvider ticketProvider = provider.GetRequiredService<IArcaTicketProvider>();
        ISoapTransport soapTransport = provider.GetRequiredService<ISoapTransport>();
        var tenant = new ArcaTenantContext("tenant-ticket", 30123456789, ArcaEnvironment.Homologation, content);

        WsaaTicket ticket = await ticketProvider.GetTicketAsync("wsfe", tenant, cancellationToken);

        ticketProvider.Should().BeOfType<WsaaTicketProvider>();
        soapTransport.Should().BeOfType<SoapTransport>();
        ticket.Token.Should().Be("tenant-token");
        ticket.Sign.Should().Be("tenant-sign");
        handler.RequestCount.Should().Be(1);
    }

    private static WsaaCertificateContent CreateContent(string commonName)
    {
        using X509Certificate2 certificate = TestCertificates.CreateWithPrivateKey(commonName);
        using var privateKey = certificate.GetRSAPrivateKey()!;
        return WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), privateKey.ExportPkcs8PrivateKeyPem());
    }
}
