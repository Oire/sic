# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SIC! (Simple Image Converter) is a Windows Forms desktop application (.NET 10.0, x64-only) by Oire Software. It converts images between formats (JPG, PNG, WEBP, ICO, BMP, TIFF, GIF, AVIF) with optional resizing, using Magick.NET as the image processing backend. HEIC/HEIF (iPhone photos) is supported as an **input** format only — the bundled Magick.NET build can decode HEIC but not encode it (no HEVC/x265 encoder), so HEIC is never offered as a conversion target (issue #30). The app has both a GUI mode and a headless CLI mode.

It uses structured logging (Serilog), a JSON configuration file (`System.Text.Json`, from the framework), and gettext-based localization (GetText.NET). Versioning is handled automatically via GitVersion (GitVersion.MsBuild) from git history.

## Build Commands

```bash
dotnet build                                   # Build the solution (Debug|x64 by default)
dotnet build -c Release                        # Release build
dotnet test                                    # Run the tests (tests/Sic.Tests, xUnit + AwesomeAssertions)
dotnet format --verify-no-changes              # Format check, as CI runs it
dotnet publish src/Sic/Sic.csproj -c Release   # Publish as single-file executable
```

Publish the csproj, not the solution: the solution also holds the test project, which must never reach the published output.

### Tests

`tests/Sic.Tests` covers the conversion engine, fit to file size, output paths, utilities, the CLI (through `Program.RunCli`, which is `internal` for that) and the `Sic.po` catalogs. CI runs `dotnet test --no-build` after the build. A few rules keep the suite honest:

- **Isolation through `userdata`.** An after-build target creates `userdata` (and `locale\de`) in the test output folder, and a `[ModuleInitializer]` (`TestAssemblySetup`) creates `userdata` again before any test runs, so `App.IsPortable` is true and `App.DataFolder` stays inside `bin` even under `dotnet test --no-build` after the folder was deleted. Tests never touch the real `%APPDATA%\Oire\Sic`: `TestDataFolder.Clear()`, which removes config files a test placed there, throws unless the data folder is inside the test output, and `TestIsolationTests` checks the same.
- **One non-parallel collection for static state.** `Config`, `Console` and `Localization` are static, and `Localization.SetLanguage` changes the culture of the whole process, so tests that touch them, or assert text returned by `_()`, go in `[Collection(StaticStateCollection.Name)]` and derive from `StaticStateTestBase`, which resets the language to `en-US`.
- **Never assert translated text.** `.mo` files aren't in git, and the catalog is named from `Environment.ProcessPath`, so under `dotnet test` translations never load. English text from `_()` is safe to assert; translated text is not. Don't assert the text of `System.CommandLine`'s own errors either, since it follows `CurrentUICulture`.
- **No binary fixtures.** `TestImages` generates images with Magick in a temporary folder. Size budgets come from a measured encode, never from constants, because Magick noise isn't seeded.
- A test that exposes a real bug stays, with `Skip = "⚠️ ..."` naming the problem, rather than being weakened.

### Localization scripts

Translation scripts live in `src/Sic/locale/scripts/`. Run them with PowerShell:

```bash
# Compile .po files to .mo (required after editing translations)
powershell -ExecutionPolicy Bypass -File src/Sic/locale/scripts/compile-translations.ps1

# Extract new translatable strings from source into messages.pot
powershell -ExecutionPolicy Bypass -File src/Sic/locale/scripts/Extract-Strings.ps1

# Merge new strings into existing .po files (preserves existing translations)
powershell -ExecutionPolicy Bypass -File src/Sic/locale/scripts/Update-Translations.ps1
```

After editing `.po` files, always run `compile-translations.ps1` to regenerate the `.mo` binaries.

**Form titles must be set in the constructor.** `GetText.Extractor` only recognizes *qualified* assignments in the designer (`okButton.Text = "&OK"`); a form's own bare `Text = "Add Folder";` is invisible to it, so the title never reaches the catalog and always renders in English. Every dialog therefore sets its own title right after `Localizer.Localize(this, Localization.Catalog)`:

```csharp
InitializeComponent();
Localizer.Localize(this, Localization.Catalog);
Text = _("Add Folder");
```

Keep the designer's English `Text` as-is — it's what the visual designer shows and the constructor simply reassigns it at runtime. `MainWindow` is the exception: its title is the product name and stays untranslated.

Note that `Update-Translations.ps1` fuzzy-matches new titles against the similarly worded menu items, so freshly merged title entries arrive with `&` mnemonics and trailing `...` and a `#, fuzzy` flag. Fix the `msgstr` and drop the flag — window titles carry neither.

`dotnet test` fails until every merged entry is translated in every language and no `#, fuzzy` flag is left, the header's included; `TranslationCatalogTests` checks all catalogs on disk, and also fails on a catalog whose header declares no `Plural-Forms`.

