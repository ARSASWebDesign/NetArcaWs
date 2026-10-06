using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Options;

namespace NetArcaWs.Transport;

/// <summary>Bounded SOAP 1.1 transport. Does not retry requests or log credentials.</summary>
public sealed class SoapTransport : ISoapTransport
{
    public const string HttpClientName = "NetArcaWs.Soap";
    private const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private readonly IHttpClientFactory clients;
    private readonly int maxResponseBytes;
    private readonly int maxRequestBytes;

    public SoapTransport(IHttpClientFactory clients, IOptions<SoapTransportOptions> options)
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(options);
        if (options.Value.MaxResponseBytes is < 1024 or > 32 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(options), "Response limit must be between 1 KiB and 32 MiB.");
        if (options.Value.MaxRequestBytes is < 1024 or > 32 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(options), "Request limit must be between 1 KiB and 32 MiB.");
        maxRequestBytes = options.Value.MaxRequestBytes;
        this.clients = clients;
        maxResponseBytes = options.Value.MaxResponseBytes;
    }

    public async Task<TResponse> SendAsync<TRequest, TResponse>(Uri endpoint, string action, TRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(action);
        if (!endpoint.IsAbsoluteUri || endpoint.Scheme != Uri.UriSchemeHttps || endpoint.UserInfo.Length != 0 ||
            endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0)
            throw new ArgumentException("SOAP endpoint must be an absolute HTTPS URL without credentials, query or fragment.", nameof(endpoint));
        if (action.Any(c => char.IsControl(c) || c == '"')) throw new ArgumentException("Invalid SOAPAction.", nameof(action));
        cancellationToken.ThrowIfCancellationRequested();
        using var buffer = new BoundedMemoryStream(maxRequestBytes);
        using (var writer = XmlWriter.Create(buffer, new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("soap", "Envelope", SoapNamespace);
            writer.WriteStartElement("soap", "Body", SoapNamespace);
            if (request is not null) new XmlSerializer(typeof(TRequest)).Serialize(writer, request);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }
        buffer.Position = 0;
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StreamContent(buffer)
        };
        message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };
        message.Headers.TryAddWithoutValidation("SOAPAction", $"\"{action}\"");
        using var client = clients.CreateClient(HttpClientName);
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (client.Timeout != Timeout.InfiniteTimeSpan) operation.CancelAfter(client.Timeout);
        try
        {
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, operation.Token).ConfigureAwait(false);
            if (response.Content.Headers.ContentLength > maxResponseBytes) throw new FormatException("SOAP response exceeds the size limit.");
            await using var source = await response.Content.ReadAsStreamAsync(operation.Token).ConfigureAwait(false);
            using var received = new MemoryStream();
            var chunk = new byte[8192];
            int count;
            while ((count = await source.ReadAsync(chunk, operation.Token).ConfigureAwait(false)) != 0)
            {
                if (received.Length + count > maxResponseBytes) throw new FormatException("SOAP response exceeds the size limit.");
                received.Write(chunk, 0, count);
            }
            received.Position = 0;
            XElement? body;
            try
            {
                using var reader = XmlReader.Create(received, new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = maxResponseBytes
                });
                var document = XDocument.Load(reader);
                XNamespace soap = SoapNamespace;
                if (document.Root?.Name != soap + "Envelope") throw new FormatException("Expected a SOAP 1.1 Envelope.");
                var bodies = document.Root.Elements(soap + "Body").ToArray();
                if (bodies.Length != 1 || bodies[0].Elements().Count() != 1) throw new FormatException("Expected exactly one SOAP response body element.");
                body = bodies[0].Elements().Single();
            }
            catch (Exception ex) when (ex is XmlException or FormatException)
            {
                response.EnsureSuccessStatusCode();
                throw new FormatException("Invalid SOAP response.", ex);
            }
            if (body.Name == XName.Get("Fault", SoapNamespace))
                throw new SoapFaultException(body.Element("faultcode")?.Value ?? "unknown",
                    body.Element("faultstring")?.Value ?? "Unspecified SOAP fault", body.Element("detail")?.ToString(SaveOptions.DisableFormatting), response.StatusCode);
            response.EnsureSuccessStatusCode();
            try
            {
                // XNodeReader cannot decode xs:base64Binary. Re-read the bounded, validated
                // envelope with a binary-capable reader, preserving ancestor namespace bindings.
                received.Position = 0;
                using var reader = XmlReader.Create(received, new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = maxResponseBytes
                });
                reader.MoveToContent();
                reader.ReadStartElement("Envelope", SoapNamespace);
                while (reader.MoveToContent() == XmlNodeType.Element && !reader.IsStartElement("Body", SoapNamespace))
                    reader.Skip();
                reader.ReadStartElement("Body", SoapNamespace);
                return (TResponse)(new XmlSerializer(typeof(TResponse)).Deserialize(reader)
                    ?? throw new FormatException("Missing SOAP response."));
            }
            catch (InvalidOperationException ex) { throw new FormatException("SOAP response does not match its expected contract.", ex); }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && operation.IsCancellationRequested)
        {
            throw new TaskCanceledException("The SOAP operation exceeded its HTTP timeout.", new TimeoutException("ARCA SOAP timeout.", ex), operation.Token);
        }
    }
}
