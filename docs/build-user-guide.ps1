param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'SongLens-User-Guide.pdf'),
    [string]$HtmlOutputPath
)

$ErrorActionPreference = 'Stop'

$wikiDirectory = Join-Path $PSScriptRoot 'wiki'
$projectPath = Join-Path $PSScriptRoot '..\src\SongMetainfoBrowser.App\SongMetainfoBrowser.App.csproj'
$chromePath = 'C:\Program Files\Google\Chrome\Application\chrome.exe'
$pages = @(
    'Home.md',
    'Getting-Started.md',
    'Browsing-Folders-And-Songs.md',
    'Search-And-Filters.md',
    'Detail-Tabs.md',
    'Exports-And-Snapshots.md',
    'Preferences-And-Themes.md',
    'Tips-And-Compatibility.md'
)

if (-not (Test-Path -LiteralPath $chromePath)) {
    throw "Google Chrome was not found at $chromePath."
}

[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$version = $project.Project.PropertyGroup.Version | Select-Object -First 1

function ConvertTo-HtmlEncodedText([string]$text) {
    return [System.Net.WebUtility]::HtmlEncode($text)
}

function ConvertTo-Slug([string]$text) {
    $slug = $text.ToLowerInvariant() -replace '[^a-z0-9]+', '-'
    return $slug.Trim('-')
}

function ConvertTo-InlineHtml([string]$text) {
    $html = ConvertTo-HtmlEncodedText $text
    $html = [regex]::Replace($html, '`([^`]+)`', '<code>$1</code>')
    $html = [regex]::Replace($html, '\*\*(.+?)\*\*', '<strong>$1</strong>')
    $html = [regex]::Replace($html, '\[([^\]]+)\]\(([^)]+)\)', {
        param($match)
        $label = $match.Groups[1].Value
        $target = $match.Groups[2].Value
        if ($target -notmatch '^(https?://|mailto:|#)') {
            $target = "#$(ConvertTo-Slug ([System.IO.Path]::GetFileNameWithoutExtension($target)))"
        }
        return '<a href="{0}">{1}</a>' -f $target, $label
    })
    return $html
}

function Convert-ImageLine([string]$line) {
    $match = [regex]::Match($line, '^!\[([^\]]*)\]\(([^)]+)\)$')
    if (-not $match.Success) {
        return $null
    }

    $alt = ConvertTo-HtmlEncodedText $match.Groups[1].Value
    $relativePath = $match.Groups[2].Value -replace '/', '\'
    $imagePath = Join-Path $wikiDirectory $relativePath
    if (-not (Test-Path -LiteralPath $imagePath)) {
        throw "Missing guide image: $imagePath"
    }

    $extension = [System.IO.Path]::GetExtension($imagePath).ToLowerInvariant()
    $mimeType = switch ($extension) {
        '.jpg' { 'image/jpeg' }
        '.jpeg' { 'image/jpeg' }
        '.webp' { 'image/webp' }
        default { 'image/png' }
    }
    $base64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($imagePath))
    return '<figure><img src="data:{0};base64,{1}" alt="{2}"><figcaption>{2}</figcaption></figure>' -f $mimeType, $base64, $alt
}

function Convert-MarkdownPage([string]$path) {
    $builder = [Text.StringBuilder]::new()
    $paragraph = [Collections.Generic.List[string]]::new()
    $listType = $null

    function Close-Paragraph {
        if ($paragraph.Count -gt 0) {
            [void]$builder.AppendLine("<p>$(ConvertTo-InlineHtml ($paragraph -join ' '))</p>")
            $paragraph.Clear()
        }
    }

    function Close-List {
        if ($null -ne $listType) {
            [void]$builder.AppendLine("</$listType>")
            Set-Variable -Name listType -Value $null -Scope 1
        }
    }

    foreach ($line in Get-Content -LiteralPath $path) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            Close-Paragraph
            Close-List
            continue
        }

        $imageHtml = Convert-ImageLine $line
        if ($null -ne $imageHtml) {
            Close-Paragraph
            Close-List
            [void]$builder.AppendLine($imageHtml)
            continue
        }

        $heading = [regex]::Match($line, '^(#{1,3})\s+(.+)$')
        if ($heading.Success) {
            Close-Paragraph
            Close-List
            $level = $heading.Groups[1].Value.Length
            $title = $heading.Groups[2].Value
            $id = ConvertTo-Slug $title
            [void]$builder.AppendLine(('<h{0} id="{1}">{2}</h{0}>' -f $level, $id, (ConvertTo-InlineHtml $title)))
            continue
        }

        $unorderedItem = [regex]::Match($line, '^\s*-\s+(.+)$')
        $orderedItem = [regex]::Match($line, '^\s*\d+\.\s+(.+)$')
        if ($unorderedItem.Success -or $orderedItem.Success) {
            Close-Paragraph
            $requiredListType = if ($orderedItem.Success) { 'ol' } else { 'ul' }
            if ($listType -ne $requiredListType) {
                Close-List
                $listType = $requiredListType
                [void]$builder.AppendLine("<$listType>")
            }
            $itemText = if ($orderedItem.Success) { $orderedItem.Groups[1].Value } else { $unorderedItem.Groups[1].Value }
            [void]$builder.AppendLine("<li>$(ConvertTo-InlineHtml $itemText)</li>")
            continue
        }

        $paragraph.Add($line.Trim())
    }

    Close-Paragraph
    Close-List
    return $builder.ToString()
}

