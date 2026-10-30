# Test coverage for existing code

## Overview

SIC! has no automated tests. This plan adds a test project and covers the code that exists today: conversion, resizing, ICO output, fit to file size, output paths, utilities, translation catalogs and the CLI.

It changes no behavior. It's the first of four plans, and it comes first so that the JSON config, presets and portable work all refactor tested code. The order is `001-test-coverage`, `002-json-config`, `23-custom-presets`, `003-portable-no-traces`, each as its own pull request.

## Done when

- [ ] `dotnet test` passes locally and in CI.
- [ ] Every public method of `ImageConverter` has tests, for both success and failure.
- [ ] Every target format is tested from every source format that SIC! can write.
- [ ] The CLI's options, exit codes and error output are tested end to end through `Program.RunCli`.
- [ ] Every `Sic.po` catalog is checked for empty entries, mismatched placeholders and duplicate menu mnemonics.
- [ ] Tests never touch the real `%APPDATA%\Oire\Sic` folder, and a test proves it.
- [ ] `build-installer.ps1` still produces the single-file `Sic.exe`, and no test files reach the published output.
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

### Patterns to follow

- Test project: copy `C:\repos\Oire\plan-cake\tests\PlanCake.Tests\PlanCake.Tests.csproj`. Keep its `net10.0-windows`, `UseWindowsForms`, `Platforms`, `Platform`, analyzer, `IsPackable`, `IsTestProject` and `PreserveCompilationContext` properties, its package versions (`xunit` 2.9.3, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `AwesomeAssertions`, `coverlet.collector`, `Microsoft.CodeAnalysis.NetAnalyzers`) and its `PrivateAssets` pattern. Leave out `Jint` and `Corpus`.
- Assertions: `using AwesomeAssertions;`, as in `plan-cake/tests`.

### Conventions and traps

- `.editorconfig` requires CRLF, so run `dotnet format` on new `.cs` files before the format check.
- `.editorconfig` raises whole analyzer categories, and it applies to `tests/` too. Warnings are errors. Security rules are errors, so CA5394 forbids `System.Random`: generate noise with Magick. Performance rules such as CA1861 apply as well.
- Same-line braces. `_` is the gettext method, so `_ = ...` is not a discard.
- The class name `ImageConverter` conflicts with `System.Drawing.ImageConverter`, so test files need `using ImageConverter = Oire.Sic.Services.ImageConverter;`.
- `.mo` files aren't in git. They reach the test output locally but not in CI, so no test may assert a translated result.
- `Config`, `Console` and `Localization` are static. Tests that touch them go in one non-parallel xUnit collection.
- Never use `/dev/null`, `nul` or any other null device in a shell command.

### Dependencies

Never add FluentAssertions. Do not add Moq; the code under test is static, so tests use real files in temporary folders.

## Development approach

- One task at a time; the validation commands pass before the next task starts.
- These tests describe the code as it is. If one finds a real bug, do not change behavior silently: record it in this plan with a "⚠️" prefix and tell the user.
- A failing catalog entry is a real finding too. Fix a wrong translation in place, and report any you can't judge.
- Tests never touch the user's real data. The test output folder contains a `userdata` folder, so `App.IsPortable` is true and `App.DataFolder` stays inside `bin` (Task 1).
- When scope changes, update this plan: new tasks get a "➕" prefix, blockers a "⚠️" prefix.

## Implementation steps

### Task 1: Add the test project and run it in CI

#### Files

- Create: `tests/Sic.Tests/Sic.Tests.csproj`
- Create: `tests/Sic.Tests/TestIsolationTests.cs`
- Create: `tests/Sic.Tests/UrlHelperTests.cs`
- Modify: `Sic.sln`
- Modify: `src/Sic/Sic.csproj`
- Modify: `.github/workflows/dotnet.yml`
- Modify: `.github/dependabot.yml`
- Modify: `installer/build-installer.ps1`

#### Steps

