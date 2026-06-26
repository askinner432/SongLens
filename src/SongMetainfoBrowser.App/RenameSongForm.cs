namespace SongMetainfoBrowser.App;

internal sealed class RenameSongForm : Form
{
    private readonly AppTheme _theme;
    private readonly TextBox _nameTextBox = new();
    private readonly Label _validationLabel = new();

    public RenameSongForm(string currentFileNameWithoutExtension, AppTheme theme)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;

        Text = "Rename Song";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(460, 180), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        SongFileNameWithoutExtension = currentFileNameWithoutExtension;
        BuildLayout(fontPreferences);

        _nameTextBox.Text = currentFileNameWithoutExtension;
        Shown += (_, _) =>
        {
            _nameTextBox.Focus();
            _nameTextBox.SelectAll();
        };
    }

    public string SongFileNameWithoutExtension { get; private set; }

    private void BuildLayout(AppFontPreferences fontPreferences)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);

        var promptLabel = new Label
        {
            AutoSize = true,
            Text = "Choose a new file name for the selected song:",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 10)
        };

        var nameLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        nameLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        nameLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _nameTextBox.Dock = DockStyle.Fill;
        _nameTextBox.BorderStyle = BorderStyle.FixedSingle;
        _nameTextBox.BackColor = _theme.PanelBackColor;
        _nameTextBox.ForeColor = _theme.TextColor;
        _nameTextBox.Margin = new Padding(0, 0, 8, 0);

        var extensionLabel = new Label
        {
            AutoSize = true,
            Text = ".song",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _theme.MutedTextColor,
            Anchor = AnchorStyles.Left,
            Margin = Padding.Empty
        };

        nameLayout.Controls.Add(_nameTextBox, 0, 0);
        nameLayout.Controls.Add(extensionLabel, 1, 0);

        _validationLabel.AutoSize = true;
        _validationLabel.ForeColor = Color.IndianRed;
        _validationLabel.Margin = new Padding(0, 8, 0, 0);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 14, 0, 0),
            BackColor = _theme.AppBackColor
        };

        var renameButton = new Button
        {
            Text = "Rename",
            AutoSize = true
        };
        var cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        StyleButton(renameButton, useAccent: true);
        StyleButton(cancelButton, useAccent: false);

        renameButton.Click += (_, _) => Submit();

        buttonPanel.Controls.Add(renameButton);
        buttonPanel.Controls.Add(cancelButton);

        layout.Controls.Add(promptLabel, 0, 0);
        layout.Controls.Add(nameLayout, 0, 1);
        layout.Controls.Add(_validationLabel, 0, 2);
        layout.Controls.Add(buttonPanel, 0, 3);

        AcceptButton = renameButton;
        CancelButton = cancelButton;
    }

    private void Submit()
    {
        var candidate = _nameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            _validationLabel.Text = "Enter a song file name.";
            return;
        }

        if (candidate.EndsWith(".", StringComparison.Ordinal) || candidate[^1] == ' ')
        {
            _validationLabel.Text = "The name cannot end with a space or period.";
            return;
        }

        if (candidate.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            _validationLabel.Text = "The name contains characters Windows does not allow.";
            return;
        }

        SongFileNameWithoutExtension = candidate;
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
