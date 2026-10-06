using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Wsaa;
using Xunit;

namespace NetArcaWs.Tests.Wsaa;

public sealed class WsaaRegistrationTests
{
    [Fact]
    public void AddNetArcaWs_registers_a_resolvable_service_and_named_http_client()
    {
        var services = new ServiceCollection();
        services.AddNetArcaWs(options => options.TraTimeToLive = TimeSpan.FromMinutes(25));

        using ServiceProvider provider = services.BuildServiceProvider();
        WsaaService service = provider.GetRequiredService<WsaaService>();
        IHttpClientFactory clientFactory = provider.GetRequiredService<IHttpClientFactory>();

        service.Should().NotBeNull();
        clientFactory.CreateClient(WsaaService.HttpClientName).Should().NotBeNull();
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
    }
}
