namespace SongMetainfoBrowser.App;

internal sealed class PreferencesForm : Form
{
    private const string CustomPlayerPathPlaceholder =
        "Browse to the Shortcut or Folder where the desired application is located.";
    private readonly AppTheme _theme;
    private readonly TextBox _rootPathTextBox = new();
    private readonly RadioButton _lightThemeRadioButton = new() { AutoSize = true, Text = "Light" };
    private readonly RadioButton _darkThemeRadioButton = new() { AutoSize = true, Text = "Dark" };
    private readonly TableLayoutPanel _rootPathRow = new();
    private readonly TableLayoutPanel _audioPlayerPathRow = new();
    private readonly TextBox _audioPlayerPathTextBox = new();
    private readonly TextBox _mixdownFolderNameTextBox = new();
    private readonly Label _defaultAudioPlayerNameLabel = new() { AutoSize = true };
    private readonly RadioButton _defaultAudioPlayerRadioButton = new() { AutoSize = true, Text = "Use Windows default player" };
    private readonly RadioButton _customAudioPlayerRadioButton = new() { AutoSize = true, Text = "Use custom player" };
    private readonly TabControl _tabs = new();
    private int _hoveredTabIndex = -1;
    private readonly CheckBox _stickyTabsCheckBox = new() { AutoSize = true, Text = "Use Sticky Tabs" };
    private readonly CheckBox _songLaunchCheckBox = new() { AutoSize = true, Text = "Enable option to open selected song" };
    private readonly CheckBox _restoreFilterSessionCheckBox = new() { AutoSize = true, Text = "Restore previous filter and view on startup" };
    private readonly CheckBox _restoreAdvancedSearchSessionCheckBox = new() { AutoSize = true, Text = "Restore last advanced search on startup" };
    private readonly CheckBox _historyRetentionPresetCheckBox = new() { AutoSize = true, Text = "Preselect history cleanup to keep the most recent" };
    private readonly NumericUpDown _historyRevisionsToKeepNumeric = new() { Minimum = 1, Maximum = 100, Width = 60 };
    private readonly Label _historyRetentionDescriptionLabel = new() { AutoSize = true };
    private readonly bool _showEnableSongLaunchOption;
    private readonly Action<IWin32Window> _showVisibleTabsDialog;
    public string SelectedRootPath { get; private set; } = "";
    public string SelectedThemeName { get; private set; } = AppThemes.Light.Name;
    public bool UseStickyTabs { get; private set; }
    public bool EnableSongLaunch { get; private set; }
    public bool RestoreFilterSessionOnStartup { get; private set; }
    public bool RestoreAdvancedSearchSessionOnStartup { get; private set; }
    public bool UseCustomAudioPlayer { get; private set; }
    public string? CustomAudioPlayerPath { get; private set; }
    public string MixdownFolderName { get; private set; }
    public bool UseHistoryRetentionPreset { get; private set; }
    public int HistoryRevisionsToKeep { get; private set; }
    public AppFontPreferences SelectedFontPreferences { get; private set; }

    public PreferencesForm(string currentRootPath, string currentThemeName, bool useStickyTabs, bool enableSongLaunch, bool showEnableSongLaunchOption, bool restoreFilterSessionOnStartup, bool restoreAdvancedSearchSessionOnStartup, string mixdownFolderName, bool useCustomAudioPlayer, string? customAudioPlayerPath, bool useHistoryRetentionPreset, int historyRevisionsToKeep, AppFontPreferences currentFontPreferences, AppTheme theme, Action<IWin32Window> showVisibleTabsDialog)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        _showEnableSongLaunchOption = showEnableSongLaunchOption;
        _showVisibleTabsDialog = showVisibleTabsDialog;
        SelectedRootPath = currentRootPath;
        SelectedThemeName = currentThemeName;
        UseStickyTabs = useStickyTabs;
        EnableSongLaunch = enableSongLaunch;
        RestoreFilterSessionOnStartup = restoreFilterSessionOnStartup;
        RestoreAdvancedSearchSessionOnStartup = restoreAdvancedSearchSessionOnStartup;
        MixdownFolderName = mixdownFolderName;
        UseCustomAudioPlayer = useCustomAudioPlayer;
        CustomAudioPlayerPath = customAudioPlayerPath;
        UseHistoryRetentionPreset = useHistoryRetentionPreset;
        HistoryRevisionsToKeep = Math.Clamp(historyRevisionsToKeep, 1, 100);
        SelectedFontPreferences = currentFontPreferences;
        var minimumClientSize = AppFontSettings.Scale(new Size(580, 460), fontPreferences, AppFontSection.Dialogs);

