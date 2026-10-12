namespace TranslateDesktop;

internal sealed record TranslationLanguageOption(string Code, string Name)
{
    public override string ToString() => Name;
}

internal static class TranslationLanguageCatalog
{
    internal const string AutoCode = "auto";
    internal const string SmartPersianEnglishCode = "smart-fa-en";

    private static readonly IReadOnlyList<TranslationLanguageOption> Languages =
    [
        new("af", "Afrikaans"),
        new("sq", "Albanian"),
        new("am", "Amharic"),
        new("ar", "Arabic"),
        new("hy", "Armenian"),
        new("as", "Assamese"),
        new("ay", "Aymara"),
        new("az", "Azerbaijani"),
        new("bm", "Bambara"),
        new("eu", "Basque"),
        new("be", "Belarusian"),
        new("bn", "Bengali"),
        new("bho", "Bhojpuri"),
        new("bs", "Bosnian"),
        new("bg", "Bulgarian"),
        new("ca", "Catalan"),
        new("ceb", "Cebuano"),
        new("ny", "Chichewa"),
        new("zh-CN", "Chinese (Simplified)"),
        new("zh-TW", "Chinese (Traditional)"),
        new("co", "Corsican"),
        new("hr", "Croatian"),
        new("cs", "Czech"),
        new("da", "Danish"),
        new("dv", "Dhivehi"),
        new("doi", "Dogri"),
        new("nl", "Dutch"),
        new("en", "English"),
        new("eo", "Esperanto"),
        new("et", "Estonian"),
        new("ee", "Ewe"),
        new("tl", "Filipino"),
        new("fi", "Finnish"),
        new("fr", "French"),
        new("fy", "Frisian"),
        new("gl", "Galician"),
        new("ka", "Georgian"),
        new("de", "German"),
        new("el", "Greek"),
        new("gn", "Guarani"),
        new("gu", "Gujarati"),
        new("ht", "Haitian Creole"),
        new("ha", "Hausa"),
        new("haw", "Hawaiian"),
        new("he", "Hebrew"),
        new("hi", "Hindi"),
        new("hmn", "Hmong"),
        new("hu", "Hungarian"),
        new("is", "Icelandic"),
        new("ig", "Igbo"),
        new("ilo", "Ilocano"),
        new("id", "Indonesian"),
        new("ga", "Irish"),
        new("it", "Italian"),
        new("ja", "Japanese"),
        new("jw", "Javanese"),
        new("kn", "Kannada"),
        new("kk", "Kazakh"),
        new("km", "Khmer"),
        new("rw", "Kinyarwanda"),
        new("gom", "Konkani"),
        new("ko", "Korean"),
        new("kri", "Krio"),
        new("ku", "Kurdish (Kurmanji)"),
        new("ckb", "Kurdish (Sorani)"),
        new("ky", "Kyrgyz"),
        new("lo", "Lao"),
        new("la", "Latin"),
        new("lv", "Latvian"),
        new("ln", "Lingala"),
        new("lt", "Lithuanian"),
        new("lg", "Luganda"),
        new("lb", "Luxembourgish"),
        new("mk", "Macedonian"),
        new("mai", "Maithili"),
        new("mg", "Malagasy"),
        new("ms", "Malay"),
        new("ml", "Malayalam"),
        new("mt", "Maltese"),
        new("mi", "Maori"),
        new("mr", "Marathi"),
        new("mni-Mtei", "Meiteilon (Manipuri)"),
        new("lus", "Mizo"),
        new("mn", "Mongolian"),
        new("my", "Myanmar (Burmese)"),
        new("ne", "Nepali"),
        new("no", "Norwegian"),
        new("or", "Odia (Oriya)"),
        new("om", "Oromo"),
        new("ps", "Pashto"),
        new("fa", "Persian"),
        new("pl", "Polish"),
        new("pt", "Portuguese"),
        new("pa", "Punjabi"),
        new("qu", "Quechua"),
        new("ro", "Romanian"),
        new("ru", "Russian"),
        new("sm", "Samoan"),
        new("sa", "Sanskrit"),
        new("gd", "Scots Gaelic"),
        new("nso", "Sepedi"),
        new("sr", "Serbian"),
        new("st", "Sesotho"),
        new("sn", "Shona"),
        new("sd", "Sindhi"),
        new("si", "Sinhala"),
        new("sk", "Slovak"),
        new("sl", "Slovenian"),
        new("so", "Somali"),
        new("es", "Spanish"),
        new("su", "Sundanese"),
        new("sw", "Swahili"),
        new("sv", "Swedish"),
        new("tg", "Tajik"),
        new("ta", "Tamil"),
        new("tt", "Tatar"),
        new("te", "Telugu"),
        new("th", "Thai"),
        new("ti", "Tigrinya"),
        new("ts", "Tsonga"),
        new("tr", "Turkish"),
        new("tk", "Turkmen"),
        new("ak", "Twi"),
        new("uk", "Ukrainian"),
        new("ur", "Urdu"),
        new("ug", "Uyghur"),
        new("uz", "Uzbek"),
        new("vi", "Vietnamese"),
        new("cy", "Welsh"),
        new("xh", "Xhosa"),
        new("yi", "Yiddish"),
        new("yo", "Yoruba"),
        new("zu", "Zulu"),
    ];

    internal static IReadOnlyList<TranslationLanguageOption> SourceOptions { get; } =
        [new(AutoCode, "Detect language"), .. Languages];

    internal static IReadOnlyList<TranslationLanguageOption> TargetOptions { get; } =
        [new(SmartPersianEnglishCode, "Smart: Persian ↔ English"), .. Languages];

    internal static string NormalizeSource(string? code)
    {
        return FindCode(SourceOptions, code) ?? AutoCode;
    }

    internal static string NormalizeTarget(string? code)
    {
        return FindCode(TargetOptions, code) ?? SmartPersianEnglishCode;
    }

    internal static TranslationLanguageOption SourceByCode(string code)
    {
        return SourceOptions.First(option => option.Code.Equals(NormalizeSource(code), StringComparison.OrdinalIgnoreCase));
    }

    internal static TranslationLanguageOption TargetByCode(string code)
    {
        return TargetOptions.First(option => option.Code.Equals(NormalizeTarget(code), StringComparison.OrdinalIgnoreCase));
    }

    private static string? FindCode(IEnumerable<TranslationLanguageOption> options, string? code)
    {
        return options.FirstOrDefault(option => option.Code.Equals(code, StringComparison.OrdinalIgnoreCase))?.Code;
    }
}
