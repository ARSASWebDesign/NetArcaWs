using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NetArcaWs.Build;

public static partial class ReleaseVersion
{
    public static IReadOnlyList<string> PackageProjects { get; } =
    [
        "NetArcaWs",
        "NetArcaWs.Tool",
        "NetArcaWs.EntityFrameworkCore",
        "NetArcaWs.EntityFrameworkCore.MySql",
        "NetArcaWs.EntityFrameworkCore.PostgreSql",
        "NetArcaWs.EntityFrameworkCore.SqlServer",
        "NetArcaWs.EntityFrameworkCore.Migrations.Sqlite",
        "NetArcaWs.EntityFrameworkCore.Migrations.MySql",
        "NetArcaWs.EntityFrameworkCore.Migrations.MariaDb",
        "NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql",
        "NetArcaWs.EntityFrameworkCore.Migrations.SqlServer",
    ];

    public static string Validate(string root, string? releaseTag = null, bool? prerelease = null)
    {
        var projects = Directory.GetFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => Path.GetFileNameWithoutExtension(path)).Order(StringComparer.Ordinal).ToArray();
        var expected = PackageProjects.Order(StringComparer.Ordinal).ToArray();
        if (!projects.SequenceEqual(expected, StringComparer.Ordinal))
            throw new InvalidOperationException($"Packable project allowlist mismatch. Expected [{string.Join(", ", expected)}], found [{string.Join(", ", projects)}].");

        var versions = PackageProjects.Select(name =>
        {
            var path = Path.Combine(root, "src", name, name + ".csproj");
            if (!File.Exists(path))
                throw new InvalidOperationException($"Expected release project is missing: {name}.");
            return XDocument.Load(path).Root?.Elements("PropertyGroup").Elements("Version").SingleOrDefault()?.Value;
        }).ToArray();
        var version = versions[0] ?? "";
        var match = SemVer().Match(version);
        if (!match.Success || versions.Any(candidate => candidate != version) ||
            match.Groups[4].Value.Split('.').Any(part => part.Length > 1 && part[0] == '0' && part.All(char.IsAsciiDigit)))
            throw new InvalidOperationException("All eleven release projects must declare the same SemVer version (lowercase prerelease, no build metadata or numeric leading zeroes).");
        if (releaseTag is not null && releaseTag != "v" + version)
            throw new InvalidOperationException("Release tag must equal v followed by the version in all eleven projects.");
        if (prerelease is not null && prerelease != match.Groups[4].Success)
            throw new InvalidOperationException("GitHub prerelease flag must match the NuGet version suffix.");
        return version;
    }

    [GeneratedRegex(@"\A(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9a-z-]+(?:\.[0-9a-z-]+)*))?\z")]
    private static partial Regex SemVer();
}
