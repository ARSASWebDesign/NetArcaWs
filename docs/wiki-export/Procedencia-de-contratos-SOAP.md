<!-- Source: docs/reference/contracts/sources.md. Generated wiki mirror; edit the repository source. -->

# Proveniencia de contratos SOAP ARCA

Los snapshots se descargaron el 2026-10-06 desde los WSDL públicos indicados. Se guardan para que los DTO y wrappers se puedan regenerar de forma reproducible; no son una afirmación de que el proveedor mantenga inmutable el contrato. SHA-256 identifica exactamente cada entrada. Las URLs son fuentes de lectura, no endpoints de invocación en tiempo de ejecución.

| Contrato | Fuente QA | Fuente producción | SHA-256 QA | SHA-256 producción |
|---|---|---|---|---|
| WSFEv1 | `https://wswhomo.afip.gov.ar/wsfev1/service.asmx?WSDL` | `https://servicios1.afip.gov.ar/wsfev1/service.asmx?WSDL` | `16f5b8450c35a2cfc0187f07d6236162bb57b663954a1e1f0267e71afe90af24` | `10b78b714ea3bf95f5c12ce2fa05805bb21b9fd13f9a98b57fbb866ee9ebd43e` |
| WSFEXv1 | `https://wswhomo.afip.gov.ar/wsfexv1/service.asmx?WSDL` | `https://servicios1.afip.gov.ar/wsfexv1/service.asmx?WSDL` | `ba478212ada4aaa4da6445b7139f800d9d1f3f90e962c850053987f10a40def2` | `531bb29622a61d1cff96776c64989910918f5b5d86fdc9bde2704abbb7043057` |
| WSMTXCA | `https://fwshomo.afip.gov.ar/wsmtxca/services/MTXCAService?wsdl` (el snapshot de QA es inválido) | `https://serviciosjava.afip.gob.ar/wsmtxca/services/MTXCAService?wsdl` | `7145b0ec33a2c6ade46722fa6331f9e73d99cb778f1d4840e4cb54db3097fe22` | `98fbc0649ad134c9d9764690ed6122cacea62a5502c2a150810c2021a49d0aab` |
| Padrón A4 | `https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA4?WSDL` | `https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA4?WSDL` | `aa8ba7a2be5916f71ecd129a5e21b975e64e6395ce5862047ae4068976921dc4` | `d0c5bb822bf99f12e987a8e3bc2f379413b4b8bfb5d20c4f2c6607cbbd5928f2` |
| Padrón A5 / Constancia | `https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5?WSDL` | `https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA5?WSDL` | `5a155cf461b71cd83d81cbf28e5b1b0123f25a17708881487020280489324a00` | `0d3502330f1f63c69d30d2a8eeebfcd00755020fa2b956e2ae128c99bbd198f6` |
| Padrón A10 | `https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA10?WSDL` | `https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA10?WSDL` | `571e63d4252b5e430c5acd4c8d50ebe18d32a53c064a582ffe93fcf5d949f9f2` | `9c74e33f7646064e54e65a978f039882a5b9109450fadcab70900e0b9ad7e4c6` |
| Padrón A13 | `https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA13?WSDL` | `https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA13?WSDL` | `d0abb0ca353d0df0cbd919021d9c408649ece944555c58b2e10f1322cd259399` | `8ddac7cdb1ca34bae87b870e8a68c5c2c2b4446e107638c35dc8dbf04e9706de` |

## Decisiones de generación y diferencias de entorno

### WSCPE: Carta de Porte Electrónica (2026-10-06)

| Contrato | Fuente QA | Fuente producción | SHA-256 QA | SHA-256 producción |
|---|---|---|---|---|
| WSCPE | `https://cpea-ws-qaext.afip.gob.ar/wscpe/services/soap?wsdl` | `https://cpea-ws.afip.gob.ar/wscpe/services/soap?wsdl` | `23dc7d94985537f7cc8a0cec2a95c57118262ffeb335c4db424429da6b6c7759` | `9d85d6b60b7a49e18ea7610e9845fa69a0c7c822e72bab698ff7b820d087bce3` |

Ambos snapshots exponen 75 operaciones, 341 tipos complejos y 150 elementos
globales, sin imports XSD ni alternativas `xs:choice`. Los DTO y el cliente
se generan del contrato de producción. El namespace contractual es
`https://serviciosjava.afip.gob.ar/wscpe/`; los elementos locales están sin
namespace. La autenticación usa `auth/token`, `auth/sign` y
`auth/cuitRepresentada`, aunque algunos ejemplos del manual muestran `cuit`.
El binding WSDL determina los nombres que se envían. `dummy` no tiene partes
de entrada: se envía el cuerpo SOAP vacío y se recibe `DummyResp/respuesta`.

