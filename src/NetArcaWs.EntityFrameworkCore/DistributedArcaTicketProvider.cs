using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Transport;
using NetArcaWs.Wsaa;

namespace NetArcaWs.EntityFrameworkCore;

/// <summary>Performs one uncached WSAA login for an already authorized tenant.</summary>
public interface IWsaaTicketLoginClient
{
    Task<WsaaTicket> LoginAsync(string service, ArcaTenantContext tenant, CancellationToken cancellationToken = default);
}

internal sealed class WsaaTicketLoginClient(WsaaService wsaa) : IWsaaTicketLoginClient
{
    public Task<WsaaTicket> LoginAsync(string service, ArcaTenantContext tenant, CancellationToken cancellationToken = default) =>
        wsaa.AuthenticateForTenantWithoutCacheAsync(service, tenant, cancellationToken);
}

/// <summary>Signals that a WSAA login may have reached ARCA but its successful TA was not durably confirmed.</summary>
public sealed class WsaaTicketLoginOutcomeUnknownException : Exception
{
    internal WsaaTicketLoginOutcomeUnknownException()
        : base("The shared WSAA login outcome is unknown. Automatic login retry is blocked; an operator must resolve the persisted ticket state.") { }
}

/// <summary>Signals that another process may still be completing a shared WSAA login.</summary>
public sealed class WsaaTicketLoginPendingException : Exception
{
    internal WsaaTicketLoginPendingException()
        : base("A shared WSAA login is still pending or its result could not be observed. Automatic login retry is blocked; retry this read later or resolve the persisted state.") { }
}

internal enum WsaaTicketAcquireKind { Owner, Pending, Unknown, StoredTicket }

internal sealed record WsaaTicketClaim(string OwnerId, long Fence);

internal sealed record WsaaTicketAcquisition(
    WsaaTicketAcquireKind Kind,
    WsaaTicketClaim? Claim = null,
    WsaaProtectedPayload? Payload = null,
    DateTimeOffset? ExpiresAt = null,
    long Version = 0,
    long Fence = 0);

internal sealed record WsaaTicketIdentity(string CertificateHash, string Endpoint, string Service)
{
    public string KeyHash { get; } = Convert.ToHexString(SHA256.HashData(Encode(CertificateHash, Endpoint, Service)));

    public byte[] AssociatedData => Encode(CertificateHash, Endpoint, Service);

    public static WsaaTicketIdentity Create(string service, ArcaTenantContext tenant, DateTimeOffset now)
    {
        Uri endpoint = tenant.Environment == ArcaEnvironment.Production
            ? WsaaOptions.ProductionEndpoint : WsaaOptions.HomologationEndpoint;
        using X509Certificate2 certificate = tenant.Certificate.LoadCertificate();
        if (!certificate.HasPrivateKey)
            throw new CryptographicException("A certificate with its private key is required.");
        if (now < certificate.NotBefore.ToUniversalTime() || now >= certificate.NotAfter.ToUniversalTime())
            throw new CryptographicException("The signing certificate is outside its validity period.");
        return new WsaaTicketIdentity(certificate.GetCertHashString(HashAlgorithmName.SHA256), endpoint.AbsoluteUri, service);
    }

    public override string ToString() => "WSAA remote identity (details redacted)";

    private static byte[] Encode(string certificateHash, string endpoint, string service)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        foreach (string value in new[] { certificateHash, endpoint, service })
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
        writer.Flush();
        return stream.ToArray();
    }
}

