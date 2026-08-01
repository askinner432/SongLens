param(
    [string]$WikiDirectory,
    [string]$CommitMessage = "Update SongLens wiki"
)

$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$wikiSource = Join-Path $repoRoot "docs\wiki"
if ([string]::IsNullOrWhiteSpace($WikiDirectory)) {
    $WikiDirectory = Join-Path (Split-Path $repoRoot -Parent) "SongLens.wiki"
}
$WikiDirectory = [System.IO.Path]::GetFullPath($WikiDirectory)

if (-not (Test-Path -LiteralPath $wikiSource -PathType Container)) {
    throw "Wiki source folder was not found: $wikiSource"
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git was not found on PATH."
}

if (-not (Test-Path -LiteralPath $WikiDirectory)) {
    Write-Host "Cloning the SongLens wiki repository..."
    git clone "https://github.com/askinner432/SongLens.wiki.git" $WikiDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "Could not clone the SongLens wiki repository."
    }
}

$wikiGitDirectory = Join-Path $WikiDirectory ".git"
if (-not (Test-Path -LiteralPath $wikiGitDirectory -PathType Container)) {
    throw "The wiki destination is not a Git repository: $WikiDirectory"
}

$wikiFiles = Get-ChildItem -LiteralPath $wikiSource -Filter "*.md" -File
if ($wikiFiles.Count -eq 0) {
    throw "No Markdown wiki pages were found in: $wikiSource"
}

Write-Host "Copying $($wikiFiles.Count) wiki pages..."
foreach ($wikiFile in $wikiFiles) {
    Copy-Item -LiteralPath $wikiFile.FullName -Destination (Join-Path $WikiDirectory $wikiFile.Name) -Force
}

git -C $WikiDirectory add -- "*.md"
if ($LASTEXITCODE -ne 0) {
    throw "Could not stage the wiki pages."
}

git -C $WikiDirectory diff --cached --quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "The GitHub wiki is already up to date."
    exit 0
}
if ($LASTEXITCODE -ne 1) {
    throw "Could not inspect the staged wiki changes."
}

git -C $WikiDirectory commit -m $CommitMessage
if ($LASTEXITCODE -ne 0) {
    throw "Could not commit the wiki changes."
}

git -C $WikiDirectory push
if ($LASTEXITCODE -ne 0) {
    throw "Could not push the wiki changes."
}

Write-Host "SongLens wiki published successfully."
Write-Host "Wiki repository: $WikiDirectory"
