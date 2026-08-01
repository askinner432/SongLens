@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%tools\publish-songlens-wiki.ps1"

if errorlevel 1 (
  echo.
  echo Wiki publishing failed.
  pause
  exit /b 1
)

echo.
pause
