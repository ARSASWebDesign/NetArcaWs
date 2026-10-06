using AwesomeAssertions;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.Migrations;
using NetArcaWs.EntityFrameworkCore.Migrations.Sqlite;
using NetArcaWs.HealthChecks;
using NetArcaWs.Services;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.Tests;

public sealed class OfficialSqliteMigrationsTests
{
    [Fact]
    public async Task InvoiceOnlyCreatesThreeTables()
    {
        await using var db = new TestDatabase(InvoiceOptions());
        NetArcaWsMigrationStatus status = await db.Migrator.ApplyAsync(TestContext.Current.CancellationToken);
        status.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.Current);
        (await db.Tables()).Should().BeEquivalentTo("NetArcaInvoices", "NetArcaInvoiceRevisions", "NetArcaInvoiceSeriesReservations", "__NetArcaWsInvoiceMigrations", "__EFMigrationsLock");
    }

    [Fact]
    public async Task TicketsOnlyCreatesOneTable()
    {
        await using var db = new TestDatabase(TicketOptions());
        await db.Migrator.ApplyAsync(TestContext.Current.CancellationToken);
        (await db.Tables()).Should().BeEquivalentTo("NetArcaWsaaTickets", "__NetArcaWsTicketMigrations", "__EFMigrationsLock");
    }

    [Fact]
    public async Task RepeatApplyPreservesHistory()
    {
        await using var db = new TestDatabase(InvoiceOptions());
        NetArcaWsMigrationStatus first = await db.Migrator.ApplyAsync(TestContext.Current.CancellationToken);
        NetArcaWsMigrationStatus second = await db.Migrator.ApplyAsync(TestContext.Current.CancellationToken);
        second.Modules.Single().Applied.Should().Equal(first.Modules.Single().Applied);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddModulesInEitherOrderPreservesRows(bool invoicesFirst)
    {
        await using var db = new TestDatabase(invoicesFirst ? InvoiceOptions() : TicketOptions());
        await db.Migrator.ApplyAsync(TestContext.Current.CancellationToken);
        if (invoicesFirst)
        {
            await using var context = db.InvoiceContext();
            await context.Database.ExecuteSqlRawAsync("INSERT INTO NetArcaInvoices (TenantHash, KeyHash, TenantId, IdempotencyKey, Service, ServiceHash, Environment, Cuit, PointOfSale, VoucherType, VoucherNumber, FiscalHash, Payload, PayloadHash, CanonicalVersion, State, Version, Attempt, CreatedUtcTicks) VALUES ('t','k','tenant','idem','WSFEv1','s',1,1,1,1,1,'f','payload','p',1,1,1,1,1)", TestContext.Current.CancellationToken);
        }
        else await db.Execute("INSERT INTO NetArcaWsaaTickets (KeyHash, CertificateHash, Endpoint, Service, State, Fence, Version, UpdatedUtcTicks) VALUES ('k','c','endpoint','wsfev1',1,1,1,1)");
        var other = new TestDatabase(db.ConnectionString, BothOptions(invoicesFirst));
        await using (other)
        {
            await other.Migrator.ApplyAsync(TestContext.Current.CancellationToken);
            await using var context = other.InvoiceContext();
            if (invoicesFirst)
                (await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM NetArcaInvoices").SingleAsync(TestContext.Current.CancellationToken)).Should().Be(1);
            else
                (await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM NetArcaWsaaTickets").SingleAsync(TestContext.Current.CancellationToken)).Should().Be(1);
            (await other.Tables()).Should().Contain("NetArcaWsaaTickets");
        }
    }

    [Fact]
    public async Task UnselectedModuleRemainsUntouched()
    {
        await using var db = new TestDatabase(InvoiceOptions());
        await db.Migrator.ApplyAsync(TestContext.Current.CancellationToken);
        (await db.Tables()).Should().NotContain("NetArcaWsaaTickets").And.NotContain("__NetArcaWsTicketMigrations");
    }

    [Theory]
    [InlineData("CREATE TABLE NetArcaInvoices (x INTEGER)")]
    [InlineData("CREATE TABLE __NetArcaWsInvoiceMigrations (MigrationId TEXT PRIMARY KEY, ProductVersion TEXT NOT NULL); INSERT INTO __NetArcaWsInvoiceMigrations VALUES ('future', '10.0.12')")]
    public async Task UntrackedAndUnknownHistoryFailWithoutMutation(string setup)
    {
        await using var db = new TestDatabase(InvoiceOptions());
        await db.Execute(setup);
        Func<Task> act = () => db.Migrator.ApplyAsync(TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<NetArcaWsMigrationPreflightException>();
        (await db.Tables()).Should().NotContain("NetArcaInvoiceRevisions");
    }

    [Fact]
    public async Task NonPrefixHistoryFails()
    {
        await using var db = new TestDatabase(InvoiceOptions());
        await db.Execute("CREATE TABLE NetArcaInvoices (x INTEGER); CREATE TABLE NetArcaInvoiceRevisions (x INTEGER); CREATE TABLE NetArcaInvoiceSeriesReservations (x INTEGER); CREATE TABLE __NetArcaWsInvoiceMigrations (MigrationId TEXT PRIMARY KEY, ProductVersion TEXT NOT NULL); INSERT INTO __NetArcaWsInvoiceMigrations VALUES ('20261001000200_Second', '10.0.12')");
        var inner = new SqliteMigrationContextFactory(db.ConnectionString);
        var factory = new HistoryGapFactory(db.ConnectionString, inner);
        INetArcaWsMigrator migrator = new NetArcaWsMigrator(factory, [NetArcaWsPersistenceModule.Invoicing]);
        NetArcaWsMigrationStatus status = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        status.Modules.Single().State.Should().Be(NetArcaWsMigrationState.InconsistentHistory);
    }

    [Fact]
    public async Task SecondInvalidModulePreventsFirstWrite()
    {
        await using var db = new TestDatabase(BothOptions());
        await db.Execute("CREATE TABLE NetArcaWsaaTickets (x INTEGER)");
        Func<Task> act = () => db.Migrator.ApplyAsync(TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<NetArcaWsMigrationPreflightException>();
        (await db.Tables()).Should().Contain("NetArcaWsaaTickets").And.NotContain("NetArcaInvoices").And.NotContain("__NetArcaWsInvoiceMigrations");
    }

    [Fact]
    public async Task FailureBetweenModulesPreservesCompletedModule()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        string connectionString = $"Data Source={path};Pooling=False";
        var sqliteFactory = new SqliteMigrationContextFactory(connectionString);
        var factory = new FailingTicketMigrationFactory(connectionString, sqliteFactory);
        using var services = new ServiceCollection().AddSingleton<INetArcaWsMigrationContextFactory>(factory)
            .AddSingleton<INetArcaWsMigrator>(new NetArcaWsMigrator(factory, [NetArcaWsPersistenceModule.Invoicing, NetArcaWsPersistenceModule.WsaaTickets])).BuildServiceProvider();
        try
        {
            Func<Task> act = () => services.GetRequiredService<INetArcaWsMigrator>().ApplyAsync(TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<InvalidOperationException>();
            NetArcaWsMigrationStatus status = await services.GetRequiredService<INetArcaWsMigrator>().GetStatusAsync(TestContext.Current.CancellationToken);
            status.Modules[0].State.Should().Be(NetArcaWsMigrationState.Current);
            status.Modules[1].State.Should().Be(NetArcaWsMigrationState.Empty);
            status.Modules[0].Applied.Should().ContainSingle().Which.Should().Be("20261006000100_InitialInvoicing");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task Failure_inside_official_migration_rolls_back_ddl_and_history()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        string connectionString = $"Data Source={path};Pooling=False";
        var inner = new SqliteMigrationContextFactory(connectionString);
        var factory = new FailDuringOfficialInvoiceMigrationFactory(connectionString, inner);
        var migrator = new NetArcaWsMigrator(factory, [NetArcaWsPersistenceModule.Invoicing]);
        try
        {
            Func<Task> apply = () => migrator.ApplyAsync(TestContext.Current.CancellationToken);
            await apply.Should().ThrowAsync<InvalidOperationException>().WithMessage("Synthetic DDL failure.");

            (await migrator.GetStatusAsync(TestContext.Current.CancellationToken)).Modules.Should().ContainSingle()
                .Which.State.Should().Be(NetArcaWsMigrationState.Empty);
            await using var verify = new SqliteConnection(connectionString);
            await verify.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = verify.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('NetArcaInvoices','NetArcaInvoiceRevisions','NetArcaInvoiceSeriesReservations')";
            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            var remaining = new List<string>();
            while (await reader.ReadAsync(TestContext.Current.CancellationToken)) remaining.Add(reader.GetString(0));
            remaining.Should().BeEmpty();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task Test_only_future_migration_upgrades_the_official_initial_schema()
    {
        await using var db = new TestDatabase(InvoiceOptions());
        await db.Migrator.ApplyAsync(TestContext.Current.CancellationToken);
        var factory = new FutureInvoicingMigrationFactory(db.ConnectionString);
        INetArcaWsMigrator futureMigrator = new NetArcaWsMigrator(factory, [NetArcaWsPersistenceModule.Invoicing]);

        NetArcaWsMigrationStatus before = await futureMigrator.GetStatusAsync(TestContext.Current.CancellationToken);
        NetArcaWsModuleMigrationStatus invoice = before.Modules.Should().ContainSingle().Which;
        invoice.State.Should().Be(NetArcaWsMigrationState.UpgradeAvailable);
        invoice.Applied.Should().Equal("20261006000100_InitialInvoicing");
        invoice.Pending.Should().Equal("20990101000100_TestOnlyNullableColumn");

        await using (var diagnosticContext = (FutureInvoicingMigrationDbContext)factory.CreateContext(NetArcaWsPersistenceModule.Invoicing))
        {
            var assembly = diagnosticContext.GetService<IMigrationsAssembly>();
            var initializer = diagnosticContext.GetService<IModelRuntimeInitializer>();
            var current = initializer.Initialize(diagnosticContext.GetService<IDesignTimeModel>().Model, designTime: true);
            var snapshot = initializer.Initialize(assembly.ModelSnapshot!.Model, designTime: true);
            snapshot.FindEntityType("NetArcaWs.EntityFrameworkCore.InvoiceJournalEntity")!.GetProperties().Select(property => property.Name).Should().Contain("Attempt");
            current.FindEntityType("NetArcaWs.EntityFrameworkCore.InvoiceJournalEntity")!.GetProperties().Select(property => property.Name).Should().Contain("Attempt");
            var differ = diagnosticContext.GetService<IMigrationsModelDiffer>();
            var snapshotDiff = differ.GetDifferences(snapshot.GetRelationalModel(), current.GetRelationalModel());
            snapshotDiff.Should().BeEmpty();
            diagnosticContext.Database.HasPendingModelChanges().Should().BeFalse();
        }

        NetArcaWsMigrationStatus upgraded = await futureMigrator.ApplyAsync(TestContext.Current.CancellationToken);
        upgraded.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.Current);
        upgraded.Modules.Single().Applied.Should().Equal("20261006000100_InitialInvoicing", "20990101000100_TestOnlyNullableColumn");

        string[] columns = await db.InvoiceColumns();
        columns.Should().Contain("TestOnlyFutureColumn");
        await using var connection = new SqliteConnection(db.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(\"NetArcaInvoices\")";
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        bool nullableFutureColumn = false;
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            if (reader.GetString(1) == "TestOnlyFutureColumn") nullableFutureColumn = reader.GetInt32(3) == 0;
        }
        nullableFutureColumn.Should().BeTrue();
    }

    [Fact]
    public async Task Cancellation_after_first_module_keeps_its_history_and_leaves_second_module_empty()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        using var cancellation = new CancellationTokenSource();
        var factory = new CancelBetweenModuleMigrationFactory($"Data Source={path};Pooling=False", cancellation);
        var migrator = new NetArcaWsMigrator(factory,
            [NetArcaWsPersistenceModule.Invoicing, NetArcaWsPersistenceModule.WsaaTickets]);
        try
        {
            Func<Task> apply = () => migrator.ApplyAsync(cancellation.Token);
            await apply.Should().ThrowAsync<OperationCanceledException>();
            cancellation.IsCancellationRequested.Should().BeTrue();

            NetArcaWsMigrationStatus status = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
            status.Modules.Select(module => module.State).Should().Equal(NetArcaWsMigrationState.Current, NetArcaWsMigrationState.Empty);
            status.Modules[0].Applied.Should().ContainSingle().Which.Should().Be("20990101001000_TestOnlyInvoicing");
            status.Modules[1].Applied.Should().BeEmpty();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task SqliteRejectsIdempotentScript()
    {
        await using var db = new TestDatabase(InvoiceOptions());
        Action act = () => db.Migrator.GenerateScript(NetArcaWsPersistenceModule.Invoicing, idempotent: true);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void RegistrationAndScriptDoNotConnect()
    {
        using var services = new ServiceCollection().AddNetArcaWsSqliteMigrations("Data Source=/this/path/must/not/be/created.db", BothOptions()).BuildServiceProvider();
        INetArcaWsMigrator migrator = services.GetRequiredService<INetArcaWsMigrator>();
        migrator.GenerateScript(NetArcaWsPersistenceModule.Invoicing).Should().Contain("CREATE TABLE");
        File.Exists("/this/path/must/not/be/created.db").Should().BeFalse();
    }

    [Fact]
    public void RegistrationRejectsSecondMigrationEngine()
    {
        var services = new ServiceCollection();
        services.AddNetArcaWsSqliteMigrations("Data Source=:memory:", InvoiceOptions());
        Action act = () => services.AddNetArcaWsSqliteMigrations("Data Source=:memory:", TicketOptions());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task MigratorCopiesAndCanonicalizesModuleSelection()
    {
        var selected = new List<NetArcaWsPersistenceModule>
        {
            NetArcaWsPersistenceModule.WsaaTickets,
            NetArcaWsPersistenceModule.Invoicing
        };
        var migrator = new NetArcaWsMigrator(new SqliteMigrationContextFactory("Data Source=:memory:"), selected);
        selected.Clear();

        NetArcaWsMigrationStatus status = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        status.Modules.Select(x => x.Module).Should().Equal(NetArcaWsPersistenceModule.Invoicing, NetArcaWsPersistenceModule.WsaaTickets);
    }

    [Fact]
    public void MigratorRejectsInvalidOrDuplicateModules()
    {
        var factory = new SqliteMigrationContextFactory("Data Source=:memory:");
        Action invalid = () => new NetArcaWsMigrator(factory, [(NetArcaWsPersistenceModule)999]);
        Action duplicate = () => new NetArcaWsMigrator(factory, [NetArcaWsPersistenceModule.Invoicing, NetArcaWsPersistenceModule.Invoicing]);
        invalid.Should().Throw<ArgumentException>();
        duplicate.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ServiceSelectionDoesNotChangeMigrationModel()
    {
        using var one = new SqliteInvoicingMigrationsDbContext(new DbContextOptionsBuilder<SqliteInvoicingMigrationsDbContext>().UseSqlite("Data Source=:memory:").Options);
        using var two = new SqliteInvoicingMigrationsDbContext(new DbContextOptionsBuilder<SqliteInvoicingMigrationsDbContext>().UseSqlite("Data Source=:memory:").Options);
        one.Database.HasPendingModelChanges().Should().BeFalse();
        two.Database.HasPendingModelChanges().Should().BeFalse();
        using var runtimeA = new ArcaWsDbContext(new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite("Data Source=:memory:").Options, NetArcaWsModelOptions.Configure(x => x.AddInvoicing(ArcaService.Wsfev1)));
        using var runtimeB = new ArcaWsDbContext(new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite("Data Source=:memory:").Options, NetArcaWsModelOptions.Configure(x => x.AddInvoicing(ArcaService.Wsfexv1)));
        runtimeA.Model.GetEntityTypes().Select(x => x.GetTableName()).Order(StringComparer.Ordinal).Should().Equal(runtimeB.Model.GetEntityTypes().Select(x => x.GetTableName()).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task CancellationBeforeApplyDoesNotMutateDatabase()
    {
        await using var db = new TestDatabase(InvoiceOptions());
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Func<Task> act = () => db.Migrator.ApplyAsync(cancellation.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        File.Exists(db.DatabasePath).Should().BeFalse();
    }

    [Fact]
    public async Task EmptySelectionHasNoModulesOrWrites()
    {
        await using var db = new TestDatabase(NetArcaWsModelOptions.Configure(_ => { }));
        (await db.Migrator.GetStatusAsync(TestContext.Current.CancellationToken)).Modules.Should().BeEmpty();
        (await db.Migrator.ApplyAsync(TestContext.Current.CancellationToken)).Modules.Should().BeEmpty();
        File.Exists(db.DatabasePath).Should().BeFalse();
    }

    [Fact]
    public async Task StatusOnMissingDatabaseIsEmptyWithoutCreatingFile()
    {
        await using var db = new TestDatabase(InvoiceOptions());
        NetArcaWsMigrationStatus status = await db.Migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        status.Modules.Single().State.Should().Be(NetArcaWsMigrationState.Empty);
        File.Exists(db.DatabasePath).Should().BeFalse();
    }

    private static NetArcaWsModelOptions InvoiceOptions() => NetArcaWsModelOptions.Configure(x => x.AddInvoicing(ArcaService.Wsfev1));
    private static NetArcaWsModelOptions TicketOptions() => NetArcaWsModelOptions.Configure(x => x.AddWsaaTickets(ArcaService.Wsfev1));
    private static NetArcaWsModelOptions BothOptions(bool invoicesFirst = true) => NetArcaWsModelOptions.Configure(x =>
    {
        if (invoicesFirst) x.AddInvoicing(ArcaService.Wsfev1).AddWsaaTickets(ArcaService.Wsfev1);
        else x.AddWsaaTickets(ArcaService.Wsfev1).AddInvoicing(ArcaService.Wsfev1);
    });

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly ServiceProvider provider;
        public string ConnectionString { get; }
        public string DatabasePath => ConnectionString[12..].Split(';')[0];
        public INetArcaWsMigrator Migrator => provider.GetRequiredService<INetArcaWsMigrator>();
        public TestDatabase(NetArcaWsModelOptions options) : this($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db")}", options) { }
        public TestDatabase(string connectionString, NetArcaWsModelOptions options)
        {
            ConnectionString = connectionString;
            provider = new ServiceCollection().AddNetArcaWsSqliteMigrations(connectionString, options).BuildServiceProvider();
        }
        public SqliteInvoicingMigrationsDbContext InvoiceContext() => new(new DbContextOptionsBuilder<SqliteInvoicingMigrationsDbContext>().UseSqlite(ConnectionString).Options);
        public async Task Execute(string sql)
        {
            await using var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand(); command.CommandText = sql; await command.ExecuteNonQueryAsync();
        }
        public async Task<string[]> Tables()
        {
            await using var connection = new SqliteConnection(ConnectionString); await connection.OpenAsync();
            await using var command = connection.CreateCommand(); command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
            var names = new List<string>(); await using var reader = await command.ExecuteReaderAsync(); while (await reader.ReadAsync()) names.Add(reader.GetString(0)); return [.. names];
        }
        public async Task<string[]> InvoiceColumns()
        {
            await using var connection = new SqliteConnection(ConnectionString); await connection.OpenAsync();
            await using var command = connection.CreateCommand(); command.CommandText = "PRAGMA table_info(\"NetArcaInvoices\")";
            var names = new List<string>(); await using var reader = await command.ExecuteReaderAsync(); while (await reader.ReadAsync()) names.Add(reader.GetString(1)); return [.. names];
        }
        public async ValueTask DisposeAsync() { await provider.DisposeAsync(); if (ConnectionString.StartsWith("Data Source=", StringComparison.Ordinal)) { var path = ConnectionString[12..].Split(';')[0]; SqliteConnection.ClearAllPools(); if (File.Exists(path)) File.Delete(path); } }
    }

    private sealed class FailingTicketMigrationFactory(string connectionString, SqliteMigrationContextFactory inner) : INetArcaWsMigrationContextFactory
    {
        public NetArcaWsMigrationProvider Provider => NetArcaWsMigrationProvider.Sqlite;
        public DbContext CreateContext(NetArcaWsPersistenceModule module)
        {
            if (module == NetArcaWsPersistenceModule.Invoicing) return inner.CreateContext(module);
            return new SqliteWsaaTicketsMigrationsDbContext(new DbContextOptionsBuilder<SqliteWsaaTicketsMigrationsDbContext>()
                .UseSqlite(connectionString, x => x.MigrationsAssembly(typeof(SqliteMigrationContextFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsTicketMigrations"))
                .AddInterceptors(new FailTicketTableCreationInterceptor()).Options);
        }

        public Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names, CancellationToken cancellationToken) =>
            inner.GetPresentTablesAsync(context, names, cancellationToken);
    }

    private sealed class HistoryGapFactory(string connectionString, SqliteMigrationContextFactory inner) : INetArcaWsMigrationContextFactory
    {
        public NetArcaWsMigrationProvider Provider => NetArcaWsMigrationProvider.Sqlite;
        public DbContext CreateContext(NetArcaWsPersistenceModule module) => module == NetArcaWsPersistenceModule.Invoicing
            ? new HistoryGapMigrationContext(new DbContextOptionsBuilder<HistoryGapMigrationContext>().UseSqlite(connectionString,
                x => x.MigrationsAssembly(typeof(HistoryGapFactory).Assembly.FullName).MigrationsHistoryTable("__NetArcaWsInvoiceMigrations")).Options)
            : throw new ArgumentOutOfRangeException(nameof(module));
        public Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names, CancellationToken cancellationToken) => inner.GetPresentTablesAsync(context, names, cancellationToken);
    }

    private sealed class FailDuringOfficialInvoiceMigrationFactory(string connectionString, SqliteMigrationContextFactory inner) : INetArcaWsMigrationContextFactory
    {
        public NetArcaWsMigrationProvider Provider => NetArcaWsMigrationProvider.Sqlite;
        public DbContext CreateContext(NetArcaWsPersistenceModule module) => module == NetArcaWsPersistenceModule.Invoicing
            ? new SqliteInvoicingMigrationsDbContext(new DbContextOptionsBuilder<SqliteInvoicingMigrationsDbContext>()
                .UseSqlite(connectionString, options => options.MigrationsAssembly(typeof(SqliteMigrationContextFactory).Assembly.FullName)
                    .MigrationsHistoryTable("__NetArcaWsInvoiceMigrations"))
                .AddInterceptors(new FailReservationTableCreationInterceptor()).Options)
            : inner.CreateContext(module);
        public Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names, CancellationToken cancellationToken) =>
            inner.GetPresentTablesAsync(context, names, cancellationToken);
    }

    private sealed class FailReservationTableCreationInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("CREATE TABLE \"NetArcaInvoiceSeriesReservations\"", StringComparison.Ordinal))
                throw new InvalidOperationException("Synthetic DDL failure.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class HistoryGapMigrationContext(DbContextOptions<HistoryGapMigrationContext> options) : DbContext(options) { }

    [Microsoft.EntityFrameworkCore.Infrastructure.DbContextAttribute(typeof(HistoryGapMigrationContext))]
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20261001000100_First")]
    public sealed class HistoryGapFirstMigration : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        protected override void Up(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder migrationBuilder) { }
        protected override void Down(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder migrationBuilder) { }
    }

    [Microsoft.EntityFrameworkCore.Infrastructure.DbContextAttribute(typeof(HistoryGapMigrationContext))]
    [Microsoft.EntityFrameworkCore.Migrations.Migration("20261001000200_Second")]
    public sealed class HistoryGapSecondMigration : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        protected override void Up(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder migrationBuilder) { }
        protected override void Down(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder migrationBuilder) { }
    }

    private sealed class FailTicketTableCreationInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("CREATE TABLE \"NetArcaWsaaTickets\"", StringComparison.Ordinal))
                throw new InvalidOperationException("Synthetic DDL failure.");
            return ValueTask.FromResult(result);
        }
    }
}

internal sealed class CancelBetweenModuleMigrationFactory(string connectionString, CancellationTokenSource cancellation)
    : INetArcaWsMigrationContextFactory
{
    public NetArcaWsMigrationProvider Provider => NetArcaWsMigrationProvider.Sqlite;

    public DbContext CreateContext(NetArcaWsPersistenceModule module) => module switch
    {
        NetArcaWsPersistenceModule.Invoicing => CreateInvoiceContext(),
        NetArcaWsPersistenceModule.WsaaTickets => new CancelBetweenTicketMigrationContext(
            new DbContextOptionsBuilder<CancelBetweenTicketMigrationContext>().UseSqlite(connectionString,
                options => options.MigrationsAssembly(typeof(CancelBetweenModuleMigrationFactory).Assembly.FullName)
                    .MigrationsHistoryTable("__NetArcaWsTicketMigrations")).Options),
        _ => throw new ArgumentOutOfRangeException(nameof(module))
    };

    private DbContext CreateInvoiceContext()
    {
        var interceptor = new CancelOnInvoiceHistoryInsertInterceptor();
        return new CancelBetweenInvoiceMigrationContext(
            new DbContextOptionsBuilder<CancelBetweenInvoiceMigrationContext>().UseSqlite(connectionString,
                options => options.MigrationsAssembly(typeof(CancelBetweenModuleMigrationFactory).Assembly.FullName)
                    .MigrationsHistoryTable("__NetArcaWsInvoiceMigrations"))
                .AddInterceptors(interceptor).Options, cancellation, interceptor);
    }

    public async Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        string[] parameters = new string[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            parameters[i] = $"$table{i}";
            command.Parameters.AddWithValue(parameters[i], names[i]);
        }
        command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name IN ({string.Join(",", parameters)})";
        var present = new List<string>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) present.Add(reader.GetString(0));
        return Array.AsReadOnly(present.Order(StringComparer.Ordinal).ToArray());
    }
}

public sealed class CancelBetweenInvoiceMigrationContext(DbContextOptions<CancelBetweenInvoiceMigrationContext> options,
    CancellationTokenSource cancellation, CancelOnInvoiceHistoryInsertInterceptor interceptor) : DbContext(options)
{
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (interceptor.Observed) cancellation.Cancel();
    }
}

public sealed class CancelBetweenTicketMigrationContext(DbContextOptions<CancelBetweenTicketMigrationContext> options) : DbContext(options) { }

public sealed class CancelOnInvoiceHistoryInsertInterceptor : DbCommandInterceptor
{
    public bool Observed { get; private set; }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("__NetArcaWsInvoiceMigrations", StringComparison.Ordinal)) Observed = true;
        return ValueTask.FromResult(result);
    }
}

[DbContext(typeof(CancelBetweenInvoiceMigrationContext))]
[Migration("20990101001000_TestOnlyInvoicing")]
public sealed class CancelBetweenInvoiceMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE TABLE \"NetArcaInvoices\" (SyntheticId INTEGER NOT NULL)");
        migrationBuilder.Sql("CREATE TABLE \"NetArcaInvoiceRevisions\" (SyntheticId INTEGER NOT NULL)");
        migrationBuilder.Sql("CREATE TABLE \"NetArcaInvoiceSeriesReservations\" (SyntheticId INTEGER NOT NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}

[DbContext(typeof(CancelBetweenTicketMigrationContext))]
[Migration("20990101002000_TestOnlyTickets")]
public sealed class CancelBetweenTicketMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("CREATE TABLE \"NetArcaWsaaTickets\" (SyntheticId INTEGER NOT NULL)");
    protected override void Down(MigrationBuilder migrationBuilder) { }
}

[DbContext(typeof(CancelBetweenInvoiceMigrationContext))]
public sealed class CancelBetweenInvoiceMigrationSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => modelBuilder.HasAnnotation("ProductVersion", "10.0.12");
}

[DbContext(typeof(CancelBetweenTicketMigrationContext))]
public sealed class CancelBetweenTicketMigrationSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => modelBuilder.HasAnnotation("ProductVersion", "10.0.12");
}

