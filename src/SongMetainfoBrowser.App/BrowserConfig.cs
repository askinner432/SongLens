using System.Text.Json;

namespace SongMetainfoBrowser.App;

/// <summary>
/// Serialized user preferences for SongLens.
/// </summary>
public sealed class BrowserConfig
{
    public string? RootPath { get; set; }
    public string? Theme { get; set; }
    public Dictionary<string, Dictionary<string, int>>? GridColumnWidths { get; set; }
}

/// <summary>
/// Loads and saves user settings, with compatibility for older repo-local configs.
/// </summary>
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

    public static IReadOnlyDictionary<string, int> LoadGridColumnWidths(string gridKey)
    {
        var config = LoadConfig();
        if (config?.GridColumnWidths is null || !config.GridColumnWidths.TryGetValue(gridKey, out var widths))
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, int>(widths, StringComparer.OrdinalIgnoreCase);
    }

    public static bool HasSavedGridColumnWidths(string gridKey)
    {
        var config = LoadConfig();
        return config?.GridColumnWidths is not null && config.GridColumnWidths.ContainsKey(gridKey);
    }

    public static void SaveGridColumnWidths(string gridKey, IReadOnlyDictionary<string, int> widths)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.GridColumnWidths ??= new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        config.GridColumnWidths[gridKey] = new Dictionary<string, int>(widths, StringComparer.OrdinalIgnoreCase);
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
        var userConfigPath = AppPaths.ConfigPath;
        if (File.Exists(userConfigPath))
        {
            return userConfigPath;
        }

        // Development builds may still run from the repo root, so walk upward
        // to find the legacy config file before falling back to LocalAppData.
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

        return userConfigPath;
    }
}
