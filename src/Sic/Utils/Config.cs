using Oire.Sic.Models;
using Serilog;
using static Oire.Sic.Utils.Localization;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic.Utils;

/// <summary>
/// The settings SIC! runs with, stored in <c>sic.json</c> in the data folder through
/// <see cref="ConfigSerializer"/>. Errors go to a message box in the GUI and to stderr in the CLI.
/// </summary>
public static class Config {
    private static readonly string ConfigFilePath = Path.Combine(App.DataFolder, App.ConfigFileName);

    private static AppConfig _config = new();
    private static bool _isGui = true;
    private static bool _savesBlocked;

    public static GeneralSettings General => _config.General;

    /// <summary>
    /// Loads <c>sic.json</c>, migrating a legacy <c>Sic.cfg</c> or repairing bad values as needed,
    /// and reports any problem the way <paramref name="isGui"/> says, which <see cref="Save"/>
    /// then follows too.
    /// </summary>
    public static void Load(bool isGui = true) {
        _isGui = isGui;
        var result = ConfigSerializer.Load(App.DataFolder, App.IsPortable);
        _config = result.Config;
        _savesBlocked = result.SavesBlocked;

        // The serializer's messages are complete and translated, so they go out as they are.
        if (result.LoadError is not null) {
            ReportError(result.LoadError, isGui);
        }

        if (result.SaveError is not null) {
            ReportError(result.SaveError, isGui);
        }
    }

    /// <summary>
    /// Writes the current settings to <c>sic.json</c>. Does nothing but log when <see cref="Load"/>
    /// couldn't read the file, so a read failure never replaces the user's settings with defaults.
    /// </summary>
    public static void Save() {
        if (_savesBlocked) {
            Log.Warning("Not saving {Path}: it couldn't be read at startup", ConfigFilePath);
            return;
        }

        try {
            ConfigSerializer.Save(ConfigFilePath, _config);
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Log.Error(ex, "Failed to save {Path}", ConfigFilePath);
            ReportError(_("Unable to save configuration: {0}", ex.Message), _isGui);
        }
    }

    private static void ReportError(string message, bool isGui) {
        if (isGui) {
            DialogHelper.Show(message, _("Error"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        } else {
            Console.Error.WriteLine(message);
        }
    }
}
