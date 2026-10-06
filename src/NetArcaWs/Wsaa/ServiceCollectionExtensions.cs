using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Transport;
using NetArcaWs.Services;
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
        services.TryAddSingleton<IArcaTicketProvider, WsaaTicketProvider>();
        services.AddOptions<SoapTransportOptions>();
        services.TryAddSingleton<ISoapTransport, SoapTransport>();
        services.TryAddSingleton<Wsfev1Service>();
        services.TryAddSingleton<IWsfev1Service>(provider => provider.GetRequiredService<Wsfev1Service>());
        services.TryAddSingleton<Wsfexv1Service>();
        services.TryAddSingleton<IWsfexv1Service>(provider => provider.GetRequiredService<Wsfexv1Service>());
        services.TryAddSingleton<Wsmtxcav1Service>();
        services.TryAddSingleton<IWsmtxcav1Service>(provider => provider.GetRequiredService<Wsmtxcav1Service>());
        services.TryAddSingleton<PadronA4Service>();
        services.TryAddSingleton<IPadronA4Service>(provider => provider.GetRequiredService<PadronA4Service>());
        services.TryAddSingleton<PadronA5Service>();
        services.TryAddSingleton<IPadronA5Service>(provider => provider.GetRequiredService<PadronA5Service>());
        services.TryAddSingleton<PadronA10Service>();
        services.TryAddSingleton<IPadronA10Service>(provider => provider.GetRequiredService<PadronA10Service>());
        services.TryAddSingleton<PadronA13Service>();
        services.TryAddSingleton<IPadronA13Service>(provider => provider.GetRequiredService<PadronA13Service>());
        services.TryAddSingleton<PadronService>();

        services.AddHttpClient(SoapTransport.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(30));
        return services.AddHttpClient(WsaaService.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(30));
    }
}
