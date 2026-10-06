# WSCPE: Carta de Porte Electrónica

**Rol:** Gestiona documentos electrónicos de traslado de granos y derivados granarios.

**Funcionalidad:** Autorizar, consultar y actualizar cartas de porte automotor, ferroviarias y de ductos, gestionar contingencias, destinos y confirmaciones, y consultar catálogos.

**Uso:** Usarlo con un tenant autorizado para wscpe. Validar los datos según el manual vigente y registrar las intenciones de escritura en la aplicación; ante resultado incierto consultar el estado antes de reenviar. Ver la guía WSCPE para endpoints y límites de cobertura.

Contrato generado desde [`wscpe-production.wsdl`](../contracts/wscpe-production.wsdl); namespace XML `https://serviciosjava.afip.gob.ar/wscpe/`. El servicio WSAA es `wscpe`. Las operaciones autenticadas envían ticket WSAA; `dummy` es una comprobación no autenticada según el cliente generado.

Los tipos C# indicados proceden de los contratos generados y el WSDL fijado. `minOccurs`, `maxOccurs` y `nillable` describen estructura XML; **no sustituyen reglas de negocio** ni garantizan aceptación por ARCA. No se inventan validaciones de negocio.

## Operaciones disponibles

| Operación | Clase | Request C# | Response C# | SOAPAction |
|---|---|---|---|---|
| `dummy` | consulta técnica, sin WSAA | sin DTO | `Wscpe.DummyResponse` | `https://serviciosjava.afip.gob.ar/wscpe/dummy` |
| `consultarProvincias` | requiere ticket WSAA | `Wscpe.ConsultarProvinciasRequest` | `Wscpe.ConsultarProvinciasResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarProvincias` |
| `consultarCategoriasSemillas` | requiere ticket WSAA | `Wscpe.ConsultarCategoriasSemillasRequest` | `Wscpe.ConsultarCategoriasSemillasResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarCategoriasSemillas` |
| `consultarVariedadesSemillas` | requiere ticket WSAA | `Wscpe.ConsultarVariedadesSemillasRequest` | `Wscpe.ConsultarVariedadesSemillasResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarVariedadesSemillas` |
| `consultarCPEDGPendienteActivacion` | requiere ticket WSAA | `Wscpe.ConsultarCpedgPendienteActivacionRequest` | `Wscpe.ConsultarCpedgPendienteActivacionResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEDGPendienteActivacion` |
| `consultarCPEEmitidasDestinoDGPendientesActivacion` | requiere ticket WSAA | `Wscpe.ConsultarCpeEmitidasDestinoDgPendientesActivacionRequest` | `Wscpe.ConsultarCpeEmitidasDestinoDgPendientesActivacionResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEEmitidasDestinoDGPendientesActivacion` |
| `consultarTiposEmbalaje` | requiere ticket WSAA | `Wscpe.ConsultarTiposEmbalajeRequest` | `Wscpe.ConsultarTiposEmbalajeResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarTiposEmbalaje` |
| `consultarUnidadesMedida` | requiere ticket WSAA | `Wscpe.ConsultarUnidadesMedidaRequest` | `Wscpe.ConsultarUnidadesMedidaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarUnidadesMedida` |
| `consultarDerivadosGranarios` | requiere ticket WSAA | `Wscpe.ConsultarDerivadosGranariosRequest` | `Wscpe.ConsultarDerivadosGranariosResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarDerivadosGranarios` |
| `consultarLocalidadesPorProvincia` | requiere ticket WSAA | `Wscpe.ConsultarLocalidadesPorProvinciaRequest` | `Wscpe.ConsultarLocalidadesPorProvinciaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarLocalidadesPorProvincia` |
| `consultarDomiciliosPorCUIT` | requiere ticket WSAA | `Wscpe.ConsultarDomiciliosPorCuitRequest` | `Wscpe.ConsultarDomiciliosPorCuitResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarDomiciliosPorCUIT` |
| `consultarLocalidadesProductor` | requiere ticket WSAA | `Wscpe.ConsultarLocalidadesProductorRequest` | `Wscpe.ConsultarLocalidadesProductorResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarLocalidadesProductor` |
| `consultarTiposGrano` | requiere ticket WSAA | `Wscpe.ConsultarTiposGranoRequest` | `Wscpe.ConsultarTiposGranoResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarTiposGrano` |
| `consultarUltNroOrden` | requiere ticket WSAA | `Wscpe.ConsultarUltNroOrdenRequest` | `Wscpe.ConsultarUltNroOrdenResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarUltNroOrden` |
| `informarContingencia` | requiere ticket WSAA | `Wscpe.InformarContingenciaRequest` | `Wscpe.InformarContingenciaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/informarContingencia` |
| `informarContingenciaEmisionDestinoDG` | requiere ticket WSAA | `Wscpe.InformarContingenciaEmisionDestinoDgRequest` | `Wscpe.InformarContingenciaEmisionDestinoDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/informarContingenciaEmisionDestinoDG` |
| `anularCPE` | requiere ticket WSAA | `Wscpe.AnularCpeRequest` | `Wscpe.AnularCpeResponse` | `https://serviciosjava.afip.gob.ar/wscpe/anularCPE` |
| `anularCPEEmisionDestinoDG` | requiere ticket WSAA | `Wscpe.AnularCpeEmisionDestinoDgRequest` | `Wscpe.AnularCpeEmisionDestinoDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/anularCPEEmisionDestinoDG` |
| `confirmarArriboCPE` | requiere ticket WSAA | `Wscpe.ConfirmarArriboCpeRequest` | `Wscpe.ConfirmarArriboCpeResponse` | `https://serviciosjava.afip.gob.ar/wscpe/confirmarArriboCPE` |
| `rechazoCPE` | requiere ticket WSAA | `Wscpe.RechazoCpeRequest` | `Wscpe.RechazoCpeResponse` | `https://serviciosjava.afip.gob.ar/wscpe/rechazoCPE` |
| `descargadoDestinoCPE` | requiere ticket WSAA | `Wscpe.DescargadoDestinoCpeRequest` | `Wscpe.DescargadoDestinoCpeResponse` | `https://serviciosjava.afip.gob.ar/wscpe/descargadoDestinoCPE` |
| `descargadoDestinoCPEEmisionDestinoDG` | requiere ticket WSAA | `Wscpe.DescargadoDestinoCpeEmisionDestinoDgRequest` | `Wscpe.DescargadoDestinoCpeEmisionDestinoDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/descargadoDestinoCPEEmisionDestinoDG` |
| `aceptarEmisionDG` | requiere ticket WSAA | `Wscpe.AceptarEmisionDgRequest` | `Wscpe.AceptarEmisionDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/aceptarEmisionDG` |
| `rechazarEmisionDG` | requiere ticket WSAA | `Wscpe.RechazarEmisionDgRequest` | `Wscpe.RechazarEmisionDgResp` | `https://serviciosjava.afip.gob.ar/wscpe/rechazarEmisionDG` |
| `aceptarEmisionDestinoDG` | requiere ticket WSAA | `Wscpe.AceptarEmisionDestinoDgRequest` | `Wscpe.AceptarEmisionDestinoDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/aceptarEmisionDestinoDG` |
| `rechazarEmisionDestinoDG` | requiere ticket WSAA | `Wscpe.RechazarEmisionDestinoDgRequest` | `Wscpe.RechazarEmisionDestinoDgResp` | `https://serviciosjava.afip.gob.ar/wscpe/rechazarEmisionDestinoDG` |
| `consultarCPEPorDestino` | requiere ticket WSAA | `Wscpe.ConsultarCpePorDestinoRequest` | `Wscpe.ConsultarCpePorDestinoResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEPorDestino` |
| `consultarCPEPPendientesDeResolucion` | requiere ticket WSAA | `Wscpe.ConsultarCpepPendientesDeResolucionRequest` | `Wscpe.ConsultarCpepPendientesDeResolucionResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEPPendientesDeResolucion` |
| `cerrarContingenciaCPE` | requiere ticket WSAA | `Wscpe.CerrarContingenciaCpeRequest` | `Wscpe.CerrarContingenciaCpeResponse` | `https://serviciosjava.afip.gob.ar/wscpe/cerrarContingenciaCPE` |
| `cerrarContingenciaCPEEmisionDestinoDG` | requiere ticket WSAA | `Wscpe.CerrarContingenciaCpeEmisionDestinoDgRequest` | `Wscpe.CerrarContingenciaCpeEmisionDestinoDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/cerrarContingenciaCPEEmisionDestinoDG` |
| `consultarPlantas` | requiere ticket WSAA | `Wscpe.ConsultarPlantasRequest` | `Wscpe.ConsultarPlantasResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarPlantas` |
| `consultarPlantasDG` | requiere ticket WSAA | `Wscpe.ConsultarPlantasDgRequest` | `Wscpe.ConsultarPlantasDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarPlantasDG` |
| `autorizarCPEFerroviaria` | requiere ticket WSAA | `Wscpe.AutorizarCpeFerroviariaRequest` | `Wscpe.AutorizarCpeFerroviariaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/autorizarCPEFerroviaria` |
| `autorizarCPEFerroviariaDG` | requiere ticket WSAA | `Wscpe.AutorizarCpeFerroviariaDgRequest` | `Wscpe.AutorizarCpeFerroviariaDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/autorizarCPEFerroviariaDG` |
| `consultarCPEFerroviaria` | requiere ticket WSAA | `Wscpe.ConsultarCpeFerroviariaRequest` | `Wscpe.ConsultarCpeFerroviariaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEFerroviaria` |
| `consultarCPEFerroviariaDG` | requiere ticket WSAA | `Wscpe.ConsultarCpeFerroviariaDgRequest` | `Wscpe.ConsultarCpeFerroviariaDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEFerroviariaDG` |
| `consultarCPEAutomotorDG` | requiere ticket WSAA | `Wscpe.ConsultarCpeAutomotorDgRequest` | `Wscpe.ConsultarCpeAutomotorDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEAutomotorDG` |
| `consultarCPEEmisionDestinoDG` | requiere ticket WSAA | `Wscpe.ConsultarCpeEmisionDestinoDgRequest` | `Wscpe.ConsultarCpeEmisionDestinoDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEEmisionDestinoDG` |
| `consultarCPEDuctos` | requiere ticket WSAA | `Wscpe.ConsultarCpeDuctosRequest` | `Wscpe.ConsultarCpeDuctosResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEDuctos` |
| `nuevoDestinoDestinatarioCPEFerroviaria` | requiere ticket WSAA | `Wscpe.NuevoDestinoDestinatarioCpeFerroviariaRequest` | `Wscpe.NuevoDestinoDestinatarioCpeFerroviariaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/nuevoDestinoDestinatarioCPEFerroviaria` |
| `nuevoDestinoDestinatarioCPEFerroviariaDG` | requiere ticket WSAA | `Wscpe.NuevoDestinoDestinatarioCpeFerroviariaDgRequest` | `Wscpe.NuevoDestinoDestinatarioCpeFerroviariaDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/nuevoDestinoDestinatarioCPEFerroviariaDG` |
| `nuevoDestinoDestinatarioCPEAutomotorDG` | requiere ticket WSAA | `Wscpe.NuevoDestinoDestinatarioCpeAutomotorDgRequest` | `Wscpe.NuevoDestinoDestinatarioCpeAutomotorDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/nuevoDestinoDestinatarioCPEAutomotorDG` |
| `nuevoDestinoDestinatarioCPEEmisionDestinoDG` | requiere ticket WSAA | `Wscpe.NuevoDestinoDestinatarioCpeEmisionDestinoDgRequest` | `Wscpe.NuevoDestinoDestinatarioCpeEmisionDestinoDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/nuevoDestinoDestinatarioCPEEmisionDestinoDG` |
| `regresoOrigenCPEFerroviaria` | requiere ticket WSAA | `Wscpe.RegresoOrigenCpeFerroviariaRequest` | `Wscpe.RegresoOrigenCpeFerroviariaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/regresoOrigenCPEFerroviaria` |
| `desvioCPEFerroviaria` | requiere ticket WSAA | `Wscpe.DesvioCpeFerroviariaRequest` | `Wscpe.DesvioCpeFerroviariaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/desvioCPEFerroviaria` |
| `desvioCPEFerroviariaDG` | requiere ticket WSAA | `Wscpe.DesvioCpeFerroviariaDgRequest` | `Wscpe.DesvioCpeFerroviariaDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/desvioCPEFerroviariaDG` |
| `desvioCPEAutomotorDG` | requiere ticket WSAA | `Wscpe.DesvioCpeAutomotorDgRequest` | `Wscpe.DesvioCpeAutomotorDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/desvioCPEAutomotorDG` |
| `confirmacionDefinitivaCPEFerroviaria` | requiere ticket WSAA | `Wscpe.ConfirmacionDefinitivaCpeFerroviariaRequest` | `Wscpe.ConfirmacionDefinitivaCpeFerroviariaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/confirmacionDefinitivaCPEFerroviaria` |
| `confirmacionDefinitivaCPEFerroviariaDG` | requiere ticket WSAA | `Wscpe.ConfirmacionDefinitivaCpeFerroviariaDgRequest` | `Wscpe.ConfirmacionDefinitivaCpeFerroviariaDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/confirmacionDefinitivaCPEFerroviariaDG` |
| `confirmacionDefinitivaCPEAutomotorDG` | requiere ticket WSAA | `Wscpe.ConfirmacionDefinitivaCpeAutomotorDgRequest` | `Wscpe.ConfirmacionDefinitivaCpeAutomotorDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/confirmacionDefinitivaCPEAutomotorDG` |
| `confirmacionDefinitivaCPEDuctosDG` | requiere ticket WSAA | `Wscpe.ConfirmacionDefinitivaCpeDuctosDgRequest` | `Wscpe.ConfirmacionDefinitivaCpeDuctosDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/confirmacionDefinitivaCPEDuctosDG` |
| `consultaCPEFerroviariaPorNroOperativo` | requiere ticket WSAA | `Wscpe.ConsultaCpeFerroviariaPorNroOperativoRequest` | `Wscpe.ConsultaCpeFerroviariaPorNroOperativoResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultaCPEFerroviariaPorNroOperativo` |
| `editarCPEFerroviaria` | requiere ticket WSAA | `Wscpe.EditarCpeFerroviariaRequest` | `Wscpe.EditarCpeFerroviariaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/editarCPEFerroviaria` |
| `editarCPEDGFerroviaria` | requiere ticket WSAA | `Wscpe.EditarCpedgFerroviariaRequest` | `Wscpe.EditarCpedgFerroviariaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/editarCPEDGFerroviaria` |
| `editarCPEDGAutomotor` | requiere ticket WSAA | `Wscpe.EditarCpedgAutomotorRequest` | `Wscpe.EditarCpedgAutomotorResponse` | `https://serviciosjava.afip.gob.ar/wscpe/editarCPEDGAutomotor` |
| `editarCPEDGDuctos` | requiere ticket WSAA | `Wscpe.EditarCpedgDuctosRequest` | `Wscpe.EditarCpedgDuctosResponse` | `https://serviciosjava.afip.gob.ar/wscpe/editarCPEDGDuctos` |
| `editarCPEConfirmadaFerroviaria` | requiere ticket WSAA | `Wscpe.EditarCpeConfirmadaFerroviariaRequest` | `Wscpe.EditarCpeConfirmadaFerroviariaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/editarCPEConfirmadaFerroviaria` |
| `autorizarCPEAutomotor` | requiere ticket WSAA | `Wscpe.AutorizarCpeAutomotorRequest` | `Wscpe.AutorizarCpeAutomotorResponse` | `https://serviciosjava.afip.gob.ar/wscpe/autorizarCPEAutomotor` |
| `autorizarCPEAutomotorDG` | requiere ticket WSAA | `Wscpe.AutorizarCpeAutomotorDgRequest` | `Wscpe.AutorizarCpeAutomotorDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/autorizarCPEAutomotorDG` |
| `autorizarCPEDuctosDG` | requiere ticket WSAA | `Wscpe.AutorizarCpeDuctosDgRequest` | `Wscpe.AutorizarCpeDuctosDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/autorizarCPEDuctosDG` |
| `autorizarCPEEmisionDestinoDG` | requiere ticket WSAA | `Wscpe.AutorizarCpeEmisionDestinoDgRequest` | `Wscpe.AutorizarCpeEmisionDestinoDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/autorizarCPEEmisionDestinoDG` |
| `consultarCPEAutomotor` | requiere ticket WSAA | `Wscpe.ConsultarCpeAutomotorRequest` | `Wscpe.ConsultarCpeAutomotorResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEAutomotor` |
| `nuevoDestinoDestinatarioCPEAutomotor` | requiere ticket WSAA | `Wscpe.NuevoDestinoDestinatarioCpeAutomotorRequest` | `Wscpe.NuevoDestinoDestinatarioCpeAutomotorResponse` | `https://serviciosjava.afip.gob.ar/wscpe/nuevoDestinoDestinatarioCPEAutomotor` |
| `regresoOrigenCPEAutomotor` | requiere ticket WSAA | `Wscpe.RegresoOrigenCpeAutomotorRequest` | `Wscpe.RegresoOrigenCpeAutomotorResponse` | `https://serviciosjava.afip.gob.ar/wscpe/regresoOrigenCPEAutomotor` |
| `regresoOrigenCPEAutomotorDG` | requiere ticket WSAA | `Wscpe.RegresoOrigenCpeAutomotorDgRequest` | `Wscpe.RegresoOrigenCpeAutomotorDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/regresoOrigenCPEAutomotorDG` |
| `regresoOrigenCPEFerroviariaDG` | requiere ticket WSAA | `Wscpe.RegresoOrigenCpeFerroviariaDgRequest` | `Wscpe.RegresoOrigenCpeFerroviariaDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/regresoOrigenCPEFerroviariaDG` |
| `regresoOrigenCPEEmisionDestinoDG` | requiere ticket WSAA | `Wscpe.RegresoOrigenCpeEmisionDestinoDgRequest` | `Wscpe.RegresoOrigenCpeEmisionDestinoDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/regresoOrigenCPEEmisionDestinoDG` |
| `desvioCPEAutomotor` | requiere ticket WSAA | `Wscpe.DesvioCpeAutomotorRequest` | `Wscpe.DesvioCpeAutomotorResponse` | `https://serviciosjava.afip.gob.ar/wscpe/desvioCPEAutomotor` |
| `confirmacionDefinitivaCPEAutomotor` | requiere ticket WSAA | `Wscpe.ConfirmacionDefinitivaCpeAutomotorRequest` | `Wscpe.ConfirmacionDefinitivaCpeAutomotorResponse` | `https://serviciosjava.afip.gob.ar/wscpe/confirmacionDefinitivaCPEAutomotor` |
| `editarCPEAutomotor` | requiere ticket WSAA | `Wscpe.EditarCpeAutomotorRequest` | `Wscpe.EditarCpeAutomotorResponse` | `https://serviciosjava.afip.gob.ar/wscpe/editarCPEAutomotor` |
| `editarCPEConfirmadaAutomotor` | requiere ticket WSAA | `Wscpe.EditarCpeConfirmadaAutomotorRequest` | `Wscpe.EditarCpeConfirmadaAutomotorResponse` | `https://serviciosjava.afip.gob.ar/wscpe/editarCPEConfirmadaAutomotor` |
| `editarCPEDGConfirmadaAutomotor` | requiere ticket WSAA | `Wscpe.EditarCpeConfirmadaAutomotorDgRequest` | `Wscpe.EditarCpeConfirmadaAutomotorDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/editarCPEDGConfirmadaAutomotor` |
| `editarCPEDGConfirmadaFerroviaria` | requiere ticket WSAA | `Wscpe.EditarCpeConfirmadaFerroviariaDgRequest` | `Wscpe.EditarCpeConfirmadaFerroviariaDgResponse` | `https://serviciosjava.afip.gob.ar/wscpe/editarCPEDGConfirmadaFerroviaria` |
| `editarCPEConfirmadaDuctos` | requiere ticket WSAA | `Wscpe.EditarCpeConfirmadaDuctosRequest` | `Wscpe.EditarCpeConfirmadaDuctosResponse` | `https://serviciosjava.afip.gob.ar/wscpe/editarCPEConfirmadaDuctos` |
| `consultarRenspa` | requiere ticket WSAA | `Wscpe.ConsultarRenspaRequest` | `Wscpe.ConsultarRenspaResponse` | `https://serviciosjava.afip.gob.ar/wscpe/consultarRenspa` |

