using System.Diagnostics;
using System.ComponentModel;

namespace SongMetainfoBrowser.App;

public sealed class MainForm : Form
{
    private sealed class SongGridRowData
    {
        public required SongMetadata Metadata { get; init; }
        public SearchResult? Match { get; init; }
    }

    private readonly TextBox _rootPathTextBox = new();
    private readonly Button _browseButton = new();
    private readonly Button _refreshButton = new();
    private readonly Button _expandAllButton = new();
    private readonly Button _collapseAllButton = new();
    private readonly TextBox _searchTextBox = new();
    private readonly Button _searchButton = new();
    private readonly MenuStrip _menuStrip = new();
    private readonly ToolStripMenuItem _viewMenuItem = new("View");
    private readonly ToolStripMenuItem _helpMenuItem = new("Help");
    private readonly ToolStripMenuItem _songAgeFilterMenuItem = new("Filter songs...");
    private readonly ToolStripMenuItem _themeMenuItem = new("Theme");
    private readonly ToolStripMenuItem _darkThemeMenuItem = new("Dark");
    private readonly ToolStripMenuItem _lightThemeMenuItem = new("Light");
    private readonly ToolStripMenuItem _helpContentsMenuItem = new("SongLens Help");
    private readonly ToolStripMenuItem _aboutMenuItem = new("About SongLens");
    private readonly TreeView _folderTree = new();
    private readonly ImageList _folderImages = new();
    private readonly DataGridView _songGrid = new();
    private readonly TabControl _detailTabs = new();
    private readonly TabPage _historyTab = new("History");
    private readonly DataGridView _summaryGrid = new();
    private readonly DataGridView _rawGrid = new();
    private readonly DataGridView _trackGrid = new();
    private readonly TextBox _notesTextBox = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly ToolTip _toolTip = new();

    private string _rootPath = "";
    private bool _searchMode;
    private SongAgeFilter? _songAgeFilter;
    private SongMetadata? _selectedMetadata;
    private int _lastNonHistoryTabIndex;
    private bool _suppressHistoryTabSelection;
    private bool _promptForRootOnFirstShow;
    private readonly Dictionary<string, bool> _folderVisibilityCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _customizedGridLayouts = new(StringComparer.OrdinalIgnoreCase);
    private AppTheme _theme;
    private bool _suspendGridWidthPersistence;

    private const string ClosedFolderImageKey = "folder-closed";
    private const string OpenFolderImageKey = "folder-open";
    private const string PlaceholderTag = "__placeholder__";
    private const string SongGridKey = "SongGrid";
    private const string SummaryGridKey = "SummaryGrid";
    private const string RawGridKey = "RawGrid";
    private const string TrackGridKey = "TrackGrid";

    public MainForm()
    {
        _theme = AppThemes.Resolve(BrowserConfigStore.LoadTheme());
        Text = "SongLens";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1050, 680);
        Size = new Size(1240, 780);
        Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

        BuildLayout();
        ApplyTheme(savePreference: false);
        WireEvents();

        var savedRoot = BrowserConfigStore.LoadRootPath();
        if (!string.IsNullOrWhiteSpace(savedRoot) && Directory.Exists(savedRoot))
        {
            SetRootPath(savedRoot);
        }
        else
        {
            _promptForRootOnFirstShow = true;
            SetStatus("Choose a songs folder to begin.");
        }
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        Controls.Add(layout);

        _menuStrip.Dock = DockStyle.Fill;
        _menuStrip.RenderMode = ToolStripRenderMode.Professional;
        _menuStrip.Font = Font;
        _viewMenuItem.DropDownItems.Add(_songAgeFilterMenuItem);
        _themeMenuItem.DropDownItems.Add(_darkThemeMenuItem);
        _themeMenuItem.DropDownItems.Add(_lightThemeMenuItem);
        _viewMenuItem.DropDownItems.Add(_themeMenuItem);
        _helpMenuItem.DropDownItems.Add(_helpContentsMenuItem);
        _helpMenuItem.DropDownItems.Add(new ToolStripSeparator());
        _helpMenuItem.DropDownItems.Add(_aboutMenuItem);
        _menuStrip.Items.Add(_viewMenuItem);
        _menuStrip.Items.Add(_helpMenuItem);
        MainMenuStrip = _menuStrip;
        layout.Controls.Add(_menuStrip, 0, 0);

        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            Padding = new Padding(6, 4, 6, 2)
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        layout.Controls.Add(toolbar, 0, 1);

        _rootPathTextBox.Dock = DockStyle.Fill;
        _rootPathTextBox.ReadOnly = true;
        _rootPathTextBox.BorderStyle = BorderStyle.FixedSingle;
        _rootPathTextBox.Margin = new Padding(0, 2, 6, 2);
        _browseButton.Text = "...";
        _browseButton.AccessibleName = "Browse";
        _toolTip.SetToolTip(_browseButton, "Browse");
        _browseButton.Dock = DockStyle.Fill;
        _browseButton.Margin = new Padding(0, 2, 6, 2);
        _searchTextBox.Dock = DockStyle.Fill;
        _searchTextBox.BorderStyle = BorderStyle.FixedSingle;
        _searchTextBox.Margin = new Padding(6, 2, 0, 2);
        _searchButton.Image = CreateSearchIcon();
        _searchButton.Text = "";
        _searchButton.AccessibleName = "Search";
        _searchButton.Dock = DockStyle.Fill;
        _searchButton.Margin = new Padding(0, 2, 0, 2);
        _toolTip.SetToolTip(_searchButton, "Search");
        _refreshButton.Text = "Refresh";
        _refreshButton.Dock = DockStyle.Fill;
        _refreshButton.Margin = new Padding(6, 2, 0, 2);
        _toolTip.SetToolTip(_refreshButton, "Refresh folders and songs");
        StyleButton(_browseButton, useAccent: false);
        StyleButton(_refreshButton, useAccent: true);
        StyleButton(_searchButton, useAccent: false);

