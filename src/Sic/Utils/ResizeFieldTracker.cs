using Oire.Sic.Utils.Enums;

namespace Oire.Sic.Utils;

/// <summary>
/// What the main window knows about its size boxes beyond their text: whether a preset's sizes
/// are pinned, and which boxes SIC! filled in itself. Kept out of the form so the rules can be
/// tested; the form calls one method per event and reads the two properties.
/// </summary>
internal sealed class ResizeFieldTracker {
    /// <summary>
    /// Whether a preset's sizes stay in the boxes when the selection changes. Set by applying a
    /// preset that resizes, cleared by any change the user makes to the resize controls.
    /// </summary>
    public bool SizesPinned { get; private set; }

    /// <summary>The boxes holding a value SIC! filled in rather than one the user typed.</summary>
    public DimensionBox AutoFilledBox { get; private set; }

    /// <summary>A preset was applied: its sizes are the user's choice, pinned when it resizes.</summary>
    public void PresetApplied(bool resizes) {
        SizesPinned = resizes;
        AutoFilledBox = DimensionBox.None;
    }

    /// <summary>SIC! filled in <paramref name="boxes"/> from the selected image.</summary>
    public void AutoFilled(DimensionBox boxes) => AutoFilledBox |= boxes;

    /// <summary>The user typed in <paramref name="box"/>: it is theirs now, and the pin is gone.</summary>
    public void UserEdited(DimensionBox box) {
        SizesPinned = false;
        AutoFilledBox &= ~box;
    }

    /// <summary>The user toggled Resize or switched the resize mode.</summary>
    public void ResizeSettingsChanged() => SizesPinned = false;
}
