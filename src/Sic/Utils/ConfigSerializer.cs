using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Oire.Sic.Models;
using Oire.Sic.Services;
using Serilog;
using static Oire.Sic.Utils.Localization;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic.Utils;

/// <summary>
/// Reads and writes <c>sic.json</c>: migrates a legacy <c>Sic.cfg</c>, repairs bad values one at a
/// time, keeps a damaged file as <c>sic.json.bad</c>, and saves atomically. It shows no UI: every
/// message comes back complete and translated in a <see cref="ConfigLoadResult"/>.
/// </summary>
internal static class ConfigSerializer {
    private const string BackupSuffix = ".bad";
    private const string LegacyBackupSuffix = ".bak";
    private const int RetryAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// The options for every value in <c>sic.json</c>. Integers are refused for enums, because the
    /// default converter would take <c>99</c> and silently turn off update checks. The converter
    /// still combines a list such as <c>"Weekly, Never"</c> into a value no member has, so
    /// <see cref="ReadGeneral"/> checks every enum it reads as well.
    /// </summary>
    internal static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    /// <summary><see cref="Options"/> doesn't reach <see cref="JsonDocument"/>, so it gets its own.</summary>
    private static readonly JsonDocumentOptions DocumentOptions = new() {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private static readonly PropertyInfo[] GeneralProperties =
        typeof(GeneralSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.CanWrite)
            .ToArray();

    /// <summary>
    /// Loads the configuration from <paramref name="folder"/>: <c>sic.json</c> if it exists,
    /// otherwise a legacy <c>Sic.cfg</c>, otherwise defaults, which are then written. Migration and
    /// first run only ever create <c>sic.json</c>, never replace it, so a CLI run and the GUI
    /// starting together can't overwrite what the other just wrote: whichever loses reads the
    /// winner's file instead.
    /// </summary>
    /// <param name="folder">The data folder holding the config files.</param>
    /// <param name="isPortable">Whether SIC! runs as portable, which lets a migrated
    /// <c>...\userdata\Converted</c> folder from another location count as the default.</param>
    public static ConfigLoadResult Load(string folder, bool isPortable) {
        ArgumentNullException.ThrowIfNull(folder);

        var jsonPath = Path.Combine(folder, App.ConfigFileName);
        var legacyPath = Path.Combine(folder, App.LegacyConfigFileName);

        for (var attempt = 1; ; attempt++) {
            // Null when sic.json vanished after File.Exists: it's then treated as missing.
            if (File.Exists(jsonPath) && LoadJson(jsonPath) is { } loaded) {
                return loaded;
            }

            // Null when another process moved the files on in the meantime: look again.
            var created = File.Exists(legacyPath)
                ? MigrateLegacy(jsonPath, legacyPath, isPortable)
                : CreateDefaults(jsonPath);

            if (created is not null) {
                return created;
            }

            if (attempt == RetryAttempts) {
                // Only if other processes keep creating and deleting sic.json: write nothing.
                Log.Warning("{Path} kept appearing and vanishing, using the defaults without saving", jsonPath);
                var config = new AppConfig();
                CleanUp(config);
                return new ConfigLoadResult(config, null, null, SavesBlocked: true);
            }
        }
    }

    /// <summary>
    /// Writes <paramref name="config"/> to <paramref name="path"/> as indented UTF-8 with no BOM,
    /// through a temporary file in the same folder that then replaces it. The default output
    /// folder is written as <c>""</c>; <paramref name="config"/> itself is never changed. Throws on
    /// failure, leaving the previous file intact and no temporary file behind.
    /// </summary>
    public static void Save(string path, AppConfig config) => Write(path, config, overwrite: true);

    /// <summary>
    /// Writes as <see cref="Save"/> does. With <paramref name="overwrite"/> off an existing file is
    /// left alone: the temporary file is then deleted and the result is <c>false</c>.
    /// </summary>
    private static bool Write(string path, AppConfig config, bool overwrite) {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(config);

        var node = JsonSerializer.SerializeToNode(config, Options)
            ?? throw new InvalidOperationException("The configuration serialized to null.");

        if (node[PropertyName(nameof(AppConfig.General))] is JsonObject general
            && App.IsDefaultOutputFolder(config.General.OutputFolder)) {
            general[PropertyName(nameof(GeneralSettings.OutputFolder))] = "";
        }

        var bytes = Utf8NoBom.GetBytes(node.ToJsonString(Options));
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("The path has no folder.", nameof(path));
        Directory.CreateDirectory(directory);

        // A unique name, because a CLI run may be saving beside the GUI.
        var tempPath = Path.Combine(directory, $"{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            if (MoveWithRetry(tempPath, fullPath, overwrite)) {
                return true;
            }
        } catch {
            TryDelete(tempPath);
            throw;
        }

        TryDelete(tempPath);
        return false;
    }

