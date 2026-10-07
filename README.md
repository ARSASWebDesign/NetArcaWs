# NetArcaWs para .NET 10

[![Última release](https://img.shields.io/github/v/release/ARSASWebDesign/NetArcaWs?label=GitHub%20release)](https://github.com/ARSASWebDesign/NetArcaWs/releases) [![NetArcaWs en NuGet](https://img.shields.io/nuget/v/NetArcaWs?label=NuGet%20biblioteca)](https://www.nuget.org/packages/NetArcaWs) [![NetArcaWs.Tool en NuGet](https://img.shields.io/nuget/v/NetArcaWs.Tool?label=NuGet%20tool)](https://www.nuget.org/packages/NetArcaWs.Tool) [![CI](https://github.com/ARSASWebDesign/NetArcaWs/actions/workflows/ci.yml/badge.svg)](https://github.com/ARSASWebDesign/NetArcaWs/actions/workflows/ci.yml) [![Release](https://github.com/ARSASWebDesign/NetArcaWs/actions/workflows/release.yml/badge.svg)](https://github.com/ARSASWebDesign/NetArcaWs/actions/workflows/release.yml)

NetArcaWs es una biblioteca y herramienta .NET 10 para integrar servicios web de ARCA, basada en [PyAfipWs](https://github.com/reingart/pyafipws) de Mariano Reingart y distribuida bajo licencia LGPL-3.0-or-later.

Ofrece clientes SOAP tipados, autenticación WSAA, selección explícita de identidad por operación, health checks opt-in y una CLI para preparar certificados y solicitudes CSR. La cobertura implementada y sus límites se describen en la [wiki publicada](https://github.com/ARSASWebDesign/NetArcaWs/wiki); el alcance del código no implica paridad completa con PyAfipWs ni homologación de todas las operaciones.

## Instalación

Agregá el paquete principal a un proyecto .NET 10:

```sh
dotnet add package NetArcaWs
```

La aplicación debe contar con un certificado emitido por ARCA y autorizado para el servicio y ambiente elegidos. La herramienta `NetArcaWs.Tool` ayuda a generar claves y CSR; ARCA emite el certificado.

## Inicio rápido

Registrá los servicios una vez al iniciar la aplicación y obtené el cliente tipado por inyección de dependencias. El ejemplo siguiente consulta un catálogo de WSCPE; la aplicación debe construir el contexto a partir de un tenant autenticado y autorizado.

```csharp
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using NetArcaWs.Wsaa;

var certificate = WsaaCertificateContent.FromPem(certificatePem, privateKeyPem);
var services = new ServiceCollection();
services.AddNetArcaWs(options =>
{
    options.Endpoint = WsaaOptions.HomologationEndpoint;
    options.Certificate = certificate;
});
using var provider = services.BuildServiceProvider();

var tenant = new ArcaTenantContext(
    tenantId: authorizedTenantId,
    cuit: representedCuit,
    environment: ArcaEnvironment.Homologation,
    certificate: certificate);
var wscpe = provider.GetRequiredService<IWscpeService>();
var provinces = await wscpe.consultarProvinciasAsync(
    tenant,
    new NetArcaWs.Contracts.Wscpe.ConsultarProvinciasRequest(),
    cancellationToken);
```

El host resuelve y autoriza tenant, CUIT, ambiente y certificado antes de crear `ArcaTenantContext`; no uses valores libres del request para elegir credenciales. Podés proporcionar el contenido PEM, PFX/P12 o Base64 desde la configuración o un almacén de secretos administrado por tu aplicación.

Consultá la [guía de inicio rápido](https://github.com/ARSASWebDesign/NetArcaWs/wiki/Inicio-rapido), [WSAA y certificados](https://github.com/ARSASWebDesign/NetArcaWs/wiki/WSAA-y-certificados) y [contexto multitenant](https://github.com/ARSASWebDesign/NetArcaWs/wiki/Contexto-multitenant) para configurar autenticación y ambientes.

## Servicios disponibles

Las fachadas tipadas exponen las operaciones implementadas para estos contratos. La wiki contiene el detalle de métodos, autenticación, ejemplos y límites por servicio.

| Servicio | Funcionalidad | Guía |
| --- | --- | --- |
| WSFEv1 | Facturación electrónica nacional, CAE/CAEA, consultas y parámetros | [WSFEv1 y servicios implementados](https://github.com/ARSASWebDesign/NetArcaWs/wiki/Servicios-implementados) |
| WSFEXv1 | Facturación de exportación, autorizaciones, consultas y parámetros | [Servicios implementados](https://github.com/ARSASWebDesign/NetArcaWs/wiki/Servicios-implementados) |
| WSMTXCA | Comprobantes con detalle, CAE/CAEA, consultas y parámetros | [Servicios implementados](https://github.com/ARSASWebDesign/NetArcaWs/wiki/Servicios-implementados) |
| Padrón A4, A5, A10 y A13 | Consultas de personas y constancias según cada contrato | [Servicios implementados](https://github.com/ARSASWebDesign/NetArcaWs/wiki/Servicios-implementados) |
| WSCDC | Constatación y consulta de comprobantes | [WSCDC](https://github.com/ARSASWebDesign/NetArcaWs/wiki/WSCDC) |
| WSFECred | Operaciones del ciclo de Factura de Crédito Electrónica MiPyME | [WSFECred](https://github.com/ARSASWebDesign/NetArcaWs/wiki/WSFECred) |
| WSCPE | Carta de Porte Electrónica y sus catálogos | [WSCPE](https://github.com/ARSASWebDesign/NetArcaWs/wiki/WSCPE) |

WSMTXCA corresponde a facturación con detalle; WSFECred cubre el ciclo de Factura de Crédito Electrónica MiPyME. La autorización fiscal depende de los permisos ARCA de cada certificado y CUIT.

## Funcionalidades adicionales

Los clientes usan SOAP 1.1 sobre `HttpClient`, contratos XML tipados y APIs asíncronas con cancelación. WSAA conserva los tickets en caché local hasta su vencimiento efectivo; esta caché no se comparte entre procesos o réplicas.

Los health checks se agregan optativamente por servicio y ambiente. Verifican disponibilidad técnica y no prueban autenticación, autorización de una CUIT ni una operación fiscal. La [guía de funcionalidades adicionales](https://github.com/ARSASWebDesign/NetArcaWs/wiki/Funcionalidades-adicionales) reúne health checks, cálculos decimales y diferencias respecto de PyAfipWs.

La [herramienta de certificados](https://github.com/ARSASWebDesign/NetArcaWs/wiki/Herramienta-de-certificados) genera claves y CSR y permite inspeccionar certificados locales; ARCA debe emitir el certificado. Protegé las claves y no registres certificados, claves privadas, tickets o datos fiscales en logs.

## Persistencia opcional

El paquete principal no depende de EF Core; los paquetes opcionales agregan persistencia para SQLite, MySQL/MariaDB, PostgreSQL y SQL Server, con migraciones oficiales por motor y módulo cuando se seleccionan. El store versionado de certificados también es opt-in y requiere un keyring externo administrado por la aplicación; la [guía de certificados multitenant](docs/wiki/Certificados-multitenant.md) documenta su uso y límites. Esta API se incorporó después de la publicación 0.6.0 y estará disponible en la siguiente publicación de los paquetes EF.

La guía [Diario fiscal](https://github.com/ARSASWebDesign/NetArcaWs/wiki/Diario-fiscal) explica configuración y recuperación, y [Modelo relacional](https://github.com/ARSASWebDesign/NetArcaWs/wiki/Modelo-relacional) documenta las tablas y las migraciones oficiales opcionales. El diario durable cubre autorizaciones unitarias WSFEv1, WSFEXv1 y WSMTXCA; una respuesta incierta requiere reconciliación y no se reenvía automáticamente.

## Documentación

La [wiki del proyecto](https://github.com/ARSASWebDesign/NetArcaWs/wiki) contiene guías de uso, catálogo de servicios, operaciones, arquitectura, seguridad, homologación y contribución. El inventario de fuentes upstream se mantiene en [referencias del repositorio](docs/reference/upstream-inventory.md).

Para conocer la arquitectura y las decisiones de seguridad, consultá [ARCHITECTURE.md](ARCHITECTURE.md), [SECURITY.md](SECURITY.md) y los [ADR](docs/adr/).

## Desarrollo y contribuciones

Se requiere el SDK fijado en [`global.json`](global.json). Para compilar la solución desde la raíz del repositorio:

```sh
dotnet restore NetArcaWs.slnx --locked-mode
dotnet build NetArcaWs.slnx --configuration Release --no-restore
dotnet test --solution NetArcaWs.slnx --configuration Release --no-build --no-restore
```

Las pruebas locales usan datos sintéticos y no acreditan homologación con ARCA. Las pruebas fiscales son opt-in y requieren certificados y permisos adecuados. Leé [CONTRIBUTING.md](CONTRIBUTING.md) antes de enviar cambios.

## Licencia

NetArcaWs se distribuye bajo [LGPL-3.0-or-later](LICENSE), con atribución al proyecto upstream [PyAfipWs](https://github.com/reingart/pyafipws) y a su autor, Mariano Reingart. Consultá [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) para los avisos de terceros.
