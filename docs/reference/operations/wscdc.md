# WSCDC: constatación de comprobantes

**Rol:** Verifica comprobantes emitidos y sus datos de autorización.

**Funcionalidad:** Constatar un comprobante y consultar modalidades, tipos de comprobante, documentos y opcionales.

**Uso:** Consultar con los datos reales del comprobante; interpretar resultado, observaciones y errores. No emite comprobantes ni reemplaza el control tributario de la aplicación.

Contrato generado desde [`wscdc-production.wsdl`](../contracts/wscdc-production.wsdl); namespace XML `http://servicios1.afip.gob.ar/wscdc/`. El servicio WSAA es `wscdc`. Las operaciones autenticadas envían ticket WSAA; `dummy` es una comprobación no autenticada según el cliente generado.

Los tipos C# indicados proceden de los contratos generados y el WSDL fijado. `minOccurs`, `maxOccurs` y `nillable` describen estructura XML; **no sustituyen reglas de negocio** ni garantizan aceptación por ARCA. No se inventan validaciones de negocio.

## Operaciones disponibles

| Operación | Clase | Request C# | Response C# | SOAPAction |
|---|---|---|---|---|
| `ComprobantesModalidadConsultar` | requiere ticket WSAA | `Wscdc.ComprobantesModalidadConsultar` | `Wscdc.ComprobantesModalidadConsultarResponse` | `http://servicios1.afip.gob.ar/wscdc/ComprobantesModalidadConsultar` |
| `ComprobantesTipoConsultar` | requiere ticket WSAA | `Wscdc.ComprobantesTipoConsultar` | `Wscdc.ComprobantesTipoConsultarResponse` | `http://servicios1.afip.gob.ar/wscdc/ComprobantesTipoConsultar` |
| `DocumentosTipoConsultar` | requiere ticket WSAA | `Wscdc.DocumentosTipoConsultar` | `Wscdc.DocumentosTipoConsultarResponse` | `http://servicios1.afip.gob.ar/wscdc/DocumentosTipoConsultar` |
| `OpcionalesTipoConsultar` | requiere ticket WSAA | `Wscdc.OpcionalesTipoConsultar` | `Wscdc.OpcionalesTipoConsultarResponse` | `http://servicios1.afip.gob.ar/wscdc/OpcionalesTipoConsultar` |
| `ComprobanteConstatar` | requiere ticket WSAA | `Wscdc.ComprobanteConstatar` | `Wscdc.ComprobanteConstatarResponse` | `http://servicios1.afip.gob.ar/wscdc/ComprobanteConstatar` |
| `ComprobanteDummy` | consulta técnica, sin WSAA | sin DTO | `Wscdc.ComprobanteDummyResponse` | `http://servicios1.afip.gob.ar/wscdc/ComprobanteDummy` |

## Campos y tipos

Cada tabla lista los campos XML del elemento raíz de request/response. Las referencias a tipos nombrados se amplían en el catálogo de tipos complejos. Para respuestas, `Errors`/`Observaciones` (cuando el tipo los declara) transportan rechazos y avisos del servicio; las tablas de errores ARCA por código deben contrastarse con el manual vigente del servicio. Un SOAP Fault de transporte/protocolo se expone como `NetArcaWs.Transport.SoapFaultException` con `Code`, `Reason`, `Detail` y `StatusCode`. No reintentar automáticamente escrituras: ante timeout o resultado incierto, reconciliar el estado antes de cualquier nuevo envío.


### `ComprobantesModalidadConsultar`

Clase: **consulta**; autenticación: **WSAA `wscdc`**. SOAPAction: `http://servicios1.afip.gob.ar/wscdc/ComprobantesModalidadConsultar`.

