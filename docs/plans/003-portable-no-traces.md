# A trace-free portable mode with working updates

## Overview

Portable mode claims to leave no traces, but it doesn't. NetSparkle writes to `HKCU\Software\Oire Software\SIC!\NetSparkleUpdater` in every mode, because `UpdateService` sets no configuration and the default on Windows is the registry. Update downloads, NetSparkle's `.cmd` file and Magick's temporary files all go to `%TEMP%`, and the file dialogs add to Windows' recent items.

Worse, portable updates don't work. The appcast lists only `setup.exe`, so "updating" a portable copy installs a second copy on the machine and leaves the portable folder on the old version.

This plan keeps NetSparkle, which already has the hooks needed, moves all of SIC!'s state into its own folder, and gives portable copies their own appcast and an in-place update.

It's the last of four plans. `002-json-config` must be merged first, because the update flow has to release `SingleInstanceGuard`.

## Done when

- [ ] SIC! itself writes nothing to the registry, apart from one-time removal of its old key.
- [ ] A portable copy writes no SIC! files outside its own folder, including while checking for and installing updates.
- [ ] Updating a portable copy replaces the files in its own folder, keeps `userdata` untouched, restarts the new version, and installs nothing on the machine.
- [ ] A failed portable update leaves the old version working, and tells the user.
- [ ] An update interrupted partway through, such as a USB drive being pulled, recovers on the next start.
- [ ] The old `HKCU\Software\Oire Software\SIC!` key is removed, and sibling keys such as `PlanCake` are left alone.
- [ ] Parallel CLI runs in a portable copy can't delete each other's temporary files.
- [ ] Answering No to the queue prompt cancels an update before any file changes, and never leaves two versions running.
- [ ] `build-installer.ps1 -Appcast` produces a signed `appcast-portable.xml` pointing at the ZIP, and `-NoPortable` skips it.
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
- Dialogs: `src/Sic/MainWindow.cs`, `AddFolderDialog.cs`, `SettingsDialog.cs`
- Magick: `src/Sic/Services/ImageConverter.cs`
- Release: `installer/build-installer.ps1`
- Docs: `README.md`, `CHANGELOG.md`, `changelogs/1.2.0.0.md`, `CLAUDE.md`, `src/Sic/help/*/manual.html`
- Translations: `src/Sic/locale/{de,es,fr,he,ru,uk}/Sic.po`

### What NetSparkle offers

Checked against the 3.1.0 package:

- `SparkleUpdater.Configuration` is settable. Its default is `RegistryConfiguration` on Windows.
- `JSONConfiguration(IAssemblyAccessor, string? savePath)` saves to a given path, and the folder must already exist. `new AssemblyDiagnosticsAccessor(null)` is what NetSparkle's own default uses.
- `TmpDownloadFilePath` sets the download folder, and `TmpDownloadFileNameWithExtension` the file name.
- `RunDownloadedInstaller` is `protected virtual`, and runs only after the Ed25519 signature check passes.
- `PreparingToExit` runs before `RunDownloadedInstaller`, and can cancel.
- `QuitApplication()` is public. With no close events set, it calls `UIFactory.Shutdown()`, which ends in `Application.Exit()` and raises `FormClosing`.

### Conventions and traps

- The published file is `Sic.exe` with a capital S. Every comparison of file names and paths uses `StringComparison.OrdinalIgnoreCase`, and fixtures use the real casing.
- `HKCU\Software\Oire Software` also holds `PlanCake`. Never delete that parent while it holds anything else.
- `ImageMagick` stays inside `ImageConverter`; `using ImageMagick` appears nowhere else in the app.
- `.editorconfig` requires CRLF, so run `dotnet format` on new `.cs` files before the format check.
- `.editorconfig` raises whole analyzer categories, and it applies to `tests/` too. Warnings are errors. Security rules are errors, so expect CA5389 on extraction paths: validate each with `Path.GetFullPath` plus a separator-terminated prefix check.
- Same-line braces, and `class X: Form` with no space before the colon. `_` is the gettext method, so `_ = ...` is not a discard.
- Every message box goes through `DialogHelper.Show`, never `MessageBox.Show`.
- Never use `/dev/null`, `nul` or any other null device in a shell command.
- Do not delete the real registry key while working on this. Tests use a throwaway key.

## Development approach

- One task at a time; the validation commands pass before the next task starts.
- Code changes come with tests for the new and changed behavior, success and error paths. Keep logic in plain classes that can be tested.
- A bad updater can't repair itself once it's in users' hands, so prefer failing safely, with the old version intact, over anything clever.
- Keep backward compatibility: installed copies update exactly as before.
- When scope changes, update this plan: new tasks get a "➕" prefix, blockers a "⚠️" prefix.

