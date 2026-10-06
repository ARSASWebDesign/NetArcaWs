<!-- Source: docs/reference/operations/wsfexv1.md. Generated wiki mirror; edit the repository source. -->

# WSFEXv1: facturación de exportación

**Rol:** Emisión electrónica de comprobantes de exportación.

**Funcionalidad:** Autorizar y consultar comprobantes de exportación, obtener último número y consultar parámetros.

**Uso:** Usarlo en el circuito de exportación con datos aduaneros y comerciales validados por el sistema consumidor.

Contrato generado desde [`wsfexv1-production.wsdl`](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/docs/reference/contracts/wsfexv1-production.wsdl); namespace XML `http://ar.gov.afip.dif.fexv1/`. El servicio WSAA es `wsfex`. Las operaciones autenticadas envían ticket WSAA; `dummy` es una comprobación no autenticada según el cliente generado.

Los tipos C# indicados proceden de los contratos generados y el WSDL fijado. `minOccurs`, `maxOccurs` y `nillable` describen estructura XML; **no sustituyen reglas de negocio** ni garantizan aceptación por ARCA. No se inventan validaciones de negocio.

## Operaciones disponibles

| Operación | Clase | Request C# | Response C# | SOAPAction |
|---|---|---|---|---|
| `FEXAuthorize` | requiere ticket WSAA | `WsfexV1.FexAuthorize` | `WsfexV1.FexAuthorizeResponse` | `http://ar.gov.afip.dif.fexv1/FEXAuthorize` |
| `FEXGetCMP` | requiere ticket WSAA | `WsfexV1.FexGetCmp` | `WsfexV1.FexGetCmpResponse2` | `http://ar.gov.afip.dif.fexv1/FEXGetCMP` |
| `FEXGetPARAM_Cbte_Tipo` | requiere ticket WSAA | `WsfexV1.FexGetParamCbteTipo` | `WsfexV1.FexGetParamCbteTipoResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Cbte_Tipo` |
| `FEXGetPARAM_Tipo_Expo` | requiere ticket WSAA | `WsfexV1.FexGetParamTipoExpo` | `WsfexV1.FexGetParamTipoExpoResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Tipo_Expo` |
| `FEXGetPARAM_Incoterms` | requiere ticket WSAA | `WsfexV1.FexGetParamIncoterms` | `WsfexV1.FexGetParamIncotermsResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Incoterms` |
| `FEXGetPARAM_Idiomas` | requiere ticket WSAA | `WsfexV1.FexGetParamIdiomas` | `WsfexV1.FexGetParamIdiomasResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Idiomas` |
| `FEXGetPARAM_UMed` | requiere ticket WSAA | `WsfexV1.FexGetParamUMed` | `WsfexV1.FexGetParamUMedResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_UMed` |
| `FEXGetPARAM_DST_pais` | requiere ticket WSAA | `WsfexV1.FexGetParamDstPais` | `WsfexV1.FexGetParamDstPaisResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_DST_pais` |
| `FEXGetPARAM_DST_CUIT` | requiere ticket WSAA | `WsfexV1.FexGetParamDstCuit` | `WsfexV1.FexGetParamDstCuitResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_DST_CUIT` |
| `FEXGetPARAM_MON` | requiere ticket WSAA | `WsfexV1.FexGetParamMon` | `WsfexV1.FexGetParamMonResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_MON` |
| `FEXGetPARAM_MON_CON_COTIZACION` | requiere ticket WSAA | `WsfexV1.FexGetParamMonConCotizacion` | `WsfexV1.FexGetParamMonConCotizacionResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_MON_CON_COTIZACION` |
| `FEXGetLast_CMP` | requiere ticket WSAA | `WsfexV1.FexGetLastCmp` | `WsfexV1.FexGetLastCmpResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetLast_CMP` |
| `FEXDummy` | consulta técnica, sin WSAA | sin DTO | `WsfexV1.FexDummyResponse` | `http://ar.gov.afip.dif.fexv1/FEXDummy` |
| `FEXGetPARAM_Ctz` | requiere ticket WSAA | `WsfexV1.FexGetParamCtz` | `WsfexV1.FexGetParamCtzResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Ctz` |
| `FEXGetLast_ID` | requiere ticket WSAA | `WsfexV1.FexGetLastId` | `WsfexV1.FexGetLastIdResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetLast_ID` |
| `FEXGetPARAM_PtoVenta` | requiere ticket WSAA | `WsfexV1.FexGetParamPtoVenta` | `WsfexV1.FexGetParamPtoVentaResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_PtoVenta` |
| `FEXCheck_Permiso` | requiere ticket WSAA | `WsfexV1.FexCheckPermiso` | `WsfexV1.FexCheckPermisoResponse` | `http://ar.gov.afip.dif.fexv1/FEXCheck_Permiso` |
| `FEXGetPARAM_Opcionales` | requiere ticket WSAA | `WsfexV1.FexGetParamOpcionales` | `WsfexV1.FexGetParamOpcionalesResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Opcionales` |
| `FEXGetPARAM_Actividades` | requiere ticket WSAA | `WsfexV1.FexGetParamActividades` | `WsfexV1.FexGetParamActividadesResponse` | `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Actividades` |

