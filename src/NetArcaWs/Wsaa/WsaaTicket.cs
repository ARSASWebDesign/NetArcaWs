using System.Xml;
using System.Xml.Linq;

namespace NetArcaWs.Wsaa;

public sealed record WsaaTicket(
    string Token, string Sign, DateTimeOffset GenerationTime, DateTimeOffset ExpirationTime,
    string Source, string Destination, uint UniqueId, string Xml)
{
    public bool IsExpired(DateTimeOffset now) => now >= ExpirationTime;

    public string? GetTag(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        using var reader = XmlReader.Create(new StringReader(Xml), WsaaXml.ReaderSettings);
        return XDocument.Load(reader).Descendants(XName.Get(name)).FirstOrDefault()?.Value;
    }

    public override string ToString() => $"WSAA ticket {UniqueId}, expires {ExpirationTime:O} (credentials redacted)";
}
