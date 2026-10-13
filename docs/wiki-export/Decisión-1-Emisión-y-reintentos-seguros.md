<!-- Source: docs/adr/0001-safe-invoice-retries.md. Generated wiki mirror; edit the repository source. -->

# ADR 0001: Emisión durable y reconciliación conservadora

- Estado: implementación y suite local verificadas; matriz autenticada local de 18 escenarios completada; la evidencia no acredita homologación de toda la biblioteca
- Fecha de investigación: 2026-10-06
- Alcance: operaciones unitarias de autorización CAE en WSFEv1, WSFEXv1 y WSMTXCA
- Fuera de alcance: emisión batch durable, asignación automática de numeración, reenvíos automáticos, scheduler distribuido y garantía de exactamente una vez

## Contexto

Una solicitud puede llegar a ARCA y procesarse aunque el cliente no reciba la respuesta. Tras un timeout, corte de conexión o cancelación, el resultado puede ser incierto. Asignar otro número o reenviar sin una decisión fundada puede producir duplicados, errores de correlatividad o pérdida del resultado original. Los servicios no definen una garantía universal de exactamente una vez ni una ventana común de consistencia de lectura.

## Decisión implementada

Las operaciones conservan un payload tipado congelado, una versión de serialización/canonicalización, el tenant, servicio y ambiente, identidad fiscal, hash, estado, respuesta y lease/versionado. La biblioteca no asigna números: la aplicación entrega el número dentro del request. La clave fiscal única es `(ambiente, CUIT, punto de venta, tipo, número)`, sin `tenantId` ni nombre de protocolo. Además, no se permite preparar otro comprobante de la misma serie `(ambiente, CUIT, punto de venta, tipo)` mientras una operación anterior de esa serie siga sin estado terminal `Authorized` o `Rejected`.

La clave de idempotencia `(tenantId, key)` ligada al mismo request devuelve la operación ya guardada y no envía otra llamada. Reutilizarla con otro payload, versión canónica o identidad conflictúa. Una identidad fiscal ocupada por otro tenant/servicio también conflictúa. Las aplicaciones deben guardar sus claves y no reutilizarlas para un comprobante distinto.

El canonicalizador vigente almacena una versión explícita (`CanonicalVersion`) y una representación XML tipada del payload fiscal antes de insertar los campos WSAA. El hash SHA-256 se calcula sobre esos bytes persistidos. Token, firma y otros campos efímeros no se incluyen en esa entrada. No se debe cambiar la versión o representación sin migración y pruebas de compatibilidad del diario.

`SafeInvoiceService` ofrece:

- `AuthorizeWsfeAsync(tenant, key, request)` para un único comprobante CAE con `CantReg = 1`, un detalle, `CbteDesde = CbteHasta`, fecha de emisión y tasa de cambio explícitas.
- `AuthorizeWsfexAsync(tenant, key, request)` para una factura con `Id` positivo, fecha y cotización explícitas.
- `AuthorizeWsmtxcaAsync(tenant, key, request)` para CAE, con fecha/cotización explícitas y sin campos de autorización ya completados.
- `ReconcileAsync(tenant, key)` para consultar el mismo comprobante con el adaptador del servicio guardado y comparar la respuesta con el payload fiscal.

`ResumeAsync(tenant, key)` recupera una operación persistida. Solo envía el snapshot inmutable cuando su estado actual es `Prepared`; en cualquier otro estado ejecuta reconciliación. Esto cubre una caída entre la preparación durable y el envío, sin reintentar automáticamente un envío incierto. `Prepared` no significa que ARCA haya recibido una solicitud.

Las sobrecargas `ReviseRejectedAsync(tenant, key, expectedVersion, replacement)` permiten preparar explícitamente una corrección para una operación cuya versión actual está en `Rejected`. El diario exige preservar tenant, clave, servicio, identidad fiscal y, cuando existe, ID remoto de la solicitud; registra el rechazo anterior en un historial inmutable y deja el nuevo snapshot en `Prepared`. La revisión no envía. La aplicación debe inspeccionar la corrección y llamar luego a `ResumeAsync` de forma explícita. Las revisiones son optimistas por versión y no permiten editar un `Unknown`, `Conflict` o una versión ya cambiada.

La CUIT representada y el ambiente de cada contexto se comprueban contra la operación persistida al reconciliar. La autorización de tenant sigue siendo responsabilidad de la aplicación. Los requests incluyen número/tipo/punto de venta proporcionados por quien llama; no hay asignador automático.

