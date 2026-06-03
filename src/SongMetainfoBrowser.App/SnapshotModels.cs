using System.Text.Json.Serialization;

namespace SongMetainfoBrowser.App;

internal enum SnapshotFormat
{
    Text,
    Json
}

internal sealed class SnapshotSectionSelection
{
    public bool IncludeSummary { get; init; } = true;
    public bool IncludeAttributes { get; init; }
    public bool IncludeTracks { get; init; } = true;
    public bool IncludeNotes { get; init; } = true;
    public SnapshotFormat Format { get; init; } = SnapshotFormat.Text;

    public bool HasAnySection =>
        IncludeSummary ||
        IncludeAttributes ||
        IncludeTracks ||
        IncludeNotes;
}

internal sealed class SongSnapshot
{
    public int SnapshotVersion { get; init; } = 1;
    public string CapturedAt { get; init; } = "";
    public string App { get; init; } = "SongLens";
    public required SongSnapshotSong Song { get; init; }
    public required SongSnapshotSections Sections { get; init; }
}

internal sealed class SongSnapshotSong
{
    public string FileName { get; init; } = "";
    public string? Title { get; init; }
    public string? Artist { get; init; }
    public string Path { get; init; } = "";
}

internal sealed class SongSnapshotSections
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string?>? Summary { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Attributes { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SongSnapshotTrack>? Tracks { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SongSnapshotNotes? Notes { get; init; }
}

internal sealed class SongSnapshotTrack
{
    public string TrackName { get; init; } = "";
    public string? InstrumentName { get; init; }
    public string? TrackNote { get; init; }
}

internal sealed class SongSnapshotNotes
{
    public string? Text { get; init; }
}
