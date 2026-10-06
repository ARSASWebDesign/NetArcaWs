# Servicios fiscales y cobertura

Esta página separa contratos de infraestructura existentes de operaciones
fiscales. Un health check `Dummy` no implementa autorización de facturas; un
manual enlazado no significa que NetArcaWs ya tenga ese cliente.

El alcance de Hitos 2–5 es la superficie completa de las operaciones vigentes
en los contratos ARCA para WSFEv1, WSFEXv1, WSMTXCA, Padrón A4, Constancia
(endpoint A5 histórico), A10 y A13. El inventario de métodos de PyAfipWs ayuda a
entender modelos, agregadores y compatibilidad, pero no recorta ni reemplaza los
contratos oficiales. Las operaciones enumeradas debajo vienen de los WSDL
descargados el 2026-10-06; volver a verificar manual/versiones y bindings antes
de usar para un release.

En QA real, el 2026-10-06, una corrida de 9 casos respondió los siete probes
`Dummy` de estos servicios y omitió las dos pruebas autenticadas por falta de
certificados. La ejecución no acredita autorización fiscal.

| Servicio | Estado funcional al corte documental | Nota de contrato/alcance |
|---|---|---|
| WSAA | Implementado: autenticación, TA, firma, caché en proceso | Auth no autoriza automáticamente otros servicios ni usuarios |
| Transporte SOAP común | Implementado: `ISoapTransport`/`SoapTransport` | SOAP 1.1, límites/validación de XML y faults; sin autenticación fiscal ni reintentos |
| Cálculo decimal | Implementado como utilitario explícito | No es un motor tributario completo ni decide tasas/reglas ARCA |
| Diario/orquestación fiscal | Suite Release verificada; emisión unitaria y reconciliación | Autoriza CAE unitario WSFE/WSFEX/WSMTXCA; no auto-reenvío ni worker |
| WSFEv1 | Hito 2 verificado: 22 métodos; QName/action contra WSDL y suite | CAE/CAEA, consultas, parámetros; conserva tipos XSD (incluye `double`) |
| WSFEXv1 | Hito 3 verificado: 19 métodos; QName/action contra WSDL y suite | Exportación; `Cmp.Id` y reproceso son específicos del flujo |
| WSMTXCA | Hito 4 verificado: 27 métodos; QName/action contra WSDL y suite | Factura de mercado interno con detalle y CAE/CAEA; no es el ciclo integral WSFCE/MiPyME |
| Padrón A4 | Hito 5 verificado: 2 métodos | `dummy`/`getPersona`; `Dummy` no requiere ticket |
| Constancia, endpoint histórico A5 | Hito 5 verificado: 5 métodos | Ruta `personaServiceA5`; service WSAA `ws_sr_constancia_inscripcion` |
| Padrón A10 | Hito 5 verificado: 2 métodos | Contrato conforme a manual/WSDL actual; distinto de `ws_sr_padron.py` upstream |
| Padrón A13 | Hito 5 verificado: 4 métodos | Incluye `getIdPersonaListByDocumento`; distinto de `ws_sr_padron.py` upstream |
| Health checks | Implementados para endpoints SOAP declarados | Comprueban disponibilidad del servicio, no autorización fiscal |
| CLI certificados | Implementada para generar CSR e inspeccionar localmente | No emite certificado ni conecta con ARCA |

La suma de los siete contratos es 81 operaciones. Se verificaron QName/action
de cada método contra WSDL y 166 tipos raíz XML en round-trip en la suite; 22
tipos de contrato conservaron sus campos `DateTime`. Una
corrida real de QA respondió los siete probes `Dummy`; las dos pruebas
autenticadas se omitieron por falta de certificados. Esto no acredita
autorización fiscal. Hitos 2–6 se cerraron con build/suite local; Hito 7 sigue
abierto y el wiki remoto no se publicó. El
[plan de hitos 2–7](../plans/remaining-milestones.md) define las operaciones,
datos fiscales, cobertura de contratos y pruebas de cierre por servicio.

## Mapa del upstream investigado

PyAfipWs ofrece más que los módulos incluidos en el port. La revisión inicial del
README/wiki y fuentes `wsfev1.py`, `wsfexv1.py`, `wsmtx.py`, `ws_sr_padron.py`
se realizó el 2026-10-06. La rama `main` del upstream es mutable; registrar el
hash exacto al cerrar una matriz exhaustiva.

### Clientes y operaciones de interés

- `wsfev1.py`: armado de comprobantes; solicitud de CAE y consulta/último
  autorizado; CAEA (solicitar, consultar, informar, sin movimiento); solicitudes
  masivas `X`; búsqueda de parámetros; `Dummy`; helpers de lote, campos, parse y
  compatibilidad. El WSDL consultado expone 22 operaciones SOAP: `FECAESolicitar`,
  `FECompTotXRequest`, `FEDummy`, `FECompUltimoAutorizado`, `FECompConsultar`,
  `FECAEARegInformativo`, `FECAEASolicitar`, `FECAEASinMovimientoConsultar`,
  `FECAEASinMovimientoInformar`, `FECAEAConsultar`, `FEParamGetCotizacion`,
  `FEParamGetTiposTributos`, `FEParamGetTiposMonedas`, `FEParamGetTiposIva`,
  `FEParamGetTiposOpcional`, `FEParamGetTiposConcepto`, `FEParamGetPtosVenta`,
  `FEParamGetTiposCbte`, `FEParamGetCondicionIvaReceptor`, `FEParamGetTiposDoc`,
  `FEParamGetTiposPaises`, `FEParamGetActividades`.
