using GetText.WindowsForms;
using Oire.Sic.Utils;
using Serilog;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

public partial class AddSizeDialog: Form {
    private const uint MinSize = 16;
    private const uint MaxSize = 512;

    public uint EnteredSize { get; private set; }

    public AddSizeDialog() {
        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog);
    }

    protected override void OnFormClosing(FormClosingEventArgs e) {
        base.OnFormClosing(e);

        if (DialogResult != DialogResult.OK)
            return;

        var text = sizeTextBox.Text.Trim();

        if (!uint.TryParse(text, out var value) || value < MinSize || value > MaxSize) {
            Log.Debug("AddSizeDialog: Invalid size entered: {Input}", text);
            MessageBox.Show(
                _("Please enter a number between {0} and {1}.", MinSize, MaxSize),
                _("Invalid Size"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            sizeTextBox.Focus();
            sizeTextBox.SelectAll();
            e.Cancel = true;
            return;
        }

        EnteredSize = value;
    }
}
