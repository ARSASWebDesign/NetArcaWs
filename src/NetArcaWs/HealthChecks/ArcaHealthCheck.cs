using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetArcaWs.Wsaa;

namespace NetArcaWs.HealthChecks;

/// <summary>Read-only ARCA probe. Each instance checks exactly one configured service.</summary>
public sealed class ArcaHealthCheck : IHealthCheck
{
    public const string HttpClientName = "NetArcaWs.HealthChecks";
    private readonly IHttpClientFactory clients;
    private readonly ArcaHealthCheckOptions options;

    public ArcaHealthCheck(IHttpClientFactory clients, ArcaHealthCheckOptions options)
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(options);
        this.clients = clients;
        this.options = options.Snapshot();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var profile = HealthProbeProfile.For(options.Service, options.Environment);
        var endpoint = options.Endpoint ?? profile.Endpoint;
        var started = Stopwatch.GetTimestamp();
        var data = new Dictionary<string, object>
        {
            ["service"] = options.Service.ToString(), ["environment"] = options.Environment.ToString(),
            ["endpoint"] = endpoint.AbsoluteUri, ["probe"] = options.Service == ArcaService.Wsaa ? "wsdl" : "dummy",
            ["authenticationVerified"] = false
        };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Timeout);
        try
        {
            using var request = CreateRequest(endpoint, profile);
            using var client = clients.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                data["httpStatus"] = (int)response.StatusCode;
                return Finish(HealthStatus.Unhealthy, "ARCA returned an unsuccessful HTTP status.", "http-error");
            }
            var xml = await ReadBoundedAsync(response.Content, timeout.Token).ConfigureAwait(false);
            if (options.Service == ArcaService.Wsaa)
            {
                using var reader = XmlReader.Create(new StringReader(xml), WsaaXml.ReaderSettings);
                var document = XDocument.Load(reader);
                XNamespace wsdl = "http://schemas.xmlsoap.org/wsdl/";
                if (document.Root?.Name != wsdl + "definitions" || !document.Root.Elements(wsdl + "portType")
                    .Elements(wsdl + "operation").Any(e => (string?)e.Attribute("name") == "loginCms"))
                    return Finish(HealthStatus.Unhealthy, "WSAA did not return its expected WSDL contract.", "invalid-contract");
                return Finish(HealthStatus.Healthy, "WSAA WSDL is reachable. Login and certificate authorization were not tested.");
            }

            var envelope = WsaaXml.Deserialize<HealthSoapEnvelopeDto>(xml);
            var elements = envelope.Body?.Elements ?? [];
            if (elements.Any(e => e.LocalName == "Fault" && e.NamespaceURI == WsaaXml.SoapNamespace))
                return Finish(HealthStatus.Unhealthy, "ARCA returned a SOAP fault.", "soap-fault");
            if (elements.Length != 1) throw new FormatException("Expected one SOAP response element.");
            var result = XElement.Parse(elements[0].OuterXml);
            if (result.Name != XName.Get(profile.Response, profile.Namespace)) throw new FormatException("Unexpected SOAP operation response.");
            if (profile.Result is not null)
                result = result.Elements(XName.Get(profile.Result, profile.ResultNamespace)).Single();
            var healthy = true;
            foreach (var (label, elementName) in profile.Components)
            {
                var value = result.Elements(XName.Get(elementName, profile.ComponentNamespace)).Single().Value.Trim();
                if (value.Length == 0 || value.Length > 64) throw new FormatException("Invalid component status.");
                data[label] = value;
                healthy &= string.Equals(value, "OK", StringComparison.OrdinalIgnoreCase);
            }
            return healthy
                ? Finish(HealthStatus.Healthy, "ARCA reports all infrastructure components OK.")
                : Finish(HealthStatus.Unhealthy, "ARCA reports an infrastructure component unavailable.", "component-down");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Finish(HealthStatus.Unhealthy, "ARCA health probe timed out.", "timeout");
        }
        catch (HttpRequestException)
        {
            return Finish(HealthStatus.Unhealthy, "ARCA could not be reached over HTTPS.", "transport-error");
        }
        catch (IOException)
        {
            return Finish(HealthStatus.Unhealthy, "ARCA response could not be read.", "transport-error");
        }
        catch (Exception ex) when (ex is FormatException or XmlException or InvalidOperationException or DecoderFallbackException)
        {
            return Finish(HealthStatus.Unhealthy, "ARCA returned an invalid or oversized response.", "invalid-response");
        }

        HealthCheckResult Finish(HealthStatus status, string description, string? failure = null)
        {
            data["durationMs"] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            if (failure is not null) data["failure"] = failure;
            return new HealthCheckResult(status, description, data: data);
        }
    }

    private HttpRequestMessage CreateRequest(Uri endpoint, HealthProbeProfile profile)
    {
        if (options.Service == ArcaService.Wsaa)
            return new HttpRequestMessage(HttpMethod.Get, new UriBuilder(endpoint) { Query = "WSDL" }.Uri);
        var document = new XmlDocument { XmlResolver = null };
        var operation = document.CreateElement(profile.Operation, profile.Namespace);
        var envelope = new HealthSoapEnvelopeDto { Body = new HealthSoapBodyDto { Elements = profile.EmptyBody ? [] : [operation] } };
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(WsaaXml.Serialize(envelope), Encoding.UTF8, "text/xml")
        };
        request.Headers.TryAddWithoutValidation("SOAPAction", $"\"{profile.Action}\"");
        return request;
    }

    private async Task<string> ReadBoundedAsync(HttpContent content, CancellationToken token)
    {
        if (content.Headers.ContentLength > options.MaxResponseBytes) throw new FormatException("Response too large.");
        await using var stream = await content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        int read;
        while ((read = await stream.ReadAsync(chunk, token).ConfigureAwait(false)) != 0)
        {
            if (buffer.Length + read > options.MaxResponseBytes) throw new FormatException("Response too large.");
            buffer.Write(chunk, 0, read);
        }
        return new UTF8Encoding(false, true).GetString(buffer.GetBuffer(), 0, (int)buffer.Length).TrimStart('\uFEFF');
    }
}

[XmlRoot("Envelope", Namespace = WsaaXml.SoapNamespace)]
public sealed class HealthSoapEnvelopeDto
{
    [XmlElement("Body", Namespace = WsaaXml.SoapNamespace)] public HealthSoapBodyDto? Body { get; set; }
}

public sealed class HealthSoapBodyDto
{
    [XmlAnyElement] public XmlElement[] Elements { get; set; } = [];
}
