# NetArcaWs.EntityFrameworkCore.PostgreSql

Optional PostgreSQL integration for the NetArcaWs EF Core persistence package. It is maintained under LGPL-3.0-or-later; see the package license and third-party notices.

```csharp
var model = NetArcaWsModelOptions.Configure(options =>
    options.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1));

services.AddNetArcaWsPostgreSqlStores(
    configuration.GetConnectionString("ArcaPersistence")!,
    model);
```

Registration uses the supplied connection string but does not connect, detect server details, create tables, apply migrations, or enable EF execution-strategy retries. Apply reviewed migrations through the consuming application. The selected model controls which NetArcaWs tables are included. The `Npgsql.EntityFrameworkCore.PostgreSQL` dependency is optional and is not referenced by the core or base EF packages.

For shared WSAA tickets, select services with `AddWsaaTickets(...)` and register an `IWsaaTicketProtector` backed by an application-managed shared key ring before calling `AddNetArcaWsPostgreSqlStores`. The key ring must be stored outside the ticket database.
