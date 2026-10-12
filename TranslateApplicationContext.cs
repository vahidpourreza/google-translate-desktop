using System.Drawing;

namespace TranslateDesktop;

internal sealed class TranslateApplicationContext : ApplicationContext
{
    private readonly AppSettingsStore _settingsStore = new();
    private readonly Control _dispatcher = new();
    private readonly HotkeyWindow _hotkeyWindow;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _trayMenu;
    private readonly RegisteredWaitHandle _openWait;
    private readonly RegisteredWaitHandle _selectionWait;
    private readonly RegisteredWaitHandle _clipboardWait;
    private readonly ToolStripMenuItem _openItem;
    private readonly ToolStripMenuItem _selectionItem;
    private readonly ToolStripMenuItem _clipboardItem;
    private readonly ToolStripMenuItem _hideItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly Icon _appIcon;
    private readonly Bitmap _menuLogo;
    private readonly SemaphoreSlim _selectionGate = new(1, 1);
    private AppSettings _settings;
    private TranslateWindow? _window;
    private bool _isExiting;
    private string? _hotkeyError;

    internal TranslateApplicationContext(
        SingleInstance instance,
        bool startInBackground,
        AppAction requestedAction)
    {
        _settings = _settingsStore.Load();
        _dispatcher.CreateControl();
        _appIcon = BrandAssets.LoadAppIcon();
        using var fullLogo = BrandAssets.LoadLogo();
        _menuLogo = new Bitmap(fullLogo, new Size(18, 18));

        _hotkeyWindow = new HotkeyWindow();
        _hotkeyWindow.ActionRequested += HandleAction;
        if (!_hotkeyWindow.TryRegister(_settings, out _hotkeyError))
        {
            _hotkeyError ??= "The global shortcuts are unavailable.";
        }

        _trayMenu = new ContextMenuStrip();
        _selectionItem = new ToolStripMenuItem(null, _menuLogo) { Enabled = false };
        _clipboardItem = new ToolStripMenuItem("Translate clipboard", null, (_, _) => HandleAction(AppAction.TranslateClipboard));
        _openItem = new ToolStripMenuItem(null, null, (_, _) => HandleAction(AppAction.Open));
        _hideItem = new ToolStripMenuItem(null, null, (_, _) => HandleAction(AppAction.HideWindow));
        var settingsItem = new ToolStripMenuItem("Settings…", null, (_, _) => OpenSettings());

        _startupItem = new ToolStripMenuItem("Launch at sign-in")
        {
            Checked = StartupRegistration.IsEnabled(),
            CheckOnClick = false,
        };
        _startupItem.Click += ToggleStartup;

        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitApplication());
        _trayMenu.Items.Add(_selectionItem);
        _trayMenu.Items.Add(_clipboardItem);
        _trayMenu.Items.Add(_openItem);
        _trayMenu.Items.Add(_hideItem);
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(settingsItem);
        _trayMenu.Items.Add(_startupItem);
        _trayMenu.Items.Add(new ToolStripSeparator());
        _trayMenu.Items.Add(exitItem);
        UpdateMenuText();

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _trayMenu,
            Icon = _appIcon,
            Text = "Google Translate — select text and use the shortcut",
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => HandleAction(AppAction.Open);

        _openWait = RegisterWait(instance.OpenEvent, AppAction.Open);
        _selectionWait = RegisterWait(instance.SelectionEvent, AppAction.TranslateSelection);
        _clipboardWait = RegisterWait(instance.ClipboardEvent, AppAction.TranslateClipboard);

