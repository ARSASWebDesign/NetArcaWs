using System.Data.Common;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.Migrations;
using NetArcaWs.EntityFrameworkCore.Migrations.MariaDb;
using NetArcaWs.EntityFrameworkCore.Migrations.MySql;
using NetArcaWs.HealthChecks;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.MySql.Tests;

public sealed class OfficialMySqlMigrationTests
{
    [Theory]
    [InlineData("untracked")]
    [InlineData("unknown-history")]
    public async Task Existing_untracked_or_unknown_schema_is_rejected_without_mutation(string setup)
    {
        bool anyOptInSetting = new[] { "NETARCA_PERSISTENCE_DB", "NETARCA_PERSISTENCE_DB_KIND", "NETARCA_PERSISTENCE_DB_VERSION" }
            .Any(name => Environment.GetEnvironmentVariable(name) is not null);
        Assert.SkipWhen(!anyOptInSetting, "Opt-in MySQL/MariaDB migration preflight test.");
        PersistenceDbSettings settings = PersistenceDbSettings.ReadRequired();
        Assert.SkipWhen(settings.Kind is not ("mysql" or "mariadb"), "This suite targets MySQL/MariaDB.");
        NetArcaWsModelOptions invoiceOptions = NetArcaWsModelOptions.Configure(builder => builder.AddInvoicing(ArcaService.Wsfev1));
        await using MySqlTestDatabase database = await MySqlTestDatabase.CreateAsync(settings,
            builder => builder.AddInvoicing(ArcaService.Wsfev1), provision: false);
        await CreateInvalidSchemaAsync(database, setup);

        INetArcaWsMigrator migrator = database.CreateOfficialMigrator(invoiceOptions);
        NetArcaWsMigrationState expected = setup == "untracked"
            ? NetArcaWsMigrationState.UntrackedSchema
            : NetArcaWsMigrationState.UnknownAppliedMigration;
        NetArcaWsMigrationStatus before = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        before.Modules.Should().ContainSingle().Which.State.Should().Be(expected);
        Func<Task> apply = () => migrator.ApplyAsync(TestContext.Current.CancellationToken);
        await apply.Should().ThrowAsync<NetArcaWsMigrationPreflightException>();
        NetArcaWsMigrationStatus after = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        after.Modules.Single().State.Should().Be(expected);
        after.Modules.Single().Applied.Should().Equal(before.Modules.Single().Applied);
        if (setup == "untracked") after.Modules.Single().PresentTables.Should().Contain("NetArcaInvoices").And.NotContain("NetArcaInvoiceRevisions");
    }

