using GetText.WindowsForms;
using Oire.Sic.Utils;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

public partial class IcoPresetDialog: Form {
    private static readonly uint[] FaviconSizes = [16, 32, 48, 64];
    private static readonly uint[] AppIconSizes = [16, 20, 24, 32, 40, 48, 64, 256];

    private readonly List<uint> _customSizes = [];

    public uint[] SelectedSizes {
        get {
            if (faviconRadioButton.Checked)
                return FaviconSizes;
            if (appIconRadioButton.Checked)
                return AppIconSizes;
            return [.. _customSizes.Order()];
        }
    }

    public IcoPresetDialog() {
        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog);

        faviconRadioButton.CheckedChanged += PresetRadioButton_CheckedChanged;
        appIconRadioButton.CheckedChanged += PresetRadioButton_CheckedChanged;
        customRadioButton.CheckedChanged += PresetRadioButton_CheckedChanged;
        addSizeButton.Click += AddSizeButton_Click;
        removeSizeButton.Click += RemoveSizeButton_Click;
        sizesListBox.SelectedIndexChanged += SizesListBox_SelectedIndexChanged;

        PopulateSizesList(FaviconSizes);
    }

    private void PresetRadioButton_CheckedChanged(object? sender, EventArgs e) {
        var isCustom = customRadioButton.Checked;

        sizesLabel.Enabled = isCustom;
        sizesListBox.Enabled = isCustom;
        sizeNumericUpDown.Enabled = isCustom;
        addSizeButton.Enabled = isCustom;
        removeSizeButton.Enabled = isCustom && sizesListBox.SelectedIndex >= 0;

        if (sender is not RadioButton { Checked: true })
            return;

        if (faviconRadioButton.Checked) {
            PopulateSizesList(FaviconSizes);
        } else if (appIconRadioButton.Checked) {
            PopulateSizesList(AppIconSizes);
        } else if (isCustom) {
            // When switching to Custom, populate with the previously selected preset as a starting point
            _customSizes.Clear();

            foreach (var item in sizesListBox.Items) {
                if (uint.TryParse(item.ToString(), out var size)) {
                    _customSizes.Add(size);
                }
            }

            if (_customSizes.Count == 0) {
                _customSizes.AddRange(FaviconSizes);
                RefreshSizesList();
            }
        }
    }

    private void AddSizeButton_Click(object? sender, EventArgs e) {
        var size = (uint)sizeNumericUpDown.Value;

        if (_customSizes.Contains(size)) {
            MessageBox.Show(
                _("Size {0} is already in the list.", size),
                _("Duplicate Size"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        _customSizes.Add(size);
        RefreshSizesList();
    }

    private void RemoveSizeButton_Click(object? sender, EventArgs e) {
        if (sizesListBox.SelectedIndex < 0)
            return;

        if (uint.TryParse(sizesListBox.SelectedItem?.ToString(), out var size)) {
            _customSizes.Remove(size);
        }

        RefreshSizesList();
    }

    private void SizesListBox_SelectedIndexChanged(object? sender, EventArgs e) {
        if (customRadioButton.Checked) {
            removeSizeButton.Enabled = sizesListBox.SelectedIndex >= 0;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e) {
        base.OnFormClosing(e);

        if (DialogResult != DialogResult.OK)
            return;

        if (customRadioButton.Checked && _customSizes.Count == 0) {
            MessageBox.Show(
                _("Please add at least one size to the list."),
                _("No Sizes"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            e.Cancel = true;
        }
    }

    private void PopulateSizesList(uint[] sizes) {
        sizesListBox.Items.Clear();

        foreach (var size in sizes) {
            sizesListBox.Items.Add(size.ToString());
        }
    }

    private void RefreshSizesList() {
        _customSizes.Sort();
        sizesListBox.Items.Clear();

        foreach (var size in _customSizes) {
            sizesListBox.Items.Add(size.ToString());
        }
    }
}
