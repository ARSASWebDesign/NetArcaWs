using System.Buffers.Binary;
using System.IO.Compression;

namespace NetArcaWs.Rendering.Examples;

internal static class SimpleLogoPng
{
    private const int DefaultSize = 240;
    private static readonly string[] Logo = [
        "#....", "#....", "#....", "#....", "#....", "#....", "#####",
        ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.",
        ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###.",
        ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."];

    public static byte[] Create(int width = DefaultSize, int height = DefaultSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        byte[] pixels = new byte[checked(width * height * 3)];
        Array.Fill(pixels, (byte)255);
        const int gap = 8, glyphWidth = 5, glyphHeight = 7;
        int scaleX = width == DefaultSize && height == DefaultSize ? 8 : Math.Max(1, (int)((width * .86 - gap * 3) / (glyphWidth * 4)));
        int scaleY = width == DefaultSize && height == DefaultSize ? 8 : Math.Max(1, (int)(height * .64 / glyphHeight));
        int wordWidth = glyphWidth * scaleX * 4 + gap * 3;
        int wordHeight = glyphHeight * scaleY;
        int left = (width - wordWidth) / 2, top = (height - wordHeight) / 2;
        for (int letter = 0; letter < 4; letter++)
        for (int row = 0; row < glyphHeight; row++)
        for (int col = 0; col < glyphWidth; col++)
        {
            if (Logo[letter * 7 + row][col] != '#') continue;
            for (int y = 0; y < scaleY; y++)
            for (int x = 0; x < scaleX; x++)
            {
                int px = left + letter * (glyphWidth * scaleX + gap) + col * scaleX + x;
                int py = top + row * scaleY + y;
                int offset = (py * width + px) * 3;
                pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = 0;
            }
        }

        using var output = new MemoryStream();
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr[4..], height);
        ihdr[8] = 8;
        ihdr[9] = 2;
        WriteChunk(output, "IHDR", ihdr);
        using var raw = new MemoryStream();
        for (int y = 0; y < height; y++)
        {
            raw.WriteByte(0);
            raw.Write(pixels, y * width * 3, width * 3);
        }
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(raw.ToArray());
        WriteChunk(output, "IDAT", compressed.ToArray());
        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    private static void WriteChunk(Stream output, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> header = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(header, data.Length);
        output.Write(header);
        byte[] name = System.Text.Encoding.ASCII.GetBytes(type);
        output.Write(name);
        output.Write(data);
        uint crc = 0xffffffff;
        foreach (byte value in name.Concat(data.ToArray()))
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320 : crc >> 1;
        }
        Span<byte> trailer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(trailer, ~crc);
        output.Write(trailer);
    }
}
