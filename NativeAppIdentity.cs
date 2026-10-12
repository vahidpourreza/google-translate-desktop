using System.Runtime.InteropServices;

namespace TranslateDesktop;

internal static class NativeAppIdentity
{
    private const string AppUserModelId = "Personal.GoogleTranslate.Desktop.v1";

    internal static void Initialize()
    {
        SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
}
