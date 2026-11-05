# A trace-free portable mode with working updates

## Overview

Portable mode claims to leave no traces, but it doesn't. NetSparkle writes to `HKCU\Software\Oire Software\SIC!\NetSparkleUpdater` in every mode, because `UpdateService` sets no configuration and the default on Windows is the registry. Update downloads, NetSparkle's `.cmd` file and Magick's temporary files all go to `%TEMP%`, and the file dialogs add to Windows' recent items.

Worse, portable updates don't work. The appcast lists only `setup.exe`, so "updating" a portable copy installs a second copy on the machine and leaves the portable folder on the old version.

This plan keeps NetSparkle, which already has the hooks needed, moves all of SIC!'s state into its own folder, and gives portable copies their own appcast and an in-place update.

It's the last of four plans. `89-json-config` must be merged first, because the update flow has to release `SingleInstanceGuard`.

## Done when

- [ ] SIC! itself writes nothing to the registry, apart from one-time removal of its old key.
- [ ] A portable copy writes no SIC! files outside its own folder, including while checking for and installing updates.
- [ ] Updating a portable copy replaces the files in its own folder, keeps `userdata` untouched, restarts the new version, and installs nothing on the machine.
- [ ] A staged update that can't run on this machine, for example because it needs a newer .NET, is refused before any file changes.
- [ ] A failed portable update leaves the old version working, and tells the user.
- [ ] An update interrupted partway through recovers on the next start, or, in the one moment with no `Sic.exe`, through a documented rename.
- [ ] A read-only portable copy still starts.
- [ ] `userdata\temp` is gone after SIC! closes, and anything a crash left there is removed on the next start.
- [ ] The old `HKCU\Software\Oire Software\SIC!` key is removed, and sibling keys such as `PlanCake` are left alone.
- [ ] Parallel CLI runs in a portable copy can't delete each other's temporary files.
- [ ] The update window shows release notes as plain text in the interface language, falling back to English, and loads nothing from the web.
- [ ] Answering No to the queue prompt cancels an update before any file changes, and never leaves two versions running.
- [ ] After a failed update, closing SIC! still asks about the queue.
- [ ] `build-installer.ps1 -Appcast` produces a signed `appcast-portable.xml` with an enclosure pointing at the ZIP, fails if it has none, and `-NoPortable` skips it. Both appcasts carry the notes in all seven languages.
- [ ] All new strings are translated into de, es, fr, he, ru and uk, and all 7 manuals are updated.
- [ ] all validation commands pass

## Validation commands

Run from the repository root.

- build: `dotnet build`
- test: `dotnet test`
- format check: `dotnet format --verify-no-changes`
- compile translations (after any `.po` change): `powershell -ExecutionPolicy Bypass -File src/Sic/locale/scripts/Compile-Translations.ps1`

There are no e2e tests. The real update flow is checked by hand; see Post-completion.

## Context

### Files involved

- Updater: `src/Sic/Services/UpdateService.cs`, `src/Sic/MainWindow.cs` (`MainWindow_FormClosing`)
- Startup: `src/Sic/Program.cs`, `src/Sic/Utils/Constants/App.cs`
- Progress UI: `src/Sic/ProgressDialog.cs`
- Update window: `src/Sic/Services/UpdateService.cs`, which uses NetSparkle's `UIFactory` today
- Dialogs: `src/Sic/MainWindow.cs`, `AddFolderDialog.cs`, `SettingsDialog.cs`
- Magick: `src/Sic/Services/ImageConverter.cs`
- Release: `installer/build-installer.ps1`
- Docs: `README.md`, `CHANGELOG.md`, `changelogs/1.2.0.0.md`, `CLAUDE.md`, `src/Sic/help/*/manual.html`
- Translations: `src/Sic/locale/{de,es,fr,he,ru,uk}/Sic.po`

### What NetSparkle offers

Checked against the 3.1.0 package and the source in `C:\repos\Oire\NetSparkle`:

