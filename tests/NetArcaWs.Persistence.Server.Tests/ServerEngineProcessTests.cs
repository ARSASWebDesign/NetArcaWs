using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.Cryptography;
using NetArcaWs.EntityFrameworkCore.Migrations;
using NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql;
using NetArcaWs.EntityFrameworkCore.Migrations.SqlServer;
using NetArcaWs.EntityFrameworkCore.PostgreSql;
using NetArcaWs.EntityFrameworkCore.SqlServer;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Persistence.Probe;
using NetArcaWs.Transport;
using NetArcaWs.Wsaa;
using Npgsql;
using Xunit;

namespace NetArcaWs.Persistence.Server.Tests;

public sealed class ServerEngineProcessTests
{
    [Fact]
    public async Task Selected_server_preserves_invoice_uniqueness_replay_series_fencing_and_payload_invariants()
    {
        PersistenceServerSettings? settings = PersistenceServerSettings.FromEnvironment();
        Assert.SkipWhen(settings is null, "Opt-in PostgreSQL/SQL Server integration test. Set NETARCA_PERSISTENCE_DB, NETARCA_PERSISTENCE_DB_KIND, and NETARCA_PERSISTENCE_DB_VERSION to request it.");
        PersistenceServerSettings selected = settings!;
        Assert.SkipWhen(selected.Kind is "mysql" or "mariadb", "This suite targets PostgreSQL/SQL Server; MySQL/MariaDB runs in NetArcaWs.EntityFrameworkCore.MySql.Tests.");

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServerTestDatabase database = await ServerTestDatabase.CreateAsync(selected,
            options => options.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1));
        IInvoiceJournal journal = database.Services.GetRequiredService<IInvoiceJournal>();
        string stem = "server-integration-" + Guid.NewGuid().ToString("N");

        InvoiceSubmission original = Submission(stem, "replay", "wsfe", 1, 11, 1, 910001);
        InvoiceOperation prepared = await journal.PrepareAsync(original, cancellationToken);
        InvoiceOperation replay = await journal.PrepareAsync(original, cancellationToken);
        replay.Submission.Should().BeEquivalentTo(prepared.Submission);

        InvoiceSubmission duplicateFiscal = original with
        {
            TenantId = stem + "-second-tenant",
            IdempotencyKey = "same-fiscal-key",
            Service = "wsfex"
        };
        Func<Task> fiscalConflict = () => journal.PrepareAsync(duplicateFiscal, cancellationToken);
        await fiscalConflict.Should().ThrowAsync<InvoiceConflictException>();

        InvoiceSubmission duplicateRemote = Submission(stem + "-remote", "duplicate-remote", "wsfe", 2, 12, 2, 910001);
        Func<Task> remoteConflict = () => journal.PrepareAsync(duplicateRemote, cancellationToken);
        await remoteConflict.Should().ThrowAsync<InvoiceConflictException>();

        InvoiceSubmission nullRemoteA = Submission(stem, "null-remote-a", "wsfe", 3, 31, 1, null);
        InvoiceSubmission nullRemoteB = Submission(stem, "null-remote-b", "wsfe", 4, 32, 1, null);
        (await journal.PrepareAsync(nullRemoteA, cancellationToken)).Submission.Should().BeEquivalentTo(nullRemoteA);
        (await journal.PrepareAsync(nullRemoteB, cancellationToken)).Submission.Should().BeEquivalentTo(nullRemoteB);

        InvoiceSubmission unicode = Submission(stem, "unicode", "wsfe", 5, 33, 1, null) with
        {
            Payload = "<comprobante>café, 日本語, 😀 — año fiscal</comprobante>"
        };
        (await journal.PrepareAsync(unicode, cancellationToken)).Submission.Payload.Should().Be(unicode.Payload);
        InvoiceSubmission large = Submission(stem, "large", "wsfe", 6, 34, 1, null) with
        {
            Payload = "<synthetic>" + new string('x', (2 * 1024 * 1024) - 23) + "</synthetic>"
        };
        InvoiceOperation persistedLarge = await journal.PrepareAsync(large, cancellationToken);
        (await journal.FindAsync(stem, "large", cancellationToken))!.Submission.Payload.Should().Be(persistedLarge.Submission.Payload);
        Func<Task> oversized = () => journal.PrepareAsync(large with { IdempotencyKey = "oversized", Payload = large.Payload + "x" }, cancellationToken);
        await oversized.Should().ThrowAsync<ArgumentException>();

