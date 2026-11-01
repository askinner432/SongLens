namespace SongMetainfoBrowser.App;

internal sealed class HistoryDeletionConfirmationForm : Form
{
    private readonly AppTheme _theme;

    public HistoryDeletionConfirmationForm(
        int deleteCount,
        IReadOnlyList<(string FileName, DateTime ModifiedDate)> keptFiles,
        AppTheme theme)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;

        Text = "Confirm History Cleanup";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(640, 420), fontPreferences, AppFontSection.Dialogs);
        MinimumSize = AppFontSettings.Scale(new Size(560, 340), fontPreferences, AppFontSection.Dialogs);
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

        var deleteLabel = new Label
        {
            AutoSize = true,
            Text = deleteCount == 1
                ? "1 history file will be permanently deleted."
                : $"{deleteCount} history files will be permanently deleted.",
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, 10)
        };
        layout.Controls.Add(deleteLabel, 0, 0);

        var keptLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        keptLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        keptLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(keptLayout, 0, 1);

        var keptLabel = new Label
        {
            AutoSize = true,
            Text = keptFiles.Count == 0
                ? "No history files will be kept."
                : keptFiles.Count == 1
                    ? "The following history file will be kept:"
                    : $"The following {keptFiles.Count} history files will be kept:",
            ForeColor = keptFiles.Count == 0 ? _theme.MutedTextColor : _theme.TextColor,
            Margin = new Padding(0, 0, 0, 6)
        };
        keptLayout.Controls.Add(keptLabel, 0, 0);

        var keptGrid = BuildKeptFilesGrid(fontPreferences);
        foreach (var keptFile in keptFiles)
        {
            keptGrid.Rows.Add(keptFile.FileName, DateTimeDisplay.Format(keptFile.ModifiedDate));
        }
        Shown += (_, _) => BeginInvoke(keptGrid.ClearSelection);
        keptLayout.Controls.Add(keptGrid, 0, 1);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 10, 0, 0),
            BackColor = _theme.AppBackColor
        };
        var cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };
        var deleteButton = new Button
        {
            Text = deleteCount == 1 ? "Delete 1 File" : $"Delete {deleteCount} Files",
            AutoSize = true,
            DialogResult = DialogResult.OK,
            Margin = new Padding(0, 0, 8, 0)
        };
        StyleButton(cancelButton, useAccent: false);
        StyleButton(deleteButton, useAccent: true);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(deleteButton);
        layout.Controls.Add(buttonPanel, 0, 2);

        AcceptButton = deleteButton;
        CancelButton = cancelButton;
    }

    private DataGridView BuildKeptFilesGrid(AppFontPreferences fontPreferences)
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            BackgroundColor = _theme.PanelBackColor,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = AppFontSettings.Scale(24, fontPreferences, AppFontSection.DetailGrids),
            EnableHeadersVisualStyles = false,
            GridColor = _theme.BorderColor,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.DetailGrids)
        };
        grid.ColumnHeadersDefaultCellStyle.BackColor = _theme.HeaderBackColor;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = _theme.TextColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = _theme.HeaderBackColor;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = _theme.TextColor;
        grid.DefaultCellStyle.BackColor = _theme.PanelBackColor;
        grid.DefaultCellStyle.ForeColor = _theme.TextColor;
        grid.DefaultCellStyle.SelectionBackColor = _theme.PanelBackColor;
        grid.DefaultCellStyle.SelectionForeColor = _theme.TextColor;
        grid.AlternatingRowsDefaultCellStyle.BackColor = _theme.PanelAltBackColor;
        grid.RowTemplate.Height = AppFontSettings.Scale(22, fontPreferences, AppFontSection.DetailGrids);
        grid.Columns.Add("FileName", "Filename");
        grid.Columns.Add("ModifiedDate", "Modified Date");
        grid.Columns["FileName"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        grid.Columns["ModifiedDate"]!.Width = AppFontSettings.Scale(190, fontPreferences, AppFontSection.DetailGrids);
        grid.Columns["ModifiedDate"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        grid.ClearSelection();
        return grid;
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
