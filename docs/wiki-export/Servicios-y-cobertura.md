<!-- Source: docs/wiki/Servicios-y-cobertura.md. Generated wiki mirror; edit the repository source. -->

# Servicios fiscales y cobertura

Esta página separa contratos de infraestructura existentes de operaciones
fiscales. Un health check `Dummy` no implementa autorización de facturas; un
manual enlazado no significa que NetArcaWs ya tenga ese cliente.

El alcance de Hitos 2–5 es la superficie completa de las operaciones vigentes
en los contratos ARCA para WSFEv1, WSFEXv1, WSMTXCA, Padrón A4, Constancia
(endpoint A5 histórico), A10 y A13. El inventario de métodos de PyAfipWs ayuda a
entender modelos, agregadores y compatibilidad, pero no recorta ni reemplaza los
contratos oficiales. Las operaciones enumeradas debajo vienen de los WSDL
descargados el 2026-10-06; volver a verificar manual/versiones y bindings antes
de usar para un release.

En QA real, el 2026-10-06, una corrida de 9 casos respondió los siete probes
`Dummy` de estos servicios y omitió las dos pruebas autenticadas por falta de
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
| Health checks | Implementados para endpoints SOAP declarados | Comprueban disponibilidad del servicio, no autorización fiscal |
| CLI certificados | Implementada para generar CSR e inspeccionar localmente | No emite certificado ni conecta con ARCA |

La suma de los siete contratos es 81 operaciones. Se verificaron QName/action
de cada método contra WSDL y 166 tipos raíz XML en round-trip en la suite; 22
tipos de contrato conservaron sus campos `DateTime`. Una
corrida real de QA respondió los siete probes `Dummy`; las dos pruebas
autenticadas se omitieron por falta de certificados. Esto no acredita
autorización fiscal. Hitos 2–6 se verificaron con build/suite local. La documentación incluye un
inventario upstream y referencias de todas las operaciones; su alcance no
amplía automáticamente la cobertura funcional. El
[plan de hitos 2–7](Plan-de-servicios-y-documentaci%C3%B3n) define las operaciones,
datos fiscales, cobertura de contratos y pruebas de cierre por servicio.

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

El [inventario del proyecto original](Inventario-del-proyecto-original)
registra cada módulo y entrada documental investigada, con revisiones fijadas,
correspondencia .NET y ausencia explícita cuando no está portado. Incluye PDF,
formatos de intercambio, COM, aplicaciones y otros servicios que no forman parte
del alcance implementado. La [guía de migración](Migracion-desde-PyAfipWs)
explica las diferencias de uso y datos.

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
