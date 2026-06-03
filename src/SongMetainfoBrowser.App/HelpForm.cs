namespace SongMetainfoBrowser.App;

public sealed class HelpForm : Form
{
    private readonly AppTheme _theme;
    private readonly AppFontPreferences _fontPreferences;

    public HelpForm(AppTheme theme)
    {
        _fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;

        Text = "SongLens Help";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        MinimumSize = AppFontSettings.Scale(new Size(760, 560), _fontPreferences, AppFontSection.Dialogs);
        ClientSize = AppFontSettings.Scale(new Size(820, 620), _fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.Dialogs);

        BuildLayout();
    }

    private void BuildLayout()
    {
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(18),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = "Using SongLens",
            Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.Dialogs, FontStyle.Bold),
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 6)
        };

        var subtitleLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(AppFontSettings.Scale(740, _fontPreferences, AppFontSection.Dialogs), 0),
            Text = "SongLens lets you browse Studio One .song files, inspect metadata, and review notes, tracks, and history without opening each song in Studio One.",
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 0, 0, 12)
        };

        var helpTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = _theme.PanelBackColor,
            ForeColor = _theme.TextColor,
            Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.Dialogs),
            HideSelection = true,
            TabStop = false,
            Text = BuildHelpText()
        };

        var closeButton = new Button
        {
            Text = "Close",
            AutoSize = true,
            Anchor = AnchorStyles.Right
        };
        StyleButton(closeButton);
        closeButton.Click += (_, _) => Close();

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 12, 0, 0)
        };
        buttonPanel.Controls.Add(closeButton);

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(subtitleLabel, 0, 1);
        layout.Controls.Add(helpTextBox, 0, 2);
        layout.Controls.Add(buttonPanel, 0, 3);

        AcceptButton = closeButton;
        CancelButton = closeButton;

        Shown += (_, _) =>
        {
            closeButton.Focus();
            helpTextBox.SelectionStart = 0;
            helpTextBox.SelectionLength = 0;
        };
    }

    private static string BuildHelpText()
    {
        return string.Join(
            Environment.NewLine,
            [
                "Getting Started",
                "1. Choose your Studio One songs folder with Browse if SongLens does not already have one saved.",
                "2. The folder tree on the left shows only folders that contain visible .song files somewhere underneath them.",
                "3. Click a folder to load the songs in that folder. Double-click a song row to reveal the file in Windows Explorer.",
                "4. Use File > Open in Recommended App to launch the selected song in the app it was last saved with.",
                "",
                "Folder Tree",
                "- Expand opens all visible folders below the current songs root.",
                "- Collapse closes child folders and leaves the root open so you do not lose your place.",
                "- The tree is filtered by the current song-age filter, so folders can appear or disappear when that filter changes.",
                "- If you click a folder while search results are showing, SongLens exits search mode and loads that folder immediately.",
                "",
                "Search And Refresh",
                "- Enter text in the search box and press Enter or click the search button to search all songs under the root folder.",
                "- Search looks across filename, title, artist, tempo, key, year, comment, notes, and other indexed metadata.",
                "- When all search results live in the same folder, SongLens also focuses that folder in the tree.",
                "- Refresh is the quickest way to reset the current view after changing files on disk or clearing a search.",
                "",
                "Song Details Tabs",
                "- Summary: high-level metadata such as title, artist, dates, tempo, key, length, and path.",
                "- Attributes: raw metainfo.xml values for deeper inspection.",
                "- Tracks: track number, track name, instrument, and track notes when present in the song.",
                "- Notes: the song-level notes.txt text.",
                "- History: opens the history viewer for the selected song.",
                "- Use View > Use Sticky Tabs if you want SongLens to stay on the current detail tab while you move between songs.",
                "",
                "Exports And Snapshots",
                "- File > Export CSV exports the current search results or your full library, depending on the current view.",
                "- Export CSV lets you choose which fields to include and save your preferred field set for later use.",
                "- File > Save Snapshot creates a musician-friendly song snapshot in Text or JSON format.",
                "- Snapshot Preview lets you review the snapshot in Text and JSON before saving.",
                "",
                "Dates, Tempo, And Layout",
                "- Dates and times are shown using your Windows regional settings.",
                "- Tempo values are rounded for easier reading.",
                "- If you resize grid columns, SongLens remembers those widths for future sessions.",
                "- Use View > Change Font Sizes to tune the main UI, folder tree, grids, notes text, and dialogs independently.",
                "",
                "Themes And Settings",
                "- Use View > Theme to switch between Dark and Light modes.",
                "- SongLens remembers your songs folder, theme choice, saved grid layouts, and font size preferences automatically.",
                "",
                "Tips",
                "- If a song seems buried, use Expand to reveal the full visible folder structure.",
                "- If a song does not appear, verify that it is a regular .song file and not an autosaved copy.",
                "- After changing your song library outside SongLens, use Refresh to reload the current view."
            ]);
    }

    private void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = _theme.NeutralHoverColor;
        button.FlatAppearance.MouseDownBackColor = _theme.NeutralPressedColor;
        button.BackColor = _theme.AccentSoftColor;
        button.ForeColor = _theme.TextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.Font = Font;
    }
}
