using System.Security.Cryptography;

namespace NetArcaWs.EntityFrameworkCore;

/// <summary>Encrypted ticket bytes persisted by the optional shared WSAA ticket store.</summary>
public sealed class WsaaProtectedPayload
{
    private readonly byte[] nonce;
    private readonly byte[] ciphertext;
    private readonly byte[] tag;

    public WsaaProtectedPayload(string keyId, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag)
    {
        if (string.IsNullOrWhiteSpace(keyId) || keyId.Length > 128 || keyId.Any(char.IsControl))
            throw new ArgumentException("Key ID must contain 1 to 128 characters without controls.", nameof(keyId));
        KeyId = keyId;
        this.nonce = nonce.ToArray();
        this.ciphertext = ciphertext.ToArray();
        this.tag = tag.ToArray();
    }

    public string KeyId { get; }
    public byte[] Nonce => nonce.ToArray();
    public byte[] Ciphertext => ciphertext.ToArray();
    public byte[] Tag => tag.ToArray();

    public override string ToString() => $"Protected WSAA payload (key {KeyId}, ciphertext redacted)";
}

/// <summary>Protects and authenticates serialized WSAA ticket content with identity-bound associated data.</summary>
public interface IWsaaTicketProtector
{
    WsaaProtectedPayload Protect(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData);
    byte[] Unprotect(WsaaProtectedPayload payload, ReadOnlySpan<byte> associatedData);
}

/// <summary>
/// AES-GCM protector for shared WSAA ticket material. Supply key material and a key ID from the application;
/// this class does not persist, derive, distribute, or retrieve keys.
/// </summary>
public sealed class AesGcmWsaaTicketProtector : IWsaaTicketProtector, IDisposable
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly Dictionary<string, byte[]> keys;
    private readonly string activeKeyId;
    private readonly object sync = new();
    private bool disposed;

    public AesGcmWsaaTicketProtector(string activeKeyId, IReadOnlyDictionary<string, byte[]> keyRing)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activeKeyId);
        ArgumentNullException.ThrowIfNull(keyRing);
        if (!keyRing.TryGetValue(activeKeyId, out _))
            throw new ArgumentException("The active key ID must exist in the supplied key ring.", nameof(activeKeyId));
        foreach ((string keyId, byte[] key) in keyRing)
        {
            if (string.IsNullOrWhiteSpace(keyId) || keyId.Length > 128 || keyId.Any(char.IsControl))
                throw new ArgumentException("Key IDs must contain 1 to 128 characters without controls.", nameof(keyRing));
            ArgumentNullException.ThrowIfNull(key);
            if (key.Length != 32) throw new ArgumentException("AES-GCM key material must contain exactly 32 bytes.", nameof(keyRing));
        }
        keys = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach ((string keyId, byte[] key) in keyRing)
            if (!keys.TryAdd(keyId, key.ToArray())) throw new ArgumentException("Key IDs must be unique.", nameof(keyRing));
        this.activeKeyId = activeKeyId;
    }

    public WsaaProtectedPayload Protect(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData)
    {
        lock (sync)
        {
            ThrowIfDisposed();
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];
            using var aes = new AesGcm(keys[activeKeyId], TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
            return new WsaaProtectedPayload(activeKeyId, nonce, ciphertext, tag);
        }
    }

    public byte[] Unprotect(WsaaProtectedPayload payload, ReadOnlySpan<byte> associatedData)
    {
        lock (sync)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(payload);
            if (!keys.TryGetValue(payload.KeyId, out byte[]? key))
                throw new CryptographicException("The WSAA ticket encryption key is unavailable.");
            byte[] nonce = payload.Nonce;
            byte[] ciphertext = payload.Ciphertext;
            byte[] tag = payload.Tag;
            if (nonce.Length != NonceSize || tag.Length != TagSize)
                throw new CryptographicException("The protected WSAA ticket payload is malformed.");
            byte[] plaintext = new byte[ciphertext.Length];
            try
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
                return plaintext;
            }
            catch
            {
                CryptographicOperations.ZeroMemory(plaintext);
                throw;
            }
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed) return;
            foreach (byte[] key in keys.Values) CryptographicOperations.ZeroMemory(key);
            keys.Clear();
            disposed = true;
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
}
