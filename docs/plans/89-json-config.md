# JSON configuration and a single running instance

## Overview

This plan replaces SharpConfig's INI file with one JSON file, `%APPDATA%\Oire\Sic\sic.json` (or `userdata\sic.json` in portable mode), backed by POCOs. Existing `Sic.cfg` files migrate automatically, and the SharpConfig package goes away.

It also makes the GUI single-instance per data folder. Two windows sharing one config file would overwrite each other's changes, and presets make that loss matter. Starting SIC! a second time now brings the running window forward instead. The CLI stays free to run in parallel.

It's the second of four plans. `88-test-coverage` must be merged first. `23-custom-presets` then adds a `presets` sub-object to this file, and `90-portable-no-traces` builds on the guard.

## Done when

- [ ] A user with an existing `Sic.cfg` keeps every setting after upgrading, and the old file becomes `Sic.cfg.bak`.
- [ ] One bad value in `Sic.cfg` or `sic.json` loses only that value, not the other settings, and the file is backed up first.
- [ ] An invalid `language` value falls back to the system language instead of stopping SIC! at startup.
- [ ] A `sic.json` that isn't valid JSON is kept as `sic.json.bad`, reported to the user, and never silently overwritten. No backup is ever overwritten.
- [ ] A `sic.json` that can't be read is never replaced, and the user is told changes won't be saved this session.
- [ ] A failed config save in CLI mode writes to stderr and never opens a message box.
- [ ] A CLI run with a missing output folder uses the default for that run and doesn't change the saved setting.
- [ ] A portable copy that moves or changes drive letter keeps using its own `Converted` folder, including a 1.1 `userdata` folder copied into a new 1.2 folder.
- [ ] `sharpconfig-huanlin` is no longer referenced in the code, the project file or the docs.
- [ ] Starting SIC! while it's already running brings the existing window forward, even with a modal dialog open, and starts no second window.
- [ ] Two portable copies in different folders, or a portable and an installed copy, can run side by side.
- [ ] The CLI still runs while the GUI is open.
- [ ] all validation commands pass

## Validation commands

Run from the repository root.

- build: `dotnet build`
- test: `dotnet test`
- format check: `dotnet format --verify-no-changes`
- compile translations (after any `.po` change): `powershell -ExecutionPolicy Bypass -File src/Sic/locale/scripts/Compile-Translations.ps1`

There are no e2e tests. Forms are checked by hand; see Post-completion.

## Context

### Files involved

- Config: `src/Sic/Utils/Config.cs`, `src/Sic/Utils/Constants/App.cs`
- Call sites, unchanged in use: `src/Sic/Program.cs`, `MainWindow.cs`, `SettingsDialog.cs`, `AddFolderDialog.cs`
- GUI startup: `Program.Main`; window messages: `MainWindow.cs`, which already has a `[DllImport("user32.dll")]` at line 43
- Project: `src/Sic/Sic.csproj`
- Docs: `README.md`, `CHANGELOG.md`, `changelogs/1.2.0.0.md`, `CLAUDE.md`, `src/Sic/help/*/manual.html`
- Translations: `src/Sic/locale/{de,es,fr,he,ru,uk}/Sic.po`

Unaffected, already checked: `installer/sic.iss` (its uninstaller removes the whole data folder, so new file names are covered), NetSparkle (its state is in the registry) and the winget manifests.

### Patterns to follow

- Error reporting: `Config.ReportError`, which routes to `DialogHelper.Show` in the GUI and `Console.Error` in the CLI.
- Tests touching `Config`, `Console` or `Localization`: the `StaticStateCollection` from `88-test-coverage`.
- Tests of the CLI: `tests/Sic.Tests/CliTests.cs`, calling `Program.RunCli`. `TestDataFolder.Clear()` removes config files from `App.DataFolder` between tests.

### Conventions and traps

