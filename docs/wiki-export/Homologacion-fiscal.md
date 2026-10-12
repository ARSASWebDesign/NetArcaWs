<!-- Source: docs/wiki/Homologacion-fiscal.md. Generated wiki mirror; edit the repository source. -->

# Homologación fiscal automática

El workflow **Homologación ARCA** ofrece una sola selección pública: el modo de ejecución. La emisión se autoriza al iniciar manualmente `emision` o `completa` desde `main` del repositorio oficial y al aprobar el environment protegido `homologacion`.

| Modo | Ejecución |
|---|---|
| `consultas` | Los nueve selectores de consulta autenticada existentes; no autoriza comprobantes. |
| `emision` | Los 18 casos de la matriz fiscal; no ejecuta consultas generales. |
| `completa` | Las nueve consultas autenticadas y los 18 casos de la matriz. |

La interfaz no solicita servicios, CUIT de receptor, comprador, punto de venta ni números. El código usa fixtures sintéticos y confirma catálogos y series en homologación antes de cada emisión. Una elección de modo de escritura no prueba que ARCA haya habilitado el certificado, emisor, receptor o escenario.

## Prerrequisitos protegidos

El certificado PEM y clave privada deben corresponder a homologación, gestionarse mediante WSASS y estar asociados a WSFEv1, WSFEXv1 y WSMTXCA según se quiera ejecutar la matriz completa. `ARCA_CUIT` identifica al emisor representado y se guarda como secreto, junto con `WSAA_CERTIFICATE_PEM`, `WSAA_PRIVATE_KEY_PEM`, `WSAA_KEY_PASSWORD` si corresponde, `HOMOLOGATION_STATE` y el PAT limitado `HOMOLOGATION_STATE_TOKEN`. No se solicita ni registra el CUIT representado en inputs. La identidad del emisor no se imprime en el resumen.

El test preflight ejecuta los mismos validadores de la matriz antes de materializar PEMs, sin abrir el estado protegido ni crear clientes autenticados. El flujo normal repite la validación. El ticket WSAA se reutiliza mientras siga vigente. El diario fiscal y los metadatos protegidos sobreviven a reintentos del mismo run.

El estado se cifra y guarda en el secreto del environment. El valor Base64 codificado debe permanecer por debajo de 48 KiB; el contenido descomprimido está limitado a 4 MiB. El sistema falla cerrado si supera cualquiera de esos límites, si hay inconsistencia del diario o si no puede persistir cambios. No borrar el secreto ni editarlo manualmente.

## Casos incluidos

La matriz incluye facturas, notas de débito y notas de crédito, en ese orden para conservar la asociación con la factura verificada:

| Servicio | Casos | Tipos |
|---|---|---|
| WSFEv1 | A, B y C | 1/2/3, 6/7/8 y 11/12/13 |
| WSFEXv1 | Exportación E | 19/20/21 |
| WSMTXCA | A y B con detalle | 1/2/3 y 6/7/8 |

Los demás tipos que puedan figurar en catálogos no se ejecutan ni se cuentan como aprobados. WSFECred es un servicio distinto y queda fuera de esta matriz. No hay emisión CAEA ni de lotes.

Las facturas usan datos sintéticos por $121 PES cuando llevan IVA 21 (neto 100 + IVA 21); factura C usa $100 PES sin desglose de IVA. WSMTXCA B representa precios con IVA incluido; conserva neto 100, IVA 21 y total 121. Exportación usa una línea sintética por 100 PES, tipo de exportación de servicios y moneda PES con cotización 1. Las notas incluyen la referencia exacta a la factura A/B/C/E de su misma serie. El resumen presenta servicio, tipo, POS, número, estado, reutilización, asociación e importes esperados; no imprime identidad fiscal, comprador, Token/Sign, CAE ni payload SOAP.

