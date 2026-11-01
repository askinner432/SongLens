param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "EventsToEditor.package")
)

$ErrorActionPreference = "Stop"
$archivePath = [System.IO.Path]::ChangeExtension($OutputPath, ".zip")
$sourceFiles = @(
    (Join-Path $PSScriptRoot "metainfo.xml"),
    (Join-Path $PSScriptRoot "classfactory.xml"),
    (Join-Path $PSScriptRoot "selected-events-to-editor.js")
)

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

if (Test-Path -LiteralPath $OutputPath) {
    Remove-Item -LiteralPath $OutputPath -Force
}

Compress-Archive -LiteralPath $sourceFiles -DestinationPath $archivePath -CompressionLevel Optimal
Move-Item -LiteralPath $archivePath -Destination $OutputPath
Write-Host "Built $OutputPath"
