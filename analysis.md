# SIC! v1.0.0 Release Readiness Analysis

**Date**: 2026-03-03
**Status**: Pre-release audit

The codebase is competently structured with solid conventions. The critical issues cluster around URL/network handling and Config class coupling to WinForms in a dual-mode app. The core conversion engine and UI architecture are solid.

---

## CRITICAL — Must fix before release

### C1. `Config.Load()` calls `MessageBox.Show` in CLI mode

**Where**: `src/Sic/Utils/Config.cs`, lines 44-57
**Trigger**: Corrupted config file or read-only data folder while running in CLI mode

`Config.Load()` calls `MessageBox.Show` on any configuration error. In CLI mode (headless, no message pump), `MessageBox.Show` blocks the process indefinitely — it appears as a silent hang with no console output. The CLI calls `Config.Load()` at `src/Sic/Program.cs` line 101.

**Fix**: Decouple `Config` from `System.Windows.Forms`. Options:
- Add a `bool isGui` parameter to `Load()` and use `Console.Error.WriteLine` + return in CLI mode
- Throw a typed exception that `Program.cs` catches and routes appropriately
- Use a callback/delegate for error reporting injected by the caller

### C2. `LoadFromUrl` has no download size limit

**Where**: `src/Sic/Services/ImageConverter.cs`, line 97

`HttpClient.GetByteArrayAsync(uri)` has no limit on response size. A URL pointing to a multi-gigabyte file (or a server streaming indefinitely) will cause `OutOfMemoryException` and crash the app. This is a denial-of-service vector.

**Fix**: Use `HttpClient.GetAsync` with `HttpCompletionOption.ResponseHeadersRead`, check `Content-Length` against a reasonable maximum (e.g., 100 MB), then read into a bounded `MemoryStream`. Also add a `CancellationToken` parameter so the download can be cancelled.

### C3. `LoadFromUrl` accepts arbitrary URI schemes including `file://`

**Where**: `src/Sic/Services/ImageConverter.cs`, lines 95-96

The method constructs a `Uri` from user input but performs no scheme validation. A user could enter `file:///C:/Windows/System32/config/SAM` or `ftp://internal-server/secrets`. This is a Server-Side Request Forgery (SSRF) risk. Even in a desktop app, it is unexpected behavior.

**Fix**: After constructing the `Uri`, validate `uri.Scheme` is `"http"` or `"https"`. Reject other schemes with a descriptive error message.

### C4. No URL validation in `AddUrlDialog`

**Where**: `src/Sic/AddUrlDialog.cs`, lines 15-22

`OnFormClosing` only checks `string.IsNullOrWhiteSpace`. Malformed input like "hello world" passes through and throws `UriFormatException` in `LoadFromUrl`, producing a confusing error message: "Invalid URI: The format of the URI could not be determined."

**Fix**: In `OnFormClosing`, validate with:
```csharp
Uri.TryCreate(urlTextBox.Text, UriKind.Absolute, out var uri)
    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
```
Show a meaningful error like "Please enter a valid URL starting with http:// or https://".

---

## SERIOUS — Should fix before release

### S1. `_previewDebounceTimer` is never disposed

**Where**: `src/Sic/MainWindow.cs` line 17 (field), `src/Sic/MainWindow.Designer.cs` lines 13-18 (Dispose override)

`System.Windows.Forms.Timer` implements `IDisposable` and wraps a Win32 timer handle. The `Dispose(bool)` override does not dispose it. In a single-window app it is cleaned up on process exit, but it is a correctness issue.

**Fix**: Add `_previewDebounceTimer?.Dispose();` inside the `if (disposing)` block in `Dispose(bool)`.

### S2. CLAUDE.md says "Nerdbank.GitVersioning" but project uses GitVersion.MsBuild

**Where**: `CLAUDE.md` (paragraph 1) vs. `src/Sic/Sic.csproj` (line 41) and `GitVersion.yml`

These are completely different tools. Any contributor reading CLAUDE.md will be confused about the versioning system.

