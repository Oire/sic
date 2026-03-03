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
        MessageLabel.Text = message;
        CancelOperationButton.Click += CancelOperationButton_Click;
    }

    private void CancelOperationButton_Click(object? sender, EventArgs e) {
        _cts.Cancel();
        CancelOperationButton.Enabled = false;
    }

    public void UpdateMessage(string message) {
        if (InvokeRequired) {
            Invoke(() => MessageLabel.Text = message);
        } else {
            MessageLabel.Text = message;
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
        if (ProgressBar.Style != ProgressBarStyle.Continuous) {
            ProgressBar.Style = ProgressBarStyle.Continuous;
        }

        ProgressBar.Maximum = total;
        ProgressBar.Value = Math.Min(current, total);
    }
}
