namespace SongMetainfoBrowser.App;

internal sealed class SnapshotPreviewForm : Form
{
    private readonly AppTheme _theme;
    private readonly TabControl _tabControl = new();
    private readonly TextBox _textPreviewTextBox = new();
    private readonly TextBox _jsonPreviewTextBox = new();

    public SnapshotFormat? SaveRequestedFormat { get; private set; }

    public SnapshotPreviewForm(string previewText, string previewJson, SnapshotFormat initialFormat, AppTheme theme)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        Text = "Snapshot Preview";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = AppFontSettings.Scale(new Size(720, 520), fontPreferences, AppFontSection.Dialogs);
        Size = AppFontSettings.Scale(new Size(860, 640), fontPreferences, AppFontSection.Dialogs);
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

        _tabControl.Dock = DockStyle.Fill;
        _tabControl.Appearance = TabAppearance.Normal;
        var textTab = new TabPage("Text");
        var jsonTab = new TabPage("JSON");
        textTab.BackColor = _theme.PanelBackColor;
        textTab.ForeColor = _theme.TextColor;
        jsonTab.BackColor = _theme.PanelBackColor;
        jsonTab.ForeColor = _theme.TextColor;
        ConfigurePreviewTextBox(_textPreviewTextBox, previewText, _theme, fontPreferences.NotesAndPreviewText);
        ConfigurePreviewTextBox(_jsonPreviewTextBox, previewJson, _theme, fontPreferences.NotesAndPreviewText);
        textTab.Controls.Add(_textPreviewTextBox);
        jsonTab.Controls.Add(_jsonPreviewTextBox);
        _tabControl.TabPages.Add(textTab);
        _tabControl.TabPages.Add(jsonTab);
        _tabControl.SelectedIndex = initialFormat == SnapshotFormat.Json ? 1 : 0;
        layout.Controls.Add(_tabControl, 0, 0);

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

        var leftPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };
        var copyButton = new Button { Text = "Copy", AutoSize = true };
        copyButton.Click += (_, _) => Clipboard.SetText(GetActivePreviewText());
        StyleButton(copyButton, useAccent: false);
        leftPanel.Controls.Add(copyButton);
        buttonPanel.Controls.Add(leftPanel, 0, 0);

        var rightPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };
        var saveTextButton = new Button { Text = "Save Text", AutoSize = true };
        saveTextButton.Click += (_, _) => RequestSave(SnapshotFormat.Text);
        var saveJsonButton = new Button { Text = "Save JSON", AutoSize = true };
        saveJsonButton.Click += (_, _) => RequestSave(SnapshotFormat.Json);
        var closeButton = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.Cancel };
        StyleButton(saveTextButton, useAccent: true);
        StyleButton(saveJsonButton, useAccent: true);
        StyleButton(closeButton, useAccent: false);
        rightPanel.Controls.Add(saveTextButton);
        rightPanel.Controls.Add(saveJsonButton);
        rightPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(rightPanel, 1, 0);

        AcceptButton = initialFormat == SnapshotFormat.Json ? saveJsonButton : saveTextButton;
        CancelButton = closeButton;
    }

    private static void ConfigurePreviewTextBox(TextBox textBox, string text, AppTheme theme, int fontSizePoints)
    {
        textBox.Dock = DockStyle.Fill;
        textBox.Multiline = true;
        textBox.ReadOnly = true;
        textBox.ScrollBars = ScrollBars.Vertical;
        textBox.WordWrap = false;
        textBox.Font = AppFontSettings.CreateMonospaceFont(fontSizePoints);
        textBox.BackColor = theme.PanelBackColor;
        textBox.ForeColor = theme.TextColor;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Text = text;
    }

    private string GetActivePreviewText()
    {
        return _tabControl.SelectedIndex == 1 ? _jsonPreviewTextBox.Text : _textPreviewTextBox.Text;
    }

    private void RequestSave(SnapshotFormat format)
    {
        SaveRequestedFormat = format;
        DialogResult = DialogResult.OK;
        Close();
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
