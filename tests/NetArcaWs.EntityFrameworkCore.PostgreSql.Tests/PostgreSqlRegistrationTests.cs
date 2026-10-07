using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.PostgreSql;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Transport;
using NetArcaWs.Wsaa;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.PostgreSql.Tests;

public sealed class PostgreSqlRegistrationTests
{
    [Fact]
    public void Registration_creates_factory_and_selected_store_without_connecting_or_creating_schema()
    {
        NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(options =>
            options.AddInvoicing(ArcaService.Wsfev1));
        var services = new ServiceCollection();

        services.AddNetArcaWsPostgreSqlStores(
            "Host=127.0.0.1;Port=1;Database=never_connect;Username=unused;Password=unused",
            model);

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().Should().NotBeNull();
        provider.GetRequiredService<IInvoiceJournal>().Should().NotBeNull();
        using ArcaWsDbContext context = provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContext();
        context.Model.FindEntityType("NetArcaWs.EntityFrameworkCore.InvoiceJournalEntity")!
            .GetTableName().Should().Be("NetArcaInvoices");
        context.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);
    }

    [Fact]
    public void Ticket_only_registration_uses_ticket_selection_and_rejects_empty_models()
    {
        NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(options =>
            options.AddWsaaTickets(ArcaService.Wsfev1, ArcaService.PadronA5));
        var services = new ServiceCollection();
        services.AddSingleton<IWsaaTicketProtector>(new TestProtector());
        services.AddSingleton<IWsaaTicketLoginClient, TestLoginClient>();

        services.AddNetArcaWsPostgreSqlStores(
            "Host=127.0.0.1;Port=1;Database=never_connect;Username=unused;Password=unused",
            model);

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<IInvoiceJournal>().Should().BeNull();
        provider.GetRequiredService<IArcaTicketProvider>().Should().NotBeNull();
        using ArcaWsDbContext context = provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContext();
        context.Model.FindEntityType("NetArcaWs.EntityFrameworkCore.WsaaTicketEntity")!
            .GetTableName().Should().Be("NetArcaWsaaTickets");
        context.Model.FindEntityType("NetArcaWs.EntityFrameworkCore.InvoiceJournalEntity").Should().BeNull();
    }

    [Fact]
    public void Registration_rejects_an_empty_capability_selection()
    {
        var services = new ServiceCollection();
        NetArcaWsModelOptions empty = NetArcaWsModelOptions.Configure(_ => { });

        Action register = () => services.AddNetArcaWsPostgreSqlStores(
            "Host=127.0.0.1;Database=unused;Username=unused;Password=unused",
            empty);

        register.Should().Throw<ArgumentException>();
    }

    private sealed class TestProtector : IWsaaTicketProtector
    {
        public WsaaProtectedPayload Protect(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData)
            => throw new NotSupportedException();

        public byte[] Unprotect(WsaaProtectedPayload payload, ReadOnlySpan<byte> associatedData)
            => throw new NotSupportedException();
    }

    private sealed class TestLoginClient : IWsaaTicketLoginClient
    {
        public Task<WsaaTicket> LoginAsync(string service, ArcaTenantContext authorizedTenant, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
