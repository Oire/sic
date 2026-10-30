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
- [ ] An invalid preset in a hand-edited `sic.json` is never lost silently: the file is backed up and the user is told.
- [ ] A size that SIC! fills in itself never triggers an "invalid size" error.
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
- List with Add and Remove, nested dialog, duplicate rejection, radio group: `src/Sic/IcoPresetDialog.cs`.
- OK validation: override `OnFormClosing`, call `base`, return unless `DialogResult == DialogResult.OK`, show `DialogHelper.Show(...)`, focus and select the field, set `e.Cancel = true`. Results live in `{ get; private set; }` properties.
- Layout: a flat `TableLayoutPanel` in the designer, `Dock = Fill`, `Padding = new Padding(12)`, as in `FitToSizeDialog.Designer.cs`. Never pixel coordinates.
- Menu: `BuildMenuSpec` and `ApplyMenuState` in `MainWindow.cs`. `ApplyMenuState` is index-based and the separator counts, so append after the existing Convert items.
- Tests touching `Config`, `Console` or `Localization`: the `StaticStateCollection`. Tests of the CLI: `tests/Sic.Tests/CliTests.cs`, calling `Program.RunCli`.

### Conventions and traps

- `.editorconfig` requires CRLF, so run `dotnet format` on new `.cs` files before the format check.
- `.editorconfig` raises whole analyzer categories, and it applies to `tests/` too. Warnings are errors.
- Same-line braces, and `class X: Form` with no space before the colon. `_` is the gettext method, so `_ = ...` is not a discard.
- Every message box goes through `DialogHelper.Show`, never `MessageBox.Show`.
- Do not set `AccessibleName` on a control that already has meaningful `Text`.
- Data records go in `Models`. Services are named `...Service`, `-or` or `-er`.
- `GetEnabledFormats` treats an empty list as "all formats", so it must never receive session formats on their own (Task 4).
- "Preset" already means ICO presets (`IcoPresetDialog`). Reuse the existing translation of that word in each language.
- `_isAutoFilling` is not re-entrant: `PopulateResizeFieldsFromSelection`, `AutoFillFromWidth` and `AutoFillFromHeight` all reset it in `finally`. Applying a preset must not rely on it (Task 5).
- `NativeMenuSpec.Add` returns the spec, not the item. Reach a new item through `spec.Items[^1]`.
- Never use `/dev/null`, `nul` or any other null device in a shell command.

## Development approach

- One task at a time; the validation commands pass before the next task starts.
- Code changes come with tests for the new and changed behavior, success and error paths. Forms have no automated tests here, so keep logic in plain classes that can be tested and keep form code thin.
- A test that cannot pass until a later task is still written now, marked with a comment naming that task.
- Keep backward compatibility: the CLI's existing options keep working. The only intended behavior changes are listed under Technical details.
- When scope changes, update this plan: new tasks get a "➕" prefix, blockers a "⚠️" prefix.

## Implementation steps

### Task 1: Share resize validation

#### Files

- Create: `src/Sic/Services/ResizeValidator.cs`
- Create: `tests/Sic.Tests/ResizeValidatorTests.cs`
- Modify: `src/Sic/MainWindow.cs`
- Modify: `src/Sic/Program.cs`
- Modify: `tests/Sic.Tests/CliTests.cs`

#### Steps

