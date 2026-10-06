<!-- Source: src/NetArcaWs.EntityFrameworkCore.MySql/README.md. Generated wiki mirror; edit the repository source. -->

# NetArcaWs.EntityFrameworkCore.MySql

Optional MySQL and MariaDB integration for the NetArcaWs EF Core persistence package. It is maintained under LGPL-3.0-or-later; see the package license and third-party notices.

Pass a known server version explicitly. Registration is local configuration only: it does not connect to the database, create schema, or enable EF execution-strategy retries.

```csharp
var model = NetArcaWsModelOptions.Configure(options =>
    options.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1));

services.AddNetArcaWsMySqlStores(
    configuration.GetConnectionString("ArcaPersistence")!,
    new MySqlServerVersion(new Version(8, 4, 11)),
    model);
```

For MariaDB, pass `new MariaDbServerVersion(new Version(11, 4, 13))`. The selected model controls which tables exist. Apply migrations or schema changes explicitly through the application; this package never initializes a database during dependency registration.

The package also supports ticket-only selection with `AddWsaaTickets(...)`. When tickets are selected, register an `IWsaaTicketProtector` backed by an application-managed shared key ring **before calling** `AddNetArcaWsMySqlStores`; registration validates that protector immediately. Never use the MySQL database as the only location for its encryption keys.
