namespace Oire.Sic.Services;

/// <summary>
/// Thrown when content was retrieved successfully but isn't a decodable image — most commonly a
/// valid link that points to an HTML page or some other non-image resource. Lets the UI show a
/// friendly "not an image" message instead of surfacing a raw Magick.NET decode error.
/// </summary>
public sealed class UnsupportedImageException: Exception {
    public UnsupportedImageException() {
    }

    public UnsupportedImageException(string message) : base(message) {
    }

    public UnsupportedImageException(string message, Exception innerException) : base(message, innerException) {
    }
}
