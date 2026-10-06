using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AwesomeAssertions;
using NetArcaWs.Cryptography;
using NetArcaWs.Tests.TestSupport;
using Xunit;

namespace NetArcaWs.Tests.Cryptography;

public sealed class WsaaCryptographyTests
{
    private const string LegacyKeyPassword = "test-password";
    private const string LegacyCertificatePem = """
        -----BEGIN CERTIFICATE-----
        MIIDYzCCAkugAwIBAgIUcW/eDDdnBzw+qH8iDd7ZvOmFU70wDQYJKoZIhvcNAQEL
        BQAwQTEZMBcGA1UEAwwQbGVnYWN5LXdzYWEtdGVzdDEXMBUGA1UECgwOUHlBZmlw
        V3MgVGVzdHMxCzAJBgNVBAYTAkFSMB4XDTI2MTAwNjA0NTY1OVoXDTI2MTAwNzA0
        NTY1OVowQTEZMBcGA1UEAwwQbGVnYWN5LXdzYWEtdGVzdDEXMBUGA1UECgwOUHlB
        ZmlwV3MgVGVzdHMxCzAJBgNVBAYTAkFSMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A
        MIIBCgKCAQEAnD2nd+L0GZtFEDjMTWyV8au5ivGtVy3PWlqX9A5CgA5xHf5F75D1
        o+HV6krBsX2bIS9mvcRdYxVox017AWQoJ/33vCocj+yfs0RZ42lHjtFgu4D1MkXi
        eiZYcwKFjWJWwWaShBMhkaH/P5Gw6CvnA9aPO+4xaFZUYLl+u6sU6U35BYTa6Wc3
        9pjmyGzODWgwd/R1D9cH3B0jKkn7QKT0tA6UBK4WT6qiQI6SiJfcOUHj9XjSLu7c
        SUY2latLlUirRmK/rrCKAoHgiuTAdxM7it/Vo7yHfCxYz4Wl3ARIRl9hZgaiV7mU
        p6t9avxOxpiRbKzZ8qRkJQEVwcGoe7rJkQIDAQABo1MwUTAdBgNVHQ4EFgQUWJ5I
        kBIBd6SG3ORZBSVUXbKR1PkwHwYDVR0jBBgwFoAUWJ5IkBIBd6SG3ORZBSVUXbKR
        1PkwDwYDVR0TAQH/BAUwAwEB/zANBgkqhkiG9w0BAQsFAAOCAQEAXu7NT/sMNLlJ
        NRMpuHAnJMvlQCAynlxAgigK9ye71xZEDbFauMdUTBVaTc/dnBhz3aBKpMvtj+Gz
        PC7a4mTVyhK4jp41wMUjs+0mHuIPcQ5Wm8XnP18uorJmStqkuUjSbFRZfcOP7vU0
        q6RtxysECNJaGpbUXcdB1Mob2j3/SIM+2n/5K3AGMfvEYecK+pa2JuptiTHYlaxA
        zLcImsPS2+aaGsHoFaxwkBCuA5RDD74gVjOB6NpN7NvWoLeMZucyheT99f2yJ4YA
        QAzdrUzjassNDuSTudmmx/INwK1AUqeUEnKVQ0BEBLTZbyEedKZphs5Plq5UQWr9
        SghZtE6c7w==
        -----END CERTIFICATE-----
        """;
    private const string LegacyEncryptedRsaPem = """
        -----BEGIN RSA PRIVATE KEY-----
        Proc-Type: 4,ENCRYPTED
        DEK-Info: AES-256-CBC,AFBEC1887109DF9FD4880A1895B0A29B

        S2wqNM3+L1toAtZ8ibAMTcORPk9vTHvci25fhmJXpwPOFxtIGnsFstZMhd4yAyfv
        5ZJkRe4KgvSkeC80diL64fewDmAEAWLKE2UPdc3hRi6cQI4f20DJuuSqNLvx1gEe
        +KVouOApOTimP/StV859AJJA6M2FVHWs9i6vmRrcS9W6QHjagSDRz/z6gx206H3o
        RaC0xotRM3NeARlOOe4lZPaTAhyJrgdfaHDHkodtzXCJ5PHx/3CfwIyvZaFg4Gt+
        EcW4iC8MHrlyeZMqOG6o/MEwNI64vOr7vW4DUVK+/MLyV2shSSUBChg3aXpr01MM
        EsrlHdBfbwCWFtI6Y6bSJ5ffgRcBTfXjG3FLFf+c7StKeU/xByS3QTep0fKkcfTt
        IBGkn8jgTHOV0Wctnka++o3em96fBrOGj1fszDfinBU9CDNVTVuPRzaPd2h3WLHF
        w5KMJjv2UWEi0RRtXafqGdIfgL4OpTaUYIXM5a9Bsx5T3y7pveYqaTG1hYBT5G0B
        SwkfVjsUuQCgG8X062FVTuCb/SKD/Oyia2yjRx3GOOmd2aI0//+0iq5ueC9m+mam
        /uxZGM0hsNq8Q4N204yggYL+z//MgKclXAWYjZp3PQvyMhk8gIV0gH9RguEduOxs
        3Wb5zuEd14EBKDgO1cESeLKzcGqCh9HAgmt7FM+tNYAqmumcK7qTzlfVBCdKFhNn
        KIFgL0MG1V2BHqK6ICOo2WGZJcP+BGpXxRU7Hru9FoH+cK/Lv/qnBMOU8dPoqDVd
        2BgIAFCy+S1iV1/cRUjirUWU3uCd8DZ2nh0kwAQQFOEdcAnVyIzvptzo6a797XOc
        MoOU+vV3UoLG3nkNhdE67NBpQaSsv3Pz7+wkSLii9oOLN7Izz2bUeDnuCvAHS4H/
        rrRqq4DcfBQ8f/8GYA/AV1SdShfjoCwvSGLFsDep/2nl/5ER3W5/OcTEg9LTZCwg
        zK/oPTqZbSeOY08F5GdYfAwWvKU7cx910Xx0X+VG0nSvX+BSL13mjTOxb8qv/Dg8
        dna5hH5ugu4FDe4kcKrPgAOab+HdX6T/8D2WqzZg0NQZNMYpGRWICxATRAUJeo6d
        zhmw6NyAv54Hn8YiH5x9dLP724SEzHQVGwgiAAJZaACpY2Vtd+URNsCLoMA8DyZ/
        S1XcEwQnVrz1g9Zk+LJ59FnDKEpIgAoMcKjAuo0vf2ajSK1GFcSf+GXARHUuhgcl
        qgdMX9Yx3UDQh1ZHWzDV1bAN8XzsLrfsM3uuQrEIV63+aOjudaox+BK+2toHAOGU
        NgZudMjIGLOp2LugXkbS80atDyqffBF3RDymg9CXCFm3Rc6Htxt2TP9d89PSpNV7
        9LVO/VjJvYO4FZcOMEudhXNyXmPXsBFMgyY2BvuF6FgG1jEmwO7E7E++09GkfcGU
        Zp0cLU5acRqs2F0lQTAcsLfP9r5MROx04ln4DDViLvDgJFaN4jE+Wl8z0Qv7Z6Pl
        FCvcNlCttsqG+C3znJ4bcI7s3VDNQEKI0auZUyug1dMY7UDZzmf+aBgHE0RI/v8d
        CXODGjUYXV8DUGDASqHDDe+BRkek/rC3Azn4jY0WPx5PXgWW0RnB5cFlv7t6FDLk
        -----END RSA PRIVATE KEY-----
        """;

