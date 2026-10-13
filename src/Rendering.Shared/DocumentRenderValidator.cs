using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Contracts.Wsmtxca;
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
        if (identity.Servicio.Equals("wsfe", StringComparison.OrdinalIgnoreCase))
        {
            FecaeRequest request = DeserializeContract<FecaeRequest>(document.SnapshotRequest!);
            FecaeDetRequest detail = request.FeDetReq.Count == 1 && request.FeCabReq is not null
                ? request.FeDetReq[0] : throw InvalidSnapshot("La solicitud WSFE no contiene una cabecera y un único detalle.");
            RequirePaths(xml.Root!, ["FeCabReq", "PtoVta"], ["FeCabReq", "CbteTipo"], ["FeCabReq", "CantReg"],
                ["FeDetReq", "FECAEDetRequest", "DocTipo"], ["FeDetReq", "FECAEDetRequest", "DocNro"],
                ["FeDetReq", "FECAEDetRequest", "CbteDesde"], ["FeDetReq", "FECAEDetRequest", "CbteHasta"],
                ["FeDetReq", "FECAEDetRequest", "CbteFch"], ["FeDetReq", "FECAEDetRequest", "ImpTotal"],
                ["FeDetReq", "FECAEDetRequest", "ImpTotConc"], ["FeDetReq", "FECAEDetRequest", "ImpNeto"],
                ["FeDetReq", "FECAEDetRequest", "ImpOpEx"], ["FeDetReq", "FECAEDetRequest", "ImpIVA"],
                ["FeDetReq", "FECAEDetRequest", "ImpTrib"], ["FeDetReq", "FECAEDetRequest", "MonId"],
                ["FeDetReq", "FECAEDetRequest", "MonCotiz"]);
            VerifyIdentity(request.FeCabReq.CbteTipo, request.FeCabReq.PtoVta, detail.CbteDesde, request.FeCabReq.CantReg == 1 && detail.CbteDesde == detail.CbteHasta, identity);
            RequireDateValue(ParseDateValue(detail.CbteFch), document.Fecha!.Value);
            RequireEqualValue(detail.DocTipo, document.Receptor.CodigoTipoDocumento);
            RequireEqualValue(detail.DocNro.ToString(CultureInfo.InvariantCulture), document.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture));
            RequireEqualValue(detail.MonId, document.Moneda);
            RequireDecimalValue(Convert.ToDecimal(detail.MonCotiz!.Value, CultureInfo.InvariantCulture), document.Cotizacion);
            RequireDecimalValue(Convert.ToDecimal(detail.ImpTotal, CultureInfo.InvariantCulture), document.Total);
            RequireOptionalDecimal(Convert.ToDecimal(detail.ImpNeto, CultureInfo.InvariantCulture), document.Neto, "neto");
            RequireOptionalDecimal(Convert.ToDecimal(detail.ImpOpEx, CultureInfo.InvariantCulture), document.Exento, "exento");
            RequireOptionalDecimal(Convert.ToDecimal(detail.ImpTotConc, CultureInfo.InvariantCulture), document.NoGravado, "no gravado");
            RequireOptionalDecimal(Convert.ToDecimal(detail.ImpIva, CultureInfo.InvariantCulture), document.Iva, "IVA");
            RequireEqualValue(detail.CondicionIvaReceptorId, document.CodigoCondicionIvaReceptor);
            VerifyWsfeTaxes(detail, document);
            if (document.Detalles.Count != 0) throw InvalidSnapshot("WSFE no transmite conceptos individuales en el snapshot.");
        }
        else if (identity.Servicio.Equals("wsfex", StringComparison.OrdinalIgnoreCase))
        {
            ClsFexRequest request = DeserializeContract<ClsFexRequest>(document.SnapshotRequest!);
            RequirePaths(xml.Root!, ["Id"], ["Fecha_cbte"], ["Cbte_Tipo"], ["Punto_vta"], ["Cbte_nro"], ["Tipo_expo"],
                ["Moneda_Id"], ["Moneda_ctz"], ["Imp_total"], ["Cuit_pais_cliente"], ["Cliente"]);
            VerifyIdentity(request.CbteTipo, request.PuntoVta, request.CbteNro, true, identity);
            RequireDateValue(ParseDateValue(request.FechaCbte), document.Fecha!.Value);
            RequireEqualValue(request.MonedaId, document.Moneda);
            RequireDecimalValue(request.MonedaCtz ?? throw InvalidSnapshot("WSFEX no contiene cotización."), document.Cotizacion);
            RequireDecimalValue(request.ImpTotal, document.Total);
            RequireEqualValue(request.Cliente, document.Receptor.Nombre);
            RequireEqualValue(request.DomicilioCliente, document.Receptor.Domicilio);
            RequireEqualValue(request.CuitPaisCliente.ToString(CultureInfo.InvariantCulture), document.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture));
            RequireEqualValue(request.IdImpositivo, document.Receptor.IdentificacionFiscal);
            VerifyWsfexItems(request, document);
            if (document.Impuestos.Count != 0) throw InvalidSnapshot("WSFEX no incluye impuestos en el snapshot de autorización; no se pueden representar impuestos añadidos al modelo.");
        }
        else if (identity.Servicio.Equals("wsmtxca", StringComparison.OrdinalIgnoreCase))
        {
            ComprobanteType request = DeserializeContract<ComprobanteType>(document.SnapshotRequest!);
            RequirePaths(xml.Root!, ["codigoTipoComprobante"], ["numeroPuntoVenta"], ["numeroComprobante"], ["fechaEmision"],
                ["importeTotal"], ["codigoMoneda"], ["cotizacionMoneda"]);
            VerifyIdentity(request.CodigoTipoComprobante, request.NumeroPuntoVenta, request.NumeroComprobante, true, identity);
            if (request.FechaEmision is null) throw InvalidSnapshot("WSMTXCA no contiene fecha de emisión.");
            RequireDateValue(DateOnly.FromDateTime(request.FechaEmision.Value), document.Fecha!.Value);
            RequireEqualValue(request.CodigoMoneda, document.Moneda);
            RequireDecimalValue(request.CotizacionMoneda ?? throw InvalidSnapshot("WSMTXCA no contiene cotización."), document.Cotizacion);
            RequireDecimalValue(request.ImporteTotal, document.Total);
            RequireOptionalEqual(request.CodigoTipoDocumento?.ToString(CultureInfo.InvariantCulture), document.Receptor.CodigoTipoDocumento?.ToString(CultureInfo.InvariantCulture), "tipo de documento del receptor");
            bool legalConsumerFinalOmission = request.CodigoTipoDocumento is null && request.NumeroDocumento is null &&
                document.Receptor.CodigoTipoDocumento is null && document.Receptor.Identificacion == 0 &&
                identity.Tipo is 6 or 7 or 8 && document.CodigoCondicionIvaReceptor == 5;
            string? expectedDocumentNumber = legalConsumerFinalOmission
                ? null : document.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture);
            RequireOptionalEqual(request.NumeroDocumento?.ToString(CultureInfo.InvariantCulture), expectedDocumentNumber, "identificación del receptor");
            RequireOptionalEqual(request.CondicionIvaReceptor?.ToString(CultureInfo.InvariantCulture), document.CodigoCondicionIvaReceptor?.ToString(CultureInfo.InvariantCulture), "condición IVA del receptor");
            RequireOptionalDecimal(request.ImporteGravado, document.Neto, "neto");
            RequireOptionalDecimal(request.ImporteExento, document.Exento, "exento");
            RequireOptionalDecimal(request.ImporteNoGravado, document.NoGravado, "no gravado");
            VerifyWsmtxcaItems(request, document);
            VerifyWsmtxcaTaxes(request, document);
        }
        else throw new NotSupportedException($"No se puede validar el snapshot del servicio {identity.Servicio}.");
    }

    private static void VerifyResponse(ComprobanteGenerado document, XDocument response)
    {
        var identity = document.Identidad!;
        ResponseFacts facts = identity.Servicio.ToLowerInvariant() switch
        {
            "wsfe" => WsfeResponseFacts(DeserializeResponse<FecaeSolicitarResponse, FeCompConsultarResponse>(response)),
            "wsfex" => WsfexResponseFacts(DeserializeResponse<FexAuthorizeResponse, FexGetCmpResponse2>(response)),
            "wsmtxca" => WsmtxcaResponseFacts(DeserializeResponse<AutorizarComprobanteResponseType, ConsultarComprobanteResponseType>(response)),
            _ => throw new NotSupportedException($"No se puede validar la respuesta del servicio {identity.Servicio}.")
        };
        if (facts.Status != "A" || facts.AuthorizationType != "E" || facts.Code != document.Resultado!.CodigoAutorizacion ||
            facts.Expiry != document.Resultado.Vencimiento)
            throw InvalidSnapshot("La autorización, CAE o vencimiento no coincide con la respuesta tipada de ARCA.");
        VerifyIdentity(facts.Type, facts.PointOfSale, facts.Number, true, identity);
        // WSFE and WSMTXCA query contracts omit issuer CUIT; the typed response parser
        // returns null only for those official query shapes. Authorization responses and
        // WSFEX queries include identities and are checked where the contract provides them.
        if (facts.Cuit is long cuit && cuit != identity.Cuit) throw InvalidSnapshot("La CUIT de la respuesta no coincide con la identidad fiscal.");
        RequireResponseIfPresent(facts.ReceiverType, document.Receptor.CodigoTipoDocumento?.ToString(CultureInfo.InvariantCulture), "tipo de documento");
        RequireResponseIfPresent(facts.ReceiverId, document.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture), "identificación del receptor");
        RequireResponseIfPresent(facts.ReceiverFiscalId, document.Receptor.IdentificacionFiscal, "identificación fiscal del receptor");
    }

    private static void RequirePaths(XElement root, params string[][] paths)
    {
        foreach (string[] path in paths)
        {
            XElement? current = root;
            foreach (string name in path) current = current?.Elements().FirstOrDefault(element => element.Name.LocalName == name);
            if (current is null) throw InvalidSnapshot($"Falta un campo obligatorio del snapshot fiscal: {string.Join("/", path)}.");
        }
    }

    private static T DeserializeContract<T>(string xml) where T : class
    {
        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using var input = new StringReader(xml);
            using XmlReader reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1_048_576 });
            reader.MoveToContent();
            if (!serializer.CanDeserialize(reader)) throw InvalidSnapshot($"El elemento raíz no coincide con el contrato {typeof(T).Name}.");
            return (T)(serializer.Deserialize(reader) ?? throw InvalidSnapshot("El snapshot no contiene el objeto fiscal esperado."));
        }
        catch (InvalidOperationException exception) when (exception.InnerException is not null)
        { throw new InvalidOperationException("El snapshot no cumple el contrato XML esperado.", exception); }
        catch (XmlException exception) { throw new InvalidOperationException("El snapshot no es XML seguro y válido.", exception); }
    }

    private static object DeserializeResponse<TAuthorize, TQuery>(XDocument response)
        where TAuthorize : class where TQuery : class
    {
        var authorizeSerializer = new XmlSerializer(typeof(TAuthorize));
        using (XmlReader reader = response.CreateReader())
        {
            reader.MoveToContent();
            if (authorizeSerializer.CanDeserialize(reader))
                return authorizeSerializer.Deserialize(reader) ?? throw InvalidSnapshot("La respuesta de autorización está vacía.");
        }
        var querySerializer = new XmlSerializer(typeof(TQuery));
        using (XmlReader reader = response.CreateReader())
        {
            reader.MoveToContent();
            if (querySerializer.CanDeserialize(reader))
                return querySerializer.Deserialize(reader) ?? throw InvalidSnapshot("La respuesta de consulta está vacía.");
        }
        throw InvalidSnapshot("El nombre raíz y namespace de respuesta no coinciden con la operación fiscal admitida.");
    }

    private static ResponseFacts WsfeResponseFacts<T>(T response) where T : class
    {
        if (response is FecaeSolicitarResponse authorize)
        {
            FecaeResponse result = authorize.FecaeSolicitarResult ?? throw InvalidSnapshot("Falta el resultado FECAESolicitar.");
            FecaeCabResponse header = result.FeCabResp ?? throw InvalidSnapshot("Falta la cabecera FECAESolicitar.");
            FecaeDetResponse detail = result.FeDetResp.Count == 1 ? result.FeDetResp[0] : throw InvalidSnapshot("La respuesta FECAESolicitar debe contener exactamente un comprobante.");
            return new(header.Resultado, "E", detail.Cae, ParseDate(detail.CaeFchVto), header.CbteTipo, header.PtoVta,
                detail.CbteDesde, header.Cuit, detail.DocTipo.ToString(CultureInfo.InvariantCulture), detail.DocNro.ToString(CultureInfo.InvariantCulture), null);
        }
        if (response is FeCompConsultarResponse query)
        {
            FeCompConsResponse result = query.FeCompConsultarResult?.ResultGet ?? throw InvalidSnapshot("Falta el comprobante de la respuesta FECompConsultar.");
            return new(result.Resultado, result.EmisionTipo == "CAE" ? "E" : result.EmisionTipo, result.CodAutorizacion,
                ParseDate(result.FchVto), result.CbteTipo, result.PtoVta, result.CbteDesde, null,
                result.DocTipo.ToString(CultureInfo.InvariantCulture), result.DocNro.ToString(CultureInfo.InvariantCulture), null);
        }
        throw InvalidSnapshot("La respuesta WSFE no corresponde a FECAESolicitar ni FECompConsultar.");
    }

    private static ResponseFacts WsfexResponseFacts<T>(T response) where T : class
    {
        if (response is FexAuthorizeResponse authorize)
        {
            ClsFexOutAuthorize result = authorize.FexAuthorizeResult?.FexResultAuth ?? throw InvalidSnapshot("Falta FEXResultAuth.");
        return new(result.Resultado, "E", result.Cae, ParseDate(result.FchVencCae), result.CbteTipo,
                result.PuntoVta, result.CbteNro, result.Cuit, null, null, null);
        }
        if (response is FexGetCmpResponse2 query)
        {
            ClsFexGetCmpr result = query.FexGetCmpResult?.FexResultGet ?? throw InvalidSnapshot("Falta FEXResultGet.");
            return new(result.Resultado, "E", result.Cae, ParseDate(result.FchVencCae), result.CbteTipo,
                result.PuntoVta, result.CbteNro, null, null, result.CuitPaisCliente.ToString(CultureInfo.InvariantCulture), result.IdImpositivo);
        }
        throw InvalidSnapshot("La respuesta WSFEX no corresponde a FEXAuthorize ni FEXGetCMP.");
    }

    private static ResponseFacts WsmtxcaResponseFacts<T>(T response) where T : class
    {
        if (response is AutorizarComprobanteResponseType authorize)
        {
            ComprobanteCaeResponseType result = authorize.ComprobanteResponse ?? throw InvalidSnapshot("Falta comprobanteResponse de autorización WSMTXCA.");
            return new(authorize.Resultado is ResultadoSimpleType.A or ResultadoSimpleType.O ? "A" : authorize.Resultado.ToString(), "E",
                result.Cae.ToString(CultureInfo.InvariantCulture), DateOnly.FromDateTime(result.FechaVencimientoCae),
                result.CodigoTipoComprobante, result.NumeroPuntoVenta, result.NumeroComprobante, result.Cuit, null, null, null);
        }
        if (response is ConsultarComprobanteResponseType query)
        {
            ComprobanteType result = query.Comprobante ?? throw InvalidSnapshot("Falta comprobante de la respuesta consultarComprobante.");
            if (query.ArrayErrores.Count != 0 || result.CodigoTipoAutorizacion?.ToString() != "E" || result.CodigoAutorizacion is null)
                throw InvalidSnapshot("La respuesta de consulta WSMTXCA no acredita un CAE electrónico.");
            return new("A", "E", result.CodigoAutorizacion.Value.ToString(CultureInfo.InvariantCulture),
                result.FechaVencimiento is null ? null : DateOnly.FromDateTime(result.FechaVencimiento.Value),
                result.CodigoTipoComprobante, result.NumeroPuntoVenta, result.NumeroComprobante, null,
                result.CodigoTipoDocumento?.ToString(CultureInfo.InvariantCulture), result.NumeroDocumento?.ToString(CultureInfo.InvariantCulture), null);
        }
        throw InvalidSnapshot("La respuesta WSMTXCA no corresponde a autorizarComprobante ni consultarComprobante.");
    }

    private static void VerifyWsfeTaxes(FecaeDetRequest source, ComprobanteGenerado document)
    {
        var taxes = source.Tributos.Select(tax => (Code: tax.Id.ToString(CultureInfo.InvariantCulture), Amount: Convert.ToDecimal(tax.Importe, CultureInfo.InvariantCulture), Base: (decimal?)Convert.ToDecimal(tax.BaseImp, CultureInfo.InvariantCulture), Iva: false))
            .Concat(source.Iva.Select(tax => (Code: tax.Id.ToString(CultureInfo.InvariantCulture), Amount: Convert.ToDecimal(tax.Importe, CultureInfo.InvariantCulture), Base: (decimal?)Convert.ToDecimal(tax.BaseImp, CultureInfo.InvariantCulture), Iva: true))).ToArray();
        VerifyTaxes(taxes, document.Impuestos);
    }

    private static void VerifyWsfexItems(ClsFexRequest source, ComprobanteGenerado document)
    {
        if (source.Items.Count != document.Detalles.Count) throw InvalidSnapshot("La cantidad de conceptos WSFEX no coincide con el snapshot.");
        for (int index = 0; index < source.Items.Count; index++)
        {
            Item item = source.Items[index]; LineaComprobante line = document.Detalles[index];
            RequireEqualValue(item.ProDs ?? item.ProCodigo ?? string.Empty, line.Descripcion);
            RequireEqualValue(item.ProCodigo, line.Codigo);
            RequireOptionalDecimal(item.ProQty, line.Cantidad, "cantidad WSFEX");
            RequireOptionalDecimal(item.ProPrecioUni, line.PrecioUnitario, "precio WSFEX");
            RequireEqualValue(item.ProUmed, line.CodigoUnidadMedida);
            RequireDecimalValue(item.ProTotalItem, line.Importe);
        }
    }

    private static void VerifyWsmtxcaItems(ComprobanteType source, ComprobanteGenerado document)
    {
        if (source.ArrayItems.Count != document.Detalles.Count) throw InvalidSnapshot("La cantidad de conceptos WSMTXCA no coincide con el snapshot.");
        for (int index = 0; index < source.ArrayItems.Count; index++)
        {
            ItemType item = source.ArrayItems[index]; LineaComprobante line = document.Detalles[index];
            RequireEqualValue(item.Descripcion, line.Descripcion);
            RequireEqualValue(item.Codigo ?? item.CodigoMtx, line.Codigo);
            RequireOptionalDecimal(item.Cantidad, line.Cantidad, "cantidad WSMTXCA");
            RequireOptionalDecimal(item.PrecioUnitario, line.PrecioUnitario, "precio WSMTXCA");
            RequireEqualValue(item.CodigoUnidadMedida, line.CodigoUnidadMedida);
            RequireDecimalValue(item.ImporteItem, line.Importe);
        }
    }

    private static void VerifyWsmtxcaTaxes(ComprobanteType source, ComprobanteGenerado document)
    {
        var taxes = source.ArraySubtotalesIva.Select(tax => (Code: tax.Codigo.ToString(CultureInfo.InvariantCulture), Amount: tax.Importe, Base: (decimal?)null, Iva: true))
            .Concat(source.ArrayOtrosTributos.Select(tax => (Code: tax.Codigo.ToString(CultureInfo.InvariantCulture), Amount: tax.Importe, Base: (decimal?)tax.BaseImponible, Iva: false))).ToArray();
        VerifyTaxes(taxes, document.Impuestos);
    }

    private static void VerifyTaxes((string Code, decimal Amount, decimal? Base, bool Iva)[] source, IReadOnlyList<ImpuestoComprobante> document)
    {
        if (source.Length != document.Count) throw InvalidSnapshot("La cantidad de impuestos del modelo no coincide con el snapshot.");
        var available = document.ToList();
        foreach (var tax in source)
        {
            int index = available.FindIndex(candidate => candidate.Codigo == tax.Code && candidate.Importe == tax.Amount &&
                candidate.BaseImponible == tax.Base && (candidate.Tipo == TipoImpuestoComprobante.Iva) == tax.Iva);
            if (index < 0) throw InvalidSnapshot("Un impuesto del modelo no coincide con el snapshot fiscal.");
            available.RemoveAt(index);
        }
    }

    private static void VerifyIdentity(int type, int point, long number, bool structurallyValid, IdentidadComprobante identity)
    {
        if (!structurallyValid || type != identity.Tipo || point != identity.PuntoVenta || number != identity.Numero)
            throw InvalidSnapshot("La identidad del snapshot no coincide con la identidad del comprobante.");
    }

    private static void RequireDateValue(DateOnly actual, DateOnly expected)
    { if (actual != expected) throw InvalidSnapshot("La fecha no coincide con el snapshot fiscal."); }
    private static DateOnly ParseDateValue(string? value) => ParseDate(value) ?? throw InvalidSnapshot("La fecha del snapshot fiscal no tiene formato válido.");
    private static void RequireDecimalValue(decimal actual, decimal? expected, string field = "importe")
    { if (expected is null || actual != expected.Value) throw InvalidSnapshot($"El campo {field} no coincide con el snapshot fiscal."); }
    private static void RequireOptionalDecimal(decimal? actual, decimal? expected, string field)
    { if (actual != expected) throw InvalidSnapshot($"El campo {field} falta o no coincide con el snapshot fiscal."); }
    private static void RequireEqualValue<T>(T actual, T expected)
    { if (!EqualityComparer<T>.Default.Equals(actual, expected)) throw InvalidSnapshot("Un campo del modelo no coincide con el snapshot fiscal."); }
    private static void RequireOptionalEqual(string? actual, string? expected, string field)
    { if (!string.Equals(actual, expected, StringComparison.Ordinal)) throw InvalidSnapshot($"El campo {field} falta o no coincide con el snapshot fiscal."); }
    private static void RequireResponseIfPresent(string? actual, string? expected, string field)
    { if (actual is not null && !string.Equals(actual, expected, StringComparison.Ordinal)) throw InvalidSnapshot($"El campo {field} de la respuesta no coincide con el snapshot fiscal."); }

    private static DateOnly? ParseDate(string? value) => DateOnly.TryParseExact(value, ["yyyyMMdd", "yyyy-MM-dd", "yyyy-MM-dd'Z'", "yyyy-MM-ddzzz"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date) ? date : null;
    private static InvalidOperationException InvalidSnapshot(string message) => new(message);
    private sealed record ResponseFacts(string? Status, string? AuthorizationType, string? Code, DateOnly? Expiry,
        int Type, int PointOfSale, long Number, long? Cuit, string? ReceiverType, string? ReceiverId, string? ReceiverFiscalId);
}
