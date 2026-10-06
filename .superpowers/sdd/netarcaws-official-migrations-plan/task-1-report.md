# Task 1 implementation report: official SQLite migrations

Date: 2026-10-06
Base: `575b2ce6e2650b449be9aac44f41a8fe1ac6cc16` on `codex/efcore-persistence-microting`.

## Scope delivered

- Added the provider-neutral migration contracts, defensive status records, the preflight exception, and a migrator that reports status, validates forward script ranges, preflights all selected modules before DDL, and applies selected modules in deterministic order.
- Added the optional `NetArcaWs.EntityFrameworkCore.Migrations.Sqlite` package with fixed invoice and WSAA contexts, SQLite catalog inspection, explicit history-table names, offline design-time factories, LGPL notices/readme, and separate real EF Core 10.0.12 initial migrations, designers, and snapshots.
- Added the package and test project to the solution and updated the relevant lockfiles. Store registration and WSAA protection remain outside this package.
- Added SQLite acceptance tests for isolated schemas, repeat apply, both module introduction orders with persisted rows, disabled modules, untracked/unknown/non-prefix histories, all-module preflight, cancellation, partial module failure, idempotent script rejection, no-connection registration/script generation, migration model stability, empty selection, missing database status, and duplicate engine registration.

## RED / GREEN evidence

RED was established by writing `OfficialSqliteMigrationsTests.cs` before the implementation. The first requested project run failed to compile because the migration API/context namespaces did not exist yet. After fixing test-only imports, the behavior run before adding the generated migration sources had failing acceptance cases for missing migration IDs/tables and pending model changes. Those failures were resolved by adding the API, scaffolding both migrations with the private EF tooling version 10.0.12, and correcting test fixtures that had used composite-format braces in raw SQL and had assumed the first migration history table already existed.

Final verification, after the last source change:

```text
dotnet restore tests/NetArcaWs.EntityFrameworkCore.Tests/NetArcaWs.EntityFrameworkCore.Tests.csproj --locked-mode
  succeeded
dotnet build tests/NetArcaWs.EntityFrameworkCore.Tests/NetArcaWs.EntityFrameworkCore.Tests.csproj --configuration Release --no-restore
  succeeded; 0 warnings, 0 errors
dotnet test tests/NetArcaWs.EntityFrameworkCore.Tests/NetArcaWs.EntityFrameworkCore.Tests.csproj --configuration Release --no-build --no-restore
  succeeded; 52 passed, 0 failed, 0 skipped
git diff --check
  succeeded
```

Both initial migrations were generated using a temporary `dotnet-ef` 10.0.12 installation under `/tmp`; no global tool version or repository tool manifest was changed. `HasPendingModelChanges()` is false for both migration contexts. No ARCA endpoints, credentials, or production databases were used.

## Self-review and limits

- SQLite preflight checks only the exact module-owned table names and that module's history table. A missing file is reported as `Empty` without creating it; errors and cancellation propagate.
- EF Core SQLite 10 creates its own `__EFMigrationsLock` table while applying migrations. Tests account for that provider-owned lock table; module histories and application-owned schemas remain separate.
- The API offers forward migration scripts and apply only. SQLite idempotent scripts are rejected. No global transaction across modules or coordination with an external deployment operator is promised.
- This is Task 1 only. The MySQL, MariaDB, PostgreSQL, and SQL Server migration providers and their database-backed verification remain for their assigned tasks.
- No blocking concerns found in the Task 1 diff.

## Commit

Implementation and this report are included in the commit titled `feat: add official module migrations and SQLite deployment API`.
