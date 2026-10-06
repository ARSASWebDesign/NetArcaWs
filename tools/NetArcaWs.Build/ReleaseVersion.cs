using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NetArcaWs.Build;

public static partial class ReleaseVersion
{
    public static string Validate(string root, string? releaseTag = null, bool? prerelease = null)
    {
        var versions = new[] { "NetArcaWs", "NetArcaWs.Tool" }
            .Select(name => XDocument.Load(Path.Combine(root, "src", name, name + ".csproj"))
                .Root?.Elements("PropertyGroup").Elements("Version").SingleOrDefault()?.Value).ToArray();
        var version = versions[0] ?? "";
        var match = SemVer().Match(version);
        if (!match.Success || versions[1] != version ||
            match.Groups[4].Value.Split('.').Any(part => part.Length > 1 && part[0] == '0' && part.All(char.IsAsciiDigit)))
            throw new InvalidOperationException("Both projects must declare the same SemVer version (lowercase prerelease, no build metadata or numeric leading zeroes).");
        if (releaseTag is not null && releaseTag != "v" + version)
            throw new InvalidOperationException("Release tag must equal v followed by the version in both projects.");
        if (prerelease is not null && prerelease != match.Groups[4].Success)
            throw new InvalidOperationException("GitHub prerelease flag must match the NuGet version suffix.");
        return version;
    }

    [GeneratedRegex(@"\A(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9a-z-]+(?:\.[0-9a-z-]+)*))?\z")]
    private static partial Regex SemVer();
}
