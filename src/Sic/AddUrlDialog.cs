using GetText.WindowsForms;
using Oire.Sic.Utils;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

public partial class AddUrlDialog: Form {
    public string Url => urlTextBox.Text.Trim();

    public AddUrlDialog() {
        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog);
    }

    protected override void OnFormClosing(FormClosingEventArgs e) {
        base.OnFormClosing(e);

        if (DialogResult == DialogResult.OK && string.IsNullOrWhiteSpace(urlTextBox.Text)) {
            MessageBox.Show(_("Please enter a URL."), _("No URL entered"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }
    }
}
