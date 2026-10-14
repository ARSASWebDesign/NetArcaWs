using System.Reflection;

namespace NetArcaWs.Rendering.Shared;

/// <summary>Immutable, validated presentation settings shared by the optional renderers.</summary>
internal sealed class RenderingStyle
{
    internal RenderingStyle(string? colorPrincipal = null)
    {
        ColorPrincipal = NormalizeColor(colorPrincipal ?? "#333333");
        TextOnPrincipal = ContrastText(ColorPrincipal);
    }

    internal string ColorPrincipal { get; }
    internal string TextOnPrincipal { get; }
    internal string CssVariables => $"--principal:{ColorPrincipal};--principal-text:{TextOnPrincipal}";

    internal static string NormalizeColor(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 7 || value[0] != '#' || !value.AsSpan(1).ToString().All(Uri.IsHexDigit))
            throw new ArgumentException("El color principal debe usar el formato #RRGGBB.", nameof(value));
        return value.ToUpperInvariant();
    }

    internal static string ContrastText(string normalizedColor)
    {
        string color = NormalizeColor(normalizedColor);
        int red = Convert.ToInt32(color.Substring(1, 2), 16);
        int green = Convert.ToInt32(color.Substring(3, 2), 16);
        int blue = Convert.ToInt32(color.Substring(5, 2), 16);
        // WCAG relative luminance, sufficient to choose the higher-contrast black or white.
        static double Linear(int channel)
        {
            double component = channel / 255d;
            return component <= .04045 ? component / 12.92 : Math.Pow((component + .055) / 1.055, 2.4);
        }
        double luminance = .2126 * Linear(red) + .7152 * Linear(green) + .0722 * Linear(blue);
        return luminance > .179 ? "#000000" : "#FFFFFF";
    }

    internal static string RendererVersion(Assembly assembly, string rendererRevision)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        string informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3)
            ?? "0.0.0";
        int metadataStart = informationalVersion.IndexOf('+');
        if (metadataStart >= 0) informationalVersion = informationalVersion[..metadataStart];
        return $"{informationalVersion}/{rendererRevision}";
    }
}
