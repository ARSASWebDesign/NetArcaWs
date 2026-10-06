using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class PadronTestCasesTests
{
    private static readonly PadronTestCase FisicaCuit = new(20002307554, "FISICA", "CUIT");
    private static readonly PadronTestCase FisicaCuil = new(20203032723, "FISICA", "CUIL");
    private static readonly PadronTestCase JuridicaCuit = new(30202020204, "JURIDICA", "CUIT");

    [Theory]
    [InlineData("padron-a4", 3)]
    [InlineData("padron-a10", 3)]
    [InlineData("padron-a13", 3)]
    [InlineData("padron-a5", 2)]
    public void ForService_returns_official_representative_cases(string service, int count)
    {
        IReadOnlyList<PadronTestCase> cases = PadronTestCases.ForService(service);

        cases.Should().HaveCount(count);
        cases.Should().Contain(FisicaCuit);
        cases.Should().Contain(JuridicaCuit);
        if (service == "padron-a5")
        {
            cases.Should().NotContain(FisicaCuil);
        }
        else
        {
            cases.Should().Contain(FisicaCuil);
        }
    }

    [Fact]
    public void ForService_rejects_unknown_service()
    {
        Action act = () => PadronTestCases.ForService("padron-a99");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("padron-a4")]
    [InlineData("padron-a5")]
    [InlineData("padron-a10")]
    [InlineData("padron-a13")]
    public void Validate_accepts_matching_typed_response(string service)
    {
        PadronTestCases.Validate(FisicaCuit, ResponseFor(service, FisicaCuit)!);
    }

    [Theory]
    [InlineData("padron-a4")]
    [InlineData("padron-a5")]
    [InlineData("padron-a10")]
    [InlineData("padron-a13")]
    public void Validate_rejects_mismatched_person_id(string service)
    {
        PadronTestCase mismatch = FisicaCuit with { Id = FisicaCuit.Id + 1 };

        Action act = () => PadronTestCases.Validate(FisicaCuit, ResponseFor(service, mismatch)!);

        act.Should().Throw<Xunit.Sdk.XunitException>();
    }

    [Theory]
    [InlineData("padron-a4")]
    [InlineData("padron-a5")]
    [InlineData("padron-a10")]
    [InlineData("padron-a13")]
    public void Validate_rejects_mismatched_person_type(string service)
    {
        PadronTestCase mismatch = FisicaCuit with { PersonType = "JURIDICA" };

        Action act = () => PadronTestCases.Validate(FisicaCuit, ResponseFor(service, mismatch)!);

        act.Should().Throw<Xunit.Sdk.XunitException>();
    }

    [Theory]
    [InlineData("padron-a4")]
    [InlineData("padron-a5")]
    [InlineData("padron-a10")]
    [InlineData("padron-a13")]
    public void Validate_rejects_mismatched_key_type(string service)
    {
        PadronTestCase mismatch = FisicaCuit with { KeyType = "CUIL" };

        Action act = () => PadronTestCases.Validate(FisicaCuit, ResponseFor(service, mismatch)!);

        act.Should().Throw<Xunit.Sdk.XunitException>();
    }

    [Theory]
    [InlineData("padron-a4")]
    [InlineData("padron-a5")]
    [InlineData("padron-a10")]
    [InlineData("padron-a13")]
    public void Validate_rejects_missing_and_error_responses(string service)
    {
        Action nullResponse = () => PadronTestCases.Validate(FisicaCuit, null!);
        Action emptyResponse = () => PadronTestCases.Validate(FisicaCuit, ResponseFor(service, null)!);

        Assert.ThrowsAny<ArgumentNullException>(nullResponse);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(emptyResponse);
    }

    [Fact]
    public void Validate_rejects_A5_error_nodes()
    {
        NetArcaWs.Contracts.PadronA5.PersonaReturn personaReturn = new()
        {
            DatosGenerales = new NetArcaWs.Contracts.PadronA5.DatosGenerales
            {
                IdPersona = FisicaCuit.Id,
                TipoPersona = FisicaCuit.PersonType,
                TipoClave = FisicaCuit.KeyType,
                Nombre = "Fixture"
            },
            ErrorConstancia = new NetArcaWs.Contracts.PadronA5.ErrorConstancia()
        };
        personaReturn.ErrorConstancia.Error.Add("synthetic upstream error");
        NetArcaWs.Contracts.PadronA5.GetPersonaResponse response = new() { PersonaReturn = personaReturn };

        Action act = () => PadronTestCases.Validate(FisicaCuit, response);

        act.Should().Throw<Xunit.Sdk.XunitException>();
    }

    [Fact]
    public async Task RunAsync_continues_after_lookup_failure_and_does_not_report_exception_details()
    {
        List<string> reports = [];
        int lookups = 0;

        IReadOnlyList<string> failures = await PadronTestCases.RunAsync(
            "padron-a4",
            (testCase, _) =>
            {
                lookups++;
                if (testCase == FisicaCuit)
                {
                    throw new InvalidOperationException("sensitive response body");
                }

                return Task.FromResult<object>(ResponseFor("padron-a4", testCase)!);
            },
            (line, _) =>
            {
                reports.Add(line);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        lookups.Should().Be(3);
        failures.Should().Equal("padron-a4 / FISICA / CUIT");
        reports.Should().Equal(
            "- padron-a4 / FISICA / CUIT: falló (consulta o validación; sin datos remotos).",
            "- padron-a4 / FISICA / CUIL: aprobado.",
            "- padron-a4 / JURIDICA / CUIT: aprobado.");
        string.Join("\n", reports).Should().NotContain("sensitive response body");
    }

    [Fact]
    public async Task RunAsync_stops_when_cancellation_is_requested()
    {
        using CancellationTokenSource source = new();
        List<string> reports = [];
        int lookups = 0;

        Func<PadronTestCase, CancellationToken, Task<object>> query = (_, _) =>
        {
            lookups++;
            source.Cancel();
            return Task.FromResult<object>(ResponseFor("padron-a4", FisicaCuit)!);
        };

        Func<string, CancellationToken, Task> report = (line, _) =>
        {
            reports.Add(line);
            return Task.CompletedTask;
        };

        Func<Task> act = () => PadronTestCases.RunAsync("padron-a4", query, report, source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        lookups.Should().Be(1);
        reports.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_throws_clean_cancellation_when_query_fails_after_cancellation()
    {
        using CancellationTokenSource source = new();
        List<string> reports = [];

        Func<PadronTestCase, CancellationToken, Task<object>> query = (_, _) =>
        {
            source.Cancel();
            throw new InvalidOperationException("sensitive response body");
        };
        Func<string, CancellationToken, Task> report = (line, _) =>
        {
            reports.Add(line);
            return Task.CompletedTask;
        };

        Func<Task> act = () => PadronTestCases.RunAsync("padron-a4", query, report, source.Token);

        var assertion = await act.Should().ThrowAsync<OperationCanceledException>();
        Exception exception = assertion.Which;
        exception.Message.Should().NotContain("sensitive response body");
        exception.InnerException.Should().BeNull();
        reports.Should().BeEmpty();
    }

    private static object? ResponseFor(string service, PadronTestCase? returned)
    {
        return service switch
        {
            "padron-a4" => new NetArcaWs.Contracts.PadronA4.GetPersonaResponse
            {
                PersonaReturn = returned is null ? null! : new NetArcaWs.Contracts.PadronA4.PersonaReturn
                {
                    Persona = new NetArcaWs.Contracts.PadronA4.Persona
                    {
                        IdPersona = returned.Id,
                        TipoPersona = returned.PersonType,
                        TipoClave = returned.KeyType,
                        Nombre = "Fixture"
                    }
                }
            },
            "padron-a5" => new NetArcaWs.Contracts.PadronA5.GetPersonaResponse
            {
                PersonaReturn = returned is null ? null! : new NetArcaWs.Contracts.PadronA5.PersonaReturn
                {
                    DatosGenerales = new NetArcaWs.Contracts.PadronA5.DatosGenerales
                    {
                        IdPersona = returned.Id,
                        TipoPersona = returned.PersonType,
                        TipoClave = returned.KeyType,
                        Nombre = "Fixture"
                    }
                }
            },
            "padron-a10" => new NetArcaWs.Contracts.PadronA10.GetPersonaResponse
            {
                PersonaReturn = returned is null ? null! : new NetArcaWs.Contracts.PadronA10.PersonaReturn
                {
                    Persona = new NetArcaWs.Contracts.PadronA10.Persona
                    {
                        IdPersona = returned.Id,
                        TipoPersona = returned.PersonType,
                        TipoClave = returned.KeyType,
                        Nombre = "Fixture"
                    }
                }
            },
            "padron-a13" => new NetArcaWs.Contracts.PadronA13.GetPersonaResponse
            {
                PersonaReturn = returned is null ? null! : new NetArcaWs.Contracts.PadronA13.PersonaReturn
                {
                    Persona = new NetArcaWs.Contracts.PadronA13.Persona
                    {
                        IdPersona = returned.Id,
                        TipoPersona = returned.PersonType,
                        TipoClave = returned.KeyType,
                        Nombre = "Fixture"
                    }
                }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(service))
        };
    }
}
