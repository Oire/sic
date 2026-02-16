namespace Oire.Sic.Models;

public class ImageItem {
    public string? FilePath { get; set; }
    public byte[]? ImageData { get; set; }
    public string OriginalFormat { get; set; } = "";
    public string FileName { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSize { get; set; }

    public string GetDisplayDescription() {
        var size = FileSize switch {
            < 1024 => $"{FileSize} B",
            < 1024 * 1024 => $"{FileSize / 1024.0:F1} KB",
            _ => $"{FileSize / (1024.0 * 1024.0):F1} MB"
        };

        return $"{FileName} ({OriginalFormat.ToUpperInvariant()}, {Width}x{Height}, {size})";
    }

    public string GetDimensionsDisplay() => $"{Width}x{Height}";

    public string GetSizeDisplay() {
        return FileSize switch {
            < 1024 => $"{FileSize} B",
            < 1024 * 1024 => $"{FileSize / 1024.0:F1} KB",
            _ => $"{FileSize / (1024.0 * 1024.0):F1} MB"
        };
    }
}