- `.editorconfig` requires CRLF, so run `dotnet format` on new `.cs` files before the format check.
- `.editorconfig` raises whole analyzer categories, and it applies to `tests/` too. Warnings are errors. CA1869 applies: make `JsonSerializerOptions` a `static readonly` field.
- Same-line braces, and `class X: Form` with no space before the colon. `_` is the gettext method, so `_ = ...` is not a discard.
- Every message box goes through `DialogHelper.Show`, never `MessageBox.Show`.
- Data records go in `Models`. Services are named `...Service`, `-or` or `-er`; other helpers go in `Utils`.
- `Config.Save()` hardcodes `isGui: true` today, so a CLI save failure opens a message box. Task 4 fixes that.
- Never use `/dev/null`, `nul` or any other null device in a shell command.

### Dependencies

- Remove `sharpconfig-huanlin`. Its package is what makes Task 2's fixtures possible, so capture them before Task 4 removes it.
- `System.Text.Json` comes with the framework; add no package. The app isn't trimmed, so reflection-based serialization is fine.

## Development approach

- One task at a time; the validation commands pass before the next task starts.
- Code changes come with tests for the new and changed behavior, success and error paths. Keep logic in plain classes that can be tested and keep form code thin.
- Keep backward compatibility: every existing config must migrate. The only intended behavior changes are listed under Technical details.
- When scope changes, update this plan: new tasks get a "➕" prefix, blockers a "⚠️" prefix.

## Implementation steps

### Task 1: Add the configuration models

#### Files

- Create: `src/Sic/Models/GeneralSettings.cs`
- Create: `src/Sic/Models/AppConfig.cs`
- Create: `src/Sic/Models/ConfigLoadResult.cs`
- Create: `tests/Sic.Tests/GeneralSettingsTests.cs`

#### Steps

- [ ] Add `GeneralSettings` with the same nine properties, defaults and XML docs as `Config.SectionGeneral`, plus `GetEnabledFormatKeys()`. Leave `Config.cs` alone until Task 4.
- [ ] Add `AppConfig` and `ConfigLoadResult` as described under Technical details.
- [ ] tests for `GeneralSettings` defaults, and for `GetEnabledFormatKeys`: empty, spaces and blank entries
- [ ] validation commands pass

### Task 2: Read legacy Sic.cfg files

#### Files

- Create: `tests/Sic.Tests/Fixtures/LegacyConfig/*.cfg`
- Create: `src/Sic/Utils/LegacyConfigReader.cs`
- Create: `tests/Sic.Tests/LegacyConfigReaderTests.cs`
- Modify: `tests/Sic.Tests/Sic.Tests.csproj`

#### Steps

- [ ] While the package is still referenced, write a temporary test that saves each path vector under Technical details with the real SharpConfig (`Section.FromObject("General", settings)` on `Config.SectionGeneral`, then `SaveToFile`). Commit the captured files as literal fixtures, delete the temporary test, and copy the fixtures to the test output. Add a hand-written four-key fixture with an empty `LastInputFolder=` line, matching the oldest real files.
- [ ] Add `LegacyConfigReader.Parse(string content)` returning `GeneralSettings`. It's a small hand parser following the rules under Technical details, and it does not use SharpConfig. `Read(path)` loads the file and calls `Parse`, letting I/O exceptions propagate.
- [ ] A bad value for one key keeps that key's default, logs a warning and carries on.
- [ ] tests for every captured fixture: the exact value comes back, including a UTF-8 BOM added by the test
- [ ] tests for the older four-key file: missing keys keep their defaults
- [ ] tests for bad input: an invalid bool or enum, an invalid `Language`, a line with no `=`, an unclosed `[General`, a missing section, unknown keys, and empty content
- [ ] validation commands pass

### Task 3: Store configuration as JSON with migration and recovery

#### Files

- Create: `src/Sic/Utils/ConfigSerializer.cs`
- Create: `tests/Sic.Tests/ConfigSerializerTests.cs`
- Modify: `src/Sic/Utils/Constants/App.cs`

#### Steps

