# SIC! — Simple Image Converter

An accessible image format converter for Windows. Batch-convert images between JPG, PNG, WebP, ICO, BMP, TIFF, GIF, and AVIF with optional resizing. Works as both a GUI app and a command-line tool.

Built with accessibility in mind — screen-reader friendly with proper labels and tab order — but designed to look and feel like a proper app for sighted users too.

## Features

- **Batch conversion** — queue multiple images and convert them all at once
- **8 formats** — JPG, PNG, WebP, ICO, BMP, TIFF, GIF, AVIF
- **Resize** — specify target dimensions (useful when you're told "upload a 128x128 photo")
- **Multiple input methods** — file dialog, drag & drop, Ctrl+V paste (files or screenshots), download from URL
- **Filename conflict handling** — always asks: overwrite, rename (`_1` suffix), or skip
- **CLI mode** — headless conversion from the command line, no UI needed
- **Accessible** — logical tab order, keyboard shortcuts, screen-reader friendly

## Requirements

- Windows 10/11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

## Installation

Download the latest release, or build from source:

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

1. Add images using the **Add File** button, drag & drop, or **Ctrl+V**
2. Select a target format from the dropdown
3. Optionally check **Resize** and enter target dimensions
4. Click **Convert**

Converted files are saved next to the originals by default. Change this in **Settings**.

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

Settings are stored in `%APPDATA%/Oire/Sic/Sic.cfg`:

- **Output folder** — where converted files are saved (default: same folder as source)
- **Language** — UI language (default: system language)

## Building

Requires [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet build              # Debug build (x64)
dotnet build -c Release   # Release build
dotnet publish -c Release # Single-file executable
```

## Project Structure

```
src/Sic/
  Program.cs                      # Entry point (GUI or CLI dispatch)
  MainWindow.cs/.Designer.cs      # Main application window
  SettingsDialog.cs/.Designer.cs  # Settings form
  AboutDialog.cs/.Designer.cs    # About dialog
  AddUrlDialog.cs/.Designer.cs   # Add image from URL dialog
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
