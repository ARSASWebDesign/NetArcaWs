using NetArcaWs.Contracts.PadronA4;
using NetArcaWs.Contracts.PadronA5;
using NetArcaWs.Contracts.PadronA10;
using NetArcaWs.Contracts.PadronA13;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using Xunit;
using Wsfex = NetArcaWs.Contracts.WsfexV1;
using Wsmtxca = NetArcaWs.Contracts.Wsmtxca;
using Wscdc = NetArcaWs.Contracts.Wscdc;
using WsfeCred = NetArcaWs.Contracts.WsfeCred;

namespace NetArcaWs.IntegrationTests;

/// <summary>Assertions shared by the opt-in authenticated read-only homologation probes.</summary>
internal static class AuthenticatedLookupValidation
{
    public static async Task<IReadOnlyList<PtoVenta>> ValidateWsfeAsync(
        IWsfev1Service client, ArcaTenantContext tenant, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(tenant);
        Assert.Equal(ArcaEnvironment.Homologation, tenant.Environment);

        ValidateCatalog<FeParamGetTiposMonedasResponse, MonedaResponse>(await client.FEParamGetTiposMonedasAsync(tenant, new FeParamGetTiposMonedas(), ct),
            r => r.FeParamGetTiposMonedasResult, "WSFE currency");
        ValidateCatalog<FeParamGetTiposCbteResponse, CbteTipoResponse>(await client.FEParamGetTiposCbteAsync(tenant, new FeParamGetTiposCbte(), ct),
            r => r.FeParamGetTiposCbteResult, "WSFE voucher type");
        ValidateCatalog<FeParamGetTiposDocResponse, DocTipoResponse>(await client.FEParamGetTiposDocAsync(tenant, new FeParamGetTiposDoc(), ct),
            r => r.FeParamGetTiposDocResult, "WSFE document type");
        ValidateCatalog<FeParamGetTiposConceptoResponse, ConceptoTipoResponse>(await client.FEParamGetTiposConceptoAsync(tenant, new FeParamGetTiposConcepto(), ct),
            r => r.FeParamGetTiposConceptoResult, "WSFE concept type");
        ValidateCatalog<FeParamGetTiposIvaResponse, IvaTipoResponse>(await client.FEParamGetTiposIvaAsync(tenant, new FeParamGetTiposIva(), ct),
            r => r.FeParamGetTiposIvaResult, "WSFE VAT type");
        ValidateCatalog<FeParamGetCondicionIvaReceptorResponse, CondicionIvaReceptorResponse>(await client.FEParamGetCondicionIvaReceptorAsync(tenant, new FeParamGetCondicionIvaReceptor(), ct),
            r => r.FeParamGetCondicionIvaReceptorResult, "WSFE recipient VAT condition");

        FeParamGetPtosVentaResponse points = await client.FEParamGetPtosVentaAsync(tenant, new FeParamGetPtosVenta(), ct);
        FePtoVentaResponse pointResult = Assert.IsType<FePtoVentaResponse>(points?.FeParamGetPtosVentaResult);
        AssertNoErrors(pointResult.Errors, "WSFE points of sale");
        Assert.NotNull(pointResult.ResultGet);
        PtoVenta[] returnedPoints = pointResult.ResultGet.ToArray();
        Assert.All(returnedPoints, point =>
        {
            Assert.NotNull(point);
            Assert.True(point!.Nro > 0, "WSFE point of sale should include a positive number.");
        });

        FeCompTotXRequestResponse maxResponse = await client.FECompTotXRequestAsync(tenant, new FeCompTotXRequest(), ct);
        FeRegXReqResponse maxResult = Assert.IsType<FeRegXReqResponse>(maxResponse?.FeCompTotXRequestResult);
        AssertNoErrors(maxResult.Errors, "WSFE maximum request count");
        Assert.True(maxResult.RegXReq > 0, "WSFE maximum request count should be positive.");

        return returnedPoints;
    }