- [ ] In `App.cs`, add `ConfigFileName = "sic.json"` and `LegacyConfigFileName = "Sic.cfg"`. Keep `ConfigFileExtension` until Task 4.
- [ ] Add `ConfigSerializer` with a `static readonly JsonSerializerOptions` as under Technical details, `Load(folder)` returning a `ConfigLoadResult`, and `Save(path, config)` as under Technical details.
- [ ] Add `BackUpFile(path)`, which keeps the newest copy as `sic.json.bad` and first renames an existing `.bad` to `sic.json.bad.<yyyyMMdd-HHmmss>`, so no backup is overwritten. The presets plan reuses it.
- [ ] Implement the load flow under Technical details. It takes a folder argument and shows no UI, so tests can use temporary folders. It builds complete translated messages for `LoadError` and `SaveError`, and the corrupt-file message names `sic.json.bad`.
- [ ] tests for loading: valid file, missing `general`, null `enabledFormats`, `language` and `lastInputFolder`, blank `outputFolder`, a hand-edited file with comments and trailing commas, and a file with a UTF-8 BOM
- [ ] tests for per-value recovery: `"updateCheckInterval": "Hourly"` and `"confirmExitWithQueue": 5` keep their defaults, every other setting survives, the file is backed up, and the message names the count. An invalid `language` becomes `System`.
- [ ] tests for round trips: every setting, with Cyrillic and Hebrew written as readable text rather than `\u` escapes
- [ ] tests for the default output folder: saved as `""`, loaded back as the current `App.DefaultOutputFolder`, including for migrated values; a custom folder stays as it is; `config.General.OutputFolder` still holds the full path after `Save`
- [ ] tests for portable migration: a legacy `OutputFolder` ending in `\userdata\Converted` from another location becomes the default in portable mode only, ignoring case and trailing separators
- [ ] tests for migration: `sic.json` is created, `Sic.cfg` becomes `Sic.cfg.bak`, and an existing `sic.json` always wins over `Sic.cfg`
- [ ] tests for failure paths: corrupt JSON moves to `sic.json.bad` without loss; with `sic.json.bad` held open, corrupt `sic.json` keeps its exact bytes and `SavesBlocked` is true; an existing `.bad` is rotated, not replaced; a locked `sic.json` sets `SavesBlocked` and changes nothing; a locked `Sic.cfg` keeps the file and sets `SavesBlocked`
- [ ] tests for atomic saves: a failed save leaves the previous file intact and no temporary file behind
- [ ] validation commands pass

### Task 4: Switch Config to JSON and remove SharpConfig

#### Files

- Create: `tests/Sic.Tests/ConfigTests.cs`
- Modify: `src/Sic/Utils/Config.cs`
- Modify: `src/Sic/Utils/Constants/App.cs`
- Modify: `src/Sic/SettingsDialog.cs`
- Modify: `src/Sic/Program.cs`
- Modify: `src/Sic/Sic.csproj`
- Modify: `tests/Sic.Tests/CliTests.cs`

#### Steps

- [ ] Rewrite `Config` over `ConfigSerializer`, keeping `Config.General`, `Load(isGui)` and `Save()` so existing call sites don't change.
- [ ] `Load` stores `isGui` in a field and passes `ConfigLoadResult`'s messages to `ReportError` unchanged. `Save` uses that field instead of the hardcoded `isGui: true`, and only logs when `SavesBlocked` is set. `ReportError` stays private.
- [ ] When saves are blocked, the load error says changes won't be saved this session.
- [ ] In `Program.EnsureOutputFolderExists`, with `showGui: false`, warn on stderr and use the default for this run without calling `Config.Save()`. The CLI is the one writer the guard doesn't cover, and today an unplugged drive permanently resets the custom folder.
- [ ] Remove `ConfigFileExtension`, update the `SettingsDialog.cs` cref to `GeneralSettings`, then delete `SectionGeneral` and the `sharpconfig-huanlin` package reference.
- [ ] tests in the non-parallel collection: with `Load(isGui: false)` and `sic.json` held open, `Save()` writes to stderr and returns; `SavesBlocked` skips the write
- [ ] tests through `Program.RunCli`, starting and ending with `TestDataFolder.Clear()`: a `Sic.cfg` with `Language=System` and an existing temporary `OutputFolder` placed in `App.DataFolder` migrates on a CLI run, output lands in that folder, and `sic.json` and `Sic.cfg.bak` appear
- [ ] tests through `Program.RunCli`: a missing custom output folder warns, writes to the default folder, and leaves `sic.json` unchanged
- [ ] validation commands pass

