using AwesomeAssertions;
using Oire.Sic.Models;
using Xunit;

namespace Oire.Sic.Tests;

/// <summary>
/// <see cref="ImageItem"/>'s display text goes through <c>_()</c> and formats numbers in the
/// current culture, so these tests run in the static-state collection, in <c>en-US</c>.
/// </summary>
[Collection(StaticStateCollection.Name)]
public sealed class ImageItemTests: StaticStateTestBase {
    private const long KB = 1024;
    private const long MB = 1024 * 1024;

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1, "1 B")]
    [InlineData(KB - 1, "1023 B")]
    [InlineData(KB, "1.0 KB")]
    [InlineData(KB + (KB / 2), "1.5 KB")]
    [InlineData(MB - 1, "1024.0 KB")]
    [InlineData(MB, "1.0 MB")]
    [InlineData((5 * MB) / 2, "2.5 MB")]
    [InlineData(1024 * MB, "1024.0 MB")]
    public void GetSizeDisplay_UsesBytesThenKilobytesThenMegabytes(long fileSize, string expected) {
        var item = new ImageItem { FileSize = fileSize };

        item.GetSizeDisplay().Should().Be(expected);
    }

    [Fact]
    public void GetDimensionsDisplay_IsWidthByHeight() {
        var item = new ImageItem { Width = 1920, Height = 1080 };

        item.GetDimensionsDisplay().Should().Be("1920x1080");
    }

    [Fact]
    public void GetDisplayDescription_CombinesNameFormatDimensionsAndSize() {
        var item = new ImageItem {
            FileName = "photo.png",
            OriginalFormat = "Png",
            Width = 640,
            Height = 480,
            FileSize = 2048,
        };

        item.GetDisplayDescription().Should().Be("photo.png (PNG, 640x480, 2.0 KB)");
    }
}
