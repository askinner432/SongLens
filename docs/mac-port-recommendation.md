# SongLens macOS Port Recommendation

## Executive Summary

SongLens can be brought to macOS, but it is not a simple rebuild of the current application.

The current app is a Windows-only WinForms application targeting `net10.0-windows`. A macOS version would require a UI port. The strongest path is to keep the existing C# codebase, extract reusable logic into a shared core library, and build a new cross-platform UI using `Avalonia`.

My recommendation is:

- Keep the current WinForms app for Windows.
- Create a shared `Core` project for reusable logic.
- Build a new `Avalonia` front end for Windows and macOS.
- Treat the work as a phased port, not a quick compile-target change.

## Bottom Line

- Difficulty: Medium-high
- Risk: Manageable
- Recommended framework: Avalonia
- Estimated effort for a functional first macOS-capable build: 4-8 weeks
- Estimated effort for a polished cross-platform release: 8-12 weeks

These estimates assume focused development and depend on whether the first goal is:

- functional cross-platform support
- or near-feature-parity with the current Windows version

## Why This Is Not a Simple Port

SongLens is currently built directly on WinForms:

- `MainForm`
- `PreferencesForm`
- `AdvancedSearchForm`
- `HistoryForm`
- themed dialog forms
- WinForms menus, toolbars, status bars, and context menus
- `TreeView`
- `DataGridView`
- WinForms-specific dialogs like `FolderBrowserDialog`, `OpenFileDialog`, and `MessageBox`

That means the UI layer is tightly tied to Windows desktop APIs and control behavior.

## What Can Be Reused

A meaningful amount of the non-UI code is already portable and should be reused:

- song archive parsing and metadata extraction
- advanced search models and filtering rules
- browser configuration persistence
- song metadata models
- CSV export models
- snapshot models
- generator display normalization
- song launch resolution logic structure

Examples of likely reusable files:

- `src/SongMetainfoBrowser.App/SongMetadataReader.cs`
- `src/SongMetainfoBrowser.App/SongMetadata.cs`
- `src/SongMetainfoBrowser.App/AdvancedSearchModels.cs`
- `src/SongMetainfoBrowser.App/BrowserConfig.cs`
- `src/SongMetainfoBrowser.App/SongGeneratorDisplay.cs`
- `src/SongMetainfoBrowser.App/SongLaunchResolver.cs`

The core “read songs, search songs, persist settings” logic is the strongest foundation for a cross-platform version.

## What Would Need Rewriting

Most of the visual and interaction layer would need to be rebuilt:

- main window layout
- folder tree
- song grids
- detail tabs
- advanced search dialog
- preferences dialog
- history dialog
- themed confirmation/message/prompt dialogs
- menu and toolbar rendering
- status bar behavior
- custom tree and grid interaction behavior

This is why the work is best described as a UI rewrite on top of reusable logic.

## Biggest Porting Challenges

### 1. Grid-heavy UI

SongLens relies heavily on `DataGridView`. Avalonia can support data grids, but not as a direct drop-in replacement. Grid behavior, sorting, sizing, and theming would need to be recreated.

### 2. Folder tree behavior

The folder tree has a fair amount of custom interaction logic, including:

- single-click and toggle behavior
- custom expand/collapse rules
- owner-drawn visuals
- filtered visibility
- context menu actions

That logic will need to be ported intentionally.

### 3. Custom dialogs

SongLens uses a collection of custom themed dialogs. These would all need Avalonia equivalents.

### 4. Windows-specific helpers

Some helpers are Windows-specific and would need replacement or abstraction:

- `WindowsShell.cs`
- Explorer reveal behavior
- native file dialogs
- WinForms message dialogs
- Windows path and executable assumptions

### 5. App launching behavior

Recommended-app launching will need platform-aware handling on macOS, especially if launching `.app` bundles or using different install paths.

## Recommended Technical Approach

### Phase 1: Extract Shared Core

Create a shared project, for example:

- `SongMetainfoBrowser.Core`

Move reusable logic there:

- metadata reading
- search and filtering logic
- config models and persistence
- snapshot and export models
- generator/app resolution helpers

This lets the current Windows app continue to work while building a second UI safely.

### Phase 2: Build a New Avalonia Front End

Create a new project, for example:

- `SongMetainfoBrowser.Avalonia`

This project would replace the WinForms UI using Avalonia views and view-model style structure.

### Phase 3: Add Cross-Platform Integration

Once the main UI is functional:

- add macOS-friendly file dialogs
- add Finder reveal behavior
- adjust app-launch integration
- refine fonts, spacing, and theme behavior per platform

## Suggested Delivery Plan

### Phase 1: Functional Cross-Platform Build

Goal: get the app working on macOS and Windows with core value intact.

Suggested scope:

- root folder selection
- folder tree
- song list
- summary/details tabs
- fast keyword search
- advanced search
- preferences
- basic export/snapshot support

This phase aims for usefulness first, not complete parity.

### Phase 2: Feature Parity And Polish

Suggested scope:

- history management
- launch integration
- saved layouts and finer preferences
- advanced theming polish
- all custom dialogs
- exact behavior parity where it matters

## Estimated Effort

### Functional First Build

Estimated: 4-8 weeks

This assumes:

- shared logic extraction
- new Avalonia app shell
- core browse/search/details experience

### Polished Cross-Platform Release

Estimated: 8-12 weeks

This assumes:

- parity-level dialog coverage
- tuned tree/grid behavior
- platform-specific polishing
- broader testing on macOS and Windows

## Risk Assessment

### Low Risk

- song metadata parsing
- archive reading
- advanced search rules
- config serialization
- basic filtering and sorting logic

### Medium Risk

- grid behavior parity
- tree interaction parity
- theme translation
- platform-specific file dialogs

### Higher Risk

- recommended-app launch integration on macOS
- exact UI parity with the current WinForms feel

## Recommendation

If macOS demand remains strong, this port is worth serious consideration.

I would recommend:

1. Do not attempt an in-place WinForms conversion.
2. Extract shared logic first.
3. Build a new Avalonia UI beside the existing Windows app.
4. Deliver it in phases.

This is not the easiest project, but it is a very reasonable one. SongLens already has a strong core model and enough reusable logic that the port would be mostly a UI effort rather than a full rewrite of the product’s intelligence.

## Final Verdict

SongLens is a good candidate for an Avalonia-based macOS version.

It is not a quick port, but it is technically straightforward, strategically sensible, and likely the cleanest way to respond to repeated requests for Mac support.