        InvoiceSubmission competingA = Submission(stem, "race-a", "wsfe", 20, 41, 1, null);
        InvoiceSubmission competingB = Submission(stem, "race-b", "wsfe", 21, 41, 1, null);
        ProcessRaceResult race = await database.RunTwoProcessRaceAsync(competingA, competingB, cancellationToken);
        race.Successes.Should().Be(1);
        race.Conflicts.Should().Be(1);

        InvoiceOperation winner = await journal.FindAsync(stem, "race-a", cancellationToken)
            ?? await journal.FindAsync(stem, "race-b", cancellationToken)
            ?? throw new InvalidOperationException("The series race did not persist its winner.");
        ProcessRaceResult claims = await database.RunTwoClaimProcessesAsync(winner.Submission.TenantId, winner.Submission.IdempotencyKey, cancellationToken);
        claims.Successes.Should().Be(1);
        claims.Conflicts.Should().Be(1);

        InvoiceSubmission identical = Submission(stem, "identical-processes", "wsfe", 30, 42, 1, null);
        ProcessRaceResult identicalReplay = await database.RunTwoProcessRaceAsync(identical, identical, cancellationToken);
        identicalReplay.Successes.Should().Be(2);
        identicalReplay.Conflicts.Should().Be(0);

        InvoiceSubmission revisionSource = Submission(stem, "revision", "wsfe", 40, 43, 1, null);
        await journal.PrepareAsync(revisionSource, cancellationToken);
        InvoiceLease lease = (await journal.TryAcquireAsync(stem, "revision", TimeSpan.FromSeconds(30), false, cancellationToken))!;
        InvoiceOperation rejected = await journal.CompleteAsync(lease, new InvoiceDecision(InvoiceState.Rejected, ResponseXml: "<synthetic-rejection/>") , cancellationToken);
        Func<Task> staleRevision = () => journal.ReviseRejectedAsync(revisionSource with { Payload = "<corrected/>", IdempotencyKey = "revision" }, rejected.Version - 1, cancellationToken);
        await staleRevision.Should().ThrowAsync<InvoiceConflictException>();
        InvoiceOperation revised = await journal.ReviseRejectedAsync(revisionSource with { Payload = "<corrected/>" }, rejected.Version, cancellationToken);
        revised.Submission.Identity.Should().Be(revisionSource.Identity);
        (await journal.ListRevisionsAsync(stem, "revision", cancellationToken)).Should().ContainSingle();

        InvoiceSubmission expiring = Submission(stem, "fenced", "wsfe", 50, 44, 1, null);
        await journal.PrepareAsync(expiring, cancellationToken);
        InvoiceLease oldLease = (await journal.TryAcquireAsync(stem, "fenced", TimeSpan.FromMilliseconds(100), false, cancellationToken))!;
        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
        InvoiceLease newLease = (await journal.TryAcquireAsync(stem, "fenced", TimeSpan.FromSeconds(30), true, cancellationToken))!;
        Func<Task> staleCompletion = () => journal.CompleteAsync(oldLease, new InvoiceDecision(InvoiceState.Rejected), cancellationToken);
        await staleCompletion.Should().ThrowAsync<InvalidOperationException>();
        await journal.CompleteAsync(newLease, new InvoiceDecision(InvoiceState.Rejected), cancellationToken);
    }

    [Fact]
    public async Task Three_server_processes_share_one_encrypted_ticket_and_reuse_it_after_restart()
    {
        PersistenceServerSettings? settings = PersistenceServerSettings.FromEnvironment();
        Assert.SkipWhen(settings is null, "Opt-in PostgreSQL/SQL Server ticket integration test. Set NETARCA_PERSISTENCE_DB, NETARCA_PERSISTENCE_DB_KIND, and NETARCA_PERSISTENCE_DB_VERSION to request it.");
        PersistenceServerSettings selected = settings!;
        Assert.SkipWhen(selected.Kind is "mysql" or "mariadb", "This suite targets PostgreSQL/SQL Server; MySQL/MariaDB runs in NetArcaWs.EntityFrameworkCore.MySql.Tests.");

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServerTestDatabase database = await ServerTestDatabase.CreateAsync(selected,
            options => options.AddWsaaTickets(ArcaService.Wsfev1));
        int loginCount = await database.RunThreeTicketProcessesAsync(cancellationToken);
        loginCount.Should().Be(1);
        await database.AssertTicketRowsContainNoPlaintextAsync(cancellationToken);
    }

    private static InvoiceSubmission Submission(string tenant, string key, string service, long number,
        int pointOfSale, int voucherType, long? remoteId) => new(
        tenant, key, service,
        new InvoiceIdentity(ArcaEnvironment.Homologation, 20999888777, pointOfSale, voucherType, number),
        "<synthetic>server-persistence-probe</synthetic>", RemoteRequestId: remoteId);
}

