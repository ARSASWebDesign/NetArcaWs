<!-- Source: AGENTS.md. Generated wiki mirror; edit the repository source. -->

# Guía para agentes de NetArcaWs

Estas instrucciones aplican a todo el repositorio. Recogen las decisiones de la
conversación «Migrar PyArcaWs a NET» y su evolución. Las instrucciones explícitas
del usuario para la tarea actual tienen prioridad. Contrasta el código, pruebas
y documentos vigentes antes de afirmar el estado de una función: los resultados
de conversaciones anteriores no constituyen evidencia actual.

## Propósito y fuentes de verdad

NetArcaWs es una biblioteca .NET 10 y una herramienta .NET para integrar servicios
ARCA, basada en **PyAfipWs**. Se distribuye como `NetArcaWs` y `NetArcaWs.Tool`,
bajo **LGPL-3.0-or-later**, preservando atribuciones, copyright y avisos de
terceros. La licencia permite uso comercial sujeto a sus obligaciones.

El objetivo es ampliar la cobertura del proyecto original con implementaciones
completas de las operaciones de los servicios elegidos. No declares paridad total
ni homologación de toda la biblioteca sin evidencia. Distingue alcance
implementado, verificado localmente y probado contra ARCA.

Lee según el área afectada:

- `README.md`, `ARCHITECTURE.md` y `PROGRESS.md`: uso, componentes, extras,
  limitaciones y hitos; contrasta cifras y estados antes de repetirlos.
- `CONTRIBUTING.md`, `SECURITY.md` y `THIRD-PARTY-NOTICES.md`: contribuciones,
  seguridad y licencias.
- `docs/adr/0001-safe-invoice-retries.md`: emisión durable y reconciliación.
- `docs/adr/0002-arca-tenant-context.md`: identidad y aislamiento multitenant.
- `docs/adr/0003-in-memory-certificates.md`: carga, rotación y vida del material
  criptográfico.
- `docs/adr/0004-public-soap-contracts.md` y `docs/reference/contracts/`:
  contratos públicos, snapshots y límites conocidos.
- `docs/releases.md`, `docs/wiki/` y `docs/plans/hito-7-wiki.md`: publicación y
  documentación integral.

El usuario pidió mantener inventarios de hallazgos upstream y propuestas
pendientes en issues/discussions, evitando agregar catálogos de trabajo futuro
al código. Conserva los snapshots y referencias técnicas necesarios para
reproducir contratos. Consulta los issues para el backlog vigente.

## Estructura y herramientas

- `src/NetArcaWs`: WSAA, criptografía, SOAP, servicios, multitenancy, health checks
  y emisión durable.
- `src/NetArcaWs.Tool`: `netarcaws` y operaciones de certificados.
- `tools/NetArcaWs.Build`: mantenimiento de contratos, wiki y releases en .NET.
- `tests/NetArcaWs.UnitTests`, `NetArcaWs.Tool.Tests`, `NetArcaWs.Build.Tests`:
  verificaciones locales con datos sintéticos.
- `tests/NetArcaWs.IntegrationTests`: pruebas opt-in contra homologación y
  verificaciones locales de la suite.
- `examples/NetArcaWs.Examples`: ejemplos compilables.
- `.codex/environments/environment.toml`: setup y acciones locales de ChatGPT.

El SDK lo determina `global.json`, con roll-forward de patch y sin prereleases.
No mantengas otra versión fija en scripts. Runtime, build, generación de
contratos, wiki y releases no deben depender de **Python**. Usa la herramienta
.NET existente, sin introducir un segundo sistema de mantenimiento. El wrapper
shell del generador es opcional.

## Implementación y contratos ARCA

- Usa C# moderno: namespaces de archivo, nullable, async y cancelación,
  constructores primarios y records cuando correspondan, y DI nativa.
- Entrega código completo para el alcance acordado y sus pruebas. No cierres
  hitos con placeholders, métodos vacíos ni `TODO: implementar`.
- Verifica los WSDL y manuales **oficiales vigentes** de ARCA antes de ampliar un
  servicio. PyAfipWs aporta contexto y flujos, pero no limita las operaciones ni
  sustituye los contratos actuales.
- Expón todas las operaciones del contrato seleccionado con requests/responses
  tipados, incluyendo las que no destacan los clientes Python.
- Mantén SOAP 1.1 con `HttpClient`, `ISoapTransport` y
  `System.Xml.Serialization`; no introduzcas WCF.
