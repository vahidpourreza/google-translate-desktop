using System.Text.Json;
using System.Text.Json.Serialization;

namespace TranslateDesktop;

[Flags]
internal enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Windows = 0x0008,
    NoRepeat = 0x4000,
}

internal sealed class HotkeyBinding
{
    internal static HotkeyBinding DefaultTranslateSelection => new(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.T);

    internal static HotkeyBinding DefaultOpen => new(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.G);

    internal static HotkeyBinding DefaultTranslateClipboard => new(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.V);

    internal static HotkeyBinding DefaultHideWindow => new(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.W);

    public HotkeyBinding()
    {
    }

    internal HotkeyBinding(HotkeyModifiers modifiers, Keys key)
    {
        Modifiers = modifiers & ~HotkeyModifiers.NoRepeat;
        Key = key & Keys.KeyCode;
    }

    public HotkeyModifiers Modifiers { get; set; }

    public Keys Key { get; set; }

    [JsonIgnore]
    internal string DisplayText
    {
        get
        {
            var parts = new List<string>();
            if (Modifiers.HasFlag(HotkeyModifiers.Control))
            {
                parts.Add("Ctrl");
            }

            if (Modifiers.HasFlag(HotkeyModifiers.Alt))
            {
                parts.Add("Alt");
            }

            if (Modifiers.HasFlag(HotkeyModifiers.Shift))
            {
                parts.Add("Shift");
            }

            if (Modifiers.HasFlag(HotkeyModifiers.Windows))
            {
                parts.Add("Win");
            }

            parts.Add(FormatKey(Key));
            return string.Join("+", parts);
        }
    }

    internal HotkeyBinding Clone() => new(Modifiers, Key);

    internal bool IsValid()
    {
        var modifiers = Modifiers & ~HotkeyModifiers.NoRepeat;
        return modifiers != HotkeyModifiers.None && IsSupportedKey(Key & Keys.KeyCode);
    }

    internal bool IsSameAs(HotkeyBinding other)
    {
        return (Modifiers & ~HotkeyModifiers.NoRepeat) == (other.Modifiers & ~HotkeyModifiers.NoRepeat)
            && (Key & Keys.KeyCode) == (other.Key & Keys.KeyCode);
    }

    private static string FormatKey(Keys key)
    {
        return key switch
        {
            Keys.Space => "Space",
            Keys.Return => "Enter",
            Keys.Escape => "Esc",
            Keys.PageUp => "Page Up",
            Keys.PageDown => "Page Down",
            _ => new KeysConverter().ConvertToInvariantString(key) ?? key.ToString(),
        };
    }

    private static bool IsSupportedKey(Keys key)
    {
        return (key >= Keys.A && key <= Keys.Z)
            || (key >= Keys.D0 && key <= Keys.D9)
            || (key >= Keys.F1 && key <= Keys.F24)
            || (key >= Keys.NumPad0 && key <= Keys.Divide)
            || key is Keys.Space or Keys.Insert or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown
            or Keys.Up or Keys.Down or Keys.Left or Keys.Right;
    }
}

internal sealed class AppSettings
{
    public int Version { get; set; } = 1;

    public HotkeyBinding TranslateSelectionHotkey { get; set; } = HotkeyBinding.DefaultTranslateSelection;

    public HotkeyBinding OpenHotkey { get; set; } = HotkeyBinding.DefaultOpen;

    public HotkeyBinding TranslateClipboardHotkey { get; set; } = HotkeyBinding.DefaultTranslateClipboard;

    public HotkeyBinding HideWindowHotkey { get; set; } = HotkeyBinding.DefaultHideWindow;

    public int WindowWidth { get; set; } = 980;

    public int WindowHeight { get; set; } = 760;

    public bool ApplyWindowSizeOnShortcut { get; set; } = true;

    public string SourceLanguageCode { get; set; } = TranslationLanguageCatalog.AutoCode;

    public string TargetLanguageCode { get; set; } = TranslationLanguageCatalog.SmartPersianEnglishCode;

    internal AppSettings Clone()
    {
        return new AppSettings
        {
            Version = Version,
            TranslateSelectionHotkey = TranslateSelectionHotkey.Clone(),
            OpenHotkey = OpenHotkey.Clone(),
            TranslateClipboardHotkey = TranslateClipboardHotkey.Clone(),
            HideWindowHotkey = HideWindowHotkey.Clone(),
            WindowWidth = WindowWidth,
            WindowHeight = WindowHeight,
            ApplyWindowSizeOnShortcut = ApplyWindowSizeOnShortcut,
            SourceLanguageCode = SourceLanguageCode,
            TargetLanguageCode = TargetLanguageCode,
        };
    }

