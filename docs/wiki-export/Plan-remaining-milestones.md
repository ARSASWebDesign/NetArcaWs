<!-- Source: docs/plans/remaining-milestones.md. Generated wiki mirror; edit the repository source. -->

# Plan de cierre de hitos 2–7

Fecha de investigación y snapshots: 2026-10-06. Hitos 2–6 están completados y
verificados en código, contratos y documentación; Hito 7 permanece pendiente de
revisión exhaustiva; la documentación local ya está publicada en la wiki. Las secciones 2–6 conservan el
alcance técnico y criterios de verificación que condujeron al cierre.
Al corte actual existen WSAA, certificados en memoria, contexto tenant para
WSAA y las fachadas autenticadas, health checks, CLI de certificados, transporte
SOAP común, cálculo decimal explícito y un diario/orquestador fiscal. Las
solicitudes/respuestas 1:1 de las siete superficies están generadas, y
`SafeInvoiceService` implementa emisión unitaria y consulta conservadora para
CAE de WSFEv1/WSFEXv1/WSMTXCA. La build Release terminó con 0 warnings/errores;
la suite tuvo 201 casos, 198 aprobados y 3 omitidos. Se verificaron QName/actions
de las 81 operaciones contra WSDL y 166 tipos raíz XML en round-trip; 22 tipos
de contrato con campos `DateTime` preservan el valor. En QA real pasaron 7 Dummies y se omitieron 2 pruebas autenticadas por
falta de certificados. No hay emisión batch durable, autoasignación de número,
auto-reenvío de estados inciertos ni worker de reconciliación.

| Fachada/contrato | Operaciones actuales |
|---|---:|
| WSFEv1 | 22 |
| WSFEXv1 | 19 |
| WSMTXCA | 27 |
| Padrón A4 | 2 |
| Constancia, ruta histórica A5 | 5 |
| Padrón A10 | 2 |
| Padrón A13 | 4 |
| **Total de estos contratos** | **81** |

El alcance funcional para Hitos 2–5 son **todas las operaciones de los contratos
ARCA actuales** para WSFEv1, WSFEXv1, WSMTXCA, Padrón A4, Constancia (ruta A5
histórica), A10 y A13, además de WSAA ya cubierto. El árbol upstream y sus
helpers sirven para entender modelos y flujos, pero no limitan el contrato ni
prevalecen sobre manual/WSDL actual. Los XML de homologación/producción
descargados el 2026-10-06 y los manuales enlazados en
[`healthchecks-contracts.md`](Referencia-healthchecks-contracts) constituyen
la base versionada; validar nuevamente el WSDL desplegado y el manual vigente
antes de fijar cada DTO/action o release.

## Criterios que aplican a todos los servicios

- Las solicitudes y respuestas públicas SOAP se modelan una a una respecto del
  XSD/WSDL elegido, conservando tipos y campos presentes en ese contrato (incluidos
  los primitivos `double` donde el XSD de WSFE los define); no se usa WCF ni se
  ocultan diferencias en un DTO genérico. Las capas de dominio/cálculo pueden
  ofrecer alternativas `decimal` explícitas sin alterar el tipo de cable.
- Cada cliente recibe un `ArcaTenantContext` resuelto y autorizado por la
  aplicación. La misma operación usa certificado, CUIT representada y ambiente
  del contexto; no muta opciones globales ni habilita failover entre ambientes.
  La CUIT representada puede diferir del titular del certificado por delegación
  ARCA. La biblioteca no autoriza tenants.
- La unicidad fiscal sigue siendo
  `(ambiente, CUIT emisora, punto de venta, tipo, número)`, sin `tenantId`.
  Tenant limita acceso y visibilidad; no permite duplicar una identidad fiscal.
- Cada llamada tiene request/response SOAP de prueba independiente, fixtures
  redactados o sintéticos, validación de namespaces, SOAPAction, faults y
  cancelación. Tests locales no necesitan certificados ni datos reales.
- El `SoapTransport` común no reintenta. `InvoiceCoordinator` y
  `SqliteInvoiceJournal` ya guardan operaciones, leases/versiones y resultados
  inciertos en un archivo SQLite local; admiten procesos que comparten ese mismo
  archivo en un único host, pero no NFS ni hosts replicados. Para despliegue
  multi-host se requiere un `IInvoiceJournal` transaccional compartido.
  `SafeInvoiceService` cubre CAE unitario y consulta exacta de reconciliación
  para tres protocolos; no proporciona autoasignación de número, lote durable
  por item, auto-reenvío ni garantía de emisión exactamente una vez.
