# ADR 0007: Store opt-in de certificados multitenant

- Estado: Aceptado e implementado en el código de desarrollo; pendiente de la siguiente publicación NuGet
- Fecha: 2026-10-06
- Complementa: [contexto multitenant](0002-arca-tenant-context.md), [certificados en memoria](0003-in-memory-certificates.md) y [persistencia EF](0005-ef-core-invoice-journal.md)

## Contexto

Las aplicaciones multitenant necesitan conservar certificados para varias
identidades y rotarlos sin guardar la clave privada en claro en una base de
datos. El paquete base debe seguir libre de EF Core, vaults y dependencias de
gestión de secretos. Persistir una credencial tampoco debe convertirse en una
decisión de autorización ni cambiar la identidad fiscal de una operación ya
preparada.

## Decisión

`NetArcaWs.EntityFrameworkCore` incorpora un store opcional de versiones de
certificado. Solo `NetArcaWsModelOptions.Configure(x => x.AddCertificates())`
incluye sus entidades. El registro requiere un `IArcaCertificateProtector` del
consumidor; el módulo puede seleccionarse sin facturación ni tickets WSAA. No se
agregan entidades ni servicios al registrar `AddNetArcaWs`.

La aplicación resuelve y autoriza tenant, CUIT representada y ambiente antes de
crear `ArcaCertificateScope` y llamar al store. El scope identifica datos; no
autoriza su lectura o modificación. Tras obtener contenido, la aplicación
construye el `ArcaTenantContext` autorizado y llama al servicio existente.
`GetVersionAsync` busca exactamente el ID pedido y no sustituye una versión
faltante por la activa.

Las rotaciones agregan una versión inmutable y avanzan la cabecera activa por
compare-and-swap. `expectedActiveVersionId: null` crea el primer registro solo
si aún no existe; reemplazarlo requiere el ID activo observado. El conflicto
debe resolverse volviendo a leer y tomando una decisión explícita. El store
conserva versiones viejas; no ofrece borrado ni reenvoltura automática.

Al importar, el store carga el contenido con `LoadCertificate()`, exige clave
privada RSA exportable y crea un sobre PEM normalizado en memoria. El sobre se
cifra con AES-256-GCM y AAD ligado al propósito, key ID, alcance, versión,
huella, vigencia y creación. Solo ciphertext, nonce, tag y metadatos quedan en
la base. Los buffers temporales se limpian cuando es posible; no se garantiza
borrado inmediato de strings administrados. `WsaaCertificateContent` no es
`IDisposable`; el consumidor administra el `Dispose` del `X509Certificate2`
devuelto por `LoadCertificate()`.

El consumidor administra el keyring fuera de la base. Debe conservar cada key
ID antiguo mientras haya ciphertext que lo use, activar la nueva clave solo
para escrituras y respaldar/restaurar el ciphertext junto con su keyring. Las
claves de protección y los certificados son materiales distintos. No hay
herramientas de reenvoltura en este módulo. El cifrado no sustituye controles de
acceso a la base ni protege claves presentes en memoria del proceso.

La vigencia `NotBefore`/`NotAfter` describe el certificado X.509. No acredita
delegación, autorización de CUIT ni habilitación de un servicio ante ARCA. El
store permite inspeccionar metadatos históricos aunque la versión todavía no
sea válida o haya vencido; al recuperar contenido comprueba la vigencia actual
y falla con `ArcaCertificateValidityException`. WSAA conserva su propia
validación al autenticar. `FindExpiringAsync` se limita a una scope y devuelve
certificados activos; el consumidor programa la consulta y entrega avisos. No
hay scheduler, renovación ni solicitud de certificado a ARCA.

La selección del certificado pertenece a la aplicación. Si una operación
preparada necesita conservar la misma selección después de una rotación, el
consumidor persiste su `VersionId` junto a su registro de operación. Esto no
modifica el hash canónico, el esquema ni las migraciones del diario de facturas.

## Esquema y despliegue

El módulo agrega `NetArcaCertificateSlots` y `NetArcaCertificateVersions` sin
claves foráneas físicas. La cabecera única se identifica por tenant hash, CUIT y
ambiente; las versiones tienen clave compuesta por ese alcance y `VersionId`.
Cada proveedor admitido tiene una historia de migración propia del módulo
`TenantCertificates`. Las migraciones se registran y aplican expresamente por
el actor de despliegue. Registrar DI o iniciar la API no abre la conexión ni
ejecuta cambios de esquema. Deshabilitar el módulo no borra filas ni historias.

Para PFX/P12, la importación usa `EphemeralKeySet` en Windows y Linux; macOS
requiere PEM. No existe fallback a archivos. Una aplicación puede cargar el
keyring por su mecanismo de secretos preferido sin incorporar ese SDK a la
biblioteca.

## Consecuencias

- La base guarda un tenant hash y metadata de alcance necesaria para selección;
  el identificador tenant original se incluye en AAD y no se persiste como
  texto claro en estas tablas.
- Una versión fijada por una operación no cambia cuando avanza la cabecera
  activa. El consumidor es responsable de guardar ese vínculo.
- La rotación conserva filas anteriores para auditoría y recuperación. Retención,
  borrado y reenvoltura requieren decisiones y herramientas futuras.
- `AddCertificates()` es independiente de servicios ARCA, permisos, tickets y
  emisión durable. El paquete no modifica autenticación ni canonicalización.

## Evidencia

Las suites locales verifican SQLite (78/78) y motores reales MySQL, MariaDB,
PostgreSQL y SQL Server (26/26 cada uno), incluidos flujos de rotación, lectura,
concurrencia y migraciones correspondientes a esos entornos. Se generaron 20
DDL y 15 scripts SQL para los cinco motores. Esta evidencia no prueba
homologación ARCA ni compatibilidad con versiones no ejecutadas. La API se
incorporó después de los paquetes publicados 0.6.0; queda pendiente de la
siguiente publicación.
