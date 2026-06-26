namespace SongMetainfoBrowser.App;

internal sealed class PreferencesForm : Form
{
    private readonly AppTheme _theme;
    private readonly TextBox _rootPathTextBox = new();
    private readonly ComboBox _themeComboBox = new();
    private readonly TableLayoutPanel _rootPathRow = new();
    private readonly TabControl _tabs = new();
    private readonly CheckBox _stickyTabsCheckBox = new() { AutoSize = true, Text = "Use Sticky Tabs" };
    private readonly CheckBox _songLaunchCheckBox = new() { AutoSize = true, Text = "Enable song launch actions" };
    private readonly CheckBox _restoreFilterSessionCheckBox = new() { AutoSize = true, Text = "Restore previous filter and view on startup" };
    private readonly CheckBox _restoreAdvancedSearchSessionCheckBox = new() { AutoSize = true, Text = "Restore last advanced search on startup" };
    private readonly bool _showEnableSongLaunchOption;
    public string SelectedRootPath { get; private set; } = "";
    public string SelectedThemeName { get; private set; } = AppThemes.Dark.Name;
    public bool UseStickyTabs { get; private set; }
    public bool EnableSongLaunch { get; private set; }
    public bool RestoreFilterSessionOnStartup { get; private set; }
    public bool RestoreAdvancedSearchSessionOnStartup { get; private set; }
    public AppFontPreferences SelectedFontPreferences { get; private set; }

    public PreferencesForm(string currentRootPath, string currentThemeName, bool useStickyTabs, bool enableSongLaunch, bool showEnableSongLaunchOption, bool restoreFilterSessionOnStartup, bool restoreAdvancedSearchSessionOnStartup, AppFontPreferences currentFontPreferences, AppTheme theme)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        _showEnableSongLaunchOption = showEnableSongLaunchOption;
        SelectedRootPath = currentRootPath;
        SelectedThemeName = currentThemeName;
        UseStickyTabs = useStickyTabs;
        EnableSongLaunch = enableSongLaunch;
        RestoreFilterSessionOnStartup = restoreFilterSessionOnStartup;
        RestoreAdvancedSearchSessionOnStartup = restoreAdvancedSearchSessionOnStartup;
        SelectedFontPreferences = currentFontPreferences;
        var minimumClientSize = AppFontSettings.Scale(new Size(540, 340), fontPreferences, AppFontSection.Dialogs);

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
        var generalTab = new TabPage("General") { BackColor = _theme.AppBackColor, ForeColor = _theme.TextColor, AutoScroll = true };
        var appearanceTab = new TabPage("Appearance") { BackColor = _theme.AppBackColor, ForeColor = _theme.TextColor, AutoScroll = true };
        _tabs.TabPages.Add(generalTab);
        _tabs.TabPages.Add(appearanceTab);
        rootLayout.Controls.Add(_tabs, 0, 0);

        BuildGeneralTab(generalTab);
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
            RowCount = 6,
            Padding = new Padding(10),
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabPage.Controls.Add(layout);

        var rootLabel = new Label
        {
            AutoSize = true,
            Text = "Songs root folder",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 6)
        };
        layout.Controls.Add(rootLabel, 0, 0);

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
        layout.Controls.Add(_rootPathRow, 0, 1);

        _stickyTabsCheckBox.BackColor = _theme.AppBackColor;
        _stickyTabsCheckBox.ForeColor = _theme.TextColor;
        _stickyTabsCheckBox.Margin = new Padding(0, 10, 0, 6);
        _stickyTabsCheckBox.Padding = Padding.Empty;
        layout.Controls.Add(_stickyTabsCheckBox, 0, 2);

        _restoreFilterSessionCheckBox.BackColor = _theme.AppBackColor;
        _restoreFilterSessionCheckBox.ForeColor = _theme.TextColor;
        _restoreFilterSessionCheckBox.Margin = new Padding(0, 10, 0, 6);
        _restoreFilterSessionCheckBox.Padding = Padding.Empty;
        layout.Controls.Add(_restoreFilterSessionCheckBox, 0, 3);

        _restoreAdvancedSearchSessionCheckBox.BackColor = _theme.AppBackColor;
        _restoreAdvancedSearchSessionCheckBox.ForeColor = _theme.TextColor;
        _restoreAdvancedSearchSessionCheckBox.Margin = new Padding(0, 10, 0, 6);
        _restoreAdvancedSearchSessionCheckBox.Padding = Padding.Empty;
        layout.Controls.Add(_restoreAdvancedSearchSessionCheckBox, 0, 4);

        _songLaunchCheckBox.BackColor = _theme.AppBackColor;
        _songLaunchCheckBox.ForeColor = _theme.TextColor;
        _songLaunchCheckBox.Margin = new Padding(0, 10, 0, 6);
        _songLaunchCheckBox.Padding = Padding.Empty;
        _songLaunchCheckBox.Visible = _showEnableSongLaunchOption;
        if (_showEnableSongLaunchOption)
        {
            layout.Controls.Add(_songLaunchCheckBox, 0, 5);
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
            RowCount = 6,
            Padding = new Padding(10),
            BackColor = _theme.AppBackColor
        };
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

        _themeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeComboBox.BackColor = _theme.PanelBackColor;
        _themeComboBox.ForeColor = _theme.TextColor;
        _themeComboBox.Width = 180;
        _themeComboBox.Items.Add(AppThemes.Dark.Name);
        _themeComboBox.Items.Add(AppThemes.Light.Name);
        layout.Controls.Add(_themeComboBox, 0, 1);

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
            Text = "Adjust the main UI, folder tree, song grid, detail grids, notes text, and dialog fonts.",
            MaximumSize = new Size(500, 0),
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 8, 0, 0)
        };
        layout.Controls.Add(fontNoteLabel, 0, 4);
    }

    private void LoadCurrentValues()
    {
        _rootPathTextBox.Text = SelectedRootPath;
        _themeComboBox.SelectedItem = SelectedThemeName;
        _stickyTabsCheckBox.Checked = UseStickyTabs;
        _songLaunchCheckBox.Checked = EnableSongLaunch;
        _restoreFilterSessionCheckBox.Checked = RestoreFilterSessionOnStartup;
        _restoreAdvancedSearchSessionCheckBox.Checked = RestoreAdvancedSearchSessionOnStartup;
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

    private void ConfirmSelection()
    {
        if (!string.IsNullOrWhiteSpace(_rootPathTextBox.Text) && !Directory.Exists(_rootPathTextBox.Text))
        {
            using var messageDialog = new ThemedMessageForm(Text, "Choose an existing songs folder.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        SelectedRootPath = _rootPathTextBox.Text.Trim();
        SelectedThemeName = _themeComboBox.SelectedItem?.ToString() ?? AppThemes.Dark.Name;
        UseStickyTabs = _stickyTabsCheckBox.Checked;
        EnableSongLaunch = _songLaunchCheckBox.Checked;
        RestoreFilterSessionOnStartup = _restoreFilterSessionCheckBox.Checked;
        RestoreAdvancedSearchSessionOnStartup = _restoreAdvancedSearchSessionCheckBox.Checked;
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
