namespace SongMetainfoBrowser.App;

internal sealed class SongGridColumnsForm : Form
{
    private readonly AppTheme _theme;
    private readonly CheckedListBox _fieldList = new();
    private readonly List<SongGridColumnField> _fields;

    public IReadOnlyList<SongGridColumnField> SelectedFields { get; private set; } = Array.Empty<SongGridColumnField>();

    public SongGridColumnsForm(IReadOnlyList<SongGridColumnField> fields, AppTheme theme, IReadOnlyCollection<string>? savedFieldKeys = null)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        _fields = fields.ToList();
        var selectedKeys = savedFieldKeys is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(savedFieldKeys, StringComparer.OrdinalIgnoreCase);

        Text = "Song Grid Columns";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(420, 520), fontPreferences, AppFontSection.Dialogs);
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

        var introLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = "Choose which columns to show in the song grid.",
            ForeColor = _theme.TextColor
        };
        layout.Controls.Add(introLabel, 0, 0);

        _fieldList.Dock = DockStyle.Fill;
        _fieldList.CheckOnClick = true;
        _fieldList.BorderStyle = BorderStyle.FixedSingle;
        _fieldList.BackColor = _theme.PanelBackColor;
        _fieldList.ForeColor = _theme.TextColor;
        foreach (var field in _fields)
        {
            var isChecked = selectedKeys.Count > 0 ? selectedKeys.Contains(field.Key) : field.IsDefault;
            var index = _fieldList.Items.Add(field.Label, isChecked);
            _fieldList.SetItemChecked(index, isChecked);
        }
        layout.Controls.Add(_fieldList, 0, 1);

        var bottomLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 0),
            BackColor = _theme.AppBackColor
        };
        bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(bottomLayout, 0, 2);

        var leftPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };
        var selectAllButton = new Button { Text = "Select All", AutoSize = true };
        selectAllButton.Click += (_, _) => SetAllFieldsChecked(true);
        var defaultsButton = new Button { Text = "Defaults", AutoSize = true };
        defaultsButton.Click += (_, _) => RestoreDefaults();
        StyleButton(selectAllButton, useAccent: false);
        StyleButton(defaultsButton, useAccent: false);
        leftPanel.Controls.Add(selectAllButton);
        leftPanel.Controls.Add(defaultsButton);
        bottomLayout.Controls.Add(leftPanel, 0, 0);

        var rightPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };
        var okButton = new Button { Text = "Apply", AutoSize = true };
        okButton.Click += (_, _) => ConfirmSelection();
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        StyleButton(okButton, useAccent: true);
        StyleButton(cancelButton, useAccent: false);
        rightPanel.Controls.Add(okButton);
        rightPanel.Controls.Add(cancelButton);
        bottomLayout.Controls.Add(rightPanel, 1, 0);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void SetAllFieldsChecked(bool isChecked)
    {
        for (var index = 0; index < _fieldList.Items.Count; index++)
        {
            _fieldList.SetItemChecked(index, isChecked);
        }
    }

    private void RestoreDefaults()
    {
        for (var index = 0; index < _fields.Count; index++)
        {
            _fieldList.SetItemChecked(index, _fields[index].IsDefault);
        }
    }

    private void ConfirmSelection()
    {
        var selectedFields = new List<SongGridColumnField>();
        for (var index = 0; index < _fields.Count; index++)
        {
            if (_fieldList.GetItemChecked(index))
            {
                selectedFields.Add(_fields[index]);
            }
        }

        if (selectedFields.Count == 0)
        {
            using var messageDialog = new ThemedMessageForm(Text, "Select at least one column to show.", _theme, ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        SelectedFields = selectedFields;
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