    [Fact]
    public void CreatePrivateKey_returns_importable_rsa_pem_with_requested_size()
    {
        string pem = WsaaCryptography.CreatePrivateKey(2048);

        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(pem);

        rsa.KeySize.Should().Be(2048);
        pem.Should().Contain("-----BEGIN PRIVATE KEY-----");
    }

    [Fact]
    public void CreatePrivateKey_can_protect_pkcs8_pem_with_a_password()
    {
        const string password = "unit-test-password";
        string pem = WsaaCryptography.CreatePrivateKey(2048, password);

        using RSA rsa = RSA.Create();
        rsa.ImportFromEncryptedPem(pem, password);

        rsa.KeySize.Should().Be(2048);
        pem.Should().Contain("-----BEGIN ENCRYPTED PRIVATE KEY-----");
    }

    [Fact]
    public void LoadPem_accepts_a_password_value_when_the_private_key_is_unencrypted()
    {
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Empty Password", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));

        using X509Certificate2 loaded = WsaaCryptography.LoadPem(
            generated.ExportCertificatePem(), rsa.ExportPkcs8PrivateKeyPem(), password: string.Empty);

        loaded.HasPrivateKey.Should().BeTrue();
        loaded.Thumbprint.Should().Be(generated.Thumbprint);
    }

