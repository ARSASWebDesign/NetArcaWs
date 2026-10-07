using System.Data.Common;
using System.Security.Cryptography;
using System.Threading;
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
using NetArcaWs.Invoicing;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.MySql.Tests;

public sealed class OfficialMySqlMigrationTests
{
    [Theory]
    [InlineData("untracked")]
    [InlineData("unknown-history")]
    [InlineData("certificate-untracked")]
    [InlineData("certificate-unknown-history")]
    [InlineData("recovery-untracked")]
    [InlineData("recovery-unknown-history")]
    public async Task Existing_untracked_or_unknown_schema_is_rejected_without_mutation(string setup)
    {
        bool anyOptInSetting = new[] { "NETARCA_PERSISTENCE_DB", "NETARCA_PERSISTENCE_DB_KIND", "NETARCA_PERSISTENCE_DB_VERSION" }
            .Any(name => Environment.GetEnvironmentVariable(name) is not null);
        Assert.SkipWhen(!anyOptInSetting, "Opt-in MySQL/MariaDB migration preflight test.");
        PersistenceDbSettings settings = PersistenceDbSettings.ReadRequired();
        Assert.SkipWhen(settings.Kind is not ("mysql" or "mariadb"), "This suite targets MySQL/MariaDB.");
        bool certificates = setup.StartsWith("certificate-", StringComparison.Ordinal);
        bool recovery = setup.StartsWith("recovery-", StringComparison.Ordinal);
        string condition = certificates || recovery ? setup[(setup.IndexOf('-') + 1)..] : setup;
        NetArcaWsPersistenceModule module = certificates ? NetArcaWsPersistenceModule.TenantCertificates
            : recovery ? NetArcaWsPersistenceModule.InvoiceRecovery : NetArcaWsPersistenceModule.Invoicing;
        NetArcaWsModelOptions selectedOptions = MigrationOptions(module);
        await using MySqlTestDatabase database = await MySqlTestDatabase.CreateAsync(settings,
            builder =>
            {
                if (module == NetArcaWsPersistenceModule.InvoiceRecovery)
                    builder.AddInvoicing(ArcaService.Wsfev1).AddInvoiceRecovery(ArcaService.Wsfev1);
                else AddModule(builder, module);
            }, provision: false);
        await CreateInvalidSchemaAsync(database, condition, module);

        INetArcaWsMigrator migrator = database.CreateOfficialMigrator(selectedOptions);
        NetArcaWsMigrationState expected = condition == "untracked"
            ? NetArcaWsMigrationState.UntrackedSchema
            : NetArcaWsMigrationState.UnknownAppliedMigration;
        NetArcaWsMigrationStatus before = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        before.Modules.Should().ContainSingle().Which.State.Should().Be(expected);
        string[] tablesBefore = before.Modules.Single().PresentTables.ToArray();
        Func<Task> apply = () => migrator.ApplyAsync(TestContext.Current.CancellationToken);
        await apply.Should().ThrowAsync<NetArcaWsMigrationPreflightException>();
        NetArcaWsMigrationStatus after = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        after.Modules.Single().State.Should().Be(expected);
        after.Modules.Single().Applied.Should().Equal(before.Modules.Single().Applied);
        after.Modules.Single().PresentTables.Should().Equal(tablesBefore);
        if (condition == "untracked")
            after.Modules.Single().PresentTables.Should().Contain(certificates ? "NetArcaCertificateSlots"
                : recovery ? "NetArcaInvoiceRecoveryJobs" : "NetArcaInvoices");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_tracked_table_is_rejected_without_rewriting_history(bool certificates)
    {
        bool anyOptInSetting = new[] { "NETARCA_PERSISTENCE_DB", "NETARCA_PERSISTENCE_DB_KIND", "NETARCA_PERSISTENCE_DB_VERSION" }
            .Any(name => Environment.GetEnvironmentVariable(name) is not null);
        Assert.SkipWhen(!anyOptInSetting, "Opt-in MySQL/MariaDB migration preflight test.");
        PersistenceDbSettings settings = PersistenceDbSettings.ReadRequired();
        Assert.SkipWhen(settings.Kind is not ("mysql" or "mariadb"), "This suite targets MySQL/MariaDB.");
        NetArcaWsPersistenceModule module = certificates ? NetArcaWsPersistenceModule.TenantCertificates : NetArcaWsPersistenceModule.Invoicing;
        NetArcaWsModelOptions selectedOptions = MigrationOptions(module);
        await using MySqlTestDatabase database = await MySqlTestDatabase.CreateAsync(settings,
            builder => AddModule(builder, module));
        await ExecuteAsync(database, certificates ? "DROP TABLE `NetArcaCertificateVersions`" : "DROP TABLE `NetArcaInvoiceRevisions`");
        INetArcaWsMigrator migrator = database.CreateOfficialMigrator(selectedOptions);
        NetArcaWsMigrationStatus before = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        before.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.TrackedSchemaIncomplete);
        string[] presentBefore = before.Modules.Single().PresentTables.ToArray();
        Func<Task> apply = () => migrator.ApplyAsync(TestContext.Current.CancellationToken);
        await apply.Should().ThrowAsync<NetArcaWsMigrationPreflightException>();
        NetArcaWsMigrationStatus after = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        after.Modules.Single().State.Should().Be(NetArcaWsMigrationState.TrackedSchemaIncomplete);
        after.Modules.Single().Applied.Should().Equal(before.Modules.Single().Applied);
        after.Modules.Single().PresentTables.Should().Equal(presentBefore);
    }

