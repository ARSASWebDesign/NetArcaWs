<!-- Source: docs/wiki/Diagnostico-y-glosario.md. Generated wiki mirror; edit the repository source. -->

# Diagnóstico, preguntas frecuentes y glosario

Esta guía describe el comportamiento de NetArcaWs. Los códigos de validación
fiscal dependen del servicio, la operación y la versión del manual ARCA; no son
una enumeración estable común a todos los web services.

## Diagnosticar una llamada

| Resultado observado | Interpretación y siguiente paso |
| --- | --- |
| Error de configuración, certificado o serialización antes del envío | Corregir la configuración o los datos; el transporte no debe usarse para adivinar valores fiscales |
| `WsaaSoapException` | WSAA rechazó la autenticación; revisar `FaultCode`, `FaultString`, entorno, servicio y habilitaciones |
| `SoapFaultException` | SOAP devolvió un fault, incluso con HTTP 200; inspeccionar `Code`, `Reason` y `StatusCode` |
| Respuesta tipada con errores/observaciones de negocio | La llamada HTTP terminó, pero eso no acredita autorización; interpretar los campos de resultado del servicio |
| Timeout, cancelación o conexión perdida durante una autorización | El resultado remoto puede ser incierto; conservar la identidad fiscal y reconciliar, sin volver a emitir automáticamente |
| `InvoiceConflictException` | El diario detectó incompatibilidad de identidad, payload, versión o serie; revisar el registro antes de modificar la operación |
| Health check saludable | El probe respondió; no demuestra que el certificado esté autorizado ni que una factura será aceptada |

Registrar el servicio, ambiente, método, duración y un identificador interno de
correlación. Token, Sign, claves privadas y XML fiscal completo no deben aparecer
en logs ordinarios. `Detail` de una excepción puede incluir datos del servicio;
revisarlo antes de compartirlo en un issue público.

## Preguntas frecuentes

### ¿El CLI crea un certificado aceptado por ARCA?

Genera una clave privada y un CSR. La emisión y asociación al servicio se hacen
en ARCA: WSASS para homologación; Administrador de Certificados Digitales y
Administrador de Relaciones para producción. Un CSR no es un certificado
habilitado. [Procedimiento oficial de WSAA](https://www.afip.gob.ar/ws/documentacion/wsaa.asp).

### ¿Puedo guardar el certificado en un vault o una base de datos?

Sí. `WsaaCertificateContent` admite PEM o PFX en memoria. Mantener el certificado
y la clave correspondientes, proteger la contraseña y definir la rotación en
la aplicación. No es necesario escribir un archivo binario intermedio. Ver
[certificados en memoria](Decisi%C3%B3n-3-Certificados-en-memoria).

### ¿Compartir el certificado comparte la caché de tickets?

No. La caché incluida es local al proceso y separa tenant, CUIT, ambiente,
servicio y certificado. Dos procesos pueden pedir tickets para la misma
identidad remota. La aplicación debe coordinar esa situación si despliega
réplicas; no hay un proveedor de caché distribuida incorporado.

### ¿El ticket siempre se reutiliza durante doce horas?

La expiración efectiva viene del TA recibido. El cliente debe respetarla en
lugar de asumir que cualquier respuesta dura doce horas. El tiempo solicitado
en el TRA y el margen de reloj no sustituyen la expiración concedida por WSAA.
Ver [WSAA y certificados](WSAA-y-certificados).

### ¿Puedo cambiar de tenant modificando opciones globales?

Usar un `ArcaTenantContext` por operación. El host debe resolver y autorizar
tenant, CUIT y material criptográfico desde un origen confiable. Los wrappers
inyectan Token, Sign y CUIT a partir de ese contexto, sin modificar el DTO del
llamador. Ver [multitenancy](Contexto-multitenant).

### ¿Por qué una propiedad opcional en XSD es obligatoria para mi factura?

`minOccurs=0` expresa una regla de serialización del contrato, no todas las
condiciones fiscales. Concepto, moneda, tipo de comprobante y operación pueden
imponer requisitos adicionales. Consultar los catálogos del servicio y su manual;
no fijar tasas o identificadores tomando valores de otro servicio.

### ¿Una factura rechazada se puede corregir?

Una operación durable en estado `Rejected` admite una revisión explícita con
`ReviseRejectedAsync`, control de versión e historial. La revisión conserva la
identidad fiscal y queda `Prepared`; `ResumeAsync` efectúa el envío explícito.
`Unknown` y `Conflict` requieren reconciliación o revisión, no una nueva factura.
Ver [diario fiscal](Diario-fiscal).

### ¿El diario SQLite sirve para varias réplicas en hosts distintos?

El proveedor incluido admite procesos del mismo host usando un archivo local.
No compartirlo mediante NFS. Para varios hosts, implementar `IInvoiceJournal`
sobre una base transaccional común, preservando unicidad fiscal, comparación de
versiones, leases y snapshots. Compartir únicamente el certificado no resuelve
la coordinación de emisiones.

### ¿WSMTXCA cubre todo el circuito MiPyME?

No. El cliente cubre las operaciones del contrato WSMTXCA de factura con detalle.
El circuito completo de Factura de Crédito Electrónica MiPyME incluye servicios
y eventos distintos. La matriz de [servicios y cobertura](Servicios-y-cobertura)
explicita esta diferencia.

### ¿Por qué NuGet y la rama principal pueden mostrar versiones distintas?

NuGet solo muestra paquetes que ya fueron aceptados e indexados. La release
0.6.0 agrega paquetes opcionales de persistencia; usá los badges de [Inicio](Home)
para comprobar las versiones disponibles. Las mejoras posteriores se
distribuyen con otra versión, sin reemplazar assets de una release publicada.

## Glosario

| Término | Significado en esta biblioteca |
| --- | --- |
| ARCA / AFIP | Organismo actual y denominación histórica que sigue apareciendo en hosts y contratos |
| WSAA | Servicio de autenticación que intercambia el TRA firmado por un ticket de acceso |
| TRA | Solicitud XML de acceso firmada como CMS con la clave del certificado |
| TA | Respuesta de WSAA con credenciales y período de validez |
| Token / Sign | Credenciales del TA enviadas al servicio de negocio; no son la clave privada |
| CMS | Contenedor criptográfico de la firma del TRA |
| CSR | Solicitud de certificado; contiene la clave pública, no la clave privada |
| PEM / PFX | Formatos de representación o contenedor del certificado y, cuando corresponde, la clave |
| CUIT representada | Identidad fiscal utilizada para operar; su autorización puede provenir de una delegación |
| Tenant | Partición interna de la aplicación; no crea por sí misma una identidad o autorización ARCA |
| CAE / CAEA | Modalidades de autorización fiscal distintas, con operaciones específicas por servicio |
| Identidad fiscal | Ambiente, CUIT emisora, punto de venta, tipo y número del comprobante |
| Idempotencia | Reutilización controlada de una misma operación local; no una garantía universal de ejecución única remota |
| Lease / fencing | Reserva temporal y versión que impiden que un proceso obsoleto sobrescriba el estado del diario |
| Reconciliación | Consulta del estado remoto y comparación con el snapshot local, sin volver a autorizar automáticamente |
| SOAP fault | Error del protocolo SOAP, distinto de los errores fiscales incluidos en una respuesta válida |
| WSDL / XSD | Contratos del servicio y esquemas de datos; no reemplazan las reglas condicionales del manual |

Fecha de revisión: 2026-10-06. Los nombres de API y límites se contrastaron con
el código versionado; las gestiones de certificados con la página oficial
enlazada. Esta guía no declara homologación fiscal autenticada.
