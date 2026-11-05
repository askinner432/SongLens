# SongLens 2.0.0

SongLens 2.0 adds workflow organization and catalog analysis features, and replaces the raw Attributes tab with a plug-in manifest.

## Workflow Organization

- Added configurable per-song Status and Genre fields, star ratings, and custom text columns for workflow-specific information.
- Added Bulk Edit for assigning Status, Genre, and ratings across songs in the current view.
- Added saved SongLens Views that remember visible columns, order and widths, sort levels, and active searches. A saved view can include the active date filter.

## Search And Analysis

- Expanded Advanced Search with Status, Genre, and Rating rules and sorting across up to four fields. Saved Searches retain their sort levels.
- Added Statistics for distributions in the current view, including table and chart views, plus single-song comparisons with the full catalog.
- Replaced the raw Attributes tab with an alphabetical plug-in manifest showing device categories and preset formats when available.

## Documentation

- Added an illustrated user guide and updated the wiki and in-app help for the new workflow and analysis features.

## Updates To This 2.0.0 Build

- Bulk Edit now applies Status, Genre, and Rating together with one Apply button and one confirmation dialog. The confirmation can be suppressed from that dialog.
- Removed the Bulk Edit folder-deletion action while its folder-matching rules are being reconsidered.
- Help editing controls are hidden by default. Set `EnableHelpEditing` to `true` in the app configuration to show them.

## Release Assets

- Portable: `SongLens-2.0.0-portable-win-x64.zip`
- Installer: `SongLens-Setup-2.0.0.exe`

## SHA-256

- Portable: `DBCFCFF84C99EC2E4CF373AEC9546FB4D20B0E5D5C471875D80823384A9D38CA`
- Installer: `F122DE53D1BFA330B135166C303D6A6F3BBE9367FBA2C04E86A9DEAA5680F738`
