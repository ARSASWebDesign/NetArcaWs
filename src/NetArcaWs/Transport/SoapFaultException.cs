using System.Net;

namespace NetArcaWs.Transport;

public sealed class SoapFaultException(string code, string reason, string? detail, HttpStatusCode statusCode)
    : Exception($"ARCA SOAP fault: {code}: {reason}")
{
    public string Code { get; } = code;
    public string Reason { get; } = reason;
    public string? Detail { get; } = detail;
    public HttpStatusCode StatusCode { get; } = statusCode;
}
