using System;
using Avalonia;
using Avalonia.Controls;

namespace C64Screensaver;

internal static class DialogSizing
{
    // WorkingArea is in physical pixels; window sizes are in scaled UI units.
    // Reserve space for title bar and decorations as well as the OS taskbar.
    internal static void FitToWorkingArea(Window window)
    {
        var screen = window.Screens.ScreenFromWindow(window) ?? window.Screens.Primary;
        if (screen == null) return;
        var area = screen.WorkingArea;
        double width = Math.Max(320, area.Width / screen.Scaling - 24);
        double height = Math.Max(240, area.Height / screen.Scaling - 56);
        if (window.Width <= width && window.Height <= height) return;
        window.MinWidth = Math.Min(window.MinWidth, width);
        window.MinHeight = Math.Min(window.MinHeight, height);
        window.Width = Math.Min(window.Width, width);
        window.Height = Math.Min(window.Height, height);
        window.Position = new PixelPoint(area.X + Math.Max(0, (area.Width - (int)(window.Width * screen.Scaling)) / 2),
            area.Y + Math.Max(0, (area.Height - (int)((window.Height + 32) * screen.Scaling)) / 2));
    }
}
