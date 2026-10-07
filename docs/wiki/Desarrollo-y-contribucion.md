# Desarrollo y contribución

Usar .NET SDK `10.0.401`, que el repositorio fija en `global.json`. Desde la raíz:

```sh
dotnet restore NetArcaWs.slnx
dotnet build NetArcaWs.slnx --configuration Release --no-restore
dotnet test --solution NetArcaWs.slnx --configuration Release --no-build --no-restore
dotnet pack src/NetArcaWs/NetArcaWs.csproj --configuration Release --no-build --no-restore --output artifacts
dotnet pack src/NetArcaWs.Tool/NetArcaWs.Tool.csproj --configuration Release --no-build --no-restore --output artifacts
```

Los proyectos de pruebas usan xUnit v3, AwesomeAssertions y
Microsoft.Testing.Platform. Las pruebas unitarias usan datos sintéticos. La
prueba de homologación requiere variables de entorno con credenciales válidas;
sin ellas se omite. No guardar credenciales en fixtures, logs o commits y no
ejecutar prueba productiva.

La [corrida de CI de referencia](https://github.com/ARSASWebDesign/NetArcaWs/actions/runs/37555719696)
registra build, suite local, motores de persistencia, migraciones, paquetes y
consumer/CLI. Las pruebas usan datos sintéticos. CI no acredita autenticación ni
homologación fiscal; los badges de [Inicio](Home.md) muestran las versiones
publicadas y la [disponibilidad por paquete](../releases.md#estado-de-activación)
permite comprobar cada ID de NuGet. La primera release 0.5.0 fue aceptada e
indexada mediante OIDC.

La CI realiza restore, build Release, suite completa, empaquetado y smoke tests
de los paquetes NuGet y la herramienta desde el feed local. No publica paquetes
ni requiere permisos de registry para una contribución.

## Pull request desde un fork

1. Crear fork y clonar su URL.
2. Configurar `upstream` como `https://github.com/ARSASWebDesign/NetArcaWs.git`.
3. Crear rama desde `upstream/main`; antes del PR ejecutar `git fetch upstream`
   y `git rebase upstream/main`.
4. Subir la rama al fork y abrir el PR hacia
   `ARSASWebDesign/NetArcaWs:main`.
5. Describir alcance, pruebas, ADR/progreso y limitaciones. No marcar trabajo
   futuro como implementado y no afirmar homologación si no se ejecutó.

Conservar los avisos de copyright y atribución de PyAfipWs. El proyecto está bajo
LGPL-3.0-or-later; consultar `LICENSE`, `COPYING` y
`THIRD-PARTY-NOTICES.md`. No se requiere CLA nuevo. Ver
[CONTRIBUTING.md](../../CONTRIBUTING.md) para instrucciones vigentes y
[formularios de issue/PR](../../.github) en el repositorio.
