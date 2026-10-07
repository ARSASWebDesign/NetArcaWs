using Microsoft.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.MySql;
using NetArcaWs.HealthChecks;

namespace NetArcaWs.Build;

public sealed record PersistenceSchemaCheckResult(bool IsCurrent, IReadOnlyList<string> Drifted);

/// <summary>Generates provider DDL directly from the optional persistence EF model without connecting to a database.</summary>
public static class PersistenceSchema
{
    private const string RelativeDirectory = "docs/reference/persistence";

    public static IReadOnlyDictionary<string, string> GenerateScripts()
    {
        string[] providers = ["sqlite", "mysql", "mariadb", "postgresql", "sqlserver"];
        string[] selections = ["invoicing", "wsaa-tickets", "tenant-certificates", "invoice-recovery", "all"];
        var scripts = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (string provider in providers)
        foreach (string selection in selections)
        {
            NetArcaWsModelOptions model = CreateSelection(selection);
            string script = NormalizeNewlines(GenerateScript(provider, model)).TrimEnd();
            scripts.Add($"{provider}/{selection}.sql", $"-- Generated from the optional NetArcaWs EF Core model. Do not edit by hand.\n-- Provider: {provider}; selection: {selection}.\n{script}\n");
        }
        return scripts;
    }

    public static PersistenceSchemaCheckResult Check(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        string directory = Path.Combine(root, RelativeDirectory);
        var drifted = new List<string>();
        IReadOnlyDictionary<string, string> expected = GenerateScripts();
        foreach ((string relative, string contents) in expected)
        {
            string path = Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path) || !string.Equals(File.ReadAllText(path), contents, StringComparison.Ordinal))
                drifted.Add(relative);
        }

        if (Directory.Exists(directory))
        {
            var expectedPaths = expected.Keys.ToHashSet(StringComparer.Ordinal);
            foreach (string path in Directory.EnumerateFiles(directory, "*.sql", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(directory, path).Replace(Path.DirectorySeparatorChar, '/');
                if (!expectedPaths.Contains(relative)) drifted.Add(relative);
            }
        }
        return new PersistenceSchemaCheckResult(drifted.Count == 0, drifted.Order(StringComparer.Ordinal).ToArray());
    }

    public static void Run(string root, bool check)
    {
        PersistenceSchemaCheckResult result = Check(root);
        if (check)
        {
            if (!result.IsCurrent)
                throw new InvalidOperationException("Persistence schema drift detected: " + string.Join(", ", result.Drifted));
            Console.WriteLine("Persistence schema scripts are current (25 provider/selection combinations).");
            return;
        }

        IReadOnlyDictionary<string, string> scripts = GenerateScripts();
        string directory = Path.Combine(root, RelativeDirectory);
        foreach ((string relative, string contents) in scripts)
        {
            string path = Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
        }
        Console.WriteLine($"Generated {scripts.Count} persistence schema scripts under {RelativeDirectory}.");
    }

    private static NetArcaWsModelOptions CreateSelection(string selection) => selection switch
    {
        "invoicing" => NetArcaWsModelOptions.Configure(builder =>
            builder.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)),
        "wsaa-tickets" => NetArcaWsModelOptions.Configure(builder =>
            builder.AddWsaaTickets(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)),
        "tenant-certificates" => NetArcaWsModelOptions.Configure(builder => builder.AddCertificates()),
        "invoice-recovery" => NetArcaWsModelOptions.Configure(builder => builder.AddInvoiceRecovery(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)),
        "all" => NetArcaWsModelOptions.Configure(builder =>
            builder.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)
                .AddWsaaTickets(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)
                .AddCertificates()
                .AddInvoiceRecovery(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)),
        _ => throw new ArgumentOutOfRangeException(nameof(selection))
    };

    private static string GenerateScript(string provider, NetArcaWsModelOptions selection)
    {
        DbContextOptionsBuilder<ArcaWsDbContext> builder = new();
        switch (provider)
        {
            case "sqlite": builder.UseSqlite("Data Source=:memory:"); break;
            case "mysql": builder.UseMySql("Server=localhost;Database=schema_only", new MySqlServerVersion(new Version(8, 4, 11))); break;
            case "mariadb": builder.UseMySql("Server=localhost;Database=schema_only", new MariaDbServerVersion(new Version(11, 4, 13))); break;
            case "postgresql": builder.UseNpgsql("Host=localhost;Database=schema_only"); break;
            case "sqlserver": builder.UseSqlServer("Server=localhost;Database=schema_only;Encrypt=True;TrustServerCertificate=True"); break;
            default: throw new ArgumentOutOfRangeException(nameof(provider));
        }
        using ArcaWsDbContext context = new(builder.Options, selection);
        return context.Database.GenerateCreateScript();
    }

    private static string NormalizeNewlines(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal)
        .Replace('\r', '\n');
}
