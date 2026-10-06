<!-- Source: PROGRESS.md. Generated wiki mirror; edit the repository source. -->

# Progreso de NetArcaWs

- [x] Automatizaciones de wiki, contratos y releases migradas a `NetArcaWs.Build` (.NET 10), sin scripts Python.

- [x] Plantillas de issues/PR y guía CONTRIBUTING para forks.
- [x] Workflow de releases de biblioteca y tool con ensayo manual sin publicación.
- [x] Configurar Trusted Publishing y publicar `NetArcaWs` y `NetArcaWs.Tool` 0.5.0 en NuGet.
- [x] Confirmar la indexación de ambos paquetes 0.5.0 en el feed NuGet.
- [x] Hito 1: WSAA nativo, NuGet LGPL-3.0-or-later, xUnit y FluentAssertions.
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
- [x] Hito 6: README, arquitectura, ADR y guías integrales actualizados; suite local Release verificada.
- [x] Publicar en la wiki toda la documentación Markdown local, con navegación y revisión de origen identificada.
- [ ] Hito 7 (final): completar la revisión exhaustiva upstream y las guías/ejemplos por operación; [criterios de cierre](Plan-hito-7-wiki).

La build Release terminó con 0 warnings y 0 errors. La suite completa tuvo
222 casos: 219 aprobados y 3 omitidos. Se verificaron los QName y SOAPAction de
81 operaciones contra WSDL y 166 tipos raíz XML en round-trip. Los 22 tipos de
contrato con campos `DateTime` (`xs:date`/`xs:dateTime`) conservaron sus valores.

En homologación real de QA con `ARCA_RUN_HOMOLOGY=1` el 2026-10-06, la corrida
de integración tuvo 9 casos: 7 probes `Dummy` aprobados y 2 pruebas autenticadas
omitidas. En la suite normal, los 3 omitidos corresponden a WSAA sin credenciales,
servicios autenticados no seleccionados y `Dummy` sin habilitación opt-in. Ningún
resultado acredita una autorización de negocio con certificado real.

La autenticación WSAA de homologación y las llamadas autenticadas fiscales siguen
pendientes de certificados autorizados. El environment GitHub `nuget` contiene
`NUGET_USER=arsas`; el workflow de release intercambió OIDC correctamente y
NuGet aceptó e indexó ambos paquetes 0.5.0 en el índice del feed v3; además, el
Tool público se instaló desde una caché aislada y `--help` terminó con código 0.
La [release v0.5.0](https://github.com/ARSASWebDesign/NetArcaWs/releases/tag/v0.5.0)
está publicada. El mirror wiki local contiene 29 páginas de contenido, barra
lateral y manifiesto, publicados en la [wiki](https://github.com/ARSASWebDesign/NetArcaWs/wiki).
El Hito 7 queda abierto por la revisión documental exhaustiva restante.
La caché de tickets es local al proceso; compartir certificados entre réplicas no agrega caché distribuida.
Este estado no equivale al 100% del port del repositorio original.

Decisiones: [contexto multitenant](ADR-0002-arca-tenant-context) y
[certificados en memoria](ADR-0003-in-memory-certificates),
[reintentos seguros](ADR-0001-safe-invoice-retries) y
[contratos SOAP fieles a WSDL](ADR-0004-public-soap-contracts).
Las fuentes de la wiki se mantienen en `docs/wiki/` y `docs/wiki-export/`; la
revisión del repositorio publicada se registra en `SOURCE_COMMIT` del repositorio wiki.
