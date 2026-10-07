# NetArcaWs migrations for MariaDb

This optional LGPL-3.0-or-later package contains the official, versioned EF Core migrations for NetArcaWs persistence modules. It is separate from operational stores. Register it and run it only from an explicit deployment actor; normal API registration and startup do not connect or change the schema.

Select only the modules required by this deployment using the same `NetArcaWsModelOptions` used by the stores. A service selection changes which operations use a module, not its schema. For example:

```csharp
NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(options =>
    options.AddInvoicing(ArcaService.Wsfev1)
        .AddInvoiceRecovery(ArcaService.Wsfev1)
        .AddWsaaTickets(ArcaService.PadronA5));
var deploymentServices = new ServiceCollection();
deploymentServices.AddNetArcaWsMariaDbMigrations(connectionString, new MariaDbServerVersion(new Version(11, 4, 13)), modelOptions);
```

For MySQL, pass a `MySqlServerVersion`; for MariaDB, pass a `MariaDbServerVersion`. Then resolve `INetArcaWsMigrator` from the deployment host's provider:

```csharp
INetArcaWsMigrator migrator = deploymentProvider.GetRequiredService<INetArcaWsMigrator>();
NetArcaWsMigrationStatus before = await migrator.GetStatusAsync(cancellationToken);
string sql = migrator.GenerateScript(NetArcaWsPersistenceModule.Invoicing);
NetArcaWsMigrationStatus after = await migrator.ApplyAsync(cancellationToken);
```

`GetStatusAsync` returns per-module `Empty`, `UpgradeAvailable`, `Current`, `UntrackedSchema`, `UnknownAppliedMigration`, `InconsistentHistory`, or `TrackedSchemaIncomplete`, along with known, applied, pending IDs and present owned tables. `ApplyAsync` preflights selected modules and applies pending migrations in deterministic order. `GenerateScript(module, fromMigration: "0", toMigration: null, idempotent: false)` accepts only an installed/selected module and known ascending migration endpoints; `null` means latest. The idempotent option is provider-dependent and SQLite throws `NotSupportedException` when it is requested. Script generation opens no database connection.

Review status and generated SQL before applying as appropriate. `GenerateScript` opens no connection; status and apply do. Give the deployment actor schema-change permissions and the application runtime only its required data permissions. Serialize deployment jobs for a database: preflight runs outside EF's migration lock and external deployments are not coordinated. Apply modules in either order. Disabling a module preserves its tables, history, and data.

The module-owned tables are `NetArcaInvoices`, `NetArcaInvoiceRevisions`, and `NetArcaInvoiceSeriesReservations` for invoicing, `NetArcaInvoiceRecoveryJobs` for durable recovery, and `NetArcaWsaaTickets` for shared WSAA tickets. The recovery module has its own history `__NetArcaWsInvoiceRecoveryMigrations` and initial migration ID `20261007000400_InitialInvoiceRecovery`; its context creates only the queue table. Recovery requires selecting the same service with both `AddInvoicing(...)` and `AddInvoiceRecovery(...)`. The migration API only moves forward. Existing tables without official history, unknown applied IDs, gaps, or incomplete tracked schemas are rejected; no baseline is inserted.

Updating means installing the new package and applying its pending official migrations. Consumers do not run `migrations add` for these dedicated tables. Future NetArcaWs model changes require a new published migration for each provider and module, preserving previous migrations and testing upgrades from the previous version. The initial migration scripts are versioned from `0` to `latest`; they are not idempotent adoption scripts. Existing DDL, `EnsureCreated` schemas, consumer-owned histories, and the core SQLite journal are not adopted or transferred automatically. Before any future adoption, manually compare ownership and schema and design a reviewed procedure; do not insert history rows automatically or drop data.

Pasá `MariaDbServerVersion` exacta; la biblioteca no autodetecta el servidor. MySQL/MariaDB puede confirmar parcialmente DDL, así que revisá el estado y seguí el diagnóstico del motor ante fallos.

This migrations package adds no ARCA calls, certificate storage, or worker. The companion EF runtime package in the repository source provides an opt-in recovery worker, but neither it nor these new migrations are present in the already published 0.6.0 package; a later package release is required. No exactly-once guarantee is made. Check current NuGet version and package availability: [![NuGet](https://img.shields.io/nuget/v/NetArcaWs.EntityFrameworkCore.Migrations.MariaDb)](https://www.nuget.org/packages/NetArcaWs.EntityFrameworkCore.Migrations.MariaDb).
