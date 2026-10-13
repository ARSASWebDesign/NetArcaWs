# Comprobantes: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** emitir modelos persistibles fiscales/X y representarlos con paquetes HTML/PDF opcionales, mostrando muestras al usuario antes de enviar cambios.

**Architecture:** el core ofrece modelos inmutables y una fachada sobre SafeInvoiceService. Los generadores toman emisor en el constructor y receptor del comprobante; comparten reglas mediante fuentes internas enlazadas, sin dependencia entre paquetes HTML y PDF.

**Tech Stack:** .NET 10 según global.json; System.Text.Json; xUnit v3, AwesomeAssertions y MTP; PDFsharp/MigraDoc 6.2.4 para PDF y QRCoder 1.8.0 para QR en paquetes opcionales. Noto Sans 2.015 Regular/Bold (SIL OFL 1.1) con licencia incluida y resolución desde recursos embebidos, sin fuentes del sistema.

**Spec:** ../specs/2026-10-13-comprobantes-rendering-design.md

## Global Constraints

- PDF se genera en .NET, sin navegador, Python o servicios remotos.
- El receptor proviene del comprobante y nunca de la configuración del generador.
- No modificar DTO SOAP generados, hashes, estados ni invariantes del diario existente.
- Dependencias de renderizado fuera de NetArcaWs; paquetes de versión igual al core.
- PNG opcional con copia defensiva, límite 1 MiB y dimensiones de hasta 2048 por lado; validar decodificación completa.
- Retener muestras sintéticas 72 horas; antes de push/PR presentar muestras y esperar validación visual del usuario.
- Agentes Luna implementan; un agente distinto revisa las pruebas y el resultado.

## Review Focus

1. Deserializar un modelo malformado no debe permitir imprimir una autorización incompleta.
2. Cambiar datos del receptor para presentación no debe contradecir la identificación enviada a ARCA.
3. Dos generadores concurrentes de distintos emisores no deben compartir configuración ni logos.
4. Textos muy largos, Unicode y múltiples páginas deben preservar totales y pie de autorización.
5. Un fallo de renderizado después de autorizar nunca debe generar otro envío fiscal.

## Task 1: modelo persistible y recibo X

**Files:** crear `src/NetArcaWs/Documents/ComprobanteGenerado.cs`, `DatosEmisor.cs`, `DatosReceptor.cs`, `DatosPresentacion.cs`, `ReciboX.cs`, `ComprobanteValidator.cs`; pruebas en `tests/NetArcaWs.UnitTests/Documents/`.

**Interfaces:** namespace `NetArcaWs.Documents`; `ComprobanteGenerado` incluye version 1, discriminador fiscal/X, identidad, estado, receptor, detalle/totales, snapshot request y resultado sin secretos. `DatosEmisor` inmutable acepta logo PNG opcional. `ReciboX.GenerarComprobante(DatosReciboX datos)` devuelve `ComprobanteGenerado`.

- [ ] Escribir pruebas fallidas de JSON round-trip, colecciones y logo defensivos, metadatos sin secretos, recibos total/parcial, asociaciones múltiples y rechazo de sumas/monedas incoherentes.
- [ ] Ejecutar pruebas del proyecto y registrar el fallo esperado por API ausente.
- [ ] Implementar modelos, validaciones comunes y generación local X; los importes son explícitos, sin tasas ni numeración automáticas. Identificar cada referencia con moneda y monto imputado.
- [ ] Probar modelos malformados tras JSON, emisor incompatible y receptor contradictorio; los métodos de generación/representación validan nuevamente los datos.
- [ ] Ejecutar pruebas; revisar diffs y registrar commit local de tarea.

## Task 2: fachada fiscal y adaptadores

**Files:** crear `src/NetArcaWs/Documents/ArcaWsComprobantes.cs`, `WsfeDocumentAdapter.cs`, `WsfexDocumentAdapter.cs`, `WsmtxcaDocumentAdapter.cs`; pruebas `tests/NetArcaWs.UnitTests/Documents/ArcaWsComprobantesTests.cs` y doubles sintéticos.

