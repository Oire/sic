# Test coverage for existing code

## Overview

SIC! has no automated tests. This plan adds a test project and covers the code that exists today: conversion, resizing, ICO output, fit to file size, output paths, utilities, translation catalogs and the CLI.

It changes no behavior. PR #91 already fixed five duplicate menu mnemonics in Russian and Ukrainian; Task 7 adds the test that stops them coming back.

It's the first of four plans, and it comes first so that the JSON config, presets and portable work all refactor tested code. The order is `88-test-coverage`, `89-json-config`, `23-custom-presets`, `90-portable-no-traces`, each as its own pull request.

## Done when

- [ ] `dotnet test` passes locally and in CI.
- [ ] Every public method of `ImageConverter` has tests for success, and for failure wherever a failure path exists.
- [ ] Every target format is tested from every source format that SIC! can write.
- [ ] The CLI's options, exit codes and error output are tested end to end through `Program.RunCli`.
- [ ] Every `Sic.po` catalog is checked for empty entries, fuzzy entries, mismatched placeholders and duplicate menu mnemonics.
- [x] Tests never touch the real `%APPDATA%\Oire\Sic` folder, and a test proves it.
- [x] `build-installer.ps1` still produces the single-file `Sic.exe`, and no test files reach the published output.
- [ ] all validation commands pass

## Validation commands

Run from the repository root.

- build: `dotnet build`
- test: `dotnet test`
- format check: `dotnet format --verify-no-changes`

There are no e2e tests.

## Context

### Files involved

- Conversion engine, tested but not changed: `src/Sic/Services/ImageConverter.cs`
- Utilities: `src/Sic/Utils/FileHelper.cs`, `UrlHelper.cs`, `Localization.cs`; `src/Sic/Models/ImageItem.cs`
- CLI: `src/Sic/Program.cs` (`RunCli`)
- Updater: `src/Sic/Services/UpdateService.cs` (`ToFrequency`)
- Catalogs: `src/Sic/locale/{de,es,fr,he,ru,uk}/Sic.po`
- Project and CI: `src/Sic/Sic.csproj`, `Sic.sln`, `.github/workflows/dotnet.yml`, `.github/dependabot.yml`, `installer/build-installer.ps1`
- Translation tools: `src/Sic/locale/scripts/Compile-Translations.ps1`, `Update-Translations.ps1`, `src/Sic/locale/README.md`

### Patterns to follow

- Test project: copy `C:\repos\Oire\plan-cake\tests\PlanCake.Tests\PlanCake.Tests.csproj`. Keep its `net10.0-windows`, `UseWindowsForms`, `Platforms`, `Platform`, analyzer, `IsPackable` and `IsTestProject` properties, its package versions (`xunit` 2.9.3, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `AwesomeAssertions`, `Microsoft.CodeAnalysis.NetAnalyzers`) and its `PrivateAssets` pattern. Leave out `Jint`, `Corpus`, `coverlet.collector`, and `PreserveCompilationContext` with its comment, which only exists for Coverlet.
- Assertions: `using AwesomeAssertions;`, as in `plan-cake/tests`.
- Non-parallel collection: `C:\repos\Oire\plan-cake\tests\PlanCake.Tests\LocalizationCollection.cs`, with `[CollectionDefinition(Name, DisableParallelization = true)]` on a `public sealed class` holding a `Name` constant.

### Conventions and traps

- `.editorconfig` requires CRLF, so run `dotnet format` on new `.cs` files before the format check.
- `.editorconfig` raises whole analyzer categories, and it applies to `tests/` too. Warnings are errors. Security rules are errors, so CA5394 forbids `System.Random`: generate noise with Magick. Performance rules such as CA1861 apply as well.
- Same-line braces. `_` is the gettext method, so `_ = ...` is not a discard.
- The class name `ImageConverter` conflicts with `System.Drawing.ImageConverter`, so test files need `using ImageConverter = Oire.Sic.Services.ImageConverter;`.
- No test may assert a translated result. `.mo` files aren't in git, and SIC! names its catalog from `Environment.ProcessPath`, so under `dotnet test` the domain is the test host and translations never load, even locally. That also makes English text from `_()` safe to assert everywhere.
- `GetCurrentCulture` only checks whether `locale/<culture>` exists, and `.mo` files reach the test output locally but not in CI. Task 1 creates `locale\de` in the test output, so both environments behave the same.
- Do not assert the text of `System.CommandLine`'s own errors, such as a missing `--input`. Its resources follow `CurrentUICulture`, so the text differs on a non-English Windows.
- Magick noise isn't seeded by default, so size budgets come from a measured encode, never from fixed constants.
- `Config`, `Console` and `Localization` are static. Tests that touch them go in one non-parallel xUnit collection.
- Never use `/dev/null`, `nul` or any other null device in a shell command.

