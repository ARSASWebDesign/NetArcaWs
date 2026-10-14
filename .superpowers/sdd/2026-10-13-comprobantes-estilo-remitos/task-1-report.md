# Task 1: modelos de remitos

## Resultado

Se añadieron `DatosRemito`, `RemitoGenerado`, `Remito`, `RemitoValidator` y sus tipos auxiliares en `NetArcaWs.Documents`. La numeración siempre la entrega la aplicación. R produce código 91, exige CAI, rango que contiene el número, vencimiento compatible con la fecha de emisión y datos de imprenta; el perfil autoimpresor R debe declararse y solo omite imprenta/rango preimpreso, conservando CAI y vencimiento. X requiere identificar el predio/polo/parque industrial y rechaza campos CAI, imprenta y rango de R. Ambos snapshots se representan como estado `Local`, sin resultado de autorización.

Las colecciones se copian al entrar y quedan de solo lectura. `RemitoValidator.Validate` vuelve a validar snapshots deserializados. El modelo requiere receptor con CUIT, domicilio y condición tributaria completos; no representa excepciones de consumidor final ni regímenes sectoriales como DTV-e. El CAI, habilitación, pertenencia al régimen de autoimpresor y ámbito X son declaraciones de la aplicación: la biblioteca no consulta ni acredita su validez en ARCA. No implementa emisión electrónica, CAE, QR fiscal ni controlador 901.

## TDD

**RED:** antes de implementar, se ejecutó:

```text
dotnet test tests/NetArcaWs.UnitTests/NetArcaWs.UnitTests.csproj --no-restore --filter FullyQualifiedName~RemitoTests
```

Falló la compilación con `CS0246`: `DatosRemito` no se encontró en `RemitoTests.cs(16,20)`. La falla corresponde a la API aún ausente.

**GREEN:** tras implementar la API, la misma orden produjo:

```text
Resumen de la serie de pruebas: Correcta!
  total: 6
  error: 0
  correcto: 6
  omitido: 0
```

`git diff --check` terminó sin errores. No se ejecutó la suite completa ni pruebas ARCA; el alcance de esta tarea pidió pruebas focales.

## Base normativa

Contraste en la versión actualizada oficial de RG 1415/2003: artículos 27–30 y Anexo V, Apartados I–V. El Anexo exige leyenda, numeración consecutiva, CAI y vencimiento para R; el punto 8 de V exime al autoimpresor R solo de los datos de imprenta y rango impreso. X aplica al traslado dentro del mismo predio, polo o parque industrial. El código 91 corresponde a Remito R en RG 100. La validación temporal compara la emisión histórica con el vencimiento y nunca con la fecha actual.
