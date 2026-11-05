using Oire.Sic.Utils.Enums;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic.Models;

/// <summary>
/// The <c>general</c> section of <c>sic.json</c>: every setting the Settings dialog edits, plus the
/// folder SIC! last opened images from.
/// </summary>
public sealed class GeneralSettings {
    /// <summary>The UI language: <c>"System"</c> to follow Windows, or a culture name such as
    /// <c>"de"</c>.</summary>
    public string Language { get; set; } = "System";

    /// <summary>The folder converted files are written to. At runtime this always holds a full path;
    /// the default <c>Converted</c> folder is saved as an empty string so a moved portable copy keeps
    /// using its own.</summary>
    public string OutputFolder { get; set; } = App.DefaultOutputFolder;

    /// <summary>When <c>true</c>, converted files are written next to their source file
    /// instead of into <see cref="OutputFolder"/> (issue #33). Items that have no source
    /// folder — clipboard captures and downloaded links — still fall back to
    /// <see cref="OutputFolder"/>. Opt-in; off by default.</summary>
    public bool SaveToSourceFolder { get; set; }

    /// <summary>The folder the Open dialog starts in, or empty when none was used yet.</summary>
    public string LastInputFolder { get; set; } = "";

    /// <summary>When <c>true</c>, closing SIC! with images still in the queue asks for confirmation.</summary>
    public bool ConfirmExitWithQueue { get; set; } = true;

    /// <summary>When <c>true</c>, the app performs a single silent update check shortly
    /// after the main window opens. A check that fails (no network, server down) is
    /// logged and otherwise ignored — it never interrupts startup or shows an error.</summary>
    public bool CheckForUpdatesOnStartup { get; set; } = true;

    /// <summary>When <c>true</c>, SIC! offers (via a prompt) to add usable clipboard
    /// content — raw image data, image files, or an image link — whenever the window opens
    /// or regains focus. Each distinct clipboard payload is offered at most once, so
    /// re-focusing never re-prompts for the same data. Opt-in; off by default.</summary>
    public bool DetectClipboardData { get; set; }

    /// <summary>SIC! format keys (e.g. <c>["JPG", "PNG", "WEBP"]</c>) to show in the
    /// target-format dropdown, letting users hide formats they never convert to (issue #47).
    /// An empty list means "show every supported format" — the default — so a fresh or upgraded
    /// config naturally exposes all formats. Read it via <see cref="GetEnabledFormatKeys"/>.</summary>
    public List<string> EnabledFormats { get; set; } = [];

    /// <summary>The configured <see cref="EnabledFormats"/> trimmed, with null and blank entries
    /// dropped. Empty when no restriction is set (all formats shown).</summary>
    public IEnumerable<string> GetEnabledFormatKeys() =>
        EnabledFormats
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim());

    /// <summary>How often the app checks for updates in the background while it runs.
    /// <see cref="UpdateCheckInterval.Never"/> disables the background loop. Independent of
    /// <see cref="CheckForUpdatesOnStartup"/>: either, both, or neither may be active.</summary>
    public UpdateCheckInterval UpdateCheckInterval { get; set; } = UpdateCheckInterval.Daily;
}