### Dependencies

Never add FluentAssertions. Do not add Moq; the code under test is static, so tests use real files in temporary folders.

## Development approach

- One task at a time; the validation commands pass before the next task starts.
- These tests describe the code as it is. If one finds a real bug, do not change behavior silently: record it in this plan with a "⚠️" prefix and tell the user.
- A failing catalog entry is a real finding too. Report it rather than rewriting a translation to make a test pass.
- A test that exposes a real bug or a bad catalog entry stays in the code with `Skip = "⚠️ ..."` naming the problem, and the problem goes in this plan. The validation commands keep passing, and the test isn't weakened.
- Tests never touch the user's real data. The test output folder contains a `userdata` folder, so `App.IsPortable` is true and `App.DataFolder` stays inside `bin` (Task 1).
- When scope changes, update this plan: new tasks get a "➕" prefix, blockers a "⚠️" prefix.

## Implementation steps

### Task 1: Add the test project and run it in CI

#### Files

- Create: `tests/Sic.Tests/Sic.Tests.csproj`
- Create: `tests/Sic.Tests/TestIsolationTests.cs`
- Create: `tests/Sic.Tests/TestDataFolder.cs`
- Create: `tests/Sic.Tests/UrlHelperTests.cs`
- Modify: `Sic.sln`
- Modify: `src/Sic/Sic.csproj`
- Modify: `.github/workflows/dotnet.yml`
- Modify: `.github/dependabot.yml`
- Modify: `installer/build-installer.ps1`
- Modify: `src/Sic/locale/scripts/Compile-Translations.ps1`
- Modify: `src/Sic/locale/scripts/Update-Translations.ps1`
- Modify: `src/Sic/locale/README.md`

#### Steps

- [x] Create the project from the PlanCake pattern, with `AssemblyName` `Sic.Tests`, `RootNamespace` `Oire.Sic.Tests`, `IsPublishable` false, and a project reference to `src/Sic/Sic.csproj`.
- [x] Add `<InternalsVisibleTo Include="Sic.Tests" />` to `Sic.csproj`, and add the test project to `Sic.sln` with Debug|x64 and Release|x64 entries.
- [x] Add an after-build target to the test project that creates `$(OutDir)userdata` and `$(OutDir)locale\de`, and a guard test asserting that `App.IsPortable` is true and `App.DataFolder` is under the test output folder.
- [x] Add `TestDataFolder.Clear()`, deleting every `Sic.cfg*` and `sic.json*` file from `App.DataFolder`, which also covers later backups such as `sic.json.bad.<timestamp>`, for tests in this and later plans that place config files there.
- [x] In `build-installer.ps1`, rename `$SolutionPath` to `$ProjectPath` pointing at `src\Sic\Sic.csproj`, with the error text "Project file not found", and pass exactly `$ProjectPath, "--configuration", "Release", "/verbosity:minimal"`. Do not copy PlanCake's `--output` or `--runtime`: here `$PublishOutputPath` is defined after the publish, and PlanCake's folder is different.
- [x] Confirm with `.\installer\build-installer.ps1 -NoSign`, which deletes `bin\x64\Release` first, so an old `Sic.exe` can't pass the check. The new `Sic.exe` must be in `src\Sic\bin\x64\Release\win-x64\publish`, with no `xunit*` or `Sic.Tests*` files beside it.
- [x] Make `Compile-Translations.ps1` and `Update-Translations.ps1` find `msgfmt` and `msgmerge` on the `PATH` through `Get-Command`, using `.Source` as `build-installer.ps1` does for 7-Zip, then in `$env:LOCALAPPDATA\Programs\gettext-iconv\bin`, where mlocati's gettext build installs, then in the two GnuWin32 folders. Later plans run both scripts. Change the install hint in both scripts and in `src/Sic/locale/README.md` to `winget install mlocati.GetText`.
- [x] Make `Update-Translations.ps1` restore CRLF after each `msgmerge` with the code under Technical details, inside `if ($LASTEXITCODE -eq 0)` and before the backup is removed, so a failed write still reaches the catch block's restore. `msgmerge` writes LF, so every update would otherwise be a whole-file diff.
- [x] Confirm by running the script twice with no new strings, then discard the changes. `msgmerge` rewrites a catalog even when it's in sync, so this does exercise the code. `git diff --stat` must show only a few lines per catalog, where `msgmerge` moves the `Language:` header, and the second run must add nothing. Done: the second run added nothing and CRLF held. `de`, `fr`, `ru` and `uk` moved only the header lines; `es` and `he` also had their hand-split `#:` reference lines joined, a one-time reformat the next real merge will carry.
- [x] tests for `UrlHelper.IsValidHttpUrl`: valid http and https links, including surrounding spaces
- [x] tests for `UrlHelper.IsValidHttpUrl`: other schemes, relative links and empty input
- [x] Add a `Test` step running `dotnet test --no-build` after `Build` in `dotnet.yml`, and `timeout-minutes: 20` on the job, so a hung socket test can't hold the runner. In `dependabot.yml`, replace `FluentAssertions*` with `AwesomeAssertions*`, and remove `coverlet*` and `Moq*`.
- [x] validation commands pass

