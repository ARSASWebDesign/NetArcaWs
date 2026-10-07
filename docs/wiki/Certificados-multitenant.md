# Store de certificados multitenant

`NetArcaWs.EntityFrameworkCore` ofrece un store opcional para conservar
versiones inmutables de certificados WSAA cifradas en la base de la aplicación.
El paquete base y los clientes ARCA no acceden al store automáticamente. Esta
API se incorporó después de la publicación NuGet 0.6.0; estará disponible en la
siguiente publicación de los paquetes EF.

El store administra material y versiones. No autoriza usuarios, tenants o CUIT,
no emite ni renueva certificados ante ARCA y no confirma que una CUIT tenga
delegación o un servicio habilitado. La aplicación debe autorizar cada operación
antes de usar el store y antes de construir `ArcaTenantContext`.

## Configurar el módulo

La selección `AddCertificates()` es independiente de facturación, tickets WSAA
y servicios fiscales. El mismo `NetArcaWsModelOptions` se aplica al modelo de
EF y al registro de stores:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Cryptography;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;

NetArcaWsModelOptions model = NetArcaWsModelOptions.Configure(options =>
    options.AddCertificates());

services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));
services.AddSingleton<IArcaCertificateProtector>(_ =>
    new AesGcmArcaCertificateProtector(activeKeyId, externallyLoadedKeyRing));
services.AddNetArcaWsEntityFrameworkStores<AppDbContext>(model);

public sealed class AppDbContext(DbContextOptions<AppDbContext> options,
    NetArcaWsModelOptions model) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.AddNetArcaWs(model);
}
```

`AddNetArcaWsEntityFrameworkStores` registra la misma instancia de
`NetArcaWsModelOptions` como singleton para que el contexto y el store usen la
selección idéntica.

`externallyLoadedKeyRing` representa claves cargadas por la aplicación desde su
vault, servicio de secretos o mecanismo equivalente; no las guardes en la misma
base que el ciphertext. Cada key ID identifica una clave AES de 256 bits. El
keyring contiene la clave activa para cifrar nuevas versiones y las claves
anteriores necesarias para descifrar las ya guardadas. No uses una clave nueva
aleatoria en cada reinicio.

Cuando se registra un protector creado por una factoría de DI, el contenedor lo
crea y dispone al cerrar el proveedor. Si se registra una instancia ya creada,
la aplicación conserva su propiedad y debe llamar `Dispose()` cuando termina su
vida útil. El protector copia sus claves al construirse y limpia esas copias al
disponerse; el consumidor sigue siendo responsable de administrar los buffers
originales del keyring.

El registro configura el modelo y los stores sin conectar ni migrar la base. El
despliegue elige el paquete de migraciones del motor, incluye
`NetArcaWsPersistenceModule.TenantCertificates` y ejecuta explícitamente el
migrador después de revisar su estado o SQL. No uses `EnsureCreated` para una
base administrada por estas migraciones. `AddNetArcaWs` no agrega entidades ni
servicios del store porque su ciclo de vida es independiente de la integración
ARCA.

## Autorizar y seleccionar contenido

La aplicación primero resuelve la identidad desde su sesión y política de
autorización. No tomes tenant, CUIT o ambiente directamente de datos editables
del usuario sin validarlos:

```csharp
AuthorizedArcaIdentity identity = await authorizer.AuthorizeAsync(user, request,
    cancellationToken);
var scope = new ArcaCertificateScope(identity.TenantId, identity.Cuit, identity.Environment);

ArcaStoredCertificate? selected = await certificateStore.GetActiveAsync(scope, cancellationToken);
if (selected is null) throw new InvalidOperationException("No hay certificado configurado.");

var tenantContext = new ArcaTenantContext(
    identity.TenantId, identity.Cuit, identity.Environment, selected.Content);
// Pasar tenantContext a la operación autenticada existente.
```

El tipo `ArcaCertificateScope` valida el formato y los límites de identidad,
pero crear ese objeto no concede permiso. Una CUIT representada puede diferir
de la del certificado cuando existe delegación autorizada; la aplicación es
responsable de comprobarla.

`GetVersionAsync(scope, versionId)` devuelve exactamente la versión solicitada
o `null`; no cambia a la activa si el ID falta. Guarda el `VersionId` elegido
junto al registro de cada operación preparada en el modelo de la aplicación. La
rotación activa posterior no debe cambiar qué certificado había seleccionado
esa operación. El store no añade una columna al diario de facturas ni altera su
hash canónico.

`ArcaStoredCertificate.Content` es `WsaaCertificateContent`, que no implementa
`IDisposable`. Si la aplicación llama a `LoadCertificate()`, administra el
`Dispose()` del `X509Certificate2` devuelto. Limita la vida de los objetos y no
registres contenido ni material criptográfico. Los buffers temporales se limpian
cuando es posible, pero no se puede prometer borrado inmediato de strings
administrados.

## Importar y rotar

La aplicación entrega `WsaaCertificateContent` a `RotateAsync`. El store carga
el certificado, requiere una clave privada RSA exportable, normaliza en memoria
certificado y clave privada PEM sin contraseña y cifra el sobre antes de
persistirlo. No guarda el PFX de entrada ni una contraseña. Rechaza material
malformado, claves ausentes/no exportables y periodos X.509 inválidos.

PFX/P12 se importa con `EphemeralKeySet` en Windows y Linux. En macOS esta ruta
requiere PEM porque .NET no admite allí esa importación efímera; el store no
escribe claves temporales a disco. `FromPem` y `FromPkcs12` reciben contenido,
no rutas:

```csharp
WsaaCertificateContent content = WsaaCertificateContent.FromPem(certificatePem, privateKeyPem);
ArcaCertificateVersion first = await certificateStore.RotateAsync(scope, content,
    expectedActiveVersionId: null, cancellationToken);

