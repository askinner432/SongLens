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
    private readonly CheckBox _pluginsCheckBox = new() { Text = "Plug-ins", AutoSize = true };
    private readonly CheckBox _tracksCheckBox = new() { Text = "Tracks", AutoSize = true };
    private readonly CheckBox _groupsCheckBox = new() { Text = "Groups", AutoSize = true };
    private readonly CheckBox _mixerCheckBox = new() { Text = "Mixer", AutoSize = true };
    private readonly CheckBox _notesCheckBox = new() { Text = "Notes", AutoSize = true };
    private readonly CheckBox _useCurrentTrackViewOptionsCheckBox = new()
    {
        Text = "Use Current Track View Options",
        AutoSize = true
    };
    private readonly TransparentCheckBox _withEventsCheckBox = new() { Text = "With Events", AutoSize = true };
    private readonly TransparentCheckBox _hideDisabledCheckBox = new() { Text = "Hide Disabled", AutoSize = true };
    private readonly TransparentCheckBox _hideFoldersCheckBox = new() { Text = "Hide Folders", AutoSize = true };
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
        ClientSize = AppFontSettings.Scale(new Size(380, 356), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        _summaryCheckBox.Checked = currentSelection.IncludeSummary;
        _pluginsCheckBox.Checked = currentSelection.IncludePlugins;
        _tracksCheckBox.Checked = currentSelection.IncludeTracks;
        _groupsCheckBox.Checked = currentSelection.IncludeGroups;
        _mixerCheckBox.Checked = currentSelection.IncludeMixer;
        _notesCheckBox.Checked = currentSelection.IncludeNotes;
        _useCurrentTrackViewOptionsCheckBox.Checked = currentSelection.UseCurrentTrackViewOptions;
        _withEventsCheckBox.Checked = currentSelection.ShowTracksWithEventsOnly;
        _hideDisabledCheckBox.Checked = currentSelection.HideDisabledTracks;
        _hideFoldersCheckBox.Checked = currentSelection.HideFolderTracks;
        _textFormatRadioButton.Checked = currentSelection.Format != SnapshotFormat.Json;
        _jsonFormatRadioButton.Checked = currentSelection.Format == SnapshotFormat.Json;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
        StyleToggle(_pluginsCheckBox);
        StyleToggle(_tracksCheckBox);
        StyleToggle(_groupsCheckBox);
        StyleToggle(_mixerCheckBox);
        StyleToggle(_notesCheckBox);
        sectionsPanel.Controls.Add(_summaryCheckBox);
        sectionsPanel.Controls.Add(_pluginsCheckBox);
        sectionsPanel.Controls.Add(_tracksCheckBox);
        sectionsPanel.Controls.Add(_groupsCheckBox);
        sectionsPanel.Controls.Add(_mixerCheckBox);
        sectionsPanel.Controls.Add(_notesCheckBox);
        layout.Controls.Add(sectionsPanel, 0, 1);

        StyleToggle(_useCurrentTrackViewOptionsCheckBox);
        _useCurrentTrackViewOptionsCheckBox.Margin = new Padding(0, 10, 0, 0);
        _useCurrentTrackViewOptionsCheckBox.CheckedChanged += (_, _) => UpdateTrackViewOptionsEnabledState();
        layout.Controls.Add(_useCurrentTrackViewOptionsCheckBox, 0, 2);

        var trackViewOptionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0),
            BackColor = _theme.AppBackColor
        };
        StyleToggle(_withEventsCheckBox);
        StyleToggle(_hideDisabledCheckBox);
        StyleToggle(_hideFoldersCheckBox);
        _withEventsCheckBox.Margin = new Padding(0, 0, 14, 0);
        _hideDisabledCheckBox.Margin = new Padding(0, 0, 14, 0);
        _hideFoldersCheckBox.Margin = Padding.Empty;
        trackViewOptionsPanel.Controls.Add(_withEventsCheckBox);
        trackViewOptionsPanel.Controls.Add(_hideDisabledCheckBox);
        trackViewOptionsPanel.Controls.Add(_hideFoldersCheckBox);
        layout.Controls.Add(trackViewOptionsPanel, 0, 3);
        UpdateTrackViewOptionsEnabledState();

        var formatLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = "Snapshot format:",
            Margin = new Padding(0, 10, 0, 0),
            ForeColor = _theme.TextColor
        };
        layout.Controls.Add(formatLabel, 0, 4);

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
        layout.Controls.Add(formatPanel, 0, 5);

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
        layout.Controls.Add(buttonPanel, 0, 6);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void Complete(SnapshotOptionsAction action)
    {
        var selection = new SnapshotSectionSelection
        {
            IncludeSummary = _summaryCheckBox.Checked,
            IncludePlugins = _pluginsCheckBox.Checked,
            IncludeTracks = _tracksCheckBox.Checked,
            IncludeGroups = _groupsCheckBox.Checked,
            IncludeMixer = _mixerCheckBox.Checked,
            IncludePresets = false,
            IncludeNotes = _notesCheckBox.Checked,
            UseCurrentTrackViewOptions = _useCurrentTrackViewOptionsCheckBox.Checked,
            ShowTracksWithEventsOnly = _withEventsCheckBox.Checked,
            HideDisabledTracks = _hideDisabledCheckBox.Checked,
            HideFolderTracks = _hideFoldersCheckBox.Checked,
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

    private void UpdateTrackViewOptionsEnabledState()
    {
        var enabled = !_useCurrentTrackViewOptionsCheckBox.Checked;
        _withEventsCheckBox.Enabled = enabled;
        _hideDisabledCheckBox.Enabled = enabled;
        _hideFoldersCheckBox.Enabled = enabled;
        var optionTextColor = enabled ? _theme.TextColor : _theme.MutedTextColor;
        _withEventsCheckBox.ForeColor = optionTextColor;
        _hideDisabledCheckBox.ForeColor = optionTextColor;
        _hideFoldersCheckBox.ForeColor = optionTextColor;
    }

    private void StyleToggle(Control toggle)
    {
        toggle.ForeColor = _theme.TextColor;
        toggle.BackColor = _theme.AppBackColor;
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
