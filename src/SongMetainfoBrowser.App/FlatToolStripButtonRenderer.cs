using System.Windows.Forms;

namespace SongMetainfoBrowser.App;

internal sealed class FlatToolStripButtonRenderer(
    AppTheme theme,
    Color? toolStripBackColor = null) : ToolStripProfessionalRenderer(new ThemeColorTable(theme))
{
    private readonly AppTheme _theme = theme;
    private readonly Color _toolStripBackColor = toolStripBackColor ?? theme.PanelBackColor;

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var backBrush = new SolidBrush(_toolStripBackColor);
        e.Graphics.FillRectangle(backBrush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
    }

    protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
    {
        var bounds = new Rectangle(Point.Empty, e.Item.Size);
        var backColor = e.Item.Pressed
            ? _theme.AccentPressedColor
            : e.Item.Selected
                ? _theme.AccentHoverColor
                : _theme.AccentSoftColor;

        using var backBrush = new SolidBrush(backColor);
        using var borderPen = new Pen(_theme.BorderColor);
        e.Graphics.FillRectangle(backBrush, bounds);
        e.Graphics.DrawRectangle(borderPen, 0, 0, bounds.Width - 1, bounds.Height - 1);
    }

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
