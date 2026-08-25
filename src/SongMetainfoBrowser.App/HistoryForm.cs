namespace SongMetainfoBrowser.App;

/// <summary>
/// Displays the History subfolder for a selected song and supports deleting saved history copies.
/// </summary>
internal sealed class HistoryForm : Form
{
    public bool FilesDeleted { get; private set; }

    private sealed class HistoryRow
    {
        public required string FileName { get; init; }
        public required string FullPath { get; init; }
        public required DateTime ModifiedDate { get; init; }
        public bool IsRevision => string.Equals(Path.GetExtension(FileName), ".song", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class LaunchTargetItem
    {
        public required SongHostApplication Application { get; init; }
        public required string DisplayName { get; init; }

        public override string ToString() => DisplayName;
    }

    private const string DeleteColumnName = "Delete";
    private const string FileNameColumnName = "FileName";
    private const string ModifiedDateColumnName = "ModifiedDate";
    private readonly SongMetadata _metadata;
    private readonly string _historyFolderPath;
    private readonly Label _pathLabel = new();
    private readonly DataGridView _historyGrid = new();
    private readonly Button _deleteButton = new();
    private readonly Button _cancelButton = new();
    private readonly Button _selectAllButton = new();
    private readonly Button _clearAllButton = new();
    private readonly CheckBox _keepNewestRevisionsCheckBox = new();
    private readonly Label _selectionSummaryLabel = new();
    private readonly CheckBox _enableRevisionLaunchCheckBox = new();
    private readonly ComboBox _revisionLaunchTargetComboBox = new();
    private readonly Button _loadRevisionButton = new();
    private readonly ToolTip _toolTip = new();
    private readonly AppTheme _theme;
    private readonly AppFontPreferences _fontPreferences;
    private readonly Action<IWin32Window, SongMetadata, SongHostApplication> _launchRevision;
    private bool _suspendGridWidthPersistence;
    private bool _suspendDeleteSelectionChanges;
    private readonly int _historyRevisionsToKeep;
    private const string HistoryGridKey = "HistoryGrid";

    public HistoryForm(
        SongMetadata metadata,
        AppTheme theme,
        Action<IWin32Window, SongMetadata, SongHostApplication> launchRevision)
    {
        _metadata = metadata;
        _theme = theme;
        _launchRevision = launchRevision;
        _fontPreferences = AppFontSettings.LoadPreferences();
        _historyRevisionsToKeep = BrowserConfigStore.LoadHistoryRevisionsToKeep();
        _historyFolderPath = string.Equals(
            Path.GetFileName(_metadata.Folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            "History",
            StringComparison.OrdinalIgnoreCase)
            ? _metadata.Folder
            : Path.Combine(_metadata.Folder, "History");

        Text = $"History - {_metadata.FileName}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(760, 500), _fontPreferences, AppFontSection.Dialogs);
        MinimumSize = AppFontSettings.Scale(new Size(640, 400), _fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.Dialogs);

        BuildLayout();
        ApplyTheme();
        LoadHistoryRows();
        Shown += (_, _) => BeginInvoke(() =>
        {
            ClearHistorySelection();
            _cancelButton.Select();
        });
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        _pathLabel.Dock = DockStyle.Fill;
        _pathLabel.AutoSize = false;
        _pathLabel.AutoEllipsis = true;
        _pathLabel.TextAlign = ContentAlignment.MiddleLeft;
        _pathLabel.Margin = new Padding(0, 0, 0, 10);
        _pathLabel.Height = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.Dialogs);
        _pathLabel.Text = $"Folder: {_historyFolderPath}";
        _toolTip.SetToolTip(_pathLabel, _historyFolderPath);
        layout.Controls.Add(_pathLabel, 0, 0);

        BuildSelectionControls(layout);
        ConfigureHistoryGrid();
        ApplySavedGridColumnWidths();
        layout.Controls.Add(_historyGrid, 0, 2);

        var footerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 0),
            BackColor = _theme.AppBackColor
        };
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        BuildRevisionLaunchControls(footerLayout);

        var buttonRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };

        _cancelButton.Text = "Cancel";
        _cancelButton.AutoSize = true;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Margin = new Padding(8, 0, 0, 0);
        _cancelButton.Click += (_, _) => Close();

