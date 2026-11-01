using System.Text.Json;

namespace SongMetainfoBrowser.App;

internal enum SongGridDoubleClickAction
{
    RevealInExplorer,
    OpenSongInDaw
}

/// <summary>
/// Serialized user preferences for SongLens.
/// </summary>
internal sealed class BrowserConfig
{
    public string? RootPath { get; set; }
    public List<string>? RecentRootPaths { get; set; }
    public List<string>? RecentSongPaths { get; set; }
    public string? Theme { get; set; }
    public int? FontSizePoints { get; set; }
    public Dictionary<string, int>? FontSizes { get; set; }
    public string? StudioOne7Path { get; set; }
    public string? FenderStudioPro8Path { get; set; }
    public bool? UseCustomAudioPlayer { get; set; }
    public string? CustomAudioPlayerPath { get; set; }
    public string? CustomAudioPlayerArguments { get; set; }
    public string? MixdownFolderName { get; set; }
    public bool? EnableSongGridContextMenu { get; set; }
    public bool? EnableSongLaunch { get; set; }
    public bool? ShowEnableSongLaunchPreference { get; set; }
    public string? SongGridDoubleClickAction { get; set; }
    public Dictionary<string, Dictionary<string, int>>? GridColumnWidths { get; set; }
    public List<string>? SongGridColumnOrder { get; set; }
    public List<SongGridSortLevel>? SongGridSortLevels { get; set; }
    public List<string>? CsvExportFieldKeys { get; set; }
    public bool? SuppressCsvMixerExportInfo { get; set; }
    public bool? SuppressExcelMixerExportInfo { get; set; }
    public bool? SuppressBulkEditIntro { get; set; }
    public bool? SuppressRestoreDefaultLayoutWarning { get; set; }
    public bool? SuppressStatisticsChartSelectionInfo { get; set; }
    public List<string>? SongGridVisibleColumnKeys { get; set; }
    public bool? SongAnnotationColumnsInitialized { get; set; }
    public bool? SongGenreColumnInitialized { get; set; }
    public bool? SongAnnotationColumnsMovedToEnd { get; set; }
    public bool? PluginsDetailTabInitialized { get; set; }
    public List<string>? SongStatuses { get; set; }
    public List<string>? SongGenres { get; set; }
    public List<CustomSongColumn>? CustomSongColumns { get; set; }
    public bool? CustomSongColumnsAutoShown { get; set; }
    public List<SavedSongLensView>? SavedSongLensViews { get; set; }
    public List<string>? DetailTabVisibleKeys { get; set; }
    public bool? ShowTracksWithEventsOnly { get; set; }
    public bool? HideFolderTracks { get; set; }
    public bool? HideDisabledTracks { get; set; }
    public List<SavedAdvancedSearch>? SavedAdvancedSearches { get; set; }
    public AdvancedSearchQuery? LastAdvancedSearchQuery { get; set; }
    public bool? LockCurrentDetailTab { get; set; }
    public int? LastSelectedDetailTabIndex { get; set; }
    public bool? ViewAllSongs { get; set; }
    public bool? RestoreFilterSessionOnStartup { get; set; }
    public bool? RestoreAdvancedSearchSessionOnStartup { get; set; }
    public bool? UseHistoryRetentionPreset { get; set; }
    public int? HistoryRevisionsToKeep { get; set; }
    public string? SongAgeFilterMode { get; set; }
    public string? SongAgeFilterOperator { get; set; }
    public int? SongAgeFilterDays { get; set; }
    public string? SongAgeFilterDateField { get; set; }
    public string? SongAgeFilterStartDate { get; set; }
    public string? SongAgeFilterEndDate { get; set; }
    public int? PreferencesWindowWidth { get; set; }
    public int? PreferencesWindowHeight { get; set; }
    public int? MainWindowWidth { get; set; }
    public int? MainWindowHeight { get; set; }
    public Dictionary<string, SongAnnotation>? SongAnnotations { get; set; }
}

internal sealed class SongAnnotation
{
    public string? Status { get; set; }
    public string? Genre { get; set; }
    public int Rating { get; set; }
    public Dictionary<string, string>? CustomValues { get; set; }
}

internal sealed class CustomSongColumn
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

