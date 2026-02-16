using System;

namespace Oire.Sic.Utils.Constants;

public static class Logging {
    public static readonly string LogFolder = Path.Combine(App.DataFolder, "logs");
    public static readonly string LogFileExtension = "log";
    public static readonly string GenericFile = Path.Combine(Logging.LogFolder, String.Format("{0}.{1}", App.Name, Logging.LogFileExtension));
    public static readonly string GenericFileShort = Path.Combine(Logging.LogFolder, String.Format("{0}-short.{1}", App.Name, Logging.LogFileExtension));
    public static readonly string ErrorsFile = Path.Combine(Logging.LogFolder, String.Format("errors.{0}", Logging.LogFileExtension));
    public static readonly string ErrorsFileShort = Path.Combine(Logging.LogFolder, String.Format("errors-short.{0}", Logging.LogFileExtension));
    public static readonly string TelemetryFile = Path.Combine(Logging.LogFolder, "analysis.json");
    public static readonly string OutputTemplateShort = "[{Level:u4}] {Message:lj}{NewLine}{Exception}";
}
