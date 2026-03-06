using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;
using NetSparkleUpdater.UI.WinForms;
using Serilog;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic.Services;

internal sealed class UpdateService: IDisposable {
    private readonly SparkleUpdater _sparkle;
    private bool _disposed;

    public UpdateService() {
        _sparkle = new SparkleUpdater(App.AppcastUrl, new Ed25519Checker(SecurityMode.Strict, App.UpdatePublicKey)) {
            UIFactory = new UIFactory(null),
            RelaunchAfterUpdate = false,
        };

        _sparkle.StartLoop(true, true, TimeSpan.FromHours(24));
        Log.Information("UpdateService: Initialized with appcast URL {Url}", App.AppcastUrl);
    }

    public async Task CheckForUpdatesAsync() {
        Log.Information("UpdateService: Manual update check requested");
        await _sparkle.CheckForUpdatesAtUserRequest();
    }

    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _sparkle.Dispose();
    }
}
