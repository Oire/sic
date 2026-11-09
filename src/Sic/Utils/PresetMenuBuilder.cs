using Oire.Sic.Models;
using Oire.Sic.Services;
using Oire.WinForms.NativeControls;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic.Utils;

/// <summary>
/// Builds the Convert > Presets submenu: the saved presets, then Add Preset and Manage Presets.
/// </summary>
internal static class PresetMenuBuilder {
    /// <summary>
    /// Appends a separator and the Presets submenu to <paramref name="convert"/>. Each preset is
    /// listed by name, with its ampersands escaped so a name such as "R&amp;D" shows as it is and
    /// never takes a mnemonic. An empty list shows one disabled "No presets yet" row, so a screen
    /// reader says why the top of the menu has no presets.
    /// </summary>
    /// <param name="convert">The Convert menu to append to.</param>
    /// <param name="presets">The saved presets, in the user's order.</param>
    /// <param name="onApply">Runs with the chosen preset.</param>
    /// <param name="onAdd">Runs when Add Preset is chosen.</param>
    /// <param name="onManage">Runs when Manage Presets is chosen.</param>
    public static void AddTo(
        NativeMenuSpec convert, IReadOnlyList<ConversionPreset> presets,
        Action<ConversionPreset> onApply, Action onAdd, Action onManage) {
        ArgumentNullException.ThrowIfNull(convert);
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(onApply);
        ArgumentNullException.ThrowIfNull(onAdd);
        ArgumentNullException.ThrowIfNull(onManage);

        convert.AddSeparator();
        convert.AddMenu(_("&Presets"), menu => {
            if (presets.Count == 0) {
                // NativeMenuSpec.Add needs a callback even for an item that can't be chosen.
                menu.Add(_("No presets yet"), () => { });
                menu.Items[^1].IsEnabled = false;
            } else {
                foreach (var preset in presets) {
                    menu.Add(PresetValidator.EscapeAmpersands(preset.Name), () => onApply(preset));
                }
            }

            menu.AddSeparator();
            menu.Add(_("&Add Preset..."), onAdd);
            menu.Add(_("&Manage Presets..."), onManage);
        });
    }
}