**Fix**: Replace "Nerdbank.GitVersioning" with "GitVersion (GitVersion.MsBuild)" everywhere in CLAUDE.md.

### S3. `SatelliteResourceLanguages` lists fr, de, uk — but only ru-RU locale exists

**Where**: `src/Sic/Sic.csproj`, line 13

The csproj declares `<SatelliteResourceLanguages>fr;de;uk;ru</SatelliteResourceLanguages>`, implying French, German, and Ukrainian translations exist. Only `locale/ru-RU/` has `.po`/`.mo` files. If anyone selects French or German, they get untranslated strings with no warning.

**Fix**: Either add the missing locale files, update `SatelliteResourceLanguages` to match reality (`ru` only), or remove the property and let the localization system handle missing translations gracefully.

### S4. Three `catch (Exception)` blocks in Config swallow exception details

**Where**: `src/Sic/Utils/Config.cs`, lines 44, 51, 66

All three blocks discard the exception entirely — no logging, no exception message shown. The user sees "Unable to save/load configuration. Please contact the developer." with zero diagnostic information. When a user contacts the developer, there is nothing to go on.

**Fix**: Add `Log.Error("Failed to load/save config: {Error}", ex.Message);` in each catch block. Include the exception message in the `MessageBox.Show` text so the user can relay it.

### S5. `catch { return true; }` in `FileHelper.IsCloudPlaceholder`

**Where**: `src/Sic/Utils/FileHelper.cs`, lines 12-13

The bare `catch { }` swallows all exceptions and returns `true` (claiming the file is a cloud placeholder). Any error — permission denied, path too long, invalid characters — silently classifies the file as a cloud placeholder and potentially skips it during batch add. The user gets no feedback about why.

**Fix**: At minimum, log the exception. Consider returning `false` for most exception types — only `FileNotFoundException` or specific cloud-related scenarios should return `true`.

### S6. `Application.DoEvents()` creates reentrancy risks

**Where**: `src/Sic/MainWindow.cs`, lines 148, 320, 470, 787

Four calls pump the Windows message queue synchronously, which can cause reentrancy — a user clicking "Convert" again while `DoEvents` is running, or pressing Ctrl+V while a batch add is in progress. The `_isLoadingFiles` guard covers one case, but `convertButton.Enabled = false` alone does not prevent menu shortcut reentrancy.

**Fix**: The heavy work already runs in `Task.Run`. The `DoEvents` calls appear to ensure dialogs are painted before work starts. Replace with `await Task.Yield()` or `await Task.Delay(1)` to allow UI painting without pumping the entire message queue.

### S7. README project structure is out of date

**Where**: `README.md`, lines 84-99

Missing at least these files:
- `AddUrlDialog.cs`
- `AddFolderDialog.cs`
- `AboutDialog.cs`
- `IcoPresetDialog.cs`
- `AddSizeDialog.cs`
- `ProgressDialog.cs`
- `Utils/FileHelper.cs`
- `Utils/Constants/ExitCode.cs`
- `Models/ResizeMode.cs`

**Fix**: Update the project structure section to include all source files. Keep descriptions brief.

### S8. No CHANGELOG.md

**Where**: Repository root (missing file)

For a v1.0.0 release, users expect to know what is new, what changed, and what was fixed. A release without a changelog looks unprofessional.

