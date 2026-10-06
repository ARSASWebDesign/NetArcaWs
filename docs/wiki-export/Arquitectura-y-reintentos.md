<!-- Source: docs/wiki/Arquitectura-y-reintentos.md. Generated wiki mirror; edit the repository source. -->

# Arquitectura y reintentos

La biblioteca separa autenticación WSAA, contexto tenant, transporte SOAP,
aritmética decimal explícita, fachadas contractuales y diario fiscal.
`SafeInvoiceService` conecta autorización CAE unitaria y consultas de
reconciliación para WSFEv1, WSFEXv1 y WSMTXCA; Hitos 2–6 y la suite Release están
verificados. Cada operación usa el contexto tenant de la aplicación; los
health checks son de infraestructura y no consumen credenciales.

## Transporte común

`ISoapTransport.SendAsync<TRequest,TResponse>` define la frontera SOAP para los
adaptadores. `SoapTransport` implementa SOAP 1.1 sobre `HttpClient`, exige URI
HTTPS sin credenciales ni query/fragment, envía el `SOAPAction` solicitado y
valida un único elemento dentro del body. Las respuestas tienen un límite
configurable (`SoapTransportOptions.MaxResponseBytes`, 4 MiB por defecto; rango
1 KiB–32 MiB). La solicitud también tiene un límite de
`SoapTransportOptions.MaxRequestBytes` (4 MiB por defecto, rango 1 KiB–32 MiB)
que se aplica durante la serialización antes de enviar. El lector no permite DTD.
Un fault se expone como `SoapFaultException`
con código, razón, detalle y status HTTP. Se propaga la cancelación del caller y
un timeout se reporta como cancelación con `TimeoutException` interna. No hay
logs de payload, autenticación ni retry genérico. La implementación no intenta
traducir fallos SOAP a decisiones fiscales; eso corresponde al adaptador.

`IArcaTicketProvider`/`WsaaTicketProvider` resuelve el ticket mediante WSAA para
un servicio y `ArcaTenantContext`, de modo que el adaptador podrá construir la
envoltura de autenticación sin aceptar token, firma ni CUIT libre del llamador.

## Cálculo decimal disponible

`NetArcaWs.Taxation.TaxCalculator` ofrece redondeo `AwayFromZero` de cero a seis
decimales, IVA para una tasa provista, una descomposición básica de importe
bruto y suma de neto/IVA/exento/no gravado/tributos. La tasa y clasificación de
base deben provenir de reglas confiables que elija la aplicación; no hay
catálogo fiscal ni cálculo integral validado para WSFE, WSFEX o WSMTXCA. Vea
[Cálculos decimales](Calculos-decimales).

## Diario fiscal disponible y límites

`IInvoiceJournal` permite proveedores de persistencia alternativos. La
implementación `SqliteInvoiceJournal` guarda payload, hash SHA-256, identidad
fiscal, estado y leases/versiones; limita unicidad por ambiente+CUIT+pto de
venta+tipo+número aunque cambie tenant. `InvoiceCoordinator` permite preparar,
adquirir lease, ejecutar una autorización una vez y mantener una operación
incierta. `SafeInvoiceService` integra autorización y consulta exacta por
servicio. `ResumeAsync` envía solo un snapshot cuyo estado siga `Prepared`, para
recuperar una caída anterior al envío; cualquier otro estado se reconcilia y no
se vuelve a autorizar. Una ausencia no es permiso para reenvío.

El backend SQLite es duradero en archivo local y soporta varios procesos del
mismo host que abren ese archivo. No se debe ubicar en NFS ni compartir entre
hosts. Para réplicas multi-host, implemente `IInvoiceJournal` sobre una base
transaccional compartida con las mismas restricciones de identidad, idempotencia,
leases y fencing. Los payloads y respuestas son datos fiscales persistidos: el
operador debe controlar acceso, retención y protección del archivo/DB.

## Reintentos de emisión

Un timeout luego de iniciar un `POST` fiscal es un resultado incierto: el
proveedor pudo haber autorizado el comprobante aunque la respuesta no haya
llegado. No se debe asignar un número nuevo ni volver a emitir con otro payload.
NetArcaWs no aplica retry general a operaciones SOAP de escritura.

[ADR 0001](ADR-0001-safe-invoice-retries) documenta la operación de emisión
durable implementada: payload tipado e inmutable, restricción de identidad fiscal,
estado `Unknown`, reconciliación específica del servicio y revisión manual si la
evidencia no alcanza. El ADR distingue comportamientos distintos: WSFE consulta
número exacto, WSFEX puede repetir con el mismo `Cmp.Id`, WSMTXCA consulta
comprobante antes de reenvío. El código actual no reenvía una operación incierta.
Una corrección de rechazo confirmado queda registrada como revisión y requiere
una llamada explícita a `ResumeAsync` para enviar el nuevo snapshot preparado. No
hay garantía universal de exactamente una vez.

El coordinador, diario y adaptadores por servicio están disponibles y pasaron la
suite local. La build Release tuvo 0 warnings y 0 errores. No aplicar retries de
transporte como sustituto. La aplicación inicia la reconciliación explícitamente;
no existe worker o backoff integrado. La homologación de negocio autenticada
sigue pendiente de certificados autorizados.

## Decisiones relacionadas

- [ADR 0001 — reintentos seguros y diario fiscal](ADR-0001-safe-invoice-retries)
- [ADR 0002 — autorización/identidad tenant](ADR-0002-arca-tenant-context)
- [ADR 0003 — certificados y rotación en memoria](ADR-0003-in-memory-certificates)
- [ADR 0004 — contratos SOAP fieles a WSDL](ADR-0004-public-soap-contracts)
- [Arquitectura actual](Arquitectura-general)
- [Plan de hitos restantes](Plan-remaining-milestones)
