# Padrón A5: constancia de inscripción

**Rol:** Consulta de constancia y datos de inscripción.

**Funcionalidad:** Consultar personas y listas, con variantes de respuesta v2.

**Uso:** Usarlo para recuperar datos de inscripción; verificar en el manual la variante adecuada y tratar faults `SRValidationException`.

Contrato generado desde [`padron-a5-production.wsdl`](../contracts/padron-a5-production.wsdl); namespace XML `http://a5.soap.ws.server.puc.sr/`. El servicio WSAA es `ws_sr_constancia_inscripcion`. Las operaciones autenticadas envían ticket WSAA; `dummy` es una comprobación no autenticada según el cliente generado.

Los tipos C# indicados proceden de los contratos generados y el WSDL fijado. `minOccurs`, `maxOccurs` y `nillable` describen estructura XML; **no sustituyen reglas de negocio** ni garantizan aceptación por ARCA. No se inventan validaciones de negocio.

## Operaciones disponibles

| Operación | Clase | Request C# | Response C# | SOAPAction |
|---|---|---|---|---|
| `getPersona` | requiere ticket WSAA | `PadronA5.GetPersona` | `PadronA5.GetPersonaResponse` | `` |
| `getPersonaList` | requiere ticket WSAA | `PadronA5.GetPersonaList` | `PadronA5.GetPersonaListResponse` | `` |
| `getPersona_v2` | requiere ticket WSAA | `PadronA5.GetPersonaV2` | `PadronA5.GetPersonaV2Response` | `` |
| `dummy` | consulta técnica, sin WSAA | sin DTO | `PadronA5.DummyResponse` | `` |
| `getPersonaList_v2` | requiere ticket WSAA | `PadronA5.GetPersonaListV2` | `PadronA5.GetPersonaListV2Response` | `` |

## Campos y tipos

Cada tabla lista los campos XML del elemento raíz de request/response. Las referencias a tipos nombrados se amplían en el catálogo de tipos complejos. Para respuestas, `Errors`/`Observaciones` (cuando el tipo los declara) transportan rechazos y avisos del servicio; las tablas de errores ARCA por código deben contrastarse con el manual vigente del servicio. Un SOAP Fault de transporte/protocolo se expone como `NetArcaWs.Transport.SoapFaultException` con `Code`, `Reason`, `Detail` y `StatusCode`. No reintentar automáticamente escrituras: ante timeout o resultado incierto, reconciliar el estado antes de cualquier nuevo envío.


### `getPersona`

Clase: **consulta**; autenticación: **WSAA `ws_sr_constancia_inscripcion`**. SOAPAction: ``.