Al enviar, solo una respuesta completa, correlacionada con la solicitud y con un CAE válido termina en `Authorized`. `Rejected` exige el resultado de rechazo en el detalle correspondiente y correlacionado, sin CAE. Las respuestas incompletas o ambiguas quedan `Unknown`. En la llamada de autorización, una respuesta mal correlacionada también queda `Unknown`; durante reconciliación, una discrepancia fiscal frente al comprobante remoto se registra como `Conflict`. Un error de transporte se guarda como `Unknown` y se propaga al caller. El XML de respuesta y los datos fiscales se guardan en el diario y requieren protección de acceso, retención y copias.

WSMTXCA devuelve una estructura distinta para algunos rechazos: `Resultado = R`, un `ArrayErrores` no vacío y ausencia de `ComprobanteResponse` se clasifica como `Rejected` de acuerdo con el manual. Para `A`/`O`, el adaptador exige respuesta de comprobante correlacionada y CAE válido antes de guardar `Authorized`.

### Excepciones explícitas: validaciones terminales WSFEX 1550 y 1673

El 12 de octubre de 2026 se aprobó una excepción acotada para el rechazo de `Permiso_existente` omitido. El manual WSFEX describe la cancelación de la ejecución ante anomalías y asigna 1550 a este campo. La respuesta original observada exige enviar el tag aun cuando su valor sea vacío; el WSDL permite omitirlo, por lo que la validación de negocio es más estricta que el XSD.

Solo una respuesta directa de `FEXAuthorize`, con 1550, mensaje confirmatorio de tag omitido, ausencia de `FEXResultAuth` y snapshot aplicable que realmente omitió el campo, puede clasificarse como `Rejected` por esta excepción. Otros errores, mensajes ausentes o diferentes, un tag ya presente o un resultado de autorización contradictorio conservan incertidumbre. `FEXGetCMP` 1020 nunca acredita rechazo ni ausencia segura y no habilita reenvíos. Un resultado de consulta vacío o con identidad incompleta conserva `Unknown`; solo un registro completo permite comparar el contenido fiscal y decidir si existe un conflicto.

Para una operación anterior incierta, la adjudicación requiere una llamada explícita con la respuesta original íntegra y su snapshot original idéntico al actual, contexto autorizado, versión esperada y metadatos de operador, origen y motivo. El diario valida la misma excepción, rechaza trabajo activo y conserva atómicamente evidencia inmutable, la operación previa y el rechazo adjudicado. No modifica payload, identidad, número ni Id remoto y no llama a ARCA. La aplicación es responsable de autenticar al operador y comprobar la procedencia del archivo de evidencia; el clasificador no verifica firmas de autor de SOAP. La evidencia XML retenida por el adaptador es la serialización de la respuesta tipada, no una captura byte por byte del intercambio HTTP/SOAP. Se preserva ese snapshot íntegro; no se reconstruyen nodos desconocidos descartados al deserializar. La adjudicación falla ante indicios positivos de identidad, incluso parciales, CAE, estructuras duplicadas o nombres críticos en namespaces inesperados. La última respuesta conservada también debe ser una validación terminal allowlisted compatible con el snapshot (1550 o 1673 bajo sus predicados respectivos) o una consulta 1020 con resultado vacío y estructura reconocida. El 1020 solo evita contradecir la evidencia original; por sí mismo nunca permite adjudicar ni reenviar.

La capacidad opcional `IInvoiceRejectionAdjudicationJournal` se implementa primero en `SqliteInvoiceJournal` y en el wrapper protegido de homologación. Los providers sin esta capacidad, incluido el diario EF Core en esta etapa, rechazan la adjudicación explícitamente. No se modifica su esquema ni se presume soporte. SQLite incorpora su ledger de auditoría de forma aditiva al abrir la base y conserva los datos existentes.

Después del rechazo 1550 adjudicado, `ReviseRejectedAsync` sigue siendo una acción explícita y versionada: prepara el tag vacío con la misma identidad e Id remoto, preserva el historial y no envía. Solo una llamada posterior explícita puede reanudar ese `Prepared`. No se reclasifican operaciones antiguas automáticamente al consultar o reanudar, ni se cambia el número para evitar una operación pendiente.

#### 1673: `Fecha_pago` informado en una nota de exportación

El manual WSFEX vigente establece que `Fecha_pago` no debe informarse para tipos distintos de 19. Se aprobó una segunda allowlist, separada de 1550 y más restringida que el código de error por sí solo. Solo acepta una autorización directa `FEXAuthorize` con código 1673 y este `ErrMsg` literal, comparado por ordinal exacto, sin trim ni normalización:

