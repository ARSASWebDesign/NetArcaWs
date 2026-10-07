<!-- Source: PROGRESS.md. Generated wiki mirror; edit the repository source. -->

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
- [x] Incremento EF del issue #19: persistencia opt-in y migraciones oficiales por módulo/proveedor; migración inicial independiente para SQLite, MySQL, MariaDB, PostgreSQL y SQL Server. Una tarea de despliegue debe consultar, revisar y aplicar; API startup no migra. Paquetes extras 0.5.0 sin publicar. CI 37547645296: 510 tests (491 aprobados, 19 skips opt-in, 0 fallos), build Release 0 warnings; MySQL 8.4.11 20/20, MariaDB 11.4.13 20/20, PostgreSQL 17.6 19/19 y SQL Server Developer 16.0.4295.3 19/19, sin skips; 10 migration SQL, 15 DDL, 11 packs y consumer/CLI nuevo. Evidencia por versión/escenario, sin homologación ARCA ni compatibilidad universal.
- [x] Issue #23: store EF opt-in de certificados versionados con keyring externo, compare-and-swap al rotar e historiales de migración independientes para los cinco motores. Verificados SQLite 78/78; MySQL, MariaDB, PostgreSQL y SQL Server 26/26 cada uno; además, 20 DDL y 15 SQL de migración generados. La API se incorporó después de los paquetes publicados 0.6.0 y queda pendiente de la siguiente publicación; no hubo llamadas ARCA. La aplicación autoriza el alcance, conserva su propio `VersionId` por operación y programa avisos; este store no renueva certificados ni publica cambios automáticamente. Consultar la [guía](Certificados-multitenant), [ADR 0007](ADR-0007-tenant-certificate-store) e [issue #23](https://github.com/ARSASWebDesign/NetArcaWs/issues/23).
- [x] Issue #24: cola durable y worker opt-in para recuperación CAE unitaria en WSFEv1, WSFEXv1 y WSMTXCA; módulo y migraciones separados por SQLite, MySQL, MariaDB, PostgreSQL y SQL Server. La extensión está en el código posterior a los paquetes públicos 0.6.0 y requiere una publicación futura. La evidencia local reportada cubre SQLite 131/131, MySQL 29/29, MariaDB 29/29, PostgreSQL 28/28 y SQL Server 28/28; metadata offline: PostgreSQL 8/8 y SQL Server 19/19. Las pruebas focalizadas del módulo recovery suman SQLite 5, MySQL 8, MariaDB 8, PostgreSQL 9 y SQL Server 9 aprobadas; las corridas dedicadas de cancelación DDL MySQL y MariaDB agregan 1/1 cada una (son subconjuntos, no se suman a la matriz completa). También se generaron 25 DDL y 20 cadenas de migración y se preservaron los artefactos históricos según el informe de Task 3. No hubo llamadas ARCA. El consumer smoke restaurado desde un feed local/cache nueva, CLI y pack secuencial de 11 proyectos 0.6.0 pasaron. La revisión de solución completa aprobó restore locked, build Release (0 warnings/0 errors), 630 pruebas (588 aprobadas, 42 skips opt-in, 0 fallos), y los chequeos de 25 DDL y 20 scripts de migración; los paquetes se volverán a empaquetar después de actualizar sus README. Los resultados por motor no prueban homologación ni compatibilidad universal.
- [ ] WSAA: ejecutar homologación real con certificados autorizados.
- [x] Hito 2: WSFEv1 — 22 operaciones, contrato SOAP y suite verificados.
- [x] Hito 3: WSFEXv1 — 19 operaciones, contrato SOAP y suite verificados.
- [x] Hito 4: WSMTXCA — 27 operaciones, contrato SOAP y suite verificados; no equivale al ciclo integral de Factura de Crédito Electrónica MiPyME.
- [x] Hito 5: Padrón A4 (2), Constancia/ruta A5 (5), A10 (2), A13 (4) — contratos, fachada y suite verificados.
- [x] WSCDC (6) y WSFECred (21): contratos QA/producción, fachadas multitenant, DI, health checks y referencias de las 27 operaciones. WSFECred permanece fuera de `SafeInvoiceService`/diario.
- [x] Hito 6: README, arquitectura, ADR y guías integrales actualizados; suite local Release verificada.
- [x] Publicar en la wiki toda la documentación Markdown local, con navegación y revisión de origen identificada.
- [x] Hito 7: catálogo por servicios, extras y ejemplos compilables; el inventario vigente es de 183 operaciones en diez contratos y el seguimiento del alcance original sigue en [issue #10](https://github.com/ARSASWebDesign/NetArcaWs/issues/10).

La suite CI del incremento EF (37547645296) corrió sobre el base exacto con 510
casos: 491 aprobados, 19 omitidos opt-in y 0 fallos; build Release con 0
warnings. Los engines reales ejecutaron sin omisiones: MySQL 8.4.11 20/20,
MariaDB 11.4.13 20/20, PostgreSQL 17.6 19/19 y SQL Server Developer
16.0.4295.3 19/19. También se verificaron 10 scripts de migración, 15 DDL,
11 packs y un consumer/CLI nuevo. La evidencia no incluye llamadas ARCA ni
publicación de extras.

El inventario SOAP verificado por la suite previa es 183 operaciones contra sus
WSDL y 369 tipos raíz XML en round-trip. Los 56 tipos con campos `DateTime`
(`xs:date`/`xs:dateTime`) conservaron sus valores.

En homologación real de QA con `ARCA_RUN_HOMOLOGY=1` el 2026-10-06, la corrida
de integración tuvo 11 casos: 9 probes `Dummy` aprobados y 2 pruebas autenticadas
omitidas. En la suite normal, cuatro casos dependen de credenciales/opt-in de
ARCA y tres de la configuración opt-in de engines de base. Ningún resultado
acredita una autorización de negocio con certificado real.

La autenticación WSAA de homologación y las llamadas autenticadas fiscales siguen
pendientes de certificados autorizados. Tests opt-in de persistencia ejecutados
con MySQL 8.4.11 (20/20), MariaDB 11.4.13 (20/20), PostgreSQL 17.6 (19/19) y SQL
Server Developer 16.0.4295.3 (19/19) cubren
esas versiones y casos entre procesos; no prueban otras versiones ni
homologación fiscal. Los casos PostgreSQL y SQL Server se ejecutaron en el
[job público de CI](https://github.com/ARSASWebDesign/NetArcaWs/actions/runs/37547645296)
en runner x64. El consumer smoke de los paquetes EF locales corrió con SQLite.

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

Validación posterior local, autorizada, del 6 de octubre de 2026: **12/12 consultas
autenticadas de padrón aprobadas** con los fixtures actualizados y el mismo
`PadronTestCases.RunAsync`/validador de la suite, reutilizando los cuatro TA guardados
en almacenamiento privado fuera del repositorio. Se reemplazó la CUIT física con
error AT por `20188192514` y el caso del listado CUIL por `20188027963`, que ARCA
devuelve actualmente como CUIT. La matriz mantiene tipo e identidad exactos; no se
modificaron los clientes SOAP. La cobertura autenticada de CUIL real sigue pendiente
de una identidad pública que efectivamente devuelva CUIL; la cobertura sintética
se conserva. La suite local sin red aprobó 376 pruebas, con cuatro opt-in omitidas.

El environment GitHub `nuget` contiene
`NUGET_USER=arsas`; el workflow de release intercambió OIDC correctamente y
NuGet aceptó e indexó ambos paquetes 0.5.0 en el índice del feed v3; además, el
Tool público se instaló desde una caché aislada y `--help` terminó con código 0.
La [release v0.5.0](https://github.com/ARSASWebDesign/NetArcaWs/releases/tag/v0.5.0)
está publicada. El mirror wiki incluye el inventario de fuentes originales,
referencias por operación, guías y manifiesto, publicados en la
[wiki](https://github.com/ARSASWebDesign/NetArcaWs/wiki). La portada organiza
servicios y extras; se mantiene solo el menú nativo de páginas de GitHub.
La caché base de tickets sigue siendo local al proceso. La persistencia compartida cifrada es opt-in desde el paquete EF; la aplicación debe distribuir sus claves y controlar el esquema. Las llamadas directas a `WsaaService` usan la caché local. Ver [ADR 0005](ADR-0005-ef-core-invoice-journal) y [ADR 0006](ADR-0006-shared-wsaa-tickets).
Este estado no equivale al 100% del port del repositorio original.

Decisiones: [contexto multitenant](Decisi%C3%B3n-2-Contexto-multitenant) y
[certificados en memoria](Decisi%C3%B3n-3-Certificados-en-memoria),
[reintentos seguros](Decisi%C3%B3n-1-Emisi%C3%B3n-y-reintentos-seguros) y
[contratos SOAP fieles a WSDL](Decisi%C3%B3n-4-Contratos-SOAP-p%C3%BAblicos),
[persistencia EF Core](ADR-0005-ef-core-invoice-journal) y
[tickets WSAA compartidos](ADR-0006-shared-wsaa-tickets).
Las fuentes de la wiki se mantienen en `docs/wiki/` y `docs/wiki-export/`; la
revisión del repositorio publicada se registra en `SOURCE_COMMIT` del repositorio wiki.
