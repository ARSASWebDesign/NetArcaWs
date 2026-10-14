using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Build.Tests;

public sealed class LicenseAuditTests
{
    [Fact]
    public void Update_writes_a_deterministic_inventory_from_locked_package_and_nupkg_metadata()
    {
        using var workspace = new BuildTestWorkspace();
        AddPackage(workspace, "Example.Dependency", "1.2.3", "MIT", "Example copyright", "MIT License\nPermission is hereby granted.");
        workspace.Write("src/Example/packages.lock.json", Lock("Example.Dependency", "1.2.3", Hash(workspace, "Example.Dependency", "1.2.3")));

        LicenseAudit.Run(workspace.Root, check: false);

        workspace.Read("licenses/dependencies.json").Should().Contain("Example.Dependency").And.Contain("MIT");
        workspace.Exists("licenses/third-party/example.dependency/1.2.3/license.txt").Should().BeTrue();
        workspace.Read("licenses/third-party/example.dependency/1.2.3/THIRD-PARTY-NOTICES.TXT").Should().Be("Copyright Example Notice.\n");
    }

    [Fact]
    public void Check_rejects_a_nupkg_that_does_not_match_its_locked_content_hash()
    {
        using var workspace = new BuildTestWorkspace();
        AddPackage(workspace, "Example.Dependency", "1.2.3", "MIT", "Example copyright", "MIT License");
        workspace.Write("src/Example/packages.lock.json", Lock("Example.Dependency", "1.2.3", Convert.ToBase64String(new byte[64])));

        Action check = () => LicenseAudit.Run(workspace.Root, check: true);

        check.Should().Throw<InvalidOperationException>().WithMessage("*content hash*");
    }

