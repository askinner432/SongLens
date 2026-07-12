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

internal enum SongAgeFilterMode
{
    RelativeDays,
    DateRange
}

internal enum SongDateField
{
    Modified,
    Created
}

internal sealed class SongAgeFilter
{
    public required SongAgeFilterMode Mode { get; init; }
    public SongDateField DateField { get; init; } = SongDateField.Modified;
    public SongAgeFilterOperator Operator { get; init; }
    public int Days { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }

    public string OperatorText => Operator == SongAgeFilterOperator.LessThan ? "less than" : "older than";
}

internal sealed class SongAgeFilterForm : Form
{
    private readonly AppTheme _theme;
    private readonly RadioButton _filterSongsRadioButton = new() { AutoSize = true };
    private readonly RadioButton _filterBetweenDatesRadioButton = new() { AutoSize = true };
    private readonly RadioButton _viewAllSongsRadioButton = new() { AutoSize = true, Text = "View All Songs" };
    private readonly ComboBox _operatorComboBox = new();
    private readonly ComboBox _dateFieldComboBox = new();
    private readonly ComboBox _daysComboBox = new();
    private readonly Label _songsLabel = new();
    private readonly Label _daysLabel = new();
    private readonly Label _oldLabel = new();
    private readonly Label _betweenLabel = new();
    private readonly Label _andLabel = new();
    private readonly DateTimePicker _startDatePicker = new();
    private readonly DateTimePicker _endDatePicker = new();

    public SongAgeFilter? SelectedFilter { get; private set; }
    public SongAgeFilter? FilterPreference { get; private set; }
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
        ClientSize = AppFontSettings.Scale(new Size(560, 275), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        ConfigureRadioButton(_viewAllSongsRadioButton);
        ConfigureRadioButton(_filterSongsRadioButton);
        ConfigureRadioButton(_filterBetweenDatesRadioButton);

        _viewAllSongsRadioButton.CheckedChanged += (_, _) =>
        {
            if (_viewAllSongsRadioButton.Checked)
            {
                _filterSongsRadioButton.Checked = false;
                _filterBetweenDatesRadioButton.Checked = false;
            }

            UpdateFilterControlState();
        };
        _filterSongsRadioButton.CheckedChanged += (_, _) =>
        {
            if (_filterSongsRadioButton.Checked)
            {
                _viewAllSongsRadioButton.Checked = false;
                _filterBetweenDatesRadioButton.Checked = false;
            }

            UpdateFilterControlState();
        };
        _filterBetweenDatesRadioButton.CheckedChanged += (_, _) =>
        {
            if (_filterBetweenDatesRadioButton.Checked)
            {
                _viewAllSongsRadioButton.Checked = false;
                _filterSongsRadioButton.Checked = false;
            }

            UpdateFilterControlState();
        };

        BuildLayout(currentFilter);
        ApplyInitialSelection(currentFilter, currentViewAllSongs);
        UpdateFilterControlState();
    }

