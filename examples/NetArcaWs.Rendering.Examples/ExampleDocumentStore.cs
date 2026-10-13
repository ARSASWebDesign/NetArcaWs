using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using NetArcaWs.Documents;

namespace NetArcaWs.Rendering.Examples;

public static class ExampleDocumentStore
{
    private const string Marker = "netarcaws-rendering-samples-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Save(string databasePath, ComprobanteGenerado document, DatosEmisor issuer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(issuer);
        string? parent = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString());
        connection.Open();
        using SqliteCommand create = connection.CreateCommand();
        create.CommandText = "CREATE TABLE IF NOT EXISTS rendering_sample (id INTEGER PRIMARY KEY CHECK (id = 1), document_json TEXT NOT NULL, issuer_json TEXT NOT NULL);";
        create.ExecuteNonQuery();
        using SqliteCommand save = connection.CreateCommand();
        save.CommandText = "INSERT INTO rendering_sample(id, document_json, issuer_json) VALUES(1, $document, $issuer) ON CONFLICT(id) DO UPDATE SET document_json = excluded.document_json, issuer_json = excluded.issuer_json;";
        save.Parameters.AddWithValue("$document", JsonSerializer.Serialize(document, JsonOptions));
        save.Parameters.AddWithValue("$issuer", JsonSerializer.Serialize(issuer, JsonOptions));
        save.ExecuteNonQuery();
    }

    public static (ComprobanteGenerado Document, DatosEmisor Issuer) Load(string databasePath)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadOnly }.ToString());
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT document_json, issuer_json FROM rendering_sample WHERE id = 1;";
        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read()) throw new InvalidDataException("No existe una muestra persistida en la base SQLite.");
        ComprobanteGenerado document = JsonSerializer.Deserialize<ComprobanteGenerado>(reader.GetString(0), JsonOptions)
            ?? throw new InvalidDataException("El comprobante JSON persistido está vacío.");
        DatosEmisor issuer = JsonSerializer.Deserialize<DatosEmisor>(reader.GetString(1), JsonOptions)
            ?? throw new InvalidDataException("El perfil del emisor JSON persistido está vacío.");
        return (document, issuer);
    }

    public static string WriteRunManifest(string directory, DateTimeOffset generatedAtUtc, IReadOnlyList<SampleCase>? scenarios = null)
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "manifest.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new RunManifest(Marker, generatedAtUtc.ToUniversalTime(),
            scenarios?.Select(item => item.Id).ToArray() ?? []), JsonOptions));
        return path;
    }

    public static int CleanExpiredRuns(string root, DateTimeOffset nowUtc)
    {
        string fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot) || IsReparsePoint(fullRoot)) return 0;
        int removed = 0;
        foreach (string candidate in Directory.EnumerateDirectories(fullRoot))
        {
            string name = Path.GetFileName(candidate);
            if (!Regex.IsMatch(name, @"^\d{8}T\d{6}Z-[a-f0-9]{8}$", RegexOptions.CultureInvariant) ||
                IsReparsePoint(candidate) || ContainsReparsePoint(candidate)) continue;
            string manifestPath = Path.Combine(candidate, "manifest.json");
            if (!File.Exists(manifestPath)) continue;
            RunManifest? manifest;
            try { manifest = JsonSerializer.Deserialize<RunManifest>(File.ReadAllText(manifestPath), JsonOptions); }
            catch (JsonException) { continue; }
            if (manifest?.Marker != Marker || manifest.GeneratedAtUtc > nowUtc.ToUniversalTime().AddHours(-72)) continue;
            Directory.Delete(candidate, recursive: true);
            removed++;
        }
        return removed;
    }

    private static bool ContainsReparsePoint(string directory)
    {
        foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
        {
            if (IsReparsePoint(entry)) return true;
            if (Directory.Exists(entry) && ContainsReparsePoint(entry)) return true;
        }
        return false;
    }

    private static bool IsReparsePoint(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    private sealed record RunManifest(string Marker, DateTimeOffset GeneratedAtUtc, string[] Scenarios);
}
