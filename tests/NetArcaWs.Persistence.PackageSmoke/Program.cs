using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.Migrations;
using NetArcaWs.EntityFrameworkCore.Migrations.MariaDb;
using NetArcaWs.EntityFrameworkCore.Migrations.MySql;
using NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql;
using NetArcaWs.EntityFrameworkCore.Migrations.Sqlite;
using NetArcaWs.EntityFrameworkCore.Migrations.SqlServer;
using NetArcaWs.EntityFrameworkCore.MySql;
using NetArcaWs.EntityFrameworkCore.PostgreSql;
using NetArcaWs.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Transport;
using NetArcaWs.Wsaa;
using NetArcaWs.Cryptography;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using NetArcaWs.Contracts.WsfeV1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Reflection;

NetArcaWsModelOptions selection = NetArcaWsModelOptions.Configure(options =>
    options.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)
        .AddWsaaTickets(ArcaService.Wsfev1, ArcaService.PadronA5)
        .AddCertificates()
        .AddInvoiceRecovery(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca));
NetArcaWsModelOptions certificateOnly = NetArcaWsModelOptions.Configure(options => options.AddCertificates());
byte[] key = new byte[32];
RandomNumberGenerator.Fill(key);
string sqlitePath = Path.Combine(Path.GetTempPath(), $"netarcaws-package-smoke-{Guid.NewGuid():N}.sqlite3");
var services = new ServiceCollection();
services.AddDbContextFactory<ArcaWsDbContext>(options => options.UseSqlite($"Data Source={sqlitePath}"));
services.AddNetArcaWs();
services.AddSingleton<IWsaaTicketProtector>(new AesGcmWsaaTicketProtector("smoke", new Dictionary<string, byte[]> { ["smoke"] = key }));
services.AddSingleton<IArcaCertificateProtector>(_ => new AesGcmArcaCertificateProtector("certificate-smoke", new Dictionary<string, byte[]> { ["certificate-smoke"] = key }));
services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(selection);
services.AddLogging();
services.AddSingleton<IWsfev1Service>(_ =>
{
    IWsfev1Service proxy = DispatchProxy.Create<IWsfev1Service, SyntheticWsfeProxy>();
    return proxy;
});
services.AddScoped<IInvoiceRecoveryContextResolver, PinnedCertificateResolver>();
services.AddScoped(sp => new SafeInvoiceService(new InvoiceCoordinator(sp.GetRequiredService<IInvoiceJournal>()),
    sp.GetRequiredService<IWsfev1Service>(), null!, null!));
services.AddNetArcaWsInvoiceRecoveryWorker(new InvoiceRecoveryScope("consumer-smoke", [ArcaService.Wsfev1]));
await using ServiceProvider provider = services.BuildServiceProvider();
string sqliteConnectionString = $"Data Source={sqlitePath}";
var migrationServices = new ServiceCollection();
migrationServices.AddNetArcaWsSqliteMigrations(sqliteConnectionString, selection);
await using ServiceProvider migrationProvider = migrationServices.BuildServiceProvider();
NetArcaWsMigrationStatus migrationStatus = await migrationProvider.GetRequiredService<INetArcaWsMigrator>().ApplyAsync();
if (migrationStatus.Modules.Count != 4 || migrationStatus.Modules.Any(module => module.State != NetArcaWsMigrationState.Current))
    throw new InvalidOperationException("The installed SQLite migration packages did not apply all four selected modules.");
await using (ArcaWsDbContext context = await provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync())
{
    string[] expectedTables = ["NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations", "NetArcaWsaaTickets", "NetArcaCertificateSlots", "NetArcaCertificateVersions", "NetArcaInvoiceRecoveryJobs"];
    string[] actualTables = context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).OfType<string>().ToArray();
    if (!expectedTables.All(actualTables.Contains)) throw new InvalidOperationException("Selected persistence tables were not included in the EF model.");
}