- `SparkleUpdater.Configuration` is settable. Its default is `RegistryConfiguration` on Windows.
- `JSONConfiguration(IAssemblyAccessor, string? savePath)` saves to a given path, and the folder must already exist. `new AssemblyDiagnosticsAccessor(null)` is what NetSparkle's own default uses.
- `TmpDownloadFilePath` sets the download folder, created at download time, and `TmpDownloadFileNameWithExtension` the file name. `GetDownloadPathForAppCastItem` is `public virtual`, and a null result ends the download quietly.
- `RunDownloadedInstaller` is `protected virtual`, and runs only after the Ed25519 signature check passes. An override must not call base, which writes the `.cmd` to `%TEMP%`.
- `PreparingToExit` runs before `RunDownloadedInstaller`, and can cancel. A handler that throws counts as permission to continue.
- `UIFactory.CreateUpdateAvailableWindow` is `public virtual` and returns an `IUpdateAvailable`, a small interface: `UserResponded`, `Show`, `Close`, `BringToFront`, `Result`, `CurrentItem`, `HideReleaseNotes`, `HideRemindMeLaterButton` and `HideSkipButton`.
- `SparkleUpdater.AppCastGenerator` is settable, and `IAppCastGenerator` can wrap the standard `XMLAppCastGenerator`, whose own methods aren't virtual. It keeps only `<description>`, and ignores elements it doesn't know.
- `InstallUpdateFailed` fires when an installed update can't start.
- `QuitApplication()` is public, and swallows exceptions. With no close events set, it calls `UIFactory.Shutdown()`, which ends in `Application.Exit()` and raises `FormClosing`.

### Conventions and traps

- The published file is `Sic.exe` with a capital S. Every comparison of file names and paths uses `StringComparison.OrdinalIgnoreCase`, and fixtures use the real casing.
- `HKCU\Software\Oire Software` also holds `PlanCake`. Never delete that parent while it holds anything else.
- `ImageMagick` stays inside `ImageConverter`; `using ImageMagick` appears nowhere else in the app.
- `.editorconfig` requires CRLF, so run `dotnet format` on new `.cs` files before the format check.
- `.editorconfig` raises whole analyzer categories, and it applies to `tests/` too. Warnings are errors. Security rules are errors, so expect CA5389 on extraction paths: validate each with `Path.GetFullPath` plus a separator-terminated prefix check.
- Same-line braces, and `class X: Form` with no space before the colon. `_` is the gettext method, so `_ = ...` is not a discard.
- Every message box goes through `DialogHelper.Show`, never `MessageBox.Show`.
- Services are named `...Service`, `-or` or `-er`.
- Never use `/dev/null`, `nul` or any other null device in a shell command.
- Do not delete the real registry key while working on this. Tests use a throwaway key.

## Development approach

- One task at a time; the validation commands pass before the next task starts.
- Code changes come with tests for the new and changed behavior, success and error paths. Keep logic in plain classes that can be tested.
- A bad updater can't repair itself once it's in users' hands, so prefer failing safely, with the old version intact, over anything clever.
- Keep backward compatibility: installed copies update exactly as before.
- When scope changes, update this plan: new tasks get a "➕" prefix, blockers a "⚠️" prefix.

## Implementation steps

### Task 1: Test the design's assumptions on the real app

#### Files

- Modify: this plan, with what was found

#### Steps

- [ ] Publish `Sic.exe` into a scratch folder with `userdata` beside it. Start it, convert one image so Magick is loaded, and rename both `Sic.exe` and `Magick.Native-Q16-x64.dll` from PowerShell while it runs, on NTFS. If that fails, stop and tell the user, because Tasks 4 to 6 depend on it.
- [ ] With the files renamed and new copies in place, use the running app, including menus not yet opened, and check whether loading a bundled assembly after the swap misbehaves. Record which assemblies matter, so code after the swap stays with already loaded ones.
- [ ] Confirm `Sic.exe --version` exits with 0 despite the required `--input`. If it doesn't, pick another harmless way for the staged exe to prove it runs, and update Task 4.
- [ ] Record the results here. The owner repeats the rename on exFAT before Task 2 starts.

### Task 2: Move updater state out of the registry

#### Files

- Create: `src/Sic/Services/LegacyUpdaterStateRemover.cs`
- Create: `tests/Sic.Tests/LegacyUpdaterStateRemoverTests.cs`
- Modify: `src/Sic/Services/UpdateService.cs`
- Modify: `src/Sic/Utils/Constants/App.cs`
- Modify: `src/Sic/Program.cs`

#### Steps

- [ ] Add `App.UpdaterStateFileName = "updater.json"`.
- [ ] In `UpdateService`, create `App.DataFolder` if needed, then set `Configuration = new JSONConfiguration(new AssemblyDiagnosticsAccessor(null), Path.Combine(App.DataFolder, App.UpdaterStateFileName))` in every edition. One code path also means the uninstaller removes this state.
- [ ] Add `LegacyUpdaterStateRemover.Remove(parentKeyPath, appKeyName)`, deleting the app key, then the parent only if it is then empty. It only reads when the key doesn't exist, and a failure is logged and ignored.
- [ ] Call it from `Program.Main` at the place given under Technical details, with `Software\Oire Software` and `SIC!`.
- [ ] tests using a throwaway key under `HKCU\Software\Oire Software Tests`: removes the app key, keeps a sibling, removes an empty parent, and does nothing when the key is missing
- [ ] validation commands pass

