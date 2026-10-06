using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.MySql;
using NetArcaWs.EntityFrameworkCore.PostgreSql;
using NetArcaWs.EntityFrameworkCore.SqlServer;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Transport;
using NetArcaWs.Wsaa;

NetArcaWsModelOptions selection = NetArcaWsModelOptions.Configure(options =>
    options.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)
        .AddWsaaTickets(ArcaService.Wsfev1, ArcaService.PadronA5));
byte[] key = new byte[32];
string sqlitePath = Path.Combine(Path.GetTempPath(), $"netarcaws-package-smoke-{Guid.NewGuid():N}.sqlite3");
var services = new ServiceCollection();
services.AddDbContextFactory<ArcaWsDbContext>(options => options.UseSqlite($"Data Source={sqlitePath}"));
services.AddNetArcaWs();
services.AddSingleton<IWsaaTicketProtector>(new AesGcmWsaaTicketProtector("smoke", new Dictionary<string, byte[]> { ["smoke"] = key }));
services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(selection);
await using ServiceProvider provider = services.BuildServiceProvider();
await using (ArcaWsDbContext context = await provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync())
{
    await context.Database.EnsureCreatedAsync();
    string[] expectedTables = ["NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations", "NetArcaWsaaTickets"];
    string[] actualTables = context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).OfType<string>().ToArray();
    if (!expectedTables.All(actualTables.Contains)) throw new InvalidOperationException("Selected persistence tables were not included in the EF model.");
}

IInvoiceJournal journal = provider.GetRequiredService<IInvoiceJournal>();
var submission = new InvoiceSubmission(
    "consumer-smoke",
    "smoke-001",
    "wsfe",
    new InvoiceIdentity(ArcaEnvironment.Homologation, 30123456789, 1, 1, 1),
    "<synthetic>consumer-package-smoke</synthetic>");
InvoiceOperation prepared = await journal.PrepareAsync(submission);
InvoiceOperation? persisted = await journal.FindAsync(submission.TenantId, submission.IdempotencyKey);
if (prepared.State != InvoiceState.Prepared || persisted?.Submission != submission)
    throw new InvalidOperationException("The packaged EF invoice journal did not persist and reload the consumer submission.");
_ = provider.GetRequiredService<IArcaTicketProvider>();

var mysqlServices = new ServiceCollection();
mysqlServices.AddNetArcaWs();
mysqlServices.AddSingleton<IWsaaTicketProtector>(new AesGcmWsaaTicketProtector("smoke", new Dictionary<string, byte[]> { ["smoke"] = key }));
mysqlServices.AddNetArcaWsMySqlStores(
    "Server=127.0.0.1;Database=unused;User ID=unused",
    new MySqlServerVersion(new Version(8, 0, 0)),
    selection);
await using ServiceProvider mysqlProvider = mysqlServices.BuildServiceProvider();
_ = mysqlProvider.GetRequiredService<IInvoiceJournal>();
_ = mysqlProvider.GetRequiredService<IArcaTicketProvider>();
await using ArcaWsDbContext mysqlContext = await mysqlProvider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync();
_ = mysqlContext.Model;

var postgreSqlServices = new ServiceCollection();
postgreSqlServices.AddNetArcaWs();
postgreSqlServices.AddSingleton<IWsaaTicketProtector>(new AesGcmWsaaTicketProtector("smoke", new Dictionary<string, byte[]> { ["smoke"] = key }));
postgreSqlServices.AddNetArcaWsPostgreSqlStores("Host=127.0.0.1;Database=unused", selection);
await using ServiceProvider postgreSqlProvider = postgreSqlServices.BuildServiceProvider();
_ = postgreSqlProvider.GetRequiredService<IInvoiceJournal>();
_ = postgreSqlProvider.GetRequiredService<IArcaTicketProvider>();
await using ArcaWsDbContext postgreSqlContext = await postgreSqlProvider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync();
_ = postgreSqlContext.Model;

var sqlServerServices = new ServiceCollection();
sqlServerServices.AddNetArcaWs();
sqlServerServices.AddSingleton<IWsaaTicketProtector>(new AesGcmWsaaTicketProtector("smoke", new Dictionary<string, byte[]> { ["smoke"] = key }));
sqlServerServices.AddNetArcaWsSqlServerStores("Server=127.0.0.1;Database=unused;User Id=unused;Password=unused;Encrypt=True;TrustServerCertificate=True", selection);
await using ServiceProvider sqlServerProvider = sqlServerServices.BuildServiceProvider();
_ = sqlServerProvider.GetRequiredService<IInvoiceJournal>();
_ = sqlServerProvider.GetRequiredService<IArcaTicketProvider>();
await using ArcaWsDbContext sqlServerContext = await sqlServerProvider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync();
_ = sqlServerContext.Model;

SqliteConnection.ClearAllPools();
File.Delete(sqlitePath);
Console.WriteLine("Optional persistence package consumer smoke passed; no database server or ARCA endpoint was contacted.");
