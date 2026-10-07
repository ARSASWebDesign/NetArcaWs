# Funcionalidades adicionales

Además de los clientes fiscales, NetArcaWs incluye piezas de infraestructura y
herramientas para integrarlas en una aplicación .NET. Cada función resuelve una
parte acotada del flujo; no reemplaza la autorización del usuario, las reglas
fiscales ni las decisiones de operación del sistema consumidor.

| Función | Para qué sirve y cómo usarla | Guía y ejemplo |
|---|---|---|
| **Contexto multitenant** | Vincula cada llamada autenticada con un tenant interno, CUIT representada, ambiente y certificado. La aplicación debe resolver y autorizar estos datos antes de crear el contexto; no los tomes de campos libres del request. | [Contexto multitenant](Contexto-multitenant.md), `GuideExamples.CreateTenant` y `GuideExamples.AuthenticateTenantAsync` en [GuideExamples.cs](../../examples/NetArcaWs.Examples/GuideExamples.cs). |
| **Certificados en memoria** | `WsaaCertificateContent` recibe PEM, bytes PFX o PFX en Base64 para WSAA sin exigir una ruta de archivo. Base64 no cifra el contenido; carga secretos desde un vault o almacenamiento protegido. En macOS, usa PEM porque la importación PFX efímera no está disponible en .NET. | [WSAA y certificados](WSAA-y-certificados.md), [contexto multitenant](Contexto-multitenant.md). |
| **Health checks** | Registros opt-in implementan `IHealthCheck` para probes por servicio/ambiente. No requieren certificado ni ticket WSAA, no reintentan y miden conectividad/disponibilidad; un resultado saludable no prueba autorización de una CUIT o aceptación de una factura. | [Health checks](Health-checks.md), `GuideExamples.ConfigureHealthChecks` en [GuideExamples.cs](../../examples/NetArcaWs.Examples/GuideExamples.cs), contratos en [healthchecks-contracts.md](../reference/healthchecks-contracts.md). |
| **CLI de certificados** | `netarcaws cert-dev` y `cert-prod` generan clave y CSR localmente; `cert-info` inspecciona metadatos y correspondencia local de clave/certificado. Ningún comando emite un certificado, lo asocia a un servicio o llama a ARCA. | [Herramienta de certificados](Herramienta-de-certificados.md), [guía detallada del CLI](../certificates-cli.md), `GuideExamples.CreateDevelopmentCertificateCommand` en [GuideExamples.cs](../../examples/NetArcaWs.Examples/GuideExamples.cs). |
| **Emisión durable y reconciliación** | `SafeInvoiceService` registra operaciones CAE unitarias de WSFEv1, WSFEXv1 y WSMTXCA con payload inmutable y diario. Ante una caída durante un envío, reconcilia el estado según el servicio; no reenvía automáticamente un resultado incierto. SQLite es local a un host; un despliegue multi-host necesita un diario transaccional propio. | [Diario fiscal](Diario-fiscal.md), [arquitectura y reintentos](Arquitectura-y-reintentos.md), ADR [reintentos seguros](../adr/0001-safe-invoice-retries.md), métodos `AuthorizeOneAsync` y `ReconcileAsync` en [GuideExamples.cs](../../examples/NetArcaWs.Examples/GuideExamples.cs). |
| **Cálculos decimales** | `TaxCalculator` ofrece redondeo decimal, IVA con tasa provista, descomposición simple de bruto y suma de componentes. La aplicación decide clasificación, tasa y reglas fiscales; esto no es un motor tributario integral. | [Cálculos decimales](Calculos-decimales.md), `GuideExamples.CalculateFromGross` en [GuideExamples.cs](../../examples/NetArcaWs.Examples/GuideExamples.cs). |
| **Herramientas de build .NET** | `NetArcaWs.Build` automatiza la generación del espejo Markdown de la wiki, la actualización de contratos mediante la herramienta configurada y verificaciones/operaciones de release. Su entrada es el repositorio .NET; no requiere un script de Python para estas tareas. | [Desarrollo y contribución](Desarrollo-y-contribucion.md), código en [`tools/NetArcaWs.Build`](../../tools/NetArcaWs.Build/Program.cs). |
| **Ejemplos .NET** | El proyecto reúne ejemplos compilables. Compilarlo no hace llamadas de red; al invocar `AuthenticateTenantAsync`, `AuthorizeOneAsync`, `ReconcileAsync` o un adaptador de operación sí puede comunicarse con ARCA. Los datos y reglas del negocio siguen a cargo de la aplicación. | [GuideExamples.cs](../../examples/NetArcaWs.Examples/GuideExamples.cs), [proyecto de ejemplos](../../examples/NetArcaWs.Examples/NetArcaWs.Examples.csproj). |
| **Paquetes y publicación** | Los once paquetes NuGet de la biblioteca, la herramienta y los extras de persistencia se publican mediante la release validada y Trusted Publishing OIDC. La validación manual genera artefactos sin publicarlos; el job de publicación requiere la configuración externa documentada. | [Releases y publicación](../releases.md), [desarrollo y contribución](Desarrollo-y-contribucion.md). |

## Cuidados al combinar funciones

1. Autentica al usuario y autoriza su acceso al tenant antes de consultar un
   padrón o emitir un comprobante. `ArcaTenantContext` organiza datos fiscales
   de la llamada, pero no otorga permisos por sí mismo.
2. Protege claves, contraseñas, tickets y datos fiscales según las políticas de
   la aplicación. Los objetos en memoria reducen archivos temporales explícitos,
   pero no garantizan el borrado inmediato de strings o memoria administrada.
3. Usa los helpers aritméticos solo después de aplicar reglas fiscales
   verificadas. No recalcules ni cambies silenciosamente los importes de una
   solicitud durante una reconciliación.
4. Trata los health checks como señales operativas. Conserva pruebas separadas
   para credenciales, permisos y resultados de negocio en un ambiente ARCA
   autorizado.

El [inicio rápido](Inicio-rapido.md) muestra el primer registro DI. La página de
[servicios implementados](Servicios-implementados.md) resume los clientes y sus
referencias de operación; el [inventario upstream](../reference/upstream-inventory.md)
aclara las herramientas de PyAfipWs que aún no tienen equivalente.
