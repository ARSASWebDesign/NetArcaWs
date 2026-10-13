using AwesomeAssertions;
using System.Security.Cryptography.X509Certificates;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.Documents;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using NetArcaWs.Tests.TestSupport;
using NetArcaWs.Tests.Invoicing;
using NetArcaWs.Transport;
using Xunit;

namespace NetArcaWs.Tests.Documents;

public sealed class ArcaWsComprobantesTests
{
    private const long Cuit = 30123456789;
    private const string Cae = "71234567890123";

    [Fact]
    public async Task Wsfe_authorized_result_uses_frozen_snapshot_and_keeps_response_without_credentials()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(_ => WsfeResponse("A", Cae));
        ArcaWsComprobantes facade = CreateFacade(database, transport);
        FecaeRequest request = WsfeRequest();

        ComprobanteGenerado result = await facade.GenerarComprobanteAsync(Tenant(), "wsfe", request, Presentation(), cancellationToken);
        ComprobanteGenerado replay = await facade.GenerarComprobanteAsync(Tenant(), "wsfe", request, Presentation(), cancellationToken);

        result.Identidad.Should().Be(new IdentidadComprobante("wsfe", "Homologation", Cuit, 1, 1, 12));
        result.Estado.Should().Be(InvoiceState.Authorized.ToString());
        result.Resultado!.CodigoAutorizacion.Should().Be(Cae);
        result.SnapshotRequest.Should().NotContain("Token");
        result.SnapshotResponse.Should().Contain("FECAESolicitarResult");
        result.SnapshotResponse.Should().NotContain("SERVER-ONLY-TICKET");
        result.Receptor.Identificacion.Should().Be(20304050607);
        result.Total.Should().Be(123m);
        result.Fecha.Should().Be(new DateOnly(2026, 10, 6));
        result.Resultado.TipoAutorizacion.Should().Be("E");
        result.Resultado.Vencimiento.Should().Be(new DateOnly(2026, 10, 16));
        result.Impuestos.Should().Contain(x => x.Nombre == "IVA" && x.Importe == 21m && x.BaseImponible == 100m);
        result.Impuestos.Should().Contain(x => x.Codigo == "3" && x.Alicuota == 2m);
        replay.SnapshotRequest.Should().Be(result.SnapshotRequest);
        transport.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Rejected_result_preserves_state_and_never_fabricates_authorization()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(_ => WsfeResponse("R", null));
        ArcaWsComprobantes facade = CreateFacade(database, transport);

        ComprobanteGenerado result = await facade.GenerarComprobanteAsync(Tenant(), "rejected", WsfeRequest(), Presentation(), cancellationToken);

