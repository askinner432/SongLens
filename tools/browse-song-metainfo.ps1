param(
  [Parameter(Position = 0)]
  [string]$RootPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$ConfigPath = Join-Path -Path (Split-Path -Parent $ScriptDirectory) -ChildPath "song-metainfo-browser.config.json"

function Get-ConfiguredRootPath {
  if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
    return ""
  }

  try {
    $config = Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
    return [string]$config.RootPath
  } catch {
    return ""
  }
}

function Save-ConfiguredRootPath {
  param([string]$Path)

  [pscustomobject]@{
    RootPath = (Resolve-Path -LiteralPath $Path).Path
  } |
    ConvertTo-Json |
    Set-Content -LiteralPath $ConfigPath -Encoding UTF8
}

function Resolve-RootPath {
  param([string]$RequestedRootPath)

  if (-not [string]::IsNullOrWhiteSpace($RequestedRootPath)) {
    return $RequestedRootPath
  }

  $configuredRootPath = Get-ConfiguredRootPath
  if (-not [string]::IsNullOrWhiteSpace($configuredRootPath)) {
    return $configuredRootPath
  }

  Write-Host "No songs folder is configured yet." -ForegroundColor Yellow
  Write-Host ""
  $enteredPath = Read-Host "Enter your Studio One songs folder"
  if ([string]::IsNullOrWhiteSpace($enteredPath)) {
    throw "No songs folder was provided."
  }

  return $enteredPath
}

function Format-SongDuration {
  param([string]$Seconds)

  $numericSeconds = 0.0
  if (-not [double]::TryParse($Seconds, [ref]$numericSeconds)) {
    return $Seconds
  }

  $timeSpan = [TimeSpan]::FromSeconds($numericSeconds)
  if ($timeSpan.TotalHours -ge 1) {
    return "{0:h\:mm\:ss}" -f $timeSpan
  }

  return "{0:m\:ss}" -f $timeSpan
}

function Get-SongMetainfo {
  param([string]$SongPath)

  Add-Type -AssemblyName System.IO.Compression.FileSystem

  $zip = [System.IO.Compression.ZipFile]::OpenRead($SongPath)
  try {
    $entry = $zip.GetEntry("metainfo.xml")
    if ($null -eq $entry) {
      throw "metainfo.xml was not found."
    }

    $reader = [System.IO.StreamReader]::new($entry.Open())
    try {
      [xml]$xml = $reader.ReadToEnd()
    } finally {
      $reader.Dispose()
    }
  } finally {
    $zip.Dispose()
  }

  $attributes = @{}
  foreach ($attribute in $xml.MetaInformation.Attribute) {
    $attributes[$attribute.id] = $attribute.value
  }

  $timeSignature = ""
  if ($attributes.ContainsKey("Media:TimeSignatureNumerator") -and $attributes.ContainsKey("Media:TimeSignatureDenominator")) {
    $timeSignature = "$($attributes["Media:TimeSignatureNumerator"])/$($attributes["Media:TimeSignatureDenominator"])"
  }

  $length = ""
  if ($attributes.ContainsKey("Media:Length")) {
    $length = Format-SongDuration -Seconds $attributes["Media:Length"]
  }

  return [pscustomobject]@{
    Path = (Resolve-Path -LiteralPath $SongPath).Path
    Title = $attributes["Document:Title"]
    Artist = $attributes["Media:Artist"]
    Year = $attributes["Media:Year"]
    Tempo = $attributes["Media:Tempo"]
    KeySignature = $attributes["Media:KeySignature"]
    TimeSignature = $timeSignature
    SampleRate = $attributes["Media:SampleRate"]
    BitDepth = $attributes["Media:BitDepth"]
    TrackCount = $attributes["Media:TrackCount"]
    Length = $length
    Generator = $attributes["Document:Generator"]
    FormatVersion = $attributes["Document:FormatVersion"]
    NotesFile = $attributes["Document:Notes"]
    ArtworkFile = $attributes["Media:Artwork"]
    Comment = $attributes["Media:Comment"]
    Attributes = $attributes
  }
}