- `TaxCalculator` aporta aritmética decimal explícita, no reglas completas de
  negocio fiscal, catálogo de tasas ni validación oficial por servicio.
- Los manuales oficiales vigentes y los WSDL desplegados prevalecen sobre el
  cliente Python histórico si difieren. El inventario upstream es un mapa de
  comportamiento para investigar, no una especificación de contrato fiscal.

### Infraestructura compartida ya implementada

`ISoapTransport`/`SoapTransport` proporciona SOAP 1.1 tipado, endpoint HTTPS,
action, límite de respuesta, errores estructurados y cancelación; no autentica,
clasifica errores fiscales ni reintenta. `IArcaTicketProvider` obtiene el ticket
WSAA a partir del contexto tenant. `TaxCalculator` redondea AwayFromZero (hasta
seis decimales), calcula IVA usando una tasa que ya eligió la aplicación,
descompone un bruto simple y suma componentes no negativos. No reemplaza reglas
particulares de WSFE ni el catálogo ARCA.

`InvoiceCoordinator` y `IInvoiceJournal` implementan persistencia/versionado y
coordinación de estados inciertos; `SqliteInvoiceJournal` permite varios
procesos en un mismo host que compartan el mismo archivo local. No está destinado
a NFS ni a hosts replicados. La extensión multi-host consiste en implementar
`IInvoiceJournal` sobre una base transaccional compartida. Fachadas, orquestador
CAE unitario, suite y checks de contratos están verificados; las operaciones
autenticadas reales aún requieren certificados de homologación.

## Hito 2 — WSFEv1

Estado: completado y verificado el 2026-10-06. Las 22 operaciones SOAP,
QName/SOAPAction y suite local pasaron; homologación fiscal autenticada no ejecutada.

### Cobertura de referencia upstream

`wsfev1.py` (rama `main` consultada el 2026-10-06; clase `WSFEv1`) organiza:

| Grupo | Métodos públicos/herramientas observados |
|---|---|
| Factura de entrada y detalle | `CrearFactura`, `AgregarIva`, `AgregarTributo`, `AgregarCmpAsoc`, `AgregarOpcional`, `AgregarComprador`, `AgregarPeriodoComprobantesAsociados`, `AgregarActividad`, `EstablecerCampoFactura`, `ObtenerCampoFactura` |
| CAE y consultas | `CAESolicitar`, `CompUltimoAutorizado`, `CompConsultar`, `CompTotXRequest` |
| CAEA | `CAEASolicitar`, `CAEAConsultar`, `CAEARegInformativo`, `CAEASinMovimientoInformar` |
| Lote “X” | `CAESolicitarX`, `IniciarFacturasX`, `AgregarFacturaX`, `LeerFacturaX` |
| Parámetros | `ParamGetTiposCbte`, `ParamGetTiposConcepto`, `ParamGetTiposDoc`, `ParamGetTiposIva`, `ParamGetTiposMonedas`, `ParamGetTiposOpcional`, `ParamGetTiposTributos`, `ParamGetTiposPaises`, `ParamGetCotizacion`, `ParamGetPtosVenta`, `ParamGetActividades`, `ParamGetCondicionIvaReceptor` |
| Infraestructura/compatibilidad | `Dummy`, `Conectar`, `SetTicketAcceso`, `SetParametros`, `GetParametro`, `AnalizarXml`, `ObtenerTagXml`, `LoadTestXML`, `DebugLog` |

