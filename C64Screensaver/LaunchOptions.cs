using System;
using System.Globalization;

namespace C64Screensaver;

public enum LaunchMode { Fullscreen, Windowed, Settings, About, Preview, Help }
public enum LinuxBackend { Auto, X11, Wayland }

public sealed record LaunchOptions(LaunchMode Mode = LaunchMode.Fullscreen,
    long ParentHandle = 0, LinuxBackend Backend = LinuxBackend.Auto)
{
    public static bool TryParse(string[] args, bool isWindows, out LaunchOptions options, out string? error)
    {
        options = new();
        error = null;
        bool modeSet = false;
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].Trim().ToLowerInvariant();
            if (arg is "--x11" or "--wayland")
            {
                var backend = arg == "--x11" ? LinuxBackend.X11 : LinuxBackend.Wayland;
                if (options.Backend != LinuxBackend.Auto && options.Backend != backend)
                { error = "Choose either --x11 or --wayland."; return false; }
                options = options with { Backend = backend };
                continue;
            }
            if (modeSet) { error = "Choose only one launch mode."; return false; }
            modeSet = true;
            switch (arg)
            {
                case "/s": case "--fullscreen": options = options with { Mode = LaunchMode.Fullscreen }; break;
                case "--windowed": options = options with { Mode = LaunchMode.Windowed }; break;
                case "/c": case "--settings": options = options with { Mode = LaunchMode.Settings }; break;
                case "--about": options = options with { Mode = LaunchMode.About }; break;
                case "--help": case "-h": case "/?": options = options with { Mode = LaunchMode.Help }; break;
                default:
                    // Windows can pass the configuration dialog's owner as /c:HWND.
                    if (arg.StartsWith("/c:", StringComparison.Ordinal))
                    { options = options with { Mode = LaunchMode.Settings }; break; }
                    if (arg == "/p" || arg.StartsWith("/p:", StringComparison.Ordinal))
                    {
                        string handle = arg.Length > 3 ? arg[3..] :
                            arg == "/p" && i + 1 < args.Length ? args[++i] : "";
                        if (!isWindows || !long.TryParse(handle, NumberStyles.None, CultureInfo.InvariantCulture,
                                out long value) || value <= 0 || (IntPtr.Size == 4 && value > int.MaxValue))
                        { error = "Preview requires Windows and a valid parent window handle."; return false; }
                        options = options with { Mode = LaunchMode.Preview, ParentHandle = value };
                        break;
                    }
                    error = $"Unknown argument: {args[i]}";
                    return false;
            }
        }
        return true;
    }
}
