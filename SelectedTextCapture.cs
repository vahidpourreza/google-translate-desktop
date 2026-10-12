using System.Runtime.InteropServices;

namespace TranslateDesktop;

internal static class SelectedTextCapture
{
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12;
    private const int VkShift = 0x10;
    private const int VkLeftWindows = 0x5B;
    private const int VkRightWindows = 0x5C;
    private const ushort VkC = 0x43;
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;

    internal static int NativeInputSize => Marshal.SizeOf<Input>();

    internal static async Task<string?> CaptureAsync()
    {
        var initialSequence = GetClipboardSequenceNumber();
        if (!await WaitForModifierReleaseAsync())
        {
            return null;
        }

        await Task.Delay(35);
        if (!SendCopyShortcut())
        {
            return null;
        }

        for (var attempt = 0; attempt < 40; attempt++)
        {
            await Task.Delay(50);
            if (GetClipboardSequenceNumber() == initialSequence)
            {
                continue;
            }

            var text = await ReadClipboardTextAsync();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        return null;
    }

    internal static async Task<string?> ReadClipboardTextAsync()
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                return Clipboard.ContainsText(TextDataFormat.UnicodeText)
                    ? Clipboard.GetText(TextDataFormat.UnicodeText)
                    : null;
            }
            catch (ExternalException)
            {
                await Task.Delay(35);
            }
        }

        return null;
    }

    private static async Task<bool> WaitForModifierReleaseAsync()
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (!IsDown(VkControl)
                && !IsDown(VkMenu)
                && !IsDown(VkShift)
                && !IsDown(VkLeftWindows)
                && !IsDown(VkRightWindows))
            {
                return true;
            }

            await Task.Delay(20);
        }

        return false;
    }

    private static bool SendCopyShortcut()
    {
        Input[] inputs =
        [
            KeyboardInput(VkControl, keyUp: false),
            KeyboardInput(VkC, keyUp: false),
            KeyboardInput(VkC, keyUp: true),
            KeyboardInput(VkControl, keyUp: true),
        ];

        return SendInput((uint)inputs.Length, inputs, NativeInputSize) == (uint)inputs.Length;
    }

    private static Input KeyboardInput(int virtualKey, bool keyUp)
    {
        return new Input
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInputData
                {
                    VirtualKey = (ushort)virtualKey,
                    Flags = keyUp ? KeyEventKeyUp : 0,
                },
            },
        };
    }

    private static bool IsDown(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        internal uint Type;
        internal InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        internal KeyboardInputData Keyboard;

        [FieldOffset(0)]
        // INPUT's native union is sized by MOUSEINPUT. Keeping that member is
        // required even though this app sends only keyboard input; otherwise
        // SendInput rejects the undersized x64 structure.
        internal MouseInputData Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInputData
    {
        internal ushort VirtualKey;
        internal ushort ScanCode;
        internal uint Flags;
        internal uint Time;
        internal UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputData
    {
        internal int X;
        internal int Y;
        internal uint MouseData;
        internal uint Flags;
        internal uint Time;
        internal UIntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();
}