    /// <summary>Validates supported response DTOs without emitting response contents.</summary>
    public static void ValidateResponse(object response)
    {
        ArgumentNullException.ThrowIfNull(response);
        switch (response)
        {
            case Wsfex.FexGetParamMonResponse r:
                Wsfex.FexResponseMon fex = Assert.IsType<Wsfex.FexResponseMon>(r.FexGetParamMonResult);
                Assert.True(fex.FexErr is null || fex.FexErr.ErrCode == 0, "WSFEX currency lookup should not report an error.");
                Assert.True(fex.FexResultGet.Any(), "WSFEX currency catalog should not be empty.");
                foreach (Wsfex.ClsFexResponseMon item in fex.FexResultGet)
                {
                    Assert.True(item is not null, "WSFEX currency catalog should not contain null items.");
                    Assert.False(string.IsNullOrWhiteSpace(item!.MonId), "WSFEX currency should include an identifier.");
                    Assert.False(string.IsNullOrWhiteSpace(item.MonDs), "WSFEX currency should include a description.");
                }
                break;
            case Wsmtxca.ConsultarMonedasResponseType r:
                Assert.NotNull(r.ArrayMonedas);
                Assert.True(r.ArrayMonedas.Any(), "WSMTXCA currency catalog should not be empty.");
                foreach (Wsmtxca.CodigoDescripcionStringType item in r.ArrayMonedas)
                {
                    Assert.True(item is not null, "WSMTXCA currency catalog should not contain null items.");
                    Assert.False(string.IsNullOrWhiteSpace(item!.Codigo), "WSMTXCA currency should include an identifier.");
                    Assert.False(string.IsNullOrWhiteSpace(item.Descripcion), "WSMTXCA currency should include a description.");
                }
                break;
            case Wscdc.ComprobantesModalidadConsultarResponse r:
                Wscdc.FacModTipoResponse modes = Assert.IsType<Wscdc.FacModTipoResponse>(r.ComprobantesModalidadConsultarResult);
                AssertNoErrors(modes.Errors, "WSCDC modalities");
                Assert.True(modes.ResultGet.Any(), "WSCDC modalities should not be empty.");
                foreach (Wscdc.FacModTipo item in modes.ResultGet)
                {
                    Assert.True(item is not null, "WSCDC modality catalog should not contain null items.");
                    Assert.False(string.IsNullOrWhiteSpace(item!.Cod), "WSCDC modality should include an identifier.");
                    Assert.False(string.IsNullOrWhiteSpace(item.Desc), "WSCDC modality should include a description.");
                }
                break;
            case WsfeCred.ConsultarTiposRetencionesResponseType r:
                WsfeCred.ConsultarTiposRetencionesReturnType retenciones = Assert.IsType<WsfeCred.ConsultarTiposRetencionesReturnType>(r.ConsultarTiposRetencionesReturn);
                AssertNoErrors(retenciones.ArrayErroresFormato, "WSFECred retention types");
                foreach (WsfeCred.TipoRetencionType item in retenciones.ArrayTiposRetenciones)
                {
                    Assert.True(item is not null, "WSFECred retention catalog should not contain null items.");
                    Assert.False(string.IsNullOrWhiteSpace(item!.DescripcionJurisdiccion), "WSFECred retention should include a jurisdiction description.");
                    Assert.True(item.CodigoJurisdiccion >= 0, "WSFECred retention should include a valid jurisdiction code.");
                }
                break;
            case NetArcaWs.Contracts.PadronA4.GetPersonaResponse r:
                ValidatePadronA4(r);
                break;
            case NetArcaWs.Contracts.PadronA5.GetPersonaResponse r:
                ValidatePadronA5(r);
                break;
            case NetArcaWs.Contracts.PadronA10.GetPersonaResponse r:
                ValidatePadronA10(r);
                break;
            case NetArcaWs.Contracts.PadronA13.GetPersonaResponse r:
                ValidatePadronA13(r);
                break;
            default:
                throw new InvalidOperationException("Unsupported authenticated lookup response type.");
        }
    }

    private static void ValidateCatalog<TResponse, TResult>(TResponse? response,
        Func<TResponse, TResult?> resultSelector, string label)
        where TResponse : class where TResult : class
    {
        TResult result = Assert.IsType<TResult>(response is null ? null : resultSelector(response));
        // All six WSFE result DTOs have the same generated error and ResultGet members.
        switch (result)
        {
            case MonedaResponse x: AssertNoErrors(x.Errors, label); AssertCatalog(x.ResultGet, label, item => item.Id, item => item.Desc); break;
            case CbteTipoResponse x: AssertNoErrors(x.Errors, label); AssertCatalog(x.ResultGet, label, item => item.Id, item => item.Desc); break;
            case DocTipoResponse x: AssertNoErrors(x.Errors, label); AssertCatalog(x.ResultGet, label, item => item.Id, item => item.Desc); break;
            case ConceptoTipoResponse x: AssertNoErrors(x.Errors, label); AssertCatalog(x.ResultGet, label, item => item.Id, item => item.Desc); break;
            case IvaTipoResponse x: AssertNoErrors(x.Errors, label); AssertCatalog(x.ResultGet, label, item => item.Id, item => item.Desc); break;
            case CondicionIvaReceptorResponse x: AssertNoErrors(x.Errors, label); AssertCatalog(x.ResultGet, label, item => item.Id, item => item.Desc); break;
            default: throw new InvalidOperationException("Unexpected WSFE catalog result type.");
        }
    }

