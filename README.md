# SongLens

SongLens is a Windows desktop app for browsing Studio One `.song` files and reading structured `metainfo.xml` data without opening each project in Studio One.

The app runs locally on Windows. No network access or external services are required.

## What It Does

- Browse a Studio One songs folder with a filtered folder tree
- Search across filename, title, artist, year, tempo, key, comments, and notes
- View summary metadata, raw `metainfo.xml` attributes, track details, and history
- Export the current song list view to `.csv` with selectable fields
- Read song notes from `notes.txt`
- Read track notes from `notepad.xml`
- Hide autosaved `.song` files from the main browsing experience
- Save theme choice, songs folder, and custom grid column widths

## Release Compatibility

- Supported target: Windows 11 x64
- Packaged releases are published as self-contained `win-x64` builds, so end users should not need to install .NET separately.
- Windows 11 ARM devices may work through x64 emulation, but that is not the primary validated target.
- Studio One itself is not required to inspect `.song` files, but users do need access to a valid songs folder.

## Developer Requirements

- Windows
- .NET SDK / runtime with Windows Forms support

## Launch The App

For packaged releases, launch `SongLens.exe` directly or use the installed Start Menu shortcut.

Use `Start SongLens.bat` to build and launch the main C# Windows app.

On first launch, if no songs folder has been saved yet, SongLens prompts you to choose your Studio One songs folder.

## Build And Run

From the repo root:

```powershell
dotnet build .\src\SongMetainfoBrowser.App\SongMetainfoBrowser.App.csproj -o .\app
.\Start SongLens.bat
```

## How SongLens Reads A `.song` File

Studio One `.song` files are zip archives. SongLens reads several internal files to assemble the UI:

- `metainfo.xml` for the main summary fields and raw attribute list
- `Song/song.xml` for track structure and music part information
- `Devices/musictrackdevice.xml` and `Devices/audiosynthfolder.xml` for track-to-instrument mapping
- `notepad.xml` for track notes
- `notes.txt` for song-level notes

The main archive parsing logic lives in `src/SongMetainfoBrowser.App/SongMetadataReader.cs`.

## Code Tour

If you want to read through the codebase, this is the easiest order:

1. `src/SongMetainfoBrowser.App/Program.cs`
2. `src/SongMetainfoBrowser.App/MainForm.cs`
3. `src/SongMetainfoBrowser.App/SongMetadataReader.cs`
4. `src/SongMetainfoBrowser.App/SongMetadata.cs`
5. `src/SongMetainfoBrowser.App/BrowserConfig.cs`
6. `src/SongMetainfoBrowser.App/HistoryForm.cs`

There is also a longer walkthrough in `docs/CODEBASE.md`.

## Repository Layout

- `src/SongMetainfoBrowser.App/` is the main WinForms application
- `installer/` contains the Inno Setup installer script
- `tools/` contains PowerShell-based helper utilities
- `docs/` contains project documentation for contributors
- `song-metainfo-browser.config.json` is a repo-local fallback config used mainly during development

## Config And Data Storage

Installed builds store writable app data under:

```text
%LocalAppData%\SongLens
```

That folder holds:

- `song-metainfo-browser.config.json`
- `startup.log`
- `startup-error.log`

During development, SongLens can still discover a repo-local `song-metainfo-browser.config.json` and migrate naturally to the user-data location.

## Themes, Layout, And UI State

SongLens remembers:

- the selected songs root folder
- dark or light theme
- custom grid column widths

Dates and times are shown with the current Windows regional settings.

## Legacy PowerShell Tools

These helper scripts are still included, but the WinForms app is the primary experience:

- `tools\song-metainfo-gui.ps1` is the older PowerShell GUI prototype
- `tools\browse-song-metainfo.ps1` is the PowerShell folder browser

## Read One Song File

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\tools\read-song-metainfo.ps1" "C:\path\to\sample.song"
```

Add `-Json` to emit structured JSON.

## Release Packaging

This repo includes helper scripts for public builds:

- `Publish SongLens Release.bat` builds the portable single-file release zip
- `Build SongLens Installer.bat` builds the Windows installer with Inno Setup

## Release Notes Guidance

If you are publishing SongLens publicly, the safest compatibility wording is:

`SongLens is supported on Windows 11 x64.`

Avoid claiming that it will run on every Windows 11 machine without exception, because ARM devices, SmartScreen policy, admin restrictions, or unusual local file-permission issues can still affect launch behavior.
