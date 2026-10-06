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

`wsmtxca-homologation-invalid.wsdl` se conserva sólo como evidencia: el contenido recibido no es XML bien formado/reutilizable como esquema de operaciones. Los DTO WSMTXCA se generan desde el snapshot de producción válido; las URLs del cliente siguen separando QA y producción. No se afirma equivalencia entre el WSDL inválido de QA y el de producción.

Para los otros servicios, las operaciones y los tipos embebidos de los WSDL QA/producción capturados coincidieron en la comparación de los snapshots; los clientes seleccionan endpoints por ambiente. Padrón A5 migró el host de servicio a `arca.gob.ar`: el snapshot del WSDL de producción todavía anuncia `aws.afip.gov.ar`. El endpoint runtime usa el alias `.arca.gob.ar` de producción y `.afip.gov.ar` de QA, que se verificaron disponibles el 2026-10-06; las URL de contrato anteriores conservan la evidencia descargada. A10/A13 usan HTTPS en runtime según sus manuales aunque el WSDL capturado anuncia esquema HTTP.

Regeneración: `scripts/generate-contracts.sh`. El script instala `dotnet-xscgen` versión `3.0.1240` en un directorio temporal, conserva namespaces QName al extraer el XSD inline y emite los DTO/wrappers. Revisar y actualizar hashes/snapshots antes de cambiar una versión de contrato.
