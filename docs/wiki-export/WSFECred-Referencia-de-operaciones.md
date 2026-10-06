<!-- Source: docs/reference/operations/wsfecred.md. Generated wiki mirror; edit the repository source. -->

# WSFECred: Factura de Crédito Electrónica MiPyME

**Rol:** Gestiona el ciclo posterior a la emisión de la FCE y su cuenta corriente.

**Funcionalidad:** Consultar, aceptar y rechazar FCE, informar cancelaciones o transferencia, consultar historiales, remitos y parámetros.

**Uso:** Usarlo con un tenant autorizado para wsfecred. Las escrituras no tienen reintentos ni diario automático; persistir la intención y reconciliar las respuestas inciertas mediante consultas antes de cualquier reenvío.

Contrato generado desde [`wsfecred-production.wsdl`](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/docs/reference/contracts/wsfecred-production.wsdl); namespace XML `http://ar.gob.afip.wsfecred/FECredService/`. El servicio WSAA es `wsfecred`. Las operaciones autenticadas envían ticket WSAA; `dummy` es una comprobación no autenticada según el cliente generado.

Los tipos C# indicados proceden de los contratos generados y el WSDL fijado. `minOccurs`, `maxOccurs` y `nillable` describen estructura XML; **no sustituyen reglas de negocio** ni garantizan aceptación por ARCA. No se inventan validaciones de negocio.

## Operaciones disponibles

| Operación | Clase | Request C# | Response C# | SOAPAction |
|---|---|---|---|---|
| `dummy` | consulta técnica, sin WSAA | sin DTO | `WsfeCred.DummyResponseType` | `http://ar.gob.afip.wsfecred/FECredService/dummy` |
| `consultarComprobantes` | requiere ticket WSAA | `WsfeCred.ConsultarComprobanteRequestType` | `WsfeCred.ConsultarComprobantesResponseType` | `http://ar.gob.afip.wsfecred/FECredService/consultarComprobantes` |
| `rechazarNotaDC` | requiere ticket WSAA | `WsfeCred.RechazarNotaDcRequestType` | `WsfeCred.RechazarNotaDcResponseType` | `http://ar.gob.afip.wsfecred/FECredService/rechazarNotaDC` |
| `consultarCtasCtes` | requiere ticket WSAA | `WsfeCred.ConsultarCtasCtesRequestType` | `WsfeCred.ConsultarCtasCtesResponseType` | `http://ar.gob.afip.wsfecred/FECredService/consultarCtasCtes` |
| `consultarCtaCte` | requiere ticket WSAA | `WsfeCred.ConsultarCtaCteRequestType` | `WsfeCred.ConsultarCtaCteResponseType` | `http://ar.gob.afip.wsfecred/FECredService/consultarCtaCte` |
| `informarCancelacionTotalFECred` | requiere ticket WSAA | `WsfeCred.InformarCancelacionTotalFeCredRequestType` | `WsfeCred.InformarCancelacionTotalFeCredResponse` | `http://ar.gob.afip.wsfecred/FECredService/informarCancelacionTotalFECred` |
| `aceptarFECred` | requiere ticket WSAA | `WsfeCred.AceptarFeCredRequestType` | `WsfeCred.AceptarFeCredResponse` | `http://ar.gob.afip.wsfecred/FECredService/aceptarFECred` |
| `rechazarFECred` | requiere ticket WSAA | `WsfeCred.RechazarFeCredRequestType` | `WsfeCred.RechazarFeCredResponse` | `http://ar.gob.afip.wsfecred/FECredService/rechazarFECred` |
| `informarFacturaAgtDptoCltv` | requiere ticket WSAA | `WsfeCred.InformarFacturaAgtDptoCltvRequestType` | `WsfeCred.InformarFacturaAgtDptoCltvResponse` | `http://ar.gob.afip.wsfecred/FECredService/informarFacturaAgtDptoCltv` |
| `consultarFacturasAgtDptoCltv` | requiere ticket WSAA | `WsfeCred.ConsultarFacturasAgtDptoCltvRequestType` | `WsfeCred.ConsultarFacturasAgtDptoCltvResponseType` | `http://ar.gob.afip.wsfecred/FECredService/consultarFacturasAgtDptoCltv` |
| `consultarCuentasEnAgtDptoCltv` | requiere ticket WSAA | `WsfeCred.ConsultarCuentasEnAgtDptoCltvRequestType` | `WsfeCred.ConsultarCuentasEnAgtDptoCltvResponseType` | `http://ar.gob.afip.wsfecred/FECredService/consultarCuentasEnAgtDptoCltv` |
| `consultarObligadoRecepcion` | requiere ticket WSAA | `WsfeCred.ConsultarObligadoRecepcionRequestType` | `WsfeCred.ConsultarObligadoRecepcionResponseType` | `http://ar.gob.afip.wsfecred/FECredService/consultarObligadoRecepcion` |
| `consultarTiposRetenciones` | requiere ticket WSAA | `WsfeCred.ConsultarTiposRetencionesRequest` | `WsfeCred.ConsultarTiposRetencionesResponseType` | `http://ar.gob.afip.wsfecred/FECredService/consultarTiposRetenciones` |
| `consultarTiposMotivosRechazo` | requiere ticket WSAA | `WsfeCred.ConsultarTiposMotivosRechazoRequest` | `WsfeCred.ConsultarTiposMotivosRechazoResponse` | `http://ar.gob.afip.wsfecred/FECredService/consultarTiposMotivosRechazo` |
| `consultarTiposFormasCancelacion` | requiere ticket WSAA | `WsfeCred.ConsultarTiposFormasCancelacionRequest` | `WsfeCred.ConsultarTiposFormasCancelacionResponse` | `http://ar.gob.afip.wsfecred/FECredService/consultarTiposFormasCancelacion` |
| `obtenerRemitos` | requiere ticket WSAA | `WsfeCred.ObtenerRemitosRequestType` | `WsfeCred.ObtenerRemitosResponseType` | `http://ar.gob.afip.wsfecred/FECredService/obtenerRemitos` |
| `consultarHistorialEstadosComprobante` | requiere ticket WSAA | `WsfeCred.ConsultarHistorialEstadosComprobanteRequestType` | `WsfeCred.ConsultarHistorialEstadosComprobanteResponseType` | `http://ar.gob.afip.wsfecred/FECredService/consultarHistorialEstadosComprobante` |
| `consultarHistorialEstadosCtaCte` | requiere ticket WSAA | `WsfeCred.ConsultarHistorialEstadosCtaCteRequestType` | `WsfeCred.ConsultarHistorialEstadosCtaCteResponseType` | `http://ar.gob.afip.wsfecred/FECredService/consultarHistorialEstadosCtaCte` |
| `consultarTiposAjustesOperacion` | requiere ticket WSAA | `WsfeCred.ConsultarTiposAjustesOperacionRequest` | `WsfeCred.ConsultarTiposAjustesOperacionResponse` | `http://ar.gob.afip.wsfecred/FECredService/consultarTiposAjustesOperacion` |
| `consultarMontoObligadoRecepcion` | requiere ticket WSAA | `WsfeCred.ConsultarMontoObligadoRecepcionRequestType` | `WsfeCred.ConsultarMontoObligadoRecepcionResponseType` | `http://ar.gob.afip.wsfecred/FECredService/consultarMontoObligadoRecepcion` |
| `modificarOpcionTransferencia` | requiere ticket WSAA | `WsfeCred.ModificarOpcionTransferenciaRequestType` | `WsfeCred.ModificarOpcionTransferenciaResponse` | `http://ar.gob.afip.wsfecred/FECredService/modificarOpcionTransferencia` |