> Para facturas de exportacion de productos y para las notas de debito y credito no informar fecha de pago

El request congelado debe pertenecer a WSFEX tipo 20 o 21, `TipoExpo == 2`, y contener un único `Fecha_pago` no vacío con formato válido `yyyyMMdd`; Id remoto, identidad y snapshot deben coincidir exactamente. Se exige la estructura XML y QName del contrato, un único error y ausencia de `FEXResultAuth`. Fecha ausente, vacía, inválida, 1671/1672/1674, mensaje diferente, respuesta ambigua o cualquier resultado de autorización fallan cerrados. No se extiende la regla a `TipoExpo` 4 ni a otros tipos de comprobante.

Un rechazo 1673 inicial solo se clasifica `Rejected` cuando satisface esa regla completa. Para operaciones inciertas ya guardadas, la capability de adjudicación requiere la respuesta original íntegra, el snapshot exacto, tenant/contexto, versión esperada, ausencia de lease activa y auditoría SQLite append-only con operador, origen y motivo. La respuesta actual debe ser la misma validación 1673 admitida o una consulta exacta 1020 estructuralmente válida sin indicios positivos; 1020 solo evita contradecir la evidencia y nunca prueba ausencia ni rechazo. La transición no modifica el payload, identidad, número o Id, ni llama a ARCA.

Después de adjudicar el pendiente observado, la única corrección aprobada para este caso es una revisión explícita y versionada que omita `Fecha_pago`, preserve exactamente los demás campos y mantenga la misma identidad e Id remoto. La adjudicación no prepara ni envía esa revisión. Una operación histórica `Unknown` no se reclasifica al consultar o reanudar: requiere invocar explícitamente la adjudicación auditada. No hay reintento, cambio de número ni reemisión automática. La capacidad sigue disponible solo en SQLite y el wrapper protegido; EF Core y providers personalizados sin ella fallan cerrados.