    private static void AssertCatalog<T>(IEnumerable<T> entries, string label, Func<T, object?> id, Func<T, string?> description)
    {
        Assert.NotNull(entries);
        Assert.True(entries.Any(), $"{label} catalog should not be empty.");
        foreach (T item in entries)
        {
            Assert.True(item is not null, $"{label} entries should not contain null items.");
            Assert.True(id(item!) switch
            {
                null => false,
                string text => !string.IsNullOrWhiteSpace(text),
                _ => true
            }, $"{label} entries should include an identifier.");
            Assert.True(!string.IsNullOrWhiteSpace(description(item!)), $"{label} entries should include a description.");
        }
    }

    private static void AssertNoErrors<T>(IEnumerable<T>? errors, string label)
    {
        // Some Padron error branches are optional: null means no errors were returned.
        if (errors is null) return;
        Assert.True(errors is null || !errors.Any(), $"{label} should return no errors.");
    }

    private static void ValidatePadronA4(NetArcaWs.Contracts.PadronA4.GetPersonaResponse response)
    {
        NetArcaWs.Contracts.PadronA4.PersonaReturn result = Assert.IsType<NetArcaWs.Contracts.PadronA4.PersonaReturn>(response.PersonaReturn);
        NetArcaWs.Contracts.PadronA4.Persona persona = Assert.IsType<NetArcaWs.Contracts.PadronA4.Persona>(result.Persona);
        Assert.True(!string.IsNullOrWhiteSpace(persona.Apellido) || !string.IsNullOrWhiteSpace(persona.Nombre) ||
            !string.IsNullOrWhiteSpace(persona.EstadoClave) ||
            !string.IsNullOrWhiteSpace(persona.RazonSocial) || persona.Actividad.Count > 0 || persona.Domicilio.Count > 0,
            "Padron A4 person data should include a populated identity or business detail.");
    }

    private static void ValidatePadronA5(NetArcaWs.Contracts.PadronA5.GetPersonaResponse response)
    {
        NetArcaWs.Contracts.PadronA5.PersonaReturn result = Assert.IsType<NetArcaWs.Contracts.PadronA5.PersonaReturn>(response.PersonaReturn);
        AssertNoErrors(result.ErrorConstancia?.Error, "Padron A5 constancia");
        AssertNoErrors(result.ErrorMonotributo?.Error, "Padron A5 monotributo");
        AssertNoErrors(result.ErrorRegimenGeneral?.Error, "Padron A5 regimen general");
        NetArcaWs.Contracts.PadronA5.DatosGenerales general = Assert.IsType<NetArcaWs.Contracts.PadronA5.DatosGenerales>(result.DatosGenerales);
        Assert.True(!string.IsNullOrWhiteSpace(general.Apellido) || !string.IsNullOrWhiteSpace(general.Nombre) ||
            !string.IsNullOrWhiteSpace(general.RazonSocial) || !string.IsNullOrWhiteSpace(general.EstadoClave),
            "Padron A5 general data should include a populated identity or status field.");
    }

    private static void ValidatePadronA10(NetArcaWs.Contracts.PadronA10.GetPersonaResponse response)
    {
        NetArcaWs.Contracts.PadronA10.PersonaReturn result = Assert.IsType<NetArcaWs.Contracts.PadronA10.PersonaReturn>(response.PersonaReturn);
        NetArcaWs.Contracts.PadronA10.Persona persona = Assert.IsType<NetArcaWs.Contracts.PadronA10.Persona>(result.Persona);
        Assert.True(!string.IsNullOrWhiteSpace(persona.Apellido) || !string.IsNullOrWhiteSpace(persona.Nombre) ||
            !string.IsNullOrWhiteSpace(persona.RazonSocial) || !string.IsNullOrWhiteSpace(persona.EstadoClave) ||
            !string.IsNullOrWhiteSpace(persona.DescripcionActividadPrincipal),
            "Padron A10 person data should include a populated identity or business detail.");
    }

    private static void ValidatePadronA13(NetArcaWs.Contracts.PadronA13.GetPersonaResponse response)
    {
        NetArcaWs.Contracts.PadronA13.PersonaReturn result = Assert.IsType<NetArcaWs.Contracts.PadronA13.PersonaReturn>(response.PersonaReturn);
        NetArcaWs.Contracts.PadronA13.Persona persona = Assert.IsType<NetArcaWs.Contracts.PadronA13.Persona>(result.Persona);
        Assert.True(!string.IsNullOrWhiteSpace(persona.Apellido) || !string.IsNullOrWhiteSpace(persona.Nombre) ||
            !string.IsNullOrWhiteSpace(persona.RazonSocial) || !string.IsNullOrWhiteSpace(persona.EstadoClave),
            "Padron A13 person data should include a populated identity or status field.");
    }
}
