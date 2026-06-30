using System.Drawing;
using System.Net.Http;
using ImageMagick;
using Oire.Sic.Models;
using Serilog;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic.Services;

public static class ImageConverter {
    private const long MaxDownloadSize = 100 * 1024 * 1024; // 100 MB

    private static readonly HttpClient HttpClient = new() {
        Timeout = TimeSpan.FromSeconds(30),
    };

    private static readonly Dictionary<string, MagickFormat> FormatMap = new(StringComparer.OrdinalIgnoreCase) {
        ["JPG"] = MagickFormat.Jpeg,
        ["PNG"] = MagickFormat.Png,
        ["WEBP"] = MagickFormat.WebP,
        ["ICO"] = MagickFormat.Ico,
        ["BMP"] = MagickFormat.Bmp,
        ["TIFF"] = MagickFormat.Tiff,
        ["GIF"] = MagickFormat.Gif,
        ["AVIF"] = MagickFormat.Avif,
    };

    public static IReadOnlyList<string> GetSupportedFormats() => FormatMap.Keys.ToList();

    /// <summary>
    /// The supported formats filtered down to those whose keys appear in <paramref name="enabledKeys"/>,
    /// preserving the canonical display order. If the filter is <c>null</c>, empty, or matches no known
    /// format, every supported format is returned — the dropdown is never left empty (issue #47).
    /// </summary>
    public static IReadOnlyList<string> GetEnabledFormats(IEnumerable<string>? enabledKeys) {
        if (enabledKeys is null) {
            return GetSupportedFormats();
        }

        var set = new HashSet<string>(enabledKeys, StringComparer.OrdinalIgnoreCase);
        if (set.Count == 0) {
            return GetSupportedFormats();
        }

        var filtered = FormatMap.Keys.Where(set.Contains).ToList();
        return filtered.Count > 0 ? filtered : GetSupportedFormats();
    }

    /// <summary>
    /// Maps an arbitrary format name (a SIC! format key like "JPG", or a Magick.NET
    /// format name like "Jpeg"/"Bmp3") to one of the canonical SIC! format keys.
    /// Returns <c>null</c> for unknown formats.
    /// </summary>
    private static string? GetCanonicalFormat(string formatName) {
        return formatName.ToUpperInvariant() switch {
            "JPG" or "JPEG" or "PJPEG" => "JPG",
            "PNG" or "PNG8" or "PNG24" or "PNG32" or "PNG48" or "PNG64" or "PNG00" => "PNG",
            "WEBP" => "WEBP",
            "ICO" or "ICON" => "ICO",
            "BMP" or "BMP2" or "BMP3" => "BMP",
            "TIFF" or "TIF" or "TIFF64" => "TIFF",
            "GIF" or "GIF87" => "GIF",
            "AVIF" => "AVIF",
            _ => null,
        };
    }

    /// <summary>
    /// Returns <c>true</c> when the source image is already in the requested target format,
    /// regardless of variant naming (e.g. "Jpeg" vs "JPG", "Bmp3" vs "BMP").
    /// </summary>
    public static bool IsSameFormat(ImageItem item, string targetFormat) {
        var source = GetCanonicalFormat(item.OriginalFormat);
        var target = GetCanonicalFormat(targetFormat);
        return source is not null && source == target;
    }

    /// <summary>
    /// Determines whether converting the item to the target format would be a no-op that only
    /// degrades quality: the source is already in the target format and no resize/crop is requested.
    /// </summary>
    public static bool ShouldSkipConversion(ImageItem item, string targetFormat, int? width, int? height) {
        return !width.HasValue && !height.HasValue && IsSameFormat(item, targetFormat);
    }

