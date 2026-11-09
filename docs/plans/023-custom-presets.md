# Custom presets

## Overview

Issue #23 asks for named conversion presets: a user who always posts WebP images 300 pixels wide should be able to pick "Blog" instead of setting the format, the resize checkbox, the mode and the width by hand each time.

Presets live in a `presets` sub-object of `sic.json`. They're available from a Presets submenu under Convert and from a new `--preset` CLI option. Issue #35 (Explorer context menu) comes next and will call `--preset`, so the CLI part must be solid.

It's the third of four plans. `89-json-config` must be merged first.

## Done when

- [ ] Convert > Presets lists saved presets, and offers Add Preset and Manage Presets, even when the queue is empty.
- [ ] Choosing a preset fills in the format and resize controls without converting, and says so in the status bar.
- [ ] A preset's sizes survive adding, selecting and removing images until the user changes the resize controls.
- [ ] A "Blog" preset (WEBP, keep proportions, width 300, no height) resizes a mixed batch of portrait and landscape images to 300 px wide.
- [ ] A height-only preset keeps its width box empty, with or without an image selected.
- [ ] A preset targeting a format unticked in Settings still applies, and that format stays in the dropdown until SIC! closes.
- [ ] A preset can be added, edited and removed with the keyboard, and the menu updates straight away.
- [ ] `sic -i a.png --preset Blog` converts with the preset. An explicit `--output` extension, `--format`, `--resize` or `--crop` wins over the matching part of the preset.
- [ ] An unknown `--preset` name exits with `ExitCode.Error` and lists the available presets on stderr.
- [ ] An invalid preset in a hand-edited `sic.json` drops only that preset, and is never lost silently: the file is backed up and the user is told.
- [ ] Saving Settings, or anything else, never loses presets.
- [x] A size that SIC! fills in itself never triggers an "invalid size" error.
- [ ] All new strings are translated into de, es, fr, he, ru and uk, and all 7 manuals are updated.
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

- Config: `src/Sic/Models/AppConfig.cs`, `src/Sic/Utils/Config.cs`, `src/Sic/Utils/ConfigSerializer.cs`
- CLI: `src/Sic/Program.cs` (`RunCli`)
- GUI: `src/Sic/MainWindow.cs`
- Conversion formats: `src/Sic/Services/ImageConverter.cs`
- Docs: `README.md`, `CHANGELOG.md`, `changelogs/1.2.0.0.md`, `CLAUDE.md`, `src/Sic/help/*/manual.html`
- Translations: `src/Sic/locale/{de,es,fr,he,ru,uk}/Sic.po`

### Patterns to follow

- Dialog constructor order: `src/Sic/FitToSizeDialog.cs`. `InitializeComponent()`, `Localizer.Localize(this, Localization.Catalog)`, `TextDirection.Apply(this)`, then `Text = _("...")`. Keep the English `Text` in the designer too.
- Checkbox enabling numeric fields, with 1 to 65535 validation: `FitToSizeDialog`.
- List with Add and Remove, nested dialog and duplicate rejection: `src/Sic/IcoPresetDialog.cs`. Take its behavior, not its layout.
- OK validation: override `OnFormClosing`, call `base`, return unless `DialogResult == DialogResult.OK`, show `DialogHelper.Show(...)`, focus and select the field, set `e.Cancel = true`. Results live in `{ get; private set; }` properties.
- Layout: one flat `TableLayoutPanel` in the designer, `Dock = Fill`, `Padding = new Padding(12)`, as in `FitToSizeDialog.Designer.cs`. Never pixel coordinates, so no control has a `Location` or a `Size`; the form's own `ClientSize` is the only size. No nested panels or group boxes unless that's absolutely unavoidable. `ColumnCount` and `RowCount` are the real counts, with one style for each column and row, and every control has `Dock = Fill`. Labels also keep `AutoSize = true` and add `TextAlign = ContentAlignment.MiddleLeft`, so they're neither clipped nor left at the top of the row; `MiddleLeft` mirrors by itself in Hebrew.
- Menu: `BuildMenuSpec` and `ApplyMenuState` in `MainWindow.cs`. `ApplyMenuState` is index-based and the separator counts, so append after the existing Convert items.
- Tests touching `Config`, `Console` or `Localization`: the `StaticStateCollection`. Tests of the CLI: `tests/Sic.Tests/CliTests.cs`, calling `Program.RunCli`.

### Conventions and traps