**Request** (`PadronA5.GetPersona`, raíz XML `getPersona`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`PadronA5.GetPersonaResponse`, raíz XML `getPersonaResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `personaReturn` | [`tns:personaReturn`](#personareturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Los WSDL de Padrón declaran `SRValidationException` como fault SOAP de servicio; su estructura aparece en el catálogo XSD y los errores no se expresan como `Errors` dentro de un resultado normal. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `getPersonaList`

Clase: **consulta**; autenticación: **WSAA `ws_sr_constancia_inscripcion`**. SOAPAction: ``.

**Request** (`PadronA5.GetPersonaList`, raíz XML `getPersonaList`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`PadronA5.GetPersonaListResponse`, raíz XML `getPersonaListResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `personaListReturn` | [`tns:personaListReturn`](#personalistreturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Los WSDL de Padrón declaran `SRValidationException` como fault SOAP de servicio; su estructura aparece en el catálogo XSD y los errores no se expresan como `Errors` dentro de un resultado normal. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `getPersona_v2`

Clase: **consulta**; autenticación: **WSAA `ws_sr_constancia_inscripcion`**. SOAPAction: ``.

**Request** (`PadronA5.GetPersonaV2`, raíz XML `getPersona_v2`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`PadronA5.GetPersonaV2Response`, raíz XML `getPersona_v2Response`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `personaReturn` | [`tns:personaReturn`](#personareturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Los WSDL de Padrón declaran `SRValidationException` como fault SOAP de servicio; su estructura aparece en el catálogo XSD y los errores no se expresan como `Errors` dentro de un resultado normal. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `dummy`

Clase: **consulta técnica**; autenticación: **no requiere ticket WSAA**. SOAPAction: ``.

**Request** (`sin elemento/payload`, raíz XML `dummy`)


**Response** (`PadronA5.DummyResponse`, raíz XML `dummyResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `return` | [`tns:dummyReturn`](#dummyreturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Los WSDL de Padrón declaran `SRValidationException` como fault SOAP de servicio; su estructura aparece en el catálogo XSD y los errores no se expresan como `Errors` dentro de un resultado normal. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `getPersonaList_v2`

Clase: **consulta**; autenticación: **WSAA `ws_sr_constancia_inscripcion`**. SOAPAction: ``.

**Request** (`PadronA5.GetPersonaListV2`, raíz XML `getPersonaList_v2`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`PadronA5.GetPersonaListV2Response`, raíz XML `getPersonaList_v2Response`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `personaListReturn` | [`tns:personaListReturn`](#personalistreturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Los WSDL de Padrón declaran `SRValidationException` como fault SOAP de servicio; su estructura aparece en el catálogo XSD y los errores no se expresan como `Errors` dentro de un resultado normal. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


## Catálogo de tipos complejos

Incluye todos los tipos complejos nombrados del XSD del servicio. Los elementos `base` señalan herencia; se conserva la cardinalidad declarada. `xs:documentation` se transcribe cuando existe.


### `SRValidationException`

El tipo no declara elementos hijo directos en el XSD; puede ser vacío o derivar su contenido de un tipo base, que se documenta por separado.

### `actividad`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `descripcionActividad` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idActividad` | `xs:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nomenclador` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `orden` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `periodo` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `caracterizacion`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `descripcionCaracterizacion` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaSolicitud` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idCaracterizacion` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `periodo` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `categoria`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `descripcionCategoria` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idCategoria` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idImpuesto` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `periodo` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `datosGenerales`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `apellido` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `caracterizacion` | [`tns:caracterizacion`](#caracterizacion) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |
| `dependencia` | [`tns:dependencia`](#dependencia) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioFiscal` | [`tns:domicilio`](#domicilio) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `esSucesion` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `estadoClave` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaContratoSocial` | `xs:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaFallecimiento` | `xs:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `mesCierre` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nombre` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `razonSocial` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoClave` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoPersona` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `datosMonotributo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `actividad` | [`tns:actividad`](#actividad) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |
| `actividadMonotributista` | [`tns:actividad`](#actividad) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `categoriaMonotributo` | [`tns:categoria`](#categoria) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `componenteDeSociedad` | [`tns:relacion`](#relacion) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |
| `impuesto` | [`tns:impuesto`](#impuesto) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `datosRegimenGeneral`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `actividad` | [`tns:actividad`](#actividad) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |
| `categoriaAutonomo` | [`tns:categoria`](#categoria) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `impuesto` | [`tns:impuesto`](#impuesto) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |
| `regimen` | [`tns:regimen`](#regimen) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `dependencia`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codPostal` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `descripcionDependencia` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `descripcionProvincia` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `direccion` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idDependencia` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idProvincia` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `localidad` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `domicilio`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codPostal` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datoAdicional` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `descripcionProvincia` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `direccion` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idProvincia` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `localidad` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoDatoAdicional` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoDomicilio` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `dummy`

El tipo no declara elementos hijo directos en el XSD; puede ser vacío o derivar su contenido de un tipo base, que se documenta por separado.

### `dummyResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `return` | [`tns:dummyReturn`](#dummyreturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `dummyReturn`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `appserver` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `authserver` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `dbserver` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `errorConstancia`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `apellido` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `error` | `xs:string` | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nombre` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `errorMonotributo`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `error` | `xs:string` | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |
| `mensaje` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `errorRegimenGeneral`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `error` | `xs:string` | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |
| `mensaje` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `getPersona`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `getPersonaList`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `getPersonaListResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `personaListReturn` | [`tns:personaListReturn`](#personalistreturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `getPersonaList_v2`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |

### `getPersonaList_v2Response`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `personaListReturn` | [`tns:personaListReturn`](#personalistreturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `getPersonaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `personaReturn` | [`tns:personaReturn`](#personareturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `getPersona_v2`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `getPersona_v2Response`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `personaReturn` | [`tns:personaReturn`](#personareturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `impuesto`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `descripcionImpuesto` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `estadoImpuesto` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idImpuesto` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `motivo` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `periodo` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `metadata`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `fechaHora` | `xs:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `servidor` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `persona`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `datosGenerales` | [`tns:datosGenerales`](#datosgenerales) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosMonotributo` | [`tns:datosMonotributo`](#datosmonotributo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosRegimenGeneral` | [`tns:datosRegimenGeneral`](#datosregimengeneral) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errorConstancia` | [`tns:errorConstancia`](#errorconstancia) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errorMonotributo` | [`tns:errorMonotributo`](#errormonotributo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errorRegimenGeneral` | [`tns:errorRegimenGeneral`](#errorregimengeneral) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `personaListReturn`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `metadata` | [`tns:metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `persona` | [`tns:persona`](#persona) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |

### `personaReturn`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `datosGenerales` | [`tns:datosGenerales`](#datosgenerales) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosMonotributo` | [`tns:datosMonotributo`](#datosmonotributo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosRegimenGeneral` | [`tns:datosRegimenGeneral`](#datosregimengeneral) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errorConstancia` | [`tns:errorConstancia`](#errorconstancia) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errorMonotributo` | [`tns:errorMonotributo`](#errormonotributo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errorRegimenGeneral` | [`tns:errorRegimenGeneral`](#errorregimengeneral) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `regimen`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `descripcionRegimen` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idImpuesto` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idRegimen` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `periodo` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoRegimen` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `relacion`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `apellidoPersonaAsociada` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `ffRelacion` | `xs:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `ffVencimiento` | `xs:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idPersonaAsociada` | `xs:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nombrePersonaAsociada` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `razonSocialPersonaAsociada` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoComponente` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
