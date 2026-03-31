using System.IO.Compression;

namespace StealthPane.ScreenCapture.Utilities;

/// <summary>
///     Writes raw RGBA pixel data as a PNG file.
///     No external dependencies — uses ZLibStream for IDAT compression.
/// </summary>
public static class PngWriter
{
    private static readonly uint[] CrcTable = GenerateCrcTable();

    public static void Write(string filePath, int width, int height, byte[] rgba)
    {
        using var fs = File.Create(filePath);

        // PNG signature
        fs.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR chunk
        var ihdr = new byte[13];
        WriteInt32BigEndian(ihdr, 0, width);
        WriteInt32BigEndian(ihdr, 4, height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 6; // color type: RGBA
        WriteChunk(fs, "IHDR"u8, ihdr);

        // IDAT chunk — deflated filtered scanlines
        using var idatStream = new MemoryStream();
        using (var deflate = new ZLibStream(idatStream, CompressionLevel.Fastest, true))
        {
            var stride = width * 4;
            for (var y = 0; y < height; y++)
            {
                deflate.WriteByte(0); // filter: None
                deflate.Write(rgba, y * stride, stride);
            }
        }

        WriteChunk(fs, "IDAT"u8, idatStream.ToArray());

        // IEND chunk
        WriteChunk(fs, "IEND"u8, []);
    }

    /// <summary>
    ///     Converts BGRA pixel data (as returned by GDI) to RGBA in-place.
    /// </summary>
    public static void ConvertBgraToRgba(byte[] pixels)
    {
        for (var i = 0; i < pixels.Length; i += 4)
        {
            (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
        }
    }

    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, byte[] data)
    {
        Span<byte> lengthBuf = stackalloc byte[4];
        WriteInt32BigEndian(lengthBuf, 0, data.Length);
        stream.Write(lengthBuf);
        stream.Write(type);
        stream.Write(data);

        var crc = Crc32(type, data);
        Span<byte> crcBuf = stackalloc byte[4];
        WriteInt32BigEndian(crcBuf, 0, (int)crc);
        stream.Write(crcBuf);
    }

    private static void WriteInt32BigEndian(Span<byte> buf, int offset, int value)
    {
        buf[offset] = (byte)(value >> 24);
        buf[offset + 1] = (byte)(value >> 16);
        buf[offset + 2] = (byte)(value >> 8);
        buf[offset + 3] = (byte)value;
    }

    private static uint Crc32(ReadOnlySpan<byte> type, byte[] data)
    {
        var crc = 0xFFFFFFFF;
        foreach (var b in type)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        foreach (var b in data)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFF;
    }

    private static uint[] GenerateCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var c = i;
            for (var j = 0; j < 8; j++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }

            table[i] = c;
        }

        return table;
    }
}
