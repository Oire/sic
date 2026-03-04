namespace Oire.Sic.Utils.Constants;

public static class App {
    public const string Name = "Sic";
    public const string ManufacturerNameShort = "Oire";
    public const string ManufacturerNameFull = "Oire Software";
    public const string ConfigFileExtension = "cfg";
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
    public const string SystemLanguageName = "System";
    public static readonly string LocalesFolder = Path.Combine(AppContext.BaseDirectory, "locale");
}
