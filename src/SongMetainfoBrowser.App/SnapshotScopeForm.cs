namespace SongMetainfoBrowser.App;

internal enum SnapshotScope
{
    SelectedSong,
    CurrentResults
}

internal enum SnapshotResultsView
{
    List,
    Detail
}

internal sealed class SnapshotScopeForm : Form
{
    private readonly AppTheme _theme;
    private readonly RadioButton _selectedSongRadioButton = new();
    private readonly RadioButton _currentResultsRadioButton = new();
    private readonly FlowLayoutPanel _resultsViewPanel = new();
    private readonly RadioButton _listViewRadioButton = new();
    private readonly RadioButton _detailViewRadioButton = new();

    public SnapshotScope SelectedScope { get; private set; } = SnapshotScope.SelectedSong;
    public SnapshotResultsView SelectedResultsView { get; private set; } = SnapshotResultsView.List;

    public SnapshotScopeForm(string selectedSongName, string currentResultsLocation, int resultCount, AppTheme theme)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        Text = "Snapshot";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(440, 220), fontPreferences, AppFontSection.Dialogs);
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

        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = "Choose what to snapshot.",
            ForeColor = _theme.TextColor
        }, 0, 0);

        var optionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0),
            BackColor = _theme.AppBackColor
        };

        _selectedSongRadioButton.AutoSize = true;
        _selectedSongRadioButton.Text = selectedSongName;
        _selectedSongRadioButton.Checked = true;
        _selectedSongRadioButton.ForeColor = _theme.TextColor;
        _selectedSongRadioButton.BackColor = _theme.AppBackColor;

        _currentResultsRadioButton.AutoSize = true;
        var songLabel = resultCount == 1 ? "Song" : "Songs";
        _currentResultsRadioButton.Text = $"{resultCount} {songLabel} in {currentResultsLocation}";
        _currentResultsRadioButton.ForeColor = _theme.TextColor;
        _currentResultsRadioButton.BackColor = _theme.AppBackColor;

        _resultsViewPanel.FlowDirection = FlowDirection.LeftToRight;
        _resultsViewPanel.WrapContents = false;
        _resultsViewPanel.AutoSize = true;
        _resultsViewPanel.Margin = new Padding(0, 16, 0, 0);
        _resultsViewPanel.BackColor = _theme.AppBackColor;

        _listViewRadioButton.AutoSize = true;
        _listViewRadioButton.Text = "List View";
        _listViewRadioButton.Margin = new Padding(16, 3, 3, 3);
        _listViewRadioButton.ForeColor = _theme.TextColor;
        _listViewRadioButton.BackColor = _theme.AppBackColor;

        _detailViewRadioButton.AutoSize = true;
        _detailViewRadioButton.Text = "Detail View";
        _detailViewRadioButton.Checked = true;
        _detailViewRadioButton.ForeColor = _theme.TextColor;
        _detailViewRadioButton.BackColor = _theme.AppBackColor;

        _resultsViewPanel.Controls.Add(_detailViewRadioButton);
        _resultsViewPanel.Controls.Add(_listViewRadioButton);
        _selectedSongRadioButton.CheckedChanged += (_, _) =>
        {
            if (_selectedSongRadioButton.Checked)
            {
                _detailViewRadioButton.Checked = true;
            }
        };
        _currentResultsRadioButton.CheckedChanged += (_, _) =>
        {
            if (_currentResultsRadioButton.Checked)
            {
                _listViewRadioButton.Checked = true;
            }
        };

        optionsPanel.Controls.Add(_selectedSongRadioButton);
        optionsPanel.Controls.Add(_currentResultsRadioButton);
        optionsPanel.Controls.Add(_resultsViewPanel);
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

        var okButton = new Button { Text = "OK", AutoSize = true };
        okButton.Click += (_, _) =>
        {
            SelectedScope = _currentResultsRadioButton.Checked
                ? SnapshotScope.CurrentResults
                : SnapshotScope.SelectedSong;
            SelectedResultsView = _detailViewRadioButton.Checked
                ? SnapshotResultsView.Detail
                : SnapshotResultsView.List;
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

        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);
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
