using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using NetSparkleUpdater.UI.WinForms;
using Serilog;
using Oire.Sic.Utils.Enums;
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
    private UpdateCheckInterval _loopInterval = UpdateCheckInterval.Never;
    private bool _loopRunning;
    private bool _disposed;

    public UpdateService() {
        _sparkle = new SparkleUpdater(App.AppcastUrl, new Ed25519Checker(SecurityMode.Strict, App.UpdatePublicKey)) {
            UIFactory = new UIFactory(null),
            RelaunchAfterUpdate = false,
            LogWriter = new SerilogSparkleLogWriter(),
            TmpDownloadFileNameWithExtension = $"sic-update-{Guid.NewGuid()}.exe",
        };

        Log.Information("UpdateService: Initialized with appcast URL {Url}", App.AppcastUrl);
    }

    /// <summary>
    /// Starts, stops, or re-times the background update loop to match the user's
    /// <c>Config.General.UpdateCheckInterval</c> preference. Idempotent: passing the same
    /// interval twice is a no-op. A frequency change restarts the loop with the new period;
    /// <see cref="UpdateCheckInterval.Never"/> stops it. The loop does no immediate check on
    /// start — the startup check is a separate, independently-toggled concern
    /// (see <see cref="CheckForUpdatesAsync"/>).
    /// </summary>
    public void ConfigurePeriodicChecks(UpdateCheckInterval interval) {
        if (interval == _loopInterval) {
            return;
        }

        try {
            // NetSparkle bakes the frequency in at StartLoop time, so a frequency change
            // means stop-then-start, not just a re-arm.
            if (_loopRunning) {
                _sparkle.StopLoop();
                _loopRunning = false;
            }

            if (ToFrequency(interval) is { } frequency) {
                _sparkle.StartLoop(doInitialCheck: false, forceInitialCheck: false, frequency);
                _loopRunning = true;
                Log.Information("UpdateService: Periodic update checks every {Frequency}", frequency);
            } else {
                Log.Information("UpdateService: Periodic update checks disabled");
            }

            _loopInterval = interval;
        } catch (Exception ex) {
            Log.Error(ex, "UpdateService: Failed to configure periodic checks to {Interval}", interval);
        }
    }

    /// <summary>Maps an interval to a loop period, or <c>null</c> for
    /// <see cref="UpdateCheckInterval.Never"/> (loop disabled).</summary>
    private static TimeSpan? ToFrequency(UpdateCheckInterval interval) => interval switch {
        UpdateCheckInterval.Daily => TimeSpan.FromDays(1),
        UpdateCheckInterval.EveryThreeDays => TimeSpan.FromDays(3),
        UpdateCheckInterval.Weekly => TimeSpan.FromDays(7),
        UpdateCheckInterval.Monthly => TimeSpan.FromDays(30),
        _ => null,
    };

    /// <summary>
    /// Checks the appcast once. When an update is available, the update UI is shown either way.
    /// <paramref name="announceNoUpdate"/> controls the quiet outcomes: a manual check
    /// (<c>true</c>) reports "up to date" / "previously skipped" / "couldn't check" in a
    /// message box; a silent startup check (<c>false</c>) keeps those outcomes to the log only.
    /// A failure (no network, server unreachable) is always caught and logged — it never
    /// throws, never stops the app, and only shows an error box on a manual check.
    /// </summary>
    public async Task CheckForUpdatesAsync(bool announceNoUpdate) {
        Log.Information("UpdateService: Update check requested (announceNoUpdate={Announce})", announceNoUpdate);

        try {
            var result = await _sparkle.CheckForUpdatesQuietly();
            var status = result?.Status ?? UpdateStatus.CouldNotDetermine;
            Log.Information("UpdateService: Update check result: {Status}", status);

            switch (status) {
                case UpdateStatus.UpdateAvailable:
                    _sparkle.ShowUpdateNeededUI(result!.Updates);
                    break;
                case UpdateStatus.UpdateNotAvailable:
                    if (announceNoUpdate) {
                        MessageBox.Show(
                            _("Your current version is up to date."),
                            _("Software Update"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    break;
                case UpdateStatus.UserSkipped:
                    if (announceNoUpdate) {
                        MessageBox.Show(
                            _("The latest available update was previously skipped."),
                            _("Software Update"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    break;
                case UpdateStatus.CouldNotDetermine:
                    if (announceNoUpdate) {
                        MessageBox.Show(
                            _("Unable to check for updates. Please try again later."),
                            _("Software Update"),
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    break;
            }
        } catch (Exception ex) {
            // Most commonly a transient network failure. Never fatal: log it and, for a
            // manual check, let the user know; a silent startup check stays silent.
            Log.Warning(ex, "UpdateService: Update check failed (likely no network)");
            if (announceNoUpdate) {
                MessageBox.Show(
                    _("Unable to check for updates. Please try again later."),
                    _("Software Update"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
