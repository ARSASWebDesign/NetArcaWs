# Diseño: recuperación durable de facturas

## Objetivo y alcance

Agregar una cola durable y un worker opt-in para recuperar operaciones CAE unitarias de WSFEv1, WSFEXv1 y WSMTXCA. La cola reutiliza el diario existente, conserva sus invariantes fiscales y permite que la aplicación consumidora autorice tenant, CUIT, ambiente y referencia de credencial antes de cada operación. El worker y las tablas solo se habilitan mediante selección explícita; ninguna migración ni ejecución comienza durante el registro de servicios.

Quedan fuera del alcance las escrituras batch, WSFECred, WSCPE, asignación de numeración, retries genéricos de escrituras SOAP, failover entre ambientes, garantía exactly-once, brokers y coordinación distribuida entre bases sin transacción compartida. No hay emisión real a ARCA dentro de estas pruebas.

## Contratos fiscales preservados

- `InvoiceSubmission`, su payload congelado, `CanonicalVersion`, hash canónico, esquema de tablas del diario e interfaz `IInvoiceJournal` permanecen sin cambios.
- La identidad única sigue siendo `(ambiente, CUIT, punto de venta, tipo, número)`, global entre tenants y servicios. La reserva de serie no se libera por vencimiento de leases, fallos o consultas negativas.
- La cola no duplica el payload fiscal. Guarda una huella/versionado del snapshot asociado: tenant/key, versión actual del diario, hash de payload, versión canónica, servicio e identidad fiscal para detectar corrupción o versión incompatible.
- La referencia de credencial es metadato técnico opaco opcional (por ejemplo, GUID de versión como string), inmutable para una generación de trabajo y fuera del payload/hash fiscal. Nunca guarda certificado, clave, token ni Sign.
- Las repeticiones de `PrepareAndEnqueueAsync` con el mismo submission no reinician intento, `NextAvailable`, referencia de credencial ni el job existente. Cambios incompatibles producen conflicto. La referencia es inmutable por generación de trabajo; una llamada explícita a `ScheduleAsync` puede comenzar una nueva generación y fijar su referencia después de validar versión exacta y compatibilidad con el diario, reactivando un job completado o suspendido.
- Solo `Prepared` sin claim expirado puede entrar a envío. Si expira el claim de cola mientras el diario sigue `Prepared`, `TryClaimAsync` transforma en la misma transacción el journal a `Unknown`, incrementa su versión y reclama trabajo para reconciliar. Así la lease vencida nunca vuelve a habilitar un envío aunque el worker anterior no haya llegado a SOAP. `Submitting` y `Reconciling` vencidos también solo se reconcilian.
- No hay transacción abierta durante SOAP. Una lease de cola vencida no demuestra que ARCA no recibió la escritura.

## API pública

Los contratos específicos de persistencia y worker están en `NetArcaWs.EntityFrameworkCore`, namespace `NetArcaWs.EntityFrameworkCore`. `SafeInvoiceService` permanece en `NetArcaWs.Invoicing`.

```csharp
public sealed class InvoiceRecoveryScope
{
    public string TenantId { get; }
    public IReadOnlySet<ArcaService> Services { get; }
    public InvoiceRecoveryScope(string tenantId, IEnumerable<ArcaService> services);
}

public enum InvoiceRecoveryState { Scheduled, Claimed, Completed, Suspended }
public enum InvoiceRecoveryDisposition { Reschedule, Complete, Suspend, ManualReview }
public enum InvoiceRecoverySafeReason
{
    None, QueryEmpty, QueryUnavailable, LeaseExpired, CredentialUnavailable,
    AuthorizationDenied, IncompatibleSnapshot, MaxAttemptsReached, PersistenceConflict
}

public sealed record InvoiceRecoveryWorkItem(
    string TenantId, string IdempotencyKey, long InvoiceVersion,
    string PayloadHash, int CanonicalVersion, ArcaService Service,
    InvoiceIdentity Identity, string? CredentialReference,
    int Attempt, DateTimeOffset NextAvailable);

public sealed record InvoiceRecoveryLease(
    InvoiceRecoveryWorkItem WorkItem, InvoiceOperation CurrentOperation,
    string ClaimId, long Generation, DateTimeOffset LeaseUntil);

public sealed record InvoiceRecoveryMetadata(
    InvoiceRecoveryState State, int Attempt, DateTimeOffset NextAvailable,
    DateTimeOffset? LeaseUntil, long Generation,
    InvoiceRecoverySafeReason LastReason);

public sealed class InvoiceRecoveryConflictException(string message)
    : InvalidOperationException(message);

public interface IInvoiceRecoveryQueue
{
    Task<InvoiceOperation> PrepareAndEnqueueAsync(InvoiceSubmission submission,
        string? credentialReference, CancellationToken cancellationToken = default);

    Task<InvoiceOperation> ScheduleAsync(string tenantId, string idempotencyKey,
        long expectedInvoiceVersion, string? credentialReference,
        CancellationToken cancellationToken = default);

    Task<InvoiceRecoveryLease?> TryClaimAsync(InvoiceRecoveryScope scope,
        TimeSpan leaseDuration, CancellationToken cancellationToken = default);

    Task<bool> IsCurrentAsync(InvoiceRecoveryLease lease,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(InvoiceRecoveryLease lease,
        InvoiceRecoveryDisposition disposition, InvoiceRecoverySafeReason safeReason,
        DateTimeOffset? nextAvailable = null,
        CancellationToken cancellationToken = default);

    Task<InvoiceRecoveryMetadata?> FindAsync(InvoiceRecoveryScope scope,
        string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IInvoiceRecoveryContextResolver
{
    ValueTask<ArcaTenantContext> ResolveAsync(InvoiceRecoveryWorkItem workItem,
        CancellationToken cancellationToken = default);
}

public sealed record InvoiceRecoveryWorkerOptions(
    TimeSpan LeaseDuration, TimeSpan IdleInterval, int MaxAttempts,
    TimeSpan BaseBackoff, TimeSpan MaxBackoff, double JitterRatio);

public sealed class InvoiceRecoveryWorkerOptionsBuilder
{
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan IdleInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxAttempts { get; set; } = 10;
    public TimeSpan BaseBackoff { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromMinutes(5);
    public double JitterRatio { get; set; } = 0.2;
}

public interface IInvoiceRecoveryProcessor
{
    Task<bool> RunOnceAsync(InvoiceRecoveryScope scope,
        CancellationToken cancellationToken = default);
}

public static IServiceCollection AddNetArcaWsInvoiceRecoveryWorker(
    this IServiceCollection services, InvoiceRecoveryScope scope,
    Action<InvoiceRecoveryWorkerOptionsBuilder>? configure = null);
```

