using System.Security.Cryptography;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
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

    [Theory]
    [InlineData("NetArcaWs.Rendering.Html")]
    [InlineData("NetArcaWs.Rendering.Pdf")]
    public void ValidateArtifacts_rejects_missing_renderer_package(string package)
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        File.Delete(Path.Combine(workspace.Root, package + ".0.6.0.nupkg"));

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

        validate.Should().Throw<InvalidOperationException>().WithMessage("*exactly the 13 allowlisted packages*");
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

        validate.Should().Throw<InvalidOperationException>().WithMessage("Release payload checksum does not match:*");
    }

    [Fact]
    public void ValidateArtifacts_rejects_missing_source_or_license_evidence()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        File.Delete(Path.Combine(workspace.Root, "SOURCE.zip"));

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateArtifacts_rejects_unexpected_top_level_release_files()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        workspace.Write("unexpected.txt", "extra");

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>().WithMessage("*exact release artifact allowlist*");
    }

    [Fact]
    public void ValidateArtifacts_rejects_source_archive_commit_mismatch()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        string archivePath = Path.Combine(workspace.Root, "SOURCE.zip");
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update)) archive.Comment = new string('b', 40);
        RewriteSums(workspace);

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>().WithMessage("*source archive commit does not match BUILD_COMMIT*");
    }

    [Fact]
    public void ValidateArtifacts_rejects_source_or_inventory_evidence_tampering()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        workspace.Write("LICENSE_EVIDENCE.json", "{\"schemaVersion\":1,\"commit\":\"" + new string('a', 40)
            + "\",\"inventoryPath\":\"licenses/dependencies.json\",\"inventorySha256\":\"" + new string('0', 64)
            + "\",\"sourceSha256\":\"" + new string('0', 64) + "\"}\n");
        RewriteSums(workspace);

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>().WithMessage("*license evidence does not match the source archive*");
    }

    [Fact]
    public void ValidateArtifacts_rejects_evidence_commit_mismatch_even_when_checksums_match()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        JsonDocument evidence = JsonDocument.Parse(workspace.Read("LICENSE_EVIDENCE.json"));
        string sourceHash = evidence.RootElement.GetProperty("sourceSha256").GetString()!;
        string inventoryHash = evidence.RootElement.GetProperty("inventorySha256").GetString()!;
        evidence.Dispose();
        workspace.Write("LICENSE_EVIDENCE.json", Evidence(new string('b', 40), inventoryHash, sourceHash));
        RewriteSums(workspace);

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>().WithMessage("*license evidence commit does not match BUILD_COMMIT*");
    }

    [Fact]
    public void ValidateArtifacts_rejects_missing_or_null_evidence_hash_fields()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        workspace.Write("LICENSE_EVIDENCE.json", "{\"schemaVersion\":1,\"commit\":\"" + new string('a', 40)
            + "\",\"inventoryPath\":\"licenses/dependencies.json\",\"inventorySha256\":null}\n");
        RewriteSums(workspace);

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>().WithMessage("*invalid inventory path or SHA-256 value*");
    }

    [Fact]
    public void ValidateArtifacts_rejects_inventory_evidence_drift_with_matching_archive_checksum()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        using JsonDocument evidence = JsonDocument.Parse(workspace.Read("LICENSE_EVIDENCE.json"));
        string commit = evidence.RootElement.GetProperty("commit").GetString()!;
        string sourceHash = evidence.RootElement.GetProperty("sourceSha256").GetString()!;
        workspace.Write("LICENSE_EVIDENCE.json", Evidence(commit, new string('0', 64), sourceHash));
        RewriteSums(workspace);

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>().WithMessage("*source archive license inventory*");
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("licenses/dependencies.json")]
    public void ValidateArtifacts_rejects_unsafe_or_duplicate_source_entries(string entryName)
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        using (var archive = ZipFile.Open(Path.Combine(workspace.Root, "SOURCE.zip"), ZipArchiveMode.Update))
        {
            ZipArchiveEntry added = archive.CreateEntry(entryName);
            using StreamWriter writer = new(added.Open());
            writer.Write("tamper");
        }
        RewriteSums(workspace);

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>().WithMessage("*unsafe path, duplicate entry or symbolic link*");
    }

    [Fact]
    public void ValidateArtifacts_rejects_symlink_entries_in_source_archive()
    {
        using var workspace = new BuildTestWorkspace();
        WriteManifest(workspace, "0.6.0");
        using (var archive = ZipFile.Open(Path.Combine(workspace.Root, "SOURCE.zip"), ZipArchiveMode.Update))
        {
            ZipArchiveEntry link = archive.CreateEntry("outside-link");
            link.ExternalAttributes = unchecked((0xA000 << 16) | 0x1FF);
        }
        RewriteSums(workspace);

        Action validate = () => ReleaseAssets.ValidateArtifacts("0.6.0", workspace.Root);

        validate.Should().Throw<InvalidOperationException>().WithMessage("*unsafe path, duplicate entry or symbolic link*");
    }

    private static void WriteManifest(BuildTestWorkspace workspace, string version)
    {
        string commit = new string('a', 40);
        var entries = new List<string>();
        foreach (string project in ReleaseVersion.PackageProjects)
        {
            string name = $"{project}.{version}.nupkg";
            byte[] contents = System.Text.Encoding.UTF8.GetBytes(project);
            workspace.Write(name, Convert.ToBase64String(contents));
            entries.Add($"{Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Convert.ToBase64String(contents)))).ToLowerInvariant()}  {name}");
        }
        const string inventory = "{\"schemaVersion\":1}\n";
        using (var archive = ZipFile.Open(Path.Combine(workspace.Root, "SOURCE.zip"), ZipArchiveMode.Create))
        {
            archive.Comment = commit;
            ZipArchiveEntry inventoryEntry = archive.CreateEntry("licenses/dependencies.json");
            using StreamWriter writer = new(inventoryEntry.Open(), new UTF8Encoding(false));
            writer.Write(inventory);
        }
        string sourceHash = Hash(File.ReadAllBytes(Path.Combine(workspace.Root, "SOURCE.zip")));
        string inventoryHash = Hash(Encoding.UTF8.GetBytes(inventory));
        workspace.Write("LICENSE_EVIDENCE.json", Evidence(commit, inventoryHash, sourceHash));
        entries.Add($"{Hash(File.ReadAllBytes(Path.Combine(workspace.Root, "SOURCE.zip")))}  SOURCE.zip");
        entries.Add($"{Hash(File.ReadAllBytes(Path.Combine(workspace.Root, "LICENSE_EVIDENCE.json")))}  LICENSE_EVIDENCE.json");
        workspace.Write("SHA256SUMS", string.Join('\n', entries) + "\n");
        workspace.Write("BUILD_COMMIT", commit + "\n");
    }

    private static void RewriteSums(BuildTestWorkspace workspace)
    {
        var entries = Directory.GetFiles(workspace.Root, "*.nupkg")
            .Select(path => $"{Hash(File.ReadAllBytes(path))}  {Path.GetFileName(path)}")
            .Append($"{Hash(File.ReadAllBytes(Path.Combine(workspace.Root, "SOURCE.zip")))}  SOURCE.zip")
            .Append($"{Hash(File.ReadAllBytes(Path.Combine(workspace.Root, "LICENSE_EVIDENCE.json")))}  LICENSE_EVIDENCE.json")
            .Order(StringComparer.Ordinal);
        workspace.Write("SHA256SUMS", string.Join('\n', entries) + "\n");
    }

    private static string Evidence(string commit, string inventoryHash, string sourceHash) =>
        JsonSerializer.Serialize(new { schemaVersion = 1, commit, inventoryPath = "licenses/dependencies.json", inventorySha256 = inventoryHash, sourceSha256 = sourceHash }) + "\n";

    private static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}
