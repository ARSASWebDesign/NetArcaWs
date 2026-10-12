using System.Security.Cryptography;
using System.Text;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;

namespace NetArcaWs.IntegrationTests;

internal sealed record FiscalHomologationScenario(string Service, int VoucherType, string Name, string? OriginalScenarioName = null);
/// <param name="FixtureReviewed">Caller confirms the configured fixture was reviewed; this does not prove ARCA enrollment or current tax status.</param>
internal sealed record FiscalHomologationReceiver(int DocumentType, long DocumentNumber, int? VatConditionId,
    bool FixtureReviewed = false);
internal sealed record FiscalHomologationCaseResult(string ScenarioName, string Service, int VoucherType, int PointOfSale, long Number,
    bool Reused, InvoiceState State);

/// <summary>One service's generated wire contract and exact-query rules.</summary>
internal interface IFiscalHomologationChannel
{
    string Service { get; }
    bool Supports(int voucherType);
    bool CanAssociate(int noteType, int originalType);
    void ValidateScenario(FiscalHomologationScenario scenario, int pointOfSale, FiscalHomologationReceiver receiver);
    void ValidateFrozenSubmission(InvoiceSubmission frozen, FiscalHomologationScenario scenario, int pointOfSale,
        FiscalHomologationReceiver receiver, InvoiceIdentity? associatedInvoice);
    Task<bool> IsVoucherTypeAvailableAsync(ArcaTenantContext tenant, int voucherType, CancellationToken cancellationToken);
    Task<long> QueryLastAuthorizedAsync(ArcaTenantContext tenant, int pointOfSale, int voucherType, CancellationToken cancellationToken);
    InvoiceSubmission CreateSubmission(ArcaTenantContext tenant, string key, int pointOfSale, int voucherType,
        long number, FiscalHomologationReceiver receiver, InvoiceIdentity? associatedInvoice);
    Task<InvoiceOperation> SubmitOrResumeAsync(ArcaTenantContext tenant, string key, InvoiceSubmission frozen,
        CancellationToken cancellationToken);
    Task<InvoiceOperation> ReconcileAsync(ArcaTenantContext tenant, string key, CancellationToken cancellationToken);
    Task VerifyExactAsync(ArcaTenantContext tenant, InvoiceOperation operation, CancellationToken cancellationToken);
}

