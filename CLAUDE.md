# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SIC! (Simple Image Converter) is a Windows Forms desktop application (.NET 8.0, x64-only) by Oire Software. It converts images between formats (JPG, PNG, WEBP, ICO, BMP, TIFF, GIF, AVIF) with optional resizing, using Magick.NET as the image processing backend. The app has both a GUI mode and a headless CLI mode.

It uses structured logging (Serilog), file-based configuration (SharpConfig), and gettext-based localization (GetText.NET). Versioning is handled automatically via GitVersion (GitVersion.MsBuild) from git history.

## Build Commands

```bash
dotnet build              # Build the solution (Debug|x64 by default)
dotnet build -c Release   # Release build
dotnet publish -c Release # Publish as single-file executable
```

There are no test projects or linting commands configured yet.

### Localization scripts

Translation scripts live in `src/Sic/locale/scripts/`. Run them with PowerShell:

```bash
# Compile .po files to .mo (required after editing translations)
powershell -ExecutionPolicy Bypass -File src/Sic/locale/scripts/compile-translations.ps1

# Extract new translatable strings from source into messages.pot
powershell -ExecutionPolicy Bypass -File src/Sic/locale/scripts/Extract-Strings.ps1

# Merge new strings into existing .po files (preserves existing translations)
powershell -ExecutionPolicy Bypass -File src/Sic/locale/scripts/Update-Translations.ps1
```

After editing `.po` files, always run `compile-translations.ps1` to regenerate the `.mo` binaries.

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

**`src/Sic/MainWindow.cs` + `MainWindow.Designer.cs`** — Main application window. Menu bar (File, Edit, Convert, Help) + `TableLayoutPanel`-based layout with:
- `ListView` (batch queue) + `PictureBox` (image preview) in the top row
- Format dropdown, resize checkbox + resize mode / width / height fields in the controls rows
- Convert Selected + Convert All buttons
- `StatusStrip` with status label at the bottom
- Batch operations show a separate `ProgressDialog` with progress bar
- Supports drag & drop, Ctrl+V paste (file drops and bitmap clipboard), Delete key to remove

**`src/Sic/SettingsDialog.cs` + `SettingsDialog.Designer.cs`** — Settings form. Flat `TableLayoutPanel` layout with output folder (textbox + browse + clear), language dropdown, OK/Cancel.

### Utilities (`src/Sic/Utils/`)

- `Config.cs` — Static config manager using SharpConfig. Reads/writes `%APPDATA%/Oire/Sic/Sic.cfg` with a `[General]` section (Language, OutputFolder, LastInputFolder). Accepts `isGui` parameter to route errors to MessageBox (GUI) or stderr (CLI).
- `FileHelper.cs` — Cloud placeholder detection (OneDrive/SharePoint recall attributes) and image file enumeration with glob patterns.
- `Localization.cs` — Wraps GetText.NET with convenience methods: `_()`, `_n()`, `_p()`, `_pn()` for translations. Loads `.mo` files from the `locale/` folder relative to the executable. Falls back through language parents to `en-US`.
- `Constants/App.cs` — Application metadata, data folder paths (`%APPDATA%/Oire/Sic/`), file extensions.
- `Constants/ExitCode.cs` — CLI exit code constants (`Success`, `Error`, `Canceled`).
- `Constants/Logging.cs` — Log file paths and output templates. Logs go to `%APPDATA%/Oire/Sic/logs/`.

### Key dependencies

| Package | Purpose |
|---------|---------|
| Magick.NET-Q16-x64 | Image loading, conversion, resizing (16-bit per channel) |
| System.CommandLine | CLI argument parsing |
| Serilog (+File, +Compact) | Structured logging |
| SharpConfig | INI-style config file |
| GetText.NET | Localization |
| GitVersion.MsBuild | Automatic versioning from git |

## CLI Mode

When launched with arguments, the app runs headless (no UI). Format is inferred from the output file extension.

```bash
sic -i input.png -o output.jpg                    # Convert PNG to JPG
sic --input photo.bmp --output photo.webp          # Convert BMP to WebP
sic -i avatar.png -o avatar.ico --resize 128x128   # Convert + resize
```

Options: `--input`/`-i` (required), `--output`/`-o` or `--format`/`-f` (one required), `--resize`/`-r` (optional, WxH/Wx/xH format), `--crop`/`-c` (optional, requires both W and H).

## Product Spec (v1)

SIC! is an accessible image format converter primarily aimed at blind and low-confidence users, but designed to be visually appealing enough for sighted users too.

