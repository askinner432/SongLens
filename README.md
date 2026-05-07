# SongLens

SongLens is a Windows desktop app for browsing Studio One `.song` files and reading structured `metainfo.xml` data without opening each project in Studio One.

The app runs locally with built-in .NET and PowerShell support. No network access or external services are required.

## Main App

Use `Start SongLens.bat` to launch the main C# Windows app.

On first launch, if no songs folder has been saved yet, SongLens will prompt you to choose your Studio One songs folder.

Features:

```text
folder tree
song grid
search
summary details
raw metainfo.xml attributes
track details
history viewer
autosaved file filtering
light and dark themes
```

## Requirements

- Windows
- .NET SDK / runtime with Windows Forms support

## Build And Run

From the repo root:

```powershell
dotnet build .\src\SongMetainfoBrowser.App\SongMetainfoBrowser.App.csproj -o .\app
.\Start SongLens.bat
```

## Repository Layout

- `src/SongMetainfoBrowser.App/` is the main WinForms application
- `tools/` contains PowerShell-based helper and prototype utilities
- `song-metainfo-browser.config.json` stores the saved songs folder and selected theme

## Legacy PowerShell Tools

These are still included, but the C# app is the primary experience:

- `Start Song Metainfo GUI.bat` launches the older PowerShell GUI prototype
- `Start Song Metainfo Browser.bat` launches the PowerShell folder browser

## Read One Song File

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ".\tools\read-song-metainfo.ps1" "C:\path\to\sample.song"
```

Add `-Json` to emit structured JSON.
