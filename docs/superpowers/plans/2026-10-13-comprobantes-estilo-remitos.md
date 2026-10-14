# Plantilla y remitos Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** aplicar diseño suministrado, color único/versiones y exportación completa a HTML/PDF, añadir remitosR/X y renovar galería.

**Architecture:** modelos remito en core sin dependencias visuales; reglas internas compartidas enlazadas; generadores mantienen APIs previas y agregan color opcional/sobrecarga remito. Snapshots fiscales y diario intactos.

**Tech Stack:** .NET10, xUnitv3/AwesomeAssertions/MTP, dependencias renderer existentes. Sin nuevas librerías.

**Spec:** ../specs/2026-10-13-comprobantes-estilo-remitos-design.md

## Global Constraints

- Rama existente, Luna, un implementador activo por vez y revisión independiente. No push/PR antes de validar visualmente.
- No guardar imágenes adjuntas ni sus datos reales; fixtures sintéticas exclusivamente.
- No implementar firma criptográfica pendiente ni alterar contratosSOAP/hash/retries. No ARCA autenticada.
- Versiones NuGet sin cambios. Fuentes embebidas, no estado global de marca/color.

## Task 1: modelos remitos

- [ ] TDD: JSON/colecciones defensivas/R válido/CAI/rango/fechas/imprenta/autoimpresor/X interno/inválidos.
- [ ] Crear modelos públicos remito y fábrica local con validación reusable, sin heredar estadoAuthorized de factura. Documentar límites normativos.
- [ ] Pruebas focales, commit local, reporte y review independiente.

## Task 2: HTML y reglas compartidas

- [ ] TDD: color normalizado inválidos/contraste/versiones, referenciasX/R, datosE desde snapshot y leyendas.
- [ ] Implementar constructor colorPrincipal opcional, VersionRenderizado, helper estilo y camposE compartidos; nueva plantilla fiel a referencias en todos tipos y remitos. Colores y tablas según spec.
- [ ] Verificar responsive y escape, compatibilidad pruebas previas; commit/reporte/review.

## Task 3: PDF

- [ ] TDD: estilo/color/VersionRenderizado/textoE/remitos/sinQR/CAE, layout prolongado y perfiles concurrentes.
- [ ] Adaptar PDF a misma jerarquía y reglas compartidas, sobrecargaRemito, fuentes sin configuración de emisor global. Radio discreto, texto de autorización sin cuadro, columnas legibles y paginación.
- [ ] Pruebas focales y raster preliminar, commit/reporte/review.

## Task 4: galería y documentación

- [ ] Ampliar aplicación de muestras con remitosR/X, E realista con campos diferenciados, color configurable y marcas; mantenerSQLite/JSON y72h/3d.
- [ ] Actualizar README/ADR/wiki, ejemplo cachéVersionRenderizado y aclarar cambios de configuración; generar wiki.NET.
- [ ] Regenerar galería marcada, QA navegador/raster, corregir hallazgos; commit/reporte/review.
- [ ] Controller restorelocked/build/test con módulossecuenciales (timeout existente documentado), pack13/smoke cuando corresponda, review final y muestras para usuario. No entrega remota.
