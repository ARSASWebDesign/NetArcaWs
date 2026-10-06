using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Taxation;
using NetArcaWs.Wsaa;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Services;
using NetArcaWs.Contracts.Wscdc;
using NetArcaWs.Contracts.WsfeCred;

namespace NetArcaWs.Examples;

/// <summary>
/// Compilable counterparts for the repository's authentication, tenant, health,
/// safe-invoice, tax-calculation, and certificate-tool guides. These methods are
/// examples only; constructing this class or building its values makes no network call.
/// </summary>
public static class GuideExamples
{
    /// <summary>Reads the CPE province catalog for a tenant authorized for wscpe.</summary>
    public static Task<NetArcaWs.Contracts.Wscpe.ConsultarProvinciasResponse> ReadCpeProvincesAsync(
        IServiceProvider provider, ArcaTenantContext tenant, CancellationToken cancellationToken)
        => provider.GetRequiredService<IWscpeService>().consultarProvinciasAsync(
            tenant, new NetArcaWs.Contracts.Wscpe.ConsultarProvinciasRequest(), cancellationToken);

    /// <summary>Reads WSCDC modalities for a tenant already authorized by the host.</summary>
    public static Task<ComprobantesModalidadConsultarResponse> ReadVerificationModesAsync(
        IServiceProvider provider, ArcaTenantContext tenant, CancellationToken cancellationToken)
        => provider.GetRequiredService<IWscdcService>().ComprobantesModalidadConsultarAsync(
            tenant, new ComprobantesModalidadConsultar(), cancellationToken);

    /// <summary>Reads FCE retention types; callers must inspect the returned business errors.</summary>
    public static Task<ConsultarTiposRetencionesResponseType> ReadCreditRetentionTypesAsync(
        IServiceProvider provider, ArcaTenantContext tenant, CancellationToken cancellationToken)
        => provider.GetRequiredService<IWsfecredService>().consultarTiposRetencionesAsync(
            tenant, new ConsultarTiposRetencionesRequest(), cancellationToken);

    /// <summary>Wraps PEM material supplied by the host's protected configuration or secret store.</summary>
    public static WsaaCertificateContent CreateCertificateContent(string certificatePem,
        string privateKeyPem, string? password = null)
        => WsaaCertificateContent.FromPem(certificatePem, privateKeyPem, password);

    /// <summary>Registers WSAA with certificate content already resolved by the host.</summary>
    public static IHttpClientBuilder ConfigureWsaa(IServiceCollection services,
        WsaaCertificateContent certificate, ArcaEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(certificate);
        return services.AddNetArcaWs(options =>
        {
            options.Endpoint = environment == ArcaEnvironment.Production
                ? WsaaOptions.ProductionEndpoint
                : WsaaOptions.HomologationEndpoint;
            options.Certificate = certificate;
        });
    }

    /// <summary>Creates an authenticated tenant context from values resolved by the host.</summary>
    public static ArcaTenantContext CreateTenant(string tenantId, long representedCuit,
        ArcaEnvironment environment, WsaaCertificateContent certificate)
        => new(tenantId, representedCuit, environment, certificate);

    /// <summary>Authenticates one tenant for a service; callers must protect the returned ticket.</summary>
    public static Task<WsaaTicket> AuthenticateTenantAsync(WsaaService wsaa,
        ArcaTenantContext tenant, string service, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wsaa);
        ArgumentNullException.ThrowIfNull(tenant);
        return wsaa.AuthenticateForTenantAsync(service, tenant, cancellationToken);
    }

    /// <summary>Authenticates the configured default identity for one service.</summary>
    public static Task<WsaaTicket> AuthenticateConfiguredAsync(WsaaService wsaa,
        string service, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wsaa);
        return wsaa.AuthenticateAsync(service, cancellationToken);
    }

    /// <summary>Opt-in health registrations; checks probe service availability without WSAA credentials.</summary>
    public static IHealthChecksBuilder ConfigureHealthChecks(IHealthChecksBuilder checks)
        => checks
            .AddWsaaHealthCheck(ArcaEnvironment.Production, timeout: TimeSpan.FromSeconds(3))
            .AddWsfev1HealthCheck(ArcaEnvironment.Production, timeout: TimeSpan.FromSeconds(3));

    /// <summary>
    /// Adds one caller-validated fiscal detail to a single-voucher WSFE request.
    /// The host must populate and validate all protocol and business fields before calling this method.
    /// </summary>
    public static FecaeRequest CreateSingleVoucherRequest(int pointOfSale, int voucherType,
        FecaeDetRequest validatedDetail)
    {
        ArgumentNullException.ThrowIfNull(validatedDetail);
        return new FecaeRequest
        {
            FeCabReq = new FecaeCabRequest
            {
                CantReg = 1,
                PtoVta = pointOfSale,
                CbteTipo = voucherType
            },
            FeDetReq =
            {
                validatedDetail
            }
        };
    }

    /// <summary>Persists and submits the single-voucher request through the durable journal.</summary>
    public static Task<InvoiceOperation> AuthorizeOneAsync(SafeInvoiceService invoices,
        ArcaTenantContext tenant, string idempotencyKey, FecaeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoices);
        return invoices.AuthorizeWsfeAsync(tenant, idempotencyKey, request, cancellationToken);
    }

    /// <summary>Registers durable single-voucher support backed by a local SQLite journal.</summary>
    public static IServiceCollection ConfigureSqliteInvoicing(IServiceCollection services,
        string databasePath)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddNetArcaWsSqliteInvoicing(databasePath);
    }

    /// <summary>Reconciles a previously recorded operation without resubmitting it.</summary>
    public static Task<InvoiceOperation> ReconcileAsync(SafeInvoiceService invoices,
        ArcaTenantContext tenant, string idempotencyKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoices);
        return invoices.ReconcileAsync(tenant, idempotencyKey, cancellationToken);
    }

    /// <summary>Uses the explicit arithmetic helper; tax classification/rates remain application decisions.</summary>
    public static TaxBreakdown CalculateFromGross(decimal gross, decimal ratePercent)
        => TaxCalculator.FromGross(gross, ratePercent);

    /// <summary>
    /// Prepares the local certificate-generation CLI invocation. The caller may start it
    /// explicitly; this method does not execute a process or set/read secret values.
    /// </summary>
    public static ProcessStartInfo CreateDevelopmentCertificateCommand(string cuit,
        string organization, string commonName, string outputDirectory, string passwordEnvironmentName)
    {
        var startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        foreach (string argument in new[]
        {
            "tool", "run", "netarcaws", "cert-dev",
            "--cuit", cuit,
            "--organization", organization,
            "--name", commonName,
            "--output", outputDirectory,
            "--password-env", passwordEnvironmentName
        })
            startInfo.ArgumentList.Add(argument);
        return startInfo;
    }
}
