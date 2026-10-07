<!-- Source: docs/adr/0005-ef-core-invoice-journal.md. Generated wiki mirror; edit the repository source. -->

# ADR 0005: Persistencia EF Core opt-in por módulo

- Estado: Aceptado; MySQL/MariaDB/PostgreSQL/SQL Server verificados en engines reales, con evidencia acotada por versión
- Fecha: 2026-10-06
- Complementa: [reintentos seguros](Decisi%C3%B3n-1-Emisi%C3%B3n-y-reintentos-seguros) y [contexto multitenant](Decisi%C3%B3n-2-Contexto-multitenant)

## Contexto

`SafeInvoiceService` y `IInvoiceJournal` ya aplican preparación durable,
idempotencia fiscal, fencing y reconciliación. SQLite atiende procesos que
comparten un archivo local en un host; no es una base distribuida ni se debe
usar sobre NFS. Las aplicaciones que ya usan EF Core necesitan integrar esas
primitivas con su esquema y controlar el ciclo de migraciones sin agregar EF al
paquete principal.

## Decisión

La integración vive en paquetes opcionales. `NetArcaWs` y `NetArcaWs.Tool` no
dependen de Entity Framework Core. `NetArcaWs.EntityFrameworkCore` proporciona
las entidades, el diario y el almacenamiento compartido de tickets WSAA;
`NetArcaWs.EntityFrameworkCore.MySql` agrega un registro para el proveedor
Microting compatible con EF Core 10. La aplicación pasa la versión exacta del
servidor MySQL/MariaDB, sin autodetección. Los paquetes opcionales
`NetArcaWs.EntityFrameworkCore.PostgreSql` (Npgsql EF provider) y
`NetArcaWs.EntityFrameworkCore.SqlServer` agregan los providers PostgreSQL y
SQL Server. Los extras `.Migrations.Sqlite`, `.MySql`, `.MariaDb`,
`.PostgreSql` y `.SqlServer` llevan cadenas oficiales versionadas separadas
por módulo, usando contextos dedicados de migración. Todos dejan la conexión y
la ejecución de migraciones bajo control de una tarea de despliegue del
consumidor. El paquete core y el paquete EF base no dependen de providers de
base de datos.

La selección de capacidades y servicios se realiza una vez mediante
`NetArcaWsModelOptions`. Puede habilitar el diario para WSFEv1, WSFEXv1 y/o
WSMTXCA, tickets para los servicios autenticados que use la aplicación y/o el
store independiente de certificados. El conjunto inmutable forma parte de la
clave de caché del modelo EF. Un contexto consumidor aplica la misma selección
a su propio modelo y DI:

```csharp
NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(options =>
    options.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1, ArcaService.Wsmtxca)
        .AddWsaaTickets(ArcaService.Wsfev1, ArcaService.PadronA5)
        .AddCertificates());

services.AddDbContextFactory<MyApplicationDbContext>(options => options.UseSqlite(connectionString));
services.AddSingleton<IWsaaTicketProtector>(protectorFromApplicationKeyRing);
services.AddSingleton<IArcaCertificateProtector>(certificateProtectorFromApplicationKeyRing);
services.AddNetArcaWsEntityFrameworkStores<MyApplicationDbContext>(modelOptions);
services.AddNetArcaWsInvoicing(); // Registra WSAA/fachadas y SafeInvoiceService.
```

`MyApplicationDbContext.OnModelCreating` llama a
`modelBuilder.AddNetArcaWs(modelOptions)` además de configurar las entidades de
la aplicación. El contexto necesita `IDbContextFactory<TContext>`. Si una misma
clase de contexto usa selecciones distintas dentro de un mismo proveedor EF, la
aplicación implementa `INetArcaWsModelOptionsProvider` y configura
`NetArcaWsModelCacheKeyFactory`; es preferible mantener una selección fija por
tipo de contexto.

Para selección de tickets, la aplicación también registra el servicio base
`services.AddNetArcaWs()` (la extensión `AddNetArcaWsInvoicing()` hace ese
registro al usar el diario). Antes de habilitar `AddWsaaTickets`, debe
proporcionar `IWsaaTicketProtector` con un keyring compartido y administrado
fuera de la base. La biblioteca no crea ni distribuye esas claves.

Para un contexto dedicado que incluye el helper MySQL/MariaDB:

```csharp
services.AddNetArcaWsMySqlStores(
    connectionString,
    new MySqlServerVersion(new Version(8, 4, 11)),
    modelOptions);
```

`MariaDbServerVersion` se usa cuando la base es MariaDB. El helper registra
`ArcaWsDbContext`, la factoría y los stores con las opciones exactas; no abre
conexiones, crea esquema ni aplica migraciones. No agrega una estrategia de
reintentos EF ni reintentos SOAP. Los helpers PostgreSQL y SQL Server usan
`AddNetArcaWsPostgreSqlStores(connectionString, modelOptions)` y
`AddNetArcaWsSqlServerStores(connectionString, modelOptions)`, respectivamente;
tampoco autodetectan la versión ni conectan al registrar.
Para un contexto EF existente, registrar el proveedor en la aplicación y usar
`AddNetArcaWsEntityFrameworkStores<TContext>`.