## Implementation steps

### Task 1: Move updater state out of the registry

#### Files

- Create: `src/Sic/Services/LegacyUpdaterState.cs`
- Create: `tests/Sic.Tests/LegacyUpdaterStateTests.cs`
- Modify: `src/Sic/Services/UpdateService.cs`
- Modify: `src/Sic/Utils/Constants/App.cs`
- Modify: `src/Sic/Program.cs`

#### Steps

- [ ] Add `App.UpdaterStateFileName = "updater.json"`.
- [ ] In `UpdateService`, create `App.DataFolder` if needed, then set `Configuration = new JSONConfiguration(new AssemblyDiagnosticsAccessor(null), Path.Combine(App.DataFolder, App.UpdaterStateFileName))` in every edition. One code path also means the uninstaller removes this state.
- [ ] Add `LegacyUpdaterState.Remove(parentKeyPath, appKeyName)`, deleting the app key, then the parent only if it is then empty. It only reads when the key doesn't exist, and a failure is logged and ignored.
- [ ] Call it on the GUI path in `Program.Main` with `Software\Oire Software` and `SIC!`.
- [ ] tests using a throwaway key under `HKCU\Software\Oire Software Tests`: removes the app key, keeps a sibling, removes an empty parent, and does nothing when the key is missing
- [ ] validation commands pass

### Task 2: Keep temporary files and recent items out of Windows

#### Files

- Create: `src/Sic/Utils/PortableTempFolder.cs`
- Create: `tests/Sic.Tests/PortableTempFolderTests.cs`
- Modify: `src/Sic/Services/ImageConverter.cs`
- Modify: `src/Sic/Services/UpdateService.cs`
- Modify: `src/Sic/Program.cs`
- Modify: `src/Sic/MainWindow.cs`
- Modify: `src/Sic/AddFolderDialog.cs`
- Modify: `src/Sic/SettingsDialog.cs`

#### Steps

- [ ] Add `ImageConverter.SetTempFolder(path)`, wrapping `MagickNET.SetTempDirectory`, so `ImageMagick` stays in Services.
- [ ] Add `PortableTempFolder` with `Create(dataFolder, processId)` returning `userdata\temp\<pid>`, `Delete(folder)`, and `RemoveStale(dataFolder, isProcessRunning)`, which deletes folders whose process has gone.
- [ ] In `Program.Main`, in portable mode only and before the CLI branch: create the folder before calling `SetTempFolder`, and delete it in the existing `finally`. On the GUI path, also call `RemoveStale`.
- [ ] In portable mode, set `TmpDownloadFilePath` to `userdata\updates`.
- [ ] Set `AddToRecent = !App.IsPortable` on the `OpenFileDialog` in `MainWindow` and the `FolderBrowserDialog`s in `AddFolderDialog` and `SettingsDialog`.
- [ ] tests for `PortableTempFolder`: two process IDs get separate folders, `RemoveStale` keeps a running process's folder and removes a dead one's, and a locked file is skipped without failing
- [ ] validation commands pass

### Task 3: Install portable updates in place

#### Files

- Create: `src/Sic/Services/PortableUpdateInstaller.cs`
- Create: `tests/Sic.Tests/PortableUpdateInstallerTests.cs`
- Modify: `src/Sic/Utils/Constants/App.cs`

#### Steps

- [ ] Add `App.ExecutableName = "Sic.exe"`.
- [ ] Add `PortableUpdateInstaller.Apply(zipPath, appFolder)`, `CleanUp(appFolder)` and `IsFolderWritable(folder)` following the flow under Technical details. It takes folders as arguments, shows no UI and starts no process, so tests can use temporary folders.
- [ ] tests for success, with fixtures using the real `Sic.exe` casing: new and changed files land in place, files are replaced in subfolders, old files end up with the `.sic-old` suffix, and `Sic.exe` is replaced last
- [ ] tests for protection: `userdata` is never touched, even if the ZIP contains files under it
- [ ] tests for bad archives: a path escaping the folder (zip slip), an absolute path, a ZIP with no `Sic.exe`, and a corrupt ZIP. Each leaves the app folder unchanged. A lowercase `sic.exe` is accepted.
- [ ] tests for rollback: a target file that can't be moved, because a test holds it open with no sharing, restores everything
- [ ] tests for leftovers: an existing `.sic-old` is deleted before renaming, and one that's locked fails safely through rollback
- [ ] tests for `CleanUp`: after a complete update it removes `.sic-old` files and `userdata\updates`; after an interrupted update it restores the old files; a user's own `.old` file survives; a locked file is skipped
- [ ] tests for `IsFolderWritable`: a normal folder, and a missing one
- [ ] validation commands pass