        if (!startInBackground)
        {
            _dispatcher.BeginInvoke(() => HandleAction(requestedAction));
        }
    }

    private RegisteredWaitHandle RegisterWait(EventWaitHandle signal, AppAction action)
    {
        return ThreadPool.RegisterWaitForSingleObject(
            signal,
            (_, _) => Dispatch(action),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    private void Dispatch(AppAction action)
    {
        if (_isExiting || _dispatcher.IsDisposed)
        {
            return;
        }

        try
        {
            _dispatcher.BeginInvoke(() => HandleAction(action));
        }
        catch (InvalidOperationException)
        {
            // The message loop is shutting down.
        }
    }

    private async void HandleAction(AppAction action)
    {
        if (_isExiting)
        {
            return;
        }

        if (action == AppAction.HideWindow)
        {
            _window?.HideFromShortcut();
            return;
        }

        if (action == AppAction.TranslateSelection)
        {
            if (!await _selectionGate.WaitAsync(0))
            {
                return;
            }

            try
            {
                var selectedText = await SelectedTextCapture.CaptureAsync();
                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    ShowNoSelectionNotice();
                    return;
                }

                await ShowTranslateAsync(TranslationUrl.ForText(selectedText, _settings), openedByShortcut: true);
            }
            finally
            {
                _selectionGate.Release();
            }

            return;
        }

        if (action == AppAction.TranslateClipboard)
        {
            var clipboardText = await SelectedTextCapture.ReadClipboardTextAsync();
            await ShowTranslateAsync(TranslationUrl.ForText(clipboardText ?? string.Empty, _settings), openedByShortcut: true);
            return;
        }

        await ShowTranslateAsync(TranslationUrl.ForHome(_settings), openedByShortcut: true);
    }

    private Task ShowTranslateAsync(string url, bool openedByShortcut)
    {
        _window ??= new TranslateWindow(
            _settings,
            _settingsStore.WebViewProfilePath,
            _appIcon,
            PersistManualWindowSize);
        return _window.ShowAndNavigateAsync(url, openedByShortcut);
    }

    private void ShowNoSelectionNotice()
    {
        _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        _notifyIcon.BalloonTipTitle = "No selected text found";
        _notifyIcon.BalloonTipText = "Select text in the active app, then press the translation shortcut again.";
        _notifyIcon.ShowBalloonTip(3500);
    }

    private void OpenSettings()
    {
        if (_isExiting)
        {
            return;
        }

        var previous = _settings.Clone();
        var editing = previous.Clone();
        _hotkeyWindow.UnregisterAll();

        while (true)
        {
            using var dialog = new SettingsForm(editing, _appIcon);
            var result = _window is { Visible: true }
                ? dialog.ShowDialog(_window)
                : dialog.ShowDialog();

            if (result != DialogResult.OK)
            {
                _hotkeyWindow.TryRegister(previous, out _hotkeyError);
                UpdateMenuText();
                return;
            }

            editing = dialog.ResultSettings.Clone();
            if (!_hotkeyWindow.TryRegister(editing, out var error))
            {
                MessageBox.Show(
                    error ?? "The selected shortcut could not be registered.",
                    "Shortcut unavailable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                continue;
            }

            _settings = editing;
            _hotkeyError = null;
            _window?.UpdateSettings(_settings);
            TrySaveSettings();
            UpdateMenuText();
            return;
        }
    }

    private void PersistManualWindowSize(Size size)
    {
        _settings.WindowWidth = size.Width;
        _settings.WindowHeight = size.Height;
        TrySaveSettings();
    }

    private void TrySaveSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
        }
        catch
        {
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
            _notifyIcon.BalloonTipTitle = "Settings were not saved";
            _notifyIcon.BalloonTipText = "The current choices work for this session but could not be written to disk.";
            _notifyIcon.ShowBalloonTip(3500);
        }
    }

    private void UpdateMenuText()
    {
        _selectionItem.Text = "Translate selected text";
        _selectionItem.ShortcutKeyDisplayString = _settings.TranslateSelectionHotkey.DisplayText;
        _selectionItem.ToolTipText = "Select text in any app, then press the shortcut to copy and translate it in one step.";
        _clipboardItem.ShortcutKeyDisplayString = _settings.TranslateClipboardHotkey.DisplayText;
        _openItem.Text = "Open Google Translate";
        _openItem.ShortcutKeyDisplayString = _settings.OpenHotkey.DisplayText;
        _hideItem.Text = "Hide translation window";
        _hideItem.ShortcutKeyDisplayString = _settings.HideWindowHotkey.DisplayText;

        if (_hotkeyError is not null)
        {
            _openItem.ToolTipText = _hotkeyError + " Open Settings to choose different shortcuts.";
        }
    }

    private void ToggleStartup(object? sender, EventArgs e)
    {
        var enable = !_startupItem.Checked;
        if (StartupRegistration.SetEnabled(enable))
        {
            _startupItem.Checked = enable;
        }
    }

    private void ExitApplication()
    {
        _window?.ExitApplication();
        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _openWait.Unregister(null);
        _selectionWait.Unregister(null);
        _clipboardWait.Unregister(null);
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _trayMenu.Dispose();
        _hotkeyWindow.ActionRequested -= HandleAction;
        _hotkeyWindow.Dispose();
        if (_window is { IsDisposed: false })
        {
            _window.ExitApplication();
            _window.Dispose();
        }

        _menuLogo.Dispose();
        _appIcon.Dispose();
        _selectionGate.Dispose();
        _dispatcher.Dispose();
        base.ExitThreadCore();
    }
}
