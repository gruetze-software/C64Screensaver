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
        var image = frameBuffer.Capture(session, settings, time);
        context.DrawRectangle(Brushes.Black, null, new Rect(Bounds.Size));
        context.Custom(new CrtDrawOperation(new Rect(Bounds.Size), image, CrtParameters.From(settings), time, PreviewDetails));
        if (session.TransitionTime is { } transition) CrtPowerEffect.Draw(context, new Rect(Bounds.Size), transition);
    }
}