ArcaCertificateVersion next = await certificateStore.RotateAsync(scope, replacement,
    expectedActiveVersionId: first.VersionId, cancellationToken);
```

`expectedActiveVersionId: null` significa crear solo si todavía no hay versión
activa. Para rotar una existente se debe enviar el ID activo que se observó. Si
otro proceso ganó la carrera, el método lanza
`ArcaCertificateConcurrencyException`; vuelve a leer y decide si debe reintentar
con el nuevo ID o detener la operación. No repitas ciegamente la rotación.

Las versiones guardadas son inmutables. `ListVersionsAsync` permite inspeccionar
metadatos históricos y `GetVersionAsync` conserva la selección exacta. No hay
borrado, expiración automática ni herramientas de reenvoltura de ciphertext en
este módulo; define y revisa cualquier política de retención por separado.

## Proteger las claves y recuperar copias

El protector `AesGcmArcaCertificateProtector` usa AES-256-GCM, nonce aleatorio
de 12 bytes y tag de 16 bytes. La AAD autentica el propósito del store, key ID,
scope, versión, fingerprint, vigencia y fecha de creación. La base conserva dos
tablas de metadatos y ciphertext; el keyring queda bajo control externo de la
aplicación.

Para activar una nueva clave de protección, incorpora su key ID al keyring,
selecciónala como clave de escritura y conserva todas las claves antiguas que
aún tengan ciphertext asociado. Respaldar solo la base no basta: una restauración
necesita el keyring correspondiente, protegido y respaldado por separado. Los
IDs de claves de cifrado son distintos de los certificados X.509. El store no
reenvuelve versiones históricas al cambiar la clave; sin una herramienta propia,
debes conservar las claves antiguas para poder leerlas.

Un keyring perdido o incompleto hace que las filas asociadas no puedan
descifrarse. Alterar el alcance, los metadatos autenticados o el ciphertext
también falla de forma cerrada. El store no devuelve contenido ante esos errores.

## Vigencia y avisos

Los campos `NotBeforeUtc` y `NotAfterUtc` provienen del certificado X.509 real.
Una importación estructuralmente válida puede quedar guardada aun si todavía no
es válida o ya venció, para permitir auditar su historial. Los métodos de
metadatos continúan disponibles; `GetActiveAsync` y `GetVersionAsync` no
entregan contenido fuera de vigencia y lanzan
`ArcaCertificateValidityException`.

La vigencia X.509 no demuestra que ARCA acepte el certificado, que el servicio
esté habilitado o que la CUIT esté autorizada. WSAA realiza su control al
autenticar y ARCA determina la autorización fiscal. `FindExpiringAsync` consulta
los certificados activos de una sola scope dentro de una ventana indicada; la
aplicación debe programar esa consulta y gestionar alertas. No hay scheduler,
renovación automática ni solicitud de certificados a ARCA.

## Esquema relacional

El módulo `TenantCertificates` crea solo estas dos tablas cuando se selecciona:

| Tabla | Contenido y claves |
| --- | --- |
| `NetArcaCertificateSlots` | Cabecera activa por `(TenantHash, Cuit, Environment)`; guarda `ActiveVersionId` y `Generation` para compare-and-swap. |
| `NetArcaCertificateVersions` | Versiones inmutables por `(TenantHash, Cuit, Environment, VersionId)`; contiene `CreatedAtUtcTicks`, `ThumbprintSha256`, `NotBeforeUtcTicks`, `NotAfterUtcTicks`, `KeyId`, `Nonce`, `Ciphertext` y `Tag`. |

`TenantHash` es SHA-256 del ID tenant. El identificador original se incluye en
el AAD al proteger y verificar el contenido, pero no se guarda en claro. La
relación entre cabecera y versiones es lógica; el módulo no agrega claves
foráneas. La transacción inserta la versión y actualiza la cabecera; un conflicto
de concurrencia revierte la inserción.

Las migraciones y su historia son independientes del diario de facturas y de
tickets WSAA. Deshabilitar el módulo conserva tablas, historia, ciphertext y
versiones anteriores. Consulta el [modelo relacional](Modelo-relacional.md) para
el DDL y los scripts de cada proveedor.
