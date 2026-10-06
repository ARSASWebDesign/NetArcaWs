<!-- Source: docs/wiki/Diario-fiscal.md. Generated wiki mirror; edit the repository source. -->

# Diario fiscal, emisión y reconciliación

`SafeInvoiceService` compone las fachadas SOAP tipadas con el diario y aplica
una política conservadora para solicitudes CAE unitarias de WSFEv1, WSFEXv1 y
WSMTXCA. `InvoiceCoordinator` controla preparación, lease, fencing y persistencia
del resultado. No reserva la numeración del contribuyente ni promete una
ejecución exactly-once frente a ARCA.

## Registro en DI

Para SQLite local:

```csharp
services.AddNetArcaWsSqliteInvoicing(databasePath);
```

Esto registra `SqliteInvoiceJournal`, WSAA, transporte y fachadas, además de
`InvoiceCoordinator` y `SafeInvoiceService`. Si la aplicación ya implementa un
diario transaccional compartido, registrar `IInvoiceJournal` y luego:

```csharp
services.AddSingleton<IInvoiceJournal, MyTransactionalInvoiceJournal>();
services.AddNetArcaWsInvoicing();
```

La extensión general configura WSAA y servicios. El proveedor elegido debe
garantizar idempotencia por tenant/clave, unicidad fiscal entre tenants/servicios,
bloqueo de serie y fencing equivalentes al contrato de `IInvoiceJournal`.

## Autorizar un comprobante

Las APIs reciben el contexto confiable, una clave de idempotencia estable de la
aplicación y un request tipado público:

```csharp
var invoices = provider.GetRequiredService<SafeInvoiceService>();

InvoiceOperation wsfe = await invoices.AuthorizeWsfeAsync(
    tenant, idempotencyKey, fecaeRequest, cancellationToken);
```

Para los otros contratos, la forma es `AuthorizeWsfexAsync(tenant, key,
ClsFexRequest, cancellationToken)` y `AuthorizeWsmtxcaAsync(tenant, key,
ComprobanteType, cancellationToken)`. Cada ejemplo corresponde a una llamada
alternativa: no reutilizar una misma clave de idempotencia para requests distintos.

Construir `tenant` después de resolver/autenticar la autorización de la
aplicación, no con campos libres del request entrante. El número de comprobante
lo asigna y entrega la aplicación dentro del DTO: la biblioteca no consulta el
último número para asignar el siguiente. La misma clave y el mismo payload
devuelven la operación existente sin reenvío; reutilizar la clave con contenido
distinto, o usar una identidad fiscal ocupada, produce conflicto.

Restricciones del orquestador actual:

- WSFEv1 exige `CantReg = 1`, un detalle, un único número (`CbteDesde = CbteHasta`),
  fecha y cotización explícitas. La interfaz low-level sigue permitiendo los
  contratos batch, pero este flujo durable no procesa lotes.
- WSFEXv1 exige `Cmp.Id` positivo, fecha y cotización explícitas.
- WSMTXCA exige fecha de emisión y cotización y rechaza requests con campos de
  autorización ya presentes.

La respuesta de autorización se compara con el tenant/identidad y con los campos
fiscales correlacionables del request. Solo una respuesta completa y correlacionada
con CAE válido se registra como `Authorized`. `Rejected` requiere el detalle de
rechazo correlacionado y sin CAE. XML ausente, campos incompletos, diferencia de
payload o resultado contradictorio no se tratan como rechazo: el resultado queda
`Unknown` o `Conflict`. Un error de transporte se persiste como `Unknown` y se
propaga al caller.

WSMTXCA tiene una forma de rechazo propia: resultado `R`, errores no vacíos y
ausencia de `ComprobanteResponse` es un rechazo de negocio documentado. Los
resultados `A`/`O` solo se aceptan como autorización si traen un comprobante
correlacionado y CAE válido; no se interpreta ausencia del objeto como éxito.

## Recuperar y reconciliar

```csharp
IInvoiceJournal journal = provider.GetRequiredService<IInvoiceJournal>();
var pending = await journal.ListPendingAsync(tenant.TenantId,
    cancellationToken: cancellationToken);
```

`ListPendingAsync` devuelve `Prepared`, `Unknown`, `Conflict` y `ManualReview`,
además de operaciones `Submitting`/`Reconciling` con lease vencida. Se filtra por
tenant y el límite predeterminado es 100 (máximo 1000). La aplicación inicia la
recuperación; no hay worker alojado, scheduler, backoff, jitter ni notificación
automáticos.

