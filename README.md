# NetArcaWs para .NET 10

Port C# de WSAA de [PyAfipWs](https://github.com/reingart/pyafipws), de Mariano
Reingart. Licencia **LGPL-3.0-or-later**. Incluye autenticación WSAA y health checks
opt-in. Las operaciones de negocio de WSFEv1, WSFEXv1, WSMTXCA y Padrón siguen
pendientes. No es todavía un port del
repositorio Python completo ni se ha publicado este paquete en nuget.org.

## Compilar, probar y empaquetar

Requiere SDK .NET 10.0.401 (global.json). Desde este directorio:

```sh
dotnet restore NetArcaWs.slnx
dotnet build NetArcaWs.slnx --configuration Release --no-restore
dotnet test --solution NetArcaWs.slnx --configuration Release --no-restore
dotnet pack src/NetArcaWs/NetArcaWs.csproj --configuration Release --no-build --output artifacts
```

Los tests usan xUnit v3 y FluentAssertions 7.2.1, con Microsoft.Testing.Platform.
Las pruebas unitarias no se conectan a ARCA. El paquete compilado localmente
puede instalarse desde una carpeta NuGet:

```sh
dotnet add package NetArcaWs --version 0.4.0 --source /ruta/absoluta/a/artifacts
```

## Autenticación

```csharp
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Cryptography;
using NetArcaWs.Wsaa;

var certificatePem = Environment.GetEnvironmentVariable("WSAA_CERTIFICATE_PEM")
    ?? throw new InvalidOperationException("Falta WSAA_CERTIFICATE_PEM.");
var privateKeyPem = Environment.GetEnvironmentVariable("WSAA_PRIVATE_KEY_PEM")
    ?? throw new InvalidOperationException("Falta WSAA_PRIVATE_KEY_PEM.");
var password = Environment.GetEnvironmentVariable("WSAA_KEY_PASSWORD");
var certificateContent = WsaaCertificateContent.FromPem(certificatePem, privateKeyPem, password);

var services = new ServiceCollection();
services.AddNetArcaWs(options =>
{
    options.Endpoint = WsaaOptions.HomologationEndpoint;
    options.Certificate = certificateContent;
});
using var provider = services.BuildServiceProvider();
var wsaa = provider.GetRequiredService<WsaaService>();
var cancellationToken = CancellationToken.None;
var ticket = await wsaa.AuthenticateAsync("wsfe", cancellationToken);
// Entregar ticket.Token y ticket.Sign al cliente del servicio de negocio.
// No registrarlos en logs: son credenciales.
Console.WriteLine($"Ticket válido hasta {ticket.ExpirationTime:O}");
```

El contenido se construye con el PEM que proporciona la aplicación; esta ruta no
necesita escribir la clave o el certificado en archivos temporales. La contraseña
es opcional para PEM sin cifrar y se suministra a `FromPem` cuando la clave privada
está cifrada. `WsaaCertificateContent` es inmutable, mantiene sus datos privados y
oculta su contenido en `ToString`. Si necesitás un `X509Certificate2` directamente,
`LoadCertificate()` devuelve un objeto desechable cuya vida y `Dispose` quedan a
cargo del llamador.

También podés importar un PKCS#12 desde bytes o desde una cadena Base64:

```csharp
var pfxBase64 = Environment.GetEnvironmentVariable("WSAA_PFX_BASE64")
    ?? throw new InvalidOperationException("Falta WSAA_PFX_BASE64.");
var certificateContent = WsaaCertificateContent.FromPkcs12Base64(
    pfxBase64,
    Environment.GetEnvironmentVariable("WSAA_PFX_PASSWORD"));
```

