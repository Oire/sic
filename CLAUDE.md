# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SIC! (Simple Image Convertor) is a Windows Forms desktop application (.NET 8.0, x64-only) by Oire Software. It uses structured logging (Serilog), file-based configuration (SharpConfig), and gettext-based localization (GetText.NET). Versioning is handled automatically via Nerdbank.GitVersioning from git history.

## Build Commands

```bash
dotnet build              # Build the solution (Debug|x64 by default)
dotnet build -c Release   # Release build
dotnet publish -c Release # Publish as single-file executable
```

There are no test projects or linting commands configured yet.

## Architecture

**Entry point:** `src/Sic/Program.cs` — Sets up Serilog logging, loads config, then launches the WinForms `MainWindow`.

**Key modules under `src/Sic/Utils/`:**
- `Config.cs` — Static config manager using SharpConfig. Reads/writes `%APPDATA%/Oire/SIC!/SIC!.cfg` with a `[General]` section (language, braille UI mode).
- `Localization.cs` — Wraps GetText.NET with convenience methods: `_()`, `_n()`, `_p()`, `_pn()` for translations. Loads `.mo` files from `./locale/`. Falls back through language parents to `en-US`.
- `Constants/App.cs` — Application metadata, data folder paths (`%APPDATA%/Oire/SIC!/`), file extensions.
- `Constants/Logging.cs` — Log file paths and output templates. Logs go to `%APPDATA%/Oire/SIC!/logs/`.

**UI:** `MainWindow.cs` + `MainWindow.Designer.cs` — Standard WinForms partial class pattern.

## Product Spec (v1)

SIC! is an accessible image format converter primarily aimed at blind and low-confidence users, but designed to be visually appealing enough for sighted users too.

### Core workflow

1. **Main window** = a list of images (batch queue). Empty on launch.
2. **Adding images** via:
   - Drag & drop files onto the list
   - Ctrl+V paste (both clipboard files like Outlook attachments, and raw bitmap data like PrintScreen screenshots)
   - "Open file" button/dialog
   - Open by URL (downloads and converts in one step, no original kept)
3. **Pick target format** from a dropdown (JPG, PNG, WEBP, ICO, BMP, TIFF, GIF, AVIF — the more the better).
4. **Optional resize** — checkbox that reveals width/height fields (critical for blind users who get told "upload a 128x128 photo").
5. **Hit Convert** — processes all items in the list to the chosen format.
6. **Output location** — by default, same folder as original, same name with new extension. Configurable in settings to use a custom output folder.
7. **Filename conflict** — always ask (overwrite / rename to `_1` suffix / skip). Never silently overwrite.

### CLI mode

The app detects whether it was launched with arguments. If yes, run headless; if no, show the WinForms UI. Uses `System.CommandLine`. Example: `sic --from C:\path\pussycat.png --to jpg --resize 128x128`.

### UI guidelines

- **Use `TableLayoutPanel` throughout** — the developer is blind and needs to adjust layouts without counting pixel coordinates.
- **Image preview panel** — sighted users should see a preview of the selected image. This is not a blind-only tool; it should look and feel like a proper app a sighted person would also choose to use.
- **Resize controls** — consider visual resize handles or interactive controls for sighted users in addition to the WxH text fields.
- **Accessibility first** — all controls must be screen-reader friendly, proper tab order, meaningful labels.

### Settings (via SharpConfig, stored in `%APPDATA%/Oire/SIC!/SIC!.cfg`)

- Output folder (default: same as source)
- Language
- Braille UI mode

## Code Conventions

- **Namespace:** `Oire.Sic`, `Oire.Sic.Utils`, `Oire.Sic.Utils.Constants`
- **Nullable reference types** are enabled; **all warnings are treated as errors** — code must compile cleanly
- **Implicit usings** are enabled (no explicit `using System;` etc.)
- **.NET analyzers** at latest analysis level are enforced
- Debug builds include full symbols and extra telemetry logging; Release builds are optimized with no symbols
- Locale `.mo` files under `locale/` are copied to output via `<None Update>` in the csproj
