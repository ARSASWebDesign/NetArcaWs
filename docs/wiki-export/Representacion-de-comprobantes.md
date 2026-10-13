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

Para obtener un snapshot fiscal, la aplicación construye un `ArcaTenantContext`
autorizado y llama la sobrecarga tipada de `ArcaWsComprobantes` para WSFEv1,
WSFEXv1 o WSMTXCA. La emisión requiere el `SafeInvoiceService` configurado y
persistido por la aplicación; conserva la clave de idempotencia y las reglas de
reconciliación del [diario fiscal](Diario-fiscal). `ReciboX` es una creación
local. El [ejemplo ejecutable de galería](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/examples/NetArcaWs.Rendering.Examples/Program.cs)
usa únicamente snapshots sintéticos y no llama a ARCA.

Conservá el snapshot y el perfil de emisor que se usó con el comprobante. Este
ejemplo completo crea un recibo X local con datos sintéticos, persiste el
snapshot y el perfil como JSON, los vuelve a cargar y genera HTML y PDF. En
producción usá el almacenamiento protegido que administre tu aplicación:

```csharp
using System;
using System.IO;
using System.Text.Json;
using NetArcaWs.Documents;
using NetArcaWs.Rendering.Html;
using NetArcaWs.Rendering.Pdf;

string dataDirectory = Path.Combine(AppContext.BaseDirectory, "datos-ejemplo");
var perfilEmisor = new DatosEmisor(
    identificacion: 30123456789,
    razonSocial: "Comercial Ejemplo SRL",
    nombreFantasia: "La Tienda",
    domicilio: "Av. Ejemplo 123, Córdoba",
    condicionTributaria: "Responsable Inscripto",
    ingresosBrutos: "30-12345678-9",
    inicioActividades: new DateOnly(2018, 4, 1));
ComprobanteGenerado comprobante = new ReciboX(perfilEmisor).GenerarComprobante(new DatosReciboX(
    NumeroInterno: "X-0001",
    Fecha: new DateOnly(2026, 10, 13),
    Receptor: new DatosReceptor(20304050607, "Cliente de ejemplo", "San Martín 456, Córdoba", "Responsable Inscripto"),
    Moneda: "ARS",
    Total: 12100m,
    Detalles: [new LineaReciboX("Servicio de ejemplo", 12100m, "ARS")],
    Pagos: [new PagoReciboX("Transferencia", 12100m, "ARS")]));
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true
};
Directory.CreateDirectory(dataDirectory);
string documentPath = Path.Combine(dataDirectory, "comprobante.json");
string issuerPath = Path.Combine(dataDirectory, "emisor.json");
await File.WriteAllTextAsync(documentPath, JsonSerializer.Serialize(comprobante, jsonOptions));
await File.WriteAllTextAsync(issuerPath, JsonSerializer.Serialize(perfilEmisor, jsonOptions));

ComprobanteGenerado comprobanteCargado = JsonSerializer.Deserialize<ComprobanteGenerado>(
    await File.ReadAllTextAsync(documentPath), jsonOptions)
    ?? throw new InvalidDataException("No se pudo leer el snapshot del comprobante.");
DatosEmisor emisorCargado = JsonSerializer.Deserialize<DatosEmisor>(
    await File.ReadAllTextAsync(issuerPath), jsonOptions)
    ?? throw new InvalidDataException("No se pudo leer el perfil histórico del emisor.");

string sampleDirectory = Path.Combine(dataDirectory, "muestras-renderizadas");
Directory.CreateDirectory(sampleDirectory);

string html = new ArcaWsGeneradorHTML(emisorCargado)
    .GenerarHTMLComprobante(comprobanteCargado);
byte[] pdf = new ArcaWsGeneradorPDF(emisorCargado)
    .GenerarPDFComprobante(comprobanteCargado);
await File.WriteAllTextAsync(Path.Combine(sampleDirectory, "comprobante.html"), html);
await File.WriteAllBytesAsync(Path.Combine(sampleDirectory, "comprobante.pdf"), pdf);
```

Para emitir un documento fiscal, reemplazá la creación local de `ReciboX` por
la sobrecarga tipada de `ArcaWsComprobantes` y un `ArcaTenantContext` que la
aplicación ya haya autorizado. Ese flujo fiscal requiere el `SafeInvoiceService`
persistente y una clave de idempotencia; el ejemplo de arriba no emite ni
consulta comprobantes en ARCA.

En el proyecto consumidor agregá sólo los paquetes que vas a usar:
`NetArcaWs.Rendering.Html` y/o `NetArcaWs.Rendering.Pdf`, además de `NetArcaWs`
para obtener y conservar el modelo. Los renderizadores no autorizan, consultan,
persisten ni reenvían comprobantes; la aplicación controla el almacenamiento y
el acceso a información fiscal y personal.

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
