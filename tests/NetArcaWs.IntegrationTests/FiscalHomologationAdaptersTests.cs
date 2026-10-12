using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Cryptography;
using System.Xml.Serialization;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class FiscalHomologationAdaptersTests
{
    [Theory]
    [InlineData(1, 2, 3)] [InlineData(6, 7, 8)] [InlineData(11, 12, 13)]
    public void Wsfe_factory_builds_invoice_and_associated_notes(int invoiceType, int debitType, int creditType)
    {
        var fixture = ReceiverFor(invoiceType);
        var invoice = FiscalHomologationWsfeChannel.CreateRequest(9001, invoiceType, 123, fixture, null, new DateOnly(2026, 10, 12));
        Assert.Equal(invoiceType, invoice.FeCabReq.CbteTipo);
        Assert.Single(invoice.FeDetReq);
        if (invoiceType is 1 or 6) Assert.Single(invoice.FeDetReq[0].Iva);
        else Assert.Empty(invoice.FeDetReq[0].Iva);
        Assert.Equal(invoice.FeDetReq[0].ImpTotal, invoice.FeDetReq[0].ImpNeto + invoice.FeDetReq[0].ImpIva);

        var identity = new InvoiceIdentity(NetArcaWs.HealthChecks.ArcaEnvironment.Homologation, 20000000001, 9001, invoiceType, 122);
        foreach (int noteType in new[] { debitType, creditType })
        {
            var note = FiscalHomologationWsfeChannel.CreateRequest(9001, noteType, 123, fixture, identity, new DateOnly(2026, 10, 12));
            Assert.Equal(noteType, note.FeCabReq.CbteTipo);
            Assert.Single(note.FeDetReq[0].CbtesAsoc);
            Assert.Equal(invoiceType, note.FeDetReq[0].CbtesAsoc[0].Tipo);
            Assert.Equal(122, note.FeDetReq[0].CbtesAsoc[0].Nro);
        }
    }

    [Theory]
    [InlineData(1, 2, 3)] [InlineData(6, 7, 8)]
    public void Wsmtxca_factory_keeps_item_detail_and_association(int invoiceType, int debitType, int creditType)
    {
        var fixture = ReceiverFor(invoiceType);
        var invoice = FiscalHomologationWsmtxcaChannel.CreateRequest(9001, invoiceType, 123, fixture, null, new DateOnly(2026, 10, 12));
        Assert.Equal(invoiceType, invoice.CodigoTipoComprobante);
        Assert.Single(invoice.ArrayItems);
        Assert.Equal(2m, invoice.ArrayItems[0].Cantidad);
        Assert.Equal(invoiceType is 1 ? 50m : 60.5m, invoice.ArrayItems[0].PrecioUnitario);
        Assert.Equal(21m, invoice.ArrayItems[0].ImporteIva);
        Assert.Equal(invoice.ImporteTotal, invoice.ArrayItems[0].ImporteItem);

        var identity = new InvoiceIdentity(NetArcaWs.HealthChecks.ArcaEnvironment.Homologation, 20000000001, 9001, invoiceType, 122);
        foreach (int noteType in new[] { debitType, creditType })
        {
            var note = FiscalHomologationWsmtxcaChannel.CreateRequest(9001, noteType, 123, fixture, identity, new DateOnly(2026, 10, 12));
            Assert.Equal(invoiceType, note.ArrayComprobantesAsociados.Single().CodigoTipoComprobante);
            Assert.Equal(122, note.ArrayComprobantesAsociados.Single().NumeroComprobante);
            Assert.Single(note.ArrayItems);
        }
    }

    [Theory]
    [InlineData(19)] [InlineData(20)] [InlineData(21)]
    public void Wsfex_factory_preserves_export_id_and_association(int voucherType)
    {
        var fixture = new FiscalExportFixture(840, 840, "Synthetic Client", "Synthetic Address", "FOREIGN-TAX-01", "USD", 1m,
            "Advance payment", new DateOnly(2026, 10, 15), 1, [new FiscalExportItem("SKU-1", "Synthetic item", 2, 50m, 0m)]);
        var association = voucherType == 19 ? null : new InvoiceIdentity(NetArcaWs.HealthChecks.ArcaEnvironment.Homologation,
            20000000001, 9002, 19, 122);
        var request = FiscalHomologationWsfexChannel.CreateRequest(9002, voucherType, 123, 701, fixture, association,
            new DateOnly(2026, 10, 12), 20000000001);
        Assert.Equal(701, request.Id);
        Assert.Equal(voucherType, request.CbteTipo);
        Assert.Equal(100m, request.ImpTotal);
        Assert.Single(request.Items);
        if (association is not null)
        {
            Assert.Single(request.CmpsAsoc);
            Assert.Equal(19, request.CmpsAsoc[0].CbteTipo);
            Assert.Equal(122, request.CmpsAsoc[0].CbteNro);
        }
        else Assert.Empty(request.CmpsAsoc);
    }

    [Fact]
    public void Wsfe_exact_projection_rejects_changed_iva_and_receiver()
    {
        var fixture = ReceiverFor(1);
        var request = FiscalHomologationWsfeChannel.CreateRequest(9001, 1, 123, fixture, null, new DateOnly(2026, 10, 12));
        // Independent synthetic consultation response, encoded through the generated XML contract.
        var independentResponse = new FeCompConsResponse
        {
            Resultado = "A", CodAutorizacion = "99999999999999", EmisionTipo = "CAE", PtoVta = 9001, CbteTipo = 1,
            Concepto = 1, DocTipo = 80, DocNro = 20000000001, CbteDesde = 123, CbteHasta = 123, CbteFch = "20261012",
            ImpTotal = 121, ImpTotConc = 0, ImpNeto = 100, ImpOpEx = 0, ImpTrib = 0, ImpIva = 21,
            MonId = "PES", MonCotiz = 1, CondicionIvaReceptorId = 1
        };
        independentResponse.Iva.Add(new AlicIva { Id = 5, BaseImp = 100, Importe = 21 });
        var remote = XmlRoundTrip(independentResponse);
        FiscalHomologationWsfeChannel.VerifyExactProjection(request.FeDetReq.Single(), remote);
        remote.ImpIva++;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfeChannel.VerifyExactProjection(request.FeDetReq.Single(), remote));
        remote = FiscalHomologationWsfeChannel.CreateQueryProjection(request, "A", "99999999999999");
        remote.DocNro++;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfeChannel.VerifyExactProjection(request.FeDetReq.Single(), remote));
    }

    [Fact]
    public void Wsmtxca_exact_projection_rejects_changed_item_quantity()
    {
        var fixture = ReceiverFor(1);
        var request = FiscalHomologationWsmtxcaChannel.CreateRequest(9001, 1, 123, fixture, null, new DateOnly(2026, 10, 12));
        var independentResponse = new ComprobanteType
        {
            CodigoTipoComprobante = 1, NumeroPuntoVenta = 9001, NumeroComprobante = 123,
            FechaEmision = new DateTime(2026, 10, 12), CodigoTipoDocumento = 80, NumeroDocumento = 20000000001,
            CondicionIvaReceptor = 1, ImporteGravado = 100m, ImporteNoGravado = 0m, ImporteExento = 0m,
            ImporteSubtotal = 100m, ImporteOtrosTributos = 0m, ImporteTotal = 121m, CodigoMoneda = "PES",
            CotizacionMoneda = 1m, CodigoConcepto = 1, CodigoAutorizacion = 123456, CodigoTipoAutorizacion = CodigoTipoAutorizacionSimpleType.A,
            FechaVencimiento = new DateTime(2026, 10, 22)
        };
        independentResponse.ArrayItems.Add(new ItemType
        {
            Codigo = "SYNTHETIC-ITEM", Descripcion = "Synthetic taxable service item", Cantidad = 2m,
            CodigoUnidadMedida = 7, PrecioUnitario = 50m, ImporteBonificacion = 0m,
            CodigoCondicionIva = 5, ImporteIva = 21m, ImporteItem = 121m
        });
        independentResponse.ArraySubtotalesIva.Add(new SubtotalIvaType { Codigo = 5, Importe = 21m });
        var projection = XmlRoundTrip(independentResponse);
        FiscalHomologationWsmtxcaChannel.VerifyExactProjection(request, projection);
        projection.ArrayItems[0].Cantidad++;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsmtxcaChannel.VerifyExactProjection(request, projection));
        projection = FiscalHomologationWsmtxcaChannel.CreateQueryProjection(request, 99999999999999);
        projection.ImporteTotal++;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsmtxcaChannel.VerifyExactProjection(request, projection));
    }

    [Fact]
    public void Wsfex_exact_projection_rejects_changed_quantity_price_total_association_or_customer()
    {
        var fixture = ExportFixture();
        var association = new InvoiceIdentity(NetArcaWs.HealthChecks.ArcaEnvironment.Homologation, 20000000001, 9002, 19, 122);
        var request = FiscalHomologationWsfexChannel.CreateRequest(9002, 20, 123, 701, fixture, association,
            new DateOnly(2026, 10, 12), 20000000001);
        var independentResponse = new ClsFexGetCmpr
        {
            Id = 701, FechaCbte = "20261012", CbteTipo = 20, PuntoVta = 9002, CbteNro = 123,
            TipoExpo = 2, DstCmp = 840, Cliente = "Synthetic Client", CuitPaisCliente = 840,
            DomicilioCliente = "Synthetic Address", IdImpositivo = "FOREIGN-TAX-01", MonedaId = "USD", MonedaCtz = 1m,
            ImpTotal = 100m, FormaPago = "Advance payment", FechaPago = "20261015", IdiomaCbte = 1
        };
        independentResponse.Items.Add(new Item
        {
            ProCodigo = "SKU-1", ProDs = "Synthetic item", ProQty = 2m, ProUmed = 7,
            ProPrecioUni = 50m, ProBonificacion = 0m, ProTotalItem = 100m
        });
        independentResponse.CmpsAsoc.Add(new CmpAsoc { CbteTipo = 19, CbtePuntoVta = 9002, CbteNro = 122, CbteCuit = 20000000001 });
        var projection = XmlRoundTrip(independentResponse);
        FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection);
        projection.Items[0].ProQty++;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection));
        projection = FiscalHomologationWsfexChannel.CreateQueryProjection(request);
        projection.Items[0].ProPrecioUni++;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection));
        projection = FiscalHomologationWsfexChannel.CreateQueryProjection(request);
        projection.ImpTotal++;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection));
        projection = FiscalHomologationWsfexChannel.CreateQueryProjection(request);
        projection.CmpsAsoc[0].CbteNro++;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection));
        projection = FiscalHomologationWsfexChannel.CreateQueryProjection(request);
        projection.Cliente = "Different synthetic buyer";
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection));
    }

    [Fact]
    public void Frozen_payload_validation_keeps_dates_and_remote_id_and_rejects_changed_fixture_or_association()
    {
        var tenant = new ArcaTenantContext("synthetic", 20000000001, NetArcaWs.HealthChecks.ArcaEnvironment.Homologation,
            WsaaCertificateContent.FromPem("synthetic certificate", "synthetic key"));
        var registered = ReceiverFor(1);
        var consumer = ReceiverFor(6);
        var invoiceId = new InvoiceIdentity(NetArcaWs.HealthChecks.ArcaEnvironment.Homologation, tenant.Cuit, 9001, 1, 122);
        var noteScenario = new FiscalHomologationScenario("wsfe", 2, "wsfe-a-nota-debito", "wsfe-a-factura");
        var wsfeRequest = FiscalHomologationWsfeChannel.CreateRequest(9001, 2, 123, registered, invoiceId, new DateOnly(2026, 10, 12));
        var wsfeFrozen = SafeInvoiceService.CreateWsfeSubmission(tenant, "synthetic-wsfe", wsfeRequest);
        var wsfe = new FiscalHomologationWsfeChannel(null!, null!, registered, consumer);
        wsfe.ValidateFrozenSubmission(wsfeFrozen, noteScenario, 9001, registered, invoiceId);
        var changedInvoice = invoiceId with { VoucherNumber = 121 };
        Assert.Throws<InvoiceConflictException>(() => wsfe.ValidateFrozenSubmission(wsfeFrozen, noteScenario, 9001, registered, changedInvoice));
        var changedReceiver = registered with { DocumentNumber = 20333333334 };
        var changedWsfe = new FiscalHomologationWsfeChannel(null!, null!, changedReceiver, consumer);
        Assert.Throws<InvoiceConflictException>(() => changedWsfe.ValidateFrozenSubmission(wsfeFrozen, noteScenario, 9001, registered, invoiceId));

        var export = ExportFixture();
        var exportScenario = new FiscalHomologationScenario("wsfex", 20, "wsfex-e-nota-debito", "wsfex-e-factura");
        var exportInvoiceId = new InvoiceIdentity(tenant.Environment, tenant.Cuit, 9002, 19, 122);
        var exportRequest = FiscalHomologationWsfexChannel.CreateRequest(9002, 20, 123, 701, export, exportInvoiceId,
            new DateOnly(2026, 10, 12), tenant.Cuit);
        var exportFrozen = SafeInvoiceService.CreateWsfexSubmission(tenant, "synthetic-wsfex", exportRequest);
        var wsfex = new FiscalHomologationWsfexChannel(null!, null!, export);
        wsfex.ValidateFrozenSubmission(exportFrozen, exportScenario, 9002, new(99, 0, 5, true), exportInvoiceId);
        Assert.Equal(701, exportFrozen.RemoteRequestId);
        Assert.Contains("20261012", exportFrozen.Payload);
        var changedExportFixture = export with { ForeignTaxId = "CHANGED-SYNTHETIC-ID" };
        Assert.Throws<InvoiceConflictException>(() => new FiscalHomologationWsfexChannel(null!, null!, changedExportFixture)
            .ValidateFrozenSubmission(exportFrozen, exportScenario, 9002, new(99, 0, 5, true), exportInvoiceId));

        var mtxcaRequest = FiscalHomologationWsmtxcaChannel.CreateRequest(9001, 2, 123, registered, invoiceId,
            new DateOnly(2026, 10, 12));
        var mtxcaFrozen = SafeInvoiceService.CreateWsmtxcaSubmission(tenant, "synthetic-mtxca", mtxcaRequest);
        var mtxca = new FiscalHomologationWsmtxcaChannel(null!, null!, registered, consumer);
        mtxca.ValidateFrozenSubmission(mtxcaFrozen, new("wsmtxca", 2, "wsmtxca-a-nota-debito", "wsmtxca-a-factura"),
            9001, registered, invoiceId);
        Assert.Throws<InvoiceConflictException>(() => mtxca.ValidateFrozenSubmission(mtxcaFrozen,
            new("wsmtxca", 2, "wsmtxca-a-nota-debito", "wsmtxca-a-factura"), 9001, registered, changedInvoice));
    }

    [Fact]
    public void Receiver_preflight_rejects_invalid_cuit_or_family_vat_condition_before_catalogue_calls()
    {
        var validRegistered = ReceiverFor(1);
        var validConsumer = ReceiverFor(6);
        var wsfe = new FiscalHomologationWsfeChannel(null!, null!, validRegistered, validConsumer);
        var mtxca = new FiscalHomologationWsmtxcaChannel(null!, null!, validRegistered, validConsumer);
        Assert.Throws<InvalidOperationException>(() => new FiscalHomologationWsfeChannel(null!, null!,
            validRegistered with { DocumentNumber = 20000000000 }, validConsumer)
            .ValidateScenario(new("wsfe", 1, "wsfe-a-factura"), 9001, validRegistered));
        Assert.Throws<InvalidOperationException>(() => new FiscalHomologationWsfeChannel(null!, null!,
            validRegistered with { VatConditionId = 0 }, validConsumer)
            .ValidateScenario(new("wsfe", 1, "wsfe-a-factura"), 9001, validRegistered));
        Assert.Throws<InvalidOperationException>(() => new FiscalHomologationWsmtxcaChannel(null!, null!,
            validRegistered with { VatConditionId = 5 }, validConsumer)
            .ValidateScenario(new("wsmtxca", 1, "wsmtxca-a-factura"), 9001, validRegistered));
        Assert.Throws<InvalidOperationException>(() => new FiscalHomologationWsmtxcaChannel(null!, null!,
            validRegistered, validConsumer with { DocumentType = 80 })
            .ValidateScenario(new("wsmtxca", 6, "wsmtxca-b-factura"), 9001, validConsumer));
        var wsfex = new FiscalHomologationWsfexChannel(null!, null!, ExportFixture());
        Assert.False(wsfex.CanAssociate(20, 20));
        Assert.False(wsfex.CanAssociate(21, 21));
        Assert.True(wsfex.CanAssociate(20, 19));
    }

    [Fact]
    public void Wsmtxca_cae_point_of_sale_must_exist_unblocked_and_without_cancellation_date()
    {
        static ConsultarPuntosVentaCaeResponse Response(int number, SiNoSimpleType blocked, DateTime? cancellationDate = null)
        {
            var response = new ConsultarPuntosVentaCaeResponse();
            response.ArrayPuntosVenta.Add(new PuntoVentaType
            { NumeroPuntoVenta = number, Bloqueado = blocked, FechaBaja = cancellationDate });
            return response;
        }

        Assert.True(FiscalHomologationWsmtxcaChannel.IsActiveCaePointOfSale(Response(9001, SiNoSimpleType.N), 9001));
        Assert.False(FiscalHomologationWsmtxcaChannel.IsActiveCaePointOfSale(Response(9001, SiNoSimpleType.N), 9002));
        Assert.False(FiscalHomologationWsmtxcaChannel.IsActiveCaePointOfSale(Response(9001, SiNoSimpleType.S), 9001));
        Assert.False(FiscalHomologationWsmtxcaChannel.IsActiveCaePointOfSale(Response(9001, SiNoSimpleType.N,
            new DateTime(2026, 10, 11)), 9001));
        Assert.False(FiscalHomologationWsmtxcaChannel.IsActiveCaePointOfSale(null, 9001));
    }

    private static FiscalExportFixture ExportFixture() => new(840, 840, "Synthetic Client", "Synthetic Address", "FOREIGN-TAX-01", "USD", 1m,
        "Advance payment", new DateOnly(2026, 10, 15), 1, [new FiscalExportItem("SKU-1", "Synthetic item", 2, 50m, 0m)]);

    private static T XmlRoundTrip<T>(T value)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        serializer.Serialize(writer, value);
        using var reader = new StringReader(writer.ToString());
        return (T)(serializer.Deserialize(reader) ?? throw new InvalidOperationException("Synthetic XML fixture did not deserialize."));
    }

    private static FiscalHomologationReceiver ReceiverFor(int type) => type is 1 or 2 or 3
        ? new(80, 20000000001, 1, FixtureReviewed: true)
        : new(99, 0, 5, FixtureReviewed: true);
}