- [ ] Add `ResizeValidator` as under Technical details. It's pure: no dialogs, no focus handling, no gettext.
- [ ] Replace the parsing in `MainWindow.TryGetConversionParams` with `Validate`, keeping the same dialogs and focus behavior for each error. Add one new message for a mistyped value when the other size is valid.
- [ ] Add `ResizeValidator.ScaleDimension(target, sourceFrom, sourceTo)`, returning a size clamped to 1..`MaxDimension`, and use it in `AutoFillFromWidth` and `AutoFillFromHeight`. Today a 4000x100 image at width 16 gets height 0, which the stricter validation would reject.
- [ ] Replace the parsing in `Program.RunCli` with `TryParseSpec` and `Validate`, keeping the existing stderr messages and exit codes.
- [ ] tests for `Validate`: keep mode with width only, height only and both; crop with both; the limits 1 and 65535
- [ ] tests for `Validate`: blank fields, 0, 65536, non-numeric text with the other size valid, and crop missing either size
- [ ] tests for `TryParseSpec`: `128x128`, `128x`, `x128`, `128`, `128X128`, and the rejected `1x2x3`
- [ ] tests for `ScaleDimension`: a normal ratio, rounding to 0 giving 1, and a result above 65535 giving 65535
- [ ] Add a `CliTests` case: `--resize 70000x` now fails.
- [ ] validation commands pass

### Task 2: Add presets to the configuration

#### Files

- Create: `src/Sic/Models/ConversionPreset.cs`
- Create: `src/Sic/Services/PresetValidator.cs`
- Create: `tests/Sic.Tests/PresetValidatorTests.cs`
- Modify: `src/Sic/Models/AppConfig.cs`
- Modify: `src/Sic/Models/ConfigLoadResult.cs`
- Modify: `src/Sic/Utils/ConfigSerializer.cs`
- Modify: `src/Sic/Utils/Config.cs`
- Modify: `tests/Sic.Tests/ConfigSerializerTests.cs`

#### Steps

- [ ] Add `ConversionPreset` as under Technical details, and `Presets` to `AppConfig`.
- [ ] Add `PresetValidator`: `ValidateName(name, takenNames)` returning an enum (`Valid`, `Empty`, `TooLong`, `InvalidCharacters`, `Duplicate`), `Normalize(presets)` for loaded data, and `EscapeForMenu(name)`.
- [ ] In `ConfigSerializer`, run `Normalize` on load. When it drops anything, copy the file to `sic.json.bad` before anything can save over it, and put a message naming the count in `LoadError`.
- [ ] Add `public static List<ConversionPreset> Presets { get; set; }` to `Config`.
- [ ] tests for `ValidateName`: trimmed names, 64 characters, Cyrillic and Hebrew names
- [ ] tests for `ValidateName`: empty, 65 characters, tab and other control characters, duplicates differing only by case
- [ ] tests for `Normalize`: each unusable case under Technical details is dropped, order is kept, the format is upper-cased, and JPEG maps to JPG and TIF to TIFF
- [ ] tests for `EscapeForMenu`: `R&D` becomes `R&&D`, `MenuTextFormatter.ExtractMnemonic(EscapeForMenu("R&D"))` is null, and a name with no ampersand is unchanged
- [ ] tests for `ConfigSerializer`: presets round trip, null `presets` and `presets: [null]` load safely, and a file with a dropped preset is backed up to `sic.json.bad` and reported
- [ ] validation commands pass

### Task 3: Add --preset to the CLI

#### Files

- Create: `src/Sic/Services/PresetResolver.cs`
- Create: `tests/Sic.Tests/PresetResolverTests.cs`
- Modify: `src/Sic/Program.cs`
- Modify: `tests/Sic.Tests/CliTests.cs`

#### Steps

- [ ] Add `PresetResolver` with `Find(presets, name)`, matching case-insensitively, and `ResolveResize(preset, hasExplicitResize, explicitWidth, explicitHeight, crop)`, following the precedence under Technical details.
- [ ] Add `var presetOption = new Option<string?>("--preset", "-p")` with a localized description, and register it on the root command.
- [ ] Unknown preset: log it, print the error and the available names to stderr, and return `ExitCode.Error`. Say so plainly when there are no presets.
- [ ] Target format order: `--output` extension, then `--format`, then the preset. Move the output-path block out of the `--format` branch (`Program.cs:193-202`) so it also runs when only the preset gives the format. Update the "Either --output or --format" error to mention `--preset`, and update its assertion in `CliTests`.
- [ ] tests for `Find`: exact match, different case, Cyrillic, unknown name, empty list
- [ ] tests for `ResolveResize`: preset only; `--resize` replacing the preset's size and mode; `--crop` alone over a preset with both sizes; crop with one size reports the error; a preset with no resize
- [ ] `CliTests`, writing a `sic.json` into `App.DataFolder`: preset only, with the default output path; `--output` extension beating the preset; `--format` beating it; an unknown preset's exit code and stderr; no presets at all
- [ ] validation commands pass

