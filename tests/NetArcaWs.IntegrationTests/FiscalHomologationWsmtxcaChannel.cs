using System.Globalization;
using System.Xml.Linq;
using System.Xml.Serialization;
using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;

namespace NetArcaWs.IntegrationTests;

internal sealed class FiscalHomologationWsmtxcaChannel(
    IWsmtxcav1Service service,
    SafeInvoiceService invoices,
    FiscalHomologationReceiver registeredTaxpayerReceiver,
    FiscalHomologationReceiver consumerFinalReceiver,
    TimeProvider? clock = null,
    bool allowFixedQaPointOfSaleForEmptyCatalogue = false) : IFiscalHomologationChannel
{
    private int? configuredPointOfSale;
    public string Service => "wsmtxca";
    private TimeProvider Clock { get; } = clock ?? TimeProvider.System;
    public bool Supports(int voucherType) => voucherType is 1 or 2 or 3 or 6 or 7 or 8;
    public bool CanAssociate(int noteType, int originalType) => (noteType, originalType) switch
    { (2 or 3, 1) or (7 or 8, 6) => true, _ => false };

    public void ValidateScenario(FiscalHomologationScenario scenario, int pointOfSale, FiscalHomologationReceiver receiver)
    {
        if (!Supports(scenario.VoucherType)) throw new InvalidOperationException("WSMTXCA supports only itemized A/B invoices and debit/credit notes in this matrix.");
        ValidateReceiver(ReceiverFor(scenario.VoucherType), scenario.VoucherType is 1 or 2 or 3);
        if (pointOfSale is <= 0 or > 99998 || (configuredPointOfSale is not null && configuredPointOfSale != pointOfSale))
            throw new InvalidOperationException("WSMTXCA requires one configured point of sale in the supported range.");
        configuredPointOfSale = pointOfSale;
        if (receiver is null || !receiver.FixtureReviewed) throw new InvalidOperationException("A reviewed receiver fixture must be selected by the caller.");
    }

    public void ValidateFrozenSubmission(InvoiceSubmission frozen, FiscalHomologationScenario scenario, int pointOfSale,
        FiscalHomologationReceiver receiver, InvoiceIdentity? associatedInvoice)
    {
        if (frozen.Service != Service || frozen.CanonicalVersion != 1 || frozen.Identity.Environment != ArcaEnvironment.Homologation ||
            frozen.Identity.PointOfSale != pointOfSale || frozen.Identity.VoucherType != scenario.VoucherType)
            throw new InvoiceConflictException("Frozen WSMTXCA fiscal identity differs from the selected case.");
        ComprobanteType request = Deserialize<ComprobanteType>(frozen.Payload);
        ComprobanteType expected = CreateRequest(pointOfSale, scenario.VoucherType, frozen.Identity.VoucherNumber,
            ReceiverFor(scenario.VoucherType), associatedInvoice, request.FechaEmision is { } date ? DateOnly.FromDateTime(date) : throw new InvoiceConflictException("Frozen WSMTXCA issue date is missing."));
        VerifyRequestEquivalent(expected, request);
    }

    public async Task<bool> IsVoucherTypeAvailableAsync(ArcaTenantContext tenant, int voucherType, CancellationToken cancellationToken)
    {
        var response = await service.consultarTiposComprobanteAsync(tenant,
            new ConsultarTiposComprobanteRequestType(), cancellationToken).ConfigureAwait(false);
        if (!response.ArrayTiposComprobante.Any(entry => entry?.Codigo == voucherType)) return false;
        FiscalHomologationReceiver receiver = ReceiverFor(voucherType);
        var conditions = await service.consultarCondicionesIVAReceptorAsync(tenant,
            new ConsultarCondicionesIvaReceptorRequestType
            {
                ConsultaCondicionesIvaReceptorRequest = new ConsultaCondicionesIvaRequestType
                {
                    CodigoTipoComprobante = checked((short)voucherType)
                }
            }, cancellationToken).ConfigureAwait(false);
        if (!conditions.ArrayCondicionesIvaReceptor.Any(entry => entry?.Codigo == receiver.VatConditionId))
            throw new InvalidOperationException("WSMTXCA receiver VAT condition is absent from the runtime catalogue.");
        int pointOfSale = configuredPointOfSale
            ?? throw new InvalidOperationException("WSMTXCA point of sale must be validated before catalogue lookup.");
        var points = await service.consultarPuntosVentaCAEAsync(tenant,
            new ConsultarPuntosVentaCaeRequestType(), cancellationToken).ConfigureAwait(false);
        bool fixedQaPoint = IsFixedQaPointOfSaleAllowed(points, tenant.Environment, pointOfSale,
            allowFixedQaPointOfSaleForEmptyCatalogue);
        if (!IsActiveCaePointOfSale(points, pointOfSale) && !fixedQaPoint)
            throw new InvalidOperationException("WSMTXCA configured point of sale is not active for CAE issuance.");
        return true;
    }

    internal static bool IsActiveCaePointOfSale(ConsultarPuntosVentaCaeResponse? response, int pointOfSale)
    {
        PuntoVentaType? selected = response?.ArrayPuntosVenta.SingleOrDefault(entry => entry.NumeroPuntoVenta == pointOfSale);
        return selected is not null && selected.Bloqueado == SiNoSimpleType.N && selected.FechaBaja is null;
    }

    internal static bool IsFixedQaPointOfSaleAllowed(ConsultarPuntosVentaCaeResponse? response,
        ArcaEnvironment environment, int pointOfSale, bool enabled)
        => enabled && environment == ArcaEnvironment.Homologation && pointOfSale == 2 &&
            response is { ArrayPuntosVenta.Count: 0, Evento: null };

    public async Task<long> QueryLastAuthorizedAsync(ArcaTenantContext tenant, int pointOfSale, int voucherType, CancellationToken cancellationToken)
    {
        var response = await service.consultarUltimoComprobanteAutorizadoAsync(tenant, new ConsultarUltimoComprobanteAutorizadoRequestType
        {
            ConsultaUltimoComprobanteAutorizadoRequest = new ConsultaUltimoComprobanteAutorizadoRequestType
            { CodigoTipoComprobante = checked((short)voucherType), NumeroPuntoVenta = pointOfSale }
        }, cancellationToken).ConfigureAwait(false);
        return ReadLastAuthorizedNumber(response, tenant.Environment);
    }

    internal static long ReadLastAuthorizedNumber(ConsultarUltimoComprobanteAutorizadoResponseType response,
        ArcaEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.NumeroComprobante is { } number)
        {
            if (response.ArrayErrores.Count != 0 || response.Evento is not null || number is < 0 or > 99999999)
                throw new InvalidOperationException("WSMTXCA returned contradictory or invalid last-authorized voucher data.");
            return number;
        }

        bool emptySeries = environment == ArcaEnvironment.Homologation && response.ArrayErrores.Count == 1 &&
            response.ArrayErrores[0].Codigo == 1502 && response.Evento is null;
        if (emptySeries) return 0;
        throw new InvalidOperationException("WSMTXCA returned an invalid last-authorized voucher response.");
    }

    public InvoiceSubmission CreateSubmission(ArcaTenantContext tenant, string key, int pointOfSale, int voucherType,
        long number, FiscalHomologationReceiver receiver, InvoiceIdentity? associatedInvoice)
    {
        var date = DateOnly.FromDateTime(Clock.GetUtcNow().ToOffset(TimeSpan.FromHours(-3)).Date);
        var request = CreateRequest(pointOfSale, voucherType, number, ReceiverFor(voucherType), associatedInvoice, date);
        return SafeInvoiceService.CreateWsmtxcaSubmission(tenant, key, request);
    }
    public Task<InvoiceOperation> SubmitOrResumeAsync(ArcaTenantContext tenant, string key, InvoiceSubmission frozen, CancellationToken cancellationToken)
        => invoices.ResumeAsync(tenant, key, cancellationToken);
    public Task<InvoiceOperation> ReconcileAsync(ArcaTenantContext tenant, string key, CancellationToken cancellationToken)
        => invoices.ReconcileAsync(tenant, key, cancellationToken);

    public async Task VerifyExactAsync(ArcaTenantContext tenant, InvoiceOperation operation, CancellationToken cancellationToken)
    {
        InvoiceIdentity id = operation.Submission.Identity;
        ComprobanteType expected = Deserialize<ComprobanteType>(operation.Submission.Payload);
        var response = await service.consultarComprobanteAsync(tenant, new ConsultarComprobanteRequestType
        { ConsultaComprobanteRequest = new ConsultaComprobanteRequestType
            { CodigoTipoComprobante = checked((short)id.VoucherType), NumeroPuntoVenta = id.PointOfSale, NumeroComprobante = id.VoucherNumber } }, cancellationToken).ConfigureAwait(false);
        ComprobanteType? actual = response.Comprobante;
        if (actual is null || actual.CodigoTipoAutorizacion?.ToString() != "A" || actual.CodigoAutorizacion is null or <= 0 ||
            actual.NumeroPuntoVenta != id.PointOfSale || actual.CodigoTipoComprobante != id.VoucherType ||
            actual.NumeroComprobante != id.VoucherNumber || actual.FechaEmision != expected.FechaEmision)
            throw new InvalidOperationException("WSMTXCA exact consultation did not confirm the selected authorized voucher.");
        VerifyExactProjection(expected, actual);
        if (operation.AuthorizationCode != actual.CodigoAutorizacion.Value.ToString(CultureInfo.InvariantCulture))
            throw new InvalidOperationException("WSMTXCA exact consultation authorization differs from the stored journal result.");
    }

    internal static ComprobanteType CreateRequest(int pointOfSale, int voucherType, long number, FiscalHomologationReceiver receiver,
        InvoiceIdentity? associatedInvoice, DateOnly issueDate)
    {
        if (voucherType is not (1 or 2 or 3 or 6 or 7 or 8)) throw new ArgumentOutOfRangeException(nameof(voucherType));
        bool isA = voucherType is 1 or 2 or 3;
        ValidateReceiver(receiver, isA);
        if ((voucherType is 2 or 3 or 7 or 8) != (associatedInvoice is not null))
            throw new ArgumentException("WSMTXCA notes require their verified invoice association.", nameof(associatedInvoice));
        short invoiceType = voucherType switch { 2 or 3 => 1, 7 or 8 => 6, _ => checked((short)voucherType) };
        if (associatedInvoice is { } assoc && (assoc.PointOfSale != pointOfSale || assoc.VoucherType != invoiceType ||
            assoc.Environment != ArcaEnvironment.Homologation || assoc.VoucherNumber <= 0))
            throw new ArgumentException("WSMTXCA association must identify the verified same-family invoice in this point of sale.", nameof(associatedInvoice));
        const decimal quantity = 2m, net = 100m, vat = 21m;
        decimal unitPrice = isA ? 50m : 60.5m;
        var result = new ComprobanteType
        {
            CodigoTipoComprobante = checked((short)voucherType), NumeroPuntoVenta = pointOfSale, NumeroComprobante = number,
            FechaEmision = issueDate.ToDateTime(TimeOnly.MinValue), CodigoTipoDocumento = checked((short)receiver.DocumentType),
            NumeroDocumento = receiver.DocumentNumber, CondicionIvaReceptor = checked((short)receiver.VatConditionId!.Value),
            ImporteGravado = net, ImporteNoGravado = 0, ImporteExento = 0, ImporteSubtotal = net,
            ImporteOtrosTributos = 0, ImporteTotal = net + vat, CodigoMoneda = "PES", CotizacionMoneda = 1m,
            CodigoConcepto = 1
        };
        result.ArrayItems.Add(new ItemType
        {
            Codigo = "SYNTHETIC-ITEM", Descripcion = "Synthetic taxable service item", Cantidad = quantity,
            CodigoUnidadMedida = 7, PrecioUnitario = unitPrice, ImporteBonificacion = 0,
            CodigoCondicionIva = 5, ImporteIva = vat, ImporteItem = net + vat
        });
        result.ArraySubtotalesIva.Add(new SubtotalIvaType { Codigo = 5, Importe = vat });
        if (associatedInvoice is { } original)
            result.ArrayComprobantesAsociados.Add(new ComprobanteAsociadoType
            { CodigoTipoComprobante = checked((short)original.VoucherType), NumeroPuntoVenta = original.PointOfSale, NumeroComprobante = original.VoucherNumber });
        return result;
    }

    internal static ComprobanteType CreateQueryProjection(ComprobanteType request, long authorizationCode)
    {
        ComprobanteType projected = Clone(request);
        projected.CodigoAutorizacion = authorizationCode;
        projected.CodigoTipoAutorizacion = CodigoTipoAutorizacionSimpleType.A;
        projected.FechaVencimiento = request.FechaEmision?.AddDays(10);
        return projected;
    }

    internal static void VerifyExactProjection(ComprobanteType expected, ComprobanteType actual)
    {
        ComprobanteType normalized = Clone(actual);
        normalized.CodigoAutorizacion = null; normalized.CodigoTipoAutorizacion = null; normalized.FechaVencimiento = null;
        if (!XmlEqual(expected, normalized)) throw new InvalidOperationException("WSMTXCA authorized query differs from the frozen itemized request.");
    }

    private FiscalHomologationReceiver ReceiverFor(int type) => type is 1 or 2 or 3 ? registeredTaxpayerReceiver : consumerFinalReceiver;
    private static void ValidateReceiver(FiscalHomologationReceiver receiver, bool registered)
    {
        int expectedCondition = registered ? 1 : 5;
        if (receiver is null || !receiver.FixtureReviewed || receiver.VatConditionId != expectedCondition)
            throw new InvalidOperationException("WSMTXCA requires the family-specific receiver VAT condition (A=1, B=5).");
        if (registered && (receiver.DocumentType != 80 || !FiscalHomologationWsfeChannel.IsValidCuit(receiver.DocumentNumber)))
            throw new InvalidOperationException("WSMTXCA A requires a valid eleven-digit CUIT receiver (document type 80, condition 1).");
        if (!registered && (receiver.DocumentType != 99 || receiver.DocumentNumber != 0))
            throw new InvalidOperationException("WSMTXCA B requires a final-consumer receiver (type 99, number 0, condition 5).");
    }
    private static ComprobanteType Deserialize<T>(string payload) where T : class
    {
        using var reader = new StringReader(payload);
        return (ComprobanteType)(new XmlSerializer(typeof(ComprobanteType)).Deserialize(reader) ?? throw new InvoiceConflictException("Frozen WSMTXCA payload is invalid."));
    }
    private static void VerifyRequestEquivalent(ComprobanteType expected, ComprobanteType actual)
    {
        if (!XmlEqual(expected, actual)) throw new InvoiceConflictException("Frozen WSMTXCA request differs from reviewed receiver or association configuration.");
    }
    private static ComprobanteType Clone(ComprobanteType value)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var serializer = new XmlSerializer(typeof(ComprobanteType)); serializer.Serialize(writer, value);
        using var reader = new StringReader(writer.ToString());
        return (ComprobanteType)(serializer.Deserialize(reader) ?? throw new InvalidOperationException("Unable to clone WSMTXCA contract DTO."));
    }
    private static bool XmlEqual(ComprobanteType left, ComprobanteType right)
    {
        string Write(ComprobanteType value) { using var writer = new StringWriter(CultureInfo.InvariantCulture); new XmlSerializer(typeof(ComprobanteType)).Serialize(writer, value); return writer.ToString(); }
        return XNode.DeepEquals(XElement.Parse(Write(left)), XElement.Parse(Write(right)));
    }
}
