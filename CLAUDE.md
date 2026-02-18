# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SIC! (Simple Image Converter) is a Windows Forms desktop application (.NET 8.0, x64-only) by Oire Software. It converts images between formats (JPG, PNG, WEBP, ICO, BMP, TIFF, GIF, AVIF) with optional resizing, using Magick.NET as the image processing backend. The app has both a GUI mode and a headless CLI mode.

It uses structured logging (Serilog), file-based configuration (SharpConfig), and gettext-based localization (GetText.NET). Versioning is handled automatically via Nerdbank.GitVersioning from git history.

## Build Commands

```bash
dotnet build              # Build the solution (Debug|x64 by default)
dotnet build -c Release   # Release build
dotnet publish -c Release # Publish as single-file executable
```

There are no test projects or linting commands configured yet.

## Architecture

**Entry point:** `src/Sic/Program.cs` — Sets up Serilog logging. If CLI arguments are present, runs headless conversion via `System.CommandLine`; otherwise loads config and launches the WinForms `MainWindow`.

### Core modules

**`src/Sic/Models/ImageItem.cs`** — Data model for an image in the conversion queue. Properties: `FilePath`, `ImageData` (for clipboard/URL sources), `OriginalFormat`, `FileName`, `Width`, `Height`, `FileSize`. Display helpers for dimensions and human-readable file sizes.

**`src/Sic/Services/ImageConverter.cs`** — Static conversion engine wrapping Magick.NET. Key methods:
- `LoadFromFile`, `LoadFromStream`, `LoadFromBytes`, `LoadFromUrl` — create `ImageItem` from various sources
- `Convert` — converts an `ImageItem` to a target format with optional resize, writes to disk
- `GeneratePreview` — produces a `Bitmap` for the preview panel
- `GenerateOutputPath`, `GetConflictRenamePath` — output path logic with conflict resolution
- Note: The class name conflicts with `System.Drawing.ImageConverter`; files that use it must import `using ImageConverter = Oire.Sic.Services.ImageConverter;`

### UI

**`src/Sic/MainWindow.cs` + `MainWindow.Designer.cs`** — Main application window. `TableLayoutPanel`-based layout with:
- `ListView` (batch queue) + `PictureBox` (image preview) in the top row
- Format dropdown, resize checkbox + width/height fields in the controls row
- Action buttons (Add File, Add URL, Remove, Settings, Convert) in the button row
- `StatusStrip` with progress bar at the bottom
- Supports drag & drop, Ctrl+V paste (file drops and bitmap clipboard), Delete key to remove

**`src/Sic/SettingsDialog.cs` + `SettingsDialog.Designer.cs`** — Settings form. Flat `TableLayoutPanel` layout with output folder (textbox + browse + clear), language dropdown, OK/Cancel.

### Utilities (`src/Sic/Utils/`)

- `Config.cs` — Static config manager using SharpConfig. Reads/writes `%APPDATA%/Oire/Sic/Sic.cfg` with a `[General]` section (Language, BrailleUiMode, OutputFolder). Empty OutputFolder = same folder as source.
- `Localization.cs` — Wraps GetText.NET with convenience methods: `_()`, `_n()`, `_p()`, `_pn()` for translations. Loads `.mo` files from `./locale/`. Falls back through language parents to `en-US`.
- `Constants/App.cs` — Application metadata, data folder paths (`%APPDATA%/Oire/Sic/`), file extensions.
- `Constants/Logging.cs` — Log file paths and output templates. Logs go to `%APPDATA%/Oire/Sic/logs/`.

### Key dependencies

| Package | Purpose |
|---------|---------|
| Magick.NET-Q16-x64 | Image loading, conversion, resizing (16-bit per channel) |
| System.CommandLine | CLI argument parsing |
| Serilog (+File, +Compact) | Structured logging |
| SharpConfig | INI-style config file |
| GetText.NET | Localization |
| Nerdbank.GitVersioning | Automatic versioning from git |

## CLI Mode

When launched with arguments, the app runs headless (no UI). Format is inferred from the output file extension.

```bash
sic -i input.png -o output.jpg                    # Convert PNG to JPG
sic --input photo.bmp --output photo.webp          # Convert BMP to WebP
sic -i avatar.png -o avatar.ico --resize 128x128   # Convert + resize
```

Options: `--input`/`-i` (required), `--output`/`-o` (required), `--resize`/`-r` (optional, WxH format).

## Product Spec (v1)

SIC! is an accessible image format converter primarily aimed at blind and low-confidence users, but designed to be visually appealing enough for sighted users too.

### Core workflow

1. **Main window** = a list of images (batch queue). Empty on launch.
2. **Adding images** via:
   - Drag & drop files onto the list
   - Ctrl+V paste (both clipboard files like Outlook attachments, and raw bitmap data like PrintScreen screenshots)
   - "Open file" button/dialog
   - Open by URL (downloads and converts in one step, no original kept)
3. **Pick target format** from a dropdown (JPG, PNG, WEBP, ICO, BMP, TIFF, GIF, AVIF).
4. **Optional resize** — checkbox that reveals width/height fields (critical for blind users who get told "upload a 128x128 photo").
5. **Hit Convert** — processes all items in the list to the chosen format.
6. **Output location** — by default, same folder as original, same name with new extension. Configurable in settings to use a custom output folder.
7. **Filename conflict** — always ask (overwrite / rename to `_1` suffix / skip). Never silently overwrite.

### UI guidelines

- **Use `TableLayoutPanel` throughout** — the developer is blind and needs to adjust layouts without counting pixel coordinates. Use flat layouts (no nesting) with percentage-based column styles where possible.
- **Image preview panel** — sighted users should see a preview of the selected image. This is not a blind-only tool; it should look and feel like a proper app a sighted person would also choose to use.
- **Resize controls** — consider visual resize handles or interactive controls for sighted users in addition to the WxH text fields.
- **Accessibility first** — proper tab order, meaningful labels. Do **not** set `AccessibleName` on controls — it overrides the `Text` property that screen readers already use, causing JAWS to read verbose descriptions instead of the actual button/menu text.

### Settings (via SharpConfig, stored in `%APPDATA%/Oire/Sic/Sic.cfg`)

- Output folder (default: same as source)
- Language

## Code Conventions

- **Namespaces:** `Oire.Sic`, `Oire.Sic.Models`, `Oire.Sic.Services`, `Oire.Sic.Utils`, `Oire.Sic.Utils.Constants`
- **Nullable reference types** are enabled; **all warnings are treated as errors** — code must compile cleanly
- **Implicit usings** are enabled (no explicit `using System;` etc.)
- **.NET analyzers** at latest analysis level are enforced
- Debug builds include full symbols and extra telemetry logging; Release builds are optimized with no symbols
- Locale `.mo` files under `locale/` are copied to output via `<None Update>` in the csproj
- The project uses a code formatter/linter — expect Allman brace style in committed code