    /// <summary>
    /// Copies <paramref name="path"/> to <c>&lt;path&gt;.bad</c>. An existing <c>.bad</c> is first
    /// renamed to <c>&lt;path&gt;.bad.&lt;yyyyMMdd-HHmmss&gt;</c>, with a number added if that name
    /// is taken, so no backup is ever overwritten. <paramref name="path"/> itself is never moved.
    /// </summary>
    /// <returns>The full path of the new backup.</returns>
    public static string BackUpFile(string path) {
        ArgumentNullException.ThrowIfNull(path);

        var backupPath = path + BackupSuffix;

        if (File.Exists(backupPath)) {
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var rotatedPath = $"{backupPath}.{stamp}";

            for (var number = 1; File.Exists(rotatedPath); number++) {
                rotatedPath = $"{backupPath}.{stamp}-{number}";
            }

            File.Move(backupPath, rotatedPath);
        }

        File.Copy(path, backupPath, overwrite: false);
        return backupPath;
    }

    /// <returns>The result, or <see langword="null"/> when the file no longer exists.</returns>
    private static ConfigLoadResult? LoadJson(string jsonPath) {
        byte[] bytes;

        try {
            bytes = ReadSharedWithRetry(jsonPath);
        } catch (FileNotFoundException) {
            Log.Information("{Path} was removed before it could be read", jsonPath);
            return null;
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Log.Error(ex, "Failed to read {Path}", jsonPath);
            return Blocked(_(
                "SIC! couldn't read the settings file {0}: {1} It started with the default settings, "
                + "and changes won't be saved this session.", jsonPath, ex.Message));
        }

        AppConfig config;
        int badValues;
        int droppedPresets;

        try {
            using var document = JsonDocument.Parse(StrictUtf8.GetString(SkipBom(bytes)), DocumentOptions);

            if (document.RootElement.ValueKind != JsonValueKind.Object) {
                throw new JsonException("The root of the settings file is not an object.");
            }

            (config, badValues, droppedPresets) = ReadConfig(document.RootElement);
        } catch (Exception ex) when (ex is JsonException or DecoderFallbackException or InvalidOperationException) {
            // InvalidOperationException: JsonDocument accepts a name with an unpaired surrogate
            // such as "\uD800", which JsonProperty.Name then can't decode.
            Log.Error(ex, "{Path} is not valid JSON", jsonPath);
            return RecoverCorruptFile(jsonPath);
        }

        CleanUp(config);

        if (badValues == 0 && droppedPresets == 0) {
            return new ConfigLoadResult(config, null, null, SavesBlocked: false);
        }

        Log.Warning("{Path}: {Count} invalid values were reset to their defaults, {Presets} presets were dropped",
            jsonPath, badValues, droppedPresets);

        // Dropped presets would vanish from disk with the next save, so the file is kept first,
        // in the same single backup as bad settings.
        var problems = DescribeProblems(jsonPath, badValues, droppedPresets);
        string backupPath;

        try {
            backupPath = BackUpFile(jsonPath);
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Log.Error(ex, "Failed to back up {Path}", jsonPath);
            var blockedError = problems + " " + _(
                "SIC! couldn't make a copy of the file ({0}), so it was left as it is, and changes won't "
                + "be saved this session.", ex.Message);
            return new ConfigLoadResult(config, blockedError, null, SavesBlocked: true);
        }

        var loadError = problems + " " + _("A copy of the original file was saved as {0}.", backupPath);
        return new ConfigLoadResult(config, loadError, TrySave(jsonPath, config), SavesBlocked: false);
    }

    /// <summary>
    /// The sentences naming what loading had to repair: bad settings, dropped presets or both. The
    /// path is named once, in the first sentence.
    /// </summary>
    private static string DescribeProblems(string jsonPath, int badValues, int droppedPresets) {
        var sentences = new List<string>(2);

        if (badValues > 0) {
            sentences.Add(_n(
                "{0} setting in {1} had an invalid value and was reset to its default.",
                "{0} settings in {1} had invalid values and were reset to their defaults.",
                badValues, badValues, jsonPath));
        }

        if (droppedPresets > 0) {
            sentences.Add(badValues > 0
                ? _n(
                    "{0} preset couldn't be used and was removed.",
                    "{0} presets couldn't be used and were removed.",
                    droppedPresets, droppedPresets)
                : _n(
                    "{0} preset in {1} couldn't be used and was removed.",
                    "{0} presets in {1} couldn't be used and were removed.",
                    droppedPresets, droppedPresets, jsonPath));
        }

        return string.Join(" ", sentences);
    }