### Task 4: Add the testable preset helpers

#### Files

- Create: `src/Sic/Services/PresetCapturer.cs`
- Create: `tests/Sic.Tests/PresetCapturerTests.cs`
- Modify: `src/Sic/Services/ImageConverter.cs`
- Modify: `tests/Sic.Tests/ImageConverterFormatTests.cs`
- Modify: `src/Sic/MainWindow.cs`

#### Steps

- [ ] Add `ImageConverter.GetDropdownFormats(enabledKeys, sessionFormats)`: the enabled formats plus the session formats, in `GetSupportedFormats` order. When `enabledKeys` is empty every format is already shown, so session formats must not be passed to `GetEnabledFormats` then.
- [ ] Add a private `HashSet<string> _sessionFormats` to `MainWindow`, and make `PopulateFormatComboBox` use `GetDropdownFormats`.
- [ ] Add `PresetCapturer.Capture(format, resizeEnabled, mode, widthText, heightText, lastTyped)` and a `DimensionBox` enum (`None`, `Width`, `Height`), with the rules under Technical details.
- [ ] tests for `GetDropdownFormats`: no session formats, a hidden format added in canonical order, empty `enabledKeys` still showing all, and no duplicates
- [ ] tests for `Capture`: resize off, crop, keep mode with each `lastTyped` value, and unparsable text
- [ ] validation commands pass

### Task 5: Apply a preset in the main window

#### Files

- Modify: `src/Sic/MainWindow.cs`

#### Steps

- [ ] Add `_isApplyingPreset`, `_presetSizesPinned` and `DimensionBox _lastTypedBox` fields.
- [ ] Add `ApplyPreset(ConversionPreset preset)` exactly as under Technical details. It doesn't use `_isAutoFilling`.
- [ ] `WidthTextBox_TextChanged`, `HeightTextBox_TextChanged` and `ResizeModeRadioButton_CheckedChanged` return at once when `_isApplyingPreset` is set.
- [ ] `ResizeCheckBox_CheckedChanged` still enables and disables the fields when `_isApplyingPreset` is set, but skips `PopulateResizeFieldsFromSelection` and `UpdatePreview`. It must not return early, or the boxes stay disabled.
- [ ] `ImageListView_SelectedIndexChanged` skips `PopulateResizeFieldsFromSelection` while `_presetSizesPinned` is set.
- [ ] Clear the pin when the user edits a size box, toggles the resize checkbox or switches the radio buttons, which means in those handlers when neither `_isApplyingPreset` nor `_isAutoFilling` is set. Record `_lastTypedBox` in the text handlers under the same condition, and reset it to `None` in `PopulateResizeFieldsFromSelection`.
- [ ] validation commands pass

### Task 6: Add the preset editor dialog

#### Files

- Create: `src/Sic/PresetDialog.cs`
- Create: `src/Sic/PresetDialog.Designer.cs`

#### Steps

