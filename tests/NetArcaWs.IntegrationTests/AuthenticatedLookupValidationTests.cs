using NetArcaWs.Contracts.PadronA4;
using NetArcaWs.Contracts.PadronA5;
using NetArcaWs.Contracts.PadronA10;
using NetArcaWs.Contracts.PadronA13;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using System.Reflection;
using Xunit;
using Xunit.Sdk;
using Wsfex = NetArcaWs.Contracts.WsfexV1;
using Wsmtxca = NetArcaWs.Contracts.Wsmtxca;
using Wscdc = NetArcaWs.Contracts.Wscdc;
using WsfeCred = NetArcaWs.Contracts.WsfeCred;

namespace NetArcaWs.IntegrationTests;

public sealed class AuthenticatedLookupValidationTests
{
    [Fact]
    public async Task Wsfe_probe_checks_all_catalogs_pos_without_description_and_maximum_request_count()
    {
        IWsfev1Service client = DispatchProxy.Create<IWsfev1Service, WsfeCatalogProxy>();
        var fake = (WsfeCatalogProxy)(object)client;
        fake.IncludePointOfSale = true;
        IReadOnlyList<NetArcaWs.Contracts.WsfeV1.PtoVenta> points = await AuthenticatedLookupValidation.ValidateWsfeAsync(
            client, Tenant(ArcaEnvironment.Homologation), CancellationToken.None);

        Assert.Single(points);
        Assert.Equal(4321, points[0].Nro);
        Assert.Equal(8, fake.CallCount);
    }

    [Fact]
    public async Task Wsfe_probe_accepts_no_points_of_sale_and_refuses_production_before_calls()
    {
        IWsfev1Service client = DispatchProxy.Create<IWsfev1Service, WsfeCatalogProxy>();
        var fake = (WsfeCatalogProxy)(object)client;
        Assert.Empty(await AuthenticatedLookupValidation.ValidateWsfeAsync(client, Tenant(ArcaEnvironment.Homologation), CancellationToken.None));
        Assert.Equal(8, fake.CallCount);

        IWsfev1Service productionClient = DispatchProxy.Create<IWsfev1Service, WsfeCatalogProxy>();
        var productionFake = (WsfeCatalogProxy)(object)productionClient;
        await Assert.ThrowsAnyAsync<XunitException>(() => AuthenticatedLookupValidation.ValidateWsfeAsync(
            productionClient, Tenant(ArcaEnvironment.Production), CancellationToken.None));
        Assert.Equal(0, productionFake.CallCount);
    }

    [Fact]
    public async Task Wsfe_probe_rejects_catalog_errors_and_missing_maximum_response()
    {
        IWsfev1Service errorClient = DispatchProxy.Create<IWsfev1Service, WsfeCatalogProxy>();
        var errorFake = (WsfeCatalogProxy)(object)errorClient;
        errorFake.CurrencyError = true;
        await Assert.ThrowsAnyAsync<XunitException>(() => AuthenticatedLookupValidation.ValidateWsfeAsync(
            errorClient, Tenant(ArcaEnvironment.Homologation), CancellationToken.None));
        Assert.Equal(1, errorFake.CallCount);

        IWsfev1Service missingClient = DispatchProxy.Create<IWsfev1Service, WsfeCatalogProxy>();
        var missingFake = (WsfeCatalogProxy)(object)missingClient;
        missingFake.MissingMaximumResponse = true;
        await Assert.ThrowsAnyAsync<XunitException>(() => AuthenticatedLookupValidation.ValidateWsfeAsync(
            missingClient, Tenant(ArcaEnvironment.Homologation), CancellationToken.None));
        Assert.Equal(8, missingFake.CallCount);
    }

