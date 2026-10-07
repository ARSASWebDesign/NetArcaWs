# Pruebas fiscales en homologación

El workflow manual **Homologación ARCA** ofrece `consultas`, `emision` y `completa`.
Requiere `main` y la aprobación del entorno `homologacion`. Todos los clientes de
esta suite usan exclusivamente homologación; el escenario de emisión rechaza un
contexto de producción antes de resolver clientes.

## Consultas y descubrimiento

Seleccionar `mode=consultas`, con `services=wsfe`. Se verifican catálogos, condiciones
IVA, capacidad de solicitudes y puntos de venta. El resumen de Actions muestra los
números de los puntos de venta y si son CAE o están bloqueados; no publica CUIT,
certificados, Token/Sign, CAE ni XML.

Para consultar la numeración, completar además `point_of_sale` y `voucher_type`:
`6` para factura B o `11` para factura C. El resumen indica el último autorizado y
el siguiente candidato. Esa lectura no reserva un número: antes de emitir se vuelve
a comprobar la serie. Utilizar un punto de venta de pruebas dedicado y evitar otras
aplicaciones que emitan concurrentemente en la misma serie.

El selector `services` también admite estas consultas, separadas por comas y sin
espacios. Se necesita autorización del certificado para **cada servicio**:

| Selector | Consulta |
|---|---|
| `wsfex` | Monedas de exportación |
| `wsmtxca` | Monedas de facturación con detalle |
| `wscdc` | Modalidades de constatación |
| `wsfecred` | Tipos de retenciones |
| `padron-a4`, `padron-a5`, `padron-a10`, `padron-a13` | Tres casos públicos: CUIT física, caso publicado como CUIL que hoy devuelve CUIT, y CUIT jurídica |

La ruta A5 corresponde a Constancia de inscripción. No se omiten fallos de permiso
ni respuestas funcionalmente inválidas: la ejecución falla. Las consultas no crean
autorizaciones WSASS ni conceden acceso a otros servicios. WSCPE conserva pruebas
públicas Dummy; su escenario autenticado de negocio queda pendiente de datos y permisos.

## Ejecución completa

Seleccionar `mode=completa`, indicar `point_of_sale` y completar por separado
`voucher_number_b` y `voucher_number_c`. Los números deben ser enteros explícitos
entre 1 y 99999999. El selector `services` no se usa en este modo. Se ejecutan las
nueve consultas autenticadas: WSFE, WSFEX, WSMTXCA, WSCDC, WSFECred y los padrones
A4, A5, A10 y A13. Cada selector informa su resultado y una falla no impide intentar
los demás. Al final, el ensayo falla si hubo alguna consulta fallida.

Luego se comprueban una factura B tipo 6 y una factura C tipo 11, ambas por $121 PES,
con la misma POS y los números independientes indicados. Cada solicitud exige que
su número sea el inmediato siguiente de su propia serie; no se asignan números. Si
la identidad B no alcanza un resultado concluyente, no se inicia C. La autorización
no se reintenta automáticamente: los resultados inciertos se consultan por la misma
identidad y requieren revisión si siguen sin resolverse.

WSFE puede responder el código 602 al listar POS y no devolver filas en homologación.
En ese caso se informa que el listado queda pendiente, sin presentar la consulta
como completa. Para la emisión explícita, el ensayo puede continuar solo con una
respuesta que contenga exactamente ese error y ninguna fila; consulta luego la
última numeración de la combinación POS/tipo y exige el número inmediato siguiente.
Si ARCA sí devuelve la POS, se valida que esté habilitada para CAE y no tenga baja.
Otros errores o respuestas mixtas detienen la emisión. Esta tolerancia se limita a
este harness de homologación.

## Personas de prueba para Padrón

