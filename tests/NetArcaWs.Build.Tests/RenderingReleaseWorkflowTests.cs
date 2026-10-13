using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Build.Tests;

public sealed class RenderingReleaseWorkflowTests
{
    [Theory]
    [InlineData("NetArcaWs.Rendering.Html")]
    [InlineData("NetArcaWs.Rendering.Pdf")]
    public void Release_workflow_builds_verifies_and_publishes_renderer_package(string package)
    {
        string workflow = File.ReadAllText(RepositoryFile(".github/workflows/release.yml"));

        Regex.Matches(workflow, $@"\b{Regex.Escape(package)}\b").Count.Should().BeGreaterThanOrEqualTo(6);
    }

    [Theory]
    [InlineData("NetArcaWs.Rendering.Html")]
    [InlineData("NetArcaWs.Rendering.Pdf")]
    public void Ci_workflow_packs_and_consumes_renderer_package(string package)
    {
        string workflow = File.ReadAllText(RepositoryFile(".github/workflows/ci.yml"));

        workflow.Should().Contain($"dotnet pack src/{package}/{package}.csproj");
        workflow.Should().Contain("tests/NetArcaWs.Rendering.PackageSmoke/NetArcaWs.Rendering.PackageSmoke.csproj");
    }

    [Fact]
    public void Ci_workflow_checks_that_core_nuspec_excludes_renderer_dependencies()
    {
        string workflow = File.ReadAllText(RepositoryFile(".github/workflows/ci.yml"));

        workflow.Should().Contain("Verify core package has no renderer dependencies")
            .And.Contain("PDFsharp|QRCoder|StbImageSharp");
    }

    private static string RepositoryFile(string relativePath)
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
