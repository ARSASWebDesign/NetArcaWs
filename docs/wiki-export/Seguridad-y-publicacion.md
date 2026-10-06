<!-- Source: docs/wiki/Seguridad-y-publicacion.md. Generated wiki mirror; edit the repository source. -->

# Seguridad del repositorio y publicación

El modelo elegido mantiene la publicación automática al publicar una release.
La revisión humana sucede antes, en la PR. Los responsables son `hlopez94`,
`RARosas` y `davidaragon08`. Una contribución desde un fork no obtiene permisos
para subir paquetes a NuGet.

## Barreras del repositorio

| Control | Efecto |
|---|---|
| PR obligatoria a `main`, sin actores exceptuados | Los commits nuevos pasan por revisión, también los de administradores. |
| Una aprobación de CODEOWNER, distinta de quien hizo el último push | Un colaborador no puede aprobar su propio último cambio. Los tres administradores están declarados en `.github/CODEOWNERS`. |
| Invalidación de aprobaciones ante cambios y conversaciones resueltas | Una aprobación anterior no cubre código agregado después. |
| Checks obligatorios y rama actualizada | `verify`, `CodeQL analysis` y `Dependency review` deben aprobarse. Se vinculan a la aplicación GitHub Actions, no a estados publicados por cualquier integración. |
| Prohibición de borrar `main` y de force push | Evita reemplazar su historia mediante push. |
| Creación restringida de tags `v*` | Solo los tres administradores designados pueden crear tags de release. Un permiso de escritura ordinario no alcanza. |
| Tags `v*` inmutables | Una regla separada impide modificar o borrar tags; no tiene excepciones de administradores. Una corrección requiere otra versión. |
| Escaneo de secretos y push protection | Detecta patrones de credenciales soportados por GitHub y bloquea su introducción. No identifica todo secreto arbitrario. |
| Dependabot, revisión de dependencias y CodeQL | Propone actualizaciones y analiza dependencias/código. La revisión de dependencias bloquea nuevas vulnerabilidades de severidad alta o crítica. |
| Reportes privados de vulnerabilidades | Permite coordinar correcciones sin revelar credenciales o un exploit en un issue público. |

Las reglas son configuración externa de GitHub: copiar los YAML a un fork no
las activa. Los mantenedores deben comprobarlas en
[Rules](https://github.com/ARSASWebDesign/NetArcaWs/rules) y en Settings.
Los cambios administrativos a esas reglas requieren vigilancia y revisión del
registro de auditoría de la organización.

## Aislamiento de Actions y NuGet

- El token de Actions tiene lectura por defecto y no puede aprobar PR.
- Las acciones externas se permiten por lista explícita y deben fijarse a un
  SHA completo. Dependabot propone sus actualizaciones mediante PR.
- Los workflows de contribuyentes externos requieren aprobación de ejecución.
  Se usa `pull_request`; no se ejecuta código de forks con `pull_request_target`.
- CI y compilación de release no reciben OIDC ni credenciales de publicación.
  Los checkouts no conservan el token en la configuración de Git.
- El job `build` restaura lockfiles, compila, prueba, empaqueta y verifica el
  tool. Exige que el commit esté integrado en `main`.
- El job `publish` se ejecuta en un runner separado, descarga los artefactos de
  esa ejecución y verifica versión, tag, commit, nombres y SHA-256. No hace
  checkout, restore, compilación ni ejecuta binarios del repositorio.
- Solo ese job recibe `id-token: write` para obtener una clave temporal de
  NuGet mediante Trusted Publishing. No se guarda una API key permanente.
- El environment `nuget` permite únicamente tags `v*`. Por decisión del
  proyecto no exige aprobación manual adicional; las releases válidas publican
  automáticamente ambos paquetes.
- Los assets existentes de la release se comparan antes de publicar; una
  diferencia cancela el trabajo. No se usa `--clobber` ni se reemplazan paquetes.

Los hashes ayudan a detectar alteraciones o discrepancias, pero no prueban por
sí solos que el código sea seguro. Trusted Publishing autentica el workflow;
no sustituye la revisión del código ni la firma de autor del paquete.
Ver [publicación y firmas NuGet](Publicar-versiones).

## Revisar una contribución

Revisar especialmente cambios en workflows, dependencias/lockfiles, generadores,
criptografía, transporte SOAP y persistencia de emisiones. Comprobar también
`.props`, `.targets`, scripts de build y el contenido final del paquete: MSBuild
y los tests pueden ejecutar código. No ejecutar aportes desconocidos en una
máquina con certificados fiscales o credenciales productivas.

Antes de aprobar, contrastar comportamiento y pruebas; una suite verde no
certifica ausencia de backdoors. No aprobar cambios propios usando otra cuenta
o desactivar controles para acelerar una release. Las contribuciones siguen el
[flujo desde fork y PR](Contribuir).

## Respuesta a incidentes y límites

Reportar por el [canal privado de seguridad](Seguridad-del-proyecto). Ante una
publicación sospechosa, suspender nuevas releases, desactivar la política de
Trusted Publishing comprometida, investigar el commit y los artefactos, y
coordinar con NuGet el retiro de visibilidad o aviso de vulnerabilidad de la
versión. Conservar hashes y logs sin exponer secretos. Una corrección se publica
con un número nuevo.

Un administrador comprometido puede cambiar configuraciones; estas barreras no
protegen contra todas las acciones de un propietario de GitHub o NuGet. Mantener
2FA/passkeys y permisos mínimos en ambas plataformas sigue siendo responsabilidad
de sus titulares. No se modificaron políticas globales de la organización.
La wiki tiene historial propio: sus contenidos no se ejecutan en la publicación
de paquetes. Sus fuentes revisables están en el repositorio principal y
`SOURCE_COMMIT` identifica la revisión publicada.

Fuentes: [rulesets de GitHub](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets),
[permisos de Actions](https://docs.github.com/en/rest/actions/permissions) y
[Trusted Publishing de NuGet](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).
