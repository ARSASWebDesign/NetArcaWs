<!-- Source: docs/reference/operations/padrona10.md. Generated wiki mirror; edit the repository source. -->

# Padrón A10: consulta registral

**Rol:** Consulta individual del padrón A10.

**Funcionalidad:** Consultar datos de una persona y comprobar disponibilidad técnica.

**Uso:** Usarlo como lectura autenticada y tratar faults `SRValidationException`.

Contrato generado desde [`padron-a10-production.wsdl`](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/docs/reference/contracts/padron-a10-production.wsdl); namespace XML `http://a10.soap.ws.server.puc.sr/`. El servicio WSAA es `ws_sr_padron_a10`. Las operaciones autenticadas envían ticket WSAA; `dummy` es una comprobación no autenticada según el cliente generado.

Los tipos C# indicados proceden de los contratos generados y el WSDL fijado. `minOccurs`, `maxOccurs` y `nillable` describen estructura XML; **no sustituyen reglas de negocio** ni garantizan aceptación por ARCA. No se inventan validaciones de negocio.

## Operaciones disponibles

| Operación | Clase | Request C# | Response C# | SOAPAction |
|---|---|---|---|---|
| `getPersona` | requiere ticket WSAA | `PadronA10.GetPersona` | `PadronA10.GetPersonaResponse` | `` |
| `dummy` | consulta técnica, sin WSAA | sin DTO | `PadronA10.DummyResponse` | `` |

## Campos y tipos

Cada tabla lista los campos XML del elemento raíz de request/response. Las referencias a tipos nombrados se amplían en el catálogo de tipos complejos. Para respuestas, `Errors`/`Observaciones` (cuando el tipo los declara) transportan rechazos y avisos del servicio; las tablas de errores ARCA por código deben contrastarse con el manual vigente del servicio. Un SOAP Fault de transporte/protocolo se expone como `NetArcaWs.Transport.SoapFaultException` con `Code`, `Reason`, `Detail` y `StatusCode`. No reintentar automáticamente escrituras: ante timeout o resultado incierto, reconciliar el estado antes de cualquier nuevo envío.


### `getPersona`

Clase: **consulta**; autenticación: **WSAA `ws_sr_padron_a10`**. SOAPAction: ``.

**Request** (`PadronA10.GetPersona`, raíz XML `getPersona`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`PadronA10.GetPersonaResponse`, raíz XML `getPersonaResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `personaReturn` | [`tns:personaReturn`](#personareturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Los WSDL de Padrón declaran `SRValidationException` como fault SOAP de servicio; su estructura aparece en el catálogo XSD y los errores no se expresan como `Errors` dentro de un resultado normal. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `dummy`

Clase: **consulta técnica**; autenticación: **no requiere ticket WSAA**. SOAPAction: ``.

**Request** (`generado por cliente`, raíz XML `dummy`)


**Response** (`PadronA10.DummyResponse`, raíz XML `dummyResponse`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `return` | [`tns:dummyReturn`](#dummyreturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

Los WSDL de Padrón declaran `SRValidationException` como fault SOAP de servicio; su estructura aparece en el catálogo XSD y los errores no se expresan como `Errors` dentro de un resultado normal. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


## Catálogo de tipos complejos

Incluye todos los tipos complejos nombrados del XSD del servicio. Los elementos `base` señalan herencia; se conserva la cardinalidad declarada. `xs:documentation` se transcribe cuando existe.


### `SRValidationException`

El tipo no declara elementos hijo directos en el XSD; puede ser vacío o derivar su contenido de un tipo base, que se documenta por separado.

### `dependencia`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `descripcionDependencia` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idDependencia` | `xs:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

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

### `getPersona`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xs:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `getPersonaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `personaReturn` | [`tns:personaReturn`](#personareturn) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `metadata`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `fechaHora` | `xs:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `servidor` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `persona`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `apellido` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `claveInactivaAsociada` | `xs:long` | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |
| `dependencia` | [`tns:dependencia`](#dependencia) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `descripcionActividadPrincipal` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilio` | [`tns:domicilio`](#domicilio) | 0 | unbounded | true | No; el esquema no valida reglas de negocio |  |
| `estadoClave` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idActividadPrincipal` | `xs:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `idPersona` | `xs:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nombre` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `numeroDocumento` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `razonSocial` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoClave` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoDocumento` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoPersona` | `xs:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `personaReturn`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `metadata` | [`tns:metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `persona` | [`tns:persona`](#persona) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
