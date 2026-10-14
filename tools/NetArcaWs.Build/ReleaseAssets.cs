using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace NetArcaWs.Build;

public static class ReleaseAssets
{
    public static async Task RunAsync(string repository, string tag, string artifacts)
    {
        if (string.IsNullOrWhiteSpace(repository) || tag.Length < 2 || tag[0] != 'v')
            throw new ArgumentException("A repository and v-prefixed release tag are required.");
        ValidateArtifacts(tag[1..], artifacts);
        using var release = JsonDocument.Parse(await GhAsync("release", "view", tag, "--repo", repository, "--json", "assets"));
        var existing = release.RootElement.GetProperty("assets").EnumerateArray()
            .Select(asset => asset.GetProperty("name").GetString() ?? throw new InvalidOperationException("GitHub release asset has no name."))
            .ToHashSet(StringComparer.Ordinal);
        string[] files = ExpectedFiles(tag[1..], artifacts);
        var expectedNames = files.Select(file => Path.GetFileName(file)!).ToHashSet(StringComparer.Ordinal);
        string[] unexpected = existing.Where(name => !expectedNames.Contains(name)).Order(StringComparer.Ordinal).ToArray();
        if (unexpected.Length != 0)
            throw new InvalidOperationException($"Release contains assets outside the exact allowlist: {string.Join(", ", unexpected)}.");
        var temporary = Directory.CreateTempSubdirectory("netarca-release-");
        try
        {
            var missing = new List<string>();
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                if (!existing.Contains(name))
                {
                    missing.Add(file);
                    continue;
                }
                await GhAsync("release", "download", tag, "--repo", repository, "--pattern", name, "--dir", temporary.FullName);
                using var local = File.OpenRead(file);
                using var remote = File.OpenRead(Path.Combine(temporary.FullName, name));
                var localHash = await SHA256.HashDataAsync(local);
                var remoteHash = await SHA256.HashDataAsync(remote);
                if (!CryptographicOperations.FixedTimeEquals(localHash, remoteHash))
                    throw new InvalidOperationException($"Existing release asset differs: {name}. Do not replace published artifacts; create a new version.");
            }
            if (missing.Count != 0)
                await GhAsync(["release", "upload", tag, "--repo", repository, .. missing]);
        }
        finally { temporary.Delete(true); }
    }

    public static void ValidateArtifacts(string version, string artifacts)
    {
        string[] expectedFiles = ExpectedFiles(version, artifacts);
        var packages = expectedFiles.Take(ReleaseVersion.PackageProjects.Count).ToArray();
        var actualPackages = Directory.GetFiles(artifacts, "*.nupkg").Order(StringComparer.Ordinal).ToArray();
        if (!actualPackages.SequenceEqual(packages.Order(StringComparer.Ordinal), StringComparer.Ordinal))
            throw new InvalidOperationException($"Release artifacts must contain exactly the {ReleaseVersion.PackageProjects.Count} allowlisted packages for the release version.");

        var expectedTopLevel = expectedFiles.Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);
        var actualTopLevel = Directory.GetFiles(artifacts, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);
        if (!actualTopLevel.SetEquals(expectedTopLevel))
            throw new InvalidOperationException("Release artifacts do not match the exact release artifact allowlist (17 top-level files).");

        string commitPath = Path.Combine(artifacts, "BUILD_COMMIT");
        string commit = File.ReadAllText(commitPath).Trim();
        if (commit.Length != 40 || commit.Any(character => character is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new InvalidOperationException("BUILD_COMMIT must contain one full 40-character commit hash.");
        ReleaseSource.ValidateEvidenceAndSource(
            Path.Combine(artifacts, ReleaseSource.SourceArchiveName),
            Path.Combine(artifacts, ReleaseSource.LicenseEvidenceName),
            commit);

        string sumsPath = Path.Combine(artifacts, "SHA256SUMS");
        var expectedNames = packages.Select(Path.GetFileName)
            .Append(ReleaseSource.SourceArchiveName)
            .Append(ReleaseSource.LicenseEvidenceName)
            .ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(sumsPath))
        {
            string[] fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length != 2 || fields[0].Length != 64 || fields[0].Any(character => !Uri.IsHexDigit(character)))
                throw new InvalidOperationException("SHA256SUMS must contain exactly one SHA-256 entry per release payload.");
            string name = fields[1].StartsWith("./", StringComparison.Ordinal) ? fields[1][2..] : fields[1];
            if (!expectedNames.Contains(name) || !seen.Add(name) || Path.GetFileName(name) != name)
                throw new InvalidOperationException("SHA256SUMS contains an unexpected, unsafe or duplicate release payload entry.");
            using var stream = File.OpenRead(Path.Combine(artifacts, name));
            string actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(fields[0]), Convert.FromHexString(actualHash)))
                throw new InvalidOperationException($"Release payload checksum does not match: {name}.");
        }
        if (seen.Count != expectedNames.Count)
            throw new InvalidOperationException("SHA256SUMS must contain exactly one SHA-256 entry per release payload.");
    }

    private static string[] ExpectedFiles(string version, string artifacts)
    {
        string[] packages = ReleaseVersion.PackageProjects.Select(project => Path.Combine(artifacts, project + "." + version + ".nupkg")).ToArray();
        var files = packages.Concat(new[]
        {
            Path.Combine(artifacts, ReleaseSource.SourceArchiveName),
            Path.Combine(artifacts, ReleaseSource.LicenseEvidenceName),
            Path.Combine(artifacts, "SHA256SUMS"),
            Path.Combine(artifacts, "BUILD_COMMIT")
        }).ToArray();
        if (files.Any(file => !File.Exists(file)))
            throw new InvalidOperationException($"Expected {ReleaseVersion.PackageProjects.Count} packages, SOURCE.zip, LICENSE_EVIDENCE.json, SHA256SUMS and BUILD_COMMIT.");
        return files;
    }

    private static async Task<string> GhAsync(params string[] arguments)
    {
        var start = new ProcessStartInfo("gh") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start gh.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var result = await stdout;
        var error = await stderr;
        if (process.ExitCode != 0) throw new InvalidOperationException($"gh failed ({process.ExitCode}): {error}");
        return result;
    }
}
