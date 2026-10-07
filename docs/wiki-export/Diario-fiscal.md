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
tenant y el límite predeterminado es 100 (máximo 1000). En el paquete core la
aplicación inicia la recuperación directamente. El worker opcional EF descrito
más abajo agrega polling y backoff para la cola durable.

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
        .AddInvoiceRecovery(ArcaService.Wsfev1)
        .AddWsaaTickets(ArcaService.Wsfev1));

services.AddDbContextFactory<MyApplicationDbContext>(options => options.UseSqlite(connectionString));
services.AddNetArcaWsEntityFrameworkStores<MyApplicationDbContext>(modelOptions);
```

`OnModelCreating` del contexto agrega `modelBuilder.AddNetArcaWs(modelOptions)`.
Usá la misma instancia de opciones para el modelo y los stores. El contexto
consumidor debe registrar `IDbContextFactory<TContext>`; la aplicación conserva
la propiedad de las migraciones y del despliegue del esquema. La selección puede
ser solo `AddInvoicing(...)`, solo `AddWsaaTickets(...)`, o incluir los módulos
adicionales requeridos. `AddInvoiceRecovery(...)` requiere el mismo servicio
dentro de `AddInvoicing(...)`. El diario acepta WSFEv1, WSFEXv1 y WSMTXCA; los
tickets admiten servicios autenticados explícitos.

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
ni agrega estrategia de reintentos EF o reintentos SOAP. Los paquetes
opcionales de persistencia usan versión 0.6.0: el paquete EF Core, tres
providers relacionales y cinco paquetes de migraciones oficiales por módulo y
motor. Consultar el [issue #19](https://github.com/ARSASWebDesign/NetArcaWs/issues/19)
para el estado de entrega. El [modelo relacional](Modelo-relacional) contiene
el diccionario de columnas y los scripts DDL por proveedor. El cifrado de tickets requiere que la aplicación
registre `IWsaaTicketProtector` con claves administradas fuera de la base de
datos; ver el [ADR 0006](ADR-0006-shared-wsaa-tickets).

## Cola y worker de recuperación durable (EF opcional)

El módulo `InvoiceRecovery` agrega una tabla de metadatos de cola al diario EF,
sin copiar su payload fiscal. Seleccioná cada servicio dos veces, como servicio
de facturación y como servicio de recuperación. El siguiente ejemplo usa SQLite
para el registro operativo local; no migra ni conecta durante la configuración:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.EntityFrameworkCore;
using NetArcaWs.EntityFrameworkCore.Migrations;
using NetArcaWs.EntityFrameworkCore.Migrations.Sqlite;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;

static IServiceCollection ConfigureRecoveryWorker(
    IServiceCollection services, string sqliteConnectionString, string authorizedTenantId)
{
    NetArcaWsModelOptions recoveryModel = NetArcaWsModelOptions.Configure(options =>
        options.AddInvoicing(ArcaService.Wsfev1)
            .AddInvoiceRecovery(ArcaService.Wsfev1));

    services.AddDbContextFactory<ArcaWsDbContext>(options =>
        options.UseSqlite(sqliteConnectionString));
    services.AddNetArcaWsEntityFrameworkStores<ArcaWsDbContext>(recoveryModel);
    services.AddNetArcaWsInvoicing();
    services.AddScoped<IArcaRecoveryAuthorization, ApplicationArcaRecoveryAuthorization>();
    services.AddScoped<IInvoiceRecoveryContextResolver, AuthorizedRecoveryContextResolver>();
    services.AddNetArcaWsInvoiceRecoveryWorker(
        new InvoiceRecoveryScope(authorizedTenantId, [ArcaService.Wsfev1]),
        options =>
        {
            options.LeaseDuration = TimeSpan.FromMinutes(5);
            options.IdleInterval = TimeSpan.FromSeconds(5);
            options.MaxAttempts = 10;
            options.BaseBackoff = TimeSpan.FromSeconds(2);
            options.MaxBackoff = TimeSpan.FromMinutes(5);
            options.JitterRatio = 0.2;
        });
    return services;
}
```