The API types above live in the optional EF package, including `IInvoiceRecoveryQueue`, `InvoiceRecoveryScope`, lease/work item/metadata/disposition/safe-reason types, and `IInvoiceRecoveryContextResolver`. `AddNetArcaWsInvoiceRecoveryWorker(...)` registers the processor and its optional hosted loop only when explicitly called. The scope constructor copies the enumerable into an immutable set, validates strict UTF-16 tenant input and service enum values, rejects empty/unsupported service selections, and exposes no mutable backing collection. Every public worker contract overrides `ToString()` to redact tenant, key, CUIT/identity, payload hash and credential reference. Queue tenant, key, and credential-reference values are at most 512 UTF-16 code units and must be valid Unicode; no raw value is emitted in exceptions/logs. `FindAsync` returns safe metadata only.

`PrepareAndEnqueueAsync` delegates to an internal shared EF context helper that atomically creates/replays the existing invoice row, series reservation and queue job in one transaction. The public `IInvoiceJournal` stays unchanged. `ScheduleAsync` never changes fiscal state or payload; it verifies exact current invoice version and compatible current snapshot before explicitly reactivating a completed/suspended job. After a rejected invoice is explicitly revised, it accepts and pins the new current snapshot/version. It may schedule only Prepared, Unknown, or expired Submitting/Reconciling; it rejects Authorized, Rejected (until explicitly revised), Conflict, ManualReview, and any job with an active lease. Scheduling does not call SOAP.

## Persistence and claim behavior

`EfInvoiceJournal<TContext>` becomes partial and implements `IInvoiceRecoveryQueue` through `EfInvoiceRecoveryQueue.cs`; existing `PrepareAsync` behavior remains and both paths call a shared `PrepareInContextAsync` helper using the existing context/transaction. Add one internal `InvoiceRecoveryJobEntity` and one table `NetArcaInvoiceRecoveryJobs`, keyed uniquely by `(TenantHash, KeyHash)`. Its columns contain job state, generation, claim ID, lease expiry, available time, attempt count, last safe reason and the pinned snapshot metadata above. It has no fiscal payload copy and no credentials beyond opaque `CredentialReference`.

Claims use a short provider transaction and conditional update. Every claim increments `Generation`, creates a new `ClaimId`, and sets lease expiry. Claim duration must be greater than zero and at most 30 minutes. Queue completion/rescheduling/suspension requires matching tenant/key, claim ID, generation and unexpired lease. `IsCurrentAsync` checks those fields and expiry. If a prior lease expired and the journal is still `Prepared`, the same claim transaction CAS-updates it to `Unknown` and increments invoice version before returning a reconciliation claim. Jobs whose snapshot no longer matches the journal are suspended with safe reason `IncompatibleSnapshot`.

The queue `Scope` filters by tenant and selected recovery services; no claim can cross tenant or service scope. No exception message or arbitrary string from a service/host enters `LastReason` or logs. Operational diagnostics report only safe reason enum, state, attempt, timestamps and generation; do not log raw exception messages or scope/identity data.

## Processor, host authorization, and recovery policy

