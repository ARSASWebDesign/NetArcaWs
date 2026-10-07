using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.Migrations;
using NetArcaWs.EntityFrameworkCore.Migrations.MariaDb;
using NetArcaWs.EntityFrameworkCore.Migrations.MySql;
using NetArcaWs.EntityFrameworkCore.MySql;
using NetArcaWs.HealthChecks;
using NetArcaWs.Services;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.MySql.Tests;

public sealed class OfficialMySqlMigrationMetadataTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MySqlMigrationContextsHaveIndependentHistoryAndStableModuleModels(bool wsaaTickets)
    {
        using DbContext context = MySqlContextFactory().CreateContext(wsaaTickets
            ? NetArcaWsPersistenceModule.WsaaTickets
            : NetArcaWsPersistenceModule.Invoicing);

        AssertMigrationContext(context, wsaaTickets ? "__NetArcaWsTicketMigrations" : "__NetArcaWsInvoiceMigrations");
        context.Database.HasPendingModelChanges().Should().BeFalse();
        context.Database.GetMigrations().Should().ContainSingle();
        context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).Should().BeEquivalentTo(
            wsaaTickets ? ["NetArcaWsaaTickets"] : ["NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations"]);
    }

    [Fact]
    public void MySqlTenantCertificateMigrationMetadataContainsOnlyItsOwnedTables()
    {
        AssertCertificateMigration(MySqlContextFactory().CreateContext(NetArcaWsPersistenceModule.TenantCertificates));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MariaDbMigrationContextsHaveIndependentHistoryAndStableModuleModels(bool wsaaTickets)
    {
        using DbContext context = MariaDbContextFactory().CreateContext(wsaaTickets
            ? NetArcaWsPersistenceModule.WsaaTickets
            : NetArcaWsPersistenceModule.Invoicing);

        AssertMigrationContext(context, wsaaTickets ? "__NetArcaWsTicketMigrations" : "__NetArcaWsInvoiceMigrations");
        context.Database.HasPendingModelChanges().Should().BeFalse();
        context.Database.GetMigrations().Should().ContainSingle();
        context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).Should().BeEquivalentTo(
            wsaaTickets ? ["NetArcaWsaaTickets"] : ["NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations"]);
    }

    [Fact]
    public void MariaDbTenantCertificateMigrationMetadataContainsOnlyItsOwnedTables()
    {
        AssertCertificateMigration(MariaDbContextFactory().CreateContext(NetArcaWsPersistenceModule.TenantCertificates));
    }

    [Fact]
    public void MySqlAndMariaDbInvoiceRecoveryMigrationsContainOnlyTheQueueAndIndependentHistory()
    {
        AssertInvoiceRecoveryMigration(MySqlContextFactory().CreateContext(NetArcaWsPersistenceModule.InvoiceRecovery));
        AssertInvoiceRecoveryMigration(MariaDbContextFactory().CreateContext(NetArcaWsPersistenceModule.InvoiceRecovery));
    }

    [Fact]
    public void MySqlAndMariaDbModuleModelsPreserveColumnsIndexesAndPrecisions()
    {
        foreach (NetArcaWsPersistenceModule module in Enum.GetValues<NetArcaWsPersistenceModule>())
        {
            using DbContext mySql = MySqlContextFactory().CreateContext(module);
            using DbContext mariaDb = MariaDbContextFactory().CreateContext(module);

            ModelShape(mySql).Should().BeEquivalentTo(ModelShape(mariaDb));
        }
    }

    [Fact]
    public void DependencyRegistrationAndSqlGenerationDoNotConnect()
    {
        const string connectionString = "Server=127.0.0.1;Port=1;Database=must_not_connect;User ID=synthetic;Password=synthetic";
        NetArcaWsModelOptions model = BothModules();
        using ServiceProvider mySql = new ServiceCollection()
            .AddNetArcaWsMySqlMigrations(connectionString, new MySqlServerVersion(new Version(8, 4, 11)), model)
            .BuildServiceProvider();
        using ServiceProvider mariaDb = new ServiceCollection()
            .AddNetArcaWsMariaDbMigrations(connectionString, new MariaDbServerVersion(new Version(11, 4, 13)), model)
            .BuildServiceProvider();

        INetArcaWsMigrator mySqlMigrator = mySql.GetRequiredService<INetArcaWsMigrator>();
        INetArcaWsMigrator mariaDbMigrator = mariaDb.GetRequiredService<INetArcaWsMigrator>();
        mySql.GetRequiredService<INetArcaWsMigrationContextFactory>().Provider.Should().Be(NetArcaWsMigrationProvider.MySql);
        mariaDb.GetRequiredService<INetArcaWsMigrationContextFactory>().Provider.Should().Be(NetArcaWsMigrationProvider.MariaDb);
        mySqlMigrator.GenerateScript(NetArcaWsPersistenceModule.Invoicing).Should().Contain("CREATE TABLE");
        mariaDbMigrator.GenerateScript(NetArcaWsPersistenceModule.WsaaTickets).Should().Contain("CREATE TABLE");
        mySqlMigrator.GenerateScript(NetArcaWsPersistenceModule.Invoicing).Should().Contain("__NetArcaWsInvoiceMigrations").And.NotContain("NetArcaWsaaTickets");
        mariaDbMigrator.GenerateScript(NetArcaWsPersistenceModule.WsaaTickets).Should().Contain("__NetArcaWsTicketMigrations").And.NotContain("NetArcaInvoices");
        mySqlMigrator.GenerateScript(NetArcaWsPersistenceModule.Invoicing, idempotent: true)
            .Should().Contain("IF NOT EXISTS").And.Contain("20261006000100_InitialInvoicing");
        mariaDbMigrator.GenerateScript(NetArcaWsPersistenceModule.WsaaTickets, idempotent: true)
            .Should().Contain("IF NOT EXISTS").And.Contain("20261006000200_InitialWsaaTickets");

        NetArcaWsModelOptions certificates = NetArcaWsModelOptions.Configure(options => options.AddCertificates());
        using ServiceProvider certificateMySql = new ServiceCollection()
            .AddNetArcaWsMySqlMigrations(connectionString, new MySqlServerVersion(new Version(8, 4, 11)), certificates)
            .BuildServiceProvider();
        using ServiceProvider certificateMariaDb = new ServiceCollection()
            .AddNetArcaWsMariaDbMigrations(connectionString, new MariaDbServerVersion(new Version(11, 4, 13)), certificates)
            .BuildServiceProvider();
        INetArcaWsMigrator certificateMySqlMigrator = certificateMySql.GetRequiredService<INetArcaWsMigrator>();
        INetArcaWsMigrator certificateMariaDbMigrator = certificateMariaDb.GetRequiredService<INetArcaWsMigrator>();
        certificateMySqlMigrator.GenerateScript(NetArcaWsPersistenceModule.TenantCertificates)
            .Should().Contain("NetArcaCertificateSlots").And.Contain("NetArcaCertificateVersions")
            .And.Contain("__NetArcaWsCertificateMigrations").And.NotContain("NetArcaInvoices");
        certificateMariaDbMigrator.GenerateScript(NetArcaWsPersistenceModule.TenantCertificates)
            .Should().Contain("NetArcaCertificateSlots").And.Contain("NetArcaCertificateVersions")
            .And.Contain("__NetArcaWsCertificateMigrations").And.NotContain("NetArcaWsaaTickets");
        certificateMySqlMigrator.GenerateScript(NetArcaWsPersistenceModule.TenantCertificates, idempotent: true)
            .Should().Contain("20261006000300_InitialTenantCertificates");
    }

    [Fact]
    public void ModuleSelectionIsFixedAndDoesNotMixMigrationHistories()
    {
        using DbContext mySqlInvoices = MySqlContextFactory().CreateContext(NetArcaWsPersistenceModule.Invoicing);
        using DbContext mySqlTickets = MySqlContextFactory().CreateContext(NetArcaWsPersistenceModule.WsaaTickets);
        using DbContext mariaDbInvoices = MariaDbContextFactory().CreateContext(NetArcaWsPersistenceModule.Invoicing);
        using DbContext mariaDbTickets = MariaDbContextFactory().CreateContext(NetArcaWsPersistenceModule.WsaaTickets);

        AssertMigrationContext(mySqlInvoices, "__NetArcaWsInvoiceMigrations");
        AssertMigrationContext(mySqlTickets, "__NetArcaWsTicketMigrations");
        AssertMigrationContext(mariaDbInvoices, "__NetArcaWsInvoiceMigrations");
        AssertMigrationContext(mariaDbTickets, "__NetArcaWsTicketMigrations");
        mySqlInvoices.Model.GetEntityTypes().Should().NotContain(entity => entity.GetTableName() == "NetArcaWsaaTickets");
        mySqlTickets.Model.GetEntityTypes().Should().NotContain(entity => entity.GetTableName() == "NetArcaInvoices");
    }

    [Fact]
    public void InvoiceMigrationMetadataRetainsNullableRemoteUniquenessAndStoreTypes()
    {
        foreach (DbContext context in new DbContext[]
        {
            MySqlContextFactory().CreateContext(NetArcaWsPersistenceModule.Invoicing),
            MariaDbContextFactory().CreateContext(NetArcaWsPersistenceModule.Invoicing)
        })
        {
            using (context)
            {
                var invoice = context.Model.GetEntityTypes().Single(entity => entity.GetTableName() == "NetArcaInvoices");
                var properties = invoice.GetProperties().ToDictionary(property => property.Name, StringComparer.Ordinal);
                properties["Cuit"].GetRelationalTypeMapping().StoreType.Should().Be("bigint");
                properties["Payload"].GetRelationalTypeMapping().StoreType.Should().Be("longtext");
                properties["RemoteHash"].IsNullable.Should().BeTrue();
                properties["RemoteHash"].GetMaxLength().Should().Be(64);
                invoice.GetIndexes().Single(index => index.Properties.Any(property => property.Name == "RemoteHash")).IsUnique.Should().BeTrue();
                invoice.GetIndexes().Single(index => index.Properties.Any(property => property.Name == "FiscalHash")).IsUnique.Should().BeTrue();
            }
        }
    }

    [Fact]
    public void MySqlAndMariaDb_initial_migrations_change_only_owned_tables_and_keep_utf8mb4()
    {
        foreach (INetArcaWsMigrationContextFactory factory in new INetArcaWsMigrationContextFactory[]
        {
            MySqlContextFactory(), MariaDbContextFactory()
        })
        foreach (NetArcaWsPersistenceModule module in Enum.GetValues<NetArcaWsPersistenceModule>())
        {
            using DbContext context = factory.CreateContext(module);
            IMigrationsAssembly migrations = context.GetService<IMigrationsAssembly>();
            Migration initial = migrations.CreateMigration(migrations.Migrations.OrderBy(entry => entry.Key, StringComparer.Ordinal).First().Value,
                context.Database.ProviderName!);
            IReadOnlyList<MigrationOperation> operations = initial.UpOperations;
            operations.Should().NotContain(operation => operation is AlterDatabaseOperation);
            CreateTableOperation[] tables = operations.OfType<CreateTableOperation>().ToArray();
            tables.Should().NotBeEmpty();
            tables.All(table => Equals(table.FindAnnotation("MySql:CharSet")?.Value, "utf8mb4")).Should().BeTrue();
            var stringColumns = tables.SelectMany(table => table.Columns).Where(column => column.ClrType == typeof(string)).ToArray();
            stringColumns.Should().NotBeEmpty();
            stringColumns.All(column => Equals(column.FindAnnotation("MySql:CharSet")?.Value, "utf8mb4")).Should().BeTrue();

            string script = context.GetService<IMigrator>().GenerateScript("0", null, MigrationsSqlGenerationOptions.Default);
            script.ToUpperInvariant().Should().NotContain("ALTER DATABASE");
            script.Should().Contain("CHARACTER SET utf8mb4");
        }
    }

    private static MySqlMigrationContextFactory MySqlContextFactory() => new(
        "Server=127.0.0.1;Port=1;Database=must_not_connect;User ID=synthetic;Password=synthetic",
        new MySqlServerVersion(new Version(8, 4, 11)));

    private static MariaDbMigrationContextFactory MariaDbContextFactory() => new(
        "Server=127.0.0.1;Port=1;Database=must_not_connect;User ID=synthetic;Password=synthetic",
        new MariaDbServerVersion(new Version(11, 4, 13)));

    private static NetArcaWsModelOptions BothModules() => NetArcaWsModelOptions.Configure(options =>
        options.AddInvoicing(ArcaService.Wsfev1).AddWsaaTickets(ArcaService.Wsfev1));

    private static void AssertMigrationContext(DbContext context, string historyTable)
    {
        context.GetService<IHistoryRepository>().GetCreateScript().Should().Contain($"`{historyTable}`");
        context.GetService<IMigrationsAssembly>().Migrations.Should().ContainSingle();
        context.GetService<IMigrationsAssembly>().ModelSnapshot.Should().NotBeNull();
    }

    private static void AssertCertificateMigration(DbContext context)
    {
        using (context)
        {
            AssertMigrationContext(context, "__NetArcaWsCertificateMigrations");
            context.Database.HasPendingModelChanges().Should().BeFalse();
            context.GetService<IMigrationsAssembly>().Migrations.Keys.Should().ContainSingle()
                .Which.Should().Be("20261006000300_InitialTenantCertificates");
            context.Model.GetEntityTypes().Select(entity => entity.GetTableName())
                .Should().BeEquivalentTo("NetArcaCertificateSlots", "NetArcaCertificateVersions");
            context.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()).Select(property => property.Name)
                .Should().Contain("Ciphertext").And.Contain("Nonce").And.Contain("Tag").And.Contain("ActiveVersionId");
        }
    }

    private static void AssertInvoiceRecoveryMigration(DbContext context)
    {
        using (context)
        {
            AssertMigrationContext(context, "__NetArcaWsInvoiceRecoveryMigrations");
            context.Database.HasPendingModelChanges().Should().BeFalse();
            IMigrationsAssembly migrations = context.GetService<IMigrationsAssembly>();
            migrations.Migrations.Keys.Should().ContainSingle().Which.Should().Be("20261007000400_InitialInvoiceRecovery");
            Migration initial = migrations.CreateMigration(migrations.Migrations.Single().Value, context.Database.ProviderName!);
            initial.UpOperations.OfType<CreateTableOperation>().Select(table => table.Name)
                .Should().ContainSingle().Which.Should().Be("NetArcaInvoiceRecoveryJobs");
            context.Model.GetEntityTypes().Select(entity => entity.GetTableName())
                .Should().ContainSingle().Which.Should().Be("NetArcaInvoiceRecoveryJobs");
            context.Model.GetEntityTypes().Single().GetIndexes().Should().HaveCount(2);
        }
    }

    private static object[] ModelShape(DbContext context) => context.Model.GetEntityTypes()
        .OrderBy(entity => entity.GetTableName(), StringComparer.Ordinal)
        .Select(entity => new
        {
            Table = entity.GetTableName(),
            Columns = entity.GetProperties().Select(property => new
            {
                Name = property.GetColumnName(StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema())),
                ClrType = property.ClrType,
                StoreType = property.GetRelationalTypeMapping().StoreType,
                property.IsNullable,
                MaxLength = property.GetMaxLength(),
                Precision = property.GetPrecision(),
                Scale = property.GetScale()
            }).OrderBy(column => column.Name, StringComparer.Ordinal).ToArray(),
            Indexes = entity.GetIndexes().Select(index => new
            {
                index.Name,
                index.IsUnique,
                Properties = index.Properties.Select(property => property.Name).ToArray()
            }).OrderBy(index => index.Name, StringComparer.Ordinal).ToArray()
        }).Cast<object>().ToArray();
}
