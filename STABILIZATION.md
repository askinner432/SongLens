# SongLens Stabilization Checklist

Use this checklist when shifting from feature work into release hardening.

## Core App

- Launch with no saved config and confirm the first-run folder prompt appears.
- Choose a valid songs folder and confirm the folder tree populates correctly.
- Click several folders and confirm songs load only for the selected folder.
- Double-click a song and confirm Windows Explorer opens with the file selected.
- Search for title, artist, tempo, notes, and comment terms.
- Clear search and confirm normal folder browsing returns cleanly.
- Test `Expand` and `Collapse` on a deeply nested songs tree.
- Confirm `Refresh` resets the current view the way you expect.

## Details Views

- Check Summary values on a few known songs.
- Check Attributes formatting, especially `Media:Tempo` and `Media:Length`.
- Check the Tracks tab column sizing, wrapping, and track notes.
- Check the Notes tab for songs with and without `notes.txt`.
- Check the History dialog on songs with and without History files.
- Confirm date and time display follows Windows regional settings.

## Persistence

- Change theme and restart the app.
- Resize grid columns and restart the app.
- Reopen with a saved root path and confirm it restores correctly.

## Installer And Release

- Install to `Program Files`.
- Launch from the Start Menu and from the installed `.exe`.
- Uninstall and confirm it removes the app cleanly.
- Test the portable single-file `.exe`.
- Rebuild the release zip and installer after final changes.

## Repo And Public Release

- Confirm `song-metainfo-browser.config.json` is sanitized before push.
- Confirm `README.md` matches the current app behavior.
- Confirm `git status` is clean before updating GitHub.
- Update GitHub release assets if the installer or portable build changed.

## Final Sanity Check

- Ask: "If someone downloads this today, will anything confuse them in the first five minutes?"
