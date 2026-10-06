<!-- Source: docs/reference/operations/wsfev1.md. Generated wiki mirror; edit the repository source. -->

# WSFEv1: facturación nacional

**Rol:** Emisión electrónica de comprobantes del mercado interno.

**Funcionalidad:** Autorizar comprobantes, consultar comprobantes y CAEA, y leer parámetros de facturación.

**Uso:** Integrarlo en el flujo de facturación nacional; proteger las escrituras con persistencia e idempotencia en la aplicación consumidora.

Contrato generado desde [`wsfev1-production.wsdl`](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/docs/reference/contracts/wsfev1-production.wsdl); namespace XML `http://ar.gov.afip.dif.FEV1/`. El servicio WSAA es `wsfe`. Las operaciones autenticadas envían ticket WSAA; `dummy` es una comprobación no autenticada según el cliente generado.

Los tipos C# indicados proceden de los contratos generados y el WSDL fijado. `minOccurs`, `maxOccurs` y `nillable` describen estructura XML; **no sustituyen reglas de negocio** ni garantizan aceptación por ARCA. No se inventan validaciones de negocio.

## Operaciones disponibles

| Operación | Clase | Request C# | Response C# | SOAPAction |
|---|---|---|---|---|
| `FECAESolicitar` | requiere ticket WSAA | `WsfeV1.FecaeSolicitar` | `WsfeV1.FecaeSolicitarResponse` | `http://ar.gov.afip.dif.FEV1/FECAESolicitar` |
| `FECompTotXRequest` | requiere ticket WSAA | `WsfeV1.FeCompTotXRequest` | `WsfeV1.FeCompTotXRequestResponse` | `http://ar.gov.afip.dif.FEV1/FECompTotXRequest` |
| `FEDummy` | consulta técnica, sin WSAA | sin DTO | `WsfeV1.FeDummyResponse` | `http://ar.gov.afip.dif.FEV1/FEDummy` |
| `FECompUltimoAutorizado` | requiere ticket WSAA | `WsfeV1.FeCompUltimoAutorizado` | `WsfeV1.FeCompUltimoAutorizadoResponse` | `http://ar.gov.afip.dif.FEV1/FECompUltimoAutorizado` |
| `FECompConsultar` | requiere ticket WSAA | `WsfeV1.FeCompConsultar` | `WsfeV1.FeCompConsultarResponse` | `http://ar.gov.afip.dif.FEV1/FECompConsultar` |
| `FECAEARegInformativo` | requiere ticket WSAA | `WsfeV1.FecaeaRegInformativo` | `WsfeV1.FecaeaRegInformativoResponse` | `http://ar.gov.afip.dif.FEV1/FECAEARegInformativo` |
| `FECAEASolicitar` | requiere ticket WSAA | `WsfeV1.FecaeaSolicitar` | `WsfeV1.FecaeaSolicitarResponse` | `http://ar.gov.afip.dif.FEV1/FECAEASolicitar` |
| `FECAEASinMovimientoConsultar` | requiere ticket WSAA | `WsfeV1.FecaeaSinMovimientoConsultar` | `WsfeV1.FecaeaSinMovimientoConsultarResponse` | `http://ar.gov.afip.dif.FEV1/FECAEASinMovimientoConsultar` |
| `FECAEASinMovimientoInformar` | requiere ticket WSAA | `WsfeV1.FecaeaSinMovimientoInformar` | `WsfeV1.FecaeaSinMovimientoInformarResponse` | `http://ar.gov.afip.dif.FEV1/FECAEASinMovimientoInformar` |
| `FECAEAConsultar` | requiere ticket WSAA | `WsfeV1.FecaeaConsultar` | `WsfeV1.FecaeaConsultarResponse` | `http://ar.gov.afip.dif.FEV1/FECAEAConsultar` |
| `FEParamGetCotizacion` | requiere ticket WSAA | `WsfeV1.FeParamGetCotizacion` | `WsfeV1.FeParamGetCotizacionResponse` | `http://ar.gov.afip.dif.FEV1/FEParamGetCotizacion` |
| `FEParamGetTiposTributos` | requiere ticket WSAA | `WsfeV1.FeParamGetTiposTributos` | `WsfeV1.FeParamGetTiposTributosResponse` | `http://ar.gov.afip.dif.FEV1/FEParamGetTiposTributos` |
| `FEParamGetTiposMonedas` | requiere ticket WSAA | `WsfeV1.FeParamGetTiposMonedas` | `WsfeV1.FeParamGetTiposMonedasResponse` | `http://ar.gov.afip.dif.FEV1/FEParamGetTiposMonedas` |
| `FEParamGetTiposIva` | requiere ticket WSAA | `WsfeV1.FeParamGetTiposIva` | `WsfeV1.FeParamGetTiposIvaResponse` | `http://ar.gov.afip.dif.FEV1/FEParamGetTiposIva` |
| `FEParamGetTiposOpcional` | requiere ticket WSAA | `WsfeV1.FeParamGetTiposOpcional` | `WsfeV1.FeParamGetTiposOpcionalResponse` | `http://ar.gov.afip.dif.FEV1/FEParamGetTiposOpcional` |
| `FEParamGetTiposConcepto` | requiere ticket WSAA | `WsfeV1.FeParamGetTiposConcepto` | `WsfeV1.FeParamGetTiposConceptoResponse` | `http://ar.gov.afip.dif.FEV1/FEParamGetTiposConcepto` |
| `FEParamGetPtosVenta` | requiere ticket WSAA | `WsfeV1.FeParamGetPtosVenta` | `WsfeV1.FeParamGetPtosVentaResponse` | `http://ar.gov.afip.dif.FEV1/FEParamGetPtosVenta` |
| `FEParamGetTiposCbte` | requiere ticket WSAA | `WsfeV1.FeParamGetTiposCbte` | `WsfeV1.FeParamGetTiposCbteResponse` | `http://ar.gov.afip.dif.FEV1/FEParamGetTiposCbte` |
| `FEParamGetCondicionIvaReceptor` | requiere ticket WSAA | `WsfeV1.FeParamGetCondicionIvaReceptor` | `WsfeV1.FeParamGetCondicionIvaReceptorResponse` | `http://ar.gov.afip.dif.FEV1/FEParamGetCondicionIvaReceptor` |
| `FEParamGetTiposDoc` | requiere ticket WSAA | `WsfeV1.FeParamGetTiposDoc` | `WsfeV1.FeParamGetTiposDocResponse` | `http://ar.gov.afip.dif.FEV1/FEParamGetTiposDoc` |
| `FEParamGetTiposPaises` | requiere ticket WSAA | `WsfeV1.FeParamGetTiposPaises` | `WsfeV1.FeParamGetTiposPaisesResponse` | `http://ar.gov.afip.dif.FEV1/FEParamGetTiposPaises` |
| `FEParamGetActividades` | requiere ticket WSAA | `WsfeV1.FeParamGetActividades` | `WsfeV1.FeParamGetActividadesResponse` | `http://ar.gov.afip.dif.FEV1/FEParamGetActividades` |

