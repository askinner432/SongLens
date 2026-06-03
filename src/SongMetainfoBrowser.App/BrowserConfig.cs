using System.Text.Json;

namespace SongMetainfoBrowser.App;

/// <summary>
/// Serialized user preferences for SongLens.
/// </summary>
public sealed class BrowserConfig
{
    public string? RootPath { get; set; }
    public string? Theme { get; set; }
    public int? FontSizePoints { get; set; }
    public Dictionary<string, int>? FontSizes { get; set; }
    public string? StudioOne7Path { get; set; }
    public string? FenderStudioPro8Path { get; set; }
    public bool? EnableSongGridContextMenu { get; set; }
    public Dictionary<string, Dictionary<string, int>>? GridColumnWidths { get; set; }
    public List<string>? CsvExportFieldKeys { get; set; }
    public List<string>? SongGridVisibleColumnKeys { get; set; }
    public bool? LockCurrentDetailTab { get; set; }
    public string? SongAgeFilterOperator { get; set; }
    public int? SongAgeFilterDays { get; set; }
}

/// <summary>
/// Loads and saves user settings, with compatibility for older repo-local configs.
/// </summary>
public static class BrowserConfigStore
{
    public static string ConfigPath { get; } = FindConfigPath();
    private static bool _configLoaded;
    private static BrowserConfig? _cachedConfig;
    private static string? _configLoadWarning;

    public static string? GetConfigLoadWarning()
    {
        EnsureConfigLoaded();
        return _configLoadWarning;
    }

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

    public static int? LoadFontSizePoints()
    {
        var config = LoadConfig();
        return config?.FontSizePoints;
    }

    public static IReadOnlyDictionary<string, int> LoadFontSizes()
    {
        var config = LoadConfig();
        if (config?.FontSizes is null || config.FontSizes.Count == 0)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, int>(config.FontSizes, StringComparer.OrdinalIgnoreCase);
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

    public static void SaveFontSizePoints(int fontSizePoints)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.FontSizePoints = fontSizePoints;
        SaveConfig(config);
    }

    public static void SaveFontSizes(IReadOnlyDictionary<string, int> fontSizes)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.FontSizes = new Dictionary<string, int>(fontSizes, StringComparer.OrdinalIgnoreCase);
        SaveConfig(config);
    }

    public static string? LoadStudioOne7Path()
    {
        var config = LoadConfig();
        return string.IsNullOrWhiteSpace(config?.StudioOne7Path) ? null : config.StudioOne7Path;
    }

    public static void SaveStudioOne7Path(string executablePath)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.StudioOne7Path = executablePath;
        SaveConfig(config);
    }

    public static string? LoadFenderStudioPro8Path()
    {
        var config = LoadConfig();
        return string.IsNullOrWhiteSpace(config?.FenderStudioPro8Path) ? null : config.FenderStudioPro8Path;
    }

    public static void SaveFenderStudioPro8Path(string executablePath)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.FenderStudioPro8Path = executablePath;
        SaveConfig(config);
    }

    public static bool LoadEnableSongGridContextMenu()
    {
        var config = LoadConfig();
        return config?.EnableSongGridContextMenu ?? false;
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

    public static IReadOnlyList<string> LoadCsvExportFieldKeys()
    {
        var config = LoadConfig();
        if (config?.CsvExportFieldKeys is null || config.CsvExportFieldKeys.Count == 0)
        {
            return Array.Empty<string>();
        }

        return config.CsvExportFieldKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static void SaveCsvExportFieldKeys(IReadOnlyList<string> fieldKeys)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.CsvExportFieldKeys = fieldKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        SaveConfig(config);
    }

    public static IReadOnlyList<string> LoadSongGridVisibleColumnKeys()
    {
        var config = LoadConfig();
        if (config?.SongGridVisibleColumnKeys is null || config.SongGridVisibleColumnKeys.Count == 0)
        {
            return Array.Empty<string>();
        }

        return config.SongGridVisibleColumnKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static void SaveSongGridVisibleColumnKeys(IReadOnlyList<string> fieldKeys)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SongGridVisibleColumnKeys = fieldKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        SaveConfig(config);
    }

    public static bool LoadLockCurrentDetailTab()
    {
        var config = LoadConfig();
        return config?.LockCurrentDetailTab ?? false;
    }

    public static void SaveLockCurrentDetailTab(bool isLocked)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.LockCurrentDetailTab = isLocked;
        SaveConfig(config);
    }

    internal static SongAgeFilter? LoadSongAgeFilterPreference()
    {
        var config = LoadConfig();
        if (config?.SongAgeFilterDays is null || config.SongAgeFilterDays <= 0)
        {
            return null;
        }

        var filterOperator = string.Equals(config.SongAgeFilterOperator, nameof(SongAgeFilterOperator.OlderThan), StringComparison.OrdinalIgnoreCase)
            ? SongAgeFilterOperator.OlderThan
            : SongAgeFilterOperator.LessThan;

        return new SongAgeFilter
        {
            Operator = filterOperator,
            Days = config.SongAgeFilterDays.Value
        };
    }

    internal static void SaveSongAgeFilterPreference(SongAgeFilter filter)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SongAgeFilterOperator = filter.Operator.ToString();
        config.SongAgeFilterDays = filter.Days;
        SaveConfig(config);
    }

    private static BrowserConfig? LoadConfig()
    {
        EnsureConfigLoaded();
        return _cachedConfig;
    }

    private static void SaveConfig(BrowserConfig config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
        _cachedConfig = config;
        _configLoadWarning = null;
        _configLoaded = true;
    }

    private static void EnsureConfigLoaded()
    {
        if (_configLoaded)
        {
            return;
        }

        _configLoaded = true;
        _cachedConfig = null;
        _configLoadWarning = null;

        if (!File.Exists(ConfigPath))
        {
            return;
        }

        try
        {
            _cachedConfig = JsonSerializer.Deserialize<BrowserConfig>(File.ReadAllText(ConfigPath));
        }
        catch (Exception ex)
        {
            _configLoadWarning =
                $"SongLens could not read its config file and will continue with defaults until the file is fixed or saved again.\n\nConfig file:\n{ConfigPath}\n\nError:\n{ex.Message}";
        }
    }

    private static string FindConfigPath()
    {
        var userConfigPath = AppPaths.ConfigPath;
        // Development builds may run from a bin folder beneath the repo, so walk
        // upward first and prefer a repo-local config when one is present.
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

        if (File.Exists(userConfigPath))
        {
            return userConfigPath;
        }

        return userConfigPath;
    }
}
