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
| `padron-a4`, `padron-a5`, `padron-a10`, `padron-a13` | Persona de prueba; requiere secreto `ARCA_QUERY_CUIT` |

La ruta A5 corresponde a Constancia de inscripción. No se omiten fallos de permiso
ni respuestas funcionalmente inválidas: la ejecución falla. Las consultas no crean
autorizaciones WSASS ni conceden acceso a otros servicios. WSCPE conserva pruebas
públicas Dummy; su escenario autenticado de negocio queda pendiente de datos y permisos.

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
