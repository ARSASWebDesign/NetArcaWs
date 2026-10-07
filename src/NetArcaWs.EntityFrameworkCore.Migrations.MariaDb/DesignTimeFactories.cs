using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NetArcaWs.EntityFrameworkCore.Migrations.MariaDb;

public sealed class InvoicingDesignTimeFactory : IDesignTimeDbContextFactory<MariaDbInvoicingMigrationsDbContext>
{
    public MariaDbInvoicingMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MariaDbInvoicingMigrationsDbContext>()
        .UseMySql("Server=localhost;Database=netarcaws_design_time", new MariaDbServerVersion(new Version(11, 4, 13)), x => x.MigrationsAssembly(typeof(InvoicingDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceMigrations")).Options);
}

public sealed class WsaaTicketsDesignTimeFactory : IDesignTimeDbContextFactory<MariaDbWsaaTicketsMigrationsDbContext>
{
    public MariaDbWsaaTicketsMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MariaDbWsaaTicketsMigrationsDbContext>()
        .UseMySql("Server=localhost;Database=netarcaws_design_time", new MariaDbServerVersion(new Version(11, 4, 13)), x => x.MigrationsAssembly(typeof(WsaaTicketsDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsTicketMigrations")).Options);
}

public sealed class TenantCertificatesDesignTimeFactory : IDesignTimeDbContextFactory<MariaDbTenantCertificatesMigrationsDbContext>
{
    public MariaDbTenantCertificatesMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MariaDbTenantCertificatesMigrationsDbContext>()
        .UseMySql("Server=localhost;Database=netarcaws_design_time", new MariaDbServerVersion(new Version(11, 4, 13)), options => options.MigrationsAssembly(typeof(TenantCertificatesDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsCertificateMigrations")).Options);
}

public sealed class InvoiceRecoveryDesignTimeFactory : IDesignTimeDbContextFactory<MariaDbInvoiceRecoveryMigrationsDbContext>
{
    public MariaDbInvoiceRecoveryMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MariaDbInvoiceRecoveryMigrationsDbContext>()
        .UseMySql("Server=localhost;Database=netarcaws_design_time", new MariaDbServerVersion(new Version(11, 4, 13)), options => options.MigrationsAssembly(typeof(InvoiceRecoveryDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceRecoveryMigrations")).Options);
}