IInvoiceJournal journal = provider.GetRequiredService<IInvoiceJournal>();
var submission = new InvoiceSubmission("consumer-smoke", "smoke-001", "wsfe",
    new InvoiceIdentity(ArcaEnvironment.Homologation, 30123456789, 2, 1, 1), "<synthetic>consumer-package-smoke</synthetic>");
InvoiceOperation prepared = await journal.PrepareAsync(submission);
InvoiceOperation? persisted = await journal.FindAsync(submission.TenantId, submission.IdempotencyKey);
if (prepared.State != InvoiceState.Prepared || persisted?.Submission != submission)
    throw new InvalidOperationException("The packaged EF invoice journal did not persist and reload the consumer submission.");
_ = provider.GetRequiredService<IArcaTicketProvider>();
IArcaCertificateStore certificateStore = provider.GetRequiredService<IArcaCertificateStore>();
var certificateScope = new ArcaCertificateScope("consumer-smoke", 30_123_456_789, ArcaEnvironment.Homologation);
ArcaCertificateVersion firstCertificate = await certificateStore.RotateAsync(certificateScope, CreateCertificateContent());
ArcaStoredCertificate? activeCertificate = await certificateStore.GetActiveAsync(certificateScope);
if (activeCertificate is null || activeCertificate.Metadata.VersionId != firstCertificate.VersionId || activeCertificate.Content.ToString().Contains("PRIVATE KEY", StringComparison.Ordinal))
    throw new InvalidOperationException("The installed EF certificate store did not round-trip a protected version safely.");
var hostPreparedOperation = new ConsumerPreparedOperation(
    submission.TenantId, submission.IdempotencyKey, prepared.State.ToString(), activeCertificate.Metadata.VersionId);
string hostRecordPath = Path.Combine(Path.GetTempPath(), $"netarcaws-consumer-operation-{Guid.NewGuid():N}.json");
await File.WriteAllTextAsync(hostRecordPath, JsonSerializer.Serialize(hostPreparedOperation));

ArcaCertificateVersion rotatedCertificate = await certificateStore.RotateAsync(
    certificateScope, CreateCertificateContent(), expectedActiveVersionId: firstCertificate.VersionId);
ConsumerPreparedOperation? reloadedHostOperation = JsonSerializer.Deserialize<ConsumerPreparedOperation>(await File.ReadAllTextAsync(hostRecordPath));
if (reloadedHostOperation is null || reloadedHostOperation.CertificateVersionId != firstCertificate.VersionId)
    throw new InvalidOperationException("The application-owned prepared operation did not retain its selected certificate version.");
ArcaStoredCertificate? historicalCertificate = await certificateStore.GetVersionAsync(certificateScope, reloadedHostOperation.CertificateVersionId);
ArcaStoredCertificate? activeAfterRotation = await certificateStore.GetActiveAsync(certificateScope);
if (historicalCertificate is null || activeAfterRotation is null || historicalCertificate.Metadata.VersionId != firstCertificate.VersionId ||
    historicalCertificate.Metadata.IsActive || activeAfterRotation.Metadata.VersionId != rotatedCertificate.VersionId)
    throw new InvalidOperationException("The consumer did not resolve its exact historical certificate version after rotation.");
InvoiceOperation? invoiceAfterRotation = await journal.FindAsync(submission.TenantId, submission.IdempotencyKey);
if (invoiceAfterRotation?.Submission != persisted?.Submission || invoiceAfterRotation?.Submission != submission)
    throw new InvalidOperationException("Certificate rotation changed the prepared invoice payload stored in the existing journal.");
var authorizedContext = new ArcaTenantContext(
    certificateScope.TenantId, certificateScope.Cuit, certificateScope.Environment, historicalCertificate.Content);
if (authorizedContext.Certificate != historicalCertificate.Content)
    throw new InvalidOperationException("The consumer could not construct its operation context from the pinned historical version.");

