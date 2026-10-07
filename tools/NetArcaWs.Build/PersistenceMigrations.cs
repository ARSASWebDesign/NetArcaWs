using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NetArcaWs.EntityFrameworkCore.Migrations.MariaDb;
using NetArcaWs.EntityFrameworkCore.Migrations.MySql;
using NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql;
using NetArcaWs.EntityFrameworkCore.Migrations.Sqlite;
using NetArcaWs.EntityFrameworkCore.Migrations.SqlServer;

namespace NetArcaWs.Build;

public sealed record PersistenceMigrationsCheckResult(bool IsCurrent, IReadOnlyList<string> Drifted);

/// <summary>Generates reviewed SQL baselines from the versioned provider migrations without opening a connection.</summary>
public static class PersistenceMigrations
{
    private const string RelativeDirectory = "docs/reference/persistence-migrations";
    private static readonly (string Provider, Func<DbContext> Invoicing, Func<DbContext> WsaaTickets)[] Contexts =
    [
        ("sqlite", SqliteInvoice, SqliteTickets),
        ("mysql", MySqlInvoice, MySqlTickets),
        ("mariadb", MariaDbInvoice, MariaDbTickets),
        ("postgresql", PostgreSqlInvoice, PostgreSqlTickets),
        ("sqlserver", SqlServerInvoice, SqlServerTickets)
    ];

    public static IReadOnlyDictionary<string, string> GenerateScripts()
    {
        var scripts = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach ((string provider, Func<DbContext> invoicing, Func<DbContext> wsaaTickets) in Contexts)
        {
            scripts.Add($"{provider}/invoicing/0-latest.sql", Generate(provider, "invoicing", invoicing));
            scripts.Add($"{provider}/wsaa-tickets/0-latest.sql", Generate(provider, "wsaa-tickets", wsaaTickets));
        }
        return scripts;
    }

    public static PersistenceMigrationsCheckResult Check(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        string directory = Path.Combine(root, RelativeDirectory);
        IReadOnlyDictionary<string, string> expected = GenerateScripts();
        var drifted = new List<string>();
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
        return new PersistenceMigrationsCheckResult(drifted.Count == 0, drifted.Order(StringComparer.Ordinal).ToArray());
    }

