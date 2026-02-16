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
    }

    private void SetupEventHandlers() {
        addFileButton.Click += AddFileButton_Click;
        addUrlButton.Click += AddUrlButton_Click;
        removeButton.Click += RemoveButton_Click;
        convertButton.Click += ConvertButton_Click;
        settingsButton.Click += SettingsButton_Click;
        resizeCheckBox.CheckedChanged += ResizeCheckBox_CheckedChanged;
        imageListView.SelectedIndexChanged += ImageListView_SelectedIndexChanged;
        imageListView.DragEnter += ImageListView_DragEnter;
        imageListView.DragDrop += ImageListView_DragDrop;
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

    private void AddFileButton_Click(object? sender, EventArgs e) {
        using var dialog = new OpenFileDialog {
            Title = "Select images to add",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.tif;*.webp;*.ico;*.avif|All files|*.*",
            Multiselect = true,
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        foreach (var file in dialog.FileNames) {
            AddImageFromFile(file);
        }
    }

    private async void AddUrlButton_Click(object? sender, EventArgs e) {
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
            statusLabel.Text = $"Added {item.FileName} from URL";
        } catch (Exception ex) {
            Log.Error("Failed to load image from URL {Url}: {Error}", url, ex.Message);
            MessageBox.Show($"Failed to load image from URL:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "Ready";
        }
    }

    private void RemoveButton_Click(object? sender, EventArgs e) {
        if (imageListView.SelectedIndices.Count == 0)
            return;

        for (var i = imageListView.SelectedIndices.Count - 1; i >= 0; i--) {
            var index = imageListView.SelectedIndices[i];
            _imageItems.RemoveAt(index);
            imageListView.Items.RemoveAt(index);
        }

        previewPictureBox.Image?.Dispose();
        previewPictureBox.Image = null;
        statusLabel.Text = $"{_imageItems.Count} image(s) in queue";
    }

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

        var converted = 0;
        var skipped = 0;

        await Task.Run(() => {
            for (var i = 0; i < _imageItems.Count; i++) {
                var item = _imageItems[i];
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

                    Invoke(() => {
                        progressBar.Value = i + 1;
                        statusLabel.Text = $"Converted {item.FileName} ({i + 1}/{_imageItems.Count})";
                    });
                } catch (Exception ex) {
                    Log.Error("Failed to convert {FileName}: {Error}", item.FileName, ex.Message);
                    Invoke(() => {
                        MessageBox.Show($"Failed to convert {item.FileName}:\n{ex.Message}", "Conversion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            }
        });

        convertButton.Enabled = true;
        progressBar.Visible = false;
        statusLabel.Text = $"Done! Converted {converted} image(s), skipped {skipped}.";
    }

    private void SettingsButton_Click(object? sender, EventArgs e) {
        using var dialog = new SettingsDialog();
        dialog.ShowDialog(this);
    }

    private void ResizeCheckBox_CheckedChanged(object? sender, EventArgs e) {
        var enabled = resizeCheckBox.Checked;
        widthLabel.Enabled = enabled;
        widthNumeric.Enabled = enabled;
        dimensionSeparatorLabel.Enabled = enabled;
        heightLabel.Enabled = enabled;
        heightNumeric.Enabled = enabled;
    }

    private void ImageListView_SelectedIndexChanged(object? sender, EventArgs e) {
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

        foreach (var file in files) {
            AddImageFromFile(file);
        }
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e) {
        if (e.Control && e.KeyCode == Keys.V) {
            HandlePaste();
            e.Handled = true;
        } else if (e.KeyCode == Keys.Delete) {
            RemoveButton_Click(sender, e);
            e.Handled = true;
        }
    }

    private void HandlePaste() {
        if (Clipboard.ContainsFileDropList()) {
            var files = Clipboard.GetFileDropList();
            foreach (var file in files) {
                if (file != null)
                    AddImageFromFile(file);
            }
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
                statusLabel.Text = "Added image from clipboard";
            } catch (Exception ex) {
                Log.Error("Failed to load clipboard image: {Error}", ex.Message);
                MessageBox.Show($"Failed to load clipboard image:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

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
