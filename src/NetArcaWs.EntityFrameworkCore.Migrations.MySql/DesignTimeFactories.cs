using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NetArcaWs.EntityFrameworkCore.Migrations.MySql;

public sealed class InvoicingDesignTimeFactory : IDesignTimeDbContextFactory<MySqlInvoicingMigrationsDbContext>
{
    public MySqlInvoicingMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MySqlInvoicingMigrationsDbContext>()
        .UseMySql("Server=localhost;Database=netarcaws_design_time", new MySqlServerVersion(new Version(8, 4, 11)), x => x.MigrationsAssembly(typeof(InvoicingDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceMigrations")).Options);
}

public sealed class WsaaTicketsDesignTimeFactory : IDesignTimeDbContextFactory<MySqlWsaaTicketsMigrationsDbContext>
{
    public MySqlWsaaTicketsMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MySqlWsaaTicketsMigrationsDbContext>()
        .UseMySql("Server=localhost;Database=netarcaws_design_time", new MySqlServerVersion(new Version(8, 4, 11)), x => x.MigrationsAssembly(typeof(WsaaTicketsDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsTicketMigrations")).Options);
}

public sealed class TenantCertificatesDesignTimeFactory : IDesignTimeDbContextFactory<MySqlTenantCertificatesMigrationsDbContext>
{
    public MySqlTenantCertificatesMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MySqlTenantCertificatesMigrationsDbContext>()
        .UseMySql("Server=localhost;Database=netarcaws_design_time", new MySqlServerVersion(new Version(8, 4, 11)), options => options.MigrationsAssembly(typeof(TenantCertificatesDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsCertificateMigrations")).Options);
}

public sealed class InvoiceRecoveryDesignTimeFactory : IDesignTimeDbContextFactory<MySqlInvoiceRecoveryMigrationsDbContext>
{
    public MySqlInvoiceRecoveryMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MySqlInvoiceRecoveryMigrationsDbContext>()
        .UseMySql("Server=localhost;Database=netarcaws_design_time", new MySqlServerVersion(new Version(8, 4, 11)), options => options.MigrationsAssembly(typeof(InvoiceRecoveryDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceRecoveryMigrations")).Options);
}
