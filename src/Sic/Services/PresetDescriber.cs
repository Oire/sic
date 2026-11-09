using Oire.Sic.Models;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic.Services;

/// <summary>
/// Puts a preset into words for the Manage Presets list, such as "WEBP, width 300".
/// </summary>
internal static class PresetDescriber {
    /// <summary>
    /// The preset's format and resize settings: "WEBP", "WEBP, width 300", "WEBP, height 200",
    /// "WEBP, fit inside 800x600" or "PNG, crop to 128x128".
    /// </summary>
    public static string Describe(ConversionPreset preset) {
        ArgumentNullException.ThrowIfNull(preset);

        return (preset.ResizeMode, preset.Width, preset.Height) switch {
            (ResizeMode.Crop, { } width, { } height) => _("{0}, crop to {1}x{2}", preset.Format, width, height),
            (_, { } width, { } height) => _("{0}, fit inside {1}x{2}", preset.Format, width, height),
            (_, { } width, null) => _("{0}, width {1}", preset.Format, width),
            (_, null, { } height) => _("{0}, height {1}", preset.Format, height),
            _ => preset.Format,
        };
    }

    /// <summary>A list row: the preset's name followed by <see cref="Describe"/>, such as
    /// "Blog: WEBP, width 300".</summary>
    public static string DescribeWithName(ConversionPreset preset) {
        ArgumentNullException.ThrowIfNull(preset);
        return _("{0}: {1}", preset.Name, Describe(preset));
    }
}
