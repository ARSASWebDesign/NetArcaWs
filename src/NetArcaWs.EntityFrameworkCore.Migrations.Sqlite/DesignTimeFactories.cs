using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NetArcaWs.EntityFrameworkCore.Migrations.Sqlite;

public sealed class InvoicingDesignTimeFactory : IDesignTimeDbContextFactory<SqliteInvoicingMigrationsDbContext>
{
    public SqliteInvoicingMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqliteInvoicingMigrationsDbContext>()
        .UseSqlite("Data Source=:memory:", x => x.MigrationsAssembly(typeof(InvoicingDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceMigrations")).Options);
}

public sealed class WsaaTicketsDesignTimeFactory : IDesignTimeDbContextFactory<SqliteWsaaTicketsMigrationsDbContext>
{
    public SqliteWsaaTicketsMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqliteWsaaTicketsMigrationsDbContext>()
        .UseSqlite("Data Source=:memory:", x => x.MigrationsAssembly(typeof(WsaaTicketsDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsTicketMigrations")).Options);
}

public sealed class TenantCertificatesDesignTimeFactory : IDesignTimeDbContextFactory<SqliteTenantCertificatesMigrationsDbContext>
{
    public SqliteTenantCertificatesMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqliteTenantCertificatesMigrationsDbContext>()
        .UseSqlite("Data Source=:memory:", x => x.MigrationsAssembly(typeof(TenantCertificatesDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsCertificateMigrations")).Options);
}

public sealed class InvoiceRecoveryDesignTimeFactory : IDesignTimeDbContextFactory<SqliteInvoiceRecoveryMigrationsDbContext>
{
    public SqliteInvoiceRecoveryMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqliteInvoiceRecoveryMigrationsDbContext>()
        .UseSqlite("Data Source=:memory:", x => x.MigrationsAssembly(typeof(InvoiceRecoveryDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceRecoveryMigrations")).Options);
}
