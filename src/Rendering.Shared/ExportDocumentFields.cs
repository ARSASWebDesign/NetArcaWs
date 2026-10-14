using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using NetArcaWs.Contracts.WsfexV1;

namespace NetArcaWs.Rendering.Shared;

/// <summary>Presentation fields read from the validated frozen WSFEX request snapshot.</summary>
internal sealed record ExportDocumentFields(
    string? CuitPais,
    string? ForeignTaxId,
    string? DestinationCountryCode,
    string CurrencyCode,
    string CurrencyName,
    string? ExchangeRate,
    string? PaymentMethod,
    string? PaymentDate,
    string? Incoterms,
    string? IncotermsDescription,
    string? ExportCondition,
    string? CommercialObservations,
    string? Observations)
{
    internal static ExportDocumentFields? Read(string? service, string? snapshot)
    {
        if (!string.Equals(service, "wsfex", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(snapshot))
            return null;
        var serializer = new XmlSerializer(typeof(ClsFexRequest));
        using var input = new StringReader(snapshot);
        using XmlReader reader = XmlReader.Create(input, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 1_048_576
        });
        reader.MoveToContent();
        if (!serializer.CanDeserialize(reader))
            throw new InvalidOperationException("El snapshot WSFEX no coincide con el contrato esperado.");
        ClsFexRequest request = (ClsFexRequest)(serializer.Deserialize(reader)
            ?? throw new InvalidOperationException("El snapshot WSFEX está vacío."));
        string currencyCode = CurrencyDisplay.Code(request.MonedaId);
        string? rate = request.MonedaCtz?.ToString("0.############################", CultureInfo.InvariantCulture);
        string? paymentDate = DateOnly.TryParseExact(request.FechaPago, "yyyyMMdd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateOnly parsedPaymentDate) ? parsedPaymentDate.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-AR")) : request.FechaPago;
        string exportType = request.TipoExpo switch
        {
            1 => "Bienes",
            2 => "Servicios",
            4 => "Otros",
            _ => $"Código {request.TipoExpo.ToString(CultureInfo.InvariantCulture)}"
        };
        return new(request.CuitPaisCliente.ToString(CultureInfo.InvariantCulture), request.IdImpositivo,
            request.DstCmp.ToString(CultureInfo.InvariantCulture), currencyCode, CurrencyDisplay.Label(currencyCode), rate,
            request.FormaPago, paymentDate, request.Incoterms, request.IncotermsDs,
            exportType, request.ObsComerciales, request.Obs);
    }
}