internal sealed record ProcessRaceResult(int Successes, int Conflicts);

internal sealed class ServerTestDatabase : IAsyncDisposable
{
    private readonly PersistenceServerSettings settings;
    private readonly string databaseName;
    private readonly string connectionString;
    private readonly ServiceProvider services;
    private readonly NetArcaWsModelOptions modelOptions;

    private ServerTestDatabase(PersistenceServerSettings settings, string databaseName, string connectionString,
        NetArcaWsModelOptions modelOptions, ServiceProvider services)
    {
        this.settings = settings;
        this.databaseName = databaseName;
        this.connectionString = connectionString;
        this.modelOptions = modelOptions;
        this.services = services;
    }

    public ServiceProvider Services => services;
    internal string ConnectionString => connectionString;
    internal string EngineKind => settings.Kind;

    public async Task<NetArcaWsMigrationStatus> ApplyOfficialMigrationsAsync(NetArcaWsModelOptions options,
        CancellationToken cancellationToken = default)
    {
        return await CreateOfficialMigrator(options).ApplyAsync(cancellationToken);
    }

    public INetArcaWsMigrator CreateOfficialMigrator(NetArcaWsModelOptions options)
    {
        INetArcaWsMigrationContextFactory factory = settings.Kind switch
        {
            "postgresql" => new PostgreSqlMigrationContextFactory(connectionString),
            "sqlserver" => new SqlServerMigrationContextFactory(connectionString),
            _ => throw new InvalidOperationException("The shared server suite supports PostgreSQL and SQL Server only.")
        };
        NetArcaWsPersistenceModule[] modules = Enum.GetValues<NetArcaWsPersistenceModule>()
            .Where(module => module switch
            {
                NetArcaWsPersistenceModule.Invoicing => options.InvoicingEnabled,
                NetArcaWsPersistenceModule.WsaaTickets => options.WsaaTicketsEnabled,
                NetArcaWsPersistenceModule.TenantCertificates => options.CertificatesEnabled,
                _ => false
            }).ToArray();
        return new NetArcaWsMigrator(factory, modules);
    }

    public static async Task<ServerTestDatabase> CreateAsync(PersistenceServerSettings settings,
        Action<NetArcaWsModelOptionsBuilder> configure, bool provision = true, byte[]? certificateProtectionKey = null)
    {
        string databaseName = "netarcaws_probe_" + Guid.NewGuid().ToString("N");
        string connectionString = await CreateDatabaseAsync(settings, databaseName);
        NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(configure);
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IWsaaTicketProtector>(CreateProtector());
        serviceCollection.AddSingleton<IWsaaTicketLoginClient, UnusedLoginClient>();
        RegisterStores(serviceCollection, settings, connectionString, modelOptions, certificateProtectionKey);
        ServiceProvider? provider = null;
        try
        {
            string actualVersion = await ReadServerVersionAsync(settings.Kind, connectionString);
            if (!MatchesRequestedVersion(settings.Version, actualVersion))
                throw new InvalidOperationException($"Requested persistence server version {settings.Version}, but the connected {settings.Kind} engine reports {actualVersion}.");
            Console.WriteLine($"Opt-in persistence test engine: {settings.Kind} {actualVersion}.");
            provider = serviceCollection.BuildServiceProvider();
            var database = new ServerTestDatabase(settings, databaseName, connectionString, modelOptions, provider);
            if (provision) await database.ApplyOfficialMigrationsAsync(modelOptions, TestContext.Current.CancellationToken);
            return database;
        }
        catch
        {
            if (provider is not null) await provider.DisposeAsync();
            await DropDatabaseAsync(settings, databaseName, connectionString);
            throw;
        }
    }

