namespace SongMetainfoBrowser.App;

internal enum ThemedMessageKind
{
    Information,
    Warning,
    Error
}

internal sealed class ThemedMessageForm : Form
{
    private readonly AppTheme _theme;

    public ThemedMessageForm(string title, string message, AppTheme theme, ThemedMessageKind kind = ThemedMessageKind.Information, string okText = "OK")
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;

        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(420, 160), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var iconLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = AppFontSettings.Scale(32, fontPreferences, AppFontSection.Dialogs),
            Width = AppFontSettings.Scale(32, fontPreferences, AppFontSection.Dialogs),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = AppFontSettings.CreateSymbolFont(fontPreferences, AppFontSection.Dialogs),
            ForeColor = ResolveIconColor(kind),
            Text = ResolveIconText(kind),
            Margin = new Padding(0, 4, 8, 0)
        };
        layout.Controls.Add(iconLabel, 0, 0);

        var messageLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Text = message,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _theme.TextColor,
            Margin = Padding.Empty
        };
        layout.Controls.Add(messageLabel, 1, 0);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 0),
            BackColor = _theme.AppBackColor
        };

        var okButton = new Button
        {
            Text = okText,
            AutoSize = true,
            DialogResult = DialogResult.OK
        };
        StyleButton(okButton);
        buttonPanel.Controls.Add(okButton);
        layout.Controls.Add(buttonPanel, 1, 1);

        AcceptButton = okButton;
        CancelButton = okButton;
    }

    private Color ResolveIconColor(ThemedMessageKind kind)
    {
        return kind switch
        {
            ThemedMessageKind.Warning => Color.Goldenrod,
            ThemedMessageKind.Error => Color.IndianRed,
            _ => _theme.AccentColor
        };
    }

    private static string ResolveIconText(ThemedMessageKind kind)
    {
        return kind switch
        {
            ThemedMessageKind.Warning => "!",
            ThemedMessageKind.Error => "×",
            _ => "i"
        };
    }

    private void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = _theme.NeutralHoverColor;
        button.FlatAppearance.MouseDownBackColor = _theme.NeutralPressedColor;
        button.BackColor = _theme.PanelBackColor;
        button.ForeColor = _theme.TextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.Font = Font;
    }
}