- `.editorconfig` requires CRLF, so run `dotnet format` on new `.cs` files before the format check.
- `.editorconfig` raises whole analyzer categories, and it applies to `tests/` too. Warnings are errors.
- Same-line braces, and `class X: Form` with no space before the colon. `_` is the gettext method, so `_ = ...` is not a discard.
- Every message box goes through `DialogHelper.Show`, never `MessageBox.Show`.
- Do not set `AccessibleName` on a control that already has meaningful `Text`.
- Data records go in `Models`. Services are named `...Service`, `-or` or `-er`.
- New enums are `public` in `src/Sic/Utils/Enums`, namespace `Oire.Sic.Utils.Enums`, like `UpdateCheckInterval`. `MainWindow.cs` and `Program.cs` need that using, and `Program.cs` also needs `Oire.Sic.Services`.
- `GetEnabledFormats` treats an empty list as "all formats", so it must never receive session formats on their own (Task 4).
- "Preset" already means ICO presets (`IcoPresetDialog`). Reuse the existing translation of that word in each language.
- `_isAutoFilling` is not re-entrant: `PopulateResizeFieldsFromSelection`, `AutoFillFromWidth` and `AutoFillFromHeight` all reset it in `finally`. Applying a preset must not rely on it (Task 5).
- `NativeMenuSpec.Add` returns the spec, not the item. Reach a new item through `spec.Items[^1]`.
- Never use `/dev/null`, `nul` or any other null device in a shell command.

## Development approach

- One task at a time; the validation commands pass before the next task starts.
- Code changes come with tests for the new and changed behavior, success and error paths. Forms have no automated tests here, so keep logic in plain classes that can be tested and keep form code thin.
- A test that cannot pass until a later task is still written now, with `[Fact(Skip = "Task N")]`, so the validation commands keep passing.
- Keep backward compatibility: the CLI's existing options keep working. The only intended behavior changes are listed under Technical details.
- When scope changes, update this plan: new tasks get a "➕" prefix, blockers a "⚠️" prefix.

## Implementation steps

### Task 1: Share resize validation

#### Files

- Create: `src/Sic/Utils/Enums/ResizeError.cs`
- Create: `src/Sic/Services/ResizeValidator.cs`
- Create: `tests/Sic.Tests/ResizeValidatorTests.cs`
- Modify: `src/Sic/MainWindow.cs`
- Modify: `src/Sic/Program.cs`
- Modify: `tests/Sic.Tests/CliTests.cs`

#### Steps

- [x] Add `ResizeValidator` as under Technical details. It's pure: no dialogs, no focus handling, no gettext.
- [x] Replace the parsing in `MainWindow.TryGetConversionParams` with `Validate`, using the GUI mapping under Technical details, with new per-box messages for a mistyped size in keep mode.
- [x] Add `ResizeValidator.ScaleDimension(target, sourceFrom, sourceTo)`, returning a size clamped to 1..`MaxDimension`, working in `double` and clamping before the cast, and use it in `AutoFillFromWidth` and `AutoFillFromHeight`. Today a 4000x100 image at width 16 gets height 0, which the stricter validation would reject.
- [x] Clamp in `PopulateResizeFieldsFromSelection` with `Math.Min(value, ResizeValidator.MaxDimension)`, because an image above 65535 px on a side would otherwise fail with a size SIC! typed itself.
- [x] Replace the parsing in `Program.RunCli` with `TryParseSpec` and `Validate`, using the CLI mapping under Technical details. It always passes `KeepProportions`, and the existing crop check stays where it is.
- [x] tests for `Validate`: keep mode with width only, height only and both; crop with both; the limits 1 and 65535
- [x] tests for `Validate`: blank fields, 0, 65536, non-numeric text with the other size valid, and crop missing either size
- [x] tests for the evaluation order: crop `("abc", "1")` gives `InvalidWidth`, crop `("", "abc")` gives `CropNeedsWidth`, and keep `("abc", "")`, `("", "abc")` and `("abc", "xyz")` give `InvalidWidth`, `InvalidHeight` and `InvalidWidth`
- [x] tests for `TryParseSpec`: `128x128`, `128x`, `x128`, `128`, `128X128`, and the rejected `1x2x3`
- [x] tests for `ScaleDimension`: a normal ratio, rounding to 0 giving 1, a result above 65535 giving 65535, and a huge target that would overflow an `int`
- [x] Add `CliTests` cases: `--resize 70000x` now fails, and `--resize 128x --crop` keeps its existing message.
- [x] validation commands pass

