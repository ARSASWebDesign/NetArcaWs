using AwesomeAssertions;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.HealthChecks;
using Xunit;

namespace NetArcaWs.EntityFrameworkCore.Tests;

public sealed class ArcaCertificateScopeUnicodeTests
{
    [Fact]
    public void ScopeRejectsDistinctIllFormedUtf16TenantIdsAndAcceptsValidSupplementaryCharacters()
    {
        string firstMalformedId = "tenant-\uD800";
        string secondMalformedId = "tenant-\uD801";
        string lowSurrogateId = "tenant-\uDC00";
        string malformedMixedId = "tenant-\uD800x";
        firstMalformedId.Should().NotBe(secondMalformedId);

        Action first = () => new ArcaCertificateScope(firstMalformedId, 20_123_456_789, ArcaEnvironment.Production);
        Action second = () => new ArcaCertificateScope(secondMalformedId, 20_123_456_789, ArcaEnvironment.Production);
        Action low = () => new ArcaCertificateScope(lowSurrogateId, 20_123_456_789, ArcaEnvironment.Production);
        Action mixed = () => new ArcaCertificateScope(malformedMixedId, 20_123_456_789, ArcaEnvironment.Production);
        first.Should().Throw<ArgumentException>().WithMessage("*well-formed Unicode*");
        second.Should().Throw<ArgumentException>().WithMessage("*well-formed Unicode*");
        low.Should().Throw<ArgumentException>().WithMessage("*well-formed Unicode*");
        mixed.Should().Throw<ArgumentException>().WithMessage("*well-formed Unicode*");

        var validSupplementaryId = new ArcaCertificateScope("tenant-😀", 20_123_456_789, ArcaEnvironment.Production);
        validSupplementaryId.TenantId.Should().Be("tenant-😀");
        var validSurrogatePairId = new ArcaCertificateScope("tenant-\uD83D\uDE00-works", 20_123_456_789, ArcaEnvironment.Production);
        validSurrogatePairId.TenantId.Should().Be("tenant-😀-works");
    }
}
