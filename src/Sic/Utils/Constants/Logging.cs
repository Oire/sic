namespace Oire.Sic.Utils.Constants;

public static class Logging {
    public static readonly string LogFolder = Path.Combine(App.DataFolder, "logs");
    public const string LogFileExtension = "log";
    public static readonly string GenericFile = Path.Combine(LogFolder, $"{App.Name}.{LogFileExtension}");
    public static readonly string GenericFileShort = Path.Combine(LogFolder, $"{App.Name}-short.{LogFileExtension}");
    public static readonly string ErrorsFile = Path.Combine(LogFolder, $"errors.{LogFileExtension}");
    public static readonly string ErrorsFileShort = Path.Combine(LogFolder, $"errors-short.{LogFileExtension}");
    public static readonly string TelemetryFile = Path.Combine(LogFolder, "analysis.json");
    public const string OutputTemplateShort = "[{Level:u4}] {Message:lj}{NewLine}{Exception}";
}
