using System.Text;

namespace Oire.Sic.Utils;

public static class UrlHelper {
    /// <summary>
    /// Trims <paramref name="text"/> and reports whether it is an absolute http(s) URL — the
    /// single rule SIC! applies to links from any source (the "Add by link" dialog, a Ctrl+V
    /// paste, or clipboard auto-detection). The trimmed value is always returned in
    /// <paramref name="url"/> so callers can use it whether validation passed or failed.
    /// </summary>
    public static bool IsValidHttpUrl(string? text, out string url) {
        url = text?.Trim() ?? string.Empty;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>The name given to a download whose link has no usable file name.</summary>
    public const string DefaultDownloadName = "downloaded_image";

    /// <summary>
    /// The file name a downloaded <paramref name="uri"/> is saved under: the last segment of its
    /// path, unescaped ("my%20photo.png" becomes "my photo.png"), with every character Windows
    /// forbids in a file name replaced by <c>_</c> and trailing dots and spaces removed. Without
    /// the cleaning, "C%3Aphoto.png" would become the rooted "C:photo.png" and escape the output
    /// folder, and "File:Photo.png" would be written into an alternate data stream. A link with
    /// no file name, or one that cleans down to nothing, gets <see cref="DefaultDownloadName"/>.
    /// </summary>
    public static string GetDownloadFileName(Uri uri) {
        ArgumentNullException.ThrowIfNull(uri);

        // AbsolutePath keeps escapes, so an escaped slash or backslash stays inside the last
        // segment and is cleaned below instead of being taken for a folder separator.
        var path = uri.AbsolutePath;
        var name = Uri.UnescapeDataString(path[(path.LastIndexOf('/') + 1)..]);

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new StringBuilder(name.Length);
        foreach (var c in name) {
            cleaned.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        }

        var result = cleaned.ToString().TrimEnd('.', ' ');
        return string.IsNullOrWhiteSpace(result) ? DefaultDownloadName : result;
    }
}