    public static ImageItem LoadFromFile(string path) {
        var fileInfo = new FileInfo(path);

        if (IsIcoFile(path)) {
            using var collection = new MagickImageCollection(path);
            var frame = collection.Count > 1
                ? collection.OrderByDescending(f => (long)f.Width * f.Height).First()
                : collection[0];

            if (collection.Count > 1) {
                Log.Information(
                    "Multi-frame ICO {FileName} contains {Count} frames ({Sizes}), using largest: {Width}x{Height}",
                    fileInfo.Name, collection.Count,
                    string.Join(", ", collection.Select(f => $"{f.Width}x{f.Height}")),
                    frame.Width, frame.Height);
            }

            return new ImageItem {
                FilePath = path,
                OriginalFormat = "Ico",
                FileName = fileInfo.Name,
                Width = (int)frame.Width,
                Height = (int)frame.Height,
                FileSize = fileInfo.Length,
            };
        }

        using var image = new MagickImage(path);

        return new ImageItem {
            FilePath = path,
            OriginalFormat = image.Format.ToString(),
            FileName = fileInfo.Name,
            Width = (int)image.Width,
            Height = (int)image.Height,
            FileSize = fileInfo.Length,
        };
    }

    public static ImageItem LoadFromStream(Stream stream, string fileName) {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var data = ms.ToArray();

        using var image = new MagickImage(data);

        return new ImageItem {
            ImageData = data,
            OriginalFormat = image.Format.ToString(),
            FileName = fileName,
            Width = (int)image.Width,
            Height = (int)image.Height,
            FileSize = data.Length,
        };
    }

    public static ImageItem LoadFromBytes(byte[] data, string fileName) {
        using var image = new MagickImage(data);

        return new ImageItem {
            ImageData = data,
            OriginalFormat = image.Format.ToString(),
            FileName = fileName,
            Width = (int)image.Width,
            Height = (int)image.Height,
            FileSize = data.Length,
        };
    }

    public static async Task<ImageItem> LoadFromUrl(string url, CancellationToken cancellationToken = default, IProgress<(long BytesRead, long? TotalBytes)>? progress = null) {
        var uri = new Uri(url);

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) {
            throw new ArgumentException($"Only HTTP and HTTPS URLs are supported, got {uri.Scheme}://");
        }

        using var response = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength;

