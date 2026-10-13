using System.Buffers.Binary;
using System.IO.Compression;

namespace NetArcaWs.Documents;

internal static class PngValidator
{
    private static ReadOnlySpan<byte> Signature => [137, 80, 78, 71, 13, 10, 26, 10];

    internal static bool IsValid(ReadOnlySpan<byte> png)
    {
        try { Validate(png); return true; }
        catch (Exception e) when (e is ArgumentException or InvalidDataException or IOException or OverflowException) { return false; }
    }

    private static void Validate(ReadOnlySpan<byte> png)
    {
        if (png.Length < 45 || png.Length > 1024 * 1024 || !png[..8].SequenceEqual(Signature)) throw new ArgumentException("Invalid PNG.");
        var offset = 8;
        var seenHeader = false;
        var seenPalette = false;
        var seenData = false;
        var dataEnded = false;
        var seenEnd = false;
        uint width = 0, height = 0;
        uint paletteEntries = 0;
        byte bitDepth = 0, colorType = 0, interlace = 0;
        using var compressed = new MemoryStream();

        while (offset < png.Length)
        {
            if (png.Length - offset < 12) throw new ArgumentException("Truncated PNG chunk.");
            var length = BinaryPrimitives.ReadUInt32BigEndian(png.Slice(offset, 4));
            if (length > png.Length - offset - 12) throw new ArgumentException("Truncated PNG chunk data.");
            var type = png.Slice(offset + 4, 4);
            var content = png.Slice(offset + 8, checked((int)length));
            var expectedCrc = BinaryPrimitives.ReadUInt32BigEndian(png.Slice(offset + 8 + (int)length, 4));
            if (Crc32(png.Slice(offset + 4, checked((int)length + 4))) != expectedCrc) throw new ArgumentException("Invalid PNG chunk checksum.");
            var name = System.Text.Encoding.ASCII.GetString(type);

            switch (name)
            {
                case "IHDR":
                    if (seenHeader || offset != 8 || length != 13) throw new ArgumentException("Invalid IHDR.");
                    seenHeader = true;
                    width = BinaryPrimitives.ReadUInt32BigEndian(content[..4]);
                    height = BinaryPrimitives.ReadUInt32BigEndian(content.Slice(4, 4));
                    bitDepth = content[8]; colorType = content[9];
                    if (width is 0 or > 2048 || height is 0 or > 2048 || !ValidDepth(colorType, bitDepth) || content[10] != 0 || content[11] != 0 || content[12] > 1)
                        throw new ArgumentException("Unsupported PNG header.");
                    interlace = content[12];
                    break;
                case "PLTE":
                    if (!seenHeader || seenData || seenPalette || length == 0 || length % 3 != 0 || length > 768 || colorType is 0 or 4)
                        throw new ArgumentException("Invalid PNG palette.");
                    seenPalette = true;
                    paletteEntries = length / 3;
                    if (colorType == 3 && paletteEntries > (1u << bitDepth)) throw new ArgumentException("PNG palette exceeds indexed bit depth.");
                    break;
                case "IDAT":
                    if (!seenHeader || dataEnded || colorType == 3 && !seenPalette) throw new ArgumentException("Invalid PNG image data order.");
                    seenData = true;
                    compressed.Write(content);
                    break;
                case "IEND":
                    if (!seenHeader || !seenData || seenEnd || length != 0) throw new ArgumentException("Invalid IEND.");
                    seenEnd = true;
                    offset += checked((int)length + 12);
                    if (offset != png.Length) throw new ArgumentException("Data follows IEND.");
                    continue;
                default:
                    if (!seenHeader || seenEnd) throw new ArgumentException("Invalid ancillary chunk order.");
                    if (seenData) dataEnded = true;
                    if ((type[0] & 0x20) == 0) throw new ArgumentException("Unknown critical PNG chunk.");
                    if (name is "tRNS" && (seenData || colorType is 4 or 6 || colorType == 3 && !seenPalette ||
                        colorType == 0 && length != 2 || colorType == 2 && length != 6 || colorType == 3 && (length == 0 || length > paletteEntries)))
                        throw new ArgumentException("Invalid tRNS chunk.");
                    if (name == "pHYs" && length != 9 || name == "gAMA" && length != 4 || name == "sRGB" && (length != 1 || content[0] > 3) || name == "tEXt" && (length == 0 || !content.Contains((byte)0)))
                        throw new ArgumentException("Invalid ancillary PNG chunk.");
                    break;
            }
            offset += checked((int)length + 12);
        }
        if (!seenEnd) throw new ArgumentException("PNG has no IEND chunk.");
        ValidatePixels(compressed.ToArray(), width, height, colorType, bitDepth, interlace);
    }