### Task 3: Keep temporary files and recent items out of Windows

#### Files

- Create: `src/Sic/Utils/PortableTempFolder.cs`
- Create: `tests/Sic.Tests/PortableTempFolderTests.cs`
- Modify: `src/Sic/Services/ImageConverter.cs`
- Modify: `src/Sic/Services/UpdateService.cs`
- Modify: `src/Sic/Utils/Constants/App.cs`
- Modify: `src/Sic/Program.cs`
- Modify: `src/Sic/MainWindow.cs`
- Modify: `src/Sic/AddFolderDialog.cs`
- Modify: `src/Sic/SettingsDialog.cs`

#### Steps

- [ ] Add `App.UpdatesFolder = Path.Combine(DataFolder, "updates")`, and use it everywhere this plan names `userdata\updates`.
- [ ] Add `ImageConverter.SetTempFolder(path)`, wrapping `MagickNET.SetTempDirectory`, so `ImageMagick` stays in Services.
- [ ] Add `PortableTempFolder` with `TryCreate(dataFolder, processId)` returning `userdata\temp\<pid>`, retrying briefly because another process may be removing `temp` at that moment, or null if it still can't be created; `Delete(folder)`, which then removes `userdata\temp` with a non-recursive `Directory.Delete`, ignoring the failure when it isn't empty; and `RemoveStale(dataFolder, isProcessRunning)`, which deletes folders with numeric names whose process has gone, and leaves other names alone. None of them throws.
- [ ] Add the real `isProcessRunning`, in which only "no such process" counts as gone. Any other exception, such as access denied for an elevated copy, counts as running. Dispose the `Process`.
- [ ] In `Program.Main`, in portable mode, follow the order under Technical details. If `TryCreate` returns null, log it and skip `SetTempFolder`, so a read-only copy still starts. Keep the folder in a `string?` declared before the `try`, because it stays null when the guard is held, and delete it in the existing `finally`, before `Log.CloseAndFlush`, so a failure can still be logged. Remove `Environment.Exit` from the fatal path, which skips `finally`; the `return ExitCode.Error` after it does the same job.
- [ ] In portable mode, set `TmpDownloadFilePath` to `App.UpdatesFolder`.
- [ ] Set `AddToRecent = !App.IsPortable` on the `OpenFileDialog` in `MainWindow` and the `FolderBrowserDialog`s in `AddFolderDialog` and `SettingsDialog`.
- [ ] tests for `PortableTempFolder`: two process IDs get separate folders, an unwritable folder gives null, `Delete` removes an empty `temp` but keeps one holding another process's folder, `RemoveStale` keeps a running process's folder and removes a dead one's, a locked file is skipped without failing, and parallel `TryCreate` and `Delete` calls on one parent never lose a live folder or return null
- [ ] validation commands pass

### Task 4: Install portable updates in place

#### Files

- Create: `src/Sic/Services/PortableUpdateInstaller.cs`
- Create: `src/Sic/Utils/Enums/PortableUpdateResult.cs`
- Create: `tests/Sic.Tests/PortableUpdateInstallerTests.cs`
- Modify: `src/Sic/Utils/Constants/App.cs`

#### Steps