- [ ] Create the project from the PlanCake pattern, with `AssemblyName` `Sic.Tests`, `RootNamespace` `Oire.Sic.Tests`, `IsPublishable` false, and a project reference to `src/Sic/Sic.csproj`.
- [ ] Add `<InternalsVisibleTo Include="Sic.Tests" />` to `Sic.csproj`, and add the test project to `Sic.sln` with Debug|x64 and Release|x64 entries.
- [ ] Add an after-build target to the test project that creates `$(OutDir)userdata`, and a guard test asserting that `App.IsPortable` is true and `App.DataFolder` is under the test output folder.
- [ ] In `build-installer.ps1`, publish `src\Sic\Sic.csproj` instead of the solution, as PlanCake's script does, keeping the existence check. Confirm the output still lands in the `win-x64\publish` folder the script expects, with a Release single-file `Sic.exe`.
- [ ] tests for `UrlHelper.IsValidHttpUrl`: valid http and https links, including surrounding spaces
- [ ] tests for `UrlHelper.IsValidHttpUrl`: other schemes, relative links and empty input
- [ ] Add a `Test` step running `dotnet test --no-build` after `Build` in `dotnet.yml`, and replace `FluentAssertions*` with `AwesomeAssertions*` in `dependabot.yml`.
- [ ] validation commands pass

### Task 2: Test format lists and output paths

#### Files

- Create: `tests/Sic.Tests/ImageConverterFormatTests.cs`
- Create: `tests/Sic.Tests/OutputPathTests.cs`
- Create: `tests/Sic.Tests/TestImages.cs`

#### Steps

- [ ] Add `TestImages`, which generates small images with Magick in a temporary folder and deletes them afterwards. Use gradients or Magick's noise rather than solid colors where encoded size matters, and commit no binary fixtures.
- [ ] tests for `GetSupportedFormats` order, and `GetEnabledFormats`: filtering, canonical order, null or empty input, and never returning an empty list
- [ ] tests for `GetSizeFitFormats`: ICO and GIF removed, and an empty result when only those are enabled
- [ ] tests for `GetFileExtension`, `IsSameFormat` and `ShouldSkipConversion`, including JPEG and TIFF spellings and a resize forcing a re-encode
- [ ] tests for `GenerateOutputPath`: default folder, `saveToSourceFolder`, clipboard and URL items falling back to the output folder, and a folder-relative `BasePath`
- [ ] tests for `GetConflictRenamePath`: `_1`, then `_2` when `_1` exists
- [ ] validation commands pass

### Task 3: Test loading, conversion, resizing, ICO output and previews

#### Files

- Create: `tests/Sic.Tests/ImageLoadingTests.cs`
- Create: `tests/Sic.Tests/ConversionTests.cs`
- Create: `tests/Sic.Tests/IcoTests.cs`
- Create: `tests/Sic.Tests/PreviewTests.cs`

#### Steps

- [ ] tests for `LoadFromFile`, `LoadFromStream` and `LoadFromBytes`: dimensions, size and format; a corrupt file; a missing file
- [ ] tests for `LoadFromUrl` against a small `TcpListener` stub on the loopback address: an image, an HTML page giving `UnsupportedImageException`, a 404, and cancellation
- [ ] tests for `Convert`: a theory over every source format to every target format, checking that the output decodes as the target format at the same size
- [ ] tests for resizing: keep proportions with width only, height only and both (the fit-inside-a-box case, in portrait and landscape), and crop giving exact dimensions
- [ ] tests for `CreateMultiSizeIco` and ICO output from `Convert`: the frame list, the 256 px frame stored as PNG (read the ICO directory and check the PNG signature), and the 512 px limit
- [ ] tests for `GeneratePreview`: the bitmap fits the maximum size, and resize settings change the result
- [ ] validation commands pass

### Task 4: Test fit to file size

#### Files

- Create: `tests/Sic.Tests/SizeFitTests.cs`

#### Steps

- [ ] tests for `FindSizeFitProposals`: every proposal is under budget, the first is `Recommended`, and ranking is resolution, then quality, then size
- [ ] tests for limits: `maxWidth` is respected, an impossible budget gives an empty list, and ICO and GIF are never proposed
- [ ] tests for `ConvertToProposal`: the written file matches the advertised `FileSize` exactly
- [ ] tests for progress and cancellation: each format is reported as its turn starts, and a canceled token stops the search
- [ ] Keep images small and include AVIF in one test only, because it is by far the slowest format.
- [ ] validation commands pass

### Task 5: Test utilities and models

