using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.HealthChecks;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class FiscalHomologationEmptySeriesTests
{
    [Fact]
    public void Only_the_explicit_homologation_matrix_can_select_fixed_POS_for_a_recognized_empty_catalogue()
    {
        Assert.Equal(1, FiscalHomologationMatrixProfile.SelectPointOfSale("wsfe", [], true, ArcaEnvironment.Homologation, true).PointOfSale);
        Assert.Equal(1, FiscalHomologationMatrixProfile.SelectPointOfSale("wsfex", [], true, ArcaEnvironment.Homologation, true).PointOfSale);
        Assert.Equal(2, FiscalHomologationMatrixProfile.SelectPointOfSale("wsmtxca", [], true, ArcaEnvironment.Homologation, true).PointOfSale);
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationMatrixProfile.SelectPointOfSale(
            "wsfe", [], true, ArcaEnvironment.Production, true));
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationMatrixProfile.SelectPointOfSale(
            "wsfe", [], false, ArcaEnvironment.Homologation, true));
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationMatrixProfile.SelectPointOfSale(
            "wsfe", [], true, ArcaEnvironment.Homologation, false));
    }

    [Fact]
    public void Active_catalogue_wins_and_frozen_or_reserved_identities_are_never_replaced()
    {
        var active = FiscalHomologationMatrixProfile.SelectPointOfSale("wsfe", [4, 2], false,
            ArcaEnvironment.Homologation, true);
        Assert.Equal(2, active.PointOfSale);
        Assert.False(active.UsesSyntheticFixture);
        Assert.Equal(4, FiscalHomologationMatrixProfile.SelectPointOfSale("wsfe", [4, 2], false,
            ArcaEnvironment.Homologation, true, frozenPointOfSale: 4).PointOfSale);
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationMatrixProfile.SelectPointOfSale(
            "wsmtxca", [2], false, ArcaEnvironment.Homologation, true, reservedPointOfSale: 2));
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationMatrixProfile.SelectPointOfSale(
            "wsfe", [], true, ArcaEnvironment.Homologation, true, frozenPointOfSale: 99));
    }

    [Fact]
    public void WSMTXCA_1502_means_empty_series_only_with_no_number_and_no_other_response_fields()
    {
        var response = new ConsultarUltimoComprobanteAutorizadoResponseType();
        response.ArrayErrores.Add(new CodigoDescripcionType { Codigo = 1502, Descripcion = "No existen comprobantes" });
        Assert.Equal(0, FiscalHomologationWsmtxcaChannel.ReadLastAuthorizedNumber(response, ArcaEnvironment.Homologation));
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsmtxcaChannel.ReadLastAuthorizedNumber(response, ArcaEnvironment.Production));

        response.NumeroComprobante = 8;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsmtxcaChannel.ReadLastAuthorizedNumber(response, ArcaEnvironment.Homologation));
        response.NumeroComprobante = null;
        response.Evento = new CodigoDescripcionType { Codigo = 1, Descripcion = "unexpected event" };
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsmtxcaChannel.ReadLastAuthorizedNumber(response, ArcaEnvironment.Homologation));
        response.Evento = null;
        response.ArrayErrores.Clear();
        response.ArrayErrores.Add(new CodigoDescripcionType { Codigo = 1501, Descripcion = "Punto de venta inválido" });
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsmtxcaChannel.ReadLastAuthorizedNumber(response, ArcaEnvironment.Homologation));
        response.ArrayErrores.Clear();
        response.ArrayErrores.Add(new CodigoDescripcionType { Codigo = 1502, Descripcion = "No existen comprobantes" });
        response.ArrayErrores.Add(new CodigoDescripcionType { Codigo = 1502, Descripcion = "No existen comprobantes" });
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsmtxcaChannel.ReadLastAuthorizedNumber(response, ArcaEnvironment.Homologation));
    }

    [Fact]
    public void WSFE_catalogue_fixture_requires_only_the_known_homologation_602_with_no_points()
    {
        var response = new FePtoVentaResponse();
        response.Errors.Add(new Err { Code = 602, Msg = "No hay puntos informados" });
        Assert.True(FiscalHomologationWsfeChannel.IsKnownEmptyQaPointOfSaleCatalogue(response, ArcaEnvironment.Homologation));
        Assert.False(FiscalHomologationWsfeChannel.IsKnownEmptyQaPointOfSaleCatalogue(response, ArcaEnvironment.Production));

        response.Errors[0].Code = 603;
        Assert.False(FiscalHomologationWsfeChannel.IsKnownEmptyQaPointOfSaleCatalogue(response, ArcaEnvironment.Homologation));
        response.Errors[0].Code = 602;
        response.ResultGet.Add(new PtoVenta { Nro = 1, EmisionTipo = "CAE", Bloqueado = "N" });
        Assert.False(FiscalHomologationWsfeChannel.IsKnownEmptyQaPointOfSaleCatalogue(response, ArcaEnvironment.Homologation));
        Assert.False(FiscalHomologationWsfeChannel.IsFixedQaPointOfSaleAllowed(response, ArcaEnvironment.Homologation, 1, enabled: true));
        Assert.False(FiscalHomologationWsfeChannel.IsFixedQaPointOfSaleAllowed(new FePtoVentaResponse(), ArcaEnvironment.Homologation, 1, enabled: true));
    }

    [Fact]
    public void Fixed_fallback_never_masks_nonempty_or_invalid_WSFEX_and_WSMTXCA_catalogues()
    {
        var fex = new FexResponsePtoVenta { FexErr = new ClsFexErr { ErrCode = 0 } };
        Assert.True(FiscalHomologationWsfexChannel.IsFixedQaPointOfSaleAllowed(fex, ArcaEnvironment.Homologation, 1, enabled: true));
        Assert.False(FiscalHomologationWsfexChannel.IsFixedQaPointOfSaleAllowed(fex, ArcaEnvironment.Production, 1, enabled: true));
        Assert.False(FiscalHomologationWsfexChannel.IsFixedQaPointOfSaleAllowed(fex, ArcaEnvironment.Homologation, 2, enabled: true));
        Assert.False(FiscalHomologationWsfexChannel.IsFixedQaPointOfSaleAllowed(fex, ArcaEnvironment.Homologation, 1, enabled: false));
        fex.FexResultGet.Add(new ClsFexResponsePtoVenta { PveNro = 1, PveBloqueado = "S" });
        Assert.False(FiscalHomologationWsfexChannel.IsFixedQaPointOfSaleAllowed(fex, ArcaEnvironment.Homologation, 1, enabled: true));

        var mtxca = new ConsultarPuntosVentaCaeResponse();
        Assert.True(FiscalHomologationWsmtxcaChannel.IsFixedQaPointOfSaleAllowed(mtxca, ArcaEnvironment.Homologation, 2, enabled: true));
        Assert.False(FiscalHomologationWsmtxcaChannel.IsFixedQaPointOfSaleAllowed(mtxca, ArcaEnvironment.Production, 2, enabled: true));
        Assert.False(FiscalHomologationWsmtxcaChannel.IsFixedQaPointOfSaleAllowed(mtxca, ArcaEnvironment.Homologation, 1, enabled: true));
        mtxca.ArrayPuntosVenta.Add(new PuntoVentaType { NumeroPuntoVenta = 2, Bloqueado = SiNoSimpleType.S });
        Assert.False(FiscalHomologationWsmtxcaChannel.IsFixedQaPointOfSaleAllowed(mtxca, ArcaEnvironment.Homologation, 2, enabled: true));
        Assert.False(FiscalHomologationWsmtxcaChannel.IsActiveCaePointOfSale(mtxca, 2));
    }
}
