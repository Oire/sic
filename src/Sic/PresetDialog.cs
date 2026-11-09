using System.Globalization;
using GetText.WindowsForms;
using Oire.Sic.Models;
using Oire.Sic.Services;
using Oire.Sic.Utils;
using Oire.Sic.Utils.Enums;
using Serilog;
using static Oire.Sic.Utils.Localization;
using ImageConverter = Oire.Sic.Services.ImageConverter;

namespace Oire.Sic;

/// <summary>
/// Adds or edits a conversion preset (issue #23): its name, target format and resize settings.
/// Unlike the main window, typing one size never fills in the other, because a preset applies
/// to images of any proportions.
/// </summary>
public partial class PresetDialog: Form {
    private const int KeepProportionsIndex = 0;
    private const int CropIndex = 1;

    private readonly List<string> _takenNames;

    /// <summary>The preset as entered, set when the dialog closes with OK.</summary>
    public ConversionPreset? Result { get; private set; }

    /// <param name="initial">The preset to show, or <see langword="null"/> to start empty.</param>
    /// <param name="takenNames">The names of the other presets. When editing, leave the edited
    /// preset's own name out, so a rename that only changes case works.</param>
    /// <param name="isEdit">Whether the title says Edit Preset rather than Add Preset.</param>
    public PresetDialog(ConversionPreset? initial, IEnumerable<string> takenNames, bool isEdit) {
        ArgumentNullException.ThrowIfNull(takenNames);
        _takenNames = [.. takenNames];

        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog);
        TextDirection.Apply(this);
        Text = isEdit ? _("Edit Preset") : _("Add Preset");

        // Tabbing never reaches the hint, a label no field points to, so the size boxes carry it
        // as their description, which screen readers read after the field's name.
        widthTextBox.AccessibleDescription = hintLabel.Text;
        heightTextBox.AccessibleDescription = hintLabel.Text;

        resizeModeComboBox.Items.AddRange([_("Keep proportions"), _("Crop")]);

        foreach (var format in ImageConverter.GetSupportedFormats()) {
            formatComboBox.Items.Add(format);
        }

        LoadPreset(initial);

        resizeCheckBox.CheckedChanged += (_, _) => UpdateResizeFieldsEnabled();
        nameTextBox.GotFocus += SelectAllOnFocus;
        widthTextBox.GotFocus += SelectAllOnFocus;
        heightTextBox.GotFocus += SelectAllOnFocus;
    }

    private void LoadPreset(ConversionPreset? initial) {
        var formatIndex = initial is null ? -1 : formatComboBox.Items.IndexOf(initial.Format);
        formatComboBox.SelectedIndex = formatIndex >= 0 ? formatIndex : 0;

        nameTextBox.Text = initial?.Name ?? "";
        resizeCheckBox.Checked = initial?.Resizes ?? false;
        resizeModeComboBox.SelectedIndex = initial?.ResizeMode == ResizeMode.Crop ? CropIndex : KeepProportionsIndex;
        widthTextBox.Text = initial?.Width?.ToString(CultureInfo.InvariantCulture) ?? "";
        heightTextBox.Text = initial?.Height?.ToString(CultureInfo.InvariantCulture) ?? "";

        // Setting Checked raises no event when the value doesn't change, so sync the fields here.
        UpdateResizeFieldsEnabled();
    }

    /// <summary>
    /// Enables the resize mode, width and height fields while Resize is ticked. Focus stays on the
    /// check box, so tabbing on reaches the mode first.
    /// </summary>
    private void UpdateResizeFieldsEnabled() {
        var enabled = resizeCheckBox.Checked;
        resizeModeLabel.Enabled = enabled;
        resizeModeComboBox.Enabled = enabled;
        widthLabel.Enabled = enabled;
        widthTextBox.Enabled = enabled;
        heightLabel.Enabled = enabled;
        heightTextBox.Enabled = enabled;
    }

    private static void SelectAllOnFocus(object? sender, EventArgs e) {
        if (sender is TextBox box) {
            box.BeginInvoke(box.SelectAll);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e) {
        base.OnFormClosing(e);

        if (DialogResult != DialogResult.OK) {
            return;
        }

        if (!TryGetName(out var name) || !TryGetResize(out var mode, out var width, out var height)) {
            e.Cancel = true;
            return;
        }

        var preset = new ConversionPreset {
            Name = name,
            Format = formatComboBox.SelectedItem as string ?? ImageConverter.GetSupportedFormats()[0],
            Width = width,
            Height = height,
        };

        preset.ResizeMode = preset.Resizes ? mode : ResizeMode.KeepProportions;
        Result = preset;
    }

    private bool TryGetName(out string name) {
        // Validated as typed: a pasted name that can't be normalized gives InvalidCharacters,
        // and only a valid name is cleaned.
        var result = PresetValidator.ValidateName(nameTextBox.Text, _takenNames);
        name = PresetValidator.TryCleanName(nameTextBox.Text, out var cleaned) ? cleaned : nameTextBox.Text;

        var message = result switch {
            PresetNameResult.Valid => null,
            PresetNameResult.Empty => _("Please enter a name for the preset."),
            PresetNameResult.TooLong => _("A preset name can be at most {0} characters long.", PresetValidator.MaxNameLength),
            PresetNameResult.InvalidCharacters => _("A preset name can't contain tabs, line breaks or other control characters."),
            PresetNameResult.Duplicate => _("A preset named \"{0}\" already exists.", name),
            _ => _("Please enter a name for the preset."),
        };

        if (message is null) {
            return true;
        }

        Log.Debug("PresetDialog: Invalid preset name entered: {Input}", nameTextBox.Text);
        DialogHelper.Show(message, _("Invalid name"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        nameTextBox.Focus();
        nameTextBox.SelectAll();
        return false;
    }

    private bool TryGetResize(out ResizeMode mode, out int? width, out int? height) {
        mode = ResizeMode.KeepProportions;
        width = null;
        height = null;

        // Stale text in a disabled box must not block OK.
        if (!resizeCheckBox.Checked) {
            return true;
        }

        mode = resizeModeComboBox.SelectedIndex == CropIndex ? ResizeMode.Crop : ResizeMode.KeepProportions;
        var error = ResizeValidator.Validate(widthTextBox.Text, heightTextBox.Text, mode, out width, out height);

        if (error == ResizeError.None) {
            return true;
        }

        Log.Debug("PresetDialog: Invalid resize sizes ({Error}): {Width} x {Height}", error, widthTextBox.Text, heightTextBox.Text);
        DialogHelper.ShowResizeError(error, mode, widthTextBox, heightTextBox);
        width = null;
        height = null;
        return false;
    }

}
