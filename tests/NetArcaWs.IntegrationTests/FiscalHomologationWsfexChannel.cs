using System.Globalization;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Collections;
using System.Reflection;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;

namespace NetArcaWs.IntegrationTests;

internal sealed record FiscalExportItem(string Code, string Description, decimal Quantity, decimal UnitPrice, decimal Discount);
internal sealed record FiscalExportFixture(short DestinationCountryCode, long CustomerCountryTaxCode, string CustomerName,
    string CustomerAddress, string ForeignTaxId, string CurrencyId, decimal CurrencyRate, string PaymentTerms,
    DateOnly PaymentDate, short InvoiceLanguage, IReadOnlyList<FiscalExportItem> Items)
{
    public void Validate()
    {
        if (DestinationCountryCode <= 0 || CustomerCountryTaxCode <= 0 || string.IsNullOrWhiteSpace(CustomerName) ||
            string.IsNullOrWhiteSpace(CustomerAddress) || string.IsNullOrWhiteSpace(ForeignTaxId) ||
            string.IsNullOrWhiteSpace(CurrencyId) || CurrencyRate <= 0 || string.IsNullOrWhiteSpace(PaymentTerms) ||
            PaymentDate == default || InvoiceLanguage <= 0 || Items is null || Items.Count == 0 ||
            Items.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Description) || x.Quantity <= 0 || x.UnitPrice <= 0 || x.Discount < 0 || x.Discount >= x.UnitPrice * x.Quantity))
            throw new InvalidOperationException("WSFEX requires explicit destination, foreign buyer identity, payment, currency, language and positive item fixtures.");
    }
}