    [Fact]
    public async Task Missing_tracked_table_is_rejected_without_rewriting_history()
    {
        bool anyOptInSetting = new[] { "NETARCA_PERSISTENCE_DB", "NETARCA_PERSISTENCE_DB_KIND", "NETARCA_PERSISTENCE_DB_VERSION" }
            .Any(name => Environment.GetEnvironmentVariable(name) is not null);
        Assert.SkipWhen(!anyOptInSetting, "Opt-in MySQL/MariaDB migration preflight test.");
        PersistenceDbSettings settings = PersistenceDbSettings.ReadRequired();
        Assert.SkipWhen(settings.Kind is not ("mysql" or "mariadb"), "This suite targets MySQL/MariaDB.");
        NetArcaWsModelOptions invoiceOptions = NetArcaWsModelOptions.Configure(builder => builder.AddInvoicing(ArcaService.Wsfev1));
        await using MySqlTestDatabase database = await MySqlTestDatabase.CreateAsync(settings,
            builder => builder.AddInvoicing(ArcaService.Wsfev1));
        await ExecuteAsync(database, "DROP TABLE `NetArcaInvoiceRevisions`");
        INetArcaWsMigrator migrator = database.CreateOfficialMigrator(invoiceOptions);
        NetArcaWsMigrationStatus before = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        before.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.TrackedSchemaIncomplete);
        Func<Task> apply = () => migrator.ApplyAsync(TestContext.Current.CancellationToken);
        await apply.Should().ThrowAsync<NetArcaWsMigrationPreflightException>();
        NetArcaWsMigrationStatus after = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        after.Modules.Single().State.Should().Be(NetArcaWsMigrationState.TrackedSchemaIncomplete);
        after.Modules.Single().Applied.Should().Equal(before.Modules.Single().Applied);
    }

    [Fact]
    public async Task Partial_DDL_failure_does_not_record_success_or_blindly_retry()
    {
        bool anyOptInSetting = new[] { "NETARCA_PERSISTENCE_DB", "NETARCA_PERSISTENCE_DB_KIND", "NETARCA_PERSISTENCE_DB_VERSION" }
            .Any(name => Environment.GetEnvironmentVariable(name) is not null);
        Assert.SkipWhen(!anyOptInSetting, "Opt-in MySQL/MariaDB partial-DDL migration test.");
        PersistenceDbSettings settings = PersistenceDbSettings.ReadRequired();
        Assert.SkipWhen(settings.Kind is not ("mysql" or "mariadb"), "This suite targets MySQL/MariaDB.");

        await using MySqlTestDatabase database = await MySqlTestDatabase.CreateAsync(settings,
            builder => builder.AddInvoicing(NetArcaWs.HealthChecks.ArcaService.Wsfev1), provision: false);
        var interceptor = new InjectTableBeforeInvoiceCreateInterceptor(database.DatabaseConnectionString);
        INetArcaWsMigrator migrator = CreateInstrumentedMigrator(settings, database.DatabaseConnectionString, interceptor);
        NetArcaWsMigrationStatus empty = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        empty.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.Empty);

        Func<Task> apply = () => migrator.ApplyAsync(TestContext.Current.CancellationToken);
        await apply.Should().ThrowAsync<MySqlException>();
        interceptor.Injected.Should().BeTrue("the conflict is inserted after preflight, immediately before the first real migration DDL");

        NetArcaWsMigrationStatus failed = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        failed.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.UntrackedSchema);
        failed.Modules.Single().Applied.Should().BeEmpty();
        failed.Modules.Single().PresentTables.Should().Contain("NetArcaInvoiceRevisions").And.Contain("NetArcaInvoices");

        Func<Task> blindRetry = () => migrator.ApplyAsync(TestContext.Current.CancellationToken);
        await blindRetry.Should().ThrowAsync<NetArcaWsMigrationPreflightException>();
        NetArcaWsMigrationStatus afterRetry = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        afterRetry.Modules.Single().Applied.Should().BeEmpty();
        afterRetry.Modules.Single().PresentTables.Should().Contain("NetArcaInvoiceRevisions").And.Contain("NetArcaInvoices");
    }

    private static INetArcaWsMigrator CreateInstrumentedMigrator(PersistenceDbSettings settings, string connectionString,
        DbCommandInterceptor interceptor)
    {
        INetArcaWsMigrationContextFactory inner = settings.Kind == "mysql"
            ? new MySqlMigrationContextFactory(connectionString, new MySqlServerVersion(settings.Version))
            : new MariaDbMigrationContextFactory(connectionString, new MariaDbServerVersion(settings.Version));
        return new NetArcaWsMigrator(new InstrumentedFactory(settings, connectionString, inner, interceptor),
            [NetArcaWsPersistenceModule.Invoicing]);
    }

    private static async Task CreateInvalidSchemaAsync(MySqlTestDatabase database, string setup)
    {
        if (setup == "untracked")
        {
            await ExecuteAsync(database, "CREATE TABLE `NetArcaInvoices` (`SyntheticTrap` int NOT NULL)");
            return;
        }
        await ExecuteAsync(database, "CREATE TABLE `__NetArcaWsInvoiceMigrations` (`MigrationId` varchar(150) NOT NULL PRIMARY KEY, `ProductVersion` varchar(32) NOT NULL)");
        await ExecuteAsync(database, "INSERT INTO `__NetArcaWsInvoiceMigrations` (`MigrationId`, `ProductVersion`) VALUES ('29990101000000_Future', '10.0.12')");
    }

    private static async Task ExecuteAsync(MySqlTestDatabase database, string sql)
    {
        await using ArcaWsDbContext context = await database.Services.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync();
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using DbCommand command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    private sealed class InstrumentedFactory(PersistenceDbSettings settings, string connectionString,
        INetArcaWsMigrationContextFactory inner, DbCommandInterceptor interceptor) : INetArcaWsMigrationContextFactory
    {
        public NetArcaWsMigrationProvider Provider => inner.Provider;

        public DbContext CreateContext(NetArcaWsPersistenceModule module)
        {
            if (module != NetArcaWsPersistenceModule.Invoicing) return inner.CreateContext(module);
            if (settings.Kind == "mysql")
                return new MySqlInvoicingMigrationsDbContext(new DbContextOptionsBuilder<MySqlInvoicingMigrationsDbContext>()
                    .UseMySql(connectionString, new MySqlServerVersion(settings.Version), options => options
                        .MigrationsAssembly(typeof(MySqlMigrationContextFactory).Assembly.FullName)
                        .MigrationsHistoryTable("__NetArcaWsInvoiceMigrations"))
                    .AddInterceptors(interceptor).Options);
            return new MariaDbInvoicingMigrationsDbContext(new DbContextOptionsBuilder<MariaDbInvoicingMigrationsDbContext>()
                .UseMySql(connectionString, new MariaDbServerVersion(settings.Version), options => options
                    .MigrationsAssembly(typeof(MariaDbMigrationContextFactory).Assembly.FullName)
                    .MigrationsHistoryTable("__NetArcaWsInvoiceMigrations"))
                .AddInterceptors(interceptor).Options);
        }

        public Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names,
            CancellationToken cancellationToken) => inner.GetPresentTablesAsync(context, names, cancellationToken);
    }

    private sealed class InjectTableBeforeInvoiceCreateInterceptor(string adminConnectionString) : DbCommandInterceptor
    {
        private int injected;
        public bool Injected => Volatile.Read(ref injected) != 0;

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!command.CommandText.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase) ||
                !command.CommandText.Contains("NetArcaInvoiceRevisions", StringComparison.Ordinal) ||
                Interlocked.Exchange(ref injected, 1) != 0)
                return result;

            await using var admin = new MySqlConnector.MySqlConnection(adminConnectionString);
            await admin.OpenAsync(cancellationToken);
            await using var trap = admin.CreateCommand();
            trap.CommandText = "CREATE TABLE `NetArcaInvoices` (`SyntheticTrap` int NOT NULL)";
            await trap.ExecuteNonQueryAsync(cancellationToken);
            return result;
        }
    }
}