    /// <summary>Keeps a file that isn't valid JSON as <c>sic.json.bad</c>, then replaces it with defaults.</summary>
    private static ConfigLoadResult RecoverCorruptFile(string jsonPath) {
        var config = new AppConfig();
        CleanUp(config);
        string backupPath;

        try {
            backupPath = BackUpFile(jsonPath);
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Log.Error(ex, "Failed to back up {Path}", jsonPath);
            return Blocked(_(
                "The settings file {0} is damaged, and SIC! couldn't make a copy of it: {1} It started "
                + "with the default settings and left the file as it is, and changes won't be saved "
                + "this session.", jsonPath, ex.Message));
        }

        var loadError = _(
            "The settings file {0} is damaged, so SIC! started with the default settings. A copy of "
            + "the damaged file was saved as {1}.", jsonPath, backupPath);

        return new ConfigLoadResult(config, loadError, TrySave(jsonPath, config), SavesBlocked: false);
    }

    /// <summary>Reads <c>Sic.cfg</c>, writes <c>sic.json</c> and only then renames <c>Sic.cfg</c> to <c>Sic.cfg.bak</c>.</summary>
    /// <returns>The result, or <see langword="null"/> when <c>Sic.cfg</c> vanished before it could
    /// be read, or <c>sic.json</c> appeared and then vanished, so <see cref="Load"/> looks again.</returns>
    internal static ConfigLoadResult? MigrateLegacy(string jsonPath, string legacyPath, bool isPortable) {
        var config = new AppConfig();

        try {
            config.General = LegacyConfigReader.Read(legacyPath);
        } catch (FileNotFoundException) {
            // Another process migrated it and renamed it to Sic.cfg.bak.
            Log.Information("{Path} was removed before it could be read", legacyPath);
            return null;
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Log.Error(ex, "Failed to read {Path}", legacyPath);
            return Blocked(_(
                "SIC! couldn't read the old settings file {0}: {1} It started with the default settings, "
                + "and changes won't be saved this session.", legacyPath, ex.Message));
        }

        // Sic.cfg stored the full path, so a userdata folder copied from another portable copy
        // would otherwise keep pointing at the old copy's Converted folder.
        if (isPortable && IsPortableDefaultOutputFolder(config.General.OutputFolder)) {
            config.General.OutputFolder = "";
        }

        CleanUp(config);

        if (!TryCreate(jsonPath, config, out var saveError)) {
            // Another process migrated first, and renames Sic.cfg itself.
            return LoadAfterLostRace(jsonPath);
        }

        if (saveError is not null) {
            return new ConfigLoadResult(config, null, saveError, SavesBlocked: false);
        }

        try {
            File.Move(legacyPath, legacyPath + LegacyBackupSuffix, overwrite: true);
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            // Harmless: sic.json wins over Sic.cfg from now on.
            Log.Warning(ex, "Failed to rename {Path} after migrating it", legacyPath);
        }

        Log.Information("Migrated {LegacyPath} to {Path}", legacyPath, jsonPath);
        return new ConfigLoadResult(config, null, null, SavesBlocked: false);
    }

    /// <summary>Writes the defaults. <see cref="Save"/> creates the data folder if it's missing.</summary>
    /// <returns>The result, or <see langword="null"/> when <c>sic.json</c> appeared and then
    /// vanished, so <see cref="Load"/> looks again.</returns>
    internal static ConfigLoadResult? CreateDefaults(string jsonPath) {
        var config = new AppConfig();
        CleanUp(config);

        if (!TryCreate(jsonPath, config, out var saveError)) {
            return LoadAfterLostRace(jsonPath);
        }

        return new ConfigLoadResult(config, null, saveError, SavesBlocked: false);
    }

    /// <summary>Reads the <c>sic.json</c> another process created while this one was about to.</summary>
    private static ConfigLoadResult? LoadAfterLostRace(string jsonPath) {
        Log.Information("{Path} was created by another process, reading it instead", jsonPath);
        return LoadJson(jsonPath);
    }

