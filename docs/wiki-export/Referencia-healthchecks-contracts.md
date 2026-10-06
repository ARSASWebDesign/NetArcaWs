<!-- Source: docs/reference/healthchecks-contracts.md. Generated wiki mirror; edit the repository source. -->

# Contratos para health checks SOAP

Fecha de consulta: 2026-10-06. Este documento reúne los contratos publicados en los manuales oficiales y los WSDL servidos por homologación enlazados abajo. El coordinador de implementación invocó `dummy` de MTXCA en homologación: HTTP 200 y `appserver/authserver/dbserver = OK`; no se invocaron endpoints de producción. Los prefijos XML (`a4`, `a5`, `a10`, `a13`, `ser`) son alias ilustrativos; la URI de namespace es lo que identifica el contrato.

## Endpoints y contratos `dummy`

| Servicio | WSAA `service` vigente | Homologación (endpoint / WSDL) | Producción (endpoint / WSDL) | Operación / SOAPAction (SOAP 1.1) | Request | Response |
|---|---|---|---|---|---|---|
| Padrón A4 | `ws_sr_padron_a4` | `https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA4` / agregar `?WSDL` | `https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA4` / agregar `?WSDL` | `dummy`; SOAPAction `""` (WSDL homol.) | `<a4:dummy/>` | `dummyResponse` en `http://a4.soap.ws.server.puc.sr/`; contiene `return` y luego `appserver`, `authserver`, `dbserver`. |
| Constancia (antes A5) | `ws_sr_constancia_inscripcion` | `https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5` / agregar `?WSDL` | `https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA5` / agregar `?WSDL` | `dummy`; SOAPAction `""` (WSDL homol.) | `<a5:dummy/>` | `dummyResponse` en `http://a5.soap.ws.server.puc.sr/`; contiene `return` y luego `appserver`, `authserver`, `dbserver`. |
| Padrón A10 | `ws_sr_padron_a10` | `https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA10` / agregar `?WSDL` | `https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA10` / agregar `?WSDL` | `dummy`; SOAPAction `""` (WSDL homol.) | `<a10:dummy/>` | `dummyResponse` en `http://a10.soap.ws.server.puc.sr/`; contiene `return` y luego `appserver`, `authserver`, `dbserver`. |
| Padrón A13 | `ws_sr_padron_a13` | `https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA13` / agregar `?WSDL` | `https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA13` / agregar `?WSDL` | `dummy`; SOAPAction `""` (WSDL homol.) | `<a13:dummy/>` | `dummyResponse` en `http://a13.soap.ws.server.puc.sr/`; contiene `return` y luego `appserver`, `authserver`, `dbserver`. |
| MTXCA | `wsmtxca` | `https://fwshomo.afip.gov.ar/wsmtxca/services/MTXCAService` / agregar `?wsdl` | `https://serviciosjava.afip.gov.ar/wsmtxca/services/MTXCAService` / agregar `?wsdl` | `dummy`; SOAPAction `http://impl.service.wsmtxca.afip.gov.ar/service/dummy` (WSDL homol.) | Body vacío: `<soapenv:Body/>` | `dummyResponse` en `http://impl.service.wsmtxca.afip.gov.ar/service/`; hijos `appserver`, `authserver`, `dbserver` sin `return`. |

### Contratos adicionales de rutas ya inspeccionadas

Estos dos contratos se incluyen como referencia complementaria para checks existentes de facturación; no son lógica de emisión ni alteran el alcance de health-check. Los WSDL homol. de WSFE y WSFEX exponen `FEDummy` / `FEXDummy`, respectivamente, y SOAP 1.1/1.2.

| Servicio | WSAA `service` | Homologación (endpoint / WSDL) | Producción (endpoint / WSDL) | Namespace / operación / SOAPAction | Request / response |
|---|---|---|---|---|---|
| WSFEv1 | `wsfe` | `https://wswhomo.afip.gov.ar/wsfev1/service.asmx` / `?WSDL` | `https://servicios1.afip.gov.ar/wsfev1/service.asmx` / `?WSDL` | `http://ar.gov.afip.dif.FEV1/`; `FEDummy`; `http://ar.gov.afip.dif.FEV1/FEDummy` | Request `<tns:FEDummy/>` vacío. Response wrapper `FEDummyResponse` y `FEDummyResult`; campos `AppServer`, `DbServer`, `AuthServer`, todos calificados en el namespace del schema. |
| WSFEXv1 | `wsfex` | `https://wswhomo.afip.gov.ar/wsfexv1/service.asmx` / `?WSDL` | `https://servicios1.afip.gov.ar/wsfexv1/service.asmx` / `?WSDL` | `http://ar.gov.afip.dif.fexv1/`; `FEXDummy`; `http://ar.gov.afip.dif.fexv1/FEXDummy` | Request `<tns:FEXDummy/>` vacío. Response wrapper `FEXDummyResponse` y `FEXDummyResult`; campos `AppServer`, `DbServer`, `AuthServer`, todos calificados en el namespace del schema. |

En los cinco contratos el namespace calificado se aplica al elemento de operación del request y al wrapper de respuesta. En los ejemplos documentados, `return` y los campos de estado aparecen sin namespace (no hay namespace por defecto que los califique). El sobre SOAP usa `http://schemas.xmlsoap.org/soap/envelope/`. Los valores de estado documentados son `OK` o `ERROR`; un `OK` solo confirma disponibilidad de esa respuesta, no autenticación o capacidad funcional de negocio.

