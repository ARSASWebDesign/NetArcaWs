using System.Net;
using NetArcaWs.Transport;
using NetArcaWs.Wsaa;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class PadronDiagnosticsTests
{
    private static readonly PadronTestCase FirstA4 = new(20002307554, "FISICA", "CUIT");
    private static readonly PadronTestCase FirstA5 = new(20002307554, "FISICA", "CUIT");

    [Theory]
    [InlineData("mismatchId", "IdentityMismatch")]
    [InlineData("tipoPersona", "PersonTypeMismatch")]
    [InlineData("tipoClave", "KeyTypeMismatch")]
    [InlineData("missingpersona", "MissingPerson")]
    [InlineData("sinNombreEstado", "InvalidPersonData")]
    public async Task RunAsync_a4_reports_safe_validation_category(string variation, string category)
    {
        const string remoteXml = "<PersonaReturn>synthetic-private-payload</PersonaReturn>";
        const string returnedType = "REMOTE-TYPE-SENTINEL";
        List<string> reports = [];

        IReadOnlyList<string> failures = await PadronTestCases.RunAsync(
            "padron-a4",
            (testCase, _) => Task.FromResult<object>(testCase == FirstA4
                ? A4Response(variation, returnedType, testCase)
                : A4Response("valid", returnedType, testCase)),
            (line, _) =>
            {
                reports.Add(line);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        AssertCategoryIsSafe(failures, reports, category, remoteXml, returnedType);
    }

    [Theory]
    [InlineData("ErrorConstancia", "FunctionalError")]
    [InlineData("ErrorMonotributo", "FunctionalError")]
    [InlineData("ErrorRegimenGeneral", "FunctionalError")]
    public async Task RunAsync_a5_reports_functional_error_category(string errorField, string category)
    {
        const string remoteXml = "<Error>synthetic-private-payload</Error>";
        List<string> reports = [];

        IReadOnlyList<string> failures = await PadronTestCases.RunAsync(
            "padron-a5",
            (testCase, _) => Task.FromResult<object>(A5Response(
                testCase == FirstA5 ? errorField : null, testCase)),
            (line, _) =>
            {
                reports.Add(line);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        AssertCategoryIsSafe(failures, reports, category, remoteXml, "synthetic upstream error");
    }

    [Theory]
    [MemberData(nameof(ExceptionCases))]
    public async Task RunAsync_reports_safe_exception_category(Exception exception, string category)
    {
        List<string> reports = [];

        IReadOnlyList<string> failures = await PadronTestCases.RunAsync(
            "padron-a4",
            (testCase, _) => testCase == FirstA4
                ? Task.FromException<object>(exception)
                : Task.FromResult<object>(A4Response("valid", "REMOTE-TYPE-SENTINEL", testCase)),
            (line, _) =>
            {
                reports.Add(line);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        List<string> forbidden = [exception.Message, exception.ToString(), "REMOTE-TYPE-SENTINEL"];
        if (exception is WsaaSoapException wsaa)
            forbidden.AddRange([wsaa.FaultCode, wsaa.FaultString, wsaa.Detail ?? string.Empty]);
        if (exception is SoapFaultException soap)
            forbidden.AddRange([soap.Code, soap.Reason, soap.Detail ?? string.Empty]);
        AssertCategoryIsSafe(failures, reports, category, [.. forbidden]);
    }

    [Fact]
    public async Task RunAsync_maps_allowlisted_soap_reason_to_person_not_found()
    {
        List<string> reports = [];
        SoapFaultException exception = new(
            "synthetic.fault", "No existe persona con ese Id", "<detail>synthetic secret</detail>",
            HttpStatusCode.InternalServerError);

        IReadOnlyList<string> failures = await PadronTestCases.RunAsync(
            "padron-a4",
            (testCase, _) => testCase == FirstA4
                ? Task.FromException<object>(exception)
                : Task.FromResult<object>(A4Response("valid", "REMOTE-TYPE-SENTINEL", testCase)),
            (line, _) =>
            {
                reports.Add(line);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        AssertCategoryIsSafe(failures, reports, "PersonNotFound", exception.Reason, exception.Detail!, "REMOTE-TYPE-SENTINEL");
    }

    [Theory]
    [MemberData(nameof(NearMatchFaultCases))]
    public async Task RunAsync_does_not_infer_categories_from_near_match_remote_text(Exception exception, string category)
    {
        List<string> reports = [];
        IReadOnlyList<string> failures = await PadronTestCases.RunAsync(
            "padron-a4",
            (testCase, _) => testCase == FirstA4
                ? Task.FromException<object>(exception)
                : Task.FromResult<object>(A4Response("valid", "REMOTE-TYPE-SENTINEL", testCase)),
            (line, _) =>
            {
                reports.Add(line);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        AssertCategoryIsSafe(failures, reports, category, exception.Message, exception.ToString(), "No existe persona con ese Id", "REMOTE-TYPE-SENTINEL");
    }

    public static TheoryData<Exception, string> ExceptionCases => new()
    {
        { new WsaaSoapException("coe.alreadyAuthenticated", "synthetic remote fault", "<detail>secret</detail>", HttpStatusCode.InternalServerError), "AlreadyAuthenticated" },
        { new WsaaSoapException("prefix.coe.alreadyAuthenticated.extra", "synthetic remote fault", "<detail>secret</detail>", HttpStatusCode.InternalServerError), "Authentication" },
        { new WsaaSoapException("synthetic.otherFault", "synthetic remote fault", "<detail>secret</detail>", HttpStatusCode.InternalServerError), "Authentication" },
        { new SoapFaultException("synthetic.fault", "synthetic remote fault", "<detail>secret</detail>", HttpStatusCode.InternalServerError), "SoapFault" },
        { new HttpRequestException("synthetic transport body"), "Transport" },
        { new TimeoutException("synthetic timeout body"), "Timeout" },
        { new TaskCanceledException("synthetic cancellation body"), "Timeout" },
        { new FormatException("synthetic malformed XML"), "InvalidResponse" },
        { new InvalidOperationException("synthetic unexpected body"), "Unexpected" }
    };

    public static TheoryData<Exception, string> NearMatchFaultCases => new()
    {
        { new SoapFaultException("synthetic.fault", "Prefix: No existe persona con ese Id", "<detail>synthetic secret</detail>", HttpStatusCode.InternalServerError), "SoapFault" },
        { new WsaaSoapException("coe.alreadyAuthenticated.extra", "synthetic remote fault", "<detail>synthetic secret</detail>", HttpStatusCode.InternalServerError), "Authentication" }
    };

    private static object A4Response(string variation, string remoteType, PadronTestCase expected)
    {
        NetArcaWs.Contracts.PadronA4.Persona? persona = variation == "missingpersona"
            ? null
            : new NetArcaWs.Contracts.PadronA4.Persona
            {
                IdPersona = variation == "mismatchId" ? expected.Id + 1 : expected.Id,
                TipoPersona = variation == "tipoPersona" ? remoteType : expected.PersonType,
                TipoClave = variation == "tipoClave" ? remoteType : expected.KeyType,
                Nombre = variation == "sinNombreEstado" ? null : "Synthetic fixture",
                EstadoClave = variation == "sinNombreEstado" ? null : "ACTIVO"
            };
        return new NetArcaWs.Contracts.PadronA4.GetPersonaResponse
        {
            PersonaReturn = new NetArcaWs.Contracts.PadronA4.PersonaReturn { Persona = persona! }
        };
    }

    private static object A5Response(string? errorField, PadronTestCase expected)
    {
        NetArcaWs.Contracts.PadronA5.PersonaReturn result = new()
        {
            DatosGenerales = new NetArcaWs.Contracts.PadronA5.DatosGenerales
            {
                IdPersona = expected.Id,
                TipoPersona = expected.PersonType,
                TipoClave = expected.KeyType,
                Nombre = "Synthetic fixture"
            }
        };
        if (errorField is not null)
        {
            switch (errorField)
            {
                case "ErrorConstancia":
                    result.ErrorConstancia = new NetArcaWs.Contracts.PadronA5.ErrorConstancia();
                    result.ErrorConstancia.Error.Add("synthetic upstream error");
                    break;
                case "ErrorMonotributo":
                    result.ErrorMonotributo = new NetArcaWs.Contracts.PadronA5.ErrorMonotributo();
                    result.ErrorMonotributo.Error.Add("synthetic upstream error");
                    break;
                case "ErrorRegimenGeneral":
                    result.ErrorRegimenGeneral = new NetArcaWs.Contracts.PadronA5.ErrorRegimenGeneral();
                    result.ErrorRegimenGeneral.Error.Add("synthetic upstream error");
                    break;
            }
        }
        return new NetArcaWs.Contracts.PadronA5.GetPersonaResponse { PersonaReturn = result };
    }

    private static void AssertCategoryIsSafe(
        IReadOnlyList<string> failures,
        IReadOnlyList<string> reports,
        string category,
        params string[] forbiddenValues)
    {
        Assert.Single(failures);
        Assert.Contains(failures, item => item.Contains($"[{category}]", StringComparison.Ordinal));
        Assert.Single(reports, line => line.Contains($"[{category}]", StringComparison.Ordinal));
        Assert.All(reports.Where(line => !line.Contains($"[{category}]", StringComparison.Ordinal)),
            line => Assert.EndsWith("aprobado.", line, StringComparison.Ordinal));
        string output = string.Join("\n", failures.Concat(reports));
        foreach (string forbidden in forbiddenValues)
            Assert.DoesNotContain(forbidden, output, StringComparison.Ordinal);
    }
}