- [ ] Add `App.ExecutableName = "Sic.exe"`, and the `PortableUpdateResult` enum: `Installed`, `Refused`, `RolledBack`, `RollbackFailed`.
- [ ] Add `PortableUpdateInstaller.Apply(zipPath, appFolder, updatesFolder, runStagedExe)`, `CleanUp(appFolder, updatesFolder)`, `IsFolderWritable(folder)` and an internal `GetSwapOrder(paths)` following the flow under Technical details. Callers pass `App.UpdatesFolder`, which is one shared folder under `dotnet test`. It takes folders as arguments, shows no UI, and starts a process only through the `runStagedExe` delegate, so tests can use temporary folders.
- [ ] tests for success, with fixtures using the real `Sic.exe` casing: new and changed files land in place, files are replaced in subfolders, and old files get the `.sic-old` suffix. `GetSwapOrder` puts `Sic.exe` last.
- [ ] tests for protection: `userdata` is never touched, including entries named `USERDATA/x` and `userdata\x`
- [ ] tests for real ZIP shapes: directory entries such as `help/` and `userdata/`, and backslash names
- [ ] tests for bad archives: a path escaping the folder (zip slip), an absolute path, a ZIP with no `Sic.exe`, and a corrupt ZIP. Each gives `Refused` and leaves the app folder unchanged. A lowercase `sic.exe` is accepted.
- [ ] tests for the staged exe: a `runStagedExe` returning false gives `Refused` with the app folder unchanged, and a `runStagedExe` that writes `userdata\sic.json` and a log into the staged `userdata` leaves the real `userdata` unchanged
- [ ] tests for rollback: a target file that can't be moved, because a test holds it open with no sharing, restores everything and gives `RolledBack`; an undo that also fails gives `RollbackFailed` and keeps the journal
- [ ] tests for leftovers: `Apply` refuses while `swap.json` exists, keeping its `.sic-old` files; otherwise it deletes old `.sic-old` files first, and refuses unchanged if one can't be deleted; its scan never enters `userdata`; a leftover `swap-done.json` is deleted; stale staging is wiped before extracting
- [ ] tests for step 6: a failure deleting the ZIP after a complete swap still gives `Installed`
- [ ] tests for unexpected failures: an exception while extracting or writing the journal gives `Refused`, with the app folder unchanged
- [ ] tests for `CleanUp`: each journal state under Technical details; two runs with one locked file; a stale `Sic.exe.sic-old` followed by an interrupted update; a renamed exe; a user's own `.old` file surviving; read-only `.sic-old` files; a folder moved after the swap began; a corrupt `swap.json`; a file the update added, which rollback removes; and `updatesFolder` being cleared once no journal remains
- [ ] tests for `IsFolderWritable`: a normal folder, and a missing one
- [ ] validation commands pass

### Task 5: Ask about the queue before an update starts

#### Files

- Create: `tests/Sic.Tests/UpdateExitTests.cs`
- Modify: `src/Sic/Services/UpdateService.cs`
- Modify: `src/Sic/MainWindow.cs`

#### Steps

- [ ] Move the question from `MainWindow_FormClosing` into a private `ConfirmExitWithQueue()` method, keeping its strings.
- [ ] Add `public Func<bool>? ConfirmExit { get; set; }`, `public bool IsExitingForUpdate { get; private set; }` and `CancelExitForUpdate()` to `UpdateService`. Subscribe to `PreparingToExit`: cancel when `ConfirmExit` returns false or throws, otherwise set `IsExitingForUpdate`. Put the decision in an internal method that tests can call. Subscribe to `InstallUpdateFailed` too, and call `CancelExitForUpdate` from it, so closing SIC! still asks about the queue after an installed copy's update fails.
- [ ] `MainWindow` sets `ConfirmExit`, and `MainWindow_FormClosing` skips its question when `_updateService?.IsExitingForUpdate` is true. This also helps installed copies, where a No after the installer's `.cmd` was already waiting would delay or lose the install.
- [ ] tests for that method: true sets the flag, false cancels and leaves it clear, a throwing `ConfirmExit` cancels, a null `ConfirmExit` proceeds, and `CancelExitForUpdate` clears the flag
- [ ] validation commands pass

### Task 6: Use the portable updater in portable mode

#### Files

- Create: `src/Sic/Services/PortableSparkleUpdater.cs`
- Create: `tests/Sic.Tests/PortableUpdateFlowTests.cs`
- Modify: `src/Sic/Services/UpdateService.cs`
- Modify: `src/Sic/ProgressDialog.cs`
- Modify: `src/Sic/ProgressDialog.Designer.cs`
- Modify: `src/Sic/Utils/Constants/App.cs`
- Modify: `src/Sic/Program.cs`

#### Steps

