namespace SongMetainfoBrowser.App;

public sealed class HelpForm : Form
{
    private readonly AppTheme _theme;

    public HelpForm(AppTheme theme)
    {
        _theme = theme;

        Text = "SongLens Help";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        MinimumSize = new Size(760, 560);
        ClientSize = new Size(820, 620);
        Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

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
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 6)
        };

        var subtitleLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(740, 0),
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
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
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
                "",
                "Folder Tree",
                "- Expand opens all visible folders below the current songs root.",
                "- Collapse closes child folders and leaves the root open so you do not lose your place.",
                "- The tree is filtered by the current song-age filter, so folders can appear or disappear when that filter changes.",
                "",
                "Search And Refresh",
                "- Enter text in the search box and press Enter or click the search button to search all songs under the root folder.",
                "- Search looks across filename, title, artist, tempo, key, year, comment, notes, and other indexed metadata.",
                "- Refresh is the quickest way to reset the current view after changing files on disk or clearing a search.",
                "",
                "Song Details Tabs",
                "- Summary: high-level metadata such as title, artist, dates, tempo, key, length, and path.",
                "- Attributes: raw metainfo.xml values for deeper inspection.",
                "- Tracks: track number, track name, instrument, and track notes when present in the song.",
                "- Notes: the song-level notes.txt text.",
                "- History: opens the history viewer for the selected song.",
                "",
                "Dates, Tempo, And Layout",
                "- Dates and times are shown using your Windows regional settings.",
                "- Tempo values are rounded for easier reading.",
                "- If you resize grid columns, SongLens remembers those widths for future sessions.",
                "",
                "Themes And Settings",
                "- Use View > Theme to switch between Dark and Light modes.",
                "- SongLens remembers your songs folder, theme choice, and saved grid layouts automatically.",
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
        button.Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
    }
}
