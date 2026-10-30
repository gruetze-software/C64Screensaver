using System;
using SkiaSharp;
namespace C64Screensaver.NativeCore;

// Each session owns its GPU context/target and is serialized by Session.Gate.
internal sealed class MetalRenderTarget : IDisposable
{
    private static readonly object ConfigurationGate = new();
    private static IntPtr device, queue;
    private GRContext? context;
    private SKSurface? surface;
    private bool attempted;
    private int width, height;
    public bool Active => surface != null;
    internal static bool Configure(IntPtr metalDevice, IntPtr commandQueue)
    {
        if (!OperatingSystem.IsMacOS() || metalDevice == IntPtr.Zero || commandQueue == IntPtr.Zero) return false;
        lock (ConfigurationGate) { device = metalDevice; queue = commandQueue; }
        return true;
    }
    public SKSurface? GetSurface(SKImageInfo info)
    {
        try
        {
            if (!attempted)
            {
                attempted = true;
                lock (ConfigurationGate)
                {
                    if (device == IntPtr.Zero || queue == IntPtr.Zero) return null;
                    using var backend = new GRMtlBackendContext { DeviceHandle = device, QueueHandle = queue };
                    context = GRContext.CreateMetal(backend);
                    context?.SetResourceCacheLimit(64 * 1024 * 1024);
                }
            }
            if (context == null) return null;
            if (surface == null || width != info.Width || height != info.Height)
            {
                surface?.Dispose();
                surface = SKSurface.Create(context, false, info);
                if (surface == null) { Disable(); return null; }
                width = info.Width; height = info.Height;
            }
            return surface;
        }
        catch { Disable(); return null; }
    }
    public void Disable() { surface?.Dispose(); surface = null; context?.Dispose(); context = null; attempted = true; }
    public void Dispose() => Disable();
}
