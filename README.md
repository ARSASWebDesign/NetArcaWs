# NetArcaWs para .NET 10

Port .NET 10 de [PyAfipWs](https://github.com/reingart/pyafipws), de Mariano
Reingart. Licencia **LGPL-3.0-or-later**. Además de WSAA y health checks opt-in,
el código ya contiene contratos tipados y fachadas de transporte para las 81
operaciones SOAP de los snapshots ARCA actuales de WSFEv1 (22), WSFEXv1 (19),
WSMTXCA (27), Padrón A4 (2), Constancia en la ruta A5 (5), A10 (2) y A13 (4).
Los contratos y las fachadas de Hitos 2–5, su suite, mapeos QName/action y
roundtrips de serialización fueron verificados. Esto no afirma paridad funcional
completa con el repositorio Python ni homologación de operaciones autenticadas.

Verificación registrada el 2026-10-06: build Release con 0 warnings/errores;
201 casos de suite, 198 aprobados y 3 omitidos. Se comprobaron QName y
SOAPAction de las 81 operaciones contra sus WSDL y 166 tipos raíz XML en
round-trip. Los 22 tipos de contrato con campos `DateTime` (`xs:date` y
`xs:dateTime`) conservaron sus valores. En QA real respondieron los siete
probes `Dummy` en una corrida `ARCA_RUN_HOMOLOGY=1` de 9 casos; dos pruebas
autenticadas se omitieron por falta de certificados. Los Dummies prueban
disponibilidad, no autorizaciones fiscales.

## Extras y adaptaciones propias respecto de PyAfipWs

Estas extensiones describen la API y arquitectura de NetArcaWs. No implican
paridad completa ni que Python carezca de certificados, CSR, caché, CLI, Dummy o tests:
son capacidades también presentes en el proyecto de origen. Las diferencias se
contrastan con el [upstream](https://github.com/reingart/pyafipws) y la
[matriz del port](docs/plans/hito-1.md).

| Extra o adaptación .NET | Estado y alcance |
| --- | --- |
| Biblioteca .NET 10 y empaquetado NuGet | Implementado; sin runtime Python ni wrappers COM. NetArcaWs 0.5.0 y NetArcaWs.Tool 0.5.0 publicados e indexados en NuGet |
| Criptografía nativa de .NET | CMS/TRA, RSA y CSR con `System.Security.Cryptography.Pkcs` y `System.Formats.Asn1`; sin procesos OpenSSL ni BouncyCastle |
| API asíncrona e inyección de dependencias | `Task`, `CancellationToken`, `IHttpClientFactory`, opciones y `TimeProvider`; SOAP con `HttpClient` y `XmlSerializer` |
| Certificados como contenido | `WsaaCertificateContent`: PEM, PFX/P12 en bytes o Base64, configuración o parámetro por operación; apto para secretos obtenidos de vault/BD por la aplicación |
| Contexto multitenant explícito | `ArcaTenantContext`: tenant, CUIT representada, entorno y certificado; usado por WSAA y las operaciones autenticadas de las fachadas SOAP actuales; autorización del tenant sigue a cargo de la aplicación |
| Caché compartida dentro del proceso | `IMemoryCache`, vencimiento real del TA y coordinación de logins concurrentes; rotación separada por huella; no es caché distribuida |
| Health checks integrables en ASP.NET Core | `IHealthCheck`, registro opt-in por WS/entorno, timeout, tags y estado de componentes; sin certificados ni login |
| Herramienta instalable con `dotnet tool` | `cert-dev`, `cert-prod`, `cert-info`; manifiesto local o instalación global, contraseña por variable de entorno y protección contra sobrescrituras |
| Manejo de recursos y errores | Certificados importados liberados por operación, claves PFX efímeras donde se soportan, XML sin DTD, límites configurables de 4 MiB para request/response y timeout de lectura; secretos ocultos en `ToString` |
| Validación propia del port | xUnit/FluentAssertions, SOAP simulado, pruebas de firmas y aislamiento concurrente entre tenants, suite separada de homologación y CI de build/test/pack/instalación de la CLI |
| Arquitectura trazable | ADR de certificados en memoria y multitenancy; límites y equivalencias documentados |
| Diario fiscal y coordinación durable | `SafeInvoiceService` + `IInvoiceJournal` / `InvoiceCoordinator` ofrecen autorización unitaria y reconciliación exacta en WSFE/WSFEX/WSMTXCA; implementación y suite local verificadas. SQLite sirve a procesos de un host, no NFS ni multi-host; sin worker ni reenvío de estados inciertos |
| Clientes SOAP por contrato ARCA | 81 fachadas tipadas verificadas por QName/action y roundtrips; sin WCF. La autorización fiscal real requiere certificados y se valida aparte |
| Wiki integral del repositorio | [Wiki publicada](https://github.com/ARSASWebDesign/NetArcaWs/wiki); revisión exhaustiva del inventario upstream pendiente |

Ver las decisiones de [multitenancy](docs/adr/0002-arca-tenant-context.md),
[certificados en memoria](docs/adr/0003-in-memory-certificates.md) y
[reintentos seguros](docs/adr/0001-safe-invoice-retries.md) y
[contratos SOAP públicos](docs/adr/0004-public-soap-contracts.md).

## Contratos SOAP ARCA disponibles

El código generado ofrece las siguientes fachadas, con una operación async por
cada operación de los WSDL versionados. Los requests y responses son contratos
tipados 1:1 con esos schemas; las llamadas autenticadas reciben un
`ArcaTenantContext` y `CancellationToken`, y las operaciones `Dummy` toman el
ambiente sin obtener ticket. Las fachadas pasaron la suite y la verificación de
QName/actions contra los WSDL; los 166 tipos raíz XML se comprobaron en round-trip.
No se ejecutaron llamadas autenticadas de negocio en homologación por falta de
certificados autorizados.

| Fachada | Operaciones del snapshot | Alcance |
| --- | ---: | --- |
| `Wsfev1Service` | 22 | CAE, CAEA, consultas y parámetros WSFEv1 |
| `Wsfexv1Service` | 19 | Autorización, consultas, validación de permisos y parámetros de exportación |
| `Wsmtxcav1Service` | 27 | Comprobantes con detalle, CAEA, ajustes IVA, consultas y parámetros |
| `PadronA4Service` | 2 | `dummy`, `getPersona` |
| `PadronA5Service` | 5 | Constancia en la ruta histórica `personaServiceA5`; service WSAA `ws_sr_constancia_inscripcion` |
| `PadronA10Service` | 2 | `dummy`, `getPersona` |
| `PadronA13Service` | 4 | `dummy`, `getIdPersonaListByDocumento`, `getPersona`, `getPersonaV2` |
| **Total** | **81** | Superficie actual de esos contratos versionados |

Las solicitudes y respuestas se derivan de los WSDL de producción fijados en
[`docs/reference/contracts`](docs/reference/contracts), cotejados con los
snapshots de homologación disponibles. La matriz de operaciones, fuente y
estado de cobertura está en [Servicios y cobertura](docs/wiki/Servicios-y-cobertura.md).
El snapshot de homologación WSMTXCA archivado está truncado y no es un contrato
válido para generar modelos; esa superficie se derivó del WSDL de producción y
requiere cotejo con la fuente desplegada antes del release. WSMTXCA es el servicio
de facturación con detalle y CAE/CAEA que define su manual; no representa por sí
solo todo el ciclo de Factura de Crédito Electrónica MiPyME. La API de servicio
está disponible en `NetArcaWs.Services`; `AddNetArcaWs` registra WSAA, el
transporte compartido, las siete fachadas por servicio y la fachada agrupadora
`PadronService`.

Ejemplo de consulta WSFEv1 por DI. La aplicación resuelve y autoriza `tenant`
antes de construirlo; el método obtiene el ticket para esa llamada.

```csharp
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Services;

var wsfe = provider.GetRequiredService<Wsfev1Service>();
var last = await wsfe.FECompUltimoAutorizadoAsync(
    tenant,
    new FeCompUltimoAutorizado
    {
        Auth = new FeAuthRequest(), // Token, Sign y CUIT se completan con WSAA.
        PtoVta = pointOfSale,
        CbteTipo = voucherType
    },
    cancellationToken);
```

Los tipos generados reflejan los primitivos del schema; por ejemplo, campos
`xsd:double` de WSFE se mantienen como `double`. `TaxCalculator` ofrece
operaciones `decimal` opt-in para cálculos básicos, no convierte el contrato SOAP
ni valida todas las reglas fiscales del servicio.

## Documentación y wiki final

La [wiki del proyecto](https://github.com/ARSASWebDesign/NetArcaWs/wiki) reúne
29 páginas de contenido, barra lateral y manifiesto de fuentes: guías .NET,
README, arquitectura, ADR, contribuciones, releases, CLI, contratos y planes.
El mirror `docs/wiki-export/` se genera desde la documentación versionada con
enlaces internos convertidos. La revisión exhaustiva del inventario upstream y
los ejemplos por operación siguen en el [plan del wiki](docs/plans/hito-7-wiki.md);
la publicación no implica paridad completa con PyAfipWs. La
cuenta NuGet `arsas`, el environment GitHub `nuget` y la política de Trusted
Publishing están configurados. El workflow intercambió OIDC y NuGet aceptó e
indexó ambos paquetes 0.5.0 en el índice v3; la [release](https://github.com/ARSASWebDesign/NetArcaWs/releases/tag/v0.5.0)
está publicada.

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
dotnet add package NetArcaWs --version 0.5.0 --source /ruta/absoluta/a/artifacts
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

Las fachadas tipadas WSFEv1, WSFEXv1, WSMTXCA y Padrón reciben este contexto en
cada operación autenticada; la suite y el contrato de contexto se verificaron.
La autenticación fiscal en vivo sigue pendiente de certificados autorizados. Los
health checks comprueban disponibilidad por servicio/ambiente; no reciben
certificados ni contexto tenant y no verifican autorización fiscal.

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
dotnet tool install --local NetArcaWs.Tool --add-source ./artifacts --version 0.5.0
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

## Reintentos y diario fiscal

No se aplica retry automático a las llamadas SOAP. `InvoiceCoordinator`,
`IInvoiceJournal` y `SqliteInvoiceJournal` permiten persistir identidad y payload
antes del envío, adquirir leases y dejar resultado incierto para reconciliación;
el archivo SQLite permite procesos en un host, no NFS ni varios hosts. Para
réplicas distribuidas se necesita otro proveedor transaccional de `IInvoiceJournal`.
`SafeInvoiceService` conecta autorización unitaria de WSFE, WSFEX y WSMTXCA con
el coordinador y sus consultas exactas de reconciliación. Una respuesta negativa
o no concluyente permanece `Unknown`; reconciliar no reenvía. `ResumeAsync` envía
solo snapshots que sigan `Prepared`; las correcciones de rechazos requieren una
revisión auditable y una llamada explícita posterior. Hitos 2–4 y sus
pruebas/contratos siguen en validación, así que no se afirma cierre funcional ni
emisión exactamente una vez. Un health check verde tampoco resuelve el resultado
fiscal.

Para el diario SQLite local, registrar `services.AddNetArcaWsSqliteInvoicing(path)`
y resolver `SafeInvoiceService`. Si se usa almacenamiento transaccional propio,
registrar `IInvoiceJournal` antes de llamar `AddNetArcaWsInvoicing()`. Las APIs de
autorización son `AuthorizeWsfeAsync`, `AuthorizeWsfexAsync` y
`AuthorizeWsmtxcaAsync`; `ReconcileAsync(tenant, key)` solo consulta la operación
guardada. `ReviseRejectedAsync` crea una revisión preparada sin enviarla.
La guía [Diario fiscal](docs/wiki/Diario-fiscal.md) incluye las restricciones por
protocolo y recuperación de `ListPendingAsync` mediante `ResumeAsync`.

## Homologación

La suite actual debe volver a validarse junto con las nuevas fachadas SOAP. La
homologación WSAA continúa siendo opt-in y requiere certificados autorizados;
no hay credenciales disponibles en el repositorio. Para la prueba WSAA existente,
configurar en el entorno del proceso, sin subir certificados ni claves al repo:

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

## Contribuir desde un fork

Las contribuciones se reciben mediante forks y pull requests hacia `main`.
Ver [CONTRIBUTING.md](CONTRIBUTING.md) para preparar el entorno, ejecutar las
verificaciones y proponer cambios. El repositorio incluye formularios de bugs y
propuestas, y una plantilla de PR. No publicar certificados, claves, tickets ni
datos fiscales reales en issues o ejemplos.

## Releases

El workflow [Release](.github/workflows/release.yml) genera y publica la biblioteca
`NetArcaWs` y el tool `NetArcaWs.Tool` al publicar una release GitHub. Permite un
ensayo manual sin publicar. La primera release v0.5.0 completó Trusted
Publishing mediante OIDC y ambos paquetes fueron aceptados e indexados en NuGet.
Ver [publicación y recuperación de releases](docs/releases.md).
