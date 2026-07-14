@echo off
setlocal

cd /d "%~dp0"

echo SongLens 1.0.9 release commit
echo.
echo This script stages only the 1.0.9 compatibility fix and release metadata.
echo Unrelated Avalonia, solution, config, and scratch changes are left unstaged.
echo.

git status --short
if errorlevel 1 goto :failed

echo.
choice /C YN /N /M "Stage the SongLens 1.0.9 release files? [Y/N] "
if errorlevel 2 goto :cancelled

git add -- ^
  "src/SongMetainfoBrowser.Core/SongMetadataReader.cs" ^
  "src/SongMetainfoBrowser.App/SongMetainfoBrowser.App.csproj" ^
  "installer/SongLens.iss" ^
  "Publish SongLens Release.bat" ^
  "docs/release-notes-1.0.9.md" ^
  "Commit SongLens 1.0.9 Release.bat"
if errorlevel 1 goto :failed

git diff --cached --check
if errorlevel 1 goto :failed

echo.
git diff --cached --stat
echo.
choice /C YN /N /M "Commit these files as SongLens 1.0.9? [Y/N] "
if errorlevel 2 goto :cancelled

git commit -m "Release SongLens 1.0.9"
if errorlevel 1 goto :failed

git tag -a v1.0.9 -m "SongLens 1.0.9"
if errorlevel 1 goto :failed

echo.
choice /C YN /N /M "Push this commit to origin/main and push tag v1.0.9? [Y/N] "
if errorlevel 2 goto :local_complete

git push origin HEAD:main
if errorlevel 1 goto :failed
git push origin v1.0.9
if errorlevel 1 goto :failed

echo.
echo SongLens 1.0.9 commit and tag were pushed successfully.
goto :done

:local_complete
echo.
echo Commit and tag were created locally but were not pushed.
goto :done

:cancelled
echo.
echo Cancelled. Review git status before continuing.
goto :done

:failed
echo.
echo The release Git operation failed. Review the output and git status.
exit /b 1

:done
echo.
pause
