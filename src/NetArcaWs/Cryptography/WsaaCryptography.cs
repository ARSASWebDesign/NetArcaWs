using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace NetArcaWs.Cryptography;

/// <summary>Cryptographic operations used by the AFIP WSAA protocol.</summary>
public static class WsaaCryptography
{
    private static readonly Oid Sha256WithRsaEncryptionOid = new("1.2.840.113549.1.1.11");
    private static readonly Oid CountryNameOid = new("2.5.4.6");
    private static readonly Oid OrganizationNameOid = new("2.5.4.10");
    private static readonly Oid CommonNameOid = new("2.5.4.3");
    private static readonly Oid SerialNumberOid = new("2.5.4.5");

    /// <summary>
    /// Signs a TRA as an attached CMS SignedData object and returns its base64 encoding.
    /// The content is signed as UTF-8 bytes without MIME canonicalization.
    /// </summary>
    public static string SignTra(string tra, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(tra);
        ArgumentNullException.ThrowIfNull(certificate);

        using RSA privateKey = certificate.GetRSAPrivateKey()
            ?? throw new CryptographicException("The certificate must contain an RSA private key.");

        EnsureKeyMatchesCertificate(certificate, privateKey);

        var content = new ContentInfo(Encoding.UTF8.GetBytes(tra));
        var signedCms = new SignedCms(content, detached: false);
        var signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate)
        {
            DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1"),
            IncludeOption = X509IncludeOption.EndCertOnly
        };

