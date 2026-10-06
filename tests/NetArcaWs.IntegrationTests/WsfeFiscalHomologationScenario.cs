using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;

namespace NetArcaWs.IntegrationTests;

/// <summary>Explicit, single-voucher WSFE scenario. Calling this method can issue a test invoice.</summary>
public static class WsfeFiscalHomologationScenario
{
    public sealed record Result(bool AlreadyExisted, InvoiceState State);

    public static async Task<Result> RunAsync(IServiceProvider provider, ArcaTenantContext tenant,
        int pointOfSale, int voucherType, long voucherNumber, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(tenant);
        if (tenant.Environment != ArcaEnvironment.Homologation)
            throw new InvalidOperationException("Fiscal homologation scenario requires the Homologation environment.");
        if (voucherType is not (6 or 11))
            throw new ArgumentOutOfRangeException(nameof(voucherType), "Only ordinary B (6) and C (11) invoices are supported.");
        if (pointOfSale is <= 0 or > 99999) throw new ArgumentOutOfRangeException(nameof(pointOfSale));
        if (voucherNumber is <= 0 or > 99999999) throw new ArgumentOutOfRangeException(nameof(voucherNumber), "Specify a valid voucher number explicitly; it is never selected automatically.");

        var wsfe = provider.GetRequiredService<IWsfev1Service>();
        var invoices = provider.GetRequiredService<SafeInvoiceService>();
        var posResponse = await wsfe.FEParamGetPtosVentaAsync(tenant, new FeParamGetPtosVenta(), ct).ConfigureAwait(false);
        var posResult = posResponse.FeParamGetPtosVentaResult
            ?? throw new InvalidOperationException("ARCA did not return point-of-sale data.");
        ThrowIfErrors(posResult.Errors.Count);
        var pos = posResult.ResultGet.SingleOrDefault(x => x?.Nro == pointOfSale);
        bool activeClosureDate = string.IsNullOrWhiteSpace(pos?.FchBaja) || pos.FchBaja == "00000000";
        if (pos is null || !string.Equals(pos.Bloqueado, "N", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(pos.EmisionTipo, "CAE", StringComparison.OrdinalIgnoreCase) || !activeClosureDate)
            throw new InvalidOperationException("The requested point of sale is not active and enabled for CAE electronic invoices.");

        var lastResponse = await wsfe.FECompUltimoAutorizadoAsync(tenant,
            new FeCompUltimoAutorizado { PtoVta = pointOfSale, CbteTipo = voucherType }, ct).ConfigureAwait(false);
        var last = lastResponse.FeCompUltimoAutorizadoResult
            ?? throw new InvalidOperationException("ARCA did not return the last authorized voucher number.");
        ThrowIfErrors(last.Errors.Count);
        if (last.PtoVta != pointOfSale || last.CbteTipo != voucherType || last.CbteNro < 0)
            throw new InvalidOperationException("ARCA returned invalid last-number data.");

        var clock = provider.GetService<TimeProvider>() ?? TimeProvider.System;
        var date = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(-3)).ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var expected = CreateRequest(pointOfSale, voucherType, voucherNumber, date);
        var key = Key(tenant, pointOfSale, voucherType, voucherNumber);
        if (voucherNumber <= last.CbteNro)
        {
            var priorResponse = await wsfe.FECompConsultarAsync(tenant, Query(pointOfSale, voucherType, voucherNumber), ct).ConfigureAwait(false);
            var priorResult = priorResponse.FeCompConsultarResult
                ?? throw new InvalidOperationException("ARCA did not return exact voucher consultation data.");
            ThrowIfErrors(priorResult.Errors.Count);
            var prior = priorResult.ResultGet
                ?? throw new InvalidOperationException("Existing voucher identity could not be confirmed; no invoice was sent.");
            AssertExact(prior, expected);
            return new(true, InvoiceState.Authorized);
        }

        if (voucherNumber != (long)last.CbteNro + 1)
            throw new InvalidOperationException("The explicit voucher number is not the next available number; no invoice was sent.");

        InvoiceOperation operation;
        bool reconciled = false;
        try
        {
            operation = await invoices.AuthorizeWsfeAsync(tenant, key, expected, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            operation = await invoices.ReconcileAsync(tenant, key, ct).ConfigureAwait(false);
            reconciled = true;
        }
        catch (TimeoutException)
        {
            operation = await invoices.ReconcileAsync(tenant, key, ct).ConfigureAwait(false);
            reconciled = true;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            operation = await invoices.ReconcileAsync(tenant, key, ct).ConfigureAwait(false);
            reconciled = true;
        }
        if (operation.State == InvoiceState.Unknown && !reconciled)
            operation = await invoices.ReconcileAsync(tenant, key, ct).ConfigureAwait(false);
        if (operation.State != InvoiceState.Authorized)
            throw new InvalidOperationException($"Fiscal operation ended in state {operation.State}; consult the same fiscal identity before taking further action.");

        var verifiedResponse = await wsfe.FECompConsultarAsync(tenant, Query(pointOfSale, voucherType, voucherNumber), ct).ConfigureAwait(false);
        var verifiedResult = verifiedResponse.FeCompConsultarResult
            ?? throw new InvalidOperationException("ARCA did not return exact voucher consultation data after authorization.");
        ThrowIfErrors(verifiedResult.Errors.Count);
        var verified = verifiedResult.ResultGet
            ?? throw new InvalidOperationException("The authorized voucher could not be confirmed by exact fiscal identity.");
        AssertExact(verified, expected);
        if (!ValidCae(operation.AuthorizationCode) || !string.Equals(operation.AuthorizationCode, verified.CodAutorizacion, StringComparison.Ordinal))
            throw new InvalidOperationException("Journal authorization code did not match the exact ARCA voucher consultation.");
        return new(false, operation.State);
    }

    private static void ThrowIfErrors(int count)
    {
        if (count != 0) throw new InvalidOperationException("ARCA returned errors; fiscal issuance was not continued.");
    }

    private static string Key(ArcaTenantContext tenant, int pos, int type, long number)
        => $"homologation-wsfe-{tenant.Cuit}-{pos}-{type}-{number}";

    private static FeCompConsultar Query(int pos, int type, long number) => new()
    {
        FeCompConsReq = new FeCompConsultaReq { PtoVta = pos, CbteTipo = type, CbteNro = number }
    };

    private static FecaeRequest CreateRequest(int pos, int type, long number, string date)
    {
        var isB = type == 6;
        double net = isB ? 100 : 121;
        double iva = isB ? 21 : 0;
        var detail = new FecaeDetRequest
        {
            Concepto = 1, DocTipo = 99, DocNro = 0, CbteDesde = number, CbteHasta = number,
            CbteFch = date, ImpTotal = 121, ImpTotConc = 0, ImpNeto = net, ImpOpEx = 0,
            ImpTrib = 0, ImpIva = iva, MonId = "PES", MonCotiz = 1, CondicionIvaReceptorId = 5
        };
        if (isB) detail.Iva.Add(new AlicIva { Id = 5, BaseImp = 100, Importe = 21 });
        var request = new FecaeRequest
        {
            FeCabReq = new FecaeCabRequest { CantReg = 1, PtoVta = pos, CbteTipo = type }
        };
        request.FeDetReq.Add(detail);
        return request;
    }

    private static void AssertExact(FeCompConsResponse actual, FecaeRequest expected)
    {
        var cab = expected.FeCabReq;
        var sent = expected.FeDetReq.Single();
        bool ivaMatches = expected.FeDetReq[0].Iva.Count == 1
            ? actual.Iva.Count == 1 && actual.Iva[0].Id == 5 && actual.Iva[0].BaseImp == 100 && actual.Iva[0].Importe == 21
            : actual.Iva.Count == 0;
        bool match = actual.PtoVta == cab.PtoVta && actual.CbteTipo == cab.CbteTipo &&
            actual.CbteDesde == sent.CbteDesde && actual.CbteHasta == sent.CbteHasta &&
            actual.DocTipo == 99 && actual.DocNro == 0 && actual.Concepto == 1 &&
            actual.CbteFch == sent.CbteFch && actual.ImpTotal == 121 &&
            actual.ImpNeto == sent.ImpNeto && actual.ImpIva == sent.ImpIva &&
            actual.ImpTotConc == 0 && actual.ImpOpEx == 0 && actual.ImpTrib == 0 &&
            actual.MonId == "PES" && actual.MonCotiz == 1 && actual.CondicionIvaReceptorId == 5 &&
            ivaMatches && actual.Resultado == "A" && actual.EmisionTipo == "CAE" && ValidCae(actual.CodAutorizacion);
        if (!match) throw new InvalidOperationException("Exact consultation did not confirm the expected authorized invoice values and identity.");
    }

    private static bool ValidCae(string? cae)
        => cae is { Length: 14 } && cae.All(char.IsAsciiDigit) && cae.Any(c => c != '0');
}
