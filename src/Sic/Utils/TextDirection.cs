namespace Oire.Sic.Utils;

/// <summary>
/// Right-to-left support for the UI. Hebrew needs the whole window mirrored, and WinForms
/// never derives that from the culture on its own — every form asks for it explicitly.
/// </summary>
internal static class TextDirection {
    /// <summary>True when the active UI language is written right to left.</summary>
    public static bool IsRightToLeft => Localization.GetCurrentCulture().TextInfo.IsRightToLeft;

    /// <summary>
    /// Mirrors <paramref name="form"/> to match the active language, and un-mirrors it when the
    /// user switches back to a left-to-right one. Call it right after <c>Localizer.Localize</c>.
    /// <see cref="Control.RightToLeft"/> is ambient and reaches every child on its own, but
    /// <c>RightToLeftLayout</c> is not inherited, so the controls that carry their own copy of it
    /// are walked here.
    /// </summary>
    public static void Apply(Form form) {
        var rightToLeft = IsRightToLeft;
        form.RightToLeft = rightToLeft ? RightToLeft.Yes : RightToLeft.No;
        ApplyLayout(form, rightToLeft);
    }

    private static void ApplyLayout(Control control, bool rightToLeft) {
        switch (control) {
            case Form form:
                form.RightToLeftLayout = rightToLeft;
                break;
            case ListView listView:
                listView.RightToLeftLayout = rightToLeft;
                break;
            case TabControl tabControl:
                tabControl.RightToLeftLayout = rightToLeft;
                break;
            case ProgressBar progressBar:
                progressBar.RightToLeftLayout = rightToLeft;
                break;
        }

        foreach (Control child in control.Controls) {
            ApplyLayout(child, rightToLeft);
        }
    }
}
