using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NetArcaWs.Build;

/// <summary>Builds a source archive and license evidence bound to one committed source tree.</summary>
public static class ReleaseSource
{
    public const string SourceArchiveName = "SOURCE.zip";
    public const string LicenseEvidenceName = "LICENSE_EVIDENCE.json";
    private const string InventoryPath = LicenseAudit.InventoryPath;
    private static readonly string[] RequiredSourceFiles = new[]
    {
        "LICENSE",
        "COPYING",
        "THIRD-PARTY-NOTICES.md",
        "licenses/policy.json",
        "NetArcaWs.slnx",
        "global.json",
        "Directory.Build.props",
        InventoryPath
    }.Concat(ReleaseVersion.PackageProjects.Select(project => $"src/{project}/{project}.csproj"))
        .Append("tools/NetArcaWs.Build/NetArcaWs.Build.csproj").ToArray();
    private static readonly JsonSerializerOptions EvidenceJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static void Generate(string repository, string expectedCommit, string artifactsDirectory)
    {
        string root = Path.GetFullPath(repository);
        string output = Path.GetFullPath(artifactsDirectory);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Repository directory does not exist: {root}.");
        if (!IsCommit(expectedCommit)) throw new InvalidOperationException("Expected release commit must be a full lowercase 40-character hash.");

        string actualCommit = Git(root, "rev-parse", "--verify", "HEAD");
        if (!actualCommit.Equals(expectedCommit, StringComparison.Ordinal))
            throw new InvalidOperationException("Expected release commit does not match HEAD.");
        if (Git(root, "status", "--porcelain", "--untracked-files=no").Length != 0)
            throw new InvalidOperationException("Cannot create release source archive because the tracked working tree is dirty.");

        Directory.CreateDirectory(output);
        string sourcePath = Path.Combine(output, SourceArchiveName);
        string evidencePath = Path.Combine(output, LicenseEvidenceName);
        string[] treeEntries = Git(root, "ls-tree", "--full-tree", "-r", "-z", expectedCommit)
            .Split('\0', StringSplitOptions.RemoveEmptyEntries);
        var parsedEntries = treeEntries.Select(entry =>
        {
            int separator = entry.IndexOf('\t');
            if (separator < 0) throw new InvalidOperationException("Committed source tree contains a malformed Git tree entry.");
            string[] metadata = entry[..separator].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (metadata.Length != 3 || metadata[1] != "blob"
                || (metadata[0] != "100644" && metadata[0] != "100755"))
                throw new InvalidOperationException("Committed source tree contains a gitlink, symlink or unsupported file mode.");
            return entry[(separator + 1)..];
        }).ToArray();
        string[] expectedFiles = parsedEntries;
        if (expectedFiles.Length == 0 || expectedFiles.Any(path => !IsSafeArchivePath(path))
            || expectedFiles.Distinct(StringComparer.Ordinal).Count() != expectedFiles.Length)
            throw new InvalidOperationException("Committed source tree contains an unsafe or duplicate archive path.");
        if (RequiredSourceFiles.Any(required => !expectedFiles.Contains(required, StringComparer.Ordinal))
            || !expectedFiles.Any(path => path.StartsWith("licenses/third-party/", StringComparison.Ordinal)))
            throw new InvalidOperationException("Committed source archive is missing required build, LGPL or third-party license inputs.");

        Git(root, "archive", "--format=zip", "--output", sourcePath, expectedCommit);
        using (var archive = ZipFile.OpenRead(sourcePath))
        {
            string[] actualFiles = archive.Entries.Where(entry => !entry.FullName.EndsWith("/", StringComparison.Ordinal))
                .Select(entry => entry.FullName).ToArray();
            if (actualFiles.Any(path => !IsSafeArchivePath(path))
                || actualFiles.Distinct(StringComparer.Ordinal).Count() != actualFiles.Length
                || !actualFiles.Order(StringComparer.Ordinal).SequenceEqual(expectedFiles.Order(StringComparer.Ordinal), StringComparer.Ordinal))
                throw new InvalidOperationException("Generated source archive does not contain the complete tracked file tree.");
            if (!archive.Comment.Equals(expectedCommit, StringComparison.Ordinal))
                throw new InvalidOperationException("Generated source archive commit comment does not match the expected commit.");
        }

        string inventoryHash = HashEntry(sourcePath, InventoryPath);
        string sourceHash = HashFile(sourcePath);
        var evidence = new LicenseEvidence(1, expectedCommit, InventoryPath, inventoryHash, sourceHash);
        string json = JsonSerializer.Serialize(evidence, EvidenceJsonOptions) + "\n";
        File.WriteAllText(evidencePath, json, new System.Text.UTF8Encoding(false));
        ValidateEvidenceAndSource(sourcePath, evidencePath, expectedCommit);
    }

