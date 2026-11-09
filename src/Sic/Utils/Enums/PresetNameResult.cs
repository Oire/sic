namespace Oire.Sic.Utils.Enums;

/// <summary>
/// The outcome of <see cref="Services.PresetValidator.ValidateName"/>. Callers map each value to
/// their own message.
/// </summary>
public enum PresetNameResult {
    /// <summary>The name can be used.</summary>
    Valid,

    /// <summary>The name is empty or only white space.</summary>
    Empty,

    /// <summary>The name is longer than <see cref="Services.PresetValidator.MaxNameLength"/> characters.</summary>
    TooLong,

    /// <summary>The name holds a control character, such as a tab or a line break.</summary>
    InvalidCharacters,

    /// <summary>Another preset already has this name, ignoring case.</summary>
    Duplicate,
}
