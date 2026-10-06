using Xunit;

namespace NetArcaWs.IntegrationTests;

internal sealed record PadronTestCase(long Id, string PersonType, string KeyType);

/// <summary>
/// Public A4 QA identities, sampled from ARCA's three published categories on 2026-10-06.
/// https://www.afip.gob.ar/ws/ws_sr_padron_a4/datos-prueba-padron-a4.txt
/// Availability on Constancia, A10 and A13 is unverified until an authenticated run.
/// These are query fixtures, never the represented CUIT or authentication credentials.
/// </summary>
internal static class PadronTestCases
{
    private static readonly IReadOnlyList<PadronTestCase> All = Array.AsReadOnly<PadronTestCase>([
        new(20002307554, "FISICA", "CUIT"),
        new(20203032723, "FISICA", "CUIL"),
        new(30202020204, "JURIDICA", "CUIT")
    ]);
    private static readonly IReadOnlyList<PadronTestCase> CuitOnly = Array.AsReadOnly(All.Where(x => x.KeyType == "CUIT").ToArray());

    public static IReadOnlyList<PadronTestCase> ForService(string service) => service switch
    {
        "padron-a4" or "padron-a10" or "padron-a13" => All,
        "padron-a5" => CuitOnly,
        _ => throw new ArgumentOutOfRangeException(nameof(service))
    };

    public static void Validate(PadronTestCase expected, object response)
    {
        AuthenticatedLookupValidation.ValidateResponse(response);
        (long? id, string? personType, string? keyType) = response switch
        {
            Contracts.PadronA4.GetPersonaResponse r => (r.PersonaReturn?.Persona?.IdPersona, r.PersonaReturn?.Persona?.TipoPersona, r.PersonaReturn?.Persona?.TipoClave),
            Contracts.PadronA5.GetPersonaResponse r => (r.PersonaReturn?.DatosGenerales?.IdPersona, r.PersonaReturn?.DatosGenerales?.TipoPersona, r.PersonaReturn?.DatosGenerales?.TipoClave),
            Contracts.PadronA10.GetPersonaResponse r => (r.PersonaReturn?.Persona?.IdPersona, r.PersonaReturn?.Persona?.TipoPersona, r.PersonaReturn?.Persona?.TipoClave),
            Contracts.PadronA13.GetPersonaResponse r => (r.PersonaReturn?.Persona?.IdPersona, r.PersonaReturn?.Persona?.TipoPersona, r.PersonaReturn?.Persona?.TipoClave),
            _ => throw new InvalidOperationException("Unsupported Padron response type.")
        };
        // Boolean assertions keep remote values out of diagnostics.
        Assert.True(id == expected.Id, "Padron returned a different test identity.");
        Assert.True(personType == expected.PersonType, "Padron returned an unexpected person type.");
        Assert.True(keyType == expected.KeyType, "Padron returned an unexpected key type.");
    }

    public static async Task<IReadOnlyList<string>> RunAsync(string service,
        Func<PadronTestCase, CancellationToken, Task<object>> query,
        Func<string, CancellationToken, Task> report, CancellationToken token)
    {
        List<string> failures = [];
        foreach (PadronTestCase fixture in ForService(service))
        {
            token.ThrowIfCancellationRequested();
            string label = $"{service} / {fixture.PersonType} / {fixture.KeyType}";
            bool passed;
            try
            {
                Validate(fixture, await query(fixture, token));
                passed = true;
            }
            catch (Exception)
            {
                token.ThrowIfCancellationRequested();
                // Do not publish exception text, remote payloads or partial personal records.
                failures.Add(label);
                passed = false;
            }
            token.ThrowIfCancellationRequested();
            await report(passed ? $"- {label}: aprobado." : $"- {label}: falló (consulta o validación; sin datos remotos).", token);
        }
        return failures.AsReadOnly();
    }
}
