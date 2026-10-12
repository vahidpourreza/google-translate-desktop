using System.Runtime.InteropServices;

namespace TranslateDesktop;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int TranslateSelectionHotkeyId = 1;
    private const int OpenHotkeyId = 2;
    private const int TranslateClipboardHotkeyId = 3;
    private const int HideWindowHotkeyId = 4;
    private readonly HashSet<int> _registeredIds = [];

    internal HotkeyWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "TranslateDesktopHotkeys",
            Parent = new IntPtr(-3), // HWND_MESSAGE
        });
    }

    internal event Action<AppAction>? ActionRequested;

    internal bool TryRegister(AppSettings settings, out string? error)
    {
        UnregisterAll();

        if (!Register(TranslateSelectionHotkeyId, settings.TranslateSelectionHotkey))
        {
            error = $"{settings.TranslateSelectionHotkey.DisplayText} is already used by another application.";
            return false;
        }

        if (!Register(OpenHotkeyId, settings.OpenHotkey))
        {
            UnregisterAll();
            error = $"{settings.OpenHotkey.DisplayText} is already used by another application.";
            return false;
        }

        if (!Register(TranslateClipboardHotkeyId, settings.TranslateClipboardHotkey))
        {
            UnregisterAll();
            error = $"{settings.TranslateClipboardHotkey.DisplayText} is already used by another application.";
            return false;
        }

        if (!Register(HideWindowHotkeyId, settings.HideWindowHotkey))
        {
            UnregisterAll();
            error = $"{settings.HideWindowHotkey.DisplayText} is already used by another application.";
            return false;
        }

        error = null;
        return true;
    }

    internal void UnregisterAll()
    {
        foreach (var id in _registeredIds)
        {
            UnregisterHotKey(Handle, id);
        }

        _registeredIds.Clear();
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmHotkey)
        {
            var id = message.WParam.ToInt32();
            if (id == TranslateSelectionHotkeyId)
            {
                ActionRequested?.Invoke(AppAction.TranslateSelection);
            }
            else if (id == OpenHotkeyId)
            {
                ActionRequested?.Invoke(AppAction.Open);
            }
            else if (id == TranslateClipboardHotkeyId)
            {
                ActionRequested?.Invoke(AppAction.TranslateClipboard);
            }
            else if (id == HideWindowHotkeyId)
            {
                ActionRequested?.Invoke(AppAction.HideWindow);
            }
        }

        base.WndProc(ref message);
    }

    public void Dispose()
    {
        UnregisterAll();
        DestroyHandle();
    }

    private bool Register(int id, HotkeyBinding binding)
    {
        var modifiers = binding.Modifiers | HotkeyModifiers.NoRepeat;
        if (!RegisterHotKey(Handle, id, (uint)modifiers, (uint)(binding.Key & Keys.KeyCode)))
        {
            return false;
        }

        _registeredIds.Add(id);
        return true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);
}
