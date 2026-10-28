using System;
using SkiaSharp;
namespace C64Screensaver;
// Original procedural graphics; no Kickstart ROM, demo assets or external fonts.
public static class AmigaFrameBuffer
{
    public static SKImage Capture(SaverSettings settings, double time)
    {
        using var surface = SKSurface.Create(new SKImageInfo(384, 288));
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(5, 7, 24));
        using var paint = new SKPaint { IsAntialias = false };
        for (int bar = 0; bar < 5; bar++)
        {
            float center = 144 + (float)Math.Sin(time * .7 + bar * .9) * 100;
            for (int row = -12; row <= 12; row++)
            {
                float light = 1 - Math.Abs(row) / 13f;
                paint.Color = SKColor.FromHsv((float)((bar * 65 + time * 10) % 360), 85, light * 80);
                canvas.DrawRect(0, center + row, 384, 1, paint);
            }
        }
        string text = settings.ScrollerText + "     ";
        double offset = time * settings.ScrollerSpeed % (text.Length * 18.0);
        for (int slot = 0; slot < 24; slot++)
        {
            int index = (int)(offset / 18) + slot;
            var rows = PixelCharacters.Rows((byte)text[index % text.Length]);
            if (rows == null) continue;
            float left = slot * 18 - (float)(offset % 18);
            for (int x = 0; x < 8; x++)
            {
                float yBase = 135 + (float)Math.Sin((left + x * 2) * .024 + time * 2) * (float)settings.ScrollerWave;
                for (int y = 0; y < 8; y++)
                {
                    if ((rows[y] & (128 >> x)) == 0) continue;
                    paint.Color = new SKColor(12, 10, 38);
                    canvas.DrawRect(left + x * 2 + 2, yBase + y * 3 + 3, 2, 3, paint);
                    paint.Color = SKColor.FromHsv(40 + y * 4, 65, 100 - y * 5);
                    canvas.DrawRect(left + x * 2, yBase + y * 3, 2, 3, paint);
                }
            }
        }
        return surface.Snapshot();
    }
}
