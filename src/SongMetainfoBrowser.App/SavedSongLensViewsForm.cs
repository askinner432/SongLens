namespace SongMetainfoBrowser.App;

internal sealed class SavedSongLensViewsForm : Form
{
    private readonly AppTheme _theme;
    private readonly ListBox _viewsListBox = new();
    private readonly List<SavedSongLensView> _savedViews;
    private readonly bool _hasDateFilter;
    private readonly Func<string, bool, SavedSongLensView> _captureCurrentView;

    public IReadOnlyList<SavedSongLensView> SavedViews => _savedViews;

    public SavedSongLensViewsForm(
        IEnumerable<SavedSongLensView> savedViews,
        bool hasDateFilter,
        Func<string, bool, SavedSongLensView> captureCurrentView,
        AppTheme theme)
    {
        _theme = theme;
        _savedViews = savedViews.Select(CloneView).ToList();
        _hasDateFilter = hasDateFilter;
        _captureCurrentView = captureCurrentView;
        var fontPreferences = AppFontSettings.LoadPreferences();

        Text = "Manage Grid Views";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = AppFontSettings.Scale(new Size(500, 360), fontPreferences, AppFontSection.Dialogs);
        MinimumSize = SizeFromClientSize(
            AppFontSettings.Scale(new Size(420, 300), fontPreferences, AppFontSection.Dialogs));
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        BuildLayout();
        RefreshViewList();
    }

    private void BuildLayout()
    {
        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(rootLayout);

        var explanation = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(460, 0),
            Text = "Add the current grid layout, or rename and delete saved views. Changes are stored when you click Save Changes.",
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 0, 0, 10)
        };
        rootLayout.Controls.Add(explanation, 0, 0);

        _viewsListBox.Dock = DockStyle.Fill;
        _viewsListBox.IntegralHeight = false;
        _viewsListBox.BorderStyle = BorderStyle.FixedSingle;
        _viewsListBox.BackColor = _theme.PanelBackColor;
        _viewsListBox.ForeColor = _theme.TextColor;
        _viewsListBox.DoubleClick += (_, _) => RenameSelectedView();
        rootLayout.Controls.Add(_viewsListBox, 0, 1);

        var actionPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 8, 0, 0)
        };
        var addButton = new Button { Text = "Add Current View...", AutoSize = true };
        var renameButton = new Button { Text = "Rename...", AutoSize = true };
        var deleteButton = new Button { Text = "Delete", AutoSize = true };
        addButton.Click += (_, _) => AddCurrentView();
        renameButton.Click += (_, _) => RenameSelectedView();
        deleteButton.Click += (_, _) => DeleteSelectedView();
        StyleButton(addButton, useAccent: false);
        StyleButton(renameButton, useAccent: false);
        StyleButton(deleteButton, useAccent: false);
        actionPanel.Controls.Add(addButton);
        actionPanel.Controls.Add(renameButton);
        actionPanel.Controls.Add(deleteButton);
        rootLayout.Controls.Add(actionPanel, 0, 2);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 12, 0, 0)
        };
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var saveButton = new Button { Text = "Save Changes", AutoSize = true, DialogResult = DialogResult.OK };
        StyleButton(cancelButton, useAccent: false);
        StyleButton(saveButton, useAccent: true);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(saveButton);
        rootLayout.Controls.Add(buttonPanel, 0, 3);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void RefreshViewList(int selectedIndex = 0)
    {
        _viewsListBox.BeginUpdate();
        try
        {
            _viewsListBox.Items.Clear();
            _viewsListBox.Items.AddRange(_savedViews.Select(view => view.Name).Cast<object>().ToArray());
        }
        finally
        {
            _viewsListBox.EndUpdate();
        }

        if (_viewsListBox.Items.Count > 0)
        {
            _viewsListBox.SelectedIndex = Math.Clamp(selectedIndex, 0, _viewsListBox.Items.Count - 1);
        }
    }

    private void AddCurrentView()
    {
        using var prompt = new SaveSongLensViewForm(_hasDateFilter, _theme);
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var name = prompt.ViewName;
        var existingIndex = _savedViews.FindIndex(
            view => string.Equals(view.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            using var confirmation = new ThemedConfirmationForm(
                "Overwrite Saved View",
                $"Replace the saved view \"{_savedViews[existingIndex].Name}\" with the current grid layout?",
                _theme,
                okText: "Replace");
            if (confirmation.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
        }

        var view = _captureCurrentView(name, prompt.IncludeDateFilter);
        if (existingIndex >= 0)
        {
            _savedViews[existingIndex] = view;
        }
        else
        {
            _savedViews.Add(view);
        }

        RefreshViewList(existingIndex >= 0 ? existingIndex : _savedViews.Count - 1);
    }

    private void RenameSelectedView()
    {
        var selectedIndex = _viewsListBox.SelectedIndex;
        if (selectedIndex < 0)
        {
            return;
        }

        var view = _savedViews[selectedIndex];
        using var prompt = new ThemedTextPromptForm(
            "Rename Saved View",
            "Enter a new name:",
            view.Name,
            _theme,
            okText: "Rename");
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var name = prompt.EnteredText;
        if (_savedViews.Where((_, index) => index != selectedIndex)
            .Any(existing => string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            using var messageDialog = new ThemedMessageForm(
                Text,
                "A saved view with that name already exists.",
                _theme,
                ThemedMessageKind.Information);
            messageDialog.ShowDialog(this);
            return;
        }

        view.Name = name;
        RefreshViewList(selectedIndex);
    }

    private void DeleteSelectedView()
    {
        var selectedIndex = _viewsListBox.SelectedIndex;
        if (selectedIndex < 0)
        {
            return;
        }

        var view = _savedViews[selectedIndex];
        using var confirmation = new ThemedConfirmationForm(
            "Delete Saved View",
            $"Delete the saved view \"{view.Name}\"?",
            _theme,
            okText: "Delete");
        if (confirmation.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _savedViews.RemoveAt(selectedIndex);
        RefreshViewList(selectedIndex);
    }

    private static SavedSongLensView CloneView(SavedSongLensView view)
    {
        return new SavedSongLensView
        {
            Name = view.Name,
            VisibleColumnKeys = view.VisibleColumnKeys.ToList(),
            ColumnOrder = view.ColumnOrder.ToList(),
            ColumnWidths = new Dictionary<string, int>(view.ColumnWidths, StringComparer.OrdinalIgnoreCase),
            SortColumnName = view.SortColumnName,
            SortDirection = view.SortDirection,
            SortLevels = (view.SortLevels ?? []).Select(level => new SongGridSortLevel
            {
                ColumnName = level.ColumnName,
                Direction = level.Direction
            }).ToList(),
            DateFilter = view.DateFilter is null
                ? null
                : new SongAgeFilter
                {
                    Mode = view.DateFilter.Mode,
                    DateField = view.DateFilter.DateField,
                    Operator = view.DateFilter.Operator,
                    Days = view.DateFilter.Days,
                    StartDate = view.DateFilter.StartDate,
                    EndDate = view.DateFilter.EndDate
                }
        };
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
