using AwesomeAssertions;
using ImageMagick;
using Oire.Sic.Models;
using Xunit;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic.Tests;

public sealed class SizeFitTests: IDisposable {
    private const int SourceWidth = 240;
    private const int SourceHeight = 180;

    // A budget every format meets at full size. A noisy 16-bit PNG is larger than the same image
    // as a BMP, so no single measured encode makes a safe "generous" budget.
    private const long NoLimit = long.MaxValue;

    // AVIF is by far the slowest encoder, so every test but the progress one leaves it out.
    private static readonly string[] FastFormats = ["JPG", "PNG", "WEBP", "BMP", "TIFF"];
    private static readonly string[] LossyAndLossless = ["JPG", "PNG", "WEBP"];
    private static readonly string[] ExcludedAndPng = ["ICO", "GIF", "PNG"];
    private static readonly string[] ExcludedOnly = ["ICO", "GIF"];
    private static readonly string[] JpgOnly = ["JPG"];

    private readonly TestImages _images = new();

    public void Dispose() {
        _images.Dispose();
    }

    /// <summary>
    /// A synchronous <see cref="IProgress{T}"/>: <see cref="Progress{T}"/> posts its callbacks
    /// asynchronously, so a list it fills could still be changing when the assertions run.
    /// Optionally cancels a token on its first report.
    /// </summary>
    private sealed class RecordingProgress(CancellationTokenSource? cancelOnFirstReport = null): IProgress<string> {
        public List<string> Reports { get; } = [];

        public void Report(string value) {
            Reports.Add(value);
            if (Reports.Count == 1) {
                cancelOnFirstReport?.Cancel();
            }
        }
    }

    private ImageItem CreateSource(int width = SourceWidth, int height = SourceHeight) {
        return ImageConverter.LoadFromFile(_images.Create("source.png", width, height, noise: true));
    }

    /// <summary>
    /// The size of <paramref name="item"/> encoded in full as <paramref name="format"/>, the way the
    /// search measures it. Budgets come from this, because Magick's noise isn't seeded and fixed
    /// constants would not hold from run to run.
    /// </summary>
    private static long MeasureFullSize(ImageItem item, MagickFormat format, uint? quality = null) {
        using var image = new MagickImage(item.FilePath!);
        if (quality.HasValue) {
            image.Quality = quality.Value;
        }

        image.Format = format;
        using var stream = new MemoryStream();
        image.Write(stream);
        return stream.Length;
    }

    /// <summary>
    /// Checks each neighboring pair: more pixels first, then higher quality (lossless counting as
    /// the highest), then the smaller file.
    /// </summary>
    private static void AssertRanked(IReadOnlyList<SizeFitProposal> proposals) {
        for (var i = 1; i < proposals.Count; i++) {
            var previous = proposals[i - 1];
            var current = proposals[i];
            var previousPixels = (long)previous.Width * previous.Height;
            var currentPixels = (long)current.Width * current.Height;
            var previousQuality = previous.Quality ?? int.MaxValue;
            var currentQuality = current.Quality ?? int.MaxValue;

            var ordered = previousPixels > currentPixels
                || (previousPixels == currentPixels && previousQuality > currentQuality)
                || (previousPixels == currentPixels && previousQuality == currentQuality && previous.FileSize <= current.FileSize);

            ordered.Should().BeTrue("{0} must not rank above {1}", current, previous);
        }
    }

    [Fact]
    public void FindSizeFitProposals_KeepsEveryProposalUnderTheBudget() {
        var item = CreateSource();
        var budget = MeasureFullSize(item, MagickFormat.Jpeg, 95) * 6 / 10;

        var proposals = ImageConverter.FindSizeFitProposals(item, LossyAndLossless, budget, null);

        proposals.Should().NotBeEmpty();
        proposals.Should().OnlyContain(p => p.FileSize > 0 && p.FileSize <= budget);
    }

    [Fact]
    public void FindSizeFitProposals_RecommendsTheFirstProposalOnly() {
        var item = CreateSource();
        var budget = MeasureFullSize(item, MagickFormat.Jpeg, 95) * 6 / 10;

        var proposals = ImageConverter.FindSizeFitProposals(item, LossyAndLossless, budget, null);

        proposals.Should().HaveCountGreaterThan(1);
        proposals[0].Recommended.Should().BeTrue();
        proposals.Skip(1).Should().OnlyContain(p => !p.Recommended);
    }

