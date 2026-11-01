using System.Globalization;
using System.ComponentModel;

namespace SongMetainfoBrowser.App;

internal sealed class AdvancedSearchForm : Form
{
    private const int MaximumSortLevels = 4;
    private readonly AppTheme _theme;
    private readonly bool _filterSortMode;
    private readonly ComboBox _savedSearchesComboBox = new();
    private readonly ComboBox _matchModeComboBox = new();
    private readonly FlowLayoutPanel _rulesPanel = new();
    private readonly Button _loadSavedSearchButton = new();
    private readonly Button _saveCurrentSearchButton = new();
    private readonly Button _deleteSavedSearchButton = new();
    private readonly Button _searchButton = new();
    private readonly Button _clearButton = new();
    private readonly List<RuleRowPanel> _ruleRows = [];
    private readonly IReadOnlyList<string> _statusOptions;
    private readonly IReadOnlyList<string> _genreOptions;
    private readonly IReadOnlyList<SortColumnOption> _sortColumns;
    private readonly ComboBox[] _sortColumnCombos = new ComboBox[MaximumSortLevels];
    private readonly ComboBox[] _sortDirectionCombos = new ComboBox[MaximumSortLevels];
    private readonly string?[] _selectedSortColumns = new string?[MaximumSortLevels];
    private readonly ListSortDirection[] _selectedSortDirections = new ListSortDirection[MaximumSortLevels];
    private bool _updatingSortControls;
    private string? _selectedSavedSearchName;

    public AdvancedSearchQuery? SearchQuery { get; private set; }
    public IReadOnlyList<SongGridSortLevel> SelectedSortLevels => Enumerable.Range(0, MaximumSortLevels)
        .TakeWhile(index => !string.IsNullOrWhiteSpace(_selectedSortColumns[index]))
        .Select(index => new SongGridSortLevel
        {
            ColumnName = _selectedSortColumns[index]!,
            Direction = _selectedSortDirections[index].ToString()
        })
        .ToArray();
    public bool ClearActiveSearchRequested { get; private set; }
    public IReadOnlyList<SavedAdvancedSearch> SavedSearches => _savedSearches
        .Select(CloneSavedSearch)
        .ToArray();

    private readonly List<SavedAdvancedSearch> _savedSearches;

