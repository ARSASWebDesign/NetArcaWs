using NetArcaWs.Build;
using AwesomeAssertions;
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

    private static int Count(string text, string value) =>
        text.Split(value, StringSplitOptions.None).Length - 1;
}
