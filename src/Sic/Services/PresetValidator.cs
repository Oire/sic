using System.Text;
using Oire.Sic.Models;
using Oire.Sic.Utils.Enums;
using Serilog;

namespace Oire.Sic.Services;

/// <summary>
/// Checks preset names and the presets loaded from <c>sic.json</c>. Every comparison of names goes
/// through <see cref="NameComparer"/>, and every stored name through <see cref="CleanName"/>, so a
/// name typed with a decomposed <c>é</c> still matches one stored composed.
/// </summary>
internal static class PresetValidator {
    /// <summary>The longest name a preset can have, in characters.</summary>
    public const int MaxNameLength = 64;

    /// <summary>How preset names are compared: ignoring case, in any script.</summary>
    public static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>The name as it is stored: trimmed and in Unicode normalization form C.</summary>
    public static string CleanName(string name) {
        ArgumentNullException.ThrowIfNull(name);
        return name.Trim().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Checks <paramref name="name"/>, cleaned, against the rules for a preset name. Any script is
    /// fine; only control characters are refused. <paramref name="takenNames"/> holds the names of
    /// the other presets; when editing, callers leave the preset's own name out, so a rename that
    /// only changes case works.
    /// </summary>
    public static PresetNameResult ValidateName(string? name, IEnumerable<string> takenNames) {
        ArgumentNullException.ThrowIfNull(takenNames);

        var cleaned = CleanName(name ?? "");

        if (cleaned.Length == 0) {
            return PresetNameResult.Empty;
        }

        if (cleaned.Length > MaxNameLength) {
            return PresetNameResult.TooLong;
        }

        if (cleaned.Any(char.IsControl)) {
            return PresetNameResult.InvalidCharacters;
        }

        return takenNames.Any(taken => taken is not null && NameComparer.Equals(CleanName(taken), cleaned))
            ? PresetNameResult.Duplicate
            : PresetNameResult.Valid;
    }

    /// <summary>
    /// Keeps the usable presets loaded from <c>sic.json</c>, in their order, and counts the rest.
    /// A preset is dropped, with a logged warning, when it is null, its name or format is null, its
    /// name fails <see cref="ValidateName"/> (a duplicate included), its format isn't a supported
    /// target, a size is outside 1..<see cref="ResizeValidator.MaxDimension"/>, or its mode is
    /// undefined or Crop with only one size. Kept presets get a cleaned name, an upper-cased format
    /// key (JPEG becomes JPG, TIF becomes TIFF), and <see cref="ResizeMode.KeepProportions"/> when
    /// they have no sizes.
    /// </summary>
    public static (List<ConversionPreset> Presets, int Dropped) Normalize(IEnumerable<ConversionPreset?>? presets) {
        var kept = new List<ConversionPreset>();
        var dropped = 0;

        if (presets is null) {
            return (kept, dropped);
        }

        var position = 0;

        foreach (var preset in presets) {
            position++;

            if (TryNormalize(preset, kept, out var reason) is { } normalized) {
                kept.Add(normalized);
            } else {
                Log.Warning("sic.json: dropping preset {Position} ({Name}): {Reason}", position, preset?.Name, reason);
                dropped++;
            }
        }

        return (kept, dropped);
    }

    /// <summary>
    /// Doubles every <c>&amp;</c> in <paramref name="name"/>, so a menu shows it as it is instead of
    /// taking the next letter as a mnemonic.
    /// </summary>
    public static string EscapeAmpersands(string name) {
        ArgumentNullException.ThrowIfNull(name);
        return name.Replace("&", "&&", StringComparison.Ordinal);
    }

    /// <returns>A cleaned copy of <paramref name="preset"/>, or <see langword="null"/> with the
    /// <paramref name="reason"/> it can't be used.</returns>
    private static ConversionPreset? TryNormalize(ConversionPreset? preset, List<ConversionPreset> kept, out string reason) {
        if (preset is null) {
            reason = "the entry is null";
            return null;
        }

        // Null despite the annotations when sic.json says "name": null.
        if (preset.Name is null || preset.Format is null) {
            reason = "the name or the format is missing";
            return null;
        }

        var nameResult = ValidateName(preset.Name, kept.Select(other => other.Name));

        if (nameResult != PresetNameResult.Valid) {
            reason = $"invalid name ({nameResult})";
            return null;
        }

        if (NormalizeFormat(preset.Format) is not { } format) {
            reason = $"unsupported format {preset.Format}";
            return null;
        }

        if (!IsValidSize(preset.Width) || !IsValidSize(preset.Height)) {
            reason = "a size is out of range";
            return null;
        }

        if (!Enum.IsDefined(preset.ResizeMode)) {
            reason = $"undefined resize mode {preset.ResizeMode}";
            return null;
        }

        if (preset.ResizeMode == ResizeMode.Crop && preset.Width.HasValue != preset.Height.HasValue) {
            reason = "Crop needs both sizes";
            return null;
        }

        reason = "";

        var normalized = new ConversionPreset {
            Name = CleanName(preset.Name),
            Format = format,
            Width = preset.Width,
            Height = preset.Height,
        };

        normalized.ResizeMode = normalized.Resizes ? preset.ResizeMode : ResizeMode.KeepProportions;
        return normalized;
    }

    /// <summary>The SIC! format key for <paramref name="format"/>, or <see langword="null"/> when
    /// it isn't a conversion target. HEIC is input-only, so it isn't one.</summary>
    private static string? NormalizeFormat(string format) {
        var key = format.Trim().ToUpperInvariant() switch {
            "JPEG" => "JPG",
            "TIF" => "TIFF",
            var other => other,
        };

        return ImageConverter.GetSupportedFormats().Contains(key, StringComparer.Ordinal) ? key : null;
    }

    private static bool IsValidSize(int? size) => size is null or (>= 1 and <= ResizeValidator.MaxDimension);
}