internal sealed class SavedSongLensView
{
    public string Name { get; set; } = "";
    public string? FastSearchText { get; set; }
    public AdvancedSearchQuery? AdvancedSearchQuery { get; set; }
    public List<string> VisibleColumnKeys { get; set; } = [];
    public List<string> ColumnOrder { get; set; } = [];
    public Dictionary<string, int> ColumnWidths { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string SortColumnName { get; set; } = "Song";
    public string SortDirection { get; set; } = nameof(System.ComponentModel.ListSortDirection.Ascending);
    public List<SongGridSortLevel>? SortLevels { get; set; }
    public SongAgeFilter? DateFilter { get; set; }
}

internal sealed class SongGridSortLevel
{
    public string ColumnName { get; set; } = "";
    public string Direction { get; set; } = nameof(System.ComponentModel.ListSortDirection.Ascending);
}

/// <summary>
/// Loads and saves user settings, with compatibility for older repo-local configs.
/// </summary>
internal static class BrowserConfigStore
{
    public static IReadOnlyList<string> DefaultSongStatuses { get; } =
    [
        "Unfinished",
        "In Progress",
        "Draft Complete",
        "Refining",
        "Mixing",
        "Ready for Review",
        "Finished",
        "On Hold",
        "Archived",
        "Remove/Delete"
    ];

    public static IReadOnlyList<string> DefaultSongGenres { get; } =
    [
        "Rock",
        "Pop",
        "Folk",
        "Jazz",
        "Guitar",
        "Solo Piano",
        "Orchestral",
        "Hybrid"
    ];

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

    public static IReadOnlyList<string> LoadRecentRootPaths()
    {
        var config = LoadConfig();
        if (config?.RecentRootPaths is null || config.RecentRootPaths.Count == 0)
        {
            return Array.Empty<string>();
        }

        return config.RecentRootPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
    }

