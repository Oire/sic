using GetText.WindowsForms;
using Oire.Sic.Utils;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

public partial class ProgressDialog: Form {
    public ProgressDialog(string message) {
        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog);
        MessageLabel.Text = message;
    }

    public void UpdateMessage(string message) {
        if (InvokeRequired) {
            Invoke(() => MessageLabel.Text = message);
        } else {
            MessageLabel.Text = message;
        }
    }
}
