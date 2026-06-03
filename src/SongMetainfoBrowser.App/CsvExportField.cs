namespace SongMetainfoBrowser.App;

internal sealed class CsvExportField
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required Func<SongMetadata, SearchResult?, string?> ValueSelector { get; init; }
    public bool IsDefault { get; init; }
}
