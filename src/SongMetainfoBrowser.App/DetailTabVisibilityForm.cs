namespace SongMetainfoBrowser.App;

internal sealed class DetailTabVisibilityForm : Form
{
    private readonly AppTheme _theme;
    private readonly IReadOnlyList<(string Key, string Label)> _tabs;
    private readonly Dictionary<string, CheckBox> _checkBoxesByKey = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> SelectedTabKeys { get; private set; } = Array.Empty<string>();

    public DetailTabVisibilityForm(
        IReadOnlyList<(string Key, string Label)> tabs,
        IReadOnlyCollection<string> selectedTabKeys,
        AppTheme theme)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        _tabs = tabs;
        Text = "Visible Tabs";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(380, 356), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        var selectedKeys = new HashSet<string>(selectedTabKeys, StringComparer.OrdinalIgnoreCase);

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

        var introLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = "Show detail tabs:",
            ForeColor = _theme.TextColor
        };
        layout.Controls.Add(introLabel, 0, 0);

        var tabsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0),
            BackColor = _theme.AppBackColor
        };

        foreach (var tab in _tabs)
        {
            var checkBox = new CheckBox
            {
                Text = tab.Label,
                AutoSize = true,
                Checked = selectedKeys.Contains(tab.Key)
            };
            StyleToggle(checkBox);
            _checkBoxesByKey[tab.Key] = checkBox;
            tabsPanel.Controls.Add(checkBox);
        }

        layout.Controls.Add(tabsPanel, 0, 1);

        var buttonPanel = new FlowLayoutPanel
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

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        layout.Controls.Add(buttonPanel, 0, 2);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void Complete()
    {
        var selectedKeys = _tabs
            .Where(tab => _checkBoxesByKey.TryGetValue(tab.Key, out var checkBox) && checkBox.Checked)
            .Select(tab => tab.Key)
            .ToArray();

        if (selectedKeys.Length == 0)
        {
            MessageBox.Show(this, "Select at least one visible tab.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SelectedTabKeys = selectedKeys;
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