## Campos y tipos

Cada tabla lista los campos XML del elemento raíz de request/response. Las referencias a tipos nombrados se amplían en el catálogo de tipos complejos. Para respuestas, `Errors`/`Observaciones` (cuando el tipo los declara) transportan rechazos y avisos del servicio; las tablas de errores ARCA por código deben contrastarse con el manual vigente del servicio. Un SOAP Fault de transporte/protocolo se expone como `NetArcaWs.Transport.SoapFaultException` con `Code`, `Reason`, `Detail` y `StatusCode`. No reintentar automáticamente escrituras: ante timeout o resultado incierto, reconciliar el estado antes de cualquier nuevo envío.


### `dummy`

Clase: **consulta técnica**; autenticación: **no requiere ticket WSAA**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/dummy`.

**Request** (`cuerpo SOAP vacío`, raíz XML `(vacío)`)


**Response** (`WsfeCred.DummyResponseType`, raíz XML `dummyResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `dummyReturn` | [`tns:DummyReturnType`](#dummyreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarComprobantes`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarComprobantes`.

**Request** (`WsfeCred.ConsultarComprobanteRequestType`, raíz XML `consultarComprobantesRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `rolCUITRepresentada` | [`tns:RolSimpleType`](#rolsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CUITContraparte` | [`tns:CuitSimpleType`](#cuitsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codTipoCmp` | `xsd:short` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `estadoCmp` | [`tns:EstadoCmpSimpleType`](#estadocmpsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fecha` | [`tns:FiltroFechaType`](#filtrofechatype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codCtaCte` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `estadoCtaCte` | [`tns:EstadoCtaCteSimpleType`](#estadoctactesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroPagina` | `xsd:short` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarComprobantesResponseType`, raíz XML `consultarComprobantesResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarCmpReturn` | [`tns:ConsultarCmpReturnType`](#consultarcmpreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `rechazarNotaDC`

Clase: **escritura**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/rechazarNotaDC`.

**Request** (`WsfeCred.RechazarNotaDcRequestType`, raíz XML `rechazarNotaDCRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idComprobante` | [`tns:IdComprobanteType`](#idcomprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayMotivosRechazo` | [`tns:ArrayMotivosRechazoType`](#arraymotivosrechazotype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.RechazarNotaDcResponseType`, raíz XML `rechazarNotaDCResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `rechazarNotaDCReturn` | [`tns:RechazarNotaDCReturnType`](#rechazarnotadcreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCtasCtes`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarCtasCtes`.

**Request** (`WsfeCred.ConsultarCtasCtesRequestType`, raíz XML `consultarCtasCtesRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `rolCUITRepresentada` | [`tns:RolSimpleType`](#rolsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CUITContraparte` | [`tns:CuitSimpleType`](#cuitsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fecha` | [`tns:FiltroFechaType`](#filtrofechatype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `estadoCtaCte` | [`tns:EstadoCtaCteSimpleType`](#estadoctactesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroPagina` | `xsd:short` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `opcionTransferencia` | [`tns:OpcionTransferenciaSimpleType`](#opciontransferenciasimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarCtasCtesResponseType`, raíz XML `consultarCtasCtesResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarCtasCtesReturn` | [`tns:ConsultarCtasCtesReturnType`](#consultarctasctesreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCtaCte`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarCtaCte`.

**Request** (`WsfeCred.ConsultarCtaCteRequestType`, raíz XML `consultarCtaCteRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarCtaCteResponseType`, raíz XML `consultarCtaCteResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarCtaCteReturn` | [`tns:ConsultarCtaCteReturnType`](#consultarctactereturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `informarCancelacionTotalFECred`

Clase: **escritura**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/informarCancelacionTotalFECred`.

**Request** (`WsfeCred.InformarCancelacionTotalFeCredRequestType`, raíz XML `informarCancelacionTotalFECredRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayFormasCancelacion` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importeCancelacion` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.InformarCancelacionTotalFeCredResponse`, raíz XML `informarCancelacionTotalFECredResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `operacionFECredReturn` | [`tns:OperacionFECredReturnType`](#operacionfecredreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `aceptarFECred`

Clase: **escritura**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/aceptarFECred`.

**Request** (`WsfeCred.AceptarFeCredRequestType`, raíz XML `aceptarFECredRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayConfirmarNotasDC` | [`tns:ArrayConfirmarNotasType`](#arrayconfirmarnotastype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayFormasCancelacion` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayRetenciones` | [`tns:ArrayRetencionesType`](#arrayretencionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayAjustesOperacion` | [`tns:ArrayAjustesOperacionType`](#arrayajustesoperaciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoCancelacion` | [`tns:TipoCancelacionSimpleType`](#tipocancelacionsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeCancelado` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeTotalRetPesos` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeEmbargoPesos` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `saldoAceptado` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codMoneda` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cotizacionMonedaUlt` | `xsd:decimal` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `informaCBU` | [`tns:SiNoSimpleType`](#sinosimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CBUComprador` | [`tns:CBUSimpleType`](#cbusimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.AceptarFeCredResponse`, raíz XML `aceptarFECredResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `operacionFECredReturn` | [`tns:OperacionFECredReturnType`](#operacionfecredreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `rechazarFECred`

Clase: **escritura**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/rechazarFECred`.

**Request** (`WsfeCred.RechazarFeCredRequestType`, raíz XML `rechazarFECredRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayMotivosRechazo` | [`tns:ArrayMotivosRechazoType`](#arraymotivosrechazotype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.RechazarFeCredResponse`, raíz XML `rechazarFECredResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `operacionFECredReturn` | [`tns:OperacionFECredReturnType`](#operacionfecredreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `informarFacturaAgtDptoCltv`

Clase: **escritura**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/informarFacturaAgtDptoCltv`.

**Request** (`WsfeCred.InformarFacturaAgtDptoCltvRequestType`, raíz XML `informarFacturaAgtDptoCltvRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ctaAgente` | [`tns:CuentaEnAgenteType`](#cuentaenagentetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.InformarFacturaAgtDptoCltvResponse`, raíz XML `informarFacturaAgtDptoCltvResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `operacionFECredReturn` | [`tns:OperacionFECredReturnType`](#operacionfecredreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarFacturasAgtDptoCltv`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarFacturasAgtDptoCltv`.

**Request** (`WsfeCred.ConsultarFacturasAgtDptoCltvRequestType`, raíz XML `consultarFacturasAgtDptoCltvRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `filtroFecha` | [`tns:FiltroFechaType`](#filtrofechatype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarFacturasAgtDptoCltvResponseType`, raíz XML `consultarFacturasAgtDptoCltvResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarFacturasAgtDptoCltvReturn` | [`tns:ConsultarFacturasAgtDptoCltvReturnType`](#consultarfacturasagtdptocltvreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCuentasEnAgtDptoCltv`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarCuentasEnAgtDptoCltv`.

**Request** (`WsfeCred.ConsultarCuentasEnAgtDptoCltvRequestType`, raíz XML `consultarCuentasEnAgtDptoCltvRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarCuentasEnAgtDptoCltvResponseType`, raíz XML `consultarCuentasEnAgtDptoCltvResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarCuentasEnAgtDptoCltvReturn` | [`tns:ConsultarCuentasEnAgtDptoCltvReturnType`](#consultarcuentasenagtdptocltvreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarObligadoRecepcion`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarObligadoRecepcion`.

**Request** (`WsfeCred.ConsultarObligadoRecepcionRequestType`, raíz XML `consultarObligadoRecepcionRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitConsultada` | [`tns:CuitSimpleType`](#cuitsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarObligadoRecepcionResponseType`, raíz XML `consultarObligadoRecepcionResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarObligadoRecepcionReturn` | [`tns:consultarObligadoRecepcionReturnType`](#consultarobligadorecepcionreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarTiposRetenciones`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarTiposRetenciones`.

**Request** (`WsfeCred.ConsultarTiposRetencionesRequest`, raíz XML `consultarTiposRetencionesRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarTiposRetencionesResponseType`, raíz XML `consultarTiposRetencionesResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarTiposRetencionesReturn` | [`tns:ConsultarTiposRetencionesReturnType`](#consultartiposretencionesreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarTiposMotivosRechazo`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarTiposMotivosRechazo`.

**Request** (`WsfeCred.ConsultarTiposMotivosRechazoRequest`, raíz XML `consultarTiposMotivosRechazoRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarTiposMotivosRechazoResponse`, raíz XML `consultarTiposMotivosRechazoResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoDescripcionReturn` | [`tns:ConsultarCodigoDescripcionReturnType`](#consultarcodigodescripcionreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarTiposFormasCancelacion`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarTiposFormasCancelacion`.

**Request** (`WsfeCred.ConsultarTiposFormasCancelacionRequest`, raíz XML `consultarTiposFormasCancelacionRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarTiposFormasCancelacionResponse`, raíz XML `consultarTiposFormasCancelacionResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoDescripcionReturn` | [`tns:ConsultarCodigoDescripcionReturnType`](#consultarcodigodescripcionreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `obtenerRemitos`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/obtenerRemitos`.

**Request** (`WsfeCred.ObtenerRemitosRequestType`, raíz XML `obtenerRemitosRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idComprobante` | [`tns:IdComprobanteType`](#idcomprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ObtenerRemitosResponseType`, raíz XML `obtenerRemitosResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `obtenerRemitosReturn` | [`tns:ObtenerRemitosReturnType`](#obtenerremitosreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarHistorialEstadosComprobante`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarHistorialEstadosComprobante`.

**Request** (`WsfeCred.ConsultarHistorialEstadosComprobanteRequestType`, raíz XML `consultarHistorialEstadosComprobanteRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idComprobante` | [`tns:IdComprobanteType`](#idcomprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarHistorialEstadosComprobanteResponseType`, raíz XML `consultarHistorialEstadosComprobanteResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarHistorialEstadosComprobanteReturn` | [`tns:ConsultarHistorialEstadosComprobanteReturnType`](#consultarhistorialestadoscomprobantereturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarHistorialEstadosCtaCte`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarHistorialEstadosCtaCte`.

**Request** (`WsfeCred.ConsultarHistorialEstadosCtaCteRequestType`, raíz XML `consultarHistorialEstadosCtaCteRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarHistorialEstadosCtaCteResponseType`, raíz XML `consultarHistorialEstadosCtaCteResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarHistorialEstadosCtaCteReturn` | [`tns:consultarHistorialEstadosCtaCteReturnType`](#consultarhistorialestadosctactereturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarTiposAjustesOperacion`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarTiposAjustesOperacion`.

**Request** (`WsfeCred.ConsultarTiposAjustesOperacionRequest`, raíz XML `consultarTiposAjustesOperacionRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarTiposAjustesOperacionResponse`, raíz XML `consultarTiposAjustesOperacionResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoDescripcionReturn` | [`tns:ConsultarCodigoDescripcionReturnType`](#consultarcodigodescripcionreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarMontoObligadoRecepcion`

Clase: **consulta**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/consultarMontoObligadoRecepcion`.

**Request** (`WsfeCred.ConsultarMontoObligadoRecepcionRequestType`, raíz XML `consultarMontoObligadoRecepcionRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitConsultada` | [`tns:CuitSimpleType`](#cuitsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaEmision` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ConsultarMontoObligadoRecepcionResponseType`, raíz XML `consultarMontoObligadoRecepcionResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarMontoObligadoRecepcionReturn` | [`tns:ConsultarMontoObligadoRecepcionReturnType`](#consultarmontoobligadorecepcionreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `modificarOpcionTransferencia`

Clase: **escritura**; autenticación: **WSAA `wsfecred`**. SOAPAction: `http://ar.gob.afip.wsfecred/FECredService/modificarOpcionTransferencia`.

**Request** (`WsfeCred.ModificarOpcionTransferenciaRequestType`, raíz XML `modificarOpcionTransferenciaRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `opcionTransferencia` | [`tns:OpcionTransferenciaSimpleType`](#opciontransferenciasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeCred.ModificarOpcionTransferenciaResponse`, raíz XML `modificarOpcionTransferenciaResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `operacionFECredReturn` | [`tns:OperacionFECredReturnType`](#operacionfecredreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas pueden incluir `arrayErrores` y `arrayErroresFormato`, aun con HTTP 200. Revisar el resultado de negocio específico antes de avanzar el estado local; no reenviar escrituras automáticamente ante una respuesta incierta. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


## Catálogo de tipos complejos

Incluye todos los tipos complejos nombrados del XSD del servicio. Los elementos `base` señalan herencia; se conserva la cardinalidad declarada. `xs:documentation` se transcribe cuando existe.


### `AceptarFECredRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayConfirmarNotasDC` | [`tns:ArrayConfirmarNotasType`](#arrayconfirmarnotastype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayFormasCancelacion` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayRetenciones` | [`tns:ArrayRetencionesType`](#arrayretencionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayAjustesOperacion` | [`tns:ArrayAjustesOperacionType`](#arrayajustesoperaciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoCancelacion` | [`tns:TipoCancelacionSimpleType`](#tipocancelacionsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeCancelado` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeTotalRetPesos` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeEmbargoPesos` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `saldoAceptado` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codMoneda` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cotizacionMonedaUlt` | `xsd:decimal` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `informaCBU` | [`tns:SiNoSimpleType`](#sinosimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CBUComprador` | [`tns:CBUSimpleType`](#cbusimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `AjusteOperacionType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importe` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayAjustesOperacionType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ajuste` | [`tns:AjusteOperacionType`](#ajusteoperaciontype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayCodigosDescripcionesStringType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoDescripcionString` | [`tns:CodigoDescripcionStringType`](#codigodescripcionstringtype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayCodigosDescripcionesType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoDescripcion` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayComprobantesType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `comprobante` | [`tns:ComprobanteType`](#comprobantetype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayConfirmarNotasType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `confirmarNota` | [`tns:ConfirmarNotaDCType`](#confirmarnotadctype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayCuentasEnAgenteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuentaEnAgente` | [`tns:CuentaEnAgenteType`](#cuentaenagentetype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayFacturasAgtDptoCltvType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `facturaInformada` | [`tns:FacturaInformadaAgtDptoCltvType`](#facturainformadaagtdptocltvtype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayHistorialEstadosComprobanteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `estadoHistorico` | [`tns:EstadoCmpType`](#estadocmptype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayHistorialEstadosCtaCteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `estadoHistorico` | [`tns:EstadoCtaCteType`](#estadoctactetype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayIdsComprobantesType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `idsComprobantes` | [`tns:IdComprobanteType`](#idcomprobantetype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayInfosCtaCteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `infoCtaCte` | [`tns:InfoCtaCteType`](#infoctactetype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayItemsType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `item` | [`tns:ItemType`](#itemtype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayMotivosRechazoType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `motivoRechazo` | [`tns:MotivoRechazoType`](#motivorechazotype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayOtrosTributosType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `otroTributo` | [`tns:OtroTributoType`](#otrotributotype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayRetencionesType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `retencion` | [`tns:RetencionType`](#retenciontype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArraySubtotalesIVAType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `subtotalIVA` | [`tns:SubtotalIVAType`](#subtotalivatype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayTexto250SimpleType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `texto` | [`tns:Texto250SimpleType`](#texto250simpletype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayTiposRetencionesType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `tipoRetencion` | [`tns:TipoRetencionType`](#tiporetenciontype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `AuthRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | [`tns:CuitSimpleType`](#cuitsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CodigoDescripcionStringType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CodigoDescripcionType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ComprobanteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitEmisor` | [`tns:CuitSimpleType`](#cuitsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `razonSocialEmi` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codTipoCmp` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ptovta` | [`tns:PuntoVentaSimpleType`](#puntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `nroCmp` | [`tns:NumeroComprobanteSimpleType`](#numerocomprobantesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitReceptor` | [`tns:CuitSimpleType`](#cuitsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `razonSocialRecep` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `tipoCodAuto` | [`tns:TipoCodAutorizacionType`](#tipocodautorizaciontype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codAutorizacion` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaEmision` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaPuestaDispo` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaVenPago` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaVenAcep` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeTotal` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codMoneda` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cotizacionMoneda` | `xsd:decimal` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CBUEmisor` | [`tns:CBUSimpleType`](#cbusimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `AliasEmisor` | [`tns:Texto250SimpleType`](#texto250simpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `esAnulacion` | [`tns:SiNoSimpleType`](#sinosimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `esPostAceptacion` | [`tns:SiNoSimpleType`](#sinosimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idComprobanteAsociado` | [`tns:IdComprobanteType`](#idcomprobantetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `referenciasComerciales` | [`tns:ArrayTexto250SimpleType`](#arraytexto250simpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arraySubtotalesIVA` | [`tns:ArraySubtotalesIVAType`](#arraysubtotalesivatype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayOtrosTributos` | [`tns:ArrayOtrosTributosType`](#arrayotrostributostype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayItems` | [`tns:ArrayItemsType`](#arrayitemstype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosGenerales` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosComerciales` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `leyendaComercial` | [`tns:Texto250SimpleType`](#texto250simpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codCtaCte` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `estado` | [`tns:EstadoCmpType`](#estadocmptype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `tipoAcep` | [`tns:TipoAceptacionSimpleType`](#tipoaceptacionsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaHoraAcep` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayMotivosRechazo` | [`tns:ArrayMotivosRechazoType`](#arraymotivosrechazotype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `opcionTransferencia` | [`tns:OpcionTransferenciaSimpleType`](#opciontransferenciasimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `infoTransferencia` | [`tns:InfoTransferenciaType`](#infotransferenciatype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConfirmarNotaDCType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `acepta` | [`tns:SiNoSimpleType`](#sinosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idNota` | [`tns:IdComprobanteType`](#idcomprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCmpReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayComprobantes` | [`tns:ArrayComprobantesType`](#arraycomprobantestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroPagina` | `xsd:short` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `hayMas` | [`tns:SiNoSimpleType`](#sinosimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCodigoDescripcionRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCodigoDescripcionResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoDescripcionReturn` | [`tns:ConsultarCodigoDescripcionReturnType`](#consultarcodigodescripcionreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCodigoDescripcionReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayCodigoDescripcion` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCodigoDescripcionStringResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoDescripcionReturn` | [`tns:ConsultarCodigoDescripcionStringReturnType`](#consultarcodigodescripcionstringreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCodigoDescripcionStringReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayCodigoDescripcion` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarComprobanteRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `rolCUITRepresentada` | [`tns:RolSimpleType`](#rolsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CUITContraparte` | [`tns:CuitSimpleType`](#cuitsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codTipoCmp` | `xsd:short` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `estadoCmp` | [`tns:EstadoCmpSimpleType`](#estadocmpsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fecha` | [`tns:FiltroFechaType`](#filtrofechatype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codCtaCte` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `estadoCtaCte` | [`tns:EstadoCtaCteSimpleType`](#estadoctactesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroPagina` | `xsd:short` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarComprobantesResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarCmpReturn` | [`tns:ConsultarCmpReturnType`](#consultarcmpreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCtaCteRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCtaCteResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarCtaCteReturn` | [`tns:ConsultarCtaCteReturnType`](#consultarctactereturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCtaCteReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ctaCte` | [`tns:CuentaCorrienteType`](#cuentacorrientetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCtasCtesRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `rolCUITRepresentada` | [`tns:RolSimpleType`](#rolsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CUITContraparte` | [`tns:CuitSimpleType`](#cuitsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fecha` | [`tns:FiltroFechaType`](#filtrofechatype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `estadoCtaCte` | [`tns:EstadoCtaCteSimpleType`](#estadoctactesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroPagina` | `xsd:short` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `opcionTransferencia` | [`tns:OpcionTransferenciaSimpleType`](#opciontransferenciasimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCtasCtesResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarCtasCtesReturn` | [`tns:ConsultarCtasCtesReturnType`](#consultarctasctesreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCtasCtesReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayInfosCtaCte` | [`tns:ArrayInfosCtaCteType`](#arrayinfosctactetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroPagina` | `xsd:short` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `hayMas` | [`tns:SiNoSimpleType`](#sinosimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCuentasEnAgtDptoCltvRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCuentasEnAgtDptoCltvReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayCuentasEnAgente` | [`tns:ArrayCuentasEnAgenteType`](#arraycuentasenagentetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservacion` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarFacturasAgtDptoCltvRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `filtroFecha` | [`tns:FiltroFechaType`](#filtrofechatype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarFacturasAgtDptoCltvResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarFacturasAgtDptoCltvReturn` | [`tns:ConsultarFacturasAgtDptoCltvReturnType`](#consultarfacturasagtdptocltvreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarFacturasAgtDptoCltvReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayFacturasAgtDptoCltv` | [`tns:ArrayFacturasAgtDptoCltvType`](#arrayfacturasagtdptocltvtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarHistorialEstadosComprobanteRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idComprobante` | [`tns:IdComprobanteType`](#idcomprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarHistorialEstadosComprobanteResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarHistorialEstadosComprobanteReturn` | [`tns:ConsultarHistorialEstadosComprobanteReturnType`](#consultarhistorialestadoscomprobantereturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarHistorialEstadosComprobanteReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `idComprobante` | [`tns:IdComprobanteType`](#idcomprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayHistorialEstados` | [`tns:ArrayHistorialEstadosComprobanteType`](#arrayhistorialestadoscomprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarHistorialEstadosCtaCteRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarMontoObligadoRecepcionRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitConsultada` | [`tns:CuitSimpleType`](#cuitsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaEmision` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarMontoObligadoRecepcionResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarMontoObligadoRecepcionReturn` | [`tns:ConsultarMontoObligadoRecepcionReturnType`](#consultarmontoobligadorecepcionreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarMontoObligadoRecepcionReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `obligado` | [`tns:SiNoSimpleType`](#sinosimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `montoDesde` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservacion` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarTiposRetencionesResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarTiposRetencionesReturn` | [`tns:ConsultarTiposRetencionesReturnType`](#consultartiposretencionesreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarTiposRetencionesReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayTiposRetenciones` | [`tns:ArrayTiposRetencionesType`](#arraytiposretencionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CuentaCorrienteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codCtaCte` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `estadoCtaCte` | [`tns:EstadoCtaCteType`](#estadoctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `factura` | [`tns:ComprobanteType`](#comprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayNotasDCAsociadas` | [`tns:ArrayComprobantesType`](#arraycomprobantestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayFormasCancelacion` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayRetenciones` | [`tns:ArrayRetencionesType`](#arrayretencionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayAjustesOperacion` | [`tns:ArrayAjustesOperacionType`](#arrayajustesoperaciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeInicial` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importeTotalNotasDC` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeCancelado` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeTotalRetPesos` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeEmbargoPesos` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `saldoAceptado` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `saldo` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codMoneda` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cotizacionMonedaUlt` | `xsd:decimal` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CuentaEnAgenteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitAgente` | [`tns:CuitSimpleType`](#cuitsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `razonSocialAgente` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idCuenta` | [`tns:IdCuentaAgenteSimpleType`](#idcuentaagentesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `denominacion` | [`tns:Texto250SimpleType`](#texto250simpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DummyResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `dummyReturn` | [`tns:DummyReturnType`](#dummyreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DummyReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `appserver` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `authserver` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `dbserver` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EstadoCmpType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `estado` | [`tns:EstadoCmpSimpleType`](#estadocmpsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHoraEstado` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EstadoCtaCteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `estado` | [`tns:EstadoCtaCteSimpleType`](#estadoctactesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHoraEstado` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `FacturaInformadaAgtDptoCltvType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `idFactura` | [`tns:IdComprobanteType`](#idcomprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `infoAgtDptoCltv` | [`tns:InfoAgtDptoCltvType`](#infoagtdptocltvtype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `FiltroFechaType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `tipo` | [`tns:TipoFechaSimpleType`](#tipofechasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `desde` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `hasta` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `IdComprobanteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CUITEmisor` | [`tns:CuitSimpleType`](#cuitsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codTipoCmp` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ptoVta` | [`tns:PuntoVentaSimpleType`](#puntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `nroCmp` | [`tns:NumeroComprobanteSimpleType`](#numerocomprobantesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `IdCtaCteType`

El tipo no declara elementos hijo directos en el XSD; puede ser vacío o derivar su contenido de un tipo base, que se documenta por separado.

### `InfoAgtDptoCltvType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `fechaInfo` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ctaAgente` | [`tns:CuentaEnAgenteType`](#cuentaenagentetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `recibida` | [`tns:SiNoSimpleType`](#sinosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaLectura` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaRecep` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `aceptada` | [`tns:SiNoSimpleType`](#sinosimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `motivoRechazo` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idPagoAgtDptoCltv` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CBUAgtDptoCltv` | [`tns:CBUSimpleType`](#cbusimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `InfoCtaCteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codCtaCte` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `estadoCtaCte` | [`tns:EstadoCtaCteType`](#estadoctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idFacturaCredito` | [`tns:IdComprobanteType`](#idcomprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importeTotalFC` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `saldo` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `saldoAceptado` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codMoneda` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `opcionTransferencia` | [`tns:OpcionTransferenciaSimpleType`](#opciontransferenciasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `InfoSCAType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `fechaAceptacionFactura` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `informaCBUReceptor` | [`tns:SiNoSimpleType`](#sinosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CBUReceptor` | [`tns:CBUSimpleType`](#cbusimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CBUValidada` | [`tns:SiNoSimpleType`](#sinosimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaLecturaSCA` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `InfoTransferenciaType`

El tipo no declara elementos hijo directos en el XSD; puede ser vacío o derivar su contenido de un tipo base, que se documenta por separado.

### `InformarCancelacionTotalFECredRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayFormasCancelacion` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importeCancelacion` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `InformarFacturaAgtDptoCltvRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ctaAgente` | [`tns:CuentaEnAgenteType`](#cuentaenagentetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ItemType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `orden` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `unidadesMtx` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigoMtx` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigo` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codNomMercosur` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cantidad` | [`tns:DecimalSimpleType`](#decimalsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigoUnidadMedida` | `xsd:short` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `precioUnitario` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeBonificacion` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigoCondicionIVA` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importeIVA` | [`tns:ImporteSimpleType`](#importesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeItem` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ModificarOpcionTransferenciaRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `opcionTransferencia` | [`tns:OpcionTransferenciaSimpleType`](#opciontransferenciasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `MotivoRechazoType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codMotivo` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descMotivo` | [`tns:Texto250SimpleType`](#texto250simpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `justificacion` | [`tns:Texto250SimpleType`](#texto250simpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ObtenerRemitosRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idComprobante` | [`tns:IdComprobanteType`](#idcomprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ObtenerRemitosResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `obtenerRemitosReturn` | [`tns:ObtenerRemitosReturnType`](#obtenerremitosreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ObtenerRemitosReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayIdsRemitos` | [`tns:ArrayIdsComprobantesType`](#arrayidscomprobantestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `OperacionFECredResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `operacionFECredReturn` | [`tns:OperacionFECredReturnType`](#operacionfecredreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `OperacionFECredReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `OtroTributoType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `detalle` | [`tns:Texto250SimpleType`](#texto250simpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `baseImponible` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importe` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RechazarFECredRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayMotivosRechazo` | [`tns:ArrayMotivosRechazoType`](#arraymotivosrechazotype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RechazarNotaDCRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idComprobante` | [`tns:IdComprobanteType`](#idcomprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayMotivosRechazo` | [`tns:ArrayMotivosRechazoType`](#arraymotivosrechazotype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RechazarNotaDCResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `rechazarNotaDCReturn` | [`tns:RechazarNotaDCReturnType`](#rechazarnotadcreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RechazarNotaDCReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `idComprobante` | [`tns:IdComprobanteType`](#idcomprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `RetencionType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codTipo` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importe` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `porcentaje` | [`tns:PorcentajeSimpleType`](#porcentajesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descMotivo` | [`tns:Texto250SimpleType`](#texto250simpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `SubtotalIVAType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `baseImponible` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importe` | [`tns:ImporteSimpleType`](#importesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `TipoRetencionType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoJurisdiccion` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcionJurisdiccion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `porcentajeRetencion` | [`tns:PorcentajeSimpleType`](#porcentajesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `consultarCuentasEnAgtDptoCltvResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarCuentasEnAgtDptoCltvReturn` | [`tns:ConsultarCuentasEnAgtDptoCltvReturnType`](#consultarcuentasenagtdptocltvreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `consultarHistorialEstadosCtaCteResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarHistorialEstadosCtaCteReturn` | [`tns:consultarHistorialEstadosCtaCteReturnType`](#consultarhistorialestadosctactereturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `consultarHistorialEstadosCtaCteReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `idCtaCte` | [`tns:IdCtaCteType`](#idctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayHistorialEstados` | [`tns:ArrayHistorialEstadosCtaCteType`](#arrayhistorialestadosctactetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `consultarObligadoRecepcionRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitConsultada` | [`tns:CuitSimpleType`](#cuitsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `consultarObligadoRecepcionResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `consultarObligadoRecepcionReturn` | [`tns:consultarObligadoRecepcionReturnType`](#consultarobligadorecepcionreturntype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `consultarObligadoRecepcionReturnType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:SiNoSimpleType`](#sinosimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `desde` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservacion` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErroresFormato` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