## Campos y tipos

Cada tabla lista los campos XML del elemento raíz de request/response. Las referencias a tipos nombrados se amplían en el catálogo de tipos complejos. Para respuestas, `Errors`/`Observaciones` (cuando el tipo los declara) transportan rechazos y avisos del servicio; las tablas de errores ARCA por código deben contrastarse con el manual vigente del servicio. Un SOAP Fault de transporte/protocolo se expone como `NetArcaWs.Transport.SoapFaultException` con `Code`, `Reason`, `Detail` y `StatusCode`. No reintentar automáticamente escrituras: ante timeout o resultado incierto, reconciliar el estado antes de cualquier nuevo envío.


### `FEXAuthorize`

Clase: **escritura**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXAuthorize`.

**Request** (`WsfexV1.FexAuthorize`, raíz XML `FEXAuthorize`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cmp` | [`tns:ClsFEXRequest`](#clsfexrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexAuthorizeResponse`, raíz XML `FEXAuthorizeResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXAuthorizeResult` | [`tns:FEXResponseAuthorize`](#fexresponseauthorize) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetCMP`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetCMP`.

**Request** (`WsfexV1.FexGetCmp`, raíz XML `FEXGetCMP`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cmp` | [`tns:ClsFEXGetCMP`](#clsfexgetcmp) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetCmpResponse2`, raíz XML `FEXGetCMPResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetCMPResult` | [`tns:FEXGetCMPResponse`](#fexgetcmpresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_Cbte_Tipo`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Cbte_Tipo`.

**Request** (`WsfexV1.FexGetParamCbteTipo`, raíz XML `FEXGetPARAM_Cbte_Tipo`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamCbteTipoResponse`, raíz XML `FEXGetPARAM_Cbte_TipoResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_Cbte_TipoResult` | [`tns:FEXResponse_Cbte_Tipo`](#fexresponse_cbte_tipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_Tipo_Expo`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Tipo_Expo`.

**Request** (`WsfexV1.FexGetParamTipoExpo`, raíz XML `FEXGetPARAM_Tipo_Expo`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamTipoExpoResponse`, raíz XML `FEXGetPARAM_Tipo_ExpoResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_Tipo_ExpoResult` | [`tns:FEXResponse_Tex`](#fexresponse_tex) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_Incoterms`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Incoterms`.

**Request** (`WsfexV1.FexGetParamIncoterms`, raíz XML `FEXGetPARAM_Incoterms`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamIncotermsResponse`, raíz XML `FEXGetPARAM_IncotermsResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_IncotermsResult` | [`tns:FEXResponse_Inc`](#fexresponse_inc) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_Idiomas`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Idiomas`.

**Request** (`WsfexV1.FexGetParamIdiomas`, raíz XML `FEXGetPARAM_Idiomas`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamIdiomasResponse`, raíz XML `FEXGetPARAM_IdiomasResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_IdiomasResult` | [`tns:FEXResponse_Idi`](#fexresponse_idi) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_UMed`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_UMed`.

**Request** (`WsfexV1.FexGetParamUMed`, raíz XML `FEXGetPARAM_UMed`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamUMedResponse`, raíz XML `FEXGetPARAM_UMedResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_UMedResult` | [`tns:FEXResponse_Umed`](#fexresponse_umed) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_DST_pais`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_DST_pais`.

**Request** (`WsfexV1.FexGetParamDstPais`, raíz XML `FEXGetPARAM_DST_pais`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamDstPaisResponse`, raíz XML `FEXGetPARAM_DST_paisResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_DST_paisResult` | [`tns:FEXResponse_DST_pais`](#fexresponse_dst_pais) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_DST_CUIT`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_DST_CUIT`.

**Request** (`WsfexV1.FexGetParamDstCuit`, raíz XML `FEXGetPARAM_DST_CUIT`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamDstCuitResponse`, raíz XML `FEXGetPARAM_DST_CUITResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_DST_CUITResult` | [`tns:FEXResponse_DST_cuit`](#fexresponse_dst_cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_MON`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_MON`.

**Request** (`WsfexV1.FexGetParamMon`, raíz XML `FEXGetPARAM_MON`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamMonResponse`, raíz XML `FEXGetPARAM_MONResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_MONResult` | [`tns:FEXResponse_Mon`](#fexresponse_mon) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_MON_CON_COTIZACION`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_MON_CON_COTIZACION`.

**Request** (`WsfexV1.FexGetParamMonConCotizacion`, raíz XML `FEXGetPARAM_MON_CON_COTIZACION`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Fecha_CTZ` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamMonConCotizacionResponse`, raíz XML `FEXGetPARAM_MON_CON_COTIZACIONResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_MON_CON_COTIZACIONResult` | [`tns:FEXResponse_Mon_CON_COTIZACION`](#fexresponse_mon_con_cotizacion) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetLast_CMP`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetLast_CMP`.

**Request** (`WsfexV1.FexGetLastCmp`, raíz XML `FEXGetLast_CMP`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEX_LastCMP`](#clsfex_lastcmp) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetLastCmpResponse`, raíz XML `FEXGetLast_CMPResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetLast_CMPResult` | [`tns:FEXResponseLast_CMP`](#fexresponselast_cmp) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXDummy`

Clase: **consulta técnica**; autenticación: **no requiere ticket WSAA**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXDummy`.

**Request** (`sin elemento/payload`, raíz XML `FEXDummy`)


**Response** (`WsfexV1.FexDummyResponse`, raíz XML `FEXDummyResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXDummyResult` | [`tns:DummyResponse`](#dummyresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_Ctz`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Ctz`.

**Request** (`WsfexV1.FexGetParamCtz`, raíz XML `FEXGetPARAM_Ctz`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Mon_id` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FchCotiz` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamCtzResponse`, raíz XML `FEXGetPARAM_CtzResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_CtzResult` | [`tns:FEXResponse_Ctz`](#fexresponse_ctz) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetLast_ID`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetLast_ID`.

**Request** (`WsfexV1.FexGetLastId`, raíz XML `FEXGetLast_ID`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetLastIdResponse`, raíz XML `FEXGetLast_IDResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetLast_IDResult` | [`tns:FEXResponse_LastID`](#fexresponse_lastid) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_PtoVenta`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_PtoVenta`.

**Request** (`WsfexV1.FexGetParamPtoVenta`, raíz XML `FEXGetPARAM_PtoVenta`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamPtoVentaResponse`, raíz XML `FEXGetPARAM_PtoVentaResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_PtoVentaResult` | [`tns:FEXResponse_PtoVenta`](#fexresponse_ptoventa) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXCheck_Permiso`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXCheck_Permiso`.

**Request** (`WsfexV1.FexCheckPermiso`, raíz XML `FEXCheck_Permiso`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `ID_Permiso` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Dst_merc` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexCheckPermisoResponse`, raíz XML `FEXCheck_PermisoResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXCheck_PermisoResult` | [`tns:FEXResponse_CheckPermiso`](#fexresponse_checkpermiso) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_Opcionales`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Opcionales`.

**Request** (`WsfexV1.FexGetParamOpcionales`, raíz XML `FEXGetPARAM_Opcionales`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamOpcionalesResponse`, raíz XML `FEXGetPARAM_OpcionalesResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_OpcionalesResult` | [`tns:FEXResponse_Opc`](#fexresponse_opc) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `FEXGetPARAM_Actividades`

Clase: **consulta**; autenticación: **WSAA `wsfex`**. SOAPAction: `http://ar.gov.afip.dif.fexv1/FEXGetPARAM_Actividades`.

**Request** (`WsfexV1.FexGetParamActividades`, raíz XML `FEXGetPARAM_Actividades`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Auth` | [`tns:ClsFEXAuthRequest`](#clsfexauthrequest) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`WsfexV1.FexGetParamActividadesResponse`, raíz XML `FEXGetPARAM_ActividadesResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetPARAM_ActividadesResult` | [`tns:FEXResponse_Actividades`](#fexresponse_actividades) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

El resultado WSFEX incluye `FEXErr` (`ErrCode`, `ErrMsg`) y, según la operación, `Obs`/`Obs_comerciales`; revisar también el resultado de autorización devuelto. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


## Catálogo de tipos complejos

Incluye todos los tipos complejos nombrados del XSD del servicio. Los elementos `base` señalan herencia; se conserva la cardinalidad declarada. `xs:documentation` se transcribe cuando existe.


### `Actividad`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayOfActividad`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Actividad` | [`tns:Actividad`](#actividad) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfClsFEXResponse_ActividadTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ClsFEXResponse_ActividadTipo` | [`tns:ClsFEXResponse_ActividadTipo`](#clsfexresponse_actividadtipo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfClsFEXResponse_Cbte_Tipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ClsFEXResponse_Cbte_Tipo` | [`tns:ClsFEXResponse_Cbte_Tipo`](#clsfexresponse_cbte_tipo) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfClsFEXResponse_DST_cuit`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ClsFEXResponse_DST_cuit` | [`tns:ClsFEXResponse_DST_cuit`](#clsfexresponse_dst_cuit) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfClsFEXResponse_DST_pais`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ClsFEXResponse_DST_pais` | [`tns:ClsFEXResponse_DST_pais`](#clsfexresponse_dst_pais) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfClsFEXResponse_Idi`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ClsFEXResponse_Idi` | [`tns:ClsFEXResponse_Idi`](#clsfexresponse_idi) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfClsFEXResponse_Inc`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ClsFEXResponse_Inc` | [`tns:ClsFEXResponse_Inc`](#clsfexresponse_inc) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfClsFEXResponse_Mon`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ClsFEXResponse_Mon` | [`tns:ClsFEXResponse_Mon`](#clsfexresponse_mon) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfClsFEXResponse_Mon_CON_Cotizacion`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ClsFEXResponse_Mon_CON_Cotizacion` | [`tns:ClsFEXResponse_Mon_CON_Cotizacion`](#clsfexresponse_mon_con_cotizacion) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfClsFEXResponse_Opc`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ClsFEXResponse_Opc` | [`tns:ClsFEXResponse_Opc`](#clsfexresponse_opc) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfClsFEXResponse_PtoVenta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ClsFEXResponse_PtoVenta` | [`tns:ClsFEXResponse_PtoVenta`](#clsfexresponse_ptoventa) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfClsFEXResponse_Tex`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ClsFEXResponse_Tex` | [`tns:ClsFEXResponse_Tex`](#clsfexresponse_tex) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfClsFEXResponse_UMed`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ClsFEXResponse_UMed` | [`tns:ClsFEXResponse_UMed`](#clsfexresponse_umed) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfCmp_asoc`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Cmp_asoc` | [`tns:Cmp_asoc`](#cmp_asoc) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfItem`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Item` | [`tns:Item`](#item) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfOpcional`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Opcional` | [`tns:Opcional`](#opcional) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ArrayOfPermiso`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Permiso` | [`tns:Permiso`](#permiso) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `ClsFEXAuthRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Token` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Sign` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cuit` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ClsFEXErr`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ErrCode` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ErrMsg` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXEvents`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `EventCode` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `EventMsg` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXGetCMP`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Cbte_tipo` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Punto_vta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cbte_nro` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ClsFEXGetCMPR`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Fecha_cbte` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cbte_tipo` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Punto_vta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cbte_nro` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Tipo_expo` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Permiso_existente` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Permisos` | [`tns:ArrayOfPermiso`](#arrayofpermiso) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Dst_cmp` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cliente` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cuit_pais_cliente` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Domicilio_cliente` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Id_impositivo` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Moneda_Id` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Moneda_ctz` | `s:decimal` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CanMisMonExt` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Obs_comerciales` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Imp_total` | `s:decimal` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Obs` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cmps_asoc` | [`tns:ArrayOfCmp_asoc`](#arrayofcmp_asoc) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Forma_pago` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Incoterms` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Incoterms_Ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Idioma_cbte` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Items` | [`tns:ArrayOfItem`](#arrayofitem) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Fecha_cbte_cae` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Fch_venc_Cae` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cae` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Resultado` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Motivos_Obs` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Opcionales` | [`tns:ArrayOfOpcional`](#arrayofopcional) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Fecha_pago` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Actividades` | [`tns:ArrayOfActividad`](#arrayofactividad) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXOutAuthorize`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cuit` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cbte_tipo` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Punto_vta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cbte_nro` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cae` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Fch_venc_Cae` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Fch_cbte` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Resultado` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Reproceso` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Motivos_Obs` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Fecha_cbte` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cbte_Tipo` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Punto_vta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cbte_nro` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Tipo_expo` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Permiso_existente` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Permisos` | [`tns:ArrayOfPermiso`](#arrayofpermiso) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Dst_cmp` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cliente` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cuit_pais_cliente` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Domicilio_cliente` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Id_impositivo` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Moneda_Id` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Moneda_ctz` | `s:decimal` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `CanMisMonExt` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Obs_comerciales` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Imp_total` | `s:decimal` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Obs` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cmps_asoc` | [`tns:ArrayOfCmp_asoc`](#arrayofcmp_asoc) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Forma_pago` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Incoterms` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Incoterms_Ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Idioma_cbte` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Items` | [`tns:ArrayOfItem`](#arrayofitem) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Opcionales` | [`tns:ArrayOfOpcional`](#arrayofopcional) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Fecha_pago` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Actividades` | [`tns:ArrayOfActividad`](#arrayofactividad) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_ActividadTipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Orden` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Desc` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_Cbte_Tipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Cbte_Id` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cbte_Ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cbte_vig_desde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cbte_vig_hasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_CheckPermiso`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Status` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_Ctz`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Mon_ctz` | `s:decimal` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Mon_fecha` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_DST_cuit`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `DST_CUIT` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `DST_Ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_DST_pais`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `DST_Codigo` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `DST_Ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_Idi`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Idi_Id` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Idi_Ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Idi_vig_desde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Idi_vig_hasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_Inc`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Inc_Id` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Inc_Ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Inc_vig_desde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Inc_vig_hasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_LastID`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_Mon`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Mon_Id` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Mon_Ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Mon_vig_desde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Mon_vig_hasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_Mon_CON_Cotizacion`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Mon_Id` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Mon_Ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Mon_ctz` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Fecha_ctz` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_Opc`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Opc_Id` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Opc_Ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Opc_vig_desde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Opc_vig_hasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_PtoVenta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Pve_Nro` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Pve_Bloqueado` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Pve_FchBaja` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_Tex`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Tex_Id` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Tex_Ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Tex_vig_desde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Tex_vig_hasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEXResponse_UMed`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Umed_Id` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Umed_Ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Umed_vig_desde` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Umed_vig_hasta` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ClsFEX_LastCMP`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Token` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Sign` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Cuit` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Pto_venta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cbte_Tipo` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ClsFEX_LastCMP_Response`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Cbte_nro` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cbte_fecha` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Cmp_asoc`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Cbte_tipo` | `s:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cbte_punto_vta` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cbte_nro` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Cbte_cuit` | `s:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DummyResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `AppServer` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `DbServer` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `AuthServer` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXGetCMPResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXGetCMPResult` | [`tns:FEXGetCMPResponse`](#fexgetcmpresponse) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponseAuthorize`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultAuth` | [`tns:ClsFEXOutAuthorize`](#clsfexoutauthorize) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponseLast_CMP`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResult_LastCMP` | [`tns:ClsFEX_LastCMP_Response`](#clsfex_lastcmp_response) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_Actividades`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ArrayOfClsFEXResponse_ActividadTipo`](#arrayofclsfexresponse_actividadtipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_Cbte_Tipo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ArrayOfClsFEXResponse_Cbte_Tipo`](#arrayofclsfexresponse_cbte_tipo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_CheckPermiso`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ClsFEXResponse_CheckPermiso`](#clsfexresponse_checkpermiso) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_Ctz`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ClsFEXResponse_Ctz`](#clsfexresponse_ctz) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_DST_cuit`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ArrayOfClsFEXResponse_DST_cuit`](#arrayofclsfexresponse_dst_cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_DST_pais`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ArrayOfClsFEXResponse_DST_pais`](#arrayofclsfexresponse_dst_pais) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_Idi`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ArrayOfClsFEXResponse_Idi`](#arrayofclsfexresponse_idi) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_Inc`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ArrayOfClsFEXResponse_Inc`](#arrayofclsfexresponse_inc) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_LastID`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ClsFEXResponse_LastID`](#clsfexresponse_lastid) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_Mon`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ArrayOfClsFEXResponse_Mon`](#arrayofclsfexresponse_mon) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_Mon_CON_COTIZACION`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ArrayOfClsFEXResponse_Mon_CON_Cotizacion`](#arrayofclsfexresponse_mon_con_cotizacion) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_Opc`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ArrayOfClsFEXResponse_Opc`](#arrayofclsfexresponse_opc) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_PtoVenta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ArrayOfClsFEXResponse_PtoVenta`](#arrayofclsfexresponse_ptoventa) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_Tex`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ArrayOfClsFEXResponse_Tex`](#arrayofclsfexresponse_tex) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `FEXResponse_Umed`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `FEXResultGet` | [`tns:ArrayOfClsFEXResponse_UMed`](#arrayofclsfexresponse_umed) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXErr` | [`tns:ClsFEXErr`](#clsfexerr) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `FEXEvents` | [`tns:ClsFEXEvents`](#clsfexevents) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Item`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Pro_codigo` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Pro_ds` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Pro_qty` | `s:decimal` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Pro_umed` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Pro_precio_uni` | `s:decimal` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Pro_bonificacion` | `s:decimal` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `Pro_total_item` | `s:decimal` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `Opcional`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Valor` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Permiso`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `Id_permiso` | `s:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `Dst_merc` | `s:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