        result.Estado.Should().Be(InvoiceState.Rejected.ToString());
        result.Resultado!.CodigoAutorizacion.Should().BeNull();
        result.Resultado.Errores.Should().ContainSingle().Which.Should().Be("1001: Rechazo sintético");
    }

    [Fact]
    public async Task Wsfex_and_wsmtxca_preserve_their_fiscal_line_details()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(call => call.RequestType == typeof(FexAuthorize)
            ? WsfexResponse()
            : WsmtxcaResponse());
        ArcaWsComprobantes facade = CreateFacade(database, transport);

        ComprobanteGenerado fex = await facade.GenerarComprobanteAsync(Tenant(), "fex", WsfexRequest(), Presentation(new LineaComprobante("Descripción adicional", 20, "DOL")), cancellationToken);
        ComprobanteGenerado mtx = await facade.GenerarComprobanteAsync(Tenant(), "mtx", WsmtxcaRequest(), Presentation(new LineaComprobante("Descripción adicional", 121, "PES")), cancellationToken);

        fex.Detalles.Should().ContainSingle().Which.Importe.Should().Be(20m);
        fex.Detalles.Single().DescripcionPresentacion.Should().Be("Descripción adicional");
        fex.Resultado!.Vencimiento.Should().Be(new DateOnly(2026, 10, 16));
        fex.SnapshotRequest.Should().Contain("SKU-001");
        mtx.SnapshotRequest.Should().Contain("numeroComprobante");
        mtx.Resultado!.Vencimiento.Should().Be(new DateOnly(2026, 10, 16));
        mtx.Detalles.Should().ContainSingle().Which.Cantidad.Should().Be(2m);
        mtx.Detalles.Single().Descripcion.Should().Be("Item MTXCA");
        mtx.Impuestos.Should().ContainSingle(x => x.Codigo == "4" && x.Importe == 10m && x.Tipo == TipoImpuestoComprobante.Iva);
        mtx.Impuestos.Should().ContainSingle(x => x.Codigo == "5" && x.Importe == 11m && x.Tipo == TipoImpuestoComprobante.Iva);
        mtx.Total.Should().Be(121m);
        mtx.Neto.Should().Be(100m);
        mtx.Iva.Should().Be(21m);
    }

    [Fact]
    public async Task Wsmtxca_total_correlates_subtotal_vat_and_other_taxes_before_write()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport();
        ArcaWsComprobantes facade = CreateFacade(database, transport);
        ComprobanteType request = WsmtxcaRequest();
        request.ImporteTotal = 120;

        Func<Task> act = () => facade.GenerarComprobanteAsync(Tenant(), "incorrect-wsmtxca-total", request,
            Presentation(new LineaComprobante("Descripción adicional", 121, "PES")), cancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
        transport.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Wsmtxca_unit_99_discount_preserves_negative_fiscal_amounts()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(_ => WsmtxcaResponse());
        ArcaWsComprobantes facade = CreateFacade(database, transport);
        ComprobanteType request = WsmtxcaRequest();
        request.ImporteSubtotal = 90;
        request.ImporteGravado = 90;
        request.ImporteTotal = 108.9m;
        request.ArrayItems[0].ImporteItem = 121;
        request.ArrayItems[0].ImporteIva = 21;
        request.ArrayItems.Add(new ItemType { Codigo = "DESCUENTO", Descripcion = "Descuento", Cantidad = 1,
            CodigoUnidadMedida = 99, PrecioUnitario = 0, CodigoCondicionIva = 1, ImporteIva = -2.1m, ImporteItem = -12.1m });
        request.ArraySubtotalesIva[1].Importe = 8.9m;

        ComprobanteGenerado result = await facade.GenerarComprobanteAsync(Tenant(), "discount-wsmtxca", request, Presentation(), cancellationToken);

        result.Total.Should().Be(108.9m);
        result.Detalles.Select(line => line.Importe).Should().Equal(121m, -12.1m);
        result.SnapshotRequest.Should().Contain("-12.1");
        ComprobanteValidator.Validate(result);
    }

    [Fact]
    public async Task Matched_supplemental_tax_enriches_the_fiscal_entry_without_duplication()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(_ => WsmtxcaResponse());
        ArcaWsComprobantes facade = CreateFacade(database, transport);
        ComprobanteType request = WsmtxcaRequest();
        request.ImporteTotal = 123;
        request.ImporteOtrosTributos = 2;
        request.ArrayOtrosTributos.Add(new OtroTributoType { Codigo = 20, Descripcion = "Impuesto provincial", BaseImponible = 100, Importe = 2 });
        var supplemental = new ImpuestoComprobante("Clasificación mostrada", 2, "PES", "20", 100, Clasificacion: "provincial");
        var ivaSupplemental = new ImpuestoComprobante("IVA discriminado", 11, "PES", "5", Tipo: TipoImpuestoComprobante.Iva);

        ComprobanteGenerado result = await facade.GenerarComprobanteAsync(Tenant(), "matched-tax", request,
            Presentation(taxes: [supplemental, ivaSupplemental]), cancellationToken);

        result.Impuestos.Should().ContainSingle(x => x.Codigo == "20" && x.Importe == 2m)
            .Which.Should().Match<ImpuestoComprobante>(x => x.Nombre == "Clasificación mostrada" && x.Clasificacion == "provincial" && x.Tipo == TipoImpuestoComprobante.OtroTributo);
        result.Impuestos.Should().ContainSingle(x => x.Codigo == "5" && x.Nombre == "IVA discriminado" && x.Tipo == TipoImpuestoComprobante.Iva);
    }

    [Fact]
    public async Task Unmatched_supplemental_tax_is_rejected_before_fiscal_write()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport();
        ArcaWsComprobantes facade = CreateFacade(database, transport);
        var supplemental = new ImpuestoComprobante("Tributo inventado", 2, "PES", "99");

        Func<Task> act = () => facade.GenerarComprobanteAsync(Tenant(), "unmatched-tax", WsmtxcaRequest(),
            Presentation(taxes: [supplemental]), cancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
        transport.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Wsfex_rejects_supplemental_tax_without_fiscal_tax_source()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport();
        ArcaWsComprobantes facade = CreateFacade(database, transport);
        var supplemental = new ImpuestoComprobante("Impuesto", 1, "DOL", "1");

        Func<Task> act = () => facade.GenerarComprobanteAsync(Tenant(), "fex-tax", WsfexRequest(),
            Presentation(taxes: [supplemental]), cancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
        transport.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Supplemental_tax_with_opposite_tax_type_is_rejected_before_fiscal_write()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport();
        ArcaWsComprobantes facade = CreateFacade(database, transport);
        var tax = new ImpuestoComprobante("No es IVA", 11, "PES", "5", Tipo: TipoImpuestoComprobante.OtroTributo);

        Func<Task> act = () => facade.GenerarComprobanteAsync(Tenant(), "wrong-tax-type", WsmtxcaRequest(),
            Presentation(taxes: [tax]), cancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
        transport.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Invalid_supplemental_receptor_is_rejected_before_any_fiscal_write()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(_ => WsfeResponse("A", Cae));
        ArcaWsComprobantes facade = CreateFacade(database, transport);
        DatosPresentacion presentation = new(new DatosReceptor(111, "Otra persona", null, null, 80));

        Func<Task> act = () => facade.GenerarComprobanteAsync(Tenant(), "mismatch", WsfeRequest(), presentation, cancellationToken);

        await act.Should().ThrowAsync<ArgumentException>();
        transport.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Unknown_result_preserves_snapshot_errors_and_does_not_create_authorization()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(_ => { var response = WsfeResponse("A", Cae); response.FecaeSolicitarResult.FeCabResp.Cuit = Cuit + 1; return response; });
        ArcaWsComprobantes facade = CreateFacade(database, transport);

        ComprobanteGenerado result = await facade.GenerarComprobanteAsync(Tenant(), "unknown", WsfeRequest(), Presentation(), cancellationToken);

        result.Estado.Should().Be(InvoiceState.Unknown.ToString());
        result.Resultado!.CodigoAutorizacion.Should().BeNull();
        result.Resultado.Errores.Should().NotBeEmpty();
        result.SnapshotRequest.Should().Contain("FECAEDetRequest");
    }

    [Fact]
    public async Task Conflict_after_query_reconciliation_is_returned_without_resubmitting()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(call => call.RequestType == typeof(FecaeSolicitar)
            ? WsfeResponse("A", null)
            : MismatchedWsfeQueryResponse());
        SafeInvoiceService invoices = CreateInvoiceService(database, transport);
        var facade = new ArcaWsComprobantes(invoices);
        ArcaTenantContext tenant = Tenant();
        FecaeRequest request = WsfeRequest();

        ComprobanteGenerado unknown = await facade.GenerarComprobanteAsync(tenant, "conflict", request, Presentation(), cancellationToken);
        InvoiceOperation reconciled = await invoices.ReconcileAsync(tenant, "conflict", cancellationToken);
        ComprobanteGenerado conflict = await facade.GenerarComprobanteAsync(tenant, "conflict", request, Presentation(), cancellationToken);

        unknown.Estado.Should().Be(InvoiceState.Unknown.ToString());
        reconciled.State.Should().Be(InvoiceState.Conflict);
        conflict.Estado.Should().Be(InvoiceState.Conflict.ToString());
        conflict.Resultado!.CodigoAutorizacion.Should().BeNull();
        conflict.SnapshotResponse.Should().Contain("FECompConsultarResult");
        transport.CallCount.Should().Be(2, "one authorization and one reconciliation query only");
    }

    [Fact]
    public async Task Caller_request_mutation_after_call_does_not_change_the_frozen_soap_payload()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new DeferredSoapTransport();
        ArcaWsComprobantes facade = CreateFacade(database, transport);
        FecaeRequest request = WsfeRequest();

        Task<ComprobanteGenerado> generation = facade.GenerarComprobanteAsync(Tenant(), "mutated", request, Presentation(), cancellationToken);
        request.FeCabReq.PtoVta = 99;
        request.FeDetReq[0].CbteDesde = 44;
        request.FeDetReq[0].ImpTotal = 999;
        var sent = transport.Call!.Request.Should().BeOfType<FecaeSolicitar>().Subject;
        sent.FeCaeReq.FeCabReq.PtoVta.Should().Be(1);
        sent.FeCaeReq.FeDetReq[0].CbteDesde.Should().Be(12);
        sent.FeCaeReq.FeDetReq[0].ImpTotal.Should().Be(123);
        transport.Complete(WsfeResponse("A", Cae));

        ComprobanteGenerado result = await generation;
        result.Total.Should().Be(123m);
        result.Identidad!.Numero.Should().Be(12);
        request.FeCabReq.PtoVta.Should().Be(99, "the façade must not mutate the caller's DTO");
    }

    [Fact]
    public async Task Presentation_collection_mutation_after_call_does_not_change_the_frozen_supplement()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new DeferredSoapTransport();
        ArcaWsComprobantes facade = CreateFacade(database, transport);
        var sourceDetails = new List<LineaComprobante> { new("Texto original", 20, "DOL") };
        var presentation = new DatosPresentacion(new DatosReceptor(20304050607, "Cliente de prueba", "Domicilio", null), sourceDetails);

        Task<ComprobanteGenerado> generation = facade.GenerarComprobanteAsync(Tenant(), "mutated-presentation", WsfexRequest(), presentation, cancellationToken);
        sourceDetails[0] = new LineaComprobante("Texto mutado", 19, "PES");
        transport.Complete(WsfexResponse());

        ComprobanteGenerado result = await generation;
        result.Detalles.Single().DescripcionPresentacion.Should().Be("Texto original");
        result.Detalles.Single().Importe.Should().Be(20m);
    }

    [Fact]
    public async Task Invalid_issue_date_and_unsupported_type_fail_before_fiscal_write()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport();
        ArcaWsComprobantes facade = CreateFacade(database, transport);
        FecaeRequest badDate = WsfeRequest();
        badDate.FeDetReq[0].CbteFch = "not-a-date";
        FecaeRequest unsupported = WsfeRequest();
        unsupported.FeCabReq.CbteTipo = 99;

        Func<Task> date = () => facade.GenerarComprobanteAsync(Tenant(), "date", badDate, Presentation(), cancellationToken);
        Func<Task> type = () => facade.GenerarComprobanteAsync(Tenant(), "type", unsupported, Presentation(), cancellationToken);

        await date.Should().ThrowAsync<InvalidOperationException>();
        await type.Should().ThrowAsync<NotSupportedException>();
        transport.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Non_finite_amount_fails_before_fiscal_write()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport();
        ArcaWsComprobantes facade = CreateFacade(database, transport);
        FecaeRequest request = WsfeRequest();
        request.FeDetReq[0].ImpTotal = double.NaN;

        Func<Task> act = () => facade.GenerarComprobanteAsync(Tenant(), "nan", request, Presentation(), cancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
        transport.CallCount.Should().Be(0);
    }

    private static ArcaWsComprobantes CreateFacade(TemporaryDatabase database, ISoapTransport transport) => new(CreateInvoiceService(database, transport));

    private static SafeInvoiceService CreateInvoiceService(TemporaryDatabase database, ISoapTransport transport)
    {
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "document-facade"));
        var coordinator = new InvoiceCoordinator(journal);
        var tickets = new RecordingArcaTicketProvider(TicketTestData.Create("SERVER-ONLY-TICKET-TOKEN", "SERVER-ONLY-TICKET-SIGN"));
        return new SafeInvoiceService(coordinator, new Wsfev1Service(transport, tickets),
            new Wsfexv1Service(transport, tickets), new Wsmtxcav1Service(transport, tickets));
    }

    private static ArcaTenantContext Tenant()
    {
        using var cert = TestCertificates.Create("facade-test");
        using var key = cert.GetRSAPrivateKey()!;
        var content = WsaaCertificateContent.FromPem(cert.ExportCertificatePem(), key.ExportPkcs8PrivateKeyPem());
        return new ArcaTenantContext("tenant-test", Cuit, ArcaEnvironment.Homologation, content);
    }

    private static DatosPresentacion Presentation(LineaComprobante? line = null, IReadOnlyList<ImpuestoComprobante>? taxes = null) => new(
        new DatosReceptor(20304050607, "Cliente de prueba", null, "IVA Responsable Inscripto", 80),
        line is null ? [] : [line], taxes);

    private static FecaeRequest WsfeRequest()
    {
        var request = new FecaeRequest { FeCabReq = new FecaeCabRequest { CantReg = 1, PtoVta = 1, CbteTipo = 1 } };
        request.FeDetReq.Add(new FecaeDetRequest { Concepto = 1, DocTipo = 80, DocNro = 20304050607,
            CbteDesde = 12, CbteHasta = 12, CbteFch = "20261006", ImpTotal = 123, ImpTotConc = 0,
            ImpNeto = 100, ImpOpEx = 0, ImpIva = 21, ImpTrib = 2, MonId = "PES", MonCotiz = 1 });
        request.FeDetReq[0].Iva.Add(new AlicIva { Id = 5, BaseImp = 100, Importe = 21 });
        request.FeDetReq[0].Tributos.Add(new Tributo { Id = 3, Desc = "Tributo sintético", BaseImp = 100, Alic = 2, Importe = 2 });
        return request;
    }

    private static ClsFexRequest WsfexRequest() => new() { Id = 4567, FechaCbte = "20261006", CbteTipo = 19,
        PuntoVta = 2, CbteNro = 12, TipoExpo = 1, DstCmp = 200, Cliente = "Cliente de prueba",
        CuitPaisCliente = 20304050607, DomicilioCliente = "Domicilio", MonedaId = "DOL", MonedaCtz = 1, ImpTotal = 20,
        Items = { new Item { ProCodigo = "SKU-001", ProDs = "Servicio", ProQty = 2, ProUmed = 7,
            ProPrecioUni = 10, ProBonificacion = 0, ProTotalItem = 20 } } };

    private static ComprobanteType WsmtxcaRequest()
    {
        var request = new ComprobanteType { CodigoTipoComprobante = 1, NumeroPuntoVenta = 1,
            NumeroComprobante = 12, FechaEmision = new DateTime(2026, 10, 6), ImporteSubtotal = 100, ImporteTotal = 121,
            ImporteGravado = 100, CodigoMoneda = "PES", CotizacionMoneda = 1,
            CodigoTipoDocumento = 80, NumeroDocumento = 20304050607,
            FechaServicioDesde = new DateTime(2026, 10, 1), FechaServicioHasta = new DateTime(2026, 10, 6),
            FechaVencimientoPago = new DateTime(2026, 10, 20) };
        request.ArrayItems.Add(new ItemType { Codigo = "MTX-1", Descripcion = "Item MTXCA", Cantidad = 2,
            CodigoUnidadMedida = 7, PrecioUnitario = 50, CodigoCondicionIva = 1, ImporteIva = 21, ImporteItem = 121 });
        request.ArraySubtotalesIva.Add(new SubtotalIvaType { Codigo = 4, Importe = 10 });
        request.ArraySubtotalesIva.Add(new SubtotalIvaType { Codigo = 5, Importe = 11 });
        return request;
    }

    private static FecaeSolicitarResponse WsfeResponse(string status, string? cae)
    {
        var result = new FecaeResponse { FeCabResp = new FecaeCabResponse { Cuit = Cuit, PtoVta = 1,
            CbteTipo = 1, CantReg = 1, Resultado = status } };
        result.FeDetResp.Add(new FecaeDetResponse { Concepto = 1, DocTipo = 80, DocNro = 20304050607,
            CbteDesde = 12, CbteHasta = 12, CbteFch = "20261006", Resultado = status, Cae = cae,
            CaeFchVto = cae is null ? null : "20261016" });
        if (status == "R") result.Errors.Add(new Err { Code = 1001, Msg = "Rechazo sintético" });
        return new FecaeSolicitarResponse { FecaeSolicitarResult = result };
    }

    private static FexAuthorizeResponse WsfexResponse() => new() { FexAuthorizeResult = new FexResponseAuthorize
    { FexResultAuth = new ClsFexOutAuthorize { Cuit = Cuit, Id = 4567, PuntoVta = 2, CbteTipo = 19,
        CbteNro = 12, FchCbte = "20261006", Resultado = "A", Cae = Cae, FchVencCae = "20261016" } } };

    private static AutorizarComprobanteResponseType WsmtxcaResponse() => new() { Resultado = ResultadoSimpleType.A,
        ComprobanteResponse = new ComprobanteCaeResponseType { Cuit = Cuit, CodigoTipoComprobante = 1,
            NumeroPuntoVenta = 1, NumeroComprobante = 12, FechaEmision = new DateTime(2026, 10, 6),
            Cae = long.Parse(Cae), FechaVencimientoCae = new DateTime(2026, 10, 16) } };

    private static FeCompConsultarResponse MismatchedWsfeQueryResponse() => new() { FeCompConsultarResult = new FeCompConsultaResponse
    { ResultGet = new FeCompConsResponse { Concepto = 1, DocTipo = 80, DocNro = 20304050608, CbteDesde = 12,
        CbteHasta = 12, CbteFch = "20261006", ImpTotal = 123, ImpTotConc = 0, ImpNeto = 100,
        ImpOpEx = 0, ImpIva = 21, ImpTrib = 2, MonId = "PES", MonCotiz = 1, PtoVta = 1,
        CbteTipo = 1, EmisionTipo = "CAE", Resultado = "A", CodAutorizacion = Cae } } };

    private sealed class DeferredSoapTransport : ISoapTransport
    {
        private readonly TaskCompletionSource<object> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal SoapCall? Call { get; private set; }
        internal void Complete(object value) => response.SetResult(value);

        public async Task<TResponse> SendAsync<TRequest, TResponse>(Uri endpoint, string action, TRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Call = new SoapCall(endpoint, action, request, typeof(TRequest), typeof(TResponse), string.Empty);
            return (TResponse)await response.Task.WaitAsync(cancellationToken);
        }
    }
}
