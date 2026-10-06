# Releases y publicación en NuGet

El workflow [Release](../.github/workflows/release.yml) distribuye dos paquetes
con la misma versión: `NetArcaWs` (biblioteca) y `NetArcaWs.Tool` (comando
`netarcaws`). El evento `release: published` compila, prueba, empaqueta, verifica
la instalación del tool, adjunta los paquetes a la release y los publica en
nuget.org. Ejecutarlo manualmente (`workflow_dispatch`) **solo valida y genera
artefactos**, sin publicar ni solicitar credenciales NuGet.

## Estado de activación

El usuario NuGet `arsas` fue confirmado y el environment GitHub `nuget` ya está
configurado con `NUGET_USER=arsas`. El mantenedor confirmó la creación de la política NuGet de Trusted Publishing;
su funcionamiento se verifica con la primera publicación. La publicación
externa no está activada hasta completar los pasos siguientes. Una release en
borrador no publica
paquetes. No confundir artefactos de GitHub con paquetes disponibles en nuget.org.

## Configuración inicial del mantenedor

1. Verificar el propietario elegido (cuenta u organización) y sus permisos de publicación. GitHub y NuGet administran cuentas y permisos independientes.
2. En NuGet, crear una política de **Trusted Publishing** bajo el propietario
   elegido. Configurar Repository Owner `ARSASWebDesign`, Repository `NetArcaWs`,
   Workflow File `release.yml` (solo el nombre) y Environment `nuget`.
3. Autorizar creación de paquetes y nuevas versiones para `NetArcaWs` y
   `NetArcaWs.Tool` (por ejemplo, patrón `NetArcaWs*`). Confirmar disponibilidad
   de ambos IDs y la propiedad deseada antes del primer envío.
4. En el environment GitHub `nuget`, definir la variable `NUGET_USER` con el
   **usuario personal de nuget.org que creó la política**, no el email ni asumir
   que el nombre de organización es ese usuario. Puede usarse una variable del
   repositorio si no existe una del environment.
5. El environment limita despliegues a tags `v*`; la validación del workflow exige
   además que el commit pertenezca a `main`. Los mantenedores pueden agregar
   revisores al environment según su proceso de releases.

La autenticación usa OIDC con `NuGet/login`; no requiere una API key permanente
en GitHub. La política otorga una clave temporal al job de publicación. Referencia:
[Trusted Publishing oficial de NuGet](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).

## Preparar una versión

1. Integrar las PR en `main` con CI aprobada.
2. Actualizar `<Version>` en ambos `.csproj` al mismo SemVer. Ajustar referencias
   de versión en documentación y la expectativa del test de `--version` del tool.
   El workflow usa `scripts/release-version.py` para detectar discrepancias.
3. Usar `X.Y.Z` para una release estable o `X.Y.Z-preview.N` para una previa.
   Los identificadores previos deben estar en minúsculas; no usar metadata `+...`.
4. Ejecutar el workflow Release manualmente desde `main`. Revisar los tests,
   instalación del tool y artefactos descargables, antes de publicar.
5. Crear una release GitHub en borrador con tag `vX.Y.Z` exactamente coincidente
   con ambos proyectos y apuntando al commit validado de `main`. Para una versión
   previa, marcar también la release como prerelease.
6. Escribir notas con alcance, cambios, compatibilidad y límites reales. No
   presentar el estado actual del port como facturación o homologación completa.
7. Con la cuenta y política NuGet configuradas, publicar el borrador. Esto activa
   el workflow; comprobar por separado que **ambos** paquetes fueron aceptados e
   indexados en NuGet. El éxito de GitHub Release no demuestra éxito en NuGet.

Los paquetes de la release se construyen una sola vez en el job `build`; el job
`publish` descarga esos artefactos y comprueba `SHA256SUMS`. Se adjunta también
`BUILD_COMMIT` para identificar el código. Los assets existentes deben coincidir
byte por byte: el flujo nunca los reemplaza. Se usa Ubuntu 24.04 y restauración
`--locked-mode` con los lockfiles versionados. Los jobs de PR y forks
no reciben OIDC ni credenciales de publicación. No se usa `pull_request_target`.
El contenido se publica mediante acciones fijadas a commit.

## Fallos y recuperación

La publicación de dos paquetes no es una transacción: la biblioteca puede subirse
aunque después falle el tool. Corregir el problema y reejecutar el mismo workflow
con el mismo tag, preferentemente reejecutando solo el job fallido para reutilizar
los mismos artefactos. Si una reconstrucción difiere de los assets ya adjuntos,
el flujo se detiene y exige resolver la discrepancia, no sobrescribe archivos.
`--skip-duplicate` permite continuar sin reemplazar una versión
que NuGet ya aceptó. Verificar que el paquete existente corresponde a la release;
no usar esta opción para dar por correcta una versión publicada por otra fuente.

No mover ni reutilizar un tag publicado para código diferente. Los paquetes ya
publicados son inmutables; una corrección de código requiere otra versión. Si
falta `NUGET_USER` o la política no coincide, el job falla con un diagnóstico; no
se degrada a una clave permanente ni marca publicación como exitosa.

Los artefactos de los ensayos manuales se retienen durante 30 días. Los assets
adjuntos a una release permiten conservar los paquetes con sus checksums. Los
`.snupkg` no se generan ni se publican en la configuración actual.

## Consumir tras la publicación efectiva

```sh
dotnet add package NetArcaWs --version 0.5.0
dotnet tool install --global NetArcaWs.Tool --version 0.5.0
```

Estos comandos contra nuget.org solo funcionarán después de completar la primera
publicación. Antes de eso, usar los paquetes locales con `--source`/`--add-source`
como se describe en el README y en la guía de la CLI.
