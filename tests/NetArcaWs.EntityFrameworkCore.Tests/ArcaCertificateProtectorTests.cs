using System.Security.Cryptography;
using AwesomeAssertions;
using NetArcaWs.EntityFrameworkCore;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.Tests;

public sealed class ArcaCertificateProtectorTests
{
    [Fact]
    public void ProtectRoundTripsWithCallerAadAndHidesCiphertext()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        using var protector = new AesGcmArcaCertificateProtector("active", new Dictionary<string, byte[]> { ["active"] = key });
        byte[] secret = RandomNumberGenerator.GetBytes(256);
        byte[] aad = "tenant-a/version-1"u8.ToArray();

        ArcaProtectedCertificate protectedValue = protector.Protect(secret, aad);

        protectedValue.Ciphertext.Should().NotEqual(secret);
        protectedValue.ToString().Should().NotContain(Convert.ToBase64String(protectedValue.Ciphertext));
        protector.Unprotect(protectedValue, aad).Should().Equal(secret);
        Action wrongAad = () => protector.Unprotect(protectedValue, "tenant-b/version-1"u8);
        wrongAad.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void ProtectorRejectsOversizedPayloadAndUnavailableKey()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        using var protector = new AesGcmArcaCertificateProtector("active", new Dictionary<string, byte[]> { ["active"] = key });
        Action oversized = () => protector.Protect(new byte[1024 * 1024 + 1], ReadOnlySpan<byte>.Empty);
        oversized.Should().Throw<ArgumentException>();
        ArcaProtectedCertificate payload = protector.Protect("secret"u8, ReadOnlySpan<byte>.Empty);
        using var empty = new AesGcmArcaCertificateProtector("other", new Dictionary<string, byte[]> { ["other"] = RandomNumberGenerator.GetBytes(32) });
        Action unavailable = () => empty.Unprotect(payload, ReadOnlySpan<byte>.Empty);
        unavailable.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void ProtectorCopiesKeysSupportsRetainedReadKeysAndDisposalIsFailClosed()
    {
        byte[] oldKey = RandomNumberGenerator.GetBytes(32), newKey = RandomNumberGenerator.GetBytes(32);
        byte[] original = oldKey.ToArray();
        using var oldWriter = new AesGcmArcaCertificateProtector("old", new Dictionary<string, byte[]> { ["old"] = oldKey });
        oldKey.AsSpan().Clear();
        ArcaProtectedCertificate oldValue = oldWriter.Protect("normalized envelope"u8, "scope"u8);
        oldValue.KeyId.Should().Be("old");
        using var rotated = new AesGcmArcaCertificateProtector("new", new Dictionary<string, byte[]> { ["old"] = original, ["new"] = newKey });
        rotated.Unprotect(oldValue, "scope"u8).Should().Equal("normalized envelope"u8.ToArray());
        rotated.Protect("new envelope"u8, "scope"u8).KeyId.Should().Be("new");

        var disposed = new AesGcmArcaCertificateProtector("key", new Dictionary<string, byte[]> { ["key"] = RandomNumberGenerator.GetBytes(32) });
        disposed.Dispose();
        Action afterDispose = () => disposed.Protect("x"u8, ReadOnlySpan<byte>.Empty);
        afterDispose.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ProtectorRejectsInvalidKeyMaterialAndEnvelopeTampering()
    {
        Action shortKey = () => new AesGcmArcaCertificateProtector("key", new Dictionary<string, byte[]> { ["key"] = new byte[31] });
        shortKey.Should().Throw<ArgumentException>();
        Action badId = () => new AesGcmArcaCertificateProtector("bad\nkey", new Dictionary<string, byte[]> { ["bad\nkey"] = new byte[32] });
        badId.Should().Throw<ArgumentException>();

        using var protector = new AesGcmArcaCertificateProtector("key", new Dictionary<string, byte[]> { ["key"] = RandomNumberGenerator.GetBytes(32) });
        ArcaProtectedCertificate payload = protector.Protect("secret"u8, "aad"u8);
        byte[] ciphertext = payload.Ciphertext; ciphertext[0] ^= 0x80;
        var tampered = new ArcaProtectedCertificate(payload.KeyId, payload.Nonce, ciphertext, payload.Tag);
        Action reject = () => protector.Unprotect(tampered, "aad"u8);
        reject.Should().Throw<CryptographicException>();
        byte[] copy = payload.Ciphertext; copy[0] ^= 0x80;
        protector.Unprotect(payload, "aad"u8).Should().Equal("secret"u8.ToArray());
    }

    [Fact]
    public void ConstructorRequiresExactActiveKeyIdFromOrdinalKeyRing()
    {
        byte[] key = RandomNumberGenerator.GetBytes(32);
        var caseInsensitiveKeyRing = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Active"] = key
        };

        Action mismatchedCase = () => new AesGcmArcaCertificateProtector("active", caseInsensitiveKeyRing);
        mismatchedCase.Should().Throw<ArgumentException>();

        using var exactMatch = new AesGcmArcaCertificateProtector("Active", caseInsensitiveKeyRing);
        exactMatch.Protect("certificate"u8, ReadOnlySpan<byte>.Empty).KeyId.Should().Be("Active");
    }

    [Fact]
    public void KeyIdsRejectUnpairedSurrogatesAndAcceptValidUnicode()
    {
        string[] invalidKeyIds = ["high\uD800", "low\uDC00", "mixed\uD800x\uDC00"];
        foreach (string invalidKeyId in invalidKeyIds)
        {
            Action activeId = () => new AesGcmArcaCertificateProtector(invalidKeyId, new Dictionary<string, byte[]> { [invalidKeyId] = new byte[32] });
            activeId.Should().Throw<ArgumentException>();

            Action keyRingId = () => new AesGcmArcaCertificateProtector("active", new Dictionary<string, byte[]>
            {
                ["active"] = new byte[32], [invalidKeyId] = new byte[32]
            });
            keyRingId.Should().Throw<ArgumentException>();
        }

        using var protector = new AesGcmArcaCertificateProtector("clave-🔐", new Dictionary<string, byte[]> { ["clave-🔐"] = RandomNumberGenerator.GetBytes(32) });
        ArcaProtectedCertificate payload = protector.Protect("certificate"u8, "scope"u8);
        payload.KeyId.Should().Be("clave-🔐");
        protector.Unprotect(payload, "scope"u8).Should().Equal("certificate"u8.ToArray());

        Action invalidPayloadKeyId = () => new ArcaProtectedCertificate("bad\uD800", new byte[12], new byte[0], new byte[16]);
        invalidPayloadKeyId.Should().Throw<ArgumentException>();
    }
}
