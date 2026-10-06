<!-- Source: docs/wiki/Home.md. Generated wiki mirror; edit the repository source. -->

# NetArcaWs

NetArcaWs es una biblioteca C# para integrar servicios web de ARCA desde .NET
10. Deriva de PyAfipWs y conserva sus avisos de copyright y licencia. La
documentación distingue funciones implementadas, documentación de contratos y
trabajo todavía pendiente; no implica paridad con todo el proyecto Python.

## Guías

- [Inicio rápido](Inicio-rapido) — requisitos, paquetes y primera autenticación.
- [WSAA y certificados](WSAA-y-certificados) — TRA, firma, TA, PEM/PFX y operación.
- [Contexto multitenant](Contexto-multitenant) — identidad confiable, ambiente y límites del caché.
- [Health checks](Health-checks) — probes opt-in sin certificado.
- [Herramienta de certificados](Herramienta-de-certificados) — comandos locales `netarcaws`.
- [Transporte SOAP](Transporte-SOAP) — envío común SOAP 1.1 y límites.
- [Cálculos decimales](Calculos-decimales) — utilitarios aritméticos y límites fiscales.
- [Arquitectura y reintentos](Arquitectura-y-reintentos) — aislamiento, manejo seguro de fallas fiscales y ADR.
- [Diario fiscal](Diario-fiscal) — persistencia SQLite local y coordinación de estados inciertos.
- [Servicios y cobertura](Servicios-y-cobertura) — estado de WSFEv1, WSFEXv1, WSMTXCA, Padrón y upstream.
- [Desarrollo y contribución](Desarrollo-y-contribucion) — build, pruebas, empaquetado y pull requests.

## Estado

La versión actual implementa autenticación WSAA, certificados en memoria,
contexto tenant, health checks, CLI, transporte SOAP, cálculo decimal,
diario/orquestación fiscal y fachadas tipadas para 81 operaciones de WSFEv1,
WSFEXv1, WSMTXCA y Padrón. Hitos 2–6 y su suite están verificados. La build
Release terminó con 0 warnings/errores; la suite tuvo 201 casos (198 aprobados,
3 omitidos), se validaron 81 QName/actions y 166 tipos raíz XML en round-trip.
22 tipos de contrato conservaron campos `DateTime`. Ver [Servicios y cobertura](Servicios-y-cobertura) para límites y estado real de QA. La cobertura documental de PyAfipWs
no significa que los otros módulos estén disponibles en .NET.

NuGet aceptó e indexó `NetArcaWs` y `NetArcaWs.Tool` 0.5.0 mediante Trusted
Publishing OIDC; consultar [Publicar versiones](Publicar-versiones).

En QA real, una corrida de 9 casos respondió los siete probes `Dummy` el
2026-10-06 y omitió las pruebas autenticadas por falta de certificados; no es una
validación fiscal de negocio. Esta wiki publica toda la documentación Markdown
local; el Hito 7 conserva pendientes de revisión exhaustiva del material upstream
y de ejemplos por operación. La revisión publicada figura en `SOURCE_COMMIT`
del repositorio Git de la wiki.

## Fuentes y decisiones

- [README del repositorio](Proyecto) — visión general y ejemplos de API.
- [Arquitectura](Arquitectura-general) — comportamiento y límites técnicos.
- [Progreso](Estado-del-proyecto) — hitos y verificaciones locales.
- [Plan de cierre de hitos 2–7](Plan-remaining-milestones) — entregables.
- [ADR 0001: reintentos seguros](ADR-0001-safe-invoice-retries).
- [ADR 0002: contexto tenant](ADR-0002-arca-tenant-context).
- [ADR 0003: certificados en memoria](ADR-0003-in-memory-certificates).
- [ADR 0004: contratos SOAP](ADR-0004-public-soap-contracts).
- [Contratos de health checks](Referencia-healthchecks-contracts).

Las fuentes oficiales y upstream se enlazan en cada guía. La fecha de consulta
y versión de los manuales debe mantenerse junto a los contratos y los casos de
prueba.
