# SongLens 1.0.7

## Highlights

- Added a new `Groups` tab after `Tracks` so group names and assigned track names are visible at a glance.
- Expanded the `Mixer` tab into three sections: `Main`, `Inserts`, and `Sends`.
- Split `Mixer Main` into `Pre` and `Post` chains where the song exposes both sections.
- Improved track instrument detection so SongLens more often shows the real routed instrument instead of repeating the track name.
- Moved `Recently Viewed` into the toolbar above the song grid for faster access.
- Added `View > Visible tabs...` so users can hide detail tabs they do not use often.

## Search, Filter, And Browsing

- Added a filter option for songs between two dates.
- Added a choice to filter on either `Modified` or `Created` dates.
- Refined Advanced Search with inline `Add Rule` and `Remove` controls plus clearer `Search`, `Clear`, and `Cancel` actions.
- Added Advanced Search result snapshots that preserve the visible song-grid columns, column order, sort order, and matching rows in Text or JSON.
- Improved folder-selection behavior so automatic first-song loading does not flood `Recently Viewed`.
- Preserved the convenience behavior where intentionally opening a folder with one visible song can still add that song to `Recently Viewed`.
- `Collapse All` now scrolls back to the top of the folder tree.

## Detail Views And Output

- Added `Groups` to Snapshot and CSV export options.
- Updated Snapshot and CSV output to reflect the newer mixer sections.
- Updated song snapshots so the `With Events` track filter also applies to Text and JSON output, with a clear `Tracks With Events` heading in text snapshots.
- Renamed generator-style displays to `Studio Version`.
- Removed redundant `Notes File` from the Summary tab.
- Cleaned up Attributes display by removing unwanted prefixes and hiding low-value fields.

## UI And Preferences

- Updated the song filter dialog to use radio-button choices and improved layout spacing.
- Changed theme selection in Preferences from a drop-down to radio buttons.
- Reorganized General preferences with descriptions and a dedicated `Startup Options` section.
- Moved song launch actions out of the File menu and kept them in the faster song right-click menu.
- Moved Snapshot and Change Columns controls into the song-grid toolbar, with `With Events` aligned to the detail-tab toolbar.
- Updated Preferences tabs to match the button styling more closely.
- Fixed theme refresh issues so controls such as `Snapshot` update correctly when switching themes.
- Fixed a brief startup flicker by completing split-pane layout before the main window is shown.
- Refreshed Help content and added a new multi-page GitHub wiki draft.

## Notes

- This release focuses on browseability, clarity, and reducing friction in everyday library review.
- Release assets should be rebuilt so the portable zip and Windows installer both carry version `1.0.7`.
