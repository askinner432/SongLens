using System.Windows.Forms;

namespace SongMetainfoBrowser.App;

internal sealed class ThemeMenuRenderer(AppTheme theme) : ToolStripProfessionalRenderer(new ThemeColorTable(theme))
{
    private readonly AppTheme _theme = theme;

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = _theme.TextColor;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = _theme.TextColor;
        base.OnRenderArrow(e);
    }
}
