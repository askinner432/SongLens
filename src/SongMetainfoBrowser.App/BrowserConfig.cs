using System.Text.Json;

namespace SongMetainfoBrowser.App;

/// <summary>
/// Serialized user preferences for SongLens.
/// </summary>
internal sealed class BrowserConfig
{
    public string? RootPath { get; set; }
    public string? Theme { get; set; }
    public int? FontSizePoints { get; set; }
    public Dictionary<string, int>? FontSizes { get; set; }
    public string? StudioOne7Path { get; set; }
    public string? FenderStudioPro8Path { get; set; }
    public bool? EnableSongGridContextMenu { get; set; }
    public bool? EnableSongLaunch { get; set; }
    public bool? ShowEnableSongLaunchPreference { get; set; }
    public Dictionary<string, Dictionary<string, int>>? GridColumnWidths { get; set; }
    public List<string>? CsvExportFieldKeys { get; set; }
    public List<string>? SongGridVisibleColumnKeys { get; set; }
    public List<SavedAdvancedSearch>? SavedAdvancedSearches { get; set; }
    public AdvancedSearchQuery? LastAdvancedSearchQuery { get; set; }
    public bool? LockCurrentDetailTab { get; set; }
    public int? LastSelectedDetailTabIndex { get; set; }
    public bool? ViewAllSongs { get; set; }
    public bool? RestoreFilterSessionOnStartup { get; set; }
    public bool? RestoreAdvancedSearchSessionOnStartup { get; set; }
    public string? SongAgeFilterOperator { get; set; }
    public int? SongAgeFilterDays { get; set; }
    public int? PreferencesWindowWidth { get; set; }
    public int? PreferencesWindowHeight { get; set; }
    public int? MainWindowWidth { get; set; }
    public int? MainWindowHeight { get; set; }
}

