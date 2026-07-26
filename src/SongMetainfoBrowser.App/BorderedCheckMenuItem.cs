namespace SongMetainfoBrowser.App;

internal sealed class BorderedCheckMenuItem(string text) : ToolStripMenuItem(text)
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (!CheckOnClick || Checked)
        {
            return;
        }

        var checkAreaWidth = Math.Max(Height, ContentRectangle.Left);
        var boxSize = Math.Max(10, Math.Min(Height - 8, checkAreaWidth - 10));
        var checkBounds = new Rectangle(
            Math.Max(3, (checkAreaWidth - boxSize) / 2),
            Math.Max(3, (Height - boxSize) / 2),
            boxSize,
            boxSize);
        var borderColor = Enabled ? Owner?.ForeColor ?? ForeColor : SystemColors.GrayText;
        using var borderPen = new Pen(borderColor);
        e.Graphics.DrawRectangle(borderPen, checkBounds);
    }
}