### Task 2: Test format lists and output paths

#### Files

- Create: `tests/Sic.Tests/ImageConverterFormatTests.cs`
- Create: `tests/Sic.Tests/OutputPathTests.cs`
- Create: `tests/Sic.Tests/TestImages.cs`

#### Steps

- [x] Add `TestImages`, which generates small images with Magick in a temporary folder and deletes them afterwards. Use gradients or Magick's noise rather than solid colors where encoded size matters, and commit no binary fixtures.
- [x] tests for `GetSupportedFormats` order, and `GetEnabledFormats`: filtering, canonical order, null or empty input, and never returning an empty list
- [x] tests for `GetSizeFitFormats`: ICO and GIF removed, and an empty result when only those are enabled
- [x] tests for `GetFileExtension`, `IsSameFormat` and `ShouldSkipConversion`, including JPEG and TIFF spellings and a resize forcing a re-encode
- [x] tests for `GenerateOutputPath`: default folder, `saveToSourceFolder`, clipboard and URL items falling back to the output folder, and a folder-relative `BasePath`
- [x] tests for `GetConflictRenamePath`: `_1`, then `_2` when `_1` exists
- [x] validation commands pass

### Task 3: Test loading, conversion, resizing, ICO output and previews

#### Files

- Create: `tests/Sic.Tests/ImageLoadingTests.cs`
- Create: `tests/Sic.Tests/ConversionTests.cs`
- Create: `tests/Sic.Tests/IcoTests.cs`
- Create: `tests/Sic.Tests/PreviewTests.cs`
- Create: `tests/Sic.Tests/StubHttpServer.cs`

#### Steps

- [x] tests for `LoadFromFile`, `LoadFromStream` and `LoadFromBytes`: dimensions, size and format; a corrupt file; a missing file
- [x] Add `StubHttpServer`, a small `TcpListener` server on the loopback address that Task 6 reuses.
- [x] tests for `LoadFromUrl` against the stub: an image, an HTML page giving `UnsupportedImageException`, a 404, cancellation, an `ftp://` link, and a faked `Content-Length` above 100 MB
- [x] tests for `Convert`: a theory over every source format to every target format, checking that the output decodes as the target format at the same size. `LoadFromFile` reports `Ico` from the file extension alone, so check ICO output by reading its header.
- [x] tests for `Convert` failures: an unsupported target gives `ArgumentException`, and an item with no data gives `InvalidOperationException`
- [x] tests for resizing: keep proportions with width only, height only and both (the fit-inside-a-box case, in portrait and landscape), and crop giving exact dimensions
- [x] tests for `CreateMultiSizeIco` and ICO output from `Convert`: the frame list, the 256 px frame stored as PNG (read the ICO directory and check the PNG signature), and the 512 px limit
- [x] tests for `GeneratePreview`: the bitmap fits the maximum size, and an item with no data fails. It always scales to the box, so keep-proportions resizing doesn't change its size; test resize settings with crop instead: 300x200 cropped to 100x100 in a 480x480 box gives 480x480, against 480x320 without crop.
- ⚠️ Bug found: `Convert` to ICO of an image with one side over 256 px and the other under it, such as 300x200, writes a bitmap frame whose directory width is 44 (300 mod 256) instead of 0. Magick switches to PNG only when both sides exceed 256 px or one is exactly 256, and `WriteIco` re-encodes only frames whose directory byte is already 0, so the frame stays an uncompressed bitmap of about 250 KB under a wrong width. Square icons and the multi-size presets are unaffected. `IcoTests.Convert_StoresAnIcoWithOneSideOver256PxAsPng` is skipped until this is fixed.
- Note: Magick's 512 px check applies to the first frame only, so `CreateMultiSizeIco` with `[16, 513]` writes a 513 px frame without complaint. The ICO presets never go above 256 px, so the UI can't reach it; `[513]` alone throws as expected.
- [x] validation commands pass

