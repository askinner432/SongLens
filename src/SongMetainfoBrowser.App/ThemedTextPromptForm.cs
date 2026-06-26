namespace SongMetainfoBrowser.App;

internal sealed class ThemedTextPromptForm : Form
{
    private readonly AppTheme _theme;
    private readonly TextBox _textBox = new();

    public string EnteredText => _textBox.Text.Trim();

    public ThemedTextPromptForm(string title, string prompt, string initialValue, AppTheme theme, string okText = "OK", string cancelText = "Cancel")
    {
        _theme = theme;
        var fontPreferences = AppFontSettings.LoadPreferences();

        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(420, 140), fontPreferences, AppFontSection.Dialogs);
        MinimumSize = Size;
        MaximumSize = Size;
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        BuildLayout(prompt, initialValue, okText, cancelText);
    }

    private void BuildLayout(string prompt, string initialValue, string okText, string cancelText)
    {
        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(rootLayout);

        var promptLabel = new Label
        {
            AutoSize = true,
            Text = prompt,
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 8)
        };
        rootLayout.Controls.Add(promptLabel, 0, 0);

        _textBox.Dock = DockStyle.Top;
        _textBox.Text = initialValue;
        _textBox.BorderStyle = BorderStyle.FixedSingle;
        _textBox.BackColor = _theme.PanelBackColor;
        _textBox.ForeColor = _theme.TextColor;
        _textBox.Margin = Padding.Empty;
        rootLayout.Controls.Add(_textBox, 0, 1);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 12, 0, 0)
        };

        var okButton = new Button { Text = okText, AutoSize = true };
        okButton.Click += (_, _) => Confirm();
        var cancelButton = new Button { Text = cancelText, AutoSize = true, DialogResult = DialogResult.Cancel };
        StyleButton(okButton, useAccent: true);
        StyleButton(cancelButton, useAccent: false);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);
        rootLayout.Controls.Add(buttonPanel, 0, 2);

        AcceptButton = okButton;
        CancelButton = cancelButton;
        Shown += (_, _) =>
        {
            _textBox.Focus();
            _textBox.SelectAll();
        };
    }

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(EnteredText))
        {
            using var dialog = new ThemedMessageForm(Text, "Enter a name before saving this search.", _theme, ThemedMessageKind.Information);
            dialog.ShowDialog(this);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void StyleButton(Button button, bool useAccent)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = useAccent ? _theme.AccentHoverColor : _theme.NeutralHoverColor;
        button.FlatAppearance.MouseDownBackColor = useAccent ? _theme.AccentPressedColor : _theme.NeutralPressedColor;
        button.BackColor = useAccent ? _theme.AccentSoftColor : _theme.PanelBackColor;
        button.ForeColor = _theme.TextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.Font = Font;
    }
}