    [Fact]
    public void FindSizeFitProposals_PutsResolutionBeforeQuality() {
        var item = CreateSource();
        var budget = MeasureFullSize(item, MagickFormat.Jpeg, 95) * 6 / 10;

        var proposals = ImageConverter.FindSizeFitProposals(item, LossyAndLossless, budget, null);

        // JPG at quality 95 doesn't fit at full size, so it is offered both ways: shrunk at 95, and
        // at full size with a lower quality. The full-size one must win.
        var jpg = proposals.Where(p => p.Format == "JPG").ToList();
        jpg.Should().HaveCount(2);
        jpg.Should().ContainSingle(p => !p.Resized && p.Quality < 95);
        jpg.Should().ContainSingle(p => p.Resized && p.Quality == 95);
        proposals.Should().Contain(p => p.Format == "PNG" && p.Resized && p.Quality == null);

        proposals[0].Resized.Should().BeFalse();
        proposals[0].Width.Should().Be(SourceWidth);
        proposals[0].Height.Should().Be(SourceHeight);
        AssertRanked(proposals);
    }

    [Fact]
    public void FindSizeFitProposals_RanksEqualResolutionsByQualityThenSize() {
        var item = CreateSource();
        const long budget = NoLimit;

        var proposals = ImageConverter.FindSizeFitProposals(item, FastFormats, budget, null);

        // Everything fits at full size, so only quality and size decide the order: the lossless
        // formats first, smallest file first, then the lossy ones at quality 95.
        proposals.Select(p => p.Format).Should().BeEquivalentTo(FastFormats);
        proposals.Should().OnlyContain(p => p.Width == SourceWidth && p.Height == SourceHeight && !p.Resized);
        proposals.Should().OnlyContain(p => p.Quality == null || p.Quality == 95);
        proposals.TakeWhile(p => p.Quality == null).Should().HaveCount(3);
        AssertRanked(proposals);
    }

    [Fact]
    public void FindSizeFitProposals_RespectsMaxWidth() {
        var item = CreateSource();
        const long budget = NoLimit;

        var proposals = ImageConverter.FindSizeFitProposals(item, FastFormats, budget, 100);

        proposals.Should().HaveCount(FastFormats.Length);
        proposals.Should().OnlyContain(p => p.Width == 100 && p.Height == 75 && p.Resized);
    }

    [Fact]
    public void FindSizeFitProposals_KeepsProportionsAndMaxWidthUnderATightBudget() {
        var item = CreateSource();
        var budget = MeasureFullSize(item, MagickFormat.Png) / 4;

        var proposals = ImageConverter.FindSizeFitProposals(item, FastFormats, budget, 160);

        proposals.Should().NotBeEmpty();
        foreach (var proposal in proposals) {
            proposal.Width.Should().BeLessThanOrEqualTo(160);
            proposal.FileSize.Should().BeLessThanOrEqualTo(budget);
            ((double)proposal.Height).Should().BeApproximately(proposal.Width * (double)SourceHeight / SourceWidth, 1.0);
            Math.Min(proposal.Width, proposal.Height).Should().BeGreaterThanOrEqualTo(16, "SIC! never shrinks below 16 px");
        }
    }