    [Fact]
    public void Update_accepts_NuGet_content_hash_that_differs_from_raw_archive_hash()
    {
        using var workspace = new BuildTestWorkspace();
        AddPackage(workspace, "Example.Dependency", "1.2.3", "MIT", "Example copyright", "MIT License\nCopyright 2026 Example");
        string package = Path.Combine(workspace.Root, ".nuget/packages/example.dependency/1.2.3/example.dependency.1.2.3.nupkg");
        string nugetContentHash = Convert.ToBase64String(new byte[64]);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(package)!, ".nupkg.metadata"),
            System.Text.Json.JsonSerializer.Serialize(new { contentHash = nugetContentHash }));
        workspace.Write("src/Example/packages.lock.json", Lock("Example.Dependency", "1.2.3", nugetContentHash));

        LicenseAudit.Run(workspace.Root, check: false);

        workspace.Exists("licenses/dependencies.json").Should().BeTrue();
    }

    [Fact]
    public void Check_rejects_a_corrupted_raw_archive_even_when_NuGet_metadata_matches_the_lock()
    {
        using var workspace = new BuildTestWorkspace();
        AddPackage(workspace, "Example.Dependency", "1.2.3", "MIT", "Example copyright", "MIT License\nCopyright 2026 Example");
        string package = Path.Combine(workspace.Root, ".nuget/packages/example.dependency/1.2.3/example.dependency.1.2.3.nupkg");
        string hash = Hash(workspace, "Example.Dependency", "1.2.3");
        workspace.Write("src/Example/packages.lock.json", Lock("Example.Dependency", "1.2.3", hash));
        LicenseAudit.Run(workspace.Root, check: false);
        File.AppendAllText(package, "tamper");

        Action check = () => LicenseAudit.Run(workspace.Root, check: true);

        check.Should().Throw<InvalidOperationException>().WithMessage("*archive checksum*");
    }

    [Fact]
    public void Update_rejects_unknown_license_metadata_instead_of_guessing()
    {
        using var workspace = new BuildTestWorkspace();
        AddPackage(workspace, "Example.Dependency", "1.2.3", "LicenseRef-Unknown", "Example copyright", "Unknown terms");
        workspace.Write("src/Example/packages.lock.json", Lock("Example.Dependency", "1.2.3", Hash(workspace, "Example.Dependency", "1.2.3")));

        Action update = () => LicenseAudit.Run(workspace.Root, check: false);

        update.Should().Throw<InvalidOperationException>().WithMessage("*unsupported license*");
    }

    [Fact]
    public void Update_rejects_NativeInterop_even_when_it_is_not_a_reference_only_inventory_entry()
    {
        using var workspace = new BuildTestWorkspace();
        AddPackage(workspace, "Microsoft.Identity.Client.NativeInterop", "0.20.6", "MIT", "Example copyright", "MIT License\nCopyright 2026 Example");
        workspace.Write("src/Example/packages.lock.json", Lock("Microsoft.Identity.Client.NativeInterop", "0.20.6", Hash(workspace, "Microsoft.Identity.Client.NativeInterop", "0.20.6")));

        Action update = () => LicenseAudit.Run(workspace.Root, check: false);

        update.Should().Throw<InvalidOperationException>().WithMessage("*NativeInterop*blocked*");
    }

    [Fact]
    public void Update_rejects_a_modified_version_pinned_upstream_license_override()
    {
        using var workspace = new BuildTestWorkspace();
        AddPackage(workspace, "PdfPig", "0.1.16", "Apache-2.0", "", "Apache License Version 2.0");
        string evidencePath = "licenses/upstream/pdfpig/0.1.16/LICENSE";
        workspace.Write(evidencePath, "pinned upstream license");
        workspace.Write("licenses/policy.json", """
            {"schemaVersion":1,"allowedLicenseIds":["Apache-2.0"],"standardLicenseFallbacks":{},"referenceOnlyPackages":[],
             "upstreamLegalOverrides":[{"id":"PdfPig","version":"0.1.16","license":"Apache-2.0","path":"licenses/upstream/pdfpig/0.1.16/LICENSE","sha256":"bad-hash","source":"fixture","copyrightReview":"reviewed absence"}]}
            """);
        workspace.Write("src/PdfPig/packages.lock.json", Lock("PdfPig", "0.1.16", Hash(workspace, "PdfPig", "0.1.16")));

        Action update = () => LicenseAudit.Run(workspace.Root, check: false);

        update.Should().Throw<InvalidOperationException>().WithMessage("*upstream legal material*modified*");
    }

    [Fact]
    public void Check_rejects_modified_committed_license_material()
    {
        using var workspace = new BuildTestWorkspace();
        AddPackage(workspace, "Example.Dependency", "1.2.3", "MIT", "Example copyright", "MIT License\nCopyright 2026 Example");
        workspace.Write("src/Example/packages.lock.json", Lock("Example.Dependency", "1.2.3", Hash(workspace, "Example.Dependency", "1.2.3")));
        LicenseAudit.Run(workspace.Root, check: false);
        workspace.Write("licenses/third-party/example.dependency/1.2.3/license.txt", "changed");

        Action check = () => LicenseAudit.Run(workspace.Root, check: true);

        check.Should().Throw<InvalidOperationException>().WithMessage("*modified*");
    }

    [Fact]
    public void Check_rejects_missing_committed_license_material()
    {
        using var workspace = new BuildTestWorkspace();
        AddPackage(workspace, "Example.Dependency", "1.2.3", "MIT", "Example copyright", "MIT License\nCopyright 2026 Example");
        workspace.Write("src/Example/packages.lock.json", Lock("Example.Dependency", "1.2.3", Hash(workspace, "Example.Dependency", "1.2.3")));
        LicenseAudit.Run(workspace.Root, check: false);
        File.Delete(Path.Combine(workspace.Root, "licenses/third-party/example.dependency/1.2.3/license.txt"));

        Action check = () => LicenseAudit.Run(workspace.Root, check: true);

        check.Should().Throw<InvalidOperationException>().WithMessage("*missing*");
    }

    [Fact]
    public void CheckPackages_rejects_an_SNI_binary_in_any_allowlisted_artifact()
    {
        using var workspace = new BuildTestWorkspace();
        AddPackage(workspace, "Example.Dependency", "1.2.3", "MIT", "Example copyright", "MIT License\nCopyright 2026 Example");
        workspace.Write("src/Example/packages.lock.json", Lock("Example.Dependency", "1.2.3", Hash(workspace, "Example.Dependency", "1.2.3")));
        LicenseAudit.Run(workspace.Root, check: false);
        string directory = Path.Combine(workspace.Root, "packages");
        Directory.CreateDirectory(directory);
        WriteReleaseProjects(workspace);
        const string projectLicense = "GNU GENERAL PUBLIC LICENSE Version 3\nLesser General Public License";
        const string projectNotices = "PyAfipWs copyright notice";
        workspace.Write("LICENSE", projectLicense);
        workspace.Write("COPYING", projectLicense);
        workspace.Write("THIRD-PARTY-NOTICES.md", projectNotices);
        workspace.Write("src/NetArcaWs.Rendering.Pdf/Resources/OFL.txt", "SIL OPEN FONT LICENSE Version 1.1");
        foreach (string project in ReleaseVersion.PackageProjects)
        {
            using var archive = ZipFile.Open(Path.Combine(directory, project + ".1.2.3.nupkg"), ZipArchiveMode.Create);
            AddEntry(archive, "LICENSE", projectLicense);
            AddEntry(archive, "COPYING", projectLicense);
            AddEntry(archive, "THIRD-PARTY-NOTICES.md", projectNotices);
            if (project == "NetArcaWs.Rendering.Pdf") AddEntry(archive, "licenses/NotoSans-OFL.txt", "SIL OPEN FONT LICENSE Version 1.1");
            if (project == "NetArcaWs.Tool")
            {
                AddEntry(archive, "licenses/dependencies.json", workspace.Read("licenses/dependencies.json"));
                AddEntry(archive, "licenses/policy.json", workspace.Read("licenses/policy.json"));
                AddEntry(archive, "licenses/third-party/example.dependency/1.2.3/license.txt", "MIT License\nCopyright 2026 Example");
                AddEntry(archive, "tools/net10/any/NetArcaWs.Tool.dll", "test application binary");
                AddEntry(archive, "tools/net10/any/Microsoft.Data.SqlClient.SNI.dll", "test restricted binary");
            }
        }
        Action check = () => LicenseAudit.Run(workspace.Root, check: true, directory);

        check.Should().Throw<InvalidOperationException>().WithMessage("*restricted runtime binaries*SNI*");
    }

    [Fact]
    public void CheckPackages_accepts_13_packages_with_deps_bound_binary_origins_and_exact_legal_materials()
    {
        using var workspace = new BuildTestWorkspace();
        string directory = CreateReleaseFixture(workspace);

        Action check = () => LicenseAudit.Run(workspace.Root, check: true, directory);

        check.Should().NotThrow();
    }

    [Theory]
    [InlineData("tools/net10.0/any/lib/Untracked.dll")]
    [InlineData("tools/net10.0/any/runtimes/linux-x64/native/Untracked.dll")]
    public void CheckPackages_rejects_an_unlisted_binary_anywhere_in_the_Tool_payload(string path)
    {
        using var workspace = new BuildTestWorkspace();
        string directory = CreateReleaseFixture(workspace, extraToolBinary: path);

        Action check = () => LicenseAudit.Run(workspace.Root, check: true, directory);

        check.Should().Throw<InvalidOperationException>().WithMessage("*deps.json*");
    }

    [Theory]
    [InlineData("lib/Untracked.dll")]
    [InlineData("runtimes/linux-x64/native/Untracked.dll")]
    public void CheckPackages_rejects_binaries_outside_the_Tool_payload(string path)
    {
        using var workspace = new BuildTestWorkspace();
        string directory = CreateReleaseFixture(workspace, extraToolBinary: path);

        Action check = () => LicenseAudit.Run(workspace.Root, check: true, directory);

        check.Should().Throw<InvalidOperationException>().WithMessage("*outside its installed Tool payload*");
    }

    [Fact]
    public void CheckPackages_rejects_a_known_asset_with_modified_content()
    {
        using var workspace = new BuildTestWorkspace();
        string directory = CreateReleaseFixture(workspace, dependencyBinaryContent: "tampered bytes");

        Action check = () => LicenseAudit.Run(workspace.Root, check: true, directory);

        check.Should().Throw<InvalidOperationException>().WithMessage("*exact locked package/version/asset origin*");
    }

    [Fact]
    public void CheckPackages_rejects_a_wrong_deps_identity_or_version()
    {
        using var workspace = new BuildTestWorkspace();
        string directory = CreateReleaseFixture(workspace, depsIdentity: "Example.Dependency/9.9.9");

        Action check = () => LicenseAudit.Run(workspace.Root, check: true, directory);

        check.Should().Throw<InvalidOperationException>().WithMessage("*deps.json package is absent*");
    }

    [Fact]
    public void CheckPackages_rejects_a_deps_asset_path_that_does_not_exist_in_the_locked_package()
    {
        using var workspace = new BuildTestWorkspace();
        string directory = CreateReleaseFixture(workspace, depsAssetPath: "lib/net10.0/Other.dll");

        Action check = () => LicenseAudit.Run(workspace.Root, check: true, directory);

        check.Should().Throw<InvalidOperationException>().WithMessage("*deps.json asset is absent*");
    }

    [Fact]
    public void CheckPackages_rejects_unsafe_zip_paths()
    {
        using var workspace = new BuildTestWorkspace();
        string directory = CreateReleaseFixture(workspace, extraToolBinary: "tools/net10.0/any/../escape.dll");

        Action check = () => LicenseAudit.Run(workspace.Root, check: true, directory);

        check.Should().Throw<InvalidOperationException>().WithMessage("*unsafe archive path*");
    }

    [Theory]
    [InlineData("Microsoft.Identity.Client.NativeInterop.dll")]
    [InlineData("msalruntime_x86.dll")]
    [InlineData("msalruntime.dll")]
    [InlineData("msalruntime_arm64.dll")]
    [InlineData("libmsalruntime.so")]
    [InlineData("msalruntime_arm64.dylib")]
    [InlineData("msalruntime.dylib")]
    public void CheckPackages_rejects_NativeInterop_binaries_even_when_absent_from_inventory(string binaryName)
    {
        using var workspace = new BuildTestWorkspace();
        string directory = CreateReleaseFixture(workspace, extraToolBinary: "tools/net10.0/any/runtimes/linux-x64/native/" + binaryName);

        Action check = () => LicenseAudit.Run(workspace.Root, check: true, directory);

        check.Should().Throw<InvalidOperationException>().WithMessage("*restricted runtime binaries*");
    }

    [Fact]
    public void CheckPackages_rejects_missing_or_modified_PDF_OFL_material()
    {
        using var missingWorkspace = new BuildTestWorkspace();
        string missingDirectory = CreateReleaseFixture(missingWorkspace, omitPdfOfl: true);
        Action missing = () => LicenseAudit.Run(missingWorkspace.Root, check: true, missingDirectory);
        missing.Should().Throw<InvalidOperationException>().WithMessage("*missing the Noto Sans OFL*");

        using var modifiedWorkspace = new BuildTestWorkspace();
        string modifiedDirectory = CreateReleaseFixture(modifiedWorkspace, pdfOflOverride: "tampered font terms");
        Action modified = () => LicenseAudit.Run(modifiedWorkspace.Root, check: true, modifiedDirectory);
        modified.Should().Throw<InvalidOperationException>().WithMessage("*OFL file differs from repository*");
    }

    [Fact]
    public void CheckPackages_rejects_project_license_and_bundled_third_party_material_tampering()
    {
        using var projectLicenseWorkspace = new BuildTestWorkspace();
        string projectLicenseDirectory = CreateReleaseFixture(projectLicenseWorkspace, tamperProjectLicense: true);
        Action projectLicense = () => LicenseAudit.Run(projectLicenseWorkspace.Root, check: true, projectLicenseDirectory);
        projectLicense.Should().Throw<InvalidOperationException>().WithMessage("*differs from the repository*");

        using var thirdPartyWorkspace = new BuildTestWorkspace();
        string thirdPartyDirectory = CreateReleaseFixture(thirdPartyWorkspace, tamperToolDependencyMaterial: true);
        Action thirdParty = () => LicenseAudit.Run(thirdPartyWorkspace.Root, check: true, thirdPartyDirectory);
        thirdParty.Should().Throw<InvalidOperationException>().WithMessage("*modified third-party material*");
    }

    private static string CreateReleaseFixture(BuildTestWorkspace workspace, string? extraToolBinary = null,
        string? dependencyBinaryContent = null, string depsIdentity = "Example.Dependency/1.2.3", bool omitPdfOfl = false,
        string? pdfOflOverride = null, bool tamperProjectLicense = false, bool tamperToolDependencyMaterial = false,
        string depsAssetPath = "lib/net10.0/Example.Dependency.dll")
    {
        const string dependencyBytes = "locked dependency bytes";
        AddPackage(workspace, "Example.Dependency", "1.2.3", "MIT", "Example copyright", "MIT License\nCopyright 2026 Example",
            "lib/net10.0/Example.Dependency.dll", dependencyBytes,
            "runtimes/browser-wasm/nativeassets/net9.0/example.a", "static bytes");
        workspace.Write("src/Example/packages.lock.json", Lock("Example.Dependency", "1.2.3", Hash(workspace, "Example.Dependency", "1.2.3")));
        LicenseAudit.Run(workspace.Root, check: false);

        string repository = FindRepositoryRoot();
        byte[] projectLicense = File.ReadAllBytes(Path.Combine(repository, "LICENSE"));
        byte[] projectCopying = File.ReadAllBytes(Path.Combine(repository, "COPYING"));
        byte[] projectNotices = File.ReadAllBytes(Path.Combine(repository, "THIRD-PARTY-NOTICES.md"));
        WriteBytes(workspace, "LICENSE", tamperProjectLicense ? Encoding.UTF8.GetBytes("GNU GENERAL PUBLIC LICENSE\nLesser altered") : projectLicense);
        WriteBytes(workspace, "COPYING", projectCopying);
        WriteBytes(workspace, "THIRD-PARTY-NOTICES.md", projectNotices);
        string oflPath = "src/NetArcaWs.Rendering.Pdf/Resources/OFL.txt";
        byte[] ofl = File.ReadAllBytes(Path.Combine(repository, oflPath));
        WriteBytes(workspace, oflPath, ofl);
        byte[] packagedOfl = pdfOflOverride is null ? ofl : Encoding.UTF8.GetBytes(pdfOflOverride);

        foreach (string project in ReleaseVersion.PackageProjects)
            workspace.Write($"src/{project}/{project}.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><Version>1.2.3</Version></PropertyGroup></Project>");
        string directory = Path.Combine(workspace.Root, "release-packages");
        Directory.CreateDirectory(directory);
        Inventory inventory = System.Text.Json.JsonSerializer.Deserialize<Inventory>(workspace.Read("licenses/dependencies.json"),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        string firstMaterialPath = inventory.Packages.SelectMany(package => package.LegalFiles)
            .Single(material => material.Path.EndsWith("THIRD-PARTY-NOTICES.TXT", StringComparison.OrdinalIgnoreCase)).Path;
        foreach (string project in ReleaseVersion.PackageProjects)
        {
            using var archive = ZipFile.Open(Path.Combine(directory, project + ".1.2.3.nupkg"), ZipArchiveMode.Create);
            AddEntryBytes(archive, "LICENSE", projectLicense);
            AddEntryBytes(archive, "COPYING", projectCopying);
            AddEntryBytes(archive, "THIRD-PARTY-NOTICES.md", projectNotices);
            if (project == "NetArcaWs.Rendering.Pdf" && !omitPdfOfl) AddEntryBytes(archive, "licenses/NotoSans-OFL.txt", packagedOfl);
            if (project != "NetArcaWs.Tool") continue;
            AddEntry(archive, "licenses/dependencies.json", workspace.Read("licenses/dependencies.json"));
            AddEntry(archive, "licenses/policy.json", workspace.Read("licenses/policy.json"));
            foreach (PackageManifest package in inventory.Packages)
            foreach (MaterialManifest material in package.LegalFiles)
            {
                byte[] contents = File.ReadAllBytes(Path.Combine(workspace.Root, material.Path));
                if (tamperToolDependencyMaterial && material.Path == firstMaterialPath) contents = Encoding.UTF8.GetBytes("modified legal text");
                AddEntryBytes(archive, material.Path, contents);
            }
            AddEntry(archive, "tools/net10.0/any/NetArcaWs.Tool.deps.json", DepsJson(depsIdentity, depsAssetPath));
            AddEntry(archive, "tools/net10.0/any/NetArcaWs.Tool.dll", "tool binary");
            AddEntry(archive, "tools/net10.0/any/NetArcaWs.dll", "project binary");
            AddEntry(archive, "tools/net10.0/any/Example.Dependency.dll", dependencyBinaryContent ?? dependencyBytes);
            AddEntry(archive, "tools/net10.0/any/runtimes/browser-wasm/nativeassets/net9.0/example.a", "static bytes");
            if (extraToolBinary is not null) AddEntry(archive, extraToolBinary, "untracked asset");
        }
        return directory;
    }

    private static string DepsJson(string packageIdentity, string assetPath) => System.Text.Json.JsonSerializer.Serialize(new
    {
        targets = new Dictionary<string, object>
        {
            [".NETCoreApp,Version=v10.0"] = new Dictionary<string, object>
            {
                [packageIdentity] = new
                {
                    runtime = new Dictionary<string, object> { [assetPath] = new { } },
                    runtimeTargets = new Dictionary<string, object>
                    {
                        ["runtimes/browser-wasm/nativeassets/net9.0/example.a"] = new { rid = "browser-wasm", assetType = "native" }
                    }
                }
            }
        },
        libraries = new Dictionary<string, object> { [packageIdentity] = new { type = "package" } }
    });

    private static string FindRepositoryRoot()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "NetArcaWs.slnx"))) return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the NetArcaWs repository root for exact legal-text fixtures.");
    }

    private static void AddEntryBytes(ZipArchive archive, string path, byte[] contents)
    {
        using var stream = archive.CreateEntry(path).Open();
        stream.Write(contents);
    }

    private static void WriteBytes(BuildTestWorkspace workspace, string relativePath, byte[] contents)
    {
        string path = Path.Combine(workspace.Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, contents);
    }

    private static string Lock(string id, string version, string hash) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            version = 1,
            dependencies = new Dictionary<string, object>
            {
                ["net10.0"] = new Dictionary<string, object>
                {
                    [id] = new { type = "Direct", requested = $"[{version}, )", resolved = version, contentHash = hash }
                }
            }
        });

    private static void WriteReleaseProjects(BuildTestWorkspace workspace)
    {
        foreach (string project in ReleaseVersion.PackageProjects)
            workspace.Write($"src/{project}/{project}.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><Version>1.2.3</Version></PropertyGroup></Project>");
    }

    private static void AddEntry(ZipArchive archive, string path, string contents)
    {
        using var stream = archive.CreateEntry(path).Open();
        stream.Write(Encoding.UTF8.GetBytes(contents));
    }

    private static void AddPackage(BuildTestWorkspace workspace, string id, string version, string license, string copyright, string text,
        string? binaryPath = null, string? binaryContent = null, string? binaryPath2 = null, string? binaryContent2 = null)
    {
        workspace.Write("licenses/policy.json", "{\"schemaVersion\":1,\"allowedLicenseIds\":[\"MIT\"],\"standardLicenseFallbacks\":{},\"referenceOnlyPackages\":[]}");
        string package = Path.Combine(workspace.Root, ".nuget", "packages", id.ToLowerInvariant(), version, $"{id.ToLowerInvariant()}.{version}.nupkg");
        Directory.CreateDirectory(Path.GetDirectoryName(package)!);
        using var archive = ZipFile.Open(package, ZipArchiveMode.Create);
        Write(archive, $"{id}.nuspec", $"<package><metadata><id>{id}</id><version>{version}</version><license type=\"expression\">{license}</license><copyright>{copyright}</copyright></metadata></package>");
        Write(archive, "license.txt", text);
        Write(archive, "THIRD-PARTY-NOTICES.TXT", "Copyright Example Notice.\n");
        if (binaryPath is not null) Write(archive, binaryPath, binaryContent ?? "package binary");
        if (binaryPath2 is not null) Write(archive, binaryPath2, binaryContent2 ?? "package binary");
        archive.Dispose();
        byte[] rawHash = SHA512.HashData(File.ReadAllBytes(package));
        File.WriteAllText(package + ".sha512", Convert.ToBase64String(rawHash));
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(package)!, ".nupkg.metadata"),
            System.Text.Json.JsonSerializer.Serialize(new { contentHash = Convert.ToBase64String(rawHash) }));
    }

    private static void Write(ZipArchive archive, string name, string text)
    {
        using var stream = archive.CreateEntry(name).Open();
        stream.Write(Encoding.UTF8.GetBytes(text));
    }

    private static string Hash(BuildTestWorkspace workspace, string id, string version)
    {
        string path = Path.Combine(workspace.Root, ".nuget", "packages", id.ToLowerInvariant(), version, $"{id.ToLowerInvariant()}.{version}.nupkg");
        return Convert.ToBase64String(SHA512.HashData(File.ReadAllBytes(path)));
    }

    private sealed record Inventory(PackageManifest[] Packages);
    private sealed record PackageManifest(MaterialManifest[] LegalFiles);
    private sealed record MaterialManifest(string Path, string Sha256);
}
