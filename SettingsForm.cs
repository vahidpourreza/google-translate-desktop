using System.Drawing;

namespace TranslateDesktop;

internal sealed class SettingsForm : Form
{
    private readonly HotkeyCaptureBox _selectionHotkey;
    private readonly HotkeyCaptureBox _clipboardHotkey;
    private readonly HotkeyCaptureBox _openHotkey;
    private readonly HotkeyCaptureBox _hideHotkey;
    private readonly NumericUpDown _width;
    private readonly NumericUpDown _height;
    private readonly CheckBox _applySize;
    private readonly ComboBox _sourceLanguage;
    private readonly ComboBox _targetLanguage;
    private readonly Label _error;
    private readonly Bitmap _logo;
    private readonly Icon _ownedIcon;
    private readonly bool _testing;

    internal SettingsForm(AppSettings settings, Icon appIcon, bool testing = false)
    {
        _testing = testing;
        ResultSettings = settings.Clone();
        _ownedIcon = (Icon)appIcon.Clone();
        _logo = BrandAssets.LoadLogo();

        Text = "Translate Desktop settings";
        Icon = _ownedIcon;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(560, 599);
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        _selectionHotkey = new HotkeyCaptureBox(settings.TranslateSelectionHotkey) { Dock = DockStyle.Fill };
        _clipboardHotkey = new HotkeyCaptureBox(settings.TranslateClipboardHotkey) { Dock = DockStyle.Fill };
        _openHotkey = new HotkeyCaptureBox(settings.OpenHotkey) { Dock = DockStyle.Fill };
        _hideHotkey = new HotkeyCaptureBox(settings.HideWindowHotkey) { Dock = DockStyle.Fill };
        _selectionHotkey.ValidationMessageChanged += SetError;
        _clipboardHotkey.ValidationMessageChanged += SetError;
        _openHotkey.ValidationMessageChanged += SetError;
        _hideHotkey.ValidationMessageChanged += SetError;

        _width = CreateDimensionInput(settings.WindowWidth, 560, 2560);
        _height = CreateDimensionInput(settings.WindowHeight, 420, 1600);
        _sourceLanguage = CreateLanguageInput(
            TranslationLanguageCatalog.SourceOptions,
            TranslationLanguageCatalog.SourceByCode(settings.SourceLanguageCode));
        _targetLanguage = CreateLanguageInput(
            TranslationLanguageCatalog.TargetOptions,
            TranslationLanguageCatalog.TargetByCode(settings.TargetLanguageCode));
        _applySize = new CheckBox
        {
            AutoSize = true,
            Checked = settings.ApplyWindowSizeOnShortcut,
            Text = "Use this size whenever a global shortcut opens Translate",
            Anchor = AnchorStyles.Left,
        };
        _error = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(185, 28, 28),
            Padding = new Padding(12, 5, 12, 0),
            TextAlign = ContentAlignment.TopLeft,
        };