    [Fact]
    public void FindSizeFitProposals_ReturnsNothingForAnImpossibleBudget() {
        var item = CreateSource();

        var proposals = ImageConverter.FindSizeFitProposals(item, FastFormats, 1, null);

        proposals.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void FindSizeFitProposals_RejectsANonPositiveBudget(long maxBytes) {
        var item = CreateSource();

        var act = () => ImageConverter.FindSizeFitProposals(item, FastFormats, maxBytes, null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FindSizeFitProposals_NeverProposesIcoOrGif() {
        var item = CreateSource();
        const long budget = NoLimit;
        var progress = new RecordingProgress();

        var proposals = ImageConverter.FindSizeFitProposals(item, ExcludedAndPng, budget, null, progress);

        proposals.Should().ContainSingle().Which.Format.Should().Be("PNG");
        progress.Reports.Should().Equal("PNG");
    }

    [Fact]
    public void FindSizeFitProposals_ReturnsNothingWhenOnlyIcoAndGifAreGiven() {
        var item = CreateSource();
        var progress = new RecordingProgress();

        var proposals = ImageConverter.FindSizeFitProposals(item, ExcludedOnly, NoLimit, null, progress);

        proposals.Should().BeEmpty();
        progress.Reports.Should().BeEmpty();
    }

    [Fact]
    public void FindSizeFitProposals_FailsForAnItemWithNoData() {
        var act = () => ImageConverter.FindSizeFitProposals(new ImageItem { FileName = "empty.png" }, FastFormats, 1000, null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConvertToProposal_WritesExactlyTheAdvertisedSize() {
        var item = CreateSource();
        var budget = MeasureFullSize(item, MagickFormat.Jpeg, 95) * 6 / 10;
        var proposals = ImageConverter.FindSizeFitProposals(item, FastFormats, budget, null);
        proposals.Should().NotBeEmpty();

        foreach (var proposal in proposals) {
            var output = Path.Combine(_images.Folder, $"fit-{proposal.Format}-{proposal.Width}-{proposal.Quality}{ImageConverter.GetFileExtension(proposal.Format)}");

            ImageConverter.ConvertToProposal(item, proposal, output);

            new FileInfo(output).Length.Should().Be(proposal.FileSize, "the {0} file must match its proposal", proposal);
            var info = new MagickImageInfo(output);
            ((int)info.Width).Should().Be(proposal.Width);
            ((int)info.Height).Should().Be(proposal.Height);
            var decoded = new ImageItem { OriginalFormat = info.Format.ToString() };
            ImageConverter.IsSameFormat(decoded, proposal.Format).Should().BeTrue("Magick decoded the output as {0}", info.Format);
        }
    }

    [Fact]
    public void ConvertToProposal_ReproducesAProposalForAnItemHeldInMemory() {
        var item = ImageConverter.LoadFromBytes(TestImages.CreateBytes(SourceWidth, SourceHeight, MagickFormat.Png, noise: true), "clipboard.png");
        var budget = item.FileSize / 3;
        var proposals = ImageConverter.FindSizeFitProposals(item, JpgOnly, budget, null);
        proposals.Should().NotBeEmpty();
        var output = Path.Combine(_images.Folder, "clipboard.jpg");

        ImageConverter.ConvertToProposal(item, proposals[0], output);

        new FileInfo(output).Length.Should().Be(proposals[0].FileSize);
    }

    [Theory]
    [InlineData("HEIC")]
    [InlineData("PSD")]
    [InlineData("")]
    public void ConvertToProposal_RejectsAnUnsupportedFormat(string format) {
        var item = CreateSource();
        var output = Path.Combine(_images.Folder, "unsupported.out");
        var proposal = new SizeFitProposal {
            Format = format,
            Width = SourceWidth,
            Height = SourceHeight,
            FileSize = 1000,
            Resized = false,
        };

        var act = () => ImageConverter.ConvertToProposal(item, proposal, output);

        act.Should().Throw<ArgumentException>();
        File.Exists(output).Should().BeFalse();
    }

    [Fact]
    public void FindSizeFitProposals_ReportsEachFormatAsItsTurnStarts() {
        // The one test that includes AVIF, on a small image, because it is by far the slowest format.
        var item = CreateSource(64, 48);
        var progress = new RecordingProgress();

        var proposals = ImageConverter.FindSizeFitProposals(item, ImageConverter.GetSupportedFormats(), NoLimit, null, progress);

        progress.Reports.Should().Equal("JPG", "PNG", "WEBP", "BMP", "TIFF", "AVIF");
        proposals.Select(p => p.Format).Should().BeEquivalentTo(progress.Reports);
    }

    [Fact]
    public void FindSizeFitProposals_StopsWhenCanceledDuringTheSearch() {
        var item = CreateSource();
        using var cancellation = new CancellationTokenSource();
        var progress = new RecordingProgress(cancellation);

        var act = () => ImageConverter.FindSizeFitProposals(item, FastFormats, MeasureFullSize(item, MagickFormat.Png) / 4, null, progress, cancellation.Token);

        act.Should().Throw<OperationCanceledException>();
        progress.Reports.Should().Equal("JPG");
    }

    [Fact]
    public void FindSizeFitProposals_DoesNotStartWithACanceledToken() {
        var item = CreateSource();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var progress = new RecordingProgress();

        var act = () => ImageConverter.FindSizeFitProposals(item, FastFormats, 1000, null, progress, cancellation.Token);

        act.Should().Throw<OperationCanceledException>();
        progress.Reports.Should().BeEmpty();
    }
}
