using System.Globalization;
using Oire.Sic.Utils;
using static Oire.Sic.Utils.Localization;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic;

public partial class SettingsDialog: Form {
    private readonly Dictionary<string, string> _languageMap = new();

    public SettingsDialog() {
        InitializeComponent();
        LoadSettings();
        browseButton.Click += BrowseButton_Click;
        clearOutputFolderButton.Click += ClearOutputFolderButton_Click;
        okButton.Click += OkButton_Click;
        outputFolderTextBox.TextChanged += (_, _) => UpdateClearButtonState();
        UpdateClearButtonState();
    }

    private void LoadSettings() {
        outputFolderTextBox.Text = Config.General.OutputFolder;

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
                    // Skip directories with invalid culture names
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
        outputFolderTextBox.Text = "";
    }

    private void UpdateClearButtonState() {
        clearOutputFolderButton.Enabled = !string.IsNullOrEmpty(outputFolderTextBox.Text);
    }

    private void OkButton_Click(object? sender, EventArgs e) {
        Config.General.OutputFolder = outputFolderTextBox.Text;
        var selectedDisplay = languageComboBox.SelectedItem as string;
        Config.General.Language = selectedDisplay != null && _languageMap.TryGetValue(selectedDisplay, out var code)
            ? code
            : App.SystemLanguageName;
        Config.Save();
        Localization.SetLanguage(Config.General.Language);
    }
}