    private static void ValidatePixels(byte[] compressed, uint width, uint height, byte colorType, byte depth, byte interlace)
    {
        var channels = colorType switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => throw new ArgumentException("Invalid color type.") };
        var bytesPerPixel = Math.Max(1, (channels * depth + 7) / 8);
        var passes = interlace == 0
            ? new[] { (0, 0, 1, 1) }
            : new[] { (0, 0, 8, 8), (4, 0, 8, 8), (0, 4, 4, 8), (2, 0, 4, 4), (0, 2, 2, 4), (1, 0, 2, 2), (0, 1, 1, 2) };
        long expectedSize = 0;
        foreach (var (sx, sy, dx, dy) in passes)
        {
            var passWidth = width <= sx ? 0 : (width - sx + dx - 1) / dx;
            var passHeight = height <= sy ? 0 : (height - sy + dy - 1) / dy;
            if (passWidth > 0 && passHeight > 0) expectedSize = checked(expectedSize + passHeight * (1L + (passWidth * channels * depth + 7) / 8));
        }
        if (expectedSize > 32L * 1024 * 1024) throw new ArgumentException("PNG decoded data exceeds validation limit.");
        using var input = new MemoryStream(compressed, writable: false);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        using var decoded = new MemoryStream(checked((int)expectedSize));
        var buffer = new byte[8192];
        int count;
        while ((count = zlib.Read(buffer)) != 0)
        {
            if (decoded.Length + count > expectedSize) throw new InvalidDataException("PNG expands beyond its dimensions.");
            decoded.Write(buffer, 0, count);
        }
        if (decoded.Length != expectedSize || input.Position != input.Length) throw new InvalidDataException("PNG pixel data is truncated or has trailing data.");
        var pixels = decoded.GetBuffer().AsSpan(0, checked((int)expectedSize));
        var cursor = 0;
        foreach (var (sx, sy, dx, dy) in passes)
        {
            var passWidth = width <= sx ? 0 : (width - sx + dx - 1) / dx;
            var passHeight = height <= sy ? 0 : (height - sy + dy - 1) / dy;
            if (passWidth == 0 || passHeight == 0) continue;
            var rowLength = checked((int)((passWidth * channels * depth + 7) / 8));
            var previous = new byte[rowLength];
            for (var row = 0; row < passHeight; row++)
            {
                var filter = pixels[cursor++];
                if (filter > 4) throw new InvalidDataException("Invalid PNG scanline filter.");
                var current = pixels.Slice(cursor, rowLength);
                cursor += rowLength;
                for (var i = 0; i < rowLength; i++)
                {
                    var left = i >= bytesPerPixel ? current[i - bytesPerPixel] : 0;
                    var up = previous[i];
                    var upperLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : 0;
                    current[i] = filter switch
                    {
                        0 => current[i],
                        1 => unchecked((byte)(current[i] + left)),
                        2 => unchecked((byte)(current[i] + up)),
                        3 => unchecked((byte)(current[i] + ((left + up) >> 1))),
                        4 => unchecked((byte)(current[i] + Paeth(left, up, upperLeft))),
                        _ => current[i]
                    };
                }
                current.CopyTo(previous);
            }
        }
    }

    private static bool ValidDepth(byte color, byte depth) => color switch
    {
        0 => depth is 1 or 2 or 4 or 8 or 16,
        2 => depth is 8 or 16,
        3 => depth is 1 or 2 or 4 or 8,
        4 or 6 => depth is 8 or 16,
        _ => false
    };

    private static int Paeth(int a, int b, int c)
    {
        var p = a + b - c; var pa = Math.Abs(p - a); var pb = Math.Abs(p - b); var pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        uint crc = 0xffffffff;
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ (0xedb88320u & (uint)-(int)(crc & 1));
        }
        return ~crc;
    }
}
