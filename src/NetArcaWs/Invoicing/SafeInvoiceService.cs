using System.Globalization;
using System.Text;
using NetArcaWs.Transport;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;

namespace NetArcaWs.Invoicing;

/// <summary>Durable single-voucher CAE issuance. Uncertain outcomes are queried, never automatically resubmitted.</summary>
public sealed class SafeInvoiceService(InvoiceCoordinator coordinator, IWsfev1Service wsfe,
    IWsfexv1Service wsfex, IWsmtxcav1Service wsmtxca)
{
    public Task<InvoiceOperation> AuthorizeWsfeAsync(ArcaTenantContext tenant, string key, FecaeSolicitar request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return AuthorizeWsfeAsync(tenant, key, request.FeCaeReq, cancellationToken);
    }

    public Task<InvoiceOperation> AuthorizeWsfeAsync(ArcaTenantContext tenant, string key, FecaeRequest request,
        CancellationToken cancellationToken = default)
    {
        InvoiceSubmission submission = CreateWsfeSubmission(tenant, key, request);
        var frozen = Deserialize<FecaeRequest>(submission.Payload);
        var header = frozen.FeCabReq!;
        var detail = frozen.FeDetReq[0];
        return coordinator.SubmitAsync(submission, async ct =>
        {
            var response = await wsfe.FECAESolicitarAsync(tenant, new FecaeSolicitar { FeCaeReq = frozen }, ct).ConfigureAwait(false);
            var result = response.FecaeSolicitarResult;
            var cab = result?.FeCabResp;
            var det = result?.FeDetResp.Count == 1 ? result.FeDetResp[0] : null;
            var xml = Serialize(response);
            if (cab is null || det is null || cab.Cuit != tenant.Cuit || cab.PtoVta != header.PtoVta ||
                cab.CbteTipo != header.CbteTipo || cab.CantReg != 1 || det.CbteDesde != detail.CbteDesde ||
                det.CbteHasta != detail.CbteHasta || det.Concepto != detail.Concepto || det.DocTipo != detail.DocTipo ||
                det.DocNro != detail.DocNro || det.CbteFch != detail.CbteFch || cab.Resultado != det.Resultado)
                return new(InvoiceState.Unknown, ResponseXml: xml);
            return Decision(det.Resultado, det.Cae, xml);
        }, cancellationToken);
    }

    public Task<InvoiceOperation> AuthorizeWsfexAsync(ArcaTenantContext tenant, string key, ClsFexRequest request,
        CancellationToken cancellationToken = default)
    {
        InvoiceSubmission submission = CreateWsfexSubmission(tenant, key, request);
        var frozen = Deserialize<ClsFexRequest>(submission.Payload);
        return coordinator.SubmitAsync(submission, async ct =>
        {
            var response = await wsfex.FEXAuthorizeAsync(tenant, new FexAuthorize { Cmp = frozen }, ct).ConfigureAwait(false);
            var xml = Serialize(response);
            FexResponseAuthorize? authorization = response.FexAuthorizeResult;
            if (authorization?.FexErr?.ErrCode == 1550)
                return new InvoiceDecision(WsfexValidationRejectionClassifier.IsKnownMissingPermisoTag(submission, xml)
                    ? InvoiceState.Rejected : InvoiceState.Unknown, ResponseXml: xml);
            if (authorization?.FexErr?.ErrCode == 1673)
                return new InvoiceDecision(WsfexValidationRejectionClassifier.IsKnownForbiddenPaymentDate(submission, xml)
                    ? InvoiceState.Rejected : InvoiceState.Unknown, ResponseXml: xml);

            var result = authorization?.FexResultAuth;
            if (result is null || result.Cuit != tenant.Cuit || result.Id != frozen.Id || result.PuntoVta != frozen.PuntoVta ||
                result.CbteTipo != frozen.CbteTipo || result.CbteNro != frozen.CbteNro || result.FchCbte != frozen.FechaCbte)
                return new(InvoiceState.Unknown, ResponseXml: xml);
            return Decision(result.Resultado, result.Cae, xml);
        }, cancellationToken);
    }

    public Task<InvoiceOperation> AuthorizeWsmtxcaAsync(ArcaTenantContext tenant, string key, ComprobanteType request,
        CancellationToken cancellationToken = default)
    {
        InvoiceSubmission submission = CreateWsmtxcaSubmission(tenant, key, request);
        var frozen = Deserialize<ComprobanteType>(submission.Payload);
        return coordinator.SubmitAsync(submission, async ct =>
        {
            var response = await wsmtxca.autorizarComprobanteAsync(tenant,
                new AutorizarComprobanteRequestType { ComprobanteCaeRequest = frozen }, ct).ConfigureAwait(false);
            var result = response.ComprobanteResponse;
            var xml = Serialize(response);
            // MTXCA omits ComprobanteResponse on documented business rejection.
            if (response.Resultado == ResultadoSimpleType.R && result is null && response.ArrayErrores.Count > 0)
                return new(InvoiceState.Rejected, ResponseXml: xml);
            if (result is null || result.Cuit != tenant.Cuit || result.NumeroPuntoVenta != frozen.NumeroPuntoVenta ||
                result.CodigoTipoComprobante != frozen.CodigoTipoComprobante || result.NumeroComprobante != frozen.NumeroComprobante ||
                result.FechaEmision != frozen.FechaEmision)
                return new(InvoiceState.Unknown, ResponseXml: xml);
            return Decision(response.Resultado is ResultadoSimpleType.A or ResultadoSimpleType.O ? "A" : response.Resultado.ToString(), result.Cae > 0 ? result.Cae.ToString(CultureInfo.InvariantCulture) : null, xml);
        }, cancellationToken);
    }

    public static InvoiceSubmission CreateWsfeSubmission(ArcaTenantContext tenant, string key, FecaeSolicitar request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CreateWsfeSubmission(tenant, key, request.FeCaeReq);
    }

    public static InvoiceSubmission CreateWsfeSubmission(ArcaTenantContext tenant, string key, FecaeRequest request)
    {
        ArgumentNullException.ThrowIfNull(tenant); ArgumentNullException.ThrowIfNull(request);
        string payload = Serialize(request);
        FecaeRequest frozen = Deserialize<FecaeRequest>(payload);
        if (frozen.FeCabReq is not { CantReg: 1 } header || frozen.FeDetReq.Count != 1)
            throw new ArgumentException("Durable issuance requires exactly one voucher; use the typed client for batches.", nameof(request));
        var detail = frozen.FeDetReq[0];
        if (detail.CbteDesde != detail.CbteHasta || string.IsNullOrWhiteSpace(detail.CbteFch) || detail.MonCotiz is null)
            throw new ArgumentException("Provide one voucher number, an explicit issue date and exchange rate.", nameof(request));
        return Submission(tenant, key, "wsfe", header.PtoVta, header.CbteTipo, detail.CbteDesde, payload);
    }

    public static InvoiceSubmission CreateWsfexSubmission(ArcaTenantContext tenant, string key, ClsFexRequest request)
    {
        ArgumentNullException.ThrowIfNull(tenant); ArgumentNullException.ThrowIfNull(request);
        string payload = Serialize(request);
        ClsFexRequest frozen = Deserialize<ClsFexRequest>(payload);
        if (frozen.Id <= 0 || string.IsNullOrWhiteSpace(frozen.FechaCbte) || frozen.MonedaCtz is null)
            throw new ArgumentException("Provide a positive request Id, explicit issue date and exchange rate.", nameof(request));
        return Submission(tenant, key, "wsfex", frozen.PuntoVta, frozen.CbteTipo, frozen.CbteNro, payload) with { RemoteRequestId = frozen.Id };
    }

    public static InvoiceSubmission CreateWsmtxcaSubmission(ArcaTenantContext tenant, string key, ComprobanteType request)
    {
        ArgumentNullException.ThrowIfNull(tenant); ArgumentNullException.ThrowIfNull(request);
        string payload = Serialize(request);
        ComprobanteType frozen = Deserialize<ComprobanteType>(payload);
        if (frozen.FechaEmision is null || frozen.CotizacionMoneda is null || frozen.CodigoAutorizacion is not null ||
            frozen.CodigoTipoAutorizacion is not null || frozen.FechaVencimiento is not null)
            throw new ArgumentException("CAE issuance requires an explicit issue date and exchange rate, without a preexisting authorization.", nameof(request));
        return Submission(tenant, key, "wsmtxca", frozen.NumeroPuntoVenta, frozen.CodigoTipoComprobante, frozen.NumeroComprobante, payload);
    }

    /// <summary>Recovers a stored operation. Only an unsent Prepared snapshot can be submitted.</summary>
    public async Task<InvoiceOperation> ResumeAsync(ArcaTenantContext tenant, string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        var operation = await coordinator.FindAsync(tenant.TenantId, key, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The invoice operation does not exist.");
        ValidateContext(tenant, operation.Submission);
        if (operation.State != InvoiceState.Prepared) return await ReconcileAsync(tenant, key, cancellationToken).ConfigureAwait(false);
        return operation.Submission.Service switch
        {
            "wsfe" => await AuthorizeWsfeAsync(tenant, key, Deserialize<FecaeRequest>(operation.Submission.Payload), cancellationToken).ConfigureAwait(false),
            "wsfex" => await AuthorizeWsfexAsync(tenant, key, Deserialize<ClsFexRequest>(operation.Submission.Payload), cancellationToken).ConfigureAwait(false),
            "wsmtxca" => await AuthorizeWsmtxcaAsync(tenant, key, Deserialize<ComprobanteType>(operation.Submission.Payload), cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException("No issuance adapter is registered for this service.")
        };
    }

    /// <summary>Explicitly prepares a corrected version of a confirmed rejection. Call ResumeAsync to send it.</summary>
    public Task<InvoiceOperation> ReviseRejectedAsync(ArcaTenantContext tenant, string key, long expectedVersion,
        FecaeRequest replacement, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant); ArgumentNullException.ThrowIfNull(replacement);
        var payload = Serialize(replacement);
        var frozen = Deserialize<FecaeRequest>(payload);
        if (frozen.FeCabReq is not { CantReg: 1 } header || frozen.FeDetReq.Count != 1 ||
            frozen.FeDetReq[0].CbteDesde != frozen.FeDetReq[0].CbteHasta ||
            string.IsNullOrWhiteSpace(frozen.FeDetReq[0].CbteFch) || frozen.FeDetReq[0].MonCotiz is null)
            throw new ArgumentException("Provide one voucher, an explicit issue date and exchange rate.", nameof(replacement));
        return coordinator.ReviseRejectedAsync(Submission(tenant, key, "wsfe", header.PtoVta, header.CbteTipo,
            frozen.FeDetReq[0].CbteDesde, payload), expectedVersion, cancellationToken);
    }

    public Task<InvoiceOperation> ReviseRejectedAsync(ArcaTenantContext tenant, string key, long expectedVersion,
        ClsFexRequest replacement, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant); ArgumentNullException.ThrowIfNull(replacement);
        var payload = Serialize(replacement);
        var frozen = Deserialize<ClsFexRequest>(payload);
        if (frozen.Id <= 0 || string.IsNullOrWhiteSpace(frozen.FechaCbte) || frozen.MonedaCtz is null)
            throw new ArgumentException("Provide an Id, explicit issue date and exchange rate.", nameof(replacement));
        return coordinator.ReviseRejectedAsync(Submission(tenant, key, "wsfex", frozen.PuntoVta, frozen.CbteTipo,
            frozen.CbteNro, payload) with { RemoteRequestId = frozen.Id }, expectedVersion, cancellationToken);
    }

    public Task<InvoiceOperation> ReviseRejectedAsync(ArcaTenantContext tenant, string key, long expectedVersion,
        ComprobanteType replacement, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant); ArgumentNullException.ThrowIfNull(replacement);
        var payload = Serialize(replacement);
        var frozen = Deserialize<ComprobanteType>(payload);
        if (frozen.FechaEmision is null || frozen.CotizacionMoneda is null || frozen.CodigoAutorizacion is not null ||
            frozen.CodigoTipoAutorizacion is not null || frozen.FechaVencimiento is not null)
            throw new ArgumentException("Provide an issue date and exchange rate without a preexisting authorization.", nameof(replacement));
        return coordinator.ReviseRejectedAsync(Submission(tenant, key, "wsmtxca", frozen.NumeroPuntoVenta,
            frozen.CodigoTipoComprobante, frozen.NumeroComprobante, payload), expectedVersion, cancellationToken);
    }

    /// <summary>
    /// Explicitly adjudicates a retained allowlisted WSFEX validation rejection using the exact frozen request that produced it.
    /// This updates only the journal and never sends or reconciles a SOAP request.
    /// </summary>
    public async Task<InvoiceOperation> AdjudicateWsfexValidationRejectionAsync(ArcaTenantContext tenant, string key,
        long expectedVersion, InvoiceRejectionEvidence evidence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(evidence);
        ValidateRejectionEvidenceMetadata(evidence);

        InvoiceOperation current = await coordinator.FindAsync(tenant.TenantId, key, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The invoice operation does not exist.");
        ValidateContext(tenant, current.Submission);
        if (current.Version != expectedVersion || current.State is not (InvoiceState.Unknown or InvoiceState.Conflict or InvoiceState.ManualReview) ||
            current.AuthorizationCode is not null)
            throw new InvoiceConflictException("Only the current unresolved operation without an authorization can be adjudicated.");
        if (current.Submission.Service != "wsfex" || evidence.OriginalSubmission != current.Submission)
            throw new InvoiceConflictException("The evidence must identify the exact immutable WSFEX submission being adjudicated.");
        if (!WsfexValidationRejectionClassifier.IsSafeCurrentResponseForAdjudication(current))
            throw new InvoiceConflictException("The latest stored WSFEX response is malformed, unknown, or contains potentially positive authorization evidence.");
        if (!WsfexValidationRejectionClassifier.IsKnownTerminalValidationRejection(current.Submission, evidence.OriginalResponseXml))
            throw new InvoiceConflictException("The supplied response and frozen request do not match an approved WSFEX validation rejection.");

        return await coordinator.AdjudicateWsfexValidationRejectionAsync(tenant.TenantId, key, expectedVersion,
            evidence, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads the append-only audit evidence for explicit WSFEX rejection adjudications.</summary>
    public async Task<IReadOnlyList<InvoiceRejectionAdjudication>> ListWsfexRejectionAdjudicationsAsync(
        ArcaTenantContext tenant, string key, int limit = 100, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        InvoiceOperation current = await coordinator.FindAsync(tenant.TenantId, key, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The invoice operation does not exist.");
        ValidateContext(tenant, current.Submission);
        if (current.Submission.Service != "wsfex")
            throw new InvoiceConflictException("WSFEX adjudication history is available only for WSFEX operations.");
        return await coordinator.ListRejectionAdjudicationsAsync(tenant.TenantId, key, limit, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InvoiceOperation> ReconcileAsync(ArcaTenantContext tenant, string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        var current = await coordinator.FindAsync(tenant.TenantId, key, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException("The invoice operation does not exist.");
        ValidateContext(tenant, current.Submission);
        return await coordinator.ReconcileAsync(tenant.TenantId, key, async (operation, ct) =>
        {
            var saved = operation.Submission;
            ValidateContext(tenant, saved);
            var id = saved.Identity;
            switch (saved.Service)
            {
                case "wsfe":
                {
                    var original = Deserialize<FecaeRequest>(saved.Payload);
                    var response = await wsfe.FECompConsultarAsync(tenant, new FeCompConsultar
                    { FeCompConsReq = new FeCompConsultaReq { PtoVta = id.PointOfSale, CbteTipo = id.VoucherType, CbteNro = id.VoucherNumber } }, ct).ConfigureAwait(false);
                    var remote = response.FeCompConsultarResult?.ResultGet;
                    var xml = Serialize(response);
                    if (response.FeCompConsultarResult?.Errors.Count > 0)
                        return new InvoiceDecision(InvoiceState.Unknown, ResponseXml: xml);
                    if (remote is null) return new InvoiceDecision(InvoiceState.Unknown, ResponseXml: xml);
                    if (remote.PtoVta != id.PointOfSale || remote.CbteTipo != id.VoucherType ||
                        !Equal(original.FeDetReq[0], Project<FecaeDetRequest>(remote)))
                        return new InvoiceDecision(InvoiceState.Conflict, ResponseXml: xml);
                    return remote.EmisionTipo == "CAE" && remote.Resultado == "A"
                        ? Decision("A", remote.CodAutorizacion, xml) : new InvoiceDecision(InvoiceState.Unknown, ResponseXml: xml);
                }
                case "wsfex":
                {
                    var original = Deserialize<ClsFexRequest>(saved.Payload);
                    var response = await wsfex.FEXGetCMPAsync(tenant, new FexGetCmp
                    { Cmp = new ClsFexGetCmp { PuntoVta = id.PointOfSale, CbteTipo = checked((short)id.VoucherType), CbteNro = id.VoucherNumber } }, ct).ConfigureAwait(false);
                    var remote = response.FexGetCmpResult?.FexResultGet;
                    var xml = Serialize(response);
                    if (response.FexGetCmpResult?.FexErr is { ErrCode: not 0 })
                        return new InvoiceDecision(InvoiceState.Unknown, ResponseXml: xml);
                    if (remote is null) return new InvoiceDecision(InvoiceState.Unknown, ResponseXml: xml);
                    if (remote.Id <= 0 || remote.CbteTipo <= 0 || remote.PuntoVta <= 0 || remote.CbteNro <= 0)
                        return new InvoiceDecision(InvoiceState.Unknown, ResponseXml: xml);
                    if (!EqualWsfexProjection(original, Project<ClsFexRequest>(remote))) return new InvoiceDecision(InvoiceState.Conflict, ResponseXml: xml);
                    return remote.Resultado == "A" ? Decision("A", remote.Cae, xml) : new InvoiceDecision(InvoiceState.Unknown, ResponseXml: xml);
                }
                case "wsmtxca":
                {
                    var original = Deserialize<ComprobanteType>(saved.Payload);
                    var response = await wsmtxca.consultarComprobanteAsync(tenant, new ConsultarComprobanteRequestType
                    { ConsultaComprobanteRequest = new ConsultaComprobanteRequestType { NumeroPuntoVenta = id.PointOfSale,
                        CodigoTipoComprobante = checked((short)id.VoucherType), NumeroComprobante = id.VoucherNumber } }, ct).ConfigureAwait(false);
                    var remote = response.Comprobante;
                    var xml = Serialize(response);
                    if (response.ArrayErrores.Count != 0)
                        return new InvoiceDecision(InvoiceState.Unknown, ResponseXml: xml);
                    if (remote is null) return new InvoiceDecision(InvoiceState.Unknown, ResponseXml: xml);
                    var projection = Deserialize<ComprobanteType>(Serialize(remote));
                    projection.CodigoAutorizacion = null; projection.CodigoTipoAutorizacion = null; projection.FechaVencimiento = null;
                    if (!EqualWsmtxcaProjection(original, projection)) return new InvoiceDecision(InvoiceState.Conflict, ResponseXml: xml);
                    return remote.CodigoTipoAutorizacion?.ToString() == "E" && remote.CodigoAutorizacion is > 0
                        ? Decision("A", remote.CodigoAutorizacion.Value.ToString(CultureInfo.InvariantCulture), xml)
                        : new InvoiceDecision(InvoiceState.Unknown, ResponseXml: xml);
                }
                default: throw new InvalidOperationException("No reconciliation adapter is registered for this service.");
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateContext(ArcaTenantContext tenant, InvoiceSubmission saved)
    {
        if (saved.TenantId != tenant.TenantId || saved.Identity.Cuit != tenant.Cuit || saved.Identity.Environment != tenant.Environment || saved.CanonicalVersion != 1)
            throw new InvoiceConflictException("The tenant context or canonical schema differs from the immutable operation.");
    }

    private static void ValidateRejectionEvidenceMetadata(InvoiceRejectionEvidence evidence)
    {
        if (evidence.OriginalSubmission is null || string.IsNullOrWhiteSpace(evidence.OriginalResponseXml) ||
            evidence.OriginalResponseXml.Length > 1024 * 1024)
            throw new ArgumentException("The original submission and a bounded original response are required.", nameof(evidence));
        ValidateAuditValue(evidence.Operator, 128, nameof(evidence.Operator));
        ValidateAuditValue(evidence.Source, 256, nameof(evidence.Source));
        ValidateAuditValue(evidence.Reason, 1024, nameof(evidence.Reason));
    }

    private static void ValidateAuditValue(string value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength || value.Any(char.IsControl))
            throw new ArgumentException("Audit metadata must be non-empty, bounded, and contain no control characters.", parameterName);
    }

    private static InvoiceSubmission Submission(ArcaTenantContext tenant, string key, string service, int pos, int type, long number, string payload)
        => new(tenant.TenantId, key, service, new(tenant.Environment, tenant.Cuit, pos, type, number), payload);

    private static InvoiceDecision Decision(string? result, string? code, string xml)
        => result == "A" && code is { Length: 14 } && code.All(char.IsAsciiDigit) && code.Any(c => c != '0')
            ? new(InvoiceState.Authorized, code, xml)
            : result == "R" && string.IsNullOrEmpty(code) ? new(InvoiceState.Rejected, ResponseXml: xml)
            : new(InvoiceState.Unknown, ResponseXml: xml);

    private static bool Equal<T>(T left, T right) => XNode.DeepEquals(XElement.Parse(Serialize(left)), XElement.Parse(Serialize(right)));

    private static bool EqualWsmtxcaProjection(ComprobanteType left, ComprobanteType right)
    {
        XElement leftXml = XElement.Parse(Serialize(left), LoadOptions.PreserveWhitespace);
        XElement rightXml = XElement.Parse(Serialize(right), LoadOptions.PreserveWhitespace);
        NormalizeWsmtxcaDecimalValues(leftXml);
        NormalizeWsmtxcaDecimalValues(rightXml);
        return XNode.DeepEquals(leftXml, rightXml);
    }

    private static void NormalizeWsmtxcaDecimalValues(XElement root)
    {
        const NumberStyles styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        foreach (XElement element in root.Descendants())
        {
            if (element.Name.NamespaceName.Length != 0 || !IsWsmtxcaDecimalPath(element)) continue;
            if (element.Nodes().Count() != 1 || element.FirstNode is not XText text ||
                !decimal.TryParse(text.Value, styles, CultureInfo.InvariantCulture, out decimal value))
                continue;
            element.Value = value == 0m ? "0" : value.ToString("0.############################", CultureInfo.InvariantCulture);
        }
    }

    private static bool IsWsmtxcaDecimalPath(XElement element)
    {
        XElement[] ancestors = element.Ancestors().Reverse().ToArray();
        if (ancestors.Any(ancestor => ancestor.Name.NamespaceName.Length != 0)) return false;
        string path = string.Join("/", ancestors.Skip(1).Append(element).Select(node => node.Name.LocalName));
        return path is "importeGravado" or "importeNoGravado" or "importeExento" or "importeSubtotal" or
            "importeOtrosTributos" or "importeTotal" or "cotizacionMoneda" or
            "arrayOtrosTributos/otroTributo/baseImponible" or "arrayOtrosTributos/otroTributo/importe" or
            "arrayItems/item/cantidad" or "arrayItems/item/precioUnitario" or
            "arrayItems/item/importeBonificacion" or "arrayItems/item/importeIVA" or
            "arrayItems/item/importeItem" or "arraySubtotalesIVA/subtotalIVA/importe" or
            "arrayCompradores/comprador/porcentaje";
    }

    private static bool EqualWsfexProjection(ClsFexRequest expected, ClsFexRequest actual)
    {
        XElement expectedXml = ParseWsfexProjection(Serialize(expected));
        XElement actualXml = ParseWsfexProjection(Serialize(actual));
        bool allowIncoterms = expected.CbteTipo != 19 || expected.TipoExpo != 1;
        RemoveExactEmptyWsfexOptionals(expectedXml, allowIncoterms);
        RemoveExactEmptyWsfexOptionals(actualXml, allowIncoterms);
        return XNode.DeepEquals(expectedXml, actualXml);
    }

    private static void RemoveExactEmptyWsfexOptionals(XElement root, bool allowIncoterms)
    {
        XNamespace expectedNamespace = "http://ar.gov.afip.dif.fexv1/";
        string[] allowedNames = allowIncoterms ? ["Obs_comerciales", "Obs", "Incoterms"] : ["Obs_comerciales", "Obs"];
        root.Elements().Where(element => element.Name.Namespace == expectedNamespace &&
            allowedNames.Contains(element.Name.LocalName, StringComparer.Ordinal) && !element.HasElements &&
            element.Value.Length == 0 && !element.Attributes().Any(attribute => !attribute.IsNamespaceDeclaration))
            .Remove();
    }

    private static XElement ParseWsfexProjection(string xml)
    {
        XElement root = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
        foreach (XElement parent in root.DescendantsAndSelf().Where(element => element.HasElements).ToArray())
            parent.Nodes().OfType<XText>().Where(text => string.IsNullOrWhiteSpace(text.Value)).Remove();
        return root;
    }

    private static T Project<T>(object source) where T : new()
    {
        var serialized = Serialize(source);
        var element = source is ClsFexGetCmpr && typeof(T) == typeof(ClsFexRequest)
            ? ParseWsfexProjection(serialized)
            : XElement.Parse(serialized);
        element.Name = XElement.Parse(Serialize(new T())).Name;
        // WSFEX uses different case for this field in request and consultation schemas.
        if (source is ClsFexGetCmpr && typeof(T) == typeof(ClsFexRequest))
        {
            XNamespace ns = "http://ar.gov.afip.dif.fexv1/";
            var voucherType = element.Element(ns + "Cbte_tipo");
            if (voucherType is not null) voucherType.Name = ns + "Cbte_Tipo";
            // These consultation-only fields precede Opcionales in the response sequence.
            // Remove them before reading the request sequence, otherwise XmlSerializer can
            // skip subsequent fiscal fields when their expected order no longer matches.
            foreach (var metadata in new[] { "Fecha_cbte_cae", "Fch_venc_Cae", "Cae", "Resultado", "Motivos_Obs" })
                element.Elements(ns + metadata).Remove();
        }
        element.Attribute(XName.Get("type", "http://www.w3.org/2001/XMLSchema-instance"))?.Remove();
        return Deserialize<T>(element.ToString(SaveOptions.DisableFormatting));
    }

    private static string Serialize<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        using var buffer = new BoundedMemoryStream(4 * 1024 * 1024);
        using (var writer = XmlWriter.Create(buffer, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = true, CloseOutput = false }))
            new XmlSerializer(value.GetType()).Serialize(writer, value);
        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, checked((int)buffer.Length));
    }

    private static T Deserialize<T>(string xml)
    {
        using var input = new StringReader(xml);
        using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 4 * 1024 * 1024 });
        return (T)(new XmlSerializer(typeof(T)).Deserialize(reader) ?? throw new InvalidDataException("Missing fiscal payload."));
    }
}