- [ ] Add `App.PortableAppcastUrl = "https://sic.oire.dev/appcast-portable.xml"`, and `Program.RestartAfterExit`.
- [ ] Add `PortableSparkleUpdater: SparkleUpdater`. Override `GetDownloadPathForAppCastItem` to check `IsFolderWritable` first. If the folder isn't writable, for example `userdata` was added to an installed copy in Program Files, say so, point to the portable ZIP, and return null. NetSparkle's own folder creation would otherwise fail first with a generic error.
- [ ] Add a way to create `ProgressDialog` with no Cancel button, because all its callers have one today and a swap can't be canceled.
- [ ] Override `RunDownloadedInstaller`, without calling base. Run `Apply` on a background thread behind that dialog, modal, with `FormClosing` blocked until it finishes, so a screen reader user hears progress and can't kill the swap. NetSparkle calls this from `async void`, so wrap the whole override: any exception calls `CancelExitForUpdate` and shows the failure message.
- [ ] `runStagedExe` starts the staged `Sic.exe --version` with a 30-second timeout and requires exit code 0, killing the process on timeout so staging can be wiped. Set `DOTNET_DISABLE_GUI_ERRORS=1` in its environment, or a missing runtime shows .NET's own dialog over the progress dialog until the timeout. The staging folder has an empty `userdata` beside the exe, so the run stays portable.
- [ ] On `Installed`, set `Program.RestartAfterExit` and call `await QuitApplication()`. In `Main`, after `Application.Run` returns, delete the temp folder, dispose `Program.InstanceGuard`, then start `Path.Combine(AppContext.BaseDirectory, App.ExecutableName)`. The guard must go first, or the new process finds the mutex held and exits. If that start fails, tell the user to start SIC! again.
- [ ] On any other result, call `CancelExitForUpdate`, log it, and show a message through `DialogHelper.Show`: for `Refused` and `RolledBack`, that the current version still works and where to download the new one; for `RollbackFailed`, to restart SIC!, which finishes the recovery.
- [ ] In `UpdateService`, pick the class, appcast URL and a `.zip` value for `TmpDownloadFileNameWithExtension` from `App.IsPortable`, sharing everything else. Both must keep `SecurityMode.Strict`.
- [ ] Move the mapping from `PortableUpdateResult` to its action, and the CLI's `swap.json` check, into small internal methods.
- [ ] tests for those methods: each result gives the right action, and the CLI refuses only while the guard is held
- [ ] Follow the `Program.Main` order under Technical details for `CleanUp`, wrapped so it can't stop startup. `CleanUp` retries locked files for a few seconds, because the old process may still be closing.
- [ ] validation commands pass

### Task 7: Show release notes in SIC!'s own update dialog

#### Files

- Create: `src/Sic/UpdateDialog.cs`
- Create: `src/Sic/UpdateDialog.Designer.cs`
- Create: `src/Sic/Services/SicUIFactory.cs`
- Create: `src/Sic/Services/LocalizedAppCastGenerator.cs`
- Create: `src/Sic/Services/ReleaseNotesSelector.cs`
- Create: `tests/Sic.Tests/ReleaseNotesSelectorTests.cs`
- Create: `tests/Sic.Tests/LocalizedAppCastGeneratorTests.cs`
- Modify: `src/Sic/Services/UpdateService.cs`
- Modify: `src/Sic/Utils/Constants/App.cs`

#### Steps

- [ ] Add `App.AppcastNotesNamespace = "https://oire.dev/xml-namespaces/appcast"`.
- [ ] Add `ReleaseNotesSelector.Select(notes, language, fallback)`, choosing as under Technical details and returning text with `ReplaceLineEndings("\r\n")`, because a multi-line `TextBox` ignores the bare LF that XML parsing leaves.
- [ ] Add `LocalizedAppCastGenerator: IAppCastGenerator`, wrapping `XMLAppCastGenerator`. After parsing, it reads each item's `<oire:notes>` from the same XML, matching by `sparkle:version`, and puts the selected text in `item.Description`, using `Localization.GetCurrentCulture().Name`. Any problem with the notes is logged and leaves `Description` as it was, so notes can never stop an update check.
- [ ] Add `UpdateDialog: Form, IUpdateAvailable` as under Technical details. It shows `updates[0]` only, and sends `UpdateAvailableResult.None` when closed without a choice. Follow NetSparkle's `UpdateAvailableWindow` for `Show`, `Close` and `Result`.
- [ ] Add `SicUIFactory: UIFactory`, overriding `CreateUpdateAvailableWindow` to return `UpdateDialog`.
- [ ] In `UpdateService`, use `SicUIFactory` and `LocalizedAppCastGenerator` in both editions.
- [ ] tests for `Select`: exact tag, primary subtag (`de-AT` finding `de`), English, then the fallback; different case in a tag; an empty list; and CRLF in the result
- [ ] tests for `LocalizedAppCastGenerator`: a real appcast with Hebrew and Cyrillic notes; no notes, which keeps `<description>`; malformed notes, which change nothing; and the same items and order as `XMLAppCastGenerator`
- [ ] validation commands pass

### Task 8: Publish the portable appcast and localized notes

#### Files

- Modify: `installer/build-installer.ps1`

#### Steps

