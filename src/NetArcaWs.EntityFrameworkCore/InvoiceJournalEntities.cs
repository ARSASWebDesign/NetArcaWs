using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Immutable;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Services;

namespace NetArcaWs.EntityFrameworkCore;

/// <summary>Persists the current fiscal operation. Identifier hashes preserve ordinal semantics on every relational collation.</summary>
internal sealed class InvoiceJournalEntity
{
    public string TenantHash { get; set; } = "";
    public string KeyHash { get; set; } = "";
    public string TenantId { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public string Service { get; set; } = "";
    public string ServiceHash { get; set; } = "";
    public int Environment { get; set; }
    public long Cuit { get; set; }
    public int PointOfSale { get; set; }
    public int VoucherType { get; set; }
    public long VoucherNumber { get; set; }
    public string FiscalHash { get; set; } = "";
    public string? RemoteHash { get; set; }
    public long? RemoteRequestId { get; set; }
    public string Payload { get; set; } = "";
    public string PayloadHash { get; set; } = "";
    public int CanonicalVersion { get; set; }
    public int State { get; set; }
    public long Version { get; set; }
    public int Attempt { get; set; }
    public long? LeaseUntilMilliseconds { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? ResponseXml { get; set; }
    public long CreatedUtcTicks { get; set; }
}

/// <summary>Immutable copy of a rejected version retained when an explicit correction is prepared.</summary>
internal sealed class InvoiceRevisionEntity
{
    public string TenantHash { get; set; } = "";
    public string KeyHash { get; set; } = "";
    public int RevisionNumber { get; set; }
    public string TenantId { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public string Service { get; set; } = "";
    public int Environment { get; set; }
    public long Cuit { get; set; }
    public int PointOfSale { get; set; }
    public int VoucherType { get; set; }
    public long VoucherNumber { get; set; }
    public long? RemoteRequestId { get; set; }
    public string Payload { get; set; } = "";
    public string PayloadHash { get; set; } = "";
    public string SnapshotHash { get; set; } = "";
    public int CanonicalVersion { get; set; }
    public int State { get; set; }
    public long Version { get; set; }
    public int Attempt { get; set; }
    public long? LeaseUntilMilliseconds { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? ResponseXml { get; set; }
}

/// <summary>Database-enforced lock for an unresolved fiscal series.</summary>
internal sealed class InvoiceSeriesReservationEntity
{
    public string SeriesHash { get; set; } = "";
    public string TenantHash { get; set; } = "";
    public string KeyHash { get; set; } = "";
}

/// <summary>Immutable startup selection of NetArcaWs model capabilities and supported ARCA services.</summary>
public sealed class NetArcaWsModelOptions
{
    private static readonly ArcaService[] SupportedInvoiceServices = [ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca];
    private readonly ImmutableHashSet<ArcaService> invoiceServices;

    private NetArcaWsModelOptions(IEnumerable<ArcaService> invoiceServices)
    {
        this.invoiceServices = invoiceServices.ToImmutableHashSet();
        Fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            "invoicing:" + string.Join("\n", this.invoiceServices.Order().Select(x => x.ToString())))));
    }

    public bool InvoicingEnabled => invoiceServices.Count != 0;
    public IReadOnlySet<ArcaService> InvoicingServices => invoiceServices;
    public string Fingerprint { get; }

    public static NetArcaWsModelOptions Configure(Action<NetArcaWsModelOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new NetArcaWsModelOptionsBuilder();
        configure(builder);
        return new NetArcaWsModelOptions(builder.InvoiceServices);
    }

    internal bool SupportsInvoiceService(string service) => invoiceServices.Any(selected =>
        string.Equals(GetTicketServiceName(selected), service, StringComparison.Ordinal));
    internal static IReadOnlyList<ArcaService> SupportedServices => SupportedInvoiceServices;
    internal static string GetTicketServiceName(ArcaService service) => service switch
    {
        ArcaService.Wsfev1 => Wsfev1Service.TicketService,
        ArcaService.Wsfexv1 => Wsfexv1Service.TicketService,
        ArcaService.Wsmtxca => Wsmtxcav1Service.TicketService,
        _ => throw new ArgumentOutOfRangeException(nameof(service), service, "The ARCA service is not supported by durable invoicing.")
    };
}

/// <summary>Builder for explicit NetArcaWs capability and ARCA service selection.</summary>
public sealed class NetArcaWsModelOptionsBuilder
{
    private readonly HashSet<ArcaService> invoiceServices = [];
    internal IEnumerable<ArcaService> InvoiceServices => invoiceServices;

