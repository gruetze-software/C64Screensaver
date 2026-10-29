using System;
using SkiaSharp;
namespace C64Screensaver;

// Bound expensive software shader work independently of the physical monitor size.
public sealed class CpuCrtRenderer : IDisposable
{
    private SKSurface? stage;
    private int width, height;
    public void Draw(SKCanvas canvas, SKRect bounds, SKImage image, CrtParameters parameters,
        double time, int quality = 0, bool previewDetails = false)
    {
        if (!parameters.Enabled) { CrtRenderer.Draw(canvas, bounds, image, parameters, time, previewDetails); return; }
        int limitWidth = quality == 2 ? 1280 : quality == 1 ? 960 : 640;
        int limitHeight = quality == 2 ? 720 : quality == 1 ? 540 : 360;
        double factor = Math.Min(1, Math.Min(limitWidth / (double)bounds.Width, limitHeight / (double)bounds.Height));
        int w = Math.Max(1, (int)Math.Round(bounds.Width * factor)), h = Math.Max(1, (int)Math.Round(bounds.Height * factor));
        if (factor == 1) { CrtRenderer.Draw(canvas, bounds, image, parameters, time, previewDetails); return; }
        if (stage == null || w != width || h != height)
        {
            stage?.Dispose();
            stage = SKSurface.Create(new SKImageInfo(w, h));
            width = w; height = h;
        }
        stage.Canvas.Clear(SKColors.Black);
        CrtRenderer.Draw(stage.Canvas, new SKRect(0, 0, w, h), image, parameters, time, previewDetails);
        using var finished = stage.Snapshot();
        canvas.DrawImage(finished, bounds, new SKSamplingOptions(SKFilterMode.Linear));
    }
    public void Dispose() { stage?.Dispose(); stage = null; }
}
