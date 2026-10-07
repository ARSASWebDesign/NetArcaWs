# ADR 0001: Emisión durable y reconciliación conservadora

- Estado: implementación y suite local verificadas; homologación autenticada pendiente
- Fecha de investigación: 2026-10-06
- Alcance: operaciones unitarias de autorización CAE en WSFEv1, WSFEXv1 y WSMTXCA
- Fuera de alcance: emisión batch durable, asignación automática de numeración,
  reenvíos automáticos, scheduler distribuido y garantía de exactamente una vez

## Contexto

Una solicitud puede llegar a ARCA y procesarse aunque el cliente no reciba la
respuesta. Tras un timeout, corte de conexión o cancelación, el resultado puede
ser incierto. Asignar otro número o reenviar sin una decisión fundada puede
producir duplicados, errores de correlatividad o pérdida del resultado original.
Los servicios no definen una garantía universal de exactamente una vez ni una
ventana común de consistencia de lectura.

## Decisión implementada

Las operaciones conservan un payload tipado congelado, una versión de
serialización/canonicalización, el tenant, servicio y ambiente, identidad fiscal,
hash, estado, respuesta y lease/versionado. La biblioteca no asigna números: la
aplicación entrega el número dentro del request. La clave fiscal única es
`(ambiente, CUIT, punto de venta, tipo, número)`, sin `tenantId` ni nombre de
protocolo. Además, no se permite preparar otro comprobante de la misma serie
`(ambiente, CUIT, punto de venta, tipo)` mientras una operación anterior de esa
serie siga sin estado terminal `Authorized` o `Rejected`.

La clave de idempotencia `(tenantId, key)` ligada al mismo request devuelve la
operación ya guardada y no envía otra llamada. Reutilizarla con otro payload,
versión canónica o identidad conflictúa. Una identidad fiscal ocupada por otro
tenant/servicio también conflictúa. Las aplicaciones deben guardar sus claves y
no reutilizarlas para un comprobante distinto.

El canonicalizador vigente almacena una versión explícita (`CanonicalVersion`)
y una representación XML tipada del payload fiscal antes de insertar los campos
WSAA. El hash SHA-256 se calcula sobre esos bytes persistidos. Token, firma y otros
campos efímeros no se incluyen en esa entrada. No se debe cambiar la versión o
representación sin migración y pruebas de compatibilidad del diario.

`SafeInvoiceService` ofrece:

- `AuthorizeWsfeAsync(tenant, key, request)` para un único comprobante CAE con
  `CantReg = 1`, un detalle, `CbteDesde = CbteHasta`, fecha de emisión y tasa de
  cambio explícitas.
- `AuthorizeWsfexAsync(tenant, key, request)` para una factura con `Id` positivo,
  fecha y cotización explícitas.
- `AuthorizeWsmtxcaAsync(tenant, key, request)` para CAE, con fecha/cotización
  explícitas y sin campos de autorización ya completados.
- `ReconcileAsync(tenant, key)` para consultar el mismo comprobante con el
  adaptador del servicio guardado y comparar la respuesta con el payload fiscal.

`ResumeAsync(tenant, key)` recupera una operación persistida. Solo envía el
snapshot inmutable cuando su estado actual es `Prepared`; en cualquier otro
estado ejecuta reconciliación. Esto cubre una caída entre la preparación durable
y el envío, sin reintentar automáticamente un envío incierto. `Prepared` no
significa que ARCA haya recibido una solicitud.

Las sobrecargas `ReviseRejectedAsync(tenant, key, expectedVersion, replacement)`
permiten preparar explícitamente una corrección para una operación cuya versión
actual está en `Rejected`. El diario exige preservar tenant, clave, servicio,
identidad fiscal y, cuando existe, ID remoto de la solicitud; registra el rechazo
anterior en un historial inmutable y deja el nuevo snapshot en `Prepared`. La
revisión no envía. La aplicación debe inspeccionar la corrección y llamar luego a
`ResumeAsync` de forma explícita. Las revisiones son optimistas por versión y no
permiten editar un `Unknown`, `Conflict` o una versión ya cambiada.

La CUIT representada y el ambiente de cada contexto se comprueban contra la
operación persistida al reconciliar. La autorización de tenant sigue siendo
responsabilidad de la aplicación. Los requests incluyen número/tipo/punto de
venta proporcionados por quien llama; no hay asignador automático.

