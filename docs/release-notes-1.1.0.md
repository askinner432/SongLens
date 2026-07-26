# SongLens 1.1.0

## Exports

- Added Excel Workbook export with linked `Catalog` and `Mixer Details` worksheets.
- Added frozen headers and filters to both Excel worksheets.
- Kept CSV catalog exports to one row per song while writing selected mixer data to a normalized companion file.
- Added folder context, disabled tracks, track notes, song notes, Groups, and expanded Mixer data to export options.

## Snapshots

- Added a scope dialog for choosing the selected song or every song in the current folder or search result.
- Added List View and Detail View for multi-song snapshots.
- List View saves the current SongLens View layout and sort order directly.
- Detail View creates the full report for every song, with a clear separator between songs and combined Text or JSON output.
- Added snapshot-only Track View overrides for events, disabled tracks, and folder rows without changing the saved Track View settings.
- Improved snapshot labels, defaults, preview buttons, and theme consistency.

## SongLens View

- Corrected date sorting by sorting Date Created and Last Modified as dates rather than text.
- Kept the Song column visible and readable when columns are added.
- New columns now appear to the right without shifting existing columns out of view.
- Added a centered `SongLens View` heading and refined the compact toolbar layout.
- Added a visible loading gauge and status message while SongLens View is being built or searched.
- Standardized the browse and advanced-search button hover and pressed colors with the other toolbar buttons.
- Improved column selection, button sizing, colors, and mouseover behavior in both themes.

## Track View

- Added folder rows and hierarchy to the track list.
- Added disabled-state indicators and filters for events, disabled tracks, and folders.
- Kept Track View Options open while multiple settings are changed.
- Made unchecked menu boxes visible in both light and dark themes.

## Compatibility

- Retained support for HTML-style named entities in Studio One metadata, including `&copy;` and `&nbsp;`.
- Preserved the compatibility fixes from SongLens 1.0.10 for Track Notes and best-effort Mixer parsing.

## Release Assets

- Portable: `SongLens-1.1.0-portable-win-x64.zip`
- Installer: `SongLens-Setup-1.1.0.exe`

## SHA-256

- Portable: `B13C5F316F5ACED32188D9576D6E2A0E1508701A3F45908894AA55F1F328BC47`
- Installer: `823D59345877C3F6109FEDF48898B621454158C50987BA3038140E2BEE2238FA`