**Menu mnemonics are tested per level.** `TranslationCatalogTests` keeps one string array per level of `MainWindow.BuildMenuSpec` (`TopLevelMenu`, `FileMenu`, `EditMenu`, `ConvertMenu`, `HelpMenu`) and fails if two items on a level share a mnemonic in any language, because `NativeMenuSpec` throws `ArgumentException` at startup on a clash. When you add, rename or move a menu item, update the matching array too; `MenuLists_MatchBuildMenuSpec` reads `BuildMenuSpec` from `MainWindow.cs` and fails until the arrays match it. The shortcut label beside each item (`_("Ctrl+N")`) is translated display text too, so German shows `Strg+N`; the key itself comes from the `Keys` value next to it, which never changes.

**Languages ship as three parallel pieces**, and a new one is only half-added if any is missing: `locale/<code>/Sic.po` + `.mo` (UI strings), `help/<code>/manual.html` (user manual, resolved by culture name then two-letter code, falling back to `en`), and `installer/Languages/Custom.<code>.isl` wired into the `[Languages]` section of `installer/sic.iss`. Add the code to `SatelliteResourceLanguages` in `src/Sic/Sic.csproj` as well (not the test project, which keeps only `en` on purpose), or NuGet dependencies (NetSparkle's update UI) stay untranslated. The Settings language dropdown needs no code change — it lists every `locale/` subdirectory that holds a `Sic.mo`, named by the culture's native name.

Hebrew is right-to-left; see `Utils/TextDirection.cs` and `Utils/DialogHelper.cs` below.

## Architecture

**Entry point:** `src/Sic/Program.cs` — Sets up Serilog logging. If CLI arguments are present, runs headless conversion via `System.CommandLine`; otherwise takes the single-instance guard (`SingleInstanceGuard.TryAcquire`, held in `Program.InstanceGuard`), loads config and launches the WinForms `MainWindow`. When the guard is already taken, it calls `SingleInstanceGuard.ActivateExisting` and exits before it reads or writes the config. The CLI never takes the guard, so it runs beside the GUI.

### Core modules

**`src/Sic/Models/ImageItem.cs`** — Data model for an image in the conversion queue. Properties: `FilePath`, `ImageData` (for clipboard/URL sources), `OriginalFormat`, `FileName`, `Width`, `Height`, `FileSize`. Display helpers for dimensions and human-readable file sizes.

**`src/Sic/Models/AppConfig.cs`, `GeneralSettings.cs`, `ConfigLoadResult.cs`** — The configuration models. `AppConfig` is the whole of `sic.json`, one property per section (`General` today; presets will add a sibling, which `ConfigSerializer.Save` writes without further changes). `GeneralSettings` is the `general` section, with its defaults as property initializers. `ConfigLoadResult` is what `ConfigSerializer.Load` returns: the config, a `LoadError` and a `SaveError` (complete, translated messages, shown unchanged), and `SavesBlocked`.

**`src/Sic/Models/SizeFitProposal.cs`** — Plain data record describing one viable "fit to file size" result (issue #24): target `Format`, output `Width`/`Height`, `Quality` (null for lossless), resulting `FileSize`, a `Resized` flag, and a `Recommended` flag set by ranking. Produced by `ImageConverter.FindSizeFitProposals` and consumed by `ConvertToProposal`. (It lives in `Models`, not `Services`, because it's data, not behavior.)

**`src/Sic/Services/ImageConverter.cs`** — Static conversion engine wrapping Magick.NET. Key methods:
- `LoadFromFile`, `LoadFromStream`, `LoadFromBytes`, `LoadFromUrl` — create `ImageItem` from various sources
- `Convert` — converts an `ImageItem` to a target format with optional resize, writes to disk
- `CreateMultiSizeIco` — one ICO holding several sizes of an image (Convert > Create Multi-size ICO…)
- Every ICO, from `Convert` too, is written by `WriteIco`: Magick's encoder switches to PNG only *above* 256 px, whatever the compression or format settings, so the 256 px frame Windows uses comes out as a ~260 KB uncompressed bitmap. `WriteIco` lets Magick write the file (keeping its 512 px limit and its encoding of the smaller frames), then swaps the bitmap of any frame with a side of 256 px or more for the same pixels as a 32-bit PNG, as Windows' own icons store it, and writes 0 (meaning 256 or more) for that side in the directory. It goes by the frame's own size, not the directory's: Magick writes a 300x200 frame as a bitmap with a directory width of 44 (300 mod 256). Magick checks the 512 px limit on the first frame only, so `WriteIco` checks every frame itself first and throws `ArgumentException` without writing anything when one is over 512 px.
- ICO has no signature Magick detects, so bytes with no file name to go by (a pasted or downloaded icon) fail with "no decode delegate". Every decode of in-memory bytes therefore goes through `ReadImage`, which recognizes the ICO header (`IsIcoData`: reserved 0, type 1, at least one image), reads it with `MagickFormat.Ico` and keeps the largest frame, as `LoadFromFile` does for an `.ico` file. Such items report `OriginalFormat` "Ico".
- `GeneratePreview` — produces a `Bitmap` for the preview panel
- `GenerateOutputPath`, `GetConflictRenamePath` — output path logic with conflict resolution
- `GetSupportedFormats` — all SIC! format keys in canonical order; `GetEnabledFormats(enabledKeys)` filters them to the user-selected subset (issue #47), preserving order and never returning an empty list
- `FindSizeFitProposals(item, formats, maxBytes, maxWidth, progress)` / `ConvertToProposal(item, proposal, outputPath)` — "fit to file size" (issue #24). Trial encodes go to a `MemoryStream`, never to disk, and every advertised size is a real measurement at the dimensions and quality that will actually be written, so `ConvertToProposal` reproduces a chosen proposal byte-for-byte. Each format is tried two ways: **keep quality, shrink** (`FitAtQuality` — proportions always kept, optional `maxWidth` applied first, never below `MinFitDimension` = 16 px on the smaller side) and, for lossy formats (`JPG`/`WEBP`/`AVIF`, the `LossyFormats` set) that had to give up pixels, **keep every pixel, drop quality** (`FitFullSizeByQuality`, never below `MinFitQuality` = 45). Ranking is **resolution first, then quality, then size** — a full-resolution result at quality 88 beats a downscaled one at 95, which is what someone facing an upload limit actually wants; `ranked[0]` gets `Recommended`. An impossible budget yields an empty list rather than a garbage thumbnail. `progress` (an `IProgress<string>`) is told each format key as its turn starts, which is what drives the determinate progress bar.
  - Both searches **extrapolate rather than bisect**: encoded size tracks pixel count closely, so one cheap `ReferenceProbeLongSide` (384 px) probe yields a bytes-per-pixel estimate that aims every probe after it (`MaxFitProbes` = 8 bounds the worst case). Convergence is tested in output pixels, not in scale — a scale-based test stops far too early on formats whose size tracks pixel count exactly, and silently returns a thumbnail where a usable size existed.
  - The quality search never brute-forces full-resolution encodes (a single full-size AVIF costs ~2 s). It picks a quality on a `QualityProbeLongSide` (768 px) proxy against a pixel-scaled budget, confirms at full size, then **calibrates the proxy from that one real measurement** (`proxyBudget = maxBytes * proxySize / realSize`) and lets the cheap search choose again. `MaxQualityConfirmations` = 2 full-resolution encodes is enough to land on the same quality an exhaustive search finds.
  - Trial encodes are memoized per format in a `(width, height, quality) -> bytes` dictionary shared by both searches; without it the two searches re-encode the same candidates repeatedly.
- `GetSizeFitFormats(formats)` — drops `SizeFitExcludedFormats` (**ICO** and **GIF**) from a format list. Both stay ordinary conversion targets but are never fit-to-size candidates: Magick's ICO encoder hard-throws above **512 px per side** (so probing any normal photo would fail the whole search) and its payload flips between BMP and PNG around 256 px, making encoded size non-monotonic in the scale factor — the exact assumption the binary search relies on; GIF quantizes to ≤256 colors (measured: ~750,000 colors → 76) while ignoring `Quality` entirely, so a badly degraded result would be advertised as "full quality", and it encodes larger than JPG/WEBP at equal dimensions anyway. `FindSizeFitProposals` applies the filter defensively too. Unlike `GetEnabledFormats` this **can** return an empty list (user enabled only ICO/GIF), which `MainWindow` reports with its own message.
- Note: The class name conflicts with `System.Drawing.ImageConverter`; files that use it must import `using ImageConverter = Oire.Sic.Services.ImageConverter;`

**`src/Sic/Services/UnsupportedImageException.cs`** — Domain exception thrown when content downloads/loads fine but isn't a decodable image (e.g. a link to an HTML page). `LoadFromUrl` translates Magick.NET's decode failure into this so the UI can show a friendly "not an image" message without referencing Magick.NET — keeps the image-library dependency inside the Services layer.

### UI

**`src/Sic/MainWindow.cs` + `MainWindow.Designer.cs`** — Main application window. Menu bar (File, Edit, Convert, Help) + `TableLayoutPanel`-based layout with:
- `ListView` (batch queue) + `PictureBox` (image preview) in the top row
- Format dropdown, resize checkbox + resize mode / width / height fields in the controls rows
- Convert Selected + Convert All buttons
- Convert menu also offers *Create Multi-size ICO…* and *Fit to File Size…* (issue #24), each acting on the single selected image. *Fit to File Size* opens `FitToSizeDialog` (max size in KB + optional max width, both plain `TextBox`es like the resize fields), runs `ImageConverter.FindSizeFitProposals` on a background thread behind `ProgressDialog`, then shows `FitToSizeResultsDialog` (a `ListBox` of ranked proposals) and converts the chosen one through the standard output-path + conflict-resolution flow. The search reports each format as it starts, so the progress bar is determinate ("Checking AVIF (6/6)…") rather than a marquee — it can still take tens of seconds on a large photo, and AVIF is by far the slowest. Cancellation lands in about a quarter of a second. The `Progress<string>` callback checks `IsDisposed` first: reports are posted to the UI thread asynchronously, so one queued just before a cancel can arrive after `finally` disposed the dialog.
- `StatusStrip` with status label at the bottom
- Batch operations show a separate `ProgressDialog` with progress bar
- Supports drag & drop, Ctrl+V paste (file drops and bitmap clipboard), Delete key to remove
- Clipboard auto-detection (issue #36, opt-in via `DetectClipboardData`): on `OnShown` and every `Activated`, `CheckClipboardForImport()` offers (Yes/No) to add image data, image files, or an image link from the clipboard. Two gates prevent nagging — the Win32 clipboard sequence number (`GetClipboardSequenceNumber`, P/Invoked) skips unchanged clipboards, and a content signature (hash of image bytes / sorted paths / URL) suppresses re-prompts when the same content is re-copied. The passive detector only offers image-extension URLs; manual Ctrl+V accepts any http(s) link.
- Second-launch activation: `WndProc` watches for `SingleInstanceGuard.GetActivationMessage(App.DataFolder)` (a static field, so it's set before the first message arrives) and `BeginInvoke`s `ActivateFromSecondLaunch`, which restores the window with `SW_RESTORE` if it's minimized (keeping a maximized window maximized) and calls `SetForegroundWindow(GetLastActivePopup(Handle))`, so an open modal dialog gets focus rather than the disabled main window. Both steps are best effort; Windows may only flash the taskbar button.

**`src/Sic/SettingsDialog.cs` + `SettingsDialog.Designer.cs`** — Settings form. A `TabControl` (filling the form, OK/Cancel beneath) with two tabs, each its own flat `TableLayoutPanel`:
- **General** — language dropdown, confirm-exit checkbox, "check for updates on startup" checkbox, background update-frequency dropdown.
- **Images** — output folder (textbox + browse + reset), a "save converted images in the same folder as the original" checkbox (issue #33), a "detect data in clipboard" checkbox (issue #36), and a target-formats checklist that selects which formats appear in the main window's dropdown (issue #47). Built as individual `CheckBox` controls stacked in a `TableLayoutPanel` (`formatsPanel`, filled at runtime in `PopulateFormats()`) inside a `GroupBox` (`formatsGroupBox`) — *not* a `CheckedListBox`, which doesn't reliably announce toggle state changes to screen readers. The group box caption (rather than a separate label) supplies the accessible group name; each checkbox is its own tab stop. OK requires at least one format ticked; when all are ticked it saves `EnabledFormats` as empty so formats added in future versions show automatically.

Tab page `Text` doesn't honor `&` mnemonics — navigate tabs with Ctrl+Tab / Ctrl+PgUp/Dn. Exposes `UpdatePeriodicCheckChanged` so `MainWindow` can re-arm the background update loop live (without a restart) when the frequency changes.

### Utilities (`src/Sic/Utils/`)

- `Config.cs` — The static settings the app runs with: `Config.General` (a `GeneralSettings`), `Load(isGui)` and `Save()`. It holds no file logic, only routing: `Load` calls `ConfigSerializer.Load(App.DataFolder, App.IsPortable)` and reports its `LoadError`/`SaveError` unchanged (never wrapped) through `DialogHelper` in the GUI or stderr in the CLI; `Save` follows the `isGui` given to `Load`, so a CLI save failure never opens a message box. When the load set `SavesBlocked`, `Save()` only logs, so a file that couldn't be read is never replaced with defaults. The settings are Language, OutputFolder, SaveToSourceFolder, LastInputFolder, ConfirmExitWithQueue, CheckForUpdatesOnStartup, UpdateCheckInterval, DetectClipboardData and EnabledFormats. `SaveToSourceFolder` (issue #33), when on, writes converted files next to each original instead of into `OutputFolder`; clipboard/URL items have no source folder and fall back to `OutputFolder`. `EnabledFormats` is a list of SIC! format keys shown in the target dropdown (empty = all); read it via `GetEnabledFormatKeys()` and filter with `ImageConverter.GetEnabledFormats(...)`. At runtime `OutputFolder` always holds a full path.
- `ConfigSerializer.cs` — Reads and writes `sic.json` (`App.ConfigFileName`) and shows no UI. Its `JsonSerializerOptions` are camelCase, indented, case-insensitive on read, tolerant of comments and trailing commas (comments aren't kept on save), write non-ASCII paths unescaped, and refuse integers for enums (`JsonStringEnumConverter(allowIntegerValues: false)`), so `"updateCheckInterval": 99` can't silently turn update checks off. The file is UTF-8 without a BOM; a BOM is accepted on read, and invalid UTF-8 counts as invalid JSON. `Load(folder, isPortable)`:
  - `sic.json` is parsed with `JsonDocument` and read one value at a time, matching names case-insensitively, so a bad value (wrong type, unknown enum name, invalid language) keeps only its own default. If any was bad, the file is first copied by `BackUpFile`, then rewritten, so the next start is silent. A file that isn't valid JSON, or whose root isn't an object, is kept the same way and replaced with defaults. If the backup fails, nothing is written and `SavesBlocked` is set. A file that can't be read at all (I/O or access error) is left untouched, with `SavesBlocked` set.
  - `BackUpFile` copies to `sic.json.bad`, first renaming an existing `.bad` to `.bad.<yyyyMMdd-HHmmss>` (with `-N` added on a clash), so no backup is ever overwritten.
  - With no `sic.json`, a legacy `Sic.cfg` (`App.LegacyConfigFileName`) is read by `LegacyConfigReader` and written as `sic.json`; only after that write succeeds is `Sic.cfg` renamed to `Sic.cfg.bak`. If it can't be read, defaults are used, `SavesBlocked` is set and `Sic.cfg` stays, so the next run tries again. In portable mode, a migrated `OutputFolder` ending in `\userdata\Converted` counts as the default, so a 1.1 `userdata` copied into a new folder stops pointing at the old copy.
  - With neither file, the data folder is created and defaults are written.
  - Every path ends with the same clean-up: null strings and a null `general` become defaults, `enabledFormats` loses null and blank entries, a blank `outputFolder` becomes `App.DefaultOutputFolder`, and a `language` that isn't `System` or a valid culture name (`Localization.IsValidLanguageName`) becomes `System`.
  - `Save(path, config)` serializes to a `JsonNode`, writes `general.outputFolder` as `""` when it equals `App.DefaultOutputFolder` (ignoring case and trailing separators) without touching the object it was given, so a moved portable copy keeps using its own `Converted` folder. It writes to a unique `sic.json.<guid>.tmp` in the same folder, flushes to disk, then `File.Move`s it over `sic.json` with a few short retries (a parallel CLI or an antivirus scanner may hold the file), deleting the temporary file on failure.
- `LegacyConfigReader.cs` — A small hand parser for the `Sic.cfg` INI files SharpConfig wrote up to 1.1 (`Read(path)`, `Parse(content)`). Only keys in `[General]` are read, case-insensitively; values have surrounding quotes stripped and backslashes kept literally; a comment starts at a `#` or `;` outside quotes and not after a backslash; bools take SharpConfig's spellings; `EnabledFormats` is split at commas. A malformed line or bad value is skipped with a logged warning, where SharpConfig discarded the whole file. The fixtures in `tests/Sic.Tests/Fixtures/LegacyConfig` were written by the real SharpConfig before it was removed; keep them, since the package is no longer there to regenerate them.
- `SingleInstanceGuard.cs` — One GUI per data folder, because two windows sharing one `sic.json` would overwrite each other's changes. `GetInstanceKey(dataFolder)` is the first 16 hex digits of the SHA-256 of the upper-cased full path without a trailing backslash; keying on the data folder lets separate portable copies, or a portable and an installed copy, run side by side. `TryAcquire` creates the `Local\Oire.Sic.<key>` mutex with `initiallyOwned: true` and returns null when it already existed, or when an elevated copy's mutex can't be opened (`UnauthorizedAccessException`, logged, so a normal launch beside an elevated copy doesn't show a false startup error). It never waits on the mutex, and `Dispose` only closes the handle, never `ReleaseMutex` (which throws off the owning thread; the portable updater may dispose from another thread); it's idempotent. The mutex is a kernel object that disappears with the last handle, so it leaves nothing behind in portable mode. `ActivateExisting` calls `AllowSetForegroundWindow(ASFW_ANY)` and posts `GetActivationMessage(dataFolder)` (`RegisterWindowMessage("Oire.Sic.Activate.<key>")`) to `HWND_BROADCAST`; Windows blocks it when the running copy is elevated. `Local\` is per session, so one portable folder used from two Windows sessions isn't caught. Arguments mean CLI mode today, so a second launch forwards nothing; opening files passed to the exe in the running window (issue #35) will need to send the paths along with the activation, for instance through `WM_COPYDATA`, before `Program.Main` treats them as a CLI run.
- `FileHelper.cs` — Cloud placeholder detection (OneDrive/SharePoint recall attributes) and image file enumeration with glob patterns.
- `UrlHelper.cs` — Single source of truth for link validation: `IsValidHttpUrl(text, out url)` trims input and checks for an absolute http(s) URL. Used by the "Add by link" dialog, Ctrl+V paste, and clipboard auto-detection so all three validate links identically.
- `Localization.cs` — Wraps GetText.NET with convenience methods: `_()`, `_n()`, `_p()`, `_pn()` for translations. Loads `.mo` files from the `locale/` folder relative to the executable. Falls back through language parents to `en-US`. `HasLocaleFolder` ignores an empty culture name: a neutral culture such as `pl` has the invariant culture as its parent, and its empty name would otherwise make `Path.Combine(LocalesFolder, "")` the locale folder itself, which always exists.
- `TextDirection.cs` — Right-to-left support (Hebrew). `Apply(form)` sets `RightToLeft` from the active culture and walks the control tree setting `RightToLeftLayout`, which — unlike `RightToLeft` — is *not* ambient and only exists on `Form`, `ListView`, `TabControl` and `ProgressBar`. Every form calls it immediately after `Localizer.Localize`, and `MainWindow.ApplyLocalization` calls it again so a live language switch re-mirrors the window. The hand-built file-conflict dialog in `MainWindow.ResolveFileConflict` calls it too.
- `DialogHelper.cs` — `Show(text, caption, buttons, icon)`, a one-method wrapper over `MessageBox.Show`. WinForms mirrors a message box only when passed `MessageBoxOptions.RtlReading | RightAlign`, and those cannot be inherited from the owning form, so **every** message box in SIC! goes through here — never call `MessageBox.Show` directly.
- `Constants/App.cs` — Application metadata, data folder paths (`%APPDATA%/Oire/Sic/`, or `userdata` in portable mode), and the config file names (`ConfigFileName` = `sic.json`, `LegacyConfigFileName` = `Sic.cfg`).
- `Constants/ExitCode.cs` — CLI exit code constants (`Success`, `Error`, `Canceled`).
- `Constants/Logging.cs` — Log file paths and output templates. Logs go to `%APPDATA%/Oire/Sic/logs/`.
- `Enums/UpdateCheckInterval.cs` — Background update-check frequency (`Daily`, `EveryThreeDays`, `Weekly`, `Monthly`, `Never`). Declared most-frequent-first so `Daily` is the default zero value and the order matches the Settings combo box.

### Key dependencies

| Package | Purpose |
|---------|---------|
| Magick.NET-Q16-x64 | Image loading, conversion, resizing (16-bit per channel) |
| System.CommandLine | CLI argument parsing |
| Serilog (+File, +Compact) | Structured logging |
| System.Text.Json (framework, no package) | `sic.json` config file |
| GetText.NET | Localization |
| GitVersion.MsBuild | Automatic versioning from git |

## CLI Mode

When launched with arguments, the app runs headless (no UI). Format is inferred from the output file extension.

```bash
sic -i input.png -o output.jpg                    # Convert PNG to JPG
sic --input photo.bmp --output photo.webp          # Convert BMP to WebP
sic -i avatar.png -o avatar.ico --resize 128x128   # Convert + resize
```

Options: `--input`/`-i` (required), `--output`/`-o` or `--format`/`-f` (one required), `--resize`/`-r` (optional, WxH/Wx/xH format), `--crop`/`-c` (optional, requires both W and H).

## Product Spec (v1)

SIC! is an accessible image format converter primarily aimed at blind and low-confidence users, but designed to be visually appealing enough for sighted users too.

### Core workflow

1. **Main window** = a list of images (batch queue). Empty on launch.
2. **Adding images** via:
   - Drag & drop files onto the list
   - Ctrl+V paste (both clipboard files like Outlook attachments, and raw bitmap data like PrintScreen screenshots)
   - "Open file" button/dialog
   - Add by link (downloads the image and adds it to the queue)
3. **Pick target format** from a dropdown (JPG, PNG, WEBP, ICO, BMP, TIFF, GIF, AVIF).
4. **Optional resize** — checkbox that reveals width/height fields (critical for blind users who get told "upload a 128x128 photo").
5. **Hit Convert** — processes all items in the list to the chosen format.
6. **Output location** — by default, `%APPDATA%\Oire\Sic\Converted\` (or `userdata\Converted\` in portable mode), same name with new extension. Configurable in settings to use a custom output folder.
7. **Filename conflict** — always ask (overwrite / rename to `_1` suffix / skip). Never silently overwrite.

### UI guidelines

- **Use `TableLayoutPanel` throughout** — the developer is blind and needs to adjust layouts without counting pixel coordinates. Use flat layouts (no nesting) with percentage-based column styles where possible.
- **Image preview panel** — sighted users should see a preview of the selected image. This is not a blind-only tool; it should look and feel like a proper app a sighted person would also choose to use.
- **Resize controls** — consider visual resize handles or interactive controls for sighted users in addition to the WxH text fields.
- **Accessibility first** — proper tab order, meaningful labels, keyboard accelerators (underlined letters via `&`) on all interactive controls. Do **not** set `AccessibleName` on controls that already have a meaningful `Text` property — it overrides what screen readers use. Only set `AccessibleName` on controls without descriptive text (e.g., `ListView`).
- **ListView re-entrancy** — WinForms `ListView` raises `SelectedIndexChanged` synchronously from inside its own `WndProc` while items are being removed. **Never mutate `imageListView.Items` (insert, remove, clear) from within `SelectedIndexChanged`, or from any MessageBox pump that runs while the list is in an intermediate state** — this causes internal `NullReferenceException`s in `ListView.WndProc`. `UpdateMenuState()` is safe to call from event handlers (only toggles `Enabled` flags); placeholder row management is split into `UpdatePlaceholderState()` which is only called from the tail of user-initiated add/remove flows.
- **Empty-list placeholder** — when `_imageItems` is empty, a fake `ListViewItem` ("Add your images here") is inserted and marked `Selected`/`Focused` so screen readers announce the empty state. All handlers that index into `_imageItems` via `SelectedIndices` must guard on `_imageItems.Count > 0` so Delete/F5/Ctrl+Alt+F5 are no-ops when only the placeholder is present.
- **WinForms internal NRE safety net** — `Program.cs` installs an `Application.ThreadException` handler that suppresses `NullReferenceException`s whose call site is inside `System.Windows.Forms.*` (notably the `ListView.Unhook` race during `Form.Dispose`, which is a known framework bug). Other UI-thread exceptions are logged and surfaced via a non-fatal MessageBox instead of killing the app.

### Settings (stored as JSON in `%APPDATA%/Oire/Sic/sic.json`, or `userdata\sic.json` in portable mode)

The file has one `general` object with camelCase keys. A `Sic.cfg` from 1.1 or earlier migrates on first start and is kept as `Sic.cfg.bak`; renaming that back to `Sic.cfg` is how a downgrade to 1.1 gets the old settings.

```json
{
  "general": {
    "language": "System",
    "outputFolder": "",
    "saveToSourceFolder": false,
    "lastInputFolder": "",
    "confirmExitWithQueue": true,
    "checkForUpdatesOnStartup": true,
    "detectClipboardData": false,
    "enabledFormats": [],
    "updateCheckInterval": "Daily"
  }
}
```

- Output folder (default: `Converted` subfolder in the data directory, saved as `""` so a moved portable copy keeps its own). A missing custom folder is reset for good in the GUI, after a warning; the CLI uses the default for that run only and doesn't save, so an unplugged drive doesn't lose the setting.
- Save converted images in the same folder as the original (`SaveToSourceFolder`, default: off) — when on, each converted file is written next to its source file instead of into the output folder (issue #33). Clipboard captures and downloaded links have no source folder, so they still go to the output folder.
- Language
- Confirm exit when images are in the queue
- Check for updates on startup (default: enabled) — a single silent check shortly after launch
- Background update-check frequency (`UpdateCheckInterval`: Daily / EveryThreeDays / Weekly / Monthly / Never; default Daily)
- Detect data in clipboard (`DetectClipboardData`, default: off) — when on, SIC! offers (via a Yes/No prompt) to add usable clipboard content (raw image, image files, or an image link) when the window opens or regains focus. Deduplicated by the Win32 clipboard sequence number so the same payload is offered at most once.
- Target formats to show (`EnabledFormats`, default: empty = all) — a JSON array of format keys, such as `["JPG", "PNG"]` (the old `Sic.cfg` stored it comma-separated; migration splits it), to display in the target-format dropdown, letting users hide formats they never convert to (issue #47). Empty means every supported format; at least one must stay selected.

## Auto-Updates (NetSparkleUpdater)

The app uses [NetSparkleUpdater](https://github.com/NetSparkleUpdater/NetSparkle) with Ed25519 signature verification for automatic updates.

### Key setup

Updates are signed with an Ed25519 key pair. The public key is embedded in `src/Sic/Utils/Constants/App.cs` (`UpdatePublicKey`). The private key is **not** checked into the repo — it lives in a `keys/` directory at the repo root (gitignored).

To generate a new key pair (only needed once, or if forking the project):

```bash
# Install the appcast tool globally
dotnet tool install --global NetSparkleUpdater.Tools.AppCastGenerator

# Generate keys into the keys/ directory
netsparkle-generate-appcast --generate-keys --key-path keys
```

This creates `keys/NetSparkle_Ed25519.priv` and `keys/NetSparkle_Ed25519.pub`. After generating, update the `UpdatePublicKey` constant in `App.cs` with the contents of the `.pub` file, and update `AppcastUrl` if hosting elsewhere.

### Building a release

The `installer/Build-Installer.ps1` script handles the full release pipeline:

```powershell
.\installer\Build-Installer.ps1                        # Build app + installer only
.\installer\Build-Installer.ps1 -Appcast               # Also generate signed appcast
.\installer\Build-Installer.ps1 -Appcast -Deploy       # Build, appcast, and deploy via SCP
.\installer\Build-Installer.ps1 -Appcast -NoPortable   # Skip portable ZIP (e.g., for beta releases)
.\installer\Build-Installer.ps1 -SkipBuild             # Installer only (use existing binaries)
.\installer\Build-Installer.ps1 -NoSign                # Unsigned build, even if signing.json exists
```

### Code signing

Releases are Authenticode-signed with **Azure Artifact Signing** (issue #86). The script signs whenever `installer/signing.json` (gitignored; copy `signing.example.json`) exists: `sic.exe` right after publish — so the installer and the portable ZIP both carry the signed exe — then the installer and its uninstaller through Inno Setup's own signing (`sic.iss` enables `SignTool=azure` only under `/DSign`, which the script passes along with the `/Sazure=` command). Every signed file is checked with `Get-AuthenticodeSignature` and the build fails on anything but `Valid`. The appcast is generated afterwards, which matters: its Ed25519 signature covers the installer's bytes, and Authenticode signing changes them. Without `signing.json`, a plain build warns and continues unsigned, but `-Appcast`/`-Deploy` refuse to run unless `-NoSign` says so.

`signing.json` is the signtool plugin's own metadata file (`Endpoint`, `CodeSigningAccountName`, `CertificateProfileName`, `ExcludeCredentials`), passed as-is via `/dmdf`. Prerequisites on the build machine: `signtool.exe` from the Windows SDK, the Artifact Signing client tools (`winget install --id Microsoft.Azure.ArtifactSigningClientTools --exact`, which provide `Azure.CodeSigning.Dlib.dll` and need .NET 8+), and the signing service principal (below). Never drop the RFC 3161 timestamp (`/tr`): Artifact Signing certificates live about three days, and the timestamp is what keeps old signatures valid. `Magick.Native-Q16-x64.dll` already carries ImageMagick's signature and is left alone.

The build signs in as a **service principal**, never as a person, so it needs no browser and no second factor. The tenant's security defaults block device code sign-in outright (`AADSTS530035`, even for an existing session that came from one), and a person's session runs out. Each build machine has its own principal, `oire-signing-<COMPUTERNAME>`, shared by every Oire repository on that machine (PlanCake signs with the same one) and holding only the Artifact Signing Certificate Profile Signer role on the `oire-software` profile. Its certificate, private key included, lives in `%USERPROFILE%\.oire\signing\oire-signing-<COMPUTERNAME>.pem`, readable by that user only and never in a repository: it signs as Oire Software. Three user environment variables point to it: `AZURE_TENANT_ID`, `AZURE_CLIENT_ID` and `AZURE_CLIENT_CERTIFICATE_PATH`. With `AZURE_CLIENT_ID` set the script checks the certificate file and skips `az`; a program started before the variables were set (a running Claude Code session) does not see them.

The certificate is valid for two years (the one on ANDRE-ACCESSMIN until 2028-11-04). To renew it, or to set up a new machine, sign in once with `az config set core.enable_broker_on_windows=false` and `az login --tenant <tenant>` (the browser sign-in; the Windows account picker az shows by default is not screen-reader accessible), then `az ad sp create-for-rbac --name oire-signing-<COMPUTERNAME> --role "Artifact Signing Certificate Profile Signer" --scopes <profile id> --create-cert --years 2` — from PowerShell, not Git Bash, which rewrites the `/subscriptions/...` scope into a Windows path. It writes the certificate into the home folder; move it into `.oire\signing\`, set the three variables, and `az logout`.

The certificate subject is `Oire Software` — the company name exactly as registered, without the legal form. Keep the publisher name in `sic.iss`, the csproj and the About dialog identical to it.

The script produces in `installer/Output/`:
- `sic-v{version}-setup.exe` — Inno Setup installer
- `sic-v{version}-portable.zip` — portable archive (unless `-NoPortable`)
- `appcast.xml` and `appcast.xml.signature` (if `-Appcast`)

The portable ZIP is compressed with 7-Zip (ultra settings) if available, otherwise falls back to built-in `Compress-Archive` with a warning.

### Deploying

The `-Deploy` switch uploads release files to the hosting server via SCP. It reads SSH connection details from `installer/deploy.json` (gitignored). Copy `installer/deploy.example.json` to get started:

```json
{
    "SshHost": "your-ssh-host-alias",
    "RemotePath": "/path/to/your/remote/folder/"
}
```

`SshHost` should reference a host alias defined in your SSH config (`~/.ssh/config`).

### Release notes

Per-version changelogs go in a `changelogs/` directory at the repo root (e.g., `changelogs/1.0.0.md`). The appcast generator embeds them in the appcast XML.

### Updating winget manifests

The app is published to winget as `Oire.Sic`. After a new GitHub release, update the manifest with `wingetcreate`. Because the app is x64-only but wingetcreate misdetects Inno Setup installers as x86, you **must** append `|x64` to the URL to override the architecture:

```bash
wingetcreate update -u 'https://github.com/Oire/sic/releases/download/v<VERSION>/sic-v<VERSION>-setup.exe|x64' -v <VERSION> --submit --token "$(gh auth token)" Oire.Sic
```

Without the `|x64` suffix the command fails with a "Multiple matches" error.

**Next update only:** the manifests already in `microsoft/winget-pkgs` still say `Oire Software SARL` (the local copies in `manifests/` were corrected), and `wingetcreate update` carries the previous version's locale files over. Run it once with `--out .` instead of `--submit`, set `Publisher: Oire Software` and `Copyright: Copyright © 2026 Oire Software.` in every locale file of the new `manifests/o/Oire/Sic/<VERSION>/` folder, then submit it with `wingetcreate submit --token "$(gh auth token)" manifests/o/Oire/Sic/<VERSION>`. Delete this paragraph afterwards.

## Code Conventions

- **Namespaces:** `Oire.Sic`, `Oire.Sic.Models`, `Oire.Sic.Services`, `Oire.Sic.Utils`, `Oire.Sic.Utils.Constants`, and `Oire.Sic.Tests` (tests/Sic.Tests)
- **Nullable reference types** are enabled; **all warnings are treated as errors** — code must compile cleanly
- **Implicit usings** are enabled (no explicit `using System;` etc.)
- **.NET analyzers** at latest analysis level are enforced
- Debug builds include full symbols and extra telemetry logging; Release builds are optimized with no symbols
- Locale `.mo` files under `locale/` are copied to output via `<None Update>` in the csproj
- The project uses a code formatter/linter — expect same-line opening braces (no newline before `{`, `else`, `catch`, `finally`) in committed code
