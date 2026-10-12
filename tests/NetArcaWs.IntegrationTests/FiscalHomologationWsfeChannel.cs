using System.Globalization;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;

namespace NetArcaWs.IntegrationTests;

internal sealed class FiscalHomologationWsfeChannel(
    IWsfev1Service service,
    SafeInvoiceService invoices,
    FiscalHomologationReceiver registeredTaxpayerReceiver,
    FiscalHomologationReceiver consumerFinalReceiver,
    TimeProvider? clock = null) : IFiscalHomologationChannel
{
    private int? configuredPointOfSale;
    public string Service => "wsfe";
    private TimeProvider Clock { get; } = clock ?? TimeProvider.System;
    public bool Supports(int voucherType) => voucherType is 1 or 2 or 3 or 6 or 7 or 8 or 11 or 12 or 13;
    public bool CanAssociate(int noteType, int originalType) => (noteType, originalType) switch
    {
        (2 or 3, 1) or (7 or 8, 6) or (12 or 13, 11) => true,
        _ => false
    };

    public void ValidateScenario(FiscalHomologationScenario scenario, int pointOfSale, FiscalHomologationReceiver receiver)
    {
        if (!Supports(scenario.VoucherType)) throw new InvalidOperationException("WSFE matrix supports only the selected A/B/C invoice and note types.");
        FiscalHomologationReceiver configured = ReceiverFor(scenario.VoucherType);
        ValidateReceiver(configured, scenario.VoucherType is 1 or 2 or 3);
        if (pointOfSale is <= 0 or > 99999 || (configuredPointOfSale is not null && configuredPointOfSale != pointOfSale))
            throw new InvalidOperationException("WSFE channel requires one explicit configured point of sale.");
        configuredPointOfSale = pointOfSale;
        if (receiver is null || !receiver.FixtureReviewed) throw new InvalidOperationException("A reviewed receiver fixture must be selected by the caller.");
    }

    public void ValidateFrozenSubmission(InvoiceSubmission frozen, FiscalHomologationScenario scenario, int pointOfSale,
        FiscalHomologationReceiver receiver, InvoiceIdentity? associatedInvoice)
    {
        RequireIdentity(frozen, scenario, pointOfSale);
        FecaeRequest request = Deserialize<FecaeRequest>(frozen.Payload);
        FecaeRequest expected = CreateRequest(pointOfSale, scenario.VoucherType, frozen.Identity.VoucherNumber,
            ReceiverFor(scenario.VoucherType), associatedInvoice, ParseDate(request.FeDetReq.Single().CbteFch));
        AssertWireRequestEquivalent(expected, request);
    }

    public async Task<bool> IsVoucherTypeAvailableAsync(ArcaTenantContext tenant, int voucherType, CancellationToken cancellationToken)
    {
        var response = await service.FEParamGetTiposCbteAsync(tenant, new FeParamGetTiposCbte(), cancellationToken).ConfigureAwait(false);
        var result = response.FeParamGetTiposCbteResult ?? throw new InvalidOperationException("WSFE returned no voucher type catalogue.");
        if (result.Errors.Count != 0) throw new InvalidOperationException("WSFE returned errors while reading the voucher type catalogue.");
        if (result.ResultGet?.Any(x => x?.Id == voucherType) != true) return false;
        FiscalHomologationReceiver receiver = ReceiverFor(voucherType);
        var conditionResponse = await service.FEParamGetCondicionIvaReceptorAsync(tenant,
            new FeParamGetCondicionIvaReceptor(), cancellationToken).ConfigureAwait(false);
        var conditionResult = conditionResponse.FeParamGetCondicionIvaReceptorResult
            ?? throw new InvalidOperationException("WSFE returned no receiver VAT-condition catalogue.");
        if (conditionResult.Errors.Count != 0 || conditionResult.ResultGet.Any(x => x?.Id == receiver.VatConditionId) != true)
            throw new InvalidOperationException("WSFE receiver VAT condition is absent from the runtime catalogue.");
        int posNumber = configuredPointOfSale ?? throw new InvalidOperationException("WSFE point of sale was not validated before catalogue lookup.");
        var pointResponse = await service.FEParamGetPtosVentaAsync(tenant, new FeParamGetPtosVenta(), cancellationToken).ConfigureAwait(false);
        var points = pointResponse.FeParamGetPtosVentaResult ?? throw new InvalidOperationException("WSFE returned no point-of-sale catalogue.");
        if (points.Errors.Count != 0) throw new InvalidOperationException("WSFE returned errors while reading the point-of-sale catalogue.");
        var selected = points.ResultGet.SingleOrDefault(x => x?.Nro == posNumber);
        return selected is not null && string.Equals(selected.Bloqueado, "N", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(selected.EmisionTipo, "CAE", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(selected.FchBaja) || selected.FchBaja == "00000000");
    }

    public async Task<long> QueryLastAuthorizedAsync(ArcaTenantContext tenant, int pointOfSale, int voucherType, CancellationToken cancellationToken)
    {
        var response = await service.FECompUltimoAutorizadoAsync(tenant,
            new FeCompUltimoAutorizado { PtoVta = pointOfSale, CbteTipo = voucherType }, cancellationToken).ConfigureAwait(false);
        var result = response.FeCompUltimoAutorizadoResult ?? throw new InvalidOperationException("WSFE returned no last-authorized voucher response.");
        if (result.Errors.Count != 0 || result.PtoVta != pointOfSale || result.CbteTipo != voucherType || result.CbteNro < 0)
            throw new InvalidOperationException("WSFE last-authorized response did not match the requested series.");
        return result.CbteNro;
    }

    public InvoiceSubmission CreateSubmission(ArcaTenantContext tenant, string key, int pointOfSale, int voucherType,
        long number, FiscalHomologationReceiver receiver, InvoiceIdentity? associatedInvoice)
    {
        var date = Clock.GetUtcNow().ToOffset(TimeSpan.FromHours(-3)).Date;
        var request = CreateRequest(pointOfSale, voucherType, number, ReceiverFor(voucherType), associatedInvoice, DateOnly.FromDateTime(date));
        return SafeInvoiceService.CreateWsfeSubmission(tenant, key, request);
    }

    public Task<InvoiceOperation> SubmitOrResumeAsync(ArcaTenantContext tenant, string key, InvoiceSubmission frozen,
        CancellationToken cancellationToken) => invoices.ResumeAsync(tenant, key, cancellationToken);
    public Task<InvoiceOperation> ReconcileAsync(ArcaTenantContext tenant, string key, CancellationToken cancellationToken)
        => invoices.ReconcileAsync(tenant, key, cancellationToken);

    public async Task VerifyExactAsync(ArcaTenantContext tenant, InvoiceOperation operation, CancellationToken cancellationToken)
    {
        var id = operation.Submission.Identity;
        FecaeRequest expected = Deserialize<FecaeRequest>(operation.Submission.Payload);
        var response = await service.FECompConsultarAsync(tenant, new FeCompConsultar
        { FeCompConsReq = new FeCompConsultaReq { PtoVta = id.PointOfSale, CbteTipo = id.VoucherType, CbteNro = id.VoucherNumber } }, cancellationToken).ConfigureAwait(false);
        var result = response.FeCompConsultarResult;
        var remote = result?.ResultGet;
        if (result is null || result.Errors.Count != 0 || remote is null || remote.Resultado != "A" || remote.EmisionTipo != "CAE" ||
            remote.CodAutorizacion is not { Length: 14 } cae || !cae.All(char.IsAsciiDigit) || !cae.Any(c => c != '0'))
            throw new InvalidOperationException("WSFE exact consultation did not confirm an authorized voucher.");
        VerifyExactProjection(expected.FeDetReq.Single(), remote);
        if (remote.PtoVta != id.PointOfSale || remote.CbteTipo != id.VoucherType || operation.AuthorizationCode != remote.CodAutorizacion)
            throw new InvalidOperationException("WSFE exact consultation did not match the stored fiscal identity or authorization.");
    }

    internal static FecaeRequest CreateRequest(int pointOfSale, int voucherType, long number, FiscalHomologationReceiver receiver,
        InvoiceIdentity? associatedInvoice, DateOnly issueDate)
    {
        if (voucherType is not (1 or 2 or 3 or 6 or 7 or 8 or 11 or 12 or 13)) throw new ArgumentOutOfRangeException(nameof(voucherType));
        if (pointOfSale is <= 0 or > 99999 || number is <= 0 or > 99999999) throw new ArgumentOutOfRangeException(nameof(number));
        ValidateReceiver(receiver, voucherType is 1 or 2 or 3);
        if ((voucherType is 2 or 3 or 7 or 8 or 12 or 13) != (associatedInvoice is not null))
            throw new ArgumentException("Only a note with its verified same-family invoice may carry an association.", nameof(associatedInvoice));
        int invoiceType = voucherType switch { 2 or 3 => 1, 7 or 8 => 6, 12 or 13 => 11, _ => voucherType };
        if (associatedInvoice is { } assoc && (assoc.PointOfSale != pointOfSale || assoc.VoucherType != invoiceType ||
            assoc.Environment != ArcaEnvironment.Homologation || assoc.VoucherNumber <= 0))
            throw new ArgumentException("WSFE note association must identify a verified invoice in the same family and point of sale.", nameof(associatedInvoice));
        bool hasVat = voucherType is 1 or 2 or 3 or 6 or 7 or 8;
        double net = hasVat ? 100d : 100d;
        double vat = hasVat ? 21d : 0d;
        var detail = new FecaeDetRequest
        {
            Concepto = 1, DocTipo = receiver.DocumentType, DocNro = receiver.DocumentNumber,
            CbteDesde = number, CbteHasta = number, CbteFch = issueDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            ImpTotal = net + vat, ImpTotConc = 0, ImpNeto = net, ImpOpEx = 0, ImpTrib = 0, ImpIva = vat,
            MonId = "PES", MonCotiz = 1d, CondicionIvaReceptorId = receiver.VatConditionId
        };
        if (hasVat) detail.Iva.Add(new AlicIva { Id = 5, BaseImp = net, Importe = vat });
        if (associatedInvoice is { } original) detail.CbtesAsoc.Add(new CbteAsoc
        { Tipo = original.VoucherType, PtoVta = original.PointOfSale, Nro = original.VoucherNumber });
        var request = new FecaeRequest { FeCabReq = new FecaeCabRequest { CantReg = 1, PtoVta = pointOfSale, CbteTipo = voucherType } };
        request.FeDetReq.Add(detail);
        return request;
    }

    internal static FeCompConsResponse CreateQueryProjection(FecaeRequest request, string result, string cae)
    {
        var source = request.FeDetReq.Single();
        var remote = new FeCompConsResponse
        {
            Concepto = source.Concepto, DocTipo = source.DocTipo, DocNro = source.DocNro,
            CbteDesde = source.CbteDesde, CbteHasta = source.CbteHasta, CbteFch = source.CbteFch,
            ImpTotal = source.ImpTotal, ImpTotConc = source.ImpTotConc, ImpNeto = source.ImpNeto,
            ImpOpEx = source.ImpOpEx, ImpTrib = source.ImpTrib, ImpIva = source.ImpIva,
            MonId = source.MonId, MonCotiz = source.MonCotiz, CondicionIvaReceptorId = source.CondicionIvaReceptorId,
            Resultado = result, EmisionTipo = "CAE", CodAutorizacion = cae,
            PtoVta = request.FeCabReq.PtoVta, CbteTipo = request.FeCabReq.CbteTipo
        };
        Copy(source.Iva, remote.Iva, x => new AlicIva { Id = x.Id, BaseImp = x.BaseImp, Importe = x.Importe });
        Copy(source.CbtesAsoc, remote.CbtesAsoc, x => new CbteAsoc { Tipo = x.Tipo, PtoVta = x.PtoVta, Nro = x.Nro, Cuit = x.Cuit, CbteFch = x.CbteFch });
        Copy(source.Tributos, remote.Tributos, x => new Tributo { Id = x.Id, Desc = x.Desc, BaseImp = x.BaseImp, Alic = x.Alic, Importe = x.Importe });
        Copy(source.Opcionales, remote.Opcionales, x => new Opcional { Id = x.Id, Valor = x.Valor });
        Copy(source.Compradores, remote.Compradores, x => new Comprador { DocTipo = x.DocTipo, DocNro = x.DocNro, Porcentaje = x.Porcentaje });
        Copy(source.Actividades, remote.Actividades, x => new Actividad { Id = x.Id });
        return remote;
    }

    internal static void VerifyExactProjection(FecaeDetRequest expected, FeCompConsResponse actual)
    {
        var projected = new FecaeDetRequest
        {
            Concepto = actual.Concepto, DocTipo = actual.DocTipo, DocNro = actual.DocNro, CbteDesde = actual.CbteDesde,
            CbteHasta = actual.CbteHasta, CbteFch = actual.CbteFch, ImpTotal = actual.ImpTotal, ImpTotConc = actual.ImpTotConc,
            ImpNeto = actual.ImpNeto, ImpOpEx = actual.ImpOpEx, ImpTrib = actual.ImpTrib, ImpIva = actual.ImpIva,
            FchServDesde = actual.FchServDesde, FchServHasta = actual.FchServHasta, FchVtoPago = actual.FchVtoPago,
            MonId = actual.MonId, MonCotiz = actual.MonCotiz, CanMisMonExt = actual.CanMisMonExt,
            CondicionIvaReceptorId = actual.CondicionIvaReceptorId, PeriodoAsoc = actual.PeriodoAsoc
        };
        Copy(actual.Iva, projected.Iva, x => new AlicIva { Id = x.Id, BaseImp = x.BaseImp, Importe = x.Importe });
        Copy(actual.CbtesAsoc, projected.CbtesAsoc, x => new CbteAsoc { Tipo = x.Tipo, PtoVta = x.PtoVta, Nro = x.Nro, Cuit = x.Cuit, CbteFch = x.CbteFch });
        Copy(actual.Tributos, projected.Tributos, x => new Tributo { Id = x.Id, Desc = x.Desc, BaseImp = x.BaseImp, Alic = x.Alic, Importe = x.Importe });
        Copy(actual.Opcionales, projected.Opcionales, x => new Opcional { Id = x.Id, Valor = x.Valor });
        Copy(actual.Compradores, projected.Compradores, x => new Comprador { DocTipo = x.DocTipo, DocNro = x.DocNro, Porcentaje = x.Porcentaje });
        Copy(actual.Actividades, projected.Actividades, x => new Actividad { Id = x.Id });
        if (!WireEqual(expected, projected)) throw new InvalidOperationException("WSFE authorized query differs from the frozen request fiscal fields.");
    }

    private FiscalHomologationReceiver ReceiverFor(int voucherType) => voucherType is 1 or 2 or 3 ? registeredTaxpayerReceiver : consumerFinalReceiver;
    private static void ValidateReceiver(FiscalHomologationReceiver receiver, bool registered)
    {
        int expectedCondition = registered ? 1 : 5;
        if (receiver is null || !receiver.FixtureReviewed || receiver.VatConditionId != expectedCondition)
            throw new InvalidOperationException("WSFE requires the family-specific receiver VAT condition (A=1, B/C=5).");
        if (registered && (receiver.DocumentType != 80 || !IsValidCuit(receiver.DocumentNumber)))
            throw new InvalidOperationException("WSFE A requires a valid eleven-digit CUIT receiver (document type 80, condition 1).");
        if (!registered && (receiver.DocumentType != 99 || receiver.DocumentNumber != 0))
            throw new InvalidOperationException("WSFE B/C requires a final-consumer receiver (type 99, number 0, condition 5).");
    }
    internal static bool IsValidCuit(long cuit)
    {
        if (cuit is < 10_000_000_000 or > 99_999_999_999) return false;
        int[] weights = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
        string digits = cuit.ToString(CultureInfo.InvariantCulture);
        int sum = 0;
        for (int i = 0; i < weights.Length; i++) sum += (digits[i] - '0') * weights[i];
        int check = 11 - sum % 11;
        if (check == 11) check = 0;
        else if (check == 10) return false;
        return digits[10] - '0' == check;
    }
    private static void RequireIdentity(InvoiceSubmission submission, FiscalHomologationScenario scenario, int pos)
    {
        if (submission.Service != "wsfe" || submission.Identity.Environment != ArcaEnvironment.Homologation ||
            submission.Identity.PointOfSale != pos || submission.Identity.VoucherType != scenario.VoucherType || submission.CanonicalVersion != 1)
            throw new InvoiceConflictException("Frozen WSFE fiscal identity differs from the selected case.");
    }
    private static DateOnly ParseDate(string value) => DateOnly.ParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture);
    private static T Deserialize<T>(string payload) where T : class
    {
        using var reader = new StringReader(payload);
        return (T)(new System.Xml.Serialization.XmlSerializer(typeof(T)).Deserialize(reader) ?? throw new InvoiceConflictException("Frozen WSFE payload is invalid."));
    }
    private static void AssertWireRequestEquivalent(FecaeRequest expected, FecaeRequest actual)
    {
        if (expected.FeCabReq.PtoVta != actual.FeCabReq?.PtoVta || expected.FeCabReq.CbteTipo != actual.FeCabReq.CbteTipo ||
            expected.FeDetReq.Count != 1 || actual.FeDetReq.Count != 1 || !WireEqual(expected.FeDetReq.Single(), actual.FeDetReq.Single()))
            throw new InvoiceConflictException("Frozen WSFE request differs from the reviewed receiver or association configuration.");
    }
    private static bool WireEqual<T>(T expected, T actual) where T : class
    {
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
        string Save(object value) { using var writer = new StringWriter(CultureInfo.InvariantCulture); serializer.Serialize(writer, value); return writer.ToString(); }
        return System.Xml.Linq.XNode.DeepEquals(System.Xml.Linq.XElement.Parse(Save(expected)), System.Xml.Linq.XElement.Parse(Save(actual)));
    }
    private static void Copy<TSource, TTarget>(IEnumerable<TSource> source, ICollection<TTarget> target, Func<TSource, TTarget> map)
    { foreach (TSource item in source) target.Add(map(item)); }
}