Se consultó el [manual oficial WSCPE v2.2.1, revisión 4.7.20 del 22/07/2026](https://www.arca.gob.ar/ws/documentos/manual-wscpe.pdf),
SHA-256 `3b1e4da2850c38cf9c7cae97636d91a7cb4ddb97260b6be73cf9f3188674af0e`.
Los hosts `.afip.gob.ar` de la tabla entregaron XML con TLS válido; los alias
`.arca.gob.ar` consultados entregaron HTML y no se adoptaron como contrato.
No se realizaron operaciones autenticadas de negocio con estos snapshots.

### WSCDC y WSFECred (2026-10-06)

| Contrato | Fuente QA | Fuente producción | SHA-256 QA | SHA-256 producción |
|---|---|---|---|---|
| WSCDC | `https://wswhomo.afip.gov.ar/WSCDC/service.asmx?WSDL` | `https://servicios1.afip.gov.ar/WSCDC/service.asmx?WSDL` | `7f3e5d23cea4f4a91777f542187010a0796223b38490d975087a8695305d2bbc` | `1d892ef1991e3408c1daafebc45d7034e619146814b253c5a0b51dd6291e568c` |
| WSFECred | `https://fwshomo.afip.gov.ar/wsfecred/FECredService?wsdl` | `https://serviciosjava.afip.gob.ar/wsfecred/FECredService?wsdl` | `3b870b489ee2b6e2797c0c1a9d5a9d7c1805df198d36c85ab78f058420de14a3` | `9389219ec9661d6093a6499aaca45c1fad5963ad93fd5eedc229dfc29b351fc7` |

Los DTO y clientes se generan desde los contratos de producción completos:
6 operaciones WSCDC y 21 WSFECred. Los snapshots QA se conservan para comparar
contratos, sin sustituir pruebas autenticadas de negocio.
El [manual WSCDC v4](https://www.afip.gob.ar/ws/WSCDCV1/WSCDC-manual-desarrollador-v4.pdf)
publica `servicios1.arca.gob.ar` y `wshomo.afip.gob.ar`. Durante la verificación,
el primero falló por nombre de certificado TLS y el segundo no resolvió por DNS.
Se utilizan los hosts AFIP de la tabla, disponibles con validación TLS completa;
el WSDL de producción también anuncia ese endpoint. No se deshabilita TLS.
WSFECred conserva elementos internos sin namespace, incluido `dummyReturn`;
su operación `dummy` envía un cuerpo SOAP vacío según el mensaje de entrada WSDL.

`wsmtxca-homologation-invalid.wsdl` se conserva sólo como evidencia: el contenido recibido no es XML bien formado/reutilizable como esquema de operaciones. Los DTO WSMTXCA se generan desde el snapshot de producción válido; las URLs del cliente siguen separando QA y producción. No se afirma equivalencia entre el WSDL inválido de QA y el de producción.

Para los otros servicios, las operaciones y los tipos embebidos de los WSDL QA/producción capturados coincidieron en la comparación de los snapshots; los clientes seleccionan endpoints por ambiente. Padrón A5 migró el host de servicio a `arca.gob.ar`: el snapshot del WSDL de producción todavía anuncia `aws.afip.gov.ar`. El endpoint runtime usa el alias `.arca.gob.ar` de producción y `.afip.gov.ar` de QA, que se verificaron disponibles el 2026-10-06; las URL de contrato anteriores conservan la evidencia descargada. A10/A13 usan HTTPS en runtime según sus manuales aunque el WSDL capturado anuncia esquema HTTP.

Regeneración: instalar la herramienta .NET `dotnet-xscgen` versión `3.0.1240` y ejecutar `dotnet run --project tools/NetArcaWs.Build -- contracts --xscgen /ruta/a/xscgen`. El generador conserva namespaces QName al extraer el XSD inline y emite los DTO/wrappers. Revisar y actualizar hashes/snapshots antes de cambiar una versión de contrato.

## Manuales consultados el 2026-10-06

Se contrastaron los contratos con el [índice oficial de facturación](https://arca.gob.ar/fe/ayuda/webservice.asp)
y se descargaron sus tres manuales. El número publicado en el índice no siempre
coincide con la portada del PDF recibido; se conserva esa diferencia explícita.

| Servicio | Documento recibido | SHA-256 del PDF |
|---|---|---|
| WSFEv1 | [FE v4.7, revisión 2026-09-01](https://arca.gob.ar/ws/documentacion/manuales/manual-desarrollador-ARCA-COMPG.pdf), 203 páginas | `11dd8e4c5dc409d9e05a88a0043cfe43ee1577e887dbc94ed765ee2c053a6aed` |
| WSFEXv1 | [URL rotulada V3.1.1](https://arca.gob.ar/ws/documentacion/manuales/WSFEX-Manualparaeldesarrollador_V3.1.1_ARCA.pdf), pero portada **3.1.0 de 2025-08-18**, 59 páginas | `5c526eea2f298f5c2235c6ff2c481ff74dcd694ad74eee035877dd3a60b103d4` |
| WSMTXCA | [Manual 0.25.8](https://arca.gob.ar/fe/ayuda/documentos/wsmtxca-RG-2904.pdf), 380 páginas | `8dbb73ea4c8d201a73c62e19759bb440801c168f04365261d9b207cf1a1eaaf4` |

Las referencias de [servicios implementados](Servicios-implementados)
incluyen los manuales de Padrón y WSAA. Las tablas por operación se generan desde
los WSDL; las reglas de negocio, restricciones de acceso y validaciones por
código requieren consultar el manual y homologar con credenciales propias.