var recoveryRequest = new FecaeRequest
{
    FeCabReq = new FecaeCabRequest { CantReg = 1, PtoVta = 3, CbteTipo = 1 },
    FeDetReq = { new FecaeDetRequest
    {
        Concepto = 1, DocTipo = 80, DocNro = 20304050607, CbteDesde = 2, CbteHasta = 2,
        CbteFch = "20261007", ImpTotal = 121, ImpTotConc = 0, ImpNeto = 100, ImpOpEx = 0,
        ImpIva = 21, ImpTrib = 0, MonId = "PES", MonCotiz = 1
    } }
};
InvoiceSubmission recoverySubmission = SafeInvoiceService.CreateWsfeSubmission(authorizedContext, "recovery-smoke", recoveryRequest);
IInvoiceRecoveryQueue recoveryQueue = provider.GetRequiredService<IInvoiceRecoveryQueue>();
await recoveryQueue.PrepareAndEnqueueAsync(recoverySubmission, firstCertificate.VersionId.ToString("D"));
var recoveryScope = new InvoiceRecoveryScope("consumer-smoke", [ArcaService.Wsfev1]);
InvoiceRecoveryMetadata? queuedMetadata = await recoveryQueue.FindAsync(recoveryScope, "recovery-smoke");
if (queuedMetadata?.State != InvoiceRecoveryState.Scheduled || queuedMetadata.Attempt != 0)
    throw new InvalidOperationException("The package consumer could not inspect scheduled recovery metadata.");
using (IServiceScope recoveryServiceScope = provider.CreateScope())
{
    bool claimed = await recoveryServiceScope.ServiceProvider.GetRequiredService<IInvoiceRecoveryProcessor>().RunOnceAsync(recoveryScope);
    InvoiceOperation? recovered = await journal.FindAsync("consumer-smoke", "recovery-smoke");
    InvoiceRecoveryMetadata? completedMetadata = await recoveryQueue.FindAsync(recoveryScope, "recovery-smoke");
    if (!claimed || recovered?.State != InvoiceState.Authorized || completedMetadata?.State != InvoiceRecoveryState.Completed)
        throw new InvalidOperationException("The package consumer did not process its queued synthetic SOAP authorization.");
}
File.Delete(hostRecordPath);

var sqliteCertificateOnlyServices = new ServiceCollection();
sqliteCertificateOnlyServices.AddDbContextFactory<ArcaWsDbContext>(options => options.UseSqlite(sqliteConnectionString));
sqliteCertificateOnlyServices.AddSingleton<IArcaCertificateProtector>(_ => new AesGcmArcaCertificateProtector("certificate-smoke", new Dictionary<string, byte[]> { ["certificate-smoke"] = key }));
sqliteCertificateOnlyServices.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(certificateOnly);
await using ServiceProvider sqliteCertificateOnlyProvider = sqliteCertificateOnlyServices.BuildServiceProvider();
_ = sqliteCertificateOnlyProvider.GetRequiredService<IArcaCertificateStore>();
await using (ArcaWsDbContext context = await sqliteCertificateOnlyProvider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync())
    AssertCertificateOnlyModel(context);

var mysqlServices = new ServiceCollection();
mysqlServices.AddSingleton<IArcaCertificateProtector>(_ => new AesGcmArcaCertificateProtector("certificate-smoke", new Dictionary<string, byte[]> { ["certificate-smoke"] = key }));
mysqlServices.AddNetArcaWsMySqlStores(
    "Server=127.0.0.1;Database=unused;User ID=unused",
    new MySqlServerVersion(new Version(8, 0, 0)),
    certificateOnly);
await using ServiceProvider mysqlProvider = mysqlServices.BuildServiceProvider();
_ = mysqlProvider.GetRequiredService<IArcaCertificateStore>();
await using ArcaWsDbContext mysqlContext = await mysqlProvider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync();
AssertCertificateOnlyModel(mysqlContext);

await VerifyAllMigrationModulesAsync(new SqliteMigrationContextFactory(sqliteConnectionString));

