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
    private bool _isLoadingFiles;
    private int _clipboardImageCount;
    private readonly System.Windows.Forms.Timer _previewDebounceTimer = new() { Interval = 300 };
    private readonly ObjectPropertiesStore _localizationStore = new();

    private sealed record BatchAddResult(List<ImageItem> Items, List<string> Errors, int SkippedPlaceholders);

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
        optionsMenuItem.Click += OptionsMenuItem_Click;
        exitMenuItem.Click += ExitMenuItem_Click;

        // Edit menu
        removeMenuItem.Click += RemoveMenuItem_Click;
        removeAllMenuItem.Click += RemoveAllMenuItem_Click;

        // Convert menu
        convertSelectedMenuItem.Click += ConvertSelectedMenuItem_Click;
        convertAllMenuItem.Click += ConvertButton_Click;
        createMultiSizeIcoMenuItem.Click += CreateMultiSizeIcoMenuItem_Click;

        // Help menu
        userGuideMenuItem.Click += UserGuideMenuItem_Click;
        supportDevelopmentMenuItem.Click += SupportDevelopmentMenuItem_Click;
        aboutMenuItem.Click += AboutMenuItem_Click;

        // Controls
        convertSelectedButton.Click += ConvertSelectedMenuItem_Click;
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

        editMenu.Enabled = hasItems;
        removeMenuItem.Enabled = hasSelection;
        removeAllMenuItem.Enabled = hasItems;
        convertMenu.Enabled = hasItems;
        convertSelectedButton.Enabled = hasSelection;
        convertButton.Enabled = hasItems;
        convertSelectedMenuItem.Enabled = hasSelection;
        convertAllMenuItem.Enabled = hasItems;
        createMultiSizeIcoMenuItem.Enabled = hasSelection;
    }

    // --- File menu handlers ---

    private async void AddImageMenuItem_Click(object? sender, EventArgs e) {
        using var dialog = new OpenFileDialog {
            Title = _("Select images to add"),
            Filter = _("Image files") + "|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.tif;*.webp;*.ico;*.avif|" + _("All files") + "|*.*",
            Multiselect = true,
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        await AddFilesAsync(dialog.FileNames);
    }

    private async void AddFolderMenuItem_Click(object? sender, EventArgs e) {
        using var dialog = new AddFolderDialog();

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var searchOption = dialog.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var extensions = dialog.SelectedExtensions;
        var folder = dialog.SelectedFolder;

        var files = FileHelper.EnumerateImageFiles(folder, extensions, searchOption).ToArray();

        if (files.Length == 0) {
            MessageBox.Show(_("No matching images found in the selected folder."), _("No images found"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await AddFilesAsync(files, folder);
    }

    private async void AddFromUrlMenuItem_Click(object? sender, EventArgs e) {
        using var dialog = new AddUrlDialog();

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        await PasteFromUrlAsync(dialog.Url);
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

    // --- Convert handlers ---

    private bool TryGetConversionParams(out string targetFormat, out int? width, out int? height, out Models.ResizeMode resizeMode) {
        targetFormat = "";
        width = null;
        height = null;
        resizeMode = Models.ResizeMode.KeepProportions;

        if (formatComboBox.SelectedItem is not string format) {
            MessageBox.Show(_("Please select a target format."), _("No format selected"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        targetFormat = format;

        if (resizeCheckBox.Checked) {
            resizeMode = cropRadioButton.Checked ? Models.ResizeMode.Crop : Models.ResizeMode.KeepProportions;

            var hasWidth = int.TryParse(widthTextBox.Text, out var w) && w >= 1 && w <= 65535;
            var hasHeight = int.TryParse(heightTextBox.Text, out var h) && h >= 1 && h <= 65535;

            if (resizeMode == Models.ResizeMode.Crop) {
                if (!hasWidth) {
                    MessageBox.Show(_("Crop mode requires a valid width (1\u201365535)."), _("Invalid width"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    widthTextBox.Focus();
                    widthTextBox.SelectAll();
                    return false;
                }

                if (!hasHeight) {
                    MessageBox.Show(_("Crop mode requires a valid height (1\u201365535)."), _("Invalid height"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    heightTextBox.Focus();
                    heightTextBox.SelectAll();
                    return false;
                }

                width = w;
                height = h;
            } else {
                if (!hasWidth && !hasHeight) {
                    MessageBox.Show(_("Please enter at least one valid dimension (1\u201365535)."), _("Invalid dimensions"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    widthTextBox.Focus();
                    widthTextBox.SelectAll();
                    return false;
                }

                if (hasWidth)
                    width = w;
                if (hasHeight)
                    height = h;
            }
        }

        return true;
    }

    private static string ValidateOutputFolder() {
        var outputFolder = Config.General.OutputFolder;

        if (!string.IsNullOrWhiteSpace(outputFolder)
            && outputFolder != Utils.Constants.App.DefaultOutputFolder
            && !Directory.Exists(outputFolder)) {
            MessageBox.Show(
                _("The output folder \"{0}\" no longer exists. The default folder will be used.", outputFolder),
                _("Output Folder Not Found"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);

            outputFolder = Utils.Constants.App.DefaultOutputFolder;
            Config.General.OutputFolder = outputFolder;
            Config.Save();
        }

        return outputFolder;
    }

    private async Task ConvertItemsAsync(List<int> indices) {
        if (!TryGetConversionParams(out var targetFormat, out var width, out var height, out var resizeMode))
            return;

        var outputFolder = ValidateOutputFolder();

        convertButton.Enabled = false;

        var totalCount = indices.Count;
        var converted = 0;
        var skipped = 0;
        var failed = 0;
        var wasCancelled = false;
        var convertedIndices = new List<int>();
        ProgressDialog? progressDialog = null;

        try {
            progressDialog = new ProgressDialog(_("Preparing to convert..."));
            progressDialog.Text = _("Converting...");
            progressDialog.Show(this);
            Application.DoEvents();

            await Task.Run(() => {
                for (var j = 0; j < totalCount; j++) {
                    if (progressDialog!.IsCancelled) {
                        wasCancelled = true;
                        break;
                    }

                    var i = indices[j];
                    var item = _imageItems[i];

                    Invoke(() => {
                        progressDialog!.UpdateMessage(
                            _("Converting {0} ({1}/{2})...", item.FileName, j + 1, totalCount));
                        progressDialog!.UpdateProgress(j + 1, totalCount);
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
                        failed++;
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
        if (wasCancelled)
            summary += " " + _("Cancelled.");

        statusLabel.Text = summary;
        MessageBox.Show(summary, _("Conversion Complete"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        UpdateMenuState();
    }

    private async void ConvertButton_Click(object? sender, EventArgs e) {
        if (_imageItems.Count == 0) {
            MessageBox.Show(_("No images to convert. Add some images first."), _("Nothing to convert"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var allIndices = Enumerable.Range(0, _imageItems.Count).ToList();
        await ConvertItemsAsync(allIndices);
    }

    private async void ConvertSelectedMenuItem_Click(object? sender, EventArgs e) {
        if (imageListView.SelectedIndices.Count == 0)
            return;

        var selectedIndices = imageListView.SelectedIndices.Cast<int>().ToList();
        await ConvertItemsAsync(selectedIndices);
    }

    private async void CreateMultiSizeIcoMenuItem_Click(object? sender, EventArgs e) {
        if (imageListView.SelectedIndices.Count == 0)
            return;

        using var presetDialog = new IcoPresetDialog();
        if (presetDialog.ShowDialog(this) != DialogResult.OK)
            return;

        var sizes = presetDialog.SelectedSizes;
        var index = imageListView.SelectedIndices[0];
        var item = _imageItems[index];
        var outputFolder = ValidateOutputFolder();
        var outputPath = ImageConverter.GenerateOutputPath(item, "ICO", outputFolder);

        if (File.Exists(outputPath)) {
            var resolution = ResolveFileConflict(outputPath);

            switch (resolution) {
                case ConflictResolution.Overwrite:
                    break;
                case ConflictResolution.Rename:
                    outputPath = ImageConverter.GetConflictRenamePath(outputPath);
                    break;
                case ConflictResolution.Skip:
                    return;
            }
        }

        convertButton.Enabled = false;
        ProgressDialog? progressDialog = null;

        try {
            progressDialog = new ProgressDialog(_("Creating multi-size ICO..."));
            progressDialog.Text = _("Converting...");
            progressDialog.Show(this);
            Application.DoEvents();

            imageListView.Items[index].SubItems[4].Text = _("Converting...");

            await Task.Run(() => {
                var dir = Path.GetDirectoryName(outputPath);
                if (dir != null && !Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }

                ImageConverter.CreateMultiSizeIco(item, outputPath, sizes);
            }).WaitAsync(progressDialog.CancellationToken);

            _imageItems.RemoveAt(index);
            imageListView.Items.RemoveAt(index);
            previewPictureBox.Image?.Dispose();
            previewPictureBox.Image = null;

            statusLabel.Text = _("Multi-size ICO created: {0}", Path.GetFileName(outputPath));
            MessageBox.Show(_("Multi-size ICO created successfully:\n{0}", outputPath), _("ICO Created"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        } catch (OperationCanceledException) {
            imageListView.Items[index].SubItems[4].Text = "";
            statusLabel.Text = _("Ready");
        } catch (Exception ex) {
            Log.Error("Failed to create multi-size ICO for {FileName}: {Error}", item.FileName, ex.Message);
            imageListView.Items[index].SubItems[4].Text = _("Failed");
            MessageBox.Show(_("Failed to create multi-size ICO:\n{0}", ex.Message), _("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        } finally {
            progressDialog?.Close();
            progressDialog?.Dispose();
            convertButton.Enabled = true;
        }

        UpdateMenuState();
    }

    private void SupportDevelopmentMenuItem_Click(object? sender, EventArgs e) {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
            FileName = "https://oire.org/donate",
            UseShellExecute = true,
        });
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
        resizeModeGroupBox.Enabled = enabled;
        widthLabel.Enabled = enabled;
        widthTextBox.Enabled = enabled;
        heightLabel.Enabled = enabled;
        heightTextBox.Enabled = enabled;

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

    private async void ImageListView_DragDrop(object? sender, DragEventArgs e) {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files)
            return;

        await AddFilesAsync(files);
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

    private async void HandlePaste() {
        if (Clipboard.ContainsFileDropList()) {
            var files = Clipboard.GetFileDropList();
            var paths = new List<string>();
            foreach (var file in files) {
                if (file != null)
                    paths.Add(file);
            }

            if (paths.Count > 0) {
                await AddFilesAsync(paths.ToArray());
            }
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
        } else if (Clipboard.ContainsText()) {
            var text = Clipboard.GetText().Trim();

            if (Uri.TryCreate(text, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)) {
                await PasteFromUrlAsync(text);
            }
        }
    }

    private async Task PasteFromUrlAsync(string url) {
        ProgressDialog? progressDialog = null;

        try {
            progressDialog = new ProgressDialog(_("Downloading image..."));
            progressDialog.Text = _("Downloading...");
            progressDialog.Show(this);
            Application.DoEvents();

            var progress = new Progress<(long BytesRead, long? TotalBytes)>(p => {
                if (p.TotalBytes.HasValue && p.TotalBytes.Value > 0) {
                    var current = (int)(p.BytesRead * 100 / p.TotalBytes.Value);
                    progressDialog!.UpdateProgress(current, 100);
                    progressDialog!.UpdateMessage(
                        _("Downloading image... {0}%", current));
                } else {
                    progressDialog!.UpdateMessage(
                        _("Downloading image... ({0} KB)", p.BytesRead / 1024));
                }
            });

            var item = await ImageConverter.LoadFromUrl(url, progressDialog.CancellationToken, progress);

            progressDialog.Close();
            progressDialog.Dispose();
            progressDialog = null;

            AddImageItem(item);
            UpdateMenuState();
            statusLabel.Text = _("Added {0} from URL", item.FileName);
        } catch (OperationCanceledException) {
            statusLabel.Text = _("Ready");
        } catch (Exception ex) {
            Log.Error("Failed to load image from URL {Url}: {Error}", url, ex.Message);
            MessageBox.Show(_("Failed to load image from URL:\n{0}", ex.Message), _("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = _("Ready");
        } finally {
            if (progressDialog != null) {
                progressDialog.Close();
                progressDialog.Dispose();
            }
        }
    }

    // --- Shared helpers ---

    private async Task AddFilesAsync(string[] paths, string? basePath = null) {
        if (_isLoadingFiles)
            return;

        if (paths.Length == 1) {
            AddImageFromFile(paths[0], basePath);
            UpdateMenuState();
            return;
        }

        _isLoadingFiles = true;

        try {
            // Pre-scan for cloud placeholders (fast — attribute checks only)
            var localPaths = new List<string>();
            var cloudPaths = new List<string>();

            foreach (var path in paths) {
                if (FileHelper.IsCloudPlaceholder(path))
                    cloudPaths.Add(path);
                else
                    localPaths.Add(path);
            }

            var skippedPlaceholders = 0;

            if (cloudPaths.Count > 0) {
                var answer = MessageBox.Show(
                    _n(
                        "{0} file is stored in the cloud and needs to be downloaded first.\nDownload it?",
                        "{0} files are stored in the cloud and need to be downloaded first.\nDownload them?",
                        cloudPaths.Count, cloudPaths.Count),
                    _("Cloud Files"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (answer == DialogResult.Yes)
                    localPaths.AddRange(cloudPaths);
                else
                    skippedPlaceholders = cloudPaths.Count;
            }

            if (localPaths.Count == 0) {
                if (skippedPlaceholders > 0)
                    ShowBatchAddSummary(new BatchAddResult([], [], skippedPlaceholders));
                return;
            }

            var pathsToLoad = localPaths;
            ProgressDialog? progressDialog = null;

            try {
                progressDialog = new ProgressDialog(
                    _("Loading images ({0}/{1})...", 0, pathsToLoad.Count));
                progressDialog.Text = _("Loading...");
                progressDialog.Show(this);
                Application.DoEvents();

                var totalCount = pathsToLoad.Count;
                var result = await Task.Run(() => {
                    var items = new List<ImageItem>();
                    var errors = new List<string>();
                    var lastUpdateTick = Environment.TickCount64;

                    for (var i = 0; i < totalCount; i++) {
                        if (progressDialog!.IsCancelled)
                            break;

                        var path = pathsToLoad[i];
                        var fileName = Path.GetFileName(path);

                        var now = Environment.TickCount64;
                        if (i == 0 || i == totalCount - 1 || now - lastUpdateTick >= 250) {
                            lastUpdateTick = now;
                            var current = i + 1;
                            BeginInvoke(() => {
                                progressDialog!.UpdateMessage(
                                    _("Loading images ({0}/{1})...", current, totalCount));
                                progressDialog!.UpdateProgress(current, totalCount);
                            });
                        }

                        try {
                            var item = ImageConverter.LoadFromFile(path);
                            item.BasePath = basePath;
                            items.Add(item);
                        } catch (Exception ex) {
                            Log.Error("Failed to load image {Path}: {Error}", path, ex.Message);
                            errors.Add($"{fileName}: {ex.Message}");
                        }
                    }

                    return new BatchAddResult(items, errors, skippedPlaceholders);
                });

                progressDialog.Close();
                progressDialog.Dispose();
                progressDialog = null;

                // Batch-add to ListView without per-item overhead.
                // AddImageItem calls AutoResizeColumns + sets Selected (firing
                // SelectedIndexChanged → UpdatePreview → Magick.NET decode)
                // per item, which freezes the UI on large batches.
                imageListView.BeginUpdate();
                try {
                    foreach (var item in result.Items) {
                        _imageItems.Add(item);

                        var listItem = new ListViewItem(item.FileName);
                        listItem.SubItems.Add(item.OriginalFormat);
                        listItem.SubItems.Add(item.GetDimensionsDisplay());
                        listItem.SubItems.Add(item.GetSizeDisplay());
                        listItem.SubItems.Add("");
                        imageListView.Items.Add(listItem);
                    }
                } finally {
                    imageListView.EndUpdate();
                }

                if (result.Items.Count > 0) {
                    imageListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                    var lastIndex = imageListView.Items.Count - 1;
                    imageListView.Items[lastIndex].Selected = true;
                    imageListView.Items[lastIndex].Focused = true;
                    imageListView.Items[lastIndex].EnsureVisible();
                }

                statusLabel.Text = _n("1 image in queue", "{0} images in queue", _imageItems.Count, _imageItems.Count);
                UpdateMenuState();
                imageListView.Focus();

                if (result.Errors.Count > 0 || result.SkippedPlaceholders > 0) {
                    ShowBatchAddSummary(result);
                }
            } finally {
                if (progressDialog != null) {
                    progressDialog.Close();
                    progressDialog.Dispose();
                }
            }
        } finally {
            _isLoadingFiles = false;
        }
    }

    private static void ShowBatchAddSummary(BatchAddResult result) {
        var parts = new List<string>();

        if (result.Items.Count > 0)
            parts.Add(_n("{0} image loaded", "{0} images loaded", result.Items.Count, result.Items.Count));
        if (result.SkippedPlaceholders > 0)
            parts.Add(_n("{0} cloud-only file skipped", "{0} cloud-only files skipped", result.SkippedPlaceholders, result.SkippedPlaceholders));
        if (result.Errors.Count > 0)
            parts.Add(_n("{0} file failed to load", "{0} files failed to load", result.Errors.Count, result.Errors.Count));

        var summary = string.Join("\n", parts);

        if (result.Errors.Count > 0) {
            summary += "\n\n" + _("Errors:") + "\n";
            var errorLines = result.Errors.Take(20).ToList();
            summary += string.Join("\n", errorLines);

            if (result.Errors.Count > 20) {
                summary += "\n" + _("...and {0} more", result.Errors.Count - 20);
            }
        }

        var icon = result.Errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information;
        MessageBox.Show(summary, _("Add Images"), MessageBoxButtons.OK, icon);
    }

    private void AddImageFromFile(string path, string? basePath = null) {
        try {
            var item = ImageConverter.LoadFromFile(path);
            item.BasePath = basePath;
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
        imageListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
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
