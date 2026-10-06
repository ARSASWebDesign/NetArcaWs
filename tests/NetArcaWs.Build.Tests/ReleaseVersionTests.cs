using FluentAssertions;
using Xunit;

namespace NetArcaWs.Build.Tests;

public sealed class ReleaseVersionTests
{
    [Fact]
    public void Validate_returns_the_shared_version_when_both_projects_match()
    {
        using var workspace = new BuildTestWorkspace();
        WriteVersions(workspace, "0.5.0", "0.5.0");

        ReleaseVersion.Validate(workspace.Root).Should().Be("0.5.0");
    }

    [Fact]
    public void Validate_accepts_a_matching_stable_release_tag_and_flag()
    {
        using var workspace = new BuildTestWorkspace();
        WriteVersions(workspace, "0.5.0", "0.5.0");

        ReleaseVersion.Validate(workspace.Root, "v0.5.0", prerelease: false).Should().Be("0.5.0");
    }

    [Fact]
    public void Validate_accepts_a_matching_prerelease_tag_and_flag()
    {
        using var workspace = new BuildTestWorkspace();
        WriteVersions(workspace, "0.6.0-rc.1", "0.6.0-rc.1");

        ReleaseVersion.Validate(workspace.Root, "v0.6.0-rc.1", prerelease: true).Should().Be("0.6.0-rc.1");
    }

    [Theory]
    [InlineData("01.2.3")]
    [InlineData("1.02.3")]
    [InlineData("1.2.03")]
    [InlineData("1.2.3-01")]
    [InlineData("1.2.3-rc.01")]
    [InlineData("1.2.3-RC.1")]
    [InlineData("1.2.3+build.7")]
    public void Validate_rejects_noncanonical_or_unsupported_semver(string version)
    {
        using var workspace = new BuildTestWorkspace();
        WriteVersions(workspace, version, version);

        Action validate = () => ReleaseVersion.Validate(workspace.Root);

        validate.Should().Throw<Exception>();
    }

    [Fact]
    public void Validate_rejects_different_versions_in_the_library_and_tool_projects()
    {
        using var workspace = new BuildTestWorkspace();
        WriteVersions(workspace, "0.5.0", "0.5.1");

        Action validate = () => ReleaseVersion.Validate(workspace.Root);

        validate.Should().Throw<Exception>();
    }

    [Fact]
    public void Validate_rejects_a_release_tag_that_does_not_match_the_version()
    {
        using var workspace = new BuildTestWorkspace();
        WriteVersions(workspace, "0.5.0", "0.5.0");

        Action validate = () => ReleaseVersion.Validate(workspace.Root, "v0.5.1", prerelease: false);

        validate.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("0.5.0", "v0.5.0", true)]
    [InlineData("0.6.0-rc.1", "v0.6.0-rc.1", false)]
    public void Validate_rejects_a_prerelease_flag_that_disagrees_with_the_version(string version, string tag, bool prerelease)
    {
        using var workspace = new BuildTestWorkspace();
        WriteVersions(workspace, version, version);

        Action validate = () => ReleaseVersion.Validate(workspace.Root, tag, prerelease);

        validate.Should().Throw<Exception>();
    }

    private static void WriteVersions(BuildTestWorkspace workspace, string libraryVersion, string toolVersion)
    {
        workspace.Write("src/NetArcaWs/NetArcaWs.csproj", ProjectFile(libraryVersion));
        workspace.Write("src/NetArcaWs.Tool/NetArcaWs.Tool.csproj", ProjectFile(toolVersion));
    }

    private static string ProjectFile(string version) =>
        $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><Version>{version}</Version></PropertyGroup></Project>";
}
