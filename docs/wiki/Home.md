# NetArcaWs

NetArcaWs es una biblioteca C# para integrar servicios web de ARCA desde .NET
10. Deriva de PyAfipWs y conserva sus avisos de copyright y licencia. La
documentación distingue funciones implementadas, documentación de contratos y
trabajo todavía pendiente; no implica paridad con todo el proyecto Python.

## Guías

- [[Inicio rápido]] — requisitos, paquetes y primera autenticación.
- [[WSAA y certificados]] — TRA, firma, TA, PEM/PFX y operación.
- [[Contexto multitenant]] — identidad confiable, ambiente y límites del caché.
- [[Health checks]] — probes opt-in sin certificado.
- [[Herramienta de certificados]] — comandos locales `netarcaws`.
- [[Transporte SOAP]] — envío común SOAP 1.1 y límites.
- [[Cálculos decimales]] — utilitarios aritméticos y límites fiscales.
- [[Arquitectura y reintentos]] — aislamiento, manejo seguro de fallas fiscales y ADR.
- [[Diario fiscal]] — persistencia SQLite local y coordinación de estados inciertos.
- [[Servicios y cobertura]] — estado de WSFEv1, WSFEXv1, WSMTXCA, Padrón y upstream.
- [[Desarrollo y contribución]] — build, pruebas, empaquetado y pull requests.

## Estado

La versión actual implementa autenticación WSAA, certificados en memoria,
contexto tenant, health checks, CLI, transporte SOAP, cálculo decimal,
diario/orquestación fiscal y fachadas tipadas para 81 operaciones de WSFEv1,
WSFEXv1, WSMTXCA y Padrón. Hitos 2–6 y su suite están verificados. La build
Release terminó con 0 warnings/errores; la suite tuvo 222 casos (219 aprobados,
3 omitidos), se validaron 81 QName/actions y 166 tipos raíz XML en round-trip.
22 tipos de contrato conservaron campos `DateTime`. Ver [[Servicios y
cobertura]] para límites y estado real de QA. La cobertura documental de PyAfipWs
no significa que los otros módulos estén disponibles en .NET.

NuGet aceptó e indexó `NetArcaWs` y `NetArcaWs.Tool` 0.5.0 mediante Trusted
Publishing OIDC; consultar [[Publicar versiones]].

En QA real, una corrida de 9 casos respondió los siete probes `Dummy` el
2026-10-06 y omitió las pruebas autenticadas por falta de certificados; no es una
validación fiscal de negocio. Esta wiki publica toda la documentación Markdown
local; el Hito 7 conserva pendientes de revisión exhaustiva del material upstream
y de ejemplos por operación. La revisión publicada figura en `SOURCE_COMMIT`
del repositorio Git de la wiki.

## Fuentes y decisiones

- [README del repositorio](../../README.md) — visión general y ejemplos de API.
- [Arquitectura](../../ARCHITECTURE.md) — comportamiento y límites técnicos.
- [Progreso](../../PROGRESS.md) — hitos y verificaciones locales.
- [Plan de cierre de hitos 2–7](../plans/remaining-milestones.md) — entregables.
- [ADR 0001: reintentos seguros](../adr/0001-safe-invoice-retries.md).
- [ADR 0002: contexto tenant](../adr/0002-arca-tenant-context.md).
- [ADR 0003: certificados en memoria](../adr/0003-in-memory-certificates.md).
- [ADR 0004: contratos SOAP](../adr/0004-public-soap-contracts.md).
- [Contratos de health checks](../reference/healthchecks-contracts.md).

Las fuentes oficiales y upstream se enlazan en cada guía. La fecha de consulta
y versión de los manuales debe mantenerse junto a los contratos y los casos de
prueba.
