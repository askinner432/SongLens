namespace SongMetainfoBrowser.App;

internal enum SnapshotOptionsAction
{
    None,
    Preview,
    Save
}

internal sealed class SnapshotOptionsForm : Form
{
    private readonly AppTheme _theme;
    private readonly CheckBox _summaryCheckBox = new() { Text = "Summary", AutoSize = true };
    private readonly CheckBox _attributesCheckBox = new() { Text = "Attributes", AutoSize = true };
    private readonly CheckBox _tracksCheckBox = new() { Text = "Tracks", AutoSize = true };
    private readonly CheckBox _notesCheckBox = new() { Text = "Notes", AutoSize = true };
    private readonly RadioButton _textFormatRadioButton = new() { Text = "Text (.txt)", AutoSize = true };
    private readonly RadioButton _jsonFormatRadioButton = new() { Text = "JSON (.json)", AutoSize = true };

    public SnapshotSectionSelection SelectedSections { get; private set; } = new();
    public SnapshotOptionsAction RequestedAction { get; private set; }

    public SnapshotOptionsForm(SnapshotSectionSelection currentSelection, AppTheme theme)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        Text = "Save Snapshot";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(380, 300), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        _summaryCheckBox.Checked = currentSelection.IncludeSummary;
        _attributesCheckBox.Checked = currentSelection.IncludeAttributes;
        _tracksCheckBox.Checked = currentSelection.IncludeTracks;
        _notesCheckBox.Checked = currentSelection.IncludeNotes;
        _textFormatRadioButton.Checked = currentSelection.Format != SnapshotFormat.Json;
        _jsonFormatRadioButton.Checked = currentSelection.Format == SnapshotFormat.Json;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var introLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = "Include sections:",
            ForeColor = _theme.TextColor
        };
        layout.Controls.Add(introLabel, 0, 0);

        var sectionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0),
            BackColor = _theme.AppBackColor
        };
        StyleToggle(_summaryCheckBox);
        StyleToggle(_attributesCheckBox);
        StyleToggle(_tracksCheckBox);
        StyleToggle(_notesCheckBox);
        sectionsPanel.Controls.Add(_summaryCheckBox);
        sectionsPanel.Controls.Add(_attributesCheckBox);
        sectionsPanel.Controls.Add(_tracksCheckBox);
        sectionsPanel.Controls.Add(_notesCheckBox);
        layout.Controls.Add(sectionsPanel, 0, 1);

        var formatLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = "Snapshot format:",
            Margin = new Padding(0, 10, 0, 0),
            ForeColor = _theme.TextColor
        };
        layout.Controls.Add(formatLabel, 0, 2);

        var formatPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0),
            BackColor = _theme.AppBackColor
        };
        StyleToggle(_textFormatRadioButton);
        StyleToggle(_jsonFormatRadioButton);
        formatPanel.Controls.Add(_textFormatRadioButton);
        formatPanel.Controls.Add(_jsonFormatRadioButton);
        layout.Controls.Add(formatPanel, 0, 3);

        var buttonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };

        var previewButton = new Button { Text = "Preview...", AutoSize = true };
        previewButton.Click += (_, _) => Complete(SnapshotOptionsAction.Preview);

        var saveButton = new Button { Text = "Save", AutoSize = true };
        saveButton.Click += (_, _) => Complete(SnapshotOptionsAction.Save);

        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        StyleButton(previewButton, useAccent: false);
        StyleButton(saveButton, useAccent: true);
        StyleButton(cancelButton, useAccent: false);

        buttonPanel.Controls.Add(previewButton);
        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(cancelButton);
        layout.Controls.Add(buttonPanel, 0, 4);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void Complete(SnapshotOptionsAction action)
    {
        var selection = new SnapshotSectionSelection
        {
            IncludeSummary = _summaryCheckBox.Checked,
            IncludeAttributes = _attributesCheckBox.Checked,
            IncludeTracks = _tracksCheckBox.Checked,
            IncludeNotes = _notesCheckBox.Checked,
            Format = _jsonFormatRadioButton.Checked ? SnapshotFormat.Json : SnapshotFormat.Text
        };

        if (!selection.HasAnySection)
        {
            MessageBox.Show(this, "Select at least one section for the snapshot.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SelectedSections = selection;
        RequestedAction = action;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void StyleToggle(ButtonBase toggle)
    {
        toggle.ForeColor = _theme.TextColor;
        toggle.BackColor = _theme.AppBackColor;
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
