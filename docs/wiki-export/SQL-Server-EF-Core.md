<!-- Source: src/NetArcaWs.EntityFrameworkCore.SqlServer/README.md. Generated wiki mirror; edit the repository source. -->

# NetArcaWs.EntityFrameworkCore.SqlServer

Optional SQL Server integration for the NetArcaWs EF Core persistence package. It is maintained under LGPL-3.0-or-later; see the package license and third-party notices.

Pass the consumer-managed connection string and explicitly select the persistence modules and ARCA services:

```csharp
var model = NetArcaWsModelOptions.Configure(options =>
    options.AddInvoicing(ArcaService.Wsfev1, ArcaService.Wsfexv1)
        .AddInvoiceRecovery(ArcaService.Wsfev1));

services.AddNetArcaWsSqlServerStores(
    configuration.GetConnectionString("ArcaPersistence")!,
    model);
```

Registration is local configuration only. It does not connect, create schema, run migrations, or enable SQL Server execution-strategy retries. Apply schema changes explicitly through the application. For shared WSAA tickets, select `AddWsaaTickets(...)` and register an application-managed `IWsaaTicketProtector` backed by keys distributed independently of the database.

Recovery selection only adds its optional queue table to the model. Select each recovery service in both `AddInvoicing(...)` and `AddInvoiceRecovery(...)`, apply its official migration explicitly, and opt into `AddNetArcaWsInvoiceRecoveryWorker(...)` only after registering an application-owned `IInvoiceRecoveryContextResolver`. The resolver authorizes the stored tenant/CUIT/environment and resolves the exact opaque credential-version reference; the library does not authorize tenants or store credentials in the queue. SQLite is limited to local files and processes on one host; multi-host recovery needs shared transactional server storage. These APIs are in repository source after 0.6.0 and are not present in the published 0.6.0 package.