        if (contentLength > MaxDownloadSize) {
            throw new InvalidOperationException($"File is too large ({contentLength / (1024 * 1024)} MB). Maximum allowed size is {MaxDownloadSize / (1024 * 1024)} MB.");
        }

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await responseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0) {
            totalRead += bytesRead;

            if (totalRead > MaxDownloadSize) {
                throw new InvalidOperationException($"Download exceeded the maximum allowed size of {MaxDownloadSize / (1024 * 1024)} MB.");
            }

            ms.Write(buffer, 0, bytesRead);
            progress?.Report((totalRead, contentLength));
        }

        var data = ms.ToArray();
        var fileName = Path.GetFileName(uri.LocalPath);

        if (string.IsNullOrWhiteSpace(fileName)) {
            fileName = "downloaded_image";
        }

        // The link resolved and downloaded, but the bytes may be an HTML page or some other
        // non-image resource. Translate Magick's decode failure into a clear domain error so
        // the UI can say "not an image" rather than leaking a raw "no decode delegate" message.
        using var image = DecodeImage(data);

        return new ImageItem {
            ImageData = data,
            OriginalFormat = image.Format.ToString(),
            FileName = fileName,
            Width = (int)image.Width,
            Height = (int)image.Height,
            FileSize = data.Length,
        };
    }

    /// <summary>Decodes raw bytes into a <see cref="MagickImage"/>, re-throwing a Magick decode
    /// failure as an <see cref="UnsupportedImageException"/> so callers don't have to depend on
    /// Magick.NET to tell "this isn't an image" apart from a real processing fault.</summary>
    private static MagickImage DecodeImage(byte[] data) {
        try {
            return new MagickImage(data);
        } catch (MagickException ex) {
            throw new UnsupportedImageException("The content is not a supported image.", ex);
        }
    }

    public static void Convert(ImageItem item, string targetFormat, string outputPath, int? width, int? height, ResizeMode mode = ResizeMode.KeepProportions) {
        if (!FormatMap.TryGetValue(targetFormat, out var magickFormat)) {
            throw new ArgumentException($"Unsupported target format: {targetFormat}");
        }

        using var image = LoadMagickImage(item);

        // Capture the source's encoding quality before any transform. When the target format
        // matches the source, a resize/crop forces one unavoidable re-encode; reapplying the
        // original quality keeps it from being re-compressed harder than the source.
        var sourceQuality = image.Quality;
        var preserveQuality = IsSameFormat(item, targetFormat);

        if (width.HasValue || height.HasValue) {
            if (mode == ResizeMode.Crop && width.HasValue && height.HasValue) {
                var scaleX = (double)width.Value / image.Width;
                var scaleY = (double)height.Value / image.Height;
                var scale = Math.Max(scaleX, scaleY);
                var intermediateWidth = (uint)Math.Round(image.Width * scale);
                var intermediateHeight = (uint)Math.Round(image.Height * scale);
                var intermediateGeometry = new MagickGeometry(intermediateWidth, intermediateHeight) {
                    IgnoreAspectRatio = true,
                };
                image.Resize(intermediateGeometry);

                var cropGeometry = new MagickGeometry((uint)width.Value, (uint)height.Value);
                image.Crop(cropGeometry, Gravity.Center);
                image.ResetPage();
            } else {
                var geometry = CalculateResizeGeometry(image, width, height);
                image.Resize(geometry);
            }
        }

        image.Format = magickFormat;

        if (preserveQuality) {
            image.Quality = sourceQuality;
        }

        image.Write(outputPath);

        Log.Information("Converted {FileName} to {Format} at {OutputPath}", item.FileName, targetFormat, outputPath);
    }

    public static Bitmap GeneratePreview(ImageItem item, int maxWidth, int maxHeight) {
        return GeneratePreview(item, maxWidth, maxHeight, null, null, ResizeMode.KeepProportions);
    }

    public static Bitmap GeneratePreview(ImageItem item, int maxWidth, int maxHeight, int? resizeWidth, int? resizeHeight, ResizeMode resizeMode) {
        using var image = LoadMagickImage(item);

        if (resizeWidth.HasValue || resizeHeight.HasValue) {
            if (resizeMode == ResizeMode.Crop && resizeWidth.HasValue && resizeHeight.HasValue) {
                var scaleX = (double)resizeWidth.Value / image.Width;
                var scaleY = (double)resizeHeight.Value / image.Height;
                var scale = Math.Max(scaleX, scaleY);
                var intermediateWidth = (uint)Math.Round(image.Width * scale);
                var intermediateHeight = (uint)Math.Round(image.Height * scale);
                var intermediateGeometry = new MagickGeometry(intermediateWidth, intermediateHeight) {
                    IgnoreAspectRatio = true,
                };
                image.Resize(intermediateGeometry);

                var cropGeometry = new MagickGeometry((uint)resizeWidth.Value, (uint)resizeHeight.Value);
                image.Crop(cropGeometry, Gravity.Center);
                image.ResetPage();
            } else {
                var geometry = CalculateResizeGeometry(image, resizeWidth, resizeHeight);
                image.Resize(geometry);
            }
        }

        var previewGeometry = new MagickGeometry((uint)maxWidth, (uint)maxHeight) {
            IgnoreAspectRatio = false,
        };
        image.Resize(previewGeometry);

        using var ms = new MemoryStream();
        image.Write(ms, MagickFormat.Bmp);
        ms.Position = 0;
        return new Bitmap(ms);
    }

    public static string GetFileExtension(string format) {
        return format.ToUpperInvariant() switch {
            "JPG" => ".jpg",
            "PNG" => ".png",
            "WEBP" => ".webp",
            "ICO" => ".ico",
            "BMP" => ".bmp",
            "TIFF" => ".tiff",
            "GIF" => ".gif",
            "AVIF" => ".avif",
            _ => $".{format.ToLowerInvariant()}"
        };
    }

    public static string GenerateOutputPath(ImageItem item, string targetFormat, string outputFolder, bool saveToSourceFolder = false) {
        var baseName = Path.GetFileNameWithoutExtension(item.FileName);
        var extension = GetFileExtension(targetFormat);

        // Write next to the original when requested (issue #33), but only for items that actually
        // came from a file on disk. Clipboard captures and downloaded links have no source folder,
        // so they fall through to the configured output folder below.
        if (saveToSourceFolder && !string.IsNullOrWhiteSpace(item.FilePath)) {
            var sourceDir = Path.GetDirectoryName(item.FilePath);
            if (!string.IsNullOrWhiteSpace(sourceDir)) {
                return Path.Combine(sourceDir, baseName + extension);
            }
        }

        if (string.IsNullOrWhiteSpace(outputFolder)) {
            outputFolder = App.DefaultOutputFolder;
        }

        if (item.BasePath != null && item.FilePath != null) {
            var relativePath = Path.GetRelativePath(item.BasePath, item.FilePath);
            var relativeDir = Path.GetDirectoryName(relativePath) ?? "";
            return Path.Combine(outputFolder, relativeDir, baseName + extension);
        }

        return Path.Combine(outputFolder, baseName + extension);
    }

    public static string GetConflictRenamePath(string outputPath) {
        var directory = Path.GetDirectoryName(outputPath) ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(outputPath);
        var extension = Path.GetExtension(outputPath);
        var counter = 1;

        string newPath;

        do {
            newPath = Path.Combine(directory, $"{baseName}_{counter}{extension}");
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }

    public static void CreateMultiSizeIco(ImageItem item, string outputPath, uint[] sizes) {
        using var source = LoadMagickImage(item);

        using var collection = new MagickImageCollection();

        foreach (var size in sizes) {
            var frame = (MagickImage)source.Clone();
            var geometry = new MagickGeometry(size, size) {
                IgnoreAspectRatio = true,
            };
            frame.Resize(geometry);
            collection.Add(frame);
        }

        collection.Write(outputPath, MagickFormat.Ico);
        Log.Information("Created multi-size ICO from {FileName} at {OutputPath} with sizes {Sizes}", item.FileName, outputPath, string.Join(", ", sizes));
    }

    private static MagickImage LoadMagickImage(ImageItem item) {
        if (item.ImageData is not null) {
            if (string.Equals(item.OriginalFormat, "Ico", StringComparison.OrdinalIgnoreCase)) {
                return SelectLargestIcoFrame(new MagickImageCollection(item.ImageData));
            }

            return new MagickImage(item.ImageData);
        }

        if (!string.IsNullOrWhiteSpace(item.FilePath)) {
            if (IsIcoFile(item.FilePath)) {
                return SelectLargestIcoFrame(new MagickImageCollection(item.FilePath));
            }

            return new MagickImage(item.FilePath);
        }

        throw new InvalidOperationException("ImageItem has no data source");
    }

    private static bool IsIcoFile(string path) {
        return Path.GetExtension(path).Equals(".ico", StringComparison.OrdinalIgnoreCase);
    }

    private static MagickImage SelectLargestIcoFrame(MagickImageCollection collection) {
        using (collection) {
            var largest = collection.OrderByDescending(f => (long)f.Width * f.Height).First();
            return (MagickImage)largest.Clone();
        }
    }

    private static MagickGeometry CalculateResizeGeometry(MagickImage image, int? width, int? height) {
        if (width.HasValue && height.HasValue) {
            var scale = Math.Min((double)width.Value / image.Width, (double)height.Value / image.Height);
            var newWidth = (uint)Math.Round(image.Width * scale);
            var newHeight = (uint)Math.Round(image.Height * scale);
            return new MagickGeometry(newWidth, newHeight) {
                IgnoreAspectRatio = true,
            };
        }

        if (width.HasValue) {
            var ratio = (double)width.Value / image.Width;
            var newHeight = (uint)Math.Round(image.Height * ratio);
            return new MagickGeometry((uint)width.Value, newHeight) {
                IgnoreAspectRatio = true,
            };
        }

        if (height.HasValue) {
            var ratio = (double)height.Value / image.Height;
            var newWidth = (uint)Math.Round(image.Width * ratio);
            return new MagickGeometry(newWidth, (uint)height.Value) {
                IgnoreAspectRatio = true,
            };
        }

        return new MagickGeometry(image.Width, image.Height);
    }
}
