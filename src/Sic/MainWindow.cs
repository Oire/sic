using System.Drawing.Imaging;
using Oire.Sic.Models;
using Oire.Sic.Utils;
using Serilog;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic;

public partial class MainWindow: Form {
    private readonly List<ImageItem> _imageItems = [];

    public MainWindow() {
        InitializeComponent();
        SetupEventHandlers();
        PopulateFormatComboBox();
        UpdateMenuState();
    }

    private void SetupEventHandlers() {
        // File menu
        addImageMenuItem.Click += AddImageMenuItem_Click;
        addFolderMenuItem.Click += AddFolderMenuItem_Click;
        addFromUrlMenuItem.Click += AddFromUrlMenuItem_Click;
        removeMenuItem.Click += RemoveMenuItem_Click;
        removeAllMenuItem.Click += RemoveAllMenuItem_Click;
        optionsMenuItem.Click += OptionsMenuItem_Click;
        exitMenuItem.Click += ExitMenuItem_Click;

        // Help menu
        userGuideMenuItem.Click += UserGuideMenuItem_Click;
        aboutMenuItem.Click += AboutMenuItem_Click;

        // Controls
        convertButton.Click += ConvertButton_Click;
        resizeCheckBox.CheckedChanged += ResizeCheckBox_CheckedChanged;
        imageListView.SelectedIndexChanged += ImageListView_SelectedIndexChanged;
        imageListView.DragEnter += ImageListView_DragEnter;
        imageListView.DragDrop += ImageListView_DragDrop;

        // Keyboard
        KeyPreview = true;
        KeyDown += MainWindow_KeyDown;
    }

    private void PopulateFormatComboBox() {
        foreach (var format in ImageConverter.GetSupportedFormats()) {
            formatComboBox.Items.Add(format);
        }

        if (formatComboBox.Items.Count > 0) {
            formatComboBox.SelectedIndex = 0;
        }
    }

    private void UpdateMenuState() {
        var hasItems = _imageItems.Count > 0;
        var hasSelection = imageListView.SelectedIndices.Count > 0;

        removeMenuItem.Enabled = hasSelection;
        removeAllMenuItem.Enabled = hasItems;
        convertButton.Enabled = hasItems;
    }

    // --- File menu handlers ---

