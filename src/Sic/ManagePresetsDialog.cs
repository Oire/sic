using GetText.WindowsForms;
using Oire.Sic.Models;
using Oire.Sic.Services;
using Oire.Sic.Utils;
using Serilog;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

/// <summary>
/// Lists the saved conversion presets (issue #23) and adds, edits and removes them. It works on a
/// copy of <see cref="Config.Presets"/> and writes it back when it closes, however it closes, so
/// Esc and Alt+F4 keep the edits too.
/// </summary>
public partial class ManagePresetsDialog: Form {
    private readonly List<ConversionPreset> _presets;
    private bool _modified;

    /// <summary>Whether the presets were changed and saved, so the caller can rebuild its menu.</summary>
    public bool Changed { get; private set; }

    public ManagePresetsDialog() {
        _presets = [.. Config.Presets];

        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog);
        TextDirection.Apply(this);
        Text = _("Manage Presets");

        addButton.Click += AddButton_Click;
        editButton.Click += EditButton_Click;
        removeButton.Click += RemoveButton_Click;
        presetsListBox.SelectedIndexChanged += (_, _) => UpdateButtonState();

        RefreshList(0);
    }

    private void AddButton_Click(object? sender, EventArgs e) {
        using var dialog = new PresetDialog(null, _presets.Select(preset => preset.Name), isEdit: false);

        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Result is { } preset) {
            _presets.Add(preset);
            _modified = true;
            Log.Information("ManagePresetsDialog: Added preset {Name}", preset.Name);
            RefreshList(_presets.Count - 1);
        }

        FocusList();
    }

    private void EditButton_Click(object? sender, EventArgs e) {
        var index = presetsListBox.SelectedIndex;

        if (index < 0 || index >= _presets.Count) {
            return;
        }

        // The preset's own name is left out, so a rename that only changes case works.
        var takenNames = _presets.Where((_, i) => i != index).Select(preset => preset.Name);
        using var dialog = new PresetDialog(_presets[index], takenNames, isEdit: true);

        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Result is { } preset) {
            _presets[index] = preset;
            _modified = true;
            Log.Information("ManagePresetsDialog: Edited preset {Name}", preset.Name);
            RefreshList(index);
        }

        FocusList();
    }

    private void RemoveButton_Click(object? sender, EventArgs e) {
        var index = presetsListBox.SelectedIndex;

        if (index < 0 || index >= _presets.Count) {
            return;
        }

        var name = _presets[index].Name;
        var answer = DialogHelper.Show(
            _("Remove the preset \"{0}\"?", name),
            _("Remove Preset"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer == DialogResult.Yes) {
            _presets.RemoveAt(index);
            _modified = true;
            Log.Information("ManagePresetsDialog: Removed preset {Name}", name);

            // The preset that took its place, or the new last one.
            RefreshList(Math.Min(index, _presets.Count - 1));
        }

        FocusList();
    }

    /// <summary>Refills the list and selects <paramref name="selectIndex"/> when it's valid.</summary>
    private void RefreshList(int selectIndex) {
        presetsListBox.BeginUpdate();

        try {
            presetsListBox.Items.Clear();

            foreach (var preset in _presets) {
                presetsListBox.Items.Add(PresetDescriber.DescribeWithName(preset));
            }
        } finally {
            presetsListBox.EndUpdate();
        }

        if (selectIndex >= 0 && selectIndex < presetsListBox.Items.Count) {
            presetsListBox.SelectedIndex = selectIndex;
        }

        UpdateButtonState();
    }

    private void UpdateButtonState() {
        var hasSelection = presetsListBox.SelectedIndex >= 0;
        editButton.Enabled = hasSelection;
        removeButton.Enabled = hasSelection;
    }

    /// <summary>
    /// Puts focus back on the list after an action, or on Add when no presets are left, since an
    /// empty list has nothing to land on.
    /// </summary>
    private void FocusList() {
        if (presetsListBox.Items.Count > 0) {
            presetsListBox.Focus();
        } else {
            addButton.Focus();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e) {
        base.OnFormClosing(e);

        if (e.Cancel || !_modified) {
            return;
        }

        Config.Presets = [.. _presets];
        Config.Save();
        _modified = false;
        Changed = true;
    }
}
