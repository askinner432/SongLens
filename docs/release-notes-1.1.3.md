# SongLens 1.1.3

## Mixdown Audio

- Added a configurable mixdown folder name under **Preferences > Audio**.
- SongLens now finds every supported audio file in the configured mixdown folder, including older mixdowns whose filenames no longer match a renamed song.
- Matching filenames remain first in the playback menu, and each choice now includes its modified date.
- Preferences identifies the application registered as the Windows default audio player when available.

## SongLens View

- Added drag-and-drop column reordering and automatic persistence of the chosen column order.
- Column visibility changes now preserve the user's chosen order.

## Diagnostics And Reliability

- Added **Selected Song** and **Entire Session** scopes to the diagnostic log window.
- Removed misleading warnings for Studio One XML names that SongLens successfully normalizes.
- Fixed the audio-player radio buttons so only one option can be selected at a time.

## Release Assets

- Portable: `SongLens-1.1.3-portable-win-x64.zip`
- Installer: `SongLens-Setup-1.1.3.exe`

## SHA-256

- Portable: `63A302B1735903FB899399E33D88917B0DDCD75C1A5EE8B3611602BE2415B72E`
- Installer: `11241938084A60A4DF75909D8C76D334C7D1D4D869C881C72AC4BE84DBE70996`