$chapters = [Text.StringBuilder]::new()
foreach ($page in $pages) {
    $pagePath = Join-Path $wikiDirectory $page
    if (-not (Test-Path -LiteralPath $pagePath)) {
        throw "Missing guide page: $pagePath"
    }
    $chapterId = ConvertTo-Slug ([IO.Path]::GetFileNameWithoutExtension($page))
    [void]$chapters.AppendLine(('<section class="chapter" id="{0}">' -f $chapterId))
    [void]$chapters.AppendLine((Convert-MarkdownPage $pagePath))
    [void]$chapters.AppendLine('</section>')
}

$html = @"
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>SongLens User Guide</title>
<style>
@page { size: Letter; margin: 0.55in 0.6in 0.6in; }
* { box-sizing: border-box; }
html { font-family: "Segoe UI", Arial, sans-serif; color: #202428; }
body { margin: 0; font-size: 10.5pt; line-height: 1.42; }
.cover { min-height: 9.2in; display: flex; flex-direction: column; justify-content: center; text-align: center; page-break-after: always; }
.cover h1 { margin: 0 0 0.15in; font-size: 34pt; color: #173a5e; }
.cover p { margin: 0.04in; color: #52606d; font-size: 13pt; }
.chapter { break-before: page; }
.chapter:first-of-type { break-before: auto; }
h1 { font-size: 24pt; color: #173a5e; border-bottom: 2px solid #7fa6c9; padding-bottom: 0.08in; }
h2 { margin-top: 0.28in; font-size: 17pt; color: #244f73; break-after: avoid; }
h3 { margin-top: 0.2in; font-size: 13pt; color: #315f84; break-after: avoid; }
p { margin: 0.08in 0 0.12in; }
ul, ol { margin: 0.06in 0 0.14in 0.3in; padding-left: 0.18in; }
li { margin: 0.035in 0; }
code { padding: 0.01in 0.04in; background: #edf1f4; border: 1px solid #d7dde2; border-radius: 3px; font-family: Consolas, monospace; font-size: 9.5pt; }
a { color: #1f5f91; text-decoration: none; }
figure { margin: 0.18in auto 0.24in; text-align: center; break-inside: avoid; }
figure img { display: block; margin: 0 auto; max-width: 100%; max-height: 8.15in; object-fit: contain; border: 1px solid #c8cdd2; }
figcaption { margin-top: 0.05in; color: #5d6872; font-size: 8.5pt; font-style: italic; }
</style>
</head>
<body>
<section class="cover">
  <h1>SongLens User Guide</h1>
  <p>Version $version</p>
  <p>Browsing, inspecting, searching, and documenting Studio One songs</p>
</section>
$chapters
</body>
</html>
"@

$temporaryDirectory = Join-Path ([IO.Path]::GetTempPath()) "SongLens-user-guide-$PID"
$temporaryHtml = Join-Path $temporaryDirectory 'SongLens-User-Guide.html'
$userDataDirectory = Join-Path $temporaryDirectory 'chrome-profile'
[void](New-Item -ItemType Directory -Path $temporaryDirectory)
[void](New-Item -ItemType Directory -Path $userDataDirectory)

try {
    [IO.File]::WriteAllText($temporaryHtml, $html, [Text.UTF8Encoding]::new($false))
    if (-not [string]::IsNullOrWhiteSpace($HtmlOutputPath)) {
        $resolvedHtmlOutputPath = [IO.Path]::GetFullPath($HtmlOutputPath)
        [IO.File]::WriteAllText($resolvedHtmlOutputPath, $html, [Text.UTF8Encoding]::new($false))
    }
    $resolvedOutputPath = [IO.Path]::GetFullPath($OutputPath)
    $outputDirectory = [IO.Path]::GetDirectoryName($resolvedOutputPath)
    if (-not (Test-Path -LiteralPath $outputDirectory)) {
        [void](New-Item -ItemType Directory -Path $outputDirectory)
    }

    $arguments = @(
        '--headless',
        '--disable-gpu',
        '--allow-file-access-from-files',
        '--no-pdf-header-footer',
        "--user-data-dir=$userDataDirectory",
        "--print-to-pdf=$resolvedOutputPath",
        ([Uri]$temporaryHtml).AbsoluteUri
    )
    $process = Start-Process -FilePath $chromePath -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $resolvedOutputPath)) {
        throw "Chrome failed to create the PDF (exit code $($process.ExitCode))."
    }

    Get-Item -LiteralPath $resolvedOutputPath
}
finally {
    if (Test-Path -LiteralPath $temporaryDirectory) {
        Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
    }
}
