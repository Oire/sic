using Oire.Sic.Models;

namespace Oire.Sic.Services;

/// <summary>
/// Finds a preset by name and merges it with the CLI's explicit options, so <c>--preset</c>
/// behaves the same wherever it is used. Pure: no console output and no translated text.
/// </summary>
internal static class PresetResolver {
    /// <summary>
    /// The preset called <paramref name="name"/>, compared as stored names are: cleaned, and
    /// ignoring case in any script. <see langword="null"/> when there is none.
    /// </summary>
    public static ConversionPreset? Find(IEnumerable<ConversionPreset> presets, string name) {
        ArgumentNullException.ThrowIfNull(presets);
        ArgumentNullException.ThrowIfNull(name);

        // A name that can't be normalized can't be a stored one.
        if (!PresetValidator.TryCleanName(name, out var cleaned)) {
            return null;
        }

        return presets.FirstOrDefault(preset =>
            PresetValidator.TryCleanName(preset.Name, out var stored)
            && PresetValidator.NameComparer.Equals(stored, cleaned));
    }

    /// <summary>
    /// The sizes and mode a conversion uses. An explicit resize is one with a size, which the CLI
    /// sets only for a valid <c>--resize</c>: its sizes win and the mode comes only from
    /// <paramref name="crop"/>, ignoring the preset's resize settings entirely. Without one, the
    /// sizes come from <paramref name="preset"/> (none when it is <see langword="null"/>), and the
    /// mode is Crop when <paramref name="crop"/> is set, otherwise the preset's.
    /// </summary>
    public static (int? Width, int? Height, ResizeMode Mode) ResolveResize(
        ConversionPreset? preset, int? explicitWidth, int? explicitHeight, bool crop) {
        var hasExplicitResize = explicitWidth.HasValue || explicitHeight.HasValue;

        return hasExplicitResize
            ? (explicitWidth, explicitHeight, crop ? ResizeMode.Crop : ResizeMode.KeepProportions)
            : (preset?.Width, preset?.Height, crop ? ResizeMode.Crop : preset?.ResizeMode ?? ResizeMode.KeepProportions);
    }
}
