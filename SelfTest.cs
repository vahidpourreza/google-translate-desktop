using System.Drawing;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace TranslateDesktop;

internal static class SelfTest
{
    internal static bool IsRequested(string[] args)
    {
        return args.Contains("--self-test", StringComparer.OrdinalIgnoreCase);
    }

    internal static int Run(string[] args)
    {
        var checks = new List<SelfTestCheck>();

        string? browserVersion = null;
        try
        {
            browserVersion = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch
        {
            // Reported by the runtime check below.
        }

        Add(checks, "webview2-runtime-found", !string.IsNullOrWhiteSpace(browserVersion));

        var englishUrl = TranslationUrl.ForText("Good morning");
        Add(checks, "english-targets-persian", englishUrl.Contains("tl=fa", StringComparison.Ordinal));
        Add(checks, "english-text-encoded", englishUrl.Contains("Good%20morning", StringComparison.Ordinal));

        var persianUrl = TranslationUrl.ForText("صبح بخیر");
        Add(checks, "persian-targets-english", persianUrl.Contains("tl=en", StringComparison.Ordinal));
        Add(checks, "long-text-falls-back", TranslationUrl.ForText(new string('a', TranslationUrl.MaximumQuickTextLength + 1)) == TranslationUrl.ForHome(new AppSettings()));

        var fixedLanguages = new AppSettings { SourceLanguageCode = "en", TargetLanguageCode = "de" };
        var fixedLanguageUrl = TranslationUrl.ForText("Good morning", fixedLanguages);
        Add(checks, "configured-languages-apply", fixedLanguageUrl.Contains("sl=en", StringComparison.Ordinal)
            && fixedLanguageUrl.Contains("tl=de", StringComparison.Ordinal));
        Add(checks, "language-catalog-includes-persian-and-english",
            TranslationLanguageCatalog.SourceOptions.Any(option => option.Code == "fa")
            && TranslationLanguageCatalog.SourceOptions.Any(option => option.Code == "en")
            && TranslationLanguageCatalog.TargetOptions.Count > 100);

        var settings = new AppSettings
        {
            TranslateSelectionHotkey = new HotkeyBinding(HotkeyModifiers.Control | HotkeyModifiers.Shift, Keys.Y),
            TranslateClipboardHotkey = new HotkeyBinding(HotkeyModifiers.Control | HotkeyModifiers.Shift, Keys.U),
            OpenHotkey = new HotkeyBinding(HotkeyModifiers.Alt, Keys.F8),
            HideWindowHotkey = new HotkeyBinding(HotkeyModifiers.Control | HotkeyModifiers.Shift, Keys.W),
            WindowWidth = 1120,
            WindowHeight = 700,
            ApplyWindowSizeOnShortcut = false,
            SourceLanguageCode = "en",
            TargetLanguageCode = "fr",
        };
        var roundTrip = AppSettingsStore.Deserialize(AppSettingsStore.Serialize(settings));
        Add(checks, "settings-round-trip", roundTrip.TranslateSelectionHotkey.IsSameAs(settings.TranslateSelectionHotkey)
            && roundTrip.TranslateClipboardHotkey.IsSameAs(settings.TranslateClipboardHotkey)
            && roundTrip.OpenHotkey.IsSameAs(settings.OpenHotkey)
            && roundTrip.HideWindowHotkey.IsSameAs(settings.HideWindowHotkey)
            && roundTrip.WindowWidth == settings.WindowWidth
            && roundTrip.WindowHeight == settings.WindowHeight
            && !roundTrip.ApplyWindowSizeOnShortcut
            && roundTrip.SourceLanguageCode == settings.SourceLanguageCode
            && roundTrip.TargetLanguageCode == settings.TargetLanguageCode);
        Add(checks, "hotkey-display", HotkeyBinding.DefaultTranslateSelection.DisplayText == "Ctrl+Alt+T");
        Add(checks, "clipboard-hotkey-display", HotkeyBinding.DefaultTranslateClipboard.DisplayText == "Ctrl+Alt+V");
        Add(checks, "hide-hotkey-display", HotkeyBinding.DefaultHideWindow.DisplayText == "Ctrl+Alt+W");
        Add(checks, "native-copy-input-layout", SelectedTextCapture.NativeInputSize == (IntPtr.Size == 8 ? 40 : 28));

        var sizingSettings = new AppSettings
        {
            WindowWidth = 840,
            WindowHeight = 620,
            ApplyWindowSizeOnShortcut = true,
        };
        var configuredBounds = WindowSizing.CalculateShortcutBounds(
            sizingSettings,
            new Size(560, 420),
            new Rectangle(100, 50, 1920, 1040));
        Add(checks, "configured-window-size-applies-and-centers", configuredBounds == new Rectangle(640, 260, 840, 620));

        var nativeWindowSizeApplied = false;
        var loadingLayoutCorrect = false;
        try
        {
            using var icon = BrandAssets.LoadAppIcon();
            using var nativeWindow = new TranslateWindow(
                sizingSettings,
                Path.Combine(Path.GetTempPath(), "TranslateDesktopSelfTestProfile"),
                icon,
                _ => { });
            nativeWindow.ApplyConfiguredBoundsForTest(new Rectangle(100, 50, 1920, 1040));
            nativeWindowSizeApplied = nativeWindow.Bounds == new Rectangle(640, 260, 840, 620);
            var loadingLayout = nativeWindow.LayoutLoadingForTest(new Size(560, 420));
            loadingLayoutCorrect = loadingLayout.LogoLocation == new Point(234, 164)
                && loadingLayout.LogoSize == new Size(92, 92)
                && !loadingLayout.TextVisible;
        }
        catch
        {
            // Reported by the check below.
        }

        Add(checks, "configured-size-reaches-native-window", nativeWindowSizeApplied);
        Add(checks, "loading-logo-centered-without-text", loadingLayoutCorrect);

        sizingSettings.WindowWidth = 2560;
        sizingSettings.WindowHeight = 1600;
        var clampedBounds = WindowSizing.CalculateShortcutBounds(
            sizingSettings,
            new Size(560, 420),
            new Rectangle(-1280, 0, 1280, 720));
        Add(checks, "configured-window-size-clamps-to-monitor", clampedBounds == new Rectangle(-1280, 0, 1280, 720));

        var customHotkeysRegistered = false;
        try
        {
            using var hotkeys = new HotkeyWindow();
            var testSettings = new AppSettings
            {
                TranslateSelectionHotkey = new HotkeyBinding(
                    HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
                    Keys.F21),
                TranslateClipboardHotkey = new HotkeyBinding(
                    HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
                    Keys.F22),
                OpenHotkey = new HotkeyBinding(
                    HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
                    Keys.F23),
                HideWindowHotkey = new HotkeyBinding(
                    HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
                    Keys.F24),
            };
            customHotkeysRegistered = hotkeys.TryRegister(testSettings, out _);
            hotkeys.UnregisterAll();
        }
        catch
        {
            // Reported by the check below.
        }

        Add(checks, "custom-global-hotkeys-register", customHotkeysRegistered);

        var logoAvailable = false;
        try
        {
            using var logo = BrandAssets.LoadLogo();
            logoAvailable = logo.Width == 512 && logo.Height == 512;
        }
        catch
        {
            // Reported by the check below.
        }

        Add(checks, "official-logo-packaged-at-source-size", logoAvailable);

        var iconAvailable = false;
        try
        {
            using var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            iconAvailable = icon is not null;
        }
        catch
        {
            // Reported by the check below.
        }

        Add(checks, "packaged-icon", iconAvailable);

        var settingsLayoutRendered = false;
        Bitmap? settingsPreview = null;
        try
        {
            using var icon = BrandAssets.LoadAppIcon();
            using var settingsForm = new SettingsForm(new AppSettings(), icon, testing: true)
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-32000, -32000),
            };
            settingsForm.Show();
            Application.DoEvents();
            settingsForm.PerformLayout();
            settingsPreview = new Bitmap(settingsForm.Width, settingsForm.Height);
            settingsForm.DrawToBitmap(settingsPreview, new Rectangle(Point.Empty, settingsForm.Size));
            settingsLayoutRendered = settingsPreview.Width >= 560
                && settingsPreview.Height >= 599
                && settingsForm.Controls.Find("saveButton", searchAllChildren: true).Length == 1
                && settingsForm.Left < -10000
                && !settingsForm.ShowInTaskbar;
            settingsForm.Hide();
        }
        catch
        {
            settingsPreview?.Dispose();
            settingsPreview = null;
        }

        Add(checks, "settings-layout-rendered", settingsLayoutRendered);

        var report = new SelfTestReport(
            checks.All(check => check.Passed),
            browserVersion,
            checks,
            DateTimeOffset.UtcNow);

        var outputPath = GetArgument(args, "--test-output");
        if (outputPath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
            File.WriteAllText(
                outputPath,
                JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            settingsPreview?.Save(
                Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outputPath))!, "settings-layout.png"));
        }

        settingsPreview?.Dispose();

        return report.Passed ? 0 : 1;
    }

    internal static string? GetArgument(string[] args, string name)
    {
        var index = Array.FindIndex(args, value => value.Equals(name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static void Add(ICollection<SelfTestCheck> checks, string name, bool passed)
    {
        checks.Add(new SelfTestCheck(name, passed));
    }

    private sealed record SelfTestCheck(string Name, bool Passed);

    private sealed record SelfTestReport(
        bool Passed,
        string? WebView2Version,
        IReadOnlyList<SelfTestCheck> Checks,
        DateTimeOffset CompletedAtUtc);
}
