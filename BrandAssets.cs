using System.Drawing;
using System.Reflection;

namespace TranslateDesktop;

internal static class BrandAssets
{
    private const string LogoResourceName = "TranslateDesktop.GoogleTranslateLogo.png";

    internal static Icon LoadAppIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? (Icon)SystemIcons.Application.Clone();
        }
        catch
        {
            return (Icon)SystemIcons.Application.Clone();
        }
    }

    internal static Bitmap LoadLogo()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoResourceName)
            ?? throw new InvalidOperationException("The packaged Google Translate logo is missing.");
        using var source = new Bitmap(stream);
        return new Bitmap(source);
    }
}
