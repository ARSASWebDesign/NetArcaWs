using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Globalization;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.HealthChecks;

namespace NetArcaWs.Invoicing;

/// <summary>Shared fail-closed classifier for the documented WSFEX 1550 missing-tag validation.</summary>
internal static class WsfexValidationRejectionClassifier
{
    private const string Namespace = "http://ar.gov.afip.dif.fexv1/";
    private const int MaximumPayloadCharacters = 4 * 1024 * 1024;
    private const int MaximumResponseCharacters = 1024 * 1024;
    private static readonly HashSet<string> CriticalResponseNames = new(StringComparer.Ordinal)
    {
        "FEXAuthorizeResponse", "FEXAuthorizeResult", "FEXErr", "ErrCode", "ErrMsg", "FEXResultAuth",
        "FEXEvents", "EventCode", "EventMsg",
        "FEXGetCMPResponse", "FEXGetCMPResult", "FEXResultGet", "Id", "Cbte_tipo", "Punto_vta",
        "Cbte_nro", "Cae", "Resultado"
    };
    private static readonly HashSet<string> NumericVoucherFields = new(StringComparer.Ordinal)
    {
        "Id", "Cbte_tipo", "Punto_vta", "Cbte_nro", "Tipo_expo", "Dst_cmp", "Cuit_pais_cliente",
        "Moneda_ctz", "Imp_total", "Idioma_cbte"
    };
    private static readonly HashSet<string> StringVoucherFields = new(StringComparer.Ordinal)
    {
        "Fecha_cbte", "Permiso_existente", "Cliente", "Domicilio_cliente", "Id_impositivo", "Moneda_Id",
        "CanMisMonExt", "Obs_comerciales", "Obs", "Forma_pago", "Incoterms", "Incoterms_Ds",
        "Fecha_cbte_cae", "Fch_venc_Cae", "Cae", "Resultado", "Motivos_Obs", "Fecha_pago"
    };
    private static readonly HashSet<string> EmptyVoucherCollections = new(StringComparer.Ordinal)
    {
        "Permisos", "Cmps_asoc", "Items", "Opcionales", "Actividades"
    };

    internal static bool IsKnownMissingPermisoTag(InvoiceSubmission submission, string responseXml)
    {
        ArgumentNullException.ThrowIfNull(submission);
        if (string.IsNullOrWhiteSpace(responseXml) || responseXml.Length > MaximumResponseCharacters ||
            submission.Service != "wsfex" || submission.CanonicalVersion != 1 ||
            submission.Identity.Environment is not (ArcaEnvironment.Homologation or ArcaEnvironment.Production))
            return false;

        try
        {
            XDocument requestDocument = ParseDocument(submission.Payload, MaximumPayloadCharacters);
            // Match the exact root QName emitted by the canonical SafeInvoiceService serializer.
            if (requestDocument.Root?.Name != XName.Get("ClsFEXRequest") ||
                requestDocument.Root.Elements().Any(element => element.Name.LocalName == "Permiso_existente")) return false;
            var request = Deserialize<ClsFexRequest>(requestDocument);
            if (request.Id <= 0 || request.Id != submission.RemoteRequestId ||
                submission.Identity.Cuit <= 0 || submission.Identity.PointOfSale <= 0 ||
                submission.Identity.VoucherType <= 0 || submission.Identity.VoucherNumber <= 0 ||
                request.PuntoVta != submission.Identity.PointOfSale ||
                request.CbteTipo != submission.Identity.VoucherType ||
                request.CbteNro != submission.Identity.VoucherNumber)
                return false;

            XDocument responseDocument = ParseDocument(responseXml, MaximumResponseCharacters);
            XElement? responseRoot = responseDocument.Root;
            if (responseRoot is null || responseRoot.Name != XName.Get("FEXAuthorizeResponse", Namespace) ||
                HasAmbiguousContractNames(responseRoot)) return false;
            XNamespace ns = Namespace;
            List<XElement> authorizationResults = responseRoot.Elements(ns + "FEXAuthorizeResult").ToList();
            if (responseRoot.Elements().Count() != 1 || authorizationResults.Count != 1) return false;
            XElement authorizationResult = authorizationResults[0];
            if (authorizationResult.Elements().Any(element => element.Name != ns + "FEXErr" &&
                element.Name != ns + "FEXEvents" && element.Name != ns + "FEXResultAuth") ||
                !HasValidEvents(authorizationResult, ns)) return false;
            List<XElement> errors = authorizationResult.Elements(ns + "FEXErr").ToList();
            if (errors.Count != 1 || responseRoot.Descendants(ns + "FEXAuthorizeResult").Count() != 1 ||
                responseRoot.Descendants(ns + "FEXErr").Count() != 1 ||
                responseRoot.Descendants(ns + "FEXResultAuth").Any() || !IsValidError(errors[0], ns)) return false;
            FexAuthorizeResponse response = Deserialize<FexAuthorizeResponse>(responseDocument);
            return IsKnownMissingPermisoTag(request, response);
        }
        catch (Exception exception) when (exception is XmlException or InvalidOperationException or ArgumentException)
        {
            return false;
        }
    }

