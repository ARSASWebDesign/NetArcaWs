# Durable Invoice Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an opt-in durable recovery queue and worker for unitary WSFEv1, WSFEXv1, and WSMTXCA invoice operations.

**Architecture:** Add the queue contracts and a partial `EfInvoiceJournal<TContext>` implementation to the optional EF package. It atomically prepares/enqueues without changing `InvoiceSubmission`, its canonical hash, schema, or `IInvoiceJournal`. Put the processor and optional hosted loop in the EF package; the host supplies authorization for the pinned opaque credential reference.

**Tech Stack:** .NET 10, C# 14, EF Core 10.0.12, existing five relational providers, xUnit v3, AwesomeAssertions, Microsoft.Testing.Platform, synthetic SOAP transports.

**Spec:** `docs/superpowers/specs/2026-10-07-invoice-recovery-design.md`

## Global Constraints

- Keep `InvoiceSubmission`, canonical payload/hash/schema, existing journal leasing/fencing, and `IInvoiceJournal` unchanged except the specifically required atomic expired-queue-claim transition `Prepared`→`Unknown` with invoice version increment.
- Preserve global fiscal identity `(environment, CUIT, point of sale, voucher type, voucher number)` and series reservation.
- A queue claim expired while journal state is `Prepared` atomically changes the journal to `Unknown` and increments its version; that job can only reconcile. The queue owns a separate generation/fence.
- Persist no duplicate fiscal payload and no credential material; `CredentialReference` is opaque and immutable per job generation.
- No DB transaction may remain open during SOAP; never retry an uncertain authorization.
- Only explicit `ScheduleAsync` may reactivate completed or suspended work, after exact-version validation. It may pin a newly revised rejected submission only after `ReviseRejectedAsync` has prepared that exact version; it never calls SOAP.
- The host resolver authorizes tenant/CUIT/environment and resolves the pinned credential version; recheck the queue fence after resolution.
- Queue/workers and migrations are opt-in; never run DDL at registration/startup.
- Support SQLite, MySQL, MariaDB, PostgreSQL, and SQL Server with independent migrations; retain existing migration artifacts byte-identical.
- Keep core free of EF/Hosting dependencies; add Hosting.Abstractions 10.0.12 only to the EF package and review its MIT license/lockfile.
- Use synthetic data and SOAP only; do not call ARCA or configure real credentials.

## Review Focus

- Queue lease expiry with journal still `Prepared`: verify the transaction changes it to `Unknown` before a new claimant can process it; prove zero second authorization calls.
- Resolver delay crossing lease expiry: prove the post-resolution fence check blocks SOAP.
- Replayed prepare/enqueue for same version: preserve attempt, schedule, credential reference and generation; incompatible replays conflict.
- Same CUIT/invoice identity under different tenants or services: preserve global uniqueness and prevent cross-scope claims.
- Supplementary Unicode edge cases: reject malformed UTF-16 scope/key/reference values deterministically without leaking raw values. Bound tenant, key, and credential reference to 512 UTF-16 code units.

---

### Task 1: Atomic queue persistence and typed request builders

**Files:**
- Create: `src/NetArcaWs.EntityFrameworkCore/InvoiceRecoveryContracts.cs` (all queue, scope, lease, metadata, resolver and options contracts in the EF namespace)
- Modify: `src/NetArcaWs/Invoicing/SafeInvoiceService.cs`
- Modify: `src/NetArcaWs.EntityFrameworkCore/EfInvoiceJournal.cs` to make it partial and share preparation helper
- Create: `src/NetArcaWs.EntityFrameworkCore/EfInvoiceRecoveryQueue.cs`
- Modify: `src/NetArcaWs.EntityFrameworkCore/InvoiceJournalEntities.cs` for entity map and model options/builder
- Create test: `tests/NetArcaWs.EntityFrameworkCore.Tests/EfInvoiceRecoveryQueueTests.cs`
- Modify test: `tests/NetArcaWs.UnitTests/Invoicing/SafeInvoiceServiceTests.cs`

