using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using NetArcaWs.Documents;

namespace NetArcaWs.Rendering.Shared;

internal static class DocumentRenderValidator
{
    internal static DocumentLayout Validate(ComprobanteGenerado document, DatosEmisor issuer)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(issuer);
        ComprobanteValidator.Validate(document);
        ValidateIssuer(issuer);
        if (document.Tipo == TipoComprobanteGenerado.ReciboX) return DocumentLayout.ReceiptX;
        var identity = document.Identidad!;
        if (identity.Cuit != issuer.Identificacion)
            throw new ArgumentException("El CUIT del emisor no coincide con la identidad fiscal del comprobante.", nameof(issuer));
        if (string.IsNullOrWhiteSpace(issuer.IngresosBrutos) || issuer.InicioActividades is null)
            throw new InvalidOperationException("El documento fiscal requiere la situación de Ingresos Brutos y la fecha de inicio de actividades del emisor.");

        if (document.Estado != "Authorized" || document.Resultado is null ||
            document.Resultado.TipoAutorizacion != "E" || document.Resultado.CodigoResultado != "A" ||
            document.Resultado.Vencimiento is null || !IsAuthorizationCode(document.Resultado.CodigoAutorizacion))
            throw new InvalidOperationException("Solo se pueden renderizar comprobantes fiscales autorizados con CAE y vencimiento válidos.");
        if (document.Fecha is null || document.SnapshotVersion != 1 || document.SnapshotResponseVersion != 1 ||
            string.IsNullOrWhiteSpace(document.SnapshotRequest) || string.IsNullOrWhiteSpace(document.SnapshotResponse))
            throw new InvalidOperationException("Faltan snapshots fiscales correlacionados para renderizar el comprobante.");

