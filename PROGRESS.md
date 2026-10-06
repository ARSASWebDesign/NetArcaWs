# Progreso de NetArcaWs

- [x] Automatizaciones de wiki, contratos y releases migradas a `NetArcaWs.Build` (.NET 10), sin scripts Python.

- [x] Seguridad: revisión CODEOWNER/CI, análisis CodeQL y dependencias, protección de secretos, tags inmutables y publicación automática OIDC aislada del código de compilación.
- [x] Plantillas de issues/PR y guía CONTRIBUTING para forks.
- [x] Workflow de releases de biblioteca y tool con ensayo manual sin publicación.
- [x] Configurar Trusted Publishing y publicar `NetArcaWs` y `NetArcaWs.Tool` 0.5.0 en NuGet.
- [x] Confirmar la indexación de ambos paquetes 0.5.0 en el feed NuGet.
- [x] Hito 1: WSAA nativo, NuGet LGPL-3.0-or-later y xUnit; aserciones migradas a AwesomeAssertions 9.6.0 (Apache-2.0).
- [x] Dependencias de pruebas: reemplazar FluentAssertions, actualizar lockfiles y avisos de licencia, y configurar bloqueo de nuevas incorporaciones en dependency review. Build y pack verificados; 222 tests aprobados y 3 de homologación omitidos.
- [x] CLI .NET tool: cert-dev, cert-prod, cert-info, paquete y suite propia.
- [x] Certificados PEM/PFX en memoria desde configuración, vault o base de datos.
- [x] Contexto multitenant y aislamiento de caché WSAA por tenant/CUIT/entorno/servicio/certificado.
- [x] Integración del contexto multitenant en las fachadas autenticadas de los hitos 2–5; autorización de tenant y certificado productivo siguen a cargo de la aplicación.
- [x] Health checks independientes y opt-in por WS, sin credenciales.
- [x] Diseño documentado de reintentos y reconciliación para evitar duplicados.
- [x] `SafeInvoiceService`, diario durable y reconciliación conservadora para emisión unitaria WSFE/WSFEX/WSMTXCA; SQLite local multi-proceso en un host, no NFS ni multi-host.
- [ ] WSAA: ejecutar homologación real con certificados autorizados.
- [x] Hito 2: WSFEv1 — 22 operaciones, contrato SOAP y suite verificados.
- [x] Hito 3: WSFEXv1 — 19 operaciones, contrato SOAP y suite verificados.
- [x] Hito 4: WSMTXCA — 27 operaciones, contrato SOAP y suite verificados; no equivale al ciclo integral de Factura de Crédito Electrónica MiPyME.
- [x] Hito 5: Padrón A4 (2), Constancia/ruta A5 (5), A10 (2), A13 (4) — contratos, fachada y suite verificados.
- [x] WSCDC (6) y WSFECred (21): contratos QA/producción, fachadas multitenant, DI, health checks y referencias de las 27 operaciones. WSFECred permanece fuera de `SafeInvoiceService`/diario.
- [x] Hito 6: README, arquitectura, ADR y guías integrales actualizados; suite local Release verificada.
- [x] Publicar en la wiki toda la documentación Markdown local, con navegación y revisión de origen identificada.
- [x] Hito 7: catálogo por servicios, extras, referencias y ejemplos compilables de las 108 operaciones; seguimiento de servicios restantes en [issue #10](https://github.com/ARSASWebDesign/NetArcaWs/issues/10).

La verificación actual terminó con build Release de 0 warnings y 0 errors; la
suite tuvo 240 casos: 237 aprobados y 3 omitidos. Se verificaron las 108
operaciones contra sus WSDL y 219 tipos raíz XML en round-trip. Los 30 tipos de
contrato con campos `DateTime` (`xs:date`/`xs:dateTime`) conservaron sus valores.

En homologación real de QA con `ARCA_RUN_HOMOLOGY=1` el 2026-10-06, la corrida
de integración tuvo 11 casos: 9 probes `Dummy` aprobados y 2 pruebas autenticadas
omitidas. En la suite normal, los 3 omitidos corresponden a WSAA sin credenciales,
servicios autenticados no seleccionados y `Dummy` sin habilitación opt-in. Ningún
resultado acredita una autorización de negocio con certificado real.

El seguimiento vigente de autenticación y escenarios ejecutados se registra en
[la issue de homologación](https://github.com/ARSASWebDesign/NetArcaWs/issues/12).
La suite de padrón incorpora tres identidades públicas del listado oficial A4:
CUIT física, CUIL física y CUIT jurídica. Las reutiliza como candidatos en A10/A13
y solo CUIT en Constancia, comprobando identificador y tipos devueltos. No requiere
`ARCA_QUERY_CUIT`. La [ejecución protegida del 6 de octubre de 2026](https://github.com/ARSASWebDesign/NetArcaWs/actions/runs/37512157290)
aprobó seis casos: CUIT jurídica en los cuatro padrones y CUIT física en A10/A13.
Fallaron CUIT física en A4/Constancia y CUIL física en A4/A10/A13; el diagnóstico
original no distinguía sus causas. Se agregaron categorías seguras de error para
la siguiente ejecución, sin relajar validaciones ni publicar datos remotos.
No se afirma que los cuatro servicios compartan datos de QA ni homologación total.
La [corrida siguiente](https://github.com/ARSASWebDesign/NetArcaWs/actions/runs/37513178195)
falló en autenticación WSAA en los once casos, sin validar respuestas de padrón.
El diagnóstico reconoce ahora también el código `alreadyAuthenticated` con un
prefijo XML válido; esto no determina retrospectivamente el código de esa corrida.

El environment GitHub `nuget` contiene
`NUGET_USER=arsas`; el workflow de release intercambió OIDC correctamente y
NuGet aceptó e indexó ambos paquetes 0.5.0 en el índice del feed v3; además, el
Tool público se instaló desde una caché aislada y `--help` terminó con código 0.
La [release v0.5.0](https://github.com/ARSASWebDesign/NetArcaWs/releases/tag/v0.5.0)
está publicada. El mirror wiki incluye el inventario de fuentes originales,
referencias por operación, guías y manifiesto, publicados en la
[wiki](https://github.com/ARSASWebDesign/NetArcaWs/wiki). La portada organiza
servicios y extras; se mantiene solo el menú nativo de páginas de GitHub.
La caché de tickets es local al proceso; compartir certificados entre réplicas no agrega caché distribuida.
Este estado no equivale al 100% del port del repositorio original.

Decisiones: [contexto multitenant](docs/adr/0002-arca-tenant-context.md) y
[certificados en memoria](docs/adr/0003-in-memory-certificates.md),
[reintentos seguros](docs/adr/0001-safe-invoice-retries.md) y
[contratos SOAP fieles a WSDL](docs/adr/0004-public-soap-contracts.md).
Las fuentes de la wiki se mantienen en `docs/wiki/` y `docs/wiki-export/`; la
revisión del repositorio publicada se registra en `SOURCE_COMMIT` del repositorio wiki.
