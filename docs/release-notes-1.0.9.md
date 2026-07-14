# SongLens 1.0.9

## Compatibility Fix

- Restored support for Studio/Fender song XML containing HTML-style named entities such as `&copy;` and `&nbsp;`.
- Prevented a single named entity in archived song metadata from causing the entire song to be skipped.
- Preserved unknown named entities as readable text while leaving standard XML and numeric entities intact.

## Release Assets

- Portable: `SongLens-1.0.9-portable-win-x64.zip`
- Installer: `SongLens-Setup-1.0.9.exe`
