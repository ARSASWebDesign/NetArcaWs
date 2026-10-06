using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Cryptography;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.MySql;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Transport;
using NetArcaWs.Wsaa;

namespace NetArcaWs.Persistence.Probe;

/// <summary>Marker type used by opt-in tests to launch this probe in independent OS processes.</summary>
public sealed class ProbeMarker;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            await ProbeStartBarrier.WaitIfConfiguredAsync();
            if (args is ["shared-ticket"])
                return await RunSharedTicketAsync();
            PersistenceConfiguration configuration = PersistenceConfiguration.FromEnvironment();
            NetArcaWsModelOptions selection = NetArcaWsModelOptions.Configure(options => options.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1));
            var services = new ServiceCollection();
            services.AddNetArcaWsMySqlStores(configuration.ConnectionString, configuration.ServerVersion, selection);
            await using ServiceProvider provider = services.BuildServiceProvider();
            IInvoiceJournal journal = provider.GetRequiredService<IInvoiceJournal>();
            if (args is ["claim", var tenant, var key])
                return await journal.TryAcquireAsync(tenant, key, TimeSpan.FromMinutes(1), false) is null ? 4 : 0;
            if (args.Length != 7 || args[0] != "prepare")
                throw new ArgumentException("Usage: prepare <tenant> <key> <service> <cuit> <point-of-sale> <voucher-number> | claim <tenant> <key>");
            var submission = new InvoiceSubmission(args[1], args[2], args[3],
                new InvoiceIdentity(ArcaEnvironment.Homologation, long.Parse(args[4]), int.Parse(args[5]), 1, long.Parse(args[6])),
                "<synthetic>independent-process</synthetic>");
            await journal.PrepareAsync(submission);
            return 0;
        }
        catch (InvoiceConflictException)
        {
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Persistence probe failed ({ex.GetType().Name}). Set the explicit NETARCA_PERSISTENCE_DB configuration and verify database schema setup.");
            return 2;
        }
    }

    private static async Task<int> RunSharedTicketAsync()
    {
        PersistenceConfiguration configuration = PersistenceConfiguration.FromEnvironment();
        string certificatePem = ProbeEnvironment.Required("NETARCA_PROBE_CERT_PEM");
        string privateKeyPem = ProbeEnvironment.Required("NETARCA_PROBE_PRIVATE_KEY_PEM");
        byte[] sharedKey = Convert.FromBase64String(ProbeEnvironment.Required("NETARCA_PROBE_AES_KEY"));
        string counterPath = ProbeEnvironment.Required("NETARCA_PROBE_LOGIN_COUNTER");
        string tenantId = ProbeEnvironment.Required("NETARCA_PROBE_TENANT_ID");
        NetArcaWsModelOptions selection = NetArcaWsModelOptions.Configure(options => options.AddWsaaTickets(ArcaService.Wsfev1));
        var services = new ServiceCollection();
        services.AddSingleton<IWsaaTicketProtector>(new AesGcmWsaaTicketProtector("probe", new Dictionary<string, byte[]> { ["probe"] = sharedKey }));
        services.AddSingleton<IWsaaTicketLoginClient>(new SyntheticLoginClient(counterPath));
        services.AddNetArcaWsMySqlStores(configuration.ConnectionString, configuration.ServerVersion, selection);
        await using ServiceProvider provider = services.BuildServiceProvider();
        var tenant = new ArcaTenantContext(tenantId, 20_123_456_789, ArcaEnvironment.Homologation,
            WsaaCertificateContent.FromPem(certificatePem, privateKeyPem));
        _ = await provider.GetRequiredService<IArcaTicketProvider>().GetTicketAsync("wsfe", tenant);
        return 0;
    }
}

internal sealed class SyntheticLoginClient(string counterPath) : IWsaaTicketLoginClient
{
    public async Task<WsaaTicket> LoginAsync(string service, ArcaTenantContext tenant, CancellationToken cancellationToken = default)
    {
        await IncrementCounterAsync(cancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(900), cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string generated = System.Xml.XmlConvert.ToString(now);
        string expires = System.Xml.XmlConvert.ToString(now.AddHours(1));
        string xml = $"<loginTicketResponse version=\"1.0\"><header><source>CN=synthetic</source><destination>CN=ARCA</destination><uniqueId>{Environment.ProcessId}</uniqueId><generationTime>{generated}</generationTime><expirationTime>{expires}</expirationTime></header><credentials><token>synthetic-token</token><sign>synthetic-signature</sign></credentials></loginTicketResponse>";
        return WsaaService.ParseTicket(xml);
    }

    private async Task IncrementCounterAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var stream = new FileStream(counterPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None,
                    bufferSize: 128, FileOptions.Asynchronous);
                using var reader = new StreamReader(stream, leaveOpen: true);
                string existing = await reader.ReadToEndAsync(cancellationToken);
                int next = int.TryParse(existing, out int current) ? current + 1 : 1;
                stream.SetLength(0);
                stream.Position = 0;
                await using var writer = new StreamWriter(stream, leaveOpen: true);
                await writer.WriteAsync(next.ToString(System.Globalization.CultureInfo.InvariantCulture).AsMemory(), cancellationToken);
                await writer.FlushAsync(cancellationToken);
                return;
            }
            catch (IOException)
            {
                await Task.Delay(25, cancellationToken);
            }
        }
    }
}

internal sealed record PersistenceConfiguration(string ConnectionString, ServerVersion ServerVersion)
{
    public static PersistenceConfiguration FromEnvironment()
    {
        string connectionString = ProbeEnvironment.Required("NETARCA_PERSISTENCE_DB");
        string kind = ProbeEnvironment.Required("NETARCA_PERSISTENCE_DB_KIND");
        string version = ProbeEnvironment.Required("NETARCA_PERSISTENCE_DB_VERSION");
        if (!Version.TryParse(version, out Version? parsed)) throw new InvalidOperationException("NETARCA_PERSISTENCE_DB_VERSION must be numeric.");
        ServerVersion serverVersion = kind switch
        {
            "mysql" => new MySqlServerVersion(parsed),
            "mariadb" => new MariaDbServerVersion(parsed),
            _ => throw new InvalidOperationException("NETARCA_PERSISTENCE_DB_KIND must be mysql or mariadb.")
        };
        return new PersistenceConfiguration(connectionString, serverVersion);
    }

}

internal static class ProbeEnvironment
{
    public static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"{name} is required.");
}

internal static class ProbeStartBarrier
{
    public static async Task WaitIfConfiguredAsync()
    {
        string? directory = Environment.GetEnvironmentVariable("NETARCA_PROBE_BARRIER_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!int.TryParse(Environment.GetEnvironmentVariable("NETARCA_PROBE_BARRIER_EXPECTED"), out int expected) || expected < 2)
            throw new InvalidOperationException("The process barrier configuration is invalid.");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, $"{Environment.ProcessId}-{Guid.NewGuid():N}.ready"), "ready");
        string releasePath = Path.Combine(directory, "release");
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (!File.Exists(releasePath))
        {
            if (DateTimeOffset.UtcNow >= deadline) throw new TimeoutException("Timed out waiting at the independent-process probe barrier.");
            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }
    }
}
