using System;
using System.Diagnostics;
using C64Screensaver.NativeCore;

unsafe
{
    delegate* unmanaged[Cdecl]<byte*, int, int, ulong> create = &Exports.Create;
    delegate* unmanaged[Cdecl]<ulong, byte*, int, int, int, int, double, int> render = &Exports.Render;
    delegate* unmanaged[Cdecl]<ulong, void> destroy = &Exports.Destroy;
    Console.WriteLine("CPU raster rendering; timings exclude AppKit presentation. Same CRT defaults, different pixel budgets.");
    foreach (var (width, height) in new[] { (960, 540), (1280, 720), (1920, 1080) })
    {
        ulong session = create(null, 0, 123);
        byte[] bytes = new byte[width * height * 4];
        fixed (byte* pixels = bytes)
        {
            for (int i = 0; i < 3; i++)
                if (render(session, pixels, bytes.Length, width, height, width * 4, 1.0 / 30) != 1) throw new Exception("Warmup failed");
            var clock = Stopwatch.StartNew();
            for (int i = 0; i < 12; i++)
                if (render(session, pixels, bytes.Length, width, height, width * 4, 1.0 / 30) != 1) throw new Exception("Render failed");
            clock.Stop();
            Console.WriteLine($"{width}x{height}: {clock.Elapsed.TotalMilliseconds / 12:F2} ms/frame; copy {bytes.Length / 1048576.0:F2} MiB/frame");
        }
        destroy(session);
    }
}
