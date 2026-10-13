# Task 1 report — persistent models and Receipt X

Implementation commit: `9f10b4c` (`feat(documents): add persistent document models and receipt X`).

## API and files

Added namespace `NetArcaWs.Documents` with:

- `ComprobanteGenerado`: immutable versioned fiscal/Receipt X snapshot, identity, state, receiver, currency and explicit totals, detail/tax/legend collections, request snapshot/version, response metadata, and Receipt X number/date/payments/allocations. Collection inputs are copied and exposed read-only. `ToString()` omits personal/fiscal values.
- `DatosEmisor`: immutable issuer profile with defensive optional `LogoPng` bytes. `PngValidator` checks PNG signature/chunk CRC and ordering, IHDR format and dimension bounds (1–2048 px), then decompresses and unfilters scanlines, including Adam7. Input is capped at 1 MiB and decoded data at 32 MiB.
- `DatosReceptor`, `DatosPresentacion`, `ResultadoComprobante`, `IdentidadComprobante`, and typed detail, tax, payment, and allocation records.
- `ComprobanteValidator.Validate` for revalidation after JSON deserialization. It does not assume fiscal detail sums equal the final total; adapters must validate service-specific totals.
- `ReciboX.GenerarComprobante(DatosReciboX)`: builds an offline X snapshot. Explicit details and payments must each sum to the receipt total and use its currency. Multiple positive allocations are supported when they do not exceed the amount received.

`DatosPresentacion` includes other indirect taxes, the receiver VAT condition, and explicit legends for later rendering without inferring tax rules.

## Evidence

- RED: before implementation, `dotnet test tests/NetArcaWs.UnitTests/NetArcaWs.UnitTests.csproj --configuration Release` failed to compile because `NetArcaWs.Documents` and its requested API were absent.
- Focused: `dotnet test tests/NetArcaWs.UnitTests/NetArcaWs.UnitTests.csproj --configuration Release --filter-class '*DocumentModelsTests*'` — 8 passed, 0 failed.
- Unit suite: `dotnet test tests/NetArcaWs.UnitTests/NetArcaWs.UnitTests.csproj --configuration Release` — 379 passed, 0 failed, 0 skipped.
- Core build: `dotnet build src/NetArcaWs/NetArcaWs.csproj --configuration Release` — succeeded with 0 warnings and 0 errors.
- `git diff --check` — clean.

Tests cover JSON round-trip and post-deserialization validation, read-only collection and logo copies, PNG metadata and corrupted CRC, safe metadata strings, total and partially allocated receipts, multiple references, issuer/receiver validation, and sum/currency mismatches.

## Concerns

- Fiscal request/response snapshots must be projected by the service adapters without WSAA Token/Sign; this task intentionally does not interpret raw SOAP payloads or infer tax rules.
- Receipt X uses an 11-digit domestic issuer CUIT. Receiver identifiers allow zero for consumer-final cases and can be extended by the adapters for foreign identification types.