- Respeta QName, SOAPAction, namespaces, orden XML, opcionalidad, cardinalidad,
  enumeraciones y primitivas del WSDL. Separa cálculos fiscales de los modelos
  de cable: si el XSD usa `double`, conserva `double`; helpers `decimal` no
  deben alterar el contrato serializado.
- Regenera modelos con `NetArcaWs.Build` y revisa diff, hashes, fixtures, XML y
  pruebas de round-trip. No edites archivos generados como solución permanente.
  Verifica fechas y números con distintas culturas.
- El snapshot WSMTXCA de homologación está documentado como truncado/malformado;
  no lo uses para generar tipos. Contrasta el contrato de producción archivado
  con el binding real antes de declarar compatibilidad validada.
- **WSMTXCA** es facturación con detalle, CAE/CAEA y consultas. **WSFECred** es el
  servicio del ciclo de Factura de Crédito Electrónica MiPyME; no los confundas.
- Padrón A5 conserva una ruta histórica; su identidad WSAA actual es
  `ws_sr_constancia_inscripcion`. No deduzcas service IDs del nombre de clase.
- El catálogo incluye WSAA, WSFEv1, WSFEXv1, WSMTXCA, Padrón A4/A5/A10/A13,
  WSCDC, WSFECred y WSCPE. Verifica cada operación en el código: una fachada no
  prueba habilitación fiscal ni validación en ARCA.

## Criptografía, certificados y multitenancy

- Firma TRA/CMS y procesa claves/CSR con APIs .NET nativas:
  `System.Security.Cryptography.Pkcs`, `System.Formats.Asn1` y X.509/RSA.
  No invoques **OpenSSL** ni agregues **BouncyCastle** para estas operaciones.
- Preserva los algoritmos y formatos requeridos por ARCA; cambiar la firma
  requiere evidencia de interoperabilidad. Distingue compatibilidad legacy de
  la política de generación de claves nuevas.
- Acepta `WsaaCertificateContent` como PEM, bytes PFX/P12 o Base64 desde
  configuración, variables, vault o BD. La aplicación obtiene el secreto; la
  biblioteca no incorpora SDKs de vault ni exige persistirlo en archivos.
- PFX en memoria usa `EphemeralKeySet` en Windows/Linux. En macOS proporciona
  PEM; no agregues un fallback que escriba la clave privada al disco.
- Respeta ownership y `Dispose` de `X509Certificate2`, copia defensiva de bytes
  y ocultamiento de secretos en representaciones públicas. No prometas borrado
  inmediato de strings administrados.
- Toda operación autenticada recibe un `ArcaTenantContext` inmutable por
  operación: tenant, CUIT representada, ambiente y certificado. Nunca alternes
  credenciales ni endpoints mediante opciones globales mutables.
- El contexto no autoriza usuarios. La aplicación resuelve y autoriza tenant,
  CUIT y ambiente antes de construirlo; no confíes en valores libres del request.
  Una delegación válida puede representar una CUIT distinta de la del certificado.
- Inserta Token, Sign y CUIT en SOAP desde el contexto autorizado y el ticket
  WSAA, sin tomar credenciales arbitrarias del payload del usuario.
- Reutiliza tickets con `IMemoryCache` hasta su vencimiento efectivo, sin imponer
  una duración fija distinta de la respuesta WSAA.
  Conserva aislamiento por tenant/CUIT/endpoint/servicio/huella de certificado y
  coordinación local acotada de solicitudes concurrentes.
- Caché y semáforos son locales al proceso. Compartir certificados por vault/BD
  no comparte tickets ni coordina logins entre réplicas. Usar el mismo
  certificado en varios tenants puede provocar `alreadyAuthenticated` en WSAA.
  No declares resuelta la coordinación distribuida sin implementarla.

## Emisión fiscal y recuperación

Mantén las invariantes del ADR 0001 y sus pruebas:

- Identidad fiscal única: `(ambiente, CUIT, punto de venta, tipo, número)`.
  Tenant y protocolo no permiten duplicar esa identidad; el tenant restringe
  acceso y visibilidad.
- Persiste request congelado, versión canónica, hash, identidad, estado,
  respuesta y lease/fencing antes de enviar. Token/Sign no integran el hash del
  payload fiscal. Cambiar la serialización exige migración y pruebas.
- La aplicación suministra número, tipo y punto de venta. No introduzcas
  asignación automática de numeración incidentalmente.