Los métodos de armado (`CrearFactura` y `Agregar*`) construyen estado local de
una factura; la capa C# deberá reemplazarlos por modelos inmutables/componibles.
La salida del wrapper Python incluye propiedades mutables y estado de la última
operación; ese patrón no debe convertirse en estado compartido entre llamadas.
El WSDL de homologación consultado expone 22 operaciones SOAP con actions
`http://ar.gov.afip.dif.FEV1/{operation}`: `FECAESolicitar`,
`FECompTotXRequest`, `FEDummy`, `FECompUltimoAutorizado`, `FECompConsultar`,
`FECAEARegInformativo`, `FECAEASolicitar`, `FECAEASinMovimientoConsultar`,
`FECAEASinMovimientoInformar`, `FECAEAConsultar`, `FEParamGetCotizacion`,
`FEParamGetTiposTributos`, `FEParamGetTiposMonedas`, `FEParamGetTiposIva`,
`FEParamGetTiposOpcional`, `FEParamGetTiposConcepto`, `FEParamGetPtosVenta`,
`FEParamGetTiposCbte`, `FEParamGetCondicionIvaReceptor`, `FEParamGetTiposDoc`,
`FEParamGetTiposPaises` y `FEParamGetActividades`. Verificar namespace, endpoint,
SOAPAction y campos con el WSDL/manual vigente antes de fijar contrato C#.
El grupo precedente es la superficie SOAP completa del WSDL descargado, no el
inventario de wrappers Python. Las operaciones C# deben estar una a una
disponibles bajo tipos solicitud/respuesta seguros; `FEDummy` es infraestructura
y `FECAESolicitar`/CAEA son emisión. El catálogo de parámetros no es un
calculador fiscal.

### Entregables y pruebas

1. Cliente WSFEv1 autenticado con `ArcaTenantContext`, endpoints por ambiente,
   DTOs para solicitudes/respuestas, warnings/observaciones y errores por
   comprobante.
2. Implementar los métodos de CAE, consulta exacta, último autorizado, catálogos,
   CAEA y familia de solicitudes múltiples descrita por el manual. Distinguir
   `Resultado` de cabecera y detalle: una respuesta parcial no convierte todos
   sus comprobantes en éxito o rechazo.
3. Reglas y validaciones financieras: límites de escala/rango, coherencia entre
   total, neto, exento, no gravado, tributos e IVA; monedas/cotización, concepto y
   fechas del comprobante/servicio. No recalcular silenciosamente lo enviado;
   validar importes con aritmética decimal y redondeo explícito y versionado.
4. Aplicar [ADR 0001](ADR-0001-safe-invoice-retries) antes de habilitar
   emisión de alto nivel: identidad fiscal durable, hash del payload canónico,
   estados `Prepared`/`Submitting`/`Unknown`/`Reconciling`/terminales, consultas
   exactas y ausencia verificable. Consultar `FECompUltimoAutorizado` no
   demuestra el resultado de una solicitud específica.
5. Pruebas con importes límite, redondeo, varios comprobantes y resultados
   parciales, CAEA, timeouts antes/durante/después del envío, consulta no
   concluyente, conflictos, dos workers y aislamiento entre tenants que
   representen una misma CUIT.

La solicitud batch es una superficie de bajo nivel que debe preservarse; el
orquestador seguro puede iniciar con un comprobante por envío. El manual vigente
define límites y estructuras concretas, a fijar en fixtures y docs antes de
codificar los DTO.

## Hito 3 — WSFEXv1

Estado: completado y verificado el 2026-10-06. Las 19 operaciones SOAP,
QName/SOAPAction y suite local pasaron; homologación fiscal autenticada no ejecutada.

### Cobertura de referencia upstream

`wsfexv1.py` (`WSFEXv1`) ofrece armado de factura de exportación, `AgregarItem`,
`AgregarPermiso`, `AgregarCmpAsoc`, `AgregarActividad`, `Authorize(id)`,
`GetCMP(tipo, punto, número)`, `GetLastCMP`, `GetLastID`, `Dummy` y catálogos de
moneda/cotización, tipo de comprobante, tipo de exportación, idioma, unidad,
incoterms, destino-país/CUIT y puntos de venta. También publica helpers de
compatibilidad/estado como `LoadTestXML`, `AnalizarXml`, `ObtenerTagXml`,
`SetTicketAcceso`, `SetParametros`, `GetParametro`, `Conectar`, `DebugLog`.
El WSDL de homologación tiene 19 operaciones: `FEXAuthorize`, `FEXGetCMP`,
`FEXGetPARAM_Cbte_Tipo`, `FEXGetPARAM_Tipo_Expo`, `FEXGetPARAM_Incoterms`,
`FEXGetPARAM_Idiomas`, `FEXGetPARAM_UMed`, `FEXGetPARAM_DST_pais`,
`FEXGetPARAM_DST_CUIT`, `FEXGetPARAM_MON`, `FEXGetPARAM_MON_CON_COTIZACION`,
`FEXGetLast_CMP`, `FEXDummy`, `FEXGetPARAM_Ctz`, `FEXGetLast_ID`,
`FEXGetPARAM_PtoVenta`, `FEXCheck_Permiso`, `FEXGetPARAM_Opcionales` y
`FEXGetPARAM_Actividades`; cada action es `{targetNamespace}/{operation}`.
Publicar y probar las 19 sin omitir `FEXCheck_Permiso` ni
`FEXGetPARAM_Opcionales`, que no aparecen como método destacado de la fachada
Python. Los modelos públicos deben ser tipados y seguros aunque el wrapper
upstream reciba mapas/diccionarios.

