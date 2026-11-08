namespace SongMetainfoBrowser.App;

/// <summary>
/// Centralizes product-facing identity and build-display helpers.
/// </summary>
public static class AppInfo
{
    public static string ProductName => "SongLens";

    public static string GetVersionText()
    {
        var applicationAssembly = System.Reflection.Assembly.GetEntryAssembly() ?? typeof(AppInfo).Assembly;
        return applicationAssembly.GetName().Version?.ToString() ?? "dev";
    }

    public static string GetBuildText()
    {
        var applicationAssembly = System.Reflection.Assembly.GetEntryAssembly() ?? typeof(AppInfo).Assembly;
        var embeddedBuildTimestamp = applicationAssembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), inherit: false)
            .OfType<System.Reflection.AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "BuildTimestamp")?.Value;
        var buildStamp = DateTime.TryParse(
            embeddedBuildTimestamp,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var parsedBuildStamp)
            ? parsedBuildStamp.ToLocalTime()
            : GetExecutableBuildTimestamp();
        var buildTime = buildStamp == DateTime.MinValue ? "" : DateTimeDisplay.Format(buildStamp);
        var version = GetVersionText();

        return string.IsNullOrWhiteSpace(buildTime)
            ? $"Build {version}"
            : $"Build {version}  {buildTime}";
    }

    private static DateTime GetExecutableBuildTimestamp()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            var fallbackPath = Path.Combine(AppContext.BaseDirectory, $"{ProductName}.exe");
            executablePath = File.Exists(fallbackPath) ? fallbackPath : null;
        }

        return string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath)
            ? DateTime.MinValue
            : File.GetLastWriteTime(executablePath);
    }
}
