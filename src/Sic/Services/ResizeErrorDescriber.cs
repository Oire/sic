using Oire.Sic.Models;
using Oire.Sic.Utils.Enums;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic.Services;

/// <summary>
/// The message a form shows for a <see cref="ResizeError"/>, shared by the main window and the
/// preset dialog so they say the same thing. Pure: the caller shows the message and focuses the
/// box, through <see cref="Utils.DialogHelper.ShowResizeError"/>.
/// </summary>
internal static class ResizeErrorDescriber {
    /// <summary>
    /// The message, the caption and the box to focus for <paramref name="error"/> in
    /// <paramref name="mode"/>, as returned by <see cref="ResizeValidator.Validate"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="error"/> is
    /// <see cref="ResizeError.None"/>, which needs no message.</exception>
    public static (string Message, string Caption, DimensionBox Focus) Describe(ResizeError error, ResizeMode mode) {
        var max = ResizeValidator.MaxDimension;

        return error switch {
            ResizeError.None => throw new ArgumentOutOfRangeException(nameof(error), error, "A valid size needs no message."),
            ResizeError.InvalidWidth or ResizeError.CropNeedsWidth when mode == ResizeMode.Crop =>
                (_("Crop mode requires a valid width (1–{0}).", max), _("Invalid width"), DimensionBox.Width),
            ResizeError.InvalidHeight or ResizeError.CropNeedsHeight when mode == ResizeMode.Crop =>
                (_("Crop mode requires a valid height (1–{0}).", max), _("Invalid height"), DimensionBox.Height),
            ResizeError.InvalidWidth =>
                (_("The width must be a whole number from 1 to {0}, or left empty.", max), _("Invalid width"), DimensionBox.Width),
            ResizeError.InvalidHeight =>
                (_("The height must be a whole number from 1 to {0}, or left empty.", max), _("Invalid height"), DimensionBox.Height),
            _ => (_("Please enter at least one valid dimension (1–{0}).", max), _("Invalid dimensions"), DimensionBox.Width),
        };
    }
}
