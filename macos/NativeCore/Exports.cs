using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using C64Screensaver;
using SkiaSharp;

namespace C64Screensaver.NativeCore;

[JsonSerializable(typeof(SaverSettings))]
internal partial class SettingsJson : JsonSerializerContext { }

public static unsafe class Exports
{
    private sealed class Session(SaverSettings settings, int seed)
    {
        internal readonly object Gate = new();
        internal readonly SaverSettings Settings = settings;
        internal readonly ScreenSession Screen = new(new Random(seed), settings.Programs, settings);
        internal readonly C64FrameBuffer Frame = new();
        internal bool Closed;
        internal double Elapsed;
        internal readonly AmigaFrameBuffer? Amiga = settings.AmigaMode ? new AmigaFrameBuffer() : null;
        internal readonly CpuCrtRenderer Crt = new();
        internal readonly MetalRenderTarget Metal = new();
    }

    private static readonly ConcurrentDictionary<ulong, Session> Sessions = new();
    private static long nextId;
    private static readonly object LoaderGate = new();
    private static IntPtr skiaLibrary;

    [UnmanagedCallersOnly(EntryPoint = "c64_abi_version", CallConvs = [typeof(CallConvCdecl)])]
    public static int Version() => 1;

    // The host supplies an absolute bundle path. NativeAOT and Skia stay loaded for
    // the lifetime of the host process; unloading a NativeAOT library is unsupported.
    [UnmanagedCallersOnly(EntryPoint = "c64_load_skia", CallConvs = [typeof(CallConvCdecl)])]
    public static int LoadSkia(byte* path, int length)
    {
        try
        {
            if (path == null || length < 1 || length > 32768) return 0;
            string name = System.Text.Encoding.UTF8.GetString(new ReadOnlySpan<byte>(path, length));
            if (!System.IO.Path.IsPathFullyQualified(name)) return 0;
            lock (LoaderGate)
            {
                if (skiaLibrary != IntPtr.Zero) return 1;
                var handle = NativeLibrary.Load(name);
                NativeLibrary.SetDllImportResolver(typeof(SKBitmap).Assembly,
                    (library, assembly, searchPath) => library == "libSkiaSharp" ? handle : IntPtr.Zero);
                skiaLibrary = handle;
                return 1;
            }
        }
        catch { return 0; }
    }

    [UnmanagedCallersOnly(EntryPoint = "c64_set_metal", CallConvs = [typeof(CallConvCdecl)])]
    public static int SetMetal(IntPtr device, IntPtr queue) => MetalRenderTarget.Configure(device, queue) ? 1 : 0;

    [UnmanagedCallersOnly(EntryPoint = "c64_render_backend", CallConvs = [typeof(CallConvCdecl)])]
    public static int RenderBackend(ulong id) => Sessions.TryGetValue(id, out var session) && session.Metal.Active ? 1 : 0;

    [UnmanagedCallersOnly(EntryPoint = "c64_create", CallConvs = [typeof(CallConvCdecl)])]
    public static ulong Create(byte* json, int length, int seed)
    {
        try
        {
            if (length < 0 || length > 65536 || (json == null && length != 0)) return 0;
            var settings = length == 0 ? new SaverSettings()
                : JsonSerializer.Deserialize(new ReadOnlySpan<byte>(json, length), SettingsJson.Default.SaverSettings);
            if (settings == null) return 0;
            settings.Validate();
            ulong id = (ulong)System.Threading.Interlocked.Increment(ref nextId);
            return Sessions.TryAdd(id, new Session(settings, seed)) ? id : 0;
        }
        catch { return 0; }
    }

    [UnmanagedCallersOnly(EntryPoint = "c64_render", CallConvs = [typeof(CallConvCdecl)])]
    public static int Render(ulong id, byte* pixels, int capacity, int width, int height, int stride, double seconds)
    {
        try
        {
            if (pixels == null || width < 1 || height < 1 || width > 8192 || height > 8192
                || (long)width * height > 8388608 || stride < (long)width * 4
                || (long)stride * height > capacity || !double.IsFinite(seconds) || seconds < 0
                || !Sessions.TryGetValue(id, out var session)) return 0;
            lock (session.Gate)
            {
                if (session.Closed) return 0;
                var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
                session.Elapsed += Math.Min(seconds, .25);
                if (!session.Settings.AmigaMode || session.Screen.TransitionTime.HasValue) session.Screen.Advance(Math.Min(seconds, .25), session.Settings.SpeedMultiplier);
                using var image = session.Settings.AmigaMode
                    ? session.Amiga!.CaptureFrame(session.Settings, session.Elapsed)
                    : session.Frame.Capture(session.Screen, session.Settings, session.Screen.Elapsed);
                var gpu = session.Metal.GetSurface(info);
                if (gpu != null)
                {
                    try
                    {
                        DrawFrame(session, gpu.Canvas, image, width, height, true);
                        gpu.Canvas.Flush();
                        if (gpu.ReadPixels(info, (IntPtr)pixels, stride, 0, 0)) return 1;
                    }
                    catch { /* A device/context failure must fall back to the same CPU frame. */ }
                    session.Metal.Disable();
                }
                using var surface = SKSurface.Create(info, (IntPtr)pixels, stride);
                if (surface == null) return 0;
                DrawFrame(session, surface.Canvas, image, width, height, false);
                surface.Canvas.Flush();
                return 1;
            }
        }
        catch { return 0; }
    }

    private static void DrawFrame(Session session, SKCanvas canvas, SKImage image, int width, int height, bool gpu)
    {
        canvas.Clear(SKColors.Black);
        var bounds = new SKRect(0, 0, width, height);
        var parameters = CrtParameters.From(session.Settings);
        double time = session.Settings.AmigaMode ? session.Elapsed : session.Screen.Elapsed;
        if (gpu) CrtRenderer.Draw(canvas, bounds, image, parameters, time);
        else session.Crt.Draw(canvas, bounds, image, parameters, time, session.Settings.NativeRenderQuality);
        if (session.Settings.AmigaMode && session.Settings.AmigaDiskActivity
            && AmigaFrameBuffer.StartupTime(session.Settings, session.Elapsed) is double activity)
            DiskActivityReflection.Draw(canvas, bounds, parameters, activity);
        if ((session.Screen.TransitionTime ?? (session.Settings.AmigaMode ? AmigaFrameBuffer.PowerTime(session.Settings, session.Elapsed) : null)) is double transition)
            CrtPowerRenderer.Draw(canvas, width, height, transition);
    }

    [UnmanagedCallersOnly(EntryPoint = "c64_test_power", CallConvs = [typeof(CallConvCdecl)])]
    public static int TestPower(ulong id)
    {
        try
        {
            if (!Sessions.TryGetValue(id, out var session)) return 0;
            lock (session.Gate)
            {
                if (session.Closed) return 0;
                session.Screen.BeginTransitionPreview();
                return 1;
            }
        }
        catch { return 0; }
    }
    [UnmanagedCallersOnly(EntryPoint = "c64_destroy", CallConvs = [typeof(CallConvCdecl)])]
    public static void Destroy(ulong id)
    {
        try
        {
            if (Sessions.TryRemove(id, out var session))
                lock (session.Gate)
                {
                    session.Closed = true;
                    session.Amiga?.Dispose();
                    session.Crt.Dispose();
                    session.Metal.Dispose();
                }
        }
        catch { }
    }
}
