using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NetArcaWs.EntityFrameworkCore.Migrations.SqlServer;

public sealed class InvoicingDesignTimeFactory : IDesignTimeDbContextFactory<SqlServerInvoicingMigrationsDbContext>
{
    public SqlServerInvoicingMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqlServerInvoicingMigrationsDbContext>()
        .UseSqlServer("Server=localhost;Database=netarcaws_design_time;Integrated Security=true;TrustServerCertificate=true", options => options.MigrationsAssembly(typeof(InvoicingDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceMigrations", "dbo")).Options);
}

public sealed class WsaaTicketsDesignTimeFactory : IDesignTimeDbContextFactory<SqlServerWsaaTicketsMigrationsDbContext>
{
    public SqlServerWsaaTicketsMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqlServerWsaaTicketsMigrationsDbContext>()
        .UseSqlServer("Server=localhost;Database=netarcaws_design_time;Integrated Security=true;TrustServerCertificate=true", options => options.MigrationsAssembly(typeof(WsaaTicketsDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsTicketMigrations", "dbo")).Options);
}