    internal static void ValidateEvidenceAndSource(string sourcePath, string evidencePath, string expectedCommit)
    {
        if (!IsCommit(expectedCommit)) throw new InvalidOperationException("BUILD_COMMIT must contain one full lowercase 40-character commit hash.");
        if (!File.Exists(sourcePath) || !File.Exists(evidencePath))
            throw new InvalidOperationException("Release artifacts require SOURCE.zip and LICENSE_EVIDENCE.json.");

        LicenseEvidence evidence;
        try
        {
            evidence = JsonSerializer.Deserialize<LicenseEvidence>(File.ReadAllText(evidencePath), EvidenceJsonOptions)
                ?? throw new JsonException("Empty evidence.");
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("LICENSE_EVIDENCE.json is invalid.");
        }

        if (evidence.SchemaVersion != 1 || !string.Equals(evidence.Commit, expectedCommit, StringComparison.Ordinal))
            throw new InvalidOperationException("License evidence commit does not match BUILD_COMMIT.");
        if (!string.Equals(evidence.InventoryPath, InventoryPath, StringComparison.Ordinal)
            || !IsHash(evidence.InventorySha256 ?? "") || !IsHash(evidence.SourceSha256 ?? ""))
            throw new InvalidOperationException("LICENSE_EVIDENCE.json has an invalid inventory path or SHA-256 value.");

        using var archive = ZipFile.OpenRead(sourcePath);
        if (!archive.Comment.Equals(expectedCommit, StringComparison.Ordinal))
            throw new InvalidOperationException("Source archive commit does not match BUILD_COMMIT.");
        string[] entries = archive.Entries.Select(entry => entry.FullName).ToArray();
        if (entries.Any(entry => !IsSafeArchiveEntry(entry))
            || entries.Distinct(StringComparer.Ordinal).Count() != entries.Length
            || archive.Entries.Any(IsSymbolicLink))
            throw new InvalidOperationException("Source archive contains an unsafe path, duplicate entry or symbolic link.");
        string actualSourceHash = HashFile(sourcePath);
        if (!actualSourceHash.Equals(evidence.SourceSha256, StringComparison.Ordinal))
            throw new InvalidOperationException("License evidence does not match the source archive checksum.");
        ZipArchiveEntry[] inventory = archive.Entries.Where(entry => entry.FullName == InventoryPath).ToArray();
        if (inventory.Length != 1)
            throw new InvalidOperationException("Source archive must contain exactly one committed license inventory.");
        using Stream inventoryStream = inventory[0].Open();
        string actualInventoryHash = Convert.ToHexString(SHA256.HashData(inventoryStream)).ToLowerInvariant();
        if (!actualInventoryHash.Equals(evidence.InventorySha256, StringComparison.Ordinal))
            throw new InvalidOperationException("License evidence does not match the source archive license inventory.");
    }

    private static string HashEntry(string archivePath, string entryName)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        ZipArchiveEntry[] entries = archive.Entries.Where(entry => entry.FullName == entryName).ToArray();
        if (entries.Length != 1) throw new InvalidOperationException($"Source archive must contain exactly one {entryName}.");
        using Stream stream = entries[0].Open();
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string HashFile(string path)
    {
        using Stream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static bool IsSafeArchivePath(string path) => !string.IsNullOrWhiteSpace(path)
        && !path.StartsWith("/", StringComparison.Ordinal)
        && !path.Contains('\\')
        && !path.Any(char.IsControl)
        && path.Split('/').All(segment => segment.Length > 0 && segment is not ("." or "..") && !segment.Contains(':'));

    private static bool IsSafeArchiveEntry(string path)
    {
        string normalized = path.EndsWith("/", StringComparison.Ordinal) ? path[..^1] : path;
        return IsSafeArchivePath(normalized);
    }

    private static bool IsSymbolicLink(ZipArchiveEntry entry) => ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000;

    private static bool IsCommit(string value) => value.Length == 40 && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static bool IsHash(string value) => value.Length == 64 && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static string Git(string workingDirectory, params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false, true),
            UseShellExecute = false
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start git for release source generation.");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        string output = stdout.GetAwaiter().GetResult();
        string error = stderr.GetAwaiter().GetResult();
        if (process.ExitCode != 0) throw new InvalidOperationException($"git failed ({process.ExitCode}): {error.Trim()}");
        return output.TrimEnd('\r', '\n');
    }

    private sealed record LicenseEvidence(int SchemaVersion, string? Commit, string? InventoryPath, string? InventorySha256, string? SourceSha256);
}
