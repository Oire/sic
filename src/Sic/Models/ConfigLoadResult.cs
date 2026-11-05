namespace Oire.Sic.Models;

/// <summary>
/// What loading the configuration produced: the settings to use, plus anything the user must be
/// told. The messages are complete and already translated, so callers show them unchanged.
/// </summary>
/// <param name="Config">The settings to use this session; defaults when nothing could be read.</param>
/// <param name="LoadError">A message about reading, repairing or migrating the file, or <c>null</c>.</param>
/// <param name="SaveError">A message about a failed write during loading, or <c>null</c>.</param>
/// <param name="SavesBlocked"><c>true</c> when the file on disk couldn't be read or backed up, so
/// saving must be skipped this session rather than replace it with defaults.</param>
public sealed record ConfigLoadResult(
    AppConfig Config, string? LoadError, string? SaveError, bool SavesBlocked);
