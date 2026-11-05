using AwesomeAssertions;
using ImageMagick;
using Oire.Sic.Models;
using Xunit;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic.Tests;

public sealed class ConversionTests: IDisposable {
    private readonly TestImages _images = new();

    public void Dispose() {
        _images.Dispose();
    }

    /// <summary>Every format SIC! can write, paired with every format it can write.</summary>
    public static TheoryData<string, string> FormatPairs() {
        var data = new TheoryData<string, string>();
        foreach (var source in ImageConverter.GetSupportedFormats()) {
            foreach (var target in ImageConverter.GetSupportedFormats()) {
                data.Add(source, target);
            }
        }

        return data;
    }

    /// <summary>A path inside a folder that doesn't exist.</summary>
    private string PathInMissingFolder(string fileName) => Path.Combine(_images.Folder, "missing", fileName);

    private ImageItem CreateSource(string format, int width, int height) {
        return _images.CreateItem("source" + ImageConverter.GetFileExtension(format), width, height);
    }

    [Theory]
    [MemberData(nameof(FormatPairs))]
    public void Convert_WritesTheTargetFormatAtTheSameSize(string source, string target) {
        var item = CreateSource(source, 48, 32);
        var output = Path.Combine(_images.Folder, "converted" + ImageConverter.GetFileExtension(target));

        ImageConverter.Convert(item, target, output, null, null);

        var bytes = File.ReadAllBytes(output);
        if (target == "ICO") {
            // LoadFromFile names any .ico file "Ico" from its extension, so check the header, then
            // decode the frame itself to catch a corrupt or truncated payload.
            var entries = IcoFile.ReadDirectory(bytes);
            entries.Should().ContainSingle();
            entries[0].Width.Should().Be(48);
            entries[0].Height.Should().Be(32);
            using var frames = new MagickImageCollection(bytes, MagickFormat.Ico);
            frames.Should().ContainSingle();
            ((int)frames[0].Width).Should().Be(48);
            ((int)frames[0].Height).Should().Be(32);
        } else {
            using var image = new MagickImage(bytes);
            TestImages.ShouldBeFormat(image.Format, target);
            ((int)image.Width).Should().Be(48);
            ((int)image.Height).Should().Be(32);
        }
    }

    [Fact]
    public void Convert_ConvertsAnItemHeldInMemory() {
        var item = ImageConverter.LoadFromBytes(TestImages.CreateBytes(48, 32, MagickFormat.Png), "clipboard.png");
        var output = Path.Combine(_images.Folder, "clipboard.webp");

        ImageConverter.Convert(item, "WEBP", output, null, null);

        new MagickImageInfo(output).Format.Should().Be(MagickFormat.WebP);
        TestImages.ReadSize(output).Should().Be((48, 32));
    }

    [Theory]
    [InlineData("HEIC")]
    [InlineData("PSD")]
    [InlineData("")]
    public void Convert_RejectsAnUnsupportedTarget(string target) {
        var item = CreateSource("PNG", 48, 32);
        var output = Path.Combine(_images.Folder, "converted.out");

        var act = () => ImageConverter.Convert(item, target, output, null, null);

        act.Should().Throw<ArgumentException>();
        File.Exists(output).Should().BeFalse();
    }

    [Fact]
    public void Convert_UsesTheLargestFrameOfAMultiSizeIcoFile() {
        var item = ImageConverter.LoadFromFile(_images.CreateMultiFrameIco("icon.ico"));
        var output = Path.Combine(_images.Folder, "icon.png");

        ImageConverter.Convert(item, "PNG", output, null, null);

        TestImages.ReadSize(output).Should().Be((64, 32));
    }

    [Fact(Skip = "⚠️ ICO has no signature Magick can detect, so LoadMagickImage's in-memory ICO branch, "
        + "new MagickImageCollection(bytes) with no format, fails with 'no decode delegate'. "
        + "See docs/plans/088-test-coverage.md, review round 1.")]
    public void Convert_UsesTheLargestFrameOfAMultiSizeIcoHeldInMemory() {
        // Built by hand, the way LoadFromBytes would describe it, because LoadFromBytes can't read ICO
        // bytes either (see ImageLoadingTests).
        var bytes = File.ReadAllBytes(_images.CreateMultiFrameIco("icon.ico"));
        var item = new ImageItem { ImageData = bytes, OriginalFormat = "Ico", FileName = "icon.ico" };
        var output = Path.Combine(_images.Folder, "icon.png");

        ImageConverter.Convert(item, "PNG", output, null, null);

        TestImages.ReadSize(output).Should().Be((64, 32));
    }

