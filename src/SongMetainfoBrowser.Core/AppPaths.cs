namespace SongMetainfoBrowser.App;

/// <summary>
/// Resolves writable per-user locations for settings and startup diagnostics.
/// Installed builds should never depend on write access beside the executable.
/// </summary>
public static class AppPaths
{
    public static string DataDirectory => EnsureDataDirectory();

    public static string ConfigPath => Path.Combine(DataDirectory, "song-metainfo-browser.config.json");

    public static string StartupLogPath => Path.Combine(DataDirectory, "startup.log");

    public static string StartupErrorLogPath => Path.Combine(DataDirectory, "startup-error.log");

    public static string DiagnosticLogPath => Path.Combine(DataDirectory, "diagnostic.log");

    private static string EnsureDataDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDirectory = Path.Combine(localAppData, AppInfo.ProductName);
        Directory.CreateDirectory(dataDirectory);
        return dataDirectory;
    }
}
