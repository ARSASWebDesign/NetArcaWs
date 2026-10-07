<!-- Source: docs/wiki/Homologacion.md. Generated wiki mirror; edit the repository source. -->

# Homologación protegida de WSFE

## Validación sin certificados ni gestiones ante ARCA

El workflow manual **Validación pública de servicios** ejecuta desde `main`
21 comprobaciones de homologación: diez operaciones Dummy de los clientes SOAP
y once health checks (los diez servicios más el WSDL de WSAA). No recibe secretos,
no solicita tickets y no emite comprobantes. Los diez health checks de servicios
exigen componentes en estado `OK`; el de WSAA verifica su WSDL y la operación
`loginCms`. Obtener simplemente una respuesta HTTP no basta.

Incluye WSFE, WSFEX, WSMTXCA, WSCDC, WSFECred, WSCPE y Padrón A4,
Constancia de inscripción (ruta histórica A5), A10 y A13.
Las dos familias de pruebas se ejecutan aunque falle la primera, para facilitar
el diagnóstico. Una caída externa hace fallar esta validación manual, sin
bloquear la CI habitual por disponibilidad de ARCA.

Para reproducirlas localmente después de compilar en Release:

```sh
ARCA_RUN_HOMOLOGY=1 dotnet test --project tests/NetArcaWs.IntegrationTests/NetArcaWs.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter-method '*Public_health_probe*'
ARCA_RUN_HOMOLOGY=1 dotnet test --project tests/NetArcaWs.IntegrationTests/NetArcaWs.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter-method '*Each_service_answers*'
```

Las pruebas locales adicionales utilizan certificados sintéticos, respuestas SOAP
simuladas y SQLite temporal. Verifican contratos de las 183 operaciones, aislamiento
de credenciales y tickets, errores de transporte, redondeos y recuperación fiscal
sin reenvío automático. La CLI admite inspección de certificados PEM y DER.

