using System.Security.Cryptography;
using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Build.Tests;

public sealed class ReleaseAssetsTests
{
    [Fact]
    public void ValidateArtifacts_accepts_exact_allowlisted_packages_with_matching_hashes()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");

        ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);
    }

    [Fact]
    public void ValidateArtifacts_rejects_missing_or_extra_package_assets()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        File.Delete(Path.Combine(workspace.Root, "NetArcaWs.Tool.0.6.0.nupkg"));

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateArtifacts_rejects_unallowlisted_package_assets()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        workspace.Write("NetArcaWs.Extra.0.6.0.nupkg", "extra");

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>().WithMessage("*exactly the eleven allowlisted packages*");
    }

    [Fact]
    public void ValidateArtifacts_rejects_missing_duplicate_or_mismatched_hash_entries()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        workspace.Write("SHA256SUMS", workspace.Read("SHA256SUMS").Replace("NetArcaWs.Tool.0.6.0.nupkg", "NetArcaWs.0.6.0.nupkg", StringComparison.Ordinal));

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateArtifacts_rejects_a_well_formed_but_wrong_package_hash()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        string sums = workspace.Read("SHA256SUMS");
        int separator = sums.IndexOf("  ", StringComparison.Ordinal);
        workspace.Write("SHA256SUMS", new string('0', 64) + sums[separator..]);

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>().WithMessage("Package checksum does not match:*");
    }

    private static void WriteManifest(BuildTestWorkspace workspace, string version)
    {
        var entries = new List<string>();
        foreach (string project in ReleaseVersion.PackageProjects)
        {
            string name = $"{project}.{version}.nupkg";
            byte[] contents = System.Text.Encoding.UTF8.GetBytes(project);
            workspace.Write(name, Convert.ToBase64String(contents));
            entries.Add($"{Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Convert.ToBase64String(contents)))).ToLowerInvariant()}  {name}");
        }
        workspace.Write("SHA256SUMS", string.Join('\n', entries) + "\n");
        workspace.Write("BUILD_COMMIT", new string('a', 40) + "\n");
    }
}
