# NetArcaWs para .NET 10

Port C# de WSAA de [PyAfipWs](https://github.com/reingart/pyafipws), de Mariano
Reingart. Licencia **LGPL-3.0-or-later**. El Hito 1 implementa autenticación;
WSFEv1, WSFEXv1, WSMTXCA y Padrón siguen pendientes. No es todavía un port del
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
dotnet add package NetArcaWs --version 0.1.0 --source /ruta/absoluta/a/artifacts
```

## Autenticación

```csharp
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Cryptography;
using NetArcaWs.Wsaa;

var services = new ServiceCollection();
services.AddNetArcaWs(options =>
{
    options.Endpoint = WsaaOptions.HomologationEndpoint;
});
using var provider = services.BuildServiceProvider();
using var certificate = WsaaCryptography.LoadPem(
    await File.ReadAllTextAsync("certificado.crt"),
    await File.ReadAllTextAsync("privada.key"),
    Environment.GetEnvironmentVariable("WSAA_KEY_PASSWORD"));

var wsaa = provider.GetRequiredService<WsaaService>();
var ticket = await wsaa.AuthenticateAsync("wsfe", certificate);
// Entregar ticket.Token y ticket.Sign al cliente del servicio de negocio.
// No registrarlos en logs: son credenciales.
Console.WriteLine($"Ticket válido hasta {ticket.ExpirationTime:O}");
```

El certificado debe estar emitido y autorizado por ARCA para el entorno y
servicio elegidos. Un certificado autofirmado sirve para tests unitarios, pero
no sustituye esas autorizaciones. Para producción seleccionar explícitamente
`WsaaOptions.ProductionEndpoint`.

El consumidor conserva y libera el certificado. `AddNetArcaWs` registra el
servicio y la caché compartida; no crear un contenedor por factura. Devuelve
`IHttpClientBuilder` para configurar timeout, proxy o autoridades de confianza.
El transporte conserva la validación TLS de .NET.

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
El vencimiento es el `expirationTime` del TA, usualmente doce horas desde su
emisión. No se renueva antes de vencer para evitar `coe.alreadyAuthenticated`.
La caché en memoria se pierde al reiniciar y no coordina réplicas: múltiples
procesos con la misma credencial requieren una estrategia compartida adicional.

Los SOAP Faults arrojan `WsaaSoapException` con `FaultCode`, `FaultString`,
`Detail` y `StatusCode`. XML inválido arroja `FormatException`; fallas HTTP sin
SOAP válido, `HttpRequestException`; cancelación, `OperationCanceledException`.
No se reintenta automáticamente un login fallido. Los mensajes de error de
ARCA pueden incluir información sensible: evitar volcados sin filtrar.

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
