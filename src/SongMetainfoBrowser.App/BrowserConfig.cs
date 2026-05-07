using System.Text.Json;

namespace SongMetainfoBrowser.App;

public sealed class BrowserConfig
{
    public string? RootPath { get; set; }
    public string? Theme { get; set; }
}

public static class BrowserConfigStore
{
    public static string ConfigPath { get; } = FindConfigPath();

    public static string? LoadRootPath()
    {
        var config = LoadConfig();
        return string.IsNullOrWhiteSpace(config?.RootPath) ? null : config.RootPath;
    }

    public static string? LoadTheme()
    {
        var config = LoadConfig();
        return string.IsNullOrWhiteSpace(config?.Theme) ? null : config.Theme;
    }

    public static void SaveRootPath(string rootPath)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.RootPath = Path.GetFullPath(rootPath);
        SaveConfig(config);
    }

    public static void SaveTheme(string themeName)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.Theme = themeName;
        SaveConfig(config);
    }

    private static BrowserConfig? LoadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BrowserConfig>(File.ReadAllText(ConfigPath));
        }
        catch
        {
            return null;
        }
    }

    private static void SaveConfig(BrowserConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    private static string FindConfigPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "song-metainfo-browser.config.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "song-metainfo-browser.config.json");
    }
}
