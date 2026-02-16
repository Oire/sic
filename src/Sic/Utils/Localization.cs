using System.Globalization;
using System.IO;
using App = Oire.Sic.Utils.Constants.App;
using GetText;
using System.Runtime.CompilerServices;

namespace Oire.Sic.Utils;

internal static class Localization {
    public static CultureInfo GetCurrentCulture() {
        CultureInfo culture;

        culture = Config.General.Language == App.SystemLanguageName
            ? CultureInfo.InstalledUICulture
            : new CultureInfo(Config.General.Language);

        if (!Directory.Exists(Path.Combine(App.LocalesFolder, culture.Name))) {
            if (!Directory.Exists(Path.Combine(App.LocalesFolder, culture.Parent.Name))) {
                culture = new CultureInfo("en-US");
            }

            culture = new CultureInfo(culture.Parent.Name);
        }

        return culture;
    }

    private static Catalog stringsCatalog = new Catalog(App.Name, App.LocalesFolder, Localization.GetCurrentCulture());

    public static string _(string text) {
        return stringsCatalog.GetString(text);
    }

    public static string _(string text, params object[] args) {
        return stringsCatalog.GetString(text, args);
    }

    public static string _n(string text, string pluralText, long n) {
        return stringsCatalog.GetPluralString(text, pluralText, n);
    }

    public static string _n(string text, string pluralText, long n, params object[] args) {
        return stringsCatalog.GetPluralString(text, pluralText, n, args);
    }

    public static string _p(string context, string text) {
        return stringsCatalog.GetParticularString(context, text);
    }

    public static string _p(string context, string text, params object[] args) {
        return stringsCatalog.GetParticularString(context, text, args);
    }

    public static string _pn(string context, string text, string pluralText, long n) {
        return stringsCatalog.GetParticularPluralString(context, text, pluralText, n);
    }

    public static string _pn(string context, string text, string pluralText, long n, params object[] args) {
        return stringsCatalog.GetParticularPluralString(context, text, pluralText, n, args);
    }
}
