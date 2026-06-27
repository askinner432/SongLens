# SongLens Avalonia Investigation Plan

## Goal

This document defines the first practical plan for investigating a macOS-capable SongLens port using Avalonia.

The aim of the investigation is not to port the entire application immediately. The aim is to prove that the core SongLens value can work in a cross-platform UI with a small, focused prototype.

## Recommended Investigation Outcome

At the end of the first investigation spike, we should be able to answer:

- Can SongLens read and display `.song` files cleanly in Avalonia?
- How much of the current code can move into a shared `Core` project?
- How comfortable does the folder tree and grid-style UI feel in Avalonia?
- Is a full cross-platform port worth continuing?

## Recommended Project Shape

The cleanest direction is:

- Keep the current WinForms app intact.
- Extract shared logic into a new project.
- Build a new Avalonia front end beside it.

Suggested future structure:

- `src/SongMetainfoBrowser.Core/`
- `src/SongMetainfoBrowser.App/` for the existing WinForms app
- `src/SongMetainfoBrowser.Avalonia/`

## File Buckets

### Strong Candidates For `Core`

These files are primarily models, parsing logic, formatting helpers, or configuration logic and should be good first candidates to move or duplicate into a shared library:

- `src/SongMetainfoBrowser.App/SongMetadata.cs`
- `src/SongMetainfoBrowser.App/SongMetadataReader.cs`
- `src/SongMetainfoBrowser.App/AdvancedSearchModels.cs`
- `src/SongMetainfoBrowser.App/BrowserConfig.cs`
- `src/SongMetainfoBrowser.App/AppPaths.cs`
- `src/SongMetainfoBrowser.App/AppInfo.cs`
- `src/SongMetainfoBrowser.App/DateTimeDisplay.cs`
- `src/SongMetainfoBrowser.App/DiagnosticLog.cs`
- `src/SongMetainfoBrowser.App/SongGeneratorDisplay.cs`
- `src/SongMetainfoBrowser.App/SongLaunchResolver.cs`
- `src/SongMetainfoBrowser.App/CsvExportField.cs`
- `src/SongMetainfoBrowser.App/SnapshotModels.cs`
- `src/SongMetainfoBrowser.App/SongGridColumnField.cs`

Notes:

- `SongLaunchResolver.cs` is structurally reusable, but some launch-path behavior is still Windows-specific.
- `BrowserConfig.cs` is reusable, though some UI-specific saved state may eventually be split from core settings.

### Likely Shared Later, But Not Required For The First Spike

These files may be useful in a later phase, but they are not necessary to prove the first Avalonia prototype:

- `src/SongMetainfoBrowser.App/AppTheme.cs`
- `src/SongMetainfoBrowser.App/AppThemes.cs`
- `src/SongMetainfoBrowser.App/AppFontSettings.cs`

These are still valuable, but the first spike should not try to fully recreate WinForms theme and font customization behavior.

### WinForms-Only UI

These files should be treated as Windows-UI-specific and replaced rather than ported line-by-line:

- `src/SongMetainfoBrowser.App/MainForm.cs`
- `src/SongMetainfoBrowser.App/MainForm.Diagnostics.cs`
- `src/SongMetainfoBrowser.App/PreferencesForm.cs`
- `src/SongMetainfoBrowser.App/AdvancedSearchForm.cs`
- `src/SongMetainfoBrowser.App/HistoryForm.cs`
- `src/SongMetainfoBrowser.App/HelpForm.cs`
- `src/SongMetainfoBrowser.App/AboutForm.cs`
- `src/SongMetainfoBrowser.App/SongAgeFilterForm.cs`
- `src/SongMetainfoBrowser.App/SongGridColumnsForm.cs`
- `src/SongMetainfoBrowser.App/CsvExportOptionsForm.cs`
- `src/SongMetainfoBrowser.App/CsvExportScopeForm.cs`
- `src/SongMetainfoBrowser.App/FontSettingsForm.cs`
- `src/SongMetainfoBrowser.App/RenameSongForm.cs`
- `src/SongMetainfoBrowser.App/SnapshotOptionsForm.cs`
- `src/SongMetainfoBrowser.App/SnapshotPreviewForm.cs`
- `src/SongMetainfoBrowser.App/ThemedConfirmationForm.cs`
- `src/SongMetainfoBrowser.App/ThemedMessageForm.cs`
- `src/SongMetainfoBrowser.App/ThemedTextPromptForm.cs`
- `src/SongMetainfoBrowser.App/Program.cs`
- `src/SongMetainfoBrowser.App/ThemeColorTable.cs`
- `src/SongMetainfoBrowser.App/ThemeMenuRenderer.cs`
- `src/SongMetainfoBrowser.App/WindowsShell.cs`

