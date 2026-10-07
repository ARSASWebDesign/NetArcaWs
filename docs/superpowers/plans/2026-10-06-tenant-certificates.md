# Tenant Certificate Store Implementation Plan

> **For agentic workers:** Use the agreed sequential task handoff. Each task is independently reviewable and must end with its own focused verification. Do not commit product changes unless the root task explicitly requests commits.

**Goal:** Add an opt-in EF Core tenant certificate store with encrypted immutable versions, explicit historical retrieval, and compare-and-swap rotation.

**Architecture:** The optional EntityFrameworkCore package owns the store contract and persistence, while a distinct external-keyring AES-GCM protector protects a normalized in-memory PEM envelope. The model adds a slot head and immutable versions as an independent persistence module; five provider-specific migration packages own the official migrations. The application authorizes the scope before calling the store and builds `ArcaTenantContext` from the selected returned content.

**Tech Stack:** .NET 10, C# nullable, EF Core 10, xUnit v3, AwesomeAssertions, Microsoft.Testing.Platform, existing five relational migration providers.

**Spec:** `docs/superpowers/specs/2026-10-06-tenant-certificates-design.md` (design for issue #23).

## Global Constraints

- `NetArcaWs` and `NetArcaWs.Tool` do not depend on EF Core or secret-manager SDKs.
- No key material is stored beside ciphertext as the sole recovery mechanism; application keyring remains external.
- Do not expose/export `WsaaCertificateContent` internals through new public APIs.
- Use `LoadCertificate()` and normalize to certificate PEM plus RSA PKCS#8 PEM in memory; reject non-exportable RSA keys and retain the existing macOS PFX limitation.
- A store scope does not grant authorization; the host authorizes tenant, represented CUIT, and environment first.
- Historical retrieval selects the exact immutable version and never falls forward to active.
- Rotation uses active-version compare-and-swap; losing concurrent writes roll back without committed orphan versions.
- Preserve issued migrations; add one new independent certificate module migration/context/history per provider.
- Migration runs only through explicit deployment `ApplyAsync`; DI/startup does not connect or migrate.
- No renewal automation, alert delivery, ARCA network calls, automatic retention, or file fallback. The consumer persists a selected `VersionId` beside its own prepared operation; this change does not alter the invoice journal payload hash/model.
- Keep temporary byte arrays zeroed where practical; do not promise immediate erasure of managed strings.

## Review Focus

- Same tenant with different CUIT or environment must not collide: test all scope dimensions in associated data and keys.
- Ciphertext, metadata, or scope substitution must be rejected: test GCM AAD authentication and post-decrypt fingerprint/validity comparison.
- Two concurrent rotations must have one winner and leave no committed losing version: test per provider.
- Historical retrieval after rotation must return the requested old certificate exactly while it is currently valid: test version ID and fingerprint; expired/future versions remain metadata-only.
- Metadata for expired/future certificates remains inspectable, while content retrieval fails closed and the existing WSAA operation-time validation remains authoritative; verify both paths separately.

---

### Task 1: Store model, public API, protector, and SQLite behavior

**Files:**
- Create: `src/NetArcaWs.EntityFrameworkCore/ArcaCertificateContracts.cs`
- Create: `src/NetArcaWs.EntityFrameworkCore/ArcaCertificateProtector.cs`
- Create: `src/NetArcaWs.EntityFrameworkCore/ArcaCertificateEntities.cs`
- Create: `src/NetArcaWs.EntityFrameworkCore/EfArcaCertificateStore.cs`
- Modify: `src/NetArcaWs.EntityFrameworkCore/InvoiceJournalEntities.cs`
- Modify: `src/NetArcaWs.EntityFrameworkCore/EfInvoiceJournal.cs` (only shared EF DI registration)
- Modify: `src/NetArcaWs.EntityFrameworkCore.MySql/NetArcaWsMySqlServiceCollectionExtensions.cs`
- Modify: `src/NetArcaWs.EntityFrameworkCore.PostgreSql/NetArcaWsPostgreSqlServiceCollectionExtensions.cs`
- Modify: `src/NetArcaWs.EntityFrameworkCore.SqlServer/NetArcaWsSqlServerServiceCollectionExtensions.cs`
- Test: `tests/NetArcaWs.EntityFrameworkCore.Tests/ArcaCertificateProtectorTests.cs`
- Test: `tests/NetArcaWs.EntityFrameworkCore.Tests/EfArcaCertificateStoreTests.cs`
- Test: `tests/NetArcaWs.EntityFrameworkCore.Tests/NetArcaWs.EntityFrameworkCore.Tests.csproj` only if test project references are needed

**Interfaces:**
- Produces `IArcaCertificateProtector.Protect(ReadOnlySpan<byte>, ReadOnlySpan<byte>) -> ArcaProtectedCertificate` and `Unprotect(ArcaProtectedCertificate, ReadOnlySpan<byte>) -> byte[]`.
- Produces `AesGcmArcaCertificateProtector(string activeKeyId, IReadOnlyDictionary<string, byte[]> keyRing)` and disposable key clearing.
- Produces `ArcaCertificateScope(string tenantId, long cuit, ArcaEnvironment environment)`; this is an identity descriptor, not an authorization decision.
- Produces `IArcaCertificateStore.RotateAsync(scope, content, expectedActiveVersionId, cancellationToken) -> Task<ArcaCertificateVersion>`, `GetActiveAsync(scope, cancellationToken) -> Task<ArcaStoredCertificate?>`, `GetVersionAsync(scope, versionId, cancellationToken) -> Task<ArcaStoredCertificate?>`, `ListVersionsAsync(scope, cancellationToken) -> Task<IReadOnlyList<ArcaCertificateVersion>>`, and scope-bound `FindExpiringAsync(scope, warningWindow, asOfUtc, cancellationToken) -> Task<IReadOnlyList<ArcaCertificateVersion>>`. `ArcaStoredCertificate` pairs metadata with redacted secret content.
- Produces `NetArcaWsModelOptions.CertificatesEnabled` and `NetArcaWsModelOptionsBuilder.AddCertificates()`. Append a certificate fingerprint segment only when selected to preserve every existing fingerprint and migration snapshot. Task 2 appends `NetArcaWsPersistenceModule.TenantCertificates` and shared migrator support.
- Consumes existing `WsaaCertificateContent.LoadCertificate()`, `ArcaTenantContext` invariants, `IWsaaTicketProtector` only as a pattern (do not reuse its ticket-specific contract), `IDbContextFactory<TContext>`, and existing `NetArcaWsModelOptions` model selection.

- [ ] Write protector tests for 32-byte key validation, key ID validation, round-trip, wrong AAD, tamper, unavailable old key ID, defensive copies, redacted `ToString`, and disposal.
- [ ] Run `dotnet test tests/NetArcaWs.EntityFrameworkCore.Tests/NetArcaWs.EntityFrameworkCore.Tests.csproj --filter FullyQualifiedName~ArcaCertificateProtectorTests` and confirm the new tests fail before implementation.
- [ ] Implement the distinct payload/protector using AES-256-GCM, 12-byte nonce, 16-byte tag, copied keyring, active write key, retained historical read keys, lock/disposal behavior parallel to the existing ticket protector. Protector internally combines a fixed certificate-purpose/domain, envelope version, payload key ID, and version ID with the caller-supplied store AAD; callers do not need to know the active key ID to decrypt.
- [ ] Write model/API tests proving certificate entities are ignored when unselected, both certificate tables appear when selected, and `CertificatesEnabled` changes the model fingerprint.
- [ ] Run focused tests and confirm failure before implementation.
- [ ] Implement `ArcaCertificateScope`, immutable metadata, exceptions, and `IArcaCertificateStore` contracts in `ArcaCertificateContracts.cs`; validate tenant length/control characters, 11-digit CUIT bounds, defined environment, positive warning windows, and UTC arguments.
- [ ] Implement slot/version entities and model mapping with composite identity key, immutable version key, ciphertext fields, UTC tick metadata, active head, and expiry lookup index.
- [ ] Implement normalizer in `EfArcaCertificateStore.cs`: load/dispose certificate, require exportable RSA private key, export certificate PEM + PKCS#8 PEM into a protected envelope, zero byte arrays in `finally`, reject malformed key/invalid period, and derive SHA-256 fingerprint/UTC validity.
- [ ] Encode AAD canonically from purpose/domain, envelope version, key ID, tenant scope, CUIT, environment, Guid version, fingerprint, NotBefore ticks, NotAfter ticks, and CreatedAt ticks. Reject normalized plaintext envelopes over 1 MiB before ciphertext allocation. On reads, decrypt, reconstruct, reload, and compare fingerprint and validity before returning `WsaaCertificateContent.FromPem`; wrap cryptographic/decode failures in sanitized public exceptions without secret-bearing inner exception text.
- [ ] Implement rotation transaction using immutable version insert then generation-conditional slot update; use a 64-bit integer `Generation`; null expected version means create-only. Translate generation/unique races to a typed concurrency exception and roll back.
- [ ] Implement exact historical lookup, active lookup, version listing, and scope-bound active expiry query. Inject `TimeProvider`; metadata remains inspectable at any validity state, while `GetActiveAsync`/`GetVersionAsync` throw a typed validity exception unless `now` is in `[NotBeforeUtc, NotAfterUtc)`. Never fall forward from an unknown historical version.
- [ ] Implement conditional DI registration in `AddNetArcaWsEntityFrameworkStores<TContext>` and update the MySQL, PostgreSQL, and SQL Server wrapper registration guards to accept certificate-only selection; require `IArcaCertificateProtector` only when certificates are selected.
- [ ] Add isolated SQLite `EnsureCreated` store tests (no migration enum, shared migrator, or migration context/history edits in this task) for model creation and no-table opt-out, encrypted round-trip, expired/future metadata retention and listing with content retrieval rejected, create-only rotation, successful compare-and-swap, stale-version conflict rollback/no orphan, exact historical retrieval, scope-bound expiring boundaries, scope collision resistance, metadata/AAD substitution rejection, and over-1-MiB normalized-envelope rejection. Verify expired/future metadata is listable but content retrieval fails closed.
- [ ] Run `dotnet test tests/NetArcaWs.EntityFrameworkCore.Tests/NetArcaWs.EntityFrameworkCore.Tests.csproj --configuration Release` and verify all focused tests pass.

### Task 2: Five-engine official migrations and schema artifacts

**Files:**
- Modify each of `src/NetArcaWs.EntityFrameworkCore.Migrations.{Sqlite,MySql,MariaDb,PostgreSql,SqlServer}/` migration context, factory, DI extension, design-time factory, and table preflight list.
- Create provider-specific `Migrations/TenantCertificates/` initial migration, designer, and model snapshot in each of the five migration projects.
- Modify: `src/NetArcaWs.EntityFrameworkCore/Migrations/MigrationContracts.cs` (append `TenantCertificates` enum value) and shared migrator selection/preflight code.
- Modify: `tools/NetArcaWs.Build` persistence-schema and migration SQL generation/selections so certificate-only, existing, and all-three module combinations are included.
- Modify: `tests/NetArcaWs.Build.Tests/` for generated DDL and migration-script artifacts.
- Modify: SQLite metadata/migration tests in `tests/NetArcaWs.EntityFrameworkCore.Tests/`.
- Modify: MySQL/MariaDB engine tests in `tests/NetArcaWs.EntityFrameworkCore.MySql.Tests/`; PostgreSQL/SQL Server tests in `tests/NetArcaWs.Persistence.Server.Tests/` (confirm actual project names from solution before editing).
- Pin migration ID `20261006000300_InitialTenantCertificates` in all five engines. No wiki/doc edits in this task; Task 3 owns documentation.

**Interfaces:**
- Consumes Task 1 public option names, entity maps, table names `NetArcaCertificateSlots`/`NetArcaCertificateVersions`, and the exact enum member appended by this task.
- Produces five official initial migration paths with ID `20261006000300_InitialTenantCertificates` and independent history `__NetArcaWsCertificateMigrations`; updates native build-generated schema artifacts. No operational API changes.

- [ ] Add the appended `TenantCertificates` enum member to each provider's selected-module list and `CreateContext` switch; set migration history to `__NetArcaWsCertificateMigrations` (SQL Server schema remains `dbo`; PostgreSQL remains `public`).
- [ ] Append `NetArcaWsPersistenceModule.TenantCertificates` without renumbering existing values; update `NetArcaWsMigrator`, provider module-selection guards, migration factories, migration contexts, and table preflight ownership. Add provider-specific migration contexts with only `AddCertificates()` selected and design-time factories. Existing invoice/ticket module selections and fingerprints remain unchanged.
- [ ] Generate initial migration ID `20261006000300_InitialTenantCertificates` and model snapshot per provider from clean model snapshots; preserve all published `Invoicing` and `WsaaTickets` migrations and snapshots byte-for-byte.
- [ ] Update `NetArcaWs.Build` persistence-schema/migration generation to include certificates and module selections: the four supported generated model selections (invoicing, tickets, certificates, and all three) × five engines = 20 DDL outputs; three independent module migration scripts × five engines = 15 scripts. Preserve all existing generated outputs except the required module-choice expansion and verify hashes/diffs.
- [ ] Add metadata tests that each provider discovers exactly one certificate initial migration and its snapshot contains only the two certificate-owned tables. Extend Build tests to assert the 20 DDL/15 script matrix and the certificate-only output contains only its two tables.
- [ ] Add engine integration assertions for clean install, exact two application tables, independent history, upgrade with existing invoice/ticket rows preserved, opt-out no-op, and conflict-safe rotation/read/historical retrieval.
- [ ] Run SQLite migration tests locally. Run MySQL, MariaDB, PostgreSQL, and SQL Server migration/store suites using existing configured engine fixtures/CI services, reporting engine versions separately.
- [ ] Verify no provider's certificate migration creates invoice/ticket tables and every engine produces the same logical columns/keys/indexes for certificate data.

### Task 3: Consumer, key operations, documentation, and final review

**Files:**
- Modify: `src/NetArcaWs.EntityFrameworkCore/README.md` or package README source if present; otherwise the root `README.md` package section.
- Modify: `docs/adr/0005-ef-core-invoice-journal.md` (describe independent certificate module and external key custody).
- Modify: `docs/adr/0003-in-memory-certificates.md` (store ingestion, normalized envelope, historical retrieval, no renewal).
- Modify: `ARCHITECTURE.md`, `PROGRESS.md`, and generated relational model documentation.
- Create/modify consumer sample/test under `examples/NetArcaWs.Examples/` or the existing EF consumer test project.
- Modify package lockfiles only if dependency graph changes; no new crypto dependency is expected.

**Interfaces:**
- Consumes the exact Task 1 APIs and Task 2 provider migration module.
- Produces documented opt-in consumer flow: authorize scope, resolve active or exact version, receive `WsaaCertificateContent`, construct `ArcaTenantContext`, then invoke existing service; deployment separately applies official selected migrations.

- [ ] Add a consumer compile test/sample configuring one external `AesGcmArcaCertificateProtector`, selecting `.AddCertificates()`, registering the EF store, and composing an authorized `ArcaTenantContext` without persisting/printing credentials. Verify the consumer records the chosen certificate `VersionId` in its own operation record; this issue does not alter invoice canonicalization or add a certificate-version column to the shared invoice journal.
- [ ] Document how to retain old key IDs, rotate the active protection key, back up/restore ciphertext plus external keyring, schedule expiring queries, and distinguish X.509 validity from ARCA authorization.
- [ ] Document macOS PEM-only import for this workflow, Windows/Linux ephemeral PFX import, no filesystem fallback, and the fact managed strings cannot be guaranteed erased immediately.
- [ ] Update ADRs, architecture, progress, README, and relational model with only evidence actually obtained; keep migration execution explicit and opt-in.
- [ ] Run the repository's locked restore, Release build, solution test, library/tool pack, and consumer sample checks only after focused engine suites pass; report any unavailable provider engine as unverified rather than inferred.
- [ ] Review `git diff --check`, generated migrations/snapshots, package locks, all public `ToString`/exception surfaces, and ensure no secret-bearing fixtures or output were added.