### Task 2: Add presets to the configuration

#### Files

- Create: `src/Sic/Models/ConversionPreset.cs`
- Create: `src/Sic/Utils/Enums/PresetNameResult.cs`
- Create: `src/Sic/Services/PresetValidator.cs`
- Create: `tests/Sic.Tests/PresetValidatorTests.cs`
- Modify: `src/Sic/Models/AppConfig.cs`
- Modify: `src/Sic/Utils/ConfigSerializer.cs`
- Modify: `src/Sic/Utils/Config.cs`
- Modify: `tests/Sic.Tests/ConfigSerializerTests.cs`
- Modify: `tests/Sic.Tests/ConfigTests.cs`

#### Steps

- [ ] Add `ConversionPreset` as under Technical details, and `Presets` to `AppConfig`.
- [ ] Add `PresetValidator`: `NameComparer` (`StringComparer.OrdinalIgnoreCase`); `CleanName(name)`, which trims and applies `Normalize(NormalizationForm.FormC)`; `ValidateName(name, takenNames)` returning `PresetNameResult` (`Valid`, `Empty`, `TooLong`, `InvalidCharacters`, `Duplicate`); `Normalize(presets)` for loaded data, returning the kept presets and the number dropped; and `EscapeAmpersands(name)`. Every comparison of names uses `NameComparer`, and every stored name goes through `CleanName`.
- [ ] In `ConfigSerializer`, read `presets` entry by entry from the `JsonDocument`, deserializing each to `ConversionPreset`, so that an entry that fails counts as dropped rather than sending the whole file to the corrupt path. A non-null `presets` that isn't an array counts as one dropped. Then run `Normalize`.
- [ ] Join this to step 1 of the load flow in `89-json-config`: bad settings and dropped presets share one `BackUpFile` call, one repaired save and one complete translated message in `LoadError`, naming both counts. If the backup fails, set `SavesBlocked` and say so.
- [ ] Add `public static List<ConversionPreset> Presets { get; set; }` to `Config`. `Load` sets it from `result.Config.Presets`, and `Save` passes both `General` and `Presets`.
- [ ] tests for `ValidateName`: trimmed names, 64 characters, and names in any script, with `Блог`, `Démarche administrative` and `בלוג` all `Valid`
- [ ] tests for `ValidateName`: empty, 65 characters, tab and other control characters, duplicates differing only by case or by a composed and decomposed `é`
- [ ] tests for `Normalize`: each unusable case under Technical details is dropped and counted, order is kept, a name with spaces around it is stored cleaned, the format is upper-cased, JPEG maps to JPG and TIF to TIFF, and a preset with no sizes stores `KeepProportions`
- [ ] tests for `EscapeAmpersands`: `R&D` becomes `R&&D`, `MenuTextFormatter.ExtractMnemonic(EscapeAmpersands("R&D"))` is null, and a name with no ampersand is unchanged
- [ ] tests for `ConfigSerializer`: presets round trip; null `presets` and `presets: [null]` load safely; `presets: {}` and `presets: "x"` count as dropped; a file with a dropped preset is backed up and reported; a file with both a bad setting and a dropped preset gets one backup and one message; and the next load is silent
- [ ] tests for per-entry recovery: `"resizeMode": "Fit"`, `"resizeMode": 5`, `"width": "300"`, `"width": 300.5`, `"name": null` and `"format": null` each drop one preset, while the other presets and the `general` values survive
- [ ] tests for a failed backup: with the `.bad` file locked, `SavesBlocked` is true and the file is unchanged
- [ ] tests for `Config`: load a file with presets, change a general setting, call `Save`, reload, and assert the presets are still there
- [ ] validation commands pass

### Task 3: Add --preset to the CLI

#### Files

- Create: `src/Sic/Services/PresetResolver.cs`
- Create: `tests/Sic.Tests/PresetResolverTests.cs`
- Modify: `src/Sic/Program.cs`
- Modify: `tests/Sic.Tests/CliTests.cs`

#### Steps