        Text = "Preferences";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        MinimumSize = SizeFromClientSize(minimumClientSize);
        ClientSize = BrowserConfigStore.LoadPreferencesWindowSize() is Size savedSize
            ? new Size(Math.Max(savedSize.Width, minimumClientSize.Width), Math.Max(savedSize.Height, minimumClientSize.Height))
            : minimumClientSize;
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        BuildLayout();
        LoadCurrentValues();
        UpdateResponsiveLayout();
        Resize += (_, _) => UpdateResponsiveLayout();
        FormClosing += (_, _) => BrowserConfigStore.SavePreferencesWindowSize(ClientSize);
    }

    private void BuildLayout()
    {
        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(rootLayout);

        _tabs.Dock = DockStyle.Fill;
        _tabs.Appearance = TabAppearance.Normal;
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.Padding = new Point(12, 4);
        _tabs.SizeMode = TabSizeMode.Normal;
        _tabs.ItemSize = AppFontSettings.Scale(new Size(110, 24), AppFontSettings.LoadPreferences(), AppFontSection.Dialogs);
        _tabs.DrawItem += DrawPreferenceTab;
        _tabs.MouseMove += TabsMouseMove;
        _tabs.MouseLeave += TabsMouseLeave;
        var generalTab = new TabPage("General") { BackColor = _theme.AppBackColor, ForeColor = _theme.TextColor, AutoScroll = true };
        var audioTab = new TabPage("Audio") { BackColor = _theme.AppBackColor, ForeColor = _theme.TextColor, AutoScroll = true };
        var historyTab = new TabPage("History") { BackColor = _theme.AppBackColor, ForeColor = _theme.TextColor, AutoScroll = true };
        var appearanceTab = new TabPage("Appearance") { BackColor = _theme.AppBackColor, ForeColor = _theme.TextColor, AutoScroll = true };
        _tabs.TabPages.Add(generalTab);
        _tabs.TabPages.Add(audioTab);
        _tabs.TabPages.Add(historyTab);
        _tabs.TabPages.Add(appearanceTab);
        _tabs.SelectedIndex = 0;
        rootLayout.Controls.Add(_tabs, 0, 0);

        BuildGeneralTab(generalTab);
        BuildAudioTab(audioTab);
        BuildHistoryTab(historyTab);
        BuildAppearanceTab(appearanceTab);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 10, 0, 0)
        };

        var okButton = new Button { Text = "OK", AutoSize = true };
        okButton.Click += (_, _) => ConfirmSelection();
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        StyleButton(okButton, useAccent: true);
        StyleButton(cancelButton, useAccent: true);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);
        rootLayout.Controls.Add(buttonPanel, 0, 1);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void BuildGeneralTab(TabPage tabPage)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            Padding = new Padding(10),
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tabPage.Controls.Add(layout);

        var rootLabel = new Label
        {
            AutoSize = true,
            Text = "Songs root folder",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 6)
        };
        layout.Controls.Add(rootLabel);

        _rootPathRow.AutoSize = false;
        _rootPathRow.ColumnCount = 2;
        _rootPathRow.RowCount = 1;
        _rootPathRow.Margin = Padding.Empty;
        _rootPathRow.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        _rootPathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _rootPathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        _rootPathRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootPathTextBox.Dock = DockStyle.Fill;
        _rootPathTextBox.ReadOnly = true;
        _rootPathTextBox.BorderStyle = BorderStyle.FixedSingle;
        _rootPathTextBox.BackColor = _theme.PanelBackColor;
        _rootPathTextBox.ForeColor = _theme.TextColor;
        var browseButton = new Button
        {
            Text = "...",
            Size = new Size(28, 28),
            MinimumSize = new Size(28, 28),
            MaximumSize = new Size(28, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        browseButton.Click += (_, _) => BrowseForRootPath();
        StyleButton(browseButton, useAccent: false);
        _rootPathRow.Controls.Add(_rootPathTextBox, 0, 0);
        _rootPathRow.Controls.Add(browseButton, 1, 0);
        layout.Controls.Add(_rootPathRow);

        if (_showEnableSongLaunchOption)
        {
            AddPreferenceOption(
                layout,
                _songLaunchCheckBox,
                "Use right-click menu for launch options.");
        }

        AddPreferenceOption(
            layout,
            _stickyTabsCheckBox,
            "Keep the current detail tab selected when moving between songs.");

        var startupHeading = new Label
        {
            AutoSize = true,
            Text = "Startup Options",
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 18, 0, 4)
        };
        layout.Controls.Add(startupHeading);

        AddPreferenceOption(
            layout,
            _restoreFilterSessionCheckBox,
            "Reopen the filter and library view that were active when SongLens closed.",
            topMargin: 6);
        AddPreferenceOption(
            layout,
            _restoreAdvancedSearchSessionCheckBox,
            "Restore the most recent advanced search when SongLens starts.");
    }

    private void AddPreferenceOption(TableLayoutPanel layout, CheckBox checkBox, string description, int topMargin = 12)
    {
        checkBox.BackColor = _theme.AppBackColor;
        checkBox.ForeColor = _theme.TextColor;
        checkBox.Margin = new Padding(0, topMargin, 0, 2);
        checkBox.Padding = Padding.Empty;
        layout.Controls.Add(checkBox);

        var descriptionLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            Text = description,
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(20, 0, 0, 2)
        };
        layout.Controls.Add(descriptionLabel);
    }

    private void BuildAudioTab(TabPage tabPage)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            Padding = new Padding(10),
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tabPage.Controls.Add(layout);

        var folderHeading = new Label
        {
            AutoSize = true,
            Text = "Mixdown Folder",
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.Controls.Add(folderHeading);

        var folderLabel = new Label
        {
            AutoSize = true,
            Text = "Folder name",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 4)
        };
        layout.Controls.Add(folderLabel);

        _mixdownFolderNameTextBox.BorderStyle = BorderStyle.FixedSingle;
        _mixdownFolderNameTextBox.BackColor = _theme.PanelBackColor;
        _mixdownFolderNameTextBox.ForeColor = _theme.TextColor;
        _mixdownFolderNameTextBox.Width = 260;
        _mixdownFolderNameTextBox.Margin = new Padding(0, 0, 0, 4);
        layout.Controls.Add(_mixdownFolderNameTextBox);

        var folderNote = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            Text = "SongLens looks for this folder directly inside each song folder. The default is Mixdown.",
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 0, 0, 18)
        };
        layout.Controls.Add(folderNote);

        var heading = new Label
        {
            AutoSize = true,
            Text = "Mixdown Audio Player",
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.Controls.Add(heading);

        _defaultAudioPlayerRadioButton.BackColor = _theme.AppBackColor;
        _defaultAudioPlayerRadioButton.ForeColor = _theme.TextColor;
        _defaultAudioPlayerRadioButton.Margin = new Padding(0, 4, 0, 4);
        _defaultAudioPlayerNameLabel.ForeColor = _theme.MutedTextColor;
        _defaultAudioPlayerNameLabel.Margin = new Padding(10, 5, 0, 4);
        _defaultAudioPlayerNameLabel.Text = BuildDefaultAudioPlayerDescription();
        var audioPlayerOptionsLayout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 3,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        audioPlayerOptionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        audioPlayerOptionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        audioPlayerOptionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        audioPlayerOptionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        audioPlayerOptionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        audioPlayerOptionsLayout.Controls.Add(_defaultAudioPlayerRadioButton, 0, 0);
        audioPlayerOptionsLayout.Controls.Add(_defaultAudioPlayerNameLabel, 1, 0);
        _customAudioPlayerRadioButton.BackColor = _theme.AppBackColor;
        _customAudioPlayerRadioButton.ForeColor = _theme.TextColor;
        _customAudioPlayerRadioButton.Margin = new Padding(0, 8, 0, 4);
        audioPlayerOptionsLayout.Controls.Add(_customAudioPlayerRadioButton, 0, 1);
        audioPlayerOptionsLayout.SetColumnSpan(_customAudioPlayerRadioButton, 2);

        _audioPlayerPathRow.AutoSize = false;
        _audioPlayerPathRow.ColumnCount = 2;
        _audioPlayerPathRow.RowCount = 1;
        _audioPlayerPathRow.Margin = new Padding(20, 0, 0, 0);
        _audioPlayerPathRow.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        _audioPlayerPathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _audioPlayerPathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        _audioPlayerPathRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _audioPlayerPathTextBox.Dock = DockStyle.Fill;
        _audioPlayerPathTextBox.ReadOnly = true;
        _audioPlayerPathTextBox.BorderStyle = BorderStyle.FixedSingle;
        _audioPlayerPathTextBox.BackColor = _theme.PanelBackColor;
        _audioPlayerPathTextBox.ForeColor = _theme.TextColor;
        var browseButton = new Button
        {
            Text = "Browse...",
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        browseButton.Click += (_, _) => BrowseForAudioPlayer();
        StyleButton(browseButton, useAccent: true);
        _audioPlayerPathRow.Controls.Add(_audioPlayerPathTextBox, 0, 0);
        _audioPlayerPathRow.Controls.Add(browseButton, 1, 0);
        audioPlayerOptionsLayout.Controls.Add(_audioPlayerPathRow, 0, 2);
        audioPlayerOptionsLayout.SetColumnSpan(_audioPlayerPathRow, 2);
        layout.Controls.Add(audioPlayerOptionsLayout);

        var note = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            Text = "SongLens uses this player for matching audio files found in the configured mixdown folder.",
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 14, 0, 0)
        };
        layout.Controls.Add(note);
    }

    private static string BuildDefaultAudioPlayerDescription()
    {
        var wavPlayerName = WindowsShell.GetDefaultApplicationFriendlyName(".wav");
        var mp3PlayerName = WindowsShell.GetDefaultApplicationFriendlyName(".mp3");
        if (string.Equals(wavPlayerName, mp3PlayerName, StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(wavPlayerName) ? "" : $"({wavPlayerName})";
        }

        if (!string.IsNullOrWhiteSpace(wavPlayerName) && !string.IsNullOrWhiteSpace(mp3PlayerName))
        {
            return $"(WAV: {wavPlayerName}; MP3: {mp3PlayerName})";
        }

        if (!string.IsNullOrWhiteSpace(wavPlayerName))
        {
            return $"(WAV: {wavPlayerName})";
        }

        return string.IsNullOrWhiteSpace(mp3PlayerName) ? "" : $"(MP3: {mp3PlayerName})";
    }

    private void BuildHistoryTab(TabPage tabPage)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            Padding = new Padding(10),
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tabPage.Controls.Add(layout);

        var heading = new Label
        {
            AutoSize = true,
            Text = "History Cleanup",
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.Controls.Add(heading);

        _historyRetentionPresetCheckBox.BackColor = _theme.AppBackColor;
        _historyRetentionPresetCheckBox.ForeColor = _theme.TextColor;
        _historyRetentionPresetCheckBox.Margin = new Padding(0, 4, 0, 4);
        _historyRetentionPresetCheckBox.CheckedChanged += (_, _) =>
            _historyRevisionsToKeepNumeric.Enabled = _historyRetentionPresetCheckBox.Checked;

        var retentionRow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        _historyRevisionsToKeepNumeric.Margin = new Padding(8, 0, 6, 0);
        _historyRevisionsToKeepNumeric.BackColor = _theme.PanelBackColor;
        _historyRevisionsToKeepNumeric.ForeColor = _theme.TextColor;
        _historyRevisionsToKeepNumeric.ValueChanged += (_, _) => UpdateHistoryRetentionDescription();
        var revisionsLabel = new Label
        {
            AutoSize = true,
            Text = "revision(s)",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 4, 0, 0)
        };
        retentionRow.Controls.Add(_historyRetentionPresetCheckBox);
        retentionRow.Controls.Add(_historyRevisionsToKeepNumeric);
        retentionRow.Controls.Add(revisionsLabel);
        layout.Controls.Add(retentionRow);

        _historyRetentionDescriptionLabel.MaximumSize = new Size(500, 0);
        _historyRetentionDescriptionLabel.ForeColor = _theme.MutedTextColor;
        _historyRetentionDescriptionLabel.Margin = new Padding(0, 8, 0, 0);
        UpdateHistoryRetentionDescription();
        layout.Controls.Add(_historyRetentionDescriptionLabel);
    }

    private void UpdateHistoryRetentionDescription()
    {
        var retentionCount = (int)_historyRevisionsToKeepNumeric.Value;
        var revisionWord = retentionCount == 1 ? "revision" : "revisions";
        _historyRetentionDescriptionLabel.Text =
            $"When History opens, SongLens marks older revisions for deletion and leaves the {retentionCount} most recent {revisionWord} unchecked. You can still manually mark any files to be kept or deleted.";
    }

    private void BuildAppearanceTab(TabPage tabPage)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(10),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabPage.Controls.Add(layout);

        var themeLabel = new Label
        {
            AutoSize = true,
            Text = "Theme",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 6)
        };
        layout.Controls.Add(themeLabel, 0, 0);

        var themeOptionsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        _lightThemeRadioButton.BackColor = _theme.AppBackColor;
        _lightThemeRadioButton.ForeColor = _theme.TextColor;
        _lightThemeRadioButton.Margin = new Padding(0, 0, 18, 0);
        _darkThemeRadioButton.BackColor = _theme.AppBackColor;
        _darkThemeRadioButton.ForeColor = _theme.TextColor;
        _darkThemeRadioButton.Margin = Padding.Empty;
        themeOptionsPanel.Controls.Add(_lightThemeRadioButton);
        themeOptionsPanel.Controls.Add(_darkThemeRadioButton);
        layout.Controls.Add(themeOptionsPanel, 0, 1);

        var descriptionLabel = new Label
        {
            AutoSize = true,
            Text = "Font sizes are saved automatically and apply across SongLens.",
            MaximumSize = new Size(500, 0),
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 14, 0, 0)
        };
        layout.Controls.Add(descriptionLabel, 0, 2);

        var fontButton = new Button
        {
            Text = "Change Font Sizes...",
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 0)
        };
        fontButton.Click += (_, _) => ShowFontSettingsDialog();
        StyleButton(fontButton, useAccent: false);
        layout.Controls.Add(fontButton, 0, 3);

        var fontNoteLabel = new Label
        {
            AutoSize = true,
            Text = "Adjust the main UI, folder tree, SongLens View, detail grids, notes text, and dialog fonts.",
            MaximumSize = new Size(500, 0),
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 8, 0, 0)
        };
        layout.Controls.Add(fontNoteLabel, 0, 4);

        var visibleTabsButton = new Button
        {
            Text = "Select Visible Tabs",
            AutoSize = true,
            Margin = new Padding(0, 16, 0, 0)
        };
        visibleTabsButton.Click += (_, _) => _showVisibleTabsDialog(this);
        StyleButton(visibleTabsButton, useAccent: false);
        layout.Controls.Add(visibleTabsButton, 0, 5);

        var visibleTabsNoteLabel = new Label
        {
            AutoSize = true,
            Text = "Select the tabs in the \"Song Details\" grid that will be visible.",
            MaximumSize = new Size(500, 0),
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 8, 0, 0)
        };
        layout.Controls.Add(visibleTabsNoteLabel, 0, 6);
    }

    private void LoadCurrentValues()
    {
        _tabs.SelectedIndex = _tabs.TabPages.Count > 0 ? 0 : -1;
        _rootPathTextBox.Text = SelectedRootPath;
        var useDarkTheme = string.Equals(SelectedThemeName, AppThemes.Dark.Name, StringComparison.OrdinalIgnoreCase);
        _darkThemeRadioButton.Checked = useDarkTheme;
        _lightThemeRadioButton.Checked = !useDarkTheme;
        _stickyTabsCheckBox.Checked = UseStickyTabs;
        _songLaunchCheckBox.Checked = EnableSongLaunch;
        _restoreFilterSessionCheckBox.Checked = RestoreFilterSessionOnStartup;
        _restoreAdvancedSearchSessionCheckBox.Checked = RestoreAdvancedSearchSessionOnStartup;
        _mixdownFolderNameTextBox.Text = MixdownFolderName;
        _defaultAudioPlayerRadioButton.Checked = !UseCustomAudioPlayer;
        _customAudioPlayerRadioButton.Checked = UseCustomAudioPlayer;
        SetCustomAudioPlayerPath(CustomAudioPlayerPath);
        _historyRetentionPresetCheckBox.Checked = UseHistoryRetentionPreset;
        _historyRevisionsToKeepNumeric.Value = HistoryRevisionsToKeep;
        _historyRevisionsToKeepNumeric.Enabled = UseHistoryRetentionPreset;
    }

    private void UpdateResponsiveLayout()
    {
        var activeTab = _tabs.TabPages.Count > 0 ? _tabs.TabPages[0] : null;
        if (activeTab is null)
        {
            return;
        }

        var targetWidth = Math.Max(
            AppFontSettings.Scale(500, AppFontSettings.LoadPreferences(), AppFontSection.Dialogs),
            activeTab.ClientSize.Width - 20);
        _rootPathRow.Size = new Size(targetWidth, AppFontSettings.Scale(28, AppFontSettings.LoadPreferences(), AppFontSection.Dialogs));
        _rootPathTextBox.MinimumSize = new Size(Math.Max(0, targetWidth - 32), 0);
        var audioPlayerRowHeight = _audioPlayerPathRow.Controls
            .Cast<Control>()
            .Select(control => control.GetPreferredSize(Size.Empty).Height + control.Margin.Vertical)
            .DefaultIfEmpty(AppFontSettings.Scale(28, AppFontSettings.LoadPreferences(), AppFontSection.Dialogs))
            .Max();
        _audioPlayerPathRow.Size = new Size(targetWidth, audioPlayerRowHeight);
        _audioPlayerPathTextBox.MinimumSize = new Size(Math.Max(0, targetWidth - 80), 0);
    }

    private void BrowseForRootPath()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select your Studio One songs folder",
            SelectedPath = Directory.Exists(_rootPathTextBox.Text) ? _rootPathTextBox.Text : ""
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _rootPathTextBox.Text = dialog.SelectedPath;
        }
    }

    private void BrowseForAudioPlayer()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choose an audio player",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false
        };

        var currentAudioPlayerPath = _audioPlayerPathTextBox.Tag as string;
        if (!string.IsNullOrWhiteSpace(currentAudioPlayerPath)
            && File.Exists(currentAudioPlayerPath))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(currentAudioPlayerPath);
            dialog.FileName = Path.GetFileName(currentAudioPlayerPath);
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            SetCustomAudioPlayerPath(dialog.FileName);
            _customAudioPlayerRadioButton.Checked = true;
        }
    }

    private void SetCustomAudioPlayerPath(string? path)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        _audioPlayerPathTextBox.Tag = normalizedPath;
        _audioPlayerPathTextBox.Text = normalizedPath ?? CustomPlayerPathPlaceholder;
        _audioPlayerPathTextBox.ForeColor = normalizedPath is null
            ? _theme.MutedTextColor
            : _theme.TextColor;
    }

    private void ConfirmSelection()
    {
        if (!string.IsNullOrWhiteSpace(_rootPathTextBox.Text) && !Directory.Exists(_rootPathTextBox.Text))
        {
            using var messageDialog = new ThemedMessageForm(Text, "Choose an existing songs folder.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        var customAudioPlayerPath = _audioPlayerPathTextBox.Tag as string;
        if (_customAudioPlayerRadioButton.Checked
            && (string.IsNullOrWhiteSpace(customAudioPlayerPath) || !File.Exists(customAudioPlayerPath)))
        {
            using var messageDialog = new ThemedMessageForm(Text, "Choose an existing audio-player executable, or use the Windows default player.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        var mixdownFolderName = _mixdownFolderNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(mixdownFolderName))
        {
            using var messageDialog = new ThemedMessageForm(Text, "Enter a mixdown folder name.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        if (mixdownFolderName is "." or ".."
            || mixdownFolderName.EndsWith(".", StringComparison.Ordinal)
            || mixdownFolderName[^1] == ' '
            || mixdownFolderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            using var messageDialog = new ThemedMessageForm(Text, "Enter a valid Windows folder name, without a path.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        SelectedRootPath = _rootPathTextBox.Text.Trim();
        SelectedThemeName = _darkThemeRadioButton.Checked ? AppThemes.Dark.Name : AppThemes.Light.Name;
        UseStickyTabs = _stickyTabsCheckBox.Checked;
        EnableSongLaunch = _songLaunchCheckBox.Checked;
        RestoreFilterSessionOnStartup = _restoreFilterSessionCheckBox.Checked;
        RestoreAdvancedSearchSessionOnStartup = _restoreAdvancedSearchSessionCheckBox.Checked;
        MixdownFolderName = mixdownFolderName;
        UseCustomAudioPlayer = _customAudioPlayerRadioButton.Checked;
        CustomAudioPlayerPath = customAudioPlayerPath;
        UseHistoryRetentionPreset = _historyRetentionPresetCheckBox.Checked;
        HistoryRevisionsToKeep = (int)_historyRevisionsToKeepNumeric.Value;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowFontSettingsDialog()
    {
        using var dialog = new FontSettingsForm(SelectedFontPreferences, _theme);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        SelectedFontPreferences = dialog.SelectedPreferences;
    }

    private void DrawPreferenceTab(object? sender, DrawItemEventArgs args)
    {
        if (args.Index < 0 || args.Index >= _tabs.TabPages.Count)
        {
            return;
        }

        var bounds = args.Bounds;
        var isSelected = (args.State & DrawItemState.Selected) == DrawItemState.Selected;
        var isHovered = args.Index == _hoveredTabIndex;
        var backColor = isSelected || isHovered
            ? _theme.AccentHoverColor
            : _theme.AccentSoftColor;
        using var backBrush = new SolidBrush(backColor);
        using var textBrush = new SolidBrush(_theme.TextColor);
        using var borderPen = new Pen(_theme.BorderColor);

        args.Graphics.FillRectangle(backBrush, bounds);
        args.Graphics.DrawRectangle(borderPen, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
        TextRenderer.DrawText(
            args.Graphics,
            _tabs.TabPages[args.Index].Text,
            Font,
            bounds,
            textBrush.Color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private void TabsMouseMove(object? sender, MouseEventArgs args)
    {
        var hoveredTabIndex = -1;
        for (var index = 0; index < _tabs.TabPages.Count; index++)
        {
            if (_tabs.GetTabRect(index).Contains(args.Location))
            {
                hoveredTabIndex = index;
                break;
            }
        }

        if (_hoveredTabIndex == hoveredTabIndex)
        {
            return;
        }

        _hoveredTabIndex = hoveredTabIndex;
        _tabs.Invalidate();
    }

    private void TabsMouseLeave(object? sender, EventArgs args)
    {
        if (_hoveredTabIndex < 0)
        {
            return;
        }

        _hoveredTabIndex = -1;
        _tabs.Invalidate();
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
