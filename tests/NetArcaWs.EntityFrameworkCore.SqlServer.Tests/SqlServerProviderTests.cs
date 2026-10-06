using AwesomeAssertions;
using System.Data.Common;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.SqlServer;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.SqlServer.Tests;

public sealed class SqlServerProviderTests
{
    [Fact]
    public void Registration_uses_explicit_provider_configuration_without_connecting_or_creating_schema()
    {
        NetArcaWsModelOptions selection = NetArcaWsModelOptions.Configure(x => x.AddInvoicing(ArcaService.Wsfev1));
        var services = new ServiceCollection();

        services.AddNetArcaWsSqlServerStores("Server=127.0.0.1,1;Database=must_not_connect;User Id=x;Password=x;TrustServerCertificate=true", selection);

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().Should().NotBeNull();
        provider.GetRequiredService<IInvoiceJournal>().Should().NotBeNull();
        using ArcaWsDbContext context = provider.GetRequiredService<IDbContextFactory<ArcaWsDbContext>>().CreateDbContext();
        context.Model.GetEntityTypes().Select(x => x.GetTableName()).Should()
            .Contain("NetArcaInvoices").And.Contain("NetArcaInvoiceRevisions").And.Contain("NetArcaInvoiceSeriesReservations");
    }

    [Fact]
    public void Remote_request_id_index_is_filtered_to_allow_multiple_null_values()
    {
        NetArcaWsModelOptions selection = NetArcaWsModelOptions.Configure(x => x.AddInvoicing(ArcaService.Wsfev1));
        var options = new DbContextOptionsBuilder<ArcaWsDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=must_not_connect;User Id=x;Password=x;TrustServerCertificate=true")
            .Options;
        using var context = new ArcaWsDbContext(options, selection);

        IReadOnlyIndex index = context.Model.FindEntityType("NetArcaWs.EntityFrameworkCore.InvoiceJournalEntity")!
            .GetIndexes().Single(x => x.GetDatabaseName() == "UX_NetArcaInvoices_Remote");

        index.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Be("[RemoteHash] IS NOT NULL");
        context.Database.GenerateCreateScript().Should().Contain("WHERE [RemoteHash] IS NOT NULL");
    }

    [Fact]
    public void Empty_persistence_selection_is_rejected_without_connecting()
    {
        NetArcaWsModelOptions empty = NetArcaWsModelOptions.Configure(_ => { });
        var services = new ServiceCollection();

        Action register = () => services.AddNetArcaWsSqlServerStores("Server=127.0.0.1,1;Database=unused", empty);

        register.Should().Throw<ArgumentException>().WithMessage("*Select at least one NetArcaWs persistence capability*");
    }

    [Theory]
    [InlineData(1205, false)]
    [InlineData(1205, true)]
    [InlineData(1222, false)]
    [InlineData(1222, true)]
    public async Task Journal_retries_only_known_sql_server_contention_numbers_with_a_fresh_attempt(int number, bool wrapped)
    {
        var interceptor = new ThrowSqlServerExceptionOnce(number, wrapInEfException: wrapped);
        await using JournalFixture database = await JournalFixture.CreateAsync(interceptor);
        InvoiceSubmission submission = Submission();

        InvoiceOperation operation = await database.Journal.PrepareAsync(submission, TestContext.Current.CancellationToken);

        operation.Submission.Should().BeEquivalentTo(submission);
        interceptor.InjectedExceptions.Should().Be(1);
    }

    [Theory]
    [InlineData(2601, false)]
    [InlineData(2601, true)]
    [InlineData(2627, false)]
    [InlineData(2627, true)]
    public async Task Journal_maps_known_sql_server_unique_numbers_to_domain_conflicts(int number, bool wrapped)
    {
        var interceptor = new ThrowSqlServerExceptionOnce(number, wrapInEfException: wrapped, matchInsert: true);
        await using JournalFixture database = await JournalFixture.CreateAsync(interceptor);

        Func<Task> prepare = () => database.Journal.PrepareAsync(Submission(), TestContext.Current.CancellationToken);

        await prepare.Should().ThrowAsync<InvoiceConflictException>();
        interceptor.InjectedExceptions.Should().Be(1);
    }

    [Theory]
    [InlineData(1223)]
    [InlineData(50001)]
    public async Task Journal_does_not_retry_other_sql_server_errors(int number)
    {
        var interceptor = new ThrowSqlServerExceptionOnce(number);
        await using JournalFixture database = await JournalFixture.CreateAsync(interceptor);

        Func<Task> prepare = () => database.Journal.PrepareAsync(Submission(), TestContext.Current.CancellationToken);

        await prepare.Should().ThrowAsync<SqlException>();
        interceptor.InjectedExceptions.Should().Be(1);
    }

