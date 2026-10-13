using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;

namespace NetArcaWs.Documents;

/// <summary>Creates renderable fiscal documents from the durable operation returned by SafeInvoiceService.</summary>
public sealed class ArcaWsComprobantes(SafeInvoiceService invoices)
{
    public async Task<ComprobanteGenerado> GenerarComprobanteAsync(ArcaTenantContext tenant, string key,
        FecaeRequest request, DatosPresentacion presentacion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presentacion);
        var presentation = Freeze(presentacion);
        InvoiceSubmission submitted = SafeInvoiceService.CreateWsfeSubmission(tenant, key, request);
        ValidateResponse(submitted.Payload);
        FecaeRequest frozen = Deserialize<FecaeRequest>(submitted.Payload);
        WsfeDocumentAdapter.Validate(frozen, presentation);
        InvoiceOperation operation = await invoices.AuthorizeWsfeAsync(tenant, key, frozen, cancellationToken).ConfigureAwait(false);
        return WsfeDocumentAdapter.Map(operation, presentation);
    }

    public async Task<ComprobanteGenerado> GenerarComprobanteAsync(ArcaTenantContext tenant, string key,
        ClsFexRequest request, DatosPresentacion presentacion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presentacion);
        var presentation = Freeze(presentacion);
        InvoiceSubmission submitted = SafeInvoiceService.CreateWsfexSubmission(tenant, key, request);
        ValidateResponse(submitted.Payload);
        ClsFexRequest frozen = Deserialize<ClsFexRequest>(submitted.Payload);
        WsfexDocumentAdapter.Validate(frozen, presentation);
        InvoiceOperation operation = await invoices.AuthorizeWsfexAsync(tenant, key, frozen, cancellationToken).ConfigureAwait(false);
        return WsfexDocumentAdapter.Map(operation, presentation);
    }

    public async Task<ComprobanteGenerado> GenerarComprobanteAsync(ArcaTenantContext tenant, string key,
        ComprobanteType request, DatosPresentacion presentacion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presentacion);
        var presentation = Freeze(presentacion);
        InvoiceSubmission submitted = SafeInvoiceService.CreateWsmtxcaSubmission(tenant, key, request);
        ValidateResponse(submitted.Payload);
        ComprobanteType frozen = Deserialize<ComprobanteType>(submitted.Payload);
        WsmtxcaDocumentAdapter.Validate(frozen, presentation);
        InvoiceOperation operation = await invoices.AuthorizeWsmtxcaAsync(tenant, key, frozen, cancellationToken).ConfigureAwait(false);
        return WsmtxcaDocumentAdapter.Map(operation, presentation);
    }

    internal static DatosPresentacion Freeze(DatosPresentacion source) => new(
        source.Receptor is null ? null : source.Receptor with { },
        source.Detalles.Select(line => line with { }).ToArray(),
        source.OtrosImpuestos.Select(tax => tax with { }).ToArray(),
        source.CondicionIvaReceptor,
        source.Leyendas.ToArray());

    internal static DatosPresentacion WithReceptor(DatosPresentacion source, DatosReceptor receptor) => new(
        receptor, source.Detalles, source.OtrosImpuestos, source.CondicionIvaReceptor, source.Leyendas);

    internal static T Deserialize<T>(string xml)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1_048_576 };
        using var text = new StringReader(xml);
        using XmlReader reader = XmlReader.Create(text, settings);
        return (T)(new XmlSerializer(typeof(T)).Deserialize(reader) ?? throw new InvalidOperationException("The frozen fiscal snapshot is invalid."));
    }

    internal static string? ValidateResponse(string? xml)
    {
        if (xml is null) return null;
        XDocument document;
        try
        {
            using var text = new StringReader(xml);
            using XmlReader reader = XmlReader.Create(text, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1_048_576 });
            document = XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException exception) { throw new InvalidOperationException("The stored ARCA response is not safe XML.", exception); }
        if (document.Descendants().Any(element => element.Name.LocalName.Equals("Auth", StringComparison.OrdinalIgnoreCase) ||
            element.Name.LocalName.Equals("AuthRequest", StringComparison.OrdinalIgnoreCase) ||
            element.Name.LocalName.Equals("Token", StringComparison.OrdinalIgnoreCase) ||
            element.Name.LocalName.Equals("Sign", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("The stored ARCA response contains authentication material and cannot be exposed as a document snapshot.");
        return xml;
    }

    internal static (IReadOnlyList<string> Observations, IReadOnlyList<string> Errors) Messages(string? xml, InvoiceState state)
    {
        var observations = new List<string>();
        var errors = new List<string>();
        if (xml is not null)
        {
            XDocument document = XDocument.Parse(xml);
            foreach (XElement element in document.Descendants())
            {
                string? code = element.Elements().FirstOrDefault(child => IsName(child, "Code", "Codigo", "ErrCode"))?.Value;
                string? message = element.Elements().FirstOrDefault(child => IsName(child, "Msg", "Descripcion", "ErrMsg"))?.Value;
                if (string.IsNullOrWhiteSpace(message)) continue;
                string detail = string.IsNullOrWhiteSpace(code) ? message : $"{code}: {message}";
                if (element.Name.LocalName.Contains("Obs", StringComparison.OrdinalIgnoreCase) ||
                    element.Ancestors().Any(ancestor => ancestor.Name.LocalName.Contains("Observaciones", StringComparison.OrdinalIgnoreCase))) observations.Add(detail);
                else if (element.Name.LocalName.Contains("Err", StringComparison.OrdinalIgnoreCase) ||
                         element.Name.LocalName.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                         element.Elements().Any(child => IsName(child, "Codigo"))) errors.Add(detail);
            }
        }
        if (state == InvoiceState.Rejected && errors.Count == 0) errors.Add("ARCA rechazó la autorización; consulte el snapshot de respuesta para el detalle.");
        if (state is InvoiceState.Unknown or InvoiceState.Conflict or InvoiceState.ManualReview && errors.Count == 0)
            errors.Add("El resultado fiscal requiere reconciliación o revisión manual.");
        return (observations.AsReadOnly(), errors.AsReadOnly());
    }

    internal static ComprobanteGenerado Build(InvoiceOperation operation, InvoiceIdentity identity,
        DatosPresentacion presentation, string currency, decimal total, IReadOnlyList<LineaComprobante> details,
        DateOnly date, decimal exchangeRate, decimal? net = null, decimal? exempt = null, decimal? nonTaxable = null,
        decimal? vat = null, IReadOnlyList<ImpuestoComprobante>? taxes = null, DateOnly? expires = null,
        int? documentType = null, DateOnly? dateFrom = null, DateOnly? dateTo = null,
        DateOnly? paymentDate = null, int? ivaConditionCode = null)
    {
        DatosReceptor receptor = presentation.Receptor ?? throw new ArgumentException("El receptor de la presentación es obligatorio.", nameof(presentation));
        if (documentType is not null && receptor.CodigoTipoDocumento is not null && documentType != receptor.CodigoTipoDocumento)
            throw new ArgumentException("El tipo de documento del receptor no coincide con el snapshot fiscal.", nameof(presentation));
        if (receptor.CodigoTipoDocumento is not null && receptor.Identificacion < 0)
            throw new ArgumentException("La identificación del receptor debe ser no negativa.", nameof(presentation));
        string? response = ValidateResponse(operation.ResponseXml);
        var messages = Messages(response, operation.State);
        receptor = documentType is null || receptor.CodigoTipoDocumento is not null ? receptor : receptor with { CodigoTipoDocumento = documentType };
        var outcome = new ResultadoComprobante(operation.State == InvoiceState.Authorized ? operation.AuthorizationCode : null,
            operation.State == InvoiceState.Authorized ? expires : null, messages.Observations, messages.Errors,
            operation.State == InvoiceState.Authorized ? "A" : operation.State == InvoiceState.Rejected ? "R" : null,
            operation.State == InvoiceState.Authorized ? "E" : null);
        return new ComprobanteGenerado(1, TipoComprobanteGenerado.Fiscal,
            new(operation.Submission.Service, identity.Environment.ToString(), identity.Cuit, identity.PointOfSale, identity.VoucherType, identity.VoucherNumber),
            operation.Submission.TenantId, operation.State.ToString(), receptor, currency, total, details, taxes ?? [],
            presentation.Leyendas, operation.Submission.Payload, operation.Submission.CanonicalVersion, outcome,
            fecha: date, cotizacion: exchangeRate, neto: net, exento: exempt, noGravado: nonTaxable, iva: vat,
            fechaDesde: dateFrom, fechaHasta: dateTo, fechaPago: paymentDate,
            snapshotResponse: response, snapshotResponseVersion: response is null ? null : 1,
            condicionIvaReceptor: presentation.CondicionIvaReceptor,
            codigoCondicionIvaReceptor: ivaConditionCode);
    }

    internal static decimal FromDouble(double value, string field)
    {
        if (!double.IsFinite(value)) throw new InvalidOperationException($"ARCA returned a non-finite {field} value.");
        try { return Convert.ToDecimal(value, CultureInfo.InvariantCulture); }
        catch (OverflowException exception) { throw new InvalidOperationException($"ARCA returned an out-of-range {field} value.", exception); }
    }

    internal static void ValidateSupplemental(DatosPresentacion presentation, string currency, decimal total)
    {
        ComprobanteValidator.ValidateCurrency(currency);
        if (presentation.Detalles.Any(line => line.Importe < 0 || !ComprobanteValidator.SameCurrency(line.Moneda, currency)) ||
            presentation.OtrosImpuestos.Any(tax => tax.Importe < 0 || !ComprobanteValidator.SameCurrency(tax.Moneda, currency)))
            throw new ArgumentException("Los datos suplementarios deben usar la moneda fiscal y no contener importes negativos.", nameof(presentation));
        decimal supplemental = presentation.Detalles.Sum(line => line.Importe) + presentation.OtrosImpuestos.Sum(tax => tax.Importe);
        if (supplemental > total)
            throw new ArgumentException("Los datos suplementarios superan el total fiscal del snapshot.", nameof(presentation));
        if (presentation.Receptor is { Identificacion: < 0 } || presentation.Receptor is { Nombre: null or "" })
            throw new ArgumentException("El receptor debe tener nombre e identificación válidos.", nameof(presentation));
    }

    internal static IReadOnlyList<ImpuestoComprobante> AddSupplementalTaxes(
        IEnumerable<ImpuestoComprobante> fiscalTaxes, DatosPresentacion presentation) =>
        fiscalTaxes.Concat(presentation.OtrosImpuestos).Select(tax => tax with { }).ToArray();

    internal static IReadOnlyList<LineaComprobante> ApplyPresentationDescriptions(
        IReadOnlyList<LineaComprobante> fiscalLines, DatosPresentacion presentation, string currency)
    {
        if (presentation.Detalles.Count == 0) return fiscalLines;
        if (presentation.Detalles.Count != fiscalLines.Count)
            throw new ArgumentException("Los conceptos suplementarios deben corresponder uno a uno con los conceptos fiscales del snapshot.", nameof(presentation));
        return fiscalLines.Select((line, index) =>
        {
            LineaComprobante supplemental = presentation.Detalles[index];
            if (supplemental.Importe != line.Importe || !ComprobanteValidator.SameCurrency(supplemental.Moneda, currency) ||
                string.IsNullOrWhiteSpace(supplemental.Descripcion))
                throw new ArgumentException("Los conceptos suplementarios deben conservar importes y moneda fiscales y aportar una descripción.", nameof(presentation));
            return line with { DescripcionPresentacion = supplemental.Descripcion };
        }).ToArray();
    }

    internal static DateOnly ParseDate(string value, string field)
    {
        if (!DateOnly.TryParseExact(value, ["yyyyMMdd", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            throw new InvalidOperationException($"The frozen {field} is not a supported ARCA date.");
        return date;
    }

    internal static DateOnly? ResponseExpiry(string? xml)
    {
        if (xml is null) return null;
        XElement? date = XDocument.Parse(xml).Descendants().FirstOrDefault(e => IsName(e, "CAEFchVto", "Fch_venc_Cae", "fechaVencimientoCAE", "fechaVencimiento", "CaeFchVto", "FchVencCae", "FechaVencimientoCae", "FchVto"));
        if (date is null || string.IsNullOrWhiteSpace(date.Value)) return null;
        string value = date.Value;
        if (DateOnly.TryParseExact(value, ["yyyyMMdd", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return parsed;
        return DateOnly.FromDateTime(XmlConvert.ToDateTime(value, XmlDateTimeSerializationMode.Utc));
    }

    private static bool IsName(XElement element, params string[] names) => names.Contains(element.Name.LocalName, StringComparer.OrdinalIgnoreCase);
}