    public AdvancedSearchForm(
        AppTheme theme,
        AdvancedSearchQuery? initialQuery = null,
        IReadOnlyList<SavedAdvancedSearch>? savedSearches = null,
        IReadOnlyList<string>? statusOptions = null,
        IReadOnlyList<string>? genreOptions = null,
        bool filterSortMode = false,
        IReadOnlyList<(string ColumnName, string Label)>? sortColumns = null,
        IReadOnlyList<SongGridSortLevel>? initialSortLevels = null)
    {
        _theme = theme;
        _filterSortMode = filterSortMode;
        _statusOptions = NormalizeOptions(statusOptions);
        _genreOptions = NormalizeOptions(genreOptions);
        _sortColumns = (sortColumns ?? [])
            .Where(column => !string.IsNullOrWhiteSpace(column.ColumnName))
            .Select(column => new SortColumnOption(column.ColumnName, column.Label))
            .ToArray();
        for (var index = 0; index < MaximumSortLevels; index++)
        {
            var level = initialSortLevels is not null && index < initialSortLevels.Count
                ? initialSortLevels[index]
                : null;
            _selectedSortColumns[index] = level is not null
                && _sortColumns.Any(column => string.Equals(column.ColumnName, level.ColumnName, StringComparison.OrdinalIgnoreCase))
                    ? level.ColumnName
                    : null;
            _selectedSortDirections[index] = string.Equals(level?.Direction, nameof(ListSortDirection.Descending), StringComparison.OrdinalIgnoreCase)
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }
        NormalizeSortSelections();
        _savedSearches = savedSearches?
            .Select(CloneSavedSearch)
            .OrderBy(search => search.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList()
            ?? [];
        var fontPreferences = AppFontSettings.LoadPreferences();

        Text = "Advanced Search";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        MinimumSize = SizeFromClientSize(AppFontSettings.Scale(new Size(760, 360), fontPreferences, AppFontSection.Dialogs));
        ClientSize = AppFontSettings.Scale(new Size(860, _filterSortMode ? 680 : 460), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        BuildLayout();
        LoadInitialQuery(initialQuery);
        RefreshSortControls();
    }

    private void BuildLayout()
    {
        var footerHeight = AppFontSettings.Scale(52, AppFontSettings.LoadPreferences(), AppFontSection.Dialogs);
        var hostLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = _theme.AppBackColor
        };
        hostLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        hostLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, footerHeight));
        Controls.Add(hostLayout);

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12, 12, 12, 0),
            BackColor = _theme.AppBackColor
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        hostLayout.Controls.Add(rootLayout, 0, 0);

        var descriptionLabel = new Label
        {
            AutoSize = true,
            Text = "Search across song metadata, SongLens fields, file dates, notes, and track details.",
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 0, 0, 10)
        };
        rootLayout.Controls.Add(descriptionLabel, 0, 0);

        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        var matchLabel = new Label
        {
            AutoSize = true,
            Text = "Match:",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 8, 8, 0)
        };
        _matchModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _matchModeComboBox.Width = 150;
        _matchModeComboBox.Items.Add(new MatchModeOption(AdvancedSearchMatchMode.AllRules, "All rules"));
        _matchModeComboBox.Items.Add(new MatchModeOption(AdvancedSearchMatchMode.AnyRule, "Any rules"));
        _matchModeComboBox.SelectedIndex = 0;
        ApplyInputTheme(_matchModeComboBox);
        topPanel.Controls.Add(matchLabel);
        topPanel.Controls.Add(_matchModeComboBox);
        rootLayout.Controls.Add(topPanel, 0, 1);

        var savedSearchPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 10, 0, 0)
        };
        var savedSearchLabel = new Label
        {
            AutoSize = true,
            Text = "Saved Search:",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 8, 8, 0)
        };
        _savedSearchesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _savedSearchesComboBox.Width = 220;
        ApplyInputTheme(_savedSearchesComboBox);

        _loadSavedSearchButton.Text = "Load";
        _loadSavedSearchButton.AutoSize = true;
        _loadSavedSearchButton.Click += (_, _) => LoadSelectedSavedSearch();
        StyleButton(_loadSavedSearchButton, useAccent: false);

        _saveCurrentSearchButton.Text = "Save Current";
        _saveCurrentSearchButton.AutoSize = true;
        _saveCurrentSearchButton.Click += (_, _) => SaveCurrentSearch();
        StyleButton(_saveCurrentSearchButton, useAccent: false);

        _deleteSavedSearchButton.Text = "Delete";
        _deleteSavedSearchButton.AutoSize = true;
        _deleteSavedSearchButton.Click += (_, _) => DeleteSelectedSavedSearch();
        StyleButton(_deleteSavedSearchButton, useAccent: false);

        savedSearchPanel.Controls.Add(savedSearchLabel);
        savedSearchPanel.Controls.Add(_savedSearchesComboBox);
        savedSearchPanel.Controls.Add(_loadSavedSearchButton);
        savedSearchPanel.Controls.Add(_saveCurrentSearchButton);
        savedSearchPanel.Controls.Add(_deleteSavedSearchButton);
        rootLayout.Controls.Add(savedSearchPanel, 0, 2);

        _rulesPanel.Dock = DockStyle.Fill;
        _rulesPanel.FlowDirection = FlowDirection.TopDown;
        _rulesPanel.WrapContents = false;
        _rulesPanel.AutoScroll = true;
        _rulesPanel.BackColor = _theme.AppBackColor;
        _rulesPanel.Margin = new Padding(0, 10, 0, 10);
        _rulesPanel.SizeChanged += (_, _) => UpdateRuleRowWidths();
        rootLayout.Controls.Add(_rulesPanel, 0, 3);

        if (_filterSortMode)
            rootLayout.Controls.Add(BuildSortSection(), 0, 4);

        var footerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _theme.AppBackColor,
            Padding = new Padding(12, 0, 12, 4),
            Margin = Padding.Empty
        };
        hostLayout.Controls.Add(footerPanel, 0, 1);

        var buttonRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _clearButton.Text = _filterSortMode ? "Clear All" : "Clear";
        _clearButton.AutoSize = true;
        _clearButton.Margin = Padding.Empty;
        _clearButton.Click += (_, _) => ClearRules();
        StyleButton(_clearButton, useAccent: false);

        var actionButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = _theme.AppBackColor,
            Margin = Padding.Empty
        };
        var cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(8, 0, 0, 0)
        };
        _searchButton.Text = "Search";
        _searchButton.AutoSize = true;
        _searchButton.Margin = new Padding(0, 0, 8, 0);
        _searchButton.Click += (_, _) => ConfirmSearch();
        StyleButton(cancelButton, useAccent: false);
        StyleButton(_searchButton, useAccent: true);
        actionButtons.Controls.Add(cancelButton);
        actionButtons.Controls.Add(_clearButton);
        actionButtons.Controls.Add(_searchButton);
        buttonRow.Controls.Add(actionButtons, 1, 0);

        footerPanel.Controls.Add(buttonRow);
        AcceptButton = _searchButton;
        CancelButton = cancelButton;
        RefreshSavedSearches();
    }

    private void AddRule()
    {
        var row = new RuleRowPanel(_theme, _statusOptions, _genreOptions, AddRule, RemoveRule);
        AddRuleRow(row);
    }

    private void AddRule(AdvancedSearchRule rule)
    {
        var row = new RuleRowPanel(_theme, _statusOptions, _genreOptions, AddRule, RemoveRule);
        row.ApplyRule(rule);
        AddRuleRow(row);
    }

    private void AddRuleRow(RuleRowPanel row)
    {
        row.Margin = new Padding(0, 0, 0, 8);
        _ruleRows.Add(row);
        _rulesPanel.Controls.Add(row);
        UpdateRuleRowWidths();
        UpdateRuleButtons();
    }

    private void RemoveRule(RuleRowPanel row)
    {
        if (_ruleRows.Count <= 1)
        {
            return;
        }

        _rulesPanel.Controls.Remove(row);
        _ruleRows.Remove(row);
        row.Dispose();
        UpdateRuleRowWidths();
        UpdateRuleButtons();
    }

    private void UpdateRuleButtons()
    {
        var allowRemoval = _ruleRows.Count > 1;
        for (var index = 0; index < _ruleRows.Count; index++)
        {
            _ruleRows[index].SetRemoveEnabled(allowRemoval);
            _ruleRows[index].SetAddEnabled(index == _ruleRows.Count - 1);
        }
    }

    private void UpdateRuleRowWidths()
    {
        var targetWidth = Math.Max(700, _rulesPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
        foreach (var row in _ruleRows)
        {
            row.Width = targetWidth;
        }
    }

    private void ClearRules()
    {
        ClearActiveSearchRequested = true;
        foreach (var row in _ruleRows.ToArray())
        {
            _rulesPanel.Controls.Remove(row);
            row.Dispose();
        }

        _ruleRows.Clear();
        AddRule();
        _selectedSavedSearchName = null;
        UpdateSavedSearchSelection();
        if (_filterSortMode)
        {
            Array.Clear(_selectedSortColumns);
            for (var index = 0; index < MaximumSortLevels; index++)
                _selectedSortDirections[index] = ListSortDirection.Ascending;
            RefreshSortControls();
        }
    }

    private void LoadInitialQuery(AdvancedSearchQuery? initialQuery)
    {
        if (initialQuery is null || initialQuery.Rules.Count == 0)
        {
            AddRule();
            return;
        }

        _matchModeComboBox.SelectedItem = _matchModeComboBox.Items
            .Cast<MatchModeOption>()
            .FirstOrDefault(option => option.Value == initialQuery.MatchMode);

        foreach (var rule in initialQuery.Rules)
        {
            AddRule(rule);
        }
    }

    private void LoadSelectedSavedSearch()
    {
        if (_savedSearchesComboBox.SelectedItem is not SavedSearchOption option)
        {
            return;
        }

        ApplyQuery(option.Search.Query);
        if (_filterSortMode && option.Search.SortLevels is not null)
        {
            Array.Clear(_selectedSortColumns);
            for (var index = 0; index < Math.Min(MaximumSortLevels, option.Search.SortLevels.Count); index++)
            {
                _selectedSortColumns[index] = option.Search.SortLevels[index].ColumnName;
                _selectedSortDirections[index] = string.Equals(
                    option.Search.SortLevels[index].Direction,
                    nameof(ListSortDirection.Descending),
                    StringComparison.OrdinalIgnoreCase)
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            NormalizeSortSelections();
            RefreshSortControls();
        }
        ClearActiveSearchRequested = false;
        _selectedSavedSearchName = option.Search.Name;
        UpdateSavedSearchSelection();
    }

    private void SaveCurrentSearch()
    {
        if (!TryCollectQuery(out var query, out var errorMessage))
        {
            using var dialog = new ThemedMessageForm(Text, errorMessage, _theme, ThemedMessageKind.Warning);
            dialog.ShowDialog(this);
            return;
        }

        var initialName = _selectedSavedSearchName ?? "";
        using var prompt = new ThemedTextPromptForm("Save Search", "Name this saved search:", initialName, _theme, okText: "Save");
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var name = prompt.EnteredText;
        var existingIndex = _savedSearches.FindIndex(search => string.Equals(search.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            using var overwriteDialog = new ThemedConfirmationForm("Overwrite Saved Search", $"Overwrite \"{_savedSearches[existingIndex].Name}\"?", _theme, okText: "Overwrite");
            if (overwriteDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _savedSearches[existingIndex] = new SavedAdvancedSearch
            {
                Name = name,
                Query = CloneQuery(query),
                SortLevels = SelectedSortLevels.Select(level => new AdvancedSearchSortLevel
                {
                    ColumnName = level.ColumnName,
                    Direction = level.Direction
                }).ToArray()
            };
        }
        else
        {
            _savedSearches.Add(new SavedAdvancedSearch
            {
                Name = name,
                Query = CloneQuery(query),
                SortLevels = SelectedSortLevels.Select(level => new AdvancedSearchSortLevel
                {
                    ColumnName = level.ColumnName,
                    Direction = level.Direction
                }).ToArray()
            });
        }

        _savedSearches.Sort((left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name));
        ClearActiveSearchRequested = false;
        _selectedSavedSearchName = name;
        RefreshSavedSearches();
        using var savedDialog = new ThemedMessageForm(Text, $"Saved search \"{name}\" saved.", _theme, ThemedMessageKind.Information);
        savedDialog.ShowDialog(this);
    }

    private void DeleteSelectedSavedSearch()
    {
        if (_savedSearchesComboBox.SelectedItem is not SavedSearchOption option)
        {
            return;
        }

        using var deleteDialog = new ThemedConfirmationForm("Delete Saved Search", $"Delete saved search \"{option.Search.Name}\"?", _theme, okText: "Delete");
        if (deleteDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _savedSearches.RemoveAll(search => string.Equals(search.Name, option.Search.Name, StringComparison.OrdinalIgnoreCase));
        if (string.Equals(_selectedSavedSearchName, option.Search.Name, StringComparison.OrdinalIgnoreCase))
        {
            _selectedSavedSearchName = null;
        }

        RefreshSavedSearches();
    }

    private void RefreshSavedSearches()
    {
        _savedSearchesComboBox.Items.Clear();
        foreach (var search in _savedSearches)
        {
            _savedSearchesComboBox.Items.Add(new SavedSearchOption(search));
        }

        UpdateSavedSearchSelection();
        var hasSavedSearches = _savedSearchesComboBox.Items.Count > 0;
        _loadSavedSearchButton.Enabled = hasSavedSearches;
        _deleteSavedSearchButton.Enabled = hasSavedSearches;
    }

    private void UpdateSavedSearchSelection()
    {
        if (string.IsNullOrWhiteSpace(_selectedSavedSearchName))
        {
            if (_savedSearchesComboBox.Items.Count > 0 && _savedSearchesComboBox.SelectedIndex < 0)
            {
                _savedSearchesComboBox.SelectedIndex = 0;
            }

            return;
        }

        var selectedOption = _savedSearchesComboBox.Items
            .Cast<SavedSearchOption>()
            .FirstOrDefault(option => string.Equals(option.Search.Name, _selectedSavedSearchName, StringComparison.OrdinalIgnoreCase));
        if (selectedOption is not null)
        {
            _savedSearchesComboBox.SelectedItem = selectedOption;
        }
    }

    private void ApplyQuery(AdvancedSearchQuery query)
    {
        foreach (var row in _ruleRows.ToArray())
        {
            _rulesPanel.Controls.Remove(row);
            row.Dispose();
        }

        _ruleRows.Clear();
        _matchModeComboBox.SelectedItem = _matchModeComboBox.Items
            .Cast<MatchModeOption>()
            .FirstOrDefault(option => option.Value == query.MatchMode);

        foreach (var rule in query.Rules)
        {
            AddRule(rule);
        }
    }

    private void ConfirmSearch()
    {
        if (!TryCollectQuery(out var query, out var errorMessage))
        {
            using var dialog = new ThemedMessageForm(Text, errorMessage, _theme, ThemedMessageKind.Warning);
            dialog.ShowDialog(this);
            return;
        }

        SearchQuery = query;
        ClearActiveSearchRequested = false;
        DialogResult = DialogResult.OK;
        Close();
    }

    private bool TryCollectQuery(out AdvancedSearchQuery query, out string errorMessage)
    {
        var errors = new List<string>();
        var rules = new List<AdvancedSearchRule>();

        for (var index = 0; index < _ruleRows.Count; index++)
        {
            if (_filterSortMode && _ruleRows[index].IsBlank)
                continue;

            if (_ruleRows[index].TryBuildRule(out var rule, out var error))
            {
                rules.Add(rule);
                continue;
            }

            errors.Add($"Rule {index + 1}: {error}");
        }

        if (errors.Count > 0)
        {
            query = null!;
            errorMessage = string.Join("\n", errors);
            return false;
        }

        if (rules.Count == 0)
        {
            if (!_filterSortMode)
            {
                query = null!;
                errorMessage = "Add at least one valid rule before searching.";
                return false;
            }
        }

        query = new AdvancedSearchQuery
        {
            MatchMode = (_matchModeComboBox.SelectedItem as MatchModeOption)?.Value ?? AdvancedSearchMatchMode.AllRules,
            Rules = rules
        };
        errorMessage = "";
        return true;
    }

    private Control BuildSortSection()
    {
        var group = new GroupBox
        {
            Text = "Sort the results by",
            Dock = DockStyle.Top,
            AutoSize = true,
            ForeColor = _theme.TextColor,
            BackColor = _theme.AppBackColor,
            Padding = new Padding(10),
            Margin = new Padding(0, 6, 0, 8)
        };
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = MaximumSortLevels,
            BackColor = _theme.AppBackColor
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        group.Controls.Add(grid);

        for (var index = 0; index < MaximumSortLevels; index++)
        {
            var rowIndex = index;
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            grid.Controls.Add(new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Text = index == 0 ? "Sort by" : "Then by",
                ForeColor = _theme.TextColor
            }, 0, index);

            var columnCombo = CreateSortCombo();
            columnCombo.SelectedIndexChanged += (_, _) =>
            {
                if (_updatingSortControls) return;
                _selectedSortColumns[rowIndex] = (columnCombo.SelectedItem as SortColumnOption)?.ColumnName;
                NormalizeSortSelections();
                RefreshSortControls();
            };
            _sortColumnCombos[index] = columnCombo;
            grid.Controls.Add(columnCombo, 1, index);

            var directionCombo = CreateSortCombo();
            directionCombo.SelectedIndexChanged += (_, _) =>
            {
                if (_updatingSortControls || directionCombo.SelectedIndex < 0) return;
                _selectedSortDirections[rowIndex] = directionCombo.SelectedIndex == 1
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            };
            _sortDirectionCombos[index] = directionCombo;
            grid.Controls.Add(directionCombo, 2, index);
        }

        return group;
    }

    private ComboBox CreateSortCombo() => new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.Flat,
        BackColor = _theme.PanelBackColor,
        ForeColor = _theme.TextColor,
        Margin = new Padding(0, 3, 8, 3)
    };

    private void NormalizeSortSelections()
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var foundEmpty = false;
        for (var index = 0; index < MaximumSortLevels; index++)
        {
            var columnName = _selectedSortColumns[index];
            if (foundEmpty || string.IsNullOrWhiteSpace(columnName) || !selected.Add(columnName))
            {
                _selectedSortColumns[index] = null;
                foundEmpty = true;
            }
        }
    }

    private void RefreshSortControls()
    {
        if (!_filterSortMode || _sortColumnCombos.Any(combo => combo is null))
            return;

        _updatingSortControls = true;
        try
        {
            var selectedNames = _selectedSortColumns
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < MaximumSortLevels; index++)
            {
                var currentName = _selectedSortColumns[index];
                var enabled = index == 0 || !string.IsNullOrWhiteSpace(_selectedSortColumns[index - 1]);
                var columnCombo = _sortColumnCombos[index];
                columnCombo.Items.Clear();
                columnCombo.Items.Add(new SortColumnOption(null, "— None —"));
                foreach (var column in _sortColumns)
                {
                    if (!selectedNames.Contains(column.ColumnName)
                        || string.Equals(column.ColumnName, currentName, StringComparison.OrdinalIgnoreCase))
                    {
                        columnCombo.Items.Add(column);
                    }
                }
                columnCombo.SelectedItem = columnCombo.Items.Cast<SortColumnOption>().FirstOrDefault(
                    option => string.Equals(option.ColumnName, currentName, StringComparison.OrdinalIgnoreCase));
                columnCombo.SelectedIndex = Math.Max(0, columnCombo.SelectedIndex);
                columnCombo.Enabled = enabled;

                var directionCombo = _sortDirectionCombos[index];
                directionCombo.Items.Clear();
                directionCombo.Items.AddRange(GetSortDirectionLabels(currentName));
                directionCombo.SelectedIndex = _selectedSortDirections[index] == ListSortDirection.Descending ? 1 : 0;
                directionCombo.Enabled = enabled && !string.IsNullOrWhiteSpace(currentName);
            }
        }
        finally
        {
            _updatingSortControls = false;
        }
    }

    private object[] GetSortDirectionLabels(string? columnName)
    {
        if (string.Equals(columnName, "Status", StringComparison.OrdinalIgnoreCase))
            return ["Workflow order", "Reverse workflow order"];
        if (string.Equals(columnName, "DateCreated", StringComparison.OrdinalIgnoreCase)
            || string.Equals(columnName, "LastModified", StringComparison.OrdinalIgnoreCase))
            return ["Oldest first", "Newest first"];
        if (string.Equals(columnName, "Rating", StringComparison.OrdinalIgnoreCase)
            || string.Equals(columnName, "Year", StringComparison.OrdinalIgnoreCase)
            || string.Equals(columnName, "Tempo", StringComparison.OrdinalIgnoreCase)
            || string.Equals(columnName, "TrackCount", StringComparison.OrdinalIgnoreCase)
            || string.Equals(columnName, "SampleRate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(columnName, "BitDepth", StringComparison.OrdinalIgnoreCase))
            return ["Lowest first", "Highest first"];
        return ["A to Z", "Z to A"];
    }

    private void ApplyInputTheme(Control control)
    {
        control.BackColor = _theme.PanelBackColor;
        control.ForeColor = _theme.TextColor;
    }

    private void StyleButton(Button button, bool useAccent)
    {
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = new Size(0, AppFontSettings.Scale(30, AppFontSettings.LoadPreferences(), AppFontSection.Dialogs));
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = _theme.AccentHoverColor;
        button.FlatAppearance.MouseDownBackColor = _theme.AccentPressedColor;
        button.BackColor = _theme.AccentSoftColor;
        button.ForeColor = _theme.TextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.Font = Font;
    }

    private static IReadOnlyList<string> NormalizeOptions(IReadOnlyList<string>? options)
    {
        return options?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
            .ToArray()
            ?? [];
    }

    private sealed class MatchModeOption
    {
        public MatchModeOption(AdvancedSearchMatchMode value, string label)
        {
            Value = value;
            _label = label;
        }

        private readonly string _label;
        public AdvancedSearchMatchMode Value { get; }

        public override string ToString() => _label;
    }

    private sealed class FieldOption
    {
        public FieldOption(AdvancedSearchFieldDefinition definition)
        {
            Definition = definition;
        }

        public AdvancedSearchFieldDefinition Definition { get; }

        public override string ToString() => Definition.Label;
    }

    private sealed record SortColumnOption(string? ColumnName, string Label)
    {
        public override string ToString() => Label;
    }

    private sealed class SavedSearchOption
    {
        public SavedSearchOption(SavedAdvancedSearch search)
        {
            Search = CloneSavedSearch(search);
        }

        public SavedAdvancedSearch Search { get; }

        public override string ToString() => Search.Name;
    }

    private static SavedAdvancedSearch CloneSavedSearch(SavedAdvancedSearch search)
    {
        return new SavedAdvancedSearch
        {
            Name = search.Name,
            Query = CloneQuery(search.Query),
            SortLevels = search.SortLevels?.Select(level => new AdvancedSearchSortLevel
            {
                ColumnName = level.ColumnName,
                Direction = level.Direction
            }).ToArray()
        };
    }

    private static AdvancedSearchQuery CloneQuery(AdvancedSearchQuery query)
    {
        return new AdvancedSearchQuery
        {
            MatchMode = query.MatchMode,
            Rules = query.Rules
                .Select(rule => new AdvancedSearchRule
                {
                    FieldKey = rule.FieldKey,
                    Operator = rule.Operator,
                    ValueText = rule.ValueText,
                    NumberValue = rule.NumberValue,
                    DateValue = rule.DateValue
                })
                .ToArray()
        };
    }

    private sealed class OperatorOption
    {
        public OperatorOption(AdvancedSearchOperator value)
        {
            Value = value;
        }

        public AdvancedSearchOperator Value { get; }

        public override string ToString() => AdvancedSearchCatalog.GetOperatorLabel(Value);
    }

    private sealed class RuleRowPanel : Panel
    {
        private readonly AppTheme _theme;
        private readonly Action _addAction;
        private readonly Action<RuleRowPanel> _removeAction;
        private readonly ComboBox _fieldComboBox = new();
        private readonly ComboBox _operatorComboBox = new();
        private readonly TextBox _valueTextBox = new();
        private readonly ComboBox _valueComboBox = new();
        private readonly DateTimePicker _datePicker = new();
        private readonly Button _addButton = new();
        private readonly Button _removeButton = new();

        private readonly IReadOnlyList<string> _statusOptions;
        private readonly IReadOnlyList<string> _genreOptions;

        public bool IsBlank => _fieldComboBox.SelectedItem is FieldOption fieldOption
            && fieldOption.Definition.FieldType == AdvancedSearchFieldType.Text
            && !IsChoiceField(fieldOption.Definition.Key)
            && string.IsNullOrWhiteSpace(_valueTextBox.Text);

        public RuleRowPanel(
            AppTheme theme,
            IReadOnlyList<string> statusOptions,
            IReadOnlyList<string> genreOptions,
            Action addAction,
            Action<RuleRowPanel> removeAction)
        {
            _theme = theme;
            _statusOptions = statusOptions;
            _genreOptions = genreOptions;
            _addAction = addAction;
            _removeAction = removeAction;

            Height = AppFontSettings.Scale(38, AppFontSettings.LoadPreferences(), AppFontSection.Dialogs);
            BackColor = theme.AppBackColor;

            BuildLayout();
            LoadFields();
            Resize += (_, _) => UpdateLayoutBounds();
        }

        public void SetRemoveEnabled(bool isEnabled)
        {
            _removeButton.Enabled = isEnabled;
        }

        public void SetAddEnabled(bool isEnabled)
        {
            _addButton.Enabled = isEnabled;
        }

        public bool TryBuildRule(out AdvancedSearchRule rule, out string error)
        {
            rule = null!;
            error = "";

            if (_fieldComboBox.SelectedItem is not FieldOption fieldOption)
            {
                error = "Choose a field.";
                return false;
            }

            if (_operatorComboBox.SelectedItem is not OperatorOption operatorOption)
            {
                error = "Choose an operator.";
                return false;
            }

            var field = fieldOption.Definition;
            if (IsChoiceField(field.Key))
            {
                var text = _valueComboBox.SelectedItem?.ToString()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(text))
                {
                    error = "Choose a value.";
                    return false;
                }

                rule = new AdvancedSearchRule
                {
                    FieldKey = field.Key,
                    Operator = operatorOption.Value,
                    ValueText = text
                };
                return true;
            }

            if (IsRatingField(field.Key))
            {
                if (_valueComboBox.SelectedItem is not RatingOption ratingOption)
                {
                    error = "Choose a rating.";
                    return false;
                }

                rule = new AdvancedSearchRule
                {
                    FieldKey = field.Key,
                    Operator = operatorOption.Value,
                    ValueText = ratingOption.Value.ToString(CultureInfo.CurrentCulture),
                    NumberValue = ratingOption.Value
                };
                return true;
            }

            switch (field.FieldType)
            {
                case AdvancedSearchFieldType.Text:
                {
                    var text = _valueTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        error = "Enter a value.";
                        return false;
                    }

                    rule = new AdvancedSearchRule
                    {
                        FieldKey = field.Key,
                        Operator = operatorOption.Value,
                        ValueText = text
                    };
                    return true;
                }
                case AdvancedSearchFieldType.Number:
                {
                    var text = _valueTextBox.Text.Trim();
                    if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var numericValue) &&
                        !decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out numericValue))
                    {
                        error = "Enter a valid number.";
                        return false;
                    }

                    rule = new AdvancedSearchRule
                    {
                        FieldKey = field.Key,
                        Operator = operatorOption.Value,
                        ValueText = text,
                        NumberValue = numericValue
                    };
                    return true;
                }
                case AdvancedSearchFieldType.Date:
                    rule = new AdvancedSearchRule
                    {
                        FieldKey = field.Key,
                        Operator = operatorOption.Value,
                        ValueText = _datePicker.Value.ToShortDateString(),
                        DateValue = _datePicker.Value.Date
                    };
                    return true;
                default:
                    error = "Unsupported field type.";
                    return false;
            }
        }

        public void ApplyRule(AdvancedSearchRule rule)
        {
            var fieldOption = _fieldComboBox.Items
                .Cast<FieldOption>()
                .FirstOrDefault(option => string.Equals(option.Definition.Key, rule.FieldKey, StringComparison.OrdinalIgnoreCase));
            if (fieldOption is null)
            {
                return;
            }

            _fieldComboBox.SelectedItem = fieldOption;

            var operatorOption = _operatorComboBox.Items
                .Cast<OperatorOption>()
                .FirstOrDefault(option => option.Value == rule.Operator);
            if (operatorOption is not null)
            {
                _operatorComboBox.SelectedItem = operatorOption;
            }

            switch (fieldOption.Definition.FieldType)
            {
                case AdvancedSearchFieldType.Text:
                case AdvancedSearchFieldType.Number:
                    _valueTextBox.Text = rule.ValueText ?? "";
                    break;
                case AdvancedSearchFieldType.Date:
                    if (rule.DateValue is DateTime dateValue)
                    {
                        _datePicker.Value = dateValue;
                    }
                    break;
            }

            if (IsChoiceField(fieldOption.Definition.Key))
            {
                SelectOrAddChoice(rule.ValueText);
            }
            else if (IsRatingField(fieldOption.Definition.Key))
            {
                var rating = rule.NumberValue ?? (decimal.TryParse(rule.ValueText, out var parsed) ? parsed : -1);
                _valueComboBox.SelectedItem = _valueComboBox.Items
                    .Cast<RatingOption>()
                    .FirstOrDefault(option => option.Value == rating);
            }
        }

        private void BuildLayout()
        {
            _fieldComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _operatorComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _valueComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _fieldComboBox.SelectedIndexChanged += (_, _) => HandleFieldChanged();
            ApplyInputTheme(_fieldComboBox);
            ApplyInputTheme(_operatorComboBox);
            ApplyInputTheme(_valueTextBox);
            ApplyInputTheme(_valueComboBox);
            ApplyInputTheme(_datePicker);

            _datePicker.Format = DateTimePickerFormat.Custom;
            _datePicker.CustomFormat = "M/dd/yyyy";
            _datePicker.Visible = false;

            _addButton.Text = "Add Rule";
            _addButton.Click += (_, _) => _addAction();
            _addButton.EnabledChanged += (_, _) => ApplyAddButtonEnabledAppearance();
            StyleButton(_addButton);
            _addButton.AutoSize = false;

            _removeButton.Text = "Remove";
            _removeButton.Click += (_, _) => _removeAction(this);
            StyleButton(_removeButton);
            _removeButton.AutoSize = false;

            Controls.Add(_fieldComboBox);
            Controls.Add(_operatorComboBox);
            Controls.Add(_valueTextBox);
            Controls.Add(_valueComboBox);
            Controls.Add(_datePicker);
            Controls.Add(_addButton);
            Controls.Add(_removeButton);
            ApplyAddButtonEnabledAppearance();
            UpdateLayoutBounds();
        }

        private void ApplyAddButtonEnabledAppearance()
        {
            _addButton.BackColor = _addButton.Enabled
                ? _theme.AccentSoftColor
                : _theme.PanelAltBackColor;
            _addButton.ForeColor = _addButton.Enabled
                ? _theme.TextColor
                : _theme.MutedTextColor;
            _addButton.FlatAppearance.BorderColor = _addButton.Enabled
                ? _theme.BorderColor
                : _theme.MutedTextColor;
        }

        private void LoadFields()
        {
            foreach (var field in AdvancedSearchCatalog.Fields)
            {
                _fieldComboBox.Items.Add(new FieldOption(field));
            }

            if (_fieldComboBox.Items.Count > 0)
            {
                _fieldComboBox.SelectedIndex = 0;
            }
        }

        private void HandleFieldChanged()
        {
            if (_fieldComboBox.SelectedItem is not FieldOption option)
            {
                return;
            }

            _operatorComboBox.Items.Clear();
            foreach (var op in AdvancedSearchCatalog.GetOperators(option.Definition.FieldType))
            {
                _operatorComboBox.Items.Add(new OperatorOption(op));
            }

            if (_operatorComboBox.Items.Count > 0)
            {
                _operatorComboBox.SelectedIndex = 0;
            }

            var useDatePicker = option.Definition.FieldType == AdvancedSearchFieldType.Date;
            var useChoicePicker = IsChoiceField(option.Definition.Key) || IsRatingField(option.Definition.Key);
            PopulateValueChoices(option.Definition.Key);
            _datePicker.Visible = useDatePicker;
            _valueComboBox.Visible = useChoicePicker;
            _valueTextBox.Visible = !useDatePicker && !useChoicePicker;
            _valueTextBox.PlaceholderText = option.Definition.FieldType == AdvancedSearchFieldType.Number
                ? "Enter a number"
                : "Enter a value";
        }

        private void UpdateLayoutBounds()
        {
            var margin = 6;
            var height = AppFontSettings.Scale(30, AppFontSettings.LoadPreferences(), AppFontSection.Dialogs);
            var y = AppFontSettings.Scale(2, AppFontSettings.LoadPreferences(), AppFontSection.Dialogs);
            var addWidth = 84;
            var removeWidth = 84;
            var fieldWidth = 170;
            var operatorWidth = 170;
            var valueWidth = Math.Max(160, Width - fieldWidth - operatorWidth - addWidth - removeWidth - (margin * 5));

            _fieldComboBox.SetBounds(0, y, fieldWidth, height);
            _operatorComboBox.SetBounds(fieldWidth + margin, y, operatorWidth, height);
            _valueTextBox.SetBounds(fieldWidth + operatorWidth + (margin * 2), y, valueWidth, height);
            _valueComboBox.SetBounds(fieldWidth + operatorWidth + (margin * 2), y, valueWidth, height);
            _datePicker.SetBounds(fieldWidth + operatorWidth + (margin * 2), y, valueWidth, height);
            _addButton.SetBounds(fieldWidth + operatorWidth + valueWidth + (margin * 3), y, addWidth, height);
            _removeButton.SetBounds(fieldWidth + operatorWidth + valueWidth + addWidth + (margin * 4), y, removeWidth, height);
        }

        private void ApplyInputTheme(Control control)
        {
            control.BackColor = _theme.PanelBackColor;
            control.ForeColor = _theme.TextColor;
            control.Font = Font;
        }

        private void StyleButton(Button button)
        {
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = new Size(0, AppFontSettings.Scale(30, AppFontSettings.LoadPreferences(), AppFontSection.Dialogs));
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = _theme.AccentHoverColor;
            button.FlatAppearance.MouseDownBackColor = _theme.AccentPressedColor;
            button.BackColor = _theme.AccentSoftColor;
            button.ForeColor = _theme.TextColor;
            button.FlatAppearance.BorderColor = _theme.BorderColor;
            button.Font = Font;
        }

        private void PopulateValueChoices(string fieldKey)
        {
            _valueComboBox.Items.Clear();
            if (IsRatingField(fieldKey))
            {
                _valueComboBox.Items.Add(new RatingOption(0, "Unrated"));
                for (var rating = 1; rating <= 5; rating++)
                {
                    _valueComboBox.Items.Add(new RatingOption(
                        rating,
                        $"{new string('\u2605', rating)} ({rating} star{(rating == 1 ? "" : "s")})"));
                }
            }
            else
            {
                var options = string.Equals(fieldKey, "status", StringComparison.OrdinalIgnoreCase)
                    ? _statusOptions
                    : _genreOptions;
                _valueComboBox.Items.AddRange(options.Cast<object>().ToArray());
            }

            if (_valueComboBox.Items.Count > 0)
            {
                _valueComboBox.SelectedIndex = 0;
            }
        }

        private void SelectOrAddChoice(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var existing = _valueComboBox.Items.Cast<object>()
                .FirstOrDefault(item => string.Equals(item.ToString(), value, StringComparison.CurrentCultureIgnoreCase));
            if (existing is null)
            {
                _valueComboBox.Items.Add(value);
                existing = value;
            }

            _valueComboBox.SelectedItem = existing;
        }

        private static bool IsChoiceField(string fieldKey)
        {
            return string.Equals(fieldKey, "status", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldKey, "genre", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRatingField(string fieldKey)
        {
            return string.Equals(fieldKey, "rating", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class RatingOption
        {
            private readonly string _label;

            public RatingOption(int value, string label)
            {
                Value = value;
                _label = label;
            }

            public int Value { get; }

            public override string ToString() => _label;
        }
    }
}