Use `ResumeAsync` para que el diario decida la acción según el estado guardado:

```csharp
foreach (InvoiceOperation item in pending)
{
    InvoiceOperation current = await invoices.ResumeAsync(
        tenant, item.Submission.IdempotencyKey, cancellationToken);
}
```

`ResumeAsync` vuelve a enviar únicamente una operación `Prepared`, usando el
payload fiscal inmutable ya almacenado. Comprueba CUIT y ambiente del contexto.
Para cualquier otro estado delega en `ReconcileAsync`, que consulta el servicio
asociado y no llama al método de autorización. Así se recupera una caída ocurrida
después de preparar y antes del envío sin permitir que la aplicación cambie el
request congelado. Un resultado incierto no es un permiso para reintentar.

`ReconcileAsync` usa la consulta exacta del servicio asociado a la operación y
compara el resultado remoto con una proyección de todos los campos fiscales
comparables. Una respuesta que no encuentra el comprobante, incompleta o que no
puede correlacionarse conserva `Unknown`; diferencias fiscales terminan en
`Conflict`. La reconciliación no puede clasificar una respuesta negativa como
ausencia segura ni volver a llamar el método de autorización, incluso si el
manual describe un reenvío seguro. Solo una coincidencia positiva y autorizada
puede resolver a `Authorized`. La repetición de la propia llamada
`ReconcileAsync` es una nueva consulta, no una nueva emisión.

Si ARCA respondió pero falla el guardado local, el coordinador intenta conservar
el XML de respuesta como evidencia en estado `Unknown` y propaga el error
original. Si también falla esa escritura, la operación sigue con su lease
durable; una vez vencida, `ResumeAsync` consulta el comprobante antes de resolver
el resultado. Si el commit de `Authorized` se realizó pero se perdió su acuse,
el fencing impide degradarlo a `Unknown`: la recuperación devuelve el resultado
guardado. Ninguno de estos caminos vuelve a emitir la autorización. La evidencia
no puede garantizarse si el almacenamiento permanece inaccesible.

Antes de consultar o devolver un resultado ya terminal, `ReconcileAsync` comprueba
que tenant, CUIT representada, ambiente y versión de canonicalización coincidan
con el registro. El contexto incorrecto no revela ni devuelve la operación.

Una respuesta de rechazo correlacionada sí puede corregirse mediante una
revisión explícita. `ReviseRejectedAsync(tenant, key, expectedVersion,
replacement)` tiene sobrecargas para `FecaeRequest`, `ClsFexRequest` y
`ComprobanteType`. Solo admite una operación confirmada como `Rejected` y usa
control de versión para evitar corregir una versión que cambió. El diario guarda
el snapshot rechazado como revisión inmutable, preserva clave de idempotencia e
identidad fiscal y prepara el nuevo payload; esta llamada no contacta ARCA.
`ListRevisionsAsync(tenantId, key)` permite consultar el historial. Tras revisar
la corrección, la aplicación llama explícitamente a `ResumeAsync(tenant, key)`
para enviar el nuevo snapshot `Prepared`.

El diario bloquea preparar otra operación de la serie `(ambiente, CUIT, punto de
venta, tipo)` si existe una operación no terminal. La serie se libera cuando
la anterior queda `Authorized` o `Rejected`; `Unknown`, `Conflict` y
`ManualReview` requieren intervención/revisión. La unicidad de comprobante es
global por `(ambiente, CUIT, punto de venta, tipo, número)`, sin `tenantId` ni
servicio. Los tenants limitan acceso y visibilidad; no habilitan duplicar un
comprobante para la misma CUIT.

## Almacenamiento y garantías

`SqliteInvoiceJournal` permite varios procesos del mismo host que comparten el
mismo archivo SQLite local. No usar ese archivo en NFS ni compartirlo entre
hosts. Para réplicas multi-host, implementar `IInvoiceJournal` sobre una base
transaccional común. El diario guarda requests y respuestas fiscales, que pueden
contener datos personales: asegurar acceso, backups y retención de la base, y no
volcar payloads a logs.

### Entity Framework Core opcional

