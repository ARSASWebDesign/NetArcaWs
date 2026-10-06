using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.MySql;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Transport;
using NetArcaWs.Wsaa;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.MySql.Tests;

public sealed class MySqlPersistenceTests
{
    [Fact]
    public void Registration_uses_explicit_server_version_without_connecting_or_creating_schema()
    {
        var services = new ServiceCollection();
        NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(x => x.AddInvoicing(ArcaService.Wsfev1));
        services.AddNetArcaWsMySqlStores("Server=127.0.0.1;Port=1;Database=never_connect;User ID=x;Password=x", new MySqlServerVersion(new Version(8, 4, 11)), model);
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().Should().NotBeNull();
        provider.GetRequiredService<IInvoiceJournal>().Should().NotBeNull();
    }

    [Fact]
    public void Partial_database_opt_in_configuration_fails_with_the_missing_setting()
    {
        Action missingKind = () => PersistenceDbSettings.FromValues("Server=localhost;Database=synthetic", null, "8.4.11");
        missingKind.Should().Throw<InvalidOperationException>().WithMessage("*NETARCA_PERSISTENCE_DB_KIND*");
        Action missingConnection = () => PersistenceDbSettings.FromValues(null, "mysql", "8.4.11");
        missingConnection.Should().Throw<InvalidOperationException>().WithMessage("*NETARCA_PERSISTENCE_DB*");
    }

    [Fact]
    public async Task Model_creation_is_provider_independent_and_ticket_selection_is_explicit()
    {
        NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(x => x.AddInvoicing(ArcaService.Wsfev1).AddWsaaTickets(ArcaService.PadronA5));
        var builder = new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite("Data Source=:memory:");
        await using var context = new ArcaWsDbContext(builder.Options, model);
        context.Model.FindEntityType("NetArcaWs.EntityFrameworkCore.InvoiceJournalEntity")!.GetTableName().Should().Be("NetArcaInvoices");
        context.Model.FindEntityType("NetArcaWs.EntityFrameworkCore.WsaaTicketEntity")!.GetTableName().Should().Be("NetArcaWsaaTickets");
    }

    [Fact]
    public void Ticket_only_registration_does_not_require_invoice_tables_or_a_database_connection()
    {
        NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(x => x.AddWsaaTickets(ArcaService.Wsfev1, ArcaService.PadronA5));
        var services = new ServiceCollection();
        services.AddSingleton<IWsaaTicketProtector>(CreateTestProtector());
        services.AddSingleton<IWsaaTicketLoginClient, NoNetworkTicketLoginClient>();
        services.AddNetArcaWsMySqlStores("Server=127.0.0.1;Port=1;Database=never_connect;User ID=x;Password=x", new MySqlServerVersion(new Version(8, 4, 11)), model);
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<IInvoiceJournal>().Should().BeNull();
        provider.GetRequiredService<IArcaTicketProvider>().Should().NotBeNull();
        using ArcaWsDbContext context = provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContext();
        context.Model.FindEntityType("NetArcaWs.EntityFrameworkCore.WsaaTicketEntity")!.GetTableName().Should().Be("NetArcaWsaaTickets");
        context.Model.FindEntityType("NetArcaWs.EntityFrameworkCore.InvoiceJournalEntity").Should().BeNull();
    }

