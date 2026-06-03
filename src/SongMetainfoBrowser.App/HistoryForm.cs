namespace SongMetainfoBrowser.App;

/// <summary>
/// Displays the History subfolder for a selected song and supports deleting saved history copies.
/// </summary>
public sealed class HistoryForm : Form
{
    private sealed class HistoryRow
    {
        public required string FileName { get; init; }
        public required DateTime ModifiedDate { get; init; }
    }

    private readonly SongMetadata _metadata;
    private readonly string _historyFolderPath;
    private readonly Label _pathLabel = new();
    private readonly DataGridView _historyGrid = new();
    private readonly Button _deleteButton = new();
    private readonly Button _cancelButton = new();
    private readonly AppTheme _theme;
    private readonly AppFontPreferences _fontPreferences;
    private bool _suspendGridWidthPersistence;
    private const string HistoryGridKey = "HistoryGrid";

    public HistoryForm(SongMetadata metadata, AppTheme theme)
    {
        _metadata = metadata;
        _theme = theme;
        _fontPreferences = AppFontSettings.LoadPreferences();
        _historyFolderPath = Path.Combine(_metadata.Folder, "History");

        Text = $"History - {_metadata.FileName}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(760, 460), _fontPreferences, AppFontSection.Dialogs);
        MinimumSize = AppFontSettings.Scale(new Size(640, 360), _fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.Dialogs);

        BuildLayout();
        ApplyTheme();
        LoadHistoryRows();
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        _pathLabel.Dock = DockStyle.Fill;
        _pathLabel.AutoSize = true;
        _pathLabel.Margin = new Padding(0, 0, 0, 10);
        _pathLabel.Text = $"Folder: {_historyFolderPath}";
        layout.Controls.Add(_pathLabel, 0, 0);

        ConfigureHistoryGrid();
        ApplySavedGridColumnWidths();
        layout.Controls.Add(_historyGrid, 0, 1);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 0)
        };

        _cancelButton.Text = "Cancel";
        _cancelButton.AutoSize = true;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Click += (_, _) => Close();

        _deleteButton.Text = "Delete";
        _deleteButton.AutoSize = true;
        _deleteButton.Click += (_, _) => DeleteHistoryFiles();

        StyleButton(_cancelButton, useAccent: false);
        StyleButton(_deleteButton, useAccent: true);

        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_deleteButton);
        layout.Controls.Add(buttonPanel, 0, 2);

        AcceptButton = _cancelButton;
        CancelButton = _cancelButton;
    }

    private void ConfigureHistoryGrid()
    {
        _historyGrid.Dock = DockStyle.Fill;
        _historyGrid.AllowUserToAddRows = false;
        _historyGrid.AllowUserToDeleteRows = false;
        _historyGrid.AllowUserToResizeRows = false;
        _historyGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _historyGrid.BackgroundColor = _theme.PanelBackColor;
        _historyGrid.BorderStyle = BorderStyle.None;
        _historyGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _historyGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _historyGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _historyGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
        _historyGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
        _historyGrid.EnableHeadersVisualStyles = false;
        _historyGrid.GridColor = _theme.BorderColor;
        _historyGrid.MultiSelect = false;
        _historyGrid.ReadOnly = true;
        _historyGrid.RowHeadersVisible = false;
        _historyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _historyGrid.ColumnHeadersDefaultCellStyle.BackColor = _theme.HeaderBackColor;
        _historyGrid.ColumnHeadersDefaultCellStyle.ForeColor = _theme.TextColor;
        _historyGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _theme.HeaderBackColor;
        _historyGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _theme.TextColor;
        _historyGrid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        _historyGrid.DefaultCellStyle.BackColor = _theme.PanelBackColor;
        _historyGrid.DefaultCellStyle.ForeColor = _theme.TextColor;
        _historyGrid.DefaultCellStyle.SelectionBackColor = _theme.TreeSelectionColor;
        _historyGrid.DefaultCellStyle.SelectionForeColor = _theme.SelectedTextColor;
        _historyGrid.AlternatingRowsDefaultCellStyle.BackColor = _theme.PanelAltBackColor;
        _historyGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
        _historyGrid.Columns.Add("FileName", "Filename");
        _historyGrid.Columns.Add("ModifiedDate", "Modified Date");
        _historyGrid.Columns["FileName"]!.Width = 470;
        _historyGrid.Columns["ModifiedDate"]!.Width = 220;
        _historyGrid.Columns["FileName"]!.SortMode = DataGridViewColumnSortMode.NotSortable;
        _historyGrid.Columns["ModifiedDate"]!.SortMode = DataGridViewColumnSortMode.NotSortable;
        _historyGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths();
    }

    private void ApplySavedGridColumnWidths()
    {
        var savedWidths = BrowserConfigStore.LoadGridColumnWidths(HistoryGridKey);
        if (savedWidths.Count == 0)
        {
            return;
        }

        _suspendGridWidthPersistence = true;
        try
        {
            foreach (DataGridViewColumn column in _historyGrid.Columns)
            {
                if (!savedWidths.TryGetValue(column.Name, out var width) || width <= 0)
                {
                    continue;
                }

                column.Width = width;
            }
        }
        finally
        {
            _suspendGridWidthPersistence = false;
        }
    }

    private void PersistGridColumnWidths()
    {
        if (_suspendGridWidthPersistence)
        {
            return;
        }

        var widths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewColumn column in _historyGrid.Columns)
        {
            widths[column.Name] = column.Width;
        }

        BrowserConfigStore.SaveGridColumnWidths(HistoryGridKey, widths);
    }

    private void LoadHistoryRows()
    {
        _historyGrid.Rows.Clear();

        if (!Directory.Exists(_historyFolderPath))
        {
            _deleteButton.Enabled = false;
            return;
        }

        foreach (var filePath in Directory.EnumerateFiles(_historyFolderPath, "*", SearchOption.AllDirectories)
                     .OrderByDescending(File.GetLastWriteTime)
                     .ThenBy(Path.GetFileName))
        {
            var fileInfo = new FileInfo(filePath);
            var relativePath = Path.GetRelativePath(_historyFolderPath, filePath);
            var rowIndex = _historyGrid.Rows.Add(relativePath, DateTimeDisplay.Format(fileInfo.LastWriteTime));
            _historyGrid.Rows[rowIndex].Tag = new HistoryRow
            {
                FileName = relativePath,
                ModifiedDate = fileInfo.LastWriteTime
            };
        }

        _deleteButton.Enabled = _historyGrid.Rows.Count > 0;
    }

    private void DeleteHistoryFiles()
    {
        if (!Directory.Exists(_historyFolderPath))
        {
            Close();
            return;
        }

        var result = MessageBox.Show(
            this,
            "Are you sure you want to delete all files in the History folder?",
            "Are You Sure?",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            DeleteHistoryFolderContents(_historyFolderPath);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Could not delete History files.\n\n{ex.Message}",
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void DeleteHistoryFolderContents(string historyFolderPath)
    {
        var rootPath = Path.GetFullPath(historyFolderPath);
        var rootDirectory = new DirectoryInfo(rootPath);
        if (!rootDirectory.Exists)
        {
            return;
        }

        foreach (var file in rootDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            file.Delete();
        }

        foreach (var directory in rootDirectory.EnumerateDirectories("*", SearchOption.AllDirectories).OrderByDescending(dir => dir.FullName.Length))
        {
            if (!directory.EnumerateFileSystemInfos().Any())
            {
                directory.Delete();
            }
        }
    }

    private void ApplyTheme()
    {
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        if (Controls.OfType<TableLayoutPanel>().FirstOrDefault() is { } layout)
        {
            layout.BackColor = _theme.AppBackColor;
            foreach (Control control in layout.Controls)
            {
                control.ForeColor = _theme.TextColor;
                if (control is FlowLayoutPanel)
                {
                    control.BackColor = _theme.AppBackColor;
                }
            }
        }

        _pathLabel.ForeColor = _theme.TextColor;
        _historyGrid.BackgroundColor = _theme.PanelBackColor;
        _historyGrid.GridColor = _theme.BorderColor;
        _historyGrid.ColumnHeadersDefaultCellStyle.BackColor = _theme.HeaderBackColor;
        _historyGrid.ColumnHeadersDefaultCellStyle.ForeColor = _theme.TextColor;
        _historyGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _theme.HeaderBackColor;
        _historyGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _theme.TextColor;
        _historyGrid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        _historyGrid.DefaultCellStyle.BackColor = _theme.PanelBackColor;
        _historyGrid.DefaultCellStyle.ForeColor = _theme.TextColor;
        _historyGrid.DefaultCellStyle.SelectionBackColor = _theme.TreeSelectionColor;
        _historyGrid.DefaultCellStyle.SelectionForeColor = _theme.SelectedTextColor;
        _historyGrid.AlternatingRowsDefaultCellStyle.BackColor = _theme.PanelAltBackColor;

        StyleButton(_cancelButton, useAccent: false);
        StyleButton(_deleteButton, useAccent: true);
    }

    private void StyleButton(Button button, bool useAccent)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = useAccent ? _theme.AccentHoverColor : _theme.NeutralHoverColor;
        button.FlatAppearance.MouseDownBackColor = useAccent ? _theme.AccentPressedColor : _theme.NeutralPressedColor;
        button.BackColor = useAccent ? _theme.AccentColor : _theme.AccentSoftColor;
        button.ForeColor = _theme.TextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.Font = Font;
    }
}
