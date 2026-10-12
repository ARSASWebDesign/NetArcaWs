using AwesomeAssertions;
using System.Security.Cryptography.X509Certificates;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
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
    public async Task Typed_submission_builders_match_the_authorize_snapshot_without_sending_soap()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(_ => WsfeResponse(result: "A", authorizationCode: "71234567890123"));
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        ArcaTenantContext tenant = CreateTenant();
        FecaeRequest wsfe = WsfeRequest().FeCaeReq;
        ClsFexRequest wsfex = FexRequest(4567);
        ComprobanteType wsmtxca = MtxRequest(CivilIssueDate());

        InvoiceSubmission wsfeSubmission = SafeInvoiceService.CreateWsfeSubmission(tenant, "wsfe-builder", wsfe);
        InvoiceSubmission wsfexSubmission = SafeInvoiceService.CreateWsfexSubmission(tenant, "fex-builder", wsfex);
        InvoiceSubmission wsmtxcaSubmission = SafeInvoiceService.CreateWsmtxcaSubmission(tenant, "mtx-builder", wsmtxca);
        InvoiceOperation authorized = await service.AuthorizeWsfeAsync(tenant, "wsfe-builder", new FecaeSolicitar { FeCaeReq = wsfe }, cancellationToken);

        wsfeSubmission.Should().BeEquivalentTo(authorized.Submission);
        wsfeSubmission.Payload.Should().Be(authorized.Submission.Payload);
        wsfeSubmission.Payload.Should().Be(SerializeXml(wsfe));
        wsfexSubmission.Identity.Should().Be(new InvoiceIdentity(tenant.Environment, tenant.Cuit, wsfex.PuntoVta, wsfex.CbteTipo, wsfex.CbteNro));
        wsfexSubmission.Payload.Should().Be(SerializeXml(wsfex));
        wsfexSubmission.RemoteRequestId.Should().Be(wsfex.Id);
        wsmtxcaSubmission.Identity.Should().Be(new InvoiceIdentity(tenant.Environment, tenant.Cuit, wsmtxca.NumeroPuntoVenta, wsmtxca.CodigoTipoComprobante, wsmtxca.NumeroComprobante));
        wsmtxcaSubmission.Payload.Should().Be(SerializeXml(wsmtxca));
        transport.CallCount.Should().Be(1);
    }

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

    [Fact]
    public async Task AuthorizeWsfexAsync_classifies_only_the_known_missing_PermisoExistente_1550_as_rejected()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ClsFexRequest request = FexRequest(76321);
        request.TipoExpo = 2;
        request.CbteTipo = 19;
        var transport = new RecordingSoapTransport(_ => KnownMissingPermiso1550Response());
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));

        InvoiceOperation result = await service.AuthorizeWsfexAsync(CreateTenant(), "fex-known-1550", request, cancellationToken);

        result.State.Should().Be(InvoiceState.Rejected);
        result.AuthorizationCode.Should().BeNull();
        transport.CallCount.Should().Be(1);
    }

    [Theory]
    [InlineData("other-code")]
    [InlineData("wrong-message")]
    [InlineData("permission-present")]
    [InlineData("unsupported-export-type")]
    [InlineData("unsupported-voucher-type")]
    [InlineData("result-auth-present")]
    public async Task AuthorizeWsfexAsync_does_not_generalize_the_1550_rejection_allowlist(string variant)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ClsFexRequest request = FexRequest(76322);
        request.TipoExpo = 2;
        request.CbteTipo = 19;
        FexResponseAuthorize response = KnownMissingPermiso1550Response().FexAuthorizeResult!;
        if (variant == "other-code") response.FexErr!.ErrCode = 1551;
        if (variant == "wrong-message") response.FexErr!.ErrMsg = "Otro campo inválido";
        if (variant == "permission-present") request.PermisoExistente = "N";
        if (variant == "unsupported-export-type") request.TipoExpo = 1;
        if (variant == "unsupported-voucher-type") request.CbteTipo = 18;
        if (variant == "result-auth-present") response.FexResultAuth = new ClsFexOutAuthorize();
        var transport = new RecordingSoapTransport(_ => new FexAuthorizeResponse { FexAuthorizeResult = response });
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));

        InvoiceOperation result = await service.AuthorizeWsfexAsync(CreateTenant(), "fex-1550-boundary-" + variant, request, cancellationToken);

        result.State.Should().Be(InvoiceState.Unknown);
        result.AuthorizationCode.Should().BeNull();
        transport.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task AdjudicateWsfexValidationRejectionAsync_requires_the_exact_frozen_snapshot_and_does_not_send()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ArcaTenantContext tenant = CreateTenant();
        ClsFexRequest request = FexRequest(76323);
        request.TipoExpo = 2;
        request.CbteTipo = 19;
        var transport = new RecordingSoapTransport(_ => new FexAuthorizeResponse());
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "safe-service"));
        InvoiceSubmission submission = CreateWsfexSubmissionForCurrentQuery(tenant, "fex-adjudicate-1550", request);
        InvoiceOperation pending = await SaveUnknownWsfexOperationAsync(journal, submission, NoResultCurrentQueryResponse(), cancellationToken);
        var evidence = new InvoiceRejectionEvidence(pending.Submission, SerializeXml(KnownMissingPermiso1550Response()),
            "qa-maintainer", "protected-runner", "Recovered original authorization response");

        InvoiceOperation rejected = await service.AdjudicateWsfexValidationRejectionAsync(
            tenant, "fex-adjudicate-1550", pending.Version, evidence, cancellationToken);
        IReadOnlyList<InvoiceRejectionAdjudication> audit = await service.ListWsfexRejectionAdjudicationsAsync(
            tenant, "fex-adjudicate-1550", cancellationToken: cancellationToken);

        pending.State.Should().Be(InvoiceState.Unknown);
        rejected.State.Should().Be(InvoiceState.Rejected);
        rejected.Submission.Should().Be(pending.Submission);
        rejected.Submission.RemoteRequestId.Should().Be(request.Id);
        audit.Should().ContainSingle();
        audit[0].OriginalResponseXml.Should().Be(evidence.OriginalResponseXml);
        audit[0].Operator.Should().Be(evidence.Operator);
        audit[0].Source.Should().Be(evidence.Source);
        audit[0].Reason.Should().Be(evidence.Reason);
        transport.CallCount.Should().Be(0, "adjudication is journal-only and never submits or reconciles");
    }

    [Fact]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_mismatched_evidence_without_mutation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ArcaTenantContext tenant = CreateTenant();
        ClsFexRequest request = FexRequest(76324);
        request.TipoExpo = 2;
        request.CbteTipo = 19;
        var transport = new RecordingSoapTransport(_ => new FexAuthorizeResponse());
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "safe-service"));
        InvoiceSubmission submission = CreateWsfexSubmissionForCurrentQuery(tenant, "fex-adjudicate-mismatch", request);
        InvoiceOperation pending = await SaveUnknownWsfexOperationAsync(journal, submission, NoResultCurrentQueryResponse(), cancellationToken);
        InvoiceSubmission mismatched = pending.Submission with { Payload = pending.Submission.Payload + " " };
        var evidence = new InvoiceRejectionEvidence(mismatched, SerializeXml(KnownMissingPermiso1550Response()),
            "qa-maintainer", "protected-runner", "Snapshot mismatch must fail closed");

        Func<Task> adjudicate = () => service.AdjudicateWsfexValidationRejectionAsync(
            tenant, "fex-adjudicate-mismatch", pending.Version, evidence, cancellationToken);

        await adjudicate.Should().ThrowAsync<InvoiceConflictException>();
        (await journal.FindAsync(tenant.TenantId, "fex-adjudicate-mismatch", cancellationToken))!.State.Should().Be(InvoiceState.Unknown);
        (await service.ListWsfexRejectionAdjudicationsAsync(tenant, "fex-adjudicate-mismatch", cancellationToken: cancellationToken)).Should().BeEmpty();
        transport.CallCount.Should().Be(0);
    }

    [Theory]
    [InlineData("wrong-response-qname")]
    [InlineData("duplicate-authorize-result")]
    [InlineData("duplicate-error")]
    [InlineData("duplicate-result-auth")]
    [InlineData("evil-authorize-result")]
    [InlineData("evil-error")]
    [InlineData("evil-result-auth")]
    [InlineData("extra-root-voucher-wrapper")]
    [InlineData("dtd")]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_malformed_or_ambiguous_xml_without_mutation(string variant)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ArcaTenantContext tenant = CreateTenant();
        ClsFexRequest request = FexRequest(76325);
        request.TipoExpo = 2;
        request.CbteTipo = 19;
        var transport = new RecordingSoapTransport(_ => new FexAuthorizeResponse());
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "safe-service"));
        InvoiceSubmission submission = CreateWsfexSubmissionForCurrentQuery(tenant, "fex-adjudicate-xml-" + variant, request);
        InvoiceOperation pending = await SaveUnknownWsfexOperationAsync(journal, submission, NoResultCurrentQueryResponse(), cancellationToken);
        var evidence = new InvoiceRejectionEvidence(pending.Submission, Malformed1550ResponseXml(variant),
            "qa-maintainer", "protected-runner", "Malformed evidence must fail closed");

        Func<Task> adjudicate = () => service.AdjudicateWsfexValidationRejectionAsync(
            tenant, "fex-adjudicate-xml-" + variant, pending.Version, evidence, cancellationToken);

        await adjudicate.Should().ThrowAsync<InvoiceConflictException>();
        (await journal.FindAsync(tenant.TenantId, "fex-adjudicate-xml-" + variant, cancellationToken))!.State.Should().Be(InvoiceState.Unknown);
        (await service.ListWsfexRejectionAdjudicationsAsync(tenant, "fex-adjudicate-xml-" + variant,
            cancellationToken: cancellationToken)).Should().BeEmpty();
        transport.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_explicitly_nil_PermisoExistente()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ArcaTenantContext tenant = CreateTenant();
        ClsFexRequest request = FexRequest(76326);
        request.TipoExpo = 2;
        request.CbteTipo = 19;
        var transport = new RecordingSoapTransport(_ => new FexAuthorizeResponse());
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "safe-service"));
        InvoiceSubmission submission = SafeInvoiceService.CreateWsfexSubmission(tenant, "fex-adjudicate-nil", request);
        XDocument requestDocument = XDocument.Parse(submission.Payload);
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        requestDocument.Root!.Add(new XElement("Permiso_existente", new XAttribute(xsi + "nil", "true")));
        submission = submission with { Payload = requestDocument.ToString(SaveOptions.DisableFormatting) };
        await journal.PrepareAsync(submission, cancellationToken);
        InvoiceLease lease = (await journal.TryAcquireAsync(tenant.TenantId, "fex-adjudicate-nil",
            TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        InvoiceOperation pending = await journal.CompleteAsync(lease,
            new InvoiceDecision(InvoiceState.Unknown, ResponseXml: SerializeXml(new FexGetCmpResponse2
            {
                FexGetCmpResult = new FexGetCmpResponse { FexErr = new ClsFexErr { ErrCode = 1020 } }
            })), cancellationToken);
        var evidence = new InvoiceRejectionEvidence(submission, SerializeXml(KnownMissingPermiso1550Response()),
            "qa-maintainer", "protected-runner", "Explicit nil is not omission");

        Func<Task> adjudicate = () => service.AdjudicateWsfexValidationRejectionAsync(
            tenant, "fex-adjudicate-nil", pending.Version, evidence, cancellationToken);

        await adjudicate.Should().ThrowAsync<InvoiceConflictException>();
        (await journal.FindAsync(tenant.TenantId, "fex-adjudicate-nil", cancellationToken))!.State.Should().Be(InvoiceState.Unknown);
        transport.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task AdjudicateWsfexValidationRejectionAsync_does_not_override_a_later_positive_query_record()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ArcaTenantContext tenant = CreateTenant();
        ClsFexRequest request = FexRequest(76327);
        request.TipoExpo = 2;
        request.CbteTipo = 19;
        var transport = new RecordingSoapTransport(call => call.ResponseType == typeof(FexAuthorizeResponse)
            ? new FexAuthorizeResponse()
            : FexQueryResponse(request, alterItem: true));
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        InvoiceOperation pending = await service.AuthorizeWsfexAsync(tenant, "fex-adjudicate-positive-query", request, cancellationToken);
        InvoiceOperation conflicted = await service.ResumeAsync(tenant, "fex-adjudicate-positive-query", cancellationToken);
        var evidence = new InvoiceRejectionEvidence(pending.Submission, SerializeXml(KnownMissingPermiso1550Response()),
            "qa-maintainer", "protected-runner", "Older rejection cannot override later positive query");

        Func<Task> adjudicate = () => service.AdjudicateWsfexValidationRejectionAsync(
            tenant, "fex-adjudicate-positive-query", conflicted.Version, evidence, cancellationToken);

        await adjudicate.Should().ThrowAsync<InvoiceConflictException>();
        conflicted.State.Should().Be(InvoiceState.Conflict);
        (await new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "safe-service"))
            .ListRevisionsAsync(tenant.TenantId, "fex-adjudicate-positive-query", cancellationToken)).Should().BeEmpty();
        (await service.ListWsfexRejectionAdjudicationsAsync(tenant, "fex-adjudicate-positive-query",
            cancellationToken: cancellationToken)).Should().BeEmpty();
        transport.CallCount.Should().Be(2);
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("Cbte_tipo")]
    [InlineData("Punto_vta")]
    [InlineData("Cbte_nro")]
    [InlineData("Cae")]
    [InlineData("Resultado")]
    [InlineData("Fecha_cbte")]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_partial_current_query_identity(string field)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ArcaTenantContext tenant = CreateTenant();
        ClsFexRequest request = FexRequest(76328);
        request.TipoExpo = 2;
        request.CbteTipo = 19;
        var transport = new RecordingSoapTransport(_ => new FexAuthorizeResponse());
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "safe-service"));
        InvoiceSubmission submission = CreateWsfexSubmissionForCurrentQuery(tenant, "fex-partial-current-" + field, request);
        InvoiceOperation pending = await SaveUnknownWsfexOperationAsync(journal, submission,
            PartialCurrentQueryResponse(field, request), cancellationToken);
        var evidence = new InvoiceRejectionEvidence(submission, SerializeXml(KnownMissingPermiso1550Response()),
            "qa-maintainer", "protected-runner", "Partial current identity must remain unresolved");

        Func<Task> adjudicate = () => service.AdjudicateWsfexValidationRejectionAsync(
            tenant, submission.IdempotencyKey, pending.Version, evidence, cancellationToken);

        await adjudicate.Should().ThrowAsync<InvoiceConflictException>();
        (await journal.FindAsync(tenant.TenantId, submission.IdempotencyKey, cancellationToken))!.Should().BeEquivalentTo(pending);
        (await journal.ListRevisionsAsync(tenant.TenantId, submission.IdempotencyKey, cancellationToken)).Should().BeEmpty();
        (await service.ListWsfexRejectionAdjudicationsAsync(tenant, submission.IdempotencyKey,
            cancellationToken: cancellationToken)).Should().BeEmpty();
        transport.CallCount.Should().Be(0);
    }

    [Theory]
    [InlineData("evil-authorize-result")]
    [InlineData("evil-error")]
    [InlineData("evil-result-auth")]
    [InlineData("duplicate-error-code")]
    [InlineData("nested-error-code")]
    [InlineData("nested-error-message")]
    [InlineData("evil-events")]
    [InlineData("duplicate-events")]
    [InlineData("nested-event")]
    [InlineData("duplicate-event-code")]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_contract_local_names_in_foreign_namespaces(string variant)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ArcaTenantContext tenant = CreateTenant();
        ClsFexRequest request = FexRequest(76329);
        request.TipoExpo = 2;
        request.CbteTipo = 19;
        var transport = new RecordingSoapTransport(_ => new FexAuthorizeResponse());
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "safe-service"));
        InvoiceSubmission submission = CreateWsfexSubmissionForCurrentQuery(tenant, "fex-evil-namespace-" + variant, request);
        InvoiceOperation pending = await SaveUnknownWsfexOperationAsync(journal, submission,
            SerializeXml(new FexGetCmpResponse2 { FexGetCmpResult = new FexGetCmpResponse
                { FexErr = new ClsFexErr { ErrCode = 1020 } } }), cancellationToken);
        var evidence = new InvoiceRejectionEvidence(submission, Malformed1550ResponseXml(variant),
            "qa-maintainer", "protected-runner", "Foreign namespace contract names are ambiguous");

        Func<Task> adjudicate = () => service.AdjudicateWsfexValidationRejectionAsync(
            tenant, submission.IdempotencyKey, pending.Version, evidence, cancellationToken);

        await adjudicate.Should().ThrowAsync<InvoiceConflictException>();
        (await journal.FindAsync(tenant.TenantId, submission.IdempotencyKey, cancellationToken))!.Should().BeEquivalentTo(pending);
        (await journal.ListRevisionsAsync(tenant.TenantId, submission.IdempotencyKey, cancellationToken)).Should().BeEmpty();
        (await service.ListWsfexRejectionAdjudicationsAsync(tenant, submission.IdempotencyKey,
            cancellationToken: cancellationToken)).Should().BeEmpty();
        transport.CallCount.Should().Be(0);
    }

    [Theory]
    [InlineData("evil-query-result")]
    [InlineData("evil-voucher-result")]
    [InlineData("evil-query-error")]
    [InlineData("duplicate-identity")]
    [InlineData("wrapped-voucher-result")]
    [InlineData("query-result-auth")]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_current_query_contract_names_in_foreign_namespaces(string variant)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ArcaTenantContext tenant = CreateTenant();
        ClsFexRequest request = FexRequest(76330);
        request.TipoExpo = 2;
        request.CbteTipo = 19;
        var transport = new RecordingSoapTransport(_ => new FexAuthorizeResponse());
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "safe-service"));
        InvoiceSubmission submission = CreateWsfexSubmissionForCurrentQuery(tenant, "fex-current-evil-namespace-" + variant, request);
        InvoiceOperation pending = await SaveUnknownWsfexOperationAsync(journal, submission,
            CurrentQueryWithForeignContractName(variant), cancellationToken);
        var evidence = new InvoiceRejectionEvidence(submission, SerializeXml(KnownMissingPermiso1550Response()),
            "qa-maintainer", "protected-runner", "Foreign namespace current-response nodes are ambiguous");

        Func<Task> adjudicate = () => service.AdjudicateWsfexValidationRejectionAsync(
            tenant, submission.IdempotencyKey, pending.Version, evidence, cancellationToken);

        await adjudicate.Should().ThrowAsync<InvoiceConflictException>();
        (await journal.FindAsync(tenant.TenantId, submission.IdempotencyKey, cancellationToken))!.Should().BeEquivalentTo(pending);
        (await journal.ListRevisionsAsync(tenant.TenantId, submission.IdempotencyKey, cancellationToken)).Should().BeEmpty();
        (await service.ListWsfexRejectionAdjudicationsAsync(tenant, submission.IdempotencyKey,
            cancellationToken: cancellationToken)).Should().BeEmpty();
        transport.CallCount.Should().Be(0);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("other-code")]
    [InlineData("wrong-message")]
    [InlineData("authorization-result")]
    public async Task AdjudicateWsfexValidationRejectionAsync_rejects_arbitrary_current_authorization_responses(string variant)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ArcaTenantContext tenant = CreateTenant();
        ClsFexRequest request = FexRequest(76331);
        request.TipoExpo = 2;
        request.CbteTipo = 19;
        var transport = new RecordingSoapTransport(_ => new FexAuthorizeResponse());
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        var journal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "safe-service"));
        InvoiceSubmission submission = CreateWsfexSubmissionForCurrentQuery(tenant, "fex-current-auth-" + variant, request);
        InvoiceOperation pending = await SaveUnknownWsfexOperationAsync(journal, submission,
            CurrentAuthorizationResponse(variant), cancellationToken);
        var evidence = new InvoiceRejectionEvidence(submission, SerializeXml(KnownMissingPermiso1550Response()),
            "qa-maintainer", "protected-runner", "Current arbitrary authorization response is not definitive");

        Func<Task> adjudicate = () => service.AdjudicateWsfexValidationRejectionAsync(
            tenant, submission.IdempotencyKey, pending.Version, evidence, cancellationToken);

        await adjudicate.Should().ThrowAsync<InvoiceConflictException>();
        (await journal.FindAsync(tenant.TenantId, submission.IdempotencyKey, cancellationToken))!.Should().BeEquivalentTo(pending);
        (await journal.ListRevisionsAsync(tenant.TenantId, submission.IdempotencyKey, cancellationToken)).Should().BeEmpty();
        (await service.ListWsfexRejectionAdjudicationsAsync(tenant, submission.IdempotencyKey,
            cancellationToken: cancellationToken)).Should().BeEmpty();
        transport.CallCount.Should().Be(0);
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

    [Fact]
    public async Task ResumeAsync_recovers_authorization_after_journal_completion_failure_and_preserves_remote_evidence()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(call => call.ResponseType == typeof(FecaeSolicitarResponse)
            ? WsfeResponse("A", "71234567890123")
            : new FeCompConsultarResponse
            {
                FeCompConsultarResult = new FeCompConsultaResponse
                {
                    ResultGet = new FeCompConsResponse
                    {
                        Concepto = 1, DocTipo = 80, DocNro = CustomerNumber, CbteDesde = 12, CbteHasta = 12,
                        CbteFch = "20261006", ImpTotal = 121, ImpTotConc = 0, ImpNeto = 100, ImpOpEx = 0,
                        ImpIva = 21, ImpTrib = 0, MonId = "PES", MonCotiz = 1, PtoVta = 1, CbteTipo = 1,
                        EmisionTipo = "CAE", Resultado = "A", CodAutorizacion = "71234567890123"
                    }
                }
            });
        var innerJournal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "completion-failure"));
        var journal = new FailAuthorizedCompletionOnceJournal(innerJournal);
        var coordinator = new InvoiceCoordinator(journal);
        var service = new SafeInvoiceService(coordinator, new Wsfev1Service(transport,
            new RecordingArcaTicketProvider(TicketTestData.Create())), new Wsfexv1Service(transport,
            new RecordingArcaTicketProvider(TicketTestData.Create())), new Wsmtxcav1Service(transport,
            new RecordingArcaTicketProvider(TicketTestData.Create())));

        Func<Task> initial = () => service.AuthorizeWsfeAsync(CreateTenant(), "journal-failure", WsfeRequest(), cancellationToken);
        await initial.Should().ThrowAsync<IOException>();

        InvoiceOperation uncertain = (await innerJournal.FindAsync("tenant-safe", "journal-failure", cancellationToken))!;
        uncertain.State.Should().Be(InvoiceState.Unknown);
        uncertain.ResponseXml.Should().Contain("71234567890123", "the authorization response is evidence even when its terminal journal write fails");

        InvoiceOperation recovered = await service.ResumeAsync(CreateTenant(), "journal-failure", cancellationToken);

        recovered.State.Should().Be(InvoiceState.Authorized);
        recovered.AuthorizationCode.Should().Be("71234567890123");
        transport.CallCount.Should().Be(2);
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FECAESolicitar", StringComparison.Ordinal));
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FECompConsultar", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResumeAsync_returns_committed_authorization_when_journal_acknowledgement_is_lost()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        var transport = new RecordingSoapTransport(_ => WsfeResponse("A", "71234567890123"));
        var innerJournal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "lost-ack"));
        var service = CreateServiceWithJournal(new CommitThenFailAuthorizedJournal(innerJournal), transport);

        Func<Task> initial = () => service.AuthorizeWsfeAsync(CreateTenant(), "lost-ack", WsfeRequest(), cancellationToken);
        await initial.Should().ThrowAsync<IOException>().WithMessage("The journal commit succeeded but its acknowledgement was lost.");

        (await innerJournal.FindAsync("tenant-safe", "lost-ack", cancellationToken))!.State.Should().Be(InvoiceState.Authorized);
        InvoiceOperation recovered = await service.ResumeAsync(CreateTenant(), "lost-ack", cancellationToken);

        recovered.State.Should().Be(InvoiceState.Authorized);
        recovered.AuthorizationCode.Should().Be("71234567890123");
        transport.CallCount.Should().Be(1, "a committed authorization is returned from the journal without another SOAP request");
    }

    [Fact]
    public async Task ResumeAsync_reconciles_after_both_completion_writes_fail_and_the_submission_lease_expires()
    {
        using var database = new TemporaryDatabase();
        var clock = new AdjustableTimeProvider(DateTimeOffset.UtcNow);
        var innerJournal = new SqliteInvoiceJournal(InvoiceTestData.NewDatabasePath(database, "double-completion-failure"), clock);
        var journal = new FailBothCompletionAttemptsJournal(innerJournal);
        var transport = new RecordingSoapTransport(call => call.ResponseType == typeof(FecaeSolicitarResponse)
            ? WsfeResponse("A", "71234567890123")
            : WsfeQueryResponse("71234567890123"));
        var service = CreateServiceWithJournal(journal, transport, TimeSpan.FromSeconds(1));
        ArcaTenantContext tenant = CreateTenant();

        Func<Task> initial = () => service.AuthorizeWsfeAsync(tenant, "double-failure", WsfeRequest(), TestContext.Current.CancellationToken);
        await initial.Should().ThrowAsync<IOException>().WithMessage("The authorization completion failed.");
        (await innerJournal.FindAsync(tenant.TenantId, "double-failure", TestContext.Current.CancellationToken))!
            .State.Should().Be(InvoiceState.Submitting);

        clock.Advance(TimeSpan.FromSeconds(2));
        InvoiceOperation recovered = await service.ResumeAsync(tenant, "double-failure", TestContext.Current.CancellationToken);

        recovered.State.Should().Be(InvoiceState.Authorized);
        recovered.AuthorizationCode.Should().Be("71234567890123");
        transport.CallCount.Should().Be(2);
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
    [InlineData("ObsComerciales", null, "")]
    [InlineData("ObsComerciales", "", null)]
    [InlineData("Obs", null, "")]
    [InlineData("Obs", "", null)]
    [InlineData("Incoterms", null, "")]
    [InlineData("Incoterms", "", null)]
    public async Task ResumeAsync_treats_only_absent_and_empty_wsfex_optional_fields_as_equivalent(
        string field, string? requestValue, string? responseValue)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ClsFexRequest original = FexRequest(45219);
        original.TipoExpo = 2;
        SetFexOptionalField(original, field, requestValue);
        var transport = new RecordingSoapTransport(call =>
        {
            if (call.ResponseType == typeof(FexAuthorizeResponse)) return new FexAuthorizeResponse();
            FexGetCmpResponse2 response = FexQueryResponse(original, alterItem: false);
            SetFexOptionalField(response.FexGetCmpResult!.FexResultGet!, field, responseValue);
            return response;
        });
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        ArcaTenantContext tenant = CreateTenant();

        InvoiceOperation first = await service.AuthorizeWsfexAsync(tenant, "fex-empty-optional-" + field, original, cancellationToken);
        InvoiceOperation reconciled = await service.ResumeAsync(tenant, "fex-empty-optional-" + field, cancellationToken);

        first.State.Should().Be(InvoiceState.Unknown);
        reconciled.State.Should().Be(InvoiceState.Authorized);
        reconciled.Submission.Payload.Should().Be(first.Submission.Payload);
        reconciled.Submission.Identity.Should().Be(first.Submission.Identity);
        transport.CallCount.Should().Be(2);
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FEXAuthorize", StringComparison.Ordinal));
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FEXGetCMP", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("ObsComerciales", "local", "remoto")]
    [InlineData("ObsComerciales", null, " ")]
    [InlineData("Obs", "local", "remoto")]
    [InlineData("Obs", null, " ")]
    [InlineData("Incoterms", "FOB", "CIF")]
    [InlineData("Incoterms", null, " ")]
    public async Task ResumeAsync_keeps_nonempty_and_whitespace_wsfex_optional_values_strict(
        string field, string? requestValue, string? responseValue)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ClsFexRequest original = FexRequest(45219);
        original.TipoExpo = 2;
        SetFexOptionalField(original, field, requestValue);
        var transport = new RecordingSoapTransport(call =>
        {
            if (call.ResponseType == typeof(FexAuthorizeResponse)) return new FexAuthorizeResponse();
            FexGetCmpResponse2 response = FexQueryResponse(original, alterItem: false);
            SetFexOptionalField(response.FexGetCmpResult!.FexResultGet!, field, responseValue);
            return response;
        });
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        ArcaTenantContext tenant = CreateTenant();

        InvoiceOperation first = await service.AuthorizeWsfexAsync(tenant, "fex-strict-optional-" + field, original, cancellationToken);
        InvoiceOperation reconciled = await service.ResumeAsync(tenant, "fex-strict-optional-" + field, cancellationToken);

        first.State.Should().Be(InvoiceState.Unknown);
        reconciled.State.Should().Be(InvoiceState.Conflict);
        reconciled.Submission.Payload.Should().Be(first.Submission.Payload);
        transport.CallCount.Should().Be(2);
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FEXAuthorize", StringComparison.Ordinal));
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FEXGetCMP", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", null)]
    public async Task ResumeAsync_keeps_mandatory_incoterms_strict_for_export_type_19(
        string? requestValue, string? responseValue)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ClsFexRequest original = FexRequest(45219);
        original.TipoExpo = 1;
        original.CbteTipo = 19;
        original.Incoterms = requestValue!;
        var transport = new RecordingSoapTransport(call =>
        {
            if (call.ResponseType == typeof(FexAuthorizeResponse)) return new FexAuthorizeResponse();
            FexGetCmpResponse2 response = FexQueryResponse(original, alterItem: false);
            response.FexGetCmpResult!.FexResultGet!.Incoterms = responseValue!;
            return response;
        });
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        ArcaTenantContext tenant = CreateTenant();
        string idempotencyKey = "fex-required-incoterms-" + (requestValue is null ? "absent" : "empty");

        InvoiceOperation first = await service.AuthorizeWsfexAsync(tenant, idempotencyKey, original, cancellationToken);
        InvoiceOperation reconciled = await service.ResumeAsync(tenant, idempotencyKey, cancellationToken);

        first.State.Should().Be(InvoiceState.Unknown);
        reconciled.State.Should().Be(InvoiceState.Conflict);
        reconciled.Submission.Payload.Should().Be(first.Submission.Payload);
        transport.CallCount.Should().Be(2);
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FEXAuthorize", StringComparison.Ordinal));
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FEXGetCMP", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResumeAsync_does_not_map_incoterms_description_zero_without_incoterms()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ClsFexRequest original = FexRequest(45219);
        original.TipoExpo = 2;
        original.Incoterms = null!;
        original.IncotermsDs = null!;
        var transport = new RecordingSoapTransport(call =>
        {
            if (call.ResponseType == typeof(FexAuthorizeResponse)) return new FexAuthorizeResponse();
            FexGetCmpResponse2 response = FexQueryResponse(original, alterItem: false);
            response.FexGetCmpResult!.FexResultGet!.IncotermsDs = "0";
            return response;
        });
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        ArcaTenantContext tenant = CreateTenant();
        const string idempotencyKey = "fex-incoterms-description-without-code";

        InvoiceOperation first = await service.AuthorizeWsfexAsync(tenant, idempotencyKey, original, cancellationToken);
        InvoiceOperation reconciled = await service.ResumeAsync(tenant, idempotencyKey, cancellationToken);

        first.State.Should().Be(InvoiceState.Unknown);
        reconciled.State.Should().Be(InvoiceState.Conflict);
        reconciled.Submission.Payload.Should().Be(first.Submission.Payload);
        transport.CallCount.Should().Be(2);
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FEXAuthorize", StringComparison.Ordinal));
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FEXGetCMP", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("IncotermsDs")]
    [InlineData("Quantity")]
    [InlineData("UnitPrice")]
    [InlineData("ItemTotal")]
    [InlineData("InvoiceTotal")]
    [InlineData("ExchangeRate")]
    [InlineData("Association")]
    public async Task ResumeAsync_keeps_incoterms_description_and_other_fiscal_fields_strict(string field)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ClsFexRequest original = FexRequest(45219);
        original.TipoExpo = 2;
        original.Incoterms = "FOB";
        original.ImpTotal = 20;
        original.CmpsAsoc.Add(new CmpAsoc { CbteTipo = 19, CbtePuntoVta = 2, CbteNro = 11, CbteCuit = 20123456789 });
        var transport = new RecordingSoapTransport(call =>
        {
            if (call.ResponseType == typeof(FexAuthorizeResponse)) return new FexAuthorizeResponse();
            FexGetCmpResponse2 response = FexQueryResponse(original, alterItem: false);
            ClsFexGetCmpr remote = response.FexGetCmpResult!.FexResultGet!;
            switch (field)
            {
                case "IncotermsDs": remote.IncotermsDs = "0"; break;
                case "Quantity": remote.Items[0].ProQty += 1; break;
                case "UnitPrice": remote.Items[0].ProPrecioUni += 1; break;
                case "ItemTotal": remote.Items[0].ProTotalItem += 1; break;
                case "InvoiceTotal": remote.ImpTotal += 1; break;
                case "ExchangeRate": remote.MonedaCtz += 1; break;
                case "Association": remote.CmpsAsoc[0].CbteNro += 1; break;
                default: throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown WSFEX fiscal field.");
            }
            return response;
        });
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        ArcaTenantContext tenant = CreateTenant();

        string idempotencyKey = "fex-strict-fiscal-projection-" + field;
        InvoiceOperation first = await service.AuthorizeWsfexAsync(tenant, idempotencyKey, original, cancellationToken);
        string frozenPayload = first.Submission.Payload;
        InvoiceOperation reconciled = await service.ResumeAsync(tenant, idempotencyKey, cancellationToken);

        first.State.Should().Be(InvoiceState.Unknown);
        reconciled.State.Should().Be(InvoiceState.Conflict);
        reconciled.Submission.Payload.Should().Be(frozenPayload);
        reconciled.Submission.Identity.Should().Be(first.Submission.Identity);
        transport.CallCount.Should().Be(2);
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FEXAuthorize", StringComparison.Ordinal));
        transport.Calls.Select(call => call.Action).Should().ContainSingle(action => action.EndsWith("/FEXGetCMP", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("error-1020-empty", InvoiceState.Unknown)]
    [InlineData("empty-without-error", InvoiceState.Unknown)]
    [InlineData("partial-identity", InvoiceState.Unknown)]
    [InlineData("complete-mismatch-with-error-1020", InvoiceState.Conflict)]
    public async Task ResumeAsync_keeps_inconclusive_wsfex_query_results_unknown(string responseKind, InvoiceState expected)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var database = new TemporaryDatabase();
        ClsFexRequest original = FexRequest(45219);
        var transport = new RecordingSoapTransport(call => call.ResponseType == typeof(FexAuthorizeResponse)
            ? new FexAuthorizeResponse()
            : FexQueryResponse(original, responseKind));
        var service = CreateService(database, transport, new RecordingArcaTicketProvider(TicketTestData.Create()));
        ArcaTenantContext tenant = CreateTenant();

        InvoiceOperation first = await service.AuthorizeWsfexAsync(tenant, "fex-inconclusive-" + responseKind, original, cancellationToken);
        InvoiceOperation reconciled = await service.ResumeAsync(tenant, "fex-inconclusive-" + responseKind, cancellationToken);

        first.State.Should().Be(InvoiceState.Unknown);
        reconciled.State.Should().Be(expected);
        reconciled.AuthorizationCode.Should().BeNull();
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

    private static SafeInvoiceService CreateServiceWithJournal(IInvoiceJournal journal, RecordingSoapTransport transport,
        TimeSpan? leaseDuration = null)
    {
        var coordinator = new InvoiceCoordinator(journal, leaseDuration);
        var tickets = new RecordingArcaTicketProvider(TicketTestData.Create());
        return new SafeInvoiceService(coordinator, new Wsfev1Service(transport, tickets),
            new Wsfexv1Service(transport, tickets), new Wsmtxcav1Service(transport, tickets));
    }

    private sealed class FailAuthorizedCompletionOnceJournal(IInvoiceJournal inner) : IInvoiceJournal
    {
        private int failed;
        public Task<InvoiceOperation> PrepareAsync(InvoiceSubmission submission, CancellationToken cancellationToken = default)
            => inner.PrepareAsync(submission, cancellationToken);
        public Task<InvoiceOperation> ReviseRejectedAsync(InvoiceSubmission replacement, long expectedVersion, CancellationToken cancellationToken = default)
            => inner.ReviseRejectedAsync(replacement, expectedVersion, cancellationToken);
        public Task<IReadOnlyList<InvoiceOperation>> ListRevisionsAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default)
            => inner.ListRevisionsAsync(tenantId, idempotencyKey, cancellationToken);
        public Task<InvoiceOperation?> FindAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default)
            => inner.FindAsync(tenantId, idempotencyKey, cancellationToken);
        public Task<IReadOnlyList<InvoiceOperation>> ListPendingAsync(string tenantId, int limit = 100, CancellationToken cancellationToken = default)
            => inner.ListPendingAsync(tenantId, limit, cancellationToken);
        public Task<InvoiceLease?> TryAcquireAsync(string tenantId, string idempotencyKey, TimeSpan leaseDuration, bool reconciliation, CancellationToken cancellationToken = default)
            => inner.TryAcquireAsync(tenantId, idempotencyKey, leaseDuration, reconciliation, cancellationToken);
        public Task<InvoiceOperation> CompleteAsync(InvoiceLease lease, InvoiceDecision decision, CancellationToken cancellationToken = default)
        {
            if (decision.State == InvoiceState.Authorized && Interlocked.Exchange(ref failed, 1) == 0)
                return Task.FromException<InvoiceOperation>(new IOException("Injected local journal failure after remote authorization."));
            return inner.CompleteAsync(lease, decision, cancellationToken);
        }
    }

    private sealed class CommitThenFailAuthorizedJournal(IInvoiceJournal inner) : IInvoiceJournal
    {
        public Task<InvoiceOperation> PrepareAsync(InvoiceSubmission submission, CancellationToken cancellationToken = default) => inner.PrepareAsync(submission, cancellationToken);
        public Task<InvoiceOperation> ReviseRejectedAsync(InvoiceSubmission replacement, long expectedVersion, CancellationToken cancellationToken = default) => inner.ReviseRejectedAsync(replacement, expectedVersion, cancellationToken);
        public Task<IReadOnlyList<InvoiceOperation>> ListRevisionsAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default) => inner.ListRevisionsAsync(tenantId, idempotencyKey, cancellationToken);
        public Task<InvoiceOperation?> FindAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default) => inner.FindAsync(tenantId, idempotencyKey, cancellationToken);
        public Task<IReadOnlyList<InvoiceOperation>> ListPendingAsync(string tenantId, int limit = 100, CancellationToken cancellationToken = default) => inner.ListPendingAsync(tenantId, limit, cancellationToken);
        public Task<InvoiceLease?> TryAcquireAsync(string tenantId, string idempotencyKey, TimeSpan leaseDuration, bool reconciliation, CancellationToken cancellationToken = default) => inner.TryAcquireAsync(tenantId, idempotencyKey, leaseDuration, reconciliation, cancellationToken);
        public async Task<InvoiceOperation> CompleteAsync(InvoiceLease lease, InvoiceDecision decision, CancellationToken cancellationToken = default)
        {
            InvoiceOperation result = await inner.CompleteAsync(lease, decision, cancellationToken);
            if (decision.State == InvoiceState.Authorized)
                throw new IOException("The journal commit succeeded but its acknowledgement was lost.");
            return result;
        }
    }

    private sealed class FailBothCompletionAttemptsJournal(IInvoiceJournal inner) : IInvoiceJournal
    {
        private int completionAttempts;
        public Task<InvoiceOperation> PrepareAsync(InvoiceSubmission submission, CancellationToken cancellationToken = default) => inner.PrepareAsync(submission, cancellationToken);
        public Task<InvoiceOperation> ReviseRejectedAsync(InvoiceSubmission replacement, long expectedVersion, CancellationToken cancellationToken = default) => inner.ReviseRejectedAsync(replacement, expectedVersion, cancellationToken);
        public Task<IReadOnlyList<InvoiceOperation>> ListRevisionsAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default) => inner.ListRevisionsAsync(tenantId, idempotencyKey, cancellationToken);
        public Task<InvoiceOperation?> FindAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken = default) => inner.FindAsync(tenantId, idempotencyKey, cancellationToken);
        public Task<IReadOnlyList<InvoiceOperation>> ListPendingAsync(string tenantId, int limit = 100, CancellationToken cancellationToken = default) => inner.ListPendingAsync(tenantId, limit, cancellationToken);
        public Task<InvoiceLease?> TryAcquireAsync(string tenantId, string idempotencyKey, TimeSpan leaseDuration, bool reconciliation, CancellationToken cancellationToken = default) => inner.TryAcquireAsync(tenantId, idempotencyKey, leaseDuration, reconciliation, cancellationToken);
        public Task<InvoiceOperation> CompleteAsync(InvoiceLease lease, InvoiceDecision decision, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref completionAttempts) <= 2)
                return Task.FromException<InvoiceOperation>(new IOException(decision.State == InvoiceState.Authorized
                    ? "The authorization completion failed." : "The uncertainty completion failed."));
            return inner.CompleteAsync(lease, decision, cancellationToken);
        }
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

    private static FexAuthorizeResponse KnownMissingPermiso1550Response() => new()
    {
        FexAuthorizeResult = new FexResponseAuthorize
        {
            FexErr = new ClsFexErr
            {
                ErrCode = 1550,
                ErrMsg = "Campo Permiso_existente madatorio: Debe ser S, N o vacio (debe enviarse tag)"
                },
                FexEvents = new ClsFexEvents { EventCode = 1, EventMsg = "Evento informativo" }
        }
    };

    private static string Malformed1550ResponseXml(string variant)
    {
        string valid = SerializeXml(KnownMissingPermiso1550Response());
        if (variant == "dtd") return "<!DOCTYPE FEXAuthorizeResponse [<!ENTITY e 'x'>]>" + valid;
        XDocument document = XDocument.Parse(valid);
        XElement root = document.Root!;
        XNamespace ns = "http://ar.gov.afip.dif.fexv1/";
        XElement authorizationResult = root.Element(ns + "FEXAuthorizeResult")!;
        switch (variant)
        {
            case "wrong-response-qname":
                root.ReplaceWith(new XElement(XName.Get("FEXAuthorizeResponse", "urn:unexpected"), new XElement(authorizationResult)));
                break;
            case "duplicate-authorize-result":
                root.Add(new XElement(authorizationResult));
                break;
            case "duplicate-error":
                authorizationResult.Add(new XElement(authorizationResult.Element(ns + "FEXErr")!));
                break;
            case "duplicate-result-auth":
                authorizationResult.Add(new XElement(ns + "FEXResultAuth"));
                authorizationResult.Add(new XElement(ns + "FEXResultAuth"));
                break;
            case "evil-authorize-result":
                root.Add(new XElement(XName.Get("FEXAuthorizeResult", "urn:evil"),
                    new XElement(XName.Get("FEXErr", "urn:evil"), new XElement(XName.Get("ErrCode", "urn:evil"), "0"))));
                break;
            case "evil-error":
                authorizationResult.Add(new XElement(XName.Get("FEXErr", "urn:evil"),
                    new XElement(XName.Get("ErrCode", "urn:evil"), "0")));
                break;
            case "evil-result-auth":
                authorizationResult.Add(new XElement(XName.Get("FEXResultAuth", "urn:evil"),
                    new XElement(XName.Get("Cae", "urn:evil"), "71234567890123")));
                break;
            case "extra-root-voucher-wrapper":
                root.Add(new XElement(ns + "Wrapper",
                    new XElement(ns + "FEXResultGet",
                        new XElement(ns + "Id", "76325"),
                        new XElement(ns + "Punto_vta", "1"),
                        new XElement(ns + "Cbte_tipo", "19"),
                        new XElement(ns + "Cbte_nro", "76325"),
                        new XElement(ns + "Cae", "71234567890123"))));
                break;
            case "duplicate-error-code":
                authorizationResult.Element(ns + "FEXErr")!.Add(new XElement(ns + "ErrCode", "0"));
                break;
            case "nested-error-code":
                authorizationResult.Element(ns + "FEXErr")!.Element(ns + "ErrCode")!
                    .ReplaceWith(new XElement(ns + "ErrCode", new XElement(ns + "Wrapper", "1550")));
                break;
            case "nested-error-message":
                authorizationResult.Element(ns + "FEXErr")!.Element(ns + "ErrMsg")!
                    .ReplaceWith(new XElement(ns + "ErrMsg", new XElement(ns + "Wrapper", "Permiso_existente debe enviarse tag")));
                break;
            case "evil-events":
                authorizationResult.Add(new XElement(XName.Get("FEXEvents", "urn:evil"),
                    new XElement(XName.Get("EventCode", "urn:evil"), "2")));
                break;
            case "duplicate-events":
                authorizationResult.Add(new XElement(ns + "FEXEvents", new XElement(ns + "EventCode", "2")));
                break;
            case "nested-event":
                authorizationResult.Element(ns + "FEXEvents")!.Add(new XElement(ns + "Wrapper",
                    new XElement(ns + "EventCode", "2")));
                break;
            case "duplicate-event-code":
                authorizationResult.Element(ns + "FEXEvents")!.Add(new XElement(ns + "EventCode", "2"));
                break;
        }
        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static InvoiceSubmission CreateWsfexSubmissionForCurrentQuery(ArcaTenantContext tenant, string key, ClsFexRequest request)
        => SafeInvoiceService.CreateWsfexSubmission(tenant, key, request);

    private static async Task<InvoiceOperation> SaveUnknownWsfexOperationAsync(SqliteInvoiceJournal journal,
        InvoiceSubmission submission, string responseXml, CancellationToken cancellationToken)
    {
        await journal.PrepareAsync(submission, cancellationToken);
        InvoiceLease lease = (await journal.TryAcquireAsync(submission.TenantId, submission.IdempotencyKey,
            TimeSpan.FromMinutes(1), reconciliation: false, cancellationToken))!;
        return await journal.CompleteAsync(lease, new InvoiceDecision(InvoiceState.Unknown, ResponseXml: responseXml), cancellationToken);
    }

    private static string NoResultCurrentQueryResponse() => SerializeXml(new FexGetCmpResponse2
    {
        FexGetCmpResult = new FexGetCmpResponse
        {
            FexResultGet = new ClsFexGetCmpr(),
            FexErr = new ClsFexErr { ErrCode = 1020 },
            FexEvents = new ClsFexEvents { EventCode = 1, EventMsg = "Evento informativo" }
        }
    });

    private static string PartialCurrentQueryResponse(string field, ClsFexRequest request)
    {
        var remote = new ClsFexGetCmpr();
        switch (field)
        {
            case "Id": remote.Id = request.Id; break;
            case "Cbte_tipo": remote.CbteTipo = request.CbteTipo; break;
            case "Punto_vta": remote.PuntoVta = request.PuntoVta; break;
            case "Cbte_nro": remote.CbteNro = request.CbteNro; break;
            case "Cae":
            case "Resultado":
            case "Fecha_cbte":
                break;
            default: throw new ArgumentOutOfRangeException(nameof(field));
        }
        string xml = SerializeXml(new FexGetCmpResponse2
        {
            FexGetCmpResult = new FexGetCmpResponse
            {
                FexResultGet = remote,
                FexErr = new ClsFexErr { ErrCode = 1020 }
            }
        });
        if (field is "Cae" or "Resultado" or "Fecha_cbte")
        {
            XNamespace ns = "http://ar.gov.afip.dif.fexv1/";
            XDocument document = XDocument.Parse(xml);
            document.Root!.Element(ns + "FEXGetCMPResult")!.Element(ns + "FEXResultGet")!
                .Add(new XElement(ns + field, field == "Cae" ? "0" : field == "Resultado" ? "false" : "0"));
            xml = document.ToString(SaveOptions.DisableFormatting);
        }
        return xml;
    }

    private static string CurrentQueryWithForeignContractName(string variant)
    {
        XNamespace ns = "http://ar.gov.afip.dif.fexv1/";
        XNamespace evil = "urn:evil";
        XElement response = XElement.Parse(SerializeXml(new FexGetCmpResponse2
        {
            FexGetCmpResult = new FexGetCmpResponse
            {
                FexResultGet = new ClsFexGetCmpr(),
                FexErr = new ClsFexErr { ErrCode = 1020 }
            }
        }));
        XElement result = response.Element(ns + "FEXGetCMPResult")!;
        switch (variant)
        {
            case "evil-query-result": response.Add(new XElement(evil + "FEXGetCMPResult")); break;
            case "evil-voucher-result": result.Add(new XElement(evil + "FEXResultGet", new XElement(evil + "Cae", "71234567890123"))); break;
            case "evil-query-error": result.Add(new XElement(evil + "FEXErr", new XElement(evil + "ErrCode", "1020"))); break;
            case "duplicate-identity": result.Element(ns + "FEXResultGet")!.Add(new XElement(ns + "Id", "0")); break;
            case "nested-error-code":
                result.Element(ns + "FEXErr")!.Element(ns + "ErrCode")!
                    .ReplaceWith(new XElement(ns + "ErrCode", new XElement(ns + "Wrapper", "1020")));
                break;
            case "nested-error-message":
                result.Element(ns + "FEXErr")!.Add(new XElement(ns + "ErrMsg", new XElement(ns + "Wrapper", "No existe")));
                break;
            case "wrapped-voucher-result":
                XElement voucher = result.Element(ns + "FEXResultGet")!;
                voucher.Remove();
                result.Add(new XElement(ns + "VoucherWrapper", new XElement(ns + "FEXResultGet",
                    new XElement(ns + "Id", "76330"), new XElement(ns + "Cbte_tipo", "19"),
                    new XElement(ns + "Punto_vta", "2"), new XElement(ns + "Cbte_nro", "12"),
                    new XElement(ns + "Cae", "71234567890123"))));
                break;
            case "query-result-auth":
                result.Add(new XElement(ns + "FEXResultAuth", new XElement(ns + "Cae", "71234567890123")));
                break;
            default: throw new ArgumentOutOfRangeException(nameof(variant));
        }
        return response.ToString(SaveOptions.DisableFormatting);
    }

    private static string CurrentAuthorizationResponse(string variant)
    {
        FexAuthorizeResponse response = variant switch
        {
            "empty" => new FexAuthorizeResponse(),
            "other-code" => new FexAuthorizeResponse
            {
                FexAuthorizeResult = new FexResponseAuthorize
                {
                    FexErr = new ClsFexErr { ErrCode = 1551, ErrMsg = "Otro rechazo" }
                }
            },
            "wrong-message" => new FexAuthorizeResponse
            {
                FexAuthorizeResult = new FexResponseAuthorize
                {
                    FexErr = new ClsFexErr { ErrCode = 1550, ErrMsg = "Otro campo inválido" }
                }
            },
            "authorization-result" => new FexAuthorizeResponse
            {
                FexAuthorizeResult = new FexResponseAuthorize
                {
                    FexErr = new ClsFexErr { ErrCode = 1550, ErrMsg = "Permiso_existente debe enviarse tag" },
                    FexResultAuth = new ClsFexOutAuthorize { Cae = "71234567890123" }
                }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant))
        };
        return SerializeXml(response);
    }

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
        foreach (CmpAsoc association in original.CmpsAsoc)
            remote.CmpsAsoc.Add(new CmpAsoc
            {
                CbteTipo = association.CbteTipo,
                CbtePuntoVta = association.CbtePuntoVta,
                CbteNro = association.CbteNro,
                CbteCuit = association.CbteCuit
            });
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

    private static void SetFexOptionalField(ClsFexRequest request, string field, string? value)
    {
        switch (field)
        {
            case "ObsComerciales": request.ObsComerciales = value!; break;
            case "Obs": request.Obs = value!; break;
            case "Incoterms": request.Incoterms = value!; break;
            default: throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown optional WSFEX field.");
        }
    }

    private static void SetFexOptionalField(ClsFexGetCmpr response, string field, string? value)
    {
        switch (field)
        {
            case "ObsComerciales": response.ObsComerciales = value!; break;
            case "Obs": response.Obs = value!; break;
            case "Incoterms": response.Incoterms = value!; break;
            default: throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown optional WSFEX field.");
        }
    }

    private static FexGetCmpResponse2 FexQueryResponse(ClsFexRequest original, string responseKind)
    {
        if (responseKind is "error-1020-empty" or "empty-without-error")
        {
            return new FexGetCmpResponse2
            {
                FexGetCmpResult = new FexGetCmpResponse
                {
                    FexResultGet = new ClsFexGetCmpr(),
                    FexErr = responseKind == "error-1020-empty" ? new ClsFexErr { ErrCode = 1020 } : null
                }
            };
        }

        FexGetCmpResponse2 complete = FexQueryResponse(original, alterItem: responseKind == "complete-mismatch-with-error-1020");
        ClsFexGetCmpr result = complete.FexGetCmpResult!.FexResultGet!;
        if (responseKind == "partial-identity") result.CbteNro = 0;
        if (responseKind == "complete-mismatch-with-error-1020")
            complete.FexGetCmpResult.FexErr = new ClsFexErr { ErrCode = 1020 };
        return complete;
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

    private static FeCompConsultarResponse WsfeQueryResponse(string authorizationCode) => new()
    {
        FeCompConsultarResult = new FeCompConsultaResponse
        {
            ResultGet = new FeCompConsResponse
            {
                Concepto = 1, DocTipo = 80, DocNro = CustomerNumber, CbteDesde = 12, CbteHasta = 12,
                CbteFch = "20261006", ImpTotal = 121, ImpTotConc = 0, ImpNeto = 100, ImpOpEx = 0,
                ImpIva = 21, ImpTrib = 0, MonId = "PES", MonCotiz = 1, PtoVta = 1, CbteTipo = 1,
                EmisionTipo = "CAE", Resultado = "A", CodAutorizacion = authorizationCode
            }
        }
    };
}
