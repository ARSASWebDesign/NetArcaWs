using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;

namespace NetArcaWs.Multitenancy;

/// <summary>
/// Explicit, immutable tenant identity and credentials for one ARCA operation.
/// The application must resolve it from its authorized tenant, not from untrusted request values.
/// </summary>
public sealed record ArcaTenantContext
{
    public ArcaTenantContext(string tenantId, long cuit, ArcaEnvironment environment, WsaaCertificateContent certificate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (tenantId.Length > 128 || tenantId.Any(char.IsControl))
            throw new ArgumentException("Tenant identity must contain at most 128 characters without controls.", nameof(tenantId));
        if (cuit is < 10_000_000_000 or > 99_999_999_999)
            throw new ArgumentOutOfRangeException(nameof(cuit), "CUIT must contain eleven digits.");
        if (!Enum.IsDefined(environment)) throw new ArgumentOutOfRangeException(nameof(environment));
        ArgumentNullException.ThrowIfNull(certificate);
        TenantId = tenantId;
        Cuit = cuit;
        Environment = environment;
        Certificate = certificate;
    }

    public string TenantId { get; }
    /// <summary>Represented CUIT; ARCA determines authorization, including delegated certificates.</summary>
    public long Cuit { get; }
    public ArcaEnvironment Environment { get; }
    public WsaaCertificateContent Certificate { get; }

    public override string ToString() => "ARCA tenant context (identity and credentials hidden)";
}
