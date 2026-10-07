using System.Buffers.Binary;
using System.Data;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NetArcaWs.Cryptography;

namespace NetArcaWs.EntityFrameworkCore;

/// <summary>Persists immutable encrypted certificate versions and atomically advances each scope's active head.</summary>
public sealed class EfArcaCertificateStore<TContext> : IArcaCertificateStore where TContext : DbContext
{
    private const string Domain = "NetArcaWs/tenant-certificate-store/v1";
    private readonly IDbContextFactory<TContext> factory;
    private readonly NetArcaWsModelOptions options;
    private readonly IArcaCertificateProtector protector;
    private readonly TimeProvider clock;

    public EfArcaCertificateStore(IDbContextFactory<TContext> factory, NetArcaWsModelOptions options,
        IArcaCertificateProtector protector, TimeProvider? timeProvider = null)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.protector = protector ?? throw new ArgumentNullException(nameof(protector));
        if (!options.CertificatesEnabled) throw new ArgumentException("Select AddCertificates before registering the certificate store.", nameof(options));
        clock = timeProvider ?? TimeProvider.System;
    }

    public async Task<ArcaCertificateVersion> RotateAsync(ArcaCertificateScope scope, WsaaCertificateContent content,
        Guid? expectedActiveVersionId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope); ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();
        Normalized normalized = Normalize(content);
        Guid versionId = Guid.NewGuid(); DateTimeOffset created = clock.GetUtcNow().ToUniversalTime();
        var provisional = new ArcaCertificateVersion(versionId, created, normalized.Fingerprint, normalized.NotBefore, normalized.NotAfter, true);
        byte[] aad = CreateAad(scope, provisional);
        ArcaProtectedCertificate protectedValue;
        try { protectedValue = protector.Protect(normalized.Envelope, aad); }
        catch (ArgumentException) { throw; }
        catch (CryptographicException) { throw new ArcaCertificateDataException(); }
        finally { CryptographicOperations.ZeroMemory(normalized.Envelope); }
        string tenantHash = HashTenant(scope.TenantId);
        var version = new ArcaCertificateVersionEntity
        {
            TenantHash = tenantHash, Cuit = scope.Cuit, Environment = (int)scope.Environment, VersionId = versionId,
            CreatedAtUtcTicks = created.UtcTicks, ThumbprintSha256 = normalized.Fingerprint,
            NotBeforeUtcTicks = normalized.NotBefore.UtcTicks, NotAfterUtcTicks = normalized.NotAfter.UtcTicks,
            KeyId = protectedValue.KeyId, Nonce = protectedValue.Nonce, Ciphertext = protectedValue.Ciphertext, Tag = protectedValue.Tag
        };

        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        await using IDbContextTransaction tx = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        context.Add(version);
        ArcaCertificateSlotEntity? slot = await context.Set<ArcaCertificateSlotEntity>().SingleOrDefaultAsync(x =>
            x.TenantHash == tenantHash && x.Cuit == scope.Cuit && x.Environment == (int)scope.Environment, cancellationToken).ConfigureAwait(false);
        if (slot is null)
        {
            if (expectedActiveVersionId is not null) throw new ArcaCertificateConcurrencyException("The active certificate version changed.");
            context.Add(new ArcaCertificateSlotEntity { TenantHash = tenantHash, Cuit = scope.Cuit, Environment = (int)scope.Environment, ActiveVersionId = versionId, Generation = 1 });
        }
        else
        {
            if (expectedActiveVersionId is null || expectedActiveVersionId != slot.ActiveVersionId)
                throw new ArcaCertificateConcurrencyException("The active certificate version changed.");
            long generation = slot.Generation;
            int changed = await context.Set<ArcaCertificateSlotEntity>().Where(x => x.TenantHash == tenantHash && x.Cuit == scope.Cuit &&
                    x.Environment == (int)scope.Environment && x.Generation == generation && x.ActiveVersionId == expectedActiveVersionId)
                .ExecuteUpdateAsync(q => q.SetProperty(x => x.ActiveVersionId, versionId).SetProperty(x => x.Generation, x => x.Generation + 1), cancellationToken).ConfigureAwait(false);
            if (changed != 1) throw new ArcaCertificateConcurrencyException("The active certificate version changed.");
        }
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintFailure(exception))
        {
            await tx.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            await using TContext check = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            bool slotExists = await check.Set<ArcaCertificateSlotEntity>().AsNoTracking().AnyAsync(x => x.TenantHash == tenantHash &&
                x.Cuit == scope.Cuit && x.Environment == (int)scope.Environment, cancellationToken).ConfigureAwait(false);
            if (slotExists) throw new ArcaCertificateConcurrencyException("The active certificate version changed.");
            throw;
        }
        return provisional;
    }

    public async Task<ArcaStoredCertificate?> GetActiveAsync(ArcaCertificateScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope); string tenantHash = HashTenant(scope.TenantId);
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        ArcaCertificateSlotEntity? slot = await context.Set<ArcaCertificateSlotEntity>().AsNoTracking().SingleOrDefaultAsync(x =>
            x.TenantHash == tenantHash && x.Cuit == scope.Cuit && x.Environment == (int)scope.Environment, cancellationToken).ConfigureAwait(false);
        if (slot is null) return null;
        ArcaCertificateVersionEntity? entity = await context.Set<ArcaCertificateVersionEntity>().AsNoTracking().SingleOrDefaultAsync(x =>
            x.TenantHash == tenantHash && x.Cuit == scope.Cuit && x.Environment == (int)scope.Environment && x.VersionId == slot.ActiveVersionId, cancellationToken).ConfigureAwait(false);
        return entity is null ? throw new ArcaCertificateDataException() : await RestoreAsync(scope, entity, true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ArcaStoredCertificate?> GetVersionAsync(ArcaCertificateScope scope, Guid versionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        string tenantHash = HashTenant(scope.TenantId);
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        ArcaCertificateVersionEntity? entity = await context.Set<ArcaCertificateVersionEntity>().AsNoTracking().SingleOrDefaultAsync(x =>
            x.TenantHash == tenantHash && x.Cuit == scope.Cuit && x.Environment == (int)scope.Environment && x.VersionId == versionId, cancellationToken).ConfigureAwait(false);
        if (entity is null) return null;
        bool active = await context.Set<ArcaCertificateSlotEntity>().AnyAsync(x => x.TenantHash == tenantHash && x.Cuit == scope.Cuit &&
            x.Environment == (int)scope.Environment && x.ActiveVersionId == versionId, cancellationToken).ConfigureAwait(false);
        return await RestoreAsync(scope, entity, active, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ArcaCertificateVersion>> ListVersionsAsync(ArcaCertificateScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope); string hash = HashTenant(scope.TenantId);
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        Guid? active = await context.Set<ArcaCertificateSlotEntity>().Where(x => x.TenantHash == hash && x.Cuit == scope.Cuit && x.Environment == (int)scope.Environment)
            .Select(x => (Guid?)x.ActiveVersionId).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        List<ArcaCertificateVersionEntity> rows = await context.Set<ArcaCertificateVersionEntity>().AsNoTracking().Where(x =>
            x.TenantHash == hash && x.Cuit == scope.Cuit && x.Environment == (int)scope.Environment).OrderBy(x => x.CreatedAtUtcTicks).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(x => Metadata(x, x.VersionId == active)).ToArray();
    }

    public async Task<IReadOnlyList<ArcaCertificateVersion>> FindExpiringAsync(ArcaCertificateScope scope, TimeSpan warningWindow,
        DateTimeOffset asOfUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (warningWindow <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(warningWindow));
        if (asOfUtc.Offset != TimeSpan.Zero) throw new ArgumentException("The timestamp must be UTC.", nameof(asOfUtc));
        string hash = HashTenant(scope.TenantId); long threshold = (asOfUtc + warningWindow).UtcTicks;
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        ArcaCertificateVersionEntity? row = await (from slot in context.Set<ArcaCertificateSlotEntity>()
            join item in context.Set<ArcaCertificateVersionEntity>() on new { slot.TenantHash, slot.Cuit, slot.Environment, VersionId = slot.ActiveVersionId }
                equals new { item.TenantHash, item.Cuit, item.Environment, item.VersionId }
            where slot.TenantHash == hash && slot.Cuit == scope.Cuit && slot.Environment == (int)scope.Environment && item.NotAfterUtcTicks <= threshold
            select item).AsNoTracking().SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return row is null ? [] : [Metadata(row, true)];
    }

    private async Task<ArcaStoredCertificate> RestoreAsync(ArcaCertificateScope scope, ArcaCertificateVersionEntity entity, bool active, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArcaCertificateVersion metadata;
        try { metadata = Metadata(entity, active); }
        catch { throw new ArcaCertificateDataException(); }
        DateTimeOffset now = clock.GetUtcNow();
        if (now < metadata.NotBeforeUtc || now >= metadata.NotAfterUtc) throw new ArcaCertificateValidityException();
        byte[]? plaintext = null;
        try
        {
            var envelope = new ArcaProtectedCertificate(entity.KeyId, entity.Nonce, entity.Ciphertext, entity.Tag);
            plaintext = protector.Unprotect(envelope, CreateAad(scope, metadata));
            (string certificatePem, string keyPem) = DecodeEnvelope(plaintext);
            using X509Certificate2 certificate = X509Certificate2.CreateFromPem(certificatePem, keyPem);
            byte[] fingerprint = SHA256.HashData(certificate.RawData);
            if (!CryptographicOperations.FixedTimeEquals(fingerprint, Convert.FromHexString(metadata.ThumbprintSha256)) ||
                certificate.NotBefore.ToUniversalTime().Ticks != metadata.NotBeforeUtc.UtcTicks || certificate.NotAfter.ToUniversalTime().Ticks != metadata.NotAfterUtc.UtcTicks)
                throw new CryptographicException();
            using RSA? rsa = certificate.GetRSAPrivateKey();
            if (rsa is null) throw new CryptographicException();
            return new ArcaStoredCertificate(metadata, WsaaCertificateContent.FromPem(certificatePem, keyPem));
        }
        catch (ArcaCertificateValidityException) { throw; }
        catch { throw new ArcaCertificateDataException(); }
        finally { if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext); }
    }

    private async Task<TContext> CreateContextAsync(CancellationToken token)
    {
        TContext context = await factory.CreateDbContextAsync(token).ConfigureAwait(false);
        if (!context.Model.GetEntityTypes().Any(x => x.ClrType == typeof(ArcaCertificateSlotEntity)) ||
            context.Model.FindAnnotation(NetArcaWsModelBuilderExtensions.OptionsAnnotationName)?.Value as string != options.Fingerprint)
        { await context.DisposeAsync().ConfigureAwait(false); throw new InvalidOperationException("The EF context model selection does not match the certificate store selection."); }
        return context;
    }

    private static ArcaCertificateVersion Metadata(ArcaCertificateVersionEntity x, bool active) => new(x.VersionId,
        new DateTimeOffset(x.CreatedAtUtcTicks, TimeSpan.Zero), x.ThumbprintSha256,
        new DateTimeOffset(x.NotBeforeUtcTicks, TimeSpan.Zero), new DateTimeOffset(x.NotAfterUtcTicks, TimeSpan.Zero), active);

    private static byte[] CreateAad(ArcaCertificateScope scope, ArcaCertificateVersion metadata)
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Domain); writer.Write((byte)1); writer.Write(scope.TenantId); writer.Write(scope.Cuit); writer.Write((int)scope.Environment);
        writer.Write(metadata.VersionId.ToByteArray()); writer.Write(metadata.ThumbprintSha256); writer.Write(metadata.NotBeforeUtc.UtcTicks);
        writer.Write(metadata.NotAfterUtc.UtcTicks); writer.Write(metadata.CreatedAtUtc.UtcTicks); writer.Flush(); return stream.ToArray();
    }

    private static string HashTenant(string tenantId) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tenantId)));

    private static Normalized Normalize(WsaaCertificateContent content)
    {
        byte[]? envelope = null, certBytes = null, keyBytes = null;
        try
        {
            using X509Certificate2 cert = content.LoadCertificate();
            using RSA rsa = cert.GetRSAPrivateKey() ?? throw new CryptographicException();
            keyBytes = rsa.ExportPkcs8PrivateKey(); certBytes = Encoding.UTF8.GetBytes(cert.ExportCertificatePem());
            string keyPem = rsa.ExportPkcs8PrivateKeyPem(); byte[] pemKey = Encoding.UTF8.GetBytes(keyPem);
            try
            {
                if (certBytes.Length + pemKey.Length + 8 > AesGcmArcaCertificateProtector.MaximumPlaintextBytes) throw new EnvelopeLimitException();
                envelope = new byte[8 + certBytes.Length + pemKey.Length];
                BinaryPrimitives.WriteInt32BigEndian(envelope, certBytes.Length); certBytes.CopyTo(envelope, 4);
                BinaryPrimitives.WriteInt32BigEndian(envelope.AsSpan(4 + certBytes.Length), pemKey.Length); pemKey.CopyTo(envelope, 8 + certBytes.Length);
            }
            finally { CryptographicOperations.ZeroMemory(pemKey); }
            DateTimeOffset before = cert.NotBefore.ToUniversalTime(), after = cert.NotAfter.ToUniversalTime();
            if (before >= after) throw new CryptographicException();
            string fingerprint = Convert.ToHexString(SHA256.HashData(cert.RawData));
            byte[] result = envelope; envelope = null; return new Normalized(result, fingerprint, before, after);
        }
        catch (EnvelopeLimitException) { throw new ArgumentException("Certificate envelope exceeds the 1 MiB limit.", "content"); }
        catch (PlatformNotSupportedException) { throw; }
        catch { throw new ArgumentException("The supplied certificate must contain a valid exportable RSA private key and validity period.", "content"); }
        finally
        {
            if (envelope is not null) CryptographicOperations.ZeroMemory(envelope);
            if (certBytes is not null) CryptographicOperations.ZeroMemory(certBytes);
            if (keyBytes is not null) CryptographicOperations.ZeroMemory(keyBytes);
        }
    }

    private static (string Certificate, string Key) DecodeEnvelope(byte[] bytes)
    {
        if (bytes.Length < 8 || bytes.Length > AesGcmArcaCertificateProtector.MaximumPlaintextBytes) throw new CryptographicException();
        int certLength = BinaryPrimitives.ReadInt32BigEndian(bytes), keyOffset;
        if (certLength <= 0 || certLength > bytes.Length - 8) throw new CryptographicException();
        keyOffset = checked(4 + certLength); int keyLength = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(keyOffset));
        if (keyLength <= 0 || keyOffset + 4 + keyLength != bytes.Length) throw new CryptographicException();
        var utf8 = new UTF8Encoding(false, true);
        return (utf8.GetString(bytes, 4, certLength), utf8.GetString(bytes, keyOffset + 4, keyLength));
    }

    private static bool IsUniqueConstraintFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            string type = current.GetType().Name;
            if (type == "SqliteException" && current.GetType().GetProperty("SqliteExtendedErrorCode")?.GetValue(current) is int sqliteCode &&
                sqliteCode is 1555 or 2067) return true;
            if (type == "SqlException" && current.GetType().GetProperty("Number")?.GetValue(current) is int sqlNumber && sqlNumber is 2601 or 2627) return true;
            if (type == "PostgresException" && current.GetType().GetProperty("SqlState")?.GetValue(current) is string sqlState && sqlState == "23505") return true;
            if (type == "MySqlException" && current.GetType().GetProperty("Number")?.GetValue(current) is int mySqlNumber && mySqlNumber is 1062 or 1586) return true;
        }
        return false;
    }

    private sealed record Normalized(byte[] Envelope, string Fingerprint, DateTimeOffset NotBefore, DateTimeOffset NotAfter);
    private sealed class EnvelopeLimitException : Exception { }
}
