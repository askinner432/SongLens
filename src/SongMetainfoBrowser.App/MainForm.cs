using System.Diagnostics;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SongMetainfoBrowser.App;

/// <summary>
/// Main SongLens window. This form coordinates folder browsing, search, metadata display,
/// theming, and the small supporting dialogs used by the app.
/// </summary>
public sealed partial class MainForm : Form
{
    private sealed class FolderTreeView : TreeView
    {
        private const int WmLButtonDblClk = 0x0203;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmLButtonDblClk)
            {
                return;
            }

            base.WndProc(ref m);
        }
    }

    private sealed class SongGridRowData
    {
        public required SongMetadata Metadata { get; init; }
        public SearchResult? Match { get; init; }
    }

    private sealed class DetailTabDefinition
    {
        public required string Key { get; init; }
        public required TabPage TabPage { get; init; }
    }

    private readonly ComboBox _rootPathComboBox = new();
    private readonly Button _browseButton = new();
    private readonly Button _advancedSearchButton = new();
    private readonly Button _refreshButton = new();
    private readonly Button _expandAllButton = new();
    private readonly Button _collapseAllButton = new();
    private readonly TextBox _searchTextBox = new();
    private readonly MenuStrip _menuStrip = new();
    private readonly ToolStripMenuItem _fileMenuItem = new("File");
    private readonly ToolStripMenuItem _saveSnapshotMenuItem = new("Save Snapshot...");
    private readonly ToolStripMenuItem _exportCsvMenuItem = new("Export CSV...");
    private readonly ToolStripMenuItem _exitMenuItem = new("Exit");
    private readonly ToolStripMenuItem _viewMenuItem = new("View");
    private readonly ToolStripMenuItem _toolsMenuItem = new("Tools");
    private readonly ToolStripMenuItem _advancedSearchMenuItem = new("Advanced Search...");
    private readonly ToolStripMenuItem _preferencesMenuItem = new("Preferences...");
    private readonly ToolStripMenuItem _visibleTabsMenuItem = new("Visible tabs...");
    private readonly ToolStripMenuItem _lockCurrentTabMenuItem = new("Use Sticky Tabs");
    private readonly ToolStripMenuItem _changeFontSizeMenuItem = new("Change Font Sizes...");
    private readonly ToolStripMenuItem _helpMenuItem = new("Help");
    private readonly ToolStripMenuItem _songAgeFilterMenuItem = new("Filter songs...");
    private readonly ToolStripMenuItem _themeMenuItem = new("Theme");
    private readonly ToolStripMenuItem _darkThemeMenuItem = new("Dark");
    private readonly ToolStripMenuItem _lightThemeMenuItem = new("Light");
    private readonly ToolStripMenuItem _helpContentsMenuItem = new("SongLens Help");
    private readonly ToolStripMenuItem _aboutMenuItem = new("About SongLens");
    private readonly FolderTreeView _folderTree = new();
    private readonly ContextMenuStrip _folderTreeContextMenu = new();
    private readonly ToolStripMenuItem _contextExpandFolderMenuItem = new("Expand Folder");
    private readonly ToolStripMenuItem _contextCollapseFolderMenuItem = new("Collapse Folder");
    private readonly ToolStripMenuItem _contextRevealFolderInExplorerMenuItem = new("Reveal in Explorer");
    private readonly ToolStripMenuItem _contextDeleteFolderMenuItem = new("Delete Folder");
    private readonly ImageList _folderImages = new();
    private readonly DataGridView _songGrid = new();
    private readonly Panel _songGridHeaderPanel = new();
    private readonly ToolStrip _songGridHeaderToolStrip = new();
    private readonly Label _songGridHintLabel = new();
    private readonly ToolStripDropDownButton _recentSongsDropDownButton = new("Recently Viewed");
    private readonly Button _songGridColumnsButton = new();
    private readonly ContextMenuStrip _songGridContextMenu = new();
    private readonly ToolStripMenuItem _contextOpenInRecommendedAppMenuItem = new("Open in Recommended App");
    private readonly ToolStripMenuItem _contextOpenInAlternateAppMenuItem = new("Open in Alternate App");
    private readonly ToolStripMenuItem _contextRenameSongMenuItem = new("Rename Song...");
    private readonly ToolStripMenuItem _contextRevealInExplorerMenuItem = new("Reveal in Explorer");
    private readonly TabControl _detailTabs = new();
    private readonly TransparentCheckBox _tracksWithEventsCheckBox = new() { Text = "With Events", AutoSize = true };
    private readonly Button _snapshotButton = new();
    private readonly TabPage _summaryTab = new("Summary");
    private readonly TabPage _attributesTab = new("Attributes");
    private readonly TabPage _tracksTab = new("Tracks");
    private readonly TabPage _groupsTab = new("Groups");
    private readonly TabPage _historyTab = new("History");
    private readonly TabPage _mixerTab = new("Mixer");
    private readonly TabPage _notesTab = new("Notes");
    private readonly DataGridView _summaryGrid = new();
    private readonly DataGridView _rawGrid = new();
    private readonly DataGridView _trackGrid = new();
    private readonly DataGridView _groupGrid = new();
    private readonly DataGridView _mixerMainGrid = new();
    private readonly DataGridView _mixerInsertGrid = new();
    private readonly DataGridView _mixerSendGrid = new();
    private readonly TextBox _notesTextBox = new();
    private readonly ToolStripStatusLabel _statusLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _filterStatusLabel = new();
    private readonly ToolStripStatusLabel _stickyTabsStatusLabel = new();
    private readonly ToolTip _toolTip = new();
    private readonly IReadOnlyList<CsvExportField> _csvExportFields;
    private readonly IReadOnlyList<SongGridColumnField> _songGridColumnFields;
    private List<string> _visibleDetailTabKeys;
    private bool _showTracksWithEventsOnly;

    private string _rootPath = "";
    private bool _enableSongLaunch;
    private bool _searchMode;
    private bool _allSongsMode;
    private AdvancedSearchQuery? _advancedSearchQuery;
    private AdvancedSearchQuery? _lastAdvancedSearchQuery;
    private List<SavedAdvancedSearch> _savedAdvancedSearches;
    private SongAgeFilter? _songAgeFilter;
    private bool _displayFilterResultsInSongGrid;
    private SongMetadata? _selectedMetadata;
    private int _lastNonHistoryTabIndex;
    private bool _suppressHistoryTabSelection;
    private bool _suppressFolderTreeSelectionLoad;
    private bool _lockCurrentDetailTab;
    private bool _restoreFilterSessionOnStartup;
    private bool _restoreAdvancedSearchSessionOnStartup;
    private string _songGridSortColumnName = "Song";
    private ListSortDirection _songGridSortDirection = ListSortDirection.Ascending;
    private bool _applyingSongGridSort;
    private bool _promptForRootOnFirstShow;
    private readonly Dictionary<string, bool> _folderVisibilityCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _customizedGridLayouts = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _folderTreeSingleClickTimer = new();
    private bool _suppressRootPathSelectionChange;
    private bool? _pendingRecentTrackingForNextSelection;
    private AppTheme _theme;
    private AppFontPreferences _fontPreferences;
    private bool _suspendGridWidthPersistence;
    private TableLayoutPanel? _rootLayout;
    private TableLayoutPanel? _leftPanelLayout;
    private TableLayoutPanel? _songGridPanel;
    private TableLayoutPanel? _mixerTabLayout;
    private Panel? _detailTabsHostPanel;
    private TreeNode? _pendingFolderTreeExpandNode;
    private SplitContainer? _songDetailSplit;
    private bool _songDetailSplitUserAdjusted;
    private bool _applyingSongDetailSplit;
    private bool _detailSplitExpandedForSingleSong;
    private List<DetailTabDefinition>? _detailTabDefinitions;

    private const string ClosedFolderImageKey = "folder-closed";
    private const string OpenFolderImageKey = "folder-open";
    private const string ClosedParentFolderImageKey = "folder-parent-closed";
    private const string OpenParentFolderImageKey = "folder-parent-open";
    private const string PlaceholderTag = "__placeholder__";
    private const string SongGridKey = "SongGrid";
    private const string SummaryGridKey = "SummaryGrid";
    private const string RawGridKey = "RawGrid";
    private const string TrackGridKey = "TrackGrid";
    private const string GroupGridKey = "GroupGrid";
    private const string MixerMainGridKey = "MixerMainGrid";
    private const string MixerInsertGridKey = "MixerInsertGrid";
    private const string MixerSendGridKey = "MixerSendGrid";
    private const string SummaryTabKey = "summary";
    private const string AttributesTabKey = "attributes";
    private const string TracksTabKey = "tracks";
    private const string GroupsTabKey = "groups";
    private const string MixerTabKey = "mixer";
    private const string NotesTabKey = "notes";
    private const string HistoryTabKey = "history";

    public MainForm()
    {
        _theme = AppThemes.Resolve(BrowserConfigStore.LoadTheme());
        _fontPreferences = AppFontSettings.LoadPreferences();
        _enableSongLaunch = BrowserConfigStore.LoadEnableSongLaunch();
        _lockCurrentDetailTab = BrowserConfigStore.LoadLockCurrentDetailTab();
        _restoreFilterSessionOnStartup = BrowserConfigStore.LoadRestoreFilterSessionOnStartup();
        _restoreAdvancedSearchSessionOnStartup = BrowserConfigStore.LoadRestoreAdvancedSearchSessionOnStartup();
        _lastNonHistoryTabIndex = BrowserConfigStore.LoadLastSelectedDetailTabIndex() ?? 0;
        var restoreViewAllSongs = _restoreFilterSessionOnStartup && BrowserConfigStore.LoadViewAllSongs();
        _songAgeFilter = _restoreFilterSessionOnStartup && !restoreViewAllSongs
            ? BrowserConfigStore.LoadSongAgeFilterPreference()
            : null;
        _displayFilterResultsInSongGrid = BrowserConfigStore.LoadDisplayFilterResultsInSongGrid();
        _lastAdvancedSearchQuery = _restoreAdvancedSearchSessionOnStartup
            ? BrowserConfigStore.LoadLastAdvancedSearchQuery()
            : null;
        _savedAdvancedSearches = BrowserConfigStore.LoadSavedAdvancedSearches().ToList();
        _csvExportFields = BuildCsvExportFields();
        _songGridColumnFields = BuildSongGridColumnFields();
        _visibleDetailTabKeys = BrowserConfigStore.LoadDetailTabVisibleKeys().ToList();
        _showTracksWithEventsOnly = BrowserConfigStore.LoadShowTracksWithEventsOnly();
        Text = "SongLens";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = AppFontSettings.Scale(new Size(1050, 680), _fontPreferences, AppFontSection.MainUi);
        var defaultWindowSize = AppFontSettings.Scale(new Size(1240, 780), _fontPreferences, AppFontSection.MainUi);
        Size = BrowserConfigStore.LoadMainWindowSize() is Size savedWindowSize
            ? new Size(
                Math.Max(savedWindowSize.Width, MinimumSize.Width),
                Math.Max(savedWindowSize.Height, MinimumSize.Height))
            : defaultWindowSize;
        Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);

        BuildLayout();
        ApplyFontSize();
        ApplyTheme(savePreference: false);
        WireEvents();
        RefreshRecentRootPathDropdown();
        _folderTreeSingleClickTimer.Interval = SystemInformation.DoubleClickTime;

        var savedRoot = BrowserConfigStore.LoadRootPath();
        if (!string.IsNullOrWhiteSpace(savedRoot) && Directory.Exists(savedRoot))
        {
            SetRootPath(savedRoot);
            if (_restoreAdvancedSearchSessionOnStartup && _lastAdvancedSearchQuery is not null)
            {
                RunAdvancedSearch(_lastAdvancedSearchQuery);
            }
            else if (_restoreFilterSessionOnStartup && _displayFilterResultsInSongGrid)
            {
                LoadAllSongs();
            }
        }
        else
        {
            _promptForRootOnFirstShow = true;
            SetStatus("Choose a songs folder to begin.");
        }
    }

    private void BuildLayout()
    {
        // The main window is organized as:
        // menu
        // top toolbar
        // folder tree + song grid/detail tabs split view
        // status bar
        _rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, AppFontSettings.Scale(26, _fontPreferences, AppFontSection.MainUi)));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, AppFontSettings.Scale(38, _fontPreferences, AppFontSection.MainUi)));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, AppFontSettings.Scale(24, _fontPreferences, AppFontSection.MainUi)));
        Controls.Add(_rootLayout);

        _menuStrip.Dock = DockStyle.Fill;
        _menuStrip.RenderMode = ToolStripRenderMode.Professional;
        _menuStrip.Font = Font;
        _fileMenuItem.DropDownItems.Add(_saveSnapshotMenuItem);
        _fileMenuItem.DropDownItems.Add(_exportCsvMenuItem);
        _fileMenuItem.DropDownItems.Add(new ToolStripSeparator());
        _fileMenuItem.DropDownItems.Add(_exitMenuItem);
        _viewMenuItem.DropDownItems.Add(_songAgeFilterMenuItem);
        _viewMenuItem.DropDownItems.Add(_visibleTabsMenuItem);
        _toolsMenuItem.DropDownItems.Add(_advancedSearchMenuItem);
        _toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());
        _toolsMenuItem.DropDownItems.Add(_preferencesMenuItem);
        _helpMenuItem.DropDownItems.Add(_helpContentsMenuItem);
        _helpMenuItem.DropDownItems.Add(new ToolStripSeparator());
        _helpMenuItem.DropDownItems.Add(_aboutMenuItem);
        _menuStrip.Items.Add(_fileMenuItem);
        _menuStrip.Items.Add(_viewMenuItem);
        _menuStrip.Items.Add(_toolsMenuItem);
        _menuStrip.Items.Add(_helpMenuItem);
        MainMenuStrip = _menuStrip;
        _rootLayout.Controls.Add(_menuStrip, 0, 0);
        RefreshRecentlyViewedMenu();

        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            Padding = new Padding(6, 4, 6, 2)
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        _rootLayout.Controls.Add(toolbar, 0, 1);

        _rootPathComboBox.Dock = DockStyle.Fill;
        _rootPathComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _rootPathComboBox.Margin = new Padding(0, 2, 6, 2);
        _rootPathComboBox.IntegralHeight = false;
        _rootPathComboBox.MaxDropDownItems = 10;
        _browseButton.Text = "...";
        _browseButton.AccessibleName = "Browse";
        _toolTip.SetToolTip(_browseButton, "Browse");
        _browseButton.Dock = DockStyle.Fill;
        _browseButton.Margin = new Padding(0, 2, 6, 2);
        _searchTextBox.Dock = DockStyle.Fill;
        _searchTextBox.BorderStyle = BorderStyle.FixedSingle;
        _searchTextBox.Margin = new Padding(6, 2, 6, 2);
        WindowsShell.SetCueBanner(_searchTextBox, "Fast Catalog Search by Keyword");
        _toolTip.SetToolTip(_searchTextBox, "Fast keyword search across your song catalog");
        _advancedSearchButton.Image = CreateAdvancedSearchIcon();
        _advancedSearchButton.Text = "";
        _advancedSearchButton.AccessibleName = "Advanced Search";
        _advancedSearchButton.Dock = DockStyle.Fill;
        _advancedSearchButton.Margin = new Padding(0, 2, 0, 2);
        _toolTip.SetToolTip(_advancedSearchButton, "Advanced filtered search");
        StyleButton(_browseButton, useAccent: false);
        StyleButton(_advancedSearchButton, useAccent: false);

        toolbar.Controls.Add(_rootPathComboBox, 0, 0);
        toolbar.Controls.Add(_browseButton, 1, 0);
        toolbar.Controls.Add(_searchTextBox, 2, 0);
        toolbar.Controls.Add(_advancedSearchButton, 3, 0);

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Panel1MinSize = 80,
            Panel2MinSize = 120
        };
        _rootLayout.Controls.Add(mainSplit, 0, 2);

        _leftPanelLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = new Padding(0)
        };
        _leftPanelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, AppFontSettings.Scale(34, _fontPreferences, AppFontSection.MainUi)));
        _leftPanelLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainSplit.Panel1.Controls.Add(_leftPanelLayout);

        var treeToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = new Padding(6, 4, 6, 2)
        };

        _expandAllButton.Text = "Expand All";
        _expandAllButton.AutoSize = true;
        _expandAllButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _expandAllButton.Margin = new Padding(0, 0, 6, 0);
        _expandAllButton.AccessibleName = "Expand All";
        _toolTip.SetToolTip(_expandAllButton, "Expand all folders");
        _collapseAllButton.Text = "Collapse All";
        _collapseAllButton.AutoSize = true;
        _collapseAllButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _collapseAllButton.Margin = new Padding(0);
        _collapseAllButton.AccessibleName = "Collapse All";
        _toolTip.SetToolTip(_collapseAllButton, "Collapse all folders");
        _refreshButton.Text = "Rescan";
        _refreshButton.AutoSize = true;
        _refreshButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _refreshButton.Margin = new Padding(6, 0, 0, 0);
        _refreshButton.AccessibleName = "Rescan";
        _toolTip.SetToolTip(_refreshButton, "Rescan the folder tree using the current filter");
        StyleButton(_expandAllButton, useAccent: false);
        StyleButton(_collapseAllButton, useAccent: false);
        StyleButton(_refreshButton, useAccent: false);
        StyleFolderToolbarButton(_expandAllButton);
        StyleFolderToolbarButton(_collapseAllButton);
        StyleFolderToolbarButton(_refreshButton);
        treeToolbar.Controls.Add(_expandAllButton);
        treeToolbar.Controls.Add(_collapseAllButton);
        treeToolbar.Controls.Add(_refreshButton);
        _leftPanelLayout.Controls.Add(treeToolbar, 0, 0);

        _folderTree.Dock = DockStyle.Fill;
        _folderTree.HideSelection = false;
        _folderTree.BorderStyle = BorderStyle.None;
        _folderTree.DrawMode = TreeViewDrawMode.OwnerDrawText;
        _folderTree.FullRowSelect = true;
        _folderTree.Indent = 22;
        _folderTree.ItemHeight = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.FolderTree);
        _folderTree.ShowLines = false;
        _folderTree.ShowPlusMinus = false;
        _folderTree.ShowRootLines = false;
        _folderTree.ImageList = _folderImages;
        _folderTree.ImageKey = ClosedFolderImageKey;
        _folderTree.SelectedImageKey = OpenFolderImageKey;
        _folderTreeContextMenu.Items.Add(_contextExpandFolderMenuItem);
        _folderTreeContextMenu.Items.Add(_contextCollapseFolderMenuItem);
        _folderTreeContextMenu.Items.Add(new ToolStripSeparator());
        _folderTreeContextMenu.Items.Add(_contextRevealFolderInExplorerMenuItem);
        _folderTreeContextMenu.Items.Add(new ToolStripSeparator());
        _folderTreeContextMenu.Items.Add(_contextDeleteFolderMenuItem);
        ConfigureFolderImages();
        _leftPanelLayout.Controls.Add(_folderTree, 0, 1);

        _songDetailSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            Panel1MinSize = 80,
            Panel2MinSize = 120
        };
        mainSplit.Panel2.Controls.Add(_songDetailSplit);
        Load += (_, _) =>
        {
            if (mainSplit.Width > 420)
            {
                mainSplit.SplitterDistance = 320;
            }

            if (_songDetailSplit.Height > 420)
            {
                _songDetailSplit.SplitterDistance = 300;
            }

            UpdateSongDetailSplitLayout(force: true);
        };

        Shown += (_, _) =>
        {
            if (_promptForRootOnFirstShow)
            {
                _promptForRootOnFirstShow = false;
                BeginInvoke(ChooseRootFolder);
            }
        };

        ConfigureSongGrid();
        _songGridPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _songGridPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, AppFontSettings.Scale(34, _fontPreferences, AppFontSection.MainUi)));
        _songGridPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _songGridHeaderPanel.Dock = DockStyle.Fill;
        _songGridHeaderPanel.Margin = Padding.Empty;
        _songGridHeaderPanel.Padding = Padding.Empty;

        _songGridHeaderToolStrip.Dock = DockStyle.Left;
        _songGridHeaderToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _songGridHeaderToolStrip.AutoSize = true;
        _songGridHeaderToolStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
        _songGridHeaderToolStrip.Padding = new Padding(6, 4, 0, 2);
        _songGridHeaderToolStrip.Margin = Padding.Empty;
        _songGridHeaderToolStrip.CanOverflow = false;
        _songGridHeaderToolStrip.Items.Add(_recentSongsDropDownButton);
        _recentSongsDropDownButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _recentSongsDropDownButton.AutoSize = true;
        _recentSongsDropDownButton.Padding = new Padding(8, 0, 8, 0);
        _recentSongsDropDownButton.ToolTipText = "Show recently viewed songs";
        _songGridHeaderPanel.Controls.Add(_songGridHeaderToolStrip);

        _songGridHintLabel.Dock = DockStyle.Fill;
        _songGridHintLabel.AutoSize = false;
        _songGridHintLabel.TextAlign = ContentAlignment.MiddleLeft;
        _songGridHintLabel.Padding = new Padding(8, 0, 8, 0);
        _songGridHintLabel.Text = "";
        _songGridHeaderPanel.Controls.Add(_songGridHintLabel);

        _songGridPanel.Controls.Add(_songGridHeaderPanel, 0, 0);
        _songGridPanel.Controls.Add(_songGrid, 0, 1);
        _songDetailSplit.Panel1.Controls.Add(_songGridPanel);

        _detailTabsHostPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = _theme.PanelBackColor
        };
        _songDetailSplit.Panel2.Controls.Add(_detailTabsHostPanel);

        _detailTabs.Dock = DockStyle.Fill;
        _detailTabs.Appearance = TabAppearance.Normal;
        _detailTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _detailTabs.Padding = new Point(12, 4);
        _detailTabs.SizeMode = TabSizeMode.Normal;
        _detailTabs.ItemSize = AppFontSettings.Scale(new Size(110, 24), _fontPreferences, AppFontSection.MainUi);
        _detailTabs.DrawItem += DrawDetailTab;
        _detailTabDefinitions =
        [
            new DetailTabDefinition { Key = SummaryTabKey, TabPage = _summaryTab },
            new DetailTabDefinition { Key = AttributesTabKey, TabPage = _attributesTab },
            new DetailTabDefinition { Key = TracksTabKey, TabPage = _tracksTab },
            new DetailTabDefinition { Key = GroupsTabKey, TabPage = _groupsTab },
            new DetailTabDefinition { Key = MixerTabKey, TabPage = _mixerTab },
            new DetailTabDefinition { Key = NotesTabKey, TabPage = _notesTab },
            new DetailTabDefinition { Key = HistoryTabKey, TabPage = _historyTab }
        ];

        ApplyDetailTabVisibility();
        _detailTabsHostPanel.Controls.Add(_detailTabs);

        _tracksWithEventsCheckBox.Checked = _showTracksWithEventsOnly;
        _tracksWithEventsCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _tracksWithEventsCheckBox.Margin = Padding.Empty;
        _tracksWithEventsCheckBox.Padding = Padding.Empty;
        _tracksWithEventsCheckBox.BackColor = _theme.PanelBackColor;
        _tracksWithEventsCheckBox.AccessibleName = "With Events";
        _toolTip.SetToolTip(_tracksWithEventsCheckBox, "Show only tracks that have assigned events");
        _detailTabsHostPanel.Controls.Add(_tracksWithEventsCheckBox);
        _tracksWithEventsCheckBox.BringToFront();

        _snapshotButton.Text = "Snapshot";
        _snapshotButton.AutoSize = true;
        _snapshotButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _snapshotButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _snapshotButton.Padding = new Padding(8, 0, 8, 0);
        _snapshotButton.Margin = Padding.Empty;
        _snapshotButton.AccessibleName = "Snapshot";
        _toolTip.SetToolTip(_snapshotButton, "View Song Snapshot");
        StyleButton(_snapshotButton, useAccent: true);
        _songGridHeaderPanel.Controls.Add(_snapshotButton);
        _snapshotButton.BringToFront();

        _songGridColumnsButton.Text = "Change Columns";
        _songGridColumnsButton.AutoSize = true;
        _songGridColumnsButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _songGridColumnsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _songGridColumnsButton.Padding = new Padding(8, 0, 8, 0);
        _songGridColumnsButton.Margin = Padding.Empty;
        _songGridColumnsButton.AccessibleName = "Song grid columns";
        _toolTip.SetToolTip(_songGridColumnsButton, "Choose which columns appear in the song grid");
        StyleButton(_songGridColumnsButton, useAccent: false);
        _songGridColumnsButton.FlatAppearance.MouseOverBackColor = _snapshotButton.FlatAppearance.MouseOverBackColor;
        _songGridHeaderPanel.Controls.Add(_songGridColumnsButton);
        _songGridColumnsButton.BringToFront();
        _songGridHeaderPanel.Resize += (_, _) => PositionDetailHeaderControls();
        _detailTabsHostPanel.Resize += (_, _) => PositionDetailHeaderControls();
        PositionDetailHeaderControls();

        ConfigureDetailGrid(_summaryGrid, ("Field", 180), ("Value", 720));
        ConfigureDetailGrid(_rawGrid, ("Id", 220), ("Value", 720));
        ConfigureDetailGrid(_trackGrid, ("#", 50), ("Track", 260), ("Instrument", 240), ("Track Note", 560));
        ConfigureDetailGrid(_groupGrid, ("Group", 220), ("Track Names", 700));
        ConfigureDetailGrid(_mixerMainGrid, ("Channel", 140), ("Pre", 320), ("Post", 320));
        ConfigureDetailGrid(_mixerInsertGrid, ("Channel", 220), ("Slot", 90), ("Plugin", 220), ("Preset", 360));
        ConfigureDetailGrid(_mixerSendGrid, ("Channel", 220), ("Send", 90), ("Destination", 180), ("Preset", 220));
        ConfigureTrackGridLayout();
        ConfigureGroupGridLayout();
        ApplySavedGridColumnWidths(_songGrid, SongGridKey);
        ApplySavedGridColumnWidths(_summaryGrid, SummaryGridKey);
        ApplySavedGridColumnWidths(_rawGrid, RawGridKey);
        ApplySavedGridColumnWidths(_trackGrid, TrackGridKey);
        ApplySavedGridColumnWidths(_groupGrid, GroupGridKey);
        ApplySavedGridColumnWidths(_mixerMainGrid, MixerMainGridKey);
        ApplySavedGridColumnWidths(_mixerInsertGrid, MixerInsertGridKey);
        ApplySavedGridColumnWidths(_mixerSendGrid, MixerSendGridKey);
        ConfigureNotesTextBox();
        _summaryTab.Controls.Add(_summaryGrid);
        _attributesTab.Controls.Add(_rawGrid);
        _tracksTab.Controls.Add(_trackGrid);
        _groupsTab.Controls.Add(_groupGrid);
        _mixerTab.Controls.Add(BuildMixerTabLayout());
        _notesTab.Controls.Add(_notesTextBox);

        var statusStrip = new StatusStrip
        {
            Dock = DockStyle.Fill,
            SizingGrip = false
        };
        _statusLabel.Text = "";
        _filterStatusLabel.Margin = new Padding(12, 3, 0, 2);
        _stickyTabsStatusLabel.Margin = new Padding(12, 3, 0, 2);
        statusStrip.Items.Add(_statusLabel);
        statusStrip.Items.Add(_filterStatusLabel);
        statusStrip.Items.Add(_stickyTabsStatusLabel);
        _rootLayout.Controls.Add(statusStrip, 0, 3);
        UpdateStatusIndicators();
        UpdateSnapshotActionsAvailability();
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
        _songGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.SongGrid);
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
        _songGrid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        _songGrid.DefaultCellStyle.BackColor = _theme.PanelBackColor;
        _songGrid.DefaultCellStyle.ForeColor = _theme.TextColor;
        _songGrid.DefaultCellStyle.SelectionBackColor = _theme.TreeSelectionColor;
        _songGrid.DefaultCellStyle.SelectionForeColor = _theme.SelectedTextColor;
        _songGrid.AlternatingRowsDefaultCellStyle.BackColor = _theme.PanelAltBackColor;
        _songGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.SongGrid);
        _toolTip.SetToolTip(_songGrid, "Double-click a song row to reveal that file in Windows Explorer.");
        SyncSongGridContextMenuAvailability();
        foreach (var field in _songGridColumnFields)
        {
            AddSongColumn(field.ColumnName, field.Label, field.Width);
        }
        ApplySongGridColumnVisibility(BrowserConfigStore.LoadSongGridVisibleColumnKeys());
        UpdateSongGridFillColumn();
        UpdateSongGridSortGlyphs();
    }

    private void AddSongColumn(string name, string header, int width)
    {
        var index = _songGrid.Columns.Add(name, header);
        _songGrid.Columns[index].Width = width;
        _songGrid.Columns[index].SortMode = DataGridViewColumnSortMode.Programmatic;
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
        grid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
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
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.DefaultCellStyle.BackColor = _theme.PanelBackColor;
        grid.DefaultCellStyle.ForeColor = _theme.TextColor;
        grid.DefaultCellStyle.SelectionBackColor = _theme.TreeSelectionColor;
        grid.DefaultCellStyle.SelectionForeColor = _theme.SelectedTextColor;
        grid.AlternatingRowsDefaultCellStyle.BackColor = _theme.PanelAltBackColor;
        grid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);

        foreach (var column in columns)
        {
            var index = grid.Columns.Add(column.Header, column.Header);
            grid.Columns[index].Width = column.Width;
            grid.Columns[index].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        UpdateResponsiveDetailGridLayout(grid);
    }

    private void ConfigureTrackGridLayout()
    {
        _trackGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _trackGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);

        _trackGrid.Columns["#"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _trackGrid.Columns["#"]!.Width = 50;

        _trackGrid.Columns["Track"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _trackGrid.Columns["Track"]!.Width = 260;

        _trackGrid.Columns["Instrument"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _trackGrid.Columns["Instrument"]!.Width = 240;

        _trackGrid.Columns["Track Note"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _trackGrid.Columns["Track Note"]!.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
    }

    private void ConfigureGroupGridLayout()
    {
        _groupGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _groupGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);

        _groupGrid.Columns["Group"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _groupGrid.Columns["Group"]!.Width = 220;

        _groupGrid.Columns["Track Names"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _groupGrid.Columns["Track Names"]!.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
    }

    private IReadOnlyList<string> GetVisibleDetailTabKeys()
    {
        if (_detailTabDefinitions is null || _detailTabDefinitions.Count == 0)
        {
            return Array.Empty<string>();
        }

        if (_visibleDetailTabKeys.Count == 0)
        {
            return _detailTabDefinitions.Select(definition => definition.Key).ToArray();
        }

        var validKeys = _detailTabDefinitions
            .Select(definition => definition.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selectedKeys = _visibleDetailTabKeys
            .Where(key => validKeys.Contains(key))
            .ToArray();

        return selectedKeys.Length == 0
            ? _detailTabDefinitions.Select(definition => definition.Key).ToArray()
            : selectedKeys;
    }

    private void ApplyDetailTabVisibility(string? preferredTabKey = null)
    {
        if (_detailTabDefinitions is null || _detailTabDefinitions.Count == 0)
        {
            return;
        }

        var visibleKeys = GetVisibleDetailTabKeys();
        var selectedTabKey = preferredTabKey
            ?? GetCurrentDetailTabKey()
            ?? SummaryTabKey;

        _detailTabs.SuspendLayout();
        try
        {
            _detailTabs.TabPages.Clear();
            foreach (var tabDefinition in _detailTabDefinitions)
            {
                if (visibleKeys.Contains(tabDefinition.Key, StringComparer.OrdinalIgnoreCase))
                {
                    _detailTabs.TabPages.Add(tabDefinition.TabPage);
                }
            }

            var preferredIndex = GetVisibleDetailTabIndex(selectedTabKey);
            if (preferredIndex < 0)
            {
                preferredIndex = GetVisibleDetailTabIndex(SummaryTabKey);
            }

            _detailTabs.SelectedIndex = preferredIndex >= 0 ? preferredIndex : 0;
            _lastNonHistoryTabIndex = GetCurrentDetailTabIndex();
        }
        finally
        {
            _detailTabs.ResumeLayout();
        }
    }

    private int GetVisibleDetailTabIndex(string tabKey)
    {
        if (_detailTabDefinitions is null)
        {
            return -1;
        }

        var targetTab = _detailTabDefinitions
            .FirstOrDefault(definition => string.Equals(definition.Key, tabKey, StringComparison.OrdinalIgnoreCase))
            ?.TabPage;

        return targetTab is null
            ? -1
            : _detailTabs.TabPages.IndexOf(targetTab);
    }

    private string? GetCurrentDetailTabKey()
    {
        if (_detailTabDefinitions is null)
        {
            return null;
        }

        var selectedTab = _detailTabs.SelectedTab;
        if (selectedTab is null)
        {
            return null;
        }

        return _detailTabDefinitions
            .FirstOrDefault(definition => definition.TabPage == selectedTab)
            ?.Key;
    }

    private Control BuildMixerTabLayout()
    {
        _mixerTabLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _mixerTabLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, GetMixerMainSectionHeight()));
        _mixerTabLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        _mixerTabLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        _mixerTabLayout.Controls.Add(BuildMixerSection("Main", _mixerMainGrid), 0, 0);
        _mixerTabLayout.Controls.Add(BuildMixerSection("Inserts", _mixerInsertGrid), 0, 1);
        _mixerTabLayout.Controls.Add(BuildMixerSection("Sends", _mixerSendGrid), 0, 2);
        return _mixerTabLayout;
    }

    private Control BuildMixerSection(string title, Control content)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, AppFontSettings.Scale(24, _fontPreferences, AppFontSection.MainUi)));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0)
        };

        layout.Controls.Add(label, 0, 0);
        layout.Controls.Add(content, 0, 1);
        return layout;
    }

    private Bitmap CreateAdvancedSearchIcon()
    {
        var bitmap = new Bitmap(18, 18);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var pen = new Pen(_theme.SearchIconColor, 1.8f)
        {
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round,
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };

        var funnelPoints = new[]
        {
            new PointF(3f, 4f),
            new PointF(15f, 4f),
            new PointF(11f, 9f),
            new PointF(11f, 13.5f),
            new PointF(7f, 15f),
            new PointF(7f, 9f)
        };

        graphics.DrawPolygon(pen, funnelPoints);
        return bitmap;
    }

    private void ConfigureFolderImages()
    {
        _folderImages.Images.Clear();
        _folderImages.ColorDepth = ColorDepth.Depth32Bit;
        _folderImages.ImageSize = AppFontSettings.Scale(new Size(18, 18), _fontPreferences, AppFontSection.FolderTree);
        _folderImages.Images.Add(ClosedFolderImageKey, CreateFolderIcon(isOpen: false, hasExpandableChildren: false));
        _folderImages.Images.Add(OpenFolderImageKey, CreateFolderIcon(isOpen: true, hasExpandableChildren: false));
        _folderImages.Images.Add(ClosedParentFolderImageKey, CreateFolderIcon(isOpen: false, hasExpandableChildren: true));
        _folderImages.Images.Add(OpenParentFolderImageKey, CreateFolderIcon(isOpen: true, hasExpandableChildren: true));
    }

    private Bitmap CreateFolderIcon(bool isOpen, bool hasExpandableChildren)
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

        var folderOffsetX = hasExpandableChildren ? 3 : 0;

        graphics.FillRectangle(tabBrush, 3 + folderOffsetX, 4, 6, 3);
        graphics.DrawRectangle(outlinePen, 3 + folderOffsetX, 4, 6, 3);

        if (isOpen)
        {
            var points = new[]
            {
                new PointF(2.5f + folderOffsetX, 7.5f),
                new PointF(15.0f + folderOffsetX, 7.5f),
                new PointF(13.5f + folderOffsetX, 14.0f),
                new PointF(4.0f + folderOffsetX, 14.0f)
            };
            graphics.FillPolygon(bodyBrush, points);
            graphics.DrawPolygon(outlinePen, points);
        }
        else
        {
            graphics.FillRectangle(bodyBrush, 2 + folderOffsetX, 6, 13, 8);
            graphics.DrawRectangle(outlinePen, 2 + folderOffsetX, 6, 13, 8);
        }

        if (hasExpandableChildren)
        {
            using var badgeBrush = new SolidBrush(_theme.AccentColor);
            using var badgePen = new Pen(_theme.BorderColor, 1f);
            using var plusPen = new Pen(_theme.SelectedTextColor, 1.4f);
            graphics.FillEllipse(badgeBrush, 0.5f, 8.0f, 7.0f, 7.0f);
            graphics.DrawEllipse(badgePen, 0.5f, 8.0f, 7.0f, 7.0f);
            graphics.DrawLine(plusPen, 2.5f, 11.5f, 5.5f, 11.5f);
            graphics.DrawLine(plusPen, 4.0f, 10.0f, 4.0f, 13.0f);
        }

        return bitmap;
    }

    private void StyleButton(Button button, bool useAccent)
    {
        var accentHover = BlendColor(_theme.AccentSoftColor, _theme.AccentColor, 0.35);
        var accentPressed = BlendColor(_theme.AccentSoftColor, _theme.AccentColor, 0.55);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = useAccent ? accentHover : _theme.NeutralHoverColor;
        button.FlatAppearance.MouseDownBackColor = useAccent ? accentPressed : _theme.NeutralPressedColor;
        button.BackColor = _theme.AccentSoftColor;
        button.ForeColor = useAccent ? _theme.SelectedTextColor : _theme.TextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
    }

    private void StyleFolderToolbarButton(Button button)
    {
        var hoverColor = BlendColor(_theme.AccentSoftColor, _theme.AccentColor, 0.18);
        var pressedColor = BlendColor(_theme.AccentSoftColor, _theme.AccentColor, 0.35);
        button.BackColor = _theme.AccentSoftColor;
        button.ForeColor = _theme.TextColor;
        button.FlatAppearance.MouseOverBackColor = hoverColor;
        button.FlatAppearance.MouseDownBackColor = pressedColor;
    }

    private void ApplyFontSize(int? previousMainUiFontSizePoints = null)
    {
        SuspendLayout();
        try
        {
            var priorMainUiFontSizePoints = AppFontSettings.Normalize(previousMainUiFontSizePoints ?? _fontPreferences.MainUi);
            var currentWindowState = WindowState;
            var currentSize = Size;

            Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            MinimumSize = AppFontSettings.Scale(new Size(1050, 680), _fontPreferences, AppFontSection.MainUi);

            _menuStrip.Font = Font;
            _rootPathComboBox.Font = Font;
            _searchTextBox.Font = Font;
            _songGridHintLabel.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            _folderTree.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.FolderTree);
            _songGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.SongGrid);
            _summaryGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
            _rawGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
            _trackGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
            _mixerMainGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
            _mixerInsertGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
            _mixerSendGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
            _songGridHeaderToolStrip.Font = Font;
            _tracksWithEventsCheckBox.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            _snapshotButton.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            _songGridColumnsButton.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            _notesTextBox.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.NotesAndPreviewText);
            _detailTabs.Font = Font;

            if (_rootLayout is not null)
            {
                _rootLayout.RowStyles[0].Height = AppFontSettings.Scale(26, _fontPreferences, AppFontSection.MainUi);
                _rootLayout.RowStyles[1].Height = AppFontSettings.Scale(38, _fontPreferences, AppFontSection.MainUi);
                _rootLayout.RowStyles[3].Height = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.MainUi);
            }

            if (_leftPanelLayout is not null)
            {
                _leftPanelLayout.RowStyles[0].Height = AppFontSettings.Scale(34, _fontPreferences, AppFontSection.MainUi);
            }

            if (_songGridPanel is not null)
            {
                _songGridPanel.RowStyles[0].Height = AppFontSettings.Scale(34, _fontPreferences, AppFontSection.MainUi);
            }

            _folderTree.ItemHeight = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.FolderTree);
            _detailTabs.ItemSize = AppFontSettings.Scale(new Size(110, 24), _fontPreferences, AppFontSection.MainUi);
            PositionDetailHeaderControls();
            _songGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.SongGrid);
            _summaryGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
            _rawGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
            _trackGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
            _mixerMainGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
            _mixerInsertGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
            _mixerSendGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
            _songGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.SongGrid);
            _summaryGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
            _rawGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
            _trackGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
            _mixerMainGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
            _mixerInsertGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
            _mixerSendGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
            _folderImages.ImageSize = AppFontSettings.Scale(new Size(18, 18), _fontPreferences, AppFontSection.FolderTree);
            ConfigureFolderImages();
            UpdateMixerLayoutHeights();
            UpdateSongDetailSplitLayout(force: true);

            if (IsHandleCreated && currentWindowState == FormWindowState.Normal && priorMainUiFontSizePoints != _fontPreferences.MainUi)
            {
                var scaleFactor = _fontPreferences.MainUi / (float)priorMainUiFontSizePoints;
                var scaledWidth = Math.Max(MinimumSize.Width, (int)Math.Round(currentSize.Width * scaleFactor));
                var scaledHeight = Math.Max(MinimumSize.Height, (int)Math.Round(currentSize.Height * scaleFactor));
                Size = new Size(scaledWidth, scaledHeight);
            }
        }
        finally
        {
            ResumeLayout(performLayout: true);
        }
    }

    private static Color BlendColor(Color baseColor, Color accentColor, double accentWeight)
    {
        var weight = Math.Max(0d, Math.Min(1d, accentWeight));
        var baseWeight = 1d - weight;

        return Color.FromArgb(
            (int)Math.Round((baseColor.R * baseWeight) + (accentColor.R * weight)),
            (int)Math.Round((baseColor.G * baseWeight) + (accentColor.G * weight)),
            (int)Math.Round((baseColor.B * baseWeight) + (accentColor.B * weight)));
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
        var unselectedTabTextColor = BlendColor(_theme.MutedTextColor, _theme.TextColor, 0.45);
        using var textBrush = new SolidBrush(isSelected ? _theme.TextColor : unselectedTabTextColor);
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
        _toolsMenuItem.ForeColor = _theme.TextColor;
        _helpMenuItem.ForeColor = _theme.TextColor;
        _songAgeFilterMenuItem.ForeColor = _theme.TextColor;
        _themeMenuItem.ForeColor = _theme.TextColor;
        _darkThemeMenuItem.ForeColor = _theme.TextColor;
        _lightThemeMenuItem.ForeColor = _theme.TextColor;
        _preferencesMenuItem.ForeColor = _theme.TextColor;
        _helpContentsMenuItem.ForeColor = _theme.TextColor;
        _aboutMenuItem.ForeColor = _theme.TextColor;
        _viewMenuItem.BackColor = _theme.AppBackColor;
        _toolsMenuItem.BackColor = _theme.AppBackColor;
        _helpMenuItem.BackColor = _theme.AppBackColor;
        _songAgeFilterMenuItem.BackColor = _theme.PanelBackColor;
        _themeMenuItem.BackColor = _theme.PanelBackColor;
        _darkThemeMenuItem.BackColor = _theme.PanelBackColor;
        _lightThemeMenuItem.BackColor = _theme.PanelBackColor;
        _preferencesMenuItem.BackColor = _theme.PanelBackColor;
        _helpContentsMenuItem.BackColor = _theme.PanelBackColor;
        _aboutMenuItem.BackColor = _theme.PanelBackColor;
        _lockCurrentTabMenuItem.Checked = _lockCurrentDetailTab;
        _lockCurrentTabMenuItem.CheckOnClick = true;
        _lockCurrentTabMenuItem.ForeColor = _theme.TextColor;
        _lockCurrentTabMenuItem.BackColor = _theme.PanelBackColor;
        _darkThemeMenuItem.Checked = string.Equals(_theme.Name, AppThemes.Dark.Name, StringComparison.OrdinalIgnoreCase);
        _lightThemeMenuItem.Checked = string.Equals(_theme.Name, AppThemes.Light.Name, StringComparison.OrdinalIgnoreCase);

        _rootPathComboBox.BackColor = _theme.PanelBackColor;
        _rootPathComboBox.ForeColor = _theme.TextColor;
        _advancedSearchButton.Image = CreateAdvancedSearchIcon();
        _searchTextBox.BackColor = _theme.PanelBackColor;
        _searchTextBox.ForeColor = _theme.TextColor;
        StyleButton(_browseButton, useAccent: false);
        StyleButton(_refreshButton, useAccent: false);
        StyleButton(_expandAllButton, useAccent: false);
        StyleButton(_collapseAllButton, useAccent: false);
        StyleButton(_advancedSearchButton, useAccent: false);
        StyleButton(_snapshotButton, useAccent: true);
        StyleButton(_songGridColumnsButton, useAccent: false);
        _songGridColumnsButton.FlatAppearance.MouseOverBackColor = _snapshotButton.FlatAppearance.MouseOverBackColor;
        _tracksWithEventsCheckBox.BackColor = _theme.PanelBackColor;
        _tracksWithEventsCheckBox.ForeColor = _theme.TextColor;
        StyleFolderToolbarButton(_expandAllButton);
        StyleFolderToolbarButton(_collapseAllButton);
        StyleFolderToolbarButton(_refreshButton);
        _songGridHeaderToolStrip.Renderer = new FlatToolStripButtonRenderer(_theme);
        _songGridHeaderToolStrip.BackColor = _theme.PanelBackColor;
        _songGridHeaderToolStrip.ForeColor = _theme.TextColor;
        _recentSongsDropDownButton.BackColor = _theme.AccentSoftColor;
        _recentSongsDropDownButton.ForeColor = _theme.TextColor;
        _recentSongsDropDownButton.DropDown.BackColor = _theme.PanelBackColor;
        _recentSongsDropDownButton.DropDown.ForeColor = _theme.TextColor;
        _recentSongsDropDownButton.DropDown.Renderer = new ThemeMenuRenderer(_theme);

        ApplyThemeToChildContainers(this);

        _folderTree.BackColor = _theme.PanelBackColor;
        _folderTree.ForeColor = _theme.TextColor;
        _folderTree.LineColor = _theme.BorderColor;
        _songGridHeaderPanel.BackColor = _theme.PanelBackColor;
        _songGridHintLabel.BackColor = _theme.PanelBackColor;
        _songGridHintLabel.ForeColor = _theme.MutedTextColor;
        ConfigureFolderImages();
        _folderTree.Invalidate();

        if (_detailTabsHostPanel is not null)
        {
            _detailTabsHostPanel.BackColor = _theme.PanelBackColor;
        }

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
        ApplyGridTheme(_mixerMainGrid);
        ApplyGridTheme(_mixerInsertGrid);
        ApplyGridTheme(_mixerSendGrid);

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
        _filterStatusLabel.ForeColor = _theme.TextColor;
        _stickyTabsStatusLabel.ForeColor = _theme.TextColor;

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
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
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

    private void SyncSongGridContextMenuAvailability()
    {
        _songGridContextMenu.Items.Clear();
        if (_enableSongLaunch)
        {
            _songGridContextMenu.Items.Add(_contextOpenInRecommendedAppMenuItem);
            _songGridContextMenu.Items.Add(_contextOpenInAlternateAppMenuItem);
            _songGridContextMenu.Items.Add(new ToolStripSeparator());
        }

        _songGridContextMenu.Items.Add(_contextRevealInExplorerMenuItem);
        _songGrid.ContextMenuStrip = _songGridContextMenu;

        if (_selectedMetadata is not null)
        {
            UpdateLaunchActionLabels(_selectedMetadata);
        }
        else
        {
            _contextOpenInRecommendedAppMenuItem.Visible = _enableSongLaunch;
            _contextOpenInAlternateAppMenuItem.Visible = false;
        }
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

        UpdateResponsiveDetailGridLayout(grid);

        if (ReferenceEquals(grid, _songGrid))
        {
            UpdateSongGridFillColumn();
        }
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
            if (ReferenceEquals(grid, _songGrid) && column.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
            {
                continue;
            }

            if (IsResponsiveDetailGrid(grid)
                && column.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
            {
                continue;
            }

            widths[column.Name] = column.Width;
        }

        BrowserConfigStore.SaveGridColumnWidths(gridKey, widths);
        _customizedGridLayouts.Add(gridKey);
    }

    private void UpdateSongGridFillColumn()
    {
        if (_songGrid.Columns.Count == 0)
        {
            return;
        }

        foreach (DataGridViewColumn column in _songGrid.Columns)
        {
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        }

        var fillCandidateNames = new[] { "Title", "Song", "Comment", "Path", "SongNotes" };
        DataGridViewColumn? fillColumn = null;

        foreach (var candidateName in fillCandidateNames)
        {
            if (_songGrid.Columns.Contains(candidateName)
                && _songGrid.Columns[candidateName] is DataGridViewColumn candidate
                && candidate.Visible)
            {
                fillColumn = candidate;
                break;
            }
        }

        fillColumn ??= _songGrid.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Visible)
            .OrderBy(column => column.DisplayIndex)
            .LastOrDefault();

        if (fillColumn is null)
        {
            return;
        }

        fillColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        fillColumn.FillWeight = 100;
    }

    private void UpdateResponsiveDetailGridLayout(DataGridView grid)
    {
        if (!IsResponsiveDetailGrid(grid) || grid.Columns.Count < 2)
        {
            return;
        }

        var fillColumn = grid.Columns[grid.Columns.Count - 1];
        for (var index = 0; index < grid.Columns.Count - 1; index++)
        {
            grid.Columns[index].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        }

        fillColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        fillColumn.FillWeight = 100;
    }

    private bool IsResponsiveDetailGrid(DataGridView grid)
    {
        return ReferenceEquals(grid, _summaryGrid)
            || ReferenceEquals(grid, _rawGrid)
            || ReferenceEquals(grid, _mixerMainGrid)
            || ReferenceEquals(grid, _mixerInsertGrid)
            || ReferenceEquals(grid, _mixerSendGrid);
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

        var imageKey = GetFolderImageKey(args.Node, args.Node.IsExpanded);
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

    private static string GetFolderImageKey(TreeNode node, bool isExpanded)
    {
        var hasExpandableChildren = HasExpandableChildren(node);
        return (isExpanded, hasExpandableChildren) switch
        {
            (true, true) => OpenParentFolderImageKey,
            (false, true) => ClosedParentFolderImageKey,
            (true, false) => OpenFolderImageKey,
            _ => ClosedFolderImageKey
        };
    }

    private static void SetNodeImage(TreeNode node, bool isExpanded)
    {
        node.ImageKey = GetFolderImageKey(node, isExpanded);
        node.SelectedImageKey = GetFolderImageKey(node, isExpanded: true);
    }

    private static bool HasExpandableChildren(TreeNode node)
    {
        return node.Nodes.Count > 0;
    }

    private void WireEvents()
    {
        _browseButton.Click += (_, _) => ChooseRootFolder();
        _advancedSearchButton.Click += (_, _) => ShowAdvancedSearchDialog();
        _refreshButton.Click += (_, _) => RescanFolderTree();
        _expandAllButton.Click += (_, _) => ExpandAllFolders();
        _collapseAllButton.Click += (_, _) => CollapseAllFolders();
        _songAgeFilterMenuItem.Click += (_, _) => ShowSongAgeFilterDialog();
        _songGridColumnsButton.Click += (_, _) => ShowSongGridColumnsDialog();
        _visibleTabsMenuItem.Click += (_, _) => ShowVisibleTabsDialog();
        _lockCurrentTabMenuItem.Click += (_, _) => ToggleLockCurrentDetailTab();
        _advancedSearchMenuItem.Click += (_, _) => ShowAdvancedSearchDialog();
        _preferencesMenuItem.Click += (_, _) => ShowPreferencesDialog();
        _darkThemeMenuItem.Click += (_, _) => ChangeTheme(AppThemes.Dark);
        _lightThemeMenuItem.Click += (_, _) => ChangeTheme(AppThemes.Light);
        _helpContentsMenuItem.Click += (_, _) => ShowHelpDialog();
        _aboutMenuItem.Click += (_, _) => ShowAboutDialog();
        _contextExpandFolderMenuItem.Click += (_, _) => ExpandSelectedFolder();
        _contextCollapseFolderMenuItem.Click += (_, _) => CollapseSelectedFolder();
        _contextRevealFolderInExplorerMenuItem.Click += (_, _) => RevealSelectedFolderInExplorer();
        _contextDeleteFolderMenuItem.Click += (_, _) => ConfirmDeleteSelectedFolder();
        _songGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_songGrid, SongGridKey);
        _songGrid.ColumnHeaderMouseClick += (_, args) => HandleSongGridColumnHeaderClick(args.ColumnIndex);
        _songGrid.Sorted += (_, _) => HandleSongGridSorted();
        _rootPathComboBox.SelectionChangeCommitted += (_, _) => HandleRootPathSelectionCommitted();
        _summaryGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_summaryGrid, SummaryGridKey);
        _rawGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_rawGrid, RawGridKey);
        _trackGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_trackGrid, TrackGridKey);
        _tracksWithEventsCheckBox.CheckedChanged += (_, _) => ToggleTracksWithEventsOnly();
        _groupGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_groupGrid, GroupGridKey);
        _mixerMainGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_mixerMainGrid, MixerMainGridKey);
        _mixerInsertGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_mixerInsertGrid, MixerInsertGridKey);
        _mixerSendGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_mixerSendGrid, MixerSendGridKey);
        FormClosing += (_, _) => PersistSessionSettings();
        if (_songDetailSplit is not null)
        {
            _songDetailSplit.SplitterMoved += (_, _) =>
            {
                if (_applyingSongDetailSplit)
                {
                    return;
                }

                _songDetailSplitUserAdjusted = true;
            };
        }
        _detailTabs.SelectedIndexChanged += (_, _) => HandleDetailTabSelectionChanged();
        _contextOpenInRecommendedAppMenuItem.Click += (_, _) => OpenSelectedSongInRecommendedApp();
        _contextOpenInAlternateAppMenuItem.Click += (_, _) => OpenSelectedSongInAlternateApp();
        _contextRenameSongMenuItem.Click += (_, _) => RenameSelectedSong();
        _contextRevealInExplorerMenuItem.Click += (_, _) => RevealSelectedSongInExplorer();
        _saveSnapshotMenuItem.Click += (_, _) => SaveSnapshot();
        _snapshotButton.Click += (_, _) => SaveSnapshot();
        _exportCsvMenuItem.Click += (_, _) => ExportCurrentSongsToCsv();
        _exitMenuItem.Click += (_, _) => Close();
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
            if (_suppressFolderTreeSelectionLoad)
            {
                return;
            }

            if (_searchMode && _advancedSearchQuery is null)
            {
                return;
            }
            if (args.Node?.Tag is string folderPath)
            {
                _advancedSearchQuery = null;
                _allSongsMode = false;
                _searchTextBox.Clear();
                var trackSingleSongRecent = args.Action is TreeViewAction.ByMouse or TreeViewAction.ByKeyboard;
                LoadSongsForFolder(folderPath, trackSingleSongRecent);
            }
        };
        _folderTree.NodeMouseClick += (_, args) =>
        {
            if (args.Node is null)
            {
                return;
            }

            if (args.Button == MouseButtons.Right)
            {
                CancelPendingFolderTreeSingleClick();
                _suppressFolderTreeSelectionLoad = true;
                try
                {
                    _folderTree.SelectedNode = args.Node;
                }
                finally
                {
                    _suppressFolderTreeSelectionLoad = false;
                }

                UpdateFolderContextMenu(args.Node);
                _folderTreeContextMenu.Show(_folderTree, args.Location);
                return;
            }

            if (args.Button != MouseButtons.Left)
            {
                return;
            }

            if (_searchMode && args.Node.Tag is string searchFolderPath)
            {
                _searchMode = false;
                _advancedSearchQuery = null;
                _allSongsMode = false;
                _searchTextBox.Clear();
                _folderTree.SelectedNode = args.Node;
                LoadSongsForFolder(searchFolderPath, trackSingleSongRecent: true);
                return;
            }

            if (!HasExpandableChildren(args.Node))
            {
                CancelPendingFolderTreeSingleClick();
                return;
            }

            ScheduleFolderTreeSingleClickToggle(args.Node);
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
        _folderTreeSingleClickTimer.Tick += (_, _) =>
        {
            _folderTreeSingleClickTimer.Stop();
            if (_pendingFolderTreeExpandNode?.TreeView is null || _pendingFolderTreeExpandNode.TreeView.IsDisposed)
            {
                _pendingFolderTreeExpandNode = null;
                return;
            }

            if (HasExpandableChildren(_pendingFolderTreeExpandNode))
            {
                if (_pendingFolderTreeExpandNode.IsExpanded)
                {
                    _pendingFolderTreeExpandNode.Collapse(false);
                }
                else
                {
                    _pendingFolderTreeExpandNode.Expand();
                }
            }

            _pendingFolderTreeExpandNode = null;
        };
        _songGrid.CellMouseDown += (_, args) =>
        {
            if (args.Button != MouseButtons.Right || args.RowIndex < 0 || args.RowIndex >= _songGrid.Rows.Count)
            {
                return;
            }

            SelectSongRow(args.RowIndex);
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

    private void HandleRootPathSelectionCommitted()
    {
        if (_suppressRootPathSelectionChange || _rootPathComboBox.SelectedItem is not string selectedPath)
        {
            return;
        }

        if (!string.Equals(Path.GetFullPath(selectedPath), _rootPath, StringComparison.OrdinalIgnoreCase))
        {
            SetRootPath(selectedPath);
        }
    }

    private void RefreshRecentRootPathDropdown(string? selectedPath = null)
    {
        var recentPaths = BrowserConfigStore.LoadRecentRootPaths().ToList();
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            var fullSelectedPath = Path.GetFullPath(selectedPath);
            recentPaths.RemoveAll(path => string.Equals(path, fullSelectedPath, StringComparison.OrdinalIgnoreCase));
            recentPaths.Insert(0, fullSelectedPath);
        }

        _suppressRootPathSelectionChange = true;
        try
        {
            _rootPathComboBox.BeginUpdate();
            _rootPathComboBox.Items.Clear();
            foreach (var recentPath in recentPaths.Take(10))
            {
                _rootPathComboBox.Items.Add(recentPath);
            }

            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                _rootPathComboBox.SelectedItem = Path.GetFullPath(selectedPath);
            }
            else
            {
                _rootPathComboBox.SelectedIndex = _rootPathComboBox.Items.Count > 0 ? 0 : -1;
            }
        }
        finally
        {
            _rootPathComboBox.EndUpdate();
            _suppressRootPathSelectionChange = false;
        }
    }

    private void RefreshRecentlyViewedMenu()
    {
        _recentSongsDropDownButton.DropDownItems.Clear();
        var recentSongPaths = BrowserConfigStore.LoadRecentSongPaths()
            .Where(File.Exists)
            .ToArray();

        _recentSongsDropDownButton.Enabled = recentSongPaths.Length > 0;

        if (recentSongPaths.Length == 0)
        {
            return;
        }

        foreach (var songPath in recentSongPaths)
        {
            var fileName = Path.GetFileName(songPath);
            _recentSongsDropDownButton.DropDownItems.Add(CreateRecentSongMenuItem(fileName, songPath));
        }
    }

    private ToolStripMenuItem CreateRecentSongMenuItem(string fileName, string songPath)
    {
        var menuItem = new ToolStripMenuItem(fileName)
        {
            Tag = songPath,
            ToolTipText = songPath,
            BackColor = _theme.PanelBackColor,
            ForeColor = _theme.TextColor
        };
        menuItem.Click += (_, _) => OpenRecentlyViewedSong(songPath);
        return menuItem;
    }

    private void OpenRecentlyViewedSong(string songPath)
    {
        if (!File.Exists(songPath))
        {
            using var messageDialog = new ThemedMessageForm(Text, $"Song not found:\n{songPath}", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            RefreshRecentlyViewedMenu();
            return;
        }

        var songFolderPath = Path.GetDirectoryName(songPath);
        if (string.IsNullOrWhiteSpace(songFolderPath) || !Directory.Exists(songFolderPath))
        {
            using var messageDialog = new ThemedMessageForm(Text, $"Song folder not found:\n{songPath}", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        var targetRootPath = BrowserConfigStore.LoadRecentRootPaths()
            .Where(rootPath => IsPathUnderRoot(rootPath, songPath))
            .OrderByDescending(path => path.Length)
            .FirstOrDefault()
            ?? songFolderPath;

        SetRootPath(targetRootPath);
        FocusTreeOnFolder(songFolderPath);
        LoadSongsForFolder(songFolderPath);
        TrySelectSongRowByPath(songPath);
    }

    private static bool IsPathUnderRoot(string rootPath, string candidatePath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || string.IsNullOrWhiteSpace(candidatePath))
        {
            return false;
        }

        var normalizedRoot = Path.GetFullPath(rootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var normalizedCandidate = Path.GetFullPath(candidatePath);
        return normalizedCandidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
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
        RefreshRecentRootPathDropdown(_rootPath);
        _searchMode = false;
        _allSongsMode = false;
        UpdateStatusIndicators();
        _folderVisibilityCache.Clear();
        _folderTree.Nodes.Clear();
        _songGrid.Rows.Clear();
        ClearDetailViews();
        _selectedMetadata = null;
        UpdateSnapshotActionsAvailability();
        UpdateSongGridHintVisibility();
        RebuildFolderTree(_rootPath);
        SetStatus("Ready");
    }

    private void RescanFolderTree()
    {
        if (string.IsNullOrWhiteSpace(_rootPath) || !Directory.Exists(_rootPath))
        {
            return;
        }

        var preferredFolderPath = _folderTree.SelectedNode?.Tag as string ?? _rootPath;
        RebuildFolderTree(preferredFolderPath);
        SetStatus("Ready");
    }

    private void RebuildFolderTree(string? preferredFolderPath = null)
    {
        _folderVisibilityCache.Clear();
        _folderTree.Nodes.Clear();

        // The tree only shows folders that contain visible song files somewhere
        // under them, so the root is populated lazily after the path is accepted.
        var rootDirectory = new DirectoryInfo(_rootPath);
        var rootNode = new TreeNode(rootDirectory.Name) { Tag = rootDirectory.FullName };
        SetNodeImage(rootNode, isExpanded: false);
        _folderTree.Nodes.Add(rootNode);
        if (HasVisibleChildDirectories(rootDirectory.FullName))
        {
            AddPlaceholderChild(rootNode);
        }

        rootNode.Expand();
        SetNodeImage(rootNode, isExpanded: true);

        var targetFolderPath = preferredFolderPath;
        if (string.IsNullOrWhiteSpace(targetFolderPath)
            || !Directory.Exists(targetFolderPath)
            || !IsPathAtOrUnderRoot(targetFolderPath))
        {
            targetFolderPath = _rootPath;
        }

        if (string.Equals(targetFolderPath, _rootPath, StringComparison.OrdinalIgnoreCase)
            && !FolderHasVisibleSongsDirectly(_rootPath)
            && FindFirstVisibleSongFolder(_rootPath) is { } firstSongFolderPath)
        {
            targetFolderPath = firstSongFolderPath;
        }

        _suppressFolderTreeSelectionLoad = true;
        try
        {
            FocusTreeOnFolder(targetFolderPath);
            if (_folderTree.SelectedNode is null)
            {
                _folderTree.SelectedNode = rootNode;
            }
        }
        finally
        {
            _suppressFolderTreeSelectionLoad = false;
        }

        if (!_searchMode && _folderTree.SelectedNode?.Tag is string selectedFolderPath)
        {
            LoadSongsForFolder(selectedFolderPath);
            EnsureSongFolderSelection();
        }
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
            rootNode.EnsureVisible();
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

    private void UpdateFolderContextMenu(TreeNode node)
    {
        var hasExpandableChildren = HasExpandableChildren(node);
        _contextExpandFolderMenuItem.Visible = hasExpandableChildren && !node.IsExpanded;
        _contextCollapseFolderMenuItem.Visible = hasExpandableChildren && node.IsExpanded;
    }

    private void ExpandSelectedFolder()
    {
        if (_folderTree.SelectedNode is not TreeNode node || !HasExpandableChildren(node))
        {
            return;
        }

        PopulateFolderChildren(node);
        node.Expand();
        SetNodeImage(node, isExpanded: true);
    }

    private void CollapseSelectedFolder()
    {
        if (_folderTree.SelectedNode is not TreeNode node || !HasExpandableChildren(node))
        {
            return;
        }

        node.Collapse(false);
        SetNodeImage(node, isExpanded: false);
    }

    private void RevealSelectedFolderInExplorer()
    {
        if (_folderTree.SelectedNode?.Tag is not string folderPath || !Directory.Exists(folderPath))
        {
            using var messageDialog = new ThemedMessageForm(Text, "Select a folder before revealing it in Explorer.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folderPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            using var messageDialog = new ThemedMessageForm(Text, $"Could not open Explorer.\n\n{ex.Message}", _theme, ThemedMessageKind.Error);
            messageDialog.ShowDialog(this);
            SetStatus($"Could not open Explorer: {ex.Message}");
        }
    }

    private void ConfirmDeleteSelectedFolder()
    {
        if (_folderTree.SelectedNode?.Tag is not string folderPath || !Directory.Exists(folderPath))
        {
            return;
        }

        var selectedPath = Path.GetFullPath(folderPath);
        var rootPath = Path.GetFullPath(_rootPath);

        if (!IsPathAtOrUnderRoot(selectedPath))
        {
            using var invalidPathDialog = new ThemedMessageForm(
                Text,
                "The selected folder is outside the current songs root folder and cannot be deleted.",
                _theme,
                ThemedMessageKind.Warning);
            invalidPathDialog.ShowDialog(this);
            return;
        }

        if (string.Equals(selectedPath, rootPath, StringComparison.OrdinalIgnoreCase))
        {
            using var rootDeleteDialog = new ThemedMessageForm(
                Text,
                "The current songs root folder cannot be deleted from SongLens.",
                _theme,
                ThemedMessageKind.Information);
            rootDeleteDialog.ShowDialog(this);
            return;
        }

        var folderName = Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var deleteSummary = BuildDeleteFolderConfirmationMessage(selectedPath, folderName);
        using var dialog = new ThemedConfirmationForm(
            $"Delete All Song Information For \"{folderName}\"?",
            deleteSummary,
            _theme);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var parentPath = Directory.GetParent(selectedPath)?.FullName;

        try
        {
            Directory.Delete(selectedPath, recursive: true);
            SetRootPath(_rootPath);

            if (!string.IsNullOrWhiteSpace(parentPath)
                && Directory.Exists(parentPath)
                && IsPathAtOrUnderRoot(parentPath))
            {
                FocusTreeOnFolder(parentPath);
            }
        }
        catch (Exception ex)
        {
            using var deleteFailedDialog = new ThemedMessageForm(
                Text,
                $"Could not delete folder.\n\n{ex.Message}",
                _theme,
                ThemedMessageKind.Error);
            deleteFailedDialog.ShowDialog(this);
        }
    }

    private static string BuildDeleteFolderConfirmationMessage(string folderPath, string folderName)
    {
        var childFolderSummaries = new List<string>();

        try
        {
            var immediateChildFolders = Directory.EnumerateDirectories(folderPath).OrderBy(Path.GetFileName).ToList();

            foreach (var childFolderPath in immediateChildFolders)
            {
                var childFolderName = Path.GetFileName(childFolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                var childFileCount = 0;

                try
                {
                    childFileCount = Directory.EnumerateFiles(childFolderPath, "*", SearchOption.AllDirectories).Count();
                }
                catch
                {
                    // If a child folder cannot be enumerated, show it as zero files.
                }

                childFolderSummaries.Add($"{childFolderName}: {FormatCount(childFileCount, "file")}");
            }
        }
        catch
        {
            // Ignore child folder summaries when enumeration fails.
        }

        const int maxChildSummaries = 6;
        var builder = new StringBuilder();
        builder.AppendLine($"All data in \"{folderName}\" will be deleted.");
        builder.AppendLine();
        builder.AppendLine("This consists of any song files, as well as:");

        if (childFolderSummaries.Count > 0)
        {
            builder.AppendLine();
            foreach (var summary in childFolderSummaries.Take(maxChildSummaries))
            {
                builder.AppendLine(summary);
            }

            var remainingCount = childFolderSummaries.Count - maxChildSummaries;
            if (remainingCount > 0)
            {
                builder.AppendLine($"...and {remainingCount} more.");
            }
        }
        else
        {
            builder.AppendLine();
            builder.AppendLine("No subfolders.");
        }

        builder.AppendLine();
        builder.Append("*Deleted files can usually be recovered using the Windows Recycle Bin utility");
        return builder.ToString();
    }

    private static string FormatCount(int count, string noun)
    {
        return count == 1 ? $"1 {noun}" : $"{count} {noun}s";
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

    private void LoadSongsForFolder(string folderPath, bool trackSingleSongRecent = false)
    {
        if (_searchMode)
        {
            return;
        }

        _allSongsMode = false;
        UpdateStatusIndicators();
        _songGrid.Rows.Clear();
        ClearDetailViews();
        _selectedMetadata = null;
        UpdateSnapshotActionsAvailability();
        UpdateSongGridHintVisibility();
        SetStatus("Loading songs...");

        var loaded = 0;
        var skipped = 0;
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
                catch (Exception ex)
                {
                    skipped++;
                    LogSongReadFailure("LoadSongsForFolder", songPath, ex);
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
            ApplySongGridSort();
            var shouldTrackRecent = _songGrid.Rows.Count == 1
                && (trackSingleSongRecent || RootHasSingleVisibleChoice(folderPath));
            SelectSongRow(0, trackRecent: shouldTrackRecent);
        }

        SetStatus(skipped > 0 ? $"Skipped {skipped} song(s) that could not be loaded." : "Ready");
    }

    private void EnsureSongFolderSelection()
    {
        if (_searchMode || _allSongsMode)
        {
            return;
        }

        if (_songGrid.Rows.Count > 0)
        {
            return;
        }

        if (_folderTree.SelectedNode?.Tag is not string selectedFolderPath)
        {
            return;
        }

        if (FolderHasVisibleSongsDirectly(selectedFolderPath))
        {
            return;
        }

        var fallbackFolderPath = FindFirstVisibleSongFolder(selectedFolderPath)
            ?? FindFirstVisibleSongFolder(_rootPath);
        if (string.IsNullOrWhiteSpace(fallbackFolderPath)
            || string.Equals(fallbackFolderPath, selectedFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        FocusTreeOnFolder(fallbackFolderPath);
        LoadSongsForFolder(fallbackFolderPath);
    }

    private void SearchSongs()
    {
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            return;
        }

        // Search is global beneath the current root folder rather than scoped to the
        // selected tree node. Clearing the query returns the user to folder mode.
        var query = _searchTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            _searchMode = false;
            _advancedSearchQuery = null;
            if (_folderTree.SelectedNode?.Tag is string folderPath)
            {
                LoadSongsForFolder(folderPath);
            }
            return;
        }

        _advancedSearchQuery = null;
        _searchMode = true;
        _allSongsMode = false;
        UpdateStatusIndicators();
        _songGrid.Rows.Clear();
        ClearDetailViews();
        _selectedMetadata = null;
        UpdateSnapshotActionsAvailability();
        UpdateSongGridHintVisibility();
        SetStatus("Searching...");

        var count = 0;
        var skipped = 0;
        var matchedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                matchedFolders.Add(metadata.Folder);
                count++;
            }
            catch (Exception ex)
            {
                skipped++;
                LogSongReadFailure("SearchSongs", songPath, ex);
            }
        }

        if (_songGrid.Rows.Count > 0)
        {
            ApplySongGridSort();
            SelectSongRow(0);
        }

        if (matchedFolders.Count == 1)
        {
            FocusTreeOnFolder(matchedFolders.First());
        }

        SetStatus(skipped > 0 ? $"Found {count} song(s) matching '{query}'. Skipped {skipped} file(s)." : "Ready");
    }

    private void RunAdvancedSearch(AdvancedSearchQuery query)
    {
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            return;
        }

        _advancedSearchQuery = CloneAdvancedSearchQuery(query);
        _lastAdvancedSearchQuery = CloneAdvancedSearchQuery(query);
        _searchMode = true;
        _allSongsMode = false;
        _searchTextBox.Clear();
        UpdateStatusIndicators();
        _songGrid.Rows.Clear();
        ClearDetailViews();
        _selectedMetadata = null;
        UpdateSnapshotActionsAvailability();
        UpdateSongGridHintVisibility();
        SetStatus("Running advanced search...");

        var count = 0;
        var skipped = 0;
        var matchedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var songPath in Directory.EnumerateFiles(_rootPath, "*.song", SearchOption.AllDirectories)
                     .Where(path => SongMetadataReader.IsRegularSongFile(Path.GetFileName(path)))
                     .Where(SongMatchesCurrentView)
                     .OrderBy(path => path))
        {
            try
            {
                var metadata = SongMetadataReader.Read(songPath);
                var match = AdvancedSongSearch.GetMatch(metadata, query);
                if (match is null)
                {
                    continue;
                }

                AddSongRow(metadata, match);
                matchedFolders.Add(metadata.Folder);
                count++;
            }
            catch (Exception ex)
            {
                skipped++;
                LogSongReadFailure("RunAdvancedSearch", songPath, ex);
            }
        }

        if (_songGrid.Rows.Count > 0)
        {
            ApplySongGridSort();
            SelectSongRow(0);
        }

        if (matchedFolders.Count == 1)
        {
            FocusTreeOnFolder(matchedFolders.First());
        }

        SetStatus(skipped > 0 ? $"Advanced search found {count} song(s). Skipped {skipped} file(s)." : "Ready");
    }

    private void LoadAllSongs()
    {
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            return;
        }

        _searchMode = false;
        _advancedSearchQuery = null;
        _allSongsMode = true;
        UpdateStatusIndicators();
        _searchTextBox.Clear();
        _songGrid.Rows.Clear();
        ClearDetailViews();
        _selectedMetadata = null;
        UpdateSnapshotActionsAvailability();
        UpdateSongGridHintVisibility();
        SetStatus("Loading all songs...");

        var loaded = 0;
        var skipped = 0;
        foreach (var songPath in Directory.EnumerateFiles(_rootPath, "*.song", SearchOption.AllDirectories)
                     .Where(path => SongMetadataReader.IsRegularSongFile(Path.GetFileName(path)))
                     .Where(SongMatchesCurrentView)
                     .OrderBy(path => path))
        {
            try
            {
                AddSongRow(SongMetadataReader.Read(songPath), null);
                loaded++;
            }
            catch (Exception ex)
            {
                skipped++;
                LogSongReadFailure("LoadAllSongs", songPath, ex);
            }
        }

        if (_songGrid.Rows.Count > 0)
        {
            ApplySongGridSort();
            SelectSongRow(0);
        }

        SetStatus(skipped > 0 ? $"Skipped {skipped} song(s) that could not be loaded." : "Ready");
    }

    private void FocusTreeOnFolder(string folderPath)
    {
        if (_folderTree.Nodes.Count == 0)
        {
            return;
        }

        var targetPath = Path.GetFullPath(folderPath);
        if (!targetPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_folderTree.Nodes[0] is not TreeNode rootNode || rootNode.Tag is not string rootPath)
        {
            return;
        }

        TreeNode currentNode = rootNode;
        var currentPath = rootPath;

        if (!string.Equals(currentPath, targetPath, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = Path.GetRelativePath(_rootPath, targetPath);
            foreach (var segment in relativePath.Split(
                         [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                         StringSplitOptions.RemoveEmptyEntries))
            {
                PopulateFolderChildren(currentNode);
                currentNode.Expand();
                SetNodeImage(currentNode, isExpanded: true);

                currentPath = Path.Combine(currentPath, segment);
                var nextNode = currentNode.Nodes
                    .Cast<TreeNode>()
                    .FirstOrDefault(node => string.Equals(node.Tag as string, currentPath, StringComparison.OrdinalIgnoreCase));

                if (nextNode is null)
                {
                    return;
                }

                currentNode = nextNode;
            }
        }

        PopulateFolderChildren(currentNode);
        currentNode.EnsureVisible();
        _suppressFolderTreeSelectionLoad = true;
        try
        {
            _folderTree.SelectedNode = currentNode;
        }
        finally
        {
            _suppressFolderTreeSelectionLoad = false;
        }
    }

    private bool IsPathAtOrUnderRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(_rootPath) || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var rootPath = Path.GetFullPath(_rootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var candidatePath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(candidatePath, rootPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return (candidatePath + Path.DirectorySeparatorChar).StartsWith(rootPath, StringComparison.OrdinalIgnoreCase);
    }

    private void ExportCurrentSongsToCsv()
    {
        var exportRows = DetermineCsvExportRows(out var wasCanceled);
        if (wasCanceled)
        {
            return;
        }

        if (exportRows.Count == 0)
        {
            using var messageDialog = new ThemedMessageForm(Text, "There are no songs available to export.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        using var optionsDialog = new CsvExportOptionsForm(_csvExportFields, _theme, BrowserConfigStore.LoadCsvExportFieldKeys());
        if (optionsDialog.ShowDialog(this) != DialogResult.OK || optionsDialog.SelectedFields.Count == 0)
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Export songs to CSV",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = "csv",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = BuildDefaultCsvFileName()
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        try
        {
            var builder = new StringBuilder();
            AppendCsvRow(builder, optionsDialog.SelectedFields.Select(field => field.Label).ToArray());

            foreach (var rowData in exportRows)
            {
                var metadata = rowData.Metadata;
                AppendCsvRow(
                    builder,
                    optionsDialog.SelectedFields
                        .Select(field => field.ValueSelector(metadata, rowData.Match))
                        .ToArray());
            }

            File.WriteAllText(dialog.FileName, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            SetStatus($"Exported {exportRows.Count} song(s) to {dialog.FileName}");
        }
        catch (Exception ex)
        {
            using var messageDialog = new ThemedMessageForm(Text, $"Could not export CSV:\n{ex.Message}", _theme, ThemedMessageKind.Error);
            messageDialog.ShowDialog(this);
            SetStatus($"CSV export failed: {ex.Message}");
        }
    }

    private void SaveSnapshot()
    {
        if (IsAdvancedSearchResultsView())
        {
            PreviewAdvancedSearchResultsSnapshot();
            return;
        }

        if (_selectedMetadata is null)
        {
            using var messageDialog = new ThemedMessageForm(Text, "Select a song before saving a snapshot.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        var selection = new SnapshotSectionSelection();
        while (true)
        {
            using var optionsDialog = new SnapshotOptionsForm(selection, _theme);
            if (optionsDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            selection = optionsDialog.SelectedSections;

            if (optionsDialog.RequestedAction == SnapshotOptionsAction.Preview)
            {
                var previewText = BuildSnapshotPreviewText(_selectedMetadata, selection);
                var previewJson = BuildSnapshotJson(_selectedMetadata, selection);
                using var previewDialog = new SnapshotPreviewForm(previewText, previewJson, selection.Format, _theme);
                previewDialog.ShowDialog(this);
                if (previewDialog.SaveRequestedFormat is SnapshotFormat requestedFormat)
                {
                    SaveSnapshot(_selectedMetadata, selection, requestedFormat);
                    return;
                }

                continue;
            }

            SaveSnapshot(_selectedMetadata, selection, selection.Format);
            return;
        }
    }

    private bool IsAdvancedSearchResultsView()
    {
        return _searchMode && _advancedSearchQuery is not null;
    }

    private void PreviewAdvancedSearchResultsSnapshot()
    {
        if (_songGrid.Rows.Count == 0)
        {
            using var messageDialog = new ThemedMessageForm(Text, "There are no advanced search results to snapshot.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        using var previewDialog = new SnapshotPreviewForm(
            BuildAdvancedSearchGridSnapshotText(),
            BuildAdvancedSearchGridSnapshotJson(),
            SnapshotFormat.Text,
            _theme);
        previewDialog.ShowDialog(this);
        if (previewDialog.SaveRequestedFormat is SnapshotFormat requestedFormat)
        {
            SaveAdvancedSearchResultsSnapshot(requestedFormat);
        }
    }

    private void SaveAdvancedSearchResultsSnapshot(SnapshotFormat format)
    {
        var isJson = format == SnapshotFormat.Json;
        using var dialog = new SaveFileDialog
        {
            Title = "Save advanced search results snapshot",
            Filter = isJson
                ? "JSON files (*.json)|*.json|All files (*.*)|*.*"
                : "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = isJson ? "json" : "txt",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = $"SongLens-advanced-search-snapshot-{DateTime.Now:yyyyMMdd-HHmmss}.{(isJson ? "json" : "txt")}" 
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        try
        {
            var contents = isJson
                ? BuildAdvancedSearchGridSnapshotJson()
                : BuildAdvancedSearchGridSnapshotText();
            File.WriteAllText(dialog.FileName, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            SetStatus($"Saved advanced search snapshot to {dialog.FileName}");
        }
        catch (Exception ex)
        {
            using var messageDialog = new ThemedMessageForm(Text, $"Could not save advanced search snapshot:\n{ex.Message}", _theme, ThemedMessageKind.Error);
            messageDialog.ShowDialog(this);
            SetStatus($"Advanced search snapshot failed: {ex.Message}");
        }
    }

    private IReadOnlyList<DataGridViewColumn> GetVisibleSongGridColumnsInDisplayOrder()
    {
        return _songGrid.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Visible)
            .OrderBy(column => column.DisplayIndex)
            .ToArray();
    }

    private static string GetSnapshotGridCellText(DataGridViewRow row, DataGridViewColumn column)
    {
        return (row.Cells[column.Index].FormattedValue?.ToString() ?? "")
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal);
    }

    private string BuildAdvancedSearchGridSnapshotText()
    {
        var columns = GetVisibleSongGridColumnsInDisplayOrder();
        var rows = _songGrid.Rows.Cast<DataGridViewRow>().Where(row => !row.IsNewRow).ToArray();
        var widths = columns
            .Select(column => Math.Max(
                column.HeaderText.Length,
                rows.Select(row => GetSnapshotGridCellText(row, column).Length).DefaultIfEmpty(0).Max()))
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("SongLens Advanced Search Results Snapshot");
        builder.Append("Captured: ");
        builder.AppendLine(DateTime.Now.ToString());
        builder.Append("Results: ");
        builder.AppendLine(rows.Length.ToString());
        builder.AppendLine();
        builder.AppendLine(string.Join("  ", columns.Select((column, index) => column.HeaderText.PadRight(widths[index]))));
        builder.AppendLine(string.Join("  ", widths.Select(width => new string('-', width))));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join("  ", columns.Select((column, index) => GetSnapshotGridCellText(row, column).PadRight(widths[index]))));
        }

        return builder.ToString().TrimEnd();
    }

    private string BuildAdvancedSearchGridSnapshotJson()
    {
        var columns = GetVisibleSongGridColumnsInDisplayOrder();
        var rows = _songGrid.Rows
            .Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Select(row => columns.ToDictionary(
                column => column.HeaderText,
                column => GetSnapshotGridCellText(row, column)))
            .ToArray();
        var snapshot = new
        {
            SnapshotVersion = 1,
            App = "SongLens",
            CapturedAt = DateTime.Now.ToString(),
            ResultCount = rows.Length,
            Columns = columns.Select(column => column.HeaderText).ToArray(),
            Rows = rows
        };

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
    }

    private List<SongGridRowData> DetermineCsvExportRows(out bool wasCanceled)
    {
        wasCanceled = false;

        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            return [];
        }

        if (_searchMode)
        {
            return _songGrid.Rows
                .Cast<DataGridViewRow>()
                .Select(row => row.Tag as SongGridRowData)
                .Where(rowData => rowData is not null)
                .Cast<SongGridRowData>()
                .ToList();
        }

        if (IsRootSelectedWithNoSongRows())
        {
            using var dialog = new ThemedConfirmationForm("Export CSV", "Export Entire Library?", _theme);
            var result = dialog.ShowDialog(this);

            if (result != DialogResult.OK)
            {
                wasCanceled = true;
                return [];
            }

            return CollectSongsForCsvExport(CsvExportScope.EntireLibrary);
        }

        if (_selectedMetadata is not null)
        {
            using var scopeDialog = new CsvExportScopeForm(canExportCurrentSong: true, _theme);
            if (scopeDialog.ShowDialog(this) != DialogResult.OK)
            {
                wasCanceled = true;
                return [];
            }

            return CollectSongsForCsvExport(scopeDialog.SelectedScope);
        }

        return CollectSongsForCsvExport(CsvExportScope.EntireLibrary);
    }

    private bool IsRootSelectedWithNoSongRows()
    {
        return !_searchMode
               && _songGrid.Rows.Count == 0
               && _folderTree.SelectedNode?.Tag is string selectedPath
               && string.Equals(Path.GetFullPath(selectedPath), _rootPath, StringComparison.OrdinalIgnoreCase);
    }

    private List<SongGridRowData> CollectSongsForCsvExport(CsvExportScope scope)
    {
        if (scope == CsvExportScope.CurrentSong)
        {
            return _selectedMetadata is null
                ? []
                : [new SongGridRowData { Metadata = _selectedMetadata, Match = null }];
        }

        var exportRows = new List<SongGridRowData>();
        foreach (var songPath in Directory.EnumerateFiles(_rootPath, "*.song", SearchOption.AllDirectories)
                     .Where(path => SongMetadataReader.IsRegularSongFile(Path.GetFileName(path)))
                     .Where(SongMatchesCurrentView)
                     .OrderBy(path => path))
        {
            try
            {
                exportRows.Add(new SongGridRowData
                {
                    Metadata = SongMetadataReader.Read(songPath),
                    Match = null
                });
            }
            catch (Exception ex)
            {
                LogSongReadFailure("ExportCurrentSongsToCsv", songPath, ex);
            }
        }

        return exportRows;
    }

    private string BuildDefaultCsvFileName()
    {
        var baseName = _advancedSearchQuery is not null
            ? "SongLens-advanced-search"
            : _searchMode
                ? $"SongLens-search-{SanitizeFileNamePart(_searchTextBox.Text)}"
                : $"SongLens-{SanitizeFileNamePart(Path.GetFileName(_rootPath))}";

        if (string.IsNullOrWhiteSpace(baseName) || baseName.EndsWith("-", StringComparison.Ordinal))
        {
            baseName = "SongLens-export";
        }

        return $"{baseName}-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
    }

    private static IReadOnlyList<CsvExportField> BuildCsvExportFields()
    {
        return
        [
            new CsvExportField { Key = "song", Label = "Song", ValueSelector = (metadata, _) => metadata.FileName, IsDefault = true },
            new CsvExportField { Key = "title", Label = "Title", ValueSelector = (metadata, _) => metadata.Title, IsDefault = true },
            new CsvExportField { Key = "artist", Label = "Artist", ValueSelector = (metadata, _) => metadata.Artist, IsDefault = true },
            new CsvExportField { Key = "year", Label = "Year", ValueSelector = (metadata, _) => metadata.Year, IsDefault = true },
            new CsvExportField { Key = "dateCreated", Label = "Date Created", ValueSelector = (metadata, _) => metadata.DateCreated, IsDefault = false },
            new CsvExportField { Key = "lastModified", Label = "Last Modified", ValueSelector = (metadata, _) => metadata.LastModified, IsDefault = true },
            new CsvExportField { Key = "tempo", Label = "Tempo", ValueSelector = (metadata, _) => metadata.Tempo, IsDefault = true },
            new CsvExportField { Key = "keySignature", Label = "Key Signature", ValueSelector = (metadata, _) => metadata.KeySignature, IsDefault = true },
            new CsvExportField { Key = "timeSignature", Label = "Time Signature", ValueSelector = (metadata, _) => metadata.TimeSignature, IsDefault = false },
            new CsvExportField { Key = "trackCount", Label = "Track Count", ValueSelector = (metadata, _) => metadata.TrackCount, IsDefault = false },
            new CsvExportField { Key = "length", Label = "Length", ValueSelector = (metadata, _) => metadata.Length, IsDefault = true },
            new CsvExportField { Key = "sampleRate", Label = "Sample Rate", ValueSelector = (metadata, _) => metadata.SampleRate, IsDefault = false },
            new CsvExportField { Key = "bitDepth", Label = "Bit Depth", ValueSelector = (metadata, _) => metadata.BitDepth, IsDefault = false },
            new CsvExportField { Key = "generator", Label = "Studio Version", ValueSelector = (metadata, _) => SongGeneratorDisplay.ToFriendlyDisplay(metadata.Generator), IsDefault = false },
            new CsvExportField { Key = "songNotes", Label = "Song Notes", ValueSelector = (metadata, _) => metadata.NotesText, IsDefault = false },
            new CsvExportField { Key = "groups", Label = "Groups", ValueSelector = (metadata, _) => BuildGroupsCsvValue(metadata.Groups), IsDefault = false },
            new CsvExportField { Key = "mixerMain", Label = "Mixer Main", ValueSelector = (metadata, _) => BuildMixerMainCsvValue(metadata.MixerMainChannels), IsDefault = false },
            new CsvExportField { Key = "mixerInserts", Label = "Mixer Inserts", ValueSelector = (metadata, _) => BuildMixerInsertCsvValue(metadata.MixerInserts, metadata.MixerMainChannels), IsDefault = false },
            new CsvExportField { Key = "mixerSends", Label = "Mixer Sends", ValueSelector = (metadata, _) => BuildMixerSendCsvValue(metadata.MixerSends), IsDefault = false },
            new CsvExportField { Key = "comment", Label = "Comment", ValueSelector = (metadata, _) => metadata.Comment, IsDefault = true },
            new CsvExportField { Key = "path", Label = "Path", ValueSelector = (metadata, _) => metadata.Path, IsDefault = false },
            new CsvExportField { Key = "matchField", Label = "Match Field", ValueSelector = (_, match) => match?.MatchField, IsDefault = false },
            new CsvExportField { Key = "matchValue", Label = "Match Value", ValueSelector = (_, match) => match?.MatchValue, IsDefault = false }
        ];
    }

    private static IReadOnlyList<SongGridColumnField> BuildSongGridColumnFields()
    {
        return
        [
            new SongGridColumnField { Key = "song", Label = "Song", ColumnName = "Song", Width = 240, IsDefault = true, ValueSelector = (metadata, _) => metadata.FileName },
            new SongGridColumnField { Key = "title", Label = "Title", ColumnName = "Title", Width = 190, IsDefault = true, ValueSelector = (metadata, _) => metadata.Title },
            new SongGridColumnField { Key = "artist", Label = "Artist", ColumnName = "Artist", Width = 150, IsDefault = true, ValueSelector = (metadata, _) => metadata.Artist },
            new SongGridColumnField { Key = "year", Label = "Year", ColumnName = "Year", Width = 90, IsDefault = true, ValueSelector = (metadata, _) => metadata.Year },
            new SongGridColumnField { Key = "dateCreated", Label = "Date Created", ColumnName = "DateCreated", Width = 150, IsDefault = false, ValueSelector = (metadata, _) => metadata.DateCreated },
            new SongGridColumnField { Key = "lastModified", Label = "Last Modified", ColumnName = "LastModified", Width = 150, IsDefault = true, ValueSelector = (metadata, _) => metadata.LastModified },
            new SongGridColumnField { Key = "tempo", Label = "Tempo", ColumnName = "Tempo", Width = 90, IsDefault = true, ValueSelector = (metadata, _) => metadata.Tempo },
            new SongGridColumnField { Key = "keySignature", Label = "Key Signature", ColumnName = "KeySignature", Width = 120, IsDefault = true, ValueSelector = (metadata, _) => metadata.KeySignature },
            new SongGridColumnField { Key = "timeSignature", Label = "Time Signature", ColumnName = "TimeSignature", Width = 120, IsDefault = false, ValueSelector = (metadata, _) => metadata.TimeSignature },
            new SongGridColumnField { Key = "trackCount", Label = "Track Count", ColumnName = "TrackCount", Width = 100, IsDefault = false, ValueSelector = (metadata, _) => metadata.TrackCount },
            new SongGridColumnField { Key = "length", Label = "Length", ColumnName = "Length", Width = 90, IsDefault = true, ValueSelector = (metadata, _) => metadata.Length },
            new SongGridColumnField { Key = "sampleRate", Label = "Sample Rate", ColumnName = "SampleRate", Width = 110, IsDefault = false, ValueSelector = (metadata, _) => metadata.SampleRate },
            new SongGridColumnField { Key = "bitDepth", Label = "Bit Depth", ColumnName = "BitDepth", Width = 100, IsDefault = false, ValueSelector = (metadata, _) => metadata.BitDepth },
            new SongGridColumnField { Key = "generator", Label = "Studio Version", ColumnName = "Generator", Width = 180, IsDefault = false, ValueSelector = (metadata, _) => SongGeneratorDisplay.ToFriendlyDisplay(metadata.Generator) },
            new SongGridColumnField { Key = "formatVersion", Label = "Format Version", ColumnName = "FormatVersion", Width = 120, IsDefault = false, ValueSelector = (metadata, _) => metadata.FormatVersion },
            new SongGridColumnField { Key = "songNotes", Label = "Song Notes", ColumnName = "SongNotes", Width = 260, IsDefault = false, ValueSelector = (metadata, _) => metadata.NotesText },
            new SongGridColumnField { Key = "comment", Label = "Comment", ColumnName = "Comment", Width = 220, IsDefault = true, ValueSelector = (metadata, _) => metadata.Comment },
            new SongGridColumnField { Key = "path", Label = "Path", ColumnName = "Path", Width = 340, IsDefault = false, ValueSelector = (metadata, _) => metadata.Path }
        ];
    }

    private static string SanitizeFileNamePart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "export";
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim())
        {
            if (invalidCharacters.Contains(character))
            {
                continue;
            }

            builder.Append(char.IsWhiteSpace(character) ? '-' : character);
        }

        return builder.Length == 0 ? "export" : builder.ToString();
    }

    private static void AppendCsvRow(StringBuilder builder, params string?[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(EscapeCsv(values[index]));
        }

        builder.AppendLine();
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;
        var mustQuote =
            text.Contains(',') ||
            text.Contains('"') ||
            text.Contains('\r') ||
            text.Contains('\n');

        if (!mustQuote)
        {
            return text;
        }

        return $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private void SaveSnapshot(SongMetadata metadata, SnapshotSectionSelection selection, SnapshotFormat format)
    {
        var isJson = format == SnapshotFormat.Json;
        using var dialog = new SaveFileDialog
        {
            Title = "Save snapshot",
            Filter = isJson
                ? "JSON files (*.json)|*.json|All files (*.*)|*.*"
                : "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = isJson ? "json" : "txt",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = BuildDefaultSnapshotFileName(metadata, format)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        try
        {
            var contents = isJson
                ? BuildSnapshotJson(metadata, selection)
                : BuildSnapshotPreviewText(metadata, selection);

            File.WriteAllText(dialog.FileName, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            SetStatus($"Saved snapshot to {dialog.FileName}");
        }
        catch (Exception ex)
        {
            using var messageDialog = new ThemedMessageForm(Text, $"Could not save snapshot:\n{ex.Message}", _theme, ThemedMessageKind.Error);
            messageDialog.ShowDialog(this);
            SetStatus($"Snapshot save failed: {ex.Message}");
        }
    }

    private static string BuildDefaultSnapshotFileName(SongMetadata metadata, SnapshotFormat format)
    {
        var extension = format == SnapshotFormat.Json ? "json" : "txt";
        return $"{SanitizeFileNamePart(Path.GetFileNameWithoutExtension(metadata.FileName))}-snapshot-{DateTime.Now:yyyyMMdd-HHmmss}.{extension}";
    }

    private static (string Col1, string Col2) SplitPresetColumns(string preset)
    {
        var normalized = preset.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        const string channelsPrefix = "Channels\\";
        if (normalized.StartsWith(channelsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[channelsPrefix.Length..];
        }

        var separatorIndex = normalized.IndexOf(Path.DirectorySeparatorChar);
        if (separatorIndex < 0)
        {
            return (normalized, "");
        }

        return (
            normalized[..separatorIndex],
            normalized[(separatorIndex + 1)..]
        );
    }

    private static string FormatMixerInsertSnapshotLine(MixerInsertInfo insert)
    {
        var plugin = FormatField(insert.PluginName);
        var preset = FormatField(insert.PresetName);
        var line = $"  {insert.SlotName,-10} {plugin,-22}";
        return string.IsNullOrWhiteSpace(preset)
            ? line.TrimEnd()
            : $"{line} Preset: {preset}";
    }

    private static string FormatMixerSendSnapshotLine(MixerSendInfo send)
    {
        var destination = FormatField(send.DestinationName);
        var preset = FormatField(send.PresetName);
        var line = $"  {send.SlotName,-10} -> {destination,-22}";
        return string.IsNullOrWhiteSpace(preset)
            ? line.TrimEnd()
            : $"{line} Preset: {preset}";
    }

    private static string FormatMixerMainSnapshotLine(MixerMainInfo mainChannel)
    {
        var lines = new List<string> { $"  {mainChannel.ChannelName}" };
        if (!string.IsNullOrWhiteSpace(mainChannel.PrePluginChain))
        {
            lines.Add($"    Pre: {mainChannel.PrePluginChain}");
        }

        if (!string.IsNullOrWhiteSpace(mainChannel.PostPluginChain))
        {
            lines.Add($"    Post: {mainChannel.PostPluginChain}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string? BuildMixerMainCsvValue(IReadOnlyList<MixerMainInfo> mainChannels)
    {
        if (mainChannels.Count == 0)
        {
            return null;
        }

        return string.Join(
            "; ",
            mainChannels
                .OrderBy(channel => channel.ChannelName, StringComparer.CurrentCultureIgnoreCase)
                .Select(channel =>
                {
                    var parts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(channel.PrePluginChain))
                    {
                        parts.Add($"Pre: {channel.PrePluginChain}");
                    }

                    if (!string.IsNullOrWhiteSpace(channel.PostPluginChain))
                    {
                        parts.Add($"Post: {channel.PostPluginChain}");
                    }

                    return parts.Count == 0
                        ? channel.ChannelName
                        : $"{channel.ChannelName}: {string.Join(" | ", parts)}";
                }));
    }

    private static string? BuildGroupsCsvValue(IReadOnlyList<SongGroupInfo> groups)
    {
        if (groups.Count == 0)
        {
            return null;
        }

        return string.Join(
            "; ",
            groups
                .OrderBy(group => group.GroupName, StringComparer.CurrentCultureIgnoreCase)
                .Select(group => $"{group.GroupName}: {string.Join(" | ", group.TrackNames)}"));
    }

    private static string? BuildMixerInsertCsvValue(IReadOnlyList<MixerInsertInfo> inserts, IReadOnlyList<MixerMainInfo> mainChannels)
    {
        if (inserts.Count == 0)
        {
            return null;
        }

        var mainChannelNames = mainChannels
            .Select(channel => channel.ChannelName)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        var filteredInserts = inserts
            .Where(insert => !mainChannelNames.Contains(insert.ChannelName))
            .ToArray();
        if (filteredInserts.Length == 0)
        {
            return null;
        }

        return string.Join(
            "; ",
            filteredInserts
                .GroupBy(insert => insert.ChannelName, StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase)
                .Select(group =>
                {
                    var entries = group
                        .OrderBy(item => item.SlotName, StringComparer.CurrentCultureIgnoreCase)
                        .Select(insert =>
                        {
                            var plugin = FormatField(insert.PluginName);
                            var preset = FormatField(insert.PresetName);
                            return string.IsNullOrWhiteSpace(preset)
                                ? $"{insert.SlotName} {plugin}".Trim()
                                : $"{insert.SlotName} {plugin} ({preset})".Trim();
                        });

                    return $"{group.Key}: {string.Join("; ", entries)}";
                }));
    }

    private static string? BuildMixerSendCsvValue(IReadOnlyList<MixerSendInfo> sends)
    {
        if (sends.Count == 0)
        {
            return null;
        }

        return string.Join(
            "; ",
            sends
                .GroupBy(send => send.ChannelName, StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase)
                .Select(group =>
                {
                    var entries = group
                        .OrderBy(item => item.SlotName, StringComparer.CurrentCultureIgnoreCase)
                        .Select(send =>
                        {
                            var destination = FormatField(send.DestinationName);
                            var preset = FormatField(send.PresetName);
                            return string.IsNullOrWhiteSpace(preset)
                                ? $"{send.SlotName} -> {destination}".Trim()
                                : $"{send.SlotName} -> {destination} ({preset})".Trim();
                        });

                    return $"{group.Key}: {string.Join("; ", entries)}";
                }));
    }

    private static void AppendTrackSnapshotSection(
        StringBuilder builder,
        IReadOnlyList<TrackInstrumentInfo> tracks,
        bool withEventsOnly)
    {
        var heading = withEventsOnly ? "Tracks With Events" : "Tracks";
        builder.AppendLine(heading);
        builder.AppendLine(new string('-', heading.Length));
        if (tracks.Count == 0)
        {
            builder.AppendLine("No track data.");
            return;
        }

        var wroteTrack = false;
        foreach (var track in tracks.OrderBy(item => item.TrackName, StringComparer.CurrentCultureIgnoreCase))
        {
            if (wroteTrack)
            {
                builder.AppendLine();
            }

            builder.AppendLine(track.TrackName);
            if (!string.IsNullOrWhiteSpace(track.InstrumentName))
            {
                builder.Append("  Instrument: ");
                builder.AppendLine(track.InstrumentName);
            }

            if (!string.IsNullOrWhiteSpace(track.TrackNote))
            {
                builder.Append("  Note: ");
                builder.AppendLine(track.TrackNote);
            }

            wroteTrack = true;
        }
    }

    private static void AppendGroupSnapshotSection(StringBuilder builder, IReadOnlyList<SongGroupInfo> groups)
    {
        builder.AppendLine("Groups");
        builder.AppendLine("------");
        if (groups.Count == 0)
        {
            builder.AppendLine("No group data.");
            return;
        }

        foreach (var group in groups.OrderBy(item => item.GroupName, StringComparer.CurrentCultureIgnoreCase))
        {
            builder.Append(group.GroupName);
            builder.Append(": ");
            builder.AppendLine(string.Join(" | ", group.TrackNames));
        }
    }

    private static void AppendPresetSnapshotSection(StringBuilder builder, IReadOnlyList<string> presets)
    {
        builder.AppendLine("Presets");
        builder.AppendLine("-------");
        if (presets.Count == 0)
        {
            builder.AppendLine("No preset data.");
            return;
        }

        var presetGroups = presets
            .Select(SplitPresetColumns)
            .GroupBy(preset => preset.Col1, StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase);

        var wroteGroup = false;
        foreach (var presetGroup in presetGroups)
        {
            if (wroteGroup)
            {
                builder.AppendLine();
            }

            builder.AppendLine(presetGroup.Key);
            foreach (var preset in presetGroup.OrderBy(item => item.Col2, StringComparer.CurrentCultureIgnoreCase))
            {
                builder.Append("  ");
                builder.AppendLine(preset.Col2);
            }

            wroteGroup = true;
        }
    }

    private static void AppendMixerInsertSnapshotSection(StringBuilder builder, IReadOnlyList<MixerInsertInfo> inserts)
    {
        builder.AppendLine("Mixer Inserts");
        builder.AppendLine("------------");
        if (inserts.Count == 0)
        {
            builder.AppendLine("No mixer insert data.");
            return;
        }

        var channelGroups = inserts
            .GroupBy(insert => insert.ChannelName, StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase);

        var wroteGroup = false;
        foreach (var channelGroup in channelGroups)
        {
            if (wroteGroup)
            {
                builder.AppendLine();
            }

            builder.AppendLine(channelGroup.Key);
            foreach (var insert in channelGroup.OrderBy(item => item.SlotName, StringComparer.CurrentCultureIgnoreCase))
            {
                builder.AppendLine(FormatMixerInsertSnapshotLine(insert));
            }

            wroteGroup = true;
        }
    }

    private static void AppendMixerMainSnapshotSection(StringBuilder builder, IReadOnlyList<MixerMainInfo> mainChannels)
    {
        builder.AppendLine("Mixer Main");
        builder.AppendLine("----------");
        if (mainChannels.Count == 0)
        {
            builder.AppendLine("No mixer main data.");
            return;
        }

        foreach (var mainChannel in mainChannels.OrderBy(item => item.ChannelName, StringComparer.CurrentCultureIgnoreCase))
        {
            builder.AppendLine(FormatMixerMainSnapshotLine(mainChannel));
        }
    }

    private static void AppendMixerSendSnapshotSection(StringBuilder builder, IReadOnlyList<MixerSendInfo> sends)
    {
        builder.AppendLine("Mixer Sends");
        builder.AppendLine("-----------");
        if (sends.Count == 0)
        {
            builder.AppendLine("No mixer send data.");
            return;
        }

        var channelGroups = sends
            .GroupBy(send => send.ChannelName, StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.CurrentCultureIgnoreCase);

        var wroteGroup = false;
        foreach (var channelGroup in channelGroups)
        {
            if (wroteGroup)
            {
                builder.AppendLine();
            }

            builder.AppendLine(channelGroup.Key);
            foreach (var send in channelGroup.OrderBy(item => item.SlotName, StringComparer.CurrentCultureIgnoreCase))
            {
                builder.AppendLine(FormatMixerSendSnapshotLine(send));
            }

            wroteGroup = true;
        }
    }

    private static IReadOnlyList<MixerInsertInfo> GetSnapshotMixerInserts(SongMetadata metadata)
    {
        var mainChannelNames = metadata.MixerMainChannels
            .Select(channel => channel.ChannelName)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        return metadata.MixerInserts
            .Where(insert => !mainChannelNames.Contains(insert.ChannelName))
            .ToArray();
    }

    private IReadOnlyList<TrackInstrumentInfo> GetSnapshotTracks(SongMetadata metadata)
    {
        return _showTracksWithEventsOnly
            ? metadata.TrackInstruments.Where(track => track.HasEvents).ToArray()
            : metadata.TrackInstruments;
    }

    private SongSnapshot BuildSnapshot(SongMetadata metadata, SnapshotSectionSelection selection)
    {
        return new SongSnapshot
        {
            CapturedAt = DateTime.Now.ToString(),
            Song = new SongSnapshotSong
            {
                FileName = metadata.FileName,
                Title = metadata.Title,
                Artist = metadata.Artist,
                Path = metadata.Path
            },
            Sections = new SongSnapshotSections
            {
                Summary = selection.IncludeSummary ? BuildSnapshotSummary(metadata) : null,
                Attributes = selection.IncludeAttributes
                    ? BuildSnapshotAttributes(metadata)
                    : null,
                Tracks = selection.IncludeTracks
                    ? GetSnapshotTracks(metadata).Select(track => new SongSnapshotTrack
                    {
                        TrackName = track.TrackName,
                        InstrumentName = track.InstrumentName,
                        TrackNote = track.TrackNote
                    }).ToList()
                    : null,
                Groups = selection.IncludeGroups
                    ? metadata.Groups.Select(group => new SongSnapshotGroup
                    {
                        GroupName = group.GroupName,
                        TrackNames = group.TrackNames.ToList()
                    }).ToList()
                    : null,
                Mixer = selection.IncludeMixer
                    ? new SongSnapshotMixer
                    {
                        Main = metadata.MixerMainChannels.Select(mainChannel => new SongSnapshotMixerMain
                        {
                            ChannelName = mainChannel.ChannelName,
                            Pre = mainChannel.PrePluginChain,
                            Post = mainChannel.PostPluginChain
                        }).ToList(),
                        Inserts = GetSnapshotMixerInserts(metadata).Select(insert => new SongSnapshotMixerInsert
                        {
                            ChannelName = insert.ChannelName,
                            SlotName = insert.SlotName,
                            PluginName = insert.PluginName,
                            PresetName = insert.PresetName
                        }).ToList(),
                        Sends = metadata.MixerSends.Select(send => new SongSnapshotMixerSend
                        {
                            ChannelName = send.ChannelName,
                            SlotName = send.SlotName,
                            DestinationName = send.DestinationName,
                            PresetName = send.PresetName
                        }).ToList()
                    }
                    : null,
                Presets = selection.IncludePresets
                    ? metadata.Presets.Select(preset =>
                    {
                        var presetParts = SplitPresetColumns(preset);
                        return new SongSnapshotPreset
                        {
                            Col1 = presetParts.Col1,
                            Col2 = presetParts.Col2
                        };
                    }).ToList()
                    : null,
                Notes = selection.IncludeNotes ? new SongSnapshotNotes { Text = metadata.NotesText } : null
            }
        };
    }

    private static Dictionary<string, string?> BuildSnapshotSummary(SongMetadata metadata)
    {
        return new Dictionary<string, string?>
        {
            ["Title"] = metadata.Title,
            ["Artist"] = metadata.Artist,
            ["Year"] = metadata.Year,
            ["Date Created"] = metadata.DateCreated,
            ["Last Modified"] = metadata.LastModified,
            ["Tempo"] = metadata.Tempo,
            ["Key Signature"] = metadata.KeySignature,
            ["Time Signature"] = metadata.TimeSignature,
            ["Length"] = metadata.Length,
            ["Track Count"] = metadata.TrackCount,
            ["Preset Count"] = metadata.Presets.Count.ToString(),
            ["Sample Rate"] = metadata.SampleRate,
            ["Bit Depth"] = metadata.BitDepth,
            ["Studio Version"] = SongGeneratorDisplay.ToFriendlyDisplay(metadata.Generator),
            ["Notes File"] = metadata.NotesFile,
            ["Comment"] = metadata.Comment,
            ["Path"] = metadata.Path
        };
    }

    private static Dictionary<string, string> BuildSnapshotAttributes(SongMetadata metadata)
    {
        return metadata.Attributes
            .Where(pair => ShouldIncludeRawAttribute(pair.Key))
            .OrderBy(pair => pair.Key)
            .ToDictionary(pair => FormatRawAttributeKey(pair.Key), pair => pair.Value);
    }

    private string BuildSnapshotJson(SongMetadata metadata, SnapshotSectionSelection selection)
    {
        var snapshot = BuildSnapshot(metadata, selection);
        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    private string BuildSnapshotPreviewText(SongMetadata metadata, SnapshotSectionSelection selection)
    {
        var builder = new StringBuilder();
        builder.AppendLine("SongLens Snapshot");
        builder.Append("Captured: ");
        builder.AppendLine(DateTime.Now.ToString());
        builder.AppendLine();
        builder.AppendLine("Song");
        builder.AppendLine("----");
        builder.Append("File: ");
        builder.AppendLine(metadata.FileName);
        builder.Append("Title: ");
        builder.AppendLine(FormatField(metadata.Title));
        builder.Append("Artist: ");
        builder.AppendLine(FormatField(metadata.Artist));
        builder.Append("Path: ");
        builder.AppendLine(metadata.Path);

        if (selection.IncludeSummary)
        {
            builder.AppendLine();
            builder.AppendLine("Summary");
            builder.AppendLine("-------");
            foreach (var pair in BuildSnapshotSummary(metadata))
            {
                builder.Append(pair.Key);
                builder.Append(": ");
                builder.AppendLine(FormatField(pair.Value));
            }
        }

        if (selection.IncludeAttributes)
        {
            builder.AppendLine();
            builder.AppendLine("Attributes");
            builder.AppendLine("----------");
            foreach (var pair in BuildSnapshotAttributes(metadata))
            {
                builder.Append(pair.Key);
                builder.Append(": ");
                builder.AppendLine(pair.Value);
            }
        }

        if (selection.IncludeTracks)
        {
            builder.AppendLine();
            AppendTrackSnapshotSection(builder, GetSnapshotTracks(metadata), _showTracksWithEventsOnly);
        }

        if (selection.IncludeGroups)
        {
            builder.AppendLine();
            AppendGroupSnapshotSection(builder, metadata.Groups);
        }

        if (selection.IncludePresets)
        {
            builder.AppendLine();
            AppendPresetSnapshotSection(builder, metadata.Presets);
        }

        if (selection.IncludeMixer)
        {
            builder.AppendLine();
            AppendMixerMainSnapshotSection(builder, metadata.MixerMainChannels);
            builder.AppendLine();
            AppendMixerInsertSnapshotSection(builder, GetSnapshotMixerInserts(metadata));
            builder.AppendLine();
            AppendMixerSendSnapshotSection(builder, metadata.MixerSends);
        }

        if (selection.IncludeNotes)
        {
            builder.AppendLine();
            builder.AppendLine("Notes");
            builder.AppendLine("-----");
            builder.AppendLine(string.IsNullOrWhiteSpace(metadata.NotesText) ? "No notes text." : metadata.NotesText);
        }

        return builder.ToString().TrimEnd();
    }

    private void AddSongRow(SongMetadata metadata, SearchResult? match)
    {
        var rowIndex = _songGrid.Rows.Add();
        var row = _songGrid.Rows[rowIndex];
        foreach (var field in _songGridColumnFields)
        {
            row.Cells[field.ColumnName].Value = FormatField(field.ValueSelector(metadata, match));
        }
        row.Tag = new SongGridRowData
        {
            Metadata = metadata,
            Match = match
        };
        UpdateSongGridHintVisibility();
    }

    private void ShowSongGridColumnsDialog()
    {
        using var dialog = new SongGridColumnsForm(_songGridColumnFields, _theme, BrowserConfigStore.LoadSongGridVisibleColumnKeys());
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedFields.Count == 0)
        {
            return;
        }

        var selectedKeys = dialog.SelectedFields.Select(field => field.Key).ToArray();
        BrowserConfigStore.SaveSongGridVisibleColumnKeys(selectedKeys);
        ApplySongGridColumnVisibility(selectedKeys);
        UpdateSongGridSortGlyphs();
    }

    private void ShowFontSizeDialog()
    {
        using var dialog = new FontSettingsForm(_fontPreferences, _theme);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var selectedPreferences = dialog.SelectedPreferences;
        if (AreFontPreferencesEqual(_fontPreferences, selectedPreferences))
        {
            return;
        }

        var previousMainUiFontSizePoints = _fontPreferences.MainUi;
        _fontPreferences = selectedPreferences;
        AppFontSettings.SavePreferences(_fontPreferences);
        ApplyFontSize(previousMainUiFontSizePoints);
        ApplyTheme(savePreference: false);
        SetStatus("Font sizes updated.");
    }

    private void ShowPreferencesDialog()
    {
        using var dialog = new PreferencesForm(
            _rootPath,
            _theme.Name,
            _lockCurrentDetailTab,
            _enableSongLaunch,
            BrowserConfigStore.HasExplicitEnableSongLaunchPreference(),
            _restoreFilterSessionOnStartup,
            _restoreAdvancedSearchSessionOnStartup,
            _fontPreferences,
            _theme);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!string.Equals(dialog.SelectedThemeName, _theme.Name, StringComparison.OrdinalIgnoreCase))
        {
            ChangeTheme(AppThemes.Resolve(dialog.SelectedThemeName));
        }

        if (_lockCurrentDetailTab != dialog.UseStickyTabs)
        {
            _lockCurrentTabMenuItem.Checked = dialog.UseStickyTabs;
            ToggleLockCurrentDetailTab();
        }

        if (_enableSongLaunch != dialog.EnableSongLaunch)
        {
            _enableSongLaunch = dialog.EnableSongLaunch;
            BrowserConfigStore.SaveEnableSongLaunch(_enableSongLaunch);
            SyncSongGridContextMenuAvailability();
        }

        if (_restoreFilterSessionOnStartup != dialog.RestoreFilterSessionOnStartup)
        {
            _restoreFilterSessionOnStartup = dialog.RestoreFilterSessionOnStartup;
            BrowserConfigStore.SaveRestoreFilterSessionOnStartup(_restoreFilterSessionOnStartup);
        }

        if (_restoreAdvancedSearchSessionOnStartup != dialog.RestoreAdvancedSearchSessionOnStartup)
        {
            _restoreAdvancedSearchSessionOnStartup = dialog.RestoreAdvancedSearchSessionOnStartup;
            BrowserConfigStore.SaveRestoreAdvancedSearchSessionOnStartup(_restoreAdvancedSearchSessionOnStartup);
            if (!_restoreAdvancedSearchSessionOnStartup)
            {
                BrowserConfigStore.SaveLastAdvancedSearchQuery(null);
            }
        }

        if (!AreFontPreferencesEqual(_fontPreferences, dialog.SelectedFontPreferences))
        {
            var previousMainUiFontSizePoints = _fontPreferences.MainUi;
            _fontPreferences = dialog.SelectedFontPreferences;
            AppFontSettings.SavePreferences(_fontPreferences);
            ApplyFontSize(previousMainUiFontSizePoints);
            ApplyTheme(savePreference: false);
        }

        if (!string.IsNullOrWhiteSpace(dialog.SelectedRootPath)
            && !string.Equals(Path.GetFullPath(dialog.SelectedRootPath), _rootPath, StringComparison.OrdinalIgnoreCase))
        {
            SetRootPath(dialog.SelectedRootPath);
        }
    }

    private static bool AreFontPreferencesEqual(AppFontPreferences left, AppFontPreferences right)
    {
        return left.MainUi == right.MainUi
            && left.FolderTree == right.FolderTree
            && left.SongGrid == right.SongGrid
            && left.DetailGrids == right.DetailGrids
            && left.NotesAndPreviewText == right.NotesAndPreviewText
            && left.Dialogs == right.Dialogs;
    }

    private void ApplySongGridColumnVisibility(IReadOnlyCollection<string> visibleKeys)
    {
        var selectedKeys = visibleKeys.Count == 0
            ? new HashSet<string>(_songGridColumnFields.Where(field => field.IsDefault).Select(field => field.Key), StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(visibleKeys, StringComparer.OrdinalIgnoreCase);

        var displayIndex = 0;
        foreach (var field in _songGridColumnFields)
        {
            if (_songGrid.Columns[field.ColumnName] is not DataGridViewColumn column)
            {
                continue;
            }

            column.Visible = selectedKeys.Contains(field.Key);
            if (column.Visible)
            {
                column.DisplayIndex = displayIndex++;
            }
        }

        UpdateSongGridSortGlyphs();
    }

    private void HandleSongGridSorted()
    {
        if (_songGrid.SortedColumn is not DataGridViewColumn sortedColumn || _songGrid.SortOrder == SortOrder.None)
        {
            return;
        }

        _songGridSortColumnName = sortedColumn.Name;
        _songGridSortDirection = _songGrid.SortOrder == SortOrder.Descending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        if (!_applyingSongGridSort)
        {
            UpdateSongGridSortGlyphs();
        }
    }

    private void HandleSongGridColumnHeaderClick(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= _songGrid.Columns.Count)
        {
            return;
        }

        var column = _songGrid.Columns[columnIndex];
        if (!column.Visible)
        {
            return;
        }

        if (string.Equals(_songGridSortColumnName, column.Name, StringComparison.OrdinalIgnoreCase))
        {
            _songGridSortDirection = _songGridSortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
        else
        {
            _songGridSortColumnName = column.Name;
            _songGridSortDirection = ListSortDirection.Ascending;
        }

        ApplySongGridSort();
    }

    private void ApplySongGridSort()
    {
        if (_songGrid.Rows.Count == 0)
        {
            UpdateSongGridSortGlyphs();
            return;
        }

        if (!_songGrid.Columns.Contains(_songGridSortColumnName))
        {
            _songGridSortColumnName = "Song";
            _songGridSortDirection = ListSortDirection.Ascending;
        }

        if (_songGrid.Columns[_songGridSortColumnName] is not DataGridViewColumn column || !column.Visible)
        {
            UpdateSongGridSortGlyphs();
            return;
        }

        _applyingSongGridSort = true;
        try
        {
            _songGrid.Sort(column, _songGridSortDirection);
        }
        finally
        {
            _applyingSongGridSort = false;
        }

        UpdateSongGridSortGlyphs();
    }

    private void UpdateSongGridSortGlyphs()
    {
        foreach (DataGridViewColumn column in _songGrid.Columns)
        {
            column.HeaderCell.SortGlyphDirection = SortOrder.None;
        }

        if (!_songGrid.Columns.Contains(_songGridSortColumnName))
        {
            return;
        }

        if (_songGrid.Columns[_songGridSortColumnName] is not DataGridViewColumn sortedColumn || !sortedColumn.Visible)
        {
            return;
        }

        sortedColumn.HeaderCell.SortGlyphDirection = _songGridSortDirection == ListSortDirection.Descending
            ? SortOrder.Descending
            : SortOrder.Ascending;
    }

    private void SelectSongRow(int rowIndex, bool trackRecent = true)
    {
        if (rowIndex < 0 || rowIndex >= _songGrid.Rows.Count)
        {
            return;
        }

        _pendingRecentTrackingForNextSelection = trackRecent;
        _songGrid.ClearSelection();
        var row = _songGrid.Rows[rowIndex];
        row.Selected = true;
        if (row.Tag is SongGridRowData)
        {
            _suppressHistoryTabSelection = true;
            try
            {
                _detailTabs.SelectedIndex = _lockCurrentDetailTab ? GetCurrentDetailTabIndex() : 0;
            }
            finally
            {
                _suppressHistoryTabSelection = false;
            }
        }
    }

    private void ShowMetadataDetails(SongMetadata metadata, SearchResult? match = null)
    {
        _selectedMetadata = metadata;
        UpdateSnapshotActionsAvailability();
        var shouldTrackRecentSong = true;
        if (_pendingRecentTrackingForNextSelection is bool pendingTrackRecent)
        {
            shouldTrackRecentSong = pendingTrackRecent;
            _pendingRecentTrackingForNextSelection = null;
        }

        if (shouldTrackRecentSong)
        {
            BrowserConfigStore.SaveRecentSongPath(metadata.Path);
            RefreshRecentlyViewedMenu();
        }
        UpdateLaunchActionLabels(metadata);
        ClearDetailViews();

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
        AddSummary("Preset Count", metadata.Presets.Count.ToString());
        AddSummary("Sample Rate", metadata.SampleRate);
        AddSummary("Bit Depth", metadata.BitDepth);
        AddSummary("Studio Version", SongGeneratorDisplay.ToFriendlyDisplay(metadata.Generator));
        AddSummary("Comment", metadata.Comment);
        AddSummary("Path", metadata.Path);

        foreach (var attribute in metadata.Attributes
                     .Where(pair => ShouldIncludeRawAttribute(pair.Key))
                     .OrderBy(pair => pair.Key))
        {
            _rawGrid.Rows.Add(FormatRawAttributeKey(attribute.Key), attribute.Value);
        }

        PopulateTrackGrid(metadata);

        foreach (var group in metadata.Groups)
        {
            _groupGrid.Rows.Add(group.GroupName, string.Join(" | ", group.TrackNames));
        }

        var mainChannelNames = metadata.MixerMainChannels
            .Select(channel => channel.ChannelName)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        foreach (var mainChannel in metadata.MixerMainChannels)
        {
            _mixerMainGrid.Rows.Add(
                mainChannel.ChannelName,
                FormatField(mainChannel.PrePluginChain),
                FormatField(mainChannel.PostPluginChain));
        }

        foreach (var insert in metadata.MixerInserts)
        {
            if (mainChannelNames.Contains(insert.ChannelName))
            {
                continue;
            }

            _mixerInsertGrid.Rows.Add(
                insert.ChannelName,
                insert.SlotName,
                FormatField(insert.PluginName),
                FormatField(insert.PresetName));
        }

        foreach (var send in metadata.MixerSends)
        {
            _mixerSendGrid.Rows.Add(
                send.ChannelName,
                send.SlotName,
                FormatField(send.DestinationName),
                FormatField(send.PresetName));
        }

        _notesTextBox.Text = string.IsNullOrWhiteSpace(metadata.NotesText)
            ? "No notes.txt content."
            : metadata.NotesText;

        // Notes search hits jump directly to the Notes tab so the matching text is visible.
        var notesTabIndex = _detailTabs.TabPages.IndexOf(_notesTab);
        var targetTabIndex = _lockCurrentDetailTab
            ? GetCurrentDetailTabIndex()
            : match?.MatchField == "Notes" && notesTabIndex >= 0
                ? notesTabIndex
                : 0;
        _detailTabs.SelectedIndex = targetTabIndex;
        _lastNonHistoryTabIndex = _detailTabs.SelectedIndex;
        UpdateMixerLayoutHeights();
        AutoSizeDetailColumns();
        SetStatus($"Selected {metadata.FileName}");
    }

    private int GetCurrentDetailTabIndex()
    {
        if (_detailTabs.SelectedIndex >= 0
            && _detailTabs.SelectedIndex < _detailTabs.TabPages.Count
            && _detailTabs.TabPages[_detailTabs.SelectedIndex] != _historyTab)
        {
            return _detailTabs.SelectedIndex;
        }

        return _lastNonHistoryTabIndex >= 0
            && _lastNonHistoryTabIndex < _detailTabs.TabPages.Count
            && _detailTabs.TabPages[_lastNonHistoryTabIndex] != _historyTab
            ? _lastNonHistoryTabIndex
            : 0;
    }

    private void ShowVisibleTabsDialog()
    {
        if (_detailTabDefinitions is null || _detailTabDefinitions.Count == 0)
        {
            return;
        }

        var tabOptions = _detailTabDefinitions
            .Select(definition => (definition.Key, definition.TabPage.Text))
            .ToArray();
        using var dialog = new DetailTabVisibilityForm(tabOptions, GetVisibleDetailTabKeys(), _theme);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _visibleDetailTabKeys = dialog.SelectedTabKeys.ToList();
        BrowserConfigStore.SaveDetailTabVisibleKeys(_visibleDetailTabKeys);
        ApplyDetailTabVisibility();
        SetStatus("Updated visible detail tabs.");
    }

    private void ToggleLockCurrentDetailTab()
    {
        _lockCurrentDetailTab = _lockCurrentTabMenuItem.Checked;
        BrowserConfigStore.SaveLockCurrentDetailTab(_lockCurrentDetailTab);
        if (_lockCurrentDetailTab)
        {
            BrowserConfigStore.SaveLastSelectedDetailTabIndex(GetCurrentDetailTabIndex());
        }
        UpdateStatusIndicators();
        SetStatus(_lockCurrentDetailTab
            ? "Current detail tab will stay selected while you move between songs."
            : "Detail tabs will switch automatically for each song.");
    }

    private void UpdateLaunchActionLabels(SongMetadata metadata)
    {
        var launchTargets = SongLaunchResolver.GetLaunchTargets(metadata);
        var primaryApplication = launchTargets.FirstOrDefault(SongHostApplication.Unknown);
        var secondaryApplication = launchTargets.Skip(1).FirstOrDefault(SongHostApplication.Unknown);

        var primaryActionText = primaryApplication == SongHostApplication.Unknown
            ? "Open in Compatible App"
            : $"Open in {SongLaunchResolver.GetApplicationName(primaryApplication)}";
        _contextOpenInRecommendedAppMenuItem.Text = primaryActionText;

        var hasAlternateLaunch = secondaryApplication != SongHostApplication.Unknown;
        var alternateActionText = hasAlternateLaunch
            ? $"Open in {SongLaunchResolver.GetApplicationName(secondaryApplication)}"
            : "Open in Alternate App";

        _contextOpenInAlternateAppMenuItem.Text = alternateActionText;
        _contextOpenInRecommendedAppMenuItem.Visible = _enableSongLaunch;
        _contextOpenInAlternateAppMenuItem.Visible = _enableSongLaunch && hasAlternateLaunch;
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

    private void OpenSelectedSongInRecommendedApp()
    {
        if (_selectedMetadata is null)
        {
            using var messageDialog = new ThemedMessageForm(Text, "Select a song before opening it in a compatible app.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        OpenSongInRecommendedApp(_selectedMetadata);
    }

    private void OpenSelectedSongInAlternateApp()
    {
        if (_selectedMetadata is null)
        {
            using var messageDialog = new ThemedMessageForm(Text, "Select a song before opening it in a compatible app.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        var alternateApplication = SongLaunchResolver.GetLaunchTargets(_selectedMetadata)
            .Skip(1)
            .FirstOrDefault(SongHostApplication.Unknown);
        if (alternateApplication == SongHostApplication.Unknown)
        {
            using var messageDialog = new ThemedMessageForm(Text, "No alternate launch target is available for this song.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        OpenSongInSpecificApp(_selectedMetadata, alternateApplication);
    }

    private void RenameSelectedSong()
    {
        if (_selectedMetadata is null)
        {
            using var messageDialog = new ThemedMessageForm(Text, "Select a song before renaming it.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        var currentFileNameWithoutExtension = Path.GetFileNameWithoutExtension(_selectedMetadata.FileName);
        using var dialog = new RenameSongForm(currentFileNameWithoutExtension, _theme);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var renamedFileName = $"{dialog.SongFileNameWithoutExtension}.song";
        var renamedPath = Path.Combine(_selectedMetadata.Folder, renamedFileName);
        if (string.Equals(renamedPath, _selectedMetadata.Path, StringComparison.OrdinalIgnoreCase))
        {
            SetStatus($"The song is already named {renamedFileName}.");
            return;
        }

        if (File.Exists(renamedPath))
        {
            using var messageDialog = new ThemedMessageForm(
                Text,
                $"A song with that name already exists.\n\n{renamedFileName}",
                _theme,
                ThemedMessageKind.Warning);
            messageDialog.ShowDialog(this);
            return;
        }

        try
        {
            File.Move(_selectedMetadata.Path, renamedPath);
        }
        catch (Exception ex)
        {
            using var messageDialog = new ThemedMessageForm(
                Text,
                $"Could not rename the selected song.\n\n{ex.Message}",
                _theme,
                ThemedMessageKind.Error);
            messageDialog.ShowDialog(this);
            SetStatus($"Rename failed: {ex.Message}");
            return;
        }

        var isVisibleAfterRename = ReloadCurrentSongView(renamedPath);
        SetStatus(isVisibleAfterRename
            ? $"Renamed song to {renamedFileName}."
            : $"Renamed song to {renamedFileName}. It is not visible in the current view.");
    }

    private void RevealSelectedSongInExplorer()
    {
        if (_selectedMetadata is null)
        {
            using var messageDialog = new ThemedMessageForm(Text, "Select a song before revealing it in Explorer.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        OpenSongInExplorer(_selectedMetadata);
    }

    private bool ReloadCurrentSongView(string? preferredSongPath = null)
    {
        if (_advancedSearchQuery is not null)
        {
            RunAdvancedSearch(_advancedSearchQuery);
        }
        else if (_searchMode)
        {
            SearchSongs();
        }
        else if (_allSongsMode)
        {
            LoadAllSongs();
        }
        else if (_folderTree.SelectedNode?.Tag is string folderPath)
        {
            LoadSongsForFolder(folderPath);
        }

        return string.IsNullOrWhiteSpace(preferredSongPath) || TrySelectSongRowByPath(preferredSongPath);
    }

    private bool TrySelectSongRowByPath(string songPath)
    {
        for (var rowIndex = 0; rowIndex < _songGrid.Rows.Count; rowIndex++)
        {
            if (_songGrid.Rows[rowIndex].Tag is not SongGridRowData rowData)
            {
                continue;
            }

            if (!string.Equals(rowData.Metadata.Path, songPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            SelectSongRow(rowIndex);
            return true;
        }

        return false;
    }

    private void OpenSongInRecommendedApp(SongMetadata metadata)
    {
        var primaryApplication = SongLaunchResolver.GetLaunchTargets(metadata)
            .FirstOrDefault(SongHostApplication.Unknown);
        if (primaryApplication == SongHostApplication.Unknown)
        {
            var generatorLabel = SongGeneratorDisplay.ToFriendlyDisplay(metadata.Generator) ?? "Unknown";
            using var messageDialog = new ThemedMessageForm(
                Text,
                $"SongLens could not determine which app should open this song.\n\nSaved In: {generatorLabel}",
                _theme,
                ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            SetStatus($"No launch mapping for {generatorLabel}");
            return;
        }

        OpenSongInSpecificApp(metadata, primaryApplication);
    }

    private void OpenSongInSpecificApp(SongMetadata metadata, SongHostApplication application)
    {
        if (!File.Exists(metadata.Path))
        {
            using var messageDialog = new ThemedMessageForm(Text, $"Song file not found:\n{metadata.Path}", _theme, ThemedMessageKind.Warning);
            messageDialog.ShowDialog(this);
            SetStatus($"File not found: {metadata.Path}");
            return;
        }

        var launchPlan = SongLaunchResolver.CreatePlan(application);

        var executablePath = launchPlan.ExecutablePath;
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            executablePath = PromptForLaunchExecutable(launchPlan);
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                SetStatus($"Launch canceled for {metadata.FileName}");
                return;
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = $"\"{metadata.Path}\"",
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = true
            });
            SetStatus($"Opening {metadata.FileName} in {launchPlan.ApplicationName}");
        }
        catch (Exception ex)
        {
            using var messageDialog = new ThemedMessageForm(
                Text,
                $"Could not launch {launchPlan.ApplicationName}.\n\n{ex.Message}",
                _theme,
                ThemedMessageKind.Error);
            messageDialog.ShowDialog(this);
            SetStatus($"Launch failed: {ex.Message}");
        }
    }

    private string? PromptForLaunchExecutable(SongLaunchPlan launchPlan)
    {
        using var dialog = new OpenFileDialog
        {
            Title = $"Locate {launchPlan.ApplicationName}",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false,
            FileName = SongLaunchResolver.GetExecutableFileName(launchPlan.Application)
        };

        var initialDirectory = launchPlan.Application switch
        {
            SongHostApplication.StudioOne7 => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PreSonus", "Studio One 7"),
            SongHostApplication.FenderStudioPro8 => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Fender", "Studio Pro 8"),
            _ => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };
        if (Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return null;
        }

        SongLaunchResolver.SaveExecutablePath(launchPlan.Application, dialog.FileName);
        return dialog.FileName;
    }

    private void ShowSongAgeFilterDialog()
    {
        var filterPreference = BrowserConfigStore.LoadSongAgeFilterPreference() ?? _songAgeFilter;
        // _allSongsMode describes the current grid scope and becomes false when a
        // folder is selected. The dialog selection must reflect the age filter:
        // no age filter means View All Songs, even while viewing one folder.
        var viewAllSongs = _songAgeFilter is null;
        var displayResultsInSongGrid = _displayFilterResultsInSongGrid;

        while (true)
        {
            using var dialog = new SongAgeFilterForm(filterPreference, viewAllSongs, displayResultsInSongGrid, _theme);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (dialog.RequestedAction == SongAgeFilterDialogAction.CustomizeView)
            {
                ShowSongGridColumnsDialog();
                filterPreference = dialog.FilterPreference;
                viewAllSongs = dialog.ViewAllSongsSelected;
                displayResultsInSongGrid = dialog.DisplayResultsInSongGrid;
                continue;
            }

            filterPreference = dialog.FilterPreference;
            viewAllSongs = dialog.ViewAllSongsSelected;
            displayResultsInSongGrid = dialog.DisplayResultsInSongGrid;
            _songAgeFilter = dialog.SelectedFilter;
            _displayFilterResultsInSongGrid = displayResultsInSongGrid;
            break;
        }

        BrowserConfigStore.SaveSongAgeFilterPreference(filterPreference);
        BrowserConfigStore.SaveViewAllSongs(viewAllSongs);
        BrowserConfigStore.SaveDisplayFilterResultsInSongGrid(displayResultsInSongGrid);

        UpdateStatusIndicators();
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            SetStatus(viewAllSongs
                ? "Viewing all songs."
                : _songAgeFilter is null
                    ? "Song filter cleared."
                    : BuildSongFilterStatusMessage(_songAgeFilter));
            return;
        }

        SetRootPath(_rootPath);
        if (displayResultsInSongGrid)
        {
            LoadAllSongs();
            SetStatus(viewAllSongs ? "Viewing all songs." : BuildSongFilterStatusMessage(_songAgeFilter!));
            return;
        }

        if (_songAgeFilter is not null)
        {
            SetStatus(BuildSongFilterStatusMessage(_songAgeFilter));
        }
        else
        {
            SetStatus("Song filter cleared.");
        }
    }

    private void ShowAboutDialog()
    {
        using var dialog = new AboutForm(_theme);
        dialog.ShowDialog(this);
    }

    private void ShowAdvancedSearchDialog()
    {
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            using var messageDialog = new ThemedMessageForm(Text, "Choose a songs folder before using Advanced Search.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        using var dialog = new AdvancedSearchForm(_theme, _advancedSearchQuery ?? _lastAdvancedSearchQuery, _savedAdvancedSearches);
        var result = dialog.ShowDialog(this);
        _savedAdvancedSearches = dialog.SavedSearches.ToList();
        BrowserConfigStore.SaveSavedAdvancedSearches(_savedAdvancedSearches);
        if (dialog.ClearActiveSearchRequested && result != DialogResult.OK)
        {
            ClearAdvancedSearchResults();
        }

        if (result != DialogResult.OK || dialog.SearchQuery is null)
        {
            return;
        }

        _lastAdvancedSearchQuery = CloneAdvancedSearchQuery(dialog.SearchQuery);
        RunAdvancedSearch(dialog.SearchQuery);
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
            BrowserConfigStore.SaveLastSelectedDetailTabIndex(_lastNonHistoryTabIndex);
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

        var songDate = _songAgeFilter.Mode == SongAgeFilterMode.DateRange && _songAgeFilter.DateField == SongDateField.Created
            ? File.GetCreationTime(songPath)
            : File.GetLastWriteTime(songPath);
        if (_songAgeFilter.Mode == SongAgeFilterMode.DateRange
            && _songAgeFilter.StartDate is DateTime startDate
            && _songAgeFilter.EndDate is DateTime endDate)
        {
            var filterDate = songDate.Date;
            return filterDate >= startDate.Date && filterDate <= endDate.Date;
        }

        var cutoff = DateTime.Now.Subtract(TimeSpan.FromDays(_songAgeFilter.Days));
        return _songAgeFilter.Operator == SongAgeFilterOperator.OlderThan
            ? songDate <= cutoff
            : songDate >= cutoff;
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

    private bool FolderHasVisibleSongsDirectly(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path, "*.song")
                .Where(songPath => SongMetadataReader.IsRegularSongFile(Path.GetFileName(songPath)))
                .Any(SongMatchesCurrentView);
        }
        catch
        {
            return false;
        }
    }

    private bool RootHasSingleVisibleChoice(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(_rootPath) || string.IsNullOrWhiteSpace(folderPath))
        {
            return false;
        }

        var normalizedFolderPath = Path.GetFullPath(folderPath);
        if (!string.Equals(normalizedFolderPath, Path.GetFullPath(_rootPath), StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedFolderPath, FindFirstVisibleSongFolder(_rootPath), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var visibleChoiceCount = 0;

        try
        {
            visibleChoiceCount += Directory.EnumerateFiles(_rootPath, "*.song")
                .Where(songPath => SongMetadataReader.IsRegularSongFile(Path.GetFileName(songPath)))
                .Count(SongMatchesCurrentView);

            foreach (var childPath in Directory.EnumerateDirectories(_rootPath))
            {
                if (!DirectoryContainsVisibleSongs(childPath))
                {
                    continue;
                }

                visibleChoiceCount++;
                if (visibleChoiceCount > 1)
                {
                    return false;
                }
            }
        }
        catch
        {
            return false;
        }

        return visibleChoiceCount == 1;
    }

    private string? FindFirstVisibleSongFolder(string path)
    {
        if (FolderHasVisibleSongsDirectly(path))
        {
            return path;
        }

        try
        {
            foreach (var childPath in Directory.EnumerateDirectories(path).OrderBy(Path.GetFileName))
            {
                if (!DirectoryContainsVisibleSongs(childPath))
                {
                    continue;
                }

                if (FindFirstVisibleSongFolder(childPath) is { } match)
                {
                    return match;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
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
        AutoSizeGridColumns(_groupGrid, 180, GroupGridKey);
        AutoSizeGridColumns(_mixerMainGrid, 120, MixerMainGridKey);
        AutoSizeGridColumns(_mixerInsertGrid, 100, MixerInsertGridKey);
        AutoSizeGridColumns(_mixerSendGrid, 90, MixerSendGridKey);
    }

    private void AutoSizeGridColumns(DataGridView grid, int minimumWidth, string gridKey)
    {
        if (_customizedGridLayouts.Contains(gridKey) || BrowserConfigStore.HasSavedGridColumnWidths(gridKey))
        {
            return;
        }

        foreach (DataGridViewColumn column in grid.Columns)
        {
            if (IsResponsiveDetailGrid(grid) && column.Index == grid.Columns.Count - 1)
            {
                continue;
            }

            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            var width = column.Width;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            column.Width = Math.Max(width, minimumWidth);
        }

        UpdateResponsiveDetailGridLayout(grid);
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

    private void ScheduleFolderTreeSingleClickToggle(TreeNode node)
    {
        _pendingFolderTreeExpandNode = node;
        _folderTreeSingleClickTimer.Stop();
        _folderTreeSingleClickTimer.Interval = SystemInformation.DoubleClickTime;
        _folderTreeSingleClickTimer.Start();
    }

    private void CancelPendingFolderTreeSingleClick()
    {
        _folderTreeSingleClickTimer.Stop();
        _pendingFolderTreeExpandNode = null;
    }

    private void SetStatus(string message)
    {
        _statusLabel.Text = "";
    }

    private void ClearDetailViews()
    {
        _summaryGrid.Rows.Clear();
        _rawGrid.Rows.Clear();
        _trackGrid.Rows.Clear();
        _groupGrid.Rows.Clear();
        _mixerMainGrid.Rows.Clear();
        _mixerInsertGrid.Rows.Clear();
        _mixerSendGrid.Rows.Clear();
        _notesTextBox.Clear();
        UpdateMixerLayoutHeights();
    }

    private void PositionDetailHeaderControls()
    {
        if (_detailTabsHostPanel is null || _detailTabsHostPanel.IsDisposed)
        {
            return;
        }

        var snapshotY = Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - _snapshotButton.Height) / 2);
        var snapshotX = Math.Max(0, _songGridHeaderPanel.ClientSize.Width - _snapshotButton.Width - 8);
        _snapshotButton.Location = new Point(snapshotX, snapshotY);

        var columnsY = Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - _songGridColumnsButton.Height) / 2);
        var columnsX = Math.Max(0, snapshotX - _songGridColumnsButton.Width - 8);
        _songGridColumnsButton.Location = new Point(columnsX, columnsY);

        var tabHeaderHeight = Math.Max(_detailTabs.ItemSize.Height, _detailTabs.Font.Height + 8);
        var checkBoxY = Math.Max(0, (tabHeaderHeight - _tracksWithEventsCheckBox.Height) / 2);
        var checkBoxX = Math.Max(0, _detailTabsHostPanel.ClientSize.Width - _tracksWithEventsCheckBox.Width - 8);
        _tracksWithEventsCheckBox.Location = new Point(checkBoxX, checkBoxY);
    }

    private void UpdateSnapshotActionsAvailability()
    {
        var hasSelectedSong = _selectedMetadata is not null;
        var isAdvancedSearchResults = IsAdvancedSearchResultsView();
        var hasAdvancedSearchResults = isAdvancedSearchResults && _songGrid.Rows.Count > 0;
        _saveSnapshotMenuItem.Text = isAdvancedSearchResults ? "Save Results Snapshot..." : "Save Snapshot...";
        _saveSnapshotMenuItem.Enabled = hasSelectedSong || hasAdvancedSearchResults;
        _snapshotButton.Text = isAdvancedSearchResults ? "Snapshot Results" : "Snapshot";
        _snapshotButton.AccessibleName = _snapshotButton.Text;
        _snapshotButton.Enabled = hasSelectedSong || hasAdvancedSearchResults;
        _toolTip.SetToolTip(
            _snapshotButton,
            isAdvancedSearchResults ? "Preview the current advanced search results grid" : "View Song Snapshot");
        _tracksWithEventsCheckBox.Enabled = hasSelectedSong;
        PositionDetailHeaderControls();
    }

    private void PopulateTrackGrid(SongMetadata metadata)
    {
        var tracks = _showTracksWithEventsOnly
            ? metadata.TrackInstruments.Where(track => track.HasEvents).ToList()
            : metadata.TrackInstruments.ToList();

        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];
            _trackGrid.Rows.Add((i + 1).ToString(), track.TrackName, FormatField(track.InstrumentName), FormatField(track.TrackNote));
        }
    }

    private void ToggleTracksWithEventsOnly()
    {
        _showTracksWithEventsOnly = _tracksWithEventsCheckBox.Checked;
        BrowserConfigStore.SaveShowTracksWithEventsOnly(_showTracksWithEventsOnly);

        if (_selectedMetadata is not null)
        {
            _trackGrid.Rows.Clear();
            PopulateTrackGrid(_selectedMetadata);
            AutoSizeGridColumns(_trackGrid, 50, TrackGridKey);
        }

        SetStatus(_showTracksWithEventsOnly
            ? "Showing only tracks with assigned events."
            : "Showing all tracks.");
    }

    private static bool ShouldIncludeRawAttribute(string key)
    {
        if (key.StartsWith("Preset[", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedKey = StripRawAttributePrefix(key);
        return !string.Equals(normalizedKey, "FormatVersion", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedKey, "FrameType", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedKey, "MimeType", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedKey, "Notes", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedKey, "ArtistPage", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedKey, "Artwork", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedKey, "TimeFormat", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatRawAttributeKey(string key)
    {
        var normalizedKey = StripRawAttributePrefix(key);
        return string.Equals(normalizedKey, "Generator", StringComparison.OrdinalIgnoreCase)
            ? "Studio Version"
            : normalizedKey;
    }

    private static string StripRawAttributePrefix(string key)
    {
        const string documentPrefix = "Document:";
        const string mediaPrefix = "Media:";

        if (key.StartsWith(documentPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return key[documentPrefix.Length..];
        }

        if (key.StartsWith(mediaPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return key[mediaPrefix.Length..];
        }

        return key;
    }

    private void PersistSessionSettings()
    {
        BrowserConfigStore.SaveTheme(_theme.Name);
        if (!string.IsNullOrWhiteSpace(_rootPath))
        {
            BrowserConfigStore.SaveRootPath(_rootPath);
        }

        BrowserConfigStore.SaveLockCurrentDetailTab(_lockCurrentDetailTab);
        BrowserConfigStore.SaveLastSelectedDetailTabIndex(GetCurrentDetailTabIndex());
        BrowserConfigStore.SaveSongAgeFilterPreference(_songAgeFilter);
        BrowserConfigStore.SaveViewAllSongs(_songAgeFilter is null);
        BrowserConfigStore.SaveDisplayFilterResultsInSongGrid(_displayFilterResultsInSongGrid);
        BrowserConfigStore.SaveLastAdvancedSearchQuery(_lastAdvancedSearchQuery);
        var persistedWindowSize = WindowState == FormWindowState.Normal ? Size : RestoreBounds.Size;
        BrowserConfigStore.SaveMainWindowSize(persistedWindowSize);
    }

    private void UpdateStatusIndicators()
    {
        _filterStatusLabel.Text = $"Filter: {BuildFilterStatusText()}";
        _stickyTabsStatusLabel.Text = $"Sticky Tabs: {(_lockCurrentDetailTab ? "On" : "Off")}";
    }

    private string BuildFilterStatusText()
    {
        if (_songAgeFilter is null)
        {
            return _allSongsMode ? "All Songs" : "Off";
        }

        if (_songAgeFilter.Mode == SongAgeFilterMode.DateRange
            && _songAgeFilter.StartDate is DateTime startDate
            && _songAgeFilter.EndDate is DateTime endDate)
        {
            var dateFieldLabel = _songAgeFilter.DateField == SongDateField.Created ? "created" : "modified";
            return $"{dateFieldLabel} between {startDate:MMM d, yyyy} and {endDate:MMM d, yyyy}";
        }

        var cutoffDate = DateTime.Now.Subtract(TimeSpan.FromDays(_songAgeFilter.Days));
        var cutoffLabel = cutoffDate.ToString("MMM d, yyyy");
        var qualifier = _songAgeFilter.Operator == SongAgeFilterOperator.OlderThan
            ? $"before {cutoffLabel}"
            : $"on/after {cutoffLabel}";

        return $"{_songAgeFilter.OperatorText} {_songAgeFilter.Days} days ({qualifier})";
    }

    private static string BuildSongFilterStatusMessage(SongAgeFilter filter)
    {
        if (filter.Mode == SongAgeFilterMode.DateRange
            && filter.StartDate is DateTime startDate
            && filter.EndDate is DateTime endDate)
        {
            var dateFieldLabel = filter.DateField == SongDateField.Created ? "created" : "modified";
            return $"Viewing songs {dateFieldLabel} between {startDate:MMM d, yyyy} and {endDate:MMM d, yyyy}.";
        }

        return $"Viewing songs {filter.OperatorText} {filter.Days} days old.";
    }

    private void UpdateSongGridHintVisibility()
    {
        UpdateSongDetailSplitLayout();
    }

    private void UpdateSongDetailSplitLayout(bool force = false)
    {
        if (_songDetailSplit is null
            || _songDetailSplit.IsDisposed
            || _songDetailSplit.Height <= 0
            || _songDetailSplit.Panel1Collapsed
            || _songDetailSplit.Panel2Collapsed)
        {
            return;
        }

        if (!force && _songDetailSplitUserAdjusted)
        {
            return;
        }

        var expandForSingleSong = _songGrid.Rows.Count == 1;
        if (!force && _detailSplitExpandedForSingleSong == expandForSingleSong)
        {
            return;
        }

        var availableHeight = _songDetailSplit.Height - _songDetailSplit.SplitterWidth;
        var targetSongPanelHeight = (int)Math.Round(availableHeight * (expandForSingleSong ? 0.24 : 0.48));
        var maxSongPanelHeight = availableHeight - _songDetailSplit.Panel2MinSize;
        targetSongPanelHeight = Math.Max(_songDetailSplit.Panel1MinSize, Math.Min(targetSongPanelHeight, maxSongPanelHeight));

        _applyingSongDetailSplit = true;
        try
        {
            _songDetailSplit.SplitterDistance = targetSongPanelHeight;
            _detailSplitExpandedForSingleSong = expandForSingleSong;
        }
        finally
        {
            _applyingSongDetailSplit = false;
        }
    }

    private void UpdateMixerLayoutHeights()
    {
        if (_mixerTabLayout is null || _mixerTabLayout.RowStyles.Count == 0)
        {
            return;
        }

        _mixerTabLayout.RowStyles[0].Height = GetMixerMainSectionHeight();
    }

    private int GetMixerMainSectionHeight()
    {
        var labelHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.MainUi);
        var headerHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
        var rowHeight = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
        var visibleRowCount = Math.Max(1, Math.Min(3, _mixerMainGrid.Rows.Count));
        var contentHeight = labelHeight + headerHeight + (rowHeight * visibleRowCount) + 10;
        var minimumHeight = AppFontSettings.Scale(86, _fontPreferences, AppFontSection.MainUi);
        var maximumHeight = AppFontSettings.Scale(150, _fontPreferences, AppFontSection.MainUi);
        return Math.Max(minimumHeight, Math.Min(contentHeight, maximumHeight));
    }

    private void ClearAdvancedSearchResults()
    {
        if (_advancedSearchQuery is null && !_searchMode)
        {
            return;
        }

        _advancedSearchQuery = null;
        _lastAdvancedSearchQuery = null;
        _searchMode = false;
        _searchTextBox.Clear();

        if (_folderTree.SelectedNode?.Tag is string folderPath)
        {
            LoadSongsForFolder(folderPath);
            return;
        }

        _songGrid.Rows.Clear();
        ClearDetailViews();
        _selectedMetadata = null;
        UpdateSnapshotActionsAvailability();
        UpdateSongGridHintVisibility();
        SetStatus("Ready");
    }

    private static AdvancedSearchQuery CloneAdvancedSearchQuery(AdvancedSearchQuery query)
    {
        return new AdvancedSearchQuery
        {
            MatchMode = query.MatchMode,
            Rules = query.Rules
                .Select(rule => new AdvancedSearchRule
                {
                    FieldKey = rule.FieldKey,
                    Operator = rule.Operator,
                    ValueText = rule.ValueText,
                    NumberValue = rule.NumberValue,
                    DateValue = rule.DateValue
                })
                .ToArray()
        };
    }

}
