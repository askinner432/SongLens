using System.Windows.Forms;

namespace SongMetainfoBrowser.App;

internal sealed class FlatToolStripButtonRenderer(AppTheme theme) : ToolStripProfessionalRenderer(new ThemeColorTable(theme))
{
    private readonly AppTheme _theme = theme;

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var backBrush = new SolidBrush(_theme.PanelBackColor);
        e.Graphics.FillRectangle(backBrush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
    }

    protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
    {
        var bounds = new Rectangle(Point.Empty, e.Item.Size);
        var hoverColor = BlendColor(_theme.AccentSoftColor, _theme.AccentColor, 0.18);
        var pressedColor = BlendColor(_theme.AccentSoftColor, _theme.AccentColor, 0.35);
        var backColor = e.Item.Pressed
            ? pressedColor
            : e.Item.Selected
                ? hoverColor
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

    private static Color BlendColor(Color baseColor, Color accentColor, double amount)
    {
        var clampedAmount = Math.Clamp(amount, 0.0, 1.0);
        return Color.FromArgb(
            (int)Math.Round(baseColor.R + ((accentColor.R - baseColor.R) * clampedAmount)),
            (int)Math.Round(baseColor.G + ((accentColor.G - baseColor.G) * clampedAmount)),
            (int)Math.Round(baseColor.B + ((accentColor.B - baseColor.B) * clampedAmount)));
    }
}
