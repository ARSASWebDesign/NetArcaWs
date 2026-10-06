using FluentAssertions;
using System.Security.Cryptography.X509Certificates;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Xml;
using NetArcaWs.Contracts.WsfeV1;
using NetArcaWs.Contracts.WsfexV1;
using NetArcaWs.Contracts.Wsmtxca;
using NetArcaWs.Cryptography;
using NetArcaWs.HealthChecks;
using NetArcaWs.Invoicing;
using NetArcaWs.Multitenancy;
using NetArcaWs.Services;
using NetArcaWs.Tests.TestSupport;
using Xunit;
using FexOpcional = NetArcaWs.Contracts.WsfexV1.Opcional;

namespace NetArcaWs.Tests.Invoicing;

public sealed class SafeInvoiceServiceTests
{
    private const long Cuit = 30123456789;
    private const long CustomerNumber = 20304050607;
    private const string TicketToken = "SERVER-ONLY-TICKET-TOKEN";
    private const string TicketSign = "SERVER-ONLY-TICKET-SIGN";

    [Fact]
    public async Task AuthorizeWsfeAsync_freezes_payload_without_credentials_and_replay_does_not_send_again()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var response = WsfeResponse(result: "A", authorizationCode: "71234567890123");
        var transport = new RecordingSoapTransport(_ => response);
        var tickets = new RecordingArcaTicketProvider(TicketTestData.Create(TicketToken, TicketSign));
        var service = CreateService(database, transport, tickets);
        ArcaTenantContext tenant = CreateTenant();
        FecaeSolicitar request = WsfeRequest(authToken: "CALLER-SUPPLIED-TOKEN", authSign: "CALLER-SUPPLIED-SIGN");

        InvoiceOperation first = await service.AuthorizeWsfeAsync(tenant, "wsfe-001", request, cancellationToken);
        InvoiceOperation replay = await service.AuthorizeWsfeAsync(tenant, "wsfe-001", request, cancellationToken);

