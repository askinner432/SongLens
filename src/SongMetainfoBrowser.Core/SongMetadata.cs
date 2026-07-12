namespace SongMetainfoBrowser.App;

/// <summary>
/// Normalized metadata extracted from a Studio One .song archive and ready for UI display.
/// </summary>
public sealed class SongMetadata
{
    public required string Path { get; init; }
    public required string FileName { get; init; }
    public required string Folder { get; init; }
    public string? Title { get; init; }
    public string? Artist { get; init; }
    public string? Year { get; init; }
    public string? DateCreated { get; init; }
    public string? LastModified { get; init; }
    public string? Tempo { get; init; }
    public string? KeySignature { get; init; }
    public string? TimeSignature { get; init; }
    public string? SampleRate { get; init; }
    public string? BitDepth { get; init; }
    public string? TrackCount { get; init; }
    public string? Length { get; init; }
    public string? Generator { get; init; }
    public string? FormatVersion { get; init; }
    public string? NotesFile { get; init; }
    public string? NotesText { get; init; }
    public string? ArtworkFile { get; init; }
    public string? Comment { get; init; }
    public required IReadOnlyList<string> Presets { get; init; }
    public required IReadOnlyList<string> MediaTrackNames { get; init; }
    public required IReadOnlyList<TrackInstrumentInfo> TrackInstruments { get; init; }
    public required IReadOnlyList<SongGroupInfo> Groups { get; init; }
    public required IReadOnlyList<MixerMainInfo> MixerMainChannels { get; init; }
    public required IReadOnlyList<MixerInsertInfo> MixerInserts { get; init; }
    public required IReadOnlyList<MixerSendInfo> MixerSends { get; init; }
    public required IReadOnlyList<MusicPartInfo> MusicParts { get; init; }
    public required IReadOnlyDictionary<string, string> Attributes { get; init; }
}

/// <summary>
/// Track-level display information merged from song structure, device routing, and notepad data.
/// </summary>
public sealed class TrackInstrumentInfo
{
    public required string TrackName { get; init; }
    public string? InstrumentName { get; init; }
    public string? TrackNote { get; init; }
    public bool HasEvents { get; init; }
}

/// <summary>
/// Lightweight representation of a song folder/group and its descendant track names.
/// </summary>
public sealed class SongGroupInfo
{
    public required string GroupName { get; init; }
    public required IReadOnlyList<string> TrackNames { get; init; }
}

/// <summary>
/// Lightweight representation of a music part discovered in song.xml.
/// </summary>
public sealed class MusicPartInfo
{
    public required string TrackName { get; init; }
    public required string PartName { get; init; }
}

/// <summary>
/// Lightweight representation of a main output mixer channel.
/// </summary>
public sealed class MixerMainInfo
{
    public required string ChannelName { get; init; }
    public string? PrePluginChain { get; init; }
    public string? PostPluginChain { get; init; }
}

/// <summary>
/// Lightweight representation of a mixer insert slot.
/// </summary>
public sealed class MixerInsertInfo
{
    public required string ChannelName { get; init; }
    public required string SlotName { get; init; }
    public string? PluginName { get; init; }
    public string? PresetName { get; init; }
    public string? PresetPath { get; init; }
    public bool IsBypassed { get; init; }
}

/// <summary>
/// Lightweight representation of a mixer send slot.
/// </summary>
public sealed class MixerSendInfo
{
    public required string ChannelName { get; init; }
    public required string SlotName { get; init; }
    public string? DestinationName { get; init; }
    public string? PresetName { get; init; }
    public bool IsPreFader { get; init; }
    public string? Level { get; init; }
    public string? Pan { get; init; }
    public bool IsBypassed { get; init; }
}

/// <summary>
/// Describes a single search hit so the UI can show both the song and the matched field.
/// </summary>
public sealed class SearchResult
{
    public required SongMetadata Metadata { get; init; }
    public string MatchField { get; init; } = "";
    public string MatchValue { get; init; } = "";
}