**Interfaces:**
- Produces `IInvoiceRecoveryQueue.PrepareAndEnqueueAsync(InvoiceSubmission submission, string? credentialReference, CancellationToken cancellationToken = default)` and `ScheduleAsync(string tenantId, string idempotencyKey, long expectedInvoiceVersion, string? credentialReference, CancellationToken cancellationToken = default)` exactly as in the spec.
- Adds `AddInvoiceRecovery(params ArcaService[] services)`; validate enum, duplicates, and service selection at runtime DI composition. `InvoiceRecoveryScope.Services` is an immutable defensive copy of `IReadOnlySet<ArcaService>`.
- Produces public static typed helpers `SafeInvoiceService.CreateWsfeSubmission(ArcaTenantContext tenant, string key, FecaeRequest request)`, `CreateWsfexSubmission(ArcaTenantContext tenant, string key, ClsFexRequest request)`, and `CreateWsmtxcaSubmission(ArcaTenantContext tenant, string key, ComprobanteType request)`; optional WSFE wrapper overload accepts `FecaeSolicitar`. Each reuses the exact current validation/freeze/canonicalization path and returns `InvoiceSubmission` without SOAP. Keep helper tests in `tests/NetArcaWs.UnitTests`; recovery processor tests belong in the existing EF test project.
- Produces the complete `IInvoiceRecoveryQueue` implementation, including `TryClaimAsync`, `IsCurrentAsync`, `CompleteAsync`, and `FindAsync`; Task 2 must not add queue-method placeholders.

- [ ] **Step 1: Add failing EF tests** in `tests/NetArcaWs.EntityFrameworkCore.Tests/EfInvoiceRecoveryQueueTests.cs` for queue+invoice+series rollback on injected DB failure; identical prepare replay preserving job fields; changed payload conflict; explicit schedule reactivation requiring exact invoice version. Cover rejected revision followed by explicit schedule pinning the revised snapshot without SOAP. Add core tests in `tests/NetArcaWs.UnitTests/Invoicing/SafeInvoiceServiceTests.cs` for each typed helper, asserting same serialized payload and identity as existing Authorize path.
- [ ] **Step 2: Run the targeted EF queue and core helper tests and confirm they fail** with new contracts/entities/helpers absent.
- [ ] **Step 3: Implement the queue entity, indexes, shared prepare helper, typed submission builders, all queue APIs/contracts and builder selection**. `EfInvoiceJournal<TContext>` becomes partial; `PrepareInContextAsync` shares the existing transaction/context while preserving current `PrepareAsync` semantics. Add strict UTF-16 and 512-code-unit bounds for tenant/key/reference; no raw values in logs/errors. Validate `ScheduleAsync` against exact current version and supported state, reject active jobs/leases, and permit pinning a newly revised Prepared version.
- [ ] **Step 4: Add regression tests** that identical replay does not reset attempt/next due/reference and that fiscal series reservation remains global across tenants/services.
- [ ] **Step 5: Run targeted tests** with `dotnet test tests/NetArcaWs.EntityFrameworkCore.Tests/NetArcaWs.EntityFrameworkCore.Tests.csproj --configuration Release` and `dotnet test tests/NetArcaWs.UnitTests/NetArcaWs.UnitTests.csproj --configuration Release`; expect all targeted tests to pass.
- [ ] **Step 6: Commit** atomic queue and submission-builder work using a Conventional Commit message.

### Task 2: Fenced processor and opt-in hosted worker

**Files:**
- Create: `src/NetArcaWs.EntityFrameworkCore/InvoiceRecoveryProcessor.cs`
- Create: `src/NetArcaWs.EntityFrameworkCore/InvoiceRecoveryHostedService.cs`
- Modify: EF DI extensions and package project/lockfile
- Test: Create `tests/NetArcaWs.EntityFrameworkCore.Tests/InvoiceRecoveryProcessorTests.cs`; add/extend queue persistence cases in `tests/NetArcaWs.EntityFrameworkCore.Tests/EfInvoiceRecoveryQueueTests.cs`

