using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Invoicing;
using NetArcaWs.Services;
using NetArcaWs.Wsaa;
using Xunit;

namespace NetArcaWs.Tests.Invoicing;

public sealed class InvoicingServiceCollectionTests
{
    [Fact]
    public void AddNetArcaWsSqliteInvoicing_registers_journal_safe_service_and_all_service_interfaces()
    {
        using var database = new TemporaryDatabase();
        var services = new ServiceCollection();

        services.AddNetArcaWsSqliteInvoicing(InvoiceTestData.NewDatabasePath(database, "di"));
        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IInvoiceJournal>().Should().BeOfType<SqliteInvoiceJournal>();
        provider.GetRequiredService<SafeInvoiceService>().Should().NotBeNull();
        provider.GetRequiredService<IPadronA4Service>().Should().NotBeNull();
        provider.GetRequiredService<IPadronA5Service>().Should().NotBeNull();
        provider.GetRequiredService<IPadronA10Service>().Should().NotBeNull();
        provider.GetRequiredService<IPadronA13Service>().Should().NotBeNull();
        provider.GetRequiredService<IWsfev1Service>().Should().NotBeNull();
        provider.GetRequiredService<IWsfexv1Service>().Should().NotBeNull();
        provider.GetRequiredService<IWsmtxcav1Service>().Should().NotBeNull();
    }
}