Las migraciones oficiales poseen seis tablas dedicadas: facturación posee
`NetArcaInvoices`, `NetArcaInvoiceRevisions` y
`NetArcaInvoiceSeriesReservations`; WSAA posee `NetArcaWsaaTickets`; certificados
posee `NetArcaCertificateSlots` y `NetArcaCertificateVersions`. Sus historias
independientes son `__NetArcaWsInvoiceMigrations`,
`__NetArcaWsTicketMigrations` y `__NetArcaWsCertificateMigrations`. Cada
proveedor y módulo mantiene su propia migración inicial y snapshot. La selección
dinámica de servicios no altera el modelo de tablas.

El consumidor actualiza los paquetes y aplica las migraciones pendientes desde
un actor de despliegue explícito. DI y el inicio normal de APIs no abren
conexiones ni migran. El actor requiere permisos de cambio de esquema; runtime
usa permisos de datos acotados. Los trabajos de despliegue deben serializarse
por base porque el preflight se ejecuta fuera del lock EF y no coordina procesos
externos. Los tres módulos independientes se pueden aplicar en cualquiera de
los seis órdenes posibles; deshabilitarlos conserva tablas, historia y filas.
No hay transacción global entre módulos ni rollback uniforme de DDL parcial en
todos los motores.

Para cambios upstream se conserva cada migración publicada y se agrega una
nueva por proveedor y módulo, probando upgrades desde la versión anterior. El
consumidor no agrega migraciones propias para las tablas dedicadas. No existe
adopción automática de diario SQLite core, `EnsureCreated`, DDL manual ni
historias administradas por el consumidor. Si se considera transferir propiedad,
compará manualmente el esquema y diseñá un procedimiento revisado: el migrador
rechaza tablas existentes sin historial, history IDs desconocidos, huecos e
integridad incompleta; nunca inserta un baseline automáticamente. No se
recomiendan instrucciones destructivas.

El paquete del diario conserva las invariantes del ADR 0001: unicidad fiscal
entre tenants, clave idempotente por tenant, snapshot/hash antes del envío,
lease/fencing y estados inciertos que se reconcilian sin reenvío automático.
Solo WSFEv1, WSFEXv1 y WSMTXCA se seleccionan para el diario durable. No se
incluyen lotes durables, WSFECred, WSCPE ni asignación automática de números.

## Consecuencias y límites

- Se puede seguir usando solo el paquete principal, SQLite o una implementación
  propia de `IInvoiceJournal`; EF y los providers MySQL/MariaDB, PostgreSQL y SQL
  Server son opt-in.
- La aplicación controla proveedor, claves de conexión, migraciones, despliegue,
  backups, retención y protección de los datos fiscales guardados.
- Seleccionar cualquiera de los tres módulos agrega únicamente sus tablas;
  cualquier combinación incorpora las familias seleccionadas al mismo contexto.
- El [modelo relacional](Modelo-relacional) describe seis tablas
  operativas y 20 DDL actuales (cinco motores × cuatro selecciones). Por separado,
  hay 15 scripts de migración versionados (cinco motores × tres módulos) para el
  upgrade inicial `0`→`latest`; no son idempotentes ni adoptan esquemas previos.
  SQLite puede crear además su tabla interna `__EFMigrationsLock`. Las relaciones actuales son lógicas por hashes; no
  hay FKs físicas, cascadas ni restricciones CHECK de enums.
- Los tests sintéticos verifican código y persistencia local; no prueban ARCA.
  Los tests con engines MySQL/MariaDB reportan por separado las versiones de
  motor que hayan ejecutado; no se declara compatibilidad universal por usar un
  proveedor EF.
- El 2026-10-06 se aprobaron 20/20 tests de integración con MySQL 8.4.11 y 20/20
  con MariaDB 11.4.13, incluidos casos entre procesos. Esa evidencia cubre
  esas versiones y esos escenarios concretos; no es una matriz universal de
  motores ni homologación fiscal.
- El 2026-10-06 el suite compartido aprobó 19/19 casos contra PostgreSQL 17.6,
  incluidos procesos independientes y tickets compartidos/reinicio. CI dispone
  de un job x64 con SQL Server Developer. La ejecución 37547645296 aprobó
  19/19 casos contra ProductVersion 16.0.4295.3. Los resultados cubren las
  versiones concretas y escenarios ejecutados, no una matriz universal de
  motores ni homologación fiscal.
- El almacenamiento compartido puede servir a varias réplicas, pero las
  propiedades transaccionales, bloqueos e índices de cada engine deben verificarse
  con ese motor y versión. No implica worker, scheduler o reintentos SOAP.
- Los tickets compartidos, sus claves y límites se especifican en el
  [ADR 0006](ADR-0006-shared-wsaa-tickets).

## Módulo independiente de certificados

El paquete EF también incluye `TenantCertificates`, un tercer módulo de
persistencia opt-in, independiente de facturación y tickets. No modifica el
comportamiento ni los historiales publicados de los módulos anteriores. Su
decisión de autorización, protección criptográfica, historial inmutable y
operación se encuentra en el [ADR 0007](ADR-0007-tenant-certificate-store).
Seleccionar `AddCertificates()` solo agrega sus dos entidades y requiere un
`IArcaCertificateProtector` configurado por la aplicación. No registra servicios
ARCA, no comparte tickets y no migra al inicio. La aplicación conserva el
`VersionId` seleccionado junto a su propia operación; el diario fiscal y su
hash canónico no cambian.

El seguimiento y las capacidades diferidas asociadas a esta integración se
mantienen en el [issue #19](https://github.com/ARSASWebDesign/NetArcaWs/issues/19).
