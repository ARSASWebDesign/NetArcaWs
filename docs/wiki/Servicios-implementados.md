# Servicios implementados

Esta página resume para qué sirve cada cliente .NET y dónde consultar el detalle
de sus operaciones. Los métodos SOAP, requests y responses se documentan en las
referencias por operación, generadas desde WSDL identificados en
[proveniencia de contratos](../reference/contracts/sources.md). El contrato
describe el intercambio XML; el manual ARCA define el uso funcional, las
condiciones de acceso y las validaciones de negocio.

## Clientes disponibles

| Servicio | Rol y funcionalidad | Referencia de operaciones | Manual/catálogo oficial |
|---|---|---|---|
| **WSAA** | Autentica una identidad para un servicio y ambiente, firma el TRA y obtiene el TA. La biblioteca reutiliza tickets válidos en su caché local. | [Guía WSAA y certificados](WSAA-y-certificados.md), [contexto multitenant](Contexto-multitenant.md) | [Documentación oficial WSAA](https://www.afip.gob.ar/ws/documentacion/wsaa.asp), [especificación técnica](https://www.afip.gob.ar/ws/WSAA/Especificacion_Tecnica_WSAA_1.2.0.pdf) |
| **WSFEv1** | Facturación electrónica del mercado interno; incluye autorización, consultas, CAEA y consulta de parámetros del contrato. | [Operaciones WSFEv1](../reference/operations/wsfev1.md) | [Manual de desarrollador ARCA](https://www.arca.gob.ar/ws/documentacion/manuales/manual-desarrollador-ARCA-COMPG.pdf) |
| **WSFEXv1** | Facturación electrónica de exportación; autoriza/consulta comprobantes y consulta datos de exportación y parámetros. | [Operaciones WSFEXv1](../reference/operations/wsfexv1.md) | [Manual de desarrollador WSFEXv1](https://arca.gob.ar/ws/documentacion/manuales/WSFEX-Manualparaeldesarrollador_V3.1.1_ARCA.pdf) |
| **WSMTXCA** | Facturación doméstica con detalle de artículos; incluye autorización, CAEA, ajustes y consultas del contrato. No es el circuito integral de Factura de Crédito MiPyME. | [Operaciones WSMTXCA](../reference/operations/wsmtxca.md) | [Manual oficial MTXCA v0.25.8](https://arca.gob.ar/fe/ayuda/documentos/wsmtxca-RG-2904.pdf) |
| **Padrón A4** | Consulta registral de una persona. | [Operaciones A4](../reference/operations/padrona4.md) | [Manual oficial Padrón A4](https://arca.gob.ar/ws/ws_sr_padron_a4/manual_ws_sr_padron_a4_v1.3.pdf) |
| **Constancia, endpoint histórico A5** | Consulta de la constancia de inscripción y listas de personas, con variantes de respuesta v2. El endpoint conserva `personaServiceA5`, pero el `service` WSAA vigente es `ws_sr_constancia_inscripcion`. | [Operaciones de Constancia/A5](../reference/operations/padrona5.md) | [Manual oficial de Constancia](https://www.afip.gov.ar/ws/WSCI/manual-ws-sr-ws-constancia-inscripcion-V3.6.pdf) |
| **Padrón A10** | Consulta registral individual. | [Operaciones A10](../reference/operations/padrona10.md) | [Manual oficial Padrón A10](https://arca.gob.ar/ws/ws_sr_padron_a10/manual_ws_sr_padron_a10_v1.2.pdf) |
| **Padrón A13** | Busca identificadores por documento y consulta datos de persona, incluidas las operaciones v2. | [Operaciones A13](../reference/operations/padrona13.md) | [Manual oficial Padrón A13](https://ftp.afip.gob.ar/ws/ws-padron-a13/manual-ws-sr-padron-a13-v1.4.pdf) |
| **WSCDC** | Constata si un comprobante está registrado y autorizado, y consulta modalidades y tablas referenciales. | [Operaciones WSCDC](../reference/operations/wscdc.md), [guía WSCDC](WSCDC.md) | [Manual oficial WSCDC, revisión 2025-12-01](https://www.afip.gob.ar/ws/WSCDCV1/WSCDC-manual-desarrollador-v4.pdf) |
| **WSFECred** | Gestiona aceptación/rechazo, cancelaciones, cuentas corrientes, estados y operaciones del ciclo de FCE MiPyME. No autoriza la factura original. | [Operaciones WSFECred](../reference/operations/wsfecred.md), [guía WSFECred](WSFECred.md) | [Manual oficial del Web Service FECred](https://servicioscf.afip.gob.ar/facturadecreditoelectronica/documentos/Manual-Desarrollador-WSFECRED.pdf) |

Los nombres A4/A5/A10/A13 se refieren a los contratos/endpoints y no determinan
por sí solos cuál servicio debe usar una aplicación. En particular, A5 se
presenta actualmente como Constancia de Inscripción en el catálogo y manual;
consultá allí el identificador WSAA que corresponde. El
[catálogo oficial de servicios web](https://ftp.afip.gob.ar/ws/documentacion/catalogo.asp)
ayuda a verificar nombres y estado publicados por ARCA.

## Integración desde .NET

Registra los servicios con `AddNetArcaWs` y construye un
`ArcaTenantContext` con tenant, CUIT representada, ambiente y certificado
obtenidos desde fuentes confiables de tu aplicación. Pasa el contexto en cada
operación autenticada. Los clientes aceptan requests tipados generados a partir
del contrato; el host debe llenar y validar los datos comerciales y fiscales
antes de enviarlos.

Para emisión CAE unitaria de WSFEv1, WSFEXv1 y WSMTXCA, usa
`SafeInvoiceService` y el diario para conservar el payload y reconciliar un
resultado incierto. Una consulta de Padrón es lectura autenticada; el `dummy`
de un servicio es un probe de disponibilidad y no acredita permisos fiscales.
Ver [emisión durable](Diario-fiscal.md), [multitenancy](Contexto-multitenant.md)
y [health checks](Health-checks.md).

El proyecto [GuideExamples.cs](../../examples/NetArcaWs.Examples/GuideExamples.cs)
contiene ejemplos C# compilables para WSAA, tenant, health checks, cálculo y
emisión durable WSFEv1. La referencia por operación aporta las clases y campos
del contrato; no construyas requests productivos copiando fixtures o ejemplos
de prueba sin validar sus datos.

## Alcance

Esta lista contiene los clientes fiscales descritos en el alcance implementado
actual. [Servicios y cobertura](Servicios-y-cobertura.md) diferencia sus
operaciones contractuales de infraestructura (transporte, certificados,
health checks y diario). Otros servicios mencionados por PyAfipWs o en manuales
generales no forman parte de estos clientes .NET; consulta el
[inventario upstream](../reference/upstream-inventory.md) antes de asumir una
equivalencia. El [seguimiento priorizado de los servicios pendientes](https://github.com/ARSASWebDesign/NetArcaWs/issues/10)
conserva su clasificación por fuente y organismo.