Al enviar, solo una respuesta completa, correlacionada con la solicitud y con un
CAE válido termina en `Authorized`. `Rejected` exige el resultado de rechazo en
el detalle correspondiente y correlacionado, sin CAE. Las respuestas incompletas
o ambiguas quedan `Unknown`. En la llamada de autorización, una respuesta mal
correlacionada también queda `Unknown`; durante reconciliación, una discrepancia
fiscal frente al comprobante remoto se registra como `Conflict`. Un error de
transporte se guarda como `Unknown` y se propaga al caller. El XML de respuesta y los datos fiscales se guardan en el
diario y requieren protección de acceso, retención y copias.

WSMTXCA devuelve una estructura distinta para algunos rechazos: `Resultado = R`,
un `ArrayErrores` no vacío y ausencia de `ComprobanteResponse` se clasifica como
`Rejected` de acuerdo con el manual. Para `A`/`O`, el adaptador exige respuesta de
comprobante correlacionada y CAE válido antes de guardar `Authorized`.

La reconciliación consulta la identidad exacta y compara todos los campos fiscales
que el adaptador puede proyectar contra el request congelado. Una respuesta
negativa, vacía, parcial o no concluyente nunca se interpreta como prueba
automática de ausencia, nunca produce un nuevo envío y conserva `Unknown`. Una
respuesta con datos incompatibles queda `Conflict`. Solo una coincidencia positiva
con autorización válida puede resolver a `Authorized`. Los rechazos se determinan
al procesar el resultado detallado de la llamada de autorización; una consulta
vacía no basta para marcarlos.

## Persistencia y recuperación

`IInvoiceJournal` define las operaciones atómicas; `InvoiceCoordinator` prepara,
adquiere leases/fencing y registra la decisión sin sobrescribir el trabajo de un
lease más reciente. `SqliteInvoiceJournal` persiste el diario local y admite varios
procesos de un host que compartan el mismo archivo. No usar en NFS ni como archivo
compartido entre hosts. Para múltiples hosts, implementar `IInvoiceJournal` sobre
una base transaccional compartida con las mismas claves únicas y fencing.

`IInvoiceJournal.ListPendingAsync(tenantId, limit)` enumera para la aplicación
operaciones `Prepared`, `Unknown`, `Conflict` y `ManualReview`, además de
envíos/reconciliaciones con lease vencida. En el paquete core la aplicación puede
iniciar la recuperación con `SafeInvoiceService.ResumeAsync`: envía solo el
snapshot `Prepared` y reconcilia los demás estados. Un elemento pendiente no es
por sí mismo una instrucción para reemitir.

El paquete opcional `NetArcaWs.EntityFrameworkCore` agrega el módulo
`InvoiceRecovery`, una cola y un `BackgroundService` de registro explícito.
`AddInvoiceRecovery(...)` se selecciona por separado de `AddInvoicing(...)`, con
el mismo servicio en ambos; la selección no migra ni activa el worker. El host
llama `AddNetArcaWsInvoiceRecoveryWorker(scope, configure)` y registra
`IInvoiceRecoveryContextResolver`. El resolver de la aplicación vuelve a autorizar
tenant, CUIT representada y ambiente y resuelve la referencia opaca de credencial
fijada por esa generación del trabajo. Debe rechazar credenciales revocadas o
ausentes y no sustituirlas silenciosamente por la versión activa. La biblioteca
no persiste certificados, claves, Token ni Sign en la cola.

La cola guarda una huella fiscal del snapshot y un pin técnico a la versión del
diario, pero no duplica el payload. Repetir `PrepareAndEnqueueAsync` con la misma
huella y referencia no reinicia intento, horario, generación ni claim. `ScheduleAsync`
requiere la versión exacta vigente, puede reactivar un job completado o suspendido
compatible y no cambia payload ni llama SOAP; después de una revisión explícita
de un rechazo, puede fijar el snapshot revisado. `FindAsync` devuelve solo estado,
intentos, horarios, generación y un motivo seguro; no expone identidad, tenant,
clave, hash ni referencia de credencial.

Cada claim tiene un fence/generación separado y vence en hasta 30 minutos. Si el
claim vence mientras el diario sigue `Prepared`, la misma transacción lo cambia
a `Unknown`, aumenta la versión técnica y reclama para reconciliar. Así el claim
antiguo no habilita un segundo envío. SOAP ocurre fuera de la transacción. Solo
un `Prepared` con claim vigente puede llamar a `ResumeAsync`; `Unknown` y los
estados `Submitting`/`Reconciling` vencidos consultan el comprobante existente.
Consultas vacías o no disponibles se programan como reconciliación con backoff
exponencial acotado y jitter; no producen autorización de reenvío. Al alcanzar el
límite de intentos (10 por defecto), el job se suspende hasta una llamada explícita
a `ScheduleAsync`. `Authorized` y `Rejected` completan el job; `Rejected` conserva
la semántica preexistente y libera la reserva de serie. `Conflict` y `ManualReview`
suspenden para intervención.

