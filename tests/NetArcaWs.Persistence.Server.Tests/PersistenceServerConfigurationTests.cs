using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.Persistence.Server.Tests;

public sealed class PersistenceServerConfigurationTests
{
    [Fact]
    public void No_configuration_is_an_explicit_non_requested_opt_in()
    {
        PersistenceServerSettings.FromValues(null, null, null).Should().BeNull();
    }

    [Theory]
    [InlineData("Host=localhost;Database=test", null, "17.6")]
    [InlineData("", null, null)]
    [InlineData("Host=localhost;Database=test", "postgresql", null)]
    [InlineData(null, "postgresql", "17.6")]
    [InlineData("Host=localhost;Database=test", "sqlserver", "not-a-version")]
    [InlineData("Host=localhost;Database=test", "unknown", "17.6")]
    public void Partial_or_invalid_opt_in_configuration_fails_clearly(string? connection, string? kind, string? version)
    {
        Action read = () => PersistenceServerSettings.FromValues(connection, kind, version);

        read.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("postgresql", "17.6")]
    [InlineData("sqlserver", "17.0")]
    [InlineData("mysql", "8.4.11")]
    [InlineData("mariadb", "11.4.13")]
    public void Complete_known_engine_configuration_is_accepted(string kind, string version)
    {
        PersistenceServerSettings settings = PersistenceServerSettings.FromValues(
            "Host=localhost;Database=test", kind, version)!;

        settings.Kind.Should().Be(kind);
        settings.Version.ToString().Should().Be(version);
    }
}

internal sealed record PersistenceServerSettings(string ConnectionString, string Kind, Version Version)
{
    public static PersistenceServerSettings? FromEnvironment()
        => FromValues(Environment.GetEnvironmentVariable("NETARCA_PERSISTENCE_DB"),
            Environment.GetEnvironmentVariable("NETARCA_PERSISTENCE_DB_KIND"),
            Environment.GetEnvironmentVariable("NETARCA_PERSISTENCE_DB_VERSION"));

    public static PersistenceServerSettings? FromValues(string? connection, string? kind, string? version)
    {
        bool anySet = connection is not null || kind is not null || version is not null;
        if (!anySet) return null;
        if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(version))
            throw new InvalidOperationException("Persistence engine opt-in was requested but NETARCA_PERSISTENCE_DB, NETARCA_PERSISTENCE_DB_KIND, or NETARCA_PERSISTENCE_DB_VERSION is missing.");
        if (!Version.TryParse(version, out Version? parsed))
            throw new InvalidOperationException("NETARCA_PERSISTENCE_DB_VERSION must be a dotted numeric server version.");
        if (kind is not ("mysql" or "mariadb" or "postgresql" or "sqlserver"))
            throw new InvalidOperationException("NETARCA_PERSISTENCE_DB_KIND must be mysql, mariadb, postgresql, or sqlserver.");
        return new PersistenceServerSettings(connection, kind, parsed);
    }
}
