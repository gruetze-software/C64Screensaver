using System;
using SkiaSharp;
namespace C64Screensaver;

// A glass reflection, composited after the phosphor/scanline pass.
public static class DiskActivityReflection
{
    private static readonly Lazy<SKImage> Glow = new(CreateGlow);
    private static SKImage CreateGlow()
    {
        using var surface = SKSurface.Create(new SKImageInfo(96, 48));
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.Translate(48, 24);
        surface.Canvas.Scale(2.3f, .7f);
        using var shader = SKShader.CreateRadialGradient(new SKPoint(0, 0), 19,
            new[] { new SKColor(255, 38, 8, 130), new SKColor(245, 20, 4, 65),
                new SKColor(230, 12, 2, 18), SKColors.Transparent },
            new[] { 0f, .18f, .58f, 1f }, SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = shader };
        surface.Canvas.DrawRect(-22, -22, 44, 44, paint);
        return surface.Snapshot();
    }
    private static double Pulse(double t, double start, double end)
    {
        if (t <= start || t >= end) return 0;
        double value = Math.Sin((t - start) / (end - start) * Math.PI);
        return value * value;
    }
    public static void Draw(SKCanvas canvas, SKRect bounds, CrtParameters parameters, double time)
    {
        double t = time % 2.43;
        double light = Pulse(t, .36, .57) + .65 * Pulse(t, .64, .92)
            + .5 * Pulse(t, 1.17, 1.31) + .9 * Pulse(t, 1.74, 2.08);
        if (light < .005) return;
        float w = bounds.Width, h = bounds.Height;
        if (parameters.KeepAspectRatio)
        {
            w = Math.Min(w, h * 4 / 3);
            h = w * 3 / 4;
            if (!parameters.FillScreen)
            {
                float scale = Math.Min(bounds.Width / 408, bounds.Height / 312);
                w = 384 * scale; h = 288 * scale;
            }
        }
        float left = bounds.Left + (bounds.Width - w) / 2, top = bounds.Top + (bounds.Height - h) / 2;
        float x = left + w * .93f, y = top + h * .93f;
        using var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)(Math.Min(1, light) * 190)) };
        canvas.DrawImage(Glow.Value, new SKRect(x - w * .05f, y - h * .027f,
            x + w * .05f, y + h * .027f), new SKSamplingOptions(SKFilterMode.Linear), paint);
    }
}