- `wsfexv1.py`: facturas de exportación y detalle; permisos, comprobantes
  asociados y actividades; `Authorize`/`GetCMP`, último ID y comprobante; listas
  de parámetros de exportación; `Dummy` y helpers. El WSDL consultado incluye
  19 operations: `FEXAuthorize`, `FEXGetCMP`, `FEXGetPARAM_Cbte_Tipo`,
  `FEXGetPARAM_Tipo_Expo`, `FEXGetPARAM_Incoterms`, `FEXGetPARAM_Idiomas`,
  `FEXGetPARAM_UMed`, `FEXGetPARAM_DST_pais`, `FEXGetPARAM_DST_CUIT`,
  `FEXGetPARAM_MON`, `FEXGetPARAM_MON_CON_COTIZACION`, `FEXGetLast_CMP`,
  `FEXDummy`, `FEXGetPARAM_Ctz`, `FEXGetLast_ID`, `FEXGetPARAM_PtoVenta`,
  `FEXCheck_Permiso`, `FEXGetPARAM_Opcionales`, `FEXGetPARAM_Actividades`.
- `wsmtx.py`: WSMTXCA con detalle/codificación de productos, IVA e ítems; CAE,
  ajuste IVA, CAEA e informes; consulta exacta y por tipo; catálogos y `Dummy`.
  El agregador upstream dice que neto/IVA deben venir calculados; una calculadora
  .NET sería extensión propia y requiere especificación separada. El WSDL actual
  incluye 27 operaciones: `dummy`, `autorizarComprobante`, `solicitarCAEA`,
  `informarComprobanteCAEA`, `consultarUltimoComprobanteAutorizado`,
  `consultarComprobante`, `consultarTiposComprobante`, `consultarTiposDocumento`,
  `consultarAlicuotasIVA`, `consultarCondicionesIVA`,
  `consultarCondicionesIVAReceptor`, `consultarMonedas`,
  `consultarCotizacionMoneda`, `consultarUnidadesMedida`, `consultarPuntosVenta`,
  `consultarPuntosVentaCAE`, `consultarPuntosVentaCAEA`,
  `informarCAEANoUtilizado`, `consultarTiposTributo`,
  `informarCAEANoUtilizadoPtoVta`, `consultarCAEA`,
  `consultarPtosVtaCAEANoInformados`, `consultarCAEAEntreFechas`,
  `autorizarAjusteIVA`, `informarAjusteIVACAEA`, `consultarTiposDatosAdicionales`,
  `consultarActividadesVigentes`.
- `ws_sr_padron.py`: consultas A4 y Constancia A5 histórica. A5 hereda A4 pero
  devuelve distintas secciones. El módulo no contiene A10/A13 en la revisión;
  esos servicios se documentan desde sus manuales oficiales. Los WSDLs revisados
  declaran A4 `dummy/getPersona`, A5/Constancia `dummy`, `getPersona`,
  `getPersona_v2`, `getPersonaList`, `getPersonaList_v2`; A10 `dummy/getPersona`;
  A13 `dummy`, `getIdPersonaListByDocumento`, `getPersona`, `getPersonaV2`. Los
  actions del binding son vacíos. Confirmar host/HTTPS del WSDL concreto; A10
  histórico anuncia HTTP en el WSDL aunque el manual exige HTTPS.

El índice upstream también enumera WSAA, WSBFE, WSCTG, WSDIGDEP, WSCOC, WSLPG,
WSLTV, WSLUM, WSLSP, WSCDC, COT de ARBA, trazabilidad ANMAT/RENPRE/SENASA y
formatos/controles fiscales. Otros archivos del repositorio incluyen WSCPE,
WSFE Crédito, SIRE, liquidaciones y servicios adicionales; se requiere un
inventario de árbol, ejemplos y tests antes de considerar esa cobertura
exhaustiva.

El wiki upstream describe asimismo PyRece/PyFactura, salida PDF, TXT/CSV/DBF/
XML/JSON, wrappers COM/OCX/DLL, instaladores y conexiones a ERP como OpenERP y
Tryton. Son recursos y flujos existentes en el ecosistema Python, no funciones
de este paquete .NET. La generación de CSR, la CLI, los Dummies y el caché
también existen en PyAfipWs; NetArcaWs cambia su implementación, no reclama que
esas capacidades sean exclusivas del port.

## Fuentes

- [README de PyAfipWs](https://github.com/reingart/pyafipws/blob/main/README.md)
- [Wiki de PyAfipWs](https://github.com/reingart/pyafipws/wiki)
- [`wsfev1.py`](https://github.com/reingart/pyafipws/blob/main/wsfev1.py),
  [`wsfexv1.py`](https://github.com/reingart/pyafipws/blob/main/wsfexv1.py),
  [`wsmtx.py`](https://github.com/reingart/pyafipws/blob/main/wsmtx.py),
  [`ws_sr_padron.py`](https://github.com/reingart/pyafipws/blob/main/ws_sr_padron.py)
- [Manuales de servicios de factura electrónica de ARCA](https://arca.gob.ar/fe/ayuda/webservice.asp)
- [Contratos de health checks y endpoints del repositorio](../reference/healthchecks-contracts.md)
- Snapshots XML por protocolo en [`docs/reference/contracts`](../reference/contracts).
  El WSDL de homologación MTXCA guardado allí es un documento truncado y no se
  usa para generar tipos; la copia de producción es la fuente completa para
  reconstruir el binding de las 27 operaciones.
