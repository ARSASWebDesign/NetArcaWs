using AwesomeAssertions;
using NetArcaWs.Tests.TestSupport;
using NetArcaWs.Wsaa;
using Xunit;

namespace NetArcaWs.Tests.Wsaa;

public sealed class WsaaTicketTests
{
    [Fact]
    public void ParseTicket_extracts_credentials_header_fields_and_original_xml()
    {
        DateTimeOffset generated = DateTimeOffset.Parse("2026-10-06T08:00:00+00:00");
        DateTimeOffset expires = DateTimeOffset.Parse("2026-10-06T13:00:00+00:00");
        string xml = WsaaTestData.TicketXml(generated, expires, "my-token", "my-sign", 456);

        WsaaTicket ticket = WsaaService.ParseTicket(xml);

        ticket.Token.Should().Be("my-token");
        ticket.Sign.Should().Be("my-sign");
        ticket.Source.Should().Be("CN=wsaa-test");
        ticket.Destination.Should().Be("CN=service-test");
        ticket.UniqueId.Should().Be(456);
        ticket.GenerationTime.Should().Be(generated);
        ticket.ExpirationTime.Should().Be(expires);
        ticket.Xml.Should().Be(xml);
        ticket.GetTag("token").Should().Be("my-token");
        ticket.GetTag("sign").Should().Be("my-sign");
        ticket.GetTag("absent").Should().BeNull();
    }

    [Fact]
    public void IsExpired_treats_the_expiration_instant_as_expired()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-10-06T09:00:00+00:00");
        WsaaTicket ticket = WsaaService.ParseTicket(
            WsaaTestData.TicketXml(now.AddMinutes(-5), now));

        ticket.IsExpired(now.AddTicks(-1)).Should().BeFalse();
        ticket.IsExpired(now).Should().BeTrue();
        ticket.IsExpired(now.AddTicks(1)).Should().BeTrue();
    }

    [Theory]
    [InlineData("<loginTicketResponse version=\"1.0\"><header/><credentials/></loginTicketResponse>")]
    [InlineData("<loginTicketResponse version=\"2.0\"><header/><credentials/></loginTicketResponse>")]
    [InlineData("<loginTicketResponse version=\"1.0\"><header><source>x</source><destination>y</destination><uniqueId>4294967296</uniqueId><generationTime>2026-10-06T08:00:00Z</generationTime><expirationTime>2026-10-06T09:00:00Z</expirationTime></header><credentials><token>a</token><sign>b</sign></credentials></loginTicketResponse>")]
    [InlineData("<loginTicketResponse version=\"1.0\"><header><source>x</source><destination>y</destination><uniqueId>1</uniqueId><generationTime>2026-10-06T08:00:00</generationTime><expirationTime>2026-10-06T09:00:00Z</expirationTime></header><credentials><token>a</token><sign>b</sign></credentials></loginTicketResponse>")]
    public void ParseTicket_rejects_missing_or_invalid_required_fields(string xml)
    {
        Action parse = () => WsaaService.ParseTicket(xml);

        parse.Should().Throw<FormatException>();
    }

    [Fact]
    public void ParseTicket_prohibits_document_type_declarations()
    {
        const string xml = "<!DOCTYPE loginTicketResponse [<!ENTITY token SYSTEM 'file:///etc/passwd'>]><loginTicketResponse version=\"1.0\"><header/><credentials><token>&token;</token></credentials></loginTicketResponse>";

        Action parse = () => WsaaService.ParseTicket(xml);

        parse.Should().Throw<FormatException>();
    }
}