- [ ] Add `PresetResolver` with `Find(presets, name)`, matching through `CleanName` and `NameComparer`, and `ResolveResize(preset, hasExplicitResize, explicitWidth, explicitHeight, crop)` returning `(int? Width, int? Height, ResizeMode Mode)`. It accepts a null preset, because every run goes through the merge, and follows the precedence under Technical details.
- [ ] Add `var presetOption = new Option<string?>("--preset", "-p")` with a localized description, and register it on the root command.
- [ ] Look the preset up before the format chain, so an unknown name fails even when the preset isn't needed. Unknown preset: log it, print the error and the available names to stderr, and return `ExitCode.Error`. Say so plainly when there are no presets.
- [ ] Target format order: `--output` extension, then `--format`, then the preset. Update the "Either --output or --format" error to mention `--preset`, and update its assertion in `CliTests`.
- [ ] Move the block that builds the default output path out of the `--format` branch, to after the supported-format check, under `if (string.IsNullOrWhiteSpace(output))`. It then also runs when only the preset gives the format, `-o` is never overwritten, and a bad `--format` no longer creates the output folder.
- [ ] Keep the crop check on the `crop` flag, after the merge, so a preset with no sizes can't trigger it.
- [ ] tests for `Find`: exact match, different case, `блог` finding `Блог`, `DÉMARCHE ADMINISTRATIVE` finding `Démarche administrative`, unknown name, empty list
- [ ] tests for `ResolveResize`: a null preset; preset only; `--resize` replacing the preset's size and mode; `--crop` alone over a preset with both sizes; a preset with no resize
- [ ] `CliTests`, writing a `sic.json` into `App.DataFolder` and using `TestDataFolder.Clear()`: preset only, with the default output path; `--output` extension beating the preset with the preset's sizes still applied; `-o noext --preset` keeping its existing error; `--format` beating it; `--crop` with a one-size preset giving the crop error; a preset name with spaces and non-Latin letters; an unknown preset's exit code and stderr, even with `--format`; no presets at all
- [ ] validation commands pass

### Task 4: Add the testable preset helpers

#### Files

- Create: `src/Sic/Utils/Enums/DimensionBox.cs`
- Create: `src/Sic/Services/PresetCapturer.cs`
- Create: `tests/Sic.Tests/PresetCapturerTests.cs`
- Modify: `src/Sic/Services/ImageConverter.cs`
- Modify: `tests/Sic.Tests/ImageConverterFormatTests.cs`
- Modify: `src/Sic/MainWindow.cs`

#### Steps

- [ ] Add `ImageConverter.GetDropdownFormats(enabledKeys, sessionFormats)`: call `GetEnabledFormats(enabledKeys)`, add the session formats, then filter `GetSupportedFormats()` by that set, so the order stays canonical. Never pass the combined keys to `GetEnabledFormats`: an empty list means "all formats", and it also has a fallback when nothing it's given is known.
- [ ] Add a private `HashSet<string> _sessionFormats` to `MainWindow`, and make `PopulateFormatComboBox` use `GetDropdownFormats`.
- [ ] Add `PresetCapturer.Capture(format, resizeEnabled, mode, widthText, heightText, autoFilledBox)` and a `DimensionBox` enum (`None`, `Width`, `Height`, `Both`), with the rules under Technical details.
- [ ] tests for `GetDropdownFormats`: no session formats, a hidden format added in canonical order, empty `enabledKeys` still showing all, `enabledKeys` matching nothing known still showing all, and no duplicates
- [ ] tests for `Capture`: resize off; crop; keep mode with each `autoFilledBox` value; both boxes typed with no selection; one box cleared while the other holds a typed size, which keeps it; one box cleared while SIC! filled the other, which gives no sizes; `Both`, which gives no sizes; unparsable text; and no sizes storing `KeepProportions`
- [ ] validation commands pass

### Task 5: Apply a preset in the main window

#### Files

- Modify: `src/Sic/MainWindow.cs`

#### Steps

- [ ] Add `_isApplyingPreset`, `_presetSizesPinned` and `DimensionBox _autoFilledBox` fields.
- [ ] Add `ApplyPreset(ConversionPreset preset)` exactly as under Technical details. It doesn't use `_isAutoFilling`.
- [ ] `WidthTextBox_TextChanged`, `HeightTextBox_TextChanged` and `ResizeModeRadioButton_CheckedChanged` return at once when `_isApplyingPreset` is set.
- [ ] `ResizeCheckBox_CheckedChanged` still enables and disables the fields when `_isApplyingPreset` is set, but skips `PopulateResizeFieldsFromSelection` and `UpdatePreview`. It must not return early, or the boxes stay disabled.
- [ ] `ImageListView_SelectedIndexChanged` skips `PopulateResizeFieldsFromSelection` while `_presetSizesPinned` is set.
- [ ] Clear the pin when the user edits a size box, toggles the resize checkbox or switches the radio buttons, which means in those handlers when neither `_isApplyingPreset` nor `_isAutoFilling` is set.
- [ ] Set `_autoFilledBox` in `AutoFillFromWidth` and `AutoFillFromHeight` when they write a box, and to `Both` in `PopulateResizeFieldsFromSelection`, which writes both. When the user edits a box, take that box out of the value, so `Both` becomes the other box alone. Set it to `None` in `ApplyPreset`.
- [ ] validation commands pass