    public async Task<ProcessRaceResult> RunTwoProcessRaceAsync(InvoiceSubmission left, InvoiceSubmission right,
        CancellationToken cancellationToken)
    {
        string barrier = CreateBarrierDirectory();
        Process? leftProcess = null;
        Process? rightProcess = null;
        try
        {
            Dictionary<string, string?> environment = BarrierEnvironment(barrier, 2);
            leftProcess = StartProbe(environment, "prepare", left.TenantId, left.IdempotencyKey, left.Service,
                left.Identity.Cuit.ToString(CultureInfo.InvariantCulture), left.Identity.PointOfSale.ToString(CultureInfo.InvariantCulture),
                left.Identity.VoucherNumber.ToString(CultureInfo.InvariantCulture));
            rightProcess = StartProbe(environment, "prepare", right.TenantId, right.IdempotencyKey, right.Service,
                right.Identity.Cuit.ToString(CultureInfo.InvariantCulture), right.Identity.PointOfSale.ToString(CultureInfo.InvariantCulture),
                right.Identity.VoucherNumber.ToString(CultureInfo.InvariantCulture));
            await ReleaseBarrierAsync(barrier, 2, cancellationToken);
            await Task.WhenAll(leftProcess.WaitForExitAsync(cancellationToken), rightProcess.WaitForExitAsync(cancellationToken));
            int[] exitCodes = [leftProcess.ExitCode, rightProcess.ExitCode];
            if (exitCodes.Any(code => code is not (0 or 3))) throw await ProcessFailureAsync([leftProcess, rightProcess], exitCodes);
            return new ProcessRaceResult(exitCodes.Count(code => code == 0), exitCodes.Count(code => code == 3));
        }
        finally
        {
            await StopIfRunningAsync(leftProcess);
            await StopIfRunningAsync(rightProcess);
            if (Directory.Exists(barrier)) Directory.Delete(barrier, recursive: true);
        }
    }

    public async Task<ProcessRaceResult> RunTwoClaimProcessesAsync(string tenant, string key, CancellationToken cancellationToken)
    {
        string barrier = CreateBarrierDirectory();
        Process? left = null;
        Process? right = null;
        try
        {
            Dictionary<string, string?> environment = BarrierEnvironment(barrier, 2);
            left = StartProbe(environment, "claim", tenant, key);
            right = StartProbe(environment, "claim", tenant, key);
            await ReleaseBarrierAsync(barrier, 2, cancellationToken);
            await Task.WhenAll(left.WaitForExitAsync(cancellationToken), right.WaitForExitAsync(cancellationToken));
            int[] exitCodes = [left.ExitCode, right.ExitCode];
            if (exitCodes.Any(code => code is not (0 or 4))) throw await ProcessFailureAsync([left, right], exitCodes);
            return new ProcessRaceResult(exitCodes.Count(code => code == 0), exitCodes.Count(code => code == 4));
        }
        finally
        {
            await StopIfRunningAsync(left);
            await StopIfRunningAsync(right);
            if (Directory.Exists(barrier)) Directory.Delete(barrier, recursive: true);
        }
    }

