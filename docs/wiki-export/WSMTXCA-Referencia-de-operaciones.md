<!-- Source: docs/reference/operations/wsmtxca.md. Generated wiki mirror; edit the repository source. -->

# WSMTXCA: factura electrónica

**Rol:** Servicio de factura electrónica con operaciones de autorización y consulta.

**Funcionalidad:** Autorizar comprobantes y ajustes, informar CAEA y consultar catálogos, estados y comprobantes.

**Uso:** Elegir las operaciones según el circuito habilitado para el contribuyente y validar los códigos de resultado con el manual vigente.

Contrato generado desde [`wsmtxca-production.wsdl`](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/docs/reference/contracts/wsmtxca-production.wsdl); namespace XML `http://impl.service.wsmtxca.afip.gov.ar/service/`. El servicio WSAA es `wsmtxca`. Las operaciones autenticadas envían ticket WSAA; `dummy` es una comprobación no autenticada según el cliente generado.

Los tipos C# indicados proceden de los contratos generados y el WSDL fijado. `minOccurs`, `maxOccurs` y `nillable` describen estructura XML; **no sustituyen reglas de negocio** ni garantizan aceptación por ARCA. No se inventan validaciones de negocio.

## Operaciones disponibles

| Operación | Clase | Request C# | Response C# | SOAPAction |
|---|---|---|---|---|
| `dummy` | consulta técnica, sin WSAA | sin DTO | `Wsmtxca.DummyResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/dummy` |
| `autorizarComprobante` | requiere ticket WSAA | `Wsmtxca.AutorizarComprobanteRequestType` | `Wsmtxca.AutorizarComprobanteResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/autorizarComprobante` |
| `solicitarCAEA` | requiere ticket WSAA | `Wsmtxca.SolicitarCaeaRequestType` | `Wsmtxca.SolicitarCaeaResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/solicitarCAEA` |
| `informarComprobanteCAEA` | requiere ticket WSAA | `Wsmtxca.InformarComprobanteCaeaRequestType` | `Wsmtxca.InformarComprobanteCaeaResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/informarComprobanteCAEA` |
| `consultarUltimoComprobanteAutorizado` | requiere ticket WSAA | `Wsmtxca.ConsultarUltimoComprobanteAutorizadoRequestType` | `Wsmtxca.ConsultarUltimoComprobanteAutorizadoResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarUltimoComprobanteAutorizado` |
| `consultarComprobante` | requiere ticket WSAA | `Wsmtxca.ConsultarComprobanteRequestType` | `Wsmtxca.ConsultarComprobanteResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarComprobante` |
| `consultarTiposComprobante` | requiere ticket WSAA | `Wsmtxca.ConsultarTiposComprobanteRequestType` | `Wsmtxca.ConsultarTiposComprobanteResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarTiposComprobante` |
| `consultarTiposDocumento` | requiere ticket WSAA | `Wsmtxca.ConsultarTiposDocumentoRequestType` | `Wsmtxca.ConsultarTiposDocumentoResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarTiposDocumento` |
| `consultarAlicuotasIVA` | requiere ticket WSAA | `Wsmtxca.ConsultarAlicuotasIvaRequestType` | `Wsmtxca.ConsultarAlicuotasIvaResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarAlicuotasIVA` |
| `consultarCondicionesIVA` | requiere ticket WSAA | `Wsmtxca.ConsultarCondicionesIvaRequestType` | `Wsmtxca.ConsultarCondicionesIvaResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarCondicionesIVA` |
| `consultarMonedas` | requiere ticket WSAA | `Wsmtxca.ConsultarMonedasRequestType` | `Wsmtxca.ConsultarMonedasResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarMonedas` |
| `consultarCotizacionMoneda` | requiere ticket WSAA | `Wsmtxca.ConsultarCotizacionMonedaRequestType` | `Wsmtxca.ConsultarCotizacionMonedaResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarCotizacionMoneda` |
| `consultarUnidadesMedida` | requiere ticket WSAA | `Wsmtxca.ConsultarUnidadesMedidaRequestType` | `Wsmtxca.ConsultarUnidadesMedidaResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarUnidadesMedida` |
| `consultarTiposTributo` | requiere ticket WSAA | `Wsmtxca.ConsultarTiposTributoRequestType` | `Wsmtxca.ConsultarTiposTributoResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarTiposTributo` |
| `consultarPuntosVenta` | requiere ticket WSAA | `Wsmtxca.ConsultarPuntosVentaRequestType` | `Wsmtxca.ConsultarPuntosVentaResponse` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarPuntosVenta` |
| `consultarPuntosVentaCAE` | requiere ticket WSAA | `Wsmtxca.ConsultarPuntosVentaCaeRequestType` | `Wsmtxca.ConsultarPuntosVentaCaeResponse` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarPuntosVentaCAE` |
| `consultarPuntosVentaCAEA` | requiere ticket WSAA | `Wsmtxca.ConsultarPuntosVentaCaeaRequestType` | `Wsmtxca.ConsultarPuntosVentaCaeaResponse` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarPuntosVentaCAEA` |
| `informarCAEANoUtilizado` | requiere ticket WSAA | `Wsmtxca.InformarCaeaNoUtilizadoRequestType` | `Wsmtxca.InformarCaeaNoUtilizadoResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/informarCAEANoUtilizado` |
| `informarCAEANoUtilizadoPtoVta` | requiere ticket WSAA | `Wsmtxca.InformarCaeaNoUtilizadoPtoVtaRequestType` | `Wsmtxca.InformarCaeaNoUtilizadoPtoVtaResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/informarCAEANoUtilizadoPtoVta` |
| `consultarPtosVtaCAEANoInformados` | requiere ticket WSAA | `Wsmtxca.ConsultarPtosVtaCaeaNoInformadosRequestType` | `Wsmtxca.ConsultarPtosVtaCaeaNoInformadosResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarPtosVtaCAEANoInformados` |
| `consultarCAEA` | requiere ticket WSAA | `Wsmtxca.ConsultarCaeaRequestType` | `Wsmtxca.ConsultarCaeaResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarCAEA` |
| `consultarCAEAEntreFechas` | requiere ticket WSAA | `Wsmtxca.ConsultarCaeaEntreFechasRequestType` | `Wsmtxca.ConsultarCaeaEntreFechasResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarCAEAEntreFechas` |
| `autorizarAjusteIVA` | requiere ticket WSAA | `Wsmtxca.AutorizarAjusteIvaRequestType` | `Wsmtxca.AutorizarAjusteIvaResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/autorizarAjusteIVA` |
| `informarAjusteIVACAEA` | requiere ticket WSAA | `Wsmtxca.InformarAjusteIvacaeaRequestType` | `Wsmtxca.InformarAjusteIvacaeaResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/informarAjusteIVACAEA` |
| `consultarTiposDatosAdicionales` | requiere ticket WSAA | `Wsmtxca.ConsultarTiposDatosAdicionalesRequestType` | `Wsmtxca.ConsultarTiposDatosAdicionalesResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarTiposDatosAdicionales` |
| `consultarActividadesVigentes` | requiere ticket WSAA | `Wsmtxca.ConsultarActividadesVigentesRequestType` | `Wsmtxca.ConsultarActividadesVigentesResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarActividadesVigentes` |
| `consultarCondicionesIVAReceptor` | requiere ticket WSAA | `Wsmtxca.ConsultarCondicionesIvaReceptorRequestType` | `Wsmtxca.ConsultarCondicionesIvaReceptorResponseType` | `http://impl.service.wsmtxca.afip.gov.ar/service/consultarCondicionesIVAReceptor` |

