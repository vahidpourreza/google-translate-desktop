using System.Text;

namespace TranslateDesktop;

internal static class TranslationUrl
{
    internal const string Home = "https://translate.google.com/";
    internal const int MaximumQuickTextLength = 6_000;

    internal static string ForText(string text, AppSettings? settings = null)
    {
        settings ??= new AppSettings();
        settings.Normalize();
        var normalized = text.Trim();
        if (normalized.Length == 0 || normalized.Length > MaximumQuickTextLength)
        {
            return ForHome(settings);
        }

        var source = settings.SourceLanguageCode;
        var target = ResolveTarget(settings, normalized);
        return $"{Home}?sl={source}&tl={target}&text={Uri.EscapeDataString(normalized)}&op=translate";
    }

    internal static string ForHome(AppSettings settings)
    {
        settings.Normalize();
        var source = settings.SourceLanguageCode;
        var target = ResolveTarget(settings, text: null);
        return $"{Home}?sl={source}&tl={target}&op=translate";
    }

    internal static bool ContainsPersianOrArabic(string text)
    {
        var scriptCharacters = 0;
        var letters = 0;

        foreach (var rune in text.EnumerateRunes())
        {
            if (!Rune.IsLetter(rune))
            {
                continue;
            }

            letters++;
            var value = rune.Value;
            if ((value >= 0x0600 && value <= 0x06FF)
                || (value >= 0x0750 && value <= 0x077F)
                || (value >= 0x08A0 && value <= 0x08FF)
                || (value >= 0xFB50 && value <= 0xFDFF)
                || (value >= 0xFE70 && value <= 0xFEFF))
            {
                scriptCharacters++;
            }
        }

        return scriptCharacters > 0 && scriptCharacters * 4 >= Math.Max(letters, 1);
    }

    private static string ResolveTarget(AppSettings settings, string? text)
    {
        if (!settings.TargetLanguageCode.Equals(
                TranslationLanguageCatalog.SmartPersianEnglishCode,
                StringComparison.OrdinalIgnoreCase))
        {
            return settings.TargetLanguageCode;
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            return ContainsPersianOrArabic(text) ? "en" : "fa";
        }

        return settings.SourceLanguageCode.Equals("fa", StringComparison.OrdinalIgnoreCase) ? "en" : "fa";
    }
}
