using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Cryptography;
using NetArcaWs.Services;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Reflection;
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
        Assert.Equal(invoiceType is 1 ? (short?)80 : null, invoice.CodigoTipoDocumento);
        Assert.Equal(invoiceType is 1 ? 20_000_000_001L : null, invoice.NumeroDocumento);
        Assert.Equal(invoiceType is 1 ? (short)1 : (short)5, invoice.CondicionIvaReceptor);
        AssertDocumentElements(invoice, invoiceType is 1);
        Assert.Null(invoice.ImporteOtrosTributos);
        Assert.Single(invoice.ArrayItems);
        Assert.Equal(2m, invoice.ArrayItems[0].Cantidad);
        Assert.Equal(invoiceType is 1 ? 50m : 60.5m, invoice.ArrayItems[0].PrecioUnitario);
        Assert.Equal(1, invoice.ArrayItems[0].UnidadesMtx);
        Assert.Equal((short)7, invoice.ArrayItems[0].CodigoUnidadMedida);
        Assert.Equal("0123456789913", invoice.ArrayItems[0].CodigoMtx);
        Assert.Equal("Synthetic taxable product item", invoice.ArrayItems[0].Descripcion);
        Assert.Equal(invoiceType is 1 ? 21m : null, invoice.ArrayItems[0].ImporteIva);
        Assert.Equal(invoice.ImporteTotal, invoice.ArrayItems[0].ImporteItem);

        var identity = new InvoiceIdentity(NetArcaWs.HealthChecks.ArcaEnvironment.Homologation, 20000000001, 9001, invoiceType, 122);
        foreach (int noteType in new[] { debitType, creditType })
        {
            var note = FiscalHomologationWsmtxcaChannel.CreateRequest(9001, noteType, 123, fixture, identity, new DateOnly(2026, 10, 12));
            Assert.Equal(invoiceType, note.ArrayComprobantesAsociados.Single().CodigoTipoComprobante);
            Assert.Equal(122, note.ArrayComprobantesAsociados.Single().NumeroComprobante);
            Assert.Equal(invoiceType is 1 ? (short?)80 : null, note.CodigoTipoDocumento);
            Assert.Equal(invoiceType is 1 ? 20_000_000_001L : null, note.NumeroDocumento);
            Assert.Equal(invoiceType is 1 ? (short)1 : (short)5, note.CondicionIvaReceptor);
            AssertDocumentElements(note, invoiceType is 1);
            Assert.Null(note.ImporteOtrosTributos);
            Assert.Single(note.ArrayItems);
            Assert.Equal(1, note.ArrayItems[0].UnidadesMtx);
            Assert.Equal((short)7, note.ArrayItems[0].CodigoUnidadMedida);
            Assert.Equal("0123456789913", note.ArrayItems[0].CodigoMtx);
            Assert.Equal(invoiceType is 1 ? 21m : null, note.ArrayItems[0].ImporteIva);
        }
    }

    private static void AssertDocumentElements(ComprobanteType request, bool expectedPresent)
    {
        using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        new XmlSerializer(typeof(ComprobanteType)).Serialize(writer, request);
        XElement xml = XElement.Parse(writer.ToString());
        Assert.Equal(expectedPresent, xml.Element("codigoTipoDocumento") is not null);
        Assert.Equal(expectedPresent, xml.Element("numeroDocumento") is not null);
    }

    [Theory]
    [InlineData(CodigoTipoAutorizacionSimpleType.E, true)]
    [InlineData(CodigoTipoAutorizacionSimpleType.A, false)]
    public async Task Wsmtxca_exact_verification_accepts_only_CAE_authorization_type(
        CodigoTipoAutorizacionSimpleType authorizationType, bool expected)
    {
        var tenant = new ArcaTenantContext("synthetic", 20000000001, NetArcaWs.HealthChecks.ArcaEnvironment.Homologation,
            WsaaCertificateContent.FromPem("synthetic certificate", "synthetic key"));
        var request = FiscalHomologationWsmtxcaChannel.CreateRequest(9001, 1, 123, ReceiverFor(1), null,
            new DateOnly(2026, 10, 12));
        InvoiceSubmission submission = SafeInvoiceService.CreateWsmtxcaSubmission(tenant, "mtx-exact-auth-type", request);
        ComprobanteType response = FiscalHomologationWsmtxcaChannel.CreateQueryProjection(request, 71234567890123);
        response.CodigoTipoAutorizacion = authorizationType;
        IWsmtxcav1Service service = DispatchProxy.Create<IWsmtxcav1Service, WsmtxcaQueryProxy>();
        ((WsmtxcaQueryProxy)(object)service).Comprobante = response;
        var channel = new FiscalHomologationWsmtxcaChannel(service, null!, ReceiverFor(1), ReceiverFor(6));
        var operation = new InvoiceOperation(submission, InvoiceState.Authorized, 2, 1, "71234567890123");

        if (expected)
            await channel.VerifyExactAsync(tenant, operation, CancellationToken.None);
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => channel.VerifyExactAsync(tenant, operation, CancellationToken.None));
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    public async Task Wsmtxca_exact_verification_rejects_matching_comprobante_when_errors_are_present(
        bool includeError, bool includeObservation, bool expectedToPass)
    {
        var tenant = new ArcaTenantContext("synthetic", 20000000001, NetArcaWs.HealthChecks.ArcaEnvironment.Homologation,
            WsaaCertificateContent.FromPem("synthetic certificate", "synthetic key"));
        var request = FiscalHomologationWsmtxcaChannel.CreateRequest(9001, 1, 123, ReceiverFor(1), null,
            new DateOnly(2026, 10, 12));
        InvoiceSubmission submission = SafeInvoiceService.CreateWsmtxcaSubmission(tenant, "mtx-exact-errors", request);
        ComprobanteType response = FiscalHomologationWsmtxcaChannel.CreateQueryProjection(request, 71234567890123);
        IWsmtxcav1Service service = DispatchProxy.Create<IWsmtxcav1Service, WsmtxcaQueryProxy>();
        var proxy = (WsmtxcaQueryProxy)(object)service;
        proxy.Comprobante = response;
        proxy.IncludeError = includeError;
        proxy.IncludeObservation = includeObservation;
        var channel = new FiscalHomologationWsmtxcaChannel(service, null!, ReceiverFor(1), ReceiverFor(6));
        var operation = new InvoiceOperation(submission, InvoiceState.Authorized, 2, 1, "71234567890123");

        if (expectedToPass)
            await channel.VerifyExactAsync(tenant, operation, CancellationToken.None);
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => channel.VerifyExactAsync(tenant, operation, CancellationToken.None));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Wsfex_exact_verification_rejects_matching_result_when_error_is_present(
        bool includeError, bool expectedToPass)
    {
        var tenant = new ArcaTenantContext("synthetic", 20000000001, NetArcaWs.HealthChecks.ArcaEnvironment.Homologation,
            WsaaCertificateContent.FromPem("synthetic certificate", "synthetic key"));
        ClsFexRequest request = FiscalHomologationWsfexChannel.CreateRequest(9002, 19, 123, 701,
            ExportFixture(), null, new DateOnly(2026, 10, 12), 20000000001);
        InvoiceSubmission submission = SafeInvoiceService.CreateWsfexSubmission(tenant, "fex-exact-errors", request);
        var result = FiscalHomologationWsfexChannel.CreateQueryProjection(request, "A", "71234567890123");
        IWsfexv1Service service = DispatchProxy.Create<IWsfexv1Service, WsfexQueryProxy>();
        ((WsfexQueryProxy)(object)service).Response = new FexGetCmpResponse2
        {
            FexGetCmpResult = new FexGetCmpResponse
            {
                FexResultGet = result,
                FexErr = includeError ? new ClsFexErr { ErrCode = 1550, ErrMsg = "redacted" } : null,
                FexEvents = new ClsFexEvents { EventCode = 1, EventMsg = "redacted" }
            }
        };
        var channel = new FiscalHomologationWsfexChannel(service, null!, ExportFixture());
        var operation = new InvoiceOperation(submission, InvoiceState.Authorized, 2, 1, "71234567890123");

        if (expectedToPass)
            await channel.VerifyExactAsync(tenant, operation, CancellationToken.None);
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => channel.VerifyExactAsync(tenant, operation, CancellationToken.None));
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
        Assert.Equal(string.Empty, request.PermisoExistente);
        using (var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture))
        {
            new XmlSerializer(typeof(ClsFexRequest)).Serialize(writer, request);
            XElement permission = XElement.Parse(writer.ToString()).Descendants()
                .Single(element => element.Name.LocalName == "Permiso_existente");
            Assert.Equal(string.Empty, permission.Value);
            Assert.Empty(permission.Elements());
        }
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

    [Theory]
    [InlineData(19, "20261015", true)]
    [InlineData(20, null, false)]
    [InlineData(21, null, false)]
    public void Wsfex_factory_includes_payment_date_only_for_export_invoices(int voucherType,
        string? expectedPaymentDate, bool expectedXmlElement)
    {
        var fixture = new FiscalExportFixture(840, 840, "Synthetic Client", "Synthetic Address", "FOREIGN-TAX-01", "USD", 1m,
            "Advance payment", new DateOnly(2026, 10, 15), 1, [new FiscalExportItem("SKU-1", "Synthetic item", 2, 50m, 0m)]);
        var association = voucherType == 19 ? null : new InvoiceIdentity(NetArcaWs.HealthChecks.ArcaEnvironment.Homologation,
            20000000001, 9002, 19, 122);

        ClsFexRequest request = FiscalHomologationWsfexChannel.CreateRequest(9002, voucherType, 123, 701,
            fixture, association, new DateOnly(2026, 10, 12), 20000000001);
        using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        new XmlSerializer(typeof(ClsFexRequest)).Serialize(writer, request);
        XElement xml = XElement.Parse(writer.ToString());
        XElement? paymentDate = xml.Descendants().SingleOrDefault(element => element.Name.LocalName == "Fecha_pago");

        Assert.Equal(expectedPaymentDate, request.FechaPago);
        Assert.Equal(expectedXmlElement, paymentDate is not null);
        if (expectedPaymentDate is not null) Assert.Equal(expectedPaymentDate, paymentDate!.Value);
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
    public void Wsfe_exact_projection_normalizes_only_empty_dates_for_product_concept_one()
    {
        var request = FiscalHomologationWsfeChannel.CreateRequest(9001, 1, 123, ReceiverFor(1), null,
            new DateOnly(2026, 10, 12));
        FecaeDetRequest expected = request.FeDetReq.Single();
        FeCompConsResponse remote = FiscalHomologationWsfeChannel.CreateQueryProjection(request, "A", "99999999999999");
        remote.FchServDesde = string.Empty;
        remote.FchServHasta = string.Empty;
        remote.FchVtoPago = string.Empty;
        FiscalHomologationWsfeChannel.VerifyExactProjection(expected, remote);

        foreach (int concept in new[] { 2, 3 })
        {
            FecaeDetRequest serviceExpected = XmlRoundTrip(expected);
            serviceExpected.Concepto = concept;
            FeCompConsResponse serviceRemote = XmlRoundTrip(remote);
            serviceRemote.Concepto = concept;
            Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfeChannel.VerifyExactProjection(serviceExpected, serviceRemote));
        }

        FeCompConsResponse validDate = XmlRoundTrip(remote);
        validDate.FchServDesde = "20261012";
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfeChannel.VerifyExactProjection(expected, validDate));

        FeCompConsResponse changedAmount = XmlRoundTrip(remote);
        changedAmount.ImpTotal++;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfeChannel.VerifyExactProjection(expected, changedAmount));
        FeCompConsResponse changedDocument = XmlRoundTrip(remote);
        changedDocument.DocNro++;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfeChannel.VerifyExactProjection(expected, changedDocument));
        FeCompConsResponse changedVat = XmlRoundTrip(remote);
        changedVat.Iva.Single().Importe++;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfeChannel.VerifyExactProjection(expected, changedVat));
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
            ImporteSubtotal = 100m, ImporteOtrosTributos = null, ImporteTotal = 121m, CodigoMoneda = "PES",
            CotizacionMoneda = 1m, CodigoConcepto = 1, CodigoAutorizacion = 123456, CodigoTipoAutorizacion = CodigoTipoAutorizacionSimpleType.E,
            FechaVencimiento = new DateTime(2026, 10, 22)
        };
        independentResponse.ArrayItems.Add(new ItemType
        {
            Codigo = "SYNTHETIC-ITEM", UnidadesMtx = 1, CodigoMtx = "0123456789913", Descripcion = "Synthetic taxable product item", Cantidad = 2m,
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
    public void Wsmtxca_exact_query_rejects_error_entries_but_allows_observations()
    {
        var request = FiscalHomologationWsmtxcaChannel.CreateRequest(9001, 1, 123, ReceiverFor(1), null,
            new DateOnly(2026, 10, 12));
        var response = new ConsultarComprobanteResponseType
        {
            Comprobante = FiscalHomologationWsmtxcaChannel.CreateQueryProjection(request, 12345678901234)
        };
        response.ArrayObservaciones.Add(new CodigoDescripcionType { Codigo = 130, Descripcion = "Allowed observation" });

        FiscalHomologationWsmtxcaChannel.EnsureNoQueryErrors(response);

        response.ArrayErrores.Add(new CodigoDescripcionType { Codigo = 504, Descripcion = "Synthetic query error" });
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsmtxcaChannel.EnsureNoQueryErrors(response));
    }

    [Fact]
    public void Wsfex_exact_query_rejects_nonzero_query_error()
    {
        var request = FiscalHomologationWsfexChannel.CreateRequest(9002, 19, 123, 701, ExportFixture(), null,
            new DateOnly(2026, 10, 12), 20000000001);
        var response = new FexGetCmpResponse
        {
            FexResultGet = FiscalHomologationWsfexChannel.CreateQueryProjection(request),
            FexErr = new ClsFexErr { ErrCode = 1020, ErrMsg = "Synthetic query error" }
        };

        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfexChannel.EnsureNoQueryErrors(response));
    }

    [Fact]
    public void Wsmtxca_exact_projection_compares_decimal_values_without_tolerance()
    {
        var request = FiscalHomologationWsmtxcaChannel.CreateRequest(9001, 1, 123, ReceiverFor(1), null,
            new DateOnly(2026, 10, 12));
        request.ImporteOtrosTributos = 1m;
        request.ArrayOtrosTributos.Add(new OtroTributoType
        {
            Codigo = 99, Descripcion = "Synthetic tax", BaseImponible = 10m, Importe = 1m
        });
        request.ArrayCompradores.Add(new CompradorType
        {
            CodigoTipoDocumento = 80, NumeroDocumento = 20000000001, Porcentaje = 100m
        });
        ComprobanteType projection = FiscalHomologationWsmtxcaChannel.CreateQueryProjection(request, 12345678901234);
        projection.ImporteGravado = 100.00m;
        projection.ImporteNoGravado = 0.00m;
        projection.ImporteExento = 0.000m;
        projection.ImporteSubtotal = 100.000m;
        projection.ImporteTotal = 121.000m;
        projection.CotizacionMoneda = 1.000m;
        projection.ArrayItems[0].Cantidad = 2.000m;
        projection.ArrayItems[0].PrecioUnitario = 50.00m;
        projection.ArrayItems[0].ImporteBonificacion = 0.00m;
        projection.ArrayItems[0].ImporteIva = 21.000m;
        projection.ArrayItems[0].ImporteItem = 121.00m;
        projection.ArraySubtotalesIva[0].Importe = 21.000m;
        projection.ArrayOtrosTributos[0].BaseImponible = 10.000m;
        projection.ArrayOtrosTributos[0].Importe = 1.00m;
        projection.ArrayCompradores[0].Porcentaje = 100.00m;

        FiscalHomologationWsmtxcaChannel.VerifyExactProjection(request, projection);

        projection.ImporteTotal = 121.01m;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsmtxcaChannel.VerifyExactProjection(request, projection));
    }

    [Theory]
    [InlineData("gtin-leading-zero")]
    [InlineData("description-whitespace")]
    public void Wsmtxca_exact_projection_keeps_gtin_and_text_exact(string changedField)
    {
        var request = FiscalHomologationWsmtxcaChannel.CreateRequest(9001, 1, 123, ReceiverFor(1), null,
            new DateOnly(2026, 10, 12));
        ComprobanteType projection = FiscalHomologationWsmtxcaChannel.CreateQueryProjection(request, 12345678901234);
        if (changedField == "gtin-leading-zero")
            projection.ArrayItems[0].CodigoMtx = "123456789913";
        else
        {
            request.ArrayItems[0].Descripcion = string.Empty;
            projection.ArrayItems[0].Descripcion = " ";
        }

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
            TipoExpo = 2, PermisoExistente = string.Empty, DstCmp = 840, Cliente = "Synthetic Client", CuitPaisCliente = 840,
            DomicilioCliente = "Synthetic Address", IdImpositivo = "FOREIGN-TAX-01", MonedaId = "USD", MonedaCtz = 1m,
            ImpTotal = 100m, FormaPago = "Advance payment", IdiomaCbte = 1
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
        projection = FiscalHomologationWsfexChannel.CreateQueryProjection(request);
        projection.MonedaCtz++;
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection));
        projection = FiscalHomologationWsfexChannel.CreateQueryProjection(request);
        projection.FechaPago = "20261015";
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection));
    }

    [Theory]
    [InlineData("Obs_comerciales")]
    [InlineData("Obs")]
    [InlineData("Incoterms")]
    public void Wsfex_exact_projection_allows_absent_empty_optional_fields_without_mutating_snapshots(string field)
    {
        var fixture = ExportFixture();
        var request = FiscalHomologationWsfexChannel.CreateRequest(9002, 19, 123, 701, fixture, null,
            new DateOnly(2026, 10, 12), 20000000001);
        foreach ((string? expectedValue, string? actualValue) in new (string?, string?)[]
        {
            (null, string.Empty), (string.Empty, null)
        })
        {
            SetWsfexOptionalProjectionField(request, field, expectedValue);
            ClsFexGetCmpr projection = FiscalHomologationWsfexChannel.CreateQueryProjection(request);
            SetWsfexOptionalProjectionField(projection, field, actualValue);

            FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection);
            Assert.Equal(expectedValue, GetWsfexOptionalProjectionField(request, field));
            Assert.Equal(actualValue, GetWsfexOptionalProjectionField(projection, field));
        }
    }

    [Fact]
    public void Wsfex_exact_projection_allows_incoterms_null_empty_only_outside_type19_export_type1()
    {
        var fixture = ExportFixture();
        var request = FiscalHomologationWsfexChannel.CreateRequest(9002, 19, 123, 701, fixture, null,
            new DateOnly(2026, 10, 12), 20000000001);
        request.TipoExpo = 1;
        ClsFexGetCmpr projection = FiscalHomologationWsfexChannel.CreateQueryProjection(request);
        projection.Incoterms = string.Empty;

        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection));
    }

    [Theory]
    [InlineData("IncotermsDs", null, "FOB")]
    [InlineData("IncotermsDs", null, " ")]
    [InlineData("IncotermsDs", "0", "")]
    [InlineData("Incoterms", "FOB", "CIF")]
    [InlineData("Obs", null, " ")]
    public void Wsfex_exact_projection_keeps_description_nonempty_and_whitespace_strict(string field,
        string? expectedValue, string actualValue)
    {
        var fixture = ExportFixture();
        var request = FiscalHomologationWsfexChannel.CreateRequest(9002, 19, 123, 701, fixture, null,
            new DateOnly(2026, 10, 12), 20000000001);
        SetWsfexOptionalProjectionField(request, field, expectedValue);
        ClsFexGetCmpr projection = FiscalHomologationWsfexChannel.CreateQueryProjection(request);
        SetWsfexOptionalProjectionField(projection, field, actualValue);

        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection));
    }

    private static void SetWsfexOptionalProjectionField(object target, string field, string? value)
    {
        switch (field)
        {
            case "Obs_comerciales":
                if (target is ClsFexRequest) ((ClsFexRequest)target).ObsComerciales = value;
                else ((ClsFexGetCmpr)target).ObsComerciales = value;
                break;
            case "Obs":
                if (target is ClsFexRequest) ((ClsFexRequest)target).Obs = value;
                else ((ClsFexGetCmpr)target).Obs = value;
                break;
            case "Incoterms":
                if (target is ClsFexRequest) ((ClsFexRequest)target).Incoterms = value;
                else ((ClsFexGetCmpr)target).Incoterms = value;
                break;
            case "IncotermsDs":
                if (target is ClsFexRequest) ((ClsFexRequest)target).IncotermsDs = value;
                else ((ClsFexGetCmpr)target).IncotermsDs = value;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(field));
        }
    }

    private static string? GetWsfexOptionalProjectionField(object target, string field) => (target, field) switch
    {
        (ClsFexRequest, "Obs_comerciales") => ((ClsFexRequest)target).ObsComerciales,
        (ClsFexGetCmpr, "Obs_comerciales") => ((ClsFexGetCmpr)target).ObsComerciales,
        (ClsFexRequest, "Obs") => ((ClsFexRequest)target).Obs,
        (ClsFexGetCmpr, "Obs") => ((ClsFexGetCmpr)target).Obs,
        (ClsFexRequest, "Incoterms") => ((ClsFexRequest)target).Incoterms,
        (ClsFexGetCmpr, "Incoterms") => ((ClsFexGetCmpr)target).Incoterms,
        (ClsFexRequest, "IncotermsDs") => ((ClsFexRequest)target).IncotermsDs,
        (ClsFexGetCmpr, "IncotermsDs") => ((ClsFexGetCmpr)target).IncotermsDs,
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    [Fact]
    public void Wsfex_exact_projection_rejects_whitespace_in_optional_incoterms()
    {
        var fixture = ExportFixture();
        var request = FiscalHomologationWsfexChannel.CreateRequest(9002, 19, 123, 701, fixture, null,
            new DateOnly(2026, 10, 12), 20000000001);
        request.Incoterms = null;
        ClsFexGetCmpr projection = FiscalHomologationWsfexChannel.CreateQueryProjection(request);
        projection.Incoterms = " ";

        Assert.Throws<InvalidOperationException>(() => FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection));
    }

    [Theory]
    [InlineData(19, 2, false)]
    [InlineData(19, 2, true)]
    [InlineData(19, 4, false)]
    [InlineData(19, 4, true)]
    [InlineData(20, 2, false)]
    [InlineData(20, 2, true)]
    [InlineData(20, 4, false)]
    [InlineData(20, 4, true)]
    [InlineData(21, 2, false)]
    [InlineData(21, 2, true)]
    [InlineData(21, 4, false)]
    [InlineData(21, 4, true)]
    public void Wsfex_exact_projection_accepts_query_description_zero_only_for_unset_export_incoterms(
        int voucherType, int exportType, bool expectedEmpty)
    {
        var fixture = ExportFixture();
        InvoiceIdentity? association = voucherType == 19 ? null :
            new InvoiceIdentity(NetArcaWs.HealthChecks.ArcaEnvironment.Homologation, 20000000001, 9002, 19, 122);
        var request = FiscalHomologationWsfexChannel.CreateRequest(9002, voucherType, 123, 701, fixture, association,
            new DateOnly(2026, 10, 12), 20000000001);
        request.TipoExpo = checked((short)exportType);
        request.Incoterms = expectedEmpty ? string.Empty : null;
        request.IncotermsDs = expectedEmpty ? string.Empty : null;
        ClsFexGetCmpr projection = FiscalHomologationWsfexChannel.CreateQueryProjection(request);
        projection.Incoterms = expectedEmpty ? null : string.Empty;
        projection.IncotermsDs = "0";

        FiscalHomologationWsfexChannel.VerifyExactProjection(request, projection);

        Assert.Equal(expectedEmpty ? string.Empty : null, request.Incoterms);
        Assert.Equal(expectedEmpty ? string.Empty : null, request.IncotermsDs);
        Assert.Equal(expectedEmpty ? null : string.Empty, projection.Incoterms);
        Assert.Equal("0", projection.IncotermsDs);
    }

    [Theory]
    [InlineData(1, null, null, null, "0")]
    [InlineData(3, null, null, null, "0")]
    [InlineData(2, "FOB", null, null, "0")]
    [InlineData(2, null, "Known description", null, "0")]
    [InlineData(2, "FOB", null, "CIF", "0")]
    [InlineData(2, null, "0", null, null)]
    [InlineData(2, null, null, null, "0 ")]
    public void Wsfex_exact_projection_rejects_description_zero_outside_exact_unset_export_guard(int exportType,
        string? expectedCode, string? expectedDescription, string? actualCode, string? actualDescription)
    {
        var fixture = ExportFixture();
        var request = FiscalHomologationWsfexChannel.CreateRequest(9002, 19, 123, 701, fixture, null,
            new DateOnly(2026, 10, 12), 20000000001);
        request.TipoExpo = checked((short)exportType);
        request.Incoterms = expectedCode;
        request.IncotermsDs = expectedDescription;
        ClsFexGetCmpr projection = FiscalHomologationWsfexChannel.CreateQueryProjection(request);
        projection.Incoterms = actualCode;
        projection.IncotermsDs = actualDescription;

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
    public void Wsfex_frozen_submission_with_omitted_permission_is_rejected_against_the_explicit_empty_fixture()
    {
        var tenant = new ArcaTenantContext("synthetic", 20000000001, NetArcaWs.HealthChecks.ArcaEnvironment.Homologation,
            WsaaCertificateContent.FromPem("synthetic certificate", "synthetic key"));
        var fixture = new FiscalExportFixture(840, 840, "Synthetic Client", "Synthetic Address", "FOREIGN-TAX-01", "USD", 1m,
            "Advance payment", new DateOnly(2026, 10, 15), 1,
            [new FiscalExportItem("SKU-1", "Synthetic item", 2, 50m, 0m)]);
        ClsFexRequest oldRequest = FiscalHomologationWsfexChannel.CreateRequest(9002, 19, 123, 701, fixture, null,
            new DateOnly(2026, 10, 12), tenant.Cuit);
        oldRequest.PermisoExistente = null!;
        InvoiceSubmission frozen = SafeInvoiceService.CreateWsfexSubmission(tenant, "old-omitted-permission", oldRequest);
        var channel = new FiscalHomologationWsfexChannel(null!, null!, fixture);

        Assert.Throws<InvoiceConflictException>(() => channel.ValidateFrozenSubmission(frozen,
            new("wsfex", 19, "wsfex-e-factura"), 9002, new(99, 0, 5, FixtureReviewed: true), null));
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

    public class WsmtxcaQueryProxy : DispatchProxy
    {
        public ComprobanteType? Comprobante { get; set; }
        public bool IncludeError { get; set; }
        public bool IncludeObservation { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.NotNull(targetMethod);
            if (targetMethod!.Name != nameof(IWsmtxcav1Service.consultarComprobanteAsync))
                throw new NotSupportedException($"Unexpected offline WSMTXCA call: {targetMethod.Name}.");
            var response = new ConsultarComprobanteResponseType { Comprobante = Comprobante };
            if (IncludeError) response.ArrayErrores.Add(new CodigoDescripcionType { Codigo = 9001, Descripcion = "redacted" });
            if (IncludeObservation) response.ArrayObservaciones.Add(new CodigoDescripcionType { Codigo = 504, Descripcion = "redacted" });
            return Task.FromResult(response);
        }
    }

    public class WsfexQueryProxy : DispatchProxy
    {
        public FexGetCmpResponse2? Response { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.NotNull(targetMethod);
            if (targetMethod!.Name != nameof(IWsfexv1Service.FEXGetCMPAsync))
                throw new NotSupportedException($"Unexpected offline WSFEX call: {targetMethod.Name}.");
            return Task.FromResult(Response ?? throw new InvalidOperationException("Synthetic WSFEX query response is missing."));
        }
    }

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