    /// <summary>
    /// Reads the root object one section and one value at a time, so a bad value costs only itself.
    /// </summary>
    /// <returns>The settings read, how many values were bad and kept their defaults, and how many
    /// presets couldn't be used.</returns>
    private static (AppConfig Config, int BadValues, int DroppedPresets) ReadConfig(JsonElement root) {
        var config = new AppConfig();
        var badValues = 0;
        var droppedPresets = 0;
        var presetsRead = false;

        foreach (var section in root.EnumerateObject()) {
            if (string.Equals(section.Name, nameof(AppConfig.Presets), StringComparison.OrdinalIgnoreCase)) {
                var (presets, dropped) = ReadPresets(section.Value);

                // A hand-edited file can hold the key twice, in any case. The last one wins, as
                // JSON readers usually do, and the presets it replaces count as dropped, so they
                // are backed up and reported rather than lost at the next save.
                if (presetsRead) {
                    Log.Warning("sic.json: {Name} appears more than once, dropping the {Count} presets read before",
                        section.Name, config.Presets.Count);
                    droppedPresets += config.Presets.Count;
                }

                config.Presets = presets;
                droppedPresets += dropped;
                presetsRead = true;
                continue;
            }

            if (!string.Equals(section.Name, nameof(AppConfig.General), StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            switch (section.Value.ValueKind) {
                case JsonValueKind.Null:
                    config.General = new GeneralSettings();
                    break;
                case JsonValueKind.Object:
                    config.General = ReadGeneral(section.Value, ref badValues);
                    break;
                default:
                    Log.Warning("sic.json: the general section is not an object, using the defaults");
                    config.General = new GeneralSettings();
                    badValues++;
                    break;
            }
        }

        return (config, badValues, droppedPresets);
    }

    /// <summary>
    /// Reads the <c>presets</c> array one entry at a time, so an entry that can't be deserialized
    /// costs only itself, then keeps the usable ones through <see cref="PresetValidator.Normalize"/>.
    /// A section that is neither an array nor null counts as one dropped preset.
    /// </summary>
    private static (List<ConversionPreset> Presets, int Dropped) ReadPresets(JsonElement section) {
        switch (section.ValueKind) {
            case JsonValueKind.Null:
                return ([], 0);
            case JsonValueKind.Array:
                break;
            default:
                Log.Warning("sic.json: the presets section is not an array, dropping it");
                return ([], 1);
        }

        var entries = new List<ConversionPreset?>();
        var failed = 0;

        foreach (var entry in section.EnumerateArray()) {
            try {
                entries.Add(entry.Deserialize<ConversionPreset>(Options));
            } catch (Exception ex) when (ex is JsonException or InvalidOperationException) {
                Log.Warning("sic.json: dropping a preset that can't be read: {Error}", ex.Message);
                failed++;
            }
        }

        var (presets, dropped) = PresetValidator.Normalize(entries);
        return (presets, failed + dropped);
    }

    private static GeneralSettings ReadGeneral(JsonElement section, ref int badValues) {
        var settings = new GeneralSettings();

        foreach (var value in section.EnumerateObject()) {
            var property = Array.Find(GeneralProperties,
                candidate => string.Equals(candidate.Name, value.Name, StringComparison.OrdinalIgnoreCase));

            if (property is null) {
                Log.Debug("sic.json: ignoring unknown setting {Name}", value.Name);
                continue;
            }

            object? parsed;

            try {
                parsed = value.Value.Deserialize(property.PropertyType, Options);
            } catch (JsonException ex) {
                Log.Warning("sic.json: invalid value for {Name}, keeping the default: {Error}", value.Name, ex.Message);
                badValues++;
                continue;
            }

            if (property.Name == nameof(GeneralSettings.Language)
                && parsed is string language
                && !Localization.IsValidLanguageName(language)) {
                Log.Warning("sic.json: invalid language {Language}, keeping the default", language);
                badValues++;
                continue;
            }

            // The enum converter ORs a list such as "Weekly, Never" into a value no member has.
            if (property.PropertyType.IsEnum && (parsed is null || !Enum.IsDefined(property.PropertyType, parsed))) {
                Log.Warning("sic.json: invalid value {Value} for {Name}, keeping the default",
                    value.Value.GetRawText(), value.Name);
                badValues++;
                continue;
            }

            property.SetValue(settings, parsed);
        }

        return settings;
    }

    /// <summary>
    /// The clean-up every loaded config goes through: nulls become defaults, null and blank format
    /// keys are dropped, a blank output folder becomes the default one, an invalid language
    /// becomes <c>System</c>, an update interval no member has becomes the default, and a null
    /// preset list becomes an empty one.
    /// </summary>
    private static void CleanUp(AppConfig config) {
        config.General ??= new GeneralSettings();
        config.Presets ??= [];
        var general = config.General;
        var defaults = new GeneralSettings();

        if (!Localization.IsValidLanguageName(general.Language)) {
            general.Language = defaults.Language;
        }

        if (string.IsNullOrWhiteSpace(general.OutputFolder)) {
            general.OutputFolder = App.DefaultOutputFolder;
        }

        if (!Enum.IsDefined(general.UpdateCheckInterval)) {
            general.UpdateCheckInterval = defaults.UpdateCheckInterval;
        }

        general.LastInputFolder ??= defaults.LastInputFolder;
        general.EnabledFormats = general.EnabledFormats is null
            ? []
            : [.. general.EnabledFormats.Where(key => !string.IsNullOrWhiteSpace(key))];
    }

    /// <summary>
    /// Saves and returns the translated error message, or <c>null</c> when the save worked. Besides
    /// I/O failures it catches a value the serializer can't write, so one bad setting never takes
    /// down startup or a save.
    /// </summary>
    internal static string? TrySave(string path, AppConfig config) {
        try {
            Save(path, config);
            return null;
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException) {
            Log.Error(ex, "Failed to save {Path}", path);
            return SaveErrorMessage(ex);
        }
    }

    /// <summary>
    /// Creates <paramref name="path"/> unless it exists, for migration and first run. A failed
    /// write is reported in <paramref name="saveError"/>, as <see cref="TrySave"/> does.
    /// </summary>
    /// <returns><c>false</c> only when <paramref name="path"/> already existed and was left alone.</returns>
    private static bool TryCreate(string path, AppConfig config, out string? saveError) {
        saveError = null;

        try {
            return Write(path, config, overwrite: false);
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException) {
            Log.Error(ex, "Failed to save {Path}", path);
            saveError = SaveErrorMessage(ex);
            return true;
        }
    }

    private static string SaveErrorMessage(Exception ex) => _("Unable to save configuration: {0}", ex.Message);

    private static ConfigLoadResult Blocked(string loadError) {
        var config = new AppConfig();
        CleanUp(config);
        return new ConfigLoadResult(config, loadError, null, SavesBlocked: true);
    }

    /// <summary>
    /// Reads the file, retrying briefly like <see cref="MoveWithRetry"/>: a parallel CLI run that
    /// is replacing it, or an antivirus scanner, may hold it for a moment. A file that doesn't
    /// exist fails at once with <see cref="FileNotFoundException"/>.
    /// </summary>
    private static byte[] ReadSharedWithRetry(string path) {
        for (var attempt = 1; ; attempt++) {
            try {
                return ReadShared(path);
            } catch (Exception ex) when (attempt < RetryAttempts
                && ex is not FileNotFoundException
                && ex is IOException or UnauthorizedAccessException) {
                Thread.Sleep(RetryDelay);
            }
        }
    }

    /// <summary>Reads the whole file while letting others read, write or delete it.</summary>
    private static byte[] ReadShared(string path) {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static ReadOnlySpan<byte> SkipBom(byte[] bytes) =>
        bytes.AsSpan().StartsWith(Utf8Bom) ? bytes.AsSpan(Utf8Bom.Length) : bytes;

    /// <summary>
    /// Moves <paramref name="source"/> to <paramref name="destination"/>, retrying briefly: a
    /// parallel CLI run or an antivirus scanner may have it open for a moment. Without
    /// <paramref name="overwrite"/> the move refuses an existing file in one step, so two
    /// processes can't both create it.
    /// </summary>
    /// <returns><c>false</c> when <paramref name="overwrite"/> is off and the destination exists.</returns>
    private static bool MoveWithRetry(string source, string destination, bool overwrite) {
        for (var attempt = 1; ; attempt++) {
            try {
                File.Move(source, destination, overwrite);
                return true;
            } catch (IOException) when (!overwrite && File.Exists(destination)) {
                return false;
            } catch (Exception ex) when (attempt < RetryAttempts && ex is IOException or UnauthorizedAccessException) {
                Thread.Sleep(RetryDelay);
            }
        }
    }

    private static void TryDelete(string path) {
        try {
            File.Delete(path);
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Log.Warning(ex, "Failed to delete the temporary file {Path}", path);
        }
    }

    private static string PropertyName(string name) => Options.PropertyNamingPolicy?.ConvertName(name) ?? name;

    /// <summary>Whether <paramref name="folder"/> is some portable copy's <c>userdata\Converted</c> folder.</summary>
    private static bool IsPortableDefaultOutputFolder(string? folder) =>
        !string.IsNullOrWhiteSpace(folder)
        && App.TrimSeparators(folder).EndsWith(@"\userdata\Converted", StringComparison.OrdinalIgnoreCase);
}
