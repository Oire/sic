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

    // Formats whose Magick encoder honors a 1–100 quality setting, so file size can be traded
    // against visual quality (issue #24). Every other supported format is lossless from SIC!'s
    // point of view, leaving resizing as the only lever for hitting a size budget.
    private static readonly HashSet<string> LossyFormats = new(StringComparer.OrdinalIgnoreCase) {
        "JPG", "WEBP", "AVIF",
    };

    // Formats deliberately kept out of the "fit to file size" search (issue #24), even when the user
    // has them enabled as conversion targets:
    //   ICO  — the encoder hard-fails ("width or height exceeds limit") above 512 px per side, so
    //          probing a normal photo throws instead of returning a size. Its payload also flips
    //          between BMP and PNG around 256 px, making encoded size non-monotonic in the scale
    //          factor — which is exactly the assumption the binary search below rests on.
    //   GIF  — quantizes to at most 256 colors (a photo measured here dropped from ~750,000 colors
    //          to 76) while ignoring the quality setting entirely, so SIC! would have to advertise a
    //          heavily degraded result as "full quality". It also encodes larger than JPG or WEBP at
    //          the same dimensions, so it would never win the ranking anyway.
    // Both remain perfectly good ordinary conversion targets — they just aren't answers to
    // "get this under N kilobytes".
    private static readonly HashSet<string> SizeFitExcludedFormats = new(StringComparer.OrdinalIgnoreCase) {
        "ICO", "GIF",
    };

    // Tuning for the "fit to file size" search (issue #24).
    // TopQuality is the "no visible loss" level every lossy format is first tried at; MinFitQuality is
    // the worst SIC! will ever propose, because below it the result stops being worth offering.
    // QualityGranularity stops the quality search once the bracket is narrower than a step nobody can
    // see, and the smaller side is never resized below MinFitDimension.
    private const uint TopQuality = 95;
    private const uint MinFitQuality = 45;
    private const uint QualityGranularity = 3;
    private const int MinFitDimension = 16;

    // The size search extrapolates from probes instead of bisecting blindly, so it needs far fewer
    // encodes than a plain binary search — but MaxFitProbes still bounds the worst case. The first
    // probe is deliberately tiny (ReferenceProbeLongSide): it costs milliseconds in any format and
    // yields the bytes-per-pixel estimate that aims every probe after it, which is what keeps SIC!
    // from spending seconds encoding a full-resolution PNG that was never going to fit.
    private const int MaxFitProbes = 8;
    private const int ReferenceProbeLongSide = 384;

    // Choosing a quality by encoding the full image over and over is what made this search slow — a
    // single full-resolution AVIF encode costs around two seconds. The quality is settled on a small
    // proxy instead, then confirmed at the real size; the error between the two is used to correct the
    // proxy's budget so the next guess is far better than a blind step would be. SIC! never advertises
    // a size it has not measured at full resolution — the proxy only decides where to look.
    private const int QualityProbeLongSide = 768;
    private const int MaxQualityConfirmations = 2;

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
    /// The subset of <paramref name="formats"/> that can sensibly be used as "fit to file size" targets
    /// (issue #24), in the same order. Unlike <see cref="GetEnabledFormats"/> this may legitimately return
    /// an empty list — when the user has enabled nothing but ICO and GIF — so callers can tell "no format
    /// can even be tried" apart from "no format fits the budget".
    /// </summary>
    public static IReadOnlyList<string> GetSizeFitFormats(IEnumerable<string> formats) {
        return [.. formats.Where(f => !SizeFitExcludedFormats.Contains(f))];
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
            "HEIC" or "HEIF" => "HEIC",
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

    /// <summary>
    /// Finds the viable ways to bring <paramref name="item"/> under <paramref name="maxBytes"/> across
    /// the given <paramref name="formats"/> (issue #24). Each format is tried two ways: keep the quality
    /// and shrink (proportions always kept, and capped to <paramref name="maxWidth"/> when one is given),
    /// and — for lossy formats — keep every pixel at the highest quality the budget allows. Returns the
    /// proposals ranked best-first, resolution before quality; empty when nothing fits even at the
    /// minimum size. <paramref name="progress"/>, if supplied, is told each format key as its turn
    /// begins, so a caller can drive a determinate progress bar.
    /// </summary>
    public static IReadOnlyList<SizeFitProposal> FindSizeFitProposals(ImageItem item, IEnumerable<string> formats, long maxBytes, int? maxWidth, IProgress<string>? progress = null, CancellationToken cancellationToken = default) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);

        // Callers are expected to have filtered already, but do it again here so a stray ICO or GIF
        // can never blow up or mislead the search. See SizeFitExcludedFormats for the reasons.
        formats = GetSizeFitFormats(formats);

        using var source = LoadMagickImage(item);
        var originalWidth = (int)source.Width;
        var originalHeight = (int)source.Height;

        // The largest candidate size: the original, proportionally capped to the optional max width.
        // Every trial encode resizes the original straight to a candidate size in one step, exactly as
        // ConvertToProposal does later, so a chosen proposal's measured size is reproduced on disk.
        var fullWidth = originalWidth;
        var fullHeight = originalHeight;

        if (maxWidth.HasValue && originalWidth > maxWidth.Value) {
            fullWidth = maxWidth.Value;
            fullHeight = Math.Max(1, (int)Math.Round(originalHeight * (double)maxWidth.Value / originalWidth));
        }

        var proposals = new List<SizeFitProposal>();

        foreach (var key in formats) {
            cancellationToken.ThrowIfCancellationRequested();

            if (!FormatMap.TryGetValue(key, out var magickFormat)) {
                continue;
            }

            progress?.Report(key);

            var lossy = LossyFormats.Contains(key);

            // Both searches below converge on overlapping candidates, so they share one cache of trial
            // encodes; it is per-format because the key does not carry the format.
            var measured = new Dictionary<(int, int, uint), long>();

            // First candidate: keep the quality, shrink only as far as the budget forces.
            var best = FitAtQuality(source, magickFormat, key, lossy ? TopQuality : null, maxBytes, fullWidth, fullHeight, originalWidth, originalHeight, measured, cancellationToken);

            if (best is not null) {
                proposals.Add(best);
            }

            // Second candidate, for lossy formats that had to give up pixels: keep every pixel instead
            // and spend the budget on quality. This is usually the better answer to "must be under
            // N kilobytes", which is why the ranking below puts resolution first.
            // `Resized` compares against the original, so it is true even when a maxWidth cap already
            // shrank the image; comparing to fullWidth is what tells us pixels were actually lost.
            if (lossy && (best is null || best.Width < fullWidth)) {
                var fullSize = FitFullSizeByQuality(source, magickFormat, key, maxBytes, fullWidth, fullHeight, originalWidth, originalHeight, measured, cancellationToken);

                if (fullSize is not null && (long)fullSize.Width * fullSize.Height > (long)(best?.Width ?? 0) * (best?.Height ?? 0)) {
                    proposals.Add(fullSize);
                }
            }
        }

        // Keep as many pixels as possible first, then give up as little quality as possible: a
        // full-resolution result at quality 88 beats a shrunken one at 95 for anyone who hit a size
        // limit on an upload form. Size is only a tie-breaker between otherwise equal results.
        var ranked = proposals
            .OrderByDescending(p => (long)p.Width * p.Height)
            .ThenByDescending(p => p.Quality ?? int.MaxValue)
            .ThenBy(p => p.FileSize)
            .ToList();

        if (ranked.Count > 0) {
            ranked[0].Recommended = true;
        }

        return ranked;
    }

    /// <summary>Writes <paramref name="item"/> to disk exactly as described by <paramref name="proposal"/>
    /// (target format, proportional dimensions, and quality), the result of a prior
    /// <see cref="FindSizeFitProposals"/> search.</summary>
    public static void ConvertToProposal(ImageItem item, SizeFitProposal proposal, string outputPath) {
        if (!FormatMap.TryGetValue(proposal.Format, out var magickFormat)) {
            throw new ArgumentException($"Unsupported target format: {proposal.Format}");
        }

        using var image = LoadMagickImage(item);

        if (proposal.Width < (int)image.Width || proposal.Height < (int)image.Height) {
            image.Resize(new MagickGeometry((uint)proposal.Width, (uint)proposal.Height) {
                IgnoreAspectRatio = true,
            });
        }

        if (proposal.Quality.HasValue) {
            image.Quality = (uint)proposal.Quality.Value;
        }

        image.Format = magickFormat;
        image.Write(outputPath);

        Log.Information("Fitted {FileName} to {Format} {Width}x{Height} (quality {Quality}, {Size} bytes) at {OutputPath}",
            item.FileName, proposal.Format, proposal.Width, proposal.Height, proposal.Quality, proposal.FileSize, outputPath);
    }

    /// <summary>Finds the largest proportional result (up to <paramref name="fullWidth"/> x
    /// <paramref name="fullHeight"/>) encoded as <paramref name="format"/> at the given
    /// <paramref name="quality"/> that still fits under <paramref name="maxBytes"/>, or <c>null</c> if even
    /// the minimum allowed size overshoots.</summary>
    private static SizeFitProposal? FitAtQuality(MagickImage source, MagickFormat format, string formatKey, uint? quality, long maxBytes, int fullWidth, int fullHeight, int originalWidth, int originalHeight, Dictionary<(int, int, uint), long> measured, CancellationToken cancellationToken) {
        var minScale = (double)MinFitDimension / Math.Min(fullWidth, fullHeight);

        if (minScale >= 1.0) {
            // The image is already no bigger than the floor, so it is fit-as-is or not at all.
            var onlySize = MeasureAt(source, format, quality, fullWidth, fullHeight, measured);

            return onlySize <= maxBytes
                ? MakeProposal(formatKey, fullWidth, fullHeight, quality, onlySize, originalWidth, originalHeight)
                : null;
        }

        // The bracket: `low` is the largest scale known to fit, `high` the smallest known not to.
        double low = 0, high = double.PositiveInfinity;
        var bestWidth = 0;
        var bestHeight = 0;
        long bestSize = 0;

        // Start small. A 384-px probe costs milliseconds even in the slow formats, and its
        // bytes-per-pixel is what lets every later probe aim instead of bisect.
        var probeScale = Math.Clamp((double)ReferenceProbeLongSide / Math.Max(fullWidth, fullHeight), minScale, 1.0);

        for (var probe = 0; probe < MaxFitProbes; probe++) {
            cancellationToken.ThrowIfCancellationRequested();

            var (width, height) = ScaleDimensions(fullWidth, fullHeight, probeScale);
            var size = MeasureAt(source, format, quality, width, height, measured);

            if (size <= maxBytes) {
                if (width > bestWidth) {
                    bestWidth = width;
                    bestHeight = height;
                    bestSize = size;
                }

                low = Math.Max(low, probeScale);

                if (probeScale >= 1.0) {
                    break; // Nothing larger than the original is on offer.
                }
            } else {
                high = Math.Min(high, probeScale);
            }

            var floor = Math.Max(low, minScale);
            var ceiling = double.IsPositiveInfinity(high) ? 1.0 : high;

            // Convergence is measured in output pixels rather than in scale, because that is what the
            // user actually sees — and because a scale-based test stops far too early on formats whose
            // size tracks pixel count exactly, leaving a usable result undiscovered.
            if (ScaleWidth(fullWidth, ceiling) - ScaleWidth(fullWidth, floor) <= 1) {
                break;
            }

            // Encoded size grows roughly in step with pixel count, so the scale that lands on the
            // budget is about probeScale * sqrt(budget / measured). An estimate outside the bracket has
            // stopped telling us anything: either the full size was never ruled out (test it now) or
            // the bracket is real and bisecting is the safe move.
            var predicted = probeScale * Math.Sqrt((double)maxBytes / Math.Max(1, size));

            var nextScale = predicted > floor && predicted < ceiling
                ? predicted
                : double.IsPositiveInfinity(high) ? 1.0 : (floor + ceiling) / 2;

            // An estimate that lands back on the picture just encoded teaches nothing — that happens
            // whenever a probe overshoots the budget by a hair. Bisect the bracket instead, and only
            // give up once bisecting cannot move off this size either.
            if (ScaleWidth(fullWidth, nextScale) == width) {
                nextScale = double.IsPositiveInfinity(high) ? 1.0 : (floor + ceiling) / 2;

                if (ScaleWidth(fullWidth, nextScale) == width) {
                    break;
                }
            }

            probeScale = nextScale;
        }

        if (bestWidth == 0) {
            // Nothing probed fits. The floor is the last thing worth trying.
            var (floorWidth, floorHeight) = ScaleDimensions(fullWidth, fullHeight, minScale);
            var floorSize = MeasureAt(source, format, quality, floorWidth, floorHeight, measured);

            if (floorSize > maxBytes) {
                return null; // Even the smallest allowed image overshoots the budget.
            }

            bestWidth = floorWidth;
            bestHeight = floorHeight;
            bestSize = floorSize;
        }

        return MakeProposal(formatKey, bestWidth, bestHeight, quality, bestSize, originalWidth, originalHeight);
    }

    /// <summary>Finds the highest quality at which <paramref name="format"/> still fits under
    /// <paramref name="maxBytes"/> at the <em>full</em> requested size, or <c>null</c> when even
    /// <see cref="MinFitQuality"/> overshoots. This is the proposal most people actually want out of a
    /// size limit — every pixel kept, and only as much quality given up as the budget demands.</summary>
    private static SizeFitProposal? FitFullSizeByQuality(MagickImage source, MagickFormat format, string formatKey, long maxBytes, int fullWidth, int fullHeight, int originalWidth, int originalHeight, Dictionary<(int, int, uint), long> measured, CancellationToken cancellationToken) {
        // At a fixed quality an encoder spends roughly the same bytes per pixel whatever the size, so
        // a budget scaled down by the pixel ratio picks out very nearly the same quality on a small
        // proxy — for a fraction of the encoding cost.
        var proxyScale = Math.Min(1.0, (double)QualityProbeLongSide / Math.Max(fullWidth, fullHeight));
        var (proxyWidth, proxyHeight) = ScaleDimensions(fullWidth, fullHeight, proxyScale);
        var proxyBudget = Math.Max(1, (long)(maxBytes * ((double)proxyWidth * proxyHeight) / ((double)fullWidth * fullHeight)));

        var (candidate, proxySize) = SearchProxyQuality(source, format, proxyWidth, proxyHeight, proxyBudget, measured, cancellationToken);

        uint bestQuality = 0;
        long bestSize = 0;
        var confirmed = new HashSet<uint>();

        for (var attempt = 0; attempt < MaxQualityConfirmations; attempt++) {
            cancellationToken.ThrowIfCancellationRequested();

            if (!confirmed.Add(candidate)) {
                break; // Already measured this quality for real; the search has settled.
            }

            var size = MeasureAt(source, format, candidate, fullWidth, fullHeight, measured);

            if (size <= maxBytes && candidate > bestQuality) {
                bestQuality = candidate;
                bestSize = size;
            }

            if (proxySize <= 0) {
                break;
            }

            // One real encode is enough to calibrate the proxy. Modelling full = factor * proxy, the
            // budget that means the same thing on the proxy is maxBytes * proxySize / size — so feed
            // that back and let the cheap search choose again, now knowing its own error.
            var corrected = Math.Max(1, (long)((double)maxBytes * proxySize / Math.Max(1, size)));

            if (corrected == proxyBudget || attempt == MaxQualityConfirmations - 1) {
                break; // Settled, or there is no confirmation left to spend on a new guess.
            }

            proxyBudget = corrected;
            (candidate, proxySize) = SearchProxyQuality(source, format, proxyWidth, proxyHeight, proxyBudget, measured, cancellationToken);
        }

        if (bestQuality == 0 && confirmed.Add(MinFitQuality)) {
            // Nothing the proxy suggested actually fit. The lowest quality on offer is the last word.
            var floorSize = MeasureAt(source, format, MinFitQuality, fullWidth, fullHeight, measured);

            if (floorSize <= maxBytes) {
                bestQuality = MinFitQuality;
                bestSize = floorSize;
            }
        }

        return bestQuality > 0
            ? MakeProposal(formatKey, fullWidth, fullHeight, bestQuality, bestSize, originalWidth, originalHeight)
            : null;
    }

    /// <summary>The highest quality whose <paramref name="proxyWidth"/> x <paramref name="proxyHeight"/>
    /// encode stays within <paramref name="proxyBudget"/>, together with the size it produced. Every
    /// encode here is small and cheap; the answer is a starting point, never a promise.</summary>
    private static (uint Quality, long Size) SearchProxyQuality(MagickImage source, MagickFormat format, int proxyWidth, int proxyHeight, long proxyBudget, Dictionary<(int, int, uint), long> measured, CancellationToken cancellationToken) {
        // Callers only reach this once TopQuality has been shown not to fit at full size, so it is a
        // sound upper bound.
        uint low = MinFitQuality, high = TopQuality;
        var best = MinFitQuality;
        long bestSize = -1;

        while (high - low > QualityGranularity) {
            cancellationToken.ThrowIfCancellationRequested();

            var mid = (low + high) / 2;
            var size = MeasureAt(source, format, mid, proxyWidth, proxyHeight, measured);

            if (size <= proxyBudget) {
                best = mid;
                bestSize = size;
                low = mid;
            } else {
                high = mid;
            }
        }

        if (bestSize < 0) {
            // Nothing in the range fit the proxy budget, so the floor was never actually measured.
            bestSize = MeasureAt(source, format, best, proxyWidth, proxyHeight, measured);
        }

        return (best, bestSize);
    }

    private static (int Width, int Height) ScaleDimensions(int width, int height, double scale) {
        return (ScaleWidth(width, scale), ScaleWidth(height, scale));
    }

    private static int ScaleWidth(int width, double scale) {
        return Math.Max(1, (int)Math.Round(width * scale));
    }

    /// <summary>Encodes a clone of <paramref name="source"/> resized to
    /// <paramref name="width"/> x <paramref name="height"/> at <paramref name="quality"/> into a memory
    /// buffer and returns its byte length, remembering the answer in <paramref name="measured"/>.
    /// Nothing is written to disk. The cache matters more than it looks: both searches converge on the
    /// same few candidates, and re-encoding a full-resolution AVIF to learn something already known
    /// costs about two seconds.</summary>
    private static long MeasureAt(MagickImage source, MagickFormat format, uint? quality, int width, int height, Dictionary<(int, int, uint), long> measured) {
        var key = (width, height, quality ?? 0);

        if (measured.TryGetValue(key, out var cached)) {
            return cached;
        }

        var size = EncodeAndMeasure(source, format, quality, width, height);
        measured[key] = size;
        return size;
    }

    private static long EncodeAndMeasure(MagickImage source, MagickFormat format, uint? quality, int width, int height) {
        using var clone = (MagickImage)source.Clone();

        if (width < (int)source.Width || height < (int)source.Height) {
            clone.Resize(new MagickGeometry((uint)width, (uint)height) {
                IgnoreAspectRatio = true,
            });
        }

        if (quality.HasValue) {
            clone.Quality = quality.Value;
        }

        clone.Format = format;

        using var ms = new MemoryStream();
        clone.Write(ms);
        return ms.Length;
    }

    private static SizeFitProposal MakeProposal(string formatKey, int width, int height, uint? quality, long size, int originalWidth, int originalHeight) {
        return new SizeFitProposal {
            Format = formatKey,
            Width = width,
            Height = height,
            Quality = quality.HasValue ? (int)quality.Value : null,
            FileSize = size,
            Resized = width < originalWidth || height < originalHeight,
        };
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
