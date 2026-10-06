using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NetArcaWs.Cryptography;

/// <summary>
/// Immutable in-memory signing material obtained from configuration, a vault or a database.
/// Contains secrets; retain only as long as needed. No certificate files are created.
/// </summary>
[DebuggerDisplay("WsaaCertificateContent (secrets hidden)")]
public sealed class WsaaCertificateContent
{
    private readonly string? certificatePem;
    private readonly string? privateKeyPem;
    private readonly byte[]? pkcs12;
    private readonly string? password;

    private WsaaCertificateContent(string? certificatePem, string? privateKeyPem, byte[]? pkcs12, string? password)
    {
        this.certificatePem = certificatePem;
        this.privateKeyPem = privateKeyPem;
        this.pkcs12 = pkcs12;
        this.password = password;
    }

    /// <summary>Accepts literal PEM content, including real line breaks, and the matching private key.</summary>
    public static WsaaCertificateContent FromPem(string certificatePem, string privateKeyPem, string? password = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePem);
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPem);
        return new(certificatePem, privateKeyPem, null, password);
    }

    /// <summary>Copies a PFX/P12 payload. Parsing and password validation occur when loading it.</summary>
    public static WsaaCertificateContent FromPkcs12(byte[] data, string? password = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0) throw new ArgumentException("PKCS#12 content must not be empty.", nameof(data));
        return new(null, null, (byte[])data.Clone(), password);
    }

    /// <summary>Accepts Base64-encoded PFX/P12 content, not a path. Base64 is not encryption.</summary>
    public static WsaaCertificateContent FromPkcs12Base64(string base64, string? password = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(base64);
        var bytes = Convert.FromBase64String(base64);
        try { return FromPkcs12(bytes, password); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    /// <summary>
    /// Creates a new independently owned certificate with its RSA private key; the caller must dispose it.
    /// PFX requires platform support for EphemeralKeySet (Windows/Linux; use PEM on macOS).
    /// Certificate validity is checked by WsaaService when authenticating.
    /// </summary>
    public X509Certificate2 LoadCertificate()
    {
        if (pkcs12 is null) return WsaaCryptography.LoadPem(certificatePem!, privateKeyPem!, password);
        if (OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("Ephemeral PKCS#12 import is not supported on macOS. Use FromPem with the certificate and private key contents.");
        var certificate = X509CertificateLoader.LoadPkcs12(pkcs12, password, X509KeyStorageFlags.EphemeralKeySet);
        try
        {
            using var key = certificate.GetRSAPrivateKey()
                ?? throw new CryptographicException("The certificate must contain an RSA private key.");
            return certificate;
        }
        catch
        {
            certificate.Dispose();
            throw;
        }
    }

    /// <summary>Never returns secret material.</summary>
    public override string ToString() => "WsaaCertificateContent (secrets hidden)";
}
