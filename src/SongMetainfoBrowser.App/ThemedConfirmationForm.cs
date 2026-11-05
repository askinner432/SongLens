namespace SongMetainfoBrowser.App;

internal sealed class ThemedConfirmationForm : Form
{
    private readonly AppTheme _theme;
    private readonly CheckBox? _optionCheckBox;

    public bool OptionChecked => _optionCheckBox?.Checked == true;

    public ThemedConfirmationForm(
        string title,
        string message,
        AppTheme theme,
        string okText = "OK",
        string cancelText = "Cancel",
        string? optionText = null)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;

        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(460, 260), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = optionText is null ? 2 : 3,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        if (optionText is not null)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var messageLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Text = message,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = _theme.TextColor,
            Margin = Padding.Empty
        };
        layout.Controls.Add(messageLabel, 0, 0);

        if (optionText is not null)
        {
            _optionCheckBox = new CheckBox
            {
                AutoSize = true,
                Text = optionText,
                ForeColor = _theme.TextColor,
                BackColor = _theme.AppBackColor,
                Margin = new Padding(0, 8, 0, 0)
            };
            layout.Controls.Add(_optionCheckBox, 0, 1);
        }

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
        var cancelButton = new Button
        {
            Text = cancelText,
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        StyleButton(okButton, useAccent: true);
        StyleButton(cancelButton, useAccent: false);

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        layout.Controls.Add(buttonPanel, 0, layout.RowCount - 1);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void StyleButton(Button button, bool useAccent)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = _theme.AccentHoverColor;
        button.FlatAppearance.MouseDownBackColor = _theme.AccentPressedColor;
        button.BackColor = _theme.AccentSoftColor;
        button.ForeColor = _theme.TextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.Font = Font;
    }
}
