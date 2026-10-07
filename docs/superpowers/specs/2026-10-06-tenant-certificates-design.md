# Issue #23 design: opt-in tenant certificate store

## Goal and constraints

Add an optional, versioned EF Core store for ARCA signing certificates. The application remains responsible for authorizing the tenant, represented CUIT, and environment before addressing the store or constructing `ArcaTenantContext`. The feature does not request, renew, or authorize certificates at ARCA. It must preserve the existing in-memory certificate API and remain disabled unless selected in immutable model options; only SQLite, MySQL, MariaDB, PostgreSQL, and SQL Server are supported for official migrations.

The key-encryption material comes from an application-managed keyring outside the database. The store package does not add a vault SDK, filesystem fallback, key persistence, startup migration, scheduler, or ARCA integration.

## Existing integration points

- `src/NetArcaWs/Cryptography/WsaaCertificateContent.cs`: immutable opaque PEM/PFX content; constructors copy PFX bytes, `LoadCertificate()` returns a caller-owned certificate, PFX import is ephemeral on Windows/Linux and unsupported on macOS, and `ToString()` hides secrets.
- `src/NetArcaWs/Multitenancy/ArcaTenantContext.cs`: immutable operation identity `(TenantId, Cuit, Environment, Certificate)`. App authorization precedes its construction. Keep this object as the boundary consumed by authenticated operations; the store is a credential source, not an authorization service.
- `src/NetArcaWs/Wsaa/WsaaService.cs:155`: authentication checks actual X.509 `NotBefore` and `NotAfter` at use time. Store metadata/alerts complement, but do not replace, that guard.
- `src/NetArcaWs.EntityFrameworkCore/WsaaTicketProtector.cs`: existing external-keyring AES-GCM pattern (`keyId`, nonce, ciphertext, tag, associated data), but the public API is ticket-specific. Do not reuse it for certificate material; add a separately named certificate protector and purpose.
- `src/NetArcaWs.EntityFrameworkCore/InvoiceJournalEntities.cs`: `NetArcaWsModelOptions`, its builder/fingerprint, `ModelBuilder.AddNetArcaWs`, and `ArcaWsDbContext` define opt-in model capabilities and table ownership.
- `src/NetArcaWs.EntityFrameworkCore.Migrations/MigrationContracts.cs` plus each `NetArcaWs.EntityFrameworkCore.Migrations.{Sqlite,MySql,MariaDb,PostgreSql,SqlServer}` package: `NetArcaWsPersistenceModule`, one migration history/context per module/provider, explicit deployment-only `ApplyAsync`, and per-module table preflight. Add certificates as a new independent module; do not fold these tables into `WsaaTickets` or alter published migration history.
- `docs/adr/0002-arca-tenant-context.md`, `docs/adr/0003-in-memory-certificates.md`, `docs/adr/0005-ef-core-invoice-journal.md`: authorization, certificate ownership/platform limits, and persistence/migration conventions.

## Recommended design

### Public APIs and ownership

In `NetArcaWs.EntityFrameworkCore` expose:

- `IArcaCertificateProtector` with `ArcaProtectedCertificate Protect(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData)` and `byte[] Unprotect(ArcaProtectedCertificate payload, ReadOnlySpan<byte> associatedData)`. `ArcaProtectedCertificate` is immutable, defensively copies nonce/ciphertext/tag, includes a validated `KeyId`, and redacts ciphertext in `ToString()`.
- `AesGcmArcaCertificateProtector(string activeKeyId, IReadOnlyDictionary<string, byte[]> keyRing) : IArcaCertificateProtector, IDisposable`, using AES-256-GCM, random 12-byte nonce and 16-byte tag, retaining old key IDs for reads and the active key ID for writes. Enforce a 1 MiB maximum normalized plaintext envelope before allocating ciphertext. The protector internally combines a fixed purpose/domain string, envelope version, payload key ID, and immutable version ID with caller-supplied store AAD; callers do not need the key ID to decrypt. Store AAD binds scope, fingerprint, UTC validity ticks, and creation ticks. Keys are copied and zeroed on disposal; no key is persisted or derived by the package.
- `ArcaCertificateScope(string tenantId, long cuit, ArcaEnvironment environment)`, validating the same bounds as `ArcaTenantContext`; document that constructing a scope does not grant authorization.
- `ArcaCertificateVersion(Guid versionId, DateTimeOffset createdAtUtc, string thumbprintSha256, DateTimeOffset notBeforeUtc, DateTimeOffset notAfterUtc, bool isActive)` with no secret-bearing members.
- `IArcaCertificateStore`: `Task<ArcaCertificateVersion> RotateAsync(ArcaCertificateScope scope, WsaaCertificateContent content, Guid? expectedActiveVersionId = null, CancellationToken cancellationToken = default)`, `Task<ArcaStoredCertificate?> GetActiveAsync(ArcaCertificateScope scope, CancellationToken cancellationToken = default)`, `Task<ArcaStoredCertificate?> GetVersionAsync(ArcaCertificateScope scope, Guid versionId, CancellationToken cancellationToken = default)`, `Task<IReadOnlyList<ArcaCertificateVersion>> ListVersionsAsync(ArcaCertificateScope scope, CancellationToken cancellationToken = default)`, and `Task<IReadOnlyList<ArcaCertificateVersion>> FindExpiringAsync(ArcaCertificateScope scope, TimeSpan warningWindow, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default)`. `ArcaStoredCertificate` pairs immutable metadata with redacted, caller-owned `WsaaCertificateContent` and redacts `ToString()`.