Los manuales de Padrón exceptúan expresamente `dummy` del requisito de enviar `token` y `sign`; los cuatro WSDLs de homologación descargados declaran para esta operación `soapAction=""` y esquema `elementFormDefault="unqualified"`. Por tanto, estos probes no necesitan solicitar ticket WSAA. El WSDL MTXCA consultado declara SOAP 1.1 document/literal, `soapAction="http://impl.service.wsmtxca.afip.gov.ar/service/dummy"` y mensaje `dummyRequest` sin partes. La URI `.gov.ar` del WSDL desplegado difiere de `.gob.ar` que aparece en el manual MTXCA v25; para invocación debe prevalecer el binding realmente desplegado.

### A5 fue reemplazado por Constancia

El endpoint real conserva el sufijo `personaServiceA5`, pero el catálogo y manual actuales lo presentan como **WS de Constancia de Inscripción**, antes llamado `ws_sr_padron_a5`; el `service` que debe enviarse a WSAA es `ws_sr_constancia_inscripcion`. No confundir el nombre histórico de endpoint `A5` con la identidad actual del servicio. El manual vigente también documenta `getPersona_v2` y la incorporación opcional de `fechaSolicitud` en `caracterizacion`.

## WSAA no ofrece un método `dummy`

WSAA publica `LoginCms` en `https://wsaahomo.afip.gov.ar/ws/services/LoginCms` (homologación) y `https://wsaa.afip.gov.ar/ws/services/LoginCms` (producción), con operación SOAP `loginCms` y entrada CMS (`in0`) que contiene el TRA firmado. No hay operación `dummy` o health anónimo en el WSDL. Un GET de `?WSDL` sirve únicamente para comprobar resolución/conectividad HTTP y disponibilidad del contrato; no valida el método `loginCms`, certificados, autorización ni salud de servicios dependientes. No enviar CMS ni realizar llamadas en una comprobación de disponibilidad sin credenciales/configuración explícitas.

## Fuentes primarias

- [Catálogo oficial de servicios web ARCA](https://ftp.afip.gob.ar/ws/documentacion/catalogo.asp) — nombres y estado actual, incluida la sustitución del A5 por Constancia.
- [Manual oficial Padrón A4 v1.3](https://arca.gob.ar/ws/ws_sr_padron_a4/manual_ws_sr_padron_a4_v1.3.pdf) — endpoints, autenticación y ejemplos `dummy`.
- [Manual oficial Constancia (A5 histórico) v3.6](https://www.afip.gob.ar/ws/WSCI/manual-ws-sr-ws-constancia-inscripcion-V3.6.pdf) — endpoints A5 y service id vigente.
- [Manual oficial Padrón A10 v1.2](https://www.afip.gob.ar/ws/ws_sr_padron_a10/manual_ws_sr_padron_a10_v1.2.pdf) — endpoints y contrato `dummy`.
- [Manual oficial Padrón A13 v1.4](https://ftp.afip.gob.ar/ws/ws-padron-a13/manual-ws-sr-padron-a13-v1.4.pdf) — endpoints y contrato `dummy`.
- [Manual oficial MTXCA v25](https://www.afip.gob.ar/fe/documentos/Web-Service-MTXCA-v25.pdf) — endpoints y request/response `dummy`. El WSDL de homologación consultado en `https://fwshomo.afip.gov.ar/wsmtxca/services/MTXCAService?wsdl` declara namespace y SOAPAction `.gov.ar`, distintos del `.gob.ar` que aparece en el manual.
- [WSDL oficial Padrón A4 homol.](https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA4?WSDL), [A10](https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA10?WSDL), [A13](https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA13?WSDL), y [Constancia/A5](https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5?WSDL) — namespaces, elementos, qualification y binding `soapAction` comprobados en los contratos servidos el 2026-10-06.
- [WSDL homol. WSFEv1](https://wswhomo.afip.gov.ar/wsfev1/service.asmx?WSDL) y [WSFEXv1](https://wswhomo.afip.gov.ar/wsfexv1/service.asmx?WSDL) — SOAPAction, dummy y qualification XML.
- [Documentación oficial WSAA](https://www.afip.gob.ar/ws/documentacion/wsaa.asp) y [Especificación técnica WSAA 1.2.0](https://www.afip.gob.ar/ws/WSAA/Especificacion_Tecnica_WSAA_1.2.0.pdf) — LoginCms y su binding.
- [PyAfipWs upstream: ws_sr_padron.py](https://github.com/reingart/pyafipws/blob/main/ws_sr_padron.py) y [wsmtx.py](https://github.com/reingart/pyafipws/blob/main/wsmtx.py) — referencia secundaria para clientes; puede conservar contratos anteriores y no prevalece sobre los manuales vigentes.

## Implicaciones para probes de aplicación

1. Usar host homologación o producción según configuración explícita; nunca derivar un host productivo para una ejecución local de prueba.
2. Para A4, Constancia/A5, A10 y A13, construir `dummy` en el namespace de cada servicio. Para MTXCA, enviar body vacío con la operación definida por el WSDL.
3. Validar el wrapper esperado y estados no vacíos; registrar `appserver`, `authserver`, `dbserver` sin asignarles interpretaciones cruzadas entre servicios.
4. Para A4, Constancia/A5, A10 y A13, el WSDL homol. consultado declara SOAPAction vacío; MTXCA declara la URI `.gov.ar` indicada arriba. Ante un cambio de WSDL, releer el binding desplegado.
5. WSAA puede tener un check separado de disponibilidad de WSDL/HTTP, no un check anónimo de su operación SOAP.