## Campos y tipos

Cada tabla lista los campos XML del elemento raíz de request/response. Las referencias a tipos nombrados se amplían en el catálogo de tipos complejos. Para respuestas, `Errors`/`Observaciones` (cuando el tipo los declara) transportan rechazos y avisos del servicio; las tablas de errores ARCA por código deben contrastarse con el manual vigente del servicio. Un SOAP Fault de transporte/protocolo se expone como `NetArcaWs.Transport.SoapFaultException` con `Code`, `Reason`, `Detail` y `StatusCode`. No reintentar automáticamente escrituras: ante timeout o resultado incierto, reconciliar el estado antes de cualquier nuevo envío.


### `dummy`

Clase: **consulta técnica**; autenticación: **no requiere ticket WSAA**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/dummy`.

**Request** (`cuerpo SOAP vacío`, raíz XML `(vacío)`)


**Response** (`Wscpe.DummyResponse`, raíz XML `DummyResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DummyRespuesta`](#dummyrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarProvincias`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarProvincias`.

**Request** (`Wscpe.ConsultarProvinciasRequest`, raíz XML `ConsultarProvinciasReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarProvinciasResponse`, raíz XML `ConsultarProvinciasResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarProvinciasRespuesta`](#consultarprovinciasrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCategoriasSemillas`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarCategoriasSemillas`.

**Request** (`Wscpe.ConsultarCategoriasSemillasRequest`, raíz XML `ConsultarCategoriasSemillasReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarCategoriasSemillasResponse`, raíz XML `ConsultarCategoriasSemillasResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarCategoriasSemillasRespuesta`](#consultarcategoriassemillasrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarVariedadesSemillas`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarVariedadesSemillas`.

**Request** (`Wscpe.ConsultarVariedadesSemillasRequest`, raíz XML `ConsultarVariedadesSemillasReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarVariedadesSemillasResponse`, raíz XML `ConsultarVariedadesSemillasResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarVariedadesSemillasRespuesta`](#consultarvariedadessemillasrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCPEDGPendienteActivacion`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEDGPendienteActivacion`.

**Request** (`Wscpe.ConsultarCpedgPendienteActivacionRequest`, raíz XML `ConsultarCPEDGPendienteActivacionReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarCPEDGPendienteActivacionSolicitud`](#consultarcpedgpendienteactivacionsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarCpedgPendienteActivacionResponse`, raíz XML `ConsultarCPEDGPendienteActivacionResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarCPEDGPendienteActivacionRespuesta`](#consultarcpedgpendienteactivacionrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCPEEmitidasDestinoDGPendientesActivacion`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEEmitidasDestinoDGPendientesActivacion`.

**Request** (`Wscpe.ConsultarCpeEmitidasDestinoDgPendientesActivacionRequest`, raíz XML `ConsultarCPEEmitidasDestinoDGPendientesActivacionReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarCPEEmitidasDestinoDGPendientesActivacionSolicitud`](#consultarcpeemitidasdestinodgpendientesactivacionsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarCpeEmitidasDestinoDgPendientesActivacionResponse`, raíz XML `ConsultarCPEEmitidasDestinoDGPendientesActivacionResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarCPEEmitidasDestinoDGPendientesActivacionRespuesta`](#consultarcpeemitidasdestinodgpendientesactivacionrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarTiposEmbalaje`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarTiposEmbalaje`.

**Request** (`Wscpe.ConsultarTiposEmbalajeRequest`, raíz XML `ConsultarTiposEmbalajeReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarTiposEmbalajeResponse`, raíz XML `ConsultarTiposEmbalajeResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarTiposEmbalajeRespuesta`](#consultartiposembalajerespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarUnidadesMedida`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarUnidadesMedida`.

**Request** (`Wscpe.ConsultarUnidadesMedidaRequest`, raíz XML `ConsultarUnidadesMedidaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarUnidadesMedidaResponse`, raíz XML `ConsultarUnidadesMedidaResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarUnidadesMedidaRespuesta`](#consultarunidadesmedidarespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarDerivadosGranarios`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarDerivadosGranarios`.

**Request** (`Wscpe.ConsultarDerivadosGranariosRequest`, raíz XML `ConsultarDerivadosGranariosReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarDerivadosGranariosResponse`, raíz XML `ConsultarDerivadosGranariosResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarDerivadosGranariosRespuesta`](#consultarderivadosgranariosrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarLocalidadesPorProvincia`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarLocalidadesPorProvincia`.

**Request** (`Wscpe.ConsultarLocalidadesPorProvinciaRequest`, raíz XML `ConsultarLocalidadesPorProvinciaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarLocalidadesPorProvinciaSolicitud`](#consultarlocalidadesporprovinciasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarLocalidadesPorProvinciaResponse`, raíz XML `ConsultarLocalidadesPorProvinciaResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarLocalidadesPorProvinciaRespuesta`](#consultarlocalidadesporprovinciarespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarDomiciliosPorCUIT`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarDomiciliosPorCUIT`.

**Request** (`Wscpe.ConsultarDomiciliosPorCuitRequest`, raíz XML `ConsultarDomiciliosPorCUITReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarDomiciliosPorCuitResponse`, raíz XML `ConsultarDomiciliosPorCUITResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarDomiciliosPorCUITRespuesta`](#consultardomiciliosporcuitrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarLocalidadesProductor`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarLocalidadesProductor`.

**Request** (`Wscpe.ConsultarLocalidadesProductorRequest`, raíz XML `ConsultarLocalidadesProductorReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarLocalidadesProductorSolicitud`](#consultarlocalidadesproductorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarLocalidadesProductorResponse`, raíz XML `ConsultarLocalidadesProductorResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarLocalidadesProductorRespuesta`](#consultarlocalidadesproductorrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarTiposGrano`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarTiposGrano`.

**Request** (`Wscpe.ConsultarTiposGranoRequest`, raíz XML `ConsultarTiposGranoReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarTiposGranoResponse`, raíz XML `ConsultarTiposGranoResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarTiposGranoRespuesta`](#consultartiposgranorespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarUltNroOrden`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarUltNroOrden`.

**Request** (`Wscpe.ConsultarUltNroOrdenRequest`, raíz XML `ConsultarUltNroOrdenReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarUltNroOrdenSolicitud`](#consultarultnroordensolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarUltNroOrdenResponse`, raíz XML `ConsultarUltNroOrdenResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarUltNroOrdenRespuesta`](#consultarultnroordenrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `informarContingencia`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/informarContingencia`.

**Request** (`Wscpe.InformarContingenciaRequest`, raíz XML `InformarContingenciaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:InformarContingenciaSolicitud`](#informarcontingenciasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.InformarContingenciaResponse`, raíz XML `InformarContingenciaResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `informarContingenciaEmisionDestinoDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/informarContingenciaEmisionDestinoDG`.

**Request** (`Wscpe.InformarContingenciaEmisionDestinoDgRequest`, raíz XML `InformarContingenciaEmisionDestinoDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:InformarContingenciaEmisionDestinoDGSolicitud`](#informarcontingenciaemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.InformarContingenciaEmisionDestinoDgResponse`, raíz XML `InformarContingenciaEmisionDestinoDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `anularCPE`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/anularCPE`.

**Request** (`Wscpe.AnularCpeRequest`, raíz XML `AnularCPEReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AnularCPESolicitud`](#anularcpesolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.AnularCpeResponse`, raíz XML `AnularCPEResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `anularCPEEmisionDestinoDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/anularCPEEmisionDestinoDG`.

**Request** (`Wscpe.AnularCpeEmisionDestinoDgRequest`, raíz XML `AnularCPEEmisionDestinoDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AnularCPEEmisionDestinoDGSolicitud`](#anularcpeemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.AnularCpeEmisionDestinoDgResponse`, raíz XML `AnularCPEEmisionDestinoDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `confirmarArriboCPE`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/confirmarArriboCPE`.

**Request** (`Wscpe.ConfirmarArriboCpeRequest`, raíz XML `ConfirmarArriboCPEReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConfirmarArriboSolicitud`](#confirmararribosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConfirmarArriboCpeResponse`, raíz XML `ConfirmarArriboCPEResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `rechazoCPE`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/rechazoCPE`.

**Request** (`Wscpe.RechazoCpeRequest`, raíz XML `RechazoCPEReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RechazoCPESolicitud`](#rechazocpesolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.RechazoCpeResponse`, raíz XML `RechazoCPEResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `descargadoDestinoCPE`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/descargadoDestinoCPE`.

**Request** (`Wscpe.DescargadoDestinoCpeRequest`, raíz XML `DescargadoDestinoCPEReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:DescargadoDestinoCPESolicitud`](#descargadodestinocpesolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.DescargadoDestinoCpeResponse`, raíz XML `DescargadoDestinoCPEResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `descargadoDestinoCPEEmisionDestinoDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/descargadoDestinoCPEEmisionDestinoDG`.

**Request** (`Wscpe.DescargadoDestinoCpeEmisionDestinoDgRequest`, raíz XML `DescargadoDestinoCPEEmisionDestinoDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:DescargadoDestinoCPEEmisionDestinoDGSolicitud`](#descargadodestinocpeemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.DescargadoDestinoCpeEmisionDestinoDgResponse`, raíz XML `DescargadoDestinoCPEEmisionDestinoDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `aceptarEmisionDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/aceptarEmisionDG`.

**Request** (`Wscpe.AceptarEmisionDgRequest`, raíz XML `AceptarEmisionDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AceptarEmisionDGSolicitud`](#aceptaremisiondgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.AceptarEmisionDgResponse`, raíz XML `AceptarEmisionDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `rechazarEmisionDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/rechazarEmisionDG`.

**Request** (`Wscpe.RechazarEmisionDgRequest`, raíz XML `RechazarEmisionDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RechazarEmisionDGSolicitud`](#rechazaremisiondgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.RechazarEmisionDgResp`, raíz XML `RechazarEmisionDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `aceptarEmisionDestinoDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/aceptarEmisionDestinoDG`.

**Request** (`Wscpe.AceptarEmisionDestinoDgRequest`, raíz XML `AceptarEmisionDestinoDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AceptarEmisionDestinoDGSolicitud`](#aceptaremisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.AceptarEmisionDestinoDgResponse`, raíz XML `AceptarEmisionDestinoDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `rechazarEmisionDestinoDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/rechazarEmisionDestinoDG`.

**Request** (`Wscpe.RechazarEmisionDestinoDgRequest`, raíz XML `RechazarEmisionDestinoDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RechazarEmisionDestinoDGSolicitud`](#rechazaremisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.RechazarEmisionDestinoDgResp`, raíz XML `RechazarEmisionDestinoDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCPEPorDestino`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEPorDestino`.

**Request** (`Wscpe.ConsultarCpePorDestinoRequest`, raíz XML `ConsultarCPEPorDestinoReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarCPEPorDestinoSolicitud`](#consultarcpepordestinosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarCpePorDestinoResponse`, raíz XML `ConsultarCPEPorDestinoResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarCPEPorDestinoRespuesta`](#consultarcpepordestinorespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCPEPPendientesDeResolucion`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEPPendientesDeResolucion`.

**Request** (`Wscpe.ConsultarCpepPendientesDeResolucionRequest`, raíz XML `ConsultarCPEPPendientesDeResolucionReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarCPEPPendientesDeResolucionSolicitud`](#consultarcpeppendientesderesolucionsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarCpepPendientesDeResolucionResponse`, raíz XML `ConsultarCPEPPendientesDeResolucionResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarCPEPPendientesDeResolucionRespuesta`](#consultarcpeppendientesderesolucionrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `cerrarContingenciaCPE`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/cerrarContingenciaCPE`.

**Request** (`Wscpe.CerrarContingenciaCpeRequest`, raíz XML `CerrarContingenciaCPEReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:CerrarContingenciaFerroviariaSolicitud`](#cerrarcontingenciaferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.CerrarContingenciaCpeResponse`, raíz XML `CerrarContingenciaCPEResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `cerrarContingenciaCPEEmisionDestinoDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/cerrarContingenciaCPEEmisionDestinoDG`.

**Request** (`Wscpe.CerrarContingenciaCpeEmisionDestinoDgRequest`, raíz XML `CerrarContingenciaCPEEmisionDestinoDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:CerrarContingenciaEmisionDestinoDGSolicitud`](#cerrarcontingenciaemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.CerrarContingenciaCpeEmisionDestinoDgResponse`, raíz XML `CerrarContingenciaCPEEmisionDestinoDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarPlantas`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarPlantas`.

**Request** (`Wscpe.ConsultarPlantasRequest`, raíz XML `ConsultarPlantasReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarPlantasSolicitud`](#consultarplantassolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarPlantasResponse`, raíz XML `ConsultarPlantasResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarPlantasRespuesta`](#consultarplantasrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarPlantasDG`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarPlantasDG`.

**Request** (`Wscpe.ConsultarPlantasDgRequest`, raíz XML `ConsultarPlantasDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarPlantasDGSolicitud`](#consultarplantasdgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarPlantasDgResponse`, raíz XML `ConsultarPlantasDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarPlantasDGRespuesta`](#consultarplantasdgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `autorizarCPEFerroviaria`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/autorizarCPEFerroviaria`.

**Request** (`Wscpe.AutorizarCpeFerroviariaRequest`, raíz XML `AutorizarCPEFerroviariaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AutorizarFerroviariaSolicitud`](#autorizarferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.AutorizarCpeFerroviariaResponse`, raíz XML `AutorizarCPEFerroviariaResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleFerroviariaRespuesta`](#detalleferroviariarespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `autorizarCPEFerroviariaDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/autorizarCPEFerroviariaDG`.

**Request** (`Wscpe.AutorizarCpeFerroviariaDgRequest`, raíz XML `AutorizarCPEFerroviariaDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AutorizarFerroviariaDGSolicitud`](#autorizarferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.AutorizarCpeFerroviariaDgResponse`, raíz XML `AutorizarCPEFerroviariaDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleFerroviariaDGRespuesta`](#detalleferroviariadgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCPEFerroviaria`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEFerroviaria`.

**Request** (`Wscpe.ConsultarCpeFerroviariaRequest`, raíz XML `ConsultarCPEFerroviariaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarFerroviariaSolicitud`](#consultarferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarCpeFerroviariaResponse`, raíz XML `ConsultarCPEFerroviariaResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleFerroviariaRespuesta`](#detalleferroviariarespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCPEFerroviariaDG`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEFerroviariaDG`.

**Request** (`Wscpe.ConsultarCpeFerroviariaDgRequest`, raíz XML `ConsultarCPEFerroviariaDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarFerroviariaDGSolicitud`](#consultarferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarCpeFerroviariaDgResponse`, raíz XML `ConsultarCPEFerroviariaDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleFerroviariaDGRespuesta`](#detalleferroviariadgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCPEAutomotorDG`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEAutomotorDG`.

**Request** (`Wscpe.ConsultarCpeAutomotorDgRequest`, raíz XML `ConsultarCPEAutomotorDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarAutomotorDGSolicitud`](#consultarautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarCpeAutomotorDgResponse`, raíz XML `ConsultarCPEAutomotorDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleAutomotorDGRespuesta`](#detalleautomotordgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCPEEmisionDestinoDG`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEEmisionDestinoDG`.

**Request** (`Wscpe.ConsultarCpeEmisionDestinoDgRequest`, raíz XML `ConsultarCPEEmisionDestinoDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarEmisionDestinoDGSolicitud`](#consultaremisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarCpeEmisionDestinoDgResponse`, raíz XML `ConsultarCPEEmisionDestinoDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleEmisionDestinoDGRespuesta`](#detalleemisiondestinodgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCPEDuctos`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEDuctos`.

**Request** (`Wscpe.ConsultarCpeDuctosRequest`, raíz XML `ConsultarCPEDuctosReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarDuctosSolicitud`](#consultarductossolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarCpeDuctosResponse`, raíz XML `ConsultarCPEDuctosResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleDuctosDGRespuesta`](#detalleductosdgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `nuevoDestinoDestinatarioCPEFerroviaria`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/nuevoDestinoDestinatarioCPEFerroviaria`.

**Request** (`Wscpe.NuevoDestinoDestinatarioCpeFerroviariaRequest`, raíz XML `NuevoDestinoDestinatarioCPEFerroviariaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:NuevoDestinoDestinatarioFerroviariaSolicitud`](#nuevodestinodestinatarioferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.NuevoDestinoDestinatarioCpeFerroviariaResponse`, raíz XML `NuevoDestinoDestinatarioCPEFerroviariaResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `nuevoDestinoDestinatarioCPEFerroviariaDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/nuevoDestinoDestinatarioCPEFerroviariaDG`.

**Request** (`Wscpe.NuevoDestinoDestinatarioCpeFerroviariaDgRequest`, raíz XML `NuevoDestinoDestinatarioCPEFerroviariaDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:NuevoDestinoDestinatarioFerroviariaDGSolicitud`](#nuevodestinodestinatarioferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.NuevoDestinoDestinatarioCpeFerroviariaDgResponse`, raíz XML `NuevoDestinoDestinatarioCPEFerroviariaDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `nuevoDestinoDestinatarioCPEAutomotorDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/nuevoDestinoDestinatarioCPEAutomotorDG`.

**Request** (`Wscpe.NuevoDestinoDestinatarioCpeAutomotorDgRequest`, raíz XML `NuevoDestinoDestinatarioCPEAutomotorDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:NuevoDestinoDestinatarioAutomotorDGSolicitud`](#nuevodestinodestinatarioautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.NuevoDestinoDestinatarioCpeAutomotorDgResponse`, raíz XML `NuevoDestinoDestinatarioCPEAutomotorDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `nuevoDestinoDestinatarioCPEEmisionDestinoDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/nuevoDestinoDestinatarioCPEEmisionDestinoDG`.

**Request** (`Wscpe.NuevoDestinoDestinatarioCpeEmisionDestinoDgRequest`, raíz XML `NuevoDestinoDestinatarioCPEEmisionDestinoDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:NuevoDestinoDestinatarioEmisionDestinoDGSolicitud`](#nuevodestinodestinatarioemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.NuevoDestinoDestinatarioCpeEmisionDestinoDgResponse`, raíz XML `NuevoDestinoDestinatarioCPEEmisionDestinoDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `regresoOrigenCPEFerroviaria`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/regresoOrigenCPEFerroviaria`.

**Request** (`Wscpe.RegresoOrigenCpeFerroviariaRequest`, raíz XML `RegresoOrigenCPEFerroviariaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RegresoOrigenFerroviariaSolicitud`](#regresoorigenferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.RegresoOrigenCpeFerroviariaResponse`, raíz XML `RegresoOrigenCPEFerroviariaResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `desvioCPEFerroviaria`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/desvioCPEFerroviaria`.

**Request** (`Wscpe.DesvioCpeFerroviariaRequest`, raíz XML `DesvioCPEFerroviariaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:DesvioFerroviariaSolicitud`](#desvioferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.DesvioCpeFerroviariaResponse`, raíz XML `DesvioCPEFerroviariaResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `desvioCPEFerroviariaDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/desvioCPEFerroviariaDG`.

**Request** (`Wscpe.DesvioCpeFerroviariaDgRequest`, raíz XML `DesvioCPEFerroviariaDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:DesvioFerroviariaDGSolicitud`](#desvioferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.DesvioCpeFerroviariaDgResponse`, raíz XML `DesvioCPEFerroviariaDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `desvioCPEAutomotorDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/desvioCPEAutomotorDG`.

**Request** (`Wscpe.DesvioCpeAutomotorDgRequest`, raíz XML `DesvioCPEAutomotorDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:DesvioAutomotorDGSolicitud`](#desvioautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.DesvioCpeAutomotorDgResponse`, raíz XML `DesvioCPEAutomotorDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `confirmacionDefinitivaCPEFerroviaria`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/confirmacionDefinitivaCPEFerroviaria`.

**Request** (`Wscpe.ConfirmacionDefinitivaCpeFerroviariaRequest`, raíz XML `ConfirmacionDefinitivaCPEFerroviariaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConfirmacionFerroviariaSolicitud`](#confirmacionferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConfirmacionDefinitivaCpeFerroviariaResponse`, raíz XML `ConfirmacionDefinitivaCPEFerroviariaResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `confirmacionDefinitivaCPEFerroviariaDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/confirmacionDefinitivaCPEFerroviariaDG`.

**Request** (`Wscpe.ConfirmacionDefinitivaCpeFerroviariaDgRequest`, raíz XML `ConfirmacionDefinitivaCPEFerroviariaDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConfirmacionFerroviariaDGSolicitud`](#confirmacionferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConfirmacionDefinitivaCpeFerroviariaDgResponse`, raíz XML `ConfirmacionDefinitivaCPEFerroviariaDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `confirmacionDefinitivaCPEAutomotorDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/confirmacionDefinitivaCPEAutomotorDG`.

**Request** (`Wscpe.ConfirmacionDefinitivaCpeAutomotorDgRequest`, raíz XML `ConfirmacionDefinitivaCPEAutomotorDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConfirmacionAutomotorDGSolicitud`](#confirmacionautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConfirmacionDefinitivaCpeAutomotorDgResponse`, raíz XML `ConfirmacionDefinitivaCPEAutomotorDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `confirmacionDefinitivaCPEDuctosDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/confirmacionDefinitivaCPEDuctosDG`.

**Request** (`Wscpe.ConfirmacionDefinitivaCpeDuctosDgRequest`, raíz XML `ConfirmacionDefinitivaCPEDuctosDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConfirmacionDuctosDGSolicitud`](#confirmacionductosdgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConfirmacionDefinitivaCpeDuctosDgResponse`, raíz XML `ConfirmacionDefinitivaCPEDuctosDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultaCPEFerroviariaPorNroOperativo`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultaCPEFerroviariaPorNroOperativo`.

**Request** (`Wscpe.ConsultaCpeFerroviariaPorNroOperativoRequest`, raíz XML `ConsultaCPEFerroviariaPorNroOperativoReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultaCPEFerroviariaPorNroOperativoSolicitud`](#consultacpeferroviariapornrooperativosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultaCpeFerroviariaPorNroOperativoResponse`, raíz XML `ConsultaCPEFerroviariaPorNroOperativoResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteFerroviariaResumenRespuesta`](#cartaporteferroviariaresumenrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `editarCPEFerroviaria`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/editarCPEFerroviaria`.

**Request** (`Wscpe.EditarCpeFerroviariaRequest`, raíz XML `EditarCPEFerroviariaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarActivaFerroviariaSolicitud`](#editaractivaferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.EditarCpeFerroviariaResponse`, raíz XML `EditarCPEFerroviariaResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `editarCPEDGFerroviaria`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/editarCPEDGFerroviaria`.

**Request** (`Wscpe.EditarCpedgFerroviariaRequest`, raíz XML `EditarCPEDGFerroviariaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarActivaDGFerroviariaSolicitud`](#editaractivadgferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.EditarCpedgFerroviariaResponse`, raíz XML `EditarCPEDGFerroviariaResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `editarCPEDGAutomotor`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/editarCPEDGAutomotor`.

**Request** (`Wscpe.EditarCpedgAutomotorRequest`, raíz XML `EditarCPEDGAutomotorReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarActivaDGAutomotorSolicitud`](#editaractivadgautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.EditarCpedgAutomotorResponse`, raíz XML `EditarCPEDGAutomotorResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `editarCPEDGDuctos`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/editarCPEDGDuctos`.

**Request** (`Wscpe.EditarCpedgDuctosRequest`, raíz XML `EditarCPEDGDuctosReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarActivaDGDuctosSolicitud`](#editaractivadgductossolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.EditarCpedgDuctosResponse`, raíz XML `EditarCPEDGDuctosResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `editarCPEConfirmadaFerroviaria`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/editarCPEConfirmadaFerroviaria`.

**Request** (`Wscpe.EditarCpeConfirmadaFerroviariaRequest`, raíz XML `EditarCPEConfirmadaFerroviariaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarCPEConfirmadaFerroviariaSolicitud`](#editarcpeconfirmadaferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.EditarCpeConfirmadaFerroviariaResponse`, raíz XML `EditarCPEConfirmadaFerroviariaResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `autorizarCPEAutomotor`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/autorizarCPEAutomotor`.

**Request** (`Wscpe.AutorizarCpeAutomotorRequest`, raíz XML `AutorizarCPEAutomotorReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AutorizarAutomotorSolicitud`](#autorizarautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.AutorizarCpeAutomotorResponse`, raíz XML `AutorizarCPEAutomotorResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleAutomotorRespuesta`](#detalleautomotorrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `autorizarCPEAutomotorDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/autorizarCPEAutomotorDG`.

**Request** (`Wscpe.AutorizarCpeAutomotorDgRequest`, raíz XML `AutorizarCPEAutomotorDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AutorizarAutomotorDGSolicitud`](#autorizarautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.AutorizarCpeAutomotorDgResponse`, raíz XML `AutorizarCPEAutomotorDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleAutomotorDGRespuesta`](#detalleautomotordgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `autorizarCPEDuctosDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/autorizarCPEDuctosDG`.

**Request** (`Wscpe.AutorizarCpeDuctosDgRequest`, raíz XML `AutorizarCPEDuctosDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AutorizarDuctosDGSolicitud`](#autorizarductosdgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.AutorizarCpeDuctosDgResponse`, raíz XML `AutorizarCPEDuctosDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleDuctosDGRespuesta`](#detalleductosdgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `autorizarCPEEmisionDestinoDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/autorizarCPEEmisionDestinoDG`.

**Request** (`Wscpe.AutorizarCpeEmisionDestinoDgRequest`, raíz XML `AutorizarCPEEmisionDestinoDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AutorizarEmisionDestinoDGSolicitud`](#autorizaremisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.AutorizarCpeEmisionDestinoDgResponse`, raíz XML `AutorizarCPEEmisionDestinoDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleEmisionDestinoDGRespuesta`](#detalleemisiondestinodgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarCPEAutomotor`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarCPEAutomotor`.

**Request** (`Wscpe.ConsultarCpeAutomotorRequest`, raíz XML `ConsultarCPEAutomotorReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarAutomotorSolicitud`](#consultarautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarCpeAutomotorResponse`, raíz XML `ConsultarCPEAutomotorResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleAutomotorRespuesta`](#detalleautomotorrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `nuevoDestinoDestinatarioCPEAutomotor`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/nuevoDestinoDestinatarioCPEAutomotor`.

**Request** (`Wscpe.NuevoDestinoDestinatarioCpeAutomotorRequest`, raíz XML `NuevoDestinoDestinatarioCPEAutomotorReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:NuevoDestinoDestinatarioAutomotorSolicitud`](#nuevodestinodestinatarioautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.NuevoDestinoDestinatarioCpeAutomotorResponse`, raíz XML `NuevoDestinoDestinatarioCPEAutomotorResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `regresoOrigenCPEAutomotor`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/regresoOrigenCPEAutomotor`.

**Request** (`Wscpe.RegresoOrigenCpeAutomotorRequest`, raíz XML `RegresoOrigenCPEAutomotorReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RegresoOrigenAutomotorSolicitud`](#regresoorigenautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.RegresoOrigenCpeAutomotorResponse`, raíz XML `RegresoOrigenCPEAutomotorResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `regresoOrigenCPEAutomotorDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/regresoOrigenCPEAutomotorDG`.

**Request** (`Wscpe.RegresoOrigenCpeAutomotorDgRequest`, raíz XML `RegresoOrigenCPEAutomotorDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RegresoOrigenAutomotorDGSolicitud`](#regresoorigenautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.RegresoOrigenCpeAutomotorDgResponse`, raíz XML `RegresoOrigenCPEAutomotorDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `regresoOrigenCPEFerroviariaDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/regresoOrigenCPEFerroviariaDG`.

**Request** (`Wscpe.RegresoOrigenCpeFerroviariaDgRequest`, raíz XML `RegresoOrigenCPEFerroviariaDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RegresoOrigenFerroviariaDGSolicitud`](#regresoorigenferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.RegresoOrigenCpeFerroviariaDgResponse`, raíz XML `RegresoOrigenCPEFerroviariaDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `regresoOrigenCPEEmisionDestinoDG`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/regresoOrigenCPEEmisionDestinoDG`.

**Request** (`Wscpe.RegresoOrigenCpeEmisionDestinoDgRequest`, raíz XML `RegresoOrigenCPEEmisionDestinoDGReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RegresoOrigenEmisionDestinoDGSolicitud`](#regresoorigenemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.RegresoOrigenCpeEmisionDestinoDgResponse`, raíz XML `RegresoOrigenCPEEmisionDestinoDGResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `desvioCPEAutomotor`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/desvioCPEAutomotor`.

**Request** (`Wscpe.DesvioCpeAutomotorRequest`, raíz XML `DesvioCPEAutomotorReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:DesvioAutomotorSolicitud`](#desvioautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.DesvioCpeAutomotorResponse`, raíz XML `DesvioCPEAutomotorResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `confirmacionDefinitivaCPEAutomotor`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/confirmacionDefinitivaCPEAutomotor`.

**Request** (`Wscpe.ConfirmacionDefinitivaCpeAutomotorRequest`, raíz XML `ConfirmacionDefinitivaCPEAutomotorReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConfirmacionAutomotorSolicitud`](#confirmacionautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConfirmacionDefinitivaCpeAutomotorResponse`, raíz XML `ConfirmacionDefinitivaCPEAutomotorResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `editarCPEAutomotor`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/editarCPEAutomotor`.

**Request** (`Wscpe.EditarCpeAutomotorRequest`, raíz XML `EditarCPEAutomotorReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarActivaAutomotorSolicitud`](#editaractivaautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.EditarCpeAutomotorResponse`, raíz XML `EditarCPEAutomotorResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `editarCPEConfirmadaAutomotor`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/editarCPEConfirmadaAutomotor`.

**Request** (`Wscpe.EditarCpeConfirmadaAutomotorRequest`, raíz XML `EditarCPEConfirmadaAutomotorReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarCPEConfirmadaAutomotorSolicitud`](#editarcpeconfirmadaautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.EditarCpeConfirmadaAutomotorResponse`, raíz XML `EditarCPEConfirmadaAutomotorResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `editarCPEDGConfirmadaAutomotor`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/editarCPEDGConfirmadaAutomotor`.

**Request** (`Wscpe.EditarCpeConfirmadaAutomotorDgRequest`, raíz XML `EditarCPEConfirmadaAutomotorDgReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarCPEConfirmadaAutomotorDgSolicitud`](#editarcpeconfirmadaautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.EditarCpeConfirmadaAutomotorDgResponse`, raíz XML `EditarCPEConfirmadaAutomotorDgResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `editarCPEDGConfirmadaFerroviaria`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/editarCPEDGConfirmadaFerroviaria`.

**Request** (`Wscpe.EditarCpeConfirmadaFerroviariaDgRequest`, raíz XML `EditarCPEConfirmadaFerroviariaDgReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarCPEConfirmadaFerroviariaDgSolicitud`](#editarcpeconfirmadaferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.EditarCpeConfirmadaFerroviariaDgResponse`, raíz XML `EditarCPEConfirmadaFerroviariaDgResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `editarCPEConfirmadaDuctos`

Clase: **escritura**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/editarCPEConfirmadaDuctos`.

**Request** (`Wscpe.EditarCpeConfirmadaDuctosRequest`, raíz XML `EditarCPEConfirmadaDuctosReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarCPEConfirmadaDuctosSolicitud`](#editarcpeconfirmadaductossolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.EditarCpeConfirmadaDuctosResponse`, raíz XML `EditarCPEConfirmadaDuctosResp`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


### `consultarRenspa`

Clase: **consulta**; autenticación: **WSAA `wscpe`**. SOAPAction: `https://serviciosjava.afip.gob.ar/wscpe/consultarRenspa`.

**Request** (`Wscpe.ConsultarRenspaRequest`, raíz XML `ConsultarRenspaReq`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codProvincia` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

**Response** (`Wscpe.ConsultarRenspaResponse`, raíz XML `ConsultarRenspaRes`)

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarRenspaRespuesta`](#consultarrensparespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

Las respuestas incluyen `respuesta` y, según la operación, `errores` (lista `error` con código y descripción) y `metadata`. Un HTTP 200 no confirma la autorización: revisar errores y datos de negocio antes de actualizar el estado local. Las escrituras no tienen reenvío automático ni diario integrado. SOAP Fault: `NetArcaWs.Transport.SoapFaultException` expone código, razón, detalle y estado HTTP. Para escrituras, no repetir automáticamente tras fallo ambiguo; consultar/reconciliar primero.


## Catálogo de tipos complejos

Incluye todos los tipos complejos nombrados del XSD del servicio. Los elementos `base` señalan herencia; se conserva la cardinalidad declarada. `xs:documentation` se transcribe cuando existe.


### `AceptarEmisionDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AceptarEmisionDGSolicitud`](#aceptaremisiondgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AceptarEmisionDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AceptarEmisionDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AceptarEmisionDestinoDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AceptarEmisionDestinoDGSolicitud`](#aceptaremisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AceptarEmisionDestinoDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AceptarEmisionDestinoDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitDestino` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AnularCPEEmisionDestinoDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AnularCPEEmisionDestinoDGSolicitud`](#anularcpeemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AnularCPEEmisionDestinoDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AnularCPEEmisionDestinoDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitDestino` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `anulacionMotivo` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `anulacionObservaciones` | [`tns:Texto100`](#texto100) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `AnularCPERequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AnularCPESolicitud`](#anularcpesolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AnularCPEResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AnularCPESolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `anulacionMotivo` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `anulacionObservaciones` | [`tns:Texto100`](#texto100) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Auth`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `token` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sign` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRepresentada` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraAutomotorDGSolicitud`](#cabeceraautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `origen` | [`tns:OrigenAutomotorDGSolicitud`](#origenautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesAutomotorDGSolicitud`](#intervinientesautomotordgsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosCarga` | [`tns:DatosCargaAutomotorDGSolicitud`](#datoscargaautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoAutomotorDGSolicitud`](#destinoautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioSolicitud`](#destinatariosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorDGSolicitud`](#transporteautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Observaciones`](#observaciones) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `AutorizarAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraAutomotorSolicitud`](#cabeceraautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `origen` | [`tns:OrigenAutomotorSolicitud`](#origenautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `correspondeRetiroProductor` | `xsd:boolean` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `esSolicitanteCampo` | `xsd:boolean` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `retiroProductor` | [`tns:RetiroProductorSolicitud`](#retiroproductorsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesSolicitud`](#intervinientessolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosCarga` | [`tns:DatosCargaAutomotorSolicitud`](#datoscargaautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoSolicitud`](#destinosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioSolicitud`](#destinatariosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorSolicitud`](#transporteautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Observaciones`](#observaciones) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `AutorizarCPEAutomotorDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AutorizarAutomotorDGSolicitud`](#autorizarautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarCPEAutomotorDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleAutomotorDGRespuesta`](#detalleautomotordgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarCPEAutomotorRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AutorizarAutomotorSolicitud`](#autorizarautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarCPEAutomotorResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleAutomotorRespuesta`](#detalleautomotorrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarCPEDuctosDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AutorizarDuctosDGSolicitud`](#autorizarductosdgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarCPEDuctosDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleDuctosDGRespuesta`](#detalleductosdgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarCPEEmisionDestinoDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AutorizarEmisionDestinoDGSolicitud`](#autorizaremisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarCPEEmisionDestinoDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleEmisionDestinoDGRespuesta`](#detalleemisiondestinodgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarCPEFerroviariaDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AutorizarFerroviariaDGSolicitud`](#autorizarferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarCPEFerroviariaDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleFerroviariaDGRespuesta`](#detalleferroviariadgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarCPEFerroviariaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:AutorizarFerroviariaSolicitud`](#autorizarferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarCPEFerroviariaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleFerroviariaRespuesta`](#detalleferroviariarespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `AutorizarDuctosDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraDuctosDGSolicitud`](#cabeceraductosdgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `origen` | [`tns:OrigenDuctosDGSolicitud`](#origenductosdgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesDuctosDGSolicitud`](#intervinientesductosdgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `datosCarga` | [`tns:DatosCargaDuctosDGSolicitud`](#datoscargaductosdgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoDuctosDGSolicitud`](#destinoductosdgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioSolicitud`](#destinatariosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Observaciones`](#observaciones) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `AutorizarEmisionDestinoDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraAutomotorDGSolicitud`](#cabeceraautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `origen` | [`tns:OrigenEmisionDestinoDGSolicitud`](#origenemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesAutomotorDGSolicitud`](#intervinientesautomotordgsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosCarga` | [`tns:DatosCargaAutomotorDGSolicitud`](#datoscargaautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoEmisionDestinoDGSolicitud`](#destinoemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioSolicitud`](#destinatariosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorDGSolicitud`](#transporteautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Observaciones`](#observaciones) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `AutorizarFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraFerroviariaDGSolicitud`](#cabeceraferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `origen` | [`tns:OrigenFerroviariaDGSolicitud`](#origenferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesFerroviariaDGSolicitud`](#intervinientesferroviariadgsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosCarga` | [`tns:DatosCargaFerroviariaDGSolicitud`](#datoscargaferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoFerroviariaDGSolicitud`](#destinoferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioSolicitud`](#destinatariosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteFerroviariaDGSolicitud`](#transporteferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Observaciones`](#observaciones) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `AutorizarFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraSolicitud`](#cabecerasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `correspondeRetiroProductor` | `xsd:boolean` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `retiroProductor` | [`tns:RetiroProductorSolicitud`](#retiroproductorsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesSolicitud`](#intervinientessolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosCarga` | [`tns:DatosCargaFerroviariaSolicitud`](#datoscargaferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoSolicitud`](#destinosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioSolicitud`](#destinatariosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteFerroviariaSolicitud`](#transporteferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Observaciones`](#observaciones) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CPEDGPendienteActivacionRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `tipoCartaPorte` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `sucursal` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroOrden` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitSolicitante` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaPartida` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CPEEmitidasDestinoDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `tipoCartaPorte` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `sucursal` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroOrden` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitSolicitante` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaPartida` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CPEResumenRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `tipoCartaPorte` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroCTG` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaPartida` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `estado` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaUltimaModificacion` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CabeceraAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `tipoCP` | [`tns:TipoCPE`](#tipocpe) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sucursal` | [`tns:NumeroSucursal`](#numerosucursal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `nroOrden` | [`tns:NumeroOrden`](#numeroorden) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CabeceraAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `tipoCP` | [`tns:TipoCPE`](#tipocpe) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sucursal` | [`tns:NumeroSucursal`](#numerosucursal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `nroOrden` | [`tns:NumeroOrden`](#numeroorden) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CabeceraDuctosDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `sucursal` | [`tns:NumeroSucursal`](#numerosucursal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `nroOrden` | [`tns:NumeroOrden`](#numeroorden) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CabeceraFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `sucursal` | [`tns:NumeroSucursal`](#numerosucursal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `nroOrden` | [`tns:NumeroOrden`](#numeroorden) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CabeceraRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `tipoCartaPorte` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `sucursal` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroOrden` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroCTG` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaEmision` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `estado` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaInicioEstado` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaVencimiento` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `observaciones` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `anulacionMotivo` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `anulacionObservaciones` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CabeceraSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `sucursal` | [`tns:NumeroSucursal`](#numerosucursal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `nroOrden` | [`tns:NumeroOrden`](#numeroorden) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CartaPorte`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `tipoCPE` | [`tns:TipoCPE`](#tipocpe) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `sucursal` | [`tns:NumeroSucursal`](#numerosucursal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `nroOrden` | [`tns:NumeroOrden`](#numeroorden) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CartaPorteFerroviariaResumenRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `fecha` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTransportista` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTransportistaTramo2` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroOperativo` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:ConsultaOperativoCPEFerroviariaRespuesta`](#consultaoperativocpeferroviariarespuesta) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `pdf` | `xsd:base64Binary` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CartaPorteRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraRespuesta`](#cabecerarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pdf` | `xsd:base64Binary` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CerrarContingenciaCPEEmisionDestinoDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:CerrarContingenciaEmisionDestinoDGSolicitud`](#cerrarcontingenciaemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CerrarContingenciaCPEEmisionDestinoDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CerrarContingenciaCPERequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:CerrarContingenciaFerroviariaSolicitud`](#cerrarcontingenciaferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CerrarContingenciaCPEResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CerrarContingenciaEmisionDestinoDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitDestino` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `concepto` | [`tns:ConceptoCierreContingencia`](#conceptocierrecontingencia) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `reactivacionDestino` | [`tns:ReactivacionDestinoFerroviariaSolicitud`](#reactivaciondestinoferroviariasolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `motivoDesactivacionCP` | [`tns:DesactivacionSolicitud`](#desactivacionsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CerrarContingenciaFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `concepto` | [`tns:ConceptoCierreContingencia`](#conceptocierrecontingencia) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `reactivacionDestino` | [`tns:ReactivacionDestinoFerroviariaSolicitud`](#reactivaciondestinoferroviariasolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `motivoDesactivacionCP` | [`tns:DesactivacionSolicitud`](#desactivacionsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CodigoDescripcion`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `CodigoDescripcionGrano`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `grano` | [`tns:CodigoGrano`](#codigograno) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoBrutoDescarga` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoTaraDescarga` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervConfirmacionAutomotorSolicitud`](#intervconfirmacionautomotorsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBrutoDescarga` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoTaraDescarga` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionDefinitivaCPEAutomotorDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConfirmacionAutomotorDGSolicitud`](#confirmacionautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionDefinitivaCPEAutomotorDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionDefinitivaCPEAutomotorRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConfirmacionAutomotorSolicitud`](#confirmacionautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionDefinitivaCPEAutomotorResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionDefinitivaCPEDuctosDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConfirmacionDuctosDGSolicitud`](#confirmacionductosdgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionDefinitivaCPEDuctosDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionDefinitivaCPEFerroviariaDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConfirmacionFerroviariaDGSolicitud`](#confirmacionferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionDefinitivaCPEFerroviariaDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionDefinitivaCPEFerroviariaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConfirmacionFerroviariaSolicitud`](#confirmacionferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionDefinitivaCPEFerroviariaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionDuctosDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoBrutoDescarga` | [`tns:KilogramosDuctos`](#kilogramosductos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoBrutoDescarga` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoTaraDescarga` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ramalDescarga` | [`tns:Ramal`](#ramal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmacionFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervConfirmacionFerroviariaSolicitud`](#intervconfirmacionferroviariasolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioSolicitud`](#destinatariosolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBrutoDescarga` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoTaraDescarga` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ramalDescarga` | [`tns:Ramal`](#ramal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmarArriboCPERequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConfirmarArriboSolicitud`](#confirmararribosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmarArriboCPEResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConfirmarArriboSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultaCPEFerroviariaPorNroOperativoRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultaCPEFerroviariaPorNroOperativoSolicitud`](#consultacpeferroviariapornrooperativosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultaCPEFerroviariaPorNroOperativoResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteFerroviariaResumenRespuesta`](#cartaporteferroviariaresumenrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultaCPEFerroviariaPorNroOperativoSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroOperativo` | [`tns:NumeroOperativo`](#numerooperativo) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultaOperativoCPEFerroviariaRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroCTG` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroVagon` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `grano` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroPrecinto` | `xsd:string` | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `pesoBruto` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoTara` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTitularPlanta` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroCTG` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroCTG` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCPEAutomotorDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarAutomotorDGSolicitud`](#consultarautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEAutomotorDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleAutomotorDGRespuesta`](#detalleautomotordgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEAutomotorRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarAutomotorSolicitud`](#consultarautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEAutomotorResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleAutomotorRespuesta`](#detalleautomotorrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEDGPendienteActivacionRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarCPEDGPendienteActivacionSolicitud`](#consultarcpedgpendienteactivacionsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEDGPendienteActivacionResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarCPEDGPendienteActivacionRespuesta`](#consultarcpedgpendienteactivacionrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEDGPendienteActivacionRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CPEDGPendienteActivacionRespuesta`](#cpedgpendienteactivacionrespuesta) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCPEDGPendienteActivacionSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEDuctosRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarDuctosSolicitud`](#consultarductossolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEDuctosResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleDuctosDGRespuesta`](#detalleductosdgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEEmisionDestinoDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarEmisionDestinoDGSolicitud`](#consultaremisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEEmisionDestinoDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleEmisionDestinoDGRespuesta`](#detalleemisiondestinodgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEEmitidasDestinoDGPendientesActivacionRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarCPEEmitidasDestinoDGPendientesActivacionSolicitud`](#consultarcpeemitidasdestinodgpendientesactivacionsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEEmitidasDestinoDGPendientesActivacionResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarCPEEmitidasDestinoDGPendientesActivacionRespuesta`](#consultarcpeemitidasdestinodgpendientesactivacionrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEEmitidasDestinoDGPendientesActivacionRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CPEEmitidasDestinoDGRespuesta`](#cpeemitidasdestinodgrespuesta) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCPEEmitidasDestinoDGPendientesActivacionSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEFerroviariaDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarFerroviariaDGSolicitud`](#consultarferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEFerroviariaDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleFerroviariaDGRespuesta`](#detalleferroviariadgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEFerroviariaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarFerroviariaSolicitud`](#consultarferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEFerroviariaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DetalleFerroviariaRespuesta`](#detalleferroviariarespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEPPendientesDeResolucionRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarCPEPPendientesDeResolucionSolicitud`](#consultarcpeppendientesderesolucionsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEPPendientesDeResolucionResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarCPEPPendientesDeResolucionRespuesta`](#consultarcpeppendientesderesolucionrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEPPendientesDeResolucionRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CPEResumenRespuesta`](#cperesumenrespuesta) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCPEPPendientesDeResolucionSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `perfil` | [`tns:SolicitanteDestino`](#solicitantedestino) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCPEPorDestinoRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarCPEPorDestinoSolicitud`](#consultarcpepordestinosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEPorDestinoResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarCPEPorDestinoRespuesta`](#consultarcpepordestinorespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCPEPorDestinoRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CPEResumenRespuesta`](#cperesumenrespuesta) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCPEPorDestinoSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaPartidaDesde` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaPartidaHasta` | `xsd:date` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `tipoCartaPorte` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarCategoriasSemillasRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCategoriasSemillasResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarCategoriasSemillasRespuesta`](#consultarcategoriassemillasrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarCategoriasSemillasRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `categoria` | [`tns:CodigoDescripcion`](#codigodescripcion) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarDerivadosGranariosRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarDerivadosGranariosResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarDerivadosGranariosRespuesta`](#consultarderivadosgranariosrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarDerivadosGranariosRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `derivadoGranario` | [`tns:DerivadoGranario`](#derivadogranario) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarDomiciliosPorCUITRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarDomiciliosPorCUITResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarDomiciliosPorCUITRespuesta`](#consultardomiciliosporcuitrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarDomiciliosPorCUITRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `domicilio` | [`tns:DomicilioPUCRespuesta`](#domiciliopucrespuesta) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarDomiciliosPorCUITSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarDuctosSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroCTG` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarEmisionDestinoDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTitularPlanta` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroCTG` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTitularPlanta` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroCTG` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroCTG` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarLocalidadesPorProvinciaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarLocalidadesPorProvinciaSolicitud`](#consultarlocalidadesporprovinciasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarLocalidadesPorProvinciaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarLocalidadesPorProvinciaRespuesta`](#consultarlocalidadesporprovinciarespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarLocalidadesPorProvinciaRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `localidad` | [`tns:CodigoDescripcion`](#codigodescripcion) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarLocalidadesPorProvinciaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codProvincia` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarLocalidadesProductorRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarLocalidadesProductorSolicitud`](#consultarlocalidadesproductorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarLocalidadesProductorResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarLocalidadesProductorRespuesta`](#consultarlocalidadesproductorrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarLocalidadesProductorRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `localidad` | [`tns:LocalidadProductor`](#localidadproductor) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarLocalidadesProductorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarPlantasDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarPlantasDGSolicitud`](#consultarplantasdgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarPlantasDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarPlantasDGRespuesta`](#consultarplantasdgrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarPlantasDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `planta` | [`tns:PlantaDGResponse`](#plantadgresponse) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarPlantasDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarPlantasRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarPlantasSolicitud`](#consultarplantassolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarPlantasResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarPlantasRespuesta`](#consultarplantasrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarPlantasRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `planta` | [`tns:PlantaResponse`](#plantaresponse) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarPlantasSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarProvinciasRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarProvinciasResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarProvinciasRespuesta`](#consultarprovinciasrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarProvinciasRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `provincia` | [`tns:CodigoDescripcion`](#codigodescripcion) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarRenspaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codProvincia` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarRenspaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarRenspaRespuesta`](#consultarrensparespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarRenspaRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `renspa` | [`tns:Renspa`](#renspa) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarTiposEmbalajeRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarTiposEmbalajeResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarTiposEmbalajeRespuesta`](#consultartiposembalajerespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarTiposEmbalajeRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `tipoEmbalaje` | [`tns:TipoEmbalajeUnidadMedida`](#tipoembalajeunidadmedida) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarTiposGranoRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarTiposGranoResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarTiposGranoRespuesta`](#consultartiposgranorespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarTiposGranoRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `grano` | [`tns:CodigoDescripcion`](#codigodescripcion) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarUltNroOrdenRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:ConsultarUltNroOrdenSolicitud`](#consultarultnroordensolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarUltNroOrdenResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarUltNroOrdenRespuesta`](#consultarultnroordenrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarUltNroOrdenRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroOrden` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarUltNroOrdenSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `sucursal` | [`tns:NumeroSucursal`](#numerosucursal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `tipoCPE` | [`tns:TipoCPE`](#tipocpe) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarUnidadesMedidaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarUnidadesMedidaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarUnidadesMedidaRespuesta`](#consultarunidadesmedidarespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarUnidadesMedidaRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `unidadMedida` | [`tns:CodigoDescripcion`](#codigodescripcion) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarVariedadesSemillasRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ConsultarVariedadesSemillasResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:ConsultarVariedadesSemillasRespuesta`](#consultarvariedadessemillasrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `ConsultarVariedadesSemillasRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `variedad` | [`tns:CodigoDescripcionGrano`](#codigodescripciongrano) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Coordenada`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `grados` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `minutos` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `segundos` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `coordenada` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CoordenadasGPS`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `latitud` | [`tns:Coordenada`](#coordenada) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `longitud` | [`tns:Coordenada`](#coordenada) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `ubicacionGeoreferencial` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CoordenadasGPSRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `latitud` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `longitud` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `ubicacionGeoreferencial` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `CoordenadasGPSSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `latitud` | [`tns:GMS`](#gms) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `longitud` | [`tns:GMS`](#gms) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ubicacionGeoreferencial` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DatosCargaAutomotorDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codDerivadoGranario` | [`tns:CodigoDerivadoGranario`](#codigoderivadogranario) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBruto` | [`tns:Kilogramos`](#kilogramos) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoTara` | [`tns:Kilogramos`](#kilogramos) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBrutoDescarga` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoTaraDescarga` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoEmbalaje` | [`tns:TipoEmbalaje`](#tipoembalaje) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `otroEmbalaje` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `unidadMedida` | [`tns:UnidadMedida`](#unidadmedida) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cantidadUnidades` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `kgLitroM3` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `lote` | [`tns:Texto15`](#texto15) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaLote` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DatosCargaAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codDerivadoGranario` | [`tns:CodigoDerivadoGranario`](#codigoderivadogranario) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoBruto` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoTara` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `tipoEmbalaje` | [`tns:TipoEmbalaje`](#tipoembalaje) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `otroEmbalaje` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `unidadMedida` | [`tns:UnidadMedida`](#unidadmedida) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cantidadUnidades` | [`tns:Digitos5`](#digitos5) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `kgLitroM3` | [`tns:Digitos5`](#digitos5) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `lote` | [`tns:Texto15`](#texto15) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaLote` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DatosCargaAutomotorRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codGrano` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `semilla` | [`tns:Semilla`](#semilla) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBruto` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoTara` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cosecha` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBrutoDescarga` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoTaraDescarga` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoEmbalaje` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `otroEmbalaje` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `unidadMedida` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cantidadUnidades` | [`tns:Digitos5`](#digitos5) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `kgLitroM3` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `lote` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DatosCargaAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cosecha` | [`tns:Cosecha`](#cosecha) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `semilla` | [`tns:Semilla`](#semilla) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBruto` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoTara` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DatosCargaDuctosDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codGrano` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codDerivadoGranario` | [`tns:CodigoDerivadoGranario`](#codigoderivadogranario) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cosecha` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBrutoDescarga` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoTaraDescarga` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoEmbalaje` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `otroEmbalaje` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `unidadMedida` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBruto` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `lote` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaLote` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DatosCargaDuctosDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codDerivadoGranario` | [`tns:CodigoDerivadoGranario`](#codigoderivadogranario) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoBruto` | [`tns:KilogramosDuctos`](#kilogramosductos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `tipoEmbalaje` | [`tns:TipoEmbalaje`](#tipoembalaje) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `otroEmbalaje` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `unidadMedida` | [`tns:UnidadMedida`](#unidadmedida) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `lote` | [`tns:Texto15`](#texto15) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaLote` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DatosCargaFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codDerivadoGranario` | [`tns:CodigoDerivadoGranario`](#codigoderivadogranario) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoBruto` | [`tns:KilogramosFerroviariaDG`](#kilogramosferroviariadg) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoTara` | [`tns:KilogramosFerroviariaDG`](#kilogramosferroviariadg) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `tipoEmbalaje` | [`tns:TipoEmbalaje`](#tipoembalaje) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `otroEmbalaje` | [`tns:Texto140`](#texto140) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `unidadMedida` | [`tns:UnidadMedida`](#unidadmedida) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cantidadUnidades` | [`tns:Digitos5`](#digitos5) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `kgLitroM3` | [`tns:Digitos5`](#digitos5) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `lote` | [`tns:Texto15`](#texto15) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaLote` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DatosCargaFerroviariaRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codGrano` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codDerivadoGranario` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cosecha` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBruto` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoTara` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBrutoDescarga` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoTaraDescarga` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoEmbalaje` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `otroEmbalaje` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `unidadMedida` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cantidadUnidades` | [`tns:Digitos5`](#digitos5) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `kgLitroM3` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `lote` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaLote` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DatosCargaFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cosecha` | [`tns:Cosecha`](#cosecha) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoBruto` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoTara` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DerivadoGranario`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `granoPadre` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `granoPadreDescripcion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codigo` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesactivacionSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `concepto` | [`tns:MotivoDesactivacion`](#motivodesactivacion) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | [`tns:Texto140`](#texto140) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DescargadoDestinoCPEEmisionDestinoDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:DescargadoDestinoCPEEmisionDestinoDGSolicitud`](#descargadodestinocpeemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DescargadoDestinoCPEEmisionDestinoDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DescargadoDestinoCPEEmisionDestinoDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitDestino` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DescargadoDestinoCPERequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:DescargadoDestinoCPESolicitud`](#descargadodestinocpesolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DescargadoDestinoCPEResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DescargadoDestinoCPESolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DestinatarioRegresoOrigenSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DestinatarioRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DestinatarioSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DestinoAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioDestino` | [`tns:DomicilioPUC`](#domiciliopuc) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DestinoDuctosDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codProvincia` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codLocalidad` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `planta` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioDestino` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaHoraInicioEnvio` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaHoraFinEnvio` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DestinoDuctosDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `domicilioDestino` | [`tns:DomicilioPUC`](#domiciliopuc) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHoraInicioEnvio` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHoraFinEnvio` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DestinoEmisionDestinoDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `domicilioDestino` | [`tns:DomicilioPUC`](#domiciliopuc) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DestinoEmisionDestinoRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `planta` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioDestino` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTitularPlanta` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DestinoFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `domicilioDestino` | [`tns:DomicilioPUC`](#domiciliopuc) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DestinoRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codProvincia` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codLocalidad` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `planta` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `plantaObservaciones` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioDestino` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DestinoSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `esDestinoCampo` | `xsd:boolean` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codProvincia` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codLocalidad` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DesvioAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DesvioDestinoAutomotorDGSolicitud`](#desviodestinoautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorModificaDGSolicitud`](#transporteautomotormodificadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DesvioDestinoAutomotorSolicitud`](#desviodestinoautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorModificaSolicitud`](#transporteautomotormodificasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioCPEAutomotorDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:DesvioAutomotorDGSolicitud`](#desvioautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioCPEAutomotorDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioCPEAutomotorRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:DesvioAutomotorSolicitud`](#desvioautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioCPEAutomotorResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioCPEFerroviariaDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:DesvioFerroviariaDGSolicitud`](#desvioferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioCPEFerroviariaDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioCPEFerroviariaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:DesvioFerroviariaSolicitud`](#desvioferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioCPEFerroviariaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioDestinoAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioDestino` | [`tns:DomicilioPUC`](#domiciliopuc) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioDestinoAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codProvincia` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codLocalidad` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `esDestinoCampo` | `xsd:boolean` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioDestinoFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `domicilioDestino` | [`tns:DomicilioPUC`](#domiciliopuc) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioDestinoFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codProvincia` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codLocalidad` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DesvioDestinoFerroviariaDGSolicitud`](#desviodestinoferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteFerroviariaModificaSolicitud`](#transporteferroviariamodificasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DesvioFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DesvioDestinoFerroviariaSolicitud`](#desviodestinoferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteFerroviariaModificaSolicitud`](#transporteferroviariamodificasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DetalleAutomotorDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraRespuesta`](#cabecerarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `origen` | [`tns:OrigenAutomotorDGRespuesta`](#origenautomotordgrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesAutomotorDGRespuesta`](#intervinientesautomotordgrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosCarga` | [`tns:DatosCargaAutomotorDGRespuesta`](#datoscargaautomotordgrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoRespuesta`](#destinorespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioRespuesta`](#destinatariorespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorDGRespuesta`](#transporteautomotordgrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pdf` | `xsd:base64Binary` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DetalleAutomotorRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraRespuesta`](#cabecerarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `origen` | [`tns:OrigenAutomotorRespuesta`](#origenautomotorrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `correspondeRetiroProductor` | `xsd:boolean` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `retiroProductor` | [`tns:RetiroProductorRespuesta`](#retiroproductorrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesAutomotorRespuesta`](#intervinientesautomotorrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosCarga` | [`tns:DatosCargaAutomotorRespuesta`](#datoscargaautomotorrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoRespuesta`](#destinorespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioRespuesta`](#destinatariorespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorRespuesta`](#transporteautomotorrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pdf` | `xsd:base64Binary` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DetalleContingenciaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `concepto` | [`tns:MotivoContingencia`](#motivocontingencia) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | [`tns:Texto140`](#texto140) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DetalleDuctosDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraRespuesta`](#cabecerarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `origen` | [`tns:OrigenDuctosDGRespuesta`](#origenductosdgrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesDuctosDGRespuesta`](#intervinientesductosdgrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosCarga` | [`tns:DatosCargaDuctosDGRespuesta`](#datoscargaductosdgrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoDuctosDGRespuesta`](#destinoductosdgrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioRespuesta`](#destinatariorespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pdf` | `xsd:base64Binary` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DetalleEmisionDestinoDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraRespuesta`](#cabecerarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `origen` | [`tns:OrigenEmisionDestinoDGRespuesta`](#origenemisiondestinodgrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesAutomotorDGRespuesta`](#intervinientesautomotordgrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosCarga` | [`tns:DatosCargaAutomotorDGRespuesta`](#datoscargaautomotordgrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoEmisionDestinoRespuesta`](#destinoemisiondestinorespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioRespuesta`](#destinatariorespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorDGRespuesta`](#transporteautomotordgrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pdf` | `xsd:base64Binary` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DetalleFerroviariaDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraRespuesta`](#cabecerarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `origen` | [`tns:OrigenFerroviariaRespuesta`](#origenferroviariarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesFerroviariaRespuesta`](#intervinientesferroviariarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosCarga` | [`tns:DatosCargaFerroviariaRespuesta`](#datoscargaferroviariarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoRespuesta`](#destinorespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioRespuesta`](#destinatariorespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteFerroviariaRespuesta`](#transporteferroviariarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pdf` | `xsd:base64Binary` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DetalleFerroviariaRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cabecera` | [`tns:CabeceraRespuesta`](#cabecerarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `origen` | [`tns:OrigenFerroviariaRespuesta`](#origenferroviariarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `correspondeRetiroProductor` | `xsd:boolean` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `retiroProductor` | [`tns:RetiroProductorRespuesta`](#retiroproductorrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesFerroviariaRespuesta`](#intervinientesferroviariarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `datosCarga` | [`tns:DatosCargaFerroviariaRespuesta`](#datoscargaferroviariarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoRespuesta`](#destinorespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioRespuesta`](#destinatariorespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteFerroviariaRespuesta`](#transporteferroviariarespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pdf` | `xsd:base64Binary` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `errores` | [`tns:Errores`](#errores) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `metadata` | [`tns:Metadata`](#metadata) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `DomicilioPUC`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `tipo` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `orden` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DomicilioPUCRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `tipo` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `orden` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DummyResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:DummyRespuesta`](#dummyrespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `DummyRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `appserver` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `authserver` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `dbserver` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `EditarActivaAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroCTG` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaPrimaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria2` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaPrimaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaSecundaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitDestinatario` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitChofer` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitTransportista` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoSolicitud`](#destinosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cosecha` | [`tns:Cosecha`](#cosecha) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBruto` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `dominio` | [`tns:NumeroDominio`](#numerodominio) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | [`tns:Kilometros`](#kilometros) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tarifa` | [`tns:Tarifa`](#tarifa) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Texto140`](#texto140) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `EditarActivaDGAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroCTG` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitTransportista` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitChofer` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codDerivadoGranario` | [`tns:CodigoDerivadoGranario`](#codigoderivadogranario) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `unidadMedida` | [`tns:UnidadMedida`](#unidadmedida) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoEmbalaje` | [`tns:TipoEmbalaje`](#tipoembalaje) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `otroEmbalaje` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cantidadUnidades` | [`tns:Digitos5`](#digitos5) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `kgLitroM3` | [`tns:Digitos5`](#digitos5) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesAutomotorDGSolicitud`](#intervinientesautomotordgsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoAutomotorDGSolicitud`](#destinoautomotordgsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitDestinatario` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tarifa` | [`tns:Tarifa`](#tarifa) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitPagadorFlete` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitIntermediarioFlete` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | [`tns:Kilometros`](#kilometros) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoBruto` | [`tns:Kilogramos`](#kilogramos) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `dominio` | [`tns:NumeroDominio`](#numerodominio) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Observaciones`](#observaciones) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `EditarActivaDGDuctosSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroCTG` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codDerivadoGranario` | [`tns:CodigoDerivadoGranario`](#codigoderivadogranario) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `unidadMedida` | [`tns:UnidadMedida`](#unidadmedida) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoEmbalaje` | [`tns:TipoEmbalaje`](#tipoembalaje) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesDuctosDGModificacionActivaSolicitud`](#intervinientesductosdgmodificacionactivasolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitDestinatario` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Observaciones`](#observaciones) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `EditarActivaDGFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroCTG` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitTransportista` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitConductor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTransportistaTramo2` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitConductorTramo2` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroVagon` | [`tns:NumeroVagon`](#numerovagon) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroPrecinto` | [`tns:NumeroPrecinto`](#numeroprecinto) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |
| `nroOperativo` | [`tns:NumeroOperativo`](#numerooperativo) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ramal` | [`tns:Ramal`](#ramal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codDerivadoGranario` | [`tns:CodigoDerivadoGranario`](#codigoderivadogranario) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `unidadMedida` | [`tns:UnidadMedida`](#unidadmedida) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tipoEmbalaje` | [`tns:TipoEmbalaje`](#tipoembalaje) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `otroEmbalaje` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cantidadUnidades` | [`tns:Digitos5`](#digitos5) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `kgLitroM3` | [`tns:Digitos5`](#digitos5) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervinientesFerroviariaDGSolicitud`](#intervinientesferroviariadgsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitDestinatario` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoFerroviariaDGSolicitud`](#destinoferroviariadgsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tarifa` | [`tns:Tarifa`](#tarifa) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitPagadorFlete` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitIntermediarioFlete` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | [`tns:Kilometros`](#kilometros) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `pesoBruto` | [`tns:Kilogramos`](#kilogramos) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Observaciones`](#observaciones) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `EditarActivaFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroCTG` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitDestinatario` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaPrimaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria2` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaPrimaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaSecundaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTransportista` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoSolicitud`](#destinosolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroVagon` | [`tns:NumeroVagon`](#numerovagon) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `pesoBruto` | [`tns:Kilogramos`](#kilogramos) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codGrano` | [`tns:CodigoGrano`](#codigograno) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEAutomotorRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarActivaAutomotorSolicitud`](#editaractivaautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEAutomotorResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaAutomotorDgRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarCPEConfirmadaAutomotorDgSolicitud`](#editarcpeconfirmadaautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaAutomotorDgResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaAutomotorDgSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroCTG` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervEditarConfirmadaAutomotorDgSolicitud`](#interveditarconfirmadaautomotordgsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Observaciones`](#observaciones) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaAutomotorRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarCPEConfirmadaAutomotorSolicitud`](#editarcpeconfirmadaautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaAutomotorResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroCTG` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervEditarConfirmadaAutomotorSolicitud`](#interveditarconfirmadaautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaDuctosRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarCPEConfirmadaDuctosSolicitud`](#editarcpeconfirmadaductossolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaDuctosResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaDuctosSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroCTG` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervEditarConfirmadaAutomotorDgSolicitud`](#interveditarconfirmadaautomotordgsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Observaciones`](#observaciones) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaFerroviariaDgRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarCPEConfirmadaFerroviariaDgSolicitud`](#editarcpeconfirmadaferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaFerroviariaDgResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaFerroviariaDgSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroCTG` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervEditarConfirmadaFerroviariaDgSolicitud`](#interveditarconfirmadaferroviariadgsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `observaciones` | [`tns:Observaciones`](#observaciones) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaFerroviariaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarCPEConfirmadaFerroviariaSolicitud`](#editarcpeconfirmadaferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaFerroviariaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEConfirmadaFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroCTG` | `xsd:long` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `intervinientes` | [`tns:IntervEditarConfirmadaFerroviariaSolicitud`](#interveditarconfirmadaferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEDGAutomotorRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarActivaDGAutomotorSolicitud`](#editaractivadgautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEDGAutomotorResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEDGDuctosRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarActivaDGDuctosSolicitud`](#editaractivadgductossolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEDGDuctosResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEDGFerroviariaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarActivaDGFerroviariaSolicitud`](#editaractivadgferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEDGFerroviariaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEFerroviariaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:EditarActivaFerroviariaSolicitud`](#editaractivaferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `EditarCPEFerroviariaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `Errores`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `error` | [`tns:CodigoDescripcion`](#codigodescripcion) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |

### `ExceptionType`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `uuid` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `timestamp` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `businessErrorId` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `exceptionDetails` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `serverName` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `GMS`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `grados` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `minutos` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `segundos` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `InformarContingenciaEmisionDestinoDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:InformarContingenciaEmisionDestinoDGSolicitud`](#informarcontingenciaemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `InformarContingenciaEmisionDestinoDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `InformarContingenciaEmisionDestinoDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitDestino` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `contingencia` | [`tns:DetalleContingenciaSolicitud`](#detallecontingenciasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `InformarContingenciaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:InformarContingenciaSolicitud`](#informarcontingenciasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `InformarContingenciaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `InformarContingenciaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `contingencia` | [`tns:DetalleContingenciaSolicitud`](#detallecontingenciasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `IntervConfirmacionAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRepresentanteRecibidor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervConfirmacionFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercialVentaPrimaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria2` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaPrimaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaSecundaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRepresentanteRecibidor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervEditarConfirmadaAutomotorDgSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercial` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitComisionista` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervEditarConfirmadaAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitCorredorVentaPrimaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaSecundaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaPrimaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialProductor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervEditarConfirmadaFerroviariaDgSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercial` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitComisionista` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervEditarConfirmadaFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitCorredorVentaPrimaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaSecundaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaPrimaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialProductor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervinientesAutomotorDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercial` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitMercadoATermino` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitComisionista` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredor` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervinientesAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercial` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitMercadoATermino` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitComisionista` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervinientesAutomotorRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercialVentaPrimaria` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria2` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitMercadoATermino` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaPrimaria` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaSecundaria` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRepresentanteEntregador` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRepresentanteRecibidor` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervinientesDuctosDGModificacionActivaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercial` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitMercadoATermino` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitComisionista` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervinientesDuctosDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercial` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitMercadoATermino` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitComisionista` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredor` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervinientesDuctosDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercial` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitMercadoATermino` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitComisionista` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervinientesFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercial` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitComisionista` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitMercadoATermino` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervinientesFerroviariaRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercial` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaPrimaria` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria2` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitMercadoATermino` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaPrimaria` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaSecundaria` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRepresentanteEntregador` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRepresentanteRecibidor` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitComisionista` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredor` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `IntervinientesSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercialVentaPrimaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRemitenteComercialVentaSecundaria2` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitMercadoATermino` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaPrimaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitCorredorVentaSecundaria` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRepresentanteEntregador` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitRepresentanteRecibidor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `LocalidadProductor`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `coordenadas` | [`tns:CoordenadasGPS`](#coordenadasgps) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |

### `Metadata`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `servidor` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaHora` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `NuevoDestinatarioCPEDuctosDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoAutomotorDGSolicitud`](#destinoautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioSolicitud`](#destinatariosolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorModificaDGSolicitud`](#transporteautomotormodificadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoSolicitud`](#destinosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioSolicitud`](#destinatariosolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorModificaSolicitud`](#transporteautomotormodificasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioCPEAutomotorDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:NuevoDestinoDestinatarioAutomotorDGSolicitud`](#nuevodestinodestinatarioautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioCPEAutomotorDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioCPEAutomotorRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:NuevoDestinoDestinatarioAutomotorSolicitud`](#nuevodestinodestinatarioautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioCPEAutomotorResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioCPEEmisionDestinoDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:NuevoDestinoDestinatarioEmisionDestinoDGSolicitud`](#nuevodestinodestinatarioemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioCPEEmisionDestinoDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioCPEFerroviariaDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:NuevoDestinoDestinatarioFerroviariaDGSolicitud`](#nuevodestinodestinatarioferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioCPEFerroviariaDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioCPEFerroviariaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:NuevoDestinoDestinatarioFerroviariaSolicitud`](#nuevodestinodestinatarioferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioCPEFerroviariaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioEmisionDestinoDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitDestino` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoAutomotorDGSolicitud`](#destinoautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioSolicitud`](#destinatariosolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorModificaDGSolicitud`](#transporteautomotormodificadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoFerroviariaDGSolicitud`](#destinoferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioSolicitud`](#destinatariosolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteFerroviariaModificaSolicitud`](#transporteferroviariamodificasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `NuevoDestinoDestinatarioFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destino` | [`tns:DestinoSolicitud`](#destinosolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `destinatario` | [`tns:DestinatarioSolicitud`](#destinatariosolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteFerroviariaModificaSolicitud`](#transporteferroviariamodificasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `OrigenAutomotorDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `esUsuarioIndustria` | `xsd:boolean` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `plantaObservaciones` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTitularPlanta` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioOrigen` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitOrigen` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `OrigenAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `esUsuarioIndustria` | `xsd:boolean` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTitularPlanta` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioOrigen` | [`tns:DomicilioPUC`](#domiciliopuc) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `OrigenAutomotorRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codProvincia` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codLocalidad` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroRenspa` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilio` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `planta` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `plantaAFIP` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `coordenadasGPS` | [`tns:CoordenadasGPSRespuesta`](#coordenadasgpsrespuesta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `OrigenAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `operador` | [`tns:OrigenOperadorAutomotorSolicitud`](#origenoperadorautomotorsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `productor` | [`tns:OrigenProductorAutomotorSolicitud`](#origenproductorautomotorsolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `OrigenDuctosDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `esUsuarioIndustria` | `xsd:boolean` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTitularPlanta` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioOrigen` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitOrigen` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `OrigenDuctosDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `esUsuarioIndustria` | `xsd:boolean` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitTitularPlanta` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioOrigen` | [`tns:DomicilioPUC`](#domiciliopuc) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `OrigenEmisionDestinoDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioOrigen` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitOrigen` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `OrigenEmisionDestinoDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitOrigen` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `domicilioOrigen` | [`tns:DomicilioPUC`](#domiciliopuc) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `OrigenFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `esUsuarioIndustria` | `xsd:boolean` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitTitularPlanta` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioOrigen` | [`tns:DomicilioPUC`](#domiciliopuc) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `OrigenFerroviariaRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuit` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `esUsuarioIndustria` | `xsd:boolean` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codProvincia` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codLocalidad` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilio` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `planta` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTitularPlanta` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `OrigenOperadorAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codProvincia` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codLocalidad` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `OrigenProductorAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codProvincia` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codLocalidad` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `nroRenspa` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `coordenadasGPS` | [`tns:CoordenadasGPSSolicitud`](#coordenadasgpssolicitud) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `PlantaDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroPlanta` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `actividad` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `conPlantaDG` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `sinPlantaDG` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `PlantaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroPlanta` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codProvincia` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codLocalidad` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `latitud` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `longitud` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `ubicacionGeoreferencial` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `Ramal`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | [`tns:CodigoRamal`](#codigoramal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | [`tns:Texto50`](#texto50) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `ReactivacionDestinoFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitTransportista` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroOperativo` | [`tns:NumeroOperativo`](#numerooperativo) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `RechazarEmisionDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RechazarEmisionDGSolicitud`](#rechazaremisiondgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RechazarEmisionDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RechazarEmisionDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RechazarEmisionDestinoDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RechazarEmisionDestinoDGSolicitud`](#rechazaremisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RechazarEmisionDestinoDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RechazarEmisionDestinoDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitDestino` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RechazoCPERequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RechazoCPESolicitud`](#rechazocpesolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RechazoCPEResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RechazoCPESolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitSolicitante` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `rechazoMotivo` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `rechazoObservaciones` | [`tns:Texto100`](#texto100) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `RegresoOrigenAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorModificaDGSolicitud`](#transporteautomotormodificadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioDestino` | [`tns:DomicilioPUC`](#domiciliopuc) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitDestinatario` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `RegresoOrigenAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorModificaSolicitud`](#transporteautomotormodificasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RegresoOrigenCPEAutomotorDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RegresoOrigenAutomotorDGSolicitud`](#regresoorigenautomotordgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RegresoOrigenCPEAutomotorDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RegresoOrigenCPEAutomotorRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RegresoOrigenAutomotorSolicitud`](#regresoorigenautomotorsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RegresoOrigenCPEAutomotorResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RegresoOrigenCPEEmisionDestinoDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RegresoOrigenEmisionDestinoDGSolicitud`](#regresoorigenemisiondestinodgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RegresoOrigenCPEEmisionDestinoDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RegresoOrigenCPEFerroviariaDGRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RegresoOrigenFerroviariaDGSolicitud`](#regresoorigenferroviariadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RegresoOrigenCPEFerroviariaDGResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RegresoOrigenCPEFerroviariaRequest`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `auth` | [`tns:Auth`](#auth) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `solicitud` | [`tns:RegresoOrigenFerroviariaSolicitud`](#regresoorigenferroviariasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RegresoOrigenCPEFerroviariaResponse`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `respuesta` | [`tns:CartaPorteRespuesta`](#cartaporterespuesta) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RegresoOrigenEmisionDestinoDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitDestino` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteAutomotorModificaDGSolicitud`](#transporteautomotormodificadgsolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `RegresoOrigenFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteFerroviariaModificaSolicitud`](#transporteferroviariamodificasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `planta` | [`tns:NumeroPlanta`](#numeroplanta) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `domicilioDestino` | [`tns:DomicilioPUC`](#domiciliopuc) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitDestinatario` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RegresoOrigenFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cartaPorte` | [`tns:CartaPorte`](#cartaporte) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `transporte` | [`tns:TransporteFerroviariaModificaSolicitud`](#transporteferroviariamodificasolicitud) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `Renspa`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `nroRenspa` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `RetiroProductorRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercialProductor` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `RetiroProductorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitRemitenteComercialProductor` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `Semilla`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `presentacionInase` | [`tns:PresentacionInase`](#presentacioninase) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `categoriaSemilla` | `xsd:int` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `variedadSemilla` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `TipoEmbalajeUnidadMedida`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `codigo` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `descripcion` | `xsd:string` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `unidadMedida` | [`tns:CodigoDescripcion`](#codigodescripcion) | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |

### `TransporteAutomotorDGRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitTransportista` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `dominio` | `xsd:string` | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `fechaHoraPartida` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | [`tns:Kilometros`](#kilometros) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitChofer` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `tarifa` | [`tns:Tarifa`](#tarifa) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tarifaReferencia` | `xsd:decimal` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitPagadorFlete` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitIntermediarioFlete` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `TransporteAutomotorDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitTransportista` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `dominio` | [`tns:NumeroDominio`](#numerodominio) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHoraPartida` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | [`tns:Kilometros`](#kilometros) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitChofer` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `tarifa` | [`tns:Tarifa`](#tarifa) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitPagadorFlete` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitIntermediarioFlete` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `TransporteAutomotorModificaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `fechaHoraPartida` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | [`tns:Kilometros`](#kilometros) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `TransporteAutomotorModificaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `fechaHoraPartida` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | [`tns:Kilometros`](#kilometros) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codigoTurno` | [`tns:CodigoTurno`](#codigoturno) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `TransporteAutomotorRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitTransportista` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `dominio` | `xsd:string` | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `fechaHoraPartida` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `codigoTurno` | `xsd:string` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitChofer` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tarifaReferencia` | `xsd:decimal` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tarifa` | `xsd:decimal` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitPagadorFlete` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitIntermediarioFlete` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `mercaderiaFumigada` | `xsd:boolean` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `TransporteAutomotorSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitTransportista` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `dominio` | [`tns:NumeroDominio`](#numerodominio) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHoraPartida` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | [`tns:Kilometros`](#kilometros) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `codigoTurno` | [`tns:CodigoTurno`](#codigoturno) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitChofer` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `tarifa` | [`tns:Tarifa`](#tarifa) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitPagadorFlete` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitIntermediarioFlete` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `mercaderiaFumigada` | `xsd:boolean` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `TransporteFerroviariaDGSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitTransportista` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitTransportistaTramo2` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroVagon` | [`tns:NumeroVagon`](#numerovagon) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `nroPrecinto` | [`tns:NumeroPrecinto`](#numeroprecinto) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |
| `nroOperativo` | [`tns:NumeroOperativo`](#numerooperativo) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ramal` | [`tns:Ramal`](#ramal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHoraPartidaTren` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | [`tns:Kilometros`](#kilometros) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitConductor` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitConductorTramo2` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tarifa` | [`tns:Tarifa`](#tarifa) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitPagadorFlete` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitIntermediarioFlete` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `TransporteFerroviariaModificaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `ramal` | [`tns:Ramal`](#ramal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHoraPartidaTren` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | [`tns:Kilometros`](#kilometros) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |

### `TransporteFerroviariaRespuesta`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitTransportista` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitTransportistaTramo2` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroVagon` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroPrecinto` | `xsd:string` | 0 | unbounded | false | No; el esquema no valida reglas de negocio |  |
| `nroOperativo` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `ramal` | [`tns:Ramal`](#ramal) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `fechaHoraPartidaTren` | `xsd:dateTime` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | `xsd:int` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitPagadorFlete` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitConductor` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitConductorTramo2` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `tarifa` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `cuitIntermediarioFlete` | `xsd:long` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `mercaderiaFumigada` | `xsd:boolean` | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |

### `TransporteFerroviariaSolicitud`

| Campo | Tipo XSD | minOccurs | maxOccurs | nillable | Requerido por XSD | Documentación XSD |
|---|---|---:|---:|---|---|---|
| `cuitTransportista` | [`tns:CUIT`](#cuit) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitTransportistaTramo2` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `nroVagon` | [`tns:NumeroVagon`](#numerovagon) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `nroPrecinto` | [`tns:NumeroPrecinto`](#numeroprecinto) | 1 | unbounded | false | Sí; el esquema no valida reglas de negocio |  |
| `nroOperativo` | [`tns:NumeroOperativo`](#numerooperativo) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `ramal` | [`tns:Ramal`](#ramal) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `fechaHoraPartidaTren` | `xsd:dateTime` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `kmRecorrer` | [`tns:Kilometros`](#kilometros) | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
| `cuitPagadorFlete` | [`tns:CUIT`](#cuit) | 0 | 1 | false | No; el esquema no valida reglas de negocio |  |
| `mercaderiaFumigada` | `xsd:boolean` | 1 | 1 | false | Sí; el esquema no valida reglas de negocio |  |
