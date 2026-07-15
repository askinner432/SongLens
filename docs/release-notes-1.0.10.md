# SongLens 1.0.10

## Compatibility Fixes

- Restored Track Note metadata from the original `NotepadItem` format while retaining support for the newer `Section` format.
- Prevented duplicate Track Note titles from rejecting an otherwise readable song.
- Made optional Mixer metadata best-effort so malformed Mixer XML no longer prevents Summary and Track metadata from loading.
- Added support for duplicate Mixer channel IDs without rejecting the song.

## Filtering and Search

- Added a **Filter By Date** button to the Song Grid toolbar.
- Made Fast Catalog Search search the entire catalog regardless of the active date filter.
- Made Rescan display catalog-wide filtered results only when both filter restoration and Song Grid filter results are enabled.
- Corrected filter-dialog state so its Song Grid option matches the displayed results.

## Song Grid and Navigation

- Added results snapshots for any Song Grid containing multiple songs while retaining individual snapshots for a single song.
- Prevented toolbar buttons and Snapshot labels from overlapping or briefly changing during Fast Catalog Search.
- Corrected Expand All and Collapse All so they retain their tree state and select and load the first folder below the root.

## Preferences and Help

- Moved Visible Tabs to **Tools > Preferences > Appearance > Adjust Visible Tabs**.
- Removed the obsolete View menu while retaining the funnel icon for Advanced Search.
- Updated Help for filtering, catalog-wide search, snapshots, and visible-tab preferences.

## Release Assets

- Portable: `SongLens-1.0.10-portable-win-x64.zip`
- Installer: `SongLens-Setup-1.0.10.exe`
