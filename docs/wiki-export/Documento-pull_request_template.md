<!-- Source: .github/pull_request_template.md. Generated wiki mirror; edit the repository source. -->

## Qué cambia

Describe el problema y el comportamiento resultante. Indica con claridad si el alcance es una corrección, documentación o una capacidad nueva.

## Alcance y decisiones

- Componentes/API afectados:
- ADR o plan relacionado (si aplica):
- Limitaciones o trabajo pendiente:

## Verificación

- [ ] `dotnet build NetArcaWs.slnx --configuration Release --no-restore`
- [ ] `dotnet test --solution NetArcaWs.slnx --configuration Release --no-build --no-restore`
- [ ] Empaquetado local de los proyectos afectados; si cambió la biblioteca o la CLI, verifiqué ambos paquetes con `dotnet pack`.
- [ ] Actualicé la documentación y el progreso/ADR cuando el alcance o una decisión lo requiere.
- [ ] No ejecuté ni afirmo homologación con ARCA salvo que se haya realizado y documentado explícitamente.

## Seguridad y datos

- [ ] Revisé el diff y eliminé claves, contraseñas, certificados, tokens/Sign, CUIT, datos de tenants y respuestas SOAP reales.
- [ ] Los ejemplos y fixtures agregados usan datos sintéticos.
- [ ] No agregué una dependencia o cambio de licencia sin documentarlo y preservar la atribución y los avisos existentes.

## Entrega

- Issue relacionado:
- Rama basada en `ARSASWebDesign/NetArcaWs:main` y actualizada con `upstream/main`:
- [ ] Este PR apunta a `ARSASWebDesign/NetArcaWs:main`.
- [ ] No se requiere publicar paquetes ni configurar secretos/permisos de publicación para revisar este cambio.
