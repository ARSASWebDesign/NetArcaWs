<!-- Source: docs/wiki/Servicios-y-cobertura.md. Generated wiki mirror; edit the repository source. -->

# Servicios fiscales y cobertura

Esta página separa contratos de infraestructura existentes de operaciones
fiscales. Un health check `Dummy` no implementa autorización de facturas; un
manual enlazado no significa que NetArcaWs ya tenga ese cliente.

Las superficies SOAP incluidas hoy son los contratos ARCA de WSFEv1, WSFEXv1,
WSMTXCA, Padrón A4, Constancia (endpoint A5 histórico), A10, A13, WSCDC y
WSFECred y WSCPE. El inventario de métodos de PyAfipWs ayuda a
entender modelos, agregadores y compatibilidad, pero no recorta ni reemplaza los
contratos oficiales. Las operaciones enumeradas debajo vienen de los WSDL
descargados el 2026-10-06; volver a verificar manual/versiones y bindings antes
de usar para un release.

En QA real, el 2026-10-06, una corrida previa a WSCPE respondió los nueve probes `Dummy` y
omitió dos pruebas autenticadas por falta de
certificados. La ejecución no acredita autorización fiscal.

| Servicio | Estado funcional al corte documental | Nota de contrato/alcance |
|---|---|---|
| WSAA | Implementado: autenticación, TA, firma, caché en proceso | Auth no autoriza automáticamente otros servicios ni usuarios |
| Transporte SOAP común | Implementado: `ISoapTransport`/`SoapTransport` | SOAP 1.1, límites/validación de XML y faults; sin autenticación fiscal ni reintentos |
| Cálculo decimal | Implementado como utilitario explícito | No es un motor tributario completo ni decide tasas/reglas ARCA |
| Diario/orquestación fiscal | Suite Release verificada; emisión unitaria y reconciliación | Autoriza CAE unitario WSFE/WSFEX/WSMTXCA; no auto-reenvío ni worker |
| WSFEv1 | Hito 2 verificado: 22 métodos; QName/action contra WSDL y suite | CAE/CAEA, consultas, parámetros; conserva tipos XSD (incluye `double`) |
| WSFEXv1 | Hito 3 verificado: 19 métodos; QName/action contra WSDL y suite | Exportación; `Cmp.Id` y reproceso son específicos del flujo |
| WSMTXCA | Hito 4 verificado: 27 métodos; QName/action contra WSDL y suite | Factura de mercado interno con detalle y CAE/CAEA; no es el ciclo integral WSFCE/MiPyME |
| Padrón A4 | Hito 5 verificado: 2 métodos | `dummy`/`getPersona`; `Dummy` no requiere ticket |
| Constancia, endpoint histórico A5 | Hito 5 verificado: 5 métodos | Ruta `personaServiceA5`; service WSAA `ws_sr_constancia_inscripcion` |
| Padrón A10 | Hito 5 verificado: 2 métodos | Contrato conforme a manual/WSDL actual; distinto de `ws_sr_padron.py` upstream |
| Padrón A13 | Hito 5 verificado: 4 métodos | Incluye `getIdPersonaListByDocumento`; distinto de `ws_sr_padron.py` upstream |
| WSCDC | 6 métodos del WSDL fijado | Constatación, tablas de referencia y `ComprobanteDummy`; la constatación no autoriza ni emite comprobantes |
| WSFECred | 21 métodos del WSDL fijado | Gestión posterior de FCE MiPyME y cuentas corrientes; separado de autorización WSFE/WSMTXCA y sin orquestación `SafeInvoiceService` |
| WSCPE | 75 métodos del WSDL fijado | Cartas de porte, contingencias, destinos, estados y catálogos; sin diario automático ni homologación autenticada |
| Health checks | Implementados para endpoints SOAP declarados | Comprueban disponibilidad del servicio, no autorización fiscal |
| CLI certificados | Implementada para generar CSR e inspeccionar localmente | No emite certificado ni conecta con ARCA |

