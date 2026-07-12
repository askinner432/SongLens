# Browsing Folders And Songs

## Folder Tree

The folder tree shows folders that contain visible `.song` files somewhere below them.

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

## Song Grid

The song grid lists the songs for the selected folder, or the current search results when search mode is active.

### Song Grid Features

- Sort by clicking column headers
- Resize columns and SongLens will remember those widths
- Double-click a song row to reveal it in Windows Explorer
- Right-click a song row to open actions such as:
  - `Open in Recommended App`
  - `Open in Alternate App`
  - `Rename Song`
  - `Reveal in Explorer`

### Song Grid Columns

Use the `Change Columns` button in the song-grid toolbar to choose which columns are shown in the song grid.

## Recently Viewed

`Recently Viewed` is available from the drop-down above the song grid.

### How It Works

- Shows recently opened songs by song filename only
- Avoids filling the list with automatic first-song selections from larger folders
- Can add a song automatically when you intentionally open a folder that contains only one visible song
- Tracks the specific song you click when a folder contains multiple songs