        Controls.Add(BuildLayout());
        AcceptButton = Controls.Find("saveButton", searchAllChildren: true).OfType<Button>().Single();
        CancelButton = Controls.Find("cancelButton", searchAllChildren: true).OfType<Button>().Single();
    }

    internal AppSettings ResultSettings { get; private set; }

    protected override bool ShowWithoutActivation => _testing || base.ShowWithoutActivation;

    protected override CreateParams CreateParams
    {
        get
        {
            var value = base.CreateParams;
            if (_testing)
            {
                value.ExStyle |= 0x08000000 | 0x80; // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
            }

            return value;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _selectionHotkey.ValidationMessageChanged -= SetError;
            _clipboardHotkey.ValidationMessageChanged -= SetError;
            _openHotkey.ValidationMessageChanged -= SetError;
            _hideHotkey.ValidationMessageChanged -= SetError;
            _logo.Dispose();
            _ownedIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            RowCount = 6,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildHotkeyGroup(), 0, 1);
        root.Controls.Add(BuildLanguageGroup(), 0, 2);
        root.Controls.Add(BuildWindowGroup(), 0, 3);
        root.Controls.Add(_error, 0, 4);
        root.Controls.Add(BuildButtons(), 0, 5);
        return root;
    }

    private Control BuildHeader()
    {
        var panel = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            RowCount = 1,
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var picture = new PictureBox
        {
            Dock = DockStyle.Fill,
            Image = _logo,
            Margin = new Padding(2, 2, 10, 8),
            SizeMode = PictureBoxSizeMode.Zoom,
        };
        var text = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Google Translate\r\nChoose the shortcuts and the size used for quick translation.",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        text.Font = new Font(text.Font.FontFamily, 10F, FontStyle.Regular);
        panel.Controls.Add(picture, 0, 0);
        panel.Controls.Add(text, 1, 0);
        return panel;
    }

    private Control BuildHotkeyGroup()
    {
        var group = new GroupBox { Dock = DockStyle.Fill, Text = "Global shortcuts", Padding = new Padding(10) };
        var table = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, RowCount = 5 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.Controls.Add(CreateFieldLabel("Copy and translate selected text"), 0, 0);
        table.Controls.Add(_selectionHotkey, 1, 0);
        table.Controls.Add(CreateFieldLabel("Translate current clipboard text"), 0, 1);
        table.Controls.Add(_clipboardHotkey, 1, 1);
        table.Controls.Add(CreateFieldLabel("Open or focus Translate"), 0, 2);
        table.Controls.Add(_openHotkey, 1, 2);
        table.Controls.Add(CreateFieldLabel("Hide Translate window"), 0, 3);
        table.Controls.Add(_hideHotkey, 1, 3);
        table.Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Text = "Click a shortcut field, then press the new key combination.",
            Anchor = AnchorStyles.Left,
        }, 0, 4);
        table.SetColumnSpan(table.GetControlFromPosition(0, 4)!, 2);
        group.Controls.Add(table);
        return group;
    }

    private Control BuildWindowGroup()
    {
        var group = new GroupBox { Dock = DockStyle.Fill, Text = "Shortcut window size", Padding = new Padding(10) };
        var table = new TableLayoutPanel { ColumnCount = 4, Dock = DockStyle.Fill, RowCount = 2 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.Controls.Add(CreateFieldLabel("Width"), 0, 0);
        table.Controls.Add(_width, 1, 0);
        table.Controls.Add(CreateFieldLabel("Height"), 2, 0);
        table.Controls.Add(_height, 3, 0);
        table.Controls.Add(_applySize, 0, 1);
        table.SetColumnSpan(_applySize, 4);
        group.Controls.Add(table);
        return group;
    }

    private Control BuildLanguageGroup()
    {
        var group = new GroupBox { Dock = DockStyle.Fill, Text = "Quick-translation languages", Padding = new Padding(10) };
        var table = new TableLayoutPanel { ColumnCount = 4, Dock = DockStyle.Fill, RowCount = 2 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.Controls.Add(CreateFieldLabel("From"), 0, 0);
        table.Controls.Add(_sourceLanguage, 1, 0);
        table.Controls.Add(CreateFieldLabel("To"), 2, 0);
        table.Controls.Add(_targetLanguage, 3, 0);
        var note = new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Text = "Smart sends Persian/Arabic-script text to English and other text to Persian.",
            Anchor = AnchorStyles.Left,
        };
        table.Controls.Add(note, 0, 1);
        table.SetColumnSpan(note, 4);
        group.Controls.Add(table);
        return group;
    }

    private Control BuildButtons()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };
        var save = new Button { Name = "saveButton", Text = "Save", AutoSize = true };
        var cancel = new Button { Name = "cancelButton", Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var reset = new Button { Text = "Reset defaults", AutoSize = true };
        save.Click += SaveSettings;
        reset.Click += (_, _) => ResetDefaults();
        panel.Controls.Add(save);
        panel.Controls.Add(cancel);
        panel.Controls.Add(reset);
        return panel;
    }

    private void SaveSettings(object? sender, EventArgs e)
    {
        var bindings = new[]
        {
            _selectionHotkey.Binding,
            _clipboardHotkey.Binding,
            _openHotkey.Binding,
            _hideHotkey.Binding,
        };
        if (bindings.SelectMany((binding, index) => bindings.Skip(index + 1).Select(other => binding.IsSameAs(other))).Any(same => same))
        {
            SetError("The two actions need different shortcuts.");
            return;
        }

        var sourceLanguage = (TranslationLanguageOption)_sourceLanguage.SelectedItem!;
        var targetLanguage = (TranslationLanguageOption)_targetLanguage.SelectedItem!;
        if (!sourceLanguage.Code.Equals(TranslationLanguageCatalog.AutoCode, StringComparison.OrdinalIgnoreCase)
            && !targetLanguage.Code.Equals(TranslationLanguageCatalog.SmartPersianEnglishCode, StringComparison.OrdinalIgnoreCase)
            && sourceLanguage.Code.Equals(targetLanguage.Code, StringComparison.OrdinalIgnoreCase))
        {
            SetError("The source and target languages need to be different.");
            return;
        }

        ResultSettings = new AppSettings
        {
            TranslateSelectionHotkey = _selectionHotkey.Binding,
            TranslateClipboardHotkey = _clipboardHotkey.Binding,
            OpenHotkey = _openHotkey.Binding,
            HideWindowHotkey = _hideHotkey.Binding,
            WindowWidth = decimal.ToInt32(_width.Value),
            WindowHeight = decimal.ToInt32(_height.Value),
            ApplyWindowSizeOnShortcut = _applySize.Checked,
            SourceLanguageCode = sourceLanguage.Code,
            TargetLanguageCode = targetLanguage.Code,
        };
        ResultSettings.Normalize();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ResetDefaults()
    {
        var defaults = new AppSettings();
        _selectionHotkey.SetBinding(defaults.TranslateSelectionHotkey);
        _clipboardHotkey.SetBinding(defaults.TranslateClipboardHotkey);
        _openHotkey.SetBinding(defaults.OpenHotkey);
        _hideHotkey.SetBinding(defaults.HideWindowHotkey);
        _width.Value = defaults.WindowWidth;
        _height.Value = defaults.WindowHeight;
        _applySize.Checked = defaults.ApplyWindowSizeOnShortcut;
        _sourceLanguage.SelectedItem = TranslationLanguageCatalog.SourceByCode(defaults.SourceLanguageCode);
        _targetLanguage.SelectedItem = TranslationLanguageCatalog.TargetByCode(defaults.TargetLanguageCode);
        SetError(null);
    }

    private void SetError(string? message) => _error.Text = message ?? string.Empty;

    private static Label CreateFieldLabel(string text)
    {
        return new Label { AutoSize = true, Text = text, Anchor = AnchorStyles.Left };
    }

    private static NumericUpDown CreateDimensionInput(int value, int minimum, int maximum)
    {
        return new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = minimum,
            Maximum = maximum,
            Increment = 20,
            Value = Math.Clamp(value, minimum, maximum),
            ThousandsSeparator = true,
        };
    }

    private static ComboBox CreateLanguageInput(
        IEnumerable<TranslationLanguageOption> options,
        TranslationLanguageOption selected)
    {
        var input = new ComboBox
        {
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            IntegralHeight = false,
            MaxDropDownItems = 14,
            DropDownHeight = 300,
        };
        input.Items.AddRange(options.Cast<object>().ToArray());
        input.SelectedItem = input.Items.Cast<TranslationLanguageOption>()
            .First(option => option.Code.Equals(selected.Code, StringComparison.OrdinalIgnoreCase));
        return input;
    }
}
