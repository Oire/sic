using System.Text.Json.Serialization;

namespace Oire.Sic.Models;

/// <summary>
/// A named set of conversion settings (issue #23), stored in the <c>presets</c> array of
/// <c>sic.json</c>. Applying it fills in the target format and the resize controls; the CLI takes
/// it through <c>--preset</c>.
/// </summary>
public sealed class ConversionPreset {
    /// <summary>The name shown in the menu: trimmed, at most 64 characters, unique ignoring case.</summary>
    public string Name { get; set; } = "";

    /// <summary>A SIC! format key from <see cref="Services.ImageConverter.GetSupportedFormats"/>.</summary>
    public string Format { get; set; } = "";

    /// <summary>How the sizes are applied. Always <see cref="ResizeMode.KeepProportions"/> when
    /// the preset has no sizes.</summary>
    public ResizeMode ResizeMode { get; set; } = ResizeMode.KeepProportions;

    /// <summary>The target width in pixels, or <c>null</c> for none.</summary>
    public int? Width { get; set; }

    /// <summary>The target height in pixels, or <c>null</c> for none.</summary>
    public int? Height { get; set; }

    /// <summary>Whether the preset resizes, which it does exactly when it has a size.</summary>
    [JsonIgnore]
    public bool Resizes => Width.HasValue || Height.HasValue;
}