    public static IReadOnlyList<string> LoadRecentSongPaths()
    {
        var config = LoadConfig();
        if (config?.RecentSongPaths is null || config.RecentSongPaths.Count == 0)
        {
            return Array.Empty<string>();
        }

        return config.RecentSongPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
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
        var fullPath = Path.GetFullPath(rootPath);
        config.RootPath = fullPath;
        config.RecentRootPaths = (config.RecentRootPaths ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Where(path => !string.Equals(path, fullPath, StringComparison.OrdinalIgnoreCase))
            .Prepend(fullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
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

    public static void SaveRecentSongPath(string songPath)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        var fullPath = Path.GetFullPath(songPath);
        config.RecentSongPaths = (config.RecentSongPaths ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Where(path => !string.Equals(path, fullPath, StringComparison.OrdinalIgnoreCase))
            .Prepend(fullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
        SaveConfig(config);
    }

    public static SongAnnotation LoadSongAnnotation(string songPath)
    {
        var config = LoadConfig();
        var fullPath = Path.GetFullPath(songPath);
        SongAnnotation? annotation = null;
        if (config?.SongAnnotations is not null
            && !config.SongAnnotations.TryGetValue(fullPath, out annotation))
        {
            annotation = config.SongAnnotations
                .FirstOrDefault(pair => string.Equals(pair.Key, fullPath, StringComparison.OrdinalIgnoreCase))
                .Value;
        }
        if (annotation is not null)
        {
            return new SongAnnotation
            {
                Status = annotation.Status,
                Genre = annotation.Genre,
                Rating = Math.Clamp(annotation.Rating, 0, 5),
                CustomValues = annotation.CustomValues is null
                    ? null
                    : new Dictionary<string, string>(annotation.CustomValues, StringComparer.OrdinalIgnoreCase)
            };
        }

        return new SongAnnotation();
    }

    public static bool HasSongStatusAssignment(string status)
    {
        return LoadConfig()?.SongAnnotations?.Values.Any(
            annotation => string.Equals(annotation.Status, status, StringComparison.OrdinalIgnoreCase)) == true;
    }

    public static bool HasSongGenreAssignment(string genre)
    {
        return LoadConfig()?.SongAnnotations?.Values.Any(
            annotation => string.Equals(annotation.Genre, genre, StringComparison.OrdinalIgnoreCase)) == true;
    }

    public static void SaveSongAnnotation(string songPath, string? status, string? genre, int rating)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        var fullPath = Path.GetFullPath(songPath);
        config.SongAnnotations = config.SongAnnotations is null
            ? new Dictionary<string, SongAnnotation>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, SongAnnotation>(config.SongAnnotations, StringComparer.OrdinalIgnoreCase);

        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        var normalizedGenre = string.IsNullOrWhiteSpace(genre) ? null : genre.Trim();
        var normalizedRating = Math.Clamp(rating, 0, 5);
        config.SongAnnotations.TryGetValue(fullPath, out var existingAnnotation);
        var customValues = existingAnnotation?.CustomValues;
        if (normalizedStatus is null && normalizedGenre is null && normalizedRating == 0
            && (customValues is null || customValues.Count == 0))
        {
            config.SongAnnotations.Remove(fullPath);
        }
        else
        {
            config.SongAnnotations[fullPath] = new SongAnnotation
            {
                Status = normalizedStatus,
                Genre = normalizedGenre,
                Rating = normalizedRating,
                CustomValues = customValues
            };
        }

        SaveConfig(config);
    }

    public static void SaveBulkSongAnnotationValue(IEnumerable<string> songPaths, bool setStatus, string? value)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SongAnnotations = config.SongAnnotations is null
            ? new Dictionary<string, SongAnnotation>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, SongAnnotation>(config.SongAnnotations, StringComparer.OrdinalIgnoreCase);
        var normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        foreach (var songPath in songPaths)
        {
            var fullPath = Path.GetFullPath(songPath);
            if (!config.SongAnnotations.TryGetValue(fullPath, out var annotation))
            {
                annotation = new SongAnnotation();
                config.SongAnnotations[fullPath] = annotation;
            }
            if (setStatus) annotation.Status = normalizedValue;
            else annotation.Genre = normalizedValue;
            if (string.IsNullOrWhiteSpace(annotation.Status)
                && string.IsNullOrWhiteSpace(annotation.Genre)
                && annotation.Rating == 0
                && (annotation.CustomValues is null || annotation.CustomValues.Count == 0))
                config.SongAnnotations.Remove(fullPath);
        }
        SaveConfig(config);
    }

    public static void SaveBulkSongRating(IEnumerable<string> songPaths, int rating)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SongAnnotations = config.SongAnnotations is null
            ? new Dictionary<string, SongAnnotation>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, SongAnnotation>(config.SongAnnotations, StringComparer.OrdinalIgnoreCase);
        var normalizedRating = Math.Clamp(rating, 0, 5);
        foreach (var songPath in songPaths)
        {
            var fullPath = Path.GetFullPath(songPath);
            if (!config.SongAnnotations.TryGetValue(fullPath, out var annotation))
            {
                annotation = new SongAnnotation();
                config.SongAnnotations[fullPath] = annotation;
            }
            annotation.Rating = normalizedRating;
            if (string.IsNullOrWhiteSpace(annotation.Status)
                && string.IsNullOrWhiteSpace(annotation.Genre)
                && annotation.Rating == 0
                && (annotation.CustomValues is null || annotation.CustomValues.Count == 0))
                config.SongAnnotations.Remove(fullPath);
        }
        SaveConfig(config);
    }

    public static IReadOnlyList<CustomSongColumn> LoadCustomSongColumns()
    {
        return (LoadConfig()?.CustomSongColumns ?? [])
            .Where(column => !string.IsNullOrWhiteSpace(column.Id) && !string.IsNullOrWhiteSpace(column.Name))
            .Select(column => new CustomSongColumn { Id = column.Id, Name = column.Name })
            .ToArray();
    }

    public static void EnsureExistingCustomSongColumnsVisible()
    {
        var config = LoadConfig() ?? new BrowserConfig();
        if (config.CustomSongColumnsAutoShown == true || config.CustomSongColumns is not { Count: > 0 })
            return;

        config.SongGridVisibleColumnKeys ??= [];
        if (config.SongGridVisibleColumnKeys.Count == 0)
        {
            config.SongGridVisibleColumnKeys.AddRange(
                ["playable", "song", "title", "artist", "year", "lastModified", "tempo",
                 "keySignature", "length", "comment", "genre", "status", "rating"]);
        }
        foreach (var column in config.CustomSongColumns)
        {
            var key = "Custom_" + column.Id;
            if (!config.SongGridVisibleColumnKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
                config.SongGridVisibleColumnKeys.Add(key);
        }
        config.CustomSongColumnsAutoShown = true;
        SaveConfig(config);
    }

    public static void SaveCustomSongColumns(IEnumerable<CustomSongColumn> columns)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        var selected = columns
            .Where(column => !string.IsNullOrWhiteSpace(column.Id) && !string.IsNullOrWhiteSpace(column.Name))
            .Select(column => new CustomSongColumn { Id = column.Id, Name = column.Name.Trim() })
            .ToList();
        var selectedIds = selected.Select(column => column.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removedIds = (config.CustomSongColumns ?? [])
            .Select(column => column.Id)
            .Where(id => !selectedIds.Contains(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (removedIds.Count > 0 && config.SongAnnotations is not null)
        {
            foreach (var annotation in config.SongAnnotations.Values)
            {
                if (annotation.CustomValues is null) continue;
                foreach (var id in removedIds) annotation.CustomValues.Remove(id);
            }
        }
        config.CustomSongColumns = selected;
        config.CustomSongColumnsAutoShown = true;
        SaveConfig(config);
    }

    public static void SaveCustomSongValue(string songPath, string columnId, string? value)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        var fullPath = Path.GetFullPath(songPath);
        config.SongAnnotations = config.SongAnnotations is null
            ? new Dictionary<string, SongAnnotation>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, SongAnnotation>(config.SongAnnotations, StringComparer.OrdinalIgnoreCase);
        if (!config.SongAnnotations.TryGetValue(fullPath, out var annotation))
        {
            annotation = new SongAnnotation();
            config.SongAnnotations[fullPath] = annotation;
        }
        annotation.CustomValues ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(value)) annotation.CustomValues.Remove(columnId);
        else annotation.CustomValues[columnId] = value.Trim();
        if (annotation.CustomValues.Count == 0 && string.IsNullOrWhiteSpace(annotation.Status)
            && string.IsNullOrWhiteSpace(annotation.Genre) && annotation.Rating == 0)
        {
            config.SongAnnotations.Remove(fullPath);
        }
        SaveConfig(config);
    }

    public static IReadOnlyList<string> LoadSongStatuses()
    {
        var statuses = LoadConfig()?.SongStatuses;
        return statuses is null
            ? DefaultSongStatuses.ToArray()
            : NormalizeSongStatuses(statuses);
    }

    public static void SaveSongStatuses(IEnumerable<string> statuses)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SongStatuses = NormalizeSongStatuses(statuses).ToList();
        SaveConfig(config);
    }

    public static void SaveSongStatuses(
        IEnumerable<string> statuses,
        IReadOnlyDictionary<string, string> renamedStatuses)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SongStatuses = NormalizeSongStatuses(statuses).ToList();

        if (config.SongAnnotations is not null && renamedStatuses.Count > 0)
        {
            foreach (var annotation in config.SongAnnotations.Values)
            {
                if (string.IsNullOrWhiteSpace(annotation.Status))
                {
                    continue;
                }

                var renamedStatus = renamedStatuses
                    .FirstOrDefault(pair => string.Equals(pair.Key, annotation.Status, StringComparison.OrdinalIgnoreCase))
                    .Value;
                if (!string.IsNullOrWhiteSpace(renamedStatus))
                {
                    annotation.Status = renamedStatus;
                }
            }
        }

        SaveConfig(config);
    }

