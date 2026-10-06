# WSCDC: constatación de comprobantes

`WscdcService` implementa el contrato SOAP de Constatación de Comprobantes
(WSCDC v1). Recibe los datos que identifican un comprobante y consulta si está
registrado y autorizado, o si todavía no fue rendido. La respuesta de
constatación no sustituye la factura original ni la autorización del emisor.
ARCA indica que puede invocarlo el emisor, el receptor u otro cliente con acceso
al servicio y los datos mínimos de identificación.

## Operaciones del contrato

El cliente se genera a partir del WSDL de producción fijado en
[`docs/reference/contracts`](../reference/contracts/sources.md). Incluye seis
operaciones: `ComprobanteConstatar`, `ComprobantesModalidadConsultar`,
`ComprobantesTipoConsultar`, `DocumentosTipoConsultar`,
`OpcionalesTipoConsultar` y `ComprobanteDummy`. La
[referencia por operación](../reference/operations/wscdc.md) documenta
requests, responses, QName y SOAPAction del snapshot.

Las cinco operaciones de consulta/constatación reciben `ArcaTenantContext` y
usan el servicio WSAA `wscdc`. `ComprobanteDummy` no requiere ticket y solo
comprueba la disponibilidad de infraestructura. El manual define errores de
formato y funcionales; un comprobante no encontrado o rechazado debe
interpretarse con la respuesta del contrato y no como un fallo de transporte.

## Registro e invocación

`AddNetArcaWs` registra `IWscdcService`. La aplicación debe resolver y autorizar
el tenant y obtener la CUIT representada, el ambiente y el certificado de una
fuente confiable antes de llamar.

El ejemplo compilable de consulta está en
[GuideExamples.ReadVerificationModesAsync](../../examples/NetArcaWs.Examples/GuideExamples.cs#L25).
El host debe proporcionar un `ArcaTenantContext` autorizado y un
`CancellationToken`.

## Health checks y límites

Registra el probe con `AddWscdcHealthCheck`. El check `ComprobanteDummy` mide
disponibilidad para el ambiente elegido; no comprueba certificados, permisos,
acceso de una CUIT ni la capacidad de constatar un comprobante real. Ver
[health checks](Health-checks.md) para configuración y etiquetas.

El contrato local, los mapeos SOAP y la serialización se verifican con
pruebas locales. Eso no equivale a una constatación de negocio autenticada en
homologación o producción. La aplicación conserva la responsabilidad de
interpretar el resultado, proteger los datos fiscales y atender la normativa
aplicable.

## Fuentes oficiales

- [Manual del desarrollador WSCDC, revisión 2025-12-01](https://www.afip.gob.ar/ws/WSCDCV1/WSCDC-manual-desarrollador-v4.pdf)
- [Catálogo ARCA de otros servicios web](https://www.arca.gob.ar/ws/documentacion/catalogo.asp)
- [Arquitectura y autenticación de servicios SOAP](https://www.arca.gob.ar/ws/documentacion/arquitectura-general.asp)
