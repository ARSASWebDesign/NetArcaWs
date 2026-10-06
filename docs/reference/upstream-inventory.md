# Inventario documental de PyAfipWs

Corte reproducible: repositorio `reingart/pyafipws` rama `main`, commit
[`d595b072110accec9dae1ddb58165ab847b8520a`](https://github.com/reingart/pyafipws/commit/d595b072110accec9dae1ddb58165ab847b8520a)
(árbol de 622 archivos); wiki Git, commit `f942ebade93c41969d44e652b065ceabec535220`
(16 páginas Markdown incluyendo `_Sidebar.md`; [páginas canónicas](https://github.com/reingart/pyafipws/wiki)).
Se clonaron ambos repositorios y se inventariaron sus árboles fijados. GitHub
sirve las páginas wiki por nombre y no ofrece un permalink de página con SHA;
por eso las filas enlazan a las páginas canónicas y el commit del clon fija el
contenido auditado. El manual web se consultó el 2026-10-06; es una fuente
externa mutable y no forma parte de esos SHA.

## Cómo leer los estados

- **Adaptado**: hay una capacidad relacionada en NetArcaWs. No afirma igualdad
  de API ni cubre todos los helpers, interfaces y usos del original.
- **No equivalente**: no se encontró contraparte con el propósito del artefacto
  upstream dentro del alcance actual.
- **Manual upstream sin soporte**: se conserva como referencia del proveedor
  original; NetArcaWs no implementa ese flujo o utilidad. Tener una explicación
  en un manual no significa que el paquete la soporte.

El port cubre cinco familias de integración: WSFEv1, WSFEXv1, WSMTXCA y Padrón
(A4, Constancia por ruta histórica A5, A10 y A13). Esos siete contratos suman
81 operaciones. Para estas familias, **adaptado** significa cobertura de las
operaciones del contrato descritas en [Servicios y cobertura](../wiki/Servicios-y-cobertura.md),
no equivalencia completa con PyAfipWs. Otros módulos, servicios, herramientas,
aplicaciones o formatos no quedan incluidos por este inventario.

## Archivos de documentación y texto del repositorio

Se enumeran todas las rutas versionadas con extensiones documentales o de texto
(`.md`, `.rst`, `.txt`, `.html/.htm`, `.pdf`, `.adoc`, `.odt`, `.doc/.docx`);
los `.txt` que son fixtures/datos se identifican como tales. En el SHA
consultado no había `.rst`, `.html/.htm`, `.adoc`, `.odt` ni `.doc/.docx` dentro
del repositorio Git.

| Fuente fijada | Tema | Destino NetArcaWs | Estado |
|---|---|---|---|
| [`.github/pull_request_template.md`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/.github/pull_request_template.md) | Lista de control y evidencia manual de PR Python | Flujo actual de contribución propio | No equivalente; plantilla upstream |
| [`CONTRIBUTING.md`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/CONTRIBUTING.md) | DCO, sign-off y aportes | `CONTRIBUTING.md` | Adaptado como proceso del repositorio; no es un port funcional |
| [`README.md`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/README.md) | Resumen, estructura, servicios, formatos, instalación y enlaces | `README.md`, [Servicios y cobertura](../wiki/Servicios-y-cobertura.md), [Inicio rápido](../wiki/Inicio-rapido.md) | Adaptado para el subconjunto .NET; el README upstream enumera alcance mayor |
| [`licencia.txt`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/licencia.txt) | Atribución, garantía y términos de licencia del programa | [esta nota de atribución](../wiki/Migracion-desde-PyAfipWs.md#atribución-y-licencia-de-las-fuentes) | Referenciado/resumido; no se copia ni se infiere licencia uniforme |
| [`requirements.txt`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/requirements.txt) | Dependencias Python de ejecución | `*.csproj`, `packages.lock.json` | No equivalente; dependencias distintas |
| [`requirements-dev.txt`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/requirements-dev.txt) | Dependencias de pruebas/desarrollo Python | Proyectos `.NET` y lock files propios | No equivalente; herramientas distintas |
| [`datos/TB_20111111112_000000_20080124_000001.txt`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/datos/TB_20111111112_000000_20080124_000001.txt) | Archivo de datos de prueba/ejemplo | Ninguno | Manual upstream sin soporte; formato/escenario del paquete Python |
| [`datos/TB_20111111112_000000_20101229_000001.txt`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/datos/TB_20111111112_000000_20101229_000001.txt) | Archivo de datos de prueba/ejemplo | Ninguno | Manual upstream sin soporte |
| [`datos/facturas.txt`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/datos/facturas.txt) | Entrada de ejemplo de factura | Ninguno; requests SOAP tipados no implementan lector de lote | Manual upstream sin soporte |
| [`ejemplos/pyfepdf/factura.pdf`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/ejemplos/pyfepdf/factura.pdf) | Salida PDF ilustrativa | Ninguno | Manual upstream sin soporte; no hay generador PDF equivalente |
| [`ejemplos/wsfev1/ej_powerbuilder.txt`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/ejemplos/wsfev1/ej_powerbuilder.txt) | Ejemplo cliente PowerBuilder | Ninguno | Manual upstream sin soporte; no hay compatibilidad PowerBuilder/COM |
| [`tests/facturas.txt`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/tests/facturas.txt) | Fixture de entrada para tests Python | Ninguno | Manual upstream sin soporte |
| [`tests/txt/entrada_receb1.txt`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/tests/txt/entrada_receb1.txt) | Fixture de interfaz de texto para comprobantes B | Ninguno | Manual upstream sin soporte |
| [`tests/txt/entrada_recex1.txt`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/tests/txt/entrada_recex1.txt) | Fixture de interfaz de texto de exportación | Ninguno | Manual upstream sin soporte |

`COPYING`, `COPYING.LESSER` y `licencia.txt` también están en el árbol, aunque
los dos primeros no tienen extensión documental: ver el apartado de licencia
abajo. El árbol entero contiene además ejemplos en C#, Java, PHP, VB/VFP,
Delphi, C y otros lenguajes; no son documentación de texto de esta lista ni
significan que esas interfaces estén portadas.

## Páginas de la wiki upstream

GitHub sirve las páginas wiki por nombre y no admite un permalink de página
con SHA; por eso cada fila enlaza a su URL canónica actual y el SHA del clon fija
el contenido inventariado. El clon Git con ese commit permite reconstruir la
copia examinada. Cada página puede remitir a instaladores, protocolos o sitios
ya obsoletos.

| Página fuente | Tema | Destino NetArcaWs | Estado |
|---|---|---|---|
| [`ComoColaborar.md`](https://github.com/reingart/pyafipws/wiki/ComoColaborar) | Formas de colaborar y canales comunitarios | `CONTRIBUTING.md` propio | No equivalente; proceso upstream |
| [`ContributorAgreement.md`](https://github.com/reingart/pyafipws/wiki/ContributorAgreement) | Acuerdo legal de contribución de PyAfipWs | Ninguno | No equivalente; no trasplantar acuerdos |
| [`Descargas.md`](https://github.com/reingart/pyafipws/wiki/Descargas) | Releases y descargas históricas | [releases](../wiki/Desarrollo-y-contribucion.md) | Manual upstream sin soporte; instaladores no se usan en .NET |
| [`Difusion.md`](https://github.com/reingart/pyafipws/wiki/Difusion) | Difusión, artículos, eventos y enlaces | Ninguno | Manual upstream sin soporte |
| [`Factura-Electronica-Python-3.md`](https://github.com/reingart/pyafipws/wiki/Factura-Electronica-Python-3) | Ejemplo de factura Python 3 con CAE y PDF | [migración](../wiki/Migracion-desde-PyAfipWs.md) | Manual upstream sin soporte; no copiar código Python ni asumir PDF |
| [`FacturaElectronicaPython.md`](https://github.com/reingart/pyafipws/wiki/FacturaElectronicaPython) | Ejemplo de API Python WSAA/WSFE | [WSAA y certificados](../wiki/WSAA-y-certificados.md), docs WSFE | Adaptado conceptualmente, no por API/código |
| [`Home.md`](https://github.com/reingart/pyafipws/wiki/Home) | Portada y presentación del ecosistema | `README.md`, wiki local | Adaptado al alcance menor |
| [`InstalacionCodigoFuente.md`](https://github.com/reingart/pyafipws/wiki/InstalacionCodigoFuente) | Instalación Python, dependencias, entornos e instaladores Windows | [desarrollo](../wiki/Desarrollo-y-contribucion.md) | Manual upstream sin soporte; las instrucciones no aplican a .NET |
| [`PresentacionesEventos.md`](https://github.com/reingart/pyafipws/wiki/PresentacionesEventos) | Presentaciones, charlas y eventos | Ninguno | Manual upstream sin soporte |
| [`ProjectSummary.md`](https://github.com/reingart/pyafipws/wiki/ProjectSummary) | Resumen en inglés de paquetes, servicios y apps | `README.md`, [Servicios y cobertura](../wiki/Servicios-y-cobertura.md) | Adaptado; servicios listados fuera de alcance permanecen sin soporte |
| [`PyAfipWs.md`](https://github.com/reingart/pyafipws/wiki/PyAfipWs) | Interfaz tipo OCX, COM y utilitarios de texto | [migración](../wiki/Migracion-desde-PyAfipWs.md) | Manual upstream sin soporte; NetArcaWs no expone OCX/COM |
| [`PyFactura.md`](https://github.com/reingart/pyafipws/wiki/PyFactura) | Aplicación de facturación visual | Ninguno | Manual upstream sin soporte |
| [`PyRece.md`](https://github.com/reingart/pyafipws/wiki/PyRece) | Aplicación GUI/lotes/PDF/email | Ninguno | Manual upstream sin soporte |
| [`WSFEX.md`](https://github.com/reingart/pyafipws/wiki/WSFEX) | Descripción WSFEX y enlaces a manuales | [WSFEXv1](../wiki/Servicios-y-cobertura.md) | Adaptado al contrato WSFEXv1; no a la API ni a todo WSFEX legado |
| [`WSFEv1.md`](https://github.com/reingart/pyafipws/wiki/WSFEv1) | Descripción WSFEv1, clases y manual | [WSFEv1](../wiki/Servicios-y-cobertura.md) | Adaptado al contrato; sin paridad completa de helpers/API |
| [`_Sidebar.md`](https://github.com/reingart/pyafipws/wiki) | Navegación de la wiki upstream (GitHub la muestra con Home) | [Home](../wiki/Home.md), navegación Markdown nativa | Adaptado a la navegación local; el contenido del índice upstream no se copia |

## Módulos Python de nivel superior

La tabla incluye los 48 `.py` ubicados en la raíz del snapshot upstream. La
descripción identifica el tema por nombre/implementación; el destino indica el
componente más cercano en el árbol actual, no una promesa de paridad. Los
servicios no adaptados también pueden estar descritos en manuales upstream.

| Módulo Python y permalink fijado | Tema | Destino NetArcaWs | Estado |
|---|---|---|---|
| [`__init__.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/__init__.py) | Paquete y metadatos Python | Estructura de ensamblados .NET | No equivalente |
| [`cot.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/cot.py) | Código de Operaciones de Traslado de ARBA | Ninguno | Manual upstream sin soporte |
| [`iibb.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/iibb.py) | Consulta/proceso de Ingresos Brutos provincial | Ninguno | Manual upstream sin soporte |
| [`nsis.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/nsis.py) | Generación de instaladores NSIS/Python | Publicación propia de paquetes .NET | No equivalente |
| [`padron.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/padron.py) | Consulta/archivo de padrón fiscal local histórico | `PadronService` | No equivalente: clientes SOAP vigentes no replican el dataset ni la API de este módulo |
| [`pyemail.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/pyemail.py) | Envío de correo asociado a aplicaciones | Ninguno | No equivalente |
| [`pyfepdf.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/pyfepdf.py) | Renderizado y diseño de comprobantes PDF | Ninguno | Manual upstream sin soporte |
| [`pyi25.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/pyi25.py) | Código de barras Interleaved 2 of 5 | Ninguno | Manual upstream sin soporte |
| [`pyqr.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/pyqr.py) | Creación de QR | Ninguno | Manual upstream sin soporte |
| [`rece1.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/rece1.py) | Interfaz de texto/RECE para WSFE mercado interno | `Wsfev1Service` cubre contrato SOAP | Manual upstream sin soporte para CLI, lotes y formatos |
| [`receb1.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/receb1.py) | Interfaz RECE de bonos fiscales | Ninguno | Manual upstream sin soporte |
| [`recem.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/recem.py) | Interfaz de texto para WSMTXCA | `Wsmtxcav1Service` cubre contrato SOAP | Manual upstream sin soporte para CLI, lotes y formatos |
| [`recet.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/recet.py) | Interfaz de texto para servicio de turismo | Ninguno | Manual upstream sin soporte |
| [`recex1.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/recex1.py) | Interfaz de texto/RECE para exportación | `Wsfexv1Service` cubre contrato SOAP | Manual upstream sin soporte para CLI, lotes y formatos |
| [`register_pyqr.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/register_pyqr.py) | Registro de objeto COM de PyQR | Ninguno | No equivalente |
| [`rg3685.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/rg3685.py) | Formatos/exportaciones asociados a RG 3685 | Ninguno | Manual upstream sin soporte |
| [`setup.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/setup.py) | Instalación/empaquetado Python | Archivos `.csproj` y build .NET | No equivalente |
| [`setup_win.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/setup_win.py) | Empaquetado Python para Windows | Publicación .NET | No equivalente |
| [`sired.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/sired.py) | Herramientas/archivos SIRED | Ninguno | Manual upstream sin soporte |
| [`trazafito.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/trazafito.py) | Trazabilidad de productos fitosanitarios | Ninguno | Manual upstream sin soporte |
| [`trazamed.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/trazamed.py) | Trazabilidad de medicamentos | Ninguno | Manual upstream sin soporte |
| [`trazaprodmed.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/trazaprodmed.py) | Trazabilidad de productos médicos | Ninguno | Manual upstream sin soporte |
| [`trazarenpre.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/trazarenpre.py) | Trazabilidad de precursores químicos | Ninguno | Manual upstream sin soporte |
| [`trazavet.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/trazavet.py) | Trazabilidad de productos veterinarios | Ninguno | Manual upstream sin soporte |
| [`utils.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/utils.py) | Helpers Python: XML, SOAP, conversiones, archivos y COM | `Transport`, contratos XML tipados, WSAA | Adaptado parcialmente; no cubre los helpers/formatos/interoperabilidad del módulo |
| [`wdigdepfiel.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wdigdepfiel.py) | Servicio de Depositario Fiel | Ninguno | Manual upstream sin soporte |
| [`ws_sire.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/ws_sire.py) | Sistema Integral de Retenciones Electrónicas | Ninguno | Manual upstream sin soporte |
| [`ws_sr_padron.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/ws_sr_padron.py) | Cliente histórico A4/A5 | `PadronA4Service`, `PadronA5Service` | Adaptado; A10/A13 y Constancia vigente se modelan desde fuentes ARCA actuales |
| [`wsaa.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wsaa.py) | WSAA, CMS, CSR y autenticación | `WsaaService`, `WsaaCertificateContent`, CLI de certificados | Adaptado parcialmente; API COM/helpers no equivalentes |
| [`wsbfev1.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wsbfev1.py) | Bono Fiscal Electrónico | Ninguno | Manual upstream sin soporte |
| [`wscdc.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wscdc.py) | Constatación de comprobantes | Ninguno | Manual upstream sin soporte |
| [`wscoc.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wscoc.py) | Consulta de operaciones cambiarias | Ninguno | Manual upstream sin soporte |
| [`wscpe.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wscpe.py) | Carta de Porte Electrónica | Ninguno | Manual upstream sin soporte |
| [`wscpe_cli.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wscpe_cli.py) | CLI de Carta de Porte Electrónica | Ninguno | Manual upstream sin soporte |
| [`wsct.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wsct.py) | Factura electrónica de turismo | Ninguno | Manual upstream sin soporte |
| [`wsctg.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wsctg.py) | Código de Trazabilidad de Granos | Ninguno | Manual upstream sin soporte |
| [`wsfecred.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wsfecred.py) | Factura de Crédito Electrónica MiPyME | Ninguno | Manual upstream sin soporte; distinto de WSMTXCA |
| [`wsfev1.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wsfev1.py) | Web service de factura electrónica doméstica | `Wsfev1Service` | Adaptado al contrato actual, sin paridad de API/helpers |
| [`wsfexv1.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wsfexv1.py) | Web service de factura de exportación | `Wsfexv1Service` | Adaptado al contrato actual, sin paridad de API/helpers |
| [`wslpg.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wslpg.py) | Liquidación Primaria de Granos | Ninguno | Manual upstream sin soporte |
| [`wslpg_datos.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wslpg_datos.py) | Datos auxiliares/catálogos WSLPG | Ninguno | Manual upstream sin soporte |
| [`wslsp.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wslsp.py) | Liquidación pecuaria | Ninguno | Manual upstream sin soporte |
| [`wsltv.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wsltv.py) | Liquidación de tabaco verde | Ninguno | Manual upstream sin soporte |
| [`wslum.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wslum.py) | Liquidación de leche | Ninguno | Manual upstream sin soporte |
| [`wsmtx.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wsmtx.py) | Factura doméstica con detalle, WSMTXCA | `Wsmtxcav1Service` | Adaptado al contrato actual; no replica apps/formatos ni confundir con WSFCE |
| [`wsremazucar.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wsremazucar.py) | Remito electrónico de azúcar | Ninguno | Manual upstream sin soporte |
| [`wsremcarne.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wsremcarne.py) | Remito electrónico cárnico | Ninguno | Manual upstream sin soporte |
| [`wsremharina.py`](https://github.com/reingart/pyafipws/blob/d595b072110accec9dae1ddb58165ab847b8520a/wsremharina.py) | Remito electrónico de harina | Ninguno | Manual upstream sin soporte |

El listado anterior describe módulos de la raíz únicamente. Paquetes como
`formatos/`, ejemplos, pruebas, fixtures SOAP, scripts de build y fuentes C
están en el mismo commit y fueron inspeccionados en el árbol, pero no son
módulos Python top-level ni parte del inventario documental por extensión.
Consultar el árbol fijado para hallarlos. Ningún módulo Python se incorporó a
NetArcaWs.

## Manual de uso externo y cobertura documental

Se examinó el [Manual de uso PyAfipWs](https://sistemasagiles.com.ar/site/websites/documentacion_herramientas/manualpyafipws.html),
servido como HTML en la consulta del 2026-10-06 (4.826 líneas expuestas por el
lector web). Es un manual de producto extenso: instalación y evaluación,
credenciales/certificados, interfaz COM y herramientas de texto, servicios de
facturación con operaciones/errores/ejemplos, reintentos/reproceso, PDF y
herramientas adicionales (padrones, verificaciones, granos, trazabilidad, entre
otros). Se usó para identificar categorías y dependencias del ecosistema; no se
copiaron pasajes largos ni ejemplos de código.

La migración documenta las cinco familias objetivo y los límites conocidos. El
manual contiene capítulos de numerosos módulos sin destino: se clasifican
**manual upstream sin soporte**, no “adaptados”. Tampoco se tomaron como prueba
actual sus direcciones de servicio, instrucciones para Python 2/Windows,
normativa o declaraciones de producción: para endpoints, contratos y conducta
vigente prevalecen los WSDL/manuales ARCA fechados en
[proveniencia de contratos](contracts/sources.md) y las fuentes enlazadas allí.

El texto consultado identifica a Mariano Reingart como autor/mantenedor del
proyecto y remite a Sistemas Ágiles. Para código, el upstream incluye avisos
LGPLv3+ en varios módulos, `COPYING` (GPL), `COPYING.LESSER` (LGPL) y una
declaración propia en `licencia.txt`; el README describe LGPLv3+ y menciona una
excepción comercial. Los avisos del archivo y las dependencias deben comprobarse
antes de reutilizar código; no se concluye una licencia uniforme. La licencia
específica de las páginas de wiki y del manual no se pudo verificar. NetArcaWs
resume, atribuye y enlaza: no republica el texto original.

## Fuentes ARCA del alcance .NET

- [Manuales de desarrollador de factura electrónica ARCA](https://www.arca.gob.ar/fe/ayuda/webservice.asp).
- Contratos fechados, hashes y excepciones: [sources.md](contracts/sources.md).
- Catálogo oficial de servicios y manuales de Padrón están referenciados en
  [healthchecks-contracts.md](healthchecks-contracts.md).
