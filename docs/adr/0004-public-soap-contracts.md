# ADR 0004: Contratos SOAP públicos fieles a WSDL

- Estado: aceptado e implementado para Hitos 2–5; build/suite y comprobaciones
  automatizadas de contratos verificadas el 2026-10-06
- Fecha: 2026-10-06
- Alcance: WSFEv1, WSFEXv1, WSMTXCA, Padrón A4, Constancia en ruta A5, A10 y A13

## Contexto

La compatibilidad fiscal depende de campos, namespaces, secuencia de XML, SOAP
actions y primitivas definidos por el contrato desplegado. La superficie del
cliente Python histórico es útil para conocer flujos y ergonomía, pero no incluye
todos los contratos actuales y puede conservar nombres, campos o tipos anteriores.
Un DTO “simplificado” sin correspondencia explícita puede perder operaciones o
alterar el XML de cable.

Los snapshots versionados de producción del 2026-10-06 exponen 81 operaciones:

| Contrato | Operaciones | Fachada generada |
| --- | ---: | --- |
| WSFEv1 | 22 | `Wsfev1Service` |
| WSFEXv1 | 19 | `Wsfexv1Service` |
| WSMTXCA | 27 | `Wsmtxcav1Service` |
| Padrón A4 | 2 | `PadronA4Service` |
| Constancia, ruta histórica A5 | 5 | `PadronA5Service` |
| Padrón A10 | 2 | `PadronA10Service` |
| Padrón A13 | 4 | `PadronA13Service` |
| **Total** | **81** | |

La suma es el inventario de esos contratos seleccionados, no el total del
catálogo de servicios ARCA ni del proyecto PyAfipWs. El endpoint A5 conserva su
ruta `personaServiceA5`, pero la identidad WSAA vigente es
`ws_sr_constancia_inscripcion`.

## Decisión

1. Generar modelos de request/response públicos tipados a partir del XSD/WSDL
   escogido. Preservar nombres, orden, namespace, opcionalidad, cardinalidad,
   enumeraciones y tipo primitivo del contrato. Las fachadas exponen una llamada
   asíncrona por operación SOAP con su request y response correspondientes.
2. Usar el contexto tenant para las operaciones autenticadas. La biblioteca
   obtiene el ticket WSAA y rellena la envoltura de autenticación al serializar;
   la aplicación no debe tomar `Token`, `Sign` ni CUIT de credenciales libres
   del request. Operaciones `Dummy` sin autenticación reciben ambiente explícito.
3. Usar los snapshots de WSDL versionados para reproducibilidad. Antes de fijar
   un release, verificar el WSDL vigente/manual oficial y comparar el snapshot;
   cambios de contrato requieren regenerar código, revisar XML y actualizar
   fixtures/pruebas. El WSDL de homologación WSMTXCA guardado en este repositorio
   está truncado/malformado y no se usa para generar tipos; su DTO proviene del
   WSDL de producción versionado y debe cotejarse con el binding desplegado.
4. No usar WCF. La capa común `ISoapTransport` implementa SOAP 1.1 sobre
   `HttpClient`; el cliente tipado conserva las particularidades XML del servicio.
5. Mantener cálculos y reglas fiscales fuera de los modelos del cable. Los
   primitivos del XSD se preservan aunque exista otro tipo mejor para dominio:
   por ejemplo, WSFE define ciertos importes como `xsd:double`, que se reflejan
   como `double`; helpers `decimal` son opt-in y no reescriben esos campos.
6. WSMTXCA cubre las operaciones de su contrato para facturación con detalle,
   CAE/CAEA y consultas. No se presentará como implementación de todo el ciclo
   de Factura de Crédito Electrónica MiPyME, que corresponde a servicios y reglas
   adicionales.

## Estado y consecuencia

El código genera las 81 llamadas y modelos tipados a partir de los contratos
archivados. La build Release tuvo 0 warnings/errores; la suite completa tuvo 201
casos (198 aprobados, 3 omitidos). Las 81 QName/SOAPAction se compararon con los
WSDL y 166 tipos raíz XML pasaron round-trip. Los 22 tipos de contrato que
contienen campos `DateTime` (`xs:date`/`xs:dateTime`) conservaron sus valores.
En homologación real la corrida opt-in tuvo 9 casos: respondieron los siete
`Dummy` y se omitieron las dos pruebas autenticadas por falta de certificados.
Hitos 2–5 se consideran implementados y
verificados en código/suite; no se afirma homologación fiscal autenticada ni
habilitación por CUIT sin credenciales autorizadas.

Las fachadas son más fieles al contrato y cubren operaciones adicionales que el
upstream no presenta como método destacado. A cambio, exponen nombres y formas
de datos propios del WSDL; los modelos de alto nivel de emisión/reconciliación
se agregarán aparte y no deben ocultar las operaciones de bajo nivel.

## Contratos y referencias

- [`docs/reference/contracts`](../reference/contracts) — snapshots de producción
  y homologación revisados; la copia WSMTXCA homol. es inválida para generación.
- [`docs/reference/healthchecks-contracts.md`](../reference/healthchecks-contracts.md)
  — hosts, service IDs y contratos Dummy.
- [`docs/plans/remaining-milestones.md`](../plans/remaining-milestones.md) —
  inventario por operación y límites de la cobertura verificada.
- [ARCA, web services de factura electrónica](https://arca.gob.ar/fe/ayuda/webservice.asp)
- [ARCA, catálogo de servicios web](https://ftp.afip.gob.ar/ws/documentacion/catalogo.asp)
- [WSFEv1 production WSDL](../reference/contracts/wsfev1-production.wsdl),
  [WSFEXv1](../reference/contracts/wsfexv1-production.wsdl),
  [WSMTXCA](../reference/contracts/wsmtxca-production.wsdl),
  [A4](../reference/contracts/padron-a4-production.wsdl),
  [Constancia/A5](../reference/contracts/padron-a5-production.wsdl),
  [A10](../reference/contracts/padron-a10-production.wsdl) y
  [A13](../reference/contracts/padron-a13-production.wsdl).