El paquete `NetArcaWs.EntityFrameworkCore` integra el diario con un contexto EF
del consumidor; el paquete principal no depende de EF. La configuración fija
qué servicios de factura y qué servicios autenticados tendrán tickets
compartidos. Se puede habilitar uno o ambos módulos:

```csharp
NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(options =>
    options.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)
        .AddWsaaTickets(ArcaService.Wsfev1));

services.AddDbContextFactory<MyApplicationDbContext>(options => options.UseSqlite(connectionString));
services.AddNetArcaWsEntityFrameworkStores<MyApplicationDbContext>(modelOptions);
```

`OnModelCreating` del contexto agrega `modelBuilder.AddNetArcaWs(modelOptions)`.
Usá la misma instancia de opciones para el modelo y los stores. El contexto
consumidor debe registrar `IDbContextFactory<TContext>`; la aplicación conserva
la propiedad de las migraciones y del despliegue del esquema. La selección puede
ser solo `AddInvoicing(...)`, solo `AddWsaaTickets(...)` o incluir ambas. El
diario acepta WSFEv1, WSFEXv1 y WSMTXCA; los tickets admiten servicios
autenticados explícitos.

Para `AddWsaaTickets(...)`, registrá `IWsaaTicketProtector` antes de
`AddNetArcaWsEntityFrameworkStores` y compartí el mismo keyring entre las
réplicas. También registrá `AddNetArcaWs()` para WSAA; si seleccionaste el
diario, `AddNetArcaWsInvoicing()` registra WSAA, las fachadas y
`SafeInvoiceService`. No guardes las claves del protector en la misma base que
los tickets.

Para el helper MySQL/MariaDB, agregar el paquete separado
`NetArcaWs.EntityFrameworkCore.MySql` y pasar la versión del servidor de forma
explícita:

```csharp
services.AddNetArcaWsMySqlStores(
    connectionString,
    new MySqlServerVersion(new Version(8, 4, 11)),
    modelOptions);
```

Para MariaDB se usa `MariaDbServerVersion`. Los paquetes
`NetArcaWs.EntityFrameworkCore.PostgreSql` y
`NetArcaWs.EntityFrameworkCore.SqlServer` exponen
`AddNetArcaWsPostgreSqlStores(connectionString, modelOptions)` y
`AddNetArcaWsSqlServerStores(connectionString, modelOptions)`. Cada helper
configura el contexto dedicado y los stores, pero no conecta, migra, crea tablas
ni agrega estrategia de reintentos EF o reintentos SOAP. Los cuatro paquetes
opcionales de persistencia están en versión 0.5.0 y aún no se publicaron en
NuGet; consultar el [issue #19](https://github.com/ARSASWebDesign/NetArcaWs/issues/19)
para su estado de entrega. El [modelo relacional](Modelo-relacional) contiene
el diccionario de columnas y los scripts DDL por proveedor. El cifrado de tickets requiere que la aplicación
registre `IWsaaTicketProtector` con claves administradas fuera de la base de
datos; ver el [ADR 0006](ADR-0006-shared-wsaa-tickets).

La suite normal usa motores y datos sintéticos; los tests opt-in aprobaron 5/5
con MySQL 8.4.11, 5/5 con MariaDB 11.4.13, 13/13 con PostgreSQL 17.6 y 13/13
con SQL Server Developer 16.0.4295.3, incluidos casos entre procesos. Son
versiones y escenarios concretos; no demuestran compatibilidad universal ni
homologación con ARCA. PostgreSQL y SQL Server corrieron en el
[job público de CI](https://github.com/ARSASWebDesign/NetArcaWs/actions/runs/37525573878)
en runner x64.

El comportamiento es conservador, no exactamente una vez: una caída externa,
una operación realizada fuera del diario, pérdida de storage o datos remotos no
comparables pueden requerir revisión humana. No hay asignación automática de
número, repetición automática de autorizaciones, reenvío automático WSFEX,
soporte de lote durable por item ni política de retry general. Una corrección
requiere registrar una revisión y reanudarla explícitamente. WSMTXCA implementa
su contrato de factura electrónica con detalle y CAE/CAEA, no el ciclo completo
de Factura de Crédito Electrónica MiPyME.

La suite Release y los contratos locales están verificados; no inferir aceptación
fiscal autenticada a partir de estas pruebas. Ver [ADR 0001](Decisi%C3%B3n-1-Emisi%C3%B3n-y-reintentos-seguros)
para las decisiones, los límites y la evolución pendiente.