    internal static bool IsKnownMissingPermisoTag(ClsFexRequest request, FexAuthorizeResponse response)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);

        FexResponseAuthorize? result = response.FexAuthorizeResult;
        ClsFexErr? error = result?.FexErr;
        return result is { FexResultAuth: null } && error is { ErrCode: 1550, ErrMsg: not null } &&
            request.PermisoExistente is null && (request.TipoExpo is 2 or 4) && (request.CbteTipo is 19 or 20 or 21) &&
            error.ErrMsg.Contains("Permiso_existente", StringComparison.OrdinalIgnoreCase) &&
            error.ErrMsg.Contains("debe enviarse tag", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsSafeCurrentResponseForAdjudication(InvoiceOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        string? responseXml = operation.ResponseXml;
        if (string.IsNullOrWhiteSpace(responseXml) || responseXml.Length > MaximumResponseCharacters) return false;

        try
        {
            XDocument document = ParseDocument(responseXml, MaximumResponseCharacters);
            XElement? root = document.Root;
            if (root is null || HasAmbiguousContractNames(root)) return false;
            XNamespace ns = Namespace;

            if (root.Name == ns + "FEXAuthorizeResponse")
                return IsKnownMissingPermisoTag(operation.Submission, responseXml);

            if (root.Name != ns + "FEXGetCMPResponse") return false;
            List<XElement> queryResults = root.Elements(ns + "FEXGetCMPResult").ToList();
            if (queryResults.Count != 1 || root.Elements().Count() != 1 ||
                queryResults[0].Name != ns + "FEXGetCMPResult")
                return false;
            XElement queryResult = queryResults[0];
            if (queryResult.Elements().Any(element => element.Name != ns + "FEXErr" && element.Name != ns + "FEXResultGet" &&
                element.Name != ns + "FEXEvents") || !HasValidEvents(queryResult, ns))
                return false;
            if (!HasOnlyNoResultError(queryResult, ns)) return false;

            List<XElement> voucherResults = queryResult.Elements(ns + "FEXResultGet").ToList();
            return voucherResults.Count == 0 || (voucherResults.Count == 1 && IsDefaultEmptyVoucher(voucherResults[0], ns));
        }
        catch (Exception exception) when (exception is XmlException or InvalidOperationException or ArgumentException)
        {
            return false;
        }
    }

    private static bool HasAmbiguousContractNames(XElement root)
    {
        XElement[] matches = root.DescendantsAndSelf()
            .Where(element => CriticalResponseNames.Contains(element.Name.LocalName))
            .ToArray();
        return matches.Any(element => element.Name.NamespaceName != Namespace) ||
            matches.GroupBy(element => element.Name.LocalName, StringComparer.Ordinal).Any(group => group.Count() > 1);
    }

    private static bool HasOnlyNoResultError(XElement queryResult, XNamespace ns)
    {
        List<XElement> errors = queryResult.Elements(ns + "FEXErr").ToList();
        if (errors.Count != 1 || !IsValidError(errors[0], ns)) return false;
        List<XElement> codes = errors[0].Elements(ns + "ErrCode").ToList();
        return codes.Count == 1 && int.TryParse(codes[0].Value, NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int code) && code == 1020;
    }

    private static bool IsValidError(XElement error, XNamespace ns)
    {
        if (error.Name != ns + "FEXErr" || error.Elements().Any(element =>
            element.Name != ns + "ErrCode" && element.Name != ns + "ErrMsg")) return false;
        List<XElement> codes = error.Elements(ns + "ErrCode").ToList();
        List<XElement> messages = error.Elements(ns + "ErrMsg").ToList();
        return codes.Count == 1 && messages.Count <= 1 && !codes[0].HasElements &&
            messages.All(message => !message.HasElements);
    }

    private static bool HasValidEvents(XElement parent, XNamespace ns)
    {
        List<XElement> events = parent.Elements(ns + "FEXEvents").ToList();
        if (events.Count > 1) return false;
        if (events.Count == 0) return true;

        XElement eventElement = events[0];
        if (eventElement.Elements().Any(element => element.Name != ns + "EventCode" && element.Name != ns + "EventMsg"))
            return false;
        List<XElement> codes = eventElement.Elements(ns + "EventCode").ToList();
        List<XElement> messages = eventElement.Elements(ns + "EventMsg").ToList();
        return codes.Count == 1 && messages.Count <= 1 &&
            int.TryParse(codes[0].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) &&
            !codes[0].HasElements && messages.All(message => !message.HasElements);
    }

    private static bool IsDefaultEmptyVoucher(XElement voucher, XNamespace ns)
    {
        if (voucher.Name != ns + "FEXResultGet" || voucher.Elements().Any(element => element.HasElements) ||
            voucher.Elements().GroupBy(element => element.Name.LocalName, StringComparer.Ordinal).Any(group => group.Count() > 1)) return false;
        foreach (string name in new[] { "Id", "Cbte_tipo", "Punto_vta", "Cbte_nro" })
        {
            List<XElement> values = voucher.Elements(ns + name).ToList();
            if (values.Count != 1 || !long.TryParse(values[0].Value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out long value) || value != 0)
                return false;
        }

        foreach (XElement value in voucher.Elements())
        {
            if (value.Name.NamespaceName != Namespace) return false;
            if (NumericVoucherFields.Contains(value.Name.LocalName))
            {
                if (!decimal.TryParse(value.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number) || number != 0)
                    return false;
                continue;
            }
            if (StringVoucherFields.Contains(value.Name.LocalName))
            {
                if (!string.IsNullOrEmpty(value.Value)) return false;
                continue;
            }
            if (EmptyVoucherCollections.Contains(value.Name.LocalName) && !value.HasElements && value.Value.Length == 0) continue;
            return false;
        }
        return true;
    }

    private static XDocument ParseDocument(string xml, int maximumCharacters)
    {
        if (string.IsNullOrWhiteSpace(xml) || xml.Length > maximumCharacters)
            throw new ArgumentException("XML evidence is empty or exceeds the limit.", nameof(xml));
        using var input = new StringReader(xml);
        using var reader = XmlReader.Create(input, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = maximumCharacters,
            ConformanceLevel = ConformanceLevel.Document
        });
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static T Deserialize<T>(XDocument document)
    {
        using var reader = document.CreateReader();
        return (T)(new XmlSerializer(typeof(T)).Deserialize(reader)
            ?? throw new InvalidOperationException("Missing XML root."));
    }
}
