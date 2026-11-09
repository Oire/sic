using System.Globalization;
using Oire.Sic.Models;
using Oire.Sic.Utils.Enums;

namespace Oire.Sic.Services;

/// <summary>
/// Parses and checks resize sizes, shared by the main window, the CLI and the preset dialog so
/// they all accept the same input. Pure: no dialogs, no focus handling and no translated text.
/// </summary>
internal static class ResizeValidator {
    /// <summary>The largest width or height SIC! accepts, and the largest it fills in itself.</summary>
    public const int MaxDimension = 65535;

    /// <summary>
    /// Checks the width and height texts for <paramref name="mode"/>. Both are trimmed; non-empty
    /// text that isn't a whole number from 1 to <see cref="MaxDimension"/> is an error. The width
    /// is checked before the height. In crop mode both sizes are required; in keep mode at least
    /// one is. On success, a blank box gives a null size.
    /// </summary>
    public static ResizeError Validate(string widthText, string heightText, ResizeMode mode, out int? width, out int? height) {
        width = null;
        height = null;

        var widthStatus = ParseDimension(widthText, out var parsedWidth);
        var heightStatus = ParseDimension(heightText, out var parsedHeight);

        if (mode == ResizeMode.Crop) {
            switch (widthStatus) {
                case DimensionStatus.Invalid:
                    return ResizeError.InvalidWidth;
                case DimensionStatus.Blank:
                    return ResizeError.CropNeedsWidth;
            }

            switch (heightStatus) {
                case DimensionStatus.Invalid:
                    return ResizeError.InvalidHeight;
                case DimensionStatus.Blank:
                    return ResizeError.CropNeedsHeight;
            }
        } else {
            if (widthStatus == DimensionStatus.Invalid)
                return ResizeError.InvalidWidth;
            if (heightStatus == DimensionStatus.Invalid)
                return ResizeError.InvalidHeight;
            if (widthStatus == DimensionStatus.Blank && heightStatus == DimensionStatus.Blank)
                return ResizeError.MissingDimension;
        }

        width = parsedWidth;
        height = parsedHeight;
        return ResizeError.None;
    }

    /// <summary>
    /// Splits a CLI resize spec into width and height texts. Accepts <c>WxH</c>, <c>Wx</c>,
    /// <c>xH</c> and a bare <c>W</c>, with <c>x</c> in either case. Returns false when the spec
    /// has more than one separator. The texts aren't checked here; pass them to
    /// <see cref="Validate"/>.
    /// </summary>
    public static bool TryParseSpec(string spec, out string widthText, out string heightText) {
        ArgumentNullException.ThrowIfNull(spec);

        var parts = spec.Split('x', 'X');

        switch (parts.Length) {
            case 1:
                widthText = parts[0];
                heightText = "";
                return true;
            case 2:
                widthText = parts[0];
                heightText = parts[1];
                return true;
            default:
                widthText = "";
                heightText = "";
                return false;
        }
    }

    /// <summary>
    /// Scales <paramref name="target"/> from one side of an image to the other, keeping its
    /// proportions: the result is <c>target / sourceFrom * sourceTo</c>, rounded and clamped to
    /// 1..<see cref="MaxDimension"/>, so a size SIC! fills in itself is always a valid one.
    /// </summary>
    public static int ScaleDimension(int target, int sourceFrom, int sourceTo) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceFrom);

        var scaled = Math.Round((double)target / sourceFrom * sourceTo);
        return (int)Math.Clamp(scaled, 1, MaxDimension);
    }

    private static DimensionStatus ParseDimension(string text, out int? value) {
        value = null;
        var trimmed = text?.Trim() ?? "";

        if (trimmed.Length == 0)
            return DimensionStatus.Blank;

        if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            || parsed < 1
            || parsed > MaxDimension)
            return DimensionStatus.Invalid;

        value = parsed;
        return DimensionStatus.Valid;
    }

    private enum DimensionStatus {
        Blank,
        Invalid,
        Valid,
    }
}