/// <summary>
/// Explicitly selected offline/opt-in invoice matrix. No service request is made until a typed channel has
/// validated catalogue availability and the frozen submission is durable in the journal.
/// </summary>
internal static class FiscalHomologationMatrix
{
    private static readonly SemaphoreSlim[] SeriesGates = Enumerable.Range(0, 128).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    internal static async Task<IReadOnlyList<FiscalHomologationCaseResult>> RunAsync(
        string runId,
        IReadOnlyList<FiscalHomologationScenario> scenarios,
        ArcaTenantContext tenant,
        IInvoiceJournal journal,
        ProtectedHomologationStateSession state,
        IReadOnlyDictionary<string, IFiscalHomologationChannel> channels,
        IReadOnlyDictionary<string, int> channelPointsOfSale,
        FiscalHomologationReceiver receiver,
        CancellationToken cancellationToken,
        Func<FiscalHomologationCaseResult, CancellationToken, Task>? onCaseCompleted = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentNullException.ThrowIfNull(scenarios);
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(channels);
        ArgumentNullException.ThrowIfNull(channelPointsOfSale);
        ArgumentNullException.ThrowIfNull(receiver);
        if (tenant.Environment != ArcaEnvironment.Homologation)
            throw new InvalidOperationException("Fiscal matrix requires the Homologation tenant context.");
        if (scenarios.Count == 0) throw new ArgumentException("At least one explicit fiscal scenario is required.", nameof(scenarios));
        if (runId.Length > 128 || receiver.DocumentType <= 0 || receiver.DocumentNumber < 0 || !receiver.FixtureReviewed)
            throw new ArgumentException("A typed receiver fixture is required.", nameof(receiver));
        if (journal is not ProtectedSqliteInvoiceJournal protectedJournal)
            throw new InvalidOperationException("Fiscal matrix requires the protected SQLite invoice journal.");
        // Fail closed before any channel catalogue/number query if a prior protected-state write failed.
        _ = await state.ListVerifiedSeriesAsync(cancellationToken).ConfigureAwait(false);

        var scenariosByName = new Dictionary<string, FiscalHomologationScenario>(StringComparer.Ordinal);
        foreach (FiscalHomologationScenario selected in scenarios)
        {
            if (string.IsNullOrWhiteSpace(selected.Name) || !scenariosByName.TryAdd(selected.Name, selected))
                throw new ArgumentException("Scenario names must be nonempty and unique within a matrix run.", nameof(scenarios));
            if (!channels.TryGetValue(selected.Service, out IFiscalHomologationChannel? selectedChannel) || !selectedChannel.Supports(selected.VoucherType))
                throw new InvalidOperationException($"Scenario '{selected.Name}' is unavailable: '{selected.Service}/{selected.VoucherType}' has no supported typed channel.");
            if (!string.Equals(selectedChannel.Service, selected.Service, StringComparison.Ordinal))
                throw new InvalidOperationException($"Scenario '{selected.Name}' selected service '{selected.Service}' but its channel identifies as '{selectedChannel.Service}'.");
            if (!channelPointsOfSale.TryGetValue(selected.Service, out int selectedPos) || selectedPos is <= 0 or > 99999)
                throw new InvalidOperationException($"Scenario '{selected.Name}' requires an explicit point of sale for '{selected.Service}'.");
        }
        foreach (FiscalHomologationScenario selected in scenarios)
        {
            if (selected.OriginalScenarioName is null) continue;
            if (!scenariosByName.TryGetValue(selected.OriginalScenarioName, out FiscalHomologationScenario? original) ||
                !string.Equals(original.Service, selected.Service, StringComparison.Ordinal) ||
                !channels[selected.Service].CanAssociate(selected.VoucherType, original.VoucherType))
                throw new InvalidOperationException($"Note scenario '{selected.Name}' must select an earlier invoice from the same service family.");
            if (Array.IndexOf(scenarios.ToArray(), selected) < Array.IndexOf(scenarios.ToArray(), original))
                throw new InvalidOperationException($"Original invoice scenario '{original.Name}' must precede note '{selected.Name}'.");
        }
        foreach (FiscalHomologationScenario selected in scenarios)
            channels[selected.Service].ValidateScenario(selected, channelPointsOfSale[selected.Service], receiver);
        foreach (FiscalHomologationScenario selected in scenarios)
            if (!await channels[selected.Service].IsVoucherTypeAvailableAsync(tenant, selected.VoucherType, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException($"Scenario '{selected.Name}' is absent from the runtime service catalogue.");

        var results = new List<FiscalHomologationCaseResult>(scenarios.Count);
        foreach (FiscalHomologationScenario scenario in scenarios)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!channels.TryGetValue(scenario.Service, out IFiscalHomologationChannel? channel))
                throw new InvalidOperationException($"Scenario '{scenario.Name}' is unavailable: service '{scenario.Service}' has no typed channel adapter.");
            if (!channel.Supports(scenario.VoucherType))
                throw new InvalidOperationException($"Scenario '{scenario.Name}' is unsupported by service '{scenario.Service}'.");
            if (!channelPointsOfSale.TryGetValue(scenario.Service, out int pos) || pos is <= 0 or > 99999)
                throw new InvalidOperationException($"Scenario '{scenario.Name}' requires an explicit point of sale for '{scenario.Service}'.");

            string key = StableKey(runId, tenant, scenario);
            string seriesIdentity = $"{tenant.Environment}:{tenant.Cuit}:{pos}:{scenario.VoucherType}";
            SemaphoreSlim gate = SeriesGates[(StringComparer.Ordinal.GetHashCode(seriesIdentity) & int.MaxValue) % SeriesGates.Length];
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                InvoiceIdentity? association = null;
                if (scenario.OriginalScenarioName is { } originalName)
                {
                    FiscalHomologationCaseResult verifiedOriginal = results.Single(result => result.ScenarioName == originalName);
                    association = new(tenant.Environment, tenant.Cuit, verifiedOriginal.PointOfSale, verifiedOriginal.VoucherType, verifiedOriginal.Number);
                }
                InvoiceOperation? existing = await journal.FindAsync(tenant.TenantId, key, cancellationToken).ConfigureAwait(false);
                if (existing is not null)
                {
                    if (existing.Submission.Service != channel.Service || existing.Submission.Identity.PointOfSale != pos ||
                        existing.Submission.Identity.VoucherType != scenario.VoucherType || existing.Submission.Identity.Environment != tenant.Environment ||
                        existing.Submission.Identity.Cuit != tenant.Cuit)
                        throw new InvoiceConflictException("The stable scenario key is already bound to a different fiscal identity.");
                    channel.ValidateFrozenSubmission(existing.Submission, scenario, pos, receiver, association);
                    if (existing.State == InvoiceState.Authorized)
                    {
                        await channel.VerifyExactAsync(tenant, existing, cancellationToken).ConfigureAwait(false);
                        await RecordVerifiedSummaryAsync(state, channel.Service, tenant, pos, scenario.VoucherType,
                            existing.Submission.Identity.VoucherNumber, cancellationToken).ConfigureAwait(false);
                        await ReportResultAsync(results, onCaseCompleted,
                            new(scenario.Name, channel.Service, scenario.VoucherType, pos, existing.Submission.Identity.VoucherNumber, true, existing.State), cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    if (existing.State is not (InvoiceState.Prepared or InvoiceState.Unknown or InvoiceState.Conflict or
                        InvoiceState.ManualReview or InvoiceState.Submitting or InvoiceState.Reconciling))
                        throw new InvalidOperationException($"Scenario '{scenario.Name}' remains {existing.State}; no automatic revision or resend is allowed.");
                    if (existing.State != InvoiceState.Prepared)
                    {
                        if (await protectedJournal.HasUnresolvedOperationsAsync(tenant.TenantId, key, cancellationToken).ConfigureAwait(false))
                            throw new InvalidOperationException("Another fiscal operation is unresolved; reconcile it before continuing the matrix.");
                        InvoiceOperation reconciled = await channel.ReconcileAsync(tenant, key, cancellationToken).ConfigureAwait(false);
                        if (reconciled.State != InvoiceState.Authorized)
                            throw new InvalidOperationException($"Scenario '{scenario.Name}' remains {reconciled.State}; matrix advancement is stopped.");
                        await channel.VerifyExactAsync(tenant, reconciled, cancellationToken).ConfigureAwait(false);
                        await RecordVerifiedSummaryAsync(state, channel.Service, tenant, pos, scenario.VoucherType,
                            reconciled.Submission.Identity.VoucherNumber, cancellationToken).ConfigureAwait(false);
                        await ReportResultAsync(results, onCaseCompleted, new(scenario.Name, channel.Service,
                            scenario.VoucherType, pos, reconciled.Submission.Identity.VoucherNumber, true, reconciled.State), cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    if (await protectedJournal.HasUnresolvedOperationsAsync(tenant.TenantId, key, cancellationToken).ConfigureAwait(false))
                        throw new InvalidOperationException("Another fiscal operation is unresolved; reconcile it before resuming this prepared scenario.");
                    long remoteLast = await channel.QueryLastAuthorizedAsync(tenant, pos, scenario.VoucherType, cancellationToken).ConfigureAwait(false);
                    if (existing.Submission.Identity.VoucherNumber != remoteLast + 1)
                        throw new InvalidOperationException($"Prepared scenario '{scenario.Name}' is no longer the immediate next number; it remains frozen.");
                    InvoiceOperation resumed = await channel.SubmitOrResumeAsync(tenant, key, existing.Submission, cancellationToken).ConfigureAwait(false);
                    if (resumed.State != InvoiceState.Authorized)
                        throw new InvalidOperationException($"Scenario '{scenario.Name}' ended in {resumed.State}; matrix advancement is stopped.");
                    await channel.VerifyExactAsync(tenant, resumed, cancellationToken).ConfigureAwait(false);
                    await RecordVerifiedSummaryAsync(state, channel.Service, tenant, pos, scenario.VoucherType,
                        resumed.Submission.Identity.VoucherNumber, cancellationToken).ConfigureAwait(false);
                    await ReportResultAsync(results, onCaseCompleted, new(scenario.Name, channel.Service, scenario.VoucherType,
                        pos, resumed.Submission.Identity.VoucherNumber, true, resumed.State), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (await protectedJournal.HasUnresolvedOperationsAsync(tenant.TenantId, cancellationToken: cancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException("A prior matrix fiscal operation is unresolved; no new number was selected.");
                long last = await channel.QueryLastAuthorizedAsync(tenant, pos, scenario.VoucherType, cancellationToken).ConfigureAwait(false);
                if (last < 0 || last >= 99999999) throw new InvalidOperationException("Service returned an invalid last authorized number.");
                long next = last + 1;
                InvoiceSubmission submission = channel.CreateSubmission(tenant, key, pos, scenario.VoucherType, next, receiver, association);
                if (submission.Identity.VoucherNumber != next || submission.Identity.PointOfSale != pos ||
                    submission.Identity.VoucherType != scenario.VoucherType || submission.Identity.Cuit != tenant.Cuit ||
                    submission.Identity.Environment != tenant.Environment || submission.IdempotencyKey != key)
                    throw new InvoiceConflictException("Typed channel produced a frozen submission with a different selected fiscal identity.");
                _ = await journal.PrepareAsync(submission, cancellationToken).ConfigureAwait(false);
                long immediateLast = await channel.QueryLastAuthorizedAsync(tenant, pos, scenario.VoucherType, cancellationToken).ConfigureAwait(false);
                if (immediateLast != last)
                    throw new InvalidOperationException($"Fiscal series advanced after number selection for '{scenario.Name}'; the frozen number remains reserved and no alternate number was selected.");
                InvoiceOperation sent = await channel.SubmitOrResumeAsync(tenant, key, submission, cancellationToken).ConfigureAwait(false);
                if (sent.State != InvoiceState.Authorized)
                    throw new InvalidOperationException($"Scenario '{scenario.Name}' ended in {sent.State}; matrix advancement is stopped.");
                await channel.VerifyExactAsync(tenant, sent, cancellationToken).ConfigureAwait(false);
                await RecordVerifiedSummaryAsync(state, channel.Service, tenant, pos, scenario.VoucherType, next, cancellationToken).ConfigureAwait(false);
                await ReportResultAsync(results, onCaseCompleted,
                    new(scenario.Name, channel.Service, scenario.VoucherType, pos, next, false, sent.State), cancellationToken).ConfigureAwait(false);
            }
            finally { gate.Release(); }
        }
        return results;
    }

    private static async Task ReportResultAsync(List<FiscalHomologationCaseResult> results,
        Func<FiscalHomologationCaseResult, CancellationToken, Task>? callback,
        FiscalHomologationCaseResult result, CancellationToken cancellationToken)
    {
        results.Add(result);
        if (callback is not null) await callback(result, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RecordVerifiedSummaryAsync(ProtectedHomologationStateSession state, string service,
        ArcaTenantContext tenant, int pos, int type, long number, CancellationToken cancellationToken)
    {
        ProtectedHomologationSeriesSummary? previous = await state.FindVerifiedSeriesAsync(service, tenant.Environment,
            tenant.Cuit, pos, type, cancellationToken).ConfigureAwait(false);
        await state.RecordVerifiedSeriesAsync(new(service, tenant.Environment, tenant.Cuit, pos, type,
            Math.Max(number, previous?.LastAuthorizedNumber ?? 0), TimeProvider.System.GetUtcNow()), cancellationToken).ConfigureAwait(false);
    }

    private static string StableKey(string runId, ArcaTenantContext tenant, FiscalHomologationScenario scenario)
    {
        string canonical = $"{runId}\n{tenant.TenantId}\n{tenant.Environment}\n{tenant.Cuit}\n{scenario.Service}\n{scenario.VoucherType}\n{scenario.Name}";
        return "homologation-matrix-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
