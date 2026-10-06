<!-- Source: docs/wiki/Inicio-rapido.md. Generated wiki mirror; edit the repository source. -->

# Inicio rápido

NetArcaWs apunta a .NET 10. El repositorio fija SDK `10.0.401` en `global.json`.
Los paquetes `NetArcaWs` y `NetArcaWs.Tool` versión `0.5.0` fueron aceptados e
indexados por NuGet.

## Referenciar la biblioteca

La aplicación puede consumir el paquete publicado desde nuget.org:

```sh
dotnet add package NetArcaWs --version 0.5.0
```

También puede usar el paquete local desde la carpeta de artifacts:

```sh
dotnet add package NetArcaWs --version 0.5.0 --source /ruta/absoluta/a/artifacts
```

La versión concreta debe coincidir con el paquete local generado por el checkout.
Para comandos de build/test y de creación de paquetes, ver [Desarrollo y contribución](Desarrollo-y-contribucion).

## Autenticar una identidad fija

La aplicación obtiene el certificado y la clave de su configuración protegida o
vault y los entrega como contenido. Este ejemplo evita materializar credenciales
en archivos:

```csharp
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Cryptography;
using NetArcaWs.Wsaa;

var certificatePem = Environment.GetEnvironmentVariable("WSAA_CERTIFICATE_PEM")
    ?? throw new InvalidOperationException("Falta WSAA_CERTIFICATE_PEM.");
var privateKeyPem = Environment.GetEnvironmentVariable("WSAA_PRIVATE_KEY_PEM")
    ?? throw new InvalidOperationException("Falta WSAA_PRIVATE_KEY_PEM.");
var password = Environment.GetEnvironmentVariable("WSAA_KEY_PASSWORD");
var content = WsaaCertificateContent.FromPem(certificatePem, privateKeyPem, password);

var services = new ServiceCollection();
services.AddNetArcaWs(options =>
{
    options.Endpoint = WsaaOptions.HomologationEndpoint;
    options.Certificate = content;
});
using var provider = services.BuildServiceProvider();
var wsaa = provider.GetRequiredService<WsaaService>();
var ticket = await wsaa.AuthenticateAsync("wsfe", cancellationToken);
```

El certificado debe estar emitido y habilitado por ARCA para el entorno y
servicio. Un certificado de prueba local no valida su aceptación por ARCA. Para
usar identidades distintas por operación, consultar [Contexto multitenant](Contexto-multitenant).

## Alcance actual

La autenticación obtiene el TA y lo cachea localmente hasta su vencimiento. El
paquete no crea facturas en este ejemplo y no reintenta escrituras fiscales. Los
health checks y el CLI son flujos separados. Ver [WSAA y certificados](WSAA-y-certificados) y
[Servicios y cobertura](Servicios-y-cobertura) antes de diseñar una integración de producción.