Estos controles prueban infraestructura y comportamiento local. No demuestran
autorización del certificado, aceptación fiscal ni reglas de negocio del servidor.
Las pruebas autenticadas y los escenarios de emisión continúan en la
[issue #12](https://github.com/ARSASWebDesign/NetArcaWs/issues/12).

## Ejecución autenticada

El flujo ofrece `consultas`, `emision` y `completa`. La selección de servicios, el
descubrimiento de puntos de venta, la numeración explícita y las condiciones del
ensayo están detallados en [Pruebas fiscales en homologación](Homologacion-fiscal).
El modo predeterminado continúa siendo de solo lectura.

El workflow **Homologación ARCA** se inicia manualmente desde **Actions →
Homologación ARCA → Run workflow**, seleccionando `main`. No se ejecuta al abrir
una PR ni al publicar una release. Usa el entorno GitHub `homologacion`, cuya
política debe permitir únicamente la rama `main` y exigir aprobación manual.
La autoaprobación está permitida para que quien hizo el push pueda iniciar y
aprobar la prueba; los otros administradores también pueden aprobarla. No se
habilita bypass de esa aprobación. Un administrador con permisos para cambiar
la configuración del repositorio sigue siendo parte de la frontera de confianza.

## Credenciales y configuración

Completar estos **secretos del entorno**, nunca archivos del repositorio:

| Nombre | Contenido |
|---|---|
| `WSAA_CERTIFICATE_PEM` | Certificado PEM emitido por WSASS para homologación |
| `WSAA_PRIVATE_KEY_PEM` | Clave privada PEM correspondiente, preferentemente cifrada |
| `WSAA_KEY_PASSWORD` | Contraseña de esa clave; puede quedar vacía si la clave no está cifrada |
| `ARCA_CUIT` | CUIT representada de 11 dígitos, autorizada para el servicio |

Variable del mismo entorno:

| Nombre | Valor de esta primera etapa |
|---|---|
| `WSAA_SERVICE` | `wsfe` |

Configurar en el environment privado el secreto `HOMOLOGATION_STATE_TOKEN`: fine-grained
PAT limitado al repositorio `ARSASWebDesign/NetArcaWs` con permiso de escritura de
environments. El token solo se entrega al proceso `gh` que persiste el estado cifrado;
no se imprime ni se publica. La primera ejecución crea automáticamente
`HOMOLOGATION_STATE` antes de contactar SOAP. No borrar ni modificar manualmente ese
secreto: contiene el TA y el diario fiscal. Su límite es 48 KiB comprimidos; si se
alcanza, el ensayo falla cerrado para preservar el estado.

La selección `services` del formulario reemplaza la variable de entorno
`ARCA_HOMOLOGY_SERVICES`. Padrón consulta los fixtures públicos versionados descritos
en [Pruebas fiscales](Homologacion-fiscal); no requiere `ARCA_QUERY_CUIT`.
La CUIT representada y sus credenciales siguen siendo secretos independientes.
Autorizar el alias/certificado para cada servicio en
WSASS antes de la ejecución. Un certificado emitido no implica habilitación
para todos los servicios.

## Qué ejecuta

Tras la aprobación, un runner hospedado Ubuntu compila el proyecto sin recibir
el certificado. El paso de prueba materializa los PEM en una carpeta temporal
con permisos privados y ejecuta el escenario seleccionado. El cliente obtiene
el ticket WSAA necesario dentro de ese mismo proceso y lo reutiliza en sus
consultas. Los catálogos y respuestas se comprueban con validaciones funcionales.

El modo `consultas` no autoriza comprobantes. El modo `emision` autoriza una
factura de prueba B o C exclusivamente en homologación y la consulta después. El
modo `completa` ejecuta nueve selectores de consulta autenticada y comprueba una
factura B y otra C con números explícitos. Ningún modo realiza operaciones
productivas ni acredita todas las reglas fiscales.

Se exige `ARCA_REQUIRE_HOMOLOGY=1`: si faltan selección o credenciales, el trabajo
falla en lugar de omitir la prueba. La CI normal continúa usando pruebas locales
y deja las pruebas reales opt-in sin ejecutar. Este workflow selecciona las pruebas
autenticadas solicitadas; no pretende quitar los skips de las demás suites.

## Seguridad y repetición de pruebas

El checkout usa el commit `main` que originó la ejecución. El workflow y el
script rechazan otros refs/eventos/repositorios; el entorno protege además la
entrega de secretos. Solo ejecutar commits revisados: cualquier código o
dependencia ejecutados en el paso con credenciales podría extraerlas.

Los PEM temporales se eliminan al finalizar, incluso si la prueba falla. No se
publican logs como artefactos, archivos de credenciales, tickets o diarios fiscales;
las verificaciones de negocio usan mensajes genéricos. No activar trazas de
shell ni agregar impresión de XML. Los runners son efímeros y las ejecuciones
de todos los modos comparten un grupo global de concurrencia; no se cancela una
prueba activa para iniciar otra.

El estado protegido conserva el TA hasta su vencimiento efectivo (`expirationTime`)
y el diario fiscal entre ejecuciones. Las mutaciones del diario se guardan antes
de devolver el control a la llamada SOAP, para que una cancelación o fallo no habilite
un reenvío inseguro en otro run. El bloqueo para nuevo login se comparte por
certificado, endpoint y servicio; antes de autorizarlo exige diez minutos desde el
final del intento previo (incluidos error y cancelación), mientras que un TA vigente
se reutiliza sin nuevo login. `coe.alreadyAuthenticated`
no se trata como éxito ni se reintenta automáticamente. No usar el caché de Actions
ni artefactos descargables para persistir Token/Sign o comprobantes.

La validación real y su evidencia se siguen en la
[issue #12](https://github.com/ARSASWebDesign/NetArcaWs/issues/12). Generar este
workflow o aprobar sus pruebas locales no demuestra que ARCA haya aceptado
una autenticación real.
