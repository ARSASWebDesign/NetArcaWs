# Pruebas fiscales en homologación

El workflow manual **Homologación ARCA** permite consultar servicios autorizados
y emitir una factura de prueba WSFE. Requiere `main` y la aprobación del entorno
`homologacion`. Todos los clientes de esta suite usan exclusivamente homologación;
el escenario de emisión rechaza un contexto de producción antes de resolver clientes.

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
| `padron-a4`, `padron-a10`, `padron-a13` | Tres casos públicos: CUIT física, CUIL física y CUIT jurídica |
| `padron-a5` | Dos casos públicos: CUIT física y CUIT jurídica (Constancia) |

La ruta A5 corresponde a Constancia de inscripción. No se omiten fallos de permiso
ni respuestas funcionalmente inválidas: la ejecución falla. Las consultas no crean
autorizaciones WSASS ni conceden acceso a otros servicios. WSCPE conserva pruebas
públicas Dummy; su escenario autenticado de negocio queda pendiente de datos y permisos.

## Personas de prueba para Padrón

La suite conserva tres casos representativos del [listado público oficial de A4](https://www.afip.gob.ar/ws/ws_sr_padron_a4/datos-prueba-padron-a4.txt),
consultado el 6 de octubre de 2026:

| Identificador público | Tipo de persona | Tipo de clave |
|---|---|---|
| `20002307554` | `FISICA` | `CUIT` |
| `20203032723` | `FISICA` | `CUIL` |
| `30202020204` | `JURIDICA` | `CUIT` |

Se mantienen en `tests/NetArcaWs.IntegrationTests/PadronTestCases.cs`, exclusivamente
para homologación. No se consulta la CUIT representada como dato de prueba ni se
usa `ARCA_QUERY_CUIT`. La autenticación continúa usando los secretos del entorno
protegido. Los datos públicos no conceden acceso a los servicios.

El [catálogo oficial](https://www.afip.gob.ar/ws/documentacion/catalogo.asp) enlaza
ese listado solo para A4. No encontramos un listado equivalente para Constancia,
A10 o A13: allí se reutilizan como **candidatos pendientes de validación real**,
sin afirmar que compartan la base de testing. Constancia documenta consultas por
CUIT; no se exige un caso CUIL positivo sin evidencia que lo respalde.

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
| `AlreadyAuthenticated` | WSAA rechazó un nuevo login con ese código; no se recuperó el ticket anterior |
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

El diario SQLite se crea en una carpeta temporal privada y se elimina al terminar.
Sirve para comprobar persistencia y reconciliación dentro del ensayo; **no es un
diario durable entre ejecuciones de Actions**. No se sube como artefacto ni se publica
la respuesta fiscal. La numeración explícita y la consulta remota protegen los
reintentos manuales; la concurrencia del workflow no coordina otros consumidores.

El ticket WSAA se reutiliza dentro del mismo proceso. No se conserva entre runs:
si WSAA responde `coe.alreadyAuthenticated`, esperar el vencimiento del ticket
anterior o usar un almacén protegido con coordinación antes de repetir. No usar
cachés públicos ni artefactos para transferir tickets.

Las pruebas offline simulan autorización, rechazo, errores y resultados inciertos.
Solo una ejecución protegida exitosa aporta evidencia del circuito contra ARCA.
Este escenario no cubre CAEA, exportación, FCE, notas de crédito, lotes, servicios
ni todas las reglas fiscales. El alcance efectivamente ejecutado se registra en
[la issue de homologación](https://github.com/ARSASWebDesign/NetArcaWs/issues/12).

Referencia de diseño: [manual oficial WSFEv1 4.7](https://www.afip.gob.ar/ws/documentacion/manuales/manual-desarrollador-ARCA-COMPG.pdf),
publicado en el [catálogo de facturación ARCA](https://www.afip.gob.ar/ws/documentacion/ws-factura-electronica.asp).
