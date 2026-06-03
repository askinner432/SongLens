namespace SongMetainfoBrowser.App;

internal enum AppFontSection
{
    MainUi,
    FolderTree,
    SongGrid,
    DetailGrids,
    NotesAndPreviewText,
    Dialogs
}

internal sealed class AppFontPreferences
{
    public int MainUi { get; init; }
    public int FolderTree { get; init; }
    public int SongGrid { get; init; }
    public int DetailGrids { get; init; }
    public int NotesAndPreviewText { get; init; }
    public int Dialogs { get; init; }

    public int GetSizePoints(AppFontSection section)
    {
        return section switch
        {
            AppFontSection.MainUi => MainUi,
            AppFontSection.FolderTree => FolderTree,
            AppFontSection.SongGrid => SongGrid,
            AppFontSection.DetailGrids => DetailGrids,
            AppFontSection.NotesAndPreviewText => NotesAndPreviewText,
            AppFontSection.Dialogs => Dialogs,
            _ => AppFontSettings.DefaultSizePoints
        };
    }

    public Dictionary<string, int> ToDictionary()
    {
        return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [AppFontSection.MainUi.ToString()] = MainUi,
            [AppFontSection.FolderTree.ToString()] = FolderTree,
            [AppFontSection.SongGrid.ToString()] = SongGrid,
            [AppFontSection.DetailGrids.ToString()] = DetailGrids,
            [AppFontSection.NotesAndPreviewText.ToString()] = NotesAndPreviewText,
            [AppFontSection.Dialogs.ToString()] = Dialogs
        };
    }
}

internal static class AppFontSettings
{
    public const int DefaultSizePoints = 9;
    public const int MinSizePoints = 9;
    public const int MaxSizePoints = 16;

    public static AppFontPreferences LoadPreferences()
    {
        var savedValues = BrowserConfigStore.LoadFontSizes();
        var legacyFontSize = BrowserConfigStore.LoadFontSizePoints();

        return new AppFontPreferences
        {
            MainUi = ResolveSectionSize(savedValues, AppFontSection.MainUi, legacyFontSize),
            FolderTree = ResolveSectionSize(savedValues, AppFontSection.FolderTree, legacyFontSize),
            SongGrid = ResolveSectionSize(savedValues, AppFontSection.SongGrid, legacyFontSize),
            DetailGrids = ResolveSectionSize(savedValues, AppFontSection.DetailGrids, legacyFontSize),
            NotesAndPreviewText = ResolveSectionSize(savedValues, AppFontSection.NotesAndPreviewText, legacyFontSize),
            Dialogs = ResolveSectionSize(savedValues, AppFontSection.Dialogs, legacyFontSize)
        };
    }

    public static AppFontPreferences CreateDefaults()
    {
        return new AppFontPreferences
        {
            MainUi = 10,
            FolderTree = 10,
            SongGrid = 10,
            DetailGrids = 10,
            NotesAndPreviewText = 10,
            Dialogs = 10
        };
    }

    public static void SavePreferences(AppFontPreferences preferences)
    {
        BrowserConfigStore.SaveFontSizes(preferences.ToDictionary());
    }

    public static int Normalize(int? fontSizePoints)
    {
        if (fontSizePoints is null)
        {
            return DefaultSizePoints;
        }

        return Math.Max(MinSizePoints, Math.Min(MaxSizePoints, fontSizePoints.Value));
    }

    public static Font CreateUiFont(int fontSizePoints, FontStyle style = FontStyle.Regular)
    {
        return new Font("Segoe UI", Normalize(fontSizePoints), style, GraphicsUnit.Point);
    }

    public static Font CreateUiFont(AppFontPreferences preferences, AppFontSection section, FontStyle style = FontStyle.Regular)
    {
        return CreateUiFont(preferences.GetSizePoints(section), style);
    }

    public static Font CreateMonospaceFont(int fontSizePoints, FontStyle style = FontStyle.Regular)
    {
        return new Font("Consolas", Normalize(fontSizePoints), style, GraphicsUnit.Point);
    }

    public static Font CreateMonospaceFont(AppFontPreferences preferences, AppFontSection section, FontStyle style = FontStyle.Regular)
    {
        return CreateMonospaceFont(preferences.GetSizePoints(section), style);
    }

    public static Font CreateSymbolFont(int fontSizePoints)
    {
        return new Font("Segoe UI Symbol", ScaleFontSize(16f, fontSizePoints), FontStyle.Bold, GraphicsUnit.Point);
    }

    public static Font CreateSymbolFont(AppFontPreferences preferences, AppFontSection section)
    {
        return CreateSymbolFont(preferences.GetSizePoints(section));
    }

    public static int Scale(int value, int fontSizePoints)
    {
        var scaleFactor = Normalize(fontSizePoints) / (float)DefaultSizePoints;
        return Math.Max(1, (int)Math.Round(value * scaleFactor));
    }

    public static int Scale(int value, AppFontPreferences preferences, AppFontSection section)
    {
        return Scale(value, preferences.GetSizePoints(section));
    }

    public static Size Scale(Size size, int fontSizePoints)
    {
        return new Size(Scale(size.Width, fontSizePoints), Scale(size.Height, fontSizePoints));
    }

    public static Size Scale(Size size, AppFontPreferences preferences, AppFontSection section)
    {
        return Scale(size, preferences.GetSizePoints(section));
    }

    public static float ScaleFontSize(float value, int fontSizePoints)
    {
        var scaleFactor = Normalize(fontSizePoints) / (float)DefaultSizePoints;
        return value * scaleFactor;
    }

    private static int ResolveSectionSize(IReadOnlyDictionary<string, int> savedValues, AppFontSection section, int? legacyFontSize)
    {
        if (savedValues.TryGetValue(section.ToString(), out var savedValue))
        {
            return Normalize(savedValue);
        }

        if (legacyFontSize is not null)
        {
            return Normalize(legacyFontSize);
        }

        return CreateDefaults().GetSizePoints(section);
    }
}