internal sealed class FutureInvoicingMigrationFactory(string connectionString) : INetArcaWsMigrationContextFactory
{
    public NetArcaWsMigrationProvider Provider => NetArcaWsMigrationProvider.Sqlite;

    public DbContext CreateContext(NetArcaWsPersistenceModule module)
    {
        if (module != NetArcaWsPersistenceModule.Invoicing) throw new ArgumentException("The fixture selects only invoicing.", nameof(module));
        DbContextOptions<FutureInvoicingMigrationDbContext> options = new DbContextOptionsBuilder<FutureInvoicingMigrationDbContext>()
            .UseSqlite(connectionString, sqlite => sqlite
                .MigrationsAssembly(typeof(FutureInvoicingMigrationDbContext).Assembly.FullName)
                .MigrationsHistoryTable("__NetArcaWsInvoiceMigrations"))
            .Options;
        return new FutureInvoicingMigrationDbContext(options);
    }

    public async Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        string[] parameters = new string[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            parameters[i] = $"$table{i}";
            command.Parameters.AddWithValue(parameters[i], names[i]);
        }
        command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name IN ({string.Join(",", parameters)})";
        var present = new List<string>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) present.Add(reader.GetString(0));
        return Array.AsReadOnly(present.Order(StringComparer.Ordinal).ToArray());
    }
}

