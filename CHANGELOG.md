# Changelog

All notable changes to SIC! (Simple Image Converter) will be documented in this file.

## [1.2.0.0] — unreleased

### Changes

- **Native menus and image list**: the menu bar and the image list are now real Windows controls rather than WinForms-drawn imitations, which is what screen readers need from them ([#68](https://github.com/Oire/sic/issues/68)). The menu is a genuine `HMENU` attached with `SetMenu`, announced as a menu bar with real submenus instead of a panel that draws things resembling menu items. The image list is a genuine `SysListView32` rather than one wearing a window class no screen reader recognizes, so every column is read instead of only the file name. Nothing changes about how either looks or behaves for sighted users.

### Other

- SIC! can now be installed and updated through WinGet.

## [1.1.0.22] — 2026-08-21

### Features

- **Fit to file size**: give SIC! a maximum file size (and optionally a maximum width) and it searches every enabled format for combinations of format, dimensions, and quality that fit under it, then converts to the result you pick ([#24](https://github.com/Oire/sic/issues/24)). Available via **Convert > Fit to File Size** (<kbd>Ctrl+Alt+Shift+F5</kbd>). Every size offered is a real measurement, the results are ranked resolution first, and nothing is written to disk until you choose one. ICO and GIF are excluded from the search; they remain ordinary conversion targets.
- **HEIC/HEIF input**: photos from newer iPhones can now be opened and converted to any supported format. Input only — HEIC can be read but not written ([#30](https://github.com/Oire/sic/issues/30))
- **Save converted images in the same folder as the original**: a new Settings option that writes each converted file next to its source file instead of into the output folder. Clipboard captures and downloaded links still go to the output folder ([#33](https://github.com/Oire/sic/issues/33))
- **Detect data in clipboard**: SIC! can now offer to add an image, image files, or an image link from the clipboard when its window opens or regains focus ([#36](https://github.com/Oire/sic/issues/36)). It is off by default and can be toggled in Settings; the same clipboard content is offered only once.
- **Adjustable update checks**: the startup check can be turned off, and the background check frequency can be set to daily, every 3 days, weekly, monthly, or never. A change takes effect immediately, without restarting ([#45](https://github.com/Oire/sic/issues/45))
- **Customizable target formats**: choose which formats appear in the target-format dropdown and hide the ones you never convert to ([#47](https://github.com/Oire/sic/issues/47))
- **Spanish and Hebrew interfaces**: the UI, the user manual and the installer are now translated into Spanish and Hebrew, bringing SIC! to seven languages. Hebrew runs the entire interface right-to-left ([#66](https://github.com/Oire/sic/pull/66))

### Changes

- The Settings dialog is now organized into **General** and **Images** tabs.
- Converting an image to the format it is already in is now skipped (reported as *Skipped (same format)*) instead of re-encoding and degrading it for nothing. When a resize does force a re-encode, the original encoding quality is reapplied ([#44](https://github.com/Oire/sic/pull/44))
- Adding a link (by paste or **Add by link**) that doesn't point to a supported image now shows a clear message instead of a raw decoder error.

### Bug Fixes

- Dialog window titles (Settings, About, Add Folder, Add Image by Link, Create Multi-size ICO, Add Size, Fit to File Size) are now localized. They had always been left in English: the string extractor only picks up qualified `control.Text` assignments, so a form's own title never reached the translation catalog.

### Other

- Updated the target framework to .NET 10.0 ([#43](https://github.com/Oire/sic/pull/43))

## [1.0.2.2] — 2026-04-17

### Bug Fixes

- Fix app crash when converting or deleting the last image in the queue ([#41](https://github.com/Oire/sic/pull/41))
- Screen readers now announce the empty-list state after the list is cleared, and when the list is focused at startup ([#41](https://github.com/Oire/sic/pull/41))
- Log unhandled UI thread exceptions and show a non-fatal dialog instead of crashing ([#41](https://github.com/Oire/sic/pull/41))

### Other

- Mention WinGet installation and update official website link to sic.oire.dev in the user manual
- Bump Microsoft.CodeAnalysis.NetAnalyzers to 10.0.202 and System.CommandLine to 2.0.6 ([#38](https://github.com/Oire/sic/pull/38))

## [1.0.1.6] — 2026-03-15

### Bug Fixes

- Fix NVDA not announcing empty images list on focus ([#31](https://github.com/Oire/sic/pull/31)) by [Quinn Gillespie](https://github.com/trypsynth)

## [1.0.0.23] — 2026-03-08

### Initial Release

SIC! is an accessible image format converter for Windows with both a GUI and a CLI mode.

### Features

- **Batch image conversion** between 8 formats: JPG, PNG, WebP, ICO, BMP, TIFF, GIF, AVIF
- **Multiple input methods:**
  - Open files via file dialog (Ctrl+N)
  - Add entire folders with optional subfolder scanning (Ctrl+Shift+N)
  - Drag and drop files onto the queue
  - Paste from clipboard (Ctrl+V) — supports file drops, bitmap screenshots, and URLs
  - Download images by URL (Ctrl+L)
- **Resize and crop** — optional target dimensions with two modes: keep proportions or crop to exact size
- **Multi-size ICO creation** — combine multiple sizes into a single `.ico` file using presets (Favicon, App Icon) or custom sizes
- **Smart output path handling:**
  - Default output folder: `%APPDATA%\Oire\Sic\Converted\`
  - Configurable custom output folder in Settings
  - Preserves subfolder structure when adding from folders
- **Filename conflict resolution** — always prompts to overwrite, rename (with `_1` suffix), or skip
- **Cloud file detection** — warns about OneDrive/SharePoint placeholder files that haven't been downloaded yet
- **Portable mode** — place an empty `userdata` folder next to `Sic.exe` to store all data (config, converted images, logs) alongside the executable instead of in `%APPDATA%`
- **Automatic updates** via NetSparkleUpdater with Ed25519 signature verification
- **Image preview panel** for sighted users
- **Progress dialog** with cancel support for batch operations
- **CLI mode** — headless conversion with `--input`, `--output`/`--format`, `--resize`, and `--crop` options
- **Localization** — English, German, French, Russian, and Ukrainian
- **Accessibility** — logical tab order, keyboard accelerators on all controls, screen-reader friendly labels
- **Confirm on exit** when images are still in the queue (configurable)
- **User manual** accessible via F1, localized per language
- **About dialog** with system info copy for bug reports
