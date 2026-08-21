using GetText.WindowsForms;
using Oire.Sic.Utils;
using Serilog;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

public partial class FitToSizeDialog: Form {
    private const int MaxDimension = 65535;

    /// <summary>The requested size budget in bytes.</summary>
    public long MaxBytes { get; private set; }

    /// <summary>The optional upper bound on output width in pixels, or <c>null</c> when not limited.</summary>
    public int? MaxWidth { get; private set; }

    public FitToSizeDialog() {
        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog);

        limitWidthCheckBox.CheckedChanged += LimitWidthCheckBox_CheckedChanged;
        maxSizeTextBox.GotFocus += (s, _) => (s as TextBox)?.BeginInvoke(((TextBox)s!).SelectAll);
        maxWidthTextBox.GotFocus += (s, _) => (s as TextBox)?.BeginInvoke(((TextBox)s!).SelectAll);
    }

    private void LimitWidthCheckBox_CheckedChanged(object? sender, EventArgs e) {
        var enabled = limitWidthCheckBox.Checked;
        maxWidthLabel.Enabled = enabled;
        maxWidthTextBox.Enabled = enabled;

        if (enabled) {
            maxWidthTextBox.Focus();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e) {
        base.OnFormClosing(e);

        if (DialogResult != DialogResult.OK) {
            return;
        }

        var sizeText = maxSizeTextBox.Text.Trim();

        if (!int.TryParse(sizeText, out var kilobytes) || kilobytes < 1) {
            Log.Debug("FitToSizeDialog: Invalid size entered: {Input}", sizeText);
            MessageBox.Show(
                _("Please enter a maximum file size in KB (a whole number of at least 1)."),
                _("Invalid size"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            maxSizeTextBox.Focus();
            maxSizeTextBox.SelectAll();
            e.Cancel = true;
            return;
        }

        int? width = null;

        if (limitWidthCheckBox.Checked) {
            var widthText = maxWidthTextBox.Text.Trim();

            if (!int.TryParse(widthText, out var w) || w < 1 || w > MaxDimension) {
                Log.Debug("FitToSizeDialog: Invalid width entered: {Input}", widthText);
                MessageBox.Show(
                    _("Please enter a valid maximum width (1–{0}).", MaxDimension),
                    _("Invalid width"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                maxWidthTextBox.Focus();
                maxWidthTextBox.SelectAll();
                e.Cancel = true;
                return;
            }

            width = w;
        }

        MaxBytes = (long)kilobytes * 1024;
        MaxWidth = width;
    }
}
