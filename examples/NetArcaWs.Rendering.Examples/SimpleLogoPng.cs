using System.Buffers.Binary;
using System.IO.Compression;

namespace NetArcaWs.Rendering.Examples;

internal static class SimpleLogoPng
{
    private const int Size = 240;
    private static readonly string[] Logo = [
        "#....", "#....", "#....", "#....", "#....", "#....", "#####",
        ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.",
        ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###.",
        ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."];

    public static byte[] Create()
    {
        byte[] pixels = new byte[Size * Size * 3];
        Array.Fill(pixels, (byte)255);
        const int scale = 8, gap = 8, glyphWidth = 5, glyphHeight = 7;
        int width = glyphWidth * scale * 4 + gap * 3;
        int height = glyphHeight * scale;
        int left = (Size - width) / 2, top = (Size - height) / 2;
        for (int letter = 0; letter < 4; letter++)
        for (int row = 0; row < glyphHeight; row++)
        for (int col = 0; col < glyphWidth; col++)
        {
            if (Logo[letter * 7 + row][col] != '#') continue;
            for (int y = 0; y < scale; y++)
            for (int x = 0; x < scale; x++)
            {
                int px = left + letter * (glyphWidth * scale + gap) + col * scale + x;
                int py = top + row * scale + y;
                int offset = (py * Size + px) * 3;
                pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = 0;
            }
        }

        using var output = new MemoryStream();
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, Size);
        BinaryPrimitives.WriteInt32BigEndian(ihdr[4..], Size);
        ihdr[8] = 8;
        ihdr[9] = 2;
        WriteChunk(output, "IHDR", ihdr);
        using var raw = new MemoryStream();
        for (int y = 0; y < Size; y++)
        {
            raw.WriteByte(0);
            raw.Write(pixels, y * Size * 3, Size * 3);
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