internal sealed class EfWsaaTicketStore<TContext>(IDbContextFactory<TContext> contextFactory,
    NetArcaWsModelOptions modelOptions, TimeProvider clock, TimeSpan ownerLeaseDuration)
    where TContext : DbContext
{
    public async Task<WsaaTicketAcquisition> AcquireOrReadAsync(WsaaTicketIdentity identity, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 4; attempt++)
        {
            await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
            WsaaTicketEntity? row = await context.Set<WsaaTicketEntity>()
                .SingleOrDefaultAsync(x => x.KeyHash == identity.KeyHash, cancellationToken).ConfigureAwait(false);
            if (row is not null) EnsureIdentityMatches(row, identity);
            DateTimeOffset now = clock.GetUtcNow();
            string ownerId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            if (row is null)
            {
                row = new WsaaTicketEntity
                {
                    KeyHash = identity.KeyHash,
                    CertificateHash = identity.CertificateHash,
                    Endpoint = identity.Endpoint,
                    Service = identity.Service,
                    State = (int)WsaaTicketState.Pending,
                    OwnerId = ownerId,
                    Fence = 1,
                    Version = 1,
                    LeaseUntilUtcTicks = (now + ownerLeaseDuration).UtcTicks,
                    UpdatedUtcTicks = now.UtcTicks
                };
                context.Add(row);
                try
                {
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return new WsaaTicketAcquisition(WsaaTicketAcquireKind.Owner, new WsaaTicketClaim(ownerId, row.Fence));
                }
                catch (Exception ex) when (attempt < 3 && IsDatabaseConflict(ex))
                {
                    continue;
                }
            }
            else if (row.State == (int)WsaaTicketState.Ticket && row.ExpiresUtcTicks is long expiresTicks)
            {
                if (row.KeyId is null || row.Nonce is null || row.Ciphertext is null || row.Tag is null)
                    throw new CryptographicException("The persisted shared WSAA ticket payload is incomplete.");
                var payload = new WsaaProtectedPayload(row.KeyId ?? "", row.Nonce ?? [], row.Ciphertext ?? [], row.Tag ?? []);
                return new WsaaTicketAcquisition(WsaaTicketAcquireKind.StoredTicket, Payload: payload,
                    ExpiresAt: new DateTimeOffset(expiresTicks, TimeSpan.Zero), Version: row.Version, Fence: row.Fence);
            }

            WsaaTicketAcquireKind kind = row.State switch
            {
                (int)WsaaTicketState.Pending => WsaaTicketAcquireKind.Pending,
                (int)WsaaTicketState.Unknown => WsaaTicketAcquireKind.Unknown,
                _ => throw new InvalidDataException("Persisted WSAA ticket state is invalid.")
            };
            return new WsaaTicketAcquisition(kind);
        }

        throw new InvalidOperationException("Could not reserve the shared WSAA login after concurrent database insert conflicts.");
    }

    public async Task<WsaaTicketAcquisition?> TryClaimExpiredAsync(WsaaTicketIdentity identity, long expectedVersion,
        long expectedFence, DateTimeOffset authenticatedExpiration, CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.GetUtcNow();
        string ownerId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        long nextFence = checked(expectedFence + 1);
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        int changed = await context.Set<WsaaTicketEntity>()
            .Where(x => x.KeyHash == identity.KeyHash && x.State == (int)WsaaTicketState.Ticket &&
                x.Version == expectedVersion && x.Fence == expectedFence &&
                x.ExpiresUtcTicks == authenticatedExpiration.UtcTicks && x.ExpiresUtcTicks <= now.UtcTicks)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.State, (int)WsaaTicketState.Pending)
                .SetProperty(x => x.OwnerId, ownerId)
                .SetProperty(x => x.Fence, nextFence)
                .SetProperty(x => x.Version, x => x.Version + 1)
                .SetProperty(x => x.LeaseUntilUtcTicks, (long?)(now + ownerLeaseDuration).UtcTicks)
                .SetProperty(x => x.ExpiresUtcTicks, (long?)null)
                .SetProperty(x => x.KeyId, (string?)null)
                .SetProperty(x => x.Nonce, (byte[]?)null)
                .SetProperty(x => x.Ciphertext, (byte[]?)null)
                .SetProperty(x => x.Tag, (byte[]?)null)
                .SetProperty(x => x.UpdatedUtcTicks, now.UtcTicks), cancellationToken).ConfigureAwait(false);
        return changed == 1
            ? new WsaaTicketAcquisition(WsaaTicketAcquireKind.Owner, new WsaaTicketClaim(ownerId, nextFence))
            : null;
    }

    private static bool IsDatabaseConflict(Exception exception) => exception switch
    {
        DbUpdateException => true,
        DbException => true,
        _ => false
    };

    public async Task<bool> CompleteAsync(WsaaTicketIdentity identity, WsaaTicketClaim claim,
        WsaaProtectedPayload payload, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        int changed = await context.Set<WsaaTicketEntity>()
            .Where(x => x.KeyHash == identity.KeyHash && x.State == (int)WsaaTicketState.Pending &&
                x.OwnerId == claim.OwnerId && x.Fence == claim.Fence)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.State, (int)WsaaTicketState.Ticket)
                .SetProperty(x => x.OwnerId, (string?)null)
                .SetProperty(x => x.LeaseUntilUtcTicks, (long?)null)
                .SetProperty(x => x.ExpiresUtcTicks, expiresAt.UtcTicks)
                .SetProperty(x => x.KeyId, payload.KeyId)
                .SetProperty(x => x.Nonce, payload.Nonce)
                .SetProperty(x => x.Ciphertext, payload.Ciphertext)
                .SetProperty(x => x.Tag, payload.Tag)
                .SetProperty(x => x.Version, x => x.Version + 1)
                .SetProperty(x => x.UpdatedUtcTicks, clock.GetUtcNow().UtcTicks), cancellationToken).ConfigureAwait(false);
        return changed == 1;
    }

    public async Task MarkUnknownAsync(WsaaTicketIdentity identity, WsaaTicketClaim claim, CancellationToken cancellationToken)
    {
        await using TContext context = await CreateContextAsync(cancellationToken).ConfigureAwait(false);
        _ = await context.Set<WsaaTicketEntity>()
            .Where(x => x.KeyHash == identity.KeyHash && x.State == (int)WsaaTicketState.Pending &&
                x.OwnerId == claim.OwnerId && x.Fence == claim.Fence)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.State, (int)WsaaTicketState.Unknown)
                .SetProperty(x => x.OwnerId, (string?)null)
                .SetProperty(x => x.LeaseUntilUtcTicks, (long?)null)
                .SetProperty(x => x.Version, x => x.Version + 1)
                .SetProperty(x => x.UpdatedUtcTicks, clock.GetUtcNow().UtcTicks), cancellationToken).ConfigureAwait(false);
    }

    private async Task<TContext> CreateContextAsync(CancellationToken cancellationToken)
    {
        TContext context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        string? configuredFingerprint = context.Model.FindAnnotation(NetArcaWsModelBuilderExtensions.OptionsAnnotationName)?.Value as string;
        if (context is INetArcaWsModelOptionsProvider provider && provider.NetArcaWsModelOptions.Fingerprint != modelOptions.Fingerprint ||
            configuredFingerprint != modelOptions.Fingerprint || !modelOptions.WsaaTicketsEnabled ||
            context.Model.FindEntityType(typeof(WsaaTicketEntity)) is null)
        {
            await context.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException("The EF context model does not match the enabled shared WSAA ticket selection.");
        }
        return context;
    }

    private static void EnsureIdentityMatches(WsaaTicketEntity row, WsaaTicketIdentity identity)
    {
        if (!string.Equals(row.CertificateHash, identity.CertificateHash, StringComparison.Ordinal) ||
            !string.Equals(row.Endpoint, identity.Endpoint, StringComparison.Ordinal) ||
            !string.Equals(row.Service, identity.Service, StringComparison.Ordinal))
            throw new CryptographicException("Shared WSAA ticket identity hash collision or row substitution detected.");
    }
}