`Authorize` transmite `Cmp.Id`; eso es una regla de reintento documentada por el
manual. No equivale a una clave idempotente arbitraria ni permite reutilizar el
identificador con payload distinto.

### Entregables y pruebas

1. DTO exportadores con datos de comprador extranjero, tipo de exportación,
   destino, moneda, pagos, incoterms, ítems, permisos aduaneros, comprobantes
   relacionados y actividades; validación por tipo de comprobante.
2. `FEXAuthorize`, `FEXGetCMP`, `FEXGetLast_CMP`, `FEXGetLast_ID`, `Dummy` y
   todos los métodos de parámetros upstream/actuales; errores y eventos siguen
   siendo listas tipadas, no texto concatenado.
3. Política de importes/tipo de cambio explícita, sin `double`; preservar escala
   y monto serializado y verificar las reglas del manual. Los casos de moneda
   extranjera no se validan mediante una conversión inventada por la biblioteca.
4. Implementar reconciliación conforme ADR 0001: antes de enviar, persistir
   payload, identidad fiscal y `Cmp.Id`; tras incertidumbre consultar el
   comprobante exacto; si se usa la repetición de WSFEX, conservar el mismo ID y
   request y reconocer `Reproceso`.
5. Probar requests con y sin permisos/asociados/actividades, respuestas `A`/`R`,
   errores/eventos, consulta inexistente, reintento con mismo ID y rechazo al
   mutar payload bajo el mismo ID, usando SOAP capturado/sintético.

## Hito 4 — WSMTXCA (factura electrónica con detalle)

Estado: completado y verificado el 2026-10-06. Las 27 operaciones SOAP,
QName/SOAPAction y suite local pasaron; homologación fiscal autenticada no ejecutada.

WSMTXCA no es sinónimo de WSFCE ni de Factura de Crédito Electrónica MiPyME. El
upstream `wsmtx.py` documenta `WSMTXCA` para emisión de mercado interno con
codificación/detalle de productos (RG 2904) y CAE/CAEA; el manual ARCA determina
el alcance y versión aplicables. El código upstream advierte literalmente que
neto e IVA no se calculan en el agregador de ítems: los importes deben llegar
calculados. Un cliente .NET que añada cálculo constituye una capacidad propia y
debe documentar su fórmula y pruebas, no una equivalencia PyAfipWs.

### Cobertura de referencia upstream

`wsmtx.py` agrupa factura/campos y detalle (`CrearFactura`, `EstablecerCampoFactura`,
`AgregarIva`, `AgregarItem`, `AgregarTributo`, `AgregarCmpAsoc`,
`EstablecerCampoItem`, `AgregarOpcional`, `AgregarPeriodoComprobantesAsociados`,
`AgregarActividad`); autorización (`AutorizarComprobante`, `CAESolicitar`,
`AutorizarAjusteIVA`); CAEA (`SolicitarCAEA`, `ConsultarCAEA`,
`ConsultarCAEAEntreFechas`, `InformarComprobanteCAEA`, `InformarAjusteIVACAEA`,
`InformarCAEANoUtilizado`, `InformarCAEANoUtilizadoPtoVta`); consultas
(`ConsultarUltimoComprobanteAutorizado`, `CompUltimoAutorizado`,
`ConsultarPtosVtaCAEANoInformados`, `ConsultarComprobante`); catálogos (tipos de
comprobante/documento, alícuotas y condición IVA, moneda, unidades, actividades,
tributos, datos adicionales, cotización y puntos de venta CAE/CAEA), más
`Dummy` y helpers de parse/configuración.

