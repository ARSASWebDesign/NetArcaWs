using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NetArcaWs.HealthChecks;
using NetArcaWs.Services;

namespace NetArcaWs.EntityFrameworkCore.Migrations.SqlServer;

public sealed class SqlServerInvoicingMigrationsDbContext(DbContextOptions<SqlServerInvoicingMigrationsDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.ReplaceService<IModelCacheKeyFactory, NetArcaWsModelCacheKeyFactory>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.AddNetArcaWs(NetArcaWsModelOptions.Configure(x => x.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)));
    }
}

public sealed class SqlServerWsaaTicketsMigrationsDbContext(DbContextOptions<SqlServerWsaaTicketsMigrationsDbContext> options) : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.ReplaceService<IModelCacheKeyFactory, NetArcaWsModelCacheKeyFactory>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");
        modelBuilder.AddNetArcaWs(NetArcaWsModelOptions.Configure(x => x.AddWsaaTickets(
            ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca, ArcaService.PadronA4, ArcaService.PadronA5,
            ArcaService.PadronA10, ArcaService.PadronA13, ArcaService.Wscdc, ArcaService.Wsfecred, ArcaService.Wscpe)));
    }
}