`FromPkcs12(byte[] data, string? password = null)` acepta los bytes del PFX. Base64
solo codifica esos bytes; no cifra el contenido. Protegé la variable Base64 y usá
la contraseña del PFX si corresponde. La importación PKCS#12 sin persistir la clave
usa `EphemeralKeySet` y funciona en Windows y Linux. .NET no admite esa importación
efímera de PFX en macOS: allí usá PEM; la biblioteca no recurre a archivos temporales.
Más detalles por plataforma en la [documentación de criptografía multiplataforma
de .NET](https://learn.microsoft.com/en-us/dotnet/standard/security/cross-platform-cryptography).

Para una operación con material de certificado explícito —por ejemplo, al rotar
una credencial fuera de un flujo multitenant— la aplicación puede leer el secreto
de manera asíncrona desde su almacén, construir un nuevo `WsaaCertificateContent`
e invocar `AuthenticateWithContentAsync`:

```csharp
// certificatePem, privateKeyPem y password provienen de la configuración
// ya cargada por la aplicación para esta identidad.
var rotatedContent = WsaaCertificateContent.FromPem(certificatePem, privateKeyPem, password);
var ticket = await wsaa.AuthenticateWithContentAsync("wsfe", rotatedContent, cancellationToken);
```

NetArcaWs no incluye un SDK de vault ni carga secretos por su cuenta. El valor de
`WsaaOptions.Certificate` se captura cuando se crea el servicio singleton; cambiar
la configuración después no reemplaza ese contenido. Para cambiar certificados por
rotación de una identidad individual en una instancia activa, pasá el contenido de
esa operación a `AuthenticateWithContentAsync`. Para una selección multitenant,
usá el contexto descrito a continuación.

## Contexto multitenant para WSAA

`ArcaTenantContext` agrupa el identificador de tenant, el CUIT representado, el
ambiente y el certificado que resolvió la aplicación. Es inmutable: sus propiedades
son de solo lectura. WSAA elige el endpoint oficial correspondiente al ambiente sin
modificar opciones globales del servicio.

```csharp
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;

// Estos valores deben provenir de la resolución autenticada/autorizada
// del tenant y de su configuración protegida, no de campos libres del request.
string tenantId = resolvedTenantId;
long cuitRepresentado = resolvedCuit;
var environment = ArcaEnvironment.Production;
var tenant = new ArcaTenantContext(tenantId, cuitRepresentado, environment, certificateContent);
var ticket = await wsaa.AuthenticateForTenantAsync("wsfe", tenant, cancellationToken);
```

La aplicación debe autorizar la selección del tenant y resolver su CUIT, ambiente
y certificado desde datos confiables antes de crear el contexto. No aceptes el
`TenantId`, el CUIT, el ambiente ni el certificado directamente como identidad
autorizada desde un request. El CUIT representado puede diferir del titular del
certificado cuando ARCA autorizó la delegación; la aplicación valida esa relación
con su propio modelo y las relaciones vigentes de ARCA, no por igualdad de valores.

`AuthenticateWithContentAsync` sigue disponible para una llamada con material
explícito, pero no representa por sí solo una frontera de aislamiento multitenant.
Para la selección por tenant, usá `AuthenticateForTenantAsync`: la identidad local
de caché incluye tenant, CUIT, endpoint/ambiente, servicio y huella del certificado.
Esa separación local no hace que ARCA considere distintas dos identidades que usan
el mismo certificado; compartirlo entre tenants todavía puede provocar
`coe.alreadyAuthenticated` y no garantiza independencia en el servidor.

Este contexto es el punto de partida para los futuros clientes de WSFEv1, WSFEXv1,
WSMTXCA y Padrones, que todavía no están implementados. Los health checks actuales
comprueban disponibilidad de infraestructura por servicio y ambiente; no aceptan
certificados ni contexts de tenant, y no verifican autorización fiscal.

El contrato y los límites de este flujo se detallan en el
[ADR de contexto multitenant](docs/adr/0002-arca-tenant-context.md).

El certificado debe estar emitido y autorizado por ARCA para el entorno y
servicio elegidos. Un certificado autofirmado sirve para tests unitarios, pero
no sustituye esas autorizaciones. Para producción seleccionar explícitamente
`WsaaOptions.ProductionEndpoint` en el flujo de opciones; en el flujo multitenant,
el ambiente del contexto elige el endpoint oficial.

Si la aplicación usa `LoadCertificate()` directamente, conserva y libera ese
`X509Certificate2`. `AddNetArcaWs` registra el servicio y la caché compartida; no
crear un contenedor por factura. Devuelve `IHttpClientBuilder` para configurar
timeout, proxy o autoridades de confianza. El transporte conserva la validación
TLS de .NET.

## CLI de certificados

El paquete `NetArcaWs.Tool` agrega los comandos `cert-dev`, `cert-prod` y
`cert-info`. Puede instalarse con un manifiesto local de herramientas .NET,
como EF Core. Genera claves y CSR; ARCA debe emitir el certificado y autorizar
los servicios. Ver [instalación y guía de la CLI](docs/certificates-cli.md).

```sh
dotnet pack src/NetArcaWs.Tool/NetArcaWs.Tool.csproj --configuration Release --no-build --output artifacts
dotnet new tool-manifest
dotnet tool install --local NetArcaWs.Tool --add-source ./artifacts --version 0.4.0
dotnet tool run netarcaws cert-dev --cuit "$ARCA_CUIT" --organization "Mi Empresa" --name "Mi App" --output ./certificados/dev --password-env NETARCA_KEY_PASSWORD
```

Definir previamente `ARCA_CUIT` y `NETARCA_KEY_PASSWORD` en el entorno.
Si el proyecto ya tiene manifiesto de herramientas, omitir `dotnet new tool-manifest`.

## Claves y CSR

```csharp
using System.Security.Cryptography;
using NetArcaWs.Cryptography;

var password = Environment.GetEnvironmentVariable("WSAA_KEY_PASSWORD");
var pem = WsaaCryptography.CreatePrivateKey(4096, password);
using var rsa = RSA.Create();
if (string.IsNullOrEmpty(password)) rsa.ImportFromPem(pem);
else rsa.ImportFromEncryptedPem(pem, password);
var csr = WsaaCryptography.CreateCertificateRequest(
    rsa, "mi-aplicacion", "AR", "Mi Empresa SA", "20123456789");
await File.WriteAllTextAsync("empresa.csr", csr);
// Persistir pem en el almacén de secretos elegido por la aplicación.
```

Las claves nuevas usan PKCS#8, opcionalmente cifrado con AES-256-CBC y PBKDF2.
`LoadPem` admite PKCS#1/PKCS#8 sin cifrar, PKCS#8 cifrado y el formato tradicional
RSA PEM AES-256-CBC generado por PyAfipWs. Otros cifrados tradicionales se
rechazan explícitamente. No se invoca OpenSSL ni se usa BouncyCastle.

## Tickets, caché y errores

`CreateTra` genera el TRA; `WsaaCryptography.SignTra` devuelve CMS attached en
Base64; `LoginCmsAsync` realiza una llamada sin caché; `AuthenticateAsync`
combina el flujo y reutiliza el TA. `ParseTicket` permite analizar un TA
persistido, `GetTag` extrae un elemento y `IsExpired` compara su vencimiento.

La clave de caché combina endpoint, huella SHA-256 del certificado y servicio.
Rotar el certificado produce otra identidad de caché. El vencimiento es el
`expirationTime` del TA, usualmente doce horas desde su emisión. No se renueva
antes de vencer para evitar `coe.alreadyAuthenticated`. La caché en memoria se
pierde al reiniciar y no coordina réplicas: usar el mismo certificado desde
varias instancias no crea una caché TA distribuida.

Los SOAP Faults arrojan `WsaaSoapException` con `FaultCode`, `FaultString`,
`Detail` y `StatusCode`. XML inválido arroja `FormatException`; fallas HTTP sin
SOAP válido, `HttpRequestException`; cancelación, `OperationCanceledException`.
No se reintenta automáticamente un login fallido. Los mensajes de error de
ARCA pueden incluir información sensible: evitar volcados sin filtrar.

## Health checks por servicio

El registro es **opt-in**: solo se consultan los WS que agregue la aplicación.
No hace falta registrar `AddNetArcaWs`, cargar certificados ni solicitar un TA
para usar estos checks. Ejemplo en una aplicación ASP.NET Core:

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NetArcaWs.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks()
    .AddWsaaHealthCheck(ArcaEnvironment.Production, timeout: TimeSpan.FromSeconds(3))
    .AddWsfev1HealthCheck(ArcaEnvironment.Production, timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();
app.MapHealthChecks("/health/arca", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("arca"),
    ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        services = report.Entries.ToDictionary(entry => entry.Key, entry => new
        {
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
            data = entry.Value.Data
        })
    }, cancellationToken: context.RequestAborted)
});
app.Run();
```

También se exponen `AddWsfexv1HealthCheck`, `AddWsmtxcaHealthCheck`,
`AddPadronA4HealthCheck`, `AddPadronA5HealthCheck`, `AddPadronA10HealthCheck` y
`AddPadronA13HealthCheck`. El registro genérico es
`AddNetArcaWsService(ArcaService.Wsfev1, ...)`. Cada registro acepta entorno,
timeout, endpoint alternativo y nombre; por defecto usa homologación y 5 segundos.
Los nombres por defecto incluyen servicio y entorno para permitir ambos a la vez.

Un Dummy solo devuelve `Healthy` si sus tres componentes informan `OK`. Fallos
HTTP, SOAP, timeout, XML inválido o componentes no disponibles devuelven
`Unhealthy`; el endpoint ASP.NET responde 503 cuando alguno falla. El resultado
incluye servicio, entorno, tiempo de respuesta y estados por componente. Una
falla de red indica indisponibilidad **desde la aplicación que ejecuta el check**,
no demuestra una caída global de ARCA.

WSAA no expone Dummy: su check solo verifica HTTPS y la presencia del contrato
WSDL con loginCms. Su resultado incluye `probe=wsdl` y
`authenticationVerified=false`; no prueba que un certificado pueda autenticarse.
Los Dummy tampoco comprueban autorizaciones fiscales de un contribuyente.

No hay sondeo en segundo plano, reintentos ni notificaciones automáticas. El
monitor del consumidor puede consultar el endpoint con la frecuencia elegida
y alertar ante fallos. Conviene mantener los checks externos separados del
endpoint de liveness para que una caída de ARCA no reinicie la aplicación.

## Reintentos y prevención de duplicados

No se aplica retry automático a las llamadas HTTP del paquete. El diseño para
facturación está en el [ADR de reintentos seguros](docs/adr/0001-safe-invoice-retries.md):
persistir identidad y contenido de negocio antes del envío; tratar un timeout como
resultado desconocido; consultar el comprobante exacto antes de decidir un reenvío;
y conservar número e identificador al repetir. WSFEX tiene una regla específica
de reproceso con el mismo `Cmp.Id`, documentada por ARCA.

La coordinación necesita almacenamiento durable y restricciones únicas para
proteger varias instancias. Un health check verde o una caché en memoria no
resuelven esa garantía. **Este flujo de emisión se implementará en los hitos de
facturación; no está implementado en esta versión.**

## Homologación

Configurar en el entorno del proceso, sin subir certificados ni claves al repo:

```sh
export WSAA_CERT_PATH=/ruta/certificado-homologacion.crt
export WSAA_KEY_PATH=/ruta/privada.key
export WSAA_SERVICE=wsfe
dotnet test --project tests/NetArcaWs.IntegrationTests/NetArcaWs.IntegrationTests.csproj --configuration Release
```

`WSAA_KEY_PASSWORD` es opcional. Sin las variables requeridas la prueba queda
**omitida**, no aprobada. La suite usa únicamente homologación. Repetirla antes
del vencimiento del TA anterior puede causar `coe.alreadyAuthenticated` porque
cada ejecución es un proceso nuevo. Esta entrega no acredita una autenticación
real hasta ejecutar esa prueba con credenciales autorizadas.

## Alcance y licencia

Ver [ARCHITECTURE.md](ARCHITECTURE.md) y la
[matriz de equivalencias y progreso](docs/plans/hito-1.md).
Los wrappers COM, CLI Python, debug con secretos y caché de archivos no se
replican: se reemplazan por APIs C#, excepciones, DI e IMemoryCache.

Copyright upstream: (C) 2008-2021 Mariano Reingart. Este port y sus cambios se
distribuyen bajo LGPL-3.0-or-later. Se incluyen [LICENSE](LICENSE), los términos
GPL complementarios en [COPYING](COPYING) y
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
