namespace SongMetainfoBrowser.App;

internal sealed class SongStatisticsField
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required IReadOnlyList<string> Values { get; init; }
    public IReadOnlyList<string>? PreferredOrder { get; init; }
    public string? ComparisonValue { get; init; }
    public string? ComparisonDisplayValue { get; init; }

    public override string ToString() => Label;
}

internal sealed class SongStatisticsForm : Form
{
    private sealed record StatisticsGroup(string Value, int Count);
    private sealed record ChartSlice(string Label, int Count, Color Color);

    private sealed class DoughnutChartPanel : Panel
    {
        private readonly AppTheme _theme;
        private readonly ToolTip _toolTip = new();
        private IReadOnlyList<ChartSlice> _slices = Array.Empty<ChartSlice>();
        private Rectangle _chartBounds;
        private int _total;
        private string _title = "";
        private string? _lastToolTip;

        public DoughnutChartPanel(AppTheme theme)
        {
            _theme = theme;
            Dock = DockStyle.Fill;
            BackColor = theme.PanelBackColor;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        public void SetData(string title, int total, IReadOnlyList<ChartSlice> slices)
        {
            _title = title;
            _total = total;
            _slices = slices;
            _lastToolTip = null;
            _toolTip.SetToolTip(this, null);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            base.OnPaint(args);
            args.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var contentBounds = Rectangle.Inflate(ClientRectangle, -20, -18);
            if (contentBounds.Width <= 0 || contentBounds.Height <= 0)
                return;

            using var titleFont = new Font(Font, FontStyle.Bold);
            TextRenderer.DrawText(
                args.Graphics,
                _title,
                titleFont,
                new Rectangle(contentBounds.Left, contentBounds.Top, contentBounds.Width, 28),
                _theme.TextColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (_total <= 0 || _slices.Count == 0)
            {
                TextRenderer.DrawText(
                    args.Graphics,
                    "No chart data is available.",
                    Font,
                    contentBounds,
                    _theme.MutedTextColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                _chartBounds = Rectangle.Empty;
                return;
            }

            var bodyTop = contentBounds.Top + 34;
            var bodyHeight = Math.Max(1, contentBounds.Bottom - bodyTop);
            var legendWidth = Math.Clamp(contentBounds.Width * 42 / 100, 210, 360);
            var chartAreaWidth = Math.Max(1, contentBounds.Width - legendWidth - 24);
            var diameter = Math.Max(80, Math.Min(chartAreaWidth, bodyHeight) - 12);
            _chartBounds = new Rectangle(
                contentBounds.Left + Math.Max(0, (chartAreaWidth - diameter) / 2),
                bodyTop + Math.Max(0, (bodyHeight - diameter) / 2),
                diameter,
                diameter);

            var startAngle = -90f;
            foreach (var slice in _slices)
            {
                var sweepAngle = slice.Count * 360f / _total;
                using var brush = new SolidBrush(slice.Color);
                args.Graphics.FillPie(brush, _chartBounds, startAngle, sweepAngle);
                startAngle += sweepAngle;
            }

            var holeBounds = Rectangle.Inflate(
                _chartBounds,
                -(int)Math.Round(_chartBounds.Width * 0.28),
                -(int)Math.Round(_chartBounds.Height * 0.28));
            using (var holeBrush = new SolidBrush(_theme.PanelBackColor))
                args.Graphics.FillEllipse(holeBrush, holeBounds);

            using var totalFont = new Font(Font.FontFamily, Font.Size + 8f, FontStyle.Bold, Font.Unit);
            var centerText = $"{_total}\nsongs";
            TextRenderer.DrawText(
                args.Graphics,
                centerText,
                totalFont,
                holeBounds,
                _theme.TextColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            var legendLeft = contentBounds.Right - legendWidth;
            var legendTop = bodyTop + Math.Max(0, (bodyHeight - _slices.Count * 30) / 2);
            for (var index = 0; index < _slices.Count; index++)
            {
                var slice = _slices[index];
                var rowTop = legendTop + index * 30;
                using var swatchBrush = new SolidBrush(slice.Color);
                args.Graphics.FillRectangle(swatchBrush, legendLeft, rowTop + 6, 14, 14);

                var percentage = slice.Count * 100d / _total;
                var legendText = $"{slice.Label} — {slice.Count} ({percentage:0.0}%)";
                TextRenderer.DrawText(
                    args.Graphics,
                    legendText,
                    Font,
                    new Rectangle(legendLeft + 22, rowTop, legendWidth - 22, 26),
                    _theme.TextColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        protected override void OnMouseMove(MouseEventArgs args)
        {
            base.OnMouseMove(args);
            var toolTipText = GetSliceToolTip(args.Location);
            if (string.Equals(toolTipText, _lastToolTip, StringComparison.Ordinal))
                return;

            _lastToolTip = toolTipText;
            _toolTip.SetToolTip(this, toolTipText);
        }

        private string? GetSliceToolTip(Point location)
        {
            if (_chartBounds.IsEmpty || _total <= 0)
                return null;

            var centerX = _chartBounds.Left + _chartBounds.Width / 2d;
            var centerY = _chartBounds.Top + _chartBounds.Height / 2d;
            var deltaX = location.X - centerX;
            var deltaY = location.Y - centerY;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            var outerRadius = _chartBounds.Width / 2d;
            var innerRadius = outerRadius * 0.44;
            if (distance < innerRadius || distance > outerRadius)
                return null;

            var angle = Math.Atan2(deltaY, deltaX) * 180d / Math.PI;
            var normalizedAngle = (angle + 90d + 360d) % 360d;
            var accumulatedAngle = 0d;
            foreach (var slice in _slices)
            {
                accumulatedAngle += slice.Count * 360d / _total;
                if (normalizedAngle <= accumulatedAngle)
                {
                    var percentage = slice.Count * 100d / _total;
                    return $"{slice.Label}: {slice.Count} song{(slice.Count == 1 ? "" : "s")} ({percentage:0.0}%)";
                }
            }

            return null;
        }
    }

    private readonly AppTheme _theme;
    private readonly IReadOnlyList<SongStatisticsField> _fields;
    private readonly ComboBox _fieldComboBox = new();
    private readonly Label _totalLabel = new();
    private readonly DataGridView _resultsGrid = new();
    private readonly Panel _contentPanel = new();
    private readonly DoughnutChartPanel _chartPanel;
    private readonly bool _comparisonMode;
    private readonly int _catalogSongCount;
    private readonly Button _tableViewButton = new() { Text = "Table", AutoSize = true };
    private readonly Button _chartViewButton = new() { Text = "Chart", AutoSize = true };
    private bool _showChart;
    private readonly SongStatisticsField _allFieldsOption = new()
    {
        Key = "all",
        Label = "All",
        Values = Array.Empty<string>()
    };

    public SongStatisticsForm(
        IReadOnlyList<SongStatisticsField> fields,
        int songCount,
        AppTheme theme,
        string? preferredFieldKey = null,
        int catalogSongCount = 0)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        _fields = fields;
        _comparisonMode = songCount == 1 && catalogSongCount > 0;
        _catalogSongCount = catalogSongCount;
        _chartPanel = new DoughnutChartPanel(theme);

        Text = _comparisonMode ? "SongLens Catalog Comparison" : "SongLens View Statistics";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        MinimumSize = SizeFromClientSize(AppFontSettings.Scale(new Size(600, 380), fontPreferences, AppFontSection.Dialogs));
        ClientSize = AppFontSettings.Scale(new Size(760, 500), fontPreferences, AppFontSection.Dialogs);
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

        var selectorRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 4,
            Margin = new Padding(0, 0, 0, 10),
            BackColor = _theme.AppBackColor
        };
        selectorRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        selectorRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        selectorRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        selectorRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        selectorRow.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Show:",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 5, 8, 0)
        }, 0, 0);

        _fieldComboBox.Dock = DockStyle.Fill;
        _fieldComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _fieldComboBox.BackColor = _theme.PanelBackColor;
        _fieldComboBox.ForeColor = _theme.TextColor;
        _fieldComboBox.Items.Add(_allFieldsOption);
        _fieldComboBox.Items.AddRange(_fields.Cast<object>().ToArray());
        selectorRow.Controls.Add(_fieldComboBox, 1, 0);

        var viewTogglePanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(12, 0, 0, 0),
            BackColor = _theme.AppBackColor
        };
        _tableViewButton.Click += (_, _) => SetChartView(false);
        _chartViewButton.Click += (_, _) => RequestChartView();
        viewTogglePanel.Controls.Add(_tableViewButton);
        viewTogglePanel.Controls.Add(_chartViewButton);
        selectorRow.Controls.Add(viewTogglePanel, 2, 0);

        _totalLabel.AutoSize = true;
        _totalLabel.Text = _comparisonMode
            ? $"Comparing this song with {_catalogSongCount} catalog song{(_catalogSongCount == 1 ? "" : "s")}"
            : $"{songCount} song{(songCount == 1 ? "" : "s")} in current view";
        _totalLabel.ForeColor = _theme.MutedTextColor;
        _totalLabel.Margin = new Padding(12, 5, 0, 0);
        selectorRow.Controls.Add(_totalLabel, 3, 0);
        layout.Controls.Add(selectorRow, 0, 0);

        ConfigureResultsGrid();
        _fieldComboBox.SelectedIndexChanged += (_, _) => PopulateStatistics();
        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.BackColor = _theme.PanelBackColor;
        _contentPanel.Controls.Add(_resultsGrid);
        _contentPanel.Controls.Add(_chartPanel);
        layout.Controls.Add(_contentPanel, 0, 1);
        StyleViewToggleButtons();

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 10, 0, 0),
            BackColor = _theme.AppBackColor
        };
        var closeButton = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.OK };
        StyleButton(closeButton);
        buttonPanel.Controls.Add(closeButton);
        layout.Controls.Add(buttonPanel, 0, 2);
        AcceptButton = closeButton;
        CancelButton = closeButton;

