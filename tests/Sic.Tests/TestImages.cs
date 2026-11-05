using AwesomeAssertions;
using ImageMagick;
using Oire.Sic.Models;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic.Tests;

/// <summary>
/// Generates small test images with Magick in a temporary folder of its own, and deletes the
/// folder on dispose. Images are gradients, optionally with Magick's noise on top, rather than
/// solid colors, so their encoded size behaves like a real picture's. No binary fixtures are
/// committed. Also the home of the checks several test classes make on the images they write.
/// </summary>
internal sealed class TestImages: IDisposable {
    public TestImages() {
        Folder = Path.Combine(Path.GetTempPath(), "Sic.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Folder);
    }

    /// <summary>The temporary folder holding this instance's images.</summary>
    public string Folder { get; }

    /// <summary>
    /// Writes a <paramref name="width"/> by <paramref name="height"/> image to
    /// <paramref name="fileName"/> inside <see cref="Folder"/>, in the format its extension names.
    /// Subfolders in <paramref name="fileName"/> are created. Returns the full path.
    /// </summary>
    public string Create(string fileName, int width, int height, bool noise = false) {
        var path = Path.Combine(Folder, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var image = CreateImage(width, height, noise);
        image.Write(path);

        return path;
    }

    /// <summary>Writes an image as <see cref="Create"/> does and loads it with <c>LoadFromFile</c>.</summary>
    public ImageItem CreateItem(string fileName, int width, int height, bool noise = false) {
        return ImageConverter.LoadFromFile(Create(fileName, width, height, noise));
    }

    /// <summary>
    /// Writes a three-frame ICO to <paramref name="fileName"/>: 16x16, then the largest frame,
    /// 64x32, then 32x32. The largest frame sits in the middle and has its own shape, so a
    /// conversion or preview that used any other frame comes out at a different size and shape.
    /// Returns the full path.
    /// </summary>
    public string CreateMultiFrameIco(string fileName) {
        var path = Path.Combine(Folder, fileName);
        using var frames = new MagickImageCollection {
            CreateImage(16, 16),
            CreateImage(64, 32),
            CreateImage(32, 32),
        };
        frames.Write(path, MagickFormat.Ico);
        return path;
    }

    /// <summary>An item with neither a file nor data, which every conversion must refuse.</summary>
    public static ImageItem EmptyItem() => new() { FileName = "empty.png", OriginalFormat = "Png" };

    /// <summary>The width and height of the image at <paramref name="path"/>.</summary>
    public static (int Width, int Height) ReadSize(string path) {
        var info = new MagickImageInfo(File.ReadAllBytes(path));
        return ((int)info.Width, (int)info.Height);
    }

    /// <summary>Asserts that <paramref name="path"/> holds a <paramref name="width"/> by
    /// <paramref name="height"/> image in <paramref name="format"/>.</summary>
    public static void ShouldBeImage(string path, MagickFormat format, int width, int height) {
        File.Exists(path).Should().BeTrue("{0} should have been written", path);
        var info = new MagickImageInfo(File.ReadAllBytes(path));
        info.Format.Should().Be(format);
        ((int)info.Width).Should().Be(width);
        ((int)info.Height).Should().Be(height);
    }

    /// <summary>Asserts that Magick's <paramref name="decoded"/> format is SIC!'s
    /// <paramref name="formatKey"/>, the way SIC! itself compares them.</summary>
    public static void ShouldBeFormat(MagickFormat decoded, string formatKey) {
        var item = new ImageItem { OriginalFormat = decoded.ToString() };
        ImageConverter.IsSameFormat(item, formatKey).Should().BeTrue("Magick decoded the output as {0}", decoded);
    }

    /// <summary>Encodes a generated image in <paramref name="format"/> and returns its bytes.</summary>
    public static byte[] CreateBytes(int width, int height, MagickFormat format, bool noise = false) {
        using var image = CreateImage(width, height, noise);
        return image.ToByteArray(format);
    }

    /// <summary>A vertical two-color gradient, with random noise on top when asked.</summary>
    public static MagickImage CreateImage(int width, int height, bool noise = false) {
        var settings = new MagickReadSettings {
            Width = (uint)width,
            Height = (uint)height,
        };
        var image = new MagickImage("gradient:#2060c0-#f0a030", settings);
        if (noise) {
            image.AddNoise(NoiseType.Random);
        }

        return image;
    }

    public void Dispose() {
        try {
            Directory.Delete(Folder, recursive: true);
        } catch (IOException) {
            // A file still held open by a failing test must not hide that test's own failure.
        } catch (UnauthorizedAccessException) {
            // Same as above.
        }
    }
}
