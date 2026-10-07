using System.Security.Cryptography;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;

namespace NetArcaWs.EntityFrameworkCore;

/// <summary>Identity descriptor for certificate storage. Creating it does not authorize access to the identity.</summary>
public sealed record ArcaCertificateScope
{
    public ArcaCertificateScope(string tenantId, long cuit, ArcaEnvironment environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (tenantId.Length > 128 || tenantId.Any(char.IsControl)) throw new ArgumentException("Tenant identity must contain at most 128 characters without controls.", nameof(tenantId));
        if (cuit is < 10_000_000_000 or > 99_999_999_999) throw new ArgumentOutOfRangeException(nameof(cuit), "CUIT must contain eleven digits.");
        if (!Enum.IsDefined(environment)) throw new ArgumentOutOfRangeException(nameof(environment));
        TenantId = tenantId; Cuit = cuit; Environment = environment;
    }
    public string TenantId { get; }
    public long Cuit { get; }
    public ArcaEnvironment Environment { get; }
    public override string ToString() => "ARCA certificate scope (identity hidden)";
}

public sealed record ArcaCertificateVersion(Guid VersionId, DateTimeOffset CreatedAtUtc, string ThumbprintSha256,
    DateTimeOffset NotBeforeUtc, DateTimeOffset NotAfterUtc, bool IsActive);

public sealed record ArcaStoredCertificate(ArcaCertificateVersion Metadata, WsaaCertificateContent Content)
{
    public override string ToString() => "Stored ARCA certificate (metadata and content hidden)";
}

public interface IArcaCertificateStore
{
    Task<ArcaCertificateVersion> RotateAsync(ArcaCertificateScope scope, WsaaCertificateContent content,
        Guid? expectedActiveVersionId = null, CancellationToken cancellationToken = default);
    Task<ArcaStoredCertificate?> GetActiveAsync(ArcaCertificateScope scope, CancellationToken cancellationToken = default);
    Task<ArcaStoredCertificate?> GetVersionAsync(ArcaCertificateScope scope, Guid versionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArcaCertificateVersion>> ListVersionsAsync(ArcaCertificateScope scope, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArcaCertificateVersion>> FindExpiringAsync(ArcaCertificateScope scope, TimeSpan warningWindow,
        DateTimeOffset asOfUtc, CancellationToken cancellationToken = default);
}

public sealed class ArcaCertificateConcurrencyException(string message) : InvalidOperationException(message);
public sealed class ArcaCertificateValidityException() : InvalidOperationException("The selected ARCA certificate is outside its validity period.");
public sealed class ArcaCertificateDataException() : CryptographicException("Stored ARCA certificate data could not be authenticated or validated.");
