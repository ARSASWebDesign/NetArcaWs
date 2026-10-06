using NetArcaWs.HealthChecks;

namespace NetArcaWs.Services;

internal static class ArcaServiceEndpoints
{
    public static readonly Uri WscdcHomologation = new("https://wswhomo.afip.gov.ar/WSCDC/service.asmx");
    public static readonly Uri WscdcProduction = new("https://servicios1.afip.gov.ar/WSCDC/service.asmx");
    public static readonly Uri WscpeHomologation = new("https://cpea-ws-qaext.afip.gob.ar/wscpe/services/soap");
    public static readonly Uri WscpeProduction = new("https://cpea-ws.afip.gob.ar/wscpe/services/soap");
    public static readonly Uri WsfecredHomologation = new("https://fwshomo.afip.gov.ar/wsfecred/FECredService");
    public static readonly Uri WsfecredProduction = new("https://serviciosjava.afip.gob.ar/wsfecred/FECredService");
    public static readonly Uri WsfeHomologation = new("https://wswhomo.afip.gov.ar/wsfev1/service.asmx");
    public static readonly Uri WsfeProduction = new("https://servicios1.afip.gov.ar/wsfev1/service.asmx");
    public static readonly Uri WsfexHomologation = new("https://wswhomo.afip.gov.ar/wsfexv1/service.asmx");
    public static readonly Uri WsfexProduction = new("https://servicios1.afip.gov.ar/wsfexv1/service.asmx");
    public static readonly Uri WsmtxcaHomologation = new("https://fwshomo.afip.gov.ar/wsmtxca/services/MTXCAService");
    public static readonly Uri WsmtxcaProduction = new("https://serviciosjava.afip.gob.ar/wsmtxca/services/MTXCAService");
    public static readonly Uri PadronA4Homologation = new("https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA4");
    public static readonly Uri PadronA4Production = new("https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA4");
    public static readonly Uri PadronA5Homologation = new("https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5");
    public static readonly Uri PadronA5Production = new("https://aws.arca.gob.ar/sr-padron/webservices/personaServiceA5");
    public static readonly Uri PadronA10Homologation = new("https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA10");
    public static readonly Uri PadronA10Production = new("https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA10");
    public static readonly Uri PadronA13Homologation = new("https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA13");
    public static readonly Uri PadronA13Production = new("https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA13");

    public static Uri Select(ArcaEnvironment environment, Uri homologation, Uri production)
    {
        if (!Enum.IsDefined(environment)) throw new ArgumentOutOfRangeException(nameof(environment));
        return environment == ArcaEnvironment.Production ? production : homologation;
    }
}
