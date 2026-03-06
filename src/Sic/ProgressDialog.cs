using GetText.WindowsForms;
using Oire.Sic.Utils;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

public partial class ProgressDialog: Form {
    private readonly CancellationTokenSource _cts = new();

    public CancellationToken CancellationToken => _cts.Token;
    public bool IsCancelled => _cts.IsCancellationRequested;

    public ProgressDialog(string message) {
        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog);
        messageLabel.Text = message;
        cancelOperationButton.Click += cancelOperationButton_Click;
    }

    private void cancelOperationButton_Click(object? sender, EventArgs e) {
        _cts.Cancel();
        cancelOperationButton.Enabled = false;
    }

    public void UpdateMessage(string message) {
        if (InvokeRequired) {
            Invoke(() => messageLabel.Text = message);
        } else {
            messageLabel.Text = message;
        }
    }

    public void UpdateProgress(int current, int total) {
        if (InvokeRequired) {
            Invoke(() => SetProgress(current, total));
        } else {
            SetProgress(current, total);
        }
    }

    private void SetProgress(int current, int total) {
        if (progressBar.Style != ProgressBarStyle.Continuous) {
            progressBar.Style = ProgressBarStyle.Continuous;
        }

        progressBar.Maximum = total;
        progressBar.Value = Math.Min(current, total);
    }
}
