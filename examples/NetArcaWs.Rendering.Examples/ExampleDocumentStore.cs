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
        create.CommandText = "CREATE TABLE IF NOT EXISTS rendering_sample (id INTEGER PRIMARY KEY CHECK (id = 1), document_kind TEXT NOT NULL, document_json TEXT NOT NULL, issuer_json TEXT NOT NULL);";
        create.ExecuteNonQuery();
        using SqliteCommand save = connection.CreateCommand();
        save.CommandText = "INSERT INTO rendering_sample(id, document_kind, document_json, issuer_json) VALUES(1, 'invoice', $document, $issuer) ON CONFLICT(id) DO UPDATE SET document_kind = excluded.document_kind, document_json = excluded.document_json, issuer_json = excluded.issuer_json;";
        save.Parameters.AddWithValue("$document", JsonSerializer.Serialize(document, JsonOptions));
        save.Parameters.AddWithValue("$issuer", JsonSerializer.Serialize(issuer, JsonOptions));
        save.ExecuteNonQuery();
    }

    public static void Save(string databasePath, RemitoGenerado document, DatosEmisor issuer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(issuer);
        string? parent = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString());
        connection.Open();
        using SqliteCommand create = connection.CreateCommand();
        create.CommandText = "CREATE TABLE IF NOT EXISTS rendering_sample (id INTEGER PRIMARY KEY CHECK (id = 1), document_kind TEXT NOT NULL, document_json TEXT NOT NULL, issuer_json TEXT NOT NULL);";
        create.ExecuteNonQuery();
        using SqliteCommand save = connection.CreateCommand();
        save.CommandText = "INSERT INTO rendering_sample(id, document_kind, document_json, issuer_json) VALUES(1, 'remito', $document, $issuer) ON CONFLICT(id) DO UPDATE SET document_kind = excluded.document_kind, document_json = excluded.document_json, issuer_json = excluded.issuer_json;";
        save.Parameters.AddWithValue("$document", JsonSerializer.Serialize(document, JsonOptions));
        save.Parameters.AddWithValue("$issuer", JsonSerializer.Serialize(issuer, JsonOptions));
        save.ExecuteNonQuery();
    }

    public static (ComprobanteGenerado Document, DatosEmisor Issuer) Load(string databasePath)
    {
        StoredSample sample = LoadSample(databasePath);
        if (sample.Document is null) throw new InvalidDataException("La muestra persistida es un remito, no un comprobante fiscal.");
        return (sample.Document, sample.Issuer);
    }

    public static StoredSample LoadSample(string databasePath)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadOnly }.ToString());
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT document_kind, document_json, issuer_json FROM rendering_sample WHERE id = 1;";
        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read()) throw new InvalidDataException("No existe una muestra persistida en la base SQLite.");
        string kind = reader.GetString(0);
        string json = reader.GetString(1);
        ComprobanteGenerado? document = kind == "invoice" ? JsonSerializer.Deserialize<ComprobanteGenerado>(json, JsonOptions)
            ?? throw new InvalidDataException("El comprobante JSON persistido está vacío.") : null;
        RemitoGenerado? remito = kind == "remito" ? JsonSerializer.Deserialize<RemitoGenerado>(json, JsonOptions)
            ?? throw new InvalidDataException("El remito JSON persistido está vacío.") : null;
        if (document is null && remito is null) throw new InvalidDataException("El tipo de documento persistido no está soportado.");
        DatosEmisor issuer = JsonSerializer.Deserialize<DatosEmisor>(reader.GetString(2), JsonOptions)
            ?? throw new InvalidDataException("El perfil del emisor JSON persistido está vacío.");
        return new StoredSample(document, remito, issuer, json);
    }

    public sealed record StoredSample(ComprobanteGenerado? Document, RemitoGenerado? Remito, DatosEmisor Issuer, string DocumentJson);

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