    private static IReadOnlyList<string> NormalizeSongStatuses(IEnumerable<string> statuses)
    {
        return statuses
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Select(status => status.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<string> LoadSongGenres()
    {
        var genres = LoadConfig()?.SongGenres;
        return genres is null
            ? DefaultSongGenres.ToArray()
            : NormalizeSongGenres(genres);
    }

    public static void SaveSongGenres(
        IEnumerable<string> genres,
        IReadOnlyDictionary<string, string> renamedGenres)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SongGenres = NormalizeSongGenres(genres).ToList();

        if (config.SongAnnotations is not null && renamedGenres.Count > 0)
        {
            foreach (var annotation in config.SongAnnotations.Values)
            {
                if (string.IsNullOrWhiteSpace(annotation.Genre))
                {
                    continue;
                }

                var renamedGenre = renamedGenres
                    .FirstOrDefault(pair => string.Equals(pair.Key, annotation.Genre, StringComparison.OrdinalIgnoreCase))
                    .Value;
                if (!string.IsNullOrWhiteSpace(renamedGenre))
                {
                    annotation.Genre = renamedGenre;
                }
            }
        }

        SaveConfig(config);
    }

    public static IReadOnlyList<SavedSongLensView> LoadSavedSongLensViews()
    {
        return (LoadConfig()?.SavedSongLensViews ?? [])
            .Where(view => !string.IsNullOrWhiteSpace(view.Name))
            .Select(CloneSavedSongLensView)
            .ToArray();
    }

    public static void SaveSavedSongLensViews(IEnumerable<SavedSongLensView> views)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SavedSongLensViews = views
            .Where(view => !string.IsNullOrWhiteSpace(view.Name))
            .GroupBy(view => view.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => CloneSavedSongLensView(group.Last()))
            .ToList();
        SaveConfig(config);
    }

    public static IReadOnlyList<SongGridSortLevel> LoadSongGridSortLevels()
    {
        var savedLevels = LoadConfig()?.SongGridSortLevels;
        return NormalizeSortLevels(savedLevels, useDefaultWhenEmpty: savedLevels is null);
    }

    public static void SaveSongGridSortLevels(IEnumerable<SongGridSortLevel> sortLevels)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SongGridSortLevels = NormalizeSortLevels(sortLevels, useDefaultWhenEmpty: false).ToList();
        SaveConfig(config);
    }

