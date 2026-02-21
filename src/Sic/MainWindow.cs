using System.Drawing.Imaging;
using GetText.WindowsForms;
using Oire.Sic.Models;
using Oire.Sic.Utils;
using static Oire.Sic.Utils.Localization;
using Serilog;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic;

public partial class MainWindow: Form {
    private readonly List<ImageItem> _imageItems = [];
    private ImageItem? _selectedItem;
    private bool _isAutoFilling;
    private int _clipboardImageCount;
    private readonly System.Windows.Forms.Timer _previewDebounceTimer = new() { Interval = 300 };
    private readonly ObjectPropertiesStore _localizationStore = new();

    public MainWindow() {
        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog, _localizationStore);
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
        keepProportionsRadioButton.CheckedChanged += ResizeModeRadioButton_CheckedChanged;
        widthTextBox.GotFocus += (s, _) => (s as TextBox)?.BeginInvoke(((TextBox)s!).SelectAll);
        heightTextBox.GotFocus += (s, _) => (s as TextBox)?.BeginInvoke(((TextBox)s!).SelectAll);
        widthTextBox.TextChanged += WidthTextBox_TextChanged;
        heightTextBox.TextChanged += HeightTextBox_TextChanged;
        imageListView.SelectedIndexChanged += ImageListView_SelectedIndexChanged;
        imageListView.DragEnter += ImageListView_DragEnter;
        imageListView.DragDrop += ImageListView_DragDrop;