        first.State.Should().Be(InvoiceState.Authorized);
        first.AuthorizationCode.Should().Be("71234567890123");
        replay.Should().BeEquivalentTo(first);
        transport.CallCount.Should().Be(1);
        tickets.CallCount.Should().Be(1);
        tickets.Calls.Single().Service.Should().Be("wsfe");
        request.Auth.Token.Should().Be("CALLER-SUPPLIED-TOKEN", "the caller's DTO must not be mutated");
        request.Auth.Sign.Should().Be("CALLER-SUPPLIED-SIGN");
        var sentRequest = transport.LastCall.Request.Should().BeOfType<FecaeSolicitar>().Subject;
        sentRequest.Auth.Token.Should().Be(TicketToken);
        sentRequest.Auth.Sign.Should().Be(TicketSign);
        sentRequest.Auth.Cuit.Should().Be(Cuit);
        first.Submission.Payload.Should().Contain("CbteDesde");
        first.Submission.Payload.Should().NotContain("CALLER-SUPPLIED-TOKEN");
        first.Submission.Payload.Should().NotContain("CALLER-SUPPLIED-SIGN");
        first.Submission.Payload.Should().NotContain(TicketToken);
        first.Submission.Payload.Should().NotContain(TicketSign);
        first.Submission.Identity.Should().Be(new InvoiceIdentity(ArcaEnvironment.Homologation, Cuit, 1, 1, 12));
    }

    [Fact]
    public async Task AuthorizeWsfeAsync_rejects_multiple_or_noncontiguous_details_before_dispatch()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport();
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        ArcaTenantContext tenant = CreateTenant();
        FecaeSolicitar multiple = WsfeRequest();
        multiple.FeCaeReq.FeDetReq.Add(WsfeDetail(number: 13));
        FecaeSolicitar noncontiguous = WsfeRequest(from: 12, to: 13);

        Func<Task> multipleDetails = () => service.AuthorizeWsfeAsync(tenant, "multi", multiple, cancellationToken);
        Func<Task> noncontiguousDetails = () => service.AuthorizeWsfeAsync(tenant, "range", noncontiguous, cancellationToken);

        await multipleDetails.Should().ThrowAsync<ArgumentException>();
        await noncontiguousDetails.Should().ThrowAsync<ArgumentException>();
        transport.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task AuthorizeWsfeAsync_never_marks_a_mismatched_or_incomplete_response_authorized()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var response = WsfeResponse(result: "A", authorizationCode: "71234567890123");
        response.FecaeSolicitarResult.FeCabResp.Cuit = Cuit + 1;
        var transport = new RecordingSoapTransport(_ => response);
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));

        InvoiceOperation result = await service.AuthorizeWsfeAsync(
            CreateTenant(), "mismatch", WsfeRequest(), cancellationToken);

        result.State.Should().NotBe(InvoiceState.Authorized);
        result.AuthorizationCode.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReconcileAsync_rejects_wrong_cuit_even_for_terminal_or_unknown_operations_without_mutating_them(bool authorize)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(_ => authorize
            ? WsfeResponse("A", "71234567890123")
            : new FecaeSolicitarResponse());
        var tickets = new RecordingArcaTicketProvider(TicketTestData.Create());
        var service = CreateService(database, transport, tickets);
        ArcaTenantContext tenant = CreateTenant();
        InvoiceOperation initial = await service.AuthorizeWsfeAsync(tenant, "wrong-cuit", WsfeRequest(), cancellationToken);
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "safe-service"));
        InvoiceOperation before = (await journal.FindAsync(tenant.TenantId, "wrong-cuit", cancellationToken))!;
        int transportCount = transport.CallCount;
        int ticketCount = tickets.CallCount;

        Func<Task> reconcile = () => service.ReconcileAsync(CreateTenant(Cuit + 1), "wrong-cuit", cancellationToken);

        await reconcile.Should().ThrowAsync<InvoiceConflictException>();
        InvoiceOperation after = (await journal.FindAsync(tenant.TenantId, "wrong-cuit", cancellationToken))!;
        after.State.Should().Be(initial.State);
        after.Version.Should().Be(before.Version);
        after.Attempt.Should().Be(before.Attempt);
        transport.CallCount.Should().Be(transportCount);
        tickets.CallCount.Should().Be(ticketCount);
    }

    [Fact]
    public async Task AuthorizeWsfeAsync_accepts_only_a_correlated_detailed_rejection_as_rejected()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var response = WsfeResponse(result: "R", authorizationCode: null);
        var transport = new RecordingSoapTransport(_ => response);
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));

        InvoiceOperation result = await service.AuthorizeWsfeAsync(
            CreateTenant(), "rejected", WsfeRequest(), cancellationToken);

        result.State.Should().Be(InvoiceState.Rejected);
        result.AuthorizationCode.Should().BeNull();
    }

    [Fact]
    public async Task AuthorizeWsfexAsync_correlates_tenant_and_fiscal_identity_and_reserves_remote_id()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        const long remoteId = 76321;
        var response = new FexAuthorizeResponse
        {
            FexAuthorizeResult = new FexResponseAuthorize
            {
                FexResultAuth = new ClsFexOutAuthorize
                {
                    Id = remoteId,
                    Cuit = Cuit,
                    CbteTipo = 19,
                    PuntoVta = 2,
                    CbteNro = 12,
                    FchCbte = "20261006",
                    Resultado = "A",
                    Cae = "71234567890123"
                }
            }
        };
        var transport = new RecordingSoapTransport(_ => response);
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create(TicketToken, TicketSign)));
        ClsFexRequest request = FexRequest(remoteId);

        InvoiceOperation result = await service.AuthorizeWsfexAsync(CreateTenant(), "fex-001", request, cancellationToken);
        InvoiceOperation restored = (await new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "safe-service"))
            .FindAsync("tenant-safe", "fex-001", cancellationToken))!;

        result.State.Should().Be(InvoiceState.Authorized);
        result.AuthorizationCode.Should().Be("71234567890123");
        restored.Submission.RemoteRequestId.Should().Be(remoteId);
        var sent = transport.LastCall.Request.Should().BeOfType<FexAuthorize>().Subject;
        sent.Auth.Token.Should().Be(TicketToken);
        sent.Auth.Sign.Should().Be(TicketSign);
        sent.Auth.Cuit.Should().Be(Cuit);
        request.FechaCbte.Should().Be("20261006");
    }

    [Theory]
    [InlineData(ResultadoSimpleType.A)]
    [InlineData(ResultadoSimpleType.O)]
    public async Task AuthorizeWsmtxcaAsync_preserves_explicit_civil_date_and_accepts_correlated_cae(
        ResultadoSimpleType resultCode)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        DateTime issueDate = new(2026, 10, 6, 0, 0, 0, DateTimeKind.Unspecified);
        var response = new AutorizarComprobanteResponseType
        {
            Resultado = resultCode,
            ComprobanteResponse = new ComprobanteCaeResponseType
            {
                Cuit = Cuit,
                CodigoTipoComprobante = 1,
                NumeroPuntoVenta = 1,
                NumeroComprobante = 12,
                FechaEmision = issueDate,
                Cae = 71234567890123,
                FechaVencimientoCae = issueDate.AddDays(10)
            }
        };
        var transport = new RecordingSoapTransport(_ => response);
        var tickets = new RecordingArcaTicketProvider(TicketTestData.Create(TicketToken, TicketSign));
        var service = CreateService(database, transport, tickets);

        InvoiceOperation result = await service.AuthorizeWsmtxcaAsync(CreateTenant(), "mtx-001", MtxRequest(issueDate), cancellationToken);

        result.State.Should().Be(InvoiceState.Authorized);
        result.AuthorizationCode.Should().Be("71234567890123");
        var sent = transport.LastCall.Request.Should().BeOfType<AutorizarComprobanteRequestType>().Subject;
        sent.AuthRequest.Token.Should().Be(TicketToken);
        sent.AuthRequest.Sign.Should().Be(TicketSign);
        sent.AuthRequest.CuitRepresentada.Should().Be(Cuit);
        sent.ComprobanteCaeRequest.FechaEmision.Should().Be(issueDate);
        result.Submission.Payload.Should().Contain("<fechaEmision>2026-10-06</fechaEmision>");
        tickets.Calls.Single().Service.Should().Be("wsmtxca");
    }

    [Fact]
    public async Task AuthorizeWsmtxcaAsync_treats_protocol_rejection_with_structured_errors_as_definitive()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var response = new AutorizarComprobanteResponseType { Resultado = ResultadoSimpleType.R };
        response.ArrayErrores.Add(new CodigoDescripcionType { Codigo = 100, Descripcion = "Error fiscal del comprobante" });
        var transport = new RecordingSoapTransport(_ => response);
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));

        InvoiceOperation result = await service.AuthorizeWsmtxcaAsync(CreateTenant(), "mtx-rejected", MtxRequest(CivilIssueDate()), cancellationToken);

        result.State.Should().Be(InvoiceState.Rejected);
        result.AuthorizationCode.Should().BeNull();
        result.ResponseXml.Should().Contain("arrayErrores");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthorizeWsmtxcaAsync_keeps_rejection_without_errors_or_with_contradictory_cae_unknown(bool includeContradictoryCae)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        DateTime issueDate = CivilIssueDate();
        var response = new AutorizarComprobanteResponseType { Resultado = ResultadoSimpleType.R };
        if (includeContradictoryCae)
        {
            response.ComprobanteResponse = new ComprobanteCaeResponseType
            {
                Cuit = Cuit,
                CodigoTipoComprobante = 1,
                NumeroPuntoVenta = 1,
                NumeroComprobante = 12,
                FechaEmision = issueDate,
                Cae = 71234567890123,
                FechaVencimientoCae = issueDate.AddDays(10)
            };
        }
        var transport = new RecordingSoapTransport(_ => response);
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));

        InvoiceOperation result = await service.AuthorizeWsmtxcaAsync(CreateTenant(), "mtx-inconsistent", MtxRequest(issueDate), cancellationToken);

        result.State.Should().Be(InvoiceState.Unknown);
        result.AuthorizationCode.Should().BeNull();
    }

    [Fact]
    public async Task ResumeAsync_reconciles_unknown_wsfe_without_resubmitting_and_authorizes_only_a_matching_remote_record()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(call => call.ResponseType == typeof(FecaeSolicitarResponse)
            ? new FecaeSolicitarResponse()
            : new FeCompConsultarResponse
            {
                FeCompConsultarResult = new FeCompConsultaResponse
                {
                    ResultGet = new FeCompConsResponse
                    {
                        Concepto = 1,
                        DocTipo = 80,
                        DocNro = CustomerNumber,
                        CbteDesde = 12,
                        CbteHasta = 12,
                        CbteFch = "20261006",
                        ImpTotal = 121,
                        ImpTotConc = 0,
                        ImpNeto = 100,
                        ImpOpEx = 0,
                        ImpIva = 21,
                        ImpTrib = 0,
                        MonId = "PES",
                        MonCotiz = 1,
                        PtoVta = 1,
                        CbteTipo = 1,
                        EmisionTipo = "CAE",
                        Resultado = "A",
                        CodAutorizacion = "71234567890123"
                    }
                }
            });
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));

        InvoiceOperation first = await service.AuthorizeWsfeAsync(CreateTenant(), "recover-001", WsfeRequest(), cancellationToken);
        InvoiceOperation recovered = await service.ResumeAsync(CreateTenant(), "recover-001", cancellationToken);

        first.State.Should().Be(InvoiceState.Unknown);
        recovered.State.Should().Be(InvoiceState.Authorized);
        recovered.AuthorizationCode.Should().Be("71234567890123");
        transport.CallCount.Should().Be(2, "recovery queries FECompConsultar and never repeats FECAESolicitar");
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FECAESolicitar", StringComparison.Ordinal));
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FECompConsultar", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false, InvoiceState.Authorized)]
    [InlineData(true, InvoiceState.Conflict)]
    public async Task ResumeAsync_reconciles_wsfex_by_comparing_nested_items_and_optionals(bool alterItem, InvoiceState expected)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ClsFexRequest original = FexRequest(45219);
        var transport = new RecordingSoapTransport(call => call.ResponseType == typeof(FexAuthorizeResponse)
            ? new FexAuthorizeResponse()
            : FexQueryResponse(original, alterItem));
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        ArcaTenantContext tenant = CreateTenant();

        InvoiceOperation first = await service.AuthorizeWsfexAsync(tenant, "fex-reconcile", original, cancellationToken);
        InvoiceOperation reconciled = await service.ResumeAsync(tenant, "fex-reconcile", cancellationToken);

        first.State.Should().Be(InvoiceState.Unknown);
        reconciled.State.Should().Be(expected);
        if (expected == InvoiceState.Authorized)
            reconciled.AuthorizationCode.Should().Be("71234567890123");
        transport.CallCount.Should().Be(2);
        transport.Calls.Last().Action.Should().EndWith("/FEXGetCMP");
    }

    [Theory]
    [InlineData(false, InvoiceState.Authorized)]
    [InlineData(true, InvoiceState.Conflict)]
    public async Task ResumeAsync_reconciles_wsmtxca_by_comparing_the_immutable_comprobante(bool alterFiscalField, InvoiceState expected)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        DateTime issueDate = new(2026, 10, 6, 0, 0, 0, DateTimeKind.Unspecified);
        ComprobanteType original = MtxRequest(issueDate);
        var transport = new RecordingSoapTransport(call => call.ResponseType == typeof(AutorizarComprobanteResponseType)
            ? new AutorizarComprobanteResponseType()
            : MtxQueryResponse(original, alterFiscalField));
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        ArcaTenantContext tenant = CreateTenant();

        InvoiceOperation first = await service.AuthorizeWsmtxcaAsync(tenant, "mtx-reconcile", original, cancellationToken);
        InvoiceOperation reconciled = await service.ResumeAsync(tenant, "mtx-reconcile", cancellationToken);

        first.State.Should().Be(InvoiceState.Unknown);
        reconciled.State.Should().Be(expected);
        if (expected == InvoiceState.Authorized)
            reconciled.AuthorizationCode.Should().Be("71234567890123");
        transport.CallCount.Should().Be(2);
        transport.Calls.Last().Action.Should().EndWith("/consultarComprobante");
    }

    [Fact]
    public async Task ResumeAsync_dispatches_a_prepared_snapshot_after_a_crash_before_first_claim()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        string journalPath = InvoiceTestData.NewDatabasePath(database, "safe-service");
        FecaeRequest request = WsfeRequest().FeCaeReq;
        var submission = new InvoiceSubmission("tenant-safe", "prepared-recovery", "wsfe",
            new InvoiceIdentity(ArcaEnvironment.Homologation, Cuit, 1, 1, 12), SerializeXml(request));
        await new SqliteInvoiceJournal(journalPath).PrepareAsync(submission, cancellationToken);
        var transport = new RecordingSoapTransport(_ => WsfeResponse("A", "71234567890123"));
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));

        InvoiceOperation recovered = await service.ResumeAsync(CreateTenant(), "prepared-recovery", cancellationToken);

        recovered.State.Should().Be(InvoiceState.Authorized);
        transport.CallCount.Should().Be(1);
        transport.LastCall.Action.Should().EndWith("/FECAESolicitar");
    }

    [Fact]
    public async Task ReviseRejectedAsync_keeps_the_rejected_payload_in_history_and_sends_only_the_explicit_revision()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        int submissionCount = 0;
        var transport = new RecordingSoapTransport(_ =>
            ++submissionCount == 1 ? WsfeResponse("R", null) : WsfeResponse("A", "71234567890123"));
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        ArcaTenantContext tenant = CreateTenant();
        FecaeRequest rejectedPayload = WsfeRequest().FeCaeReq;
        InvoiceOperation rejected = await service.AuthorizeWsfeAsync(tenant, "revise-wsfe", rejectedPayload, cancellationToken);
        FecaeRequest corrected = WsfeRequest().FeCaeReq;
        corrected.FeDetReq[0].ImpTotal = 133.10;
        corrected.FeDetReq[0].ImpNeto = 110;

        InvoiceOperation preparedRevision = await service.ReviseRejectedAsync(
            tenant, "revise-wsfe", rejected.Version, corrected, cancellationToken);
        InvoiceOperation authorized = await service.ResumeAsync(tenant, "revise-wsfe", cancellationToken);
        IReadOnlyList<InvoiceOperation> history = await new SqliteInvoiceJournal(
            InvoiceTestData.NewDatabasePath(database, "safe-service"))
            .ListRevisionsAsync(tenant.TenantId, "revise-wsfe", cancellationToken);

        rejected.State.Should().Be(InvoiceState.Rejected);
        preparedRevision.State.Should().Be(InvoiceState.Prepared);
        authorized.State.Should().Be(InvoiceState.Authorized);
        authorized.Submission.Payload.Should().Contain("133.1");
        history.Should().ContainSingle();
        history[0].Submission.Payload.Should().NotContain("133.1");
        history[0].State.Should().Be(InvoiceState.Rejected);
        transport.CallCount.Should().Be(2, "the second send only happens after explicit revision and ResumeAsync");
    }

    private static SafeInvoiceService CreateService(
        TemporaryDatabase database,
        RecordingSoapTransport transport,
        RecordingArcaTicketProvider tickets)
    {
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "safe-service"));
        var coordinator = new InvoiceCoordinator(journal);
        var wsfe = new Wsfev1Service(transport, tickets);
        var wsfex = new Wsfexv1Service(transport, tickets);
        var wsmtxca = new Wsmtxcav1Service(transport, tickets);
        return new SafeInvoiceService(coordinator, wsfe, wsfex, wsmtxca);
    }

    private static ArcaTenantContext CreateTenant(long cuit = Cuit)
    {
        using var certificate = TestCertificates.CreateWithPrivateKey("safe-invoice-tests");
        string privateKey = certificate.GetRSAPrivateKey()!.ExportPkcs8PrivateKeyPem();
        var content = WsaaCertificateContent.FromPem(certificate.ExportCertificatePem(), privateKey);
        return new ArcaTenantContext("tenant-safe", cuit, ArcaEnvironment.Homologation, content);
    }

    private static FecaeSolicitar WsfeRequest(string authToken = "CALLER-TOKEN", string authSign = "CALLER-SIGN",
        long from = 12, long? to = null) => new()
    {
        Auth = new FeAuthRequest { Token = authToken, Sign = authSign, Cuit = 99999999999 },
        FeCaeReq = new FecaeRequest
        {
            FeCabReq = new FecaeCabRequest { CantReg = 1, PtoVta = 1, CbteTipo = 1 },
            FeDetReq = { WsfeDetail(from, to ?? from) }
        }
    };

    private static FecaeDetRequest WsfeDetail(long number, long? to = null) => new()
    {
        Concepto = 1,
        DocTipo = 80,
        DocNro = CustomerNumber,
        CbteDesde = number,
        CbteHasta = to ?? number,
        CbteFch = "20261006",
        ImpTotal = 121,
        ImpTotConc = 0,
        ImpNeto = 100,
        ImpOpEx = 0,
        ImpIva = 21,
        ImpTrib = 0,
        MonId = "PES",
        MonCotiz = 1
    };

    private static ClsFexRequest FexRequest(long id) => new()
    {
        Id = id,
        FechaCbte = "20261006",
        CbteTipo = 19,
        PuntoVta = 2,
        CbteNro = 12,
        TipoExpo = 1,
        DstCmp = 200,
        Cliente = "Cliente de prueba",
        CuitPaisCliente = 50000000000,
        DomicilioCliente = "Domicilio de prueba",
        MonedaId = "DOL",
        MonedaCtz = 1,
        Items = { new Item
        {
            ProCodigo = "SKU-001",
            ProDs = "Producto de prueba",
            ProQty = 2,
            ProUmed = 7,
            ProPrecioUni = 10,
            ProBonificacion = 0,
            ProTotalItem = 20
        } },
        Opcionales = { new FexOpcional { Id = "OBSERVACION", Valor = "Entrega pactada" } }
    };

    private static ComprobanteType MtxRequest(DateTime issueDate) => new()
    {
        CodigoTipoComprobante = 1,
        NumeroPuntoVenta = 1,
        NumeroComprobante = 12,
        FechaEmision = issueDate,
        ImporteTotal = 121,
        CodigoMoneda = "PES",
        CotizacionMoneda = 1
    };

    private static DateTime CivilIssueDate() => new(2026, 10, 6, 0, 0, 0, DateTimeKind.Unspecified);

    private static FexGetCmpResponse2 FexQueryResponse(ClsFexRequest original, bool alterItem)
    {
        var remote = new ClsFexGetCmpr();
        CopyMatchingScalarProperties(original, remote);
        foreach (Item item in original.Items)
            remote.Items.Add(new Item
            {
                ProCodigo = item.ProCodigo,
                ProDs = item.ProDs,
                ProQty = item.ProQty,
                ProUmed = item.ProUmed,
                ProPrecioUni = item.ProPrecioUni,
                ProBonificacion = item.ProBonificacion,
                ProTotalItem = item.ProTotalItem
            });
        foreach (FexOpcional option in original.Opcionales)
            remote.Opcionales.Add(new FexOpcional { Id = option.Id, Valor = option.Valor });
        remote.Resultado = "A";
        remote.Cae = "71234567890123";
        if (alterItem) remote.Items[0].ProTotalItem += 1;
        return new FexGetCmpResponse2
        {
            FexGetCmpResult = new FexGetCmpResponse { FexResultGet = remote }
        };
    }

    private static ConsultarComprobanteResponseType MtxQueryResponse(ComprobanteType original, bool alterFiscalField)
    {
        var remote = new ComprobanteType();
        CopyMatchingScalarProperties(original, remote);
        remote.CodigoAutorizacion = 71234567890123;
        remote.CodigoTipoAutorizacion = CodigoTipoAutorizacionSimpleType.A;
        remote.FechaVencimiento = original.FechaEmision!.Value.AddDays(10);
        if (alterFiscalField) remote.NumeroComprobante++;
        return new ConsultarComprobanteResponseType { Comprobante = remote };
    }

    private static void CopyMatchingScalarProperties(object source, object target)
    {
        foreach (PropertyInfo sourceProperty in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            PropertyInfo? targetProperty = target.GetType().GetProperty(sourceProperty.Name, BindingFlags.Public | BindingFlags.Instance);
            if (targetProperty is null || !targetProperty.CanWrite ||
                (sourceProperty.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(sourceProperty.PropertyType)))
                continue;
            object? value = sourceProperty.GetValue(source);
            if (value is not null && targetProperty.PropertyType.IsAssignableFrom(value.GetType()))
                targetProperty.SetValue(target, value);
        }
    }

    private static string SerializeXml<T>(T value)
    {
        using var buffer = new MemoryStream();
        using (var writer = XmlWriter.Create(buffer, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            CloseOutput = false
        }))
            new System.Xml.Serialization.XmlSerializer(typeof(T)).Serialize(writer, value);
        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, checked((int)buffer.Length));
    }

    private static FecaeSolicitarResponse WsfeResponse(string result, string? authorizationCode)
    {
        var response = new FecaeResponse
        {
            FeCabResp = new FecaeCabResponse
            {
                Cuit = Cuit,
                PtoVta = 1,
                CbteTipo = 1,
                CantReg = 1,
                Resultado = result
            }
        };
        response.FeDetResp.Add(new FecaeDetResponse
        {
            Concepto = 1,
            DocTipo = 80,
            DocNro = CustomerNumber,
            CbteDesde = 12,
            CbteHasta = 12,
            CbteFch = "20261006",
            Resultado = result,
            Cae = authorizationCode,
            CaeFchVto = authorizationCode is null ? null : "20261016"
        });
        return new FecaeSolicitarResponse { FecaeSolicitarResult = response };
    }
}
