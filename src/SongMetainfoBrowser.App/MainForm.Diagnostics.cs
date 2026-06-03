namespace SongMetainfoBrowser.App;

public sealed partial class MainForm
{
    private static void LogSongReadFailure(string operation, string songPath, Exception ex)
    {
        DiagnosticLog.WriteSongReadFailure(operation, songPath, ex);
    }

    private static string FormatSkippedFilesSuffix(int skipped)
    {
        return skipped > 0
            ? $" Skipped {skipped} unreadable file(s)."
            : string.Empty;
    }
}
