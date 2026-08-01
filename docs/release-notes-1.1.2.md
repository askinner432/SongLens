# SongLens 1.1.2

## Mixdown Playback

- Added a musical-note column that identifies songs with matching audio files in their `Mixdown` folder.
- Added `Listen to This Song` to the song context menu, including format/version selection when several matching files exist.
- Added Playback preferences for the Windows default audio player or a custom player executable.
- Recognizes exact song names and numbered mixdowns with or without a space before the version number.

## Full Mixer

- Added a complete mixer window covering inputs, outputs, audio and instrument channels, buses, FX, auxes, VCAs, and the listen bus.
- Uses the saved Studio One console order and identifies channels hidden in the Studio One mixer.
- Shows channel format, input/output routing, VCA assignment, groups, volume, pan, detailed inserts, detailed sends, and channel state.
- Added a frozen expand/collapse column for folder tracks, including nested routed descendants.
- Added `Copy All` for a tab-separated mixer table with headers.

## Interface And Documentation

- Refined the Tracks and Mixer section headers and themed the song context menu.
- Added in-app help and wiki documentation for playback and Full Mixer features.
- Added a helper for publishing the repository wiki and removed obsolete release helpers and completed Avalonia investigation notes.

## Release Assets

- Portable: `SongLens-1.1.2-portable-win-x64.zip`
- Installer: `SongLens-Setup-1.1.2.exe`

## SHA-256

- Portable: `C449E491D56DBEE8FA6B7F494ADB4EE9936C93E1B769353D2F174DCCB1E76A1D`
- Installer: `153F02DF3CAA0E1F59A68571210C9C50C8D2B2928750395E2387689BF4EB6012`
