namespace Oire.Sic.Utils.Enums;

/// <summary>
/// The outcome of <see cref="Services.ResizeValidator.Validate"/>. Callers map each value to their
/// own message: a dialog that focuses the bad box in the GUI, a line on stderr in the CLI.
/// </summary>
public enum ResizeError {
    /// <summary>The sizes are usable.</summary>
    None,

    /// <summary>The width box holds text that isn't a whole number from 1 to 65535.</summary>
    InvalidWidth,

    /// <summary>The height box holds text that isn't a whole number from 1 to 65535.</summary>
    InvalidHeight,

    /// <summary>Keep proportions with both boxes blank.</summary>
    MissingDimension,

    /// <summary>Crop with a blank width.</summary>
    CropNeedsWidth,

    /// <summary>Crop with a blank height.</summary>
    CropNeedsHeight,
}