        toolbar.Controls.Add(_browseButton, 0, 0);
        toolbar.Controls.Add(_rootPathTextBox, 1, 0);
        toolbar.Controls.Add(_searchButton, 2, 0);
        toolbar.Controls.Add(_searchTextBox, 3, 0);
        toolbar.Controls.Add(_refreshButton, 4, 0);

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Panel1MinSize = 80,
            Panel2MinSize = 120
        };
        layout.Controls.Add(mainSplit, 0, 2);

        var leftPanelLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = new Padding(0)
        };
        leftPanelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        leftPanelLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainSplit.Panel1.Controls.Add(leftPanelLayout);

        var treeToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = new Padding(6, 4, 6, 2)
        };

        _expandAllButton.Text = "Expand";
        _expandAllButton.AutoSize = true;
        _expandAllButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _expandAllButton.Margin = new Padding(0, 0, 6, 0);
        _expandAllButton.AccessibleName = "Expand All";
        _toolTip.SetToolTip(_expandAllButton, "Expand all folders");
        _collapseAllButton.Text = "Collapse";
        _collapseAllButton.AutoSize = true;
        _collapseAllButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _collapseAllButton.Margin = new Padding(0);
        _collapseAllButton.AccessibleName = "Collapse All";
        _toolTip.SetToolTip(_collapseAllButton, "Collapse all folders");
        StyleButton(_expandAllButton, useAccent: false);
        StyleButton(_collapseAllButton, useAccent: false);
        treeToolbar.Controls.Add(_expandAllButton);
        treeToolbar.Controls.Add(_collapseAllButton);
        leftPanelLayout.Controls.Add(treeToolbar, 0, 0);

        _folderTree.Dock = DockStyle.Fill;
        _folderTree.HideSelection = false;
        _folderTree.BorderStyle = BorderStyle.None;
        _folderTree.DrawMode = TreeViewDrawMode.OwnerDrawText;
        _folderTree.FullRowSelect = true;
        _folderTree.Indent = 22;
        _folderTree.ItemHeight = 22;
        _folderTree.ShowLines = false;
        _folderTree.ShowPlusMinus = false;
        _folderTree.ShowRootLines = false;
        _folderTree.ImageList = _folderImages;
        _folderTree.ImageKey = ClosedFolderImageKey;
        _folderTree.SelectedImageKey = OpenFolderImageKey;
        ConfigureFolderImages();
        leftPanelLayout.Controls.Add(_folderTree, 0, 1);

        var rightSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            Panel1MinSize = 80,
            Panel2MinSize = 120
        };
        mainSplit.Panel2.Controls.Add(rightSplit);
        Shown += (_, _) =>
        {
            if (mainSplit.Width > 420)
            {
                mainSplit.SplitterDistance = 320;
            }

            if (rightSplit.Height > 420)
            {
                rightSplit.SplitterDistance = 300;
            }

            if (_promptForRootOnFirstShow)
            {
                _promptForRootOnFirstShow = false;
                BeginInvoke(ChooseRootFolder);
            }
        };

        ConfigureSongGrid();
        rightSplit.Panel1.Controls.Add(_songGrid);

        _detailTabs.Dock = DockStyle.Fill;
        _detailTabs.Appearance = TabAppearance.Normal;
        _detailTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _detailTabs.Padding = new Point(12, 4);
        _detailTabs.SizeMode = TabSizeMode.Normal;
        _detailTabs.ItemSize = new Size(110, 24);
        _detailTabs.DrawItem += DrawDetailTab;
        var mainFactsTab = new TabPage("Summary");
        var rawTab = new TabPage("Attributes");
        var notesTab = new TabPage("Notes");
        var tracksTab = new TabPage("Tracks");

        _detailTabs.TabPages.Add(mainFactsTab);
        _detailTabs.TabPages.Add(rawTab);
        _detailTabs.TabPages.Add(tracksTab);
        _detailTabs.TabPages.Add(notesTab);
        _detailTabs.TabPages.Add(_historyTab);
        _detailTabs.SelectedIndex = 0;
        rightSplit.Panel2.Controls.Add(_detailTabs);

        ConfigureDetailGrid(_summaryGrid, ("Field", 180), ("Value", 720));
        ConfigureDetailGrid(_rawGrid, ("Id", 220), ("Value", 720));
        ConfigureDetailGrid(_trackGrid, ("#", 50), ("Track", 260), ("Instrument", 240), ("Track Note", 560));
        ConfigureTrackGridLayout();
        ApplySavedGridColumnWidths(_songGrid, SongGridKey);
        ApplySavedGridColumnWidths(_summaryGrid, SummaryGridKey);
        ApplySavedGridColumnWidths(_rawGrid, RawGridKey);
        ApplySavedGridColumnWidths(_trackGrid, TrackGridKey);
        ConfigureNotesTextBox();
        mainFactsTab.Controls.Add(_summaryGrid);
        rawTab.Controls.Add(_rawGrid);
        tracksTab.Controls.Add(_trackGrid);
        notesTab.Controls.Add(_notesTextBox);

        var statusStrip = new StatusStrip
        {
            Dock = DockStyle.Fill,
            SizingGrip = false
        };
        _statusLabel.Text = "Ready";
        statusStrip.Items.Add(_statusLabel);
        layout.Controls.Add(statusStrip, 0, 3);
    }

    private void ConfigureSongGrid()
    {
        _songGrid.Dock = DockStyle.Fill;
        _songGrid.AllowUserToAddRows = false;
        _songGrid.AllowUserToDeleteRows = false;
        _songGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _songGrid.BackgroundColor = _theme.PanelBackColor;
        _songGrid.BorderStyle = BorderStyle.None;
        _songGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _songGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _songGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _songGrid.ColumnHeadersHeight = 24;
        _songGrid.EnableHeadersVisualStyles = false;
        _songGrid.GridColor = _theme.BorderColor;
        _songGrid.MultiSelect = false;
        _songGrid.ReadOnly = true;
        _songGrid.RowHeadersVisible = false;
        _songGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _songGrid.ColumnHeadersDefaultCellStyle.BackColor = _theme.HeaderBackColor;
        _songGrid.ColumnHeadersDefaultCellStyle.ForeColor = _theme.TextColor;
        _songGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _theme.HeaderBackColor;
        _songGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _theme.TextColor;
        _songGrid.DefaultCellStyle.BackColor = _theme.PanelBackColor;
        _songGrid.DefaultCellStyle.ForeColor = _theme.TextColor;
        _songGrid.DefaultCellStyle.SelectionBackColor = _theme.TreeSelectionColor;
        _songGrid.DefaultCellStyle.SelectionForeColor = _theme.SelectedTextColor;
        _songGrid.AlternatingRowsDefaultCellStyle.BackColor = _theme.PanelAltBackColor;
        _songGrid.RowTemplate.Height = 22;
        AddSongColumn("Song", "Song", 240);
        AddSongColumn("Title", "Title", 190);
        AddSongColumn("Artist", "Artist", 150);
        AddSongColumn("Key", "Key", 90);
        AddSongColumn("Tempo", "Tempo", 90);
        AddSongColumn("Match", "Match", 310);
    }

    private void AddSongColumn(string name, string header, int width)
    {
        var index = _songGrid.Columns.Add(name, header);
        _songGrid.Columns[index].Width = width;
    }

    private void ConfigureDetailGrid(DataGridView grid, params (string Header, int Width)[] columns)
    {
        grid.Dock = DockStyle.Fill;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        grid.BackgroundColor = _theme.PanelBackColor;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 24;
        grid.EnableHeadersVisualStyles = false;
        grid.GridColor = _theme.BorderColor;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.ColumnHeadersDefaultCellStyle.BackColor = _theme.HeaderBackColor;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = _theme.TextColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _theme.HeaderBackColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _theme.TextColor;
        grid.DefaultCellStyle.BackColor = _theme.PanelBackColor;
        grid.DefaultCellStyle.ForeColor = _theme.TextColor;
        grid.DefaultCellStyle.SelectionBackColor = _theme.TreeSelectionColor;
        grid.DefaultCellStyle.SelectionForeColor = _theme.SelectedTextColor;
        grid.AlternatingRowsDefaultCellStyle.BackColor = _theme.PanelAltBackColor;

        foreach (var column in columns)
        {
            var index = grid.Columns.Add(column.Header, column.Header);
            grid.Columns[index].Width = column.Width;
            grid.Columns[index].SortMode = DataGridViewColumnSortMode.NotSortable;
        }
    }

    private void ConfigureTrackGridLayout()
    {
        _trackGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _trackGrid.RowTemplate.Height = 22;

        _trackGrid.Columns["#"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _trackGrid.Columns["#"]!.Width = 50;

        _trackGrid.Columns["Track"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _trackGrid.Columns["Track"]!.Width = 260;

        _trackGrid.Columns["Instrument"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _trackGrid.Columns["Instrument"]!.Width = 240;

        _trackGrid.Columns["Track Note"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _trackGrid.Columns["Track Note"]!.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
    }

    private Bitmap CreateSearchIcon()
    {
        var bitmap = new Bitmap(18, 18);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var pen = new Pen(_theme.SearchIconColor, 2);
        graphics.DrawEllipse(pen, 3, 3, 9, 9);
        graphics.DrawLine(pen, 10, 10, 15, 15);

        return bitmap;
    }

    private void ConfigureFolderImages()
    {
        _folderImages.Images.Clear();
        _folderImages.ColorDepth = ColorDepth.Depth32Bit;
        _folderImages.ImageSize = new Size(18, 18);
        _folderImages.Images.Add(ClosedFolderImageKey, CreateFolderIcon(isOpen: false));
        _folderImages.Images.Add(OpenFolderImageKey, CreateFolderIcon(isOpen: true));
    }

    private Bitmap CreateFolderIcon(bool isOpen)
    {
        var bitmap = new Bitmap(18, 18);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var fill = isOpen ? _theme.FolderOpenFillColor : _theme.FolderFillColor;
        var tabFill = isOpen ? _theme.FolderOpenTabColor : _theme.FolderTabColor;
        using var outlinePen = new Pen(_theme.FolderOutlineColor, 1f);
        using var bodyBrush = new SolidBrush(fill);
        using var tabBrush = new SolidBrush(tabFill);

        graphics.FillRectangle(tabBrush, 3, 4, 6, 3);
        graphics.DrawRectangle(outlinePen, 3, 4, 6, 3);

        if (isOpen)
        {
            var points = new[]
            {
                new PointF(2.5f, 7.5f),
                new PointF(15.0f, 7.5f),
                new PointF(13.5f, 14.0f),
                new PointF(4.0f, 14.0f)
            };
            graphics.FillPolygon(bodyBrush, points);
            graphics.DrawPolygon(outlinePen, points);
        }
        else
        {
            graphics.FillRectangle(bodyBrush, 2, 6, 13, 8);
            graphics.DrawRectangle(outlinePen, 2, 6, 13, 8);
        }

        return bitmap;
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
        button.Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
    }

    private void DrawDetailTab(object? sender, DrawItemEventArgs args)
    {
        if (args.Index < 0 || args.Index >= _detailTabs.TabPages.Count)
        {
            return;
        }

        var bounds = args.Bounds;
        var isSelected = (args.State & DrawItemState.Selected) == DrawItemState.Selected;
        using var backBrush = new SolidBrush(isSelected ? _theme.PanelBackColor : _theme.AppBackColor);
        using var textBrush = new SolidBrush(isSelected ? _theme.TextColor : _theme.MutedTextColor);
        using var borderPen = new Pen(_theme.BorderColor);

        args.Graphics.FillRectangle(backBrush, bounds);
        args.Graphics.DrawLine(borderPen, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
        if (isSelected)
        {
            using var accentPen = new Pen(_theme.AccentColor, 2);
            args.Graphics.DrawLine(accentPen, bounds.Left + 4, bounds.Bottom - 1, bounds.Right - 4, bounds.Bottom - 1);
        }
        if (!isSelected)
        {
            args.Graphics.DrawLine(borderPen, bounds.Right - 1, bounds.Top + 5, bounds.Right - 1, bounds.Bottom - 3);
        }
        TextRenderer.DrawText(
            args.Graphics,
            _detailTabs.TabPages[args.Index].Text,
            Font,
            bounds,
            textBrush.Color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private void ApplyTheme(bool savePreference)
    {
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        _menuStrip.BackColor = _theme.AppBackColor;
        _menuStrip.ForeColor = _theme.TextColor;
        _menuStrip.Renderer = new ThemeMenuRenderer(_theme);
        _viewMenuItem.ForeColor = _theme.TextColor;
        _helpMenuItem.ForeColor = _theme.TextColor;
        _songAgeFilterMenuItem.ForeColor = _theme.TextColor;
        _themeMenuItem.ForeColor = _theme.TextColor;
        _darkThemeMenuItem.ForeColor = _theme.TextColor;
        _lightThemeMenuItem.ForeColor = _theme.TextColor;
        _helpContentsMenuItem.ForeColor = _theme.TextColor;
        _aboutMenuItem.ForeColor = _theme.TextColor;
        _viewMenuItem.BackColor = _theme.AppBackColor;
        _helpMenuItem.BackColor = _theme.AppBackColor;
        _songAgeFilterMenuItem.BackColor = _theme.PanelBackColor;
        _themeMenuItem.BackColor = _theme.PanelBackColor;
        _darkThemeMenuItem.BackColor = _theme.PanelBackColor;
        _lightThemeMenuItem.BackColor = _theme.PanelBackColor;
        _helpContentsMenuItem.BackColor = _theme.PanelBackColor;
        _aboutMenuItem.BackColor = _theme.PanelBackColor;
        _darkThemeMenuItem.Checked = string.Equals(_theme.Name, AppThemes.Dark.Name, StringComparison.OrdinalIgnoreCase);
        _lightThemeMenuItem.Checked = string.Equals(_theme.Name, AppThemes.Light.Name, StringComparison.OrdinalIgnoreCase);

        _rootPathTextBox.BackColor = _theme.PanelBackColor;
        _rootPathTextBox.ForeColor = _theme.TextColor;
        _searchTextBox.BackColor = _theme.PanelBackColor;
        _searchTextBox.ForeColor = _theme.TextColor;
        _searchButton.Image = CreateSearchIcon();
        StyleButton(_browseButton, useAccent: false);
        StyleButton(_refreshButton, useAccent: true);
        StyleButton(_expandAllButton, useAccent: false);
        StyleButton(_collapseAllButton, useAccent: false);
        StyleButton(_searchButton, useAccent: false);

        ApplyThemeToChildContainers(this);

        _folderTree.BackColor = _theme.PanelBackColor;
        _folderTree.ForeColor = _theme.TextColor;
        _folderTree.LineColor = _theme.BorderColor;
        ConfigureFolderImages();
        _folderTree.Invalidate();

        _detailTabs.BackColor = _theme.SurfaceBackColor;
        foreach (TabPage tabPage in _detailTabs.TabPages)
        {
            tabPage.BackColor = _theme.SurfaceBackColor;
            tabPage.ForeColor = _theme.TextColor;
        }
        _detailTabs.Invalidate();

        ApplyGridTheme(_songGrid);
        ApplyGridTheme(_summaryGrid);
        ApplyGridTheme(_rawGrid);
        ApplyGridTheme(_trackGrid);

        _notesTextBox.BackColor = _theme.PanelBackColor;
        _notesTextBox.ForeColor = _theme.TextColor;

        var statusStrip = Controls.OfType<TableLayoutPanel>()
            .SelectMany(table => table.Controls.OfType<StatusStrip>())
            .FirstOrDefault();
        if (statusStrip is not null)
        {
            statusStrip.BackColor = _theme.StatusBackColor;
            statusStrip.ForeColor = _theme.TextColor;
        }

        _statusLabel.ForeColor = _theme.TextColor;

        if (savePreference)
        {
            BrowserConfigStore.SaveTheme(_theme.Name);
        }
    }

    private void ApplySplitContainerTheme(SplitContainer splitContainer)
    {
        splitContainer.BackColor = _theme.BorderColor;
        splitContainer.Panel1.BackColor = _theme.PanelBackColor;
        splitContainer.Panel2.BackColor = _theme.PanelBackColor;
    }

    private void ApplyThemeToChildContainers(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            switch (control)
            {
                case TableLayoutPanel or FlowLayoutPanel:
                    control.BackColor = _theme.AppBackColor;
                    control.ForeColor = _theme.TextColor;
                    break;
                case Panel:
                    control.BackColor = _theme.PanelBackColor;
                    control.ForeColor = _theme.TextColor;
                    break;
            }

            if (control is SplitContainer splitContainer)
            {
                ApplySplitContainerTheme(splitContainer);
                ApplyThemeToChildContainers(splitContainer.Panel1);
                ApplyThemeToChildContainers(splitContainer.Panel2);
                continue;
            }

            ApplyThemeToChildContainers(control);
        }
    }

    private void ApplyGridTheme(DataGridView grid)
    {
        grid.BackgroundColor = _theme.PanelBackColor;
        grid.GridColor = _theme.BorderColor;
        grid.ColumnHeadersDefaultCellStyle.BackColor = _theme.HeaderBackColor;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = _theme.TextColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _theme.HeaderBackColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _theme.TextColor;
        grid.DefaultCellStyle.BackColor = _theme.PanelBackColor;
        grid.DefaultCellStyle.ForeColor = _theme.TextColor;
        grid.DefaultCellStyle.SelectionBackColor = _theme.TreeSelectionColor;
        grid.DefaultCellStyle.SelectionForeColor = _theme.SelectedTextColor;
        grid.AlternatingRowsDefaultCellStyle.BackColor = _theme.PanelAltBackColor;
        grid.Invalidate();
    }

    private void ChangeTheme(AppTheme theme)
    {
        _theme = theme;
        ApplyTheme(savePreference: true);
    }

    private void ApplySavedGridColumnWidths(DataGridView grid, string gridKey)
    {
        var savedWidths = BrowserConfigStore.LoadGridColumnWidths(gridKey);
        if (savedWidths.Count == 0)
        {
            return;
        }

        _suspendGridWidthPersistence = true;
        try
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (!savedWidths.TryGetValue(column.Name, out var width) || width <= 0)
                {
                    continue;
                }

                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                column.Width = width;
            }
        }
        finally
        {
            _suspendGridWidthPersistence = false;
        }

        _customizedGridLayouts.Add(gridKey);
    }

    private void PersistGridColumnWidths(DataGridView grid, string gridKey)
    {
        if (_suspendGridWidthPersistence)
        {
            return;
        }

        var widths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewColumn column in grid.Columns)
        {
            widths[column.Name] = column.Width;
        }

        BrowserConfigStore.SaveGridColumnWidths(gridKey, widths);
        _customizedGridLayouts.Add(gridKey);
    }

    private void DrawFolderNode(object? sender, DrawTreeNodeEventArgs args)
    {
        if (args.Node is null)
        {
            return;
        }

        var isSelected = (args.State & TreeNodeStates.Selected) == TreeNodeStates.Selected;
        var backColor = isSelected ? _theme.TreeSelectionColor : _theme.PanelBackColor;
        var foreColor = isSelected ? _theme.SelectedTextColor : _theme.TextColor;
        using var backBrush = new SolidBrush(backColor);

        var bounds = new Rectangle(0, args.Bounds.Top, _folderTree.ClientSize.Width, args.Bounds.Height);
        args.Graphics.FillRectangle(backBrush, bounds);

        var imageKey = args.Node.IsExpanded ? OpenFolderImageKey : ClosedFolderImageKey;
        var image = _folderImages.Images[imageKey];
        var imageY = args.Bounds.Top + Math.Max(0, (args.Bounds.Height - _folderImages.ImageSize.Height) / 2);
        var imageX = args.Bounds.Left - _folderImages.ImageSize.Width - 3;
        if (image is not null && imageX >= 0)
        {
            args.Graphics.DrawImage(image, imageX, imageY, _folderImages.ImageSize.Width, _folderImages.ImageSize.Height);
        }

        TextRenderer.DrawText(
            args.Graphics,
            args.Node.Text,
            _folderTree.Font,
            args.Bounds,
            foreColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }

    private static void SetNodeImage(TreeNode node, bool isExpanded)
    {
        node.ImageKey = isExpanded ? OpenFolderImageKey : ClosedFolderImageKey;
        node.SelectedImageKey = OpenFolderImageKey;
    }

    private static bool HasExpandableChildren(TreeNode node)
    {
        if (node.Nodes.Count == 0)
        {
            return false;
        }

        return node.Nodes.Count > 1 || !Equals(node.Nodes[0].Tag, PlaceholderTag);
    }

    private void WireEvents()
    {
        _browseButton.Click += (_, _) => ChooseRootFolder();
        _refreshButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_rootPath))
            {
                _songAgeFilter = null;
                SetRootPath(_rootPath);
            }
        };
        _expandAllButton.Click += (_, _) => ExpandAllFolders();
        _collapseAllButton.Click += (_, _) => CollapseAllFolders();
        _songAgeFilterMenuItem.Click += (_, _) => ShowSongAgeFilterDialog();
        _darkThemeMenuItem.Click += (_, _) => ChangeTheme(AppThemes.Dark);
        _lightThemeMenuItem.Click += (_, _) => ChangeTheme(AppThemes.Light);
        _helpContentsMenuItem.Click += (_, _) => ShowHelpDialog();
        _aboutMenuItem.Click += (_, _) => ShowAboutDialog();
        _songGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_songGrid, SongGridKey);
        _summaryGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_summaryGrid, SummaryGridKey);
        _rawGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_rawGrid, RawGridKey);
        _trackGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_trackGrid, TrackGridKey);
        _detailTabs.SelectedIndexChanged += (_, _) => HandleDetailTabSelectionChanged();
        _searchButton.Click += (_, _) => SearchSongs();
        _searchTextBox.KeyDown += (_, args) =>
        {
            if (args.KeyCode != Keys.Enter)
            {
                return;
            }

            SearchSongs();
            args.SuppressKeyPress = true;
        };
        _folderTree.BeforeExpand += (_, args) =>
        {
            if (args.Node is not null)
            {
                SetNodeImage(args.Node, isExpanded: true);
                PopulateFolderChildren(args.Node);
            }
        };
        _folderTree.DrawNode += DrawFolderNode;
        _folderTree.AfterCollapse += (_, args) =>
        {
            if (args.Node is not null)
            {
                SetNodeImage(args.Node, isExpanded: false);
            }
        };
        _folderTree.AfterSelect += (_, args) =>
        {
            if (_searchMode)
            {
                return;
            }
            if (args.Node?.Tag is string folderPath)
            {
                _searchTextBox.Clear();
                LoadSongsForFolder(folderPath);
            }
        };
        _folderTree.NodeMouseClick += (_, args) =>
        {
            if (args.Button != MouseButtons.Left || args.Node is null)
            {
                return;
            }

            if (_folderTree.SelectedNode != args.Node || !HasExpandableChildren(args.Node))
            {
                return;
            }

            if (args.Node.IsExpanded)
            {
                args.Node.Collapse();
            }
            else
            {
                args.Node.Expand();
            }
        };
        _songGrid.SelectionChanged += (_, _) =>
        {
            if (_songGrid.SelectedRows.Count == 0)
            {
                return;
            }

            if (_songGrid.SelectedRows[0].Tag is SongGridRowData rowData)
            {
                ShowMetadataDetails(rowData.Metadata, rowData.Match);
            }
        };
        _songGrid.CellDoubleClick += (_, args) =>
        {
            if (args.RowIndex < 0 || args.RowIndex >= _songGrid.Rows.Count)
            {
                return;
            }

            if (_songGrid.Rows[args.RowIndex].Tag is SongGridRowData rowData)
            {
                OpenSongInExplorer(rowData.Metadata);
            }
        };
    }

    private void ChooseRootFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select your Studio One songs folder",
            SelectedPath = Directory.Exists(_rootPath) ? _rootPath : ""
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            SetRootPath(dialog.SelectedPath);
        }
    }

    private void SetRootPath(string path)
    {
        if (!Directory.Exists(path))
        {
            MessageBox.Show(this, $"Folder not found:\n{path}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _rootPath = Path.GetFullPath(path);
        BrowserConfigStore.SaveRootPath(_rootPath);
        _rootPathTextBox.Text = _rootPath;
        _searchMode = false;
        _folderVisibilityCache.Clear();
        _folderTree.Nodes.Clear();
        _songGrid.Rows.Clear();
        _summaryGrid.Rows.Clear();
        _rawGrid.Rows.Clear();
        _trackGrid.Rows.Clear();
        _notesTextBox.Clear();
        _selectedMetadata = null;

        var rootDirectory = new DirectoryInfo(_rootPath);
        var rootNode = new TreeNode(rootDirectory.Name) { Tag = rootDirectory.FullName };
        SetNodeImage(rootNode, isExpanded: false);
        _folderTree.Nodes.Add(rootNode);
        if (HasVisibleChildDirectories(rootDirectory.FullName))
        {
            AddPlaceholderChild(rootNode);
        }
        rootNode.Expand();
        _folderTree.SelectedNode = rootNode;
        SetStatus($"Loaded {_rootPath}");
    }

    private void AddPlaceholderChild(TreeNode node)
    {
        node.Nodes.Clear();
        node.Nodes.Add(new TreeNode("Loading") { Tag = PlaceholderTag });
    }

    private void ExpandAllFolders()
    {
        if (_folderTree.Nodes.Count == 0)
        {
            return;
        }

        Cursor = Cursors.WaitCursor;
        try
        {
            foreach (TreeNode node in _folderTree.Nodes)
            {
                ExpandNodeAndChildren(node);
            }
        }
        finally
        {
            Cursor = Cursors.Default;
        }

        SetStatus("Expanded all folders.");
    }

    private void ExpandNodeAndChildren(TreeNode node)
    {
        PopulateFolderChildren(node);
        node.Expand();

        foreach (TreeNode child in node.Nodes)
        {
            ExpandNodeAndChildren(child);
        }
    }

    private void CollapseAllFolders()
    {
        if (_folderTree.Nodes.Count == 0)
        {
            return;
        }

        foreach (TreeNode node in _folderTree.Nodes)
        {
            CollapseNodeChildren(node);
        }

        if (_folderTree.Nodes[0] is TreeNode rootNode)
        {
            rootNode.Expand();
            SetNodeImage(rootNode, isExpanded: true);
            _folderTree.SelectedNode = rootNode;
        }

        SetStatus("Collapsed all folders.");
    }

    private void CollapseNodeChildren(TreeNode node)
    {
        foreach (TreeNode child in node.Nodes)
        {
            CollapseNodeChildren(child);
            child.Collapse(false);
            SetNodeImage(child, isExpanded: false);
        }
    }

    private void PopulateFolderChildren(TreeNode node)
    {
        if (node.Tag is not string path || path == PlaceholderTag)
        {
            return;
        }

        node.Nodes.Clear();
        try
        {
            foreach (var directory in Directory.EnumerateDirectories(path).OrderBy(Path.GetFileName))
            {
                var info = new DirectoryInfo(directory);
                if (!DirectoryContainsVisibleSongs(info.FullName))
                {
                    continue;
                }

                var child = new TreeNode(info.Name) { Tag = info.FullName };
                SetNodeImage(child, isExpanded: false);
                node.Nodes.Add(child);

                if (HasVisibleChildDirectories(info.FullName))
                {
                    AddPlaceholderChild(child);
                }
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Could not load folders: {ex.Message}");
        }
    }

    private void LoadSongsForFolder(string folderPath)
    {
        if (_searchMode)
        {
            return;
        }

        _songGrid.Rows.Clear();
        _summaryGrid.Rows.Clear();
        _rawGrid.Rows.Clear();
        _trackGrid.Rows.Clear();
        _notesTextBox.Clear();
        _selectedMetadata = null;
        SetStatus("Loading songs...");

        var loaded = 0;
        try
        {
            foreach (var songPath in Directory.EnumerateFiles(folderPath, "*.song")
                         .Where(path => SongMetadataReader.IsRegularSongFile(Path.GetFileName(path)))
                         .Where(SongMatchesCurrentView)
                         .OrderBy(Path.GetFileName))
            {
                try
                {
                    AddSongRow(SongMetadataReader.Read(songPath), null);
                    loaded++;
                }
                catch
                {
                    // Ignore song files that do not contain readable metainfo.xml.
                }
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Could not load songs: {ex.Message}");
            return;
        }

        if (_songGrid.Rows.Count > 0)
        {
            SelectSongRow(0);
        }

        SetStatus($"Loaded {loaded} song(s) from {folderPath}");
    }

    private void SearchSongs()
    {
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            return;
        }

        var query = _searchTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            _searchMode = false;
            if (_folderTree.SelectedNode?.Tag is string folderPath)
            {
                LoadSongsForFolder(folderPath);
            }
            return;
        }

        _searchMode = true;
        _songGrid.Rows.Clear();
        _summaryGrid.Rows.Clear();
        _rawGrid.Rows.Clear();
        _trackGrid.Rows.Clear();
        _notesTextBox.Clear();
        _selectedMetadata = null;
        SetStatus("Searching...");

        var count = 0;
        foreach (var songPath in Directory.EnumerateFiles(_rootPath, "*.song", SearchOption.AllDirectories)
                     .Where(path => SongMetadataReader.IsRegularSongFile(Path.GetFileName(path)))
                     .Where(SongMatchesCurrentView)
                     .OrderBy(path => path))
        {
            try
            {
                var metadata = SongMetadataReader.Read(songPath);
                var match = SongMetadataReader.GetSearchMatch(metadata, query);
                if (match is null)
                {
                    continue;
                }

                AddSongRow(metadata, match);
                count++;
            }
            catch
            {
                // Ignore unreadable song files.
            }
        }

        if (_songGrid.Rows.Count > 0)
        {
            _songGrid.ClearSelection();
            if (_songGrid.Rows[0].Tag is SongGridRowData rowData)
            {
                ShowMetadataDetails(rowData.Metadata, rowData.Match);
            }
        }

        SetStatus($"Found {count} song(s) matching '{query}'");
    }

    private void AddSongRow(SongMetadata metadata, SearchResult? match)
    {
        var rowIndex = _songGrid.Rows.Add();
        var row = _songGrid.Rows[rowIndex];
        row.Cells["Song"].Value = metadata.FileName;
        row.Cells["Title"].Value = FormatField(metadata.Title);
        row.Cells["Artist"].Value = FormatField(metadata.Artist);
        row.Cells["Key"].Value = FormatField(metadata.KeySignature);
        row.Cells["Tempo"].Value = FormatField(metadata.Tempo);
        row.Cells["Match"].Value = match is null ? "" : $"{match.MatchField}: {match.MatchValue}";
        row.Tag = new SongGridRowData
        {
            Metadata = metadata,
            Match = match
        };
    }

    private void SelectSongRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _songGrid.Rows.Count)
        {
            return;
        }

        _songGrid.ClearSelection();
        var row = _songGrid.Rows[rowIndex];
        row.Selected = true;
        if (row.Tag is SongGridRowData rowData)
        {
            _detailTabs.SelectedIndex = 0;
            ShowMetadataDetails(rowData.Metadata, rowData.Match);
        }
    }

    private void ShowMetadataDetails(SongMetadata metadata, SearchResult? match = null)
    {
        _selectedMetadata = metadata;
        _summaryGrid.Rows.Clear();
        _rawGrid.Rows.Clear();
        _trackGrid.Rows.Clear();
        _notesTextBox.Clear();

        AddSummary("Title", metadata.Title);
        AddSummary("Artist", metadata.Artist);
        AddSummary("Year", metadata.Year);
        AddSummary("Date Created", metadata.DateCreated);
        AddSummary("Last Modified", metadata.LastModified);
        AddSummary("Tempo", metadata.Tempo);
        AddSummary("Key Signature", metadata.KeySignature);
        AddSummary("Time Signature", metadata.TimeSignature);
        AddSummary("Length", metadata.Length);
        AddSummary("Track Count", metadata.TrackCount);
        AddSummary("Sample Rate", metadata.SampleRate);
        AddSummary("Bit Depth", metadata.BitDepth);
        AddSummary("Studio One Version", metadata.Generator);
        AddSummary("Format Version", metadata.FormatVersion);
        AddSummary("Notes File", metadata.NotesFile);
        AddSummary("Artwork File", metadata.ArtworkFile);
        AddSummary("Comment", metadata.Comment);
        AddSummary("Path", metadata.Path);

        foreach (var attribute in metadata.Attributes.OrderBy(pair => pair.Key))
        {
            _rawGrid.Rows.Add(attribute.Key, attribute.Value);
        }

        for (var i = 0; i < metadata.TrackInstruments.Count; i++)
        {
            var track = metadata.TrackInstruments[i];
            _trackGrid.Rows.Add((i + 1).ToString(), track.TrackName, FormatField(track.InstrumentName), FormatField(track.TrackNote));
        }

        _notesTextBox.Text = string.IsNullOrWhiteSpace(metadata.NotesText)
            ? "No notes.txt content."
            : metadata.NotesText;

        _detailTabs.SelectedIndex = match?.MatchField == "Notes" ? 3 : 0;
        _lastNonHistoryTabIndex = _detailTabs.SelectedIndex;
        AutoSizeDetailColumns();
        SetStatus($"Selected {metadata.FileName}");
    }

    private void OpenSongInExplorer(SongMetadata metadata)
    {
        if (!File.Exists(metadata.Path))
        {
            SetStatus($"File not found: {metadata.Path}");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{metadata.Path}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            SetStatus($"Could not open Explorer: {ex.Message}");
        }
    }

    private void ShowSongAgeFilterDialog()
    {
        using var dialog = new SongAgeFilterForm(_songAgeFilter);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedFilter is null)
        {
            return;
        }

        _songAgeFilter = dialog.SelectedFilter;
        var statusMessage = $"Viewing songs {_songAgeFilter.OperatorText} {_songAgeFilter.Days} days.";
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            SetStatus(statusMessage);
            return;
        }

        SetRootPath(_rootPath);
        SetStatus(statusMessage);
    }

    private void ShowAboutDialog()
    {
        using var dialog = new AboutForm(_theme);
        dialog.ShowDialog(this);
    }

    private void ShowHelpDialog()
    {
        using var dialog = new HelpForm(_theme);
        dialog.ShowDialog(this);
    }

    private void HandleDetailTabSelectionChanged()
    {
        if (_suppressHistoryTabSelection)
        {
            return;
        }

        if (_detailTabs.SelectedTab != _historyTab)
        {
            _lastNonHistoryTabIndex = _detailTabs.SelectedIndex;
            return;
        }

        if (_selectedMetadata is null)
        {
            RestorePreviousDetailTab();
            SetStatus("Select a song to view its history.");
            return;
        }

        using var dialog = new HistoryForm(_selectedMetadata, _theme);
        dialog.ShowDialog(this);
        RestorePreviousDetailTab();
        SetStatus($"Selected {_selectedMetadata.FileName}");
    }

    private void RestorePreviousDetailTab()
    {
        var targetIndex = _lastNonHistoryTabIndex;
        if (targetIndex < 0 || targetIndex >= _detailTabs.TabPages.Count || _detailTabs.TabPages[targetIndex] == _historyTab)
        {
            targetIndex = 0;
        }

        _suppressHistoryTabSelection = true;
        try
        {
            _detailTabs.SelectedIndex = targetIndex;
        }
        finally
        {
            _suppressHistoryTabSelection = false;
        }
    }

    private bool SongMatchesCurrentView(string songPath)
    {
        if (_songAgeFilter is null)
        {
            return true;
        }

        var cutoff = DateTime.Now.Subtract(TimeSpan.FromDays(_songAgeFilter.Days));
        var lastModified = File.GetLastWriteTime(songPath);
        return _songAgeFilter.Operator == SongAgeFilterOperator.OlderThan
            ? lastModified <= cutoff
            : lastModified >= cutoff;
    }

    private bool DirectoryContainsVisibleSongs(string path)
    {
        if (_folderVisibilityCache.TryGetValue(path, out var isVisible))
        {
            return isVisible;
        }

        var visible = false;

        try
        {
            visible = Directory.EnumerateFiles(path, "*.song")
                .Where(songPath => SongMetadataReader.IsRegularSongFile(Path.GetFileName(songPath)))
                .Any(SongMatchesCurrentView);

            if (!visible)
            {
                foreach (var childPath in Directory.EnumerateDirectories(path))
                {
                    if (DirectoryContainsVisibleSongs(childPath))
                    {
                        visible = true;
                        break;
                    }
                }
            }
        }
        catch
        {
            visible = false;
        }

        _folderVisibilityCache[path] = visible;
        return visible;
    }

    private bool HasVisibleChildDirectories(string path)
    {
        try
        {
            foreach (var childPath in Directory.EnumerateDirectories(path))
            {
                if (DirectoryContainsVisibleSongs(childPath))
                {
                    return true;
                }
            }
        }
        catch
        {
            // Ignore folders that cannot be enumerated.
        }

        return false;
    }

    private void AddSummary(string field, string? value)
    {
        _summaryGrid.Rows.Add(field, FormatField(value));
    }

    private void AutoSizeDetailColumns()
    {
        AutoSizeGridColumns(_summaryGrid, 140, SummaryGridKey);
        AutoSizeGridColumns(_rawGrid, 180, RawGridKey);
    }

    private void AutoSizeGridColumns(DataGridView grid, int minimumWidth, string gridKey)
    {
        if (_customizedGridLayouts.Contains(gridKey) || BrowserConfigStore.HasSavedGridColumnWidths(gridKey))
        {
            return;
        }

        foreach (DataGridViewColumn column in grid.Columns)
        {
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            var width = column.Width;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            column.Width = Math.Max(width, minimumWidth);
        }
    }

    private static string FormatField(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value;
    }

    private void ConfigureNotesTextBox()
    {
        _notesTextBox.Dock = DockStyle.Fill;
        _notesTextBox.BackColor = _theme.PanelBackColor;
        _notesTextBox.BorderStyle = BorderStyle.None;
        _notesTextBox.ForeColor = _theme.TextColor;
        _notesTextBox.Multiline = true;
        _notesTextBox.ReadOnly = true;
        _notesTextBox.ScrollBars = ScrollBars.Vertical;
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = message;
        Application.DoEvents();
    }

}