    [Fact]
    public void Wsfex_currency_validates_entries_and_zero_error_and_rejects_missing_result_or_error()
    {
        var valid = new Wsfex.FexResponseMon { FexErr = new Wsfex.ClsFexErr { ErrCode = 0 } };
        valid.FexResultGet.Add(new Wsfex.ClsFexResponseMon { MonId = "PES", MonDs = "Peso" });
        AuthenticatedLookupValidation.ValidateResponse(new Wsfex.FexGetParamMonResponse
        {
            FexGetParamMonResult = valid
        });

        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new Wsfex.FexGetParamMonResponse()));
        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new Wsfex.FexGetParamMonResponse
        {
            FexGetParamMonResult = new Wsfex.FexResponseMon { FexErr = new Wsfex.ClsFexErr { ErrCode = 1 } }
        }));
    }

    [Fact]
    public void Wsmtxca_currency_validates_nonempty_catalog_entries()
    {
        var response = new Wsmtxca.ConsultarMonedasResponseType();
        response.ArrayMonedas.Add(new Wsmtxca.CodigoDescripcionStringType { Codigo = "PES", Descripcion = "Peso" });
        AuthenticatedLookupValidation.ValidateResponse(response);

        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new Wsmtxca.ConsultarMonedasResponseType()));
        var malformed = new Wsmtxca.ConsultarMonedasResponseType();
        malformed.ArrayMonedas.Add(new Wsmtxca.CodigoDescripcionStringType { Codigo = "PES" });
        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(malformed));
    }

    [Fact]
    public void Wscdc_modalities_validate_wrapper_entries_and_errors()
    {
        var goodResult = new Wscdc.FacModTipoResponse();
        goodResult.ResultGet.Add(new Wscdc.FacModTipo { Cod = "1", Desc = "Modalidad" });
        AuthenticatedLookupValidation.ValidateResponse(new Wscdc.ComprobantesModalidadConsultarResponse
        {
            ComprobantesModalidadConsultarResult = goodResult
        });

        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new Wscdc.ComprobantesModalidadConsultarResponse()));
        var errorResult = new Wscdc.FacModTipoResponse();
        errorResult.Errors.Add(new Wscdc.Err { Code = 1, Msg = "failure" });
        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new Wscdc.ComprobantesModalidadConsultarResponse
        {
            ComprobantesModalidadConsultarResult = errorResult
        }));
    }

    [Fact]
    public void Wsfecred_retenciones_validate_wrapper_format_errors_and_entries()
    {
        AuthenticatedLookupValidation.ValidateResponse(new WsfeCred.ConsultarTiposRetencionesResponseType
        {
            ConsultarTiposRetencionesReturn = new WsfeCred.ConsultarTiposRetencionesReturnType()
        });

        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new WsfeCred.ConsultarTiposRetencionesResponseType()));
        var errorResult = new WsfeCred.ConsultarTiposRetencionesReturnType();
        errorResult.ArrayErroresFormato.Add(new WsfeCred.CodigoDescripcionStringType { Codigo = "1", Descripcion = "error" });
        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new WsfeCred.ConsultarTiposRetencionesResponseType
        {
            ConsultarTiposRetencionesReturn = errorResult
        }));
        var malformedEntry = new WsfeCred.ConsultarTiposRetencionesReturnType();
        malformedEntry.ArrayTiposRetenciones.Add(new WsfeCred.TipoRetencionType { CodigoJurisdiccion = -1, DescripcionJurisdiccion = "Jurisdiction" });
        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new WsfeCred.ConsultarTiposRetencionesResponseType
        {
            ConsultarTiposRetencionesReturn = malformedEntry
        }));
    }

    [Fact]
    public void Padron_a4_requires_real_person_data()
    {
        AuthenticatedLookupValidation.ValidateResponse(new NetArcaWs.Contracts.PadronA4.GetPersonaResponse
        {
            PersonaReturn = new NetArcaWs.Contracts.PadronA4.PersonaReturn
            {
                Persona = new NetArcaWs.Contracts.PadronA4.Persona { EstadoClave = "ACTIVO" }
            }
        });
        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new NetArcaWs.Contracts.PadronA4.GetPersonaResponse()));
        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new NetArcaWs.Contracts.PadronA4.GetPersonaResponse
        {
            PersonaReturn = new NetArcaWs.Contracts.PadronA4.PersonaReturn { Metadata = new NetArcaWs.Contracts.PadronA4.Metadata { Servidor = "server" } }
        }));
    }

    [Fact]
    public void Padron_a5_uses_datos_generales_and_rejects_business_errors()
    {
        AuthenticatedLookupValidation.ValidateResponse(new NetArcaWs.Contracts.PadronA5.GetPersonaResponse
        {
            PersonaReturn = new NetArcaWs.Contracts.PadronA5.PersonaReturn
            {
                DatosGenerales = new NetArcaWs.Contracts.PadronA5.DatosGenerales { EstadoClave = "ACTIVO" }
            }
        });
        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new NetArcaWs.Contracts.PadronA5.GetPersonaResponse()));

        var error = new NetArcaWs.Contracts.PadronA5.ErrorConstancia();
        error.Error.Add("failure");
        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new NetArcaWs.Contracts.PadronA5.GetPersonaResponse
        {
            PersonaReturn = new NetArcaWs.Contracts.PadronA5.PersonaReturn
            {
                DatosGenerales = new NetArcaWs.Contracts.PadronA5.DatosGenerales { EstadoClave = "ACTIVO" },
                ErrorConstancia = error
            }
        }));
    }

    [Fact]
    public void Padron_a10_and_a13_require_business_person_fields()
    {
        AuthenticatedLookupValidation.ValidateResponse(new NetArcaWs.Contracts.PadronA10.GetPersonaResponse
        {
            PersonaReturn = new NetArcaWs.Contracts.PadronA10.PersonaReturn
            {
                Persona = new NetArcaWs.Contracts.PadronA10.Persona { EstadoClave = "ACTIVO" }
            }
        });
        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new NetArcaWs.Contracts.PadronA10.GetPersonaResponse()));

        AuthenticatedLookupValidation.ValidateResponse(new NetArcaWs.Contracts.PadronA13.GetPersonaResponse
        {
            PersonaReturn = new NetArcaWs.Contracts.PadronA13.PersonaReturn
            {
                Persona = new NetArcaWs.Contracts.PadronA13.Persona { EstadoClave = "ACTIVO" }
            }
        });
        Assert.ThrowsAny<XunitException>(() => AuthenticatedLookupValidation.ValidateResponse(new NetArcaWs.Contracts.PadronA13.GetPersonaResponse()));
    }

    private static ArcaTenantContext Tenant(ArcaEnvironment environment) => new(
        "offline-authenticated-lookup-test", 20123456786, environment,
        WsaaCertificateContent.FromPem("synthetic certificate content", "synthetic private key content"));

    public class WsfeCatalogProxy : DispatchProxy
    {
        public int CallCount { get; private set; }
        public bool IncludePointOfSale { get; set; }
        public bool CurrencyError { get; set; }
        public bool MissingMaximumResponse { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.NotNull(targetMethod);
            CallCount++;
            return targetMethod!.Name switch
            {
                nameof(IWsfev1Service.FEParamGetTiposMonedasAsync) => Task.FromResult(new NetArcaWs.Contracts.WsfeV1.FeParamGetTiposMonedasResponse
                { FeParamGetTiposMonedasResult = Monedas(CurrencyError) }),
                nameof(IWsfev1Service.FEParamGetTiposCbteAsync) => Task.FromResult(new NetArcaWs.Contracts.WsfeV1.FeParamGetTiposCbteResponse
                { FeParamGetTiposCbteResult = VoucherTypes() }),
                nameof(IWsfev1Service.FEParamGetTiposDocAsync) => Task.FromResult(new NetArcaWs.Contracts.WsfeV1.FeParamGetTiposDocResponse
                { FeParamGetTiposDocResult = DocumentTypes() }),
                nameof(IWsfev1Service.FEParamGetTiposConceptoAsync) => Task.FromResult(new NetArcaWs.Contracts.WsfeV1.FeParamGetTiposConceptoResponse
                { FeParamGetTiposConceptoResult = Concepts() }),
                nameof(IWsfev1Service.FEParamGetTiposIvaAsync) => Task.FromResult(new NetArcaWs.Contracts.WsfeV1.FeParamGetTiposIvaResponse
                { FeParamGetTiposIvaResult = VatTypes() }),
                nameof(IWsfev1Service.FEParamGetCondicionIvaReceptorAsync) => Task.FromResult(new NetArcaWs.Contracts.WsfeV1.FeParamGetCondicionIvaReceptorResponse
                { FeParamGetCondicionIvaReceptorResult = VatReceiverConditions() }),
                nameof(IWsfev1Service.FEParamGetPtosVentaAsync) => Task.FromResult(Points()),
                nameof(IWsfev1Service.FECompTotXRequestAsync) => Task.FromResult(new NetArcaWs.Contracts.WsfeV1.FeCompTotXRequestResponse
                { FeCompTotXRequestResult = MissingMaximumResponse ? null : new FeRegXReqResponse { RegXReq = 1000 } }),
                _ => throw new NotSupportedException("Unexpected offline WSFE lookup method.")
            };
        }

        private static MonedaResponse Monedas(bool withError)
        {
            var value = new MonedaResponse();
            value.ResultGet.Add(new Moneda { Id = "PES", Desc = "Peso" });
            if (withError) value.Errors.Add(new Err { Code = 1, Msg = "synthetic error" });
            return value;
        }
        private static CbteTipoResponse VoucherTypes() { var value = new CbteTipoResponse(); value.ResultGet.Add(new CbteTipo { Id = 1, Desc = "Factura" }); return value; }
        private static DocTipoResponse DocumentTypes() { var value = new DocTipoResponse(); value.ResultGet.Add(new DocTipo { Id = 80, Desc = "CUIT" }); return value; }
        private static ConceptoTipoResponse Concepts() { var value = new ConceptoTipoResponse(); value.ResultGet.Add(new ConceptoTipo { Id = 1, Desc = "Productos" }); return value; }
        private static IvaTipoResponse VatTypes() { var value = new IvaTipoResponse(); value.ResultGet.Add(new IvaTipo { Id = "5", Desc = "IVA 21%" }); return value; }
        private static CondicionIvaReceptorResponse VatReceiverConditions() { var value = new CondicionIvaReceptorResponse(); value.ResultGet.Add(new CondicionIvaReceptor { Id = 5, Desc = "Consumidor final" }); return value; }

        private NetArcaWs.Contracts.WsfeV1.FeParamGetPtosVentaResponse Points()
        {
            var result = new FePtoVentaResponse();
            if (IncludePointOfSale)
                result.ResultGet.Add(new PtoVenta { Nro = 4321, EmisionTipo = "CAE", Bloqueado = "N" });
            return new NetArcaWs.Contracts.WsfeV1.FeParamGetPtosVentaResponse { FeParamGetPtosVentaResult = result };
        }
    }
}
