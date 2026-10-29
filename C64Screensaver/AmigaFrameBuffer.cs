using System;
using System.Runtime.InteropServices;
using SkiaSharp;
namespace C64Screensaver;

public enum AmigaBootStyle { None, Classic13, Purple20, Purple31 }
public enum AmigaDemo { Cycle, Copper, Boing, Starflight, Plasma, Runner }

// Procedural demos with historical Kickstart screen captures; no ROM is loaded.
public sealed class AmigaFrameBuffer : IDisposable
{
    private readonly SKSurface surface = SKSurface.Create(new SKImageInfo(384, 288));
    private SKSurface? bootSurface;
    private readonly SKBitmap plasma = new(96, 72);
    private readonly int[] plasmaPixels = new int[96 * 72];
    private readonly SKPaint paint = new() { IsAntialias = false };
    private AmigaRunner? runner;
    private readonly SKPoint3[] stars = CreateStars();
    private static readonly Lazy<SKImage> GlyphAtlas = new(CreateGlyphAtlas);
    private static readonly Lazy<SKImage[]> Balls = new(CreateBalls);
    private static readonly Lazy<SKImage> BoingFloor = new(CreateBoingFloor);
    private static readonly float[] Sines = CreateSines();
    private static readonly SKColor[] PlasmaColors = CreatePalette();
    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear);
    private bool disposed;

    public AmigaFrameBuffer()
    {
        // Prepare sprites during startup, before a demo needs them for its first frame.
        _ = GlyphAtlas.Value;
        _ = Balls.Value;
        _ = BoingFloor.Value;
    }
    public static SKImage Capture(SaverSettings settings, double time)
    {
        using var renderer = new AmigaFrameBuffer();
        return renderer.CaptureFrame(settings, time);
    }
    public static double BootDuration(SaverSettings settings) => settings.AmigaBoot == AmigaBootStyle.None ? 0 : settings.AmigaBoot == AmigaBootStyle.Classic13 ? 5 : KickstartFrames.PlaybackSeconds(settings.AmigaBoot);
    public static double? StartupTime(SaverSettings settings, double time)
    {
        double boot = BootDuration(settings);
        if (boot == 0) return null;
        double end = boot + settings.AmigaDurationSeconds;
        double phase = time % (end + (settings.PowerTransition ? 1.42 : 0));
        if (phase < boot) return phase;
        return settings.PowerTransition && phase >= end + 1.02 ? 0 : null;
    }
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
            ? (AmigaDemo)(1 + (int)(time / length % 5)) : settings.AmigaAnimation;
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
        var demo = CurrentDemo(settings, time + (revealNext ? .4 : 0));
        if (demo == AmigaDemo.Runner) runner ??= new AmigaRunner();
        if (contentPhase < boot)
        {
            bootSurface ??= SKSurface.Create(new SKImageInfo(640, 400));
            var original = KickstartFrames.At(settings.AmigaBoot, contentPhase);
            // Mark the surface dirty for a distinct snapshot: Avalonia can retain
            // multiple frame operations, each with independent image ownership.
            bootSurface.Canvas.Clear(SKColors.Black);
            bootSurface.Canvas.DrawImage(original, 0, 0);
            return bootSurface.Snapshot();
        }
        else
        {
            double demoTime = Math.Min(contentPhase - boot, settings.AmigaDurationSeconds);
            canvas.Clear(new SKColor(5, 7, 24));
            switch (demo)
            {
                case AmigaDemo.Boing: DrawBoing(canvas, demoTime); break;
                case AmigaDemo.Starflight: DrawStars(canvas, demoTime); break;
                case AmigaDemo.Runner: runner!.Draw(canvas, demoTime, paint); break;
                case AmigaDemo.Plasma: DrawPlasma(canvas, demoTime); break;
                default: DrawCopper(canvas, demoTime); break;
            }
            double scrollTime = Math.Floor(time / length) * settings.AmigaDurationSeconds
                + (revealNext ? settings.AmigaDurationSeconds : Math.Clamp(phase - boot, 0, settings.AmigaDurationSeconds));
            if (settings.ShowAmigaScroller) DrawScroller(canvas, settings, scrollTime, demo == AmigaDemo.Copper ? 135 : Math.Min(226, 261 - (float)settings.ScrollerWave));
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
    private static SKImage CreateBoingFloor()
    {
        const float horizon = 170, bottom = 288;
        using var floor = SKSurface.Create(new SKImageInfo(384, 288));
        var canvas = floor.Canvas;
        canvas.Clear(SKColors.Transparent);
        canvas.ClipRect(new SKRect(0, horizon, 384, bottom));
        using var grid = new SKPaint { Color = new SKColor(8, 9, 27) };
        canvas.DrawRect(0, horizon, 384, bottom - horizon, grid);
        var color = new SKColor(55, 38, 88);
        // Fade distant grid lines so they do not form a dense triangular ridge.
        using var fade = SKShader.CreateLinearGradient(new SKPoint(0, horizon),
            new SKPoint(0, horizon + 34), new[] { color.WithAlpha(0), color }, SKShaderTileMode.Clamp);
        grid.Shader = fade;
        // Project a level ground plane: equal world-depth steps become 1/z rows.
        for (int depth = 1; depth <= 48; depth++)
        {
            float y = horizon + (bottom - horizon) / depth;
            canvas.DrawLine(0, y, 384, y, grid);
        }
        // A wide fan covers the whole ground, not only a narrow center triangle.
        for (int x = -3840; x <= 4224; x += 48)
            canvas.DrawLine(192, horizon, x, bottom, grid);
        grid.Shader = null;
        grid.Color = new SKColor(30, 25, 46);
        canvas.DrawLine(0, horizon, 384, horizon, grid);
        return floor.Snapshot();
    }
    private void DrawBoing(SKCanvas canvas, double time)
    {
        paint.Color = SKColors.White;
        canvas.DrawImage(BoingFloor.Value, 0, 0, paint);
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
        surface.Dispose(); bootSurface?.Dispose(); plasma.Dispose(); paint.Dispose();
    }
}