### Task 6: Add the preset editor dialog

#### Files

- Create: `src/Sic/PresetDialog.cs`
- Create: `src/Sic/PresetDialog.Designer.cs`

#### Steps

- [ ] Build the designer as one flat `TableLayoutPanel` with the grid and control texts under Technical details. Resize mode is a `DropDownList` `ComboBox` with plain `_("Keep proportions")` and `_("Crop")` items, added in the constructor. It can't be radio buttons in the table: arrow keys would walk through every control beside them, and reaching one by arrow or mnemonic would select it.
- [ ] Constructor `PresetDialog(ConversionPreset? initial, IEnumerable<string> takenNames, bool isEdit)`. Follow the standard constructor order, and set the title to "Add Preset" or "Edit Preset". The dropdown lists `GetSupportedFormats()`, not only the enabled formats, and selects the first when `initial` is null. When editing, callers leave the preset's own name out of `takenNames`, so a case-only rename works.
- [ ] Enable the resize mode, width and height fields from the Resize checkbox, as `FitToSizeDialog` does, but don't move focus when it's ticked, or the mode would be skipped. Run the enable routine once after loading `initial`, so an edited preset can't show a ticked box with disabled fields. Unlike the main window, typing one size must never auto-fill the other.
- [ ] Add the hint: "With Keep proportions, fill in one size to scale every image to it, or both to fit images inside that box."
- [ ] Validate in `OnFormClosing` with `PresetValidator.ValidateName`, and with `ResizeValidator.Validate` only when Resize is ticked, because stale text in a disabled box must not block OK. Show one specific message per error and move focus to the field.
- [ ] Expose the outcome as `public ConversionPreset? Result { get; private set; }`. Its name goes through `CleanName`. With Resize unticked it has null sizes and `KeepProportions`.
- [ ] validation commands pass

### Task 7: Add the Manage Presets dialog

#### Files

- Create: `src/Sic/ManagePresetsDialog.cs`
- Create: `src/Sic/ManagePresetsDialog.Designer.cs`
- Create: `src/Sic/Services/PresetDescriber.cs`
- Create: `tests/Sic.Tests/PresetDescriberTests.cs`

#### Steps

- [ ] Set `Text = _("Manage Presets")` in the constructor, after `TextDirection.Apply`, because the extractor can't see a form's own title.

- [ ] Build the designer as one flat `TableLayoutPanel` with the grid under Technical details, not from `IcoPresetDialog`'s layout, which nests a `GroupBox` and a `FlowLayoutPanel`.
- [ ] Add `PresetDescriber.Describe(preset)`, and show each list row as the preset's name plus that summary, such as "Blog: WEBP, width 300". Set `HorizontalScrollbar = true` on the list, because a 64-character name plus its summary is wider than the dialog.
- [ ] tests for `Describe`, one for each form: "WEBP", "WEBP, width 300", "WEBP, height 200", "WEBP, fit inside 800x600" and "PNG, crop to 128x128"
- [ ] Add and Edit open `PresetDialog`. Remove asks for confirmation through `DialogHelper.Show`. Edit and Remove are disabled with nothing selected, and focus returns to the list predictably after each action.
- [ ] Set `closeButton.DialogResult = DialogResult.Cancel` and `CancelButton = closeButton`, so Esc closes the dialog.
- [ ] Work on a copy of `Config.Presets`. In `OnFormClosing`, so Esc and Alt+F4 don't lose edits, and only if something changed, write it back, call `Config.Save()`, and set a `Changed` property for the caller.
- [ ] validation commands pass

### Task 8: Add the Presets submenu

#### Files

- Create: `src/Sic/Utils/PresetMenuBuilder.cs`
- Create: `tests/Sic.Tests/PresetMenuBuilderTests.cs`
- Modify: `src/Sic/MainWindow.cs`

#### Steps

