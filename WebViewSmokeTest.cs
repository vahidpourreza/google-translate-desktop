using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace TranslateDesktop;

internal static class WebViewSmokeTest
{
    internal static bool IsRequested(string[] args)
    {
        return args.Contains("--webview-self-test", StringComparer.OrdinalIgnoreCase);
    }

    internal static int Run(string[] args)
    {
        var outputPath = SelfTest.GetArgument(args, "--test-output")
            ?? Path.Combine(Path.GetTempPath(), "translate-desktop-webview-test.json");
        var profilePath = SelfTest.GetArgument(args, "--profile-path")
            ?? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outputPath))!, "webview-smoke-profile");

        using var form = new SmokeTestForm(outputPath, profilePath);
        Application.Run(form);
        return form.ExitCode;
    }

    private sealed class SmokeTestForm : Form
    {
        private readonly string _outputPath;
        private readonly string _profilePath;
        private readonly WebView2 _browser;

        internal SmokeTestForm(string outputPath, string profilePath)
        {
            _outputPath = Path.GetFullPath(outputPath);
            _profilePath = Path.GetFullPath(profilePath);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-32000, -32000);
            Size = new Size(900, 700);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            _browser = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(_browser);
            Shown += RunTest;
        }

        internal int ExitCode { get; private set; } = 1;

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var value = base.CreateParams;
                value.ExStyle |= 0x08000000 | 0x80; // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW
                return value;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _browser.Dispose();
            base.OnFormClosed(e);
        }

        private async void RunTest(object? sender, EventArgs e)
        {
            var passed = false;
            string? title = null;
            string? source = null;
            string? version = null;
            string? error = null;

            try
            {
                Directory.CreateDirectory(_profilePath);
                var options = new CoreWebView2EnvironmentOptions("--disable-backgrounding-occluded-windows");
                var environment = await CoreWebView2Environment.CreateAsync(null, _profilePath, options);
                await _browser.EnsureCoreWebView2Async(environment);
                version = _browser.CoreWebView2.Environment.BrowserVersionString;

                var navigation = new TaskCompletionSource<CoreWebView2NavigationCompletedEventArgs>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                void Completed(object? navigationSender, CoreWebView2NavigationCompletedEventArgs navigationArgs)
                    => navigation.TrySetResult(navigationArgs);

                _browser.CoreWebView2.NavigationCompleted += Completed;
                _browser.CoreWebView2.Navigate(TranslationUrl.Home);
                var result = await navigation.Task.WaitAsync(TimeSpan.FromSeconds(30));
                _browser.CoreWebView2.NavigationCompleted -= Completed;

                source = _browser.CoreWebView2.Source;
                var titleJson = await _browser.CoreWebView2.ExecuteScriptAsync("document.title");
                title = JsonSerializer.Deserialize<string>(titleJson);
                passed = result.IsSuccess
                    && Uri.TryCreate(source, UriKind.Absolute, out var uri)
                    && uri.Host.Equals("translate.google.com", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(title);
            }
            catch (Exception exception)
            {
                error = exception.GetType().Name + ": " + exception.Message;
            }

            var report = new
            {
                Passed = passed,
                Offscreen = Left < -10000 && !ShowInTaskbar,
                Source = source,
                Title = title,
                WebView2Version = version,
                Error = error,
                CompletedAtUtc = DateTimeOffset.UtcNow,
            };
            Directory.CreateDirectory(Path.GetDirectoryName(_outputPath)!);
            await File.WriteAllTextAsync(
                _outputPath,
                JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

            ExitCode = passed && report.Offscreen ? 0 : 1;
            Close();
        }
    }
}
