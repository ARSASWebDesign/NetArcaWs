using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NetArcaWs.Build;

public static class WikiMirror
{
    private const string Repository = "https://github.com/ARSASWebDesign/NetArcaWs";
    private static readonly Regex LinkRegex = new(@"(?<prefix>!?\[[^\]]*\]\()(?<target>[^)\s]+)(?<suffix>[^)]*\))", RegexOptions.Compiled);
    private static readonly Regex WikiLinkRegex = new(@"\[\[(?<target>.+?)\]\]", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex ManifestPageRegex = new("- `([^`]+)` → `([^`]+)`", RegexOptions.Compiled);
    private static readonly Regex NonSlugCharacters = new("[^a-z0-9]+", RegexOptions.Compiled);

    public static void Run(string root)
    {
        var repositoryRoot = Path.GetFullPath(root);
        EnsureNotReparsePoint(repositoryRoot);
        var output = Path.Combine(repositoryRoot, "docs", "wiki-export");
        EnsureOutputPathIsSafe(repositoryRoot, output);
        var sources = EnumerateMarkdown(repositoryRoot, output)
            .OrderBy(path => Path.GetRelativePath(repositoryRoot, path).Replace(Path.DirectorySeparatorChar, '/'), StringComparer.Ordinal)
            .ToArray();

        var names = sources.ToDictionary(path => path, path => PageName(repositoryRoot, path), StringComparer.Ordinal);
        var collisions = names.GroupBy(item => item.Value, StringComparer.Ordinal).Where(group => group.Count() > 1).ToArray();
        if (collisions.Length > 0)
        {
            var details = string.Join("; ", collisions.Select(group =>
                $"{group.Key}: {string.Join(", ", group.Select(item => item.Key))}"));
            throw new InvalidOperationException($"Wiki page name collision: {details}");
        }

        if (Directory.Exists(output))
        {
            var manifest = Path.Combine(output, "MIRROR-SOURCES.md");
            if (File.Exists(manifest) && !IsReparsePoint(manifest))
            {
                var oldManifest = File.ReadAllText(manifest, Encoding.UTF8);
                foreach (Match match in ManifestPageRegex.Matches(oldManifest))
                {
                    var sourceRelative = match.Groups[1].Value;
                    var pageName = match.Groups[2].Value;
                    if (!IsSimpleRelativePath(sourceRelative) || !IsSimpleFileName(pageName))
                        continue;

                    var sourcePath = Path.GetFullPath(Path.Combine(repositoryRoot, sourceRelative.Replace('/', Path.DirectorySeparatorChar)));
                    if (!IsWithin(repositoryRoot, sourcePath) || !string.Equals(PageName(repositoryRoot, sourcePath), pageName, StringComparison.Ordinal))
                        continue;

                    var oldPage = Path.GetFullPath(Path.Combine(output, pageName + ".md"));
                    if (IsWithin(output, oldPage) && File.Exists(oldPage) && !IsReparsePoint(oldPage))
                        File.Delete(oldPage);
                }
            }

            DeleteIfExists(Path.Combine(output, "_Sidebar.md"));
            DeleteIfExists(Path.Combine(output, "MIRROR-SOURCES.md"));
        }

        Directory.CreateDirectory(output);
        var pageLookup = names.Values.Distinct(StringComparer.Ordinal)
            .ToDictionary(name => Normalize(name), name => name, StringComparer.Ordinal);

        foreach (var (source, name) in names)
        {
            var destination = Path.Combine(output, name + ".md");
            EnsureNotReparsePoint(destination);
            var relative = RelativeUnix(repositoryRoot, source);
            var sourceHeader = $"<!-- Source: {relative}. Generated wiki mirror; edit the repository source. -->\n\n";
            var content = sourceHeader + Render(repositoryRoot, source, File.ReadAllText(source, Encoding.UTF8), names, pageLookup);
            File.WriteAllText(destination, content, new UTF8Encoding(false));
        }

        var sidebar = new List<string> { "# Pages", "", "- [Inicio](Home)" };
        sidebar.AddRange(names.Values.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal)
            .Where(name => name != "Home")
            .Select(name => $"- [{name}]({Quote(name, "-_.")})"));
        var sidebarPath = Path.Combine(output, "_Sidebar.md");
        EnsureNotReparsePoint(sidebarPath);
        File.WriteAllText(sidebarPath, string.Join('\n', sidebar) + "\n", new UTF8Encoding(false));

        var manifestBody = new StringBuilder()
            .AppendLine("# Fuentes del mirror local")
            .AppendLine()
            .Append("Generado por `dotnet run --project tools/NetArcaWs.Build -- wiki`: ")
            .Append(names.Count)
            .Append(" páginas de contenido, más `_Sidebar.md` y este manifiesto. Cada página incluye ")
            .Append("su fuente y reescribe enlaces relativos para navegación de wiki. Los ")
            .Append("contratos ARCA archivados en el repositorio tienen fecha de snapshot ")
            .Append("2026-10-06. La salida refleja el árbol de trabajo. Al publicar, ")
            .AppendLine("SOURCE_COMMIT en el repositorio wiki registra el SHA de origen. Revisar el mirror antes de publicar.")
            .AppendLine();
        foreach (var (source, name) in names)
            manifestBody.Append("- `").Append(RelativeUnix(repositoryRoot, source)).Append("` → `").Append(name).AppendLine("`");
        var newManifest = Path.Combine(output, "MIRROR-SOURCES.md");
        EnsureNotReparsePoint(newManifest);
        File.WriteAllText(newManifest, manifestBody.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));