## First Avalonia Prototype Scope

The first prototype should stay intentionally narrow.

### Include

- a basic main window
- choose songs root folder
- load and scan `.song` files
- show a simple folder tree
- show a song list
- show summary details for the selected song
- basic fast keyword search

### Do Not Include Yet

- advanced search
- saved searches
- history management
- song launching into DAWs
- full theme parity
- full preferences parity
- CSV export polish
- snapshot workflow
- every custom dialog

The first spike should answer “can the app work?” before asking “can it match every feature?”

## Suggested First Milestone

Build a very small Avalonia shell that can:

1. choose a root folder
2. recursively scan visible `.song` files
3. show folders on the left
4. show songs in a list/grid
5. show summary metadata on the right

If that works well, the investigation is already a success.

## Suggested Second Milestone

After the first milestone works:

1. move fast search into the Avalonia UI
2. add one detail tab beyond summary
3. confirm config persistence works in a cross-platform-safe path
4. validate that performance feels acceptable on a medium library

## Recommended Migration Order

### Step 1: Create `Core`

Start by creating a shared library and moving or copying the easiest reusable pieces first:

- `SongMetadata.cs`
- `SongMetadataReader.cs`
- `AdvancedSearchModels.cs`
- `DateTimeDisplay.cs`
- `SongGeneratorDisplay.cs`
- `AppPaths.cs`
- `AppInfo.cs`

Then resolve any namespace or project-reference issues.

### Step 2: Introduce A Thin Service Layer

Before building much UI, it may help to add a couple of small service abstractions:

- folder scanning service
- config service
- song search service

This is not strictly required, but it will make both front ends easier to reason about.

### Step 3: Create Avalonia App Shell

Start with:

- app entry point
- main window
- root folder picker
- simple layout placeholders

### Step 4: Add Read/Display Loop

Wire:

- folder selection
- file scanning
- metadata reading
- list selection
- summary detail rendering

This is the core proof-of-value loop.

## Risks To Watch Early

### 1. Tree And Grid Feel

SongLens depends heavily on a pleasant folder tree and song-grid experience. Avalonia can do both, but we need to validate the feel early.

### 2. Performance

The current app reads songs directly from archives and displays the results pragmatically. If Avalonia bindings or view-model structure add too much overhead, we want to find that out early.

### 3. Platform-Specific File Integration

macOS folder picking, Finder reveal, and executable launching should be treated as separate integration tasks, not assumed to work like Windows.

## Suggested “Definition Of Success” For The Spike

The investigation should be considered successful if:

- the prototype runs on Windows and macOS
- it can open a songs folder
- it can display folders and songs
- it can show summary metadata for a selected song
- the experience feels credible enough to justify continuing

## Suggested “Stop Condition”

The investigation should pause if:

- the tree/list interaction feels fundamentally awkward in Avalonia
- the UI complexity is clearly much higher than expected
- the performance cost is too high for real libraries

If any of those happen, it is better to learn that during the spike than halfway through a full port.

## First Concrete Work Items

Recommended order for the next actual coding steps:

1. Create `src/SongMetainfoBrowser.Core/`
2. Move the core models/readers/helpers into it
3. Update the existing WinForms app to reference `Core`
4. Create `src/SongMetainfoBrowser.Avalonia/`
5. Build the smallest working browse-and-display prototype

## Recommendation

Do not start by trying to recreate all of SongLens.

Start by proving:

- core logic reuse
- song scanning
- folder browsing
- song selection
- summary display

If those five pieces work well, the path to a real macOS version becomes much clearer.
