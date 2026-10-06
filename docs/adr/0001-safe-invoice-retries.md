# ADR 0001: Reintentos seguros para autorizar comprobantes

- Estado: Propuesto para Hito 2
- Fecha de investigación: 2026-10-06
- Alcance: diseño de WSFEv1, WSFEXv1 y WSMTXCA; no implementa facturación

## Contexto

Una solicitud de autorización puede llegar a ARCA y ser procesada aunque el cliente
no reciba la respuesta. Ante un timeout, un corte de conexión o la cancelación del
cliente después de enviar bytes, el resultado es **desconocido**. Si la aplicación
asigna otro número y vuelve a emitir, puede producir dos comprobantes para la misma
operación comercial. Si reenvía sin reconciliar, puede recibir errores de
consecutividad, perder el CAE o confundir una aceptación con un rechazo.

Los servicios no ofrecen un contrato universal de exactamente una vez. Las
consultas de estado y la repetición segura varían por servicio; los manuales no
prometen una ventana general de consistencia de lectura.

## Decisión propuesta

La autorización será una operación durable e inmutable. Antes de la primera llamada
se persistirán el identificador local de operación, CUIT emisora, servicio y
ambiente, punto de venta, tipo y número de comprobante, el payload fiscal canónico
completo y su hash. La identidad fiscal única será
`(ambiente, CUIT emisora, punto de venta, tipo, número)`; el servicio/protocolo se
guarda como metadato inmutable de la operación, no como parte de esa restricción.
Así, seleccionar otro adaptador no permite registrar una segunda identidad para el
mismo comprobante. Una operación nunca cambia de servicio ni hace failover a otro
protocolo después de ser preparada. También habrá una clave de idempotencia del
cliente: repetirla con el mismo hash devuelve el estado existente; repetirla con otro
hash produce conflicto y requiere intervención.

El hash cubre solo los campos fiscales/business payload serializados por una versión
explícita del canonicalizador. Incluye los valores que pueden cambiar el comprobante
emitido; excluye `Token`, `Sign`, `Auth`, SOAPAction, headers HTTP y otros datos
efímeros del transporte. Una renovación de WSAA puede cambiar esos valores sin
alterar la identidad fiscal ni el hash. El diario retiene la versión del
canonicalizador para poder volver a calcular y comparar el payload original.

El payload y el número quedan congelados desde `Prepared` hasta un resultado
terminal. Ningún timeout, excepción de transporte ni respuesta de consulta ambigua
autoriza a asignar un número nuevo. La aplicación consultará exactamente la misma
clave de comprobante y comparará la respuesta con los campos congelados. Cuando la
consulta confirma una aceptación con el mismo comprobante, se guarda el CAE y se
marca `Authorized`. Si devuelve el mismo número con datos incompatibles, se marca
`Conflict` y no se reenvía.

Una consulta negativa se tratará como evidencia de ausencia solo cuando el adaptador
del servicio pueda identificar la respuesta documentada de “no encontrado”. Si el
servicio devuelve error transitorio, respuesta incompleta, dato contradictorio o no
permite distinguir ausencia de procesamiento pendiente, la operación conserva el
estado `Unknown` y se consulta de nuevo con espera acotada. No se usa un texto libre,
un HTTP status genérico ni el último número remoto como prueba suficiente. Si no se
resuelve dentro del horizonte de reintentos, queda en `ManualReview`; no se emite el
siguiente número automáticamente.

Solo un resultado de negocio explícito documentado como rechazo definitivo termina
en `Rejected`. Observaciones no excluyentes que traen CAE son autorización, no
rechazo. Los códigos se clasifican por método y versión del manual vigente; no se
adivinan rangos de códigos ni se reintentan genéricamente todos los SOAP Faults o
errores 5xx.

## Diario durable y coordinación

El diario debe persistir, como mínimo:

- `OperationId`, clave de idempotencia, ambiente y servicio.
- CUIT, punto de venta, tipo y número asignados antes del envío.
- Payload canónico inmutable, hash SHA-256, fecha de creación y versión del
  canonicalizador/esquema.
- Estado, número de intento, próxima fecha de consulta, último error clasificado,
  respuesta remota saneada, CAE/fecha de vencimiento cuando existan.
- Para WSFEX, el `Cmp.Id` local y los identificadores de correlación recibidos en
  `FEHeaderInfo`.

La reserva de número y la inserción del diario se hacen en una transacción con
restricción única. Un `IMemoryCache`, un `lock` local o un semaphore por proceso no
protegen varias réplicas. El envío se reclama con una lease durable y un fencing
token/versión para que un worker vencido no pueda escribir un resultado obsoleto.
La lease vencida de una operación que pudo salir a la red la devuelve a
`Reconciling`, no a `Prepared`.

El flujo recomendado es outbox más worker durable:

