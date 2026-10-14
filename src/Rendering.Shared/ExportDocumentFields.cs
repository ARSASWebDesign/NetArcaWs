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
        string currency = request.MonedaId;
        string currencyName = currency.ToUpperInvariant() switch
        {
            "DOL" => "USD (dólares estadounidenses)",
            "PES" => "ARS (pesos)",
            _ => currency.ToUpperInvariant()
        };
        string? rate = request.MonedaCtz?.ToString("0.############################", CultureInfo.InvariantCulture);
        return new(request.CuitPaisCliente.ToString(CultureInfo.InvariantCulture), request.IdImpositivo,
            request.DstCmp.ToString(CultureInfo.InvariantCulture), currency, currencyName, rate,
            request.FormaPago, request.FechaPago, request.Incoterms, request.IncotermsDs,
            request.TipoExpo.ToString(CultureInfo.InvariantCulture), request.ObsComerciales, request.Obs);
    }
}