El host debe implementar su resolver con la autorización propia de la aplicación.
El contrato recibe `InvoiceRecoveryWorkItem` y `CancellationToken`, y devuelve
`ValueTask<ArcaTenantContext>`. Use `workItem.TenantId`, la CUIT y ambiente de
`workItem.Identity` y `workItem.CredentialReference` para resolver la versión
exacta guardada. Si la credencial fijada fue revocada, eliminada o ya no está
autorizada, rechace la operación; no sustituya silenciosamente una credencial
nueva. El processor vuelve a comprobar tenant, CUIT, ambiente, servicio y fence
después de resolver el contexto, justo antes de llamar a `SafeInvoiceService`.
El contexto no constituye autorización de usuario.

Este patrón completo compila al implementar el origen de autorización con el
modelo de identidad/credenciales de tu aplicación:

```csharp
public interface IArcaRecoveryAuthorization
{
    ValueTask<ArcaTenantContext> ResolveAuthorizedContextAsync(
        string tenantId, long representedCuit, ArcaEnvironment environment,
        string credentialReference, CancellationToken cancellationToken);
}

public sealed class AuthorizedRecoveryContextResolver(
    IArcaRecoveryAuthorization authorization) : IInvoiceRecoveryContextResolver
{
    public ValueTask<ArcaTenantContext> ResolveAsync(
        InvoiceRecoveryWorkItem workItem, CancellationToken cancellationToken = default)
    {
        string credentialReference = workItem.CredentialReference
            ?? throw new InvalidOperationException("This host requires a pinned credential version.");
        return authorization.ResolveAuthorizedContextAsync(
            workItem.TenantId, workItem.Identity.Cuit, workItem.Identity.Environment,
            credentialReference, cancellationToken);
    }
}

// ApplicationArcaRecoveryAuthorization is implemented by the host. Its method
// authenticates the tenant and retrieves the exact version named by credentialReference.
```

Al preparar, primero construí el mismo snapshot tipado que utiliza la emisión
normal, luego encolalo en una sola transacción junto con factura y reserva de
serie:

```csharp
static Task<InvoiceOperation> PrepareWsfeRecoveryAsync(
    IInvoiceRecoveryQueue queue,
    ArcaTenantContext authorizedTenantContext,
    string idempotencyKey,
    FecaeRequest fecaeRequest,
    Guid credentialVersionId,
    CancellationToken cancellationToken)
{
    InvoiceSubmission snapshot = SafeInvoiceService.CreateWsfeSubmission(
        authorizedTenantContext, idempotencyKey, fecaeRequest);
    string credentialReference = credentialVersionId.ToString("D");
    return queue.PrepareAndEnqueueAsync(snapshot, credentialReference, cancellationToken);
}
```

La referencia es opaca, opcional y de hasta 512 unidades UTF-16. Puede ser un ID
de versión propio, pero nunca incluye PFX/PEM, clave privada, Token o Sign. Un
replay compatible conserva intento, próximo horario, referencia y claim. Un
cambio del payload fiscal o de la referencia en la misma generación entra en
conflicto. Para revisar un `Rejected`, prepará antes la revisión explícita del
diario; la cola solo fija esa versión cuando se la programa expresamente.

El paquete de migraciones se registra y ejecuta desde un actor de despliegue
separado del host de la API. Para SQLite, agregá la referencia
`NetArcaWs.EntityFrameworkCore.Migrations.Sqlite` y usa la misma selección de
módulos:

```csharp
static async Task<NetArcaWsMigrationStatus> ApplyRecoveryMigrations(
    string sqliteConnectionString, CancellationToken cancellationToken)
{
    NetArcaWsModelOptions deploymentModel = NetArcaWsModelOptions.Configure(options =>
        options.AddInvoicing(ArcaService.Wsfev1)
            .AddInvoiceRecovery(ArcaService.Wsfev1));
    var deploymentServices = new ServiceCollection();
    deploymentServices.AddNetArcaWsSqliteMigrations(sqliteConnectionString, deploymentModel);
    await using ServiceProvider deploymentProvider = deploymentServices.BuildServiceProvider();
    INetArcaWsMigrator migrator = deploymentProvider.GetRequiredService<INetArcaWsMigrator>();

    NetArcaWsMigrationStatus before = await migrator.GetStatusAsync(cancellationToken);
    if (before.Modules.Any(module => module.State is NetArcaWsMigrationState.UntrackedSchema
            or NetArcaWsMigrationState.UnknownAppliedMigration or NetArcaWsMigrationState.InconsistentHistory
            or NetArcaWsMigrationState.TrackedSchemaIncomplete))
        throw new InvalidOperationException("Migration preflight found a schema requiring operator diagnosis.");
    string reviewedSql = migrator.GenerateScript(
        NetArcaWsPersistenceModule.InvoiceRecovery,
        fromMigration: "0", toMigration: null, idempotent: false);
    // El actor de despliegue revisa before y reviewedSql antes de esta llamada.
    _ = reviewedSql;
    return await migrator.ApplyAsync(cancellationToken);
}
```