    [Fact]
    public async Task Missing_recovery_queue_table_is_rejected_without_rewriting_its_history()
    {
        bool anyOptInSetting = new[] { "NETARCA_PERSISTENCE_DB", "NETARCA_PERSISTENCE_DB_KIND", "NETARCA_PERSISTENCE_DB_VERSION" }
            .Any(name => Environment.GetEnvironmentVariable(name) is not null);
        Assert.SkipWhen(!anyOptInSetting, "Opt-in MySQL/MariaDB recovery migration preflight test.");
        PersistenceDbSettings settings = PersistenceDbSettings.ReadRequired();
        Assert.SkipWhen(settings.Kind is not ("mysql" or "mariadb"), "This suite targets MySQL/MariaDB.");
        NetArcaWsModelOptions selectedOptions = MigrationOptions(NetArcaWsPersistenceModule.InvoiceRecovery);
        await using MySqlTestDatabase database = await MySqlTestDatabase.CreateAsync(settings,
            builder => builder.AddInvoicing(ArcaService.Wsfev1).AddInvoiceRecovery(ArcaService.Wsfev1));
        await ExecuteAsync(database, "DROP TABLE `NetArcaInvoiceRecoveryJobs`");
        INetArcaWsMigrator migrator = database.CreateOfficialMigrator(selectedOptions);
        NetArcaWsMigrationStatus before = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        before.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.TrackedSchemaIncomplete);
        string[] presentBefore = before.Modules.Single().PresentTables.ToArray();
        Func<Task> apply = () => migrator.ApplyAsync(TestContext.Current.CancellationToken);
        await apply.Should().ThrowAsync<NetArcaWsMigrationPreflightException>();
        NetArcaWsMigrationStatus after = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        after.Modules.Single().State.Should().Be(NetArcaWsMigrationState.TrackedSchemaIncomplete);
        after.Modules.Single().Applied.Should().Equal(before.Modules.Single().Applied);
        after.Modules.Single().PresentTables.Should().Equal(presentBefore);
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