**Interfaces:** `ArcaWsComprobantes(SafeInvoiceService invoices)`; tres sobrecargas `Task<ComprobanteGenerado> GenerarComprobanteAsync(ArcaTenantContext tenant, string key, <FecaeRequest|ClsFexRequest|ComprobanteType> request, DatosPresentacion presentacion, CancellationToken cancellationToken = default)`. Receptor y conceptos suplementarios se reciben en presentacion; lo fiscal procede del snapshot congelado y de la operación retornada.

- [ ] Escribir y ejecutar pruebas fallidas de los tres servicios y de rejected/unknown/conflict; preservar estado y errores, sin inventar CAE.
- [ ] Implementar fachada aditiva usando AuthorizeWsfeAsync/AuthorizeWsfexAsync/AuthorizeWsmtxcaAsync. Copiar antes de await y proyectar desde operación persistida para evitar mutación, incluso cuando se reusa idempotencia.
- [ ] Validar identidad/totales/receptor suplementarios contra snapshot; un servicio sin detalle no recibe conceptos inventados. Conservar datos fiscales exactos y expresar conversión decimal explícitamente, rechazando NaN, infinito y overflow. WSFE usa resumen sin ítems inventados; WSFEX/MTXCA conservan sus ítems. La fachada devuelve el modelo con estado y errores en todos los resultados; solo Authorized y X local son representables. No imprimir tenant interno ni evidencia XML.
- [ ] Probar cultura, diferencias de respuesta, revisiones e idempotencia, vencimiento CAE y renderización fallida sin segundo envío. Revisar manuales oficiales vigentes para los tipos A/B/C/E habilitados; tipos fuera del alcance fallan explícitamente.
- [ ] Ejecutar pruebas y registrar commit local de tarea.

## Task 3: paquete HTML y reglas comunes

**Files:** crear `src/NetArcaWs.Rendering.Html/`, `src/Rendering.Shared/DocumentLayout.cs`, `FiscalQrPayload.cs`, `DocumentRenderValidator.cs`; crear `tests/NetArcaWs.Rendering.Tests/` y registrar proyectos en `NetArcaWs.slnx`.

**Interfaces:** `ArcaWsGeneradorHTML(DatosEmisor emisor)`; `string GenerarHTMLComprobante(ComprobanteGenerado datos)`. Fuentes shared internas enlazadas en los dos paquetes; QRCoder en cada paquete que lo consume.

- [ ] Escribir pruebas fallidas para X y fiscal, nombres con/sin logo, emisor incompatible, receptor desde documento, escape HTML, rechazo de estados incompletos y QR.
- [ ] Implementar reglas de layout y payload QR conforme al enlace de especificaciones publicado por ARCA; verificar campos, codificación Base64, URL y representación de números/fechas contra https://www.afip.gob.ar/fe/qr/documentos/QRespecificaciones.pdf. Campos obligatorios: ver, fecha, cuit, ptoVta, tipoCmp, nroCmp, importe, moneda, ctz, tipoCodAut, codAut. CAE usa tipoCodAut E; receptor según corresponda. Mapear explícitamente códigos WSFE/MTXCA 1/2/3 (A), 6/7/8 (B), WSFE 11/12/13 (C) y WSFEX 19/20/21 (E), verificando disponibilidad y leyendas oficiales antes de habilitar cada combinación. No inferir letra del servicio.
- [ ] Implementar HTML A4 autónomo, QR e imagen embebidos, encabezado/totales/pie, estilos de impresión y tablas multipágina. X lleva leyenda obligatoria superior y carece de QR/CAE.
- [ ] Probar valores largos, culturas es-AR/en-US, caracteres especiales y concurrencia de dos emisores; validar QR por decodificación independiente de sus muestras.
- [ ] Restore deliberado para nuevos proyectos/dependencias con `--force-evaluate`, revisar lockfiles, ejecutar pruebas y commit local.

## Task 4: paquete PDF

**Files:** crear `src/NetArcaWs.Rendering.Pdf/`, resolver de fuentes y recursos con licencia; ampliar `tests/NetArcaWs.Rendering.Tests/`.