- [ ] After the installer appcast, run `netsparkle-generate-appcast` again with the same arguments, except `--single-file $PortableZip` and `--output-file-name appcast-portable`, with no extension, producing `appcast-portable.xml` and `appcast-portable.xml.signature` in `$OutputDir`, and check `$LASTEXITCODE`. `--file-version` is mandatory, because a ZIP has no version resource. Without it the tool writes and signs an empty appcast and exits 0.
- [ ] Fail the build unless `appcast-portable.xml` contains an `<enclosure`.
- [ ] Keep this step after the ZIP is built and after Authenticode signing, so the Ed25519 signature covers the final ZIP.
- [ ] Add a step that puts the notes into each appcast, as under Technical details, and fails the build unless `changelogs\$Version` holds a non-empty file for all seven languages.
- [ ] After adding notes, sign each appcast again, because its signature covers the file's bytes: run `--generate-signature` with `--key-path`, read the `Signature: <base64>` line, write only the base64 to the `.signature` file, and check it with `--verify`.
- [ ] With `-NoPortable`, skip this step, so existing portable copies aren't offered a ZIP that doesn't exist.
- [ ] Add both files to the `-Deploy` upload list and to the "files to upload" summary.
- [ ] Run the generator against a local test ZIP into a scratch folder, with throwaway keys rather than `keys/`, and confirm the enclosure points to the ZIP with `sparkle:os="windows"` and a signature, every `<oire:notes>` is there, and `--verify` accepts the new `.signature`.
- [ ] validation commands pass

### Task 9: Translate the new strings

#### Files

- Modify: `src/Sic/locale/messages.pot`
- Modify: `src/Sic/locale/{de,es,fr,he,ru,uk}/Sic.po`

#### Steps

- [ ] Run `Extract-Strings.ps1`, then `Update-Translations.ps1`.
- [ ] Translate every new string in all six languages, and remove the `#, fuzzy` flag from each new entry.
- [ ] Run `Compile-Translations.ps1`.
- [ ] validation commands pass, including `TranslationCatalogTests`

### Task 10: Update the manuals and documentation

#### Files

- Modify: `src/Sic/help/{en,de,es,fr,he,ru,uk}/manual.html`
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `changelogs/1.2.0.0.md`
- Modify: `CLAUDE.md`

#### Steps

- [ ] All seven manuals: rewrite the portable mode text. Replace "without leaving traces" with: SIC! itself keeps everything in its own folder; Windows may keep its own records, such as a list of programs that have run, which no app controls. Any correction after the Process Monitor check goes in Post-completion.
- [ ] All seven manuals, in Software updates: a portable copy updates its own folder, and copies older than 1.2 need the new ZIP downloaded by hand once.
- [ ] All seven manuals, in Software updates: describe the update window, and that release notes appear in the interface language where available.
- [ ] All seven manuals: if a portable update is cut off and `Sic.exe` is missing, rename `Sic.exe.sic-old` back to `Sic.exe` and start it, which finishes the recovery.
- [ ] `README.md`, `CHANGELOG.md` and `changelogs/1.2.0.0.md`: describe portable updates, and tell existing portable users to download the 1.2 ZIP by hand once, because older copies still point at the installer.
- [ ] Write the 1.2 release notes as plain text in `changelogs/1.2.0.0/{en,de,es,fr,he,ru,uk}.txt`, for someone deciding whether to update. Numbered sections and bullets are written by hand, and there's no Markdown. #35 ships in 1.2 too, so it must add to them.
- [ ] `CLAUDE.md`: state the rule that portable mode must not write outside its folder or to the registry, and document `updater.json`, the second appcast, `PortableUpdateInstaller` with its journal, the `Program.Main` order, and `build-installer.ps1`'s new output. Update the Release notes section with the two kinds of notes, and the folder needing the same version name as the `.md` file.
- [ ] `CLAUDE.md`: add what the 1.2 updater freezes. Every later release must keep `Sic.exe` at the ZIP root, keep `--version` exiting 0 from a folder with an empty `userdata`, and keep the appcast names and URLs.
- [ ] validation commands pass

## Technical details

### Program.Main order

GUI path, inside the `try`:

1. Acquire `SingleInstanceGuard`; return if it's held. This comes first, so a second launch can't touch files during a running update.
2. In portable mode: `PortableUpdateInstaller.CleanUp`, then temp folder setup, then `RemoveStale`. `CleanUp` must come before anything loads Magick's native library, which is why it precedes `SetTempFolder`.
3. `LegacyUpdaterStateRemover.Remove`.
4. `Config.Load`, and the rest of startup.

CLI path, in portable mode, in `Main` inside the `try`, before `RunCli`, so tests that call `RunCli` directly skip it:

1. If `swap.json` exists, try the guard. If it's held, an update is running: print an error and exit with `ExitCode.Error`. Otherwise run `CleanUp`, then release the guard.
2. Temp folder setup, then `RemoveStale`.

Nothing in these steps may stop startup. Each is wrapped, and a failure is logged and skipped.

### Temporary files

