namespace SongMetainfoBrowser.App;

internal sealed class FontSettingsForm : Form
{
    private readonly AppTheme _theme;
    private readonly Dictionary<AppFontSection, ComboBox> _fontSizeComboBoxes = new();

    public AppFontPreferences SelectedPreferences { get; private set; }

    public FontSettingsForm(AppFontPreferences currentPreferences, AppTheme theme)
    {
        _theme = theme;
        SelectedPreferences = currentPreferences;
        var dialogFontSize = currentPreferences.Dialogs;

        Text = "Change Font Sizes";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(430, 340), dialogFontSize);
        Font = AppFontSettings.CreateUiFont(dialogFontSize);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(AppFontSettings.Scale(10, dialogFontSize)),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var introLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = "Choose font sizes for the main sections of SongLens. These settings are saved automatically when you click OK.",
            MaximumSize = new Size(AppFontSettings.Scale(395, dialogFontSize), 0),
            ForeColor = _theme.TextColor
        };
        layout.Controls.Add(introLabel, 0, 0);

        var optionsTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Margin = new Padding(0, AppFontSettings.Scale(10, dialogFontSize), 0, 0),
            BackColor = _theme.AppBackColor
        };
        optionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        optionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, AppFontSettings.Scale(64, dialogFontSize)));
        for (var row = 0; row < 6; row++)
        {
            optionsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        layout.Controls.Add(optionsTable, 0, 1);

        AddOptionRow(optionsTable, 0, "Main UI", AppFontSection.MainUi, currentPreferences.MainUi, dialogFontSize);
        AddOptionRow(optionsTable, 1, "Folder Tree", AppFontSection.FolderTree, currentPreferences.FolderTree, dialogFontSize);
        AddOptionRow(optionsTable, 2, "Song Grid", AppFontSection.SongGrid, currentPreferences.SongGrid, dialogFontSize);
        AddOptionRow(optionsTable, 3, "Detail Grids", AppFontSection.DetailGrids, currentPreferences.DetailGrids, dialogFontSize);
        AddOptionRow(optionsTable, 4, "Notes / Preview Text", AppFontSection.NotesAndPreviewText, currentPreferences.NotesAndPreviewText, dialogFontSize);
        AddOptionRow(optionsTable, 5, "Dialogs", AppFontSection.Dialogs, currentPreferences.Dialogs, dialogFontSize);

        var buttonLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, AppFontSettings.Scale(10, dialogFontSize), 0, 0),
            BackColor = _theme.AppBackColor
        };
        buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(buttonLayout, 0, 2);

        var leftButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };
        var resetButton = new Button { Text = "Reset to Defaults", AutoSize = true };
        resetButton.Click += (_, _) => ResetToDefaults();
        StyleButton(resetButton, useAccent: false);
        leftButtons.Controls.Add(resetButton);
        buttonLayout.Controls.Add(leftButtons, 0, 0);

        var rightButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };
        var okButton = new Button { Text = "OK", AutoSize = true };
        okButton.Click += (_, _) => Complete();
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        StyleButton(okButton, useAccent: true);
        StyleButton(cancelButton, useAccent: false);
        rightButtons.Controls.Add(okButton);
        rightButtons.Controls.Add(cancelButton);
        buttonLayout.Controls.Add(rightButtons, 1, 0);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void AddOptionRow(TableLayoutPanel optionsTable, int rowIndex, string labelText, AppFontSection section, int currentValue, int dialogFontSize)
    {
        var label = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = labelText,
            Margin = new Padding(0, AppFontSettings.Scale(5, dialogFontSize), AppFontSettings.Scale(8, dialogFontSize), 0),
            ForeColor = _theme.TextColor
        };

        var comboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            BackColor = _theme.PanelBackColor,
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 0, AppFontSettings.Scale(5, dialogFontSize))
        };
        for (var size = AppFontSettings.MinSizePoints; size <= AppFontSettings.MaxSizePoints; size++)
        {
            comboBox.Items.Add(size.ToString());
        }

        comboBox.SelectedItem = AppFontSettings.Normalize(currentValue).ToString();
        _fontSizeComboBoxes[section] = comboBox;

        optionsTable.Controls.Add(label, 0, rowIndex);
        optionsTable.Controls.Add(comboBox, 1, rowIndex);
    }

    private void ResetToDefaults()
    {
        var defaults = AppFontSettings.CreateDefaults();
        SetComboBoxValue(AppFontSection.MainUi, defaults.MainUi);
        SetComboBoxValue(AppFontSection.FolderTree, defaults.FolderTree);
        SetComboBoxValue(AppFontSection.SongGrid, defaults.SongGrid);
        SetComboBoxValue(AppFontSection.DetailGrids, defaults.DetailGrids);
        SetComboBoxValue(AppFontSection.NotesAndPreviewText, defaults.NotesAndPreviewText);
        SetComboBoxValue(AppFontSection.Dialogs, defaults.Dialogs);
    }

    private void SetComboBoxValue(AppFontSection section, int value)
    {
        _fontSizeComboBoxes[section].SelectedItem = AppFontSettings.Normalize(value).ToString();
    }

    private void Complete()
    {
        SelectedPreferences = new AppFontPreferences
        {
            MainUi = GetSelectedValue(AppFontSection.MainUi),
            FolderTree = GetSelectedValue(AppFontSection.FolderTree),
            SongGrid = GetSelectedValue(AppFontSection.SongGrid),
            DetailGrids = GetSelectedValue(AppFontSection.DetailGrids),
            NotesAndPreviewText = GetSelectedValue(AppFontSection.NotesAndPreviewText),
            Dialogs = GetSelectedValue(AppFontSection.Dialogs)
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private int GetSelectedValue(AppFontSection section)
    {
        var comboBox = _fontSizeComboBoxes[section];
        return comboBox.SelectedItem is not null && int.TryParse(comboBox.SelectedItem.ToString(), out var fontSizePoints)
            ? AppFontSettings.Normalize(fontSizePoints)
            : AppFontSettings.DefaultSizePoints;
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
