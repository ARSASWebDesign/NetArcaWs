using System.Reflection;
using PdfSharp.Fonts;

namespace NetArcaWs.Rendering.Pdf;

internal sealed class EmbeddedFontResolver : IFontResolver
{
    internal const string Family = "Noto Sans";
    private static readonly byte[] Regular = Read("NotoSans-Regular.ttf");
    private static readonly byte[] Bold = Read("NotoSans-Bold.ttf");
    internal static EmbeddedFontResolver Instance { get; } = new();
    private EmbeddedFontResolver() { }

    public string DefaultFontName => Family;

    public byte[] GetFont(string faceName) => faceName switch
    {
        "NotoSans#Regular" => (byte[])Regular.Clone(),
        "NotoSans#Bold" => (byte[])Bold.Clone(),
        _ => throw new InvalidOperationException($"Fuente PDF no soportada: {faceName}.")
    };

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (!familyName.Equals(Family, StringComparison.OrdinalIgnoreCase) &&
            !familyName.Equals("Courier New", StringComparison.OrdinalIgnoreCase)) return null;
        if (isItalic) throw new InvalidOperationException("Noto Sans cursiva no está disponible en el paquete PDF.");
        return new FontResolverInfo(isBold ? "NotoSans#Bold" : "NotoSans#Regular");
    }

    private static byte[] Read(string suffix)
    {
        Assembly assembly = typeof(EmbeddedFontResolver).Assembly;
        string name = assembly.GetManifestResourceNames().Single(resource => resource.EndsWith(suffix, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
