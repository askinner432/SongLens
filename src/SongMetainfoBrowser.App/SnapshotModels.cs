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
    public bool IncludeGroups { get; init; } = true;
    public bool IncludeMixer { get; init; } = true;
    public bool IncludePresets { get; init; }
    public bool IncludeNotes { get; init; } = true;
    public SnapshotFormat Format { get; init; } = SnapshotFormat.Text;

    public bool HasAnySection =>
        IncludeSummary ||
        IncludeAttributes ||
        IncludeTracks ||
        IncludeGroups ||
        IncludeMixer ||
        IncludePresets ||
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
    public List<SongSnapshotGroup>? Groups { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SongSnapshotMixer? Mixer { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SongSnapshotPreset>? Presets { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SongSnapshotNotes? Notes { get; init; }
}

internal sealed class SongSnapshotTrack
{
    public string TrackName { get; init; } = "";
    public string? InstrumentName { get; init; }
    public string? TrackNote { get; init; }
}

internal sealed class SongSnapshotGroup
{
    public string GroupName { get; init; } = "";
    public List<string>? TrackNames { get; init; }
}

internal sealed class SongSnapshotMixer
{
    public List<SongSnapshotMixerMain>? Main { get; init; }
    public List<SongSnapshotMixerInsert>? Inserts { get; init; }
    public List<SongSnapshotMixerSend>? Sends { get; init; }
}

internal sealed class SongSnapshotMixerMain
{
    public string ChannelName { get; init; } = "";
    public string? Pre { get; init; }
    public string? Post { get; init; }
}

internal sealed class SongSnapshotMixerInsert
{
    public string ChannelName { get; init; } = "";
    public string SlotName { get; init; } = "";
    public string? PluginName { get; init; }
    public string? PresetName { get; init; }
}

internal sealed class SongSnapshotMixerSend
{
    public string ChannelName { get; init; } = "";
    public string SlotName { get; init; } = "";
    public string? DestinationName { get; init; }
    public string? PresetName { get; init; }
}

internal sealed class SongSnapshotPreset
{
    public string Col1 { get; init; } = "";
    public string Col2 { get; init; } = "";
}

internal sealed class SongSnapshotNotes
{
    public string? Text { get; init; }
}
