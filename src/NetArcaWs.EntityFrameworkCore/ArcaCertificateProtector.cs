using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace NetArcaWs.EntityFrameworkCore;

/// <summary>Encrypted certificate envelope. Secret-bearing buffers are immutable and copied on access.</summary>
public sealed class ArcaProtectedCertificate
{
    private readonly byte[] nonce, ciphertext, tag;
    public ArcaProtectedCertificate(string keyId, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag)
    {
        ArcaCertificateValidation.ValidateKeyId(keyId);
        if (nonce.Length != 12 || tag.Length != 16 || ciphertext.Length > AesGcmArcaCertificateProtector.MaximumPlaintextBytes)
            throw new ArgumentException("The protected certificate envelope is malformed.");
        KeyId = keyId;
        this.nonce = nonce.ToArray(); this.ciphertext = ciphertext.ToArray(); this.tag = tag.ToArray();
    }
    public string KeyId { get; }
    public byte[] Nonce => nonce.ToArray();
    public byte[] Ciphertext => ciphertext.ToArray();
    public byte[] Tag => tag.ToArray();
    public override string ToString() => $"Protected ARCA certificate (key {KeyId}, content redacted)";
}

public interface IArcaCertificateProtector
{
    ArcaProtectedCertificate Protect(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData);
    byte[] Unprotect(ArcaProtectedCertificate payload, ReadOnlySpan<byte> associatedData);
}

/// <summary>AES-256-GCM protector backed by an application-managed key ring.</summary>
public sealed class AesGcmArcaCertificateProtector : IArcaCertificateProtector, IDisposable
{
    public const int MaximumPlaintextBytes = 1024 * 1024;
    private const int NonceSize = 12, TagSize = 16;
    private static readonly byte[] Domain = "NetArcaWs/tenant-certificate/v1"u8.ToArray();
    private readonly Dictionary<string, byte[]> keys = new(StringComparer.Ordinal);
    private readonly string activeKeyId;
    private readonly object sync = new();
    private bool disposed;

    public AesGcmArcaCertificateProtector(string activeKeyId, IReadOnlyDictionary<string, byte[]> keyRing)
    {
        ArcaCertificateValidation.ValidateKeyId(activeKeyId);
        ArgumentNullException.ThrowIfNull(keyRing);
        if (!keyRing.ContainsKey(activeKeyId)) throw new ArgumentException("The active encryption key is unavailable.", nameof(activeKeyId));
        try
        {
            foreach ((string id, byte[] key) in keyRing)
            {
                ArcaCertificateValidation.ValidateKeyId(id);
                ArgumentNullException.ThrowIfNull(key);
                if (key.Length != 32) throw new ArgumentException("AES-256-GCM keys must contain exactly 32 bytes.", nameof(keyRing));
                if (!keys.TryAdd(id, key.ToArray())) throw new ArgumentException("Key IDs must be unique.", nameof(keyRing));
            }
            this.activeKeyId = activeKeyId;
        }
        catch { ClearKeys(); throw; }
    }

    public ArcaProtectedCertificate Protect(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData)
    {
        if (plaintext.Length > MaximumPlaintextBytes) throw new ArgumentException("Certificate envelope exceeds the 1 MiB limit.", nameof(plaintext));
        lock (sync)
        {
            ThrowIfDisposed();
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize), ciphertext = new byte[plaintext.Length], tag = new byte[TagSize];
            byte[] aad = ComposeAad(activeKeyId, associatedData);
            using var aes = new AesGcm(keys[activeKeyId], TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);
            return new ArcaProtectedCertificate(activeKeyId, nonce, ciphertext, tag);
        }
    }

    public byte[] Unprotect(ArcaProtectedCertificate payload, ReadOnlySpan<byte> associatedData)
    {
        ArgumentNullException.ThrowIfNull(payload);
        lock (sync)
        {
            ThrowIfDisposed();
            if (!keys.TryGetValue(payload.KeyId, out byte[]? key)) throw new CryptographicException("The certificate encryption key is unavailable.");
            byte[] nonce = payload.Nonce, ciphertext = payload.Ciphertext, tag = payload.Tag;
            if (nonce.Length != NonceSize || tag.Length != TagSize || ciphertext.Length > MaximumPlaintextBytes)
                throw new CryptographicException("The protected certificate envelope is malformed.");
            byte[] plaintext = new byte[ciphertext.Length], aad = ComposeAad(payload.KeyId, associatedData);
            try { using var aes = new AesGcm(key, TagSize); aes.Decrypt(nonce, ciphertext, tag, plaintext, aad); return plaintext; }
            catch (CryptographicException) { CryptographicOperations.ZeroMemory(plaintext); throw new CryptographicException("The protected certificate could not be authenticated."); }
        }
    }

    public void Dispose() { lock (sync) { if (disposed) return; ClearKeys(); disposed = true; } }
    private void ClearKeys() { foreach (byte[] key in keys.Values) CryptographicOperations.ZeroMemory(key); keys.Clear(); }
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
    private static byte[] ComposeAad(string keyId, ReadOnlySpan<byte> callerAad)
    {
        byte[] id = Encoding.UTF8.GetBytes(keyId), result = new byte[Domain.Length + 4 + 1 + 4 + id.Length + 4 + callerAad.Length];
        int offset = 0; Domain.CopyTo(result, offset); offset += Domain.Length;
        result[offset++] = 1;
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(offset), id.Length); offset += 4; id.CopyTo(result, offset); offset += id.Length;
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(offset), callerAad.Length); offset += 4; callerAad.CopyTo(result.AsSpan(offset));
        return result;
    }
}

internal static class ArcaCertificateValidation
{
    internal static void ValidateKeyId(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId) || keyId.Length > 128 || keyId.Any(char.IsControl))
            throw new ArgumentException("Key ID must contain 1 to 128 characters without controls.", nameof(keyId));
    }
}