/// <summary>
/// Loads and saves user settings, with compatibility for older repo-local configs.
/// </summary>
internal static class BrowserConfigStore
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

    public static void SaveEnableSongGridContextMenu(bool isEnabled)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.EnableSongGridContextMenu = isEnabled;
        SaveConfig(config);
    }

    public static bool LoadEnableSongLaunch()
    {
        var config = LoadConfig();
        if (config?.EnableSongLaunch is bool isEnabled)
        {
            return isEnabled;
        }

        return config?.EnableSongGridContextMenu ?? false;
    }

    public static bool HasExplicitEnableSongLaunchPreference()
    {
        var config = LoadConfig();
        return config?.ShowEnableSongLaunchPreference == true
            || config?.EnableSongLaunch == true
            || config?.EnableSongGridContextMenu == true;
    }

    public static void SaveEnableSongLaunch(bool isEnabled)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        var hadLaunchAccess = config.ShowEnableSongLaunchPreference == true
            || config.EnableSongLaunch == true
            || config.EnableSongGridContextMenu == true;
        config.EnableSongLaunch = isEnabled;
        if (isEnabled || hadLaunchAccess)
        {
            config.ShowEnableSongLaunchPreference = true;
        }

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

    public static IReadOnlyList<SavedAdvancedSearch> LoadSavedAdvancedSearches()
    {
        var config = LoadConfig();
        if (config?.SavedAdvancedSearches is null || config.SavedAdvancedSearches.Count == 0)
        {
            return Array.Empty<SavedAdvancedSearch>();
        }

        return config.SavedAdvancedSearches
            .Where(search => search is not null
                && !string.IsNullOrWhiteSpace(search.Name)
                && search.Query is not null
                && search.Query.Rules is not null
                && search.Query.Rules.Count > 0)
            .GroupBy(search => search.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new SavedAdvancedSearch
            {
                Name = group.First().Name.Trim(),
                Query = CloneAdvancedSearchQuery(group.First().Query)
            })
            .OrderBy(search => search.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public static void SaveSavedAdvancedSearches(IReadOnlyList<SavedAdvancedSearch> searches)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SavedAdvancedSearches = searches
            .Where(search => search is not null
                && !string.IsNullOrWhiteSpace(search.Name)
                && search.Query is not null
                && search.Query.Rules is not null
                && search.Query.Rules.Count > 0)
            .GroupBy(search => search.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new SavedAdvancedSearch
            {
                Name = group.First().Name.Trim(),
                Query = CloneAdvancedSearchQuery(group.First().Query)
            })
            .OrderBy(search => search.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        SaveConfig(config);
    }

    public static AdvancedSearchQuery? LoadLastAdvancedSearchQuery()
    {
        var config = LoadConfig();
        if (config?.LastAdvancedSearchQuery?.Rules is null || config.LastAdvancedSearchQuery.Rules.Count == 0)
        {
            return null;
        }

        return CloneAdvancedSearchQuery(config.LastAdvancedSearchQuery);
    }

    public static void SaveLastAdvancedSearchQuery(AdvancedSearchQuery? query)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.LastAdvancedSearchQuery = query is null || query.Rules is null || query.Rules.Count == 0
            ? null
            : CloneAdvancedSearchQuery(query);
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

    public static int? LoadLastSelectedDetailTabIndex()
    {
        var config = LoadConfig();
        return config?.LastSelectedDetailTabIndex is >= 0 ? config.LastSelectedDetailTabIndex : null;
    }

    public static void SaveLastSelectedDetailTabIndex(int tabIndex)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.LastSelectedDetailTabIndex = tabIndex;
        SaveConfig(config);
    }

    public static bool LoadViewAllSongs()
    {
        var config = LoadConfig();
        return config?.ViewAllSongs ?? false;
    }

    public static void SaveViewAllSongs(bool viewAllSongs)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.ViewAllSongs = viewAllSongs;
        SaveConfig(config);
    }

    public static bool LoadRestoreFilterSessionOnStartup()
    {
        var config = LoadConfig();
        return config?.RestoreFilterSessionOnStartup ?? true;
    }

    public static void SaveRestoreFilterSessionOnStartup(bool restoreOnStartup)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.RestoreFilterSessionOnStartup = restoreOnStartup;
        SaveConfig(config);
    }

    public static bool LoadRestoreAdvancedSearchSessionOnStartup()
    {
        var config = LoadConfig();
        return config?.RestoreAdvancedSearchSessionOnStartup ?? false;
    }

    public static void SaveRestoreAdvancedSearchSessionOnStartup(bool restoreOnStartup)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.RestoreAdvancedSearchSessionOnStartup = restoreOnStartup;
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

    internal static void SaveSongAgeFilterPreference(SongAgeFilter? filter)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SongAgeFilterOperator = filter?.Operator.ToString();
        config.SongAgeFilterDays = filter?.Days;
        SaveConfig(config);
    }

    public static Size? LoadPreferencesWindowSize()
    {
        var config = LoadConfig();
        if (config?.PreferencesWindowWidth is not > 0 || config.PreferencesWindowHeight is not > 0)
        {
            return null;
        }

        return new Size(config.PreferencesWindowWidth.Value, config.PreferencesWindowHeight.Value);
    }

    public static void SavePreferencesWindowSize(Size clientSize)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.PreferencesWindowWidth = clientSize.Width;
        config.PreferencesWindowHeight = clientSize.Height;
        SaveConfig(config);
    }

    public static Size? LoadMainWindowSize()
    {
        var config = LoadConfig();
        if (config?.MainWindowWidth is not > 0 || config.MainWindowHeight is not > 0)
        {
            return null;
        }

        return new Size(config.MainWindowWidth.Value, config.MainWindowHeight.Value);
    }

    public static void SaveMainWindowSize(Size size)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.MainWindowWidth = size.Width;
        config.MainWindowHeight = size.Height;
        SaveConfig(config);
    }

    private static BrowserConfig? LoadConfig()
    {
        EnsureConfigLoaded();
        return _cachedConfig;
    }

    private static void SaveConfig(BrowserConfig config)
    {
        config.EnableSongLaunch ??= false;
        config.RestoreAdvancedSearchSessionOnStartup ??= false;
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

    private static AdvancedSearchQuery CloneAdvancedSearchQuery(AdvancedSearchQuery query)
    {
        return new AdvancedSearchQuery
        {
            MatchMode = query.MatchMode,
            Rules = query.Rules
                .Select(rule => new AdvancedSearchRule
                {
                    FieldKey = rule.FieldKey,
                    Operator = rule.Operator,
                    ValueText = rule.ValueText,
                    NumberValue = rule.NumberValue,
                    DateValue = rule.DateValue
                })
                .ToArray()
        };
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
