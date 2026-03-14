# Changelog

All notable changes to SIC! (Simple Image Converter) will be documented in this file.

## [1.0.1] — 2026-03-15

### Bug Fixes

- Fix NVDA not announcing empty images list on focus ([#31](https://github.com/Oire/sic/pull/31)) by [Quinn Gillespie](https://github.com/trypsynth)

## [1.0.0] — 2026-03-08

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
