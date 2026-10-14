using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NetArcaWs.Build;

/// <summary>Maintains and verifies the repository's NuGet license inventory and release package legal materials.</summary>
public static class LicenseAudit
{
    public const string InventoryPath = "licenses/dependencies.json";
    public const string MaterialsRoot = "licenses/third-party";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    /// <summary>Updates or checks the locked dependency inventory, or checks release packages when a directory is supplied.</summary>
    public static void Run(string root, bool check, string? packageDirectory = null)
    {
        string repository = Path.GetFullPath(root);
        if (packageDirectory is not null)
        {
            CheckPackages(repository, Path.GetFullPath(packageDirectory));
            Console.WriteLine("Release package license audit passed.");
            return;
        }

        LicensePolicy policy = ReadPolicy(repository);
        List<PackageRecord> packages = ReadLockedPackages(repository, policy);
        string inventory = JsonSerializer.Serialize(new Inventory(1, packages.Select(package => package.ToManifest()).ToArray()), JsonOptions) + "\n";
        string inventoryFile = Path.Combine(repository, InventoryPath);
        if (check)
        {
            if (!File.Exists(inventoryFile) || File.ReadAllText(inventoryFile) != inventory)
                throw new InvalidOperationException("License inventory is missing or stale; run `dotnet run --project tools/NetArcaWs.Build -- licenses --update` and review the diff.");
            VerifyMaterials(repository, packages);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(inventoryFile)!);
            foreach (PackageRecord package in packages)
            foreach (Material file in package.Materials)
            {
                string target = SafeCombine(repository, file.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllBytes(target, file.Content);
            }
            File.WriteAllText(inventoryFile, inventory, new UTF8Encoding(false));
        }
        Console.WriteLine($"License inventory {(check ? "verified" : "updated")}: {packages.Count} locked package/version records.");
    }

