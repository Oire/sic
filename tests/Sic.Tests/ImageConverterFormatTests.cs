using AwesomeAssertions;
using Oire.Sic.Models;
using Xunit;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic.Tests;

public class ImageConverterFormatTests {
    private static readonly string[] CanonicalOrder = ["JPG", "PNG", "WEBP", "ICO", "BMP", "TIFF", "GIF", "AVIF"];

    [Fact]
    public void GetSupportedFormats_ListsEveryFormatInCanonicalOrder() {
        ImageConverter.GetSupportedFormats().Should().Equal(CanonicalOrder);
    }

    [Fact]
    public void GetSupportedFormats_NeverOffersHeic() {
        ImageConverter.GetSupportedFormats().Should().NotContain("HEIC");
    }

    [Fact]
    public void GetEnabledFormats_KeepsOnlyTheEnabledFormats() {
        ImageConverter.GetEnabledFormats(["PNG", "JPG"]).Should().Equal("JPG", "PNG");
    }

    [Fact]
    public void GetEnabledFormats_KeepsCanonicalOrderWhateverTheInputOrder() {
        ImageConverter.GetEnabledFormats(["AVIF", "BMP", "WEBP", "JPG"])
            .Should().Equal("JPG", "WEBP", "BMP", "AVIF");
    }

    [Fact]
    public void GetEnabledFormats_IgnoresCase() {
        ImageConverter.GetEnabledFormats(["webp", "Gif"]).Should().Equal("WEBP", "GIF");
    }

    [Fact]
    public void GetEnabledFormats_DropsUnknownKeys() {
        ImageConverter.GetEnabledFormats(["PNG", "HEIC", "XYZ"]).Should().Equal("PNG");
    }

    [Fact]
    public void GetEnabledFormats_ReturnsEveryFormatForNull() {
        ImageConverter.GetEnabledFormats(null).Should().Equal(CanonicalOrder);
    }

    [Fact]
    public void GetEnabledFormats_ReturnsEveryFormatForAnEmptyList() {
        ImageConverter.GetEnabledFormats([]).Should().Equal(CanonicalOrder);
    }

    [Fact]
    public void GetEnabledFormats_NeverReturnsAnEmptyList() {
        ImageConverter.GetEnabledFormats(["HEIC", "XYZ"]).Should().Equal(CanonicalOrder);
    }

    [Fact]
    public void GetSizeFitFormats_RemovesIcoAndGif() {
        ImageConverter.GetSizeFitFormats(CanonicalOrder)
            .Should().Equal("JPG", "PNG", "WEBP", "BMP", "TIFF", "AVIF");
    }

    [Fact]
    public void GetSizeFitFormats_IgnoresCase() {
        ImageConverter.GetSizeFitFormats(["ico", "Gif", "png"]).Should().Equal("png");
    }

    [Fact]
    public void GetSizeFitFormats_IsEmptyWhenOnlyIcoAndGifAreEnabled() {
        ImageConverter.GetSizeFitFormats(["ICO", "GIF"]).Should().BeEmpty();
    }

    [Theory]
    [InlineData("JPG", ".jpg")]
    [InlineData("PNG", ".png")]
    [InlineData("WEBP", ".webp")]
    [InlineData("ICO", ".ico")]
    [InlineData("BMP", ".bmp")]
    [InlineData("TIFF", ".tiff")]
    [InlineData("GIF", ".gif")]
    [InlineData("AVIF", ".avif")]
    [InlineData("jpg", ".jpg")]
    [InlineData("Tiff", ".tiff")]
    public void GetFileExtension_MapsEveryFormatKey(string format, string expected) {
        ImageConverter.GetFileExtension(format).Should().Be(expected);
    }

    [Theory]
    [InlineData("JPEG", ".jpeg")]
    [InlineData("TIF", ".tif")]
    [InlineData("Heic", ".heic")]
    public void GetFileExtension_LowercasesAnyOtherName(string format, string expected) {
        ImageConverter.GetFileExtension(format).Should().Be(expected);
    }

