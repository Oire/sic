using Oire.Sic.Utils;

namespace Oire.Sic;

public partial class AddFolderDialog: Form {
    private static readonly (string Label, string[] Extensions)[] Filters = [
        ("All supported images", ["*.jpg", "*.jpeg", "*.png", "*.webp", "*.ico", "*.bmp", "*.tif", "*.tiff", "*.gif", "*.avif"]),
        ("JPEG images (*.jpg, *.jpeg)", ["*.jpg", "*.jpeg"]),
        ("PNG images (*.png)", ["*.png"]),
        ("WebP images (*.webp)", ["*.webp"]),
        ("ICO icons (*.ico)", ["*.ico"]),
        ("BMP images (*.bmp)", ["*.bmp"]),
        ("TIFF images (*.tif, *.tiff)", ["*.tif", "*.tiff"]),
        ("GIF images (*.gif)", ["*.gif"]),
        ("AVIF images (*.avif)", ["*.avif"]),
    ];

    public string SelectedFolder => folderTextBox.Text;
    public string[] SelectedExtensions => Filters[filterComboBox.SelectedIndex].Extensions;
    public bool IncludeSubfolders => includeSubfoldersCheckBox.Checked;

    public AddFolderDialog() {
        InitializeComponent();

        foreach (var (label, _) in Filters) {
            filterComboBox.Items.Add(label);
        }
        filterComboBox.SelectedIndex = 0;

        folderTextBox.Text = Config.General.LastInputFolder;

        browseButton.Click += BrowseButton_Click;
    }

    private void BrowseButton_Click(object? sender, EventArgs e) {
        using var dialog = new FolderBrowserDialog {
            Description = "Select folder containing images",
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
                MessageBox.Show("Please select a folder.", "No folder selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            Config.General.LastInputFolder = folderTextBox.Text;
            Config.Save();
        }
    }
}