- [ ] Add `PresetMenuBuilder.AddTo(NativeMenuSpec convert, presets, onApply, onAdd, onManage)`, appending a separator and `convert.AddMenu(_("&Presets"), ...)`. It lists each preset through `PresetValidator.EscapeAmpersands`, then a separator, `_("&Add Preset...")` and `_("&Manage Presets...")`.
- [ ] Show a disabled "No presets yet" item when the list is empty, so a screen reader user hears why the top of the menu has no presets.
- [ ] Call it at the end of the Convert block in `BuildMenuSpec`. In `ApplyMenuState`, remove both `convert.IsEnabled = _hasItems` and `edit.IsEnabled = _hasItems`; their children keep their own flags, and Presets stays enabled. Root items are pushed to Windows only when the menu is built, so a rebuild would otherwise freeze the Edit menu at whatever the queue state was then.
- [ ] Add Preset opens `PresetDialog` pre-filled from `PresetCapturer.Capture`. After OK, append to `Config.Presets`, call `Config.Save()` and rebuild with `_menuBar?.Attach(BuildMenuSpec())` once the dialog has closed. Manage Presets rebuilds the same way when `Changed` is true.
- [ ] tests: a spec with presets named `R&D`, `Blog` and `Banner` passes `MenuSpecValidator.Validate`; an empty list gives one disabled row; 200 presets pass. `NativeMenuSpec.Add` needs a non-null `onClick`, so the disabled row passes a no-op.
- [ ] validation commands pass

### Task 9: Translate all new strings

#### Files

- Modify: `src/Sic/locale/messages.pot`
- Modify: `src/Sic/locale/{de,es,fr,he,ru,uk}/Sic.po`
- Modify: `tests/Sic.Tests/TranslationCatalogTests.cs`

#### Steps

- [ ] Run `Extract-Strings.ps1`, then `Update-Translations.ps1`.
- [ ] Translate every new string in all six languages, reusing each language's existing word for "preset".
- [ ] Remove every `#, fuzzy` flag on new entries. Window titles must have no `&` and no trailing `...`.
- [ ] Add `&Presets` to `ConvertMenuStrings` in `TranslationCatalogTests`, and a new `PresetsMenuStrings` with `&Add Preset...` and `&Manage Presets...`.
- [ ] Add `PresetDialogStrings` and `ManagePresetsDialogStrings` with the control texts under Technical details, because a clash there sends Alt+letter to the wrong control. Where a language clashes, change the mnemonic of a new string, not an existing one.
- [ ] Run `Compile-Translations.ps1`.
- [ ] validation commands pass, including `TranslationCatalogTests`

### Task 10: Update the manuals

#### Files

- Modify: `src/Sic/help/{en,de,es,fr,he,ru,uk}/manual.html`

#### Steps

- [ ] Add a Presets section to the English manual: adding, applying, editing and removing, the rule that a preset fills in the controls without converting, one size versus both, and hidden formats. Say that ICO presets are a separate thing.
- [ ] Document `--preset` and its precedence in the command-line section.
- [ ] Translate all of this into the other six manuals, matching the structure and heading levels already there.
- [ ] validation commands pass

### Task 11: Update project documentation

#### Files

- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `changelogs/1.2.0.0.md`
- Modify: `CLAUDE.md`

#### Steps

