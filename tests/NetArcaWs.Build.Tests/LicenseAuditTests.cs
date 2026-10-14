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

        check.Should().Throw<InvalidOperationException>().WithMessage("*SNI*forbidden*");
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

    private static void AddPackage(BuildTestWorkspace workspace, string id, string version, string license, string copyright, string text)
    {
        workspace.Write("licenses/policy.json", "{\"schemaVersion\":1,\"allowedLicenseIds\":[\"MIT\"],\"standardLicenseFallbacks\":{},\"referenceOnlyPackages\":[]}");
        string package = Path.Combine(workspace.Root, ".nuget", "packages", id.ToLowerInvariant(), version, $"{id.ToLowerInvariant()}.{version}.nupkg");
        Directory.CreateDirectory(Path.GetDirectoryName(package)!);
        using var archive = ZipFile.Open(package, ZipArchiveMode.Create);
        Write(archive, $"{id}.nuspec", $"<package><metadata><id>{id}</id><version>{version}</version><license type=\"expression\">{license}</license><copyright>{copyright}</copyright></metadata></package>");
        Write(archive, "license.txt", text);
        Write(archive, "THIRD-PARTY-NOTICES.TXT", "Copyright Example Notice.\n");
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
}
