<!-- Source: docs/wiki/Home.md. Generated wiki mirror; edit the repository source. -->

# NetArcaWs

NetArcaWs es una biblioteca .NET para integrar servicios web de ARCA desde
aplicaciones C#. Consulta [los servicios implementados](Servicios-implementados)
para conocer el alcance de esta biblioteca y el
[catálogo oficial de servicios web ARCA](https://ftp.afip.gob.ar/ws/documentacion/catalogo.asp)
para verificar los servicios publicados por el organismo. Esta wiki describe
el uso y los límites del código disponible; no implica paridad completa con
PyAfipWs ni homologación fiscal de una instalación.

## Empezar

- [Inicio rápido](Inicio-rapido) — requisitos, paquetes y primera autenticación.
- [Servicios implementados](Servicios-implementados) — qué hace cada cliente, cómo integrarlo y dónde consultar operaciones y manuales ARCA.
- [WSCDC](WSCDC) — constatación y consulta de comprobantes.
- [WSFECred](WSFECred) — gestión del ciclo de Factura de Crédito Electrónica MiPyME.
- [Seguimiento de servicios PyAfipWs pendientes](https://github.com/ARSASWebDesign/NetArcaWs/issues/10) — prioridades y comprobaciones de vigencia por organismo.
- [Funcionalidades adicionales](Funcionalidades-adicionales) — multitenancy, certificados, health checks, CLI, emisión durable, cálculos, herramientas y publicación.
- [Diagnóstico y glosario](Diagnostico-y-glosario) — errores, límites y términos habituales.
- [Migración desde PyAfipWs](Migracion-desde-PyAfipWs) — equivalencias parciales y funciones que requieren una solución propia.

## Guías por tema

- [WSAA y certificados](WSAA-y-certificados) — autenticación, tickets y material criptográfico en memoria.
- [Contexto multitenant](Contexto-multitenant) — cómo seleccionar CUIT, certificado y entorno por operación.
- [Emisión durable y reconciliación](Diario-fiscal) — diario, claves de idempotencia y tratamiento de resultados inciertos.
- [Arquitectura y reintentos](Arquitectura-y-reintentos) — límites del transporte y política para fallas fiscales.
- [Transporte SOAP](Transporte-SOAP) — serialización, límites, faults y cancelación.
- [Health checks](Health-checks) — probes opt-in de disponibilidad sin credenciales fiscales.
- [Cálculos decimales](Calculos-decimales) — utilitarios aritméticos y límites fiscales.
- [Herramienta de certificados](Herramienta-de-certificados) — generación local de CSR e inspección de certificados.
- [Desarrollo y contribución](Desarrollo-y-contribucion) — build, validaciones y pull requests.
- [Releases y publicación NuGet](Publicar-versiones) — empaquetado y publicación.

- [Seguridad y publicación](Seguridad-y-publicacion) — protecciones, revisión y respuesta a incidentes.

## Referencias

- [Inventario de PyAfipWs](Inventario-del-proyecto-original) — fuentes investigadas y alcance que sigue fuera del port.
- [Contratos SOAP de health checks](Contratos-de-health-checks) — endpoints y bindings de probes.
- [Contratos de producción](Procedencia-de-contratos-SOAP) — procedencia y hashes de WSDL.
- [Catálogo de operaciones por servicio](WSFEv1-Referencia-de-operaciones) — detalle de WSFEv1; ver también los vínculos de cada servicio en [Servicios implementados](Servicios-implementados).
- [Ejemplos compilables de integración](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/examples/NetArcaWs.Examples/GuideExamples.cs) — snippets de las guías, sin llamadas de red al construirlos.
- [Arquitectura](Arquitectura-general) y [progreso del proyecto](Estado-del-proyecto) — decisiones técnicas y estado del trabajo.

La homologación autenticada requiere certificados autorizados por ARCA. Un
build, un test local, un health check o la publicación de un paquete no prueban
por sí solos que una factura real será aceptada.

## Homologación

- [Homologación protegida de WSFE](Homologacion) — ejecución manual, credenciales y aprobación en GitHub.