- [ ] Build the designer as a flat `TableLayoutPanel`: name, format dropdown, resize checkbox, a keep proportions or crop group, width, height, a hint label, OK and Cancel. Give every control a mnemonic and a logical tab order.
- [ ] Constructor `PresetDialog(ConversionPreset? initial, IEnumerable<string> takenNames, bool isEdit)`. Follow the standard constructor order, and set the title to "Add Preset" or "Edit Preset". The dropdown lists `GetSupportedFormats()`, not only the enabled formats. When editing, callers leave the preset's own name out of `takenNames`, so a case-only rename works.
- [ ] Use the checkbox-enabled fields from `FitToSizeDialog`. Unlike the main window, typing one size must never auto-fill the other.
- [ ] Add the hint: "With Keep proportions, fill in one size to scale every image to it, or both to fit images inside that box."
- [ ] Validate in `OnFormClosing` with `PresetValidator.ValidateName` and `ResizeValidator.Validate`, showing one specific message per error and moving focus to the field. Expose the outcome as `public ConversionPreset? Result { get; private set; }`.
- [ ] validation commands pass

### Task 7: Add the Manage Presets dialog

#### Files

- Create: `src/Sic/ManagePresetsDialog.cs`
- Create: `src/Sic/ManagePresetsDialog.Designer.cs`

#### Steps

- [ ] Build the designer from `IcoPresetDialog`: a `ListBox` labeled "Presets", then Add, Edit, Remove and Close buttons.
- [ ] Each list row reads as the preset's name plus a plain summary, such as "Blog: WEBP, width 300". Put the summary text in a method on `ConversionPreset` or `PresetValidator` and test it.
- [ ] Add and Edit open `PresetDialog`. Remove asks for confirmation through `DialogHelper.Show`. Edit and Remove are disabled with nothing selected, and focus returns to the list predictably after each action.
- [ ] Work on a copy of `Config.Presets`. In `OnFormClosing`, so Esc and Alt+F4 don't lose edits, write it back, call `Config.Save()`, and set a `Changed` property for the caller.
- [ ] validation commands pass

### Task 8: Add the Presets submenu

#### Files

- Create: `src/Sic/Utils/PresetMenuBuilder.cs`
- Create: `tests/Sic.Tests/PresetMenuBuilderTests.cs`
- Modify: `src/Sic/MainWindow.cs`
- Modify: `tests/Sic.Tests/TranslationCatalogTests.cs`

#### Steps

- [ ] Add `PresetMenuBuilder.AddTo(NativeMenuSpec convert, presets, onApply, onAdd, onManage)`, appending a separator and `convert.AddMenu(_("&Presets"), ...)`. It lists each preset through `PresetValidator.EscapeForMenu`, then a separator, `_("&Add Preset...")` and `_("&Manage Presets...")`.
- [ ] Show a disabled "No presets yet" item when the list is empty, so a screen reader user hears why the top of the menu has no presets.
- [ ] Call it at the end of the Convert block in `BuildMenuSpec`. In `ApplyMenuState`, stop disabling the Convert menu as a whole; its children keep their own flags, and Presets stays enabled.
- [ ] Add Preset opens `PresetDialog` pre-filled from `PresetCapturer.Capture`. After OK, append to `Config.Presets`, call `Config.Save()` and rebuild with `_menuBar?.Attach(BuildMenuSpec())` once the dialog has closed. Manage Presets rebuilds the same way when `Changed` is true.
- [ ] tests: a spec with presets named `R&D`, `Blog` and `Banner` passes `MenuSpecValidator.Validate`; an empty list gives one disabled row; 200 presets pass
- [ ] Add the Convert and Presets menu strings to the mnemonic lists in `TranslationCatalogTests`.
- [ ] validation commands pass

### Task 9: Translate all new strings

#### Files

- Modify: `src/Sic/locale/messages.pot`
- Modify: `src/Sic/locale/{de,es,fr,he,ru,uk}/Sic.po`

#### Steps

- [ ] Run `Extract-Strings.ps1`, then `Update-Translations.ps1`.
- [ ] Translate every new string in all six languages, reusing each language's existing word for "preset".
- [ ] Remove every `#, fuzzy` flag on new entries. Window titles must have no `&` and no trailing `...`.
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
- [ ] `CLAUDE.md`: add the new classes and CLI option, and record the gotchas future work needs: the preset pin and `_isApplyingPreset`, why session formats go through `GetDropdownFormats`, and escaping names for the menu.
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

