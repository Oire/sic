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

    /// <summary>
    /// Whether <paramref name="language"/> can be stored as the UI language: exactly
    /// <see cref="App.SystemLanguageName"/>, or the name of a culture Windows knows. Anything else
    /// would make <see cref="GetCurrentCulture"/> throw at startup.
    /// </summary>
    public static bool IsValidLanguageName(string? language) {
        if (string.IsNullOrWhiteSpace(language)) {
            return false;
        }

        if (language == App.SystemLanguageName) {
            return true;
        }

        try {
            return CultureInfo.GetCultureInfo(language, predefinedOnly: true).Name.Length > 0;
        } catch (CultureNotFoundException) {
            return false;
        }
    }

    public static CultureInfo GetCurrentCulture() {
        CultureInfo culture;

        // Use the language override if set, otherwise use system culture
        culture = String.IsNullOrWhiteSpace(_languageOverride) || _languageOverride == App.SystemLanguageName
            ? CultureInfo.InstalledUICulture
            : new CultureInfo(_languageOverride);

        // Check if locale files exist for this culture
        if (!HasLocaleFolder(culture.Name)) {
            culture = HasLocaleFolder(culture.Parent.Name)
                // Use parent culture (e.g., "de" instead of "de-AT")
                ? new CultureInfo(culture.Parent.Name)
                // Fall back to English if no locale files found
                : new CultureInfo("en-US");
        }

        return culture;
    }

    /// <summary>
    /// Whether <c>locale/&lt;cultureName&gt;</c> exists. The invariant culture, which is the parent
    /// of a neutral culture such as "pl", has an empty name, and an empty name would otherwise
    /// point at the locale folder itself.
    /// </summary>
    private static bool HasLocaleFolder(string cultureName) {
        return !string.IsNullOrEmpty(cultureName) && Directory.Exists(Path.Combine(App.LocalesFolder, cultureName));
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
