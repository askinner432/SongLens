# Browsing Folders And Songs

## Folder Tree

The folder tree shows folders that contain visible `.song` files somewhere below them.
Per-song `History` folders are excluded so saved revisions do not appear as duplicate
catalog songs.

### Folder Tree Features

- `Expand All` opens all visible folders below the current songs root
- `Collapse All` closes child folders, leaves the root open, and scrolls back to the top
- Clicking a folder loads songs for that folder
- If a folder has subfolders, a single click also expands it
- Right-clicking a folder opens a context menu with actions such as:
  - `Reveal in Explorer`
  - `Expand Folder`
  - `Collapse Folder`
  - `Delete Folder`

### Folder Loading Behavior

- If search results are showing and you click a folder, SongLens exits search mode and loads that folder immediately
- If a root or selected folder effectively leads to one visible song, SongLens can load it automatically so the detail area is not blank
- Automatic loading is tuned so larger folders do not flood `Recently Viewed` with unwanted first-song selections

## SongLens View

SongLens View lists the songs for the selected folder, or the current search results when search mode is active.
History revisions are excluded from folder views, searches, and catalog exports.

### SongLens View Features

- Sort by clicking column headers
- Drag column headers horizontally to reorder them; SongLens remembers the order
- Resize columns and SongLens will remember those widths
- Double-click a song row to reveal it in Windows Explorer
- Right-click a song row to open actions such as:
  - `Open in Recommended App`
  - `Open in Alternate App`
  - `Listen to This Song` when a matching Mixdown audio file is available
  - `Rename Song`
  - `Reveal in Explorer`

### SongLens View Columns

Use the `Columns` button in the SongLens View toolbar to choose which columns are shown.

When an active date filter is configured to display its results in SongLens View,
`Reload Filtered View` returns directly to those results after you have opened an
individual folder. It re-evaluates the existing filter without reopening or changing
the filter dialog.

The musical-note column identifies songs with supported audio in their configured
mixdown folder. SongLens shows every supported file in that folder even when an older
mixdown name no longer matches a renamed song. Files that still match the current song
name are listed first, and the playback menu shows each file's modified date to help you
choose which one to open.

## Recently Viewed

`Recently Viewed` is available from the drop-down above SongLens View.

### How It Works

- Shows recently opened songs by song filename only
- Avoids filling the list with automatic first-song selections from larger folders
- Can add a song automatically when you intentionally open a folder that contains only one visible song
- Tracks the specific song you click when a folder contains multiple songs
