using System.Globalization;
using System.Xml.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;

namespace NetArcaWs.IntegrationTests;

/// <summary>Fixed, synthetic matrix choices. Runtime ARCA catalogues and eligibility remain authoritative.</summary>
internal static class FiscalHomologationMatrixProfile
{
    internal readonly record struct PointOfSaleChoice(int PointOfSale, bool UsesSyntheticFixture);
    private sealed record PointOfSaleCatalogue(IReadOnlyList<int> ActivePoints, bool RecognizedEmpty);

    internal static PointOfSaleChoice SelectPointOfSale(string service, IEnumerable<int> activePoints,
        bool recognizedEmptyCatalogue, ArcaEnvironment environment, bool allowFixedQaFixture,
        int? frozenPointOfSale = null, int? reservedPointOfSale = null)
    {
        ArgumentNullException.ThrowIfNull(activePoints);
        int fixture = service switch
        {
            "wsfe" => 1,
            "wsfex" => 1,
            "wsmtxca" => 2,
            _ => throw new InvalidOperationException("No POS fixture is defined for this service.")
        };
        int[] active = activePoints.Where(x => x > 0).Distinct().Order().ToArray();
        if (recognizedEmptyCatalogue && active.Length != 0)
            throw new InvalidOperationException($"{service} reported a recognized empty POS catalogue with active entries.");

        if (frozenPointOfSale is { } frozen)
        {
            bool validActive = active.Contains(frozen);
            bool validFixture = environment == ArcaEnvironment.Homologation && allowFixedQaFixture &&
                recognizedEmptyCatalogue && frozen == fixture;
            if (!validActive && !validFixture)
                throw new InvalidOperationException($"Frozen {service} point of sale {frozen} is not active or the approved empty-catalogue QA fixture.");
            if (reservedPointOfSale == frozen)
                throw new InvalidOperationException($"Frozen {service} POS {frozen} conflicts with another service's fiscal identity.");
            return new(frozen, validFixture && !validActive);
        }

        int selected = active.FirstOrDefault(x => x != reservedPointOfSale);
        if (selected > 0) return new(selected, false);
        if (active.Length != 0)
            throw new InvalidOperationException($"Every active {service} POS conflicts with another service's fiscal identity.");
        if (environment == ArcaEnvironment.Homologation && allowFixedQaFixture && recognizedEmptyCatalogue &&
            reservedPointOfSale != fixture)
            return new(fixture, true);
        throw new InvalidOperationException($"ARCA has no active CAE point of sale available for {service}.");
    }

    // Official QR invoice example identifies this as a test CUIT: https://www.afip.gob.ar/fe/qr/documentos/30000000007_001_00010_00000094.pdf
    // The recipient fixture is synthetic and is never treated as proof of current homologation eligibility.
    internal static readonly FiscalHomologationReceiver RegisteredReceiver = new(80, 20_000_000_001, 1, FixtureReviewed: true);
    internal static readonly FiscalHomologationReceiver ConsumerReceiver = new(99, 0, 5, FixtureReviewed: true);

    internal static IReadOnlyList<FiscalHomologationScenario> AllScenarios { get; } = Array.AsReadOnly(new[]
    {
        new FiscalHomologationScenario("wsfe", 1, "wsfe-a-factura"),
        new FiscalHomologationScenario("wsfe", 2, "wsfe-a-nota-debito", "wsfe-a-factura"),
        new FiscalHomologationScenario("wsfe", 3, "wsfe-a-nota-credito", "wsfe-a-factura"),
        new FiscalHomologationScenario("wsfe", 6, "wsfe-b-factura"),
        new FiscalHomologationScenario("wsfe", 7, "wsfe-b-nota-debito", "wsfe-b-factura"),
        new FiscalHomologationScenario("wsfe", 8, "wsfe-b-nota-credito", "wsfe-b-factura"),
        new FiscalHomologationScenario("wsfe", 11, "wsfe-c-factura"),
        new FiscalHomologationScenario("wsfe", 12, "wsfe-c-nota-debito", "wsfe-c-factura"),
        new FiscalHomologationScenario("wsfe", 13, "wsfe-c-nota-credito", "wsfe-c-factura"),
        new FiscalHomologationScenario("wsfex", 19, "wsfex-e-factura"),
        new FiscalHomologationScenario("wsfex", 20, "wsfex-e-nota-debito", "wsfex-e-factura"),
        new FiscalHomologationScenario("wsfex", 21, "wsfex-e-nota-credito", "wsfex-e-factura"),
        new FiscalHomologationScenario("wsmtxca", 1, "wsmtxca-a-factura"),
        new FiscalHomologationScenario("wsmtxca", 2, "wsmtxca-a-nota-debito", "wsmtxca-a-factura"),
        new FiscalHomologationScenario("wsmtxca", 3, "wsmtxca-a-nota-credito", "wsmtxca-a-factura"),
        new FiscalHomologationScenario("wsmtxca", 6, "wsmtxca-b-factura"),
        new FiscalHomologationScenario("wsmtxca", 7, "wsmtxca-b-nota-debito", "wsmtxca-b-factura"),
        new FiscalHomologationScenario("wsmtxca", 8, "wsmtxca-b-nota-credito", "wsmtxca-b-factura")
    });

