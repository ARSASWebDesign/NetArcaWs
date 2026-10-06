using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql;

public sealed class InvoicingDesignTimeFactory : IDesignTimeDbContextFactory<PostgreSqlInvoicingMigrationsDbContext>
{
    public PostgreSqlInvoicingMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<PostgreSqlInvoicingMigrationsDbContext>()
        .UseNpgsql("Host=localhost;Database=netarcaws_design_time;Username=netarcaws", options => options.MigrationsAssembly(typeof(InvoicingDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceMigrations", "public")).Options);
}

public sealed class WsaaTicketsDesignTimeFactory : IDesignTimeDbContextFactory<PostgreSqlWsaaTicketsMigrationsDbContext>
{
    public PostgreSqlWsaaTicketsMigrationsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<PostgreSqlWsaaTicketsMigrationsDbContext>()
        .UseNpgsql("Host=localhost;Database=netarcaws_design_time;Username=netarcaws", options => options.MigrationsAssembly(typeof(WsaaTicketsDesignTimeFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsTicketMigrations", "public")).Options);
}