        signedCms.ComputeSignature(signer, silent: true);
        return Convert.ToBase64String(signedCms.Encode());
    }

    /// <summary>Creates an RSA private key encoded as an unencrypted PKCS#8 PEM.</summary>
    public static string CreatePrivateKey(int keySize = 4096, string? password = null)
    {
        if (keySize < 2048 || keySize % 8 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(keySize), keySize,
                "RSA keys must be at least 2048 bits and have a size divisible by 8.");
        }

        using RSA key = RSA.Create(keySize);
        if (string.IsNullOrEmpty(password))
        {
            return key.ExportPkcs8PrivateKeyPem();
        }

        var encryption = new PbeParameters(
            PbeEncryptionAlgorithm.Aes256Cbc,
            HashAlgorithmName.SHA256,
            iterationCount: 100_000);
        return key.ExportEncryptedPkcs8PrivateKeyPem(password, encryption);
    }

    /// <summary>
    /// Creates a PKCS#10 request signed with SHA-256 and RSA PKCS#1 v1.5.
    /// Its subject follows pyafipws: C=AR, O=organization, CN=commonName,
    /// serialNumber="CUIT {cuit}".
    /// </summary>
    public static string CreateCertificateRequest(
        RSA key,
        string commonName,
        string country,
        string organization,
        string cuit)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(commonName);
        ArgumentNullException.ThrowIfNull(country);
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(cuit);

        if (country.Length != 2 || country.Any(character => !char.IsAsciiLetter(character)))
        {
            throw new ArgumentException("The country name must be a two-letter ASCII code.", nameof(country));
        }

        if (key.KeySize < 2048)
        {
            throw new ArgumentException("RSA keys must be at least 2048 bits.", nameof(key));
        }

        byte[] subject = EncodeSubject(
            country.ToUpperInvariant(),
            organization.Normalize(NormalizationForm.FormKD),
            commonName.Normalize(NormalizationForm.FormKD),
            $"CUIT {cuit}");
        byte[] subjectPublicKeyInfo = key.ExportSubjectPublicKeyInfo();

        var infoWriter = new AsnWriter(AsnEncodingRules.DER);
        using (infoWriter.PushSequence())
        {
            infoWriter.WriteInteger(0);
            infoWriter.WriteEncodedValue(subject);
            infoWriter.WriteEncodedValue(subjectPublicKeyInfo);
            // CertificationRequestInfo.attributes is [0] IMPLICIT SET OF Attribute.
            infoWriter.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 0)).Dispose();
        }

        byte[] requestInfo = infoWriter.Encode();
        byte[] signature = key.SignData(requestInfo, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var requestWriter = new AsnWriter(AsnEncodingRules.DER);
        using (requestWriter.PushSequence())
        {
            requestWriter.WriteEncodedValue(requestInfo);
            using (requestWriter.PushSequence())
            {
                requestWriter.WriteObjectIdentifier(Sha256WithRsaEncryptionOid.Value!);
                requestWriter.WriteNull();
            }

            requestWriter.WriteBitString(signature);
        }

        return PemEncoding.WriteString("CERTIFICATE REQUEST", requestWriter.Encode());
    }

    /// <summary>Extracts the commonly used identity, validity, issuer and serial fields.</summary>
    public static CertificateInfo AnalyzeCertificate(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        return new CertificateInfo(
            certificate.Subject,
            new DateTimeOffset(certificate.NotBefore.ToUniversalTime()),
            new DateTimeOffset(certificate.NotAfter.ToUniversalTime()),
            certificate.Issuer,
            NormalizeSerialNumber(certificate.SerialNumber),
            certificate.Thumbprint ?? string.Empty);
    }

    /// <summary>
    /// Loads a PEM certificate and its matching RSA private key. Encrypted PKCS#8 is supported
    /// by .NET; traditional OpenSSL RSA PEM supports the upstream AES-256-CBC format.
    /// </summary>
    public static X509Certificate2 LoadPem(
        string certificatePem,
        string privateKeyPem,
        string? password = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePem);
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPem);
        if (password is "") password = null;

        X509Certificate2 certificate;
        if (password is not null && IsLegacyEncryptedRsaPem(privateKeyPem))
        {
            certificate = CreateFromLegacyEncryptedRsaPem(certificatePem, privateKeyPem, password);
        }
        else
        {
            certificate = password is null
                ? X509Certificate2.CreateFromPem(certificatePem, privateKeyPem)
                : X509Certificate2.CreateFromEncryptedPem(certificatePem, privateKeyPem, password);
        }

        try
        {
            using RSA privateKey = certificate.GetRSAPrivateKey()
                ?? throw new CryptographicException("The PEM private key must be RSA.");
            EnsureKeyMatchesCertificate(certificate, privateKey);
            return certificate;
        }
        catch
        {
            certificate.Dispose();
            throw;
        }
    }

    private static byte[] EncodeSubject(string country, string organization, string commonName, string serialNumber)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        {
            WriteNameAttribute(writer, CountryNameOid, country, UniversalTagNumber.PrintableString);
            WriteNameAttribute(writer, OrganizationNameOid, organization, UniversalTagNumber.UTF8String);
            WriteNameAttribute(writer, CommonNameOid, commonName, UniversalTagNumber.UTF8String);
            WriteNameAttribute(writer, SerialNumberOid, serialNumber, UniversalTagNumber.PrintableString);
        }

        return writer.Encode();
    }

    private static void WriteNameAttribute(
        AsnWriter writer,
        Oid oid,
        string value,
        UniversalTagNumber stringType)
    {
        using (writer.PushSetOf())
        using (writer.PushSequence())
        {
            writer.WriteObjectIdentifier(oid.Value!);
            writer.WriteCharacterString(stringType, value);
        }
    }

    private static void EnsureKeyMatchesCertificate(X509Certificate2 certificate, RSA privateKey)
    {
        using RSA publicKey = certificate.GetRSAPublicKey()
            ?? throw new CryptographicException("The certificate public key must be RSA.");

        RSAParameters publicParameters = publicKey.ExportParameters(includePrivateParameters: false);
        RSAParameters privatePublicParameters = privateKey.ExportParameters(includePrivateParameters: false);
        if (!publicParameters.Modulus!.AsSpan().SequenceEqual(privatePublicParameters.Modulus)
            || !publicParameters.Exponent!.AsSpan().SequenceEqual(privatePublicParameters.Exponent))
        {
            throw new CryptographicException("The certificate and private key do not match.");
        }
    }

    private static string NormalizeSerialNumber(string serialNumber) =>
        serialNumber.TrimStart('0') is { Length: > 0 } normalized ? normalized : "0";

    private static bool IsLegacyEncryptedRsaPem(string privateKeyPem) =>
        privateKeyPem.Contains("-----BEGIN RSA PRIVATE KEY-----", StringComparison.Ordinal)
        && privateKeyPem.Contains("Proc-Type: 4,ENCRYPTED", StringComparison.Ordinal);

    private static X509Certificate2 CreateFromLegacyEncryptedRsaPem(
        string certificatePem,
        string privateKeyPem,
        string password)
    {
        string[] lines = privateKeyPem.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        int dekInfoIndex = Array.FindIndex(lines, line => line.StartsWith("DEK-Info:", StringComparison.Ordinal));
        if (dekInfoIndex < 0 || dekInfoIndex + 1 >= lines.Length)
        {
            throw new CryptographicException("The encrypted RSA PEM is missing its DEK-Info header.");
        }

        string[] encryptionParameters = lines[dekInfoIndex]["DEK-Info:".Length..].Trim().Split(',', 2);
        if (encryptionParameters.Length != 2
            || !string.Equals(encryptionParameters[0], "AES-256-CBC", StringComparison.Ordinal))
        {
            throw new CryptographicException("Only traditional RSA PEM encrypted with AES-256-CBC is supported.");
        }

        byte[] iv;
        try
        {
            iv = Convert.FromHexString(encryptionParameters[1]);
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("The encrypted RSA PEM has an invalid IV.", exception);
        }

        if (iv.Length != 16)
        {
            CryptographicOperations.ZeroMemory(iv);
            throw new CryptographicException("The AES-256-CBC IV must contain 16 bytes.");
        }

        int bodyStart = Array.FindIndex(lines, dekInfoIndex + 1, line => string.IsNullOrWhiteSpace(line));
        if (bodyStart < 0)
        {
            CryptographicOperations.ZeroMemory(iv);
            throw new CryptographicException("The encrypted RSA PEM is missing its base64 body.");
        }

        string base64Body = string.Concat(lines.Skip(bodyStart + 1)
            .TakeWhile(line => !line.StartsWith("-----END", StringComparison.Ordinal)));

        byte[] encrypted;
        try
        {
            encrypted = Convert.FromBase64String(base64Body);
        }
        catch (FormatException exception)
        {
            CryptographicOperations.ZeroMemory(iv);
            throw new CryptographicException("The encrypted RSA PEM has an invalid base64 body.", exception);
        }

        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] key = DeriveLegacyOpenSslKey(passwordBytes, iv.AsSpan(0, 8));
        byte[] privateKeyDer = [];
        try
        {
            using Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            privateKeyDer = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

            using RSA privateKey = RSA.Create();
            privateKey.ImportRSAPrivateKey(privateKeyDer, out int bytesRead);
            if (bytesRead != privateKeyDer.Length)
            {
                throw new CryptographicException("The encrypted RSA PEM contains trailing data.");
            }

            using X509Certificate2 certificateWithoutKey = X509Certificate2.CreateFromPem(certificatePem);
            return certificateWithoutKey.CopyWithPrivateKey(privateKey);
        }
        catch (CryptographicException exception)
        {
            throw new CryptographicException(
                "The traditional RSA PEM could not be decrypted or did not match its certificate.", exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(iv);
            CryptographicOperations.ZeroMemory(encrypted);
            CryptographicOperations.ZeroMemory(privateKeyDer);
        }
    }

    private static byte[] DeriveLegacyOpenSslKey(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt)
    {
        byte[] derivedKey = new byte[32];
        byte[] previousBlock = [];
        int written = 0;
        try
        {
            while (written < derivedKey.Length)
            {
                byte[] derivationInput = new byte[previousBlock.Length + password.Length + salt.Length];
                previousBlock.CopyTo(derivationInput, 0);
                password.CopyTo(derivationInput.AsSpan(previousBlock.Length));
                salt.CopyTo(derivationInput.AsSpan(previousBlock.Length + password.Length));

                byte[] block = MD5.HashData(derivationInput);
                CryptographicOperations.ZeroMemory(derivationInput);
                CryptographicOperations.ZeroMemory(previousBlock);
                int count = Math.Min(block.Length, derivedKey.Length - written);
                block.AsSpan(0, count).CopyTo(derivedKey.AsSpan(written));
                written += count;
                previousBlock = block;
            }

            return derivedKey;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(derivedKey);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(previousBlock);
        }
    }
}
