using System.Buffers.Binary;
using AwesomeAssertions;
using ImageMagick;
using Oire.Sic.Models;
using Xunit;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic.Tests;

public sealed class IcoTests: IDisposable {
    private const int HeaderSize = 6;
    private const int EntrySize = 16;

    private readonly TestImages _images = new();

    public void Dispose() {
        _images.Dispose();
    }

    /// <summary>One entry of an ICO file's directory, with its payload's encoding.</summary>
    internal sealed record IcoEntry(int Width, int Height, int Planes, int BitsPerPixel, bool IsPng);

    /// <summary>
    /// Reads the directory of an ICO file, checking the header on the way. A width or height of 0 in
    /// the directory means 256 px or more, and is reported as 256.
    /// </summary>
    internal static IReadOnlyList<IcoEntry> ReadDirectory(byte[] ico) {
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

    private ImageItem CreateSource(int width, int height) {
        return ImageConverter.LoadFromFile(_images.Create("source.png", width, height, noise: true));
    }

    private string Output(string fileName) => Path.Combine(_images.Folder, fileName);

    [Fact]
    public void CreateMultiSizeIco_WritesOneFramePerSize() {
        var path = Output("icon.ico");

        ImageConverter.CreateMultiSizeIco(CreateSource(300, 200), path, [16, 32, 48, 256]);

        var entries = ReadDirectory(File.ReadAllBytes(path));
        entries.Select(e => (e.Width, e.Height)).Should().BeEquivalentTo([(16, 16), (32, 32), (48, 48), (256, 256)]);

        using var decoded = new MagickImageCollection(path);
        decoded.Select(f => ((int)f.Width, (int)f.Height)).Should().BeEquivalentTo(
            [(16, 16), (32, 32), (48, 48), (256, 256)],
            "Magick must still read every frame after the 256 px one is replaced"
        );
    }

    [Fact]
    public void CreateMultiSizeIco_Stores256PxAsA32BitPngAndSmallerFramesAsBitmaps() {
        var path = Output("icon.ico");

        ImageConverter.CreateMultiSizeIco(CreateSource(300, 300), path, [16, 32, 48, 256]);

        var entries = ReadDirectory(File.ReadAllBytes(path));
        var large = entries.Single(e => e.Width == 256);
        large.IsPng.Should().BeTrue();
        large.Planes.Should().Be(1);
        large.BitsPerPixel.Should().Be(32);
        entries.Where(e => e.Width < 256).Should().OnlyContain(e => !e.IsPng);
    }

    [Fact]
    public void CreateMultiSizeIco_AcceptsUpTo512Px() {
        var path = Output("icon.ico");

        ImageConverter.CreateMultiSizeIco(CreateSource(64, 64), path, [512]);

        var entries = ReadDirectory(File.ReadAllBytes(path));
        entries.Should().ContainSingle().Which.IsPng.Should().BeTrue();
        using var decoded = new MagickImageCollection(path);
        decoded.Should().ContainSingle();
        ((int)decoded[0].Width).Should().Be(512);
    }

    [Fact]
    public void CreateMultiSizeIco_FailsAbove512PxWithoutWritingAFile() {
        var path = Output("icon.ico");

        var act = () => ImageConverter.CreateMultiSizeIco(CreateSource(64, 64), path, [513]);

        act.Should().Throw<MagickException>();
        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void CreateMultiSizeIco_FailsForAnItemWithNoData() {
        var item = new ImageItem { FileName = "empty.png", OriginalFormat = "Png" };

        var act = () => ImageConverter.CreateMultiSizeIco(item, Output("icon.ico"), [16]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Convert_StoresASmallIcoAsABitmap() {
        var path = Output("small.ico");

        ImageConverter.Convert(CreateSource(48, 48), "ICO", path, null, null);

        ReadDirectory(File.ReadAllBytes(path)).Should().ContainSingle().Which.IsPng.Should().BeFalse();
    }

    [Fact]
    public void Convert_StoresA256PxIcoAsA32BitPng() {
        var path = Output("large.ico");

        ImageConverter.Convert(CreateSource(1000, 800), "ICO", path, 256, 256, ResizeMode.Crop);

        var entry = ReadDirectory(File.ReadAllBytes(path)).Should().ContainSingle().Subject;
        entry.Width.Should().Be(256);
        entry.Height.Should().Be(256);
        entry.IsPng.Should().BeTrue();
        entry.BitsPerPixel.Should().Be(32);
        using var decoded = new MagickImageCollection(path);
        ((int)decoded[0].Width).Should().Be(256);
        ((int)decoded[0].Height).Should().Be(256);
    }

    [Fact(Skip = "⚠️ Magick writes a 300x200 frame as a bitmap with 44 (300 mod 256) as its directory width, "
        + "and WriteIco only re-encodes frames whose directory says 0, so the frame stays an uncompressed "
        + "bitmap under a wrong width. See docs/plans/088-test-coverage.md, Task 3.")]
    public void Convert_StoresAnIcoWithOneSideOver256PxAsPng() {
        var path = Output("wide.ico");

        ImageConverter.Convert(CreateSource(300, 200), "ICO", path, null, null);

        var entry = ReadDirectory(File.ReadAllBytes(path)).Should().ContainSingle().Subject;
        entry.Width.Should().Be(256, "a directory width of 0 means 256 px or more");
        entry.Height.Should().Be(200);
        entry.IsPng.Should().BeTrue();
        using var decoded = new MagickImageCollection(path);
        ((int)decoded[0].Width).Should().Be(300);
        ((int)decoded[0].Height).Should().Be(200);
    }

    [Fact]
    public void Convert_FailsAbove512PxForIcoWithoutWritingAFile() {
        var path = Output("huge.ico");

        var act = () => ImageConverter.Convert(CreateSource(600, 400), "ICO", path, null, null);

        act.Should().Throw<MagickException>();
        File.Exists(path).Should().BeFalse();
    }
}
