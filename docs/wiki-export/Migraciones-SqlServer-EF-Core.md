<!-- Source: src/NetArcaWs.EntityFrameworkCore.Migrations.SqlServer/README.md. Generated wiki mirror; edit the repository source. -->

# NetArcaWs migrations for SqlServer

This optional LGPL-3.0-or-later package contains the official, versioned EF Core migrations for the NetArcaWs invoice and WSAA ticket modules. It is separate from operational stores. Register it and run it only from an explicit deployment actor; normal API registration and startup do not connect or change the schema.

Select only the modules required by this deployment using the same `NetArcaWsModelOptions` used by the stores. A service selection changes which operations use a module, not its schema. For example:

```csharp
NetArcaWsModelOptions modelOptions = NetArcaWsModelOptions.Configure(options =>
    options.AddInvoicing(ArcaService.Wsfev1)
        .AddWsaaTickets(ArcaService.PadronA5));
var deploymentServices = new ServiceCollection();
deploymentServices.AddNetArcaWsSqlServerMigrations(connectionString, modelOptions);
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

The module-owned tables are `NetArcaInvoices`, `NetArcaInvoiceRevisions`, and `NetArcaInvoiceSeriesReservations` for invoicing, and `NetArcaWsaaTickets` for shared WSAA tickets. Independent histories are `__NetArcaWsInvoiceMigrations` and `__NetArcaWsTicketMigrations`. The migration API only moves forward. Existing tables without official history, unknown applied IDs, gaps, or incomplete tracked schemas are rejected; no baseline is inserted.

Updating means installing the new package and applying its pending official migrations. Consumers do not run `migrations add` for these dedicated tables. Future NetArcaWs model changes require a new published migration for each provider and module, preserving previous migrations and testing upgrades from the previous version. The initial migration scripts are versioned from `0` to `latest`; they are not idempotent adoption scripts. Existing DDL, `EnsureCreated` schemas, consumer-owned histories, and the core SQLite journal are not adopted or transferred automatically. Before any future adoption, manually compare ownership and schema and design a reviewed procedure; do not insert history rows automatically or drop data.

El historial está en el schema `dbo`.

This package adds no ARCA calls, certificate storage, startup migrations, retry worker, or exactly-once guarantee. Local and engine verification is reported separately from ARCA homologation. The package version is currently 0.5.0 and is not published on NuGet.