```text
Prepared -> Submitting -> Authorized
                    \-> Rejected
                    \-> Unknown -> Reconciling -> Authorized | Rejected
                                               \-> Unknown (reconsultar)
                                               \-> ManualReview
```

Antes de enviar, se confirma que `Prepared` está guardado. Después de comenzar el
envío, cualquier cancelación o timeout del caller deja la operación en `Unknown`
salvo que se sepa con certeza que ningún byte pudo salir. El trabajo de
reconciliación sobrevive a la cancelación de la petición HTTP del usuario. Las
consultas usan backoff exponencial con jitter, límites de frecuencia por servicio y
un horizonte configurable; agotarlo detiene la automatización, no crea otra factura.

La numeración se serializa en almacenamiento compartido por CUIT/servicio/punto de
venta/tipo. Al iniciar o recuperar una secuencia se consulta el último número remoto,
pero esa lectura solo sirve para sincronizar la asignación. Una consulta del último
autorizado no identifica el payload del intento incierto y no reemplaza la consulta
exacta. Emisiones externas al sistema pueden cambiar la secuencia; una discrepancia
se bloquea y se resuelve explícitamente.

El orquestador de alto nivel usará un comprobante por request (`CantReg = 1`) como
predeterminado seguro para Hito 2. La API de bajo nivel conservará el soporte de
lotes de `FECAESolicitar` para completar el port. Cada item de un lote tiene su
propia identidad fiscal, snapshot/hash y estado de autorización; el lote conserva
además la composición original como metadato de envío. Si el resultado es parcial,
se guardan los resultados `A`/`R` por comprobante y los items no procesados quedan
reconciliables individualmente. Nunca se reenvía el lote completo por una respuesta
parcial; cualquier repetición debe seleccionar solo los items pendientes según el
resultado documentado del servicio.

## Comportamiento por servicio

### WSFEv1

- `FECAESolicitar` autoriza por CAE. El manual describe el caso de timeout ambiguo:
  reenviar la misma factura puede fallar por consecutividad si el primer envío ya se
  procesó.
- Tras resultado desconocido, `FECompConsultar` consulta por CUIT representada,
  tipo, punto de venta y número. Devuelve los datos enviados y el CAE para el
  comprobante emitido; comparar sus campos fiscales con el snapshot local.
- `FECompUltimoAutorizado` devuelve el último autorizado para CUIT/punto/tipo (y
  tipo de emisión en la documentación); sirve para coordinar secuencia, nunca para
  atribuir el último comprobante a una operación local concreta.
- Solo aceptar/rechazar cuando la respuesta y cada `FECAEDetResponse` representen un
  resultado de negocio completo. En lotes el encabezado puede ser `A`, `R` o `P` y
  los detalles individuales pueden diferir.
- Reconsultar la clave exacta después de ausencia verificable; reenviar, si el
  adaptador determina que procede, únicamente el mismo payload y el mismo número.
  La documentación no fija un plazo de propagación para la consulta: ante esa duda,
  continuar en `Unknown` en vez de avanzar de número.

### WSFEXv1

- `FEXAuthorize` tiene un identificador cliente `Cmp.Id`. El manual documenta que,
  ante pérdida de solicitud o respuesta, se vuelve a enviar exactamente la misma
  solicitud con el mismo `Cmp.Id`; si ya fue aprobada, WSFEX devuelve
  `Reproceso = S`, y si no, la procesa normalmente. Es una excepción explícita al
  flujo de consulta previa de otros servicios, no permiso para reutilizar el ID con
  otro payload fiscal. `Cmp.Id` y los campos de `Cmp` quedan congelados; si caduca
  WSAA, se puede renovar Token/Sign y regenerar solo la envoltura de autenticación.
  El hash del payload fiscal excluye esos campos efímeros.
- Reservar `Cmp.Id` único y persistirlo con el hash antes del primer envío. Tras una
  respuesta perdida, consultar primero `FEXGetCMP` por la identidad fiscal. Si aún
  no se confirma y se decide usar la repetición documentada, el retry mantiene el
  mismo `Cmp.Id` y los mismos campos fiscales, nunca un ID nuevo para resolver ese
  timeout.
- `FEXGetCMP` consulta el comprobante aprobado por tipo/punto/número. `FEXGetLast_ID`
  devuelve el máximo `Id` recibido por CUIT y `FEXGetLast_CMP` el último comprobante
  aprobado por tipo/punto. Ninguno de esos máximos demuestra por sí solo el estado
  de un `Cmp.Id` específico.
- Conservar también `FEHeaderInfo` de la respuesta si está presente, para soporte y
  correlación. No confundir ese identificador de respuesta con `Cmp.Id`.

### WSMTXCA

- `autorizarComprobante` incluye tipo, punto de venta y número; no documenta una
  clave idempotente de request equivalente a `Cmp.Id` de WSFEX.
