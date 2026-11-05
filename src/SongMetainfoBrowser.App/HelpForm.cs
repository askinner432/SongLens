namespace SongMetainfoBrowser.App;

public sealed class HelpForm : Form
{
    private readonly AppTheme _theme;
    private readonly AppFontPreferences _fontPreferences;
    private readonly bool _focusSongLensView;
    private readonly RichTextBox _helpTextBox = new();

    public HelpForm(AppTheme theme, bool focusSongLensView = false)
    {
        _fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        _focusSongLensView = focusSongLensView;

        Text = focusSongLensView ? "SongLens View Help" : "SongLens Help";
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
            Text = _focusSongLensView ? "SongLens View" : "Using SongLens",
            Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.Dialogs, FontStyle.Bold),
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 6)
        };

        var subtitleLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(AppFontSettings.Scale(740, _fontPreferences, AppFontSection.Dialogs), 0),
            Text = _focusSongLensView
                ? $"Use this guide to understand the SongLens View header, filters, columns, statistics, saved layouts, and mixdown indicators.\nSource: {GetSongLensViewHelpPath()}"
                : "SongLens lets you browse Studio One .song files, inspect metadata, and review notes, tracks, and history without opening each song in Studio One.",
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 0, 0, 12)
        };

        _helpTextBox.Dock = DockStyle.Fill;
        _helpTextBox.ReadOnly = true;
        _helpTextBox.BorderStyle = BorderStyle.FixedSingle;
        _helpTextBox.BackColor = _theme.PanelBackColor;
        _helpTextBox.ForeColor = _theme.TextColor;
        _helpTextBox.Font = AppFontSettings.CreateUiFont(_fontPreferences, AppFontSection.Dialogs);
        _helpTextBox.HideSelection = true;
        _helpTextBox.TabStop = false;
        _helpTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
        _helpTextBox.DetectUrls = false;
        _helpTextBox.ShortcutsEnabled = true;

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
        if (_focusSongLensView && BrowserConfigStore.LoadEnableHelpEditing())
        {
            var reloadButton = new Button { Text = "Reload", AutoSize = true };
            StyleButton(reloadButton);
            reloadButton.Click += (_, _) =>
            {
                PopulateHelpText(_helpTextBox);
                _helpTextBox.Select(0, 0);
                _helpTextBox.ScrollToCaret();
            };
            buttonPanel.Controls.Add(reloadButton);

            var editButton = new Button { Text = "Edit Help Text", AutoSize = true };
            StyleButton(editButton);
            editButton.Click += (_, _) =>
            {
                var helpPath = GetSongLensViewHelpPath();
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(helpPath)
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception exception)
                {
                    using var message = new ThemedMessageForm(
                        Text,
                        $"Could not open the help text file:\n{helpPath}\n\n{exception.Message}",
                        _theme,
                        ThemedMessageKind.Information);
                    message.ShowDialog(this);
                }
            };
            buttonPanel.Controls.Add(editButton);
        }

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(subtitleLabel, 0, 1);
        layout.Controls.Add(_helpTextBox, 0, 2);
        layout.Controls.Add(buttonPanel, 0, 3);

        AcceptButton = closeButton;
        CancelButton = closeButton;

        Shown += (_, _) =>
        {
            PopulateHelpText(_helpTextBox);
            closeButton.Focus();
            if (_focusSongLensView)
            {
                var sectionStart = _helpTextBox.Find("SongLens View" + Environment.NewLine, 0, RichTextBoxFinds.MatchCase);
                if (sectionStart >= 0)
                {
                    _helpTextBox.Select(sectionStart, 0);
                    _helpTextBox.ScrollToCaret();
                }
                else
                {
                    _helpTextBox.Select(0, 0);
                }
            }
            else
            {
                _helpTextBox.SelectionStart = 0;
                _helpTextBox.SelectionLength = 0;
            }
        };
    }

    private void PopulateHelpText(RichTextBox helpTextBox)
    {
        string[]? externalSongLensHelpLines = null;
        if (_focusSongLensView)
        {
            var songLensHelpPath = GetSongLensViewHelpPath();
            if (File.Exists(songLensHelpPath))
            {
                try
                {
                    externalSongLensHelpLines = ReadSongLensViewHelpLines(songLensHelpPath);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    externalSongLensHelpLines =
                    [
                        "The help text file could not be read:",
                        songLensHelpPath,
                        "",
                        exception.Message
                    ];
                }
            }
        }

        var regularFont = helpTextBox.Font;
        var fonts = new Dictionary<FontStyle, Font>
        {
            [regularFont.Style] = regularFont
        };

        helpTextBox.Clear();
        var lines = _focusSongLensView
            ? externalSongLensHelpLines ?? BuildSongLensViewHelpLines()
            : BuildHelpLines();
        helpTextBox.SelectionIndent = 0;
        helpTextBox.SelectionHangingIndent = 0;
        foreach (var sourceLine in lines)
        {
            foreach (var line in ExpandHelpLine(sourceLine, _focusSongLensView))
            {
                var baseStyle = !_focusSongLensView && IsSectionTitle(line)
                    ? FontStyle.Bold
                    : FontStyle.Regular;
                AppendFormattedLine(helpTextBox, line, regularFont, baseStyle, fonts);
                helpTextBox.SelectionFont = GetHelpFont(regularFont, baseStyle, fonts);
                helpTextBox.AppendText(Environment.NewLine);
            }
        }

        foreach (var font in fonts.Values.Where(font => !ReferenceEquals(font, regularFont)))
        {
            font.Dispose();
        }

        helpTextBox.SelectionStart = 0;
        helpTextBox.SelectionLength = 0;
    }

    private static string[] ReadSongLensViewHelpLines(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }

    private static string GetSongLensViewHelpPath()
    {
        var bundledPath = Path.Combine(AppContext.BaseDirectory, "Help", "SongLensViewHelp.txt");
        var configDirectory = Path.GetDirectoryName(BrowserConfigStore.ConfigPath);
        if (string.IsNullOrWhiteSpace(configDirectory))
        {
            return bundledPath;
        }

        var editablePath = Path.Combine(configDirectory, "SongLensViewHelp.txt");
        if (File.Exists(editablePath))
        {
            return editablePath;
        }

        if (File.Exists(bundledPath))
        {
            try
            {
                Directory.CreateDirectory(configDirectory);
                File.Copy(bundledPath, editablePath);
                return editablePath;
            }
            catch (IOException) when (File.Exists(editablePath))
            {
                return editablePath;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return bundledPath;
    }

    private static string[] ExpandHelpLine(string sourceLine, bool splitLabels)
    {
        var line = sourceLine.Replace("\\t", "  ", StringComparison.Ordinal).Replace("\t", "  ", StringComparison.Ordinal);
        if (!splitLabels)
        {
            return [line];
        }

        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("**", StringComparison.Ordinal))
        {
            var labelEnd = trimmed.IndexOf("**", 2, StringComparison.Ordinal);
            if (labelEnd > 2)
            {
                var label = trimmed[2..labelEnd].Trim();
                var body = trimmed[(labelEnd + 2)..].TrimStart();
                if (label.EndsWith(':') && body.Length > 0)
                {
                    return [$"**{label}**", body];
                }
            }
        }

        return [line];
    }

    private static void AppendFormattedLine(
        RichTextBox textBox,
        string line,
        Font baseFont,
        FontStyle baseStyle,
        IDictionary<FontStyle, Font> fonts)
    {
        var activeStyle = baseStyle;
        var segment = new System.Text.StringBuilder();
        var pairedMarkup = FindPairedMarkupTokens(line);

        void FlushSegment()
        {
            if (segment.Length == 0)
            {
                return;
            }

            textBox.SelectionFont = GetHelpFont(baseFont, activeStyle, fonts);
            textBox.AppendText(segment.ToString());
            segment.Clear();
        }

        for (var index = 0; index < line.Length;)
        {
            if (pairedMarkup.TryGetValue(index, out var markup))
            {
                FlushSegment();
                activeStyle ^= markup.Style;
                index += markup.Length;
                continue;
            }

            segment.Append(line[index]);
            index++;
        }

        FlushSegment();
    }

    private static Dictionary<int, (int Length, FontStyle Style)> FindPairedMarkupTokens(string line)
    {
        var tokens = new List<(int Index, int Length, FontStyle Style)>();
        for (var index = 0; index < line.Length;)
        {
            if (line[index] == '*')
            {
                var runStart = index;
                while (index < line.Length && line[index] == '*')
                {
                    index++;
                }

                var runLength = index - runStart;
                while (runLength >= 2)
                {
                    tokens.Add((runStart, 2, FontStyle.Bold));
                    runStart += 2;
                    runLength -= 2;
                }

                if (runLength == 1)
                {
                    tokens.Add((runStart, 1, FontStyle.Italic));
                }

                continue;
            }

            if (line[index] == '_')
            {
                tokens.Add((index, 1, FontStyle.Underline));
            }

            index++;
        }

        var paired = new Dictionary<int, (int Length, FontStyle Style)>();
        var pending = new Dictionary<FontStyle, (int Index, int Length)>();
        foreach (var token in tokens)
        {
            if (pending.Remove(token.Style, out var opening))
            {
                paired[opening.Index] = (opening.Length, token.Style);
                paired[token.Index] = (token.Length, token.Style);
            }
            else
            {
                pending[token.Style] = (token.Index, token.Length);
            }
        }

        return paired;
    }

    private static Font GetHelpFont(Font baseFont, FontStyle style, IDictionary<FontStyle, Font> fonts)
    {
        if (!fonts.TryGetValue(style, out var font))
        {
            font = new Font(baseFont, style);
            fonts[style] = font;
        }

        return font;
    }

    private static string[] BuildHelpLines()
    {
        return
        [
            "Getting Started",
            "1. Choose your Studio One songs folder with Browse if SongLens does not already have one saved.",
            "2. The folder tree on the left shows only folders that contain visible .song files somewhere underneath them.",
            "3. Click a folder to load the songs in that folder. If the folder has subfolders, a single click also expands it.",
            "4. Double-click a song row to reveal the file in Windows Explorer or open it in your DAW, according to Preferences, or right-click it for song actions.",
            "5. Right-click a song and choose Open in Recommended App to launch it in the app it was last saved with.",
            "",
            "Folder Tree",
            "- Expand All opens all visible folders below the current songs root.",
            "- Collapse All closes child folders, leaves the root open, and scrolls back to the top of the tree.",
            "- The tree is filtered by the current song-age filter, so folders can appear or disappear when that filter changes.",
            "- Per-song History folders are excluded from the catalog so revisions do not appear as duplicate songs in the tree, searches, or exports.",
            "- If you click a folder while search results are showing, SongLens exits search mode and loads that folder immediately.",
            "- When a root effectively leads to one visible song, SongLens can load it automatically so the detail tabs are not blank.",
            "- Right-click a folder to open its context menu for actions such as Reveal in Explorer and Delete Folder.",
            "",
            "Right-Click Menus",
            "- Right-click a song row to open the song context menu.",
            "- The song context menu can include actions such as Open in Recommended App, Open in Alternate App, Listen to This Song, Rename Song, and Reveal in Explorer.",
            "- In SongLens View, ♪ means one mixdown is available and ♫ means multiple mixdowns are available. Double-click ♪ to play the single file. Double-click ♫ to choose a version, or use the existing right-click Listen to This Song menu. Audio remains available even when its filename no longer matches a renamed song.",
            "- Right-click a folder in the tree to open the folder context menu.",
            "- The folder context menu can include actions such as Reveal in Explorer, Expand Folder, Collapse Folder, and Delete Folder.",
            "",
            "Fast Search, Advanced Search, And Refresh",
            "- Use Fast Catalog Search by Keyword to search all songs under the current root folder.",
            "- Fast keyword search looks across Status, Genre, filename, title, artist, year, tempo, key, time signature, comment, notes, track names, instrument names, and track notes.",
            "- An exact assigned Status or Genre option such as Hybrid is treated as a classification search, preventing similarly named tracks or instruments from adding unrelated songs.",
            "- Use the funnel button or **Tools > Advanced Search...** for multi-rule searches such as dates, numeric comparisons, and combined conditions.",
            "- Advanced Search lets you choose All rules or Any rules, sort by up to four fields, save named searches with their sort order, and reopen the last search during the current session.",
            "- Status and Genre use your configured choices. Rating offers Unrated through five stars; use = for an exact rating, >= for at least, or <= for at most.",
            "",
            "SongLens View",
            "- Use Filter above SongLens View to show the full catalog, filter by age in days, or filter between two Created or Modified dates. An active age or date filter shows every matching song together in SongLens View.",
            "- The SongLens View header identifies how many songs are in the current view. Use Statistics to show every available distribution together or select one supported visible column. Individual fields can be viewed as a table or doughnut chart; small chart categories are grouped as Other.",
            "- When only one song is visible, Statistics compares that song with the full catalog. It shows the catalog count and percentage for each of the song's values; tempo uses a ten-BPM comparison range.",
            "- Use the Status and Genre dropdown columns to classify songs. Manage their choices under **Preferences > Statuses** and **Preferences > Genres**. Click the Rating stars to rate a song.",
            "- Click a column header for a quick single-column sort. Use Advanced Search for up to four sort levels; numbered arrows in the headers show their priority. Status follows your configured workflow order.",
            "- Use **View > Save Current View** to save the current layout and active Fast Search or Advanced Search under a name. You can optionally include the active date filter. Manage Grid Views lets you rename or delete saved views.",
            "- Choose **View > Saved Views** to restore one. Restore Default Layout returns to the built-in layout.",
            "- The Snapshot button creates a report from the selected song or current results.",
            "- In the Song column, ♪ indicates one mixdown and ♫ indicates multiple versions. Double-click the icon to play or choose a version.",
            "- When all search results live in the same folder, SongLens also focuses that folder in the tree.",
            "- Use the Refresh icon beside the SongLens View heading, or press F5, after files change on disk. It rebuilds the folder catalog and preserves the current folder, filter, or search view.",
            "",
            "Song Details Tabs",
            "- Summary: high-level metadata such as title, artist, dates, tempo, key, time signature, length, Studio Version, comment, and path.",
            "- Plug-ins: an alphabetical manifest of loaded native and third-party devices, showing name, category, subcategory, and preset type when available. Duplicate instances with matching device metadata are combined. Preset Type identifies the preset format, not the currently loaded preset name.",
            "- Date Created reflects the Windows file creation date, which can differ from the musical history of the song if the file was copied or restored.",
            "- Tracks: track number, a disabled-state icon when applicable, track name, instrument, and track notes when present in the song.",
            "- Groups: group or folder names and the track names assigned to each group.",
            "- Mixer: separate Main, Inserts, and Sends sections so you can review routing and plugin chains without opening Studio One.",
            "- Mixer Main shows the main channel insert chain split into Pre and Post where available.",
            "- View Full Mixer shows every channel in Studio One console order, including hidden channels, detailed inserts and sends, routing, VCA assignment, and channel state.",
            "- Use the leftmost Full Mixer column to collapse or expand channels routed through a folder track.",
            "- Notes: the song-level notes.txt text.",
            "- History: opens the History folder viewer for the selected song. Checkboxes explicitly identify the files selected for deletion.",
            "- History cleanup can keep the most recent revision automatically, and Review Deletion shows every retained file with its date before anything is deleted.",
            "- To open a saved revision, select it in History, enable Load filename, choose a compatible application, and select Load this backup version. Changing rows clears the launch checkbox.",
            "- Use **Tools > Preferences > Appearance > Select Visible Tabs** if you want to hide detail tabs you do not use often.",
            "- Use **Tools > Preferences...** if you want SongLens to stay on the current detail tab while you move between songs.",
            "",
            "Exports And Snapshots",
            "- **File > Export CSV** exports the current search results or your full library, depending on the current view.",
            "- Export CSV keeps one row per song and writes selected Mixer data to a companion mixer-details CSV.",
            "- **File > Export Excel Workbook** creates linked Catalog and Mixer Details worksheets in one .xlsx file.",
            "- Song links in the Catalog worksheet jump to that song's first row in Mixer Details.",
            "- CSV and Excel exports let you choose fields—including Disabled Tracks, Track Notes, and Song Notes—and save your preferred field set for later use.",
            "- **File > Save Snapshot** creates a musician-friendly song snapshot in Text or JSON format.",
            "- The Snapshot button at the right side of the SongLens View toolbar opens the same snapshot flow as **File > Save Snapshot...**",
            "- For multiple songs, List View captures the visible SongLens View columns, their display order, the current sort order, and every visible row.",
            "- For multiple songs, Detail View creates a full report for every song, with a clear separator between songs.",
            "- List View goes directly to the save dialog. Detail View opens Snapshot Preview so you can select sections and review the result before saving.",
            "- Detailed snapshots use the current Track View Options by default. Clear Use Current Track View Options to apply snapshot-only track filters without changing the main Track View settings.",
            "",
            "Recently Viewed",
            "- The binoculars icon beside Refresh opens or closes Recently Viewed, listing songs by filename only.",
            "- Automatic folder loading does not flood this list with first-song selections from larger folders.",
            "- If you intentionally open a folder that contains only one visible song, SongLens can add that song to Recently Viewed for convenience.",
            "",
            "Dates, Tempo, And Layout",
            "- Dates and times are shown using your Windows regional settings.",
            "- Tempo values are rounded for easier reading.",
            "- If you resize the main window, SongLens remembers that size for future sessions.",
            "- If you resize grid columns, SongLens remembers those widths for future sessions.",
            "- Use **View > Select Columns to Display** to choose which columns appear in the song list.",
            "- Drag SongLens View column headers horizontally to reorder them. SongLens remembers the order for future sessions.",
            "- Use **Tools > Preferences...** to tune the main UI, folder tree, grids, notes text, and dialogs independently.",
            "",
            "Themes And Settings",
            "- Use **Tools > Preferences...** to switch between Light and Dark themes.",
            "- Preferences is resizable, and SongLens remembers its size.",
            "- Preferences uses separate General, Audio, History, Statuses, Genres, and Appearance tabs.",
            "- Audio preferences let you name the folder used for mixdowns and use the Windows default audio player or a custom player.",
            "- History preferences let you choose how many of the most recent revisions the cleanup screen leaves unchecked.",
            "- SongLens remembers your songs folder, theme choice, saved grid layouts, visible detail tabs, main window size, and font size preferences automatically.",
            "- Preferences can restore the SongLens View that was active when the app closed, including its Quick Filter or Advanced Search.",
            "",
            "Searchable Fields",
            "- Filename",
            "- Title",
            "- Artist",
            "- Status",
            "- Genre",
            "- Rating",
            "- Year",
            "- Date created",
            "- Last modified",
            "- Tempo",
            "- Key",
            "- Time signature",
            "- Track count",
            "- Sample rate",
            "- Bit depth",
            "- Comment",
            "- Notes",
            "- Saved In",
            "- Path",
            "- Folder name",
            "- Track name",
            "- Instrument name",
            "- Track note",
            "",
            "Tips",
            "- Double-click a song row to perform the action selected in Preferences: reveal it in Windows Explorer or open it in the installed DAW.",
            "- If SongLens View shows multiple songs, click the exact song you want to add it to Recently Viewed.",
            "- If a song seems buried, use Expand All to reveal the full visible folder structure.",
            "- If a song does not appear, verify that it is a regular .song file and not an autosaved copy.",
            "- After changing your song library outside SongLens, use the Refresh icon beside the SongLens View heading.",
            "- If Fast Search text is no longer useful, clear it and search again from the start."
        ];
    }

    private static string[] BuildSongLensViewHelpLines()
    {
        return
        [
            "**SongLens View**",
            "The song grid shows the songs in the current folder, filtered catalog, or search results. The header shows the number of songs and the active view or filter.",
            "",
            "Header Controls",
            "- **Refresh (F5):**\tRebuilds the folder catalog and reloads the current folder, filter, or search results after changes on disk.",
            "- **View:**\tSave the current view, open saved views, manage grid views, restore the default layout, or choose columns. Use the binoculars icon for Recently Viewed.",
            "- **Filter:**\tShow the full catalog or filter by age in days or between Created or Modified dates. An active filter displays all matching songs together in SongLens View.",
            "- **Statistics:**\tSummarize the current songs with counts, percentages, distribution bars, and doughnut charts for individual fields. With one song in view, compare its values with the full catalog.",
            "- **Snapshot:**\tCreate a report for the selected song or current results.",
            "",
            "**Columns And Song Values**",
            "- Select Columns to Display \tto choose visible columns. Drag column headers to reorder them; drag a divider to resize a column. SongLens remembers the layout.",
            "- Click a column header to sort by that value. Click it again to reverse the sort direction.",
            "- Click a column header for a quick single-column sort. Use Advanced Search to sort by up to four visible columns; numbered arrows identify the priorities in the grid headers. Status uses the configured workflow order.",
            "- Status and Genre are editable dropdown columns. Manage their choices in **Preferences > Statuses** and **Preferences > Genres**.",
            "- Click the Rating stars to set a song’s rating. SongLens stores Status, Genre, and Rating separately from the Studio One song file.",
            "- The musical-note column indicates available mixdowns. Double-click ♪ to play one file. Double-click ♫ to choose among multiple versions. Right-click also offers Listen to This Song.",
            "",
            "Saved Views",
            "Save Current View stores the visible columns, order, widths, sort levels, and active Fast Search or Advanced Search. It can also include the active date filter. Manage Grid Views can rename or delete saved views. Choose one from **View > Saved Views** to restore it.",
            "Restore Default Layout returns to the built-in columns, order, widths, and song-name sort."
        ];
    }

    private static bool IsSectionTitle(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        if (line.StartsWith("-", StringComparison.Ordinal) || char.IsDigit(line[0]))
        {
            return false;
        }

        return true;
    }

    private void StyleButton(Button button)
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
