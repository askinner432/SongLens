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
        Color.FromArgb(245, 247, 250),
        Color.White,
        Color.FromArgb(248, 250, 252),
        Color.FromArgb(0, 102, 204),
        Color.FromArgb(230, 236, 242),
        Color.FromArgb(0, 120, 215),
        Color.FromArgb(0, 84, 153),
        Color.FromArgb(220, 227, 234),
        Color.FromArgb(204, 214, 224),
        Color.FromArgb(205, 211, 220),
        Color.FromArgb(250, 251, 253),
        Color.FromArgb(204, 228, 247),
        Color.FromArgb(230, 236, 242),
        Color.FromArgb(245, 247, 250),
        Color.FromArgb(30, 41, 59),
        Color.FromArgb(100, 116, 139),
        Color.FromArgb(15, 23, 42),
        Color.FromArgb(100, 116, 139),
        Color.FromArgb(230, 190, 20),
        Color.FromArgb(246, 205, 50),
        Color.FromArgb(250, 216, 90),
        Color.FromArgb(252, 225, 120),
        Color.FromArgb(156, 118, 18));

    public static AppTheme Resolve(string? themeName)
    {
        return string.Equals(themeName, Light.Name, StringComparison.OrdinalIgnoreCase) ? Light : Dark;
    }
}
