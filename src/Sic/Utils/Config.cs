using SharpConfig;
using Serilog;
using static Oire.Sic.Utils.Localization;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic.Utils;

public class Config {
    private static readonly string ConfigFileName = Path.Combine(App.DataFolder, $"{App.Name}.{App.ConfigFileExtension}");

    public static SectionGeneral General { get; private set; } = new();
    private static Configuration Cfg { get; set; } = new();

    #region Config Section Classes
    public class SectionGeneral {
        public string Language { get; set; } = "System";
        public string OutputFolder { get; set; } = App.DefaultOutputFolder;
        public string LastInputFolder { get; set; } = "";
        public bool ConfirmExitWithQueue { get; set; } = true;
    }

    #endregion

    public static void Load(bool isGui = true) {
        try {
            Cfg = Configuration.LoadFromFile(ConfigFileName);
            General = Cfg["General"].ToObject<SectionGeneral>();

            if (string.IsNullOrWhiteSpace(General.OutputFolder)) {
                General.OutputFolder = App.DefaultOutputFolder;
            }
        } catch (FileNotFoundException) {
            Cfg = new Configuration();
            General = new SectionGeneral();
            Cfg.Add(Section.FromObject("General", General));

            try {
                if (!Directory.Exists(App.DataFolder)) {
                    Directory.CreateDirectory(App.DataFolder);
                }
                Cfg.SaveToFile(ConfigFileName);
            } catch (Exception ex) {
                Log.Error("Failed to save initial config: {Error}", ex.Message);
                ReportError(_("Unable to save configuration: {0}", ex.Message), isGui);
            }
        } catch (Exception ex) {
            Log.Error("Failed to load config: {Error}", ex.Message);
            ReportError(_("Unable to load configuration: {0}", ex.Message), isGui);
        }
    }

    public static void Save() {
        Cfg.Remove("General");
        Cfg.Add(Section.FromObject("General", General));

        try {
            Cfg.SaveToFile(ConfigFileName);
        } catch (Exception ex) {
            Log.Error("Failed to save config: {Error}", ex.Message);
            ReportError(_("Unable to save configuration: {0}", ex.Message), isGui: true);
        }
    }

    private static void ReportError(string message, bool isGui) {
        if (isGui) {
            MessageBox.Show(message, _("Error"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        } else {
            Console.Error.WriteLine(message);
        }
    }
}