**Request** (`Wscdc.ComprobantesModalidadConsultar`, raíz XML `ComprobantesModalidadConsultar`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:CmpAuthRequest`](#cmpauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`Wscdc.ComprobantesModalidadConsultarResponse`, raíz XML `ComprobantesModalidadConsultarResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ComprobantesModalidadConsultarResult` | [`tns:FacModTipoResponse`](#facmodtiporesponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

La constatación devuelve `Resultado`, `Observaciones`, `Errors` y `Events`: un HTTP 200 no acredita la validez del comprobante. Revisar los códigos devueltos según el manual de WSCDC. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `ComprobantesTipoConsultar`

Clase: **consulta**; autenticación: **WSAA `wscdc`**. SOAPAction: `http://servicios1.afip.gob.ar/wscdc/ComprobantesTipoConsultar`.

**Request** (`Wscdc.ComprobantesTipoConsultar`, raíz XML `ComprobantesTipoConsultar`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:CmpAuthRequest`](#cmpauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`Wscdc.ComprobantesTipoConsultarResponse`, raíz XML `ComprobantesTipoConsultarResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ComprobantesTipoConsultarResult` | [`tns:CbteTipoResponse`](#cbtetiporesponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

La constatación devuelve `Resultado`, `Observaciones`, `Errors` y `Events`: un HTTP 200 no acredita la validez del comprobante. Revisar los códigos devueltos según el manual de WSCDC. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `DocumentosTipoConsultar`

Clase: **consulta**; autenticación: **WSAA `wscdc`**. SOAPAction: `http://servicios1.afip.gob.ar/wscdc/DocumentosTipoConsultar`.

**Request** (`Wscdc.DocumentosTipoConsultar`, raíz XML `DocumentosTipoConsultar`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:CmpAuthRequest`](#cmpauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`Wscdc.DocumentosTipoConsultarResponse`, raíz XML `DocumentosTipoConsultarResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `DocumentosTipoConsultarResult` | [`tns:DocTipoResponse`](#doctiporesponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

La constatación devuelve `Resultado`, `Observaciones`, `Errors` y `Events`: un HTTP 200 no acredita la validez del comprobante. Revisar los códigos devueltos según el manual de WSCDC. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `OpcionalesTipoConsultar`

Clase: **consulta**; autenticación: **WSAA `wscdc`**. SOAPAction: `http://servicios1.afip.gob.ar/wscdc/OpcionalesTipoConsultar`.

**Request** (`Wscdc.OpcionalesTipoConsultar`, raíz XML `OpcionalesTipoConsultar`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:CmpAuthRequest`](#cmpauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`Wscdc.OpcionalesTipoConsultarResponse`, raíz XML `OpcionalesTipoConsultarResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `OpcionalesTipoConsultarResult` | [`tns:OpcionalTipoResponse`](#opcionaltiporesponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

La constatación devuelve `Resultado`, `Observaciones`, `Errors` y `Events`: un HTTP 200 no acredita la validez del comprobante. Revisar los códigos devueltos según el manual de WSCDC. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `ComprobanteConstatar`

Clase: **consulta**; autenticación: **WSAA `wscdc`**. SOAPAction: `http://servicios1.afip.gob.ar/wscdc/ComprobanteConstatar`.

**Request** (`Wscdc.ComprobanteConstatar`, raíz XML `ComprobanteConstatar`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:CmpAuthRequest`](#cmpauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CmpReq` | [`tns:CmpDatos`](#cmpdatos) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`Wscdc.ComprobanteConstatarResponse`, raíz XML `ComprobanteConstatarResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ComprobanteConstatarResult` | [`tns:CmpResponse`](#cmpresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

La constatación devuelve `Resultado`, `Observaciones`, `Errors` y `Events`: un HTTP 200 no acredita la validez del comprobante. Revisar los códigos devueltos según el manual de WSCDC. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `ComprobanteDummy`

Clase: **consulta técnica**; autenticación: **no requiere ticket WSAA**. SOAPAction: `http://servicios1.afip.gob.ar/wscdc/ComprobanteDummy`.

**Request** (`generado por cliente`, raíz XML `ComprobanteDummy`)


**Response** (`Wscdc.ComprobanteDummyResponse`, raíz XML `ComprobanteDummyResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ComprobanteDummyResult` | [`tns:DummyResponse`](#dummyresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

La constatación devuelve `Resultado`, `Observaciones`, `Errors` y `Events`: un HTTP 200 no acredita la validez del comprobante. Revisar los códigos devueltos según el manual de WSCDC. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


## Catálogo de tipos complejos

Incluye todos los tipos complejos nombrados del XSD del servicio. Los elementos `base` señalan herencia; se conserva la cardinalidad declarada. `xs:documentation` se transcribe cuando existe.


### `ArrayOfCbteTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CbteTipo` | [`tns:CbteTipo`](#cbtetipo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfDocTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `DocTipo` | [`tns:DocTipo`](#doctipo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfErr`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Err` | [`tns:Err`](#err) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfEvt`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Evt` | [`tns:Evt`](#evt) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfFacModTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FacModTipo` | [`tns:FacModTipo`](#facmodtipo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfObs`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Obs` | [`tns:Obs`](#obs) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfOpcional`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Opcional` | [`tns:Opcional`](#opcional) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfOpcionalTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `OpcionalTipo` | [`tns:OpcionalTipo`](#opcionaltipo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `CbteTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CbteTipoResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfCbteTipo`](#arrayofcbtetipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CmpAuthRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Token` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Sign` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cuit` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CmpDatos`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CbteModo` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CuitEmisor` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `PtoVta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteTipo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteNro` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteFch` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `ImpTotal` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CodAutorizacion` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `DocTipoReceptor` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `DocNroReceptor` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Opcionales` | [`tns:ArrayOfOpcional`](#arrayofopcional) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CmpResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CmpResp` | [`tns:CmpDatos`](#cmpdatos) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Resultado` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Observaciones` | [`tns:ArrayOfObs`](#arrayofobs) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchProceso` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DocTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DocTipoResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfDocTipo`](#arrayofdoctipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DummyResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `AppServer` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `DbServer` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `AuthServer` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Err`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Code` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Msg` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Evt`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Code` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Msg` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FacModTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Cod` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchDesde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchHasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FacModTipoResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfFacModTipo`](#arrayoffacmodtipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Obs`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Code` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Msg` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Opcional`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Valor` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `OpcionalTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `OpcionalTipoResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfOpcionalTipo`](#arrayofopcionaltipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