The consumer uses `GetActiveAsync` or `GetVersionAsync` after its own tenant/CUIT/environment authorization, then explicitly constructs `ArcaTenantContext`. `GetVersionAsync` must return that exact immutable version or null; it must never fall forward to active. `expectedActiveVersionId == null` means “create only if no active version exists”; rotating an existing slot requires the caller to pass its observed active ID. Return a typed concurrency exception on mismatch. Metadata methods keep expired/future versions inspectable; imports may store a structurally valid but expired/future certificate for audit/history. Content retrieval checks current validity against injected `TimeProvider` and throws a typed validity exception when `now < NotBeforeUtc || now >= NotAfterUtc`; thus it never returns unusable content, including for an expired historical version. This compare-and-swap makes competing rotations visible rather than last-writer-wins.

### Content normalization and protection

Do not add public export accessors to `WsaaCertificateContent`. On `RotateAsync`, call `LoadCertificate()`, require an RSA private key that can export PKCS#8, and serialize in memory to a canonical envelope containing certificate PEM and unencrypted PKCS#8 PEM. Protect the envelope immediately; do not store the password or source PFX representation. Clear temporary byte arrays with `CryptographicOperations.ZeroMemory` in `finally`; managed PEM strings cannot be promised erased. This intentionally retains existing macOS behavior: callers supply PEM on macOS; import does not add a disk-based fallback.

Extract `thumbprintSha256`, `NotBeforeUtc`, and `NotAfterUtc` from the loaded certificate using UTC. Reject malformed material, missing private key, non-exportable RSA key, or an invalid period (`NotBeforeUtc >= NotAfterUtc`). Do not reject a currently expired but structurally valid certificate at persistence time: retaining historical material can be needed for audit/recovery; authentication already fails closed outside actual validity. `FindExpiringAsync` is scope-bound and reports only that scope's active certificate when `NotAfterUtc` falls at or before `asOfUtc + warningWindow`; applications own alert delivery and scheduling.

Build associated data using an unambiguous canonical encoding (length-prefixed UTF-8 fields or fixed binary fields) of a fixed certificate-store purpose/domain, envelope version, key ID, tenant scope, represented CUIT, environment, immutable version ID, normalized SHA-256 thumbprint, UTC validity ticks, and UTC creation ticks. On decrypt, authenticate AAD, parse the envelope, reload the certificate, and verify thumbprint and validity exactly match stored metadata before constructing `WsaaCertificateContent.FromPem`. Any mismatch or unavailable key fails closed with sanitized public cryptographic errors; do not include inner exception text, secret bytes, PEM, or scope identity in exception messages or logs.

### Relational model and concurrency

Add two independently owned tables, only when `AddCertificates()` is selected:

