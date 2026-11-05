using System.Buffers.Binary;
using AwesomeAssertions;

namespace Oire.Sic.Tests;

/// <summary>One entry of an ICO file's directory, with its payload's encoding.</summary>
internal sealed record IcoEntry(int Width, int Height, int Planes, int BitsPerPixel, bool IsPng);

/// <summary>Reads the directory of an ICO file, for tests that check what SIC! wrote.</summary>
internal static class IcoFile {
    private const int HeaderSize = 6;
    private const int EntrySize = 16;

    /// <summary>
    /// Reads the directory of an ICO file, checking the header on the way. A width or height of 0 in
    /// the directory means 256 px or more, and is reported as 256.
    /// </summary>
    public static IReadOnlyList<IcoEntry> ReadDirectory(byte[] ico) {
        ReadOnlySpan<byte> pngSignature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

        ico.Length.Should().BeGreaterThan(HeaderSize);
        BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(0)).Should().Be(0, "the ICO header starts with a reserved zero");
        BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(2)).Should().Be(1, "type 1 is an icon");

        var count = BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(4));
        var entries = new List<IcoEntry>(count);

        for (var i = 0; i < count; i++) {
            var entry = ico.AsSpan(HeaderSize + (i * EntrySize), EntrySize);
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]);
            var offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry[12..]);
            (offset + size).Should().BeLessThanOrEqualTo(ico.Length, "frame {0} must lie inside the file", i);

            entries.Add(new IcoEntry(
                entry[0] == 0 ? 256 : entry[0],
                entry[1] == 0 ? 256 : entry[1],
                BinaryPrimitives.ReadUInt16LittleEndian(entry[4..]),
                BinaryPrimitives.ReadUInt16LittleEndian(entry[6..]),
                ico.AsSpan(offset, size).StartsWith(pngSignature)
            ));
        }

        return entries;
    }
}
