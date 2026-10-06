# Migración desde PyAfipWs

Esta guía orienta una migración gradual de integraciones que usan PyAfipWs hacia
NetArcaWs. No es un port completo de PyAfipWs. El código y los ejemplos de
referencia se inspeccionaron en el snapshot `reingart/pyafipws`
[`d595b072110accec9dae1ddb58165ab847b8520a`](https://github.com/reingart/pyafipws/tree/d595b072110accec9dae1ddb58165ab847b8520a),
y la wiki en la página [PyAfipWs](https://github.com/reingart/pyafipws/wiki/PyAfipWs),
consultada desde el clon con commit `f942ebade93c41969d44e652b065ceabec535220`.
La matriz de archivos, páginas y alcance está en [Inventario upstream](../reference/upstream-inventory.md).

## Alcance de esta migración

NetArcaWs implementa seis familias de servicio del alcance del port:
WSFEv1, WSFEXv1, WSMTXCA y Padrón, que agrupa A4, Constancia (endpoint
histórico A5), A10, A13, WSCDC y WSFECred. Los nueve contratos abarcan 108 operaciones SOAP según
los WSDL fijados en [proveniencia de contratos](../reference/contracts/sources.md).
La presencia de una clase o contrato tipado no cubre las utilidades, interfaces,
formatos de entrada/salida, aplicaciones ni servicios adicionales de PyAfipWs.

| PyAfipWs | NetArcaWs | Alcance de la adaptación |
|---|---|---|
| `wsaa.WSAA` y configuración global `HOMO` | `WsaaService`, `WsaaOptions`, `ArcaEnvironment` | Autenticación y TA para los servicios registrados. El entorno y la configuración se expresan con opciones tipadas; no hay objeto COM ni propiedades globales mutables. |
| `wsfev1.WSFEv1` | `Wsfev1Service` / `IWsfev1Service` | Operaciones del contrato WSFEv1 modeladas como métodos async con requests/responses tipados. No preserva todos los alias, helpers ni campos de conveniencia del objeto Python. |
| `wsfexv1.WSFEXv1` | `Wsfexv1Service` / `IWsfexv1Service` | Operaciones WSFEXv1 tipadas; el armado mutable `CrearFactura`/`AgregarItem` se representa con los DTO del contrato. |
| `wsmtx.WSMTXCA` | `Wsmtxcav1Service` / `IWsmtxcav1Service` | Operaciones WSMTXCA con detalle y DTO tipados. No equivale a las herramientas WSFCE ni al ciclo integral de Factura de Crédito MiPyME. |
| `wscdc.WSCDC` | `WscdcService` / `IWscdcService` | Constatación y consultas del contrato WSCDC. No emite ni modifica la factura constatada. |
| `wsfecred.WSFECred` | `WsfecredService` / `IWsfecredService` | Gestión posterior de comprobantes FCE MiPyME y cuentas corrientes. Complementa la autorización del comprobante en WSFEv1/WSMTXCA; no es una capacidad de WSMTXCA ni de `SafeInvoiceService`. |
| `ws_sr_padron.PadronAFIP` (A4) y clase histórica A5 | `PadronService` y clientes `PadronA4Service`, `PadronA5Service`, `PadronA10Service`, `PadronA13Service` | Se implementan los contratos individualmente. La implementación upstream examinada no aporta los contratos A10/A13 actuales; estos se derivan de los WSDL/manuales ARCA versionados. A5 se conserva por la ruta contractual `personaServiceA5`, identificando Constancia con su `service` WSAA vigente. |
| `rece1.py`, `recex1.py`, `recem.py`, archivos TXT/DBF/CSV y aplicaciones GUI | Sin reemplazo directo | La aplicación .NET debe decidir su propio modelo de negocio, almacenamiento e interfaz. El paquete no replica PyRece/PyFactura, conversores de archivos ni su proceso de emisión por lotes. |

La [cobertura de servicios](Servicios-y-cobertura.md) distingue contrato,
transporte, health checks y autorización fiscal. Los checks `Dummy` no
demuestran acceso autenticado ni autorización de comprobantes.

## Configuración e identidad

En PyAfipWs es común construir una interfaz, asignar atributos como `Token`,
`Sign`, `Cuit`, `HOMO` y llamar `Conectar`. En NetArcaWs se registra el cliente
una vez y se pasa un `ArcaTenantContext` en cada operación autenticada. El
contexto identifica la CUIT representada y el entorno; los certificados deben
provenir de configuración protegida o almacenamiento seguro de la aplicación.
No se debe compartir un contexto de tenant entre CUIT distintas.

```csharp
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Cryptography;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using NetArcaWs.Wsaa;

services.AddNetArcaWs(options =>
{
    options.Endpoint = WsaaOptions.HomologationEndpoint;
    options.Certificate = certificateContent;
});

var wsfe = provider.GetRequiredService<IWsfev1Service>();
var tenant = new ArcaTenantContext(
    "tenant-01", cuit, ArcaEnvironment.Homologation, certificateContent);
var request = new FeCompConsultar
{
    FeCompConsReq = new FeCompConsultaReq { CbteTipo = 1, CbteNro = 1, PtoVta = 1 }
};
var response = await wsfe.FECompConsultarAsync(tenant, request, cancellationToken);
```

El fragmento muestra la forma de configuración y llamada, no un comprobante
completo. Los nombres de operaciones deben consultarse en las interfaces del
paquete y sus DTO generados. Para identidades múltiples, revisar [contexto
multitenant](Contexto-multitenant.md) y [WSAA y certificados](WSAA-y-certificados.md).
NetArcaWs obtiene/cacha el ticket asociado a la identidad y servicio; la
aplicación no copia `Token`/`Sign` de una sesión global de PyAfipWs.

## Emisión durable y reintentos

Los métodos SOAP de autorización son llamadas remotas con resultado potencialmente
incierto si se corta la conexión. El flujo de NetArcaWs congela una solicitud y
registra la operación para reconciliarla según el servicio; no vuelve a emitir a
ciegas. Conservá una clave de operación estable y los datos fiscales exactos
hasta resolver su estado. No sustituyas esa conciliación por `UltNro` ni generes
un número nuevo al recibir timeout. Ver [arquitectura y reintentos](Arquitectura-y-reintentos.md)
y el [plan de hitos](../plans/remaining-milestones.md) para las diferencias por
WSFEv1, WSFEXv1 y WSMTXCA.

El [calculador decimal](Calculos-decimales.md) es un helper aritmético explícito,
no un motor que determine automáticamente alícuotas, reglas de negocio, criterios
de redondeo normativos o vigencia fiscal. La aplicación sigue siendo responsable
de entregar datos válidos y de revisar normativa y respuestas ARCA.

## Lo que requiere una solución propia o sigue fuera del port

- Servicios fuera de las cinco familias indicadas, incluidos WSAA como interfaz
  COM, WSCT, WSBFE, WSCDC, WSCPE, WSCTG, WSLPG/WSLSP/WSLTV/WSLUM, COT/IIBB,
  SIRE y trazabilidades sectoriales.
- PyRece/PyFactura, GUI, PDF/QR, correo, códigos de barra, DBF, formatos de lote,
  wrappers ActiveX/COM, DLL, TypeLib, compatibilidad con Visual Basic/FOX/Cobol,
  e instaladores Python/Windows.
- Helpers de conveniencia y retrocompatibilidad de PyAfipWs. Una operación SOAP
  equivalente no prueba paridad de validaciones locales, defaults, alias ni
  comportamiento de errores.
- Homologación real para cada CUIT/certificado, publicación NuGet y puesta en
  producción: son verificaciones externas, no se infieren de compilar o pasar
  pruebas locales. Consultar el estado del proyecto y sus bloqueos vigentes.

Antes de retirar PyAfipWs, inventaría por separado los módulos realmente usados,
los formatos de intercambio y las dependencias externas de la aplicación. Para
cualquier módulo que no figure como adaptado en el inventario, planifica un
reemplazo independiente o conserva esa dependencia explícitamente.

## Atribución y licencia de las fuentes

La documentación upstream identifica a Mariano Reingart como autor/mantenedor y
remite al manual de Sistemas Ágiles. Este texto resume conceptos y señala las
fuentes; no copia ejemplos extensos ni el manual. El repositorio upstream incluye
`COPYING` (GPL), `COPYING.LESSER` (LGPL), `licencia.txt` (que declara LGPLv3 para
el programa) y archivos con avisos individuales; su README también describe
LGPLv3+ y menciona una excepción comercial. Esos avisos no bastan para concluir
una licencia uniforme para cada archivo o para el manual: revisa los avisos del
archivo original y sus condiciones antes de reutilizar código o documentación.
La licencia específica del contenido del manual/wiki no quedó declarada de
forma verificable en las páginas consultadas.

Referencias primarias: [manual de uso PyAfipWs](https://sistemasagiles.com.ar/site/websites/documentacion_herramientas/manualpyafipws.html),
[README upstream fijado](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/README.md),
[COPYING](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/COPYING),
[COPYING.LESSER](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/COPYING.LESSER) y
[`licencia.txt`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/licencia.txt).
