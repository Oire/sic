using AwesomeAssertions;
using ImageMagick;
using Oire.Sic.Models;
using Xunit;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic.Tests;

public sealed class PreviewTests: IDisposable {
    private readonly TestImages _images = new();

    public void Dispose() {
        _images.Dispose();
    }

    private ImageItem CreateSource(int width, int height) => _images.CreateItem("source.png", width, height);

    [Theory]
    [InlineData(1000, 500, 200, 200, 200, 100)]
    [InlineData(500, 1000, 200, 200, 100, 200)]
    [InlineData(300, 200, 480, 480, 480, 320)]
    [InlineData(400, 400, 640, 360, 360, 360)]
    public void GeneratePreview_ScalesTheImageToFitTheBox(int width, int height, int maxWidth, int maxHeight, int expectedWidth, int expectedHeight) {
        using var preview = ImageConverter.GeneratePreview(CreateSource(width, height), maxWidth, maxHeight);

        preview.Width.Should().Be(expectedWidth);
        preview.Height.Should().Be(expectedHeight);
    }

    [Fact]
    public void GeneratePreview_WorksForAnItemHeldInMemory() {
        var item = ImageConverter.LoadFromBytes(TestImages.CreateBytes(300, 200, MagickFormat.Jpeg), "clipboard.jpg");

        using var preview = ImageConverter.GeneratePreview(item, 480, 480);

        preview.Width.Should().Be(480);
        preview.Height.Should().Be(320);
    }

    [Fact]
    public void GeneratePreview_ShowsTheLargestFrameOfAMultiSizeIcoFile() {
        var item = ImageConverter.LoadFromFile(_images.CreateMultiFrameIco("icon.ico"));

        using var preview = ImageConverter.GeneratePreview(item, 480, 480);

        // Only the 64x32 frame gives a 2:1 preview; the 16 and 32 px frames are square.
        preview.Width.Should().Be(480);
        preview.Height.Should().Be(240);
    }

    [Fact]
    public void GeneratePreview_ShowsTheLargestFrameOfAMultiSizeIcoHeldInMemory() {
        // Built by hand, because LoadFromBytes can't read ICO bytes either (see ImageLoadingTests).
        var bytes = File.ReadAllBytes(_images.CreateMultiFrameIco("icon.ico"));
        var item = new ImageItem { ImageData = bytes, OriginalFormat = "Ico", FileName = "icon.ico" };

        using var preview = ImageConverter.GeneratePreview(item, 480, 480);

        preview.Width.Should().Be(480);
        preview.Height.Should().Be(240);
    }

    [Fact]
    public void GeneratePreview_ShowsTheCropShape() {
        var item = CreateSource(300, 200);

        using var cropped = ImageConverter.GeneratePreview(item, 480, 480, 100, 100, ResizeMode.Crop);
        using var uncropped = ImageConverter.GeneratePreview(item, 480, 480, null, null, ResizeMode.Crop);

        cropped.Width.Should().Be(480);
        cropped.Height.Should().Be(480);
        uncropped.Width.Should().Be(480);
        uncropped.Height.Should().Be(320);
    }

    [Fact]
    public void GeneratePreview_KeepsTheShapeWhenKeepingProportions() {
        using var preview = ImageConverter.GeneratePreview(CreateSource(300, 200), 480, 480, 150, null, ResizeMode.KeepProportions);

        preview.Width.Should().Be(480);
        preview.Height.Should().Be(320);
    }

    [Fact]
    public void GeneratePreview_FailsForAnItemWithNoData() {
        var act = () => ImageConverter.GeneratePreview(TestImages.EmptyItem(), 480, 480);

        act.Should().Throw<InvalidOperationException>();
    }
}
