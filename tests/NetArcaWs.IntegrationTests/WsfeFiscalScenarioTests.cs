using System.Reflection;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class WsfeFiscalScenarioTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Production_context_is_rejected_before_resolving_or_calling_a_client()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            WsfeFiscalHomologationScenario.RunAsync(new ServiceCollection().BuildServiceProvider(), Tenant(ArcaEnvironment.Production), 1, 6, 1, Ct));
        exception.Message.Should().Contain("Homologation");
    }

    [Fact]
    public async Task Unsupported_voucher_type_is_rejected_before_any_service_resolution()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            WsfeFiscalHomologationScenario.RunAsync(new ServiceCollection().BuildServiceProvider(), Tenant(ArcaEnvironment.Homologation), 1, 1, 1, Ct));
        exception.ParamName.Should().Be("voucherType");
    }

    [Theory]
    [InlineData(100000, 1, "pointOfSale")]
    [InlineData(1, 100000000, "voucherNumber")]
    public async Task Out_of_range_fiscal_numbers_are_rejected_before_any_service_resolution(int pos, long number, string parameter)
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            WsfeFiscalHomologationScenario.RunAsync(new ServiceCollection().BuildServiceProvider(), Tenant(ArcaEnvironment.Homologation), pos, 6, number, Ct));
        exception.ParamName.Should().Be(parameter);
    }

    [Theory]
    [InlineData(6, 100d, 21d, 1)]
    [InlineData(11, 121d, 0d, 0)]
    public async Task Ordinary_B_and_C_authorizations_use_the_expected_synthetic_amounts(int type, double net, double iva, int ivaLines)
    {
        await WithFixture(async (fake, provider) =>
        {
            var result = await WsfeFiscalHomologationScenario.RunAsync(provider, Tenant(ArcaEnvironment.Homologation), 9001, type, 1, Ct);
            result.AlreadyExisted.Should().BeFalse();
            result.State.Should().Be(InvoiceState.Authorized);
            fake.SubmissionCount.Should().Be(1);
            fake.LastSubmitted!.FeDetReq.Single().ImpTotal.Should().Be(121);
            fake.LastSubmitted.FeDetReq.Single().ImpNeto.Should().Be(net);
            fake.LastSubmitted.FeDetReq.Single().ImpIva.Should().Be(iva);
            fake.LastSubmitted.FeDetReq.Single().Iva.Count.Should().Be(ivaLines);
        });
    }

    [Fact]
    public async Task Repeating_an_explicit_invoice_consults_existing_record_without_resubmitting()
    {
        await WithFixture(async (fake, provider) =>
        {
            var tenant = Tenant(ArcaEnvironment.Homologation);
            var first = await WsfeFiscalHomologationScenario.RunAsync(provider, tenant, 9001, 6, 1, Ct);
            var second = await WsfeFiscalHomologationScenario.RunAsync(provider, tenant, 9001, 6, 1, Ct);
            first.State.Should().Be(InvoiceState.Authorized);
            second.AlreadyExisted.Should().BeTrue();
            second.State.Should().Be(InvoiceState.Authorized);
            fake.SubmissionCount.Should().Be(1);
            fake.ExactConsultationCount.Should().Be(2);
        });
    }

    [Fact]
    public async Task Mismatched_preexisting_invoice_fails_closed_without_submission()
    {
        await WithFixture(async (fake, provider) =>
        {
            fake.LastVoucherNumber = 1;
            fake.Existing = ExistingInvoice(6, 1, 99);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                WsfeFiscalHomologationScenario.RunAsync(provider, Tenant(ArcaEnvironment.Homologation), 9001, 6, 1, Ct));
            exception.Message.Should().Contain("Exact consultation");
            fake.SubmissionCount.Should().Be(0);
        });
    }

    [Fact]
    public async Task Voucher_number_gap_fails_without_query_or_submission()
    {
        await WithFixture(async (fake, provider) =>
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                WsfeFiscalHomologationScenario.RunAsync(provider, Tenant(ArcaEnvironment.Homologation), 9001, 6, 2, Ct));
            exception.Message.Should().Contain("not the next");
            fake.ExactConsultationCount.Should().Be(0);
            fake.SubmissionCount.Should().Be(0);
        });
    }

    [Theory]
    [InlineData("pos")]
    [InlineData("last")]
    [InlineData("query")]
    public async Task ARCA_error_responses_stop_the_scenario_before_submission(string errorAt)
    {
        await WithFixture(async (fake, provider) =>
        {
            if (errorAt == "pos") fake.PointOfSaleError = true;
            else if (errorAt == "last") fake.LastNumberError = true;
            else
            {
                fake.LastVoucherNumber = 1;
                fake.QueryError = true;
            }
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                WsfeFiscalHomologationScenario.RunAsync(provider, Tenant(ArcaEnvironment.Homologation), 9001, 6,
                    errorAt == "query" ? 1 : 1, Ct));
            exception.Message.Should().Contain("errors");
            fake.SubmissionCount.Should().Be(0);
        });
    }

    [Fact]
    public async Task Unknown_submission_and_transient_query_errors_reconcile_without_resubmission()
    {
        await WithFixture(async (fake, provider) =>
        {
            fake.SubmissionMode = SubmissionMode.Unknown;
            fake.QueryError = true;
            var tenant = Tenant(ArcaEnvironment.Homologation);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                WsfeFiscalHomologationScenario.RunAsync(provider, tenant, 9001, 6, 1, Ct));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                WsfeFiscalHomologationScenario.RunAsync(provider, tenant, 9001, 6, 1, Ct));
            fake.SubmissionCount.Should().Be(1);
            fake.ExactConsultationCount.Should().Be(2);
        });
    }

    [Fact]
    public async Task Definite_rejection_is_never_automatically_retried()
    {
        await WithFixture(async (fake, provider) =>
        {
            fake.SubmissionMode = SubmissionMode.Rejected;
            var tenant = Tenant(ArcaEnvironment.Homologation);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                WsfeFiscalHomologationScenario.RunAsync(provider, tenant, 9001, 6, 1, Ct));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                WsfeFiscalHomologationScenario.RunAsync(provider, tenant, 9001, 6, 1, Ct));
            fake.SubmissionCount.Should().Be(1);
        });
    }

    [Fact]
    public async Task Timeout_after_remote_save_reconciles_exact_voucher_without_resubmitting()
    {
        await WithFixture(async (fake, provider) =>
        {
            fake.SubmissionMode = SubmissionMode.TimeoutAfterSave;
            var result = await WsfeFiscalHomologationScenario.RunAsync(provider, Tenant(ArcaEnvironment.Homologation), 9001, 6, 1, Ct);
            result.AlreadyExisted.Should().BeFalse();
            result.State.Should().Be(InvoiceState.Authorized);
            fake.SubmissionCount.Should().Be(1);
            fake.ExactConsultationCount.Should().Be(2);
        });
    }

    [Fact]
    public async Task Post_authorization_CAE_mismatch_fails_closed()
    {
        await WithFixture(async (fake, provider) =>
        {
            fake.PostQueryCaeOverride = "74123456789013";
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                WsfeFiscalHomologationScenario.RunAsync(provider, Tenant(ArcaEnvironment.Homologation), 9001, 6, 1, Ct));
            exception.Message.Should().Contain("authorization code");
            fake.SubmissionCount.Should().Be(1);
        });
    }

    private static async Task WithFixture(Func<OfflineWsfe, ServiceProvider, Task> test)
    {
        string database = Path.Combine(Path.GetTempPath(), $"netarcaws-homology-{Guid.NewGuid():N}.db");
        try
        {
            var fake = WsfeDispatchProxy.CreateOffline();
            var services = new ServiceCollection();
            services.AddSingleton<IWsfev1Service>((IWsfev1Service)fake);
            services.AddSingleton<IWsfexv1Service>(WsfeDispatchProxy.Create<IWsfexv1Service>());
            services.AddSingleton<IWsmtxcav1Service>(WsfeDispatchProxy.Create<IWsmtxcav1Service>());
            services.AddSingleton<IInvoiceJournal>(new SqliteInvoiceJournal(database));
            services.AddSingleton<TimeProvider>(new FrozenTimeProvider(new DateTimeOffset(2026, 10, 6, 15, 0, 0, TimeSpan.Zero)));
            services.AddSingleton<InvoiceCoordinator>();
            services.AddSingleton<SafeInvoiceService>();
            using var provider = services.BuildServiceProvider();
            await test(fake, provider);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(database)) File.Delete(database);
        }
    }

    private static ArcaTenantContext Tenant(ArcaEnvironment environment) => new(
        "offline-homology-test", 20123456786, environment,
        WsaaCertificateContent.FromPem("synthetic certificate content", "synthetic private key content"));

    private static FeCompConsResponse ExistingInvoice(int type, long number, double total)
    {
        var date = "20261006";
        var net = type == 6 ? 100d : 121d;
        var iva = type == 6 ? 21d : 0d;
        var prior = new FeCompConsResponse
        {
            PtoVta = 9001, CbteTipo = type, CbteDesde = number, CbteHasta = number,
            DocTipo = 99, DocNro = 0, Concepto = 1, CbteFch = date,
            ImpTotal = total, ImpTotConc = 0, ImpNeto = net, ImpOpEx = 0, ImpTrib = 0, ImpIva = iva,
            MonId = "PES", MonCotiz = 1, CondicionIvaReceptorId = 5,
            Resultado = "A", EmisionTipo = "CAE", CodAutorizacion = "74123456789012"
        };
        if (type == 6) prior.Iva.Add(new AlicIva { Id = 5, BaseImp = 100, Importe = 21 });
        return prior;
    }

    public enum SubmissionMode { Authorized, Unknown, Rejected, TimeoutAfterSave }

    public class OfflineWsfe : WsfeDispatchProxy
    {
        private FeCompConsResponse? saved;
        public int LastVoucherNumber { get; set; }
        public bool PointOfSaleError { get; set; }
        public bool LastNumberError { get; set; }
        public bool QueryError { get; set; }
        public FeCompConsResponse? Existing { get; set; }
        public SubmissionMode SubmissionMode { get; set; }
        public string? PostQueryCaeOverride { get; set; }
        public int SubmissionCount { get; private set; }
        public int ExactConsultationCount { get; private set; }
        public FecaeRequest? LastSubmitted { get; private set; }

        protected override object? InvokeCall(MethodInfo method, object?[] args)
        {
            switch (method.Name)
            {
                case nameof(IWsfev1Service.FEParamGetPtosVentaAsync):
                    var points = new FePtoVentaResponse();
                    points.ResultGet.Add(new PtoVenta { Nro = 9001, Bloqueado = "N", EmisionTipo = "CAE", FchBaja = "00000000" });
                    if (PointOfSaleError) points.Errors.Add(new Err { Code = 1, Msg = "synthetic" });
                    return Task.FromResult(new FeParamGetPtosVentaResponse { FeParamGetPtosVentaResult = points });
                case nameof(IWsfev1Service.FECompUltimoAutorizadoAsync):
                    var lastRequest = (FeCompUltimoAutorizado)args[1]!;
                    var last = new FeRecuperaLastCbteResponse
                    { PtoVta = lastRequest.PtoVta, CbteTipo = lastRequest.CbteTipo, CbteNro = LastVoucherNumber };
                    if (LastNumberError) last.Errors.Add(new Err { Code = 1, Msg = "synthetic" });
                    return Task.FromResult(new FeCompUltimoAutorizadoResponse { FeCompUltimoAutorizadoResult = last });
                case nameof(IWsfev1Service.FECompConsultarAsync):
                    ExactConsultationCount++;
                    var found = Existing ?? saved;
                    if (found is not null && PostQueryCaeOverride is not null && saved is not null)
                        found.CodAutorizacion = PostQueryCaeOverride;
                    var consult = new FeCompConsultaResponse { ResultGet = found };
                    if (QueryError) consult.Errors.Add(new Err { Code = 1, Msg = "synthetic" });
                    return Task.FromResult(new FeCompConsultarResponse { FeCompConsultarResult = consult });
                case nameof(IWsfev1Service.FECAESolicitarAsync):
                    SubmissionCount++;
                    var request = (FecaeSolicitar)args[1]!;
                    LastSubmitted = request.FeCaeReq;
                    var header = request.FeCaeReq.FeCabReq;
                    var detail = request.FeCaeReq.FeDetReq.Single();
                    const string cae = "74123456789012";
                    if (SubmissionMode is SubmissionMode.Authorized or SubmissionMode.TimeoutAfterSave)
                    {
                        saved = CopyAsConsultation(header.PtoVta, header.CbteTipo, detail, "A", cae);
                        LastVoucherNumber = checked((int)detail.CbteHasta);
                    }
                    if (SubmissionMode == SubmissionMode.TimeoutAfterSave)
                        throw new HttpRequestException("Synthetic transport interruption after remote save.");
                    var resultCode = SubmissionMode == SubmissionMode.Rejected ? "R" : "A";
                    var detailResponse = new FecaeDetResponse
                    {
                        Concepto = detail.Concepto, DocTipo = detail.DocTipo, DocNro = detail.DocNro,
                        CbteDesde = detail.CbteDesde, CbteHasta = detail.CbteHasta,
                        CbteFch = detail.CbteFch, Resultado = resultCode,
                        Cae = SubmissionMode == SubmissionMode.Authorized ? cae : null
                    };
                    if (SubmissionMode == SubmissionMode.Unknown) detailResponse.DocNro++;
                    var response = new FecaeResponse
                    {
                        FeCabResp = new FecaeCabResponse
                        {
                            Cuit = 20123456786, PtoVta = header.PtoVta, CbteTipo = header.CbteTipo,
                            CantReg = 1, Resultado = resultCode
                        }
                    };
                    response.FeDetResp.Add(detailResponse);
                    return Task.FromResult(new FecaeSolicitarResponse { FecaeSolicitarResult = response });
                default:
                    throw new NotSupportedException($"Offline WSFE fake does not implement {method.Name}.");
            }
        }

        private static FeCompConsResponse CopyAsConsultation(int pos, int type, FecaeDetRequest detail, string result, string cae)
        {
            var remote = new FeCompConsResponse
            {
                PtoVta = pos, CbteTipo = type, CbteDesde = detail.CbteDesde, CbteHasta = detail.CbteHasta,
                DocTipo = detail.DocTipo, DocNro = detail.DocNro, Concepto = detail.Concepto,
                CbteFch = detail.CbteFch, ImpTotal = detail.ImpTotal, ImpTotConc = detail.ImpTotConc,
                ImpNeto = detail.ImpNeto, ImpOpEx = detail.ImpOpEx, ImpTrib = detail.ImpTrib,
                ImpIva = detail.ImpIva, MonId = detail.MonId, MonCotiz = detail.MonCotiz,
                CondicionIvaReceptorId = detail.CondicionIvaReceptorId, Resultado = result,
                EmisionTipo = "CAE", CodAutorizacion = cae
            };
            foreach (var iva in detail.Iva) remote.Iva.Add(new AlicIva { Id = iva.Id, BaseImp = iva.BaseImp, Importe = iva.Importe });
            return remote;
        }
    }

    public class FrozenTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    public class WsfeDispatchProxy : DispatchProxy
    {
        public static T Create<T>() where T : class => DispatchProxy.Create<T, WsfeDispatchProxy>();
        public static OfflineWsfe CreateOffline() => (OfflineWsfe)DispatchProxy.Create<IWsfev1Service, OfflineWsfe>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null) throw new InvalidOperationException("Missing fake method.");
            if (this is OfflineWsfe wsfe) return wsfe.InvokeCall(targetMethod, args ?? []);
            Type resultType = targetMethod.ReturnType.GetGenericArguments().Single();
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType)
                .Invoke(null, [resultType.IsValueType ? Activator.CreateInstance(resultType) : null]);
        }

        protected virtual object? InvokeCall(MethodInfo method, object?[] args) => throw new NotSupportedException(method.Name);
    }
}
