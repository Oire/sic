using System.Globalization;
using App = Oire.Sic.Utils.Constants.App;
using GetText;

namespace Oire.Sic.Utils;

internal static class Localization {
    public static ICatalog Catalog => StringsCatalog;
    private static string? _languageOverride;

    /// <summary>
    /// Sets the language override from command-line options.
    /// Should be called before any localization functions are used.
    /// </summary>
    /// <param name="language">Language code (e.g., "en-US", "pl-PL") or "System" for system default</param>
    public static void SetLanguage(string? language) {
        _languageOverride = language;
        // Reset the catalog to force re-initialization with new language
        lock (_lock) {
            _stringsCatalog = null;
        }

        var culture = GetCurrentCulture();
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
    }

    public static CultureInfo GetCurrentCulture() {
        CultureInfo culture;

        // Use the language override if set, otherwise use system culture
        culture = String.IsNullOrWhiteSpace(_languageOverride) || _languageOverride == App.SystemLanguageName
            ? CultureInfo.InstalledUICulture
            : new CultureInfo(_languageOverride);

        // Check if locale files exist for this culture
        if (!Directory.Exists(Path.Combine(App.LocalesFolder, culture.Name))) {
            if (!Directory.Exists(Path.Combine(App.LocalesFolder, culture.Parent.Name))) {
                // Fall back to English if no locale files found
                culture = new CultureInfo("en-US");
            } else {
                // Use parent culture (e.g., "en" instead of "en-US")
                culture = new CultureInfo(culture.Parent.Name);
            }
        }

        return culture;
    }

    private static Catalog? _stringsCatalog;
    private static readonly object _lock = new();

    private static Catalog StringsCatalog {
        get {
            if (_stringsCatalog == null) {
                lock (_lock) {
                    if (_stringsCatalog == null) {
                        // Use executable name (without extension) as the catalog name
                        // Use Environment.ProcessPath which works with single-file apps
                        var exePath = Environment.ProcessPath;
                        if (string.IsNullOrEmpty(exePath)) {
                            throw new InvalidOperationException("Unable to determine executable path for localization catalog");
                        }
                        var exeName = Path.GetFileNameWithoutExtension(exePath);
                        if (string.IsNullOrEmpty(exeName)) {
                            throw new InvalidOperationException("Unable to determine executable name for localization catalog");
                        }
                        _stringsCatalog = new Catalog(exeName, App.LocalesFolder, GetCurrentCulture());
                    }
                }
            }
            return _stringsCatalog;
        }
    }

    public static string _(string text) => StringsCatalog.GetString(text);

    public static string _(string text, params object[] args) => StringsCatalog.GetString(text, args);

    public static string _n(string text, string pluralText, long n) => StringsCatalog.GetPluralString(text, pluralText, n);

    public static string _n(string text, string pluralText, long n, params object[] args) => StringsCatalog.GetPluralString(text, pluralText, n, args);

    public static string _p(string context, string text) => StringsCatalog.GetParticularString(context, text);

    public static string _p(string context, string text, params object[] args) => StringsCatalog.GetParticularString(context, text, args);

    public static string _pn(string context, string text, string pluralText, long n) => StringsCatalog.GetParticularPluralString(context, text, pluralText, n);

    public static string _pn(string context, string text, string pluralText, long n, params object[] args) => StringsCatalog.GetParticularPluralString(context, text, pluralText, n, args);
}
