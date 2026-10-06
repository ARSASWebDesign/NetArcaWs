using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Cryptography;
using NetArcaWs.Wsaa;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class RequiresHomologationCredentialsFactAttribute : FactAttribute
{
    public RequiresHomologationCredentialsFactAttribute()
    {
        string? certificatePath = Environment.GetEnvironmentVariable("WSAA_CERT_PATH");
        string? keyPath = Environment.GetEnvironmentVariable("WSAA_KEY_PATH");
        string? service = Environment.GetEnvironmentVariable("WSAA_SERVICE");
        if (string.IsNullOrWhiteSpace(certificatePath) || string.IsNullOrWhiteSpace(keyPath) ||
            string.IsNullOrWhiteSpace(service))
        {
            Skip = "Set WSAA_CERT_PATH, WSAA_KEY_PATH, and WSAA_SERVICE to run against ARCA homologation.";
        }
    }
}

public sealed class WsaaHomologationTests
{
    [RequiresHomologationCredentialsFact]
    [Trait("Category", "Integration")]
    public async Task AuthenticateAsync_obtains_a_live_ticket_from_ARCA_homologation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string certificatePath = Environment.GetEnvironmentVariable("WSAA_CERT_PATH")!;
        string keyPath = Environment.GetEnvironmentVariable("WSAA_KEY_PATH")!;
        string serviceName = Environment.GetEnvironmentVariable("WSAA_SERVICE")!;
        string? keyPassword = Environment.GetEnvironmentVariable("WSAA_KEY_PASSWORD");

        using var certificate = WsaaCryptography.LoadPem(
            await File.ReadAllTextAsync(certificatePath, cancellationToken),
            await File.ReadAllTextAsync(keyPath, cancellationToken),
            keyPassword);
        var services = new ServiceCollection();
        services.AddNetArcaWs(options => options.Endpoint = WsaaOptions.HomologationEndpoint);
        using ServiceProvider provider = services.BuildServiceProvider();

        WsaaTicket ticket = await provider.GetRequiredService<WsaaService>()
            .AuthenticateAsync(serviceName, certificate, cancellationToken);

        ticket.Token.Should().NotBeNullOrWhiteSpace();
        ticket.Sign.Should().NotBeNullOrWhiteSpace();
        ticket.IsExpired(TimeProvider.System.GetUtcNow()).Should().BeFalse();
        ticket.GetTag("token").Should().Be(ticket.Token);
        ticket.GetTag("sign").Should().Be(ticket.Sign);
    }
}
