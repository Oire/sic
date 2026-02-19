using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

public partial class ProgressDialog: Form {
    public ProgressDialog(string message) {
        InitializeComponent();
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
