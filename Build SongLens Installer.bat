@echo off
setlocal EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
set "PROJECT=%SCRIPT_DIR%src\SongMetainfoBrowser.App\SongMetainfoBrowser.App.csproj"
set "PUBLISH_DIR=%SCRIPT_DIR%release-build\single-file-win-x64"
set "INSTALLER_SCRIPT=%SCRIPT_DIR%installer\SongLens.iss"
set "INNO_EXE=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_CLI_HOME=%SCRIPT_DIR%"

if not exist "%INNO_EXE%" set "INNO_EXE=C:\Program Files\Inno Setup 6\ISCC.exe"

if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
mkdir "%PUBLISH_DIR%"

echo Publishing SongLens single-file build...
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%PUBLISH_DIR%"
if errorlevel 1 (
  echo.
  echo Publish failed.
  pause
  exit /b 1
)

if not exist "%PUBLISH_DIR%\SongLens.exe" (
  echo.
  echo Expected output was not found:
  echo %PUBLISH_DIR%\SongLens.exe
  pause
  exit /b 1
)

if not exist "!INNO_EXE!" (
  echo.
  echo Inno Setup 6 was not found.
  echo Install Inno Setup, then run this script again.
  echo Expected path:
  echo !INNO_EXE!
  pause
  exit /b 1
)

echo.
echo Building installer...
"!INNO_EXE!" "%INSTALLER_SCRIPT%"
if errorlevel 1 (
  echo.
  echo Installer build failed.
  pause
  exit /b 1
)

echo.
echo Installer ready in:
echo   %SCRIPT_DIR%installer-output
pause
