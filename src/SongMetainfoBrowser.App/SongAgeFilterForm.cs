namespace SongMetainfoBrowser.App;

internal enum SongAgeFilterOperator
{
    LessThan,
    OlderThan
}

internal sealed class SongAgeFilter
{
    public required SongAgeFilterOperator Operator { get; init; }
    public required int Days { get; init; }

    public string OperatorText => Operator == SongAgeFilterOperator.LessThan ? "less than" : "older than";
}

internal sealed class SongAgeFilterForm : Form
{
    private readonly ComboBox _operatorComboBox = new();
    private readonly ComboBox _daysComboBox = new();

    public SongAgeFilter? SelectedFilter { get; private set; }

    public SongAgeFilterForm(SongAgeFilter? currentFilter)
    {
        Text = "View Filter";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 150);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var sentencePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Anchor = AnchorStyles.None
        };

        var songsLabel = new Label
        {
            AutoSize = true,
            Text = "Songs",
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 8, 8, 0)
        };

        _operatorComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _operatorComboBox.Width = 100;
        _operatorComboBox.Items.AddRange(new object[] { "less than", "older than" });
        _operatorComboBox.SelectedIndex = currentFilter?.Operator == SongAgeFilterOperator.OlderThan ? 1 : 0;

        _daysComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _daysComboBox.Width = 80;
        _daysComboBox.Items.AddRange(new object[] { "30", "60", "90", "120", "360" });
        var selectedDays = currentFilter?.Days.ToString() ?? "30";
        _daysComboBox.SelectedItem = _daysComboBox.Items.Contains(selectedDays) ? selectedDays : "30";

        var daysLabel = new Label
        {
            AutoSize = true,
            Text = "days",
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(8, 8, 0, 0)
        };

        sentencePanel.Controls.Add(songsLabel);
        sentencePanel.Controls.Add(_operatorComboBox);
        sentencePanel.Controls.Add(_daysComboBox);
        sentencePanel.Controls.Add(daysLabel);
        layout.Controls.Add(sentencePanel, 0, 0);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        var okButton = new Button
        {
            Text = "OK",
            AutoSize = true
        };
        okButton.Click += (_, _) =>
        {
            SelectedFilter = new SongAgeFilter
            {
                Operator = _operatorComboBox.SelectedIndex == 0 ? SongAgeFilterOperator.LessThan : SongAgeFilterOperator.OlderThan,
                Days = int.Parse((string)_daysComboBox.SelectedItem!)
            };
            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        layout.Controls.Add(buttonPanel, 0, 1);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}
