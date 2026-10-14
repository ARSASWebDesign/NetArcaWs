using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Build.Tests;

public sealed class ReleaseLicenseWorkflowTests
{
    [Theory]
    [InlineData("ci.yml", "dotnet pack src/NetArcaWs.Rendering.Pdf/NetArcaWs.Rendering.Pdf.csproj")]
    [InlineData("release.yml", "dotnet pack \"src/$project/$project.csproj\"")]
    public void Workflow_checks_inventory_before_pack_and_packages_after_pack(string workflowName, string lastPackStep)
    {
        string workflow = File.ReadAllText(RepositoryFile(".github", "workflows", workflowName));

        workflow.IndexOf("licenses --check", StringComparison.Ordinal).Should().BeGreaterThanOrEqualTo(0);
        workflow.IndexOf("licenses --check", StringComparison.Ordinal)
            .Should().BeLessThan(workflow.IndexOf(lastPackStep, StringComparison.Ordinal));
        workflow.IndexOf(lastPackStep, StringComparison.Ordinal)
            .Should().BeLessThan(workflow.IndexOf("licenses --check-packages artifacts", StringComparison.Ordinal));
    }

    [Fact]
    public void Release_publisher_verifies_immutable_source_evidence_before_oidc_without_running_repo_code()
    {
        string workflow = File.ReadAllText(RepositoryFile(".github", "workflows", "release.yml"));
        string publish = workflow[workflow.IndexOf("\n  publish:", StringComparison.Ordinal)..];

        publish.Should().NotContain("actions/checkout@").And.NotContain("tools/NetArcaWs.Build");
        publish.Should().NotMatchRegex("dotnet\\s+(run|build|restore|test|pack)\\b");
        publish.Should().Contain("SOURCE.zip").And.Contain("LICENSE_EVIDENCE.json");
        publish.IndexOf("Verify build artifacts", StringComparison.Ordinal)
            .Should().BeLessThan(publish.IndexOf("NuGet trusted publishing login", StringComparison.Ordinal));
        publish.Should().Contain("unzip -z SOURCE.zip").And.Contain("gh release upload");
        publish.Should().Contain("zipinfo -l SOURCE.zip")
            .And.Contain("type != \"-\" && type != \"d\"")
            .And.Contain("sub(/\\/$/, \"\", name); print name");
        publish.Should().Contain("Do not replace published artifacts");
    }

    private static string RepositoryFile(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "NetArcaWs.slnx")))
            directory = directory.Parent;
        string root = directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate NetArcaWs.slnx.");
        return Path.Combine([root, .. segments]);
    }
}
