using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using C64Screensaver;
using System.Runtime.InteropServices;

if (!OperatingSystem.IsMacOS()) { Console.WriteLine("SKIP: native macOS fullscreen probe"); return 0; }
return AppBuilder.Configure<ProbeApp>().UsePlatformDetect().WithInterFont().StartWithClassicDesktopLifetime([]);

public sealed class ProbeApp : Application
{
    public override void Initialize() => Name = "Gruetze RetroSaver UI probe";
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint="sel_registerName")]
    private static extern IntPtr Selector(string name);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint="objc_msgSend")]
    private static extern nuint ReadUnsigned(IntPtr target, IntPtr selector);
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow(false, IntPtr.Zero, settings: new SaverSettings {
                AmigaMode = true, AmigaBoot = AmigaBootStyle.None, CrtEnabled = false });
            desktop.MainWindow = window;
            window.Opened += (_, _) =>
            {
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    int code = 0;
                    try
                    {
                        var handle = window.TryGetPlatformHandle() ?? throw new Exception("No native window handle");
                        if (handle.HandleDescriptor != "NSWindow") throw new Exception("Unexpected window: " + handle.HandleDescriptor);
                        nuint mask = ReadUnsigned(handle.Handle, Selector("styleMask"));
                        if (!window.IsVisible || window.WindowState != WindowState.FullScreen || (mask & (nuint)(1 << 14)) == 0)
                            throw new Exception($"Not native fullscreen: visible={window.IsVisible}, state={window.WindowState}, style={mask}");
                        Console.WriteLine($"PASS: actual AppKit fullscreen flag, {window.Bounds.Width:F0}x{window.Bounds.Height:F0} client area.");
                    }
                    catch (Exception e) { Console.Error.WriteLine(e.Message); code = 1; }
                    desktop.Shutdown(code);
                };
                timer.Start();
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
