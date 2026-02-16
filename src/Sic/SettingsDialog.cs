using Oire.Sic.Utils;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic;

public partial class SettingsDialog : Form
{
    public SettingsDialog()
    {
        InitializeComponent();
        LoadSettings();
        browseButton.Click += BrowseButton_Click;
        clearOutputFolderButton.Click += ClearOutputFolderButton_Click;
        okButton.Click += OkButton_Click;
    }

    private void LoadSettings()
    {
        outputFolderTextBox.Text = Config.General.OutputFolder;

        languageComboBox.Items.Add(App.SystemLanguageName);
        languageComboBox.Items.Add("en-US");

        var currentLang = Config.General.Language;
        var index = languageComboBox.Items.IndexOf(currentLang);
        languageComboBox.SelectedIndex = index >= 0 ? index : 0;
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select output folder for converted images",
            UseDescriptionForTitle = true,
        };

        if (!string.IsNullOrWhiteSpace(outputFolderTextBox.Text))
        {
            dialog.InitialDirectory = outputFolderTextBox.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            outputFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void ClearOutputFolderButton_Click(object? sender, EventArgs e)
    {
        outputFolderTextBox.Text = "";
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        Config.General.OutputFolder = outputFolderTextBox.Text;
        Config.General.Language = languageComboBox.SelectedItem as string ?? App.SystemLanguageName;
        Config.Save();
    }
}
