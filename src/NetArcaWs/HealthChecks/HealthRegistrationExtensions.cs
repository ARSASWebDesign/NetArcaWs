using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NetArcaWs.HealthChecks;

public static class HealthRegistrationExtensions
{
    public static IHealthChecksBuilder AddWscdcHealthCheck(this IHealthChecksBuilder builder,
        ArcaEnvironment environment = ArcaEnvironment.Homologation, TimeSpan? timeout = null, Uri? endpoint = null, string? name = null)
        => builder.AddNetArcaWsService(ArcaService.Wscdc, environment, timeout, endpoint, name);

    public static IHealthChecksBuilder AddWsfecredHealthCheck(this IHealthChecksBuilder builder,
        ArcaEnvironment environment = ArcaEnvironment.Homologation, TimeSpan? timeout = null, Uri? endpoint = null, string? name = null)
        => builder.AddNetArcaWsService(ArcaService.Wsfecred, environment, timeout, endpoint, name);

    public static IHealthChecksBuilder AddWscpeHealthCheck(this IHealthChecksBuilder builder,
        ArcaEnvironment environment = ArcaEnvironment.Homologation, TimeSpan? timeout = null, Uri? endpoint = null, string? name = null)
        => builder.AddNetArcaWsService(ArcaService.Wscpe, environment, timeout, endpoint, name);

    /// <summary>Registers only the selected WS. No credentials or WSAA authentication service are required.</summary>
    public static IHealthChecksBuilder AddNetArcaWsService(this IHealthChecksBuilder builder, ArcaService service,
        ArcaEnvironment environment = ArcaEnvironment.Homologation, TimeSpan? timeout = null, Uri? endpoint = null, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var options = new ArcaHealthCheckOptions
        {
            Service = service, Environment = environment,
            Timeout = timeout ?? TimeSpan.FromSeconds(5), Endpoint = endpoint
        }.Snapshot();
        var registrationName = name ?? $"arca-{service.ToString().ToLowerInvariant()}-{environment.ToString().ToLowerInvariant()}";
        ArgumentException.ThrowIfNullOrWhiteSpace(registrationName);
        builder.Services.AddHttpClient(ArcaHealthCheck.HttpClientName, client => client.Timeout = Timeout.InfiniteTimeSpan);
        return builder.Add(new HealthCheckRegistration(registrationName,
            provider => new ArcaHealthCheck(provider.GetRequiredService<IHttpClientFactory>(), options),
            HealthStatus.Unhealthy, ["arca", service.ToString(), environment.ToString()]));
    }

    public static IHealthChecksBuilder AddWsaaHealthCheck(this IHealthChecksBuilder builder,
        ArcaEnvironment environment = ArcaEnvironment.Homologation, TimeSpan? timeout = null, Uri? endpoint = null, string? name = null)
        => builder.AddNetArcaWsService(ArcaService.Wsaa, environment, timeout, endpoint, name);

    public static IHealthChecksBuilder AddWsfev1HealthCheck(this IHealthChecksBuilder builder,
        ArcaEnvironment environment = ArcaEnvironment.Homologation, TimeSpan? timeout = null, Uri? endpoint = null, string? name = null)
        => builder.AddNetArcaWsService(ArcaService.Wsfev1, environment, timeout, endpoint, name);

    public static IHealthChecksBuilder AddWsfexv1HealthCheck(this IHealthChecksBuilder builder,
        ArcaEnvironment environment = ArcaEnvironment.Homologation, TimeSpan? timeout = null, Uri? endpoint = null, string? name = null)
        => builder.AddNetArcaWsService(ArcaService.Wsfexv1, environment, timeout, endpoint, name);

    public static IHealthChecksBuilder AddWsmtxcaHealthCheck(this IHealthChecksBuilder builder,
        ArcaEnvironment environment = ArcaEnvironment.Homologation, TimeSpan? timeout = null, Uri? endpoint = null, string? name = null)
        => builder.AddNetArcaWsService(ArcaService.Wsmtxca, environment, timeout, endpoint, name);

    public static IHealthChecksBuilder AddPadronA4HealthCheck(this IHealthChecksBuilder builder,
        ArcaEnvironment environment = ArcaEnvironment.Homologation, TimeSpan? timeout = null, Uri? endpoint = null, string? name = null)
        => builder.AddNetArcaWsService(ArcaService.PadronA4, environment, timeout, endpoint, name);

    public static IHealthChecksBuilder AddPadronA5HealthCheck(this IHealthChecksBuilder builder,
        ArcaEnvironment environment = ArcaEnvironment.Homologation, TimeSpan? timeout = null, Uri? endpoint = null, string? name = null)
        => builder.AddNetArcaWsService(ArcaService.PadronA5, environment, timeout, endpoint, name);

    public static IHealthChecksBuilder AddPadronA10HealthCheck(this IHealthChecksBuilder builder,
        ArcaEnvironment environment = ArcaEnvironment.Homologation, TimeSpan? timeout = null, Uri? endpoint = null, string? name = null)
        => builder.AddNetArcaWsService(ArcaService.PadronA10, environment, timeout, endpoint, name);

    public static IHealthChecksBuilder AddPadronA13HealthCheck(this IHealthChecksBuilder builder,
        ArcaEnvironment environment = ArcaEnvironment.Homologation, TimeSpan? timeout = null, Uri? endpoint = null, string? name = null)
        => builder.AddNetArcaWsService(ArcaService.PadronA13, environment, timeout, endpoint, name);
}