/// <summary>Coordinates and reuses encrypted shared WSAA tickets across application processes.</summary>
public sealed class DistributedArcaTicketProvider<TContext> : IArcaTicketProvider where TContext : DbContext
{
    private readonly EfWsaaTicketStore<TContext> store;
    private readonly IWsaaTicketProtector protector;
    private readonly IWsaaTicketLoginClient loginClient;
    private readonly NetArcaWsModelOptions modelOptions;
    private readonly TimeProvider clock;
    private readonly TimeSpan waitTimeout;
    private readonly TimeSpan pollingInterval;

    internal DistributedArcaTicketProvider(EfWsaaTicketStore<TContext> store, IWsaaTicketProtector protector,
        IWsaaTicketLoginClient loginClient, NetArcaWsModelOptions modelOptions, TimeProvider clock,
        TimeSpan waitTimeout, TimeSpan pollingInterval)
    {
        this.store = store;
        this.protector = protector;
        this.loginClient = loginClient;
        this.modelOptions = modelOptions;
        this.clock = clock;
        this.waitTimeout = waitTimeout;
        this.pollingInterval = pollingInterval;
    }

    public async Task<WsaaTicket> GetTicketAsync(string service, ArcaTenantContext tenant, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(service);
        ArgumentNullException.ThrowIfNull(tenant);
        cancellationToken.ThrowIfCancellationRequested();
        if (!modelOptions.SupportsWsaaTicketService(service))
            throw new InvalidOperationException("Shared WSAA tickets are not enabled for this ARCA service.");
        WsaaTicketIdentity identity = WsaaTicketIdentity.Create(service, tenant, clock.GetUtcNow());
        Stopwatch waitTimer = Stopwatch.StartNew();

        while (true)
        {
            WsaaTicketAcquisition acquisition = await store.AcquireOrReadAsync(identity, cancellationToken).ConfigureAwait(false);
            switch (acquisition.Kind)
            {
                case WsaaTicketAcquireKind.StoredTicket:
                    WsaaTicket ticket = RestoreTicket(identity, acquisition);
                    if (!ticket.IsExpired(clock.GetUtcNow())) return ticket;
                    WsaaTicketAcquisition? expiredClaim = await store.TryClaimExpiredAsync(identity, acquisition.Version,
                        acquisition.Fence, ticket.ExpirationTime, cancellationToken).ConfigureAwait(false);
                    if (expiredClaim is not null)
                        return await LoginAndPersistAsync(identity, expiredClaim.Claim!, service, tenant, cancellationToken).ConfigureAwait(false);
                    continue;
                case WsaaTicketAcquireKind.Unknown:
                    throw new WsaaTicketLoginOutcomeUnknownException();
                case WsaaTicketAcquireKind.Pending:
                    TimeSpan remaining = waitTimeout - waitTimer.Elapsed;
                    if (remaining <= TimeSpan.Zero) throw new WsaaTicketLoginPendingException();
                    TimeSpan delay = remaining < pollingInterval ? remaining : pollingInterval;
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    remaining -= delay;
                    continue;
                case WsaaTicketAcquireKind.Owner:
                    return await LoginAndPersistAsync(identity, acquisition.Claim!, service, tenant, cancellationToken).ConfigureAwait(false);
                default:
                    throw new InvalidDataException("Shared WSAA ticket acquisition result is invalid.");
            }
        }
    }

