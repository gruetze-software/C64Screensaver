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
    [System.ThreadStatic] private static CpuCrtRenderer? softwareRenderer;
    private readonly SKImage image;
    private readonly CrtParameters parameters;
    private readonly double time;
    private readonly bool previewDetails;
    private readonly int quality;
    private readonly double? diskActivityTime;
    public Rect Bounds { get; }
    public CrtDrawOperation(Rect bounds, SKImage image, CrtParameters parameters, double time, bool previewDetails = false, int quality = 0, double? diskActivityTime = null)
    { Bounds = bounds; this.image = image; this.parameters = parameters; this.time = time; this.previewDetails = previewDetails; this.quality = quality; this.diskActivityTime = diskActivityTime; }
    public bool HitTest(Point p) => false;
    public bool Equals(ICustomDrawOperation? other) => false;
    public void Dispose() => image.Dispose();
    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } feature) return;
        using var lease = feature.Lease();
        var target = new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height);
        if (lease.GrContext == null)
        {
            // One reusable software target per rendering thread; never shared across threads.
            var cpu = softwareRenderer ??= new CpuCrtRenderer();
            cpu.Draw(lease.SkCanvas, target, image, parameters, time, quality, previewDetails);
        }
        else CrtRenderer.Draw(lease.SkCanvas, target, image, parameters, time, previewDetails);
        if (diskActivityTime is double activity) DiskActivityReflection.Draw(lease.SkCanvas, target, parameters, activity);
    }
}
