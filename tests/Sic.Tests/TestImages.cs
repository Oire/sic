using ImageMagick;

namespace Oire.Sic.Tests;

/// <summary>
/// Generates small test images with Magick in a temporary folder of its own, and deletes the
/// folder on dispose. Images are gradients, optionally with Magick's noise on top, rather than
/// solid colors, so their encoded size behaves like a real picture's. No binary fixtures are
/// committed.
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
    /// <paramref name="fileName"/> inside <see cref="Folder"/>, in the format given by
    /// <paramref name="format"/> or, when that's <c>null</c>, inferred from the file extension.
    /// Subfolders in <paramref name="fileName"/> are created. Returns the full path.
    /// </summary>
    public string Create(string fileName, int width, int height, MagickFormat? format = null, bool noise = false) {
        var path = Path.Combine(Folder, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var image = CreateImage(width, height, noise);
        if (format.HasValue) {
            image.Write(path, format.Value);
        } else {
            image.Write(path);
        }

        return path;
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