        // Preview debounce
        _previewDebounceTimer.Tick += (_, _) => {
            _previewDebounceTimer.Stop();
            UpdatePreview();
        };

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
            Title = _("Select images to add"),
            Filter = _("Image files") + "|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.tif;*.webp;*.ico;*.avif|" + _("All files") + "|*.*",
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
            MessageBox.Show(_("No matching images found in the selected folder."), _("No images found"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void AddFromUrlMenuItem_Click(object? sender, EventArgs e) {
        using var dialog = new AddUrlDialog();

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        ProgressDialog? progressDialog = null;

        try {
            progressDialog = new ProgressDialog(_("Downloading image..."));
            progressDialog.Show(this);
            Application.DoEvents();

            var item = await ImageConverter.LoadFromUrl(dialog.Url);

            progressDialog.Close();
            progressDialog.Dispose();
            progressDialog = null;

            AddImageItem(item);
            UpdateMenuState();
            statusLabel.Text = _("Added {0} from URL", item.FileName);
        } catch (Exception ex) {
            Log.Error("Failed to load image from URL {Url}: {Error}", dialog.Url, ex.Message);
            MessageBox.Show(_("Failed to load image from URL:\n{0}", ex.Message), _("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = _("Ready");
        } finally {
            if (progressDialog != null) {
                progressDialog.Close();
                progressDialog.Dispose();
            }
        }
    }

    private void RemoveMenuItem_Click(object? sender, EventArgs e) {
        if (imageListView.SelectedIndices.Count == 0)
            return;

        var index = imageListView.SelectedIndices[0];
        _imageItems.RemoveAt(index);
        imageListView.Items.RemoveAt(index);

        if (imageListView.Items.Count > 0) {
            var newIndex = index < imageListView.Items.Count ? index : imageListView.Items.Count - 1;
            imageListView.Items[newIndex].Selected = true;
            imageListView.Items[newIndex].Focused = true;
            imageListView.EnsureVisible(newIndex);
        } else {
            previewPictureBox.Image?.Dispose();
            previewPictureBox.Image = null;
        }

        statusLabel.Text = _n("1 image in queue", "{0} images in queue", _imageItems.Count, _imageItems.Count);
        UpdateMenuState();
    }

    private void RemoveAllMenuItem_Click(object? sender, EventArgs e) {
        _imageItems.Clear();
        imageListView.Items.Clear();
        previewPictureBox.Image?.Dispose();
        previewPictureBox.Image = null;
        statusLabel.Text = _("Ready");
        UpdateMenuState();
    }

    private void OptionsMenuItem_Click(object? sender, EventArgs e) {
        var previousLanguage = Config.General.Language;
        using var dialog = new SettingsDialog();
        dialog.ShowDialog(this);

        if (Config.General.Language != previousLanguage) {
            ApplyLocalization();
        }
    }

    private void ApplyLocalization() {
        Localizer.Revert(this, _localizationStore);
        Localizer.Localize(this, Localization.Catalog, _localizationStore);
        statusLabel.Text = _("Ready");
    }

    private void ExitMenuItem_Click(object? sender, EventArgs e) {
        Close();
    }

    // --- Convert button handler ---

    private async void ConvertButton_Click(object? sender, EventArgs e) {
        if (_imageItems.Count == 0) {
            MessageBox.Show(_("No images to convert. Add some images first."), _("Nothing to convert"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (formatComboBox.SelectedItem is not string targetFormat) {
            MessageBox.Show(_("Please select a target format."), _("No format selected"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int? width = null, height = null;
        var resizeMode = Models.ResizeMode.KeepProportions;

        if (resizeCheckBox.Checked) {
            resizeMode = cropRadioButton.Checked ? Models.ResizeMode.Crop : Models.ResizeMode.KeepProportions;

            var hasWidth = int.TryParse(widthTextBox.Text, out var w) && w >= 1 && w <= 65535;
            var hasHeight = int.TryParse(heightTextBox.Text, out var h) && h >= 1 && h <= 65535;

            if (resizeMode == Models.ResizeMode.Crop) {
                if (!hasWidth) {
                    MessageBox.Show(_("Crop mode requires a valid width (1\u201365535)."), _("Invalid width"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    widthTextBox.Focus();
                    widthTextBox.SelectAll();
                    return;
                }

                if (!hasHeight) {
                    MessageBox.Show(_("Crop mode requires a valid height (1\u201365535)."), _("Invalid height"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    heightTextBox.Focus();
                    heightTextBox.SelectAll();
                    return;
                }

                width = w;
                height = h;
            } else {
                if (!hasWidth && !hasHeight) {
                    MessageBox.Show(_("Please enter at least one valid dimension (1\u201365535)."), _("Invalid dimensions"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    widthTextBox.Focus();
                    widthTextBox.SelectAll();
                    return;
                }

                if (hasWidth)
                    width = w;
                if (hasHeight)
                    height = h;
            }
        }

        var outputFolder = string.IsNullOrWhiteSpace(Config.General.OutputFolder) ? null : Config.General.OutputFolder;

        convertButton.Enabled = false;

        var totalCount = _imageItems.Count;
        var converted = 0;
        var skipped = 0;
        var convertedIndices = new List<int>();
        ProgressDialog? progressDialog = null;

        try {
            progressDialog = new ProgressDialog(_("Preparing to convert..."));
            progressDialog.Show(this);
            Application.DoEvents();

            await Task.Run(() => {
                for (var i = 0; i < totalCount; i++) {
                    var item = _imageItems[i];

                    Invoke(() => {
                        progressDialog!.UpdateMessage(
                            _("Converting {0} ({1}/{2})...", item.FileName, i + 1, totalCount));
                        imageListView.Items[i].SubItems[4].Text = _("Converting...");
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
                                    imageListView.Items[i].SubItems[4].Text = _("Skipped");
                                });
                                continue;
                        }
                    }

                    try {
                        var dir = Path.GetDirectoryName(outputPath);
                        if (dir != null && !Directory.Exists(dir)) {
                            Directory.CreateDirectory(dir);
                        }

                        ImageConverter.Convert(item, targetFormat, outputPath, width, height, resizeMode);
                        converted++;
                        convertedIndices.Add(i);
                    } catch (Exception ex) {
                        Log.Error("Failed to convert {FileName}: {Error}", item.FileName, ex.Message);
                        Invoke(() => {
                            imageListView.Items[i].SubItems[4].Text = _("Failed");
                            MessageBox.Show(_("Failed to convert {0}:\n{1}", item.FileName, ex.Message), _("Conversion Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                    }
                }
            });
        } finally {
            progressDialog?.Close();
            progressDialog?.Dispose();
        }

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

        var failed = totalCount - converted - skipped;
        var total = converted + skipped + failed;
        var summary = _n("Processed {0} image.", "Processed {0} images.", total, total);
        var parts = new List<string>();
        if (converted > 0)
            parts.Add(_("{0} converted", converted));
        if (skipped > 0)
            parts.Add(_("{0} skipped", skipped));
        if (failed > 0)
            parts.Add(_("{0} failed", failed));
        if (parts.Count > 0)
            summary += " " + string.Join(_(", "), parts);

        statusLabel.Text = summary;
        MessageBox.Show(summary, _("Conversion Complete"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        UpdateMenuState();
    }

    // --- Help menu handlers ---

    private void UserGuideMenuItem_Click(object? sender, EventArgs e) {
        MessageBox.Show(_("The user guide is coming soon."), _("User Guide"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void AboutMenuItem_Click(object? sender, EventArgs e) {
        using var dialog = new AboutDialog();
        dialog.ShowDialog(this);
    }

    // --- Controls and list handlers ---

    private void ResizeCheckBox_CheckedChanged(object? sender, EventArgs e) {
        var enabled = resizeCheckBox.Checked;
        widthLabel.Enabled = enabled;
        widthTextBox.Enabled = enabled;
        dimensionSeparatorLabel.Enabled = enabled;
        heightLabel.Enabled = enabled;
        heightTextBox.Enabled = enabled;
        keepProportionsRadioButton.Enabled = enabled;
        cropRadioButton.Enabled = enabled;

        if (enabled) {
            PopulateResizeFieldsFromSelection();
        }

        UpdatePreview();
    }

    private void PopulateResizeFieldsFromSelection() {
        if (_selectedItem == null)
            return;

        _isAutoFilling = true;
        try {
            widthTextBox.Text = _selectedItem.Width.ToString();
            heightTextBox.Text = _selectedItem.Height.ToString();
        } finally {
            _isAutoFilling = false;
        }
    }

    private void ResizeModeRadioButton_CheckedChanged(object? sender, EventArgs e) {
        if (!cropRadioButton.Checked) {
            AutoFillFromWidth();
        }

        UpdatePreview();
    }

    private void WidthTextBox_TextChanged(object? sender, EventArgs e) {
        if (_isAutoFilling)
            return;

        if (!cropRadioButton.Checked && resizeCheckBox.Checked) {
            AutoFillFromWidth();
        }

        SchedulePreviewUpdate();
    }

    private void HeightTextBox_TextChanged(object? sender, EventArgs e) {
        if (_isAutoFilling)
            return;

        if (!cropRadioButton.Checked && resizeCheckBox.Checked) {
            AutoFillFromHeight();
        }

        SchedulePreviewUpdate();
    }

    private void AutoFillFromWidth() {
        if (_selectedItem == null || _selectedItem.Width <= 0)
            return;

        if (!int.TryParse(widthTextBox.Text, out var w) || w < 1)
            return;

        var newHeight = (int)Math.Round((double)w / _selectedItem.Width * _selectedItem.Height);
        _isAutoFilling = true;
        try {
            heightTextBox.Text = newHeight.ToString();
        } finally {
            _isAutoFilling = false;
        }
    }

    private void AutoFillFromHeight() {
        if (_selectedItem == null || _selectedItem.Height <= 0)
            return;

        if (!int.TryParse(heightTextBox.Text, out var h) || h < 1)
            return;

        var newWidth = (int)Math.Round((double)h / _selectedItem.Height * _selectedItem.Width);
        _isAutoFilling = true;
        try {
            widthTextBox.Text = newWidth.ToString();
        } finally {
            _isAutoFilling = false;
        }
    }

    private void SchedulePreviewUpdate() {
        _previewDebounceTimer.Stop();
        _previewDebounceTimer.Start();
    }

    private void UpdatePreview() {
        if (_selectedItem == null)
            return;

        int? resizeWidth = null, resizeHeight = null;
        var resizeMode = Models.ResizeMode.KeepProportions;

        if (resizeCheckBox.Checked) {
            resizeMode = cropRadioButton.Checked ? Models.ResizeMode.Crop : Models.ResizeMode.KeepProportions;

            if (int.TryParse(widthTextBox.Text, out var w) && w >= 1)
                resizeWidth = w;
            if (int.TryParse(heightTextBox.Text, out var h) && h >= 1)
                resizeHeight = h;
        }

        try {
            var preview = ImageConverter.GeneratePreview(
                _selectedItem, previewPictureBox.Width, previewPictureBox.Height,
                resizeWidth, resizeHeight, resizeMode);
            previewPictureBox.Image?.Dispose();
            previewPictureBox.Image = preview;
        } catch (Exception ex) {
            Log.Warning("Failed to generate preview for {FileName}: {Error}", _selectedItem.FileName, ex.Message);
            previewPictureBox.Image?.Dispose();
            previewPictureBox.Image = null;
        }
    }

    private void ImageListView_SelectedIndexChanged(object? sender, EventArgs e) {
        UpdateMenuState();

        if (imageListView.SelectedIndices.Count == 0) {
            _selectedItem = null;
            previewPictureBox.Image?.Dispose();
            previewPictureBox.Image = null;
            return;
        }

        var index = imageListView.SelectedIndices[0];
        var item = _imageItems[index];
        _selectedItem = item;

        if (resizeCheckBox.Checked && !widthTextBox.Focused && !heightTextBox.Focused) {
            PopulateResizeFieldsFromSelection();
        }

        UpdatePreview();
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
        if (e.KeyCode == Keys.Delete && imageListView.Focused) {
            RemoveMenuItem_Click(sender, e);
            e.Handled = true;
        } else if (e.Control && e.KeyCode == Keys.V) {
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
                _clipboardImageCount++;
                var suffix = _clipboardImageCount > 1 ? $"_{_clipboardImageCount}" : "";
                var item = ImageConverter.LoadFromBytes(data, $"clipboard_image{suffix}.png");
                AddImageItem(item);
                UpdateMenuState();
                statusLabel.Text = _("Added image from clipboard");
            } catch (Exception ex) {
                Log.Error("Failed to load clipboard image: {Error}", ex.Message);
                MessageBox.Show(_("Failed to load clipboard image:\n{0}", ex.Message), _("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show(_("Failed to load image:\n{0}\n{1}", path, ex.Message), _("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        listItem.Selected = true;
        listItem.Focused = true;
        listItem.EnsureVisible();
        statusLabel.Text = _n("1 image in queue", "{0} images in queue", _imageItems.Count, _imageItems.Count);
    }

    private ConflictResolution ResolveFileConflict(string outputPath) {
        var result = ConflictResolution.Skip;
        var fileName = Path.GetFileName(outputPath);

        Invoke(() => {
            using var dialog = new Form {
                Text = _("File Already Exists"),
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
                Text = _("The file \"{0}\" already exists.\nWhat would you like to do?", fileName),
                AutoSize = true,
            };

            var buttonPanel = new FlowLayoutPanel {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
            };

            var overwriteBtn = new Button { Text = _("Overwrite") };
            var renameBtn = new Button { Text = _("Rename") };
            var skipBtn = new Button { Text = _("Skip") };

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