        var knownPages = names.Values.ToHashSet(StringComparer.Ordinal);
        knownPages.Add("_Sidebar");
        knownPages.Add("MIRROR-SOURCES");
        var broken = new List<string>();
        foreach (var generated in Directory.EnumerateFiles(output, "*.md", SearchOption.TopDirectoryOnly).Where(path => !IsReparsePoint(path)))
        {
            var body = File.ReadAllText(generated, Encoding.UTF8);
            foreach (Match match in LinkRegex.Matches(body))
            {
                var uri = ParseTarget(match.Groups["target"].Value);
                if (uri.Scheme.Length > 0 || uri.Host.Length > 0 || uri.Path.Length == 0)
                    continue;
                var destination = Unquote(uri.Path);
                if (destination.EndsWith(".md", StringComparison.Ordinal))
                    destination = destination[..^3];
                if (!knownPages.Contains(destination))
                    broken.Add($"{Path.GetFileName(generated)}: {match.Groups["target"].Value}");
            }
        }
        if (broken.Count > 0)
            throw new InvalidOperationException("Unresolved wiki links:\n" + string.Join('\n', broken));
    }

    private static bool IsSource(string root, string output, string path)
    {
        if (IsWithin(output, path))
            return false;
        var relative = Path.GetRelativePath(root, path);
        return !relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part is ".git" or "artifacts" or "bin" or "obj");
    }

    private static IEnumerable<string> EnumerateMarkdown(string root, string output)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var file in Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly))
                if (!IsReparsePoint(file) && IsSource(root, output, file))
                    yield return file;

            foreach (var child in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(child);
                if (name is ".git" or "artifacts" or "bin" or "obj" || IsWithin(output, child) || IsReparsePoint(child))
                    continue;
                pending.Push(child);
            }
        }
    }

    private static string PageName(string root, string source)
    {
        var relative = RelativeUnix(root, source);
        var stem = Path.GetFileNameWithoutExtension(source);
        if (relative == "README.md") return "Proyecto";
        if (relative == "ARCHITECTURE.md") return "Arquitectura-general";
        if (relative == "PROGRESS.md") return "Estado-del-proyecto";
        if (relative == "CONTRIBUTING.md") return "Contribuir";
        if (relative == "THIRD-PARTY-NOTICES.md") return "Avisos-de-terceros";
        if (relative.StartsWith("docs/wiki/", StringComparison.Ordinal)) return stem;
        if (relative.StartsWith("docs/adr/", StringComparison.Ordinal)) return $"ADR-{stem}";
        if (relative == "docs/releases.md") return "Publicar-versiones";
        if (relative == "docs/certificates-cli.md") return "CLI-certificados";
        if (relative.StartsWith("docs/plans/", StringComparison.Ordinal)) return $"Plan-{stem}";
        if (relative.StartsWith("docs/reference/", StringComparison.Ordinal)) return $"Referencia-{stem}";
        return $"Documento-{stem}";
    }

    private static string Render(string root, string source, string text, IReadOnlyDictionary<string, string> names, IReadOnlyDictionary<string, string> pageLookup)
    {
        text = LinkRegex.Replace(text, match =>
        {
            var target = match.Groups["target"].Value;
            var image = match.Groups["prefix"].Value.StartsWith('!');
            return match.Groups["prefix"].Value + RewriteLink(root, source, target, names, image) + match.Groups["suffix"].Value;
        });
        return WikiLinkRegex.Replace(text, match =>
        {
            var label = string.Join(' ', match.Groups["target"].Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return pageLookup.TryGetValue(Normalize(label), out var target)
                ? $"[{label}]({Quote(target, "-_.")})"
                : label;
        });
    }

    private static string RewriteLink(string root, string source, string target, IReadOnlyDictionary<string, string> names, bool image)
    {
        var uri = ParseTarget(target);
        if (uri.Scheme.Length > 0 || uri.Host.Length > 0 || uri.Path.Length == 0)
            return target;

        var sourceTarget = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, Unquote(uri.Path)));
        if (!IsWithin(root, sourceTarget))
            return target;
        if (names.TryGetValue(sourceTarget, out var page))
            return Quote(page, "-_.") + (uri.Fragment.Length > 0 ? "#" + uri.Fragment : "");

        var relative = RelativeUnix(root, sourceTarget);
        var url = $"{Repository}/{(image ? "raw" : "blob")}/main/{Quote(relative, "/-_.")}";
        return url + (uri.Fragment.Length > 0 ? "#" + uri.Fragment : "");
    }

    private static (string Scheme, string Host, string Path, string Fragment) ParseTarget(string target)
    {
        var fragmentIndex = target.IndexOf('#');
        var fragment = fragmentIndex < 0 ? "" : target[(fragmentIndex + 1)..];
        var beforeFragment = fragmentIndex < 0 ? target : target[..fragmentIndex];
        var queryIndex = beforeFragment.IndexOf('?');
        var withoutQuery = queryIndex < 0 ? beforeFragment : beforeFragment[..queryIndex];
        var schemeEnd = withoutQuery.IndexOf(':');
        var slash = withoutQuery.IndexOf('/');
        var scheme = schemeEnd > 0 && (slash < 0 || schemeEnd < slash) ? withoutQuery[..schemeEnd] : "";
        var host = "";
        var path = withoutQuery;
        if (withoutQuery.StartsWith("//", StringComparison.Ordinal))
        {
            var hostEnd = withoutQuery.IndexOf('/', 2);
            host = hostEnd < 0 ? withoutQuery[2..] : withoutQuery[2..hostEnd];
            path = hostEnd < 0 ? "" : withoutQuery[hostEnd..];
        }
        else if (scheme.Length > 0)
        {
            path = withoutQuery[(schemeEnd + 1)..];
        }
        return (scheme, host, path, fragment);
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormKD);
        var plain = new string(decomposed.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark).ToArray());
        return NonSlugCharacters.Replace(plain.ToLowerInvariant(), "-").Trim('-');
    }

    private static string Quote(string value, string safe)
    {
        const string unreserved = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_.~";
        var allowed = unreserved + safe;
        var result = new StringBuilder();
        foreach (var valueByte in Encoding.UTF8.GetBytes(value))
        {
            var character = (char)valueByte;
            if (valueByte < 128 && allowed.IndexOf(character) >= 0)
                result.Append(character);
            else
                result.Append('%').Append(valueByte.ToString("X2", CultureInfo.InvariantCulture));
        }
        return result.ToString();
    }

    private static string Unquote(string value) => Uri.UnescapeDataString(value);
    private static string RelativeUnix(string root, string path) => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
    private static bool IsWithin(string directory, string path)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(directory), Path.GetFullPath(path));
        return relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !Path.IsPathRooted(relative);
    }
    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static void EnsureOutputPathIsSafe(string root, string output)
    {
        var docs = Path.Combine(root, "docs");
        EnsureNotReparsePoint(docs);
        EnsureNotReparsePoint(output);
    }

    private static void EnsureNotReparsePoint(string path)
    {
        if (IsReparsePoint(path))
            throw new InvalidOperationException($"Wiki mirror path must not be a symbolic link or reparse point: {path}");
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
    }

    private static bool IsSimpleFileName(string value) =>
        value.Length > 0 && !Path.IsPathRooted(value) &&
        value != "." && value != ".." &&
        value.IndexOfAny(['/', '\\']) < 0;

    private static bool IsSimpleRelativePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value) || value.Contains('\\'))
            return false;
        return value.Split('/').All(part => part.Length > 0 && part is not "." and not "..");
    }
}
