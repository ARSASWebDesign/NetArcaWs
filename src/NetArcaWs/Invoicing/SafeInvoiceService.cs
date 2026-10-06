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
        ArgumentNullException.ThrowIfNull(tenant); ArgumentNullException.ThrowIfNull(request);
        var payload = Serialize(request);
        var frozen = Deserialize<FecaeRequest>(payload);
        if (frozen.FeCabReq is not { CantReg: 1 } header || frozen.FeDetReq.Count != 1)
            throw new ArgumentException("Durable issuance requires exactly one voucher; use the typed client for batches.", nameof(request));
        var detail = frozen.FeDetReq[0];
        if (detail.CbteDesde != detail.CbteHasta || string.IsNullOrWhiteSpace(detail.CbteFch) || detail.MonCotiz is null)
            throw new ArgumentException("Provide one voucher number, an explicit issue date and exchange rate.", nameof(request));
        var submission = Submission(tenant, key, "wsfe", header.PtoVta, header.CbteTipo, detail.CbteDesde, payload);
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
        ArgumentNullException.ThrowIfNull(tenant); ArgumentNullException.ThrowIfNull(request);
        var payload = Serialize(request);
        var frozen = Deserialize<ClsFexRequest>(payload);
        if (frozen.Id <= 0 || string.IsNullOrWhiteSpace(frozen.FechaCbte) || frozen.MonedaCtz is null)
            throw new ArgumentException("Provide a positive request Id, explicit issue date and exchange rate.", nameof(request));
        var submission = Submission(tenant, key, "wsfex", frozen.PuntoVta, frozen.CbteTipo, frozen.CbteNro, payload) with { RemoteRequestId = frozen.Id };
        return coordinator.SubmitAsync(submission, async ct =>
        {
            var response = await wsfex.FEXAuthorizeAsync(tenant, new FexAuthorize { Cmp = frozen }, ct).ConfigureAwait(false);
            var result = response.FexAuthorizeResult?.FexResultAuth;
            var xml = Serialize(response);
            if (result is null || result.Cuit != tenant.Cuit || result.Id != frozen.Id || result.PuntoVta != frozen.PuntoVta ||
                result.CbteTipo != frozen.CbteTipo || result.CbteNro != frozen.CbteNro || result.FchCbte != frozen.FechaCbte)
                return new(InvoiceState.Unknown, ResponseXml: xml);
            return Decision(result.Resultado, result.Cae, xml);
        }, cancellationToken);
    }

    public Task<InvoiceOperation> AuthorizeWsmtxcaAsync(ArcaTenantContext tenant, string key, ComprobanteType request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tenant); ArgumentNullException.ThrowIfNull(request);
        var payload = Serialize(request);
        var frozen = Deserialize<ComprobanteType>(payload);
        if (frozen.FechaEmision is null || frozen.CotizacionMoneda is null || frozen.CodigoAutorizacion is not null ||
            frozen.CodigoTipoAutorizacion is not null || frozen.FechaVencimiento is not null)
            throw new ArgumentException("CAE issuance requires an explicit issue date and exchange rate, without a preexisting authorization.", nameof(request));
        var submission = Submission(tenant, key, "wsmtxca", frozen.NumeroPuntoVenta, frozen.CodigoTipoComprobante, frozen.NumeroComprobante, payload);
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
                    if (remote is null) return new InvoiceDecision(InvoiceState.Unknown, ResponseXml: xml);
                    if (!Equal(original, Project<ClsFexRequest>(remote))) return new InvoiceDecision(InvoiceState.Conflict, ResponseXml: xml);
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
                    if (remote is null) return new InvoiceDecision(InvoiceState.Unknown, ResponseXml: xml);
                    var projection = Deserialize<ComprobanteType>(Serialize(remote));
                    projection.CodigoAutorizacion = null; projection.CodigoTipoAutorizacion = null; projection.FechaVencimiento = null;
                    if (!Equal(original, projection)) return new InvoiceDecision(InvoiceState.Conflict, ResponseXml: xml);
                    return remote.CodigoTipoAutorizacion?.ToString() == "A" && remote.CodigoAutorizacion is > 0
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

    private static InvoiceSubmission Submission(ArcaTenantContext tenant, string key, string service, int pos, int type, long number, string payload)
        => new(tenant.TenantId, key, service, new(tenant.Environment, tenant.Cuit, pos, type, number), payload);

    private static InvoiceDecision Decision(string? result, string? code, string xml)
        => result == "A" && code is { Length: 14 } && code.All(char.IsAsciiDigit) && code.Any(c => c != '0')
            ? new(InvoiceState.Authorized, code, xml)
            : result == "R" && string.IsNullOrEmpty(code) ? new(InvoiceState.Rejected, ResponseXml: xml)
            : new(InvoiceState.Unknown, ResponseXml: xml);

    private static bool Equal<T>(T left, T right) => XNode.DeepEquals(XElement.Parse(Serialize(left)), XElement.Parse(Serialize(right)));

    private static T Project<T>(object source) where T : new()
    {
        var element = XElement.Parse(Serialize(source));
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
