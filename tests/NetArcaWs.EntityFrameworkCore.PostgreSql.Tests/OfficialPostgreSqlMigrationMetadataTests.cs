using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.Migrations;
using NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql;
using NetArcaWs.HealthChecks;
using NetArcaWs.Services;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.PostgreSql.Tests;

public sealed class OfficialPostgreSqlMigrationMetadataTests
{
    [Theory]
    [InlineData(NetArcaWsPersistenceModule.Invoicing, "__NetArcaWsInvoiceMigrations")]
    [InlineData(NetArcaWsPersistenceModule.WsaaTickets, "__NetArcaWsTicketMigrations")]
    [InlineData(NetArcaWsPersistenceModule.InvoiceRecovery, "__NetArcaWsInvoiceRecoveryMigrations")]
    public void PostgreSql_module_has_an_independent_initial_migration_snapshot_and_default_schema_history(
        NetArcaWsPersistenceModule module, string historyTable)
    {
        using DbContext context = Factory().CreateContext(module);

        context.Model.GetDefaultSchema().Should().Be("public");
        context.Database.HasPendingModelChanges().Should().BeFalse();
        context.Database.GetMigrations().Should().ContainSingle();
        context.GetService<IMigrationsAssembly>().ModelSnapshot.Should().NotBeNull();
        IMigrationsAssembly migrations = context.GetService<IMigrationsAssembly>();
        Migration initial = migrations.CreateMigration(migrations.Migrations.Single().Value, context.Database.ProviderName!);
        IReadOnlyList<MigrationOperation> operations = initial.UpOperations;
        operations.OfType<CreateTableOperation>().Select(table => table.Name).Should().BeEquivalentTo(module switch
        {
            NetArcaWsPersistenceModule.Invoicing => ["NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations"],
            NetArcaWsPersistenceModule.WsaaTickets => ["NetArcaWsaaTickets"],
            _ => ["NetArcaInvoiceRecoveryJobs"]
        });
        operations.OfType<CreateTableOperation>().Should().OnlyContain(table => table.Schema == "public");
        operations.Should().NotContain(operation => operation is RenameTableOperation);
        context.GetService<IHistoryRepository>().GetCreateScript().Should().Contain($"public.\"{historyTable}\"");
        context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).Should().BeEquivalentTo(module switch
        {
            NetArcaWsPersistenceModule.Invoicing => ["NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations"],
            NetArcaWsPersistenceModule.WsaaTickets => ["NetArcaWsaaTickets"],
            _ => ["NetArcaInvoiceRecoveryJobs"]
        });
    }

    [Fact]
    public void PostgreSql_remote_hash_unique_index_keeps_nullable_values_and_invoice_types()
    {
        using DbContext context = Factory().CreateContext(NetArcaWsPersistenceModule.Invoicing);
        var invoice = context.Model.GetEntityTypes().Single(entity => entity.GetTableName() == "NetArcaInvoices");
        var properties = invoice.GetProperties().ToDictionary(property => property.Name, StringComparer.Ordinal);
        var remoteHash = invoice.GetIndexes().Single(index => index.Properties.Any(property => property.Name == "RemoteHash"));
        IMigrationsAssembly migrations = context.GetService<IMigrationsAssembly>();
        Migration initial = migrations.CreateMigration(migrations.Migrations.Single().Value, context.Database.ProviderName!);
        var remoteIndex = initial.UpOperations.OfType<CreateIndexOperation>().Single(index => index.Columns.Contains("RemoteHash"));

        properties["RemoteHash"].IsNullable.Should().BeTrue();
        properties["RemoteHash"].GetMaxLength().Should().Be(64);
        remoteHash.IsUnique.Should().BeTrue();
        remoteHash.GetFilter().Should().BeNull();
        remoteIndex.IsUnique.Should().BeTrue();
        remoteIndex.Filter.Should().BeNull();
        properties["Payload"].GetRelationalTypeMapping().StoreType.Should().Be("text");
        properties["TenantId"].GetMaxLength().Should().Be(128);
        context.Database.GenerateCreateScript().Should().Contain("CREATE UNIQUE INDEX \"UX_NetArcaInvoices_Remote\"")
            .And.NotContain("WHERE \"RemoteHash\" IS NOT NULL");
    }

    [Fact]
    public async Task Registration_and_script_generation_do_not_connect_and_table_inspection_rejects_unowned_names()
    {
        var services = new ServiceCollection();
        services.AddNetArcaWsPostgreSqlMigrations(ConnectionString, BothModules());
        using ServiceProvider provider = services.BuildServiceProvider();
        INetArcaWsMigrator migrator = provider.GetRequiredService<INetArcaWsMigrator>();
        provider.GetRequiredService<INetArcaWsMigrationContextFactory>().Provider.Should().Be(NetArcaWsMigrationProvider.PostgreSql);
        string invoiceScript = migrator.GenerateScript(NetArcaWsPersistenceModule.Invoicing);
        invoiceScript.Should().Contain("CREATE TABLE public.\"NetArcaInvoices\"")
            .And.Contain("public.\"__NetArcaWsInvoiceMigrations\"").And.NotContain("NetArcaWsaaTickets");
        migrator.GenerateScript(NetArcaWsPersistenceModule.WsaaTickets, idempotent: true).Should()
            .Contain("20261006000200_InitialWsaaTickets").And.NotContain("NetArcaInvoices");

        using DbContext context = Factory().CreateContext(NetArcaWsPersistenceModule.Invoicing);
        PostgreSqlMigrationContextFactory factory = Factory();
        (await factory.GetPresentTablesAsync(context, Array.Empty<string>(), TestContext.Current.CancellationToken)).Should().BeEmpty();
        Func<Task> inspect = () => factory.GetPresentTablesAsync(context, ["consumer_table"], TestContext.Current.CancellationToken);
        await inspect.Should().ThrowAsync<ArgumentException>();
        context.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);
    }

    private const string ConnectionString = "Host=127.0.0.1;Port=1;Database=must_not_connect;Username=synthetic;Password=synthetic";
    private static PostgreSqlMigrationContextFactory Factory() => new(ConnectionString);
    private static NetArcaWsModelOptions BothModules() => NetArcaWsModelOptions.Configure(options =>
        options.AddInvoicing(ArcaService.Wsfev1).AddWsaaTickets(ArcaService.Wsfev1));
}