    internal static FiscalExportFixture CreateExportFixture(DateOnly paymentDate) => new(
        DestinationCountryCode: 203,
        CustomerCountryTaxCode: 50_000_000_016,
        CustomerName: "Synthetic Export Buyer",
        CustomerAddress: "Synthetic address, testing only",
        ForeignTaxId: "SYNTHETIC-TAX-0001",
        CurrencyId: "PES",
        CurrencyRate: 1m,
        PaymentTerms: "Transferencia bancaria",
        PaymentDate: paymentDate,
        InvoiceLanguage: 1,
        Items: [new FiscalExportItem("SYN-001", "Synthetic testing item", 2m, 50m, 0m)]);

    internal static void ValidatePreflight(string mode, string runId)
    {
        if (mode is not ("consultas" or "emision" or "completa"))
            throw new InvalidOperationException("ARCA_HOMOLOGY_MODE must be consultas, emision, or completa.");
        if (mode is "emision" or "completa")
        {
            if (string.IsNullOrWhiteSpace(runId) || runId.Length > 128)
                throw new InvalidOperationException("The workflow run identifier is missing or invalid.");
            if (AllScenarios.Count != 18 || AllScenarios.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != 18)
                throw new InvalidOperationException("The supported fiscal matrix definition is incomplete.");
            if (!FiscalHomologationWsfeChannel.IsValidCuit(RegisteredReceiver.DocumentNumber) ||
                RegisteredReceiver.DocumentType != 80 || RegisteredReceiver.VatConditionId != 1 || !RegisteredReceiver.FixtureReviewed)
                throw new InvalidOperationException("The synthetic registered-receiver fixture is invalid.");
            FiscalExportFixture export = CreateExportFixture(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10));
            export.Validate();
        }
    }

    internal static async Task<(IReadOnlyDictionary<string, IFiscalHomologationChannel> Channels,
        IReadOnlyDictionary<string, int> PointsOfSale, FiscalExportFixture ExportFixture)> CreateRuntimeChannelsAsync(
        IServiceProvider provider, SafeInvoiceService invoices, IInvoiceJournal journal, ArcaTenantContext tenant,
        string runId, CancellationToken cancellationToken)
    {
        DateOnly paymentDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);
        int? frozenWsfexPos = null;
        foreach (FiscalHomologationScenario scenario in AllScenarios.Where(x => x.Service == "wsfex"))
        {
            InvoiceOperation? existing = await journal.FindAsync(tenant.TenantId,
                FiscalHomologationMatrix.StableKey(runId, tenant, scenario), cancellationToken).ConfigureAwait(false);
            if (existing is null) continue;
            if (frozenWsfexPos is not null && frozenWsfexPos != existing.Submission.Identity.PointOfSale)
                throw new InvalidOperationException("Frozen WSFEX matrix cases use inconsistent point-of-sale identities.");
            frozenWsfexPos = existing.Submission.Identity.PointOfSale;
            using var reader = new StringReader(existing.Submission.Payload);
            var request = (ClsFexRequest)(new XmlSerializer(typeof(ClsFexRequest)).Deserialize(reader)
                ?? throw new InvalidOperationException("Frozen WSFEX fixture cannot be restored."));
            paymentDate = DateOnly.ParseExact(request.FechaPago, "yyyyMMdd", CultureInfo.InvariantCulture);
            break;
        }

        var wsfexFixture = CreateExportFixture(paymentDate);
        var positions = new Dictionary<string, int>(StringComparer.Ordinal);
        var selections = new Dictionary<string, PointOfSaleChoice>(StringComparer.Ordinal);
        foreach (string service in new[] { "wsfe", "wsfex", "wsmtxca" })
        {
            int? frozenPos = null;
            foreach (FiscalHomologationScenario scenario in AllScenarios.Where(x => x.Service == service))
            {
                InvoiceOperation? existing = await journal.FindAsync(tenant.TenantId,
                    FiscalHomologationMatrix.StableKey(runId, tenant, scenario), cancellationToken).ConfigureAwait(false);
                if (existing is null) continue;
                int candidate = existing.Submission.Identity.PointOfSale;
                if (frozenPos is not null && frozenPos != candidate)
                    throw new InvalidOperationException($"Frozen {service} matrix cases use inconsistent point-of-sale identities.");
                frozenPos = candidate;
            }
            PointOfSaleCatalogue catalogue = await GetPointOfSaleCatalogueAsync(provider, service, tenant, cancellationToken).ConfigureAwait(false);
            int? reserved = service == "wsmtxca" ? positions.GetValueOrDefault("wsfe") : null;
            PointOfSaleChoice selection = SelectPointOfSale(service, catalogue.ActivePoints, catalogue.RecognizedEmpty,
                tenant.Environment, allowFixedQaFixture: true, frozenPos, reserved);
            selections.Add(service, selection);
            positions.Add(service, selection.PointOfSale);
        }
        var channels = new Dictionary<string, IFiscalHomologationChannel>(StringComparer.Ordinal)
        {
            ["wsfe"] = new FiscalHomologationWsfeChannel(provider.GetRequiredService<IWsfev1Service>(), invoices,
                RegisteredReceiver, ConsumerReceiver, allowFixedQaPointOfSaleForEmptyCatalogue: selections["wsfe"].UsesSyntheticFixture),
            ["wsfex"] = new FiscalHomologationWsfexChannel(provider.GetRequiredService<IWsfexv1Service>(), invoices,
                wsfexFixture, allowFixedQaPointOfSaleForEmptyCatalogue: selections["wsfex"].UsesSyntheticFixture),
            ["wsmtxca"] = new FiscalHomologationWsmtxcaChannel(provider.GetRequiredService<IWsmtxcav1Service>(), invoices,
                RegisteredReceiver, ConsumerReceiver, allowFixedQaPointOfSaleForEmptyCatalogue: selections["wsmtxca"].UsesSyntheticFixture)
        };
        return (channels, positions, wsfexFixture);
    }

    private static async Task<PointOfSaleCatalogue> GetPointOfSaleCatalogueAsync(IServiceProvider provider, string service,
        ArcaTenantContext tenant, CancellationToken cancellationToken)
    {
        return service switch
        {
            "wsfe" => await GetWsfePointsAsync(provider, tenant, cancellationToken).ConfigureAwait(false),
            "wsfex" => await GetWsfexPointsAsync(provider, tenant, cancellationToken).ConfigureAwait(false),
            "wsmtxca" => await GetWsmtxcaPointsAsync(provider, tenant, cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException("No POS catalogue is supported for this service.")
        };
    }

    private static async Task<PointOfSaleCatalogue> GetWsfePointsAsync(IServiceProvider provider, ArcaTenantContext tenant, CancellationToken ct)
    {
        var response = await provider.GetRequiredService<IWsfev1Service>().FEParamGetPtosVentaAsync(tenant, new FeParamGetPtosVenta(), ct).ConfigureAwait(false);
        var result = response.FeParamGetPtosVentaResult ?? throw new InvalidOperationException("WSFE returned no point-of-sale catalogue.");
        bool empty602 = FiscalHomologationWsfeChannel.IsKnownEmptyQaPointOfSaleCatalogue(result, tenant.Environment);
        if (result.Errors.Count != 0 && !empty602) throw new InvalidOperationException("WSFE point-of-sale catalogue returned errors.");
        return new(result.ResultGet.Where(x => x is not null && x.EmisionTipo == "CAE" && x.Bloqueado == "N" &&
            (string.IsNullOrWhiteSpace(x.FchBaja) || x.FchBaja == "00000000")).Select(x => x!.Nro).ToArray(), empty602);
    }

    private static async Task<PointOfSaleCatalogue> GetWsfexPointsAsync(IServiceProvider provider, ArcaTenantContext tenant, CancellationToken ct)
    {
        var response = await provider.GetRequiredService<IWsfexv1Service>().FEXGetPARAM_PtoVentaAsync(tenant, new FexGetParamPtoVenta(), ct).ConfigureAwait(false);
        var result = response.FexGetParamPtoVentaResult ?? throw new InvalidOperationException("WSFEX returned no point-of-sale catalogue.");
        if (result.FexErr is { ErrCode: not 0 }) throw new InvalidOperationException("WSFEX point-of-sale catalogue returned an error.");
        return new(result.FexResultGet.Where(x => x is not null && x.PveBloqueado == "N" &&
            (string.IsNullOrWhiteSpace(x.PveFchBaja) || x.PveFchBaja == "00000000")).Select(x => x!.PveNro).ToArray(),
            FiscalHomologationWsfexChannel.IsFixedQaPointOfSaleAllowed(result, tenant.Environment, 1, enabled: true));
    }

    private static async Task<PointOfSaleCatalogue> GetWsmtxcaPointsAsync(IServiceProvider provider, ArcaTenantContext tenant, CancellationToken ct)
    {
        var response = await provider.GetRequiredService<IWsmtxcav1Service>().consultarPuntosVentaCAEAsync(tenant,
            new ConsultarPuntosVentaCaeRequestType(), ct).ConfigureAwait(false);
        return new(response.ArrayPuntosVenta.Where(x => x is not null && x.Bloqueado == SiNoSimpleType.N && x.FechaBaja is null)
            .Select(x => x!.NumeroPuntoVenta).ToArray(), tenant.Environment == ArcaEnvironment.Homologation &&
            response.ArrayPuntosVenta.Count == 0 && response.Evento is null);
    }
}