- [ ] `README.md`: add presets and `--preset`.
- [ ] `CHANGELOG.md` and `changelogs/1.2.0.0.md`: add presets ([#23](https://github.com/Oire/sic/issues/23)) and the behavior changes listed under Technical details.
- [ ] `CLAUDE.md`: add the new classes, the `Oire.Sic.Utils.Enums` namespace and the CLI option, and record the gotchas future work needs: the preset pin and `_isApplyingPreset`, why session formats go through `GetDropdownFormats`, and escaping names for the menu.
- [ ] validation commands pass

## Technical details

### sic.json

The `presets` sub-object joins `general`:

```json
{
  "general": { "language": "System" },
  "presets": [
    { "name": "Blog", "format": "WEBP", "resizeMode": "KeepProportions", "width": 300 },
    { "name": "Avatar", "format": "PNG", "resizeMode": "Crop", "width": 128, "height": 128 }
  ]
}
```

`presets` is an array rather than an object keyed by name, because that keeps the user's order.

### ConversionPreset

```csharp
public sealed class ConversionPreset {
    public string Name { get; set; } = "";
    public string Format { get; set; } = "";
    public ResizeMode ResizeMode { get; set; } = ResizeMode.KeepProportions;
    public int? Width { get; set; }
    public int? Height { get; set; }

    [JsonIgnore]
    public bool Resizes => Width.HasValue || Height.HasValue;
}
```

A preset resizes exactly when it has a size, so there is no separate flag that could disagree with the sizes.

### Normalize

An entry is unusable, and dropped with a logged warning, when any of these is true:

- it is null, or its name or format is null
- its name fails `ValidateName`
- its name duplicates an earlier entry, ignoring case
- its format isn't in `GetSupportedFormats()` after upper-casing and mapping JPEG to JPG and TIF to TIFF (this excludes HEIC, which is input-only)
- a size is outside 1..`ResizeValidator.MaxDimension`
- its mode is an undefined enum value, or Crop with only one size

A preset with no sizes is stored with `KeepProportions`, in `Normalize`, `PresetDialog.Result` and `PresetCapturer.Capture`. A leftover Crop there would select the Crop radio button while Resize is off.

Entries that fail to deserialize, such as `"width": "300"`, count as dropped too. Because dropped entries would disappear from disk on the next save, `ConfigSerializer` calls `BackUpFile` first and tells the user how many were dropped.

Names are at most 64 characters, trimmed, and unique ignoring case. Any script is fine, with or without diacritics, such as `Блог`, `Démarche administrative` or `בלוג`. `InvalidCharacters` means control characters and nothing else. `CleanName` means a name typed with a decomposed `é` still matches one stored composed.

### ResizeValidator

```csharp
internal static class ResizeValidator {
    public const int MaxDimension = 65535;
    public static ResizeError Validate(string widthText, string heightText, ResizeMode mode, out int? width, out int? height);
    public static bool TryParseSpec(string spec, out string widthText, out string heightText);
    public static int ScaleDimension(int target, int sourceFrom, int sourceTo);
}
```

- `ResizeError` is an enum with `None`, `InvalidWidth`, `InvalidHeight`, `MissingDimension`, `CropNeedsWidth` and `CropNeedsHeight`.
- `Validate` trims both texts. Non-empty text that isn't an integer from 1 to `MaxDimension` is an error.
- It checks the width first, then the height.
- In crop mode, a blank box gives `CropNeedsWidth` or `CropNeedsHeight`, and bad text gives `InvalidWidth` or `InvalidHeight`.
- In keep mode, bad text gives `InvalidWidth` or `InvalidHeight`, and two blanks give `MissingDimension`.
- `TryParseSpec` keeps the CLI's accepted forms: `WxH`, `Wx`, `xH` and a bare `W`.

GUI mapping in `TryGetConversionParams`:

- Crop with `InvalidWidth` or `CropNeedsWidth`: the existing "Crop mode requires a valid width" dialog, focusing width. The same for height.
- Keep with `MissingDimension`: the existing "at least one valid dimension" dialog, focusing width.
- Keep with `InvalidWidth` or `InvalidHeight`: a new per-box message that focuses the bad box, whatever the other box holds.

CLI mapping in `RunCli`, which always passes `KeepProportions`:

- `TryParseSpec` returning false: the existing "Use WxH, Wx, xH, or W" message.
- `InvalidWidth`: "Invalid width value: {0}" with the width text. `InvalidHeight`: the height equivalent.
- `MissingDimension`: the existing "At least one dimension is required" message.

### PresetCapturer rules

- Resize off: no sizes, and `KeepProportions`.
- Text that isn't a valid size is left out.
- Crop keeps both sizes.
- Keep proportions leaves out only what SIC! filled itself (`autoFilledBox`, which is both boxes for `Both`), because those values were calculated for one image and would turn the preset into a fit-inside-a-box. Everything the user typed stays, so typing both sizes with nothing selected keeps both.

### ApplyPreset

```csharp
private void ApplyPreset(ConversionPreset preset) {
    _isApplyingPreset = true;
    try {
        if (!formatComboBox.Items.Contains(preset.Format)) {
            _sessionFormats.Add(preset.Format);
            PopulateFormatComboBox();
        }

        formatComboBox.SelectedItem = preset.Format;
        resizeCheckBox.Checked = preset.Resizes;
        cropRadioButton.Checked = preset.ResizeMode == Models.ResizeMode.Crop;
        keepProportionsRadioButton.Checked = preset.ResizeMode != Models.ResizeMode.Crop;
        widthTextBox.Text = preset.Width?.ToString() ?? "";
        heightTextBox.Text = preset.Height?.ToString() ?? "";
    } finally {
        _isApplyingPreset = false;
    }

    _presetSizesPinned = preset.Resizes;
    _autoFilledBox = DimensionBox.None;
    UpdatePreview();
    statusLabel.Text = _("Preset applied: {0}", PresetValidator.EscapeAmpersands(preset.Name));
}
```

### Dialog layouts

`PresetDialog` has 3 columns (AutoSize, Percent 50, Percent 50), 8 rows (six AutoSize, then Percent 100, then AutoSize) and a `ClientSize` of 440x300:

1. `&Name:` label; name box spanning columns 2 and 3
2. `Target &Format:` label; format dropdown spanning columns 2 and 3
3. `Resi&ze` checkbox spanning all 3 columns
4. `Resize &Mode:` label; resize mode dropdown spanning columns 2 and 3
5. `&Width:` label; width box spanning columns 2 and 3
6. `&Height:` label; height box spanning columns 2 and 3
7. Hint label spanning all 3 columns, with `AutoSize = false`, so it wraps inside the row that absorbs spare height
8. Empty cell; `&OK`; `&Cancel`

Without the Percent row, `TableLayoutPanel` hands spare height to the last row and the buttons grow tall, by a different amount in each language.

`ManagePresetsDialog` has 4 columns (Percent 25 each), 3 rows (AutoSize, Percent 100, AutoSize) and a `ClientSize` of 440x320:

1. `Pre&sets:` label spanning all 4 columns
2. Presets `ListBox` spanning all 4 columns
3. `&Add...`; `&Edit...`; `&Remove`; `&Close`

### CLI precedence

- Format: `--output` extension, then `--format`, then the preset.
- Size and mode with `--resize`: the sizes come from `--resize`, the mode comes only from `--crop`, and the preset's resize settings are ignored entirely.
- Without `--resize`: sizes come from the preset, and the mode is Crop if `--crop` is given, otherwise the preset's mode.
- The existing check that crop needs both sizes runs after this merge, on the `crop` flag.

The default output path is built after the supported-format check, under `if (string.IsNullOrWhiteSpace(output))`, so:

- `-o a.jpg`, alone or with a preset: the extension wins, `-o` is kept, and preset sizes still apply.
- `-o noext` with a preset: the existing error stays.
- `--format`, alone or with a preset: the format wins, with the default path.
- A preset alone: the preset's format, with the default path.
- None of the three: the updated error.

### Intended behavior changes

- The GUI now gives a mistyped size its own message and moves focus to that box, whatever the other box holds. It used to drop it silently, or show the general message.
- The CLI now rejects sizes above 65535, matching the GUI. It had no upper limit.
- Filled-in sizes are clamped to 1..65535. They could be 0 or too large before.
- The Edit and Convert menus are always enabled, with their items enabled individually. Convert being disabled on an empty queue would have hidden Presets.
- A bad `--format` no longer creates the output folder.

### Out of scope

- Exporting and importing presets as separate shareable files. The issue lists it as optional, and it can follow on from the array in `sic.json`.
- Reordering presets by hand (#92), and Ctrl+1 to Ctrl+0 for the first ten presets (#93).
- Converting directly from a preset in the GUI. #35 covers one-step conversion through the CLI.
- Replacing format string keys with an enum (#94). `Format` stays a string here, and the text in `sic.json` won't change when that happens.

## Post-completion

- Test the whole preset flow by keyboard with JAWS and NVDA: Convert > Presets on an empty queue, Add Preset, the hint, validation messages and focus, Manage Presets, and Remove.
- Check that JAWS and NVDA read the hint in the preset dialog, which is a label outside the tab order. If they don't, set `AccessibleDescription` on the two size boxes in the constructor.
- Confirm a preset's sizes survive adding, selecting and removing images, and that a height-only preset keeps its width box empty.
- Convert a mixed portrait and landscape batch with a width-only preset, and confirm every result is 300 px wide.
- Apply a preset with a format unticked in Settings, and confirm the format stays in the dropdown until SIC! closes.
- Check whether the "Preset applied" status text is heard after the menu closes. If it isn't, decide whether SIC! needs a spoken announcement there; the other status messages have the same limitation.
- Apply a no-resize preset while focus is in a size box. Disabling that box should move focus, and the result should be heard.
- Start with an empty queue, add an image, and confirm Alt+E and Alt+C open their menus.
- Launch once in each language to confirm the menu attaches, and check the dialogs and menu in Hebrew for right-to-left layout.