- El apartado de manejo transaccional manda consultar `consultarComprobante` después
  de un error de comunicación. Si la consulta confirma que se procesó y aceptó, se
  cierra con esa respuesta; si confirma que no fue procesado, el manual permite
  repetir la solicitud. Si se repite algo ya aceptado, el WS lo rechaza por
  correlatividad.
- `consultarComprobante` consulta la clave exacta y `consultarUltimoComprobanteAutorizado`
  devuelve el último autorizado/informado para tipo y punto. Usar la consulta exacta
  primero; el último comprobante solo ayuda a reconciliar secuencia y no identifica
  el contenido de esta operación.
- Si la respuesta de consulta no es inequívocamente “no existe” según el contrato
  documentado de la versión elegida, conservar `Unknown`. Si confirma ausencia,
  repetir únicamente el mismo payload con el mismo número según la guía transaccional.
  No cambiar el número porque una respuesta de consulta llegue tarde.

## Máquina de reintentos

1. **Preparar:** validar el payload, reservar secuencia en una transacción y guardar
   request/hash; la restricción única impide dos operaciones locales para la misma
   clave fiscal.
2. **Enviar una vez:** registrar intento y lease. Un resultado estructurado de éxito
   con CAE marca `Authorized`; rechazo de negocio explícito marca `Rejected`; error
   de red después de iniciar envío marca `Unknown`.
3. **Reconciliar:** llamar la consulta exacta del servicio con el número reservado;
   guardar cada respuesta. No generar un request alternativo mientras el primero
   siga incierto.
4. **Repetir:** ante ausencia verificada, seguir la guía específica: mismo
   `Cmp.Id`/payload para WSFEX; mismo número/payload solo cuando la consulta del
   adaptador y su contrato lo permitan para WSFEv1/WSMTXCA. Cada repetición pasa por
   el mismo lease y control de estado.
5. **Detener:** tras límite de intentos/tiempo o ante respuestas contradictorias,
   entrar en `ManualReview` y notificar a la aplicación. La acción de soporte debe
   consultar y registrar evidencia antes de cualquier corrección manual.

Los reintentos de lectura (consultas de estado) y las escrituras (autorizaciones)
son políticas separadas. No aplicar `HttpClient`/Polly retry general a `POST`; no
hacer failover entre homologación y producción ni enviar concurrentemente el mismo
comprobante por dos workers.

## Consecuencias y garantías

Esto evita que el propio sistema reserve dos veces una misma identidad fiscal y
preserva evidencia suficiente para recuperar un resultado incierto. No puede
garantizar exactamente una vez frente a fallos del proveedor, escrituras externas,
consultas con propagación no documentada, pérdida del diario ni intervención
manual. El límite es explícito: el sistema no crea una nueva identidad de
comprobante mientras una anterior pueda estar autorizada.

Persistir payload fiscal requiere control de acceso, cifrado en reposo, retención
limitada y exclusión de logs ordinarios. Registrar `OperationId`, clave de
comprobante, ambiente, tipo de transición y correlación remota; ocultar token, firma
WSAA y datos personales/de facturación.

## Fuentes oficiales consultadas

La página oficial general lista WSFEv1 V4.7 y WSFEXv1 V3.1.1. La página de
homologación externa lista WSFEv1 V4.8 y WSMTXCA V0.25.8; sin embargo, la portada
del PDF de WSFEv1 V4.8 indica revisión del 1 de diciembre de 2026, posterior a la
fecha de esta investigación. Los comportamientos WSFEv1 descritos se contrastaron
con los manuales V4.7 y V4.8; antes de fijar clasificadores/códigos se debe confirmar
la versión aplicable al despliegue. Las reglas deben revalidarse al implementar
cada adaptador.

- [ARCA, homologación externa y versiones WSFEv1/WSMTXCA](https://www.arca.gob.ar/fe/ayuda/homologacion_externa.asp)
- [ARCA, manual WSFEv1 V4.8](https://www.arca.gob.ar/fe/ayuda/documentos/wsfev1-RG-4291.pdf)
- [ARCA, manual WSFEv1 V4.7 listado en documentación general](https://www.arca.gob.ar/ws/documentacion/manuales/manual-desarrollador-ARCA-COMPG.pdf)
- [ARCA, manual WSFEXv1 V3.1.1](https://arca.gob.ar/ws/documentacion/manuales/WSFEX-Manualparaeldesarrollador_V3.1.1_ARCA.pdf)
- [ARCA, manual WSMTXCA V0.25.8](https://www.arca.gob.ar/fe/ayuda/documentos/wsmtxca-RG-2904.pdf)
- [ARCA, documentación general de web services de factura electrónica](https://arca.gob.ar/ws/documentacion/ws-factura-electronica.asp)
