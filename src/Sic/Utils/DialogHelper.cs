using Oire.Sic.Models;
using Oire.Sic.Services;
using Oire.Sic.Utils.Enums;

namespace Oire.Sic.Utils;

/// <summary>
/// Message boxes that follow the active language's reading direction. WinForms mirrors a message
/// box only when told to through <see cref="MessageBoxOptions"/>, and unlike
/// <see cref="Control.RightToLeft"/> that is not an ambient property a form can pass down — so
/// SIC! routes every message box through here instead of calling <see cref="MessageBox"/> directly.
/// </summary>
internal static class DialogHelper {
    private static MessageBoxOptions DirectionOptions => TextDirection.IsRightToLeft
        ? MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading
        : 0;

    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon) =>
        MessageBox.Show(text, caption, buttons, icon, MessageBoxDefaultButton.Button1, DirectionOptions);

    /// <summary>
    /// Shows the warning <see cref="ResizeErrorDescriber"/> gives for <paramref name="error"/>, then
    /// focuses and selects the size box it names, so the user can retype it at once.
    /// </summary>
    public static void ShowResizeError(ResizeError error, ResizeMode mode, TextBox widthBox, TextBox heightBox) {
        ArgumentNullException.ThrowIfNull(widthBox);
        ArgumentNullException.ThrowIfNull(heightBox);

        var (message, caption, focus) = ResizeErrorDescriber.Describe(error, mode);
        Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);

        var box = focus == DimensionBox.Height ? heightBox : widthBox;
        box.Focus();
        box.SelectAll();
    }
}
