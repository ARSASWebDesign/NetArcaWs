using System.Data.Common;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.Migrations;
using NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql;
using NetArcaWs.EntityFrameworkCore.Migrations.SqlServer;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using Xunit;

namespace NetArcaWs.Persistence.Server.Tests;

public sealed class OfficialServerMigrationTests
{
    [Fact]
    public async Task Official_migration_DDL_failure_rolls_back_tables_and_history_on_transactional_servers()
    {
        PersistenceServerSettings? settings = PersistenceServerSettings.FromEnvironment();
        Assert.SkipWhen(settings is null, "Opt-in PostgreSQL/SQL Server migration failure test.");
        PersistenceServerSettings selected = settings!;
        Assert.SkipWhen(selected.Kind is not ("postgresql" or "sqlserver"), "This suite targets PostgreSQL/SQL Server.");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServerTestDatabase database = await ServerTestDatabase.CreateAsync(selected,
            builder => builder.AddInvoicing(ArcaService.Wsfev1), provision: false);
        var inner = selected.Kind == "postgresql"
            ? (INetArcaWsMigrationContextFactory)new PostgreSqlMigrationContextFactory(database.ConnectionString)
            : new SqlServerMigrationContextFactory(database.ConnectionString);
        var factory = new FailDuringOfficialServerInvoiceMigrationFactory(database.ConnectionString, selected.Kind, inner);
        INetArcaWsMigrator migrator = new NetArcaWsMigrator(factory, [NetArcaWsPersistenceModule.Invoicing]);

        Func<Task> apply = () => migrator.ApplyAsync(cancellationToken);
        await apply.Should().ThrowAsync<InvalidOperationException>().WithMessage("Synthetic DDL failure.");
        NetArcaWsMigrationStatus after = await migrator.GetStatusAsync(cancellationToken);
        after.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.Empty);
        await using DbContext context = database.Services.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContext();
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using DbCommand command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = selected.Kind == "sqlserver"
            ? "SELECT COUNT(*) FROM sys.tables WHERE name IN ('NetArcaInvoices','NetArcaInvoiceRevisions','NetArcaInvoiceSeriesReservations')"
            : "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = current_schema() AND table_name IN ('NetArcaInvoices','NetArcaInvoiceRevisions','NetArcaInvoiceSeriesReservations')";
        Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)).Should().Be(0);
    }

    [Theory]
    [InlineData("untracked")]
    [InlineData("unknown-history")]
    public async Task Existing_unknown_schema_is_rejected_without_mutation(string setup)
    {
        PersistenceServerSettings? settings = PersistenceServerSettings.FromEnvironment();
        Assert.SkipWhen(settings is null, "Opt-in PostgreSQL/SQL Server migration preflight test.");
        PersistenceServerSettings selected = settings!;
        Assert.SkipWhen(selected.Kind is not ("postgresql" or "sqlserver"), "This suite targets PostgreSQL/SQL Server.");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        NetArcaWsModelOptions invoiceOptions = Options(NetArcaWsPersistenceModule.Invoicing);
        await using ServerTestDatabase database = await ServerTestDatabase.CreateAsync(selected,
            builder => builder.AddInvoicing(ArcaService.Wsfev1), provision: false);
        await CreateInvalidSchemaAsync(database, selected.Kind, setup, cancellationToken);

        INetArcaWsMigrator migrator = database.CreateOfficialMigrator(invoiceOptions);
        NetArcaWsMigrationState expected = setup == "untracked"
            ? NetArcaWsMigrationState.UntrackedSchema
            : NetArcaWsMigrationState.UnknownAppliedMigration;
        NetArcaWsMigrationStatus before = await migrator.GetStatusAsync(cancellationToken);
        before.Modules.Should().ContainSingle().Which.State.Should().Be(expected);
        Func<Task> apply = () => migrator.ApplyAsync(cancellationToken);
        await apply.Should().ThrowAsync<NetArcaWsMigrationPreflightException>();
        NetArcaWsMigrationStatus after = await migrator.GetStatusAsync(cancellationToken);
        after.Modules.Should().ContainSingle().Which.State.Should().Be(expected);
        after.Modules.Single().Applied.Should().Equal(before.Modules.Single().Applied);
        after.Modules.Single().Pending.Should().BeEquivalentTo(after.Modules.Single().Known);
    }

    [Fact]
    public async Task Missing_tracked_table_fails_preflight_and_preserves_history()
    {
        PersistenceServerSettings? settings = PersistenceServerSettings.FromEnvironment();
        Assert.SkipWhen(settings is null, "Opt-in PostgreSQL/SQL Server migration preflight test.");
        PersistenceServerSettings selected = settings!;
        Assert.SkipWhen(selected.Kind is not ("postgresql" or "sqlserver"), "This suite targets PostgreSQL/SQL Server.");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        NetArcaWsModelOptions invoiceOptions = Options(NetArcaWsPersistenceModule.Invoicing);
        await using ServerTestDatabase database = await ServerTestDatabase.CreateAsync(selected,
            builder => builder.AddInvoicing(ArcaService.Wsfev1));
        await ExecuteSchemaCommandAsync(database, selected.Kind,
            selected.Kind == "sqlserver" ? "DROP TABLE [dbo].[NetArcaInvoiceRevisions]" : "DROP TABLE \"NetArcaInvoiceRevisions\"",
            cancellationToken);

        INetArcaWsMigrator migrator = database.CreateOfficialMigrator(invoiceOptions);
        NetArcaWsMigrationStatus before = await migrator.GetStatusAsync(cancellationToken);
        before.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.TrackedSchemaIncomplete);
        Func<Task> apply = () => migrator.ApplyAsync(cancellationToken);
        await apply.Should().ThrowAsync<NetArcaWsMigrationPreflightException>();
        NetArcaWsMigrationStatus after = await migrator.GetStatusAsync(cancellationToken);
        after.Modules.Single().Applied.Should().Equal(before.Modules.Single().Applied);
        after.Modules.Single().State.Should().Be(NetArcaWsMigrationState.TrackedSchemaIncomplete);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Adding_modules_in_either_order_preserves_data_and_disabled_modules(bool invoicingFirst)
    {
        PersistenceServerSettings? settings = PersistenceServerSettings.FromEnvironment();
        Assert.SkipWhen(settings is null, "Opt-in PostgreSQL/SQL Server migration lifecycle test.");
        PersistenceServerSettings selected = settings!;
        Assert.SkipWhen(selected.Kind is not ("postgresql" or "sqlserver"), "This lifecycle suite targets PostgreSQL/SQL Server.");

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        NetArcaWsModelOptions firstOptions = Options(invoicingFirst ? NetArcaWsPersistenceModule.Invoicing : NetArcaWsPersistenceModule.WsaaTickets);
        NetArcaWsModelOptions secondOptions = Options(invoicingFirst ? NetArcaWsPersistenceModule.WsaaTickets : NetArcaWsPersistenceModule.Invoicing);
        NetArcaWsModelOptions allOptions = Options(NetArcaWsPersistenceModule.Invoicing, NetArcaWsPersistenceModule.WsaaTickets);
        await using ServerTestDatabase database = await ServerTestDatabase.CreateAsync(selected,
            builder => Add(builder, invoicingFirst ? NetArcaWsPersistenceModule.Invoicing : NetArcaWsPersistenceModule.WsaaTickets));

        var disabled = NetArcaWsModelOptions.Configure(_ => { });
        (await database.ApplyOfficialMigrationsAsync(disabled, cancellationToken)).Modules.Should().BeEmpty();
        await PersistFixtureAsync(database, selected.Kind, invoicingFirst, cancellationToken);

        NetArcaWsMigrationStatus added = await database.ApplyOfficialMigrationsAsync(secondOptions, cancellationToken);
        added.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.Current);
        NetArcaWsMigrationStatus allCurrent = await database.ApplyOfficialMigrationsAsync(allOptions, cancellationToken);
        allCurrent.Modules.Should().HaveCount(2).And.OnlyContain(module => module.State == NetArcaWsMigrationState.Current);
        NetArcaWsMigrationStatus repeated = await database.ApplyOfficialMigrationsAsync(allOptions, cancellationToken);
        repeated.Modules.Select(module => module.Applied).Should().BeEquivalentTo(allCurrent.Modules.Select(module => module.Applied));

        if (invoicingFirst)
        {
            IInvoiceJournal journal = database.Services.GetRequiredService<IInvoiceJournal>();
            (await journal.FindAsync("migration-lifecycle", "invoice-fixture", cancellationToken))!.Submission.Payload.Should().Be("<synthetic>revision</synthetic>");
            (await journal.ListRevisionsAsync("migration-lifecycle", "invoice-fixture", cancellationToken))
                .Should().ContainSingle().Which.Submission.Payload.Should().Be("<synthetic>preserved</synthetic>");
        }
        else
        {
            byte[] savedCiphertext = await ReadTicketCiphertextAsync(database, selected.Kind, cancellationToken);
            savedCiphertext.Should().Equal([11, 22, 33, 44, 55]);
        }
    }

    private static async Task PersistFixtureAsync(ServerTestDatabase database, string kind, bool invoicingFirst, CancellationToken cancellationToken)
    {
        if (invoicingFirst)
        {
            IInvoiceJournal journal = database.Services.GetRequiredService<IInvoiceJournal>();
            var submission = new InvoiceSubmission("migration-lifecycle", "invoice-fixture", "wsfe",
                new InvoiceIdentity(ArcaEnvironment.Homologation, 20999888777, 71, 1, 1),
                "<synthetic>preserved</synthetic>");
            InvoiceOperation prepared = await journal.PrepareAsync(submission, cancellationToken);
            InvoiceLease lease = (await journal.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey,
                TimeSpan.FromSeconds(30), false, cancellationToken))!;
            InvoiceOperation rejected = await journal.CompleteAsync(lease, new InvoiceDecision(InvoiceState.Rejected), cancellationToken);
            await journal.ReviseRejectedAsync(submission with { Payload = "<synthetic>revision</synthetic>" }, rejected.Version, cancellationToken);
            prepared.State.Should().Be(InvoiceState.Prepared);
        }
        else
        {
            await using DbContext context = database.Services.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContext();
            await context.Database.OpenConnectionAsync(cancellationToken);
            await using DbCommand command = context.Database.GetDbConnection().CreateCommand();
            string table = kind == "sqlserver" ? "[dbo].[NetArcaWsaaTickets]" : "\"NetArcaWsaaTickets\"";
            string columns = kind == "sqlserver"
                ? "[KeyHash], [CertificateHash], [Endpoint], [Service], [State], [Fence], [Version], [Nonce], [Ciphertext], [Tag], [UpdatedUtcTicks]"
                : "\"KeyHash\", \"CertificateHash\", \"Endpoint\", \"Service\", \"State\", \"Fence\", \"Version\", \"Nonce\", \"Ciphertext\", \"Tag\", \"UpdatedUtcTicks\"";
            command.CommandText = $"INSERT INTO {table} ({columns}) VALUES (@key, @cert, @endpoint, @service, 3, 4, 5, @nonce, @ciphertext, @tag, 123456789)";
            AddParameter(command, "@key", new string('a', 64));
            AddParameter(command, "@cert", new string('b', 64));
            AddParameter(command, "@endpoint", "https://synthetic.invalid/wsaa");
            AddParameter(command, "@service", "wsfev1");
            AddParameter(command, "@nonce", new byte[] { 1, 2, 3 });
            AddParameter(command, "@ciphertext", new byte[] { 11, 22, 33, 44, 55 });
            AddParameter(command, "@tag", new byte[] { 6, 7, 8 });
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task CreateInvalidSchemaAsync(ServerTestDatabase database, string kind, string setup, CancellationToken cancellationToken)
    {
        if (setup == "untracked")
        {
            string table = kind == "sqlserver" ? "[dbo].[NetArcaInvoices]" : "\"NetArcaInvoices\"";
            await ExecuteSchemaCommandAsync(database, kind, $"CREATE TABLE {table} (SyntheticTrap int NOT NULL)", cancellationToken);
            return;
        }

        string history = kind == "sqlserver" ? "[dbo].[__NetArcaWsInvoiceMigrations]" : "\"__NetArcaWsInvoiceMigrations\"";
        string columns = kind == "sqlserver" ? "[MigrationId] varchar(150) NOT NULL PRIMARY KEY, [ProductVersion] varchar(32) NOT NULL" : "\"MigrationId\" varchar(150) PRIMARY KEY, \"ProductVersion\" varchar(32) NOT NULL";
        await ExecuteSchemaCommandAsync(database, kind, $"CREATE TABLE {history} ({columns})", cancellationToken);
        await ExecuteSchemaCommandAsync(database, kind, $"INSERT INTO {history} ({(kind == "sqlserver" ? "[MigrationId], [ProductVersion]" : "\"MigrationId\", \"ProductVersion\"")}) VALUES ('29990101000000_Future', '10.0.12')", cancellationToken);
    }

    private static async Task ExecuteSchemaCommandAsync(ServerTestDatabase database, string kind, string sql, CancellationToken cancellationToken)
    {
        await using ArcaWsDbContext context = await database.Services.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync(cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using DbCommand command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<byte[]> ReadTicketCiphertextAsync(ServerTestDatabase database, string kind, CancellationToken cancellationToken)
    {
        await using DbContext context = database.Services.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContext();
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using DbCommand command = context.Database.GetDbConnection().CreateCommand();
        string table = kind == "sqlserver" ? "[dbo].[NetArcaWsaaTickets]" : "\"NetArcaWsaaTickets\"";
        command.CommandText = kind == "sqlserver" ? $"SELECT [Ciphertext] FROM {table} WHERE [KeyHash] = @key" : $"SELECT \"Ciphertext\" FROM {table} WHERE \"KeyHash\" = @key";
        AddParameter(command, "@key", new string('a', 64));
        return (byte[])(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("The synthetic WSAA ticket row was not preserved."));
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static NetArcaWsModelOptions Options(params NetArcaWsPersistenceModule[] modules) => NetArcaWsModelOptions.Configure(builder =>
    {
        foreach (NetArcaWsPersistenceModule module in modules) Add(builder, module);
    });

    private static void Add(NetArcaWsModelOptionsBuilder builder, NetArcaWsPersistenceModule module)
    {
        if (module == NetArcaWsPersistenceModule.Invoicing) builder.AddInvoicing(ArcaService.Wsfev1);
        else builder.AddWsaaTickets(ArcaService.Wsfev1);
    }
}

internal sealed class FailDuringOfficialServerInvoiceMigrationFactory(string connectionString, string kind,
    INetArcaWsMigrationContextFactory inner) : INetArcaWsMigrationContextFactory
{
    public NetArcaWsMigrationProvider Provider => inner.Provider;
    public DbContext CreateContext(NetArcaWsPersistenceModule module)
    {
        if (module != NetArcaWsPersistenceModule.Invoicing) return inner.CreateContext(module);
        var interceptor = new FailReservationTableCreationInterceptor();
        if (kind == "postgresql")
            return new PostgreSqlInvoicingMigrationsDbContext(new DbContextOptionsBuilder<PostgreSqlInvoicingMigrationsDbContext>()
                .UseNpgsql(connectionString, options => options.MigrationsAssembly(typeof(PostgreSqlMigrationContextFactory).Assembly.FullName)
                    .MigrationsHistoryTable("__NetArcaWsInvoiceMigrations", "public"))
                .AddInterceptors(interceptor).Options);
        return new SqlServerInvoicingMigrationsDbContext(new DbContextOptionsBuilder<SqlServerInvoicingMigrationsDbContext>()
            .UseSqlServer(connectionString, options => options.MigrationsAssembly(typeof(SqlServerMigrationContextFactory).Assembly.FullName)
                .MigrationsHistoryTable("__NetArcaWsInvoiceMigrations", "dbo"))
            .AddInterceptors(interceptor).Options);
    }

    public Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names, CancellationToken cancellationToken) =>
        inner.GetPresentTablesAsync(context, names, cancellationToken);

    private sealed class FailReservationTableCreationInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("NetArcaInvoiceSeriesReservations", StringComparison.Ordinal))
                throw new InvalidOperationException("Synthetic DDL failure.");
            return ValueTask.FromResult(result);
        }
    }
}
