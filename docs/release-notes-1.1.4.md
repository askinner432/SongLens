# SongLens 1.1.4

## History Cleanup And Revision Access

- Added explicit deletion marks to the History window, with **Select All**, **Clear All**, and a required review step before files are deleted.
- Added a dated confirmation list showing every History file that will be kept.
- Added **Preferences > History**, where SongLens can automatically leave a chosen number of the most recent revisions unmarked for deletion.
- Added a guarded History revision launcher. Select a revision, enable its load option, choose a compatible application, and use **Load this backup version**.
- Compatible revision launch targets are listed newest-first, with Fender Studio Pro 8 ahead of Studio One 7 when both are available.
- History revisions are excluded from the main catalog, searches, filtered views, and exports so backups no longer appear as duplicate songs.
- The active SongLens view now refreshes automatically after History cleanup.

## SongLens View And Filters

- Added **Reload Filtered View** to return directly to the current date-filter or all-songs results after browsing an individual folder.
- Fixed SongLens View columns so users can widen the Title column and other columns without an artificial fill-column limit.
- Added a bounded, file-change-aware metadata cache for faster repeat folder navigation.
- Removed the folder-loading header transition that caused **SongLens View** to flicker during fast navigation.

## Notes And Documentation

- Updated the empty Notes message to **The session note is empty**.
- Updated built-in Help and the GitHub wiki for the new History and filtered-view workflows.

## Release Assets

- Portable: `SongLens-1.1.4-portable-win-x64.zip`
- Installer: `SongLens-Setup-1.1.4.exe`

## SHA-256

- Portable: `36ECAF989F43FACFBBB00748C8FFC7EA951F394B24BDB9EE5FB78A9969C5ABB0`
- Installer: `CF2C5304BDD8BDB2DDE68EA4EEA5F1C9ADE959BDB2D3277C79C995C921488430`