    private WsaaTicket RestoreTicket(WsaaTicketIdentity identity, WsaaTicketAcquisition acquisition)
    {
        byte[] xmlBytes = protector.Unprotect(acquisition.Payload!, identity.AssociatedData);
        try
        {
            string xml = new UTF8Encoding(false, true).GetString(xmlBytes);
            WsaaTicket ticket = WsaaService.ParseTicket(xml);
            DateTimeOffset now = clock.GetUtcNow();
            if (ticket.ExpirationTime != acquisition.ExpiresAt)
                throw new CryptographicException("The shared WSAA ticket payload does not match its persisted expiration.");
            return ticket;
        }
        finally { CryptographicOperations.ZeroMemory(xmlBytes); }
    }

    private async Task<WsaaTicket> LoginAndPersistAsync(WsaaTicketIdentity identity, WsaaTicketClaim claim,
        string service, ArcaTenantContext tenant, CancellationToken cancellationToken)
    {
        WsaaTicket ticket;
        WsaaProtectedPayload protectedPayload;
        try
        {
            WsaaTicket received = await loginClient.LoginAsync(service, tenant, cancellationToken).ConfigureAwait(false);
            WsaaTicket parsed = WsaaService.ParseTicket(received.Xml);
            if (received.Token != parsed.Token || received.Sign != parsed.Sign || received.GenerationTime != parsed.GenerationTime ||
                received.ExpirationTime != parsed.ExpirationTime || received.Source != parsed.Source || received.Destination != parsed.Destination ||
                received.UniqueId != parsed.UniqueId || parsed.IsExpired(clock.GetUtcNow()))
                throw new FormatException("WSAA ticket fields were inconsistent or expired.");
            ticket = parsed;
            byte[] xml = Encoding.UTF8.GetBytes(ticket.Xml);
            try { protectedPayload = protector.Protect(xml, identity.AssociatedData); }
            finally { CryptographicOperations.ZeroMemory(xml); }
        }
        catch (Exception)
        {
            try { await store.MarkUnknownAsync(identity, claim, CancellationToken.None).ConfigureAwait(false); }
            catch (Exception) { /* The durable Pending marker still prevents another remote login. */ }
            throw new WsaaTicketLoginOutcomeUnknownException();
        }

        try
        {
            if (await store.CompleteAsync(identity, claim, protectedPayload, ticket.ExpirationTime, cancellationToken).ConfigureAwait(false))
                return ticket;
        }
        catch (Exception)
        {
            try
            {
                WsaaTicketAcquisition current = await store.AcquireOrReadAsync(identity, CancellationToken.None).ConfigureAwait(false);
                if (current.Kind == WsaaTicketAcquireKind.StoredTicket)
                {
                    WsaaTicket persisted = RestoreTicket(identity, current);
                    if (!persisted.IsExpired(clock.GetUtcNow())) return persisted;
                }
            }
            catch (Exception) { }
        }
        try { await store.MarkUnknownAsync(identity, claim, CancellationToken.None).ConfigureAwait(false); }
        catch (Exception) { }
        throw new WsaaTicketLoginOutcomeUnknownException();
    }
}

