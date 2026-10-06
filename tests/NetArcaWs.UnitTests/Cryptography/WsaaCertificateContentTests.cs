using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetArcaWs.Cryptography;
using NetArcaWs.Tests.TestSupport;
using NetArcaWs.Wsaa;
using Xunit;

namespace NetArcaWs.Tests.Cryptography;

public sealed class WsaaCertificateContentTests
{
    private const string Password = "memory-only-password 29cc";

    [Fact]
    public void FromPem_accepts_plain_pkcs8_key_and_returns_rsa_certificate_with_private_key()
    {
        using var source = TestCertificates.CreateWithPrivateKey("pem-plain");
        string certificatePem = source.ExportCertificatePem();
        string privateKeyPem = source.GetRSAPrivateKey()!.ExportPkcs8PrivateKeyPem();
        var content = WsaaCertificateContent.FromPem(certificatePem, privateKeyPem);

        using X509Certificate2 loaded = content.LoadCertificate();

        loaded.HasPrivateKey.Should().BeTrue();
        loaded.GetRSAPrivateKey().Should().NotBeNull();
        loaded.Thumbprint.Should().Be(source.Thumbprint);
    }

    [Fact]
    public void FromPem_accepts_encrypted_pkcs8_key_with_password()
    {
        using var source = TestCertificates.CreateWithPrivateKey("pem-encrypted");
        using RSA key = source.GetRSAPrivateKey()!;
        string privateKeyPem = key.ExportEncryptedPkcs8PrivateKeyPem(
            Password,
            new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000));
        var content = WsaaCertificateContent.FromPem(source.ExportCertificatePem(), privateKeyPem, Password);

        using X509Certificate2 loaded = content.LoadCertificate();