**Interfaces:** `ArcaWsGeneradorPDF(DatosEmisor emisor)`; `byte[] GenerarPDFComprobante(ComprobanteGenerado datos)`; consume mismas reglas de Task 3 sin llamar a HTML.

- [ ] Escribir y ejecutar pruebas fallidas de PDF legible, presencia de textos legales, marca, ausencia QR fiscal en X, QR fiscal decodificable y múltiples páginas.
- [ ] Revisar licencias de paquetes/transitivas y Noto Sans 2.015 (SIL OFL 1.1); incluir fuente regular/bold y licencia en recursos y avisos. Usar resolver global inmutable exclusivamente para esas fuentes; no almacenar emisor/logo en estado global.
- [ ] Implementar con PDFsharp/MigraDoc core, A4, tablas y paginación; imágenes locales en memoria. Fallar antes de generar ante PNG inválido o datos no imprimibles.
- [ ] Verificar documentos largos, caracteres acentuados y que totales/autorización no se recorten; ejecutar pruebas y commit local.

## Task 5: ejemplos persistibles y artefactos visuales

**Files:** crear `examples/NetArcaWs.Rendering.Examples/` como ejecutable local; fixtures sintéticas compartidas bajo tests; modificar CI para ejecutar muestras y upload con retención 3 días y acción fijada por SHA.

**Interfaces:** `dotnet run --project examples/NetArcaWs.Rendering.Examples --configuration Release -- --output artifacts/comprobantes`; salida por ejecución, manifiesto timestamp y escenarios.

- [ ] Escribir prueba de round-trip SQLite/JSON del comprobante y perfil del emisor; volver a representar produce el mismo contenido fiscal sin invocar servicios.
- [ ] Generar PNG cuadrado LOGO en .NET y muestras de factura A/B/C WSFE, exportación E WSFEX, A/B MTXCA, X total/parcial, nota de crédito/débito cuando el adaptador habilite su tipo y multipágina. Usar únicamente datos sintéticos identificados como muestras.
- [ ] Emitir HTML/PDF con logo+fantasía+razón social, sin logo con ambos nombres y con razón social sola. Conservar directorios menores a 72 horas; limpiar únicamente directorios generados reconocidos y vencidos, mediante manifiesto.
- [ ] Renderizar PDFs con herramienta local de inspección y abrir HTML; corregir recortes, espacios, tamaño QR y legibilidad. Guardar previews PNG y rutas de muestras.
- [ ] Presentar galería y enlaces PDF al usuario; no hacer push ni abrir PR antes de su validación visual.

## Task 6: documentación, paquetes y verificación independiente

**Files:** README, ARCHITECTURE, PROGRESS, THIRD-PARTY-NOTICES, `docs/adr/`, `docs/wiki/`, `docs/releases.md`, `tools/NetArcaWs.Build/ReleaseVersion.cs`, `ReleaseAssets.cs`, pruebas de build y workflows de empaquetado/release; crear consumidor package smoke para renderizadores.

- [ ] Documentar API, receptor del comprobante, conservación del perfil de emisor, límites por servicio/tipo, separación emisión/representación y ejemplo completo. Generar wiki con herramienta .NET; no publicar.
- [ ] Incorporar los dos paquetes opcionales a allowlists/manifiestos de release, hashes y pruebas, sin cambiar versiones publicadas ni activar publicación. Añadir pruebas que detecten omisión de un renderizador en build/pack.
- [ ] Ejecutar restore locked, build/test Release, pack de core/tool/renderizadores y package smoke aislado; comprobar que core no incorpora dependencias PDF/QR.
- [ ] Agente Luna independiente revisa contra spec, contratos, fallos/culturas, aislamiento y evidencias visuales; corregir hallazgos y repetir solo comprobaciones afectadas.
- [ ] Tras validación visual del usuario, preparar PR con evidencia local y límites; merge/release quedan fuera de esta tarea.

## Handoff

Método solicitado por el usuario: agentes Luna. Revisar este plan antes de
implementar; la revisión visual de las plantillas es una aprobación distinta,
posterior a la generación de muestras y anterior a enviar cambios.
