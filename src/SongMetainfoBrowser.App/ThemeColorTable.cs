using System.Windows.Forms;

namespace SongMetainfoBrowser.App;

internal sealed class ThemeColorTable(AppTheme theme) : ProfessionalColorTable
{
    public override Color MenuStripGradientBegin => theme.AppBackColor;
    public override Color MenuStripGradientEnd => theme.AppBackColor;
    public override Color ToolStripDropDownBackground => theme.PanelBackColor;
    public override Color ImageMarginGradientBegin => theme.PanelBackColor;
    public override Color ImageMarginGradientMiddle => theme.PanelBackColor;
    public override Color ImageMarginGradientEnd => theme.PanelBackColor;
    public override Color MenuItemSelected => theme.TreeSelectionColor;
    public override Color MenuItemBorder => theme.BorderColor;
    public override Color MenuItemSelectedGradientBegin => theme.TreeSelectionColor;
    public override Color MenuItemSelectedGradientEnd => theme.TreeSelectionColor;
    public override Color MenuItemPressedGradientBegin => theme.PanelBackColor;
    public override Color MenuItemPressedGradientMiddle => theme.PanelBackColor;
    public override Color MenuItemPressedGradientEnd => theme.PanelBackColor;
    public override Color ButtonSelectedHighlight => theme.TreeSelectionColor;
    public override Color ButtonSelectedHighlightBorder => theme.BorderColor;
    public override Color ButtonPressedHighlight => theme.TreeSelectionColor;
    public override Color ButtonPressedHighlightBorder => theme.BorderColor;
    public override Color SeparatorDark => theme.BorderColor;
    public override Color SeparatorLight => theme.BorderColor;
}