    [Fact]
    public async Task Partial_recovery_DDL_failure_does_not_record_success_or_blindly_retry_and_preserves_existing_data()
    {
        bool anyOptInSetting = new[] { "NETARCA_PERSISTENCE_DB", "NETARCA_PERSISTENCE_DB_KIND", "NETARCA_PERSISTENCE_DB_VERSION" }
            .Any(name => Environment.GetEnvironmentVariable(name) is not null);
        Assert.SkipWhen(!anyOptInSetting, "Opt-in MySQL/MariaDB recovery migration DDL test.");
        PersistenceDbSettings settings = PersistenceDbSettings.ReadRequired();
        Assert.SkipWhen(settings.Kind is not ("mysql" or "mariadb"), "This suite targets MySQL/MariaDB.");

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        byte[] certificateProtectionKey = RandomNumberGenerator.GetBytes(32);
        await using MySqlTestDatabase database = await MySqlTestDatabase.CreateAsync(settings, builder =>
            builder.AddInvoicing(ArcaService.Wsfev1).AddWsaaTickets(ArcaService.Wsfev1).AddCertificates()
                .AddInvoiceRecovery(ArcaService.Wsfev1), provision: false,
            certificateProtectionKey: certificateProtectionKey);
        NetArcaWsModelOptions oldOptions = NetArcaWsModelOptions.Configure(builder =>
            builder.AddInvoicing(ArcaService.Wsfev1).AddWsaaTickets(ArcaService.Wsfev1).AddCertificates());
        (await database.ApplyOfficialMigrationsAsync(oldOptions, cancellationToken)).Modules.Should().HaveCount(3);
        InvoiceSubmission invoice = new("recovery-ddl", "existing-invoice", "wsfe",
            new InvoiceIdentity(ArcaEnvironment.Homologation, 20999888777, 63, 1, 81), "<synthetic>existing-invoice</synthetic>");
        IInvoiceJournal journal = database.Services.GetRequiredService<IInvoiceJournal>();
        await journal.PrepareAsync(invoice, cancellationToken);
        await ExecuteAsync(database, "INSERT INTO `NetArcaWsaaTickets` (`KeyHash`,`CertificateHash`,`Endpoint`,`Service`,`State`,`Fence`,`Version`,`Nonce`,`Ciphertext`,`Tag`,`UpdatedUtcTicks`) VALUES ('ticket-key','cert-hash','https://synthetic.invalid/wsaa','wsfev1',3,4,5,0x010203,0x0B16212C37,0x060708,123456789)");
        await ExecuteAsync(database, "INSERT INTO `NetArcaCertificateVersions` (`TenantHash`,`Cuit`,`Environment`,`VersionId`,`CreatedAtUtcTicks`,`ThumbprintSha256`,`NotBeforeUtcTicks`,`NotAfterUtcTicks`,`KeyId`,`Nonce`,`Ciphertext`,`Tag`) VALUES ('cert-tenant',20999888777,1,'00000000-0000-0000-0000-000000000001',1,'thumbprint',1,2,'key-1',0x0102,0x0304,0x0506)");
        await ExecuteAsync(database, "INSERT INTO `NetArcaCertificateSlots` (`TenantHash`,`Cuit`,`Environment`,`ActiveVersionId`,`Generation`) VALUES ('cert-tenant',20999888777,1,'00000000-0000-0000-0000-000000000001',1)");
        string ticketBefore = await ReadScalarAsync(database, "SELECT HEX(`Ciphertext`) FROM `NetArcaWsaaTickets` WHERE `KeyHash`='ticket-key'");
        string certificateBefore = await ReadScalarAsync(database, "SELECT HEX(`Ciphertext`) FROM `NetArcaCertificateVersions` WHERE `VersionId`='00000000-0000-0000-0000-000000000001'");

        var interceptor = new InjectRecoveryTableBeforeCreateInterceptor(database.DatabaseConnectionString);
        var migrator = CreateRecoveryInstrumentedMigrator(settings, database.DatabaseConnectionString, interceptor);
        NetArcaWsMigrationStatus empty = await migrator.GetStatusAsync(cancellationToken);
        empty.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.Empty);
        Func<Task> apply = () => migrator.ApplyAsync(cancellationToken);
        await apply.Should().ThrowAsync<MySqlException>();
        interceptor.Injected.Should().BeTrue();
        NetArcaWsMigrationStatus partial = await migrator.GetStatusAsync(cancellationToken);
        partial.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.UntrackedSchema);
        partial.Modules.Single().Applied.Should().BeEmpty();
        partial.Modules.Single().PresentTables.Should().Contain("NetArcaInvoiceRecoveryJobs");
        Func<Task> blindRetry = () => migrator.ApplyAsync(cancellationToken);
        await blindRetry.Should().ThrowAsync<NetArcaWsMigrationPreflightException>();
        NetArcaWsMigrationStatus afterRetry = await migrator.GetStatusAsync(cancellationToken);
        afterRetry.Modules.Single().Applied.Should().BeEmpty();
        afterRetry.Modules.Single().PresentTables.Should().Equal(partial.Modules.Single().PresentTables);
        (await journal.FindAsync(invoice.TenantId, invoice.IdempotencyKey, cancellationToken))!.Submission.Should().BeEquivalentTo(invoice);
        (await ReadScalarAsync(database, "SELECT HEX(`Ciphertext`) FROM `NetArcaWsaaTickets` WHERE `KeyHash`='ticket-key'")).Should().Be(ticketBefore);
        (await ReadScalarAsync(database, "SELECT HEX(`Ciphertext`) FROM `NetArcaCertificateVersions` WHERE `VersionId`='00000000-0000-0000-0000-000000000001'")).Should().Be(certificateBefore);
    }

    [Fact]
    public async Task Recovery_DDL_cancellation_leaves_untracked_schema_without_success_history_or_data_loss()
    {
        bool anyOptInSetting = new[] { "NETARCA_PERSISTENCE_DB", "NETARCA_PERSISTENCE_DB_KIND", "NETARCA_PERSISTENCE_DB_VERSION" }
            .Any(name => Environment.GetEnvironmentVariable(name) is not null);
        Assert.SkipWhen(!anyOptInSetting, "Opt-in MySQL/MariaDB recovery migration cancellation test.");
        PersistenceDbSettings settings = PersistenceDbSettings.ReadRequired();
        Assert.SkipWhen(settings.Kind is not ("mysql" or "mariadb"), "This suite targets MySQL/MariaDB.");

        byte[] certificateProtectionKey = RandomNumberGenerator.GetBytes(32);
        await using MySqlTestDatabase database = await MySqlTestDatabase.CreateAsync(settings, builder =>
            builder.AddInvoicing(ArcaService.Wsfev1).AddWsaaTickets(ArcaService.Wsfev1).AddCertificates()
                .AddInvoiceRecovery(ArcaService.Wsfev1), provision: false,
            certificateProtectionKey: certificateProtectionKey);
        NetArcaWsModelOptions oldOptions = NetArcaWsModelOptions.Configure(builder =>
            builder.AddInvoicing(ArcaService.Wsfev1).AddWsaaTickets(ArcaService.Wsfev1).AddCertificates());
        (await database.ApplyOfficialMigrationsAsync(oldOptions, TestContext.Current.CancellationToken)).Modules.Should().HaveCount(3);
        InvoiceSubmission invoice = new("recovery-cancel", "existing-invoice", "wsfe",
            new InvoiceIdentity(ArcaEnvironment.Homologation, 20999888777, 63, 1, 82), "<synthetic>existing-invoice</synthetic>");
        IInvoiceJournal journal = database.Services.GetRequiredService<IInvoiceJournal>();
        await journal.PrepareAsync(invoice, TestContext.Current.CancellationToken);
        await ExecuteAsync(database, "INSERT INTO `NetArcaWsaaTickets` (`KeyHash`,`CertificateHash`,`Endpoint`,`Service`,`State`,`Fence`,`Version`,`Nonce`,`Ciphertext`,`Tag`,`UpdatedUtcTicks`) VALUES ('ticket-key','cert-hash','https://synthetic.invalid/wsaa','wsfev1',3,4,5,0x010203,0x0B16212C37,0x060708,123456789)");
        await ExecuteAsync(database, "INSERT INTO `NetArcaCertificateVersions` (`TenantHash`,`Cuit`,`Environment`,`VersionId`,`CreatedAtUtcTicks`,`ThumbprintSha256`,`NotBeforeUtcTicks`,`NotAfterUtcTicks`,`KeyId`,`Nonce`,`Ciphertext`,`Tag`) VALUES ('cert-tenant',20999888777,1,'00000000-0000-0000-0000-000000000001',1,'thumbprint',1,2,'key-1',0x0102,0x0304,0x0506)");
        await ExecuteAsync(database, "INSERT INTO `NetArcaCertificateSlots` (`TenantHash`,`Cuit`,`Environment`,`ActiveVersionId`,`Generation`) VALUES ('cert-tenant',20999888777,1,'00000000-0000-0000-0000-000000000001',1)");
        string ticketBefore = await ReadScalarAsync(database, "SELECT HEX(`Ciphertext`) FROM `NetArcaWsaaTickets` WHERE `KeyHash`='ticket-key'");
        string certificateBefore = await ReadScalarAsync(database, "SELECT HEX(`Ciphertext`) FROM `NetArcaCertificateVersions` WHERE `VersionId`='00000000-0000-0000-0000-000000000001'");

        using var cancellation = new CancellationTokenSource();
        var interceptor = new CreateRecoveryTableThenCancelInterceptor(database.DatabaseConnectionString, cancellation);
        var migrator = CreateRecoveryInstrumentedMigrator(settings, database.DatabaseConnectionString, interceptor);
        NetArcaWsMigrationStatus empty = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        empty.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.Empty);
        Func<Task> apply = () => migrator.ApplyAsync(cancellation.Token);
        await apply.Should().ThrowAsync<OperationCanceledException>();
        interceptor.CancelledAfterCreate.Should().BeTrue();

        NetArcaWsMigrationStatus partial = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        partial.Modules.Should().ContainSingle().Which.State.Should().Be(NetArcaWsMigrationState.UntrackedSchema);
        partial.Modules.Single().Applied.Should().BeEmpty();
        partial.Modules.Single().PresentTables.Should().Contain("NetArcaInvoiceRecoveryJobs");
        Func<Task> blindRetry = () => migrator.ApplyAsync(TestContext.Current.CancellationToken);
        await blindRetry.Should().ThrowAsync<NetArcaWsMigrationPreflightException>();
        NetArcaWsMigrationStatus afterRetry = await migrator.GetStatusAsync(TestContext.Current.CancellationToken);
        afterRetry.Modules.Single().Applied.Should().BeEmpty();
        afterRetry.Modules.Single().PresentTables.Should().Equal(partial.Modules.Single().PresentTables);
        (await journal.FindAsync(invoice.TenantId, invoice.IdempotencyKey, TestContext.Current.CancellationToken))!.Submission.Should().BeEquivalentTo(invoice);
        (await ReadScalarAsync(database, "SELECT HEX(`Ciphertext`) FROM `NetArcaWsaaTickets` WHERE `KeyHash`='ticket-key'")).Should().Be(ticketBefore);
        (await ReadScalarAsync(database, "SELECT HEX(`Ciphertext`) FROM `NetArcaCertificateVersions` WHERE `VersionId`='00000000-0000-0000-0000-000000000001'")).Should().Be(certificateBefore);
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

    private static INetArcaWsMigrator CreateRecoveryInstrumentedMigrator(PersistenceDbSettings settings, string connectionString,
        DbCommandInterceptor interceptor)
    {
        INetArcaWsMigrationContextFactory inner = settings.Kind == "mysql"
            ? new MySqlMigrationContextFactory(connectionString, new MySqlServerVersion(settings.Version))
            : new MariaDbMigrationContextFactory(connectionString, new MariaDbServerVersion(settings.Version));
        return new NetArcaWsMigrator(new RecoveryInstrumentedFactory(settings, connectionString, inner, interceptor),
            [NetArcaWsPersistenceModule.InvoiceRecovery]);
    }

    private static async Task<string> ReadScalarAsync(MySqlTestDatabase database, string sql)
    {
        await using ArcaWsDbContext context = await database.Services.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContextAsync();
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using DbCommand command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture)
            ?? throw new InvalidOperationException("Synthetic persistence fixture row was not found.");
    }

    private static async Task CreateInvalidSchemaAsync(MySqlTestDatabase database, string setup, NetArcaWsPersistenceModule module)
    {
        string table = module switch
        {
            NetArcaWsPersistenceModule.TenantCertificates => "NetArcaCertificateSlots",
            NetArcaWsPersistenceModule.InvoiceRecovery => "NetArcaInvoiceRecoveryJobs",
            _ => "NetArcaInvoices"
        };
        string history = module switch
        {
            NetArcaWsPersistenceModule.TenantCertificates => "__NetArcaWsCertificateMigrations",
            NetArcaWsPersistenceModule.InvoiceRecovery => "__NetArcaWsInvoiceRecoveryMigrations",
            _ => "__NetArcaWsInvoiceMigrations"
        };
        if (setup == "untracked")
        {
            await ExecuteAsync(database, $"CREATE TABLE `{table}` (`SyntheticTrap` int NOT NULL)");
            return;
        }
        await ExecuteAsync(database, $"CREATE TABLE `{history}` (`MigrationId` varchar(150) NOT NULL PRIMARY KEY, `ProductVersion` varchar(32) NOT NULL)");
        await ExecuteAsync(database, $"INSERT INTO `{history}` (`MigrationId`, `ProductVersion`) VALUES ('29990101000000_Future', '10.0.12')");
    }

    private static NetArcaWsModelOptions MigrationOptions(NetArcaWsPersistenceModule module) => NetArcaWsModelOptions.Configure(builder => AddModule(builder, module));

    private static void AddModule(NetArcaWsModelOptionsBuilder builder, NetArcaWsPersistenceModule module)
    {
        if (module == NetArcaWsPersistenceModule.Invoicing) builder.AddInvoicing(ArcaService.Wsfev1);
        else if (module == NetArcaWsPersistenceModule.TenantCertificates) builder.AddCertificates();
        else if (module == NetArcaWsPersistenceModule.InvoiceRecovery) builder.AddInvoiceRecovery(ArcaService.Wsfev1);
        else builder.AddWsaaTickets(ArcaService.Wsfev1);
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

    private sealed class RecoveryInstrumentedFactory(PersistenceDbSettings settings, string connectionString,
        INetArcaWsMigrationContextFactory inner, DbCommandInterceptor interceptor) : INetArcaWsMigrationContextFactory
    {
        public NetArcaWsMigrationProvider Provider => inner.Provider;

        public DbContext CreateContext(NetArcaWsPersistenceModule module)
        {
            if (module != NetArcaWsPersistenceModule.InvoiceRecovery) return inner.CreateContext(module);
            if (settings.Kind == "mysql")
                return new MySqlInvoiceRecoveryMigrationsDbContext(new DbContextOptionsBuilder<MySqlInvoiceRecoveryMigrationsDbContext>()
                    .UseMySql(connectionString, new MySqlServerVersion(settings.Version), options => options
                        .MigrationsAssembly(typeof(MySqlMigrationContextFactory).Assembly.FullName)
                        .MigrationsHistoryTable("__NetArcaWsInvoiceRecoveryMigrations"))
                    .AddInterceptors(interceptor).Options);
            return new MariaDbInvoiceRecoveryMigrationsDbContext(new DbContextOptionsBuilder<MariaDbInvoiceRecoveryMigrationsDbContext>()
                .UseMySql(connectionString, new MariaDbServerVersion(settings.Version), options => options
                    .MigrationsAssembly(typeof(MariaDbMigrationContextFactory).Assembly.FullName)
                    .MigrationsHistoryTable("__NetArcaWsInvoiceRecoveryMigrations"))
                .AddInterceptors(interceptor).Options);
        }

        public Task<IReadOnlyList<string>> GetPresentTablesAsync(DbContext context, IReadOnlyList<string> names,
            CancellationToken cancellationToken) => inner.GetPresentTablesAsync(context, names, cancellationToken);
    }

    private sealed class InjectRecoveryTableBeforeCreateInterceptor(string adminConnectionString) : DbCommandInterceptor
    {
        private int injected;
        public bool Injected => Volatile.Read(ref injected) != 0;

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!command.CommandText.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase) ||
                !command.CommandText.Contains("NetArcaInvoiceRecoveryJobs", StringComparison.Ordinal) ||
                Interlocked.Exchange(ref injected, 1) != 0)
                return result;
            await using var admin = new MySqlConnector.MySqlConnection(adminConnectionString);
            await admin.OpenAsync(cancellationToken);
            await using var trap = admin.CreateCommand();
            trap.CommandText = "CREATE TABLE `NetArcaInvoiceRecoveryJobs` (`SyntheticTrap` int NOT NULL)";
            await trap.ExecuteNonQueryAsync(cancellationToken);
            return result;
        }
    }

    private sealed class CreateRecoveryTableThenCancelInterceptor(string adminConnectionString, CancellationTokenSource cancellation) : DbCommandInterceptor
    {
        private int cancelled;
        public bool CancelledAfterCreate => Volatile.Read(ref cancelled) != 0;

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!command.CommandText.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase) ||
                !command.CommandText.Contains("NetArcaInvoiceRecoveryJobs", StringComparison.Ordinal) ||
                Interlocked.Exchange(ref cancelled, 1) != 0)
                return result;
            await using var admin = new MySqlConnection(adminConnectionString);
            await admin.OpenAsync(cancellationToken);
            await using var create = admin.CreateCommand();
            create.CommandText = command.CommandText;
            await create.ExecuteNonQueryAsync(cancellationToken);
            cancellation.Cancel();
            return InterceptionResult<int>.SuppressWithResult(0);
        }
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