**Interfaces:**
- Consumes the complete Task 1 queue API: `TryClaimAsync`, `IsCurrentAsync`, `CompleteAsync`, and `FindAsync`; Task 2 implements no queue persistence operations. Produces the processor, authorization resolver and hosted-service registration.
- Produces `IInvoiceRecoveryContextResolver.ResolveAsync(InvoiceRecoveryWorkItem workItem, CancellationToken cancellationToken = default)` and `IInvoiceRecoveryProcessor.RunOnceAsync(InvoiceRecoveryScope scope, CancellationToken cancellationToken = default)`.
- Defaults: lease 5 minutes, idle poll 5 seconds, base backoff 2 seconds, max backoff 5 minutes, max attempts 10, jitter ratio 0.2. Use `TimeProvider`. Validate lease `(0, 30m]`, idle `(0, 1d]`, base `(0, max]`, max backoff `(0, 1d]`, attempts `1..1000`, and finite jitter `0..1`.
- `AddNetArcaWsInvoiceRecoveryWorker(scope, configure)` is opt-in, validates queue/resolver/SafeInvoiceService and service coverage, and does not execute DDL.

- [ ] **Step 1: Add failing tests** for claim isolation by tenant/service, monotonic generation and claim ID, expired-lease fencing, and expired `Prepared` claim atomically converting to `Unknown`/version+1 before reconciliation.
- [ ] **Step 2: Add fake resolver and SOAP tests** proving only fresh Prepared is resumed; Unknown and expired Submitting/Reconciling reconcile; terminal/completed/suspended work does not poll-loop; max 10 inconclusive attempts suspend.
- [ ] **Step 3: Add the resolver-delay test**: expire/reclaim the queue lease during `ResolveAsync`, then assert the stale processor fails its second `IsCurrentAsync` and makes zero SOAP calls.
- [ ] **Step 4: Implement the processor using Task 1 queue claim/CAS APIs**. Do not change existing journal lease/fencing semantics or hold a DB transaction around SafeInvoiceService calls. Use only safe reason enums; never persist or log exception strings.
- [ ] **Step 5: Add optional hosted loop and dependency** using `Microsoft.Extensions.Hosting.Abstractions` 10.0.12, review MIT terms, then run `dotnet restore NetArcaWs.slnx --force-evaluate` and inspect only expected project lockfile changes.
- [ ] **Step 6: Run targeted tests** for `EfInvoiceRecoveryQueueTests`, `InvoiceRecoveryProcessorTests`, and core `SafeInvoiceServiceTests`. Expect all synthetic tests to pass without ARCA configuration.
- [ ] **Step 7: Commit** processor and hosting integration with a Conventional Commit message.

### Task 3: Five-provider modular migration

**Files:**
- Modify: `src/NetArcaWs.EntityFrameworkCore/Migrations/MigrationContracts.cs`
- Modify: `src/NetArcaWs.EntityFrameworkCore/Migrations/NetArcaWsMigrator.cs`
- Create/update: `InvoiceRecovery` migration contexts, migration and snapshot in all five `src/NetArcaWs.EntityFrameworkCore.Migrations.*` projects
- Update: `tools/NetArcaWs.Build` native DDL generation/source manifests and `tests/NetArcaWs.Build.Tests` plus `tests/NetArcaWs.EntityFrameworkCore.Tests/OfficialSqliteMigrationsTests.cs`, `tests/NetArcaWs.EntityFrameworkCore.MySql.Tests/OfficialMySqlMigrationTests.cs`, `tests/NetArcaWs.Persistence.Server.Tests/OfficialServerMigrationTests.cs`, and provider metadata suites

**Interfaces:**
- Adds `NetArcaWsPersistenceModule.InvoiceRecovery` and history table `__NetArcaWsInvoiceRecoveryMigrations`.
- Uses migration ID `20261007000400_InitialInvoiceRecovery` for each provider.
- Recovery-only migration context creates only `NetArcaInvoiceRecoveryJobs`.
- Expected native artifact totals become 25; migration IDs total 20. Exactly five all-modules DDL artifacts add the queue table; the other 15 existing DDL artifacts and 15 historical migration SQL/scripts remain byte-identical.