The optional processor lives in `NetArcaWs.EntityFrameworkCore`, not the core package. `RunOnceAsync` claims one job, validates the fence before resolution, calls `IInvoiceRecoveryContextResolver.ResolveAsync`, checks resolved tenant, represented CUIT and environment against the stored fiscal identity, verifies the resolved service is in the selected scope, then validates the fence again before calling `SafeInvoiceService`.

The host resolver owns authorization and resolves the immutable pinned `CredentialReference` to the expected certificate version. It must reject a removed/revoked or unauthorized version; it must not silently substitute a newer credential after rotation. It returns the authorized `ArcaTenantContext`. The library never infers tenant authorization from user payload or journal contents.

For a current `Prepared` operation, processor calls existing `SafeInvoiceService.ResumeAsync`. Before that, public static typed helpers `SafeInvoiceService.CreateWsfeSubmission(ArcaTenantContext tenant, string key, FecaeRequest request)`, `CreateWsfexSubmission(ArcaTenantContext tenant, string key, ClsFexRequest request)`, and `CreateWsmtxcaSubmission(ArcaTenantContext tenant, string key, ComprobanteType request)` expose the same validation/freeze/canonicalization used by `Authorize*Async` so consumers can prepare and atomically enqueue without SOAP. An optional wrapper overload accepts `FecaeSolicitar` and extracts its `FeCaeReq`; these helpers do not alter canonicalization. For `Unknown`, expired `Submitting`, or expired `Reconciling`, it calls `ReconcileAsync` on that same invoice. `Authorized`/`Rejected` end the job; preserve the current ADR rule that `Rejected` is terminal and releases the series reservation, and do not alter that existing behavior. `Conflict`/`ManualReview` become suspended/manual-review and do not poll-loop. Empty/negative or unavailable queries reschedule reconciliation with capped exponential backoff and bounded jitter. After 10 attempts by default, suspend and require explicit `ScheduleAsync` to resume. Validate options: lease `(0, 30m]`, idle interval `(0, 1d]`, base backoff `(0, max]`, maximum backoff `(0, 1d]`, max attempts `1..1000`, and finite jitter ratio `0..1`. A lease expiry while `Prepared` is atomically made `Unknown` before processing and therefore always queries. No state except fresh `Prepared` while its current queue claim is valid can authorize a submission call.

Cancellation before SOAP releases/reschedules if fence remains current. Cancellation or timeout during SOAP leaves reconciliation work; it never authorizes a resend. A response received before persistence failure may be retained as journal evidence under existing SafeInvoiceService behavior, but a stale queue fence cannot overwrite a newer claim. Do not add retry around SOAP writes.

## Modular configuration, dependencies, and migrations

Add `NetArcaWsModelOptionsBuilder.AddInvoiceRecovery(params ArcaService[] services)` independently from `AddInvoicing`. At runtime validate that every recovery service has the matching invoice journal service selected. Register only the selected invoice and recovery services; recovery configuration alone must not create invoice tables or resolve a queue implementation.

Add a new migration module `InvoiceRecovery`, new history table `__NetArcaWsInvoiceRecoveryMigrations`, and migration ID `20261007000400_InitialInvoiceRecovery`. The recovery-only context must generate only the queue table. Recovery-only migration context creates only the queue table. Existing migrations remain byte-identical. Update five provider packages (SQLite, MySQL, MariaDB, PostgreSQL, SQL Server), module ownership/preflight, migration generation/native DDL and snapshots. The current total changes from 20 to 25 native DDL artifacts and from 15 to 20 migration IDs. The five all-modules DDL artifacts change to add the queue table; the other 15 existing DDL artifacts and all 15 historical migration SQL/scripts remain byte-identical. No migration runs on DI registration or host startup.

Add a direct `Microsoft.Extensions.Hosting.Abstractions` 10.0.12 reference only to the optional EF package for its hosted worker; check its MIT license and update its lockfile deliberately. Keep the worker and EF integration out of core, add no new direct Hosting reference or EF dependency to core, and preserve its existing dependency closure (which already includes Hosting.Abstractions transitively through Microsoft.Extensions.Http). Do not introduce a second persistence library, paid service, Python, WCF or new provider.

## Verificación y límites

Las pruebas locales sintéticas verifican atomicidad, unicidad, scope tenant/servicio, versionado, vencimiento de leases, fencing y transiciones. Agregar pruebas de claim/reinicio entre procesos usando el probe y fixtures existentes: PostgreSQL, SQL Server, MySQL y MariaDB, además de SQLite. Usar transportes SOAP falsos y datos sintéticos. No acreditan homologación ARCA. Verificar instalación limpia y upgrade/preservación en los cinco motores, selección solo del módulo de recuperación y fallas/cancelación; respetar la semántica DDL de cada motor (MySQL/MariaDB pueden dejar DDL parcial y deben bloquear reintento ciego mediante preflight, sin afirmar rollback). Mantener registro opt-in y no configurar credenciales reales ni llamar ARCA.