    private void AddImageMenuItem_Click(object? sender, EventArgs e) {
        using var dialog = new OpenFileDialog {
            Title = "Select images to add",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.tif;*.webp;*.ico;*.avif|All files|*.*",
            Multiselect = true,
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        imageListView.BeginUpdate();
        try {
            foreach (var file in dialog.FileNames) {
                AddImageFromFile(file);
            }
        } finally {
            imageListView.EndUpdate();
        }
        UpdateMenuState();
    }

    private void AddFolderMenuItem_Click(object? sender, EventArgs e) {
        using var dialog = new AddFolderDialog();

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var searchOption = dialog.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var extensions = dialog.SelectedExtensions;
        var folder = dialog.SelectedFolder;
        var addedCount = 0;

        imageListView.BeginUpdate();
        try {
            foreach (var extension in extensions) {
                var pattern = extension; // e.g. "*.jpg"
                foreach (var file in Directory.EnumerateFiles(folder, pattern, searchOption)) {
                    AddImageFromFile(file);
                    addedCount++;
                }
            }
        } finally {
            imageListView.EndUpdate();
        }
        UpdateMenuState();

        if (addedCount == 0) {
            MessageBox.Show("No matching images found in the selected folder.", "No images found", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void AddFromUrlMenuItem_Click(object? sender, EventArgs e) {
        using var inputDialog = new Form {
            Text = "Add Image from URL",
            Size = new Size(500, 150),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
        };

        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var urlLabel = new Label {
            Text = "Enter image URL:",
            AutoSize = true,
            AccessibleName = "Image URL label",
        };

        var urlTextBox = new TextBox {
            Dock = DockStyle.Fill,
            AccessibleName = "Image URL",
        };

        var buttonPanel = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
        };

        var cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        var okBtn = new Button { Text = "OK", DialogResult = DialogResult.OK };
        buttonPanel.Controls.Add(cancelBtn);
        buttonPanel.Controls.Add(okBtn);

        inputDialog.AcceptButton = okBtn;
        inputDialog.CancelButton = cancelBtn;

        layout.Controls.Add(urlLabel, 0, 0);
        layout.Controls.Add(urlTextBox, 0, 1);
        layout.Controls.Add(buttonPanel, 0, 2);
        inputDialog.Controls.Add(layout);

        if (inputDialog.ShowDialog() != DialogResult.OK)
            return;

        var url = urlTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(url))
            return;

        statusLabel.Text = "Downloading image...";
        try {
            var item = await ImageConverter.LoadFromUrl(url);
            AddImageItem(item);
            UpdateMenuState();
            statusLabel.Text = $"Added {item.FileName} from URL";
        } catch (Exception ex) {
            Log.Error("Failed to load image from URL {Url}: {Error}", url, ex.Message);
            MessageBox.Show($"Failed to load image from URL:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "Ready";
        }
    }

    private void RemoveMenuItem_Click(object? sender, EventArgs e) {
        if (imageListView.SelectedIndices.Count == 0)
            return;

        var index = imageListView.SelectedIndices[0];
        _imageItems.RemoveAt(index);
        imageListView.Items.RemoveAt(index);

        previewPictureBox.Image?.Dispose();
        previewPictureBox.Image = null;
        statusLabel.Text = $"{_imageItems.Count} image(s) in queue";
        UpdateMenuState();
    }

    private void RemoveAllMenuItem_Click(object? sender, EventArgs e) {
        _imageItems.Clear();
        imageListView.Items.Clear();
        previewPictureBox.Image?.Dispose();
        previewPictureBox.Image = null;
        statusLabel.Text = "Ready";
        UpdateMenuState();
    }

    private void OptionsMenuItem_Click(object? sender, EventArgs e) {
        using var dialog = new SettingsDialog();
        dialog.ShowDialog(this);
    }

    private void ExitMenuItem_Click(object? sender, EventArgs e) {
        Close();
    }

    // --- Convert button handler ---

    private async void ConvertButton_Click(object? sender, EventArgs e) {
        if (_imageItems.Count == 0) {
            MessageBox.Show("No images to convert. Add some images first.", "Nothing to convert", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (formatComboBox.SelectedItem is not string targetFormat) {
            MessageBox.Show("Please select a target format.", "No format selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int? width = null, height = null;
        if (resizeCheckBox.Checked) {
            width = (int)widthNumeric.Value;
            height = (int)heightNumeric.Value;
        }

        var outputFolder = string.IsNullOrWhiteSpace(Config.General.OutputFolder) ? null : Config.General.OutputFolder;

        progressBar.Visible = true;
        progressBar.Maximum = _imageItems.Count;
        progressBar.Value = 0;
        convertButton.Enabled = false;

        var totalCount = _imageItems.Count;
        var converted = 0;
        var skipped = 0;
        var convertedIndices = new List<int>();

        await Task.Run(() => {
            for (var i = 0; i < totalCount; i++) {
                var item = _imageItems[i];

                Invoke(() => {
                    imageListView.Items[i].SubItems[4].Text = "Converting...";
                });

                var outputPath = ImageConverter.GenerateOutputPath(item, targetFormat, outputFolder);

                if (File.Exists(outputPath)) {
                    var resolution = ResolveFileConflict(outputPath);

                    switch (resolution) {
                        case ConflictResolution.Overwrite:
                            break;
                        case ConflictResolution.Rename:
                            outputPath = ImageConverter.GetConflictRenamePath(outputPath);
                            break;
                        case ConflictResolution.Skip:
                            skipped++;
                            Invoke(() => {
                                progressBar.Value = i + 1;
                                statusLabel.Text = $"Skipped {item.FileName}";
                                imageListView.Items[i].SubItems[4].Text = "Skipped";
                            });
                            continue;
                    }
                }

                try {
                    var dir = Path.GetDirectoryName(outputPath);
                    if (dir != null && !Directory.Exists(dir)) {
                        Directory.CreateDirectory(dir);
                    }

                    ImageConverter.Convert(item, targetFormat, outputPath, width, height);
                    converted++;
                    convertedIndices.Add(i);

                    Invoke(() => {
                        progressBar.Value = i + 1;
                        statusLabel.Text = $"Converted {item.FileName} ({i + 1}/{totalCount})";
                    });
                } catch (Exception ex) {
                    Log.Error("Failed to convert {FileName}: {Error}", item.FileName, ex.Message);
                    Invoke(() => {
                        imageListView.Items[i].SubItems[4].Text = "Failed";
                        MessageBox.Show($"Failed to convert {item.FileName}:\n{ex.Message}", "Conversion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            }
        });

        // Remove successfully converted items from queue (in reverse order to preserve indices)
        imageListView.BeginUpdate();
        try {
            foreach (var index in convertedIndices.OrderByDescending(i => i)) {
                _imageItems.RemoveAt(index);
                imageListView.Items.RemoveAt(index);
            }
        } finally {
            imageListView.EndUpdate();
        }

        previewPictureBox.Image?.Dispose();
        previewPictureBox.Image = null;
        convertButton.Enabled = true;
        progressBar.Visible = false;
        statusLabel.Text = $"Done! Converted {converted} image(s), skipped {skipped}.";
        UpdateMenuState();
    }

    // --- Help menu handlers ---

    private void UserGuideMenuItem_Click(object? sender, EventArgs e) {
        MessageBox.Show("The user guide is coming soon.", "User Guide", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void AboutMenuItem_Click(object? sender, EventArgs e) {
        using var dialog = new AboutDialog();
        dialog.ShowDialog(this);
    }

    // --- Controls and list handlers ---

    private void ResizeCheckBox_CheckedChanged(object? sender, EventArgs e) {
        var enabled = resizeCheckBox.Checked;
        widthLabel.Enabled = enabled;
        widthNumeric.Enabled = enabled;
        dimensionSeparatorLabel.Enabled = enabled;
        heightLabel.Enabled = enabled;
        heightNumeric.Enabled = enabled;
    }

    private void ImageListView_SelectedIndexChanged(object? sender, EventArgs e) {
        UpdateMenuState();

        if (imageListView.SelectedIndices.Count == 0) {
            previewPictureBox.Image?.Dispose();
            previewPictureBox.Image = null;
            return;
        }

        var index = imageListView.SelectedIndices[0];
        var item = _imageItems[index];

        try {
            var preview = ImageConverter.GeneratePreview(item, previewPictureBox.Width, previewPictureBox.Height);
            previewPictureBox.Image?.Dispose();
            previewPictureBox.Image = preview;
        } catch (Exception ex) {
            Log.Warning("Failed to generate preview for {FileName}: {Error}", item.FileName, ex.Message);
            previewPictureBox.Image?.Dispose();
            previewPictureBox.Image = null;
        }
    }

    private void ImageListView_DragEnter(object? sender, DragEventArgs e) {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void ImageListView_DragDrop(object? sender, DragEventArgs e) {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files)
            return;

        imageListView.BeginUpdate();
        try {
            foreach (var file in files) {
                AddImageFromFile(file);
            }
        } finally {
            imageListView.EndUpdate();
        }
        UpdateMenuState();
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e) {
        if (e.Control && e.KeyCode == Keys.V) {
            HandlePaste();
            e.Handled = true;
        }
    }

    private void HandlePaste() {
        if (Clipboard.ContainsFileDropList()) {
            var files = Clipboard.GetFileDropList();
            imageListView.BeginUpdate();
            try {
                foreach (var file in files) {
                    if (file != null)
                        AddImageFromFile(file);
                }
            } finally {
                imageListView.EndUpdate();
            }
            UpdateMenuState();
        } else if (Clipboard.ContainsImage()) {
            var clipImage = Clipboard.GetImage();
            if (clipImage == null)
                return;

            using var ms = new MemoryStream();
            clipImage.Save(ms, ImageFormat.Png);
            var data = ms.ToArray();

            try {
                var item = ImageConverter.LoadFromBytes(data, "clipboard_image.png");
                AddImageItem(item);
                UpdateMenuState();
                statusLabel.Text = "Added image from clipboard";
            } catch (Exception ex) {
                Log.Error("Failed to load clipboard image: {Error}", ex.Message);
                MessageBox.Show($"Failed to load clipboard image:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // --- Shared helpers ---

    private void AddImageFromFile(string path) {
        try {
            var item = ImageConverter.LoadFromFile(path);
            AddImageItem(item);
        } catch (Exception ex) {
            Log.Error("Failed to load image {Path}: {Error}", path, ex.Message);
            MessageBox.Show($"Failed to load image:\n{path}\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddImageItem(ImageItem item) {
        _imageItems.Add(item);

        var listItem = new ListViewItem(item.FileName);
        listItem.SubItems.Add(item.OriginalFormat);
        listItem.SubItems.Add(item.GetDimensionsDisplay());
        listItem.SubItems.Add(item.GetSizeDisplay());
        listItem.SubItems.Add(""); // Status column — blank on add

        imageListView.Items.Add(listItem);
        statusLabel.Text = $"{_imageItems.Count} image(s) in queue";
    }

    private ConflictResolution ResolveFileConflict(string outputPath) {
        var result = ConflictResolution.Skip;
        var fileName = Path.GetFileName(outputPath);

        Invoke(() => {
            using var dialog = new Form {
                Text = "File Already Exists",
                Size = new Size(450, 180),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
            };

            var layout = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(8),
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var messageLabel = new Label {
                Text = $"The file \"{fileName}\" already exists.\nWhat would you like to do?",
                AutoSize = true,
                AccessibleName = $"File {fileName} already exists",
            };

            var buttonPanel = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
            };

            var overwriteBtn = new Button { Text = "Overwrite", AccessibleName = "Overwrite existing file" };
            var renameBtn = new Button { Text = "Rename", AccessibleName = "Save with different name" };
            var skipBtn = new Button { Text = "Skip", AccessibleName = "Skip this file" };

            overwriteBtn.Click += (_, _) => { result = ConflictResolution.Overwrite; dialog.Close(); };
            renameBtn.Click += (_, _) => { result = ConflictResolution.Rename; dialog.Close(); };
            skipBtn.Click += (_, _) => { result = ConflictResolution.Skip; dialog.Close(); };

            buttonPanel.Controls.Add(overwriteBtn);
            buttonPanel.Controls.Add(renameBtn);
            buttonPanel.Controls.Add(skipBtn);

            layout.Controls.Add(messageLabel, 0, 0);
            layout.Controls.Add(buttonPanel, 0, 1);
            dialog.Controls.Add(layout);
            dialog.AcceptButton = overwriteBtn;
            dialog.CancelButton = skipBtn;

            dialog.ShowDialog(this);
        });

        return result;
    }

    private enum ConflictResolution {
        Overwrite,
        Rename,
        Skip,
    }
}
