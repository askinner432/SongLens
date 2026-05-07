namespace SongMetainfoBrowser.App;

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
    public required IReadOnlyList<string> MediaTrackNames { get; init; }
    public required IReadOnlyList<TrackInstrumentInfo> TrackInstruments { get; init; }
    public required IReadOnlyList<MusicPartInfo> MusicParts { get; init; }
    public required IReadOnlyDictionary<string, string> Attributes { get; init; }
}

public sealed class TrackInstrumentInfo
{
    public required string TrackName { get; init; }
    public string? InstrumentName { get; init; }
}

public sealed class MusicPartInfo
{
    public required string TrackName { get; init; }
    public required string PartName { get; init; }
}

public sealed class SearchResult
{
    public required SongMetadata Metadata { get; init; }
    public string MatchField { get; init; } = "";
    public string MatchValue { get; init; } = "";
}