    [Fact]
    public async Task Selected_engine_enforces_cross_service_fiscal_identity_and_series_reservation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool anyOptInSetting = new[] { "NETARCA_PERSISTENCE_DB", "NETARCA_PERSISTENCE_DB_KIND", "NETARCA_PERSISTENCE_DB_VERSION" }
            .Any(name => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)));
        Assert.SkipWhen(!anyOptInSetting,
            "Opt-in MySQL/MariaDB engine test. Set NETARCA_PERSISTENCE_DB, NETARCA_PERSISTENCE_DB_KIND, and NETARCA_PERSISTENCE_DB_VERSION to request it.");
        PersistenceDbSettings settings = PersistenceDbSettings.ReadRequired();
        await using var database = await MySqlTestDatabase.CreateAsync(settings);
        IInvoiceJournal journal = database.Services.GetRequiredService<IInvoiceJournal>();
        string stem = "integration-" + Guid.NewGuid().ToString("N");
        InvoiceSubmission first = Submission(stem, "same-series", "wsfe", 1);
        InvoiceOperation prepared = await journal.PrepareAsync(first, cancellationToken);
        InvoiceOperation replay = await journal.PrepareAsync(first, cancellationToken);
        replay.Submission.Should().BeEquivalentTo(prepared.Submission);

        InvoiceSubmission crossService = Submission(stem + "-other", "other", "wsfex", 1);
        Func<Task> fiscalConflict = () => journal.PrepareAsync(crossService);
        await fiscalConflict.Should().ThrowAsync<InvoiceConflictException>();

        InvoiceSubmission remote = Submission(stem, "remote-source", "wsfex", 2, 21, 9001);
        await journal.PrepareAsync(remote, cancellationToken);
        InvoiceSubmission duplicateRemote = Submission(stem + "-remote-other", "remote-duplicate", "wsfex", 3, 22, 9001);
        Func<Task> remoteConflict = () => journal.PrepareAsync(duplicateRemote, cancellationToken);
        await remoteConflict.Should().ThrowAsync<InvoiceConflictException>();

        InvoiceSubmission caseA = Submission(stem + " ", "Key ", "wsfe", 4, 23);
        InvoiceSubmission caseB = Submission(stem.ToLowerInvariant(), "key", "wsfe", 5, 24);
        (await journal.PrepareAsync(caseA, cancellationToken)).Submission.Should().BeEquivalentTo(caseA);
        (await journal.PrepareAsync(caseB, cancellationToken)).Submission.Should().BeEquivalentTo(caseB);

        InvoiceSubmission maximumPayload = Submission(stem, "max-payload", "wsfe", 6, 25) with { Payload = new string('x', 2 * 1024 * 1024) };
        InvoiceOperation persistedLarge = await journal.PrepareAsync(maximumPayload, cancellationToken);
        (await journal.FindAsync(stem, "max-payload", cancellationToken))!.Submission.Payload.Should().Be(persistedLarge.Submission.Payload);
        InvoiceSubmission unicode = Submission(stem, "unicode-payload", "wsfe", 7, 27) with { Payload = "<fiscal>café, 日本語, 😀</fiscal>" };
        (await journal.PrepareAsync(unicode, cancellationToken)).Submission.Payload.Should().Be(unicode.Payload);
        Func<Task> oversized = () => journal.PrepareAsync(maximumPayload with { IdempotencyKey = "too-large", Payload = maximumPayload.Payload + "x" }, cancellationToken);
        await oversized.Should().ThrowAsync<ArgumentException>();

        InvoiceSubmission competingA = Submission(stem, "race-a", "wsfe", 30, 18);
        InvoiceSubmission competingB = Submission(stem, "race-b", "wsfe", 31, 18);
        ProcessRaceResult result = await database.RunTwoProcessesAsync(competingA, competingB, cancellationToken);
        result.Successes.Should().Be(1);
        result.Conflicts.Should().Be(1);

        InvoiceOperation? preparedWinner = await journal.FindAsync(stem, "race-a", cancellationToken) ?? await journal.FindAsync(stem, "race-b", cancellationToken);
        preparedWinner.Should().NotBeNull();
        ProcessRaceResult claims = await database.RunTwoClaimProcessesAsync(preparedWinner!.Submission.TenantId, preparedWinner.Submission.IdempotencyKey, cancellationToken);
        claims.Successes.Should().Be(1);
        claims.Conflicts.Should().Be(1);

        InvoiceSubmission identical = Submission(stem, "identical-processes", "wsfe", 50, 19);
        ProcessRaceResult identicalResult = await database.RunTwoProcessesAsync(identical, identical, cancellationToken);
        identicalResult.Successes.Should().Be(2);
        identicalResult.Conflicts.Should().Be(0);

        InvoiceLease? firstLease = await journal.TryAcquireAsync(first.TenantId, first.IdempotencyKey, TimeSpan.FromSeconds(30), false, cancellationToken);
        firstLease.Should().NotBeNull();
        InvoiceLease? duplicateLease = await journal.TryAcquireAsync(first.TenantId, first.IdempotencyKey, TimeSpan.FromSeconds(30), false, cancellationToken);
        duplicateLease.Should().BeNull();
        InvoiceOperation rejected = await journal.CompleteAsync(firstLease!, new InvoiceDecision(InvoiceState.Rejected, ResponseXml: "<rejected/>") , cancellationToken);
        InvoiceOperation revised = await journal.ReviseRejectedAsync(first with { Payload = "<synthetic>revision</synthetic>" }, rejected.Version, cancellationToken);
        revised.Submission.Identity.Should().Be(first.Identity);
        revised.Submission.RemoteRequestId.Should().Be(first.RemoteRequestId);
        (await journal.ListRevisionsAsync(first.TenantId, first.IdempotencyKey, cancellationToken)).Should().ContainSingle();

        InvoiceSubmission expiring = Submission(stem, "expiring-lease", "wsfe", 70, 26);
        await journal.PrepareAsync(expiring, cancellationToken);
        InvoiceLease expired = (await journal.TryAcquireAsync(stem, "expiring-lease", TimeSpan.FromMilliseconds(100), false, cancellationToken))!;
        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
        InvoiceLease replacementLease = (await journal.TryAcquireAsync(stem, "expiring-lease", TimeSpan.FromSeconds(30), true, cancellationToken))!;
        Func<Task> staleCompletion = () => journal.CompleteAsync(expired, new InvoiceDecision(InvoiceState.Rejected), cancellationToken);
        await staleCompletion.Should().ThrowAsync<InvalidOperationException>();
        await journal.CompleteAsync(replacementLease, new InvoiceDecision(InvoiceState.Rejected), cancellationToken);

        InvoiceSubmission responseBoundary = Submission(stem, "max-response", "wsfe", 80, 28);
        await journal.PrepareAsync(responseBoundary, cancellationToken);
        InvoiceLease responseLease = (await journal.TryAcquireAsync(stem, "max-response", TimeSpan.FromSeconds(30), false, cancellationToken))!;
        string maximumResponse = new('r', 4 * 1024 * 1024);
        InvoiceOperation responseSaved = await journal.CompleteAsync(responseLease, new InvoiceDecision(InvoiceState.Rejected, ResponseXml: maximumResponse), cancellationToken);
        responseSaved.ResponseXml.Should().HaveLength(4 * 1024 * 1024);
        InvoiceSubmission oversizedResponse = Submission(stem, "oversized-response", "wsfe", 81, 29);
        await journal.PrepareAsync(oversizedResponse, cancellationToken);
        InvoiceLease oversizedResponseLease = (await journal.TryAcquireAsync(stem, "oversized-response", TimeSpan.FromSeconds(30), false, cancellationToken))!;
        Func<Task> rejectOversizedResponse = () => journal.CompleteAsync(oversizedResponseLease,
            new InvoiceDecision(InvoiceState.Rejected, ResponseXml: maximumResponse + "r"), cancellationToken);
        await rejectOversizedResponse.Should().ThrowAsync<ArgumentException>();

        int loginCount = await database.RunThreeTicketProcessesAsync(cancellationToken);
        loginCount.Should().Be(1);
    }

    private static InvoiceSubmission Submission(string tenant, string key, string service, long number, int pointOfSale = 17, long? remoteId = null) => new(
        tenant, key, service,
        new InvoiceIdentity(ArcaEnvironment.Homologation, 20999888777, pointOfSale, 1, number),
        "<synthetic>probe</synthetic>", RemoteRequestId: remoteId);

    internal static IWsaaTicketProtector CreateTestProtector() => new AesGcmWsaaTicketProtector("probe",
        new Dictionary<string, byte[]> { ["probe"] = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32) });

    private sealed class NoNetworkTicketLoginClient : IWsaaTicketLoginClient
    {
        public Task<WsaaTicket> LoginAsync(string service, NetArcaWs.Multitenancy.ArcaTenantContext tenant,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("The registration test must not perform WSAA login.");
    }

}

