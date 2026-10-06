using NetArcaWs.Transport;
using NetArcaWs.Wsaa;
using Xunit.Sdk;

namespace NetArcaWs.IntegrationTests;

internal sealed record PadronTestCase(long Id, string PersonType, string KeyType, string? PublishedKeyType = null);

/// <summary>
/// Public A4 QA identities, sampled from ARCA's three published categories on 2026-10-06.
/// https://www.afip.gob.ar/ws/ws_sr_padron_a4/datos-prueba-padron-a4.txt
/// Expected types verified against all four QA services on 2026-10-06.
/// The published CUIL sample currently returns CUIT; this does not cover a live CUIL response.
/// These are query fixtures, never the represented CUIT or authentication credentials.
/// </summary>
internal static class PadronTestCases
{
    private static readonly IReadOnlyList<PadronTestCase> All = Array.AsReadOnly<PadronTestCase>([
        new(20188192514, "FISICA", "CUIT"),
        new(20188027963, "FISICA", "CUIT", PublishedKeyType: "CUIL"),
        new(30202020204, "JURIDICA", "CUIT")
    ]);

    public static IReadOnlyList<PadronTestCase> ForService(string service) => service switch
    {
        "padron-a4" or "padron-a5" or "padron-a10" or "padron-a13" => All,
        _ => throw new ArgumentOutOfRangeException(nameof(service))
    };

    public static void Validate(PadronTestCase expected, object response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response is Contracts.PadronA5.GetPersonaResponse a5 &&
            (a5.PersonaReturn?.ErrorConstancia?.Error.Count > 0 ||
             a5.PersonaReturn?.ErrorMonotributo?.Error.Count > 0 ||
             a5.PersonaReturn?.ErrorRegimenGeneral?.Error.Count > 0))
            throw new PadronValidationException(PadronFailure.FunctionalError);
        (long? id, string? personType, string? keyType) = response switch
        {
            Contracts.PadronA4.GetPersonaResponse r => (r.PersonaReturn?.Persona?.IdPersona, r.PersonaReturn?.Persona?.TipoPersona, r.PersonaReturn?.Persona?.TipoClave),
            Contracts.PadronA5.GetPersonaResponse r => (r.PersonaReturn?.DatosGenerales?.IdPersona, r.PersonaReturn?.DatosGenerales?.TipoPersona, r.PersonaReturn?.DatosGenerales?.TipoClave),
            Contracts.PadronA10.GetPersonaResponse r => (r.PersonaReturn?.Persona?.IdPersona, r.PersonaReturn?.Persona?.TipoPersona, r.PersonaReturn?.Persona?.TipoClave),
            Contracts.PadronA13.GetPersonaResponse r => (r.PersonaReturn?.Persona?.IdPersona, r.PersonaReturn?.Persona?.TipoPersona, r.PersonaReturn?.Persona?.TipoClave),
            _ => throw new InvalidOperationException("Unsupported Padron response type.")
        };
        if (id is null) throw new PadronValidationException(PadronFailure.MissingPerson);
        try { AuthenticatedLookupValidation.ValidateResponse(response); }
        catch (XunitException) { throw new PadronValidationException(PadronFailure.InvalidPersonData); }
        // Typed categories keep all remote values out of diagnostics.
        if (id != expected.Id) throw new PadronValidationException(PadronFailure.IdentityMismatch);
        if (personType != expected.PersonType) throw new PadronValidationException(PadronFailure.PersonTypeMismatch);
        if (keyType != expected.KeyType) throw new PadronValidationException(PadronFailure.KeyTypeMismatch);
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
            if (fixture.PublishedKeyType is not null)
                label += $" / listado {fixture.PublishedKeyType}";
            PadronFailure? failure = null;
            try
            {
                Validate(fixture, await query(fixture, token));
            }
            catch (Exception exception)
            {
                token.ThrowIfCancellationRequested();
                // Do not publish exception text, remote payloads or partial personal records.
                failure = Classify(exception);
                failures.Add($"{label} [{failure}]");
            }
            token.ThrowIfCancellationRequested();
            await report(failure is null ? $"- {label}: aprobado." : $"- {label}: falló [{failure}].", token);
        }
        return failures.AsReadOnly();
    }

    // Only fixed labels leave this boundary. Unknown codes/reasons are never interpolated.
    private static PadronFailure Classify(Exception exception) => exception switch
    {
        PadronValidationException validation => validation.Failure,
        WsaaSoapException wsaa when IsFaultCode(wsaa.FaultCode, "coe.alreadyAuthenticated") => PadronFailure.AlreadyAuthenticated,
        WsaaSoapException => PadronFailure.Authentication,
        SoapFaultException { Reason: "No existe persona con ese Id" } => PadronFailure.PersonNotFound,
        SoapFaultException => PadronFailure.SoapFault,
        HttpRequestException => PadronFailure.Transport,
        TimeoutException or OperationCanceledException => PadronFailure.Timeout,
        FormatException or System.Xml.XmlException => PadronFailure.InvalidResponse,
        _ => PadronFailure.Unexpected
    };

    private static bool IsFaultCode(string code, string expected)
    {
        if (code == expected) return true;
        int colon = code.IndexOf(':');
        if (colon <= 0 || code[(colon + 1)..] != expected) return false;
        try { System.Xml.XmlConvert.VerifyNCName(code[..colon]); }
        catch (System.Xml.XmlException) { return false; }
        return true;
    }

    private enum PadronFailure
    {
        AlreadyAuthenticated, Authentication, PersonNotFound, SoapFault, Transport,
        Timeout, InvalidResponse, MissingPerson, FunctionalError, InvalidPersonData,
        IdentityMismatch, PersonTypeMismatch, KeyTypeMismatch, Unexpected
    }

    private sealed class PadronValidationException(PadronFailure failure) : XunitException(failure.ToString())
    {
        public PadronFailure Failure { get; } = failure;
    }
}
