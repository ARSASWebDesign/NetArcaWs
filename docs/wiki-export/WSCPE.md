<!-- Source: docs/wiki/WSCPE.md. Generated wiki mirror; edit the repository source. -->

# WSCPE: Carta de Porte Electrónica

WSCPE gestiona documentación electrónica para el traslado de granos y derivados
granarios. El cliente `WscpeService` expone las **75 operaciones** del WSDL
publicado por ARCA y fijado el 2026-10-06, incluidas las variantes automotor,
ferroviaria, derivados granarios, emisión en destino y ductos.

## Funcionalidades

| Circuito | Operaciones representativas |
|---|---|
| Emisión | `autorizarCPEAutomotor`, `autorizarCPEFerroviaria`, `autorizarCPEAutomotorDG`, `autorizarCPEFerroviariaDG`, `autorizarCPEDuctosDG`, `autorizarCPEEmisionDestinoDG` |
| Consulta y seguimiento | Consultas por modalidad y destino, pendientes de activación/resolución y número operativo |
| Destino y recepción | Nuevo destino/destinatario, desvío, regreso a origen, arribo, descarga y confirmación definitiva según modalidad |
| Edición y cancelación | Edición de CPE y CPE confirmadas, anulación, aceptación y rechazo según circuito |
| Contingencias | Informar y cerrar contingencias, incluidas variantes de emisión en destino |
| Catálogos | Provincias, localidades, domicilios, plantas, granos, semillas, variedades, embalajes, unidades de medida, derivados granarios y RENSPA |
| Disponibilidad | `dummy`, sin autenticación ni emisión |

La [referencia completa](WSCPE-Referencia-de-operaciones) detalla cada método,
request, response, SOAPAction, cardinalidad y tipo XML. La aplicabilidad de cada
operación depende del circuito y de la habilitación del contribuyente; los DTO
no sustituyen las validaciones de negocio del manual oficial.

## Registro y consulta por tenant

Registrar los clientes mediante `services.AddNetArcaWs()`. Resolver el contexto
confiable de la aplicación, con su CUIT, ambiente y certificado propio; ver
[contexto multitenant](Contexto-multitenant). No construirlo directamente con
valores enviados por un usuario sin verificar su autorización.

```csharp
using NetArcaWs.Contracts.Wscpe;
using NetArcaWs.Services;

var service = provider.GetRequiredService<IWscpeService>();
var response = await service.consultarProvinciasAsync(
    tenant, new ConsultarProvinciasRequest(), cancellationToken);

if (response.Respuesta.Errores.Count != 0)
    throw new InvalidOperationException("ARCA rechazó la consulta de provincias.");

foreach (var province in response.Respuesta.Provincia)
    Console.WriteLine($"{province.Codigo}: {province.Descripcion}");
```

El ejemplo compilable `GuideExamples.ReadCpeProvincesAsync` está en
[GuideExamples.cs](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/examples/NetArcaWs.Examples/GuideExamples.cs).
Los adaptadores de las 75 operaciones están en
[OperationExamples.g.cs](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/examples/NetArcaWs.Examples/OperationExamples.g.cs).
El cliente obtiene un ticket WSAA para `wscpe` e inyecta token, sign y
`cuitRepresentada` en una copia del request. No modifica el DTO del consumidor.
El namespace de los wrappers es `https://serviciosjava.afip.gob.ar/wscpe/`;
los hijos locales `auth`, `solicitud` y `respuesta` no tienen namespace.

## Resultados y escrituras seguras

Un HTTP 200 no confirma autorización. Interpretar `respuesta`, `errores`,
`metadata` y los campos específicos de cada operación antes de avanzar el
estado local. Los faults SOAP se exponen como `SoapFaultException`.

WSCPE no está integrado con `SafeInvoiceService` ni con su diario de facturas.
Sus escrituras no se reintentan automáticamente. La aplicación debe guardar
la intención y sus identificadores antes del envío y conservar la respuesta.
Ante timeout, desconexión o fallo local posterior, consultar el documento
mediante la operación correspondiente antes de decidir otro envío. No reutilizar
la estrategia de numeración o reconciliación de facturas sin adaptarla al
circuito de cartas de porte.

## Health check opcional

```csharp
services.AddHealthChecks()
    .AddWscpeHealthCheck(ArcaEnvironment.Homologation,
        timeout: TimeSpan.FromSeconds(5));
```

El check envía el cuerpo SOAP vacío de `dummy` y exige `OK` en `appserver`,
`authserver` y `dbserver`. No requiere certificado ni ticket y no acredita
autorización para emitir cartas de porte. Ver [health checks](Health-checks).

## Endpoints y documentación oficial

| Entorno | Endpoint |
|---|---|
| Homologación | `https://cpea-ws-qaext.afip.gob.ar/wscpe/services/soap` |
| Producción | `https://cpea-ws.afip.gob.ar/wscpe/services/soap` |

- [Manual WSCPE v2.2.1, revisión 4.7.20 del 22/07/2026](https://www.arca.gob.ar/ws/documentos/manual-wscpe.pdf).
- [WSDL de homologación](https://cpea-ws-qaext.afip.gob.ar/wscpe/services/soap?wsdl) y [WSDL de producción](https://cpea-ws.afip.gob.ar/wscpe/services/soap?wsdl).
- [Fuentes, hashes y diferencias de contrato](Procedencia-de-contratos-SOAP).
- [Homologación autenticada pendiente: issue #12](https://github.com/ARSASWebDesign/NetArcaWs/issues/12).

La cobertura del contrato y las pruebas SOAP locales no equivalen a
homologación fiscal autenticada ni a validar todos los circuitos de negocio.
