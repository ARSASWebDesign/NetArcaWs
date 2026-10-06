using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NetArcaWs.Wsaa;
using System.Xml.Linq;

namespace NetArcaWs.Tests.TestSupport;

internal sealed class AdjustableTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = initialUtcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    internal void Advance(TimeSpan amount) => _utcNow += amount;
}

internal sealed class RecordingHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
{
    private int _requestCount;

    internal int RequestCount => Volatile.Read(ref _requestCount);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _requestCount);
        return responder(request, cancellationToken);
    }

    internal static HttpResponseMessage Response(HttpStatusCode status, string content, string mediaType = "text/xml") =>
        new(status)
        {
            Content = new StringContent(content, Encoding.UTF8, mediaType)
        };
}

internal sealed class TestHttpClientFactory(HttpMessageHandler handler, TimeSpan? clientTimeout = null) : IHttpClientFactory
{
    private int _createCount;
    private string? _lastClientName;

    internal int CreateCount => Volatile.Read(ref _createCount);
    internal string? LastClientName => Volatile.Read(ref _lastClientName);

    public HttpClient CreateClient(string name)
    {
        Interlocked.Increment(ref _createCount);
        Volatile.Write(ref _lastClientName, name);
        var client = new HttpClient(handler, disposeHandler: false);
        if (clientTimeout is { } timeout) client.Timeout = timeout;
        return client;
    }
}

internal static class WsaaTestData
{
    internal const string SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    internal const string WsaaNamespace = "http://wsaa.view.sua.dvadac.desein.afip.gov";

    internal static string TicketXml(
        DateTimeOffset generatedAt,
        DateTimeOffset expiresAt,
        string token = "test-token",
        string sign = "test-sign",
        uint uniqueId = 12345) => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <loginTicketResponse version="1.0">
          <header>
            <source>CN=wsaa-test</source>
            <destination>CN=service-test</destination>
            <uniqueId>{uniqueId}</uniqueId>
            <generationTime>{generatedAt:O}</generationTime>
            <expirationTime>{expiresAt:O}</expirationTime>
          </header>
          <credentials>
            <token>{token}</token>
            <sign>{sign}</sign>
          </credentials>
        </loginTicketResponse>
        """;

    internal static string SoapSuccess(string ticketXml)
    {
        XNamespace soap = SoapNamespace;
        XNamespace wsaa = WsaaNamespace;
        return new XDocument(
            new XElement(soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", soap),
                new XAttribute(XNamespace.Xmlns + "ns", wsaa),
                new XElement(soap + "Body",
                    new XElement(wsaa + "loginCmsResponse",
                        new XElement(wsaa + "loginCmsReturn", ticketXml)))))
            .ToString(SaveOptions.DisableFormatting);
    }

    internal static string SoapFault(string code, string message, string detail = "")
    {
        XNamespace soap = SoapNamespace;
        return new XDocument(
            new XElement(soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", soap),
                new XElement(soap + "Body",
                    new XElement(soap + "Fault",
                        new XElement("faultcode", code),
                        new XElement("faultstring", message),
                        new XElement("detail", detail)))))
            .ToString(SaveOptions.DisableFormatting);
    }

    internal static string SoapResponseWithReturn(string returnXml)
    {
        XNamespace soap = SoapNamespace;
        XNamespace wsaa = WsaaNamespace;
        return new XDocument(
            new XElement(soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", soap),
                new XAttribute(XNamespace.Xmlns + "ns", wsaa),
                new XElement(soap + "Body",
                    new XElement(wsaa + "loginCmsResponse",
                        new XElement(wsaa + "loginCmsReturn", returnXml)))))
            .ToString(SaveOptions.DisableFormatting);
    }
}

internal sealed class WsaaServiceHarness : IDisposable
{
    internal WsaaServiceHarness(
        HttpMessageHandler handler,
        AdjustableTimeProvider? clock = null,
        Uri? endpoint = null,
        TimeSpan? ttl = null,
        int maxResponseBytes = 1024 * 1024,
        TimeSpan? clientTimeout = null)
    {
        Clock = clock ?? new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        Factory = new TestHttpClientFactory(handler, clientTimeout);
        Cache = new MemoryCache(new MemoryCacheOptions());
        Options = Microsoft.Extensions.Options.Options.Create(new WsaaOptions
        {
            Endpoint = endpoint ?? WsaaOptions.HomologationEndpoint,
            TraTimeToLive = ttl ?? TimeSpan.FromMinutes(40),
            MaxResponseBytes = maxResponseBytes,
            AllowedClockSkew = TimeSpan.FromMinutes(2)
        });
        Service = new WsaaService(Factory, Cache, Options, Clock);
    }

    internal AdjustableTimeProvider Clock { get; }
    internal TestHttpClientFactory Factory { get; }
    internal MemoryCache Cache { get; }
    internal IOptions<WsaaOptions> Options { get; }
    internal WsaaService Service { get; }

    public void Dispose() => Cache.Dispose();
}

internal sealed class BlockingReadHttpContent : HttpContent
{
    protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context) =>
        throw new NotSupportedException("This content streams only during response reads.");

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    protected override Task<Stream> CreateContentReadStreamAsync() => Task.FromResult<Stream>(new BlockingReadStream());
}

internal sealed class BlockingReadStream : Stream
{
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return 0;
    }
}