    /// <summary>Enables the shared invoice journal tables for the selected ARCA services.</summary>
    public NetArcaWsModelOptionsBuilder AddInvoicing(params ArcaService[] services)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.Length == 0) throw new ArgumentException("Select at least one ARCA service for invoicing.", nameof(services));
        foreach (ArcaService service in services)
        {
            if (!Enum.IsDefined(service) || !NetArcaWsModelOptions.SupportedServices.Contains(service))
                throw new ArgumentException($"Unsupported invoicing service '{service}'. Supported services: {string.Join(", ", NetArcaWsModelOptions.SupportedServices)}.", nameof(services));
            invoiceServices.Add(service);
        }
        return this;
    }
}

/// <summary>Implemented by contexts whose immutable NetArcaWs model selection can vary within one EF service provider.</summary>
public interface INetArcaWsModelOptionsProvider
{
    NetArcaWsModelOptions NetArcaWsModelOptions { get; }
}

/// <summary>Includes immutable NetArcaWs capability selection in EF's model cache key.</summary>
public sealed class NetArcaWsModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        (context.GetType(), (context as INetArcaWsModelOptionsProvider)?.NetArcaWsModelOptions.Fingerprint, designTime);
}

public static class NetArcaWsModelBuilderExtensions
{
    public const string OptionsAnnotationName = "NetArcaWs:OptionsFingerprint";

    /// <summary>Adds only the explicitly selected NetArcaWs capabilities to a consumer-owned EF model.</summary>
    /// <remarks>
    /// Keep the selection fixed for a given consumer DbContext type and EF service provider. If one context type
    /// must use multiple selections in the same provider, implement <see cref="INetArcaWsModelOptionsProvider"/>
    /// and register <see cref="NetArcaWsModelCacheKeyFactory"/> with EF's ReplaceService option.
    /// </remarks>
    public static ModelBuilder AddNetArcaWs(this ModelBuilder modelBuilder, NetArcaWsModelOptions options)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(options);
        modelBuilder.HasAnnotation(OptionsAnnotationName, options.Fingerprint);
        if (!options.InvoicingEnabled) return modelBuilder;
        modelBuilder.Entity<InvoiceJournalEntity>(entity =>
        {
            entity.ToTable("NetArcaInvoices");
            entity.HasKey(x => new { x.TenantHash, x.KeyHash });
            entity.Property(x => x.TenantHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.KeyHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Service).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ServiceHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.FiscalHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RemoteHash).HasMaxLength(64);
            entity.Property(x => x.PayloadHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.HasIndex(x => x.FiscalHash).IsUnique().HasDatabaseName("UX_NetArcaInvoices_Fiscal");
            entity.HasIndex(x => x.RemoteHash).IsUnique().HasDatabaseName("UX_NetArcaInvoices_Remote");
            entity.HasIndex(x => new { x.TenantHash, x.CreatedUtcTicks });
        });
        modelBuilder.Entity<InvoiceRevisionEntity>(entity =>
        {
            entity.ToTable("NetArcaInvoiceRevisions");
            entity.HasKey(x => new { x.TenantHash, x.KeyHash, x.RevisionNumber });
            entity.Property(x => x.TenantHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.KeyHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Service).HasMaxLength(128).IsRequired();
            entity.Property(x => x.PayloadHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SnapshotHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
        });
        modelBuilder.Entity<InvoiceSeriesReservationEntity>(entity =>
        {
            entity.ToTable("NetArcaInvoiceSeriesReservations");
            entity.HasKey(x => x.SeriesHash);
            entity.Property(x => x.SeriesHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.TenantHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.KeyHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.TenantHash, x.KeyHash }).IsUnique();
        });
        return modelBuilder;
    }
}

/// <summary>Dedicated context for applications that want NetArcaWs to own its database model.</summary>
public sealed class ArcaWsDbContext(DbContextOptions<ArcaWsDbContext> options, NetArcaWsModelOptions modelOptions)
    : DbContext(options), INetArcaWsModelOptionsProvider
{
    public NetArcaWsModelOptions NetArcaWsModelOptions { get; } = modelOptions ?? throw new ArgumentNullException(nameof(modelOptions));

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, NetArcaWsModelCacheKeyFactory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddNetArcaWs(NetArcaWsModelOptions);
}
