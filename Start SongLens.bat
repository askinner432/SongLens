@echo off
set "SCRIPT_DIR=%~dp0"
set "APP_DLL=%SCRIPT_DIR%app\SongLens.dll"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_CLI_HOME=%SCRIPT_DIR%"

if not exist "%APP_DLL%" (
  echo The C# app has not been built yet.
  echo Building now...
)

echo Building C# app...
dotnet build "%SCRIPT_DIR%src\SongMetainfoBrowser.App\SongMetainfoBrowser.App.csproj" -o "%SCRIPT_DIR%app"
if errorlevel 1 (
  pause
  exit /b 1
)

if exist "%APP_DLL%" (
  dotnet "%APP_DLL%"
  echo.
  echo SongLens closed.
  pause
) else (
  echo Could not find the app:
  echo %APP_DLL%
  pause
)
