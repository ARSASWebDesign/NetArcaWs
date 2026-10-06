using NetArcaWs.Build;
using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Build.Tests;

public sealed class PersistenceSchemaTests
{
    [Fact]
    public void GenerateScriptsIncludesAllProviderAndCapabilitySelections()
    {
        IReadOnlyDictionary<string, string> scripts = PersistenceSchema.GenerateScripts();

        scripts.Should().HaveCount(15);
        scripts.Keys.Should().Contain("sqlite/all.sql");
        scripts.Keys.Should().Contain("mysql/invoicing.sql");
        scripts.Keys.Should().Contain("mariadb/wsaa-tickets.sql");
        scripts.Keys.Should().Contain("postgresql/all.sql");
        scripts.Keys.Should().Contain("sqlserver/all.sql");
        scripts["sqlite/invoicing.sql"].Should().Contain("NetArcaInvoices")
            .And.Contain("NetArcaInvoiceRevisions")
            .And.Contain("NetArcaInvoiceSeriesReservations")
            .And.NotContain("NetArcaWsaaTickets");
        scripts["postgresql/wsaa-tickets.sql"].Should().Contain("NetArcaWsaaTickets")
            .And.NotContain("NetArcaInvoices");
        scripts["sqlserver/all.sql"].Should().Contain("NetArcaWsaaTickets")
            .And.Contain("NetArcaInvoices");
    }

    [Fact]
    public void CheckReportsDriftWithoutRewritingTheExistingScript()
    {
        string root = Path.Combine(Path.GetTempPath(), $"netarcaws-schema-{Guid.NewGuid():N}");
        PersistenceSchema.Run(root, check: false);
        string file = Path.Combine(root, "docs", "reference", "persistence", "sqlite", "all.sql");

        try
        {
            PersistenceSchema.Check(root).IsCurrent.Should().BeTrue();
            File.WriteAllText(file, "stale schema\n");
            PersistenceSchemaCheckResult result = PersistenceSchema.Check(root);

            result.IsCurrent.Should().BeFalse();
            result.Drifted.Should().ContainSingle().Which.Should().Be("sqlite/all.sql");
            File.ReadAllText(file).Should().Be("stale schema\n");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
