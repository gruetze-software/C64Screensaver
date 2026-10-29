using System;
using System.Runtime.InteropServices;
using SkiaSharp;
namespace C64Screensaver;

public enum AmigaBootStyle { None, Classic13, Purple20, Purple31 }
public enum AmigaDemo { Cycle, Copper, Boing, Starflight, Plasma }

// Original procedural artwork, not a ROM dump or an extracted demo asset.
public sealed class AmigaFrameBuffer : IDisposable
{
    private readonly SKSurface surface = SKSurface.Create(new SKImageInfo(384, 288));
    private readonly SKBitmap plasma = new(96, 72);
    private readonly int[] plasmaPixels = new int[96 * 72];
    private readonly SKPaint paint = new() { IsAntialias = false };
    private readonly SKPoint3[] stars = CreateStars();
    private static readonly Lazy<SKImage> GlyphAtlas = new(CreateGlyphAtlas);
    private static readonly Lazy<SKImage[]> Balls = new(CreateBalls);
    private static readonly float[] Sines = CreateSines();
    private static readonly SKColor[] PlasmaColors = CreatePalette();
    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear);
    private bool disposed;

    public AmigaFrameBuffer()
    {
        // Prepare sprites during startup, before a demo needs them for its first frame.
        _ = GlyphAtlas.Value;
        _ = Balls.Value;
    }
    public static SKImage Capture(SaverSettings settings, double time)
    {
        using var renderer = new AmigaFrameBuffer();
        return renderer.CaptureFrame(settings, time);
    }
    public static double BootDuration(SaverSettings settings) => settings.AmigaBoot == AmigaBootStyle.None ? 0 : 5;
    public static double? PowerTime(SaverSettings settings, double time)
    {
        if (!settings.PowerTransition) return null;
        double before = BootDuration(settings) + settings.AmigaDurationSeconds;
        double phase = time % (before + 1.42);
        return phase >= before ? phase - before : null;
    }
    public static AmigaDemo CurrentDemo(SaverSettings settings, double time)
    {
        double length = BootDuration(settings) + settings.AmigaDurationSeconds + (settings.PowerTransition ? 1.42 : 0);
        return settings.AmigaAnimation == AmigaDemo.Cycle
            ? (AmigaDemo)(1 + (int)(time / length % 4)) : settings.AmigaAnimation;
    }
    public SKImage CaptureFrame(SaverSettings settings, double time)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        time = double.IsFinite(time) ? Math.Max(0, time) : 0;
        var canvas = surface.Canvas;
        double boot = BootDuration(settings);
        double length = boot + settings.AmigaDurationSeconds + (settings.PowerTransition ? 1.42 : 0);
        double phase = time % length;
        bool revealNext = settings.PowerTransition && phase >= boot + settings.AmigaDurationSeconds + 1.02;
        double contentPhase = revealNext ? 0 : phase;
        if (contentPhase < boot) DrawBoot(canvas, settings.AmigaBoot, contentPhase);
        else
        {
            double demoTime = Math.Min(contentPhase - boot, settings.AmigaDurationSeconds);
            canvas.Clear(new SKColor(5, 7, 24));
            var demo = CurrentDemo(settings, time + (revealNext ? .4 : 0));
            switch (demo)
            {
                case AmigaDemo.Boing: DrawBoing(canvas, demoTime); break;
                case AmigaDemo.Starflight: DrawStars(canvas, demoTime); break;
                case AmigaDemo.Plasma: DrawPlasma(canvas, demoTime); break;
                default: DrawCopper(canvas, demoTime); break;
            }
            double scrollTime = Math.Floor(time / length) * settings.AmigaDurationSeconds
                + (revealNext ? settings.AmigaDurationSeconds : Math.Clamp(phase - boot, 0, settings.AmigaDurationSeconds));
            DrawScroller(canvas, settings, scrollTime, demo == AmigaDemo.Copper ? 135 : Math.Min(226, 261 - (float)settings.ScrollerWave));
        }
        return surface.Snapshot();
    }
    private void DrawCopper(SKCanvas canvas, double time)
    {
        for (int bar = 0; bar < 5; bar++)
        {
            float center = 144 + (float)Math.Sin(time * .7 + bar * .9) * 100;
            for (int row = -12; row <= 12; row++)
            {
                paint.Color = SKColor.FromHsv((float)((bar * 65 + time * 10) % 360), 85, (1 - Math.Abs(row) / 13f) * 80);
                canvas.DrawRect(0, center + row, 384, 1, paint);
            }
        }
    }
    private void DrawScroller(SKCanvas canvas, SaverSettings settings, double time, float baseline)
    {
        string text = settings.ScrollerText;
        int length = text.Length + 5;
        double offset = time * settings.ScrollerSpeed % (length * 18.0);
        var atlas = GlyphAtlas.Value;
        paint.Color = SKColors.White;
        for (int slot = 0; slot < 24; slot++)
        {
            int index = ((int)(offset / 18) + slot) % length;
            char ch = index < text.Length ? text[index] : ' ';
            if (ch < 32 || ch > 126) continue;
            float left = slot * 18 - (float)(offset % 18);
            for (int column = 0; column < 8; column++)
            {
                float x = left + column * 2;
                float y = baseline + (float)Math.Sin(x * .024 + time * 2) * (float)settings.ScrollerWave;
                float sourceX = (ch - 32) * 18 + column * 2;
                canvas.DrawImage(atlas, new SKRect(sourceX, 0, sourceX + 2, 27),
                    new SKRect(x, y, x + 2, y + 27), Sampling, paint);
            }
        }
    }
    private void DrawBoing(SKCanvas canvas, double time)
    {
        paint.Color = new SKColor(55, 38, 88);
        for (int y = 195; y < 288; y += 12) canvas.DrawLine(0, y, 384, y, paint);
        for (int x = -180; x < 600; x += 40) canvas.DrawLine(192, 170, x, 288, paint);
        float bx = 192 + (float)Math.Sin(time * .8) * 110;
        float bounce = (float)Math.Abs(Math.Sin(time * 1.6));
        paint.Color = new SKColor(0, 0, 0, (byte)(100 - bounce * 60));
        canvas.DrawOval(bx, 201, 40 - bounce * 18, 7 - bounce * 3, paint);
        paint.Color = SKColors.White;
        var ball = Balls.Value[(int)(time * 18) % Balls.Value.Length];
        canvas.DrawImage(ball, new SKRect(bx - 48, 105 - bounce * 95, bx + 48, 201 - bounce * 95), Sampling, paint);
    }
    private void DrawStars(SKCanvas canvas, double time)
    {
        foreach (var star in stars)
        {
            float z = (float)((star.Z - time * .24) % 1);
            if (z <= 0) z += 1;
            float x = 192 + star.X / z, y = 130 + star.Y / z;
            if (x < 0 || x > 384 || y < 0 || y > 288) continue;
            byte light = (byte)(60 + (1 - z) * 195);
            paint.Color = new SKColor(light, light, 255);
            canvas.DrawRect(x, y, z < .3 ? 2 : 1, z < .3 ? 2 : 1, paint);
        }
        Span<SKPoint> points = stackalloc SKPoint[8];
        double a = time * .6, b = time * .35;
        for (int i = 0; i < 8; i++)
        {
            double x = (i & 1) == 0 ? -1 : 1, y = (i & 2) == 0 ? -1 : 1, z = (i & 4) == 0 ? -1 : 1;
            double rx = x * Math.Cos(a) - z * Math.Sin(a), rz = x * Math.Sin(a) + z * Math.Cos(a);
            double ry = y * Math.Cos(b) - rz * Math.Sin(b), depth = 4 + y * Math.Sin(b) + rz * Math.Cos(b);
            points[i] = new SKPoint(192 + (float)(rx * 130 / depth), 120 + (float)(ry * 130 / depth));
        }
        paint.Color = new SKColor(80, 235, 255);
        paint.StrokeWidth = 1.2f;
        for (int i = 0; i < 8; i++)
            for (int bit = 1; bit <= 4; bit *= 2)
                if ((i & bit) == 0) canvas.DrawLine(points[i], points[i | bit], paint);
        paint.StrokeWidth = 1;
    }
    private void DrawPlasma(SKCanvas canvas, double time)
    {
        int tick = (int)(time * 55) & 1023;
        int slowTick = (int)(time * 27.5) & 1023;
        int shiftX = (int)(Math.Sin(time * .6) * 90), shiftY = (int)(Math.Cos(time * .4) * 90);
        for (int y = 0; y < 72; y++)
        for (int x = 0; x < 96; x++)
        {
            float value = Sines[(x * 11 + tick) & 1023] + Sines[(y * 15 - tick) & 1023]
                + Sines[((x + y) * 7 + slowTick) & 1023]
                + Sines[((x + shiftX) * 6 + (y + shiftY) * 9) & 1023];
            plasmaPixels[y * 96 + x] = unchecked((int)(uint)PlasmaColors[(int)((value + 4) * 31.8f) & 255]);
        }
        Marshal.Copy(plasmaPixels, 0, plasma.GetPixels(), plasmaPixels.Length);
        paint.Color = SKColors.White;
        canvas.DrawBitmap(plasma, new SKRect(0, 0, 384, 288), paint);
    }
    private void DrawBoot(SKCanvas canvas, AmigaBootStyle style, double time)
    {
        bool classic = style == AmigaBootStyle.Classic13;
        canvas.Clear(classic ? new SKColor(220, 220, 220) : new SKColor(73, 27, 111));
        // Independently drawn hand/disk and drive motifs, deliberately not pixel-identical ROM artwork.
        float diskY = 78 - (time > 3.6 ? (float)Math.Min(36, (time - 3.6) * 36) : 0);
        if (classic)
        {
            using var hand = new SKPath();
            hand.MoveTo(98, 220); hand.LineTo(116, 174); hand.LineTo(127, 161);
            hand.LineTo(132, 137); hand.CubicTo(134, 125, 143, 126, 144, 135);
            hand.LineTo(142, 153); hand.LineTo(149, 153); hand.LineTo(154, 138);
            hand.CubicTo(158, 129, 166, 133, 163, 143); hand.LineTo(160, 155);
            hand.LineTo(176, 155); hand.CubicTo(184, 148, 191, 149, 190, 157);
            hand.LineTo(191, 164); hand.LineTo(209, 164);
            hand.CubicTo(226, 164, 228, 177, 216, 184);
            hand.LineTo(200, 202); hand.LineTo(173, 206); hand.LineTo(158, 220); hand.Close();
            paint.Color = SKColors.White;
            canvas.DrawPath(hand, paint);
            paint.Color = new SKColor(30, 30, 50);
            paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 2;
            canvas.DrawPath(hand, paint);
            canvas.DrawLine(132, 161, 149, 168, paint);
            canvas.DrawLine(149, 174, 184, 177, paint);
            canvas.DrawLine(161, 191, 190, 188, paint);
            paint.Style = SKPaintStyle.Fill; paint.StrokeWidth = 1;
        }
        else
        {
            paint.Color = new SKColor(180, 180, 195);
            canvas.DrawRoundRect(106, 164, 172, 30, 3, 3, paint);
            paint.Color = new SKColor(25, 18, 38);
            canvas.DrawRect(120, 174, 128, 5, paint);
            paint.Color = time > 3.6 ? SKColors.LimeGreen : SKColors.DarkGreen;
            canvas.DrawRect(258, 177, 8, 4, paint);
        }
        paint.Color = classic ? new SKColor(38, 61, 166) : new SKColor(220, 215, 232);
        canvas.DrawRect(154, diskY, 76, 76, paint);
        paint.Color = new SKColor(50, 50, 70);
        canvas.DrawRect(165, diskY, 48, 25, paint);
        paint.Color = new SKColor(185, 188, 200);
        canvas.DrawRect(173, diskY + 3, 14, 19, paint);
        paint.Color = SKColors.White;
        canvas.DrawRect(161, diskY + 43, 62, 28, paint);
        DrawText(canvas, "RETRO", 172, diskY + 52, 1, new SKColor(30, 30, 50));
        string label = classic ? "KICKSTART 1.3 STYLE" : style == AmigaBootStyle.Purple20 ? "KICKSTART 2.0 STYLE" : "KICKSTART 3.1 STYLE";
        DrawText(canvas, label, 192 - label.Length * 4, 223, 1, classic ? SKColors.Black : SKColors.White);
        if (time < 3.6) DrawText(canvas, "INSERT DISK", 148, 245, 1, classic ? SKColors.Black : SKColors.White);
        else
        {
            paint.Color = classic ? new SKColor(38, 61, 166) : SKColors.White;
            canvas.DrawRect(128, 247, (float)Math.Clamp((time - 3.6) / 1.4, 0, 1) * 128, 3, paint);
        }
        if (style == AmigaBootStyle.Purple31)
        {
            SKColor[] colors = [SKColors.Red, SKColors.Orange, SKColors.Yellow, SKColors.LimeGreen, SKColors.Cyan, SKColors.Blue];
            for (int i = 0; i < colors.Length; i++)
            {
                paint.Color = colors[i];
                canvas.DrawRect(145 + i * 15, 42 - i * 3, 13, 6, paint);
            }
        }
    }
    private void DrawText(SKCanvas canvas, string text, float x, float y, int scale, SKColor color)
    {
        paint.Color = color;
        foreach (char ch in text)
        {
            var rows = PixelCharacters.Rows((byte)ch);
            if (rows != null)
                for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                    if ((rows[row] & (128 >> col)) != 0) canvas.DrawRect(x + col * scale, y + row * scale, scale, scale, paint);
            x += 8 * scale;
        }
    }
    private static SKImage CreateGlyphAtlas()
    {
        using var target = SKSurface.Create(new SKImageInfo(95 * 18, 27));
        target.Canvas.Clear(SKColors.Transparent);
        using var p = new SKPaint();
        for (int ch = 32; ch <= 126; ch++)
        {
            var rows = PixelCharacters.Rows((byte)ch);
            if (rows == null) continue;
            for (int pass = 0; pass < 2; pass++)
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                if ((rows[y] & (128 >> x)) == 0) continue;
                p.Color = pass == 0 ? new SKColor(12, 10, 38) : SKColor.FromHsv(40 + y * 4, 65, 100 - y * 5);
                target.Canvas.DrawRect((ch - 32) * 18 + x * 2 + (pass == 0 ? 2 : 0), y * 3 + (pass == 0 ? 3 : 0), 2, 3, p);
            }
        }
        return target.Snapshot();
    }
    private static SKImage[] CreateBalls()
    {
        var result = new SKImage[12];
        using var bitmap = new SKBitmap(96, 96);
        var pixels = new int[96 * 96];
        for (int frame = 0; frame < result.Length; frame++)
        {
            Array.Clear(pixels);
            for (int y = 0; y < 96; y++)
            for (int x = 0; x < 96; x++)
            {
                double nx = (x - 47.5) / 47, ny = (y - 47.5) / 47, radius = nx * nx + ny * ny;
                if (radius > 1) continue;
                double nz = Math.Sqrt(1 - radius);
                int u = (int)Math.Floor((Math.Atan2(nx, nz) / Math.PI + 1 + frame / 48.0) * 8);
                int v = (int)Math.Floor((Math.Asin(ny) / Math.PI + .5) * 8);
                double light = .35 + .65 * Math.Max(0, -.25 * nx - .35 * ny + .8 * nz);
                bool red = ((u + v) & 1) == 0;
                pixels[y * 96 + x] = unchecked((int)(uint)new SKColor((byte)(240 * light), (byte)((red ? 35 : 240) * light), (byte)((red ? 45 : 240) * light)));
            }
            Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixels.Length);
            // A copy keeps each atlas frame independent of the reused bitmap.
            result[frame] = SKImage.FromPixelCopy(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes);
        }
        return result;
    }
    private static SKPoint3[] CreateStars()
    {
        var random = new Random(64);
        var result = new SKPoint3[150];
        for (int i = 0; i < result.Length; i++) result[i] = new SKPoint3((float)(random.NextDouble() - .5) * 180, (float)(random.NextDouble() - .5) * 130, (float)random.NextDouble());
        return result;
    }
    private static float[] CreateSines()
    {
        var result = new float[1024];
        for (int i = 0; i < result.Length; i++) result[i] = (float)Math.Sin(i * Math.PI * 2 / 1024);
        return result;
    }
    private static SKColor[] CreatePalette()
    {
        var result = new SKColor[256];
        for (int i = 0; i < result.Length; i++) result[i] = SKColor.FromHsv(i * 360f / 256, 80, 60);
        return result;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        surface.Dispose(); plasma.Dispose(); paint.Dispose();
    }
}