internal sealed record PersistenceDbSettings(string ConnectionString, string Kind, Version Version)
{
    public static PersistenceDbSettings ReadRequired()
        => FromValues(Environment.GetEnvironmentVariable("NETARCA_PERSISTENCE_DB"),
            Environment.GetEnvironmentVariable("NETARCA_PERSISTENCE_DB_KIND"),
            Environment.GetEnvironmentVariable("NETARCA_PERSISTENCE_DB_VERSION"));

    internal static PersistenceDbSettings FromValues(string? connection, string? kind, string? version)
    {
        if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(version))
            throw new InvalidOperationException("MySQL/MariaDB opt-in was requested, but NETARCA_PERSISTENCE_DB, NETARCA_PERSISTENCE_DB_KIND, or NETARCA_PERSISTENCE_DB_VERSION is missing.");
        if (!Version.TryParse(version, out Version? parsed))
            throw new InvalidOperationException("NETARCA_PERSISTENCE_DB_VERSION must be a dotted numeric version.");
        if (kind is not ("mysql" or "mariadb"))
            throw new InvalidOperationException("NETARCA_PERSISTENCE_DB_KIND must be 'mysql' or 'mariadb'.");
        return new PersistenceDbSettings(connection, kind, parsed);
    }
}

internal sealed record ProcessRaceResult(int Successes, int Conflicts);

