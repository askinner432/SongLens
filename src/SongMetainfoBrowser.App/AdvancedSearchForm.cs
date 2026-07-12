using System.Globalization;

namespace SongMetainfoBrowser.App;

internal sealed class AdvancedSearchForm : Form
{
    private readonly AppTheme _theme;
    private readonly ComboBox _savedSearchesComboBox = new();
    private readonly ComboBox _matchModeComboBox = new();
    private readonly FlowLayoutPanel _rulesPanel = new();
    private readonly Button _loadSavedSearchButton = new();
    private readonly Button _saveCurrentSearchButton = new();
    private readonly Button _deleteSavedSearchButton = new();
    private readonly Button _searchButton = new();
    private readonly Button _clearButton = new();
    private readonly List<RuleRowPanel> _ruleRows = [];
    private string? _selectedSavedSearchName;

    public AdvancedSearchQuery? SearchQuery { get; private set; }
    public bool ClearActiveSearchRequested { get; private set; }
    public IReadOnlyList<SavedAdvancedSearch> SavedSearches => _savedSearches
        .Select(CloneSavedSearch)
        .ToArray();

    private readonly List<SavedAdvancedSearch> _savedSearches;

    public AdvancedSearchForm(AppTheme theme, AdvancedSearchQuery? initialQuery = null, IReadOnlyList<SavedAdvancedSearch>? savedSearches = null)
    {
        _theme = theme;
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
        ClientSize = AppFontSettings.Scale(new Size(860, 460), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        BuildLayout();
        LoadInitialQuery(initialQuery);
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
            RowCount = 3,
            Padding = new Padding(12, 12, 12, 0),
            BackColor = _theme.AppBackColor
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        hostLayout.Controls.Add(rootLayout, 0, 0);

        var descriptionLabel = new Label
        {
            AutoSize = true,
            Text = "Search across song metadata, file dates, notes, and track details.",
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

        _clearButton.Text = "Clear";
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
        var row = new RuleRowPanel(_theme, AddRule, RemoveRule);
        AddRuleRow(row);
    }

    private void AddRule(AdvancedSearchRule rule)
    {
        var row = new RuleRowPanel(_theme, AddRule, RemoveRule);
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
                Query = CloneQuery(query)
            };
        }
        else
        {
            _savedSearches.Add(new SavedAdvancedSearch
            {
                Name = name,
                Query = CloneQuery(query)
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
            query = null!;
            errorMessage = "Add at least one valid rule before searching.";
            return false;
        }

        query = new AdvancedSearchQuery
        {
            MatchMode = (_matchModeComboBox.SelectedItem as MatchModeOption)?.Value ?? AdvancedSearchMatchMode.AllRules,
            Rules = rules
        };
        errorMessage = "";
        return true;
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
        button.FlatAppearance.MouseOverBackColor = useAccent ? _theme.AccentHoverColor : _theme.NeutralHoverColor;
        button.FlatAppearance.MouseDownBackColor = useAccent ? _theme.AccentPressedColor : _theme.NeutralPressedColor;
        button.BackColor = useAccent ? _theme.AccentSoftColor : _theme.PanelBackColor;
        button.ForeColor = _theme.TextColor;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.Font = Font;
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
            Query = CloneQuery(search.Query)
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
        private readonly DateTimePicker _datePicker = new();
        private readonly Button _addButton = new();
        private readonly Button _removeButton = new();

        public RuleRowPanel(AppTheme theme, Action addAction, Action<RuleRowPanel> removeAction)
        {
            _theme = theme;
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
        }

        private void BuildLayout()
        {
            _fieldComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _operatorComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _fieldComboBox.SelectedIndexChanged += (_, _) => HandleFieldChanged();
            ApplyInputTheme(_fieldComboBox);
            ApplyInputTheme(_operatorComboBox);
            ApplyInputTheme(_valueTextBox);
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
            Controls.Add(_datePicker);
            Controls.Add(_addButton);
            Controls.Add(_removeButton);
            ApplyAddButtonEnabledAppearance();
            UpdateLayoutBounds();
        }

        private void ApplyAddButtonEnabledAppearance()
        {
            _addButton.BackColor = _addButton.Enabled
                ? _theme.PanelBackColor
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
            _datePicker.Visible = useDatePicker;
            _valueTextBox.Visible = !useDatePicker;
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
            button.FlatAppearance.MouseOverBackColor = _theme.NeutralHoverColor;
            button.FlatAppearance.MouseDownBackColor = _theme.NeutralPressedColor;
            button.BackColor = _theme.PanelBackColor;
            button.ForeColor = _theme.TextColor;
            button.FlatAppearance.BorderColor = _theme.BorderColor;
            button.Font = Font;
        }
    }
}