    public async Task<int> RunThreeTicketProcessesAsync(CancellationToken cancellationToken)
    {
        using RSA privateKey = RSA.Create(2048);
        var request = new CertificateRequest("CN=netarcaws-synthetic-server-probe", privateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
        string certificatePem = certificate.ExportCertificatePem();
        string privateKeyPem = privateKey.ExportPkcs8PrivateKeyPem();
        string counterPath = Path.Combine(Path.GetTempPath(), $"netarcaws-server-ticket-counter-{Guid.NewGuid():N}.txt");
        string barrier = CreateBarrierDirectory();
        byte[] key = RandomNumberGenerator.GetBytes(32);
        var processes = new List<Process>();
        try
        {
            Dictionary<string, string?> environment = new()
            {
                ["NETARCA_PROBE_CERT_PEM"] = certificatePem,
                ["NETARCA_PROBE_PRIVATE_KEY_PEM"] = privateKeyPem,
                ["NETARCA_PROBE_AES_KEY"] = Convert.ToBase64String(key),
                ["NETARCA_PROBE_LOGIN_COUNTER"] = counterPath,
                ["NETARCA_PROBE_TENANT_ID"] = "synthetic-server-tenant"
            };
            foreach ((string name, string? value) in BarrierEnvironment(barrier, 3)) environment[name] = value;
            for (int index = 0; index < 3; index++)
            {
                var processEnvironment = new Dictionary<string, string?>(environment)
                {
                    ["NETARCA_PROBE_TENANT_ID"] = "synthetic-server-tenant-" + index.ToString(CultureInfo.InvariantCulture)
                };
                processes.Add(StartProbe(processEnvironment, "shared-ticket"));
            }
            await ReleaseBarrierAsync(barrier, 3, cancellationToken);
            await Task.WhenAll(processes.Select(process => process.WaitForExitAsync(cancellationToken)));
            int[] exitCodes = processes.Select(process => process.ExitCode).ToArray();
            if (exitCodes.Any(code => code != 0)) throw await ProcessFailureAsync(processes, exitCodes);

            Dictionary<string, string?> restartEnvironment = new(environment)
            {
                ["NETARCA_PROBE_TENANT_ID"] = "synthetic-server-tenant-restarted"
            };
            restartEnvironment.Remove("NETARCA_PROBE_BARRIER_DIRECTORY");
            restartEnvironment.Remove("NETARCA_PROBE_BARRIER_EXPECTED");
            Process restarted = StartProbe(restartEnvironment, "shared-ticket");
            processes.Add(restarted);
            await restarted.WaitForExitAsync(cancellationToken);
            if (restarted.ExitCode != 0) throw await ProcessFailureAsync([restarted], [restarted.ExitCode]);
            return int.Parse(await File.ReadAllTextAsync(counterPath, cancellationToken), CultureInfo.InvariantCulture);
        }
        finally
        {
            foreach (Process process in processes) await StopIfRunningAsync(process);
            CryptographicOperations.ZeroMemory(key);
            if (Directory.Exists(barrier)) Directory.Delete(barrier, recursive: true);
            if (File.Exists(counterPath)) File.Delete(counterPath);
        }
    }

    public async Task AssertTicketRowsContainNoPlaintextAsync(CancellationToken cancellationToken)
    {
        await using ArcaWsDbContext context = await services.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync(cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using System.Data.Common.DbCommand command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = settings.Kind == "postgresql"
            ? "SELECT * FROM \"NetArcaWsaaTickets\""
            : "SELECT * FROM [NetArcaWsaaTickets]";
        await using System.Data.Common.DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        reader.Read().Should().BeTrue("the shared ticket provider should persist a synthetic ticket");
        var storedBytes = new List<byte>();
        for (int ordinal = 0; ordinal < reader.FieldCount; ordinal++)
        {
            if (reader.IsDBNull(ordinal)) continue;
            object value = reader.GetValue(ordinal);
            storedBytes.AddRange(value is byte[] binary
                ? binary
                : Encoding.UTF8.GetBytes(Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""));
        }
        byte[] rowBytes = storedBytes.ToArray();
        ContainsSequence(rowBytes, Encoding.UTF8.GetBytes("synthetic-token")).Should().BeFalse();
        ContainsSequence(rowBytes, Encoding.UTF8.GetBytes("synthetic-signature")).Should().BeFalse();
        ContainsSequence(rowBytes, Encoding.UTF8.GetBytes("<loginTicketResponse")).Should().BeFalse();
    }

    private Process StartProbe(IReadOnlyDictionary<string, string?> extraEnvironment, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(typeof(ProbeMarker).Assembly.Location);
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        start.Environment["NETARCA_PERSISTENCE_DB"] = connectionString;
        start.Environment["NETARCA_PERSISTENCE_DB_KIND"] = settings.Kind;
        start.Environment["NETARCA_PERSISTENCE_DB_VERSION"] = settings.Version.ToString();
        foreach ((string name, string? value) in extraEnvironment) start.Environment[name] = value;
        return Process.Start(start) ?? throw new InvalidOperationException("Could not start the independent persistence probe process.");
    }

    private static async Task<string> CreateDatabaseAsync(PersistenceServerSettings settings, string databaseName)
    {
        switch (settings.Kind)
        {
            case "postgresql":
            {
                var builder = new NpgsqlConnectionStringBuilder(settings.ConnectionString) { Database = "postgres" };
                await using var admin = new NpgsqlConnection(builder.ConnectionString);
                await admin.OpenAsync();
                await using var command = admin.CreateCommand();
                command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await command.ExecuteNonQueryAsync();
                builder.Database = databaseName;
                return builder.ConnectionString;
            }
            case "sqlserver":
            {
                var builder = new SqlConnectionStringBuilder(settings.ConnectionString) { InitialCatalog = "master" };
                await using var admin = new SqlConnection(builder.ConnectionString);
                await admin.OpenAsync();
                await using var command = admin.CreateCommand();
                command.CommandText = $"CREATE DATABASE [{databaseName}]";
                await command.ExecuteNonQueryAsync();
                builder.InitialCatalog = databaseName;
                return builder.ConnectionString;
            }
            default:
                throw new InvalidOperationException("The shared server suite supports PostgreSQL and SQL Server only.");
        }
    }

    private static async Task<string> ReadServerVersionAsync(string kind, string connectionString)
    {
        if (kind == "postgresql")
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SHOW server_version";
            return Convert.ToString(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture)
                ?? throw new InvalidOperationException("PostgreSQL returned no server version.");
        }

        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CONVERT(varchar(128), SERVERPROPERTY('ProductVersion'))";
            return Convert.ToString(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture)
                ?? throw new InvalidOperationException("SQL Server returned no product version.");
        }
    }

