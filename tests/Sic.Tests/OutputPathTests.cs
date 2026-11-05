using AwesomeAssertions;
using Oire.Sic.Models;
using Oire.Sic.Utils.Constants;
using Xunit;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic.Tests;

public sealed class OutputPathTests: IDisposable {
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Sic.Tests", Guid.NewGuid().ToString("N"));

    public OutputPathTests() {
        Directory.CreateDirectory(_root);
    }

    public void Dispose() {
        Directory.Delete(_root, recursive: true);
    }

    private string Output => Path.Combine(_root, "Output");

    private string Source => Path.Combine(_root, "Source");

    private ImageItem FileItem(string relativePath) {
        var path = Path.Combine(Source, relativePath);
        return new ImageItem { FilePath = path, FileName = Path.GetFileName(path), OriginalFormat = "Png" };
    }

    private static ImageItem DataItem(string fileName) {
        return new ImageItem { ImageData = [1, 2, 3], FileName = fileName, OriginalFormat = "Png" };
    }

    [Fact]
    public void GenerateOutputPath_UsesTheOutputFolderAndTheTargetExtension() {
        var path = ImageConverter.GenerateOutputPath(FileItem("photo.png"), "JPG", Output);

        path.Should().Be(Path.Combine(Output, "photo.jpg"));
    }

    [Fact]
    public void GenerateOutputPath_KeepsDotsInTheBaseName() {
        var path = ImageConverter.GenerateOutputPath(FileItem("holiday.2026.png"), "WEBP", Output);

        path.Should().Be(Path.Combine(Output, "holiday.2026.webp"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateOutputPath_FallsBackToTheDefaultOutputFolder(string outputFolder) {
        var path = ImageConverter.GenerateOutputPath(FileItem("photo.png"), "TIFF", outputFolder);

        path.Should().Be(Path.Combine(App.DefaultOutputFolder, "photo.tiff"));
    }

    [Fact]
    public void GenerateOutputPath_DefaultOutputFolderIsInsideTheTestOutput() {
        Path.GetFullPath(App.DefaultOutputFolder).Should().StartWith(Path.GetFullPath(AppContext.BaseDirectory));
    }

    [Fact]
    public void GenerateOutputPath_WritesNextToTheOriginalWithSaveToSourceFolder() {
        var path = ImageConverter.GenerateOutputPath(FileItem("photo.png"), "JPG", Output, saveToSourceFolder: true);

        path.Should().Be(Path.Combine(Source, "photo.jpg"));
    }

    [Fact]
    public void GenerateOutputPath_SaveToSourceFolderIgnoresTheBasePath() {
        var item = FileItem(Path.Combine("Holidays", "photo.png"));
        item.BasePath = Source;

        var path = ImageConverter.GenerateOutputPath(item, "JPG", Output, saveToSourceFolder: true);

        path.Should().Be(Path.Combine(Source, "Holidays", "photo.jpg"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenerateOutputPath_PutsClipboardAndUrlItemsInTheOutputFolder(bool saveToSourceFolder) {
        var path = ImageConverter.GenerateOutputPath(DataItem("downloaded_image"), "PNG", Output, saveToSourceFolder);

        path.Should().Be(Path.Combine(Output, "downloaded_image.png"));
    }

    [Fact]
    public void GenerateOutputPath_PutsADownloadedLinkInTheOutputFolder() {
        var path = ImageConverter.GenerateOutputPath(DataItem("cat.webp"), "JPG", Output, saveToSourceFolder: true);

        path.Should().Be(Path.Combine(Output, "cat.jpg"));
    }

    [Fact]
    public void GenerateOutputPath_KeepsTheFolderStructureBelowTheBasePath() {
        var item = FileItem(Path.Combine("2026", "Holidays", "photo.png"));
        item.BasePath = Source;

        var path = ImageConverter.GenerateOutputPath(item, "GIF", Output);

        path.Should().Be(Path.Combine(Output, "2026", "Holidays", "photo.gif"));
    }

    [Fact]
    public void GenerateOutputPath_PutsAFileInTheBaseFolderItselfAtTheTopOfTheOutputFolder() {
        var item = FileItem("photo.png");
        item.BasePath = Source;

        var path = ImageConverter.GenerateOutputPath(item, "BMP", Output);

        path.Should().Be(Path.Combine(Output, "photo.bmp"));
    }

    [Fact]
    public void GetConflictRenamePath_AddsUnderscoreOne() {
        var existing = Path.Combine(_root, "photo.jpg");
        File.WriteAllBytes(existing, [0]);

        ImageConverter.GetConflictRenamePath(existing).Should().Be(Path.Combine(_root, "photo_1.jpg"));
    }

    [Fact]
    public void GetConflictRenamePath_AddsUnderscoreTwoWhenUnderscoreOneExists() {
        var existing = Path.Combine(_root, "photo.jpg");
        File.WriteAllBytes(existing, [0]);
        File.WriteAllBytes(Path.Combine(_root, "photo_1.jpg"), [0]);

        ImageConverter.GetConflictRenamePath(existing).Should().Be(Path.Combine(_root, "photo_2.jpg"));
    }

    [Fact]
    public void GetConflictRenamePath_SkipsEveryTakenNumber() {
        var existing = Path.Combine(_root, "photo.jpg");
        File.WriteAllBytes(existing, [0]);
        File.WriteAllBytes(Path.Combine(_root, "photo_1.jpg"), [0]);
        File.WriteAllBytes(Path.Combine(_root, "photo_2.jpg"), [0]);
        File.WriteAllBytes(Path.Combine(_root, "photo_4.jpg"), [0]);

        ImageConverter.GetConflictRenamePath(existing).Should().Be(Path.Combine(_root, "photo_3.jpg"));
    }
}
