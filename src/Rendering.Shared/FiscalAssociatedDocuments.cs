using System.Globalization;
using System.Xml;
using System.Xml.Serialization;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.Documents;

namespace NetArcaWs.Rendering.Shared;

internal static class FiscalAssociatedDocuments
{
    internal static IReadOnlyList<string> Read(ComprobanteGenerado document)
    {
        if (document.Tipo != TipoComprobanteGenerado.Fiscal || document.SnapshotRequest is null) return [];
        IEnumerable<(int Type, int Point, long Number)> references = document.Identidad!.Servicio.ToLowerInvariant() switch
        {
            "wsfe" => Deserialize<FecaeRequest>(document.SnapshotRequest).FeDetReq.Single().CbtesAsoc
                .Select(item => (item.Tipo, item.PtoVta, item.Nro)),
            "wsfex" => Deserialize<ClsFexRequest>(document.SnapshotRequest).CmpsAsoc
                .Select(item => ((int)item.CbteTipo, item.CbtePuntoVta, item.CbteNro)),
            "wsmtxca" => Deserialize<ComprobanteType>(document.SnapshotRequest).ArrayComprobantesAsociados
                .Select(item => ((int)item.CodigoTipoComprobante, item.NumeroPuntoVenta, item.NumeroComprobante)),
            _ => []
        };
        return references.Select(reference =>
        {
            DocumentLayout layout = DocumentLayout.For(document.Identidad.Servicio, reference.Type);
            return $"{layout.Title} {reference.Point:00000}-{reference.Number:00000000}";
        }).ToArray();
    }

    private static T Deserialize<T>(string xml) where T : class
    {
        var serializer = new XmlSerializer(typeof(T));
        using var input = new StringReader(xml);
        using XmlReader reader = XmlReader.Create(input, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 1_048_576
        });
        reader.MoveToContent();
        if (!serializer.CanDeserialize(reader)) throw new InvalidOperationException("El snapshot de asociación no coincide con el contrato fiscal.");
        return (T)(serializer.Deserialize(reader) ?? throw new InvalidOperationException("El snapshot asociado está vacío."));
    }
}