        DocumentLayout layout = DocumentLayout.For(identity.Servicio, identity.Tipo);
        XDocument request = ParseSafe(document.SnapshotRequest);
        XDocument response = ParseSafe(document.SnapshotResponse);
        RejectAuthenticationMaterial(request);
        RejectAuthenticationMaterial(response);
        VerifyRequest(document, issuer, request);
        VerifyResponse(document, response);
        return layout;
    }

    private static bool IsAuthorizationCode(string? value) => value is { Length: 14 } && value.All(char.IsAsciiDigit);

    private static void ValidateIssuer(DatosEmisor issuer)
    {
        if (issuer.Identificacion is < 10_000_000_000 or > 99_999_999_999 ||
            string.IsNullOrWhiteSpace(issuer.RazonSocial) || string.IsNullOrWhiteSpace(issuer.Domicilio) ||
            string.IsNullOrWhiteSpace(issuer.CondicionTributaria))
            throw new ArgumentException("El perfil legal del emisor está incompleto.", nameof(issuer));
    }

    private static XDocument ParseSafe(string xml)
    {
        try
        {
            using var input = new StringReader(xml);
            using XmlReader reader = XmlReader.Create(input, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1_048_576
            });
            return XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException exception) { throw new InvalidOperationException("El snapshot fiscal no es XML seguro y válido.", exception); }
    }

    private static void RejectAuthenticationMaterial(XDocument document)
    {
        if (document.Descendants().Any(element => element.Name.LocalName.Equals("Auth", StringComparison.OrdinalIgnoreCase) ||
            element.Name.LocalName.Equals("AuthRequest", StringComparison.OrdinalIgnoreCase) ||
            element.Name.LocalName.Equals("Token", StringComparison.OrdinalIgnoreCase) ||
            element.Name.LocalName.Equals("Sign", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("El snapshot contiene material de autenticación y no puede exponerse en un documento.");
    }

    private static void VerifyRequest(ComprobanteGenerado document, DatosEmisor issuer, XDocument xml)
    {
        var identity = document.Identidad!;
        string service = identity.Servicio;
        string typeName = service.Equals("wsfex", StringComparison.OrdinalIgnoreCase) ? "Cbte_Tipo" :
            service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) ? "codigoTipoComprobante" : "CbteTipo";
        string pointName = service.Equals("wsfex", StringComparison.OrdinalIgnoreCase) ? "Punto_vta" :
            service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) ? "numeroPuntoVenta" : "PtoVta";
        string numberName = service.Equals("wsfex", StringComparison.OrdinalIgnoreCase) ? "Cbte_nro" :
            service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) ? "numeroComprobante" : "CbteDesde";
        string dateName = service.Equals("wsfex", StringComparison.OrdinalIgnoreCase) ? "Fecha_cbte" :
            service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) ? "fechaEmision" : "CbteFch";
        string totalName = service.Equals("wsfex", StringComparison.OrdinalIgnoreCase) ? "Imp_total" :
            service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) ? "importeTotal" : "ImpTotal";
        string currencyName = service.Equals("wsfex", StringComparison.OrdinalIgnoreCase) ? "Moneda_Id" :
            service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) ? "codigoMoneda" : "MonId";
        string rateName = service.Equals("wsfex", StringComparison.OrdinalIgnoreCase) ? "Moneda_ctz" :
            service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) ? "cotizacionMoneda" : "MonCotiz";

        RequireEqual(xml, typeName, identity.Tipo.ToString(CultureInfo.InvariantCulture));
        RequireEqual(xml, pointName, identity.PuntoVenta.ToString(CultureInfo.InvariantCulture));
        RequireEqual(xml, numberName, identity.Numero.ToString(CultureInfo.InvariantCulture));
        RequireDate(xml, dateName, document.Fecha!.Value);
        RequireDecimal(xml, totalName, document.Total);
        RequireEqual(xml, currencyName, document.Moneda);
        RequireDecimal(xml, rateName, document.Cotizacion);

        string? receiverId = Value(xml, "DocNro", "numeroDocumento", "Cuit_pais_cliente");
        if (receiverId is not null)
        {
            string expectedId = document.Receptor.IdentificacionFiscal ?? document.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(receiverId, expectedId, StringComparison.Ordinal))
                throw new InvalidOperationException("La identificación del receptor no coincide con el snapshot fiscal.");
        }
        string? receiverType = Value(xml, "DocTipo", "codigoTipoDocumento");
        if (receiverType is not null && document.Receptor.CodigoTipoDocumento is int code && receiverType != code.ToString(CultureInfo.InvariantCulture))
            throw new InvalidOperationException("El tipo de documento del receptor no coincide con el snapshot fiscal.");
        string? recipientName = Value(xml, "Cliente");
        if (recipientName is not null && !string.Equals(recipientName, document.Receptor.Nombre, StringComparison.Ordinal))
            throw new InvalidOperationException("El nombre del receptor no coincide con el snapshot fiscal.");
        string? foreignId = Value(xml, "Id_impositivo");
        if (foreignId is not null && document.Receptor.IdentificacionFiscal is not null && !string.Equals(foreignId, document.Receptor.IdentificacionFiscal, StringComparison.Ordinal))
            throw new InvalidOperationException("La identificación fiscal extranjera no coincide con el snapshot fiscal.");
        string? ivaCondition = Value(xml, "CondicionIvaReceptorId", "condicionIvaReceptor");
        if (ivaCondition is not null && document.CodigoCondicionIvaReceptor is int condition && ivaCondition != condition.ToString(CultureInfo.InvariantCulture))
            throw new InvalidOperationException("La condición de IVA del receptor no coincide con el snapshot fiscal.");
        VerifyAmounts(document, xml, service);
    }

    private static void VerifyResponse(ComprobanteGenerado document, XDocument response)
    {
        string? result = Value(response, "Resultado", "resultado", "FEX_ResultAuth");
        bool mtXca = document.Identidad!.Servicio.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase);
        if (result is null || (mtXca ? result is not ("A" or "O") : result != "A"))
            throw new InvalidOperationException("El snapshot de respuesta no acredita una autorización fiscal positiva.");
        string code = document.Resultado!.CodigoAutorizacion!;
        string? responseCode = Value(response, "CAE", "CAEA", "Cae", "CodAutorizacion", "codigoAutorizacion", "CodigoAutorizacion", "FEX_Cae");
        if (responseCode is null || responseCode != code)
            throw new InvalidOperationException("El CAE del resultado no coincide con el snapshot de respuesta ARCA.");
        string? expiryText = Value(response, "CAEFchVto", "Fch_venc_Cae", "FEX_Fch_venc_Cae", "fechaVencimientoCAE", "fechaVencimiento", "CaeFchVto", "FchVto");
        if (expiryText is null || !DateOnly.TryParseExact(expiryText, ["yyyyMMdd", "yyyy-MM-dd", "yyyy-MM-dd'Z'", "yyyy-MM-ddzzz"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly expiry) || expiry != document.Resultado.Vencimiento)
            throw new InvalidOperationException("El vencimiento del CAE no coincide con el snapshot de respuesta ARCA.");
        var identity = document.Identidad!;
        if (identity.Servicio.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase))
            VerifyOptionalEqual(response, ["Cuit"], identity.Cuit.ToString(CultureInfo.InvariantCulture));
        VerifyOptionalEqual(response, ["CbteTipo", "Cbte_Tipo", "codigoTipoComprobante"], identity.Tipo.ToString(CultureInfo.InvariantCulture));
        VerifyOptionalEqual(response, ["PtoVta", "Punto_vta", "numeroPuntoVenta"], identity.PuntoVenta.ToString(CultureInfo.InvariantCulture));
        VerifyOptionalEqual(response, ["CbteDesde", "Cbte_nro", "numeroComprobante"], identity.Numero.ToString(CultureInfo.InvariantCulture));
        string? receiverId = Value(response, "DocNro", "numeroDocumento", "Cuit_pais_cliente");
        if (receiverId is not null && receiverId != (document.Receptor.IdentificacionFiscal ?? document.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture)))
            throw new InvalidOperationException("La identificación del receptor no coincide con la respuesta fiscal.");
        string? receiverType = Value(response, "DocTipo", "codigoTipoDocumento");
        if (receiverType is not null && document.Receptor.CodigoTipoDocumento is int receiverCode && receiverType != receiverCode.ToString(CultureInfo.InvariantCulture))
            throw new InvalidOperationException("El tipo de documento del receptor no coincide con la respuesta fiscal.");
    }

    private static void RequireEqual(XDocument xml, string name, string expected)
    {
        string? actual = Value(xml, name);
        if (actual is null || !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"El campo {name} no coincide con el snapshot fiscal.");
    }

    private static void RequireDecimal(XDocument xml, string name, decimal expected)
    {
        string? actual = Value(xml, name);
        if (actual is null || !decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed) || parsed != expected)
            throw new InvalidOperationException($"El campo {name} no coincide con el snapshot fiscal.");
    }

    private static void VerifyOptionalEqual(XDocument xml, string[] names, string expected)
    {
        string? actual = Value(xml, names);
        if (actual is not null && actual != expected)
            throw new InvalidOperationException($"El campo {names[0]} de la respuesta no coincide con la identidad fiscal.");
    }

    private static void VerifyAmounts(ComprobanteGenerado document, XDocument xml, string service)
    {
        string? netName = service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) ? "importeGravado" : "ImpNeto";
        string? exemptName = service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) ? "importeExento" : "ImpOpEx";
        string? nonTaxableName = service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) ? "importeNoGravado" : "ImpTotConc";
        VerifyOptionalDecimal(xml, netName, document.Neto);
        VerifyOptionalDecimal(xml, exemptName, document.Exento);
        VerifyOptionalDecimal(xml, nonTaxableName, document.NoGravado);
        string? ivaSummaryName = service.Equals("wsfex", StringComparison.OrdinalIgnoreCase) ? null :
            service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) ? null : "ImpIVA";
        VerifyOptionalDecimal(xml, ivaSummaryName ?? string.Empty, ivaSummaryName is null ? null : document.Iva);
        if (service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) && document.Iva is decimal expectedIva)
        {
            decimal ivaTotal = xml.Descendants().Where(element => element.Name.LocalName.Equals("subtotalIVA", StringComparison.OrdinalIgnoreCase))
                .SelectMany(element => element.Descendants().Where(child => child.Name.LocalName.Equals("importe", StringComparison.OrdinalIgnoreCase)))
                .Sum(element => decimal.TryParse(element.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) ? amount : decimal.MinValue);
            if (ivaTotal != expectedIva) throw new InvalidOperationException("El IVA total del modelo no coincide con el snapshot WSMTXCA.");
        }
        string[] itemNames = service.Equals("wsfex", StringComparison.OrdinalIgnoreCase) ? ["Pro_total_item"] : ["importeItem"];
        XElement[] itemAmounts = xml.Descendants().Where(element => itemNames.Contains(element.Name.LocalName, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (itemAmounts.Length > 0)
        {
            if (itemAmounts.Length != document.Detalles.Count || itemAmounts.Where((element, index) =>
                !decimal.TryParse(element.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) || amount != document.Detalles[index].Importe).Any())
                throw new InvalidOperationException("Los conceptos del modelo no coinciden con el snapshot fiscal.");
        }
        string[] taxAmounts = service.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase) ? ["importe"] : ["Importe"];
        XElement[] sourceTaxAmounts = xml.Descendants().Where(element => taxAmounts.Contains(element.Name.LocalName, StringComparer.OrdinalIgnoreCase) &&
            element.Parent is XElement parent && (parent.Name.LocalName.Contains("Tributo", StringComparison.OrdinalIgnoreCase) || parent.Name.LocalName.Contains("AlicIva", StringComparison.OrdinalIgnoreCase) || parent.Name.LocalName.Equals("subtotalIVA", StringComparison.OrdinalIgnoreCase))).ToArray();
        if (sourceTaxAmounts.Length > 0)
        {
            decimal[] expected = document.Impuestos.Select(tax => tax.Importe).Order().ToArray();
            decimal[] actual = sourceTaxAmounts.Select(element => decimal.TryParse(element.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) ? amount : decimal.MinValue).Order().ToArray();
            if (!actual.SequenceEqual(expected)) throw new InvalidOperationException("Los impuestos del modelo no coinciden con el snapshot fiscal.");
        }
    }

    private static void VerifyOptionalDecimal(XDocument xml, string name, decimal? expected)
    {
        string? actual = Value(xml, name);
        if (actual is not null && (expected is null || !decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed) || parsed != expected))
            throw new InvalidOperationException($"El campo {name} no coincide con el snapshot fiscal.");
    }

    private static string? Value(XDocument xml, params string[] names) => xml.Descendants()
        .FirstOrDefault(element => names.Contains(element.Name.LocalName, StringComparer.OrdinalIgnoreCase))?.Value;

    private static void RequireDate(XDocument xml, string name, DateOnly expected)
    {
        string? actual = Value(xml, name);
        if (actual is null || !DateOnly.TryParseExact(actual, ["yyyyMMdd", "yyyy-MM-dd", "yyyy-MM-dd'Z'", "yyyy-MM-ddzzz"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed) || parsed != expected)
            throw new InvalidOperationException($"El campo {name} no coincide con el snapshot fiscal.");
    }
}
