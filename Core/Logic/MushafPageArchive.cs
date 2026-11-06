using System.IO.Compression;
using System.Text;

namespace AlMithaq.Core.Logic;

/// <summary>Lossless groups retain the exact UTF-8 bytes of the original page SVGs.</summary>
public static class MushafPageArchive
{
    public static int GroupStart(int page) => page is >= 1 and <= 604 ? ((page - 1) / 10) * 10 + 1 : throw new ArgumentOutOfRangeException(nameof(page));
    public static string Read(Stream compressed, int page)
    {
        var expected = GroupStart(page);
        using var decoded = new BrotliStream(compressed, CompressionMode.Decompress, leaveOpen: true);
        using var reader = new BinaryReader(decoded, Encoding.UTF8, leaveOpen: true);
        if (!reader.ReadBytes(8).AsSpan().SequenceEqual("MTHQPG01"u8)) throw new InvalidDataException("Invalid page archive");
        var start = reader.ReadInt32(); var count = reader.ReadInt32();
        if (start != expected || count != Math.Min(10, 605 - start)) throw new InvalidDataException("Page group mismatch");
        var skip = new byte[8192];
        for (var current = start; current < start + count; current++)
        {
            var length = reader.ReadInt32();
            if (length is <= 0 or > 16 * 1024 * 1024) throw new InvalidDataException("Invalid page length");
            if (current != page)
            {
                var remaining = length;
                while (remaining > 0)
                {
                    var read = reader.Read(skip, 0, Math.Min(skip.Length, remaining));
                    if (read == 0) throw new EndOfStreamException("Truncated page archive");
                    remaining -= read;
                }
                continue;
            }
            var bytes = reader.ReadBytes(length);
            if (bytes.Length != length) throw new EndOfStreamException("Truncated page archive");
            if (current == page) return Encoding.UTF8.GetString(bytes);
        }
        throw new InvalidDataException("Page missing");
    }
}
