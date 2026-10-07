using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NetArcaWs.HealthChecks;
using NetArcaWs.Services;

namespace NetArcaWs.EntityFrameworkCore.Migrations.MySql;

public sealed class MySqlInvoicingMigrationsDbContext(DbContextOptions<MySqlInvoicingMigrationsDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.ReplaceService<IModelCacheKeyFactory, NetArcaWsModelCacheKeyFactory>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddNetArcaWs(NetArcaWsModelOptions.Configure(x => x.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)));
}

public sealed class MySqlWsaaTicketsMigrationsDbContext(DbContextOptions<MySqlWsaaTicketsMigrationsDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.ReplaceService<IModelCacheKeyFactory, NetArcaWsModelCacheKeyFactory>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddNetArcaWs(NetArcaWsModelOptions.Configure(x => x.AddWsaaTickets(
        ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca, ArcaService.PadronA4, ArcaService.PadronA5,
        ArcaService.PadronA10, ArcaService.PadronA13, ArcaService.Wscdc, ArcaService.Wsfecred, ArcaService.Wscpe)));
}

public sealed class MySqlTenantCertificatesMigrationsDbContext(DbContextOptions<MySqlTenantCertificatesMigrationsDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.ReplaceService<IModelCacheKeyFactory, NetArcaWsModelCacheKeyFactory>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddNetArcaWs(NetArcaWsModelOptions.Configure(x => x.AddCertificates()));
}

public sealed class MySqlInvoiceRecoveryMigrationsDbContext(DbContextOptions<MySqlInvoiceRecoveryMigrationsDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.ReplaceService<IModelCacheKeyFactory, NetArcaWsModelCacheKeyFactory>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddNetArcaWs(NetArcaWsModelOptions.Configure(x => x.AddInvoiceRecovery(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)));
}
