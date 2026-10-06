# NetArcaWs MariaDB migrations

This optional LGPL-3.0-or-later package contains independent, versioned migrations for the NetArcaWs invoice journal and WSAA ticket modules. Choose a known MariaDB version explicitly. Registration does not connect to the database or apply schema changes.

```csharp
var options = NetArcaWsModelOptions.Configure(x =>
    x.AddInvoicing(ArcaService.Wsfev1).AddWsaaTickets(ArcaService.Wsfev1));
services.AddNetArcaWsMariaDbMigrations(connectionString, new MariaDbServerVersion(new Version(11, 4, 13)), options);
```

Use `INetArcaWsMigrator` from an explicit deployment step to inspect status, review generated SQL, and apply forward migrations. Invoice and WSAA histories are independent. Existing NetArcaWs tables without their official history are rejected rather than adopted automatically. The package owns only the four NetArcaWs module tables and their two migration histories.