- [ ] **Step 1: Add failing migration metadata tests** for module selection, table ownership, module history, clean install and recovery-only DDL containing exactly the recovery table.
- [ ] **Step 2: Generate independent provider migrations** for SQLite, MySQL, MariaDB, PostgreSQL and SQL Server; verify each schema and index semantics.
- [ ] **Step 3: Add upgrade/preservation tests** in the existing SQLite, MySQL/MariaDB and server-engine migration suites with existing journal rows/reservations; assert recovery-only context generates exactly the queue table and leaves all other tables untouched. Existing all-modules DDL adds the table; other 15 DDL outputs and all 15 historical migration scripts remain byte-identical.
- [ ] **Step 4: Test repeated apply and incomplete/cancelled migration preflight behavior**; assert no migration runs implicitly during DI or service-provider construction. For SQLite/PostgreSQL/SQL Server assert rollback where provider DDL transactions promise it; for MySQL/MariaDB assert partial DDL is not marked successful and preflight blocks blind retry.
- [ ] **Step 5: Regenerate native DDL and source manifest** with `dotnet run --project tools/NetArcaWs.Build -- persistence-schema` (and documented migration script command); inspect generated diff/hashes. Do not edit generated artifacts by hand.
- [ ] **Step 6: Run runtime migration and independent-process recovery tests on all five engines**: SQLite via `tests/NetArcaWs.EntityFrameworkCore.Tests/OfficialSqliteMigrationsTests.cs`; PostgreSQL and SQL Server via `tests/NetArcaWs.Persistence.Server.Tests/OfficialServerMigrationTests.cs` and `ServerEngineProcessTests.cs`; MySQL and MariaDB via `tests/NetArcaWs.EntityFrameworkCore.MySql.Tests/OfficialMySqlMigrationTests.cs` and `MySqlPersistenceTests.cs`. Extend `tests/NetArcaWs.Persistence.Probe/Program.cs` and those existing process fixtures to cover queue claim, restart/reclaim after lease expiry, Prepared→Unknown conversion, and stale-fence rejection. Assert module status/current and artifact counts 25 DDL / 20 migrations; report each engine separately.
- [ ] **Step 7: Commit** the migration module and generated artifacts with a Conventional Commit message.

### Task 4: Consumer documentation, package verification, and delivery

**Files:**
- Modify: `README.md`, `ARCHITECTURE.md`, `PROGRESS.md` where needed
- Modify: service wiki sources and generated wiki only for the newly implemented recovery feature
- Modify: `docs/adr/0001-safe-invoice-retries.md` to record queue/lease decision and boundaries
- Modify: migration and package docs for opt-in setup and SQLite limits

**Interfaces:**
- Document module selection, explicit migration procedure, runtime registration, authorized context resolver contract, pinned credential reference lifecycle, backoff/suspension, queue metadata lookup, and host-managed scheduling.
- State clearly that SQLite supports local-file/same-host processes only; multi-host use requires shared transactional server storage with supported provider semantics.
- Do not publish wiki, create release/tag, bump package version, or invoke live ARCA scenarios.

- [ ] **Step 1: Add consumer documentation** with complete opt-in configuration and exact migration commands; explain that `ScheduleAsync` is explicit and never performs SOAP itself.
- [ ] **Step 2: Update ADR/progress/readme and wiki sources** with evidence-bounded implemented/local-tested status; generate wiki using `dotnet run --project tools/NetArcaWs.Build -- wiki` and inspect it.
- [ ] **Step 3: Run full repository checks** from `AGENTS.md`: locked restore, Release build, full tests, and pack all 11 packages (library, tool, EF persistence and five migration packages) at the existing version; do not bump version or publish.
- [ ] **Step 4: Extend `tests/NetArcaWs.Persistence.PackageSmoke/Program.cs` and project references to compile the new APIs from a fresh cache/local package feed**, following its existing source/version mapping; validate registration/migration package flow and library/CLI packs. Do not claim the public 0.6.0 package contains these not-yet-released APIs.
- [ ] **Step 5: Review diff and status** to ensure no credentials, real fiscal data, unrelated files, generated Python tooling, release changes, or published wiki output. Do not claim the API is present in public package version 0.6.0; it is pending a later release.
- [ ] **Step 6: Commit** docs and implementation completion notes with a Conventional Commit message; prepare the PR toward `main` only when root directs.
