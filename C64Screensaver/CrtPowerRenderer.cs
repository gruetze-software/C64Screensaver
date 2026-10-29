using System;
using SkiaSharp;

namespace C64Screensaver;

// Native hosts use this without starting an Avalonia application.
public static class CrtPowerRenderer
{
    public static void Draw(SKCanvas canvas, int width, int height, double time)
    {
        float w = width, h = height;
        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.Black };
        if (time < 1.02)
        {
            canvas.DrawRect(0, 0, w, h, paint);
            if (time < .10) return;
            if (time < .14) { paint.Color = SKColors.White; canvas.DrawRect(0, 0, w, h, paint); return; }
            if (time < .46) Beam(canvas, w, h, w, Math.Max(2, h * (float)Math.Pow(1 - (time - .14) / .32, 3)), 1);
            else if (time < .67) Beam(canvas, w, h, Math.Max(4, w * (float)Math.Pow(1 - (time - .46) / .21, 2)), 2, 1);
            else if (time < .86)
            {
                float light = (float)(1 - (time - .67) / .19);
                using var shader = SKShader.CreateRadialGradient(new SKPoint(w / 2, h / 2), 10,
                    [new SKColor(220, 235, 255, (byte)(170 * light)), SKColors.Transparent], SKShaderTileMode.Clamp);
                paint.Shader = shader;
                canvas.DrawCircle(w / 2, h / 2, 10, paint);
                paint.Shader = null;
                paint.Color = SKColors.White.WithAlpha((byte)(255 * light));
                canvas.DrawCircle(w / 2, h / 2, 2, paint);
            }
            return;
        }
        double opening = Math.Clamp((time - 1.02) / .40, 0, 1);
        float rw = w * (float)Math.Min(1, opening * 3);
        float rh = h * (float)Math.Pow(Math.Clamp((opening - .20) / .80, 0, 1), 1.6);
        float x = (w - rw) / 2, y = (h - rh) / 2;
        canvas.DrawRect(0, 0, w, y, paint);
        canvas.DrawRect(0, h - y, w, y, paint);
        canvas.DrawRect(0, y, x, rh, paint);
        canvas.DrawRect(w - x, y, x, rh, paint);
        if (rh < 5) Beam(canvas, w, h, Math.Max(2, rw), 2, (float)(1 - opening));
    }

    private static void Beam(SKCanvas canvas, float w, float h, float width, float height, float light)
    {
        using var shader = SKShader.CreateLinearGradient(new SKPoint(0, (h - height) / 2 - 12),
            new SKPoint(0, (h + height) / 2 + 12),
            [SKColors.Transparent, new SKColor(170, 205, 255, (byte)(90 * light)),
             new SKColor(215, 235, 255, (byte)(160 * light)), SKColors.Transparent],
            [0, .35f, .5f, 1], SKShaderTileMode.Clamp);
        using var paint = new SKPaint { Shader = shader };
        canvas.DrawRect((w - width) / 2, (h - height) / 2 - 12, width, height + 24, paint);
        paint.Shader = null;
        paint.Color = SKColors.White.WithAlpha((byte)(255 * light));
        canvas.DrawRect((w - width) / 2, (h - height) / 2, width, height, paint);
    }
}
