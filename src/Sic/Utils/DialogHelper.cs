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
}
