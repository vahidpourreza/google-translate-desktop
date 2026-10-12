using System.Drawing;

namespace TranslateDesktop;

internal static class WindowSizing
{
    internal static Rectangle CalculateShortcutBounds(
        AppSettings settings,
        Size minimumSize,
        Rectangle workingArea)
    {
        var minimumWidth = Math.Min(minimumSize.Width, workingArea.Width);
        var minimumHeight = Math.Min(minimumSize.Height, workingArea.Height);
        var width = Math.Min(Math.Max(settings.WindowWidth, minimumWidth), workingArea.Width);
        var height = Math.Min(Math.Max(settings.WindowHeight, minimumHeight), workingArea.Height);

        return new Rectangle(
            workingArea.Left + ((workingArea.Width - width) / 2),
            workingArea.Top + ((workingArea.Height - height) / 2),
            width,
            height);
    }
}
