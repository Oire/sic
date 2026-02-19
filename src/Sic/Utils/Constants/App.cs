using System;

namespace Oire.Sic.Utils.Constants;

public static class App {
    public static readonly string Name = "Sic";
    public static readonly string ManufacturerNameShort = "Oire";
    public static readonly string ManufacturerNameFull = "Oire Software";
    public static readonly string ConfigFileExtension = "cfg";
    public static readonly string DatabaseFileExtension = "oidb";
    public static readonly bool IsPortable = Directory.Exists(Path.Combine(AppContext.BaseDirectory, "userdata"));
    public static readonly string DataFolder = IsPortable
        ? Path.Combine(AppContext.BaseDirectory, "userdata")
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ManufacturerNameShort,
            Name
        );
    public static readonly string SystemLanguageName = "System";
    public static readonly string LocalesFolder = "./locale";
    public static readonly string DatabaseName = Path.Combine(App.DataFolder, String.Format("{0}.{1}", App.Name, App.DatabaseFileExtension));
}
