using System.Text;
using Oire.Sic.Models;
using Oire.Sic.Utils.Enums;
using Serilog;

namespace Oire.Sic.Utils;

/// <summary>
/// Reads the <c>Sic.cfg</c> INI files that SIC! 1.1 and earlier wrote with SharpConfig, so they can
/// migrate to <c>sic.json</c>. It's a small hand parser rather than SharpConfig itself: a bad value
/// keeps only that setting's default instead of discarding the whole file.
/// </summary>
internal static class LegacyConfigReader {
    private const string GeneralSection = "General";

    /// <summary>
    /// Loads <paramref name="path"/> as UTF-8 (a BOM is accepted) and parses it with
    /// <see cref="Parse"/>. I/O and access exceptions propagate to the caller.
    /// </summary>
    public static GeneralSettings Read(string path) => Parse(File.ReadAllText(path, Encoding.UTF8));

    /// <summary>
    /// Parses the content of a <c>Sic.cfg</c> file. Only keys inside <c>[General]</c> are read,
    /// matched case-insensitively; unknown keys are ignored and missing keys keep their defaults.
    /// A malformed line or an invalid value is logged and skipped, and never stops the parse.
    /// </summary>
    public static GeneralSettings Parse(string content) {
        ArgumentNullException.ThrowIfNull(content);

        var settings = new GeneralSettings();
        var inGeneral = false;
        var lineNumber = 0;

        using var reader = new StringReader(content.TrimStart('\uFEFF'));

        while (reader.ReadLine() is { } rawLine) {
            lineNumber++;
            var line = StripComment(rawLine).Trim();

            if (line.Length == 0) {
                continue;
            }

            if (line[0] == '[') {
                if (line.Length < 2 || line[^1] != ']') {
                    Log.Warning("Sic.cfg line {Line}: skipping malformed section header", lineNumber);
                    continue;
                }

                inGeneral = string.Equals(line[1..^1].Trim(), GeneralSection, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inGeneral) {
                continue;
            }

            var separator = line.IndexOf('=', StringComparison.Ordinal);

            if (separator < 0) {
                Log.Warning("Sic.cfg line {Line}: skipping a line with no '='", lineNumber);
                continue;
            }

            var key = line[..separator].Trim();
            var value = Unquote(line[(separator + 1)..].Trim());

            if (!Apply(settings, key, value)) {
                Log.Warning("Sic.cfg line {Line}: invalid value {Value} for {Key}, keeping the default",
                    lineNumber, value, key);
            }
        }

        return settings;
    }

    /// <summary>
    /// Sets the setting <paramref name="key"/> names from <paramref name="value"/>. Returns
    /// <c>false</c> when the value is invalid for that setting; an unknown key is ignored and
    /// counts as handled.
    /// </summary>
    private static bool Apply(GeneralSettings settings, string key, string value) {
        bool parsed;

        switch (key.ToUpperInvariant()) {
            case "LANGUAGE":
                if (!Localization.IsValidLanguageName(value)) {
                    return false;
                }

                settings.Language = value;
                return true;
            case "OUTPUTFOLDER":
                settings.OutputFolder = value;
                return true;
            case "LASTINPUTFOLDER":
                settings.LastInputFolder = value;
                return true;
            case "SAVETOSOURCEFOLDER":
                if (!TryParseBool(value, out parsed)) {
                    return false;
                }

                settings.SaveToSourceFolder = parsed;
                return true;
            case "CONFIRMEXITWITHQUEUE":
                if (!TryParseBool(value, out parsed)) {
                    return false;
                }

                settings.ConfirmExitWithQueue = parsed;
                return true;
            case "CHECKFORUPDATESONSTARTUP":
                if (!TryParseBool(value, out parsed)) {
                    return false;
                }

                settings.CheckForUpdatesOnStartup = parsed;
                return true;
            case "DETECTCLIPBOARDDATA":
                if (!TryParseBool(value, out parsed)) {
                    return false;
                }

                settings.DetectClipboardData = parsed;
                return true;
            case "ENABLEDFORMATS":
                settings.EnabledFormats =
                    [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
                return true;
            case "UPDATECHECKINTERVAL":
                // Exact member names only: Enum.TryParse would also take "99" or "daily".
                if (!Enum.GetNames<UpdateCheckInterval>().Contains(value, StringComparer.Ordinal)) {
                    return false;
                }

                settings.UpdateCheckInterval = Enum.Parse<UpdateCheckInterval>(value);
                return true;
            default:
                Log.Debug("Sic.cfg: ignoring unknown key {Key}", key);
                return true;
        }
    }

    /// <summary>The spellings SharpConfig accepts for a boolean, case-insensitively.</summary>
    private static bool TryParseBool(string value, out bool result) {
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || value == "1") {
            result = true;
            return true;
        }

        if (value.Equals("false", StringComparison.OrdinalIgnoreCase)
            || value.Equals("no", StringComparison.OrdinalIgnoreCase)
            || value == "0") {
            result = false;
            return true;
        }

        result = false;
        return false;
    }

    /// <summary>
    /// Cuts <paramref name="line"/> at the first <c>#</c> or <c>;</c> that is outside double quotes
    /// and not preceded by a backslash. Backslashes are otherwise literal, so a quote right after
    /// one (<c>"C:\a b\"</c>) still closes the quoted part.
    /// </summary>
    private static string StripComment(string line) {
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++) {
            var c = line[i];

            if (c == '"') {
                inQuotes = !inQuotes;
            } else if (!inQuotes && (c == '#' || c == ';') && (i == 0 || line[i - 1] != '\\')) {
                return line[..i];
            }
        }

        return line;
    }

    /// <summary>Removes one pair of surrounding double quotes, if both are there.</summary>
    private static string Unquote(string value) =>
        value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;
}
