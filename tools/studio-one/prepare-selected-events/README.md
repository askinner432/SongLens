# Selected Events to Editor

This Studio One script adds a `Track > Selected Events to Editor` command that runs:

1. Select Tracks with Selected Events
2. Apply Track Names to Events
3. Apply Track Names to Channels
4. Apply Track Names to Instruments
5. Solo Selected Events
6. Assign Unique Track Colors
7. View > Editor

`View > Editor` is Studio One's command for the default `F2` shortcut.

The script resolves every command before doing any work. Navigation Essentials registers `Select Tracks with Selected Events` in `Scoring Tools`. The color step also checks `Scoring Tools` first, then falls back to the named Track/Macro command and Navigation Essentials' `Track > Colorize Tracks`. The Solo step accepts the named command/macro first, then falls back to Studio One's `Track > Solo` command.

Commands are submitted through Studio One's deferred-command queue so each action is processed in order. The immediate execution fallback is used only on host versions that do not expose `deferCommand`.

## Build

From PowerShell:

```powershell
.\build-package.ps1
```

## Install

Copy `EventsToEditor.package` into Studio One's `Scripts` folder and restart Studio One. The exact application folder name varies by installed version, for example:

```text
C:\Program Files\PreSonus\Studio One 7\Scripts\
```

The Navigation Essentials commands must already be installed. Assign `Selected Events to Editor` to a keyboard shortcut or Macro Toolbar button from Studio One's command list.
