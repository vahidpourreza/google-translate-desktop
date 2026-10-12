namespace TranslateDesktop;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (SelfTest.IsRequested(args))
        {
            return SelfTest.Run(args);
        }

        if (WebViewSmokeTest.IsRequested(args))
        {
            NativeAppIdentity.Initialize();
            return WebViewSmokeTest.Run(args);
        }

        NativeAppIdentity.Initialize();

        using var instance = new SingleInstance();
        var startInBackground = args.Contains("--background", StringComparer.OrdinalIgnoreCase);
        var requestedAction = args.Contains("--selection", StringComparer.OrdinalIgnoreCase)
            ? AppAction.TranslateSelection
            : args.Contains("--clipboard", StringComparer.OrdinalIgnoreCase)
                ? AppAction.TranslateClipboard
                : AppAction.Open;

        if (!instance.IsFirstInstance)
        {
            if (!startInBackground)
            {
                instance.Signal(requestedAction);
            }

            return 0;
        }

        using var context = new TranslateApplicationContext(instance, startInBackground, requestedAction);
        Application.Run(context);
        return 0;
    }
}