A portable copy keeps Magick's temporary files in `userdata\temp\<pid>`, not in `%TEMP%`. That way a crash on a borrowed computer leaves nothing on it: the leftovers stay on the portable copy, which can still clean them up.

Each process deletes its own folder on every exit, GUI or CLI, and `userdata\temp` with it if nothing else is there. A crash skips that, so every start removes folders whose process has gone.

### Portable update flow

`PortableUpdateInstaller.Apply(zipPath, appFolder, runStagedExe)`:

1. Return `Refused` if `swap.json` exists, because its `.sic-old` files may be the only copy of the old version. Otherwise delete every leftover `.sic-old` file, skipping `userdata` by name, and return `Refused` unchanged if one can't be deleted. Clear the read-only attribute first. Then delete any leftover `swap-done.json`, and wipe `updatesFolder\staging`.
2. Read the ZIP's entries before writing anything. Return `Refused` if any entry is absolute or resolves outside `appFolder`, or if `App.ExecutableName` isn't at its root, ignoring case. Normalize separators. Skip entries with an empty `Name`, which are directories, and any under `userdata` by full path, ignoring case. The accepted entries are the file list for every later step. Never enumerate staging, because the staged exe's own run writes into its `userdata`.
3. Extract into `updatesFolder\staging`, on the same volume as the app, so the moves below are renames. Flush each file with `FileStream.Flush(true)` and compare its length with the entry. Create an empty `userdata` beside the staged exe, and call `runStagedExe`. Return `Refused` if it fails.
4. Write `updatesFolder\swap.json` under a temporary name, flush it, then rename it, so a journal is either complete or absent. It lists each accepted file as a path relative to `appFolder`, and whether a target already existed. Relative paths keep it valid if a USB drive comes back under another letter.
5. For each accepted file, with `Sic.exe` last: rename an existing target to `<name>.sic-old`, then move the new file in, with a short retry on sharing violations, because antivirus often touches new files. Then rename `swap.json` to `swap-done.json`. Windows allows renaming a running exe and loaded DLLs but not overwriting them, which is why no helper process or script is needed.
6. Delete the ZIP and the staging folder, best effort. A failure here can't undo a good update.

Any exception before step 5 gives `Refused`, with the app folder unchanged. On a failure in step 5, undo every rename and move in reverse order, delete `swap.json` and the staging folder, and return `RolledBack`. If an undo fails, keep `swap.json` and return `RollbackFailed`.

`CleanUp(appFolder, updatesFolder)` trusts only the journal, and never scans for `.sic-old` files to restore. Each path in it is checked on reading with the same prefix check as ZIP entries. The journal is written atomically, so an unreadable one can only be disk damage: it's logged and treated as no journal.

- `swap.json`, with no `Sic.exe.sic-old`: the swap stopped before the exe's turn. Move each listed `.sic-old` file back over its target, delete any listed file that had no target before, then delete the journal last.
- `swap.json`, with both `Sic.exe` and `Sic.exe.sic-old`: the swap finished just before the journal's rename, because the exe goes last. Treat it as `swap-done.json`.
- `swap-done.json`: delete the listed `.sic-old` files, and keep the journal until all are gone. A locked file waits for the next start.
- No journal: restore nothing, and delete `updatesFolder`, including any download a crash left behind.

The `.sic-old` suffix is distinctive so that a user's own `.old` files are never touched. Files that the new version no longer ships stay in the folder, because deleting them would risk removing something the user put there.

The one moment with no `Sic.exe` is between its rename and the move, and renames can't close it. If a USB drive is pulled right there, the manual's recovery step applies: rename `Sic.exe.sic-old` back, and that start finishes the rollback.

### Exit order after a portable update

1. `Apply` returns `Installed`.
2. `Program.RestartAfterExit` is set, and `QuitApplication` closes the window. `IsExitingForUpdate` skips the queue question, which `PreparingToExit` already asked before any file changed.
3. `Application.Run` returns in `Main`.
4. The temp folder is deleted, which leaves `finally` nothing to do.
5. `Program.InstanceGuard` is disposed, so the new process can acquire it.
6. The new `Sic.exe` starts.

Starting the new version only after the old one has left `Application.Run` means two versions can't run together, even though `QuitApplication` swallows exceptions.

### Why portable copies have their own appcast

One `appcast.xml` with an exe and a ZIP entry was considered and rejected. Installed 1.1 copies can't be changed, and they'd treat both entries as the same update: they'd show the release notes twice and depend on the exe coming first. A second file leaves what they see exactly as it is today.

### Update dialog

