# Cálculos decimales

`NetArcaWs.Taxation.TaxCalculator` ofrece utilitarios aritméticos para bases
que la aplicación ya clasificó y tasas obtenidas de una fuente válida. Usa
`decimal` y redondeo `AwayFromZero`. No es un motor fiscal: no busca alícuotas,
no distribuye descuentos, no valida comprobantes contra ARCA y no completa ni
corrige automáticamente los DTO de emisión. Los servicios reciben importes,
ítems y cantidades preparados por la aplicación.

- `Round(value, decimals = 2)` acepta de cero a seis decimales.
- `Vat(taxableBase, ratePercent)` calcula y redondea `base × tasa / 100`.
- `FromGross(gross, ratePercent)` redondea el total a dos decimales, obtiene el
  neto dividiendo por `1 + tasa/100` y calcula IVA como la diferencia.
- `Total(net, vat, exempt, untaxed, tributes)` suma los componentes, redondeando
  cada uno a dos decimales.

Como ejemplo aritmético de WSFEv1, una base gravada de 100 con IVA de 21 da
total 121 para A/B. En C, el ejemplo de total 121 se informa como subtotal/neto
121 e IVA 0. Son valores ilustrativos; el consumidor debe construir el
comprobante de acuerdo con el tipo, el servicio y sus reglas de validación.

## Qué informa cada servicio

| Servicio | Alcance publicado por ARCA | Datos de importes que debe preparar la aplicación |
| --- | --- | --- |
| WSFEv1 | Comprobantes sin detalle de ítems; consultar el catálogo y los códigos vigentes para la clase habilitada | Total, netos, IVA por alícuota, conceptos no gravados/exentos y tributos, según aplique. El DTO no contiene cantidades ni precios unitarios. |
| WSMTXCA | Comprobantes A y B con detalle de ítems | Cantidad, precio, bonificación, código de condición IVA, IVA e importe por ítem, además de subtotales y totales. El DTO no calcula esos valores. |
| WSFEXv1 | Comprobantes E de exportación con ítems | Cantidad, unidad, precio unitario, bonificación y total por ítem. ARCA valida el total del ítem contra precio × cantidad − bonificación, con la tolerancia indicada en su manual. |

La composición de importes depende del tipo. Para comprobantes ordinarios A y
B, la validación WSFEv1 del código 10048 contrasta el total con la suma de
conceptos no gravados, neto gravado, exento, tributos e IVA. Para C, el total
corresponde a neto más tributos, con los campos de IVA, conceptos no gravados y
exentos en cero. El manual contempla excepciones para otros códigos, como el
49 de bienes usados bajo ciertas condiciones; no extrapolar la fórmula de A/B
a todos los comprobantes. Las notas y otros códigos tienen reglas propias: no
se debe inferir su signo ni reutilizar una fórmula sin consultar las
validaciones aplicables.

## Precisión y agrupación

Redondear cada línea antes de agrupar puede producir un resultado distinto de
agrupar las bases y redondear el IVA una sola vez. Por ejemplo, para tres líneas
con base 0,03 y tasa 21 %:

| Método aritmético | Cálculo | Resultado |
| --- | --- | ---: |
| Base agrupada | `Round((3 × 0,03) × 21 / 100, 2)` | 0,02 |
| Cada línea redondeada | `3 × Round(0,03 × 21 / 100, 2)` | 0,03 |

El helper `Vat` redondea cada llamada; no decide qué bases deben agruparse. La
precisión y agrupación correctas dependen del servicio y del comprobante. No
debe elegirse un método por conveniencia ni tomarse la tolerancia de aceptación
de ARCA como una regla de cálculo.

Los importes o tasas negativos producen una excepción en estos helpers. Esto no
define cómo deben representarse notas de crédito o débito; aplíquense las reglas
vigentes para el código de comprobante y servicio correspondiente.

## Fuentes oficiales

- [ARCA: manuales de factura electrónica y alcance de cada servicio](https://arca.gob.ar/ws/documentacion/ws-factura-electronica.asp).
- [ARCA: homologación externa y adecuaciones publicadas para WSFEv1 y WSMTXCA](https://arca.gob.ar/fe/ayuda/homologacion_externa.asp).
- [ARCA: manual para desarrolladores de comprobantes WSFEv1](https://www.afip.gob.ar/ws/documentacion/manuales/manual-desarrollador-ARCA-COMPG.pdf).
- [ARCA: manual para desarrolladores de WSFEXv1, versión 3.1.1](https://www.arca.gob.ar/ws/documentacion/manuales/WSFEX-Manualparaeldesarrollador_V3.1.1_ARCA.pdf).

Las adecuaciones publicadas para homologación pueden tener una fecha futura de
aplicación. Antes de cambiar modelos o validaciones, revisar el manual vigente
en la fecha de uso y sus ejemplos para el servicio y tipo de comprobante.
