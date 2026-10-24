using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace C64Screensaver;

public sealed record LinuxProbeResult(IdleSample? Sample, string Message);

public sealed class LinuxSessionProbe
{
    private readonly Func<string[], Task<string?>> call;
    private readonly Func<long?> xIdle;
    private readonly string desktop;
    private readonly bool wayland;
    public LinuxSessionProbe(Func<string[], Task<string?>>? call = null, Func<long?>? xIdle = null,
        string? desktop = null, bool? wayland = null)
    {
        this.call = call ?? RunGdbus;
        this.xIdle = xIdle ?? ReadXIdle;
        this.desktop = (desktop ?? Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "").ToUpperInvariant();
        this.wayland = wayland ?? (Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") == "wayland" ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")));
    }
    public static long? ParseNumber(string? value) =>
        value != null && Regex.Match(value.Trim(), @"^\((?:uint(?:32|64) )?([0-9]+),?\)$") is { Success: true } match &&
        long.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out long n) ? n : null;
    public static bool? ParseBool(string? value) => value?.Trim() switch
    { "(true,)" => true, "(false,)" => false, _ => null };

    private Task<string?> Query(string bus, string path, string method, params string[] arguments)
    {
        string[] command = ["call", "--session", "--timeout", "2", "--dest", bus,
            "--object-path", path, "--method", method, ..arguments];
        return call(command);
    }
    public async Task<LinuxProbeResult> ReadAsync()
    {
        bool gnome = desktop.Contains("GNOME") || desktop.Contains("UNITY");
        bool cinnamon = desktop.Contains("CINNAMON");
        bool mate = desktop.Contains("MATE");
        bool xfce = desktop.Contains("XFCE");
        if (!gnome && !cinnamon && !mate && !xfce)
            return new(null, "Automatic activation is not supported on this desktop yet.");
        long? idle;
        if (gnome)
            idle = ParseNumber(await Query("org.gnome.Mutter.IdleMonitor", "/org/gnome/Mutter/IdleMonitor/Core",
                "org.gnome.Mutter.IdleMonitor.GetIdletime"));
        else
        {
            // XWayland cannot report authoritative whole-session input activity.
            if (wayland) return new(null, "This desktop's Wayland idle interface is not supported yet.");
            idle = xIdle();
        }
        if (!idle.HasValue) return new(null, "Idle time unavailable. Check gdbus, desktop session and sandbox permissions.");
        string saver = gnome ? "org.gnome.ScreenSaver" : cinnamon ? "org.cinnamon.ScreenSaver" :
            mate ? "org.mate.ScreenSaver" : "org.xfce.ScreenSaver";
        string path = "/" + saver.Replace('.', '/');
        bool? locked = ParseBool(await Query(saver, path, saver + ".GetActive"));
        if (!locked.HasValue) return new(null, "Screen lock status unavailable; automatic activation is paused.");
        if (locked.Value) return new(new(idle.Value, true, false), "Desktop is locked or blanked; animation is stopped.");
        bool? inhibited;
        if (gnome || cinnamon || mate)
        {
            string manager = "org.gnome.SessionManager";
            inhibited = ParseBool(await Query(manager, "/" + manager.Replace('.', '/'), manager + ".IsInhibited", "8"));
        }
        else
            inhibited = ParseBool(await Query("org.freedesktop.PowerManagement", "/org/freedesktop/PowerManagement/Inhibit",
                "org.freedesktop.PowerManagement.Inhibit.HasInhibit"));
        if (!inhibited.HasValue)
            return new(null, "Video/presentation inhibition status unavailable; automatic activation is paused.");
        return new(new(idle.Value, false, inhibited.Value), inhibited.Value
            ? "A desktop inhibitor is active; automatic activation is paused."
            : $"Ready on {desktop}: idle {idle.Value / 1000} seconds.");
    }
    private static async Task<string?> RunGdbus(string[] arguments)
    {
        try
        {
            var start = new ProcessStartInfo("gdbus") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string argument in arguments) start.ArgumentList.Add(argument);
            using var process = Process.Start(start);
            if (process == null) return null;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(4));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { process.Kill(); await process.WaitForExitAsync(); return null; }
            await error;
            string text = await output;
            return process.ExitCode == 0 ? text.Trim() : null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or System.IO.IOException)
        { return null; }
    }

    private static long? ReadXIdle()
    {
        try
        {
            IntPtr display = XOpenDisplay(IntPtr.Zero);
            if (display == IntPtr.Zero) return null;
            try
            {
                if (XScreenSaverQueryExtension(display, out _, out _) == 0) return null;
                IntPtr info = XScreenSaverAllocInfo();
                if (info == IntPtr.Zero) return null;
                try
                {
                    if (XScreenSaverQueryInfo(display, XDefaultRootWindow(display), info) == 0) return null;
                    ulong idle = Marshal.PtrToStructure<XScreenSaverInfo>(info).Idle.ToUInt64();
                    return idle <= long.MaxValue ? (long)idle : null;
                }
                finally { XFree(info); }
            }
            finally { XCloseDisplay(display); }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) { return null; }
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct XScreenSaverInfo
    {
        public UIntPtr Window;
        public int State, Kind;
        public UIntPtr TilOrSince, Idle, EventMask;
    }
    [DllImport("libX11.so.6")] private static extern IntPtr XOpenDisplay(IntPtr name);
    [DllImport("libX11.so.6")] private static extern int XCloseDisplay(IntPtr display);
    [DllImport("libX11.so.6")] private static extern UIntPtr XDefaultRootWindow(IntPtr display);
    [DllImport("libX11.so.6")] private static extern int XFree(IntPtr value);
    [DllImport("libXss.so.1")] private static extern int XScreenSaverQueryExtension(IntPtr display, out int eventBase, out int errorBase);
    [DllImport("libXss.so.1")] private static extern IntPtr XScreenSaverAllocInfo();
    [DllImport("libXss.so.1")] private static extern int XScreenSaverQueryInfo(IntPtr display, UIntPtr window, IntPtr info);
}
