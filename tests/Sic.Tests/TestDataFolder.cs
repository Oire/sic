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
        if (!Directory.Exists(App.DataFolder)) {
            return;
        }

        foreach (var pattern in ConfigPatterns) {
            foreach (var file in Directory.EnumerateFiles(App.DataFolder, pattern)) {
                File.Delete(file);
            }
        }
    }
}
