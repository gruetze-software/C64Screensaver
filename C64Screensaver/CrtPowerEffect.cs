using System;
using Avalonia;
using Avalonia.Media;

namespace C64Screensaver;

public static class CrtPowerEffect
{
    public static void Draw(DrawingContext context, Rect bounds, double time)
    {
        double w = bounds.Width, h = bounds.Height;
        if (time < 1.02)
        {
            context.DrawRectangle(Brushes.Black, null, bounds);
            if (time < 0.10) return;
            if (time < 0.14) { context.DrawRectangle(Brushes.White, null, bounds); return; }
            if (time < 0.46)
            {
                double p = (time - 0.14) / 0.32;
                DrawBeam(context, w, h, w, Math.Max(2, h * Math.Pow(1 - p, 3)), 1);
            }
            else if (time < 0.67)
            {
                double p = (time - 0.46) / 0.21;
                DrawBeam(context, w, h, Math.Max(4, w * Math.Pow(1 - p, 2)), 2, 1);
            }
            else if (time < 0.86)
            {
                double light = 1 - (time - 0.67) / 0.19;
                using (context.PushOpacity(light))
                {
                    var glow = new RadialGradientBrush
                    {
                        GradientStops = new GradientStops
                        {
                            new GradientStop(Color.FromArgb(170, 220, 235, 255), 0),
                            new GradientStop(Color.FromArgb(50, 150, 190, 255), 0.30),
                            new GradientStop(Colors.Transparent, 1)
                        }
                    };
                    context.DrawEllipse(glow, null, new Rect(w / 2 - 10, h / 2 - 10, 20, 20));
                    context.DrawEllipse(Brushes.White, null, new Rect(w / 2 - 2, h / 2 - 2, 4, 4));
                }
            }
            return;
        }

        // Reveal the next boot screen from a central line to its full height.
        double opening = Math.Clamp((time - 1.02) / 0.40, 0, 1);
        double revealW = w * Math.Min(1, opening * 3);
        double revealH = h * Math.Pow(Math.Clamp((opening - 0.20) / 0.80, 0, 1), 1.6);
        double x = (w - revealW) / 2, y = (h - revealH) / 2;
        context.DrawRectangle(Brushes.Black, null, new Rect(0, 0, w, y));
        context.DrawRectangle(Brushes.Black, null, new Rect(0, h - y, w, y));
        context.DrawRectangle(Brushes.Black, null, new Rect(0, y, x, revealH));
        context.DrawRectangle(Brushes.Black, null, new Rect(w - x, y, x, revealH));
        if (revealH < 5) DrawBeam(context, w, h, Math.Max(2, revealW), 2, 1 - opening);
    }
    private static void DrawBeam(DrawingContext context, double w, double h, double width, double height, double light)
    {
        using (context.PushOpacity(light))
        {
            var glow = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Colors.Transparent, 0),
                    new GradientStop(Color.FromArgb(90, 170, 205, 255), 0.35),
                    new GradientStop(Color.FromArgb(160, 215, 235, 255), 0.5),
                    new GradientStop(Color.FromArgb(90, 170, 205, 255), 0.65),
                    new GradientStop(Colors.Transparent, 1)
                }
            };
            context.DrawRectangle(glow, null, new Rect((w - width) / 2, (h - height) / 2 - 12, width, height + 24));
            context.DrawRectangle(Brushes.White, null, new Rect((w - width) / 2, (h - height) / 2, width, height));
        }
    }
}
