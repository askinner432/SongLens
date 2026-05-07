param(
  [Parameter(Mandatory = $true, Position = 0)]
  [string]$SongPath,

  [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

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

function Get-FriendlySongMetadata {
  param([hashtable]$Attributes)

  $timeSignature = ""
  if ($Attributes.ContainsKey("Media:TimeSignatureNumerator") -and $Attributes.ContainsKey("Media:TimeSignatureDenominator")) {
    $timeSignature = "$($Attributes["Media:TimeSignatureNumerator"])/$($Attributes["Media:TimeSignatureDenominator"])"
  }

  $length = ""
  if ($Attributes.ContainsKey("Media:Length")) {
    $length = Format-SongDuration -Seconds $Attributes["Media:Length"]
  }

  return [ordered]@{
    Title = $Attributes["Document:Title"]
    MediaTitle = $Attributes["Media:Title"]
    Artist = $Attributes["Media:Artist"]
    Year = $Attributes["Media:Year"]
    Generator = $Attributes["Document:Generator"]
    FormatVersion = $Attributes["Document:FormatVersion"]
    Tempo = $Attributes["Media:Tempo"]
    TimeSignature = $timeSignature
    KeySignature = $Attributes["Media:KeySignature"]
    TrackCount = $Attributes["Media:TrackCount"]
    Length = $length
    SampleRate = $Attributes["Media:SampleRate"]
    BitDepth = $Attributes["Media:BitDepth"]
    NotesFile = $Attributes["Document:Notes"]
    ArtworkFile = $Attributes["Media:Artwork"]
    Comment = $Attributes["Media:Comment"]
  }
}

if (-not (Test-Path -LiteralPath $SongPath -PathType Leaf)) {
  throw "Song file not found: $SongPath"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

$zip = [System.IO.Compression.ZipFile]::OpenRead($SongPath)
try {
  $entry = $zip.GetEntry("metainfo.xml")
  if ($null -eq $entry) {
    throw "metainfo.xml was not found in: $SongPath"
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

$friendly = Get-FriendlySongMetadata -Attributes $attributes
$allAttributes = $attributes.GetEnumerator() |
  Sort-Object -Property Name |
  ForEach-Object {
    [pscustomobject]@{
      Id = $_.Name
      Value = $_.Value
    }
  }

if ($Json) {
  [pscustomobject]@{
    SongPath = (Resolve-Path -LiteralPath $SongPath).Path
    Summary = [pscustomobject]$friendly
    Attributes = $allAttributes
  } | ConvertTo-Json -Depth 4

  exit
}

Write-Host ""
Write-Host "Song metadata summary" -ForegroundColor Cyan
Write-Host "Source: $((Resolve-Path -LiteralPath $SongPath).Path)"
Write-Host ""

[pscustomobject]$friendly | Format-List

Write-Host ""
Write-Host "All metainfo.xml attributes" -ForegroundColor Cyan
Write-Host ""

$allAttributes | Format-Table -AutoSize