### Task 4: Ask about the queue before an update starts

#### Files

- Modify: `src/Sic/Services/UpdateService.cs`
- Modify: `src/Sic/MainWindow.cs`
- Create: `tests/Sic.Tests/UpdateExitTests.cs`

#### Steps

- [ ] Move the question from `MainWindow_FormClosing` into a private `ConfirmExitWithQueue()` method, keeping its strings.
- [ ] Add `public Func<bool>? ConfirmExit { get; set; }` and `public bool IsExitingForUpdate { get; private set; }` to `UpdateService`. Subscribe to `PreparingToExit`: cancel when `ConfirmExit` returns false, otherwise set `IsExitingForUpdate`. Put the decision in an internal method that tests can call.
- [ ] `MainWindow` sets `ConfirmExit`, and `MainWindow_FormClosing` skips its question when `_updateService?.IsExitingForUpdate` is true. This also covers installed copies, which today can be left running after the installer starts.
- [ ] tests for that method: true sets the flag, false cancels and leaves it clear, and a null `ConfirmExit` proceeds
- [ ] validation commands pass

### Task 5: Use the portable updater in portable mode

#### Files

- Create: `src/Sic/Services/PortableSparkleUpdater.cs`
- Modify: `src/Sic/Services/UpdateService.cs`
- Modify: `src/Sic/Utils/Constants/App.cs`
- Modify: `src/Sic/Program.cs`

#### Steps

- [ ] Add `App.PortableAppcastUrl = "https://sic.oire.dev/appcast-portable.xml"`.
- [ ] Add `PortableSparkleUpdater: SparkleUpdater`, overriding `RunDownloadedInstaller`. It checks `IsFolderWritable`, calls `PortableUpdateInstaller.Apply`, disposes `Program.InstanceGuard`, starts the new `Sic.exe`, then calls `await QuitApplication()`. The guard must go first, or the new process finds the mutex held and exits. This never writes the `.cmd` file NetSparkle puts in `%TEMP%`.
- [ ] If the folder isn't writable, for example `userdata` was added to an installed copy in Program Files, say so and point to the portable ZIP, without changing anything.
- [ ] On a failed `Apply`, log it and show a message through `DialogHelper.Show` saying the current version still works and where to download the new one. If the swap worked but starting the new version failed, tell the user to start SIC! again.
- [ ] In `UpdateService`, pick the class, appcast URL and a `.zip` value for `TmpDownloadFileNameWithExtension` from `App.IsPortable`, sharing everything else. Both must keep `SecurityMode.Strict`.
- [ ] Call `PortableUpdateInstaller.CleanUp` first thing on the GUI path in portable mode, before anything loads Magick's native library.
- [ ] validation commands pass

### Task 6: Publish a portable appcast

#### Files

- Modify: `installer/build-installer.ps1`

#### Steps

- [ ] After the installer appcast, run `netsparkle-generate-appcast` again with `--single-file $PortableZip`, producing `appcast-portable.xml` and `appcast-portable.xml.signature` in `$OutputDir`. Use `--output-file-name` if the installed version has it; otherwise generate into a separate folder and copy the files. Confirm the tool accepts a `.zip` with these arguments.
- [ ] Keep this step after the ZIP is built and after Authenticode signing, so the Ed25519 signature covers the final ZIP.
- [ ] With `-NoPortable`, skip this step, so existing portable copies aren't offered a ZIP that doesn't exist.
- [ ] Add both files to the `-Deploy` upload list and to the "files to upload" summary.
- [ ] Run the generator against a local test ZIP into a scratch folder, with throwaway keys rather than `keys/`, and confirm the enclosure points to the ZIP with `sparkle:os="windows"` and a signature.
- [ ] validation commands pass

### Task 7: Translate the new strings

#### Files

- Modify: `src/Sic/locale/messages.pot`
- Modify: `src/Sic/locale/{de,es,fr,he,ru,uk}/Sic.po`

#### Steps

- [ ] Run `Extract-Strings.ps1`, then `Update-Translations.ps1`.
- [ ] Translate every new string in all six languages, and remove the `#, fuzzy` flag from each new entry.
- [ ] Run `Compile-Translations.ps1`.
- [ ] validation commands pass, including `TranslationCatalogTests`

### Task 8: Update the manuals and documentation

#### Files

