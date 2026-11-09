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
    /// The user's conversion presets, in their order. Part of the same configuration as
    /// <see cref="General"/>, so <see cref="Save"/> writes both and saving settings never loses presets.
    /// </summary>
    public static List<ConversionPreset> Presets {
        get => _config.Presets;
        set => _config.Presets = value ?? throw new ArgumentNullException(nameof(value));
    }

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
            ReportError(result.LoadError);
        }

        if (result.SaveError is not null) {
            ReportError(result.SaveError);
        }
    }

    /// <summary>
    /// Writes the current settings and presets to <c>sic.json</c>. Does nothing but log when <see cref="Load"/>
    /// couldn't read the file, so a read failure never replaces the user's settings with defaults.
    /// </summary>
    public static void Save() {
        if (_savesBlocked) {
            Log.Warning("Not saving {Path}: it couldn't be read at startup", ConfigFilePath);
            return;
        }

        if (ConfigSerializer.TrySave(ConfigFilePath, _config) is { } error) {
            ReportError(error);
        }
    }

    private static void ReportError(string message) {
        if (_isGui) {
            DialogHelper.Show(message, _("Error"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        } else {
            Console.Error.WriteLine(message);
        }
    }
}
