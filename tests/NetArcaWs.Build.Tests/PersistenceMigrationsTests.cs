using NetArcaWs.Build;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace NetArcaWs.Build.Tests;

public sealed class PersistenceMigrationsTests
{
    [Fact]
    public void Generates_ten_deterministic_offline_scripts_with_real_initial_migration_operations()
    {
        IReadOnlyDictionary<string, string> scripts = PersistenceMigrations.GenerateScripts();
        IReadOnlyDictionary<string, string> repeated = PersistenceMigrations.GenerateScripts();

        scripts.Should().HaveCount(10);
        repeated.Should().BeEquivalentTo(scripts);
        foreach (string provider in new[] { "sqlite", "mysql", "mariadb", "postgresql", "sqlserver" })
        {
            string invoice = scripts[$"{provider}/invoicing/0-latest.sql"];
            string tickets = scripts[$"{provider}/wsaa-tickets/0-latest.sql"];
            invoice.Should().Contain("20261006000100_InitialInvoicing")
                .And.Contain("__NetArcaWsInvoiceMigrations")
                .And.Contain("CREATE TABLE")
                .And.NotContain("RenameTable");
            tickets.Should().Contain("20261006000200_InitialWsaaTickets")
                .And.Contain("__NetArcaWsTicketMigrations")
                .And.Contain("CREATE TABLE")
                .And.NotContain("RenameTable");
            Count(invoice, "NetArcaInvoices").Should().BeGreaterThanOrEqualTo(3);
            Count(tickets, "NetArcaWsaaTickets").Should().BeGreaterThanOrEqualTo(1);
        }
        scripts.Values.Should().OnlyContain(script => script.EndsWith('\n') && !script.Contains("\r", StringComparison.Ordinal));
    }

    [Fact]
    public void Check_detects_missing_extra_and_corrupt_files_without_rewriting_anything()
    {
        string root = Path.Combine(Path.GetTempPath(), $"netarcaws-migrations-{Guid.NewGuid():N}");
        try
        {
            PersistenceMigrations.GenerateScripts().Should().HaveCount(10);
            IReadOnlyDictionary<string, string> generated = PersistenceMigrations.GenerateScripts();
            foreach ((string relative, string contents) in generated)
            {
                string path = Path.Combine(root, "docs", "reference", "persistence-migrations", relative.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, contents);
            }
            PersistenceMigrations.Check(root).IsCurrent.Should().BeTrue();
            string corrupt = Path.Combine(root, "docs", "reference", "persistence-migrations", "sqlite", "invoicing", "0-latest.sql");
            File.WriteAllText(corrupt, "corrupt\n");
            string extra = Path.Combine(root, "docs", "reference", "persistence-migrations", "sqlite", "extra.sql");
            File.WriteAllText(extra, "extra\n");
            string missing = Path.Combine(root, "docs", "reference", "persistence-migrations", "mysql", "wsaa-tickets", "0-latest.sql");
            File.Delete(missing);

            PersistenceMigrations.Check(root).Drifted.Should().BeEquivalentTo(
                "mysql/wsaa-tickets/0-latest.sql", "sqlite/extra.sql", "sqlite/invoicing/0-latest.sql");
            File.ReadAllText(corrupt).Should().Be("corrupt\n");
            File.Exists(missing).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Generates_the_full_zero_to_latest_script_for_a_test_only_two_migration_chain()
    {
        using DbContext context = new TwoMigrationSqliteContext(new DbContextOptionsBuilder<TwoMigrationSqliteContext>()
            .UseSqlite("Data Source=:memory:", options => options.MigrationsAssembly(typeof(TwoMigrationSqliteContext).Assembly.FullName)
                .MigrationsHistoryTable("__NetArcaWsInvoiceMigrations"))
            .Options);

        string script = PersistenceMigrations.GenerateScriptForContext("sqlite", "invoicing", context);

        script.Should().Contain("20261006000100_InitialInvoicing")
            .And.Contain("20261006000300_AddFixtureTable")
            .And.Contain("CREATE TABLE \"FutureInvoiceAux\"");
    }

    [Fact]
    public void PostgreSql_baselines_qualify_history_table_creation_and_migration_insert_in_public()
    {
        IReadOnlyDictionary<string, string> scripts = PersistenceMigrations.GenerateScripts();

        scripts["postgresql/invoicing/0-latest.sql"].Should()
            .Contain("CREATE TABLE IF NOT EXISTS public.\"__NetArcaWsInvoiceMigrations\"")
            .And.Contain("INSERT INTO public.\"__NetArcaWsInvoiceMigrations\"");
        scripts["postgresql/wsaa-tickets/0-latest.sql"].Should()
            .Contain("CREATE TABLE IF NOT EXISTS public.\"__NetArcaWsTicketMigrations\"")
            .And.Contain("INSERT INTO public.\"__NetArcaWsTicketMigrations\"");
    }

    private static int Count(string text, string value) =>
        text.Split(value, StringSplitOptions.None).Length - 1;
}

[DbContext(typeof(TwoMigrationSqliteContext))]
internal sealed class TwoMigrationSqliteSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) { }
}

internal sealed class TwoMigrationSqliteContext(DbContextOptions<TwoMigrationSqliteContext> options) : DbContext(options) { }

[DbContext(typeof(TwoMigrationSqliteContext))]
[Migration("20261006000100_InitialInvoicing")]
internal sealed class TestInitialInvoicingMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateTable(migrationBuilder, "NetArcaInvoices");
        CreateTable(migrationBuilder, "NetArcaInvoiceRevisions");
        CreateTable(migrationBuilder, "NetArcaInvoiceSeriesReservations");
    }

    protected override void Down(MigrationBuilder migrationBuilder) { }

    private static void CreateTable(MigrationBuilder migrationBuilder, string name) => migrationBuilder.CreateTable(
        name: name,
        columns: table => new { Id = table.Column<int>(type: "INTEGER", nullable: false) },
        constraints: table => table.PrimaryKey($"PK_{name}", row => row.Id));
}

[DbContext(typeof(TwoMigrationSqliteContext))]
[Migration("20261006000300_AddFixtureTable")]
internal sealed class TestFutureInvoicingMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "FutureInvoiceAux",
        columns: table => new { Id = table.Column<int>(type: "INTEGER", nullable: false) },
        constraints: table => table.PrimaryKey("PK_FutureInvoiceAux", row => row.Id));

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("FutureInvoiceAux");
}
