# Plantilla común, exportación y remitos

El usuario proporcionó tres referencias visuales A/B/C y una factura E y pidió aplicar el diseño a todos los comprobantes y remitos R/X. Confirmó ambas clases de remito. Se continúa la rama local de issue33; no enviar cambios antes de la revisión visual.

## Diseño solicitado

HTML y PDF comparten jerarquía: ORIGINAL arriba; cabecera compacta con marca y domicilio a izquierda, letra centrada, tipo/número/fecha y datos fiscales del emisor a derecha; receptor en dos columnas; tabla con filas blancas y separadores grises; totales destacados a derecha; QR fiscal y autorización en texto simple en negrita. Radios discretos (4px HTML o equivalente). No copiar marcas, datos personales, códigos de barras ni pies promocionales de las imágenes.

Cada generador admite `colorPrincipal` opcional `#RRGGBB`, por defecto `#333333`. Una sola tinta configurable en cabecera, encabezados de tablas y total; el resto blanco/gris/negro. Contraste automático negro/blanco para texto sobre ese color. No CSS libre ni colores secundarios. Logo opcional mantiene proporción. Pie fiscal dice ARCA y CAE; X/remitos no dicen autorizados ni tienen QR/CAE. Originales y duplicados se identifican cuando el modelo lo indica.

`VersionRenderizado` pública en cada generador combina versión real del paquete y revisión del renderer (HTML/PDF independientes). No cambiar versión NuGet0.6.0 en esta tarea. La versión identifica código/plantilla, no el documento, emisor, logo ni color; consumidor debe invalidar también cuando cambie su configuración. No reemitir ARCA al regenerar.

Exportación: leer exclusivamente el request WSFEX congelado validado para CUIT país, ID fiscal extranjero, código país destino, divisa/cotización, forma/fecha de pago, Incoterms y observaciones pertinentes. No confundir país destino con CUIT país ni inventar nombres de países; etiquetas suplementarias solo si hay correlación explícita. Usar nombres/códigos de moneda claros, precio/total con código en encabezado, cambio con hasta6decimales sin redondear el dato fuente. Mantener invariantes de snapshots/diario y tipos SOAP.

## Remitos

Nuevo modelo inmutable `RemitoGenerado`, separado de autorización fiscal de factura, y fábrica local `Remito(DatosEmisor).GenerarComprobante(DatosRemito)`. Generadores aceptan sobrecargas HTML/PDF del modelo. Numeración aportada por aplicación, sin asignación ni solicitud CAI automática. JSON roundtrip y validación al renderizar. Detalles de mercadería con descripción, cantidad positiva y unidad; destino, receptor, fecha, punto/número, copia y transportista cuando corresponda. No importes ni impuestos de venta inventados.

R: código91, leyenda obligatoria, CAI14dígitos, vencimiento, rango autorizado que contiene el número, datos de imprenta/impresión/habilitación o condición autoimpresor explícita con excepciones documentadas. Exigir perfil legal emisor y receptor. Validar fecha emisión contra vigencia/rango, nunca contra fecha actual al reimprimir histórico. CAI aportado no equivale a autorización comprobada por esta biblioteca. No incorporar regímenes sectoriales electrónicos ni controladores fiscales.

X: uso interno dentro del mismo predio/polo/parque industrial; leyenda obligatoria, sin CAI/CAE/QR fiscal y sin código901 de controlador fiscal que esta biblioteca no implementa. Identificar el ámbito interno explícitamente; no presentarlo como remito general habilitado para transporte externo.

## Evidencia

Pruebas de color/escape/contraste/aislamiento/versiones, receptor/exportación desde snapshot, JSON/remitos inválidos y autorización ausente, extracción PDF/QR, documentos extensos. Galería sintética con todos los tipos previos, remitosR/X y variante de color única; logo cuadradoLOGO, fantasía/razón social, sinlogo/sinfantasía. Aviso sintético en cada archivo; conservar72h, CI3d. QA real PDF raster y navegador desktop/móvil antes de mostrar.

Endurecimiento criptográfico de facturas queda como decisión pendiente: no añadir una firma ni afirmar procedencia ARCA sin definir la clave externa de la aplicación. Esta tarea no convierte coherencia de snapshots en autenticidad.

## Fuentes consultadas

- RG1415 texto actualizado, artículos27–30 y AnexoV: https://www.argentina.gob.ar/normativa/nacional/resoluci%C3%B3n-1415-2003-81316/actualizacion
- ARCA consulta remitosX: https://www.afip.gob.ar/genericos/guiavirtual/consultas_detalle.aspx?id=364752
- RG100 tabla de comprobantes, RemitoR91: https://biblioteca.afip.gob.ar/search/query/norma.aspx?p=t%3ARAG%7Cn%3A100%7Co%3A3%7Ca%3A1998%7Cf%3A11%2F03%2F1998
- WSFEX manual vigente enlazado: https://afip.gob.ar/fe/documentos/WSFEX-Manual-para-el-desarrollador.pdf
