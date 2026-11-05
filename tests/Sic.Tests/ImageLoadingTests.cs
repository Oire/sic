using System.Text;
using AwesomeAssertions;
using ImageMagick;
using Oire.Sic.Services;
using Xunit;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic.Tests;

public sealed class ImageLoadingTests: IDisposable {
    private const long OverDownloadLimit = 101L * 1024 * 1024;

    private readonly TestImages _images = new();

    public void Dispose() {
        _images.Dispose();
    }

    [Theory]
    [InlineData("photo.jpg", "JPG")]
    [InlineData("photo.png", "PNG")]
    [InlineData("photo.webp", "WEBP")]
    [InlineData("photo.bmp", "BMP")]
    [InlineData("photo.tiff", "TIFF")]
    [InlineData("photo.gif", "GIF")]
    [InlineData("photo.avif", "AVIF")]
    [InlineData("photo.ico", "ICO")]
    public void LoadFromFile_ReadsDimensionsSizeAndFormat(string fileName, string format) {
        var path = _images.Create(fileName, 40, 30);

        var item = ImageConverter.LoadFromFile(path);

        item.FilePath.Should().Be(path);
        item.ImageData.Should().BeNull();
        item.FileName.Should().Be(fileName);
        item.Width.Should().Be(40);
        item.Height.Should().Be(30);
        item.FileSize.Should().Be(new FileInfo(path).Length);
        ImageConverter.IsSameFormat(item, format).Should().BeTrue("Magick reported {0}", item.OriginalFormat);
    }

    [Fact]
    public void LoadFromFile_UsesTheLargestFrameOfAMultiSizeIco() {
        var source = ImageConverter.LoadFromFile(_images.Create("source.png", 64, 64));
        var path = Path.Combine(_images.Folder, "icon.ico");
        ImageConverter.CreateMultiSizeIco(source, path, [16, 48, 32]);

        var item = ImageConverter.LoadFromFile(path);

        item.OriginalFormat.Should().Be("Ico");
        item.Width.Should().Be(48);
        item.Height.Should().Be(48);
    }

    [Fact]
    public void LoadFromFile_FailsOnACorruptFile() {
        var path = Path.Combine(_images.Folder, "corrupt.png");
        File.WriteAllText(path, "This is not a PNG file.");

        var act = () => ImageConverter.LoadFromFile(path);

        act.Should().Throw<MagickException>();
    }

    [Fact]
    public void LoadFromFile_FailsOnAMissingFile() {
        var path = Path.Combine(_images.Folder, "missing.png");

        var act = () => ImageConverter.LoadFromFile(path);

        act.Should().Throw<MagickBlobErrorException>();
    }

    [Fact]
    public void LoadFromStream_ReadsDimensionsSizeFormatAndKeepsTheData() {
        var data = TestImages.CreateBytes(40, 30, MagickFormat.Png);
        using var stream = new MemoryStream(data);

        var item = ImageConverter.LoadFromStream(stream, "pasted.png");

        item.FilePath.Should().BeNull();
        item.ImageData.Should().Equal(data);
        item.FileName.Should().Be("pasted.png");
        item.Width.Should().Be(40);
        item.Height.Should().Be(30);
        item.FileSize.Should().Be(data.Length);
        ImageConverter.IsSameFormat(item, "PNG").Should().BeTrue("Magick reported {0}", item.OriginalFormat);
    }

    [Fact]
    public void LoadFromStream_FailsOnCorruptData() {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("This is not an image."));

        var act = () => ImageConverter.LoadFromStream(stream, "pasted.png");

        act.Should().Throw<MagickException>();
    }

    [Fact]
    public void LoadFromBytes_ReadsDimensionsSizeFormatAndKeepsTheData() {
        var data = TestImages.CreateBytes(40, 30, MagickFormat.Jpeg);

        var item = ImageConverter.LoadFromBytes(data, "clipboard.jpg");

        item.FilePath.Should().BeNull();
        item.ImageData.Should().BeSameAs(data);
        item.FileName.Should().Be("clipboard.jpg");
        item.Width.Should().Be(40);
        item.Height.Should().Be(30);
        item.FileSize.Should().Be(data.Length);
        ImageConverter.IsSameFormat(item, "JPG").Should().BeTrue("Magick reported {0}", item.OriginalFormat);
    }

    [Fact]
    public void LoadFromBytes_FailsOnCorruptData() {
        var act = () => ImageConverter.LoadFromBytes(Encoding.ASCII.GetBytes("This is not an image."), "clipboard.png");

        act.Should().Throw<MagickException>();
    }

    [Fact]
    public async Task LoadFromUrl_DownloadsAnImage() {
        var data = TestImages.CreateBytes(40, 30, MagickFormat.Png);
        using var server = new StubHttpServer();
        var url = server.Serve("/images/photo.png", data, "image/png");

        var item = await ImageConverter.LoadFromUrl(url);

        item.FilePath.Should().BeNull();
        item.ImageData.Should().Equal(data);
        item.FileName.Should().Be("photo.png");
        item.Width.Should().Be(40);
        item.Height.Should().Be(30);
        item.FileSize.Should().Be(data.Length);
        ImageConverter.IsSameFormat(item, "PNG").Should().BeTrue("Magick reported {0}", item.OriginalFormat);
    }

    [Fact]
    public async Task LoadFromUrl_NamesAnImageWithoutAFileNameInTheLink() {
        using var server = new StubHttpServer();
        var url = server.Serve("/", TestImages.CreateBytes(40, 30, MagickFormat.Png), "image/png");

        var item = await ImageConverter.LoadFromUrl(url);

        item.FileName.Should().Be("downloaded_image");
    }

    [Fact]
    public async Task LoadFromUrl_ReportsAnHtmlPageAsUnsupported() {
        using var server = new StubHttpServer();
        var html = "<!DOCTYPE html><html><head><title>Photos</title></head><body><p>Hello</p></body></html>";
        var url = server.Serve("/gallery.html", Encoding.UTF8.GetBytes(html), "text/html");

        var act = () => ImageConverter.LoadFromUrl(url);

        await act.Should().ThrowAsync<UnsupportedImageException>();
    }

    [Fact]
    public async Task LoadFromUrl_FailsOnNotFound() {
        using var server = new StubHttpServer();

        var act = () => ImageConverter.LoadFromUrl(server.Url("/missing.png"));

        (await act.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LoadFromUrl_StopsWhenCanceled() {
        using var server = new StubHttpServer();
        var url = server.ServeStalled("/slow.png", 1024 * 1024, [0x89, (byte)'P', (byte)'N', (byte)'G']);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        var act = () => ImageConverter.LoadFromUrl(url, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task LoadFromUrl_RejectsAnFtpLink() {
        var act = () => ImageConverter.LoadFromUrl("ftp://example.com/photo.png");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task LoadFromUrl_RejectsADeclaredSizeAbove100MB() {
        using var server = new StubHttpServer();
        var url = server.ServeStalled("/huge.png", OverDownloadLimit, []);

        var act = () => ImageConverter.LoadFromUrl(url);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