    internal void Normalize()
    {
        Version = 1;
        TranslateSelectionHotkey ??= HotkeyBinding.DefaultTranslateSelection;
        OpenHotkey ??= HotkeyBinding.DefaultOpen;
        TranslateClipboardHotkey ??= HotkeyBinding.DefaultTranslateClipboard;
        HideWindowHotkey ??= HotkeyBinding.DefaultHideWindow;

        if (!TranslateSelectionHotkey.IsValid())
        {
            TranslateSelectionHotkey = HotkeyBinding.DefaultTranslateSelection;
        }

        if (!OpenHotkey.IsValid())
        {
            OpenHotkey = HotkeyBinding.DefaultOpen;
        }

        if (!TranslateClipboardHotkey.IsValid())
        {
            TranslateClipboardHotkey = HotkeyBinding.DefaultTranslateClipboard;
        }

        if (!HideWindowHotkey.IsValid())
        {
            HideWindowHotkey = HotkeyBinding.DefaultHideWindow;
        }

        if (TranslateSelectionHotkey.IsSameAs(OpenHotkey))
        {
            OpenHotkey = HotkeyBinding.DefaultOpen;
            if (TranslateSelectionHotkey.IsSameAs(OpenHotkey))
            {
                OpenHotkey = new HotkeyBinding(HotkeyModifiers.Control | HotkeyModifiers.Shift, Keys.G);
            }
        }

        if (TranslateClipboardHotkey.IsSameAs(TranslateSelectionHotkey)
            || TranslateClipboardHotkey.IsSameAs(OpenHotkey))
        {
            TranslateClipboardHotkey = HotkeyBinding.DefaultTranslateClipboard;
            if (TranslateClipboardHotkey.IsSameAs(TranslateSelectionHotkey)
                || TranslateClipboardHotkey.IsSameAs(OpenHotkey))
            {
                TranslateClipboardHotkey = new HotkeyBinding(
                    HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
                    Keys.V);
            }
        }

        if (HideWindowHotkey.IsSameAs(TranslateSelectionHotkey)
            || HideWindowHotkey.IsSameAs(TranslateClipboardHotkey)
            || HideWindowHotkey.IsSameAs(OpenHotkey))
        {
            HideWindowHotkey = HotkeyBinding.DefaultHideWindow;
            if (HideWindowHotkey.IsSameAs(TranslateSelectionHotkey)
                || HideWindowHotkey.IsSameAs(TranslateClipboardHotkey)
                || HideWindowHotkey.IsSameAs(OpenHotkey))
            {
                HideWindowHotkey = new HotkeyBinding(
                    HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift,
                    Keys.W);
            }
        }

        WindowWidth = Math.Clamp(WindowWidth, 560, 2560);
        WindowHeight = Math.Clamp(WindowHeight, 420, 1600);
        SourceLanguageCode = TranslationLanguageCatalog.NormalizeSource(SourceLanguageCode);
        TargetLanguageCode = TranslationLanguageCatalog.NormalizeTarget(TargetLanguageCode);

        if (!SourceLanguageCode.Equals(TranslationLanguageCatalog.AutoCode, StringComparison.OrdinalIgnoreCase)
            && SourceLanguageCode.Equals(TargetLanguageCode, StringComparison.OrdinalIgnoreCase))
        {
            SourceLanguageCode = TranslationLanguageCatalog.AutoCode;
        }
    }
}

internal sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    internal AppSettingsStore(string? root = null)
    {
        Root = root ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TranslateDesktop");
    }

    internal string Root { get; }

    internal string SettingsPath => Path.Combine(Root, "settings.json");

    internal string WebViewProfilePath => Path.Combine(Root, "WebView2");

    internal AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions)
                    ?? new AppSettings();
                settings.Normalize();
                return settings;
            }
        }
        catch
        {
            // Invalid settings fall back to safe defaults without blocking translation.
        }

        return new AppSettings();
    }

    internal void Save(AppSettings settings)
    {
        settings.Normalize();
        Directory.CreateDirectory(Root);
        var temporaryPath = SettingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }

    internal static string Serialize(AppSettings settings)
    {
        return JsonSerializer.Serialize(settings, JsonOptions);
    }

    internal static AppSettings Deserialize(string json)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        settings.Normalize();
        return settings;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
