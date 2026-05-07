param(
  [Parameter(Position = 0)]
  [string]$RootPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression.FileSystem

$ScriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDirectory = Split-Path -Parent $ScriptDirectory
$ConfigPath = Join-Path -Path $ProjectDirectory -ChildPath "song-metainfo-browser.config.json"
$script:CurrentRootPath = ""
$script:IsSearchMode = $false

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

function Resolve-InitialRootPath {
  param([string]$RequestedRootPath)

  if (-not [string]::IsNullOrWhiteSpace($RequestedRootPath)) {
    return $RequestedRootPath
  }

  return Get-ConfiguredRootPath
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

function Test-RegularSongFile {
  param([string]$FileName)

  return $FileName -notmatch "\s\(Autosaved\)\.song$"
}

function Get-SongMetainfo {
  param([string]$SongPath)

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
    FileName = [System.IO.Path]::GetFileName($SongPath)
    Folder = [System.IO.Path]::GetDirectoryName($SongPath)
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

function Get-SongSearchMatch {
  param(
    [object]$Metadata,
    [string]$Query
  )

  if ([string]::IsNullOrWhiteSpace($Query)) {
    return $null
  }

  $fields = @(
    [pscustomobject]@{ Label = "Filename"; Value = $Metadata.FileName },
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

function Format-Field {
  param([string]$Value)

  if ([string]::IsNullOrWhiteSpace($Value)) {
    return ""
  }

  return $Value
}

$form = [System.Windows.Forms.Form]::new()
$form.Text = "Song Metainfo Browser"
$form.StartPosition = "CenterScreen"
$form.MinimumSize = [System.Drawing.Size]::new(980, 640)
$form.Size = [System.Drawing.Size]::new(1180, 760)

$layout = [System.Windows.Forms.TableLayoutPanel]::new()
$layout.Dock = "Fill"
$layout.ColumnCount = 1
$layout.RowCount = 3
$layout.RowStyles.Add([System.Windows.Forms.RowStyle]::new([System.Windows.Forms.SizeType]::Absolute, 42)) | Out-Null
$layout.RowStyles.Add([System.Windows.Forms.RowStyle]::new([System.Windows.Forms.SizeType]::Percent, 100)) | Out-Null
$layout.RowStyles.Add([System.Windows.Forms.RowStyle]::new([System.Windows.Forms.SizeType]::Absolute, 24)) | Out-Null
$form.Controls.Add($layout)

$topPanel = [System.Windows.Forms.TableLayoutPanel]::new()
$topPanel.Dock = "Fill"
$topPanel.ColumnCount = 5
$topPanel.RowCount = 1
$topPanel.Padding = [System.Windows.Forms.Padding]::new(8, 8, 8, 4)
$topPanel.ColumnStyles.Add([System.Windows.Forms.ColumnStyle]::new([System.Windows.Forms.SizeType]::Percent, 100)) | Out-Null
$topPanel.ColumnStyles.Add([System.Windows.Forms.ColumnStyle]::new([System.Windows.Forms.SizeType]::Absolute, 86)) | Out-Null
$topPanel.ColumnStyles.Add([System.Windows.Forms.ColumnStyle]::new([System.Windows.Forms.SizeType]::Absolute, 86)) | Out-Null
$topPanel.ColumnStyles.Add([System.Windows.Forms.ColumnStyle]::new([System.Windows.Forms.SizeType]::Absolute, 220)) | Out-Null
$topPanel.ColumnStyles.Add([System.Windows.Forms.ColumnStyle]::new([System.Windows.Forms.SizeType]::Absolute, 86)) | Out-Null

$rootTextBox = [System.Windows.Forms.TextBox]::new()
$rootTextBox.Dock = "Fill"
$rootTextBox.ReadOnly = $true

$browseButton = [System.Windows.Forms.Button]::new()
$browseButton.Text = "Browse..."
$browseButton.Dock = "Fill"

$refreshButton = [System.Windows.Forms.Button]::new()
$refreshButton.Text = "Refresh"
$refreshButton.Dock = "Fill"

$searchTextBox = [System.Windows.Forms.TextBox]::new()
$searchTextBox.Dock = "Fill"

$searchButton = [System.Windows.Forms.Button]::new()
$searchButton.Text = "Search"
$searchButton.Dock = "Fill"

$topPanel.Controls.Add($rootTextBox, 0, 0)
$topPanel.Controls.Add($browseButton, 1, 0)
$topPanel.Controls.Add($refreshButton, 2, 0)
$topPanel.Controls.Add($searchTextBox, 3, 0)
$topPanel.Controls.Add($searchButton, 4, 0)
$layout.Controls.Add($topPanel, 0, 0)

$statusStrip = [System.Windows.Forms.StatusStrip]::new()
$statusStrip.Dock = "Fill"
$statusLabel = [System.Windows.Forms.ToolStripStatusLabel]::new()
$statusLabel.Text = "Ready"
$statusStrip.Items.Add($statusLabel) | Out-Null
$layout.Controls.Add($statusStrip, 0, 2)

$mainSplit = [System.Windows.Forms.SplitContainer]::new()
$mainSplit.Dock = "Fill"
$layout.Controls.Add($mainSplit, 0, 1)

$folderTree = [System.Windows.Forms.TreeView]::new()
$folderTree.Dock = "Fill"
$folderTree.HideSelection = $false
$mainSplit.Panel1.Controls.Add($folderTree)

$rightSplit = [System.Windows.Forms.SplitContainer]::new()
$rightSplit.Dock = "Fill"
$rightSplit.Orientation = "Horizontal"
$mainSplit.Panel2.Controls.Add($rightSplit)

$songList = [System.Windows.Forms.DataGridView]::new()
$songList.Dock = "Fill"
$songList.AllowUserToAddRows = $false
$songList.AllowUserToDeleteRows = $false
$songList.AutoSizeRowsMode = "None"
$songList.BackgroundColor = [System.Drawing.SystemColors]::Window
$songList.BorderStyle = "Fixed3D"
$songList.ColumnHeadersHeightSizeMode = "AutoSize"
$songList.MultiSelect = $false
$songList.ReadOnly = $true
$songList.RowHeadersVisible = $false
$songList.SelectionMode = "FullRowSelect"
$songList.Columns.Add("Song", "Song") | Out-Null
$songList.Columns.Add("Title", "Title") | Out-Null
$songList.Columns.Add("Artist", "Artist") | Out-Null
$songList.Columns.Add("Key", "Key") | Out-Null
$songList.Columns.Add("Tempo", "Tempo") | Out-Null
$songList.Columns.Add("Match", "Match") | Out-Null
$rightSplit.Panel1.Controls.Add($songList)

$detailsTabs = [System.Windows.Forms.TabControl]::new()
$detailsTabs.Dock = "Fill"
$rightSplit.Panel2.Controls.Add($detailsTabs)

$summaryTab = [System.Windows.Forms.TabPage]::new()
$summaryTab.Text = "Main Facts"
$rawTab = [System.Windows.Forms.TabPage]::new()
$rawTab.Text = "Raw Attributes"
$detailsTabs.TabPages.Add($summaryTab) | Out-Null
$detailsTabs.TabPages.Add($rawTab) | Out-Null

$summaryList = [System.Windows.Forms.ListView]::new()
$summaryList.Dock = "Fill"
$summaryList.UseCompatibleStateImageBehavior = $false
$summaryList.View = "Details"
$summaryList.FullRowSelect = $true
$summaryList.GridLines = $true
$summaryList.Columns.Add("Field", 150) | Out-Null
$summaryList.Columns.Add("Value", 780) | Out-Null
$summaryTab.Controls.Add($summaryList)

$rawList = [System.Windows.Forms.ListView]::new()
$rawList.Dock = "Fill"
$rawList.UseCompatibleStateImageBehavior = $false
$rawList.View = "Details"
$rawList.FullRowSelect = $true
$rawList.GridLines = $true
$rawList.Columns.Add("Id", 250) | Out-Null
$rawList.Columns.Add("Value", 680) | Out-Null
$rawTab.Controls.Add($rawList)

function Set-Status {
  param([string]$Message)

  $statusLabel.Text = $Message
  [System.Windows.Forms.Application]::DoEvents()
}

function Add-PlaceholderChild {
  param([System.Windows.Forms.TreeNode]$Node)

  $Node.Nodes.Clear()
  $placeholder = [System.Windows.Forms.TreeNode]::new("Loading")
  $placeholder.Tag = "__placeholder__"
  $Node.Nodes.Add($placeholder) | Out-Null
}

function Add-FolderNode {
  param(
    [System.Windows.Forms.TreeNodeCollection]$Nodes,
    [System.IO.DirectoryInfo]$Directory
  )

  $node = [System.Windows.Forms.TreeNode]::new($Directory.Name)
  $node.Tag = $Directory.FullName
  $Nodes.Add($node) | Out-Null

  $hasChildDirectory = $false
  try {
    $hasChildDirectory = ($Directory.GetDirectories() | Select-Object -First 1).Count -gt 0
  } catch {
    $hasChildDirectory = $false
  }

  if ($hasChildDirectory) {
    Add-PlaceholderChild -Node $node
  }

  return $node
}

function Populate-FolderChildren {
  param([System.Windows.Forms.TreeNode]$Node)

  if ($null -eq $Node.Tag -or [string]$Node.Tag -eq "__placeholder__") {
    return
  }

  $path = [string]$Node.Tag
  $Node.Nodes.Clear()

  try {
    Get-ChildItem -LiteralPath $path -Directory -ErrorAction Stop |
      Sort-Object -Property Name |
      ForEach-Object { Add-FolderNode -Nodes $Node.Nodes -Directory $_ | Out-Null }
  } catch {
    Set-Status "Could not load folders: $($_.Exception.Message)"
  }
}

function Set-RootPath {
  param([string]$Path)

  if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
    [System.Windows.Forms.MessageBox]::Show("Folder not found:`r`n$Path", "Song Metainfo Browser", "OK", "Warning") | Out-Null
    return
  }

  $script:CurrentRootPath = (Resolve-Path -LiteralPath $Path).Path
  Save-ConfiguredRootPath -Path $script:CurrentRootPath
  $rootTextBox.Text = $script:CurrentRootPath
  $folderTree.Nodes.Clear()
  $songList.Rows.Clear()
  $summaryList.Items.Clear()
  $rawList.Items.Clear()
  $script:IsSearchMode = $false

  $rootDirectory = Get-Item -LiteralPath $script:CurrentRootPath
  $rootNode = [System.Windows.Forms.TreeNode]::new($rootDirectory.Name)
  $rootNode.Tag = $rootDirectory.FullName
  $folderTree.Nodes.Add($rootNode) | Out-Null
  Add-PlaceholderChild -Node $rootNode
  $rootNode.Expand()
  $folderTree.SelectedNode = $rootNode
  Set-Status "Loaded $script:CurrentRootPath"
}

function Add-SongListItem {
  param(
    [object]$Metadata,
    [object]$Match
  )

  $matchText = ""
  if ($null -ne $Match) {
    $matchText = "$($Match.Label): $($Match.Value)"
  }

  $rowIndex = $songList.Rows.Add()
  $row = $songList.Rows[$rowIndex]
  $row.Cells["Song"].Value = Format-Field -Value $Metadata.FileName
  $row.Cells["Title"].Value = Format-Field -Value $Metadata.Title
  $row.Cells["Artist"].Value = Format-Field -Value $Metadata.Artist
  $row.Cells["Key"].Value = Format-Field -Value $Metadata.KeySignature
  $row.Cells["Tempo"].Value = Format-Field -Value $Metadata.Tempo
  $row.Cells["Match"].Value = $matchText
  $row.Tag = $Metadata
}

function Resize-SongColumns {
  if ($songList.Columns.Count -eq 0) {
    return
  }

  $songList.Columns["Song"].Width = 230
  $songList.Columns["Title"].Width = 180
  $songList.Columns["Artist"].Width = 140
  $songList.Columns["Key"].Width = 90
  $songList.Columns["Tempo"].Width = 90
  $songList.Columns["Match"].Width = 280
}

function Resize-DetailColumns {
  foreach ($column in $summaryList.Columns) {
    $column.Width = -2
    if ($column.Width -lt 120) {
      $column.Width = 120
    }
  }

  foreach ($column in $rawList.Columns) {
    $column.Width = -2
    if ($column.Width -lt 160) {
      $column.Width = 160
    }
  }
}

function Load-SongsForFolder {
  param([string]$FolderPath)

  if ($script:IsSearchMode) {
    return
  }

  $songList.Rows.Clear()
  $summaryList.Items.Clear()
  $rawList.Items.Clear()
  Set-Status "Loading songs..."

  $loaded = 0
  try {
    $songs = Get-ChildItem -LiteralPath $FolderPath -File -Filter "*.song" -ErrorAction Stop |
      Where-Object { Test-RegularSongFile -FileName $_.Name } |
      Sort-Object -Property Name

    foreach ($song in $songs) {
      try {
        Add-SongListItem -Metadata (Get-SongMetainfo -SongPath $song.FullName) -Match $null
        $loaded += 1
      } catch {
        continue
      }
    }
  } catch {
    Set-Status "Could not load songs: $($_.Exception.Message)"
    return
  }

  Resize-SongColumns
  Set-Status "Loaded $loaded song(s) from $FolderPath"
}

function Show-MetadataDetails {
  param([object]$Metadata)

  $summaryList.Items.Clear()
  $rawList.Items.Clear()

  $fields = @(
    [pscustomobject]@{ Name = "Title"; Value = $Metadata.Title },
    [pscustomobject]@{ Name = "Artist"; Value = $Metadata.Artist },
    [pscustomobject]@{ Name = "Year"; Value = $Metadata.Year },
    [pscustomobject]@{ Name = "Tempo"; Value = $Metadata.Tempo },
    [pscustomobject]@{ Name = "Key Signature"; Value = $Metadata.KeySignature },
    [pscustomobject]@{ Name = "Time Signature"; Value = $Metadata.TimeSignature },
    [pscustomobject]@{ Name = "Length"; Value = $Metadata.Length },
    [pscustomobject]@{ Name = "Track Count"; Value = $Metadata.TrackCount },
    [pscustomobject]@{ Name = "Sample Rate"; Value = $Metadata.SampleRate },
    [pscustomobject]@{ Name = "Bit Depth"; Value = $Metadata.BitDepth },
    [pscustomobject]@{ Name = "Studio One Version"; Value = $Metadata.Generator },
    [pscustomobject]@{ Name = "Format Version"; Value = $Metadata.FormatVersion },
    [pscustomobject]@{ Name = "Notes File"; Value = $Metadata.NotesFile },
    [pscustomobject]@{ Name = "Artwork File"; Value = $Metadata.ArtworkFile },
    [pscustomobject]@{ Name = "Comment"; Value = $Metadata.Comment },
    [pscustomobject]@{ Name = "Path"; Value = $Metadata.Path }
  )

  foreach ($field in $fields) {
    $item = [System.Windows.Forms.ListViewItem]::new($field.Name)
    $item.SubItems.Add((Format-Field -Value $field.Value)) | Out-Null
    $summaryList.Items.Add($item) | Out-Null
  }

  $Metadata.Attributes.GetEnumerator() |
    Sort-Object -Property Name |
    ForEach-Object {
      $item = [System.Windows.Forms.ListViewItem]::new($_.Name)
      $item.SubItems.Add((Format-Field -Value $_.Value)) | Out-Null
      $rawList.Items.Add($item) | Out-Null
    }

  Resize-DetailColumns
  Set-Status "Selected $($Metadata.FileName)"
}

function Search-Songs {
  param([string]$Query)

  if ([string]::IsNullOrWhiteSpace($script:CurrentRootPath)) {
    return
  }

  if ([string]::IsNullOrWhiteSpace($Query)) {
    $script:IsSearchMode = $false
    if ($null -ne $folderTree.SelectedNode) {
      Load-SongsForFolder -FolderPath ([string]$folderTree.SelectedNode.Tag)
    }
    return
  }

  $script:IsSearchMode = $true
  $songList.Rows.Clear()
  $summaryList.Items.Clear()
  $rawList.Items.Clear()
  Set-Status "Searching..."

  $count = 0
  try {
    $songFiles = Get-ChildItem -LiteralPath $script:CurrentRootPath -Recurse -File -Filter "*.song" -ErrorAction SilentlyContinue |
      Where-Object { Test-RegularSongFile -FileName $_.Name } |
      Sort-Object -Property FullName

    foreach ($song in $songFiles) {
      try {
        $metadata = Get-SongMetainfo -SongPath $song.FullName
        $match = Get-SongSearchMatch -Metadata $metadata -Query $Query
        if ($null -ne $match) {
          Add-SongListItem -Metadata $metadata -Match $match
          $count += 1
        }
      } catch {
        continue
      }
    }
  } finally {
  }

  Resize-SongColumns
  if ($songList.Rows.Count -gt 0) {
    $songList.Refresh()
    [System.Windows.Forms.Application]::DoEvents()
    $songList.ClearSelection()
    $songList.FirstDisplayedScrollingRowIndex = 0
    Show-MetadataDetails -Metadata $songList.Rows[0].Tag
  }

  Set-Status "Found $count song(s) matching '$Query'; displayed $($songList.Rows.Count)"
}

$folderTree.Add_BeforeExpand({
  param($sender, $eventArgs)
  Populate-FolderChildren -Node $eventArgs.Node
})

$folderTree.Add_AfterSelect({
  param($sender, $eventArgs)
  if ($script:IsSearchMode) {
    return
  }

  $script:IsSearchMode = $false
  $searchTextBox.Text = ""
  Load-SongsForFolder -FolderPath ([string]$eventArgs.Node.Tag)
})

$songList.Add_SelectionChanged({
  if ($songList.SelectedRows.Count -eq 0) {
    return
  }

  $metadata = $songList.SelectedRows[0].Tag
  if ($null -eq $metadata) {
    return
  }

  Show-MetadataDetails -Metadata $metadata
})

$browseButton.Add_Click({
  $dialog = [System.Windows.Forms.FolderBrowserDialog]::new()
  $dialog.Description = "Select your Studio One songs folder"
  if (-not [string]::IsNullOrWhiteSpace($script:CurrentRootPath)) {
    $dialog.SelectedPath = $script:CurrentRootPath
  }

  if ($dialog.ShowDialog($form) -eq [System.Windows.Forms.DialogResult]::OK) {
    Set-RootPath -Path $dialog.SelectedPath
  }
})

$refreshButton.Add_Click({
  if (-not [string]::IsNullOrWhiteSpace($script:CurrentRootPath)) {
    Set-RootPath -Path $script:CurrentRootPath
  }
})

$searchButton.Add_Click({
  Search-Songs -Query $searchTextBox.Text
})

$searchTextBox.Add_KeyDown({
  param($sender, $eventArgs)
  if ($eventArgs.KeyCode -eq [System.Windows.Forms.Keys]::Enter) {
    Search-Songs -Query $searchTextBox.Text
    $eventArgs.SuppressKeyPress = $true
  }
})

$form.Add_Shown({
  $mainSplit.Panel1MinSize = 160
  $mainSplit.Panel2MinSize = 260
  $mainSplit.SplitterDistance = 310
  $rightSplit.Panel1MinSize = 140
  $rightSplit.Panel2MinSize = 160
  $rightSplit.SplitterDistance = 280
})

$initialRootPath = Resolve-InitialRootPath -RequestedRootPath $RootPath
if (-not [string]::IsNullOrWhiteSpace($initialRootPath) -and (Test-Path -LiteralPath $initialRootPath -PathType Container)) {
  Set-RootPath -Path $initialRootPath
} else {
  Set-Status "Choose a songs folder to begin"
}

[System.Windows.Forms.Application]::EnableVisualStyles()
[System.Windows.Forms.Application]::Run($form)
