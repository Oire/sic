namespace Oire.Sic.Utils.Enums;

/// <summary>
/// Which of the main window's size boxes hold a value SIC! filled in itself, from the selected
/// image's proportions, rather than one the user typed. <see cref="Services.PresetCapturer"/>
/// leaves those values out of a new preset, because they were calculated for one image.
/// </summary>
public enum DimensionBox {
    /// <summary>The user typed everything in both boxes, or they are empty.</summary>
    None,

    /// <summary>SIC! filled in the width box.</summary>
    Width,

    /// <summary>SIC! filled in the height box.</summary>
    Height,

    /// <summary>SIC! filled in both boxes, from the selected image's own size.</summary>
    Both,
}
