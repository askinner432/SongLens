namespace SongMetainfoBrowser.App;

internal enum SongAgeFilterDialogAction
{
    None,
    Apply,
    CustomizeView
}

internal enum SongAgeFilterOperator
{
    LessThan,
    OlderThan
}

internal sealed class SongAgeFilter
{
    public required SongAgeFilterOperator Operator { get; init; }
    public required int Days { get; init; }

    public string OperatorText => Operator == SongAgeFilterOperator.LessThan ? "less than" : "older than";
}

internal sealed class SongAgeFilterForm : Form
{
    private readonly AppTheme _theme;
    private readonly CheckBox _filterSongsCheckBox = new() { AutoSize = true };
    private readonly ComboBox _operatorComboBox = new();
    private readonly ComboBox _daysComboBox = new();
    private readonly Label _songsLabel = new();
    private readonly Label _daysLabel = new();
    private readonly Label _oldLabel = new();
    private readonly CheckBox _viewAllSongsCheckBox = new() { AutoSize = true, Text = "View All Songs" };

    public SongAgeFilter? SelectedFilter { get; private set; }
    public bool ViewAllSongsSelected { get; private set; }
    public SongAgeFilterDialogAction RequestedAction { get; private set; }

    public SongAgeFilterForm(SongAgeFilter? currentFilter, bool currentViewAllSongs, AppTheme theme)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        Text = "View Filter";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(440, 205), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        _viewAllSongsCheckBox.Checked = currentViewAllSongs;
        _filterSongsCheckBox.Checked = currentFilter is not null && !currentViewAllSongs;
        _viewAllSongsCheckBox.ForeColor = _theme.TextColor;
        _viewAllSongsCheckBox.BackColor = _theme.AppBackColor;
        _viewAllSongsCheckBox.CheckedChanged += (_, _) =>
        {
            if (_viewAllSongsCheckBox.Checked)
            {
                _filterSongsCheckBox.Checked = false;
            }
        };

        _filterSongsCheckBox.CheckedChanged += (_, _) =>
        {
            if (_filterSongsCheckBox.Checked)
            {
                _viewAllSongsCheckBox.Checked = false;
            }

            UpdateFilterControlState();
        };

        BuildLayout(currentFilter);
        UpdateFilterControlState();
    }

    private void BuildLayout(SongAgeFilter? currentFilter)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };

        _songsLabel.AutoSize = true;
        _songsLabel.Text = "Songs";
        _songsLabel.TextAlign = ContentAlignment.MiddleLeft;
        _songsLabel.Margin = new Padding(0, 8, 8, 0);
        _songsLabel.ForeColor = _theme.TextColor;

        _operatorComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _operatorComboBox.Width = 100;
        _operatorComboBox.Items.AddRange(new object[] { "less than", "older than" });
        _operatorComboBox.SelectedIndex = currentFilter?.Operator == SongAgeFilterOperator.OlderThan ? 1 : 0;
        StyleComboBox(_operatorComboBox);

        _daysComboBox.DropDownStyle = ComboBoxStyle.DropDown;
        _daysComboBox.Width = 80;
        _daysComboBox.Items.AddRange(new object[] { "30", "60", "90", "120", "360" });
        _daysComboBox.Text = currentFilter?.Days.ToString() ?? "30";
        StyleComboBox(_daysComboBox);

        _daysLabel.AutoSize = true;
        _daysLabel.Text = "days";
        _daysLabel.TextAlign = ContentAlignment.MiddleLeft;
        _daysLabel.Margin = new Padding(8, 8, 0, 0);
        _daysLabel.ForeColor = _theme.TextColor;

        _oldLabel.AutoSize = true;
        _oldLabel.Text = "old";
        _oldLabel.TextAlign = ContentAlignment.MiddleLeft;
        _oldLabel.Margin = new Padding(4, 8, 0, 0);
        _oldLabel.ForeColor = _theme.TextColor;

        _filterSongsCheckBox.Margin = new Padding(0, 6, 8, 0);
        _filterSongsCheckBox.BackColor = _theme.AppBackColor;
        _filterSongsCheckBox.ForeColor = _theme.TextColor;

        filterPanel.Controls.Add(_filterSongsCheckBox);
        filterPanel.Controls.Add(_songsLabel);
        filterPanel.Controls.Add(_operatorComboBox);
        filterPanel.Controls.Add(_daysComboBox);
        filterPanel.Controls.Add(_daysLabel);
        filterPanel.Controls.Add(_oldLabel);
        layout.Controls.Add(filterPanel, 0, 0);

        var customizeViewButton = new Button
        {
            Text = "Customize View",
            AutoSize = true,
        };
        customizeViewButton.Click += (_, _) =>
        {
            var selectedFilter = BuildSelectedFilter();
            if (_filterSongsCheckBox.Checked && selectedFilter is null)
            {
                return;
            }

            RequestedAction = SongAgeFilterDialogAction.CustomizeView;
            SelectedFilter = selectedFilter;
            ViewAllSongsSelected = _viewAllSongsCheckBox.Checked;
            DialogResult = DialogResult.OK;
            Close();
        };
        StyleButton(customizeViewButton, useAccent: false);

        var spacer = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 10,
            BackColor = _theme.AppBackColor
        };
        layout.Controls.Add(spacer, 0, 2);

        var viewAllLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        viewAllLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        viewAllLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var viewAllPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        _viewAllSongsCheckBox.Margin = new Padding(0, 4, 0, 0);
        viewAllPanel.Controls.Add(_viewAllSongsCheckBox);
        viewAllLayout.Controls.Add(viewAllPanel, 0, 0);
        customizeViewButton.Margin = new Padding(16, 0, 0, 0);
        viewAllLayout.Controls.Add(customizeViewButton, 1, 0);
        layout.Controls.Add(viewAllLayout, 0, 3);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 12, 0, 0)
        };

        var okButton = new Button
        {
            Text = "OK",
            AutoSize = true
        };
        okButton.Click += (_, _) =>
        {
            var selectedFilter = BuildSelectedFilter();
            if (_filterSongsCheckBox.Checked && selectedFilter is null)
            {
                return;
            }

            RequestedAction = SongAgeFilterDialogAction.Apply;
            SelectedFilter = selectedFilter;
            ViewAllSongsSelected = _viewAllSongsCheckBox.Checked;
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
        layout.Controls.Add(buttonPanel, 0, 4);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private SongAgeFilter? BuildSelectedFilter()
    {
        if (!_filterSongsCheckBox.Checked)
        {
            return null;
        }

        if (!int.TryParse(_daysComboBox.Text.Trim(), out var days) || days <= 0)
        {
            using var messageDialog = new ThemedMessageForm(Text, "Enter a whole number of days greater than zero.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return null;
        }

        return new SongAgeFilter
        {
            Operator = _operatorComboBox.SelectedIndex == 0 ? SongAgeFilterOperator.LessThan : SongAgeFilterOperator.OlderThan,
            Days = days
        };
    }

    private void UpdateFilterControlState()
    {
        var enabled = _filterSongsCheckBox.Checked;
        _songsLabel.Enabled = enabled;
        _operatorComboBox.Enabled = enabled;
        _daysComboBox.Enabled = enabled;
        _daysLabel.Enabled = enabled;
        _oldLabel.Enabled = enabled;
    }

    private void StyleComboBox(ComboBox comboBox)
    {
        comboBox.BackColor = _theme.PanelBackColor;
        comboBox.ForeColor = _theme.TextColor;
        comboBox.FlatStyle = FlatStyle.Flat;
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