public static class WsaaTicketServiceCollectionExtensions
{
    /// <summary>Registers shared WSAA ticket persistence and selects it as the ticket provider.</summary>
    public static IServiceCollection AddWsaaTicketStores<TContext>(this IServiceCollection services,
        NetArcaWsModelOptions modelOptions, TimeSpan? waitTimeout = null, TimeSpan? pollingInterval = null,
        TimeSpan? ownerLeaseDuration = null) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(modelOptions);
        if (!modelOptions.WsaaTicketsEnabled)
            throw new ArgumentException("Select at least one ARCA service with AddWsaaTickets before registering shared WSAA tickets.", nameof(modelOptions));
        if (!services.Any(x => x.ServiceType == typeof(IWsaaTicketProtector)))
            throw new InvalidOperationException("Register an IWsaaTicketProtector with externally managed key material before enabling shared WSAA tickets.");
        TimeSpan wait = waitTimeout ?? TimeSpan.FromSeconds(30);
        TimeSpan poll = pollingInterval ?? TimeSpan.FromMilliseconds(150);
        TimeSpan lease = ownerLeaseDuration ?? TimeSpan.FromMinutes(5);
        if (wait <= TimeSpan.Zero || wait > TimeSpan.FromMinutes(10)) throw new ArgumentOutOfRangeException(nameof(waitTimeout));
        if (poll <= TimeSpan.Zero || poll > TimeSpan.FromSeconds(5)) throw new ArgumentOutOfRangeException(nameof(pollingInterval));
        if (lease <= TimeSpan.Zero || lease > TimeSpan.FromHours(1)) throw new ArgumentOutOfRangeException(nameof(ownerLeaseDuration));
        services.TryAddSingleton(modelOptions);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IWsaaTicketLoginClient, WsaaTicketLoginClient>();
        services.AddSingleton(provider => new EfWsaaTicketStore<TContext>(
            provider.GetRequiredService<IDbContextFactory<TContext>>(), modelOptions,
            provider.GetRequiredService<TimeProvider>(), lease));
        services.Replace(ServiceDescriptor.Singleton<IArcaTicketProvider>(provider => new DistributedArcaTicketProvider<TContext>(
            provider.GetRequiredService<EfWsaaTicketStore<TContext>>(), provider.GetRequiredService<IWsaaTicketProtector>(),
            provider.GetRequiredService<IWsaaTicketLoginClient>(), modelOptions, provider.GetRequiredService<TimeProvider>(), wait, poll)));
        return services;
    }
}
