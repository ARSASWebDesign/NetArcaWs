# Contribuir a NetArcaWs

Gracias por ayudar con el proyecto. Antes de proponer un cambio, revisa el
[README](README.md), el [progreso y alcance por hitos](PROGRESS.md), la
[arquitectura](ARCHITECTURE.md) y los ADR de `docs/adr/`. La matriz de cobertura
distingue clientes disponibles de funcionalidades pendientes; un issue no
representa un compromiso de implementación.

## Preparar una rama desde un fork

Haz un fork en GitHub y clona tu fork (sustituye `<tu-fork>` por tu cuenta):

```sh
git clone https://github.com/<tu-fork>/NetArcaWs.git
cd NetArcaWs
git remote add upstream https://github.com/ARSASWebDesign/NetArcaWs.git
git fetch upstream
git switch --create mi-cambio upstream/main
```

Trabaja en una rama propia, no en `main`. Antes de abrir el pull request,
actualízala con la rama principal del repositorio original y publícala en tu
fork:

```sh
git fetch upstream
git rebase upstream/main
git push --set-upstream origin mi-cambio
```

Abre el pull request desde tu rama hacia `ARSASWebDesign/NetArcaWs:main`.
Resuelve los conflictos en tu rama y vuelve a ejecutar las verificaciones.

## Compilar y verificar

El repositorio fija .NET SDK 10.0.401 en `global.json`. Desde la raíz:

```sh
dotnet restore NetArcaWs.slnx --locked-mode
dotnet build NetArcaWs.slnx --configuration Release --no-restore
dotnet test --solution NetArcaWs.slnx --configuration Release --no-build --no-restore
```

Las pruebas usan xUnit v3, AwesomeAssertions y Microsoft.Testing.Platform. La
prueba de homologación se omite si no se proporcionaron sus variables de
entorno; no agregues credenciales ni certificados al repositorio. Para
comprobar el empaquetado local sin publicar:

```sh
dotnet pack src/NetArcaWs/NetArcaWs.csproj --configuration Release --no-build --no-restore --output artifacts
dotnet pack src/NetArcaWs.Tool/NetArcaWs.Tool.csproj --configuration Release --no-build --no-restore --output artifacts
```

No hace falta publicar paquetes ni solicitar permisos o secretos de publicación
para desarrollar, probar o revisar un cambio. Los chequeos de CI compilan,
ejecutan las pruebas, empaquetan los proyectos y hacen una instalación de prueba
local de la herramienta.

### Entorno local de ChatGPT / Codex

El repositorio incluye [environment.toml](.codex/environments/environment.toml)
para Codex en la aplicación de escritorio de ChatGPT, y [AGENTS.md](AGENTS.md)
con las decisiones e instrucciones para trabajar en el proyecto.