    private static bool MatchesRequestedVersion(Version requested, string actualText)
    {
        int end = actualText.TakeWhile(character => char.IsAsciiDigit(character) || character == '.').Count();
        return Version.TryParse(actualText[..end], out Version? actual) &&
            requested.Major == actual.Major && requested.Minor == actual.Minor;
    }

    private static void RegisterStores(IServiceCollection services, PersistenceServerSettings settings,
        string connectionString, NetArcaWsModelOptions modelOptions, byte[]? certificateProtectionKey)
    {
        if (modelOptions.CertificatesEnabled)
            services.AddSingleton<IArcaCertificateProtector>(new AesGcmArcaCertificateProtector("provider-test",
                new Dictionary<string, byte[]> { ["provider-test"] = certificateProtectionKey ?? RandomNumberGenerator.GetBytes(32) }));
        switch (settings.Kind)
        {
            case "postgresql": services.AddNetArcaWsPostgreSqlStores(connectionString, modelOptions); break;
            case "sqlserver": services.AddNetArcaWsSqlServerStores(connectionString, modelOptions); break;
            default: throw new InvalidOperationException("The shared server suite supports PostgreSQL and SQL Server only.");
        }
    }

    private static async Task DropDatabaseAsync(PersistenceServerSettings settings, string databaseName, string connectionString)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            if (settings.Kind == "postgresql")
            {
                NpgsqlConnection.ClearAllPools();
                var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };
                await using var admin = new NpgsqlConnection(builder.ConnectionString);
                await admin.OpenAsync();
                await using var command = admin.CreateCommand();
                command.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)";
                await command.ExecuteNonQueryAsync();
            }
            else if (settings.Kind == "sqlserver")
            {
                SqlConnection.ClearAllPools();
                var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
                await using var admin = new SqlConnection(builder.ConnectionString);
                await admin.OpenAsync();
                await using var command = admin.CreateCommand();
                command.CommandText = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]";
                await command.ExecuteNonQueryAsync();
            }
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Could not remove the uniquely named synthetic server-test database.", exception);
        }
    }

    private static IWsaaTicketProtector CreateProtector() => new AesGcmWsaaTicketProtector("server-probe",
        new Dictionary<string, byte[]> { ["server-probe"] = RandomNumberGenerator.GetBytes(32) });

    private static string CreateBarrierDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "netarcaws-server-barrier-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static Dictionary<string, string?> BarrierEnvironment(string directory, int expected) => new()
    {
        ["NETARCA_PROBE_BARRIER_DIRECTORY"] = directory,
        ["NETARCA_PROBE_BARRIER_EXPECTED"] = expected.ToString(CultureInfo.InvariantCulture)
    };

    private static async Task ReleaseBarrierAsync(string directory, int expected, CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (Directory.GetFiles(directory, "*.ready").Length < expected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTimeOffset.UtcNow >= deadline) throw new TimeoutException("Independent server processes did not reach the start barrier.");
            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
        }
        await File.WriteAllTextAsync(Path.Combine(directory, "release"), "go", cancellationToken);
    }

    private static async Task StopIfRunningAsync(Process? process)
    {
        if (process is null) return;
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }
        process.Dispose();
    }

    private static async Task<InvalidOperationException> ProcessFailureAsync(IReadOnlyList<Process> processes, int[] exitCodes)
    {
        var diagnostics = new StringBuilder();
        foreach (Process process in processes)
            diagnostics.Append(await process.StandardError.ReadToEndAsync());
        return new InvalidOperationException($"Independent persistence processes failed with exit codes {string.Join(',', exitCodes)}. {diagnostics}");
    }

    private static bool ContainsSequence(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        if (value.Length == 0) return true;
        return source.IndexOf(value) >= 0;
    }

    public async ValueTask DisposeAsync()
    {
        await services.DisposeAsync();
        await DropDatabaseAsync(settings, databaseName, connectionString);
    }

    private sealed class UnusedLoginClient : IWsaaTicketLoginClient
    {
        public Task<WsaaTicket> LoginAsync(string service, ArcaTenantContext authorizedTenant, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("The server test setup must not perform WSAA login from its main process.");
    }
}