`UpdateDialog` follows the rules in `23-custom-presets`: one flat `TableLayoutPanel`, `Dock = Fill` on every control, and labels with `AutoSize = true` and `TextAlign = ContentAlignment.MiddleLeft`. It has 3 columns (Percent 33.33 each), 4 rows (AutoSize, AutoSize, Percent 100, AutoSize) and a `ClientSize` of 520x400, and the title is the existing `_("Software Update")`:

1. Message, such as "SIC! {0} is available. You have version {1}.", spanning all 3 columns
2. `Release &notes:` label spanning all 3 columns
3. Notes `TextBox` spanning all 3 columns, with `Multiline`, `ReadOnly` and `ScrollBars = Vertical`
4. `&Install`; `Remind Me &Later`; `&Skip This Version`

Focus starts in the notes box, which keeps its caret, so a screen reader reads them and they can be reviewed in Braille. Empty notes show "No release notes are available." Follow the standard dialog constructor order, including `TextDirection.Apply`.

The idea comes from AccessMind Runtime's update window, where a hands-on comparison under a screen reader settled on a real edit control. It also means SIC! no longer loads a `WebBrowser`, which could write to Internet Settings and caches.

### Notes in the appcast

```xml
<rss version="2.0" xmlns:sparkle="..." xmlns:oire="https://oire.dev/xml-namespaces/appcast">
  <channel>
    <item>
      <description>Markdown from changelogs/1.2.0.0.md</description>
      <oire:notes xml:lang="en">Plain text from changelogs/1.2.0.0/en.txt</oire:notes>
      <oire:notes xml:lang="de">Plain text from changelogs/1.2.0.0/de.txt</oire:notes>
    </item>
  </channel>
</rss>
```

`<description>` stays as the generator writes it, because copies older than 1.2 read only that and render Markdown. Build the notes with `[System.Xml.Linq.XDocument]`, which escapes the text, and save as UTF-8 with no BOM.

`Select` follows AccessMind Runtime's order: the exact tag, then the primary subtag, then English, then the fallback, which is `<description>`. Runtime matches `notes` by local name, so this feed would work there unchanged if SIC!'s installed copies ever moved to a shared runtime.

### Why updater state is a separate file

`updater.json` is written by NetSparkle's own `JSONConfiguration`, which saves it whenever it likes. Keeping it apart from `sic.json` means NetSparkle can never overwrite the user's settings. It holds no settings: only the last check time and any skipped version.

### Intended behavior changes

- Installed copies keep updater state in `updater.json`, so a version someone chose to skip is offered once more.
- The queue question for an update now comes before the installer starts, in both editions.

### Limits

- Copies older than 1.2 already point at the installer appcast. They can't be fixed from here, so those users need the new ZIP by hand once.
- Windows itself records that a program ran, for example in Prefetch and UserAssist. No app can prevent that.
- A CLI run refused during an update stops before `SetLanguage`, so that error may not be in the chosen language.
- A read-only copy has nowhere of its own for temporary files, so Magick uses `%TEMP%` there.

## Post-completion

- Check portable traces with Process Monitor from the command line, so the results come out as CSV rather than through its interface: `Procmon /Quiet /Minimized /BackingFile trace.pml`, then `Procmon /Terminate`, then `Procmon /OpenLog trace.pml /SaveAs trace.csv`. Look for `Sic.exe` writes outside the portable folder across adding, converting, checking for updates, the update window and updating.
- In that trace, check whether `AddToRecent = false` is enough. The shell may still write `ComDlg32\OpenSavePidlMRU`, `LastVisitedPidlMRU`, `CIDSizeMRU` or `Bags` from inside the process. If it does, that's Windows' own record, and the manuals' wording must say so.
- In that trace, look for `%LOCALAPPDATA%\ImageMagick`. If it appears, set Magick's OpenCL cache folder inside `ImageConverter.SetTempFolder`.
- Run a portable build and confirm the old registry key is gone and `userdata\updater.json` exists.
- Update a real 1.2 portable copy from a test appcast, on NTFS and exFAT, to confirm the new version starts.
- Kill the process during the swap, and confirm the next start recovers. Pull a USB drive during an update, and try the manual's recovery step.
- With images in the queue, answer No to the update's queue question, and confirm nothing changed. Then make an update fail, and confirm closing SIC! still asks about the queue.
- Run a portable copy from a read-only folder, and confirm it starts.
- Update an installed copy, to confirm the installer path still works.
- With JAWS and NVDA, check the update dialog in English and Hebrew: focus starts in the notes, they read naturally, and the caret works on a Braille display.
- Point a real 1.1 copy at a test appcast with `<oire:notes>`, and confirm it still shows its `<description>` and updates.
