namespace SongMetainfoBrowser.App;

public sealed partial class MainForm
{
    private static void LogSongReadFailure(string operation, string songPath, Exception ex)
    {
        DiagnosticLog.WriteSongReadFailure(operation, songPath, ex);
    }

    private static void LogSongReadWarnings(SongMetadata metadata)
    {
        foreach (var warning in metadata.ReadWarnings)
        {
            DiagnosticLog.WriteEvent(
                $"SongMetadataReader: {metadata.Path}{Environment.NewLine}" +
                $"Archive entry: {warning.ArchiveEntry}{Environment.NewLine}" +
                warning.Message);
        }
    }

    private static string FormatSkippedFilesSuffix(int skipped)
    {
        return skipped > 0
            ? $" Skipped {skipped} unreadable file(s)."
            : string.Empty;
    }
}
