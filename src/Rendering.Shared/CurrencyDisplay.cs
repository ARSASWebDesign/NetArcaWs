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
        "ARS" => "Peso Argentino",
        "USD" => "Dólar Estadounidense",
        "EUR" => "Euro",
        "BRL" => "Real Brasilero",
        string code => code
    };

    internal static string Label(string? value)
    {
        string code = Code(value);
        if (string.IsNullOrWhiteSpace(code)) return string.Empty;
        string name = Name(value);
        return code == name ? code : $"{code} - {name}";
    }

    internal static bool IsPeso(string? value) => Code(value) == "ARS";
}