La suite conserva tres casos representativos del [listado público oficial de A4](https://www.afip.gob.ar/ws/ws_sr_padron_a4/datos-prueba-padron-a4.txt),
consultado el 6 de octubre de 2026:

| Identificador público | Tipo de persona | Tipo de clave |
|---|---|---|
| `20188192514` | `FISICA` | `CUIT` |
| `20188027963` | `FISICA` | `CUIT` (publicado en el grupo CUIL) |
| `30202020204` | `JURIDICA` | `CUIT` |

Se mantienen en `tests/NetArcaWs.IntegrationTests/PadronTestCases.cs`, exclusivamente
para homologación. No se consulta la CUIT representada como dato de prueba ni se
usa `ARCA_QUERY_CUIT`. La autenticación continúa usando los secretos del entorno
protegido. Los datos públicos no conceden acceso a los servicios.

El [catálogo oficial](https://www.afip.gob.ar/ws/documentacion/catalogo.asp) enlaza
ese listado solo para A4. No encontramos un listado equivalente para Constancia,
A10 o A13. El 6 de octubre de 2026 los tres casos seleccionados pasaron las doce
consultas locales autenticadas (cuatro servicios), reutilizando tickets privados
y aplicando el mismo validador de la suite: identidad, tipos, datos mínimos y
ausencia de errores funcionales. Esto valida esos casos, no una base compartida.

La CUIT física anterior producía un error del servidor al calcular el identificador
AT en A4/Constancia. Se reemplazó por otra identidad del mismo listado. Los diez
identificadores publicados como CUIL no proporcionaron una respuesta CUIL válida
al contrastarlos: las respuestas obtenidas indicaron CUIT; hubo además un caso
inactivo en A13. Se conserva la procedencia mediante `PublishedKeyType` y la etiqueta
`listado CUIL`, pero el tipo esperado es el realmente observado, CUIT.
**La cobertura autenticada de una respuesta CUIL sigue pendiente.** Las pruebas
sintéticas mantienen la aceptación de CUIL y el rechazo de discrepancias de tipo;
no se flexibilizó el validador ni se modificaron los clientes SOAP. Una ejecución
verde de esta matriz no debe presentarse como validación real de CUIL.

Cada respuesta debe contener datos y coincidir exactamente en identificador,
`tipoPersona` y `tipoClave`. No se infiere el tipo por el prefijo del número.
Si un caso falla, se prueban los siguientes casos y padrones seleccionados; al final
la suite falla si hubo cualquier error. No se omiten casos inexistentes ni se
transforman errores SOAP en resultados aprobados. El resumen informa servicio,
categoría y resultado, sin nombres, domicilios, XML ni mensajes remotos.

Los fallos incluyen una categoría fija para distinguir la causa sin publicar el
contenido de la respuesta:

| Categoría | Significado |
|---|---|
| `AlreadyAuthenticated` | WSAA rechazó un nuevo login con ese código (con o sin prefijo XML válido); no se recuperó el ticket anterior |
| `Authentication` | Otro error SOAP de WSAA |
| `PersonNotFound` | El padrón devolvió el mensaje conocido de persona inexistente |
| `SoapFault` | Otro error SOAP del servicio; no se interpreta como persona inexistente |
| `Transport`, `Timeout` | Fallo HTTP/conectividad o tiempo agotado |
| `InvalidResponse` | XML o respuesta incompatibles con el contrato |
| `MissingPerson` | Falta el nodo de persona o datos generales |
| `FunctionalError` | Constancia devolvió errores funcionales |
| `InvalidPersonData` | La persona carece de los datos mínimos exigidos |
| `IdentityMismatch` | El identificador devuelto difiere del solicitado |
| `PersonTypeMismatch`, `KeyTypeMismatch` | Difiere el tipo de persona o de clave esperado |
| `Unexpected` | Fallo no clasificado; requiere investigación adicional |

Solo se emiten estas etiquetas controladas; no se imprimen códigos o mensajes
arbitrarios del servidor ni valores recibidos. Una categoría señala la primera
comprobación fallida, no prueba que el resto de los campos sea correcto. La
cancelación solicitada detiene la suite y no se confunde con un timeout.

Esta matriz cubre `getPersona`; no acredita las demás operaciones ni reglas de cada
padrón. Las comprobaciones locales usan respuestas sintéticas; la disponibilidad
real de estos casos se registra por ejecución en la issue de homologación.

## Emisión de prueba WSFE

Seleccionar `mode=emision`, `services=wsfe`, `point_of_sale`, `voucher_type` y
`voucher_number`. Los tres valores fiscales deben ser explícitos. El workflow no
elige automáticamente el siguiente número en cada ejecución.

El escenario usa una factura de bienes a consumidor final sin identificación
individual (documento 99/0), condición IVA 5 y fecha argentina del día. Importe
total: **$121 PES**, cotización 1. Para B: neto 100 e IVA 21, alícuota 5. Para C:
neto 121, IVA cero y sin renglones de IVA. El tipo debe estar habilitado para la
CUIT de homologación; no se intenta cambiar su condición tributaria.

El flujo verifica punto de venta activo y CAE, errores de ARCA y última numeración.
Si el número ya existe, exige coincidencia con los datos del escenario y comprueba
su autorización; no vuelve a emitir. Si no existe, solo admite el número inmediato
al último autorizado, solicita el CAE una vez con `SafeInvoiceService` y consulta
el comprobante para confirmar identidad, importes, IVA, fecha, moneda y código.

Si hay un resultado incierto, intenta reconciliar mediante consulta, sin repetir
automáticamente la autorización. Una discrepancia, rechazo o incertidumbre restante
hace fallar el ensayo. No cambiar de número para sortear una ejecución fallida:
consultar primero la identidad enviada. La coincidencia de una factura existente
se informa como tal, no como una nueva emisión. Un reintento en otro día no coincide
con la fecha del escenario y se detiene.

## Alcance de la persistencia y límites

El ticket WSAA y el diario SQLite se conservan en el secreto privado y cifrado
`HOMOLOGATION_STATE` del environment protegido `homologacion`. Cada ejecución los
restaura antes de crear el proveedor de servicios. El mismo TA se reutiliza para
las consultas y ambas series WSFE mientras no haya vencido; la renovación ocurre
solo al expirar. El diario conserva identidades y snapshots fiscales para reconciliar
resultados inciertos entre runs sin reenviar una autorización. No se suben secretos,
tickets, diarios ni respuestas fiscales como artefactos. El bundle comprimido debe
respetar el límite de 48 KiB de GitHub Secrets; si lo supera, la persistencia falla
en cerrado y no se continúa con ARCA. No se eliminan entradas del diario para liberar
espacio automáticamente.

La configuración manual requiere `HOMOLOGATION_STATE_TOKEN`, un fine-grained PAT
con acceso únicamente al repositorio `ARSASWebDesign/NetArcaWs` y permiso de escritura
de environments. El helper usa ese token exclusivamente como credencial del proceso
`gh` que actualiza el secreto del environment; no se copia a logs. Cargar el token
en la interfaz de GitHub, nunca en el chat. La caducidad y rotación del PAT siguen la
política elegida por el mantenedor y se gestionan fuera del harness. `HOMOLOGATION_STATE`
se crea automáticamente en el primer run. No borrarlo ni modificarlo manualmente:
contiene el ticket reutilizable y el diario necesarios para evitar reautenticaciones
o reenvíos inseguros. Si falta el PAT, no está disponible un estado ya creado o falla
la persistencia previa, el ensayo se detiene antes de autenticar o emitir.

Consultas, emisión y completa comparten el grupo global de concurrencia y el mismo
estado protegido. Así se conserva el TA entre runs en vez de forzar un nuevo login
cada diez minutos. El bloqueo de login se comparte por certificado, endpoint y
servicio WSAA; el TA se aísla por tenant, CUIT, ambiente, servicio, endpoint y
certificado. Antes de intentar un login se guarda un bloqueo preventivo de once
minutos, que cubre hasta treinta segundos para actualizar el secreto y una solicitud
WSAA acotada a treinta segundos. Al terminar con éxito, error o cancelación se guarda
la hora real de finalización y se exigen diez minutos antes del próximo login para
esa identidad. Un TA válido se reutiliza hasta su `expirationTime`, sin esperar ni
autenticar de nuevo. El estado falla en cerrado ante contenido inválido, errores de
actualización o situaciones del diario que no permitan demostrar que una identidad
no tuvo un envío anterior.

Las pruebas offline simulan autorización, rechazo, errores y resultados inciertos.
Solo una ejecución protegida exitosa aporta evidencia del circuito contra ARCA.
Este escenario no cubre CAEA, exportación, FCE, notas de crédito, lotes, servicios
ni todas las reglas fiscales. El alcance efectivamente ejecutado se registra en
[la issue de homologación](https://github.com/ARSASWebDesign/NetArcaWs/issues/12).

Referencia de diseño: [manual oficial WSFEv1 4.7](https://www.afip.gob.ar/ws/documentacion/manuales/manual-desarrollador-ARCA-COMPG.pdf),
publicado en el [catálogo de facturación ARCA](https://www.afip.gob.ar/ws/documentacion/ws-factura-electronica.asp).