- Reutilizar una clave de idempotencia devuelve el mismo intento solo si payload
  e identidad coinciden; en caso contrario genera conflicto.
- No prepares otro comprobante de una serie mientras una operación anterior
  conserve resultado no terminal.
- Un timeout, cancelación, respuesta parcial o fallo de persistencia puede dejar
  `Unknown`. Consulta el mismo comprobante y compara su contenido; no repitas
  automáticamente la autorización ni cambies el número.
- Una consulta vacía/negativa no prueba ausencia ni permite reenviar. Evidencia
  positiva y correlacionada resuelve `Authorized`; discrepancias fiscales exigen
  `Conflict`/revisión. Correlaciona rechazos con el detalle de la respuesta al envío.
- `ResumeAsync` envía solo snapshots que sigan en `Prepared`; los demás estados
  se reconcilian. `ReviseRejectedAsync` prepara una corrección explícita,
  auditable y versionada de un rechazo; no envía por sí misma.
- No agregues retries HTTP genéricos de escrituras SOAP ni failover entre
  ambientes. No prometas exactamente una vez.
- `SafeInvoiceService` cubre CAE unitario WSFE/WSFEX/WSMTXCA. Las llamadas batch
  de bajo nivel no equivalen a diario durable por ítem; WSFECred no queda
  cubierto automáticamente por esa abstracción.
- SQLite sirve para procesos del mismo host con un archivo local, no NFS ni
  hosts distintos. Un diario multi-host necesita storage transaccional compartido
  con las mismas claves únicas y fencing.
- Recuperación pendiente no implica worker, scheduler o backoff incorporados.
  La aplicación programa la recuperación y protege los datos fiscales del diario.

## Health checks y CLI

Los health checks son independientes y **opt-in** por servicio/ambiente, con
timeout configurable. Registra solo los solicitados. Dummy o conectividad/WSDL
WSAA no prueban autenticación, autorización de una CUIT ni una operación fiscal.
Los checks no requieren certificados ni tenant.

`netarcaws cert-dev` y `cert-prod` generan claves y CSR; **ARCA emite el
certificado**. `cert-info` verifica información local y correspondencia con la
clave. Conserva claves cifradas, contraseñas vía variable de entorno, permisos
restrictivos y protección contra sobrescrituras. No expongas claves/contraseñas
en argumentos, logs o ejemplos. Mantén instalación local mediante manifiesto y
el paquete `NetArcaWs.Tool`.

## Pruebas y evidencia

Usa xUnit v3, **AwesomeAssertions** y Microsoft.Testing.Platform. La instrucción
inicial de FluentAssertions fue reemplazada por la decisión de licencias: no lo
reincorpores. AwesomeAssertions debe seguir con `PrivateAssets="all"`.

Cada cambio de comportamiento requiere pruebas pertinentes: SOAP simulado y
parseo, firma/certificados, caché/vencimiento, concurrencia entre tenants,
alícuotas/redondeos, idempotencia, rechazos y recuperación ante fallos. Verifica
culturas, fechas/decimales y certificados entre macOS, Linux y Windows cuando
el cambio los afecte. Usa fixtures sintéticos.

Desde la raíz, reproduce los chequeos del repositorio:

```sh
dotnet restore NetArcaWs.slnx --locked-mode
dotnet build NetArcaWs.slnx --configuration Release --no-restore
dotnet test --solution NetArcaWs.slnx --configuration Release --no-build --no-restore
dotnet pack src/NetArcaWs/NetArcaWs.csproj --configuration Release --no-build --no-restore --output artifacts
dotnet pack src/NetArcaWs.Tool/NetArcaWs.Tool.csproj --configuration Release --no-build --no-restore --output artifacts
```

Los lockfiles están versionados. Una actualización deliberada usa
`dotnet restore NetArcaWs.slnx --force-evaluate`, revisa el diff y lo incluye en
la PR. No relajes `--locked-mode` para encubrir discrepancias.

Las pruebas reales son opt-in. No configures certificados ni habilites
homologación automáticamente en el entorno local de ChatGPT. No uses credenciales
de producción para pruebas. Reporta por separado pruebas locales, omitidas,
probes públicos, autenticación y escenarios fiscales ejecutados, con ambiente,
servicio y evidencia. Un Dummy exitoso no acredita homologación completa.
Valida el paquete instalado en un consumidor/ejemplo y el CLI cuando corresponda.