        _deleteButton.Text = "Review Deletion...";
        _deleteButton.AutoSize = true;
        _deleteButton.Margin = Padding.Empty;
        _deleteButton.Click += (_, _) => DeleteHistoryFiles();

        StyleButton(_cancelButton, useAccent: false);
        StyleButton(_deleteButton, useAccent: true);

        buttonRow.Controls.Add(_cancelButton);
        buttonRow.Controls.Add(_deleteButton);
        footerLayout.Controls.Add(buttonRow, 1, 0);
        layout.Controls.Add(footerLayout, 0, 3);

        AcceptButton = _cancelButton;
        CancelButton = _cancelButton;
    }

    private void BuildRevisionLaunchControls(TableLayoutPanel footerLayout)
    {
        var launchLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };
        launchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, AppFontSettings.Scale(220, _fontPreferences, AppFontSection.Dialogs)));
        launchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        launchLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        launchLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _enableRevisionLaunchCheckBox.AutoSize = false;
        _enableRevisionLaunchCheckBox.Dock = DockStyle.Fill;
        _enableRevisionLaunchCheckBox.AutoEllipsis = true;
        _enableRevisionLaunchCheckBox.Text = "Select a history revision to load";
        _enableRevisionLaunchCheckBox.Enabled = false;
        _enableRevisionLaunchCheckBox.Margin = new Padding(0, 3, 8, 3);
        _enableRevisionLaunchCheckBox.CheckedChanged += (_, _) => UpdateRevisionLaunchEnabledState();
        launchLayout.Controls.Add(_enableRevisionLaunchCheckBox, 0, 0);
        launchLayout.SetColumnSpan(_enableRevisionLaunchCheckBox, 2);

        _revisionLaunchTargetComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _revisionLaunchTargetComboBox.Width = AppFontSettings.Scale(210, _fontPreferences, AppFontSection.Dialogs);
        _revisionLaunchTargetComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        _revisionLaunchTargetComboBox.Enabled = false;
        _revisionLaunchTargetComboBox.Margin = new Padding(0, 3, 0, 0);
        launchLayout.Controls.Add(_revisionLaunchTargetComboBox, 0, 1);

        _loadRevisionButton.Text = "Load this backup version";
        _loadRevisionButton.AutoSize = true;
        _loadRevisionButton.Enabled = false;
        _loadRevisionButton.Margin = new Padding(8, 3, 0, 0);
        _loadRevisionButton.Click += (_, _) => LoadSelectedRevision();
        StyleButton(_loadRevisionButton, useAccent: true);
        launchLayout.Controls.Add(_loadRevisionButton, 1, 1);

        footerLayout.Controls.Add(launchLayout, 0, 0);
    }

    private void BuildSelectionControls(TableLayoutPanel layout)
    {
        var selectionLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = _theme.AppBackColor
        };
        selectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        selectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(selectionLayout, 0, 1);

        var leftPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };
        _keepNewestRevisionsCheckBox.AutoSize = true;
        _keepNewestRevisionsCheckBox.Text = _historyRevisionsToKeep == 1
            ? "Keep most recent revision"
            : $"Keep {_historyRevisionsToKeep} most recent revisions";
        _keepNewestRevisionsCheckBox.Checked = BrowserConfigStore.LoadUseHistoryRetentionPreset();
        _keepNewestRevisionsCheckBox.Margin = new Padding(0, 4, 14, 0);
        _keepNewestRevisionsCheckBox.CheckedChanged += (_, _) => ApplyRetentionSelection();
        _selectionSummaryLabel.AutoSize = true;
        _selectionSummaryLabel.Margin = new Padding(0, 4, 0, 0);
        leftPanel.Controls.Add(_keepNewestRevisionsCheckBox);
        leftPanel.Controls.Add(_selectionSummaryLabel);
        selectionLayout.Controls.Add(leftPanel, 0, 0);

        var actionPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };
        _selectAllButton.Text = "Select All";
        _selectAllButton.AutoSize = true;
        _selectAllButton.Margin = Padding.Empty;
        _selectAllButton.Click += (_, _) => SetAllDeleteSelections(true);
        _clearAllButton.Text = "Clear All";
        _clearAllButton.AutoSize = true;
        _clearAllButton.Margin = new Padding(8, 0, 0, 0);
        _clearAllButton.Click += (_, _) => SetAllDeleteSelections(false);
        StyleButton(_selectAllButton, useAccent: false);
        StyleButton(_clearAllButton, useAccent: false);
        actionPanel.Controls.Add(_selectAllButton);
        actionPanel.Controls.Add(_clearAllButton);
        selectionLayout.Controls.Add(actionPanel, 1, 0);
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
        _historyGrid.ReadOnly = false;
        _historyGrid.RowHeadersVisible = false;
        _historyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _historyGrid.EditMode = DataGridViewEditMode.EditOnEnter;
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
        _historyGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = DeleteColumnName,
            HeaderText = "Delete",
            Width = 58,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            ReadOnly = false,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            FlatStyle = FlatStyle.Standard
        });
        _historyGrid.Columns.Add(FileNameColumnName, "Filename");
        _historyGrid.Columns.Add(ModifiedDateColumnName, "Modified Date");
        _historyGrid.Columns[FileNameColumnName]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _historyGrid.Columns[FileNameColumnName]!.FillWeight = 100;
        _historyGrid.Columns[FileNameColumnName]!.ReadOnly = true;
        _historyGrid.Columns[ModifiedDateColumnName]!.Width = 220;
        _historyGrid.Columns[ModifiedDateColumnName]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _historyGrid.Columns[ModifiedDateColumnName]!.ReadOnly = true;
        _historyGrid.Columns[FileNameColumnName]!.SortMode = DataGridViewColumnSortMode.NotSortable;
        _historyGrid.Columns[ModifiedDateColumnName]!.SortMode = DataGridViewColumnSortMode.NotSortable;
        _historyGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_historyGrid.IsCurrentCellDirty
                && _historyGrid.CurrentCell?.OwningColumn?.Name == DeleteColumnName)
            {
                _historyGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _historyGrid.CellValueChanged += (_, args) =>
        {
            if (!_suspendDeleteSelectionChanges
                && args.RowIndex >= 0
                && args.ColumnIndex == _historyGrid.Columns[DeleteColumnName]!.Index)
            {
                UpdateDeleteSelectionSummary();
            }
        };
        _historyGrid.SelectionChanged += (_, _) => UpdateRevisionLaunchControls();
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
                if (string.Equals(column.Name, FileNameColumnName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(column.Name, DeleteColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

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
            if (string.Equals(column.Name, FileNameColumnName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(column.Name, DeleteColumnName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

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
            _selectAllButton.Enabled = false;
            _clearAllButton.Enabled = false;
            _selectionSummaryLabel.Text = "No history files found.";
            return;
        }

        foreach (var filePath in Directory.EnumerateFiles(_historyFolderPath, "*", SearchOption.AllDirectories)
                     .OrderByDescending(File.GetLastWriteTime)
                     .ThenBy(Path.GetFileName))
        {
            var fileInfo = new FileInfo(filePath);
            var relativePath = Path.GetRelativePath(_historyFolderPath, filePath);
            var rowIndex = _historyGrid.Rows.Add(false, relativePath, DateTimeDisplay.Format(fileInfo.LastWriteTime));
            _historyGrid.Rows[rowIndex].Tag = new HistoryRow
            {
                FileName = relativePath,
                FullPath = fileInfo.FullName,
                ModifiedDate = fileInfo.LastWriteTime
            };
        }

        ClearHistorySelection();
        _selectAllButton.Enabled = _historyGrid.Rows.Count > 0;
        _clearAllButton.Enabled = _historyGrid.Rows.Count > 0;
        if (_historyGrid.Rows.Count == 0)
        {
            _deleteButton.Enabled = false;
            _selectionSummaryLabel.Text = "No history files found.";
            return;
        }

        ApplyRetentionSelection();
    }

    private void ClearHistorySelection()
    {
        _historyGrid.ClearSelection();
        _historyGrid.CurrentCell = null;
        UpdateRevisionLaunchControls();
    }

    private void UpdateRevisionLaunchControls()
    {
        _enableRevisionLaunchCheckBox.Checked = false;
        _revisionLaunchTargetComboBox.Items.Clear();

        if (_historyGrid.SelectedRows.Count != 1
            || _historyGrid.SelectedRows[0].Tag is not HistoryRow { IsRevision: true } historyRow
            || !File.Exists(historyRow.FullPath))
        {
            _enableRevisionLaunchCheckBox.Text = "Select a history revision to load";
            _enableRevisionLaunchCheckBox.Enabled = false;
            UpdateRevisionLaunchEnabledState();
            return;
        }

        _enableRevisionLaunchCheckBox.Text = $"Load {historyRow.FileName}";
        _toolTip.SetToolTip(_enableRevisionLaunchCheckBox, historyRow.FullPath);

        try
        {
            var revisionMetadata = SongMetadataReader.Read(historyRow.FullPath);
            foreach (var application in SongLaunchResolver.GetLaunchTargets(revisionMetadata)
                         .OrderByDescending(GetLaunchTargetSortOrder))
            {
                _revisionLaunchTargetComboBox.Items.Add(new LaunchTargetItem
                {
                    Application = application,
                    DisplayName = SongLaunchResolver.GetApplicationName(application)
                });
            }
        }
        catch
        {
            // A damaged or inaccessible revision cannot be offered for launch.
        }

        _enableRevisionLaunchCheckBox.Enabled = _revisionLaunchTargetComboBox.Items.Count > 0;
        if (_revisionLaunchTargetComboBox.Items.Count > 0)
        {
            _revisionLaunchTargetComboBox.SelectedIndex = 0;
        }

        UpdateRevisionLaunchEnabledState();
    }

    private void UpdateRevisionLaunchEnabledState()
    {
        var launchEnabled = _enableRevisionLaunchCheckBox.Enabled
            && _enableRevisionLaunchCheckBox.Checked
            && _revisionLaunchTargetComboBox.Items.Count > 0;
        _enableRevisionLaunchCheckBox.ForeColor = launchEnabled
            ? _theme.TextColor
            : _theme.MutedTextColor;
        _revisionLaunchTargetComboBox.Enabled = launchEnabled;
        _loadRevisionButton.Enabled = launchEnabled;
        _loadRevisionButton.BackColor = launchEnabled
            ? _theme.AccentSoftColor
            : _theme.PanelAltBackColor;
        _loadRevisionButton.ForeColor = launchEnabled
            ? _theme.SelectedTextColor
            : _theme.MutedTextColor;
    }

    private void LoadSelectedRevision()
    {
        if (!_enableRevisionLaunchCheckBox.Checked
            || _historyGrid.SelectedRows.Count != 1
            || _historyGrid.SelectedRows[0].Tag is not HistoryRow { IsRevision: true } historyRow
            || _revisionLaunchTargetComboBox.SelectedItem is not LaunchTargetItem launchTarget)
        {
            return;
        }

        try
        {
            var revisionMetadata = SongMetadataReader.Read(historyRow.FullPath);
            _launchRevision(this, revisionMetadata, launchTarget.Application);
        }
        catch (Exception ex)
        {
            using var messageDialog = new ThemedMessageForm(
                Text,
                $"Could not load the selected History revision.\n\n{ex.Message}",
                _theme,
                ThemedMessageKind.Error);
            messageDialog.ShowDialog(this);
        }
    }

    private static int GetLaunchTargetSortOrder(SongHostApplication application)
    {
        return application switch
        {
            SongHostApplication.FenderStudioPro8 => 2008,
            SongHostApplication.StudioOne7 => 1007,
            _ => 0
        };
    }

    private void DeleteHistoryFiles()
    {
        if (!Directory.Exists(_historyFolderPath))
        {
            Close();
            return;
        }

        _historyGrid.EndEdit();
        var selectedRows = _historyGrid.Rows
            .Cast<DataGridViewRow>()
            .Where(IsMarkedForDeletion)
            .Select(row => row.Tag as HistoryRow)
            .Where(row => row is not null)
            .Select(row => row!)
            .ToArray();
        if (selectedRows.Length == 0)
        {
            return;
        }

        var keptRows = _historyGrid.Rows
            .Cast<DataGridViewRow>()
            .Where(row => !IsMarkedForDeletion(row))
            .Select(row => row.Tag as HistoryRow)
            .Where(row => row is not null)
            .Select(row => (row!.FileName, row.ModifiedDate))
            .ToArray();
        using var confirmationDialog = new HistoryDeletionConfirmationForm(
            selectedRows.Length,
            keptRows,
            _theme);
        if (confirmationDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            DeleteSelectedHistoryFiles(_historyFolderPath, selectedRows.Select(row => row.FileName));
            FilesDeleted = true;
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

    private void ApplyRetentionSelection()
    {
        if (_suspendDeleteSelectionChanges)
        {
            return;
        }

        _suspendDeleteSelectionChanges = true;
        try
        {
            var revisionRows = _historyGrid.Rows
                .Cast<DataGridViewRow>()
                .Where(row => row.Tag is HistoryRow { IsRevision: true })
                .ToArray();
            foreach (var row in revisionRows)
            {
                row.Cells[DeleteColumnName].Value = true;
            }

            if (_keepNewestRevisionsCheckBox.Checked)
            {
                foreach (var row in revisionRows.Take(_historyRevisionsToKeep))
                {
                    row.Cells[DeleteColumnName].Value = false;
                }
            }
        }
        finally
        {
            _suspendDeleteSelectionChanges = false;
        }

        UpdateDeleteSelectionSummary();
    }

    private void SetAllDeleteSelections(bool isSelected)
    {
        _suspendDeleteSelectionChanges = true;
        try
        {
            _keepNewestRevisionsCheckBox.Checked = false;
            foreach (DataGridViewRow row in _historyGrid.Rows)
            {
                row.Cells[DeleteColumnName].Value = isSelected;
            }
        }
        finally
        {
            _suspendDeleteSelectionChanges = false;
        }

        UpdateDeleteSelectionSummary();
    }

    private void UpdateDeleteSelectionSummary()
    {
        var deleteCount = _historyGrid.Rows.Cast<DataGridViewRow>().Count(IsMarkedForDeletion);
        var keepCount = _historyGrid.Rows.Count - deleteCount;
        _selectionSummaryLabel.Text = $"{deleteCount} selected for deletion; {keepCount} kept";
        _deleteButton.Enabled = deleteCount > 0;
    }

    private static bool IsMarkedForDeletion(DataGridViewRow row)
    {
        return row.Cells[DeleteColumnName].Value is true;
    }

    private static void DeleteSelectedHistoryFiles(string historyFolderPath, IEnumerable<string> relativeFilePaths)
    {
        var rootPath = Path.GetFullPath(historyFolderPath);
        var rootPathWithSeparator = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        foreach (var relativeFilePath in relativeFilePaths)
        {
            var filePath = Path.GetFullPath(Path.Combine(rootPath, relativeFilePath));
            if (!filePath.StartsWith(rootPathWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("A selected History file was outside the History folder.");
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        var rootDirectory = new DirectoryInfo(rootPath);
        foreach (var directory in rootDirectory.EnumerateDirectories("*", SearchOption.AllDirectories)
                     .OrderByDescending(directory => directory.FullName.Length))
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
        _keepNewestRevisionsCheckBox.BackColor = _theme.AppBackColor;
        _keepNewestRevisionsCheckBox.ForeColor = _theme.TextColor;
        _selectionSummaryLabel.ForeColor = _theme.MutedTextColor;
        _enableRevisionLaunchCheckBox.BackColor = _theme.AppBackColor;
        _enableRevisionLaunchCheckBox.ForeColor = _theme.TextColor;
        _revisionLaunchTargetComboBox.BackColor = _theme.PanelBackColor;
        _revisionLaunchTargetComboBox.ForeColor = _theme.TextColor;
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
        StyleButton(_selectAllButton, useAccent: false);
        StyleButton(_clearAllButton, useAccent: false);
        StyleButton(_loadRevisionButton, useAccent: true);
    }

    private void StyleButton(Button button, bool useAccent)
    {
        var accentHover = BlendColor(_theme.AccentSoftColor, _theme.AccentColor, 0.35);
        var accentPressed = BlendColor(_theme.AccentSoftColor, _theme.AccentColor, 0.55);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = accentHover;
        button.FlatAppearance.MouseDownBackColor = accentPressed;
        button.BackColor = _theme.AccentSoftColor;
        button.ForeColor = _theme.SelectedTextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.Font = Font;
    }

    private static Color BlendColor(Color background, Color foreground, double amount)
    {
        amount = Math.Clamp(amount, 0.0, 1.0);
        var inverse = 1.0 - amount;
        return Color.FromArgb(
            (int)Math.Round(background.A * inverse + foreground.A * amount),
            (int)Math.Round(background.R * inverse + foreground.R * amount),
            (int)Math.Round(background.G * inverse + foreground.G * amount),
            (int)Math.Round(background.B * inverse + foreground.B * amount));
    }
}
