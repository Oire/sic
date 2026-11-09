using Oire.Sic.Models;
using Oire.Sic.Utils.Enums;

namespace Oire.Sic.Services;

/// <summary>
/// Turns the main window's format and resize controls into a preset, to pre-fill the Add Preset
/// dialog. Pure: no dialogs and no translated text.
/// </summary>
internal static class PresetCapturer {
    /// <summary>
    /// A preset with no name holding <paramref name="format"/> and the sizes worth keeping. With
    /// resize off it has no sizes. Text that isn't a valid size is left out. Crop keeps both sizes.
    /// Keep proportions leaves out the boxes SIC! filled in itself (<paramref name="autoFilledBox"/>),
    /// because those were calculated for one image, and keeps everything the user typed. A preset
    /// left with no sizes always uses <see cref="ResizeMode.KeepProportions"/>.
    /// </summary>
    public static ConversionPreset Capture(
        string format, bool resizeEnabled, ResizeMode mode, string widthText, string heightText,
        DimensionBox autoFilledBox) {
        ArgumentNullException.ThrowIfNull(format);

        var preset = new ConversionPreset { Format = format };
        if (!resizeEnabled) {
            return preset;
        }

        var width = ParseSize(widthText);
        var height = ParseSize(heightText);

        if (mode != ResizeMode.Crop) {
            if (autoFilledBox.HasFlag(DimensionBox.Width)) {
                width = null;
            }

            if (autoFilledBox.HasFlag(DimensionBox.Height)) {
                height = null;
            }
        }

        preset.Width = width;
        preset.Height = height;
        preset.ResizeMode = preset.Resizes ? mode : ResizeMode.KeepProportions;
        return preset;
    }

    private static int? ParseSize(string text) {
        // A lone width in keep mode is valid exactly when the text is a usable size.
        var error = ResizeValidator.Validate(text ?? "", "", ResizeMode.KeepProportions, out var size, out _);
        return error == ResizeError.None ? size : null;
    }
}
