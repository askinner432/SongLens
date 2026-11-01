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
    private readonly TextBox _customAudioPlayerArgumentsTextBox = new();
    private readonly TextBox _mixdownFolderNameTextBox = new();
    private readonly Label _defaultAudioPlayerNameLabel = new() { AutoSize = true };
    private readonly RadioButton _defaultAudioPlayerRadioButton = new() { AutoSize = true, Text = "Use Windows default player" };
    private readonly RadioButton _customAudioPlayerRadioButton = new() { AutoSize = true, Text = "Use custom player" };
    private readonly TabControl _tabs = new();
    private int _hoveredTabIndex = -1;
    private readonly CheckBox _stickyTabsCheckBox = new() { AutoSize = true, Text = "Use Sticky Tabs" };
    private readonly CheckBox _songLaunchCheckBox = new() { AutoSize = true, Text = "Enable option to open selected song" };
    private readonly ComboBox _songGridDoubleClickComboBox = new();
    private readonly bool _enableSongGridDoubleClickPreference;
    private readonly CheckBox _restoreFilterSessionCheckBox = new() { AutoSize = true, Text = "Restore previous SongLens View on startup" };
    private readonly CheckBox _historyRetentionPresetCheckBox = new() { AutoSize = true, Text = "Preselect history cleanup to keep the most recent" };
    private readonly NumericUpDown _historyRevisionsToKeepNumeric = new() { Minimum = 1, Maximum = 100, Width = 60 };
    private readonly Label _historyRetentionDescriptionLabel = new() { AutoSize = true };
    private readonly ListBox _songStatusesListBox = new();
    private readonly TextBox _songStatusTextBox = new();
    private readonly Button _renameSongStatusButton = new() { Text = "Rename Selected...", AutoSize = true };
    private readonly Button _removeSongStatusButton = new() { Text = "Remove Selected", AutoSize = true };
    private readonly Button _moveSongStatusUpButton = new() { Text = "Move Up", AutoSize = true };
    private readonly Button _moveSongStatusDownButton = new() { Text = "Move Down", AutoSize = true };
    private readonly Dictionary<string, string> _songStatusRenames = new(StringComparer.OrdinalIgnoreCase);
    private readonly ListBox _songGenresListBox = new();
    private readonly TextBox _songGenreTextBox = new();
    private readonly Button _renameSongGenreButton = new() { Text = "Rename Selected...", AutoSize = true };
    private readonly Button _removeSongGenreButton = new() { Text = "Remove Selected", AutoSize = true };
    private readonly Button _moveSongGenreUpButton = new() { Text = "Move Up", AutoSize = true };
    private readonly Button _moveSongGenreDownButton = new() { Text = "Move Down", AutoSize = true };
    private readonly Dictionary<string, string> _songGenreRenames = new(StringComparer.OrdinalIgnoreCase);
    private readonly ListBox _customColumnsListBox = new();
    private readonly TextBox _customColumnNameTextBox = new();
    private readonly bool _showEnableSongLaunchOption;
    private readonly Action<IWin32Window> _showVisibleTabsDialog;
    public string SelectedRootPath { get; private set; } = "";
    public string SelectedThemeName { get; private set; } = AppThemes.Light.Name;
    public bool UseStickyTabs { get; private set; }
    public bool EnableSongLaunch { get; private set; }
    public SongGridDoubleClickAction SelectedSongGridDoubleClickAction { get; private set; }
    public bool RestoreFilterSessionOnStartup { get; private set; }
    public bool UseCustomAudioPlayer { get; private set; }
    public string? CustomAudioPlayerPath { get; private set; }
    public string? CustomAudioPlayerArguments { get; private set; }
    public string MixdownFolderName { get; private set; }
    public bool UseHistoryRetentionPreset { get; private set; }
    public int HistoryRevisionsToKeep { get; private set; }
    public IReadOnlyList<string> SelectedSongStatuses { get; private set; }
    public IReadOnlyDictionary<string, string> SongStatusRenames => _songStatusRenames;
    public IReadOnlyList<string> SelectedSongGenres { get; private set; }
    public IReadOnlyList<CustomSongColumn> SelectedCustomSongColumns { get; private set; }
    public IReadOnlyDictionary<string, string> SongGenreRenames => _songGenreRenames;
    public AppFontPreferences SelectedFontPreferences { get; private set; }

    public PreferencesForm(string currentRootPath, string currentThemeName, bool useStickyTabs, bool enableSongLaunch, bool showEnableSongLaunchOption, SongGridDoubleClickAction songGridDoubleClickAction, bool enableSongGridDoubleClickPreference, bool restoreFilterSessionOnStartup, string mixdownFolderName, bool useCustomAudioPlayer, string? customAudioPlayerPath, string? customAudioPlayerArguments, bool useHistoryRetentionPreset, int historyRevisionsToKeep, IReadOnlyList<string> songStatuses, IReadOnlyList<string> songGenres, IReadOnlyList<CustomSongColumn> customSongColumns, AppFontPreferences currentFontPreferences, AppTheme theme, Action<IWin32Window> showVisibleTabsDialog, bool openCustomColumnsTab = false)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        _showEnableSongLaunchOption = showEnableSongLaunchOption;
        _showVisibleTabsDialog = showVisibleTabsDialog;
        SelectedRootPath = currentRootPath;
        SelectedThemeName = currentThemeName;
        UseStickyTabs = useStickyTabs;
        EnableSongLaunch = enableSongLaunch;
        SelectedSongGridDoubleClickAction = songGridDoubleClickAction;
        _enableSongGridDoubleClickPreference = enableSongGridDoubleClickPreference;
        RestoreFilterSessionOnStartup = restoreFilterSessionOnStartup;
        MixdownFolderName = mixdownFolderName;
        UseCustomAudioPlayer = useCustomAudioPlayer;
        CustomAudioPlayerPath = customAudioPlayerPath;
        CustomAudioPlayerArguments = customAudioPlayerArguments;
        UseHistoryRetentionPreset = useHistoryRetentionPreset;
        HistoryRevisionsToKeep = Math.Clamp(historyRevisionsToKeep, 1, 100);
        SelectedSongStatuses = songStatuses.ToArray();
        SelectedSongGenres = songGenres.ToArray();
        SelectedCustomSongColumns = customSongColumns.Select(column => new CustomSongColumn { Id = column.Id, Name = column.Name }).ToArray();
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
        if (openCustomColumnsTab)
            _tabs.SelectedTab = _tabs.TabPages.Cast<TabPage>().First(tab => tab.Text == "Custom Columns");
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
        var statusesTab = new TabPage("Statuses") { BackColor = _theme.AppBackColor, ForeColor = _theme.TextColor, AutoScroll = true };
        var genresTab = new TabPage("Genres") { BackColor = _theme.AppBackColor, ForeColor = _theme.TextColor, AutoScroll = true };
        var customColumnsTab = new TabPage("Custom Columns") { BackColor = _theme.AppBackColor, ForeColor = _theme.TextColor, AutoScroll = true };
        var appearanceTab = new TabPage("Appearance") { BackColor = _theme.AppBackColor, ForeColor = _theme.TextColor, AutoScroll = true };
        _tabs.TabPages.Add(generalTab);
        _tabs.TabPages.Add(audioTab);
        _tabs.TabPages.Add(historyTab);
        _tabs.TabPages.Add(statusesTab);
        _tabs.TabPages.Add(genresTab);
        _tabs.TabPages.Add(customColumnsTab);
        _tabs.TabPages.Add(appearanceTab);
        _tabs.SelectedIndex = 0;
        rootLayout.Controls.Add(_tabs, 0, 0);

        BuildGeneralTab(generalTab);
        BuildAudioTab(audioTab);
        BuildHistoryTab(historyTab);
        BuildStatusesTab(statusesTab);
        BuildGenresTab(genresTab);
        BuildCustomColumnsTab(customColumnsTab);
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
        StyleButton(cancelButton, useAccent: false);
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

        var doubleClickLabel = new Label
        {
            AutoSize = true,
            Text = "Song grid double-click action",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 14, 0, 4)
        };
        layout.Controls.Add(doubleClickLabel);

        _songGridDoubleClickComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _songGridDoubleClickComboBox.Width = 220;
        _songGridDoubleClickComboBox.Items.AddRange(["Open Song in DAW", "Reveal in Explorer"]);
        _songGridDoubleClickComboBox.Enabled = _enableSongGridDoubleClickPreference;
        _songGridDoubleClickComboBox.BackColor = _theme.PanelBackColor;
        _songGridDoubleClickComboBox.ForeColor = _theme.TextColor;
        _songGridDoubleClickComboBox.Margin = new Padding(0, 0, 0, 2);
        layout.Controls.Add(_songGridDoubleClickComboBox);

        var doubleClickDescription = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            Text = _enableSongGridDoubleClickPreference
                ? "Choose what happens when you double-click a song row."
                : "This option is available when exactly one supported DAW is installed.",
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 0, 0, 2)
        };
        layout.Controls.Add(doubleClickDescription);

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
            "Reopen the all-songs, Quick Filter, or Advanced Search view that was active when SongLens closed.",
            topMargin: 6);
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
            RowCount = 5,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        audioPlayerOptionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        audioPlayerOptionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        audioPlayerOptionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        audioPlayerOptionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
        _audioPlayerPathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
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
        StyleButton(browseButton, useAccent: false);
        _audioPlayerPathRow.Controls.Add(_audioPlayerPathTextBox, 0, 0);
        _audioPlayerPathRow.Controls.Add(browseButton, 1, 0);
        audioPlayerOptionsLayout.Controls.Add(_audioPlayerPathRow, 0, 2);
        audioPlayerOptionsLayout.SetColumnSpan(_audioPlayerPathRow, 2);

        var argumentsLabel = new Label
        {
            AutoSize = true,
            Text = "Command-line arguments (optional)",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 14, 0, 4)
        };
        audioPlayerOptionsLayout.Controls.Add(argumentsLabel, 0, 3);
        audioPlayerOptionsLayout.SetColumnSpan(argumentsLabel, 2);

        _customAudioPlayerArgumentsTextBox.Dock = DockStyle.Fill;
        _customAudioPlayerArgumentsTextBox.BorderStyle = BorderStyle.FixedSingle;
        _customAudioPlayerArgumentsTextBox.BackColor = _theme.PanelBackColor;
        _customAudioPlayerArgumentsTextBox.ForeColor = _theme.TextColor;
        _customAudioPlayerArgumentsTextBox.Margin = Padding.Empty;
        audioPlayerOptionsLayout.Controls.Add(_customAudioPlayerArgumentsTextBox, 0, 4);
        audioPlayerOptionsLayout.SetColumnSpan(_customAudioPlayerArgumentsTextBox, 2);
        layout.Controls.Add(audioPlayerOptionsLayout);

        var note = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            Text = "Arguments apply to the selected player. "
                + "Use {file} where the audio path belongs; if omitted, SongLens appends the file. "
                + "VLC examples: --one-instance {file} or --no-one-instance {file}. "
                + "Some Windows Store players may require selecting their executable as a custom player.",
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 14, 0, 0)
        };
        layout.Controls.Add(note);

        _customAudioPlayerRadioButton.CheckedChanged += (_, _) => UpdateCustomAudioPlayerControls();
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

    private void BuildStatusesTab(TabPage tabPage)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(10),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabPage.Controls.Add(layout);

        var heading = new Label
        {
            AutoSize = true,
            Text = "Song Status Options",
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 6)
        };
        layout.Controls.Add(heading, 0, 0);

        var explanation = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Text = "These entries appear in the Status dropdown in SongLens View. Removing an option does not erase that status from songs already using it.",
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 0, 0, 10)
        };
        layout.Controls.Add(explanation, 0, 1);

        _songStatusesListBox.Dock = DockStyle.Fill;
        _songStatusesListBox.IntegralHeight = false;
        _songStatusesListBox.BackColor = _theme.PanelBackColor;
        _songStatusesListBox.ForeColor = _theme.TextColor;
        _songStatusesListBox.BorderStyle = BorderStyle.FixedSingle;
        _songStatusesListBox.SelectedIndexChanged += (_, _) => UpdateSongStatusActionButtons();
        _songStatusesListBox.DoubleClick += (_, _) => RenameSongStatus();
        layout.Controls.Add(_songStatusesListBox, 0, 2);

        var editorRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 8, 0, 0)
        };
        editorRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editorRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var addStatusLabel = new Label
        {
            AutoSize = true,
            Text = "Add a new status",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 4)
        };
        editorRow.Controls.Add(addStatusLabel, 0, 0);
        editorRow.SetColumnSpan(addStatusLabel, 2);
        _songStatusTextBox.Dock = DockStyle.Fill;
        _songStatusTextBox.BorderStyle = BorderStyle.FixedSingle;
        _songStatusTextBox.BackColor = _theme.PanelBackColor;
        _songStatusTextBox.ForeColor = _theme.TextColor;
        WindowsShell.SetCueBanner(_songStatusTextBox, "Enter a new status");
        _songStatusTextBox.KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.Enter)
            {
                AddSongStatus();
                args.SuppressKeyPress = true;
            }
        };
        var addButton = new Button { Text = "Add", AutoSize = true };
        addButton.Click += (_, _) => AddSongStatus();
        StyleButton(addButton, useAccent: false);
        editorRow.Controls.Add(_songStatusTextBox, 0, 1);
        editorRow.Controls.Add(addButton, 1, 1);
        layout.Controls.Add(editorRow, 0, 3);

        var actionRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 8, 0, 0)
        };
        var restoreButton = new Button { Text = "Restore Defaults", AutoSize = true };
        _renameSongStatusButton.Click += (_, _) => RenameSongStatus();
        _removeSongStatusButton.Click += (_, _) => RemoveSongStatus();
        _moveSongStatusUpButton.Click += (_, _) => MoveSongStatus(-1);
        _moveSongStatusDownButton.Click += (_, _) => MoveSongStatus(1);
        restoreButton.Click += (_, _) => RestoreDefaultSongStatuses();
        foreach (var button in new[]
                 {
                     _renameSongStatusButton,
                     _removeSongStatusButton,
                     _moveSongStatusUpButton,
                     _moveSongStatusDownButton,
                     restoreButton
                 })
        {
            StyleButton(button, useAccent: false);
            actionRow.Controls.Add(button);
        }
        layout.Controls.Add(actionRow, 0, 4);
        UpdateSongStatusActionButtons();
    }

    private void AddSongStatus()
    {
        var status = _songStatusTextBox.Text.Trim();
        if (!ValidateSongStatus(status))
        {
            return;
        }

        _songStatusesListBox.Items.Add(status);
        _songStatusesListBox.SelectedIndex = _songStatusesListBox.Items.Count - 1;
        _songStatusTextBox.Clear();
        _songStatusTextBox.Focus();
    }

    private void RenameSongStatus()
    {
        var selectedIndex = _songStatusesListBox.SelectedIndex;
        if (selectedIndex < 0)
        {
            ShowStatusValidationMessage("Select a status to rename.");
            return;
        }

        var oldStatus = _songStatusesListBox.Items[selectedIndex]?.ToString() ?? "";
        using var prompt = new ThemedTextPromptForm(
            "Rename Status",
            "Enter a new name for the selected status:",
            oldStatus,
            _theme,
            okText: "Rename",
            emptyValueMessage: "Enter a status name.");
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var status = prompt.EnteredText;
        if (!ValidateSongStatus(status, selectedIndex))
        {
            return;
        }

        _songStatusesListBox.Items[selectedIndex] = status;
        _songStatusesListBox.SelectedIndex = selectedIndex;

        var originalNames = _songStatusRenames
            .Where(pair => string.Equals(pair.Value, oldStatus, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Key)
            .ToArray();
        if (originalNames.Length == 0)
        {
            _songStatusRenames[oldStatus] = status;
        }
        else
        {
            foreach (var originalName in originalNames)
            {
                _songStatusRenames[originalName] = status;
            }
        }
    }

    private bool ValidateSongStatus(string status, int ignoredIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            ShowStatusValidationMessage("Enter a status name.");
            return false;
        }

        var duplicate = _songStatusesListBox.Items
            .Cast<string>()
            .Where((_, index) => index != ignoredIndex)
            .Any(existing => string.Equals(existing, status, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            ShowStatusValidationMessage("That status already exists.");
            return false;
        }

        return true;
    }

    private void ShowStatusValidationMessage(string message)
    {
        using var dialog = new ThemedMessageForm(Text, message, _theme, ThemedMessageKind.Information);
        dialog.ShowDialog(this);
    }

    private void RemoveSongStatus()
    {
        var selectedIndex = _songStatusesListBox.SelectedIndex;
        if (selectedIndex < 0)
        {
            return;
        }

        var removedStatus = _songStatusesListBox.Items[selectedIndex]?.ToString() ?? "";
        using var confirmation = new ThemedConfirmationForm(
            "Remove Status",
            $"Are you sure you want to remove the status \"{removedStatus}\"?\n\nThis action cannot be undone after you save Preferences.",
            _theme,
            okText: "Remove");
        if (confirmation.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _songStatusesListBox.Items.RemoveAt(selectedIndex);
        if (!string.IsNullOrWhiteSpace(removedStatus))
        {
            foreach (var originalName in _songStatusRenames
                         .Where(pair => string.Equals(pair.Value, removedStatus, StringComparison.OrdinalIgnoreCase))
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                _songStatusRenames.Remove(originalName);
            }
        }
        if (_songStatusesListBox.Items.Count > 0)
        {
            _songStatusesListBox.SelectedIndex = Math.Min(selectedIndex, _songStatusesListBox.Items.Count - 1);
        }
        else
        {
            _songStatusTextBox.Clear();
        }
    }

    private void RestoreDefaultSongStatuses()
    {
        using var confirmation = new ThemedConfirmationForm(
            "Restore Default Statuses",
            "Are you sure you want to restore the default statuses? Any custom statuses will be removed.\n\nThis action cannot be undone after you save Preferences.",
            _theme,
            okText: "Restore Defaults");
        if (confirmation.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _songStatusRenames.Clear();
        SetSongStatusItems(BrowserConfigStore.DefaultSongStatuses);
    }

    private void MoveSongStatus(int offset)
    {
        var oldIndex = _songStatusesListBox.SelectedIndex;
        var newIndex = oldIndex + offset;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= _songStatusesListBox.Items.Count)
        {
            return;
        }

        var status = _songStatusesListBox.Items[oldIndex];
        _songStatusesListBox.Items.RemoveAt(oldIndex);
        _songStatusesListBox.Items.Insert(newIndex, status);
        _songStatusesListBox.SelectedIndex = newIndex;
    }

    private void UpdateSongStatusActionButtons()
    {
        var selectedIndex = _songStatusesListBox.SelectedIndex;
        var hasSelection = selectedIndex >= 0;
        _renameSongStatusButton.Enabled = hasSelection;
        _removeSongStatusButton.Enabled = hasSelection;
        _moveSongStatusUpButton.Enabled = selectedIndex > 0;
        _moveSongStatusDownButton.Enabled = hasSelection
            && selectedIndex < _songStatusesListBox.Items.Count - 1;
    }

    private void SetSongStatusItems(IEnumerable<string> statuses)
    {
        _songStatusesListBox.BeginUpdate();
        try
        {
            _songStatusesListBox.Items.Clear();
            _songStatusesListBox.Items.AddRange(statuses.Cast<object>().ToArray());
        }
        finally
        {
            _songStatusesListBox.EndUpdate();
        }

        if (_songStatusesListBox.Items.Count > 0)
        {
            _songStatusesListBox.SelectedIndex = 0;
        }
    }

    private void BuildCustomColumnsTab(TabPage tabPage)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabPage.Controls.Add(layout);
        layout.Controls.Add(new Label
        {
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Text = "Add text columns for details such as Client No. Values are saved for each song. New columns can be shown with View > Select Columns to Display.",
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 0);
        _customColumnsListBox.Dock = DockStyle.Fill;
        _customColumnsListBox.IntegralHeight = false;
        _customColumnsListBox.BackColor = _theme.PanelBackColor;
        _customColumnsListBox.ForeColor = _theme.TextColor;
        _customColumnsListBox.DoubleClick += (_, _) => RenameCustomColumn();
        layout.Controls.Add(_customColumnsListBox, 0, 1);
        var addRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 8, 0, 0) };
        addRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        addRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _customColumnNameTextBox.Dock = DockStyle.Fill;
        _customColumnNameTextBox.BackColor = _theme.PanelBackColor;
        _customColumnNameTextBox.ForeColor = _theme.TextColor;
        WindowsShell.SetCueBanner(_customColumnNameTextBox, "New column name, e.g. Client No.");
        addRow.Controls.Add(_customColumnNameTextBox, 0, 0);
        var addButton = new Button { Text = "Add", AutoSize = true };
        StyleButton(addButton, useAccent: false);
        addButton.Click += (_, _) => AddCustomColumn();
        addRow.Controls.Add(addButton, 1, 0);
        layout.Controls.Add(addRow, 0, 2);
        var actions = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        var renameButton = new Button { Text = "Rename Selected...", AutoSize = true };
        var removeButton = new Button { Text = "Remove Selected", AutoSize = true };
        StyleButton(renameButton, useAccent: false);
        StyleButton(removeButton, useAccent: false);
        renameButton.Click += (_, _) => RenameCustomColumn();
        removeButton.Click += (_, _) => RemoveCustomColumn();
        actions.Controls.Add(renameButton);
        actions.Controls.Add(removeButton);
        layout.Controls.Add(actions, 0, 3);
    }

    private void RefreshCustomColumnsList(int selectedIndex = -1)
    {
        _customColumnsListBox.Items.Clear();
        _customColumnsListBox.Items.AddRange(SelectedCustomSongColumns.Select(column => (object)column.Name).ToArray());
        if (_customColumnsListBox.Items.Count > 0)
            _customColumnsListBox.SelectedIndex = Math.Clamp(selectedIndex, 0, _customColumnsListBox.Items.Count - 1);
    }

    private bool ValidateCustomColumnName(string name, string? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        string[] builtInNames =
        [
            "Song", "Title", "Artist", "Year", "Date Created", "Last Modified", "Tempo",
            "Key Signature", "Time Signature", "Track Count", "Length", "Sample Rate",
            "Bit Depth", "Studio Version", "Format Version", "Song Notes", "Notes File",
            "Preset Count", "Comment",
            "Path", "Genre", "Status", "Rating", "Mixdowns"
        ];
        if (builtInNames.Contains(name, StringComparer.OrdinalIgnoreCase)
            || SelectedCustomSongColumns.Any(column => column.Id != currentId
                && string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            using var message = new ThemedMessageForm(Text, "That column name is already in use.", _theme, ThemedMessageKind.Information);
            message.ShowDialog(this);
            return false;
        }
        return true;
    }

    private void AddCustomColumn()
    {
        var name = _customColumnNameTextBox.Text.Trim();
        if (!ValidateCustomColumnName(name)) return;
        SelectedCustomSongColumns = SelectedCustomSongColumns.Concat(
            [new CustomSongColumn { Id = Guid.NewGuid().ToString("N"), Name = name }]).ToArray();
        RefreshCustomColumnsList(SelectedCustomSongColumns.Count - 1);
        _customColumnNameTextBox.Clear();
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData == Keys.Enter && _customColumnNameTextBox.Focused)
        {
            AddCustomColumn();
            return true;
        }

        return base.ProcessDialogKey(keyData);
    }

    private void RenameCustomColumn()
    {
        var index = _customColumnsListBox.SelectedIndex;
        if (index < 0) return;
        var current = SelectedCustomSongColumns[index];
        using var prompt = new ThemedTextPromptForm("Rename Custom Column", "Column name", current.Name, _theme);
        if (prompt.ShowDialog(this) != DialogResult.OK || !ValidateCustomColumnName(prompt.EnteredText, current.Id)) return;
        current.Name = prompt.EnteredText;
        RefreshCustomColumnsList(index);
    }

    private void RemoveCustomColumn()
    {
        var index = _customColumnsListBox.SelectedIndex;
        if (index < 0) return;
        var column = SelectedCustomSongColumns[index];
        using var confirmation = new ThemedConfirmationForm(
            "Remove Custom Column",
            $"Remove '{column.Name}' and its values from all songs when you click OK?",
            _theme,
            okText: "Remove");
        if (confirmation.ShowDialog(this) != DialogResult.OK) return;
        SelectedCustomSongColumns = SelectedCustomSongColumns.Where(item => item.Id != column.Id).ToArray();
        RefreshCustomColumnsList(index);
    }

    private void BuildGenresTab(TabPage tabPage)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(10),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabPage.Controls.Add(layout);

        var heading = new Label
        {
            AutoSize = true,
            Text = "Song Genre Options",
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 6)
        };
        layout.Controls.Add(heading, 0, 0);

        var explanation = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Text = "These entries appear in the Genre dropdown in SongLens View. Removing an option does not erase that genre from songs already using it.",
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 0, 0, 10)
        };
        layout.Controls.Add(explanation, 0, 1);

        _songGenresListBox.Dock = DockStyle.Fill;
        _songGenresListBox.IntegralHeight = false;
        _songGenresListBox.BackColor = _theme.PanelBackColor;
        _songGenresListBox.ForeColor = _theme.TextColor;
        _songGenresListBox.BorderStyle = BorderStyle.FixedSingle;
        _songGenresListBox.SelectedIndexChanged += (_, _) => UpdateSongGenreActionButtons();
        _songGenresListBox.DoubleClick += (_, _) => RenameSongGenre();
        layout.Controls.Add(_songGenresListBox, 0, 2);

        var editorRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 8, 0, 0)
        };
        editorRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editorRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var addGenreLabel = new Label
        {
            AutoSize = true,
            Text = "Add a new genre",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 4)
        };
        editorRow.Controls.Add(addGenreLabel, 0, 0);
        editorRow.SetColumnSpan(addGenreLabel, 2);
        _songGenreTextBox.Dock = DockStyle.Fill;
        _songGenreTextBox.BorderStyle = BorderStyle.FixedSingle;
        _songGenreTextBox.BackColor = _theme.PanelBackColor;
        _songGenreTextBox.ForeColor = _theme.TextColor;
        WindowsShell.SetCueBanner(_songGenreTextBox, "Enter a new genre");
        _songGenreTextBox.KeyDown += (_, args) =>
        {
            if (args.KeyCode == Keys.Enter)
            {
                AddSongGenre();
                args.SuppressKeyPress = true;
            }
        };
        var addButton = new Button { Text = "Add", AutoSize = true };
        addButton.Click += (_, _) => AddSongGenre();
        StyleButton(addButton, useAccent: false);
        editorRow.Controls.Add(_songGenreTextBox, 0, 1);
        editorRow.Controls.Add(addButton, 1, 1);
        layout.Controls.Add(editorRow, 0, 3);

        var actionRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 8, 0, 0)
        };
        var restoreButton = new Button { Text = "Restore Defaults", AutoSize = true };
        _renameSongGenreButton.Click += (_, _) => RenameSongGenre();
        _removeSongGenreButton.Click += (_, _) => RemoveSongGenre();
        _moveSongGenreUpButton.Click += (_, _) => MoveSongGenre(-1);
        _moveSongGenreDownButton.Click += (_, _) => MoveSongGenre(1);
        restoreButton.Click += (_, _) => RestoreDefaultSongGenres();
        foreach (var button in new[]
                 {
                     _renameSongGenreButton,
                     _removeSongGenreButton,
                     _moveSongGenreUpButton,
                     _moveSongGenreDownButton,
                     restoreButton
                 })
        {
            StyleButton(button, useAccent: false);
            actionRow.Controls.Add(button);
        }
        layout.Controls.Add(actionRow, 0, 4);
        UpdateSongGenreActionButtons();
    }

    private void AddSongGenre()
    {
        var genre = _songGenreTextBox.Text.Trim();
        if (!ValidateSongGenre(genre))
        {
            return;
        }

        _songGenresListBox.Items.Add(genre);
        _songGenresListBox.SelectedIndex = _songGenresListBox.Items.Count - 1;
        _songGenreTextBox.Clear();
        _songGenreTextBox.Focus();
    }

    private void RenameSongGenre()
    {
        var selectedIndex = _songGenresListBox.SelectedIndex;
        if (selectedIndex < 0)
        {
            ShowStatusValidationMessage("Select a genre to rename.");
            return;
        }

        var oldGenre = _songGenresListBox.Items[selectedIndex]?.ToString() ?? "";
        using var prompt = new ThemedTextPromptForm(
            "Rename Genre",
            "Enter a new name for the selected genre:",
            oldGenre,
            _theme,
            okText: "Rename",
            emptyValueMessage: "Enter a genre name.");
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var genre = prompt.EnteredText;
        if (!ValidateSongGenre(genre, selectedIndex))
        {
            return;
        }

        _songGenresListBox.Items[selectedIndex] = genre;
        _songGenresListBox.SelectedIndex = selectedIndex;

        var originalNames = _songGenreRenames
            .Where(pair => string.Equals(pair.Value, oldGenre, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Key)
            .ToArray();
        if (originalNames.Length == 0)
        {
            _songGenreRenames[oldGenre] = genre;
        }
        else
        {
            foreach (var originalName in originalNames)
            {
                _songGenreRenames[originalName] = genre;
            }
        }
    }

    private bool ValidateSongGenre(string genre, int ignoredIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(genre))
        {
            ShowStatusValidationMessage("Enter a genre name.");
            return false;
        }

        var duplicate = _songGenresListBox.Items
            .Cast<string>()
            .Where((_, index) => index != ignoredIndex)
            .Any(existing => string.Equals(existing, genre, StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            ShowStatusValidationMessage("That genre already exists.");
            return false;
        }

        return true;
    }

    private void RemoveSongGenre()
    {
        var selectedIndex = _songGenresListBox.SelectedIndex;
        if (selectedIndex < 0)
        {
            return;
        }

        var removedGenre = _songGenresListBox.Items[selectedIndex]?.ToString() ?? "";
        using var confirmation = new ThemedConfirmationForm(
            "Remove Genre",
            $"Are you sure you want to remove the genre \"{removedGenre}\"?\n\nThis action cannot be undone after you save Preferences.",
            _theme,
            okText: "Remove");
        if (confirmation.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _songGenresListBox.Items.RemoveAt(selectedIndex);
        if (!string.IsNullOrWhiteSpace(removedGenre))
        {
            foreach (var originalName in _songGenreRenames
                         .Where(pair => string.Equals(pair.Value, removedGenre, StringComparison.OrdinalIgnoreCase))
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                _songGenreRenames.Remove(originalName);
            }
        }
        if (_songGenresListBox.Items.Count > 0)
        {
            _songGenresListBox.SelectedIndex = Math.Min(selectedIndex, _songGenresListBox.Items.Count - 1);
        }
        else
        {
            _songGenreTextBox.Clear();
        }
    }

    private void RestoreDefaultSongGenres()
    {
        using var confirmation = new ThemedConfirmationForm(
            "Restore Default Genres",
            "Are you sure you want to restore the default genres? Any custom genres will be removed.\n\nThis action cannot be undone after you save Preferences.",
            _theme,
            okText: "Restore Defaults");
        if (confirmation.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _songGenreRenames.Clear();
        SetSongGenreItems(BrowserConfigStore.DefaultSongGenres);
    }

    private void MoveSongGenre(int offset)
    {
        var oldIndex = _songGenresListBox.SelectedIndex;
        var newIndex = oldIndex + offset;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= _songGenresListBox.Items.Count)
        {
            return;
        }

        var genre = _songGenresListBox.Items[oldIndex];
        _songGenresListBox.Items.RemoveAt(oldIndex);
        _songGenresListBox.Items.Insert(newIndex, genre);
        _songGenresListBox.SelectedIndex = newIndex;
    }

    private void UpdateSongGenreActionButtons()
    {
        var selectedIndex = _songGenresListBox.SelectedIndex;
        var hasSelection = selectedIndex >= 0;
        _renameSongGenreButton.Enabled = hasSelection;
        _removeSongGenreButton.Enabled = hasSelection;
        _moveSongGenreUpButton.Enabled = selectedIndex > 0;
        _moveSongGenreDownButton.Enabled = hasSelection
            && selectedIndex < _songGenresListBox.Items.Count - 1;
    }

    private void SetSongGenreItems(IEnumerable<string> genres)
    {
        _songGenresListBox.BeginUpdate();
        try
        {
            _songGenresListBox.Items.Clear();
            _songGenresListBox.Items.AddRange(genres.Cast<object>().ToArray());
        }
        finally
        {
            _songGenresListBox.EndUpdate();
        }

        if (_songGenresListBox.Items.Count > 0)
        {
            _songGenresListBox.SelectedIndex = 0;
        }
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
        _songGridDoubleClickComboBox.SelectedIndex = SelectedSongGridDoubleClickAction == SongGridDoubleClickAction.OpenSongInDaw
            ? 0
            : 1;
        _restoreFilterSessionCheckBox.Checked = RestoreFilterSessionOnStartup;
        _mixdownFolderNameTextBox.Text = MixdownFolderName;
        _defaultAudioPlayerRadioButton.Checked = !UseCustomAudioPlayer;
        _customAudioPlayerRadioButton.Checked = UseCustomAudioPlayer;
        SetCustomAudioPlayerPath(CustomAudioPlayerPath);
        _customAudioPlayerArgumentsTextBox.Text = CustomAudioPlayerArguments ?? "";
        UpdateCustomAudioPlayerControls();
        _historyRetentionPresetCheckBox.Checked = UseHistoryRetentionPreset;
        _historyRevisionsToKeepNumeric.Value = HistoryRevisionsToKeep;
        _historyRevisionsToKeepNumeric.Enabled = UseHistoryRetentionPreset;
        SetSongStatusItems(SelectedSongStatuses);
        SetSongGenreItems(SelectedSongGenres);
        RefreshCustomColumnsList();
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
        _audioPlayerPathTextBox.MinimumSize = Size.Empty;
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

    private void UpdateCustomAudioPlayerControls()
    {
        _audioPlayerPathRow.Enabled = _customAudioPlayerRadioButton.Checked;
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
        SelectedSongGridDoubleClickAction = _songGridDoubleClickComboBox.SelectedIndex == 0
            ? SongGridDoubleClickAction.OpenSongInDaw
            : SongGridDoubleClickAction.RevealInExplorer;
        RestoreFilterSessionOnStartup = _restoreFilterSessionCheckBox.Checked;
        MixdownFolderName = mixdownFolderName;
        UseCustomAudioPlayer = _customAudioPlayerRadioButton.Checked;
        CustomAudioPlayerPath = customAudioPlayerPath;
        CustomAudioPlayerArguments = string.IsNullOrWhiteSpace(_customAudioPlayerArgumentsTextBox.Text)
            ? null
            : _customAudioPlayerArgumentsTextBox.Text.Trim();
        UseHistoryRetentionPreset = _historyRetentionPresetCheckBox.Checked;
        HistoryRevisionsToKeep = (int)_historyRevisionsToKeepNumeric.Value;
        SelectedSongStatuses = _songStatusesListBox.Items.Cast<string>().ToArray();
        SelectedSongGenres = _songGenresListBox.Items.Cast<string>().ToArray();
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
        button.FlatAppearance.MouseOverBackColor = _theme.AccentHoverColor;
        button.FlatAppearance.MouseDownBackColor = _theme.AccentPressedColor;
        button.BackColor = _theme.AccentSoftColor;
        button.ForeColor = _theme.TextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.Font = Font;
    }
}
