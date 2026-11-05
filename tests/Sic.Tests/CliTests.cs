using AwesomeAssertions;
using ImageMagick;
using Oire.Sic.Utils;
using Oire.Sic.Utils.Constants;
using Xunit;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic.Tests;

/// <summary>
/// End-to-end tests of the headless CLI through <c>Program.RunCli</c>, with stdout and stderr
/// captured. They run in the static-state collection because <c>RunCli</c> loads
/// <see cref="Config"/>, sets the language and writes to <see cref="Console"/>. Every test starts
/// from a fresh default config in the test output's <c>userdata</c> folder, so <c>--format</c>
/// writes into <see cref="App.DefaultOutputFolder"/> there; each such file gets a unique name and
/// is deleted afterwards. The text of <c>System.CommandLine</c>'s own errors is never asserted,
/// because it follows the Windows display language.
/// </summary>
[Collection(StaticStateCollection.Name)]
public sealed class CliTests: StaticStateTestBase {
    private readonly TestImages _images = new();
    private readonly List<string> _defaultFolderOutputs = [];

    public CliTests() {
        TestDataFolder.Clear();
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            foreach (var path in _defaultFolderOutputs) {
                File.Delete(path);
            }

            _images.Dispose();
            TestDataFolder.Clear();
        }

        base.Dispose(disposing);
    }

    private sealed record CliResult(int ExitCode, string Output, string Error);

    private static CliResult Run(params string[] args) {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var output = new StringWriter();
        using var error = new StringWriter();
        Console.SetOut(output);
        Console.SetError(error);

        try {
            var exitCode = Program.RunCli(args);
            return new CliResult(exitCode, output.ToString(), error.ToString());
        } finally {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    /// <summary>A unique file stem, so files written to the shared default output folder never collide.</summary>
    private static string UniqueName(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N");

    /// <summary>The path <c>--format</c> writes to for <paramref name="baseName"/>, registered for cleanup.</summary>
    private string DefaultFolderOutput(string baseName, string extension) {
        var path = Path.Combine(App.DefaultOutputFolder, baseName + extension);
        _defaultFolderOutputs.Add(path);
        return path;
    }

    private string Output(string fileName) => Path.Combine(_images.Folder, "out", fileName);

    private static void ShouldFail(CliResult result) {
        result.ExitCode.Should().Be(ExitCode.Error);
        result.Error.Should().NotBeNullOrWhiteSpace();
        result.Output.Should().BeEmpty();
    }

    // Success

    [Fact]
    public void InputAndOutput_ConvertsToTheOutputExtension() {
        var input = _images.Create("photo.png", 40, 30);
        var output = Output("photo.jpg");

        var result = Run("-i", input, "-o", output);

        result.ExitCode.Should().Be(ExitCode.Success);
        result.Error.Should().BeEmpty();
        result.Output.Trim().Should().Be($"Converted: {output}");
        TestImages.ShouldBeImage(output, MagickFormat.Jpeg, 40, 30);
    }

    [Fact]
    public void LongOptionNames_Work() {
        var input = _images.Create("photo.bmp", 40, 30);
        var output = Output("photo.webp");

        var result = Run("--input", input, "--output", output);

        result.ExitCode.Should().Be(ExitCode.Success);
        TestImages.ShouldBeImage(output, MagickFormat.WebP, 40, 30);
    }

    [Fact]
    public void Output_CreatesAMissingFolder() {
        var input = _images.Create("photo.png", 40, 30);
        var output = Path.Combine(_images.Folder, "deep", "er", "photo.gif");

        var result = Run("-i", input, "-o", output);

        result.ExitCode.Should().Be(ExitCode.Success);
        TestImages.ShouldBeImage(output, MagickFormat.Gif, 40, 30);
    }

    [Theory]
    [InlineData("webp", ".webp", MagickFormat.WebP)]
    [InlineData(".png", ".png", MagickFormat.Png)]
    [InlineData("BMP", ".bmp", MagickFormat.Bmp)]
    [InlineData("jpeg", ".jpg", MagickFormat.Jpeg)]
    [InlineData("JPEG", ".jpg", MagickFormat.Jpeg)]
    [InlineData("tif", ".tiff", MagickFormat.Tiff)]
    [InlineData("TIF", ".tiff", MagickFormat.Tiff)]
    public void Format_WritesToTheDefaultOutputFolder(string format, string extension, MagickFormat expected) {
        var baseName = UniqueName("cli-format");
        var input = _images.Create(baseName + ".gif", 40, 30);
        var output = DefaultFolderOutput(baseName, extension);

        var result = Run("-i", input, "--format", format);

        result.ExitCode.Should().Be(ExitCode.Success);
        result.Error.Should().BeEmpty();
        result.Output.Trim().Should().Be($"Converted: {output}");
        TestImages.ShouldBeImage(output, expected, 40, 30);
    }

    [Fact]
    public void ShortFormatOption_Works() {
        var baseName = UniqueName("cli-short-format");
        var input = _images.Create(baseName + ".png", 40, 30);
        var output = DefaultFolderOutput(baseName, ".avif");

        var result = Run("-i", input, "-f", "avif");

        result.ExitCode.Should().Be(ExitCode.Success);
        TestImages.ShouldBeImage(output, MagickFormat.Avif, 40, 30);
    }

    [Fact]
    public void Output_WinsOverFormat() {
        var input = _images.Create("photo.png", 40, 30);
        var output = Output("photo.bmp");

        var result = Run("-i", input, "-o", output, "-f", "webp");

        result.ExitCode.Should().Be(ExitCode.Success);
        TestImages.ShouldBeImage(output, MagickFormat.Bmp, 40, 30);
    }

    [Theory]
    [InlineData("photo.jpeg", MagickFormat.Jpeg)]
    [InlineData("photo.JPEG", MagickFormat.Jpeg)]
    [InlineData("photo.tif", MagickFormat.Tiff)]
    [InlineData("photo.TIF", MagickFormat.Tiff)]
    [InlineData("photo.ICO", MagickFormat.Ico)]
    public void Output_AcceptsAlternativeExtensionSpellings(string fileName, MagickFormat expected) {
        var input = _images.Create("photo.png", 40, 30);
        var output = Output(fileName);

        var result = Run("-i", input, "-o", output);

        result.ExitCode.Should().Be(ExitCode.Success);
        result.Output.Trim().Should().Be($"Converted: {output}");
        File.Exists(output).Should().BeTrue();
        if (expected == MagickFormat.Ico) {
            var header = File.ReadAllBytes(output).AsSpan(0, 4).ToArray();
            header.Should().Equal(0, 0, 1, 0);
        } else {
            TestImages.ShouldBeImage(output, expected, 40, 30);
        }
    }

    [Fact]
    public void Url_WithOutput_DownloadsAndConverts() {
        using var server = new StubHttpServer();
        var url = server.Serve("/images/remote.png", TestImages.CreateBytes(40, 30, MagickFormat.Png), "image/png");
        var output = Output("remote.webp");

        var result = Run("-i", url, "-o", output);

        result.ExitCode.Should().Be(ExitCode.Success);
        result.Error.Should().BeEmpty();
        TestImages.ShouldBeImage(output, MagickFormat.WebP, 40, 30);
    }

    [Fact]
    public void Url_WithFormat_NamesTheOutputAfterTheLink() {
        var baseName = UniqueName("cli-url");
        using var server = new StubHttpServer();
        var url = server.Serve($"/images/{baseName}.png", TestImages.CreateBytes(40, 30, MagickFormat.Png), "image/png");
        var output = DefaultFolderOutput(baseName, ".jpg");

        var result = Run("-i", url, "--format", "jpg");

        result.ExitCode.Should().Be(ExitCode.Success);
        result.Output.Trim().Should().Be($"Converted: {output}");
        TestImages.ShouldBeImage(output, MagickFormat.Jpeg, 40, 30);
    }

    [Fact]
    public void Url_WithFormat_UnescapesTheNameInTheLink() {
        var baseName = UniqueName("cli url");
        using var server = new StubHttpServer();
        var url = server.Serve($"/images/{Uri.EscapeDataString(baseName)}.png", TestImages.CreateBytes(40, 30, MagickFormat.Png), "image/png");
        var output = DefaultFolderOutput(baseName, ".jpg");

        var result = Run("-i", url, "--format", "jpg");

        result.ExitCode.Should().Be(ExitCode.Success);
        result.Output.Trim().Should().Be($"Converted: {output}");
        TestImages.ShouldBeImage(output, MagickFormat.Jpeg, 40, 30);
    }

    [Theory]
    [InlineData("%3A")]
    [InlineData("%5C")]
    [InlineData("%3F")]
    public void Url_WithFormat_KeepsTheOutputInTheOutputFolder(string escapedCharacter) {
        // "C%3A..." would otherwise decode to the rooted "C:...", which Path.Combine lets replace
        // the output folder; a backslash would add a folder and "?" would make the write throw.
        var unique = UniqueName("cli");
        using var server = new StubHttpServer();
        var url = server.Serve($"/images/C{escapedCharacter}{unique}.png", TestImages.CreateBytes(40, 30, MagickFormat.Png), "image/png");
        var output = DefaultFolderOutput($"C_{unique}", ".jpg");

        var result = Run("-i", url, "--format", "jpg");

        result.ExitCode.Should().Be(ExitCode.Success);
        result.Output.Trim().Should().Be($"Converted: {output}");
        TestImages.ShouldBeImage(output, MagickFormat.Jpeg, 40, 30);
    }

    [Fact]
    public void Url_WithFormat_NamesALinkWithoutAFileNameLikeTheGuiDoes() {
        using var server = new StubHttpServer();
        var url = server.Serve("/", TestImages.CreateBytes(40, 30, MagickFormat.Png), "image/png");
        var output = DefaultFolderOutput("downloaded_image", ".jpg");

        var result = Run("-i", url, "--format", "jpg");

        result.ExitCode.Should().Be(ExitCode.Success);
        result.Output.Trim().Should().Be($"Converted: {output}");
        TestImages.ShouldBeImage(output, MagickFormat.Jpeg, 40, 30);
        File.Exists(Path.Combine(App.DefaultOutputFolder, ".jpg")).Should().BeFalse();
    }

    [Theory]
    [InlineData("100x", 100, 50)]
    [InlineData("100", 100, 50)]
    [InlineData("x25", 50, 25)]
    [InlineData("60x60", 60, 30)]
    [InlineData("60X60", 60, 30)]
    [InlineData("400x400", 400, 200)]
    public void Resize_KeepsProportions(string resize, int width, int height) {
        var input = _images.Create("wide.png", 200, 100);
        var output = Output("wide.jpg");

        var result = Run("-i", input, "-o", output, "--resize", resize);

        result.ExitCode.Should().Be(ExitCode.Success);
        TestImages.ShouldBeImage(output, MagickFormat.Jpeg, width, height);
    }

    [Fact]
    public void ShortResizeOption_Works() {
        var input = _images.Create("wide.png", 200, 100);
        var output = Output("wide.png");

        var result = Run("-i", input, "-o", output, "-r", "50x");

        result.ExitCode.Should().Be(ExitCode.Success);
        TestImages.ShouldBeImage(output, MagickFormat.Png, 50, 25);
    }

    [Theory]
    [InlineData("--crop")]
    [InlineData("-c")]
    public void Crop_GivesExactDimensions(string cropOption) {
        var input = _images.Create("wide.png", 200, 100);
        var output = Output("square.png");

        var result = Run("-i", input, "-o", output, "-r", "50x50", cropOption);

        result.ExitCode.Should().Be(ExitCode.Success);
        TestImages.ShouldBeImage(output, MagickFormat.Png, 50, 50);
    }

    // Skipping

    [Theory]
    [InlineData("photo.png", "copy.png")]
    [InlineData("photo.jpg", "copy.jpeg")]
    [InlineData("photo.tiff", "copy.tif")]
    public void SameFormatWithoutResize_IsSkipped(string inputName, string outputName) {
        var input = _images.Create(inputName, 40, 30);
        var output = Output(outputName);

        var result = Run("-i", input, "-o", output);

        result.ExitCode.Should().Be(ExitCode.Success);
        result.Error.Should().BeEmpty();
        result.Output.Should().StartWith($"Skipped: {input} is already in ");
        File.Exists(output).Should().BeFalse();
    }

    [Fact]
    public void SameFormatWithResize_IsConverted() {
        var input = _images.Create("photo.png", 40, 30);
        var output = Output("small.png");

        var result = Run("-i", input, "-o", output, "-r", "20x");

        result.ExitCode.Should().Be(ExitCode.Success);
        result.Output.Trim().Should().Be($"Converted: {output}");
        TestImages.ShouldBeImage(output, MagickFormat.Png, 20, 15);
    }

    // Failure

    [Fact]
    public void MissingInputOption_Fails() {
        var result = Run("-o", Output("photo.jpg"));

        result.ExitCode.Should().Be(ExitCode.Error);
        result.Error.Should().NotBeNullOrWhiteSpace();
        File.Exists(Output("photo.jpg")).Should().BeFalse();
    }

    [Fact]
    public void MissingInputFile_Fails() {
        var input = Path.Combine(_images.Folder, "missing.png");

        var result = Run("-i", input, "-o", Output("photo.jpg"));

        ShouldFail(result);
        result.Error.Trim().Should().Be($"File not found: {input}");
    }

    [Fact]
    public void OutputWithoutExtension_Fails() {
        var input = _images.Create("photo.png", 40, 30);

        var result = Run("-i", input, "-o", Output("photo"));

        ShouldFail(result);
        result.Error.Trim().Should().Be("Output path must have a file extension (e.g. .jpg, .png)");
        File.Exists(Output("photo")).Should().BeFalse();
    }

    [Fact]
    public void NeitherOutputNorFormat_Fails() {
        var input = _images.Create("photo.png", 40, 30);

        var result = Run("-i", input);

        ShouldFail(result);
        result.Error.Trim().Should().Be("Either --output or --format must be specified.");
    }

    [Fact]
    public void UnsupportedOutputExtension_Fails() {
        var input = _images.Create("photo.png", 40, 30);
        var output = Output("photo.xyz");

        var result = Run("-i", input, "-o", output);

        ShouldFail(result);
        var lines = result.Error.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().Equal(
            "Unsupported format: XYZ",
            "Supported formats: " + string.Join(", ", ImageConverter.GetSupportedFormats()));
        File.Exists(output).Should().BeFalse();
    }

    [Fact]
    public void UnsupportedFormatOption_Fails() {
        var baseName = UniqueName("cli-heic");
        var input = _images.Create(baseName + ".png", 40, 30);
        var output = DefaultFolderOutput(baseName, ".heic");

        var result = Run("-i", input, "--format", "heic");

        ShouldFail(result);
        result.Error.Should().StartWith("Unsupported format: HEIC");
        File.Exists(output).Should().BeFalse();
    }

    [Theory]
    [InlineData("axb", "Invalid width value: a")]
    [InlineData("0x10", "Invalid width value: 0")]
    [InlineData("-5", "Invalid width value: -5")]
    [InlineData("10xb", "Invalid height value: b")]
    [InlineData("10x0", "Invalid height value: 0")]
    [InlineData("10x20x30", "Invalid resize format: 10x20x30. Use WxH, Wx, xH, or W (e.g. 128x128, 128x, x128, 128)")]
    [InlineData("x", "Invalid resize format: x. At least one dimension is required.")]
    public void InvalidResize_Fails(string resize, string message) {
        var input = _images.Create("photo.png", 40, 30);
        var output = Output("photo.jpg");

        var result = Run("-i", input, "-o", output, "-r", resize);

        ShouldFail(result);
        result.Error.Trim().Should().Be(message);
        File.Exists(output).Should().BeFalse();
    }

    [Theory]
    [InlineData("50x")]
    [InlineData("x50")]
    [InlineData("50")]
    public void CropWithOneSize_Fails(string resize) {
        var input = _images.Create("photo.png", 40, 30);
        var output = Output("photo.jpg");

        var result = Run("-i", input, "-o", output, "-r", resize, "--crop");

        ShouldFail(result);
        result.Error.Trim().Should().Be("Crop mode requires both width and height (e.g. --resize 128x128 --crop)");
        File.Exists(output).Should().BeFalse();
    }

    [Fact]
    public void CropWithoutResize_Fails() {
        var input = _images.Create("photo.png", 40, 30);
        var output = Output("photo.jpg");

        var result = Run("-i", input, "-o", output, "--crop");

        ShouldFail(result);
        result.Error.Trim().Should().Be("Crop mode requires both width and height (e.g. --resize 128x128 --crop)");
        File.Exists(output).Should().BeFalse();
    }

    [Fact]
    public void CorruptInputFile_Fails() {
        var input = Path.Combine(_images.Folder, "corrupt.png");
        File.WriteAllText(input, "This is not a PNG file.");
        var output = Output("photo.jpg");

        var result = Run("-i", input, "-o", output);

        ShouldFail(result);
        result.Error.Should().StartWith("Conversion failed: ");
        File.Exists(output).Should().BeFalse();
    }

    [Fact]
    public void UrlGiving404_Fails() {
        using var server = new StubHttpServer();
        var output = Output("remote.jpg");

        var result = Run("-i", server.Url("/missing.png"), "-o", output);

        ShouldFail(result);
        result.Error.Should().StartWith("Conversion failed: ");
        File.Exists(output).Should().BeFalse();
    }

    [Fact]
    public void UrlGivingAPage_Fails() {
        using var server = new StubHttpServer();
        var url = server.Serve("/page.png", "<html><body>Not an image</body></html>"u8.ToArray(), "text/html");
        var output = Output("remote.jpg");

        var result = Run("-i", url, "-o", output);

        ShouldFail(result);
        result.Error.Should().StartWith("Conversion failed: ");
        File.Exists(output).Should().BeFalse();
    }

    // Configuration

    [Fact]
    public void MissingCustomOutputFolder_WarnsAndFallsBackToTheDefault() {
        var missingFolder = Path.Combine(_images.Folder, "gone");
        Config.Load(isGui: false);
        Config.General.OutputFolder = missingFolder;
        Config.Save(isGui: false);
        var baseName = UniqueName("cli-fallback");
        var input = _images.Create(baseName + ".png", 40, 30);
        var output = DefaultFolderOutput(baseName, ".jpg");

        var result = Run("-i", input, "-f", "jpg");

        result.ExitCode.Should().Be(ExitCode.Success);
        result.Error.Trim().Should().Be($"Warning! The output folder \"{missingFolder}\" no longer exists. Using default folder.");
        Directory.Exists(missingFolder).Should().BeFalse();
        TestImages.ShouldBeImage(output, MagickFormat.Jpeg, 40, 30);

        // The reset must reach the file, or the warning would come back on every run.
        Config.Load(isGui: false);
        Config.General.OutputFolder.Should().Be(App.DefaultOutputFolder);
        var secondOutput = DefaultFolderOutput(baseName, ".webp");
        var second = Run("-i", input, "-f", "webp");
        second.ExitCode.Should().Be(ExitCode.Success);
        second.Error.Should().BeEmpty();
        File.Exists(secondOutput).Should().BeTrue();
    }

    [Fact]
    public void FirstRun_CreatesTheConfigFileInTheTestDataFolder() {
        var input = _images.Create("photo.png", 40, 30);

        Run("-i", input, "-o", Output("photo.jpg"));

        File.Exists(Path.Combine(App.DataFolder, $"{App.Name}.{App.ConfigFileExtension}")).Should().BeTrue();
    }
}
