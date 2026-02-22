using static Oire.Sic.Utils.Localization;

namespace Oire.Sic.Models;

public class ImageItem {
    public string? FilePath { get; set; }
    public string? BasePath { get; set; }
    public byte[]? ImageData { get; set; }
    public string OriginalFormat { get; set; } = "";
    public string FileName { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSize { get; set; }

    public string GetDisplayDescription() {
        return _("{0} ({1}, {2}, {3})", FileName, OriginalFormat.ToUpperInvariant(), GetDimensionsDisplay(), GetSizeDisplay());
    }

    public string GetDimensionsDisplay() => _("{0}x{1}", Width, Height);

    public string GetSizeDisplay() {
        return FileSize switch {
            < 1024 => _("{0} B", FileSize),
            < 1024 * 1024 => _("{0} KB", (FileSize / 1024.0).ToString("F1")),
            _ => _("{0} MB", (FileSize / (1024.0 * 1024.0)).ToString("F1")),
        };
    }
}
