namespace Oire.Sic.Models;

/// <summary>
/// The whole of <c>sic.json</c>. Each top-level member is one section of the file, and the
/// serializer saves every section without further changes.
/// </summary>
public sealed class AppConfig {
    /// <summary>The <c>general</c> section: the settings edited in the Settings dialog.</summary>
    public GeneralSettings General { get; set; } = new();

    /// <summary>The <c>presets</c> section: the user's conversion presets, in the user's order
    /// (issue #23). An array rather than an object keyed by name, so the order is kept.</summary>
    public List<ConversionPreset> Presets { get; set; } = [];
}
