<!-- Source: docs/wiki/Revision-de-issues-y-PR-de-PyAfipWs.md. Generated wiki mirror; edit the repository source. -->

# Revisión de issues y PR de PyAfipWs

Corte: **2026-10-06**. Se inventariaron **33 issues y 21 pull requests abiertos**
de [PyAfipWs](https://github.com/reingart/pyafipws). Se contrastaron sus cuerpos,
comentarios de conversación, comentarios de revisión disponibles y los cambios
pertinentes de los PR con NetArcaWs
[`17a49c5`](https://github.com/ARSASWebDesign/NetArcaWs/tree/17a49c5ba48caee8f79bf1e9c276399309f4d85c).

El HEAD de `main` upstream es
[`d595b072`](https://github.com/reingart/pyafipws/commit/d595b072110accec9dae1ddb58165ab847b8520a),
del **2025-05-21**; GitHub informa el último push del repositorio el
**2025-07-20**. Son fechas diferentes. Hay aportes abiertos de 2026: la falta de
integración reciente no significa falta de actividad de colaboradores.

Esta es una evaluación de aplicabilidad, no una aprobación de los PR ni una
auditoría de cada línea de sus ramas. Los PR apuntan a `main`, `develop`, `py3k`
o `master`, tienen antigüedades distintas y algunos incluyen cambios ajenos a su
título. Se paginó el listado de archivos; el PR #146, por ejemplo, contiene
**561 archivos**. No se ejecutó código upstream, no se copiaron certificados,
tickets, binarios ni datos fiscales de sus ejemplos. Los reportes comunitarios
no sustituyen la documentación vigente de ARCA ni demuestran un defecto de .NET.

El [registro de entradas revisadas](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/docs/reference/upstream-open-items-2026-10-06.json)
conserva IDs, URLs, estados, fechas y SHA de cabecera de cada PR. El
[inventario documental original](Inventario-del-proyecto-original) cubre el
resto del contexto del proyecto Python.

## Resultado y prioridades

La mayor mejora pendiente es **coordinar WSAA cuando se comparte una identidad
criptográfica**, junto con reforzar las pruebas fiscales con datos poblados.
No apareció evidencia suficiente para afirmar un defecto fiscal nuevo y
reproducido en NetArcaWs. Sí hay limitaciones confirmadas, funciones ausentes y
escenarios que las pruebas actuales no demuestran.

| Prioridad | Trabajo recomendado | Criterio de aceptación |
|---|---|---|
| **P1** | Proveedor opcional de TA persistido y coordinación WSAA entre instancias | Dos procesos con la misma credencial/ambiente/servicio no solicitan tickets redundantes; expiración, rotación, caída del coordinador y `coe.alreadyAuthenticated` tienen pruebas. El acceso entre tenants sigue autorizado y aislado. |
| **P1** | Regresiones SOAP con datos fiscales completos | Verificar XML de `CanMisMonExt`, `CondicionIVAReceptorId`, fechas de servicio, múltiples detalles CAE y reporte CAEA; deserializar `estadoImpuesto`, `motivo`, impuestos múltiples/nulos y errores de Padrón. |
| **P1** | Reforzar recuperación ante fallo local después de autorización remota | Simular que ARCA autoriza y falla la persistencia o salida local; recuperar por consulta sin volver a emitir. Conservar evidencia y reconciliar, sin interpretar un error local como rechazo fiscal. |
| **P2** | Ejemplos y validación de dominio opcional | Escenarios documentados de comprobante C, exento/no gravado y moneda extranjera, basados en manual vigente. No alterar automáticamente importes ni cambiar los DTO del WSDL. |
| **P2** | Matriz CI de Windows, Linux y macOS; pruebas de cultura | Validar PKCS#8/PFX donde se soporta, CLI, Unicode, fechas y XML decimal con culturas `es-AR` y `fr-FR`. La CI actual principal corre en Ubuntu. |
| **P2** | Mejorar consulta de impuestos y diagnóstico de credenciales | Ejemplo/helper tipado de A5 por ID de impuesto; avisos de vencimiento/rotación del certificado sin publicar secretos. Diferenciar estos avisos de los probes `Dummy`. |
| **P2, hito separado** | Ciclo completo de Factura de Crédito MiPyME | Inventariar y fijar el WSDL/manual vigente de WSFECred; implementar operaciones, estados, permisos y homologación propios. WSMTXCA no sustituye ese circuito. |
| **P3, según demanda** | QR/PDF, WSLPG, WSCPE, ARBA, ANMAT o formatos legacy | Especificar cada módulo antes de ampliarlo. Reusar ideas y casos de prueba de los reportes, no sus contratos antiguos o binarios. |

P1 significa primero en el próximo trabajo de robustez, no vulnerabilidad crítica
confirmada. P2 mejora de uso/cobertura o capacidad adicional. P3 ampliación cuya
prioridad depende del uso del producto. Esta revisión **no implementa** esos
trabajos ni cambia el comportamiento fiscal de la versión publicada.

## Hallazgos que requieren atención

### WSAA: aislamiento local no equivale a sesiones remotas independientes

[#152](https://github.com/reingart/pyafipws/issues/152) y
[#54](https://github.com/reingart/pyafipws/issues/54) son los casos más relevantes
para un SaaS. NetArcaWs acepta certificados por tenant y contenido desde vault/BD,
reutiliza tickets válidos y coordina misses concurrentes **de la misma clave de
caché**. Eso está implementado y probado con respuestas simuladas.

Sin embargo, la clave actual incluye tenant y CUIT representada. Dos contextos
pueden usar el mismo certificado/servicio y obtener claves distintas, mientras
WSAA sigue viendo la misma identidad criptográfica. Puede aparecer
`coe.alreadyAuthenticated` incluso dentro de un proceso, además del caso de
réplicas con cachés independientes. El
[ADR de multitenancy](Decisi%C3%B3n-2-Contexto-multitenant) ya documenta ese límite.
No lo clasificamos como «resuelto» por el mero hecho de usar `IMemoryCache`.

La mejora debe separar la **autorización de acceso del tenant** de la coordinación
de la **credencial WSAA**. Un store distribuido necesita expiración validada,
protección de Token/Sign, exclusión mutua con leases y tratamiento de fallos;
agregar solamente `IDistributedCache` no resuelve las carreras. Tampoco corresponde
compartir tickets entre tenants automáticamente sin una política confiable del
host. Un `alreadyAuthenticated` no devuelve por sí mismo el TA perdido: no
inventar credenciales ni entrar en un bucle de login. Evitar una renovación
anticipada ciega que repita el mismo problema. Validar el diseño con
[WSAA oficial](https://www.afip.gob.ar/ws/documentacion/wsaa.asp) y homologación.

### Contrato disponible versus escenario fiscal probado

[#124](https://github.com/reingart/pyafipws/issues/124),
[PR #127](https://github.com/reingart/pyafipws/pull/127) y
[PR #128](https://github.com/reingart/pyafipws/pull/128) tratan condición IVA y
cancelación en la misma moneda. Nuestros tipos ya contienen
`CondicionIVAReceptorId` y `CanMisMonExt`. .NET transmite un request tipado;
no tiene el helper Python `CAESolicitarX` que olvidaba copiar propiedades.
Las 81 operaciones verificadas no implican que se hayan probado todas las
combinaciones fiscales: faltan asserts específicos con estos valores poblados,
listas de varios comprobantes y CAEA. `SafeInvoiceService` sigue limitado a
emisión CAE **unitaria**; una llamada SOAP de lote no hereda esa orquestación.

[#136](https://github.com/reingart/pyafipws/issues/136) reporta un tipo no encontrado
de Padrón. El WSDL actual A5 y los DTO .NET ya declaran `estadoImpuesto` como
string, además de `motivo` y `periodo`. Es una diferencia respecto del contrato
viejo reportado, no evidencia de un fallo actual. Hace falta una respuesta fixture
representativa para demostrar el parseo de esas secciones.

El [PR #146](https://github.com/reingart/pyafipws/pull/146) sí agrega
`ObtenerCampoImpuesto`: conserva impuestos detallados y busca un campo por ID.
En .NET esos datos ya están disponibles tipados en la colección `Impuesto`.
Podemos agregar un ejemplo o extensión tipada, evitando una API de nombres de
propiedades en strings. El diff de 561 archivos impide tratar ese PR entero como
un cambio pequeño de Padrón.

### Un fallo local puede ocurrir después de obtener CAE

[#134](https://github.com/reingart/pyafipws/issues/134) muestra una respuesta de
autorización seguida por errores Python al escribir XML/JSON. El error de
`bytes`/`str` no se traslada literalmente a .NET, pero el caso operativo sí:
fallar al guardar o mostrar un resultado no prueba que ARCA haya rechazado la
factura. Nuestro diario conserva estados inciertos y `ResumeAsync` reconcilia;
conviene añadir fault injection específico al completar la persistencia tras
una autorización remota. No agregar un retry automático de emisión.

### No trasladar arreglos de plataforma ni parámetros antiguos sin validación

- [#131](https://github.com/reingart/pyafipws/issues/131), PKCS#8, está cubierto
  por importación nativa y pruebas de PEM plano/cifrado. No necesita OpenSSL ni
  BouncyCastle.
- [#61](https://github.com/reingart/pyafipws/issues/61) pide renovación automática,
  no solo generar una CSR. El CLI actual cubre clave/CSR e inspección; la emisión,
  relación y renovación del certificado siguen el trámite ARCA. No prometer
  auto-renovación por tener `cert-dev`/`cert-prod`.
- [PR #153](https://github.com/reingart/pyafipws/pull/153) intenta normalizar
  números recibidos por Python/COM. Los DTO .NET usan `long`; no hay que copiar
  la sustitución silenciosa de valores ausentes por cero.
- [PR #88](https://github.com/reingart/pyafipws/pull/88) mezcla encoding/setup,
  funciones fiscales, un wheel binario, cachés/TA y eliminación de tests. Varias
  ideas fiscales ya están en nuestros contratos: cotizaciones múltiples FEX,
  `FechaPago`, fechas de comprobantes asociados, adicionales MTXCA y
  `CbteFchHsGen` CAEA. Revisar cada caso; no integrar el PR en bloque.
- [PR #46](https://github.com/reingart/pyafipws/pull/46) tiene además una observación
  de revisión por `X511` en lugar de `X509`. Que una corrección esté propuesta
  no demuestra que esté lista para incorporar.
- [PR #108](https://github.com/reingart/pyafipws/pull/108) incorpora un certificado
  ARBA histórico, ajeno al alcance actual. No usarlo como solución TLS vigente.

## Matriz de los 33 issues abiertos

**Cubierto** significa capacidad constatada; **reforzar** indica evidencia de
contrato/código con pruebas específicas faltantes; **parcial** identifica una
limitación real del alcance; **ampliación** requiere un módulo nuevo; **no aplica**
identifica un componente Python/COM ausente. Los identificadores E1–E8 remiten a
la evidencia del repositorio al final de esta página.

| Issue | Tema y contraste | Decisión |
|---|---|---|
| [#157](https://github.com/reingart/pyafipws/issues/157) | PDF falla con IVA exento/no gravado; no hay motor PDF .NET. | Ampliación P3; conservar caso de prueba para PDF y escenarios fiscales P2. E8. |
| [#152](https://github.com/reingart/pyafipws/issues/152) | Rotación WSAA, múltiples CUIT y credencial compartida. | Parcial; coordinación/store TA P1. E1. |
| [#148](https://github.com/reingart/pyafipws/issues/148) | Wheels M2Crypto/pywin32 ya no descargan. | No aplica; NuGet/CLI usan dependencias declaradas. Reforzar smoke multi-OS P2. E7. |
| [#147](https://github.com/reingart/pyafipws/issues/147) | `bytes.encode` al consultar IIBB. | Módulo IIBB ausente; no trasladar corrección Python. E8. |
| [#136](https://github.com/reingart/pyafipws/issues/136) | Tipo `estadoImpuesto` de Constancia. | Contrato cubierto; fixture con impuestos poblados P1. E4. |
| [#135](https://github.com/reingart/pyafipws/issues/135) | Tipo bytes/string en salida PDF. | No aplica hoy; prueba de Unicode si se agrega PDF. E8. |
| [#134](https://github.com/reingart/pyafipws/issues/134) | Escritura XML/JSON falla después de recibir autorización. | Error Python no aplica; reforzar recuperación de persistencia P1. E5. |
| [#131](https://github.com/reingart/pyafipws/issues/131) | Claves privadas PKCS#8. | Cubierto con pruebas de PEM plano y cifrado. E2. |
| [#125](https://github.com/reingart/pyafipws/issues/125) | No se instancia Padrón A5 por COM. | No aplica; API administrada, sin registro COM. E7. |
| [#124](https://github.com/reingart/pyafipws/issues/124) | Lote Python no incorpora campos fiscales nuevos. | Campos presentes; reforzar serialización de lote/CAE/CAEA P1. E3. |
| [#119](https://github.com/reingart/pyafipws/issues/119) | Compilación DLL py2exe/Win32. | No aplica; mantener validación de paquetes .NET. E7. |
| [#117](https://github.com/reingart/pyafipws/issues/117) | Instalación pip en Linux con sintaxis Python 2. | No aplica al runtime; smoke Linux ya existe, ampliar matriz P2. E7. |
| [#105](https://github.com/reingart/pyafipws/issues/105) | Comprobante C: inconsistencias de importes reportadas. | Revisar ejemplo/validador opcional P2; DTO no valida todas las reglas fiscales. E3/E6. |
| [#99](https://github.com/reingart/pyafipws/issues/99) | `getargspec`/distutils y versiones Python nuevas. | No aplica; .NET no depende de Python/pysimplesoap. E7. |
| [#92](https://github.com/reingart/pyafipws/issues/92) | Compatibilidad Python 2/3. | No aplica; SDK .NET fijado y documentado. E7. |
| [#85](https://github.com/reingart/pyafipws/issues/85) | QR en PDF desde VB6. | Ampliación QR/PDF P3, no operación SOAP. E8. |
| [#84](https://github.com/reingart/pyafipws/issues/84) | PyQR en Windows XP. | Plataforma legacy no soportada; QR futuro es alcance distinto. E8. |
| [#83](https://github.com/reingart/pyafipws/issues/83) | `setup.py`: print e indentación. | No aplica. E7. |
| [#71](https://github.com/reingart/pyafipws/issues/71) | Importación `get_http` en SOAP Python. | No aplica; HttpClient/XmlSerializer nativos. E7. |
| [#66](https://github.com/reingart/pyafipws/issues/66) | Dependencias no declaradas en setup. | Mecanismo distinto ya atendido por PackageReference/lockfiles; mantener smoke de instalación. E7. |
| [#64](https://github.com/reingart/pyafipws/issues/64) | Incompatibilidad de httplib2/SSL. | No aplica; no copiar downgrade ni desactivar TLS. E7. |
| [#62](https://github.com/reingart/pyafipws/issues/62) | Factura de Crédito Electrónica. | Parcial respecto del objetivo funcional; WSFECred completo como hito P2 separado. E8. |
| [#61](https://github.com/reingart/pyafipws/issues/61) | Renovación automática de certificados. | CSR cubierta, renovación no; diagnóstico de vencimiento P2. E2. |
| [#57](https://github.com/reingart/pyafipws/issues/57) | Claves de fechas duplicadas en diccionario. | No se reproduce en DTO tipado; regresión de tres fechas pobladas P1 junto con E3. |
| [#55](https://github.com/reingart/pyafipws/issues/55) | Operaciones de productos ANMAT. | Ampliación P3; cliente ausente. E8. |
| [#54](https://github.com/reingart/pyafipws/issues/54) | Reutilizar TA almacenado externamente. | Caché local cubierta; restauración persistida/store opcional P1. E1. |
| [#53](https://github.com/reingart/pyafipws/issues/53) | Instalación M2Crypto. | No aplica; criptografía nativa .NET. E2/E7. |
| [#52](https://github.com/reingart/pyafipws/issues/52) | Setup depende de rutas/DLL locales. | No aplica; no importar redistribuibles históricos. E7. |
| [#40](https://github.com/reingart/pyafipws/issues/40) | Instanciación pywin32/registro COM. | No aplica a API .NET. E7. |
| [#31](https://github.com/reingart/pyafipws/issues/31) | Falta `designer.py` al empaquetar. | No aplica; PDF/designer no existen en este paquete. E7/E8. |
| [#18](https://github.com/reingart/pyafipws/issues/18) | Modo DBF de rece1. | Ampliación de formatos P3; sin importador DBF actual. E8. |
| [#14](https://github.com/reingart/pyafipws/issues/14) | Acentos/Ñ y separadores en PDF. | PDF ausente; trasladar idea a tests de cultura/Unicode de SOAP P2. E6/E8. |
| [#13](https://github.com/reingart/pyafipws/issues/13) | Ejemplo Delphi/COM y consultas de numeración. | COM no aplica; numeración se consulta con operaciones tipadas. E3/E8. |

## Matriz de los 21 PR abiertos

| PR | Propuesta y contraste | Decisión |
|---|---|---|
| [#155](https://github.com/reingart/pyafipws/pull/155) | Cambia imports SafeConfigParser en 14 archivos por compatibilidad Python 3.13. | No aplica al runtime .NET. E7. |
| [#153](https://github.com/reingart/pyafipws/pull/153) | Convierte cbt_desde/hasta a entero y usa cero si están vacíos. | DTO ya usa `long`; rechazar inválidos, no normalizar a cero silenciosamente. E3/E5. |
| [#151](https://github.com/reingart/pyafipws/pull/151) | WSLPG 1.24: nroCTG opcional en retiro/transferencia y respuesta. | Ampliación WSLPG P3, contrastar versión vigente antes de portar. E8. |
| [#150](https://github.com/reingart/pyafipws/pull/150) | A122R/ARBA IDP, retenciones REST y ejemplos. | Ampliación ARBA P3, fuera de ARCA SOAP actual. E8. |
| [#146](https://github.com/reingart/pyafipws/pull/146) | Helper de impuestos A5, incluido en un diff de 561 archivos. | Datos cubiertos; ejemplo/helper tipado P2. No integrar el diff completo. E4. |
| [#145](https://github.com/reingart/pyafipws/pull/145) | WSCPE: RENSPA de productor y consulta. | Ampliación WSCPE P3. E8. |
| [#138](https://github.com/reingart/pyafipws/pull/138) | Falta cerrar un archivo al generar instalador NSIS. | No aplica; no hay NSIS. Revisar disposal nativo como práctica, no como bug heredado. E7. |
| [#128](https://github.com/reingart/pyafipws/pull/128) | Agrega campos de moneda/IVA en CAESolicitar y reescribe WSFEX en otro archivo. | Campos .NET presentes; probar valores poblados P1. No asumir que corrige CAESolicitarX. E3. |
| [#127](https://github.com/reingart/pyafipws/pull/127) | Condición IVA del receptor con default y mapeo Python. | Campo .NET presente; no copiar una condición fiscal predeterminada para todos los clientes. E3. |
| [#116](https://github.com/reingart/pyafipws/pull/116) | Downgrade M2Crypto a 0.40.1. | No aplica; no agregar esa dependencia. E2/E7. |
| [#108](https://github.com/reingart/pyafipws/pull/108) | Certificado ARBA 2023 para COT. | No incorporar material TLS histórico; COT no está implementado. E8. |
| [#103](https://github.com/reingart/pyafipws/pull/103) | Actualiza Python cryptography 3.4.7 a 39.0.1. | No aplica; mantenimiento de dependencias .NET por sus propios avisos/lockfiles. E7. |
| [#90](https://github.com/reingart/pyafipws/pull/90) | API de imagen QR y encoding JSON/base64. | Ampliación P3: payload QR separado de renderizador PDF/imagen. E8. |
| [#88](https://github.com/reingart/pyafipws/pull/88) | Setup/encoding más cambios fiscales, binarios, cachés y tests borrados. | No incorporar en bloque; capacidades fiscales mayormente presentes, verificar escenarios E3/E8. |
| [#87](https://github.com/reingart/pyafipws/pull/87) | print/indentación de setup.py. | No aplica. E7. |
| [#74](https://github.com/reingart/pyafipws/pull/74) | WSFECred y adaptación PDF para FCE. | Hito WSFECred P2; renderizado independiente. E8. |
| [#70](https://github.com/reingart/pyafipws/pull/70) | Configuración WSFECred en INI. | Útil como intención del módulo futuro; usar Options/DI nativos. E8. |
| [#63](https://github.com/reingart/pyafipws/pull/63) | Setup: sintaxis print/indentación. | No aplica. E7. |
| [#49](https://github.com/reingart/pyafipws/pull/49) | Ejemplo PHP de WSCDC. | Servicio de constatación futuro P3; el ejemplo no es un cliente .NET. E8. |
| [#46](https://github.com/reingart/pyafipws/pull/46) | UTF-8 Python 3, parser de certificados; revisión señala typo X511. | UTF-8 nativo ya existe; no incorporar parche tal cual. E2/E7. |
| [#33](https://github.com/reingart/pyafipws/pull/33) | Ruta fija `/tmp/padron.db`. | No trasladar: padrón remoto distinto del cache local legacy; configuración explícita si se agrega. E8. |

## Evidencia en NetArcaWs

Los enlaces siguientes fijan la revisión auditada para no confundir el diagnóstico
con cambios posteriores. Las pruebas citadas fueron inspeccionadas: no se
agregaron nuevas regresiones ni se ejecutó homologación como parte de este análisis.

- **E1 — WSAA/contexto/cache:** [WsaaService](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/src/NetArcaWs/Wsaa/WsaaService.cs#L107), [tests multitenant](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/tests/NetArcaWs.UnitTests/Multitenancy/ArcaTenantContextTests.cs), [ADR 0002](Decisi%C3%B3n-2-Contexto-multitenant).
- **E2 — certificados:** [WsaaCryptography](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/src/NetArcaWs/Cryptography/WsaaCryptography.cs#L144), [PKCS#8 plano/cifrado](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/tests/NetArcaWs.UnitTests/Cryptography/WsaaCertificateContentTests.cs#L21), [CLI y límites](CLI-certificados).
- **E3 — campos fiscales y operaciones:** [DTO WSFE](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/src/NetArcaWs/Contracts/Generated/WsfeV1.g.cs#L273), [fachadas](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/src/NetArcaWs/Services/Generated/ArcaServices.g.cs), [tests genéricos de contratos](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/tests/NetArcaWs.UnitTests/Services/WsdlContractTests.cs#L127). Un round-trip genérico no reemplaza un caso de negocio poblado.
- **E4 — Padrón:** [DTO Impuesto A5](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/src/NetArcaWs/Contracts/Generated/PadronA5.g.cs#L1022), [WSDL A5](https://github.com/ARSASWebDesign/NetArcaWs/blob/main/docs/reference/contracts/padron-a5-production.wsdl), [operaciones](Padr%C3%B3n-A5-Referencia-de-operaciones).
- **E5 — emisión segura:** [SafeInvoiceService](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/src/NetArcaWs/Invoicing/SafeInvoiceService.cs), [tests de coordinación](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/tests/NetArcaWs.UnitTests/Invoicing/InvoiceCoordinatorTests.cs), [tests de reconciliación](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/tests/NetArcaWs.UnitTests/Invoicing/SafeInvoiceServiceTests.cs#L275).
- **E6 — aritmética/cultura:** [TaxCalculator](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/src/NetArcaWs/Taxation/TaxCalculator.cs), [alcance de cálculos](Calculos-decimales). No existe un validador tributario completo por tipo de comprobante.
- **E7 — plataforma/distribución:** [proyecto NuGet](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/src/NetArcaWs/NetArcaWs.csproj), [transporte nativo](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/src/NetArcaWs/Transport/SoapTransport.cs), [CI Ubuntu](https://github.com/ARSASWebDesign/NetArcaWs/blob/17a49c5ba48caee8f79bf1e9c276399309f4d85c/.github/workflows/ci.yml).
- **E8 — módulos disponibles/ausentes:** [servicios implementados](Servicios-implementados), [inventario upstream](Inventario-del-proyecto-original), [ADR de contratos](Decisi%C3%B3n-4-Contratos-SOAP-p%C3%BAblicos). La ausencia fue contrastada también con el árbol `src/`, no inferida solo del README.

## Orden propuesto de implementación

1. **Robustez del alcance actual:** regresiones SOAP y recuperación tras fallos
   locales; CI multi-OS/culturas. Convertir los reportes aplicables en fixtures
   sintéticos, sin datos personales ni credenciales de upstream.
2. **WSAA distribuido:** ADR y contrato de almacenamiento/coordinación, pruebas
   con procesos independientes y prueba autorizada de homologación. Mantener el
   proveedor en memoria como opción simple y no romper consumidores actuales.
3. **Usabilidad fiscal:** ejemplos completos validados, consulta tipada de
   impuestos y diagnóstico de vencimiento. La aplicación conserva la decisión
   fiscal; no introducir defaults tributarios silenciosos.
4. **Ampliaciones:** WSFECred como servicio separado; luego QR/PDF y servicios
   sectoriales según demanda. Revalidar los contratos oficiales en cada hito.

Para información fiscal actual, usar el [índice oficial de manuales ARCA](https://arca.gob.ar/fe/ayuda/webservice.asp)
y la [procedencia de los contratos](Procedencia-de-contratos-SOAP), no la
fecha o las afirmaciones de un issue. Las mejoras propuestas requieren sus
propias PR y pruebas; esta matriz no marca trabajo futuro como implementado.