var postgreSqlServices = new ServiceCollection();
postgreSqlServices.AddSingleton<IArcaCertificateProtector>(_ => new AesGcmArcaCertificateProtector("certificate-smoke", new Dictionary<string, byte[]> { ["certificate-smoke"] = key }));
postgreSqlServices.AddNetArcaWsPostgreSqlStores("Host=127.0.0.1;Database=unused", certificateOnly);
await using ServiceProvider postgreSqlProvider = postgreSqlServices.BuildServiceProvider();
_ = postgreSqlProvider.GetRequiredService<IArcaCertificateStore>();
await using ArcaWsDbContext postgreSqlContext = await postgreSqlProvider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync();
AssertCertificateOnlyModel(postgreSqlContext);

var mariaDbServices = new ServiceCollection();
mariaDbServices.AddSingleton<IArcaCertificateProtector>(_ => new AesGcmArcaCertificateProtector("certificate-smoke", new Dictionary<string, byte[]> { ["certificate-smoke"] = key }));
mariaDbServices.AddNetArcaWsMySqlStores("Server=127.0.0.1;Database=unused;User ID=unused", new MariaDbServerVersion(new Version(11, 4, 13)), certificateOnly);
await using ServiceProvider mariaDbProvider = mariaDbServices.BuildServiceProvider();
_ = mariaDbProvider.GetRequiredService<IArcaCertificateStore>();
await using ArcaWsDbContext mariaDbContext = await mariaDbProvider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync();
AssertCertificateOnlyModel(mariaDbContext);

await VerifyAllMigrationModulesAsync(new MySqlMigrationContextFactory("Server=localhost;Database=unused", new MySqlServerVersion(new Version(8, 4, 11))));
await VerifyAllMigrationModulesAsync(new MariaDbMigrationContextFactory("Server=localhost;Database=unused", new MariaDbServerVersion(new Version(11, 4, 13))));
await VerifyAllMigrationModulesAsync(new PostgreSqlMigrationContextFactory("Host=localhost;Database=unused"));
await VerifyAllMigrationModulesAsync(new SqlServerMigrationContextFactory("Server=localhost;Database=unused;Encrypt=True;TrustServerCertificate=True"));

var sqlServerServices = new ServiceCollection();
sqlServerServices.AddSingleton<IArcaCertificateProtector>(_ => new AesGcmArcaCertificateProtector("certificate-smoke", new Dictionary<string, byte[]> { ["certificate-smoke"] = key }));
sqlServerServices.AddNetArcaWsSqlServerStores("Server=127.0.0.1;Database=unused;User Id=unused;Password=unused;Encrypt=True;TrustServerCertificate=True", certificateOnly);
await using ServiceProvider sqlServerProvider = sqlServerServices.BuildServiceProvider();
_ = sqlServerProvider.GetRequiredService<IArcaCertificateStore>();
await using ArcaWsDbContext sqlServerContext = await sqlServerProvider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync();
AssertCertificateOnlyModel(sqlServerContext);

SqliteConnection.ClearAllPools();
File.Delete(sqlitePath);
Console.WriteLine("Optional persistence package consumer smoke passed; no database server or ARCA endpoint was contacted.");

static async Task VerifyMigrationPackageAsync(INetArcaWsMigrationContextFactory factory, NetArcaWsPersistenceModule module)
{
    await using DbContext context = factory.CreateContext(module);
    string[] migrationIds = context.Database.GetMigrations().ToArray();
    if (migrationIds.Length != 1) throw new InvalidOperationException($"The installed {factory.Provider} migration package did not expose its initial migration.");
    string script = context.GetService<IMigrator>().GenerateScript("0", null, MigrationsSqlGenerationOptions.Default);
    string expectedTable = module switch
    {
        NetArcaWsPersistenceModule.Invoicing => "NetArcaInvoices",
        NetArcaWsPersistenceModule.WsaaTickets => "NetArcaWsaaTickets",
        NetArcaWsPersistenceModule.TenantCertificates => "NetArcaCertificateSlots",
        NetArcaWsPersistenceModule.InvoiceRecovery => "NetArcaInvoiceRecoveryJobs",
        _ => throw new ArgumentOutOfRangeException(nameof(module))
    };
    if (!script.Contains(migrationIds[0], StringComparison.Ordinal) || !script.Contains(expectedTable, StringComparison.Ordinal))
        throw new InvalidOperationException($"The installed {factory.Provider} migration package could not generate its offline SQL.");
}