**Fix**: Create `CHANGELOG.md` following [Keep a Changelog](https://keepachangelog.com/) format. Document the v1.0.0 feature set.

### S9. `ExitCode` class defined but never used

**Where**: `src/Sic/Utils/Constants/ExitCode.cs` (entire file); `src/Sic/Program.cs` lines 33, 39, 48, 129, etc.

Defines `ExitCode.Success`, `ExitCode.Error`, and `ExitCode.Canceled`, but `Program.cs` uses hardcoded `return 0` and `return 1` everywhere.

**Fix**: Either replace all hardcoded `return 0`/`return 1` in `Program.cs` with the constants, or delete the file. Using the constants is preferred — it improves readability.

---

## MODERATE — Should fix for polish

### M1. Dead code: `DatabaseFileExtension` and `DatabaseName` in `App.cs`

**Where**: `src/Sic/Utils/Constants/App.cs`, lines 10 and 22

`DatabaseFileExtension = "oidb"` and `DatabaseName` are never referenced anywhere in the codebase. They imply a database feature that does not exist, confusing contributors.

**Fix**: Remove both fields.

### M2. Unused `using` directives

**Where**:
- `src/Sic/Utils/Config.cs` line 2: `using System.Diagnostics.Metrics;` — completely unused
- `src/Sic/Utils/Config.cs` lines 1, 3, 4: `using System;`, `using System.IO;`, `using System.Windows.Forms;` — unnecessary with `ImplicitUsings` + `UseWindowsForms`
- `src/Sic/Utils/Constants/App.cs` line 1: `using System;` — unnecessary
- `src/Sic/Utils/Constants/Logging.cs` line 1: `using System;` — unnecessary

**Fix**: Remove all unused/unnecessary `using` directives.

### M3. `Config` class uses `public static` mutable fields instead of properties

**Where**: `src/Sic/Utils/Config.cs`, lines 14-15

```csharp
public static Config.SectionGeneral General = new();
public static Configuration Cfg = new();
```

Fields violate .NET API design guidelines (CA1051). Exposing `Cfg` gives any code the ability to modify the raw SharpConfig `Configuration` object, bypassing the `Save()` method.

**Fix**: Make `General` and `Cfg` properties with private setters. If `Cfg` does not need to be public, make it `private` or `internal`.

### M4. PascalCase private fields in `ProgressDialog.Designer.cs`

**Where**: `src/Sic/ProgressDialog.Designer.cs`, lines 108-111

Private fields named `MainLayout`, `MessageLabel`, `ProgressBar`, `CancelOperationButton`. Every other Designer file uses camelCase (`mainLayout`, `messageLabel`).

**Fix**: Rename to `mainLayout`, `messageLabel`, `progressBar`, `cancelOperationButton` for consistency. Update all references in the file.

### M5. `Config.Save()` overwrites entire config with a new `Configuration`

**Where**: `src/Sic/Utils/Config.cs`, lines 61-62

`Cfg = new Configuration();` creates a brand-new object, adds the `General` section, and writes. Any sections or keys not part of `SectionGeneral` are silently destroyed. If a future version adds config sections, or if a user manually edits the config file, their additions will be lost on next save.

**Fix**: Modify the existing `Cfg` object in place instead of creating a new one, or re-read and merge before writing.

### M6. `LocalesFolder` uses relative path `"./locale"`

**Where**: `src/Sic/Utils/Constants/App.cs`, line 21

Relative path resolves against the current working directory, not the executable directory. If launched from a different directory (e.g., `C:\Users\User> C:\Apps\Sic\Sic.exe`), the locale folder won't be found and all strings display untranslated — silently.

**Fix**: Use `Path.Combine(AppContext.BaseDirectory, "locale")` to resolve relative to the executable, consistent with how `DataFolder` works in portable mode.

### M7. `ResolveFileConflict` blocks thread pool threads via `Invoke`

**Where**: `src/Sic/MainWindow.cs`, line 342 (call site in `ConvertItemsAsync`) and lines 930-986 (method)

Called from within `Task.Run`, it uses `Invoke()` to marshal to the UI thread and blocks until the user dismisses the dialog. With a batch of 100 conflicting files, this holds a thread pool thread hostage for each conflict resolution.

**Fix**: Pre-compute conflict detection on the UI thread before entering `Task.Run`, or use a producer-consumer pattern where the background thread yields conflicts back to the UI without blocking.

### M8. `GitVersion.yml` references branch `main` but repository uses `master`

**Where**: `GitVersion.yml`, line 7

Configures `branches: main: increment: None` but the repository's default branch is `master`. GitVersion will not match the `main` branch configuration, potentially causing unexpected version increments.

**Fix**: Change `main:` to `master:` in `GitVersion.yml`, or rename the repository branch.

---

## NITPICK — Would be nice to fix

### N1. `String.Format` used instead of string interpolation

**Where**: `src/Sic/Utils/Constants/App.cs` line 22; `src/Sic/Utils/Constants/Logging.cs` lines 8-12

`String.Format("{0}.{1}", App.Name, App.DatabaseFileExtension)` is less readable than `$"{Name}.{DatabaseFileExtension}"` in modern C#.

**Fix**: Replace with string interpolation.

### N2. `Logging` constants self-reference with `Logging.` prefix

**Where**: `src/Sic/Utils/Constants/Logging.cs`, lines 8-12

Members like `Logging.LogFolder` and `Logging.LogFileExtension` don't need the class name qualifier when referenced within the same class.

**Fix**: Remove the `Logging.` prefix.

### N3. `App.cs` self-references with `App.DataFolder` and `App.Name`

**Where**: `src/Sic/Utils/Constants/App.cs`, line 22

Same as N2 — `App.` is unnecessary inside the `App` class.

**Fix**: Remove the `App.` self-references.

### N4. `ExitCode.Success` relies on implicit default value

**Where**: `src/Sic/Utils/Constants/ExitCode.cs`, line 4

`public static readonly int Success;` is initialized to 0 implicitly. The other values (`Error = 1`, `Canceled = 2`) are explicitly assigned. This looks like an oversight.

**Fix**: Explicitly assign `= 0`: `public static readonly int Success = 0;`

### N5. `_lock` field uses `object` instead of `System.Threading.Lock`

**Where**: `src/Sic/Utils/Localization.cs`, line 47

`private static readonly object _lock = new object();` — in .NET 8+, `System.Threading.Lock` is the recommended type for dedicated lock objects.

**Fix**: Use `private static readonly Lock _lock = new();`

### N6. `using System.Drawing.Imaging` imported for single reference

**Where**: `src/Sic/MainWindow.cs`, line 1

The entire `System.Drawing.Imaging` namespace is imported only for `ImageFormat.Png` on line 712. Not harmful but could be a fully qualified reference instead.

**Fix**: Minor — either leave it or use `System.Drawing.Imaging.ImageFormat.Png` inline and remove the using.

### N7. `static readonly` fields instead of `const` for string literals

**Where**: `src/Sic/Utils/Constants/App.cs`, lines 6-10, 20-21; `src/Sic/Utils/Constants/Logging.cs`, line 7

Values like `ConfigFileExtension = "cfg"`, `Name = "Sic"`, `SystemLanguageName = "System"` are `static readonly` but could be `const`. `const` is inlined by the compiler and is preferred for pure string/integer literals that will never change across assemblies.

**Fix**: Change simple string/int literals to `const`. Keep `static readonly` only for values involving runtime computation (paths, `IsPortable`, etc.).

### N8. Settings dialog column percentages may clip localized text

**Where**: `src/Sic/SettingsDialog.Designer.cs`, lines 33-36

Columns are 20% + 50% + 15% + 15% = 100%. The "Reset" button column at 15% may be too narrow for some localized text (e.g., French "Reinitialiser").

**Fix**: Consider `SizeType.AutoSize` for button columns so they adapt to localized text length.

### N9. CLAUDE.md says "Allman brace style" but code and `.editorconfig` use K&R

**Where**: `CLAUDE.md` (last paragraph of "Code Conventions") vs. `.editorconfig` line 28 (`csharp_new_line_before_open_brace = none`)

Allman places opening braces on new lines. The actual code and editorconfig use K&R/Egyptian style (opening brace on same line).

**Fix**: Change CLAUDE.md to say "K&R brace style" instead of "Allman brace style".

---

## SUGGESTIONS — Not issues, but improvements for a polished release

### SG1. Add a `global.json` to pin the SDK version

Without it, a developer with .NET 9 SDK installed will build with .NET 9 SDK, which may produce different analyzer behavior. Adding `global.json` with `"version": "8.0.xxx"` and `"rollForward": "latestFeature"` ensures consistent builds.

### SG2. Add a test project

The core conversion engine (`ImageConverter.Convert`, `GenerateOutputPath`, `GetConflictRenamePath`, `CalculateResizeGeometry`) and CLI parsing logic are eminently testable without a GUI. Even 20-30 xUnit tests covering happy paths and edge cases (zero dimensions, negative dimensions, unsupported formats, conflict rename with many existing files) would dramatically increase confidence.

### SG3. Seal classes not designed for inheritance

`ImageItem`, `Config`, `AddUrlDialog`, `AddFolderDialog`, `AddSizeDialog`, `SettingsDialog`, `IcoPresetDialog`, `AboutDialog`, and `ProgressDialog` are all `public class` but none are designed for inheritance. Sealing communicates design intent and allows JIT devirtualization.

### SG4. Add `CancellationToken` support to `ImageConverter` methods

`LoadFromFile`, `LoadFromStream`, `LoadFromBytes`, `Convert`, and `GeneratePreview` are synchronous and can block on I/O for large files. Adding `CancellationToken` parameters would allow the UI to cancel long-running operations more precisely than the current `WaitAsync` approach.

### SG5. Centralized error boundary for `async void` event handlers

All eight `async void` event handlers in `MainWindow.cs` catch exceptions individually. A missed `try-catch` in any future handler will crash the app via `SynchronizationContext`. Consider a helper:

```csharp
private async void SafeAsync(Func<Task> action) {
    try { await action(); }
    catch (Exception ex) { Log.Error("Unhandled: {Error}", ex); /* show error dialog */ }
}
```

### SG6. Add log file rotation/retention limits

`Program.cs` sets `rollOnFileSizeLimit: true` but does not set `retainedFileCountLimit`. Serilog's default is 31 rolled files. For an app running for months, logs could grow significantly. Consider `retainedFileCountLimit: 5` and `fileSizeLimitBytes: 10_000_000`.

### SG7. Add `dotnet test` step to CI pipeline

The GitHub Actions workflow runs `dotnet restore`, `dotnet format --verify-no-changes`, and `dotnet build --no-restore`. Once tests exist (see SG2), add `dotnet test --no-build` to the pipeline.

### SG8. Add badges to README

For a public release, adding shields.io badges for build status, latest release version, and license type at the top of README gives an immediate quality signal.

### SG9. Configure `HttpClient` timeout

The static `HttpClient` in `ImageConverter.cs` uses the default `Timeout` of 100 seconds. For downloading images, this may be too long (or too short for very large files on slow connections). Consider making it configurable or at least documenting the default.

### SG10. Clean up premature `dependabot.yml` test dependency groups

`dependabot.yml` defines a `test-dependencies` group with patterns for xunit, FluentAssertions, Moq, and coverlet. None of these packages are referenced yet. Harmless but premature — remove until a test project exists.

### SG11. README license wording contradicts Apache 2.0

**Where**: `README.md`, line 103

"Copyright 2026 Oire Software SARL. All rights reserved." combined with Apache 2.0 license is technically contradictory. The license governs, so this isn't legally binding, but it looks odd.

**Fix**: Change to "Licensed under the Apache License 2.0. See LICENSE for details." or remove "All rights reserved."

---

## Summary

| Severity | Count | Key Themes |
|----------|-------|------------|
| CRITICAL | 4 | SSRF in URL loading, Config hangs CLI, no download size limit, no URL validation |
| SERIOUS | 9 | Resource leaks, doc drift, dead code, swallowed exceptions, outdated README |
| MODERATE | 8 | Dead code, naming inconsistency, relative locale path, config overwrite |
| NITPICK | 9 | Style, `const` vs `static readonly`, self-references, brace style doc mismatch |
| SUGGESTIONS | 11 | Tests, `global.json`, sealed classes, CancellationToken, log rotation |
| **TOTAL** | **41** | |

Fixing the 4 critical and 9 serious issues (13 total) would bring this to a confident v1.0.0 release. The moderate and nitpick items are polish that would distinguish a professional release from a "good enough" one.