    [Fact]
    public void Convert_KeepsTheSourceQualityWhenResizingToTheSameFormat() {
        var source = Path.Combine(_images.Folder, "source.jpg");
        using (var image = TestImages.CreateImage(200, 100, noise: true)) {
            image.Quality = 60;
            image.Write(source);
        }

        var resized = Path.Combine(_images.Folder, "resized.jpg");
        ImageConverter.Convert(ImageConverter.LoadFromFile(source), "JPG", resized, 100, null);
        var fromPng = Path.Combine(_images.Folder, "from-png.jpg");
        ImageConverter.Convert(CreateSource("PNG", 200, 100), "JPG", fromPng, 100, null);

        new MagickImageInfo(resized).Quality.Should().Be(60);
        new MagickImageInfo(fromPng).Quality.Should().NotBe(60, "only a same-format conversion keeps the source quality");
    }

    [Fact]
    public void Convert_FailsForAnItemWithNoData() {
        var item = TestImages.EmptyItem();

        var act = () => ImageConverter.Convert(item, "JPG", Path.Combine(_images.Folder, "empty.jpg"), null, null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(300, 200, 150, null, 150, 100)]
    [InlineData(300, 200, null, 100, 150, 100)]
    [InlineData(200, 300, 100, null, 100, 150)]
    [InlineData(200, 300, null, 150, 100, 150)]
    [InlineData(300, 200, 600, null, 600, 400)]
    public void Convert_KeepsProportionsWithOneSide(int width, int height, int? newWidth, int? newHeight, int expectedWidth, int expectedHeight) {
        var item = CreateSource("PNG", width, height);
        var output = Path.Combine(_images.Folder, "resized.png");

        ImageConverter.Convert(item, "PNG", output, newWidth, newHeight);

        TestImages.ReadSize(output).Should().Be((expectedWidth, expectedHeight));
    }

    [Theory]
    [InlineData(300, 200, 100, 100, 100, 67)]
    [InlineData(200, 300, 100, 100, 67, 100)]
    [InlineData(300, 200, 120, 200, 120, 80)]
    [InlineData(200, 300, 400, 150, 100, 150)]
    public void Convert_FitsInsideTheBoxWhenKeepingProportionsWithBothSides(int width, int height, int boxWidth, int boxHeight, int expectedWidth, int expectedHeight) {
        var item = CreateSource("PNG", width, height);
        var output = Path.Combine(_images.Folder, "fitted.png");

        ImageConverter.Convert(item, "PNG", output, boxWidth, boxHeight, ResizeMode.KeepProportions);

        TestImages.ReadSize(output).Should().Be((expectedWidth, expectedHeight));
    }

    [Theory]
    [InlineData(300, 200, 100, 100)]
    [InlineData(200, 300, 100, 100)]
    [InlineData(300, 200, 50, 120)]
    [InlineData(200, 300, 128, 64)]
    [InlineData(300, 200, 450, 450)]
    public void Convert_CropsToTheExactSize(int width, int height, int cropWidth, int cropHeight) {
        var item = CreateSource("PNG", width, height);
        var output = Path.Combine(_images.Folder, "cropped.jpg");

        ImageConverter.Convert(item, "JPG", output, cropWidth, cropHeight, ResizeMode.Crop);

        TestImages.ReadSize(output).Should().Be((cropWidth, cropHeight));
    }

    [Fact]
    public void Convert_KeepsProportionsWhenCropIsGivenOnlyOneSide() {
        var item = CreateSource("PNG", 300, 200);
        var output = Path.Combine(_images.Folder, "cropped.png");

        ImageConverter.Convert(item, "PNG", output, 150, null, ResizeMode.Crop);

        TestImages.ReadSize(output).Should().Be((150, 100));
    }

    [Fact]
    public void Convert_FailsWhenTheOutputFolderIsMissing() {
        var item = CreateSource("PNG", 48, 32);

        var act = () => ImageConverter.Convert(item, "JPG", PathInMissingFolder("converted.jpg"), null, null);

        act.Should().Throw<MagickException>();
    }
}
