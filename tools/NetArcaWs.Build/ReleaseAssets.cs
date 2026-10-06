using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace NetArcaWs.Build;

public static class ReleaseAssets
{
    public static async Task RunAsync(string repository, string tag, string artifacts)
    {
        if (string.IsNullOrWhiteSpace(repository) || !tag.StartsWith('v'))
            throw new ArgumentException("A repository and v-prefixed release tag are required.");
        using var release = JsonDocument.Parse(await GhAsync("release", "view", tag, "--repo", repository, "--json", "assets"));
        var existing = release.RootElement.GetProperty("assets").EnumerateArray()
            .Select(asset => asset.GetProperty("name").GetString()).ToHashSet(StringComparer.Ordinal);
        var files = Directory.GetFiles(artifacts, "*.nupkg").Order(StringComparer.Ordinal)
            .Concat(new[] { Path.Combine(artifacts, "SHA256SUMS"), Path.Combine(artifacts, "BUILD_COMMIT") }).ToArray();
        if (files.Length != 4 || files.Any(file => !File.Exists(file)))
            throw new InvalidOperationException("Expected two packages, SHA256SUMS and BUILD_COMMIT.");
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
