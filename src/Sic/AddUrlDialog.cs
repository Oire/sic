using GetText.WindowsForms;
using Oire.Sic.Utils;
using Serilog;
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

        if (DialogResult != DialogResult.OK)
            return;

        if (string.IsNullOrWhiteSpace(urlTextBox.Text)) {
            Log.Debug("AddUrlDialog: Empty URL submitted");
            MessageBox.Show(_("Please enter a link."), _("No link entered"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }

        if (!Uri.TryCreate(urlTextBox.Text.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) {
            Log.Debug("AddUrlDialog: Invalid URL submitted: {Url}", urlTextBox.Text.Trim());
            MessageBox.Show(
                _("Please enter a valid link starting with http:// or https://."),
                _("Invalid link"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }
    }
}