        if (_fieldComboBox.Items.Count > 0)
        {
            var preferredIndex = string.IsNullOrWhiteSpace(preferredFieldKey)
                ? 0
                : _fields.ToList().FindIndex(field => string.Equals(field.Key, preferredFieldKey, StringComparison.OrdinalIgnoreCase)) + 1;
            var selectedIndex = preferredIndex > 0 ? preferredIndex : 0;
            _fieldComboBox.SelectedIndex = Math.Min(selectedIndex, _fieldComboBox.Items.Count - 1);
            PopulateStatistics();
        }
    }

    private void ConfigureResultsGrid()
    {
        _resultsGrid.Dock = DockStyle.Fill;
        _resultsGrid.AllowUserToAddRows = false;
        _resultsGrid.AllowUserToDeleteRows = false;
        _resultsGrid.AllowUserToResizeRows = false;
        _resultsGrid.AutoGenerateColumns = false;
        _resultsGrid.BackgroundColor = _theme.PanelBackColor;
        _resultsGrid.BorderStyle = BorderStyle.FixedSingle;
        _resultsGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _resultsGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _resultsGrid.EnableHeadersVisualStyles = false;
        _resultsGrid.GridColor = _theme.BorderColor;
        _resultsGrid.MultiSelect = false;
        _resultsGrid.ReadOnly = true;
        _resultsGrid.RowHeadersVisible = false;
        _resultsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _resultsGrid.ShowCellToolTips = false;
        _resultsGrid.ColumnHeadersDefaultCellStyle.BackColor = _theme.HeaderBackColor;
        _resultsGrid.ColumnHeadersDefaultCellStyle.ForeColor = _theme.TextColor;
        _resultsGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _theme.HeaderBackColor;
        _resultsGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _theme.TextColor;
        _resultsGrid.DefaultCellStyle.BackColor = _theme.PanelBackColor;
        _resultsGrid.DefaultCellStyle.ForeColor = _theme.TextColor;
        _resultsGrid.DefaultCellStyle.SelectionBackColor = _theme.TreeSelectionColor;
        _resultsGrid.DefaultCellStyle.SelectionForeColor = _theme.SelectedTextColor;
        _resultsGrid.AlternatingRowsDefaultCellStyle.BackColor = _theme.PanelAltBackColor;
        _resultsGrid.CellPainting += DrawDistributionBar;

        _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Category",
            HeaderText = "Category",
            Width = 130,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Value",
            HeaderText = "Value",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 180,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Count",
            HeaderText = "Songs",
            Width = 80,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight },
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Percent",
            HeaderText = "Percent",
            Width = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight },
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Distribution",
            HeaderText = "Distribution",
            Width = 220,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        if (_comparisonMode)
        {
            _resultsGrid.Columns["Value"]!.HeaderText = "This Song";
            _resultsGrid.Columns["Count"]!.HeaderText = "Catalog Songs";
            _resultsGrid.Columns["Count"]!.Width = 110;
            _resultsGrid.Columns["Percent"]!.HeaderText = "Catalog %";
        }

    }

    private void PopulateStatistics()
    {
        _resultsGrid.Rows.Clear();
        if (_fieldComboBox.SelectedItem is not SongStatisticsField field)
        {
            _chartViewButton.Enabled = false;
            SetChartView(false);
            return;
        }

        if (ReferenceEquals(field, _allFieldsOption))
        {
            _chartViewButton.Enabled = true;
            SetChartView(false);
            PopulateAllStatistics();
            return;
        }

        if (field.Values.Count == 0)
        {
            _chartViewButton.Enabled = false;
            SetChartView(false);
            return;
        }

        _chartViewButton.Enabled = true;
        _chartPanel.SetData(field.Label, field.Values.Count, BuildChartSlices(field));
        _resultsGrid.Columns["Category"]!.Visible = false;
        if (_comparisonMode)
        {
            AddComparisonRow(field, showCategory: false);
        }
        else
        {
            AddStatisticsRows(field, showCategory: false);
        }

        _resultsGrid.ClearSelection();
        SetChartView(_showChart);
    }

    private void PopulateAllStatistics()
    {
        _resultsGrid.Columns["Category"]!.Visible = true;
        foreach (var field in _fields)
        {
            if (_comparisonMode)
            {
                AddComparisonRow(field, showCategory: true);
            }
            else
            {
                AddStatisticsRows(field, showCategory: true);
            }
        }

        _resultsGrid.ClearSelection();
    }

    private void AddComparisonRow(SongStatisticsField field, bool showCategory)
    {
        if (field.Values.Count == 0 || string.IsNullOrWhiteSpace(field.ComparisonValue))
        {
            return;
        }

        var count = field.Values.Count(
            value => string.Equals(value, field.ComparisonValue, StringComparison.OrdinalIgnoreCase));
        var percentage = count * 100d / field.Values.Count;
        _resultsGrid.Rows.Add(
            showCategory ? field.Label : "",
            field.ComparisonDisplayValue ?? field.ComparisonValue,
            $"{count} of {field.Values.Count}",
            $"{percentage:0.0}%",
            percentage);
    }

    private void AddStatisticsRows(SongStatisticsField field, bool showCategory)
    {
        if (field.Values.Count == 0)
        {
            return;
        }

        var groups = GetStatisticsGroups(field);

        for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            var group = groups[groupIndex];
            var percentage = group.Count * 100d / field.Values.Count;
            _resultsGrid.Rows.Add(
                showCategory && groupIndex == 0 ? field.Label : "",
                group.Value,
                group.Count,
                $"{percentage:0.0}%",
                percentage);
        }
    }

    private static IReadOnlyList<StatisticsGroup> GetStatisticsGroups(SongStatisticsField field)
    {
        var orderByValue = field.PreferredOrder?
            .Select((value, index) => (value, index))
            .ToDictionary(item => item.value, item => item.index, StringComparer.OrdinalIgnoreCase);
        return field.Values
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Select(group => new StatisticsGroup(group.Key, group.Count()))
            .OrderBy(group => orderByValue is not null
                && orderByValue.TryGetValue(group.Value, out var index) ? index : int.MaxValue)
            .ThenByDescending(group => group.Count)
            .ThenBy(group => group.Value, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private IReadOnlyList<ChartSlice> BuildChartSlices(SongStatisticsField field)
    {
        var groups = GetStatisticsGroups(field);
        if (groups.Count == 0)
            return Array.Empty<ChartSlice>();

        var includedIndexes = new HashSet<int>();
        if (groups.Count <= 6)
        {
            for (var index = 0; index < groups.Count; index++)
                includedIndexes.Add(index);
        }
        else
        {
            for (var index = 0; index < groups.Count && includedIndexes.Count < 8; index++)
            {
                if (groups[index].Count * 100d / field.Values.Count >= 3d)
                    includedIndexes.Add(index);
            }

            foreach (var candidate in groups
                    .Select((group, index) => (group, index))
                    .OrderByDescending(item => item.group.Count)
                    .ThenBy(item => item.index))
            {
                if (includedIndexes.Count >= Math.Min(3, groups.Count))
                    break;
                includedIndexes.Add(candidate.index);
            }
        }

        var palette = BuildChartPalette();
        var slices = new List<ChartSlice>();
        var otherCount = 0;
        for (var index = 0; index < groups.Count; index++)
        {
            var group = groups[index];
            if (!includedIndexes.Contains(index))
            {
                otherCount += group.Count;
                continue;
            }

            slices.Add(new ChartSlice(
                group.Value,
                group.Count,
                palette[slices.Count % palette.Count]));
        }

        if (otherCount > 0)
            slices.Add(new ChartSlice("Other", otherCount, _theme.MutedTextColor));
        return slices;
    }

    private IReadOnlyList<Color> BuildChartPalette()
    {
        return
        [
            _theme.AccentColor,
            Color.FromArgb(76, 155, 232),
            Color.FromArgb(228, 166, 74),
            Color.FromArgb(98, 183, 136),
            Color.FromArgb(176, 122, 216),
            Color.FromArgb(224, 116, 116),
            Color.FromArgb(99, 185, 188),
            Color.FromArgb(211, 141, 176)
        ];
    }

    private void SetChartView(bool showChart)
    {
        _showChart = showChart && _chartViewButton.Enabled;
        _resultsGrid.Visible = !_showChart;
        _chartPanel.Visible = _showChart;
        if (_showChart)
            _chartPanel.BringToFront();
        else
            _resultsGrid.BringToFront();
        StyleViewToggleButtons();
    }

    private void RequestChartView()
    {
        if (_fieldComboBox.SelectedItem is not SongStatisticsField field
            || ReferenceEquals(field, _allFieldsOption))
        {
            ShowChartSelectionInformation();
            return;
        }

        SetChartView(true);
    }

    private void ShowChartSelectionInformation()
    {
        if (BrowserConfigStore.LoadSuppressStatisticsChartSelectionInfo())
            return;

        using var dialog = new ThemedMessageForm(
            "Statistics Chart",
            "Select a specific item from the Show list before displaying a chart. "
                + "The All view combines unrelated statistics and is available as a table only.",
            _theme,
            ThemedMessageKind.Information,
            optionText: "Don't show this again");
        dialog.ShowDialog(this);

        if (dialog.OptionChecked)
            BrowserConfigStore.SaveSuppressStatisticsChartSelectionInfo(true);
    }

    private void StyleViewToggleButtons()
    {
        StyleButton(_tableViewButton);
        StyleButton(_chartViewButton);
        _tableViewButton.BackColor = _showChart ? _theme.AccentSoftColor : _theme.TreeSelectionColor;
        _tableViewButton.ForeColor = _showChart ? _theme.TextColor : _theme.SelectedTextColor;
        _tableViewButton.FlatAppearance.BorderColor = _showChart ? _theme.BorderColor : _theme.AccentColor;
        _chartViewButton.BackColor = _showChart ? _theme.TreeSelectionColor : _theme.AccentSoftColor;
        _chartViewButton.ForeColor = !_chartViewButton.Enabled
            ? _theme.MutedTextColor
            : _showChart ? _theme.SelectedTextColor : _theme.TextColor;
        _chartViewButton.FlatAppearance.BorderColor = _showChart ? _theme.AccentColor : _theme.BorderColor;
    }

    private void DrawDistributionBar(object? sender, DataGridViewCellPaintingEventArgs args)
    {
        if (args.RowIndex < 0
            || args.ColumnIndex < 0
            || _resultsGrid.Columns[args.ColumnIndex].Name != "Distribution"
            || args.Graphics is not Graphics graphics)
        {
            return;
        }

        args.Paint(args.ClipBounds,
            DataGridViewPaintParts.Background
            | DataGridViewPaintParts.Border
            | DataGridViewPaintParts.SelectionBackground);

        var percentage = double.TryParse(_resultsGrid.Rows[args.RowIndex].Cells[args.ColumnIndex].Value?.ToString(), out var value)
            ? Math.Clamp(value, 0d, 100d)
            : 0d;
        var barBounds = Rectangle.Inflate(args.CellBounds, -5, -5);
        barBounds.Width = (int)Math.Round(barBounds.Width * percentage / 100d);
        if (barBounds.Width > 0)
        {
            using var brush = new SolidBrush(_theme.AccentColor);
            graphics.FillRectangle(brush, barBounds);
        }

        args.Handled = true;
    }

    private void StyleButton(Button button)
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
