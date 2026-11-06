namespace Oire.Sic.Models;

/// <summary>
/// The whole of <c>sic.json</c>. Each top-level member is one section of the file; later plans add
/// sections (presets) here, and the serializer saves them without further changes.
/// </summary>
public sealed class AppConfig {
    /// <summary>The <c>general</c> section: the settings edited in the Settings dialog.</summary>
    public GeneralSettings General { get; set; } = new();
}
