# SongLens 1.1.1

## Song Compatibility

- Recovered songs containing Studio One or Fender Studio Pro internal XML element names that use invalid C++-style double colons.
- Kept songs visible when optional internal XML cannot be fully read, while preserving all metadata that remains available.
- Added a selected-song status warning when SongLens repairs malformed XML or loads limited metadata.
- Added a local diagnostic-log viewer with Copy All and Open Folder actions.

## History

- Added Ctrl/Shift multi-selection to the History window.
- Delete now removes only the selected history files when a selection exists.
- Kept the existing delete-all confirmation when no history files are selected.
- Added selection-aware confirmation text for one or multiple files.

## Release Assets

- Portable: `SongLens-1.1.1-portable-win-x64.zip`
- Installer: `SongLens-Setup-1.1.1.exe`

## SHA-256

- Portable: `447E80510A8EDA4A44F229DDA2CA1D45963046E26298D9866EB16EE85F1D0A3C`
- Installer: `A0C548B6063F3AE3150DC14A968572AA214CD6093CD0C00BC1E10AD9259D4A56`