### Task 5: Allow one GUI per data folder

#### Files

- Create: `src/Sic/Utils/SingleInstanceGuard.cs`
- Create: `tests/Sic.Tests/SingleInstanceGuardTests.cs`
- Modify: `src/Sic/Program.cs`
- Modify: `src/Sic/MainWindow.cs`

#### Steps

- [ ] Add `SingleInstanceGuard` as under Technical details: `TryAcquire(dataFolder)`, `Dispose`, `ActivateExisting(dataFolder)`, `GetActivationMessage(dataFolder)` and `GetInstanceKey(dataFolder)`.
- [ ] In `Program.Main`, on the GUI path only, before `Config.Load`: keep the guard in `internal static SingleInstanceGuard? InstanceGuard` for the app's lifetime. If it can't be acquired, call `ActivateExisting` and return `ExitCode.Success` without showing anything.
- [ ] In `MainWindow`, keep `GetActivationMessage(App.DataFolder)` in a `static readonly` field, because an instance field that isn't set yet equals `WM_NULL`, and override `WndProc`. On that message, call `ShowWindow(Handle, SW_RESTORE)` if `IsIconic(Handle)`, so a maximized window stays maximized, then bring forward `GetLastActivePopup(Handle)`, so an open modal dialog gets focus rather than the disabled main window. These window P/Invokes stay private in `MainWindow`, beside the one at line 43.
- [ ] tests for `TryAcquire`: a second acquire for the same folder returns null, a different folder succeeds, and acquiring works again after `Dispose`
- [ ] tests for `Dispose`: it's idempotent, and disposing from another thread lets the next `TryAcquire` succeed
- [ ] tests for `GetActivationMessage`: the same non-zero value for the same folder, and different values for different folders
- [ ] tests for `GetInstanceKey`: stable, and the same for paths differing only by case or a trailing backslash
- [ ] validation commands pass

### Task 6: Translate the new strings

#### Files

- Modify: `src/Sic/locale/messages.pot`
- Modify: `src/Sic/locale/{de,es,fr,he,ru,uk}/Sic.po`

#### Steps

- [ ] Run `Extract-Strings.ps1`, then `Update-Translations.ps1`.
- [ ] Translate every new string in all six languages, and remove the `#, fuzzy` flag from each new entry.
- [ ] Run `Compile-Translations.ps1`.
- [ ] validation commands pass, including `TranslationCatalogTests`

### Task 7: Update the manuals and documentation

#### Files

- Modify: `src/Sic/help/{en,de,es,fr,he,ru,uk}/manual.html`
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `changelogs/1.2.0.0.md`
- Modify: `CLAUDE.md`

#### Steps

- [ ] All seven manuals: replace `Sic.cfg` with `sic.json` at the existing mention, say settings migrate automatically, and say that starting SIC! again brings the open window forward.
- [ ] `README.md`: replace the `Sic.cfg` mention and "SharpConfig-based settings".
- [ ] `CHANGELOG.md` and `changelogs/1.2.0.0.md`: add the automatic move to `sic.json`, the single-instance behavior, and the behavior changes under Technical details.
- [ ] Remove `sharpconfig-huanlin` from any remaining mention outside `docs/plans`.
- [ ] `CLAUDE.md`: update the config references, the settings section and the dependency table. Document `SingleInstanceGuard`, and note that #35 will need to forward file paths to the running instance, because arguments mean CLI mode today.
- [ ] validation commands pass

## Technical details

### sic.json

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
    "enabledFormats": "",
    "updateCheckInterval": "Daily"
  }
}
```

`enabledFormats` stays a comma-separated string so `GetEnabledFormatKeys` and its callers don't change.

`outputFolder` is saved as `""` when it equals `App.DefaultOutputFolder`, ignoring case and trailing separators, and a blank value loads as the default. At runtime `Config.General.OutputFolder` still holds the full path. This keeps a portable copy working after it moves, and stops a moved copy from writing back to its old location.

Saving must not change the object it's given, or Settings would show an empty box. `Save` serializes to a `JsonNode` with `JsonSerializer.SerializeToNode`, blanks `general.outputFolder` on the node, then writes that. Working on the node also means any member added to `AppConfig` later is saved without further changes.

Migration in portable mode only: a legacy `OutputFolder` ending in `\userdata\Converted`, ignoring case and trailing separators, counts as the default. `Sic.cfg` stored the full path, so a 1.1 `userdata` folder copied into a new 1.2 folder would otherwise keep pointing at the old copy.

### Models

```csharp
public sealed class AppConfig {
    public GeneralSettings General { get; set; } = new();
}

