using System.Globalization;

namespace NetArcaWs.IntegrationTests;

internal static class HomologationInputValidation
{
    public static long ReadPositive(string name, string? rawValue, long maximum)
    {
        if (!long.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out long value)
            || value < 1 || value > maximum)
            throw new InvalidOperationException($"Missing or invalid homologation configuration: {name}.");
        return value;
    }
}