El WSDL de producción descargado el 2026-10-06 declara 27 operaciones con
SOAPAction `{http://impl.service.wsmtxca.afip.gov.ar/service/}{operation}`:
`dummy`, `autorizarComprobante`, `solicitarCAEA`, `informarComprobanteCAEA`,
`consultarUltimoComprobanteAutorizado`, `consultarComprobante`,
`consultarTiposComprobante`, `consultarTiposDocumento`, `consultarAlicuotasIVA`,
`consultarCondicionesIVA`, `consultarCondicionesIVAReceptor`, `consultarMonedas`,
`consultarCotizacionMoneda`, `consultarUnidadesMedida`, `consultarPuntosVenta`,
`consultarPuntosVentaCAE`, `consultarPuntosVentaCAEA`,
`informarCAEANoUtilizado`, `consultarTiposTributo`,
`informarCAEANoUtilizadoPtoVta`, `consultarCAEA`,
`consultarPtosVtaCAEANoInformados`, `consultarCAEAEntreFechas`,
`autorizarAjusteIVA`, `informarAjusteIVACAEA`, `consultarTiposDatosAdicionales` y
`consultarActividadesVigentes`. El WSDL de homologación debe cotejar el binding,
endpoint y namespace antes de tomar el de producción como única copia de
contrato. El endpoint declarado por producción es
`https://serviciosjava.afip.gob.ar/wsmtxca/services/MTXCAService`; homologación
es `https://fwshomo.afip.gov.ar/wsmtxca/services/MTXCAService`.

### Entregables y pruebas

1. Contratos de producto con códigos de producto/mercadería, unidad, cantidad,
   precio, bonificación, alícuota y subtotal por línea; incluir ajustes de IVA,
   tributos, notas de crédito/débito, asociados, opcionales y períodos.
2. Implementar cada familia de `WSMTXCA` del manual efectivamente soportado. La
   autorización, consulta exacta, CAEA, informes de CAEA, ajuste IVA y catálogos
   deben tener respuesta/errores estructurados y estar ligados a tenant.
3. Definir por separado una API opcional de cálculo de líneas/totales solo si se
   puede expresar fielmente la regla oficial. Usar `decimal`, precisión,
   redondeo, acumulación, notas/ítems especiales y tolerancias fijadas por casos
   de manual; ninguna implementación debe inferir una alícuota o redondear en
   silencio. Si no hay regla suficiente para un caso, exigir monto explícito y
   validarlo, en vez de fingir que se calculó.
4. Reconciliar tras error de comunicación con `consultarComprobante` exacto;
   reenviar únicamente cuando el manual/resultado inequívoco confirme ausencia,
   siempre mismo número y mismo payload. Un `consultarUltimoComprobanteAutorizado`
   no atribuye contenido a una operación incierta.
5. Probar totales por servicio con ejemplos oficiales, precisión y moneda,
   códigos de producto, líneas/ítems especiales, CAE y CAEA, error/timeout y
   conciliación de número reservado.

## Hito 5 — Padrón A4, Constancia (A5 histórico), A10 y A13

Estado: completado y verificado el 2026-10-06. Las 13 operaciones de los cuatro
contratos y la suite local pasaron; QA real confirmó sus cuatro probes `Dummy`.

`ws_sr_padron.py` upstream cubre A4 y A5; A5 hereda el cliente A4, cambia la ruta
de WSDL a `personaServiceA5` y consulta `getPersona`, pero su identidad de
servicio vigente es `ws_sr_constancia_inscripcion`: es Constancia de Inscripción,
no debe presentarse como un Padrón A5 vigente. Ese módulo Python no contiene
clientes A10 ni A13 en la revisión consultada. Los endpoints, modelos y
operaciones de A10/A13 deben derivarse de sus manuales/WSDL oficiales, sin
atribuirles métodos que no existan en ese upstream.