## Campos y tipos

Cada tabla lista los campos XML del elemento raíz de request/response. Las referencias a tipos nombrados se amplían en el catálogo de tipos complejos. Para respuestas, `Errors`/`Observaciones` (cuando el tipo los declara) transportan rechazos y avisos del servicio; las tablas de errores ARCA por código deben contrastarse con el manual vigente del servicio. Un SOAP Fault de transporte/protocolo se expone como `NetArcaWs.Transport.SoapFaultException` con `Code`, `Reason`, `Detail` y `StatusCode`. No reintentar automáticamente escrituras: ante timeout o resultado incierto, reconciliar el estado antes de cualquier nuevo envío.


### `FECAESolicitar`

Clase: **escritura**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FECAESolicitar`.

**Request** (`WsfeV1.FecaeSolicitar`, raíz XML `FECAESolicitar`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FeCAEReq` | [`tns:FECAERequest`](#fecaerequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FecaeSolicitarResponse`, raíz XML `FECAESolicitarResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECAESolicitarResult` | [`tns:FECAEResponse`](#fecaeresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FECompTotXRequest`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FECompTotXRequest`.

**Request** (`WsfeV1.FeCompTotXRequest`, raíz XML `FECompTotXRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeCompTotXRequestResponse`, raíz XML `FECompTotXRequestResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECompTotXRequestResult` | [`tns:FERegXReqResponse`](#feregxreqresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEDummy`

Clase: **consulta técnica**; autenticación: **no requiere ticket WSAA**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEDummy`.

**Request** (`sin elemento/payload`, raíz XML `FEDummy`)


**Response** (`WsfeV1.FeDummyResponse`, raíz XML `FEDummyResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEDummyResult` | [`tns:DummyResponse`](#dummyresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FECompUltimoAutorizado`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FECompUltimoAutorizado`.

**Request** (`WsfeV1.FeCompUltimoAutorizado`, raíz XML `FECompUltimoAutorizado`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `PtoVta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteTipo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeCompUltimoAutorizadoResponse`, raíz XML `FECompUltimoAutorizadoResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECompUltimoAutorizadoResult` | [`tns:FERecuperaLastCbteResponse`](#ferecuperalastcbteresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FECompConsultar`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FECompConsultar`.

**Request** (`WsfeV1.FeCompConsultar`, raíz XML `FECompConsultar`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FeCompConsReq` | [`tns:FECompConsultaReq`](#fecompconsultareq) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeCompConsultarResponse`, raíz XML `FECompConsultarResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECompConsultarResult` | [`tns:FECompConsultaResponse`](#fecompconsultaresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FECAEARegInformativo`

Clase: **escritura**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FECAEARegInformativo`.

**Request** (`WsfeV1.FecaeaRegInformativo`, raíz XML `FECAEARegInformativo`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FeCAEARegInfReq` | [`tns:FECAEARequest`](#fecaearequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FecaeaRegInformativoResponse`, raíz XML `FECAEARegInformativoResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECAEARegInformativoResult` | [`tns:FECAEAResponse`](#fecaearesponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FECAEASolicitar`

Clase: **escritura**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FECAEASolicitar`.

**Request** (`WsfeV1.FecaeaSolicitar`, raíz XML `FECAEASolicitar`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Periodo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Orden` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FecaeaSolicitarResponse`, raíz XML `FECAEASolicitarResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECAEASolicitarResult` | [`tns:FECAEAGetResponse`](#fecaeagetresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FECAEASinMovimientoConsultar`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FECAEASinMovimientoConsultar`.

**Request** (`WsfeV1.FecaeaSinMovimientoConsultar`, raíz XML `FECAEASinMovimientoConsultar`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CAEA` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `PtoVta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FecaeaSinMovimientoConsultarResponse`, raíz XML `FECAEASinMovimientoConsultarResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECAEASinMovimientoConsultarResult` | [`tns:FECAEASinMovConsResponse`](#fecaeasinmovconsresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FECAEASinMovimientoInformar`

Clase: **escritura**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FECAEASinMovimientoInformar`.

**Request** (`WsfeV1.FecaeaSinMovimientoInformar`, raíz XML `FECAEASinMovimientoInformar`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `PtoVta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FecaeaSinMovimientoInformarResponse`, raíz XML `FECAEASinMovimientoInformarResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECAEASinMovimientoInformarResult` | [`tns:FECAEASinMovResponse`](#fecaeasinmovresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FECAEAConsultar`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FECAEAConsultar`.

**Request** (`WsfeV1.FecaeaConsultar`, raíz XML `FECAEAConsultar`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Periodo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Orden` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FecaeaConsultarResponse`, raíz XML `FECAEAConsultarResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECAEAConsultarResult` | [`tns:FECAEAGetResponse`](#fecaeagetresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEParamGetCotizacion`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEParamGetCotizacion`.

**Request** (`WsfeV1.FeParamGetCotizacion`, raíz XML `FEParamGetCotizacion`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `MonId` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchCotiz` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeParamGetCotizacionResponse`, raíz XML `FEParamGetCotizacionResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEParamGetCotizacionResult` | [`tns:FECotizacionResponse`](#fecotizacionresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEParamGetTiposTributos`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEParamGetTiposTributos`.

**Request** (`WsfeV1.FeParamGetTiposTributos`, raíz XML `FEParamGetTiposTributos`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeParamGetTiposTributosResponse`, raíz XML `FEParamGetTiposTributosResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEParamGetTiposTributosResult` | [`tns:FETributoResponse`](#fetributoresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEParamGetTiposMonedas`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEParamGetTiposMonedas`.

**Request** (`WsfeV1.FeParamGetTiposMonedas`, raíz XML `FEParamGetTiposMonedas`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeParamGetTiposMonedasResponse`, raíz XML `FEParamGetTiposMonedasResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEParamGetTiposMonedasResult` | [`tns:MonedaResponse`](#monedaresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEParamGetTiposIva`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEParamGetTiposIva`.

**Request** (`WsfeV1.FeParamGetTiposIva`, raíz XML `FEParamGetTiposIva`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeParamGetTiposIvaResponse`, raíz XML `FEParamGetTiposIvaResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEParamGetTiposIvaResult` | [`tns:IvaTipoResponse`](#ivatiporesponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEParamGetTiposOpcional`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEParamGetTiposOpcional`.

**Request** (`WsfeV1.FeParamGetTiposOpcional`, raíz XML `FEParamGetTiposOpcional`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeParamGetTiposOpcionalResponse`, raíz XML `FEParamGetTiposOpcionalResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEParamGetTiposOpcionalResult` | [`tns:OpcionalTipoResponse`](#opcionaltiporesponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEParamGetTiposConcepto`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEParamGetTiposConcepto`.

**Request** (`WsfeV1.FeParamGetTiposConcepto`, raíz XML `FEParamGetTiposConcepto`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeParamGetTiposConceptoResponse`, raíz XML `FEParamGetTiposConceptoResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEParamGetTiposConceptoResult` | [`tns:ConceptoTipoResponse`](#conceptotiporesponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEParamGetPtosVenta`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEParamGetPtosVenta`.

**Request** (`WsfeV1.FeParamGetPtosVenta`, raíz XML `FEParamGetPtosVenta`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeParamGetPtosVentaResponse`, raíz XML `FEParamGetPtosVentaResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEParamGetPtosVentaResult` | [`tns:FEPtoVentaResponse`](#feptoventaresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEParamGetTiposCbte`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEParamGetTiposCbte`.

**Request** (`WsfeV1.FeParamGetTiposCbte`, raíz XML `FEParamGetTiposCbte`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeParamGetTiposCbteResponse`, raíz XML `FEParamGetTiposCbteResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEParamGetTiposCbteResult` | [`tns:CbteTipoResponse`](#cbtetiporesponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEParamGetCondicionIvaReceptor`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEParamGetCondicionIvaReceptor`.

**Request** (`WsfeV1.FeParamGetCondicionIvaReceptor`, raíz XML `FEParamGetCondicionIvaReceptor`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `ClaseCmp` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeParamGetCondicionIvaReceptorResponse`, raíz XML `FEParamGetCondicionIvaReceptorResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEParamGetCondicionIvaReceptorResult` | [`tns:CondicionIvaReceptorResponse`](#condicionivareceptorresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEParamGetTiposDoc`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEParamGetTiposDoc`.

**Request** (`WsfeV1.FeParamGetTiposDoc`, raíz XML `FEParamGetTiposDoc`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeParamGetTiposDocResponse`, raíz XML `FEParamGetTiposDocResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEParamGetTiposDocResult` | [`tns:DocTipoResponse`](#doctiporesponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEParamGetTiposPaises`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEParamGetTiposPaises`.

**Request** (`WsfeV1.FeParamGetTiposPaises`, raíz XML `FEParamGetTiposPaises`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeParamGetTiposPaisesResponse`, raíz XML `FEParamGetTiposPaisesResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEParamGetTiposPaisesResult` | [`tns:FEPaisResponse`](#fepaisresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEParamGetActividades`

Clase: **consulta**; autenticación: **WSAA `wsfe`**. SOAPAction: `http://ar.gov.afip.dif.FEV1/FEParamGetActividades`.

**Request** (`WsfeV1.FeParamGetActividades`, raíz XML `FEParamGetActividades`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:FEAuthRequest`](#feauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfeV1.FeParamGetActividadesResponse`, raíz XML `FEParamGetActividadesResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEParamGetActividadesResult` | [`tns:FEActividadesResponse`](#feactividadesresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Errores funcionales WSFE se devuelven en `Errors` (tipo `Err`, campos `Code` y `Msg`); los avisos van en `Observaciones` (tipo `Obs`). SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


## Catálogo de tipos complejos

Incluye todos los tipos complejos nombrados del XSD del servicio. Los elementos `base` señalan herencia; se conserva la cardinalidad declarada. `xs:documentation` se transcribe cuando existe.


### `Actividad`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ActividadesTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Orden` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `AlicIva`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `BaseImp` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Importe` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayOfActividad`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Actividad` | [`tns:Actividad`](#actividad) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfActividadesTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ActividadesTipo` | [`tns:ActividadesTipo`](#actividadestipo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfAlicIva`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `AlicIva` | [`tns:AlicIva`](#aliciva) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfCbteAsoc`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CbteAsoc` | [`tns:CbteAsoc`](#cbteasoc) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfCbteTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CbteTipo` | [`tns:CbteTipo`](#cbtetipo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfComprador`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Comprador` | [`tns:Comprador`](#comprador) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfConceptoTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ConceptoTipo` | [`tns:ConceptoTipo`](#conceptotipo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfCondicionIvaReceptor`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CondicionIvaReceptor` | [`tns:CondicionIvaReceptor`](#condicionivareceptor) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

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

### `ArrayOfFECAEADetRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECAEADetRequest` | [`tns:FECAEADetRequest`](#fecaeadetrequest) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfFECAEADetResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECAEADetResponse` | [`tns:FECAEADetResponse`](#fecaeadetresponse) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfFECAEASinMov`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECAEASinMov` | [`tns:FECAEASinMov`](#fecaeasinmov) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfFECAEDetRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECAEDetRequest` | [`tns:FECAEDetRequest`](#fecaedetrequest) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfFECAEDetResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FECAEDetResponse` | [`tns:FECAEDetResponse`](#fecaedetresponse) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfIvaTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `IvaTipo` | [`tns:IvaTipo`](#ivatipo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfMoneda`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Moneda` | [`tns:Moneda`](#moneda) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

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

### `ArrayOfPaisTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `PaisTipo` | [`tns:PaisTipo`](#paistipo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfPtoVenta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `PtoVenta` | [`tns:PtoVenta`](#ptoventa) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfTributo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Tributo` | [`tns:Tributo`](#tributo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfTributoTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `TributoTipo` | [`tns:TributoTipo`](#tributotipo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `CbteAsoc`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Tipo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `PtoVta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Nro` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cuit` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CbteFch` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CbteTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchDesde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchHasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CbteTipoResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfCbteTipo`](#arrayofcbtetipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Comprador`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `DocTipo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `DocNro` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Porcentaje` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConceptoTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchDesde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchHasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConceptoTipoResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfConceptoTipo`](#arrayofconceptotipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CondicionIvaReceptor`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cmp_Clase` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CondicionIvaReceptorResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfCondicionIvaReceptor`](#arrayofcondicionivareceptor) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Cotizacion`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `MonId` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `MonCotiz` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `FchCotiz` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DocTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchDesde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchHasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

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

### `FEActividadesResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfActividadesTipo`](#arrayofactividadestipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEAuthRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Token` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Sign` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cuit` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `FECAEACabRequest`

Extiende `FECabRequest`.

El tipo no declara elementos hijo directos en el XSD; puede ser vacío o derivar su contenido de un tipo base, que se documenta por separado.

### `FECAEACabResponse`

Extiende `FECabResponse`.

El tipo no declara elementos hijo directos en el XSD; puede ser vacío o derivar su contenido de un tipo base, que se documenta por separado.

### `FECAEADetRequest`

Extiende `FEDetRequest`.

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CAEA` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CbteFchHsGen` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECAEADetResponse`

Extiende `FEDetResponse`.

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CAEA` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECAEAGet`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CAEA` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Periodo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Orden` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `FchVigDesde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchVigHasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchTopeInf` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchProceso` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Observaciones` | [`tns:ArrayOfObs`](#arrayofobs) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECAEAGetResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:FECAEAGet`](#fecaeaget) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECAEARequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FeCabReq` | [`tns:FECAEACabRequest`](#fecaeacabrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FeDetReq` | [`tns:ArrayOfFECAEADetRequest`](#arrayoffecaeadetrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECAEAResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FeCabResp` | [`tns:FECAEACabResponse`](#fecaeacabresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FeDetResp` | [`tns:ArrayOfFECAEADetResponse`](#arrayoffecaeadetresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECAEASinMov`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CAEA` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchProceso` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `PtoVta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `FECAEASinMovConsResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfFECAEASinMov`](#arrayoffecaeasinmov) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECAEASinMovResponse`

Extiende `FECAEASinMov`.

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Resultado` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECAECabRequest`

Extiende `FECabRequest`.

El tipo no declara elementos hijo directos en el XSD; puede ser vacío o derivar su contenido de un tipo base, que se documenta por separado.

### `FECAECabResponse`

Extiende `FECabResponse`.

El tipo no declara elementos hijo directos en el XSD; puede ser vacío o derivar su contenido de un tipo base, que se documenta por separado.

### `FECAEDetRequest`

Extiende `FEDetRequest`.

El tipo no declara elementos hijo directos en el XSD; puede ser vacío o derivar su contenido de un tipo base, que se documenta por separado.

### `FECAEDetResponse`

Extiende `FEDetResponse`.

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CAE` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CAEFchVto` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECAERequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FeCabReq` | [`tns:FECAECabRequest`](#fecaecabrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FeDetReq` | [`tns:ArrayOfFECAEDetRequest`](#arrayoffecaedetrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECAEResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FeCabResp` | [`tns:FECAECabResponse`](#fecaecabresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FeDetResp` | [`tns:ArrayOfFECAEDetResponse`](#arrayoffecaedetresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECabRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CantReg` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `PtoVta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteTipo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `FECabResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Cuit` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `PtoVta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteTipo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `FchProceso` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CantReg` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Resultado` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Reproceso` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECompConsResponse`

Extiende `FECAEDetRequest`.

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Resultado` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CodAutorizacion` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `EmisionTipo` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchVto` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchProceso` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Observaciones` | [`tns:ArrayOfObs`](#arrayofobs) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `PtoVta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteTipo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `FECompConsultaReq`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CbteTipo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteNro` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `PtoVta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `FECompConsultaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:FECompConsResponse`](#fecompconsresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FECotizacionResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:Cotizacion`](#cotizacion) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEDetRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Concepto` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `DocTipo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `DocNro` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteDesde` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteHasta` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteFch` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `ImpTotal` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ImpTotConc` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ImpNeto` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ImpOpEx` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ImpTrib` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ImpIVA` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `FchServDesde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchServHasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchVtoPago` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `MonId` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `MonCotiz` | `s:double` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CanMisMonExt` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CondicionIVAReceptorId` | `s:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CbtesAsoc` | [`tns:ArrayOfCbteAsoc`](#arrayofcbteasoc) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Tributos` | [`tns:ArrayOfTributo`](#arrayoftributo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Iva` | [`tns:ArrayOfAlicIva`](#arrayofaliciva) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Opcionales` | [`tns:ArrayOfOpcional`](#arrayofopcional) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Compradores` | [`tns:ArrayOfComprador`](#arrayofcomprador) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `PeriodoAsoc` | [`tns:Periodo`](#periodo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Actividades` | [`tns:ArrayOfActividad`](#arrayofactividad) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEDetResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Concepto` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `DocTipo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `DocNro` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteDesde` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteHasta` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteFch` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Resultado` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Observaciones` | [`tns:ArrayOfObs`](#arrayofobs) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEPaisResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfPaisTipo`](#arrayofpaistipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEPtoVentaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfPtoVenta`](#arrayofptoventa) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FERecuperaLastCbteResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `PtoVta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteTipo` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CbteNro` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FERegXReqResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `RegXReq` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FETributoResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfTributoTipo`](#arrayoftributotipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IvaTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchDesde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchHasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IvaTipoResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfIvaTipo`](#arrayofivatipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Moneda`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchDesde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchHasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `MonedaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfMoneda`](#arrayofmoneda) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
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
| `FchDesde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchHasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `OpcionalTipoResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ResultGet` | [`tns:ArrayOfOpcionalTipo`](#arrayofopcionaltipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Errors` | [`tns:ArrayOfErr`](#arrayoferr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Events` | [`tns:ArrayOfEvt`](#arrayofevt) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `PaisTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Periodo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FchDesde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchHasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `PtoVenta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Nro` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `EmisionTipo` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Bloqueado` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchBaja` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Tributo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `BaseImp` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Alic` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Importe` | `s:double` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `TributoTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchDesde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchHasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