internal sealed class FiscalHomologationWsfexChannel(
    IWsfexv1Service service,
    SafeInvoiceService invoices,
    FiscalExportFixture fixture,
    TimeProvider? clock = null,
    bool allowFixedQaPointOfSaleForEmptyCatalogue = false) : IFiscalHomologationChannel
{
    private long? lastObservedExportId;
    private int? configuredPointOfSale;
    private readonly object exportIdGate = new();
    public string Service => "wsfex";
    private TimeProvider Clock { get; } = clock ?? TimeProvider.System;
    public bool Supports(int voucherType) => voucherType is 19 or 20 or 21;
    public bool CanAssociate(int noteType, int originalType) => noteType is 20 or 21 && originalType == 19;

    public void ValidateScenario(FiscalHomologationScenario scenario, int pointOfSale, FiscalHomologationReceiver receiver)
    {
        if (!Supports(scenario.VoucherType)) throw new InvalidOperationException("WSFEX matrix supports export invoice/debit/credit types 19/20/21 only.");
        fixture.Validate();
        if (pointOfSale is <= 0 or > 99999 || (configuredPointOfSale is not null && configuredPointOfSale != pointOfSale))
            throw new InvalidOperationException("WSFEX channel requires one explicit configured export point of sale.");
        configuredPointOfSale = pointOfSale;
        if (receiver is null || !receiver.FixtureReviewed) throw new InvalidOperationException("A reviewed export customer fixture must be selected by the caller.");
    }

    public void ValidateFrozenSubmission(InvoiceSubmission frozen, FiscalHomologationScenario scenario, int pointOfSale,
        FiscalHomologationReceiver receiver, InvoiceIdentity? associatedInvoice)
    {
        if (frozen.Service != Service || frozen.CanonicalVersion != 1 || frozen.Identity.Environment != ArcaEnvironment.Homologation ||
            frozen.Identity.PointOfSale != pointOfSale || frozen.Identity.VoucherType != scenario.VoucherType ||
            frozen.RemoteRequestId is not > 0)
            throw new InvoiceConflictException("Frozen WSFEX fiscal identity or service request Id differs from the selected case.");
        ClsFexRequest request = Deserialize(frozen.Payload);
        if (request.Id != frozen.RemoteRequestId || request.CbteNro != frozen.Identity.VoucherNumber ||
            request.PuntoVta != pointOfSale || request.CbteTipo != scenario.VoucherType)
            throw new InvoiceConflictException("Frozen WSFEX request identity differs from its durable journal identity.");
        ClsFexRequest expected = CreateRequest(pointOfSale, scenario.VoucherType, frozen.Identity.VoucherNumber, request.Id,
            fixture, associatedInvoice, DateOnly.ParseExact(request.FechaCbte, "yyyyMMdd", CultureInfo.InvariantCulture), frozen.Identity.Cuit);
        VerifyRequestEquivalent(expected, request);
    }

    public async Task<bool> IsVoucherTypeAvailableAsync(ArcaTenantContext tenant, int voucherType, CancellationToken cancellationToken)
    {
        var response = await service.FEXGetPARAM_Cbte_TipoAsync(tenant, new FexGetParamCbteTipo(), cancellationToken).ConfigureAwait(false);
        var result = response.FexGetParamCbteTipoResult ?? throw new InvalidOperationException("WSFEX returned no voucher type catalogue.");
        if (result.FexErr is { ErrCode: not 0 }) throw new InvalidOperationException("WSFEX returned a voucher catalogue error.");
        var exportTypes = await service.FEXGetPARAM_Tipo_ExpoAsync(tenant, new FexGetParamTipoExpo(), cancellationToken).ConfigureAwait(false);
        var destinations = await service.FEXGetPARAM_DST_paisAsync(tenant, new FexGetParamDstPais(), cancellationToken).ConfigureAwait(false);
        var buyerCountries = await service.FEXGetPARAM_DST_CUITAsync(tenant, new FexGetParamDstCuit(), cancellationToken).ConfigureAwait(false);
        var currencies = await service.FEXGetPARAM_MONAsync(tenant, new FexGetParamMon(), cancellationToken).ConfigureAwait(false);
        var languages = await service.FEXGetPARAM_IdiomasAsync(tenant, new FexGetParamIdiomas(), cancellationToken).ConfigureAwait(false);
        var units = await service.FEXGetPARAM_UMedAsync(tenant, new FexGetParamUMed(), cancellationToken).ConfigureAwait(false);
        var points = await service.FEXGetPARAM_PtoVentaAsync(tenant, new FexGetParamPtoVenta(), cancellationToken).ConfigureAwait(false);
        var exportResult = exportTypes.FexGetParamTipoExpoResult ?? throw new InvalidOperationException("WSFEX returned no export-type catalogue.");
        var destinationResult = destinations.FexGetParamDstPaisResult ?? throw new InvalidOperationException("WSFEX returned no destination-country catalogue.");
        var buyerResult = buyerCountries.FexGetParamDstCuitResult ?? throw new InvalidOperationException("WSFEX returned no buyer-country-code catalogue.");
        var currencyResult = currencies.FexGetParamMonResult ?? throw new InvalidOperationException("WSFEX returned no currency catalogue.");
        var languageResult = languages.FexGetParamIdiomasResult ?? throw new InvalidOperationException("WSFEX returned no language catalogue.");
        var unitResult = units.FexGetParamUMedResult ?? throw new InvalidOperationException("WSFEX returned no unit-of-measure catalogue.");
        var pointResult = points.FexGetParamPtoVentaResult ?? throw new InvalidOperationException("WSFEX returned no point-of-sale catalogue.");
        if (exportResult.FexErr is { ErrCode: not 0 } || destinationResult.FexErr is { ErrCode: not 0 } ||
            buyerResult.FexErr is { ErrCode: not 0 } || currencyResult.FexErr is { ErrCode: not 0 } ||
            languageResult.FexErr is { ErrCode: not 0 } || unitResult.FexErr is { ErrCode: not 0 } || pointResult.FexErr is { ErrCode: not 0 })
            throw new InvalidOperationException("WSFEX returned an error while validating export fixture catalogues.");
        int pos = configuredPointOfSale ?? throw new InvalidOperationException("WSFEX point of sale was not validated before catalogue lookup.");
        var selectedPoint = pointResult.FexResultGet.SingleOrDefault(x => x?.PveNro == pos);
        bool fixedQaPoint = IsFixedQaPointOfSaleAllowed(pointResult, tenant.Environment, pos,
            allowFixedQaPointOfSaleForEmptyCatalogue);
        return result.FexResultGet.Any(x => x?.CbteId == voucherType) &&
            exportResult.FexResultGet.Any(x => x?.TexId == 2) &&
            destinationResult.FexResultGet.Any(x => x?.DstCodigo == fixture.DestinationCountryCode.ToString(CultureInfo.InvariantCulture)) &&
            buyerResult.FexResultGet.Any(x => x?.DstCuit == fixture.CustomerCountryTaxCode) &&
            currencyResult.FexResultGet.Any(x => x?.MonId == fixture.CurrencyId) &&
            languageResult.FexResultGet.Any(x => x?.IdiId == fixture.InvoiceLanguage) &&
            unitResult.FexResultGet.Any(x => x?.UmedId == 7) && (fixedQaPoint || selectedPoint is not null &&
            string.Equals(selectedPoint.PveBloqueado, "N", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(selectedPoint.PveFchBaja) || selectedPoint.PveFchBaja == "00000000"));
    }

    internal static bool IsFixedQaPointOfSaleAllowed(FexResponsePtoVenta? response, ArcaEnvironment environment,
        int pointOfSale, bool enabled)
        => enabled && environment == ArcaEnvironment.Homologation && pointOfSale == 1 &&
            response is { FexResultGet.Count: 0 } && response.FexErr is null or { ErrCode: 0 };

    public async Task<long> QueryLastAuthorizedAsync(ArcaTenantContext tenant, int pointOfSale, int voucherType, CancellationToken cancellationToken)
    {
        var response = await service.FEXGetLast_CMPAsync(tenant, new FexGetLastCmp
        { Auth = new ClsFexLastCmp { Cuit = tenant.Cuit, PtoVenta = pointOfSale, CbteTipo = checked((short)voucherType) } }, cancellationToken).ConfigureAwait(false);
        var result = response.FexGetLastCmpResult ?? throw new InvalidOperationException("WSFEX returned no last voucher response.");
        if (result.FexErr is { ErrCode: not 0 } || result.FexResultLastCmp is null || result.FexResultLastCmp.CbteNro < 0 || result.FexResultLastCmp.CbteNro > 99999999)
            throw new InvalidOperationException("WSFEX returned an invalid last voucher response.");
        var idResponse = await service.FEXGetLast_IDAsync(tenant,
            new FexGetLastId { Auth = new ClsFexAuthRequest { Cuit = tenant.Cuit } }, cancellationToken).ConfigureAwait(false);
        var idResult = idResponse.FexGetLastIdResult ?? throw new InvalidOperationException("WSFEX returned no last request Id response.");
        if (idResult.FexErr is { ErrCode: not 0 } || idResult.FexResultGet is null || idResult.FexResultGet.Id < 0 || idResult.FexResultGet.Id == long.MaxValue)
            throw new InvalidOperationException("WSFEX returned an invalid last request Id.");
        lock (exportIdGate) lastObservedExportId = idResult.FexResultGet.Id;
        return result.FexResultLastCmp.CbteNro;
    }

    public InvoiceSubmission CreateSubmission(ArcaTenantContext tenant, string key, int pointOfSale, int voucherType,
        long number, FiscalHomologationReceiver receiver, InvoiceIdentity? associatedInvoice)
    {
        long lastId;
        lock (exportIdGate) lastId = lastObservedExportId ?? throw new InvalidOperationException("WSFEX last request Id must be queried before selecting a frozen request Id.");
        long nextId = checked(lastId + 1);
        if (nextId <= 0) throw new InvalidOperationException("WSFEX next request Id is outside the positive supported range.");
        DateOnly date = DateOnly.FromDateTime(Clock.GetUtcNow().ToOffset(TimeSpan.FromHours(-3)).Date);
        ClsFexRequest request = CreateRequest(pointOfSale, voucherType, number, nextId, fixture, associatedInvoice, date, tenant.Cuit);
        return SafeInvoiceService.CreateWsfexSubmission(tenant, key, request);
    }

    public async Task<InvoiceOperation> SubmitOrResumeAsync(ArcaTenantContext tenant, string key, InvoiceSubmission frozen,
        CancellationToken cancellationToken)
    {
        long observed;
        lock (exportIdGate) observed = lastObservedExportId ?? throw new InvalidOperationException("WSFEX Id sequence must be queried immediately before authorization.");
        if (frozen.RemoteRequestId != checked(observed + 1))
            throw new InvoiceConflictException("Frozen WSFEX request Id is no longer the next service Id; the number and Id remain reserved.");
        return await invoices.ResumeAsync(tenant, key, cancellationToken).ConfigureAwait(false);
    }
    public Task<InvoiceOperation> ReconcileAsync(ArcaTenantContext tenant, string key, CancellationToken cancellationToken)
        => invoices.ReconcileAsync(tenant, key, cancellationToken);

    public async Task VerifyExactAsync(ArcaTenantContext tenant, InvoiceOperation operation, CancellationToken cancellationToken)
    {
        InvoiceIdentity id = operation.Submission.Identity;
        ClsFexRequest expected = Deserialize(operation.Submission.Payload);
        var response = await service.FEXGetCMPAsync(tenant, new FexGetCmp
        { Cmp = new ClsFexGetCmp { PuntoVta = id.PointOfSale, CbteTipo = checked((short)id.VoucherType), CbteNro = id.VoucherNumber } }, cancellationToken).ConfigureAwait(false);
        ClsFexGetCmpr? actual = response.FexGetCmpResult?.FexResultGet;
        if (actual is null || actual.Resultado != "A" || actual.Id != operation.Submission.RemoteRequestId ||
            actual.PuntoVta != id.PointOfSale || actual.CbteTipo != id.VoucherType || actual.CbteNro != id.VoucherNumber ||
            actual.Cae != operation.AuthorizationCode)
            throw new InvalidOperationException("WSFEX exact consultation did not confirm the stored authorized export voucher.");
        VerifyExactProjection(expected, actual);
    }

    internal static ClsFexRequest CreateRequest(int pointOfSale, int voucherType, long number, long requestId,
        FiscalExportFixture fixture, InvoiceIdentity? association, DateOnly issueDate, long issuerCuit)
    {
        fixture.Validate();
        if (pointOfSale is <= 0 or > 99999 || number is <= 0 or > 99999999) throw new ArgumentOutOfRangeException(nameof(number));
        if (voucherType is not (19 or 20 or 21)) throw new ArgumentOutOfRangeException(nameof(voucherType));
        if (requestId <= 0 || requestId == long.MaxValue) throw new ArgumentOutOfRangeException(nameof(requestId));
        if ((voucherType is 20 or 21) != (association is not null)) throw new ArgumentException("WSFEX debit and credit notes require their verified invoice association.", nameof(association));
        if (association is { } original && (original.Environment != ArcaEnvironment.Homologation || original.VoucherType != 19 || original.PointOfSale != pointOfSale || original.VoucherNumber <= 0))
            throw new ArgumentException("WSFEX association must identify the verified export invoice from this point of sale.", nameof(association));
        var request = new ClsFexRequest
        {
            Id = requestId, FechaCbte = issueDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture), CbteTipo = checked((short)voucherType),
            PuntoVta = pointOfSale, CbteNro = number, TipoExpo = 2, DstCmp = fixture.DestinationCountryCode,
            PermisoExistente = string.Empty,
            Cliente = fixture.CustomerName, CuitPaisCliente = fixture.CustomerCountryTaxCode, DomicilioCliente = fixture.CustomerAddress,
            IdImpositivo = fixture.ForeignTaxId, MonedaId = fixture.CurrencyId, MonedaCtz = fixture.CurrencyRate,
            FormaPago = fixture.PaymentTerms,
            FechaPago = voucherType == 19 ? fixture.PaymentDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture) : null,
            IdiomaCbte = fixture.InvoiceLanguage, ImpTotal = fixture.Items.Sum(x => x.Quantity * x.UnitPrice - x.Discount)
        };
        foreach (FiscalExportItem item in fixture.Items)
            request.Items.Add(new Item { ProCodigo = item.Code, ProDs = item.Description, ProQty = item.Quantity,
                ProUmed = 7, ProPrecioUni = item.UnitPrice, ProBonificacion = item.Discount,
                ProTotalItem = item.Quantity * item.UnitPrice - item.Discount });
        if (association is { } invoice)
            request.CmpsAsoc.Add(new CmpAsoc { CbteTipo = checked((short)invoice.VoucherType), CbtePuntoVta = invoice.PointOfSale,
                CbteNro = invoice.VoucherNumber, CbteCuit = issuerCuit });
        return request;
    }

    internal static ClsFexGetCmpr CreateQueryProjection(ClsFexRequest request, string result = "A", string cae = "99999999999999")
    {
        var projected = new ClsFexGetCmpr();
        CopyRequestFields(request, projected);
        projected.Resultado = result; projected.Cae = cae;
        return projected;
    }

    internal static void VerifyExactProjection(ClsFexRequest expected, ClsFexGetCmpr actual)
    {
        var projected = new ClsFexRequest();
        CopyQueryFields(actual, projected);
        XElement expectedXml = ParseWsfexProjection(Serialize(expected));
        XElement actualXml = ParseWsfexProjection(Serialize(projected));
        bool allowIncoterms = expected.CbteTipo != 19 || expected.TipoExpo != 1;
        bool allowIncotermsDescriptionZero = expected.CbteTipo is 19 or 20 or 21 && actual.CbteTipo == expected.CbteTipo &&
            expected.TipoExpo is 2 or 4 && IsEmptyOrNull(expected.Incoterms) && IsEmptyOrNull(expected.IncotermsDs) &&
            IsEmptyOrNull(actual.Incoterms) && string.Equals(actual.IncotermsDs, "0", StringComparison.Ordinal);
        RemoveExactEmptyProjectionOptionals(expectedXml, allowIncoterms, allowIncotermsDescriptionZero);
        RemoveExactEmptyProjectionOptionals(actualXml, allowIncoterms, allowIncotermsDescriptionZero, removeZeroDescription: allowIncotermsDescriptionZero);
        if (!XNode.DeepEquals(expectedXml, actualXml))
            throw new InvalidOperationException("WSFEX authorized query differs from the frozen export request.");
    }

    private static bool IsEmptyOrNull(string? value) => value is null or "";

    private static void RemoveExactEmptyProjectionOptionals(XElement root, bool allowIncoterms,
        bool allowIncotermsDescriptionZero, bool removeZeroDescription = false)
    {
        XNamespace expectedNamespace = "http://ar.gov.afip.dif.fexv1/";
        string[] names = allowIncoterms
            ? allowIncotermsDescriptionZero ? ["Obs_comerciales", "Obs", "Incoterms", "Incoterms_Ds"] : ["Obs_comerciales", "Obs", "Incoterms"]
            : ["Obs_comerciales", "Obs"];
        root.Elements().Where(element => element.Name.Namespace == expectedNamespace &&
            names.Contains(element.Name.LocalName, StringComparer.Ordinal) && !element.HasElements &&
            element.Value.Length == 0 && !element.Attributes().Any(attribute => !attribute.IsNamespaceDeclaration))
            .Remove();
        if (!removeZeroDescription) return;
        root.Elements().Where(element => element.Name == expectedNamespace + "Incoterms_Ds" && !element.HasElements &&
            element.Value == "0" && !element.Attributes().Any(attribute => !attribute.IsNamespaceDeclaration))
            .Remove();
    }

    private static XElement ParseWsfexProjection(string xml)
    {
        XElement root = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
        foreach (XElement parent in root.DescendantsAndSelf().Where(element => element.HasElements).ToArray())
            parent.Nodes().OfType<XText>().Where(text => string.IsNullOrWhiteSpace(text.Value)).Remove();
        return root;
    }

    private static void VerifyRequestEquivalent(ClsFexRequest expected, ClsFexRequest actual)
    {
        if (!XNode.DeepEquals(XElement.Parse(Serialize(expected)), XElement.Parse(Serialize(actual))))
            throw new InvoiceConflictException("Frozen WSFEX request differs from the reviewed export fixture or verified association.");
    }
    private static ClsFexRequest Deserialize(string payload)
    {
        using var reader = new StringReader(payload);
        return (ClsFexRequest)(new XmlSerializer(typeof(ClsFexRequest)).Deserialize(reader) ?? throw new InvoiceConflictException("Frozen WSFEX payload is invalid."));
    }
    private static string Serialize(object value)
    { using var writer = new StringWriter(CultureInfo.InvariantCulture); new XmlSerializer(value.GetType()).Serialize(writer, value); return writer.ToString(); }
    private static void CopyRequestFields(object source, object target)
    {
        foreach (PropertyInfo sourceProperty in source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            PropertyInfo? targetProperty = target.GetType().GetProperty(sourceProperty.Name, BindingFlags.Instance | BindingFlags.Public);
            if (targetProperty is null) continue;
            object? sourceValue = sourceProperty.GetValue(source);
            MethodInfo? setter = targetProperty.GetSetMethod();
            if (setter is not null)
            {
                Type sourceType = Nullable.GetUnderlyingType(sourceProperty.PropertyType) ?? sourceProperty.PropertyType;
                Type targetType = Nullable.GetUnderlyingType(targetProperty.PropertyType) ?? targetProperty.PropertyType;
                if (sourceValue is null) { setter.Invoke(target, [null]); continue; }
                if (sourceType == targetType)
                {
                    object value = targetType.IsInstanceOfType(sourceValue) ? sourceValue : Convert.ChangeType(sourceValue, targetType, CultureInfo.InvariantCulture);
                    setter.Invoke(target, [value]);
                    continue;
                }
            }
            if (sourceProperty.PropertyType != targetProperty.PropertyType) continue;
            if (sourceValue is not IEnumerable sourceItems || targetProperty.GetValue(target) is not IList targetItems) continue;
            foreach (object? item in sourceItems) targetItems.Add(item is null ? null : CloneContractValue(item));
        }
    }
    private static object CloneContractValue(object value)
    {
        string xml = Serialize(value);
        using var reader = new StringReader(xml);
        return new XmlSerializer(value.GetType()).Deserialize(reader) ?? throw new InvalidOperationException("Unable to clone WSFEX nested contract value.");
    }
    private static void CopyQueryFields(ClsFexGetCmpr source, ClsFexRequest target) => CopyRequestFields(source, target);
}