La suma de los diez contratos es 183 operaciones. La suite cotejó las
operaciones contra WSDL y verificó 369 tipos raíz XML en round-trip; 56 tipos
con campos `DateTime` conservaron sus valores. Los snapshots QA/producción de
WSCDC, WSFECred y WSCPE coinciden en sus schemas.
La corrida real de QA anterior a WSCPE respondió los nueve probes `Dummy`; las dos pruebas
autenticadas se omitieron por falta de certificados. Esto no acredita
autorización fiscal. Hitos 2–6 se verificaron con build/suite local. La
documentación de operaciones describe contratos, no amplía automáticamente la
cobertura funcional ni afirma paridad completa con PyAfipWs.

## Mapa del upstream investigado

PyAfipWs ofrece más que los módulos incluidos en el port. La revisión del
2026-10-06 fija las revisiones Git del repositorio y wiki originales en el
inventario enlazado debajo.

### Referencias organizadas por servicio

| Servicio | Operaciones y ejemplos | API del proyecto original |
|---|---|---|
| WSFEv1 | [Referencia completa](WSFEv1-Referencia-de-operaciones) | `wsfev1.py` |
| WSFEXv1 | [Referencia completa](WSFEXv1-Referencia-de-operaciones) | `wsfexv1.py` |
| WSMTXCA | [Referencia completa](WSMTXCA-Referencia-de-operaciones) | `wsmtx.py` |
| Padrón A4 | [Referencia completa](Padr%C3%B3n-A4-Referencia-de-operaciones) | `ws_sr_padron.py` |
| Constancia / A5 | [Referencia completa](Padr%C3%B3n-A5-Referencia-de-operaciones) | `ws_sr_padron.py` |
| Padrón A10 | [Referencia completa](Padr%C3%B3n-A10-Referencia-de-operaciones) | Sin equivalente en ese módulo upstream |
| Padrón A13 | [Referencia completa](Padr%C3%B3n-A13-Referencia-de-operaciones) | Sin equivalente en ese módulo upstream |
| WSCDC | [Referencia completa](WSCDC-Referencia-de-operaciones), [guía](WSCDC) | `wscdc.py` |
| WSFECred | [Referencia completa](WSFECred-Referencia-de-operaciones), [guía](WSFECred) | `wsfecred.py` |
| WSCPE | [Referencia completa](WSCPE-Referencia-de-operaciones), [guía](WSCPE) | `wscpe.py`; la cobertura .NET sigue las 75 operaciones actuales del WSDL |

El [inventario del proyecto original](Inventario-del-proyecto-original)
registra fuentes fijadas y límites de adaptación. El seguimiento priorizado de
servicios faltantes, con autoridad y vigencia por comprobar, está en el
[issue #10](https://github.com/ARSASWebDesign/NetArcaWs/issues/10). La
[guía de migración](Migracion-desde-PyAfipWs) explica las diferencias de uso
y datos.

## Fuentes

- [README de PyAfipWs](https://github.com/reingart/pyafipws/blob/main/README.md)
- [Wiki de PyAfipWs](https://github.com/reingart/pyafipws/wiki)
- [`wsfev1.py`](https://github.com/reingart/pyafipws/blob/main/wsfev1.py),
  [`wsfexv1.py`](https://github.com/reingart/pyafipws/blob/main/wsfexv1.py),
  [`wsmtx.py`](https://github.com/reingart/pyafipws/blob/main/wsmtx.py),
  [`ws_sr_padron.py`](https://github.com/reingart/pyafipws/blob/main/ws_sr_padron.py)
- [Manuales de servicios de factura electrónica de ARCA](https://arca.gob.ar/fe/ayuda/webservice.asp)
- [Contratos de health checks y endpoints del repositorio](Contratos-de-health-checks)
- Snapshots XML por protocolo en [`docs/reference/contracts`](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/docs/reference/contracts).
  El WSDL de homologación MTXCA guardado allí es un documento truncado y no se
  usa para generar tipos; la copia de producción es la fuente completa para
  reconstruir el binding de las 27 operaciones.