### Core workflow

1. **Main window** = a list of images (batch queue). Empty on launch.
2. **Adding images** via:
   - Drag & drop files onto the list
   - Ctrl+V paste (both clipboard files like Outlook attachments, and raw bitmap data like PrintScreen screenshots)
   - "Open file" button/dialog
   - Add by link (downloads the image and adds it to the queue)
3. **Pick target format** from a dropdown (JPG, PNG, WEBP, ICO, BMP, TIFF, GIF, AVIF).
4. **Optional resize** — checkbox that reveals width/height fields (critical for blind users who get told "upload a 128x128 photo").
5. **Hit Convert** — processes all items in the list to the chosen format.
6. **Output location** — by default, `%APPDATA%\Oire\Sic\Converted\` (or `userdata\Converted\` in portable mode), same name with new extension. Configurable in settings to use a custom output folder.
7. **Filename conflict** — always ask (overwrite / rename to `_1` suffix / skip). Never silently overwrite.

### UI guidelines

- **Use `TableLayoutPanel` throughout** — the developer is blind and needs to adjust layouts without counting pixel coordinates. Use flat layouts (no nesting) with percentage-based column styles where possible.
- **Image preview panel** — sighted users should see a preview of the selected image. This is not a blind-only tool; it should look and feel like a proper app a sighted person would also choose to use.
- **Resize controls** — consider visual resize handles or interactive controls for sighted users in addition to the WxH text fields.
- **Accessibility first** — proper tab order, meaningful labels, keyboard accelerators (underlined letters via `&`) on all interactive controls. Do **not** set `AccessibleName` on controls that already have a meaningful `Text` property — it overrides what screen readers use. Only set `AccessibleName` on controls without descriptive text (e.g., `ListView`).

### Settings (via SharpConfig, stored in `%APPDATA%/Oire/Sic/Sic.cfg`)

- Output folder (default: `Converted` subfolder in the data directory)
- Language
- Confirm exit when images are in the queue

## Auto-Updates (NetSparkleUpdater)

The app uses [NetSparkleUpdater](https://github.com/NetSparkleUpdater/NetSparkle) with Ed25519 signature verification for automatic updates.

### Key setup

Updates are signed with an Ed25519 key pair. The public key is embedded in `src/Sic/Utils/Constants/App.cs` (`UpdatePublicKey`). The private key is **not** checked into the repo — it lives in a `keys/` directory at the repo root (gitignored).

To generate a new key pair (only needed once, or if forking the project):

```bash
# Install the appcast tool globally
dotnet tool install --global NetSparkleUpdater.Tools.AppCastGenerator

# Generate keys into the keys/ directory
netsparkle-generate-appcast --generate-keys --key-path keys
```

This creates `keys/NetSparkle_Ed25519.priv` and `keys/NetSparkle_Ed25519.pub`. After generating, update the `UpdatePublicKey` constant in `App.cs` with the contents of the `.pub` file, and update `AppcastUrl` if hosting elsewhere.

### Generating an appcast

After building the installer (`installer/build-installer.ps1`), generate the signed appcast:

```powershell
.\installer\generate-appcast.ps1                           # Uses keys/ directory by default
.\installer\generate-appcast.ps1 -KeyPath "C:\path\to\keys" # Custom key location
.\installer\generate-appcast.ps1 -ChangeLog "changelogs"    # Include release notes
```

The script produces `appcast.xml` and `appcast.xml.signature` in `installer/Output/`. Upload both along with the installer to the URL specified in `App.AppcastUrl`.

### Release notes

Per-version changelogs go in a `changelogs/` directory at the repo root (e.g., `changelogs/1.0.0.md`). The appcast generator embeds them in the appcast XML.

## Code Conventions

- **Namespaces:** `Oire.Sic`, `Oire.Sic.Models`, `Oire.Sic.Services`, `Oire.Sic.Utils`, `Oire.Sic.Utils.Constants`
- **Nullable reference types** are enabled; **all warnings are treated as errors** — code must compile cleanly
- **Implicit usings** are enabled (no explicit `using System;` etc.)
- **.NET analyzers** at latest analysis level are enforced
- Debug builds include full symbols and extra telemetry logging; Release builds are optimized with no symbols
- Locale `.mo` files under `locale/` are copied to output via `<None Update>` in the csproj
- The project uses a code formatter/linter — expect same-line opening braces (no newline before `{`, `else`, `catch`, `finally`) in committed code