        loaded.HasPrivateKey.Should().BeTrue();
        loaded.Thumbprint.Should().Be(source.Thumbprint);
    }

    [Fact]
    public void FromPem_rejects_wrong_password_and_mismatched_private_key()
    {
        using var source = TestCertificates.CreateWithPrivateKey("pem-error");
        using RSA otherKey = RSA.Create(2048);
        string encryptedKey = source.GetRSAPrivateKey()!.ExportEncryptedPkcs8PrivateKeyPem(
            Password,
            new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000));
        Action wrongPassword = () => LoadAndDispose(WsaaCertificateContent.FromPem(
            source.ExportCertificatePem(), encryptedKey, "incorrect-password"));
        Action mismatchedKey = () => LoadAndDispose(WsaaCertificateContent.FromPem(
            source.ExportCertificatePem(), otherKey.ExportPkcs8PrivateKeyPem()));

        wrongPassword.Should().Throw<CryptographicException>();
        mismatchedKey.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void FromPkcs12_accepts_bytes_and_password()
    {
        using var source = TestCertificates.CreateWithPrivateKey("pfx-bytes");
        byte[] pfx = source.Export(X509ContentType.Pkcs12, Password);
        var content = WsaaCertificateContent.FromPkcs12(pfx, Password);

        if (AssertPfxPlatformNotSupported(content)) return;
        using X509Certificate2 loaded = content.LoadCertificate();

        loaded.HasPrivateKey.Should().BeTrue();
        loaded.Thumbprint.Should().Be(source.Thumbprint);
    }

    [Fact]
    public void FromPkcs12Base64_accepts_base64_and_password()
    {
        using var source = TestCertificates.CreateWithPrivateKey("pfx-base64");
        string pfxBase64 = Convert.ToBase64String(source.Export(X509ContentType.Pkcs12, Password));
        var content = WsaaCertificateContent.FromPkcs12Base64(pfxBase64, Password);

        if (AssertPfxPlatformNotSupported(content)) return;
        using X509Certificate2 loaded = content.LoadCertificate();

        loaded.HasPrivateKey.Should().BeTrue();
        loaded.Thumbprint.Should().Be(source.Thumbprint);
    }

    [Fact]
    public void FromPkcs12_rejects_wrong_password_empty_data_and_malformed_pfx()
    {
        using var source = TestCertificates.CreateWithPrivateKey("pfx-reject");
        byte[] pfx = source.Export(X509ContentType.Pkcs12, Password);

        Action wrongPassword = () => LoadAndDispose(WsaaCertificateContent.FromPkcs12(pfx, "incorrect-password"));
        Action empty = () => WsaaCertificateContent.FromPkcs12([]);
        Action malformed = () => LoadAndDispose(WsaaCertificateContent.FromPkcs12([0x01, 0x02, 0x03]));
        Action malformedBase64 = () => WsaaCertificateContent.FromPkcs12Base64("not base64!");

        if (OperatingSystem.IsMacOS())
        {
            wrongPassword.Should().Throw<PlatformNotSupportedException>();
            malformed.Should().Throw<PlatformNotSupportedException>();
        }
        else
        {
            wrongPassword.Should().Throw<CryptographicException>();
            malformed.Should().Throw<CryptographicException>();
        }
        empty.Should().Throw<ArgumentException>();
        malformedBase64.Should().Throw<FormatException>();
    }

    [Fact]
    public void FromPkcs12_rejects_certificate_without_private_key_and_non_rsa_key()
    {
        using var source = TestCertificates.CreateWithPrivateKey("pfx-no-key");
        using var publicOnly = X509CertificateLoader.LoadCertificate(source.RawData);
        byte[] publicOnlyPfx = publicOnly.Export(X509ContentType.Pkcs12, Password);
        Action noPrivateKey = () => LoadAndDispose(WsaaCertificateContent.FromPkcs12(publicOnlyPfx, Password));

        using ECDsa ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=EC content", ec, HashAlgorithmName.SHA256);
        using X509Certificate2 ecCertificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        byte[] ecPfx = ecCertificate.Export(X509ContentType.Pkcs12, Password);
        Action ecKey = () => LoadAndDispose(WsaaCertificateContent.FromPkcs12(ecPfx, Password));

        if (OperatingSystem.IsMacOS())
        {
            noPrivateKey.Should().Throw<PlatformNotSupportedException>();
            ecKey.Should().Throw<PlatformNotSupportedException>();
        }
        else
        {
            noPrivateKey.Should().Throw<CryptographicException>();
            ecKey.Should().Throw<CryptographicException>();
        }
    }

    [Fact]
    public void FromPkcs12_copies_input_bytes_and_does_not_retain_a_mutable_caller_buffer()
    {
        using var source = TestCertificates.CreateWithPrivateKey("pfx-copy");
        byte[] original = source.Export(X509ContentType.Pkcs12, Password);
        var content = WsaaCertificateContent.FromPkcs12(original, Password);
        Array.Fill(original, (byte)0);

        if (AssertPfxPlatformNotSupported(content)) return;
        using X509Certificate2 loaded = content.LoadCertificate();

        loaded.HasPrivateKey.Should().BeTrue();
        loaded.Thumbprint.Should().Be(source.Thumbprint);
    }

    [Fact]
    public void ToString_and_json_serialization_do_not_reveal_certificate_or_private_key_material()
    {
        using var source = TestCertificates.CreateWithPrivateKey("redaction");
        string certificatePem = source.ExportCertificatePem();
        string privateKeyPem = source.GetRSAPrivateKey()!.ExportPkcs8PrivateKeyPem();
        var content = WsaaCertificateContent.FromPem(certificatePem, privateKeyPem);

        content.ToString().Should().NotContain(certificatePem);
        content.ToString().Should().NotContain(privateKeyPem);
        string json = JsonSerializer.Serialize(content);
        json.Should().NotContain("PRIVATE KEY");
        json.Should().NotContain("CERTIFICATE");
        json.Should().NotContain(Password);
    }

    [Fact]
    public async Task WsaaService_uses_configured_content_and_cache_is_stable_across_import_instances_and_rotation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        int requestCount = 0;
        var handler = new RecordingHttpMessageHandler((_, _) =>
        {
            Interlocked.Increment(ref requestCount);
            return Task.FromResult(RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(now.AddMinutes(-1), now.AddHours(1)))));
        });
        var clock = new AdjustableTimeProvider(now);
        using var firstCertificate = TestCertificates.CreateWithPrivateKey("content-configured");
        using var rotatedCertificate = TestCertificates.CreateWithPrivateKey("content-rotated");
        WsaaCertificateContent configuredContent = FromCertificate(firstCertificate);
        WsaaCertificateContent reimportedContent = FromCertificate(firstCertificate);
        WsaaCertificateContent rotatedContent = FromCertificate(rotatedCertificate);
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);
        services.AddNetArcaWs(options => options.Certificate = configuredContent)
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        using ServiceProvider provider = services.BuildServiceProvider();
        WsaaService service = provider.GetRequiredService<WsaaService>();

        await service.AuthenticateAsync("wsfe", cancellationToken);
        await service.AuthenticateAsync("wsfe", cancellationToken);
        await service.AuthenticateWithContentAsync("wsfe", reimportedContent, cancellationToken);
        await service.AuthenticateWithContentAsync("wsfe", rotatedContent, cancellationToken);

        requestCount.Should().Be(2, "a reimport of the same key reuses the cached ticket while key rotation has a distinct cache identity");
    }

    [Fact]
    public async Task WsaaService_requires_configured_content_and_honors_cancellation_for_per_call_content()
    {
        CancellationToken testToken = TestContext.Current.CancellationToken;
        var handler = new RecordingHttpMessageHandler((_, _) => Task.FromResult(
            RecordingHttpMessageHandler.Response(HttpStatusCode.OK,
                WsaaTestData.SoapSuccess(WsaaTestData.TicketXml(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1))))));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new WsaaService(
            new TestHttpClientFactory(handler),
            cache,
            Options.Create(new WsaaOptions()),
            new AdjustableTimeProvider(DateTimeOffset.UtcNow));

        Func<Task> missingConfiguration = () => service.AuthenticateAsync("wsfe", testToken);
        await missingConfiguration.Should().ThrowAsync<InvalidOperationException>();

        using var certificate = TestCertificates.CreateWithPrivateKey("content-cancel");
        WsaaCertificateContent content = FromCertificate(certificate);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Func<Task> cancellation = () => service.AuthenticateWithContentAsync("wsfe", content, cancelled.Token);

        await cancellation.Should().ThrowAsync<OperationCanceledException>();
        handler.RequestCount.Should().Be(0);
    }

    private static WsaaCertificateContent FromCertificate(X509Certificate2 certificate)
    {
        using RSA privateKey = certificate.GetRSAPrivateKey()!;
        return WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), privateKey.ExportPkcs8PrivateKeyPem());
    }

    private static bool AssertPfxPlatformNotSupported(WsaaCertificateContent content)
    {
        if (!OperatingSystem.IsMacOS()) return false;
        Action load = () => LoadAndDispose(content);
        load.Should().Throw<PlatformNotSupportedException>();
        return true;
    }

    private static void LoadAndDispose(WsaaCertificateContent content)
    {
        using X509Certificate2 certificate = content.LoadCertificate();
    }
}