#### Files

- Create: `tests/Sic.Tests/FileHelperTests.cs`
- Create: `tests/Sic.Tests/ImageItemTests.cs`
- Create: `tests/Sic.Tests/LocalizationTests.cs`
- Create: `tests/Sic.Tests/UpdateIntervalTests.cs`
- Create: `tests/Sic.Tests/StaticStateCollection.cs`
- Modify: `src/Sic/Services/UpdateService.cs`

#### Steps

- [ ] Add `StaticStateCollection`, a non-parallel xUnit collection for tests that touch `Config`, `Console` or `Localization`.
- [ ] tests for `FileHelper.EnumerateImageFiles`: top folder only, recursive, several extensions, and an inaccessible folder being skipped. `IsCloudPlaceholder` returns false for a normal file.
- [ ] tests for `ImageItem`: the byte, KB and MB boundaries in `GetSizeDisplay`, and the dimension and description text, under a fixed culture
- [ ] tests for `Localization`, in the non-parallel collection: English source text, placeholders, plurals, and `SetLanguage` and `GetCurrentCulture` fallback. Assert no translated text.
- [ ] Make `UpdateService.ToFrequency` internal, then test every `UpdateCheckInterval`, including `Never` giving null.
- [ ] validation commands pass

### Task 6: Test the existing CLI

#### Files

- Create: `tests/Sic.Tests/CliTests.cs`
- Modify: `src/Sic/Program.cs`

#### Steps

- [ ] Make `Program.RunCli` internal, and call it directly with stderr captured through `Console.SetError`, in the non-parallel collection.
- [ ] tests for success: `-i` with `-o`, `-i` with `--format`, a URL input, `JPEG` and `TIF` spellings, every `--resize` form, and `--crop`
- [ ] tests for failure: missing input, no extension on `--output`, neither `--output` nor `--format`, an unsupported format, invalid `--resize` values, and `--crop` with one size. Each returns `ExitCode.Error` and writes to stderr.
- [ ] tests for skipping: same format, no resize, returns `ExitCode.Success`
- [ ] validation commands pass

### Task 7: Check the translation catalogs

#### Files

- Create: `tests/Sic.Tests/PoCatalog.cs`
- Create: `tests/Sic.Tests/TranslationCatalogTests.cs`

#### Steps

- [ ] Add `PoCatalog`, a small reader for `Sic.po` that finds the repository root by walking up to `Sic.sln`. It handles multi-line strings and plural entries, and skips the header entry and obsolete `#~` entries.
- [ ] tests over all six languages: no fuzzy entries and no empty `msgstr`
- [ ] tests for placeholders: each `msgstr` uses the same `{0}`-style placeholders as its `msgid`. Plural forms compare against `msgid_plural`, because `msgid "1 image in queue"` has no `{0}` while Russian `msgstr[0]` correctly does.
- [ ] Add a list of the menu strings at each menu level in `BuildMenuSpec`, and test that no level has duplicate mnemonics in any language. A duplicate throws `ArgumentException` at startup. Use `MenuTextFormatter.ExtractMnemonic` from `Oire.WinForms.NativeControls`.
- [ ] validation commands pass

### Task 8: Update project documentation

#### Files

- Modify: `CLAUDE.md`
- Modify: `README.md`

#### Steps

- [ ] `CLAUDE.md`: replace "There are no test projects" with `dotnet test`, the real format command `dotnet format --verify-no-changes`, test isolation through `userdata`, the non-parallel collection, and the rule against asserting translated text.
- [ ] `README.md`: mention `dotnet test` wherever build commands are listed.
- [ ] validation commands pass

## Technical details

### Test isolation

`App.IsPortable` is `Directory.Exists(Path.Combine(AppContext.BaseDirectory, "userdata"))`, so a `userdata` folder in the test output makes every path under `App.DataFolder` land in `bin`. The guard test in Task 1 fails loudly if that ever stops being true.

### Out of scope

- Automated tests that drive the forms. A UI Automation suite (FlaUI, for example) would be separate work.
- A HEIC decoding test. This Magick build can't write HEIC, so a test can't generate one; it needs a small committed sample.

## Post-completion

- Run a real `Build-Installer.ps1` build and confirm the test project isn't in the published output.
