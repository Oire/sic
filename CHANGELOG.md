# Changelog

All notable changes to SIC! (Simple Image Converter) will be documented in this file.

## [1.2.0.0] — unreleased

### Features

- **Conversion presets**: save a target format and resize settings under a name, such as "Blog" for WebP images 300 px wide, and pick it instead of setting everything by hand each time ([#23](https://github.com/Oire/sic/issues/23)). The new **Convert > Presets** submenu lists your presets and offers **Add Preset** and **Manage Presets**, even with an empty queue. Choosing a preset fills in the format and resize controls without converting, and its sizes stay put while you add and select images, until you change them yourself. A preset with only a width scales every image of a batch to that width, portrait or landscape; one with both sizes fits each image inside them. A preset whose format is hidden in Settings still applies, and that format stays in the dropdown until SIC! closes. Presets are saved in `sic.json`; a preset that can't be used, for instance after a hand edit, is removed on its own, after the file is backed up as `sic.json.bad`, and SIC! says how many were removed. These presets are separate from the sizes offered by **Create Multi-size ICO**.
- **`--preset` on the command line**: `sic -i photo.png --preset Blog` converts with a saved preset (`-p` for short; case doesn't matter). An explicit `--output` extension, `--format`, `--resize` or `--crop` wins over the matching part of the preset, and an unknown name fails with the list of saved presets. Without `--output`, `--format` or `--preset`, the error message now names all three.

### Changes

- **Native menus and image list**: the menu bar and the image list are now real Windows controls rather than WinForms-drawn imitations, which is what screen readers need from them ([#68](https://github.com/Oire/sic/issues/68)). The menu is a genuine `HMENU` attached with `SetMenu`, announced as a menu bar with real submenus instead of a panel that draws things resembling menu items. The image list is a genuine `SysListView32` rather than one wearing a window class no screen reader recognizes, so every column is read instead of only the file name. Nothing changes about how either looks or behaves for sighted users.
- **Smaller icons**: the 256 px image in an ICO file is now stored as PNG, as in Windows' own icons, instead of as an uncompressed bitmap. A multi-size icon made with the Windows application preset drops from about 285 KB to about 85 KB, with the same pixels.
- **Signed by Oire Software**: the installer, its uninstaller and `sic.exe` now carry Oire Software's digital signature ([#86](https://github.com/Oire/sic/issues/86)). When you install, Windows' permission prompt names Oire Software as the verified publisher instead of an unknown one, and a file's **Properties** show the signature on the **Digital Signatures** tab. Windows SmartScreen may still warn for a while after a release; the manual's Installation section explains what to check.
- The publisher shown in Windows' list of installed apps is now **Oire Software**, the company's name as registered and as the signature gives it.
- **Settings in JSON**: settings are now stored in `sic.json` instead of `Sic.cfg`, in the same folder ([#89](https://github.com/Oire/sic/issues/89)). The first start of this version moves your settings over automatically and keeps the old file as `Sic.cfg.bak`; to go back to 1.1 with your old settings, rename it back to `Sic.cfg`. A value SIC! can't use is reset to its default on its own, keeping every other setting, and a damaged file is kept as `sic.json.bad` and reported rather than silently replaced. If the file can't be read at all, SIC! says so and leaves it untouched, and changes made in that session aren't saved.
- **One window at a time**: starting SIC! while it's already running brings the open window forward, or the dialog open in it, instead of opening a second window that could overwrite the first one's settings ([#89](https://github.com/Oire/sic/issues/89)). If Windows doesn't let it come forward, SIC! says it's already running and that Alt+Tab switches to it. Copies with their own settings, such as portable copies in different folders or a portable and an installed copy, still run side by side, and the command line still runs while the window is open.
- Menu shortcut labels now use the key names of your language, such as `Strg+N` in German or `Maj` in French, matching the manual. The keys themselves are unchanged.
- The **Edit** and **Convert** menus are no longer unavailable as a whole while the queue is empty; their items that need images are unavailable on their own, so **Convert > Presets** always works.
- A mistyped size in the main window, such as `30o` in **Width**, now gets its own message and moves the focus to that field, whatever the other field holds. It used to be ignored, or to bring up the general "at least one valid dimension" message.
- Command line: `--resize` now rejects sizes above 65535 pixels, as the main window always has, and the "Invalid width value" and "Invalid height value" messages quote the size exactly as typed.

### Fixes

- The main window now shows the SIC! icon in its title bar and in Alt+Tab instead of the generic Windows Forms one.
- Converting an image with one side of 256 px or more and the other smaller, such as 300x200, to ICO now stores it as PNG with the right dimensions. It used to come out as an uncompressed bitmap of about 250 KB with the wrong width recorded in the file.
- Icons added by link, such as a site's `favicon.ico`, are now accepted. SIC! used to reject them as not an image, although it opened the same icon from a file.
- Converting an image larger than 512 px to ICO now fails with a clear message about the 512x512 px limit instead of an internal error, and every size of a multi-size icon is checked, not only the first.
- A language set to a code SIC! has no translation for, such as `pl`, now falls back to English as intended.
- Command line: converting a link with `--format` names the output after the file in the link, or `downloaded_image` when the link has no file name, instead of writing a file called `.jpg`.
- A downloaded image's file name no longer keeps characters a file name can't hold, so an escaped slash in a link can't place the converted file outside the output folder.
- Command line: when SIC! can't save its settings, it now says so in the console instead of opening a message box.
- One bad value in the settings file no longer makes SIC! discard all the others, and a language setting SIC! doesn't recognize no longer stops it from starting: it falls back to the system language.
- A portable copy moved to another folder or drive letter now keeps saving to its own `Converted` folder. It used to keep the old copy's full path, writing there if it still existed or warning that the output folder no longer existed.
- Command line: when the custom output folder is missing, for instance on a drive that is unplugged, SIC! now uses the default folder for that run only, and no longer forgets the custom one.
- The size SIC! fills in for the other side of a very wide or very tall image is now always from 1 to 65535 pixels. A 4000x100 image set to a width of 16 used to get a height of 0, and an image over 65535 pixels on a side filled in a size SIC! then refused.
- Command line: an unsupported `--format` no longer creates the output folder before failing.

### Other

- SIC! can now be installed and updated through WinGet.
- SIC! now has automated tests, run on every change: conversion, resizing, ICO output, fit to file size, the command line and the translation catalogs ([#88](https://github.com/Oire/sic/issues/88)).

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