Instala previamente el SDK indicado por `global.json` y asegúrate de que
`dotnet` esté en el PATH del proceso que ejecuta los comandos. Usa el SDK de la
arquitectura del equipo (por ejemplo, Arm64 en Apple Silicon). Las instrucciones
oficiales de instalación cubren [macOS, Linux y Windows](https://learn.microsoft.com/dotnet/core/install/).
Comprueba `dotnet --version` desde la raíz del repositorio; si falta una versión
compatible, el setup se detendrá. No se instala ni cambia el SDK automáticamente.

Abre esta raíz como proyecto en Codex y selecciona el entorno **NetArcaWs** en
los ajustes de entornos locales. El archivo usa la estructura aceptada por la
app: versión 1, setup general, overrides por plataforma y acciones compartidas.
Si lo editas o guardas desde los ajustes, revisa el diff que genere la app.

| Host que ejecuta los comandos | Perfil | Setup |
| --- | --- | --- |
| macOS | `setup.darwin` | Verifica SDK, restaura lockfiles y compila Release |
| Linux, incluido WSL cuando ejecuta allí | `setup.linux` | Verifica SDK, restaura lockfiles y compila Release |
| Windows nativo con PowerShell | `setup.win32` | Verifica SDK, restaura lockfiles y compila Release; detiene el flujo ante errores |

Codex ejecuta el setup al crear un worktree. En un clon existente también puedes
ejecutar los comandos de la sección anterior manualmente. El setup general
restaura con `--locked-mode` si no se aplica un override. No modifica ramas,
remotos, certificados ni configuraciones de la máquina.

Las acciones **Build Release**, **Tests**, **Pack library and tool** y **CLI help**
usan comandos .NET comunes a los tres sistemas, con restauración bloqueada
incluso si todavía no se ejecutó el setup. Tests compila y ejecuta la solución;
Pack genera los dos paquetes en `artifacts/`; CLI help muestra la ayuda sin crear
certificados. No se publican paquetes mediante estas acciones.

El entorno no configura secretos ni habilita pruebas reales de ARCA. La suite
de integración sigue sus condiciones opt-in: si ya configuraste variables de
homologación en tu shell, revísalas antes de ejecutar Tests. Mantén esas
credenciales fuera de este archivo. En macOS usa PEM para certificados en
memoria; la importación efímera de PFX está disponible en Windows/Linux.

Ver [entornos locales de ChatGPT](https://learn.chatgpt.com/docs/environments/local-environment)
y [certificados en memoria](docs/adr/0003-in-memory-certificates.md).

### Licencias de dependencias

Revisa la licencia de cada dependencia nueva y de cada actualización, incluidas
las herramientas de desarrollo. Las pruebas usan AwesomeAssertions 9.6.0
(Apache-2.0), aislado mediante `PrivateAssets="all"`. No agregues
FluentAssertions: desde v8 tiene condiciones comerciales diferentes, y la
revisión de dependencias bloquea su incorporación. La versión 7.2.1 utilizada
anteriormente también era Apache-2.0. Consulta los
[avisos de terceros](THIRD-PARTY-NOTICES.md) para el detalle y las fuentes.

Las actualizaciones automáticas son propuestas de revisión; no garantizan que
una nueva versión conserve la licencia anterior.

## Herramientas de mantenimiento .NET

El desarrollo, CI y las releases no requieren Python. La herramienta interna
`tools/NetArcaWs.Build` usa .NET 10 y no se publica como paquete NuGet:

```sh
dotnet run --project tools/NetArcaWs.Build -- operations
dotnet run --project tools/NetArcaWs.Build -- wiki
dotnet run --project tools/NetArcaWs.Build -- release-version
dotnet tool install dotnet-xscgen --tool-path artifacts/xscgen --version 3.0.1240
dotnet run --project tools/NetArcaWs.Build -- contracts --xscgen artifacts/xscgen/xscgen
```

En Windows, usar `artifacts/xscgen/xscgen.exe`. El wrapper opcional
`scripts/generate-contracts.sh` realiza la instalación temporal y ejecuta el
mismo generador .NET. Revisar los cambios de contratos antes de confirmarlos.
La wiki se genera desde los Markdown versionados; el procedimiento para
publicarla y registrar su revisión está en [el plan de la wiki](docs/plans/hito-7-wiki.md).
La portada `docs/wiki/Home.md` mantiene la navegación temática; GitHub aporta el único menú de páginas. No se genera un segundo `_Sidebar.md`.
El comando interno `release-assets OWNER/REPO TAG DIRECTORY` requiere GitHub CLI
autenticado y verifica los hashes de assets existentes antes de adjuntar los
faltantes; no reemplaza archivos ya publicados.

## Revisión obligatoria y seguridad

Los cambios a `main` requieren una PR, aprobación de un CODEOWNER distinta del
último autor de push, conversaciones resueltas y CI/análisis de seguridad
aprobados sobre la rama actualizada. Los CODEOWNERS son `hlopez94`, `RARosas` y
`davidaragon08`; una nueva modificación invalida aprobaciones anteriores.
Los workflows de contribuyentes externos requieren autorización de ejecución
de un mantenedor y no reciben secretos de publicación. Reporta vulnerabilidades
por el [canal privado](SECURITY.md), nunca en un issue público.
Ver [protecciones del repositorio](docs/wiki/Seguridad-y-publicacion.md).

## Issues y pull requests

Usa los formularios de issue para reportar defectos reproducibles o explicar una
necesidad. En un PR describe el cambio, su alcance, las verificaciones y las
decisiones relacionadas. Actualiza el progreso y agrega o corrige un ADR cuando
la implementación altere el alcance de un hito o introduzca una decisión de
arquitectura. No marques trabajo futuro como implementado ni declares paridad
total con PyAfipWs sin evidencia.

Revisa cuidadosamente todos los archivos del diff antes de enviar. Nunca
incluyas claves privadas, archivos PEM/PFX, contraseñas, tokens o Sign de WSAA,
credenciales, CUIT ni datos de tenants/contribuyentes reales. No adjuntes logs o
mensajes SOAP sin redactar; usa datos sintéticos para reproducir problemas.

## Licencia y atribución

NetArcaWs se distribuye bajo LGPL-3.0-or-later. Al contribuir, conserva los
avisos de copyright y atribución existentes de PyAfipWs y sus autores, y
documenta las nuevas dependencias con sus licencias en
`THIRD-PARTY-NOTICES.md`. No se requiere firmar un CLA adicional para enviar una
contribución.

Los `packages.lock.json` están versionados. Al actualizar dependencias, ejecutar
`dotnet restore NetArcaWs.slnx --force-evaluate`, revisar y subir los lockfiles
modificados. CI usa `--locked-mode` y rechaza cambios no reflejados en ellos.
La publicación corresponde a los mantenedores; ver [releases](docs/releases.md).