La homologación autenticada en GitHub se ejecuta manualmente desde `main` del
repositorio oficial, mediante el environment protegido `homologacion`, sin bypass.
El mantenedor principal puede aprobar su ejecución: no exige siempre un segundo
administrador. Usa código revisado; nunca código/artefactos de forks con esos
secretos. La suite estricta falla ante configuración incompleta, sin omisiones
silenciosas. Reutiliza tickets para evitar logins redundantes. Los comprobantes
en homologación son de prueba y no tienen validez fiscal; escribir uno requiere
selección explícita del escenario. Validar la biblioteca no habilita consumidores:
cada certificado/CUIT requiere autorizaciones ARCA para cada servicio.

## Colaboración, documentación y entrega

- Trabaja en rama propia y entrega por PR hacia `main`; para externos sigue el
  flujo fork/upstream de `CONTRIBUTING.md`. No sobrescribas trabajo ajeno ni eludas
  protecciones de rama.
- En trabajos por hitos coordina especialistas de criptografía, integración,
  DevOps/documentación y pruebas cuando existan herramientas de delegación.
  La conversación los denominó Luna; las pruebas tienen responsable independiente
  de la implementación. Adapta modelos a capacidades disponibles y tarea vigente.
- El usuario autorizó continuar los hitos acordados sin pedir confirmación en
  cada iteración. Completa el alcance solicitado y reporta código, pruebas,
  progreso y límites. Una solicitud de PR no autoriza merge ni release.
- Actualiza README, arquitectura, ADR y progreso cuando cambien API, decisiones
  o alcance; los estados de implementado y validado deben tener evidencia.
- La wiki explica rol, funcionalidades, uso, operaciones y extras por servicio.
  Usa títulos legibles y listas/tablas, evitando catálogos en párrafos de comas
  y menús duplicados. Conserva Home y el menú nativo de GitHub; no generes otro
  `_Sidebar.md`.
- Genera documentación con `dotnet run --project tools/NetArcaWs.Build -- wiki`
  y contratos/operaciones con las opciones documentadas. Mantén fuentes Markdown
  y manifiesto de origen, preserva atribuciones PyAfipWs y agrega extras .NET.
  Publicar la wiki requiere la instrucción de entrega correspondiente.
- Revisa issues/PR upstream por aplicabilidad; no traslades binarios,
  credenciales, eliminación de pruebas ni cambios de Python/COM sin análisis.
  Mantén backlog y limitaciones en los issues correspondientes.

## Seguridad, dependencias y publicación

- Nunca confirmes claves, PEM/PFX, contraseñas, tokens/Sign, CUIT ni datos reales
  de tenants. Evita shell tracing, SOAP completo y artefactos con credenciales;
  redacta diagnósticos. Usa el canal privado de `SECURITY.md` para vulnerabilidades.
- Revisa licencias de dependencias nuevas/actualizadas, incluidas herramientas
  de desarrollo. Preserva LGPL y avisos de terceros; no introduzcas condiciones
  comerciales incompatibles sin una decisión expresa.
- El proyecto debe mantenerse gratuito: no habilites servicios pagos, runners
  con coste ni suscripciones. Revisa condiciones actuales antes de habilitar
  herramientas; conserva CI, CodeQL y controles gratuitos del repo público.
- Respeta CODEOWNERS, aprobación distinta del último autor de push, checks de la
  rama actualizada y conversaciones resueltas. Las protecciones de PR y aprobación
  de homologación son distintas; no agregues bypasses.
- Mantén acciones fijadas por SHA, permisos mínimos y secretos/OIDC fuera de PR
  y forks. No uses `pull_request_target` para ejecutar su código.
- NuGet publica biblioteca y tool con igual versión mediante Trusted
  Publishing/OIDC, usuario `arsas` y environment `nuget`. La publicación es
  automática tras una release publicada con tag protegido sobre `main`; el
  ensayo manual solo verifica y genera artefactos.
- Publicación consume paquetes verificados del build, comprueba hashes/versión/
  commit y no ejecuta código del repo con credenciales de publicación. No
  reemplaces assets existentes, muevas tags publicados ni reutilices versiones
  para código diferente.
- OIDC no es firma de autor. NuGet agrega firma de repositorio; una firma de
  autor exige otra decisión y custodia de un certificado de firma de código.
  No reutilices certificados ARCA ni compres certificados para el entorno local.
- Una release no prueba publicación de ambos paquetes: verifica aceptación e
  indexación por separado. Lee `docs/releases.md` antes de cambiar versiones,
  tags o flujos de publicación.
