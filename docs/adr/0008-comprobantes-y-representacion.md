# ADR 0008: separar emisión y representación de comprobantes

- Estado: aceptado; implementación local verificada con muestras sintéticas.
- Fecha: 2026-10-13.

## Contexto

La aplicación necesita conservar una respuesta durable de emisión y producir
HTML/PDF sin acoplar la autorización fiscal a la plantilla ni introducir
dependencias gráficas en el paquete base. Una representación puede repetirse
después de reiniciar el proceso y debe conservar el receptor y la identidad
fiscal correlacionados con el snapshot autorizado.

## Decisión

`ArcaWsComprobantes` reutiliza la emisión durable existente para CAE unitario
WSFEv1, WSFEXv1 y WSMTXCA, y produce recibos X localmente. `ComprobanteGenerado`
es el snapshot que la aplicación puede serializar y persistir. Los paquetes
opcionales `NetArcaWs.Rendering.Html` y `NetArcaWs.Rendering.Pdf` aceptan ese
modelo; no acceden a red, BD o archivos de la aplicación ni reenvían
operaciones. Sólo representan un resultado fiscal autorizado y completo; X
incluye la leyenda no fiscal y carece de CAE y QR fiscal.

El receptor del documento proviene siempre de `ComprobanteGenerado`. La
aplicación conserva el perfil `DatosEmisor` usado en esa ocasión, incluyendo
logo y nombres, asociado al snapshot si necesita reproducir su presentación.
WSFE conserva totales sin inventar líneas; WSFEX y WSMTXCA muestran el detalle
presente en el request autorizado. La fachada no cubre WSFECred, CAEA, emisión
batch durable ni el PDF de WSCPE.

PDF se genera localmente con PDFsharp/MigraDoc, fuentes Noto Sans OFL y
normalización PNG en memoria. Su resolver de fuente PDFsharp es global al
proceso e inmutable: se instala al primer uso y no reemplaza otro resolver ya
configurado. Datos de emisor/logo no se almacenan globalmente. El core no
depende de PDFsharp, QRCoder ni StbImageSharp.

Los paquetes opcionales usan versión de proyecto `0.6.0`, sin modificar los
paquetes ya publicados ni declarar públicamente publicados estos nuevos IDs.
La automatización de release incluye 13 IDs y verifica hashes; CI consume los
paquetes desde un feed local en un proyecto aislado. La publicación de paquetes
y wiki continúa sujeta al proceso de entrega.

## Consecuencias

- La aplicación protege el snapshot fiscal, sus datos de receptor y el perfil
  histórico del emisor. Los renderizadores no proporcionan un almacén.
- La interfaz no habilita tipos más allá de las combinaciones que validan los
  adaptadores. Una muestra sintética no acredita autorización ARCA.
- El usuario puede instalar únicamente el renderer requerido; el core conserva
  una instalación libre de dependencias PDF/QR.
- Cambiar un logo o perfil de emisor no reescribe el snapshot de emisión. Para
  una salida histórica se debe reutilizar el perfil que la aplicación guardó.
