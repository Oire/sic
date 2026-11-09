# SIC! — Simple Image Converter

An accessible image format converter for Windows. Batch-convert images between JPG, PNG, WebP, ICO, BMP, TIFF, GIF, and AVIF (plus reading HEIC photos from iPhones) with optional resizing. Works as both a GUI app and a command-line tool.

Built with accessibility in mind — screen-reader friendly with proper labels and tab order — but designed to look and feel like a proper app for sighted users too.

## Features

- **Batch conversion** — queue multiple images and convert them all at once
- **8 output formats** — JPG, PNG, WebP, ICO, BMP, TIFF, GIF, AVIF
- **HEIC input** — open and convert HEIC/HEIF photos from newer iPhones to any supported format (input only; HEIC can't be written)
- **Resize and crop** — specify target dimensions with two modes: keep proportions or crop to exact size
- **Multi-size ICO** — create `.ico` files with multiple embedded sizes using built-in presets or custom dimensions
- **Conversion presets** — save a target format and resize settings under a name, such as "Blog" for WebP images 300 px wide, and pick it from **Convert > Presets** or pass it to the CLI with `--preset`
- **Fit to file size** — give a maximum file size (and optionally a maximum width) and SIC! searches every enabled format for a combination of format, dimensions, and quality that fits, then converts to the result you pick
- **Multiple input methods** — file dialog, folder import, drag & drop, Ctrl+V paste (files, screenshots, or URLs), download by link
- **Clipboard detection** — optionally offers to add an image, image files, or an image link from the clipboard when the window opens or gains focus (opt-in)
- **Filename conflict handling** — always asks: overwrite, rename (`_1` suffix), or skip
- **Configurable output location** — a custom output folder, or save each converted file next to its original
- **Selectable target formats** — hide the formats you never convert to from the target-format dropdown
- **Cloud file detection** — warns about OneDrive/SharePoint placeholder files that haven't been downloaded yet
- **CLI mode** — headless conversion from the command line, no UI needed
- **Automatic updates** — checks for new versions in the background with Ed25519 signature verification, with adjustable frequency (or off) in Settings
- **Portable mode** — place an empty `userdata` folder next to `Sic.exe` to keep all data alongside the executable
- **Localized** — English, German, Spanish, French, Hebrew, Russian, Ukrainian (Hebrew runs the whole UI right-to-left)
- **Accessible** — logical tab order, keyboard shortcuts, screen-reader friendly

## Requirements

- Windows 10/11 (x64)
- [.NET 10.0 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

## Installation

Download the latest release from [GitHub Releases](https://github.com/Oire/sic/releases) or the official [SIC! website](https://sic.oire.dev/).

Starting with version 1.2, SIC! is signed by Oire Software: the installer and `sic.exe` carry its digital signature, and Windows names Oire Software as the verified publisher when you install. For a while after a new release, Windows SmartScreen may still say "Windows protected your PC"; choose **More info**, check that the publisher is Oire Software, then choose **Run anyway**.

You can also install via [winget](https://learn.microsoft.com/windows/package-manager/winget/):

```bash
winget install Oire.Sic
```

Alternatively, build from source:

```bash
git clone https://github.com/Oire/sic.git
cd sic
dotnet build
dotnet test
```

For a single-file executable:

```bash
dotnet publish src/Sic/Sic.csproj -c Release
```

## Usage

### GUI

Launch the app without arguments to open the graphical interface:

1. Add images using **File > Add Image**, drag & drop, or **Ctrl+V**
2. Select a target format from the dropdown
3. Optionally check **Resize** and enter target dimensions
4. Click **Convert Selected** or **Convert All**

To reuse settings, save them as a preset with **Convert > Presets > Add Preset**. Choosing a preset from **Convert > Presets** fills in the format and resize controls without converting, so you can check them before you convert. **Manage Presets** edits and removes them.

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
| Ctrl+Alt+Shift+F5 | Fit to file size |
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
sic -i photo.png --preset Blog
sic -i photo.png -p "Product banner" -o banner.jpg
```

| Option | Short | Required | Description |
|--------|-------|----------|-------------|
| `--input` | `-i` | Yes | Path to the source image |
| `--output` | `-o` | No* | Path for the converted image (format inferred from extension) |
| `--format` | `-f` | No* | Target format (e.g. jpg, png, webp). Used when `--output` is omitted |
| `--resize` | `-r` | No | Target dimensions as WxH, Wx, or xH (e.g. `128x128`, `128x`, `x128`) |
| `--crop` | `-c` | No | Crop mode: scale to cover, then center-crop to exact dimensions |
| `--preset` | `-p` | No* | Name of a saved preset (case doesn't matter) whose format and resize settings to use |

\* At least one of `--output`, `--format` or `--preset` must be specified. The format comes from the `--output` extension first, then `--format`, then the preset. `--resize` replaces the preset's sizes and mode, and `--crop` alone crops to the preset's sizes. An unknown preset name fails with the list of saved presets on stderr. Sizes go from 1 to 65535.

## Configuration

Settings are stored as JSON in `%APPDATA%\Oire\Sic\sic.json` (or `userdata\sic.json` in portable mode). A `Sic.cfg` from SIC! 1.1 or earlier is migrated automatically on first start and kept as `Sic.cfg.bak`. The settings are:

- **Output folder** — where converted files are saved (default: `Converted` subfolder in the data directory)
- **Save converted images in the same folder as the original** — write each converted file next to its source file instead of into the output folder; clipboard captures and downloaded links still go to the output folder (default: off)
- **Language** — UI language (default: system language)
- **Confirm exit** — warn when closing with images still in the queue (default: enabled)
- **Check for updates on startup** — perform a single silent update check shortly after launch (default: enabled)
- **Check for updates in the background** — how often to check for updates while running: once a day, every 3 days, once a week, once a month, or never (default: once a day)
- **Detect images in clipboard** — offer to add an image, image files, or an image link from the clipboard when the window opens or gains focus (default: off)
- **Target formats to show in the list** — which formats appear in the target-format dropdown (default: all)

The same file holds your conversion presets, in a `presets` array beside the settings. A preset that can't be used, for instance after a hand edit, is removed on its own; the file is backed up as `sic.json.bad` first and SIC! says how many were removed.

### Portable Mode

To run SIC! in portable mode, create an empty `userdata` folder next to `Sic.exe`. When this folder is present, all application data — configuration, converted images, and logs — is stored there instead of in `%APPDATA%`. This is useful for running from a USB drive or keeping everything self-contained.

### One window per data folder

Only one SIC! window runs per data folder: starting SIC! again brings the open window forward, along with any dialog open in it, instead of opening a second one. If Windows doesn't let it come forward, SIC! says it's already running, so a second launch never ends in silence. Copies with their own data folder, such as two portable copies or a portable and an installed copy, run side by side, and the command line always runs, even while the window is open.

## Building

Requires [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet build                                   # Debug build (x64)
dotnet build -c Release                        # Release build
dotnet test                                    # Run the tests
dotnet publish src/Sic/Sic.csproj -c Release   # Single-file executable
```

## Building the Installer

The `installer/` directory contains an [Inno Setup](https://jrsoftware.org/isdl.php) script that builds a Windows installer with automatic .NET 8 Desktop Runtime detection and installation.

### Quick start

Double-click `installer/build-installer.bat` — it builds the app in Release mode and compiles the installer.

### PowerShell options

```powershell
.\installer\Build-Installer.ps1                        # Build app + installer + portable ZIP
.\installer\Build-Installer.ps1 -Appcast               # Also generate signed appcast
.\installer\Build-Installer.ps1 -Appcast -Deploy       # Build everything and deploy via SCP
.\installer\Build-Installer.ps1 -NoPortable            # Skip portable ZIP (e.g., for beta releases)
.\installer\Build-Installer.ps1 -SkipBuild             # Installer only (use existing binaries)
.\installer\Build-Installer.ps1 -OpenOutput            # Open output folder when done
.\installer\Build-Installer.ps1 -InnoSetupPath "C:\Path\To\ISCC.exe"  # Custom Inno Setup path
```

Output files are created in `installer/Output/`:

- `sic-v{version}-setup.exe` — Windows installer
- `sic-v{version}-portable.zip` — portable archive with `userdata/` folder (unless `-NoPortable`)
- `appcast.xml` and `appcast.xml.signature` — auto-update manifest (if `-Appcast`)

The portable ZIP is compressed with [7-Zip](https://www.7-zip.org/) if available (auto-detected), otherwise falls back to built-in compression with a warning.

### Deploying

The `-Deploy` switch uploads release files to the hosting server via SCP. It reads connection details from `installer/deploy.json` (gitignored). Copy `installer/deploy.example.json` and fill in your SSH host alias and remote path to get started.

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
  PresetDialog.cs/.Designer.cs    # Add or edit a conversion preset
  ManagePresetsDialog.cs/.Designer.cs # List, add, edit and remove conversion presets
  AddSizeDialog.cs/.Designer.cs   # Custom ICO size entry dialog
  FitToSizeDialog.cs/.Designer.cs # Fit to file size: budget and max width entry
  FitToSizeResultsDialog.cs/.Designer.cs # Fit to file size: ranked results picker
  ProgressDialog.cs/.Designer.cs  # Progress dialog for batch operations
  Models/
    AppConfig.cs                  # The whole of sic.json
    ConfigLoadResult.cs           # Loaded settings plus any message for the user
    ConversionPreset.cs           # A named format and resize setting (Convert > Presets, --preset)
    GeneralSettings.cs            # The general section of sic.json
    ImageItem.cs                  # Image queue item data model
    ResizeMode.cs                 # Resize mode enum (KeepProportions, Crop)
    SizeFitProposal.cs            # One viable fit-to-file-size result
  Services/
    ImageConverter.cs             # Magick.NET conversion engine
    PresetCapturer.cs             # Turns the main window's controls into a preset
    PresetDescriber.cs            # Summaries such as "WEBP, width 300"
    PresetResolver.cs             # Finds a preset by name, merges it with CLI options
    PresetValidator.cs            # Preset names and loaded presets
    ResizeValidator.cs            # Resize sizes, shared by the GUI, the CLI and presets
    UpdateService.cs              # NetSparkle update checks
    UnsupportedImageException.cs  # Thrown when content loads but isn't a decodable image
  Utils/
    ActivationListener.cs         # Answers a second launch in the running GUI
    Config.cs                     # Settings the app runs with, loaded from sic.json
    ConfigSerializer.cs           # sic.json reading, migration, recovery and atomic saving
    FileHelper.cs                 # Cloud placeholder detection, file enumeration
    LegacyConfigReader.cs         # Reads Sic.cfg files from SIC! 1.1 and earlier
    Localization.cs               # GetText.NET wrapper
    PresetMenuBuilder.cs          # The Convert > Presets submenu
    SingleInstanceGuard.cs        # One GUI per data folder
    UrlHelper.cs                  # http(s) link validation
    Enums/
      DimensionBox.cs             # Which size boxes SIC! filled in itself
      PresetNameResult.cs         # Outcome of checking a preset name
      ResizeError.cs              # Outcome of checking resize sizes
      UpdateCheckInterval.cs      # Background update-check frequency
    Constants/
      App.cs                      # App metadata and paths
      ExitCode.cs                 # CLI exit code constants
      Logging.cs                  # Log file paths and templates
tests/Sic.Tests/                  # xUnit tests (dotnet test): conversion, fit to file size, CLI, catalogs
```

## License

Copyright 2026 Oire Software. Licensed under the [Apache License 2.0](LICENSE).
