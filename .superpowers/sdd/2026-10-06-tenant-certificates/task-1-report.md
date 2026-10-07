# Task 1 report: tenant certificate store

Implemented the optional encrypted, versioned EF certificate store in commit `0a546fc` on `codex/certificate-store`.

## Files

- Added `ArcaCertificateContracts.cs`, `ArcaCertificateEntities.cs`, `ArcaCertificateProtector.cs`, and `EfArcaCertificateStore.cs`.
- Updated `InvoiceJournalEntities.cs` with `AddCertificates()`, a certificate selection fingerprint segment, and opt-in mappings for the slot and immutable version tables.
- Updated shared EF store registration and the MySQL, PostgreSQL, and SQL Server wrapper guards to accept certificate-only selection. Certificate selection requires a registered `IArcaCertificateProtector`.
- Added protector and SQLite EnsureCreated coverage in `ArcaCertificateProtectorTests.cs` and `EfArcaCertificateStoreTests.cs`.

## TDD and verification

- RED: the requested protector test command failed to compile because `AesGcmArcaCertificateProtector` and `ArcaProtectedCertificate` did not exist yet.
- GREEN: after implementing the protector, the focused test run passed: `2/2`.
- Expanded tests cover AES key and key ID validation, AES-GCM round-trip, AAD/tamper rejection, historical key reads, defensive copies, disposal, the 1 MiB limit, model opt-in and fingerprint, SQLite table omission when disabled, encrypted certificate round-trip, exact historical lookup, create-only and stale compare-and-swap, rollback without orphan versions, competing rotations, copied-database restoration and missing-key failure, expired metadata retention with content rejection, active-scope expiry boundaries, tenant case isolation, altered metadata/AAD rejection, registration requirements, and model fingerprint mismatch.
- `/Users/hlopez/.dotnet/dotnet test tests/NetArcaWs.EntityFrameworkCore.Tests/NetArcaWs.EntityFrameworkCore.Tests.csproj --configuration Release`: passed, `69/69`, no skipped tests.
- `/Users/hlopez/.dotnet/dotnet build NetArcaWs.slnx --configuration Release --no-restore`: passed, zero warnings and errors.
- `git diff --check`: passed.

## Limits

This task adds model tables and SQLite EnsureCreated behavior only. It does not add migration module enum entries, shared migrator support, migration contexts, or official provider migrations. No ARCA calls were made. The application still authorizes the tenant/CUIT/environment before using the store and constructing `ArcaTenantContext`; key distribution and migration execution remain application/deployment responsibilities.