## Campos y tipos

Cada tabla lista los campos XML del elemento raíz de request/response. Las referencias a tipos nombrados se amplían en el catálogo de tipos complejos. Para respuestas, `Errors`/`Observaciones` (cuando el tipo los declara) transportan rechazos y avisos del servicio; las tablas de errores ARCA por código deben contrastarse con el manual vigente del servicio. Un SOAP Fault de transporte/protocolo se expone como `NetArcaWs.Transport.SoapFaultException` con `Code`, `Reason`, `Detail` y `StatusCode`. No reintentar automáticamente escrituras: ante timeout o resultado incierto, reconciliar el estado antes de cualquier nuevo envío.


### `dummy`

Clase: **consulta técnica**; autenticación: **no requiere ticket WSAA**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/dummy`.

**Request** (`cuerpo SOAP vacío`, raíz XML `(vacío)`)


**Response** (`Wsmtxca.DummyResponseType`, raíz XML `dummyResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `appserver` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `authserver` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `dbserver` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `autorizarComprobante`

Clase: **escritura**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/autorizarComprobante`.

**Request** (`Wsmtxca.AutorizarComprobanteRequestType`, raíz XML `autorizarComprobanteRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteCAERequest` | [`tns:ComprobanteType`](#comprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.AutorizarComprobanteResponseType`, raíz XML `autorizarComprobanteResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteResponse` | [`tns:ComprobanteCAEResponseType`](#comprobantecaeresponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `solicitarCAEA`

Clase: **escritura**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/solicitarCAEA`.

**Request** (`Wsmtxca.SolicitarCaeaRequestType`, raíz XML `solicitarCAEARequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitudCAEA` | [`tns:SolicitudCAEAType`](#solicitudcaeatype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.SolicitarCaeaResponseType`, raíz XML `solicitarCAEAResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CAEAResponse` | [`tns:CAEAResponseType`](#caearesponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `informarComprobanteCAEA`

Clase: **escritura**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/informarComprobanteCAEA`.

**Request** (`Wsmtxca.InformarComprobanteCaeaRequestType`, raíz XML `informarComprobanteCAEARequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteCAEARequest` | [`tns:ComprobanteType`](#comprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.InformarComprobanteCaeaResponseType`, raíz XML `informarComprobanteCAEAResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaProceso` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteCAEAResponse` | [`tns:ComprobanteCAEAResponseType`](#comprobantecaearesponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarUltimoComprobanteAutorizado`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarUltimoComprobanteAutorizado`.

**Request** (`Wsmtxca.ConsultarUltimoComprobanteAutorizadoRequestType`, raíz XML `consultarUltimoComprobanteAutorizadoRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `consultaUltimoComprobanteAutorizadoRequest` | [`tns:ConsultaUltimoComprobanteAutorizadoRequestType`](#consultaultimocomprobanteautorizadorequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarUltimoComprobanteAutorizadoResponseType`, raíz XML `consultarUltimoComprobanteAutorizadoResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `numeroComprobante` | [`tns:NumeroComprobanteSimpleType`](#numerocomprobantesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarComprobante`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarComprobante`.

**Request** (`Wsmtxca.ConsultarComprobanteRequestType`, raíz XML `consultarComprobanteRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `consultaComprobanteRequest` | [`tns:ConsultaComprobanteRequestType`](#consultacomprobanterequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarComprobanteResponseType`, raíz XML `consultarComprobanteResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `comprobante` | [`tns:ComprobanteType`](#comprobantetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarTiposComprobante`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarTiposComprobante`.

**Request** (`Wsmtxca.ConsultarTiposComprobanteRequestType`, raíz XML `consultarTiposComprobanteRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarTiposComprobanteResponseType`, raíz XML `consultarTiposComprobanteResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayTiposComprobante` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarTiposDocumento`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarTiposDocumento`.

**Request** (`Wsmtxca.ConsultarTiposDocumentoRequestType`, raíz XML `consultarTiposDocumentoRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarTiposDocumentoResponseType`, raíz XML `consultarTiposDocumentoResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayTiposDocumento` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarAlicuotasIVA`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarAlicuotasIVA`.

**Request** (`Wsmtxca.ConsultarAlicuotasIvaRequestType`, raíz XML `consultarAlicuotasIVARequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarAlicuotasIvaResponseType`, raíz XML `consultarAlicuotasIVAResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayAlicuotasIVA` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCondicionesIVA`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarCondicionesIVA`.

**Request** (`Wsmtxca.ConsultarCondicionesIvaRequestType`, raíz XML `consultarCondicionesIVARequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarCondicionesIvaResponseType`, raíz XML `consultarCondicionesIVAResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayCondicionesIVA` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarMonedas`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarMonedas`.

**Request** (`Wsmtxca.ConsultarMonedasRequestType`, raíz XML `consultarMonedasRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarMonedasResponseType`, raíz XML `consultarMonedasResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayMonedas` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCotizacionMoneda`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarCotizacionMoneda`.

**Request** (`Wsmtxca.ConsultarCotizacionMonedaRequestType`, raíz XML `consultarCotizacionMonedaRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codigoMoneda` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaCotizacion` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarCotizacionMonedaResponseType`, raíz XML `consultarCotizacionMonedaResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cotizacionMoneda` | `xsd:decimal` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarUnidadesMedida`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarUnidadesMedida`.

**Request** (`Wsmtxca.ConsultarUnidadesMedidaRequestType`, raíz XML `consultarUnidadesMedidaRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarUnidadesMedidaResponseType`, raíz XML `consultarUnidadesMedidaResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayUnidadesMedida` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarTiposTributo`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarTiposTributo`.

**Request** (`Wsmtxca.ConsultarTiposTributoRequestType`, raíz XML `consultarTiposTributoRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarTiposTributoResponseType`, raíz XML `consultarTiposTributoResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayTiposTributo` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarPuntosVenta`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarPuntosVenta`.

**Request** (`Wsmtxca.ConsultarPuntosVentaRequestType`, raíz XML `consultarPuntosVentaRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarPuntosVentaResponse`, raíz XML `consultarPuntosVentaResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayPuntosVenta` | [`tns:ArrayPuntosVentaType`](#arraypuntosventatype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarPuntosVentaCAE`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarPuntosVentaCAE`.

**Request** (`Wsmtxca.ConsultarPuntosVentaCaeRequestType`, raíz XML `consultarPuntosVentaCAERequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarPuntosVentaCaeResponse`, raíz XML `consultarPuntosVentaCAEResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayPuntosVenta` | [`tns:ArrayPuntosVentaType`](#arraypuntosventatype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarPuntosVentaCAEA`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarPuntosVentaCAEA`.

**Request** (`Wsmtxca.ConsultarPuntosVentaCaeaRequestType`, raíz XML `consultarPuntosVentaCAEARequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarPuntosVentaCaeaResponse`, raíz XML `consultarPuntosVentaCAEAResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayPuntosVenta` | [`tns:ArrayPuntosVentaType`](#arraypuntosventatype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `informarCAEANoUtilizado`

Clase: **escritura**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/informarCAEANoUtilizado`.

**Request** (`Wsmtxca.InformarCaeaNoUtilizadoRequestType`, raíz XML `informarCAEANoUtilizadoRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.InformarCaeaNoUtilizadoResponseType`, raíz XML `informarCAEANoUtilizadoResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaProceso` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `informarCAEANoUtilizadoPtoVta`

Clase: **escritura**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/informarCAEANoUtilizadoPtoVta`.

**Request** (`Wsmtxca.InformarCaeaNoUtilizadoPtoVtaRequestType`, raíz XML `informarCAEANoUtilizadoPtoVtaRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroPuntoVenta` | [`tns:NumeroPuntoVentaSimpleType`](#numeropuntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.InformarCaeaNoUtilizadoPtoVtaResponseType`, raíz XML `informarCAEANoUtilizadoPtoVtaResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaProceso` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroPuntoVenta` | [`tns:NumeroPuntoVentaSimpleType`](#numeropuntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarPtosVtaCAEANoInformados`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarPtosVtaCAEANoInformados`.

**Request** (`Wsmtxca.ConsultarPtosVtaCaeaNoInformadosRequestType`, raíz XML `consultarPtosVtaCAEANoInformadosRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarPtosVtaCaeaNoInformadosResponseType`, raíz XML `consultarPtosVtaCAEANoInformadosResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayPuntosVenta` | [`tns:ArrayPuntosVentaType`](#arraypuntosventatype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCAEA`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarCAEA`.

**Request** (`Wsmtxca.ConsultarCaeaRequestType`, raíz XML `consultarCAEARequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarCaeaResponseType`, raíz XML `consultarCAEAResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CAEAResponse` | [`tns:CAEAResponseType`](#caearesponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCAEAEntreFechas`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarCAEAEntreFechas`.

**Request** (`Wsmtxca.ConsultarCaeaEntreFechasRequestType`, raíz XML `consultarCAEAEntreFechasRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaDesde` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHasta` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarCaeaEntreFechasResponseType`, raíz XML `consultarCAEAEntreFechasResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayCAEAResponse` | [`tns:ArrayCAEAResponseType`](#arraycaearesponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `autorizarAjusteIVA`

Clase: **escritura**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/autorizarAjusteIVA`.

**Request** (`Wsmtxca.AutorizarAjusteIvaRequestType`, raíz XML `autorizarAjusteIVARequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteCAERequest` | [`tns:ComprobanteType`](#comprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.AutorizarAjusteIvaResponseType`, raíz XML `autorizarAjusteIVAResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteResponse` | [`tns:ComprobanteCAEResponseType`](#comprobantecaeresponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `informarAjusteIVACAEA`

Clase: **escritura**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/informarAjusteIVACAEA`.

**Request** (`Wsmtxca.InformarAjusteIvacaeaRequestType`, raíz XML `informarAjusteIVACAEARequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteCAEARequest` | [`tns:ComprobanteType`](#comprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.InformarAjusteIvacaeaResponseType`, raíz XML `informarAjusteIVACAEAResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaProceso` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteCAEAResponse` | [`tns:ComprobanteCAEAResponseType`](#comprobantecaearesponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarTiposDatosAdicionales`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarTiposDatosAdicionales`.

**Request** (`Wsmtxca.ConsultarTiposDatosAdicionalesRequestType`, raíz XML `consultarTiposDatosAdicionalesRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarTiposDatosAdicionalesResponseType`, raíz XML `consultarTiposDatosAdicionalesResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayTiposDatosAdicionales` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarActividadesVigentes`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarActividadesVigentes`.

**Request** (`Wsmtxca.ConsultarActividadesVigentesRequestType`, raíz XML `consultarActividadesVigentesRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarActividadesVigentesResponseType`, raíz XML `consultarActividadesVigentesResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayActividades` | [`tns:ArrayActividadesVigentesType`](#arrayactividadesvigentestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCondicionesIVAReceptor`

Clase: **consulta**; autenticación: **WSAA `wsmtxca`**. SOAPAction: `http://impl.service.wsmtxca.afip.gov.ar/service/consultarCondicionesIVAReceptor`.

**Request** (`Wsmtxca.ConsultarCondicionesIvaReceptorRequestType`, raíz XML `consultarCondicionesIVAReceptorRequest`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `consultaCondicionesIVAReceptorRequest` | [`tns:ConsultaCondicionesIVARequestType`](#consultacondicionesivarequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wsmtxca.ConsultarCondicionesIvaReceptorResponseType`, raíz XML `consultarCondicionesIVAReceptorResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayCondicionesIVAReceptor` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Las respuestas WSMTXCA exponen `resultado` y, según operación, `arrayErrores` y `arrayObservaciones` como códigos y descripciones; interpretar sus valores con el manual vigente. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


## Catálogo de tipos complejos

Incluye todos los tipos complejos nombrados del XSD del servicio. Los elementos `base` señalan herencia; se conserva la cardinalidad declarada. `xs:documentation` se transcribe cuando existe.


### `ActividadType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ActividadVigenteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `orden` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayActividadesType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `actividad` | [`tns:ActividadType`](#actividadtype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayActividadesVigentesType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `actividad` | [`tns:ActividadVigenteType`](#actividadvigentetype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayCAEAResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CAEAResponse` | [`tns:CAEAResponseType`](#caearesponsetype) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |

### `ArrayCodigosDescripcionesStringType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoDescripcion` | [`tns:CodigoDescripcionStringType`](#codigodescripcionstringtype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayCodigosDescripcionesType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoDescripcion` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayCompradoresType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `comprador` | [`tns:CompradorType`](#compradortype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayComprobantesAsociadosType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `comprobanteAsociado` | [`tns:ComprobanteAsociadoType`](#comprobanteasociadotype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayDatosAdicionalesType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `datoAdicional` | [`tns:DatoAdicionalType`](#datoadicionaltype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayItemsType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `item` | [`tns:ItemType`](#itemtype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayObservacionesType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `observacion` | `xsd:string` | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayOtrosTributosType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `otroTributo` | [`tns:OtroTributoType`](#otrotributotype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayPuntosVentaType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `puntoVenta` | [`tns:PuntoVentaType`](#puntoventatype) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |

### `ArraySubtotalesIVAType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `subtotalIVA` | [`tns:SubtotalIVAType`](#subtotalivatype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayTiposComprobanteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoDescripcion` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `ArrayTiposDocumentoType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoDescripcion` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `AuthRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarAjusteIVARequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteCAERequest` | [`tns:ComprobanteType`](#comprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarAjusteIVAResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteResponse` | [`tns:ComprobanteCAEResponseType`](#comprobantecaeresponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `AutorizarComprobanteRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteCAERequest` | [`tns:ComprobanteType`](#comprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarComprobanteResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteResponse` | [`tns:ComprobanteCAEResponseType`](#comprobantecaeresponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CAEAResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `fechaProceso` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `periodo` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `orden` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaDesde` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHasta` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaTopeInforme` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

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

### `CompradorType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoTipoDocumento` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroDocumento` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `porcentaje` | [`tns:PorcentajeSimpleType`](#porcentajesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ComprobanteAsociadoType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoTipoComprobante` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroPuntoVenta` | [`tns:NumeroPuntoVentaSimpleType`](#numeropuntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroComprobante` | [`tns:NumeroComprobanteSimpleType`](#numerocomprobantesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuit` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaEmision` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ComprobanteCAEAResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codigoTipoComprobante` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroPuntoVenta` | [`tns:NumeroPuntoVentaSimpleType`](#numeropuntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroComprobante` | [`tns:NumeroComprobanteSimpleType`](#numerocomprobantesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ComprobanteCAEResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codigoTipoComprobante` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroPuntoVenta` | [`tns:NumeroPuntoVentaSimpleType`](#numeropuntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroComprobante` | [`tns:NumeroComprobanteSimpleType`](#numerocomprobantesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaEmision` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAE` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaVencimientoCAE` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ComprobanteType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoTipoComprobante` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroPuntoVenta` | [`tns:NumeroPuntoVentaSimpleType`](#numeropuntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroComprobante` | [`tns:NumeroComprobanteSimpleType`](#numerocomprobantesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaEmision` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigoTipoAutorizacion` | [`tns:CodigoTipoAutorizacionSimpleType`](#codigotipoautorizacionsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigoAutorizacion` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaVencimiento` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigoTipoDocumento` | `xsd:short` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `numeroDocumento` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `condicionIVAReceptor` | `xsd:short` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeGravado` | [`tns:ImporteSubtotalSimpleType`](#importesubtotalsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeNoGravado` | [`tns:ImporteSubtotalSimpleType`](#importesubtotalsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeExento` | [`tns:ImporteSubtotalSimpleType`](#importesubtotalsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeSubtotal` | [`tns:ImporteSubtotalSimpleType`](#importesubtotalsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importeOtrosTributos` | [`tns:ImporteTotalSimpleType`](#importetotalsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeTotal` | [`tns:ImporteTotalSimpleType`](#importetotalsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codigoMoneda` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cotizacionMoneda` | `xsd:decimal` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cancelaEnMismaMonedaExtranjera` | [`tns:SiNoSimpleType`](#sinosimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `observaciones` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigoConcepto` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaServicioDesde` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaServicioHasta` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaVencimientoPago` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaHoraGen` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayComprobantesAsociados` | [`tns:ArrayComprobantesAsociadosType`](#arraycomprobantesasociadostype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `periodoComprobantesAsociados` | [`tns:PeriodoComprobantesAsociadosType`](#periodocomprobantesasociadostype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayOtrosTributos` | [`tns:ArrayOtrosTributosType`](#arrayotrostributostype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayItems` | [`tns:ArrayItemsType`](#arrayitemstype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arraySubtotalesIVA` | [`tns:ArraySubtotalesIVAType`](#arraysubtotalesivatype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayDatosAdicionales` | [`tns:ArrayDatosAdicionalesType`](#arraydatosadicionalestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayCompradores` | [`tns:ArrayCompradoresType`](#arraycompradorestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayActividades` | [`tns:ArrayActividadesType`](#arrayactividadestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultaComprobanteRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoTipoComprobante` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroPuntoVenta` | [`tns:NumeroPuntoVentaSimpleType`](#numeropuntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroComprobante` | [`tns:NumeroComprobanteSimpleType`](#numerocomprobantesimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultaCondicionesIVARequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoTipoComprobante` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultaUltimoComprobanteAutorizadoRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigoTipoComprobante` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroPuntoVenta` | [`tns:NumeroPuntoVentaSimpleType`](#numeropuntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarActividadesVigentesRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarActividadesVigentesResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayActividades` | [`tns:ArrayActividadesVigentesType`](#arrayactividadesvigentestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarAlicuotasIVARequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarAlicuotasIVAResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayAlicuotasIVA` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCAEAEntreFechasRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaDesde` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHasta` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCAEAEntreFechasResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayCAEAResponse` | [`tns:ArrayCAEAResponseType`](#arraycaearesponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCAEARequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCAEAResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CAEAResponse` | [`tns:CAEAResponseType`](#caearesponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarComprobanteRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `consultaComprobanteRequest` | [`tns:ConsultaComprobanteRequestType`](#consultacomprobanterequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarComprobanteResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `comprobante` | [`tns:ComprobanteType`](#comprobantetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCondicionesIVAReceptorRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `consultaCondicionesIVAReceptorRequest` | [`tns:ConsultaCondicionesIVARequestType`](#consultacondicionesivarequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCondicionesIVAReceptorResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayCondicionesIVAReceptor` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCondicionesIVARequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCondicionesIVAResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayCondicionesIVA` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCotizacionMonedaRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codigoMoneda` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaCotizacion` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCotizacionMonedaResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cotizacionMoneda` | `xsd:decimal` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarMonedasRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarMonedasResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayMonedas` | [`tns:ArrayCodigosDescripcionesStringType`](#arraycodigosdescripcionesstringtype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarPtosVtaCAEANoInformadosRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarPtosVtaCAEANoInformadosResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayPuntosVenta` | [`tns:ArrayPuntosVentaType`](#arraypuntosventatype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarPuntosVentaCAEARequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarPuntosVentaCAERequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarPuntosVentaRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarPuntosVentaResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayPuntosVenta` | [`tns:ArrayPuntosVentaType`](#arraypuntosventatype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarTiposComprobanteRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarTiposComprobanteResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayTiposComprobante` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarTiposDatosAdicionalesRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarTiposDatosAdicionalesResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayTiposDatosAdicionales` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarTiposDocumentoRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarTiposDocumentoResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayTiposDocumento` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarTiposTributoRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarTiposTributoResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayTiposTributo` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarUltimoComprobanteAutorizadoRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `consultaUltimoComprobanteAutorizadoRequest` | [`tns:ConsultaUltimoComprobanteAutorizadoRequestType`](#consultaultimocomprobanteautorizadorequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarUltimoComprobanteAutorizadoResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `numeroComprobante` | [`tns:NumeroComprobanteSimpleType`](#numerocomprobantesimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarUnidadesMedidaRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarUnidadesMedidaResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `arrayUnidadesMedida` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DatoAdicionalType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `t` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `c1` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `c2` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `c3` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `c4` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `c5` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `c6` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DummyResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `appserver` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `authserver` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `dbserver` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ExceptionResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `exception` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `InformarAjusteIVACAEARequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteCAEARequest` | [`tns:ComprobanteType`](#comprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `InformarAjusteIVACAEAResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaProceso` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteCAEAResponse` | [`tns:ComprobanteCAEAResponseType`](#comprobantecaearesponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `InformarCAEANoUtilizadoPtoVtaRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroPuntoVenta` | [`tns:NumeroPuntoVentaSimpleType`](#numeropuntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `InformarCAEANoUtilizadoPtoVtaResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaProceso` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `numeroPuntoVenta` | [`tns:NumeroPuntoVentaSimpleType`](#numeropuntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `InformarCAEANoUtilizadoRequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `InformarCAEANoUtilizadoResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaProceso` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `CAEA` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `InformarComprobanteCAEARequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteCAEARequest` | [`tns:ComprobanteType`](#comprobantetype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `InformarComprobanteCAEAResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `resultado` | [`tns:ResultadoSimpleType`](#resultadosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaProceso` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `comprobanteCAEAResponse` | [`tns:ComprobanteCAEAResponseType`](#comprobantecaearesponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayObservaciones` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ItemType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `unidadesMtx` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigoMtx` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigo` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cantidad` | [`tns:DecimalSimpleType`](#decimalsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigoUnidadMedida` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `precioUnitario` | [`tns:DecimalSimpleType`](#decimalsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeBonificacion` | [`tns:DecimalSimpleType`](#decimalsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigoCondicionIVA` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importeIVA` | [`tns:ImporteSubtotalSimpleType`](#importesubtotalsimpletype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `importeItem` | [`tns:ImporteSubtotalSimpleType`](#importesubtotalsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `OtroTributoType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `baseImponible` | [`tns:ImporteTotalSimpleType`](#importetotalsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importe` | [`tns:ImporteTotalSimpleType`](#importetotalsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `PeriodoComprobantesAsociadosType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `fechaDesde` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHasta` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `PuntoVentaType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `numeroPuntoVenta` | [`tns:NumeroPuntoVentaSimpleType`](#numeropuntoventasimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `bloqueado` | [`tns:SiNoSimpleType`](#sinosimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaBaja` | `xsd:date` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `SolicitarCAEARequestType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `authRequest` | [`tns:AuthRequestType`](#authrequesttype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitudCAEA` | [`tns:SolicitudCAEAType`](#solicitudcaeatype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `SolicitarCAEAResponseType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `CAEAResponse` | [`tns:CAEAResponseType`](#caearesponsetype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `arrayErrores` | [`tns:ArrayCodigosDescripcionesType`](#arraycodigosdescripcionestype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `evento` | [`tns:CodigoDescripcionType`](#codigodescripciontype) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `SolicitudCAEAType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `periodo` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `orden` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `SubtotalIVAType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:short` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `importe` | [`tns:ImporteSubtotalSimpleType`](#importesubtotalsimpletype) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