- Modify: `src/Sic/help/{en,de,es,fr,he,ru,uk}/manual.html`
- Modify: `README.md`
- Modify: `CHANGELOG.md`
- Modify: `changelogs/1.2.0.0.md`
- Modify: `CLAUDE.md`

#### Steps

- [ ] All seven manuals: rewrite the portable mode text. SIC! itself writes nothing outside its folder, and Windows' own records of programs that have run are outside any app's control. Replace "without leaving traces". Word this to match what the Process Monitor check finds about file dialogs.
- [ ] All seven manuals, in Software updates: a portable copy updates its own folder, and copies older than 1.2 need the new ZIP downloaded by hand once.
- [ ] `README.md`, `CHANGELOG.md` and `changelogs/1.2.0.0.md`: describe portable updates, and tell existing portable users to download the 1.2 ZIP by hand once, because older copies still point at the installer.
- [ ] `CLAUDE.md`: state the rule that portable mode must not write outside its folder or to the registry, and document `updater.json`, the second appcast, `PortableUpdateInstaller` and `build-installer.ps1`'s new output.
- [ ] validation commands pass

## Technical details

### Portable update flow

`PortableUpdateInstaller.Apply(zipPath, appFolder)`:

1. Read the ZIP's entries before writing anything. Reject the archive if any entry is absolute or resolves outside `appFolder`, or if `App.ExecutableName` isn't at its root, ignoring case. Skip entries under `userdata/`, because the ZIP ships an empty one.
2. Extract into `userdata\updates\staging`. It's on the same volume as the app, so the moves below are renames.
3. For each staged file, with `Sic.exe` last: delete any existing `<name>.sic-old`, rename an existing target to `<name>.sic-old`, then move the new file in. Windows allows renaming a running exe and loaded DLLs but not overwriting them, which is why no helper process or script is needed.
4. On any failure, undo every rename and move in reverse order, delete the staging folder, and rethrow. The old version keeps working.
5. Delete the ZIP and the staging folder.

`CleanUp(appFolder)` runs at the next start:

- If `Sic.exe.sic-old` exists, the swap finished, because the exe goes last. Delete every `*.sic-old` file outside `userdata`, plus `userdata\updates`.
- If other `*.sic-old` files exist without it, the swap was interrupted. Move each one back over its target, then delete `userdata\updates`.
- A file that's still locked is skipped until the next start.

The `.sic-old` suffix is distinctive so that a user's own `.old` files are never touched. Files that the new version no longer ships stay in the folder, because deleting them would risk removing something the user put there.

### Exit order after a portable update

1. `Apply` succeeds.
2. `Program.InstanceGuard` is disposed, so the new process can acquire it.
3. The new `Sic.exe` starts.
4. `QuitApplication` closes the window. `IsExitingForUpdate` skips the queue question, which `PreparingToExit` already asked before any file changed.

### Why updater state is a separate file

`updater.json` is written by NetSparkle's own `JSONConfiguration`, which saves it whenever it likes. Keeping it apart from `sic.json` means NetSparkle can never overwrite the user's settings. It holds no settings: only the last check time and any skipped version.

### Intended behavior changes

- Installed copies keep updater state in `updater.json`, so a version someone chose to skip is offered once more.
- The queue question for an update now comes before the installer starts, in both editions.

### Limits

- Copies older than 1.2 already point at the installer appcast. They can't be fixed from here, so those users need the new ZIP by hand once.
- Windows itself records that a program ran, for example in Prefetch and UserAssist. No app can prevent that.

## Post-completion

- Check portable traces with Process Monitor from the command line, so the results come out as CSV rather than through its interface: `Procmon /Quiet /Minimized /BackingFile trace.pml`, then `Procmon /Terminate`, then `Procmon /OpenLog trace.pml /SaveAs trace.csv`. Look for `Sic.exe` writes outside the portable folder across adding, converting, checking for updates and updating.
- In that trace, check whether `AddToRecent = false` is enough. The shell may still write `ComDlg32\OpenSavePidlMRU` from inside the process. If it does, that's Windows' own record, and the manuals' wording must say so.
- Run a portable build and confirm the old registry key is gone and `userdata\updater.json` exists.
- Update a real 1.2 portable copy from a test appcast, to confirm that renaming the running `Sic.exe` and `Magick.Native-Q16-x64.dll` works and that the new version starts.
- Pull the power on that update partway through, or kill the process during the swap, and confirm the next start recovers.
- With images in the queue, answer No to the update's queue question, and confirm nothing changed.
- Update an installed copy, to confirm the installer path still works.
