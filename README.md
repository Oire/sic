# SIC! — Simple Image Converter

An accessible image format converter for Windows. Batch-convert images between JPG, PNG, WebP, ICO, BMP, TIFF, GIF, and AVIF with optional resizing. Works as both a GUI app and a command-line tool.

Built with accessibility in mind — screen-reader friendly with proper labels and tab order — but designed to look and feel like a proper app for sighted users too.

## Features

- **Batch conversion** — queue multiple images and convert them all at once
- **8 formats** — JPG, PNG, WebP, ICO, BMP, TIFF, GIF, AVIF
- **Resize and crop** — specify target dimensions with two modes: keep proportions or crop to exact size
- **Multi-size ICO** — create `.ico` files with multiple embedded sizes using built-in presets or custom dimensions
- **Multiple input methods** — file dialog, folder import, drag & drop, Ctrl+V paste (files, screenshots, or URLs), download by link
- **Filename conflict handling** — always asks: overwrite, rename (`_1` suffix), or skip
- **Cloud file detection** — warns about OneDrive/SharePoint placeholder files that haven't been downloaded yet
- **CLI mode** — headless conversion from the command line, no UI needed
- **Automatic updates** — checks for new versions in the background with Ed25519 signature verification
- **Portable mode** — place an empty `userdata` folder next to `Sic.exe` to keep all data alongside the executable
- **Localized** — English, German, French, Russian, Ukrainian
- **Accessible** — logical tab order, keyboard shortcuts, screen-reader friendly

## Requirements

- Windows 10/11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

## Installation

Download the latest release from [GitHub Releases](https://github.com/Oire/sic/releases) or the official [Oire Software website](https://oire.org/). Alternatively, build from source:

```bash
git clone https://github.com/Oire/sic.git
cd sic
dotnet build
```

For a single-file executable:

```bash
dotnet publish -c Release
```

## Usage

### GUI

Launch the app without arguments to open the graphical interface:

1. Add images using **File > Add Image**, drag & drop, or **Ctrl+V**
2. Select a target format from the dropdown
3. Optionally check **Resize** and enter target dimensions
4. Click **Convert Selected** or **Convert All**

Converted files are saved to `%APPDATA%\Oire\Sic\Converted\` by default (or `userdata\Converted\` in portable mode). Change this in **Settings**.

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | Add image |
| Ctrl+Shift+N | Add folder |
| Ctrl+L | Add image by link |
| Ctrl+V | Paste (files, screenshots, or URLs) |
| Delete | Remove selected image |
| Ctrl+Shift+Delete | Remove all images |
| F5 | Convert selected |
| Ctrl+Shift+F5 | Convert all |
| Ctrl+Alt+F5 | Create multi-size ICO |
| Ctrl+, | Settings |
| F1 | Open user manual |
| Ctrl+Shift+D | Donate |
| Shift+F1 | About |
| Escape | Close the application |

### Command Line

```bash
sic -i input.png -o output.jpg
sic -i photo.bmp -o photo.webp
sic -i avatar.png -o avatar.ico -r 128x128
```

| Option | Short | Required | Description |
|--------|-------|----------|-------------|
| `--input` | `-i` | Yes | Path to the source image |
| `--output` | `-o` | No* | Path for the converted image (format inferred from extension) |
| `--format` | `-f` | No* | Target format (e.g. jpg, png, webp). Used when `--output` is omitted |
| `--resize` | `-r` | No | Target dimensions as WxH, Wx, or xH (e.g. `128x128`, `128x`, `x128`) |
| `--crop` | `-c` | No | Crop mode: scale to cover, then center-crop to exact dimensions |

\* Either `--output` or `--format` must be specified.

## Configuration

Settings are stored in `%APPDATA%\Oire\Sic\Sic.cfg` (or `userdata\Sic.cfg` in portable mode):

- **Output folder** — where converted files are saved (default: `Converted` subfolder in the data directory)
- **Language** — UI language (default: system language)
- **Confirm exit** — warn when closing with images still in the queue (default: enabled)

### Portable Mode

To run SIC! in portable mode, create an empty `userdata` folder next to `Sic.exe`. When this folder is present, all application data — configuration, converted images, and logs — is stored there instead of in `%APPDATA%`. This is useful for running from a USB drive or keeping everything self-contained.

## Building

Requires [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet build              # Debug build (x64)
dotnet build -c Release   # Release build
dotnet publish -c Release # Single-file executable
```

## Building the Installer

The `installer/` directory contains an [Inno Setup](https://jrsoftware.org/isdl.php) script that builds a Windows installer with automatic .NET 8 Desktop Runtime detection and installation.

### Quick start

Double-click `installer/build-installer.bat` — it builds the app in Release mode and compiles the installer.

### PowerShell options

```powershell
.\installer\build-installer.ps1                  # Build app + installer
.\installer\build-installer.ps1 -SkipBuild       # Installer only (use existing binaries)
.\installer\build-installer.ps1 -OpenOutput      # Open output folder when done
.\installer\build-installer.ps1 -InnoSetupPath "C:\Path\To\ISCC.exe"  # Custom compiler path
```

The installer is created in `installer/Output/` as `SIC!-V{version}-Setup.exe`.

### What the installer includes

- `Sic.exe` (single-file executable)
- `Magick.Native-Q16-x64.dll`
- `help/` (user manual, per language)
- `locale/` (translation `.mo` files)

User data (`%APPDATA%\Oire\Sic`) is not bundled — it is created at runtime. On uninstall, the user is offered the option to remove it.

## Project Structure

```
src/Sic/
  Program.cs                      # Entry point (GUI or CLI dispatch)
  MainWindow.cs/.Designer.cs      # Main application window
  SettingsDialog.cs/.Designer.cs  # Settings form
  AboutDialog.cs/.Designer.cs    # About dialog
  AddUrlDialog.cs/.Designer.cs   # Add image by link dialog
  AddFolderDialog.cs/.Designer.cs # Add images from folder dialog
  IcoPresetDialog.cs/.Designer.cs # Multi-size ICO preset picker
  AddSizeDialog.cs/.Designer.cs   # Custom ICO size entry dialog
  ProgressDialog.cs/.Designer.cs  # Progress dialog for batch operations
  Models/
    ImageItem.cs                  # Image queue item data model
    ResizeMode.cs                 # Resize mode enum (KeepProportions, Crop)
  Services/
    ImageConverter.cs             # Magick.NET conversion engine
  Utils/
    Config.cs                     # SharpConfig-based settings
    FileHelper.cs                 # Cloud placeholder detection, file enumeration
    Localization.cs               # GetText.NET wrapper
    Constants/
      App.cs                      # App metadata and paths
      ExitCode.cs                 # CLI exit code constants
      Logging.cs                  # Log file paths and templates
```

## License

Copyright 2026 Oire Software SARL. Licensed under the [Apache License 2.0](LICENSE).
