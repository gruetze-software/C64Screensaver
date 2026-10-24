using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SkiaSharp;

namespace C64Screensaver;

public sealed class C64Scene : Control
{
    private static readonly int[] Palette = Array.ConvertAll(
        new[] { "#000000", "#FFFFFF", "#883932", "#67B6BD", "#8B3F96", "#55A049", "#40318D", "#BFCE72",
            "#8B5429", "#574200", "#B86962", "#505050", "#787878", "#94E089", "#7869C4", "#9F9F9F" },
        color => unchecked((int)(uint)SKColor.Parse(color)));
    private readonly int[] framePalette = new int[16];
    private readonly int[] pixels = new int[384 * 288];
    private ScreenSession session = new();
    private SaverSettings settings = new();
    private bool disposed;
    private double? effectTime;
    public bool IsPowerTransitionActive => session.TransitionTime.HasValue;
    public bool PreviewDetails { get; set; }
    public void Initialize(SaverSettings configuration, bool preview, double width, double height, Random random)
    {
        effectTime = null;
        settings = configuration;
        session = new ScreenSession(random, settings.Programs, settings);
    }
    public void ShowProgramPreview() { session.StartPreview(); InvalidateVisual(); }
    public void Advance(double width, double height, double seconds)
    {
        session.Advance(seconds, settings.SpeedMultiplier);
        InvalidateVisual();
    }
    public void PreviewPowerTransition() { session.BeginTransitionPreview(); InvalidateVisual(); }
    public void AdvancePreview(double seconds, bool runProgram)
    {
        effectTime = (effectTime ?? session.Elapsed) + seconds;
        if (runProgram || session.TransitionTime.HasValue) session.Advance(seconds, settings.SpeedMultiplier);
        InvalidateVisual();
    }
    public void Clear() => disposed = true;
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (disposed || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        double time = effectTime ?? session.Elapsed;
        var machine = session.Machine;
        // In borderless mode every non-black pixel follows a very gradual
        // brightness cycle, including otherwise untouched background pixels.
        double level = settings.FillScreen ? 0.96 + 0.04 * Math.Sin(time / 16) : 1;
        for (int i = 0; i < Palette.Length; i++)
        {
            var color = new SKColor(unchecked((uint)Palette[i]));
            framePalette[i] = unchecked((int)(uint)new SKColor(
                (byte)(color.Red * level), (byte)(color.Green * level), (byte)(color.Blue * level)));
        }
        int border = session.Executing && settings.CycleColors ? machine.Border : 14;
        Array.Fill(pixels, framePalette[border]);
        for (int y = 44; y < 244; y++) Array.Fill(pixels, framePalette[machine.Background], y * 384 + 32, 320);
        for (int cell = 0; cell < 1000; cell++)
        {
            byte[]? rows = PixelCharacters.Rows(machine.Characters[cell]);
            if (rows == null) continue;
            int x = 32 + cell % 40 * 8, y = 44 + cell / 40 * 8;
            int color = framePalette[machine.Colors[cell] & 15];
            for (int gy = 0; gy < 8; gy++)
                for (int gx = 0; gx < 8; gx++)
                    if ((rows[gy] & (128 >> gx)) != 0) pixels[(y + gy) * 384 + x + gx] = color;
        }
        if (!session.Executing && (int)(time / 0.48) % 2 == 0)
        {
            int cell = machine.CursorIndex;
            for (int y = 0; y < 8; y++)
                Array.Fill(pixels, framePalette[14], (44 + cell / 40 * 8 + y) * 384 + 32 + cell % 40 * 8, 8);
        }
        // Immutable image snapshots belong to their draw operation, allowing Avalonia
        // to render on its compositor thread without sharing a mutable framebuffer.
        using var bitmap = new SKBitmap(new SKImageInfo(384, 288, SKColorType.Bgra8888, SKAlphaType.Premul));
        Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixels.Length);
        bitmap.SetImmutable();
        var image = SKImage.FromBitmap(bitmap);
        if (settings.FillScreen)
        {
            var content = image.Subset(new SKRectI(32, 44, 352, 244));
            image.Dispose();
            image = content;
        }
        context.DrawRectangle(Brushes.Black, null, new Rect(Bounds.Size));
        context.Custom(new CrtDrawOperation(new Rect(Bounds.Size), image, CrtParameters.From(settings), time, PreviewDetails));
        if (session.TransitionTime is { } transition) CrtPowerEffect.Draw(context, new Rect(Bounds.Size), transition);
    }
}