    private static SavedSongLensView CloneSavedSongLensView(SavedSongLensView view)
    {
        var sortLevels = NormalizeSortLevels(view.SortLevels, useDefaultWhenEmpty: false).ToList();
        if (view.SortLevels is null && sortLevels.Count == 0 && !string.IsNullOrWhiteSpace(view.SortColumnName))
        {
            sortLevels.Add(new SongGridSortLevel
            {
                ColumnName = view.SortColumnName,
                Direction = NormalizeSortDirection(view.SortDirection)
            });
        }

        return new SavedSongLensView
        {
            Name = view.Name.Trim(),
            FastSearchText = string.IsNullOrWhiteSpace(view.FastSearchText)
                ? null
                : view.FastSearchText.Trim(),
            AdvancedSearchQuery = view.AdvancedSearchQuery is null
                ? null
                : CloneAdvancedSearchQuery(view.AdvancedSearchQuery),
            VisibleColumnKeys = (view.VisibleColumnKeys ?? [])
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ColumnOrder = (view.ColumnOrder ?? [])
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ColumnWidths = new Dictionary<string, int>(view.ColumnWidths ?? [], StringComparer.OrdinalIgnoreCase),
            SortColumnName = string.IsNullOrWhiteSpace(view.SortColumnName) ? "Song" : view.SortColumnName,
            SortDirection = string.Equals(
                view.SortDirection,
                nameof(System.ComponentModel.ListSortDirection.Descending),
                StringComparison.OrdinalIgnoreCase)
                ? nameof(System.ComponentModel.ListSortDirection.Descending)
                : nameof(System.ComponentModel.ListSortDirection.Ascending),
            SortLevels = sortLevels,
            DateFilter = CloneSongAgeFilter(view.DateFilter)
        };
    }