static async Task VerifyAllMigrationModulesAsync(INetArcaWsMigrationContextFactory factory)
{
    foreach (NetArcaWsPersistenceModule module in Enum.GetValues<NetArcaWsPersistenceModule>())
        await VerifyMigrationPackageAsync(factory, module);
    await using DbContext recoveryOnly = factory.CreateContext(NetArcaWsPersistenceModule.InvoiceRecovery);
    string[] tables = recoveryOnly.Model.GetEntityTypes().Select(entity => entity.GetTableName()).OfType<string>().ToArray();
    if (!tables.Contains("NetArcaInvoiceRecoveryJobs", StringComparer.Ordinal) || tables.Contains("NetArcaInvoices", StringComparer.Ordinal))
        throw new InvalidOperationException($"The {factory.Provider} recovery migration module was not isolated from invoicing tables.");
}

static void AssertCertificateOnlyModel(DbContext context)
{
    string[] tables = context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).OfType<string>().Order(StringComparer.Ordinal).ToArray();
    if (!tables.SequenceEqual(new[] { "NetArcaCertificateSlots", "NetArcaCertificateVersions" }, StringComparer.Ordinal))
        throw new InvalidOperationException("Certificate-only options included unrelated EF tables.");
}

static WsaaCertificateContent CreateCertificateContent()
{
    using RSA key = RSA.Create(2048);
    var request = new CertificateRequest("CN=netarcaws-package-smoke", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
    return WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), key.ExportPkcs8PrivateKeyPem());
}

sealed record ConsumerPreparedOperation(string TenantId, string IdempotencyKey, string State, Guid CertificateVersionId);

sealed class PinnedCertificateResolver(IArcaCertificateStore certificateStore) : IInvoiceRecoveryContextResolver
{
    public async ValueTask<ArcaTenantContext> ResolveAsync(InvoiceRecoveryWorkItem workItem, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(workItem.CredentialReference, out Guid versionId))
            throw new InvalidOperationException("The synthetic recovery job did not retain its pinned credential reference.");
        var scope = new ArcaCertificateScope(workItem.TenantId, workItem.Identity.Cuit, workItem.Identity.Environment);
        ArcaStoredCertificate? pinned = await certificateStore.GetVersionAsync(scope, versionId, cancellationToken);
        return pinned is null
            ? throw new InvalidOperationException("The pinned synthetic certificate version was not found.")
            : new ArcaTenantContext(workItem.TenantId, workItem.Identity.Cuit, workItem.Identity.Environment, pinned.Content);
    }
}

class SyntheticWsfeProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod?.Name != nameof(IWsfev1Service.FECAESolicitarAsync))
            throw new NotSupportedException("The synthetic consumer smoke only supports one WSFE authorization operation.");
        var response = new FecaeSolicitarResponse
        {
            FecaeSolicitarResult = new FecaeResponse
            {
                FeCabResp = new FecaeCabResponse { Cuit = 30123456789, PtoVta = 3, CbteTipo = 1, CantReg = 1, Resultado = "A" },
                FeDetResp = { new FecaeDetResponse
                {
                    Concepto = 1, DocTipo = 80, DocNro = 20304050607, CbteDesde = 2, CbteHasta = 2,
                    CbteFch = "20261007", Resultado = "A", Cae = "71234567890123"
                } }
            }
        };
        return Task.FromResult(response);
    }
}
