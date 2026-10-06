using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;

namespace NetArcaWs.Tests.Invoicing;

internal static class InvoiceTestData
{
    internal static InvoiceSubmission Submission(
        string tenantId = "tenant-a",
        string idempotencyKey = "invoice-001",
        string service = "wsfe",
        InvoiceIdentity? identity = null,
        string payload = "<invoice><total>121.00</total></invoice>") => new(
            tenantId,
            idempotencyKey,
            service,
            identity ?? Identity(),
            payload);

    internal static InvoiceIdentity Identity(
        ArcaEnvironment environment = ArcaEnvironment.Homologation,
        long cuit = 30123456789,
        int pointOfSale = 1,
        int voucherType = 1,
        long voucherNumber = 1) => new(environment, cuit, pointOfSale, voucherType, voucherNumber);

    internal static string NewDatabasePath(TemporaryDatabase database, string label) =>
        System.IO.Path.Combine(database.Path, $"{label}.sqlite3");
}

internal sealed class TemporaryDatabase : IDisposable
{
    internal TemporaryDatabase()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NetArcaWs.Invoice.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    internal string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
    }
}
