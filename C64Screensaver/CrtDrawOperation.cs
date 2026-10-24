using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace C64Screensaver;

// Avalonia owns the immutable image until its render operation is retired.
public sealed class CrtDrawOperation : ICustomDrawOperation
{
    public static string? ShaderError => CrtRenderer.ShaderError;
    public static bool ShaderAvailable => CrtRenderer.ShaderAvailable;
    private readonly SKImage image;
    private readonly CrtParameters parameters;
    private readonly double time;
    private readonly bool previewDetails;
    public Rect Bounds { get; }
    public CrtDrawOperation(Rect bounds, SKImage image, CrtParameters parameters, double time, bool previewDetails = false)
    { Bounds = bounds; this.image = image; this.parameters = parameters; this.time = time; this.previewDetails = previewDetails; }
    public bool HitTest(Point p) => false;
    public bool Equals(ICustomDrawOperation? other) => false;
    public void Dispose() => image.Dispose();
    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } feature) return;
        using var lease = feature.Lease();
        CrtRenderer.Draw(lease.SkCanvas, new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height),
            image, parameters, time, previewDetails);
    }
}