    [Theory]
    [InlineData("Jpeg", "JPG")]
    [InlineData("Jpeg", "JPEG")]
    [InlineData("Pjpeg", "jpg")]
    [InlineData("Png", "PNG")]
    [InlineData("Png32", "PNG")]
    [InlineData("WebP", "WEBP")]
    [InlineData("Ico", "ICO")]
    [InlineData("Icon", "ICO")]
    [InlineData("Bmp3", "BMP")]
    [InlineData("Bmp", "BMP")]
    [InlineData("Tiff", "TIFF")]
    [InlineData("Tif", "TIFF")]
    [InlineData("Tiff", "TIF")]
    [InlineData("Tiff64", "TIFF")]
    [InlineData("Gif", "GIF")]
    [InlineData("Gif87", "GIF")]
    [InlineData("Avif", "AVIF")]
    [InlineData("Heic", "HEIF")]
    public void IsSameFormat_IsTrueForEverySpellingOfTheSameFormat(string originalFormat, string target) {
        var item = new ImageItem { OriginalFormat = originalFormat };

        ImageConverter.IsSameFormat(item, target).Should().BeTrue();
    }

    [Theory]
    [InlineData("Jpeg", "PNG")]
    [InlineData("Png", "JPG")]
    [InlineData("Tiff", "BMP")]
    [InlineData("Heic", "JPG")]
    [InlineData("Svg", "SVG")]
    [InlineData("", "")]
    [InlineData("Unknown", "Unknown")]
    public void IsSameFormat_IsFalseForDifferentOrUnknownFormats(string originalFormat, string target) {
        var item = new ImageItem { OriginalFormat = originalFormat };

        ImageConverter.IsSameFormat(item, target).Should().BeFalse();
    }

    [Theory]
    [InlineData("photo.jpg", "JPG")]
    [InlineData("photo.jpeg", "JPG")]
    [InlineData("photo.png", "PNG")]
    [InlineData("photo.webp", "WEBP")]
    [InlineData("photo.bmp", "BMP")]
    [InlineData("photo.tiff", "TIFF")]
    [InlineData("photo.tif", "TIFF")]
    [InlineData("photo.gif", "GIF")]
    [InlineData("photo.ico", "ICO")]
    public void IsSameFormat_RecognizesTheFormatMagickReportsForALoadedFile(string fileName, string target) {
        using var images = new TestImages();
        var item = ImageConverter.LoadFromFile(images.Create(fileName, 32, 24));

        ImageConverter.IsSameFormat(item, target).Should().BeTrue(
            "Magick reports {0} as {1}", fileName, item.OriginalFormat
        );
    }

    [Theory]
    [InlineData("Jpeg", "JPG")]
    [InlineData("Jpeg", "JPEG")]
    [InlineData("Tiff", "TIF")]
    [InlineData("Bmp3", "BMP")]
    public void ShouldSkipConversion_SkipsTheSameFormatWithoutAResize(string originalFormat, string target) {
        var item = new ImageItem { OriginalFormat = originalFormat };

        ImageConverter.ShouldSkipConversion(item, target, null, null).Should().BeTrue();
    }

    [Theory]
    [InlineData(100, null)]
    [InlineData(null, 100)]
    [InlineData(100, 100)]
    public void ShouldSkipConversion_ReencodesTheSameFormatWhenResizing(int? width, int? height) {
        var item = new ImageItem { OriginalFormat = "Jpeg" };

        ImageConverter.ShouldSkipConversion(item, "JPG", width, height).Should().BeFalse();
    }

    [Fact]
    public void ShouldSkipConversion_ConvertsADifferentFormat() {
        var item = new ImageItem { OriginalFormat = "Png" };

        ImageConverter.ShouldSkipConversion(item, "JPG", null, null).Should().BeFalse();
    }

    [Fact]
    public void ShouldSkipConversion_SkipsALoadedJpegFileConvertedToJpg() {
        using var images = new TestImages();
        var item = ImageConverter.LoadFromFile(images.Create("photo.jpg", 32, 24));

        ImageConverter.ShouldSkipConversion(item, "JPG", null, null).Should().BeTrue();
        ImageConverter.ShouldSkipConversion(item, "JPG", 16, null).Should().BeFalse();
    }
}