    [Fact]
    public void LoadPem_loads_legacy_AES256_CBC_encrypted_TraditionalOpenSSL_RSA_PEM()
    {
        using X509Certificate2 loaded = WsaaCryptography.LoadPem(
            LegacyCertificatePem, LegacyEncryptedRsaPem, LegacyKeyPassword);

        loaded.HasPrivateKey.Should().BeTrue();
        loaded.Subject.Should().Contain("CN=legacy-wsaa-test");
        WsaaCryptography.SignTra("<tra />", loaded).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreateCertificateRequest_returns_valid_signed_csr_with_requested_subject()
    {
        using RSA key = RSA.Create(2048);

        string pem = WsaaCryptography.CreateCertificateRequest(
            key,
            commonName: "Mi Empresa",
            country: "AR",
            organization: "Empresa Ejemplo",
            cuit: "30123456789");

        CertificateRequest csr = CertificateRequest.LoadSigningRequestPem(
            pem,
            HashAlgorithmName.SHA256,
            CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);

        using RSA csrPublicKey = csr.PublicKey.GetRSAPublicKey()!;
        csrPublicKey.KeySize.Should().Be(2048);
        csr.SubjectName.Name.Should().Contain("CN=Mi Empresa");
        csr.SubjectName.Name.Should().Contain("O=Empresa Ejemplo");
        csr.SubjectName.Name.Should().Contain("C=AR");
        csr.SubjectName.Name.Should().Contain("30123456789");
    }

    [Fact]
    public void CreateCertificateRequest_normalizes_names_uppercases_country_and_encodes_CUIT_as_PrintableString()
    {
        using RSA key = RSA.Create(2048);
        string pem = WsaaCryptography.CreateCertificateRequest(key, "Peña", "ar", "Razón Social", "30123456789");
        CertificateRequest csr = CertificateRequest.LoadSigningRequestPem(
            pem, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);
        Dictionary<string, (string Value, UniversalTagNumber Tag)> attributes = ReadSubjectAttributes(csr.SubjectName.RawData);

        attributes["2.5.4.6"].Should().Be(("AR", UniversalTagNumber.PrintableString));
        attributes["2.5.4.10"].Should().Be(("Razo\u0301n Social", UniversalTagNumber.UTF8String));
        attributes["2.5.4.3"].Should().Be(("Pen\u0303a", UniversalTagNumber.UTF8String));
        attributes["2.5.4.5"].Should().Be(("CUIT 30123456789", UniversalTagNumber.PrintableString));
    }

    [Fact]
    public void SignTra_returns_attached_cms_whose_signature_and_content_can_be_verified()
    {
        using X509Certificate2 certificate = TestCertificates.CreateWithPrivateKey();
        const string tra = "<loginTicketRequest version=\"1.0\"><service>wsfe</service></loginTicketRequest>";

        string cmsBase64 = WsaaCryptography.SignTra(tra, certificate);

        Convert.FromBase64String(cmsBase64).Should().NotBeEmpty();
        var signedCms = new SignedCms();
        signedCms.Decode(Convert.FromBase64String(cmsBase64));
        signedCms.Detached.Should().BeFalse();
        signedCms.CheckSignature(verifySignatureOnly: true);
        System.Text.Encoding.UTF8.GetString(signedCms.ContentInfo.Content).Should().Be(tra);
        signedCms.SignerInfos.Count.Should().Be(1);
        signedCms.SignerInfos[0].DigestAlgorithm.Value.Should().Be(Oid.FromFriendlyName("SHA256", OidGroup.HashAlgorithm)!.Value);
    }

    [Fact]
    public void LoadPem_loads_matching_certificate_and_private_key()
    {
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Load PEM", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        string certificatePem = generated.ExportCertificatePem();
        string privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();

        using X509Certificate2 loaded = WsaaCryptography.LoadPem(certificatePem, privateKeyPem);

        loaded.HasPrivateKey.Should().BeTrue();
        loaded.Thumbprint.Should().Be(generated.Thumbprint);
        WsaaCryptography.SignTra("<tra />", loaded).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AnalyzeCertificate_reports_identity_issuer_dates_and_thumbprint()
    {
        using X509Certificate2 certificate = TestCertificates.Create("certificate-analysis");

        var info = WsaaCryptography.AnalyzeCertificate(certificate);

        info.Subject.Should().Be(certificate.Subject);
        info.Issuer.Should().Be(certificate.Issuer);
        info.NotBefore.Should().Be(new DateTimeOffset(certificate.NotBefore.ToUniversalTime()));
        info.NotAfter.Should().Be(new DateTimeOffset(certificate.NotAfter.ToUniversalTime()));
        info.SerialNumber.Should().Be(certificate.SerialNumber.TrimStart('0') is { Length: > 0 } normalized ? normalized : "0");
        info.Thumbprint.Should().Be(certificate.Thumbprint);
    }

    private static Dictionary<string, (string Value, UniversalTagNumber Tag)> ReadSubjectAttributes(byte[] encodedName)
    {
        var values = new Dictionary<string, (string Value, UniversalTagNumber Tag)>();
        var nameReader = new AsnReader(encodedName, AsnEncodingRules.DER);
        AsnReader relativeDistinguishedNames = nameReader.ReadSequence();
        while (relativeDistinguishedNames.HasData)
        {
            AsnReader attributes = relativeDistinguishedNames.ReadSetOf();
            while (attributes.HasData)
            {
                AsnReader attribute = attributes.ReadSequence();
                string oid = attribute.ReadObjectIdentifier();
                Asn1Tag valueTag = attribute.PeekTag();
                UniversalTagNumber stringTag = (UniversalTagNumber)valueTag.TagValue;
                string value = attribute.ReadCharacterString(stringTag);
                attribute.ThrowIfNotEmpty();
                values[oid] = (value, stringTag);
            }
        }
        nameReader.ThrowIfNotEmpty();
        return values;
    }
}
