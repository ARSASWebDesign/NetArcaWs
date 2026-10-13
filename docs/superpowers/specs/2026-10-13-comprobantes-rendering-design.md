# Issue 33: emisión y representación de comprobantes

Estado: diseño aprobado por el usuario el 13 de octubre de 2026; implementado en
la rama local `codex/33-comprobantes-rendering`, con verificación sintética
local reportada en Task 6. La implementación y el smoke test de paquetes no
acreditan homologación ARCA ni disponibilidad pública de los nuevos IDs NuGet.

## Objetivo y decisiones acordadas

Separar emisión de representación. La emisión devuelve un nuevo modelo que reúne
los datos enviados y el resultado de ARCA, apto para almacenar en la BD del
consumidor. Dos generadores opcionales consumen ese modelo y devuelven HTML o
PDF, sin otra autorización fiscal. El receptor proviene del comprobante y nunca
de la configuración del generador.

El generador se inicializa con los datos del emisor y un logo opcional. El mismo
flujo sirve para comprobantes fiscales y recibos X, compartiendo disposición
visual y variando los datos y reglas según el tipo de documento.

## Paquetes y API

- `NetArcaWs`: modelos persistibles y fachada de generación/emisión que reutiliza
  los clientes y la emisión durable actuales.
- `NetArcaWs.Rendering.Html`: `ArcaWsGeneradorHTML`, constructor con
  `DatosEmisor`, método `string GenerarHTMLComprobante(ComprobanteGenerado datos)`.
- `NetArcaWs.Rendering.Pdf`: `ArcaWsGeneradorPDF`, mismo constructor y método
  `byte[] GenerarPDFComprobante(ComprobanteGenerado datos)`.

La fachada ofrece `GenerarComprobanteAsync` con sobrecargas tipadas para WSFE,
WSFEX y WSMTXCA CAE unitarios y una operación local para recibos X. La emisión
fiscal permanece asíncrona y acepta cancelación. Se conservan los clientes y
retornos existentes; la fachada es aditiva. La representación es síncrona y no
realiza accesos a red, BD ni archivos del consumidor.

Los renderizadores comparten modelos y reglas de presentación sin introducir
dependencias PDF/QR en el core. HTML no depende de PDF, ni PDF de HTML. Las
dependencias externas y recursos de fuentes se aíslan en los paquetes opcionales
que los necesitan, con licencias gratuitas compatibles, avisos y lockfiles.
PDF se genera en .NET, sin navegador, Python o servicios remotos. La selección
de versiones y fuentes verificadas se documentará en el plan de implementación.

## Modelo persistible

`ComprobanteGenerado` es un snapshot versionado, serializable mediante
`System.Text.Json`, con discriminador fiscal/X y colecciones defensivas:

- Identidad del documento, servicio, ambiente, tenant interno y estado.
- Datos originales de emisión sin credenciales, conservando el contrato fiscal
  y su versión; no cambiar el snapshot ni el hash del diario existente.
- Receptor: identificación, nombre/razón social, domicilio y condición tributaria
  cuando correspondan. El consumidor aporta los datos que ARCA no devuelve.
- Conceptos, importes, moneda, cotización, impuestos, fechas y asociaciones.
- Para fiscales: identidad correlacionada, resultado, autorización, vencimiento,
  observaciones y errores de ARCA sin Token/Sign.
- Para X: número interno, concepto de cobro, medios de pago e imputaciones a
  facturas; sin campos de autorización fiscal aplicables.

Los DTO SOAP mantienen sus primitivas originales. La proyección de presentación
no recalcula ni modifica silenciosamente los valores fiscales. Importes locales
y de recibos X usan `decimal`; conversiones de primitivas SOAP tienen validación
explícita y pruebas de cultura y precisión.

El modelo se puede persistir y deserializar antes de generar HTML/PDF. La
aplicación es responsable de guardar el snapshot y proteger su acceso. No se
agrega un esquema obligatorio de persistencia ni se convierten los renderizadores
en un nuevo diario fiscal. Su `ToString` no expone información personal o fiscal.

## Emisor, receptor y marca

`DatosEmisor` contiene CUIT/identificación, razón social, nombre de fantasía
opcional, domicilio, condición frente al IVA, ingresos brutos e inicio de
actividades cuando correspondan; logo opcional como bytes PNG con copia defensiva.
Se limita tamaño y dimensiones y se verifica el contenido. No se descargan logos
desde URLs ni se interpretan SVG/HTML suministrados por el consumidor.

El renderizador se vincula a un emisor inmutable. Para un comprobante fiscal,
valida que la CUIT configurada coincida con la CUIT representada del documento.
No deduce esa identidad de la CUIT del certificado. No hay receptor predeterminado:
todo dato del receptor se toma del snapshot del comprobante.

