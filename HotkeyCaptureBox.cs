namespace TranslateDesktop;

internal sealed class HotkeyCaptureBox : TextBox
{
    private HotkeyBinding _binding;

    internal HotkeyCaptureBox(HotkeyBinding binding)
    {
        _binding = binding.Clone();
        ReadOnly = true;
        ShortcutsEnabled = false;
        TextAlign = HorizontalAlignment.Center;
        TabStop = true;
        SetBinding(binding);
    }

    internal event Action<string?>? ValidationMessageChanged;

    internal HotkeyBinding Binding => _binding.Clone();

    internal void SetBinding(HotkeyBinding binding)
    {
        _binding = binding.Clone();
        Text = _binding.DisplayText;
        ValidationMessageChanged?.Invoke(null);
    }

    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        SelectAll();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;

        var key = e.KeyCode & Keys.KeyCode;
        if (key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu
            or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.LWin or Keys.RWin)
        {
            return;
        }

        if (key == Keys.Escape)
        {
            FindForm()?.Close();
            return;
        }

        var modifiers = HotkeyModifiers.None;
        if (e.Control)
        {
            modifiers |= HotkeyModifiers.Control;
        }

        if (e.Alt)
        {
            modifiers |= HotkeyModifiers.Alt;
        }

        if (e.Shift)
        {
            modifiers |= HotkeyModifiers.Shift;
        }

        var candidate = new HotkeyBinding(modifiers, key);
        if (!candidate.IsValid())
        {
            ValidationMessageChanged?.Invoke("Use Ctrl, Alt, or Shift together with a letter, number, function, navigation, or Space key.");
            return;
        }

        SetBinding(candidate);
    }
}