public sealed class FutureInvoicingMigrationDbContext(DbContextOptions<FutureInvoicingMigrationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) => TestOnlyFutureInvoicingModel.Build(modelBuilder);
}

[DbContext(typeof(FutureInvoicingMigrationDbContext))]
[Migration("20261006000100_InitialInvoicing")]
public sealed class TestOnlyInitialInvoicingMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) { }
    protected override void Down(MigrationBuilder migrationBuilder) { }
}

[DbContext(typeof(FutureInvoicingMigrationDbContext))]
[Migration("20990101000100_TestOnlyNullableColumn")]
public sealed class TestOnlyNullableInvoiceColumnMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: "TestOnlyFutureColumn", table: "NetArcaInvoices", type: "TEXT", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: "TestOnlyFutureColumn", table: "NetArcaInvoices");

}

internal static class TestOnlyFutureInvoicingModel
{
    public static void Build(ModelBuilder modelBuilder)
    {
        NetArcaWsModelOptions options = NetArcaWsModelOptions.Configure(builder =>
            builder.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca));
        modelBuilder.AddNetArcaWs(options);
        Type invoiceEntityType = typeof(ArcaWsDbContext).Assembly.GetType("NetArcaWs.EntityFrameworkCore.InvoiceJournalEntity", throwOnError: true)!;
        modelBuilder.Entity(invoiceEntityType)
            .Property<string>("TestOnlyFutureColumn").HasColumnType("TEXT").IsRequired(false);
        modelBuilder.HasAnnotation("ProductVersion", "10.0.12");
    }
}
