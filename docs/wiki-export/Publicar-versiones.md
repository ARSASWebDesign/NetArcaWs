<!-- Source: docs/releases.md. Generated wiki mirror; edit the repository source. -->

# Releases y publicación en NuGet

El workflow [Release](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/.github/workflows/release.yml) distribuye once paquetes
con la misma versión: `NetArcaWs`, `NetArcaWs.Tool`, cuatro paquetes EF Core de
persistencia/proveedores y cinco de migraciones. El evento `release: published`
compila, prueba, verifica los snapshots de schemas y migraciones, empaqueta,
prueba la instalación del tool y un consumidor aislado de persistencia, adjunta
los once paquetes a la release y los publica en nuget.org. Ejecutarlo
manualmente (`workflow_dispatch`) **solo valida y genera artefactos**, sin
publicar ni solicitar credenciales NuGet.

## Estado de activación

El usuario NuGet `arsas`, el environment GitHub `nuget` (`NUGET_USER=arsas`) y la
política de Trusted Publishing están configurados. La primera publicación
completó el intercambio OIDC y NuGet aceptó los dos paquetes 0.5.0. La
[ejecución de publicación](https://github.com/ARSASWebDesign/NetArcaWs/actions/runs/37477043984)
registró `Your package was pushed` para ambos; ambos aparecen en el índice v3 de
NuGet. La [release v0.5.0](https://github.com/ARSASWebDesign/NetArcaWs/releases/tag/v0.5.0)
está publicada. La instalación pública de `NetArcaWs.Tool` 0.5.0 desde
`api.nuget.org` en una caché aislada y la ejecución de `--help` terminaron con
código 0. Esa prueba no verifica la firma de autor del paquete. Una release en
borrador no inicia la publicación.

La versión 0.6.0 agrupa once paquetes: `NetArcaWs`, `NetArcaWs.Tool`,
`NetArcaWs.EntityFrameworkCore`, sus providers `MySql`, `PostgreSql` y
`SqlServer`, y los paquetes de migraciones `Sqlite`, `MySql`, `MariaDb`,
`PostgreSql` y `SqlServer`. Consultá la disponibilidad actual de cada ID y
versión en NuGet; estos enlaces y badges reflejan el feed, sin depender del
estado de una release de GitHub:

| Paquete | Última versión en NuGet; enlace a 0.6.0 |
| --- | --- |
| `NetArcaWs` | [![NuGet](https://img.shields.io/nuget/v/NetArcaWs)](https://www.nuget.org/packages/NetArcaWs) · [0.6.0](https://www.nuget.org/packages/NetArcaWs/0.6.0) |
| `NetArcaWs.Tool` | [![NuGet](https://img.shields.io/nuget/v/NetArcaWs.Tool)](https://www.nuget.org/packages/NetArcaWs.Tool) · [0.6.0](https://www.nuget.org/packages/NetArcaWs.Tool/0.6.0) |
| `NetArcaWs.EntityFrameworkCore` | [![NuGet](https://img.shields.io/nuget/v/NetArcaWs.EntityFrameworkCore)](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore) · [0.6.0](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore/0.6.0) |
| `NetArcaWs.EntityFrameworkCore.MySql` | [![NuGet](https://img.shields.io/nuget/v/NetArcaWs.EntityFrameworkCore.MySql)](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.MySql) · [0.6.0](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.MySql/0.6.0) |
| `NetArcaWs.EntityFrameworkCore.PostgreSql` | [![NuGet](https://img.shields.io/nuget/v/NetArcaWs.EntityFrameworkCore.PostgreSql)](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.PostgreSql) · [0.6.0](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.PostgreSql/0.6.0) |
| `NetArcaWs.EntityFrameworkCore.SqlServer` | [![NuGet](https://img.shields.io/nuget/v/NetArcaWs.EntityFrameworkCore.SqlServer)](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.SqlServer) · [0.6.0](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.SqlServer/0.6.0) |
| `NetArcaWs.EntityFrameworkCore.Migrations.Sqlite` | [![NuGet](https://img.shields.io/nuget/v/NetArcaWs.EntityFrameworkCore.Migrations.Sqlite)](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.Migrations.Sqlite) · [0.6.0](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.Migrations.Sqlite/0.6.0) |
| `NetArcaWs.EntityFrameworkCore.Migrations.MySql` | [![NuGet](https://img.shields.io/nuget/v/NetArcaWs.EntityFrameworkCore.Migrations.MySql)](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.Migrations.MySql) · [0.6.0](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.Migrations.MySql/0.6.0) |
| `NetArcaWs.EntityFrameworkCore.Migrations.MariaDb` | [![NuGet](https://img.shields.io/nuget/v/NetArcaWs.EntityFrameworkCore.Migrations.MariaDb)](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.Migrations.MariaDb) · [0.6.0](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.Migrations.MariaDb/0.6.0) |
| `NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql` | [![NuGet](https://img.shields.io/nuget/v/NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql)](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql) · [0.6.0](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql/0.6.0) |
| `NetArcaWs.EntityFrameworkCore.Migrations.SqlServer` | [![NuGet](https://img.shields.io/nuget/v/NetArcaWs.EntityFrameworkCore.Migrations.SqlServer)](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.Migrations.SqlServer) · [0.6.0](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.Migrations.SqlServer/0.6.0) |

La publicación se verifica por separado para cada paquete: el éxito de la
release de GitHub no demuestra que los once IDs hayan sido aceptados e
indexados. La publicación de once paquetes tampoco es una transacción; una
falla parcial requiere verificar cada ID y versión.

El alcance previsto incluye persistencia relacional optativa para diario fiscal
y tickets WSAA, providers para MySQL/MariaDB, PostgreSQL y SQL Server, cinco
paquetes de migraciones oficiales y ejemplos/documentación de consumo. El
consumidor conserva la responsabilidad de autorizar tenants, proteger claves,
programar la recuperación y aplicar migraciones explícitamente. La release no
incorpora almacenamiento de certificados ni un worker de recuperación; las
migraciones no adoptan automáticamente tablas previas sin historial. Las
pruebas locales y de motores relacionales no equivalen a homologación fiscal
completa contra ARCA.

## Configuración inicial del mantenedor

Los pasos siguientes describen la configuración inicial para un fork o una
rotación/recreación de la integración. En el repositorio principal ya se
completaron y verificaron en la ejecución enlazada arriba.

1. Verificar el propietario elegido (cuenta u organización) y sus permisos de publicación. GitHub y NuGet administran cuentas y permisos independientes.
2. En NuGet, crear una política de **Trusted Publishing** bajo el propietario
   elegido. Configurar Repository Owner `ARSASWebDesign`, Repository `NetArcaWs`,
   Workflow File `release.yml` (solo el nombre) y Environment `nuget`.
3. Autorizar creación de paquetes y nuevas versiones para los once IDs de esta
   release (por ejemplo, patrón `NetArcaWs*`). Confirmar disponibilidad de cada
   ID y la propiedad deseada antes del primer envío.
4. En el environment GitHub `nuget`, definir la variable `NUGET_USER` con el
   **usuario personal de nuget.org que creó la política**, no el email ni asumir
   que el nombre de organización es ese usuario. Puede usarse una variable del
   repositorio si no existe una del environment.
5. El environment limita despliegues a tags `v*`; la validación del workflow exige
   además que el commit pertenezca a `main`. En este proyecto la publicación es
   automática, sin aprobación manual del environment: la revisión ocurre en la
   PR y solo los tres administradores autorizados pueden crear tags de release.
   Los tags no se pueden mover ni eliminar. Ver [protecciones](Seguridad-y-publicacion).

La autenticación usa OIDC con `NuGet/login`; no requiere una API key permanente
en GitHub. La política otorga una clave temporal al job de publicación. Referencia:
[Trusted Publishing oficial de NuGet](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).

## Firma de los paquetes

Trusted Publishing autentica al workflow mediante OIDC; no es una firma de autor
del archivo `.nupkg`. NuGet.org aplica su firma de repositorio a los paquetes
aceptados. Los assets de GitHub se verifican mediante `SHA256SUMS` y
`BUILD_COMMIT`, pero esta versión no incorpora firma de autor de Arsas.

Agregar firma de autor requiere un certificado de firma de código aceptado por
NuGet y registrado previamente en la cuenta, custodia segura de su clave privada
y timestamp de la firma. Debe firmarse antes de generar checksums y distribuir
los artefactos. Los certificados fiscales de ARCA no se reutilizan para este fin.
No se almacena una clave privada de firma de paquetes en el repositorio.
Referencias: [firmas NuGet](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference)
y [requisitos de firma de autor](https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package).

## Preparar una versión

1. Integrar las PR en `main` con CI aprobada.
2. Actualizar `<Version>` en los once proyectos empaquetables a un único SemVer.
   Ajustar la expectativa del test de `--version` del tool. El workflow usa
   `NetArcaWs.Build release-version`, con allowlist explícita, para detectar
   versiones distintas o proyectos de paquete faltantes/inesperados.
3. Usar `X.Y.Z` para una release estable o `X.Y.Z-preview.N` para una previa.
   Los identificadores previos deben estar en minúsculas; no usar metadata `+...`.
4. Ejecutar el workflow Release manualmente desde `main`. Revisar los tests,
   instalación del tool y artefactos descargables, antes de publicar.
5. Crear una release GitHub en borrador con tag `vX.Y.Z` exactamente coincidente
   con los once proyectos y apuntando al commit validado de `main`. Para una versión
   previa, marcar también la release como prerelease.
6. Escribir notas con alcance, cambios, compatibilidad y límites reales. No
   presentar el estado actual del port como facturación o homologación completa.
7. Con la cuenta y política NuGet configuradas, publicar el borrador. Esto activa
   el workflow; comprobar por separado que **los once** paquetes fueron aceptados e
   indexados en NuGet. El éxito de GitHub Release no demuestra éxito en NuGet.

Los paquetes de la release se construyen una sola vez en el job `build`; el job
`publish` descarga esos artefactos y comprueba `SHA256SUMS`, identidad de versión y commit. No hace checkout, restore ni ejecuta código del repositorio con credenciales de publicación. Se adjunta también
`BUILD_COMMIT` para identificar el código. Los assets existentes deben coincidir
byte por byte: el flujo nunca los reemplaza. Se usa Ubuntu 24.04 y restauración
`--locked-mode` con los lockfiles versionados. Los jobs de PR y forks
no reciben OIDC ni credenciales de publicación. No se usa `pull_request_target`.
El contenido se publica mediante acciones fijadas a commit.

Antes de solicitar el login OIDC de NuGet, el workflow consulta el flat-container
público para cada ID y versión. Un 404 significa que todavía no existe ese
paquete; un 200 exige comparar todos los nombres y bytes del ZIP con el artefacto
construido, ignorando únicamente la firma de repositorio en la entrada raíz
`.signature.p7s`. Las entradas duplicadas, contenido distinto y respuestas HTTP
distintas de 200/404 detienen la publicación. Solo los IDs existentes con
contenido idéntico se marcan para omitir su envío. Si otro actor publica después
de esta comprobación, el push normal falla y la ejecución debe repetirse para
comparar de nuevo el contenido antes de omitirlo.

## Fallos y recuperación

La publicación de once paquetes no es una transacción: algunos pueden subirse
aunque después falle otro. Corregir el problema y reejecutar el mismo workflow
con el mismo tag para que vuelva a comparar cada versión existente antes de
continuar. Si una reconstrucción difiere de los assets ya adjuntos o de un
paquete NuGet existente, el flujo se detiene y exige resolver la discrepancia;
nunca reemplaza assets ni usa `--skip-duplicate` para aceptar una versión
preexistente sin verificar su contenido.

No mover ni reutilizar un tag publicado para código diferente. Los paquetes ya
publicados son inmutables; una corrección de código requiere otra versión. Si
falta `NUGET_USER` o la política no coincide, el job falla con un diagnóstico; no
se degrada a una clave permanente ni marca publicación como exitosa.

Los artefactos de los ensayos manuales se retienen durante 30 días. Los assets
adjuntos a una release permiten conservar los paquetes con sus checksums. Los
`.snupkg` no se generan ni se publican en la configuración actual.

## Consumir desde NuGet

```sh
dotnet add package NetArcaWs --version 0.5.0
dotnet tool install --global NetArcaWs.Tool --version 0.5.0
```

La publicación 0.5.0 ya está indexada en NuGet. También se pueden usar los
paquetes locales con `--source`/`--add-source` como se describe en el README y en
la guía de la CLI.
