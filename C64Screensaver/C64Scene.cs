using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace C64Screensaver;

public sealed class C64Scene : Control
{
    private readonly C64FrameBuffer frameBuffer = new();
    private ScreenSession session = new();
    private SaverSettings settings = new();
    private bool disposed;
    private double amigaTime;
    private AmigaFrameBuffer? amiga;
    private double? effectTime;
    public bool IsPowerTransitionActive => session.TransitionTime.HasValue || (settings.AmigaMode && AmigaFrameBuffer.PowerTime(settings, amigaTime).HasValue);
    public bool PreviewDetails { get; set; }
    public void Initialize(SaverSettings configuration, bool preview, double width, double height, Random random)
    {
        effectTime = null;
        amigaTime = 0;
        settings = configuration;
        amiga?.Dispose();
        amiga = settings.AmigaMode ? new AmigaFrameBuffer() : null;
        session = new ScreenSession(random, settings.Programs, settings);
    }
    public void ShowProgramPreview() { session.StartPreview(); InvalidateVisual(); }
    public void Advance(double width, double height, double seconds)
    {
        if (settings.AmigaMode) amigaTime += seconds;
        if (!settings.AmigaMode || session.TransitionTime.HasValue) session.Advance(seconds, settings.SpeedMultiplier);
        InvalidateVisual();
    }
    public void PreviewPowerTransition() { session.BeginTransitionPreview(); InvalidateVisual(); }
    public void AdvancePreview(double seconds, bool runProgram)
    {
        if (settings.AmigaMode) amigaTime += seconds;
        effectTime = (effectTime ?? session.Elapsed) + seconds;
        if ((!settings.AmigaMode && runProgram) || session.TransitionTime.HasValue) session.Advance(seconds, settings.SpeedMultiplier);
        InvalidateVisual();
    }
    public void Clear() { disposed = true; amiga?.Dispose(); amiga = null; }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (disposed || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        double time = settings.AmigaMode ? amigaTime : effectTime ?? session.Elapsed;
        var image = settings.AmigaMode ? amiga!.CaptureFrame(settings, time) : frameBuffer.Capture(session, settings, time);
        context.DrawRectangle(Brushes.Black, null, new Rect(Bounds.Size));
        context.Custom(new CrtDrawOperation(new Rect(Bounds.Size), image, CrtParameters.From(settings), time, PreviewDetails, settings.NativeRenderQuality,
            settings.AmigaMode && settings.AmigaDiskActivity ? AmigaFrameBuffer.StartupTime(settings, time) : null));
        if ((session.TransitionTime ?? (settings.AmigaMode ? AmigaFrameBuffer.PowerTime(settings, time) : null)) is { } transition) CrtPowerEffect.Draw(context, new Rect(Bounds.Size), transition);
    }
}
