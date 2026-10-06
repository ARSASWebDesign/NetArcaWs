# Progreso de NetArcaWs

- [x] Hito 1: WSAA nativo, NuGet LGPL-3.0-or-later, xUnit y FluentAssertions.
- [x] CLI .NET tool: cert-dev, cert-prod, cert-info, paquete y suite propia.
- [x] Certificados PEM/PFX en memoria desde configuración, vault o base de datos.
- [x] Contexto multitenant y aislamiento de caché WSAA por tenant/CUIT/entorno/servicio/certificado.
- [ ] Integrar y probar contexto multitenant en cada cliente de negocio de los hitos 2 a 5.
- [x] Health checks independientes y opt-in por WS, sin credenciales.
- [x] Diseño documentado de reintentos y reconciliación para evitar duplicados.
- [ ] Implementar y probar diario durable/reconciliación en los hitos de facturación.
- [ ] WSAA: ejecutar homologación real con certificados autorizados.
- [ ] Hito 2: WSFEv1 y pruebas.
- [ ] Hito 3: WSFEXv1 y pruebas.
- [ ] Hito 4: WSMTXCA y pruebas; precisar alcance oficial del servicio.
- [ ] Hito 5: Padrón A4/A5/A10/A13 y pruebas.
- [ ] Hito 6: README y arquitectura integrales de todos los módulos.

La documentación del Hito 1 ya está disponible en README.md y ARCHITECTURE.md.
El detalle de compatibilidad y verificación está en docs/plans/hito-1.md.
Pruebas locales con health checks, CLI, certificados en memoria y multitenancy: 118 aprobadas y 1 omitida por falta de credenciales de homologación.
La CLI tiene 31 pruebas. Los paquetes 0.4.0 se compilan para .NET 10.
La caché de tickets es local al proceso; compartir certificados entre réplicas no agrega caché distribuida.
Este estado no equivale al 100% del port del repositorio original.
