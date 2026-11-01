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
    private const string BulkSelectColumnName = "BulkSelect";
    private static readonly Color LightRatingStarColor = Color.FromArgb(31, 62, 96);
    private static readonly Color DarkRatingStarColor = Color.FromArgb(230, 176, 20);

    private sealed class RatingPickerControl : Control
    {
        private int _rating;
        private Color _filledStarColor = Color.FromArgb(230, 176, 20);
        private Color _emptyStarColor = SystemColors.GrayText;

        public int Rating
        {
            get => _rating;
            private set
            {
                var normalizedRating = Math.Clamp(value, 0, 5);
                if (_rating == normalizedRating) return;
                _rating = normalizedRating;
                Invalidate();
            }
        }

        public RatingPickerControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.Selectable
                | ControlStyles.UserPaint, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            AccessibleName = "Bulk rating";
            AccessibleDescription = "Choose a rating from zero to five stars.";
        }

        public void SetStarColors(Color filledStarColor, Color emptyStarColor)
        {
            _filledStarColor = filledStarColor;
            _emptyStarColor = emptyStarColor;
            Invalidate();
        }

        public void ClearRating()
        {
            Rating = 0;
        }

        protected override void OnPaint(PaintEventArgs args)
        {
            base.OnPaint(args);
            args.Graphics.Clear(BackColor);
            var starWidth = ClientSize.Width / 5f;
            using var starFont = new Font(Font.FontFamily, Font.Size + 2f, FontStyle.Regular, Font.Unit);
            for (var starIndex = 0; starIndex < 5; starIndex++)
            {
                var starBounds = Rectangle.Round(new RectangleF(
                    starIndex * starWidth,
                    0,
                    starWidth,
                    ClientSize.Height));
                TextRenderer.DrawText(
                    args.Graphics,
                    starIndex < Rating ? "\u2605" : "\u2606",
                    starFont,
                    starBounds,
                    starIndex < Rating ? _filledStarColor : _emptyStarColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }

            if (Focused)
                ControlPaint.DrawFocusRectangle(args.Graphics, ClientRectangle);
        }

        protected override void OnMouseDown(MouseEventArgs args)
        {
            base.OnMouseDown(args);
            if (args.Button != MouseButtons.Left) return;
            Focus();
            var clickedRating = Math.Clamp((args.X * 5 / Math.Max(1, ClientSize.Width)) + 1, 1, 5);
            Rating = Rating == clickedRating ? 0 : clickedRating;
        }

        protected override void OnKeyDown(KeyEventArgs args)
        {
            base.OnKeyDown(args);
            if (args.KeyCode is >= Keys.D1 and <= Keys.D5)
            {
                Rating = (int)args.KeyCode - (int)Keys.D0;
                args.Handled = true;
            }
            else if (args.KeyCode is Keys.D0 or Keys.Delete or Keys.Back)
            {
                Rating = 0;
                args.Handled = true;
            }
            else if (args.KeyCode is Keys.Left or Keys.Down)
            {
                Rating--;
                args.Handled = true;
            }
            else if (args.KeyCode is Keys.Right or Keys.Up)
            {
                Rating++;
                args.Handled = true;
            }
        }

        protected override void OnGotFocus(EventArgs args)
        {
            base.OnGotFocus(args);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs args)
        {
            base.OnLostFocus(args);
            Invalidate();
        }
    }

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

    private sealed class SongQueryRow
    {
        public required SongMetadata Metadata { get; init; }
        public required SongAnnotation Annotation { get; init; }
        public SearchResult? Match { get; init; }
    }

    private sealed class CachedSongMetadata
    {
        public required SongMetadata Metadata { get; init; }
        public required long FileLength { get; init; }
        public required DateTime LastWriteTimeUtc { get; init; }
        public long LastAccess { get; set; }
    }

    private sealed class MixerDetailExportRow
    {
        public required string SongPath { get; init; }
        public int? TrackNumber { get; init; }
        public required string?[] Values { get; init; }
    }

    private sealed class DetailTabDefinition
    {
        public required string Key { get; init; }
        public required TabPage TabPage { get; init; }
    }

    private enum SongViewHistoryKind
    {
        AllSongs,
        KeywordSearch,
        AdvancedSearch
    }

    private sealed class SongViewHistoryEntry
    {
        public required SongViewHistoryKind Kind { get; init; }
        public required string Key { get; init; }
        public required string Description { get; init; }
        public string? Keyword { get; init; }
        public AdvancedSearchQuery? AdvancedQuery { get; init; }
        public SongAgeFilter? DateFilter { get; init; }
        public string? SelectedSongPath { get; set; }
        public string? ReturnFolderPath { get; set; }
    }

    private readonly ComboBox _rootPathComboBox = new();
    private readonly Button _browseButton = new();
    private readonly Button _advancedSearchButton = new();
    private readonly Button _clearSearchButton = new();
    private readonly Button _viewBackButton = new();
    private readonly Button _viewForwardButton = new();
    private readonly Button _refreshButton = new();
    private readonly Button _recentlyViewedButton = new();
    private readonly Button _expandAllButton = new();
    private readonly Button _collapseAllButton = new();
    private readonly TextBox _searchTextBox = new();
    private readonly MenuStrip _menuStrip = new();
    private readonly ToolStripMenuItem _fileMenuItem = new("&File");
    private readonly ToolStripMenuItem _saveSnapshotMenuItem = new("&Save Snapshot...");
    private readonly ToolStripMenuItem _exportCsvMenuItem = new("Export &CSV...");
    private readonly ToolStripMenuItem _exportExcelMenuItem = new("Export &Excel Workbook...");
    private readonly ToolStripMenuItem _exitMenuItem = new("E&xit");
    private readonly ToolStripMenuItem _toolsMenuItem = new("&Tools");
    private readonly ToolStripMenuItem _advancedSearchMenuItem = new("&Advanced Search...");
    private readonly ToolStripMenuItem _preferencesMenuItem = new("&Preferences...");
    private readonly ToolStripMenuItem _lockCurrentTabMenuItem = new("Use Sticky Tabs");
    private readonly ToolStripMenuItem _changeFontSizeMenuItem = new("Change Font Sizes...");
    private readonly ToolStripMenuItem _helpMenuItem = new("&Help");
    private readonly ToolStripMenuItem _themeMenuItem = new("Theme");
    private readonly ToolStripMenuItem _darkThemeMenuItem = new("Dark");
    private readonly ToolStripMenuItem _lightThemeMenuItem = new("Light");
    private readonly ToolStripMenuItem _helpContentsMenuItem = new("SongLens &Help");
    private readonly ToolStripMenuItem _aboutMenuItem = new("&About SongLens");
    private readonly FolderTreeView _folderTree = new();
    private readonly ContextMenuStrip _folderTreeContextMenu = new();
    private readonly ToolStripMenuItem _contextExpandFolderMenuItem = new("Expand Folder");
    private readonly ToolStripMenuItem _contextCollapseFolderMenuItem = new("Collapse Folder");
    private readonly ToolStripMenuItem _contextRevealFolderInExplorerMenuItem = new("Reveal in Explorer");
    private readonly ToolStripMenuItem _contextExportFolderMenuItem = new("Export Folder");
    private readonly ToolStripMenuItem _contextExportFolderCsvMenuItem = new("CSV...");
    private readonly ToolStripMenuItem _contextExportFolderExcelMenuItem = new("Excel Workbook...");
    private readonly ToolStripMenuItem _contextDeleteFolderMenuItem = new("Delete Folder");
    private readonly ImageList _folderImages = new();
    private readonly DataGridView _songGrid = new();
    private readonly Panel _songGridHeaderPanel = new();
    private readonly ToolStrip _songGridHeaderToolStrip = new();
    private readonly Label _songGridHintLabel = new();
    private readonly Label _songGridTitleLabel = new();
    private readonly Dictionary<int, Font> _songGridTitleFonts = new();
    private readonly FlowLayoutPanel _songLensViewLoadingPanel = new();
    private readonly Label _songLensViewLoadingLabel = new();
    private readonly ProgressBar _songLensViewProgressBar = new();
    private readonly ContextMenuStrip _viewOptionsMenu = new();
    private readonly ContextMenuStrip _recentlyViewedMenu = new();
    private long _recentlyViewedMenuClosedOnButtonTick;
    private readonly ToolStripMenuItem _saveCurrentViewMenuItem = new("Save Current View");
    private readonly ToolStripMenuItem _savedViewsMenuItem = new("Saved Views");
    private readonly ToolStripMenuItem _manageSavedViewsMenuItem = new("Manage Grid Views...");
    private readonly ToolStripMenuItem _restoreDefaultViewMenuItem = new("Restore Default Layout");
    private readonly ToolStripMenuItem _viewColumnsMenuItem = new("Select Columns to Display...");
    private readonly ToolStripMenuItem _bulkEditMenuItem = new("Bulk Edit Songs...");
    private readonly FlowLayoutPanel _bulkEditBar = new();
    private readonly Label _bulkEditCountLabel = new();
    private readonly ComboBox _bulkStatusComboBox = new();
    private readonly ComboBox _bulkGenreComboBox = new();
    private readonly Button _bulkSelectAllButton = new() { Text = "All", AutoSize = true };
    private readonly Button _bulkClearButton = new() { Text = "Clear", AutoSize = true };
    private readonly Button _bulkSetStatusButton = new() { Text = "Set", AutoSize = true };
    private readonly Button _bulkSetGenreButton = new() { Text = "Set", AutoSize = true };
    private readonly Label _bulkRatingLabel = new() { Text = "Rating", AutoSize = true };
    private readonly RatingPickerControl _bulkRatingPicker = new();
    private readonly Button _bulkSetRatingButton = new() { Text = "Set", AutoSize = true };
    private readonly Button _bulkDeleteButton = new() { Text = "Delete", AutoSize = true };
    private readonly Button _bulkDoneButton = new() { Text = "Done", AutoSize = true };
    private readonly Button _filterByDateButton = new();
    private readonly Button _songGridColumnsButton = new();
    private readonly Button _statisticsButton = new();
    private readonly Button _songGridHelpButton = new();
    private readonly ContextMenuStrip _songGridContextMenu = new();
    private readonly ToolStripMenuItem _contextOpenInRecommendedAppMenuItem = new("Open in Recommended App");
    private readonly ToolStripMenuItem _contextOpenInAlternateAppMenuItem = new("Open in Alternate App");
    private readonly ToolStripMenuItem _contextPlaySongMenuItem = new("Listen to This Song");
    private readonly ToolStripSeparator _contextSongActionsSeparator = new();
    private readonly ToolStripMenuItem _contextRenameSongMenuItem = new("Rename Song...");
    private readonly ToolStripMenuItem _contextRevealInExplorerMenuItem = new("Reveal in Explorer");
    private readonly TabControl _detailTabs = new();
    private readonly Panel _trackFiltersPanel = new();
    private readonly ToolStrip _trackViewOptionsToolStrip = new();
    private readonly ToolStripDropDownButton _trackViewOptionsDropDownButton = new("Track View Options");
    private readonly BorderedCheckMenuItem _tracksWithEventsMenuItem = new("With Events") { CheckOnClick = true };
    private readonly BorderedCheckMenuItem _hideDisabledTracksMenuItem = new("Hide Disabled") { CheckOnClick = true };
    private readonly BorderedCheckMenuItem _hideFolderTracksMenuItem = new("Hide Folders") { CheckOnClick = true };
    private readonly Button _snapshotButton = new();
    private readonly TabPage _summaryTab = new("Summary");
    private readonly TabPage _pluginsTab = new("Plug-ins");
    private readonly TabPage _tracksTab = new("Tracks");
    private readonly TabPage _groupsTab = new("Groups");
    private readonly TabPage _historyTab = new("History");
    private readonly TabPage _mixerTab = new("Mixer");
    private readonly TabPage _notesTab = new("Notes");
    private readonly DataGridView _summaryGrid = new();
    private readonly DataGridView _pluginsGrid = new();
    private readonly DataGridView _trackGrid = new();
    private readonly DataGridView _groupGrid = new();
    private readonly DataGridView _mixerMainGrid = new();
    private readonly DataGridView _mixerInsertGrid = new();
    private readonly DataGridView _mixerSendGrid = new();
    private readonly Button _viewFullMixerButton = new();
    private readonly TextBox _notesTextBox = new();
    private readonly ToolStripStatusLabel _statusLabel = new() { TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _diagnosticLogLinkLabel = new()
    {
        Text = "View diagnostic log…",
        IsLink = true,
        LinkBehavior = LinkBehavior.HoverUnderline,
        Visible = false
    };
    private readonly ToolStripStatusLabel _statusSpacerLabel = new() { Spring = true };
    private readonly ToolStripStatusLabel _filterStatusLabel = new();
    private readonly ToolStripStatusLabel _stickyTabsStatusLabel = new();
    private readonly ToolTip _toolTip = new();
    private readonly IReadOnlyList<CsvExportField> _csvExportFields;
    private readonly List<SongGridColumnField> _songGridColumnFields;
    private List<CustomSongColumn> _customSongColumns;
    private List<string> _songStatuses;
    private List<string> _songGenres;
    private List<SavedSongLensView> _savedSongLensViews;
    private List<string> _visibleDetailTabKeys;
    private bool _showTracksWithEventsOnly;
    private bool _bulkEditMode;
    private bool _suppressBulkSelectionCount;
    private bool _hideDisabledTracks;
    private bool _hideFolderTracks;

    private string _rootPath = "";
    private string? _loadedFolderViewPath;
    private bool _enableSongLaunch;
    private SongGridDoubleClickAction _songGridDoubleClickAction;
    private bool _searchMode;
    private bool _allSongsMode;
    private const int SongViewHistoryLimit = 5;
    private readonly List<SongViewHistoryEntry> _songViewHistory = [];
    private int _songViewHistoryIndex = -1;
    private bool _applyingSongViewHistory;
    private bool _songViewHistoryDetached;
    private bool _suppressFastSearchTextChanged;
    private string? _keywordSearchReturnFolderPath;
    private AdvancedSearchQuery? _advancedSearchQuery;
    private AdvancedSearchQuery? _lastAdvancedSearchQuery;
    private List<SavedAdvancedSearch> _savedAdvancedSearches;
    private SongAgeFilter? _songAgeFilter;
    private SongMetadata? _selectedMetadata;
    private int _lastNonHistoryTabIndex;
    private bool _suppressHistoryTabSelection;
    private bool _suppressFolderTreeSelectionLoad;
    private bool _lockCurrentDetailTab;
    private bool _restoreFilterSessionOnStartup;
    private bool _restoreViewAllSongsOnStartup;
    private List<SongGridSortLevel> _songGridSortLevels = [];
    private bool _promptForRootOnFirstShow;
    private string? _startupRootPath;
    private int _songLensViewBuildDepth;
    private readonly Dictionary<string, bool> _folderVisibilityCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CachedSongMetadata> _songMetadataCache = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<SongMetadata>? _catalogStatisticsMetadataCache;
    private string? _catalogStatisticsCacheRoot;
    private long _songMetadataCacheAccess;
    private readonly HashSet<string> _customizedGridLayouts = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _folderTreeSingleClickTimer = new();
    private bool _suppressRootPathSelectionChange;
    private bool? _pendingRecentTrackingForNextSelection;
    private AppTheme _theme;
    private AppFontPreferences _fontPreferences;
    private bool _suspendGridWidthPersistence;
    private bool _suspendSongGridOrderPersistence;
    private bool _suppressSongAnnotationPersistence;
    private bool _songGridSortRefreshPending;
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
    private const string PlayableColumnName = "Playable";
    private const string SingleMixdownIndicator = "♪";
    private const string MultipleMixdownsIndicator = "♫";
    private const string StatusColumnName = "Status";
    private const string GenreColumnName = "Genre";
    private const string RatingColumnName = "Rating";
    private const string SummaryGridKey = "SummaryGrid";
    private const string PluginsGridKey = "PluginsGrid";
    private const string TrackGridKey = "TrackGrid";
    private const string GroupGridKey = "GroupGrid";
    private const string MixerMainGridKey = "MixerMainGrid";
    private const string MixerInsertGridKey = "MixerInsertGrid";
    private const string MixerSendGridKey = "MixerSendGrid";
    private const string SummaryTabKey = "summary";
    private const string PluginsTabKey = "plugins";
    private const string TracksTabKey = "tracks";
    private const string GroupsTabKey = "groups";
    private const string MixerTabKey = "mixer";
    private const string NotesTabKey = "notes";
    private const string HistoryTabKey = "history";
    private const int SongMetadataCacheLimit = 256;
    public MainForm()
    {
        _theme = AppThemes.Resolve(BrowserConfigStore.LoadTheme());
        _fontPreferences = AppFontSettings.LoadPreferences();
        _enableSongLaunch = BrowserConfigStore.LoadEnableSongLaunch();
        _songGridDoubleClickAction = BrowserConfigStore.LoadSongGridDoubleClickAction();
        _lockCurrentDetailTab = BrowserConfigStore.LoadLockCurrentDetailTab();
        _restoreFilterSessionOnStartup = BrowserConfigStore.LoadRestoreFilterSessionOnStartup()
            || BrowserConfigStore.LoadRestoreAdvancedSearchSessionOnStartup();
        BrowserConfigStore.EnsurePluginsDetailTabInitialized();
        _lastNonHistoryTabIndex = BrowserConfigStore.LoadLastSelectedDetailTabIndex() ?? 0;
        _restoreViewAllSongsOnStartup = _restoreFilterSessionOnStartup && BrowserConfigStore.LoadViewAllSongs();
        _songAgeFilter = _restoreFilterSessionOnStartup && !_restoreViewAllSongsOnStartup
            ? BrowserConfigStore.LoadSongAgeFilterPreference()
            : null;
        _lastAdvancedSearchQuery = _restoreFilterSessionOnStartup
            ? BrowserConfigStore.LoadLastAdvancedSearchQuery()
            : null;
        _savedAdvancedSearches = BrowserConfigStore.LoadSavedAdvancedSearches().ToList();
        _csvExportFields = BuildCsvExportFields();
        _customSongColumns = BrowserConfigStore.LoadCustomSongColumns().ToList();
        _songGridColumnFields = BuildSongGridColumnFields().ToList();
        _songStatuses = BrowserConfigStore.LoadSongStatuses().ToList();
        _songGenres = BrowserConfigStore.LoadSongGenres().ToList();
        _songGridSortLevels = BrowserConfigStore.LoadSongGridSortLevels().ToList();
        _savedSongLensViews = BrowserConfigStore.LoadSavedSongLensViews().ToList();
        BrowserConfigStore.EnsureSongAnnotationColumnsInitialized();
        BrowserConfigStore.EnsureSongGenreColumnInitialized();
        BrowserConfigStore.EnsureSongAnnotationColumnsMovedToEnd();
        BrowserConfigStore.EnsureExistingCustomSongColumnsVisible();
        _visibleDetailTabKeys = BrowserConfigStore.LoadDetailTabVisibleKeys().ToList();
        _showTracksWithEventsOnly = BrowserConfigStore.LoadShowTracksWithEventsOnly();
        _hideDisabledTracks = BrowserConfigStore.LoadHideDisabledTracks();
        _hideFolderTracks = BrowserConfigStore.LoadHideFolderTracks();
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
            _startupRootPath = savedRoot;
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
        _fileMenuItem.DropDownItems.Add(_exportExcelMenuItem);
        _fileMenuItem.DropDownItems.Add(new ToolStripSeparator());
        _fileMenuItem.DropDownItems.Add(_exitMenuItem);
        _toolsMenuItem.DropDownItems.Add(_advancedSearchMenuItem);
        _toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());
        _toolsMenuItem.DropDownItems.Add(_preferencesMenuItem);
        _helpMenuItem.DropDownItems.Add(_helpContentsMenuItem);
        _helpMenuItem.DropDownItems.Add(new ToolStripSeparator());
        _helpMenuItem.DropDownItems.Add(_aboutMenuItem);
        _menuStrip.Items.Add(_fileMenuItem);
        _menuStrip.Items.Add(_toolsMenuItem);
        _menuStrip.Items.Add(_helpMenuItem);
        MainMenuStrip = _menuStrip;
        _rootLayout.Controls.Add(_menuStrip, 0, 0);
        RefreshRecentlyViewedMenu();

        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            Padding = new Padding(6, 4, 6, 2)
        };
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
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
        _searchTextBox.Margin = new Padding(6, 2, 2, 2);
        WindowsShell.SetCueBanner(_searchTextBox, "Fast Catalog Search by Keyword");
        _toolTip.SetToolTip(_searchTextBox, "Fast keyword search across your song catalog");
        _clearSearchButton.Text = "×";
        _clearSearchButton.AccessibleName = "Clear Fast Keyword Search";
        _clearSearchButton.Dock = DockStyle.Fill;
        _clearSearchButton.Margin = new Padding(0, 2, 6, 2);
        _clearSearchButton.Enabled = false;
        _toolTip.SetToolTip(_clearSearchButton, "Clear the Fast Keyword Search text");
        _advancedSearchButton.Image = CreateAdvancedSearchIcon();
        _advancedSearchButton.Text = "";
        _advancedSearchButton.AccessibleName = "Advanced Search";
        _advancedSearchButton.Dock = DockStyle.Fill;
        _advancedSearchButton.Margin = new Padding(0, 2, 0, 2);
        _toolTip.SetToolTip(_advancedSearchButton, "Advanced filtered search");
        StyleButton(_browseButton, useAccent: false);
        StyleButton(_clearSearchButton, useAccent: false);
        StyleButton(_advancedSearchButton, useAccent: false);
        StyleFolderToolbarButton(_browseButton);
        StyleFolderToolbarButton(_advancedSearchButton);

        toolbar.Controls.Add(_rootPathComboBox, 0, 0);
        toolbar.Controls.Add(_browseButton, 1, 0);
        toolbar.Controls.Add(_searchTextBox, 2, 0);
        toolbar.Controls.Add(_clearSearchButton, 3, 0);
        toolbar.Controls.Add(_advancedSearchButton, 4, 0);

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
        StyleButton(_expandAllButton, useAccent: false);
        StyleButton(_collapseAllButton, useAccent: false);
        StyleFolderToolbarButton(_expandAllButton);
        StyleFolderToolbarButton(_collapseAllButton);
        treeToolbar.Controls.Add(_expandAllButton);
        treeToolbar.Controls.Add(_collapseAllButton);
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
        _folderTreeContextMenu.Items.Add(_contextRevealFolderInExplorerMenuItem);
        _contextExportFolderMenuItem.DropDownItems.Add(_contextExportFolderCsvMenuItem);
        _contextExportFolderMenuItem.DropDownItems.Add(_contextExportFolderExcelMenuItem);
        _folderTreeContextMenu.Items.Add(_contextExportFolderMenuItem);
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
                return;
            }

            if (_startupRootPath is not { } startupRootPath)
            {
                return;
            }

            _startupRootPath = null;
            BeginInvoke(() =>
            {
                SetRootPath(startupRootPath);
                if (_restoreFilterSessionOnStartup && _lastAdvancedSearchQuery is not null)
                {
                    RunAdvancedSearch(_lastAdvancedSearchQuery);
                }
                else if (_restoreFilterSessionOnStartup
                    && (_restoreViewAllSongsOnStartup || _songAgeFilter is not null))
                {
                    LoadAllSongs();
                }
            });
        };

        ConfigureSongGrid();
        _songGridPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _songGridPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, AppFontSettings.Scale(34, _fontPreferences, AppFontSection.MainUi)));
        _songGridPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
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
        _songGridHeaderToolStrip.Visible = false;

        _viewOptionsMenu.Items.Add(_saveCurrentViewMenuItem);
        _viewOptionsMenu.Items.Add(new ToolStripSeparator());
        _viewOptionsMenu.Items.Add(_savedViewsMenuItem);
        _viewOptionsMenu.Items.Add(_manageSavedViewsMenuItem);
        _viewOptionsMenu.Items.Add(_restoreDefaultViewMenuItem);
        _viewOptionsMenu.Items.Add(new ToolStripSeparator());
        _viewOptionsMenu.Items.Add(_viewColumnsMenuItem);
        _viewOptionsMenu.Items.Add(new ToolStripSeparator());
        _viewOptionsMenu.Items.Add(_bulkEditMenuItem);
        RefreshSavedViewsMenu();

        _songGridHintLabel.Dock = DockStyle.Fill;
        _songGridHintLabel.AutoSize = false;
        _songGridHintLabel.TextAlign = ContentAlignment.MiddleLeft;
        _songGridHintLabel.Padding = new Padding(8, 0, 8, 0);
        _songGridHintLabel.Text = "";
        _songGridHeaderPanel.Controls.Add(_songGridHintLabel);

        _songGridTitleLabel.AutoSize = false;
        _songGridTitleLabel.AutoEllipsis = true;
        _songGridTitleLabel.Text = "SongLens View · 0 songs";
        _songGridTitleLabel.Font = GetSongGridTitleFont(18);
        _songGridTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _songGridTitleLabel.AccessibleName = "SongLens View";
        _songGridHeaderPanel.Controls.Add(_songGridTitleLabel);
        _songGridTitleLabel.BringToFront();

        ConfigureViewHistoryButton(_viewBackButton, "←", "Back", "Go back to the previous SongLens View");
        ConfigureViewHistoryButton(_viewForwardButton, "→", "Forward", "Go forward to the next SongLens View");

        _refreshButton.Image = CreateRefreshIcon();
        _refreshButton.Text = "";
        _refreshButton.AutoSize = false;
        _refreshButton.Size = AppFontSettings.Scale(new Size(24, 24), _fontPreferences, AppFontSection.MainUi);
        _refreshButton.Margin = Padding.Empty;
        _refreshButton.AccessibleName = "Refresh";
        _refreshButton.Enabled = false;
        _toolTip.SetToolTip(_refreshButton, "Refresh (F5)");
        StyleRefreshButton();
        _songGridHeaderPanel.Controls.Add(_refreshButton);
        _refreshButton.BringToFront();

        _recentlyViewedButton.Image = CreateRecentlyViewedIcon();
        _recentlyViewedButton.Text = "";
        _recentlyViewedButton.AutoSize = false;
        _recentlyViewedButton.Size = AppFontSettings.Scale(new Size(24, 24), _fontPreferences, AppFontSection.MainUi);
        _recentlyViewedButton.Margin = Padding.Empty;
        _recentlyViewedButton.AccessibleName = "Recently Viewed songs";
        _toolTip.SetToolTip(_recentlyViewedButton, "Recently Viewed");
        StyleRefreshButton(_recentlyViewedButton);
        _songGridHeaderPanel.Controls.Add(_recentlyViewedButton);
        _recentlyViewedButton.BringToFront();
        UpdateSongViewHistoryButtons();

        _songLensViewLoadingPanel.AutoSize = true;
        _songLensViewLoadingPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _songLensViewLoadingPanel.FlowDirection = FlowDirection.LeftToRight;
        _songLensViewLoadingPanel.WrapContents = false;
        _songLensViewLoadingPanel.Margin = Padding.Empty;
        _songLensViewLoadingPanel.Padding = Padding.Empty;
        _songLensViewLoadingPanel.Visible = false;
        _songLensViewLoadingLabel.AutoSize = true;
        _songLensViewLoadingLabel.Text = "Building SongLens View...";
        _songLensViewLoadingLabel.TextAlign = ContentAlignment.MiddleLeft;
        _songLensViewLoadingLabel.Margin = new Padding(0, 3, 8, 0);
        _songLensViewLoadingLabel.AccessibleName = "Building SongLens View";
        _songLensViewProgressBar.Style = ProgressBarStyle.Marquee;
        _songLensViewProgressBar.MarqueeAnimationSpeed = 28;
        _songLensViewProgressBar.Size = new Size(96, 14);
        _songLensViewProgressBar.Margin = new Padding(0, 4, 0, 0);
        _songLensViewProgressBar.AccessibleName = "Building SongLens View progress";
        _songLensViewLoadingPanel.Controls.Add(_songLensViewLoadingLabel);
        _songLensViewLoadingPanel.Controls.Add(_songLensViewProgressBar);
        _songGridHeaderPanel.Controls.Add(_songLensViewLoadingPanel);
        _songLensViewLoadingPanel.BringToFront();

        _songGridPanel.Controls.Add(_songGridHeaderPanel, 0, 0);
        ConfigureBulkEditBar();
        _songGridPanel.Controls.Add(_bulkEditBar, 0, 1);
        _songGridPanel.Controls.Add(_songGrid, 0, 2);
        _songDetailSplit.Panel1.Controls.Add(_songGridPanel);

        _detailTabsHostPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = _theme.AppBackColor
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
            new DetailTabDefinition { Key = PluginsTabKey, TabPage = _pluginsTab },
            new DetailTabDefinition { Key = TracksTabKey, TabPage = _tracksTab },
            new DetailTabDefinition { Key = GroupsTabKey, TabPage = _groupsTab },
            new DetailTabDefinition { Key = MixerTabKey, TabPage = _mixerTab },
            new DetailTabDefinition { Key = NotesTabKey, TabPage = _notesTab },
            new DetailTabDefinition { Key = HistoryTabKey, TabPage = _historyTab }
        ];

        ApplyDetailTabVisibility();
        _detailTabsHostPanel.Controls.Add(_detailTabs);

        _trackFiltersPanel.Margin = Padding.Empty;
        _trackFiltersPanel.Padding = Padding.Empty;
        _trackFiltersPanel.BackColor = _theme.SurfaceBackColor;
        _trackFiltersPanel.Dock = DockStyle.Fill;

        _trackViewOptionsToolStrip.AutoSize = true;
        _trackViewOptionsToolStrip.Dock = DockStyle.Right;
        _trackViewOptionsToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _trackViewOptionsToolStrip.Margin = Padding.Empty;
        _trackViewOptionsToolStrip.Padding = Padding.Empty;
        _trackViewOptionsToolStrip.BackColor = _theme.SurfaceBackColor;
        _trackViewOptionsToolStrip.CanOverflow = false;
        _trackViewOptionsDropDownButton.AutoSize = true;
        _trackViewOptionsDropDownButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _trackViewOptionsDropDownButton.AccessibleName = "Track View Options";
        _trackViewOptionsDropDownButton.ToolTipText = "Choose which tracks appear in the Tracks view";
        _tracksWithEventsMenuItem.Checked = _showTracksWithEventsOnly;
        _tracksWithEventsMenuItem.ToolTipText = "Show tracks with events and their parent folders";
        _hideDisabledTracksMenuItem.Checked = _hideDisabledTracks;
        _hideDisabledTracksMenuItem.ToolTipText = "Hide or restore disabled tracks";
        _hideFolderTracksMenuItem.Checked = _hideFolderTracks;
        _hideFolderTracksMenuItem.ToolTipText = "Hide or restore folder rows";
        _trackViewOptionsDropDownButton.DropDownItems.AddRange(
        [
            _tracksWithEventsMenuItem,
            _hideDisabledTracksMenuItem,
            _hideFolderTracksMenuItem
        ]);
        _trackViewOptionsDropDownButton.DropDown.Closing += (_, args) =>
        {
            if (args.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
            {
                args.Cancel = true;
            }
        };
        _trackViewOptionsToolStrip.Items.Add(_trackViewOptionsDropDownButton);
        _trackFiltersPanel.Controls.Add(_trackViewOptionsToolStrip);

        _snapshotButton.Text = "Snapshot";
        _snapshotButton.AutoSize = true;
        _snapshotButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _snapshotButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _snapshotButton.Padding = new Padding(2, 0, 2, 0);
        _snapshotButton.Margin = Padding.Empty;
        _snapshotButton.AccessibleName = "Snapshot";
        _toolTip.SetToolTip(_snapshotButton, "View Song Snapshot");
        StyleButton(_snapshotButton, useAccent: true);
        _songGridHeaderPanel.Controls.Add(_snapshotButton);
        _snapshotButton.BringToFront();

        _songGridHelpButton.Image = CreateHelpIcon();
        _songGridHelpButton.Text = "";
        _songGridHelpButton.AutoSize = false;
        _songGridHelpButton.Size = AppFontSettings.Scale(new Size(24, 24), _fontPreferences, AppFontSection.MainUi);
        _songGridHelpButton.Margin = Padding.Empty;
        _songGridHelpButton.AccessibleName = "SongLens View help";
        _toolTip.SetToolTip(_songGridHelpButton, "SongLens View help");
        StyleRefreshButton(_songGridHelpButton);
        _songGridHeaderPanel.Controls.Add(_songGridHelpButton);
        _songGridHelpButton.BringToFront();

        _filterByDateButton.Text = "Filter";
        _filterByDateButton.AutoSize = true;
        _filterByDateButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _filterByDateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _filterByDateButton.Padding = new Padding(2, 0, 2, 0);
        _filterByDateButton.Margin = Padding.Empty;
        _filterByDateButton.AccessibleName = "Filter songs by date";
        _toolTip.SetToolTip(_filterByDateButton, "Choose and apply a date filter");
        StyleButton(_filterByDateButton, useAccent: false);
        _songGridHeaderPanel.Controls.Add(_filterByDateButton);
        _filterByDateButton.BringToFront();

        _songGridColumnsButton.Text = "View ▼";
        _songGridColumnsButton.AutoSize = true;
        _songGridColumnsButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _songGridColumnsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _songGridColumnsButton.Padding = new Padding(2, 0, 2, 0);
        _songGridColumnsButton.Margin = Padding.Empty;
        _songGridColumnsButton.AccessibleName = "SongLens View options";
        _toolTip.SetToolTip(_songGridColumnsButton, "Open saved views or choose visible columns");
        StyleButton(_songGridColumnsButton, useAccent: false);
        _songGridHeaderPanel.Controls.Add(_songGridColumnsButton);
        _songGridColumnsButton.BringToFront();

        _statisticsButton.Text = "Statistics";
        _statisticsButton.AutoSize = true;
        _statisticsButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _statisticsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _statisticsButton.Padding = new Padding(2, 0, 2, 0);
        _statisticsButton.Margin = Padding.Empty;
        _statisticsButton.AccessibleName = "SongLens View statistics";
        _toolTip.SetToolTip(_statisticsButton, "Summarize the current SongLens View");
        StyleButton(_statisticsButton, useAccent: false);
        _songGridHeaderPanel.Controls.Add(_statisticsButton);
        _statisticsButton.BringToFront();
        ReserveSongGridHeaderButtonWidths();
        _songGridHeaderPanel.Resize += (_, _) => PositionDetailHeaderControls();
        _detailTabsHostPanel.Resize += (_, _) => PositionDetailHeaderControls();
        PositionDetailHeaderControls();

        ConfigureDetailGrid(_summaryGrid, ("Field", 180), ("Value", 720));
        ConfigureDetailGrid(_pluginsGrid,
            ("Device", 300), ("Category", 150), ("Subcategory", 240), ("Preset Type", 140));
        _pluginsGrid.Columns["Device"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        ConfigureDetailGrid(_trackGrid, ("#", 50), ("Disabled", 28), ("Track", 260), ("Instrument", 240), ("Track Note", 560));
        ConfigureDetailGrid(_groupGrid, ("Group", 220), ("Track Names", 700));
        ConfigureDetailGrid(_mixerMainGrid, ("Channel", 140), ("Pre", 320), ("Post", 320));
        ConfigureDetailGrid(_mixerInsertGrid, ("Channel", 220), ("Slot", 90), ("Plugin", 220), ("Preset", 360));
        ConfigureDetailGrid(_mixerSendGrid, ("Channel", 220), ("Send", 90), ("Destination", 180), ("Preset", 220));
        ConfigureTrackGridLayout();
        ConfigureGroupGridLayout();
        ApplySavedGridColumnWidths(_songGrid, SongGridKey);
        ApplySavedGridColumnWidths(_summaryGrid, SummaryGridKey);
        ApplySavedGridColumnWidths(_pluginsGrid, PluginsGridKey);
        ApplySavedGridColumnWidths(_trackGrid, TrackGridKey);
        ApplySavedGridColumnWidths(_groupGrid, GroupGridKey);
        ApplySavedGridColumnWidths(_mixerMainGrid, MixerMainGridKey);
        ApplySavedGridColumnWidths(_mixerInsertGrid, MixerInsertGridKey);
        ApplySavedGridColumnWidths(_mixerSendGrid, MixerSendGridKey);
        ConfigureNotesTextBox();
        _summaryTab.Controls.Add(_summaryGrid);
        _pluginsTab.Controls.Add(_pluginsGrid);
        _tracksTab.Controls.Add(BuildTracksTabLayout());
        _groupsTab.Controls.Add(_groupGrid);
        _mixerTab.Controls.Add(BuildMixerTabLayout());
        _notesTab.Controls.Add(_notesTextBox);

        var statusStrip = new StatusStrip
        {
            Dock = DockStyle.Fill,
            SizingGrip = false
        };
        _statusLabel.Text = "";
        _statusLabel.Margin = new Padding(0, 3, 0, 2);
        _diagnosticLogLinkLabel.Margin = new Padding(6, 3, 0, 2);
        _diagnosticLogLinkLabel.Click += (_, _) => ShowDiagnosticLog();
        _filterStatusLabel.Margin = new Padding(12, 3, 0, 2);
        _stickyTabsStatusLabel.Margin = new Padding(12, 3, 0, 2);
        statusStrip.Items.Add(_statusLabel);
        statusStrip.Items.Add(_diagnosticLogLinkLabel);
        statusStrip.Items.Add(_statusSpacerLabel);
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
        _songGrid.EditMode = DataGridViewEditMode.EditOnEnter;
        _songGrid.GridColor = _theme.BorderColor;
        _songGrid.AllowUserToOrderColumns = true;
        _songGrid.MultiSelect = false;
        _songGrid.ReadOnly = false;
        _songGrid.RowHeadersVisible = false;
        _songGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _songGrid.ShowCellToolTips = false;
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
        SyncSongGridContextMenuAvailability();
        foreach (var field in _songGridColumnFields)
        {
            AddSongColumn(field.ColumnName, field.Label, field.Width);
        }
        ApplySongGridColumnVisibility(BrowserConfigStore.LoadSongGridVisibleColumnKeys());
        ApplySavedSongGridColumnOrder();
        ConfigureSongGridColumnSizing();
        ApplySongGridSort();
    }

    private void ConfigureBulkEditBar()
    {
        _bulkEditBar.Dock = DockStyle.Fill;
        _bulkEditBar.Margin = Padding.Empty;
        _bulkEditBar.Padding = new Padding(6, 3, 6, 2);
        _bulkEditBar.WrapContents = false;
        _bulkEditBar.AutoScroll = true;
        _bulkEditBar.Visible = false;
        _bulkEditCountLabel.AutoSize = true;
        _bulkEditCountLabel.Text = "0 selected";
        _bulkEditCountLabel.Margin = new Padding(0, 7, 8, 0);
        foreach (var combo in new[] { _bulkStatusComboBox, _bulkGenreComboBox })
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Width = AppFontSettings.Scale(116, _fontPreferences, AppFontSection.MainUi);
            combo.Margin = new Padding(6, 2, 1, 0);
        }
        foreach (var button in new[]
                 { _bulkSelectAllButton, _bulkClearButton, _bulkSetStatusButton, _bulkSetGenreButton, _bulkSetRatingButton, _bulkDeleteButton, _bulkDoneButton })
        {
            StyleButton(button, useAccent: false);
            button.AutoSize = false;
            button.Height = AppFontSettings.Scale(26, _fontPreferences, AppFontSection.MainUi);
            button.Margin = new Padding(1, 1, 1, 0);
        }
        _bulkSelectAllButton.Width = AppFontSettings.Scale(42, _fontPreferences, AppFontSection.MainUi);
        _bulkClearButton.Width = AppFontSettings.Scale(50, _fontPreferences, AppFontSection.MainUi);
        _bulkSetStatusButton.Width = AppFontSettings.Scale(42, _fontPreferences, AppFontSection.MainUi);
        _bulkSetGenreButton.Width = AppFontSettings.Scale(42, _fontPreferences, AppFontSection.MainUi);
        _bulkSetRatingButton.Width = AppFontSettings.Scale(42, _fontPreferences, AppFontSection.MainUi);
        _bulkDeleteButton.Width = AppFontSettings.Scale(58, _fontPreferences, AppFontSection.MainUi);
        _bulkDoneButton.Width = AppFontSettings.Scale(50, _fontPreferences, AppFontSection.MainUi);
        _bulkRatingLabel.Margin = new Padding(6, 7, 0, 0);
        _bulkRatingPicker.Size = AppFontSettings.Scale(new Size(88, 26), _fontPreferences, AppFontSection.MainUi);
        _bulkRatingPicker.Margin = new Padding(1, 0, 1, 0);
        _toolTip.SetToolTip(_bulkRatingPicker, "Choose 1-5 stars for every song in the current grid. Click the selected rating again to clear ratings.");
        _bulkEditBar.Controls.AddRange(
        [
            _bulkEditCountLabel, _bulkSelectAllButton, _bulkClearButton,
            _bulkStatusComboBox, _bulkSetStatusButton,
            _bulkGenreComboBox, _bulkSetGenreButton,
            _bulkRatingLabel, _bulkRatingPicker, _bulkSetRatingButton,
            _bulkDeleteButton, _bulkDoneButton
        ]);
        RefreshBulkEditOptions();
    }

    private void RefreshBulkEditOptions()
    {
        _bulkStatusComboBox.Items.Clear();
        _bulkStatusComboBox.Items.AddRange(["Choose status", "Clear status", .. _songStatuses]);
        _bulkStatusComboBox.SelectedIndex = 0;
        _bulkGenreComboBox.Items.Clear();
        _bulkGenreComboBox.Items.AddRange(["Choose genre", "Clear genre", .. _songGenres]);
        _bulkGenreComboBox.SelectedIndex = 0;
    }

    private void AddSongColumn(string name, string header, int width)
    {
        DataGridViewColumn column;
        if (name is StatusColumnName or GenreColumnName)
        {
            column = new DataGridViewComboBoxColumn
            {
                Name = name,
                HeaderText = header,
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
            };
            ((DataGridViewComboBoxColumn)column).Items.Add("");
            var options = string.Equals(name, StatusColumnName, StringComparison.Ordinal)
                ? _songStatuses
                : _songGenres;
            ((DataGridViewComboBoxColumn)column).Items.AddRange(options.Cast<object>().ToArray());
        }
        else
        {
            column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header
            };
        }

        _songGrid.Columns.Add(column);
        column.Width = width;
        column.SortMode = DataGridViewColumnSortMode.Programmatic;
        column.ReadOnly = name is not StatusColumnName and not GenreColumnName
            && !_customSongColumns.Any(custom => CustomSongColumnName(custom.Id) == name);
        if (string.Equals(name, PlayableColumnName, StringComparison.Ordinal))
        {
            column.MinimumWidth = width;
            column.Resizable = DataGridViewTriState.False;
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }

        if (IsSongGridDateColumn(name))
        {
            column.ValueType = typeof(DateTime);
            column.DefaultCellStyle.Format = "g";
        }
        else if (string.Equals(name, RatingColumnName, StringComparison.Ordinal))
        {
            column.ValueType = typeof(int);
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
    }

    private static bool IsSongGridDateColumn(string columnName)
    {
        return string.Equals(columnName, "DateCreated", StringComparison.Ordinal)
            || string.Equals(columnName, "LastModified", StringComparison.Ordinal);
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

        _trackGrid.Columns["Disabled"]!.HeaderText = "";
        _trackGrid.Columns["Disabled"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        _trackGrid.Columns["Disabled"]!.Width = 28;
        _trackGrid.Columns["Disabled"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

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

        var defaultKeys = _detailTabDefinitions
            .Select(definition => definition.Key)
            .ToArray();

        if (_visibleDetailTabKeys.Count == 0)
        {
            return defaultKeys;
        }

        var validKeys = _detailTabDefinitions
            .Select(definition => definition.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selectedKeys = _visibleDetailTabKeys
            .Where(key => validKeys.Contains(key))
            .ToArray();

        return selectedKeys.Length == 0
            ? defaultKeys
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
        _viewFullMixerButton.Text = "View Full Mixer...";
        _viewFullMixerButton.AutoSize = true;
        _viewFullMixerButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _viewFullMixerButton.Padding = new Padding(4, 0, 4, 0);
        _viewFullMixerButton.Margin = new Padding(4, 1, 8, 1);
        _viewFullMixerButton.AccessibleName = "View Full Mixer";
        _viewFullMixerButton.Enabled = false;
        _toolTip.SetToolTip(_viewFullMixerButton, "View every mixer channel for the selected song");
        StyleButton(_viewFullMixerButton, useAccent: true);

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
        _mixerTabLayout.Controls.Add(BuildMixerSection("Main", _mixerMainGrid, _viewFullMixerButton), 0, 0);
        _mixerTabLayout.Controls.Add(BuildMixerSection("Inserts", _mixerInsertGrid), 0, 1);
        _mixerTabLayout.Controls.Add(BuildMixerSection("Sends", _mixerSendGrid), 0, 2);
        return _mixerTabLayout;
    }

    private Control BuildTracksTabLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, AppFontSettings.Scale(28, _fontPreferences, AppFontSection.MainUi)));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var label = new Label
        {
            Text = "Tracks",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0)
        };
        _trackFiltersPanel.Controls.Add(label);
        label.SendToBack();

        layout.Controls.Add(_trackFiltersPanel, 0, 0);
        layout.Controls.Add(_trackGrid, 0, 1);
        return layout;
    }

    private Control BuildMixerSection(string title, Control content, Control? headerAction = null)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, AppFontSettings.Scale(28, _fontPreferences, AppFontSection.MainUi)));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0)
        };

        var header = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        header.Controls.Add(label);
        if (headerAction is not null)
        {
            headerAction.Dock = DockStyle.Right;
            header.Controls.Add(headerAction);
            headerAction.BringToFront();
        }

        layout.Controls.Add(header, 0, 0);
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

    private Bitmap CreateRefreshIcon()
    {
        var bitmap = new Bitmap(18, 18);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var pen = new Pen(_theme.SearchIconColor, 1.8f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };
        graphics.DrawArc(pen, 3f, 3f, 12f, 12f, -55f, 285f);
        graphics.DrawLines(
            pen,
            [new PointF(13.8f, 2.5f), new PointF(14.5f, 6.5f), new PointF(10.5f, 5.2f)]);
        return bitmap;
    }

    private Bitmap CreateRecentlyViewedIcon()
    {
        var bitmap = new Bitmap(18, 18);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var pen = new Pen(_theme.SearchIconColor, 1.4f)
        {
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };
        graphics.DrawEllipse(pen, 1.5f, 7f, 6.5f, 6.5f);
        graphics.DrawEllipse(pen, 10f, 7f, 6.5f, 6.5f);
        graphics.DrawArc(pen, 6.5f, 7.5f, 5f, 4f, 190f, 160f);
        graphics.DrawLine(pen, 3f, 7f, 4.5f, 3.5f);
        graphics.DrawLine(pen, 15f, 7f, 13.5f, 3.5f);
        graphics.DrawLine(pen, 4.5f, 3.5f, 7f, 3.5f);
        graphics.DrawLine(pen, 11f, 3.5f, 13.5f, 3.5f);
        return bitmap;
    }

    private Bitmap CreateHelpIcon()
    {
        var bitmap = new Bitmap(18, 18);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var pen = new Pen(_theme.SearchIconColor, 1.5f);
        graphics.DrawEllipse(pen, 2.25f, 2.25f, 13.5f, 13.5f);
        using var questionFont = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel);
        TextRenderer.DrawText(
            graphics,
            "?",
            questionFont,
            new Rectangle(3, 1, 12, 16),
            _theme.SearchIconColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
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
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = _theme.AccentHoverColor;
        button.FlatAppearance.MouseDownBackColor = _theme.AccentPressedColor;
        button.BackColor = _theme.AccentSoftColor;
        button.ForeColor = _theme.TextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
    }

    private void StyleFolderToolbarButton(Button button)
    {
        button.BackColor = _theme.AccentSoftColor;
        button.ForeColor = _theme.TextColor;
        button.FlatAppearance.MouseOverBackColor = _theme.AccentHoverColor;
        button.FlatAppearance.MouseDownBackColor = _theme.AccentPressedColor;
    }

    private void StyleRefreshButton(Button? button = null)
    {
        var iconButton = button ?? _refreshButton;
        StyleButton(iconButton, useAccent: true);
    }

    private void ConfigureViewHistoryButton(Button button, string text, string accessibleName, string toolTip)
    {
        button.Text = text;
        button.AutoSize = false;
        button.Size = AppFontSettings.Scale(new Size(24, 24), _fontPreferences, AppFontSection.MainUi);
        button.Margin = Padding.Empty;
        button.AccessibleName = accessibleName;
        button.Enabled = false;
        _toolTip.SetToolTip(button, toolTip);
        StyleRefreshButton(button);
        _songGridHeaderPanel.Controls.Add(button);
        button.BringToFront();
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
            if (_bulkEditMode && _songGridPanel is not null)
                _songGridPanel.RowStyles[1].Height = AppFontSettings.Scale(38, _fontPreferences, AppFontSection.MainUi);
            foreach (Control control in _bulkEditBar.Controls)
                control.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            _bulkStatusComboBox.Width = AppFontSettings.Scale(116, _fontPreferences, AppFontSection.MainUi);
            _bulkGenreComboBox.Width = AppFontSettings.Scale(116, _fontPreferences, AppFontSection.MainUi);
            _bulkSelectAllButton.Size = AppFontSettings.Scale(new Size(42, 26), _fontPreferences, AppFontSection.MainUi);
            _bulkClearButton.Size = AppFontSettings.Scale(new Size(50, 26), _fontPreferences, AppFontSection.MainUi);
            _bulkSetStatusButton.Size = AppFontSettings.Scale(new Size(42, 26), _fontPreferences, AppFontSection.MainUi);
            _bulkSetGenreButton.Size = AppFontSettings.Scale(new Size(42, 26), _fontPreferences, AppFontSection.MainUi);
            _bulkSetRatingButton.Size = AppFontSettings.Scale(new Size(42, 26), _fontPreferences, AppFontSection.MainUi);
            _bulkDeleteButton.Size = AppFontSettings.Scale(new Size(58, 26), _fontPreferences, AppFontSection.MainUi);
            _bulkDoneButton.Size = AppFontSettings.Scale(new Size(50, 26), _fontPreferences, AppFontSection.MainUi);
            _bulkRatingPicker.Size = AppFontSettings.Scale(new Size(88, 26), _fontPreferences, AppFontSection.MainUi);
            _summaryGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
            _pluginsGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
            _trackGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
            _mixerMainGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
            _mixerInsertGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
            _mixerSendGrid.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.DetailGrids);
            _songGridHeaderToolStrip.Font = Font;
            _songLensViewLoadingLabel.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            _trackViewOptionsToolStrip.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            _viewBackButton.Size = AppFontSettings.Scale(new Size(24, 24), _fontPreferences, AppFontSection.MainUi);
            _viewForwardButton.Size = AppFontSettings.Scale(new Size(24, 24), _fontPreferences, AppFontSection.MainUi);
            _refreshButton.Size = AppFontSettings.Scale(new Size(24, 24), _fontPreferences, AppFontSection.MainUi);
            _recentlyViewedButton.Size = AppFontSettings.Scale(new Size(24, 24), _fontPreferences, AppFontSection.MainUi);
            _songGridHelpButton.Size = AppFontSettings.Scale(new Size(24, 24), _fontPreferences, AppFontSection.MainUi);
            _snapshotButton.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            _filterByDateButton.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            _songGridColumnsButton.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            _statisticsButton.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            _viewFullMixerButton.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.MainUi);
            ReserveSongGridHeaderButtonWidths();
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
            _pluginsGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
            _trackGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
            _mixerMainGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
            _mixerInsertGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
            _mixerSendGrid.ColumnHeadersHeight = AppFontSettings.Scale(24, _fontPreferences, AppFontSection.DetailGrids);
            _songGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.SongGrid);
            _summaryGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
            _pluginsGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
            _trackGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
            _mixerMainGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
            _mixerInsertGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
            _mixerSendGrid.RowTemplate.Height = AppFontSettings.Scale(22, _fontPreferences, AppFontSection.DetailGrids);
            _folderImages.ImageSize = AppFontSettings.Scale(new Size(18, 18), _fontPreferences, AppFontSection.FolderTree);
            ConfigureFolderImages();
            ApplyFolderTrackRowStyles();
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
        _toolsMenuItem.ForeColor = _theme.TextColor;
        _helpMenuItem.ForeColor = _theme.TextColor;
        _themeMenuItem.ForeColor = _theme.TextColor;
        _darkThemeMenuItem.ForeColor = _theme.TextColor;
        _lightThemeMenuItem.ForeColor = _theme.TextColor;
        _preferencesMenuItem.ForeColor = _theme.TextColor;
        _helpContentsMenuItem.ForeColor = _theme.TextColor;
        _aboutMenuItem.ForeColor = _theme.TextColor;
        _toolsMenuItem.BackColor = _theme.AppBackColor;
        _helpMenuItem.BackColor = _theme.AppBackColor;
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
        _songGridContextMenu.BackColor = _theme.PanelBackColor;
        _songGridContextMenu.ForeColor = _theme.TextColor;
        _songGridContextMenu.Renderer = new ThemeMenuRenderer(_theme);
        foreach (ToolStripItem item in _songGridContextMenu.Items)
        {
            item.BackColor = _theme.PanelBackColor;
            item.ForeColor = _theme.TextColor;
        }
        _darkThemeMenuItem.Checked = string.Equals(_theme.Name, AppThemes.Dark.Name, StringComparison.OrdinalIgnoreCase);
        _lightThemeMenuItem.Checked = string.Equals(_theme.Name, AppThemes.Light.Name, StringComparison.OrdinalIgnoreCase);

        _rootPathComboBox.BackColor = _theme.PanelBackColor;
        _rootPathComboBox.ForeColor = _theme.TextColor;
        _advancedSearchButton.Image = CreateAdvancedSearchIcon();
        _refreshButton.Image?.Dispose();
        _refreshButton.Image = CreateRefreshIcon();
        _recentlyViewedButton.Image?.Dispose();
        _recentlyViewedButton.Image = CreateRecentlyViewedIcon();
        _songGridHelpButton.Image?.Dispose();
        _songGridHelpButton.Image = CreateHelpIcon();
        _searchTextBox.BackColor = _theme.PanelBackColor;
        _searchTextBox.ForeColor = _theme.TextColor;
        StyleButton(_browseButton, useAccent: false);
        StyleButton(_clearSearchButton, useAccent: false);
        StyleRefreshButton(_viewBackButton);
        StyleRefreshButton(_viewForwardButton);
        StyleRefreshButton();
        StyleRefreshButton(_recentlyViewedButton);
        StyleRefreshButton(_songGridHelpButton);
        StyleButton(_expandAllButton, useAccent: false);
        StyleButton(_collapseAllButton, useAccent: false);
        StyleButton(_advancedSearchButton, useAccent: false);
        StyleFolderToolbarButton(_browseButton);
        StyleFolderToolbarButton(_advancedSearchButton);
        StyleButton(_snapshotButton, useAccent: true);
        StyleButton(_filterByDateButton, useAccent: false);
        StyleButton(_songGridColumnsButton, useAccent: false);
        StyleButton(_statisticsButton, useAccent: false);
        StyleButton(_viewFullMixerButton, useAccent: true);
        _trackFiltersPanel.BackColor = _theme.SurfaceBackColor;
        _trackViewOptionsToolStrip.BackColor = _theme.SurfaceBackColor;
        _trackViewOptionsToolStrip.ForeColor = _theme.TextColor;
        _trackViewOptionsToolStrip.Renderer = new FlatToolStripButtonRenderer(_theme, _theme.SurfaceBackColor);
        _trackViewOptionsDropDownButton.ForeColor = _theme.TextColor;
        _trackViewOptionsDropDownButton.DropDown.BackColor = _theme.PanelBackColor;
        _trackViewOptionsDropDownButton.DropDown.ForeColor = _theme.TextColor;
        _trackViewOptionsDropDownButton.DropDown.Renderer = new ThemeMenuRenderer(_theme);
        StyleFolderToolbarButton(_expandAllButton);
        StyleFolderToolbarButton(_collapseAllButton);
        _songGridHeaderToolStrip.Renderer = new FlatToolStripButtonRenderer(_theme);
        _songGridHeaderToolStrip.BackColor = _theme.PanelBackColor;
        _songGridHeaderToolStrip.ForeColor = _theme.TextColor;
        _viewOptionsMenu.BackColor = _theme.PanelBackColor;
        _viewOptionsMenu.ForeColor = _theme.TextColor;
        _viewOptionsMenu.Renderer = new ThemeMenuRenderer(_theme);
        _recentlyViewedMenu.BackColor = _theme.PanelBackColor;
        _recentlyViewedMenu.ForeColor = _theme.TextColor;
        _recentlyViewedMenu.Renderer = new ThemeMenuRenderer(_theme);
        foreach (ToolStripItem item in _viewOptionsMenu.Items)
        {
            item.BackColor = _theme.PanelBackColor;
            item.ForeColor = _theme.TextColor;
        }
        _savedViewsMenuItem.DropDown.BackColor = _theme.PanelBackColor;
        _savedViewsMenuItem.DropDown.ForeColor = _theme.TextColor;
        _savedViewsMenuItem.DropDown.Renderer = new ThemeMenuRenderer(_theme);
        foreach (ToolStripItem item in _savedViewsMenuItem.DropDownItems)
        {
            item.BackColor = _theme.PanelBackColor;
            item.ForeColor = _theme.TextColor;
        }

        ApplyThemeToChildContainers(this);

        _bulkEditBar.BackColor = _theme.SurfaceBackColor;
        _bulkEditCountLabel.ForeColor = _theme.TextColor;
        _bulkRatingLabel.ForeColor = _theme.TextColor;
        _bulkRatingPicker.BackColor = _theme.SurfaceBackColor;
        _bulkRatingPicker.SetStarColors(GetRatingStarColor(), _theme.MutedTextColor);
        foreach (var combo in new[] { _bulkStatusComboBox, _bulkGenreComboBox })
        {
            combo.BackColor = _theme.PanelBackColor;
            combo.ForeColor = _theme.TextColor;
        }
        foreach (var button in new[]
                 { _bulkSelectAllButton, _bulkClearButton, _bulkSetStatusButton, _bulkSetGenreButton, _bulkSetRatingButton, _bulkDeleteButton, _bulkDoneButton })
            StyleButton(button, useAccent: false);

        _folderTree.BackColor = _theme.PanelBackColor;
        _folderTree.ForeColor = _theme.TextColor;
        _folderTree.LineColor = _theme.BorderColor;
        _songGridHeaderPanel.BackColor = _theme.PanelBackColor;
        _songGridHintLabel.BackColor = _theme.PanelBackColor;
        _songGridHintLabel.ForeColor = _theme.MutedTextColor;
        _songGridTitleLabel.BackColor = _theme.PanelBackColor;
        _songGridTitleLabel.ForeColor = _theme.TextColor;
        _songLensViewLoadingPanel.BackColor = _theme.PanelBackColor;
        _songLensViewLoadingLabel.BackColor = _theme.PanelBackColor;
        _songLensViewLoadingLabel.ForeColor = _theme.TextColor;
        ConfigureFolderImages();
        _folderTree.Invalidate();

        if (_detailTabsHostPanel is not null)
        {
            // The host is visible behind the overlaid track filters in the tab-header strip.
            // Match the unselected tab background so transparent filters blend into the strip.
            _detailTabsHostPanel.BackColor = _theme.AppBackColor;
            _trackFiltersPanel.BackColor = _theme.SurfaceBackColor;
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
        ApplyGridTheme(_pluginsGrid);
        ApplyGridTheme(_trackGrid);
        ApplyFolderTrackRowStyles();
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
        _statusLabel.BackColor = _theme.StatusBackColor;
        _diagnosticLogLinkLabel.BackColor = _theme.StatusBackColor;
        _diagnosticLogLinkLabel.ForeColor = _theme.TextColor;
        _diagnosticLogLinkLabel.LinkColor = _theme.TextColor;
        _diagnosticLogLinkLabel.ActiveLinkColor = _theme.AccentColor;
        _diagnosticLogLinkLabel.VisitedLinkColor = _theme.TextColor;
        _filterStatusLabel.ForeColor = _theme.TextColor;
        _stickyTabsStatusLabel.ForeColor = _theme.TextColor;

        if (_diagnosticLogLinkLabel.Visible)
        {
            ApplyStatusWarningAppearance();
        }

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
        }

        _contextPlaySongMenuItem.Visible = false;
        _contextSongActionsSeparator.Visible = _enableSongLaunch;
        _songGridContextMenu.Items.Add(_contextPlaySongMenuItem);
        _songGridContextMenu.Items.Add(_contextSongActionsSeparator);
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

                if (ReferenceEquals(grid, _songGrid))
                {
                    width = MigrateLegacySongGridColumnWidth(column.Name, width);
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
            ConfigureSongGridColumnSizing();
        }
    }

    private static int MigrateLegacySongGridColumnWidth(string columnName, int width)
    {
        return (columnName, width) switch
        {
            ("Year", 90) => 56,
            ("Tempo", 90) => 64,
            ("KeySignature", 120) => 76,
            ("TimeSignature", 120) => 60,
            ("TrackCount", 100) => 66,
            ("Length", 90) => 70,
            _ => width
        };
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
            if (ReferenceEquals(grid, _songGrid) && column.Name == BulkSelectColumnName)
                continue;
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

    private void ApplySavedSongGridColumnOrder()
    {
        var savedOrder = BrowserConfigStore.LoadSongGridColumnOrder();
        if (savedOrder.Count == 0)
        {
            return;
        }

        var orderedColumnNames = savedOrder
            .Where(columnName => _songGrid.Columns.Contains(columnName))
            .Concat(_songGrid.Columns
                .Cast<DataGridViewColumn>()
                .OrderBy(column => column.DisplayIndex)
                .Select(column => column.Name)
                .Where(columnName => !savedOrder.Contains(columnName, StringComparer.OrdinalIgnoreCase)))
            .ToArray();

        _suspendSongGridOrderPersistence = true;
        try
        {
            for (var displayIndex = 0; displayIndex < orderedColumnNames.Length; displayIndex++)
            {
                _songGrid.Columns[orderedColumnNames[displayIndex]]!.DisplayIndex = displayIndex;
            }
        }
        finally
        {
            _suspendSongGridOrderPersistence = false;
        }

        ConfigureSongGridColumnSizing();
    }

    private void PersistSongGridColumnOrder()
    {
        if (_suspendSongGridOrderPersistence || _songGrid.Columns.Count == 0)
        {
            return;
        }

        var columnNames = _songGrid.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Name != BulkSelectColumnName)
            .OrderBy(column => column.DisplayIndex)
            .Select(column => column.Name)
            .ToArray();
        BrowserConfigStore.SaveSongGridColumnOrder(columnNames);
    }

    private void ConfigureSongGridColumnSizing()
    {
        foreach (DataGridViewColumn column in _songGrid.Columns)
        {
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        }
    }

    private void UpdateResponsiveDetailGridLayout(DataGridView grid)
    {
        if (!IsResponsiveDetailGrid(grid) || grid.Columns.Count == 0)
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
            || ReferenceEquals(grid, _pluginsGrid)
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
        _clearSearchButton.Click += (_, _) => ClearFastKeywordSearch();
        _viewBackButton.Click += (_, _) => NavigateSongViewHistory(-1);
        _viewForwardButton.Click += (_, _) => NavigateSongViewHistory(1);
        _refreshButton.Click += (_, _) => RefreshCurrentView();
        _recentlyViewedMenu.Closed += (_, args) =>
        {
            if (args.CloseReason == ToolStripDropDownCloseReason.AppClicked
                && _recentlyViewedButton.ClientRectangle.Contains(
                    _recentlyViewedButton.PointToClient(Cursor.Position)))
            {
                _recentlyViewedMenuClosedOnButtonTick = Environment.TickCount64;
            }
        };
        _recentlyViewedButton.Click += (_, _) =>
        {
            if (_recentlyViewedMenu.Visible)
            {
                _recentlyViewedMenu.Close();
                return;
            }

            // Clicking the button can auto-close the menu before Button.Click fires.
            if (Environment.TickCount64 - _recentlyViewedMenuClosedOnButtonTick < 500)
            {
                _recentlyViewedMenuClosedOnButtonTick = 0;
                return;
            }

            RefreshRecentlyViewedMenu();
            if (_recentlyViewedMenu.Items.Count > 0)
            {
                _recentlyViewedMenu.Show(_recentlyViewedButton, new Point(0, _recentlyViewedButton.Height));
            }
        };
        _expandAllButton.Click += (_, _) => ExpandAllFolders();
        _collapseAllButton.Click += (_, _) => CollapseAllFolders();
        _filterByDateButton.Click += (_, _) => ShowSongAgeFilterDialog();
        _songGridColumnsButton.Click += (_, _) => _viewOptionsMenu.Show(_songGridColumnsButton, new Point(0, _songGridColumnsButton.Height));
        _saveCurrentViewMenuItem.Click += (_, _) => SaveCurrentSongLensView();
        _manageSavedViewsMenuItem.Click += (_, _) => ManageSavedSongLensViews();
        _restoreDefaultViewMenuItem.Click += (_, _) => RestoreDefaultSongLensView();
        _viewColumnsMenuItem.Click += (_, _) => ShowSongGridColumnsDialog();
        _bulkEditMenuItem.Click += (_, _) => ToggleBulkEditMode();
        _bulkSelectAllButton.Click += (_, _) => SetAllBulkSelections(true);
        _bulkClearButton.Click += (_, _) => SetAllBulkSelections(false);
        _bulkSetStatusButton.Click += (_, _) => ApplyBulkAnnotationValue(setStatus: true);
        _bulkSetGenreButton.Click += (_, _) => ApplyBulkAnnotationValue(setStatus: false);
        _bulkSetRatingButton.Click += (_, _) => ApplyBulkRating();
        _bulkDeleteButton.Click += (_, _) => DeleteSelectedSongFolders();
        _bulkDoneButton.Click += (_, _) => ToggleBulkEditMode();
        _statisticsButton.Click += (_, _) => ShowSongStatisticsDialog();
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
        _contextExportFolderCsvMenuItem.Click += (_, _) => ExportSelectedFolderToCsv();
        _contextExportFolderExcelMenuItem.Click += (_, _) => ExportSelectedFolderToExcel();
        _contextDeleteFolderMenuItem.Click += (_, _) => ConfirmDeleteSelectedFolder();
        _songGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_songGrid, SongGridKey);
        _songGrid.ColumnDisplayIndexChanged += (_, _) => PersistSongGridColumnOrder();
        _songGrid.ColumnHeaderMouseClick += (_, args) => HandleSongGridColumnHeaderClick(args.ColumnIndex);
        _songGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_songGrid.IsCurrentCellDirty
                && _songGrid.CurrentCell?.OwningColumn?.Name is StatusColumnName or GenreColumnName or BulkSelectColumnName)
            {
                _songGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _songGrid.CellValueChanged += (_, args) =>
        {
            if (args.ColumnIndex >= 0 && _songGrid.Columns[args.ColumnIndex].Name == BulkSelectColumnName)
                UpdateBulkSelectionCount();
            else
            {
                SaveSongAnnotationSelection(args.RowIndex, args.ColumnIndex);
                if (args.ColumnIndex >= 0)
                    QueueSongGridSortAfterValueChange(_songGrid.Columns[args.ColumnIndex].Name);
            }
        };
        _songGrid.RowsRemoved += (_, _) => { if (_bulkEditMode) UpdateBulkSelectionCount(); };
        _songGrid.CellPainting += DrawSongRatingCell;
        _songGrid.CellMouseClick += (_, args) => SetSongRatingFromClick(args);
        _rootPathComboBox.SelectionChangeCommitted += (_, _) => HandleRootPathSelectionCommitted();
        _summaryGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_summaryGrid, SummaryGridKey);
        _pluginsGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_pluginsGrid, PluginsGridKey);
        _trackGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_trackGrid, TrackGridKey);
        _trackGrid.CellPainting += DrawDisabledTrackIcon;
        _trackGrid.CellPainting += DrawTrackGridFolderIcon;
        _tracksWithEventsMenuItem.CheckedChanged += (_, _) => ToggleTracksWithEventsOnly();
        _hideDisabledTracksMenuItem.CheckedChanged += (_, _) => ToggleHideDisabledTracks();
        _hideFolderTracksMenuItem.CheckedChanged += (_, _) => ToggleHideFolderTracks();
        _groupGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_groupGrid, GroupGridKey);
        _mixerMainGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_mixerMainGrid, MixerMainGridKey);
        _mixerInsertGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_mixerInsertGrid, MixerInsertGridKey);
        _mixerSendGrid.ColumnWidthChanged += (_, _) => PersistGridColumnWidths(_mixerSendGrid, MixerSendGridKey);
        _viewFullMixerButton.Click += (_, _) => ShowFullMixer();
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
        _contextPlaySongMenuItem.Click += (_, _) => PlaySingleMixdownFromContextMenu();
        _songGridContextMenu.Opening += (_, _) => UpdateMixdownPlaybackMenu();
        _contextRenameSongMenuItem.Click += (_, _) => RenameSelectedSong();
        _contextRevealInExplorerMenuItem.Click += (_, _) => RevealSelectedSongInExplorer();
        _saveSnapshotMenuItem.Click += (_, _) => SaveSnapshot();
        _snapshotButton.Click += (_, _) => SaveSnapshot();
        _songGridHelpButton.Click += (_, _) => ShowSongGridHelpDialog();
        _exportCsvMenuItem.Click += (_, _) => ExportCurrentSongsToCsv();
        _exportExcelMenuItem.Click += (_, _) => ExportCurrentSongsToExcel();
        _exitMenuItem.Click += (_, _) => Close();
        _searchTextBox.KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.Escape)
            {
                ClearFastKeywordSearch();
                args.SuppressKeyPress = true;
                return;
            }

            if (args.KeyCode != Keys.Enter)
            {
                return;
            }

            SearchSongs();
            args.SuppressKeyPress = true;
        };
        _searchTextBox.TextChanged += (_, _) =>
        {
            _clearSearchButton.Enabled = _searchTextBox.TextLength > 0;
            if (!_suppressFastSearchTextChanged
                && _searchTextBox.TextLength == 0
                && _searchMode
                && _advancedSearchQuery is null)
            {
                ClearFastKeywordSearch();
            }
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
                SetFastSearchText("");
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

            if (args.Node.Tag is string searchFolderPath
                && !string.Equals(
                    _loadedFolderViewPath,
                    Path.GetFullPath(searchFolderPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                _searchMode = false;
                _advancedSearchQuery = null;
                _allSongsMode = false;
                SetFastSearchText("");
                _folderTree.SelectedNode = args.Node;
                if (!string.Equals(
                        _loadedFolderViewPath,
                        Path.GetFullPath(searchFolderPath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    LoadSongsForFolder(searchFolderPath, trackSingleSongRecent: true);
                }
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

            if (args.ColumnIndex >= 0
                && string.Equals(_songGrid.Columns[args.ColumnIndex].Name, PlayableColumnName, StringComparison.Ordinal))
            {
                if (_songGrid.Rows[args.RowIndex].Tag is SongGridRowData mixdownRowData)
                {
                    PlayMixdownFromIndicator(mixdownRowData.Metadata);
                }
                return;
            }

            if (args.ColumnIndex >= 0
                && _songGrid.Columns[args.ColumnIndex].Name is StatusColumnName or GenreColumnName or RatingColumnName or BulkSelectColumnName)
            {
                return;
            }

            if (_songGrid.Rows[args.RowIndex].Tag is SongGridRowData rowData)
            {
                var installedApplications = SongLaunchResolver.GetInstalledApplications();
                if (_songGridDoubleClickAction == SongGridDoubleClickAction.OpenSongInDaw
                    && installedApplications.Count == 1)
                {
                    OpenSongInSpecificApp(rowData.Metadata, installedApplications[0]);
                }
                else
                {
                    OpenSongInExplorer(rowData.Metadata);
                }
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
        _recentlyViewedMenu.Items.Clear();
        var recentSongPaths = BrowserConfigStore.LoadRecentSongPaths()
            .Where(File.Exists)
            .ToArray();

        _recentlyViewedButton.Enabled = recentSongPaths.Length > 0;

        if (recentSongPaths.Length == 0)
        {
            return;
        }

        foreach (var songPath in recentSongPaths)
        {
            var fileName = Path.GetFileName(songPath);
            _recentlyViewedMenu.Items.Add(CreateRecentSongMenuItem(fileName, songPath));
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

        BeginSongLensViewBuild("Building SongLens View...");
        try
        {
            var normalizedRootPath = Path.GetFullPath(path);
            var rootChanged = !string.Equals(_rootPath, normalizedRootPath, StringComparison.OrdinalIgnoreCase);
            if (rootChanged)
            {
                _songMetadataCache.Clear();
                InvalidateCatalogStatisticsCache();
                _songViewHistory.Clear();
                _songViewHistoryIndex = -1;
                _songViewHistoryDetached = false;
                _keywordSearchReturnFolderPath = null;
                UpdateSongViewHistoryButtons();
            }
            _rootPath = normalizedRootPath;
            _loadedFolderViewPath = null;
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
        finally
        {
            EndSongLensViewBuild();
        }
    }

    private void RefreshCurrentView()
    {
        if (string.IsNullOrWhiteSpace(_rootPath) || !Directory.Exists(_rootPath))
        {
            return;
        }

        var wasAllSongsMode = _allSongsMode;
        var wasSearchMode = _searchMode;
        var searchText = _searchTextBox.Text;
        var advancedSearchQuery = _advancedSearchQuery is null
            ? null
            : CloneAdvancedSearchQuery(_advancedSearchQuery);
        var preferredFolderPath = _folderTree.SelectedNode?.Tag as string
            ?? _selectedMetadata?.Folder
            ?? _rootPath;

        BeginSongLensViewBuild("Rebuilding SongLens View...");
        try
        {
            InvalidateCatalogStatisticsCache();
            RebuildFolderTree(preferredFolderPath);
            if (advancedSearchQuery is not null)
            {
                RunAdvancedSearch(advancedSearchQuery);
                return;
            }

            if (wasSearchMode && !string.IsNullOrWhiteSpace(searchText))
            {
                SetFastSearchText(searchText);
                SearchSongs();
                return;
            }

            if (wasAllSongsMode)
            {
                LoadAllSongs();
            }
        }
        finally
        {
            EndSongLensViewBuild();
        }
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

        SelectFirstFolderUnderRoot();
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
            rootNode.EnsureVisible();
        }

        SelectFirstFolderUnderRoot();
        SetStatus("Collapsed all folders.");
    }

    private void SelectFirstFolderUnderRoot()
    {
        if (_folderTree.Nodes.Count == 0 || _folderTree.Nodes[0] is not TreeNode rootNode)
        {
            return;
        }

        var rootNeedsPopulation = rootNode.Nodes.Count == 0
            || (rootNode.Nodes.Count == 1 && Equals(rootNode.Nodes[0].Tag, PlaceholderTag));
        if (rootNeedsPopulation)
        {
            PopulateFolderChildren(rootNode);
        }

        var firstFolderNode = rootNode.Nodes
            .Cast<TreeNode>()
            .FirstOrDefault(node => node.Tag is string);
        if (firstFolderNode?.Tag is not string folderPath)
        {
            return;
        }

        _searchMode = false;
        _advancedSearchQuery = null;
        _allSongsMode = false;
        SetFastSearchText("");
        UpdateStatusIndicators();

        _suppressFolderTreeSelectionLoad = true;
        try
        {
            _folderTree.SelectedNode = firstFolderNode;
            firstFolderNode.EnsureVisible();
        }
        finally
        {
            _suppressFolderTreeSelectionLoad = false;
        }

        LoadSongsForFolder(folderPath);
        EnsureSongFolderSelection();
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
        _contextExportFolderMenuItem.Enabled = node.Tag is string folderPath && Directory.Exists(folderPath);
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
            InvalidateCatalogStatisticsCache();
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

        BeginSongLensViewBuild("Loading songs...");
        try
        {
            _loadedFolderViewPath = Path.GetFullPath(folderPath);
            _allSongsMode = false;
            _songViewHistoryDetached = _songViewHistoryIndex >= 0;
            _keywordSearchReturnFolderPath = null;
            UpdateSongViewHistoryButtons();
            UpdateStatusIndicators();
            _songGrid.Rows.Clear();
            ClearDetailViews();
            _selectedMetadata = null;
            UpdateSnapshotActionsAvailability();
            UpdateSongGridHintVisibility();

            var skipped = 0;
            var queryRows = new List<SongQueryRow>();
            try
            {
                var songPaths = Directory.EnumerateFiles(folderPath, "*.song")
                    .Where(IsCatalogSongFile)
                    .Where(SongMatchesCurrentView)
                    .OrderBy(Path.GetFileName)
                    .ToArray();
                foreach (var songPath in songPaths)
                {
                    try
                    {
                        var metadata = ReadSongMetadata(songPath);
                        var annotation = BrowserConfigStore.LoadSongAnnotation(metadata.Path);
                        queryRows.Add(new SongQueryRow { Metadata = metadata, Annotation = annotation });
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
                SetStatus($"Could not build SongLens View: {ex.Message}");
                return;
            }

            foreach (var queryRow in OrderSongQueryRows(queryRows))
                AddSongRow(queryRow.Metadata, queryRow.Match);
            UpdateSongGridSortGlyphs();

            if (_songGrid.Rows.Count > 0)
            {
                var shouldTrackRecent = _songGrid.Rows.Count == 1
                    && (trackSingleSongRecent || RootHasSingleVisibleChoice(folderPath));
                SelectSongRow(0, trackRecent: shouldTrackRecent);
            }

            SetStatusForSelectedSongWarningOr(
                skipped > 0 ? $"Skipped {skipped} song(s) that could not be loaded." : "Ready");
        }
        finally
        {
            EndSongLensViewBuild();
        }
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
        // selected tree node. Clearing the query leaves the current results in place.
        var query = _searchTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            ClearFastKeywordSearch();
            return;
        }

        BeginSongLensViewBuild("Searching SongLens View...");
        try
        {
            if (!_searchMode && !_allSongsMode && _folderTree.SelectedNode?.Tag is string returnFolderPath)
            {
                _keywordSearchReturnFolderPath = returnFolderPath;
            }

            _advancedSearchQuery = null;
            _searchMode = true;
            _allSongsMode = false;
            _loadedFolderViewPath = null;
            UpdateStatusIndicators();
            _songGrid.Rows.Clear();
            ClearDetailViews();
            _selectedMetadata = null;
            UpdateSongGridHintVisibility();
            SetStatus("Searching SongLens View...");

            var count = 0;
            var skipped = 0;
            var queryRows = new List<SongQueryRow>();
            var matchedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var recognizedStatus = _songStatuses.Any(
                    status => string.Equals(status, query, StringComparison.OrdinalIgnoreCase))
                && BrowserConfigStore.HasSongStatusAssignment(query);
            var recognizedGenre = _songGenres.Any(
                    genre => string.Equals(genre, query, StringComparison.OrdinalIgnoreCase))
                && BrowserConfigStore.HasSongGenreAssignment(query);
            var searchAnnotationsOnly = recognizedStatus || recognizedGenre;
            // Fast Catalog Search is catalog-wide, then composes with the active
            // Quick Filter so every grid entry point follows the same filter state.
            foreach (var songPath in Directory.EnumerateFiles(_rootPath, "*.song", SearchOption.AllDirectories)
                         .Where(IsCatalogSongFile)
                         .Where(SongMatchesCurrentView)
                         .OrderBy(path => path))
            {
                try
                {
                    var metadata = ReadSongMetadata(songPath);
                    var annotation = BrowserConfigStore.LoadSongAnnotation(metadata.Path);
                    var match = GetSongAnnotationSearchMatch(
                        metadata,
                        annotation,
                        query,
                        recognizedStatus,
                        recognizedGenre,
                        exactMatch: searchAnnotationsOnly);
                    if (match is null && !searchAnnotationsOnly)
                    {
                        match = SongMetadataReader.GetSearchMatch(metadata, query);
                    }
                    if (match is null)
                    {
                        continue;
                    }

                    queryRows.Add(new SongQueryRow { Metadata = metadata, Annotation = annotation, Match = match });
                    matchedFolders.Add(metadata.Folder);
                    count++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    LogSongReadFailure("SearchSongs", songPath, ex);
                }
            }

            foreach (var queryRow in OrderSongQueryRows(queryRows))
                AddSongRow(queryRow.Metadata, queryRow.Match);
            UpdateSongGridSortGlyphs();

            if (_songGrid.Rows.Count > 0)
            {
                UpdateSongGridSortGlyphs();
                SelectSongRow(0);
            }
            else
            {
                UpdateSnapshotActionsAvailability();
            }

            if (matchedFolders.Count == 1)
            {
                FocusTreeOnFolder(matchedFolders.First());
            }

            SetStatusForSelectedSongWarningOr(
                skipped > 0 ? $"Found {count} song(s) matching '{query}'. Skipped {skipped} file(s)." : "Ready");
            CommitCurrentSongViewToHistory();
        }
        finally
        {
            EndSongLensViewBuild();
        }
    }

    private SearchResult? GetSongAnnotationSearchMatch(
        SongMetadata metadata,
        SongAnnotation annotation,
        string query,
        bool searchStatus,
        bool searchGenre,
        bool exactMatch)
    {
        var fields = new List<(string Label, string? Value, bool Enabled)>
        {
            (StatusColumnName, annotation.Status, !exactMatch || searchStatus),
            (GenreColumnName, annotation.Genre, !exactMatch || searchGenre)
        };
        if (!exactMatch && annotation.CustomValues is not null)
        {
            fields.AddRange(_customSongColumns.Select(column => (
                column.Name,
                annotation.CustomValues.GetValueOrDefault(column.Id),
                true)));
        }

        foreach (var field in fields)
        {
            if (!field.Enabled || string.IsNullOrWhiteSpace(field.Value))
            {
                continue;
            }

            var matches = exactMatch
                ? string.Equals(field.Value, query, StringComparison.OrdinalIgnoreCase)
                : SongMetadataReader.ContainsWholeWord(field.Value, query);
            if (matches)
            {
                return new SearchResult
                {
                    Metadata = metadata,
                    MatchField = field.Label,
                    MatchValue = field.Value
                };
            }
        }

        return null;
    }

    private void RunAdvancedSearch(AdvancedSearchQuery query)
    {
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            return;
        }

        BeginSongLensViewBuild("Searching SongLens View...");
        try
        {
            _advancedSearchQuery = CloneAdvancedSearchQuery(query);
            _lastAdvancedSearchQuery = CloneAdvancedSearchQuery(query);
            _searchMode = true;
            _allSongsMode = false;
            _loadedFolderViewPath = null;
            _keywordSearchReturnFolderPath = null;
            SetFastSearchText("");
            UpdateStatusIndicators();
            _songGrid.Rows.Clear();
            ClearDetailViews();
            _selectedMetadata = null;
            UpdateSnapshotActionsAvailability();
            UpdateSongGridHintVisibility();
            SetStatus("Running advanced search...");

            var count = 0;
            var skipped = 0;
            var queryRows = new List<SongQueryRow>();
            var matchedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var songPath in Directory.EnumerateFiles(_rootPath, "*.song", SearchOption.AllDirectories)
                         .Where(IsCatalogSongFile)
                         .Where(SongMatchesCurrentView)
                         .OrderBy(path => path))
            {
                try
                {
                    var metadata = ReadSongMetadata(songPath);
                    var annotation = BrowserConfigStore.LoadSongAnnotation(metadata.Path);
                    var match = query.Rules.Count == 0
                        ? null
                        : AdvancedSongSearch.GetMatch(
                            metadata,
                            query,
                            new AdvancedSearchAnnotationValues
                            {
                                Status = annotation.Status,
                                Genre = annotation.Genre,
                                Rating = annotation.Rating
                            });
                    if (query.Rules.Count > 0 && match is null)
                    {
                        continue;
                    }

                    queryRows.Add(new SongQueryRow { Metadata = metadata, Annotation = annotation, Match = match });
                    matchedFolders.Add(metadata.Folder);
                    count++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    LogSongReadFailure("RunAdvancedSearch", songPath, ex);
                }
            }

            foreach (var queryRow in OrderSongQueryRows(queryRows))
                AddSongRow(queryRow.Metadata, queryRow.Match);
            UpdateSongGridSortGlyphs();

            if (_songGrid.Rows.Count > 0)
            {
                UpdateSongGridSortGlyphs();
                SelectSongRow(0);
            }

            if (matchedFolders.Count == 1)
            {
                FocusTreeOnFolder(matchedFolders.First());
            }

            SetStatusForSelectedSongWarningOr(
                skipped > 0 ? $"Advanced search found {count} song(s). Skipped {skipped} file(s)." : "Ready");
            CommitCurrentSongViewToHistory();
        }
        finally
        {
            EndSongLensViewBuild();
        }
    }

    private void LoadAllSongs()
    {
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            return;
        }

        BeginSongLensViewBuild("Building SongLens View...");
        try
        {
            _searchMode = false;
            _advancedSearchQuery = null;
            _allSongsMode = true;
            _loadedFolderViewPath = null;
            _keywordSearchReturnFolderPath = null;
            UpdateStatusIndicators();
            SetFastSearchText("");
            _songGrid.Rows.Clear();
            ClearDetailViews();
            _selectedMetadata = null;
            UpdateSnapshotActionsAvailability();
            UpdateSongGridHintVisibility();
            SetStatus("Building SongLens View...");

            var skipped = 0;
            var queryRows = new List<SongQueryRow>();
            foreach (var songPath in Directory.EnumerateFiles(_rootPath, "*.song", SearchOption.AllDirectories)
                         .Where(IsCatalogSongFile)
                         .Where(SongMatchesCurrentView)
                         .OrderBy(path => path))
            {
                try
                {
                    var metadata = ReadSongMetadata(songPath);
                    var annotation = BrowserConfigStore.LoadSongAnnotation(metadata.Path);
                    queryRows.Add(new SongQueryRow { Metadata = metadata, Annotation = annotation });
                }
                catch (Exception ex)
                {
                    skipped++;
                    LogSongReadFailure("LoadAllSongs", songPath, ex);
                }
            }

            foreach (var queryRow in OrderSongQueryRows(queryRows))
                AddSongRow(queryRow.Metadata, queryRow.Match);
            UpdateSongGridSortGlyphs();

            if (_songGrid.Rows.Count > 0)
            {
                SelectSongRow(0);
            }

            SetStatusForSelectedSongWarningOr(
                skipped > 0 ? $"Skipped {skipped} song(s) that could not be loaded." : "Ready");
            CommitCurrentSongViewToHistory();
        }
        finally
        {
            EndSongLensViewBuild();
        }
    }

    private void SetFastSearchText(string text)
    {
        _suppressFastSearchTextChanged = true;
        try
        {
            _searchTextBox.Text = text;
            _clearSearchButton.Enabled = text.Length > 0;
        }
        finally
        {
            _suppressFastSearchTextChanged = false;
        }
    }

    private void ClearFastKeywordSearch()
    {
        SetFastSearchText("");
    }

    private void CommitCurrentSongViewToHistory()
    {
        if (_applyingSongViewHistory)
        {
            return;
        }

        var entry = CreateCurrentSongViewHistoryEntry();
        if (entry is null)
        {
            return;
        }

        if (_songViewHistoryIndex >= 0
            && _songViewHistoryIndex < _songViewHistory.Count
            && string.Equals(_songViewHistory[_songViewHistoryIndex].Key, entry.Key, StringComparison.Ordinal))
        {
            _songViewHistory[_songViewHistoryIndex].SelectedSongPath = entry.SelectedSongPath;
            _songViewHistory[_songViewHistoryIndex].ReturnFolderPath = entry.ReturnFolderPath;
            _songViewHistoryDetached = false;
            UpdateSongViewHistoryButtons();
            return;
        }

        if (_songViewHistoryIndex + 1 < _songViewHistory.Count)
        {
            _songViewHistory.RemoveRange(
                _songViewHistoryIndex + 1,
                _songViewHistory.Count - _songViewHistoryIndex - 1);
        }

        _songViewHistory.Add(entry);
        if (_songViewHistory.Count > SongViewHistoryLimit)
        {
            _songViewHistory.RemoveAt(0);
        }

        _songViewHistoryIndex = _songViewHistory.Count - 1;
        _songViewHistoryDetached = false;
        UpdateSongViewHistoryButtons();
    }

    private SongViewHistoryEntry? CreateCurrentSongViewHistoryEntry()
    {
        var dateFilter = CloneSongAgeFilter(_songAgeFilter);
        var filterKey = JsonSerializer.Serialize(dateFilter);
        if (_advancedSearchQuery is not null)
        {
            var advancedQuery = CloneAdvancedSearchQuery(_advancedSearchQuery);
            return new SongViewHistoryEntry
            {
                Kind = SongViewHistoryKind.AdvancedSearch,
                Key = $"advanced|{filterKey}|{JsonSerializer.Serialize(advancedQuery)}",
                Description = "Advanced search",
                AdvancedQuery = advancedQuery,
                DateFilter = dateFilter,
                SelectedSongPath = _selectedMetadata?.Path
            };
        }

        if (_searchMode)
        {
            var keyword = _searchTextBox.Text.Trim();
            if (keyword.Length == 0)
            {
                return null;
            }

            return new SongViewHistoryEntry
            {
                Kind = SongViewHistoryKind.KeywordSearch,
                Key = $"keyword|{filterKey}|{keyword.ToUpperInvariant()}",
                Description = $"Search: {keyword}",
                Keyword = keyword,
                DateFilter = dateFilter,
                SelectedSongPath = _selectedMetadata?.Path,
                ReturnFolderPath = _keywordSearchReturnFolderPath
            };
        }

        if (!_allSongsMode)
        {
            return null;
        }

        return new SongViewHistoryEntry
        {
            Kind = SongViewHistoryKind.AllSongs,
            Key = $"all|{filterKey}",
            Description = dateFilter is null ? "All songs" : BuildSongFilterStatusMessage(dateFilter),
            DateFilter = dateFilter,
            SelectedSongPath = _selectedMetadata?.Path
        };
    }

    private void NavigateSongViewHistory(int direction)
    {
        if (direction == 0 || _songViewHistory.Count == 0)
        {
            return;
        }

        CaptureCurrentSongViewHistorySelection();
        var targetIndex = _songViewHistoryDetached && direction < 0
            ? _songViewHistoryIndex
            : _songViewHistoryIndex + Math.Sign(direction);
        if (targetIndex < 0 || targetIndex >= _songViewHistory.Count)
        {
            return;
        }

        _songViewHistoryIndex = targetIndex;
        RestoreSongViewHistoryEntry(_songViewHistory[targetIndex]);
    }

    private void RestoreSongViewHistoryEntry(SongViewHistoryEntry entry)
    {
        _applyingSongViewHistory = true;
        try
        {
            _songAgeFilter = CloneSongAgeFilter(entry.DateFilter);
            _keywordSearchReturnFolderPath = entry.ReturnFolderPath;
            switch (entry.Kind)
            {
                case SongViewHistoryKind.KeywordSearch:
                    _searchMode = true;
                    _allSongsMode = false;
                    _advancedSearchQuery = null;
                    SetFastSearchText(entry.Keyword ?? "");
                    SearchSongs();
                    break;
                case SongViewHistoryKind.AdvancedSearch when entry.AdvancedQuery is not null:
                    RunAdvancedSearch(CloneAdvancedSearchQuery(entry.AdvancedQuery));
                    break;
                default:
                    LoadAllSongs();
                    break;
            }

            if (!string.IsNullOrWhiteSpace(entry.SelectedSongPath))
            {
                TrySelectSongRowByPath(entry.SelectedSongPath);
            }

            _songViewHistoryDetached = false;
            UpdateStatusIndicators();
        }
        finally
        {
            _applyingSongViewHistory = false;
            UpdateSongViewHistoryButtons();
        }
    }

    private void CaptureCurrentSongViewHistorySelection()
    {
        if (_songViewHistoryDetached
            || _songViewHistoryIndex < 0
            || _songViewHistoryIndex >= _songViewHistory.Count)
        {
            return;
        }

        _songViewHistory[_songViewHistoryIndex].SelectedSongPath = _selectedMetadata?.Path;
    }

    private void UpdateSongViewHistoryButtons()
    {
        if (_viewBackButton.IsDisposed || _viewForwardButton.IsDisposed)
        {
            return;
        }

        var currentEntry = _songViewHistoryIndex >= 0 && _songViewHistoryIndex < _songViewHistory.Count
            ? _songViewHistory[_songViewHistoryIndex]
            : null;
        _viewBackButton.Enabled = _songViewHistoryDetached
            ? _songViewHistoryIndex >= 0
            : _songViewHistoryIndex > 0;
        _viewForwardButton.Enabled = !_songViewHistoryDetached
            && _songViewHistoryIndex >= 0
            && _songViewHistoryIndex < _songViewHistory.Count - 1;

        var backDescription = _songViewHistoryDetached && currentEntry is not null
            ? currentEntry.Description
            : _songViewHistoryIndex > 0
                ? _songViewHistory[_songViewHistoryIndex - 1].Description
                : "No previous view";
        var forwardDescription = _songViewHistoryIndex >= 0 && _songViewHistoryIndex < _songViewHistory.Count - 1
            ? _songViewHistory[_songViewHistoryIndex + 1].Description
            : "No next view";
        _toolTip.SetToolTip(_viewBackButton, $"Back: {backDescription}");
        _toolTip.SetToolTip(_viewForwardButton, $"Forward: {forwardDescription}");
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

    private void ExportSelectedFolderToCsv()
    {
        var folderPath = GetSelectedExportFolderPath();
        if (folderPath is not null)
        {
            ExportCurrentSongsToCsv(folderPath);
        }
    }

    private void ExportSelectedFolderToExcel()
    {
        var folderPath = GetSelectedExportFolderPath();
        if (folderPath is not null)
        {
            ExportCurrentSongsToExcel(folderPath);
        }
    }

    private void ExportCurrentSongsToCsv(string? folderPathOverride = null)
    {
        List<SongGridRowData> exportRows;
        bool wasCanceled;
        bool exportingEntireLibrary;
        bool exportingCurrentSong;
        string? exportingFolderPath;
        if (folderPathOverride is not null)
        {
            wasCanceled = false;
            exportingEntireLibrary = false;
            exportingCurrentSong = false;
            exportingFolderPath = folderPathOverride;
            exportRows = CollectSongsForCsvExport(CsvExportScope.SelectedFolder, folderPathOverride);
        }
        else
        {
            exportRows = DetermineExportRows(
                "Export CSV",
                out wasCanceled,
                out exportingEntireLibrary,
                out exportingCurrentSong,
                out exportingFolderPath);
        }
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

        ShowMixerExportInformationIfNeeded(optionsDialog.SelectedFields, isExcelWorkbook: false);

        using var dialog = new SaveFileDialog
        {
            Title = "Export songs to CSV",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = "csv",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = BuildDefaultCsvFileName(
                exportingEntireLibrary,
                exportingCurrentSong,
                exportingFolderPath)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        try
        {
            var selectedMixerFieldKeys = optionsDialog.SelectedFields
                .Where(IsMixerCsvField)
                .Select(field => field.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var catalogFields = optionsDialog.SelectedFields
                .Where(field => !IsMixerCsvField(field))
                .ToList();
            if (catalogFields.Count == 0)
            {
                catalogFields.Add(_csvExportFields.First(field => field.Key == "song"));
            }

            var builder = new StringBuilder();
            AppendCsvRow(builder, catalogFields.Select(field => field.Label).ToArray());

            foreach (var rowData in exportRows)
            {
                var metadata = rowData.Metadata;
                AppendCsvRow(
                    builder,
                    catalogFields
                        .Select(field => field.ValueSelector(metadata, rowData.Match))
                        .ToArray());
            }

            File.WriteAllText(dialog.FileName, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            if (selectedMixerFieldKeys.Count > 0)
            {
                var mixerDetailsPath = BuildMixerDetailsCsvPath(dialog.FileName);
                var mixerDetails = BuildMixerDetailsCsv(exportRows, selectedMixerFieldKeys);
                File.WriteAllText(mixerDetailsPath, mixerDetails, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                SetStatus($"Exported {exportRows.Count} song(s) and mixer details to {mixerDetailsPath}");
            }
            else
            {
                SetStatus($"Exported {exportRows.Count} song(s) to {dialog.FileName}");
            }
        }
        catch (Exception ex)
        {
            using var messageDialog = new ThemedMessageForm(Text, $"Could not export CSV:\n{ex.Message}", _theme, ThemedMessageKind.Error);
            messageDialog.ShowDialog(this);
            SetStatus($"CSV export failed: {ex.Message}");
        }
    }

    private void ExportCurrentSongsToExcel(string? folderPathOverride = null)
    {
        List<SongGridRowData> exportRows;
        bool wasCanceled;
        bool exportingEntireLibrary;
        bool exportingCurrentSong;
        string? exportingFolderPath;
        if (folderPathOverride is not null)
        {
            wasCanceled = false;
            exportingEntireLibrary = false;
            exportingCurrentSong = false;
            exportingFolderPath = folderPathOverride;
            exportRows = CollectSongsForCsvExport(CsvExportScope.SelectedFolder, folderPathOverride);
        }
        else
        {
            exportRows = DetermineExportRows(
                "Export Excel Workbook",
                out wasCanceled,
                out exportingEntireLibrary,
                out exportingCurrentSong,
                out exportingFolderPath);
        }
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

        using var optionsDialog = new CsvExportOptionsForm(
            _csvExportFields,
            _theme,
            BrowserConfigStore.LoadCsvExportFieldKeys(),
            "Export Excel Workbook",
            "Choose Catalog worksheet fields.\nMixer selections create a linked Mixer Details worksheet.");
        if (optionsDialog.ShowDialog(this) != DialogResult.OK || optionsDialog.SelectedFields.Count == 0)
        {
            return;
        }

        ShowMixerExportInformationIfNeeded(optionsDialog.SelectedFields, isExcelWorkbook: true);

        using var dialog = new SaveFileDialog
        {
            Title = "Export songs to Excel",
            Filter = "Excel workbooks (*.xlsx)|*.xlsx|All files (*.*)|*.*",
            DefaultExt = "xlsx",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = Path.ChangeExtension(
                BuildDefaultCsvFileName(
                    exportingEntireLibrary,
                    exportingCurrentSong,
                    exportingFolderPath),
                ".xlsx")
        };
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        try
        {
            var selectedMixerFieldKeys = optionsDialog.SelectedFields
                .Where(IsMixerCsvField)
                .Select(field => field.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var catalogFields = optionsDialog.SelectedFields
                .Where(field => !IsMixerCsvField(field))
                .ToList();
            var songField = _csvExportFields.First(field => field.Key == "song");
            if (!catalogFields.Any(field => field.Key == songField.Key))
            {
                catalogFields.Insert(0, songField);
            }

            var catalogRows = new List<IReadOnlyList<string?>>
            {
                catalogFields.Select(field => (string?)field.Label).ToArray()
            };
            catalogRows.AddRange(exportRows.Select(rowData =>
                (IReadOnlyList<string?>)catalogFields
                    .Select(field => field.ValueSelector(rowData.Metadata, rowData.Match))
                    .ToArray()));

            var worksheets = new List<ExcelWorksheetData>();
            var catalogLinks = new List<ExcelCellLink>();
            var mixerRows = BuildMixerDetailRows(exportRows, selectedMixerFieldKeys);
            if (mixerRows.Count > 0)
            {
                var mixerRangeBySongPath = mixerRows
                    .Select((row, index) => new { row.SongPath, WorksheetRow = index + 2 })
                    .GroupBy(item => item.SongPath, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group => group.Key,
                        group => (FirstRow: group.Min(item => item.WorksheetRow), LastRow: group.Max(item => item.WorksheetRow)),
                        StringComparer.OrdinalIgnoreCase);

                var songColumn = catalogFields.FindIndex(field => field.Key == "song") + 1;
                for (var index = 0; index < exportRows.Count; index++)
                {
                    if (mixerRangeBySongPath.TryGetValue(exportRows[index].Metadata.Path, out var mixerRange))
                    {
                        catalogLinks.Add(new ExcelCellLink(
                            index + 2,
                            songColumn,
                            "Mixer Details",
                            mixerRange.FirstRow,
                            mixerRange.LastRow,
                            MixerDetailHeaders.Length));
                    }
                }
            }

            worksheets.Add(new ExcelWorksheetData
            {
                Name = "Catalog",
                Rows = catalogRows,
                Links = catalogLinks
            });
            if (mixerRows.Count > 0)
            {
                var mixerWorksheetRows = new List<IReadOnlyList<string?>> { MixerDetailHeaders };
                mixerWorksheetRows.AddRange(mixerRows.Select(row => (IReadOnlyList<string?>)row.Values));
                worksheets.Add(new ExcelWorksheetData
                {
                    Name = "Mixer Details",
                    Rows = mixerWorksheetRows
                });
            }

            ExcelWorkbookWriter.Write(dialog.FileName, worksheets);
            SetStatus($"Exported {exportRows.Count} song(s) to {dialog.FileName}");
        }
        catch (Exception ex)
        {
            using var messageDialog = new ThemedMessageForm(Text, $"Could not export Excel workbook:\n{ex.Message}", _theme, ThemedMessageKind.Error);
            messageDialog.ShowDialog(this);
            SetStatus($"Excel export failed: {ex.Message}");
        }
    }

    private void SaveSnapshot()
    {
        if (IsSongGridResultsView())
        {
            if (_songGrid.Rows.Count > 1 && _selectedMetadata is not null)
            {
                using var scopeDialog = new SnapshotScopeForm(
                    _selectedMetadata.FileName,
                    GetSnapshotCurrentResultsLocation(),
                    _songGrid.Rows.Count,
                    _theme);
                if (scopeDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (scopeDialog.SelectedResultsView == SnapshotResultsView.List)
                {
                    var rows = scopeDialog.SelectedScope == SnapshotScope.CurrentResults
                        ? GetSongGridSnapshotRows()
                        : GetSelectedSongGridSnapshotRows();
                    PreviewSongGridResultsSnapshot(rows);
                    return;
                }

                if (scopeDialog.SelectedScope == SnapshotScope.CurrentResults)
                {
                    RunDetailedSongGridSnapshotWorkflow();
                    return;
                }
            }
            else
            {
                PreviewSongGridResultsSnapshot();
                return;
            }
        }

        if (_selectedMetadata is null)
        {
            using var messageDialog = new ThemedMessageForm(Text, "Select a song before saving a snapshot.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        var selection = CreateSnapshotSectionSelection();
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

    private bool IsSongGridResultsView()
    {
        return (_searchMode && _advancedSearchQuery is not null) || _songGrid.Rows.Count > 1;
    }

    private string GetSnapshotCurrentResultsLocation()
    {
        if (_searchMode)
        {
            return "the Current Search Result";
        }

        return !_allSongsMode && _folderTree.SelectedNode?.Tag is string
            ? "the Current Folder"
            : "Current Results";
    }

    private IReadOnlyList<DataGridViewRow> GetSongGridSnapshotRows()
    {
        return _songGrid.Rows
            .Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .ToArray();
    }

    private IReadOnlyList<DataGridViewRow> GetSelectedSongGridSnapshotRows()
    {
        if (_selectedMetadata is null)
        {
            return [];
        }

        return _songGrid.Rows
            .Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Where(row => row.Tag is SongGridRowData rowData
                && string.Equals(rowData.Metadata.Path, _selectedMetadata.Path, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private void PreviewSongGridResultsSnapshot(IReadOnlyList<DataGridViewRow>? rows = null)
    {
        var snapshotRows = rows ?? GetSongGridSnapshotRows();
        if (snapshotRows.Count == 0)
        {
            using var messageDialog = new ThemedMessageForm(Text, "There are no SongLens View results to snapshot.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        using var previewDialog = new SnapshotPreviewForm(
            BuildSongGridResultsSnapshotText(snapshotRows),
            BuildSongGridResultsSnapshotJson(snapshotRows),
            SnapshotFormat.Text,
            _theme);
        previewDialog.ShowDialog(this);
        if (previewDialog.SaveRequestedFormat is SnapshotFormat requestedFormat)
        {
            SaveSongGridResultsSnapshot(requestedFormat, snapshotRows);
        }
    }

    private void RunDetailedSongGridSnapshotWorkflow()
    {
        var songs = GetSongGridSnapshotMetadata();
        if (songs.Count == 0)
        {
            using var messageDialog = new ThemedMessageForm(Text, "There are no songs to snapshot.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        var selection = CreateSnapshotSectionSelection();
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
                using var previewDialog = new SnapshotPreviewForm(
                    BuildDetailedSongGridSnapshotText(songs, selection),
                    BuildDetailedSongGridSnapshotJson(songs, selection),
                    selection.Format,
                    _theme);
                previewDialog.ShowDialog(this);
                if (previewDialog.SaveRequestedFormat is SnapshotFormat requestedFormat)
                {
                    SaveDetailedSongGridSnapshot(songs, selection, requestedFormat);
                    return;
                }

                continue;
            }

            SaveDetailedSongGridSnapshot(songs, selection, selection.Format);
            return;
        }
    }

    private IReadOnlyList<SongMetadata> GetSongGridSnapshotMetadata()
    {
        return _songGrid.Rows
            .Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Select(row => row.Tag as SongGridRowData)
            .Where(rowData => rowData is not null)
            .Select(rowData => rowData!.Metadata)
            .ToArray();
    }

    private string BuildDetailedSongGridSnapshotText(
        IReadOnlyList<SongMetadata> songs,
        SnapshotSectionSelection selection)
    {
        const string separator = "================================================================================";
        var builder = new StringBuilder();
        builder.AppendLine("SongLens Detailed Snapshot");
        builder.Append("Captured: ");
        builder.AppendLine(DateTime.Now.ToString());
        builder.Append("Songs: ");
        builder.AppendLine(songs.Count.ToString());

        for (var index = 0; index < songs.Count; index++)
        {
            builder.AppendLine();
            builder.AppendLine(separator);
            builder.Append("Song ");
            builder.Append(index + 1);
            builder.Append(" of ");
            builder.AppendLine(songs.Count.ToString());
            builder.AppendLine(separator);
            builder.AppendLine();
            builder.AppendLine(BuildSnapshotPreviewText(songs[index], selection, includeDocumentHeader: false));
        }

        return builder.ToString().TrimEnd();
    }

    private string BuildDetailedSongGridSnapshotJson(
        IReadOnlyList<SongMetadata> songs,
        SnapshotSectionSelection selection)
    {
        var snapshot = new
        {
            SnapshotVersion = 1,
            App = "SongLens",
            View = "Detail",
            CapturedAt = DateTime.Now.ToString(),
            SongCount = songs.Count,
            Songs = songs.Select(song => BuildSnapshot(song, selection)).ToArray()
        };

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    private void SaveDetailedSongGridSnapshot(
        IReadOnlyList<SongMetadata> songs,
        SnapshotSectionSelection selection,
        SnapshotFormat format)
    {
        var isJson = format == SnapshotFormat.Json;
        using var dialog = new SaveFileDialog
        {
            Title = "Save detailed snapshot",
            Filter = isJson
                ? "JSON files (*.json)|*.json|All files (*.*)|*.*"
                : "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = isJson ? "json" : "txt",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = $"SongLens-detailed-snapshot-{DateTime.Now:yyyyMMdd-HHmmss}.{(isJson ? "json" : "txt")}"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        try
        {
            var contents = isJson
                ? BuildDetailedSongGridSnapshotJson(songs, selection)
                : BuildDetailedSongGridSnapshotText(songs, selection);
            File.WriteAllText(dialog.FileName, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            SetStatus($"Saved detailed snapshot for {songs.Count} song(s) to {dialog.FileName}");
        }
        catch (Exception ex)
        {
            using var messageDialog = new ThemedMessageForm(
                Text,
                $"Could not save detailed snapshot:\n{ex.Message}",
                _theme,
                ThemedMessageKind.Error);
            messageDialog.ShowDialog(this);
            SetStatus($"Detailed snapshot save failed: {ex.Message}");
        }
    }

    private void SaveSongGridResultsSnapshot(
        SnapshotFormat format,
        IReadOnlyList<DataGridViewRow>? rows = null)
    {
        var snapshotRows = rows ?? GetSongGridSnapshotRows();
        var isJson = format == SnapshotFormat.Json;
        using var dialog = new SaveFileDialog
        {
            Title = "Save SongLens View results snapshot",
            Filter = isJson
                ? "JSON files (*.json)|*.json|All files (*.*)|*.*"
                : "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = isJson ? "json" : "txt",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = $"SongLens-view-snapshot-{DateTime.Now:yyyyMMdd-HHmmss}.{(isJson ? "json" : "txt")}"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        try
        {
            var contents = isJson
                ? BuildSongGridResultsSnapshotJson(snapshotRows)
                : BuildSongGridResultsSnapshotText(snapshotRows);
            File.WriteAllText(dialog.FileName, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            SetStatus($"Saved SongLens View snapshot to {dialog.FileName}");
        }
        catch (Exception ex)
        {
            using var messageDialog = new ThemedMessageForm(Text, $"Could not save SongLens View snapshot:\n{ex.Message}", _theme, ThemedMessageKind.Error);
            messageDialog.ShowDialog(this);
            SetStatus($"SongLens View snapshot failed: {ex.Message}");
        }
    }

    private IReadOnlyList<DataGridViewColumn> GetVisibleSongGridColumnsInDisplayOrder()
    {
        return _songGrid.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Visible && column.Name != BulkSelectColumnName)
            .OrderBy(column => column.DisplayIndex)
            .ToArray();
    }

    private string GetSongGridColumnLabel(DataGridViewColumn column)
    {
        return _songGridColumnFields.FirstOrDefault(
                   field => string.Equals(field.ColumnName, column.Name, StringComparison.OrdinalIgnoreCase))
               ?.Label
            ?? column.HeaderText;
    }

    private static string GetSnapshotGridCellText(DataGridViewRow row, DataGridViewColumn column)
    {
        if (string.Equals(column.Name, RatingColumnName, StringComparison.Ordinal)
            && int.TryParse(row.Cells[column.Index].Value?.ToString(), out var rating))
        {
            return FormatStarRating(rating);
        }

        return (row.Cells[column.Index].FormattedValue?.ToString() ?? "")
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal);
    }

    private string BuildSongGridResultsSnapshotText(IReadOnlyList<DataGridViewRow> rows)
    {
        var columns = GetVisibleSongGridColumnsInDisplayOrder();
        var widths = columns
            .Select(column => Math.Max(
                GetSongGridColumnLabel(column).Length,
                rows.Select(row => GetSnapshotGridCellText(row, column).Length).DefaultIfEmpty(0).Max()))
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("SongLens View Results Snapshot");
        builder.Append("Captured: ");
        builder.AppendLine(DateTime.Now.ToString());
        builder.Append("Results: ");
        builder.AppendLine(rows.Count.ToString());
        builder.AppendLine();
        builder.AppendLine(string.Join("  ", columns.Select((column, index) => GetSongGridColumnLabel(column).PadRight(widths[index]))));
        builder.AppendLine(string.Join("  ", widths.Select(width => new string('-', width))));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join("  ", columns.Select((column, index) => GetSnapshotGridCellText(row, column).PadRight(widths[index]))));
        }

        return builder.ToString().TrimEnd();
    }

    private string BuildSongGridResultsSnapshotJson(IReadOnlyList<DataGridViewRow> snapshotRows)
    {
        var columns = GetVisibleSongGridColumnsInDisplayOrder();
        var rows = snapshotRows
            .Select(row => columns.ToDictionary(
                column => GetSongGridColumnLabel(column),
                column => GetSnapshotGridCellText(row, column)))
            .ToArray();
        var snapshot = new
        {
            SnapshotVersion = 1,
            App = "SongLens",
            CapturedAt = DateTime.Now.ToString(),
            ResultCount = rows.Length,
            Columns = columns.Select(GetSongGridColumnLabel).ToArray(),
            Rows = rows
        };

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
    }

    private List<SongGridRowData> DetermineExportRows(
        string exportTitle,
        out bool wasCanceled,
        out bool exportingEntireLibrary,
        out bool exportingCurrentSong,
        out string? exportingFolderPath)
    {
        wasCanceled = false;
        exportingEntireLibrary = false;
        exportingCurrentSong = false;
        exportingFolderPath = null;

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

        var selectedFolderPath = GetSelectedExportFolderPath();
        if (_selectedMetadata is not null || selectedFolderPath is not null)
        {
            var selectedFolderName = selectedFolderPath is null
                ? null
                : _folderTree.SelectedNode?.Text ?? Path.GetFileName(selectedFolderPath);
            using var scopeDialog = new CsvExportScopeForm(
                GetCatalogRootName(),
                selectedFolderName,
                CanExportSelectedFolderAsDistinctScope(selectedFolderPath),
                _selectedMetadata?.FileName,
                _theme,
                exportTitle);
            if (scopeDialog.ShowDialog(this) != DialogResult.OK)
            {
                wasCanceled = true;
                return [];
            }

            exportingEntireLibrary = scopeDialog.SelectedScope == CsvExportScope.EntireLibrary;
            exportingCurrentSong = scopeDialog.SelectedScope == CsvExportScope.CurrentSong;
            exportingFolderPath = scopeDialog.SelectedScope == CsvExportScope.SelectedFolder
                ? selectedFolderPath
                : null;
            return CollectSongsForCsvExport(scopeDialog.SelectedScope, exportingFolderPath);
        }

        exportingEntireLibrary = true;
        return CollectSongsForCsvExport(CsvExportScope.EntireLibrary);
    }

    private string GetCatalogRootName()
    {
        if (_folderTree.Nodes.Count > 0 && !string.IsNullOrWhiteSpace(_folderTree.Nodes[0].Text))
        {
            return _folderTree.Nodes[0].Text;
        }

        var rootName = Path.GetFileName(Path.TrimEndingDirectorySeparator(_rootPath));
        return string.IsNullOrWhiteSpace(rootName) ? "Catalog" : rootName;
    }

    private string? GetSelectedExportFolderPath()
    {
        if (_folderTree.SelectedNode?.Tag is not string selectedPath
            || !Directory.Exists(selectedPath)
            || !IsPathAtOrUnderRoot(selectedPath))
        {
            return null;
        }

        return Path.GetFullPath(selectedPath);
    }

    private bool CanExportSelectedFolderAsDistinctScope(string? selectedFolderPath)
    {
        if (string.IsNullOrWhiteSpace(selectedFolderPath))
        {
            return false;
        }

        if (_selectedMetadata is null)
        {
            return true;
        }

        try
        {
            var songCount = Directory
                .EnumerateFiles(selectedFolderPath, "*.song", SearchOption.AllDirectories)
                .Where(IsCatalogSongFile)
                .Take(2)
                .Count();
            return songCount != 1;
        }
        catch (Exception ex)
        {
            LogSongReadFailure("CanExportSelectedFolderAsDistinctScope", selectedFolderPath, ex);
            return true;
        }
    }

    private List<SongGridRowData> CollectSongsForCsvExport(
        CsvExportScope scope,
        string? selectedFolderPath = null)
    {
        if (scope == CsvExportScope.CurrentSong)
        {
            return _selectedMetadata is null
                ? []
                : [new SongGridRowData { Metadata = _selectedMetadata, Match = null }];
        }

        var exportRoot = scope == CsvExportScope.SelectedFolder
            ? selectedFolderPath
            : _rootPath;
        if (string.IsNullOrWhiteSpace(exportRoot) || !Directory.Exists(exportRoot))
        {
            return [];
        }

        var exportRows = new List<SongGridRowData>();
        // "Entire Library" means every regular song beneath the configured root,
        // independent of the date filter currently applied to the on-screen view.
        foreach (var songPath in Directory.EnumerateFiles(exportRoot, "*.song", SearchOption.AllDirectories)
                     .Where(IsCatalogSongFile)
                     .OrderBy(path => path))
        {
            try
            {
                exportRows.Add(new SongGridRowData
                {
                    Metadata = ReadSongMetadata(songPath),
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

    private string BuildDefaultCsvFileName(
        bool exportingEntireLibrary,
        bool exportingCurrentSong,
        string? exportingFolderPath)
    {
        var timestamp = BuildLocalizedFileNameTimestamp();
        if (exportingEntireLibrary)
        {
            var catalogRootName = Path.GetFileName(Path.TrimEndingDirectorySeparator(_rootPath));
            if (string.IsNullOrWhiteSpace(catalogRootName))
            {
                catalogRootName = "Catalog";
            }

            return $"SongLens Export of {catalogRootName} - {timestamp}.csv";
        }

        if (exportingCurrentSong && _selectedMetadata is not null)
        {
            var songName = Path.GetFileNameWithoutExtension(_selectedMetadata.FileName);
            return $"{songName} - {timestamp}.csv";
        }

        if (!string.IsNullOrWhiteSpace(exportingFolderPath))
        {
            var folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(exportingFolderPath));
            return $"SongLens Export of {folderName} - {timestamp}.csv";
        }

        var baseName = _advancedSearchQuery is not null
            ? "SongLens-advanced-search"
            : _searchMode
                ? $"SongLens-search-{SanitizeFileNamePart(_searchTextBox.Text)}"
                : $"SongLens-{SanitizeFileNamePart(Path.GetFileName(_rootPath))}";

        if (string.IsNullOrWhiteSpace(baseName) || baseName.EndsWith("-", StringComparison.Ordinal))
        {
            baseName = "SongLens-export";
        }

        return $"{baseName} - {timestamp}.csv";
    }

    private static string BuildLocalizedFileNameTimestamp()
    {
        var localizedDateTime = DateTime.Now.ToString(
            "G",
            System.Globalization.CultureInfo.CurrentCulture);
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(localizedDateTime.Length);
        foreach (var character in localizedDateTime)
        {
            var safeCharacter = invalidCharacters.Contains(character) ? '-' : character;
            if (safeCharacter == '-' && builder.Length > 0 && builder[^1] == '-')
            {
                continue;
            }

            builder.Append(safeCharacter);
        }

        return builder.ToString().Trim().TrimEnd('.');
    }

    private static bool IsMixerCsvField(CsvExportField field)
    {
        return field.Key is "mixerMain" or "mixerInserts" or "mixerSends";
    }

    private void ShowMixerExportInformationIfNeeded(
        IReadOnlyList<CsvExportField> selectedFields,
        bool isExcelWorkbook)
    {
        if (!selectedFields.Any(IsMixerCsvField))
        {
            return;
        }

        var isSuppressed = isExcelWorkbook
            ? BrowserConfigStore.LoadSuppressExcelMixerExportInfo()
            : BrowserConfigStore.LoadSuppressCsvMixerExportInfo();
        if (isSuppressed)
        {
            return;
        }

        var title = isExcelWorkbook ? "Excel Workbook Export" : "CSV Mixer Details";
        var message = isExcelWorkbook
            ? "The workbook will contain a Catalog worksheet and a linked Mixer Details worksheet for the selected mixer data."
            : "Mixer data will be written to an accompanying details CSV in the same folder. Its filename will end with -mixer-details.csv.";
        using var dialog = new ThemedMessageForm(
            title,
            message,
            _theme,
            ThemedMessageKind.Information,
            optionText: "Don't show this again");
        dialog.ShowDialog(this);

        if (!dialog.OptionChecked)
        {
            return;
        }

        if (isExcelWorkbook)
        {
            BrowserConfigStore.SaveSuppressExcelMixerExportInfo(true);
        }
        else
        {
            BrowserConfigStore.SaveSuppressCsvMixerExportInfo(true);
        }
    }

    private static string BuildMixerDetailsCsvPath(string catalogCsvPath)
    {
        var directory = Path.GetDirectoryName(catalogCsvPath) ?? "";
        var fileName = $"{Path.GetFileNameWithoutExtension(catalogCsvPath)}-mixer-details.csv";
        return Path.Combine(directory, fileName);
    }

    private static string BuildMixerDetailsCsv(
        IReadOnlyList<SongGridRowData> exportRows,
        IReadOnlySet<string> selectedMixerFieldKeys)
    {
        var builder = new StringBuilder();
        AppendCsvRow(builder, MixerDetailHeaders);
        foreach (var row in BuildMixerDetailRows(exportRows, selectedMixerFieldKeys))
        {
            AppendCsvRow(builder, row.Values);
        }

        return builder.ToString();
    }

    private static readonly string?[] MixerDetailHeaders =
        ["Song", "Track #", "Type", "Channel", "Position/Slot", "Plugin", "Preset", "Destination"];

    private static IReadOnlyList<MixerDetailExportRow> BuildMixerDetailRows(
        IReadOnlyList<SongGridRowData> exportRows,
        IReadOnlySet<string> selectedMixerFieldKeys)
    {
        var rows = new List<MixerDetailExportRow>();

        foreach (var rowData in exportRows)
        {
            var metadata = rowData.Metadata;
            var trackNumberByName = metadata.TrackInstruments
                .Where(track => !string.IsNullOrWhiteSpace(track.TrackName))
                .GroupBy(track => track.TrackName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First().TrackNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    StringComparer.OrdinalIgnoreCase);
            if (selectedMixerFieldKeys.Contains("mixerMain"))
            {
                foreach (var channel in metadata.MixerMainChannels.OrderBy(item => item.ChannelName, StringComparer.CurrentCultureIgnoreCase))
                {
                    var wrotePlugin = false;
                    trackNumberByName.TryGetValue(channel.ChannelName.Trim(), out var trackNumber);
                    wrotePlugin |= AppendMixerMainChainRows(rows, metadata, trackNumber, channel.ChannelName, "Pre", channel.PrePluginChain);
                    wrotePlugin |= AppendMixerMainChainRows(rows, metadata, trackNumber, channel.ChannelName, "Post", channel.PostPluginChain);
                    if (!wrotePlugin)
                    {
                        AddMixerDetailRow(rows, metadata, trackNumber, "Main", channel.ChannelName, null, null, null, null);
                    }
                }
            }

            if (selectedMixerFieldKeys.Contains("mixerInserts"))
            {
                foreach (var insert in GetSnapshotMixerInserts(metadata)
                             .OrderBy(item => item.ChannelName, StringComparer.CurrentCultureIgnoreCase)
                             .ThenBy(item => item.SlotName, StringComparer.CurrentCultureIgnoreCase))
                {
                    trackNumberByName.TryGetValue(insert.ChannelName.Trim(), out var trackNumber);
                    AddMixerDetailRow(
                        rows,
                        metadata,
                        trackNumber,
                        "Insert",
                        insert.ChannelName,
                        insert.SlotName,
                        insert.PluginName,
                        insert.PresetName,
                        null);
                }
            }

            if (selectedMixerFieldKeys.Contains("mixerSends"))
            {
                foreach (var send in metadata.MixerSends
                             .OrderBy(item => item.ChannelName, StringComparer.CurrentCultureIgnoreCase)
                             .ThenBy(item => item.SlotName, StringComparer.CurrentCultureIgnoreCase))
                {
                    trackNumberByName.TryGetValue(send.ChannelName.Trim(), out var trackNumber);
                    AddMixerDetailRow(
                        rows,
                        metadata,
                        trackNumber,
                        "Send",
                        send.ChannelName,
                        send.SlotName,
                        null,
                        send.PresetName,
                        send.DestinationName);
                }
            }
        }

        return rows
            .GroupBy(row => row.SongPath, StringComparer.OrdinalIgnoreCase)
            .SelectMany(group => group
                .OrderBy(GetMixerDetailSortGroup)
                .ThenBy(row => row.TrackNumber ?? int.MaxValue)
                .ThenBy(row => GetMixerDetailTypeOrder(row.Values[2]))
                .ThenBy(row => row.Values[3], StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(row => GetMixerDetailPositionOrder(row.Values[4]))
                .ThenBy(row => row.Values[4], StringComparer.CurrentCultureIgnoreCase))
            .ToArray();
    }

    private static bool AppendMixerMainChainRows(
        ICollection<MixerDetailExportRow> rows,
        SongMetadata metadata,
        string? trackNumber,
        string channelName,
        string position,
        string? pluginChain)
    {
        if (string.IsNullOrWhiteSpace(pluginChain))
        {
            return false;
        }

        var plugins = pluginChain
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var index = 0; index < plugins.Length; index++)
        {
            var numberedPosition = plugins.Length == 1 ? position : $"{position} {index + 1}";
            AddMixerDetailRow(rows, metadata, trackNumber, "Main", channelName, numberedPosition, plugins[index], null, null);
        }

        return plugins.Length > 0;
    }

    private static void AddMixerDetailRow(
        ICollection<MixerDetailExportRow> rows,
        SongMetadata metadata,
        string? trackNumber,
        string type,
        string channel,
        string? positionOrSlot,
        string? plugin,
        string? preset,
        string? destination)
    {
        rows.Add(new MixerDetailExportRow
        {
            SongPath = metadata.Path,
            TrackNumber = int.TryParse(
                trackNumber,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsedTrackNumber)
                ? parsedTrackNumber
                : null,
            Values = [metadata.FileName, trackNumber, type, channel, positionOrSlot, plugin, preset, destination]
        });
    }

    private static int GetMixerDetailSortGroup(MixerDetailExportRow row)
    {
        if (string.Equals(row.Values[2], "Main", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return row.TrackNumber.HasValue ? 1 : 2;
    }

    private static int GetMixerDetailTypeOrder(string? type)
    {
        return type switch
        {
            "Main" => 0,
            "Insert" => 1,
            "Send" => 2,
            _ => 3
        };
    }

    private static int GetMixerDetailPositionOrder(string? position)
    {
        if (position?.StartsWith("Pre", StringComparison.OrdinalIgnoreCase) == true)
        {
            return 0;
        }

        return position?.StartsWith("Post", StringComparison.OrdinalIgnoreCase) == true ? 1 : 2;
    }

    private static IReadOnlyList<CsvExportField> BuildCsvExportFields()
    {
        return
        [
            new CsvExportField { Key = "song", Label = "Song", ValueSelector = (metadata, _) => metadata.FileName, IsDefault = true },
            new CsvExportField { Key = "status", Label = "Status", ValueSelector = (metadata, _) => BrowserConfigStore.LoadSongAnnotation(metadata.Path).Status, IsDefault = true },
            new CsvExportField { Key = "genre", Label = "Genre", ValueSelector = (metadata, _) => BrowserConfigStore.LoadSongAnnotation(metadata.Path).Genre, IsDefault = true },
            new CsvExportField { Key = "rating", Label = "Rating", ValueSelector = (metadata, _) => FormatStarRating(BrowserConfigStore.LoadSongAnnotation(metadata.Path).Rating), IsDefault = true },
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
            new CsvExportField { Key = "folderTracks", Label = "Folder Tracks", ValueSelector = (metadata, _) => BuildFolderTracksCsvValue(metadata.TrackInstruments), IsDefault = false },
            new CsvExportField { Key = "disabledTracks", Label = "Disabled Tracks", ValueSelector = (metadata, _) => BuildDisabledTracksCsvValue(metadata.TrackInstruments), IsDefault = false },
            new CsvExportField { Key = "trackNotes", Label = "Track Notes", ValueSelector = (metadata, _) => BuildTrackNotesCsvValue(metadata.TrackInstruments), IsDefault = false },
            new CsvExportField { Key = "songNotes", Label = "Song Notes", ValueSelector = (metadata, _) => metadata.NotesText, IsDefault = false },
            new CsvExportField { Key = "groups", Label = "Groups", ValueSelector = (metadata, _) => BuildGroupsCsvValue(metadata.Groups), IsDefault = false },
            new CsvExportField { Key = "mixerMain", Label = "Mixer Main → details", ValueSelector = (metadata, _) => BuildMixerMainCsvValue(metadata.MixerMainChannels), IsDefault = false },
            new CsvExportField { Key = "mixerInserts", Label = "Mixer Inserts → details", ValueSelector = (metadata, _) => BuildMixerInsertCsvValue(metadata.MixerInserts, metadata.MixerMainChannels), IsDefault = false },
            new CsvExportField { Key = "mixerSends", Label = "Mixer Sends → details", ValueSelector = (metadata, _) => BuildMixerSendCsvValue(metadata.MixerSends), IsDefault = false },
            new CsvExportField { Key = "comment", Label = "Comment", ValueSelector = (metadata, _) => metadata.Comment, IsDefault = true },
            new CsvExportField { Key = "path", Label = "Path", ValueSelector = (metadata, _) => metadata.Path, IsDefault = false },
            new CsvExportField { Key = "matchField", Label = "Match Field", ValueSelector = (_, match) => match?.MatchField, IsDefault = false },
            new CsvExportField { Key = "matchValue", Label = "Match Value", ValueSelector = (_, match) => match?.MatchValue, IsDefault = false }
        ];
    }

    private static string CustomSongColumnName(string id) => "Custom_" + id;

    private IReadOnlyList<SongGridColumnField> BuildSongGridColumnFields()
    {
        var fields = new List<SongGridColumnField>
        {
            new SongGridColumnField { Key = "playable", Label = "♪", ColumnName = PlayableColumnName, Width = 40, IsDefault = true, ValueSelector = (metadata, _) => GetMixdownIndicator(metadata) },
            new SongGridColumnField { Key = "song", Label = "Song", ColumnName = "Song", Width = 240, IsDefault = true, ValueSelector = (metadata, _) => metadata.FileName },
            new SongGridColumnField { Key = "title", Label = "Title", ColumnName = "Title", Width = 190, IsDefault = true, ValueSelector = (metadata, _) => metadata.Title },
            new SongGridColumnField { Key = "artist", Label = "Artist", ColumnName = "Artist", Width = 150, IsDefault = true, ValueSelector = (metadata, _) => metadata.Artist },
            new SongGridColumnField { Key = "year", Label = "Year", ColumnName = "Year", Width = 56, IsDefault = true, ValueSelector = (metadata, _) => metadata.Year },
            new SongGridColumnField { Key = "dateCreated", Label = "Date Created", ColumnName = "DateCreated", Width = 150, IsDefault = false, ValueSelector = (metadata, _) => metadata.DateCreated },
            new SongGridColumnField { Key = "lastModified", Label = "Last Modified", ColumnName = "LastModified", Width = 150, IsDefault = true, ValueSelector = (metadata, _) => metadata.LastModified },
            new SongGridColumnField { Key = "tempo", Label = "Tempo", ColumnName = "Tempo", Width = 64, IsDefault = true, ValueSelector = (metadata, _) => metadata.Tempo },
            new SongGridColumnField { Key = "keySignature", Label = "Key", ColumnName = "KeySignature", Width = 76, IsDefault = true, ValueSelector = (metadata, _) => metadata.KeySignature },
            new SongGridColumnField { Key = "timeSignature", Label = "Time", ColumnName = "TimeSignature", Width = 60, IsDefault = false, ValueSelector = (metadata, _) => metadata.TimeSignature },
            new SongGridColumnField { Key = "trackCount", Label = "Tracks", ColumnName = "TrackCount", Width = 66, IsDefault = false, ValueSelector = (metadata, _) => metadata.TrackCount },
            new SongGridColumnField { Key = "length", Label = "Length", ColumnName = "Length", Width = 70, IsDefault = true, ValueSelector = (metadata, _) => metadata.Length },
            new SongGridColumnField { Key = "sampleRate", Label = "Sample Rate", ColumnName = "SampleRate", Width = 110, IsDefault = false, ValueSelector = (metadata, _) => metadata.SampleRate },
            new SongGridColumnField { Key = "bitDepth", Label = "Bit Depth", ColumnName = "BitDepth", Width = 100, IsDefault = false, ValueSelector = (metadata, _) => metadata.BitDepth },
            new SongGridColumnField { Key = "generator", Label = "Studio Version", ColumnName = "Generator", Width = 180, IsDefault = false, ValueSelector = (metadata, _) => SongGeneratorDisplay.ToFriendlyDisplay(metadata.Generator) },
            new SongGridColumnField { Key = "formatVersion", Label = "Format Version", ColumnName = "FormatVersion", Width = 120, IsDefault = false, ValueSelector = (metadata, _) => metadata.FormatVersion },
            new SongGridColumnField { Key = "songNotes", Label = "Song Notes", ColumnName = "SongNotes", Width = 260, IsDefault = false, ValueSelector = (metadata, _) => metadata.NotesText },
            new SongGridColumnField { Key = "comment", Label = "Comment", ColumnName = "Comment", Width = 220, IsDefault = true, ValueSelector = (metadata, _) => metadata.Comment },
            new SongGridColumnField { Key = "path", Label = "Path", ColumnName = "Path", Width = 340, IsDefault = false, ValueSelector = (metadata, _) => metadata.Path },
            new SongGridColumnField { Key = "genre", Label = "Genre", ColumnName = GenreColumnName, Width = 125, IsDefault = true, ValueSelector = (metadata, _) => BrowserConfigStore.LoadSongAnnotation(metadata.Path).Genre },
            new SongGridColumnField { Key = "status", Label = "Status", ColumnName = StatusColumnName, Width = 145, IsDefault = true, ValueSelector = (metadata, _) => BrowserConfigStore.LoadSongAnnotation(metadata.Path).Status },
            new SongGridColumnField { Key = "rating", Label = "Rating", ColumnName = RatingColumnName, Width = 105, IsDefault = true, ValueSelector = (metadata, _) => BrowserConfigStore.LoadSongAnnotation(metadata.Path).Rating.ToString() }
        };
        fields.AddRange(_customSongColumns.Select(custom => new SongGridColumnField
        {
            Key = CustomSongColumnName(custom.Id),
            ColumnName = CustomSongColumnName(custom.Id),
            Label = custom.Name,
            Width = 150,
            IsDefault = false,
            ValueSelector = (metadata, _) => BrowserConfigStore.LoadSongAnnotation(metadata.Path).CustomValues?.GetValueOrDefault(custom.Id)
        }));
        return fields;
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

    private static string? BuildTrackNotesCsvValue(IReadOnlyList<TrackInstrumentInfo> tracks)
    {
        var notedTracks = tracks
            .Where(track => !string.IsNullOrWhiteSpace(track.TrackNote))
            .OrderBy(track => track.TrackNumber)
            .Select(track => $"#{track.TrackNumber} {track.TrackName}: {track.TrackNote!.Trim()}")
            .ToArray();

        return notedTracks.Length == 0
            ? null
            : string.Join(" | ", notedTracks);
    }

    private static string? BuildDisabledTracksCsvValue(IReadOnlyList<TrackInstrumentInfo> tracks)
    {
        var disabledTracks = tracks
            .Where(track => track.IsDisabled)
            .OrderBy(track => track.TrackNumber)
            .Select(track => $"#{track.TrackNumber} {track.TrackName}")
            .ToArray();

        return disabledTracks.Length == 0
            ? null
            : string.Join(" | ", disabledTracks);
    }

    private static string? BuildFolderTracksCsvValue(IReadOnlyList<TrackInstrumentInfo> tracks)
    {
        var folderTracks = tracks
            .Where(track => track.IsFolderTrack)
            .OrderBy(track => track.TrackNumber)
            .Select(track => $"#{track.TrackNumber} {track.TrackName}")
            .ToArray();

        return folderTracks.Length == 0
            ? null
            : string.Join(" | ", folderTracks);
    }

    private static string? BuildMixerInsertCsvValue(IReadOnlyList<MixerInsertInfo> inserts, IReadOnlyList<MixerMainInfo> mainChannels)
    {
        if (inserts.Count == 0)
        {
            return null;
        }

        var mainChannelNames = mainChannels
            .Where(channel => channel.IsMainOutput)
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
            .Where(channel => channel.IsMainOutput)
            .Select(channel => channel.ChannelName)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        return metadata.MixerInserts
            .Where(insert => !mainChannelNames.Contains(insert.ChannelName))
            .ToArray();
    }

    private SnapshotSectionSelection CreateSnapshotSectionSelection()
    {
        return new SnapshotSectionSelection
        {
            ShowTracksWithEventsOnly = _showTracksWithEventsOnly,
            HideDisabledTracks = _hideDisabledTracks,
            HideFolderTracks = _hideFolderTracks
        };
    }

    private IReadOnlyList<TrackInstrumentInfo> GetSnapshotTracks(
        SongMetadata metadata,
        SnapshotSectionSelection selection)
    {
        var showTracksWithEventsOnly = selection.UseCurrentTrackViewOptions
            ? _showTracksWithEventsOnly
            : selection.ShowTracksWithEventsOnly;
        var hideDisabledTracks = selection.UseCurrentTrackViewOptions
            ? _hideDisabledTracks
            : selection.HideDisabledTracks;
        var hideFolderTracks = selection.UseCurrentTrackViewOptions
            ? _hideFolderTracks
            : selection.HideFolderTracks;

        IEnumerable<TrackInstrumentInfo> tracks = metadata.TrackInstruments;
        if (showTracksWithEventsOnly)
        {
            tracks = tracks.Where(track => track.HasEvents);
        }

        if (hideFolderTracks)
        {
            tracks = tracks.Where(track => !track.IsFolderTrack);
        }

        if (hideDisabledTracks)
        {
            tracks = tracks.Where(track => !track.IsDisabled);
        }

        return tracks.ToArray();
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
                Plugins = selection.IncludePlugins
                    ? metadata.PluginDevices.ToList()
                    : null,
                Tracks = selection.IncludeTracks
                    ? GetSnapshotTracks(metadata, selection).Select(track => new SongSnapshotTrack
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

    private Dictionary<string, string?> BuildSnapshotSummary(SongMetadata metadata)
    {
        var annotation = BrowserConfigStore.LoadSongAnnotation(metadata.Path);
        var summary = new Dictionary<string, string?>
        {
            ["Title"] = metadata.Title,
            ["Artist"] = metadata.Artist,
            ["Status"] = annotation.Status,
            ["Genre"] = annotation.Genre,
            ["Rating"] = FormatStarRating(annotation.Rating),
            ["Year"] = metadata.Year,
            ["Date Created"] = metadata.DateCreated,
            ["Last Modified"] = metadata.LastModified,
            ["Tempo"] = metadata.Tempo,
            ["Key Signature"] = metadata.KeySignature,
            ["Time Signature"] = metadata.TimeSignature,
            ["Length"] = metadata.Length,
            ["Studio Version"] = SongGeneratorDisplay.ToFriendlyDisplay(metadata.Generator),
            ["Notes File"] = metadata.NotesFile,
            ["Comment"] = metadata.Comment,
            ["Path"] = metadata.Path
        };
        foreach (var column in _customSongColumns)
            summary[column.Name] = annotation.CustomValues?.GetValueOrDefault(column.Id);
        return summary;
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

    private string BuildSnapshotPreviewText(
        SongMetadata metadata,
        SnapshotSectionSelection selection,
        bool includeDocumentHeader = true)
    {
        var builder = new StringBuilder();
        if (includeDocumentHeader)
        {
            builder.AppendLine("SongLens Snapshot");
            builder.Append("Captured: ");
            builder.AppendLine(DateTime.Now.ToString());
            builder.AppendLine();
        }

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

        if (selection.IncludePlugins)
        {
            builder.AppendLine();
            builder.AppendLine("Plug-ins");
            builder.AppendLine("--------");
            if (metadata.PluginDevices.Count == 0)
            {
                builder.AppendLine("None");
            }
            else
            {
                foreach (var pluginDevice in metadata.PluginDevices)
                {
                    builder.Append("- ");
                    builder.AppendLine(pluginDevice);
                }
            }
        }

        if (selection.IncludeTracks)
        {
            builder.AppendLine();
            var showTracksWithEventsOnly = selection.UseCurrentTrackViewOptions
                ? _showTracksWithEventsOnly
                : selection.ShowTracksWithEventsOnly;
            AppendTrackSnapshotSection(
                builder,
                GetSnapshotTracks(metadata, selection),
                showTracksWithEventsOnly);
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

    private SongMetadata ReadSongMetadata(string songPath)
    {
        var fullPath = Path.GetFullPath(songPath);
        var fileInfo = new FileInfo(fullPath);
        var access = ++_songMetadataCacheAccess;
        if (_songMetadataCache.TryGetValue(fullPath, out var cached)
            && cached.FileLength == fileInfo.Length
            && cached.LastWriteTimeUtc == fileInfo.LastWriteTimeUtc)
        {
            cached.LastAccess = access;
            return cached.Metadata;
        }

        var metadata = SongMetadataReader.Read(fullPath);
        _songMetadataCache[fullPath] = new CachedSongMetadata
        {
            Metadata = metadata,
            FileLength = fileInfo.Length,
            LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
            LastAccess = access
        };
        TrimSongMetadataCache();
        return metadata;
    }

    private void TrimSongMetadataCache()
    {
        var entriesToRemove = _songMetadataCache.Count - SongMetadataCacheLimit;
        if (entriesToRemove <= 0)
        {
            return;
        }

        foreach (var path in _songMetadataCache
                     .OrderBy(entry => entry.Value.LastAccess)
                     .Take(entriesToRemove)
                     .Select(entry => entry.Key)
                     .ToArray())
        {
            _songMetadataCache.Remove(path);
        }
    }

    private void AddSongRow(SongMetadata metadata, SearchResult? match)
    {
        LogSongReadWarnings(metadata);
        var annotation = BrowserConfigStore.LoadSongAnnotation(metadata.Path);
        var rowIndex = _songGrid.Rows.Add();
        var row = _songGrid.Rows[rowIndex];
        foreach (var field in _songGridColumnFields)
        {
            if (string.Equals(field.ColumnName, PlayableColumnName, StringComparison.Ordinal))
            {
                var matchingFiles = MixdownAudioPlayback.FindMatchingFiles(metadata);
                row.Cells[field.ColumnName].Value = GetMixdownIndicator(matchingFiles.Count);
                continue;
            }

            if (string.Equals(field.ColumnName, RatingColumnName, StringComparison.Ordinal))
            {
                row.Cells[field.ColumnName].Value = annotation.Rating;
                continue;
            }

            if (field.ColumnName is StatusColumnName or GenreColumnName)
            {
                var optionCell = (DataGridViewComboBoxCell)row.Cells[field.ColumnName];
                var annotationValue = string.Equals(field.ColumnName, StatusColumnName, StringComparison.Ordinal)
                    ? annotation.Status
                    : annotation.Genre;
                var configuredOptions = string.Equals(field.ColumnName, StatusColumnName, StringComparison.Ordinal)
                    ? _songStatuses
                    : _songGenres;
                if (!string.IsNullOrWhiteSpace(annotationValue)
                    && !configuredOptions.Contains(annotationValue, StringComparer.OrdinalIgnoreCase))
                {
                    optionCell.Items.Add(annotationValue);
                }

                optionCell.Value = annotationValue ?? "";
                continue;
            }

            var customColumn = _customSongColumns.FirstOrDefault(custom => CustomSongColumnName(custom.Id) == field.ColumnName);
            if (customColumn is not null)
            {
                row.Cells[field.ColumnName].Value = annotation.CustomValues?.GetValueOrDefault(customColumn.Id) ?? "";
                continue;
            }

            var value = field.ValueSelector(metadata, match);
            row.Cells[field.ColumnName].Value = IsSongGridDateColumn(field.ColumnName)
                && DateTime.TryParse(value, out var dateValue)
                    ? dateValue
                    : FormatField(value);
        }
        row.Tag = new SongGridRowData
        {
            Metadata = metadata,
            Match = match
        };
        UpdateSongGridHintVisibility();
    }

    private void SaveSongAnnotationSelection(int rowIndex, int columnIndex)
    {
        if (_suppressSongAnnotationPersistence || rowIndex < 0 || columnIndex < 0
            || _songGrid.Rows[rowIndex].Tag is not SongGridRowData rowData)
        {
            return;
        }

        var columnName = _songGrid.Columns[columnIndex].Name;
        var row = _songGrid.Rows[rowIndex];
        var customColumn = _customSongColumns.FirstOrDefault(custom => CustomSongColumnName(custom.Id) == columnName);
        if (customColumn is not null)
        {
            BrowserConfigStore.SaveCustomSongValue(rowData.Metadata.Path, customColumn.Id, row.Cells[columnIndex].Value?.ToString());
            SetStatus($"Updated {customColumn.Name} for {rowData.Metadata.FileName}.");
            return;
        }
        if (columnName is not (StatusColumnName or GenreColumnName)) return;
        var status = row.Cells[StatusColumnName].Value?.ToString();
        var genre = row.Cells[GenreColumnName].Value?.ToString();
        var rating = GetSongRating(row);
        BrowserConfigStore.SaveSongAnnotation(rowData.Metadata.Path, status, genre, rating);
        var fieldName = string.Equals(_songGrid.Columns[columnIndex].Name, StatusColumnName, StringComparison.Ordinal)
            ? "status"
            : "genre";
        SetStatus($"Updated {fieldName} for {rowData.Metadata.FileName}.");
    }

    private void ApplyRenamedSongOptions(
        string columnName,
        IReadOnlyDictionary<string, string> renamedOptions)
    {
        if (renamedOptions.Count == 0)
        {
            return;
        }

        _suppressSongAnnotationPersistence = true;
        try
        {
            foreach (DataGridViewRow row in _songGrid.Rows)
            {
                var optionCell = row.Cells[columnName];
                var currentValue = optionCell.Value?.ToString();
                if (string.IsNullOrWhiteSpace(currentValue))
                {
                    continue;
                }

                var renamedValue = renamedOptions
                    .FirstOrDefault(pair => string.Equals(pair.Key, currentValue, StringComparison.OrdinalIgnoreCase))
                    .Value;
                if (!string.IsNullOrWhiteSpace(renamedValue))
                {
                    optionCell.Value = renamedValue;
                }
            }
        }
        finally
        {
            _suppressSongAnnotationPersistence = false;
        }
    }

    private void RefreshSongAnnotationOptions(string columnName, IReadOnlyList<string> configuredOptions)
    {
        if (_songGrid.Columns[columnName] is not DataGridViewComboBoxColumn optionColumn)
        {
            return;
        }

        optionColumn.Items.Clear();
        optionColumn.Items.Add("");
        optionColumn.Items.AddRange(configuredOptions.Cast<object>().ToArray());

        foreach (DataGridViewRow row in _songGrid.Rows)
        {
            if (row.Cells[columnName] is not DataGridViewComboBoxCell optionCell)
            {
                continue;
            }

            var currentValue = optionCell.Value?.ToString() ?? "";
            optionCell.Items.Clear();
            optionCell.Items.Add("");
            optionCell.Items.AddRange(configuredOptions.Cast<object>().ToArray());
            if (!string.IsNullOrWhiteSpace(currentValue)
                && !configuredOptions.Contains(currentValue, StringComparer.OrdinalIgnoreCase))
            {
                optionCell.Items.Add(currentValue);
            }
        }

        _songGrid.InvalidateColumn(optionColumn.Index);
    }

    private void SetSongRatingFromClick(DataGridViewCellMouseEventArgs args)
    {
        if (args.Button != MouseButtons.Left || args.RowIndex < 0 || args.ColumnIndex < 0
            || !string.Equals(_songGrid.Columns[args.ColumnIndex].Name, RatingColumnName, StringComparison.Ordinal)
            || _songGrid.Rows[args.RowIndex].Tag is not SongGridRowData rowData)
        {
            return;
        }

        const int horizontalPadding = 4;
        var availableWidth = Math.Max(1, _songGrid.Columns[args.ColumnIndex].Width - (horizontalPadding * 2));
        var clickX = Math.Clamp(args.X - horizontalPadding, 0, availableWidth - 1);
        var clickedRating = Math.Clamp((clickX * 5 / availableWidth) + 1, 1, 5);
        var row = _songGrid.Rows[args.RowIndex];
        var newRating = GetSongRating(row) == clickedRating ? 0 : clickedRating;
        row.Cells[RatingColumnName].Value = newRating;

        var status = row.Cells[StatusColumnName].Value?.ToString();
        var genre = row.Cells[GenreColumnName].Value?.ToString();
        BrowserConfigStore.SaveSongAnnotation(rowData.Metadata.Path, status, genre, newRating);
        _songGrid.InvalidateCell(args.ColumnIndex, args.RowIndex);
        SetStatus(newRating == 0
            ? $"Cleared rating for {rowData.Metadata.FileName}."
            : $"Rated {rowData.Metadata.FileName} {newRating} star{(newRating == 1 ? "" : "s")}.");
    }

    private void DrawSongRatingCell(object? sender, DataGridViewCellPaintingEventArgs args)
    {
        if (args.RowIndex < 0 || args.ColumnIndex < 0
            || !string.Equals(_songGrid.Columns[args.ColumnIndex].Name, RatingColumnName, StringComparison.Ordinal)
            || args.Graphics is not Graphics graphics)
        {
            return;
        }

        args.Paint(args.ClipBounds,
            DataGridViewPaintParts.Background
            | DataGridViewPaintParts.Border
            | DataGridViewPaintParts.SelectionBackground
            | DataGridViewPaintParts.Focus);

        var rating = GetSongRating(_songGrid.Rows[args.RowIndex]);
        const int horizontalPadding = 4;
        var contentBounds = new Rectangle(
            args.CellBounds.Left + horizontalPadding,
            args.CellBounds.Top,
            Math.Max(1, args.CellBounds.Width - (horizontalPadding * 2)),
            args.CellBounds.Height);
        var starWidth = contentBounds.Width / 5f;
        var filledColor = GetRatingStarColor();
        var emptyColor = args.State.HasFlag(DataGridViewElementStates.Selected)
            ? _theme.SelectedTextColor
            : _theme.MutedTextColor;

        for (var starIndex = 0; starIndex < 5; starIndex++)
        {
            var starBounds = Rectangle.Round(new RectangleF(
                contentBounds.Left + (starIndex * starWidth),
                contentBounds.Top,
                starWidth,
                contentBounds.Height));
            TextRenderer.DrawText(
                graphics,
                starIndex < rating ? "★" : "☆",
                args.CellStyle?.Font ?? _songGrid.Font,
                starBounds,
                starIndex < rating ? filledColor : emptyColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        args.Handled = true;
    }

    private Color GetRatingStarColor()
    {
        return string.Equals(_theme.Name, AppThemes.Light.Name, StringComparison.OrdinalIgnoreCase)
            ? LightRatingStarColor
            : DarkRatingStarColor;
    }

    private static int GetSongRating(DataGridViewRow row)
    {
        return int.TryParse(row.Cells[RatingColumnName].Value?.ToString(), out var rating)
            ? Math.Clamp(rating, 0, 5)
            : 0;
    }

    private static string FormatStarRating(int rating)
    {
        rating = Math.Clamp(rating, 0, 5);
        return rating == 0 ? "" : new string('★', rating);
    }

    private static string GetMixdownIndicator(SongMetadata metadata)
    {
        return GetMixdownIndicator(MixdownAudioPlayback.FindMatchingFiles(metadata).Count);
    }

    private static string GetMixdownIndicator(int mixdownCount)
    {
        return mixdownCount switch
        {
            1 => SingleMixdownIndicator,
            > 1 => MultipleMixdownsIndicator,
            _ => ""
        };
    }

    private void RefreshPlayableMixdownIndicators()
    {
        foreach (DataGridViewRow row in _songGrid.Rows)
        {
            if (row.Tag is not SongGridRowData rowData)
            {
                continue;
            }

            var matchingFiles = MixdownAudioPlayback.FindMatchingFiles(rowData.Metadata);
            row.Cells[PlayableColumnName].Value = GetMixdownIndicator(matchingFiles.Count);
        }

        var playableColumn = _songGrid.Columns[PlayableColumnName];
        if (playableColumn is not null)
        {
            _songGrid.InvalidateColumn(playableColumn.Index);
        }
    }

    private void ShowSongGridColumnsDialog()
    {
        if (_bulkEditMode) ToggleBulkEditMode();
        var visibleColumnKeys = BrowserConfigStore.LoadSongGridVisibleColumnKeys().ToList();
        if (visibleColumnKeys.Count > 0
            && !visibleColumnKeys.Contains("playable", StringComparer.OrdinalIgnoreCase))
        {
            visibleColumnKeys.Insert(0, "playable");
        }

        using var dialog = new SongGridColumnsForm(
            _songGridColumnFields,
            _theme,
            visibleColumnKeys,
            owner => ShowPreferencesDialog(owner, openCustomColumnsTab: true),
            () => _songGridColumnFields,
            BrowserConfigStore.LoadSongGridVisibleColumnKeys);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedFields.Count == 0)
        {
            return;
        }

        if (dialog.RestoreDefaultLayoutRequested)
        {
            RestoreDefaultSongLensView();
            return;
        }

        var selectedKeys = dialog.SelectedFields.Select(field => field.Key).ToArray();
        BrowserConfigStore.SaveSongGridVisibleColumnKeys(selectedKeys);
        ApplySongGridColumnVisibility(selectedKeys);
        ApplySongGridSort();
    }

    private void ToggleBulkEditMode()
    {
        if (_songGridPanel is null) return;
        if (_bulkEditMode)
        {
            _songGrid.EndEdit();
            _bulkEditMode = false;
            _suspendSongGridOrderPersistence = true;
            try
            {
                _songGrid.Columns.Remove(BulkSelectColumnName);
            }
            finally
            {
                _suspendSongGridOrderPersistence = false;
            }
            _bulkEditBar.Visible = false;
            _songGridPanel.RowStyles[1].Height = 0;
            _songGrid.AllowUserToOrderColumns = true;
            _bulkRatingPicker.ClearRating();
            _bulkEditMenuItem.Text = "Bulk Edit Songs...";
            SetStatus("Bulk Edit finished.");
            return;
        }

        if (!BrowserConfigStore.LoadSuppressBulkEditIntro())
        {
            using var introduction = new ThemedMessageForm(
                "Bulk Edit Songs",
                "Check songs in the current view, or click All, to set their Status, Genre, or Rating. Delete moves qualifying checked song folders to the Recycle Bin after confirmation. Click Done to leave Bulk Edit.",
                _theme,
                okText: "Start Bulk Edit",
                optionText: "Don't show this again");
            introduction.ShowDialog(this);
            if (introduction.OptionChecked)
                BrowserConfigStore.SaveSuppressBulkEditIntro(true);
        }

        _songGrid.EndEdit();

        _suspendSongGridOrderPersistence = true;
        try
        {
            var selectColumn = new DataGridViewCheckBoxColumn
            {
                Name = BulkSelectColumnName,
                HeaderText = "✓",
                Width = 32,
                MinimumWidth = 32,
                Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = false
            };
            _songGrid.Columns.Add(selectColumn);
            selectColumn.DisplayIndex = 0;
            foreach (DataGridViewRow row in _songGrid.Rows)
                row.Cells[BulkSelectColumnName].Value = false;
        }
        finally
        {
            _suspendSongGridOrderPersistence = false;
        }
        _bulkEditMode = true;
        _bulkRatingPicker.ClearRating();
        _bulkEditBar.Visible = true;
        _songGridPanel.RowStyles[1].Height = AppFontSettings.Scale(38, _fontPreferences, AppFontSection.MainUi);
        _songGrid.AllowUserToOrderColumns = false;
        _bulkEditMenuItem.Text = "Finish Bulk Edit";
        RefreshBulkEditOptions();
        UpdateBulkSelectionCount();
        SetStatus("Bulk Edit: Status and Genre use checked songs; Rating applies to every song in the grid.");
    }

    private void UpdateBulkSelectionCount()
    {
        if (!_bulkEditMode || _suppressBulkSelectionCount || _songLensViewBuildDepth > 0
            || !_songGrid.Columns.Contains(BulkSelectColumnName)) return;
        var count = _songGrid.Rows.Cast<DataGridViewRow>()
            .Count(row => row.Tag is SongGridRowData && row.Cells[BulkSelectColumnName].Value is true);
        _bulkEditCountLabel.Text = $"{count} selected";
        _bulkSetStatusButton.Enabled = count > 0;
        _bulkSetGenreButton.Enabled = count > 0;
        _bulkSetRatingButton.Enabled = _songGrid.Rows.Cast<DataGridViewRow>()
            .Any(row => row.Tag is SongGridRowData);
        _bulkDeleteButton.Enabled = count > 0;
        _bulkClearButton.Enabled = count > 0;
    }

    private void SetAllBulkSelections(bool selected)
    {
        if (!_bulkEditMode) return;
        _songGrid.EndEdit();
        _suppressBulkSelectionCount = true;
        try
        {
            foreach (DataGridViewRow row in _songGrid.Rows)
            {
                if (row.Tag is SongGridRowData)
                    row.Cells[BulkSelectColumnName].Value = selected;
            }
        }
        finally
        {
            _suppressBulkSelectionCount = false;
        }
        UpdateBulkSelectionCount();
    }

    private void ApplyBulkAnnotationValue(bool setStatus)
    {
        if (!_bulkEditMode) return;
        _songGrid.EndEdit();
        var combo = setStatus ? _bulkStatusComboBox : _bulkGenreComboBox;
        var fieldName = setStatus ? "Status" : "Genre";
        if (combo.SelectedIndex < 1)
        {
            using var message = new ThemedMessageForm(Text, $"Choose a {fieldName.ToLowerInvariant()} first.", _theme, ThemedMessageKind.Information);
            message.ShowDialog(this);
            return;
        }
        var value = combo.SelectedIndex == 1 ? null : combo.SelectedItem?.ToString();
        var columnName = setStatus ? StatusColumnName : GenreColumnName;
        var rows = _songGrid.Rows.Cast<DataGridViewRow>()
            .Where(row => row.Tag is SongGridRowData
                && row.Cells[BulkSelectColumnName].Value is true)
            .ToArray();
        if (rows.Length == 0) return;
        var changedRows = rows.Where(row => !string.Equals(
                row.Cells[columnName].Value?.ToString() ?? "", value ?? "", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (changedRows.Length == 0)
        {
            SetStatus($"All {rows.Length} selected songs already have this {fieldName.ToLowerInvariant()}.");
            return;
        }
        var overwrites = changedRows.Count(row => !string.IsNullOrWhiteSpace(row.Cells[columnName].Value?.ToString()));
        var valueDescription = value is null ? $"clear {fieldName.ToLowerInvariant()}" : $"set {fieldName.ToLowerInvariant()} to '{value}'";
        using var confirmation = new ThemedConfirmationForm(
            $"Bulk Edit {fieldName}",
            $"{rows.Length} songs selected in the current view.\n\n{valueDescription} for {changedRows.Length} songs?\n{overwrites} already have a different {fieldName.ToLowerInvariant()} that will be replaced.",
            _theme,
            okText: "Apply");
        if (confirmation.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            BrowserConfigStore.SaveBulkSongAnnotationValue(
                changedRows.Select(row => ((SongGridRowData)row.Tag!).Metadata.Path), setStatus, value);
        }
        catch (Exception exception)
        {
            using var message = new ThemedMessageForm(Text, $"Bulk edit could not be saved:\n{exception.Message}", _theme, ThemedMessageKind.Error);
            message.ShowDialog(this);
            return;
        }
        _suppressSongAnnotationPersistence = true;
        try
        {
            foreach (var row in changedRows)
                row.Cells[columnName].Value = value ?? "";
        }
        finally
        {
            _suppressSongAnnotationPersistence = false;
        }
        SetStatus($"Updated {fieldName.ToLowerInvariant()} for {changedRows.Length} songs.");
    }

    private void ApplyBulkRating()
    {
        if (!_bulkEditMode) return;
        _songGrid.EndEdit();
        var rating = _bulkRatingPicker.Rating;
        var rows = _songGrid.Rows.Cast<DataGridViewRow>()
            .Where(row => row.Tag is SongGridRowData && row.Cells[BulkSelectColumnName].Value is true)
            .ToArray();
        if (rows.Length == 0) return;

        var changedRows = rows.Where(row => GetSongRating(row) != rating).ToArray();
        if (changedRows.Length == 0)
        {
            SetStatus($"All {rows.Length} songs in the current grid already have this rating.");
            return;
        }

        var overwrites = changedRows.Count(row => GetSongRating(row) > 0);
        var ratingDescription = rating == 0
            ? "clear the rating"
            : $"set the rating to {rating} star{(rating == 1 ? "" : "s")}";
        using var confirmation = new ThemedConfirmationForm(
            "Bulk Edit Rating",
            $"{rows.Length} songs are displayed in the current grid.\n\n{ratingDescription} for {changedRows.Length} songs?\n{overwrites} already have a rating that will be replaced.",
            _theme,
            okText: "Apply");
        if (confirmation.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            BrowserConfigStore.SaveBulkSongRating(
                changedRows.Select(row => ((SongGridRowData)row.Tag!).Metadata.Path),
                rating);
        }
        catch (Exception exception)
        {
            using var message = new ThemedMessageForm(Text, $"Bulk edit could not be saved:\n{exception.Message}", _theme, ThemedMessageKind.Error);
            message.ShowDialog(this);
            return;
        }

        _suppressSongAnnotationPersistence = true;
        try
        {
            foreach (var row in changedRows)
                row.Cells[RatingColumnName].Value = rating;
        }
        finally
        {
            _suppressSongAnnotationPersistence = false;
        }
        if (_songGrid.Columns[RatingColumnName] is { } ratingColumn)
            _songGrid.InvalidateColumn(ratingColumn.Index);
        SetStatus(rating == 0
            ? $"Cleared ratings for {changedRows.Length} songs."
            : $"Updated rating for {changedRows.Length} songs.");
    }

    private void DeleteSelectedSongFolders()
    {
        if (!_bulkEditMode || !_songGrid.Columns.Contains(BulkSelectColumnName)) return;

        _songGrid.EndEdit();
        var selectedSongPaths = _songGrid.Rows.Cast<DataGridViewRow>()
            .Where(row => row.Tag is SongGridRowData
                && row.Cells[BulkSelectColumnName].Value is true)
            .Select(row => ((SongGridRowData)row.Tag!).Metadata.Path)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (selectedSongPaths.Length == 0) return;

        var qualifiedSongs = new List<(string SongPath, string FolderPath)>();
        foreach (var songPath in selectedSongPaths)
        {
            try
            {
                var fullSongPath = Path.GetFullPath(songPath);
                var parent = Directory.GetParent(fullSongPath);
                if (!File.Exists(fullSongPath)
                    || !string.Equals(Path.GetExtension(fullSongPath), ".song", StringComparison.OrdinalIgnoreCase)
                    || parent is null
                    || !Directory.Exists(parent.FullName)
                    || !string.Equals(parent.Name, Path.GetFileNameWithoutExtension(fullSongPath), StringComparison.OrdinalIgnoreCase)
                    || !IsPathAtOrUnderRoot(parent.FullName)
                    || string.Equals(Path.GetFullPath(parent.FullName), Path.GetFullPath(_rootPath), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                qualifiedSongs.Add((fullSongPath, parent.FullName));
            }
            catch
            {
                // Invalid or inaccessible selections do not qualify for deletion.
            }
        }

        var foldersToDelete = qualifiedSongs
            .Select(item => item.FolderPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(path => path.Length)
            .ToArray();
        if (foldersToDelete.Length == 0)
        {
            using var noMatchesDialog = new ThemedMessageForm(
                "Delete Songs",
                "None of the selected songs qualify for deletion. A song must be directly inside a parent folder with the same name as the .song file.",
                _theme,
                ThemedMessageKind.Information);
            noMatchesDialog.ShowDialog(this);
            return;
        }

        var skippedCount = selectedSongPaths.Length - qualifiedSongs.Count;
        var confirmationMessage = new StringBuilder()
            .AppendLine($"{FormatCount(qualifiedSongs.Count, "song")} in {FormatCount(foldersToDelete.Length, "folder")} will be moved to the Recycle Bin.")
            .AppendLine()
            .AppendLine("Each entire matching parent folder and all of its contents will be moved to the Recycle Bin.")
            .Append(skippedCount > 0
                ? $"\n{FormatCount(skippedCount, "selected song")} did not meet the matching-folder rule and will not be deleted."
                : "")
            .ToString();
        using var confirmation = new ThemedConfirmationForm(
            "Delete Selected Songs?",
            confirmationMessage,
            _theme,
            okText: "OK",
            cancelText: "Cancel");
        if (confirmation.ShowDialog(this) != DialogResult.OK) return;

        var deletedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();
        foreach (var folderPath in foldersToDelete)
        {
            try
            {
                WindowsShell.MoveDirectoryToRecycleBin(folderPath);
                if (Directory.Exists(folderPath))
                {
                    failures.Add($"{Path.GetFileName(folderPath)}: the folder was not moved");
                    continue;
                }

                deletedFolders.Add(folderPath);
            }
            catch (Exception exception)
            {
                failures.Add($"{Path.GetFileName(folderPath)}: {exception.Message}");
            }
        }

        if (deletedFolders.Count > 0)
        {
            foreach (var songPath in qualifiedSongs
                         .Where(item => deletedFolders.Contains(item.FolderPath))
                         .Select(item => item.SongPath))
            {
                _songMetadataCache.Remove(songPath);
            }

            InvalidateCatalogStatisticsCache();
            RefreshCurrentView();
        }

        if (failures.Count > 0)
        {
            var failureDetails = string.Join("\n", failures.Take(6));
            if (failures.Count > 6)
                failureDetails += $"\n...and {failures.Count - 6} more.";
            using var failureDialog = new ThemedMessageForm(
                "Delete Songs",
                $"{FormatCount(deletedFolders.Count, "folder")} moved to the Recycle Bin.\n\nSome folders could not be moved:\n{failureDetails}",
                _theme,
                ThemedMessageKind.Error);
            failureDialog.ShowDialog(this);
        }

        SetStatus($"Moved {FormatCount(deletedFolders.Count, "song folder")} to the Recycle Bin.");
    }

    private void RefreshSavedViewsMenu()
    {
        _savedViewsMenuItem.DropDownItems.Clear();
        if (_savedSongLensViews.Count == 0)
        {
            _savedViewsMenuItem.DropDownItems.Add(new ToolStripMenuItem("(No saved views)") { Enabled = false });
            return;
        }

        foreach (var savedView in _savedSongLensViews.OrderBy(view => view.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            var view = savedView;
            var restoresSearch = view.FastSearchText is not null || view.AdvancedSearchQuery is not null;
            var menuItem = new ToolStripMenuItem(view.Name)
            {
                BackColor = _theme.PanelBackColor,
                ForeColor = _theme.TextColor,
                ToolTipText = restoresSearch
                    ? view.DateFilter is null
                        ? "Restore this search, column layout, and sorting"
                        : "Restore this search, column layout, sorting, and filter"
                    : view.DateFilter is null
                        ? "Restore this column layout and sorting"
                        : "Restore this column layout, sorting, and filter"
            };
            menuItem.Click += (_, _) => ApplySavedSongLensView(view);
            _savedViewsMenuItem.DropDownItems.Add(menuItem);
        }

    }

    private SavedSongLensView CaptureCurrentSongLensView(string name, bool includeDateFilter)
    {
        var visibleKeys = _songGridColumnFields
            .Where(field => _songGrid.Columns[field.ColumnName]?.Visible == true)
            .Select(field => field.Key)
            .ToList();
        var columnOrder = _songGrid.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Name != BulkSelectColumnName)
            .OrderBy(column => column.DisplayIndex)
            .Select(column => column.Name)
            .ToList();
        var columnWidths = _songGrid.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Name != BulkSelectColumnName)
            .ToDictionary(column => column.Name, column => column.Width, StringComparer.OrdinalIgnoreCase);

        return new SavedSongLensView
        {
            Name = name,
            FastSearchText = _advancedSearchQuery is null && _searchMode
                ? _searchTextBox.Text.Trim()
                : null,
            AdvancedSearchQuery = _advancedSearchQuery is null
                ? null
                : CloneAdvancedSearchQuery(_advancedSearchQuery),
            VisibleColumnKeys = visibleKeys,
            ColumnOrder = columnOrder,
            ColumnWidths = columnWidths,
            SortColumnName = _songGridSortLevels.FirstOrDefault()?.ColumnName ?? "Song",
            SortDirection = _songGridSortLevels.FirstOrDefault()?.Direction
                ?? nameof(ListSortDirection.Ascending),
            SortLevels = CloneSongGridSortLevels(_songGridSortLevels),
            DateFilter = includeDateFilter ? CloneSongAgeFilter(_songAgeFilter) : null
        };
    }

    private void ApplySavedSongLensView(SavedSongLensView savedView)
    {
        if (_bulkEditMode) ToggleBulkEditMode();
        BrowserConfigStore.SaveSongGridVisibleColumnKeys(savedView.VisibleColumnKeys);
        ApplySongGridColumnVisibility(savedView.VisibleColumnKeys);

        BrowserConfigStore.SaveSongGridColumnOrder(savedView.ColumnOrder);
        ApplySavedSongGridColumnOrder();

        BrowserConfigStore.SaveGridColumnWidths(SongGridKey, savedView.ColumnWidths);
        ApplySavedGridColumnWidths(_songGrid, SongGridKey);

        _songGridSortLevels = CloneSongGridSortLevels(savedView.SortLevels ?? []);
        if (savedView.SortLevels is null
            && _songGridSortLevels.Count == 0
            && _songGrid.Columns.Contains(savedView.SortColumnName))
        {
            _songGridSortLevels.Add(new SongGridSortLevel
            {
                ColumnName = savedView.SortColumnName,
                Direction = savedView.SortDirection
            });
        }
        BrowserConfigStore.SaveSongGridSortLevels(_songGridSortLevels);
        if (savedView.DateFilter is not null)
        {
            _songAgeFilter = CloneSongAgeFilter(savedView.DateFilter);
            BrowserConfigStore.SaveSongAgeFilterPreference(_songAgeFilter);
            BrowserConfigStore.SaveViewAllSongs(false);
        }

        if (savedView.FastSearchText is not null)
        {
            _keywordSearchReturnFolderPath = null;
            _advancedSearchQuery = null;
            SetFastSearchText(savedView.FastSearchText);
            SearchSongs();
        }
        else if (savedView.AdvancedSearchQuery is not null)
        {
            RunAdvancedSearch(CloneAdvancedSearchQuery(savedView.AdvancedSearchQuery));
        }
        else if (savedView.DateFilter is not null
            && !string.IsNullOrWhiteSpace(_rootPath)
            && Directory.Exists(_rootPath))
            LoadAllSongs();
        else if (!string.IsNullOrWhiteSpace(_rootPath) && Directory.Exists(_rootPath))
            RebuildCurrentViewForQuerySort();
        else
            UpdateSongGridSortGlyphs();
        UpdateStatusIndicators();

        SetStatus($"Applied SongLens view {savedView.Name}.");
    }

    private void SaveCurrentSongLensView()
    {
        using var prompt = new SaveSongLensViewForm(_songAgeFilter is not null, _theme);
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var name = prompt.ViewName;
        var existingIndex = _savedSongLensViews.FindIndex(
            view => string.Equals(view.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            using var confirmation = new ThemedConfirmationForm(
                "Overwrite Saved View",
                $"Replace the saved view \"{_savedSongLensViews[existingIndex].Name}\" with the current view?",
                _theme,
                okText: "Replace");
            if (confirmation.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
        }

        var savedView = CaptureCurrentSongLensView(name, prompt.IncludeDateFilter);
        if (existingIndex >= 0)
        {
            _savedSongLensViews[existingIndex] = savedView;
        }
        else
        {
            _savedSongLensViews.Add(savedView);
        }

        BrowserConfigStore.SaveSavedSongLensViews(_savedSongLensViews);
        RefreshSavedViewsMenu();
        SetStatus($"Saved SongLens view {name}.");
    }

    private void ManageSavedSongLensViews()
    {
        using var dialog = new SavedSongLensViewsForm(
            _savedSongLensViews,
            _songAgeFilter is not null,
            CaptureCurrentSongLensView,
            _theme);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _savedSongLensViews = dialog.SavedViews.ToList();
        BrowserConfigStore.SaveSavedSongLensViews(_savedSongLensViews);
        RefreshSavedViewsMenu();
        SetStatus("Saved views updated.");
    }

    private void RestoreDefaultSongLensView()
    {
        if (!BrowserConfigStore.LoadSuppressRestoreDefaultLayoutWarning())
        {
            using var confirmation = new ThemedConfirmationForm(
                "Restore Default Layout",
                "Replace the current SongLens View layout with the built-in defaults?\n\nThis resets visible columns, column order and widths, and sorting. Saved views will not be deleted.",
                _theme,
                okText: "Restore Defaults",
                optionText: "Don't show this again");
            if (confirmation.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (confirmation.OptionChecked)
            {
                BrowserConfigStore.SaveSuppressRestoreDefaultLayoutWarning(true);
            }
        }

        if (_bulkEditMode) ToggleBulkEditMode();
        var defaultFields = _songGridColumnFields.Where(field => field.IsDefault).ToArray();
        var visibleKeys = defaultFields.Select(field => field.Key).ToArray();
        var columnOrder = _songGridColumnFields.Select(field => field.ColumnName).ToArray();
        var columnWidths = _songGridColumnFields.ToDictionary(
            field => field.ColumnName,
            field => field.Width,
            StringComparer.OrdinalIgnoreCase);

        BrowserConfigStore.SaveSongGridVisibleColumnKeys(visibleKeys);
        ApplySongGridColumnVisibility(visibleKeys);
        BrowserConfigStore.SaveSongGridColumnOrder(columnOrder);
        ApplySavedSongGridColumnOrder();
        BrowserConfigStore.SaveGridColumnWidths(SongGridKey, columnWidths);
        ApplySavedGridColumnWidths(_songGrid, SongGridKey);
        _songGridSortLevels =
        [
            new SongGridSortLevel
            {
                ColumnName = "Song",
                Direction = nameof(ListSortDirection.Ascending)
            }
        ];
        BrowserConfigStore.SaveSongGridSortLevels(_songGridSortLevels);
        RebuildCurrentViewForQuerySort();
        SetStatus("Restored the default SongLens layout.");
    }

    private static SongAgeFilter? CloneSongAgeFilter(SongAgeFilter? filter)
    {
        return filter is null
            ? null
            : new SongAgeFilter
            {
                Mode = filter.Mode,
                DateField = filter.DateField,
                Operator = filter.Operator,
                Days = filter.Days,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate
            };
    }

    private static List<SongGridSortLevel> CloneSongGridSortLevels(IEnumerable<SongGridSortLevel> sortLevels)
    {
        return sortLevels
            .Where(level => !string.IsNullOrWhiteSpace(level.ColumnName))
            .Take(4)
            .Select(level => new SongGridSortLevel
            {
                ColumnName = level.ColumnName,
                Direction = string.Equals(
                    level.Direction,
                    nameof(ListSortDirection.Descending),
                    StringComparison.OrdinalIgnoreCase)
                    ? nameof(ListSortDirection.Descending)
                    : nameof(ListSortDirection.Ascending)
            })
            .ToList();
    }

    private void ShowSongStatisticsDialog()
    {
        var songCount = _songGrid.Rows.Count;
        IReadOnlyList<SongStatisticsField> fields;
        var catalogSongCount = 0;
        if (songCount == 1
            && _songGrid.Rows[0].Tag is SongGridRowData rowData)
        {
            var previousCursor = Cursor;
            try
            {
                Cursor = Cursors.WaitCursor;
                SetStatus("Comparing this song with the full catalog...");
                Update();
                var catalogMetadata = GetCatalogStatisticsMetadata();
                catalogSongCount = catalogMetadata.Count;
                fields = BuildSongComparisonStatisticsFields(rowData.Metadata, catalogMetadata);
            }
            finally
            {
                Cursor = previousCursor;
            }
        }
        else
        {
            fields = BuildSongStatisticsFields();
        }

        if (fields.Count == 0)
        {
            SetStatus("Ready");
            using var messageDialog = new ThemedMessageForm(
                Text,
                "Show at least one statistical column in SongLens View before opening Statistics.",
                _theme,
                ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        using var dialog = new SongStatisticsForm(
            fields,
            songCount,
            _theme,
            catalogSongCount: catalogSongCount);
        dialog.ShowDialog(this);
        SetStatus("Ready");
    }

    private IReadOnlyList<SongStatisticsField> BuildSongStatisticsFields()
    {
        return GetVisibleSongGridColumnsInDisplayOrder()
            .Where(column => IsSupportedSongStatisticsColumn(column.Name))
            .Select(column => new SongStatisticsField
            {
                Key = _songGridColumnFields
                    .First(field => string.Equals(field.ColumnName, column.Name, StringComparison.OrdinalIgnoreCase))
                    .Key,
                Label = string.Equals(column.Name, PlayableColumnName, StringComparison.OrdinalIgnoreCase)
                    ? "Mixdowns"
                    : GetSongGridColumnLabel(column),
                Values = _songGrid.Rows
                    .Cast<DataGridViewRow>()
                    .Select(row => GetSongStatisticsValue(row, column.Name))
                    .ToArray(),
                PreferredOrder = GetSongStatisticsPreferredOrder(column.Name)
            })
            .ToArray();
    }

    private IReadOnlyList<SongStatisticsField> BuildSongComparisonStatisticsFields(
        SongMetadata selectedSong,
        IReadOnlyList<SongMetadata> catalogMetadata)
    {
        return GetVisibleSongGridColumnsInDisplayOrder()
            .Where(column => IsSupportedSongStatisticsColumn(column.Name))
            .Select(column =>
            {
                var selectedDisplayValue = GetSongStatisticsValue(selectedSong, column.Name);
                var selectedComparisonValue = GetSongStatisticsComparisonValue(column.Name, selectedDisplayValue);
                var comparisonDisplayValue = string.Equals(column.Name, "Tempo", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(selectedDisplayValue, selectedComparisonValue, StringComparison.OrdinalIgnoreCase)
                        ? $"{selectedDisplayValue} BPM ({selectedComparisonValue})"
                        : selectedDisplayValue;
                return new SongStatisticsField
                {
                    Key = _songGridColumnFields
                        .First(field => string.Equals(field.ColumnName, column.Name, StringComparison.OrdinalIgnoreCase))
                        .Key,
                    Label = string.Equals(column.Name, PlayableColumnName, StringComparison.OrdinalIgnoreCase)
                        ? "Mixdowns"
                        : GetSongGridColumnLabel(column),
                    Values = catalogMetadata
                        .Select(metadata => GetSongStatisticsValue(metadata, column.Name))
                        .Select(value => GetSongStatisticsComparisonValue(column.Name, value))
                        .ToArray(),
                    PreferredOrder = GetSongStatisticsPreferredOrder(column.Name),
                    ComparisonValue = selectedComparisonValue,
                    ComparisonDisplayValue = comparisonDisplayValue
                };
            })
            .ToArray();
    }

    private bool IsSupportedSongStatisticsColumn(string columnName)
    {
        return _customSongColumns.Any(column => CustomSongColumnName(column.Id) == columnName)
            || columnName is PlayableColumnName
            or StatusColumnName
            or GenreColumnName
            or RatingColumnName
            or "Artist"
            or "Year"
            or "Tempo"
            or "KeySignature"
            or "TimeSignature"
            or "TrackCount"
            or "SampleRate"
            or "BitDepth"
            or "Generator"
            or "FormatVersion";
    }

    private IReadOnlyList<SongMetadata> GetCatalogStatisticsMetadata()
    {
        if (_catalogStatisticsMetadataCache is not null
            && string.Equals(_catalogStatisticsCacheRoot, _rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return _catalogStatisticsMetadataCache;
        }

        var metadata = new List<SongMetadata>();
        if (!string.IsNullOrWhiteSpace(_rootPath) && Directory.Exists(_rootPath))
        {
            foreach (var songPath in Directory.EnumerateFiles(_rootPath, "*.song", SearchOption.AllDirectories)
                         .Where(IsCatalogSongFile)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    metadata.Add(ReadSongMetadata(songPath));
                }
                catch (Exception ex)
                {
                    LogSongReadFailure("CatalogStatistics", songPath, ex);
                }
            }
        }

        _catalogStatisticsCacheRoot = _rootPath;
        _catalogStatisticsMetadataCache = metadata;
        return metadata;
    }

    private void InvalidateCatalogStatisticsCache()
    {
        _catalogStatisticsMetadataCache = null;
        _catalogStatisticsCacheRoot = null;
    }

    private string GetSongStatisticsValue(DataGridViewRow row, string columnName)
    {
        var value = row.Cells[columnName].Value?.ToString()?.Trim();
        return NormalizeSongStatisticsValue(columnName, value);
    }

    private string GetSongStatisticsValue(SongMetadata metadata, string columnName)
    {
        string? value;
        if (string.Equals(columnName, PlayableColumnName, StringComparison.OrdinalIgnoreCase))
        {
            value = GetMixdownIndicator(metadata);
        }
        else
        {
            var field = _songGridColumnFields.First(
                candidate => string.Equals(candidate.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
            value = field.ValueSelector(metadata, null)?.Trim();
        }

        return NormalizeSongStatisticsValue(columnName, value);
    }

    private static string NormalizeSongStatisticsValue(string columnName, string? value)
    {
        if (string.Equals(columnName, RatingColumnName, StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(value, out var rating) && rating > 0
                ? FormatStarRating(rating)
                : "Unrated";
        }

        if (string.Equals(columnName, StatusColumnName, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(value) ? "No Status" : value;
        }

        if (string.Equals(columnName, GenreColumnName, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(value) ? "No Genre" : value;
        }

        if (string.Equals(columnName, PlayableColumnName, StringComparison.OrdinalIgnoreCase))
        {
            return value switch
            {
                SingleMixdownIndicator => "One Mixdown",
                MultipleMixdownsIndicator => "Multiple Mixdowns",
                _ => "No Mixdown"
            };
        }

        return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
    }

    private static string GetSongStatisticsComparisonValue(string columnName, string value)
    {
        if (!string.Equals(columnName, "Tempo", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        if (!double.TryParse(
                value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var tempo)
            && !double.TryParse(value, out tempo))
        {
            return value;
        }

        var lowerBound = (int)Math.Floor(Math.Max(0d, tempo) / 10d) * 10;
        return $"{lowerBound}–{lowerBound + 9} BPM";
    }

    private IReadOnlyList<string>? GetSongStatisticsPreferredOrder(string columnName)
    {
        if (string.Equals(columnName, RatingColumnName, StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                FormatStarRating(5),
                FormatStarRating(4),
                FormatStarRating(3),
                FormatStarRating(2),
                FormatStarRating(1),
                "Unrated"
            ];
        }

        if (string.Equals(columnName, StatusColumnName, StringComparison.OrdinalIgnoreCase))
        {
            return _songStatuses.Concat(["No Status"]).ToArray();
        }

        if (string.Equals(columnName, GenreColumnName, StringComparison.OrdinalIgnoreCase))
        {
            return _songGenres.Concat(["No Genre"]).ToArray();
        }

        if (string.Equals(columnName, PlayableColumnName, StringComparison.OrdinalIgnoreCase))
        {
            return ["Multiple Mixdowns", "One Mixdown", "No Mixdown"];
        }

        return null;
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

    private void ShowPreferencesDialog(IWin32Window? owner = null, bool openCustomColumnsTab = false)
    {
        using var dialog = new PreferencesForm(
            _rootPath,
            _theme.Name,
            _lockCurrentDetailTab,
            _enableSongLaunch,
            BrowserConfigStore.HasExplicitEnableSongLaunchPreference(),
            _songGridDoubleClickAction,
            SongLaunchResolver.GetInstalledApplications().Count == 1,
            _restoreFilterSessionOnStartup,
            BrowserConfigStore.LoadMixdownFolderName(),
            BrowserConfigStore.LoadUseCustomAudioPlayer(),
            BrowserConfigStore.LoadCustomAudioPlayerPath(),
            BrowserConfigStore.LoadCustomAudioPlayerArguments(),
            BrowserConfigStore.LoadUseHistoryRetentionPreset(),
            BrowserConfigStore.LoadHistoryRevisionsToKeep(),
            _songStatuses,
            _songGenres,
            _customSongColumns,
            _fontPreferences,
            _theme,
            owner => ShowVisibleTabsDialog(owner),
            openCustomColumnsTab);

        if (dialog.ShowDialog(owner ?? this) != DialogResult.OK)
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

        if (_songGridDoubleClickAction != dialog.SelectedSongGridDoubleClickAction)
        {
            _songGridDoubleClickAction = dialog.SelectedSongGridDoubleClickAction;
            BrowserConfigStore.SaveSongGridDoubleClickAction(_songGridDoubleClickAction);
        }

        if (_restoreFilterSessionOnStartup != dialog.RestoreFilterSessionOnStartup)
        {
            _restoreFilterSessionOnStartup = dialog.RestoreFilterSessionOnStartup;
            BrowserConfigStore.SaveRestoreFilterSessionOnStartup(_restoreFilterSessionOnStartup);
            BrowserConfigStore.SaveRestoreAdvancedSearchSessionOnStartup(_restoreFilterSessionOnStartup);
            if (!_restoreFilterSessionOnStartup)
            {
                BrowserConfigStore.SaveLastAdvancedSearchQuery(null);
            }
        }

        var currentUseCustomAudioPlayer = BrowserConfigStore.LoadUseCustomAudioPlayer();
        var currentCustomAudioPlayerPath = BrowserConfigStore.LoadCustomAudioPlayerPath();
        var currentCustomAudioPlayerArguments = BrowserConfigStore.LoadCustomAudioPlayerArguments();
        var currentMixdownFolderName = BrowserConfigStore.LoadMixdownFolderName();
        var audioPreferencesChanged = currentUseCustomAudioPlayer != dialog.UseCustomAudioPlayer
            || !string.Equals(currentMixdownFolderName, dialog.MixdownFolderName, StringComparison.Ordinal)
            || !string.Equals(currentCustomAudioPlayerPath, dialog.CustomAudioPlayerPath, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(currentCustomAudioPlayerArguments, dialog.CustomAudioPlayerArguments, StringComparison.Ordinal);
        if (audioPreferencesChanged)
        {
            BrowserConfigStore.SaveAudioPreferences(
                dialog.MixdownFolderName,
                dialog.UseCustomAudioPlayer,
                dialog.CustomAudioPlayerPath,
                dialog.CustomAudioPlayerArguments);
            RefreshPlayableMixdownIndicators();
        }

        var currentUseHistoryRetentionPreset = BrowserConfigStore.LoadUseHistoryRetentionPreset();
        var currentHistoryRevisionsToKeep = BrowserConfigStore.LoadHistoryRevisionsToKeep();
        if (currentUseHistoryRetentionPreset != dialog.UseHistoryRetentionPreset
            || currentHistoryRevisionsToKeep != dialog.HistoryRevisionsToKeep)
        {
            BrowserConfigStore.SaveHistoryRetentionPreferences(
                dialog.UseHistoryRetentionPreset,
                dialog.HistoryRevisionsToKeep);
        }

        var newStatusAdded = dialog.SelectedSongStatuses.Any(status =>
            !_songStatuses.Contains(status, StringComparer.OrdinalIgnoreCase));
        var newGenreAdded = dialog.SelectedSongGenres.Any(genre =>
            !_songGenres.Contains(genre, StringComparer.OrdinalIgnoreCase));

        if (!_songStatuses.SequenceEqual(dialog.SelectedSongStatuses, StringComparer.Ordinal)
            || dialog.SongStatusRenames.Count > 0)
        {
            _songStatuses = dialog.SelectedSongStatuses.ToList();
            BrowserConfigStore.SaveSongStatuses(_songStatuses, dialog.SongStatusRenames);
            RefreshSongAnnotationOptions(StatusColumnName, _songStatuses);
            ApplyRenamedSongOptions(StatusColumnName, dialog.SongStatusRenames);
            RefreshBulkEditOptions();
        }

        if (!_songGenres.SequenceEqual(dialog.SelectedSongGenres, StringComparer.Ordinal)
            || dialog.SongGenreRenames.Count > 0)
        {
            _songGenres = dialog.SelectedSongGenres.ToList();
            BrowserConfigStore.SaveSongGenres(_songGenres, dialog.SongGenreRenames);
            RefreshSongAnnotationOptions(GenreColumnName, _songGenres);
            ApplyRenamedSongOptions(GenreColumnName, dialog.SongGenreRenames);
            RefreshBulkEditOptions();
        }

        if (!_customSongColumns.Select(column => (column.Id, column.Name))
                .SequenceEqual(dialog.SelectedCustomSongColumns.Select(column => (column.Id, column.Name))))
        {
            var previousIds = _customSongColumns.Select(column => column.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var visibleKeys = _songGridColumnFields
                .Where(field => _songGrid.Columns[field.ColumnName]?.Visible == true)
                .Select(field => field.Key)
                .ToList();
            BrowserConfigStore.SaveCustomSongColumns(dialog.SelectedCustomSongColumns);
            var selectedIds = dialog.SelectedCustomSongColumns.Select(column => column.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var oldColumn in _customSongColumns.Where(column => !selectedIds.Contains(column.Id)))
            {
                var columnName = CustomSongColumnName(oldColumn.Id);
                if (_songGrid.Columns.Contains(columnName)) _songGrid.Columns.Remove(columnName);
            }
            _customSongColumns = dialog.SelectedCustomSongColumns
                .Select(column => new CustomSongColumn { Id = column.Id, Name = column.Name }).ToList();
            _songGridColumnFields.Clear();
            _songGridColumnFields.AddRange(BuildSongGridColumnFields());
            foreach (var custom in _customSongColumns)
            {
                var columnName = CustomSongColumnName(custom.Id);
                if (!_songGrid.Columns.Contains(columnName))
                {
                    AddSongColumn(columnName, custom.Name, 150);
                    foreach (DataGridViewRow row in _songGrid.Rows)
                    {
                        if (row.Tag is SongGridRowData rowData)
                            row.Cells[columnName].Value = BrowserConfigStore.LoadSongAnnotation(rowData.Metadata.Path)
                                .CustomValues?.GetValueOrDefault(custom.Id) ?? "";
                    }
                    if (!previousIds.Contains(custom.Id)) visibleKeys.Add(columnName);
                }
                else _songGrid.Columns[columnName]!.HeaderText = custom.Name;
            }
            visibleKeys = visibleKeys.Where(key => _songGridColumnFields.Any(field => field.Key == key))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            BrowserConfigStore.SaveSongGridVisibleColumnKeys(visibleKeys);
            ApplySongGridColumnVisibility(visibleKeys);
            ApplySavedSongGridColumnOrder();
            SetStatus("Custom columns updated.");
        }

        if (!AreFontPreferencesEqual(_fontPreferences, dialog.SelectedFontPreferences))
        {
            var previousMainUiFontSizePoints = _fontPreferences.MainUi;
            _fontPreferences = dialog.SelectedFontPreferences;
            AppFontSettings.SavePreferences(_fontPreferences);
            ApplyFontSize(previousMainUiFontSizePoints);
            ApplyTheme(savePreference: false);
        }

        var rootPathChanged = !string.IsNullOrWhiteSpace(dialog.SelectedRootPath)
            && !string.Equals(Path.GetFullPath(dialog.SelectedRootPath), _rootPath, StringComparison.OrdinalIgnoreCase);
        if (rootPathChanged)
        {
            SetRootPath(dialog.SelectedRootPath);
        }
        else if (newStatusAdded || newGenreAdded)
        {
            RefreshCurrentView();
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
        selectedKeys.Add("playable");

        var selectedColumnNames = _songGridColumnFields
            .Where(field => selectedKeys.Contains(field.Key))
            .Select(field => field.ColumnName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var previouslyVisibleColumnNames = _songGrid.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Visible && selectedColumnNames.Contains(column.Name))
            .OrderBy(column => column.DisplayIndex)
            .Select(column => column.Name)
            .ToList();
        var newlyVisibleColumnNames = _songGridColumnFields
            .Where(field => selectedColumnNames.Contains(field.ColumnName))
            .Select(field => field.ColumnName)
            .Where(columnName => !previouslyVisibleColumnNames.Contains(columnName, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var visibleColumnNames = previouslyVisibleColumnNames
            .Concat(newlyVisibleColumnNames)
            .ToList();

        var hiddenColumnNames = _songGridColumnFields
            .Select(field => field.ColumnName)
            .Where(columnName => !selectedColumnNames.Contains(columnName))
            .ToList();
        var orderedColumnNames = visibleColumnNames
            .Concat(hiddenColumnNames)
            .ToArray();

        foreach (var field in _songGridColumnFields)
        {
            if (_songGrid.Columns[field.ColumnName] is not DataGridViewColumn column)
            {
                continue;
            }

            column.Visible = selectedColumnNames.Contains(field.ColumnName);
        }

        for (var displayIndex = 0; displayIndex < orderedColumnNames.Length; displayIndex++)
        {
            _songGrid.Columns[orderedColumnNames[displayIndex]]!.DisplayIndex = displayIndex;
        }

        ConfigureSongGridColumnSizing();
        if (visibleColumnNames.Count > 0
            && _songGrid.Columns[visibleColumnNames[0]] is DataGridViewColumn firstVisibleColumn)
        {
            _songGrid.FirstDisplayedScrollingColumnIndex = firstVisibleColumn.Index;
        }

        ApplySongGridSort();
    }

    private void HandleSongGridColumnHeaderClick(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= _songGrid.Columns.Count)
        {
            return;
        }

        var column = _songGrid.Columns[columnIndex];
        if (!column.Visible || column.Name == BulkSelectColumnName)
        {
            return;
        }

        var currentPrimarySort = _songGridSortLevels.FirstOrDefault();
        var direction = currentPrimarySort is not null
            && string.Equals(currentPrimarySort.ColumnName, column.Name, StringComparison.OrdinalIgnoreCase)
            ? ParseSortDirection(currentPrimarySort.Direction) == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending
            : GetDefaultSortDirection(column.Name);

        _songGridSortLevels =
        [
            new SongGridSortLevel
            {
                ColumnName = column.Name,
                Direction = direction.ToString()
            }
        ];
        BrowserConfigStore.SaveSongGridSortLevels(_songGridSortLevels);

        RebuildCurrentViewForQuerySort();
    }

    private void RebuildCurrentViewForQuerySort()
    {
        var selectedSongPath = _songGrid.SelectedRows.Count > 0
            ? (_songGrid.SelectedRows[0].Tag as SongGridRowData)?.Metadata.Path
            : _selectedMetadata?.Path;
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            UpdateSongGridSortGlyphs();
            return;
        }

        if (_advancedSearchQuery is not null)
        {
            RunAdvancedSearch(CloneAdvancedSearchQuery(_advancedSearchQuery));
        }
        else if (_searchMode && !string.IsNullOrWhiteSpace(_searchTextBox.Text))
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
        else
            UpdateSongGridSortGlyphs();

        RestoreSongGridSelectionByPath(selectedSongPath);
    }

    private void RestoreSongGridSelectionByPath(string? songPath)
    {
        if (string.IsNullOrWhiteSpace(songPath))
            return;

        for (var rowIndex = 0; rowIndex < _songGrid.Rows.Count; rowIndex++)
        {
            if (!string.Equals(
                    (_songGrid.Rows[rowIndex].Tag as SongGridRowData)?.Metadata.Path,
                    songPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            SelectSongRow(rowIndex, trackRecent: false);
            return;
        }
    }

    private void ApplySongGridSort()
    {
        var normalizedLevels = _songGridSortLevels
            .Where(level => _songGrid.Columns.Contains(level.ColumnName)
                && _songGrid.Columns[level.ColumnName]?.Visible == true
                && !string.Equals(level.ColumnName, BulkSelectColumnName, StringComparison.OrdinalIgnoreCase))
            .GroupBy(level => level.ColumnName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(4)
            .Select(level => new SongGridSortLevel
            {
                ColumnName = level.ColumnName,
                Direction = ParseSortDirection(level.Direction).ToString()
            })
            .ToList();
        if (!AreSameSortLevels(_songGridSortLevels, normalizedLevels))
        {
            _songGridSortLevels = normalizedLevels;
            BrowserConfigStore.SaveSongGridSortLevels(_songGridSortLevels);
        }

        UpdateSongGridSortGlyphs();
    }

    private void QueueSongGridSortAfterValueChange(string columnName)
    {
        if (_songLensViewBuildDepth > 0
            || _songGridSortRefreshPending
            || !IsHandleCreated
            || !_songGridSortLevels.Any(level =>
                string.Equals(level.ColumnName, columnName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _songGridSortRefreshPending = true;
        BeginInvoke(new Action(() =>
        {
            _songGridSortRefreshPending = false;
            if (!IsDisposed)
                RebuildCurrentViewForQuerySort();
        }));
    }

    private void UpdateSongGridSortGlyphs()
    {
        foreach (DataGridViewColumn column in _songGrid.Columns)
        {
            column.HeaderCell.SortGlyphDirection = SortOrder.None;
            var field = _songGridColumnFields.FirstOrDefault(
                candidate => string.Equals(candidate.ColumnName, column.Name, StringComparison.OrdinalIgnoreCase));
            if (field is not null)
            {
                column.HeaderText = field.Label;
            }
        }

        for (var index = 0; index < _songGridSortLevels.Count; index++)
        {
            var level = _songGridSortLevels[index];
            if (!_songGrid.Columns.Contains(level.ColumnName)
                || _songGrid.Columns[level.ColumnName] is not DataGridViewColumn sortedColumn
                || !sortedColumn.Visible)
            {
                continue;
            }

            var arrow = ParseSortDirection(level.Direction) == ListSortDirection.Descending
                ? "\u2193"
                : "\u2191";
            var priority = _songGridSortLevels.Count > 1 ? $"{index + 1}" : "";
            sortedColumn.HeaderText = $"{sortedColumn.HeaderText} {priority}{arrow}";
        }
    }

    private static ListSortDirection ParseSortDirection(string? direction)
    {
        return string.Equals(direction, nameof(ListSortDirection.Descending), StringComparison.OrdinalIgnoreCase)
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
    }

    private static ListSortDirection GetDefaultSortDirection(string columnName)
    {
        return columnName is RatingColumnName or "DateCreated" or "LastModified" or PlayableColumnName
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
    }

    private static bool AreSameSortLevels(
        IReadOnlyList<SongGridSortLevel> left,
        IReadOnlyList<SongGridSortLevel> right)
    {
        return left.Count == right.Count
            && left.Zip(right).All(pair =>
                string.Equals(pair.First.ColumnName, pair.Second.ColumnName, StringComparison.OrdinalIgnoreCase)
                && ParseSortDirection(pair.First.Direction) == ParseSortDirection(pair.Second.Direction));
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

    private void ShowFullMixer()
    {
        if (_selectedMetadata is null)
        {
            return;
        }

        using var dialog = new FullMixerForm(_selectedMetadata, _theme);
        dialog.ShowDialog(this);
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
        _viewFullMixerButton.Enabled = metadata.FullMixerChannels.Count > 0;

        AddSummary("Title", metadata.Title);
        AddSummary("Artist", metadata.Artist);
        AddSummary("Year", metadata.Year);
        AddSummary("Date Created", metadata.DateCreated);
        AddSummary("Last Modified", metadata.LastModified);
        AddSummary("Tempo", metadata.Tempo);
        AddSummary("Key Signature", metadata.KeySignature);
        AddSummary("Time Signature", metadata.TimeSignature);
        AddSummary("Length", metadata.Length);
        AddSummary("Studio Version", SongGeneratorDisplay.ToFriendlyDisplay(metadata.Generator));
        AddSummary("Comment", metadata.Comment);
        AddSummary("Path", metadata.Path);

        PopulatePluginManifest(metadata);

        PopulateTrackGrid(metadata);

        foreach (var group in metadata.Groups)
        {
            _groupGrid.Rows.Add(group.GroupName, string.Join(" | ", group.TrackNames));
        }

        var mainChannelNames = metadata.MixerMainChannels
            .Where(channel => channel.IsMainOutput)
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
            ? "The session note is empty"
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
        SetSelectedSongStatus(metadata);
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

    private void ShowVisibleTabsDialog(IWin32Window? owner = null)
    {
        if (_detailTabDefinitions is null || _detailTabDefinitions.Count == 0)
        {
            return;
        }

        var tabOptions = _detailTabDefinitions
            .Select(definition => (definition.Key, definition.TabPage.Text))
            .ToArray();
        using var dialog = new DetailTabVisibilityForm(tabOptions, GetVisibleDetailTabKeys(), _theme);
        if (dialog.ShowDialog(owner ?? this) != DialogResult.OK)
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
        var launchTargets = SongLaunchResolver.GetInstalledLaunchTargets(metadata);
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
        _contextOpenInRecommendedAppMenuItem.Visible = _enableSongLaunch
            && primaryApplication != SongHostApplication.Unknown;
        _contextOpenInAlternateAppMenuItem.Visible = _enableSongLaunch && hasAlternateLaunch;
    }

    private void UpdateMixdownPlaybackMenu()
    {
        _contextPlaySongMenuItem.DropDownItems.Clear();
        _contextPlaySongMenuItem.Tag = null;

        var matchingFiles = _selectedMetadata is null
            ? Array.Empty<string>()
            : MixdownAudioPlayback.FindMatchingFiles(_selectedMetadata).ToArray();
        _contextPlaySongMenuItem.Visible = matchingFiles.Length > 0;
        _contextSongActionsSeparator.Visible = _contextOpenInRecommendedAppMenuItem.Visible
            || _contextOpenInAlternateAppMenuItem.Visible
            || matchingFiles.Length > 0;

        if (matchingFiles.Length == 1)
        {
            _contextPlaySongMenuItem.Tag = matchingFiles[0];
            _contextPlaySongMenuItem.ToolTipText = matchingFiles[0];
            return;
        }

        _contextPlaySongMenuItem.ToolTipText = "";
        foreach (var audioPath in matchingFiles)
        {
            var modified = File.GetLastWriteTime(audioPath).ToString("g");
            var item = new ToolStripMenuItem(
                $"{MixdownAudioPlayback.GetFormatLabel(audioPath)} — {Path.GetFileNameWithoutExtension(audioPath)} — Modified {modified}")
            {
                Tag = audioPath,
                ToolTipText = audioPath,
                BackColor = _theme.PanelBackColor,
                ForeColor = _theme.TextColor
            };
            item.Click += (_, _) => PlayMixdownAudio(audioPath);
            _contextPlaySongMenuItem.DropDownItems.Add(item);
        }

        if (_contextPlaySongMenuItem.DropDownItems.Count > 0)
        {
            _contextPlaySongMenuItem.DropDown.BackColor = _theme.PanelBackColor;
            _contextPlaySongMenuItem.DropDown.ForeColor = _theme.TextColor;
            _contextPlaySongMenuItem.DropDown.Renderer = new ThemeMenuRenderer(_theme);
        }
    }

    private void PlaySingleMixdownFromContextMenu()
    {
        if (_contextPlaySongMenuItem.DropDownItems.Count == 0
            && _contextPlaySongMenuItem.Tag is string audioPath)
        {
            PlayMixdownAudio(audioPath);
        }
    }

    private void PlayMixdownFromIndicator(SongMetadata metadata)
    {
        var matchingFiles = MixdownAudioPlayback.FindMatchingFiles(metadata);
        if (matchingFiles.Count == 1)
        {
            PlayMixdownAudio(matchingFiles[0]);
            return;
        }

        if (matchingFiles.Count > 1)
        {
            using var dialog = new MixdownSelectionForm(metadata.FileName, matchingFiles, _theme);
            if (dialog.ShowDialog(this) == DialogResult.OK
                && dialog.SelectedAudioPath is string selectedAudioPath)
            {
                PlayMixdownAudio(selectedAudioPath);
            }
        }
    }

    private void PlayMixdownAudio(string audioPath)
    {
        try
        {
            MixdownAudioPlayback.Play(audioPath);
            SetStatus($"Playing {Path.GetFileName(audioPath)}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            using var messageDialog = new ThemedMessageForm(
                Text,
                $"Could not play the mixdown audio file.\n\n{exception.Message}",
                _theme,
                ThemedMessageKind.Error);
            messageDialog.ShowDialog(this);
            SetStatus($"Playback failed: {exception.Message}");
        }
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

        var alternateApplication = SongLaunchResolver.GetInstalledLaunchTargets(_selectedMetadata)
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
            var originalPath = _selectedMetadata.Path;
            File.Move(originalPath, renamedPath);
            BrowserConfigStore.MoveSongAnnotation(originalPath, renamedPath);
            InvalidateCatalogStatisticsCache();
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
        var primaryApplication = SongLaunchResolver.GetInstalledLaunchTargets(metadata)
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

    private void OpenSongInSpecificApp(
        SongMetadata metadata,
        SongHostApplication application,
        IWin32Window? dialogOwner = null)
    {
        dialogOwner ??= this;
        if (!File.Exists(metadata.Path))
        {
            using var messageDialog = new ThemedMessageForm(Text, $"Song file not found:\n{metadata.Path}", _theme, ThemedMessageKind.Warning);
            messageDialog.ShowDialog(dialogOwner);
            SetStatus($"File not found: {metadata.Path}");
            return;
        }

        var launchPlan = SongLaunchResolver.CreatePlan(application);

        var executablePath = launchPlan.ExecutablePath;
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            executablePath = PromptForLaunchExecutable(launchPlan, dialogOwner);
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
            messageDialog.ShowDialog(dialogOwner);
            SetStatus($"Launch failed: {ex.Message}");
        }
    }

    private string? PromptForLaunchExecutable(SongLaunchPlan launchPlan, IWin32Window? dialogOwner = null)
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

        if (dialog.ShowDialog(dialogOwner ?? this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return null;
        }

        SongLaunchResolver.SaveExecutablePath(launchPlan.Application, dialog.FileName);
        return dialog.FileName;
    }

    private void ShowSongAgeFilterDialog()
    {
        var filterPreference = BrowserConfigStore.LoadSongAgeFilterPreference() ?? _songAgeFilter;
        var viewAllSongs = _songAgeFilter is null;
        while (true)
        {
            using var dialog = new SongAgeFilterForm(filterPreference, viewAllSongs, _theme);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            if (dialog.RequestedAction == SongAgeFilterDialogAction.CustomizeView)
            {
                ShowSongGridColumnsDialog();
                filterPreference = dialog.FilterPreference;
                viewAllSongs = dialog.ViewAllSongsSelected;
                continue;
            }

            if (dialog.RequestedAction == SongAgeFilterDialogAction.AdvancedSearch)
            {
                ShowAdvancedSearchDialog();
                return;
            }

            filterPreference = dialog.FilterPreference;
            viewAllSongs = dialog.ViewAllSongsSelected;
            _songAgeFilter = dialog.SelectedFilter;
            break;
        }

        BrowserConfigStore.SaveSongAgeFilterPreference(filterPreference);
        BrowserConfigStore.SaveViewAllSongs(viewAllSongs);

        UpdateStatusIndicators();
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            SetStatus(viewAllSongs ? "Viewing all songs." : BuildSongFilterStatusMessage(_songAgeFilter!));
            return;
        }

        SetRootPath(_rootPath);
        LoadAllSongs();
        SetStatus(viewAllSongs ? "Viewing all songs." : BuildSongFilterStatusMessage(_songAgeFilter!));
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

        var sortColumns = _songGrid.Columns
            .Cast<DataGridViewColumn>()
            .Where(column => column.Visible && column.Name != BulkSelectColumnName)
            .OrderBy(column => column.DisplayIndex)
            .Select(column => (
                column.Name,
                _songGridColumnFields.FirstOrDefault(field =>
                    string.Equals(field.ColumnName, column.Name, StringComparison.OrdinalIgnoreCase))?.Label
                ?? column.HeaderText))
            .ToArray();
        using var dialog = new AdvancedSearchForm(
            _theme,
            _advancedSearchQuery ?? _lastAdvancedSearchQuery,
            _savedAdvancedSearches,
            _songStatuses,
            _songGenres,
            filterSortMode: true,
            sortColumns: sortColumns,
            initialSortLevels: _songGridSortLevels);
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
        _songGridSortLevels = CloneSongGridSortLevels(dialog.SelectedSortLevels);
        BrowserConfigStore.SaveSongGridSortLevels(_songGridSortLevels);
        RunAdvancedSearch(dialog.SearchQuery);
    }

    private void ShowHelpDialog()
    {
        using var dialog = new HelpForm(_theme);
        dialog.ShowDialog(this);
    }

    private void ShowSongGridHelpDialog()
    {
        using var dialog = new HelpForm(_theme, focusSongLensView: true);
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

        using var dialog = new HistoryForm(
            _selectedMetadata,
            _theme,
            (owner, revisionMetadata, application) =>
                OpenSongInSpecificApp(revisionMetadata, application, owner));
        dialog.ShowDialog(this);
        RestorePreviousDetailTab();
        if (dialog.FilesDeleted)
        {
            RefreshAfterHistoryCleanup();
            return;
        }

        SetSelectedSongStatus(_selectedMetadata);
    }

    private void RefreshAfterHistoryCleanup()
    {
        var wasAllSongsMode = _allSongsMode;
        var wasSearchMode = _searchMode;
        var searchText = _searchTextBox.Text;
        var advancedSearchQuery = _advancedSearchQuery is null
            ? null
            : CloneAdvancedSearchQuery(_advancedSearchQuery);
        var preferredFolderPath = _folderTree.SelectedNode?.Tag as string ?? _selectedMetadata?.Folder ?? _rootPath;
        if (IsHistoryDirectoryUnderRoot(preferredFolderPath))
        {
            preferredFolderPath = Directory.GetParent(preferredFolderPath)?.FullName ?? _rootPath;
        }

        RebuildFolderTree(preferredFolderPath);
        if (advancedSearchQuery is not null)
        {
            RunAdvancedSearch(advancedSearchQuery);
            return;
        }

        if (wasSearchMode && !string.IsNullOrWhiteSpace(searchText))
        {
            SetFastSearchText(searchText);
            SearchSongs();
            return;
        }

        if (wasAllSongsMode)
        {
            LoadAllSongs();
        }
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

    private IReadOnlyList<SongQueryRow> OrderSongQueryRows(IEnumerable<SongQueryRow> rows)
    {
        var orderedRows = rows.ToList();
        orderedRows.Sort(CompareSongQueryRows);
        return orderedRows;
    }

    private int CompareSongQueryRows(SongQueryRow left, SongQueryRow right)
    {
        foreach (var level in _songGridSortLevels)
        {
            var leftValue = GetSongQuerySortValue(left, level.ColumnName);
            var rightValue = GetSongQuerySortValue(right, level.ColumnName);
            var comparison = CompareSongQuerySortValues(level.ColumnName, leftValue, rightValue);
            if (comparison != 0)
            {
                return ParseSortDirection(level.Direction) == ListSortDirection.Descending
                    ? -comparison
                    : comparison;
            }
        }

        return StringComparer.CurrentCultureIgnoreCase.Compare(left.Metadata.Path, right.Metadata.Path);
    }

    private object? GetSongQuerySortValue(SongQueryRow row, string columnName)
    {
        if (string.Equals(columnName, StatusColumnName, StringComparison.OrdinalIgnoreCase))
            return row.Annotation.Status;
        if (string.Equals(columnName, GenreColumnName, StringComparison.OrdinalIgnoreCase))
            return row.Annotation.Genre;
        if (string.Equals(columnName, RatingColumnName, StringComparison.OrdinalIgnoreCase))
            return row.Annotation.Rating;

        var customColumn = _customSongColumns.FirstOrDefault(
            custom => string.Equals(CustomSongColumnName(custom.Id), columnName, StringComparison.OrdinalIgnoreCase));
        if (customColumn is not null)
            return row.Annotation.CustomValues?.GetValueOrDefault(customColumn.Id);

        return _songGridColumnFields.FirstOrDefault(
            field => string.Equals(field.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
            ?.ValueSelector(row.Metadata, row.Match);
    }

    private int CompareSongQuerySortValues(string columnName, object? left, object? right)
    {
        var leftText = left?.ToString()?.Trim() ?? "";
        var rightText = right?.ToString()?.Trim() ?? "";
        var leftEmpty = string.IsNullOrWhiteSpace(leftText);
        var rightEmpty = string.IsNullOrWhiteSpace(rightText);
        if (leftEmpty || rightEmpty)
        {
            if (leftEmpty && rightEmpty) return 0;
            return leftEmpty ? 1 : -1;
        }

        if (string.Equals(columnName, StatusColumnName, StringComparison.OrdinalIgnoreCase))
        {
            var leftIndex = _songStatuses.FindIndex(status => string.Equals(status, leftText, StringComparison.OrdinalIgnoreCase));
            var rightIndex = _songStatuses.FindIndex(status => string.Equals(status, rightText, StringComparison.OrdinalIgnoreCase));
            if (leftIndex >= 0 || rightIndex >= 0)
            {
                if (leftIndex >= 0 && rightIndex >= 0) return leftIndex.CompareTo(rightIndex);
                return leftIndex >= 0 ? -1 : 1;
            }
        }

        if (left is int leftInteger && right is int rightInteger)
            return leftInteger.CompareTo(rightInteger);
        if (DateTime.TryParse(leftText, out var leftDate) && DateTime.TryParse(rightText, out var rightDate))
            return leftDate.CompareTo(rightDate);
        if (decimal.TryParse(leftText, out var leftNumber) && decimal.TryParse(rightText, out var rightNumber))
            return leftNumber.CompareTo(rightNumber);
        return StringComparer.CurrentCultureIgnoreCase.Compare(leftText, rightText);
    }

    private bool IsCatalogSongFile(string songPath)
    {
        return SongMetadataReader.IsRegularSongFile(Path.GetFileName(songPath))
            && !IsHistoryDirectoryUnderRoot(Path.GetDirectoryName(songPath));
    }

    private bool IsHistoryDirectoryUnderRoot(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || string.IsNullOrWhiteSpace(_rootPath))
        {
            return false;
        }

        var rootPath = Path.GetFullPath(_rootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var directory = new DirectoryInfo(directoryPath);
        while (directory is not null)
        {
            var currentPath = directory.FullName
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(currentPath, rootPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(directory.Name, "History", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            directory = directory.Parent;
        }

        return false;
    }

    private bool DirectoryContainsVisibleSongs(string path)
    {
        if (IsHistoryDirectoryUnderRoot(path))
        {
            return false;
        }

        if (_folderVisibilityCache.TryGetValue(path, out var isVisible))
        {
            return isVisible;
        }

        var visible = false;

        try
        {
            visible = Directory.EnumerateFiles(path, "*.song")
                .Where(IsCatalogSongFile)
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
                .Where(IsCatalogSongFile)
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
                .Where(IsCatalogSongFile)
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

    private void PopulatePluginManifest(SongMetadata metadata)
    {
        foreach (var pluginDevice in metadata.PluginDeviceDetails)
        {
            _pluginsGrid.Rows.Add(
                pluginDevice.DeviceName,
                FormatField(pluginDevice.Category),
                FormatField(pluginDevice.Subcategory),
                FormatField(pluginDevice.PresetType));
        }
    }

    private void AutoSizeDetailColumns()
    {
        AutoSizeGridColumns(_summaryGrid, 140, SummaryGridKey);
        AutoSizeGridColumns(_pluginsGrid, 180, PluginsGridKey);
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

    private void SetStatus(string message, bool isWarning = false)
    {
        _statusLabel.Text = message;
        _diagnosticLogLinkLabel.Visible = isWarning;
        if (isWarning)
        {
            ApplyStatusWarningAppearance();
            return;
        }

        _statusLabel.BackColor = _theme.StatusBackColor;
        _statusLabel.ForeColor = _theme.TextColor;
        _statusLabel.Font = Font;
        _diagnosticLogLinkLabel.BackColor = _theme.StatusBackColor;
    }

    private void ShowDiagnosticLog()
    {
        using var dialog = new DiagnosticLogForm(_theme, _selectedMetadata?.Path);
        dialog.ShowDialog(this);
    }

    private void ApplyStatusWarningAppearance()
    {
        var warningBackColor = Color.FromArgb(255, 224, 128);
        _statusLabel.BackColor = warningBackColor;
        _statusLabel.ForeColor = Color.Black;
        _statusLabel.Font = Font;
        _diagnosticLogLinkLabel.BackColor = warningBackColor;
        _diagnosticLogLinkLabel.ForeColor = Color.Black;
        _diagnosticLogLinkLabel.LinkColor = Color.Black;
        _diagnosticLogLinkLabel.ActiveLinkColor = Color.FromArgb(70, 70, 70);
        _diagnosticLogLinkLabel.VisitedLinkColor = Color.Black;
        _diagnosticLogLinkLabel.Font = _statusLabel.Font;
    }

    private void SetSelectedSongStatus(SongMetadata metadata)
    {
        if (metadata.ReadWarnings.Count > 0)
        {
            SetStatus(
                $"{metadata.FileName} loaded with limited metadata; some information may be unavailable.",
                isWarning: true);
            return;
        }

        SetStatus($"Selected {metadata.FileName}");
    }

    private void SetStatusForSelectedSongWarningOr(string fallbackMessage)
    {
        if (_selectedMetadata is { ReadWarnings.Count: > 0 } metadata)
        {
            SetSelectedSongStatus(metadata);
            return;
        }

        SetStatus(fallbackMessage);
    }

    private void BeginSongLensViewBuild(string message)
    {
        _songLensViewBuildDepth++;
        if (_songLensViewBuildDepth > 1)
        {
            return;
        }

        _songLensViewLoadingLabel.Text = message;
        _songLensViewProgressBar.MarqueeAnimationSpeed = 28;
        SetStatus(message);
        PositionDetailHeaderControls();
        _songLensViewLoadingPanel.BringToFront();
        _songGridHeaderPanel.Refresh();
    }

    private void EndSongLensViewBuild()
    {
        if (_songLensViewBuildDepth == 0)
        {
            return;
        }

        _songLensViewBuildDepth--;
        if (_songLensViewBuildDepth > 0)
        {
            return;
        }

        _songLensViewProgressBar.MarqueeAnimationSpeed = 0;
        UpdateBulkSelectionCount();
        PositionDetailHeaderControls();
        _songGridHeaderPanel.Refresh();
    }

    private void ClearDetailViews()
    {
        _summaryGrid.Rows.Clear();
        _pluginsGrid.Rows.Clear();
        _trackGrid.Rows.Clear();
        _groupGrid.Rows.Clear();
        _mixerMainGrid.Rows.Clear();
        _mixerInsertGrid.Rows.Clear();
        _mixerSendGrid.Rows.Clear();
        _viewFullMixerButton.Enabled = false;
        _notesTextBox.Clear();
        UpdateMixerLayoutHeights();
    }

    private void PositionDetailHeaderControls()
    {
        if (_detailTabsHostPanel is null || _detailTabsHostPanel.IsDisposed)
        {
            return;
        }

        // Snapshot changes to the wider "Snapshot Results" label for multi-song
        // views. Force all buttons to their final sizes before positioning
        // them so an intermediate layout cannot briefly overlap the controls.
        RefreshAutoSizedButton(_snapshotButton);
        RefreshAutoSizedButton(_songGridColumnsButton);
        RefreshAutoSizedButton(_statisticsButton);
        RefreshAutoSizedButton(_filterByDateButton);

        const int buttonGap = 4;
        const int edgePadding = 6;

        var helpY = Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - _songGridHelpButton.Height) / 2);
        var helpX = Math.Max(0, _songGridHeaderPanel.ClientSize.Width - _songGridHelpButton.Width - edgePadding);
        _songGridHelpButton.Location = new Point(helpX, helpY);

        var snapshotY = Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - _snapshotButton.Height) / 2);
        var snapshotX = Math.Max(0, helpX - _snapshotButton.Width - buttonGap);
        _snapshotButton.Location = new Point(snapshotX, snapshotY);

        var statisticsY = Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - _statisticsButton.Height) / 2);
        var statisticsX = Math.Max(0, snapshotX - _statisticsButton.Width - buttonGap);
        _statisticsButton.Location = new Point(statisticsX, statisticsY);

        var filterY = Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - _filterByDateButton.Height) / 2);
        var filterX = Math.Max(0, statisticsX - _filterByDateButton.Width - buttonGap);
        _filterByDateButton.Location = new Point(filterX, filterY);

        var columnsY = Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - _songGridColumnsButton.Height) / 2);
        var columnsX = Math.Max(0, filterX - _songGridColumnsButton.Width - buttonGap);
        _songGridColumnsButton.Location = new Point(columnsX, columnsY);

        var refreshY = Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - _refreshButton.Height) / 2);
        _refreshButton.Location = new Point(4, refreshY);

        var recentlyViewedY = Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - _recentlyViewedButton.Height) / 2);
        _recentlyViewedButton.Location = new Point(_refreshButton.Right + buttonGap, recentlyViewedY);

        var viewBackY = Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - _viewBackButton.Height) / 2);
        _viewBackButton.Location = new Point(_recentlyViewedButton.Right + buttonGap, viewBackY);

        var viewForwardY = Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - _viewForwardButton.Height) / 2);
        _viewForwardButton.Location = new Point(_viewBackButton.Right + buttonGap, viewForwardY);

        var titleAreaLeft = _viewForwardButton.Right + buttonGap;
        var titleAreaRight = columnsX - buttonGap;
        var titleAreaWidth = Math.Max(0, titleAreaRight - titleAreaLeft);
        var titleFont = GetSongGridTitleFont(18);
        _songGridTitleLabel.Font = titleFont;
        var titleHeight = TextRenderer.MeasureText(
            _songGridTitleLabel.Text,
            titleFont,
            new Size(Math.Max(1, titleAreaWidth), int.MaxValue),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Height;
        _songGridTitleLabel.Size = new Size(titleAreaWidth, titleHeight);
        var titleX = titleAreaLeft;
        var titleY = Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - titleHeight) / 2);
        _songGridTitleLabel.Location = new Point(titleX, titleY);
        _songGridTitleLabel.Visible = _songLensViewBuildDepth == 0 && titleAreaWidth >= 120;

        var loadingSize = _songLensViewLoadingPanel.GetPreferredSize(Size.Empty);
        _songLensViewLoadingPanel.Size = loadingSize;
        _songLensViewLoadingPanel.Location = new Point(
            titleAreaLeft + Math.Max(0, (titleAreaWidth - loadingSize.Width) / 2),
            Math.Max(0, (_songGridHeaderPanel.ClientSize.Height - loadingSize.Height) / 2));
        _songLensViewLoadingPanel.Visible = _songLensViewBuildDepth > 0 && titleAreaWidth >= loadingSize.Width;

        AlignBulkEditActionsWithHeader();
    }

    private void AlignBulkEditActionsWithHeader()
    {
        if (_bulkEditBar.IsDisposed
            || _bulkSetRatingButton.IsDisposed
            || _bulkDeleteButton.IsDisposed
            || _bulkDoneButton.IsDisposed)
            return;

        if (_bulkDeleteButton.Left <= 0 || _bulkDoneButton.Left <= 0)
        {
            // Establish the initial flow position while the bulk bar is still
            // hidden during form construction.
            _bulkDeleteButton.Margin = new Padding(1, 1, 1, 0);
            _bulkDoneButton.Margin = new Padding(1, 1, 1, 0);
            _bulkDoneButton.Width = _snapshotButton.Width;
            _bulkEditBar.PerformLayout();
        }

        var availableSpace = Math.Max(
            0,
            _snapshotButton.Left - _bulkSetRatingButton.Right - _bulkDeleteButton.Width);
        var firstGap = availableSpace / 2;
        var secondGap = availableSpace - firstGap;
        var desiredDeleteMargin = new Padding(
            Math.Max(1, firstGap - _bulkSetRatingButton.Margin.Right),
            1,
            1,
            0);
        var desiredDoneMargin = new Padding(
            Math.Max(1, secondGap - desiredDeleteMargin.Right),
            1,
            1,
            0);

        if (_bulkDoneButton.Width == _snapshotButton.Width
            && _bulkDeleteButton.Margin == desiredDeleteMargin
            && _bulkDoneButton.Margin == desiredDoneMargin)
            return;

        _bulkEditBar.SuspendLayout();
        _bulkDoneButton.Width = _snapshotButton.Width;
        _bulkDeleteButton.Margin = desiredDeleteMargin;
        _bulkDoneButton.Margin = desiredDoneMargin;
        _bulkEditBar.ResumeLayout(performLayout: true);
    }

    private static void RefreshAutoSizedButton(Button button)
    {
        var preferredSize = button.GetPreferredSize(Size.Empty);
        preferredSize.Height = Math.Max(20, preferredSize.Height - 2);
        if (button.Size != preferredSize)
        {
            button.Size = preferredSize;
        }
    }

    private Font GetSongGridTitleFont(int size)
    {
        if (!_songGridTitleFonts.TryGetValue(size, out var font))
        {
            font = new Font("Calibri", size, FontStyle.Bold, GraphicsUnit.Point);
            _songGridTitleFonts[size] = font;
        }

        return font;
    }

    private void ReserveSongGridHeaderButtonWidths()
    {
        _filterByDateButton.MinimumSize = Size.Empty;
        _songGridColumnsButton.MinimumSize = Size.Empty;
        _statisticsButton.MinimumSize = Size.Empty;
        _snapshotButton.MinimumSize = Size.Empty;

        var currentSnapshotText = _snapshotButton.Text;
        var sharedWidth = Math.Max(
            _filterByDateButton.GetPreferredSize(Size.Empty).Width,
            _songGridColumnsButton.GetPreferredSize(Size.Empty).Width);
        sharedWidth = Math.Max(sharedWidth, _statisticsButton.GetPreferredSize(Size.Empty).Width);
        _snapshotButton.Text = "Snapshot";
        sharedWidth = Math.Max(sharedWidth, _snapshotButton.GetPreferredSize(Size.Empty).Width);

        _snapshotButton.Text = currentSnapshotText;
        var sharedMinimumSize = new Size(sharedWidth, 0);
        _filterByDateButton.MinimumSize = sharedMinimumSize;
        _songGridColumnsButton.MinimumSize = sharedMinimumSize;
        _statisticsButton.MinimumSize = sharedMinimumSize;
        _snapshotButton.MinimumSize = sharedMinimumSize;
    }

    private void UpdateSnapshotActionsAvailability()
    {
        var hasSelectedSong = _selectedMetadata is not null;
        var isSongGridResults = IsSongGridResultsView();
        var hasSongGridResults = isSongGridResults && _songGrid.Rows.Count > 0;
        var canChooseSnapshotScope = isSongGridResults && hasSelectedSong && _songGrid.Rows.Count > 1;
        _saveSnapshotMenuItem.Text = canChooseSnapshotScope
            ? "Save Snapshot..."
            : isSongGridResults
                ? "Save Results Snapshot..."
                : "Save Snapshot...";
        _saveSnapshotMenuItem.Enabled = hasSelectedSong || hasSongGridResults;

        _songGridHeaderPanel.SuspendLayout();
        try
        {
            _snapshotButton.Text = "Snapshot";
            _snapshotButton.AccessibleName = _snapshotButton.Text;
            _snapshotButton.Enabled = hasSelectedSong || hasSongGridResults;
            _statisticsButton.Enabled = _songGrid.Rows.Count > 0;
            _toolTip.SetToolTip(
                _snapshotButton,
                canChooseSnapshotScope
                    ? "Snapshot the selected song or all current results"
                    : isSongGridResults
                        ? "Preview the current SongLens View results"
                        : "View Song Snapshot");
            _trackViewOptionsDropDownButton.Enabled = hasSelectedSong;
            PositionDetailHeaderControls();
        }
        finally
        {
            _songGridHeaderPanel.ResumeLayout(performLayout: false);
            _songGridHeaderPanel.Invalidate();
        }
    }

    private void PopulateTrackGrid(SongMetadata metadata)
    {
        var tracks = GetVisibleTracks(metadata);

        foreach (var track in tracks)
        {
            var rowIndex = _trackGrid.Rows.Add(
                track.TrackNumber.ToString(),
                "",
                track.TrackName,
                FormatField(track.InstrumentName),
                FormatField(track.TrackNote));
            var row = _trackGrid.Rows[rowIndex];
            row.Tag = track;
            if (track.IsDisabled)
            {
                row.Cells["Disabled"].ToolTipText = "Disabled track";
            }
            ApplyFolderTrackRowStyle(row, track);
        }
    }

    private void ApplyFolderTrackRowStyles()
    {
        foreach (DataGridViewRow row in _trackGrid.Rows)
        {
            if (row.Tag is TrackInstrumentInfo track)
            {
                ApplyFolderTrackRowStyle(row, track);
            }
        }
    }

    private void ApplyFolderTrackRowStyle(DataGridViewRow row, TrackInstrumentInfo track)
    {
        if (track.IsFolderTrack)
        {
            row.DefaultCellStyle.BackColor = BlendColor(_theme.PanelBackColor, _theme.AccentSoftColor, 0.45);
        }

        if (track.IsDisabled)
        {
            row.DefaultCellStyle.ForeColor = _theme.MutedTextColor;
        }

        if (_hideFolderTracks && !track.IsFolderTrack)
        {
            return;
        }

        if (track.IsFolderTrack || track.HierarchyDepth > 0)
        {
            const int baseContentOffset = 22;
            const int hierarchyIndent = 18;
            var leftPadding = AppFontSettings.Scale(
                (track.IsFolderTrack ? baseContentOffset : 0) + (track.HierarchyDepth * hierarchyIndent),
                _fontPreferences,
                AppFontSection.DetailGrids);
            row.Cells["Track"].Style.Padding = new Padding(leftPadding, 0, 0, 0);
        }
    }

    private void DrawTrackGridFolderIcon(object? sender, DataGridViewCellPaintingEventArgs args)
    {
        if (args.RowIndex < 0
            || args.ColumnIndex != _trackGrid.Columns["Track"]?.Index
            || _trackGrid.Rows[args.RowIndex].Tag is not TrackInstrumentInfo { IsFolderTrack: true })
        {
            return;
        }

        var track = (TrackInstrumentInfo)_trackGrid.Rows[args.RowIndex].Tag!;
        var imageKey = track.HasChildFolders ? ClosedParentFolderImageKey : ClosedFolderImageKey;
        var image = _folderImages.Images[imageKey];
        var graphics = args.Graphics;
        if (image is null || graphics is null)
        {
            return;
        }

        args.Paint(args.CellBounds, args.PaintParts);
        var iconSize = Math.Min(
            AppFontSettings.Scale(16, _fontPreferences, AppFontSection.DetailGrids),
            Math.Max(1, args.CellBounds.Height - 4));
        var iconX = args.CellBounds.Left + 4 + AppFontSettings.Scale(
            track.HierarchyDepth * 18,
            _fontPreferences,
            AppFontSection.DetailGrids);
        var iconY = args.CellBounds.Top + Math.Max(0, (args.CellBounds.Height - iconSize) / 2);
        graphics.DrawImage(image, iconX, iconY, iconSize, iconSize);
        args.Handled = true;
    }

    private void DrawDisabledTrackIcon(object? sender, DataGridViewCellPaintingEventArgs args)
    {
        if (args.RowIndex < 0
            || args.ColumnIndex != _trackGrid.Columns["Disabled"]?.Index
            || _trackGrid.Rows[args.RowIndex].Tag is not TrackInstrumentInfo { IsDisabled: true })
        {
            return;
        }

        var graphics = args.Graphics;
        if (graphics is null)
        {
            return;
        }

        args.Paint(args.CellBounds, args.PaintParts);
        var iconSize = Math.Min(12, Math.Max(6, args.CellBounds.Height - 8));
        var iconX = args.CellBounds.Left + Math.Max(2, (args.CellBounds.Width - iconSize) / 2);
        var iconY = args.CellBounds.Top + Math.Max(2, (args.CellBounds.Height - iconSize) / 2);
        var iconColor = args.State.HasFlag(DataGridViewElementStates.Selected)
            ? _theme.SelectedTextColor
            : _theme.MutedTextColor;
        using var pen = new Pen(iconColor, 1.5f);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.DrawEllipse(pen, iconX, iconY, iconSize, iconSize);
        graphics.DrawLine(pen, iconX + 2, iconY + iconSize - 2, iconX + iconSize - 2, iconY + 2);
        args.Handled = true;
    }

    private void ToggleTracksWithEventsOnly()
    {
        _showTracksWithEventsOnly = _tracksWithEventsMenuItem.Checked;
        BrowserConfigStore.SaveShowTracksWithEventsOnly(_showTracksWithEventsOnly);

        RefreshTrackGrid();

        SetStatus(BuildTrackFilterStatusMessage());
    }

    private void ToggleHideFolderTracks()
    {
        _hideFolderTracks = _hideFolderTracksMenuItem.Checked;
        BrowserConfigStore.SaveHideFolderTracks(_hideFolderTracks);
        RefreshTrackGrid();

        SetStatus(BuildTrackFilterStatusMessage());
    }

    private void ToggleHideDisabledTracks()
    {
        _hideDisabledTracks = _hideDisabledTracksMenuItem.Checked;
        BrowserConfigStore.SaveHideDisabledTracks(_hideDisabledTracks);
        RefreshTrackGrid();

        SetStatus(BuildTrackFilterStatusMessage());
    }

    private IReadOnlyList<TrackInstrumentInfo> GetVisibleTracks(SongMetadata metadata)
    {
        IEnumerable<TrackInstrumentInfo> tracks = metadata.TrackInstruments;
        if (_showTracksWithEventsOnly)
        {
            tracks = tracks.Where(track => track.HasEvents);
        }

        if (_hideFolderTracks)
        {
            tracks = tracks.Where(track => !track.IsFolderTrack);
        }

        if (_hideDisabledTracks)
        {
            tracks = tracks.Where(track => !track.IsDisabled);
        }

        return tracks.ToArray();
    }

    private void RefreshTrackGrid()
    {
        if (_selectedMetadata is null)
        {
            return;
        }

        _trackGrid.Rows.Clear();
        PopulateTrackGrid(_selectedMetadata);
        AutoSizeGridColumns(_trackGrid, 50, TrackGridKey);
    }

    private string BuildTrackFilterStatusMessage()
    {
        var description = _showTracksWithEventsOnly
            ? "Showing tracks with events"
            : "Showing all tracks";
        var hiddenTypes = new List<string>(2);
        if (_hideDisabledTracks)
        {
            hiddenTypes.Add("disabled tracks");
        }

        if (_hideFolderTracks)
        {
            hiddenTypes.Add("folders");
        }

        return hiddenTypes.Count == 0
            ? $"{description}."
            : $"{description}; {string.Join(" and ", hiddenTypes)} are hidden.";
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
        BrowserConfigStore.SaveLastAdvancedSearchQuery(
            _restoreFilterSessionOnStartup ? _advancedSearchQuery : null);
        var persistedWindowSize = WindowState == FormWindowState.Normal ? Size : RestoreBounds.Size;
        BrowserConfigStore.SaveMainWindowSize(persistedWindowSize);
    }

    private void UpdateStatusIndicators()
    {
        _filterStatusLabel.Text = $"Filter: {BuildFilterStatusText()}";
        _stickyTabsStatusLabel.Text = $"Sticky Tabs: {(_lockCurrentDetailTab ? "On" : "Off")}";
        _refreshButton.Enabled = !string.IsNullOrWhiteSpace(_rootPath)
            && Directory.Exists(_rootPath);
    }

    private string BuildFilterStatusText()
    {
        if (_advancedSearchQuery is not null && _advancedSearchQuery.Rules.Count > 0)
        {
            var joinText = _advancedSearchQuery.MatchMode == AdvancedSearchMatchMode.AllRules ? "all" : "any";
            return $"{joinText} of {_advancedSearchQuery.Rules.Count} rule{(_advancedSearchQuery.Rules.Count == 1 ? "" : "s")}";
        }

        if (_songAgeFilter is null)
        {
            return _allSongsMode ? "All Songs" : "Off";
        }

        if (_songAgeFilter.Mode == SongAgeFilterMode.DateRange
            && _songAgeFilter.StartDate is DateTime startDate
            && _songAgeFilter.EndDate is DateTime endDate)
        {
            var dateFieldLabel = _songAgeFilter.DateField == SongDateField.Created ? "created" : "modified";
            return $"{dateFieldLabel} between {startDate:d} and {endDate:d}";
        }

        var cutoffDate = DateTime.Now.Subtract(TimeSpan.FromDays(_songAgeFilter.Days));
        var cutoffLabel = cutoffDate.ToString("d");
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
            return $"Viewing songs {dateFieldLabel} between {startDate:d} and {endDate:d}.";
        }

        return $"Viewing songs {filter.OperatorText} {filter.Days} days old.";
    }

    private void UpdateSongGridHintVisibility()
    {
        var songCount = _songGrid.Rows.Count;
        var scopeText = BuildSongGridViewScopeText();
        _songGridTitleLabel.Text = $"SongLens View · {songCount} song{(songCount == 1 ? "" : "s")}{(string.IsNullOrWhiteSpace(scopeText) ? "" : $" · {scopeText}")}";
        PositionDetailHeaderControls();
        UpdateSongDetailSplitLayout();
    }

    private string BuildSongGridViewScopeText()
    {
        if (_advancedSearchQuery is not null)
        {
            return "Advanced search";
        }

        if (_searchMode)
        {
            var query = _searchTextBox.Text.Trim();
            if (query.Length > 32)
            {
                query = $"{query[..29]}…";
            }
            return string.IsNullOrWhiteSpace(query) ? "Search results" : $"Search: “{query}”";
        }

        var filterText = BuildCompactSongAgeFilterText();
        if (_allSongsMode)
        {
            return string.IsNullOrWhiteSpace(filterText) ? "All songs" : filterText;
        }

        var folderName = _folderTree.SelectedNode?.Tag is string folderPath
            ? new DirectoryInfo(folderPath).Name
            : "";
        if (!string.IsNullOrWhiteSpace(folderName) && !string.IsNullOrWhiteSpace(filterText))
        {
            return $"Folder: {folderName} · {filterText}";
        }

        if (!string.IsNullOrWhiteSpace(folderName))
        {
            return $"Folder: {folderName}";
        }

        return filterText;
    }

    private string BuildCompactSongAgeFilterText()
    {
        if (_advancedSearchQuery is not null && _advancedSearchQuery.Rules.Count > 0)
            return $"Filtered by {_advancedSearchQuery.Rules.Count} rule{(_advancedSearchQuery.Rules.Count == 1 ? "" : "s")}";

        if (_songAgeFilter is null)
        {
            return "";
        }

        if (_songAgeFilter.Mode == SongAgeFilterMode.DateRange
            && _songAgeFilter.StartDate is DateTime startDate
            && _songAgeFilter.EndDate is DateTime endDate)
        {
            var dateFieldLabel = _songAgeFilter.DateField == SongDateField.Created ? "Created" : "Modified";
            var compactDatePattern = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern
                .Replace("yyyy", "yy", StringComparison.OrdinalIgnoreCase);
            return $"{dateFieldLabel}: {startDate.ToString(compactDatePattern)}–{endDate.ToString(compactDatePattern)}";
        }

        return $"Modified {_songAgeFilter.OperatorText} {_songAgeFilter.Days} days old";
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
        var labelHeight = AppFontSettings.Scale(28, _fontPreferences, AppFontSection.MainUi);
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
        SetFastSearchText("");

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

    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == Keys.F5)
        {
            RefreshCurrentView();
            return true;
        }

        return base.ProcessCmdKey(ref message, keyData);
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