Los contratos WSDL revisados exponen: A4 `dummy`/`getPersona`; A5/Constancia
`dummy`, `getPersona`, `getPersona_v2`, `getPersonaList` y `getPersonaList_v2`;
A10 `dummy`/`getPersona`; A13 `dummy`, `getIdPersonaListByDocumento`,
`getPersona` y `getPersonaV2`. Los actions SOAP del binding Padrón son vacíos.
Los hosts y namespaces varían entre WSDLs actuales: usar HTTPS aunque un WSDL
A10 histórico aún anuncie address HTTP. Registrar por contrato la URL, fecha,
host y fuente oficial; no normalizar todos los hosts sin verificar.
Los documentos públicos de cada cliente deben reflejar todas esas operaciones,
incluidos `getPersonaList`/`getPersonaList_v2` en A5 y
`getIdPersonaListByDocumento` en A13. Los actions vacíos en estos bindings
siguen siendo un valor contractual, no un action sintetizado a partir del
nombre.

### Entregables y pruebas

1. Cliente independiente por operación/contrato publicado: A4, Constancia con
   endpoint histórico A5, A10 y A13. Confirmar el `service` WSAA vigente para
   cada uno, especialmente Constancia/A5, y documentar métodos nuevos/versión
   `getPersona_v2` y campos añadidos.
2. Modelos con campos generales, domicilio, actividad e impuestos por servicio;
   no aplastar extensiones de A5 (régimen general/monotributo/error por
   subsistema) en el esquema simple A4. Preservar extensiones/versiones sin
   perder campos desconocidos si el contrato lo permite.
3. Implementar operaciones de búsqueda adicionales que exponga cada manual
   (p. ej. por tipo/número de documento cuando exista), distinguir cero, una y
   varias personas, ausencia, autorización y error de negocio; no usar índices
   de listas como identificador.
4. `ArcaTenantContext` por operación; identidad solicitada puede ser CUIT, CUIL,
   DNI u otra clave conforme servicio, mientras que la CUIT representada del
   contexto es la credencial que accede al WS. No confundirlas.
5. Fixtures sintéticos por schema/version, respuestas con secciones opcionales,
   listas múltiples, domicilio fiscal/otros, errores parciales, identidad no
   encontrada, SOAP Fault, cancellation y ensayo de homologación opt-in que no
   envíe consultas a producción.

## Hito 6 — Documentación de producto y arquitectura

Estado: completado el 2026-10-06 en README, ARCHITECTURE, ADR y guías locales.

Integrar el inventario funcional con API XML docs y las guías operativas. Toda
afirmación de capacidad debe enlazar implementación y prueba. Publicar por
servicio tablas de operaciones, request/response/DTO, restricciones y manejo de
errores; enlazar el manual ARCA/versiones revisadas. Incluir ejemplos de
compilación con .NET 10, tenant confiable, contenidos de certificado en memoria,
health checks opt-in, CLI local y pruebas. Aclarar qué aspectos son extras del
port, cuáles están en PyAfipWs y cuáles no se portan.

Revisar instalación/paquetes, disponibilidad, operación multi-instancia,
protección de datos y producción con responsables antes de presentarlo como
guía lista para operar. README y `ARCHITECTURE.md` conservan el resumen de
entrada; el wiki contiene la guía de referencia. No duplicar decisiones: enlazar
ADR y agregar docs de API/servicio junto al lugar de verdad.

## Hito 7 — Wiki de cobertura completa

Mantener las fuentes Markdown versionadas en `docs/wiki/`; publicar la copia a
GitHub Wiki solo después de compilar, verificar tests, pack y ejemplos. El wiki
debe identificar commit de fuentes, hacer navegación completa y enlazar las
fuentes locales y oficiales.

| Tema del upstream / proyecto | Tratamiento en NetArcaWs |
|---|---|
| WSAA y certificado/CSR | Implementación .NET actual: documentar APIs/CLI y límites; atribución a PyAfipWs preservada |
| WSFEv1, WSFEXv1, WSMTXCA, Padrón | Implementados y verificados en Hitos 2–5; no mezclar MTXCA con WSFCE |
| WSBFE, WSCTG, WSLPG, WSLTV, WSLUM, WSLSP, WSCDC, WSCOC, WSCPE, WSREMAZUCAR, WSREMCARNE, WSREMHARINA, SIRE, FE Crédito, otros módulos listados por upstream | Inventario de páginas/código oficial upstream; estado explícito `pendiente/no portado` salvo implementación verificable |
| PyAfipWs helpers de tabla (`padron.py`, tablas fiscales), formatos XLS/CSV/TXT/XML/JSON/DBF, validadores, parsing y serialización | Describir usos upstream, decidir por funcionalidad qué admite APIs .NET futuras; no copiar formatos por semejanza superficial |
| PyAfipWs CLI, examples, `rece.py`, PyRece/PyFactura | Identificar flujos y dependencias; la CLI .NET actual solo prepara/inspecciona certificados y no emite o genera PDF |
| COM/OCX/DLL, registros Windows, wrappers a VB/VFP/Delphi/Java/PHP, instaladores | Python/upstream específico; no existe equivalencia .NET en alcance actual |
| ERP OpenERP/Tryton, ERP integrations, documentación de difusión/artículos, manuales de instalación/uso | Referenciar e inventariar; no afirmar que los addons, UI o flujos de distribución funcionen con NetArcaWs |
| Caché de TA, certificado/CSR y Dummies | Las capacidades también existen en PyAfipWs; documentar diseño y diferencia de implementación .NET, no atribuir originalidad exclusiva al port |
| Tests upstream, simulador, PDFs e informes | Enumerar recursos existentes y su compatibilidad/estado; no convertir fixtures específicos de Python en equivalencia de producción |

