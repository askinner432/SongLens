namespace SongMetainfoBrowser.App;

internal sealed class SaveSongLensViewForm : Form
{
    private readonly AppTheme _theme;
    private readonly TextBox _nameTextBox = new();
    private readonly CheckBox _includeDateFilterCheckBox = new()
    {
        AutoSize = true,
        Text = "Include the current date filter"
    };

    public string ViewName => _nameTextBox.Text.Trim();
    public bool IncludeDateFilter => _includeDateFilterCheckBox.Checked;

    public SaveSongLensViewForm(bool hasDateFilter, AppTheme theme)
    {
        _theme = theme;
        var fontPreferences = AppFontSettings.LoadPreferences();

        Text = "Save Current View";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(440, 180), fontPreferences, AppFontSection.Dialogs);
        MinimumSize = Size;
        MaximumSize = Size;
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        _includeDateFilterCheckBox.Enabled = hasDateFilter;
        _includeDateFilterCheckBox.Checked = hasDateFilter;
        BuildLayout();
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);

        var nameLabel = new Label
        {
            AutoSize = true,
            Text = "Name this view:",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 6)
        };
        layout.Controls.Add(nameLabel, 0, 0);

        _nameTextBox.Dock = DockStyle.Top;
        _nameTextBox.BorderStyle = BorderStyle.FixedSingle;
        _nameTextBox.BackColor = _theme.PanelBackColor;
        _nameTextBox.ForeColor = _theme.TextColor;
        _nameTextBox.Margin = new Padding(0, 0, 0, 10);
        layout.Controls.Add(_nameTextBox, 0, 1);

        _includeDateFilterCheckBox.ForeColor = _includeDateFilterCheckBox.Enabled
            ? _theme.TextColor
            : _theme.MutedTextColor;
        layout.Controls.Add(_includeDateFilterCheckBox, 0, 2);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 12, 0, 0)
        };
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var saveButton = new Button { Text = "Save", AutoSize = true };
        saveButton.Click += (_, _) => Confirm();
        StyleButton(cancelButton, useAccent: false);
        StyleButton(saveButton, useAccent: true);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(saveButton);
        layout.Controls.Add(buttonPanel, 0, 3);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Shown += (_, _) => _nameTextBox.Focus();
    }

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(ViewName))
        {
            using var messageDialog = new ThemedMessageForm(
                Text,
                "Enter a name before adding this view.",
                _theme,
                ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
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
