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
}
