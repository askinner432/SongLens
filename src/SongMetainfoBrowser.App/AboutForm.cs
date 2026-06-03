namespace SongMetainfoBrowser.App;

public sealed class AboutForm : Form
{
    private readonly AppTheme _theme;
    private readonly AppFontPreferences _fontPreferences;

    public AboutForm(AppTheme theme)
    {
        _fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;

        Text = $"About {AppInfo.ProductName}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(420, 220), _fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.Dialogs);

        BuildLayout();
    }

    private void BuildLayout()
    {
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(18),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = AppInfo.ProductName,
            Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.Dialogs, FontStyle.Bold),
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 6)
        };

        var versionLabel = new Label
        {
            AutoSize = true,
            Text = AppInfo.GetBuildText(),
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 0, 0, 10)
        };

        var descriptionLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(AppFontSettings.Scale(360, _fontPreferences, AppFontSection.Dialogs), 0),
            Text = "Browse Studio One .song metadata, track details, and project history from a native Windows app.",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 10)
        };

        var licenseLabel = new Label
        {
            AutoSize = true,
            Text = "Released under the MIT License.",
            ForeColor = _theme.MutedTextColor
        };

        var closeButton = new Button
        {
            Text = "Close",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };
        StyleButton(closeButton);
        closeButton.Click += (_, _) => Close();

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 12, 0, 0)
        };
        buttonPanel.Controls.Add(closeButton);

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(versionLabel, 0, 1);
        layout.Controls.Add(descriptionLabel, 0, 2);
        layout.Controls.Add(licenseLabel, 0, 3);
        layout.Controls.Add(buttonPanel, 0, 4);

        AcceptButton = closeButton;
        CancelButton = closeButton;
    }

    private void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = _theme.NeutralHoverColor;
        button.FlatAppearance.MouseDownBackColor = _theme.NeutralPressedColor;
        button.BackColor = _theme.AccentSoftColor;
        button.ForeColor = _theme.TextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.Font = Font;
    }
}