    [Fact]
    public async Task Journal_does_not_retry_another_provider_exception_with_a_lock_like_number()
    {
        var interceptor = new ThrowOtherProviderExceptionOnce();
        await using JournalFixture database = await JournalFixture.CreateAsync(interceptor);

        Func<Task> prepare = () => database.Journal.PrepareAsync(Submission(), TestContext.Current.CancellationToken);

        await prepare.Should().ThrowAsync<OtherProviderException>();
        interceptor.InjectedExceptions.Should().Be(1);
    }

    private static InvoiceSubmission Submission() => new("tenant", "unique-key", "wsfe",
        new InvoiceIdentity(ArcaEnvironment.Homologation, 20999888777, 1, 1, 1), "<synthetic>invoice</synthetic>");

    private sealed class JournalFixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private JournalFixture(SqliteConnection connection, EfInvoiceJournal<ArcaWsDbContext> journal)
        {
            this.connection = connection;
            Journal = journal;
        }

        public EfInvoiceJournal<ArcaWsDbContext> Journal { get; }

        public static async Task<JournalFixture> CreateAsync(DbCommandInterceptor interceptor)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(x => x.AddInvoicing(ArcaService.Wsfev1));
            var baseOptions = new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite(connection).Options;
            await using (var schemaContext = new ArcaWsDbContext(baseOptions, model))
                await schemaContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var options = new DbContextOptionsBuilder<ArcaWsDbContext>().UseSqlite(connection).AddInterceptors(interceptor).Options;
            var factory = new JournalContextFactory(options, model);
            return new JournalFixture(connection, new EfInvoiceJournal<ArcaWsDbContext>(factory, model));
        }

        public async ValueTask DisposeAsync() => await connection.DisposeAsync();
    }

    private sealed class JournalContextFactory(DbContextOptions<ArcaWsDbContext> options, NetArcaWsModelOptions model)
        : IDbContextFactory<ArcaWsDbContext>
    {
        public ArcaWsDbContext CreateDbContext() => new(options, model);
        public Task<ArcaWsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateDbContext());
        }
    }

    private sealed class ThrowSqlServerExceptionOnce(int number, bool wrapInEfException = false, bool matchInsert = false) : DbCommandInterceptor
    {
        private int injectedExceptions;
        public int InjectedExceptions => Volatile.Read(ref injectedExceptions);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if ((!matchInsert || command.CommandText.Contains("INSERT", StringComparison.OrdinalIgnoreCase)) &&
                Interlocked.CompareExchange(ref injectedExceptions, 1, 0) == 0)
            {
                SqlException driverException = SqlServerExceptionFactory.Create(number);
                throw wrapInEfException ? new DbUpdateException("Synthetic provider command failed.", driverException) : driverException;
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ThrowOtherProviderExceptionOnce : DbCommandInterceptor
    {
        private int injectedExceptions;
        public int InjectedExceptions => Volatile.Read(ref injectedExceptions);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.CompareExchange(ref injectedExceptions, 1, 0) == 0)
                throw new OtherProviderException();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class OtherProviderException : DbException
    {
        public override string? SqlState => "HY000";
        public int Number => 1205;
    }

    private static class SqlServerExceptionFactory
    {
        public static SqlException Create(int number)
        {
            Type errorType = typeof(SqlException).Assembly.GetType("Microsoft.Data.SqlClient.SqlError", throwOnError: true)!;
            ConstructorInfo errorConstructor = (ConstructorInfo)RequireSingle(errorType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(constructor =>
                {
                    ParameterInfo[] parameters = constructor.GetParameters();
                    return parameters.Length == 9 && parameters[7].ParameterType == typeof(int) && parameters[8].ParameterType == typeof(Exception);
                }), "SqlError constructor");
            object error = errorConstructor.Invoke([number, (byte)1, (byte)14, "synthetic", "synthetic", "", 0, 0, null]);
            Type collectionType = typeof(SqlException).Assembly.GetType("Microsoft.Data.SqlClient.SqlErrorCollection", throwOnError: true)!;
            object collection = Activator.CreateInstance(collectionType, nonPublic: true)!;
            MethodInfo add = collectionType.GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!;
            add.Invoke(collection, [error]);
            MethodInfo createException = (MethodInfo)RequireSingle(typeof(SqlException).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Where(method =>
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    return method.Name == "CreateException" && parameters.Length == 2 &&
                        parameters[0].ParameterType == collectionType && parameters[1].ParameterType == typeof(string);
                }), "SqlException factory method");
            return (SqlException)createException.Invoke(null, [collection, "10.0"])!;
        }

        private static MemberInfo RequireSingle(IEnumerable<MemberInfo> candidates, string member)
        {
            MemberInfo[] matches = candidates.Take(2).ToArray();
            return matches.Length == 1
                ? matches[0]
                : throw new InvalidOperationException($"Microsoft.Data.SqlClient changed its private {member} used only by this unit-test fixture.");
        }
    }
}
