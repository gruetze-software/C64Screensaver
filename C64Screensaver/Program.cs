using Avalonia;
using System;
using System.Diagnostics;
using System.Globalization;

namespace C64Screensaver;

internal static class Program
{
    internal static LaunchOptions Options { get; private set; } = new();
    public const string Help = "Gruetze RetroSaver: --companion | --settings | --about | --windowed | --fullscreen\n" +
        "Linux backend: --x11 | --wayland (default: native Wayland when WAYLAND_DISPLAY is set)\n" +
        "Windows screensaver: /s | /c[:HWND] | /p HWND | /p:HWND\n" +
        "Fullscreen: move the mouse, click or press a key to exit; F toggles FPS.";

    [STAThread]
    public static int Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        if (!LaunchOptions.TryParse(args, OperatingSystem.IsWindows(), out var options, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(Help);
            return 2;
        }
        Options = options;
        if (options.Mode == LaunchMode.Help) { Console.WriteLine(Help); return 0; }
        try { return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args); }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
            Console.Error.WriteLine(Tools.GetExcMsg(ex));
            return 1;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>().UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                RenderingMode = [Win32RenderingMode.AngleEgl, Win32RenderingMode.Software]
            })
            .With(new X11PlatformOptions
            {
                RenderingMode = [X11RenderingMode.Glx, X11RenderingMode.Software],
                EnableMultiTouch = false,
                UseDBusMenu = false
            });
        if (OperatingSystem.IsLinux() && (Options.Backend == LinuxBackend.Wayland ||
            (Options.Backend == LinuxBackend.Auto &&
             !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))))
            builder = builder.UseWayland();
        return builder.WithInterFont().LogToTrace();
    }
}