### Task 4: Test fit to file size

#### Files

- Create: `tests/Sic.Tests/SizeFitTests.cs`

#### Steps

- [ ] tests for `FindSizeFitProposals`: every proposal is under budget, the first is `Recommended`, and ranking is resolution, then quality, then size
- [ ] tests for limits: `maxWidth` is respected, an impossible budget gives an empty list, `maxBytes` of 0 throws `ArgumentOutOfRangeException`, and ICO and GIF are never proposed. Derive budgets from a measured encode.
- [ ] tests for `ConvertToProposal`: the written file matches the advertised `FileSize` exactly, and an unsupported format fails
- [ ] tests for progress and cancellation: each format is reported as its turn starts, and a canceled token stops the search. Add a small synchronous `IProgress<string>` that appends to a list, and can cancel a token on its first report. `Progress<T>` posts callbacks asynchronously, so asserting right after the search would race.
- [ ] Keep images small and include AVIF in one test only, because it is by far the slowest format.
- [ ] validation commands pass

### Task 5: Test utilities and models

#### Files

- Create: `tests/Sic.Tests/FileHelperTests.cs`
- Create: `tests/Sic.Tests/ImageItemTests.cs`
- Create: `tests/Sic.Tests/LocalizationTests.cs`
- Create: `tests/Sic.Tests/UpdateIntervalTests.cs`
- Create: `tests/Sic.Tests/StaticStateCollection.cs`
- Create: `tests/Sic.Tests/StaticStateTestBase.cs`
- Modify: `src/Sic/Services/UpdateService.cs`

#### Steps

- [ ] Add `StaticStateCollection` on the PlanCake pattern, for tests that touch `Config`, `Console` or `Localization`, and a `StaticStateTestBase` class that calls `Localization.SetLanguage("en-US")` in its constructor and `Dispose`, because `SetLanguage` writes `CultureInfo.DefaultThreadCurrentCulture` process-wide.
- [ ] tests for `FileHelper.EnumerateImageFiles`: top folder only, recursive, several extensions, and an inaccessible folder being skipped, using a Deny ACE for the current user that's removed in `finally`. `IsCloudPlaceholder` returns false for a normal file.
- [ ] tests for `ImageItem`, in the non-parallel collection because it calls `_()`: the byte, KB and MB boundaries in `GetSizeDisplay`, and the dimension and description text, under a fixed culture
- [ ] tests for `Localization`, in the non-parallel collection: English source text, placeholders and plurals; `SetLanguage("de")` keeps `de`, and `pl-PL` falls back to `en-US`. Assert no translated text. A neutral culture such as `pl` resolves oddly because `Path.Combine(LocalesFolder, "")` exists: record that with a "⚠️" as a possible bug.
- [ ] Make `UpdateService.ToFrequency` internal, then test every `UpdateCheckInterval`, including `Never` giving null.
- [ ] validation commands pass

### Task 6: Test the existing CLI

#### Files

- Create: `tests/Sic.Tests/CliTests.cs`
- Modify: `src/Sic/Program.cs`

#### Steps