- `NetArcaCertificateSlots`: one row per `(tenantHash, Cuit, Environment)` with current `ActiveVersionId` and integer `Generation`. Store the tenant only as a deterministic SHA-256 identifier hash; include the original tenant ID only in authenticated encryption scope/AAD. A unique composite key prevents two slots for the same authorized identity.
- `NetArcaCertificateVersions`: immutable version rows keyed by `(tenantHash, Cuit, Environment, VersionId)`, containing `CreatedAtUtcTicks`, `ThumbprintSha256`, `NotBeforeUtcTicks`, `NotAfterUtcTicks`, `KeyId`, `Nonce`, `Ciphertext`, and `Tag`. Index scope plus validity for active-expiry querying as supported by the common model; active selection joins through the slot head. No FK is required if the model follows current logical-relation convention, but the write transaction must guarantee the head always points to an existing version.

Rotation is one EF transaction: insert the immutable version, then conditional-update the slot with `WHERE Generation = expectedGeneration` (or insert the initial slot under its unique key), increment generation, and commit. A failed compare-and-swap or unique-key race rolls back the inserted version. Provider-independent EF transactions and affected-row count provide the concurrency guarantee; verify each engine. Never mutate/delete old versions as part of rotation. Explicit retention/deletion is outside this issue.

### Opt-in registration and migrations

Add the appended `NetArcaWsPersistenceModule.TenantCertificates` module in Task 2; Task 1 adds `NetArcaWsModelOptions.CertificatesEnabled`, builder method `AddCertificates()`, and include the option in `Fingerprint`. `ModelBuilder.AddNetArcaWs` ignores both certificate entities unless enabled and maps only those two tables when selected. Extend `AddNetArcaWsEntityFrameworkStores<TContext>` to register `EfArcaCertificateStore<TContext>` and require a registered `IArcaCertificateProtector` only when selected. No EF package provider or core package gains a dependency on a secret manager.

Each of the five migration packages receives its own certificate migration context, design-time factory, independent migration history (`__NetArcaWsCertificateMigrations`), one initial migration/snapshot, and module table ownership/preflight list updates. The official migrator still only includes selected modules and runs only when the deployment actor explicitly invokes `ApplyAsync`; DI and startup remain connection-free. Disabling certificates later preserves tables, history, ciphertext, and old versions.

## Data flows

1. Application authenticates and authorizes tenant, represented CUIT, and environment.
2. Application builds `ArcaCertificateScope` and calls store read or rotation.
3. Rotation validates/imports content, normalizes and protects it, stores an immutable version, and atomically advances the slot head by compare-and-swap.
4. Read resolves active or exact requested version, unprotects with scope/version metadata as AAD, validates decrypted certificate metadata, returns new `WsaaCertificateContent`.
5. Application builds `ArcaTenantContext`; existing WSAA authentication performs the final validity check at operation time.
6. Deployment operator separately checks status/scripts and applies the selected official migration package.

## Failure behavior and risks

- Wrong scope, altered ciphertext/AAD, absent key ID, non-exportable key, malformed envelope, or metadata mismatch must fail closed without returning bytes.
- Key rotation requires retaining old key IDs until every ciphertext protected by them is re-encrypted or deleted; this issue can defer re-encryption tooling. Backup/restore needs the same external keyring and is documented, not automated.
- EF optimistic CAS races are provider-sensitive in details; engine tests must prove one winner and no orphan committed version for all five supported engines.
- Storing normalized unencrypted PKCS#8 inside the encrypted envelope means the database only sees AES-GCM ciphertext; memory strings remain subject to normal managed-memory lifetime.
- Certificate validity describes X.509 lifetime, not ARCA service enablement, CUIT delegation, or authorization. The application and ARCA remain authoritative for those facts.
- Certificate versions are not automatically linked to the invoice journal: the consuming application must persist the selected version ID alongside any prepared operation in its own domain so rotation does not change an already-selected context. No automation renews certificates or sends warnings. The host schedules `FindExpiringAsync` and protects operational logs/results.

## Acceptance evidence

Unit tests prove scope isolation, immutable history, exact historical lookup, create-only/expected-version CAS, certificate parsing and private-key requirements, validity metadata verification, AAD swap/tamper rejection, key-ID rotation/read compatibility, redacted representations, and expiring-query boundaries under a fake `TimeProvider` where time is needed. Task 1 SQLite `EnsureCreated` tests prove active/historical round-trip, transactional rotation conflicts, and no certificate tables when unselected. Task 2 provider tests separately verify official migrations and rotation/read behavior on SQLite, MySQL, MariaDB, PostgreSQL, and SQL Server. Consumer tests compile the opt-in API and demonstrate the host constructs `ArcaTenantContext` only after scope authorization. No test calls ARCA.