    public static void Run(string root, bool check)
    {
        PersistenceMigrationsCheckResult result = Check(root);
        if (check)
        {
            if (!result.IsCurrent)
                throw new InvalidOperationException("Persistence migration SQL drift detected: " + string.Join(", ", result.Drifted));
            Console.WriteLine("Persistence migration SQL is current (10 provider/module combinations).");
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
        Console.WriteLine($"Generated {scripts.Count} migration SQL scripts under {RelativeDirectory}.");
    }

    private static string Generate(string provider, string module, Func<DbContext> createContext)
    {
        using DbContext context = createContext();
        return GenerateScriptForContext(provider, module, context);
    }

    internal static string GenerateScriptForContext(string provider, string module, DbContext context)
    {
        if (context.Database.HasPendingModelChanges())
            throw new InvalidOperationException($"The {provider}/{module} migration snapshot has pending model changes.");

        IMigrationsAssembly assembly = context.GetService<IMigrationsAssembly>();
        KeyValuePair<string, TypeInfo>[] migrations = assembly.Migrations.OrderBy(entry => entry.Key, StringComparer.Ordinal).ToArray();
        string initialMigrationId = module == "invoicing"
            ? "20261006000100_InitialInvoicing"
            : "20261006000200_InitialWsaaTickets";
        if (migrations.Length == 0 || !string.Equals(migrations[0].Key, initialMigrationId, StringComparison.Ordinal))
            throw new InvalidOperationException($"The {provider}/{module} migration chain must start with {initialMigrationId}.");
        Migration migration = assembly.CreateMigration(migrations[0].Value, context.Database.ProviderName!);
        IReadOnlyList<MigrationOperation> operations = migration.UpOperations;
        int createTables = operations.OfType<CreateTableOperation>().Count();
        string[] expectedTables = module == "invoicing"
            ? ["NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations"]
            : ["NetArcaWsaaTickets"];
        string[] created = operations.OfType<CreateTableOperation>().Select(operation => operation.Name).ToArray();
        if (createTables != expectedTables.Length || expectedTables.Except(created, StringComparer.Ordinal).Any()
            || operations.OfType<RenameTableOperation>().Any())
            throw new InvalidOperationException($"The {provider}/{module} initial migration does not create the expected tables directly.");

        string sql = context.GetService<IMigrator>().GenerateScript("0", null, MigrationsSqlGenerationOptions.Default);
        sql = sql.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').TrimEnd();
        return $"-- Generated from the versioned NetArcaWs EF Core migrations. Do not edit by hand.\n-- Provider: {provider}; module: {module}.\n{sql}\n";
    }

    private static DbContext SqliteInvoice() => new SqliteInvoicingMigrationsDbContext(new DbContextOptionsBuilder<SqliteInvoicingMigrationsDbContext>()
        .UseSqlite("Data Source=:memory:", options => options.MigrationsAssembly(typeof(SqliteMigrationContextFactory).Assembly.FullName)
            .MigrationsHistoryTable("__NetArcaWsInvoiceMigrations")).Options);
    private static DbContext SqliteTickets() => new SqliteWsaaTicketsMigrationsDbContext(new DbContextOptionsBuilder<SqliteWsaaTicketsMigrationsDbContext>()
        .UseSqlite("Data Source=:memory:", options => options.MigrationsAssembly(typeof(SqliteMigrationContextFactory).Assembly.FullName)
            .MigrationsHistoryTable("__NetArcaWsTicketMigrations")).Options);
    private static DbContext MySqlInvoice() => new MySqlInvoicingMigrationsDbContext(new DbContextOptionsBuilder<MySqlInvoicingMigrationsDbContext>()
        .UseMySql("Server=localhost;Database=offline", new MySqlServerVersion(new Version(8, 4, 11)), options => options.MigrationsHistoryTable("__NetArcaWsInvoiceMigrations")).Options);
    private static DbContext MySqlTickets() => new MySqlWsaaTicketsMigrationsDbContext(new DbContextOptionsBuilder<MySqlWsaaTicketsMigrationsDbContext>()
        .UseMySql("Server=localhost;Database=offline", new MySqlServerVersion(new Version(8, 4, 11)), options => options.MigrationsHistoryTable("__NetArcaWsTicketMigrations")).Options);
    private static DbContext MariaDbInvoice() => new MariaDbInvoicingMigrationsDbContext(new DbContextOptionsBuilder<MariaDbInvoicingMigrationsDbContext>()
        .UseMySql("Server=localhost;Database=offline", new MariaDbServerVersion(new Version(11, 4, 13)), options => options.MigrationsHistoryTable("__NetArcaWsInvoiceMigrations")).Options);
    private static DbContext MariaDbTickets() => new MariaDbWsaaTicketsMigrationsDbContext(new DbContextOptionsBuilder<MariaDbWsaaTicketsMigrationsDbContext>()
        .UseMySql("Server=localhost;Database=offline", new MariaDbServerVersion(new Version(11, 4, 13)), options => options.MigrationsHistoryTable("__NetArcaWsTicketMigrations")).Options);
    private static DbContext PostgreSqlInvoice() => new PostgreSqlInvoicingMigrationsDbContext(new DbContextOptionsBuilder<PostgreSqlInvoicingMigrationsDbContext>()
        .UseNpgsql("Host=localhost;Database=offline", options => options.MigrationsHistoryTable("__NetArcaWsInvoiceMigrations", "public")).Options);
    private static DbContext PostgreSqlTickets() => new PostgreSqlWsaaTicketsMigrationsDbContext(new DbContextOptionsBuilder<PostgreSqlWsaaTicketsMigrationsDbContext>()
        .UseNpgsql("Host=localhost;Database=offline", options => options.MigrationsHistoryTable("__NetArcaWsTicketMigrations", "public")).Options);
    private static DbContext SqlServerInvoice() => new SqlServerInvoicingMigrationsDbContext(new DbContextOptionsBuilder<SqlServerInvoicingMigrationsDbContext>()
        .UseSqlServer("Server=localhost;Database=offline;Encrypt=True;TrustServerCertificate=True", options => options.MigrationsHistoryTable("__NetArcaWsInvoiceMigrations", "dbo")).Options);
    private static DbContext SqlServerTickets() => new SqlServerWsaaTicketsMigrationsDbContext(new DbContextOptionsBuilder<SqlServerWsaaTicketsMigrationsDbContext>()
        .UseSqlServer("Server=localhost;Database=offline;Encrypt=True;TrustServerCertificate=True", options => options.MigrationsHistoryTable("__NetArcaWsTicketMigrations", "dbo")).Options);
}
