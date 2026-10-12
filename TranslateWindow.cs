using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace TranslateDesktop;

internal sealed class TranslateWindow : Form
{
    private readonly WebView2 _browser;
    private readonly Panel _loadingPanel;
    private readonly Label _loadingText;
    private readonly PictureBox _loadingLogo;
    private readonly Bitmap _logo;
    private readonly Icon _ownedIcon;
    private readonly string _profilePath;
    private readonly Action<Size> _manualSizeChanged;
    private AppSettings _settings;
    private Task? _initializationTask;
    private bool _exitRequested;
    private bool _firstPageReady;
    private bool _ownedResourcesDisposed;

    internal TranslateWindow(
        AppSettings settings,
        string profilePath,
        Icon appIcon,
        Action<Size> manualSizeChanged)
    {
        _settings = settings.Clone();
        _profilePath = profilePath;
        _manualSizeChanged = manualSizeChanged;
        _ownedIcon = (Icon)appIcon.Clone();
        _logo = BrandAssets.LoadLogo();

        Text = "Google Translate";
        Icon = _ownedIcon;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.White;
        ClientSize = new Size(_settings.WindowWidth, _settings.WindowHeight);
        MinimumSize = new Size(560, 420);
        StartPosition = FormStartPosition.CenterScreen;

        _browser = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.White,
        };

        _loadingText = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.FromArgb(71, 85, 105),
            Visible = false,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        _loadingLogo = new PictureBox
        {
            Image = _logo,
            Size = new Size(92, 92),
            SizeMode = PictureBoxSizeMode.Zoom,
        };

        _loadingPanel = new Panel { BackColor = Color.White, Dock = DockStyle.Fill };
        _loadingPanel.Controls.Add(_loadingLogo);
        _loadingPanel.Controls.Add(_loadingText);
        _loadingPanel.Resize += (_, _) => LayoutLoadingContent();

        Controls.Add(_browser);
        Controls.Add(_loadingPanel);
        _loadingPanel.BringToFront();
        LayoutLoadingContent();
        ResizeEnd += (_, _) => PersistManualSize();
    }

    internal void UpdateSettings(AppSettings settings)
    {
        _settings = settings.Clone();
    }

    internal async Task ShowAndNavigateAsync(string url, bool openedByShortcut)
    {
        var sourceScreen = ResolveSourceScreen();
        if (openedByShortcut && _settings.ApplyWindowSizeOnShortcut)
        {
            ApplyConfiguredBounds(sourceScreen);
        }

        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        if (!Visible)
        {
            Show();
        }

        Activate();
        BringToFront();

        try
        {
            await EnsureBrowserAsync();
            _browser.CoreWebView2.Navigate(url);
            _browser.Focus();
        }
        catch (Exception exception)
        {
            ShowLoadingError($"Google Translate could not start.\r\n{exception.GetType().Name}");
        }
    }

    internal void ExitApplication()
    {
        _exitRequested = true;
        Close();
    }

    internal void HideFromShortcut()
    {
        if (Visible)
        {
            Hide();
        }
    }

    internal void ApplyConfiguredBoundsForTest(Rectangle workingArea)
    {
        WindowState = FormWindowState.Normal;
        Bounds = WindowSizing.CalculateShortcutBounds(_settings, MinimumSize, workingArea);
    }

    internal (Point LogoLocation, Size LogoSize, bool TextVisible) LayoutLoadingForTest(Size clientSize)
    {
        _loadingPanel.ClientSize = clientSize;
        LayoutLoadingContent();
        return (_loadingLogo.Location, _loadingLogo.Size, _loadingText.Visible);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_ownedResourcesDisposed)
        {
            _ownedResourcesDisposed = true;
            _browser.Dispose();
            _logo.Dispose();
            _ownedIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private Task EnsureBrowserAsync()
    {
        return _initializationTask ??= InitializeBrowserAsync();
    }

    private async Task InitializeBrowserAsync()
    {
        Directory.CreateDirectory(_profilePath);
        var environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: _profilePath);
        await _browser.EnsureCoreWebView2Async(environment);

        var core = _browser.CoreWebView2;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = true;
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.IsZoomControlEnabled = true;
        core.NewWindowRequested += HandleNewWindow;
        core.NavigationCompleted += HandleNavigationCompleted;
        core.ProcessFailed += HandleProcessFailed;
    }

    private void HandleNewWindow(object? sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
        {
            args.Handled = true;
            _browser.CoreWebView2.Navigate(uri.AbsoluteUri);
        }
    }

    private void HandleNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (args.IsSuccess && !_firstPageReady)
        {
            _firstPageReady = true;
            _loadingPanel.Visible = false;
        }
        else if (!args.IsSuccess && !_firstPageReady)
        {
            ShowLoadingError("Google Translate could not be reached. Check the internet connection and try again.");
        }
    }

    private void HandleProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs args)
    {
        if (args.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited)
        {
            _initializationTask = null;
            _firstPageReady = false;
            ShowLoadingError("The translation window stopped. Reopen it to try again.");
        }
    }

    private void ShowLoadingError(string message)
    {
        _loadingText.Text = message;
        _loadingText.Visible = true;
        _loadingPanel.Visible = true;
        LayoutLoadingContent();
        _loadingPanel.BringToFront();
    }

    private void LayoutLoadingContent()
    {
        var width = _loadingPanel.ClientSize.Width;
        var height = _loadingPanel.ClientSize.Height;
        if (!_loadingText.Visible)
        {
            _loadingLogo.Location = new Point(
                Math.Max(0, (width - _loadingLogo.Width) / 2),
                Math.Max(0, (height - _loadingLogo.Height) / 2));
            return;
        }

        _loadingText.MaximumSize = new Size(Math.Max(180, width - 48), 0);
        var gap = 12;
        var contentHeight = _loadingLogo.Height + gap + _loadingText.Height;
        var top = Math.Max(0, (height - contentHeight) / 2);
        _loadingLogo.Location = new Point(Math.Max(0, (width - _loadingLogo.Width) / 2), top);
        _loadingText.Location = new Point(
            Math.Max(0, (width - _loadingText.Width) / 2),
            top + _loadingLogo.Height + gap);
    }

    private Screen ResolveSourceScreen()
    {
        var foreground = GetForegroundWindow();
        return foreground == IntPtr.Zero ? Screen.PrimaryScreen! : Screen.FromHandle(foreground);
    }

    private void ApplyConfiguredBounds(Screen screen)
    {
        ApplyConfiguredBoundsForTest(screen.WorkingArea);
    }

    private void PersistManualSize()
    {
        if (!_settings.ApplyWindowSizeOnShortcut && WindowState == FormWindowState.Normal)
        {
            _manualSizeChanged(Size);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