La evidencia autenticada del caso observado es más estrecha que la política: la nota de débito tipo 20/Expo 2 devolvió 1673 con `Fecha_pago` y sin `FEXResultAuth`. La adjudicación explícita preservó el rechazo, la respuesta original y el registro append-only. La continuación preparó una revisión que quitó únicamente `Fecha_pago`, con la misma identidad e Id; ARCA la autorizó y la consulta exacta posterior verificó la coincidencia. La nota de crédito WSFEX también quedó autorizada y confirmada. Estos casos son evidencia por escenario, no homologación completa. Fuente: [manual WSFEX V3.1.1, validación de `Fecha_pago`](https://arca.gob.ar/ws/documentacion/manuales/WSFEX-Manualparaeldesarrollador_V3.1.1_ARCA.pdf).

### Reconciliación

La reconciliación consulta la identidad exacta y compara todos los campos fiscales que el adaptador puede proyectar contra el request congelado. Una respuesta negativa, vacía, parcial o no concluyente nunca se interpreta como prueba automática de ausencia, nunca produce un nuevo envío y conserva `Unknown`. Una respuesta con datos incompatibles queda `Conflict`. Solo una coincidencia positiva con autorización válida puede resolver a `Authorized`. Los rechazos se determinan al procesar el resultado detallado de la llamada de autorización; una consulta vacía no basta para marcarlos.

Una consulta exacta con errores de servicio no puede confirmar autorización aunque incluya una proyección positiva: queda inconclusa (`Unknown`). Las observaciones WSMTXCA no son errores; una discrepancia fiscal sin error de servicio sigue siendo `Conflict`.

Para los strings opcionales `Obs_comerciales`, `Obs` e `Incoterms`, ausencia y cadena vacía se consideran representaciones equivalentes; no se recortan ni normalizan otros valores, y el payload congelado no se modifica. La excepción `Incoterms_Ds="0"` pertenece exclusivamente a la proyección del harness: para WSFEX con `TipoExpo` 2 o 4, si los valores del request para `Incoterms` e `Incoterms_Ds` son nulos/vacíos, se considera equivalente una respuesta con `Incoterms` nulo/vacío e `Incoterms_Ds` exactamente `0`. No aplica a valores no vacíos informados, otros textos o espacios, valores distintos de `0` ni otros tipos de exportación. El core conserva comparación estricta de `Incoterms_Ds`, incluido `0`. Estas reglas se aplicaron a la consulta exacta posterior, que coincidió en identidad, CAE, autorización y contenido para la factura E autorizada. `ClsFexRequest` ya expone `Incoterms` e `IncotermsDs`; no se necesita ampliar la API pública para informar esos datos.

Una consulta exacta WSMTXCA identificó diferencias solo de escala léxica en doce campos XSD `decimal`; los valores numéricos eran equivalentes, sin diferencias de GTIN, unidad ni valores por defecto. La comparación de proyección del core y del harness acepta igualdad numérica solo en una allowlist de paths tipados `decimal`. Esta regla no redondea ni tolera diferencias de valor y no muta el request congelado, la serialización canónica ni su hash. La corrida local final verificó por consulta exacta los seis escenarios WSMTXCA A/B con esta comparación.

Los tres escenarios WSMTXCA A1/A2/A3 y los tres B6/B7/B8 quedaron autorizados y confirmados por consulta exacta. Las respuestas A incluyeron `Resultado=O` y observaciones 130 y 504; B incluyó `Resultado=O` y observación 504, sin errores. B6 había quedado inicialmente `Rejected` con error 132. El request usaba `codigoTipoDocumento=99` y `numeroDocumento=0`, valores de la fixture de consumidor final usada en WSFE que no son válidos para MTXCA. Se preparó una revisión explícita de la misma identidad omitiendo solo esos campos, conforme a la regla 128 del manual. Se preservaron el snapshot original y su hash; esta corrección no usó la adjudicación del core para WSFEX 1550/1673. La revisión y los otros escenarios B fueron autorizados y confirmados por consulta exacta.

## Persistencia y recuperación

`IInvoiceJournal` define las operaciones atómicas; `InvoiceCoordinator` prepara, adquiere leases/fencing y registra la decisión sin sobrescribir el trabajo de un lease más reciente. `SqliteInvoiceJournal` persiste el diario local y admite varios procesos de un host que compartan el mismo archivo. No usar en NFS ni como archivo compartido entre hosts. Para múltiples hosts, implementar `IInvoiceJournal` sobre una base transaccional compartida con las mismas claves únicas y fencing.

`IInvoiceJournal.ListPendingAsync(tenantId, limit)` enumera para la aplicación operaciones `Prepared`, `Unknown`, `Conflict` y `ManualReview`, además de envíos/reconciliaciones con lease vencida. En el paquete core la aplicación puede iniciar la recuperación con `SafeInvoiceService.ResumeAsync`: envía solo el snapshot `Prepared` y reconcilia los demás estados. Un elemento pendiente no es por sí mismo una instrucción para reemitir.

El paquete opcional `NetArcaWs.EntityFrameworkCore` agrega el módulo `InvoiceRecovery`, una cola y un `BackgroundService` de registro explícito. `AddInvoiceRecovery(...)` se selecciona por separado de `AddInvoicing(...)`, con el mismo servicio en ambos; la selección no migra ni activa el worker. El host llama `AddNetArcaWsInvoiceRecoveryWorker(scope, configure)` y registra `IInvoiceRecoveryContextResolver`. El resolver de la aplicación vuelve a autorizar tenant, CUIT representada y ambiente y resuelve la referencia opaca de credencial fijada por esa generación del trabajo. Debe rechazar credenciales revocadas o ausentes y no sustituirlas silenciosamente por la versión activa. La biblioteca no persiste certificados, claves, Token ni Sign en la cola.

La cola guarda una huella fiscal del snapshot y un pin técnico a la versión del diario, pero no duplica el payload. Repetir `PrepareAndEnqueueAsync` con la misma huella y referencia no reinicia intento, horario, generación ni claim. `ScheduleAsync` requiere la versión exacta vigente, puede reactivar un job completado o suspendido compatible y no cambia payload ni llama SOAP; después de una revisión explícita de un rechazo, puede fijar el snapshot revisado. `FindAsync` devuelve solo estado, intentos, horarios, generación y un motivo seguro; no expone identidad, tenant, clave, hash ni referencia de credencial.

Cada claim tiene un fence/generación separado y vence en hasta 30 minutos. Si el claim vence mientras el diario sigue `Prepared`, la misma transacción lo cambia a `Unknown`, aumenta la versión técnica y reclama para reconciliar. Así el claim antiguo no habilita un segundo envío. SOAP ocurre fuera de la transacción. Solo un `Prepared` con claim vigente puede llamar a `ResumeAsync`; `Unknown` y los estados `Submitting`/`Reconciling` vencidos consultan el comprobante existente. Consultas vacías o no disponibles se programan como reconciliación con backoff exponencial acotado y jitter; no producen autorización de reenvío. Al alcanzar el límite de intentos (10 por defecto), el job se suspende hasta una llamada explícita a `ScheduleAsync`. `Authorized` y `Rejected` completan el job; `Rejected` conserva la semántica preexistente y libera la reserva de serie. `Conflict` y `ManualReview` suspenden para intervención.

SQLite admite archivo local y procesos del mismo host, no NFS ni hosts distintos. Una instalación multi-host necesita almacenamiento transaccional compartido y un provider soportado con las restricciones únicas y fencing correspondientes; el worker no aporta coordinación distribuida ni un scheduler externo. La cola cubre solo CAE unitario WSFEv1, WSFEXv1 y WSMTXCA. No hay exactly-once, emisión batch, retry HTTP genérico ni failover de ambiente.

## Límites de la política actual

- No hay asignación/consulta automática de próximo número. La aplicación resuelve secuencias según su integración; el diario bloquea una misma serie mientras haya un resultado no terminal.
- `SafeInvoiceService` acepta un comprobante por operación durable. Aunque los clientes SOAP de bajo nivel exponen operaciones batch, no se afirma que un lote esté dividido y persistido durablemente por item.
- La reconciliación actual es solo consulta/comparación. No reenvía un resultado desconocido aunque el manual de un servicio describa una repetición segura. `ResumeAsync` solo puede enviar el snapshot original si permanece `Prepared`; no convierte un estado incierto a preparado. Corregir un rechazo requiere una revisión auditable y una nueva llamada explícita a `ResumeAsync`.
- No hay garantía de exactamente una vez frente a fallas externas, emisores que operen fuera del diario, pérdida/corrupción del storage o intervención manual.
- No se hace failover entre homologación y producción ni retry HTTP genérico de una escritura SOAP.

## Comportamiento documentado de los servicios y uso actual

Los manuales describen diferencias que pueden guiar una evolución futura, pero no autorizan un comportamiento más amplio que el implementado:

- **WSFEv1:** se envía un detalle; la decisión positiva/rechazo se correlaciona con la cabecera y el único detalle. `FECompConsultar` es la consulta exacta. Un último autorizado no identifica el payload de un intento incierto.
- **WSFEXv1:** `Cmp.Id` y los campos fiscales quedan congelados. El manual describe reproceso con el mismo ID, pero la implementación actual no reenvía: consulta `FEXGetCMP` y solo resuelve una respuesta positiva correlacionada.
- **WSMTXCA:** se consulta `consultarComprobante` por la clave exacta. El manual contiene una regla de manejo transaccional para solicitudes no procesadas, pero el código actual no vuelve a autorizar por ausencia; conserva el estado incierto.

`WSMTXCA` cubre su propio contrato de facturación con detalle/CAE/CAEA; esta ADR no le atribuye la implementación del ciclo completo de Factura de Crédito Electrónica MiPyME.

## Evolución pendiente

Una ampliación futura aún requiere evidencia por contrato para cualquier regla de reenvío, decisión sobre asignación concurrente de números, lotes con identidad y resultado por item, y políticas de retención/notificación. La cola tampoco resuelve lectura remota eventualmente consistente, pérdida/corrupción del storage, emisores que omitan el diario ni coordinación entre bases. Ninguna de estas condiciones habilita retry HTTP ni una garantía exactly-once.

## Fuentes oficiales consultadas

La evidencia de contratos desplegados y manuales se registra en [`healthchecks-contracts.md`](Contratos-de-health-checks), [`docs/reference/contracts`](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/docs/reference/contracts) y [ADR 0004](Decisi%C3%B3n-4-Contratos-SOAP-p%C3%BAblicos). Revalidar las versiones al implementar cada cambio. Las ejecuciones reales de escenarios seleccionados no equivalen a la homologación integral de la matriz; documentar por separado autorizaciones, consultas exactas y casos pendientes.

- [ARCA, homologación externa y versiones WSFEv1/WSMTXCA](https://www.arca.gob.ar/fe/ayuda/homologacion_externa.asp)
- [ARCA, manual WSFEv1 V4.7](https://www.arca.gob.ar/ws/documentacion/manuales/manual-desarrollador-ARCA-COMPG.pdf)
- [ARCA, manual WSFEXv1 V3.1.1](https://arca.gob.ar/ws/documentacion/manuales/WSFEX-Manualparaeldesarrollador_V3.1.1_ARCA.pdf)
- [ARCA, manual WSMTXCA V0.25.8](https://www.arca.gob.ar/fe/ayuda/documentos/wsmtxca-RG-2904.pdf)
- [ARCA, documentación general de factura electrónica](https://arca.gob.ar/ws/documentacion/ws-factura-electronica.asp)