El receptor A es una fixture sintética basada en el receptor RI que aparece en un [ejemplo oficial de comprobante QR de ARCA](https://www.afip.gob.ar/fe/qr/documentos/30000000007_001_00010_00000094.pdf). Su presencia en un ejemplo no demuestra que esté hoy habilitado para una operación; la respuesta de homologación es la autoridad. B/C usan receptor consumidor final tipo 99/0, condición IVA 5.

Para WSFEX se usa una pareja de códigos de país/CUIT-país tomada de un ejemplo de request del [manual oficial](https://www.afip.gob.ar/ws/documentacion/manuales/WSFEX-Manualparaeldesarrollador_V3.1.1_ARCA.pdf), con nombre, domicilio e identificación fiscal extranjera inventados para testing. Los catálogos actuales de país destino, CUIT-país, monedas, idiomas, unidades y tipo de exportación se validan en ejecución. La pareja de ejemplo no prueba habilitación ni que siga vigente; si no coincide con los catálogos actuales, el caso falla antes de preparar una factura.

## POS, numeración y reintentos

Para cada servicio se consulta su catálogo de POS CAE activos, no bloqueados y sin baja. Cuando hay más de uno, se elige determinísticamente el menor POS válido. WSFE, WSFEX y WSMTXCA mantienen elecciones independientes aunque coincidan numéricamente. Si el mismo run ya tiene una operación fiscal congelada, el POS de ese snapshot se restaura y se revalida; no se cambia a otra serie. Si el servicio no publica un POS apto, el caso falla en vez de asumir un valor de ejemplo.

Cada número se obtiene como último autorizado más uno para el servicio/POS/tipo y se persiste con el request fiscal antes del envío. Para WSFEX también se conserva el próximo ID de request. Esa identidad candidata queda congelada en el diario local; ARCA no ofrece una reserva remota. Se vuelve a consultar la serie inmediatamente antes de autorizar. Si el número o ID dejó de ser el siguiente, se detiene; no se cambia automáticamente el número.

`GITHUB_RUN_ID` forma parte de la clave estable de los 18 escenarios. Reintentar el mismo run conserva identidades, fechas, POS y payload congelado. Una nueva ejecución manual tiene otro ID y crea nuevas claves; igualmente verifica y persiste cada serie antes de emitir. Una operación incierta se reconcilia por la misma identidad. No se repite una autorización automáticamente ni se inicia el siguiente caso mientras haya un resultado no terminal. Las notas esperan a que su factura asociada quede autorizada y verificada.

Todas las modalidades comparten un grupo de concurrencia. El estado y el diario quedan protegidos por el environment, y solo los procesos del repositorio oficial desde `main` pueden acceder a esos secretos. El workflow no se ejecuta para PRs ni forks.

## Lectura de resultados

Cada resumen de caso se escribe al completar la consulta exacta posterior y al guardar el avance durable. Si una ejecución se interrumpe, las filas ya informadas corresponden a casos verificados; el caso sin fila debe reanudarse con el mismo run. Si falla una validación de catálogo o un prerrequisito, el caso falla explícitamente: no se cuenta como omitido o exitoso.

El resumen es evidencia por escenario, no homologación universal de la biblioteca. La habilitación de cada emisor y certificado depende de sus relaciones WSASS y las reglas ARCA vigentes. El estado de esta matriz y los resultados de pruebas locales se reportan por separado de la validación protegida en vivo.

## Ejecución autenticada local

En la ejecución local autorizada del 12 de octubre de 2026 se intentaron los nueve selectores autenticados de consulta. Cinco selectores de servicios pasaron; cuatro selectores de padrón fallaron con un resultado genérico del harness o de validación, sin causa exacta confirmada. El selector WSFE permitió un listado parcial según su validador. El diagnóstico tipado consultó directamente los catálogos: WSFE devolvió cero POS con error ARCA 602; WSFEX y WSMTXCA devolvieron cero POS sin código numérico de error. Todos los contadores de filtros fueron cero, así que el resultado no se atribuye a filtros locales. El diagnóstico terminó correctamente, lo que confirma que las consultas se completaron, no que los prerrequisitos fiscales estén satisfechos. Para homologación, el harness prioriza POS activos del catálogo y solo habilita el fixture QA POS 1 de WSFE/WSFEX o POS 2 de WSMTXCA cuando la respuesta tipada del catálogo está vacía y el entorno confirma homologación; este fallback no acredita una habilitación fiscal ni crea una reserva remota.

En la ejecución del modo `emision`, el preflight de los 18 escenarios pasó, pero la matriz fiscal quedó incompleta. Los nueve casos WSFE se confirmaron; la factura A ya estaba autorizada y se verificó sin reenviar, y los otros ocho casos también se autorizaron y confirmaron. Un envío de factura E WSFEX quedó `Unknown`: la respuesta contenía el código 1550, sin resultado de autorización ni campos de identidad para correlacionar. La consulta exacta de la misma identidad devolvió 1020 y el diario quedó en `Conflict` con marcador pendiente. No se reintentó la escritura. El fixture futuro serializa `Permiso_existente` como elemento vacío; este ajuste solo se validó con el contrato y pruebas sintéticas, no contra ARCA. El diario del intento original permanece inmutable y no se considera resuelto. Las dos notas WSFEX y los seis casos WSMTXCA no se iniciaron porque el estado no terminal bloqueó la serie. Resultado observado: 9 de 18 confirmados, 1 `Unknown`/`Conflict`, 8 no ejecutados. Se detuvo la corrida sin más emisiones; esto no equivale a homologación integral ni a validación protegida del modo `completa`.

La inspección local del certificado encontró vencimiento el 5 de octubre de 2028 y correspondencia con su clave privada. El código ARCA 602 de WSFE significa «No existen datos en nuestros registros» según el [manual oficial vigente de WSFE (p. 20)](https://www.afip.gob.ar/ws/documentacion/manuales/manual-desarrollador-ARCA-COMPG.pdf); no indica vencimiento del certificado.

Los probes públicos opt-in de ARCA ejecutaron 21 casos: 13 aprobados, 8 fallidos y 0 omitidos. WSAA público respondió; fallaron los cuatro health checks de padrón por timeout de 20 segundos y los cuatro probes Dummy de padrón por HTTP 500. Estos probes no requieren certificado. En una verificación autenticada dirigida adicional, cuatro consultas individuales de CUIT física a A4/A5/A10/A13 agotaron el tiempo de espera, aunque los cuatro tickets existentes seguían válidos y no hubo un nuevo login; esto no demuestra un problema del certificado. Los cuatro fallos de la verificación completa por selector siguen siendo un resultado separado. La prueba aislada de WSAA autenticó 1/1 con un login directo nuevo; las consultas posteriores de serie y padrón reutilizaron el ticket protegido previo, que seguía válido.

Los opt-in de persistencia en contenedores locales aprobaron 134/134 casos: MySQL 8.4.11 34/34, MariaDB 11.4.13 34/34, PostgreSQL 17.6 33/33 y SQL Server 16.0.4295.3 33/33, con 0 omitidos y 0 fallos. Los contenedores x86_64 corrieron emulados en macOS ARM, por lo que esta ejecución no sustituye la CI nativa.
