using System.Text;

namespace SongMetainfoBrowser.App;

/// <summary>
/// Read-only view of every channel in a song's mixer.
/// </summary>
internal sealed class FullMixerForm : Form
{
    private readonly AppTheme _theme;
    private readonly DataGridView _mixerGrid = new();
    private readonly HashSet<FullMixerChannelInfo> _folderChannels = [];
    private readonly HashSet<FullMixerChannelInfo> _collapsedFolderChannels = [];
    private readonly Dictionary<FullMixerChannelInfo, FullMixerChannelInfo> _parentFolderByChannel = [];
    private const string FolderToggleColumnName = "FolderToggle";
    private const string ChannelColumnName = "Channel";

    public FullMixerForm(SongMetadata metadata, AppTheme theme)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        Text = $"Full Mixer — {Path.GetFileNameWithoutExtension(metadata.FileName)}";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = AppFontSettings.Scale(new Size(900, 540), fontPreferences, AppFontSection.Dialogs);
        Size = AppFontSettings.Scale(new Size(1400, 750), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        BuildFolderHierarchy(metadata);
        ConfigureMixerGrid(fontPreferences);
        PopulateMixerGrid(metadata.FullMixerChannels);
        layout.Controls.Add(_mixerGrid, 0, 0);

        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 0)
        };
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(buttonPanel, 0, 1);

        var hiddenChannelCount = metadata.FullMixerChannels.Count(channel => channel.IsVisible == false);
        var countLabel = new Label
        {
            Text = hiddenChannelCount == 0
                ? $"{metadata.FullMixerChannels.Count} channel(s)"
                : $"{metadata.FullMixerChannels.Count} channel(s), {hiddenChannelCount} hidden in Studio One",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 6, 0, 0)
        };
        buttonPanel.Controls.Add(countLabel, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };
        var copyButton = new Button { Text = "Copy All", AutoSize = true };
        copyButton.Click += (_, _) => Clipboard.SetText(BuildClipboardText(metadata.FullMixerChannels));
        var closeButton = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.Cancel };
        StyleButton(copyButton);
        StyleButton(closeButton);
        buttons.Controls.Add(copyButton);
        buttons.Controls.Add(closeButton);
        buttonPanel.Controls.Add(buttons, 1, 0);

        CancelButton = closeButton;
    }

    private void ConfigureMixerGrid(AppFontPreferences fontPreferences)
    {
        _mixerGrid.Dock = DockStyle.Fill;
        _mixerGrid.ReadOnly = true;
        _mixerGrid.AllowUserToAddRows = false;
        _mixerGrid.AllowUserToDeleteRows = false;
        _mixerGrid.AllowUserToResizeRows = false;
        _mixerGrid.AutoGenerateColumns = false;
        _mixerGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _mixerGrid.BackgroundColor = _theme.PanelBackColor;
        _mixerGrid.BorderStyle = BorderStyle.FixedSingle;
        _mixerGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _mixerGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _mixerGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _mixerGrid.ColumnHeadersHeight = AppFontSettings.Scale(26, fontPreferences, AppFontSection.DetailGrids);
        _mixerGrid.EnableHeadersVisualStyles = false;
        _mixerGrid.Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.DetailGrids);
        _mixerGrid.GridColor = _theme.BorderColor;
        _mixerGrid.MultiSelect = true;
        _mixerGrid.RowHeadersVisible = false;
        _mixerGrid.RowTemplate.Height = AppFontSettings.Scale(24, fontPreferences, AppFontSection.DetailGrids);
        _mixerGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _mixerGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;

        _mixerGrid.ColumnHeadersDefaultCellStyle.BackColor = _theme.HeaderBackColor;
        _mixerGrid.ColumnHeadersDefaultCellStyle.ForeColor = _theme.TextColor;
        _mixerGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _theme.HeaderBackColor;
        _mixerGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _theme.TextColor;
        _mixerGrid.DefaultCellStyle.BackColor = _theme.PanelBackColor;
        _mixerGrid.DefaultCellStyle.ForeColor = _theme.TextColor;
        _mixerGrid.DefaultCellStyle.SelectionBackColor = _theme.TreeSelectionColor;
        _mixerGrid.DefaultCellStyle.SelectionForeColor = _theme.SelectedTextColor;
        _mixerGrid.AlternatingRowsDefaultCellStyle.BackColor = _theme.PanelAltBackColor;
        _mixerGrid.CellClick += MixerGridCellClick;

        AddFolderToggleColumn();
        AddColumn("#", 46, DataGridViewContentAlignment.MiddleRight);
        AddColumn("Type", 90);
        AddColumn(ChannelColumnName, 180);
        AddColumn("Visible", 76);
        AddColumn("Format", 86);
        AddColumn("Input", 130);
        AddColumn("Volume", 84, DataGridViewContentAlignment.MiddleRight);
        AddColumn("Pan", 64);
        AddColumn("Destination", 140);
        AddColumn("VCA", 110);
        AddColumn("Groups", 100);
        AddColumn("Input FX", 240);
        AddColumn("Pre Inserts", 280);
        AddColumn("Post Inserts", 280);
        AddColumn("Sends", 360);
        AddColumn("State", 130);
    }

    private void AddFolderToggleColumn()
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = "",
            Name = FolderToggleColumnName,
            Width = 34,
            MinimumWidth = 34,
            Frozen = true,
            Resizable = DataGridViewTriState.False,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            ToolTipText = "Expand or collapse folder tracks"
        };
        column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        column.DefaultCellStyle.Font = new Font(_mixerGrid.Font, FontStyle.Bold);
        _mixerGrid.Columns.Add(column);
    }

    private void AddColumn(string header, int width, DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            Name = header,
            Width = width,
            MinimumWidth = 40,
            SortMode = DataGridViewColumnSortMode.Automatic
        };
        column.DefaultCellStyle.Alignment = alignment;
        _mixerGrid.Columns.Add(column);
    }

    private void BuildFolderHierarchy(SongMetadata metadata)
    {
        var folderTrackNames = metadata.TrackInstruments
            .Where(track => track.IsFolderTrack)
            .Select(track => track.TrackName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        var candidateFolders = metadata.FullMixerChannels
            .Where(channel => folderTrackNames.Contains(channel.ChannelName))
            .ToArray();

        foreach (var channel in metadata.FullMixerChannels)
        {
            if (string.IsNullOrWhiteSpace(channel.DestinationName))
            {
                continue;
            }

            var parent = candidateFolders.FirstOrDefault(folder =>
                !ReferenceEquals(folder, channel)
                && string.Equals(folder.ChannelName, channel.DestinationName, StringComparison.CurrentCultureIgnoreCase));
            if (parent is not null)
            {
                _parentFolderByChannel[channel] = parent;
                _folderChannels.Add(parent);
            }
        }
    }

    private int GetHierarchyDepth(FullMixerChannelInfo channel)
    {
        var depth = 0;
        var visited = new HashSet<FullMixerChannelInfo>();
        while (_parentFolderByChannel.TryGetValue(channel, out var parent) && visited.Add(parent))
        {
            depth++;
            channel = parent;
        }

        return depth;
    }

    private void MixerGridCellClick(object? sender, DataGridViewCellEventArgs args)
    {
        if (args.RowIndex < 0
            || args.ColumnIndex != _mixerGrid.Columns[FolderToggleColumnName]!.Index
            || _mixerGrid.Rows[args.RowIndex].Tag is not FullMixerChannelInfo channel
            || !_folderChannels.Contains(channel))
        {
            return;
        }

        if (!_collapsedFolderChannels.Add(channel))
        {
            _collapsedFolderChannels.Remove(channel);
        }

        ApplyFolderVisibility();
    }

    private void ApplyFolderVisibility()
    {
        _mixerGrid.CurrentCell = null;
        foreach (DataGridViewRow row in _mixerGrid.Rows)
        {
            if (row.Tag is not FullMixerChannelInfo channel)
            {
                continue;
            }

            row.Visible = !HasCollapsedAncestor(channel);
            if (_folderChannels.Contains(channel))
            {
                var isCollapsed = _collapsedFolderChannels.Contains(channel);
                row.Cells[FolderToggleColumnName].Value = isCollapsed ? "+" : "−";
                row.Cells[FolderToggleColumnName].ToolTipText = isCollapsed
                    ? "Expand folder track"
                    : "Collapse folder track";
            }
        }
    }

    private bool HasCollapsedAncestor(FullMixerChannelInfo channel)
    {
        var visited = new HashSet<FullMixerChannelInfo>();
        while (_parentFolderByChannel.TryGetValue(channel, out var parent) && visited.Add(parent))
        {
            if (_collapsedFolderChannels.Contains(parent))
            {
                return true;
            }

            channel = parent;
        }

        return false;
    }

    private void PopulateMixerGrid(IReadOnlyList<FullMixerChannelInfo> channels)
    {
        foreach (var channel in channels.OrderBy(channel => channel.DisplayOrder))
        {
            var isFolder = _folderChannels.Contains(channel);
            var rowIndex = _mixerGrid.Rows.Add(
                isFolder ? "−" : "",
                channel.DisplayOrder,
                channel.ChannelType,
                channel.ChannelName,
                FormatVisibility(channel.IsVisible),
                FormatValue(channel.SpeakerType),
                FormatValue(channel.InputName),
                FormatVolume(channel.VolumeGain),
                FormatPan(channel.PanPosition),
                FormatValue(channel.DestinationName),
                FormatValue(channel.VcaTargetName),
                FormatValue(channel.GroupIds),
                FormatInserts(channel.Inserts, "Input"),
                FormatInserts(channel.Inserts, "Pre"),
                FormatInserts(channel.Inserts, "Post"),
                FormatSends(channel.Sends),
                FormatChannelState(channel));
            var row = _mixerGrid.Rows[rowIndex];
            row.Tag = channel;
            row.Cells[FolderToggleColumnName].ToolTipText = isFolder
                ? "Collapse folder track"
                : "";
            row.Cells[ChannelColumnName].Style.Padding = new Padding(GetHierarchyDepth(channel) * 16, 0, 0, 0);
        }

        _mixerGrid.ClearSelection();
    }

    private static string BuildClipboardText(IReadOnlyList<FullMixerChannelInfo> channels)
    {
        var builder = new StringBuilder();
        builder.AppendLine("#\tType\tChannel\tVisible\tFormat\tInput\tVolume\tPan\tDestination\tVCA\tGroups\tInput FX\tPre Inserts\tPost Inserts\tSends\tState");
        foreach (var channel in channels.OrderBy(channel => channel.DisplayOrder))
        {
            builder.Append(channel.DisplayOrder).Append('\t')
                .Append(channel.ChannelType).Append('\t')
                .Append(channel.ChannelName).Append('\t')
                .Append(FormatVisibility(channel.IsVisible)).Append('\t')
                .Append(FormatValue(channel.SpeakerType)).Append('\t')
                .Append(FormatValue(channel.InputName)).Append('\t')
                .Append(FormatVolume(channel.VolumeGain)).Append('\t')
                .Append(FormatPan(channel.PanPosition)).Append('\t')
                .Append(FormatValue(channel.DestinationName)).Append('\t')
                .Append(FormatValue(channel.VcaTargetName)).Append('\t')
                .Append(FormatValue(channel.GroupIds)).Append('\t')
                .Append(FormatInserts(channel.Inserts, "Input")).Append('\t')
                .Append(FormatInserts(channel.Inserts, "Pre")).Append('\t')
                .Append(FormatInserts(channel.Inserts, "Post")).Append('\t')
                .Append(FormatSends(channel.Sends)).Append('\t')
                .AppendLine(FormatChannelState(channel));
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatVolume(double? gain)
    {
        if (gain is null)
        {
            return "—";
        }

        if (gain <= 0)
        {
            return "−∞ dB";
        }

        var decibels = 20 * Math.Log10(gain.Value);
        return Math.Abs(decibels) < 0.05
            ? "0.0 dB"
            : $"{decibels:0.0} dB";
    }

    private static string FormatPan(double? pan)
    {
        if (pan is null)
        {
            return "—";
        }

        var offset = (pan.Value - 0.5) * 200;
        if (Math.Abs(offset) < 0.5)
        {
            return "Center";
        }

        return offset < 0
            ? $"{Math.Abs(offset):0}% L"
            : $"{offset:0}% R";
    }

    private static string FormatValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "—" : value;
    }

    private static string FormatVisibility(bool? isVisible)
    {
        return isVisible switch
        {
            true => "Shown",
            false => "Hidden",
            null => "—"
        };
    }

    private static string FormatInserts(IReadOnlyList<FullMixerInsertInfo> inserts, string stage)
    {
        var values = inserts
            .Where(insert => string.Equals(insert.Stage, stage, StringComparison.Ordinal))
            .Select(insert =>
            {
                var plugin = string.IsNullOrWhiteSpace(insert.PluginName) ? "Empty" : insert.PluginName;
                var preset = !string.IsNullOrWhiteSpace(insert.PresetName)
                    ? insert.PresetName
                    : string.IsNullOrWhiteSpace(insert.PresetPath)
                        ? null
                        : Path.GetFileNameWithoutExtension(insert.PresetPath);
                var details = string.IsNullOrWhiteSpace(preset) ? plugin : $"{plugin} ({preset})";
                return insert.IsBypassed
                    ? $"{insert.SlotName}: {details} [Bypassed]"
                    : $"{insert.SlotName}: {details}";
            })
            .ToArray();

        return values.Length == 0 ? "—" : string.Join(" | ", values);
    }

    private static string FormatSends(IReadOnlyList<FullMixerSendInfo> sends)
    {
        if (sends.Count == 0)
        {
            return "—";
        }

        return string.Join(" | ", sends.Select(send =>
        {
            var details = new List<string>
            {
                $"{send.SlotName} to {FormatValue(send.DestinationName)}",
                FormatVolume(send.LevelGain),
                FormatPan(send.PanPosition),
                send.IsPreFader ? "Pre" : "Post"
            };
            if (send.IsBypassed)
            {
                details.Add("Bypassed");
            }

            return string.Join(", ", details);
        }));
    }

    private static string FormatChannelState(FullMixerChannelInfo channel)
    {
        var states = new List<string>();
        if (channel.IsMuted)
        {
            states.Add("Muted");
        }

        if (channel.IsSoloed)
        {
            states.Add("Soloed");
        }

        if (channel.IsSoloSafe)
        {
            states.Add("Solo Safe");
        }

        if (channel.IsDisabled)
        {
            states.Add("Disabled");
        }

        if (channel.IsExpanded == true)
        {
            states.Add("Expanded");
        }

        return states.Count == 0 ? "Active" : string.Join(", ", states);
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
