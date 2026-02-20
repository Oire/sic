using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

public partial class AboutDialog: Form {
    public AboutDialog() {
        InitializeComponent();
        versionLabel.Text = _("Version {0}", Application.ProductVersion);
        copyrightLabel.Text = _("\u00a9 {0} Oire Software", DateTime.Now.Year);
    }
}
