<!-- Source: docs/wiki/Representacion-de-comprobantes.md. Generated wiki mirror; edit the repository source. -->

# Representación de comprobantes

NetArcaWs separa la autorización fiscal de la salida visual. La fachada
`ArcaWsComprobantes` reutiliza el diario durable para CAE unitario WSFEv1,
WSFEXv1 y WSMTXCA; `ReciboX` crea un recibo local sin llamada fiscal. El snapshot
`ComprobanteGenerado` puede serializarse y persistirse por la aplicación.

Instalá `NetArcaWs.Rendering.Html`, `NetArcaWs.Rendering.Pdf` o ambos según los
formatos que necesite el consumidor. Cada renderer toma `DatosEmisor` y un
snapshot. El receptor se toma exclusivamente del snapshot; el emisor no se
deduce del certificado. Para volver a producir una representación histórica,
guardá el perfil de emisor y el logo usados junto al comprobante.

```csharp
ComprobanteGenerado comprobante = await emision.GenerarComprobanteAsync(
    tenantAutorizado, claveIdempotencia, solicitud, presentacion, cancellationToken);
await repositorio.GuardarAsync(comprobante, cancellationToken);
await repositorio.GuardarPerfilEmisorAsync(perfilEmisorActual, cancellationToken);

string html = new ArcaWsGeneradorHTML(perfilEmisorActual)
    .GenerarHTMLComprobante(comprobante);
byte[] pdf = new ArcaWsGeneradorPDF(perfilEmisorActual)
    .GenerarPDFComprobante(comprobante);
```

El ejemplo representa el snapshot ya obtenido; llamar a los renderizadores no
autoriza, consulta, persiste ni reenvía un comprobante. La aplicación controla
el almacenamiento y acceso a información fiscal y personal.

## Alcance y datos disponibles

| Origen | Datos representados | Límite |
| --- | --- | --- |
| WSFEv1 | Identidad, receptor, fechas, totales, IVA/tributos, autorización y QR cuando están correlacionados | El contrato fuente no trae ítems; la vista muestra el resumen fiscal y no inventa productos, cantidades o precios. |
| WSFEXv1 | Comprador/receptor, moneda, cotización, autorización, QR e ítems del request | Sólo se admiten tipos de exportación validados por el adaptador. |
| WSMTXCA | Receptor, conceptos, importes, impuestos, autorización, QR y detalle autorizado | Sólo se admiten tipos y campos que el adaptador correlaciona con las respuestas. |
| Recibo X | Receptor, número interno, concepto, pagos e imputaciones | Es un recibo local, no una factura ni documento de traslado. Presenta `DOCUMENTO NO VALIDO COMO FACTURA`; no incluye CAE, CAEA o QR fiscal. |

La fachada cubre CAE unitario. No cubre WSFECred, CAEA, batch durable ni el PDF
de WSCPE. Un estado rechazado, incierto, conflictivo o incompleto no se imprime
como autorizado. La representación no constituye prueba independiente de una
autorización real.

## Paquetes y comportamiento

`NetArcaWs` no depende de paquetes de PDF o QR. HTML entrega un documento
autónomo con estilos e imágenes embebidos. PDF usa PDFsharp/MigraDoc y Noto Sans
2.015 bajo SIL OFL 1.1; el aviso acompaña el paquete. PDFsharp requiere resolver
global de fuentes: el paquete instala su resolver Noto Sans inmutable en el
primer uso y falla si el proceso ya configuró otro. No guarda perfiles ni logos
del emisor en ese estado global. PNG se normaliza en memoria con StbImageSharp;
no se invoca System.Drawing, aunque QRCoder declare System.Drawing.Common en su
árbol de dependencias.

La implementación de la rama tiene verificación local sintética. Los paquetes
renderer todavía no se declaran disponibles en NuGet y las muestras no prueban
homologación, permisos del certificado ni validez fiscal.

Consultá [diario fiscal](Diario-fiscal), [releases](Publicar-versiones) y
[ADR 0008](ADR-0008-comprobantes-y-representacion).
