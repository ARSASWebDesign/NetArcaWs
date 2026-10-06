# Hito 7 final: wiki integral de NetArcaWs

Estado: documentación Markdown local publicada en la wiki el 2026-10-06.
Quedan la revisión exhaustiva del inventario upstream y los ejemplos por operación;
la publicación no implica que el port esté completo.

## Objetivo y publicación

Consolidar **toda la documentación** del proyecto en el wiki de este repositorio
GitHub, con navegación por tarea y por servicio. Usar la documentación de PyAfipWs
como base temática y agregar las características propias de NetArcaWs, ejemplos
C# y sus diferencias de comportamiento.

Mantener las fuentes Markdown del wiki en `docs/wiki/` dentro del repositorio
principal, revisadas junto al código. Se publica una copia en la
pestaña Wiki de `ARSASWebDesign/NetArcaWs`, con portada, barra lateral y enlaces
entre páginas. Los ADR y contratos técnicos existentes conservarán su historial:
el wiki incluirá su contenido y procedencia, sin crear dos versiones editadas
independientemente. Definir y verificar un mecanismo reproducible de publicación
para que fuentes y wiki correspondan al mismo commit.

El espejo plano local se genera con `dotnet run --project tools/NetArcaWs.Build -- wiki` en
`docs/wiki-export/`; contiene 29 páginas de contenido más barra lateral y
manifiesto. Incluye las páginas temáticas y copia README, arquitectura, progreso,
ADR, guía de contribución, releases, CLI y referencias, convirtiendo enlaces
relativos a navegación entre páginas. El manifiesto declara las fuentes, la fecha
de snapshots de contratos (2026-10-06) y la revisión de origen se registra en `SOURCE_COMMIT` del repositorio wiki.
Se revisa junto a las fuentes antes de cada publicación. El mantenedor inicializó
la wiki; la publicación utiliza su repositorio Git dedicado.

## Fuentes de base e inventario

