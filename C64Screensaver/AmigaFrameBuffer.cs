using System;
using System.Runtime.InteropServices;
using SkiaSharp;
namespace C64Screensaver;

public enum AmigaBootStyle { None, Classic13, Purple20, Purple31 }
public enum AmigaDemo { Cycle = 0, Runner = 5, Plasma = 4 }

// Procedural demos with historical Kickstart screen captures; no ROM is loaded.
public sealed class AmigaFrameBuffer : IDisposable
{
    private readonly SKSurface surface = SKSurface.Create(new SKImageInfo(384, 288));
    private SKSurface? bootSurface;
    private readonly SKBitmap plasma = new(96, 72);
    private readonly int[] plasmaPixels = new int[96 * 72];
    private readonly SKPaint paint = new() { IsAntialias = false };
    private AmigaRunner? runner;
    private static readonly Lazy<SKImage> GlyphAtlas = new(CreateGlyphAtlas);
    private static readonly float[] Sines = CreateSines();
    private static readonly SKColor[] PlasmaColors = CreatePalette();
    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear);
    private bool disposed;

    public AmigaFrameBuffer()
    {
        // Prepare sprites during startup, before a demo needs them for its first frame.
        _ = GlyphAtlas.Value;
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
            ? ((long)(time / length) % 2 == 0 ? AmigaDemo.Runner : AmigaDemo.Plasma) : settings.AmigaAnimation;
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
                case AmigaDemo.Runner: runner!.Draw(canvas, demoTime, paint); break;
                default: DrawPlasma(canvas, demoTime); break;
            }
            double scrollTime = Math.Floor(time / length) * settings.AmigaDurationSeconds
                + (revealNext ? settings.AmigaDurationSeconds : Math.Clamp(phase - boot, 0, settings.AmigaDurationSeconds));
            if (settings.ShowAmigaScroller) DrawScroller(canvas, settings, scrollTime, Math.Min(226, 261 - (float)settings.ScrollerWave));
        }
        return surface.Snapshot();
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
