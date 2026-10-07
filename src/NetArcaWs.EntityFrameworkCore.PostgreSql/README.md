# NetArcaWs.EntityFrameworkCore.PostgreSql

Optional PostgreSQL integration for the NetArcaWs EF Core persistence package. It is maintained under LGPL-3.0-or-later; see the package license and third-party notices.

```csharp
var model = NetArcaWsModelOptions.Configure(options =>
    options.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1)
        .AddInvoiceRecovery(ArcaService.Wsfev1));

services.AddNetArcaWsPostgreSqlStores(
    configuration.GetConnectionString("ArcaPersistence")!,
    model);
```

Registration uses the supplied connection string but does not connect, detect server details, create tables, apply migrations, or enable EF execution-strategy retries. Apply reviewed migrations through the consuming application. The selected model controls which NetArcaWs tables are included. The `Npgsql.EntityFrameworkCore.PostgreSQL` dependency is optional and is not referenced by the core or base EF packages.

For shared WSAA tickets, select services with `AddWsaaTickets(...)` and register an `IWsaaTicketProtector` backed by an application-managed shared key ring before calling `AddNetArcaWsPostgreSqlStores`. The key ring must be stored outside the ticket database.

Recovery selection only adds its optional queue table to the model. Select each recovery service in both `AddInvoicing(...)` and `AddInvoiceRecovery(...)`, apply its official migration explicitly, and opt into `AddNetArcaWsInvoiceRecoveryWorker(...)` only after registering an application-owned `IInvoiceRecoveryContextResolver`. The resolver authorizes the stored tenant/CUIT/environment and resolves the exact opaque credential-version reference; the library does not authorize tenants or store credentials in the queue. SQLite is limited to local files and processes on one host; multi-host recovery needs shared transactional server storage. These APIs are in repository source after 0.6.0 and are not present in the published 0.6.0 package.