    private static List<PackageRecord> ReadLockedPackages(string root, LicensePolicy policy)
    {
        string repositoryCache = Path.Combine(root, ".nuget", "packages");
        string cache = Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } configured
            ? Path.GetFullPath(configured)
            : Directory.Exists(repositoryCache) ? repositoryCache
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var locked = new SortedDictionary<string, LockedPackage>(StringComparer.OrdinalIgnoreCase);
        foreach (string lockPath in Directory.EnumerateFiles(root, "packages.lock.json", SearchOption.AllDirectories)
                     .Where(path => !Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar)
                         .Any(part => part is ".git" or "obj" or "bin" or "artifacts" or ".nuget" or "packages"))
                     .Order(StringComparer.Ordinal))
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(lockPath));
            if (!document.RootElement.TryGetProperty("dependencies", out JsonElement frameworks))
                throw new InvalidOperationException($"Invalid NuGet lock file: {Relative(root, lockPath)}.");
            foreach (JsonProperty framework in frameworks.EnumerateObject())
            foreach (JsonProperty package in framework.Value.EnumerateObject())
            {
                if (package.Value.TryGetProperty("type", out JsonElement packageType)
                    && packageType.GetString()?.Equals("Project", StringComparison.OrdinalIgnoreCase) == true)
                    continue;
                if (!package.Value.TryGetProperty("resolved", out JsonElement resolved))
                    throw new InvalidOperationException($"Locked package has no resolved version: {package.Name} in {Relative(root, lockPath)}.");
                string version = resolved.GetString() ?? "";
                string hash = package.Value.TryGetProperty("contentHash", out JsonElement hashValue) ? hashValue.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(hash))
                    throw new InvalidOperationException($"Missing locked version/content hash for {package.Name} in {Relative(root, lockPath)}.");
                string key = package.Name.ToLowerInvariant() + "/" + version.ToLowerInvariant();
                if (locked.TryGetValue(key, out LockedPackage? existing) && existing.ContentHash != hash)
                    throw new InvalidOperationException($"Conflicting lock hashes for {package.Name} {version}.");
                locked[key] = new LockedPackage(package.Name, version, hash);
            }
        }
        if (locked.Count == 0) throw new InvalidOperationException("No NuGet packages.lock.json files were found.");
        var records = new List<PackageRecord>(locked.Count);
        foreach (LockedPackage package in locked.Values)
        {
            string packageFile = Path.Combine(cache, package.Id.ToLowerInvariant(), package.Version.ToLowerInvariant(), $"{package.Id.ToLowerInvariant()}.{package.Version.ToLowerInvariant()}.nupkg");
            if (!File.Exists(packageFile)) throw new InvalidOperationException($"Cached NuGet package is missing: {package.Id} {package.Version}.");
            byte[] archiveBytes = File.ReadAllBytes(packageFile);
            byte[] expectedHash;
            try { expectedHash = Convert.FromBase64String(package.ContentHash); }
            catch (FormatException) { throw new InvalidOperationException($"Invalid locked content hash for {package.Id} {package.Version}."); }
            string metadataPath = Path.Combine(Path.GetDirectoryName(packageFile)!, ".nupkg.metadata");
            if (!File.Exists(metadataPath)) throw new InvalidOperationException($"NuGet restore metadata is missing for {package.Id} {package.Version}.");
            using JsonDocument restoreMetadata = JsonDocument.Parse(File.ReadAllText(metadataPath));
            string restoredContentHash = restoreMetadata.RootElement.TryGetProperty("contentHash", out JsonElement metadataHash) ? metadataHash.GetString() ?? "" : "";
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(restoredContentHash), expectedHash))
                throw new InvalidOperationException($"Restored NuGet package content hash does not match packages.lock.json: {package.Id} {package.Version}.");
            string rawHashPath = packageFile + ".sha512";
            string rawHashText = File.Exists(rawHashPath) ? File.ReadAllText(rawHashPath).Trim() : "";
            byte[] rawHash;
            try { rawHash = Convert.FromBase64String(rawHashText); }
            catch (FormatException) { throw new InvalidOperationException($"Raw NuGet archive hash is missing or malformed for {package.Id} {package.Version}."); }
            if (!CryptographicOperations.FixedTimeEquals(SHA512.HashData(archiveBytes), rawHash))
                throw new InvalidOperationException($"Cached NuGet archive checksum is invalid: {package.Id} {package.Version}.");
            records.Add(ReadPackage(root, package, archiveBytes, policy, rawHashText));
        }
        return records;
    }

    private static PackageRecord ReadPackage(string root, LockedPackage package, byte[] bytes, LicensePolicy policy, string rawArchiveSha512)
    {
        using var memory = new MemoryStream(bytes);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
        ZipArchiveEntry nuspecEntry = archive.Entries.SingleOrDefault(entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"NuGet package has no nuspec: {package.Id} {package.Version}.");
        XDocument nuspec;
        using (Stream stream = nuspecEntry.Open()) nuspec = XDocument.Load(stream);
        XElement metadata = nuspec.Descendants().FirstOrDefault(element => element.Name.LocalName == "metadata")
            ?? throw new InvalidOperationException($"NuGet package metadata is missing: {package.Id} {package.Version}.");
        XElement? licenseElement = metadata.Elements().FirstOrDefault(element => element.Name.LocalName == "license");
        string license = licenseElement?.Value.Trim() ?? "";
        string licenseType = licenseElement?.Attribute("type")?.Value ?? "";
        ReferenceOnlyPackage? reference = policy.ReferenceOnlyPackages.SingleOrDefault(candidate =>
            candidate.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase));
        UpstreamLegalOverride? upstreamOverride = (policy.UpstreamLegalOverrides ?? []).SingleOrDefault(candidate =>
            candidate.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase));
        UpstreamEvidence[] evidences = (policy.UpstreamEvidence ?? []).Where(candidate => candidate.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase)).ToArray();
        UpstreamEvidence? evidence = evidences.FirstOrDefault();
        bool referenceOnly = reference is not null;
        if (referenceOnly)
        {
            if (reference!.Version != package.Version || !licenseType.Equals("file", StringComparison.OrdinalIgnoreCase)
                || !license.Equals(reference.LicensePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Reviewed reference-only package metadata changed: {package.Id} {package.Version}.");
        }
        else if (!string.Equals(licenseType, "expression", StringComparison.OrdinalIgnoreCase) || !IsAllowedExpression(license, policy.AllowedLicenseIds))
            throw new InvalidOperationException($"Unsupported license expression for {package.Id} {package.Version}: {licenseType} {license}.");
        if (upstreamOverride is not null && (upstreamOverride.Version != package.Version || upstreamOverride.License != license))
            throw new InvalidOperationException($"Reviewed upstream legal material metadata changed: {package.Id} {package.Version}.");
        if (evidences.Any(item => item.Version != package.Version || item.License != license))
            throw new InvalidOperationException($"Reviewed upstream evidence metadata changed: {package.Id} {package.Version}.");
        string copyright = metadata.Elements().FirstOrDefault(element => element.Name.LocalName == "copyright")?.Value.Trim() ?? "";
        AssetManifest[] assets = archive.Entries.Where(IsPackageBinaryAsset).OrderBy(entry => entry.FullName, StringComparer.Ordinal)
            .Select(entry =>
            {
                using Stream assetStream = entry.Open();
                return new AssetManifest(package.Id, package.Version, entry.FullName,
                    Convert.ToHexString(SHA256.HashData(assetStream)).ToLowerInvariant());
            }).ToArray();
        if (referenceOnly)
        {
            ZipArchiveEntry licenseEntry = archive.Entries.SingleOrDefault(entry => entry.FullName.Equals(reference!.LicensePath, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Reviewed SNI license file is missing from {package.Id} {package.Version}.");
            using Stream licenseStream = licenseEntry.Open(); using var licenseBytes = new MemoryStream(); licenseStream.CopyTo(licenseBytes);
            string licenseHash = Convert.ToHexString(SHA256.HashData(licenseBytes.ToArray())).ToLowerInvariant();
            if (!licenseHash.Equals(reference!.LicenseSha256, StringComparison.Ordinal))
                throw new InvalidOperationException($"Reviewed reference-only license hash changed for {package.Id} {package.Version}; manual review is required.");
            if (string.IsNullOrWhiteSpace(copyright)) throw new InvalidOperationException($"Copyright notice is missing from {package.Id} {package.Version}.");
            return new PackageRecord(package.Id, package.Version, "REFERENCE_ONLY", package.ContentHash, rawArchiveSha512, copyright, "", [], assets);
        }

        var materialEntries = archive.Entries.Where(entry => IsLegalMaterial(entry.FullName)).OrderBy(entry => entry.FullName, StringComparer.Ordinal).ToArray();
        var materials = new List<Material>();
        foreach (ZipArchiveEntry entry in materialEntries)
        {
            string safeName = SafeRelative(entry.FullName);
            using Stream source = entry.Open(); using var output = new MemoryStream(); source.CopyTo(output);
            byte[] content = output.ToArray();
            if (content.Length == 0 || content.Length > 1_000_000) throw new InvalidOperationException($"Invalid legal material size in {package.Id} {package.Version}: {entry.FullName}.");
            string path = $"{MaterialsRoot}/{package.Id.ToLowerInvariant()}/{package.Version}/{safeName}";
            materials.Add(new Material(path, Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant(), content));
        }
        string copyrightReview = "";
        if (upstreamOverride is not null)
        {
            string sourcePath = SafeCombine(root, upstreamOverride.Path);
            if (!File.Exists(sourcePath)) throw new InvalidOperationException($"Reviewed upstream legal material is missing: {upstreamOverride.Path}.");
            byte[] content = File.ReadAllBytes(sourcePath);
            string actualHash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
            if (actualHash != upstreamOverride.Sha256) throw new InvalidOperationException($"Reviewed upstream legal material was modified: {upstreamOverride.Path}.");
            materials.RemoveAll(material => IsFullLicenseName(material.Path));
            materials.Add(new Material($"{MaterialsRoot}/{package.Id.ToLowerInvariant()}/{package.Version}/LICENSE", actualHash, content));
            copyrightReview = upstreamOverride.CopyrightReview;
        }
        foreach (UpstreamEvidence evidenceItem in evidences)
        {
            string evidencePath = SafeCombine(root, evidenceItem.Path);
            if (!File.Exists(evidencePath)) throw new InvalidOperationException($"Reviewed upstream evidence is missing: {evidenceItem.Path}.");
            byte[] evidenceBytes = File.ReadAllBytes(evidencePath);
            string evidenceHash = Convert.ToHexString(SHA256.HashData(evidenceBytes)).ToLowerInvariant();
            if (evidenceHash != evidenceItem.Sha256) throw new InvalidOperationException($"Reviewed upstream evidence was modified: {evidenceItem.Path}.");
            materials.Add(new Material($"{MaterialsRoot}/{package.Id.ToLowerInvariant()}/{package.Version}/{Path.GetFileName(evidenceItem.Path)}", evidenceHash, evidenceBytes));
            copyrightReview = string.Join(" ", evidences.Select(item => item.CopyrightReview));
        }
        if (string.IsNullOrWhiteSpace(copyright) && !materials.Any(file => HasConcreteCopyright(Encoding.UTF8.GetString(file.Content))) && upstreamOverride is null && evidence is null)
            throw new InvalidOperationException($"Copyright notice is missing from {package.Id} {package.Version}.");
        if (upstreamOverride is null && !materialEntries.Any(entry => IsFullLicenseName(entry.FullName)))
        {
            string? standardId = license.Split(new[] { " AND ", " OR " }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .SingleOrDefault(policy.StandardLicenseFallbacks.ContainsKey);
            if (standardId is null)
                throw new InvalidOperationException($"Required full license text is missing from {package.Id} {package.Version}.");
            StandardFallback fallback = policy.StandardLicenseFallbacks[standardId];
            string source = SafeCombine(root, fallback.Path);
            if (!File.Exists(source)) throw new InvalidOperationException($"Pinned standard license fallback is missing: {fallback.Path}.");
            byte[] content = File.ReadAllBytes(source);
            string actualHash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
            if (!actualHash.Equals(fallback.Sha256, StringComparison.Ordinal)) throw new InvalidOperationException($"Pinned standard license fallback was modified: {fallback.Path}.");
            string targetPath = $"{MaterialsRoot}/{package.Id.ToLowerInvariant()}/{package.Version}/LICENSE.txt";
            materials.Add(new Material(targetPath, actualHash, content));
        }
        return new PackageRecord(package.Id, package.Version, license, package.ContentHash, rawArchiveSha512, copyright, copyrightReview, materials, assets);
    }

    private static void VerifyMaterials(string root, IReadOnlyCollection<PackageRecord> packages)
    {
        foreach (PackageRecord package in packages)
        foreach (Material material in package.Materials)
        {
            string path = SafeCombine(root, material.Path);
            if (!File.Exists(path)) throw new InvalidOperationException($"Required third-party legal material is missing: {material.Path}.");
            string actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(actual), Convert.FromHexString(material.Sha256)))
                throw new InvalidOperationException($"Third-party legal material was modified: {material.Path}.");
        }
    }

    private static void CheckPackages(string root, string directory)
    {
        Run(root, check: true);
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException($"Package directory does not exist: {directory}.");
        string version = ReleaseVersion.Validate(root);
        string[] expected = ReleaseVersion.PackageProjects.Select(name => name + "." + version + ".nupkg").Order(StringComparer.Ordinal).ToArray();
        string[] actual = Directory.GetFiles(directory, "*.nupkg").Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray()!;
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal)) throw new InvalidOperationException("Package directory must contain exactly the 13 allowlisted release nupkgs.");
        Inventory inventory = JsonSerializer.Deserialize<Inventory>(File.ReadAllText(Path.Combine(root, InventoryPath)), JsonOptions)
            ?? throw new InvalidOperationException("Committed dependency inventory is invalid.");
        AssetManifest[] forbiddenAssets = inventory.Packages.Where(package => package.License == "REFERENCE_ONLY")
            .SelectMany(package => package.Assets).ToArray();
        foreach (string file in Directory.GetFiles(directory, "*.nupkg").Order(StringComparer.Ordinal))
        {
            using var archive = ZipFile.OpenRead(file);
            string[] names = archive.Entries.Select(entry => entry.FullName).ToArray();
            if (names.Any(name => name.Contains("Microsoft.Data.SqlClient.SNI", StringComparison.OrdinalIgnoreCase) || name.EndsWith("SNI.dll", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"SNI native/runtime binaries are forbidden in release packages: {Path.GetFileName(file)}.");
            var packageBinaries = archive.Entries.Where(entry => IsPackageBinaryPath(entry.FullName)).ToArray();
            string[] restricted = packageBinaries.Where(entry => forbiddenAssets.Any(asset =>
                Path.GetFileName(asset.PackagePath).Equals(Path.GetFileName(entry.FullName), StringComparison.OrdinalIgnoreCase)))
                .Select(entry => entry.FullName).ToArray();
            if (restricted.Length != 0)
                throw new InvalidOperationException($"Release package contains binaries with restricted redistribution terms: {Path.GetFileName(file)} ({Path.GetFileName(restricted[0])}).");
            foreach (string required in new[] { "LICENSE", "COPYING", "THIRD-PARTY-NOTICES.md" })
            {
                string source = Path.Combine(root, required);
                if (!File.Exists(source)) throw new InvalidOperationException($"Repository project legal material is missing: {required}.");
                ZipArchiveEntry entry = archive.Entries.SingleOrDefault(candidate => candidate.FullName.Equals(required, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"Release package is missing required project legal material: {Path.GetFileName(file)} ({required}).");
                string expectedHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))).ToLowerInvariant();
                if (!HashEntry(entry).Equals(expectedHash, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Release package project legal material differs from the repository: {Path.GetFileName(file)} ({required}).");
            }
            string projectLicense = Encoding.UTF8.GetString(ReadEntry(archive, "LICENSE"));
            string projectNotices = Encoding.UTF8.GetString(ReadEntry(archive, "THIRD-PARTY-NOTICES.md"));
            if (!projectLicense.Contains("GNU GENERAL PUBLIC LICENSE", StringComparison.Ordinal)
                || !projectLicense.Contains("Lesser", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Release package does not carry the project LGPL text: {Path.GetFileName(file)}.");
            if (!projectNotices.Contains("PyAfipWs", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Release package is missing PyAfipWs attribution: {Path.GetFileName(file)}.");
            if (Path.GetFileName(file).StartsWith("NetArcaWs.Rendering.Pdf.", StringComparison.Ordinal))
            {
                ZipArchiveEntry ofl = archive.Entries.SingleOrDefault(entry => entry.FullName.Equals("licenses/NotoSans-OFL.txt", StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("PDF package is missing the Noto Sans OFL license text.");
                using Stream stream = ofl.Open(); using var oflText = new MemoryStream(); stream.CopyTo(oflText);
                if (!Encoding.UTF8.GetString(oflText.ToArray()).Contains("SIL OPEN FONT LICENSE", StringComparison.Ordinal))
                    throw new InvalidOperationException("PDF package Noto Sans OFL license text is invalid.");
                string sourceOfl = Path.Combine(root, "src", "NetArcaWs.Rendering.Pdf", "Resources", "OFL.txt");
                if (!File.Exists(sourceOfl) || !HashEntry(ofl).Equals(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourceOfl))).ToLowerInvariant(), StringComparison.Ordinal))
                    throw new InvalidOperationException("PDF package Noto Sans OFL file differs from repository source.");
            }
            if (!Path.GetFileName(file).StartsWith("NetArcaWs.Tool.", StringComparison.Ordinal)) continue;
            foreach (string required in new[] { InventoryPath, "licenses/policy.json" })
            {
                ZipArchiveEntry entry = archive.Entries.SingleOrDefault(candidate => candidate.FullName.Equals(required, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"Tool package is missing required legal material: {required}.");
                string source = Path.Combine(root, required);
                string expectedHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))).ToLowerInvariant();
                if (!HashEntry(entry).Equals(expectedHash, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Tool package does not contain current licensing evidence: {required}.");
            }
            if (!names.Any(name => name.StartsWith(MaterialsRoot + "/", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Tool package is missing dependency legal materials.");
            string[] binaries = names.Where(name => name.StartsWith("tools/", StringComparison.OrdinalIgnoreCase)
                && (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".so", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))).ToArray();
            if (binaries.Length == 0 || !binaries.Any(name => name.Contains("NetArcaWs.Tool", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Tool package has no recognizable managed application binary.");
            AssetManifest[] knownBinaries = inventory.Packages.SelectMany(package => package.Assets).ToArray();
            foreach (string binary in binaries)
            {
                string name = Path.GetFileName(binary);
                if (name.Equals("NetArcaWs.Tool.dll", StringComparison.OrdinalIgnoreCase) || name.Equals("NetArcaWs.dll", StringComparison.OrdinalIgnoreCase)) continue;
                ZipArchiveEntry entry = archive.GetEntry(binary)!;
                string hash = HashEntry(entry);
                if (!knownBinaries.Any(asset => Path.GetFileName(asset.PackagePath).Equals(name, StringComparison.OrdinalIgnoreCase)
                                                && asset.Sha256.Equals(hash, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"Tool binary has no matching locked NuGet package asset: {binary}.");
            }
            foreach (PackageManifest package in inventory.Packages)
            foreach (MaterialManifest material in package.LegalFiles)
            {
                string bundledPath = material.Path;
                ZipArchiveEntry bundled = archive.Entries.SingleOrDefault(entry => entry.FullName.Equals(bundledPath, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException($"Tool package is missing required third-party material: {bundledPath}.");
                using Stream stream = bundled.Open();
                string hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
                if (!hash.Equals(material.Sha256, StringComparison.Ordinal)) throw new InvalidOperationException($"Tool package modified third-party material: {bundledPath}.");
            }
        }
    }

    private static byte[] ReadEntry(ZipArchive archive, string name)
    {
        ZipArchiveEntry entry = archive.Entries.Single(candidate => candidate.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));
        using Stream stream = entry.Open(); using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray();
    }

    private static bool IsAllowedExpression(string expression, HashSet<string> allowedLicenses)
    {
        if (string.IsNullOrWhiteSpace(expression)) return false;
        string[] tokens = Regex.Split(expression, @"\s+(?:AND|OR)\s+|[()]", RegexOptions.IgnoreCase).Where(token => token.Length > 0).ToArray();
        return tokens.Length > 0 && tokens.All(token => allowedLicenses.Contains(token.Trim()));
    }

    private static bool IsFullLicenseName(string path) => Regex.IsMatch(Path.GetFileName(path), @"^(LICENSE|LICENCE|COPYING)(\..*)?$", RegexOptions.IgnoreCase);
    private static bool HasConcreteCopyright(string text) => text.Contains("copyright", StringComparison.OrdinalIgnoreCase)
        && !Regex.IsMatch(text, @"<\s*(?:year|name|copyright|holder)", RegexOptions.IgnoreCase)
        && !text.Contains("<year>", StringComparison.OrdinalIgnoreCase);
    private static bool IsPackageBinaryAsset(ZipArchiveEntry entry) =>
        (entry.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) || entry.FullName.StartsWith("runtimes/", StringComparison.OrdinalIgnoreCase))
        && (entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || entry.FullName.EndsWith(".so", StringComparison.OrdinalIgnoreCase)
            || entry.FullName.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase) || entry.FullName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
    private static bool IsPackageBinaryPath(string path) => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".so", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
    private static string HashEntry(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static LicensePolicy ReadPolicy(string root)
    {
        string path = Path.Combine(root, "licenses", "policy.json");
        if (!File.Exists(path)) throw new InvalidOperationException("License policy is missing: licenses/policy.json.");
        LicensePolicy? policy = JsonSerializer.Deserialize<LicensePolicy>(File.ReadAllText(path), JsonOptions);
        if (policy is null || policy.SchemaVersion != 1 || policy.AllowedLicenseIds is null || policy.ReferenceOnlyPackages is null || policy.StandardLicenseFallbacks is null)
            throw new InvalidOperationException("License policy is invalid.");
        foreach (StandardFallback fallback in policy.StandardLicenseFallbacks.Values)
        {
            string fallbackPath = SafeCombine(root, fallback.Path);
            if (!File.Exists(fallbackPath) || Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fallbackPath))).ToLowerInvariant() != fallback.Sha256)
                throw new InvalidOperationException($"Pinned standard license fallback is missing or modified: {fallback.Path}.");
        }
        foreach (UpstreamLegalOverride overrideEntry in policy.UpstreamLegalOverrides ?? [])
        {
            string overridePath = SafeCombine(root, overrideEntry.Path);
            if (!File.Exists(overridePath) || Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(overridePath))).ToLowerInvariant() != overrideEntry.Sha256)
                throw new InvalidOperationException($"Pinned upstream legal material is missing or modified: {overrideEntry.Path}.");
        }
        foreach (UpstreamEvidence evidence in policy.UpstreamEvidence ?? [])
        {
            string evidencePath = SafeCombine(root, evidence.Path);
            if (!File.Exists(evidencePath) || Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(evidencePath))).ToLowerInvariant() != evidence.Sha256)
                throw new InvalidOperationException($"Pinned upstream evidence is missing or modified: {evidence.Path}.");
        }
        return policy with { AllowedLicenseIds = new HashSet<string>(policy.AllowedLicenseIds, StringComparer.Ordinal) };
    }

    private static bool IsLegalMaterial(string path)
    {
        string name = Path.GetFileName(path);
        return Regex.IsMatch(name, @"^(LICENSE|LICENCE|COPYING|NOTICE|COPYRIGHT)(\..*)?$", RegexOptions.IgnoreCase)
            || new string(Path.GetFileNameWithoutExtension(name).Where(char.IsLetterOrDigit).ToArray())
                .Equals("THIRDPARTYNOTICES", StringComparison.OrdinalIgnoreCase);
    }

    private static string SafeRelative(string path)
    {
        string normalized = path.Replace('\\', '/');
        if (Path.IsPathRooted(normalized) || normalized.Split('/').Any(part => part is ".." or "." or "")) throw new InvalidOperationException($"Unsafe path in NuGet legal material: {path}.");
        return normalized;
    }

    private static string SafeCombine(string root, string relative)
    {
        string full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new InvalidOperationException($"Unsafe inventory path: {relative}.");
        return full;
    }

    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
    private sealed record LockedPackage(string Id, string Version, string ContentHash);
    private sealed record StandardFallback(string Path, string Sha256, string Source);
    private sealed record ReferenceOnlyPackage(string Id, string Version, string LicensePath, string LicenseSha256, string Source, string Reason);
    private sealed record UpstreamLegalOverride(string Id, string Version, string License, string Path, string Sha256, string Source, string CopyrightReview);
    private sealed record UpstreamEvidence(string Id, string Version, string License, string Path, string Sha256, string Source, string CopyrightReview);
    private sealed record LicensePolicy(int SchemaVersion, HashSet<string> AllowedLicenseIds,
        Dictionary<string, StandardFallback> StandardLicenseFallbacks, ReferenceOnlyPackage[] ReferenceOnlyPackages, UpstreamLegalOverride[] UpstreamLegalOverrides, UpstreamEvidence[] UpstreamEvidence);
    private sealed record Material(string Path, string Sha256, byte[] Content);
    private sealed record AssetManifest(string PackageId, string PackageVersion, string PackagePath, string Sha256);
    private sealed record PackageRecord(string Id, string Version, string License, string ContentHash, string RawArchiveSha512, string Copyright, string CopyrightReview, List<Material> Materials, AssetManifest[] Assets)
    {
        public PackageManifest ToManifest() => new(Id, Version, License, ContentHash, RawArchiveSha512, Copyright, CopyrightReview,
            Materials.Select(file => new MaterialManifest(file.Path, file.Sha256)).ToArray(), Assets);
    }
    private sealed record MaterialManifest(string Path, string Sha256);
    private sealed record PackageManifest(string Id, string Version, string License, string ContentHash, string RawArchiveSha512, string Copyright, string CopyrightReview, MaterialManifest[] LegalFiles, AssetManifest[] Assets);
    private sealed record Inventory(int SchemaVersion, PackageManifest[] Packages);
}
