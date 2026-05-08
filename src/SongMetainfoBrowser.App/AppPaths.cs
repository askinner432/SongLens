namespace SongMetainfoBrowser.App;

internal static class AppPaths
{
    public static string DataDirectory => EnsureDataDirectory();

    public static string ConfigPath => Path.Combine(DataDirectory, "song-metainfo-browser.config.json");

    public static string StartupLogPath => Path.Combine(DataDirectory, "startup.log");

    public static string StartupErrorLogPath => Path.Combine(DataDirectory, "startup-error.log");

    private static string EnsureDataDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDirectory = Path.Combine(localAppData, AppInfo.ProductName);
        Directory.CreateDirectory(dataDirectory);
        return dataDirectory;
    }
}