internal sealed class MySqlTestDatabase : IAsyncDisposable
{
    private readonly string connectionString;
    private readonly string kind;
    private readonly Version version;
    private readonly ServiceProvider services;

    private MySqlTestDatabase(string connectionString, string kind, Version version, ServiceProvider services)
    {
        this.connectionString = connectionString;
        this.kind = kind;
        this.version = version;
        this.services = services;
    }

    public ServiceProvider Services => services;

    public static async Task<MySqlTestDatabase> CreateAsync(PersistenceDbSettings settings)
    {
        var builder = new MySqlConnectionStringBuilder(settings.ConnectionString)
        {
            Database = "netarcaws_probe_" + Guid.NewGuid().ToString("N")
        };
        NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(x =>
            x.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1).AddWsaaTickets(ArcaService.Wsfev1, ArcaService.PadronA5));
        var services = new ServiceCollection();
        services.AddSingleton<IWsaaTicketProtector>(MySqlPersistenceTests.CreateTestProtector());
        ServerVersion server = settings.Kind == "mysql" ? new MySqlServerVersion(settings.Version) : new MariaDbServerVersion(settings.Version);
        services.AddNetArcaWsMySqlStores(builder.ConnectionString, server, model);
        ServiceProvider provider = services.BuildServiceProvider();
        await using (ArcaWsDbContext context = await provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync())
            await context.Database.EnsureCreatedAsync();
        return new MySqlTestDatabase(builder.ConnectionString, settings.Kind, settings.Version, provider);
    }

    public async Task<ProcessRaceResult> RunTwoProcessesAsync(InvoiceSubmission first, InvoiceSubmission second, CancellationToken cancellationToken)
    {
        string barrier = CreateBarrierDirectory();
        var environment = BarrierEnvironment(barrier, 2);
        try
        {
            using Process left = StartProbe(environment, "prepare", first.TenantId, first.IdempotencyKey, first.Service,
                first.Identity.Cuit.ToString(), first.Identity.PointOfSale.ToString(), first.Identity.VoucherNumber.ToString());
            using Process right = StartProbe(environment, "prepare", second.TenantId, second.IdempotencyKey, second.Service,
                second.Identity.Cuit.ToString(), second.Identity.PointOfSale.ToString(), second.Identity.VoucherNumber.ToString());
            await ReleaseBarrierAsync(barrier, 2, cancellationToken);
            await Task.WhenAll(left.WaitForExitAsync(cancellationToken), right.WaitForExitAsync(cancellationToken));
            int[] exitCodes = [left.ExitCode, right.ExitCode];
            if (exitCodes.Any(code => code is not (0 or 3))) throw await ProcessFailureAsync(left, right, exitCodes);
            return new ProcessRaceResult(exitCodes.Count(x => x == 0), exitCodes.Count(x => x == 3));
        }
        finally { Directory.Delete(barrier, recursive: true); }
    }

    public async Task<ProcessRaceResult> RunTwoClaimProcessesAsync(string tenant, string key, CancellationToken cancellationToken)
    {
        string barrier = CreateBarrierDirectory();
        try
        {
            var environment = BarrierEnvironment(barrier, 2);
            using Process left = StartProbe(environment, "claim", tenant, key);
            using Process right = StartProbe(environment, "claim", tenant, key);
            await ReleaseBarrierAsync(barrier, 2, cancellationToken);
            await Task.WhenAll(left.WaitForExitAsync(cancellationToken), right.WaitForExitAsync(cancellationToken));
            int[] exitCodes = [left.ExitCode, right.ExitCode];
            if (exitCodes.Any(code => code is not (0 or 4))) throw await ProcessFailureAsync(left, right, exitCodes);
            return new ProcessRaceResult(exitCodes.Count(x => x == 0), exitCodes.Count(x => x == 4));
        }
        finally { Directory.Delete(barrier, recursive: true); }
    }

    public async Task<int> RunThreeTicketProcessesAsync(CancellationToken cancellationToken)
    {
        using RSA privateKey = RSA.Create(2048);
        var request = new CertificateRequest("CN=netarcaws-synthetic-probe", privateKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
        string certificatePem = certificate.ExportCertificatePem();
        string privateKeyPem = privateKey.ExportPkcs8PrivateKeyPem();
        string counterPath = Path.Combine(Path.GetTempPath(), $"netarcaws-ticket-login-count-{Guid.NewGuid():N}.txt");
        string barrier = CreateBarrierDirectory();
        byte[] key = RandomNumberGenerator.GetBytes(32);
        try
        {
            Dictionary<string, string?> environment = new()
            {
                ["NETARCA_PROBE_CERT_PEM"] = certificatePem,
                ["NETARCA_PROBE_PRIVATE_KEY_PEM"] = privateKeyPem,
                ["NETARCA_PROBE_AES_KEY"] = Convert.ToBase64String(key),
                ["NETARCA_PROBE_LOGIN_COUNTER"] = counterPath
            };
            foreach ((string name, string? value) in BarrierEnvironment(barrier, 3)) environment[name] = value;
            Process[] processes = Enumerable.Range(1, 3).Select(index =>
            {
                var processEnvironment = new Dictionary<string, string?>(environment)
                {
                    ["NETARCA_PROBE_TENANT_ID"] = "synthetic-tenant-" + index
                };
                return StartProbe(processEnvironment, "shared-ticket");
            }).ToArray();
            using (processes[0]) using (processes[1]) using (processes[2])
            {
                await ReleaseBarrierAsync(barrier, 3, cancellationToken);
                await Task.WhenAll(processes.Select(process => process.WaitForExitAsync(cancellationToken)));
                int[] exitCodes = processes.Select(process => process.ExitCode).ToArray();
                if (exitCodes.Any(code => code != 0)) throw await ProcessFailureAsync(processes[0], processes[1], exitCodes);
            }
            var restartEnvironment = new Dictionary<string, string?>(environment)
            {
                ["NETARCA_PROBE_TENANT_ID"] = "synthetic-tenant-restarted"
            };
            restartEnvironment.Remove("NETARCA_PROBE_BARRIER_DIRECTORY");
            restartEnvironment.Remove("NETARCA_PROBE_BARRIER_EXPECTED");
            using (Process restarted = StartProbe(restartEnvironment, "shared-ticket"))
            {
                await restarted.WaitForExitAsync(cancellationToken);
                if (restarted.ExitCode != 0) throw await ProcessFailureAsync(restarted, restarted, [restarted.ExitCode]);
            }
            await AssertTicketCiphertextHasNoPlaintextAsync(cancellationToken);
            return int.Parse(await File.ReadAllTextAsync(counterPath, cancellationToken), System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            if (Directory.Exists(barrier)) Directory.Delete(barrier, recursive: true);
            if (File.Exists(counterPath)) File.Delete(counterPath);
        }

    }

    private Process StartProbe(params string[] arguments) => StartProbe(new Dictionary<string, string?>(), arguments);

    private Process StartProbe(IReadOnlyDictionary<string, string?> extraEnvironment, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(typeof(NetArcaWs.Persistence.Probe.ProbeMarker).Assembly.Location);
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        start.Environment["NETARCA_PERSISTENCE_DB"] = connectionString;
        start.Environment["NETARCA_PERSISTENCE_DB_KIND"] = kind;
        start.Environment["NETARCA_PERSISTENCE_DB_VERSION"] = version.ToString();
        foreach ((string name, string? value) in extraEnvironment) start.Environment[name] = value;
        return Process.Start(start) ?? throw new InvalidOperationException("Could not start independent persistence probe process.");
    }

    private static string CreateBarrierDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"netarcaws-probe-barrier-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static Dictionary<string, string?> BarrierEnvironment(string directory, int expected) => new()
    {
        ["NETARCA_PROBE_BARRIER_DIRECTORY"] = directory,
        ["NETARCA_PROBE_BARRIER_EXPECTED"] = expected.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };

    private static async Task ReleaseBarrierAsync(string directory, int expected, CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (Directory.GetFiles(directory, "*.ready").Length < expected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTimeOffset.UtcNow >= deadline) throw new TimeoutException("Independent persistence processes did not reach the start barrier.");
            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
        }
        await File.WriteAllTextAsync(Path.Combine(directory, "release"), "go", cancellationToken);
    }

    private async Task AssertTicketCiphertextHasNoPlaintextAsync(CancellationToken cancellationToken)
    {
        NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(x =>
            x.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1).AddWsaaTickets(ArcaService.Wsfev1, ArcaService.PadronA5));
        var options = new DbContextOptionsBuilder<ArcaWsDbContext>()
            .UseMySql(connectionString, kind == "mysql" ? new MySqlServerVersion(version) : new MariaDbServerVersion(version)).Options;
        await using var context = new ArcaWsDbContext(options, model);
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT HEX(Ciphertext) FROM NetArcaWsaaTickets LIMIT 1";
        string ciphertextHex = (string?)await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("The independent WSAA ticket processes did not persist a ticket row.");
        ciphertextHex.Should().NotContain(Convert.ToHexString(Encoding.UTF8.GetBytes("synthetic-token")));
        ciphertextHex.Should().NotContain(Convert.ToHexString(Encoding.UTF8.GetBytes("synthetic-signature")));
        await context.Database.CloseConnectionAsync();
    }

    private static async Task<InvalidOperationException> ProcessFailureAsync(Process left, Process right, int[] exitCodes)
    {
        string diagnostic = (await left.StandardError.ReadToEndAsync()) + (await right.StandardError.ReadToEndAsync());
        return new InvalidOperationException($"Independent persistence processes failed with exit codes {string.Join(',', exitCodes)}. {diagnostic}");
    }

    public async ValueTask DisposeAsync()
    {
        await services.DisposeAsync();
        // The database name was generated for this test run and contains only synthetic fixtures.
        var options = new DbContextOptionsBuilder<ArcaWsDbContext>()
            .UseMySql(connectionString, kind == "mysql" ? new MySqlServerVersion(version) : new MariaDbServerVersion(version)).Options;
        await using var context = new ArcaWsDbContext(options,
            NetArcaWsModelOptions.Configure(x => x.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1).AddWsaaTickets(ArcaService.Wsfev1, ArcaService.PadronA5)));
        await context.Database.EnsureDeletedAsync();
    }
}