- [ ] Make `Program.RunCli` internal, and call it directly with stderr captured through `Console.SetError`, in the non-parallel collection.
- [ ] tests for success: `-i` with `-o`, `-i` with `--format`, a URL input through `StubHttpServer`, `JPEG` and `TIF` spellings, every `--resize` form, and `--crop`
- [ ] tests for failure: missing input, no extension on `--output`, neither `--output` nor `--format`, an unsupported format, invalid `--resize` values, `--crop` with one size, a corrupt input file, and a URL giving 404. Each returns `ExitCode.Error` and writes to stderr. Assert the exact text "Either --output or --format must be specified.", because the presets plan changes that message.
- [ ] tests for skipping: same format, no resize, returns `ExitCode.Success`
- [ ] validation commands pass

### Task 7: Check the translation catalogs

#### Files

- Create: `tests/Sic.Tests/PoCatalog.cs`
- Create: `tests/Sic.Tests/TranslationCatalogTests.cs`

#### Steps

- [ ] Add `PoCatalog`, a small reader for `Sic.po` that finds the repository root by walking up to `Sic.sln`, and lists languages from `locale/*/Sic.po` on disk rather than hard-coding them. It handles multi-line strings and plural entries, skips the header and obsolete `#~` entries, and ties a `#, fuzzy` flag to the entry after it, because today's flags all sit on obsolete entries.
- [ ] tests over all languages: no fuzzy entries and no empty `msgstr`
- [ ] tests for placeholders on singular entries: each `msgstr` uses the same `{0}`-style placeholders as its `msgid`.
- [ ] tests for placeholders on plural entries: every `msgstr[i]` uses only placeholders found in `msgid_plural`, the last form matches `msgid_plural` exactly, and `msgstr[0]` may omit a placeholder only when `msgid` has none. In a language whose `Plural-Forms` has `nplurals` above 2, as Russian and Ukrainian do, `msgstr[0]` needs `{0}`, because that form also covers 21, 31 and so on.
- [ ] Add one `TheoryData` per menu level in `BuildMenuSpec` (`TopLevelMenuStrings` for the bar itself, then `FileMenuStrings`, `EditMenuStrings`, `ConvertMenuStrings`, `HelpMenuStrings`), and test that no level has duplicate mnemonics in any language. A duplicate throws `ArgumentException` at startup. Use `MenuTextFormatter.ExtractMnemonic` from `Oire.WinForms.NativeControls`. A listed `msgid` missing from a catalog fails the test, so a renamed string can't pass silently.
- [ ] validation commands pass

### Task 8: Update project documentation

#### Files

- Modify: `CLAUDE.md`
- Modify: `README.md`

#### Steps

- [ ] `CLAUDE.md`: replace "There are no test projects" with `dotnet test`, the real format command `dotnet format --verify-no-changes`, test isolation through `userdata`, the non-parallel collection, and the rule against asserting translated text.
- [ ] `README.md`: mention `dotnet test` wherever build commands are listed.
- [ ] `README.md` lines 55 and 133 and `CLAUDE.md` say `dotnet publish -c Release` at solution level. Point them at the csproj, now that the solution includes tests.
- [ ] validation commands pass

## Technical details

### Test isolation

`App.IsPortable` is `Directory.Exists(Path.Combine(AppContext.BaseDirectory, "userdata"))`, so a `userdata` folder in the test output makes every path under `App.DataFolder` land in `bin`. The guard test in Task 1 fails loudly if that ever stops being true.

### Restoring CRLF

These scripts run under Windows PowerShell 5.1, where `Get-Content -Raw` reads the UTF-8 catalogs as ANSI and `Set-Content -Encoding UTF8` adds a BOM, so use .NET directly. `msgmerge` can also leave a file as it was, so the replacement must not touch lines that already end in CRLF.

```powershell
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$merged = [System.IO.File]::ReadAllText($poFile, $utf8NoBom)
$crlf = [regex]::Replace($merged, '(?<!\r)\n', "`r`n")
if ($crlf -cne $merged) {
    [System.IO.File]::WriteAllText($poFile, $crlf, $utf8NoBom)
}
```

### Out of scope

- Automated tests that drive the forms. A UI Automation suite (FlaUI, for example) would be separate work.
- A HEIC decoding test. This Magick build can't write HEIC, so a test can't generate one; it needs a small committed sample.
- Coverage reports. Nothing would act on the figures, and collecting them would mean changing the Debug `DebugType`.

## Post-completion

- Run a real `Build-Installer.ps1` build and confirm the test project isn't in the published output.
