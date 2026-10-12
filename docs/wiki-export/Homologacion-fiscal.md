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

En WSMTXCA, `codigoTipoAutorizacion=E` identifica un CAE y `A` un CAEA, según los ejemplos del [manual oficial](https://arca.gob.ar/fe/ayuda/documentos/wsmtxca-RG-2904.pdf). Para unidad de medida 7, `unidadesMtx` es obligatorio y el fixture informa 1. Si no hay otros tributos, el fixture omite `ImporteOtrosTributos` y el array vacío; el contrato marca ambos opcionales y el manual exige que el importe informado sea la suma del detalle.

El receptor A es una fixture sintética basada en el receptor RI que aparece en un [ejemplo oficial de comprobante QR de ARCA](https://www.afip.gob.ar/fe/qr/documentos/30000000007_001_00010_00000094.pdf). Su presencia en un ejemplo no demuestra que esté hoy habilitado para una operación; la respuesta de homologación es la autoridad. WSFE B/C usan receptor consumidor final tipo 99/0, condición IVA 5. WSMTXCA B conserva la condición IVA 5 y omite tipo y número de documento en este fixture de $121, según la regla 128 del manual.

Para WSFEX se usa una pareja de códigos de país/CUIT-país tomada de un ejemplo de request del [manual oficial](https://www.afip.gob.ar/ws/documentacion/manuales/WSFEX-Manualparaeldesarrollador_V3.1.1_ARCA.pdf), con nombre, domicilio e identificación fiscal extranjera inventados para testing. Los catálogos actuales de país destino, CUIT-país, monedas, idiomas, unidades y tipo de exportación se validan en ejecución. La pareja de ejemplo no prueba habilitación ni que siga vigente; si no coincide con los catálogos actuales, el caso falla antes de preparar una factura.

## POS, numeración y reintentos

Para cada servicio se consulta su catálogo de POS CAE activos, no bloqueados y sin baja. Cuando hay más de uno, se elige determinísticamente el menor POS válido. WSMTXCA excluye el POS elegido por WSFE para evitar compartir la misma identidad fiscal. Solo ante una respuesta vacía reconocida de homologación se permiten los fixtures QA fijos: POS 1 para WSFE/WSFEX y POS 2 para WSMTXCA. No se aplica ese fallback en producción, ante otros errores ni para ignorar POS bloqueados. Si el mismo run ya tiene una operación fiscal congelada, el POS de ese snapshot se restaura y se revalida; no se cambia a otra serie.

Cada número se obtiene como último autorizado más uno para el servicio/POS/tipo y se persiste con el request fiscal antes del envío. Para WSFEX también se conserva el próximo ID de request. Esa identidad candidata queda congelada en el diario local; ARCA no ofrece una reserva remota. Se vuelve a consultar la serie inmediatamente antes de autorizar. Si el número o ID dejó de ser el siguiente, se detiene; no se cambia automáticamente el número.

`GITHUB_RUN_ID` forma parte de la clave estable de los 18 escenarios. Reintentar el mismo run conserva identidades, fechas, POS y payload congelado. Una nueva ejecución manual tiene otro ID y crea nuevas claves; igualmente verifica y persiste cada serie antes de emitir. Una operación incierta se reconcilia por la misma identidad. No se repite una autorización automáticamente ni se inicia el siguiente caso mientras haya un resultado no terminal. Las notas esperan a que su factura asociada quede autorizada y verificada.

Todas las modalidades comparten un grupo de concurrencia. El estado y el diario quedan protegidos por el environment, y solo los procesos del repositorio oficial desde `main` pueden acceder a esos secretos. El workflow no se ejecuta para PRs ni forks.

## Recuperación explícita del rechazo WSFEX 1550

La respuesta original de homologación confirmó que `Permiso_existente` debe
enviarse como elemento XML, incluso vacío. El fixture lo serializa de esa forma.
La excepción aprobada permite reconocer únicamente ese rechazo de validación
documentado; no interpreta cualquier error WSFEX como rechazo terminal.

Si una operación anterior quedó incierta, `AdjudicateWsfexValidationRejectionAsync`
requiere la respuesta original íntegra, su snapshot exacto, versión esperada y
metadatos de operador, origen y motivo. Conserva evidencia inmutable y la
operación previa y no llama a ARCA. La aplicación debe autorizar al operador y
verificar el origen de la evidencia. Luego una revisión explícita puede preparar
el tag vacío conservando número e Id; preparar no envía.

Esta capacidad opcional está implementada en el diario SQLite core y su wrapper
protegido. EF Core y los providers personalizados sin la capacidad la rechazan
explícitamente. El ledger SQLite es aditivo y se incluye en el estado protegido.
Ver el [ADR de emisión durable](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/docs/adr/0001-safe-invoice-retries.md)
para las condiciones exactas. El error 1020 de una consulta no permite resolver
el rechazo, liberar la reserva ni cambiar el número.

## Lectura de resultados

Cada resumen de caso se escribe al completar la consulta exacta posterior y al guardar el avance durable. Si una ejecución se interrumpe, las filas ya informadas corresponden a casos verificados; el caso sin fila debe reanudarse con el mismo run. Si falla una validación de catálogo o un prerrequisito, el caso falla explícitamente: no se cuenta como omitido o exitoso.

El resumen es evidencia por escenario, no homologación universal de la biblioteca. La habilitación de cada emisor y certificado depende de sus relaciones WSASS y las reglas ARCA vigentes. El estado de esta matriz y los resultados de pruebas locales se reportan por separado de la validación protegida en vivo.

## Ejecución autenticada local

La última ejecución local autorizada del modo `completa` terminó con código 0: pasaron los nueve selectores y consultas autenticadas (WSFE, WSFEX, WSMTXCA, WSCDC, WSFECred y Padrón A4, A5, A10 y A13), los tres diagnósticos de catálogo POS y los 18 escenarios fiscales con autorización y consulta exacta. El diario terminó con cero operaciones pendientes. Una ejecución anterior había tenido fallos de padrón y catálogos vacíos; no describe el resultado actual. Los probes públicos separados (21 casos: 9 aprobados y 12 fallidos) no forman parte de estos selectores. WSCPE y el Dummy público tampoco se incluyen en esta matriz. El resultado acredita los flujos y escenarios ejecutados, no homologación de toda la biblioteca.

El preflight de los 18 escenarios pasó. Nueve casos WSFE quedaron confirmados. La recuperación auditada WSFEX 1550 preparó una revisión explícita con el mismo número e Id, y ARCA la autorizó; la consulta exacta confirmó identidad, CAE, autorización y contenido. La nota de débito WSFEX tipo 20/Expo 2, inicialmente rechazada con 1673, se adjudicó con evidencia append-only y se revisó quitando únicamente `Fecha_pago`; ARCA autorizó y la consulta exacta verificó esa revisión. La nota de crédito WSFEX también quedó autorizada y verificada. Los tres escenarios WSMTXCA A1/A2/A3 y los tres B6/B7/B8 quedaron autorizados y confirmados por consulta exacta. En total quedaron verificados los 18 escenarios: nueve WSFE, tres WSFEX y seis WSMTXCA.

Para `Obs_comerciales`, `Obs` e `Incoterms`, ausencia y cadena vacía se tratan como representaciones equivalentes. No se recortan ni normalizan otros valores y el payload congelado no se modifica. Solo en el harness, para `TipoExpo` 2 o 4 y si el request no informó valores para `Incoterms` e `Incoterms_Ds`, se acepta una respuesta con `Incoterms` nulo/vacío e `Incoterms_Ds` exactamente `0` (U+0030). No se aceptan espacios, otros textos ni valores explícitos distintos; el core mantiene comparación estricta de `Incoterms_Ds`. `ClsFexRequest` ya expone ambos campos.

La regla 1673 sigue limitada a un error directo con mensaje literal exacto, request WSFEX tipo 20/21, `TipoExpo` 2, fecha válida `yyyyMMdd`, snapshot/identidad/Id coincidentes y respuesta sin `FEXResultAuth`. La operación observada se adjudicó explícitamente; el rechazo, la respuesta original y la auditoría quedaron preservados. La revisión corregida fue autorizada y confirmada por consulta exacta. No hay reclasificación automática ni se extiende esta regla a otros errores, tipos o valores de `TipoExpo`.

Las autorizaciones WSMTXCA A incluyeron `Resultado=O` y observaciones 130 y 504; B6/B7/B8 incluyeron `Resultado=O` y observación 504. No hubo errores en esas respuestas y las consultas exactas confirmaron identidad y contenido. B6 había sido rechazada inicialmente con error 132. El request usaba `codigoTipoDocumento=99` y `numeroDocumento=0`, valores de la fixture de consumidor final usada en WSFE que no son válidos para MTXCA. Se preparó una revisión explícita de la misma identidad, omitiendo solo esos campos conforme a la regla 128 del manual. Se preservaron el snapshot original y su hash; no se usó la adjudicación del core para WSFEX 1550/1673. La revisión y los otros dos escenarios B quedaron autorizados y verificados por consulta exacta. La consulta exacta WSMTXCA había mostrado 12 diferencias únicamente de escala léxica en campos XSD `decimal`, con valores numéricos equivalentes; no halló diferencias de GTIN, unidad ni valores por defecto. El comparador del core y del harness compara numéricamente solo los paths XSD `decimal` permitidos, sin mutar el payload fiscal congelado ni su hash; la corrida final completó las consultas exactas de los seis escenarios A/B.

El checkpoint local más reciente reportó restore locked y build Release con 0 advertencias/errores y 907 pruebas: 865 aprobadas, 42 opt-in omitidas y 0 fallos. Los packs finales de biblioteca y herramienta pasaron; el consumer limpio con la capability opcional y el esquema SQLite también pasó. No hubo CI de GitHub ni publicación para estas validaciones; tampoco se probó toda la biblioteca ni se incluyeron WSCPE o los probes públicos separados.

La inspección local del certificado encontró vencimiento el 5 de octubre de 2028 y correspondencia con su clave privada. El código ARCA 602 de WSFE significa «No existen datos en nuestros registros» según el [manual oficial vigente de WSFE (p. 20)](https://www.afip.gob.ar/ws/documentacion/manuales/manual-desarrollador-ARCA-COMPG.pdf); no indica vencimiento del certificado.

La fábrica WSFEX informa `Fecha_pago` solo para comprobantes tipo 19 y la omite para tipos 20/21; la revisión autorizada de la nota tipo 20 se confirmó con el mismo número e Id.

Los probes públicos opt-in separados ejecutaron 21 casos: 9 aprobados, 12 fallidos y 0 omitidos. WSDL respondió HTTP 200 con .NET y curl, pero ambos agotaron el tiempo al enviar el mismo Dummy; los checks/probes públicos de padrón fallaron por HTTP 500 o timeout. En intentos autenticados anteriores, cuatro consultas dirigidas de padrón fallaron por HTTP 500/timeout pese a reutilizar tickets válidos. En la ejecución actual del modo `completa`, los nueve selectores y consultas autenticadas pasaron. Estos resultados distintos no demuestran un problema del certificado. No se cambiaron endpoints, umbrales ni validadores para ocultar fallos externos.

Los opt-in de persistencia en contenedores locales aprobaron 134/134 casos: MySQL 8.4.11 34/34, MariaDB 11.4.13 34/34, PostgreSQL 17.6 33/33 y SQL Server 16.0.4295.3 33/33, con 0 omitidos y 0 fallos. Los contenedores x86_64 corrieron emulados en macOS ARM, por lo que esta ejecución no sustituye la CI nativa.
