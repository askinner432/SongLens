namespace SongMetainfoBrowser.App;

internal enum CsvExportScope
{
    EntireLibrary,
    CurrentSong
}

internal sealed class CsvExportScopeForm : Form
{
    private readonly AppTheme _theme;
    private readonly RadioButton _entireLibraryRadioButton = new();
    private readonly RadioButton _currentSongRadioButton = new();

    public CsvExportScope SelectedScope { get; private set; } = CsvExportScope.EntireLibrary;

    public CsvExportScopeForm(bool canExportCurrentSong, AppTheme theme)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        Text = "Export CSV";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(380, 170), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var introLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = "Choose what to export.",
            ForeColor = _theme.TextColor
        };
        layout.Controls.Add(introLabel, 0, 0);

        var optionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0),
            BackColor = _theme.AppBackColor
        };

        _entireLibraryRadioButton.AutoSize = true;
        _entireLibraryRadioButton.Text = "Export Entire Library";
        _entireLibraryRadioButton.Checked = true;
        _entireLibraryRadioButton.ForeColor = _theme.TextColor;
        _entireLibraryRadioButton.BackColor = _theme.AppBackColor;

        _currentSongRadioButton.AutoSize = true;
        _currentSongRadioButton.Text = "Export this Song Only";
        _currentSongRadioButton.Enabled = canExportCurrentSong;
        _currentSongRadioButton.ForeColor = _theme.TextColor;
        _currentSongRadioButton.BackColor = _theme.AppBackColor;

        optionsPanel.Controls.Add(_entireLibraryRadioButton);
        optionsPanel.Controls.Add(_currentSongRadioButton);
        layout.Controls.Add(optionsPanel, 0, 1);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };

        var okButton = new Button
        {
            Text = "OK",
            AutoSize = true
        };
        okButton.Click += (_, _) =>
        {
            SelectedScope = _currentSongRadioButton.Checked
                ? CsvExportScope.CurrentSong
                : CsvExportScope.EntireLibrary;

            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };
        StyleButton(okButton, useAccent: true);
        StyleButton(cancelButton, useAccent: false);

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        layout.Controls.Add(buttonPanel, 0, 2);

        AcceptButton = okButton;
        CancelButton = cancelButton;
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
