using SharpConfig;
using Serilog;
using Oire.Sic.Utils.Enums;
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

        /// <summary>When <c>true</c>, the app performs a single silent update check shortly
        /// after the main window opens. A check that fails (no network, server down) is
        /// logged and otherwise ignored — it never interrupts startup or shows an error.</summary>
        public bool CheckForUpdatesOnStartup { get; set; } = true;

        /// <summary>When <c>true</c>, SIC! offers (via a prompt) to add usable clipboard
        /// content — raw image data, image files, or an image link — whenever the window opens
        /// or regains focus. Each distinct clipboard payload is offered at most once, so
        /// re-focusing never re-prompts for the same data. Opt-in; off by default.</summary>
        public bool DetectClipboardData { get; set; }

        /// <summary>Comma-separated list of SIC! format keys (e.g. <c>"JPG,PNG,WEBP"</c>) to show
        /// in the target-format dropdown, letting users hide formats they never convert to
        /// (issue #47). An empty value means "show every supported format" — the default — so a
        /// fresh or upgraded config naturally exposes all formats. Parse it via
        /// <see cref="GetEnabledFormatKeys"/>.</summary>
        public string EnabledFormats { get; set; } = "";

        /// <summary>The configured <see cref="EnabledFormats"/> split into individual format keys,
        /// trimmed and with blanks dropped. Empty when no restriction is set (all formats shown).</summary>
        public IEnumerable<string> GetEnabledFormatKeys() =>
            EnabledFormats.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        /// <summary>How often the app checks for updates in the background while it runs.
        /// <see cref="UpdateCheckInterval.Never"/> disables the background loop. Independent of
        /// <see cref="CheckForUpdatesOnStartup"/>: either, both, or neither may be active.</summary>
        public UpdateCheckInterval UpdateCheckInterval { get; set; } = UpdateCheckInterval.Daily;
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
