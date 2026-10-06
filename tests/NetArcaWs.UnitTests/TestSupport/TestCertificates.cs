using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NetArcaWs.Tests.TestSupport;

internal static class TestCertificates
{
    internal static X509Certificate2 Create(string commonName = "wsaa-tests", int keySize = 2048)
    {
        using RSA rsa = RSA.Create(keySize);
        var request = new CertificateRequest(
            $"CN={commonName}, O=NetArcaWs Tests, C=AR",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(1));
    }

    internal static X509Certificate2 CreateWithPrivateKey(string commonName = "wsaa-tests", int keySize = 2048) =>
        Create(commonName, keySize);
}