public sealed record ConfigLoadResult(
    AppConfig Config, string? LoadError, string? SaveError, bool SavesBlocked);
```

### JsonSerializerOptions

`WriteIndented = true`, `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`, `PropertyNameCaseInsensitive = true`, `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`, `ReadCommentHandling = JsonCommentHandling.Skip`, `AllowTrailingCommas = true`, `Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping` (so Cyrillic and Hebrew stay readable in a hand-edited file), and a `JsonStringEnumConverter`.

### Load flow

Every path that produces values finishes with the same clean-up: a null `general` becomes defaults, null strings become their defaults, a blank `outputFolder` becomes `App.DefaultOutputFolder`, and a `language` that isn't `System` or a valid culture name becomes `System`.

1. If `sic.json` exists, read it with sharing allowed (`FileShare.ReadWrite | FileShare.Delete`) and parse it with `JsonDocument`. Read `general` one property at a time, deserializing each value to its property's type. A value of the wrong type or an unknown enum name keeps its default and counts as bad. If anything was bad, call `BackUpFile` before going on, and report how many values were reset. If that backup fails, set `SavesBlocked`.
2. If the file isn't valid JSON, call `BackUpFile`, use defaults, write a new `sic.json` and report the error, naming `sic.json.bad`. If the backup fails, set `SavesBlocked`, write nothing, and leave `sic.json` in place.
3. If `sic.json` can't be read (I/O or access error), use defaults, set `SavesBlocked`, and report the error. The file is left untouched.
4. If `sic.json` doesn't exist and `Sic.cfg` does, read it with `LegacyConfigReader`. If reading fails, use defaults, set `SavesBlocked`, leave `Sic.cfg` alone and report the error, so the next run tries again. Otherwise write `sic.json`, and only after that succeeds move `Sic.cfg` to `Sic.cfg.bak`, overwriting an older one. A failed rename is logged and is harmless, because `sic.json` wins from then on. A failed write keeps `Sic.cfg` as it is, reports the error, and the migrated values are used for this run.
5. If neither exists, create the data folder, write defaults, and report any failure.

`SavesBlocked` exists so that a read failure can't lead to the next `Save()` replacing the user's file with defaults. SharpConfig's version has exactly this flaw today.

`Load`'s own writes are skipped when `SavesBlocked` is set, as well as `Save()`.

Saving writes `sic.json.<guid>.tmp` in the same folder, calls `Flush(flushToDisk: true)`, then `File.Move(tmp, path, overwrite: true)`, retrying the move briefly because a parallel CLI or an antivirus scanner may have `sic.json` open. It deletes the temporary file if anything fails. The name is unique because the CLI can run in parallel. The writer creates the data folder if it's missing.

`ConfigSerializer` builds complete translated messages, so the user hears what happened and where the backup is. `Config` routes them unchanged, and never wraps them in "Unable to load configuration: {0}".

### Legacy Sic.cfg format

Output for the full settings object:

```
[General]
Language=System
OutputFolder="C:\Data\My Pictures\Out"
SaveToSourceFolder=False
LastInputFolder=D:\In
ConfirmExitWithQueue=True
CheckForUpdatesOnStartup=True
DetectClipboardData=True
EnabledFormats=JPG,PNG,WEBP
UpdateCheckInterval=EveryThreeDays
```

Path vectors for the Task 2 fixtures: `C:\Pics #1\out`, `C:\a;b\out`, `C:\a=b\out`, `C:\a,b\out`, `C:\{x}\out`, `C:\a b\out`, a trailing backslash `C:\a\`, a space plus trailing backslash `C:\a b\` (which writes a closing `\"`), `C:\back\#slash`, a Cyrillic path and a Hebrew path.

