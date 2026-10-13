<!-- Source: docs/release-notes-0.7.0.md. Generated wiki mirror; edit the repository source. -->

# NetArcaWs 0.7.0

## Cambios

- Store opt-in de certificados WSAA versionados y cifrados, con rotación compare-and-swap e historial de cambios. La aplicación mantiene la autorización, administra el keyring y fija el `VersionId` por operación; el store no renueva certificados ni publica rotaciones. Ver [Certificados multitenant](Certificados-multitenant) y [ADR 0007](ADR-0007-tenant-certificate-store).
- Cola durable y worker opt-in para recuperar autorizaciones CAE unitarias de WSFEv1, WSFEXv1 y WSMTXCA. Requiere optar explícitamente por `AddInvoiceRecovery` junto con `AddInvoicing`, implementar `IInvoiceRecoveryContextResolver` y aplicar expresamente el paquete de migraciones del motor. Se conservan los historiales existentes; esquemas sin seguimiento o inconsistentes fallan cerrados. La aplicación decide cuándo programar la recuperación. SQLite admite procesos del mismo host sobre archivo local; no es almacenamiento multi-host. Ver [Diario fiscal](Diario-fiscal).
- Adjudicación explícita y auditable, con el mismo número e Id, de los rechazos WSFEX 1550 y 1673 bajo sus validadores y allowlists acotados; no permite reclasificar otros rechazos. La capability está disponible solo en `SqliteInvoiceJournal`; providers EF y stores personalizados sin ella fallan cerrados. Ver [ADR de emisión durable](Decisi%C3%B3n-1-Emisi%C3%B3n-y-reintentos-seguros).
- Flujo protegido de homologación con nueve selectores autenticados y 18 escenarios fiscales tipados para el modo `completa`.

## Evidencia

Sobre `960266563f15c43425bdca20ccdcabf68fb2f2e7`, la ejecución protegida [38327713674](https://github.com/ARSASWebDesign/NetArcaWs/actions/runs/38327713674) terminó correctamente en modo `completa`. Ejecutó los nueve selectores y los 18 escenarios previstos. CI [38327702696](https://github.com/ARSASWebDesign/NetArcaWs/actions/runs/38327702696) y Security [38327702694](https://github.com/ARSASWebDesign/NetArcaWs/actions/runs/38327702694) también finalizaron correctamente sobre ese commit. Esto acredita los flujos ejecutados; no representa homologación de toda la biblioteca ni de todas las operaciones ARCA. El listado de puntos de venta de WSFE puede ser parcial si ARCA devuelve el error 602; la ejecución de los escenarios no convierte esa respuesta en evidencia de catálogo completo.