    private static IReadOnlyList<SongGridSortLevel> NormalizeSortLevels(
        IEnumerable<SongGridSortLevel>? sortLevels,
        bool useDefaultWhenEmpty)
    {
        var normalized = (sortLevels ?? [])
            .Where(level => !string.IsNullOrWhiteSpace(level.ColumnName))
            .GroupBy(level => level.ColumnName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(4)
            .Select(level => new SongGridSortLevel
            {
                ColumnName = level.ColumnName.Trim(),
                Direction = NormalizeSortDirection(level.Direction)
            })
            .ToList();
        if (normalized.Count == 0 && useDefaultWhenEmpty)
        {
            normalized.Add(new SongGridSortLevel
            {
                ColumnName = "Song",
                Direction = nameof(System.ComponentModel.ListSortDirection.Ascending)
            });
        }

        return normalized;
    }

    private static string NormalizeSortDirection(string? direction)
    {
        return string.Equals(
            direction,
            nameof(System.ComponentModel.ListSortDirection.Descending),
            StringComparison.OrdinalIgnoreCase)
            ? nameof(System.ComponentModel.ListSortDirection.Descending)
            : nameof(System.ComponentModel.ListSortDirection.Ascending);
    }

    private static SongAgeFilter? CloneSongAgeFilter(SongAgeFilter? filter)
    {
        return filter is null
            ? null
            : new SongAgeFilter
            {
                Mode = filter.Mode,
                DateField = filter.DateField,
                Operator = filter.Operator,
                Days = filter.Days,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate
            };
    }

    private static IReadOnlyList<string> NormalizeSongGenres(IEnumerable<string> genres)
    {
        return genres
            .Where(genre => !string.IsNullOrWhiteSpace(genre))
            .Select(genre => genre.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static void MoveSongAnnotation(string oldSongPath, string newSongPath)
    {
        var config = LoadConfig();
        if (config?.SongAnnotations is null)
        {
            return;
        }

        var oldFullPath = Path.GetFullPath(oldSongPath);
        var newFullPath = Path.GetFullPath(newSongPath);
        var annotations = new Dictionary<string, SongAnnotation>(config.SongAnnotations, StringComparer.OrdinalIgnoreCase);
        if (!annotations.Remove(oldFullPath, out var annotation))
        {
            return;
        }

        annotations[newFullPath] = annotation;
        config.SongAnnotations = annotations;
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

    public static bool LoadUseCustomAudioPlayer()
    {
        return LoadConfig()?.UseCustomAudioPlayer ?? false;
    }

    public static string? LoadCustomAudioPlayerPath()
    {
        var path = LoadConfig()?.CustomAudioPlayerPath;
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    public static string? LoadCustomAudioPlayerArguments()
    {
        var arguments = LoadConfig()?.CustomAudioPlayerArguments;
        return string.IsNullOrWhiteSpace(arguments) ? null : arguments.Trim();
    }

    public static string LoadMixdownFolderName()
    {
        var folderName = LoadConfig()?.MixdownFolderName?.Trim();
        return IsValidMixdownFolderName(folderName) ? folderName! : "Mixdown";
    }

    public static void SaveAudioPreferences(
        string mixdownFolderName,
        bool useCustomPlayer,
        string? executablePath,
        string? customPlayerArguments)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.MixdownFolderName = string.IsNullOrWhiteSpace(mixdownFolderName)
            ? "Mixdown"
            : mixdownFolderName.Trim();
        config.UseCustomAudioPlayer = useCustomPlayer;
        config.CustomAudioPlayerPath = string.IsNullOrWhiteSpace(executablePath)
            ? null
            : Path.GetFullPath(executablePath);
        config.CustomAudioPlayerArguments = string.IsNullOrWhiteSpace(customPlayerArguments)
            ? null
            : customPlayerArguments.Trim();
        SaveConfig(config);
    }

    public static bool LoadUseHistoryRetentionPreset()
    {
        return LoadConfig()?.UseHistoryRetentionPreset ?? true;
    }

    public static int LoadHistoryRevisionsToKeep()
    {
        return Math.Clamp(LoadConfig()?.HistoryRevisionsToKeep ?? 1, 1, 100);
    }

    public static void SaveHistoryRetentionPreferences(bool useRetentionPreset, int revisionsToKeep)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.UseHistoryRetentionPreset = useRetentionPreset;
        config.HistoryRevisionsToKeep = Math.Clamp(revisionsToKeep, 1, 100);
        SaveConfig(config);
    }

    private static bool IsValidMixdownFolderName(string? folderName)
    {
        return !string.IsNullOrWhiteSpace(folderName)
            && folderName is not "." and not ".."
            && !folderName.EndsWith(".", StringComparison.Ordinal)
            && folderName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
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

        return config?.EnableSongGridContextMenu ?? true;
    }

    public static bool HasExplicitEnableSongLaunchPreference()
    {
        var config = LoadConfig();
        return config?.ShowEnableSongLaunchPreference == true
            || config?.EnableSongLaunch is bool
            || config?.EnableSongGridContextMenu is bool;
    }

    public static void SaveEnableSongLaunch(bool isEnabled)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.EnableSongLaunch = isEnabled;
        config.ShowEnableSongLaunchPreference = true;
        SaveConfig(config);
    }

    public static SongGridDoubleClickAction LoadSongGridDoubleClickAction()
    {
        var value = LoadConfig()?.SongGridDoubleClickAction;
        return Enum.TryParse(value, ignoreCase: true, out SongGridDoubleClickAction action)
            ? action
            : SongGridDoubleClickAction.RevealInExplorer;
    }

    public static void SaveSongGridDoubleClickAction(SongGridDoubleClickAction action)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SongGridDoubleClickAction = action.ToString();
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

    public static IReadOnlyList<string> LoadSongGridColumnOrder()
    {
        var config = LoadConfig();
        if (config?.SongGridColumnOrder is null || config.SongGridColumnOrder.Count == 0)
        {
            return Array.Empty<string>();
        }

        return config.SongGridColumnOrder
            .Where(columnName => !string.IsNullOrWhiteSpace(columnName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static void SaveSongGridColumnOrder(IReadOnlyList<string> columnNames)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SongGridColumnOrder = columnNames
            .Where(columnName => !string.IsNullOrWhiteSpace(columnName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    public static bool LoadSuppressCsvMixerExportInfo()
    {
        return LoadConfig()?.SuppressCsvMixerExportInfo ?? false;
    }

    public static void SaveSuppressCsvMixerExportInfo(bool suppress)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SuppressCsvMixerExportInfo = suppress;
        SaveConfig(config);
    }

    public static bool LoadSuppressExcelMixerExportInfo()
    {
        return LoadConfig()?.SuppressExcelMixerExportInfo ?? false;
    }

    public static void SaveSuppressExcelMixerExportInfo(bool suppress)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SuppressExcelMixerExportInfo = suppress;
        SaveConfig(config);
    }

    public static bool LoadSuppressBulkEditIntro()
    {
        return LoadConfig()?.SuppressBulkEditIntro ?? false;
    }

    public static void SaveSuppressBulkEditIntro(bool suppress)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SuppressBulkEditIntro = suppress;
        SaveConfig(config);
    }

    public static bool LoadSuppressRestoreDefaultLayoutWarning()
    {
        return LoadConfig()?.SuppressRestoreDefaultLayoutWarning ?? false;
    }

    public static void SaveSuppressRestoreDefaultLayoutWarning(bool suppress)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SuppressRestoreDefaultLayoutWarning = suppress;
        SaveConfig(config);
    }

    public static bool LoadSuppressStatisticsChartSelectionInfo()
    {
        return LoadConfig()?.SuppressStatisticsChartSelectionInfo ?? false;
    }

    public static void SaveSuppressStatisticsChartSelectionInfo(bool suppress)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SuppressStatisticsChartSelectionInfo = suppress;
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

    public static void EnsureSongAnnotationColumnsInitialized()
    {
        var config = LoadConfig() ?? new BrowserConfig();
        if (config.SongAnnotationColumnsInitialized == true)
        {
            return;
        }

        if (config.SongGridVisibleColumnKeys is { Count: > 0 })
        {
            if (!config.SongGridVisibleColumnKeys.Contains("status", StringComparer.OrdinalIgnoreCase))
            {
                config.SongGridVisibleColumnKeys.Insert(0, "status");
            }

            if (!config.SongGridVisibleColumnKeys.Contains("rating", StringComparer.OrdinalIgnoreCase))
            {
                config.SongGridVisibleColumnKeys.Insert(1, "rating");
            }
        }

        config.SongAnnotationColumnsInitialized = true;
        SaveConfig(config);
    }

    public static void EnsureSongGenreColumnInitialized()
    {
        var config = LoadConfig() ?? new BrowserConfig();
        if (config.SongGenreColumnInitialized == true)
        {
            return;
        }

        if (config.SongGridVisibleColumnKeys is { Count: > 0 }
            && !config.SongGridVisibleColumnKeys.Contains("genre", StringComparer.OrdinalIgnoreCase))
        {
            var statusIndex = config.SongGridVisibleColumnKeys.FindIndex(
                key => string.Equals(key, "status", StringComparison.OrdinalIgnoreCase));
            config.SongGridVisibleColumnKeys.Insert(statusIndex >= 0 ? statusIndex + 1 : 0, "genre");
        }

        if (config.SongGridColumnOrder is { Count: > 0 }
            && !config.SongGridColumnOrder.Contains("Genre", StringComparer.OrdinalIgnoreCase))
        {
            var statusIndex = config.SongGridColumnOrder.FindIndex(
                columnName => string.Equals(columnName, "Status", StringComparison.OrdinalIgnoreCase));
            config.SongGridColumnOrder.Insert(statusIndex >= 0 ? statusIndex + 1 : 0, "Genre");
        }

        config.SongGenreColumnInitialized = true;
        SaveConfig(config);
    }

    public static void EnsureSongAnnotationColumnsMovedToEnd()
    {
        var config = LoadConfig() ?? new BrowserConfig();
        if (config.SongAnnotationColumnsMovedToEnd == true)
        {
            return;
        }

        if (config.SongGridColumnOrder is { Count: > 0 })
        {
            var annotationColumns = new[] { "Genre", "Status", "Rating" };
            config.SongGridColumnOrder.RemoveAll(
                columnName => annotationColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase));
            config.SongGridColumnOrder.AddRange(annotationColumns);
        }

        config.SongAnnotationColumnsMovedToEnd = true;
        SaveConfig(config);
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

    public static IReadOnlyList<string> LoadDetailTabVisibleKeys()
    {
        var config = LoadConfig();
        if (config?.DetailTabVisibleKeys is null || config.DetailTabVisibleKeys.Count == 0)
        {
            return Array.Empty<string>();
        }

        return config.DetailTabVisibleKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static void SaveDetailTabVisibleKeys(IReadOnlyList<string> tabKeys)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.DetailTabVisibleKeys = tabKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        SaveConfig(config);
    }

    public static void EnsurePluginsDetailTabInitialized()
    {
        var config = LoadConfig() ?? new BrowserConfig();
        if (config.PluginsDetailTabInitialized == true)
        {
            return;
        }

        if (config.DetailTabVisibleKeys is { Count: > 0 }
            && !config.DetailTabVisibleKeys.Contains("plugins", StringComparer.OrdinalIgnoreCase))
        {
            var summaryIndex = config.DetailTabVisibleKeys.FindIndex(
                key => string.Equals(key, "summary", StringComparison.OrdinalIgnoreCase));
            config.DetailTabVisibleKeys.Insert(summaryIndex >= 0 ? summaryIndex + 1 : 0, "plugins");
        }

        if (config.LastSelectedDetailTabIndex is >= 1)
        {
            config.LastSelectedDetailTabIndex++;
        }

        config.PluginsDetailTabInitialized = true;
        SaveConfig(config);
    }

    public static bool LoadShowTracksWithEventsOnly()
    {
        var config = LoadConfig();
        return config?.ShowTracksWithEventsOnly ?? false;
    }

    public static void SaveShowTracksWithEventsOnly(bool showTracksWithEventsOnly)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.ShowTracksWithEventsOnly = showTracksWithEventsOnly;
        SaveConfig(config);
    }

    public static bool LoadHideFolderTracks()
    {
        var config = LoadConfig();
        return config?.HideFolderTracks ?? false;
    }

    public static void SaveHideFolderTracks(bool hideFolderTracks)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.HideFolderTracks = hideFolderTracks;
        SaveConfig(config);
    }

    public static bool LoadHideDisabledTracks()
    {
        var config = LoadConfig();
        return config?.HideDisabledTracks ?? false;
    }

    public static void SaveHideDisabledTracks(bool hideDisabledTracks)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.HideDisabledTracks = hideDisabledTracks;
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
                && search.Query.Rules is not null)
            .GroupBy(search => search.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new SavedAdvancedSearch
            {
                Name = group.First().Name.Trim(),
                Query = CloneAdvancedSearchQuery(group.First().Query),
                SortLevels = CloneAdvancedSortLevels(group.First().SortLevels)
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
                && search.Query.Rules is not null)
            .GroupBy(search => search.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new SavedAdvancedSearch
            {
                Name = group.First().Name.Trim(),
                Query = CloneAdvancedSearchQuery(group.First().Query),
                SortLevels = CloneAdvancedSortLevels(group.First().SortLevels)
            })
            .OrderBy(search => search.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        SaveConfig(config);
    }

    private static IReadOnlyList<AdvancedSearchSortLevel>? CloneAdvancedSortLevels(
        IEnumerable<AdvancedSearchSortLevel>? sortLevels)
    {
        return sortLevels?
            .Where(level => !string.IsNullOrWhiteSpace(level.ColumnName))
            .Take(4)
            .Select(level => new AdvancedSearchSortLevel
            {
                ColumnName = level.ColumnName,
                Direction = string.Equals(level.Direction, nameof(System.ComponentModel.ListSortDirection.Descending), StringComparison.OrdinalIgnoreCase)
                    ? nameof(System.ComponentModel.ListSortDirection.Descending)
                    : nameof(System.ComponentModel.ListSortDirection.Ascending)
            })
            .ToArray();
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
        if (config is null)
        {
            return null;
        }

        if (string.Equals(config.SongAgeFilterMode, nameof(SongAgeFilterMode.DateRange), StringComparison.OrdinalIgnoreCase)
            && DateTime.TryParse(config.SongAgeFilterStartDate, out var startDate)
            && DateTime.TryParse(config.SongAgeFilterEndDate, out var endDate))
        {
            return new SongAgeFilter
            {
                Mode = SongAgeFilterMode.DateRange,
                DateField = string.Equals(config.SongAgeFilterDateField, nameof(SongDateField.Created), StringComparison.OrdinalIgnoreCase)
                    ? SongDateField.Created
                    : SongDateField.Modified,
                StartDate = startDate.Date,
                EndDate = endDate.Date
            };
        }

        if (config.SongAgeFilterDays is null || config.SongAgeFilterDays <= 0)
        {
            return null;
        }

        var filterOperator = string.Equals(config.SongAgeFilterOperator, nameof(SongAgeFilterOperator.OlderThan), StringComparison.OrdinalIgnoreCase)
            ? SongAgeFilterOperator.OlderThan
            : SongAgeFilterOperator.LessThan;

        return new SongAgeFilter
        {
            Mode = SongAgeFilterMode.RelativeDays,
            Operator = filterOperator,
            Days = config.SongAgeFilterDays.Value
        };
    }

    internal static void SaveSongAgeFilterPreference(SongAgeFilter? filter)
    {
        var config = LoadConfig() ?? new BrowserConfig();
        config.SongAgeFilterMode = filter?.Mode.ToString();
        config.SongAgeFilterOperator = filter?.Mode == SongAgeFilterMode.RelativeDays ? filter.Operator.ToString() : null;
        config.SongAgeFilterDays = filter?.Mode == SongAgeFilterMode.RelativeDays ? filter.Days : null;
        config.SongAgeFilterDateField = filter?.Mode == SongAgeFilterMode.DateRange ? filter.DateField.ToString() : null;
        config.SongAgeFilterStartDate = filter?.Mode == SongAgeFilterMode.DateRange ? filter.StartDate?.ToString("yyyy-MM-dd") : null;
        config.SongAgeFilterEndDate = filter?.Mode == SongAgeFilterMode.DateRange ? filter.EndDate?.ToString("yyyy-MM-dd") : null;
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
        config.Theme ??= AppThemes.Light.Name;
        config.EnableSongLaunch ??= true;
        config.ShowEnableSongLaunchPreference ??= true;
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
