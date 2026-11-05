using System.Text;
using AwesomeAssertions;
using ImageMagick;
using Oire.Sic.Services;
using Xunit;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic.Tests;

public sealed class ImageLoadingTests: IDisposable {
    private const long OverDownloadLimit = 101L * 1024 * 1024;

    // How long a download test may run before it counts as hung. The stub stalls forever on purpose,
    // and HttpClient.Timeout doesn't cover reading the body, so without this bound a regression would
    // show up as a CI job timeout instead of a failing test.
    private static readonly TimeSpan HangLimit = TimeSpan.FromSeconds(30);

    /// <summary>A synchronous <see cref="IProgress{T}"/> that records every report.</summary>
    private sealed class RecordingProgress<T>: IProgress<T> {
        public List<T> Reports { get; } = [];

        public void Report(T value) => Reports.Add(value);
    }

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
    public void LoadFromFile_FailsOnACorruptIco() {
        var path = Path.Combine(_images.Folder, "corrupt.ico");
        File.WriteAllText(path, "This is not an ICO file.");

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

    [Fact(Skip = "⚠️ ICO has no signature Magick can detect, so new MagickImage(bytes) fails with "
        + "'no decode delegate' and LoadFromBytes, LoadFromStream and LoadFromUrl all reject a valid ICO. "
        + "See docs/plans/088-test-coverage.md, review round 1.")]
    public void LoadFromBytes_ReadsAMultiSizeIcoByItsLargestFrame() {
        var bytes = File.ReadAllBytes(_images.CreateMultiFrameIco("icon.ico"));

        var item = ImageConverter.LoadFromBytes(bytes, "icon.ico");

        item.OriginalFormat.Should().Be("Ico");
        item.Width.Should().Be(64);
        item.Height.Should().Be(32);
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

    [Theory]
    [InlineData("/C%3Aphoto.png", "C_photo.png")]
    [InlineData("/images/a%5Cb.png", "a_b.png")]
    [InlineData("/images/a%3Fb.png", "a_b.png")]
    public async Task LoadFromUrl_CleansCharactersAFileNameCannotHold(string path, string expected) {
        using var server = new StubHttpServer();
        var url = server.Serve(path, TestImages.CreateBytes(40, 30, MagickFormat.Png), "image/png");

        var item = await ImageConverter.LoadFromUrl(url);

        item.FileName.Should().Be(expected);
    }

    [Fact]
    public async Task LoadFromUrl_ReportsProgressUpToTheWholeFile() {
        var data = TestImages.CreateBytes(200, 150, MagickFormat.Bmp);
        using var server = new StubHttpServer();
        var url = server.Serve("/photo.bmp", data, "image/bmp");
        var progress = new RecordingProgress<(long BytesRead, long? TotalBytes)>();

        await ImageConverter.LoadFromUrl(url, progress: progress).WaitAsync(HangLimit);

        progress.Reports.Should().NotBeEmpty();
        progress.Reports.Select(r => r.BytesRead).Should().BeInAscendingOrder();
        progress.Reports[^1].Should().Be(((long)data.Length, (long?)data.Length));
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

        // A token that no longer reaches the read would hang on the stalled stub; the outer bound
        // turns that into a TimeoutException, which fails the assertion.
        var act = () => ImageConverter.LoadFromUrl(url, cancellation.Token).WaitAsync(HangLimit);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task LoadFromUrl_RejectsAnFtpLink() {
        var act = () => ImageConverter.LoadFromUrl("ftp://example.com/photo.png");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("not a link")]
    [InlineData("images/photo.png")]
    public async Task LoadFromUrl_RejectsSomethingThatIsNotAnAbsoluteLink(string url) {
        var act = () => ImageConverter.LoadFromUrl(url);

        await act.Should().ThrowAsync<UriFormatException>();
    }

    [Fact]
    public async Task LoadFromUrl_RejectsADeclaredSizeAbove100MB() {
        using var server = new StubHttpServer();
        var url = server.ServeStalled("/huge.png", OverDownloadLimit, []);
        // Without the length check the read would hang on the stalled stub. The token turns that
        // into an OperationCanceledException, which fails the assertion instead of the CI job.
        using var cancellation = new CancellationTokenSource(HangLimit);

        var act = () => ImageConverter.LoadFromUrl(url, cancellation.Token);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("File is too large*");
    }

    [Fact]
    public async Task LoadFromUrl_StopsReadingAbove100MBWhenNoSizeIsDeclared() {
        using var server = new StubHttpServer();
        var url = server.ServeUnsized("/endless.png", OverDownloadLimit);
        using var cancellation = new CancellationTokenSource(HangLimit);

        var act = () => ImageConverter.LoadFromUrl(url, cancellation.Token);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Download exceeded*");
    }
}