El módulo queda seleccionado por `recoveryModel`; solo `ApplyAsync` cambia el
esquema. El estado `UntrackedSchema`, IDs desconocidos, historial inconsistente
o esquema incompleto requiere diagnóstico, no un baseline ni retry ciego. La
historia independiente es `__NetArcaWsInvoiceRecoveryMigrations` y el ID inicial
`20261007000400_InitialInvoiceRecovery`; el apply exclusivo crea solo
`NetArcaInvoiceRecoveryJobs`. SQLite no admite SQL idempotente. MySQL/MariaDB
pueden confirmar DDL parcial y el preflight bloquea el reintento ciego.

El worker hace un claim por iteración, crea un scope DI, resuelve autorización y
procesa fuera de la transacción de base. Los valores iniciales son lease de cinco
minutos, intervalo idle de cinco segundos, diez intentos, backoff de dos segundos
a cinco minutos y jitter de 0,2. Los rangos admitidos son lease mayor que cero y
hasta 30 minutos, idle mayor que cero y hasta un día, backoff base mayor que cero
y no superior al máximo, máximo de un día, 1–1000 intentos y jitter finito de 0–1.
El worker no es un scheduler distribuido; la aplicación controla instancias y
scope tenant/servicio.

Un claim vencido mientras el journal siga `Prepared` hace que la misma
transacción cambie el journal a `Unknown` antes de devolver un claim de
reconciliación. Por eso un lease vencido nunca significa que ARCA no recibió la
escritura. Solo `Prepared` con claim vigente puede enviar; `Unknown` o
`Submitting`/`Reconciling` vencidos consultan la misma identidad fiscal. Una
consulta vacía/no disponible solo reprograma la reconciliación. Backoff acotado
y jitter regulan esas consultas, no el reenvío fiscal. Al llegar a `MaxAttempts`,
el trabajo se suspende y requiere `ScheduleAsync(tenantId, key,
expectedInvoiceVersion, credentialReference)` explícito. Este método valida la
versión exacta y reactiva un trabajo compatible; nunca hace SOAP. `FindAsync(scope,
key)` devuelve metadata segura (estado, intento, horarios, generación y motivo),
sin identidad, tenant/key, hash, referencia ni payload.

La cola solo cubre CAE unitario WSFEv1, WSFEXv1 y WSMTXCA. No agrega batch,
WSFECred, WSCPE, retry SOAP genérico, failover ni garantía exactly-once. SQLite
solo admite archivo local y procesos del mismo host; para multi-host se necesita
una base transaccional de servidor compartida por un provider soportado. Esta API
no está en los paquetes públicos 0.6.0 y requiere una publicación posterior.

La evidencia de Task 3 reporta suites locales completas para estas revisiones:
SQLite 131/131, MySQL 8.4.11 29/29, MariaDB 11.4.13 29/29, PostgreSQL 17.6
28/28 y SQL Server Developer 28/28; los proyectos offline de metadatos aprobaron
PostgreSQL 8/8 y SQL Server 19/19. Las pruebas recovery específicas de preflight,
fallo y cancelación fueron focalizadas y son subconjuntos de esas matrices,
excepto las ejecuciones dedicadas MySQL y MariaDB de cancelación (1/1 cada una).
Son datos sintéticos y verifican las versiones indicadas; no son homologación
ARCA ni compatibilidad universal. El consumer smoke usó un feed local y caché
nueva, aplicó los cuatro módulos SQLite, verificó recovery sintético, generó
scripts offline para los cinco providers e instaló el CLI; también se empaquetaron
los once proyectos a 0.6.0. La revisión de solución completa aprobó restore
locked, build Release (0 warnings/0 errors), 630 pruebas (588 aprobadas, 42
skips opt-in, 0 fallos), 25 DDL y 20 scripts de migración. El pack se repetirá
después de actualizar los README incluidos en los paquetes.

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
