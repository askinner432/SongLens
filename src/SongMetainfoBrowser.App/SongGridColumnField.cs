namespace SongMetainfoBrowser.App;

internal sealed class SongGridColumnField
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required string ColumnName { get; init; }
    public required Func<SongMetadata, SearchResult?, string?> ValueSelector { get; init; }
    public bool IsDefault { get; init; }
    public int Width { get; init; }
}
