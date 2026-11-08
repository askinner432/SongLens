@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "PROJECT=%SCRIPT_DIR%src\SongMetainfoBrowser.App\SongMetainfoBrowser.App.csproj"
set "PUBLISH_DIR=%SCRIPT_DIR%release-build\single-file-win-x64"
set "ZIP_BASENAME=SongLens-2.0.0-portable-win-x64"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_CLI_HOME=%SCRIPT_DIR%"
for /f %%i in ('powershell -NoProfile -Command "[DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')"') do set "BUILD_TIMESTAMP=%%i"
if not defined BUILD_TIMESTAMP (
  echo.
  echo Could not create the release build timestamp.
  pause
  exit /b 1
)

if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
mkdir "%PUBLISH_DIR%"

echo Publishing SongLens single-file release build...
dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:BuildTimestamp="%BUILD_TIMESTAMP%" -o "%PUBLISH_DIR%"
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

if exist "%PUBLISH_DIR%\startup.log" del /q "%PUBLISH_DIR%\startup.log"
if exist "%PUBLISH_DIR%\startup-error.log" del /q "%PUBLISH_DIR%\startup-error.log"
if exist "%PUBLISH_DIR%\diagnostic.log" del /q "%PUBLISH_DIR%\diagnostic.log"
if exist "%PUBLISH_DIR%\SongLens.pdb" del /q "%PUBLISH_DIR%\SongLens.pdb"

echo.
echo Creating zip package...
if exist "%SCRIPT_DIR%%ZIP_BASENAME%.zip" del /q "%SCRIPT_DIR%%ZIP_BASENAME%.zip"
powershell -NoProfile -ExecutionPolicy Bypass -Command "Compress-Archive -Path '%PUBLISH_DIR%\SongLens.exe' -DestinationPath '%SCRIPT_DIR%%ZIP_BASENAME%.zip' -Force"
if errorlevel 1 (
  echo.
  echo Zip creation failed.
  pause
  exit /b 1
)

echo.
echo Release build ready:
echo   Folder: %PUBLISH_DIR%
echo   Zip:    %SCRIPT_DIR%%ZIP_BASENAME%.zip
pause
