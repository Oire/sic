using AwesomeAssertions;
using ImageMagick;
using Oire.Sic.Models;
using Xunit;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic.Tests;

public sealed class IcoTests: IDisposable {
    private readonly TestImages _images = new();

    public void Dispose() {
        _images.Dispose();
    }

    private ImageItem CreateSource(int width, int height) => _images.CreateItem("source.png", width, height, noise: true);

    private string Output(string fileName) => Path.Combine(_images.Folder, fileName);

    [Fact]
    public void CreateMultiSizeIco_WritesOneFramePerSize() {
        var path = Output("icon.ico");

        ImageConverter.CreateMultiSizeIco(CreateSource(300, 200), path, [16, 32, 48, 256]);

        var entries = IcoFile.ReadDirectory(File.ReadAllBytes(path));
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

        var entries = IcoFile.ReadDirectory(File.ReadAllBytes(path));
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

        var entries = IcoFile.ReadDirectory(File.ReadAllBytes(path));
        entries.Should().ContainSingle().Which.IsPng.Should().BeTrue();
        using var decoded = new MagickImageCollection(path);
        decoded.Should().ContainSingle();
        ((int)decoded[0].Width).Should().Be(512);
    }

    [Theory]
    [InlineData(new uint[] { 513 })]
    [InlineData(new uint[] { 16, 513 })]
    [InlineData(new uint[] { 16, 32, 600, 48 })]
    public void CreateMultiSizeIco_FailsWhenAnyFrameIsAbove512PxWithoutWritingAFile(uint[] sizes) {
        var path = Output("icon.ico");

        var act = () => ImageConverter.CreateMultiSizeIco(CreateSource(64, 64), path, sizes);

        act.Should().Throw<ArgumentException>().WithMessage("*at most 512x512 px*");
        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void CreateMultiSizeIco_FailsForAnItemWithNoData() {
        var act = () => ImageConverter.CreateMultiSizeIco(TestImages.EmptyItem(), Output("icon.ico"), [16]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Convert_StoresASmallIcoAsABitmap() {
        var path = Output("small.ico");

        ImageConverter.Convert(CreateSource(48, 48), "ICO", path, null, null);

        IcoFile.ReadDirectory(File.ReadAllBytes(path)).Should().ContainSingle().Which.IsPng.Should().BeFalse();
    }

    [Fact]
    public void Convert_StoresA256PxIcoAsA32BitPng() {
        var path = Output("large.ico");

        ImageConverter.Convert(CreateSource(1000, 800), "ICO", path, 256, 256, ResizeMode.Crop);

        var entry = IcoFile.ReadDirectory(File.ReadAllBytes(path)).Should().ContainSingle().Subject;
        entry.Width.Should().Be(256);
        entry.Height.Should().Be(256);
        entry.IsPng.Should().BeTrue();
        entry.BitsPerPixel.Should().Be(32);
        using var decoded = new MagickImageCollection(path);
        ((int)decoded[0].Width).Should().Be(256);
        ((int)decoded[0].Height).Should().Be(256);
    }

    [Fact]
    public void Convert_StoresAnIcoWithOneSideOver256PxAsPng() {
        // Magick writes this frame as a bitmap with a directory width of 44 (300 mod 256), so
        // WriteIco has to go by the frame's own size, not the directory's.
        var path = Output("wide.ico");

        ImageConverter.Convert(CreateSource(300, 200), "ICO", path, null, null);

        var entry = IcoFile.ReadDirectory(File.ReadAllBytes(path)).Should().ContainSingle().Subject;
        entry.Width.Should().Be(256, "a directory width of 0 means 256 px or more");
        entry.Height.Should().Be(200);
        entry.IsPng.Should().BeTrue();
        entry.BitsPerPixel.Should().Be(32);
        using var decoded = new MagickImageCollection(path);
        ((int)decoded[0].Width).Should().Be(300);
        ((int)decoded[0].Height).Should().Be(200);
    }

    [Fact]
    public void Convert_FailsAbove512PxForIcoWithoutWritingAFile() {
        var path = Output("huge.ico");

        var act = () => ImageConverter.Convert(CreateSource(600, 400), "ICO", path, null, null);

        act.Should().Throw<ArgumentException>().WithMessage("*600x400 px*");
        File.Exists(path).Should().BeFalse();
    }
}
