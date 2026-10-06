using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace NetArcaWs.Wsaa;

internal static class WsaaXml
{
    internal const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    internal const string ServiceNamespace = "http://wsaa.view.sua.dvadac.desein.afip.gov";
    internal static XmlReaderSettings ReaderSettings => new()
    {
        DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
        MaxCharactersInDocument = 16 * 1024 * 1024, IgnoreComments = true
    };

    internal static string Serialize<T>(T value)
    {
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) }))
        {
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");
            new XmlSerializer(typeof(T)).Serialize(writer, value, namespaces);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static T Deserialize<T>(string xml)
    {
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), ReaderSettings);
            var result = (T?)new XmlSerializer(typeof(T)).Deserialize(reader)
                ?? throw new FormatException("Empty WSAA XML document.");
            while (reader.Read()) { }
            return result;
        }
        catch (Exception ex) when (ex is InvalidOperationException or XmlException)
        {
            throw new FormatException("Invalid WSAA XML document.", ex);
        }
    }
}

[XmlRoot("loginTicketRequest")]
public sealed class LoginTicketRequestDto
{
    [XmlAttribute("version")] public string Version { get; set; } = "1.0";
    [XmlElement("header", Order = 0)] public TicketHeaderDto Header { get; set; } = new();
    [XmlElement("service", Order = 1)] public string Service { get; set; } = "";
}

[XmlRoot("loginTicketResponse")]
public sealed class LoginTicketResponseDto
{
    [XmlAttribute("version")] public string Version { get; set; } = "1.0";
    [XmlElement("header", Order = 0)] public TicketHeaderDto? Header { get; set; }
    [XmlElement("credentials", Order = 1)] public TicketCredentialsDto? Credentials { get; set; }
}

public sealed class TicketHeaderDto
{
    [XmlElement("source", Order = 0)] public string? Source { get; set; }
    [XmlElement("destination", Order = 1)] public string? Destination { get; set; }
    [XmlElement("uniqueId", Order = 2)] public string? UniqueId { get; set; }
    [XmlElement("generationTime", Order = 3)] public string? GenerationTime { get; set; }
    [XmlElement("expirationTime", Order = 4)] public string? ExpirationTime { get; set; }
}

public sealed class TicketCredentialsDto
{
    [XmlElement("token", Order = 0)] public string? Token { get; set; }
    [XmlElement("sign", Order = 1)] public string? Sign { get; set; }
    public override string ToString() => "WSAA credentials (redacted)";
}

[XmlRoot("Envelope", Namespace = WsaaXml.SoapNamespace)]
public sealed class WsaaSoapEnvelopeDto
{
    [XmlElement("Body", Namespace = WsaaXml.SoapNamespace)] public WsaaSoapBodyDto? Body { get; set; }
}

public sealed class WsaaSoapBodyDto
{
    [XmlElement("loginCms", Namespace = WsaaXml.ServiceNamespace)] public LoginCmsDto? Request { get; set; }
    [XmlElement("loginCmsResponse", Namespace = WsaaXml.ServiceNamespace)] public LoginCmsResponseDto? Response { get; set; }
    [XmlElement("Fault", Namespace = WsaaXml.SoapNamespace)] public SoapFaultDto? Fault { get; set; }
}

public sealed class LoginCmsDto
{
    [XmlElement("in0", Namespace = WsaaXml.ServiceNamespace)] public string Cms { get; set; } = "";
}

public sealed class LoginCmsResponseDto
{
    [XmlElement("loginCmsReturn", Namespace = WsaaXml.ServiceNamespace)] public string? Ticket { get; set; }
}

public sealed class SoapFaultDto
{
    [XmlElement("faultcode", Namespace = "")] public string? Code { get; set; }
    [XmlElement("faultstring", Namespace = "")] public string? Message { get; set; }
    [XmlAnyElement(Name = "detail", Namespace = "")] public System.Xml.XmlElement? Detail { get; set; }
}
