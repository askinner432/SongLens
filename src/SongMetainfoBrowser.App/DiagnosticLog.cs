namespace SongMetainfoBrowser.App;

/// <summary>
/// Writes lightweight diagnostics for file-read issues without interrupting the UI.
/// </summary>
internal static class DiagnosticLog
{
    public static void Reset()
    {
        TryWrite(string.Empty, append: false);
    }

    public static void WriteEvent(string message)
    {
        var line = $"[{DateTime.Now:O}] {message}{Environment.NewLine}";
        TryWrite(line, append: true);
    }

    public static void WriteSongReadFailure(string operation, string songPath, Exception ex)
    {
        WriteEvent($"{operation}: {songPath}{Environment.NewLine}{ex}");
    }

    private static void TryWrite(string contents, bool append)
    {
        try
        {
            if (append)
            {
                File.AppendAllText(AppPaths.DiagnosticLogPath, contents);
            }
            else
            {
                File.WriteAllText(AppPaths.DiagnosticLogPath, contents);
            }
        }
        catch
        {
            // Diagnostics should never break the app.
        }
    }
}