    private void BuildLayout(SongAgeFilter? currentFilter)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        layout.Controls.Add(BuildRelativeDaysPanel(currentFilter), 0, 0);
        layout.Controls.Add(new Panel { Dock = DockStyle.Fill, Height = 14, BackColor = _theme.AppBackColor }, 0, 1);
        layout.Controls.Add(BuildDateRangePanel(currentFilter), 0, 2);
        layout.Controls.Add(new Panel { Dock = DockStyle.Fill, Height = 14, BackColor = _theme.AppBackColor }, 0, 3);
        layout.Controls.Add(BuildViewAllRow(), 0, 4);
        layout.Controls.Add(BuildButtonPanel(), 0, 6);
    }

    private FlowLayoutPanel BuildRelativeDaysPanel(SongAgeFilter? currentFilter)
    {
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
        _songsLabel.Margin = new Padding(0, 6, 8, 0);
        _songsLabel.ForeColor = _theme.TextColor;

        _operatorComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _operatorComboBox.Width = 100;
        _operatorComboBox.Items.AddRange(new object[] { "less than", "older than" });
        _operatorComboBox.SelectedIndex = currentFilter?.Mode == SongAgeFilterMode.RelativeDays && currentFilter.Operator == SongAgeFilterOperator.OlderThan ? 1 : 0;
        StyleComboBox(_operatorComboBox);

        _daysComboBox.DropDownStyle = ComboBoxStyle.DropDown;
        _daysComboBox.Width = 80;
        _daysComboBox.Items.AddRange(new object[] { "30", "60", "90", "120", "360" });
        _daysComboBox.Text = currentFilter?.Mode == SongAgeFilterMode.RelativeDays && currentFilter.Days > 0 ? currentFilter.Days.ToString() : "30";
        StyleComboBox(_daysComboBox);

        _daysLabel.AutoSize = true;
        _daysLabel.Text = "days";
        _daysLabel.TextAlign = ContentAlignment.MiddleLeft;
        _daysLabel.Margin = new Padding(8, 6, 0, 0);
        _daysLabel.ForeColor = _theme.TextColor;

        _oldLabel.AutoSize = true;
        _oldLabel.Text = "old";
        _oldLabel.TextAlign = ContentAlignment.MiddleLeft;
        _oldLabel.Margin = new Padding(4, 6, 0, 0);
        _oldLabel.ForeColor = _theme.TextColor;

        _filterSongsRadioButton.Margin = new Padding(0, 6, 8, 0);
        filterPanel.Controls.Add(_filterSongsRadioButton);
        filterPanel.Controls.Add(_songsLabel);
        filterPanel.Controls.Add(_operatorComboBox);
        filterPanel.Controls.Add(_daysComboBox);
        filterPanel.Controls.Add(_daysLabel);
        filterPanel.Controls.Add(_oldLabel);
        return filterPanel;
    }

    private FlowLayoutPanel BuildDateRangePanel(SongAgeFilter? currentFilter)
    {
        var dateRangePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };

        var defaultEndDate = currentFilter?.Mode == SongAgeFilterMode.DateRange && currentFilter.EndDate is DateTime configuredEndDate
            ? configuredEndDate.Date
            : DateTime.Today;
        var defaultStartDate = currentFilter?.Mode == SongAgeFilterMode.DateRange && currentFilter.StartDate is DateTime configuredStartDate
            ? configuredStartDate.Date
            : defaultEndDate.AddDays(-30);

        _dateFieldComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _dateFieldComboBox.Width = 90;
        _dateFieldComboBox.Items.AddRange(new object[] { "Modified", "Created" });
        _dateFieldComboBox.SelectedIndex = currentFilter?.DateField == SongDateField.Created ? 1 : 0;
        StyleComboBox(_dateFieldComboBox);

        _betweenLabel.AutoSize = true;
        _betweenLabel.Text = "between";
        _betweenLabel.TextAlign = ContentAlignment.MiddleLeft;
        _betweenLabel.Margin = new Padding(8, 6, 8, 0);
        _betweenLabel.ForeColor = _theme.TextColor;

        _startDatePicker.Format = DateTimePickerFormat.Short;
        _startDatePicker.Width = 110;
        _startDatePicker.Value = defaultStartDate;
        StyleDatePicker(_startDatePicker);

        _andLabel.AutoSize = true;
        _andLabel.Text = "and";
        _andLabel.TextAlign = ContentAlignment.MiddleLeft;
        _andLabel.Margin = new Padding(8, 6, 8, 0);
        _andLabel.ForeColor = _theme.TextColor;

        _endDatePicker.Format = DateTimePickerFormat.Short;
        _endDatePicker.Width = 110;
        _endDatePicker.Value = defaultEndDate;
        StyleDatePicker(_endDatePicker);

        _filterBetweenDatesRadioButton.Margin = new Padding(0, 6, 8, 0);
        dateRangePanel.Controls.Add(_filterBetweenDatesRadioButton);
        dateRangePanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Songs",
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 6, 8, 0),
            ForeColor = _theme.TextColor,
            BackColor = _theme.AppBackColor
        });
        dateRangePanel.Controls.Add(_dateFieldComboBox);
        dateRangePanel.Controls.Add(_betweenLabel);
        dateRangePanel.Controls.Add(_startDatePicker);
        dateRangePanel.Controls.Add(_andLabel);
        dateRangePanel.Controls.Add(_endDatePicker);
        return dateRangePanel;
    }

    private TableLayoutPanel BuildViewAllRow()
    {
        var customizeViewButton = new Button
        {
            Text = "Customize View",
            AutoSize = true
        };
        customizeViewButton.Click += (_, _) =>
        {
            var configuredFilter = BuildConfiguredFilter();
            if (configuredFilter is null)
            {
                return;
            }

            RequestedAction = SongAgeFilterDialogAction.CustomizeView;
            FilterPreference = configuredFilter;
            SelectedFilter = _viewAllSongsRadioButton.Checked ? null : configuredFilter;
            ViewAllSongsSelected = _viewAllSongsRadioButton.Checked;
            DialogResult = DialogResult.OK;
            Close();
        };
        StyleButton(customizeViewButton, useAccent: false);

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
        _viewAllSongsRadioButton.Margin = new Padding(0, 4, 0, 0);
        viewAllPanel.Controls.Add(_viewAllSongsRadioButton);
        viewAllLayout.Controls.Add(viewAllPanel, 0, 0);
        customizeViewButton.Margin = new Padding(16, 0, 0, 0);
        viewAllLayout.Controls.Add(customizeViewButton, 1, 0);
        return viewAllLayout;
    }

    private FlowLayoutPanel BuildButtonPanel()
    {
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 28, 0, 0)
        };

        var okButton = new Button
        {
            Text = "OK",
            AutoSize = true
        };
        okButton.Click += (_, _) =>
        {
            var configuredFilter = BuildConfiguredFilter();
            if (configuredFilter is null)
            {
                return;
            }

            RequestedAction = SongAgeFilterDialogAction.Apply;
            FilterPreference = configuredFilter;
            SelectedFilter = _viewAllSongsRadioButton.Checked ? null : configuredFilter;
            ViewAllSongsSelected = _viewAllSongsRadioButton.Checked;
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

        AcceptButton = okButton;
        CancelButton = cancelButton;
        return buttonPanel;
    }

    private SongAgeFilter? BuildConfiguredFilter()
    {
        if (_filterBetweenDatesRadioButton.Checked)
        {
            var startDate = _startDatePicker.Value.Date;
            var endDate = _endDatePicker.Value.Date;
            if (startDate > endDate)
            {
                using var messageDialog = new ThemedMessageForm(Text, "Choose a start date on or before the end date.", _theme, ThemedMessageKind.Information);
                messageDialog.ShowDialog(this);
                return null;
            }

            return new SongAgeFilter
            {
                Mode = SongAgeFilterMode.DateRange,
                DateField = _dateFieldComboBox.SelectedIndex == 1 ? SongDateField.Created : SongDateField.Modified,
                StartDate = startDate,
                EndDate = endDate
            };
        }

        var rawDays = _daysComboBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(rawDays))
        {
            return new SongAgeFilter
            {
                Mode = SongAgeFilterMode.RelativeDays,
                Operator = _operatorComboBox.SelectedIndex == 0 ? SongAgeFilterOperator.LessThan : SongAgeFilterOperator.OlderThan,
                Days = 30
            };
        }

        if (!int.TryParse(rawDays, out var days) || days <= 0)
        {
            if (_filterSongsRadioButton.Checked)
            {
                using var messageDialog = new ThemedMessageForm(Text, "Enter a whole number of days greater than zero.", _theme, ThemedMessageKind.Information);
                messageDialog.ShowDialog(this);
                return null;
            }

            days = 30;
        }

        return new SongAgeFilter
        {
            Mode = SongAgeFilterMode.RelativeDays,
            Operator = _operatorComboBox.SelectedIndex == 0 ? SongAgeFilterOperator.LessThan : SongAgeFilterOperator.OlderThan,
            Days = days
        };
    }

    private void UpdateFilterControlState()
    {
        var enableRelativeDays = _filterSongsRadioButton.Checked;
        var enableDateRange = _filterBetweenDatesRadioButton.Checked;
        _songsLabel.Enabled = enableRelativeDays;
        _operatorComboBox.Enabled = enableRelativeDays;
        _daysComboBox.Enabled = enableRelativeDays;
        _daysLabel.Enabled = enableRelativeDays;
        _oldLabel.Enabled = enableRelativeDays;
        _dateFieldComboBox.Enabled = enableDateRange;
        _betweenLabel.Enabled = enableDateRange;
        _startDatePicker.Enabled = enableDateRange;
        _andLabel.Enabled = enableDateRange;
        _endDatePicker.Enabled = enableDateRange;
    }

    private void ApplyInitialSelection(SongAgeFilter? currentFilter, bool currentViewAllSongs)
    {
        var useViewAllSongs = currentViewAllSongs && currentFilter is null;
        _viewAllSongsRadioButton.Checked = useViewAllSongs;
        _filterSongsRadioButton.Checked = !useViewAllSongs && (currentFilter?.Mode ?? SongAgeFilterMode.RelativeDays) == SongAgeFilterMode.RelativeDays;
        _filterBetweenDatesRadioButton.Checked = !useViewAllSongs && currentFilter?.Mode == SongAgeFilterMode.DateRange;
    }

    private void ConfigureRadioButton(RadioButton radioButton)
    {
        radioButton.ForeColor = _theme.TextColor;
        radioButton.BackColor = _theme.AppBackColor;
    }

    private void StyleComboBox(ComboBox comboBox)
    {
        comboBox.BackColor = _theme.PanelBackColor;
        comboBox.ForeColor = _theme.TextColor;
        comboBox.FlatStyle = FlatStyle.Flat;
    }

    private void StyleDatePicker(DateTimePicker datePicker)
    {
        datePicker.CalendarMonthBackground = _theme.PanelBackColor;
        datePicker.CalendarForeColor = _theme.TextColor;
        datePicker.CalendarTitleBackColor = _theme.HeaderBackColor;
        datePicker.CalendarTitleForeColor = _theme.TextColor;
        datePicker.CalendarTrailingForeColor = _theme.MutedTextColor;
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
