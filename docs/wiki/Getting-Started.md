# Getting Started

## Quick Start

1. Launch SongLens.
2. Choose your Studio One songs folder if SongLens does not already have one saved.
3. Use the folder tree on the left to browse folders that contain visible `.song` files.
4. Click a folder to load its songs into the song grid.
5. Click a song to load its details into the tabs below.
6. Double-click a song row to reveal the file in Windows Explorer, or right-click it for song actions.

## Opening Songs In Compatible Apps

Right-click a song and choose `Open in Recommended App` to launch it in the app it was last saved with.

If an alternate compatible app is available, SongLens can also show `Open in Alternate App`.

The default behavior is configured so launching the recommended app is available by default.

## What SongLens Reads

Studio One `.song` files are zip archives. SongLens reads internal files such as:

- `metainfo.xml`
- `Song/song.xml`
- `Devices/musictrackdevice.xml`
- `Devices/audiosynthfolder.xml`
- `notepad.xml`
- `notes.txt`

These sources are used to assemble the Summary, Attributes, Tracks, Groups, Mixer, Notes, and search data.