Con imagen se muestra el logo y los datos legales. Sin imagen se utiliza el nombre
de fantasía y la razón social; si no hay nombre de fantasía, la razón social.
La razón social se muestra también cuando existe logo. Para reproducir una
representación histórica, el consumidor conserva el perfil de emisor y logo
utilizados junto al comprobante; cambiar la configuración no altera el snapshot
fiscal. El ejemplo documentará esta asociación.

## Plantilla y validación

Una plantilla habitual A4 comparte cabecera, partes, identificación, detalle,
totales y pie. Los documentos extensos tienen paginación sin superposición y
cabeceras identificables; HTML dispone de estilos de impresión. La salida es
autónoma, sin fuentes, imágenes ni scripts remotos. Todo texto del usuario se
escapa en HTML y se representa como texto en PDF.

Para fiscales, los adaptadores de cada servicio construyen el bloque de
autorización y los datos del QR oficial a partir de valores correlacionados del
request y la respuesta. El QR se genera localmente y su contenido se valida con
las especificaciones vigentes de ARCA. No se imprime como autorizado un resultado
rechazado, incierto, en conflicto o incompleto; los métodos fallan explícitamente.
La representación no acredita por sí misma una autorización real.

Para X se muestran la letra X y `DOCUMENTO NO VALIDO COMO FACTURA`, destacadas en
el centro superior, sin CAE, CAEA ni QR fiscal. Las opciones ordinarias no permiten
ocultar esos elementos. Importe y medios de pago deben coincidir; imputaciones
positivas no pueden superar lo recibido y usan la misma moneda, sin conversiones
implícitas. El recibo no sustituye facturas ni documentos de traslado.

La API falla explícitamente ante tipos no soportados. El alcance inicial de
emisión es CAE unitario WSFE/WSFEX/WSMTXCA y recibo X; no incorpora batch durable,
CAEA, WSFECred ni rediseña el PDF provisto por WSCPE. Cada tipo fiscal habilitado
requiere validar sus datos y leyendas vigentes antes de declararlo soportado.

## Pruebas y muestras para revisión

- Pruebas sintéticas de emisión simulada, correlación y preservación de respuestas;
  repetir la representación no emite ni consulta ARCA.
- Round-trip JSON del modelo, protección frente a mutaciones y recuperación desde
  BD del consumidor en un ejemplo local.
- Pruebas de receptor, emisor incompatible, importes, culturas, Unicode, fechas,
  estados no autorizados, escape HTML y PNG inválido o excesivo.
- QR: validar payload oficial, decodificar muestras y contrastar con datos fiscales.
- Muestras HTML/PDF para los tres adaptadores fiscales y X total/parcial, con
  referencias múltiples, documento multipágina y textos largos.
- Casos con PNG cuadrado que diga `LOGO`, nombre de fantasía y razón social;
  sin logo con ambos nombres; y sin logo ni fantasía con razón social.
- Conservar salidas en `artifacts/comprobantes/<ejecucion>/`, fuera de Git, con
  manifiesto de escenarios y hora de creación. No borrar salidas con antigüedad
  menor a 72 horas. Limpieza de muestras vencidas en ejecuciones posteriores;
  CI conserva el artefacto de muestras durante tres días.
- Abrir y revisar visualmente HTML y páginas PDF, incluyendo logo, leyendas, QR,
  paginación y ausencia de recortes. Entregar rutas locales de las muestras.

Verificar restore locked, build/test Release, empaquetado, consumo de los paquetes
opcionales y documentación/wiki generada con herramientas .NET del repositorio.
Pruebas locales sintéticas no equivalen a homologación ARCA y no requieren
activar credenciales ni enviar nuevos comprobantes reales.

## Entrega

Rama propia y PR hacia `main`, con ejemplo compilable de emisión, persistencia y
representación. Actualizar README, arquitectura, progreso, ADR de la separación y
wiki. Incorporar los paquetes a las verificaciones y al inventario de release sin
publicar una release, cambiar tags o publicar la wiki como parte de esta tarea.

## Fuentes normativas

- [Recibos X y situaciones especiales](https://www.afip.gob.ar/facturacion/comprobantes/situaciones-especiales.asp).
- [Datos de comprobantes electrónicos](https://www.afip.gob.ar/fe/emision-autorizacion/datos-comprobantes.asp).
- [QR oficial y enlace a especificaciones](https://www.afip.gob.ar/fe/qr/).
- [RG 1415, texto actualizado](https://www.argentina.gob.ar/normativa/nacional/resoluci%C3%B3n-1415-2003-81316/actualizacion).

Consultar los contratos y requisitos oficiales vigentes por cada tipo fiscal
durante la implementación; no usar la plantilla como sustituto de esa revisión.
