# Homologación protegida de WSFE

El workflow **Homologación WSFE** se inicia manualmente desde **Actions →
Homologación WSFE → Run workflow**, seleccionando `main`. No se ejecuta al abrir
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

Variables del mismo entorno:

| Nombre | Valor de esta primera etapa |
|---|---|
| `WSAA_SERVICE` | `wsfe` |
| `ARCA_HOMOLOGY_SERVICES` | `wsfe` |

`ARCA_QUERY_CUIT` se reserva para futuras consultas de Padrón; no se necesita
ni se entrega a este workflow. Autorizar el alias/certificado para `wsfe` en
WSASS antes de la ejecución. Un certificado emitido no implica habilitación
para todos los servicios.

## Qué ejecuta

Tras la aprobación, un runner hospedado Ubuntu compila el proyecto sin recibir
el certificado. El paso de prueba materializa los PEM en una carpeta temporal
con permisos privados y ejecuta únicamente la consulta autenticada
`FEParamGetTiposMonedas`. El cliente obtiene el ticket WSAA necesario dentro
de ese mismo proceso. Comprueba un resultado presente, sin errores funcionales,
y una lista no vacía de monedas con identificador y descripción.

La prueba no autoriza comprobantes ni realiza operaciones productivas. Esto
valida el circuito autenticado de consulta WSFE, no la homologación de emisión,
CAEA, otros servicios o todas las reglas fiscales.

Se exige `ARCA_REQUIRE_HOMOLOGY=1`: si faltan selección o credenciales, el trabajo
falla en lugar de omitir la prueba. La CI normal continúa usando pruebas locales
y deja las pruebas reales opt-in sin ejecutar. Este workflow selecciona una
prueba autenticada; no pretende ejecutar ni quitar los skips de todas las suites.

## Seguridad y repetición de pruebas

El checkout usa el commit `main` que originó la ejecución. El workflow y el
script rechazan otros refs/eventos/repositorios; el entorno protege además la
entrega de secretos. Solo ejecutar commits revisados: cualquier código o
dependencia ejecutados en el paso con credenciales podría extraerlas.

Los PEM temporales se eliminan al finalizar, incluso si la prueba falla. No se
publican logs como artefactos, archivos de credenciales ni tickets de acceso;
las verificaciones de negocio usan mensajes genéricos. No activar trazas de
shell ni agregar impresión de XML. Los runners son efímeros y las ejecuciones
se serializan; no se cancela una prueba activa para iniciar otra.

La serialización evita concurrencia, pero **no conserva un ticket entre runs**.
Si WSAA ya concedió un TA que sigue vigente, un nuevo login puede responder
`coe.alreadyAuthenticated`. No se reintenta automáticamente ni se trata ese error
como éxito. Esperar la expiración del TA anterior o implementar un almacén
protegido con coordinación de tickets antes de exigir ejecuciones frecuentes;
ese trabajo continúa en la [issue #9](https://github.com/ARSASWebDesign/NetArcaWs/issues/9).
No usar el caché de Actions ni artefactos descargables para persistir Token/Sign.

La validación real y su evidencia se siguen en la
[issue #12](https://github.com/ARSASWebDesign/NetArcaWs/issues/12). Generar este
workflow o aprobar sus pruebas locales no demuestra que ARCA haya aceptado
una autenticación real.
