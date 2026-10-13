using System.Globalization;
using System.Text;
using System.Text.Json;
using NetArcaWs.Documents;

namespace NetArcaWs.Rendering.Shared;

internal static class FiscalQrPayload
{
    internal const string BaseUrl = "https://www.arca.gob.ar/fe/qr/";

    internal static string Create(ComprobanteGenerado document, DatosEmisor issuer)
    {
        var identity = document.Identidad!;
        ValidateQrBounds(document, identity, issuer);
        var values = new Dictionary<string, object?>
        {
            ["ver"] = 1,
            ["fecha"] = document.Fecha!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["cuit"] = identity.Cuit,
            ["ptoVta"] = identity.PuntoVenta,
            ["tipoCmp"] = identity.Tipo,
            ["nroCmp"] = identity.Numero,
            ["importe"] = decimal.Round(document.Total, 2),
            ["moneda"] = document.Moneda,
            ["ctz"] = decimal.Round(document.Cotizacion, 6),
            ["tipoCodAut"] = "E",
            ["codAut"] = long.Parse(document.Resultado!.CodigoAutorizacion!, CultureInfo.InvariantCulture)
        };
        int? docType = document.Receptor.CodigoTipoDocumento;
        string? recipientNumber = document.Receptor.IdentificacionFiscal ?? document.Receptor.Identificacion.ToString(CultureInfo.InvariantCulture);
        if (docType is not null)
        {
            if (string.IsNullOrEmpty(recipientNumber) || recipientNumber.Length > 20 ||
                recipientNumber.Any(character => !char.IsAsciiDigit(character)) ||
                !decimal.TryParse(recipientNumber, NumberStyles.None, CultureInfo.InvariantCulture, out decimal numericId))
                throw new InvalidOperationException("El número de documento del receptor no es válido para el QR de ARCA.");
            values["tipoDocRec"] = docType.Value;
            values["nroDocRec"] = numericId;
        }
        string json = JsonSerializer.Serialize(values);
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return BaseUrl + "?p=" + encoded;
    }

    private static void ValidateQrBounds(ComprobanteGenerado document, IdentidadComprobante identity, DatosEmisor issuer)
    {
        if (issuer.Identificacion is < 10_000_000_000 or > 99_999_999_999 || identity.PuntoVenta is < 1 or > 99_999 ||
            identity.Tipo is < 1 or > 999 || identity.Numero is < 1 or > 99_999_999 ||
            document.Total < 0 || document.Total > 9_999_999_999_999.99m || document.Cotizacion <= 0 ||
            document.Cotizacion > 9_999_999_999_999.999999m || decimal.Round(document.Total, 2) != document.Total ||
            decimal.Round(document.Cotizacion, 6) != document.Cotizacion)
            throw new InvalidOperationException("Los datos del comprobante exceden los límites numéricos definidos para el QR de ARCA.");
    }
}
