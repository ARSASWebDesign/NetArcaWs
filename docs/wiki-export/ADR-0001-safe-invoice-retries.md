<!-- Source: docs/adr/0001-safe-invoice-retries.md. Generated wiki mirror; edit the repository source. -->

# ADR 0001: Emisión durable y reconciliación conservadora

- Estado: implementación y suite local verificadas; homologación autenticada pendiente
- Fecha de investigación: 2026-10-06
- Alcance: operaciones unitarias de autorización CAE en WSFEv1, WSFEXv1 y WSMTXCA
- Fuera de alcance: emisión batch durable, asignación automática de numeración,
  reenvíos automáticos, worker alojado y garantía de exactamente una vez

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
envíos/reconciliaciones con lease vencida. La aplicación debe programar la
recuperación: usar `SafeInvoiceService.ResumeAsync` para cada elemento. Este método
envía solo el snapshot `Prepared`; para los demás llama a reconciliación. No hay
worker alojado, scheduler, backoff/jitter ni notificación integrados; no se ejecuta
recuperación en segundo plano. Un pending item no es por sí mismo una instrucción
para reemitir.

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

Para ampliar esta política se requiere una ADR o revisión de esta decisión con
pruebas de contrato y reglas oficiales por servicio. Como mínimo: demostrar cómo
se clasifica “no existe” frente a resultado pendiente; fijar retención y backoff;
crear un worker idempotente con límite y telemetría; comprobar la regla de
reenvío de cada operación y su efecto de correlatividad; definir asignación de
número bajo concurrencia; diseñar lotes con identidad/resultado independiente por
item; y verificar recuperación tras caída de DB o del proceso. Ninguno de esos
pasos se debe inferir de `ListPendingAsync` ni habilitar por un retry HTTP.

## Fuentes oficiales consultadas

La evidencia de contratos desplegados y manuales se registra en
[`healthchecks-contracts.md`](Referencia-healthchecks-contracts),
[`docs/reference/contracts`](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/docs/reference/contracts) y
[ADR 0004](ADR-0004-public-soap-contracts). Revalidar las versiones al implementar
cada cambio. No se afirma homologación de emisión sin una ejecución real con
credenciales autorizadas.

- [ARCA, homologación externa y versiones WSFEv1/WSMTXCA](https://www.arca.gob.ar/fe/ayuda/homologacion_externa.asp)
- [ARCA, manual WSFEv1 V4.7](https://www.arca.gob.ar/ws/documentacion/manuales/manual-desarrollador-ARCA-COMPG.pdf)
- [ARCA, manual WSFEXv1 V3.1.1](https://arca.gob.ar/ws/documentacion/manuales/WSFEX-Manualparaeldesarrollador_V3.1.1_ARCA.pdf)
- [ARCA, manual WSMTXCA V0.25.8](https://www.arca.gob.ar/fe/ayuda/documentos/wsmtxca-RG-2904.pdf)
- [ARCA, documentación general de factura electrónica](https://arca.gob.ar/ws/documentacion/ws-factura-electronica.asp)
