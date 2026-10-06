# Contribuir a NetArcaWs

Gracias por ayudar con el proyecto. Antes de proponer un cambio, revisa el
[README](README.md), el [progreso y alcance por hitos](PROGRESS.md), la
[arquitectura](ARCHITECTURE.md) y los ADR de `docs/adr/`. Los clientes de
negocio de WSFEv1, WSFEXv1, WSMTXCA y Padrón siguen pendientes; una propuesta o
un issue no representa un compromiso de implementación.

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

Las pruebas usan xUnit v3, FluentAssertions y Microsoft.Testing.Platform. La
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

## Herramientas de mantenimiento .NET

El desarrollo, CI y las releases no requieren Python. La herramienta interna
`tools/NetArcaWs.Build` usa .NET 10 y no se publica como paquete NuGet:

```sh
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
La organización manual de la barra lateral se conserva en `docs/wiki-sidebar.txt`.
El comando interno `release-assets OWNER/REPO TAG DIRECTORY` requiere GitHub CLI
autenticado y verifica los hashes de assets existentes antes de adjuntar los
faltantes; no reemplaza archivos ya publicados.

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