Parser rules:

- Files are UTF-8, normally with no BOM, with CRLF line endings. Accept a BOM and LF too.
- Older files may contain as few as 4 keys. Missing keys keep their defaults.
- Trim each line and skip blanks.
- A comment starts at the first `#` or `;` that is outside double quotes and not preceded by a backslash.
- A line starting with `[` is a section header. Read only keys inside `[General]`, matching key names case-insensitively, and ignore unknown keys.
- Split at the first `=`, trim the value, and strip surrounding double quotes.
- Nothing is escaped inside a value, and backslashes are literal.
- Bools: `True`, `False`, `true`, `yes`, `1` and their negatives. Enums: exact member names. `Language`: `System` or a valid culture name.
- A malformed line is skipped with a logged warning, where SharpConfig discarded the whole file.

### SingleInstanceGuard

- `GetInstanceKey(dataFolder)`: the first 16 hex characters of the SHA-256 of the full path, upper-cased, with any trailing backslash removed. Keying on the data folder is what lets separate copies run side by side, since each has its own config.
- `TryAcquire(dataFolder)`: `new Mutex(initiallyOwned: true, $@"Local\Oire.Sic.{key}", out var createdNew)`. If `createdNew` is false, dispose it and return null. No `WaitOne`, so ownership can't be abandoned in a way that matters: a crashed process's handle closes and the mutex disappears with it.
- `TryAcquire` catches `UnauthorizedAccessException`, logs it and returns null. A normal launch beside an elevated copy would otherwise land in `Program.Main`'s catch and show a false "Unable to start" error. Activation is best effort in that case, because Windows blocks the broadcast to an elevated window.
- `Dispose` only closes the handle. It never calls `ReleaseMutex`, which throws on any thread but the owner, and the portable updater disposes the guard from another thread. It's idempotent. Closing the last handle destroys the mutex, so the next process gets `createdNew == true`.
- `GetActivationMessage(dataFolder)`: returns `RegisterWindowMessage($"Oire.Sic.Activate.{key}")`. The P/Invoke lives here and needs `CharSet = CharSet.Unicode`, unlike the existing example at `MainWindow.cs:43`.
- `ActivateExisting(dataFolder)`: call `AllowSetForegroundWindow(ASFW_ANY)`, because the new process is the one Windows lets take the foreground, then post `GetActivationMessage(dataFolder)` to `HWND_BROADCAST`.
- `MainWindow` handles that message by restoring if iconic, then calling `SetForegroundWindow(GetLastActivePopup(Handle))`. Both are best effort, so Windows may only flash the taskbar button.

The mutex is a kernel object that vanishes when SIC! exits, so it leaves nothing behind in portable mode. The CLI never takes it.

### Intended behavior changes

- Starting SIC! twice no longer opens a second window.
- A bad value in `Sic.cfg` no longer discards every other setting during migration.
- An invalid language value no longer stops SIC! at startup.
- A CLI save failure now goes to stderr instead of a message box.
- A CLI run no longer saves a reset of a missing output folder, so an unplugged drive doesn't lose the custom folder.

### Out of scope

- Forwarding file paths from a second launch to the running window. #35 needs that and will add it.
- Catching one portable folder used from two Windows sessions. `Local\` is per session, and that case is rare enough to accept.

## Post-completion

- Run the GUI against a real installed `Sic.cfg` on Windows 10 and 11, and confirm settings carry over.
- Run the portable ZIP with an existing `userdata` folder containing `Sic.cfg`.
- Move a portable folder to another drive letter, and confirm no "output folder no longer exists" message appears.
- With NVDA and JAWS: start SIC! a second time, with the window normal, minimized, maximized then minimized, and with Settings open, and confirm focus lands where it should.
- Run two portable copies side by side, and a portable beside an installed copy. Run the CLI while the GUI is open.
- Start SIC! normally while an elevated copy runs, and confirm there's no error dialog.
- A downgrade to 1.1 won't read `sic.json`. To recover the old settings, rename `Sic.cfg.bak` back to `Sic.cfg`.
