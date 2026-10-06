# WSFECred: gestión de Factura de Crédito Electrónica MiPyME

`WsfecredService` implementa el contrato SOAP `FECredService`, usado para
gestionar el ciclo de Facturas de Crédito Electrónicas MiPyME y sus cuentas
corrientes. Es una superficie distinta de WSFEv1 y WSMTXCA: esos servicios
autorizan comprobantes electrónicos; WSFECred gestiona acciones posteriores
como aceptación o rechazo, cancelaciones, ajustes, consulta de estados y
operaciones relacionadas con Agentes de Depósito Colectivo.

## Alcance de las operaciones

El snapshot WSDL de producción fijado en
[`docs/reference/contracts`](../reference/contracts/sources.md) define 21
operaciones. Incluye `dummy`; aceptación y rechazo de FECred, rechazo de notas
de débito/crédito, cancelación total, informar facturas a un Agente de Depósito
Colectivo y modificar la opción de transferencia; además de consultas de
comprobantes, cuentas corrientes, obligado a recepción, retenciones, motivos y
formas, remitos, historiales de estado, ajustes y montos de recepción. La
[referencia SOAP completa](../reference/operations/wsfecred.md) detalla el
request y response tipado de cada operación.

El manual oficial diferencia errores excepcionales, de formato y de negocio, y
observaciones/eventos de negocio. No se deben reducir a un booleano genérico de
éxito. Algunos métodos alteran el estado de una factura o cuenta corriente; si
una llamada de escritura termina con timeout, su resultado puede ser incierto.
No la reenvíes automáticamente. Consultá el estado/historial por las
operaciones disponibles y resolvé el caso según la semántica del método y las
reglas vigentes. Las fachadas no aceptan una clave de idempotencia ni tienen
diario/reconciliación integrados para WSFECred; la aplicación debe conservar la
intención y su clave propia de operación, deduplicar solicitudes y resolver el
resultado mediante consultas. `SafeInvoiceService` no orquesta este ciclo.

## Registro y contexto multitenant

`AddNetArcaWs` registra `IWsfecredService`. Las operaciones autenticadas usan el
servicio WSAA `wsfecred`, endpoint separado por ambiente, y reciben el
`ArcaTenantContext` por llamada. La aplicación debe autorizar al tenant y
resolver CUIT representada, ambiente y certificado antes de invocar.

El ejemplo compilable de consulta está en
[GuideExamples.ReadCreditRetentionTypesAsync](../../examples/NetArcaWs.Examples/GuideExamples.cs#L31).
El host debe proporcionar un `ArcaTenantContext` autorizado y un
`CancellationToken`.

La biblioteca transporta los tipos del contrato, pero no decide si una empresa
está alcanzada por el régimen, calcula plazos, interpreta elegibilidad, ni
administra el proceso registral web. La normativa y los plazos pueden cambiar;
confirmalos al implementar reglas de negocio.

## Health checks y límites

`AddWsfecredHealthCheck` registra el probe `dummy` para el ambiente elegido. Es
una comprobación de disponibilidad de infraestructura y no verifica el TA,
autorización para la CUIT, habilitación para operar o estados de facturas.

Los contratos se generan desde el WSDL versionado y se verifican localmente;
esto no acredita llamadas autenticadas reales ni homologación funcional. El
manual define el servicio, estados y validaciones de negocio; las decisiones de
transacción deben respetar el estado devuelto por ARCA.

## Fuentes oficiales

- [Manual del desarrollador WSFECred](https://servicioscf.afip.gob.ar/facturadecreditoelectronica/documentos/Manual-Desarrollador-WSFECRED.pdf)
- [Guía ARCA de registro de Facturas de Crédito Electrónicas MiPyME](https://serviciosweb.arca.gob.ar/genericos/guiasPasoPaso/VerGuia.aspx?id=301)