function Show-SongDetails {
  param([string]$SongPath)

  try {
    $metadata = Get-SongMetainfo -SongPath $SongPath
  } catch {
    Clear-Host
    Write-Host "Song metadata" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Could not read metadata: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Path: $SongPath"
    Write-Host ""
    Read-Host "Press Enter to go back"
    return
  }

  while ($true) {
    Clear-Host
    Write-Host "Song metadata" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Main facts" -ForegroundColor Cyan
    Write-Host ""
    $metadata |
      Select-Object Title, Artist, Year, Tempo, KeySignature, TimeSignature, Length, TrackCount, SampleRate, BitDepth, Generator, FormatVersion, NotesFile, ArtworkFile, Comment, Path |
      Format-List

    Write-Host ""
    Write-Host "Commands: r = raw attributes, b = back"
    $choice = Read-Host "Choose"

    if ($choice -eq "b" -or [string]::IsNullOrWhiteSpace($choice)) {
      return
    }

    if ($choice -ne "r") {
      continue
    }

    Clear-Host
    Write-Host "Raw metainfo.xml attributes" -ForegroundColor Cyan
    Write-Host ""

    $metadata.Attributes.GetEnumerator() |
      Sort-Object -Property Name |
      ForEach-Object {
        [pscustomobject]@{
          Id = $_.Name
          Value = $_.Value
        }
      } |
      Format-Table -AutoSize

    Write-Host ""
    Read-Host "Press Enter to return to main facts"
  }
}

