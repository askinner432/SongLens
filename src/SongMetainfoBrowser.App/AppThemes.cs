namespace SongMetainfoBrowser.App;

public static class AppThemes
{
    public static AppTheme Dark { get; } = new(
        "Dark",
        Color.FromArgb(31, 31, 31),
        Color.FromArgb(37, 37, 38),
        Color.FromArgb(43, 43, 43),
        Color.FromArgb(14, 99, 156),
        Color.FromArgb(58, 61, 65),
        Color.FromArgb(18, 111, 173),
        Color.FromArgb(12, 84, 132),
        Color.FromArgb(69, 73, 79),
        Color.FromArgb(55, 58, 62),
        Color.FromArgb(74, 74, 74),
        Color.FromArgb(42, 42, 42),
        Color.FromArgb(14, 99, 156),
        Color.FromArgb(48, 48, 48),
        Color.FromArgb(31, 31, 31),
        Color.FromArgb(242, 242, 242),
        Color.FromArgb(184, 184, 184),
        Color.FromArgb(242, 242, 242),
        Color.FromArgb(184, 184, 184),
        Color.FromArgb(238, 201, 30),
        Color.FromArgb(255, 214, 51),
        Color.FromArgb(247, 212, 60),
        Color.FromArgb(255, 225, 88),
        Color.FromArgb(148, 120, 18));

    public static AppTheme Light { get; } = new(
        "Light",
        Color.FromArgb(204, 199, 191),
        Color.FromArgb(220, 215, 207),
        Color.FromArgb(210, 205, 197),
        Color.FromArgb(52, 98, 142),
        Color.FromArgb(193, 203, 212),
        Color.FromArgb(173, 190, 206),
        Color.FromArgb(156, 174, 190),
        Color.FromArgb(205, 200, 192),
        Color.FromArgb(191, 185, 176),
        Color.FromArgb(156, 149, 139),
        Color.FromArgb(212, 207, 198),
        Color.FromArgb(174, 198, 220),
        Color.FromArgb(198, 206, 213),
        Color.FromArgb(194, 188, 179),
        Color.FromArgb(40, 46, 54),
        Color.FromArgb(100, 107, 116),
        Color.FromArgb(23, 28, 34),
        Color.FromArgb(92, 100, 109),
        Color.FromArgb(230, 190, 20),
        Color.FromArgb(246, 205, 50),
        Color.FromArgb(250, 216, 90),
        Color.FromArgb(252, 225, 120),
        Color.FromArgb(156, 118, 18));

    public static AppTheme Resolve(string? themeName)
    {
        return string.Equals(themeName, Dark.Name, StringComparison.OrdinalIgnoreCase) ? Dark : Light;
    }
}
