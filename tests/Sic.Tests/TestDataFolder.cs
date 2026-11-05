using Oire.Sic.Utils.Constants;

namespace Oire.Sic.Tests;

/// <summary>
/// Helpers for tests that place config files in <see cref="App.DataFolder"/>, which under test
/// is the <c>userdata</c> folder in the test output (see <see cref="TestIsolationTests"/>).
/// </summary>
internal static class TestDataFolder {
    private static readonly string[] ConfigPatterns = ["Sic.cfg*", "sic.json*"];

    /// <summary>
    /// Deletes every config file and its backups (<c>Sic.cfg*</c>, <c>sic.json*</c>, which also
    /// covers names such as <c>sic.json.bad.&lt;timestamp&gt;</c>) from the data folder.
    /// </summary>
    public static void Clear() {
        EnsureIsolated();

        if (!Directory.Exists(App.DataFolder)) {
            return;
        }

        foreach (var pattern in ConfigPatterns) {
            foreach (var file in Directory.EnumerateFiles(App.DataFolder, pattern)) {
                File.Delete(file);
            }
        }
    }

    /// <summary>
    /// Throws unless SIC! runs as portable with its data folder inside the test output, so a test
    /// that deletes or writes config files can never reach the user's real <c>%APPDATA%\Oire\Sic</c>.
    /// </summary>
    public static void EnsureIsolated() {
        var outputFolder = Path.GetFullPath(AppContext.BaseDirectory);
        var dataFolder = Path.GetFullPath(App.DataFolder);

        if (!App.IsPortable || !dataFolder.StartsWith(outputFolder, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException(
                $"Tests are not isolated: the data folder is {dataFolder}, outside the test output {outputFolder}. "
                + "Refusing to touch config files there."
            );
        }
    }
}
