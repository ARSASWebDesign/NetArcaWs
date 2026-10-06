using System.Net;

namespace NetArcaWs.Wsaa;

public sealed class WsaaSoapException(string faultCode, string faultString, string? detail, HttpStatusCode statusCode)
    : Exception($"WSAA SOAP fault: {faultCode}: {faultString}")
{
    public string FaultCode { get; } = faultCode;
    public string FaultString { get; } = faultString;
    public string? Detail { get; } = detail;
    public HttpStatusCode StatusCode { get; } = statusCode;
}