function Test-SongSearchMatch {
  param(
    [object]$Metadata,
    [string]$FileName,
    [string]$Query
  )

  $fields = @(
    [pscustomobject]@{ Label = "Filename"; Value = $FileName },
    [pscustomobject]@{ Label = "Title"; Value = $Metadata.Title },
    [pscustomobject]@{ Label = "Artist"; Value = $Metadata.Artist },
    [pscustomobject]@{ Label = "Year"; Value = $Metadata.Year },
    [pscustomobject]@{ Label = "Tempo"; Value = $Metadata.Tempo },
    [pscustomobject]@{ Label = "Key"; Value = $Metadata.KeySignature },
    [pscustomobject]@{ Label = "Time signature"; Value = $Metadata.TimeSignature },
    [pscustomobject]@{ Label = "Comment"; Value = $Metadata.Comment }
  )

  foreach ($field in $fields) {
    if (-not [string]::IsNullOrWhiteSpace($field.Value) -and $field.Value.IndexOf($Query, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
      return $field
    }
  }

  return $null
}

function Test-RegularSongFile {
  param([string]$FileName)

  return $FileName -notmatch "\s\(Autosaved\)\.song$"
}

function Format-SearchField {
  param([string]$Value)

  if ([string]::IsNullOrWhiteSpace($Value)) {
    return "-"
  }

  return $Value
}

function Show-Search {
  param([string]$SearchRoot)

  Clear-Host
  Write-Host "Search songs" -ForegroundColor Cyan
  Write-Host "Search root: $SearchRoot"
  Write-Host ""
  $query = Read-Host "Search text"

  if ([string]::IsNullOrWhiteSpace($query)) {
    return
  }

  Clear-Host
  Write-Host "Searching..." -ForegroundColor Cyan
  Write-Host ""

  $results = @()
  $songFiles = Get-ChildItem -LiteralPath $SearchRoot -Recurse -File -Filter "*.song" -ErrorAction SilentlyContinue |
    Where-Object { Test-RegularSongFile -FileName $_.Name } |
    Sort-Object -Property FullName

  foreach ($song in $songFiles) {
    try {
      $metadata = Get-SongMetainfo -SongPath $song.FullName
    } catch {
      continue
    }

    $match = Test-SongSearchMatch -Metadata $metadata -FileName $song.Name -Query $query
    if ($null -ne $match) {
      $results += [pscustomobject]@{
        Name = $song.Name
        Folder = $song.DirectoryName
        Path = $song.FullName
        Metadata = $metadata
        MatchedField = $match.Label
        MatchedValue = $match.Value
      }
    }
  }

  while ($true) {
    Clear-Host
    Write-Host "Search results" -ForegroundColor Cyan
    Write-Host "Query: $query"
    Write-Host ""

    if ($results.Count -eq 0) {
      Write-Host "No matching songs found."
      Write-Host ""
      Read-Host "Press Enter to go back"
      return
    }

    for ($index = 0; $index -lt $results.Count; $index += 1) {
      $result = $results[$index]
      $metadata = $result.Metadata
      Write-Host ("[{0}] {1} | Title: {2} | Key: {3} | Tempo: {4}" -f `
        ($index + 1),
        $result.Name,
        (Format-SearchField -Value $metadata.Title),
        (Format-SearchField -Value $metadata.KeySignature),
        (Format-SearchField -Value $metadata.Tempo))
      Write-Host ("    Match: {0}: {1} | Folder: {2}" -f `
        $result.MatchedField,
        (Format-SearchField -Value $result.MatchedValue),
        $result.Folder)
      Write-Host ""
    }

    Write-Host "Commands: number = open, b = back"
    $choice = Read-Host "Choose"

    if ($choice -eq "b" -or [string]::IsNullOrWhiteSpace($choice)) {
      return
    }

    $selectedIndex = 0
    if (-not [int]::TryParse($choice, [ref]$selectedIndex)) {
      continue
    }

    if ($selectedIndex -lt 1 -or $selectedIndex -gt $results.Count) {
      continue
    }

    Show-SongDetails -SongPath $results[$selectedIndex - 1].Path
  }
}

function Show-Folder {
  param([string]$FolderPath)

  $currentPath = (Resolve-Path -LiteralPath $FolderPath).Path
  $searchRoot = $currentPath

  while ($true) {
    Clear-Host
    Write-Host "Song metadata browser" -ForegroundColor Cyan
    Write-Host "Current folder: $currentPath"
    Write-Host ""

    $directories = Get-ChildItem -LiteralPath $currentPath -Directory |
      Sort-Object -Property Name
    $songs = Get-ChildItem -LiteralPath $currentPath -File -Filter "*.song" |
      Where-Object { Test-RegularSongFile -FileName $_.Name } |
      Sort-Object -Property Name

    $items = @()
    foreach ($directory in $directories) {
      $items += [pscustomobject]@{
        Type = "Folder"
        Name = $directory.Name
        Path = $directory.FullName
      }
    }

    foreach ($song in $songs) {
      $items += [pscustomobject]@{
        Type = "Song"
        Name = $song.Name
        Path = $song.FullName
      }
    }

    if ($items.Count -eq 0) {
      Write-Host "No folders or .song files found here."
    } else {
      for ($index = 0; $index -lt $items.Count; $index += 1) {
        $item = $items[$index]
        Write-Host ("[{0}] {1,-6} {2}" -f ($index + 1), $item.Type, $item.Name)
      }
    }

    Write-Host ""
    Write-Host "Commands: number = open, s = search, b = back, q = quit"
    $choice = Read-Host "Choose"

    if ($choice -eq "q") {
      return
    }

    if ($choice -eq "s") {
      Show-Search -SearchRoot $searchRoot
      continue
    }

    if ($choice -eq "b") {
      $parent = Split-Path -Parent $currentPath
      if ($parent -and (Test-Path -LiteralPath $parent -PathType Container)) {
        $currentPath = (Resolve-Path -LiteralPath $parent).Path
      }
      continue
    }

    $selectedIndex = 0
    if (-not [int]::TryParse($choice, [ref]$selectedIndex)) {
      continue
    }

    if ($selectedIndex -lt 1 -or $selectedIndex -gt $items.Count) {
      continue
    }

    $selected = $items[$selectedIndex - 1]
    if ($selected.Type -eq "Folder") {
      $currentPath = $selected.Path
    } else {
      Show-SongDetails -SongPath $selected.Path
    }
  }
}

$ResolvedRootPath = Resolve-RootPath -RequestedRootPath $RootPath

if (-not (Test-Path -LiteralPath $ResolvedRootPath -PathType Container)) {
  throw "Folder not found: $ResolvedRootPath"
}

Save-ConfiguredRootPath -Path $ResolvedRootPath
Show-Folder -FolderPath $ResolvedRootPath
