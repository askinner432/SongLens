namespace SongMetainfoBrowser.App;

/// <summary>
/// Centralizes product-facing identity and build-display helpers.
/// </summary>
internal static class AppInfo
{
    public static string ProductName => "SongLens";

    public static string GetVersionText()
    {
        return typeof(MainForm).Assembly.GetName().Version?.ToString(3) ?? "dev";
    }

    public static string GetBuildText()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            var fallbackPath = Path.Combine(AppContext.BaseDirectory, $"{ProductName}.exe");
            executablePath = File.Exists(fallbackPath) ? fallbackPath : null;
        }

        var buildStamp = string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath)
            ? DateTime.MinValue
            : File.GetLastWriteTime(executablePath);
        var buildTime = buildStamp == DateTime.MinValue ? "" : DateTimeDisplay.Format(buildStamp);
        var version = GetVersionText();

        return string.IsNullOrWhiteSpace(buildTime)
            ? $"Build {version}"
            : $"Build {version}  {buildTime}";
    }
}
