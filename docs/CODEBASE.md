# SongLens Codebase Guide

This file is meant for contributors and for future-you when you want to re-enter the project quickly.

## Big Picture

SongLens is a WinForms desktop app that scans a Studio One songs folder, reads `.song` archives, and presents the extracted metadata in a searchable UI.

There are three main concerns in the code:

1. UI layout and interaction
2. `.song` archive parsing
3. local configuration and packaging

## Recommended Reading Order

If you want to understand the app without bouncing around too much, read the files in this order:

1. `src/SongMetainfoBrowser.App/Program.cs`
This is the application entry point. It sets the Windows shell AppUserModelID, writes startup logs, and launches the main form.

2. `src/SongMetainfoBrowser.App/MainForm.cs`
This is the core of the app. It builds the UI, loads folders, runs search, displays metadata, and coordinates the theme and dialogs.

3. `src/SongMetainfoBrowser.App/SongMetadataReader.cs`
This is the parsing layer. It opens `.song` files as zip archives and reads the internal XML and text files needed for display.

4. `src/SongMetainfoBrowser.App/SongMetadata.cs`
These are the lightweight data models that move parsed information into the UI.

5. `src/SongMetainfoBrowser.App/BrowserConfig.cs`
This is where saved state lives: root path, theme, and persisted grid column widths.

6. `src/SongMetainfoBrowser.App/HistoryForm.cs`
This dialog reads and manages the `History` subfolder for the selected song.

## Data Flow

The main path through the app looks like this:

1. `Program.Main()` starts the app and opens `MainForm`
2. `MainForm` loads the saved root folder from `BrowserConfigStore`
3. the folder tree is built from directories that contain visible `.song` files
4. selecting a folder loads the `.song` files directly under that folder
5. `SongMetadataReader.Read()` parses each `.song` file into a `SongMetadata` object
6. `MainForm.ShowMetadataDetails()` pushes that data into the Summary, Attributes, Tracks, Notes, and History views

## What Is Inside A `.song` File

Studio One `.song` files are zip archives. SongLens reads these internal files:

- `metainfo.xml`
Primary metadata source. This drives the Summary and Attributes tabs.

- `Song/song.xml`
Track structure, media track names, and music parts.

- `Devices/musictrackdevice.xml`
Maps music track channels to device routing.

- `Devices/audiosynthfolder.xml`
Maps device IDs to human-readable instrument names.

- `notepad.xml`
Track-level notes.

- `notes.txt`
Song-level notes.

The parser normalizes some XML before reading because some Studio One files use an undeclared `x:` prefix.

## Key UI Concepts

### MainForm

`MainForm` owns:

- the songs folder path
- search mode vs. normal folder mode
- the folder tree and song grid
- the detail tabs
- theme application
- grid width persistence

It is intentionally a broad coordinator class. If the app keeps growing, this would be the first place to split into smaller UI-focused helpers.

### Search

Search is global beneath the current root folder, not just the selected tree node. A search result stores both the metadata and the matched field so the grid can show useful context.

### History

The History tab is implemented as a modal dialog instead of an embedded grid. Selecting the tab opens `HistoryForm`, then returns the user to the previous detail tab.

## Configuration And Writable Data

Installed builds should write to:

```text
%LocalAppData%\SongLens
```

That path is defined in `AppPaths.cs`.

The app stores:

- `song-metainfo-browser.config.json`
- `startup.log`
- `startup-error.log`

`BrowserConfigStore.FindConfigPath()` also looks upward from the executable directory so the repo-local config file can still be found during development.

## Theme System

The theme model is intentionally simple:

- `AppTheme.cs` defines the palette object
- `AppThemes.cs` defines the dark and light presets
- `ThemeColorTable.cs` and `ThemeMenuRenderer.cs` style the menu system

Most theme application happens in `MainForm.ApplyTheme()` and `HistoryForm.ApplyTheme()`.

## Release And Installer Files

- `Publish SongLens Release.bat`
Builds the single-file portable release asset.

- `Build SongLens Installer.bat`
Builds the installer after publishing the app.

- `installer/SongLens.iss`
Inno Setup script for the Windows installer.

## Good First Places To Modify

If you want to make a focused change, these entry points are usually the right ones:

- add or reformat metadata fields:
`SongMetadataReader.cs` and `MainForm.ShowMetadataDetails()`

- change theme colors:
`AppThemes.cs`

- adjust grid layout or toolbar behavior:
`MainForm.cs`

- change config behavior:
`BrowserConfig.cs` and `AppPaths.cs`

- change installer behavior:
`installer/SongLens.iss`

## Known Structural Tradeoffs

- `MainForm.cs` is intentionally central and fairly large.
- parsing is direct and pragmatic rather than abstracted behind interfaces.
- config naming still uses the legacy filename `song-metainfo-browser.config.json` for compatibility.

Those choices keep the app simple, but they are good candidates for future refactoring if the project grows much further.
