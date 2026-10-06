namespace NetArcaWs.Cryptography;

/// <summary>Identity and validity details extracted from an X.509 certificate.</summary>
public sealed record CertificateInfo(
    string Subject,
    DateTimeOffset NotBefore,
    DateTimeOffset NotAfter,
    string Issuer,
    string SerialNumber,
    string Thumbprint);
