# NetArcaWs SQLite migrations

This optional LGPL-3.0-or-later package contains the official SQLite schema migrations for the invoice journal and shared WSAA ticket modules. It is separate from the operational stores package and does not run migrations during application startup.

Register it in an explicit deployment task, then call `GetStatusAsync`, review generated SQL when appropriate, and call `ApplyAsync`. Select modules with `NetArcaWsModelOptions`; disabled modules retain their tables and rows. Existing tables without this package's migration history and unknown history identifiers are rejected rather than baselined.

```csharp
var options = NetArcaWsModelOptions.Configure(x => x.AddInvoicing(ArcaService.Wsfev1));
services.AddNetArcaWsSqliteMigrations(connectionString, options);
```

SQLite idempotent scripts are unsupported. Use a forward migration range. This package owns only `NetArcaInvoices`, `NetArcaInvoiceRevisions`, `NetArcaInvoiceSeriesReservations`, and `NetArcaWsaaTickets`, with independent migration histories. SQLite is intended for a local file and does not provide multi-host migration coordination.
