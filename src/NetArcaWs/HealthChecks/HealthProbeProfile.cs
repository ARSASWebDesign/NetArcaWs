using NetArcaWs.Wsaa;
using NetArcaWs.Services;

namespace NetArcaWs.HealthChecks;

internal sealed record HealthProbeProfile(Uri Endpoint, string Namespace, string Operation, string Action,
    string Response, string? Result, string ResultNamespace, string ComponentNamespace,
    IReadOnlyDictionary<string, string> Components, bool EmptyBody = false)
{
    private static readonly IReadOnlyDictionary<string, string> PascalComponents = new Dictionary<string, string>
    {
        ["appServer"] = "AppServer", ["dbServer"] = "DbServer", ["authServer"] = "AuthServer"
    };
    private static readonly IReadOnlyDictionary<string, string> LowerComponents = new Dictionary<string, string>
    {
        ["appServer"] = "appserver", ["dbServer"] = "dbserver", ["authServer"] = "authserver"
    };

    internal static HealthProbeProfile For(ArcaService service, ArcaEnvironment environment)
    {
        var production = environment == ArcaEnvironment.Production;
        return service switch
        {
            ArcaService.Wsaa => new(production ? WsaaOptions.ProductionEndpoint : WsaaOptions.HomologationEndpoint,
                "", "", "", "", null, "", "", LowerComponents),
            ArcaService.Wsfev1 => Invoice("wsfev1", "http://ar.gov.afip.dif.FEV1/", "FEDummy", production),
            ArcaService.Wsfexv1 => Invoice("wsfexv1", "http://ar.gov.afip.dif.fexv1/", "FEXDummy", production),
            ArcaService.Wsmtxca => new(production ? ArcaServiceEndpoints.WsmtxcaProduction : ArcaServiceEndpoints.WsmtxcaHomologation,
                "http://impl.service.wsmtxca.afip.gov.ar/service/", "dummy",
                "http://impl.service.wsmtxca.afip.gov.ar/service/dummy", "dummyResponse", null, "", "", LowerComponents, EmptyBody: true),
            ArcaService.PadronA4 => Padron(4, production),
            ArcaService.PadronA5 => Padron(5, production),
            ArcaService.PadronA10 => Padron(10, production),
            ArcaService.PadronA13 => Padron(13, production),
            ArcaService.Wscdc => new(production ? ArcaServiceEndpoints.WscdcProduction : ArcaServiceEndpoints.WscdcHomologation,
                "http://servicios1.afip.gob.ar/wscdc/", "ComprobanteDummy",
                "http://servicios1.afip.gob.ar/wscdc/ComprobanteDummy", "ComprobanteDummyResponse", "ComprobanteDummyResult",
                "http://servicios1.afip.gob.ar/wscdc/", "http://servicios1.afip.gob.ar/wscdc/", PascalComponents),
            ArcaService.Wsfecred => new(production ? ArcaServiceEndpoints.WsfecredProduction : ArcaServiceEndpoints.WsfecredHomologation,
                "http://ar.gob.afip.wsfecred/FECredService/", "dummy",
                "http://ar.gob.afip.wsfecred/FECredService/dummy", "dummyResponse", "dummyReturn", "", "", LowerComponents, EmptyBody: true),
            _ => throw new ArgumentOutOfRangeException(nameof(service))
        };
    }

    private static HealthProbeProfile Invoice(string path, string ns, string operation, bool production)
        => new(new Uri($"https://{(production ? "servicios1" : "wswhomo")}.afip.gov.ar/{path}/service.asmx"),
            ns, operation, ns + operation, operation + "Response", operation + "Result", ns, ns, PascalComponents);

    private static HealthProbeProfile Padron(int version, bool production)
        => new(version == 5
                ? (production ? ArcaServiceEndpoints.PadronA5Production : ArcaServiceEndpoints.PadronA5Homologation)
                : new Uri($"https://{(production ? "aws" : "awshomo")}.afip.gov.ar/sr-padron/webservices/personaServiceA{version}"),
            $"http://a{version}.soap.ws.server.puc.sr/", "dummy", "", "dummyResponse", "return", "", "", LowerComponents);
}
