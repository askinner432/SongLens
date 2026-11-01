namespace SongMetainfoBrowser.App;

internal sealed class MixdownSelectionForm : Form
{
    private readonly AppTheme _theme;
    private readonly DataGridView _mixdownGrid = new();
    private readonly Button _playButton = new() { Text = "Play This Version", AutoSize = true };

    public string? SelectedAudioPath { get; private set; }

    public MixdownSelectionForm(string songName, IReadOnlyList<string> audioPaths, AppTheme theme)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;

        Text = $"Choose Mixdown — {songName}";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        MinimumSize = SizeFromClientSize(AppFontSettings.Scale(new Size(560, 280), fontPreferences, AppFontSection.Dialogs));
        ClientSize = AppFontSettings.Scale(new Size(680, 340), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = "Double-click a version to play it, or select one and click Play This Version.",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);

        ConfigureGrid();
        foreach (var audioPath in audioPaths)
        {
            var rowIndex = _mixdownGrid.Rows.Add(
                Path.GetFileNameWithoutExtension(audioPath),
                MixdownAudioPlayback.GetFormatLabel(audioPath),
                File.GetLastWriteTime(audioPath));
            _mixdownGrid.Rows[rowIndex].Tag = audioPath;
        }

        layout.Controls.Add(_mixdownGrid, 0, 1);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 10, 0, 0),
            BackColor = _theme.AppBackColor
        };
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        _playButton.Click += (_, _) => ConfirmSelection();
        StyleButton(_playButton, useAccent: true);
        StyleButton(cancelButton, useAccent: false);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(_playButton);
        layout.Controls.Add(buttonPanel, 0, 2);

        _mixdownGrid.SelectionChanged += (_, _) => UpdatePlayButton();
        _mixdownGrid.CellDoubleClick += (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                ConfirmSelection();
            }
        };

        AcceptButton = _playButton;
        CancelButton = cancelButton;
        Shown += (_, _) =>
        {
            if (_mixdownGrid.Rows.Count > 0)
            {
                _mixdownGrid.Rows[0].Selected = true;
                _mixdownGrid.CurrentCell = _mixdownGrid.Rows[0].Cells[0];
            }
            UpdatePlayButton();
        };
    }

    private void ConfigureGrid()
    {
        _mixdownGrid.Dock = DockStyle.Fill;
        _mixdownGrid.AllowUserToAddRows = false;
        _mixdownGrid.AllowUserToDeleteRows = false;
        _mixdownGrid.AllowUserToResizeRows = false;
        _mixdownGrid.AutoGenerateColumns = false;
        _mixdownGrid.BackgroundColor = _theme.PanelBackColor;
        _mixdownGrid.BorderStyle = BorderStyle.FixedSingle;
        _mixdownGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _mixdownGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _mixdownGrid.EnableHeadersVisualStyles = false;
        _mixdownGrid.GridColor = _theme.BorderColor;
        _mixdownGrid.MultiSelect = false;
        _mixdownGrid.ReadOnly = true;
        _mixdownGrid.RowHeadersVisible = false;
        _mixdownGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _mixdownGrid.ColumnHeadersDefaultCellStyle.BackColor = _theme.HeaderBackColor;
        _mixdownGrid.ColumnHeadersDefaultCellStyle.ForeColor = _theme.TextColor;
        _mixdownGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _theme.HeaderBackColor;
        _mixdownGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _theme.TextColor;
        _mixdownGrid.DefaultCellStyle.BackColor = _theme.PanelBackColor;
        _mixdownGrid.DefaultCellStyle.ForeColor = _theme.TextColor;
        _mixdownGrid.DefaultCellStyle.SelectionBackColor = _theme.TreeSelectionColor;
        _mixdownGrid.DefaultCellStyle.SelectionForeColor = _theme.SelectedTextColor;
        _mixdownGrid.AlternatingRowsDefaultCellStyle.BackColor = _theme.PanelAltBackColor;

        _mixdownGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Version",
            HeaderText = "Version",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 220,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _mixdownGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Format",
            HeaderText = "Format",
            Width = 80,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _mixdownGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Modified",
            HeaderText = "Modified",
            Width = 155,
            ValueType = typeof(DateTime),
            DefaultCellStyle = new DataGridViewCellStyle { Format = "g" },
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void UpdatePlayButton()
    {
        _playButton.Enabled = _mixdownGrid.SelectedRows.Count > 0;
    }

    private void ConfirmSelection()
    {
        if (_mixdownGrid.SelectedRows.Count == 0
            || _mixdownGrid.SelectedRows[0].Tag is not string audioPath)
        {
            return;
        }

        SelectedAudioPath = audioPath;
        DialogResult = DialogResult.OK;
        Close();
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
