namespace NetArcaWs.Rendering.Shared;

/// <summary>Human-readable currency labels for source codes used by fiscal services.</summary>
internal static class CurrencyDisplay
{
    internal static string Code(string? value) => value?.ToUpperInvariant() switch
    {
        "PES" or "ARS" => "ARS",
        "DOL" or "USD" => "USD",
        "EUR" => "EUR",
        "BRL" => "BRL",
        null or "" => string.Empty,
        string code => code
    };

    internal static string Name(string? value) => Code(value) switch
    {
        "ARS" => "pesos",
        "USD" => "dólares estadounidenses",
        "EUR" => "euros",
        "BRL" => "reales",
        string code => code
    };

    internal static string Label(string? value) => string.IsNullOrWhiteSpace(Code(value))
        ? string.Empty : $"{Code(value)} ({Name(value)})";

    internal static bool IsPeso(string? value) => Code(value) == "ARS";
}
