<!-- Source: docs/wiki/Calculos-decimales.md. Generated wiki mirror; edit the repository source. -->

# Cálculos decimales

`NetArcaWs.Taxation.TaxCalculator` contiene utilitarios aritméticos pequeños
para bases que la aplicación ya clasificó y tasas que obtuvo de una fuente
válida. Usa `decimal` y redondeo `AwayFromZero`; no busca alícuotas, no valida
comprobantes contra el manual de un web service y no representa un motor fiscal
completo.

- `Round(value, decimals = 2)` acepta de cero a seis decimales.
- `Vat(taxableBase, ratePercent)` calcula y redondea `base × tasa / 100`.
- `FromGross(gross, ratePercent)` primero redondea el total a dos decimales,
  obtiene el neto dividiendo por `1 + tasa/100`, y define IVA como la diferencia.
- `Total(net, vat, exempt, untaxed, tributes)` suma los componentes, redondeando
  cada uno a dos decimales.

Los importes/tasas negativos producen una excepción. Estas fórmulas simples no
deben sustituir las reglas de líneas, tasas, cotización, precisión, ajustes o
períodos del servicio ARCA que se use. No recalcular silenciosamente importes
enviados; los adaptadores deberán validar las reglas del manual y las pruebas
oficiales aplicables.
