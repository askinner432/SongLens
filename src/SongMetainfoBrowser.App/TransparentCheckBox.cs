using System.ComponentModel;

namespace SongMetainfoBrowser.App;

internal sealed class TransparentCheckBox : Control
{
    private bool _checked;

    public TransparentCheckBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor
            | ControlStyles.UserPaint, true);

        AutoSize = true;
        BackColor = Color.Transparent;
        ForeColor = SystemColors.ControlText;
        Cursor = Cursors.Hand;
        TabStop = true;
        Size = GetPreferredSize(Size.Empty);
    }

    public event EventHandler? CheckedChanged;

    [DefaultValue(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
            {
                return;
            }

            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var text = string.IsNullOrWhiteSpace(Text) ? " " : Text;
        var textSize = TextRenderer.MeasureText(
            text,
            Font,
            new Size(int.MaxValue, int.MaxValue),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        const int boxSize = 13;
        const int spacing = 6;
        return new Size(boxSize + spacing + textSize.Width, Math.Max(boxSize, textSize.Height) + 2);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        if (AutoSize)
        {
            Size = GetPreferredSize(Size.Empty);
        }
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (AutoSize)
        {
            Size = GetPreferredSize(Size.Empty);
        }
    }

    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            Checked = !Checked;
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        if (Parent is not null && BackColor == Color.Transparent)
        {
            var state = pevent.Graphics.Save();
            try
            {
                pevent.Graphics.TranslateTransform(-Left, -Top);
                var clip = new Rectangle(Left, Top, Width, Height);
                using var clipRegion = new Region(clip);
                pevent.Graphics.Clip = clipRegion;
                InvokePaintBackground(Parent, pevent);
                InvokePaint(Parent, pevent);
            }
            finally
            {
                pevent.Graphics.Restore(state);
            }

            return;
        }

        using var backBrush = new SolidBrush(BackColor);
        pevent.Graphics.FillRectangle(backBrush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        const int boxSize = 13;
        const int spacing = 6;
        var boxY = Math.Max(0, (Height - boxSize) / 2);
        var boxBounds = new Rectangle(0, boxY, boxSize, boxSize);

        var parentBackColor = Parent?.BackColor ?? SystemColors.Control;
        using var boxBrush = new SolidBrush(parentBackColor);
        using var borderPen = new Pen(Color.FromArgb(120, ForeColor));
        e.Graphics.FillRectangle(boxBrush, boxBounds);
        e.Graphics.DrawRectangle(borderPen, boxBounds);

        if (Checked)
        {
            using var checkPen = new Pen(ForeColor, 2f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };

            var p1 = new Point(boxBounds.Left + 3, boxBounds.Top + 7);
            var p2 = new Point(boxBounds.Left + 6, boxBounds.Top + 10);
            var p3 = new Point(boxBounds.Left + 10, boxBounds.Top + 3);
            e.Graphics.DrawLines(checkPen, [p1, p2, p3]);
        }

        var textBounds = new Rectangle(boxBounds.Right + spacing, 0, Math.Max(0, Width - boxBounds.Right - spacing), Height);
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            textBounds,
            ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

        if (Focused)
        {
            var focusBounds = Rectangle.Inflate(ClientRectangle, -1, -1);
            ControlPaint.DrawFocusRectangle(e.Graphics, focusBounds, ForeColor, Color.Transparent);
        }
    }
}
