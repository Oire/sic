using GetText.WindowsForms;
using Oire.Sic.Utils;
using Serilog;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

public partial class AddFolderDialog: Form {
    private static (string Label, string[] Extensions)[] GetFilters() => [
        (_("All supported images"), ["*.jpg", "*.jpeg", "*.png", "*.webp", "*.ico", "*.bmp", "*.tif", "*.tiff", "*.gif", "*.avif"]),
        (_("JPEG images (*.jpg, *.jpeg)"), ["*.jpg", "*.jpeg"]),
        (_("PNG images (*.png)"), ["*.png"]),
        (_("WebP images (*.webp)"), ["*.webp"]),
        (_("ICO icons (*.ico)"), ["*.ico"]),
        (_("BMP images (*.bmp)"), ["*.bmp"]),
        (_("TIFF images (*.tif, *.tiff)"), ["*.tif", "*.tiff"]),
        (_("GIF images (*.gif)"), ["*.gif"]),
        (_("AVIF images (*.avif)"), ["*.avif"]),
    ];

    private readonly (string Label, string[] Extensions)[] _filters = GetFilters();

    public string SelectedFolder => folderTextBox.Text;
    public string[] SelectedExtensions => _filters[filterComboBox.SelectedIndex].Extensions;
    public bool IncludeSubfolders => includeSubfoldersCheckBox.Checked;

    public AddFolderDialog() {
        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog);

        foreach (var (label, _) in _filters) {
            filterComboBox.Items.Add(label);
        }
        filterComboBox.SelectedIndex = 0;

        folderTextBox.Text = Config.General.LastInputFolder;

        browseButton.Click += BrowseButton_Click;
    }

    private void BrowseButton_Click(object? sender, EventArgs e) {
        using var dialog = new FolderBrowserDialog {
            Description = _("Select folder containing images"),
            UseDescriptionForTitle = true,
        };

        if (!string.IsNullOrWhiteSpace(folderTextBox.Text)) {
            dialog.InitialDirectory = folderTextBox.Text;
        }

        if (dialog.ShowDialog() == DialogResult.OK) {
            folderTextBox.Text = dialog.SelectedPath;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e) {
        base.OnFormClosing(e);

        if (DialogResult == DialogResult.OK) {
            if (string.IsNullOrWhiteSpace(folderTextBox.Text)) {
                Log.Debug("AddFolderDialog: No folder selected");
                MessageBox.Show(_("Please select a folder."), _("No folder selected"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            Config.General.LastInputFolder = folderTextBox.Text;
            Config.Save();
        }
    }
}