- it is null, or its name fails `ValidateName`
- its name duplicates an earlier entry, ignoring case
- its format isn't in `GetSupportedFormats()` after upper-casing and mapping JPEG to JPG and TIF to TIFF (this excludes HEIC, which is input-only)
- a size is outside 1..`ResizeValidator.MaxDimension`
- its mode is Crop with only one size

Because dropped entries would disappear from disk on the next save, `ConfigSerializer` copies the file to `sic.json.bad` first and tells the user how many were dropped.

Names are at most 64 characters, trimmed, with no control characters, and unique ignoring case.

### ResizeValidator

```csharp
internal enum ResizeError { None, InvalidWidth, InvalidHeight, MissingDimension, CropNeedsWidth, CropNeedsHeight }

internal static class ResizeValidator {
    public const int MaxDimension = 65535;
    public static ResizeError Validate(string widthText, string heightText, ResizeMode mode, out int? width, out int? height);
    public static bool TryParseSpec(string spec, out string widthText, out string heightText);
    public static int ScaleDimension(int target, int sourceFrom, int sourceTo);
}
```

- `Validate` trims both texts. Non-empty text that isn't an integer from 1 to `MaxDimension` is an error.
- In keep mode, at least one size is required. In crop mode, both are.
- `TryParseSpec` keeps the CLI's accepted forms: `WxH`, `Wx`, `xH` and a bare `W`.

### PresetCapturer rules

- Resize off: no sizes.
- Text that isn't a valid size is left out.
- Crop keeps both sizes.
- Keep proportions keeps only the box the user last typed in, because the other was auto-filled for one image and would turn the preset into a fit-inside-a-box. With nothing typed, it keeps both boxes as shown.

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
    _lastTypedBox = DimensionBox.None;
    UpdatePreview();
    statusLabel.Text = _("Preset applied: {0}", preset.Name);
}
```

### CLI precedence

- Format: `--output` extension, then `--format`, then the preset.
- Size and mode with `--resize`: the sizes come from `--resize`, the mode comes only from `--crop`, and the preset's resize settings are ignored entirely.
- Without `--resize`: sizes come from the preset, and the mode is Crop if `--crop` is given, otherwise the preset's mode.
- The existing check that crop needs both sizes runs after this merge.

### Intended behavior changes

- The GUI now reports a mistyped size even when the other size is valid. It used to drop it silently.
- The CLI now rejects sizes above 65535, matching the GUI. It had no upper limit.
- Auto-filled sizes are clamped to 1..65535. They could be 0 or too large before.
- The Convert menu is always enabled, with its items enabled individually. It used to be disabled while the queue was empty, which would have hidden Presets.

### Out of scope

- Exporting and importing presets as separate shareable files. The issue lists it as optional, and it can follow on from the array in `sic.json`.
- Reordering presets by hand, and keyboard shortcuts for individual presets.
- Converting directly from a preset in the GUI. #35 covers one-step conversion through the CLI.

## Post-completion

- Test the whole preset flow by keyboard with NVDA and JAWS: Convert > Presets on an empty queue, Add Preset, the hint, validation messages and focus, Manage Presets, and Remove.
- Confirm a preset's sizes survive adding, selecting and removing images, and that a height-only preset keeps its width box empty.
- Convert a mixed portrait and landscape batch with a width-only preset, and confirm every result is 300 px wide.
- Apply a preset with a format unticked in Settings, and confirm the format stays in the dropdown until SIC! closes.
- Check whether the "Preset applied" status text is heard after the menu closes. If it isn't, decide whether SIC! needs a spoken announcement there; the other status messages have the same limitation.
- Launch once in each language to confirm the menu attaches, and check the dialogs and menu in Hebrew for right-to-left layout.
