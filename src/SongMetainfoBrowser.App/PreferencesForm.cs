namespace SongMetainfoBrowser.App;

internal sealed class PreferencesForm : Form
{
    private readonly AppTheme _theme;
    private readonly TextBox _rootPathTextBox = new();
    private readonly RadioButton _lightThemeRadioButton = new() { AutoSize = true, Text = "Light" };
    private readonly RadioButton _darkThemeRadioButton = new() { AutoSize = true, Text = "Dark" };
    private readonly TableLayoutPanel _rootPathRow = new();
    private readonly TabControl _tabs = new();
    private int _hoveredTabIndex = -1;
    private readonly CheckBox _stickyTabsCheckBox = new() { AutoSize = true, Text = "Use Sticky Tabs" };
    private readonly CheckBox _songLaunchCheckBox = new() { AutoSize = true, Text = "Enable option to open selected song" };
    private readonly CheckBox _restoreFilterSessionCheckBox = new() { AutoSize = true, Text = "Restore previous filter and view on startup" };
    private readonly CheckBox _restoreAdvancedSearchSessionCheckBox = new() { AutoSize = true, Text = "Restore last advanced search on startup" };
    private readonly bool _showEnableSongLaunchOption;
    private readonly Action<IWin32Window> _showVisibleTabsDialog;
    public string SelectedRootPath { get; private set; } = "";
    public string SelectedThemeName { get; private set; } = AppThemes.Light.Name;
    public bool UseStickyTabs { get; private set; }
    public bool EnableSongLaunch { get; private set; }
    public bool RestoreFilterSessionOnStartup { get; private set; }
    public bool RestoreAdvancedSearchSessionOnStartup { get; private set; }
    public AppFontPreferences SelectedFontPreferences { get; private set; }

    public PreferencesForm(string currentRootPath, string currentThemeName, bool useStickyTabs, bool enableSongLaunch, bool showEnableSongLaunchOption, bool restoreFilterSessionOnStartup, bool restoreAdvancedSearchSessionOnStartup, AppFontPreferences currentFontPreferences, AppTheme theme, Action<IWin32Window> showVisibleTabsDialog)
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
        var appearanceTab = new TabPage("Appearance") { BackColor = _theme.AppBackColor, ForeColor = _theme.TextColor, AutoScroll = true };
        _tabs.TabPages.Add(generalTab);
        _tabs.TabPages.Add(appearanceTab);
        _tabs.SelectedIndex = 0;
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
            Text = "Adjust the main UI, folder tree, song grid, detail grids, notes text, and dialog fonts.",
            MaximumSize = new Size(500, 0),
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 8, 0, 0)
        };
        layout.Controls.Add(fontNoteLabel, 0, 4);

        var visibleTabsButton = new Button
        {
            Text = "Adjust Visible Tabs",
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
        SelectedThemeName = _darkThemeRadioButton.Checked ? AppThemes.Dark.Name : AppThemes.Light.Name;
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
