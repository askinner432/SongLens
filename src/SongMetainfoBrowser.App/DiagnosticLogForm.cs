using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SongMetainfoBrowser.App;

internal sealed class DiagnosticLogForm : Form
{
    private readonly AppTheme _theme;
    private readonly TextBox _logTextBox;
    private readonly Label _actionStatusLabel;
    private readonly Button _copyButton;
    private readonly string _entireSessionLog;
    private readonly string? _selectedSongPath;

    public DiagnosticLogForm(AppTheme theme, string? selectedSongPath)
    {
        var fontPreferences = AppFontSettings.LoadPreferences();
        _theme = theme;
        _selectedSongPath = selectedSongPath;
        _entireSessionLog = ReadDiagnosticLog();

        Text = "SongLens Diagnostic Log";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = AppFontSettings.Scale(new Size(620, 400), fontPreferences, AppFontSection.Dialogs);
        ClientSize = AppFontSettings.Scale(new Size(780, 520), fontPreferences, AppFontSection.Dialogs);
        Font = AppFontSettings.CreateUiFont(fontPreferences, AppFontSection.Dialogs);
        BackColor = _theme.AppBackColor;
        ForeColor = _theme.TextColor;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(12),
            BackColor = _theme.AppBackColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var descriptionLabel = new Label
        {
            AutoSize = true,
            Text = "Diagnostic details are stored locally and are never sent automatically.",
            ForeColor = _theme.MutedTextColor,
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.Controls.Add(descriptionLabel, 0, 0);

        var scopePanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = _theme.AppBackColor,
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.Controls.Add(scopePanel, 0, 1);

        var scopeLabel = new Label
        {
            AutoSize = true,
            Text = "Show:",
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 3, 8, 0)
        };
        scopePanel.Controls.Add(scopeLabel);

        var selectedSongRadioButton = new RadioButton
        {
            AutoSize = true,
            Text = "Selected Song",
            Enabled = !string.IsNullOrWhiteSpace(_selectedSongPath),
            Checked = !string.IsNullOrWhiteSpace(_selectedSongPath),
            BackColor = _theme.AppBackColor,
            ForeColor = _theme.TextColor,
            Margin = new Padding(0, 0, 14, 0)
        };
        scopePanel.Controls.Add(selectedSongRadioButton);

        var entireSessionRadioButton = new RadioButton
        {
            AutoSize = true,
            Text = "Entire Session",
            Checked = string.IsNullOrWhiteSpace(_selectedSongPath),
            BackColor = _theme.AppBackColor,
            ForeColor = _theme.TextColor,
            Margin = Padding.Empty
        };
        scopePanel.Controls.Add(entireSessionRadioButton);

        _logTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            BackColor = _theme.PanelBackColor,
            ForeColor = _theme.TextColor,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", Font.Size),
            Margin = Padding.Empty
        };
        layout.Controls.Add(_logTextBox, 0, 2);

        var actionPanel = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 10, 0, 0),
            BackColor = _theme.AppBackColor
        };
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(actionPanel, 0, 3);

        _actionStatusLabel = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = _theme.MutedTextColor,
            Margin = Padding.Empty
        };
        actionPanel.Controls.Add(_actionStatusLabel, 0, 0);

        var buttonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            BackColor = _theme.AppBackColor
        };
        actionPanel.Controls.Add(buttonPanel, 1, 0);

        _copyButton = CreateButton("Copy Displayed");
        _copyButton.Click += (_, _) => CopyLog();
        buttonPanel.Controls.Add(_copyButton);

        var openFolderButton = CreateButton("Open Folder");
        openFolderButton.Click += (_, _) => OpenLogFolder();
        buttonPanel.Controls.Add(openFolderButton);

        var closeButton = CreateButton("Close");
        closeButton.DialogResult = DialogResult.Cancel;
        buttonPanel.Controls.Add(closeButton);
        CancelButton = closeButton;

        selectedSongRadioButton.CheckedChanged += (_, _) =>
        {
            if (selectedSongRadioButton.Checked)
            {
                ShowSelectedSongLog();
            }
        };
        entireSessionRadioButton.CheckedChanged += (_, _) =>
        {
            if (entireSessionRadioButton.Checked)
            {
                ShowEntireSessionLog();
            }
        };

        if (selectedSongRadioButton.Checked)
        {
            ShowSelectedSongLog();
        }
        else
        {
            ShowEntireSessionLog();
        }
    }

    private void ShowSelectedSongLog()
    {
        var entries = Regex.Split(
                _entireSessionLog,
                @"(?=^\[\d{4}-\d{2}-\d{2}T)",
                RegexOptions.Multiline | RegexOptions.CultureInvariant)
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Where(entry => !string.IsNullOrWhiteSpace(_selectedSongPath)
                && entry.Contains(_selectedSongPath, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.TrimEnd())
            .ToArray();

        SetDisplayedLog(entries.Length == 0
            ? "No diagnostic messages have been recorded for the selected song."
            : string.Join(Environment.NewLine + Environment.NewLine, entries));
    }

    private void ShowEntireSessionLog()
    {
        SetDisplayedLog(string.IsNullOrWhiteSpace(_entireSessionLog)
            ? "No diagnostic messages have been recorded for this session."
            : _entireSessionLog);
    }

    private void SetDisplayedLog(string text)
    {
        _logTextBox.Text = text;
        _logTextBox.SelectionStart = 0;
        _logTextBox.SelectionLength = 0;
        _copyButton.Enabled = !string.IsNullOrWhiteSpace(text)
            && !text.StartsWith("No diagnostic messages", StringComparison.Ordinal);
        _actionStatusLabel.Text = "";
    }

    private static string ReadDiagnosticLog()
    {
        try
        {
            return File.Exists(AppPaths.DiagnosticLogPath)
                ? File.ReadAllText(AppPaths.DiagnosticLogPath)
                : "No diagnostic messages have been recorded for this session.";
        }
        catch (Exception exception)
        {
            return $"SongLens could not read the diagnostic log.{Environment.NewLine}{Environment.NewLine}{exception.Message}";
        }
    }

    private void CopyLog()
    {
        try
        {
            Clipboard.SetText(_logTextBox.Text);
            _actionStatusLabel.Text = "Copied to clipboard.";
        }
        catch (Exception exception)
        {
            ShowError($"Could not copy the diagnostic log.\n\n{exception.Message}");
        }
    }

    private void OpenLogFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{AppPaths.DataDirectory}\"",
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            ShowError($"Could not open the diagnostic folder.\n\n{exception.Message}");
        }
    }

    private Button CreateButton(string text)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = _theme.PanelBackColor,
            ForeColor = _theme.TextColor,
            Font = Font,
            Margin = new Padding(6, 0, 0, 0)
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = _theme.BorderColor;
        button.FlatAppearance.MouseOverBackColor = _theme.NeutralHoverColor;
        button.FlatAppearance.MouseDownBackColor = _theme.NeutralPressedColor;
        return button;
    }

    private void ShowError(string message)
    {
        using var dialog = new ThemedMessageForm(Text, message, _theme, ThemedMessageKind.Error);
        dialog.ShowDialog(this);
    }
}
