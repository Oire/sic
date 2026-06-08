using System.Globalization;
using GetText.WindowsForms;
using Oire.Sic.Utils;
using Oire.Sic.Utils.Enums;
using Serilog;
using static Oire.Sic.Utils.Localization;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic;

public partial class SettingsDialog: Form {
    private readonly Dictionary<string, string> _languageMap = new();

    /// <summary>Set when the background update-frequency preference changed; MainWindow starts,
    /// stops, or re-times the background update loop to match without waiting for a restart. The
    /// "on startup" preference has no live effect — it's read only at the next launch.</summary>
    public bool UpdatePeriodicCheckChanged { get; private set; }

    public SettingsDialog() {
        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog);
        PopulateUpdateIntervals();
        LoadSettings();
        browseButton.Click += BrowseButton_Click;
        clearOutputFolderButton.Click += ClearOutputFolderButton_Click;
        okButton.Click += OkButton_Click;
        outputFolderTextBox.TextChanged += (_, _) => UpdateClearButtonState();
        UpdateClearButtonState();
    }

    private void LoadSettings() {
        outputFolderTextBox.Text = Config.General.OutputFolder;
        confirmExitCheckBox.Checked = Config.General.ConfirmExitWithQueue;
        checkUpdatesOnStartupCheckBox.Checked = Config.General.CheckForUpdatesOnStartup;
        SelectUpdateInterval(Config.General.UpdateCheckInterval);

        // "System" always first — uses the OS language
        var systemDisplayName = _("System");
        _languageMap[systemDisplayName] = App.SystemLanguageName;
        languageComboBox.Items.Add(systemDisplayName);

        // English always available (it's the source language)
        var enCulture = new CultureInfo("en-US");
        var enDisplayName = enCulture.TextInfo.ToTitleCase(enCulture.NativeName);
        _languageMap[enDisplayName] = "en-US";
        languageComboBox.Items.Add(enDisplayName);

        // Auto-detect available translations from .mo files in locale directory
        if (Directory.Exists(App.LocalesFolder)) {
            var catalogFile = $"{App.Name}.mo";
            var detectedLanguages = new SortedDictionary<string, string>();

            foreach (var dir in Directory.GetDirectories(App.LocalesFolder)) {
                var dirName = Path.GetFileName(dir);
                if (dirName is "scripts" or "en-US")
                    continue;
                if (!File.Exists(Path.Combine(dir, catalogFile)))
                    continue;

                try {
                    var culture = new CultureInfo(dirName);
                    var displayName = culture.TextInfo.ToTitleCase(culture.NativeName);
                    detectedLanguages[displayName] = dirName;
                } catch (CultureNotFoundException) {
                    Log.Debug("Settings: Skipping locale directory with invalid culture name: {DirName}", dirName);
                }
            }

            foreach (var (displayName, code) in detectedLanguages) {
                _languageMap[displayName] = code;
                languageComboBox.Items.Add(displayName);
            }
        }

        // Select current language
        var currentLang = Config.General.Language;
        var selectedDisplay = _languageMap.FirstOrDefault(kvp => kvp.Value == currentLang).Key;
        var index = selectedDisplay != null ? languageComboBox.Items.IndexOf(selectedDisplay) : -1;
        languageComboBox.SelectedIndex = index >= 0 ? index : 0;
    }

    /// <summary>
    /// Fills the background-update-frequency combo. Order matches
    /// <see cref="UpdateCheckInterval"/>'s declaration (most frequent first, "Never" last).
    /// Labels are localized at runtime so they translate with the rest of the dialog.
    /// </summary>
    private void PopulateUpdateIntervals() {
        updateIntervalComboBox.Items.Add(new UpdateIntervalOption(UpdateCheckInterval.Daily, _("Once a day")));
        updateIntervalComboBox.Items.Add(new UpdateIntervalOption(UpdateCheckInterval.EveryThreeDays, _("Every 3 days")));
        updateIntervalComboBox.Items.Add(new UpdateIntervalOption(UpdateCheckInterval.Weekly, _("Once a week")));
        updateIntervalComboBox.Items.Add(new UpdateIntervalOption(UpdateCheckInterval.Monthly, _("Once a month")));
        updateIntervalComboBox.Items.Add(new UpdateIntervalOption(UpdateCheckInterval.Never, _("Never")));
    }

    private void SelectUpdateInterval(UpdateCheckInterval interval) {
        for (var i = 0; i < updateIntervalComboBox.Items.Count; i++) {
            if (updateIntervalComboBox.Items[i] is UpdateIntervalOption opt && opt.Interval == interval) {
                updateIntervalComboBox.SelectedIndex = i;
                return;
            }
        }
        updateIntervalComboBox.SelectedIndex = 0; // fall back to Daily
    }

    private UpdateCheckInterval SelectedUpdateInterval() =>
        (updateIntervalComboBox.SelectedItem as UpdateIntervalOption)?.Interval ?? UpdateCheckInterval.Daily;

    private void BrowseButton_Click(object? sender, EventArgs e) {
        using var dialog = new FolderBrowserDialog {
            Description = _("Select output folder for converted images"),
            UseDescriptionForTitle = true,
        };

        if (!string.IsNullOrWhiteSpace(outputFolderTextBox.Text)) {
            dialog.InitialDirectory = outputFolderTextBox.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK) {
            outputFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void ClearOutputFolderButton_Click(object? sender, EventArgs e) {
        outputFolderTextBox.Text = App.DefaultOutputFolder;
    }

    private void UpdateClearButtonState() {
        clearOutputFolderButton.Enabled = outputFolderTextBox.Text != App.DefaultOutputFolder;
    }

    private void OkButton_Click(object? sender, EventArgs e) {
        var folder = outputFolderTextBox.Text;

        if (!string.IsNullOrWhiteSpace(folder)
            && folder != App.DefaultOutputFolder
            && !Directory.Exists(folder)) {
            Log.Warning("Settings: Selected output folder does not exist: {Folder}", folder);
            MessageBox.Show(
                _("The selected folder does not exist. Please choose an existing folder."),
                _("Folder Not Found"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        var oldUpdateInterval = Config.General.UpdateCheckInterval;

        Config.General.OutputFolder = folder;
        Config.General.ConfirmExitWithQueue = confirmExitCheckBox.Checked;
        Config.General.CheckForUpdatesOnStartup = checkUpdatesOnStartupCheckBox.Checked;
        Config.General.UpdateCheckInterval = SelectedUpdateInterval();
        var selectedDisplay = languageComboBox.SelectedItem as string;
        Config.General.Language = selectedDisplay != null && _languageMap.TryGetValue(selectedDisplay, out var code)
            ? code
            : App.SystemLanguageName;

        UpdatePeriodicCheckChanged = Config.General.UpdateCheckInterval != oldUpdateInterval;

        Config.Save();
        Localization.SetLanguage(Config.General.Language);
    }

    /// <summary>Combo item pairing an <see cref="UpdateCheckInterval"/> with its localized label.</summary>
    private sealed class UpdateIntervalOption {
        public UpdateCheckInterval Interval { get; }
        private string Display { get; }

        public UpdateIntervalOption(UpdateCheckInterval interval, string display) {
            Interval = interval;
            Display = display;
        }

        public override string ToString() => Display;
    }
}