SQLite admite archivo local y procesos del mismo host, no NFS ni hosts distintos.
Una instalación multi-host necesita almacenamiento transaccional compartido y un
provider soportado con las restricciones únicas y fencing correspondientes; el
worker no aporta coordinación distribuida ni un scheduler externo. La cola cubre
solo CAE unitario WSFEv1, WSFEXv1 y WSMTXCA. No hay exactly-once, emisión batch,
retry HTTP genérico ni failover de ambiente. El código de esta extensión es
posterior a los paquetes públicos 0.6.0 y aún requiere una publicación posterior.

## Límites de la política actual

- No hay asignación/consulta automática de próximo número. La aplicación resuelve
  secuencias según su integración; el diario bloquea una misma serie mientras
  haya un resultado no terminal.
- `SafeInvoiceService` acepta un comprobante por operación durable. Aunque los
  clientes SOAP de bajo nivel exponen operaciones batch, no se afirma que un lote
  esté dividido y persistido durablemente por item.
- La reconciliación actual es solo consulta/comparación. No reenvía un resultado
  desconocido aunque el manual de un servicio describa una repetición segura.
  `ResumeAsync` solo puede enviar el snapshot original si permanece `Prepared`;
  no convierte un estado incierto a preparado. Corregir un rechazo requiere una
  revisión auditable y una nueva llamada explícita a `ResumeAsync`.
- No hay garantía de exactamente una vez frente a fallas externas, emisores que
  operen fuera del diario, pérdida/corrupción del storage o intervención manual.
- No se hace failover entre homologación y producción ni retry HTTP genérico de
  una escritura SOAP.

## Comportamiento documentado de los servicios y uso actual

Los manuales describen diferencias que pueden guiar una evolución futura, pero
no autorizan un comportamiento más amplio que el implementado:

- **WSFEv1:** se envía un detalle; la decisión positiva/rechazo se correlaciona
  con la cabecera y el único detalle. `FECompConsultar` es la consulta exacta. Un
  último autorizado no identifica el payload de un intento incierto.
- **WSFEXv1:** `Cmp.Id` y los campos fiscales quedan congelados. El manual describe
  reproceso con el mismo ID, pero la implementación actual no reenvía: consulta
  `FEXGetCMP` y solo resuelve una respuesta positiva correlacionada.
- **WSMTXCA:** se consulta `consultarComprobante` por la clave exacta. El manual
  contiene una regla de manejo transaccional para solicitudes no procesadas, pero
  el código actual no vuelve a autorizar por ausencia; conserva el estado incierto.

`WSMTXCA` cubre su propio contrato de facturación con detalle/CAE/CAEA; esta ADR
no le atribuye la implementación del ciclo completo de Factura de Crédito
Electrónica MiPyME.

## Evolución pendiente

Una ampliación futura aún requiere evidencia por contrato para cualquier regla de
reenvío, decisión sobre asignación concurrente de números, lotes con identidad y
resultado por item, y políticas de retención/notificación. La cola tampoco
resuelve lectura remota eventualmente consistente, pérdida/corrupción del storage,
emisores que omitan el diario ni coordinación entre bases. Ninguna de estas
condiciones habilita retry HTTP ni una garantía exactly-once.

## Fuentes oficiales consultadas

La evidencia de contratos desplegados y manuales se registra en
[`healthchecks-contracts.md`](../reference/healthchecks-contracts.md),
[`docs/reference/contracts`](../reference/contracts) y
[ADR 0004](0004-public-soap-contracts.md). Revalidar las versiones al implementar
cada cambio. No se afirma homologación de emisión sin una ejecución real con
credenciales autorizadas.

- [ARCA, homologación externa y versiones WSFEv1/WSMTXCA](https://www.arca.gob.ar/fe/ayuda/homologacion_externa.asp)
- [ARCA, manual WSFEv1 V4.7](https://www.arca.gob.ar/ws/documentacion/manuales/manual-desarrollador-ARCA-COMPG.pdf)
- [ARCA, manual WSFEXv1 V3.1.1](https://arca.gob.ar/ws/documentacion/manuales/WSFEX-Manualparaeldesarrollador_V3.1.1_ARCA.pdf)
- [ARCA, manual WSMTXCA V0.25.8](https://www.arca.gob.ar/fe/ayuda/documentos/wsmtxca-RG-2904.pdf)
- [ARCA, documentación general de factura electrónica](https://arca.gob.ar/ws/documentacion/ws-factura-electronica.asp)