- [Repositorio y README de PyAfipWs](https://github.com/reingart/pyafipws).
- [Wiki original](https://github.com/reingart/pyafipws/wiki).
- [Manual PyAfipWs](https://www.sistemasagiles.com.ar/trac/wiki/ManualPyAfipWs),
  enlazado por el README original. Su contenido no pudo recuperarse desde esta
  consulta directa; también se identificó el [manual HTML del sitio original](https://www.sistemasagiles.com.ar/site/websites/documentacion_herramientas/manualpyafipws.html).
  Registrar la versión recuperada y revisar ambos puntos de entrada durante el hito.
- Documentación, WSDL y manuales vigentes de ARCA enlazados desde los contratos
  de cada módulo, para revisar información histórica del proyecto original.
- Todos los Markdown, ADR, contratos XML, ejemplos, configuración y guías de
  contribución que existan en NetArcaWs al ejecutar el hito.

Antes de escribir el wiki completo, elaborar un inventario página por página:
URL/ruta original, revisión o fecha consultada, tema, página destino NetArcaWs,
estado de adaptación, diferencias y atribución. Revisar también los enlaces a
manuales de cada servicio. La revisión inicial del índice upstream es del
2026-10-06 y no constituye ese inventario exhaustivo.

El upstream cubre más servicios y herramientas que los hitos 1 a 5. El inventario
no debe excluirlos silenciosamente: identificar todos los temas encontrados y
registrar cuáles tienen equivalente, cuáles son específicos de Python/COM y
cuáles siguen pendientes de portar. Cobertura documental no equivale a paridad
funcional; no presentar ejemplos como ejecutables si su API todavía no existe.

El catálogo también debe revisar las herramientas de PDF, formatos TXT/CSV/DBF/
XML/JSON, ejemplos de integración, interfaces COM/DLL, aplicaciones y módulos de
ERP que aparecen en el ecosistema upstream. Documentar su correspondencia o
situación pendiente no significa incorporarlos automáticamente a la entrega actual.

## Contenido obligatorio

| Área | Contenido a integrar/adaptar |
| --- | --- |
| Inicio | Alcance, estado real del port, compatibilidad, requisitos, instalación NuGet y primer uso |
| Migración desde Python | Equivalencias de clases/métodos/datos, cambios de errores, tipos, configuración y límites COM/CLI |
| Certificados y WSAA | Trámite ARCA, CSR, firma, TRA/TA, homologación/producción, vencimiento, caché y errores |
| Servicios de negocio | WSFEv1, WSFEXv1, WSMTXCA y Padrón A4/A5/A10/A13: cada operación implementada, DTOs, campos, ejemplos y errores |
| Catálogo upstream completo | Módulos restantes, herramientas y formatos: equivalencia .NET o declaración explícita de ausencia/pendiente |
| Reglas impositivas | Importes, alícuotas, redondeos, monedas, comprobantes, consultas y reconciliación, con fuentes y pruebas |
| Material en memoria | PEM/PFX/Base64, contraseñas, ownership, vault/base de datos, rotación y límites por plataforma |
| Multitenancy | Resolución autorizada del tenant, CUIT representada, certificados por tenant, aislamiento, concurrencia y entornos |
| Operación distribuida | Diferencia entre compartir secretos y compartir TA; coordinación entre réplicas según lo realmente implementado |
| Disponibilidad | Health checks opt-in por WS, ejemplos ASP.NET, tags, timeout, resultados y límites de los Dummy/WSDL |
| CLI .NET | Instalación global/local, cert-dev, cert-prod, cert-info, archivos, permisos, errores y ejemplos |
| Emisión segura | Idempotencia, identidad fiscal, diario durable, reintentos y resultados desconocidos; estado efectivo de implementación |
| Arquitectura | Criptografía y SOAP nativos, DI, HttpClient, seguridad de XML, decisiones y todos los ADR |
| Desarrollo | Compilar, probar, fixtures, homologación, CI, empaquetado/publicación y contribuciones |
| Referencia y soporte | API pública completa, configuración, diagnóstico, FAQ, glosario, fuentes, licencia y atribuciones |

Para WSMTXCA, verificar el alcance contra los manuales oficiales: no equiparar
el servicio de facturación con detalle con todo el circuito de Factura de Crédito
Electrónica MiPyME. Mantener las diferencias en la matriz de equivalencias.

La organización toma el alcance del [wiki upstream](https://github.com/reingart/pyafipws/wiki)
como punto de partida; la estructura de operación .NET y sus extras son propios.

## Criterios de cierre

- [ ] Inventario exhaustivo de documentación upstream y local con cada entrada resuelta.
- [x] Toda la documentación local incorporada al wiki, incluidos ADR y referencias.
- [ ] Cada servicio y operación implementada tiene guía, parámetros, ejemplo y manejo de errores.
- [ ] Cada tema upstream sin implementación equivalente está identificado, sin promesas de soporte.
- [ ] Todos los extras listados en el README tienen su página y estado verificable.
- [ ] Ejemplos C# compilados; ejemplos de credenciales ficticios y sin secretos reales.
- [x] Homologación real distinguida de tests simulados y de comprobaciones de infraestructura.
- [ ] Enlaces internos/externos, navegación, portada y barra lateral revisados.
- [ ] Información fiscal/operativa contrastada con fuentes oficiales vigentes, con fecha de revisión.
- [ ] Procedencia, autores y licencia del material adaptado registrados; verificar la licencia documental antes de copiar textos.
- [x] Publicación reproducible validada: fuentes `docs/wiki/` y wiki publicado corresponden a una revisión identificada.
- [x] README enlaza el wiki publicado y conserva inicio rápido, extras y límites actuales.
- [ ] Checklist y matriz de compatibilidad actualizados; revisión final antes de declarar completo este hito.

Este hito permanece al final del plan. Si aparecen módulos adicionales necesarios
para completar el port, sus hitos de implementación deben preceder al cierre del
wiki integral.

## Reproducir la publicación

1. Desde una revisión limpia del repositorio principal, ejecutar
   `dotnet run --project tools/NetArcaWs.Build -- wiki`; el generador valida destinos internos.
   Revisar y confirmar las fuentes y el mirror antes de continuar.
2. Clonar `git@github.com:ARSASWebDesign/NetArcaWs.wiki.git` en una carpeta
   externa al repositorio principal. Actualizar su rama antes de cada publicación.
3. Copiar los Markdown de `docs/wiki-export/` a la raíz del checkout wiki.
   Revisar conflictos con páginas editadas manualmente; conservar páginas ajenas
   al mirror. No reemplazar el historial ni usar push forzado.
4. Guardar el SHA completo de `git rev-parse HEAD` del repositorio principal en
   el archivo `SOURCE_COMMIT` de la wiki. Confirmar el cambio en la wiki indicando
   ese SHA en el mensaje y hacer push a su rama predeterminada.
5. Verificar el HEAD remoto y comparar cada archivo publicado con el mirror.
   La barra lateral y el manifiesto enumeran las páginas y sus fuentes originales.

El manifiesto permite reproducir el contenido de una revisión concreta del
repositorio principal; `SOURCE_COMMIT` pertenece exclusivamente al checkout wiki
para evitar referencias circulares en los commits del repositorio principal.
