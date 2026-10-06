using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NetArcaWs.Wsaa;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers WSAA, its shared ticket cache and a factory-managed HTTP transport.</summary>
    public static IHttpClientBuilder AddNetArcaWs(this IServiceCollection services, Action<WsaaOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddMemoryCache();
        services.AddOptions<WsaaOptions>();
        if (configure is not null) services.Configure(configure);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<WsaaService>();
        return services.AddHttpClient(WsaaService.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(30));
    }
}
