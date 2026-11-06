namespace Oire.Sic.Utils.Constants;

public static class App {
    public const string Name = "Sic";
    public const string ManufacturerNameShort = "Oire";
    public const string ManufacturerNameFull = "Oire Software";
    public const string ConfigFileName = "sic.json";
    public const string LegacyConfigFileName = "Sic.cfg";
    public static readonly bool IsPortable = Directory.Exists(Path.Combine(AppContext.BaseDirectory, "userdata"));
    public static readonly string DataFolder = IsPortable
        ? Path.Combine(AppContext.BaseDirectory, "userdata")
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ManufacturerNameShort,
            Name
        );
    public static readonly string DefaultOutputFolder = Path.Combine(DataFolder, "Converted");
    public const string RepoUrl = "https://github.com/Oire/sic";
    public const string AppcastUrl = "https://sic.oire.dev/appcast.xml";
    public const string UpdatePublicKey = "1Q9hfqwf3i6ZcncHvt08rqAO17iDrhHTvrjHAdCXw68=";
    public const string SystemLanguageName = "System";
    public static readonly string LocalesFolder = Path.Combine(AppContext.BaseDirectory, "locale");
    public static readonly string HelpFolder = Path.Combine(AppContext.BaseDirectory, "help");

    /// <summary>Whether <paramref name="folder"/> is <see cref="DefaultOutputFolder"/>, ignoring case and trailing separators.</summary>
    public static bool IsDefaultOutputFolder(string? folder) =>
        !string.IsNullOrWhiteSpace(folder)
        && string.Equals(TrimSeparators(folder), TrimSeparators(DefaultOutputFolder), StringComparison.OrdinalIgnoreCase);

    /// <summary><paramref name="folder"/> with forward slashes made backslashes and trailing ones removed.</summary>
    public static string TrimSeparators(string folder) => folder.Replace('/', '\\').TrimEnd('\\');
}