La lista anterior es semilla, no inventario exhaustivo. El cierre requiere recorrer
índice entero del wiki upstream, README/manual enlazado, árbol de módulos/scripts,
tests, ejemplos, instaladores, formatos y links de soporte. Cada entrada tendrá
fuente, revisión/fecha, tema, destino wiki, estado de adaptación y atribución.
Resumir con redacción original; no copiar páginas enteras. Ver
[alcance y checklist original](Plan-hito-7-wiki).

## Fuentes de investigación

- [PyAfipWs: README](https://github.com/reingart/pyafipws/blob/main/README.md),
  [wiki](https://github.com/reingart/pyafipws/wiki),
  [WSFEv1](https://github.com/reingart/pyafipws/blob/main/wsfev1.py),
  [WSFEXv1](https://github.com/reingart/pyafipws/blob/main/wsfexv1.py),
  [WSMTXCA (`wsmtx.py`)](https://github.com/reingart/pyafipws/blob/main/wsmtx.py),
  [Padrón A4/Constancia A5 (`ws_sr_padron.py`)](https://github.com/reingart/pyafipws/blob/main/ws_sr_padron.py).
- [ARCA: portal de ayuda y manuales oficiales de factura electrónica](https://arca.gob.ar/fe/ayuda/webservice.asp): lista WSFEv1 4.7, WSFEXv1 3.1.1 y WSMTXCA 0.25.8 al consultar esta página.
- Contratos SOAP actuales comprobados en snapshots locales fechados el
  2026-10-06: [WSFEv1 WSDL](https://wswhomo.afip.gov.ar/wsfev1/service.asmx?WSDL),
  [WSFEXv1 WSDL](https://wswhomo.afip.gov.ar/wsfexv1/service.asmx?WSDL),
  [WSMTXCA WSDL](https://fwshomo.afip.gov.ar/wsmtxca/services/MTXCAService?wsdl),
  [Padrón A4](https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA4?WSDL),
  [Constancia/A5](https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5?WSDL),
  [Padrón A10](https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA10?WSDL),
  [Padrón A13](https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA13?WSDL).
- Snapshots versionados de ese corte: `docs/reference/contracts/` contiene
  `wsfev1-homologation.wsdl`, `wsfexv1-homologation.wsdl`,
  `wsmtxca-production.wsdl` y los cuatro snapshots de Padrón A4/A5/A10/A13.
  `wsmtxca-homologation-invalid.wsdl` se conserva solo como prueba de que la
  respuesta descargada de homologación fue truncada/malformada; no es un
  contrato base ni fixture para generación. El WSDL de producción MTXCA es la
  base completa de sus 27 operaciones y namespace `...wsmtxca.afip.gov.ar...`;
  validar la homologación nuevamente antes de release.
- [ARCA: contratos de health checks y WSDL consultados](Referencia-healthchecks-contracts),
  [reintentos seguros](ADR-0001-safe-invoice-retries),
  [contexto tenant](ADR-0002-arca-tenant-context) y
  [certificados en memoria](ADR-0003-in-memory-certificates).

Antes de fijar requisitos/códigos fiscales por versión, volver a descargar los
manuales y actualizar fecha, WSDL y fixtures. La rama `main` upstream es mutable;
registrar el commit exacto cuando se cierre la matriz de adaptación.
