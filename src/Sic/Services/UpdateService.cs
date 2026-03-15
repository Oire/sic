using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using NetSparkleUpdater.UI.WinForms;
using Serilog;
using static Oire.Sic.Utils.Localization;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic.Services;

/// <summary>
/// Routes NetSparkle log output through Serilog so it ends up in our log files.
/// </summary>
file sealed class SerilogSparkleLogWriter: NetSparkleUpdater.Interfaces.ILogger {
    public void PrintMessage(string message, params object[]? arguments) {
        Log.Information("NetSparkle: " + message, arguments);
    }
}

internal sealed class UpdateService: IDisposable {
    private readonly SparkleUpdater _sparkle;
    private bool _disposed;

    public UpdateService() {
        _sparkle = new SparkleUpdater(App.AppcastUrl, new Ed25519Checker(SecurityMode.Strict, App.UpdatePublicKey)) {
            UIFactory = new UIFactory(null),
            RelaunchAfterUpdate = false,
            LogWriter = new SerilogSparkleLogWriter(),
            TmpDownloadFileNameWithExtension = $"sic-update-{Guid.NewGuid()}.exe",
        };

        try {
            _sparkle.StartLoop(true, true, TimeSpan.FromHours(24));
        } catch (Exception ex) {
            Log.Error(ex, "UpdateService: Failed to start update loop");
        }

        Log.Information("UpdateService: Initialized with appcast URL {Url}", App.AppcastUrl);
    }

    public async Task CheckForUpdatesAsync() {
        Log.Information("UpdateService: Manual update check requested");

        try {
            var result = await _sparkle.CheckForUpdatesQuietly();
            var status = result?.Status ?? UpdateStatus.CouldNotDetermine;
            Log.Information("UpdateService: Update check result: {Status}", status);

            switch (status) {
                case UpdateStatus.UpdateAvailable:
                    _sparkle.ShowUpdateNeededUI(result!.Updates);
                    break;
                case UpdateStatus.UpdateNotAvailable:
                    MessageBox.Show(
                        _("Your current version is up to date."),
                        _("Software Update"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case UpdateStatus.UserSkipped:
                    MessageBox.Show(
                        _("The latest available update was previously skipped."),
                        _("Software Update"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                case UpdateStatus.CouldNotDetermine:
                    MessageBox.Show(
                        _("Unable to check for updates. Please try again later."),
                        _("Software Update"),
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }
        } catch (Exception ex) {
            Log.Error(ex, "UpdateService: Update check failed");
            MessageBox.Show(
                _("Unable to check for updates. Please try again later."),
                _("Software Update"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _sparkle.Dispose();
    }
}
